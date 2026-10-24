#!/usr/bin/env bash
# Start or update the monitoring of NF Hotel in its LXC container (ADR-0020, MIL-023 task 7).
# S03 runs it as root in a checkout of NF-Hotel/NFHotel on the LXC, after
# scripts/create-monitoring-lxc.sh, and again after a change of ops/deploy/monitoring/.
#
#   1. refuse unless monitoring.env names MONITORING_BIND as an address of this host
#   2. refuse unless the three secret files exist, are not empty, and no group or other
#      user can read them
#   3. check the configuration with the tools of the pinned images: promtool check
#      config, promtool test rules, amtool check-config, loki -verify-config
#   4. give each secret file to the user its container runs as (0400), the folder 0711
#   5. docker compose up -d --wait, so it returns once every service is running; each service
#      whose configuration folder changed is recreated, so it reads the new files
#
# Usage: deploy-monitoring.sh [--apply]
#   --apply  start or update the services; without it the script only checks (a dry run)
# Exit:  0 checked (or started); 1 a check or the start failed; 2 usage error
# Needs: bash 4+, root, docker with the compose plugin, ip (iproute2)

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

echo "==> checking the configuration with the pinned tools"
# The delimiter is | because the image names hold a slash.
image() { sed -n "s|^    image: \(.*$1.*\)$|\1|p" "$dir/docker-compose.yml" | head -n 1; }
docker run --rm --entrypoint promtool -v "$dir/prometheus:/p:ro" -w /p "$(image prom/prometheus)" \
  check config /p/prometheus.yml >/dev/null || die "promtool check config failed"
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
chmod 0400 "$dir/secrets/discord-webhook-url" "$dir/secrets/healthchecks-watchdog-url" \
  "$dir/secrets/grafana-admin-password" || die "cannot set the mode of the secrets"
chmod 0711 "$dir/secrets" || die "cannot set the mode of $dir/secrets"

# The configuration files are bind mounts, so compose does not see a change in them, and a
# running Prometheus kept its old rules (the alarm drill of 2026-10-24). A hash of each folder
# goes into its service's environment: compose then recreates exactly the changed services.
hash_of() { (cd "$dir" && find "$1" -type f -print0 | sort -z | xargs -0 sha256sum | sha256sum | cut -c1-16); }
export PROMETHEUS_CONFIG_HASH ALERTMANAGER_CONFIG_HASH BLACKBOX_CONFIG_HASH LOKI_CONFIG_HASH GRAFANA_CONFIG_HASH
PROMETHEUS_CONFIG_HASH="$(hash_of prometheus)" || die "cannot hash prometheus/"
ALERTMANAGER_CONFIG_HASH="$(hash_of alertmanager)" || die "cannot hash alertmanager/"
BLACKBOX_CONFIG_HASH="$(hash_of blackbox)" || die "cannot hash blackbox/"
LOKI_CONFIG_HASH="$(hash_of loki)" || die "cannot hash loki/"
GRAFANA_CONFIG_HASH="$(hash_of grafana)" || die "cannot hash grafana/"

echo "==> starting the monitoring on $bind"
(cd "$dir" && docker compose --env-file monitoring.env up -d --remove-orphans --wait --wait-timeout 180) ||
  die "the monitoring did not start; see: docker compose --env-file monitoring.env logs"
echo "==> done: Grafana answers on port 3000 of $bind, over the tailnet (user admin)"
