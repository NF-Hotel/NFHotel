#!/usr/bin/env bash
# Back up the NFHotel database (ADR-0008, MIL-021 task 7). scripts/install-backup.sh
# installs it as /usr/local/sbin/nfhotel-backup; a systemd timer runs it every day,
# and scripts/deploy-release.sh runs it before every deploy.
#
# Steps:
#   1. pg_dump --format=custom inside the postgres container (no port is opened)
#   2. encrypt the dump with age to the public key of S03; the VM cannot read its own
#      backups, so a stolen VM disk does not reveal them
#   2b. archive the settings and secrets that the rebuild of the VM needs (MIL-037, ADR-0025):
#      the settings files of ops/deploy, the market analysis settings, the proxy certificate,
#      /etc/nfhotel and the ssh key of the copy, encrypted the same way, as
#      nfhotel-settings-<kind>-<time>.tar.age; the secret files that the deploy writes from the
#      settings (ADR-0024) are left out, because the deploy writes them again
#   3. copy every backup that the target lacks off the VM with rsync over ssh, when
#      BACKUP_TARGET is set; a failed copy warns and is caught up by the next run
#   4. keep 14 daily and 8 weekly (Sunday) files on the VM, delete older ones
#   5. report the time of the backup, and of the copy when it succeeded, to the monitoring
#      (backup.prom and offvm_copy.prom in NFHOTEL_METRICS_DIR, ADR-0020)
#
# Settings: /etc/nfhotel/backup.env (root, 0600), written by install-backup.sh:
#   AGE_RECIPIENT   the age public key (age1...) of S03; required
#   BACKUP_TARGET   rsync destination off the VM (D7): the user nfhotel-backup of the
#                   backup target (ops/deploy/backup-target), restricted there to one
#                   write-only folder by rrsync, for example nfhotel-backup@legion:/;
#                   empty keeps the backups on the VM only and prints a warning
#   BACKUP_SSH_PORT the ssh port of the target (default 22)
#   BACKUP_SSH_KEY  the key for that copy (default /root/.ssh/nfhotel-backup); the host
#                   key of the target must be in root's known_hosts (checked strictly)
#   BACKUP_DIR      local folder (default /var/backups/nfhotel)
#   BACKUP_CONTAINER  the running PostgreSQL container to dump (ADR-0010); empty uses the
#                   compose project in ops/deploy/postgres (ADR-0016), or deploy/postgres
#                   in a checkout of NF-Hotel/DevOps
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
etc_dir="${NFHOTEL_ETC:-/etc/nfhotel}"
readonly DAILY_KEEP=14
readonly WEEKLY_KEEP=8
apply=0

die() {
  echo "error: $*" >&2
  exit 1
}

