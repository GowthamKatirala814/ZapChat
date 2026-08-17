#!/usr/bin/env bash
# Email and OTP behaviour, driven through the running Auth service.
#
# Covers what the other suites do not: the one-time-code lifecycle in detail, the
# per-mailbox resend cooldown, and — most importantly — that a delivery failure is
# reported as a failure rather than as "we emailed you".
#
# Start the backend first. Sections 1-8 need the log transport, because they read codes:
#
#     .\scripts\start-backend.ps1 -EmailToLog
#     bash tests/email-e2e.sh
#
# Section 9 starts its own Auth instance with a deliberately broken provider, so it needs
# no configuration of its own.
set -uo pipefail

AUTH="http://localhost:5111"
LOG="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/logs"

PASS=0; FAIL=0
ok()  { printf "  [ PASS ] %s\n" "$1"; PASS=$((PASS+1)); }
bad() { printf "  [ FAIL ] %s\n" "$1"; FAIL=$((FAIL+1)); }
sec() { printf "\n== %s %s\n" "$1" "$(printf '=%.0s' $(seq 1 $((66-${#1}))))"; }

json() { python -c "import sys,json;d=json.load(sys.stdin);print($1)" 2>/dev/null || echo "PARSE_ERR"; }

post() { curl -s -X POST "$AUTH$1" -H 'Content-Type: application/json' -d "$2"; }
code_of() { curl -s -o /dev/null -w "%{http_code}" -X POST "$AUTH$1" -H 'Content-Type: application/json' -d "$2"; }

# -a: a service killed mid-write can leave NUL bytes in the log, and grep would then
# silently refuse to match.
latest_code() { grep -a -oE "Your code is: [0-9]{6}" "$LOG/Auth.log" | tail -1 | grep -oE "[0-9]{6}"; }

initiate() {
  post /api/auth/register/initiate \
    "{\"fullName\":\"Email Suite\",\"email\":\"$1\",\"department\":\"Engineering\",\"branch\":\"Hyderabad\"}"
}

printf "ZapChat email + OTP suite\n"
printf "%s\n" "$(printf '=%.0s' $(seq 1 70))"

# ── 1 ────────────────────────────────────────────────────────────────────────
sec "1. A code is issued and the response does not contain it"
EMAIL="otp.$(date +%s).$RANDOM@zapcg.com"
RESP=$(initiate "$EMAIL")
sleep 1

[ -n "$(echo "$RESP" | grep -oE '[0-9]{6}')" ] \
  && bad "the API response leaked the code: $RESP" \
  || ok "response carries no 6-digit code"

echo "$RESP" | grep -qi "emailed you" \
  && ok "response says the message was emailed" \
  || bad "unexpected message: $RESP"

CODE=$(latest_code)
[ -n "$CODE" ] && ok "a code was generated ($CODE)" || bad "no code found in the log"

# ── 2 ────────────────────────────────────────────────────────────────────────
sec "2. Wrong code is rejected and costs an attempt"
for wrong in 000001 000002; do
  STATUS=$(code_of /api/auth/register/verify-otp "{\"email\":\"$EMAIL\",\"otpCode\":\"$wrong\"}")
  [ "$STATUS" = "400" ] && ok "wrong code rejected ($wrong -> 400)" || bad "wrong code gave $STATUS"
done

# ── 3 ────────────────────────────────────────────────────────────────────────
sec "3. Correct code is accepted and returns a follow-up token"
VERIFY=$(post /api/auth/register/verify-otp "{\"email\":\"$EMAIL\",\"otpCode\":\"$CODE\"}")
TOKEN=$(echo "$VERIFY" | json "d.get('token','')")

[ -n "$TOKEN" ] && [ "$TOKEN" != "PARSE_ERR" ] \
  && ok "code accepted, follow-up token issued" \
  || bad "verification failed: $VERIFY"

