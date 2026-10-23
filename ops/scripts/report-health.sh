#!/usr/bin/env bash
# Report the health of the long-running containers on nf-hotel to the monitoring (ADR-0020,
# MIL-023 task 9). The update command of CD installs it as /usr/local/sbin/nfhotel-report-health,
# and nfhotel-report-health.timer runs it every minute. Alloy reads the file it writes, and
# Prometheus alerts (ContainerUnhealthy) when a value is 0.
#
# A container counts when its restart policy is not "no", the services that should always
# run; a one-off container, such as a migration run, is left out. Its value is 1 when it
# runs and its health check, if it has one, is not "unhealthy" ("starting" counts as 1, so
# a deploy raises no alert), and 0 otherwise, a stopped container included.
#
# Usage: report-health.sh
# Env:   NFHOTEL_METRICS_DIR (default /var/lib/nfhotel-metrics), NFHOTEL_DOCKER (default
#        docker; for the tests)
# Exit:  0 written; 1 docker or the write failed (the old file stays)
# Needs: bash 4+, docker

set -euo pipefail

dir="${NFHOTEL_METRICS_DIR:-/var/lib/nfhotel-metrics}"
docker="${NFHOTEL_DOCKER:-docker}"

die() {
  echo "error: $*" >&2
  exit 1
}

install -d -m 0755 "$dir" || die "cannot make $dir"
# The output is read into a variable first: a failure inside <(...) would not stop the script.
out="$("$docker" ps --all --quiet)" || die "docker ps failed"
lines=()
if [[ -n "$out" ]]; then
  mapfile -t ids <<<"$out"
  out="$("$docker" inspect --format \
    '{{.Name}} {{.HostConfig.RestartPolicy.Name}} {{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' \
    "${ids[@]}")" || die "docker inspect failed"
  mapfile -t lines <<<"$out"
fi

tmp="$(mktemp "$dir/.containers.XXXXXX")" || die "cannot write in $dir"
trap 'rm -f "$tmp"' EXIT
{
  echo '# HELP nfhotel_container_healthy 1 when the container runs and is not unhealthy, else 0.'
  echo '# TYPE nfhotel_container_healthy gauge'
  for line in "${lines[@]}"; do
    read -r name policy state health <<<"$line"
    [[ "$policy" == "" || "$policy" == "no" ]] && continue
    value=0
    [[ "$state" == "running" && "$health" != "unhealthy" ]] && value=1
    printf 'nfhotel_container_healthy{container="%s"} %s\n' "${name#/}" "$value"
  done
} >"$tmp"
chmod 0644 "$tmp"
mv "$tmp" "$dir/containers.prom" || die "cannot write $dir/containers.prom"
