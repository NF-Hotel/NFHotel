#!/usr/bin/env bash
# Tests of the read-back of scripts/backup-database.sh (MIL-076 task 1): tee passes the dump to
# pg_restore --list and pg_restore --file=/dev/null in the postgres container while age encrypts
# it. A whole dump is read back and reported as backup_check, at the time of the backup, with its
# tables and the size of the encrypted file. A dump cut short, an unreadable archive, readers
# that cannot run and a reader that hangs each fail the read-back and report no backup_check,
# while the backup succeeds and keeps the dump. Age gets the same bytes as the readers, with and
# without the read-back; the readers read pipes, and no file holds the clear dump. The compose
# project that the VM uses runs the dump and both readers in its folder.
#
# A fake docker stands in for the container. Its dump is larger than the 64 KiB of a pipe, and its
# pg_restore --list ends after the table of contents, as the real one does, so a tee that stopped
# at the first reader that ends would cut the backup short. Its pg_restore --file=/dev/null
# accepts only the whole dump. A throwaway age key stands in for the key of S03.
#
# Run it in Debian 13 (as the VM) with the ops folder at /src, optionally against another copy of
# the script, such as the one before MIL-076:
#   docker run --rm -v "$PWD:/src:ro" debian:13-slim bash /src/tests/test-backup-read-back.sh [script]
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 or 13 (installs age)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT="${1:-$OPS/scripts/backup-database.sh}"
apt-get update -qq >/dev/null && apt-get install -y -qq --no-install-recommends age >/dev/null ||
  {
    echo "FAIL  cannot install age"
    exit 1
  }
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

# A VM in miniature: /etc/nfhotel with the settings, the folder of the compose project, the
# backups, the reports, a private temporary folder and the fakes.
app="$W/opt/nfhotel" etc="$W/etc/nfhotel" fake="$W/fake" metrics="$W/metrics"
mkdir -p "$app/ops/deploy/postgres" "$etc" "$W/bin" "$W/backups" "$W/tmp" "$fake"
echo "POSTGRES_DB=nfhotel" >"$app/ops/deploy/postgres/postgres.env"
age-keygen -o "$W/identity" 2>/dev/null
recipient="$(age-keygen -y "$W/identity")"
settings() {
  printf 'AGE_RECIPIENT=%s\nBACKUP_DIR=%s\nBACKUP_SSH_KEY=%s\n' "$recipient" "$W/backups" "$W/no-key" >"$etc/backup.env"
  [[ -z "${1:-}" ]] || echo "BACKUP_CONTAINER=$1" >>"$etc/backup.env"
}
settings nfhotel-postgres-postgres-1

# The dump: a header, a table of contents with three tables, then the rows, which carry a marker
# that no other file has, and an end. dump.cut ends early; dump.bad is not an archive.
marker="row-of-the-clear-dump-7f3a9c"
{
  echo PGDMP
  echo "215; 1259 16386 TABLE public room nfhotel_owner"
  echo "216; 1259 16390 TABLE public guest nfhotel_owner"
  echo "217; 1259 16395 TABLE public __EFMigrationsHistory nfhotel_owner"
  echo "3400; 0 16386 TABLE DATA public room nfhotel_owner"
  echo "3401; 0 16390 TABLE DATA public guest nfhotel_owner"
  echo "3402; 0 0 TABLE ATTACH public guest_part nfhotel_owner"
  echo DATA
  for i in $(seq 20000); do echo "guest $i $marker"; done
  echo PGDMP-END
} >"$fake/dump"
head -c 300000 "$fake/dump" >"$fake/dump.cut"
printf 'not an archive %.0s' $(seq 2000) >"$fake/dump.bad"
sha256sum <"$fake/dump" | cut -d' ' -f1 >"$fake/whole.sha"

