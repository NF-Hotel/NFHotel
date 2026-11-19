#!/usr/bin/env bash
# Tests of scripts/check-security-txt.sh (MIL-084, ADR-0048). A good file passes, and so does the
# file of the repository with the three warnings that ADR-0048 expects. Each requirement of RFC 9116
# and each rule of the project fails with its kind and its source in the message; Expires, the size,
# the number of lines and the length of a field are tested at and beside each limit. Then the
# warnings with and without --expect-warnings; the answer of an address (status, redirect, type,
# Canonical, Strict-Transport-Security) and the addresses that the file names, through a fake curl;
# and the usage errors. No message may print a mailbox or a telephone number. --now fixes the clock.
#
# Run it in Debian 12 with ops/ at /src:
#   docker run --rm -v "$PWD/ops:/src:ro" debian:12-slim bash /src/tests/test-check-security-txt.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 12

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

OPS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CHECK="$OPS/scripts/check-security-txt.sh"
W="$(mktemp -d)"
trap 'rm -rf "$W"' EXIT
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

readonly NOW=2026-11-18T00:00:00Z SITE=https://www.example.com
readonly CANONICAL="$SITE/.well-known/security.txt"
BASE="$(date -u -d "$NOW" +%s)"
# day <n>: the time n days after NOW, written as Expires is.
day() { date -u -d "@$((BASE + $1 * 86400))" +%Y-%m-%dT%H:%M:%SZ; }

# good: a file that meets every requirement and rule; its only warning is unsigned. Line 7 is Policy.
good() {
  printf '%s\n' '# A file of the tests.' "Contact: $SITE/report" 'Contact: mailto:security@example.com' \
    "Expires: $(day 180)" 'Preferred-Languages: da, en' "Canonical: $CANONICAL" "Policy: $SITE/policy" \
    "Encryption: $SITE/key.txt" 'Bug-Bounty: False'
}
# variant <name> <sed argument...>: the good file changed by sed, as $W/<name>.
variant() {
  local name="$1"
  shift
  good | sed "$@" >"$W/$name"
}
# appended <name> <line...>: the good file with the lines after it (from line 10), as $W/<name>.
appended() {
  local name="$1"
  shift
  { good && printf '%s\n' "$@"; } >"$W/$name"
}
# run <file> [option...]: the check of a file with the fixed clock, the canonical address and the
# warning unsigned expected (a later option wins), into $W/out.
run() {
  local file="$1"
  shift
  bash "$CHECK" --file "$file" --canonical "$CANONICAL" --now "$NOW" --expect-warnings unsigned "$@" >"$W/out" 2>&1
}
# passes <name> <file> [option...]: the check passes.
passes() {
  local name="$1"
  shift
  run "$@" && grep -q '^PASS' "$W/out" && ok "$name" || ko "$name: $(cat "$W/out")"
}
# fails <name> <text> <file> [option...]: the check exits 1 and prints the text.
fails() {
  local name="$1" text="$2" status
  shift 2
  run "$@"
  status=$?
  [[ $status == 1 ]] && grep -qF -- "$text" "$W/out" && ok "$name" || ko "$name (exit $status): $(cat "$W/out")"
}

echo "== good files"
good >"$W/good"
passes "a file that meets every requirement passes, with the warning unsigned expected" "$W/good"
grep -q '^WARN  unsigned: ' "$W/out" && ok "the warning unsigned is shown" || ko "no warning unsigned: $(cat "$W/out")"
repo="$OPS/deploy/nginx/well-known/security.txt"
expires="$(sed -n 's/^Expires: //p' "$repo" 2>/dev/null)"
repo_now="$(date -u -d "@$(($(date -u -d "${expires:-1970-01-01T00:00:00Z}" +%s) - 100 * 86400))" +%Y-%m-%dT%H:%M:%SZ)"
bash "$CHECK" --file "$repo" --canonical https://nfhotel.devmatiker.com/.well-known/security.txt --now "$repo_now" \
  --expect-warnings unsigned,no-encryption,no-role-address >"$W/out" 2>&1 &&
  ok "the file of the repository passes with the three warnings of ADR-0048, 100 days before its Expires" ||
  ko "the file of the repository: $(cat "$W/out")"
