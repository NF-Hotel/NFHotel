#!/usr/bin/env bash
# Find the exact name that the model server knows for a model name that a person typed or chose (ADR-0014).
#
# Ollama lists a model with its tag, for example `phi3.5:latest`, and the market analysis service uses a model only
# when the name in its settings is exactly one of the listed names. A default of `phi3.5` therefore selected no model
# and the AI insights were missing. This script reads the listed names on standard input, one per line (the first word
# of each line, so the output of `ollama list` can be piped in with its header line), and prints the exact name:
#   - the name itself when it is listed;
#   - `<name>:latest` when the name has no tag and that name is listed;
#   - nothing, and exit 3, when neither is listed.
#
# Usage: ollama list | resolve-model-name.sh <model>
# Exit:  0 the exact name was printed; 2 usage error; 3 the model is not installed
# Needs: bash 4 or later, awk

set -euo pipefail

if [[ $# -ne 1 || -z "${1// /}" ]]; then
  echo "usage: resolve-model-name.sh <model>   (the installed names on standard input)" >&2
  exit 2
fi

model="${1}"
installed="$(awk 'NR > 0 && $1 != "NAME" && $1 != "" {print $1}' | tr -d '\r')"

if grep -Fxq -- "$model" <<<"$installed"; then
  printf '%s\n' "$model"
  exit 0
fi

if [[ "$model" != *:* ]] && grep -Fxq -- "${model}:latest" <<<"$installed"; then
  printf '%s\n' "${model}:latest"
  exit 0
fi

exit 3
