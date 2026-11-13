#!/usr/bin/env bash
# Test of Ollama as a user that is not root (MIL-082 task 3): deploy/ollama/docker-compose.yml with
# its pinned image, on a volume of models that root wrote, as the volume of nf-hotel was until
# MIL-082. First the image runs as the compose file before MIL-082 ran it (root without
# capabilities): Ollama makes its key on the volume, and a model is added as root. Then the compose
# project starts as the compose file says: the one-shot service models-owner gives the volume to
# 10001 and exits, and Ollama runs as 10001.
#
# It checks the user of Ollama (in its configuration, in the kernel's view and with the runtime
# check of MIL-080), its health check, the model that root wrote in its list, a write to the volume,
# the one-shot service (exit 0, only the capabilities that change an owner, no network, a read-only
# root), and that Ollama is healthy again after a restart and after a second start of the project.
# Then, without the one-shot service, Ollama as 10001 cannot write a volume that root owns: the
# reason the service exists. Last, a rollback of deploy-release.sh to the release before MIL-082
# runs the image as root without capabilities on the volume that 10001 now owns: it must still
# start and list the models.
#
# The model is a manifest and a configuration of a few bytes, stored as Ollama stores a model, so
# nothing is downloaded: the list of the models reads only the manifests and their configurations.
#
# Usage: sudo bash ops/tests/test-ollama-user.sh
# It starts the compose project t-ollama with the network t-ollama-llm and the containers
# t-ollama-root and t-ollama-old, and removes them and their volume at the end.
# Exit:  0 every check passed; 1 a check failed
# Needs: bash 4+, root (the runtime check reads the main process), docker with the compose plugin

set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly COMPOSE="$OPS/deploy/ollama/docker-compose.yml" PROJECT=t-ollama NETWORK=t-ollama-llm
readonly OLLAMA="t-ollama-ollama-1" OWNER="t-ollama-models-owner-1" VOLUME="t-ollama_models"
# The image of the compose file, so the test runs what nf-hotel runs.
IMAGE="$(sed -n 's/^    image: \(ollama\/ollama:.*\)$/\1/p' "$COMPOSE" | head -n 1)"
readonly IMAGE
W="$(mktemp -d)"
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
  NFHOTEL_LLM_NETWORK="$NETWORK" OLLAMA_ENV_FILE="$OPS/deploy/ollama/ollama.env.example" \
    docker compose -p "$PROJECT" -f "$COMPOSE" "$@"
}
remove_all() {
  compose down -v >/dev/null 2>&1
  docker rm -f t-ollama-root t-ollama-old >/dev/null 2>&1
  docker network rm "$NETWORK" >/dev/null 2>&1
}
cleanup() {
  remove_all
  rm -rf "$W"
}
# in_volume <command>...: run a command of the image as root on the volume, with no network.
in_volume() {
  docker run --rm --user 0 --network none -v "$VOLUME:/v" --entrypoint "$1" "$IMAGE" "${@:2}"
}
# as_before <name>: start the image as the compose file before MIL-082 ran it: as root without
# capabilities, no-new-privileges and a read-only root, with the volume in the home of root.
as_before() {
  docker run -d --name "$1" --network none --cap-drop ALL --security-opt no-new-privileges:true \
    --read-only --tmpfs /tmp -v "$VOLUME:/root/.ollama" "$IMAGE" >/dev/null
}
# health_of <container>: its health, after waiting up to two minutes for it to leave "starting".
health_of() {
  local state=""
  for _ in $(seq 1 60); do
    state="$(docker inspect -f '{{.State.Health.Status}}' "$1" 2>/dev/null)"
    [[ "$state" == starting ]] || break
    sleep 2
  done
  echo "${state:-none}"
}
# answers <container>: wait up to a minute for its Ollama to answer the list of the models.
answers() {
  for _ in $(seq 1 30); do
    docker exec "$1" ollama list >/dev/null 2>&1 && return 0
    sleep 2
  done
  return 1
}
# listed <model> [container]: yes when Ollama lists the model; the container is Ollama of the
# project unless named.
listed() {
  docker exec "${2:-$OLLAMA}" ollama list 2>/dev/null |
    awk -v m="$1" '$1 == m { found = 1 } END { print found ? "yes" : "no" }'
}

[[ "$(id -u)" == 0 ]] || {
  echo "error: run it as root" >&2
  exit 1
}
[[ -n "$IMAGE" ]] || {
  echo "error: no image of ollama in $COMPOSE" >&2
  exit 1
}
trap cleanup EXIT
remove_all
docker network create "$NETWORK" >/dev/null || exit 1

echo "== the compose file"
check "the one-shot service and Ollama name the same image" 1 "$(sed -n 's/^    image: //p' "$COMPOSE" | sort -u | wc -l)"

echo "== the volume as root wrote it"
# Compose makes the volume, as on nf-hotel; the image then runs on it as it did there.
compose create >"$W/create" 2>&1 || {
  echo "error: compose create failed: $(tail -3 "$W/create")" >&2
  exit 1
}
if ! as_before t-ollama-root || ! answers t-ollama-root; then
  echo "error: the image as root did not answer" >&2
  exit 1