! grep -q '@' "$W/out" && ok "the check of the file of the repository prints no mailbox" || ko "mailbox: $(cat "$W/out")"
variant crlf 's/$/\r/'
passes "CRLF line ends pass (RFC 9116, section 2.2)" "$W/crlf"
variant case -e 's/^Contact:/contact:/' -e 's/^Expires:/EXPIRES:/'
passes "field names in another letter case pass (RFC 9116, section 2)" "$W/case"
{ echo && good | sed 's/^Policy: .*/& \t/' && printf ' \t\n# The end.\n'; } >"$W/blank"
passes "empty lines and white space before a line end pass (eol, RFC 9116, section 4)" "$W/blank"
appended dns 'Encryption: dns:5d2d37ab76d47d36._openpgpkey.example.com?type=OPENPGPKEY'
passes "an address that is not on the web passes in Encryption (RFC 9116, section 2.5.4)" "$W/dns"
variant registry -e 's/^Bug-Bounty: False/Bug-Bounty: True/' -e 's/^Preferred-Languages: .*/Preferred-Languages: en-GB,da/' \
  -e "\$a CSAF: $SITE/csaf/provider-metadata.json" -e "\$a Hiring: $SITE/jobs" -e "\$a Acknowledgments: $SITE/thanks"
passes "every field of the registry, Bug-Bounty True and language tags without spaces pass" "$W/registry"

echo "== requirements of RFC 9116: failures"
variant no-contact '/^Contact:/d'
fails "no Contact fails" "RFC 9116 2.5.3: no Contact" "$W/no-contact"
variant no-expires '/^Expires:/d'
fails "no Expires fails" "RFC 9116 2.5.5: no Expires" "$W/no-expires"
appended two-expires "Expires: $(day 100)"
fails "Expires twice fails" "RFC 9116 2.5.5: Expires appears 2 times" "$W/two-expires"
appended two-languages 'Preferred-Languages: en'
fails "Preferred-Languages twice fails" "RFC 9116 2.5.8: Preferred-Languages appears 2 times" "$W/two-languages"
for tags in 'da, , en' 'da;en' 'da,' 'da_DK'; do
  variant tags "s/^Preferred-Languages: .*/Preferred-Languages: $tags/"
  fails "Preferred-Languages '$tags' fails" \
    "RFC 9116 2.5.8: Preferred-Languages on line 5 is not a list of language tags" "$W/tags"
done
for bad in 'Contact:https://www.example.com/report' 'Contact:  https://www.example.com/report' \
  ' Contact: https://www.example.com/report' $'Contact:\thttps://www.example.com/report' 'Preferred-Languages:' \
  'Report it to the form' ' # an indented comment' 'Hiring :https://www.example.com/jobs'; do
  appended bad "$bad"
  fails "the line '$bad' fails" "RFC 9116 4: line 10 is neither empty, a comment nor a field" "$W/bad"
done
for entry in Acknowledgments:2.5.1 Canonical:2.5.2 Contact:2.5.3 Encryption:2.5.4 Hiring:2.5.6 Policy:2.5.7; do
  field="${entry%%:*}"
  appended http "$field: http://www.example.com/$field"
  fails "$field over http fails" \
    "RFC 9116 ${entry#*:}: $field on line 10 is a web address that does not begin with https://" "$W/http"
done
appended no-slashes 'Hiring: https:www.example.com/jobs'
fails "https: without // fails" "RFC 9116 2.5.6: Hiring on line 10 is a web address that does not begin with https://" \
  "$W/no-slashes"
appended not-uri 'Policy: www.example.com/policy'
fails "a Policy that is not a URI fails" "RFC 9116 2.5.7: Policy on line 10 is not a URI" "$W/not-uri"
for value in '2027-05-17 00:00:00' '17 May 2027' '2027-02-30T00:00:00Z' '2027-05-17T24:00:00Z'; do
  variant expires "s/^Expires: .*/Expires: $value/"
  fails "Expires '$value' fails" "RFC 9116 2.5.5: Expires on line 4 is not a date and time of RFC 3339" "$W/expires"
