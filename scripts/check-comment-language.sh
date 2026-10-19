#!/usr/bin/env bash
# Fail when a comment in the source is written in Danish. The comments of this repository are in English,
# the Product Owner language (BC-001, MIL-010); the check keeps them so.
#
# A comment is flagged when its text has a Danish letter (ae, o-slash, a-ring, in either case) or a word that
# is Danish and not English (the list is WORDS below). Only comments are read, never string literals or
# prose: the comment markers are `//`, `/* */`, `@* *@` and `<!-- -->` in C#, Razor, CSS, JavaScript and
# project files; `#` in YAML, shell, Python, Dockerfile, env, ini and ignore files; `--` in SQL.
#
# Usage: scripts/check-comment-language.sh [--staged] [file...]
#   (no argument)  every file that git tracks
#   --staged       the files that are staged for the next commit (for a pre-commit hook)
#   file...        exactly these files
#
# Exceptions:
#   - a comment line that holds the text `language-check: ignore` is skipped;
#   - a path that matches a line of `.language-check-ignore` (shell glob, one per line, `#` for notes) is skipped;
#   - generated and vendored paths are skipped (see is_skipped).
#
# Exit codes: 0 no Danish comment; 1 at least one, each printed as file:line: text; 2 wrong usage.
# Needs: bash 4 or later, git (when no file is named), awk (POSIX: gawk or mawk).
set -euo pipefail

# Danish words that are not English words. Short function words that are also English are left out on purpose
# (see WEAK below): the letters and these words catch what is written in Danish.
WORDS='og|ikke|til|af|som|skal|bliver|hvis|fra|også|når|eller|uden|efter|mellem|denne|dette|hvor|kun|være|blive|hent|bruger|bruge|brugeren|kontroller|kontrollér|ændringer|kode|byg|sikkerhed|afhængigheder|fejl|kørsel|projektets|både|alle|hvad|deres|disse|adgang|rollen|rolle|sammenlign|sammenligner|forventede|resultatet|forbered|forbered|opret|opretter|oprettet|ukendte|tilladelse|tilladelser|siden|sine|ligger|findes|hvilke|hvilken|hvilket|samme|kun|inde|ude|fordi|mange|nogle|ingen|hele|dette|først|første|anden|tredje|bliver|gemmer|henter|kalder|sender|viser|tilføjer|fjerner|returnerer|registrerer|opsat|klasse|metode|værdier|værdien|bookinger|gæster|booket|ledig|vedligehold'
# Short Danish words that are rare in English but not impossible: two different ones in one comment make it Danish.
WEAK='er|med|den|det|der|kan|ved|mod|nu|jeg'
LETTERS='æ|ø|å|Æ|Ø|Å'

root="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
ignore_file="${root}/.language-check-ignore"

fail_usage() {
    echo "error: $*" >&2
    echo "usage: scripts/check-comment-language.sh [--staged] [file...]" >&2
    exit 2
}

# True for generated and vendored paths, which are not written by hand.
is_skipped() {
    case "$1" in
        framework/* | docs/* | */wwwroot/lib/* | *.min.* | *.map | */Migrations/* | g/* | */bin/* | */obj/*) return 0 ;;
    esac
    if [[ -f "${ignore_file}" ]]; then
        local pattern
        while IFS= read -r pattern || [[ -n "${pattern}" ]]; do
            pattern="${pattern%%#*}"
            pattern="${pattern//[$'\t\r ']/}"
            # shellcheck disable=SC2254 # the pattern is a glob on purpose
            [[ -n "${pattern}" ]] && case "$1" in ${pattern}) return 0 ;; esac
        done <"${ignore_file}"
    fi
    return 1
}

# The comment style of a file: c (// and block markers), hash, sql, or none.
style_of() {
    local name base
    name="$1"
    base="${name##*/}"
    case "${base}" in
        Dockerfile | Dockerfile.* | .gitignore | .dockerignore | .editorconfig | .gitattributes | .yamllint | .env | *.env) echo hash ;;
        *.cs | *.razor | *.css | *.js | *.csproj | *.props | *.targets | *.xml) echo c ;;
        *.yml | *.yaml | *.sh | *.py | *.conf | *.example | *.toml | *.ini | *.template | *.ps1) echo hash ;;
        *.sql) echo sql ;;
        *) echo none ;;
    esac
}

