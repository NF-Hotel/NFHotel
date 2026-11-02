#!/usr/bin/env bash
# Prepare this machine so NF Hotel starts from Visual Studio with F5 (ADR-0036, MIL-057): the API and the web
# application then run as .NET processes with the debugger, and only PostgreSQL runs in Docker. Run it once, then open
# NFHotel.sln, choose the launch profile "API and web" and press F5 (or: dotnet run --project src/NFHotel.Api, and
# dotnet run --project src/NFHotel.Web --launch-profile https). From PowerShell: .\scripts\dev-up.ps1 --f5
#
#   1. start PostgreSQL 16 in Docker (the image of ops/deploy/postgres), on 127.0.0.1:55432 only, with a volume
#   2. write the .NET user secrets of NFHotel.Api (database, API token, signing key, passport key, first Manager) and of
#      NFHotel.Web (the API address and token, the passport key). A key that is already set is kept, so a second run
#      changes nothing; the keys this script wrote are listed in ops/.dev-f5/written-keys, so --destroy removes those
#      and no others. The secrets are passed to dotnet user-secrets as JSON on standard input, never on a command line.
#   3. print the development Manager account (development only, as the other development modes do)
#
# Usage: dev-f5.sh [--check | --down | --destroy]
#   --check    only test that this machine can run it, and say how to fix each problem
#   --down     stop PostgreSQL; the database and the secrets stay
#   --destroy  remove PostgreSQL, its volume, ops/.dev-f5/ and the user secrets this script wrote
# Env:   DEV_F5_DB_PORT port of PostgreSQL on 127.0.0.1 (default 55432); DEV_F5_NAME prefix of the container and the
#        volume (default nfhotel-f5); DEV_F5_DIR folder of the state (default ops/.dev-f5). A test sets them.
# Exit:  0 done; 1 a check or step failed; 2 usage error
# Needs: bash 4+, docker, dotnet (the SDK of global.json), openssl

set -euo pipefail
# Before anything of bash 4 runs: macOS comes with bash 3.2.
if ((BASH_VERSINFO[0] < 4)); then
  echo "error: bash ${BASH_VERSION} is too old; install bash 4 or later (on macOS: brew install bash, then open a new terminal)" >&2
  exit 1
fi

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$root/ops/scripts/lib/common.sh"
export MSYS_NO_PATHCONV=1

readonly PORT="${DEV_F5_DB_PORT:-55432}"
readonly NAME="${DEV_F5_NAME:-nfhotel-f5}"
readonly DIR="${DEV_F5_DIR:-$root/ops/.dev-f5}"
readonly CONTAINER="$NAME-postgres" VOLUME="$NAME-pgdata"
readonly API_PROJECT="$root/src/NFHotel.Api/NFHotel.Api.csproj" WEB_PROJECT="$root/src/NFHotel.Web/NFHotel.Web.csproj"
# The address of the API that the web application calls: the http address of the API's launch profile.
readonly API_URL="http://localhost:59103"
action=up

for arg in "$@"; do
  case "$arg" in
    --check) action=check ;;
    --down) action=down ;;
    --destroy) action=destroy ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "error: unknown argument: $arg" >&2
      usage >&2
      exit 2
      ;;
  esac
done
[[ "$PORT" =~ ^[0-9]+$ ]] || die "DEV_F5_DB_PORT must be a port number"
[[ "$NAME" =~ ^[a-z0-9][a-z0-9-]*$ ]] || die "DEV_F5_NAME must be lower case letters, digits and -"

# A native path for the Docker daemon and dotnet (Git Bash on Windows has cygpath).
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

# A native Windows openssl ends its output with \r\n, and a \r in a JSON value breaks dotnet user-secrets.
secret() { openssl rand -hex "${1:-24}" | tr -d '\r\n'; }

# The pinned PostgreSQL image of the deployment, read from its compose file so it is named once.
postgres_image() {
  sed -n 's/^ *image: *\(postgres:[^ ]*\).*/\1/p' "$root/ops/deploy/postgres/docker-compose.yml" | head -n 1
}

# secrets_of <project>: the user secrets of a project as key=value lines (read into this process only).
secrets_of() {
  dotnet user-secrets list --json --project "$(native "$1")" 2>/dev/null |
    sed -n 's/^ *"\([^"]*\)": *"\(.*\)",\{0,1\} *$/\1=\2/p' || true
}

# value_in <key=value lines> <key>: the value of one key.
value_in() {
  local line
  while IFS= read -r line; do
    [[ "${line%%=*}" == "$2" ]] && printf '%s' "${line#*=}" && return 0
  done <<<"$1"
  return 1
}

