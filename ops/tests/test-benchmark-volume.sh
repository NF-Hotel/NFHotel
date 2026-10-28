#!/usr/bin/env bash
# Test of the results volume of the benchmark service (MIL-045): deploy/benchmark/docker-compose.yml
# with a stand-in image that runs as the user app (uid 10001), like the real one. A volume made
# earlier by another uid (1000) is given to app by the one-shot service results-owner before the
# service starts, so the service turns healthy and can write; the one-shot service has only the
# capabilities that change an owner; and the healthcheck fails when /data is not writable.
#
# Usage: test-benchmark-volume.sh
# It builds the image t-benchvol-standin and starts the compose project t-benchvol with the
# networks t-benchvol-llm and t-benchvol-api, and removes them at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, docker with the compose plugin

set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly COMPOSE="$REPO/deploy/benchmark/docker-compose.yml"
readonly PROJECT=t-benchvol IMAGE=t-benchvol-standin
# The base of the benchmark image (deploy/benchmark/Dockerfile).
readonly PYTHON="python:3.14-slim@sha256:a2b82f3c48559aa0a8446d9af49826b6e2b2016f4cd2afabfe6013ec53729170"
pass=0
fail=0
check() {
  if [[ "$2" == "$3" ]]; then
    echo "PASS  $1 ($3)"
    pass=$((pass + 1))
  else
    echo "FAIL  $1 (got $3, expected $2)"
    fail=$((fail + 1))
  fi
}
compose() {
  NFHOTEL_LLM_NETWORK=t-benchvol-llm NFHOTEL_API_NETWORK=t-benchvol-api BENCHMARK_IMAGE="$IMAGE" \
    BENCHMARK_ENV_FILE=benchmark.env.example API_TOKEN=test-token \
    docker compose -p "$PROJECT" -f "$COMPOSE" "$@"
}
cleanup() {
  compose down -v >/dev/null 2>&1
  docker network rm t-benchvol-llm t-benchvol-api >/dev/null 2>&1
  docker image rm -f "$IMAGE" >/dev/null 2>&1
}
# to_uid_1000: give the volume to uid 1000, with a file in it, as the server's volume was. Docker
# copies the image's /data into an empty volume at every mount, which would reset the owner.
to_uid_1000() {
  docker run --rm --user 0 --entrypoint sh -v "${PROJECT}_results:/data" "$IMAGE" \
    -c 'touch /data/.gitkeep && chown -R 1000:1000 /data'
}
trap cleanup EXIT
cleanup

# The stand-in answers /health on port 8000 as the user app, uid 10001, as the real image does.
docker build -q -t "$IMAGE" - >/dev/null <<EOF || exit 1
FROM $PYTHON
RUN useradd --create-home --uid 10001 app && mkdir -p /srv /data && echo ok >/srv/health && chown app /data
USER app
CMD ["python", "-m", "http.server", "8000", "--directory", "/srv"]
EOF
docker network create t-benchvol-llm >/dev/null
docker network create t-benchvol-api >/dev/null

echo "== a volume made by another uid"
# Compose makes the volume, as on the server; then it is given to uid 1000, as the image of
# 2026-10-20 had left it, and the stack is started again.
compose up -d --wait --wait-timeout 120 >/dev/null 2>&1
compose stop >/dev/null 2>&1
to_uid_1000
check "the volume is owned by uid 1000" 1000 \
  "$(docker run --rm --entrypoint stat -v "${PROJECT}_results:/data" "$IMAGE" -c %u /data)"

compose up -d --wait --wait-timeout 120 --force-recreate >/dev/null 2>&1
check "the service turns healthy" healthy "$(docker inspect -f '{{.State.Health.Status}}' "${PROJECT}-benchmark-1" 2>/dev/null)"
check "the volume belongs to app" 10001 "$(docker exec "${PROJECT}-benchmark-1" stat -c %u /data)"
check "the service can write its results" ok \
  "$(docker exec "${PROJECT}-benchmark-1" sh -c 'echo x >>/data/results.jsonl && echo ok')"

echo "== the one-shot service"
owner="${PROJECT}-results-owner-1"
check "it ended successfully" "exited 0" "$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$owner")"
check "it keeps only the capabilities that change an owner" "[ALL] [CHOWN DAC_READ_SEARCH]" \
  "$(docker inspect -f '{{.HostConfig.CapDrop}} {{.HostConfig.CapAdd}}' "$owner" | sed 's/CAP_//g')"
check "it has no network and a read-only root" "none true" \
  "$(docker inspect -f '{{.HostConfig.NetworkMode}} {{.HostConfig.ReadonlyRootfs}}' "$owner")"

echo "== a volume the service cannot write"
to_uid_1000
# The healthcheck of the compose file, run once by hand instead of waiting for its retries.
probe="$(docker inspect -f '{{index .Config.Healthcheck.Test 3}}' "${PROJECT}-benchmark-1")"
docker exec "${PROJECT}-benchmark-1" python -c "$probe" >/dev/null 2>&1
check "the healthcheck fails" 1 "$?"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
