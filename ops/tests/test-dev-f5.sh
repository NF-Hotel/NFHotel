#!/usr/bin/env bash
# Test the F5 preparation (scripts/dev-f5.sh, ADR-0036, MIL-057 task 4) as Visual Studio uses it: after the script,
# the API and the web application start with `dotnet run` and their launch profiles, the API answers its health check
# and signs in the development Manager, and the back office signs in through the web form. A second run writes
# nothing, and --destroy removes the container, the volume and the user secrets it wrote. It uses its own container,
# port, state folder and user-secrets folder (APPDATA), so a developer's own F5 setup is never touched.
#
# Usage: test-dev-f5.sh
# Exit:  0 every check passed; 1 a check failed or a step did not start
# Needs: bash 4+, docker, dotnet (the SDK of global.json, with a development certificate), openssl, curl

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
W="$(mktemp -d)"
# The user secrets of .NET live under APPDATA when it is set, on every operating system.
export DEV_F5_NAME=nfhotel-f5test DEV_F5_DB_PORT=55433 DEV_F5_DIR="$W/state" APPDATA="$W/appdata"
# dotnet on Windows needs a Windows path; elsewhere cygpath does not exist.
command -v cygpath >/dev/null 2>&1 && APPDATA="$(cygpath -m "$APPDATA")"
readonly API=http://localhost:59103 WEB=https://localhost:7060
pids=()
f5() { bash "$REPO/scripts/dev-f5.sh" "$@"; }
# stop_apps: stop the two `dotnet run` and the application each started (the application is a child process).
stop_apps() {
  local pid
  for pid in ${pids[@]+"${pids[@]}"}; do
    pkill -P "$pid" >/dev/null 2>&1
    kill "$pid" >/dev/null 2>&1
  done
  pids=()
}
cleanup() {
  stop_apps
  f5 --destroy >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT
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
# secret_keys <project folder>: the names of the user secrets of a project.
secret_keys() {
  (cd "$REPO/$1" && dotnet user-secrets list 2>/dev/null) | sed -n 's/^\([^ ]*\) = .*/\1/p' | sort | tr '\n' ' '
}

mkdir -p "$APPDATA"
f5 >"$W/first.log" 2>&1 && ok "dev-f5.sh prepares a clean machine" || {
  grep -v 'Manager  ' "$W/first.log" >&2
  ko "dev-f5.sh failed"
  exit 1
}
grep -q 'api: 9 user secret(s) written, 0 kept' "$W/first.log" && ok "it writes the nine secrets of the API" || ko "api secrets: $(grep '==> api' "$W/first.log")"
grep -q 'web: 4 user secret(s) written, 0 kept' "$W/first.log" && ok "it writes the four secrets of the web application" || ko "web secrets: $(grep '==> web' "$W/first.log")"
[[ "$(docker port "$DEV_F5_NAME-postgres" 5432)" == "127.0.0.1:$DEV_F5_DB_PORT" ]] &&
  ok "PostgreSQL listens on 127.0.0.1:$DEV_F5_DB_PORT only" || ko "PostgreSQL port: $(docker port "$DEV_F5_NAME-postgres" 5432)"
docker inspect -f '{{range .Config.Env}}{{println .}}{{end}}' "$DEV_F5_NAME-postgres" | grep -q '^POSTGRES_PASSWORD=' &&
  ko "the database password is in the environment of the container" || ok "the database password is a file, not in the environment (ADR-0024)"
git -C "$REPO" grep -q "$(cat "$DEV_F5_DIR/postgres-password")" && ko "a secret is in a file of the repository" ||
  ok "no secret is in a file of the repository"

f5 >"$W/second.log" 2>&1
grep -q 'api: 0 user secret(s) written, 9 kept' "$W/second.log" && grep -q 'web: 0 user secret(s) written, 4 kept' "$W/second.log" &&
  ok "a second run writes nothing" || ko "second run: $(grep '==>' "$W/second.log")"

(cd "$REPO" && dotnet build src/NFHotel.Api -v q -nologo && dotnet build src/NFHotel.Web -v q -nologo) >"$W/build.log" 2>&1 || {
  tail -20 "$W/build.log"
  ko "the build failed"
  exit 1
}
(cd "$REPO" && exec dotnet run --project src/NFHotel.Api --launch-profile NFHotel.Api --no-build) >"$W/api.log" 2>&1 &
pids+=($!)
(cd "$REPO" && exec dotnet run --project src/NFHotel.Web --launch-profile https --no-build) >"$W/web.log" 2>&1 &
pids+=($!)
for _ in $(seq 1 60); do
  [[ "$(curl -s -o /dev/null -w '%{http_code}' "$API/health" || true)" == 200 &&
  "$(curl -sk -o /dev/null -w '%{http_code}' "$WEB/admin/login" || true)" == 200 ]] && break
  sleep 2
done
[[ "$(curl -s -o /dev/null -w '%{http_code}' "$API/health" || true)" == 200 ]] && ok "the API answers its health check with its launch profile" || {
  tail -20 "$W/api.log"
  ko "the API did not start"
}
[[ "$(curl -sk -o /dev/null -w '%{http_code}' "$WEB/admin/login" || true)" == 200 ]] && ok "the web application serves the sign-in page with its launch profile" || {
  tail -20 "$W/web.log"
  ko "the web application did not start"
}

# The password goes to curl in a file the owner only reads, never on its command line.
manager_password="$(sed -n 's/^manager=//p' "$DEV_F5_DIR/accounts")"
(
  umask 077
  printf '{"userName":"manager","password":"%s"}' "$manager_password" >"$W/login.json"
)
[[ "$(curl -s -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' --data @"$W/login.json" "$API/api/auth/login" || true)" == 200 ]] &&
  ok "the API signs in the development Manager" || ko "the API sign-in was refused"
token="$(curl -sk -c "$W/jar" "$WEB/admin/login" | sed -n 's/.*name="__RequestVerificationToken" value="\([^"]*\)".*/\1/p' | head -n 1)"
(
  umask 077
  printf '_handler=staff-login&__RequestVerificationToken=%s&Input.UserName=manager&Input.Password=%s' "$token" "$manager_password" >"$W/form.txt"
)
redirect="$(curl -sk -b "$W/jar" -c "$W/jar" -o /dev/null -w '%{http_code} %{redirect_url}' --data @"$W/form.txt" "$WEB/admin/login" || true)"
[[ "$redirect" == "302 $WEB/admin" ]] && [[ "$(curl -sk -b "$W/jar" "$WEB/admin" | grep -c 'Dashboard')" -ge 1 ]] &&
  ok "the back office signs in through the web form and the API" || ko "back office sign-in: $redirect"

# The API made the tables at its start, so the sample data can be loaded now.
f5 --seed >"$W/seed.log" 2>&1 &&
  [[ "$(docker exec "$DEV_F5_NAME-postgres" psql -U nfhotel -d nfhotel -tAc 'select count(*) from room')" -gt 0 ]] &&
  ok "--seed loads the sample rooms of db/seed_dev.sql" || ko "--seed: $(grep -E 'error|ERROR' "$W/seed.log" | head -n 3)"

stop_apps
f5 --destroy >/dev/null 2>&1
[[ -z "$(docker ps -aq --filter "name=^$DEV_F5_NAME-postgres$")$(docker volume ls -q --filter "name=^$DEV_F5_NAME-pgdata$")" && ! -e "$DEV_F5_DIR" ]] &&
  ok "--destroy removes the container, the volume and the state" || ko "--destroy left the container, the volume or the state"
[[ -z "$(secret_keys src/NFHotel.Api)$(secret_keys src/NFHotel.Web)" ]] && ok "--destroy removes the user secrets it wrote" ||
  ko "user secrets left: $(secret_keys src/NFHotel.Api) $(secret_keys src/NFHotel.Web)"

echo "$pass passed, $fail failed"
((fail == 0))
