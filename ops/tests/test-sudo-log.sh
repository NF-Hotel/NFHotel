#!/usr/bin/env bash
# Tests that no secret of the deploy reaches the log of sudo (MIL-070 task 2, ADR-0041), in
# Debian 13, the system of the VM, with the sudo settings of the VM: Debian's /etc/sudoers
# (use_pty) and deploy/host/sudo/nfhotel-logging (Defaults logfile). sudo writes every variable
# that it keeps (--preserve-env, -E, or NAME=value before the command) into the ENV= field of
# its log line, in its log file and in the journal. The checks:
#   - the trap is real: a connection string kept with --preserve-env is in /var/log/sudo.log
#   - as_app of scripts/deploy-release.sh, run as root for the application user, gives the
#     command the connection string of the API byte for byte, the flag
#     NFHOTEL_API_MIGRATE_ON_START and the checkout as its folder; sudo logs the call, the
#     connection string is in no file under /var/log and in the arguments of no process while
#     the command runs, and the command cannot read it from its standard input
#   - without the settings the command has neither variable; as_app ends with the exit status
#     of the command; as the application user, as_app runs the command without sudo
#   - the second check: no sudo call in ops/, outside the tests, keeps a variable that is not
#     on NOT_SECRET (--preserve-env=<list>, --preserve-env, -E, NAME=value); its own cases first
#
# Run it in Debian 13 with the repository's ops folder at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:13-slim bash /src/tests/test-sudo-log.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get with the Debian archive, for sudo), root

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

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

readonly SCRIPT=/src/scripts/deploy-release.sh
readonly LOG=/var/log/sudo.log
# Fake passwords that only this test writes, so a match in a log or in an argument comes from it.
readonly TRAP_PASSWORD=trap-a6c41f0e
readonly FAKE_PASSWORD=fake-9d2b7e35
# The connection string holds what a shell would change if it read the value as code.
readonly CONNECTION="Host=postgres;Username=nfhotel_app;Password=$FAKE_PASSWORD;Application Name=it's \"a\" \$HOME \\ test"
# The variables that hold no secret and may cross sudo in its environment.
readonly NOT_SECRET=(NFHOTEL_API_MIGRATE_ON_START)

# sudo_kept <file>: one line "<file>:<line>: <what>" for each sudo call of a shell file that keeps
# a variable outside NOT_SECRET (--preserve-env=<list>, NAME=value before the command) or the
# whole environment (--preserve-env without a list, -E). Continued lines are joined; a comment
# ends the line.
sudo_kept() {
  local file="$1" number=0 start=1 line text=""
  while IFS= read -r line || [[ -n "$line" ]]; do
    number=$((number + 1))
    [[ -z "$text" ]] && start="$number"
    if [[ "$line" == *\\ ]]; then
      text+="${line%\\} "
      continue
    fi
    sudo_call "$file:$start" "$text$line"
    text=""
  done <"$1"
}

# sudo_call <place> <text>: the findings of sudo_kept for one joined line.
sudo_call() {
  local place="$1" word name names in_sudo=0 skip=0
  local -a words
  read -ra words <<<"$2"
  for word in "${words[@]}"; do
    [[ "$word" == \#* ]] && break
    if ((skip)); then
      skip=0
      continue
    fi
    if ((!in_sudo)); then
      [[ "$word" == sudo || "$word" == */sudo ]] && in_sudo=1
      continue
    fi
    word="${word//\"/}"
    word="${word//\'/}"
    case "$word" in
      --preserve-env=*)
        IFS=, read -ra names <<<"${word#--preserve-env=}"
        for name in "${names[@]}"; do
          not_secret "$name" || echo "$place: --preserve-env keeps $name"
        done
        ;;
      --preserve-env) echo "$place: --preserve-env keeps the whole environment" ;;
      -u | -g | -C | -D | -h | -p | -R | -r | -t | -T | -U) skip=1 ;;
      --) ;;
      --* | -[!E]) ;;
      -*E*) echo "$place: $word keeps the whole environment" ;;
      -*) ;;
      [A-Za-z_]*=*)
        name="${word%%=*}"
        if [[ "$name" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
          not_secret "$name" || echo "$place: $name=... sets a variable for sudo"
        else
          in_sudo=0
        fi
        ;;
      *) in_sudo=0 ;;
    esac
  done
}

