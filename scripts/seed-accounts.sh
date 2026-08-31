#!/usr/bin/env bash
# Creates the local development accounts through the REAL registration API.
#
# Deliberately not hand-inserted into MongoDB: going through the API means the records
# carry a properly BCrypt-hashed password, a server-allocated anonymous name, the same
# document shape the application writes, and the normal indexes and validation. A
# hand-crafted user document drifts from the real shape the moment the model changes.
#
# Talks to the Auth service directly on :5111, not the gateway. Registration is limited
# to five per minute per IP at the gateway, which is correct and would throttle this.
#
# Requires the backend started with -EmailToLog, since it reads codes from the log.
set -uo pipefail

AUTH="http://localhost:5111"
LOG="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/logs"
PASSWORD="${ZAPCHAT_SEED_PASSWORD:-Str0ngPass!23}"

# -a: a service killed mid-write can leave NUL bytes in the log, which makes grep treat
# it as binary and match nothing.
code_for() { grep -a -oE "Your code is: [0-9]{6}" "$LOG/Auth.log" | tail -1 | grep -oE "[0-9]{6}"; }
field() { python3 -c "import sys,json;print(json.load(sys.stdin).get('$1',''))" 2>/dev/null; }

register() { # register <email> <full name> <department> <branch>
  local email="$1" name="$2" dept="$3" branch="$4"

  printf "  %-26s " "$email"

  curl -s -o /dev/null -X POST "$AUTH/api/auth/register/initiate" \
    -H 'Content-Type: application/json' \
    -d "{\"fullName\":\"$name\",\"email\":\"$email\",\"department\":\"$dept\",\"branch\":\"$branch\"}"

  sleep 1.5
  local otp; otp=$(code_for)

  if [ -z "$otp" ]; then printf "FAILED (no code in the log)\n"; return 1; fi

  local token
  token=$(curl -s -X POST "$AUTH/api/auth/register/verify-otp" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$email\",\"otpCode\":\"$otp\"}" | field token)

  if [ -z "$token" ]; then printf "FAILED (code not accepted)\n"; return 1; fi

  local result
  result=$(curl -s -X POST "$AUTH/api/auth/register/complete" -H 'Content-Type: application/json' \
    -d "{\"verificationToken\":\"$token\",\"password\":\"$PASSWORD\",\"confirmPassword\":\"$PASSWORD\"}" | field success)

  if [ "$result" != "True" ] && [ "$result" != "true" ]; then printf "FAILED (completion refused)\n"; return 1; fi

  # Sign in once. This is what applies the Admin role when the address matches
  # AdminSettings:AdminEmail, and it proves the credentials actually work.
  local anon
  anon=$(curl -s -X POST "$AUTH/api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$email\",\"password\":\"$PASSWORD\"}" | field anonymousName)

  printf "created, signs in as %s\n" "${anon:-?}"
}

printf "Seeding development accounts (password: %s)\n" "$PASSWORD"
printf "%s\n" "$(printf '=%.0s' $(seq 1 62))"

register "alpha@zapcg.com" "Alpha Tester"  "Engineering" "Hyderabad"
register "bravo@zapcg.com" "Bravo Tester"  "Product"     "Bangalore"
register "carol@zapcg.com" "Carol Tester"  "Design"      "Hyderabad"

printf "\n"
printf "  alpha is an administrator if ZAPCHAT_ADMINSETTINGS__ADMINEMAIL=alpha@zapcg.com\n"
printf "  was set before its first sign-in.\n"