fi
# The model as Ollama stores one: a configuration blob named by its digest and a manifest that
# names it, both written by root.
docker exec t-ollama-root sh -c '
  set -e
  m=/root/.ollama/models
  config="{\"model_format\":\"gguf\",\"model_family\":\"test\",\"model_type\":\"0B\"}"
  digest="$(printf "%s" "$config" | sha256sum | cut -d" " -f1)"
  mkdir -p "$m/blobs" "$m/manifests/registry.ollama.ai/library/t-model"
  printf "%s" "$config" >"$m/blobs/sha256-$digest"
  printf "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\",\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"digest\":\"sha256:%s\",\"size\":%s},\"layers\":[]}" \
    "$digest" "${#config}" >"$m/manifests/registry.ollama.ai/library/t-model/latest"
'
check "Ollama as root lists the model it stores" yes "$(listed t-model:latest t-ollama-root)"
docker rm -f t-ollama-root >/dev/null
check "root owns the volume and the key of Ollama, which only root reads" "0 0 600" \
  "$(in_volume stat -c '%u %a' /v /v/id_ed25519 2>/dev/null | awk 'NR == 1 { u = $1 } NR == 2 { print u, $1, $2 }')"

echo "== Ollama as 10001"
compose up -d --wait --wait-timeout 180 >"$W/up" 2>&1
check "the project starts and Ollama turns healthy" healthy "$(health_of "$OLLAMA")"
check "Ollama is configured as 10001" "10001:10001" "$(docker inspect -f '{{.Config.User}}' "$OLLAMA" 2>/dev/null)"
pid="$(docker inspect -f '{{.State.Pid}}' "$OLLAMA" 2>/dev/null)"
check "its main process runs as 10001 in the kernel's view (real and effective)" "10001 10001" \
  "$(awk '/^Uid:/ { print $2, $3 }' "/proc/${pid:-0}/status" 2>/dev/null)"
check "the model that root wrote is listed" yes "$(listed t-model:latest)"
check "the volume and the key belong to 10001" "10001 10001" \
  "$(in_volume stat -c %u /v /v/id_ed25519 2>/dev/null | paste -sd ' ')"
check "Ollama writes to the volume (a copy of the model)" yes \
  "$(docker exec "$OLLAMA" ollama cp t-model t-copy >/dev/null 2>&1 && listed t-copy:latest)"
bash "$OPS/scripts/check-container-runtime.sh" --filter "label=com.docker.compose.project=$PROJECT" >"$W/check" 2>&1
check "the runtime check passes Ollama with its profile" 0 "$?"
grep -E '^(PASS|FAIL|WARN)|^      ' "$W/check" | sed 's/^/      /'

echo "== the one-shot service"
check "it ended successfully" "exited 0" "$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$OWNER" 2>/dev/null)"
check "it keeps only the capabilities that change an owner" "[ALL] [CHOWN DAC_READ_SEARCH]" \
  "$(docker inspect -f '{{.HostConfig.CapDrop}} {{.HostConfig.CapAdd}}' "$OWNER" 2>/dev/null | sed 's/CAP_//g')"
check "it has no network and a read-only root" "none true" \
  "$(docker inspect -f '{{.HostConfig.NetworkMode}} {{.HostConfig.ReadonlyRootfs}}' "$OWNER" 2>/dev/null)"

echo "== a restart and a second start"
docker restart "$OLLAMA" >/dev/null
check "Ollama is healthy again after a restart" healthy "$(health_of "$OLLAMA")"
check "the model is still listed" yes "$(listed t-model:latest)"
compose up -d --wait --wait-timeout 180 --force-recreate >"$W/up2" 2>&1
check "a second start of the project, on a volume that 10001 owns, is healthy" healthy "$(health_of "$OLLAMA")"
check "the one-shot service ended successfully again" "exited 0" \
  "$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$OWNER" 2>/dev/null)"

echo "== without the one-shot service"
# The volume back to root, and Ollama started alone: as 10001 it starts and lists the models, which
# every user may read, but it cannot write there.
compose stop >/dev/null 2>&1
in_volume chown -R 0:0 /v
compose up -d --no-deps ollama >"$W/up3" 2>&1
check "Ollama as 10001 starts on a volume that root owns" healthy "$(health_of "$OLLAMA")"
check "it cannot write there (a copy of the model)" no \
  "$(docker exec "$OLLAMA" ollama cp t-model t-copy2 >/dev/null 2>&1 && listed t-copy2:latest || echo no)"

echo "== a rollback to the release before MIL-082"
# deploy-release.sh deploys the release before when a deploy fails. Its compose file runs the image
# as root without capabilities, on the volume that 10001 owns after MIL-082: root without
# DAC_OVERRIDE reads what every user may read, so Ollama starts and lists the models; a pull waits
# for the next deploy of MIL-082, whose one-shot service gives the volume to 10001 again.
compose stop >/dev/null 2>&1
in_volume chown -R 10001:10001 /v
as_before t-ollama-old || exit 1
answers t-ollama-old
check "the release before MIL-082 lists the models on a volume of 10001" yes "$(listed t-model:latest t-ollama-old)"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
