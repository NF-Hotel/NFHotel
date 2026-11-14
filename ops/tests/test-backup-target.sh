#!/usr/bin/env bash
# Tests of the off-VM backup copy (DSA-001 D7, ADR-0008, MIL-021 task 7): the entrypoint
# of deploy/backup-target refuses a bad key, keeps its host key across restarts, and
# accepts only new files: replacing, deleting and reading back are refused (MIL-066
# task 1); backup-database.sh copies every backup the target lacks, warns and keeps the
# backup when the target is off, and catches up when it is back; a copy cut off midway
# leaves nothing under the final name, and the next run sends the whole file (MIL-066
# task 2); --copy-only sends what the target lacks and makes, archives and deletes nothing,
# skips its turn while a backup holds the lock, a backup waits for the lock, and a failed copy
# reports backup_host_seen only when the host answers a Tailscale ping (MIL-074);
# prune-backups.sh deletes only what the retention allows, by the time in the
# name, keeps the newest files of each kind, and names and fails on a file that it cannot
# delete (MIL-069). sshd runs in this container in place of the target container; a
# stand-in docker writes the dump, and a stand-in tailscale answers or not.
#
# Run it in Debian 13, the release of the target image, with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:13-slim bash /src/tests/test-backup-target.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get; an rrsync with -no-overwrite)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null
apt-get -qq install -y openssh-server openssh-client rsync python3 age procps >/dev/null 2>&1
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENTRY="$REPO/deploy/backup-target/entrypoint.sh"
W="$(mktemp -d)"
trap 'pkill -f "sshd -D" 2>/dev/null; rm -rf "$W"' EXIT
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
readonly PUB=age1q06hr0q8r0anpr292l5rse023848yurf704z8u4y8r0p40f6afkqcqtpjh

useradd --create-home --shell /bin/sh nfhotel-backup && passwd --lock nfhotel-backup >/dev/null
mkdir -p /root/.ssh && ssh-keygen -q -t ed25519 -N '' -C root@nf-hotel -f /root/.ssh/nfhotel-backup
# The target folder is outside $W, whose mode 0700 would stop the user nfhotel-backup.
export BACKUP_DIR=/srv/nfhotel-backups BACKUP_KEY_DIR="$W/keys"
start() {
  BACKUP_PUBLIC_KEY="$(cat /root/.ssh/nfhotel-backup.pub)" bash "$ENTRY" >"$W/target.log" 2>&1 &
  for _ in $(seq 50); do
    (echo >/dev/tcp/127.0.0.1/22) 2>/dev/null && return 0
    sleep 0.1
  done
  return 1
}
stop() {
  pkill -f "sshd -D" && while pgrep -f "sshd -D" >/dev/null; do sleep 0.1; done
}

echo "== the backup target"
BACKUP_PUBLIC_KEY="ssh-rsa AAAA x" bash "$ENTRY" >/dev/null 2>&1 && ko "accepted an rsa key" || ok "refuses a key that is not ed25519"
BACKUP_PUBLIC_KEY="$(cat /root/.ssh/nfhotel-backup.pub)
ssh-ed25519 AAAA second" bash "$ENTRY" >/dev/null 2>&1 && ko "accepted two keys" || ok "refuses more than one key"
start && ok "the target starts" || ko "start: $(cat "$W/target.log")"
first="$(ssh-keygen -l -f "$W/keys/ssh_host_ed25519_key.pub")"
grep -q 'command="rrsync -wo -no-del -no-overwrite ' /home/nfhotel-backup/.ssh/authorized_keys &&
  grep -q ',restrict ssh-ed25519 ' /home/nfhotel-backup/.ssh/authorized_keys &&
  ok "the key may only run rrsync -wo -no-del -no-overwrite, with restrict" || ko "authorized_keys"
