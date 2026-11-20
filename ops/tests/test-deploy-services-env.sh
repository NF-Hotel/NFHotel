#!/usr/bin/env bash
# Test of the settings that scripts/deploy-services.sh reads from the settings files: each secret
# becomes a file (ADR-0024), a value that ends with = (base64) stays whole, and no secret is
# exported to compose. A fake docker records API_TOKEN and POSTGRES_PASSWORD at `compose up`, so
# no container runs. The forwarder of the mail (MIL-088, ADR-0053) gets the host and the port of
# the relay from nfhotel-api.env and nothing else, and settings that cannot send refuse the deploy.
#
# Run it with the repository at /src or from a checkout:  bash tests/test-deploy-services-env.sh
# Exit:  0 every check passed; 1 a check failed
# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/repo/scripts" "$work/repo/deploy/postgres" "$work/repo/deploy/nfhotel-api" "$work/repo/deploy/nfhotel-web" "$work/bin"
cp -r "$root/scripts/." "$work/repo/scripts/"
token='abc+def/ghi0123456789ABCDEFGHIJKLMNOPQR='
password='pw+with/equals=='
printf 'POSTGRES_USER=u\nPOSTGRES_PASSWORD=%s\nPOSTGRES_DB=d\n' "$password" >"$work/repo/deploy/postgres/postgres.env"
printf '# a comment\nApi__Token=%s\nOther=1\n' "$token" >"$work/repo/deploy/nfhotel-api/nfhotel-api.env"
: >"$work/repo/deploy/nfhotel-web/nfhotel-web.env"
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "*) echo "\$PWD api_token=\${API_TOKEN-} pg=\${POSTGRES_PASSWORD-}" >>"$work/calls" ;;
  # deploy-services.sh waits for a healthy database and API before web.
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
chmod +x "$work/bin/docker"

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
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build postgres api web >"$work/out" 2>&1 || echo "note: the script exited non-zero: $(tail -1 "$work/out")"
d="$work/repo/deploy"
# The secrets are files (MIL-035, ADR-0024), and a value that ends with = (base64) stays whole.
[[ "$(cat "$d/nfhotel-api/secrets/Api__Token" 2>/dev/null)" == "$token" ]] &&
  ok "the API token is a file and keeps its trailing =" || ko "the API token file: $(tail -2 "$work/out")"
[[ "$(cat "$d/postgres/secrets/POSTGRES_PASSWORD" 2>/dev/null)" == "$password" ]] &&
  ok "the database password is a file and keeps its trailing =" || ko "the database password file"
grep -q "Password=$password\$" "$d/nfhotel-api/secrets/ConnectionStrings__HotelDatabase" 2>/dev/null &&
  ok "the connection string of the API is built from postgres.env" || ko "the connection string"
[[ "$(cat "$d/nfhotel-web/secrets/Api__Token" 2>/dev/null)" == "$token" ]] &&
  ok "the web application takes the API token from nfhotel-api.env" || ko "the API token of the web application"
grep -q "^Other=1$" "$d/nfhotel-api/secrets/settings.env" 2>/dev/null && ! grep -q Api__Token "$d/nfhotel-api/secrets/settings.env" &&
  ok "the settings for the container keep the other settings and leave the secret out" || ko "the settings without secrets"
! grep -q "api_token=[^ ]\|pg=[^ ]" "$work/calls" 2>/dev/null &&
  ok "no secret is exported to compose" || ko "a secret is exported to compose: $(cat "$work/calls")"

