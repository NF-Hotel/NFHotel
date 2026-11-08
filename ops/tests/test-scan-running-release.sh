#!/usr/bin/env bash
# Tests of scripts/scan-running-release.sh (MIL-036 task 1, MIL-064 task 6): every image of the
# manifest is pulled by its digest and scanned with the gate; a failed gate or a failed pull fails
# the run and is written to the summary for the issue; a clean release passes; a manifest that
# is not as expected is refused. With --third-party, every third-party image that the compose
# files of the deploy folder pin by digest is scanned once, in its own section of the summary,
# and removed after its scan: a compose file with a known vulnerable image fails the run, one
# without findings passes; a variable, an alias, a comment and the development-only stack
# proxmox are left out; a folder without such images is refused before anything is pulled.
# A summary too long for an issue is cut. The steps of the workflow (running-image-scan-steps.sh):
# find writes the manifest of a test image, or downloads the one of the release in production and
# names its operations commit, and refuses a release without one; scan passes --third-party only
# with an operations commit and reports a failed gate in its output, not as a failed step. Fakes
# stand in for docker, gh, scan-image.sh and, for the steps, scan-running-release.sh, so nothing is
# pulled, downloaded or scanned.
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

# The fakes log their calls. FAKE_FAIL_SCAN is a part of the reference of the image whose gate
# fails, FAKE_FAIL_PULL of the one that cannot be pulled; with FAKE_LONG every gate fails with
# a long report.
cat >"$W/docker" <<'EOF'
#!/bin/bash
echo "docker $*" >>"$FAKE_LOG"
[[ "$1" == pull && -n "${FAKE_FAIL_PULL:-}" && "$*" == *"$FAKE_FAIL_PULL"* ]] && exit 1
exit 0
EOF
cat >"$W/scan-image" <<'EOF'
#!/bin/bash
echo "scan $*" >>"$FAKE_LOG"
if [[ -n "${FAKE_LONG:-}" ]]; then
  for i in $(seq 60); do printf 'HIGH  CVE-2099-%04d  libexample 1.0 -> 1.1 %0280d\n' "$i" 0; done
  exit 1
fi
if [[ -n "${FAKE_FAIL_SCAN:-}" && "$1" == *"$FAKE_FAIL_SCAN"* ]]; then
  echo "CRITICAL CVE-2099-0001 libexample 1.0 (fixed in 1.1)"
  exit 1
fi
exit 0
EOF
chmod +x "$W/docker" "$W/scan-image"
export FAKE_LOG="$W/log" NFHOTEL_DOCKER="$W/docker" NFHOTEL_SCAN_IMAGE="$W/scan-image"

d() { printf 'sha256:%064d' "$1"; }
cat >"$W/release.json" <<EOF
{ "sha": "$(printf '%040d' 7)", "devops_sha": "$(printf '%040d' 8)", "images": {
  "nfhotel-web": "ghcr.io/nf-hotel/nfhotel-web@$(d 1)",
  "nfhotel-api": "ghcr.io/nf-hotel/nfhotel-api@$(d 2)",
  "market-analysis": "ghcr.io/nf-hotel/market-analysis@$(d 3)",
  "benchmark": "ghcr.io/nf-hotel/benchmark@$(d 4)",
  "nfhotel-nginx": "ghcr.io/nf-hotel/nfhotel-nginx@$(d 5)" } }
EOF

# A deploy folder as ops/deploy has it: the images of the release by variable and alias, a local
# build, third-party images on one line, quoted, folded and commented out, one image pinned in
# two stacks, and the development-only stack proxmox.
mkdir -p "$W/deploy/web" "$W/deploy/db" "$W/deploy/tunnel" "$W/deploy/backup" "$W/deploy/proxmox" "$W/deploy/notes"
cat >"$W/deploy/web/docker-compose.yml" <<'EOF'
x-web-image: &web-image ${NFHOTEL_WEB_IMAGE:-nfhotel-web:local}
services:
  web:
    image: *web-image
  api:
    image: ${NFHOTEL_API_IMAGE:-nfhotel-api:local}
    build:
      context: .
