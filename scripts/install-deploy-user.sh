#!/usr/bin/env bash
# Prepare the NFHotel VM for the CD workflow (ADR-0007, MIL-021 task 5). S03 runs it
# once as root in the checkout /opt/nfhotel, and again after scripts/deploy-release.sh
# changes in git; the deploy command is a root-owned copy, so a change in git alone
# never changes what runs as root.
#
# It:
#   1. creates the user deploy (no password, not in the docker or sudo group)
#   2. installs scripts/deploy-release.sh as /usr/local/sbin/nfhotel-deploy (root, 0755)
#   3. allows deploy exactly one command through sudo: /usr/local/sbin/nfhotel-deploy
#      (/etc/sudoers.d/nfhotel-deploy, checked with visudo before it is used)
#   4. creates the state folder /var/lib/nfhotel-deploy (release log and lock)
#   5. prints the Tailscale steps and the policy that S03 applies in the admin console
#
# The CD job reaches the VM with Tailscale SSH as deploy and runs
#   sudo -n /usr/local/sbin/nfhotel-deploy deploy <sha> <web> <api> <analysis> <benchmark>
# No ssh key is stored anywhere: Tailscale SSH authorizes the tagged CI node.
#
# Usage: install-deploy-user.sh [--apply]
#   --apply  make the changes; without it the script prints what it would do (dry run)
# Exit:  0 installed; 1 a step failed; 2 usage error
# Needs: bash 4+, root, useradd, install, visudo (sudo), tailscale (for step 5)

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly DEPLOY_USER=deploy
readonly COMMAND_PATH=/usr/local/sbin/nfhotel-deploy
readonly SUDOERS=/etc/sudoers.d/nfhotel-deploy
readonly STATE_DIR=/var/lib/nfhotel-deploy
dry_run=1

run() {
  if ((dry_run)); then
    echo "would run: $*"
  else
    "$@"
  fi
}

for arg in "$@"; do
  case "$arg" in
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
done

((dry_run)) || [[ "$(id -u)" == 0 ]] || die "run it as root (sudo)"
[[ -f "$ROOT/scripts/deploy-release.sh" ]] || die "scripts/deploy-release.sh is missing in $ROOT"

echo "==> 1. user $DEPLOY_USER"
if id "$DEPLOY_USER" >/dev/null 2>&1; then
  echo "    exists"
  for group in docker sudo; do
    if id -nG "$DEPLOY_USER" | tr ' ' '\n' | grep -qx "$group"; then
      die "$DEPLOY_USER is in the group $group; remove it first (gpasswd -d $DEPLOY_USER $group)"
    fi
  done
else
  run useradd --create-home --shell /bin/bash --comment "NFHotel CD" "$DEPLOY_USER"
  run passwd --lock "$DEPLOY_USER"
fi

echo "==> 2. deploy command $COMMAND_PATH"
run install -o root -g root -m 0755 "$ROOT/scripts/deploy-release.sh" "$COMMAND_PATH"

echo "==> 3. sudo rule $SUDOERS"
rule="$DEPLOY_USER ALL=(root) NOPASSWD: $COMMAND_PATH"
if ((dry_run)); then
  echo "would write: $rule"
else
  tmp="$(mktemp)"
  echo "$rule" >"$tmp"
  visudo -cf "$tmp" >/dev/null || {
    rm -f "$tmp"
    die "the sudo rule did not pass visudo"
  }
  install -o root -g root -m 0440 "$tmp" "$SUDOERS"
  rm -f "$tmp"
fi

echo "==> 4. state folder $STATE_DIR"
run install -d -o root -g root -m 0750 "$STATE_DIR"

cat <<EOF
==> 5. Tailscale (done by S03)
   On this VM, turn on Tailscale SSH:
     tailscale set --ssh
   In the admin console, give this machine the tag tag:nfhotel-vm (Machines, Edit ACL tags),
   and add to the tailnet policy:
     "tagOwners": { "tag:ci": ["autogroup:admin"], "tag:nfhotel-vm": ["autogroup:admin"] },
     "grants":    [ { "src": ["tag:ci"], "dst": ["tag:nfhotel-vm"], "ip": ["tcp:22"] } ],
     "ssh":       [ { "action": "accept", "src": ["tag:ci"], "dst": ["tag:nfhotel-vm"], "users": ["$DEPLOY_USER"] } ]
   Then create a trust credential (workload identity federation) for GitHub with the
   scope auth_keys and the tag tag:ci, limited to the repository NF-Hotel/NFHotel and
   the environment production, and give its client ID and audience to the repository
   as the environment variables TS_OIDC_CLIENT_ID and TS_OIDC_AUDIENCE.
EOF
echo "==> done"
