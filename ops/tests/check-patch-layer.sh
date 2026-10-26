#!/usr/bin/env bash
# Check the patch layer of the market-analysis image (ADR-0030, MIL-042 criteria 2 and 4)
# after the release-images action built it: the files of the application package are byte
# for byte those of the frozen base, the image still runs as the user app, and every
# package pinned in deploy/market-analysis/requirements.txt has its pinned version.
# The base is read from the FROM line of the Dockerfile; the build has pulled it.
#
# Usage: check-patch-layer.sh <NFHotel commit> [<registry>]
# Exit:  0 the layer is as expected; 1 a check failed; 2 usage error
# Needs: bash 4+, docker

set -uo pipefail

[[ $# -ge 1 && "$1" =~ ^[0-9a-f]{40}$ ]] || {
  echo "usage: check-patch-layer.sh <NFHotel commit> [<registry>]" >&2
  exit 2
}
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly DIR="$REPO/deploy/market-analysis"
readonly PACKAGES=/usr/local/lib/python3.13/site-packages
image="${2:-ghcr.io/nf-hotel}/market-analysis:sha-$1"
base="$(sed -n 's/^FROM \([^ ]*\).*/\1/p' "$DIR/Dockerfile")"
failures=0
check() {
  if [[ "$2" == "$3" ]]; then
    echo "PASS  $1"
  else
    echo "FAIL  $1 (expected '$2', got '$3')"
    failures=$((failures + 1))
  fi
}

# in_image <image> <shell command>: run a command in the image, without network.
in_image() { docker run --rm --network none --entrypoint sh "$1" -c "$2"; }

app_files='cd '"$PACKAGES"' && find hotel_booking_analysis -type f -print0 | sort -z | xargs -0 sha256sum | sha256sum'
check "the application files equal those of the frozen base" "$(in_image "$base" "$app_files")" \
  "$(in_image "$image" "$app_files")"
check "the image runs as the user app" app "$(docker image inspect --format '{{.Config.User}}' "$image")"

while read -r name version; do
  check "$name is at its pinned version" "$version" \
    "$(in_image "$image" "python -c 'import importlib.metadata as m; print(m.version(\"$name\"))'")"
done < <(sed -n 's/^\([A-Za-z0-9_.-]*\)==\([^ ]*\).*/\1 \2/p' "$DIR/requirements.txt")

((failures == 0)) || exit 1
echo "==> the patch layer of $image is as expected"