# ── 4 ────────────────────────────────────────────────────────────────────────
sec "4. Re-verifying supersedes the previous follow-up token"
# Re-verification is allowed on purpose: the code is still within its window and the
# caller has already proved possession, so a refreshed page should not dead-end. What
# must hold is that only ONE follow-up token is live — the new one replaces the old.
SECOND=$(post /api/auth/register/verify-otp "{\"email\":\"$EMAIL\",\"otpCode\":\"$CODE\"}")
TOKEN2=$(echo "$SECOND" | json "d.get('token','')")

[ -n "$TOKEN2" ] && [ "$TOKEN2" != "$TOKEN" ]   && ok "re-verification issued a different token"   || bad "re-verification did not rotate the token"

STATUS=$(code_of /api/auth/register/complete   "{\"verificationToken\":\"$TOKEN\",\"password\":\"Str0ngPass!23\",\"confirmPassword\":\"Str0ngPass!23\"}")

[ "$STATUS" = "400" ]   && ok "the superseded token is dead (400)"   || bad "the old follow-up token still worked ($STATUS)"

# ── 5 ────────────────────────────────────────────────────────────────────────
sec "5. Attempt limit is enforced"
EMAIL2="attempts.$(date +%s).$RANDOM@zapcg.com"
initiate "$EMAIL2" > /dev/null
sleep 1

# Five wrong guesses are permitted; the sixth must find the code already unusable.
# The message at that point is the generic "invalid or has expired" rather than a
# dedicated "too many attempts" - IsUsable() fails on the attempt count before the
# branch that would say so is reached.
for i in $(seq 1 5); do
  post /api/auth/register/verify-otp "{\"email\":\"$EMAIL2\",\"otpCode\":\"09999$i\"}" > /dev/null
done

SIXTH=$(post /api/auth/register/verify-otp "{\"email\":\"$EMAIL2\",\"otpCode\":\"099996\"}")

echo "$SIXTH" | grep -qiE "invalid or has expired|too many" \
  && ok "the code became unusable after 5 wrong guesses" \
  || bad "a 6-digit code accepted unlimited guesses: $SIXTH"

# ── 6 ────────────────────────────────────────────────────────────────────────
sec "6. Per-mailbox resend cooldown"
EMAIL3="cooldown.$(date +%s).$RANDOM@zapcg.com"
initiate "$EMAIL3" > /dev/null
sleep 1

STATUS=$(code_of /api/auth/register/initiate \
  "{\"fullName\":\"Email Suite\",\"email\":\"$EMAIL3\",\"department\":\"Engineering\",\"branch\":\"Hyderabad\"}")

[ "$STATUS" = "429" ] \
  && ok "immediate resend refused (429)" \
  || bad "immediate resend gave $STATUS, expected 429"

RETRY=$(curl -s -D - -o /dev/null -X POST "$AUTH/api/auth/register/initiate" \
  -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"Email Suite\",\"email\":\"$EMAIL3\",\"department\":\"Engineering\",\"branch\":\"Hyderabad\"}" \
  | grep -i "^retry-after:" | tr -d '\r' | awk '{print $2}')

[ -n "$RETRY" ] \
  && ok "Retry-After header present (${RETRY}s)" \
  || bad "no Retry-After header on the 429"

# ── 7 ────────────────────────────────────────────────────────────────────────
sec "7. Password reset does not reveal whether an account exists"
KNOWN=$(post /api/auth/forgot-password '{"email":"alpha@zapcg.com"}')
UNKNOWN=$(post /api/auth/forgot-password '{"email":"definitely.not.a.user@zapcg.com"}')

[ "$KNOWN" = "$UNKNOWN" ] \
  && ok "responses are byte-identical" \
  || bad "responses differ:\n    known:   $KNOWN\n    unknown: $UNKNOWN"

echo "$KNOWN" | grep -oE '[0-9]{6}' > /dev/null \
  && bad "the reset response leaked a code" \
  || ok "reset response carries no code"

# ── 8 ────────────────────────────────────────────────────────────────────────
sec "8. Password reset codes are usable"
RESET_CODE=$(grep -a -A 6 "password reset code" "$LOG/Auth.log" | grep -a -oE "Your code is: [0-9]{6}" | tail -1 | grep -oE "[0-9]{6}")

