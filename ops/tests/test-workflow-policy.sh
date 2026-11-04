#!/usr/bin/env bash
# Static test of the GitHub Actions workflows and composite actions of this repository
# (MIL-059 task 4, ADR-0037, QC-WF-001 criterion 24): the rules that keep a compromised
# account or action from turning a workflow against the repository, as in the GhostAction
# campaign of 2026. It reads the files with grep and awk only, so it runs on the runner and on
# a developer machine without a YAML library, and it is run by the job Ops linters of
# ops-ci.yaml after ops/tests/lint.sh.
#
# Rules (the log names the rule, the file and the line):
#   1  every `uses:` of an action outside this repository is pinned to a full 40-character
#      commit SHA with the version in a comment (`# v1.2.3`); a local action (`./...`) is exempt
#   2  every workflow declares `permissions:` at the top with `contents: read` and nothing else,
#      so a job has write access only where it declares it
#   3  every actions/checkout step sets `persist-credentials: false`
#   4  no workflow is triggered by `pull_request_target`
#   5  a secret is read only by a step (`env:` or `with:` of a step); at the job level only a
#      yes/no derived from a secret (`secrets.NAME != ''`) is allowed
#   6  no `${{ }}` expression inside a `run:` script: event data reaches a script through `env:`
#   7  no download piped to a shell (`curl ... | sh`) and no `base64 --decode` piped to a shell
#
# Usage: test-workflow-policy.sh [<.github directory>]   (default: the one of this repository)
#        test-workflow-policy.sh --self-test              fixtures that break each rule must fail
# Exit:  0 every rule holds (or every fixture failed as it should); 1 a rule is broken
# Needs: bash 4+, grep, awk, sed, mktemp

# No -e: every rule runs on every file and the broken ones are counted, as the other tests do.
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly SHA_PIN='^[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40}[[:space:]]+#[[:space:]]*v?[0-9]'
failures=0

broken() {
  echo "FAIL  rule $1  $2:$3  $4"
  failures=$((failures + 1))
}

# files <dir>: the workflow files and the composite actions, one path per line.
files() {
  find "$1/workflows" -maxdepth 1 -type f \( -name '*.yml' -o -name '*.yaml' \) 2>/dev/null | sort
  find "$1/actions" -mindepth 2 -maxdepth 2 -type f \( -name 'action.yml' -o -name 'action.yaml' \) 2>/dev/null | sort
}

# code <file>: the file without comment lines and without the comment at the end of a line
# that follows a space (a `#` inside quotes stays).
code() {
  sed -E 's/^[[:space:]]*#.*$//; s/[[:space:]]+#[^"'"'"']*$//' "$1"
}

# Rule 1: pins. The comment with the version is read from the raw line, so the file is read
# twice: once for the value, once for the comment.
check_pins() {
  local file="$1" line n value
  while IFS= read -r line; do
    n="${line%%:*}"
    value="$(sed -E 's/^[0-9]+:[[:space:]]*(- )?uses:[[:space:]]*//; s/^"//; s/"[[:space:]]*(#.*)?$//' <<<"$line")"
    case "$value" in
      ./* | '') continue ;;
    esac
    [[ "$value" =~ $SHA_PIN ]] || broken 1 "$file" "$n" "the action is not pinned to a full commit SHA with its version in a comment: $value"
  done < <(grep -n -E '^[[:space:]]*(- )?uses:' "$file")
}

# Rule 2: a workflow's top-level permissions are `contents: read` only.
check_permissions() {
  local file="$1" block
  block="$(code "$file" | awk '
    /^permissions:/ { on = 1; print; next }
    on && /^[^[:space:]]/ { on = 0 }
    on && NF { print }
  ')"
  if [[ -z "$block" ]]; then
    broken 2 "$file" 0 "no top-level permissions: block"
    return
  fi
  if [[ "$(sed '1d; s/^[[:space:]]*//' <<<"$block")" != "contents: read" ]]; then
    broken 2 "$file" "$(grep -n '^permissions:' "$file" | head -n 1 | cut -d: -f1)" "the top-level permissions are not contents: read only"
  fi
}

