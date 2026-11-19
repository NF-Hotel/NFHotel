#!/usr/bin/env bash
# Check a security.txt against RFC 9116 and the rules of ADR-0048 (MIL-084). Each finding names its
# kind and its source: "RFC 9116 <section>" for a requirement of the RFC and "ADR-0048 rule" for a
# rule of this project, both failures, and WARN for a recommendation of the RFC that ADR-0048 lists
# as an exception. Warnings that differ from --expect-warnings fail too, so a new gap fails, and so
# does an exception that was closed. A message names a line and a field, never a value of the file:
# the output of the weekly look goes into a public issue (ADR-0048 decision 5).
#
# Usage: check-security-txt.sh (--file <path> | --url <address>) [--canonical <address>]
#          [--same-as <path>] [--links] [--hsts-like <address>] [--expect-warnings <names>]
#          [--min-days <n>] [--now <date>]
#   --file             the file to check
#   --url              fetch the file from this https address, without following a redirect, and
#                      check the answer too: its status, its type and a Canonical that names it
#   --canonical        the one Canonical that the file must have
#   --same-as          the file must equal this file, byte for byte (the file on main)
#   --links            each https address in the file must answer below 400 and end on its own host
#   --hsts-like        with --url: the answer must send the Strict-Transport-Security of this address
#   --expect-warnings  the warnings that ADR-0048 accepts, separated by commas, out of unsigned,
#                      no-encryption and no-role-address; the check fails unless it finds exactly these
#   --min-days         the fewest days that Expires may lie ahead (default 30)
#   --now              the time to compare Expires with (default: now; for the tests)
# Env:   NFHOTEL_CURL (default curl; for the tests)
# Exit:  0 every check passed; 1 a check failed; 2 usage error
# Needs: bash 4+, GNU coreutils and sed, grep, iconv, cmp; curl for --url, --links and --hsts-like

set -euo pipefail
export LC_ALL=C

curl="${NFHOTEL_CURL:-curl}"
# The limits a reader may apply (RFC 9116, section 5.4), and the window of Expires (ADR-0048).
readonly MAX_BYTES=32000 MAX_LINES=1000 MAX_FIELD=2048 MAX_DAYS=366 DAY=86400
# The fields of the registry of IANA (updated 2026-03-07), with the sections of RFC 9116 that define them.
readonly REGISTRY=" acknowledgments canonical contact csaf expires encryption hiring policy preferred-languages bug-bounty "
declare -rA FIELD_NAME=([acknowledgments]=Acknowledgments [canonical]=Canonical [contact]=Contact [csaf]=CSAF
  [expires]=Expires [encryption]=Encryption [hiring]=Hiring [policy]=Policy
  ["preferred-languages"]=Preferred-Languages ["bug-bounty"]=Bug-Bounty)
declare -rA FIELD_SECTION=([acknowledgments]=2.5.1 [canonical]=2.5.2 [contact]=2.5.3 [encryption]=2.5.4
  [hiring]=2.5.6 [policy]=2.5.7)
readonly WARNINGS=" unsigned no-encryption no-role-address "
# A field: a name of visible characters but the colon, a colon, one space and a value (RFC 9116, section 4).
readonly FIELD_RE='^([!-9;-~]+): ([^[:blank:]].*)$'
# The shape of a URI of RFC 3986: a scheme, a colon and no white space (not its full grammar).
readonly URI_RE='^[A-Za-z][A-Za-z0-9+.-]*:[^[:space:]]+$'
# Language tags of RFC 5646 in their simple form, separated by commas (lang-values, RFC 9116, section 4).
readonly TAG='[A-Za-z]{1,8}(-[A-Za-z0-9]{1,8})*'
readonly LANGUAGES_RE="^$TAG([[:blank:]]*,[[:blank:]]*$TAG)*\$"
# The date-time of RFC 3339 (section 5.6), and the one form that this project writes.
readonly RFC3339_RE='^[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])[Tt]([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]+)?([Zz]|[+-]([01][0-9]|2[0-3]):[0-5][0-9])$'
readonly PROJECT_RE='^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$'

