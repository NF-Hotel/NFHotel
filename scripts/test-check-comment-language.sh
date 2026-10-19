#!/usr/bin/env bash
# Tests of scripts/check-comment-language.sh on small files in a temporary folder (no git needed).
#
# Usage: scripts/test-check-comment-language.sh        (exit 0 when every test passes)
#
# No `set -e` on purpose: every case is run to the end so that all failures are counted.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="${root}/scripts/check-comment-language.sh"
work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT

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

# Run the check on one file with the given content; sets `status` and `output`.
check() {
    local name="$1" content="$2"
    printf '%s\n' "${content}" >"${work}/${name}"
    output="$(cd "${work}" && bash "${script}" "${name}" 2>&1)"
    status=$?
}

check ok.cs '// Creates the room and saves it.
var x = 1;'
expect "english line comment, exit 0" "0" "${status}"

check bad-letters.cs '// Opretter værelset og gemmer det.
var x = 1;'
expect "danish letters in a line comment, exit 1" "1" "${status}"
expect "danish letters, file and line are named" "1" "$(grep -c '^bad-letters.cs:1:' <<<"${output}")"

check bad-words.cs '/// <summary>Hent kode og byg projektet.</summary>
public class A { }'
expect "danish words in an xml doc comment, exit 1" "1" "${status}"

check block.css '/* Farven er ikke
   ændret */
a { color: red; }'
expect "danish in a multi-line block comment, exit 1" "1" "${status}"

check razor.razor '@* Viser alle gæster *@
<p>text</p>'
expect "danish in a razor comment, exit 1" "1" "${status}"

check html.csproj '<!-- Kontrollér ændringer før merge -->
<Project />'
expect "danish in an xml comment, exit 1" "1" "${status}"

check ok.yml '# Build, test and check the code
name: x'
expect "english yaml comment, exit 0" "0" "${status}"

check bad.yml '# Hent kode fra repository
name: x'
expect "danish yaml comment, exit 1" "1" "${status}"

check bad-inline.sh '#!/usr/bin/env bash
echo "hello" # Skriver teksten ud og afslutter
'
expect "danish trailing shell comment, exit 1" "1" "${status}"

check shebang.sh '#!/usr/bin/env bash
echo hello'
expect "the shebang is not a comment, exit 0" "0" "${status}"

check string.cs 'var s = "Opretter værelset"; // Creates the text.'
expect "danish in a string literal only, exit 0" "0" "${status}"

check url.cs 'var u = "http://example.org/ændring"; // The address of the service.'
expect "double slash in a url is not a comment, exit 0" "0" "${status}"

check color.css 'a { color: #fff; } /* white link */'
expect "hash colour in css is not a comment, exit 0" "0" "${status}"

check ignored.cs '// Dette er dansk, language-check: ignore (quoted Danish text for a test)'
expect "a marked line is skipped, exit 0" "0" "${status}"

check english-words.cs '// The order of the days is the same for each guest and for all rooms.'
expect "english words that look short are not flagged, exit 0" "0" "${status}"

check readme.md '# Hent kode og byg'
expect "markdown is not read, exit 0" "0" "${status}"

mkdir -p "${work}/docs"
printf '%s\n' '// Opretter værelset' >"${work}/docs/note.cs"
output="$(cd "${work}" && bash "${script}" docs/note.cs 2>&1)"
expect "a path under docs is skipped, exit 0" "0" "$?"

printf '%s\n' 'bad-ignored.cs' >"${work}/.language-check-ignore"
printf '%s\n' '// Opretter værelset' >"${work}/bad-ignored.cs"
output="$(cd "${work}" && bash "${script}" bad-ignored.cs 2>&1)"
expect "a path in .language-check-ignore is skipped, exit 0" "0" "$?"

output="$(cd "${work}" && bash "${script}" bad-letters.cs ok.cs 2>&1)"
expect "two files, one bad, exit 1" "1" "$?"
expect "two files, the summary counts one comment" "1" "$(grep -c '1 comment(s)' <<<"${output}")"

output="$(cd "${work}" && bash "${script}" --bogus 2>&1)"
expect "unknown option, exit 2" "2" "$?"

output="$(cd "${work}" && bash "${script}" missing.cs 2>&1)"
expect "a file that does not exist is skipped, exit 0" "0" "$?"

if ((failures > 0)); then
    echo "${failures} of ${checks} checks failed"
    exit 1
fi
echo "all ${checks} checks passed"
