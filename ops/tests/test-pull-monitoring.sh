#!/usr/bin/env bash
# Tests of scripts/pull-monitoring.sh (ADR-0044, MIL-051 task 6): the install; a change outside
# the monitoring paths, which deploys nothing and asks no API; a change that waits for the Ops CI
# run of its push and is then deployed; a head whose Ops CI run failed, which is not tried again;
# the dry run; a broken rule, refused before the start; a failed start, which starts the deployed
# commit again; a later good commit, which clears the failure and installs its own pull script
# and units; a head that does not descend from the deployed commit; a main that cannot be
# fetched; the lock; and that the scan exporter accepts every report. A stand-in
# deploy-monitoring.sh records its calls and fails its checks or its start when the rule file
# says so, a local HTTP server stands in for the API of GitHub, and git rewrites the URL of
# GitHub to a local repository, so nothing leaves the container and no container runs.
#
# Run it in Debian 13, the release of the monitoring LXC, with the operations folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:13-slim bash /src/tests/test-pull-monitoring.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get: git, curl, python3)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null && apt-get -qq install -y git curl python3 >/dev/null 2>&1
git config --global user.email test@example.com
git config --global user.name test
git config --global init.defaultBranch main

W="$(mktemp -d)"
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

# The API of GitHub: a local server answers the runs of Ops CI from the file that each case writes.
runs="$W/api/repos/NF-Hotel/NFHotel/actions/workflows/ops-ci.yaml/runs"
mkdir -p "$(dirname "$runs")"
python3 -m http.server 18080 --bind 127.0.0.1 --directory "$W/api" >"$W/api.log" 2>&1 &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null; rm -rf "$W"' EXIT
# ci none: no run yet; ci SHA STATUS CONCLUSION: the run of the push of SHA to main.
ci() {
  if [[ "$1" == none ]]; then
    echo '{"total_count": 0, "workflow_runs": []}' >"$runs"
  else
    printf '{"total_count": 1, "workflow_runs": [{"run_number": 7, "head_sha": "%s", "event": "push",
      "head_branch": "main", "status": "%s", "conclusion": %s}]}\n' "$1" "$2" "$3" >"$runs"
  fi
}
asked() { grep -c "GET /repos/NF-Hotel/NFHotel/actions/workflows/ops-ci.yaml/runs" "$W/api.log"; }
ci none
for _ in $(seq 1 50); do
  curl -fs -o /dev/null "http://127.0.0.1:18080/repos/NF-Hotel/NFHotel/actions/workflows/ops-ci.yaml/runs" && break
  sleep 0.1
done

# The repository: the real pull script and units, a stand-in deploy, a rule file and a page.
git init -q --bare "$W/origin.git"
git clone -q "$W/origin.git" "$W/src" 2>/dev/null
mon=ops/deploy/monitoring
mkdir -p "$W/src/ops/scripts" "$W/src/$mon/prometheus/rules" "$W/src/$mon/systemd" "$W/src/docs"
cp /src/scripts/pull-monitoring.sh "$W/src/ops/scripts/"
cp /src/deploy/monitoring/systemd/nfhotel-monitoring-pull.service \
  /src/deploy/monitoring/systemd/nfhotel-monitoring-pull.timer "$W/src/$mon/systemd/"
cat >"$W/src/ops/scripts/deploy-monitoring.sh" <<EOF
#!/usr/bin/env bash
ops="\$(cd "\$(dirname "\${BASH_SOURCE[0]}")/.." && pwd)"
rule="\$(cat "\$ops/deploy/monitoring/prometheus/rules/a.yml")"
echo "\$(git -C "\$ops" rev-parse HEAD) \${1:-check}" >>"$W/calls"
[[ "\$rule" != broken ]] || { echo "error: the unit tests of the alert rules failed" >&2; exit 1; }
[[ "\${1:-}" != --apply || "\$rule" != nostart ]] || { echo "error: the monitoring did not start" >&2; exit 1; }
EOF
echo good >"$W/src/$mon/prometheus/rules/a.yml"
echo page >"$W/src/docs/page.md"
git -C "$W/src" add -A && git -C "$W/src" commit -qm a && git -C "$W/src" push -q origin main 2>/dev/null
A="$(git -C "$W/src" rev-parse HEAD)"
# commit FILE CONTENT: change one file of the repository, push it to main, print the commit.
commit() {
  echo "$2" >"$W/src/$1"
  git -C "$W/src" add -A && git -C "$W/src" commit -qm "$2" && git -C "$W/src" push -q origin HEAD:main 2>/dev/null
  git -C "$W/src" rev-parse HEAD
}

