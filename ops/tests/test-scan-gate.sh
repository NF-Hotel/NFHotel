#!/usr/bin/env bash
# Tests of scripts/scan-gate.sh (MIL-022 task 1, DSA-001 section 8): the gate fails on a
# critical finding with or without a fix and on a high one with a fix, and passes a high
# one without a fix, medium findings and an empty report; the ignore file check refuses
# an entry without a statement, without an expiry or with an expiry beyond 30 days.
# Hand-written Trivy reports stand in for real scans.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-scan-gate.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (apt-get, jq)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null && apt-get -qq install -y jq >/dev/null 2>&1
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GATE="$REPO/scripts/scan-gate.sh"
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

# report <file> <severity:fixed version>...: a Trivy report with one finding per argument.
report() {
  local file="$1" vulns="" sev fix n=0
  shift
  for f in "$@"; do
    sev="${f%%:*}" fix="${f#*:}" n=$((n + 1))
    vulns+="${vulns:+,}{\"VulnerabilityID\":\"CVE-2026-000$n\",\"PkgName\":\"pkg$n\",\"InstalledVersion\":\"1.0\",\"FixedVersion\":\"$fix\",\"Severity\":\"$sev\"}"
  done
  echo "{\"ArtifactName\":\"test/image:1\",\"Results\":[{\"Target\":\"t\",\"Vulnerabilities\":[$vulns]}]}" >"$file"
}

report "$W/empty.json"
echo '{"ArtifactName":"test/image:1","Results":[{"Target":"t"}]}' >"$W/none.json"
report "$W/critical-fix.json" "CRITICAL:1.1"
report "$W/critical-nofix.json" "CRITICAL:"
report "$W/high-fix.json" "HIGH:2.0"
report "$W/high-nofix.json" "HIGH:"
report "$W/medium.json" "MEDIUM:1.1" "LOW:1.1" "HIGH:"

bash "$GATE" "$W/empty.json" >/dev/null && ok "an empty report passes" || ko "an empty report passes"
bash "$GATE" "$W/none.json" >/dev/null && ok "a result without vulnerabilities passes" || ko "a result without vulnerabilities passes"
bash "$GATE" "$W/critical-fix.json" >/dev/null && ko "a critical finding with a fix fails" || ok "a critical finding with a fix fails"
bash "$GATE" "$W/critical-nofix.json" >/dev/null && ko "a critical finding without a fix fails" || ok "a critical finding without a fix fails"
bash "$GATE" "$W/high-fix.json" >/dev/null && ko "a high finding with a fix fails" || ok "a high finding with a fix fails"
bash "$GATE" "$W/high-nofix.json" >/dev/null && ok "a high finding without a fix passes" || ko "a high finding without a fix passes"
bash "$GATE" "$W/medium.json" >/dev/null && ok "medium, low and unfixed high findings pass" || ko "medium, low and unfixed high findings pass"
out="$(bash "$GATE" "$W/high-fix.json" 2>&1)"
[[ "$out" == *"CVE-2026-0001"*"pkg1 1.0 -> 2.0"* ]] && ok "the blocking finding is named with its fix" || ko "the blocking finding is named with its fix"
bash "$GATE" "$W/missing.json" >/dev/null 2>&1 && ko "a missing report fails" || ok "a missing report fails"
bash "$GATE" >/dev/null 2>&1
[[ $? -eq 2 ]] && ok "no argument is a usage error" || ko "no argument is a usage error"

ignore() {
  printf 'vulnerabilities:\n' >"$W/ignore.yaml"
  printf '%s\n' "$@" >>"$W/ignore.yaml"
}
check() { bash "$GATE" --check-ignore "$W/ignore.yaml" 2026-10-20 >/dev/null; }

bash "$GATE" --check-ignore "$REPO/security/trivyignore.yaml" >/dev/null &&
  ok "the ignore file of this repository is valid" || ko "the ignore file of this repository is valid"
ignore "  - id: CVE-2026-1" "    statement: Not reachable." "    expired_at: 2026-11-19"
check && ok "an entry with a reason and a 30-day expiry passes" || ko "an entry with a reason and a 30-day expiry passes"
ignore "  - id: CVE-2026-1" "    statement: Not reachable." "    expired_at: 2026-11-20"
check && ko "an expiry of 31 days fails" || ok "an expiry of 31 days fails"
ignore "  - id: CVE-2026-1" "    expired_at: 2026-11-01"
check && ko "an entry without a statement fails" || ok "an entry without a statement fails"
ignore "  - id: CVE-2026-1" "    statement: Not reachable."
check && ko "an entry without an expiry fails" || ok "an entry without an expiry fails"
ignore "  - id: CVE-2026-1" "    statement: Not reachable." "    expired_at: 2026-11-01" \
  "  - id: CVE-2026-2" "    statement: Not reachable."
check && ko "the second entry is checked on its own" || ok "the second entry is checked on its own"
ignore "  - id: CVE-2026-1" "    statement: Old." "    expired_at: 2026-10-01"
check && ok "an expired entry passes with a note (Trivy no longer ignores it)" || ko "an expired entry passes with a note"

echo "==> $pass passed, $fail failed"
((fail == 0))
