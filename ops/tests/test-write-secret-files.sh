#!/usr/bin/env bash
# Tests of scripts/write-secret-files.sh (MIL-035 tasks 2 and 3): each secret becomes a file of
# its own with the value whole (CRLF settings, a trailing =), the settings for the container hold
# no secret, the login of nfhotel_app wins over the superuser, a missing setting is an empty
# file, the folder is closed to others, and bad input is refused. MIL-075 task 1: the web
# application gets no passport key, also when its settings file holds the line; a secret of
# another service never reaches the settings of a service and is named in a warning without its
# value; a secret file that a service no longer lists is deleted; and the compose file and the
# example settings of the web name no passport key.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-write-secret-files.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
# No -e: every check runs and is counted, and the exit code says whether one failed.
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
list() { bash "$OPS/scripts/write-secret-files.sh" --list "$1"; }

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
[[ "$(list postgres)" == POSTGRES_PASSWORD ]] &&
  ok "--list prints the secrets of a service" || ko "list"

# The tunnel token of MIL-040, and the environment that the deploy of a release sets.
printf 'TUNNEL_TOKEN=eyJh.tok=\nNFHOTEL_PROXY_NETWORK=nfhotel-proxy\n' >"$W/cloudflared.env"
write cloudflared --out "$W/cf" --cloudflared "$W/cloudflared.env" &&
  [[ "$(cat "$W/cf/TUNNEL_TOKEN")" == "eyJh.tok=" ]] && ! grep -q TUNNEL_TOKEN "$W/cf/settings.env" &&
  ok "the tunnel token is a file, and the settings for the container leave it out" || ko "tunnel: $(cat "$W/log")"
printf 'TUNNEL_TOKEN=\n' >"$W/empty.env"
write cloudflared --out "$W/cf2" --cloudflared "$W/empty.env" && ko "accepted an empty tunnel token" ||
  ok "an empty tunnel token is refused"
printf 'ASPNETCORE_ENVIRONMENT=Development\nTls__Require=true\n' >"$W/web-dev.env"
NFHOTEL_ENVIRONMENT=Production write web --out "$W/webp" --web "$W/web-dev.env" --api "$W/api.env" &&
  grep -qx "ASPNETCORE_ENVIRONMENT=Production" "$W/webp/settings.env" && ! grep -q "=Development" "$W/webp/settings.env" &&
  ok "the deploy of a release runs the web application as Production" || ko "environment: $(cat "$W/webp/settings.env")"
write web --out "$W/webd" --web "$W/web-dev.env" --api "$W/api.env" &&
  grep -qx "ASPNETCORE_ENVIRONMENT=Development" "$W/webd/settings.env" &&
  ok "without NFHOTEL_ENVIRONMENT the settings keep their own environment (development stack)" || ko "kept"
NFHOTEL_ENVIRONMENT='Prod;rm' write web --out "$W/webx" --web "$W/web-dev.env" --api "$W/api.env" &&
  ko "accepted a bad environment name" || ok "a bad environment name is refused"

# MIL-075: only the API holds the passport key (ADR-0004). The settings file of the web on the
# VM held the line before, so the script must leave it out of the environment, not only the file.
printf 'Tls__Require=true\nEncryption__PassportKey=passport-key-value=\nHolidayApi__ApiKey=holiday-key\n' >"$W/web-key.env"
write web --out "$W/webk" --web "$W/web-key.env" --api "$W/api.env" &&
  [[ ! -e "$W/webk/Encryption__PassportKey" ]] && ! grep -q PassportKey "$W/webk/settings.env" &&
  ! list web | grep -qx Encryption__PassportKey &&
  ok "the web gets no passport key, also when its settings file holds the line" ||
  ko "web key: $(ls "$W/webk" 2>&1) $(cat "$W/log")"
grep -q "warning: .*Encryption__PassportKey" "$W/log" && ! grep -q "passport-key-value" "$W/log" &&
  ! grep -rq "passport-key-value" "$W/webk" &&
  ok "the key left in the settings of the web is named in a warning, and its value is nowhere" ||
  ko "warning: $(cat "$W/log")"
grep -qx "Tls__Require=true" "$W/webk/settings.env" && [[ "$(cat "$W/webk/HolidayApi__ApiKey")" == holiday-key ]] &&
  ok "the settings and the own secrets of the web stay as they were" || ko "web kept: $(cat "$W/webk/settings.env")"

# A secret of any other service is left out of the settings of a service too, with a warning; the
# secrets of the service itself are its source and give no warning.
printf 'Api__Token=tok\nTUNNEL_TOKEN=tunnel-token-value\nHotelTime__TimeZoneId=Europe/Copenhagen\n' >"$W/api-x.env"
write api --out "$W/apix" --api "$W/api-x.env" --postgres "$W/postgres.env" &&
  ! grep -q TUNNEL_TOKEN "$W/apix/settings.env" && grep -qx "HotelTime__TimeZoneId=Europe/Copenhagen" "$W/apix/settings.env" &&
  grep -q "warning: .*TUNNEL_TOKEN" "$W/log" && ! grep -q "warning: .*Api__Token" "$W/log" &&
  ! grep -q "tunnel-token-value" "$W/log" &&
  ok "a secret of another service is left out of the settings of the API with a warning, its own are not warned" ||
  ko "cross-service: $(cat "$W/log")"
write api --out "$W/api4" --api "$W/api.env" --postgres "$W/postgres.env" && ! grep -q warning "$W/log" &&
  ok "settings that hold only the own secrets give no warning" || ko "own: $(cat "$W/log")"

# The folder of a service keeps only the secret files that it lists: the passport key that the web
# held before MIL-075 is deleted at the first deploy, and a file that is no secret stays.
mkdir -p "$W/webold"
printf 'old-key' >"$W/webold/Encryption__PassportKey"
chmod 0444 "$W/webold/Encryption__PassportKey"
printf 'note' >"$W/webold/README"
write web --out "$W/webold" --web "$W/web.env" --api "$W/api.env" &&
  [[ ! -e "$W/webold/Encryption__PassportKey" && -e "$W/webold/README" && -e "$W/webold/Api__Token" ]] &&
  ok "a secret file that the service no longer lists is deleted, and another file stays" ||
  ko "stale file: $(ls "$W/webold" 2>&1) $(cat "$W/log")"

# The compose file mounts every secret of the list and no passport key, and the example settings
# of the web hold no passport key line, not even a commented one, for an operator to copy back.
compose="$OPS/deploy/nfhotel-web/docker-compose.yml"
example="$OPS/deploy/nfhotel-web/nfhotel-web.env.example"
mounted=1
while IFS= read -r name; do
  grep -q "file: \${NFHOTEL_WEB_SECRETS:-secrets}/$name\$" "$compose" || mounted=0
done < <(list web)
((mounted)) && ! grep -q PassportKey "$compose" && ! grep -Eq '^[#[:space:]]*Encryption__PassportKey=' "$example" &&
  ok "the compose file and the example settings of the web hold no passport key, and the compose file mounts every secret of the list" ||
  ko "compose or example of the web: $(grep -n PassportKey "$compose" "$example")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
