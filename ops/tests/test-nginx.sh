#!/usr/bin/env bash
# Test of the nginx configuration (MIL-017, ADR-0011 decision 7): the HTTPS server with no
# optional route, with the optional API route and no API, and with an API reached from a
# tailnet address and refused from another address. Then the public listener of MIL-040 (port 8082,
# Cloudflare Tunnel): the site with the client's scheme and address, the back office only with the
# Access token header in any spelling of /admin (MIL-041, MIL-072), and only the two mobile routes
# of the API. The public listener serves security.txt from the folder well-known, with its type and
# headers, and redirects /security.txt to it, while every other path of /.well-known/ and the HTTPS
# server of the tailnet still go to the web application (MIL-084, ADR-0048). The public listener refuses a
# body over 64 kilobytes with 413 while the HTTPS server of the tailnet keeps 1 megabyte, and in the loaded
# configuration every location waits 60 seconds and only the web socket of the pages, /_blazor on both
# servers, an hour (MIL-086 decisions 4 and 7). Last, the hardened
# service of deploy/nginx/docker-compose.yml (MIL-023 task 2) starts and turns healthy with a key as
# production has it: 0600 and owned by another user than root, and serves security.txt from its mount.
#
# Usage: test-nginx.sh
# Env:   REPO_HOST  the repository path as the Docker daemon sees it (default: this
#                   checkout; on Windows give C:/... for Docker Desktop)
# It builds the image nfhotel-nginx:local from deploy/nginx (the proxy of production, ADR-0040) and
# starts the containers t-nginx, nfhotel-web and nfhotel-api, the compose project t-hardened, and the
# networks t-ngnet, t-tsnet and t-apinet, and removes them, not the image, at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker

set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_HOST="${REPO_HOST:-$REPO}"
W="$REPO/tests/output/nginx"
W_HOST="$REPO_HOST/tests/output/nginx"
readonly NGINX=nfhotel-nginx:local CURL=curlimages/curl:8.10.1 ECHO=hashicorp/http-echo:1.0
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
  docker rm -f t-nginx nfhotel-web nfhotel-api >/dev/null 2>&1
  docker network rm t-ngnet t-tsnet t-apinet >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT
cleanup
mkdir -p "$W/tls"
docker run --rm -v "$W_HOST/tls:/t" alpine/openssl req -x509 -newkey rsa:2048 -nodes -days 1 -subj /CN=t \
  -keyout /t/privkey.pem -out /t/fullchain.pem >/dev/null 2>&1