stop && start
[[ "$(ssh-keygen -l -f "$W/keys/ssh_host_ed25519_key.pub")" == "$first" ]] && ok "the host key stays the same after a restart" || ko "host key changed"
ssh-keyscan -t ed25519 127.0.0.1 2>/dev/null >/root/.ssh/known_hosts
RSH="ssh -i /root/.ssh/nfhotel-backup -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes"
echo x >"$W/nfhotel-daily-x.dump.age"
rsync -a --rsh "$RSH" "$W/nfhotel-daily-x.dump.age" nfhotel-backup@127.0.0.1:/ && [[ -f /srv/nfhotel-backups/nfhotel-daily-x.dump.age ]] &&
  ok "a file can be written" || ko "write"
echo y >"$W/nfhotel-daily-x.dump.age"
rsync -a --rsh "$RSH" "$W/nfhotel-daily-x.dump.age" nfhotel-backup@127.0.0.1:/ >/dev/null 2>&1
[[ "$(cat /srv/nfhotel-backups/nfhotel-daily-x.dump.age)" == x ]] &&
  ok "a file on the target cannot be replaced" || ko "replaced: $(cat /srv/nfhotel-backups/nfhotel-daily-x.dump.age)"
mkdir -p "$W/empty"
rsync -r --delete --rsh "$RSH" "$W/empty/" nfhotel-backup@127.0.0.1:/ >/dev/null 2>&1 &&
  ko "--delete was accepted" || ok "--delete is refused"
[[ -f /srv/nfhotel-backups/nfhotel-daily-x.dump.age ]] &&
  ok "files on the target cannot be deleted (--delete from an empty folder)" || ko "deleted"
rsync --rsh "$RSH" nfhotel-backup@127.0.0.1:/nfhotel-daily-x.dump.age "$W/back" >/dev/null 2>&1 && ko "read back" || ok "reading back is refused"
[[ "$($RSH nfhotel-backup@127.0.0.1 id 2>&1)" == *"does not run rsync"* ]] && ok "a shell is refused" || ko "shell"
ssh -i /root/.ssh/nfhotel-backup -o BatchMode=yes -o StrictHostKeyChecking=yes root@127.0.0.1 true 2>/dev/null && ko "root logged in" || ok "root cannot log in"

echo "== backup-database.sh copies what the target lacks"
mkdir -p "$W/bin"
printf '#!/bin/sh\necho dump\n' >"$W/bin/docker" && chmod +x "$W/bin/docker"
settings() {
  printf 'AGE_RECIPIENT=%s\nBACKUP_DIR=%s\nBACKUP_CONTAINER=pg\nBACKUP_TARGET=nfhotel-backup@127.0.0.1:/\nBACKUP_SSH_PORT=%s\n' \
    "$PUB" "$W/vm" "$1" >"$W/backup.env"
}
backup() {
  sleep 1 # each backup file is named by the second
  rm -f "$W/metrics/"*.prom
  PATH="$W/bin:$PATH" NFHOTEL_BACKUP_SETTINGS="$W/backup.env" NFHOTEL_METRICS_DIR="$W/metrics" \
    bash "$REPO/scripts/backup-database.sh" --apply >"$W/out" 2>&1
}
reported() { grep -Eq "^nfhotel_task_last_success_timestamp_seconds\\{task=\"$1\"\\} [0-9]{10}\$" "$W/metrics/$1.prom" 2>/dev/null; }
on_target() { find /srv/nfhotel-backups -name 'nfhotel-*-2*.dump.age' | wc -l; }
settings 22
backup && [[ "$(on_target)" == 1 && -f "$W/vm/.last-copy" ]] && ok "a backup is copied off the VM" || ko "copy: $(cat "$W/out")"
reported backup && reported offvm_copy && ok "the backup and the copy report their success to the monitoring" || ko "reports: $(ls "$W/metrics" 2>&1)"
settings 2299
backup && grep -q "the copy to .* failed" "$W/out" && grep -q "the last copy off the VM was at" "$W/out" && [[ "$(on_target)" == 1 ]] &&
  ok "a target that is off gives a warning, and the backup stays on the VM" || ko "target off: $(cat "$W/out")"
