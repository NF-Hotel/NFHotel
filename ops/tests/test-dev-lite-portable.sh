#!/usr/bin/env bash
# Test that the scripts of the light development mode (ADR-0035, MIL-056 task 5) run with the tools of macOS as
# well as with those of Linux. It needs no Docker, so it runs on a macOS runner of GitHub Actions:
#   - no GNU-only option is in the scripts the light mode runs;
#   - the settings, the TLS pair and the secret files are made with the tools of this machine (BSD or GNU, LibreSSL
#     or OpenSSL), with the right names and modes;
#   - dev-lite.sh --check runs and reports, and the bash 3.2 of macOS gets the install hint, not a syntax error.
#
# Usage: test-dev-lite-portable.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, openssl, grep, sed, find

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# The scripts that the light mode runs on the developer's machine.
readonly SCRIPTS=(scripts/dev-lite.sh ops/scripts/lib/stack.sh ops/scripts/lib/common.sh
  ops/scripts/write-secret-files.sh ops/scripts/make-dev-certificate.sh ops/scripts/check-secret-env.sh)
# Options that GNU has and macOS has not, or has with another meaning.
readonly GNU_ONLY='sed -i |stat -c|stat --|base64 -w|-printf|readlink -f|date -d|grep -P|xargs -r|sha256sum|mktemp -p|(^|[^-a-z_])timeout [0-9]'
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

cd "$REPO" || exit 1
for script in "${SCRIPTS[@]}"; do
  found="$(grep -nE "$GNU_ONLY" "$script" | grep -vE '^[0-9]+:[[:space:]]*#' || true)"
  [[ -z "$found" ]] && ok "$script uses no GNU-only option" || ko "$script: $found"
done

# The settings and the TLS pair, with the library as both stacks call it.
(
  # shellcheck source=ops/scripts/lib/common.sh
  . ops/scripts/lib/common.sh
  # shellcheck source=ops/scripts/lib/stack.sh
  . ops/scripts/lib/stack.sh
  stack_init nfhotel-portable
  stack_write_settings "$W/settings" manager "$(stack_secret 9)" false
) >"$W/settings.log" 2>&1 && ok "stack_write_settings runs here" || ko "stack_write_settings: $(cat "$W/settings.log")"
for file in postgres.env nfhotel-api.env nfhotel-web.env nginx.env tls/fullchain.pem tls/privkey.pem; do
  [[ -s "$W/settings/$file" ]] && ok "it writes $file" || ko "$file is missing"
done
if [[ "$(uname -s)" == MINGW* || "$(uname -s)" == MSYS* ]]; then
  echo "SKIP  Git Bash on Windows has no Unix file modes, so the modes are not tested"
else
  [[ "$(find "$W/settings" -maxdepth 0 -perm 700 | wc -l | tr -d ' ')" == 1 ]] && ok "the settings folder is 0700" || ko "the settings folder is not 0700"
  [[ "$(find "$W/settings" -maxdepth 1 -name '*.env' -perm 600 | wc -l | tr -d ' ')" == 4 ]] &&
    ok "the settings files are 0600" || ko "a settings file is not 0600"
fi
[[ ! -e "$W/settings/tls/.openssl.cnf" ]] && ok "the certificate's configuration file is removed" || ko "the .openssl.cnf is left"
san="$(openssl x509 -in "$W/settings/tls/fullchain.pem" -noout -text 2>/dev/null | grep -A1 'Subject Alternative Name' | tail -1)"
[[ "$san" == *"DNS:localhost"* && "$san" == *"DNS:nginx"* ]] && ok "the certificate names localhost and nginx ($(openssl version))" ||
  ko "the certificate names: $san"
grep -q '^Staff__InitialManager__MustChangePassword=false$' "$W/settings/nfhotel-api.env" &&
  ok "the development Manager need not change the password" || ko "MustChangePassword is not false"

for service in postgres api web; do
  bash ops/scripts/write-secret-files.sh "$service" --out "$W/settings/$service" --postgres "$W/settings/postgres.env" \
    --api "$W/settings/nfhotel-api.env" --web "$W/settings/nfhotel-web.env" >"$W/$service.log" 2>&1 &&
    [[ -s "$W/settings/$service/settings.env" ]] && ok "the secret files of $service are written here" ||
    ko "write-secret-files.sh $service: $(cat "$W/$service.log")"
done
! grep -q 'Api__Token' "$W/settings/api/settings.env" && [[ -s "$W/settings/api/Api__Token" ]] &&
  ok "the API token is a file, not a setting (ADR-0024)" || ko "the API token is in settings.env"

# --check reports even where Docker is missing; any PROBLEM then is about Docker only.
DEV_LITE_DIR="$W/lite" bash scripts/dev-lite.sh --check >"$W/check.log" 2>&1
grep -q '^  ok       bash' "$W/check.log" && ok "dev-lite.sh --check runs here" || ko "--check: $(cat "$W/check.log")"
others="$(grep 'PROBLEM' "$W/check.log" | grep -vi 'docker' || true)"
[[ -z "$others" ]] && ok "--check finds no problem but Docker's" || ko "--check: $others"

if [[ -x /bin/bash ]] && /bin/bash -c '((BASH_VERSINFO[0] < 4))'; then
  /bin/bash scripts/dev-lite.sh --check >"$W/old.log" 2>&1
  [[ $? -eq 1 ]] && grep -q 'brew install bash' "$W/old.log" && ok "bash $(/bin/bash -c 'echo $BASH_VERSION') gets the install hint" ||
    ko "bash 3: $(cat "$W/old.log")"
else
  echo "SKIP  /bin/bash is bash 4 or later here, so the hint for bash 3 is not tested"
fi

echo "$pass passed, $fail failed"
((fail == 0))