not_secret() {
  local name
  for name in "${NOT_SECRET[@]}"; do
    [[ "$name" == "$1" ]] && return 0
  done
  return 1
}

echo "==> sudo with the settings of the VM"
if ! { apt-get -qq update && DEBIAN_FRONTEND=noninteractive apt-get -qq install -y --no-install-recommends sudo; } >/dev/null; then
  echo "FAIL  sudo could not be installed"
  exit 1
fi
install -m 0440 /src/deploy/host/sudo/nfhotel-logging /etc/sudoers.d/nfhotel-logging
visudo -cq && ok "visudo accepts /etc/sudoers and nfhotel-logging" || ko "visudo refuses the settings"
useradd --create-home --shell /bin/bash nfhotel
W="$(mktemp -d)"
chmod 0755 "$W"
app_user=nfhotel
# shellcheck disable=SC2034 # used by as_app, which is read with eval
app_dir="$W/app"
seen="$W/seen"
install -d -o nfhotel -g nfhotel "$W/app" "$seen"
# The command for as_app: it writes its folder, the two settings and its standard input, and the
# arguments of every process while it runs.
cat >"$W/show" <<'EOF'
#!/bin/bash
{
  printf 'folder=%s\n' "$PWD"
  printf 'connection=%s\n' "${NFHOTEL_API_CONNECTION_STRING-<not set>}"
  printf 'migrate=%s\n' "${NFHOTEL_API_MIGRATE_ON_START-<not set>}"
  printf 'input=%s\n' "$(cat)"
} >"$1/settings"
for f in /proc/[0-9]*/cmdline; do
  tr '\0' ' ' 2>/dev/null <"$f"
  echo
done >"$1/arguments"
EOF
chmod 0755 "$W/show"

echo "==> the trap: sudo logs a variable that it keeps"
NFHOTEL_API_CONNECTION_STRING="Password=$TRAP_PASSWORD" \
  sudo -u nfhotel -H --preserve-env=NFHOTEL_API_CONNECTION_STRING true
grep -q -F "$TRAP_PASSWORD" "$LOG" &&
  ok "the trap is real: a connection string kept with --preserve-env is in $LOG" ||
  ko "the trap: a connection string kept with --preserve-env is not in $LOG, so the log shows nothing"

echo "==> as_app of deploy-release.sh"
# as_app as the script defines it, with the shell that sudo starts and the line of the connection
# string where the script has them.
eval "$(sed -n '/^readonly APP_SHELL=/p' "$SCRIPT")"
eval "$(sed -n '/^connection_string_line() {/,/^}/p' "$SCRIPT")"
eval "$(sed -n '/^as_app() {/,/^}/p' "$SCRIPT")"
declare -F as_app >/dev/null && ok "as_app is read from deploy-release.sh" || ko "deploy-release.sh has no as_app"

before="$(wc -l <"$LOG")"
export NFHOTEL_API_CONNECTION_STRING="$CONNECTION" NFHOTEL_API_MIGRATE_ON_START=false
as_app "$W/show" "$seen"
status=$?
unset NFHOTEL_API_CONNECTION_STRING NFHOTEL_API_MIGRATE_ON_START
added="$(tail -n +"$((before + 1))" "$LOG")"
[[ "$status" == 0 ]] && ok "as_app runs the command as $app_user" || ko "as_app ended with $status"
[[ "$(sed -n 's/^connection=//p' "$seen/settings")" == "$CONNECTION" ]] &&
  ok "the command has the connection string, byte for byte" ||
  ko "the command has another connection string, or none"
grep -qx 'migrate=false' "$seen/settings" && ok "the command has NFHOTEL_API_MIGRATE_ON_START=false" ||
  ko "the command lacks NFHOTEL_API_MIGRATE_ON_START=false"
grep -qx "folder=$W/app" "$seen/settings" && ok "the command runs in the checkout" || ko "the command runs in another folder"
grep -qx 'input=' "$seen/settings" && ok "the command reads nothing from its standard input" ||
  ko "the command read something from its standard input"
grep -q -F "$W/show" <<<"$added" && ok "sudo logged the call in $LOG" || ko "sudo did not log the call in $LOG"
grep -q -F 'NFHOTEL_API_MIGRATE_ON_START=false' <<<"$added" &&
  ok "the log keeps the flag NFHOTEL_API_MIGRATE_ON_START, which holds no secret" ||
  ko "the log lacks the flag NFHOTEL_API_MIGRATE_ON_START"
