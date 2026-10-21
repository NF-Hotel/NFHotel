#!/usr/bin/env bash
# Smoke test for the nginx proxy and the NFHotel web service (MIL-002 task 6).
#
# Starts the Docker Compose services (nginx, web, market analysis) on a private
# network with throwaway
# settings, checks the web application through the proxy, then removes every
# container, image layer reference, network and temporary file it created.
# It never reads nginx.env or nfhotel-web.env and needs no input. The web
# application starts without a database, so no PostgreSQL is needed.
#
# Usage: test-compose-proxy.sh [--port <host port>] [--keep]
#   --port  host port for the proxy (default 18480)
#   --keep  leave the services running after the test (for debugging)
# Exit:  0 all checks passed; 1 a check or the setup failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, curl, openssl
# Task:  MIL-002 task 6 and MIL-004 task 7 (docs/milestones)

set -euo pipefail

readonly NETWORK="nfhotel-smoke-net"
readonly DB_NETWORK="nfhotel-smoke-db"
readonly API_NETWORK="nfhotel-smoke-api"
readonly SMOKE_API_TOKEN="smoke-token-0123456789-smoke-token-0123456789"
readonly LLM_NETWORK="nfhotel-smoke-llm"
readonly NGINX_PROJECT="nfhotel-smoke-nginx"
readonly WEB_PROJECT="nfhotel-smoke-web"
readonly API_PROJECT="nfhotel-smoke-api"
readonly ANALYSIS_PROJECT="nfhotel-smoke-analysis"
readonly ANALYSIS_HOST="analysis.localhost"
readonly ANALYSIS_API_HOST="analysis-api.localhost"

TMP_DIR=""
TMP_NATIVE=""
KEEP=0
PORT=18480
FAILURES=0

die() {
  echo "error: $*" >&2
  exit 1
}

check() {
  local name="$1" expected="$2" actual="$3"
  if [[ "$actual" == "$expected" ]]; then
    echo "PASS  $name"
  else
    echo "FAIL  $name (expected $expected, got $actual)" >&2
    FAILURES=$((FAILURES + 1))
  fi
}

# The public service is served over TLS (MIL-017): the certificate is a throwaway self-signed one, so -k.
https_port() {
  echo "$((PORT + 2))"
}

https_status() {
  local method="$1" path="$2"
  curl --silent --insecure --output /dev/null --write-out '%{http_code}' \
    --max-time 10 --request "$method" "https://127.0.0.1:$(https_port)${path}" || true
}

https_location() {
  local path="$1"
  curl --silent --insecure --output /dev/null --write-out '%{redirect_url}' \
    --max-time 10 "https://127.0.0.1:$(https_port)${path}" || true
}

http_status() {
  local method="$1" path="$2" host="${3:-}" port="${4:-$PORT}"
  local -a header=()
  [[ -n "$host" ]] && header=(--header "Host: $host")
  curl --silent --output /dev/null --write-out '%{http_code}' \
    --max-time 10 --request "$method" "${header[@]}" \
    "http://127.0.0.1:${port}${path}" || true
}

wait_for_status() {
  local method="$1" path="$2" expected="$3" tries="${4:-30}" host="${5:-}" port="${6:-$PORT}" status=""
  while ((tries > 0)); do
    status="$(http_status "$method" "$path" "$host" "$port")"
    [[ "$status" == "$expected" ]] && return 0
    tries=$((tries - 1))
    sleep 2
  done
  return 1
}

compose_nginx() {
  NGINX_ENV_FILE="$TMP_NATIVE/nginx.env" NFHOTEL_PROXY_NETWORK="$NETWORK" \
    NGINX_BIND=127.0.0.1 NGINX_HTTP_PORT="$PORT" NGINX_INTERNAL_PORT="$((PORT + 1))" \
    NGINX_HTTPS_PORT="$((PORT + 2))" NGINX_TLS_DIR="$TMP_NATIVE/tls" \
    docker compose --project-name "$NGINX_PROJECT" \
    --file deploy/nginx/docker-compose.yml "$@"
}

compose_web() {
  NFHOTEL_WEB_ENV_FILE="$TMP_NATIVE/nfhotel-web.env" NFHOTEL_PROXY_NETWORK="$NETWORK" NFHOTEL_API_NETWORK="$API_NETWORK" API_TOKEN="$SMOKE_API_TOKEN" \
    docker compose --project-name "$WEB_PROJECT" \
    --file deploy/nfhotel-web/docker-compose.yml "$@"
}

compose_api() {
  NFHOTEL_API_ENV_FILE="$TMP_NATIVE/nfhotel-api.env" NFHOTEL_API_NETWORK="$API_NETWORK" \
    NFHOTEL_DB_NETWORK="$DB_NETWORK" POSTGRES_PASSWORD=smoke \
    docker compose --project-name "$API_PROJECT" \
    --file deploy/nfhotel-api/docker-compose.yml "$@"
}