EOF
cat >"$W/deploy/db/docker-compose.yml" <<EOF
services:
  db:
    # image: example/commented:1.0@$(d 19)
    image: example/vulnerable:1.0@$(d 11)
  relay:
    image: "example/clean:2.0@$(d 12)"  # quoted, with a comment
EOF
cat >"$W/deploy/tunnel/docker-compose.yml" <<EOF
services:
  tunnel:
    image: >-
      example/folded:3.0@$(d 13)
  sidecar:
    image: example/clean:2.0@$(d 12)
EOF
cat >"$W/deploy/backup/docker-compose.yml" <<'EOF'
services:
  backup:
    build: .
    image: example-backup:local
EOF
cat >"$W/deploy/proxmox/docker-compose.yml" <<EOF
services:
  proxmox:
    image: example/lab:9.0@$(d 14)
EOF
echo "no compose file here" >"$W/deploy/notes/README.md"

# run [<argument>...]: the scan of $MANIFEST (default release.json) with the arguments added.
run() {
  : >"$W/log"
  rm -rf "$W/out"
  bash "$OPS/scripts/scan-running-release.sh" --manifest "${MANIFEST:-$W/release.json}" --results "$W/out" "$@" \
    >"$W/stdout" 2>&1
}
scans() { grep -c '^scan ' "$W/log"; }

echo "== the release images"
run && ok "a clean release passes" || ko "clean: $(cat "$W/stdout")"
[[ "$(grep -c '^docker pull' "$W/log")" == 5 && "$(scans)" == 5 ]] &&
  ok "every image is pulled and scanned" || ko "calls: $(cat "$W/log")"
grep -q "pull -q ghcr.io/nf-hotel/nfhotel-web@$(d 1)" "$W/log" && ok "an image is pulled by its digest" || ko "pull by digest"
# Since ADR-0030 no image is exempt from the gate: market-analysis is scanned like the others.
grep -q "^scan ghcr.io/nf-hotel/market-analysis@" "$W/log" &&
  ok "market-analysis is scanned with the gate" || ko "market-analysis: $(grep '^scan' "$W/log")"
grep -q "found nothing that the gate refuses" "$W/out/summary.md" && ok "a clean summary says so" || ko "clean summary"
grep -qF -- "- **nfhotel-nginx**: passes (\`ghcr.io/nf-hotel/nfhotel-nginx@$(d 5)\`)" "$W/out/summary.md" &&
  ok "the summary lists each release image with its result and digest" || ko "release list: $(cat "$W/out/summary.md")"
! grep -q -e "## Third-party images" -e "Rebuild and release" -e "is cut at" "$W/out/summary.md" &&
  ! grep -q "image rm" "$W/log" && ok "without --third-party no third-party image is scanned (the test image)" ||
  ko "third-party without the option: $(cat "$W/out/summary.md")"

FAKE_FAIL_SCAN=nfhotel-api run
[[ $? == 1 ]] && grep -q "### nfhotel-api" "$W/out/summary.md" && grep -q "CVE-2099-0001" "$W/out/summary.md" &&
  ! grep -q "### nfhotel-web" "$W/out/summary.md" && ok "a failed gate fails the run and names the image and the finding" ||
  ko "gate: $(cat "$W/out/summary.md" 2>&1)"
grep -q "Rebuild and release the image" "$W/out/summary.md" && grep -q "found vulnerabilities" "$W/out/summary.md" &&
  ok "a failed release image gets the remedy of a release" || ko "remedy: $(cat "$W/out/summary.md")"
[[ "$(scans)" == 5 ]] && ok "the other images are still scanned after a failure" || ko "stopped early"

FAKE_FAIL_PULL=benchmark run
[[ $? == 1 ]] && grep -q "benchmark\*\*: the image could not be pulled" "$W/out/summary.md" &&
  ok "an image that cannot be pulled fails the run" || ko "pull failure: $(cat "$W/out/summary.md" 2>&1)"

