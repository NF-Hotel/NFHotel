#!/usr/bin/env bash
# Tests of scripts/deploy-release.sh (MIL-021 task 6, ADR-0010, ADR-0012 of NFHotel): the
# argument checks, a deploy, a failed smoke test with an automatic rollback, a manual
# rollback, the lock, and the refusal of a VM whose /opt/nfhotel is not a checkout of
# NF-Hotel/DevOps. Fakes stand in for deploy-services.sh and curl, so no container runs.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-deploy-release.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, git, flock)

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

# A DevOps repository with two commits; the VM checkout points at its GitHub URL, which
# git rewrites to the local copy (the script checks the configured URL).
git init -q --bare "$W/origin.git"
git -C "$W/origin.git" config uploadpack.allowAnySHA1InWant true
git clone -q "$W/origin.git" "$W/src" 2>/dev/null
mkdir -p "$W/src/scripts" "$W/src/deploy/nginx" "$W/bin"
cat >"$W/src/scripts/deploy-services.sh" <<EOF
echo "web=\$NFHOTEL_WEB_IMAGE" >>"$W/calls"
echo "api-db=\${NFHOTEL_API_CONNECTION_STRING:-superuser} migrate=\${NFHOTEL_API_MIGRATE_ON_START:-true}" >>"$W/calls"
if [[ "\$NFHOTEL_WEB_IMAGE" == *bbbbbbbb* ]]; then echo bad >"$W/health"; else echo good >"$W/health"; fi
EOF
git -C "$W/src" add -A && git -C "$W/src" commit -qm a
A="$(git -C "$W/src" rev-parse HEAD)"
echo 1 >"$W/src/x" && git -C "$W/src" add -A && git -C "$W/src" commit -qm b
B="$(git -C "$W/src" rev-parse HEAD)"
git -C "$W/src" push -q origin main
git clone -q "$W/origin.git" "$W/app"
git config --global url."$W/origin.git".insteadOf https://github.com/NF-Hotel/DevOps.git
git -C "$W/app" remote set-url origin https://github.com/NF-Hotel/DevOps.git
git clone -q "$W/origin.git" "$W/other" && git -C "$W/other" remote set-url origin https://github.com/NF-Hotel/Other.git
mkdir -p "$W/legacy/deployment" && echo "services: {}" >"$W/legacy/deployment/compose.yaml"

# An NFHotel repository with the operations under ops/ (ADR-0016), for the VM after its
# switch: the deploy command must run ops/scripts/deploy-services.sh of that checkout.
git init -q --bare "$W/nfh-origin.git"
git -C "$W/nfh-origin.git" config uploadpack.allowAnySHA1InWant true
git clone -q "$W/nfh-origin.git" "$W/nfh-src" 2>/dev/null
# Its first commit stands for a DevOps commit brought in with its history (files at the
# root, settings missing there), the second for the layout with ops/.
mkdir -p "$W/nfh-src/scripts"
echo 'echo "old layout: postgres.env is missing" >&2; exit 1' >"$W/nfh-src/scripts/deploy-services.sh"
git -C "$W/nfh-src" add -A && git -C "$W/nfh-src" commit -qm devops
L="$(git -C "$W/nfh-src" rev-parse HEAD)"
git -C "$W/nfh-src" rm -q -r scripts
mkdir -p "$W/nfh-src/ops/scripts" "$W/nfh-src/ops/deploy/nginx" "$W/nfh-src/src"
cat >"$W/nfh-src/ops/scripts/deploy-services.sh" <<EOF
echo "ops-layout web=\$NFHOTEL_WEB_IMAGE" >>"$W/calls"
if [[ "\$NFHOTEL_WEB_IMAGE" == *bbbbbbbb* ]]; then echo bad >"$W/health"; else echo good >"$W/health"; fi
EOF
git -C "$W/nfh-src" add -A && git -C "$W/nfh-src" commit -qm nfh
N="$(git -C "$W/nfh-src" rev-parse HEAD)"
git -C "$W/nfh-src" push -q origin main
git config --global url."$W/nfh-origin.git".insteadOf https://github.com/NF-Hotel/NFHotel.git
git clone -q "$W/nfh-origin.git" "$W/nfh"
git -C "$W/nfh" remote set-url origin https://github.com/NF-Hotel/NFHotel.git
cat >"$W/bin/curl" <<EOF
#!/bin/bash
for a in "\$@"; do case "\$a" in http*://*) echo "\$a" >>$W/urls ;; esac; done
[[ "\$(cat $W/health)" == good ]] || { echo 502; exit 0; }
for a in "\$@"; do case "\$a" in http://*) echo 301; exit 0 ;; https://*) echo 200; exit 0 ;; esac; done
EOF
chmod +x "$W/bin/curl"
# docker for the migrate command: ps finds the PostgreSQL container, exec records the
# call and the script it reads; the script "fail" makes psql fail.
cat >"$W/bin/docker" <<EOF
#!/bin/bash
case "\$1" in
  ps) echo pg123 ;;
  exec) echo "exec \$*" >>"$W/order"; cat >"$W/applied"; ! grep -qx fail "$W/applied" ;;
