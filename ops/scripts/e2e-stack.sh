#!/usr/bin/env bash
# Start or remove the throwaway NF Hotel stack that the end-to-end tests and the DAST
# scan run against (MIL-027 tasks 1 and 2 of NFHotel). It runs nginx with a self-signed
# certificate, the web application and the API built from an NF-Hotel/NFHotel checkout,
# and an empty PostgreSQL 16, with the compose files of deploy/ in the deployed shape:
# TLS at nginx, the API and the database on internal networks with no host port.
#
# Every secret is generated for this run and written only to files under <out>, readable
# by the owner; nothing is read from a real settings file. The API applies its migrations
# at start and creates one staff account per role. The stack never reaches the VM, the
# tailnet or a public NF Hotel host: a client joins the Docker network <name>-proxy and
# opens https://nginx.
#
# Usage: e2e-stack.sh up --app-dir <NFHotel checkout> --out <folder> [--name <prefix>]
#        e2e-stack.sh down [--name <prefix>]
#   up       build web and api, start the stack, wait for the health checks, and write
#            <out>/stack.env: E2E_NETWORK, E2E_BASE_URL and the user name and password
#            of the Manager, Receptionist and Housekeeping test accounts
#   down     remove the containers, volumes and networks of the stack
#   --name   prefix of the compose projects and networks (default nfhotel-e2e)
# Exit:  0 done; 1 a build, start or check failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, openssl

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

readonly ROLES=(Manager Receptionist Housekeeping)

action="${1:-}"
[[ "$action" == "up" || "$action" == "down" ]] || {
  usage >&2
  exit 2
}
shift
name="nfhotel-e2e"
app_dir=""
out=""
while (($#)); do
  case "$1" in
    --app-dir)
      app_dir="${2:?--app-dir needs a value}"
      shift
      ;;
    --out)
      out="${2:?--out needs a value}"
      shift
      ;;
    --name)
      name="${2:?--name needs a value}"
      shift
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
[[ "$name" =~ ^[a-z0-9][a-z0-9-]*$ ]] || die "the name '$name' must be lower case letters, digits and -"
readonly PROXY_NET="$name-proxy" API_NET="$name-api" DB_NET="$name-db"

# A native path for the Docker daemon: Git Bash on Windows has cygpath, Linux does not.
native() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else echo "$1"; fi
}

# compose <service folder> <compose arguments...>: run compose for one service of deploy/
# with the settings of this stack.
compose() {
  local folder="$1"
  shift
  NFHOTEL_PROXY_NETWORK="$PROXY_NET" NFHOTEL_API_NETWORK="$API_NET" NFHOTEL_DB_NETWORK="$DB_NET" \
    docker compose --project-name "$name-$folder" --file "$ROOT/deploy/$folder/docker-compose.yml" "$@"
}

down() {
  local folder
  for folder in nginx nfhotel-web nfhotel-api postgres; do
    compose "$folder" down --volumes --remove-orphans >/dev/null 2>&1 || true
  done
  docker network rm "$PROXY_NET" "$API_NET" "$DB_NET" >/dev/null 2>&1 || true
}

if [[ "$action" == "down" ]]; then
  # The compose files need their variables even to be taken down.
  export POSTGRES_PASSWORD=unused API_TOKEN=unused
  down
  echo "==> removed the stack $name"
  exit 0
fi

[[ -n "$app_dir" && -f "$app_dir/Dockerfile" && -f "$app_dir/Dockerfile.api" ]] ||
  die "give --app-dir, an NF-Hotel/NFHotel checkout with Dockerfile and Dockerfile.api"
[[ -n "$out" ]] || die "give --out, the folder for the settings and stack.env"
docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"
mkdir -p "$out/settings"
chmod 700 "$out" "$out/settings"
out="$(cd "$out" && pwd)"
settings="$out/settings"

# Secrets for this run only. hex keeps them free of ; and quotes (see the API compose file).
secret() { openssl rand -hex "${1:-24}"; }
export POSTGRES_PASSWORD API_TOKEN NFHOTEL_APP_DIR NFHOTEL_APP_SHA
POSTGRES_PASSWORD="$(secret)"
API_TOKEN="$(secret)"
NFHOTEL_APP_DIR="$(native "$(cd "$app_dir" && pwd)")"
NFHOTEL_APP_SHA="$(git -C "$app_dir" rev-parse HEAD 2>/dev/null || echo unknown)"
export NFHOTEL_WEB_IMAGE="$name-web:local" NFHOTEL_API_IMAGE="$name-api:local"
manager_password="$(secret 16)"

