#!/usr/bin/env bash
# Test of the nginx configuration (MIL-017, ADR-0011 decision 7): the HTTPS server with no
# optional route, with the optional API route and no API, and with an API reached from a
# tailnet address and refused from another address. Last, the hardened service of
# deploy/nginx/docker-compose.yml (MIL-023 task 2) starts and turns healthy with a key as
# production has it: 0600 and owned by another user than root.
#
# Usage: test-nginx.sh
# Env:   REPO_HOST  the repository path as the Docker daemon sees it (default: this
#                   checkout; on Windows give C:/... for Docker Desktop)
# It starts the containers t-nginx and nfhotel-api, the compose project t-hardened, and the
# networks t-ngnet and t-tsnet, and removes them at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker

set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_HOST="${REPO_HOST:-$REPO}"
W="$REPO/tests/output/nginx"
W_HOST="$REPO_HOST/tests/output/nginx"
readonly NGINX=nginx:1.27-alpine CURL=curlimages/curl:8.10.1 ECHO=hashicorp/http-echo:1.0
pass=0
fail=0
check() {
  if [[ "$2" == "$3" ]]; then
    echo "PASS  $1 ($3)"
    pass=$((pass + 1))
  else
    echo "FAIL  $1 (got $3, expected $2)"
    fail=$((fail + 1))
  fi
}
cleanup() {
  docker compose -p t-hardened -f "$REPO/deploy/nginx/docker-compose.yml" --env-file "$W/hardened.env" \
    down >/dev/null 2>&1
  docker rm -f t-nginx nfhotel-api >/dev/null 2>&1
  docker network rm t-ngnet t-tsnet >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT
cleanup
mkdir -p "$W/tls"
docker run --rm -v "$W_HOST/tls:/t" alpine/openssl req -x509 -newkey rsa:2048 -nodes -days 1 -subj /CN=t \
  -keyout /t/privkey.pem -out /t/fullchain.pem >/dev/null 2>&1
chmod 644 "$W"/tls/*
docker network create t-ngnet >/dev/null

start_nginx() {
  rm -rf "$W/snippets"
  cp -r "$REPO/deploy/nginx/snippets" "$W/snippets"
  [[ "$1" == route ]] && cp "$REPO/deploy/nginx/optional/api.conf" "$W/snippets/locations.d/api.conf"
  docker rm -f t-nginx >/dev/null 2>&1
  docker run -d --name t-nginx --network t-ngnet \
    -e NGINX_SERVER_NAME=_ -e NGINX_REDIRECT_PORT= -e NGINX_HSTS= -e UPSTREAM_HOST=nfhotel-web -e UPSTREAM_PORT=8080 \
    -e 'NGINX_ENVSUBST_FILTER=^(NGINX_SERVER_NAME|NGINX_REDIRECT_PORT|NGINX_HSTS|UPSTREAM_HOST|UPSTREAM_PORT)$' \
    -v "$REPO_HOST/deploy/nginx/templates/default.conf.template:/etc/nginx/templates/default.conf.template:ro" \
    -v "$REPO_HOST/deploy/nginx/templates/00-upgrade.conf.template:/etc/nginx/templates/00-upgrade.conf.template:ro" \
    -v "$REPO_HOST/deploy/nginx/templates/00-server-tokens.conf.template:/etc/nginx/templates/00-server-tokens.conf.template:ro" \
    -v "$W_HOST/snippets:/etc/nginx/snippets:ro" -v "$W_HOST/tls:/etc/nginx/tls:ro" "$NGINX" >/dev/null
  sleep 3
}
code() { docker run --rm --network "${2:-t-ngnet}" "$CURL" -sk -o /dev/null -w '%{http_code}' "$1"; }

echo "== no optional route"
start_nginx none
check "nginx runs" true "$(docker inspect -f '{{.State.Running}}' t-nginx)"
check "port 80 redirects" 301 "$(code http://t-nginx/)"
# The Server header names nginx without its version (MIL-033 criterion 6), on the redirect and on HTTPS.
server() { docker run --rm --network t-ngnet "$CURL" -skI "$1" | tr -d '\r' | sed -n 's/^[Ss]erver: //p'; }
check "port 80 names no nginx version" nginx "$(server http://t-nginx/)"
check "HTTPS names no nginx version" nginx "$(server https://t-nginx/)"
check "HTTPS proxies to the web application (absent here)" 502 "$(code https://t-nginx/)"
check "/api/ goes to the web application" 502 "$(code https://t-nginx/api/rooms)"

echo "== optional API route, no API running"
start_nginx route
check "nginx runs with the route and no API" true "$(docker inspect -f '{{.State.Running}}' t-nginx)"
check "port 80 still redirects" 301 "$(code http://t-nginx/)"
check "the web route is unchanged" 502 "$(code https://t-nginx/)"
check "/api/ is refused to an address outside the tailnet" 403 "$(code https://t-nginx/api/rooms)"

echo "== optional API route with an API, from a tailnet address"
docker network create --subnet 100.100.0.0/24 t-tsnet >/dev/null
docker network connect t-tsnet t-nginx
docker run -d --name nfhotel-api --network t-ngnet "$ECHO" -listen=:8080 -text=api-reached >/dev/null
sleep 2
ip="$(docker inspect -f '{{(index .NetworkSettings.Networks "t-tsnet").IPAddress}}' t-nginx)"
check "a tailnet address reaches the API" api-reached "$(docker run --rm --network t-tsnet "$CURL" -sk "https://$ip/api/rooms")"
check "another address is still refused" 403 "$(code https://t-nginx/api/rooms)"

echo "== the hardened compose service with a key as production has it"
# The key is 0600 and owned by uid 1000, as renew-certificate.sh installs it for the user of
# the checkout. Root in the container needs DAC_READ_SEARCH to open it.
mkdir -p "$W/hardened-tls"
# The image's entrypoint is openssl, so sh is named as the entrypoint.
docker run --rm --entrypoint sh -v "$W_HOST/tls:/s:ro" -v "$W_HOST/hardened-tls:/t" alpine/openssl -c \
  'cp /s/fullchain.pem /s/privkey.pem /t/ && chown 1000:1000 /t/*.pem && chmod 600 /t/privkey.pem'
cat >"$W/hardened.env" <<EOF
NGINX_ENV_FILE=$W/hardened.env
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
NGINX_BIND=127.0.0.1
NGINX_HTTP_PORT=18080
NGINX_HTTPS_PORT=18443
NGINX_INTERNAL_PORT=18081
NGINX_TLS_DIR=$W_HOST/hardened-tls
NFHOTEL_PROXY_NETWORK=t-ngnet
EOF
docker compose -p t-hardened -f "$REPO/deploy/nginx/docker-compose.yml" --env-file "$W/hardened.env" \
  up -d --wait --wait-timeout 90 >"$W/hardened.out" 2>&1
check "the hardened proxy turns healthy with a 0600 key of another user" healthy \
  "$(docker inspect -f '{{.State.Health.Status}}' t-hardened-nginx-1 2>/dev/null || echo missing)"
check "the hardened proxy redirects port 80" 301 "$(code http://t-hardened-nginx-1/)"
check "the hardened proxy serves HTTPS (no web application here)" 502 "$(code https://t-hardened-nginx-1/)"
if ((fail)); then
  { cat "$W/hardened.out" && docker logs --tail 20 t-hardened-nginx-1; } 2>&1 | sed 's/^/      /'
fi

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