# The proxy (MIL-021 criterion 5): compose recreates it only when its configuration
# changed. deploy-services.sh passes a hash of the templates, snippets and settings, and no
# --force-recreate.
n="$work/repo/deploy/nginx"
mkdir -p "$n/templates" "$n/snippets" "$n/tls"
echo "server {}" >"$n/templates/default.conf.template"
echo "# snippet" >"$n/snippets/a.conf"
echo "NGINX_SERVER_NAME=_" >"$n/nginx.env"
echo cert >"$n/tls/fullchain.pem" && echo key >"$n/tls/privkey.pem"
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "*) echo "\$PWD hash=\${NGINX_CONFIG_HASH-} args=\$*" >>"$work/nginx-calls" ;;
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
nginx_hash() {
  : >"$work/nginx-calls"
  PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build nginx >"$work/out" 2>&1
  sed -n 's/.* hash=\([0-9a-f]*\) .*/\1/p' "$work/nginx-calls" | tail -1
}
first="$(nginx_hash)"
[[ "$first" =~ ^[0-9a-f]{16}$ ]] && ok "the proxy gets a configuration hash" || ko "the proxy gets a configuration hash"
[[ "$(nginx_hash)" == "$first" ]] && ok "the same configuration gives the same hash" || ko "the same configuration gives the same hash"
! grep -q -- --force-recreate "$work/nginx-calls" && ok "the proxy is not forced to recreate" || ko "the proxy is not forced to recreate"
# A release (--no-build) only pulls the digests it names: compose gets --no-build, so a failed pull is never a build
# on the VM (ADR-0007, MIL-065 task 4).
grep -q -- " --no-build" "$work/nginx-calls" && ! grep -q -- " --build" "$work/nginx-calls" &&
  ok "a release passes --no-build to compose and never --build" || ko "the build flags of a release: $(tail -1 "$work/nginx-calls")"
echo "server { listen 81; }" >"$n/templates/default.conf.template"
[[ "$(nginx_hash)" != "$first" ]] && ok "a changed template changes the hash" || ko "a changed template changes the hash"
echo "# other" >"$n/snippets/a.conf"
before="$(nginx_hash)"
echo "NGINX_SERVER_NAME=x" >"$n/nginx.env"
[[ "$(nginx_hash)" != "$before" ]] && ok "changed settings change the hash" || ko "changed settings change the hash"

# The tunnel of MIL-040: left out without cloudflared.env; with it, the token becomes a secret file.
mkdir -p "$d/cloudflared"
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "*) echo "\$PWD" >>"$work/tunnel-calls" ;;
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
: >"$work/tunnel-calls"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build tunnel >"$work/out-tunnel" 2>&1
grep -q "tunnel left out" "$work/out-tunnel" && ! grep -q cloudflared "$work/tunnel-calls" &&
  ok "without cloudflared.env the tunnel is left out" || ko "tunnel skipped: $(tail -2 "$work/out-tunnel")"
printf 'TUNNEL_TOKEN=tok-123\nNFHOTEL_PROXY_NETWORK=nfhotel-proxy\n' >"$d/cloudflared/cloudflared.env"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build tunnel >"$work/out-tunnel" 2>&1
[[ "$(cat "$d/cloudflared/secrets/TUNNEL_TOKEN" 2>/dev/null)" == tok-123 ]] && grep -q cloudflared "$work/tunnel-calls" &&
  ok "with cloudflared.env the token is a secret file and the tunnel starts" || ko "tunnel: $(tail -3 "$work/out-tunnel")"

# The database relay of MIL-048: left out without postgres-tailnet.env; with it, it starts and
# a failure only warns, so the hotel keeps running (ADR-0034).
mkdir -p "$d/postgres-tailnet"
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "*) echo "\$PWD" >>"$work/relay-calls"; [[ -f "$work/relay-fails" ]] && exit 1 ;;
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
: >"$work/relay-calls"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build dbrelay >"$work/out-relay" 2>&1
grep -q "dbrelay left out" "$work/out-relay" && ! grep -q postgres-tailnet "$work/relay-calls" &&
  ok "without postgres-tailnet.env the database relay is left out" || ko "relay skipped: $(tail -2 "$work/out-relay")"
printf 'NFHOTEL_TAILNET_IP=100.64.0.1
' >"$d/postgres-tailnet/postgres-tailnet.env"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build dbrelay >"$work/out-relay" 2>&1 &&
  grep -q postgres-tailnet "$work/relay-calls" && ok "with postgres-tailnet.env the database relay starts" || ko "relay: $(tail -3 "$work/out-relay")"
touch "$work/relay-fails"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build dbrelay >"$work/out-relay" 2>&1 &&
  grep -q "database relay did not turn healthy" "$work/out-relay" && ok "a failed relay warns and the deploy goes on" || ko "relay failure: $(tail -3 "$work/out-relay")"