chmod 644 "$W"/tls/*
docker network create t-ngnet >/dev/null
# The proxy of production (ADR-0040): the pinned nginx image with the fixes of its Alpine
# packages, built under the name that the compose file starts without a release digest, so the
# hardened service below runs this image too.
docker build -q -t "$NGINX" "$REPO/deploy/nginx" >/dev/null || {
  echo "FAIL  the proxy image of deploy/nginx does not build"
  exit 1
}

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
    -v "$REPO_HOST/deploy/nginx/templates/00-forwarded.conf.template:/etc/nginx/templates/00-forwarded.conf.template:ro" \
    -v "$REPO_HOST/deploy/nginx/templates/public.conf.template:/etc/nginx/templates/public.conf.template:ro" \
    -v "$REPO_HOST/deploy/nginx/well-known:/etc/nginx/well-known:ro" \
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

echo "== the public listener of Cloudflare Tunnel (MIL-040)"
# A second nginx stands in for the web application and answers with the headers it got.
mkdir -p "$W/web"
# shellcheck disable=SC2016 # nginx variables, written as they are into its configuration
printf 'server { listen 8080; location / { return 200 "$http_x_forwarded_proto $http_x_real_ip"; } }' >"$W/web/default.conf"
docker run -d --name nfhotel-web --network t-ngnet -v "$W_HOST/web:/etc/nginx/conf.d:ro" "$NGINX" >/dev/null
sleep 2
# The public listener speaks plain HTTP on purpose: in production only cloudflared reaches it, inside the VM.
readonly PUBLIC=http://t-nginx:8082 # NOSONAR: an internal Docker hop, as in production (ADR-0028)
public() {
  docker run --rm --network t-ngnet "$CURL" -s -o /dev/null -w '%{http_code}' "$PUBLIC$1" # NOSONAR: see PUBLIC
}
# A request with the Access token header, as the validating route of the tunnel forwards it after Access.
with_token() {
  local -r access_header='Cf-Access-Jwt-Assertion: eyJ.test.token' client_header='CF-Connecting-IP: 203.0.113.7'
  docker run --rm --network t-ngnet "$CURL" -s -H "$access_header" -H "$client_header" "$PUBLIC$1" # NOSONAR: see PUBLIC
}
check "the public site reaches the web application as HTTPS, with the client's address" "https 203.0.113.7" \
  "$(docker run --rm --network t-ngnet "$CURL" -s -H 'CF-Connecting-IP: 203.0.113.7' "$PUBLIC/book")"
check "the tailnet server still reports its own scheme" "https" \
  "$(docker run --rm --network t-ngnet "$CURL" -sk https://t-nginx/ | cut -d' ' -f1)"
check "the back office without the Access token is refused" 404 "$(public /admin/login)"
check "the back office root without the Access token is refused" 404 "$(public /admin)"
check "the back office with the Access token reaches the web application" "https 203.0.113.7" \
  "$(with_token /admin/login)"
# Every spelling of /admin (MIL-072): the web application matches its routes without regard to letter case, so the
# guard must too, a percent-encoded letter included.
for spelling in /ADMIN/login /Admin/login /aDmin/login /%41DMIN/login; do
  check "the back office spelt $spelling without the Access token is refused" 404 "$(public "$spelling")"
done
check "the back office spelt /ADMIN/login with the Access token reaches the web application" "https 203.0.113.7" \
  "$(with_token /ADMIN/login)"
check "the mobile sign-in route reaches the API" api-reached \
  "$(docker run --rm --network t-ngnet "$CURL" -s -X POST "$PUBLIC/mobile-api/api/auth/login")"
check "the mobile rooms route reaches the API" 200 "$(public /mobile-api/api/rooms/3)"
check "another API route is not public" 404 "$(public /mobile-api/api/bookings)"
check "the API itself is not on the public site" 404 "$(public /mobile-api/)"

echo "== the size and the time limits of a request (MIL-086)"
# body <bytes> <address>: the status of a POST of that many bytes; the stand-in answers every request with 200.
body() {
  head -c "$1" /dev/zero | docker run --rm -i --network t-ngnet "$CURL" -sk -o /dev/null -w '%{http_code}' \
    -X POST -H 'Content-Type: application/octet-stream' --data-binary @- "$2"
}
check "the public listener takes a body of 64 kilobytes" 200 "$(body 65536 "$PUBLIC/book")"
check "the public listener refuses a body of 64 kilobytes and one byte" 413 "$(body 65537 "$PUBLIC/book")"
check "the HTTPS server of the tailnet keeps its limit of 1 megabyte" 200 "$(body 65537 https://t-nginx/book)"
check "the web socket of the pages reaches the web application with the client's scheme and address" \
  "https 203.0.113.7" "$(docker run --rm --network t-ngnet "$CURL" -s -H 'CF-Connecting-IP: 203.0.113.7' \
    "$PUBLIC/_blazor?id=t")"
check "the web socket of the pages reaches the web application on the HTTPS server of the tailnet" https \
  "$(docker run --rm --network t-ngnet "$CURL" -sk 'https://t-nginx/_blazor?id=t' | cut -d' ' -f1)"
# The configuration that nginx loaded (nginx -T), with the name of each file before its lines.
loaded="$(docker exec t-nginx nginx -T 2>/dev/null)"
check "the shared proxy settings wait 60 seconds" "proxy_read_timeout 60s; proxy_send_timeout 60s;" \
  "$(awk '/^# configuration file /{file=$4; next} file == "/etc/nginx/snippets/proxy.conf:" && /_timeout/' \
    <<<"$loaded" | paste -sd' ')"
check "only the location of the web socket waits an hour" /etc/nginx/snippets/blazor.conf \
  "$(awk '/^# configuration file /{file=$4} /_timeout 1h;/{print file}' <<<"$loaded" | tr -d ':' | sort -u | paste -sd' ')"
check "both servers of the web application have the location of the web socket" 2 \
  "$(grep -c 'include /etc/nginx/snippets/blazor.conf;' <<<"$loaded")"

echo "== security.txt on the public listener (MIL-084, ADR-0048)"
readonly SECURITY_TXT="$REPO/deploy/nginx/well-known/security.txt"
# answer <path> [curl option...]: the status line and the headers of the public listener's answer, without CR.
answer() {
  local path="$1"
  shift
  docker run --rm --network t-ngnet "$CURL" -s -o /dev/null -D - "$@" "$PUBLIC$path" | tr -d '\r' # NOSONAR: see PUBLIC
}
# status <answer>: the status code of an answer; field <name> <answer>: the value of one of its headers.
status() { awk 'NR == 1 { print $2 }' <<<"$1"; }
field() { sed -n "s/^$1: //Ip" <<<"$2" | head -n 1; }
# guest <path>: the body of the public listener's answer to a guest at 203.0.113.7.
guest() {
  docker run --rm --network t-ngnet "$CURL" -s -H 'CF-Connecting-IP: 203.0.113.7' "$PUBLIC$1" # NOSONAR: see PUBLIC
}
# digest: the SHA-256 of standard input.
digest() { sha256sum | cut -d' ' -f1; }
file_answer="$(answer /.well-known/security.txt)"
check "security.txt answers 200 without the Access token" 200 "$(status "$file_answer")"
check "security.txt is text/plain in UTF-8 (RFC 9116, section 3)" "text/plain; charset=utf-8" \
  "$(field Content-Type "$file_answer")"
check "security.txt is sent with nosniff (RFC 8615, section 4.2)" nosniff "$(field X-Content-Type-Options "$file_answer")"
check "security.txt is sent with a content security policy that allows nothing" \
  "default-src 'none'; frame-ancestors 'none'" "$(field Content-Security-Policy "$file_answer")"
check "security.txt is sent with the Strict-Transport-Security of the web application" max-age=2592000 \
  "$(field Strict-Transport-Security "$file_answer")"
check "the body of security.txt is the file of the repository" "$(digest <"$SECURITY_TXT")" \
  "$(guest /.well-known/security.txt | digest)"
check "HEAD of security.txt answers 200" 200 "$(status "$(answer /.well-known/security.txt -I)")"
redirect="$(answer /security.txt)"
check "/security.txt answers 301 (RFC 9116, section 3)" 301 "$(status "$redirect")"
check "the redirect names security.txt by a relative address" /.well-known/security.txt "$(field Location "$redirect")"
check "the redirect is sent with the Strict-Transport-Security of the web application" max-age=2592000 \
  "$(field Strict-Transport-Security "$redirect")"
check "another path of /.well-known/ reaches the web application" "https 203.0.113.7" \
  "$(guest /.well-known/change-password)"
check "security.txt spelt in capitals reaches the web application" "https 203.0.113.7" \
  "$(guest /.well-known/SECURITY.TXT)"
check "the HTTPS server of the tailnet hands security.txt to the web application" https \
  "$(docker run --rm --network t-ngnet "$CURL" -sk https://t-nginx/.well-known/security.txt | cut -d' ' -f1)"
# The stand-in leaves, so the hardened proxy below finds no web application, as before.
docker rm -f nfhotel-web >/dev/null 2>&1

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
NGINX_BIND=127.0.0.1
NGINX_HTTP_PORT=18080
NGINX_HTTPS_PORT=18443
NGINX_TLS_DIR=$W_HOST/hardened-tls
NFHOTEL_PROXY_NETWORK=t-ngnet
NFHOTEL_API_NETWORK=t-apinet
EOF
docker network create --internal t-apinet >/dev/null
docker compose -p t-hardened -f "$REPO/deploy/nginx/docker-compose.yml" --env-file "$W/hardened.env" \
  up -d --wait --wait-timeout 90 >"$W/hardened.out" 2>&1
check "the hardened proxy turns healthy with a 0600 key of another user" healthy \
  "$(docker inspect -f '{{.State.Health.Status}}' t-hardened-nginx-1 2>/dev/null || echo missing)"
check "the hardened proxy redirects port 80" 301 "$(code http://t-hardened-nginx-1/)"
check "the hardened proxy serves HTTPS (no web application here)" 502 "$(code https://t-hardened-nginx-1/)"
check "the hardened proxy serves security.txt from its read-only mount" "$(digest <"$SECURITY_TXT")" \
  "$(docker run --rm --network t-ngnet "$CURL" -s http://t-hardened-nginx-1:8082/.well-known/security.txt | digest)"
if ((fail)); then
  { cat "$W/hardened.out" && docker logs --tail 20 t-hardened-nginx-1; } 2>&1 | sed 's/^/      /'
fi

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
