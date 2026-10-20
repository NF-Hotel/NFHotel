#!/usr/bin/env bash
# Resolve the NF-Hotel/DevOps commit that a composite action of this repository runs at
# (ADR-0012 of NFHotel, decision 2), and refuse an action that is not pinned by a full
# commit SHA. Used by .github/actions/release-images and .github/actions/deploy-release.
#
# A caller in another repository pins `NF-Hotel/DevOps/.github/actions/<name>@<sha>`, so
# github.action_ref is that SHA. In this repository's own CI the action is local
# (`uses: ./.github/actions/<name>`), github.action_ref is empty, and the commit is the
# one under test, given in DEVOPS_SHA_INPUT.
#
# Env:   ACTION_REF        github.action_ref (set through env, as GitHub requires)
#        DEVOPS_SHA_INPUT  the commit, only for a local action in NF-Hotel/DevOps
#        GITHUB_REPOSITORY, GITHUB_OUTPUT   set by the runner
# Usage: action-ref.sh
# Exit:  0 the commit is written as devops_sha to GITHUB_OUTPUT; 1 the action is not
#        pinned by a full SHA
# Needs: bash 4+

set -euo pipefail

readonly DEVOPS_REPOSITORY=NF-Hotel/DevOps
readonly FULL_SHA='^[0-9a-f]{40}$'

fail() {
  echo "::error::$*"
  exit 1
}

ref="${ACTION_REF:-}"
given="${DEVOPS_SHA_INPUT:-}"

if [[ "$ref" =~ $FULL_SHA ]]; then
  [[ -z "$given" || "$given" == "$ref" ]] || fail "devops-sha ($given) differs from the pinned ref ($ref)"
  sha="$ref"
elif [[ -z "$ref" && "${GITHUB_REPOSITORY:-}" == "$DEVOPS_REPOSITORY" ]]; then
  [[ "$given" =~ $FULL_SHA ]] || fail "a local action in $DEVOPS_REPOSITORY needs devops-sha (github.sha)"
  sha="$given"
else
  fail "pin the NF-Hotel/DevOps actions by a full commit SHA, not '${ref:-a local path}' (ADR-0012)"
fi
echo "devops_sha=$sha" >>"$GITHUB_OUTPUT"
echo "NF-Hotel/DevOps commit: $sha"
