#!/usr/bin/env bash
# Tests of the scan exporter of the monitoring (MIL-053 task 5): ruff (lint and format),
# mypy --strict and the pytest unit tests with coverage of at least 80 %, with a fake client in place
# of GitHub and SonarQube Cloud, so no test reaches the network. The tools are pinned and installed in a throwaway
# container of the image the exporter runs in; the source is mounted read-only.
#
# Usage: bash ops/tests/test-scan-exporter.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: bash, docker

set -euo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
src="$OPS/deploy/monitoring/scan-exporter"
image="$(sed -n 's|^    image: \(python:.*\)$|\1|p' "$OPS/deploy/monitoring/docker-compose.yml" | head -n 1)"
[[ -n "$image" ]] || {
  echo "error: no python image in the monitoring compose file" >&2
  exit 1
}

docker run --rm -v "$src:/src:ro" -e PYTHONDONTWRITEBYTECODE=1 "$image" sh -ec '
  pip install --quiet --disable-pip-version-check --root-user-action=ignore \
    pytest==8.4.2 pytest-cov==7.0.0 ruff==0.14.0 mypy==1.18.2
  cp -r /src /work && cd /work
  echo "==> ruff check" && ruff check --no-cache .
  echo "==> ruff format --check" && ruff format --check --no-cache .
  echo "==> mypy --strict" && mypy --no-incremental --cache-dir=/dev/null scan_exporter tests
  echo "==> pytest" && python -m pytest -q -p no:cacheprovider \
    --cov=scan_exporter --cov-report=term-missing --cov-fail-under=80
'
