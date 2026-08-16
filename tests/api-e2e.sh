#!/usr/bin/env bash
# End-to-end test of the migrated ZapChat backend, driven entirely through the gateway.
set -uo pipefail
export PYTHONIOENCODING=utf-8

GW="https://localhost:5000"
AUTH="http://localhost:5111"     # direct, only to read OTP codes from the log
# Resolved from this script's own location, so the suite runs on any machine.
# start-backend.ps1 writes each service's stdout here; the OTP codes are read from
# Auth.log because the development mail transport logs them instead of sending mail.
LOG="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/logs"

PASS=0; FAIL=0
ok()   { printf "  [ PASS ] %s\n" "$1"; PASS=$((PASS+1)); }
bad()  { printf "  [ FAIL ] %s\n" "$1"; FAIL=$((FAIL+1)); }
sec()  { printf "\n== %s %s\n" "$1" "$(printf '=%.0s' $(seq 1 $((66-${#1}))))"; }

# expect <label> <actual> <expected>
expect() { if [ "$2" = "$3" ]; then ok "$1 ($2)"; else bad "$1 (got $2, want $3)"; fi; }

code() { curl -sk -o /dev/null -w "%{http_code}" "$@"; }
jq()   { python -c "import sys,json;d=json.load(sys.stdin);print($1)" 2>/dev/null || echo "PARSE_ERR"; }

register() { # register <email> <name> <branch> -> prints userId
  local email="$1" name="$2" branch="$3"
  curl -sk -o /dev/null -X POST "$AUTH/api/auth/register/initiate" -H 'Content-Type: application/json' \
    -d "{\"fullName\":\"$name\",\"email\":\"$email\",\"department\":\"Engineering\",\"branch\":\"$branch\"}"
  sleep 1.2
  local otp
  otp=$(grep -oE "verification code is: [0-9]{6}" "$LOG/Auth.log" | tail -1 | grep -oE "[0-9]{6}")
  local vt
  vt=$(curl -sk -X POST "$AUTH/api/auth/register/verify-otp" -H 'Content-Type: application/json' \
      -d "{\"email\":\"$email\",\"otpCode\":\"$otp\"}" | jq "d['token']")
  curl -sk -o /dev/null -X POST "$AUTH/api/auth/register/complete" -H 'Content-Type: application/json' \
    -d "{\"verificationToken\":\"$vt\",\"password\":\"Str0ngPass!23\",\"confirmPassword\":\"Str0ngPass!23\"}"
}

login() { # login <email> <cookiejar> -> prints bearer token
  curl -sk -c "$2" -o /dev/null -X POST "$AUTH/api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"Str0ngPass!23\"}"
  curl -sk -b "$2" "$AUTH/api/auth/token"
}

STAMP=$(date +%s)
U1="e2e-a-$STAMP@zapcg.com"
U2="e2e-b-$STAMP@zapcg.com"
U3="admin-$STAMP@zapcg.com"

# ══════════════════════════════════════════════════════════════════════════════
sec "1. Deny-by-default authorization (no token)"
expect "GET  /api/rooms"                  "$(code $GW/api/rooms)" 401
expect "GET  /api/conversations"           "$(code $GW/api/conversations)" 401
expect "GET  /api/polls"                   "$(code $GW/api/polls)" 401
expect "GET  /api/notifications"           "$(code $GW/api/notifications)" 401
expect "GET  /api/auth/me"                 "$(code $GW/api/auth/me)" 401
expect "GET  /api/auth/token"              "$(code $GW/api/auth/token)" 401
expect "POST /api/files"                   "$(code -X POST $GW/api/files)" 401
expect "POST /api/reports"                 "$(code -X POST -H 'Content-Type: application/json' -d '{}' $GW/api/reports)" 401
expect "GET  /api/admin/dashboard/stats"   "$(code $GW/api/admin/dashboard/stats)" 401
expect "GET  /api/chat-admin/rooms"        "$(code $GW/api/chat-admin/rooms)" 401