# set_missing <project> <label> <key=value...>: set the keys the project does not have yet, as JSON on standard input,
# and list them in written-keys.
set_missing() {
  local project="$1" label="$2" existing pair key json="" count=0
  shift 2
  existing="$(secrets_of "$project")"
  for pair in "$@"; do
    key="${pair%%=*}"
    value_in "$existing" "$key" >/dev/null && continue
    json+="${json:+,}\"${key}\":\"${pair#*=}\""
    printf '%s %s\n' "$label" "$key" >>"$DIR/written-keys"
    count=$((count + 1))
  done
  if ((count > 0)); then
    printf '{%s}' "$json" | dotnet user-secrets set --project "$(native "$project")" >/dev/null ||
      die "dotnet user-secrets could not write the secrets of $label"
  fi
  echo "==> $label: $count user secret(s) written, $(($# - count)) kept"
}

# Tests that this machine can run it, and says how to fix each problem. Nothing is started or changed.
preflight() {
  local problems=0 tool

  report() { # status text
    printf '  %-8s %s\n' "$1" "$2"
    [[ "$1" == "PROBLEM" ]] && problems=$((problems + 1))
    return 0
  }

  echo "Checking this machine for F5 in Visual Studio"
  # `bash` in PowerShell is the bash of WSL: its dotnet and its user secrets are Linux ones, which Visual Studio on
  # Windows never sees. A checkout on a Windows drive is prepared from Git Bash instead.
  if grep -qi microsoft /proc/version 2>/dev/null && [[ "$root" == /mnt/[a-z]/* ]]; then
    report PROBLEM "this is the bash of WSL, so the secrets would not reach Visual Studio; from PowerShell run .\\scripts\\dev-f5.ps1, or run the script in Git Bash"
  else
    report ok "bash ${BASH_VERSION%%(*}"
  fi
  for tool in openssl dotnet; do
    if command -v "$tool" >/dev/null 2>&1; then
      report ok "$tool"
    else
      report PROBLEM "$tool is not on PATH (dotnet: install the .NET SDK of global.json, or Visual Studio 2026)"
    fi
  done
  if command -v dotnet >/dev/null 2>&1; then
    if (cd "$root" && dotnet --version >/dev/null 2>&1); then
      report ok ".NET SDK $(cd "$root" && dotnet --version) (global.json)"
    else
      report PROBLEM "the .NET SDK of global.json is not installed: $(sed -n 's/.*"version": *"\([^"]*\)".*/\1/p' "$root/global.json")"
    fi
  fi
  if ! command -v docker >/dev/null 2>&1; then
    report PROBLEM "docker is not on PATH; install Docker Desktop (Windows, macOS) or Docker Engine (Linux)"
  elif ! docker info >/dev/null 2>&1; then
    report PROBLEM "the Docker daemon does not answer; start Docker Desktop, wait until it says running, and try again"
  else
    report ok "docker $(docker version --format '{{.Server.Version}}' 2>/dev/null)"
  fi
  if [[ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER" 2>/dev/null || true)" == "true" ]]; then
    report ok "port ${PORT} belongs to the running $CONTAINER"
  elif (exec 3<>"/dev/tcp/127.0.0.1/${PORT}") 2>/dev/null; then
    report PROBLEM "port ${PORT} on this machine is in use; stop what uses it, or set DEV_F5_DB_PORT"
  else
    report ok "port ${PORT} is free"
  fi

  if ((problems > 0)); then
    echo "$problems problem(s). Fix them and run: bash scripts/dev-up.sh --f5 --check"
    return 1
  fi
  echo "This machine can run NF Hotel with F5."
}

case "$action" in
  check)
    preflight
    exit
    ;;
  down)
    docker stop "$CONTAINER" >/dev/null 2>&1 || true
    echo "Stopped $CONTAINER; the database and the user secrets stay."
    exit 0
    ;;
  destroy)
    docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
    docker volume rm -f "$VOLUME" >/dev/null 2>&1 || true
    if [[ -f "$DIR/written-keys" ]]; then
      while read -r label key; do
        case "$label" in
          api) project="$API_PROJECT" ;;
          web) project="$WEB_PROJECT" ;;
          *) continue ;;
        esac
        dotnet user-secrets remove "$key" --project "$(native "$project")" >/dev/null 2>&1 || true
      done <"$DIR/written-keys"
    fi
    rm -rf "$DIR"
    echo "Removed $CONTAINER, its volume, the user secrets this script wrote, and the state folder."
    exit 0
    ;;
esac

preflight || exit 1

# 1. State, made once: the database password must stay the one PostgreSQL was created with.
mkdir -p "$DIR"
chmod 700 "$DIR"
if [[ ! -f "$DIR/postgres-password" ]]; then
  (
    umask 077
    secret >"$DIR/postgres-password"
    printf 'manager=%s\n' "$(secret 9)" >"$DIR/accounts"
  )
  # The container user (postgres) is not the owner of the file; the folder keeps others out.
  chmod 444 "$DIR/postgres-password"