# The forwarder of the mail of MIL-088 (ADR-0053 decision 4): it has no settings file and never
# env_file, it is left out without Smtp__Forwarder, it starts before the API with the host and the
# port of the relay and nothing else of nfhotel-api.env, and the deploy refuses settings that
# cannot send, naming the setting.
relay_file="$root/deploy/mail-relay/docker-compose.yml"
# shellcheck disable=SC2016 # the ${ in single quotes is a pattern, not an expansion
! grep -Eq '^[[:space:]]*env_file[[:space:]]*:' "$relay_file" &&
  [[ "$(sed -n '/^    environment:/,/^    [a-z_]*:/p' "$relay_file" | grep -Eo '^      [A-Za-z_]+:' | tr -d ' :' | paste -sd ' ' -)" == "MAIL_RELAY_HOST MAIL_RELAY_PORT" ]] &&
  [[ "$(grep -Eo '\$\{[A-Z_]+' "$relay_file" | sort -u | tr -d '${' | paste -sd ' ' -)" == "MAIL_RELAY_HOST MAIL_RELAY_PORT NFHOTEL_MAIL_NETWORK NFHOTEL_MAIL_OUT_NETWORK" ]] &&
  ok "the compose file of the forwarder has no env_file and takes only the host and the port of the relay" ||
  ko "the compose file of the forwarder: $(grep -nE 'env_file|\$\{' "$relay_file")"
mkdir -p "$d/mail-relay"
mail_password='app+pass/word=='
api_settings="$d/nfhotel-api/nfhotel-api.env"
# mail_settings <lines...>: nfhotel-api.env with the API token and the given lines.
mail_settings() {
  printf 'Api__Token=%s\nSmtp__UserName=hotel@example.com\nSmtp__Password=%s\n' "$token" "$mail_password" >"$api_settings"
  printf '%s\n' "$@" >>"$api_settings"
}
cat >"$work/bin/docker" <<FAKE
#!/bin/bash
case "\$*" in
  *" up "* | *" down"*)
    { echo "call \${PWD##*/} \$*"; env | sed 's/^/env /'; } >>"$work/mail-calls"
    [[ -f "$work/mail-fails" && "\$PWD" == */mail-relay ]] && exit 1 ;;
  "network inspect"*) [[ -f "$work/no-networks" ]] && exit 1 ;;
  "network create"*) echo "\$*" >>"$work/networks" ;;
  inspect\ -f*) echo "true healthy" ;;
esac
exit 0
FAKE
# deploy_mail <arguments...>: run the deploy with the fake, its output in $work/out-mail.
deploy_mail() {
  : >"$work/mail-calls"
  PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build "$@" >"$work/out-mail" 2>&1
}
# relay_env: the environment of the compose call of the forwarder.
relay_env() { sed -n '/^call mail-relay /,/^call /p' "$work/mail-calls" | grep '^env '; }

mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465'
deploy_mail mailrelay api
grep -q "mailrelay left out: no Smtp__Forwarder" "$work/out-mail" && ! grep -q '^call mail-relay' "$work/mail-calls" &&
  grep -q '^call nfhotel-api ' "$work/mail-calls" &&
  ok "without Smtp__Forwarder the forwarder is left out and the API is deployed" || ko "forwarder skipped: $(tail -3 "$work/out-mail")"

mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465' 'Smtp__Forwarder=mail-relay:1465' \
  'BookingVerification__Enabled=true'
deploy_mail mailrelay api
[[ "$(grep -o '^call [a-z-]*' "$work/mail-calls" | paste -sd ' ' -)" == "call mail-relay call nfhotel-api" ]] &&
  ok "with Smtp__Forwarder the forwarder starts before the API" ||
  ko "forwarder order: $(grep -o '^call [a-z-]*' "$work/mail-calls" | paste -sd ' ' -)"
grep '^call mail-relay ' "$work/mail-calls" | grep -q -- ' up ' && ! grep '^call mail-relay ' "$work/mail-calls" | grep -q -- '--env-file' &&
  relay_env | grep -qx 'env MAIL_RELAY_HOST=smtp.example.com' && relay_env | grep -qx 'env MAIL_RELAY_PORT=465' &&
  ok "the forwarder gets no settings file, and the host and the port of the relay" || ko "forwarder call: $(grep '^call mail-relay' "$work/mail-calls")"