compose_analysis() {
  MARKET_ANALYSIS_ENV_FILE="$TMP_NATIVE/market-analysis.env" \
    MARKET_ANALYSIS_CONFIG="$TMP_NATIVE/market-analysis.toml" \
    NFHOTEL_PROXY_NETWORK="$NETWORK" NFHOTEL_LLM_NETWORK="$LLM_NETWORK" \
    docker compose --project-name "$ANALYSIS_PROJECT" \
    --file deploy/market-analysis/docker-compose.yml "$@"
}

cleanup() {
  if ((KEEP == 0)); then
    compose_nginx down --volumes --remove-orphans >/dev/null 2>&1 || true
    compose_web down --volumes --remove-orphans >/dev/null 2>&1 || true
    compose_api down --volumes --remove-orphans >/dev/null 2>&1 || true
    compose_analysis down --volumes --remove-orphans >/dev/null 2>&1 || true
    docker network rm "$NETWORK" "$DB_NETWORK" "$LLM_NETWORK" "$API_NETWORK" >/dev/null 2>&1 || true
  fi
  [[ -n "$TMP_DIR" ]] && rm -rf "$TMP_DIR"
}

write_settings() {
  cat >"$TMP_DIR/nginx.env" <<EOF
NGINX_SERVER_NAME=_
NGINX_REDIRECT_PORT=:$((PORT + 2))
NGINX_HSTS=
UPSTREAM_HOST=nfhotel-web
UPSTREAM_PORT=8080
ANALYSIS_SERVER_NAME=$ANALYSIS_HOST
ANALYSIS_UPSTREAM_HOST=market-analysis-notebook
ANALYSIS_UPSTREAM_PORT=2718
ANALYSIS_API_SERVER_NAME=$ANALYSIS_API_HOST
ANALYSIS_API_UPSTREAM_HOST=market-analysis-api
ANALYSIS_API_UPSTREAM_PORT=8000
EOF
  cp deploy/market-analysis/market-analysis.toml.example "$TMP_DIR/market-analysis.toml"
  printf 'NFHOTEL_PROXY_NETWORK=%s
' "$NETWORK" >"$TMP_DIR/market-analysis.env"
  cat >"$TMP_DIR/nfhotel-web.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Tls__Require=true
HotelTime__TimeZoneId=Europe/Copenhagen
EOF
  bash scripts/make-dev-certificate.sh "$TMP_DIR/tls" >/dev/null
  cat >"$TMP_DIR/nfhotel-api.env" <<EOF
ASPNETCORE_ENVIRONMENT=Production
Encryption__PassportKey=$(openssl rand -base64 32)
HotelTime__TimeZoneId=Europe/Copenhagen
Api__Token=$SMOKE_API_TOKEN
Staff__Token__SigningKey=$(openssl rand -base64 48 | tr -d '\n')
EOF
}

