#!/usr/bin/env bash
# Tests of scripts/scan-running-release.sh (MIL-036 task 1): every image of the manifest is
# pulled by its digest and scanned; the frozen image is scanned with --report-only; a failed
# gate or a failed pull fails the run and is written to the summary for the issue; a clean
# release passes; a manifest that is not as expected is refused. Fakes stand in for docker
# and for scan-image.sh, so nothing is pulled or scanned.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-scan-running-release.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (jq is installed by the test)

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

command -v jq >/dev/null || { apt-get update -qq && apt-get install -y -qq jq >/dev/null; } || exit 1

# The fakes log their calls. FAKE_FAIL_SCAN names the image whose gate fails, FAKE_FAIL_PULL
# the one that cannot be pulled.
cat >"$W/docker" <<'EOF'
#!/bin/bash
echo "docker $*" >>"$FAKE_LOG"
[[ "$1" == pull && -n "${FAKE_FAIL_PULL:-}" && "$*" == *"/$FAKE_FAIL_PULL@"* ]] && exit 1
exit 0
EOF
cat >"$W/scan-image" <<'EOF'
#!/bin/bash
echo "scan $*" >>"$FAKE_LOG"
args=("$@")
name="${args[-2]}"
if [[ "$name" == "${FAKE_FAIL_SCAN:-}" ]]; then
  echo "CRITICAL CVE-2099-0001 libexample 1.0 (fixed in 1.1)"
  exit 1
fi
exit 0
EOF
chmod +x "$W/docker" "$W/scan-image"
export FAKE_LOG="$W/log" NFHOTEL_DOCKER="$W/docker" NFHOTEL_SCAN_IMAGE="$W/scan-image"

d() { printf 'sha256:%064d' "$1"; }
cat >"$W/release.json" <<EOF
{ "sha": "$(printf '%040d' 7)", "images": {
  "nfhotel-web": "ghcr.io/nf-hotel/nfhotel-web@$(d 1)",
  "nfhotel-api": "ghcr.io/nf-hotel/nfhotel-api@$(d 2)",
  "market-analysis": "ghcr.io/nf-hotel/market-analysis@$(d 3)",
  "benchmark": "ghcr.io/nf-hotel/benchmark@$(d 4)" } }
EOF
run() {
  : >"$W/log"
  rm -rf "$W/out"
  bash "$OPS/scripts/scan-running-release.sh" --manifest "${1:-$W/release.json}" --results "$W/out" >"$W/stdout" 2>&1
}

run && ok "a clean release passes" || ko "clean: $(cat "$W/stdout")"
[[ "$(grep -c '^docker pull' "$W/log")" == 4 && "$(grep -c '^scan ' "$W/log")" == 4 ]] &&
  ok "every image is pulled and scanned" || ko "calls: $(cat "$W/log")"
grep -q "pull -q ghcr.io/nf-hotel/nfhotel-web@$(d 1)" "$W/log" && ok "an image is pulled by its digest" || ko "pull by digest"
grep -q "^scan --report-only ghcr.io/nf-hotel/market-analysis@" "$W/log" && ! grep -q "^scan --report-only ghcr.io/nf-hotel/nfhotel-web@" "$W/log" &&
  ok "only the frozen image is scanned with --report-only" || ko "report-only: $(grep '^scan' "$W/log")"
grep -q "found nothing that the gate refuses" "$W/out/summary.md" && ok "a clean summary says so" || ko "clean summary"

FAKE_FAIL_SCAN=nfhotel-api run
[[ $? == 1 ]] && grep -q "### nfhotel-api" "$W/out/summary.md" && grep -q "CVE-2099-0001" "$W/out/summary.md" &&
  ! grep -q "### nfhotel-web" "$W/out/summary.md" && ok "a failed gate fails the run and names the image and the finding" ||
  ko "gate: $(cat "$W/out/summary.md" 2>&1)"
[[ "$(grep -c '^scan ' "$W/log")" == 4 ]] && ok "the other images are still scanned after a failure" || ko "stopped early"

FAKE_FAIL_PULL=benchmark run
[[ $? == 1 ]] && grep -q "benchmark\*\*: the image could not be pulled" "$W/out/summary.md" &&
  ok "an image that cannot be pulled fails the run" || ko "pull failure: $(cat "$W/out/summary.md" 2>&1)"

echo '{ "sha": "not-a-sha", "images": {} }' >"$W/bad.json"
run "$W/bad.json" && ko "accepted a manifest without a commit" || ok "a manifest without a release commit is refused"
printf '{ "sha": "%s", "images": { "x": "ghcr.io/x:latest" } }' "$(printf '%040d' 7)" >"$W/tag.json"
run "$W/tag.json" && ko "accepted an image by tag" || ok "an image that is not pinned by digest is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
