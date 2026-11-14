#!/usr/bin/env bash
# End-to-end test of the PostgreSQL preparation (MIL-024, ADR-0011) against PostgreSQL 16
# with the real migrations of NF-Hotel/NFHotel and its API image: roles and privileges,
# the ownership transfer without data change, the internal network, the migrator, the
# API through the alias postgres, backup as nfhotel_backup, read back to its end with the
# tables of the database while it is made (MIL-076), and the restore drill; and the
# migration history, which nfhotel_app may read and not change, both on a database migrated
# before its roles and on a new one whose first migration runs through the migrate step of
# deploy-release.sh (MIL-066 task 4). Last, the rebuild of the VM in the order of the wiki page
# Disaster-Recovery: CD before the restore stops at its backup, and the dump restored into an
# empty server, the roles with the passwords of the restored settings, CD and the team roles,
# in that order, bring back the rows, the first manager and the logins (MIL-066 task 9).
#
# Usage: test-database.sh <migrations.sql> <API image>
#   migrations.sql  dotnet ef migrations script --idempotent from an NFHotel checkout
#   API image       built from Dockerfile.api of the same checkout
# It starts the containers t-pg and t-api, the volume t-pgdata and the networks t-prodnet,
# t-apinet and nfhotel-db on the local Docker, and removes them at the end.
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, docker, openssl, age, sha256sum, flock

set -uo pipefail

