#!/usr/bin/env bash
# Tests of scripts/switch-vm-checkout.sh (MIL-031 task 6, ADR-0016): a dry run changes
# nothing; --apply backs up first, makes an NFHotel checkout at the commit, copies the
# settings files into ops/ with mode 0600, and keeps the DevOps checkout as the way back;
# it refuses a second run, a checkout that is not of NF-Hotel/DevOps, a commit without ops/,
# and a commit that is not a full SHA. Local bare repositories stand in for GitHub.
#
# Run it in Debian 12 with the operations folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-switch-vm-checkout.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, git)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null && apt-get -qq install -y git >/dev/null 2>&1
git config --global user.email test@example.com
git config --global user.name test
git config --global init.defaultBranch main
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
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

# NFHotel with ops/ (one commit) and without it (another commit).
git init -q --bare "$W/nfhotel.git"
git -C "$W/nfhotel.git" config uploadpack.allowAnySHA1InWant true
git clone -q "$W/nfhotel.git" "$W/nfh" 2>/dev/null
echo app >"$W/nfh/README.md" && git -C "$W/nfh" add -A && git -C "$W/nfh" commit -qm "without ops"
OLD_APP="$(git -C "$W/nfh" rev-parse HEAD)"
mkdir -p "$W/nfh/ops/scripts" && echo '#!/bin/bash' >"$W/nfh/ops/scripts/deploy-release.sh"
git -C "$W/nfh" add -A && git -C "$W/nfh" commit -qm "with ops"
APP="$(git -C "$W/nfh" rev-parse HEAD)"
git -C "$W/nfh" push -q origin main

# The VM today: a DevOps checkout with its git-ignored settings.
vm_checkout() {
  rm -rf "$W/vm"
  mkdir -p "$W/vm/opt"
  git init -q "$W/vm/opt/nfhotel"
  git -C "$W/vm/opt/nfhotel" remote add origin "${1:-https://github.com/NF-Hotel/DevOps.git}"
  mkdir -p "$W/vm/opt/nfhotel/deploy/postgres" "$W/vm/opt/nfhotel/deploy/nginx/tls" "$W/vm/opt/nfhotel/deploy/market-analysis"
  echo "POSTGRES_PASSWORD=secret" >"$W/vm/opt/nfhotel/deploy/postgres/postgres.env"
  echo "[llm]" >"$W/vm/opt/nfhotel/deploy/market-analysis/market-analysis.toml"
  echo "VM=1" >"$W/vm/opt/nfhotel/deploy.env"
  echo "cert" >"$W/vm/opt/nfhotel/deploy/nginx/tls/fullchain.pem"
}
printf '#!/bin/bash\necho backup >>"%s/order"\n' "$W" >"$W/backup" && chmod +x "$W/backup"
switch() {
  NFHOTEL_DIR="$W/vm/opt/nfhotel" NFHOTEL_USER=root NFHOTEL_REPO_URL="$W/nfhotel.git" \
    NFHOTEL_BACKUP_COMMAND="$W/backup" NFHOTEL_INSTALL=0 \
    bash /src/scripts/switch-vm-checkout.sh "$@" >"$W/out" 2>&1
}

vm_checkout
switch "$APP" && [[ ! -e "$W/vm/opt/nfhotel-next" && ! -e "$W/vm/opt/nfhotel-devops" ]] &&
  ok "a dry run changes nothing" || ko "dry run: $(tail -1 "$W/out")"
: >"$W/order"
switch "$APP" --apply && ok "--apply switches" || ko "--apply: $(tail -1 "$W/out")"
[[ "$(head -1 "$W/order")" == backup ]] && ok "the backup runs first" || ko "backup order"
[[ "$(git -C "$W/vm/opt/nfhotel" rev-parse HEAD)" == "$APP" ]] && [[ -f "$W/vm/opt/nfhotel/ops/scripts/deploy-release.sh" ]] &&
  ok "the deployment folder is NFHotel at the commit" || ko "new checkout"
[[ -d "$W/vm/opt/nfhotel-devops/.git" ]] && [[ -f "$W/vm/opt/nfhotel-devops/deploy/postgres/postgres.env" ]] &&
  ok "the DevOps checkout is kept as the way back" || ko "old checkout"
[[ "$(cat "$W/vm/opt/nfhotel/ops/deploy/postgres/postgres.env")" == "POSTGRES_PASSWORD=secret" ]] &&
  [[ "$(stat -c %a "$W/vm/opt/nfhotel/ops/deploy/postgres/postgres.env")" == 600 ]] &&
  ok "a settings file is copied into ops/ with mode 0600" || ko "settings copy"
[[ "$(stat -c %a "$W/vm/opt/nfhotel/ops/deploy/market-analysis/market-analysis.toml" 2>/dev/null)" == 644 ]] &&
  ok "the settings of the market analysis stay readable by its container (0644)" || ko "market-analysis.toml mode"
[[ -f "$W/vm/opt/nfhotel/ops/deploy.env" && -f "$W/vm/opt/nfhotel/ops/deploy/nginx/tls/fullchain.pem" ]] &&
  ok "deploy.env and the TLS folder are copied" || ko "deploy.env or tls"
grep -q "secret" "$W/out" && ko "no secret is printed" || ok "no secret is printed"
switch "$APP" --apply && ko "a second switch is refused" || ok "a second switch is refused"

vm_checkout https://github.com/NF-Hotel/NFHotel.git
switch "$APP" --apply && ko "a checkout that is not of NF-Hotel/DevOps is refused" || ok "a checkout that is not of NF-Hotel/DevOps is refused"
vm_checkout
switch "$OLD_APP" --apply && ko "a commit without ops/ is refused" || ok "a commit without ops/ is refused"
[[ -d "$W/vm/opt/nfhotel/.git" && ! -e "$W/vm/opt/nfhotel-devops" ]] && [[ -f "$W/vm/opt/nfhotel/deploy/postgres/postgres.env" ]] &&
  ok "a refused commit leaves the deployment folder as it was" || ko "refused commit changed something"
rm -rf "$W/vm/opt/nfhotel-next"
switch main --apply
[[ $? == 2 ]] && ok "a commit that is not a full SHA is a usage error" || ko "usage error"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