esac
EOF
printf '#!/bin/bash\necho backup >>"%s/order"\n' "$W" >"$W/bin/backup"
chmod +x "$W/bin/docker" "$W/bin/backup"
mkdir -p "$W/secrets"
echo 'NFHOTEL_MIGRATOR_URL=postgresql://nfhotel_migrator:x@postgres:5432/nfhotel' >"$W/secrets/nfhotel-migrator-database.env"

APP="$(printf 'c%.0s' $(seq 40))"
D="sha256:$(printf 'a%.0s' $(seq 64))"
BAD="sha256:$(printf 'b%.0s' $(seq 64))"
R=ghcr.io/nf-hotel
good() { echo "$APP $1 $R/nfhotel-web@$D $R/nfhotel-api@$D $R/market-analysis@$D $R/benchmark@$D"; }
bad() { echo "$APP $1 $R/nfhotel-web@$BAD $R/nfhotel-api@$D $R/market-analysis@$D $R/benchmark@$D"; }
run() {
  local dir="$1"
  shift
  PATH="$W/bin:$PATH" NFHOTEL_USER=root NFHOTEL_STATE="$W/state-$(basename "$dir")" NFHOTEL_DIR="$dir" \
  NFHOTEL_SECRETS="${SECRETS:-$W/secrets}" NFHOTEL_BACKUP_COMMAND="${BACKUP:-$W/bin/backup}" \
  NFHOTEL_COMMAND_DIR="${CMD_DIR:-$W/sbin}" NFHOTEL_SYSTEMD_DIR="$W/systemd" NFHOTEL_SYSTEMCTL="$W/bin/systemctl" \
    bash /src/scripts/deploy-release.sh "$@" <"${STDIN:-/dev/null}" >"$W/out" 2>&1
}
expect_code() {
  local want="$1" desc="$2"
  shift 2
  run "$W/app" "$@"
  local got=$?
  if [[ "$got" == "$want" ]]; then ok "$desc"; else ko "$desc (exit $got, wanted $want: $(head -1 "$W/out"))"; fi
}