echo '{ "sha": "not-a-sha", "images": {} }' >"$W/bad.json"
MANIFEST="$W/bad.json" run && ko "accepted a manifest without a commit" || ok "a manifest without a release commit is refused"
printf '{ "sha": "%s", "images": { "x": "ghcr.io/x:latest" } }' "$(printf '%040d' 7)" >"$W/tag.json"
MANIFEST="$W/tag.json" run && ko "accepted an image by tag" || ok "an image that is not pinned by digest is refused"

echo "== the third-party images (--third-party)"
run --third-party "$W/deploy" && grep -q "## Third-party images" "$W/out/summary.md" && ok "third-party images without findings pass" ||
  ko "third-party clean: $(cat "$W/stdout")"
for ref in "example/vulnerable:1.0@$(d 11)" "example/clean:2.0@$(d 12)" "example/folded:3.0@$(d 13)"; do
  grep -q "^docker pull -q $ref$" "$W/log" && grep -q "^scan $ref " "$W/log" && grep -qF "(\`$ref\`)" "$W/out/summary.md" ||
    ko "not pulled, scanned and listed by its digest: $ref"
done
[[ "$(scans)" == 8 ]] && ok "each pinned third-party image is pulled, scanned and listed by its digest, once" ||
  ko "scans: $(grep '^scan' "$W/log")"
grep -qF -- "- **example/clean:2.0** (db, tunnel): passes" "$W/out/summary.md" &&
  ok "an image pinned in two stacks is scanned once and names both" || ko "two stacks: $(cat "$W/out/summary.md")"
grep -q "example/folded:3.0@" "$W/log" && ok "a folded value (image: >-) is read" || ko "folded"
! grep -q -e "example/lab" -e "example/commented" -e "example-backup" -e "nfhotel-web:local" -e "nfhotel-api:local" "$W/log" &&
  ok "proxmox, a comment, a local build, a variable and an alias are left out" || ko "left out: $(cat "$W/log")"
[[ "$(grep -c '^docker image rm ' "$W/log")" == 3 ]] && ! grep -q "image rm ghcr.io" "$W/log" &&
  ok "each third-party image is removed after its scan, a release image is not" || ko "removed: $(grep 'image rm' "$W/log")"
[[ -f "$W/out/third-party/example-vulnerable-000000000000.gate.txt" ]] &&
  ok "the reports of a third-party image are in third-party/, named by its path and digest" || ko "reports: $(ls -R "$W/out")"
grep -q "at the operations commit \`0000000\`" "$W/out/summary.md" && ! grep -q "remedy under ADR-0040" "$W/out/summary.md" &&
  ok "the section names the operations commit, and no remedy when all pass" || ko "section: $(cat "$W/out/summary.md")"

FAKE_FAIL_SCAN=example/vulnerable run --third-party "$W/deploy"
[[ $? == 1 ]] && grep -q "found vulnerabilities" "$W/out/summary.md" && grep -q "### example/vulnerable:1.0" "$W/out/summary.md" &&
  grep -q "CVE-2099-0001" "$W/out/summary.md" && grep -qF -- "- **example/vulnerable:1.0** (db): fails the gate" "$W/out/summary.md" &&
  ok "a compose file with a known vulnerable image fails the run and names the image and the finding" ||
  ko "vulnerable: $(cat "$W/out/summary.md" 2>&1)"
grep -q "remedy under ADR-0040" "$W/out/summary.md" && ! grep -q "Rebuild and release" "$W/out/summary.md" &&
  ! grep -q "### nfhotel-" "$W/out/summary.md" && ok "a failed third-party image gets the remedies of ADR-0040, the release images pass" ||
  ko "third-party remedy: $(cat "$W/out/summary.md")"
[[ "$(scans)" == 8 ]] && ok "the other third-party images are still scanned after a failure" || ko "stopped early"