reported backup && ! reported offvm_copy && ok "a failed copy reports the backup but not the copy" || ko "reports when the target is off"
backup
[[ "$(find "$W/vm" -name 'nfhotel-*.dump.age' | wc -l)" == 3 ]] && ok "the backups are kept on the VM meanwhile" || ko "local count"
settings 22
backup && [[ "$(on_target)" == 4 ]] && ok "when the target is back, every missing backup is copied" || ko "catch up: $(on_target) on the target"
[[ ! -e /srv/nfhotel-backups/.last-copy ]] && ok "only backup files are copied" || ko "other files copied"
echo "BACKUP_SSH_PORT=port22" >>"$W/backup.env"
backup && ko "accepted a bad port" || ok "a port that is not a number is refused"

echo "== an interrupted copy leaves nothing under the final name (MIL-066 task 2)"
# A copy is cut off midway: rsync sends at 100 KiB/s, and the test stops the sending side
# once the target has written part of the file, as a dropped connection would.
target=/srv/nfhotel-backups
started() { # started <file name>: the target has written more than 100 KiB of it
  for _ in $(seq 100); do
    find "$target" -maxdepth 1 -name ".$1.*" -size +100k | grep -q . && return 0
    sleep 0.1
  done
  return 1
}
receiver_gone() { # the rsync of the target has ended
  for _ in $(seq 100); do
    pgrep -u nfhotel-backup rsync >/dev/null || return 0
    sleep 0.1
  done
  return 1
}
same() { [[ "$(sha256sum <"$1")" == "$(sha256sum <"$2")" ]]; }
# Finding 2 first, with the options of the copy before MIL-066: --partial keeps the part
# under the final name, and --ignore-existing never replaces it.
old=nfhotel-daily-20000101T000000Z.dump.age
head -c 4000000 /dev/urandom >"$W/$old"
rsync --archive --partial --ignore-existing --bwlimit=100 --rsh "$RSH" "$W/$old" nfhotel-backup@127.0.0.1:/ >/dev/null 2>&1 &
copy=$!
started "$old" && kill "$copy"
wait "$copy" 2>/dev/null
receiver_gone
[[ -f "$target/$old" ]] && ! same "$W/$old" "$target/$old" &&
  ok "with --partial a cut-off copy leaves a part under the final name" || ko "old options: $(ls -la "$target")"
rsync --archive --partial --ignore-existing --rsh "$RSH" "$W/$old" nfhotel-backup@127.0.0.1:/ >/dev/null 2>&1
! same "$W/$old" "$target/$old" && ok "and the next copy with --ignore-existing keeps that part" ||
  ko "the next copy replaced the part"
# Then backup-database.sh: a stand-in rsync sends slowly and writes its process id, so the
# test can stop the sending side of the copy of the script.
new=nfhotel-daily-20000102T000000Z.dump.age
head -c 4000000 /dev/urandom >"$W/vm/$new"
printf '#!/bin/sh\necho $$ >"%s/rsync.pid"\nexec /usr/bin/rsync --bwlimit=100 "$@"\n' "$W" >"$W/bin/rsync"
chmod +x "$W/bin/rsync"
rm -f "$W/rsync.pid"
settings 22 # the check of the port above left a bad port in the settings
backup &
run=$!
started "$new" && kill "$(cat "$W/rsync.pid")"
wait "$run"
receiver_gone
rm -f "$W/bin/rsync"
grep -q "the copy to .* failed" "$W/out" && [[ ! -e "$target/$new" ]] &&
  [[ -z "$(find "$target" -maxdepth 1 -name ".$new.*")" ]] &&
  ok "a cut-off copy of the script leaves nothing on the target" || ko "cut-off copy: $(ls -la "$target") $(cat "$W/out")"
backup && [[ -f "$target/$new" ]] && same "$W/vm/$new" "$target/$new" &&
  ok "the next run sends the whole file: its SHA-256 on the target equals the one on the VM" || ko "the next run: $(cat "$W/out")"

