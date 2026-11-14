#!/usr/bin/env bash
# Tests of scripts/deploy-release.sh (MIL-021 task 6, ADR-0010, ADR-0012 of NFHotel): the
# argument checks, a deploy, a failed smoke test with an automatic rollback, a manual
# rollback, the lock, the refusal of a VM whose /opt/nfhotel is not a checkout of
# NF-Hotel/DevOps, the prune of the images of old releases (MIL-062), and the references by
# name with the patched proxy images, including the rollback to a release line from before each
# of them (ADR-0040, MIL-064 task 4, MIL-065 task 4), and the runtime check of a release with the
# rollback of a release whose containers break their profiles (MIL-080). Fakes stand in for
# deploy-services.sh, the runtime check, curl and docker, so no container runs.
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
echo "nginx=\${NFHOTEL_NGINX_IMAGE:-the compose file}" >>"$W/calls"
echo "socket-proxy=\${NFHOTEL_SOCKET_PROXY_IMAGE:-the compose file}" >>"$W/calls"
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
# A proxy that is not listening yet: the first \$(cat $W/not-yet) calls get no answer.
n=\$(cat $W/not-yet 2>/dev/null || echo 0)
if ((n > 0)); then echo \$((n - 1)) >$W/not-yet; echo 000; exit 0; fi
[[ "\$(cat $W/health)" == good ]] || { echo 502; exit 0; }
for a in "\$@"; do case "\$a" in http://*) echo 301; exit 0 ;; https://*) echo 200; exit 0 ;; esac; done
EOF
chmod +x "$W/bin/curl"
# docker for the migrate command: ps finds the PostgreSQL container, exec records the
# call and the SQL that its n-th call reads (applied.<n>); the SQL "fail" makes psql fail.
# For the prune (MIL-062):
# ps -aq lists the containers of $W/containers (name and image id per line), inspect gives
# their image ids, image ls lists $W/images, image inspect resolves a release image to its id
# from $W/image-refs, and image rm records the id in $W/removed and fails for an id listed in
# $W/unremovable.
cat >"$W/bin/docker" <<EOF
#!/bin/bash
case "\$1" in
  ps) if [[ "\$2" == -aq ]]; then cut -d' ' -f1 "$W/containers" 2>/dev/null; else echo pg123; fi ;;
  inspect) shift 3; for c in "\$@"; do awk -v c="\$c" '\$1 == c { print \$2 }' "$W/containers" 2>/dev/null; done ;;
  image) case "\$2" in
    ls) cat "$W/images" 2>/dev/null ;;
    inspect) awk -v r="\${!#}" '\$1 == r { print \$2 }' "$W/image-refs" 2>/dev/null ;;
    rm) echo "\${!#}" >>"$W/removed"; ! grep -qx "\${!#}" "$W/unremovable" 2>/dev/null ;;
    esac ;;
  exec) echo "exec \$*" >>"$W/order"; n="\$(grep -c '^exec' "$W/order")"; cat >"$W/applied.\$n"; ! grep -qx fail "$W/applied.\$n" ;;
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
# The four references that every release has, and the full release of today with both proxies.
four() { echo "$R/nfhotel-web@$1 $R/nfhotel-api@$D $R/market-analysis@$D $R/benchmark@$D"; }
good() { echo "$APP $1 $(four "$D") $R/nfhotel-nginx@$D $R/nfhotel-socket-proxy@$D"; }
bad() { echo "$APP $1 $(four "$BAD") $R/nfhotel-nginx@$D $R/nfhotel-socket-proxy@$D"; }
run() {
  local dir="$1"
  shift
  PATH="$W/bin:$PATH" NFHOTEL_USER=root NFHOTEL_STATE="$W/state-$(basename "$dir")" NFHOTEL_DIR="$dir" \
  NFHOTEL_SMOKE_WAIT=0 NFHOTEL_SECRETS="${SECRETS:-$W/secrets}" NFHOTEL_BACKUP_COMMAND="${BACKUP:-$W/bin/backup}" \
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
  NGINX="$R/nfhotel-nginx@$D"
  PROXY="$R/nfhotel-socket-proxy@$D"
  expect_code 2 "refuses a short NFHotel commit" deploy abc "$A" $(four "$D") "$NGINX" "$PROXY"
  expect_code 2 "refuses a short DevOps commit" deploy "$APP" main $(four "$D") "$NGINX" "$PROXY"
  expect_code 2 "refuses shell text in a digest" deploy "$APP" "$A" "$R/nfhotel-web@$D;id" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D" "$NGINX" "$PROXY"
  expect_code 2 "refuses another registry" deploy "$APP" "$A" "evil.io/nf-hotel/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D" "$NGINX" "$PROXY"
  expect_code 2 "refuses a dot used as a wildcard" deploy "$APP" "$A" "ghcrXio/nf-hotel/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D" "$R/benchmark@$D" "$NGINX" "$PROXY"
  expect_code 2 "refuses three digests" deploy "$APP" "$A" "$R/nfhotel-web@$D" "$R/nfhotel-api@$D" "$R/market-analysis@$D"
  # The patched proxies (ADR-0040, MIL-065): a release names each, once, with a digest of its pattern.
  expect_code 2 "refuses a release without the nginx digest" deploy "$APP" "$A" $(four "$D") "$PROXY"
  grep -q "no reference of nfhotel-nginx" "$W/out" && ok "the refusal names the missing nginx digest" || ko "missing nginx: $(head -1 "$W/out")"
  expect_code 2 "refuses a release without the socket proxy digest" deploy "$APP" "$A" $(four "$D") "$NGINX"
  grep -q "no reference of nfhotel-socket-proxy" "$W/out" && ok "the refusal names the missing socket proxy digest" ||
    ko "missing socket proxy: $(head -1 "$W/out")"
  expect_code 2 "refuses a malformed nginx digest" deploy "$APP" "$A" $(four "$D") "$R/nfhotel-nginx@sha256:abc" "$PROXY"
  expect_code 2 "refuses a malformed socket proxy digest" deploy "$APP" "$A" $(four "$D") "$NGINX" "$R/nfhotel-socket-proxy@sha256:abc"
  expect_code 2 "refuses an image that is not of the release" deploy "$APP" "$A" $(four "$D") "$R/nfhotel-proxy@$D" "$PROXY"
  expect_code 2 "refuses an image named twice" deploy "$APP" "$A" $(four "$D") "$NGINX" "$PROXY" "$R/nfhotel-web@$D"
  grep -q "names nfhotel-web a second time" "$W/out" && ok "the refusal names the image given twice" || ko "twice: $(head -1 "$W/out")"
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
  tail -n 1 "$W/state-app/releases.log" | grep -q " $APP $B $R/nfhotel-web@$D .* $R/nfhotel-nginx@$D $R/nfhotel-socket-proxy@$D$" &&
    ok "the log records both commits and the digests of both proxies" || ko "log fields: $(tail -n 1 "$W/state-app/releases.log")"
  expect_code 0 "rolls back by hand" rollback
  [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the manual rollback restored A" || ko "manual rollback"
  # The rollback drill (MIL-021 criterion 6): B deploys and passes, is treated as failed,
  # and A comes back; the run fails and B is not recorded.
  lines="$(wc -l <"$W/state-app/releases.log")"
  expect_code 1 "a rollback drill fails the run" deploy $(good "$B") --drill
  grep -q "rollback drill: the deploy of $APP passed" "$W/out" && grep -q "rolled back to $APP" "$W/out" &&
    [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the drill rolls back to the previous release" || ko "drill: $(tail -3 "$W/out")"
  [[ "$(wc -l <"$W/state-app/releases.log")" == "$lines" ]] && ok "the drilled release is not recorded" || ko "drill recorded"
  expect_code 2 "refuses an unknown last argument" deploy $(good "$B") --other
  # A proxy that was just recreated (MIL-023 task 2) answers after a few seconds: the smoke
  # test tries again before it counts the deploy as failed.
  echo 2 >"$W/not-yet"
  expect_code 0 "a proxy that answers on the third try passes the smoke test" deploy $(good "$B")
  echo 99 >"$W/not-yet"
  expect_code 1 "a proxy that never answers fails the deploy" deploy $(good "$A")
  rm -f "$W/not-yet"
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
cmp -s "$W/m.sql" "$W/applied.1" && ok "migrate passes the script unchanged" || ko "applied script"
[[ "$(grep -c "^exec exec -i --env-file $W/secrets/nfhotel-migrator-database.env pg123" "$W/order")" == 2 ]] &&
  grep -qxF 'REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM nfhotel_app;' "$W/applied.2" &&
  ok "then the migrator takes INSERT, UPDATE and DELETE on the history back from nfhotel_app" ||
  ko "the second call: $(cat "$W/applied.2" 2>/dev/null)"
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
  echo '[Service] copy' >"$W/nfh-src/ops/deploy/systemd/nfhotel-offvm-copy.service"
  echo '[Timer] copy' >"$W/nfh-src/ops/deploy/systemd/nfhotel-offvm-copy.timer"
  echo 'echo renew v1' >"$W/nfh-src/ops/scripts/renew-certificate.sh"
  echo '[Service] cert' >"$W/nfh-src/ops/deploy/systemd/nfhotel-cert-renew.service"
  echo '[Timer] cert' >"$W/nfh-src/ops/deploy/systemd/nfhotel-cert-renew.timer"
  echo 'echo health v1' >"$W/nfh-src/ops/scripts/report-health.sh"
  echo '[Service] health' >"$W/nfh-src/ops/deploy/systemd/nfhotel-report-health.service"
  echo '[Timer] health' >"$W/nfh-src/ops/deploy/systemd/nfhotel-report-health.timer"
  echo 'echo check v1' >"$W/nfh-src/ops/scripts/check-container-runtime.sh"
  echo '[Service] check' >"$W/nfh-src/ops/deploy/systemd/nfhotel-container-check.service"
  echo '[Timer] check' >"$W/nfh-src/ops/deploy/systemd/nfhotel-container-check.timer"
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
  [[ "$(cat "$W/systemd/nfhotel-offvm-copy.service")" == "[Service] copy" ]] &&
    [[ "$(cat "$W/systemd/nfhotel-offvm-copy.timer")" == "[Timer] copy" ]] &&
    grep -qx "systemctl enable --now nfhotel-offvm-copy.timer" "$W/systemctl.log" &&
    ok "update installs the hourly copy off the VM and starts its timer (MIL-074)" || ko "hourly copy: $(cat "$W/systemctl.log")"
  [[ "$(cat "$W/sbin/nfhotel-cert-renew")" == "echo renew v1" ]] && [[ -f "$W/systemd/nfhotel-cert-renew.timer" ]] &&
    grep -q "enable --now nfhotel-cert-renew.timer" "$W/systemctl.log" &&
    ok "update installs the certificate renewal and enables its timer" || ko "certificate renewal: $(cat "$W/systemctl.log")"
  [[ "$(cat "$W/sbin/nfhotel-report-health")" == "echo health v1" ]] && [[ -f "$W/systemd/nfhotel-report-health.timer" ]] &&
    grep -q "enable --now nfhotel-report-health.timer" "$W/systemctl.log" &&
    ok "update installs the health report and enables its timer" || ko "health report: $(cat "$W/systemctl.log")"
  [[ "$(cat "$W/sbin/nfhotel-container-check")" == "echo check v1" ]] && [[ -f "$W/systemd/nfhotel-container-check.timer" ]] &&
    grep -q "enable --now nfhotel-container-check.timer" "$W/systemctl.log" &&
    ok "update installs the runtime check and enables its timer" || ko "runtime check: $(cat "$W/systemctl.log")"
  [[ "$(grep -c " update $U1 " "$W/state-nfh/releases.log")" == 15 ]] && ok "each installed file is logged with its hashes" || ko "update log"
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

# The references by name (ADR-0040, MIL-064 task 4, MIL-065 task 4), on a checkout of its own: each
# proxy gets its digest of the release, the references may come in any order, and a release line
# from before a proxy image (four references, or five without the socket proxy) still rolls back, by
# hand and after a failed deploy: no image variable is set for it, so the compose file of that
# operations commit names the upstream image.
echo "== references by name (ADR-0040)"
git clone -q "$W/origin.git" "$W/app4" && git -C "$W/app4" remote set-url origin https://github.com/NF-Hotel/DevOps.git
mkdir -p "$W/state-app4"
echo "2026-11-01T00:00:00Z $APP $A $(four "$D")" >"$W/state-app4/releases.log"
# shellcheck disable=SC2046 # the release fields are split on purpose
{
  : >"$W/calls"
  run "$W/app4" deploy $(good "$B") && grep -qx "nginx=$R/nfhotel-nginx@$D" "$W/calls" &&
    ok "a deploy passes the nginx digest to the proxy" || ko "nginx variable: $(tr '\n' '|' <"$W/calls")"
  grep -qx "socket-proxy=$R/nfhotel-socket-proxy@$D" "$W/calls" &&
    ok "a deploy passes the socket proxy digest to Alloy" || ko "socket proxy variable: $(tr '\n' '|' <"$W/calls")"
  : >"$W/calls"
  run "$W/app4" rollback && [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$A" ]] &&
    grep -qx "nginx=the compose file" "$W/calls" && grep -qx "socket-proxy=the compose file" "$W/calls" &&
    ok "a rollback to a line with four references starts both proxies from the compose files of that release" ||
    ko "rollback to four references: $(tail -2 "$W/out") $(tr '\n' '|' <"$W/calls")"
  [[ "$(tail -n 1 "$W/state-app4/releases.log" | wc -w)" == 7 ]] &&
    ok "the rollback records the line with its four references" || ko "rollback line: $(tail -n 1 "$W/state-app4/releases.log")"
  : >"$W/calls"
  run "$W/app4" deploy $(bad "$B")
  [[ $? == 1 ]] && grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$A" ]] &&
    [[ "$(grep "^nginx=" "$W/calls" | tail -n 1)" == "nginx=the compose file" ]] && [[ "$(cat "$W/health")" == good ]] &&
    ok "a failed deploy rolls back to a line with four references" || ko "failed deploy to four references: $(tail -3 "$W/out")"
  run "$W/app4" deploy "$APP" "$B" "$R/nfhotel-nginx@$D" "$R/benchmark@$D" "$R/nfhotel-socket-proxy@$D" "$R/market-analysis@$D" "$R/nfhotel-api@$D" "$R/nfhotel-web@$D" &&
    [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$B" ]] && ok "a deploy takes the references in any order" || ko "any order: $(head -1 "$W/out")"
  run "$W/app4" deploy $(good "$A") --drill
  [[ $? == 1 ]] && grep -q "rollback drill: the deploy of $APP passed" "$W/out" && [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$B" ]] &&
    ok "a drill with six references rolls back to the release before it" || ko "drill: $(tail -3 "$W/out")"

  # A release line of MIL-064 has five references, without the socket proxy (MIL-065): its rollback
  # passes the nginx digest and starts the socket proxy from the compose file of that release.
  echo "2026-11-08T00:00:00Z $APP $A $(four "$D") $R/nfhotel-nginx@$D" >"$W/state-app4/releases.log"
  run "$W/app4" deploy $(good "$B") || ko "deploy before the rollback to five references: $(tail -2 "$W/out")"
  : >"$W/calls"
  run "$W/app4" rollback && [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$A" ]] &&
    grep -qx "nginx=$R/nfhotel-nginx@$D" "$W/calls" && grep -qx "socket-proxy=the compose file" "$W/calls" &&
    ok "a rollback to a line with five references passes the nginx digest and leaves the socket proxy to the compose file" ||
    ko "rollback to five references: $(tail -2 "$W/out") $(tr '\n' '|' <"$W/calls")"
  [[ "$(tail -n 1 "$W/state-app4/releases.log" | wc -w)" == 8 ]] &&
    ok "the rollback records the line with its five references" || ko "rollback line: $(tail -n 1 "$W/state-app4/releases.log")"
  : >"$W/calls"
  run "$W/app4" deploy $(bad "$B")
  [[ $? == 1 ]] && grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/app4" rev-parse HEAD)" == "$A" ]] &&
    [[ "$(grep "^socket-proxy=" "$W/calls" | tail -n 1)" == "socket-proxy=the compose file" ]] && [[ "$(cat "$W/health")" == good ]] &&
    ok "a failed deploy rolls back to a line with five references" || ko "failed deploy to five references: $(tail -3 "$W/out")"
}

# The prune of MIL-062, on a checkout of its own: after a deploy that passed, the images of the
# containers and of the two latest releases stay and every other image goes; a rollback, a drill
# and the dry run remove nothing; an image that cannot be removed is a warning, not a failed deploy.
echo "== prune (MIL-062)"
git clone -q "$W/origin.git" "$W/app3" && git -C "$W/app3" remote set-url origin https://github.com/NF-Hotel/DevOps.git
rel() { echo "$APP $1 $R/nfhotel-web@$2 $R/nfhotel-api@$2 $R/market-analysis@$2 $R/benchmark@$2 $R/nfhotel-nginx@$2 $R/nfhotel-socket-proxy@$2"; }
D1="sha256:$(printf '1%.0s' $(seq 64))"
D2="sha256:$(printf '2%.0s' $(seq 64))"
D3="sha256:$(printf '3%.0s' $(seq 64))"
: >"$W/image-refs"
for i in 1 2 3; do
  v="D$i"
  d="${!v}"
  for name in web api ana ben ngx spx; do
    case "$name" in
      web) repo=nfhotel-web ;; api) repo=nfhotel-api ;; ana) repo=market-analysis ;; ben) repo=benchmark ;;
      ngx) repo=nfhotel-nginx ;; spx) repo=nfhotel-socket-proxy ;;
    esac
    echo "$R/$repo@$d id-$name$i" >>"$W/image-refs"
  done