FAKE_FAIL_PULL=example/folded run --third-party "$W/deploy"
[[ $? == 1 ]] && grep -qF -- "- **example/folded:3.0** (tunnel): the image could not be pulled" "$W/out/summary.md" &&
  ok "a third-party image that cannot be pulled fails the run" || ko "third-party pull: $(cat "$W/out/summary.md" 2>&1)"

jq 'del(.devops_sha)' "$W/release.json" >"$W/no-ops.json"
MANIFEST="$W/no-ops.json" run --third-party "$W/deploy" && ko "accepted a manifest without an operations commit" ||
  { [[ ! -s "$W/log" ]] && ok "--third-party with a manifest without an operations commit is refused"; } || ko "pulled after a refusal"
run --third-party "$W/missing" && ko "accepted a missing folder" ||
  { [[ ! -s "$W/log" ]] && ok "a missing folder is refused before anything is pulled"; } || ko "pulled after a refusal"
run --third-party "$W/deploy/notes" && ko "accepted a folder without compose files" ||
  { [[ ! -s "$W/log" ]] && ok "a folder without a stack is refused before anything is pulled"; } || ko "pulled after a refusal"
mkdir -p "$W/lab"
cp -r "$W/deploy/proxmox" "$W/deploy/web" "$W/deploy/backup" "$W/lab/"
run --third-party "$W/lab" && ko "accepted a folder without third-party images" ||
  { [[ ! -s "$W/log" ]] && grep -q "pin no third-party image" "$W/stdout" &&
    ok "a folder whose compose files pin no third-party image is refused before anything is pulled"; } ||
  ko "no third-party: $(cat "$W/stdout")"
run --third-party && ko "accepted --third-party without a folder" || ok "--third-party without a folder is a usage error"

echo "== a summary too long for an issue"
FAKE_LONG=1 run --third-party "$W/deploy"
status=$?
size="$(wc -c <"$W/out/summary.md")"
[[ $status == 1 && "$size" -le 60200 ]] && tail -n 1 "$W/out/summary.md" | grep -q "is cut at 60000 characters" &&
  ok "a summary over 60000 characters is cut, with a note ($size bytes)" || ko "cut: $size bytes, $(tail -n 2 "$W/out/summary.md")"
(($(grep -c '^```' "$W/out/summary.md") % 2 == 0)) && ok "the cut summary closes its code block" || ko "open code block"

echo "== the steps of the workflow (running-image-scan-steps.sh)"
# The fake gh: the last CD run has the head FAKE_CD_HEAD, the release run of that commit is
# FAKE_RELEASE_RUN, and its artifact holds FAKE_MANIFEST. The fake scan logs its arguments.
cat >"$W/gh" <<'EOF'
#!/bin/bash
echo "gh $*" >>"$FAKE_LOG"
case "$*" in
  "run list"*"--workflow cd.yaml"*) echo "${FAKE_CD_HEAD:-}" ;;
  "run list"*"--workflow release.yaml"*) echo "${FAKE_RELEASE_RUN:-}" ;;
  "run download"*) mkdir -p "${!#}" && cp "$FAKE_MANIFEST" "${!#}/release.json" ;;
  *) exit 1 ;;
esac
EOF
cat >"$W/scan-release" <<'EOF'
#!/bin/bash
echo "scan-release $*" >>"$FAKE_LOG"
mkdir -p results && echo "summary of the fake scan" >results/summary.md
exit "${FAKE_SCAN_EXIT:-0}"
EOF
chmod +x "$W/gh" "$W/scan-release"
head7="$(printf '%040d' 7)" ops8="$(printf '%040d' 8)" sha9="$(printf '%040d' 9)"
# steps <subcommand>: run a step in an empty job folder, with the variables of the runner.
steps() {
  rm -rf "$W/job" && mkdir -p "$W/job" && : >"$FAKE_LOG"
  (
    cd "$W/job" || exit 1
    export REPO=NF-Hotel/NFHotel GITHUB_SHA="$sha9" GITHUB_OUTPUT="$W/job/output"
    export GITHUB_STEP_SUMMARY="$W/job/step-summary" NFHOTEL_GH="$W/gh" NFHOTEL_SCAN_RELEASE="$W/scan-release"
    bash "$OPS/scripts/running-image-scan-steps.sh" "$@"
  ) >"$W/stdout" 2>&1
}

