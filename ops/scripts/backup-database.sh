#!/usr/bin/env bash
# Back up the NFHotel database (ADR-0008, MIL-021 task 7). scripts/install-backup.sh
# installs it as /usr/local/sbin/nfhotel-backup; a systemd timer runs it every day,
# and scripts/deploy-release.sh runs it before every deploy.
#
# Steps:
#   1. pg_dump --format=custom inside the postgres container (no port is opened)
#   1b. read the dump back while it is made (MIL-076): tee passes the stream to two readers in
#      the same container, pg_restore --list (the tables) and pg_restore --file=/dev/null (every
#      block to the end), through named pipes in a private folder, so the clear dump is on no
#      disk; a read-back that fails warns and reports no backup_check, and the backup goes on
#      until S01 decides otherwise at the gate of MIL-076
#   2. encrypt the dump with age to the public key of S03; the VM cannot read its own
#      backups, so a stolen VM disk does not reveal them
#   2b. archive the settings and secrets that the rebuild of the VM needs (MIL-037, ADR-0025):
#      the settings files of ops/deploy, the market analysis settings, the proxy certificate,
#      /etc/nfhotel and the ssh key of the copy, encrypted the same way, as
#      nfhotel-settings-<kind>-<time>.tar.age; the secret files that the deploy writes from the
#      settings (ADR-0024) are left out, because the deploy writes them again
#   3. copy every backup that the target lacks off the VM with rsync over ssh, when
#      BACKUP_TARGET is set; a failed copy warns and is caught up by the next run, which is
#      at most an hour later (nfhotel-offvm-copy.timer runs step 3 alone, MIL-074)
#   4. keep 14 daily and 8 weekly (Sunday) files on the VM, delete older ones
#   5. report the time of the backup, and of the copy when it succeeded, to the monitoring
#      (backup.prom and offvm_copy.prom in NFHOTEL_METRICS_DIR, ADR-0020); the same time as
#      backup_check when the dump was read back (0 while none has been), the size of the
#      encrypted dump as nfhotel_backup_bytes, and the tables that the read-back found as
#      nfhotel_backup_tables (backup_bytes.prom and backup_tables.prom); and the time when
#      the backup host last answered as backup_host_seen.prom: after a copy that succeeded, and
#      after a failed copy when the host of BACKUP_TARGET answers a Tailscale ping, so the
#      alert BackupTargetDown tells a target that is down while its host is on from a host
#      that is off (MIL-074)
#
# One lock, .lock in the backup folder, keeps a backup and an hourly copy apart: the two never
# send to the target at once, and a copy never sends a file that the prune is deleting. A backup
# waits for the lock; a copy that finds it taken skips its turn, because the backup copies at
# its end.
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
# Usage: backup-database.sh [--apply | --copy-only]
#   --apply      make the backup; without it the settings are checked and nothing is written
#   --copy-only  run only step 3 and its reports: send what the target lacks; it dumps,
#                archives and deletes nothing (nfhotel-offvm-copy.timer, every hour)
# The die helper is kept in this file because it is installed alone.
# Exit:  0 backed up, copied, skipped or checked (a failed copy is a warning); 1 a step failed;
#        2 usage error
# Needs: bash 4+, flock, GNU coreutils (tee -p, timeout, mkfifo), docker with the compose
#        plugin, age, rsync (for BACKUP_TARGET), tailscale (for backup_host_seen after a failed
#        copy; without it nothing is reported)

set -euo pipefail

settings="${NFHOTEL_BACKUP_SETTINGS:-/etc/nfhotel/backup.env}"
app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
etc_dir="${NFHOTEL_ETC:-/etc/nfhotel}"
readonly DAILY_KEEP=14
readonly WEEKLY_KEEP=8
# A backup waits this long for an hourly copy to give the lock back; nfhotel-offvm-copy.service
# stops a copy after 30 minutes, so the lock comes back well within it.
readonly LOCK_WAIT_SECONDS=2700
# A reader of the read-back is stopped this long after it starts. The dump takes seconds, so
# only a reader that hangs meets it, and the backup, and a deploy after it, go on (MIL-076).
# NFHOTEL_READ_BACK_SECONDS shortens it for the tests.
readonly READ_BACK_SECONDS="${NFHOTEL_READ_BACK_SECONDS:-1800}"
readonly USAGE="usage: backup-database.sh [--apply | --copy-only]"
apply=0
copy_only=0

