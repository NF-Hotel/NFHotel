#!/usr/bin/env bash
# Rotation drill of the secrets of NF Hotel on a throwaway stack (MIL-035 task 4, ADR-0024). It
# follows the rotation steps of the wiki page Proxmox-Deployment ("Rotate a secret"): write the
# new value in the settings file, write the secret files again and redeploy. Then it proves the
# new value works and the old one no longer does:
#   - the API token: the old token is refused (401), the new one signs in, and the web
#     application was recreated with it;
#   - the staff token signing key: a staff session from before the rotation is refused;
#   - the database password: ALTER ROLE first, then the API logs in with the new password and
#     the old one is refused;
#   - the first Manager's password: after the account exists it is removed from the settings,
#     and the API still starts.
# The passport key is not rotated here: it encrypts stored data, so a new key needs the data
# re-encrypted first (ADR-0024).
#
# Usage: test-secret-rotation.sh --app-dir <NFHotel checkout>
# Exit:  0 every check passed; 1 a check failed or the stack did not start; 2 usage error
# Needs: bash 4+, docker with the compose plugin, openssl

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly NAME=nfhotel-rot CURL=curlimages/curl:8.10.1
[[ "${1:-}" == "--app-dir" && -n "${2:-}" ]] || {
  echo "usage: test-secret-rotation.sh --app-dir <NFHotel checkout>" >&2
  exit 2
}
app="$(cd "$2" && pwd)"
W="$(mktemp -d)"
trap 'bash "$OPS/scripts/e2e-stack.sh" down --name "$NAME" >/dev/null 2>&1; rm -rf "$W"' EXIT
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

bash "$OPS/scripts/e2e-stack.sh" up --app-dir "$app" --out "$W/stack" --name "$NAME" >"$W/up.log" 2>&1 || {
  tail -20 "$W/up.log"
  echo "FAIL  the throwaway stack did not start"
  exit 1
}
settings="$W/stack/settings"
user="$(sed -n 's/^E2E_MANAGER_USER=//p' "$W/stack/stack.env")"
password="$(sed -n 's/^E2E_MANAGER_PASSWORD=//p' "$W/stack/stack.env")"

# The compose call of e2e-stack.sh, with the images it built.
export NFHOTEL_API_IMAGE="$NAME-api:local" NFHOTEL_WEB_IMAGE="$NAME-web:local" NFHOTEL_APP_DIR="$app"
export NFHOTEL_PROXY_NETWORK="$NAME-proxy" NFHOTEL_API_NETWORK="$NAME-api" NFHOTEL_DB_NETWORK="$NAME-db"
export POSTGRES_ENV_FILE="$settings/postgres/settings.env" POSTGRES_SECRETS="$settings/postgres"
export NFHOTEL_API_ENV_FILE="$settings/api/settings.env" NFHOTEL_API_SECRETS="$settings/api"
export NFHOTEL_WEB_ENV_FILE="$settings/web/settings.env" NFHOTEL_WEB_SECRETS="$settings/web"
compose() {
  local folder="$1"
  shift
  docker compose --project-name "$NAME-$folder" --file "$OPS/deploy/$folder/docker-compose.yml" "$@"
}

# set_setting <file> <key> <value>: the operator's step, writing the new value in the settings.
set_setting() {
  grep -v "^$2=" "$1" >"$1.new" || true
  echo "$2=$3" >>"$1.new"
  mv "$1.new" "$1"
}

# redeploy <service...>: what scripts/deploy-services.sh does at a deploy.
redeploy() {
  local service folder
  for service in "$@"; do
    bash "$OPS/scripts/write-secret-files.sh" "$service" --out "$settings/$service" \
      --postgres "$settings/postgres.env" --api "$settings/nfhotel-api.env" --web "$settings/nfhotel-web.env" >/dev/null
    [[ "$service" == postgres ]] && continue
    folder="nfhotel-$service"
    NFHOTEL_SETTINGS_HASH="$(cat "$settings/$service"/* | sha256sum | cut -c1-16)" \
      compose "$folder" up --detach --wait --wait-timeout 120 >>"$W/redeploy.log" 2>&1 || return 1
  done
}