# Rule 3: every checkout keeps no credential. The step of a `uses: actions/checkout` line ends
# at the first non-blank line indented less than the `uses:` key.
check_checkouts() {
  local file="$1"
  code "$file" | awk -v file="$file" '
    function indent(s) { match(s, /^[ ]*/); return RLENGTH }
    {
      if (in_step) {
        if (NF && indent($0) < step_indent) { finish() }
        else if ($0 ~ /^[ ]*persist-credentials:[ ]*false[ ]*$/) { found = 1 }
      }
      if ($0 ~ /^[ ]*(- )?uses:[ ]*"?actions\/checkout@/) {
        in_step = 1; found = 0; step_line = NR; step_indent = indent($0)
        if ($0 ~ /^[ ]*- /) { step_indent += 2 }
      }
    }
    END { if (in_step) finish() }
    function finish() {
      if (!found) { printf "FAIL  rule 3  %s:%d  the checkout does not set persist-credentials: false\n", file, step_line; bad++ }
      in_step = 0
    }
    END { exit bad ? 1 : 0 }
  ' || failures=$((failures + 1))
}

# Rule 4: no pull_request_target.
check_triggers() {
  local file="$1" hit
  hit="$(code "$file" | grep -n -E '(^|[[:space:]])pull_request_target([[:space:]]|:|$)' | head -n 1)" || return 0
  broken 4 "$file" "${hit%%:*}" "the workflow is triggered by pull_request_target"
}