# Print "line<TAB>text" for every comment of a file in the given style that looks Danish. One awk process reads
# the file and does the whole test, because a process per comment would make a full run take minutes.
danish_comments_of() {
    local file="$1" style="$2"
    awk -v style="${style}" -v letters="${LETTERS}" -v words="${WORDS}" -v weak="${WEAK}" '
        function danish(t,    rest, n) {
            if (t ~ letters) return 1
            rest = " " tolower(t) " "
            if (rest ~ ("[^a-z]+(" words ")[^a-z]")) return 1
            n = 0
            split("", seen)
            while (match(rest, "[^a-z]+(" weak ")[^a-z]")) {
                word = substr(rest, RSTART, RLENGTH)
                gsub(/[^a-z]/, "", word)
                if (!(word in seen)) { seen[word] = 1; n++ }
                rest = substr(rest, RSTART + RLENGTH - 1)
            }
            return n >= 2
        }
        function emit(n, t) {
            if (t !~ /[^ \t]/ || index(t, "language-check: ignore") > 0) return
            if (danish(t)) print n "\t" t
        }
        {
            line = $0
            if (style == "hash") {
                if (NR == 1 && line ~ /^#!/) next
                pos = match(line, /(^|[ \t])#/)
                if (pos > 0) emit(NR, substr(line, pos + RLENGTH))
                next
            }
            if (style == "sql") {
                pos = index(line, "--")
                if (pos > 0) emit(NR, substr(line, pos + 2))
                next
            }
            # style c: walk the line, one marker at a time
            while (length(line) > 0) {
                if (block != "") {
                    end = index(line, block)
                    if (end == 0) { emit(NR, line); break }
                    emit(NR, substr(line, 1, end - 1))
                    line = substr(line, end + length(block))
                    block = ""
                    continue
                }
                best = 0; kind = ""
                p = index(line, "//")
                while (p > 1 && substr(line, p - 1, 1) == ":") {
                    q = index(substr(line, p + 2), "//")
                    if (q == 0) { p = 0; break }
                    p = p + 1 + q
                }
                if (p > 0) { best = p; kind = "//" }
                p = index(line, "/*"); if (p > 0 && (best == 0 || p < best)) { best = p; kind = "/*" }
                p = index(line, "@*"); if (p > 0 && (best == 0 || p < best)) { best = p; kind = "@*" }
                p = index(line, "<!--"); if (p > 0 && (best == 0 || p < best)) { best = p; kind = "<!--" }
                if (best == 0) break
                if (kind == "//") { emit(NR, substr(line, best + 2)); break }
                if (kind == "/*") { block = "*/"; line = substr(line, best + 2) }
                else if (kind == "@*") { block = "*@"; line = substr(line, best + 2) }
                else { block = "-->"; line = substr(line, best + 4) }
            }
        }
    ' "${file}"
}

files=()
staged=0
while (($# > 0)); do
    case "$1" in
        --staged) staged=1; shift ;;
        -h | --help) sed -n '2,/^set -euo/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'; exit 0 ;;
        -*) fail_usage "unknown option $1" ;;
        *) files+=("$1"); shift ;;
    esac
done

if ((${#files[@]} == 0)); then
    if ((staged == 1)); then
        mapfile -t files < <(git diff --cached --name-only --diff-filter=ACM)
    else
        mapfile -t files < <(git ls-files)
    fi
fi

found=0
for file in "${files[@]}"; do
    [[ -f "${file}" ]] || continue
    relative="${file#"${root}"/}"
    is_skipped "${relative}" && continue
    style="$(style_of "${file}")"
    [[ "${style}" == none ]] && continue
    while IFS=$'\t' read -r number text; do
        printf '%s:%s: %s\n' "${relative}" "${number}" "${text## }"
        found=$((found + 1))
    done < <(danish_comments_of "${file}" "${style}")
done

if ((found > 0)); then
    echo "error: ${found} comment(s) look Danish; write comments in English (or add 'language-check: ignore' with a reason)" >&2
    exit 1
fi
