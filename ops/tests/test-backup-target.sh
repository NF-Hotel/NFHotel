#!/usr/bin/env bash
# Tests of the off-VM backup copy (DSA-001 D7, ADR-0008, MIL-021 task 7): the entrypoint
# of deploy/backup-target refuses a bad key, keeps its host key across restarts, and
# accepts only write-only rsync; backup-database.sh copies every backup the target
# lacks, warns and keeps the backup when the target is off, and catches up when it is
# back. sshd runs in this container in place of the target container; a stand-in docker
# writes the dump.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-backup-target.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get)

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
grep -q 'command="rrsync -wo ' /home/nfhotel-backup/.ssh/authorized_keys && grep -q ',restrict ssh-ed25519 ' /home/nfhotel-backup/.ssh/authorized_keys &&
  ok "the key may only run rrsync -wo, with restrict" || ko "authorized_keys"
stop && start
[[ "$(ssh-keygen -l -f "$W/keys/ssh_host_ed25519_key.pub")" == "$first" ]] && ok "the host key stays the same after a restart" || ko "host key changed"
ssh-keyscan -t ed25519 127.0.0.1 2>/dev/null >/root/.ssh/known_hosts
RSH="ssh -i /root/.ssh/nfhotel-backup -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes"
echo x >"$W/nfhotel-daily-x.dump.age"
rsync -a --rsh "$RSH" "$W/nfhotel-daily-x.dump.age" nfhotel-backup@127.0.0.1:/ && [[ -f /srv/nfhotel-backups/nfhotel-daily-x.dump.age ]] &&
  ok "a file can be written" || ko "write"
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
  PATH="$W/bin:$PATH" NFHOTEL_BACKUP_SETTINGS="$W/backup.env" bash "$REPO/scripts/backup-database.sh" --apply >"$W/out" 2>&1
}
on_target() { find /srv/nfhotel-backups -name 'nfhotel-*-2*.dump.age' | wc -l; }
settings 22
backup && [[ "$(on_target)" == 1 && -f "$W/vm/.last-copy" ]] && ok "a backup is copied off the VM" || ko "copy: $(cat "$W/out")"
settings 2299
backup && grep -q "the copy to .* failed" "$W/out" && grep -q "the last copy off the VM was at" "$W/out" && [[ "$(on_target)" == 1 ]] &&
  ok "a target that is off gives a warning, and the backup stays on the VM" || ko "target off: $(cat "$W/out")"
backup
[[ "$(find "$W/vm" -name 'nfhotel-*.dump.age' | wc -l)" == 3 ]] && ok "the backups are kept on the VM meanwhile" || ko "local count"
settings 22
backup && [[ "$(on_target)" == 4 ]] && ok "when the target is back, every missing backup is copied" || ko "catch up: $(on_target) on the target"
[[ ! -e /srv/nfhotel-backups/.last-copy ]] && ok "only backup files are copied" || ko "other files copied"
echo "BACKUP_SSH_PORT=port22" >>"$W/backup.env"
backup && ko "accepted a bad port" || ok "a port that is not a number is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