usage() {
  [[ -z "${1:-}" ]] || echo "error: $1" >&2
  echo "usage: check-security-txt.sh (--file <path> | --url <address>) [--canonical <address>]" >&2
  echo "         [--same-as <path>] [--links] [--hsts-like <address>] [--expect-warnings <names>]" >&2
  echo "         [--min-days <n>] [--now <date>]" >&2
  exit 2
}

file="" url="" canonical="" same_as="" links=0 hsts_like="" expected="" min_days=30 now=""
while (($#)); do
  case "$1" in
    --links)
      links=1
      shift
      continue
      ;;
    --file | --url | --canonical | --same-as | --hsts-like | --expect-warnings | --min-days | --now)
      (($# >= 2)) || usage "$1 needs a value"
      ;;
    *) usage "unknown option: $1" ;;
  esac
  case "$1" in
    --file) file="$2" ;;
    --url) url="$2" ;;
    --canonical) canonical="$2" ;;
    --same-as) same_as="$2" ;;
    --hsts-like) hsts_like="$2" ;;
    --expect-warnings) expected="$2" ;;
    --min-days) min_days="$2" ;;
    --now) now="$2" ;;
  esac
  shift 2
done
if [[ -n "$file" && -n "$url" ]] || [[ -z "$file" && -z "$url" ]]; then
  usage "give either --file or --url"
fi
[[ -z "$file" || (-f "$file" && -r "$file") ]] || usage "cannot read $file"
[[ -z "$same_as" || (-f "$same_as" && -r "$same_as") ]] || usage "cannot read $same_as"
[[ -z "$hsts_like" || -n "$url" ]] || usage "--hsts-like needs --url"
[[ "$min_days" =~ ^[0-9]+$ ]] || usage "--min-days is not a number of days"
if [[ -n "$now" ]]; then
  now_s="$(date -u -d "$now" +%s 2>/dev/null)" || usage "--now is not a date"
else
  now_s="$(date -u +%s)"
fi
IFS=, read -ra names <<<"$expected"
for name in "${names[@]}"; do
  [[ -z "$name" || "$WARNINGS" == *" $name "* ]] || usage "unknown warning in --expect-warnings: $name"
done

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
failures=0
warnings=()
parsed=0
has_mailto=0
other_mailbox=0
canonicals=()
addresses=()
expires_lines=()
declare -A count=()

# rfc <section> <text>: a requirement of RFC 9116 is not met.
rfc() {
  echo "FAIL  RFC 9116 $1: $2"
  failures=$((failures + 1))
}
# rule <text>: a rule of this project (ADR-0048) is not met.
rule() {
  echo "FAIL  ADR-0048 rule: $1"
  failures=$((failures + 1))
}
# warn <name> <text>: a recommendation of RFC 9116 that ADR-0048 lists as an exception is not met.
warn() {
  echo "WARN  $1: $2"
  warnings+=("$1")
}
# sorted <names separated by commas>: the names sorted, without repeats, joined by commas, or none.
sorted() {
  local list
  list="$(tr ',' '\n' <<<"$1" | sed '/^$/d' | sort -u | paste -sd, -)"
  echo "${list:-none}"
}
# clean <text>: the text with harmless characters only, at most 100 of them, for a public issue.
clean() { tr -cd 'A-Za-z0-9=;,./: _-' <<<"$1" | cut -c1-100; }
# fetch <address> <body file> <header file> [curl option...]: one request; prints "<status> <final address>".
fetch() {
  local address="$1" body="$2" headers="$3"
  shift 3
  "$curl" --silent --show-error --proto '=https' --max-time 20 "$@" \
    --output "$body" --dump-header "$headers" --write-out '%{http_code} %{url_effective}' "$address"
}
# header <name> <header file>: the value of the last header of that name, without white space at its ends.
header() {
  { grep -i "^$1:" "$2" || true; } | tail -n 1 | cut -d: -f2- | tr -d '\r' | sed 's/^[[:blank:]]*//; s/[[:blank:]]*$//'
}
# host <address>: the host of an address, in small letters.
host() {
  local rest="${1#*://}"
  rest="${rest%%[/?#]*}"
  rest="${rest##*@}"
  rest="${rest%%:*}"
  echo "${rest,,}"
}

