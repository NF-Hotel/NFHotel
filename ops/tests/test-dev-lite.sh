#!/usr/bin/env bash
# Test the light development mode (scripts/dev-lite.sh, ADR-0035, MIL-056 task 5) as a developer runs it: from a
# clean start it builds the working tree, the back office and the two routes of the mobile app answer on the
# loopback of this machine, the other API paths do not, the stack keeps its database over --down, and --destroy
# removes everything. It uses its own name, folder and port, so a developer's own light stack is never touched.
#
# Usage: test-dev-lite.sh
# Exit:  0 every check passed; 1 a check failed or the stack did not start
# Needs: bash 4+, docker with the compose plugin 2.24.4 or later, openssl, curl, git

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
W="$(mktemp -d)"
export DEV_LITE_NAME=nfhotel-devtest DEV_LITE_DIR="$W/lite" DEV_SITE_PORT=18443
readonly URL="https://localhost:$DEV_SITE_PORT"
lite() { bash "$REPO/scripts/dev-lite.sh" "$@"; }
trap 'lite --destroy >/dev/null 2>&1; rm -rf "$W"' EXIT
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}
# code <curl arguments...>: the HTTP status of one request to the stack.
code() { curl -sk -o /dev/null -w '%{http_code}' "$@" || true; }

lite --seed >"$W/up.log" 2>&1 || {
  tail -30 "$W/up.log" | grep -v 'password' >&2
  echo "FAIL  the light stack did not start"
  exit 1
}
ok "dev-lite.sh --seed starts the stack from a clean start"

[[ "$(code "$URL/")" == 200 ]] && ok "the site answers 200 over TLS" || ko "the site: $(code "$URL/")"
[[ "$(code "$URL/admin/login")" == 200 ]] && ok "the back office sign-in answers 200" || ko "admin/login: $(code "$URL/admin/login")"

# The password goes to curl in a file the owner only reads, never on its command line.
manager_password="$(sed -n 's/^manager=//p' "$DEV_LITE_DIR/dev-staff.env")"
(
  umask 077
  printf '{"userName":"manager","password":"%s"}' "$manager_password" >"$W/login.json"
  printf '{"userName":"manager","password":"wrong-%s"}' "$manager_password" >"$W/wrong.json"
)
login() { curl -sk -H 'Content-Type: application/json' --data @"$1" "$URL/mobile-api/api/auth/login" || true; }
token="$(login "$W/login.json" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')"
[[ -n "$token" ]] && ok "the mobile app signs in through /mobile-api/ as the Manager" || ko "no token from the mobile sign-in"
[[ "$(code -H 'Content-Type: application/json' --data @"$W/wrong.json" "$URL/mobile-api/api/auth/login")" == 401 ]] &&
  ok "a wrong password is refused with 401" || ko "a wrong password was not refused with 401"
printf 'header = "X-Staff-Token: %s"\n' "$token" >"$W/token.cfg"
rooms="$(curl -sk --config "$W/token.cfg" "$URL/mobile-api/api/rooms" || true)"
[[ "$rooms" == '{"value":[{'* ]] && ok "the rooms of the seed come back through /mobile-api/api/rooms" || ko "rooms: ${rooms:0:80}"
notes="$(curl -sk --config "$W/token.cfg" "$URL/mobile-api/api/room-service-notes" || true)"
[[ "$notes" == '{"value":['* ]] && ok "the service notes come back through /mobile-api/api/room-service-notes" || ko "notes: ${notes:0:80}"
staff="$(curl -sk --config "$W/token.cfg" "$URL/mobile-api/api/staff" || true)"
[[ "$staff" == '{"value":[{'* ]] && ok "the Manager lists the staff accounts through /mobile-api/api/staff" || ko "staff: ${staff:0:80}"
[[ "$(code "$URL/mobile-api/api/bookings")" == 404 ]] && ok "another API path under /mobile-api/ answers 404" ||
  ko "/mobile-api/api/bookings: $(code "$URL/mobile-api/api/bookings")"

for user in receptionist housekeeping; do
  grep -q "^created-$user$" "$DEV_LITE_DIR/dev-staff.env" && ok "the $user account was made" || ko "the $user account is missing"
done
if [[ "$(uname -s)" != MINGW* && "$(uname -s)" != MSYS* ]]; then
  [[ "$(find "$DEV_LITE_DIR" -maxdepth 0 -perm 700 | wc -l | tr -d ' ')" == 1 ]] && ok "the settings folder is the owner's only" ||
    ko "the settings folder is not 0700"
fi

ports="$(docker ps --filter "name=^$DEV_LITE_NAME-" --format '{{.Names}} {{.Ports}}')"
published="$(grep -c -- '->' <<<"$ports" || true)"
[[ "$published" == 1 && "$ports" == *"127.0.0.1:$DEV_SITE_PORT->443/tcp"* ]] &&
  ok "only nginx publishes a port, $DEV_SITE_PORT on the loopback" || ko "published ports: $ports"
privileged="$(docker ps -q --filter "name=^$DEV_LITE_NAME-" | xargs docker inspect -f '{{.HostConfig.Privileged}}' | sort -u)"
[[ "$privileged" == false ]] && ok "no container is privileged" || ko "privileged: $privileged"
grep -q 'password' "$W/up.log" || ko "the run did not print the development accounts"
grep -qi 'kvm' "$W/up.log" && ko "the light mode mentions KVM" || ok "the light mode needs no KVM"

lite --down >/dev/null 2>&1
[[ -z "$(docker ps -aq --filter "name=^$DEV_LITE_NAME-")" ]] && ok "--down removes the containers" || ko "--down left containers"
lite >"$W/again.log" 2>&1 && ok "a second run starts from the kept settings" || ko "the second run failed"
token="$(login "$W/login.json" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')"
printf 'header = "X-Staff-Token: %s"\n' "$token" >"$W/token.cfg"
rooms="$(curl -sk --config "$W/token.cfg" "$URL/mobile-api/api/rooms" || true)"
[[ "$rooms" == '{"value":[{'* ]] && ok "the database and the accounts survive --down" || ko "after --down: ${rooms:0:80}"

lite --destroy >/dev/null 2>&1
left="$(docker ps -aq --filter "name=^$DEV_LITE_NAME-")$(docker volume ls -q --filter "name=^$DEV_LITE_NAME-")"
[[ -z "$left" && ! -e "$DEV_LITE_DIR" ]] &&
  ok "--destroy removes the containers, the volume and the settings" || ko "--destroy left something behind"

echo "$pass passed, $fail failed"
((fail == 0))
