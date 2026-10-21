#!/usr/bin/env bash
# Tests of scripts/renew-certificate.sh (ADR-0009, RC-161 action 2): a refused host name,
# a new certificate installed into the folder the running nginx mounts and reloaded, an
# unchanged certificate that changes nothing, a configuration that nginx refuses (the old
# pair comes back), no running nginx, and a failed tailscale. Fakes stand in for
# tailscale and docker, so no certificate is issued and no container runs.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-renew-certificate.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (coreutils, diffutils)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
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

# The checkout, with the nginx settings, and the folder the running nginx mounts.
mkdir -p "$W/app/ops/deploy/nginx/tls" "$W/mounted" "$W/bin"
echo "NGINX_SERVER_NAME=nf-hotel.taildb84b5.ts.net" >"$W/app/ops/deploy/nginx/nginx.env"
useradd -M nfhotel 2>/dev/null
echo "cert v1" >"$W/mounted/fullchain.pem" && echo "key v1" >"$W/mounted/privkey.pem"
chown -R nfhotel:nfhotel "$W/mounted"

# tailscale cert writes FAKE_CERT and FAKE_KEY into the files it is given.
cat >"$W/bin/tailscale" <<'EOF'
#!/bin/bash
[[ "${FAKE_TS_FAIL:-}" == 1 ]] && exit 1
for a in "$@"; do
  case "$a" in
    --cert-file=*) echo "$FAKE_CERT" >"${a#--cert-file=}" ;;
    --key-file=*) echo "$FAKE_KEY" >"${a#--key-file=}" ;;
  esac
done
echo "$*" >>"$FAKE_LOG"
EOF
# docker: ps finds the nginx when FAKE_NGINX is set, inspect names the mounted folder,
# exec records the call and fails `nginx -t` when FAKE_NGINX_BAD is set.
cat >"$W/bin/docker" <<'EOF'
#!/bin/bash
case "$1" in
  ps) if [[ -n "${FAKE_NGINX:-}" ]]; then echo nginx123; fi ;;
  inspect) echo "$FAKE_MOUNT" ;;
  exec)
    echo "exec $*" >>"$FAKE_LOG"
    [[ "$*" == *"nginx -t"* && "${FAKE_NGINX_BAD:-}" == 1 ]] && exit 1
    exit 0 ;;
esac
EOF
chmod +x "$W/bin/"*
export PATH="$W/bin:$PATH" FAKE_LOG="$W/log" FAKE_MOUNT="$W/mounted" NFHOTEL_DIR="$W/app"
run() {
  : >"$W/log"
  bash "$REPO/scripts/renew-certificate.sh" >"$W/out" 2>&1
}

echo "NGINX_SERVER_NAME=_" >"$W/app/ops/deploy/nginx/nginx.env"
FAKE_NGINX=1 FAKE_CERT=x FAKE_KEY=y run && ko "accepted a host that is not a tailnet name" ||
  ok "refuses a host name that is not a tailnet name"
echo "NGINX_SERVER_NAME=nf-hotel.taildb84b5.ts.net" >"$W/app/ops/deploy/nginx/nginx.env"

FAKE_NGINX=1 FAKE_CERT="cert v2" FAKE_KEY="key v2" run
[[ $? == 0 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" && "$(cat "$W/mounted/privkey.pem")" == "key v2" ]] &&
  ok "a new certificate goes into the folder the running nginx mounts" || ko "install: $(cat "$W/out")"
grep -q "nf-hotel.taildb84b5.ts.net" "$W/log" && ok "tailscale is asked for the name of nginx.env" || ko "host name"
[[ "$(stat -c '%a %U' "$W/mounted/fullchain.pem")" == "644 nfhotel" && "$(stat -c '%a %U' "$W/mounted/privkey.pem")" == "600 nfhotel" ]] &&
  ok "certificate 0644 and key 0600, owned as the folder" || ko "modes: $(stat -c '%n %a %U' "$W/mounted/"*)"
[[ "$(cat "$W/mounted/fullchain.pem.previous")" == "cert v1" ]] && ok "the old pair is kept as .previous" || ko "previous"
grep -q "nginx -t" "$W/log" && grep -q "nginx -s reload" "$W/log" && ok "nginx is checked, then reloaded" || ko "reload: $(cat "$W/log")"

FAKE_NGINX=1 FAKE_CERT="cert v2" FAKE_KEY="key v2" run && grep -q "^unchanged" "$W/out" && ! grep -q exec "$W/log" &&
  ok "an unchanged certificate changes and reloads nothing" || ko "unchanged: $(cat "$W/out")"

FAKE_NGINX=1 FAKE_NGINX_BAD=1 FAKE_CERT="cert v3" FAKE_KEY="key v3" run
[[ $? == 1 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" && "$(cat "$W/mounted/privkey.pem")" == "key v2" ]] &&
  ! grep -q "reload" "$W/log" && ok "a refused configuration puts the old pair back and does not reload" ||
  ko "refused: $(cat "$W/out")"

FAKE_TS_FAIL=1 FAKE_NGINX=1 run
[[ $? == 1 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" ]] && ok "a failed tailscale changes nothing" || ko "tailscale fail"

FAKE_CERT="cert v4" FAKE_KEY="key v4" run
[[ $? == 0 && "$(cat "$W/app/ops/deploy/nginx/tls/fullchain.pem")" == "cert v4" ]] && grep -q "no nginx is running" "$W/out" &&
  ok "without a running nginx it installs into ops/deploy/nginx/tls" || ko "no nginx: $(cat "$W/out")"

[[ -z "$(find /tmp -maxdepth 1 -name 'tmp.*' -newer "$W/log" -type d)" ]] && ok "no temporary folder is left" || ko "temporary folder left"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
