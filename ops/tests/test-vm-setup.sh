#!/usr/bin/env bash
# Tests of the VM setup scripts (MIL-021 tasks 5 and 7): install-deploy-user.sh (a dry run
# by default; with --apply the user deploy may run only the deploy command), the input
# checks of install-backup.sh and backup-database.sh, and the write-only, folder-limited
# rsync copy of decision D7 (an unknown host key, reading back, a shell and an escape are
# refused).
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-vm-setup.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null
apt-get -qq install -y sudo age openssh-server openssh-client rsync python3 >/dev/null 2>&1
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

echo "== install-deploy-user.sh"
[[ "$(bash /src/scripts/install-deploy-user.sh | grep -c '^would')" -ge 4 ]] && ! id deploy >/dev/null 2>&1 &&
  ok "the default is a dry run that creates nothing" || ko "dry run"
bash /src/scripts/install-deploy-user.sh --apply >/dev/null && ok "--apply installs" || ko "--apply"
[[ "$(su deploy -c 'sudo -n /usr/local/sbin/nfhotel-deploy status' 2>&1)" == "no release deployed yet" ]] &&
  ok "deploy may run the deploy command" || ko "deploy command"
su deploy -c 'sudo -n id' >/dev/null 2>&1 && ko "deploy may run another command" || ok "deploy may not run another command"
su deploy -c 'echo x >>/usr/local/sbin/nfhotel-deploy' 2>/dev/null && ko "deploy changed the command" || ok "deploy may not change the command"
[[ "$(id -nG deploy)" == deploy ]] && ok "deploy is in no other group" || ko "groups: $(id -nG deploy)"
second="$(bash /src/scripts/install-deploy-user.sh --apply 2>&1)"
[[ "$second" == *exists* && "$second" == *"==> done"* ]] && ok "a second run changes nothing" || ko "second run"

echo "== install-backup.sh and backup-database.sh"
[[ "$(bash /src/scripts/install-backup.sh --recipient "$PUB" | grep -c '^would')" -ge 5 ]] && [[ ! -e /etc/nfhotel/backup.env ]] &&
  ok "install-backup.sh is a dry run by default" || ko "install-backup dry run"
bash /src/scripts/install-backup.sh --recipient bad >/dev/null 2>&1 && ko "accepted a bad recipient" || ok "refuses a recipient that is not an age key"
bash /src/scripts/install-backup.sh --recipient "$PUB" --target 'x;rm -rf /' >/dev/null 2>&1 && ko "accepted a bad target" || ok "refuses a target with shell text"
printf 'AGE_RECIPIENT=%s\n' "$PUB" >/tmp/backup.env
NFHOTEL_BACKUP_SETTINGS=/tmp/backup.env bash /src/scripts/backup-database.sh | grep -q "give --apply" && [[ ! -d /var/backups/nfhotel ]] &&
  ok "backup-database.sh only checks without --apply" || ko "backup dry run"
printf 'AGE_RECIPIENT=nope\n' >/tmp/bad.env
NFHOTEL_BACKUP_SETTINGS=/tmp/bad.env bash /src/scripts/backup-database.sh --apply >/dev/null 2>&1 && ko "accepted a bad recipient" || ok "backup refuses a bad recipient"

echo "== write-only copy to the Proxmox node (D7)"
useradd -m -s /bin/sh nfhotel-backup
install -d -o nfhotel-backup -m 700 /var/lib/nfhotel-backups
mkdir -p /root/.ssh && ssh-keygen -q -t ed25519 -N '' -f /root/.ssh/nfhotel-backup
install -d -o nfhotel-backup -m 700 /home/nfhotel-backup/.ssh
echo "command=\"rrsync -wo /var/lib/nfhotel-backups\",restrict $(cat /root/.ssh/nfhotel-backup.pub)" >/home/nfhotel-backup/.ssh/authorized_keys
chown nfhotel-backup /home/nfhotel-backup/.ssh/authorized_keys && chmod 600 /home/nfhotel-backup/.ssh/authorized_keys
ssh-keygen -A >/dev/null && mkdir -p /run/sshd && /usr/sbin/sshd
RSH="ssh -i /root/.ssh/nfhotel-backup -o IdentitiesOnly=yes -o BatchMode=yes -o StrictHostKeyChecking=yes"
echo test >/tmp/nfhotel-daily-x.dump.age
rsync -a --rsh "$RSH" /tmp/nfhotel-daily-x.dump.age nfhotel-backup@127.0.0.1:/ >/dev/null 2>&1 && ko "copied to an unknown host" || ok "an unknown host key is refused"
ssh-keyscan -t ed25519 127.0.0.1 2>/dev/null >>/root/.ssh/known_hosts
rsync -a --rsh "$RSH" /tmp/nfhotel-daily-x.dump.age nfhotel-backup@127.0.0.1:/ && [[ -f /var/lib/nfhotel-backups/nfhotel-daily-x.dump.age ]] &&
  ok "the copy works with the pinned host key" || ko "copy"
rsync --rsh "$RSH" nfhotel-backup@127.0.0.1:/nfhotel-daily-x.dump.age /tmp/back >/dev/null 2>&1 && ko "read back" || ok "reading back is refused (write-only)"
shell="$($RSH nfhotel-backup@127.0.0.1 id 2>&1)"
[[ "$shell" == *"does not run rsync"* ]] && ok "a shell is refused" || ko "shell: $shell"
rsync -a --rsh "$RSH" /tmp/nfhotel-daily-x.dump.age "nfhotel-backup@127.0.0.1:/../" >/dev/null 2>&1
[[ -e /var/lib/nfhotel-daily-x.dump.age ]] && ko "escaped the folder" || ok "an escape out of the folder is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