done
printf '%s\n' id-web1 id-api1 id-ana1 id-ben1 id-ngx1 id-spx1 id-web2 id-api2 id-ana2 id-ben2 id-ngx2 id-spx2 id-web3 id-api3 \
  id-ana3 id-ben3 id-ngx3 id-spx3 id-postgres-used id-old-nginx id-old-socket-proxy >"$W/images"
# The containers run release 3 and a third-party image in use; a stopped one-shot container (results-owner) counts
# too. id-old-nginx and id-old-socket-proxy are the upstream images of the proxies from before ADR-0040 and MIL-065,
# which no container uses any more.
printf '%s\n' 'c1 id-web3' 'c2 id-api3' 'c3 id-ana3' 'c4 id-ben3' 'c5 id-postgres-used' 'c6 id-ben3' 'c7 id-ngx3' 'c8 id-spx3' >"$W/containers"
# shellcheck disable=SC2046 # the release fields are split on purpose
{
  expect_code 0 "deploys release 1" deploy $(rel "$A" "$D1")
  expect_code 0 "deploys release 2" deploy $(rel "$A" "$D2")
  rm -f "$W/removed"
  expect_code 0 "deploys release 3 and prunes" deploy $(rel "$B" "$D3")
  [[ "$(sort "$W/removed" 2>/dev/null | tr '\n' ' ')" == "id-ana1 id-api1 id-ben1 id-ngx1 id-old-nginx id-old-socket-proxy id-spx1 id-web1 " ]] &&
    ok "the images of release 1 and the unused upstream proxies go; releases 2 and 3, their proxies and the containers' images stay" ||
    ko "pruned set: $(sort "$W/removed" 2>/dev/null | tr '\n' ' ')"
  grep -q "prune: removed 8 image(s)" "$W/out" && ok "the deploy log counts the removed images" || ko "prune log: $(grep prune "$W/out")"
  rm -f "$W/removed"
  expect_code 0 "prune without --apply is a dry run" prune
  [[ ! -f "$W/removed" ]] && grep -q "would remove id-old-socket-proxy" "$W/out" && grep -q "8 image(s) would be removed" "$W/out" &&
    ok "the dry run lists what would go and removes nothing" || ko "dry run: $(tail -3 "$W/out")"
  expect_code 2 "prune refuses another argument" prune --now
  expect_code 0 "prune --apply removes by hand" prune --apply
  [[ "$(sort "$W/removed" | wc -l)" == 8 ]] && ok "prune --apply removes the same eight" || ko "prune --apply: $(cat "$W/removed")"
  rm -f "$W/removed"
  expect_code 1 "a failed deploy rolls back" deploy $(bad "$B")
  [[ ! -f "$W/removed" ]] && ok "a failed deploy and its rollback prune nothing" || ko "pruned after a rollback: $(cat "$W/removed")"
  expect_code 1 "a drill fails the run" deploy $(rel "$B" "$D3") --drill
  [[ ! -f "$W/removed" ]] && ok "a drill prunes nothing" || ko "pruned after a drill: $(cat "$W/removed")"
  echo id-old-nginx >"$W/unremovable"
  expect_code 0 "a deploy stays green when an image cannot be removed" deploy $(rel "$B" "$D3")
  grep -q "warning: cannot remove the image id-old-nginx" "$W/out" && grep -q "the prune did not finish; the release is deployed" "$W/out" &&
    ok "the failed removal is a warning in the log" || ko "warning: $(grep -i warning "$W/out")"
  expect_code 1 "prune --apply fails when an image cannot be removed" prune --apply
  rm -f "$W/unremovable"
}