# The fake docker. It logs the folder and the arguments of each call, never its input. FAKE_DUMP
# picks the dump; FAKE_READERS=down makes the readers fail at once, FAKE_READERS=hang makes the
# whole-dump reader wait without reading.
cat >"$W/bin/docker" <<'FAKE'
#!/bin/bash
echo "$PWD :: $*" >>"$FAKE_DIR/calls"
case "$*" in
  *pg_dump*) cat "$FAKE_DUMP" ;;
  *pg_restore*)
    stat -L -c %F /proc/self/fd/0 >>"$FAKE_DIR/stdin-kinds"
    if [[ "${FAKE_READERS:-}" == down ]]; then
      echo "Error response from daemon: container is not running" >&2
      exit 1
    fi
    if [[ "$*" == *"pg_restore --list"* ]]; then
      IFS= read -r first
      [[ "$first" == PGDMP ]] || { echo "pg_restore: error: input file does not appear to be a valid archive" >&2; exit 1; }
      while IFS= read -r line && [[ "$line" != DATA ]]; do echo "$line"; done
      exit 0
    fi
    [[ "${FAKE_READERS:-}" == hang ]] && exec sleep 600
    sum="$(sha256sum | cut -d' ' -f1)"
    echo "$sum" >"$FAKE_DIR/read.sha"
    [[ "$sum" == "$(cat "$FAKE_DIR/whole.sha")" ]] || { echo "pg_restore: error: could not read from input file: end of file" >&2; exit 1; }
    ;;
  *) exit 1 ;;
esac
FAKE
# The fake date gives FAKE_NOW as the time of a report, so each run has a time of its own.
cat >"$W/bin/date" <<'FAKE'
#!/bin/bash
[[ "$*" == +%s && -n "${FAKE_NOW:-}" ]] && { echo "$FAKE_NOW"; exit 0; }
PATH=/usr/bin:/bin exec date "$@"
FAKE
chmod +x "$W/bin/docker" "$W/bin/date"

# backup <now> [VAR=value...]: one backup at the time <now>, with the whole dump unless a
# variable says otherwise; its log is in $W/out.
backup() {
  local now="$1"
  shift
  rm -f "$W"/backups/nfhotel-* "$fake/read.sha" "$fake/stdin-kinds" "$fake/calls"
  env PATH="$W/bin:$PATH" TMPDIR="$W/tmp" FAKE_DIR="$fake" FAKE_DUMP="$fake/dump" FAKE_NOW="$now" \
    NFHOTEL_BACKUP_SETTINGS="$etc/backup.env" NFHOTEL_DIR="$app" NFHOTEL_ETC="$etc" \
    NFHOTEL_METRICS_DIR="$metrics" NFHOTEL_READ_BACK_SECONDS=3 "$@" bash "$SCRIPT" --apply >"$W/out" 2>&1
}
# report <name>: the value in <name>.prom, or "none".
report() {
  if [[ -f "$metrics/$1.prom" ]]; then awk '!/^#/ { print $2 }' "$metrics/$1.prom"; else echo none; fi
}
# encrypted: the sha256 of the clear content of the newest encrypted dump.
encrypted() {
  local file
  file="$(find "$W/backups" -name 'nfhotel-*.dump.age' | head -n 1)"
  [[ -n "$file" ]] && age --decrypt --identity "$W/identity" "$file" | sha256sum | cut -d' ' -f1
}
# kept_out: no file outside the fakes holds the marker of the clear dump, and no folder of the
# read-back is left behind.
kept_out() {
  ! grep -rq --binary-files=text "$marker" "$W" --exclude-dir=fake 2>/dev/null &&
    [[ -z "$(find "$W/tmp" -mindepth 1 -maxdepth 1)" ]]
}

echo "== a whole dump"
backup 1000 && ok "the backup runs" || ko "the backup: $(tail -3 "$W/out")"
grep -qx '    read back to its end: 3 tables' "$W/out" && ok "the log names the tables that the read-back found" ||
  ko "the log: $(grep -i 'read' "$W/out" | head -3)"
[[ "$(report backup_check)" == 1000 && "$(report backup)" == 1000 ]] &&
  ok "backup_check has the time of the backup" || ko "backup_check $(report backup_check), backup $(report backup)"
[[ "$(report backup_tables)" == 3 ]] && ok "nfhotel_backup_tables counts the tables, not their data" ||
  ko "nfhotel_backup_tables is $(report backup_tables)"
size="$(stat -c %s "$(find "$W/backups" -name 'nfhotel-*.dump.age' | head -n 1)" 2>/dev/null)"
[[ -n "$size" && "$(report backup_bytes)" == "$size" ]] && ok "nfhotel_backup_bytes is the size of the encrypted dump" ||
  ko "nfhotel_backup_bytes is $(report backup_bytes), the file $size"
grep -qx 'nfhotel_backup_tables 3' "$metrics/backup_tables.prom" 2>/dev/null &&
  grep -qx '# TYPE nfhotel_backup_bytes gauge' "$metrics/backup_bytes.prom" 2>/dev/null &&
  ok "the reports are gauges in the text format" || ko "the reports: $(cat "$metrics"/backup_*.prom 2>&1 | head -6)"