# The checkout of the LXC: its origin is the URL of GitHub, which git rewrites to the local one.
git clone -q "$W/origin.git" "$W/opt" 2>/dev/null
git -C "$W/opt" remote set-url origin https://github.com/NF-Hotel/NFHotel.git
git config --global url."$W/origin.git".insteadOf https://github.com/NF-Hotel/NFHotel.git
mkdir -p "$W/sbin" "$W/systemd" "$W/bin"
cat >"$W/bin/systemctl" <<EOF
#!/usr/bin/env bash
echo "\$*" >>"$W/systemctl"
EOF
chmod +x "$W/bin/systemctl"
export NFHOTEL_MONITORING_STATE="$W/state" MONITORING_REPORT_DIR="$W/reports" NFHOTEL_GITHUB_API=http://127.0.0.1:18080 \
  NFHOTEL_COMMAND_DIR="$W/sbin" NFHOTEL_SYSTEMD_DIR="$W/systemd" NFHOTEL_SYSTEMCTL="$W/bin/systemctl"

pull() {
  rc=0
  "$W/sbin/nfhotel-monitoring-pull" "$@" >"$W/out" 2>&1 || rc=$?
}
calls() { paste -sd';' "$W/calls" 2>/dev/null; }
state() { cat "$W/state/$1" 2>/dev/null; }
at() { git -C "$W/opt" rev-parse HEAD; }
has() { grep -qxF "$1" "$W/reports/monitoring_pull.prom" 2>/dev/null; }
failed_line() { echo "nfhotel_monitoring_deploy_failed{host=\"nf-monitoring\",commit=\"${1:0:12}\",stage=\"$2\"} 1"; }
deployed_line() { echo "nfhotel_monitoring_deployed_commit_info{host=\"nf-monitoring\",commit=\"$1\"} 1"; }
not_failed='nfhotel_monitoring_deploy_failed{host="nf-monitoring"} 0'
waiting() { grep -q '^nfhotel_monitoring_deploy_waiting_since_timestamp_seconds{host="nf-monitoring"} [0-9]*$' \
  "$W/reports/monitoring_pull.prom"; }
# The scan exporter serves a report only when its own check accepts it (MIL-051).
accepted() {
  python3 -B - "$W/reports/monitoring_pull.prom" <<'PY' >/dev/null 2>"$W/check"
import sys
sys.path.insert(0, "/src/deploy/monitoring/scan-exporter")
from scan_exporter.domain.reports import checked_report
from scan_exporter.presentation.prometheus_text import METRIC_NAMES
with open(sys.argv[1], encoding="utf-8") as report:
    checked_report(report.read(), METRIC_NAMES)
PY
}

bash "$W/opt/ops/scripts/pull-monitoring.sh" --install >"$W/out" 2>&1 &&
  [[ -x "$W/sbin/nfhotel-monitoring-pull" && -f "$W/systemd/nfhotel-monitoring-pull.service" ]] &&
  [[ -f "$W/systemd/nfhotel-monitoring-pull.timer" && "$(state deployed-commit)" == "$A" ]] &&
  [[ "$(state checkout)" == "$W/opt" ]] &&
  [[ "$(paste -sd';' "$W/systemctl")" == "daemon-reload;enable --now nfhotel-monitoring-pull.timer" ]] &&
  ok "--install puts the command and units in place, records the checkout and its commit, and starts the timer" ||
  ko "install: $(cat "$W/out"); systemctl: $(cat "$W/systemctl" 2>&1)"
rc=0
bash "$W/opt/ops/scripts/pull-monitoring.sh" --apply >"$W/out" 2>&1 || rc=$?
[[ "$rc" == 1 ]] && grep -q "run the installed copy" "$W/out" &&
  ok "--apply refuses to run from the checkout it changes" || ko "rc $rc: $(cat "$W/out")"

# A change outside the monitoring paths: nothing deployed, the API not asked.
B="$(commit docs/page.md "page 2")"
before="$(asked)"
pull --apply
[[ "$rc" == 0 && -z "$(calls)" && "$(asked)" == "$before" && "$(state deployed-commit)" == "$A" ]] &&
  grep -q "^nothing to deploy: the monitoring paths of $B are those of the deployed commit $A$" "$W/out" &&
  ok "a change outside the monitoring paths deploys nothing and asks no API" ||
  ko "rc $rc, calls $(calls), asked $before -> $(asked): $(cat "$W/out")"
