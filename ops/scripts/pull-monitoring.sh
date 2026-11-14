#!/usr/bin/env bash
# Deploy the monitoring of NF Hotel from main by a pull on its LXC (ADR-0044, MIL-051).
# nfhotel-monitoring-pull.timer runs the installed copy, /usr/local/sbin/nfhotel-monitoring-pull,
# every 2 minutes as root, on the checkout that deploy-monitoring.sh runs from:
#
#   1. fetch main of NF-Hotel/NFHotel over HTTPS; the checkout must have no other origin
#   2. do nothing when no file of the monitoring paths differs between the deployed commit and
#      the head of main, or when the head was refused before
#   3. refuse a head that does not descend from the deployed commit
#   4. wait while the Ops CI run of the push of the head to main has not passed, and refuse the
#      head when that run failed; the API of GitHub is read without a token
#   5. refuse a checkout in which a tracked file was changed by hand, as the change would start
#      with the head; check out the head (detached) and run deploy-monitoring.sh, first its
#      checks, then with --apply, which starts the stack and checks its health. When the checks
#      fail, the deployed commit is checked out again and nothing starts; when the start fails,
#      the deployed commit is also started again
#   6. record the deployed commit; after a deploy, the script and the units of the new commit
#      install themselves (--install), so a change of them is deployed like any other
#   7. write the report monitoring_pull.prom, which the scan exporter serves to Prometheus: the
#      deployed commit, the time of the last run that read main, whether the last attempt
#      failed (with its commit and stage), and since when a change waits
#
# The monitoring paths: ops/deploy/monitoring/ (with the units of the timer),
# ops/scripts/deploy-monitoring.sh, ops/scripts/pull-monitoring.sh,
# ops/scripts/check-container-runtime.sh, ops/scripts/lib/ and ops/security/container-profiles.tsv.
# A refused head is not tried again; a new commit of main, or removing $state/failed, ends that.
#
# Usage: pull-monitoring.sh [--apply | --install]
#   (none)     fetch main and tell what --apply would do; nothing is deployed or recorded
#   --apply    fetch main and deploy its head when steps 2 to 4 allow it (the timer runs this)
#   --install  install this script as nfhotel-monitoring-pull and the units of its checkout,
#              record the commit of the checkout as deployed when nothing is recorded yet, and
#              enable the timer; run it once after the first manual deploy
# Exit:  0 nothing to do, waiting, or deployed; 1 refused, a failed deploy, or main or the API
#        could not be read; 2 usage error
# Needs: bash 4+, root, git, curl, python3, flock, systemd
# Environment (defaults for the LXC; the tests set their own): NFHOTEL_MONITORING_STATE
#   (/var/lib/nfhotel-monitoring), MONITORING_REPORT_DIR (/var/lib/nfhotel-monitoring/metrics),
#   NFHOTEL_GITHUB_API (https://api.github.com), NFHOTEL_COMMAND_DIR (/usr/local/sbin),
#   NFHOTEL_SYSTEMD_DIR (/etc/systemd/system), NFHOTEL_SYSTEMCTL (systemctl)

set -euo pipefail

readonly STATE="${NFHOTEL_MONITORING_STATE:-/var/lib/nfhotel-monitoring}"
readonly REPORTS="${MONITORING_REPORT_DIR:-/var/lib/nfhotel-monitoring/metrics}"
readonly API="${NFHOTEL_GITHUB_API:-https://api.github.com}"
readonly COMMAND_DIR="${NFHOTEL_COMMAND_DIR:-/usr/local/sbin}"
readonly SYSTEMD_DIR="${NFHOTEL_SYSTEMD_DIR:-/etc/systemd/system}"
readonly SYSTEMCTL="${NFHOTEL_SYSTEMCTL:-systemctl}"
readonly REPO=NF-Hotel/NFHotel
readonly HOST_LABEL=nf-monitoring
readonly PATHS=(ops/deploy/monitoring ops/scripts/deploy-monitoring.sh ops/scripts/pull-monitoring.sh
  ops/scripts/check-container-runtime.sh ops/scripts/lib ops/security/container-profiles.tsv)
readonly UNITS=(nfhotel-monitoring-pull.service nfhotel-monitoring-pull.timer)

# The result of the Ops CI run of the push of a commit to main, from the answer of the API on
# standard input: success, pending (no run yet, running, or cancelled by a newer push) or
# failed:<conclusion>.
readonly CI_RESULT='
import json, sys
sha = sys.argv[1]
runs = [run for run in json.load(sys.stdin).get("workflow_runs", [])
        if run.get("head_sha") == sha and run.get("event") == "push"
        and run.get("head_branch") == "main"]
