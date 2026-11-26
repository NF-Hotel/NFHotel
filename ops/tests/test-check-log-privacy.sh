#!/usr/bin/env bash
# Tests of scripts/check-log-privacy.sh (MIL-036 task 4): clean logs pass; a value in any
# container's log fails, naming the container but not printing the line; no container fails;
# and no value is a usage error. The values of a file (MIL-088), one per line, are checked as
# well, also one that starts with -, and so are the matches of a pattern, such as the emails
# that the tests of the verification make for themselves. A fake stands in for docker.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-check-log-privacy.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
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

# The fake lists the containers in $W/containers and prints $W/logs/<name> as their log.
mkdir -p "$W/logs"
cat >"$W/docker" <<'EOF'
#!/bin/bash
case "$1" in
  ps) cat "$FAKE_DIR/containers" ;;
  logs) cat "$FAKE_DIR/logs/$2" 2>/dev/null ;;
esac
EOF
chmod +x "$W/docker"
export NFHOTEL_DOCKER="$W/docker" FAKE_DIR="$W"
printf '%s\n' nfhotel-e2e-web-1 nfhotel-e2e-api-1 other-container >"$W/containers"
echo "info: GET /book 200" >"$W/logs/nfhotel-e2e-web-1"
echo "info: booking 42 created" >"$W/logs/nfhotel-e2e-api-1"
echo "ada@example.com" >"$W/logs/other-container"
check() { bash "$OPS/scripts/check-log-privacy.sh" "$@" >"$W/out" 2>&1; }

check ada@example.com Lovelace && grep -q "2 containers" "$W/out" &&
  ok "clean logs of the stack pass, and other containers are not read" || ko "clean: $(cat "$W/out")"

echo "warn: guest Ada Lovelace <ada@example.com> could not be saved" >>"$W/logs/nfhotel-e2e-api-1"
check ada@example.com Lovelace
[[ $? == 1 ]] && grep -q "nfhotel-e2e-api-1" "$W/out" && ! grep -q "ada@example.com" "$W/out" &&
  ok "a value in a log fails, names the container and does not print the data" || ko "found: $(cat "$W/out")"

: >"$W/containers"
check ada@example.com && ko "passed with no container" || ok "a missing stack fails"
check && ko "accepted no value" || ok "no value is a usage error"

# The tokens of the verification by email come in a file; a token may start with -.
printf '%s\n' nfhotel-e2e-web-1 nfhotel-e2e-api-1 >"$W/containers"
echo "info: booking 42 created" >"$W/logs/nfhotel-e2e-api-1"
printf -- '-tok_en-1\r\n\nsecond-token\n' >"$W/tokens"
check --values-file "$W/tokens" Lovelace && grep -q "2 containers" "$W/out" &&
  ok "values of a file that are in no log pass" || ko "file, clean: $(cat "$W/out")"
echo "warn: link https://nginx/booking/verify#-tok_en-1 sent" >>"$W/logs/nfhotel-e2e-api-1"
check --values-file "$W/tokens"
[[ $? == 1 ]] && grep -q "nfhotel-e2e-api-1" "$W/out" && ! grep -q -- "-tok_en-1" "$W/out" &&
  ok "a value of the file that starts with - is found, and not printed" || ko "file, found: $(cat "$W/out")"
check --values-file "$W/no-such-file" Lovelace
[[ $? == 2 ]] && ok "a missing file of values is a usage error" || ko "missing file: $(cat "$W/out")"

# The emails of the tests of the verification by email, one made up for each test
# (e2e-<guid>@example.com), are found by a pattern; a line without such an email is not.
email_pattern='e2e-[0-9a-f-]+@example[.]com'
echo "info: booking_request_made 172.18.0.5" >"$W/logs/nfhotel-e2e-api-1"
check --pattern "$email_pattern" && grep -q "2 containers" "$W/out" &&
  ok "a pattern alone is enough, and a line without such an email does not match it" || ko "pattern, clean: $(cat "$W/out")"
echo "warn: mail to e2e-0f3c5a7e9b1d4f2a8c6e0b4d2f6a8c1e@example.com failed" >>"$W/logs/nfhotel-e2e-web-1"
check Lovelace --pattern "$email_pattern"
[[ $? == 1 ]] && grep -q "nfhotel-e2e-web-1" "$W/out" && ! grep -q "0f3c5a7e9b1d" "$W/out" &&
  ok "an email of a test of the verification is found by the pattern, and not printed" || ko "pattern, found: $(cat "$W/out")"
check Lovelace --pattern
[[ $? == 2 ]] && ok "--pattern without a pattern is a usage error" || ko "empty pattern: $(cat "$W/out")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
