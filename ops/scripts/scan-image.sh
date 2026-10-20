#!/usr/bin/env bash
# Scan one release image with Trivy before it is pushed (MIL-022 tasks 1 and 2, DSA-001
# section 8): a JSON report, the same findings as SARIF for code scanning, and an SPDX
# SBOM of the image. Then the gate of scripts/scan-gate.sh decides, unless
# --report-only (the frozen image of ADR-0500, which cannot be rebuilt to fix anything).
# Trivy runs in a container at a pinned digest and reads the image from the local
# Docker daemon through its socket, so nothing is pushed and no registry login is needed.
# Its vulnerability database is kept in the Docker volume nfhotel-trivy-cache.
#
# Usage: scan-image.sh [--report-only] <image> <name> <output folder>
#   writes <folder>/<name>.trivy.json, <name>.sarif and <name>.spdx.json
# Exit:  0 the gate passes (or --report-only); 1 it fails or a scan failed; 2 usage error
# Needs: bash 4+, docker, jq, GNU date

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"

# Trivy 0.75.0; Dependabot does not see this line, so bump it by hand with the digest.
readonly TRIVY="aquasec/trivy:0.75.0@sha256:af6acf9a6b85dfe389a1941505c0ce9efef52a4719635e1a962f022a3d855daa"
readonly IGNORE="$ROOT/security/trivyignore.yaml"

report_only=0
if [[ "${1:-}" == "--report-only" ]]; then
  report_only=1
  shift
fi
[[ $# -eq 3 ]] || {
  usage >&2
  exit 2
}
image="$1" name="$2" out="$3"
[[ "$name" =~ ^[a-z0-9-]+$ ]] || die "the name '$name' must be lower case letters, digits and -"
mkdir -p "$out"
bash "$ROOT/scripts/scan-gate.sh" --check-ignore "$IGNORE" >/dev/null ||
  die "security/trivyignore.yaml is not valid (run scripts/scan-gate.sh --check-ignore on it)"

# trivy <trivy arguments...>: run Trivy with the ignore file and the daemon socket;
# the ignore file goes in on standard input, the result comes out on standard output.
# MSYS_NO_PATHCONV keeps Git Bash on Windows from rewriting the container paths.
trivy() {
  MSYS_NO_PATHCONV=1 docker run --rm -i \
    -v /var/run/docker.sock:/var/run/docker.sock \
    -v nfhotel-trivy-cache:/root/.cache/trivy \
    --entrypoint sh "$TRIVY" -c 'cat >/tmp/trivyignore.yaml && exec trivy "$@"' trivy "$@" <"$IGNORE"
}

echo "==> scanning $image"
trivy image --quiet --scanners vuln --format json --ignorefile /tmp/trivyignore.yaml \
  "$image" >"$out/$name.trivy.json" || die "the Trivy scan of $image failed"
# The SARIF report is made from the JSON one, so both hold the same findings.
MSYS_NO_PATHCONV=1 docker run --rm -i --entrypoint sh "$TRIVY" -c \
  'cat >/tmp/report.json && exec trivy convert --quiet --format sarif /tmp/report.json' \
  <"$out/$name.trivy.json" >"$out/$name.sarif" || die "converting the report of $image to SARIF failed"
trivy image --quiet --format spdx-json "$image" >"$out/$name.spdx.json" ||
  die "the SBOM of $image failed"
jq -e '.spdxVersion and (.packages | length > 0)' "$out/$name.spdx.json" >/dev/null ||
  die "the SBOM of $image has no packages"
echo "    $(jq '.packages | length' "$out/$name.spdx.json") packages in the SBOM"

if ((report_only)); then
  bash "$ROOT/scripts/scan-gate.sh" "$out/$name.trivy.json" ||
    echo "    report only: $name is frozen (ADR-0500), so these findings do not block the release"
  exit 0
fi
exec bash "$ROOT/scripts/scan-gate.sh" "$out/$name.trivy.json"
