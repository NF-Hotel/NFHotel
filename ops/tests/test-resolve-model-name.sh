#!/usr/bin/env bash
# Tests of scripts/resolve-model-name.sh. No Docker and no model server: the installed names are fed in as text.
#
# Usage: tests/test-resolve-model-name.sh        (exit 0 when every test passes)
#
# No `set -e` on purpose: every case is run to the end so that all failures are counted.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="${root}/scripts/resolve-model-name.sh"

failures=0
checks=0

expect() {
  local name="$1" want="$2" got="$3"
  checks=$((checks + 1))
  if [[ "${got}" != "${want}" ]]; then
    echo "FAIL ${name}: wanted '${want}', got '${got}'"
    failures=$((failures + 1))
  fi
}

# Run the script with the installed names as input; sets `output` and `status`.
resolve() {
  local installed="$1" model="$2"
  output="$(printf '%s' "${installed}" | bash "${script}" "${model}" 2>/dev/null)"
  status=$?
}

listing=$'NAME            ID            SIZE    MODIFIED\nphi3.5:latest   61819fb370a3  2.2 GB  2 hours ago\nllama3.2:3b     a80c4f17acd5  2.0 GB  2 hours ago'

resolve "${listing}" "phi3.5"
expect "a name without a tag gets :latest, exit 0" "0" "${status}"
expect "a name without a tag gets :latest, the name" "phi3.5:latest" "${output}"

resolve "${listing}" "phi3.5:latest"
expect "an exact name stays, exit 0" "0" "${status}"
expect "an exact name stays, the name" "phi3.5:latest" "${output}"

resolve "${listing}" "llama3.2:3b"
expect "a name with another tag stays, the name" "llama3.2:3b" "${output}"

resolve "${listing}" "llama3.2"
expect "a name whose :latest is not listed is not installed, exit 3" "3" "${status}"
expect "a name that is not installed prints nothing" "" "${output}"

resolve "${listing}" "mistral:7b"
expect "a tagged name that is not listed is not installed, exit 3" "3" "${status}"

resolve "${listing}" "NAME"
expect "the header word is not a model, exit 3" "3" "${status}"

resolve $'phi3.5:latest\r\n' "phi3.5"
expect "a line ending with a carriage return is read, the name" "phi3.5:latest" "${output}"

resolve "" "phi3.5"
expect "nothing installed, exit 3" "3" "${status}"

resolve "${listing}" "phi3"
expect "a name that only starts like a listed one is not matched, exit 3" "3" "${status}"

output="$(printf '' | bash "${script}" 2>&1)"
expect "no argument, exit 2" "2" "$?"

output="$(printf '' | bash "${script}" "   " 2>&1)"
expect "a blank argument, exit 2" "2" "$?"

if ((failures > 0)); then
  echo "${failures} of ${checks} checks failed"
  exit 1
fi
echo "all ${checks} checks passed"