has "$(deployed_line "$A")" && has "$not_failed" && ! waiting &&
  grep -q '^nfhotel_task_last_success_timestamp_seconds{host="nf-monitoring",task="monitoring_pull"} [0-9]*$' \
    "$W/reports/monitoring_pull.prom" && ok "the report gives the deployed commit, no failure and the time of the run" ||
  ko "report: $(cat "$W/reports/monitoring_pull.prom" 2>&1)"
accepted && ok "the scan exporter accepts the report" || ko "the exporter refuses the report: $(cat "$W/check")"
[[ "$(stat -c %a "$W/reports/monitoring_pull.prom")" == 644 && ! -e "$W/reports/.monitoring_pull.prom.new" ]] &&
  ok "the report is readable by the user of the exporter, and no temporary file stays" ||
  ko "report: $(ls -la "$W/reports")"

# A change of the monitoring waits for the Ops CI run of its push, then is deployed.
C="$(commit "$mon/prometheus/rules/a.yml" good2)"
ci none
pull --apply
[[ "$rc" == 0 && -z "$(calls)" ]] && grep -q "^waiting:" "$W/out" && waiting &&
  ok "a change waits while its Ops CI run has not started, and the report says since when" ||
  ko "rc $rc, calls $(calls): $(cat "$W/out")"
since="$(state waiting-since)"
ci "$C" in_progress null
pull --apply
[[ "$rc" == 0 && -z "$(calls)" && "$(state waiting-since)" == "$since" ]] && grep -q "^waiting:" "$W/out" &&
  ok "a change waits while its Ops CI run runs, and keeps the time it began to wait" ||
  ko "rc $rc, calls $(calls), since $since -> $(state waiting-since): $(cat "$W/out")"
ci "$C" completed '"success"'
: >"$W/systemctl"
pull --apply
[[ "$rc" == 0 && "$(calls)" == "$C check;$C --apply" && "$(state deployed-commit)" == "$C" && "$(at)" == "$C" ]] &&
  ok "a change whose Ops CI run passed is checked, then started, and recorded as deployed" ||
  ko "rc $rc, calls $(calls), deployed $(state deployed-commit): $(cat "$W/out")"
has "$(deployed_line "$C")" && has "$not_failed" && ! waiting && [[ -z "$(state waiting-since)" ]] &&
  ok "after the deploy the report gives the new commit and nothing waiting" ||
  ko "report: $(cat "$W/reports/monitoring_pull.prom" 2>&1)"
[[ "$(paste -sd';' "$W/systemctl")" == "enable --now nfhotel-monitoring-pull.timer" ]] &&
  ok "the deployed commit installs its pull; unchanged units need no daemon-reload" ||
  ko "systemctl: $(cat "$W/systemctl")"

# A head whose Ops CI run failed is refused, and not asked about again.
: >"$W/calls"
D="$(commit "$mon/prometheus/rules/a.yml" good3)"
ci "$D" completed '"failure"'
pull --apply
[[ "$rc" == 1 && -z "$(calls)" && "$(state failed)" == "$D ci" && "$(state deployed-commit)" == "$C" ]] &&
  has "$(failed_line "$D" ci)" && accepted &&
  ok "a head whose Ops CI run failed is refused, and the report names its commit and stage" ||
  ko "rc $rc, calls $(calls), failed $(state failed): $(cat "$W/out")"
before="$(asked)"
pull --apply
[[ "$rc" == 0 && "$(asked)" == "$before" && -z "$(calls)" ]] && grep -q "refused before (ci)" "$W/out" &&
  has "$(failed_line "$D" ci)" && ok "a refused head is not tried again, and stays reported" ||
  ko "rc $rc, asked $before -> $(asked): $(cat "$W/out")"

# The dry run tells and changes nothing; a broken rule is refused before the start.
E="$(commit "$mon/prometheus/rules/a.yml" broken)"
ci "$E" completed '"success"'
cp -p "$W/reports/monitoring_pull.prom" "$W/report.before"
pull
[[ "$rc" == 0 && -z "$(calls)" && "$(state failed)" == "$D ci" ]] && grep -q "would deploy $E" "$W/out" &&
  cmp -s "$W/report.before" "$W/reports/monitoring_pull.prom" && ok "the dry run tells what --apply would do and changes nothing" ||
  ko "rc $rc, calls $(calls): $(cat "$W/out")"
pull --apply
[[ "$rc" == 1 && "$(calls)" == "$E check" && "$(at)" == "$C" && "$(state deployed-commit)" == "$C" ]] &&
  [[ "$(state failed)" == "$E checks" ]] && has "$(failed_line "$E" checks)" &&
  ok "a broken rule is refused before the start, and the checkout returns to the deployed commit" ||
  ko "rc $rc, calls $(calls), at $(at): $(cat "$W/out")"

