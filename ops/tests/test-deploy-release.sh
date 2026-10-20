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
git clone -q "$W/origin.git" "$W/other" && git -C "$W/other" remote set-url origin https://github.com/NF-Hotel/NFHotel.git
mkdir -p "$W/legacy/deployment" && echo "services: {}" >"$W/legacy/deployment/compose.yaml"
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
  grep -q '^http://127.0.0.1:80/$' "$W/urls" && ok "the smoke test uses 127.0.0.1 when no address is set" || ko "smoke test default host"
  mkdir -p "$W/app/deploy/nginx" && echo "NGINX_BIND=100.64.0.7" >"$W/app/deploy/nginx/nginx.env"
  expect_code 1 "a failed smoke test fails the deploy of B" deploy $(bad "$B")
  grep -q "rolled back to $APP" "$W/out" && [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] &&
    [[ "$(cat "$W/health")" == good ]] && ok "it rolled back to release A" || ko "rollback after failed B"
  expect_code 0 "deploys release B" deploy $(good "$B")
  grep -q '^http://100.64.0.7:80/$' "$W/urls" && grep -q '^https://100.64.0.7:443/$' "$W/urls" &&
    ok "the smoke test uses the published address of the proxy" || ko "smoke test published address"
  [[ "$(wc -l <"$W/state-app/releases.log")" == 2 ]] && ok "the release log holds two releases" || ko "release log"
  awk 'NF == 7' "$W/state-app/releases.log" | grep -q "$APP $B" && ok "the log records both commits" || ko "log fields"
  expect_code 0 "rolls back by hand" rollback
  [[ "$(git -C "$W/app" rev-parse HEAD)" == "$A" ]] && ok "the manual rollback restored A" || ko "manual rollback"
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
  [[ $? == 1 ]] && grep -q "not of NF-Hotel/DevOps" "$W/out" && ok "refuses a checkout of another repository" ||
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

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
