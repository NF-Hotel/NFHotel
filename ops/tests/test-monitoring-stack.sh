#!/usr/bin/env bash
# Start the monitoring stack the way the wiki page Monitoring says (ADR-0020, MIL-023 task 7):
# settings and secret files made by root with mode 0600, then deploy-monitoring.sh --apply.
# Then check that every service runs, that each container can read its own secret files and
# no other, that Grafana takes the admin password from its file, that Prometheus loaded the
# rules, and that the Watchdog reaches Alertmanager. The stand-in secrets reach no real channel.
#
# Run it as root on a Linux host with Docker, in a checkout without monitoring settings:
#   sudo bash ops/tests/test-monitoring-stack.sh
# It starts the compose project of ops/deploy/monitoring on 127.0.0.1 and removes it, its
# volumes, monitoring.env and secrets/ at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, root, docker with the compose plugin, curl

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mon="$OPS/deploy/monitoring"
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

[[ "$(id -u)" == 0 ]] || {
  echo "error: run it as root" >&2
  exit 1
}
[[ ! -e "$mon/monitoring.env" && ! -e "$mon/secrets" ]] || {
  echo "error: $mon has settings already; run it in a clean checkout" >&2
  exit 1
}
compose() { (cd "$mon" && docker compose --env-file monitoring.env "$@"); }
cleanup() {
  compose down -v >/dev/null 2>&1
  rm -rf "$mon/secrets" "$mon/monitoring.env"
}
trap cleanup EXIT

# The steps of the wiki page, part C, with stand-in values.
echo "MONITORING_BIND=127.0.0.1" >"$mon/monitoring.env"
install -d -m 700 "$mon/secrets"
(
  umask 077
  printf '%s' "https://discord.com/api/webhooks/0/stand-in" >"$mon/secrets/discord-webhook-url"
  printf '%s' "https://hc-ping.com/00000000-0000-4000-8000-000000000000" >"$mon/secrets/healthchecks-watchdog-url"
  head -c 18 /dev/urandom | base64 | tr -d '\n' >"$mon/secrets/grafana-admin-password"
)
password="$(cat "$mon/secrets/grafana-admin-password")"

bash "$OPS/scripts/deploy-monitoring.sh" --apply >/tmp/monitoring-deploy.out 2>&1 &&
  ok "deploy-monitoring.sh --apply starts the stack" || {
  ko "deploy-monitoring.sh --apply: $(tail -5 /tmp/monitoring-deploy.out)"
  compose ps
  compose logs --tail 20
}
running="$(compose ps --status running --format '{{.Service}}' | sort | tr '\n' ' ')"
[[ "$running" == "alertmanager blackbox grafana loki prometheus " ]] && ok "the five services run" || ko "running: $running"

modes="$(stat -c '%a %u' "$mon/secrets" "$mon/secrets/discord-webhook-url" "$mon/secrets/grafana-admin-password" | tr '\n' ' ')"
[[ "$modes" == "711 0 400 65534 400 472 " ]] &&
  ok "each secret belongs to the user of its container, 0400, in a 0711 folder" ||
  ko "secret modes: $(stat -c '%n %a %u' "$mon/secrets" "$mon/secrets/"*)"

user="$(curl -fsS -u "admin:$password" http://127.0.0.1:3000/api/user 2>/dev/null)"
[[ "$user" == *'"login":"admin"'* ]] &&
  ok "Grafana takes the admin password from its file" || ko "Grafana login: $(compose logs --tail 10 grafana)"

rules="$(curl -fsS http://127.0.0.1:9090/api/v1/rules 2>/dev/null)"
[[ "$rules" == *'"name":"TaskReportMissing"'* && "$rules" == *'"name":"Watchdog"'* ]] &&
  ok "Prometheus loaded the alert rules" || ko "rules: ${rules:0:200}"

# Each container reads its own secret as the user it runs as.
compose exec -T alertmanager cat /etc/alertmanager-secrets/discord-webhook-url \
  /etc/alertmanager-secrets/healthchecks-watchdog-url >/dev/null 2>&1 &&
  ok "Alertmanager can read its secret files" || ko "Alertmanager cannot read its secret files"
compose exec -T grafana cat /run/secrets/grafana-admin-password >/dev/null 2>&1 &&
  ok "Grafana can read its password file" || ko "Grafana cannot read its password file"
compose exec -T alertmanager cat /etc/alertmanager-secrets/grafana-admin-password >/dev/null 2>&1 &&
  ko "Alertmanager can read the Grafana password" || ok "Alertmanager cannot read the Grafana password"

# The Watchdog fires at the first evaluation (30 s) and reaches Alertmanager.
for _ in $(seq 1 24); do
  alerts="$(compose exec -T alertmanager wget -qO- http://localhost:9093/api/v2/alerts 2>/dev/null)"
  [[ "$alerts" == *'"alertname":"Watchdog"'* ]] && break
  sleep 5
done
[[ "$alerts" == *'"alertname":"Watchdog"'* ]] && ok "the Watchdog reaches Alertmanager" || ko "alerts: ${alerts:0:200}"
logs="$(compose logs alertmanager 2>&1)"
[[ "${logs,,}" != *"permission denied"* ]] && ok "Alertmanager logs no permission error" ||
  ko "Alertmanager: $(compose logs alertmanager 2>&1 | grep -i 'permission denied' | head -2)"

# The silence of the wiki page Monitoring (Daily use) works as written.
am=(exec -T alertmanager amtool --alertmanager.url=http://localhost:9093)
compose "${am[@]}" silence add --duration=1h --comment="test" 'alertname!=Watchdog' >/dev/null 2>&1 &&
  [[ "$(compose "${am[@]}" silence query -q 2>/dev/null | wc -l)" == 1 ]] &&
  ok "the silence of the wiki page is accepted" || ko "amtool silence add failed"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
