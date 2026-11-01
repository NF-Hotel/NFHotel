#!/usr/bin/env bash
# Tests of scripts/check-coverage-floor.sh on small Cobertura files in a temporary folder (MIL-054 task 9).
#
# Usage: scripts/test-check-coverage-floor.sh        (exit 0 when every test passes)
#
# No `set -e` on purpose: every case is run to the end so that all failures are counted.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
script="${root}/scripts/check-coverage-floor.sh"
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

# Writes the lines of one class: covered lines first, then uncovered ones, numbered from the start.
class_lines() {
    local file="$1" start="$2" covered="$3" uncovered="$4" n
    echo "        <class name=\"C\" filename=\"${file}\" line-rate=\"0\"><lines>"
    for ((n = 0; n < covered; n++)); do echo "          <line number=\"$((start + n))\" hits=\"3\" />"; done
    for ((n = 0; n < uncovered; n++)); do echo "          <line hits=\"0\" number=\"$((start + covered + n))\" />"; done
    echo "        </lines></class>"
}

# Writes a report of the given projects, each as name:covered:uncovered.
report() {
    local name="$1" project
    shift
    {
        echo '<?xml version="1.0" encoding="utf-8"?>'
        echo '<coverage line-rate="0" version="1.9"><packages>'
        for project in "$@"; do
            IFS=: read -r package covered uncovered <<<"${project}"
            echo "    <package name=\"${package}\" line-rate=\"0\"><classes>"
            class_lines "src/${package}/A.cs" 1 "${covered}" "${uncovered}"
            echo "    </classes></package>"
        done
        echo '</packages></coverage>'
    } >"${work}/${name}"
}

# Runs the check; sets `status` and `output`.
check() {
    output="$(bash "${script}" "$@" 2>&1)"
    status=$?
}

report good.xml NFHotel.Web:96:4 NFHotel.Api:95:5
check "${work}/good.xml"
expect "every floor met, exit 0" "0" "${status}"
expect "every floor met, the total is printed" "1" "$(grep -c '^total .*95.50 %  ok$' <<<"${output}")"

report project-under.xml NFHotel.Web:990:10 NFHotel.Api:89:11
check "${work}/project-under.xml"
expect "one project under 90 %, exit 1" "1" "${status}"
expect "one project under 90 %, it is named" "1" "$(grep -c 'error: NFHotel.Api has 89.00 %' <<<"${output}")"
expect "one project under 90 %, the other is not named" "0" "$(grep -c 'error: NFHotel.Web' <<<"${output}")"

report total-under.xml NFHotel.Web:94:6 NFHotel.Api:94:6
check "${work}/total-under.xml"
expect "total under 95 %, exit 1" "1" "${status}"
expect "total under 95 %, it is named" "1" "$(grep -c 'error: the total line coverage is 94.00 %' <<<"${output}")"

report boundary.xml NFHotel.Web:95:5 NFHotel.Api:90:10 NFHotel.Domain:100:0
check "${work}/boundary.xml"
expect "exactly on the floors (95 % total, 90 % a project), exit 0" "0" "${status}"
check --total 95 --project 90 "${work}/good.xml"
expect "explicit floors equal to the defaults, exit 0" "0" "${status}"
report on-floor.xml NFHotel.Web:95:5
check --total 95 --project 95 "${work}/on-floor.xml"
expect "a rate equal to its floor passes, exit 0" "0" "${status}"
check --total 95.01 "${work}/on-floor.xml"
expect "a rate just under its floor fails, exit 1" "1" "${status}"

# A line in two classes of the same file (a lambda) counts once, covered when either class ran it.
{
    echo '<coverage><packages><package name="NFHotel.Web"><classes>'
    class_lines "src/NFHotel.Web/A.cs" 1 0 10
    class_lines "src/NFHotel.Web/A.cs" 1 10 0
    echo '</classes></package></packages></coverage>'
} >"${work}/duplicate.xml"
check "${work}/duplicate.xml"
expect "a line in two classes counts once and is covered, exit 0" "0" "${status}"
expect "a line in two classes counts once" "1" "$(grep -c 'NFHotel.Web .* 10/10 ' <<<"${output}")"

echo '<coverage><packages /></coverage>' >"${work}/empty.xml"
check "${work}/empty.xml"
expect "a report without lines, exit 2" "2" "${status}"

check "${work}/missing.xml"
expect "a missing report, exit 2" "2" "${status}"
check
expect "no report given, exit 2" "2" "${status}"
check --total "${work}/good.xml"
expect "a floor that is not a percent, exit 2" "2" "${status}"
check --project 101 "${work}/good.xml"
expect "a floor over 100, exit 2" "2" "${status}"
check --verbose "${work}/good.xml"
expect "an unknown option, exit 2" "2" "${status}"
check "${work}/good.xml" "${work}/good.xml"
expect "two reports, exit 2" "2" "${status}"

echo "${checks} checks, ${failures} failed"
((failures == 0))