echo "== the copy every hour, its lock and the report of the backup host (MIL-074)"
copy_only() {
  rm -f "$W/metrics/"*.prom
  PATH="$W/bin:$PATH" NFHOTEL_BACKUP_SETTINGS="$W/backup.env" NFHOTEL_METRICS_DIR="$W/metrics" \
    bash "$REPO/scripts/backup-database.sh" --copy-only >"$W/out" 2>&1
}
vm_files() { find "$W/vm" -maxdepth 1 -type f -name 'nfhotel-*' | sort; }
on_target_named() { [[ -f "/srv/nfhotel-backups/$1" ]]; }
NFHOTEL_BACKUP_SETTINGS="$W/backup.env" bash "$REPO/scripts/backup-database.sh" --apply --copy-only >/dev/null 2>&1
[[ $? == 2 ]] && ok "--apply and --copy-only together are refused" || ko "both modes were accepted"
# More daily files than the VM keeps, so a prune would delete some; the target lacks them all.
for day in $(seq -w 1 16); do echo "$day" >"$W/vm/nfhotel-daily-200101${day}T000000Z.dump.age"; done
before="$(vm_files)"
copy_only && [[ "$(vm_files)" == "$before" ]] && on_target_named nfhotel-daily-20010116T000000Z.dump.age &&
  ! grep -q '==> dumping' "$W/out" &&
  ok "--copy-only sends what the target lacks, and makes, archives and deletes nothing on the VM" ||
  ko "copy only: $(cat "$W/out")"
reported offvm_copy && reported backup_host_seen && ! reported backup &&
  ok "--copy-only reports the copy and the backup host, not a backup" || ko "copy only reports: $(ls "$W/metrics" 2>&1)"

# The test holds the lock as a backup would; the runs below get the descriptor closed, so only
# their own lock counts.
late=nfhotel-daily-20010117T000000Z.dump.age
echo late >"$W/vm/$late"
exec 8>>"$W/vm/.lock"
flock 8
copy_only 8>&- && grep -q '^skipped: a backup holds ' "$W/out" && ! on_target_named "$late" && ! reported offvm_copy &&
  ok "a copy that finds the lock taken skips its turn and sends nothing" || ko "locked copy: $(cat "$W/out")"
before="$(vm_files)"
backup 8>&- &
run=$!
for _ in $(seq 100); do grep -q '^==> waiting for the hourly copy' "$W/out" 2>/dev/null && break || sleep 0.1; done
waited=0
grep -q '^==> waiting for the hourly copy' "$W/out" && [[ "$(vm_files)" == "$before" ]] && waited=1
exec 8>&-
wait "$run"
status=$?
((waited && status == 0)) && on_target_named "$late" &&
  ok "a backup waits while the lock is taken, then backs up and copies" || ko "waiting backup: status $status, $(cat "$W/out")"

# shellcheck disable=SC2016 # the stand-in tailscale writes its own arguments, $*
printf '#!/bin/sh\necho "$*" >"%s/tailscale.args"\n' "$W" >"$W/bin/tailscale" && chmod +x "$W/bin/tailscale"
settings 2299 # nothing answers on this port
copy_only && grep -q "the copy to .* failed" "$W/out" && ! reported offvm_copy && reported backup_host_seen &&
  [[ "$(cat "$W/tailscale.args")" == "ping -c 1 --until-direct=false --timeout 10s 127.0.0.1" ]] &&
  ok "a failed copy whose host answers a Tailscale ping reports backup_host_seen, not the copy" ||
  ko "host seen: $(cat "$W/out" "$W/tailscale.args" 2>&1)"
printf '#!/bin/sh\nexit 1\n' >"$W/bin/tailscale"
copy_only && grep -q "the copy to .* failed" "$W/out" && ! reported backup_host_seen && ! reported offvm_copy &&
  ok "a failed copy whose host does not answer reports nothing" || ko "host not seen: $(ls "$W/metrics" 2>&1)"
rm -f "$W/bin/tailscale"
settings 22

