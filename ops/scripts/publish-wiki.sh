#!/usr/bin/env bash
# Publish docs/wiki to the GitHub wiki of the repository (ADR-0022). The workflow
# publish-wiki.yaml runs it after every push to main that changes docs/wiki.
#
#   1. clone the wiki repository into a temporary folder
#   2. make its Markdown pages equal to docs/wiki: copy every page, and remove the pages
#      that docs/wiki no longer has
#   3. commit, naming the source commit, and push; with no change, push nothing
#
# The token reaches git as an HTTP header through the environment (GIT_CONFIG_*), never in
# the URL or on a command line, so it is not in the clone's configuration or a process list.
#
# Usage: publish-wiki.sh
# Env:   REPO      owner/name of the repository, for example NF-Hotel/NFHotel; required
#        SHA       the commit of docs/wiki that is published; required
#        GH_TOKEN  a token that may push to the wiki (the job's GITHUB_TOKEN); empty sends none
#        WIKI_URL  the wiki remote (default https://github.com/$REPO.wiki.git; for the tests)
# Exit:  0 published (or nothing to change); 1 a step failed
# Needs: bash 4+, git

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
src="$ROOT/docs/wiki"

die() {
  echo "error: $*" >&2
  exit 1
}

[[ "${REPO:-}" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || die "REPO must be owner/name"
[[ "${SHA:-}" =~ ^[0-9a-f]{40}$ ]] || die "SHA must be 40 lower-case hex characters"
[[ -d "$src" ]] || die "$src is missing"
url="${WIKI_URL:-https://github.com/$REPO.wiki.git}"

if [[ -n "${GH_TOKEN:-}" ]]; then
  export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0="http.https://github.com/.extraheader"
  GIT_CONFIG_VALUE_0="AUTHORIZATION: basic $(printf 'x-access-token:%s' "$GH_TOKEN" | base64 -w0)"
  export GIT_CONFIG_VALUE_0
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
git clone --quiet --depth 1 "$url" "$work/wiki" || die "cannot clone $url"

shopt -s nullglob
for page in "$work"/wiki/*.md; do
  [[ -f "$src/${page##*/}" ]] || git -C "$work/wiki" rm --quiet "${page##*/}"
done
cp "$src"/*.md "$work/wiki/"
git -C "$work/wiki" add --all -- '*.md'

if git -C "$work/wiki" diff --cached --quiet; then
  echo "unchanged: the wiki already matches docs/wiki of ${SHA:0:7}"
  exit 0
fi
git -C "$work/wiki" -c user.name="github-actions[bot]" \
  -c user.email="41898282+github-actions[bot]@users.noreply.github.com" \
  commit --quiet -m "Publish docs/wiki of $REPO@${SHA:0:7}" -m "Source: https://github.com/$REPO/tree/$SHA/docs/wiki (ADR-0022)"
git -C "$work/wiki" push --quiet origin HEAD || die "cannot push to $url"
echo "published: $(git -C "$work/wiki" show --stat --format= HEAD | tail -n 1)"
