#!/usr/bin/env bash
# Back up the NFHotel database (ADR-0008, MIL-021 task 7). scripts/install-backup.sh
# installs it as /usr/local/sbin/nfhotel-backup; a systemd timer runs it every day,
# and scripts/deploy-release.sh runs it before every deploy.
#
# Steps:
#   1. pg_dump --format=custom inside the postgres container (no port is opened)
#   2. encrypt the dump with age to the public key of S03; the VM cannot read its own
#      backups, so a stolen VM disk does not reveal them
#   3. copy the file off the VM with rsync over ssh, when BACKUP_TARGET is set
#   4. keep 14 daily and 8 weekly (Sunday) files on the VM, delete older ones
#
# Settings: /etc/nfhotel/backup.env (root, 0600), written by install-backup.sh:
#   AGE_RECIPIENT   the age public key (age1...) of S03; required
#   BACKUP_TARGET   rsync destination off the VM (D7): the user nfhotel-backup on the
#                   Proxmox node, restricted there to one folder by rrsync, for
#                   example nfhotel-backup@proxmox:; empty keeps the backups on the
#                   VM only and prints a warning
#   BACKUP_SSH_KEY  the key for that copy (default /root/.ssh/nfhotel-backup); the host
#                   key of the target must be in root's known_hosts (checked strictly)
#   BACKUP_DIR      local folder (default /var/backups/nfhotel)
#   BACKUP_CONTAINER  the running PostgreSQL container to dump (ADR-0010); empty uses the
#                   compose project in deploy/postgres
#   BACKUP_ROLE     the role that dumps, for example nfhotel_backup (ADR-0011); empty uses
#                   the superuser of the image
#
# Usage: backup-database.sh [--apply]
#   --apply  make the backup; without it the settings are checked and nothing is written
# The die helper is kept in this file because it is installed alone.
# Exit:  0 backed up (or checked); 1 a step failed; 2 usage error
# Needs: bash 4+, docker with the compose plugin, age, rsync (for BACKUP_TARGET)

set -euo pipefail

settings="${NFHOTEL_BACKUP_SETTINGS:-/etc/nfhotel/backup.env}"
app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
readonly DAILY_KEEP=14
readonly WEEKLY_KEEP=8
apply=0

die() {
  echo "error: $*" >&2
  exit 1
}

for arg in "$@"; do
  case "$arg" in
    --apply) apply=1 ;;
    *)
      echo "usage: backup-database.sh [--apply]" >&2
      exit 2
      ;;
  esac
done

[[ -r "$settings" ]] || die "cannot read $settings (run scripts/install-backup.sh)"
# shellcheck source=/dev/null
. "$settings"
[[ "${AGE_RECIPIENT:-}" =~ ^age1[0-9a-z]{58}$ ]] || die "AGE_RECIPIENT in $settings is not an age public key"
backup_dir="${BACKUP_DIR:-/var/backups/nfhotel}"
command -v age >/dev/null || die "age is not installed"
((apply)) || {
  echo "settings are valid; give --apply to make a backup into $backup_dir"
  exit 0
}

install -d -m 0700 "$backup_dir"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
kind=daily
[[ "$(date -u +%u)" == 7 ]] && kind=weekly
file="$backup_dir/nfhotel-$kind-$stamp.dump.age"
partial="$file.partial"

echo "==> dumping the database to $file"
# The role that dumps: the read-only nfhotel_backup of ADR-0011 when BACKUP_ROLE says so,
# otherwise the superuser of the image. It connects over the socket inside the container.
role="${BACKUP_ROLE:-}"
[[ -z "$role" || "$role" =~ ^[a-z_][a-z0-9_]*$ ]] || die "BACKUP_ROLE '$role' is not a role name"
# shellcheck disable=SC2016 # expanded by the shell inside the container, not here
dump='pg_dump --username "${1:-$POSTGRES_USER}" --dbname "$POSTGRES_DB" --format=custom --no-owner'
if [[ -n "${BACKUP_CONTAINER:-}" ]]; then
  [[ "$BACKUP_CONTAINER" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || die "BACKUP_CONTAINER is not a container name"
  docker exec "$BACKUP_CONTAINER" sh -c "$dump" _ "$role"
else
  (cd "$app_dir/deploy/postgres" && docker compose --env-file postgres.env exec -T postgres sh -c "$dump" _ "$role")
fi |
  age --encrypt --recipient "$AGE_RECIPIENT" --output "$partial" ||
  {
    rm -f "$partial"
    die "the dump or the encryption failed"
  }
[[ -s "$partial" ]] || {
  rm -f "$partial"
  die "the backup is empty"
}
chmod 0600 "$partial"
mv "$partial" "$file"
echo "    $(du -h "$file" | cut -f1)"

if [[ -n "${BACKUP_TARGET:-}" ]]; then
  echo "==> copying it to $BACKUP_TARGET"
  key="${BACKUP_SSH_KEY:-/root/.ssh/nfhotel-backup}"
  [[ -r "$key" ]] || die "cannot read the backup ssh key $key"
  rsync --archive --partial \
    --rsh "ssh -i $key -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes" \
    "$file" "$BACKUP_TARGET/" || die "the copy off the VM failed"
else
  echo "warning: BACKUP_TARGET is empty; the backup stays on the VM only (decision D7 is open)" >&2
fi

# prune <kind> <keep>: keep the newest <keep> files of a kind on the VM.
prune() {
  local old
  mapfile -t old < <(find "$backup_dir" -maxdepth 1 -name "nfhotel-$1-*.dump.age" -printf '%f\n' | sort -r | tail -n +"$(($2 + 1))")
  for name in "${old[@]}"; do
    rm -f "${backup_dir:?}/$name"
  done
}
prune daily "$DAILY_KEEP"
prune weekly "$WEEKLY_KEEP"
echo "==> done"