echo "== the prune of the target (MIL-069)"
# The prune runs with its clock at NOW, so a file can be named exactly at a limit.
PRUNE="$REPO/deploy/backup-target/prune-backups.sh"
P="$W/prune"
NOW="$(date -u -d 2026-11-08T12:00:00Z +%s)"
readonly DAY=86400
backup_name() { # backup_name <prefix> <suffix> <seconds before NOW>: named as the VM names it
  echo "$1-$(date -u -d "@$((NOW - $3))" +%Y%m%dT%H%M%SZ)$2"
}
make_backup() { # make_backup <prefix> <suffix> <seconds before NOW> [date of the file]
  local name
  name="$(backup_name "$1" "$2" "$3")"
  : >"$P/$name"
  [[ -z "${4:-}" ]] || touch -d "$4" "$P/$name"
  echo "$name"
}
run_prune() { BACKUP_DIR="$P" PRUNE_NOW="$NOW" bash "$PRUNE" "$@" >"$W/prune.out" 2>&1; }
present() { [[ -f "$P/$1" ]]; }
count_named() { find "$P" -maxdepth 1 -type f -name "$1" | wc -l; }
fresh_folder() { rm -rf "$P" && mkdir -p "$P"; }
vm_keep() { sed -nE "s/^readonly $1_KEEP=([0-9]+)\$/\1/p" "$REPO/scripts/backup-database.sh"; }

# The two halves of the retention agree: the prune reads every name that backup-database.sh
# wrote to the target above, and by default keeps as many files as the VM keeps.
written="$(find /srv/nfhotel-backups -maxdepth 1 -type f -name 'nfhotel-*-2*.age' | wc -l)"
BACKUP_DIR=/srv/nfhotel-backups bash "$PRUNE" >"$W/prune.out" 2>&1
counted="$(awk '/: [0-9]+ files, / { n += $2 } END { print n + 0 }' "$W/prune.out")"
((written >= 4 && counted == written)) &&
  grep -q "never fewer than the newest $(vm_keep DAILY) daily and $(vm_keep WEEKLY) weekly " "$W/prune.out" &&
  ok "the prune reads every name that backup-database.sh wrote, and keeps what the VM keeps" ||
  ko "names of the VM: $written written, $counted read; $(cat "$W/prune.out")"

# The newest 14 daily and 8 weekly files are kept by the floors, so the files behind them
# show the limits: 30 days for a daily file, 12 weeks (84 days) for a weekly file.
fresh_folder
for d in $(seq 14); do make_backup nfhotel-daily .dump.age $((d * DAY)) >/dev/null; done
for w in $(seq 8); do make_backup nfhotel-weekly .dump.age $((w * 7 * DAY)) >/dev/null; done
named_new="$(make_backup nfhotel-daily .dump.age $((20 * DAY)) 2000-01-01)"
daily_limit="$(make_backup nfhotel-daily .dump.age $((30 * DAY)))"
daily_past="$(make_backup nfhotel-daily .dump.age $((30 * DAY + 1)))"
named_old="$(make_backup nfhotel-daily .dump.age $((40 * DAY)))"
weekly_limit="$(make_backup nfhotel-weekly .dump.age $((84 * DAY)))"
weekly_past="$(make_backup nfhotel-weekly .dump.age $((84 * DAY + 1)))"
run_prune && [[ "$(count_named 'nfhotel-*')" == 28 ]] && grep -qx "would delete $daily_past" "$W/prune.out" &&
  ok "the dry run names what it would delete, and deletes nothing" || ko "dry run: $(cat "$W/prune.out")"
run_prune --apply || ko "the prune failed: $(cat "$W/prune.out")"
present "$daily_limit" && ! present "$daily_past" &&
  ok "a daily file 30 days old is kept, and one a second older is deleted" || ko "daily limit: $(cat "$W/prune.out")"
present "$weekly_limit" && ! present "$weekly_past" &&
  ok "a weekly file 12 weeks old is kept, and one a second older is deleted" || ko "weekly limit: $(cat "$W/prune.out")"
present "$named_new" && ! present "$named_old" &&
  ok "the time comes from the name, not from the date of the file" || ko "time of a file: $(cat "$W/prune.out")"