fi
db_password="$(cat "$DIR/postgres-password")"
manager_password="$(sed -n 's/^manager=//p' "$DIR/accounts")"

# 2. PostgreSQL, with the password as a file and not in the environment of the container (ADR-0024).
image="$(postgres_image)"
[[ -n "$image" ]] || die "cannot read the PostgreSQL image from ops/deploy/postgres/docker-compose.yml"
if [[ -z "$(docker ps -aq --filter "name=^${CONTAINER}$")" ]]; then
  echo "==> creating $CONTAINER on 127.0.0.1:${PORT}"
  docker run --detach --name "$CONTAINER" --restart unless-stopped \
    --publish "127.0.0.1:${PORT}:5432" --volume "$VOLUME:/var/lib/postgresql/data" \
    --volume "$(native "$DIR/postgres-password"):/run/secrets/postgres-password:ro" \
    --env POSTGRES_USER=nfhotel --env POSTGRES_DB=nfhotel --env POSTGRES_PASSWORD_FILE=/run/secrets/postgres-password \
    "$image" >/dev/null || die "PostgreSQL did not start"
else
  docker start "$CONTAINER" >/dev/null || die "PostgreSQL did not start"
fi
echo "==> waiting for PostgreSQL"
for _ in $(seq 1 30); do
  docker exec "$CONTAINER" pg_isready -U nfhotel -d nfhotel >/dev/null 2>&1 && break
  sleep 2
done
docker exec "$CONTAINER" pg_isready -U nfhotel -d nfhotel >/dev/null 2>&1 || die "PostgreSQL did not become ready"

# 3. The user secrets. The passport key encrypts data that both projects read, so one value serves both: an existing
#    value of either project is reused.
api_secrets="$(secrets_of "$API_PROJECT")"
web_secrets="$(secrets_of "$WEB_PROJECT")"
passport_key="$(value_in "$api_secrets" Encryption:PassportKey || value_in "$web_secrets" Encryption:PassportKey ||
  openssl rand -base64 32 | tr -d '\r\n')"
api_token="$(value_in "$api_secrets" Api:Token || value_in "$web_secrets" Api:Token || secret 32)"
set_missing "$API_PROJECT" api \
  "ConnectionStrings:HotelDatabase=Host=127.0.0.1;Port=${PORT};Database=nfhotel;Username=nfhotel;Password=${db_password}" \
  "Encryption:PassportKey=${passport_key}" \
  "Api:Token=${api_token}" \
  "Staff:Token:SigningKey=$(secret 32)" \
  "Staff:InitialManager:UserName=manager" \
  "Staff:InitialManager:Password=${manager_password}" \
  "Staff:InitialManager:MustChangePassword=false" \
  "Database:MigrateOnStart=true" \
  "HotelTime:TimeZoneId=Europe/Copenhagen"
set_missing "$WEB_PROJECT" web \
  "Api:BaseUrl=${API_URL}" \
  "Api:Token=${api_token}" \
  "Encryption:PassportKey=${passport_key}" \
  "HotelTime:TimeZoneId=Europe/Copenhagen"

# A key the developer had before, with another value, wins over this script; say so, so a mismatch is found.
api_secrets="$(secrets_of "$API_PROJECT")"
web_secrets="$(secrets_of "$WEB_PROJECT")"
for key in Api:Token Encryption:PassportKey; do
  [[ "$(value_in "$api_secrets" "$key")" == "$(value_in "$web_secrets" "$key")" ]] ||
    echo "warning: $key differs between NFHotel.Api and NFHotel.Web in your user secrets; make them the same" >&2
done
[[ "$(value_in "$web_secrets" Api:BaseUrl)" == "$API_URL" ]] ||
  echo "warning: Api:BaseUrl of NFHotel.Web is $(value_in "$web_secrets" Api:BaseUrl), not $API_URL" >&2

echo
echo "Ready. Open NFHotel.sln in Visual Studio, choose the launch profile \"API and web\" and press F5."
echo "  The back office: https://localhost:7060/admin/login   The API: ${API_URL}/health"
echo "  Without Visual Studio: dotnet run --project src/NFHotel.Api, and in a second terminal"
echo "                         dotnet run --project src/NFHotel.Web --launch-profile https"
echo
echo "Back office account (development only):"
printf '  %-14s %-13s %s\n' "user name" "role" "password"
if [[ "$(value_in "$api_secrets" Staff:InitialManager:Password)" == "$manager_password" ]]; then
  printf '  %-14s %-13s %s\n' manager Manager "$manager_password"
else
  echo "  your own first Manager from the user secrets of NFHotel.Api (this script kept it)"
fi