leaked="$(grep -r -l -F "$FAKE_PASSWORD" /var/log | paste -sd ' ' -)"
[[ -z "$leaked" ]] && ok "the connection string is in no file under /var/log" ||
  ko "the connection string is in $leaked"
grep -q -F "$W/show" "$seen/arguments" && ok "the arguments of the processes were read while the command ran" ||
  ko "the arguments of the processes were not read while the command ran"
grep -q -F "$FAKE_PASSWORD" "$seen/arguments" && ko "the connection string is in the arguments of a process" ||
  ok "the connection string is in the arguments of no process"

rm -f "$seen/settings"
as_app "$W/show" "$seen"
grep -qx 'connection=<not set>' "$seen/settings" && grep -qx 'migrate=<not set>' "$seen/settings" &&
  ok "without the settings the command has neither variable" ||
  ko "without the settings the command still has a variable, or did not run"
as_app bash -c 'exit 3'
status=$?
[[ "$status" == 3 ]] && ok "as_app ends with the exit status of the command" || ko "as_app ended with $status, not 3"

app_user=root
before="$(wc -l <"$LOG")"
rm -f "$seen/settings"
export NFHOTEL_API_CONNECTION_STRING="$CONNECTION"
as_app "$W/show" "$seen"
unset NFHOTEL_API_CONNECTION_STRING
[[ "$(sed -n 's/^connection=//p' "$seen/settings")" == "$CONNECTION" && "$(wc -l <"$LOG")" == "$before" ]] &&
  ok "as the application user, as_app runs the command without sudo, with the connection string" ||
  ko "as the application user, as_app went through sudo or lost the connection string"
app_user=nfhotel

echo "==> the second check: no sudo call keeps a variable outside NOT_SECRET"
cat >"$W/kept.sh" <<'EOF'
sudo --preserve-env=NFHOTEL_API_CONNECTION_STRING true
sudo -u nfhotel -H --preserve-env=NFHOTEL_API_MIGRATE_ON_START,DB_PASSWORD bash -c true
sudo -E make
sudo -HE make
sudo --preserve-env make
sudo -u nfhotel TOKEN=x bash -c true
/usr/bin/sudo -u "$app_user" -H \
  --preserve-env="SECRET_KEY" \
  true
EOF
found="$(sudo_kept "$W/kept.sh")"
expected="$W/kept.sh:1: --preserve-env keeps NFHOTEL_API_CONNECTION_STRING
$W/kept.sh:2: --preserve-env keeps DB_PASSWORD
$W/kept.sh:3: -E keeps the whole environment
$W/kept.sh:4: -HE keeps the whole environment
$W/kept.sh:5: --preserve-env keeps the whole environment
$W/kept.sh:6: TOKEN=... sets a variable for sudo
$W/kept.sh:7: --preserve-env keeps SECRET_KEY"
[[ "$found" == "$expected" ]] && ok "the check finds each of 7 sudo calls that keep a variable, on its line" ||
  ko "the check found: ${found:-nothing}"
cat >"$W/clean.sh" <<'EOF'
sudo -u nfhotel -H --preserve-env=NFHOTEL_API_MIGRATE_ON_START bash -c true
sudo -l -U deploy
sudo -u nfhotel -H git -C /opt/nfhotel log --format=%H -E
sudo systemctl restart nfhotel-backup.timer # sudo -E would keep everything
# sudo --preserve-env=TOKEN in a comment
echo "the deploy runs as deploy through sudo"
EOF
found="$(sudo_kept "$W/clean.sh")"
[[ -z "$found" ]] && ok "the check passes sudo calls that keep nothing or only NOT_SECRET" ||
  ko "the check refused: $found"
: >"$W/found"
while IFS= read -r -d '' file; do
  sudo_kept "$file" >>"$W/found"
done < <(find /src -path /src/tests -prune -o -type f -name '*.sh' -print0)
[[ ! -s "$W/found" ]] && ok "no sudo call in ops/, outside the tests, keeps a variable outside NOT_SECRET" ||
  ko "sudo calls in ops/ keep a variable: $(paste -sd ';' "$W/found")"

rm -rf "$W"
echo "$pass passed, $fail failed"
((fail == 0))