die() {
  echo "error: $*" >&2
  exit 1
}

# write_report <report> <metric> <help> <value> [<labels>]: write one gauge to <report>.prom in
# the folder that Alloy reads (ADR-0020, MIL-023 task 9). The helpers are kept in this file
# because the file is installed alone. A failed write only warns.
write_report() {
  local dir="${NFHOTEL_METRICS_DIR:-/var/lib/nfhotel-metrics}" file
  file="$dir/$1.prom"
  {
    install -d -m 0755 "$dir" &&
      printf '# HELP %s %s\n# TYPE %s gauge\n%s%s %s\n' "$2" "$3" "$2" "$2" "${5:-}" "$4" >"$file.tmp" &&
      chmod 0644 "$file.tmp" && mv "$file.tmp" "$file"
  } 2>/dev/null || echo "warning: cannot write the report $file" >&2
}

# report_success <task> [<time>]: the time of this success, now unless given, as <task>.prom;
# Prometheus alerts when it grows old.
report_success() {
  write_report "$1" nfhotel_task_last_success_timestamp_seconds 'When the task last succeeded.' \
    "${2:-$(date +%s)}" "{task=\"$1\"}"
}

# host_answers: whether the host of BACKUP_TARGET (user@host:path) answers one Tailscale ping.
# A reply through a relay counts, so a host that is on but not yet reached directly answers.
host_answers() {
  local host="${BACKUP_TARGET#*@}"
  host="${host%%:*}"
  [[ "$host" =~ ^[A-Za-z0-9][A-Za-z0-9.-]*$ ]] || return 1
  command -v tailscale >/dev/null || return 1
  tailscale ping -c 1 --until-direct=false --timeout 10s "$host" >/dev/null 2>&1
}

# copy_off_vm: step 3 and its reports. The target may be off, such as a laptop (D7). A failed
# copy is a warning: the backup is safe on the VM, and the next run, at most an hour later,
# sends every file that the target lacks.
copy_off_vm() {
  local port="${BACKUP_SSH_PORT:-22}"
  if [[ -z "${BACKUP_TARGET:-}" ]]; then
    echo "warning: BACKUP_TARGET is empty; the backup stays on the VM only (decision D7 is not in place)" >&2
    return 0
  fi
  echo "==> copying the backups to $BACKUP_TARGET"
  [[ -r "$copy_key" ]] || die "cannot read the backup ssh key $copy_key"
  [[ "$port" =~ ^[0-9]{1,5}$ ]] || die "BACKUP_SSH_PORT '$port' is not a port number"
  # The target is write-only, so files there are never replaced (--ignore-existing). Without
  # --partial an interrupted copy leaves nothing under the final name, so the next run sends the
  # whole file instead of keeping a part of it for good (MIL-066 task 2).
  if rsync --archive --ignore-existing --include='nfhotel-*.dump.age' --include='nfhotel-settings-*.tar.age' --exclude='*' \
    --rsh "ssh -p $port -i $copy_key -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes -o ConnectTimeout=20" \
    "$backup_dir/" "$BACKUP_TARGET/"; then
    date -u +%Y-%m-%dT%H:%M:%SZ >"$backup_dir/.last-copy"
    report_success offvm_copy
    report_success backup_host_seen
    echo "    copied off the VM"
    return 0
  fi
  echo "warning: the copy to $BACKUP_TARGET failed; the backups stay on the VM, and the next run sends what the target lacks" >&2
  if [[ -f "$backup_dir/.last-copy" ]]; then
    echo "warning: the last copy off the VM was at $(cat "$backup_dir/.last-copy")" >&2
  else
    echo "warning: no copy off the VM has succeeded yet" >&2
  fi
  if host_answers; then
    report_success backup_host_seen
    echo "warning: the backup host answers over Tailscale, so its target is down while the host is on" >&2
  fi
}

