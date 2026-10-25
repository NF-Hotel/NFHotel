#!/usr/bin/env bash
# Test of the settings that scripts/deploy-services.sh reads from the settings files: each secret
# becomes a file (ADR-0024), a value that ends with = (base64) stays whole, and no secret is
# exported to compose. A fake docker records API_TOKEN and POSTGRES_PASSWORD at `compose up`, so
# no container runs.
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

# The settings of the market analysis are read by its container as uid 1000 (MIL-023
# task 2), so a file at 0600, as the VM had it, must become readable (HTTP 422, 2026-10-24).
a="$work/repo/deploy/market-analysis"
mkdir -p "$a"
: >"$a/market-analysis.env"
printf '[llm]\nmodel = ""\n' >"$a/market-analysis.toml"
chmod 0600 "$a/market-analysis.toml"
PATH="$work/bin:$PATH" bash "$work/repo/scripts/deploy-services.sh" --no-build analysis >"$work/out-analysis" 2>&1
if [[ "$(stat -c %a "$a/market-analysis.toml")" == 644 ]]; then
  echo "PASS  the settings of the market analysis become readable by its container"
  pass=$((pass + 1))
else
  echo "FAIL  market-analysis.toml stays $(stat -c %a "$a/market-analysis.toml"): $(tail -2 "$work/out-analysis")"
  fail=$((fail + 1))
fi

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

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
