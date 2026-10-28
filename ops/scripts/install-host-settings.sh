#!/usr/bin/env bash
# Install the sshd, sudo and kernel settings of the NF Hotel VM (MIL-023 tasks 2 and 11,
# CIS-001 rows 1.5.x, 3.3.x, 5.1.x and 5.2.x). S01 or S03 runs it as root in the checkout
# /opt/nfhotel, and again after a file in deploy/host/ssh, sudo, logrotate or sysctl changes.
#
# It:
#   1. installs deploy/host/ssh/nfhotel.conf in /etc/ssh/sshd_config.d, checks the whole
#      configuration with sshd -t, and reloads sshd; a configuration that sshd refuses is
#      taken back before anything is reloaded
#   2. checks deploy/host/sudo/nfhotel-logging with visudo and installs it in /etc/sudoers.d,
#      mode 0440, and deploy/host/logrotate/nfhotel-sudo in /etc/logrotate.d
#   3. installs deploy/host/sysctl/60-nfhotel.conf in /etc/sysctl.d and loads it
#
# The administrators log in by Tailscale SSH, which this does not touch, so a mistake in step 1
# does not lock them out. A file that is already in place unchanged is left as it is.
#
# Usage: install-host-settings.sh [--apply]
#   --apply  make the changes; without it the script prints what it would do (dry run)
# Exit:  0 installed; 1 a step failed; 2 usage error
# Needs: bash 4+, root, install, cmp, sshd, visudo (sudo), sysctl; systemd for the reload

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly HOST="$ROOT/deploy/host"
readonly SSHD_FILE=/etc/ssh/sshd_config.d/nfhotel.conf
readonly SUDO_FILE=/etc/sudoers.d/nfhotel-logging
readonly LOGROTATE_FILE=/etc/logrotate.d/nfhotel-sudo
readonly SYSCTL_FILE=/etc/sysctl.d/60-nfhotel.conf
dry_run=1

run() {
  if ((dry_run)); then
    echo "would run: $*"
  else
    "$@"
  fi
}

# same <source> <target>: true when the target already holds the source unchanged.
same() { [[ -f "$2" ]] && cmp -s "$1" "$2"; }

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
for tool in sshd visudo sysctl; do
  command -v "$tool" >/dev/null || die "$tool is not installed"
done
systemd=0
[[ -d /run/systemd/system ]] && systemd=1

echo "==> 1. sshd: keys only, no root login, no forwarding, strong algorithms"
if same "$HOST/ssh/nfhotel.conf" "$SSHD_FILE"; then
  echo "    $SSHD_FILE is up to date"
else
  backup=""
  if [[ -f "$SSHD_FILE" ]]; then
    backup="$SSHD_FILE.$(date -u +%Y%m%dT%H%M%SZ).bak"
    run cp -p "$SSHD_FILE" "$backup"
  fi
  run install -D -o root -g root -m 0644 "$HOST/ssh/nfhotel.conf" "$SSHD_FILE"
  if ((! dry_run)) && ! sshd -t; then
    if [[ -n "$backup" ]]; then
      mv "$backup" "$SSHD_FILE"
    else
      rm -f "$SSHD_FILE"
    fi
    die "sshd -t refused the configuration; $SSHD_FILE is taken back and sshd is not reloaded"
  fi
  if ((systemd)); then
    run systemctl reload ssh
  else
    echo "    systemd is not running here; sshd reads the file at its next start"
  fi
fi

echo "==> 2. sudo: a log file, rotated weekly and kept 13 weeks"
visudo -cqf "$HOST/sudo/nfhotel-logging" || die "visudo refused $HOST/sudo/nfhotel-logging"
if same "$HOST/sudo/nfhotel-logging" "$SUDO_FILE"; then
  echo "    $SUDO_FILE is up to date"
else
  run install -D -o root -g root -m 0440 "$HOST/sudo/nfhotel-logging" "$SUDO_FILE"
fi
if same "$HOST/logrotate/nfhotel-sudo" "$LOGROTATE_FILE"; then
  echo "    $LOGROTATE_FILE is up to date"
else
  run install -D -o root -g root -m 0644 "$HOST/logrotate/nfhotel-sudo" "$LOGROTATE_FILE"
fi

echo "==> 3. sysctl: no redirects or source routing, martians logged, ptrace restricted"
if same "$HOST/sysctl/60-nfhotel.conf" "$SYSCTL_FILE"; then
  echo "    $SYSCTL_FILE is up to date"
else
  run install -D -o root -g root -m 0644 "$HOST/sysctl/60-nfhotel.conf" "$SYSCTL_FILE"
fi
# A container sees /proc/sys read-only; there the file waits for the next boot of a real host.
if [[ -w /proc/sys/net/ipv4/tcp_syncookies ]]; then
  run sysctl --quiet --load="$SYSCTL_FILE"
else
  echo "    /proc/sys is read-only here; the settings load at the next boot"
fi

if ((dry_run)); then
  echo "==> dry run; give --apply to make the changes"
  exit 0
fi
echo "==> done; check with: sshd -T | grep -E 'passwordauth|permitroot|disableforwarding'"