[[ $# == 2 && -f "$1" ]] || {
  echo "usage: test-database.sh <migrations.sql> <API image>" >&2
  exit 2
}
MIGRATIONS="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
readonly MIGRATIONS API_IMAGE="$2"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO PG=t-pg API=t-api IMG=postgres:16-alpine CURL=curlimages/curl:8.10.1
W="$(mktemp -d)"
readonly W
pass=0
fail=0

cleanup() {
  docker rm -f "$API" "$PG" >/dev/null 2>&1
  docker volume rm t-pgdata >/dev/null 2>&1
  docker network rm nfhotel-db t-prodnet t-apinet >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT
cleanup
mkdir -p "$W/bin" "$W/backups" "$W/etc"

ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}
# check <description> <command...>: passes when the command succeeds.
check() {
  local desc="$1"
  shift
  if "$@" >"$W/o" 2>&1; then ok "$desc"; else ko "$desc: $(head -c 300 "$W/o")"; fi
}
# refused <description> <command...>: passes when the command fails with a permission error.
refused() {
  local desc="$1"
  shift
  if "$@" >"$W/o" 2>&1; then
    ko "$desc (was allowed)"
  elif grep -qiE "permission denied|must be owner|not permitted|denied" "$W/o"; then
    ok "$desc"
  else
    ko "$desc: unexpected error $(head -c 200 "$W/o")"
  fi
}
su_sql() { docker exec -i "$PG" psql -X -At -v ON_ERROR_STOP=1 -U nfhotel -d nfhotel -c "$1"; }
counts() {
  su_sql "SELECT (SELECT count(*) FROM room)||'/'||(SELECT count(*) FROM guest)||'/'||(SELECT count(*) FROM booking)||'/'||(SELECT string_agg(migration_id, chr(44) ORDER BY 1) FROM \"__EFMigrationsHistory\")"
}
prepare() { NFHOTEL_BACKUP_COMMAND="$W/bin/nfhotel-backup" bash "$REPO/scripts/prepare-database-roles.sh" --container "$PG" --secrets-dir "$W/secrets" "$@"; }
verify() { bash "$REPO/scripts/verify-database.sh" --container "$PG" "$@"; }
same() { [[ "$1" == "$2" ]]; }
apply_migrations() { docker exec -i "$PG" psql -X -q -v ON_ERROR_STOP=1 -U nfhotel -d nfhotel -f - <"$MIGRATIONS"; }
# add_rooms: the rooms R1 to R3, as the superuser.
add_rooms() { su_sql "INSERT INTO room(room_number,floor,size,capacity,status,housekeeping_status) SELECT 'R'||i, 1, 20, 2, 0, 0 FROM generate_series(1,3) i"; }
# start_pg [docker run option...]: a PostgreSQL 16 on t-prodnet with the volume t-pgdata, up
# when its real server answers: only that one listens on TCP, the temporary init server uses
# the socket only.
start_pg() {
  docker run -d --name "$PG" --network t-prodnet "$@" -e POSTGRES_USER=nfhotel -e POSTGRES_PASSWORD=superpw \
    -e POSTGRES_DB=nfhotel -v t-pgdata:/var/lib/postgresql/data "$IMG" >/dev/null || return 1
  for _ in $(seq 60); do
    docker exec "$PG" pg_isready -h 127.0.0.1 -U nfhotel -d nfhotel >/dev/null 2>&1 && return 0
    sleep 1
  done
  return 1
}

echo "== 1. a production-like database: objects owned by the image superuser"
docker network create t-prodnet >/dev/null
check "PostgreSQL 16 starts" start_pg
check "real migrations applied as the superuser" apply_migrations
add_rooms >/dev/null
su_sql "INSERT INTO guest(first_name,last_name,email,phone_number,country) SELECT 'g'||i,'x','g'||i||'@x.dk','1','DK' FROM generate_series(1,250) i;" >/dev/null
su_sql "CREATE ROLE legacy_reader LOGIN PASSWORD 'legacypw'" >/dev/null
BEFORE="$(counts)"
echo "      rows room/guest/booking/history: $BEFORE"
echo "      owners before: $(su_sql "SELECT string_agg(DISTINCT tableowner, ',') FROM pg_tables WHERE schemaname='public'")"

echo "== 2. dry run changes nothing"
prepare >"$W/dry" 2>&1
if [[ "$(su_sql "SELECT count(*) FROM pg_roles WHERE rolname LIKE 'nfhotel\_%'")" == 0 && ! -d "$W/secrets" ]]; then
  ok "dry run: no role and no secret created"
else
  ko "dry run changed something"
fi
if verify >"$W/v" 2>&1; then ko "verify passed before the preparation"; else ok "verify fails before the preparation ($(grep -c FAIL "$W/v") checks)"; fi

echo "== 3. apply with backup, ownership transfer and network join"
recipient="$(age-keygen -o "$W/key.txt" 2>&1 | sed 's/^Public key: //')"
printf 'AGE_RECIPIENT=%s\nBACKUP_TARGET=\nBACKUP_DIR=%s\nBACKUP_CONTAINER=%s\n' "$recipient" "$W/backups" "$PG" >"$W/etc/backup.env"
printf '#!/bin/bash\nNFHOTEL_BACKUP_SETTINGS=%s NFHOTEL_METRICS_DIR=%s exec bash %s/scripts/backup-database.sh "$@"\n' \
  "$W/etc/backup.env" "$W/metrics" "$REPO" >"$W/bin/nfhotel-backup"
chmod +x "$W/bin/nfhotel-backup"
id_before="$(docker inspect -f '{{.Id}} {{.State.StartedAt}}' "$PG")"
if prepare --apply --transfer-ownership --connect-network >"$W/apply" 2>&1; then
  ok "prepare --apply --transfer-ownership --connect-network"
else
  ko "prepare failed: $(tail -3 "$W/apply")"
fi
check "the PostgreSQL container was not restarted or recreated" same "$(docker inspect -f '{{.Id}} {{.State.StartedAt}}' "$PG")" "$id_before"
after="$(counts)"
if [[ -n "$BEFORE" && "$after" == "$BEFORE" ]]; then ok "rows and migration history unchanged ($BEFORE)"; else ko "data changed: $after"; fi
backups=("$W"/backups/*.dump.age)
check "a backup was taken before the transfer" test -f "${backups[0]}"
if verify --expect-transfer --expect-network >"$W/v" 2>&1; then
  ok "verify-database: all checks pass ($(grep -c PASS "$W/v"))"
else
  ko "verify failed: $(grep FAIL "$W/v" | head -3)"
fi
check "secrets and settings files are 0600" same "$(stat -c %a "$W/secrets/postgres-app.password" "$W/secrets/nfhotel-api-database.env" | sort -u)" 600
APP="$(cat "$W/secrets/postgres-app.password")"
MIG="$(cat "$W/secrets/postgres-migrator.password")"
BAK="$(cat "$W/secrets/postgres-backup.password")"
if grep -qF "$APP" "$W/apply" "$W/dry" "$W/v"; then ko "a password appears in the script output"; else ok "no password in the script output"; fi
if docker logs "$PG" 2>&1 | grep -qF "$APP"; then ko "a password appears in the server log"; else ok "no password in the server log"; fi

echo "== 4. idempotent second run"
hash_before="$(sha256sum "$W"/secrets/* | sha256sum)"
if prepare --apply --transfer-ownership --connect-network >"$W/apply2" 2>&1 && [[ "$(sha256sum "$W"/secrets/* | sha256sum)" == "$hash_before" ]]; then
  ok "second run succeeds and keeps the passwords"
else
  ko "second run: $(tail -3 "$W/apply2")"
fi
check "an existing role keeps CONNECT to the database postgres" same "$(su_sql "SELECT has_database_privilege('legacy_reader','postgres','CONNECT')")" t

echo "== 5. nfhotel_app over the internal network (service name postgres)"
asapp() { docker run --rm --network nfhotel-db -e PGPASSWORD="$APP" "$IMG" psql -X -At -v ON_ERROR_STOP=1 -h postgres -U nfhotel_app -d "${2:-nfhotel}" -c "$1"; }
app_dml() {
  asapp "INSERT INTO guest(first_name,last_name,email,phone_number,country) VALUES ('t','t','t@t','1','DK') RETURNING guest_id" &&
    asapp "UPDATE guest SET country='SE' WHERE email='t@t'" &&
    asapp "DELETE FROM guest WHERE email='t@t'" &&
    asapp 'SELECT count(*) FROM room'
}
check "app: SELECT, INSERT, UPDATE, DELETE on guest" app_dml
# history_rights <database>: nfhotel_app reads the migration history and may not change it, so
# a takeover of the API cannot mark a migration as applied or remove one (MIL-066 task 4).
history_rights() {
  check "app: reads the migration history ($1)" asapp 'SELECT count(*) FROM "__EFMigrationsHistory"'
  refused "app: INSERT into the migration history ($1)" \
    asapp 'INSERT INTO "__EFMigrationsHistory"(migration_id, product_version) VALUES (current_user, current_user)'
  refused "app: UPDATE the migration history ($1)" asapp 'UPDATE "__EFMigrationsHistory" SET product_version = product_version'
  refused "app: DELETE from the migration history ($1)" asapp 'DELETE FROM "__EFMigrationsHistory"'
}
history_rights "migrated before its roles"
refused "app: CREATE TABLE" asapp 'CREATE TABLE x(i int)'
refused "app: DROP TABLE room" asapp 'DROP TABLE room'
refused "app: ALTER TABLE guest" asapp 'ALTER TABLE guest ADD COLUMN x int'
refused "app: TRUNCATE guest" asapp 'TRUNCATE guest'
refused "app: CREATE TEMP TABLE" asapp 'CREATE TEMP TABLE x(i int)'
refused "app: connect to the database postgres" asapp 'SELECT 1' postgres
refused "app: read pg_authid (password hashes)" asapp 'SELECT * FROM pg_authid'
check "nfhotel-db is internal (no route out)" same "$(docker network inspect -f '{{.Internal}}' nfhotel-db)" true

echo "== 6. nfhotel_migrator runs migrations as nfhotel_owner"
asmig() { docker run --rm -i --network nfhotel-db "$IMG" psql -X -q -v ON_ERROR_STOP=1 "postgresql://nfhotel_migrator:$MIG@postgres:5432/nfhotel?options=-c%20role%3Dnfhotel_owner" "$@"; }
migrate_again() { asmig -f - <"$MIGRATIONS"; }
check "migrator: the idempotent migration script runs again (no change)" migrate_again
check "migrator: a new migration creates a table" asmig -c 'CREATE TABLE future_feature(id int GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY, note text)'
check "the new table is owned by nfhotel_owner" same "$(su_sql "SELECT tableowner FROM pg_tables WHERE tablename='future_feature'")" nfhotel_owner
use_new_table() { asapp "INSERT INTO future_feature(note) VALUES ('x')" && asapp 'SELECT count(*) FROM future_feature'; }
check "app: uses the new table with no extra grant (default privileges)" use_new_table
no_set_role() { docker run --rm --network nfhotel-db -e PGPASSWORD="$MIG" "$IMG" psql -X -v ON_ERROR_STOP=1 -h postgres -U nfhotel_migrator -d nfhotel -c 'CREATE TABLE y(i int)'; }
refused "migrator without SET ROLE cannot create (no INHERIT)" no_set_role
asmig -c 'DROP TABLE future_feature' >/dev/null

http() { docker run --rm --network t-apinet "$CURL" -s "$@"; }
# start_api: the API image on nfhotel-db with the database settings that the preparation wrote,
# up when /health answers; sets token, manager_password and code.
start_api() {
  token="$(openssl rand -hex 24)"
  manager_password="$(openssl rand -hex 12)"
  cat "$W/secrets/nfhotel-api-database.env" >"$W/api.env"
  # This test checks the database access of the API, not the password flow of MIL-030, so the
  # first Manager need not choose a new password before the API answers its calls.
  printf 'ASPNETCORE_ENVIRONMENT=Production\nEncryption__PassportKey=%s\nHotelTime__TimeZoneId=Europe/Copenhagen\nApi__Token=%s\nStaff__Token__SigningKey=%s\nStaff__InitialManager__UserName=manager\nStaff__InitialManager__Password=%s\nStaff__InitialManager__MustChangePassword=false\nDatabase__MigrateOnStart=false\n' \
    "$(openssl rand -base64 32)" "$token" "$(openssl rand -hex 32)" "$manager_password" >>"$W/api.env"
  docker network inspect t-apinet >/dev/null 2>&1 || docker network create t-apinet >/dev/null
  docker run -d --name "$API" --network nfhotel-db --env-file "$W/api.env" "$API_IMAGE" >/dev/null
  docker network connect t-apinet "$API"
  code=""
  for _ in $(seq 40); do
    code="$(http -o /dev/null -w '%{http_code}' "http://$API:8080/health" 2>/dev/null)"
    [[ "$code" == 200 ]] && break
    sleep 2
  done
}
# sign_in [password]: the staff token of the first manager, through the API; the password is
# the one that start_api gave it, unless another is given.
sign_in() {
  http -H "Authorization: Bearer $token" -H 'Content-Type: application/json' \
    -d "{\"userName\":\"manager\",\"password\":\"${1:-$manager_password}\"}" "http://$API:8080/api/auth/login" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p'
}
echo "== 7. the API image reads the database through the alias postgres as nfhotel_app"
start_api
if [[ "$code" == 200 ]]; then ok "API /health answers 200"; else ko "API health: ${code:-none} $(docker logs "$API" 2>&1 | tail -5)"; fi
staff="$(sign_in)"
check "API sign-in works (the first manager was written as nfhotel_app)" test -n "$staff"
rooms="$(http -H "Authorization: Bearer $token" -H "X-Staff-Token: $staff" "http://$API:8080/api/rooms")"
check "API /api/rooms returns the 3 rooms from PostgreSQL" same "$(grep -o '"roomNumber":"R[0-9]"' <<<"$rooms" | wc -l)" 3
if docker logs "$API" 2>&1 | grep -qF "$APP"; then ko "the DB password appears in the API log"; else ok "no DB password in the API log"; fi

echo "== 8. backup as nfhotel_backup and restore drill"
backup_writes() { docker run --rm --network nfhotel-db -e PGPASSWORD="$BAK" "$IMG" psql -X -v ON_ERROR_STOP=1 -h postgres -U nfhotel_backup -d nfhotel -c 'DELETE FROM room'; }
refused "backup role cannot write" backup_writes
rm -f "$W"/backups/*
echo "BACKUP_ROLE=nfhotel_backup" >>"$W/etc/backup.env"
check "backup-database.sh dumps the running container as nfhotel_backup" "$W/bin/nfhotel-backup" --apply
cp "$W/o" "$W/backup.log"
# The read-back of MIL-076: pg_restore in the container reads the dump of the real server to its
# end while it is made, and finds every table of the database.
report() { awk '!/^#/ { print $2 }' "$W/metrics/$1.prom" 2>/dev/null; }
live_tables="$(su_sql "SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema')")"
read_back_reported() {
  [[ -n "$(report backup)" && "$(report backup_check)" == "$(report backup)" && "$(report backup_tables)" == "$live_tables" ]]
}
check "the dump is read back to its end with the $live_tables tables of the database" \
  grep -qx "    read back to its end: $live_tables tables" "$W/backup.log"
check "the read-back reports backup_check at the time of the backup, and the tables" read_back_reported
backups=("$W"/backups/*.dump.age)
if bash "$REPO/scripts/restore-drill.sh" --identity "$W/key.txt" "${backups[0]}" >"$W/drill" 2>&1; then
  ok "restore drill passes (btree_gist, EXCLUDE constraint, history)"
else
  ko "restore drill: $(tail -3 "$W/drill")"
fi
grep -E "rows|latest migration" "$W/drill" | sed 's/^/      /'

echo "== 9. a new database: the roles first, then the first migration through CD (MIL-066 task 4)"
docker rm -f "$API" "$PG" >/dev/null 2>&1
docker volume rm t-pgdata >/dev/null 2>&1
# migrate_cd: the migrate step of CD, which finds the container by its compose labels.
migrate_cd() {
  local hash
  hash="$(sha256sum <"$MIGRATIONS" | cut -d' ' -f1)"
  NFHOTEL_STATE="$W/state" NFHOTEL_SECRETS="$W/secrets" NFHOTEL_BACKUP_COMMAND="$W/bin/nfhotel-backup" \
    bash "$REPO/scripts/deploy-release.sh" migrate "$hash" <"$MIGRATIONS"
}
# start_compose_pg: an empty PostgreSQL 16 with the labels that deploy-services.sh gives it.
start_compose_pg() { start_pg --label com.docker.compose.project=nfhotel-postgres --label com.docker.compose.service=postgres; }
# deploy-release.sh takes the first container with those labels, so parts 9 and 10 run only
# where no other container has them.
if [[ -n "$(docker ps -q --filter label=com.docker.compose.project=nfhotel-postgres)" ]]; then
  ko "another container of the compose project nfhotel-postgres runs here; parts 9 and 10 were not tested"
  echo "RESULT: $pass passed, $fail failed"
  exit 1
fi
check "an empty PostgreSQL 16 starts with the labels of the compose project" start_compose_pg
check "prepare --apply --connect-network on the empty database" prepare --apply --connect-network
check "the migrate step of deploy-release.sh applies the first migration" migrate_cd
check "the first migration created the history as nfhotel_owner" same \
  "$(su_sql "SELECT tableowner FROM pg_tables WHERE tablename = '__EFMigrationsHistory'")" nfhotel_owner
history_rights "roles before its first migration"
if verify --expect-transfer --expect-network >"$W/v" 2>&1; then
  ok "verify-database on the new database: all checks pass ($(grep -c PASS "$W/v"))"
else
  ko "verify on the new database: $(grep FAIL "$W/v" | head -3)"
fi
start_api
if [[ "$code" == 200 ]]; then ok "API /health answers 200 on the new database"; else ko "API health on the new database: ${code:-none}"; fi
check "API sign-in works on the new database (the first manager was written as nfhotel_app)" test -n "$(sign_in)"

echo "== 10. the rebuild of the VM in the order of Disaster-Recovery (MIL-066 task 9)"
# The VM before the loss: the database of part 9 with three rooms, its first manager and the
# team role dev_anna, and the nightly backup of it on the laptop.
team() { bash "$REPO/scripts/manage-team-db-users.sh" --container "$PG" --secrets-dir "$W/secrets" "$@"; }
# as_anna <sql>: SQL as dev_anna over the internal network, with the password of its file.
as_anna() {
  docker run --rm --network nfhotel-db -e PGPASSWORD="$(cat "$W/secrets/team/dev_anna.password")" "$IMG" \
    psql -X -At -v ON_ERROR_STOP=1 -h postgres -U dev_anna -d nfhotel -c "$1"
}
add_rooms >/dev/null
check "the team role dev_anna is added before the loss" team --apply --add anna
old_anna="$(cat "$W/secrets/team/dev_anna.password")"
old_manager_password="$manager_password"
before_loss="$(counts)"
rm -f "$W"/backups/*
check "the nightly backup of the old database" "$W/bin/nfhotel-backup" --apply
cp "$W"/backups/*.dump.age "$W/laptop.dump.age"
# The loss: the containers and the volume go. The settings archive brings /etc/nfhotel back,
# here $W/secrets, and install-backup.sh the backup command, here $W/bin/nfhotel-backup; the
# runbook does both before the database, because the roles and CD each back up first.
docker rm -f "$API" "$PG" >/dev/null 2>&1
docker volume rm t-pgdata >/dev/null 2>&1
role_secrets() { sha256sum "$W"/secrets/*.password "$W"/secrets/*.env | sha256sum; }
secrets_before="$(role_secrets)"
check "an empty PostgreSQL 16 starts on the new VM" start_compose_pg
# The order of the runbook before MIL-066 ran CD before the restore. Its migrate step stops at
# the backup, because the new server has no role nfhotel_backup yet, and changes nothing.
if migrate_cd >"$W/cd-first" 2>&1; then
  ko "CD before the restore ran its migrations on a server without roles"
elif grep -q "the backup failed; no migration was applied" "$W/cd-first" &&
  same "$(su_sql "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'")" 0; then
  ok "CD before the restore stops at its backup and creates no table (the old order fails)"
else
  ko "CD before the restore: $(tail -3 "$W/cd-first")"
fi
age --decrypt --identity "$W/key.txt" --output "$W/nfhotel.dump" "$W/laptop.dump.age"
# restore_dump: the pg_restore of the runbook, as the superuser of the image.
restore_dump() {
  # shellcheck disable=SC2016 # expanded by the shell inside the container
  docker exec -i "$PG" sh -c 'pg_restore --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --no-owner --no-privileges' \
    <"$W/nfhotel.dump"
}
check "pg_restore --no-owner --no-privileges into the empty server" restore_dump
check "the rows and the migration history of the dump are back" same "$(counts)" "$before_loss"
check "pg_dump keeps no roles: dev_anna is gone" same "$(su_sql "SELECT count(*) FROM pg_roles WHERE rolname = 'dev_anna'")" 0
check "prepare --apply --transfer-ownership on the restored database" prepare --apply --transfer-ownership --connect-network
check "the roles get the passwords of the restored settings back" same "$(role_secrets)" "$secrets_before"
if verify --expect-transfer --expect-network >"$W/v" 2>&1; then
  ok "verify-database after the restore: all checks pass ($(grep -c PASS "$W/v"))"
else
  ko "verify after the restore: $(grep FAIL "$W/v" | head -3)"
fi
check "only then CD: its migrate step runs" migrate_cd
check "the migrations skip: the rows and the history are unchanged" same "$(counts)" "$before_loss"
history_rights "rebuilt from the dump"
check "the team role dev_anna is added again" team --apply --add anna
check "dev_anna has a new password" test "$(cat "$W/secrets/team/dev_anna.password")" != "$old_anna"
check "dev_anna signs in and reads the three rooms of the dump" same "$(as_anna 'SELECT count(*) FROM room')" 3
start_api
if [[ "$code" == 200 ]]; then ok "API /health answers 200 after the rebuild"; else ko "API health after the rebuild: ${code:-none}"; fi
staff="$(sign_in "$old_manager_password")"
check "the first manager of the dump signs in through the API" test -n "$staff"
rooms="$(http -H "Authorization: Bearer $token" -H "X-Staff-Token: $staff" "http://$API:8080/api/rooms")"
check "API /api/rooms returns the three rooms of the dump" same "$(grep -o '"roomNumber":"R[0-9]"' <<<"$rooms" | wc -l)" 3

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
