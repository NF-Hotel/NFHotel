#!/usr/bin/env bash
# Test of the personal database roles of the team (MIL-048, ADR-0034) against PostgreSQL 16:
# scripts/manage-team-db-users.sh and scripts/db/team-roles.sql. A dev_ role reads and writes
# rows, also in a table the migrations add later, and cannot create, truncate or drop a table,
# change the migration history or log in after --remove; the password stays in its 0600 file;
# a change is logged with the role's name.
#
# Run it on a host with Docker:  bash ops/tests/test-team-db-users.sh
# It starts the container t-team-pg and the network t-team, and removes them at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO PG=t-team-pg NET=t-team IMG=postgres:16-alpine
readonly SCRIPT="$REPO/scripts/manage-team-db-users.sh"
W="$(mktemp -d)"
readonly W
pass=0
fail=0

cleanup() {
  docker rm -f "$PG" >/dev/null 2>&1
  docker network rm "$NET" >/dev/null 2>&1
  rm -rf "$W"
}
trap cleanup EXIT
cleanup
mkdir -p "$W"

ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}
# as_dev <role> <sql>: run SQL as the role over TCP, as DBeaver would, with the password of its file.
as_dev() {
  docker run --rm -i --network "$NET" -e PGPASSWORD="$(cat "$W/secrets/team/$1.password")" "$IMG" \
    psql -X -At -v ON_ERROR_STOP=1 -h "$PG" -U "$1" -d nfhotel <<<"$2" >"$W/o" 2>&1
}
# refused <description> <role> <sql>: passes when the SQL fails for want of a right, not for
# another reason such as a failed login.
refused() {
  if as_dev "$2" "$3"; then
    ko "$1 (was allowed)"
  elif grep -qiE "permission denied|must be owner" "$W/o"; then
    ok "$1"
  else
    ko "$1 (other error: $(head -c 200 "$W/o"))"
  fi
}
# superuser <sql>: run SQL as the superuser of the image.
superuser() { docker exec -i "$PG" psql -X -At -v ON_ERROR_STOP=1 -U nfhotel -d nfhotel <<<"$1"; }
manage() { bash "$SCRIPT" --container "$PG" --secrets-dir "$W/secrets" "$@" >"$W/out" 2>&1; }

docker network create "$NET" >/dev/null
docker run -d --name "$PG" --network "$NET" -e POSTGRES_USER=nfhotel -e POSTGRES_DB=nfhotel \
  -e POSTGRES_PASSWORD=superuser-test-password "$IMG" >/dev/null
for _ in $(seq 1 30); do
  docker exec "$PG" pg_isready -U nfhotel -d nfhotel -h 127.0.0.1 >/dev/null 2>&1 && break
  sleep 1
done
# What ADR-0011 left: the owner role, a table with a sequence, and the migration history.
superuser "
CREATE ROLE nfhotel_owner NOLOGIN;
GRANT CREATE ON SCHEMA public TO nfhotel_owner;
SET ROLE nfhotel_owner;
CREATE TABLE guest (guest_id serial PRIMARY KEY, name text NOT NULL);
CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" text PRIMARY KEY, \"ProductVersion\" text);
INSERT INTO \"__EFMigrationsHistory\" VALUES ('20261001_Init', '10.0');" >/dev/null ||
  {
    echo "FAIL  test setup"
    exit 1
  }

echo "== usage and dry run"
manage --add "Jakob"
[[ $? == 2 ]] && ok "a name with a capital letter is refused" || ko "name check: $(cat "$W/out")"
manage --add "j"
[[ $? == 2 ]] && ok "a one-letter name is refused" || ko "short name"
manage --add jakob && grep -q "dry run" "$W/out" && [[ "$(superuser "SELECT count(*) FROM pg_roles WHERE rolname = 'dev_jakob'")" == 0 ]] &&
  ok "without --apply nothing is created" || ko "dry run: $(cat "$W/out")"