# fetch_file: fetch the file of --url without following a redirect, and check the answer (RFC 9116, section 3).
fetch_file() {
  local answer status=0 code type
  if [[ "${url,,}" != https://* ]]; then
    rfc 3 "$url is not an https address"
    return 1
  fi
  answer="$(fetch "$url" "$work/body" "$work/headers" --max-filesize 1048576 2>/dev/null)" || status=$?
  if ((status)); then
    rfc 3 "$url could not be fetched (curl exit $status)"
    return 1
  fi
  code="${answer%% *}"
  case "$code" in
    200) ;;
    3??)
      rule "$url answers $code, a redirect: RFC 9116 3 and 5.2 allow one, ADR-0048 serves the file itself"
      return 1
      ;;
    *)
      rfc 3 "$url answers $(clean "$code"), not 200"
      return 1
      ;;
  esac
  type="$(header Content-Type "$work/headers")"
  if [[ "$(tr -d ' \t' <<<"${type,,}")" != 'text/plain;charset=utf-8' ]]; then
    rfc 3 "the type is '$(clean "$type")', not text/plain; charset=utf-8"
  fi
}

# check_file: the bytes of the file, then its lines; parsed is 1 when every line was read.
check_file() {
  local bytes lines
  bytes="$(wc -c <"$file")"
  lines="$(grep -c '' "$file" || true)"
  if ((bytes > MAX_BYTES)); then
    rule "the file has $bytes bytes, more than $MAX_BYTES (RFC 9116 5.4)"
    return
  fi
  if ((lines > MAX_LINES)); then
    rule "the file has $lines lines, more than $MAX_LINES (RFC 9116 5.4)"
    return
  fi
  if [[ "$(head -c 3 "$file" | od -An -tx1 | tr -d ' \n')" == efbbbf ]]; then
    rfc 4 "the file begins with a byte order mark"
  fi
  if ! iconv -f UTF-8 -t UTF-8 "$file" >/dev/null 2>&1; then
    rfc 4 "the file is not UTF-8"
  elif (($(tr -d '\000-\177' <"$file" | wc -c))); then
    rule "the file has bytes outside ASCII"
  fi
  # Every control character but the tab, the line feed and the carriage return (RFC 9116, section 4).
  if (($(tr -cd '\000-\010\013\014\016-\037\177' <"$file" | wc -c))); then
    rfc 4 "the file has a control character"
  fi
  if ((bytes)) && [[ "$(tail -c 1 "$file" | od -An -tx1 | tr -d ' \n')" != 0a ]]; then
    rfc 2.2 "the last line has no line end"
  fi
  if grep -q '^-----BEGIN PGP SIGNED MESSAGE-----' "$file"; then
    rule "the file is signed, and reading a signed file is not built yet (exception E1)"
    return
  fi
  read_lines
  parsed=1
}