# A failed start starts the deployed commit again.
: >"$W/calls"
F="$(commit "$mon/prometheus/rules/a.yml" nostart)"
ci "$F" completed '"success"'
pull --apply
[[ "$rc" == 1 && "$(calls)" == "$F check;$F --apply;$C --apply" && "$(at)" == "$C" ]] &&
  [[ "$(state deployed-commit)" == "$C" && "$(state failed)" == "$F start" ]] && has "$(failed_line "$F" start)" &&
  ok "a failed start returns to the deployed commit and starts it again" ||
  ko "rc $rc, calls $(calls), at $(at), failed $(state failed): $(cat "$W/out")"

# A later good commit clears the failure and installs its own pull script and units.
: >"$W/calls"
: >"$W/systemctl"
echo "# a change of the pull itself" >>"$W/src/ops/scripts/pull-monitoring.sh"
sed -i 's/^AccuracySec=10s$/AccuracySec=15s/' "$W/src/$mon/systemd/nfhotel-monitoring-pull.timer"
G="$(commit "$mon/prometheus/rules/a.yml" good4)"
ci "$G" completed '"success"'
pull --apply
[[ "$rc" == 0 && "$(calls)" == "$G check;$G --apply" && "$(state deployed-commit)" == "$G" && -z "$(state failed)" ]] &&
  has "$(deployed_line "$G")" && has "$not_failed" && ok "a later good commit is deployed and clears the failure" ||
  ko "rc $rc, calls $(calls), failed $(state failed): $(cat "$W/out")"
cmp -s "$W/sbin/nfhotel-monitoring-pull" "$W/src/ops/scripts/pull-monitoring.sh" &&
  grep -q '^AccuracySec=15s$' "$W/systemd/nfhotel-monitoring-pull.timer" &&
  [[ "$(paste -sd';' "$W/systemctl")" == "daemon-reload;enable --now nfhotel-monitoring-pull.timer" ]] &&
  ok "the deployed commit installs its changed pull script and units, and reloads systemd" ||
  ko "installed: $(diff "$W/sbin/nfhotel-monitoring-pull" "$W/src/ops/scripts/pull-monitoring.sh" | head -3);" \
    "systemctl: $(cat "$W/systemctl")"

# A head that does not descend from the deployed commit is refused before the API is asked.
: >"$W/calls"
git -C "$W/src" reset -q --hard "$C"
echo other >"$W/src/$mon/prometheus/rules/a.yml"
git -C "$W/src" commit -qam other && git -C "$W/src" push -q -f origin HEAD:main 2>/dev/null
H="$(git -C "$W/src" rev-parse HEAD)"
before="$(asked)"
pull --apply
[[ "$rc" == 1 && -z "$(calls)" && "$(asked)" == "$before" && "$(state failed)" == "$H ancestry" ]] &&
  grep -q "does not descend from the deployed commit $G" "$W/out" &&
  ok "a head that does not descend from the deployed commit is refused" ||
  ko "rc $rc, calls $(calls), failed $(state failed): $(cat "$W/out")"

# A main that cannot be fetched changes nothing, so the report grows old and raises its alert.
cp -p "$W/reports/monitoring_pull.prom" "$W/report.before"
mv "$W/origin.git" "$W/origin.away"
pull --apply
[[ "$rc" == 1 ]] && grep -q "cannot fetch main" "$W/out" && cmp -s "$W/report.before" "$W/reports/monitoring_pull.prom" &&
  ok "a main that cannot be fetched fails the run and leaves the report as it was" || ko "rc $rc: $(cat "$W/out")"
mv "$W/origin.away" "$W/origin.git"

# Two runs never overlap.
rc=0
flock "$W/state/lock" "$W/sbin/nfhotel-monitoring-pull" --apply >"$W/out" 2>&1 || rc=$?
[[ "$rc" == 0 ]] && grep -q "holds the lock; nothing done" "$W/out" &&
  ok "a run while another holds the lock does nothing" || ko "rc $rc: $(cat "$W/out")"

pull --help
[[ "$rc" == 0 ]] && grep -q "^Usage: pull-monitoring.sh" "$W/out" && ok "--help prints the usage" || ko "help: $(cat "$W/out")"
pull --deploy
[[ "$rc" == 2 ]] && ok "an unknown option is a usage error" || ko "rc $rc"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