# sign_in <api token>: print the HTTP status of a staff sign-in, and keep the answer in $W/session.
sign_in() {
  docker run --rm --network "$NAME-api" "$CURL" -s -o - -w '\n%{http_code}' \
    -H "Authorization: Bearer $1" -H "Content-Type: application/json" \
    -d "{\"userName\":\"$user\",\"password\":\"$password\"}" http://nfhotel-api:8080/api/auth/login >"$W/answer"
  sed '$d' "$W/answer" >"$W/session"
  tail -n 1 "$W/answer"
}

# staff_list <api token> <staff token>: the HTTP status of a call that needs a staff session.
staff_list() {
  docker run --rm --network "$NAME-api" "$CURL" -s -o /dev/null -w '%{http_code}' \
    -H "Authorization: Bearer $1" -H "X-Staff-Token: $2" http://nfhotel-api:8080/api/staff
}

session_token() { sed -n 's/.*"token":"\([^"]*\)".*/\1/p' "$W/session"; }

# 1. The API token, shared by the API and the web application.
old_token="$(cat "$settings/api/Api__Token")"
new_token="$(openssl rand -hex 24)"
set_setting "$settings/nfhotel-api.env" Api__Token "$new_token"
web_before="$(docker inspect --format '{{.Id}}' "$NAME-nfhotel-web-nfhotel-web-1")"
redeploy api web && ok "the API and the web application are redeployed with a new API token" || ko "redeploy: $(tail -5 "$W/redeploy.log")"
[[ "$(sign_in "$old_token")" == 401 ]] && ok "the old API token is refused" || ko "the old API token still works"
[[ "$(sign_in "$new_token")" == 200 ]] && ok "the new API token signs in" || ko "the new API token: $(cat "$W/session")"
[[ "$(docker inspect --format '{{.Id}}' "$NAME-nfhotel-web-nfhotel-web-1")" != "$web_before" ]] &&
  docker exec "$NAME-nfhotel-web-nfhotel-web-1" sh -c 'cat /run/secrets/Api__Token' | cmp -s - <(printf '%s' "$new_token") &&
  ok "the web application was recreated and reads the new token" || ko "the web application kept the old token"

# 2. The signing key of the staff tokens.
sign_in "$new_token" >/dev/null
old_session="$(session_token)"
[[ "$(staff_list "$new_token" "$old_session")" != 401 ]] || ko "the session did not work before the rotation"
set_setting "$settings/nfhotel-api.env" Staff__Token__SigningKey "$(openssl rand -hex 32)"
redeploy api || ko "redeploy: $(tail -5 "$W/redeploy.log")"
[[ "$(staff_list "$new_token" "$old_session")" == 401 ]] && ok "a staff session signed with the old key is refused" ||
  ko "a session of the old signing key still works"
sign_in "$new_token" >/dev/null
[[ "$(staff_list "$new_token" "$(session_token)")" != 401 ]] && ok "a new staff session works" || ko "a new session is refused"

# 3. The database password: the role first, then the settings.
old_db="$(sed -n 's/^POSTGRES_PASSWORD=//p' "$settings/postgres.env")"
new_db="$(openssl rand -hex 24)"
printf "ALTER ROLE nfhotel PASSWORD '%s';\n" "$new_db" |
  docker exec -i "$NAME-postgres-postgres-1" psql -q -U nfhotel -d nfhotel -v ON_ERROR_STOP=1 >/dev/null &&
  ok "the role has a new password" || ko "ALTER ROLE failed"
set_setting "$settings/postgres.env" POSTGRES_PASSWORD "$new_db"
redeploy postgres api && [[ "$(sign_in "$new_token")" == 200 ]] &&
  ok "the API logs in to the database with the new password" || ko "the API after the new database password: $(cat "$W/session")"
image="$(docker inspect --format '{{.Config.Image}}' "$NAME-postgres-postgres-1")"
docker run --rm --network "$NAME-db" -e PGPASSWORD="$old_db" "$image" \
  psql -h postgres -U nfhotel -d nfhotel -c 'select 1' >/dev/null 2>&1 &&
  ko "the old database password still logs in" || ok "the old database password is refused"

# 4. The first Manager's password is only read while no staff account exists.
set_setting "$settings/nfhotel-api.env" Staff__InitialManager__Password ""
redeploy api && [[ "$(sign_in "$new_token")" == 200 ]] &&
  ok "the API starts without the first Manager's password, and the account keeps its own" || ko "after removing it"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
