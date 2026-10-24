#!/usr/bin/env bash
# Install the patching and log settings of the NF Hotel VM (MIL-023 task 4, CIS-001; DSA-001
# risks R11 and R12). S01 or S03 runs it once as root in the checkout /opt/nfhotel, and again
# after a file in deploy/host/apt or deploy/host/journald changes in git.
#
# It:
#   1. installs unattended-upgrades and logrotate from Debian
#   2. installs deploy/host/apt/20auto-upgrades and 52nfhotel-unattended-upgrades in
#      /etc/apt/apt.conf.d: a daily run that installs Debian security updates only, and no
#      automatic reboot
#   3. installs deploy/host/journald/nfhotel.conf in /etc/systemd/journald.conf.d: a
#      persistent journal of at most 1 GB, and restarts journald
#   4. turns on the timers apt-daily and apt-daily-upgrade
#   5. checks, with a dry run of unattended-upgrade, that only the security origin is allowed
#
# Container logs are limited by the Docker daemon (scripts/install-docker-daemon.sh); the
# files in /var/log are rotated by Debian's logrotate.
#
# Usage: install-patching.sh [--apply]
#   --apply  make the changes; without it the script prints what it would do (dry run)
# Exit:  0 installed; 1 a step failed; 2 usage error
# Needs: bash 4+, root, apt-get, install; systemd for steps 3 and 4

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly HOST="$ROOT/deploy/host"
readonly APT_DIR=/etc/apt/apt.conf.d JOURNALD_DIR=/etc/systemd/journald.conf.d
dry_run=1

run() {
  if ((dry_run)); then
    echo "would run: $*"
  else
    "$@"
  fi
}

while (($#)); do
  case "$1" in
    --apply) dry_run=0 ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done
((dry_run)) || [[ "$(id -u)" == 0 ]] || die "run it as root (sudo)"
systemd=0
[[ -d /run/systemd/system ]] && systemd=1

echo "==> 1. packages"
# The lists first: with old ones, the check of step 5 asks the mirror for a security update
# that a newer one has replaced, gets 404, and fails (nf-hotel, 2026-10-24).
run apt-get update -q
run apt-get install -y --no-install-recommends unattended-upgrades logrotate

echo "==> 2. unattended-upgrades: Debian security updates, daily, no automatic reboot"
run install -o root -g root -m 0644 "$HOST/apt/20auto-upgrades" "$APT_DIR/20auto-upgrades"
run install -o root -g root -m 0644 "$HOST/apt/52nfhotel-unattended-upgrades" "$APT_DIR/52nfhotel-unattended-upgrades"

echo "==> 3. journald: persistent, at most 1 GB"
run install -D -o root -g root -m 0644 "$HOST/journald/nfhotel.conf" "$JOURNALD_DIR/nfhotel.conf"
if ((systemd)); then
  run systemctl restart systemd-journald
else
  echo "    systemd is not running here; journald reads the file at its next start"
fi

echo "==> 4. timers"
if ((systemd)); then
  run systemctl enable --now apt-daily.timer apt-daily-upgrade.timer
else
  echo "    systemd is not running here; the timers are not turned on"
fi

echo "==> 5. check"
if ((dry_run)); then
  echo "would check that unattended-upgrade allows only the Debian security origin"
  echo "==> dry run; give --apply to make the changes"
  exit 0
fi
patterns="$(apt-config dump Unattended-Upgrade::Origins-Pattern | grep -c '^Unattended-Upgrade::Origins-Pattern:: ')" || true
[[ "$patterns" == 1 ]] || die "unattended-upgrade has $patterns origin patterns, expected only the security one"
apt-config dump Unattended-Upgrade::Origins-Pattern | grep -F -- "-security,label=Debian-Security" >/dev/null ||
  die "the security origin is not configured"
unattended-upgrade --dry-run >/dev/null 2>&1 || die "unattended-upgrade --dry-run failed; see /var/log/unattended-upgrades"
echo "==> done; unattended-upgrade installs Debian security updates only. Its log is"
echo "    /var/log/unattended-upgrades/unattended-upgrades.log"