main() {
  local root web_id api_id
  root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --port)
        PORT="${2:?usage: $0 [--port <host port>] [--keep]}"
        shift 2
        ;;
      --keep)
        KEEP=1
        shift
        ;;
      *)
        echo "usage: $0 [--port <host port>] [--keep]" >&2
        exit 2
        ;;
    esac
  done

  for tool in docker curl openssl; do
    command -v "$tool" >/dev/null 2>&1 || die "$tool is not installed or not on PATH"
  done
  docker info >/dev/null 2>&1 || die "the docker daemon is not running; start Docker and retry"
  docker compose version >/dev/null 2>&1 || die "the docker compose plugin is missing"

  cd "$root"
  TMP_DIR="$(mktemp -d)"
  # Docker on Windows needs a native path; cygpath exists in Git Bash.
  TMP_NATIVE="$TMP_DIR"
  if command -v cygpath >/dev/null 2>&1; then
    TMP_NATIVE="$(cygpath -m "$TMP_DIR")"
  fi
  trap cleanup EXIT
  write_settings

  docker network create "$NETWORK" >/dev/null
  docker network create --internal "$DB_NETWORK" >/dev/null
  docker network create --internal "$API_NETWORK" >/dev/null
  docker network create "$LLM_NETWORK" >/dev/null
  echo "Building and starting the database API..."
  compose_api up --detach --build --wait --wait-timeout 180 >/dev/null ||
    die "the database API did not become healthy"
  echo "Building and starting the web service..."
  compose_web up --detach --build --wait --wait-timeout 180 >/dev/null ||
    die "the web service did not become healthy"
  echo "Building and starting the market analysis service..."
  compose_analysis up --detach --build --wait --wait-timeout 300 >/dev/null ||
    die "the market analysis service did not become healthy"
  echo "Starting the proxy..."
  compose_nginx up --detach --wait --wait-timeout 60 >/dev/null ||
    die "the proxy did not start"

  wait_for_status GET / 301 || true
  check "GET / on port 80 redirects" 301 "$(http_status GET /)"
  check "port 80 redirects to HTTPS" "https://127.0.0.1:$(https_port)/" \
    "$(curl --silent --output /dev/null --write-out '%{redirect_url}' --max-time 10 "http://127.0.0.1:${PORT}/")"
  check "GET /admin on port 80 serves no page" 301 "$(http_status GET /admin)"
  check "GET / through the proxy over TLS" 200 "$(https_status GET /)"
  check "POST /_blazor/negotiate through the proxy over TLS" 200 \
    "$(https_status POST '/_blazor/negotiate?negotiateVersion=1')"
  check "TLS 1.2 is accepted" 200 \
    "$(curl --silent --insecure --tls-max 1.2 --tlsv1.2 --output /dev/null --write-out '%{http_code}' --max-time 10 "https://127.0.0.1:$(https_port)/")"
  check "TLS 1.1 is refused" 000 \
    "$(curl --silent --insecure --tls-max 1.1 --output /dev/null --write-out '%{http_code}' --max-time 10 "https://127.0.0.1:$(https_port)/" || true)"
  check "no Strict-Transport-Security with the development certificate" "" \
    "$(curl --silent --insecure --head --max-time 10 "https://127.0.0.1:$(https_port)/" | grep -i '^strict-transport-security' || true)"
  check "plain HTTP sent to the TLS port is redirected" 301 "$(http_status GET /admin/login "" "$(https_port)")"
  check "plain HTTP sent to the TLS port goes to the same address over HTTPS" \
    "https://127.0.0.1:$(https_port)/admin/login?returnUrl=%2Fadmin" \
    "$(curl --silent --output /dev/null --write-out '%{redirect_url}' --max-time 10 "http://127.0.0.1:$(https_port)/admin/login?returnUrl=%2Fadmin")"

  wait_for_status GET / 200 30 "$ANALYSIS_HOST" "$((PORT + 1))" || true
  check "GET / of the analysis notebook through the proxy" 200 \
    "$(http_status GET / "$ANALYSIS_HOST" "$((PORT + 1))")"
  check "GET /health of the analysis API through the proxy" 200 \
    "$(http_status GET /health "$ANALYSIS_API_HOST" "$((PORT + 1))")"
  check "analysis services publish no host port" "" \
    "$(for id in $(compose_analysis ps --quiet); do docker port "$id" 2>/dev/null; done)"

  web_id="$(compose_web ps --quiet nfhotel-web)"
  check "web service publishes no host port" "" \
    "$(docker port "$web_id" 2>/dev/null || true)"

  api_id="$(compose_api ps --quiet nfhotel-api)"
  check "database API publishes no host port" "" \
    "$(docker port "$api_id" 2>/dev/null || true)"
  check "web service is not on the database network" "" \
    "$(docker inspect --format '{{range $name, $net := .NetworkSettings.Networks}}{{$name}} {{end}}' "$web_id" | tr ' ' '\n' | grep -x "$DB_NETWORK" || true)"
  check "database API is on the database network" "$DB_NETWORK" \
    "$(docker inspect --format '{{range $name, $net := .NetworkSettings.Networks}}{{$name}} {{end}}' "$api_id" | tr ' ' '\n' | grep -x "$DB_NETWORK" || true)"
  check "web container holds no database setting" "" \
    "$(docker exec "$web_id" printenv | grep -i -E 'ConnectionStrings|PassportKey|POSTGRES' || true)"
  check "database API refuses a call without the token" 401 \
    "$(docker exec "$web_id" bash -c 'exec 3<>/dev/tcp/nfhotel-api/8080 && printf "GET /api/rooms HTTP/1.0\r\n\r\n" >&3 && head -n 1 <&3' | cut -d' ' -f2)"

  # The back office is behind the staff sign-in (MIL-016, ADR-0005).
  check "back office answers with a redirect when nobody is signed in" 302 \
    "$(https_status GET /admin)"
  check "back office redirects to the login page" "/admin/login" \
    "$(https_location /admin | sed -E 's|^https?://[^/]+||; s|[?].*$||')"
  check "login page is open" 200 "$(https_status GET /admin/login)"
  check "database API refuses a staff endpoint with the web token alone" 401 \
    "$(docker exec "$web_id" bash -c 'exec 3<>/dev/tcp/nfhotel-api/8080 && printf "GET /api/rooms HTTP/1.0\r\nAuthorization: Bearer %s\r\n\r\n" "$Api__Token" >&3 && head -n 1 <&3' | cut -d' ' -f2)"
  check "web container holds no staff signing key" "" \
    "$(docker exec "$web_id" printenv | grep -i -E 'SigningKey|InitialManager' || true)"

  compose_web stop >/dev/null
  check "proxy runs while the web service is stopped" true \
    "$(docker inspect --format '{{.State.Running}}' "$(compose_nginx ps --quiet nginx)")"
  check "proxy answers 502 while the web service is stopped" 502 \
    "$(https_status GET /)"

  compose_web start >/dev/null
  compose_nginx stop >/dev/null
  check "web service runs while the proxy is stopped" true \
    "$(docker inspect --format '{{.State.Running}}' "$web_id")"

  if ((FAILURES > 0)); then
    die "$FAILURES check(s) failed"
  fi
  echo "All checks passed."
}

main "$@"
