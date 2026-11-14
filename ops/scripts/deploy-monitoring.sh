#!/usr/bin/env bash
# Start or update the monitoring of NF Hotel in its LXC container (ADR-0020, MIL-023 task 7).
# The pull deploy of ADR-0044 (scripts/pull-monitoring.sh, MIL-051) runs it as root in the
# checkout on the LXC after each change of the monitoring on main; S03 runs it by hand after
# scripts/create-monitoring-lxc.sh, and in an emergency.
#
#   1. refuse unless monitoring.env names MONITORING_BIND as an address of this host
#   2. refuse unless the three secret files exist, are not empty, and no group or other
#      user can read them; the GitHub token of the scan exporter (MIL-053) is optional
#   3. check the configuration with the tools of the pinned images: promtool check
#      config, promtool test rules, amtool check-config, loki -verify-config
#   4. give each secret file to the user its container runs as (0400), the folder 0711, and
#      make the report folder that the scan exporter serves (MIL-051)
#   5. docker compose up -d --wait, so it returns once every service is running; each service
#      whose configuration folder changed is recreated, so it reads the new files
#   6. check that Prometheus, Alertmanager, Loki, Grafana and the blackbox exporter answer their
#      ready endpoints, and that Prometheus has loaded every rule of the rule files (MIL-051)
#   7. check that every container of the monitoring keeps the rights of its profile in
#      ops/security/container-profiles.tsv (scripts/check-container-runtime.sh, MIL-080)
#
# Usage: deploy-monitoring.sh [--apply]
#   --apply  start or update the services; without it the script only checks (a dry run)
# Exit:  0 checked (or started); 1 a check or the start failed; 2 usage error
# Needs: bash 4+, root, docker with the compose plugin, ip (iproute2), curl
# Environment: MONITORING_REPORT_DIR, the report folder (default
#   /var/lib/nfhotel-monitoring/metrics; the stack test sets its own)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
dir="$ROOT/deploy/monitoring"
apply=0

case "${1:-}" in
  --apply) apply=1 ;;
  "") ;;
  -h | --help)
    usage
    exit 0
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac

[[ -f "$dir/monitoring.env" ]] || die "$dir/monitoring.env is missing; copy monitoring.env.example and fill it in"
bind="$(sed -n 's/^MONITORING_BIND=\([0-9.]*\)$/\1/p' "$dir/monitoring.env" | tail -n 1)"
[[ "$bind" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]] || die "MONITORING_BIND in monitoring.env is not an IPv4 address"
# grep reads all of the input: with grep -q, ip could get SIGPIPE, and pipefail made the check
# fail at random on a host that had the address (2026-10-24).
ip -4 -o addr show | grep " $bind/" >/dev/null || die "$bind is not an address of this host (tailscale ip -4)"

