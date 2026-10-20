#!/usr/bin/env bash
# Run every linter of this repository in containers at pinned versions (QC-SH-001,
# QC-YML-001, QC-WF-001): actionlint (with shellcheck) on the workflows and actions,
# the shell scripts through shellcheck and shfmt, yamllint on the YAML files. The same command
# runs in CI and on a developer machine.
#
# Usage: lint.sh
# Env:   REPO_HOST  the repository path as the Docker daemon sees it (default: this
#                   checkout; on Windows give C:/... for Docker Desktop)
# Exit:  0 every linter is clean; 1 a linter found something
# Needs: bash 4+, docker

set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_HOST="${REPO_HOST:-$REPO}"
readonly ACTIONLINT=rhysd/actionlint:1.7.7 SHELLCHECK=koalaman/shellcheck:v0.11.0
readonly SHFMT=mvdan/shfmt:v3.14.1 PYTHON=python:3.13-slim YAMLLINT_VERSION=1.35.1
failures=0

run() {
  echo "==> $1"
  shift
  "$@" || failures=$((failures + 1))
}

cd "$REPO" || exit 1
mapfile -t shell_files < <(git ls-files '*.sh')
mapfile -t yaml_files < <(git ls-files '*.yml' '*.yaml' | grep -v '^framework/')

run "actionlint" docker run --rm -v "$REPO_HOST:/repo:ro" -w /repo "$ACTIONLINT" -no-color
run "shellcheck" docker run --rm -v "$REPO_HOST:/mnt:ro" -w /mnt "$SHELLCHECK" -x "${shell_files[@]}"
run "shfmt" docker run --rm -v "$REPO_HOST:/mnt:ro" -w /mnt "$SHFMT" -i 2 -ci -d "${shell_files[@]}"
run "yamllint" docker run --rm -v "$REPO_HOST:/data:ro" -w /data "$PYTHON" sh -c \
  "pip install -q yamllint==$YAMLLINT_VERSION 2>/dev/null && yamllint -s -c .yamllint \"\$@\"" _ "${yaml_files[@]}"

((failures == 0)) || {
  echo "error: $failures linter(s) found problems" >&2
  exit 1
}
echo "==> all linters are clean"