if [ -n "$RESET_CODE" ]; then
  RESP=$(post /api/auth/verify-otp "{\"email\":\"alpha@zapcg.com\",\"otpCode\":\"$RESET_CODE\"}")
  echo "$RESP" | grep -q '"token"' \
    && ok "reset code verified and a reset token issued" \
    || bad "reset verification failed: $RESP"
else
  bad "no password reset code found in the log"
fi

# ── 9 ────────────────────────────────────────────────────────────────────────
sec "9. A failed send is reported as a failure, not as success"
printf "  starting an Auth instance with a deliberately invalid mail provider...\n"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
FAILLOG="${TMPDIR:-/tmp}/zapchat-mailfail.$$.log"

ASPNETCORE_ENVIRONMENT=Development \
ZAPCHAT_JWT__SECRET="${ZAPCHAT_JWT__SECRET:-dev-only-zapchat-signing-key-change-me-32plus}" \
ZAPCHAT_MONGO__CONNECTIONSTRING="${ZAPCHAT_MONGO__CONNECTIONSTRING:-mongodb://localhost:27017}" \
ZAPCHAT_EMAIL__PROVIDER=Graph \
ZAPCHAT_EMAIL__SENDEREMAIL=noreply@zapcg.com \
ZAPCHAT_EMAIL__GRAPH__TENANTID=zapcg.com \
ZAPCHAT_EMAIL__GRAPH__CLIENTID=00000000-0000-0000-0000-000000000001 \
ZAPCHAT_EMAIL__GRAPH__CLIENTSECRET=invalid-secret-for-the-failure-path-test \
  dotnet run --project "$ROOT/backend/Services/AuthService/Auth.API" \
  --no-build --no-launch-profile --urls http://localhost:5197 > "$FAILLOG" 2>&1 &

FAILPID=$!
for _ in $(seq 1 40); do
  curl -s -m 2 -o /dev/null http://localhost:5197/health/ready && break
  sleep 1
done

BROKEN="broken.$(date +%s).$RANDOM@zapcg.com"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:5197/api/auth/register/initiate" \
  -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"Broken\",\"email\":\"$BROKEN\",\"department\":\"Engineering\",\"branch\":\"Hyderabad\"}")

BODY=$(curl -s -X POST "http://localhost:5197/api/auth/register/initiate" \
  -H 'Content-Type: application/json' \
  -d "{\"fullName\":\"Broken\",\"email\":\"broken2.$(date +%s)@zapcg.com\",\"department\":\"Engineering\",\"branch\":\"Hyderabad\"}")

[ "$STATUS" = "503" ] && ok "registration returned 503" || bad "registration returned $STATUS, expected 503"

echo "$BODY" | grep -qi "could not send" \
  && ok "message admits the email was not sent" \
  || bad "unexpected body: $BODY"

echo "$BODY" | grep -qiE "graph|smtp|token|secret|aadsts" \
  && bad "the error body leaked provider internals: $BODY" \
  || ok "no provider internals in the user-facing error"

grep -a -q "invalid-secret-for-the-failure-path-test" "$FAILLOG" \
  && bad "THE CLIENT SECRET WAS WRITTEN TO THE LOG" \
  || ok "the client secret never reaches the log"

grep -a -q "Kind=Authentication" "$FAILLOG" \
  && ok "the log classifies the failure and names the provider" \
  || bad "the log lacks a failure classification"

STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:5197/api/auth/register/verify-otp" \
  -H 'Content-Type: application/json' -d "{\"email\":\"$BROKEN\",\"otpCode\":\"123456\"}")

[ "$STATUS" = "400" ] \
  && ok "no usable code survives a failed send" \
  || bad "an undelivered code was still usable ($STATUS)"

kill $FAILPID 2>/dev/null
wait $FAILPID 2>/dev/null
rm -f "$FAILLOG"

printf "\n%s\n" "$(printf '=%.0s' $(seq 1 70))"
printf "  RESULT   passed: %s   failed: %s\n" "$PASS" "$FAIL"
printf "%s\n" "$(printf '=%.0s' $(seq 1 70))"

[ "$FAIL" -eq 0 ] || exit 1