TEST_IMAGE="example/vulnerable:1.0@$(d 11)" steps find &&
  [[ "$(jq -r '.images.test' "$W/job/release/release.json")" == "example/vulnerable:1.0@$(d 11)" ]] &&
  [[ ! -s "$W/job/output" ]] && ! grep -q '^gh ' "$W/log" &&
  ok "find with a test image writes a manifest of that image, without an operations commit or a download" ||
  ko "find test image: $(cat "$W/stdout")"
FAKE_CD_HEAD="$head7" FAKE_RELEASE_RUN=4242 FAKE_MANIFEST="$W/release.json" steps find &&
  [[ "$(cat "$W/job/output")" == "devops_sha=$ops8" ]] && cmp -s "$W/release.json" "$W/job/release/release.json" &&
  grep -qF "Release in production: $head7 (NF Hotel Release run 4242), operations commit $ops8" "$W/job/step-summary" &&
  grep -q "^gh run list --repo NF-Hotel/NFHotel --workflow release.yaml --commit $head7 " "$W/log" &&
  grep -q "^gh run download 4242 --repo NF-Hotel/NFHotel --name release-manifest --dir release$" "$W/log" &&
  ok "find downloads the manifest of the release in production and writes its operations commit" ||
  ko "find release: $(cat "$W/stdout" "$W/log")"
FAKE_CD_HEAD="" steps find && ko "find accepted no CD run" ||
  { grep -q "::error::no successful CD run found" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "find without a successful CD run fails with the reason"; } || ko "no CD run: $(cat "$W/stdout")"
FAKE_CD_HEAD="$head7" FAKE_RELEASE_RUN="" steps find && ko "find accepted no release run" ||
  { grep -q "::error::no release run for $head7" "$W/stdout" && ! grep -q '^gh run download' "$W/log" &&
    ok "find without a release run of that commit fails before a download"; } || ko "no release: $(cat "$W/stdout")"
FAKE_CD_HEAD="$head7" FAKE_RELEASE_RUN=4242 FAKE_MANIFEST="$W/no-ops.json" steps find && ko "find accepted no operations commit" ||
  { grep -q "::error::the release.json of $head7 has no operations commit" "$W/stdout" && [[ ! -s "$W/job/output" ]] &&
    ok "find refuses a release.json without an operations commit"; } || ko "find no ops: $(cat "$W/stdout")"
DEVOPS_SHA="$ops8" FAKE_SCAN_EXIT=1 steps scan &&
  grep -q "^scan-release --manifest release/release.json --results results --third-party running/ops/deploy$" "$W/log" &&
  [[ "$(cat "$W/job/output")" == failed=true ]] && grep -q "summary of the fake scan" "$W/job/step-summary" &&
  ok "scan with an operations commit scans the third-party images, and a failed gate is an output, not a failed step" ||
  ko "scan third-party: $(cat "$W/stdout" "$W/log")"
steps scan && grep -q "^scan-release --manifest release/release.json --results results$" "$W/log" &&
  [[ "$(cat "$W/job/output")" == failed=false ]] &&
  ok "scan without an operations commit (a test image) scans the manifest only" || ko "scan alone: $(cat "$W/stdout" "$W/log")"
steps
[[ $? == 2 ]] && ok "the steps without a subcommand are a usage error" || ko "no subcommand: $(cat "$W/stdout")"
steps deploy
[[ $? == 2 ]] && ok "an unknown step is a usage error" || ko "unknown step: $(cat "$W/stdout")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
