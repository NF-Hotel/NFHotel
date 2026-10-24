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
# status --json lists the CertDomains as tailscale prints them (FAKE_CERT_DOMAIN, empty: none).
if [[ "$1" == status ]]; then
  printf '{\n  "Self": {\n    "DNSName": "nf-hotel.taildb84b5.ts.net."\n  },\n  "CertDomains": [\n'
  [[ -n "${FAKE_CERT_DOMAIN-nf-hotel.taildb84b5.ts.net}" ]] && printf '    "%s"\n' "${FAKE_CERT_DOMAIN-nf-hotel.taildb84b5.ts.net}"
  printf '  ]\n}\n'
  exit 0
fi
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
export PATH="$W/bin:$PATH" FAKE_LOG="$W/log" FAKE_MOUNT="$W/mounted" NFHOTEL_DIR="$W/app" NFHOTEL_METRICS_DIR="$W/metrics"
report="$W/metrics/cert_renew.prom"
run() {
  : >"$W/log"
  rm -f "$report"
  bash "$REPO/scripts/renew-certificate.sh" >"$W/out" 2>&1
}

# Production: server_name _ (any name). The name comes from tailscale's CertDomains.
printf 'NGINX_SERVER_NAME=_\r\n' >"$W/app/ops/deploy/nginx/nginx.env"
FAKE_NGINX=1 FAKE_CERT="cert v1" FAKE_KEY="key v1" run && grep -q "cert .*nf-hotel.taildb84b5.ts.net$" "$W/log" &&
  ok "with server_name _, the name comes from tailscale's CertDomains" || ko "server_name _: $(cat "$W/out")"
FAKE_CERT_DOMAIN="" FAKE_NGINX=1 FAKE_CERT=x FAKE_KEY=y run && ko "renewed with no tailnet name anywhere" ||
  { grep -q "tailscale lists no CertDomains" "$W/out" && ok "with no tailnet name anywhere it refuses, and says why"; } ||
  ko "no name: $(cat "$W/out")"
FAKE_CERT_DOMAIN="evil.example.com" FAKE_NGINX=1 FAKE_CERT=x FAKE_KEY=y run && ko "accepted a CertDomain that is not a tailnet name" ||
  ok "a CertDomain that is not a tailnet name is refused"

# The forms of the line that the old reader missed: a file saved on Windows, a space after the
# name, and several names as nginx's server_name allows. Each must find the tailnet name.
for line in $'NGINX_SERVER_NAME=nf-hotel.taildb84b5.ts.net\r' 'NGINX_SERVER_NAME=nf-hotel.taildb84b5.ts.net ' \
  'NGINX_SERVER_NAME=localhost nf-hotel.taildb84b5.ts.net 100.72.194.127'; do
  printf '%s\n' "$line" >"$W/app/ops/deploy/nginx/nginx.env"
  FAKE_NGINX=1 FAKE_CERT="cert v1" FAKE_KEY="key v1" run && grep -q "cert .*nf-hotel.taildb84b5.ts.net$" "$W/log" &&
    ok "finds the tailnet name in: $(printf '%q' "$line")" || ko "$(printf '%q' "$line"): $(cat "$W/out")"
done
echo "NGINX_SERVER_NAME=nf-hotel.taildb84b5.ts.net" >"$W/app/ops/deploy/nginx/nginx.env"

FAKE_NGINX=1 FAKE_CERT="cert v2" FAKE_KEY="key v2" run
[[ $? == 0 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" && "$(cat "$W/mounted/privkey.pem")" == "key v2" ]] &&
  ok "a new certificate goes into the folder the running nginx mounts" || ko "install: $(cat "$W/out")"
grep -q "nf-hotel.taildb84b5.ts.net" "$W/log" && ok "tailscale is asked for the name of nginx.env" || ko "host name"
[[ "$(stat -c '%a %U' "$W/mounted/fullchain.pem")" == "644 nfhotel" && "$(stat -c '%a %U' "$W/mounted/privkey.pem")" == "600 nfhotel" ]] &&
  ok "certificate 0644 and key 0600, owned as the folder" || ko "modes: $(stat -c '%n %a %U' "$W/mounted/"*)"
[[ "$(cat "$W/mounted/fullchain.pem.previous")" == "cert v1" ]] && ok "the old pair is kept as .previous" || ko "previous"
grep -q "nginx -t" "$W/log" && grep -q "nginx -s reload" "$W/log" && ok "nginx is checked, then reloaded" || ko "reload: $(cat "$W/log")"
grep -Eq '^nfhotel_task_last_success_timestamp_seconds\{task="cert_renew"\} [0-9]{10}$' "$report" &&
  [[ "$(stat -c '%a' "$report")" == 644 ]] && ok "a renewal reports its success to the monitoring" || ko "report: $(cat "$report" 2>&1)"

FAKE_NGINX=1 FAKE_CERT="cert v2" FAKE_KEY="key v2" run && grep -q "^unchanged" "$W/out" && ! grep -q exec "$W/log" &&
  ok "an unchanged certificate changes and reloads nothing" || ko "unchanged: $(cat "$W/out")"
[[ -s "$report" ]] && ok "an unchanged certificate is a success too" || ko "no report when unchanged"

FAKE_NGINX=1 FAKE_NGINX_BAD=1 FAKE_CERT="cert v3" FAKE_KEY="key v3" run
[[ $? == 1 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" && "$(cat "$W/mounted/privkey.pem")" == "key v2" ]] &&
  ! grep -q "reload" "$W/log" && ok "a refused configuration puts the old pair back and does not reload" ||
  ko "refused: $(cat "$W/out")"
[[ ! -e "$report" ]] && ok "a failed renewal reports nothing" || ko "a failure was reported as a success"

FAKE_TS_FAIL=1 FAKE_NGINX=1 run
[[ $? == 1 && "$(cat "$W/mounted/fullchain.pem")" == "cert v2" ]] && ok "a failed tailscale changes nothing" || ko "tailscale fail"

FAKE_CERT="cert v4" FAKE_KEY="key v4" run
[[ $? == 0 && "$(cat "$W/app/ops/deploy/nginx/tls/fullchain.pem")" == "cert v4" ]] && grep -q "no nginx is running" "$W/out" &&
  ok "without a running nginx it installs into ops/deploy/nginx/tls" || ko "no nginx: $(cat "$W/out")"

[[ -z "$(find /tmp -maxdepth 1 -name 'tmp.*' -newer "$W/log" -type d)" ]] && ok "no temporary folder is left" || ko "temporary folder left"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