for f in discord-webhook-url healthchecks-watchdog-url grafana-admin-password; do
  file="$dir/secrets/$f"
  [[ -s "$file" ]] || die "$file is missing or empty (see secrets.example/README.md)"
  ((8#$(stat -c %a "$file") & 8#077)) && die "$file must be readable by its owner only (chmod 600)"
done
[[ "$(head -c 33 "$dir/secrets/discord-webhook-url")" == "https://discord.com/api/webhooks/" ]] ||
  die "secrets/discord-webhook-url is not a Discord webhook URL"
[[ "$(head -c 8 "$dir/secrets/healthchecks-watchdog-url")" == "https://" ]] ||
  die "secrets/healthchecks-watchdog-url is not an https URL"
(($(tr -d '\n' <"$dir/secrets/grafana-admin-password" | wc -c) >= 16)) ||
  die "secrets/grafana-admin-password must be at least 16 characters"
# The GitHub token of the scan exporter is optional (MIL-053): without it, code scanning is not
# read and ScanSourceDown says so. With --apply a missing file is made empty below.
scan_token="$dir/secrets/github-scan-token"
if [[ -s "$scan_token" ]]; then
  ((8#$(stat -c %a "$scan_token") & 8#077)) && die "$scan_token must be readable by its owner only (chmod 600)"
  [[ "$(head -c 11 "$scan_token")" == "github_pat_" ]] ||
    die "secrets/github-scan-token is not a fine-grained GitHub token (github_pat_...)"
else
  echo "warning: secrets/github-scan-token is missing or empty; code scanning is not read (wiki page Monitoring)" >&2
fi

echo "==> checking the configuration with the pinned tools"
# The delimiter is | because the image names hold a slash.
image() { sed -n "s|^    image: \(.*$1.*\)$|\1|p" "$dir/docker-compose.yml" | head -n 1; }
# Mounted where prometheus.yml looks for its rule files, so check config reads every one of
# them, also a rule file that rules-test.yml does not list (MIL-051).
docker run --rm --entrypoint promtool -v "$dir/prometheus:/etc/prometheus:ro" "$(image prom/prometheus)" \
  check config /etc/prometheus/prometheus.yml >/dev/null || die "promtool check config failed"
docker run --rm --entrypoint promtool -v "$dir/prometheus:/p:ro" -w /p "$(image prom/prometheus)" \
  test rules rules-test.yml >/dev/null || die "the unit tests of the alert rules failed"
docker run --rm --entrypoint amtool -v "$dir/alertmanager:/etc/alertmanager:ro" \
  -v "$dir/secrets:/etc/alertmanager-secrets:ro" "$(image prom/alertmanager)" \
  check-config /etc/alertmanager/alertmanager.yml >/dev/null || die "amtool check-config failed"
docker run --rm -v "$dir/loki:/etc/loki:ro" "$(image grafana/loki)" \
  -config.file=/etc/loki/config.yml -verify-config >/dev/null 2>&1 || die "loki -verify-config failed"
echo "    configuration valid"

if ((! apply)); then
  echo "dry run: give --apply to start or update the monitoring on $bind"
  exit 0
fi

[[ "$(id -u)" == 0 ]] || die "run it as root: the secret files are given to the users of their containers"
# Alertmanager runs as nobody (65534) and Grafana as 472 in their images, and cap_drop ALL
# takes away the override that would let them read a file of root. So each file is owned by
# the one container that reads it, 0400, and the folder is 0711: it can be passed, not listed.
echo "==> giving each secret to the user of its container"
chown 65534:65534 "$dir/secrets/discord-webhook-url" "$dir/secrets/healthchecks-watchdog-url" ||
  die "cannot change the owner of the Alertmanager secrets"
chown 472:472 "$dir/secrets/grafana-admin-password" || die "cannot change the owner of the Grafana password"
# The scan exporter runs as 10001; an empty token file keeps compose from making a folder.
[[ -e "$scan_token" ]] || install -m 0600 /dev/null "$scan_token" || die "cannot make $scan_token"
chown 10001:10001 "$scan_token" || die "cannot change the owner of the scan exporter token"
chmod 0400 "$dir/secrets/discord-webhook-url" "$dir/secrets/healthchecks-watchdog-url" \
  "$dir/secrets/grafana-admin-password" "$scan_token" || die "cannot set the mode of the secrets"
chmod 0711 "$dir/secrets" || die "cannot set the mode of $dir/secrets"
# The report folder of the host (MIL-051): the scan exporter (user 10001) serves the reports in
# it, such as the state of the pull deploy (ADR-0044). It is made here, so docker never makes
# it, and compose reads the same path from the environment.
export MONITORING_REPORT_DIR="${MONITORING_REPORT_DIR:-/var/lib/nfhotel-monitoring/metrics}"
[[ "$MONITORING_REPORT_DIR" == /* ]] || die "MONITORING_REPORT_DIR must be an absolute path"
install -d -m 0755 "$MONITORING_REPORT_DIR" || die "cannot make $MONITORING_REPORT_DIR"

# The configuration files are bind mounts, so compose does not see a change in them, and a
# running Prometheus kept its old rules (the alarm drill of 2026-10-24). A hash of each folder
# goes into its service's environment: compose then recreates exactly the changed services.
hash_of() { (cd "$dir" && find "$1" -type f -print0 | sort -z | xargs -0 sha256sum | sha256sum | cut -c1-16); }
export PROMETHEUS_CONFIG_HASH ALERTMANAGER_CONFIG_HASH BLACKBOX_CONFIG_HASH LOKI_CONFIG_HASH GRAFANA_CONFIG_HASH \
  SCAN_EXPORTER_CONFIG_HASH
PROMETHEUS_CONFIG_HASH="$(hash_of prometheus)" || die "cannot hash prometheus/"
ALERTMANAGER_CONFIG_HASH="$(hash_of alertmanager)" || die "cannot hash alertmanager/"
BLACKBOX_CONFIG_HASH="$(hash_of blackbox)" || die "cannot hash blackbox/"
LOKI_CONFIG_HASH="$(hash_of loki)" || die "cannot hash loki/"
GRAFANA_CONFIG_HASH="$(hash_of grafana)" || die "cannot hash grafana/"
SCAN_EXPORTER_CONFIG_HASH="$(hash_of scan-exporter)" || die "cannot hash scan-exporter/"

compose() { (cd "$dir" && docker compose --env-file monitoring.env "$@"); }
echo "==> starting the monitoring on $bind"
compose up -d --remove-orphans --wait --wait-timeout 180 ||
  die "the monitoring did not start; see: docker compose --env-file monitoring.env logs"

# A service can run without answering, and Prometheus keeps running when it cannot read a rule
# file, so `up --wait` proves neither (MIL-051 task 3). Each service must answer its ready
# endpoint (Alertmanager and blackbox publish no port, so they are asked from inside their
# containers), and every rule of the rule files must be loaded. A failure fails the deploy, so
# the pull deploy goes back to the commit that ran before (ADR-0044).
echo "==> checking that the services answer and that Prometheus loaded every rule"
rule_names="$(sed -n 's/^ *- \(alert\|record\): *\([^ ]*\) *$/\2/p' "$dir"/prometheus/rules/*.yml | sort -u)"
[[ -n "$rule_names" ]] || die "no rule in $dir/prometheus/rules"
healthy() {
  local url loaded name
  for url in "http://$bind:9090/-/ready" "http://$bind:3100/ready" "http://$bind:3000/api/health"; do
    curl -fsS -o /dev/null --max-time 5 "$url" 2>/dev/null || {
      unhealthy="$url does not answer"
      return 1
    }
  done
  compose exec -T alertmanager wget -q -O /dev/null -T 5 http://localhost:9093/-/ready >/dev/null 2>&1 || {
    unhealthy="Alertmanager does not answer /-/ready"
    return 1
  }
  compose exec -T blackbox wget -q -O /dev/null -T 5 http://localhost:9115/-/healthy >/dev/null 2>&1 || {
    unhealthy="the blackbox exporter does not answer /-/healthy"
    return 1
  }
  loaded="$(curl -fsS --max-time 10 "http://$bind:9090/api/v1/rules" 2>/dev/null)" || {
    unhealthy="Prometheus does not answer /api/v1/rules"
    return 1
  }
  unhealthy=""
  for name in $rule_names; do
    [[ "$loaded" == *"\"name\":\"$name\""* ]] || unhealthy+=" $name"
  done
  [[ -z "$unhealthy" ]] || {
    unhealthy="Prometheus has not loaded the rules$unhealthy"
    return 1
  }
}
unhealthy=""
for _ in $(seq 1 36); do
  healthy && break
  sleep 5
done
[[ -z "$unhealthy" ]] || die "the monitoring is not healthy after 3 minutes: $unhealthy"
echo "    the five services answer, and Prometheus loaded all $(wc -w <<<"$rule_names") rules"

echo "==> checking the containers of the monitoring against their profiles (MIL-080)"
bash "$ROOT/scripts/check-container-runtime.sh" --filter label=com.docker.compose.project=nfhotel-monitoring ||
  die "a container of the monitoring breaks its profile in ops/security/container-profiles.tsv"
echo "==> done: Grafana answers on port 3000 of $bind, over the tailnet (user admin)"