done
variant past "s/^Expires: .*/Expires: $(day -1)/"
fails "an Expires in the past fails" "RFC 9116 2.5.5: Expires is past" "$W/past"
printf '%s' "$(good)" >"$W/no-end"
fails "a last line without its line end fails" "RFC 9116 2.2: the last line has no line end" "$W/no-end"
{ good && printf 'Hiring: %s/jobs\rx\n' "$SITE"; } >"$W/cr"
fails "a carriage return inside a line fails" "RFC 9116 2.2: line 10 has a carriage return that does not end it" "$W/cr"
{ printf '\357\273\277' && good; } >"$W/bom"
fails "a byte order mark fails" "RFC 9116 4: the file begins with a byte order mark" "$W/bom"
{ good && printf '# \377\n'; } >"$W/latin"
fails "bytes that are not UTF-8 fail" "RFC 9116 4: the file is not UTF-8" "$W/latin"
{ good && printf '# a bell \007\n'; } >"$W/control"
fails "a control character fails" "RFC 9116 4: the file has a control character" "$W/control"

echo "== rules of the project (ADR-0048): failures"
for value in '2027-05-17T01:00:00+01:00' '2027-05-17t00:00:00z' '2027-05-17T00:00:00.000Z'; do
  variant format "s/^Expires: .*/Expires: $value/"
  fails "Expires '$value' fails the form of the project" \
    "ADR-0048 rule: Expires is not written as YYYY-MM-DDThh:mm:ssZ" "$W/format"
  ! grep -q '^FAIL  RFC 9116' "$W/out" && ok "Expires '$value' is RFC 3339, so no requirement of the RFC fails" ||
    ko "RFC 3339 '$value': $(cat "$W/out")"
done
for days in 29 30 366 367; do
  variant "d$days" "s/^Expires: .*/Expires: $(day "$days")/"
done
fails "Expires 29 days ahead fails" "ADR-0048 rule: Expires is 29 days ahead, fewer than 30" "$W/d29"
passes "Expires 30 days ahead passes" "$W/d30"
passes "Expires 366 days ahead passes" "$W/d366"
fails "Expires 367 days ahead fails" "ADR-0048 rule: Expires is 367 days ahead, more than 366" "$W/d367"
fails "--min-days moves the lower limit" "ADR-0048 rule: Expires is 30 days ahead, fewer than 31" "$W/d30" --min-days 31
variant no-canonical '/^Canonical:/d'
fails "no Canonical fails" "ADR-0048 rule: Canonical appears 0 times, not once" "$W/no-canonical"
appended two-canonicals "Canonical: $SITE/security.txt"
fails "two Canonical fields fail" "ADR-0048 rule: Canonical appears 2 times, not once" "$W/two-canonicals"
variant other-canonical 's|^Canonical: .*|Canonical: https://other.example.com/.well-known/security.txt|'
fails "a Canonical other than --canonical fails" "ADR-0048 rule: Canonical is not $CANONICAL" "$W/other-canonical"
variant no-policy '/^Policy:/d'
fails "no Policy fails" "ADR-0048 rule: no Policy" "$W/no-policy"
variant tel 's/^Contact: mailto:.*/Contact: tel:+45-12-34-56-78/'
fails "a Contact by telephone fails" "ADR-0048 rule: Contact on line 3 is tel:, neither https: nor mailto:" "$W/tel"
! grep -q '12-34-56-78' "$W/out" && ok "the telephone number is not printed" || ko "number printed: $(cat "$W/out")"
appended unknown "Contacts: $SITE/report"
fails "a field outside the registry of IANA fails" \
  "ADR-0048 rule: the field Contacts on line 10 is not in the registry of IANA" "$W/unknown"
variant bounty 's/^Bug-Bounty: False/Bug-Bounty: yes/'
fails "a Bug-Bounty other than True or False fails" "ADR-0048 rule: Bug-Bounty on line 9 is neither True nor False" \
  "$W/bounty"