whole="$(cat "$fake/whole.sha")"
[[ "$(encrypted)" == "$whole" && "$(cat "$fake/read.sha" 2>/dev/null)" == "$whole" ]] &&
  ok "age and the whole-dump reader get the same bytes, the whole dump" ||
  ko "age $(encrypted), the reader $(cat "$fake/read.sha" 2>/dev/null), the dump $whole"
[[ "$(sort -u "$fake/stdin-kinds" 2>/dev/null)" == fifo && "$(wc -l <"$fake/stdin-kinds" 2>/dev/null)" == 2 ]] &&
  kept_out && ok "both readers read pipes, and no file holds the clear dump" ||
  ko "the readers read $(sort -u "$fake/stdin-kinds" 2>/dev/null | tr '\n' ' '); left: $(find "$W/tmp" -mindepth 1 | head -3)"
grep -q 'nfhotel-postgres-postgres-1 sh -c pg_restore --list$' "$fake/calls" &&
  grep -q 'exec -i nfhotel-postgres-postgres-1 sh -c pg_restore --file=/dev/null$' "$fake/calls" &&
  ok "the readers run in the postgres container" || ko "the calls: $(cat "$fake/calls")"

echo "== a dump cut short"
backup 2000 FAKE_DUMP="$fake/dump.cut" && ok "the backup succeeds" || ko "the backup: $(tail -3 "$W/out")"
[[ "$(report backup_check)" == 1000 && "$(report backup)" == 2000 ]] &&
  grep -q 'warning: the dump was not read back' "$W/out" &&
  ok "the read-back fails, and only the backup reports" ||
  ko "backup_check $(report backup_check), backup $(report backup): $(grep warning "$W/out")"
[[ "$(encrypted)" == "$(sha256sum <"$fake/dump.cut" | cut -d' ' -f1)" ]] && kept_out &&
  ok "the dump is kept as it came, and no file holds it in clear" || ko "the kept dump differs"
grep -q 'could not read from input file' "$W/out" && ok "the log shows the error of pg_restore" ||
  ko "the log: $(tail -4 "$W/out")"

echo "== an unreadable archive"
backup 3000 FAKE_DUMP="$fake/dump.bad" && [[ "$(report backup_check)" == 1000 && "$(report backup)" == 3000 ]] &&
  grep -q 'pg_restore --list 1, pg_restore 1, 0 tables' "$W/out" &&
  ok "the backup succeeds, both readers fail, and backup_check stays" || ko "the unreadable archive: $(tail -4 "$W/out")"
[[ "$(report backup_tables)" == 3 ]] && ok "nfhotel_backup_tables keeps the count of the last read-back" ||
  ko "nfhotel_backup_tables is $(report backup_tables)"

echo "== readers that cannot run, and a reader that hangs"
backup 4000 FAKE_READERS=down && [[ "$(report backup_check)" == 1000 && "$(encrypted)" == "$whole" ]] &&
  ok "readers that fail at once leave the backup whole" || ko "readers down: $(tail -4 "$W/out")"
start=$SECONDS
backup 5000 FAKE_READERS=hang && [[ "$(report backup_check)" == 1000 && "$(encrypted)" == "$whole" ]] &&
  ((SECONDS - start < 30)) && grep -q 'pg_restore 124' "$W/out" &&
  ok "a reader that hangs is stopped, and the backup is whole ($((SECONDS - start)) s)" ||
  ko "a hanging reader: $((SECONDS - start)) s, $(tail -4 "$W/out")"

echo "== without the read-back"
rm -rf "$metrics"
backup 6000 TMPDIR="$W/no-such-folder" && grep -q 'warning: the read-back cannot start' "$W/out" &&
  [[ "$(encrypted)" == "$whole" ]] && ok "a read-back that cannot start leaves the same bytes to age" ||
  ko "without the read-back: $(tail -4 "$W/out")"
[[ "$(report backup_check)" == 0 && "$(report backup)" == 6000 ]] &&
  ok "before any read-back, backup_check is 0, so the alert has a report to compare" ||
  ko "backup_check $(report backup_check), backup $(report backup)"

echo "== the compose project of the VM"
settings
backup 7000 && [[ "$(report backup_check)" == 7000 ]] && ok "the read-back runs through docker compose" ||
  ko "compose: $(tail -4 "$W/out")"
[[ "$(grep -c "^$app/ops/deploy/postgres :: compose --env-file postgres.env exec -T postgres sh -c" "$fake/calls")" == 3 ]] &&
  ok "the dump and both readers run in the folder of the compose project" || ko "the calls: $(cat "$fake/calls")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
