#!/usr/bin/env bash
# Fail when the line coverage of the .NET code falls under its floor (MIL-054, ADR-0033): 95 % in total and 90 % for
# each project under src/. It reads the Cobertura file that dotnet-coverage writes in the build job of NF Hotel CI, in
# which a package is a project. A line that appears in more than one class of the same file (a lambda, a partial class)
# is counted once, covered when any of them ran it.
#
# Usage: scripts/check-coverage-floor.sh [--total PERCENT] [--project PERCENT] <coverage.cobertura.xml>
#   --total PERCENT    the floor of all projects together (default 95)
#   --project PERCENT  the floor of each project (default 90)
#
# Exit codes: 0 every floor is met; 1 a floor is not met, each one printed; 2 wrong usage, a missing file or a
# report without lines.
# Needs: bash, awk (POSIX: gawk or mawk).
set -euo pipefail

total_floor=95
project_floor=90
report=""

fail_usage() {
    echo "error: $*" >&2
    echo "usage: scripts/check-coverage-floor.sh [--total PERCENT] [--project PERCENT] <coverage.cobertura.xml>" >&2
    exit 2
}

is_percent() {
    [[ "$1" =~ ^[0-9]+([.][0-9]+)?$ ]] && awk -v value="$1" 'BEGIN { exit !(value <= 100) }'
}

while (($# > 0)); do
    case "$1" in
        --total | --project)
            (($# >= 2)) || fail_usage "$1 needs a percent"
            is_percent "$2" || fail_usage "$1 needs a percent from 0 to 100, not '$2'"
            if [[ "$1" == "--total" ]]; then total_floor="$2"; else project_floor="$2"; fi
            shift 2
            ;;
        -*) fail_usage "unknown option $1" ;;
        *)
            [[ -z "$report" ]] || fail_usage "only one report can be checked"
            report="$1"
            shift
            ;;
    esac
done

[[ -n "$report" ]] || fail_usage "no report given"
[[ -f "$report" ]] || fail_usage "no report at $report"

# One record per package, class and line; the attributes are read by name, so their order does not matter.
awk -v total_floor="$total_floor" -v project_floor="$project_floor" '
    function attribute(text, name,    found) {
        if (match(text, name "=\"[^\"]*\"")) {
            found = substr(text, RSTART + length(name) + 2, RLENGTH - length(name) - 3)
            return found
        }
        return ""
    }
    {
        rest = $0
        while (match(rest, /<(package|class|line) [^>]*>/)) {
            tag = substr(rest, RSTART, RLENGTH)
            rest = substr(rest, RSTART + RLENGTH)
            if (tag ~ /^<package /) {
                package = attribute(tag, "name")
            } else if (tag ~ /^<class /) {
                file = attribute(tag, "filename")
            } else {
                key = package SUBSEP file SUBSEP attribute(tag, "number")
                if (!(key in hits)) {
                    projects[package] = 1
                    lines[package]++
                    hits[key] = 0
                }
                if (attribute(tag, "hits") + 0 > 0 && hits[key] == 0) {
                    hits[key] = 1
                    covered[package]++
                }
            }
        }
    }
    END {
        for (project in projects) {
            all += lines[project]
            done += covered[project]
        }
        if (all == 0) {
            print "error: the report has no lines" > "/dev/stderr"
            exit 2
        }
        failed = 0
        for (project in projects) {
            rate = 100 * covered[project] / lines[project]
            mark = rate < project_floor ? "UNDER" : "ok"
            printf "%-32s %6d/%-6d %6.2f %%  %s\n", project, covered[project], lines[project], rate, mark | "sort"
            if (rate < project_floor) {
                printf "error: %s has %.2f %% line coverage, under the floor of %s %% per project\n", project, rate, project_floor > "/dev/stderr"
                failed = 1
            }
        }
        close("sort")
        rate = 100 * done / all
        printf "%-32s %6d/%-6d %6.2f %%  %s\n", "total", done, all, rate, rate < total_floor ? "UNDER" : "ok"
        if (rate < total_floor) {
            printf "error: the total line coverage is %.2f %%, under the floor of %s %%\n", rate, total_floor > "/dev/stderr"
            failed = 1
        }
        exit failed
    }
' "$report"
