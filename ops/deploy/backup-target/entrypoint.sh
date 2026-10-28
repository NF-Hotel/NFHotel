#!/usr/bin/env bash
# Start the write-only backup target (D7): one ssh key, restricted by rrsync to writing
# new files into the backup folder (ADR-0008). The host keys live in their own folder,
# a volume, so the host key that nf-hotel pins stays the same after an update.
#
#   1. refuse a BACKUP_PUBLIC_KEY that is not one ed25519 public key
#   2. make the host key once in the key folder
#   3. write authorized_keys: command="rrsync -wo <folder>",restrict <key>
#   4. run sshd in the foreground: keys only, no root, only the user nfhotel-backup
#
# Env:   BACKUP_PUBLIC_KEY (required; the content of /root/.ssh/nfhotel-backup.pub on
#        nf-hotel), BACKUP_DIR (default /backups), BACKUP_KEY_DIR (default
#        /etc/ssh/keys), BACKUP_SSHD (default /usr/sbin/sshd; for the tests)
# Exit:  only when sshd stops; 1 a bad setting
# Needs: bash 4+, root, openssh-server, rsync and python3 (rrsync)

set -euo pipefail

key="${BACKUP_PUBLIC_KEY:-}"
backup_dir="${BACKUP_DIR:-/backups}"
key_dir="${BACKUP_KEY_DIR:-/etc/ssh/keys}"
sshd="${BACKUP_SSHD:-/usr/sbin/sshd}"
readonly USER_NAME=nfhotel-backup

die() {
  echo "error: $*" >&2
  exit 1
}

[[ "$key" =~ ^ssh-ed25519\ [A-Za-z0-9+/]{68}(\ [A-Za-z0-9@._-]+)?$ ]] ||
  die "BACKUP_PUBLIC_KEY must be one ssh-ed25519 public key (the .pub file of nf-hotel)"

install -d -m 0700 "$key_dir"
[[ -f "$key_dir/ssh_host_ed25519_key" ]] || ssh-keygen -q -t ed25519 -N '' -f "$key_dir/ssh_host_ed25519_key"
install -d -o "$USER_NAME" -m 0700 "$backup_dir"
home="$(getent passwd "$USER_NAME" | cut -d: -f6)"
install -d -o "$USER_NAME" -m 0700 "$home/.ssh"
echo "command=\"rrsync -wo $backup_dir\",restrict $key" >"$home/.ssh/authorized_keys"
chown "$USER_NAME" "$home/.ssh/authorized_keys"
chmod 0600 "$home/.ssh/authorized_keys"
mkdir -p /run/sshd

echo "backup target: $USER_NAME writes into $backup_dir; host key $(ssh-keygen -l -f "$key_dir/ssh_host_ed25519_key.pub" | cut -d' ' -f2)"
exec "$sshd" -D -e \
  -o HostKey="$key_dir/ssh_host_ed25519_key" \
  -o PasswordAuthentication=no \
  -o KbdInteractiveAuthentication=no \
  -o PermitRootLogin=no \
  -o AllowUsers="$USER_NAME" \
  -o AllowTcpForwarding=no \
  -o X11Forwarding=no \
  -o PermitTunnel=no