# shellcheck disable=SC2046 # the release fields are split on purpose
{
  expect_code 2 "refuses a short NFHotel commit" deploy abc "$A" "$R/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses a short DevOps commit" deploy "$APP" main "$R/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses the images in the wrong order" deploy "$APP" "$A" "$R/nfhotel-api@$D" "$R/nfhotel-web@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses shell text in a digest" deploy "$APP" "$A" "$R/nfhotel-web@$D;id" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses another registry" deploy "$APP" "$A" "evil.io/nf-hotel/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses a dot used as a wildcard" deploy "$APP" "$A" "ghcrXio/nf-hotel/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D"
  expect_code 2 "refuses three digests" deploy "$APP" "$A" "$R/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D"
  expect_code 2 "refuses an unknown command" install

  expect_code 0 "deploys release A" deploy $(good "$A")
  [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the VM is at DevOps commit A" || ko "HEAD after A"
  grep -q '^http://127.0.0.1:80/$' "$W/urls" && ok "the smoke test uses 127.0.0.1 when no address is set" || ko "smoke test default host" # NOSONAR: the URL that the smoke test checks for its redirect
  mkdir -p "$W/app/deploy/nginx" && echo "NGINX_BIND=100.64.0.7" >"$W/app/deploy/nginx/nginx.env"
  expect_code 1 "a failed smoke test fails the deploy of B" deploy $(bad "$B")
  grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] &&
    [[ "$(cat "$W/health")" == good ]] && ok "it rolled back to release A" || ko "rollback after failed B"
  expect_code 0 "deploys release B" deploy $(good "$B")
  grep -q '^http://100.64.0.7:80/$' "$W/urls" && grep -q '^https://100.64.0.7:443/$' "$W/urls" && # NOSONAR: the URL that the smoke test checks for its redirect
    ok "the smoke test uses the published address of the proxy" || ko "smoke test published address"
  [[ "$(wc -l <"$W/state-app/releases.log")" == 2 ]] && ok "the release log holds two releases" || ko "release log"
  awk 'NF == 7' "$W/state-app/releases.log" | grep -q "$APP $B" && ok "the log records both commits" || ko "log fields"
  expect_code 0 "rolls back by hand" rollback
  [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the manual rollback restored A" || ko "manual rollback"
  # The rollback drill (MIL-021 criterion 6): B deploys and passes, is treated as failed,
  # and A comes back; the run fails and B is not recorded.
  lines="$(wc -l <"$W/state-app/releases.log")"
  expect_code 1 "a rollback drill fails the run" deploy $(good "$B") --drill
  grep -q "rollback drill: the deploy of $APP passed" "$W/out" && grep -q "rolled back to $APP" "$W/out" &&
    [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the drill rolls back to the previous release" || ko "drill: $(tail -3 "$W/out")"
  [[ "$(wc -l <"$W/state-app/releases.log")" == "$lines" ]] && ok "the drilled release is not recorded" || ko "drill recorded"
  expect_code 2 "refuses an unknown eighth argument" deploy $(good "$B") --other
}

(
  exec 9>"$W/state-app/lock"
  flock 9
  sleep 3
) &
sleep 1
expect_code 1 "refuses a second deploy while one runs" status
wait

# shellcheck disable=SC2046
{
  run "$W/legacy" deploy $(good "$A")
  [[ $? == 1 ]] && grep -q "not a git checkout" "$W/out" && [[ -f "$W/legacy/deployment/compose.yaml" ]] &&
    ok "refuses a VM without a DevOps checkout and leaves it untouched" || ko "legacy layout: $(head -1 "$W/out")"
  run "$W/other" deploy $(good "$A")
  [[ $? == 1 ]] && grep -q "not of NF-Hotel/NFHotel" "$W/out" && ok "refuses a checkout of another repository" ||
    ko "other repository: $(head -1 "$W/out")"
}

# migrate (MIL-022 task 6): the script must have the given hash; a backup comes first;
# psql runs in the PostgreSQL container with the migrator settings file.
printf 'CREATE TABLE IF NOT EXISTS t (id int);\n' >"$W/m.sql"
H="$(sha256sum "$W/m.sql" | cut -d' ' -f1)"
OTHER="$(printf 'f%.0s' $(seq 64))"
STDIN="$W/m.sql" expect_code 2 "migrate refuses a hash that is not 64 hex characters" migrate abc
STDIN="$W/m.sql" expect_code 2 "migrate refuses a script with another hash" migrate "$OTHER"
: >"$W/order"
STDIN="$W/m.sql" expect_code 0 "migrate applies the script with the right hash" migrate "$H"
[[ "$(head -1 "$W/order")" == backup ]] && grep -q "exec exec -i --env-file $W/secrets/nfhotel-migrator-database.env pg123" "$W/order" &&
  ok "migrate backs up first, then runs psql with the migrator settings" || ko "migrate order: $(tr '\n' '|' <"$W/order")"
cmp -s "$W/m.sql" "$W/applied" && ok "migrate passes the script unchanged" || ko "applied script"
BACKUP="$W/none" STDIN="$W/m.sql" expect_code 1 "migrate refuses without the backup command" migrate "$H"
SECRETS="$W/none" STDIN="$W/m.sql" expect_code 1 "migrate refuses without the migrator settings" migrate "$H"
echo fail >"$W/f.sql"
STDIN="$W/f.sql" expect_code 1 "a failing script fails migrate" migrate "$(sha256sum "$W/f.sql" | cut -d' ' -f1)"

# The API database settings (ADR-0011): superuser and migrations at start without the
# roles; nfhotel_app and no migrations at start once its settings file exists.
# shellcheck disable=SC2046
{
  : >"$W/calls"
  expect_code 0 "deploys release A before the roles exist" deploy $(good "$A")
  grep -q "api-db=superuser migrate=true" "$W/calls" && ok "without the roles the API keeps the superuser" || ko "no roles: $(tail -1 "$W/calls")"
  echo 'ConnectionStrings__HotelDatabase=Host=postgres;Port=5432;Database=nfhotel;Username=nfhotel_app;Password=p' \
    >"$W/secrets/nfhotel-api-database.env"
  : >"$W/calls"
  expect_code 0 "deploys release A with the roles" deploy $(good "$A")
  grep -q "api-db=Host=postgres;Port=5432;Database=nfhotel;Username=nfhotel_app;Password=p migrate=false" "$W/calls" &&
    ok "with the roles the API connects as nfhotel_app and does not migrate at start" || ko "roles: $(tail -1 "$W/calls")"
  grep -q "Password=p" "$W/out" && ko "the password is not printed" || ok "the password is not printed"
  echo 'ConnectionStrings__HotelDatabase=Host=postgres;Username=nfhotel;Password=p' >"$W/secrets/nfhotel-api-database.env"
  expect_code 1 "refuses an API settings file that is not for nfhotel_app" deploy $(good "$A")
  rm -f "$W/secrets/nfhotel-api-database.env"
}

# The NFHotel layout (ADR-0016): an NFHotel checkout runs ops/scripts/deploy-services.sh.
# shellcheck disable=SC2046
{
  : >"$W/calls"
  if run "$W/nfh" deploy $(good "$N") && grep -q "ops-layout web=$R/nfhotel-web@$D" "$W/calls"; then
    ok "deploys from an NFHotel checkout with ops/"
  else
    ko "NFHotel layout: $(head -1 "$W/out")"
  fi
  # The first deploy after the switch fails: the rollback goes to the last release from
  # before it, whose operations commit has no ops/. It keeps the checkout of ops/ and
  # starts the images of that release (the deploy of 1e284e2 on 2026-10-21).
  rm -rf "$W/state-nfh"
  if run "$W/nfh" deploy $(good "$L") && [[ "$(git -C "$W/nfh" rev-parse HEAD)" == "$N" ]]; then
    ok "a release from before the switch keeps the checkout of ops/"
  else
    ko "pre-switch release: $(tail -2 "$W/out")"
  fi
  run "$W/nfh" deploy $(bad "$N")
  grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/nfh" rev-parse HEAD)" == "$N" ]] &&
    [[ "$(cat "$W/health")" == good ]] && ok "the rollback across the switch starts the old images from ops/" ||
    ko "rollback across the switch: $(tail -3 "$W/out")"
}

