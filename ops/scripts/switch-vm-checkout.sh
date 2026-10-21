#!/usr/bin/env bash
# Switch the deployment folder of the VM from a checkout of NF-Hotel/DevOps to a checkout
# of NF-Hotel/NFHotel with its operations in ops/ (ADR-0016, MIL-031 task 6). S03 runs it
# once as root on nf-hotel, after the pull request of MIL-031 is merged and before the
# first CD from NFHotel.
#
# Steps, each checked before the next:
#   1. refuse unless /opt/nfhotel is a checkout of NF-Hotel/DevOps and the commit is a
#      full SHA of NF-Hotel/NFHotel that has ops/scripts/deploy-release.sh
#   2. back up the database (/usr/local/sbin/nfhotel-backup --apply)
#   3. clone NF-Hotel/NFHotel at the commit into /opt/nfhotel-next, owned by the app user
#   4. copy the git-ignored settings of the old checkout into ops/ of the new one: every
#      deploy/*/*.env, deploy/market-analysis/market-analysis.toml, deploy/nginx/tls/,
#      deploy.env and pve.env (0600, the same owner)
#   5. move /opt/nfhotel to /opt/nfhotel-devops (kept as the way back) and the new
#      checkout to /opt/nfhotel; no container is stopped or recreated, and the volumes
#      are untouched
#   6. install the deploy command again from the new checkout (install-deploy-user.sh)
# The next deploy (CD) recreates only what changed, from the new paths.
# Way back: move /opt/nfhotel-devops to /opt/nfhotel again and install the deploy command
# from it; nothing else was changed.
#
# Usage: switch-vm-checkout.sh <NFHotel commit> [--apply]
#   without --apply it prints what it would do and changes nothing
# Env:   NFHOTEL_DIR (default /opt/nfhotel), NFHOTEL_USER (default nfhotel),
#        NFHOTEL_REPO_URL (default https://github.com/NF-Hotel/NFHotel.git; for the tests),
#        NFHOTEL_BACKUP_COMMAND (default /usr/local/sbin/nfhotel-backup; for the tests),
#        NFHOTEL_INSTALL (default 1; 0 skips step 6, for the tests)
# Exit:  0 switched (or dry run); 1 a step failed; 2 usage error
# Needs: bash 4+, root, git

set -euo pipefail

app_dir="${NFHOTEL_DIR:-/opt/nfhotel}"
app_user="${NFHOTEL_USER:-nfhotel}"
repo_url="${NFHOTEL_REPO_URL:-https://github.com/NF-Hotel/NFHotel.git}"
backup="${NFHOTEL_BACKUP_COMMAND:-/usr/local/sbin/nfhotel-backup}"
install="${NFHOTEL_INSTALL:-1}"
next="$app_dir-next"
old="$app_dir-devops"

die() {
  echo "error: $*" >&2
  exit 1
}

usage_error() {
  sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//' >&2
  exit 2
}

sha="${1:-}"
apply=0
if (($# == 2)); then
  [[ "$2" == "--apply" ]] || usage_error
  apply=1
elif (($# != 1)); then
  usage_error
fi
[[ "$sha" =~ ^[0-9a-f]{40}$ ]] || usage_error

# 1. Checks.
[[ -d "$app_dir/.git" ]] || die "$app_dir is not a git checkout"
origin="$(git -c safe.directory="$app_dir" -C "$app_dir" config --get remote.origin.url)" || die "$app_dir has no origin"
[[ "$origin" =~ ^(https://github\.com/|git@github\.com:)NF-Hotel/DevOps(\.git)?$ ]] ||
  die "$app_dir is a checkout of '$origin'; this switch is only for a NF-Hotel/DevOps checkout"
[[ ! -e "$next" ]] || die "$next exists; remove it after checking it is not needed"
[[ ! -e "$old" ]] || die "$old exists; the switch was done before"
mapfile -t settings < <(cd "$app_dir" && find deploy -type f \( -name '*.env' -o -name 'market-analysis.toml' \) 2>/dev/null | sort)
[[ -f "$app_dir/deploy.env" ]] && settings+=(deploy.env)
[[ -f "$app_dir/pve.env" ]] && settings+=(pve.env)
tls=0
[[ -d "$app_dir/deploy/nginx/tls" ]] && tls=1

echo "==> switch $app_dir from NF-Hotel/DevOps to NF-Hotel/NFHotel at $sha"
echo "    settings files to copy into ops/: ${#settings[@]}$( ((tls)) && echo ', and deploy/nginx/tls/')"
if ((apply == 0)); then
  echo "==> dry run: give --apply to back up, clone into $next, copy the settings, move $app_dir to $old and $next to $app_dir"
  exit 0
fi

# 2. Backup.
"$backup" --apply || die "the backup failed; nothing was changed"

# 3. The new checkout.
echo "==> cloning $repo_url at $sha into $next"
git init -q "$next"
git -C "$next" remote add origin "$repo_url"
git -C "$next" fetch -q --depth 1 origin "$sha" || die "cannot fetch $sha from $repo_url"
git -C "$next" checkout -q --detach FETCH_HEAD
[[ -f "$next/ops/scripts/deploy-release.sh" ]] || die "$sha has no ops/scripts/deploy-release.sh; is it a commit after ADR-0016?"

# 4. The settings files, into ops/.
for f in "${settings[@]}"; do
  install -D -m 0600 "$app_dir/$f" "$next/ops/$f"
done
if ((tls)); then
  mkdir -p "$next/ops/deploy/nginx"
  cp -a "$app_dir/deploy/nginx/tls" "$next/ops/deploy/nginx/tls"
fi
chown -R "$app_user:$app_user" "$next"
echo "==> copied ${#settings[@]} settings files$( ((tls)) && echo ' and the TLS folder')"

# 5. The swap; the old checkout is kept as the way back.
mv "$app_dir" "$old"
mv "$next" "$app_dir"
echo "==> $app_dir is NF-Hotel/NFHotel at $sha; the DevOps checkout is kept in $old"

# 6. The deploy command from the new checkout.
if ((install)); then
  bash "$app_dir/ops/scripts/install-deploy-user.sh" --apply >/dev/null || die "installing the deploy command failed; see $app_dir/ops/scripts/install-deploy-user.sh"
  echo "==> installed the deploy command from $app_dir/ops"
fi
echo "==> done; the next approved CD deploys from $app_dir"