for arg in "$@"; do
  case "$arg" in
    --apply) apply=1 ;;
    --copy-only) copy_only=1 ;;
    *)
      echo "$USAGE" >&2
      exit 2
      ;;
  esac
done
if ((apply && copy_only)); then
  echo "$USAGE" >&2
  exit 2
fi

[[ -r "$settings" ]] || die "cannot read $settings (run scripts/install-backup.sh)"
# shellcheck source=/dev/null
. "$settings"
[[ "${AGE_RECIPIENT:-}" =~ ^age1[0-9a-z]{58}$ ]] || die "AGE_RECIPIENT in $settings is not an age public key"
backup_dir="${BACKUP_DIR:-/var/backups/nfhotel}"
copy_key="${BACKUP_SSH_KEY:-/root/.ssh/nfhotel-backup}"
command -v age >/dev/null || die "age is not installed"
((apply || copy_only)) || {
  echo "settings are valid; give --apply to make a backup into $backup_dir, or --copy-only to send what the target lacks"
  exit 0
}

install -d -m 0700 "$backup_dir"
lock="$backup_dir/.lock"
exec 9>>"$lock" || die "cannot open the lock $lock"
if ((copy_only)); then
  if ! flock --nonblock 9; then
    echo "skipped: a backup holds $lock and copies at its end"
    exit 0
  fi
  copy_off_vm
  echo "==> done"
  exit 0
fi
if ! flock --nonblock 9; then
  echo "==> waiting for the hourly copy, which holds $lock"
  flock --wait "$LOCK_WAIT_SECONDS" 9 || die "the lock $lock was not given back within $LOCK_WAIT_SECONDS seconds"
fi

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
# The postgres container: the one that BACKUP_CONTAINER names, or the service postgres of the
# compose project, run from its folder. Both pass the standard input on, which the readers of
# the read-back take; the dump gets none.
if [[ -n "${BACKUP_CONTAINER:-}" ]]; then
  [[ "$BACKUP_CONTAINER" =~ ^[A-Za-z0-9][A-Za-z0-9_.-]*$ ]] || die "BACKUP_CONTAINER is not a container name"
  postgres_dir=.
  in_postgres=(docker exec -i "$BACKUP_CONTAINER")
else
  postgres_dir="$app_dir/ops/deploy/postgres"
  [[ -d "$postgres_dir" ]] || postgres_dir="$app_dir/deploy/postgres"
  in_postgres=(docker compose --env-file postgres.env exec -T postgres)
fi

# The read-back (MIL-076). Each reader waits on its named pipe in a private folder, and tee
# writes the stream to both while age encrypts it, so no file holds the clear dump. With -p,
# tee goes on when a reader ends early, as pg_restore --list does after the table of contents.
# A read-back that cannot start leaves the pipes out, and the dump is made as before.
pipes=()
readers=()
# start_reader <pipe> <script>: run the script in the postgres container in the background, with
# the named pipe as its input and <pipe>.out and <pipe>.err as its output; add its process id to
# readers.
start_reader() {
  (cd "$postgres_dir" && exec timeout --kill-after=10 "$READ_BACK_SECONDS" "${in_postgres[@]}" sh -c "$2") \
    <"$read_back_dir/$1" >"$read_back_dir/$1.out" 2>"$read_back_dir/$1.err" &
  readers+=("$!")
}
if read_back_dir="$(mktemp -d "${TMPDIR:-/tmp}/nfhotel-read-back.XXXXXX")"; then
  trap 'rm -rf "$read_back_dir"' EXIT
  if mkfifo -m 0600 "$read_back_dir/list" "$read_back_dir/read"; then
    pipes=("$read_back_dir/list" "$read_back_dir/read")
    start_reader list 'pg_restore --list'
    start_reader read 'pg_restore --file=/dev/null'
  fi