# read_lines: each line is empty, a comment or a field of the registry (RFC 9116, sections 2 and 4).
read_lines() {
  local number=0 line key value
  while IFS= read -r line || [[ -n "$line" ]]; do
    number=$((number + 1))
    if [[ "$line" == *$'\r'?* ]]; then
      rfc 2.2 "line $number has a carriage return that does not end it"
      continue
    fi
    # A line ends with white space, a carriage return and a line feed (eol, RFC 9116, section 4).
    line="${line%$'\r'}"
    line="${line%"${line##*[![:blank:]]}"}"
    if [[ -z "$line" || "$line" == '#'* ]]; then
      continue
    fi
    if [[ ! "$line" =~ $FIELD_RE ]]; then
      rfc 4 "line $number is neither empty, a comment nor a field (a name, a colon, one space and a value)"
      continue
    fi
    key="${BASH_REMATCH[1],,}"
    value="${BASH_REMATCH[2]}"
    if ((${#line} > MAX_FIELD)); then
      rule "the field on line $number has ${#line} characters, more than $MAX_FIELD (RFC 9116 5.4)"
    fi
    if [[ "$REGISTRY" != *" $key "* ]]; then
      rule "the field $(tr -cd 'A-Za-z0-9-' <<<"${BASH_REMATCH[1]}" | cut -c1-40) on line $number is not in the registry of IANA"
      continue
    fi
    count[$key]=$((${count[$key]:-0} + 1))
    check_value "$key" "$value" "$number"
  done <"$file"
}

# check_value <key> <value> <line>: the value of one field of the registry.
check_value() {
  local key="$1" value="$2" number="$3" field="${FIELD_NAME[$1]}" scheme mailbox
  case "$key" in
    acknowledgments | canonical | contact | encryption | hiring | policy)
      if [[ ! "$value" =~ $URI_RE ]]; then
        rfc "${FIELD_SECTION[$key]}" "$field on line $number is not a URI"
        return
      fi
      scheme="${value%%:*}"
      scheme="${scheme,,}"
      if [[ "$scheme" == http || ("$scheme" == https && "${value,,}" != https://*) ]]; then
        rfc "${FIELD_SECTION[$key]}" "$field on line $number is a web address that does not begin with https://"
      elif [[ "$scheme" == https ]]; then
        addresses+=("$number $field $value")
      fi
      ;;
  esac
  case "$key" in
    contact)
      case "$scheme" in
        https) ;;
        mailto)
          has_mailto=1
          mailbox="${value#*:}"
          mailbox="${mailbox%%@*}"
          if [[ "${mailbox,,}" != security ]]; then
            other_mailbox=1
          fi
          ;;
        *) rule "Contact on line $number is $(clean "$scheme" | tr -cd 'a-z0-9+.-'):, neither https: nor mailto:" ;;
      esac
      ;;
    canonical) canonicals+=("$value") ;;
    expires) expires_lines+=("$number $value") ;;
    preferred-languages)
      if [[ ! "$value" =~ $LANGUAGES_RE ]]; then
        rfc 2.5.8 "Preferred-Languages on line $number is not a list of language tags"
      fi
      ;;
    bug-bounty)
      if [[ "$value" != True && "$value" != False ]]; then
        rule "Bug-Bounty on line $number is neither True nor False"
      fi
      ;;
  esac
}

# check_expires <line> <value>: a date and time of RFC 3339 in the form of this project, within the window.
check_expires() {
  local expires_s ahead days
  if [[ ! "$2" =~ $RFC3339_RE ]] || ! expires_s="$(date -u -d "${2^^}" +%s 2>/dev/null)"; then
    rfc 2.5.5 "Expires on line $1 is not a date and time of RFC 3339"
    return
  fi
  if [[ ! "$2" =~ $PROJECT_RE ]]; then
    rule "Expires is not written as YYYY-MM-DDThh:mm:ssZ"
  fi
  ahead=$((expires_s - now_s))
  days=$((ahead / DAY))
  if ((ahead <= 0)); then
    rfc 2.5.5 "Expires is past"
  elif ((ahead > MAX_DAYS * DAY)); then
    rule "Expires is $days days ahead, more than $MAX_DAYS"
  elif ((ahead < min_days * DAY)); then
    rule "Expires is $days days ahead, fewer than $min_days"
  fi
}

# check_counts: the fields that must appear, and how often (RFC 9116, sections 2.5.3, 2.5.5 and 2.5.8;
# ADR-0048 for Canonical, Policy and Bug-Bounty).
check_counts() {
  if ((${count[contact]:-0} == 0)); then
    rfc 2.5.3 "no Contact"
  fi
  case "${count[expires]:-0}" in
    0) rfc 2.5.5 "no Expires" ;;
    1) check_expires "${expires_lines[0]%% *}" "${expires_lines[0]#* }" ;;
    *) rfc 2.5.5 "Expires appears ${count[expires]} times" ;;
  esac
  if ((${count["preferred-languages"]:-0} > 1)); then
    rfc 2.5.8 "Preferred-Languages appears ${count["preferred-languages"]} times"
  fi
  if ((${count[canonical]:-0} != 1)); then
    rule "Canonical appears ${count[canonical]:-0} times, not once"
  elif [[ -n "$canonical" && "${canonicals[0]}" != "$canonical" ]]; then
    rule "Canonical is not $canonical"
  fi
  if ((${count[policy]:-0} == 0)); then
    rule "no Policy"
  fi
  if ((${count["bug-bounty"]:-0} > 1)); then
    rule "Bug-Bounty appears ${count["bug-bounty"]} times; the registry of IANA allows it once"
  fi
}

