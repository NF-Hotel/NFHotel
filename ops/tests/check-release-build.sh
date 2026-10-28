#!/usr/bin/env bash
# Check the images that the release-images action built in CI without pushing (MIL-025
# criterion 2): every built image exists with the tag sha-<NFHotel commit> and carries
# the labels that tie it to that commit and to NF-Hotel/NFHotel.
#
# Usage: check-release-build.sh <NFHotel commit> [<registry>]
# Exit:  0 every image is as expected; 1 an image is missing or mislabelled; 2 usage error
# Needs: bash 4+, docker

set -uo pipefail

[[ $# -ge 1 && "$1" =~ ^[0-9a-f]{40}$ ]] || {
  echo "usage: check-release-build.sh <NFHotel commit> [<registry>]" >&2
  exit 2
}
sha="$1"
registry="${2:-ghcr.io/nf-hotel}"
readonly NAMES=(nfhotel-web nfhotel-api market-analysis benchmark)
failures=0

for name in "${NAMES[@]}"; do
  image="$registry/$name:sha-$sha"
  labels="$(docker image inspect --format '{{index .Config.Labels "org.opencontainers.image.revision"}} {{index .Config.Labels "org.opencontainers.image.source"}}' "$image" 2>/dev/null)" || labels=""
  if [[ "$labels" == "$sha https://github.com/NF-Hotel/NFHotel" ]]; then
    echo "PASS  $image (revision and source labels)"
  else
    echo "FAIL  $image: labels '${labels:-image missing}'"
    failures=$((failures + 1))
  fi
done
((failures == 0)) || exit 1
echo "==> the release images of NFHotel $sha are as expected"