# update (ADR-0018): the commands are installed from a commit on main, never from a commit
# that is only on a pull-request ref, and only when they differ.
# shellcheck disable=SC2046
{
  mkdir -p "$W/sbin" "$W/systemd" "$W/nfh-src/ops/deploy/systemd"
  printf '#!/bin/bash\necho "systemctl $*" >>"%s/systemctl.log"\n' "$W" >"$W/bin/systemctl" && chmod +x "$W/bin/systemctl"
  echo 'echo deploy v1' >"$W/nfh-src/ops/scripts/deploy-release.sh"
  echo 'echo backup v1' >"$W/nfh-src/ops/scripts/backup-database.sh"
  echo '[Service]' >"$W/nfh-src/ops/deploy/systemd/nfhotel-backup.service"
  echo '[Timer]' >"$W/nfh-src/ops/deploy/systemd/nfhotel-backup.timer"
  echo 'echo renew v1' >"$W/nfh-src/ops/scripts/renew-certificate.sh"
  echo '[Service] cert' >"$W/nfh-src/ops/deploy/systemd/nfhotel-cert-renew.service"
  echo '[Timer] cert' >"$W/nfh-src/ops/deploy/systemd/nfhotel-cert-renew.timer"
  git -C "$W/nfh-src" add -A && git -C "$W/nfh-src" commit -qm commands
  U1="$(git -C "$W/nfh-src" rev-parse HEAD)"
  git -C "$W/nfh-src" push -q origin main
  echo 'echo deploy unreviewed' >"$W/nfh-src/ops/scripts/deploy-release.sh"
  git -C "$W/nfh-src" commit -qam unreviewed
  P="$(git -C "$W/nfh-src" rev-parse HEAD)"
  git -C "$W/nfh-src" push -q origin "HEAD:refs/pull/1/head"
  git -C "$W/nfh-src" reset -q --hard "$U1"

  expect_code 2 "update refuses a short commit" update abc
  run "$W/nfh" update "$U1" && [[ "$(cat "$W/sbin/nfhotel-deploy")" == "echo deploy v1" ]] &&
    [[ "$(cat "$W/sbin/nfhotel-backup")" == "echo backup v1" ]] && [[ -f "$W/systemd/nfhotel-backup.timer" ]] &&
    [[ "$(stat -c %a "$W/sbin/nfhotel-deploy")" == 755 ]] && ok "update installs the commands and units of a main commit" ||
    ko "update from main: $(tail -2 "$W/out")"
  grep -q "try-restart nfhotel-backup.timer" "$W/systemctl.log" && ok "new units reload systemd and restart the timer" || ko "systemctl"
  [[ "$(cat "$W/sbin/nfhotel-cert-renew")" == "echo renew v1" ]] && [[ -f "$W/systemd/nfhotel-cert-renew.timer" ]] &&
    grep -q "enable --now nfhotel-cert-renew.timer" "$W/systemctl.log" &&
    ok "update installs the certificate renewal and enables its timer" || ko "certificate renewal: $(cat "$W/systemctl.log")"
  [[ "$(grep -c " update $U1 " "$W/state-nfh/releases.log")" == 7 ]] && ok "each installed file is logged with its hashes" || ko "update log"
  : >"$W/systemctl.log"
  lines="$(wc -l <"$W/state-nfh/releases.log")"
  run "$W/nfh" update "$U1" && [[ "$(wc -l <"$W/state-nfh/releases.log")" == "$lines" ]] && [[ ! -s "$W/systemctl.log" ]] &&
    ok "a second update with equal files changes and logs nothing" || ko "idempotent update: $(tail -2 "$W/out")"
  run "$W/nfh" update "$P"
  [[ $? == 2 ]] && grep -q "not on main" "$W/out" && [[ "$(cat "$W/sbin/nfhotel-deploy")" == "echo deploy v1" ]] &&
    ok "update refuses a commit that is only on a pull-request ref" || ko "unreviewed commit: $(tail -2 "$W/out")"

  echo 'echo deploy v2' >"$W/nfh-src/ops/scripts/deploy-release.sh"
  git -C "$W/nfh-src" commit -qam v2 && git -C "$W/nfh-src" push -q origin main
  U2="$(git -C "$W/nfh-src" rev-parse HEAD)"
  run "$W/nfh" update "$U2" && [[ "$(cat "$W/sbin/nfhotel-deploy")" == "echo deploy v2" ]] &&
    [[ "$(cat "$W/sbin/nfhotel-deploy.previous")" == "echo deploy v1" ]] && [[ ! -s "$W/systemctl.log" ]] &&
    ok "a changed command is installed and the old one kept as .previous" || ko "update v2: $(tail -2 "$W/out")"

  # The update lines in the release log do not hide the releases from the rollback.
  run "$W/nfh" deploy $(good "$U2")
  run "$W/nfh" update "$U2"
  run "$W/nfh" deploy $(bad "$U2")
  grep -q "rolled back to $APP" "$W/out" && [[ "$(cat "$W/health")" == good ]] &&
    ok "the rollback finds the release past the update lines" || ko "rollback after update: $(tail -3 "$W/out")"

  # The VM checkout is shallow (depth 1). update still finds a newer main commit, and still
  # refuses a commit that is only on a pull-request ref.
  git clone -q --depth 1 "file://$W/nfh-origin.git" "$W/nfhs"
  git -C "$W/nfhs" remote set-url origin https://github.com/NF-Hotel/NFHotel.git
  echo 'echo deploy v3' >"$W/nfh-src/ops/scripts/deploy-release.sh"
  git -C "$W/nfh-src" commit -qam v3 && git -C "$W/nfh-src" push -q origin main
  U3="$(git -C "$W/nfh-src" rev-parse HEAD)"
  run "$W/nfhs" update "$U3" && [[ "$(cat "$W/sbin/nfhotel-deploy")" == "echo deploy v3" ]] &&
    ok "update works in a shallow checkout" || ko "shallow update: $(tail -2 "$W/out")"
  run "$W/nfhs" update "$P"
  [[ $? == 2 ]] && [[ "$(cat "$W/sbin/nfhotel-deploy")" == "echo deploy v3" ]] &&
    ok "a shallow checkout also refuses a pull-request commit" || ko "shallow refusal: $(tail -2 "$W/out")"

  # A failed install stops update with an error, though it runs inside a condition where
  # set -e does not apply, and update leaves no temporary file behind.
  echo 'echo deploy v4' >"$W/nfh-src/ops/scripts/deploy-release.sh"
  git -C "$W/nfh-src" commit -qam v4 && git -C "$W/nfh-src" push -q origin main
  U4="$(git -C "$W/nfh-src" rev-parse HEAD)"
  before="$(find /tmp -maxdepth 1 -name 'tmp.*' | wc -l)"
  CMD_DIR="$W/missing" run "$W/nfh" update "$U4"
  [[ $? == 1 ]] && grep -q "cannot install $W/missing/nfhotel-deploy" "$W/out" &&
    ok "a failed install fails update with its reason" || ko "failed install: $(tail -2 "$W/out")"
  [[ "$(find /tmp -maxdepth 1 -name 'tmp.*' | wc -l)" == "$before" ]] &&
    ok "update removes its temporary folder" || ko "temporary files left in /tmp"
}

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