# check_warnings: the recommendations of RFC 9116 that ADR-0048 lists as the exceptions E1, E2 and E3.
check_warnings() {
  warn unsigned "no signature (RFC 9116 2.3; exception E1 of ADR-0048)"
  if ((has_mailto && ${count[encryption]:-0} == 0)); then
    warn no-encryption "a mailto: Contact and no Encryption (RFC 9116 2.5.3 and 5.7; exception E3 of ADR-0048)"
  fi
  if ((other_mailbox)); then
    warn no-role-address "a mailto: Contact whose mailbox is not security (RFC 9116 2.5.3, RFC 2142; exception E2 of ADR-0048)"
  fi
}

# check_named: a file fetched from an address that its Canonical fields do not name should not be trusted
# (RFC 9116, section 2.5.2).
check_named() {
  local each
  for each in "${canonicals[@]}"; do
    if [[ "$each" == "$url" ]]; then
      return
    fi
  done
  if ((${#canonicals[@]})); then
    rule "the Canonical fields do not name $url (RFC 9116 2.5.2: such a file should not be trusted)"
  fi
}

# check_links: each https address in the file answers below 400 and ends on its own host (RFC 9116, 5.1 and 5.2).
check_links() {
  local entry number field address answer status code final
  for entry in "${addresses[@]}"; do
    read -r number field address <<<"$entry"
    status=0
    answer="$(fetch "$address" /dev/null "$work/link-headers" --location --max-redirs 5 --proto-redir '=https' \
      2>/dev/null)" || status=$?
    if ((status)); then
      rule "the address on line $number ($field) could not be fetched (curl exit $status)"
      continue
    fi
    code="${answer%% *}"
    final="${answer#* }"
    if [[ ! "$code" =~ ^[0-9]{3}$ ]] || ((10#$code < 200 || 10#$code >= 400)); then
      rule "the address on line $number ($field) answers $(clean "$code")"
    fi
    if [[ "$(host "$final")" != "$(host "$address")" ]]; then
      rule "the address on line $number ($field) ends on another host: $(clean "$(host "$final")")"
    fi
  done
}

# check_hsts: the answer sends the Strict-Transport-Security of the address of --hsts-like.
check_hsts() {
  local status=0 mine theirs
  fetch "$hsts_like" /dev/null "$work/hsts-headers" >/dev/null 2>&1 || status=$?
  if ((status)); then
    rule "$hsts_like could not be fetched (curl exit $status)"
    return
  fi
  mine="$(header Strict-Transport-Security "$work/headers")"
  theirs="$(header Strict-Transport-Security "$work/hsts-headers")"
  if [[ "$mine" != "$theirs" ]]; then
    rule "Strict-Transport-Security is '$(clean "$mine")', and $hsts_like sends '$(clean "$theirs")'"
  fi
}

# finish: print the verdict and exit with it.
finish() {
  if ((failures)); then
    echo "RESULT: $failures failed for $what"
    exit 1
  fi
  echo "PASS  $what meets RFC 9116 and the rules of ADR-0048 (warnings: $(IFS=, && sorted "${warnings[*]}"))"
  exit 0
}

if [[ -n "$url" ]]; then
  what="$url"
  file="$work/body"
  fetch_file || finish
else
  what="$file"
fi
check_file
if ((parsed)); then
  check_counts
  check_warnings
  if [[ -n "$url" ]]; then
    check_named
  fi
fi
if [[ -n "$same_as" ]] && ! cmp -s "$file" "$same_as"; then
  rule "the body differs from $same_as"
fi
if ((links)); then
  check_links
fi
if [[ -n "$hsts_like" ]]; then
  check_hsts
fi
if ((parsed)); then
  found="$(IFS=, && sorted "${warnings[*]}")"
  if [[ "$found" != "$(sorted "$expected")" ]]; then
    rule "the warnings are $found, not $(sorted "$expected") (--expect-warnings)"
  fi
fi
finish
