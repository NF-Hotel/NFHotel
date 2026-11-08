#!/usr/bin/env bash
# Tests of scripts/scan-gate.sh (MIL-022 task 1, DSA-001 section 8): the gate fails on a
# secret in the image, on a critical finding with or without a fix and on a high one with a
# fix, and passes a high
# one without a fix, medium findings and an empty report; the ignore file check refuses
# an entry without a statement, without an expiry or with an expiry beyond 30 days, and
# before that a file that Trivy cannot parse (a plain statement with ": ", a value without
# its closing quote, one that starts with a YAML indicator), a key that Trivy does not read
# and a key twice, with the line number (MIL-066 task 5).
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
echo '{"ArtifactName":"test/image:1","Results":[{"Target":"app/appsettings.json","Secrets":[{"RuleID":"aws-access-key-id","Severity":"CRITICAL","Title":"AWS"}]}]}' >"$W/secret.json"
bash "$GATE" "$W/secret.json" >/dev/null && ko "a secret in the image fails" || ok "a secret in the image fails"
out="$(bash "$GATE" "$W/secret.json" 2>&1)"
[[ "$out" == *"aws-access-key-id"*"app/appsettings.json"* ]] && ok "the secret is named with its file" || ko "the secret is named with its file"
echo '{"ArtifactName":"test/image:1","Results":[{"Target":"t","Secrets":[]}]}' >"$W/nosecret.json"
bash "$GATE" "$W/nosecret.json" >/dev/null && ok "an empty secret list passes" || ko "an empty secret list passes"
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

# MIL-066 task 5: what Trivy cannot parse fails before the entries, with its line number.
ignore "  - id: CVE-2026-1" "    statement: Not reachable: the service never parses untrusted archives." "    expired_at: 2026-11-19"
check && ko "a plain statement with \": \" fails (Trivy cannot parse it)" || ok "a plain statement with \": \" fails (Trivy cannot parse it)"
out="$(bash "$GATE" --check-ignore "$W/ignore.yaml" 2026-10-20 2>&1)"
[[ "$out" == *"line 3: a plain value with \": \""* ]] && ok "the failure names the line" || ko "the failure names the line: $out"
ignore "  - id: CVE-2026-1" "    statement: \"Not reachable: the service never parses untrusted archives.\"" "    expired_at: 2026-11-19"
check && ok "the same statement in double quotes passes" || ko "the same statement in double quotes passes"
ignore "  - id: CVE-2026-1" "    statement: \"Not used: the \\\"x\\\" tool never runs.\"" "    expired_at: 2026-11-19"
check && ok "a double-quoted statement with escaped quotes passes" || ko "a double-quoted statement with escaped quotes passes"
ignore "  - id: CVE-2026-1" "    statement: 'Not used: it''s never run.'" "    expired_at: 2026-11-19"
check && ok "a single-quoted statement with a doubled quote passes" || ko "a single-quoted statement with a doubled quote passes"
ignore "  - id: CVE-2026-1" "    statement: \"Not used: no closing quote" "    expired_at: 2026-11-19"
check && ko "a statement without its closing quote fails" || ok "a statement without its closing quote fails"
ignore "  - id: CVE-2026-1" "    statement: *anchor" "    expired_at: 2026-11-19"
check && ko "a plain statement that starts with a YAML indicator fails" || ok "a plain statement that starts with a YAML indicator fails"
ignore "  - id: CVE-2026-1" "    statement: # later" "    expired_at: 2026-11-19"
check && ko "a statement that is only a comment has no reason and fails" || ok "a statement that is only a comment has no reason and fails"
ignore "  - id: CVE-2026-1" "    path:" "      - usr/bin/x" "    statement: Not used." "    expired_at: 2026-11-19"
check && ko "a key that Trivy does not read fails (path for paths would ignore the finding in every image)" ||
  ok "a key that Trivy does not read fails (path for paths would ignore the finding in every image)"
ignore "  - id: CVE-2026-1" "    statement: Not used." "    statement: Not reachable." "    expired_at: 2026-11-19"
check && ko "a key twice in one entry fails" || ok "a key twice in one entry fails"
ignore "  - id: CVE-2026-1" "    paths: [\"usr/bin/x\", \"usr/bin/y\"]   # narrows the entry" "    statement: Not used." "    expired_at: 2026-11-19"
check && ok "a list of double-quoted paths in [ ] passes" || ko "a list of double-quoted paths in [ ] passes"
ignore "  - id: CVE-2026-1" "    purls:" "      - pkg:golang/golang.org/x/net" "      - \"pkg:deb/debian/zlib1g@1.3?arch=amd64&distro=debian-13\"" \
  "    statement: Not used." "    expired_at: 2026-11-19"
check && ok "a list of purls, plain and quoted, passes" || ko "a list of purls, plain and quoted, passes"

# --fixable (ADR-0032): code scanning gets the vulnerabilities with a fixed version and every
# secret; a result without vulnerabilities and an empty report stay valid.
fixable() { bash "$GATE" --fixable "$1" | jq -c "$2"; }
[[ "$(fixable "$W/medium.json" '[.Results[].Vulnerabilities[].VulnerabilityID]')" == '["CVE-2026-0001","CVE-2026-0002"]' ]] &&
  ok "--fixable keeps the findings with a fix and drops the one without" || ko "--fixable: $(fixable "$W/medium.json" '.')"
[[ "$(fixable "$W/critical-nofix.json" '[.Results[].Vulnerabilities[]] | length')" == 0 ]] &&
  ok "--fixable drops a critical finding without a fix" || ko "--fixable critical without fix"
[[ "$(fixable "$W/none.json" '.Results | length')" == 1 ]] && ok "--fixable keeps a result without vulnerabilities" ||
  ko "--fixable none"
echo '{"ArtifactName":"a","Results":[{"Target":"t","Secrets":[{"RuleID":"aws-access-key-id","Severity":"CRITICAL"}]}]}' >"$W/secret.json"
[[ "$(fixable "$W/secret.json" '[.Results[].Secrets[]] | length')" == 1 ]] && ok "--fixable keeps every secret" ||
  ko "--fixable secret"
echo '{"ArtifactName":"a"}' >"$W/no-results.json"
[[ "$(fixable "$W/no-results.json" '.Results')" == "[]" ]] && ok "--fixable accepts a report without results" ||
  ko "--fixable no results"
bash "$GATE" --fixable "$W/missing.json" >/dev/null 2>&1
[[ $? == 2 ]] && ok "--fixable refuses a missing report (exit 2)" || ko "--fixable missing report"

echo "==> $pass passed, $fail failed"
((fail == 0))