# Rule 5: secrets only in a step. A step's env: or with: entry is indented at least 10 spaces
# in this repository (jobs 2, steps 6, keys 8, entries 10); a job-level entry is at 6, a
# workflow-level one at 2. The yes/no form `secrets.NAME != ''` is allowed anywhere.
check_secrets() {
  local file="$1" line n
  while IFS= read -r line; do
    n="${line%%:*}"
    line="${line#*:}"
    [[ "$line" =~ secrets\.[A-Za-z0-9_]+[[:space:]]*(!=|==)[[:space:]]*\'\' ]] && continue
    [[ "$line" =~ ^[[:space:]]{10} ]] || broken 5 "$file" "$n" "a secret is read above a step; give it to the step that needs it through env:"
  done < <(code "$file" | grep -n -E 'secrets\.')
}

# Rule 6: no expression inside a run: script, inline or as a block scalar.
check_run_blocks() {
  local file="$1"
  code "$file" | awk -v file="$file" '
    function indent(s) { match(s, /^[ ]*/); return RLENGTH }
    {
      if (in_block) {
        if (NF && indent($0) <= run_indent) { in_block = 0 }
        else if ($0 ~ /\$\{\{/) { printf "FAIL  rule 6  %s:%d  an expression inside a run: script; pass the value through env:\n", file, NR; bad++ }
      }
      if (!in_block && match($0, /^[ ]*(- )?run:[ ]*/)) {
        rest = substr($0, RSTART + RLENGTH)
        run_indent = indent($0)
        if ($0 ~ /^[ ]*- /) { run_indent += 2 }
        if (rest ~ /^[|>][-+]?[ ]*$/) { in_block = 1 }
        else if (rest ~ /\$\{\{/) { printf "FAIL  rule 6  %s:%d  an expression inside a run: script; pass the value through env:\n", file, NR; bad++ }
      }
    }
    END { exit bad ? 1 : 0 }
  ' || failures=$((failures + 1))
}

# Rule 7: no download or decoded text piped to a shell.
check_pipes() {
  local file="$1" hit
  hit="$(code "$file" | grep -n -E '(curl|wget)[^|]*\|[[:space:]]*(sudo[[:space:]]+)?(ba|z|da)?sh([[:space:]]|$)|base64[[:space:]]+(-d|--decode)[^|]*\|[[:space:]]*(ba)?sh([[:space:]]|$)' | head -n 1)" || return 0
  broken 7 "$file" "${hit%%:*}" "a download or decoded text is piped to a shell"
}

check_dir() {
  local dir="$1" file count=0
  while IFS= read -r file; do
    count=$((count + 1))
    check_pins "$file"
    [[ "$file" == */workflows/* ]] && {
      check_permissions "$file"
      check_triggers "$file"
    }
    check_checkouts "$file"
    check_secrets "$file"
    check_run_blocks "$file"
    check_pipes "$file"
  done < <(files "$dir")
  ((count > 0)) || {
    echo "error: no workflow or action under $dir" >&2
    return 1
  }
  if ((failures == 0)); then
    echo "PASS  the $count workflows and actions of $dir follow the policy (ADR-0037)"
    return 0
  fi
  echo "error: $failures rule(s) broken in $dir" >&2
  return 1
}

# self_test: one fixture per rule, each of which must fail with that rule, and one good
# fixture, which must pass.
self_test() {
  local pass=0 fail=0 rule out
  # Not local: the trap runs after the function has returned.
  work="$(mktemp -d)"
  trap 'rm -rf "$work"' EXIT
  mkdir -p "$work/good/workflows" "$work/good/actions/local"
  cat >"$work/good/workflows/good.yaml" <<'EOF'
name: Good
on:
  push:
    branches: [main]
permissions:
  contents: read
jobs:
  build:
    runs-on: ubuntu-24.04
    env:
      HAS_TOKEN: ${{ secrets.TOKEN != '' }}
    steps:
      - name: Check out
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1
        with:
          persist-credentials: false
      - name: Run
        env:
          TOKEN: ${{ secrets.TOKEN }}
          TITLE: ${{ github.event.pull_request.title }}
        run: |
          echo "$TITLE"
          echo ok
      - name: Local
        uses: ./.github/actions/local
EOF
  cat >"$work/good/actions/local/action.yml" <<'EOF'
name: Local
runs:
  using: composite
  steps:
    - name: Step
      shell: bash
      run: echo ok
EOF
  for rule in 1 2 3 4 5 6 7; do
    mkdir -p "$work/bad$rule/workflows"
    cp "$work/good/workflows/good.yaml" "$work/bad$rule/workflows/bad.yaml"
  done
  sed -i 's|actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1|actions/checkout@v7|' "$work/bad1/workflows/bad.yaml"
  sed -i 's|^  contents: read|  contents: write|' "$work/bad2/workflows/bad.yaml"
  sed -i '/persist-credentials: false/d' "$work/bad3/workflows/bad.yaml"
  sed -i 's|^  push:|  pull_request_target:|' "$work/bad4/workflows/bad.yaml"
  sed -i "s|HAS_TOKEN: \${{ secrets.TOKEN != '' }}|TOKEN: \${{ secrets.TOKEN }}|" "$work/bad5/workflows/bad.yaml"
  sed -i "s|echo ok|echo \"\${{ github.event.pull_request.title }}\"|" "$work/bad6/workflows/bad.yaml"
  sed -i 's|echo ok|curl -fsSL https://example.invalid/install.sh \| sh|' "$work/bad7/workflows/bad.yaml"

  if bash "${BASH_SOURCE[0]}" "$work/good" >/dev/null 2>&1; then
    echo "PASS  the good fixture passes"
    pass=$((pass + 1))
  else
    echo "FAIL  the good fixture passes"
    fail=$((fail + 1))
  fi
  for rule in 1 2 3 4 5 6 7; do
    out="$(bash "${BASH_SOURCE[0]}" "$work/bad$rule" 2>&1)"
    if [[ $? -ne 0 && "$out" == *"FAIL  rule $rule "* ]]; then
      echo "PASS  the fixture of rule $rule fails with rule $rule"
      pass=$((pass + 1))
    else
      echo "FAIL  the fixture of rule $rule fails with rule $rule"
      fail=$((fail + 1))
    fi
  done
  echo "$pass passed, $fail failed"
  ((fail == 0))
}

case "${1:-}" in
  --self-test) self_test ;;
  -h | --help) sed -n '2,/^set -uo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//' ;;
  *) check_dir "${1:-$REPO/.github}" ;;
esac
