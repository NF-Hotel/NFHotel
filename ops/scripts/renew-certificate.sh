#!/usr/bin/env bash
# Renew the TLS certificate of the tailnet name with `tailscale cert` and hand it to the
# running nginx (ADR-0009, ADR-0018, RC-161 action 2). The update command of CD installs
# it as /usr/local/sbin/nfhotel-cert-renew, and nfhotel-cert-renew.timer runs it every day.
#
# It replaces the renewal that was set up on nf-hotel by hand, which wrote to
# /etc/nginx/certs/ and reloaded the container nfhotel-nginx of the stack from before
# the reconciliation; after that container stopped, the renewal failed (2026-10-21).
#
#   1. find the host name: the first tailnet name in NGINX_SERVER_NAME of
#      ops/deploy/nginx/nginx.env (nginx's server_name may list several), or else the first
#      of CertDomains in `tailscale status --json`, the names tailscale cert can issue for
#      this machine; production has server_name _, which matches any name
#   2. find the running nginx by its compose labels (project nfhotel-nginx, service
#      nginx) and the folder it mounts as /etc/nginx/tls; without a running nginx, the
#      folder ops/deploy/nginx/tls of the checkout
#   3. let tailscale write the certificate and key into a temporary folder
#   4. when they equal fullchain.pem and privkey.pem, stop: nothing changes
#   5. otherwise keep the old pair as *.previous, install the new one (certificate 0644,
#      key 0600, the owner of the folder), check the nginx configuration and reload it;
#      when the check fails, put the old pair back and exit 1
#   6. on success, changed or not, report the time to the monitoring (cert_renew.prom in
#      NFHOTEL_METRICS_DIR, ADR-0020)
#
# Usage: renew-certificate.sh
# Env:   NFHOTEL_DIR (default /opt/nfhotel), NFHOTEL_TAILSCALE (default tailscale; for
#        the tests), NFHOTEL_METRICS_DIR (default /var/lib/nfhotel-metrics)
# Exit:  0 renewed, unchanged, or installed with no nginx running; 1 a step failed
# Needs: bash 4+, root, tailscale, docker, install, cmp

set -euo pipefail

app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
tailscale="${NFHOTEL_TAILSCALE:-tailscale}"
settings="$app_dir/ops/deploy/nginx/nginx.env"

die() {
  echo "error: $*" >&2
  exit 1
}

# report_success <task>: write the time of this success to <task>.prom in the folder that
# Alloy reads (ADR-0020, MIL-023 task 9); Prometheus alerts when it grows old. The helper
# is kept in this file because the file is installed alone. A failed write only warns.
report_success() {
  local dir="${NFHOTEL_METRICS_DIR:-/var/lib/nfhotel-metrics}" file
  file="$dir/$1.prom"
  {
    install -d -m 0755 "$dir" &&
      printf '%s\n%s\n%s{task="%s"} %s\n' \
        '# HELP nfhotel_task_last_success_timestamp_seconds When the task last succeeded.' \
        '# TYPE nfhotel_task_last_success_timestamp_seconds gauge' \
        nfhotel_task_last_success_timestamp_seconds "$1" "$(date +%s)" >"$file.tmp" &&
      chmod 0644 "$file.tmp" && mv "$file.tmp" "$file"
  } 2>/dev/null || echo "warning: cannot write the report $file" >&2
}

# The renewal used to read only NGINX_SERVER_NAME, but production sets it to _ (any name), so
# it found no name and failed every night from 2026-10-21 until the monitoring reported it
# (TaskReportMissing, 2026-10-24, RC-187). The line is read as deploy-services.sh reads it: a
# carriage return and spaces around the names are not part of them.
tailnet_name='^[a-z0-9-]+\.[a-z0-9.-]+\.ts\.net$'
names="$(sed -n 's/^NGINX_SERVER_NAME=//p' "$settings" 2>/dev/null | tail -n 1 | tr -d '\r')" || names=""
host=""
for name in $names; do
  if [[ "$name" =~ $tailnet_name ]]; then
    host="$name"
    break
  fi
done
if [[ -z "$host" ]]; then
  host="$("$tailscale" status --json 2>/dev/null | tr -d ' \t\r\n' |
    sed -n 's/.*"CertDomains":\["\([^"]*\)".*/\1/p')" || host=""
  [[ "$host" =~ $tailnet_name ]] ||
    die "no tailnet name: NGINX_SERVER_NAME in $settings is '$names', and tailscale lists no CertDomains (is HTTPS enabled for the tailnet?)"
fi

nginx="$(docker ps --quiet --filter label=com.docker.compose.project=nfhotel-nginx \
  --filter label=com.docker.compose.service=nginx | head -n 1)" || die "docker ps failed"
tls_dir="$app_dir/ops/deploy/nginx/tls"
if [[ -n "$nginx" ]]; then
  tls_dir="$(docker inspect --format \
    '{{range .Mounts}}{{if eq .Destination "/etc/nginx/tls"}}{{.Source}}{{end}}{{end}}' "$nginx")" ||
    die "docker inspect failed for $nginx"
  [[ -n "$tls_dir" ]] || die "the running nginx $nginx mounts no /etc/nginx/tls"
fi
[[ -d "$tls_dir" ]] || die "$tls_dir is not a folder"

umask 077
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
"$tailscale" cert --cert-file="$work/fullchain.pem" --key-file="$work/privkey.pem" "$host" ||
  die "tailscale cert failed for $host"

if cmp -s "$work/fullchain.pem" "$tls_dir/fullchain.pem" && cmp -s "$work/privkey.pem" "$tls_dir/privkey.pem"; then
  echo "unchanged: the certificate of $host in $tls_dir is current"
  report_success cert_renew
  exit 0
fi

owner="$(stat -c '%U:%G' "$tls_dir")"
for f in fullchain.pem privkey.pem; do
  if [[ -f "$tls_dir/$f" ]]; then cp -p "$tls_dir/$f" "$tls_dir/$f.previous"; fi
done
install -o "${owner%%:*}" -g "${owner##*:}" -m 0644 "$work/fullchain.pem" "$tls_dir/fullchain.pem"
install -o "${owner%%:*}" -g "${owner##*:}" -m 0600 "$work/privkey.pem" "$tls_dir/privkey.pem"
echo "installed: a new certificate of $host in $tls_dir"

if [[ -z "$nginx" ]]; then
  echo "warning: no nginx is running; it reads the new certificate when it starts" >&2
  report_success cert_renew
  exit 0
fi
if ! docker exec "$nginx" nginx -t; then
  for f in fullchain.pem privkey.pem; do
    if [[ -f "$tls_dir/$f.previous" ]]; then cp -p "$tls_dir/$f.previous" "$tls_dir/$f"; fi
  done
  die "nginx refused the new certificate; the previous one is back in $tls_dir"
fi
docker exec "$nginx" nginx -s reload || die "nginx did not reload"
echo "reloaded: nginx serves the new certificate of $host"
report_success cert_renew