echo "== --add"
manage --apply --add jakob && ok "--add jakob" || ko "--add: $(cat "$W/out")"
f="$W/secrets/team/dev_jakob.password"
[[ "$(stat -c %a "$f")" == 600 && "$(cat "$f")" =~ ^[0-9a-f]{48}$ ]] && ok "the password file is 0600 with 48 hex characters" || ko "password file"
! grep -q "$(cat "$f")" "$W/out" && ok "the password is not printed" || ko "the password was printed"
[[ "$(superuser "SELECT rolcanlogin::text || rolsuper::text || pg_has_role('dev_jakob', 'nfhotel_team', 'MEMBER')::text FROM pg_roles WHERE rolname = 'dev_jakob'")" == truefalsetrue ]] &&
  ok "dev_jakob logs in, is no superuser and is in nfhotel_team" || ko "role attributes"
manage --apply --add jakob
[[ $? == 1 ]] && grep -q "use --reset" "$W/out" && ok "a second --add is refused" || ko "second --add: $(cat "$W/out")"

echo "== rights of dev_jakob"
as_dev dev_jakob "INSERT INTO guest (name) VALUES ('Test Guest'); UPDATE guest SET name = 'Changed'; SELECT name FROM guest;" &&
  grep -qx Changed "$W/o" && ok "SELECT, INSERT and UPDATE work" || ko "write: $(cat "$W/o")"
as_dev dev_jakob "DELETE FROM guest; SELECT count(*) FROM guest;" && grep -qx 0 "$W/o" && ok "DELETE works" || ko "delete: $(cat "$W/o")"
refused "CREATE TABLE is refused" dev_jakob "CREATE TABLE mine (id int);"
refused "TRUNCATE is refused" dev_jakob "TRUNCATE guest;"
refused "DROP TABLE is refused" dev_jakob "DROP TABLE guest;"
refused "the migration history cannot be changed" dev_jakob "DELETE FROM \"__EFMigrationsHistory\";"
as_dev dev_jakob "SELECT count(*) FROM \"__EFMigrationsHistory\";" && grep -qx 1 "$W/o" && ok "the migration history can be read" || ko "read history"
superuser "SET ROLE nfhotel_owner; CREATE TABLE room (room_id serial PRIMARY KEY, number int);" >/dev/null
as_dev dev_jakob "INSERT INTO room (number) VALUES (101); SELECT count(*) FROM room;" && grep -qx 1 "$W/o" &&
  ok "a table the migrations add later is writable too" || ko "default privileges: $(cat "$W/o")"
as_dev dev_jakob "SHOW lock_timeout;" && grep -qx 5s "$W/o" && ok "lock_timeout is 5s" || ko "lock_timeout: $(cat "$W/o")"
docker logs "$PG" 2>&1 | grep -q "dev_jakob@nfhotel .*INSERT INTO room" && ok "a change is logged with the role's name" || ko "log line"
! docker logs "$PG" 2>&1 | grep -q "$(cat "$f")" && ok "the password is not in the server log" || ko "the password is in the log"

echo "== --reset and --remove"
old="$(cat "$f")"
manage --apply --reset jakob && [[ "$(cat "$f")" != "$old" ]] && as_dev dev_jakob "SELECT 1;" && ok "--reset gives a new password that works" || ko "--reset: $(cat "$W/out")"
docker run --rm --network "$NET" -e PGPASSWORD="$old" "$IMG" psql -X -h "$PG" -U dev_jakob -d nfhotel -c "SELECT 1" >/dev/null 2>&1 &&
  ko "the old password still works" || ok "the old password is refused"
manage --apply --add william && manage --list && grep -qx dev_jakob "$W/out" && grep -qx dev_william "$W/out" && ok "--list shows the team" || ko "--list: $(cat "$W/out")"
manage --apply --remove jakob && [[ ! -e "$f" && "$(superuser "SELECT count(*) FROM pg_roles WHERE rolname = 'dev_jakob'")" == 0 ]] &&
  ok "--remove drops the role and its file" || ko "--remove: $(cat "$W/out")"
manage --apply --remove jakob
[[ $? == 1 ]] && ok "--remove of a missing role is refused" || ko "second --remove"
as_dev dev_william "SELECT 1;" && ok "the others keep their access" || ko "william after removing jakob"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