appended two-bounties 'Bug-Bounty: True'
fails "Bug-Bounty twice fails" "ADR-0048 rule: Bug-Bounty appears 2 times" "$W/two-bounties"
{ good && printf '# S\303\270ren\n'; } >"$W/utf8"
fails "UTF-8 outside ASCII fails the rule of the project" "ADR-0048 rule: the file has bytes outside ASCII" "$W/utf8"
! grep -q '^FAIL  RFC 9116' "$W/out" && ok "UTF-8 outside ASCII meets the RFC" || ko "UTF-8: $(cat "$W/out")"
# pad <bytes> <file>: the good file with comment lines up to exactly that many bytes.
pad() {
  local rest
  good >"$2"
  while rest=$(($1 - $(wc -c <"$2"))) && ((rest >= 200)); do
    printf '#%098d\n' 0 >>"$2"
  done
  printf '#%0*d\n' $((rest - 2)) 0 >>"$2"
}
pad 32000 "$W/32000"
passes "a file of 32,000 bytes passes" "$W/32000"
pad 32001 "$W/32001"
fails "a file of 32,001 bytes fails" "ADR-0048 rule: the file has 32001 bytes, more than 32000" "$W/32001"
{ good && yes '#' | head -n 991; } >"$W/1000"
passes "a file of 1,000 lines passes" "$W/1000"
{ good && yes '#' | head -n 992; } >"$W/1001"
fails "a file of 1,001 lines fails" "ADR-0048 rule: the file has 1001 lines, more than 1000" "$W/1001"
# letters <n>: n times the letter a.
letters() { head -c "$1" /dev/zero | tr '\0' a; }
appended 2048 "Hiring: $SITE/$(letters 2016)"
passes "a field of 2,048 characters passes" "$W/2048"
appended 2049 "Hiring: $SITE/$(letters 2017)"
fails "a field of 2,049 characters fails" "ADR-0048 rule: the field on line 10 has 2049 characters, more than 2048" \
  "$W/2049"
{ printf '%s\n' '-----BEGIN PGP SIGNED MESSAGE-----' 'Hash: SHA512' '' && good; } >"$W/signed"
fails "a signed file fails until the check can read one (E1)" "ADR-0048 rule: the file is signed" "$W/signed"
cp "$W/good" "$W/same"
passes "a file equal to --same-as passes" "$W/good" --same-as "$W/same"
fails "a file that differs from --same-as fails" "ADR-0048 rule: the body differs from $W/crlf" "$W/good" \
  --same-as "$W/crlf"

echo "== warnings: the exceptions of ADR-0048"
variant like-repo -e 's/^Contact: mailto:security@/Contact: mailto:someone@/' -e '/^Encryption:/d'
fails "warnings that are not expected fail" \
  "ADR-0048 rule: the warnings are no-encryption,no-role-address,unsigned, not none" "$W/like-repo" --expect-warnings ''
grep -q '^WARN  unsigned: .*exception E1 of ADR-0048)$' "$W/out" &&
  grep -q '^WARN  no-encryption: .*exception E3 of ADR-0048)$' "$W/out" &&
  grep -q '^WARN  no-role-address: .*exception E2 of ADR-0048)$' "$W/out" &&
  ok "each warning is shown with its name and its exception in full" || ko "warnings: $(cat "$W/out")"
! grep -q 'someone' "$W/out" && ok "no message prints the mailbox" || ko "mailbox printed: $(cat "$W/out")"
passes "the three warnings pass when they are expected" "$W/like-repo" \
  --expect-warnings unsigned,no-encryption,no-role-address
passes "the order of the expected warnings does not matter" "$W/like-repo" \
  --expect-warnings no-role-address,unsigned,no-encryption
fails "a closed exception fails until the option changes too" \
  "ADR-0048 rule: the warnings are unsigned, not no-encryption,no-role-address,unsigned" "$W/good" \
  --expect-warnings unsigned,no-encryption,no-role-address
variant role '/^Encryption:/d'
fails "a mailbox named security without a key gives no-encryption only" \
  "ADR-0048 rule: the warnings are no-encryption,unsigned, not unsigned" "$W/role"