# report_success <task>: write the time of this success to <task>.prom in the folder that
# Alloy reads (ADR-0020, MIL-023 task 9); Prometheus alerts when it grows old. The helper
# is kept in this file because the file is installed alone. A failed write only warns.
report_success() {
  local dir="${NFHOTEL_METRICS_DIR:-/var/lib/nfhotel-metrics}" file
  file="$dir/$1.prom"
  {
    install -d -m 0755 "$dir" &&
      printf '%s\n%s\n%s{task="%s"} %s\n' \
        '# HELP nfhotel_task_last_success_timestamp_seconds When the task last succeeded.' \
        '# TYPE nfhotel_task_last_success_timestamp_seconds gauge' \
        nfhotel_task_last_success_timestamp_seconds "$1" "$(date +%s)" >"$file.tmp" &&
      chmod 0644 "$file.tmp" && mv "$file.tmp" "$file"
  } 2>/dev/null || echo "warning: cannot write the report $file" >&2
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
  compose_dir="$app_dir/ops/deploy/postgres"
  [[ -d "$compose_dir" ]] || compose_dir="$app_dir/deploy/postgres"
  (cd "$compose_dir" && docker compose --env-file postgres.env exec -T postgres sh -c "$dump" _ "$role")
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

# The settings and secrets, for a rebuild of the VM from nothing (MIL-037, ADR-0025). The paths
# are relative to /, so a restore unpacks them in place with tar --directory /.
settings_file="$backup_dir/nfhotel-settings-$kind-$stamp.tar.age"
echo "==> archiving the settings to $settings_file"
deploy_dir="$app_dir/ops/deploy"
[[ -d "$deploy_dir" ]] || deploy_dir="$app_dir/deploy"
mapfile -t paths < <(
  find "$deploy_dir" -mindepth 2 -maxdepth 2 '(' -name '*.env' -o -name 'market-analysis.toml' ')' -type f 2>/dev/null
  find "$deploy_dir/nginx/tls" -maxdepth 1 -name '*.pem' -type f 2>/dev/null
  [[ -d "$etc_dir" ]] && echo "$etc_dir"
  key="${BACKUP_SSH_KEY:-/root/.ssh/nfhotel-backup}"
  [[ -f "$key" ]] && echo "$key"
)
if ((${#paths[@]} == 0)); then
  # A host without the deployment, such as the CI test of the dump, has nothing to archive.
  echo "warning: no settings found in $deploy_dir or $etc_dir; only the database is backed up" >&2
else
  tar --create --file - --absolute-names --transform 's,^/,,' "${paths[@]}" |
    age --encrypt --recipient "$AGE_RECIPIENT" --output "$settings_file.partial" ||
    {
      rm -f "$settings_file.partial"
      die "archiving or encrypting the settings failed"
    }
  chmod 0600 "$settings_file.partial"
  mv "$settings_file.partial" "$settings_file"
  echo "    ${#paths[@]} paths"
fi

if [[ -n "${BACKUP_TARGET:-}" ]]; then
  echo "==> copying the backups to $BACKUP_TARGET"
  key="${BACKUP_SSH_KEY:-/root/.ssh/nfhotel-backup}"
  port="${BACKUP_SSH_PORT:-22}"
  [[ -r "$key" ]] || die "cannot read the backup ssh key $key"
  [[ "$port" =~ ^[0-9]{1,5}$ ]] || die "BACKUP_SSH_PORT '$port' is not a port number"
  # The target may be off, such as a laptop (D7). A failed copy is a warning: the backup
  # is safe on the VM, and the next run sends every file that the target lacks. The
  # target is write-only, so files there are never replaced (--ignore-existing).
  if rsync --archive --partial --ignore-existing --include='nfhotel-*.dump.age' --include='nfhotel-settings-*.tar.age' --exclude='*' \
    --rsh "ssh -p $port -i $key -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes -o ConnectTimeout=20" \
    "$backup_dir/" "$BACKUP_TARGET/"; then
    date -u +%Y-%m-%dT%H:%M:%SZ >"$backup_dir/.last-copy"
    report_success offvm_copy
    echo "    copied off the VM"
  else
    echo "warning: the copy to $BACKUP_TARGET failed; the backups stay on the VM, and the next run sends what the target lacks" >&2
    if [[ -f "$backup_dir/.last-copy" ]]; then
      echo "warning: the last copy off the VM was at $(cat "$backup_dir/.last-copy")" >&2
    else
      echo "warning: no copy off the VM has succeeded yet" >&2
    fi
  fi
else
  echo "warning: BACKUP_TARGET is empty; the backup stays on the VM only (decision D7 is not in place)" >&2
fi

# prune <name pattern> <keep>: keep the newest <keep> files of a kind on the VM.
prune() {
  local old
  mapfile -t old < <(find "$backup_dir" -maxdepth 1 -name "$1" -printf '%f\n' | sort -r | tail -n +"$(($2 + 1))")
  for name in "${old[@]}"; do
    rm -f "${backup_dir:?}/$name"
  done
}
prune 'nfhotel-daily-*.dump.age' "$DAILY_KEEP"
prune 'nfhotel-weekly-*.dump.age' "$WEEKLY_KEEP"
prune 'nfhotel-settings-daily-*.tar.age' "$DAILY_KEEP"
prune 'nfhotel-settings-weekly-*.tar.age' "$WEEKLY_KEEP"
report_success backup
echo "==> done"