grep -qx "deleted $daily_past" "$W/prune.out" && grep -qx "deleted $weekly_past" "$W/prune.out" &&
  grep -qx "deleted $named_old" "$W/prune.out" && [[ "$(count_named 'nfhotel-*')" == 25 ]] &&
  ok "the log names each file it deleted, and only those went" || ko "log: $(cat "$W/prune.out")"

fresh_folder
for i in $(seq 20); do
  make_backup nfhotel-daily .dump.age $((100 * DAY + i)) >/dev/null
  make_backup nfhotel-settings-daily .tar.age $((100 * DAY + i)) >/dev/null
done
for i in $(seq 10); do
  make_backup nfhotel-weekly .dump.age $((200 * DAY + i)) >/dev/null
  make_backup nfhotel-settings-weekly .tar.age $((200 * DAY + i)) >/dev/null
done
run_prune --apply || ko "the prune failed: $(cat "$W/prune.out")"
[[ "$(count_named 'nfhotel-daily-*')" == 14 && "$(count_named 'nfhotel-settings-daily-*')" == 14 ]] &&
  present "$(backup_name nfhotel-daily .dump.age $((100 * DAY + 1)))" &&
  ! present "$(backup_name nfhotel-daily .dump.age $((100 * DAY + 15)))" &&
  ok "the newest 14 daily files of each kind are kept, whatever their age" || ko "daily floor: $(cat "$W/prune.out")"
[[ "$(count_named 'nfhotel-weekly-*')" == 8 && "$(count_named 'nfhotel-settings-weekly-*')" == 8 ]] &&
  present "$(backup_name nfhotel-weekly .dump.age $((200 * DAY + 1)))" &&
  ! present "$(backup_name nfhotel-weekly .dump.age $((200 * DAY + 9)))" &&
  ok "the newest 8 weekly files of each kind are kept, whatever their age" || ko "weekly floor: $(cat "$W/prune.out")"

# Floors of 1, and a newer file of each kind involved, so that only the name protects the rest.
fresh_folder
long_ago="$(date -u -d "@$((NOW - 400 * DAY))" +%Y%m%dT%H%M%SZ)"
others=(".nfhotel-daily-$long_ago.dump.age.Xy12Ab" "nfhotel-daily-$long_ago-copy.dump.age"
  "nfhotel-daily-$long_ago.dump.age.bak" "nfhotel-daily-20261341T000000Z.dump.age"
  "nfhotel-monthly-$long_ago.dump.age" "nfhotel-daily-x.dump.age" .last-copy
  $'nfhotel-daily-x\nnfhotel-daily-'"$long_ago.dump.age")
