#!/usr/bin/env bash
# Tests of scripts/write-secret-files.sh (MIL-035 tasks 2 and 3): each secret becomes a file of
# its own with the value whole (CRLF settings, a trailing =), the settings for the container hold
# no secret, the login of nfhotel_app wins over the superuser, a missing setting is an empty
# file, the folder is closed to others, and bad input is refused.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-write-secret-files.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
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
write() { bash "$OPS/scripts/write-secret-files.sh" "$@" >"$W/log" 2>&1; }

printf 'POSTGRES_USER=nfhotel\r\nPOSTGRES_PASSWORD=pw0123=\r\nPOSTGRES_DB=hotel\r\n' >"$W/postgres.env"
printf '# comment\nApi__Token=tok+en/==\nEncryption__PassportKey=key=\nHotelTime__TimeZoneId=Europe/Copenhagen\n' >"$W/api.env"
printf 'Tls__Require=true\nHolidayApi__ApiKey=\n' >"$W/web.env"
echo "NFBENCH_API_TOKEN=bench" >"$W/benchmark.env"

write postgres --out "$W/pg" --postgres "$W/postgres.env" &&
  [[ "$(cat "$W/pg/POSTGRES_PASSWORD")" == "pw0123=" ]] && ! grep -q PASSWORD "$W/pg/settings.env" &&
  grep -qx "POSTGRES_DB=hotel" "$W/pg/settings.env" &&
  ok "the database password is a file without CR, and the settings keep the rest" || ko "postgres: $(cat "$W/log")"

write api --out "$W/api" --api "$W/api.env" --postgres "$W/postgres.env" &&
  [[ "$(cat "$W/api/ConnectionStrings__HotelDatabase")" == "Host=postgres;Port=5432;Database=hotel;Username=nfhotel;Password=pw0123=" ]] &&
  [[ "$(cat "$W/api/Api__Token")" == "tok+en/==" && ! -s "$W/api/Staff__InitialManager__Password" ]] &&
  ok "the API gets the built connection string, whole values, and an empty file for a missing setting" ||
  ko "api: $(cat "$W/log")"
grep -q "Api__Token\|Encryption__PassportKey" "$W/api/settings.env" && ko "the settings of the API hold a secret" ||
  ok "the settings of the API hold no secret"
[[ "$(stat -c %a "$W/api")" == 700 && "$(stat -c %a "$W/api/Api__Token")" == 444 ]] &&
  ok "the folder is closed to others, and a secret is readable by the container user" ||
  ko "modes: $(stat -c '%a %n' "$W/api" "$W/api/Api__Token")"

NFHOTEL_API_CONNECTION_STRING="Host=postgres;Username=nfhotel_app;Password=app" \
  write api --out "$W/api2" --api "$W/api.env" &&
  [[ "$(cat "$W/api2/ConnectionStrings__HotelDatabase")" == *"nfhotel_app"* ]] &&
  ok "the login of nfhotel_app wins, and postgres.env is not needed then" || ko "app login: $(cat "$W/log")"

write web --out "$W/web" --web "$W/web.env" --api "$W/api.env" --benchmark "$W/benchmark.env" &&
  [[ "$(cat "$W/web/Api__Token")" == "tok+en/==" && "$(cat "$W/web/BenchmarkService__Token")" == bench ]] &&
  [[ ! -s "$W/web/HolidayApi__ApiKey" ]] && ! grep -q HolidayApi__ApiKey "$W/web/settings.env" &&
  ok "the web application takes the tokens of the API and the benchmark service" || ko "web: $(cat "$W/log")"

printf 'POSTGRES_USER=n\nPOSTGRES_PASSWORD=a;b\nPOSTGRES_DB=d\n' >"$W/bad.env"
write api --out "$W/api3" --api "$W/api.env" --postgres "$W/bad.env" && ko "accepted a ; in the password" ||
  ok "a ; in the password is refused (it would break the connection string)"
write web --out "$W/web2" --web "$W/web.env" && ko "web ran without the API settings" ||
  ok "a missing settings file is refused"
write mail --out "$W/x" && ko "accepted an unknown service" || ok "an unknown service is a usage error"
[[ "$(bash "$OPS/scripts/write-secret-files.sh" --list postgres)" == POSTGRES_PASSWORD ]] &&
  ok "--list prints the secrets of a service" || ko "list"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
