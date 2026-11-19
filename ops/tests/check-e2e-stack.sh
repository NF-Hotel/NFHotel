#!/usr/bin/env bash
# Check the throwaway stack of scripts/e2e-stack.sh (MIL-027 task 1 of NFHotel) from a
# client on its Docker network: the public pages answer through nginx over TLS, the back
# office sends a visitor to the login, port 80 redirects to HTTPS, no container of the
# stack publishes a host port, every container has the hardening of MIL-023 task 2, the API
# and PostgreSQL are not on the proxy network, nginx is not on the python network while the
# web application and the API are (ADR-0039), the three test accounts exist, stack.env
# holds what the API scan needs: the internal network of the API, the API token and the
# account of the scan (MIL-077), and a guest who signs in with the booking reference and the
# email reaches the own booking and nothing else (MIL-018, ADR-0047, ADR-0049).
#
# Usage: check-e2e-stack.sh <stack.env> [<name>]
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, docker

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/tests/lib/hardening.sh
. "$ROOT/tests/lib/hardening.sh"

[[ $# -ge 1 && -f "$1" ]] || {
  echo "usage: check-e2e-stack.sh <stack.env> [<name>]" >&2
  exit 2
}
stack_env="$1"
name="${2:-nfhotel-e2e}"
network="$(sed -n 's/^E2E_NETWORK=//p' "$stack_env")"
readonly CURL="curlimages/curl:8.10.1"
failures=0

check() {
  if [[ "$3" == "$2" ]]; then
    echo "PASS  $1"
  else
    echo "FAIL  $1 (expected '$2', got '$3')"
    failures=$((failures + 1))
  fi
}
# get <format> <url>: one curl call from a container on the stack's network.
get() {
  MSYS_NO_PATHCONV=1 docker run --rm --network "$network" "$CURL" -sk -o /dev/null -w "$1" "$2" || true
}

check "the home page answers over TLS" 200 "$(get '%{http_code}' https://nginx/)"
check "the booking page answers over TLS" 200 "$(get '%{http_code}' https://nginx/book)"
check "the login page answers over TLS" 200 "$(get '%{http_code}' https://nginx/admin/login)"
check "the back office sends a visitor to the login" "https://nginx/admin/login?returnUrl=%2Fadmin" \
  "$(get '%{redirect_url}' https://nginx/admin)"
check "port 80 redirects to HTTPS" 301 "$(get '%{http_code}' http://nginx/)"

ports=""
for id in $(docker ps --quiet --filter "label=com.docker.compose.project" | xargs -r docker inspect \
  --format '{{index .Config.Labels "com.docker.compose.project"}} {{.Id}}' | awk -v p="$name-" 'index($1, p) == 1 {print $2}'); do
  ports+="$(docker port "$id" 2>/dev/null)"
  check "$(docker inspect --format '{{.Name}}' "$id") has the hardening" "" "$(hardening_missing "$id")"
done
check "no container of the stack publishes a host port" "" "$ports"
on_proxy="$(docker network inspect "$network" --format '{{range .Containers}}{{.Name}} {{end}}')"
check "the API is not on the proxy network" "" "$(tr ' ' '\n' <<<"$on_proxy" | grep -E 'nfhotel-api|postgres' || true)"
on_python="$(docker network inspect "$name-python" --format '{{range .Containers}}{{.Name}} {{end}}' 2>/dev/null)"
check "nginx is not on the python network" "" "$(tr ' ' '\n' <<<"$on_python" | grep nginx || true)"
check "the web application and the API are on the python network" "nfhotel-api nfhotel-web" \
  "$(tr ' ' '\n' <<<"$on_python" | grep -oE 'nfhotel-(api|web)' | sort -u | tr '\n' ' ' | sed 's/ $//')"
for role in MANAGER RECEPTIONIST HOUSEKEEPING; do
  check "stack.env holds the $role account" yes \
    "$(grep -qE "^E2E_${role}_USER=.+" "$stack_env" && grep -qE "^E2E_${role}_PASSWORD=.{32}$" "$stack_env" && echo yes)"
done
# The API scan of the DAST run (MIL-077) joins the internal network of the API with the API
# token and an account of its own.
check "stack.env names the internal network of the API" true \
  "$(docker network inspect "$(sed -n 's/^E2E_API_NETWORK=//p' "$stack_env")" --format '{{.Internal}}' 2>/dev/null || true)"
check "stack.env holds the API token and the account of the API scan" yes \
  "$(grep -qE '^E2E_API_TOKEN=[0-9a-f]{48}$' "$stack_env" && grep -qE '^E2E_API_USER=.+' "$stack_env" &&
    grep -qE '^E2E_API_PASSWORD=.{32}$' "$stack_env" && echo yes)"

# The guest sign-in (MIL-018, ADR-0049): from a client on the internal network of the API,
# with the API token and the account of the API scan, a Suite of its own with a random number
# takes two public bookings in 2031, so neither meets a booking of the end-to-end tests. Each
# answer holds a random reference and no key. The guest of the first booking signs in with its
# reference in upper case and the email in another letter case, and reads the own booking on
# the guest route, which names no booking: the answer is the receipt of that booking, with no
# key, guest or email. The guest token is refused on the routes of staff; the guest route
# without a token, a wrong email and an old booking number get 401, and the old lookup of a
# receipt is gone. The secrets reach the container in its environment (--env-file), and its
# curl reads them from files, so none is on a command line.
guest_script="$(
  cat <<'SH'