echo "== the answer of an address, and the addresses in the file (a fake curl)"
mkdir -p "$W/web"
cat >"$W/curl" <<'EOF'
#!/bin/bash
# The fake answers an address from $FAKE_WEB/<the address with _ for each other character>/: status,
# headers, body, and with --location the final address in effective. It logs each address it is asked.
output=/dev/null headers=/dev/null format="" address="" follow=0
while (($#)); do
  case "$1" in
    --output) output="$2" && shift ;;
    --dump-header) headers="$2" && shift ;;
    --write-out) format="$2" && shift ;;
    --location) follow=1 ;;
    --max-time | --max-filesize | --proto | --proto-redir | --max-redirs) shift ;;
    -*) ;;
    *) address="$1" ;;
  esac
  shift
done
echo "$address" >>"$FAKE_WEB/asked"
d="$FAKE_WEB/$(printf '%s' "$address" | tr -c 'A-Za-z0-9' '_')"
[[ -d "$d" ]] || { echo "curl: (6) Could not resolve host" >&2 && exit 6; }
final="$address"
((follow)) && [[ -f "$d/effective" ]] && final="$(cat "$d/effective")"
cat "$d/headers" >"$headers"
cat "$d/body" >"$output"
format="${format//'%{http_code}'/$(cat "$d/status")}"
printf '%s' "${format//'%{url_effective}'/$final}"
EOF
chmod +x "$W/curl"
export NFHOTEL_CURL="$W/curl" FAKE_WEB="$W/web"
# place <address>: the folder of the fake for the address.
place() { printf '%s/%s' "$FAKE_WEB" "$(printf '%s' "$1" | tr -c 'A-Za-z0-9' '_')"; }
# serve <address> <status> [header...]: the fake answers the address with the status, the headers and $W/body.
serve() {
  local d
  d="$(place "$1")"
  mkdir -p "$d"
  echo "$2" >"$d/status"
  printf 'HTTP/1.1 %s\r\n' "$2" >"$d/headers"
  if (($# > 2)); then printf '%s\r\n' "${@:3}" >>"$d/headers"; fi
  cp "$W/body" "$d/body"
  rm -f "$d/effective"
}
# moved <address> <final address>: with --location, the address ends at the final address.
moved() { echo "$2" >"$(place "$1")/effective"; }
# at_address [option...]: the check of the canonical address through the fake, into $W/out.
at_address() {
  bash "$CHECK" --url "$CANONICAL" --canonical "$CANONICAL" --now "$NOW" --expect-warnings unsigned "$@" >"$W/out" 2>&1
}
# address_fails <name> <text> [option...]: the check of an address exits 1 and prints the text.
address_fails() {
  local name="$1" text="$2" status
  shift 2
  at_address "$@"
  status=$?
  [[ $status == 1 ]] && grep -qF -- "$text" "$W/out" && ok "$name" || ko "$name (exit $status): $(cat "$W/out")"
}
readonly TEXT='Content-Type: text/plain; charset=utf-8' HSTS='Strict-Transport-Security: max-age=2592000'
good >"$W/body"
serve "$CANONICAL" 200 "$TEXT" "$HSTS"
at_address && grep -q '^PASS' "$W/out" && ok "the file at its address passes" || ko "the address: $(cat "$W/out")"
at_address --same-as "$W/good" && ok "the file at its address equals the file of --same-as" ||
  ko "--same-as at the address: $(cat "$W/out")"
serve "$CANONICAL" 200 'Content-Type: text/plain;charset=UTF-8' "$HSTS"
at_address && ok "another spelling of the same type passes" || ko "the type spelt otherwise: $(cat "$W/out")"
serve "$CANONICAL" 200 'Content-Type: text/html; charset=utf-8' "$HSTS"
address_fails "another type fails" "RFC 9116 3: the type is 'text/html; charset=utf-8', not text/plain; charset=utf-8"
serve "$CANONICAL" 404 'Content-Type: text/html'
address_fails "an answer of 404 fails" "RFC 9116 3: $CANONICAL answers 404, not 200"
serve "$CANONICAL" 301 'Location: /security.txt'
address_fails "a redirect fails" "ADR-0048 rule: $CANONICAL answers 301, a redirect"
address_fails "an http address fails" "RFC 9116 3: http://www.example.com/.well-known/security.txt is not an https address" \
  --url http://www.example.com/.well-known/security.txt
! grep -q '^http:' "$FAKE_WEB/asked" && ok "an http address is not asked" || ko "asked: $(cat "$FAKE_WEB/asked")"
address_fails "an address that cannot be fetched fails" \
  "RFC 9116 3: https://nowhere.example.org/.well-known/security.txt could not be fetched (curl exit 6)" \
  --url https://nowhere.example.org/.well-known/security.txt
serve "$SITE/copy.txt" 200 "$TEXT" "$HSTS"
address_fails "a file whose Canonical does not name its address fails" \
  "ADR-0048 rule: the Canonical fields do not name $SITE/copy.txt" --url "$SITE/copy.txt"
serve "$CANONICAL" 200 "$TEXT" "$HSTS"
serve "$SITE/" 200 'Content-Type: text/html' "$HSTS"
at_address --hsts-like "$SITE/" && ok "the Strict-Transport-Security of the front page passes" ||
  ko "--hsts-like: $(cat "$W/out")"
serve "$SITE/" 200 'Content-Type: text/html' 'Strict-Transport-Security: max-age=31536000'
address_fails "another Strict-Transport-Security fails" \
  "ADR-0048 rule: Strict-Transport-Security is 'max-age=2592000', and $SITE/ sends 'max-age=31536000'" \
  --hsts-like "$SITE/"
serve "$SITE/" 200 'Content-Type: text/html'
address_fails "a front page without Strict-Transport-Security fails" \
  "ADR-0048 rule: Strict-Transport-Security is 'max-age=2592000', and $SITE/ sends ''" --hsts-like "$SITE/"
for path in report policy key.txt; do
  serve "$SITE/$path" 200 'Content-Type: text/html'
done
moved "$SITE/report" "$SITE/login?return_to=report"
passes "addresses that answer on their own host pass, also after a redirect" "$W/good" --links
! grep -q mailto "$FAKE_WEB/asked" && ok "a mailbox is not asked" || ko "asked: $(cat "$FAKE_WEB/asked")"
serve "$SITE/policy" 404 'Content-Type: text/html'
fails "an address that answers 404 fails" "ADR-0048 rule: the address on line 7 (Policy) answers 404" "$W/good" --links
serve "$SITE/policy" 200 'Content-Type: text/html'
moved "$SITE/policy" https://elsewhere.example.net/policy
fails "an address that ends on another host fails" \
  "ADR-0048 rule: the address on line 7 (Policy) ends on another host: elsewhere.example.net" "$W/good" --links

echo "== usage errors"
# exit_of [option...]: the exit status of the check.
exit_of() {
  bash "$CHECK" "$@" >"$W/out" 2>&1
  echo $?
}
[[ "$(exit_of)" == 2 ]] && ok "no option is a usage error" || ko "no option: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --url "$CANONICAL")" == 2 ]] && ok "--file and --url together are a usage error" ||
  ko "both: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --bogus)" == 2 ]] && ok "an unknown option is a usage error" || ko "bogus: $(cat "$W/out")"
[[ "$(exit_of --file)" == 2 ]] && ok "an option without its value is a usage error" || ko "no value: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --expect-warnings unsigned,unknown)" == 2 ]] &&
  ok "an unknown warning is a usage error" || ko "unknown warning: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --min-days thirty)" == 2 ]] && ok "--min-days that is not a number is a usage error" ||
  ko "--min-days: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --now 'not a date')" == 2 ]] && ok "--now that is not a date is a usage error" ||
  ko "--now: $(cat "$W/out")"
[[ "$(exit_of --file "$W/good" --hsts-like "$SITE/")" == 2 ]] && ok "--hsts-like without --url is a usage error" ||
  ko "--hsts-like: $(cat "$W/out")"
[[ "$(exit_of --file "$W/missing")" == 2 ]] && ok "a file that cannot be read is a usage error" ||
  ko "missing file: $(cat "$W/out")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