for name in "${others[@]}"; do : >"$P/$name"; done
ln -s /etc/hostname "$P/nfhotel-weekly-$long_ago.dump.age"
mkdir "$P/nfhotel-settings-weekly-$long_ago.tar.age"
for kind in nfhotel-daily nfhotel-weekly; do make_backup "$kind" .dump.age "$DAY" >/dev/null; done
make_backup nfhotel-settings-weekly .tar.age "$DAY" >/dev/null
expired="$(make_backup nfhotel-daily .dump.age $((400 * DAY + 1)))"
BACKUP_DIR="$P" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 PRUNE_WEEKLY_KEEP=1 bash "$PRUNE" --apply >"$W/prune.out" 2>&1
status=$?
untouched=0
for name in "${others[@]}"; do [[ -f "$P/$name" ]] && untouched=$((untouched + 1)); done
((status == 0 && untouched == ${#others[@]})) && ! present "$expired" &&
  (($(grep -c '^deleted ' "$W/prune.out") == 1)) &&
  [[ -L "$P/nfhotel-weekly-$long_ago.dump.age" && -d "$P/nfhotel-settings-weekly-$long_ago.tar.age" ]] &&
  ok "no other name is touched: rsync temporary files, other times and kinds, a link, a folder, a line break" ||
  ko "other names: status $status, $(ls -A "$P") $(cat "$W/prune.out")"

refused=0
for bad in 0 abc 1.5 -3; do
  BACKUP_DIR="$P" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 PRUNE_DAILY_DAYS="$bad" bash "$PRUNE" --apply >/dev/null 2>&1 ||
    refused=$((refused + 1))
done
for args in "--apply --force" "--apply --every" "--apply --every 0"; do
  # shellcheck disable=SC2086 # each entry is a list of arguments
  BACKUP_DIR="$P" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 bash "$PRUNE" $args >/dev/null 2>&1 || refused=$((refused + 1))
done
BACKUP_DIR="$W/none" bash "$PRUNE" --apply >/dev/null 2>&1 || refused=$((refused + 1))
make_backup nfhotel-daily .dump.age $((400 * DAY + 2)) >/dev/null
BACKUP_DIR="$P" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 PRUNE_DAILY_DAYS=0 bash "$PRUNE" --apply >/dev/null 2>&1
((refused == 8)) && present "$(backup_name nfhotel-daily .dump.age $((400 * DAY + 2)))" &&
  ok "a bad limit, argument or folder is refused, and nothing is deleted" || ko "refused $refused of 8"

fresh_folder
make_backup nfhotel-daily .dump.age "$DAY" >/dev/null
BACKUP_DIR="$P" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 bash "$PRUNE" --apply --every 1 >"$W/every.out" 2>&1 &
loop=$!
for _ in $(seq 50); do grep -q '^nfhotel-settings-weekly-' "$W/every.out" && break || sleep 0.1; done
late="$(make_backup nfhotel-daily .dump.age $((400 * DAY)))"
for _ in $(seq 50); do present "$late" && sleep 0.1 || break; done
kill -TERM "$loop"
wait "$loop"
status=$?
! present "$late" && (($(grep -c '^prune of ' "$W/every.out") >= 2 && status == 0)) &&
  ok "with --every it prunes at its start and again after the wait, and ends with 0 on TERM" ||
  ko "every: status $status, $(cat "$W/every.out")"

# A file that the prune cannot delete, because its user, as in the service, may not write the
# folder: it names the file, still prunes the other kinds and fails; as the service it goes on.
as_service_user=(setpriv --reuid=nfhotel-backup --regid=nfhotel-backup --init-groups)
locked="$W/locked"
mkdir -m 0755 "$locked"
chmod 0711 "$W" # the user may pass through the work folder, not list it
for kind in nfhotel-daily nfhotel-weekly; do
  : >"$locked/$(backup_name "$kind" .dump.age "$DAY")"
  : >"$locked/$(backup_name "$kind" .dump.age $((400 * DAY)))"
done
BACKUP_DIR="$locked" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 PRUNE_WEEKLY_KEEP=1 \
  "${as_service_user[@]}" bash "$PRUNE" --apply >"$W/prune.out" 2>&1
status=$?
((status == 1 && $(grep -c '^error: could not delete nfhotel-' "$W/prune.out") == 2)) &&
  (($(grep -c ' files, ' "$W/prune.out") == 4 && $(find "$locked" -type f | wc -l) == 4)) &&
  ok "a file that cannot be deleted is named, the other kinds are still pruned, and it fails" ||
  ko "cannot delete: status $status, $(cat "$W/prune.out")"
failed_passes() { grep -c '^error: a file could not be deleted' "$W/every.out"; }
BACKUP_DIR="$locked" PRUNE_NOW="$NOW" PRUNE_DAILY_KEEP=1 PRUNE_WEEKLY_KEEP=1 \
  "${as_service_user[@]}" bash "$PRUNE" --apply --every 1 >"$W/every.out" 2>&1 &
loop=$!
for _ in $(seq 50); do (($(failed_passes) >= 2)) && break || sleep 0.1; done
kill -TERM "$loop"
wait "$loop"
status=$?
chmod 0700 "$W"
((status == 0 && $(failed_passes) >= 2)) &&
  ok "with --every a pass that cannot delete a file is logged, and the next pass still runs" ||
  ko "every failure: status $status, $(cat "$W/every.out")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