# The runtime check (MIL-080): the deploy runs the check of the release with --deploy once the
# containers are up, and a release whose containers break their profiles is rolled back. Releases A
# and B are from before the check and deploy without it.
echo "== the runtime check (MIL-080)"
cat >"$W/src/scripts/check-container-runtime.sh" <<EOF
echo "check \$*" >>"$W/calls"
if [[ -e "$W/violation" ]]; then echo "FAIL  t-web  nfhotel-web/nfhotel-web  privileged"; exit 1; fi
EOF
git -C "$W/src" add -A && git -C "$W/src" commit -qm c && git -C "$W/src" push -q origin main
C="$(git -C "$W/src" rev-parse HEAD)"
# shellcheck disable=SC2046 # the release fields are split on purpose
{
  expect_code 0 "deploys release B, which has no runtime check" deploy $(good "$B")
  : >"$W/calls"
  touch "$W/violation"
  expect_code 1 "a container that breaks its profile fails the deploy of C" deploy $(good "$C")
  grep -q "FAIL  t-web" "$W/out" && grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/app" rev-parse HEAD)" == "$B" ]] &&
    ok "the deploy log names the container, and B comes back" || ko "violation: $(tail -3 "$W/out")"
  [[ "$(grep -c '^check' "$W/calls")" == 1 ]] && ok "the rollback to B, which has no check, runs none" ||
    ko "checks: $(grep '^check' "$W/calls" | tr '\n' '|')"
  rm -f "$W/violation"
  : >"$W/calls"
  : >"$W/systemctl.log"
  expect_code 0 "deploys release C once its containers keep their profiles" deploy $(good "$C")
  grep -qx "check --deploy" "$W/calls" && ok "the deploy runs the check of the release with --deploy" ||
    ko "check call: $(tr '\n' '|' <"$W/calls")"
  [[ "$(grep -cx "systemctl start nfhotel-container-check.service" "$W/systemctl.log")" == 1 ]] &&
    grep -q "^==> the container check service passed" "$W/out" &&
    ok "after the release is recorded, the deploy runs the service of the timer once" ||
    ko "service run: $(cat "$W/systemctl.log") $(tail -2 "$W/out")"
  # shellcheck disable=SC2016 # the fake systemctl fails a start: $1 is its own first argument
  printf '#!/bin/bash\necho "systemctl $*" >>"%s/systemctl.log"\n[[ "$1" != start ]]\n' "$W" >"$W/bin/systemctl"
  expect_code 0 "a failed run of the service leaves the deploy green" deploy $(good "$C")
  grep -q "warning: the container check service failed" "$W/out" && ok "the failed run is a warning in the log" ||
    ko "service warning: $(tail -2 "$W/out")"
  printf '#!/bin/bash\necho "systemctl $*" >>"%s/systemctl.log"\n' "$W" >"$W/bin/systemctl"
}

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