# sh of the curl image, without -e: every step runs, and one that fails leaves its value empty for the checks.
set -u
api=http://nfhotel-api:8080
umask 077
dir="$(mktemp -d)"
printf 'Authorization: Bearer %s\n' "$E2E_API_TOKEN" >"$dir/auth"
printf '{"userName":"%s","password":"%s"}' "$E2E_API_USER" "$E2E_API_PASSWORD" >"$dir/login"
token_of() { sed -n 's/.*"token":"\([^"]*\)".*/\1/p'; }
reference_of() { sed -n 's/.*"reference":"\([^"]*\)".*/\1/p'; }
staff="$(curl -s -H @"$dir/auth" -H 'Content-Type: application/json' --data @"$dir/login" "$api/api/auth/login" | token_of)"
printf 'X-Staff-Token: %s\n' "$staff" >"$dir/staff"
echo "room=$(curl -s -o /dev/null -w '%{http_code}' -H @"$dir/auth" -H @"$dir/staff" -H 'Content-Type: application/json' \
  -d "{\"roomNumber\":\"$GUEST_ROOM\",\"floor\":9,\"size\":2,\"capacity\":2}" "$api/api/rooms")"
book() {
  curl -s -H @"$dir/auth" -H 'Content-Type: application/json' \
    -d "{\"checkInDate\":\"$1\",\"checkOutDate\":\"$2\",\"roomSize\":2,\"guests\":1,\"newGuest\":{\"firstName\":\"Smoke\",\"lastName\":\"Guest\",\"email\":\"$3\",\"phoneNumber\":\"+4512345678\",\"country\":\"Denmark\",\"passportNumber\":null}}" \
    "$api/api/bookings/public" >"$dir/booked-$4"
  reference_of <"$dir/booked-$4"
}
sign_in() {
  curl -s -w '\n%{http_code}' -H @"$dir/auth" -H 'Content-Type: application/json' \
    -d "{\"reference\":\"$1\",\"email\":\"$2\"}" "$api/api/auth/guest-login"
}
status() { curl -s -o /dev/null -w '%{http_code}' -H @"$dir/auth" "$@"; }
a="$(book 2031-03-03 2031-03-05 "a-$GUEST_ROOM@example.com" a)"
b="$(book 2031-03-10 2031-03-12 "b-$GUEST_ROOM@example.com" b)"
echo "keys=$(cat "$dir/booked-a" "$dir/booked-b" | grep -cE '"(bookingId|guestId|email)"')"
echo "references=$(printf '%s\n%s\n' "$a" "$b" | grep -cE '^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$')"
answer="$(sign_in "$(printf '%s' "$a" | tr '[:lower:]' '[:upper:]')" "A-$GUEST_ROOM@Example.com")"
echo "signin=$(printf '%s\n' "$answer" | tail -n 1)"
printf 'X-Guest-Token: %s\n' "$(printf '%s\n' "$answer" | token_of)" >"$dir/guest"
curl -s -H @"$dir/auth" -H @"$dir/guest" "$api/api/guest/booking" >"$dir/own"
echo "own=$([ -n "$a" ] && [ "$(reference_of <"$dir/own")" = "$a" ] && echo yes)"
echo "own_names=$(grep -cE '"(bookingId|guestId|email|firstName|lastName)"' "$dir/own")"
echo "staff_read=$(status -H @"$dir/guest" "$api/api/bookings/1")"
echo "staff_cancel=$(status -X POST -H @"$dir/guest" "$api/api/bookings/1/cancel")"
echo "staff_endpoint=$(status -H @"$dir/guest" "$api/api/rooms")"
echo "no_token=$(status "$api/api/guest/booking")"
echo "wrong_email=$(sign_in "$a" "nobody-$GUEST_ROOM@example.com" | tail -n 1)"
echo "old_number=$(sign_in FLZ-000001 "a-$GUEST_ROOM@example.com" | tail -n 1)"
echo "old_lookup=$(status -X POST -H 'Content-Type: application/json' -d '{"bookingId":1,"email":"x@example.com"}' "$api/api/bookings/public/receipt")"
SH
)"
api_network="$(sed -n 's/^E2E_API_NETWORK=//p' "$stack_env")"
# A failed run is not fatal here (|| true): its values stay empty, and each check below reports it.
guest="$(MSYS_NO_PATHCONV=1 docker run --rm --network "$api_network" --env-file "$stack_env" \
  --env GUEST_ROOM="G$RANDOM" "$CURL" sh -c "$guest_script" || true)"
guest_value() { sed -n "s/^$1=//p" <<<"$guest"; }
check "the guest smoke test has a Suite of its own" 200 "$(guest_value room)"
check "the public booking answers two random references" 2 "$(guest_value references)"
check "the public booking answers no key, guest or email" 0 "$(guest_value keys)"
check "a guest signs in with the reference in upper case and the email in another letter case" 200 "$(guest_value signin)"
check "the guest reads the own booking on the guest route" yes "$(guest_value own)"
check "the guest's answer names no key, guest or email" 0 "$(guest_value own_names)"
check "the guest is refused the read of a booking on a staff route" 403 "$(guest_value staff_read)"
check "the guest is refused the cancellation on a staff route" 403 "$(guest_value staff_cancel)"
check "the guest is refused a staff endpoint" 403 "$(guest_value staff_endpoint)"
check "the guest route without a guest token gets 401" 401 "$(guest_value no_token)"
check "a wrong email gets 401" 401 "$(guest_value wrong_email)"
check "an old booking number with its email gets 401" 401 "$(guest_value old_number)"
check "the old lookup of a receipt is gone" 404 "$(guest_value old_lookup)"

((failures == 0)) || exit 1
echo "==> the end-to-end stack is as expected"