umask 077
cat >"$settings/postgres.env" <<EOF
POSTGRES_USER=nfhotel
POSTGRES_PASSWORD=$POSTGRES_PASSWORD
POSTGRES_DB=nfhotel
EOF
cat >"$settings/nfhotel-api.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
Encryption__PassportKey=$(openssl rand -base64 32)
HotelTime__TimeZoneId=Europe/Copenhagen
Api__Token=$API_TOKEN
Staff__Token__SigningKey=$(secret 32)
Staff__InitialManager__UserName=e2e-manager
Staff__InitialManager__Password=$manager_password
Database__MigrateOnStart=true
EOF
cat >"$settings/nfhotel-web.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Tls__Require=true
HotelTime__TimeZoneId=Europe/Copenhagen
EOF
cat >"$settings/nginx.env" <<EOF
NGINX_SERVER_NAME=_
NGINX_REDIRECT_PORT=
NGINX_HSTS=
UPSTREAM_HOST=nfhotel-web
UPSTREAM_PORT=8080
ANALYSIS_SERVER_NAME=analysis.localhost
ANALYSIS_UPSTREAM_HOST=market-analysis-notebook
ANALYSIS_UPSTREAM_PORT=2718
ANALYSIS_API_SERVER_NAME=analysis-api.localhost
ANALYSIS_API_UPSTREAM_HOST=market-analysis-api
ANALYSIS_API_UPSTREAM_PORT=8000
EOF
umask 022
bash "$ROOT/scripts/make-dev-certificate.sh" "$settings/tls" nginx >/dev/null

down
docker network create "$PROXY_NET" >/dev/null
docker network create --internal "$API_NET" >/dev/null
docker network create --internal "$DB_NET" >/dev/null

native_settings="$(native "$settings")"
echo "==> starting PostgreSQL"
POSTGRES_ENV_FILE="$native_settings/postgres.env" compose postgres up --detach --wait --wait-timeout 120 >/dev/null ||
  die "PostgreSQL did not become healthy"
echo "==> building and starting the API (migrations at start)"
NFHOTEL_API_ENV_FILE="$native_settings/nfhotel-api.env" compose nfhotel-api up --detach --build --wait --wait-timeout 240 ||
  die "the API did not become healthy"
echo "==> building and starting the web application"
NFHOTEL_WEB_ENV_FILE="$native_settings/nfhotel-web.env" compose nfhotel-web up --detach --build --wait --wait-timeout 240 ||
  die "the web application did not become healthy"
echo "==> starting nginx (TLS with a self-signed certificate, no host port)"
NGINX_ENV_FILE="$native_settings/nginx.env" NGINX_TLS_DIR="$native_settings/tls" \
  compose nginx --file "$ROOT/deploy/e2e/nginx-no-host-ports.yml" up --detach --wait --wait-timeout 60 >/dev/null ||
  die "nginx did not start"

# The first Manager comes from the settings above; the other roles are made with the
# create-staff command of the API image, the password passed in its environment only.
declare -A users=([Manager]=e2e-manager)
declare -A passwords=([Manager]="$manager_password")
for role in "${ROLES[@]:1}"; do
  user="e2e-${role,,}"
  password="$(secret 16)"
  echo "==> creating the $role account $user"
  STAFF_PASSWORD="$password" NFHOTEL_API_ENV_FILE="$native_settings/nfhotel-api.env" \
    compose nfhotel-api run --rm --no-deps -e STAFF_PASSWORD nfhotel-api create-staff "$user" "$role" >/dev/null ||
    die "creating the $role account failed"
  users[$role]="$user"
  passwords[$role]="$password"
done

umask 077
{
  echo "E2E_NETWORK=$PROXY_NET"
  echo "E2E_BASE_URL=https://nginx"
  for role in "${ROLES[@]}"; do
    echo "E2E_${role^^}_USER=${users[$role]}"
    echo "E2E_${role^^}_PASSWORD=${passwords[$role]}"
  done
} >"$out/stack.env"
echo "==> the stack $name is up: join $PROXY_NET and open https://nginx (settings in $out/stack.env)"