if not runs:
    print("pending")
else:
    run = max(runs, key=lambda run: run.get("run_number", 0))
    if run.get("status") != "completed" or run.get("conclusion") == "cancelled":
        print("pending")
    elif run.get("conclusion") == "success":
        print("success")
    else:
        print("failed:" + str(run.get("conclusion")))
'

die() {
  echo "error: $*" >&2
  exit 1
}
usage() { sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'; }

# The checkout: the one --install recorded, else the one this script is part of.
find_checkout() {
  local own
  if [[ -s "$STATE/checkout" ]]; then
    cat "$STATE/checkout"
    return
  fi
  own="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
  [[ -f "$own/ops/scripts/deploy-monitoring.sh" ]] ||
    die "no checkout recorded in $STATE/checkout; run pull-monitoring.sh --install from the checkout"
  echo "$own"
}

check_checkout() {
  local url
  git -C "$checkout" rev-parse --git-dir >/dev/null 2>&1 || die "$checkout is not a git checkout"
  # The configured URL: get-url would give it after an insteadOf of the git configuration.
  url="$(git -C "$checkout" config --get remote.origin.url)" || die "$checkout has no origin"
  [[ "$url" == "https://github.com/$REPO.git" || "$url" == "https://github.com/$REPO" ]] ||
    die "the origin of $checkout is not https://github.com/$REPO.git"
}

# put NAME VALUE: a state file, replaced by a rename; forget NAME removes it.
put() {
  printf '%s\n' "$2" >"$STATE/.$1.new" || die "cannot write $STATE/$1"
  mv -f "$STATE/.$1.new" "$STATE/$1" || die "cannot write $STATE/$1"
}
forget() { rm -f "$STATE/$1" || die "cannot remove $STATE/$1"; }

read_state() {
  deployed="$(cat "$STATE/deployed-commit" 2>/dev/null || true)"
  [[ "$deployed" =~ ^[0-9a-f]{40}$ ]] ||
    die "no deployed commit in $STATE/deployed-commit; deploy by hand, then run pull-monitoring.sh --install once"
  git -C "$checkout" cat-file -e "$deployed^{commit}" 2>/dev/null ||
    die "the deployed commit $deployed is not in $checkout"
  failed_commit="" failed_stage=""
  if [[ -s "$STATE/failed" ]]; then
    read -r failed_commit failed_stage <"$STATE/failed" || true
  fi
  waiting_since="$(cat "$STATE/waiting-since" 2>/dev/null || true)"
  [[ "$waiting_since" =~ ^[0-9]+$ ]] || waiting_since=""
}

monitoring_changed() {
  local rc=0
  git -C "$checkout" diff --quiet "$1" "$2" -- "${PATHS[@]}" || rc=$?
  ((rc <= 1)) || die "cannot compare $1 with $2"
  ((rc == 1))
}

ops_ci() {
  local query="head_sha=$1&event=push&branch=main&exclude_pull_requests=true&per_page=10" answer
  answer="$(curl -fsS --max-time 30 -H "Accept: application/vnd.github+json" \
    "$API/repos/$REPO/actions/workflows/ops-ci.yaml/runs?$query")" || return 1
  python3 -c "$CI_RESULT" "$1" <<<"$answer"
}

record_failed() {
  failed_commit="$1" failed_stage="$2"
  put failed "$1 $2"
}

write_report() {
  local tmp="$REPORTS/.monitoring_pull.prom.new" labels
  install -d -m 0755 "$REPORTS" || die "cannot make $REPORTS"
  {
    echo "# HELP nfhotel_task_last_success_timestamp_seconds Time the task last ran to its end."
    echo "# TYPE nfhotel_task_last_success_timestamp_seconds gauge"
    echo "nfhotel_task_last_success_timestamp_seconds{host=\"$HOST_LABEL\",task=\"monitoring_pull\"} $now"
    echo "# HELP nfhotel_monitoring_deployed_commit_info The commit whose monitoring runs (always 1)."
    echo "# TYPE nfhotel_monitoring_deployed_commit_info gauge"
    echo "nfhotel_monitoring_deployed_commit_info{host=\"$HOST_LABEL\",commit=\"$deployed\"} 1"
    echo "# HELP nfhotel_monitoring_deploy_failed 1 when the last deploy of main failed or was refused."
    echo "# TYPE nfhotel_monitoring_deploy_failed gauge"
    if [[ -n "$failed_commit" ]]; then
      labels="host=\"$HOST_LABEL\",commit=\"${failed_commit:0:12}\",stage=\"$failed_stage\""
      echo "nfhotel_monitoring_deploy_failed{$labels} 1"
    else
      echo "nfhotel_monitoring_deploy_failed{host=\"$HOST_LABEL\"} 0"
    fi
    if [[ -n "$waiting_since" ]]; then
      echo "# HELP nfhotel_monitoring_deploy_waiting_since_timestamp_seconds Since when a change waits."
      echo "# TYPE nfhotel_monitoring_deploy_waiting_since_timestamp_seconds gauge"
      echo "nfhotel_monitoring_deploy_waiting_since_timestamp_seconds{host=\"$HOST_LABEL\"} $waiting_since"
    fi
  } >"$tmp" || die "cannot write $tmp"
  chmod 0644 "$tmp" || die "cannot write the report in $REPORTS"
  mv -f "$tmp" "$REPORTS/monitoring_pull.prom" || die "cannot write the report in $REPORTS"
}

# deploy-monitoring.sh of the checkout, without the lock (fd 9), so nothing it starts keeps it.
deploy_monitoring() { bash "$checkout/ops/scripts/deploy-monitoring.sh" "$@" 9>&-; }

deploy() {
  local target="$1" changed
  echo "==> deploying $target; the deployed commit is $deployed"
  # git checkout carries a change made by hand over to the head when the head does not change
  # that file, so the stack would start what no review saw (ADR-0044).
  changed="$(git -C "$checkout" status --porcelain --untracked-files=no)" || die "cannot read the state of $checkout"
  if [[ -n "$changed" ]]; then
    record_failed "$target" checkout
    echo "error: a tracked file of $checkout was changed by hand; see git -C $checkout status" >&2
    return 1
  fi
  git -C "$checkout" checkout -q --detach "$target" || {
    record_failed "$target" checkout
    echo "error: cannot check out $target in $checkout" >&2
    return 1
  }
  if ! deploy_monitoring; then
    git -C "$checkout" checkout -q --detach "$deployed" || echo "error: cannot check out $deployed again" >&2
    record_failed "$target" checks
    echo "error: the checks of $target failed; nothing was started, and $deployed stays" >&2
    return 1
  fi
  if ! deploy_monitoring --apply; then
    echo "==> the start of $target failed; starting $deployed again"
    if git -C "$checkout" checkout -q --detach "$deployed" && deploy_monitoring --apply; then
      record_failed "$target" start
      echo "error: the start of $target failed; $deployed runs again" >&2
    else
      record_failed "$target" rollback
      echo "error: the start of $target failed, and so did the start of $deployed" >&2
    fi
    return 1
  fi
  put deployed-commit "$target"
  deployed="$target" failed_commit="" failed_stage="" waiting_since=""
  forget failed
  forget waiting-since
  echo "==> $target runs; installing its pull script and units"
  bash "$checkout/ops/scripts/pull-monitoring.sh" --install 9>&- || {
    record_failed "$target" install
    echo "error: $target runs, but its pull script or units did not install" >&2
    return 1
  }
}

pull() {
  local apply="$1" head ci
  checkout="$(find_checkout)"
  check_checkout
  if ((apply)); then
    [[ "$(readlink -f "${BASH_SOURCE[0]}")" != "$(readlink -f "$checkout")"/* ]] ||
      die "run the installed copy, $COMMAND_DIR/nfhotel-monitoring-pull: a checkout changes this file while it runs"
    install -d -m 0755 "$STATE" || die "cannot make $STATE"
    exec 9>>"$STATE/lock"
    flock --nonblock 9 || {
      echo "another run of the pull holds the lock; nothing done"
      return 0
    }
  fi
  read_state
  timeout 300 git -C "$checkout" fetch --quiet --no-tags origin "+refs/heads/main:refs/remotes/origin/main" ||
    die "cannot fetch main from origin"
  head="$(git -C "$checkout" rev-parse --verify --quiet 'refs/remotes/origin/main^{commit}')" ||
    die "no main after the fetch"
  now="$(date +%s)"

  if [[ "$head" == "$deployed" ]] || ! monitoring_changed "$deployed" "$head"; then
    echo "nothing to deploy: the monitoring paths of $head are those of the deployed commit $deployed"
    if ((apply)); then
      # A failed install of the deployed commit stays reported until the next deploy.
      if [[ "$failed_stage" != install || "$failed_commit" != "$deployed" ]]; then
        failed_commit="" failed_stage=""
        forget failed
      fi
      waiting_since=""
      forget waiting-since
      write_report
    fi
    return 0
  fi
  if [[ "$head" == "$failed_commit" ]]; then
    echo "the head $head was refused before ($failed_stage); waiting for a new commit of main"
    if ((apply)); then write_report; fi
    return 0
  fi
  if ((apply)) && [[ -z "$waiting_since" ]]; then
    waiting_since="$now"
    put waiting-since "$now"
  fi
  if ! git -C "$checkout" merge-base --is-ancestor "$deployed" "$head"; then
    echo "error: $head does not descend from the deployed commit $deployed; refused" >&2
    if ((apply)); then
      record_failed "$head" ancestry
      write_report
    fi
    return 1
  fi
  ci="$(ops_ci "$head")" || die "cannot read the Ops CI run of $head from the API of GitHub"
  case "$ci" in
    pending)
      echo "waiting: the Ops CI run of the push of $head to main has not passed yet"
      if ((apply)); then write_report; fi
      return 0
      ;;
    success) ;;
    *)
      echo "error: the Ops CI run of the push of $head to main ended with ${ci#failed:}; refused" >&2
      if ((apply)); then
        record_failed "$head" ci
        write_report
      fi
      return 1
      ;;
  esac
  if ((! apply)); then
    echo "dry run: --apply would deploy $head (Ops CI passed); the deployed commit is $deployed"
    return 0
  fi
  if ! deploy "$head"; then
    write_report
    return 1
  fi
  write_report
  echo "==> done: $head runs on the monitoring host"
}

# --install: this script and the units of its checkout, each replaced by a rename, so the copy
# that runs keeps reading the old file (ADR-0018).
install_from_checkout() {
  local source="$1" target="$2" mode="$3"
  if [[ -f "$target" ]] && cmp -s "$source" "$target"; then
    return 1
  fi
  install -m "$mode" "$source" "$target.new" || die "cannot install $target"
  mv -f "$target.new" "$target" || die "cannot install $target"
  echo "    installed $target"
}

install_pull() {
  local own unit changed=0
  own="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
  [[ -f "$own/ops/scripts/deploy-monitoring.sh" && -d "$own/ops/deploy/monitoring/systemd" ]] ||
    die "--install runs from ops/scripts of a checkout of $REPO"
  checkout="$own"
  check_checkout
  [[ -d "$COMMAND_DIR" && -d "$SYSTEMD_DIR" ]] || die "$COMMAND_DIR or $SYSTEMD_DIR is missing"
  install -d -m 0755 "$STATE" || die "cannot make $STATE"
  [[ "$(cat "$STATE/checkout" 2>/dev/null || true)" == "$own" ]] || put checkout "$own"
  if [[ ! -s "$STATE/deployed-commit" ]]; then
    put deployed-commit "$(git -C "$own" rev-parse --verify 'HEAD^{commit}')"
    echo "    recorded $(cat "$STATE/deployed-commit") as the deployed commit"
  fi
  install_from_checkout "$own/ops/scripts/pull-monitoring.sh" "$COMMAND_DIR/nfhotel-monitoring-pull" 0755 || true
  for unit in "${UNITS[@]}"; do
    if install_from_checkout "$own/ops/deploy/monitoring/systemd/$unit" "$SYSTEMD_DIR/$unit" 0644; then
      changed=1
    fi
  done
  if ((changed)); then
    "$SYSTEMCTL" daemon-reload || die "systemctl daemon-reload failed"
  fi
  "$SYSTEMCTL" enable --now nfhotel-monitoring-pull.timer || die "the timer of the pull did not start"
  echo "==> the pull deploy of the monitoring is installed from $own"
}

case "${1:-}" in
  "") pull 0 ;;
  --apply)
    [[ "$(id -u)" == 0 ]] || die "run it as root, as deploy-monitoring.sh --apply runs"
    pull 1
    ;;
  --install)
    [[ "$(id -u)" == 0 ]] || die "run it as root: it installs into $COMMAND_DIR and $SYSTEMD_DIR"
    install_pull
    ;;
  -h | --help) usage ;;
  *)
    usage >&2
    exit 2
    ;;
esac
