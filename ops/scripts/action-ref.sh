#!/usr/bin/env bash
# Resolve the commit of the operations code that a composite action of this repository
# runs at, written as devops_sha (ADR-0016; the name is kept from ADR-0012, so the release
# manifest and the release log on the VM keep their format). Used by
# .github/actions/release-images and .github/actions/deploy-release.
#
# The actions are local (`uses: ./.github/actions/<name>`), so github.action_ref is empty
# and the commit is the checkout the action is read from. A caller may give it in
# DEVOPS_SHA_INPUT; it must then be that same commit. A ref that is not empty must be a
# full commit SHA: an action of this repository is never run from a branch or a tag.
#
# Env:   ACTION_REF        github.action_ref (set through env, as GitHub requires)
#        DEVOPS_SHA_INPUT  optional: the commit, which must match the checkout
#        GITHUB_OUTPUT     set by the runner
# Usage: action-ref.sh
# Exit:  0 the commit is written as devops_sha to GITHUB_OUTPUT; 1 it cannot be resolved,
#        or a ref is not a full SHA
# Needs: bash 4+, git

set -euo pipefail

readonly FULL_SHA='^[0-9a-f]{40}$'

fail() {
  echo "::error::$*"
  exit 1
}

ref="${ACTION_REF:-}"
given="${DEVOPS_SHA_INPUT:-}"

if [[ "$ref" =~ $FULL_SHA ]]; then
  sha="$ref"
elif [[ -z "$ref" ]]; then
  # A local action: the commit of the checkout that holds this script.
  sha="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse HEAD 2>/dev/null)" ||
    fail "the action is not read from a git checkout; check out the repository first"
else
  fail "run the actions of this repository at a full commit SHA, not '$ref' (ADR-0016)"
fi
[[ "$sha" =~ $FULL_SHA ]] || fail "'$sha' is not a full commit SHA"
[[ -z "$given" || "$given" == "$sha" ]] || fail "devops-sha ($given) differs from the checkout of the action ($sha)"
echo "devops_sha=$sha" >>"$GITHUB_OUTPUT"
echo "operations commit: $sha"
