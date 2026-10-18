#!/usr/bin/env bash
# Install the daily NFHotel database backup on the VM (ADR-0008, MIL-021 task 7).
# S03 runs it once as root in the checkout /opt/nfhotel, and again after
# scripts/backup-database.sh or deploy/systemd/ change in git.
#
# It:
#   1. installs age and rsync from Debian
#   2. installs scripts/backup-database.sh as /usr/local/sbin/nfhotel-backup (root, 0755)
#   3. writes /etc/nfhotel/backup.env (root, 0600) if it does not exist, with the
#      public key given by --recipient and the target given by --target
#   4. installs and starts the systemd timer nfhotel-backup.timer (daily, 02:00 UTC)
#
# The private age key never comes to the VM: S03 creates it on their own machine
# (age-keygen -o nfhotel-backup.key) and gives only the public key here.
#
# Usage: install-backup.sh --recipient <age1...> [--target <user@host:/path>] [--dry-run]
#   --recipient  the age public key of S03 (required when backup.env does not exist yet)
#   --target     rsync destination off the VM (decision D7); may be set later in backup.env
#   --dry-run    print what would be done and change nothing
# Exit:  0 installed; 1 a step failed; 2 usage error
# Needs: bash 4+, root, apt-get, install, systemctl

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
settings=/etc/nfhotel/backup.env
recipient=""
target=""
dry_run=0

die() {
  echo "error: $*" >&2
  exit 1
}

usage() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'
}

run() {
  if ((dry_run)); then
    echo "would run: $*"
  else
    "$@"
  fi
}

while (($#)); do
  case "$1" in
    --recipient)
      recipient="${2:?--recipient needs a value}"
      shift
      ;;
    --target)
      target="${2:?--target needs a value}"
      shift
      ;;
    --dry-run) dry_run=1 ;;
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
[[ -z "$recipient" || "$recipient" =~ ^age1[0-9a-z]{58}$ ]] || die "--recipient is not an age public key"
[[ -z "$target" || "$target" =~ ^[A-Za-z0-9._@:/-]+$ ]] || die "--target has characters that are not allowed"

echo "==> 1. packages"
run apt-get install -y --no-install-recommends age rsync

echo "==> 2. backup command /usr/local/sbin/nfhotel-backup"
run install -o root -g root -m 0755 "$root/scripts/backup-database.sh" /usr/local/sbin/nfhotel-backup

echo "==> 3. settings $settings"
if [[ -f "$settings" ]]; then
  echo "    exists; not changed"
else
  [[ -n "$recipient" ]] || die "give --recipient for the first install"
  if ((dry_run)); then
    echo "would write AGE_RECIPIENT and BACKUP_TARGET to $settings"
  else
    install -d -o root -g root -m 0700 /etc/nfhotel
    umask 077
    {
      echo "# NFHotel database backup settings (scripts/backup-database.sh)."
      echo "AGE_RECIPIENT=$recipient"
      echo "BACKUP_TARGET=$target"
      echo "BACKUP_DIR=/var/backups/nfhotel"
    } >"$settings"
    chmod 0600 "$settings"
  fi
fi

echo "==> 4. systemd timer"
run install -o root -g root -m 0644 "$root/deploy/systemd/nfhotel-backup.service" /etc/systemd/system/nfhotel-backup.service
run install -o root -g root -m 0644 "$root/deploy/systemd/nfhotel-backup.timer" /etc/systemd/system/nfhotel-backup.timer
run systemctl daemon-reload
run systemctl enable --now nfhotel-backup.timer
echo "==> done; test it now with: systemctl start nfhotel-backup.service && journalctl -u nfhotel-backup -n 20"