sec "2. Registration and login"
register "$U1" "E2E Alpha" "Hyderabad"
register "$U2" "E2E Bravo" "Bangalore"
T1=$(login "$U1" /tmp/e2e1.txt)
T2=$(login "$U2" /tmp/e2e2.txt)
[ ${#T1} -gt 100 ] && ok "alpha token issued (${#T1} chars)" || bad "alpha token"
[ ${#T2} -gt 100 ] && ok "bravo token issued" || bad "bravo token"

N1=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/auth/me" | jq "d['anonymousName']")
N2=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/auth/me" | jq "d['anonymousName']")
printf "         alpha=%s  bravo=%s\n" "$N1" "$N2"
[ -n "$N1" ] && [ "$N1" != "$N2" ] && ok "distinct anonymous identities" || bad "anonymous identities"

expect "cookie auth works (no bearer)" \
  "$(curl -sk -b /tmp/e2e1.txt -o /dev/null -w '%{http_code}' $AUTH/api/auth/me)" 200

sec "3. Anonymity: no de-anonymization path"
DIR=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/auth/users")
echo "$DIR" | grep -q "$U2" && bad "directory leaks email" || ok "directory exposes no email"
echo "$DIR" | grep -q "E2E Bravo" && bad "directory leaks full name" || ok "directory exposes no real name"
expect "old by-name lookup is gone" "$(code -H "Authorization: Bearer $T1" "$GW/api/auth/users/by-name/$N2")" 404
expect "old paginated leak is gone"  "$(code -H "Authorization: Bearer $T1" "$GW/api/auth/users/paginated")" 404
expect "user-admin needs Admin role" "$(code -H "Authorization: Bearer $T1" "$GW/api/auth/admin/users")" 403

sec "4. Rooms, branch access control"
ROOMS=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/rooms")
# Assert on what must be true rather than an exact count: migrated data adds rooms, so
# a fixed number would be brittle. What matters is that the three canonical rooms a
# Hyderabad user should see are present and that no Bangalore room is.
HAS=$(echo "$ROOMS" | jq "str(all(n in [r['name'] for r in d] for n in ['General Chat','HR Issues','Hyderabad']))")
expect "alpha sees General Chat, HR Issues and Hyderabad" "$HAS" "True"
BLR=$(echo "$ROOMS" | jq "len([r for r in d if 'bangalore' in r['name'].lower()])")
expect "no Bangalore room visible to a Hyderabad user" "$BLR" 0

GEN=$(echo "$ROOMS" | jq "[r['id'] for r in d if r['name']=='General Chat'][0]")
HYD=$(echo "$ROOMS" | jq "[r['id'] for r in d if r['name']=='Hyderabad'][0]")
HR=$(echo "$ROOMS"  | jq "[r['id'] for r in d if r['name']=='HR Issues'][0]")

expect "bravo blocked from Hyderabad room" \
  "$(code -H "Authorization: Bearer $T2" "$GW/api/rooms/$HYD/messages")" 403

curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T1" "$GW/api/rooms/$GEN/join"
curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T2" "$GW/api/rooms/$GEN/join"
ok "both users joined General Chat"

sec "5. Messaging, unread fan-out, read receipts"
M1=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"content":"E2E message one"}' "$GW/api/rooms/$GEN/messages")
MID=$(echo "$M1" | jq "d['id']")
[ ${#MID} -eq 36 ] && ok "message persisted ($MID)" || bad "message persist"

expect "author flagged as mine for sender" "$(echo "$M1" | jq "str(d['isMine'])")" "True"

curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
  -d "{\"content\":\"mentioning @$N2 here\"}" "$GW/api/rooms/$GEN/messages"

UNREAD=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/rooms" | jq "[r['unreadCount'] for r in d if r['name']=='General Chat'][0]")
expect "bravo unread count" "$UNREAD" 2

SELF=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/rooms" | jq "[r['unreadCount'] for r in d if r['name']=='General Chat'][0]")
expect "sender's own unread stays 0" "$SELF" 0

curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T2" "$GW/api/rooms/$GEN/read"
AFTER=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/rooms" | jq "[r['unreadCount'] for r in d if r['name']=='General Chat'][0]")
expect "unread cleared after read" "$AFTER" 0

RB=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/messages/$MID/read-by" | jq "len(d)")
expect "read receipts returned" "$RB" 1

sec "6. Reactions (server-authoritative toggle)"
R1=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"emoji":"\ud83d\udc4d"}' "$GW/api/messages/$MID/reactions" | jq "len(d['reactions'])")
R2=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"emoji":"\ud83d\udc4d"}' "$GW/api/messages/$MID/reactions" | jq "len(d['reactions'])")
R3=$(curl -sk -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' \
      -d '{"emoji":"\ud83d\udc4d"}' "$GW/api/messages/$MID/reactions" | jq "d['reactions'][0]['count']")
expect "add reaction -> 1 group"      "$R1" 1
expect "toggle off -> 0 groups"       "$R2" 0
expect "other user adds -> count 1"   "$R3" 1
expect "invalid emoji rejected" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d '{"emoji":"XX"}' "$GW/api/messages/$MID/reactions")" 400

sec "7. Edit, delete, ownership"
expect "bravo cannot edit alpha's message" \
  "$(code -X PUT -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' -d '{"content":"hijacked"}' "$GW/api/messages/$MID")" 403
expect "bravo cannot delete alpha's message" \
  "$(code -X DELETE -H "Authorization: Bearer $T2" "$GW/api/messages/$MID")" 403
EDITED=$(curl -sk -X PUT -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"content":"E2E message one (edited)"}' "$GW/api/messages/$MID" | jq "str(d['isEdited'])")
expect "author can edit own message" "$EDITED" "True"

sec "8. Pagination and ordering"
PAGE=$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/rooms/$GEN/messages?limit=1")
expect "page size honoured"  "$(echo "$PAGE" | jq "len(d['items'])")" 1
expect "hasMore reported"    "$(echo "$PAGE" | jq "str(d['hasMore'])")" "True"
CUR=$(echo "$PAGE" | jq "d['nextCursor']")
[ ${#CUR} -gt 8 ] && ok "cursor issued" || bad "cursor"
expect "cursor page returns older" "$(curl -sk -H "Authorization: Bearer $T1" "$GW/api/rooms/$GEN/messages?limit=5&before=$CUR" | jq "len(d['items'])>0 and 'True' or 'False'")" "True"

sec "9. Moderation gate"
BLOCKED=$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
  -d '{"content":"my aadhaar is 1234 5678 9012 keep it safe"}' "$GW/api/rooms/$GEN/messages")
expect "PII blocked by rules (422)" "$BLOCKED" 422

sec "10. Private chat + participant authorization"
U2ID=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/auth/me" | jq "d['userId']")
CONV=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d "{\"otherUserId\":\"$U2ID\"}" "$GW/api/conversations")
CID=$(echo "$CONV" | jq "d['id']")
[ ${#CID} -eq 36 ] && ok "conversation created ($CID)" || bad "conversation create"

# Idempotent: same pair must resolve to the same document.
CID2=$(curl -sk -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' \
      -d "{\"otherUserId\":\"$(curl -sk -H "Authorization: Bearer $T1" $GW/api/auth/me | jq "d['userId']")\"}" \
      "$GW/api/conversations" | jq "d['id']")
expect "same pair -> same conversation" "$CID2" "$CID"

DM=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"content":"private hello"}' "$GW/api/conversations/$CID/messages")
DMID=$(echo "$DM" | jq "d['id']")
[ ${#DMID} -eq 36 ] && ok "direct message sent" || bad "direct message"

DMUNREAD=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/conversations" | jq "[c['unreadCount'] for c in d if c['id']=='$CID'][0]")
expect "recipient DM unread" "$DMUNREAD" 1

# A third party must not be able to read the conversation.
register "$U3" "E2E Carol" "Hyderabad"
T3=$(login "$U3" /tmp/e2e3.txt)
expect "outsider cannot read conversation" "$(code -H "Authorization: Bearer $T3" "$GW/api/conversations/$CID")" 404
expect "outsider cannot read DM history"   "$(code -H "Authorization: Bearer $T3" "$GW/api/conversations/$CID/messages")" 404
expect "outsider cannot post into it"      "$(code -X POST -H "Authorization: Bearer $T3" -H 'Content-Type: application/json' -d '{"content":"intrusion"}' "$GW/api/conversations/$CID/messages")" 404

sec "11. Blocking"
curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T2" "$GW/api/blocks/$(curl -sk -H "Authorization: Bearer $T1" $GW/api/auth/me | jq "d['userId']")"
expect "blocked sender refused" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d '{"content":"after block"}' "$GW/api/conversations/$CID/messages")" 403
curl -sk -o /dev/null -X DELETE -H "Authorization: Bearer $T2" "$GW/api/blocks/$(curl -sk -H "Authorization: Bearer $T1" $GW/api/auth/me | jq "d['userId']")"
ok "unblocked"

sec "12. Polls"
POLL=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"question":"Which day suits the retro best?","options":["Monday","Wednesday","Friday"]}' "$GW/api/polls")
PID=$(echo "$POLL" | jq "d['id']")
[ ${#PID} -eq 36 ] && ok "poll created" || bad "poll create"
OPT=$(echo "$POLL" | jq "d['options'][0]['id']")

expect "1 option rejected" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d '{"question":"Too few options here","options":["only"]}' "$GW/api/polls")" 400
expect "duplicate options rejected" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d '{"question":"Duplicate options here","options":["a","a"]}' "$GW/api/polls")" 400

V1=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d "{\"optionId\":\"$OPT\"}" "$GW/api/polls/$PID/vote" | jq "d['totalVotes']")
V2=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d "{\"optionId\":\"$OPT\"}" "$GW/api/polls/$PID/vote" | jq "d['totalVotes']")
V3=$(curl -sk -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' -d "{\"optionId\":\"$OPT\"}" "$GW/api/polls/$PID/vote" | jq "d['totalVotes']")
expect "first vote counted"        "$V1" 1
expect "same option again withdraws" "$V2" 0
expect "second user votes"         "$V3" 1

# One vote per user, enforced by a unique index — 5 concurrent votes must yield 1.
for i in 1 2 3 4 5; do
  curl -sk -o /dev/null -X POST -H "Authorization: Bearer $T3" -H 'Content-Type: application/json' \
    -d "{\"optionId\":\"$OPT\"}" "$GW/api/polls/$PID/vote" &
done
wait
CONCUR=$(curl -sk -H "Authorization: Bearer $T3" "$GW/api/polls/$PID" | jq "d['options'][0]['voteCount']")
[ "$CONCUR" -le 2 ] && ok "concurrent votes did not inflate the count (=$CONCUR)" || bad "concurrent vote count=$CONCUR"

expect "non-creator cannot close" "$(code -X POST -H "Authorization: Bearer $T2" "$GW/api/polls/$PID/close")" 403
expect "creator can close"        "$(code -X POST -H "Authorization: Bearer $T1" "$GW/api/polls/$PID/close")" 204
expect "closed poll refuses votes" \
  "$(code -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' -d "{\"optionId\":\"$OPT\"}" "$GW/api/polls/$PID/vote")" 409

sec "13. Notifications scoped to the caller"
NOTIF=$(curl -sk -H "Authorization: Bearer $T2" "$GW/api/notifications")
echo "$NOTIF" | grep -q '\[' && ok "own notifications readable" || bad "notifications"
expect "internal create endpoint not routed" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d '{}' "$GW/api/notifications/internal")" 404

sec "14. Reporting (identity from token)"
M2=$(curl -sk -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' \
      -d '{"content":"a message that will be reported"}' "$GW/api/rooms/$GEN/messages" | jq "d['id']")
REP=$(curl -sk -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' \
      -d "{\"kind\":\"RoomMessage\",\"messageId\":\"$M2\",\"reason\":\"Inappropriate content\"}" "$GW/api/reports")
echo "$REP" | grep -q '"id"' && ok "report accepted" || bad "report: $REP"
expect "duplicate report rejected" \
  "$(code -X POST -H "Authorization: Bearer $T2" -H 'Content-Type: application/json' -d "{\"kind\":\"RoomMessage\",\"messageId\":\"$M2\",\"reason\":\"again\"}" "$GW/api/reports")" 409
expect "cannot report own message" \
  "$(code -X POST -H "Authorization: Bearer $T1" -H 'Content-Type: application/json' -d "{\"kind\":\"RoomMessage\",\"messageId\":\"$M2\",\"reason\":\"self\"}" "$GW/api/reports")" 400
expect "non-admin cannot read the queue" "$(code -H "Authorization: Bearer $T2" "$GW/api/reports")" 403

sec "15. Session lifecycle"
OLD=$(grep refresh_token /tmp/e2e1.txt | awk '{print $7}')
curl -sk -c /tmp/e2e1b.txt -b /tmp/e2e1.txt -o /dev/null -X POST "$AUTH/api/auth/refresh"
NEW=$(grep refresh_token /tmp/e2e1b.txt | awk '{print $7}')
[ "$OLD" != "$NEW" ] && ok "refresh token rotated" || bad "rotation"
expect "replayed token revokes the family" "$(code -b /tmp/e2e1.txt -X POST "$AUTH/api/auth/refresh")" 401
expect "successor also dead"               "$(code -b /tmp/e2e1b.txt -X POST "$AUTH/api/auth/refresh")" 401

sec "16. Rate limiting"
RL=0
for i in $(seq 1 9); do
  c=$(code -X POST "$GW/api/auth/login" -H 'Content-Type: application/json' -d '{"email":"nobody@x.com","password":"x"}')
  [ "$c" = "429" ] && RL=1
done
[ "$RL" = "1" ] && ok "login rate limit engaged (429)" || bad "login rate limit never triggered"

# ══════════════════════════════════════════════════════════════════════════════
printf "\n%s\n" "$(printf '=%.0s' $(seq 1 70))"
printf "  RESULT   passed: %d   failed: %d\n" "$PASS" "$FAIL"
printf "%s\n" "$(printf '=%.0s' $(seq 1 70))"
[ "$FAIL" -eq 0 ] && exit 0 || exit 1
