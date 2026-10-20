#!/usr/bin/env bash
# Test of the nginx configuration (MIL-017, ADR-0011 decision 7): the HTTPS server with no
# optional route, with the optional API route and no API, and with an API reached from a
# tailnet address and refused from another address.
#
# Usage: test-nginx.sh
# Env:   REPO_HOST  the repository path as the Docker daemon sees it (default: this
#                   checkout; on Windows give C:/... for Docker Desktop)
# It starts the containers t-nginx and nfhotel-api and the networks t-ngnet and t-tsnet,
# and removes them at the end.
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
    -v "$W_HOST/snippets:/etc/nginx/snippets:ro" -v "$W_HOST/tls:/etc/nginx/tls:ro" "$NGINX" >/dev/null
  sleep 3
}
code() { docker run --rm --network "${2:-t-ngnet}" "$CURL" -sk -o /dev/null -w '%{http_code}' "$1"; }

echo "== no optional route"
start_nginx none
check "nginx runs" true "$(docker inspect -f '{{.State.Running}}' t-nginx)"
check "port 80 redirects" 301 "$(code http://t-nginx/)"
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

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