fi
((${#pipes[@]})) || echo "warning: the read-back cannot start; the dump is not read back" >&2
if ! (cd "$postgres_dir" && "${in_postgres[@]}" sh -c "$dump" _ "$role" </dev/null) |
  tee -p "${pipes[@]}" | age --encrypt --recipient "$AGE_RECIPIENT" --output "$partial"; then
  # A reader whose pipe tee never opened would wait for ever.
  ((${#readers[@]} == 0)) || kill "${readers[@]}" 2>/dev/null || true
  rm -f "$partial"
  die "the dump or the encryption failed"
fi
[[ -s "$partial" ]] || {
  rm -f "$partial"
  die "the backup is empty"
}
chmod 0600 "$partial"
mv "$partial" "$file"
bytes="$(stat -c %s "$file")"
echo "    $(du -h "$file" | cut -f1)"

# The result of the read-back: both readers ended well, and the table of contents names at least
# one table. A table is a line such as "215; 1259 16386 TABLE public room nfhotel_owner"; TABLE
# DATA and TABLE ATTACH are other entries.
read_back=0
tables=0
if ((${#readers[@]})); then
  list_status=0
  read_status=0
  wait "${readers[0]}" || list_status=$?
  wait "${readers[1]}" || read_status=$?
  tables="$(awk '$1 ~ /^[0-9]+;$/ && $4 == "TABLE" && $5 != "DATA" && $5 != "ATTACH" { n++ } END { print n + 0 }' \
    "$read_back_dir/list.out" 2>/dev/null)" || tables=0
  if ((list_status == 0 && read_status == 0 && tables > 0)); then
    read_back=1
    echo "    read back to its end: $tables tables"
  else
    echo "warning: the dump was not read back (pg_restore --list $list_status, pg_restore $read_status, $tables tables); the backup is kept" >&2
    tail -n 2 "$read_back_dir/list.err" "$read_back_dir/read.err" >&2 || true
  fi
fi

# The settings and secrets, for a rebuild of the VM from nothing (MIL-037, ADR-0025). The paths
# are relative to /, so a restore unpacks them in place with tar --directory /. Each part of
# the list is added on its own, so a folder that does not exist, such as nginx/tls when the
# certificate lives elsewhere, adds nothing and never drops the parts after it (MIL-066 task 3).
settings_file="$backup_dir/nfhotel-settings-$kind-$stamp.tar.age"
echo "==> archiving the settings to $settings_file"
deploy_dir="$app_dir/ops/deploy"
[[ -d "$deploy_dir" ]] || deploy_dir="$app_dir/deploy"
paths=()
if [[ -d "$deploy_dir" ]]; then
  mapfile -t paths < <(find "$deploy_dir" -mindepth 2 -maxdepth 2 '(' -name '*.env' -o -name 'market-analysis.toml' ')' -type f)
fi
if [[ -d "$deploy_dir/nginx/tls" ]]; then
  mapfile -t -O "${#paths[@]}" paths < <(find "$deploy_dir/nginx/tls" -maxdepth 1 -name '*.pem' -type f)
fi
if [[ -d "$etc_dir" ]]; then paths+=("$etc_dir"); fi
if [[ -f "$copy_key" ]]; then paths+=("$copy_key"); fi
# A rebuild needs both, so a change to the list above that drops one fails the backup
# instead of writing an archive that cannot restore the VM.
for required in "$etc_dir" "$copy_key"; do
  if [[ -e "$required" ]] && ! printf '%s\n' "${paths[@]}" | grep -qxF -- "$required"; then
    die "the settings archive would lack $required"
  fi
done
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
  # The names only, so the log shows what a rebuild would get back; never the content.
  echo "    ${#paths[@]} paths:"
  printf '      %s\n' "${paths[@]}"
fi

copy_off_vm

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
# backup_check gets the time of the backup, and is written first, so the alert BackupUnreadable
# (backup newer than backup_check) never sees a backup whose read-back is not yet reported. Until
# a dump has been read back, backup_check is 0, so that alert has a report to compare.
now="$(date +%s)"
write_report backup_bytes nfhotel_backup_bytes 'The size of the newest encrypted dump in bytes.' "$bytes"
if ((read_back)); then
  write_report backup_tables nfhotel_backup_tables 'The tables of the newest dump that was read back.' "$tables"
  report_success backup_check "$now"
elif [[ ! -f "${NFHOTEL_METRICS_DIR:-/var/lib/nfhotel-metrics}/backup_check.prom" ]]; then
  report_success backup_check 0
fi
report_success backup "$now"
echo "==> done"
