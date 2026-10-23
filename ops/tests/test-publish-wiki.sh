#!/usr/bin/env bash
# Tests of scripts/publish-wiki.sh (ADR-0022) against a local bare repository that stands in
# for the GitHub wiki: new and changed pages are published, a page that docs/wiki no longer
# has is removed, a second run pushes nothing, a file that is not a page is kept, and a bad
# commit or repository name is refused. No network and no token are used.
#
# Run it in Debian 12 with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:12-slim bash /src/tests/test-publish-wiki.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12 (git is installed by the test)

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

command -v git >/dev/null || { apt-get update -qq && apt-get install -y -qq git >/dev/null; } || exit 1
export GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@t GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@t

# A copy of the script in a fake repository, so docs/wiki can change freely.
mkdir -p "$W/repo/ops/scripts" "$W/repo/docs/wiki"
cp "$OPS/scripts/publish-wiki.sh" "$W/repo/ops/scripts/"
# The stand-in wiki: an old page, a page that is gone from docs/wiki, and an image.
git init -q --bare "$W/wiki.git"
git clone -q "$W/wiki.git" "$W/seed" 2>/dev/null
echo "old home" >"$W/seed/Home.md"
echo "gone" >"$W/seed/Gone.md"
echo "png" >"$W/seed/logo.png"
git -C "$W/seed" add -A && git -C "$W/seed" commit -qm seed && git -C "$W/seed" push -q origin HEAD:master
git -C "$W/wiki.git" symbolic-ref HEAD refs/heads/master

echo "new home" >"$W/repo/docs/wiki/Home.md"
echo "monitoring" >"$W/repo/docs/wiki/Monitoring.md"
SHA="$(printf 'a%.0s' {1..40})"
run() { REPO=NF-Hotel/NFHotel SHA="${1:-$SHA}" WIKI_URL="$W/wiki.git" bash "$W/repo/ops/scripts/publish-wiki.sh" >"$W/out" 2>&1; }
wiki() {
  rm -rf "$W/check"
  git clone -q "$W/wiki.git" "$W/check" 2>/dev/null
}

run && ok "the first run publishes" || ko "publish: $(cat "$W/out")"
wiki
[[ "$(cat "$W/check/Home.md")" == "new home" && "$(cat "$W/check/Monitoring.md")" == monitoring ]] &&
  ok "new and changed pages reach the wiki" || ko "pages: $(ls "$W/check")"
[[ ! -e "$W/check/Gone.md" ]] && ok "a page that docs/wiki no longer has is removed" || ko "Gone.md is still there"
[[ -f "$W/check/logo.png" ]] && ok "a file that is not a page is kept" || ko "logo.png was removed"
git -C "$W/check" log -1 --format=%B | grep -q "tree/$SHA/docs/wiki" && ok "the commit names the source commit" ||
  ko "message: $(git -C "$W/check" log -1 --format=%B)"

before="$(git -C "$W/wiki.git" rev-parse HEAD)"
run && grep -q "^unchanged" "$W/out" && [[ "$(git -C "$W/wiki.git" rev-parse HEAD)" == "$before" ]] &&
  ok "a second run pushes nothing" || ko "second run: $(cat "$W/out")"

run "not-a-sha" && ko "accepted a bad commit" || ok "a commit that is not 40 hex characters is refused"
REPO="bad name" SHA="$SHA" WIKI_URL="$W/wiki.git" bash "$W/repo/ops/scripts/publish-wiki.sh" >/dev/null 2>&1 &&
  ko "accepted a bad repository name" || ok "a repository name that is not owner/name is refused"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