! relay_env | grep -qF -e "$mail_password" -e "$token" -e "hotel@example.com" -e "mail-relay:1465" &&
  ok "no other value of nfhotel-api.env reaches the forwarder" || ko "a value of nfhotel-api.env reaches the forwarder"
[[ "$(cat "$d/nfhotel-api/secrets/Smtp__Password" 2>/dev/null)" == "$mail_password" ]] &&
  ! grep -q Smtp__Password "$d/nfhotel-api/secrets/settings.env" &&
  ok "Smtp__Password is a secret file of the API and not in its settings" || ko "the secret file Smtp__Password"

mail_settings 'Smtp__Port=465' 'Smtp__Forwarder=mail-relay:1465'
! deploy_mail mailrelay api && grep -q "Smtp__Forwarder is set .*, but Smtp__Host is not" "$work/out-mail" && ! grep -q '^call' "$work/mail-calls" &&
  ok "Smtp__Forwarder without Smtp__Host refuses the deploy and names the setting" || ko "no host: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Forwarder=mail-relay:1465'
! deploy_mail api && grep -q "Smtp__Forwarder is set .*, but Smtp__Port is not" "$work/out-mail" && ! grep -q '^call' "$work/mail-calls" &&
  ok "Smtp__Forwarder without Smtp__Port refuses the deploy of the API too" || ko "no port: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465' 'BookingVerification__Enabled=True'
! deploy_mail mailrelay api && grep -q "BookingVerification__Enabled is true .*, but Smtp__Forwarder is not set" "$work/out-mail" &&
  ! grep -q '^call' "$work/mail-calls" &&
  ok "the verification without Smtp__Forwarder refuses the deploy and names the setting" || ko "verification: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com:25 #' 'Smtp__Port=465' 'Smtp__Forwarder=mail-relay:1465'
! deploy_mail mailrelay && grep -q "Smtp__Host in .* is not a host name" "$work/out-mail" && ! grep -q '^call' "$work/mail-calls" &&
  ok "a Smtp__Host that is not a host name is refused" || ko "bad host: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=70000' 'Smtp__Forwarder=mail-relay:1465'
! deploy_mail mailrelay && grep -q "Smtp__Port in .* is not a port" "$work/out-mail" && ! grep -q '^call' "$work/mail-calls" &&
  ok "a Smtp__Port out of range is refused" || ko "bad port: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465' 'Smtp__Forwarder=mailrelay:1465'
deploy_mail mailrelay
grep -q "warning: Smtp__Forwarder in .* is not mail-relay:1465" "$work/out-mail" && grep -q '^call mail-relay ' "$work/mail-calls" &&
  ok "another Smtp__Forwarder warns and the forwarder starts" || ko "other forwarder: $(tail -2 "$work/out-mail")"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465' 'Smtp__Forwarder=mail-relay:1465'
touch "$work/mail-fails"
! deploy_mail mailrelay api && grep -q "deploying the forwarder of the mail failed" "$work/out-mail" && ! grep -q '^call nfhotel-api' "$work/mail-calls" &&
  ok "a forwarder that does not start fails the deploy before the API" || ko "failed forwarder: $(tail -2 "$work/out-mail")"
rm -f "$work/mail-fails"
deploy_mail --down mailrelay
grep -q '^call mail-relay compose down' "$work/mail-calls" && relay_env | grep -qx 'env MAIL_RELAY_HOST=smtp.example.com' &&
  ok "--down stops the forwarder" || ko "down: $(grep '^call' "$work/mail-calls" | paste -sd ' ' -)"
mail_settings 'Smtp__Host=smtp.example.com' 'Smtp__Port=465'
deploy_mail --down mailrelay
! grep -q '^call' "$work/mail-calls" && ok "--down leaves out a forwarder that is not set up" || ko "down without forwarder"
# The networks of the mail exist whether the forwarder runs or not: the API joins nfhotel-mail.
touch "$work/no-networks"
: >"$work/networks"
deploy_mail api
grep -qx 'network create --internal nfhotel-mail' "$work/networks" && grep -qx 'network create nfhotel-mail-out' "$work/networks" &&
  ok "the deploy creates nfhotel-mail internal and nfhotel-mail-out with a route out" || ko "networks: $(cat "$work/networks")"
rm -f "$work/no-networks"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
