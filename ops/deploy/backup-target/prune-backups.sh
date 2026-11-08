#!/usr/bin/env bash
# Prune the copies on the backup target (MIL-069, DSA-001 D7, ADR-0008). The key of nf-hotel
# may only add files there (rrsync -wo -no-del -no-overwrite, MIL-066 task 1), so the backup
# host deletes the old copies itself, in the service prune of docker-compose.yml.
#
#   1. refuse an argument or a setting that is not valid, and a missing backup folder
#   2. read the four kinds of name that backup-database.sh writes on the VM:
#      nfhotel-daily-<time>.dump.age, nfhotel-weekly-<time>.dump.age,
#      nfhotel-settings-daily-<time>.tar.age and nfhotel-settings-weekly-<time>.tar.age,
#      where <time> is the UTC time of the backup (YYYYMMDDTHHMMSSZ)
#   3. per kind, newest first: keep the newest PRUNE_DAILY_KEEP daily or PRUNE_WEEKLY_KEEP
#      weekly files whatever their age; of the rest delete the daily files older than
#      PRUNE_DAILY_DAYS days and the weekly files older than PRUNE_WEEKLY_WEEKS weeks,
#      counted from the time in the name, not from the date of the file
#   4. name each file it deletes (or would delete), and count each kind
#
# The floors are the numbers that the VM keeps (backup-database.sh): the copy sends every
# file that the target lacks (rsync --ignore-existing), so a file deleted here while the VM
# still keeps it would come back with the next copy. No other name is touched: not the
# temporary files of rsync (.nfhotel-...), not .last-copy, not a link, a folder or a name
# with a line break.
#
# Usage: prune-backups.sh [--dry-run | --apply] [--every <seconds>]
#   --dry-run  name what the retention would delete, and delete nothing (the default)
#   --apply    delete it
#   --every    prune, wait that many seconds, and prune again, until stopped (the service)
# Env:   BACKUP_DIR (default /backups); PRUNE_DAILY_DAYS (default 30); PRUNE_WEEKLY_WEEKS
#        (default 12); PRUNE_DAILY_KEEP (default 14) and PRUNE_WEEKLY_KEEP (default 8), the
#        numbers that the VM keeps; PRUNE_NOW (seconds since 1970, default the clock; for
#        the tests)
# Exit:  0 done, or stopped while it waits; 1 a bad argument or setting, or a file that
#        could not be deleted
# Needs: bash 4.4+ (mapfile -d), GNU coreutils and findutils (date -d, sort -z, find -printf)

set -euo pipefail

backup_dir="${BACKUP_DIR:-/backups}"
daily_days="${PRUNE_DAILY_DAYS:-30}"
weekly_weeks="${PRUNE_WEEKLY_WEEKS:-12}"
daily_keep="${PRUNE_DAILY_KEEP:-14}"
weekly_keep="${PRUNE_WEEKLY_KEEP:-8}"
readonly DAY_SECONDS=86400
readonly USAGE="usage: prune-backups.sh [--dry-run | --apply] [--every <seconds>]"
apply=false
every=""

die() {
  echo "error: $*" >&2
  exit 1
}

# whole_number <name> <value>: refuse a value that is not a whole number from 1 to 999999
whole_number() {
  [[ "$2" =~ ^[1-9][0-9]{0,5}$ ]] || die "$1 must be a whole number above 0, not \"$2\""
}

while (($# > 0)); do
  case "$1" in
    --dry-run) apply=false ;;
    --apply) apply=true ;;
    --every)
      (($# > 1)) || die "$USAGE"
      every="$2"
      shift
      ;;
    *) die "$USAGE" ;;
  esac
  shift
done
whole_number PRUNE_DAILY_DAYS "$daily_days"
whole_number PRUNE_WEEKLY_WEEKS "$weekly_weeks"
whole_number PRUNE_DAILY_KEEP "$daily_keep"
whole_number PRUNE_WEEKLY_KEEP "$weekly_keep"
[[ -z "$every" ]] || whole_number --every "$every"
[[ "${PRUNE_NOW:-0}" =~ ^[0-9]{1,11}$ ]] || die "PRUNE_NOW must be seconds since 1970"
[[ -d "$backup_dir" ]] || die "no backup folder at $backup_dir"
if [[ "$apply" == true ]]; then
  mode=apply action=deleted outcome=deleted
else
  mode="dry run" action="would delete" outcome="to delete"
fi

# name_time <time>: the UTC time of a name (YYYYMMDDTHHMMSSZ) in seconds since 1970; fails
# for a time that does not exist, such as month 13
name_time() {
  date -u -d "${1:0:4}-${1:4:2}-${1:6:2} ${1:9:2}:${1:11:2}:${1:13:2}" +%s
}

# prune_kind <prefix> <suffix> <keep> <limit in seconds> <now>: prune one kind of name,
# newest first; fails when a file could not be deleted
prune_kind() {
  local prefix="$1" suffix="$2" keep="$3" limit="$4" now="$5"
  local name stamp taken seen=0 kept=0 gone=0 failed=0
  local -a names
  # NUL-separated, so that a name with a line break stays one name, which the check skips
  mapfile -d '' -t names < <(
    find "$backup_dir" -maxdepth 1 -type f -name "$prefix-*$suffix" -printf '%f\0' | LC_ALL=C sort -rz
  )
  for name in "${names[@]}"; do
    stamp="${name#"$prefix-"}"
    stamp="${stamp%"$suffix"}"
    [[ "$stamp" =~ ^[0-9]{8}T[0-9]{6}Z$ ]] || continue
    taken="$(name_time "$stamp" 2>/dev/null)" || continue
    seen=$((seen + 1))
    if ((seen <= keep || now - taken <= limit)); then
      kept=$((kept + 1))
    elif [[ "$apply" == false ]] || rm -f -- "$backup_dir/$name"; then
      echo "$action $name"
      gone=$((gone + 1))
    else
      echo "error: could not delete $name" >&2
      failed=1
    fi
  done
  echo "$prefix-*$suffix: $seen files, $kept kept, $gone $outcome"
  ((failed == 0))
}

# prune_all: one pass over the four kinds; fails when a file could not be deleted
prune_all() {
  local now status=0
  now="${PRUNE_NOW:-$(date +%s)}"
  local daily=$((daily_days * DAY_SECONDS)) weekly=$((weekly_weeks * 7 * DAY_SECONDS))
  printf 'prune of %s at %s (%s): daily files %s days, weekly files %s weeks,' \
    "$backup_dir" "$(date -u -d "@$now" +%Y-%m-%dT%H:%M:%SZ)" "$mode" "$daily_days" "$weekly_weeks"
  printf ' never fewer than the newest %s daily and %s weekly of each kind\n' "$daily_keep" "$weekly_keep"
  prune_kind nfhotel-daily .dump.age "$daily_keep" "$daily" "$now" || status=1
  prune_kind nfhotel-weekly .dump.age "$weekly_keep" "$weekly" "$now" || status=1
  prune_kind nfhotel-settings-daily .tar.age "$daily_keep" "$daily" "$now" || status=1
  prune_kind nfhotel-settings-weekly .tar.age "$weekly_keep" "$weekly" "$now" || status=1
  return "$status"
}

if [[ -z "$every" ]]; then
  prune_all
  exit
fi
# The service: bash is the first process of its container, which ignores a signal it does
# not trap, so docker stop would wait for its time-out; the trap ends the wait at once.
trap 'exit 0' TERM INT
while true; do
  prune_all || echo "error: a file could not be deleted; the next prune is in $every seconds" >&2
  sleep "$every" &
  wait "$!"
done
