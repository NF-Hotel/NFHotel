#!/usr/bin/env bash
# Check the guest sign-in of MIL-018 (ADR-0047) on a deployed NF Hotel site, the way a
# browser uses it: the sign-in page answers; a wrong email is refused and sets no cookie; the
# booking number with the right email signs the guest in with the cookie nfhotel.guest, which
# is Secure, HttpOnly, SameSite=Lax, sent only to /my-booking and ends with the session; the
# own booking opens; with --cancel the booking is cancelled; the sign-out removes the cookie,
# and the own booking then asks for the sign-in again. Task 7 of MIL-018 runs it after the
# deploy, with a booking made for the test on /book (criterion 5).
#
# Each run makes one failed attempt for the booking number (the wrong email). Five failures
# within 15 minutes stop the number for 15 minutes (HTTP 429), so wait between many runs.
#
# Usage: check-guest-login.sh [--cancel] [--insecure] <site> <booking number>
#   <site>            the address of the site, for example https://nfhotel.devmatiker.com
#   <booking number>  the number of the test booking as the receipt shows it (FLZ-000123)
#   --cancel          also cancel the booking, which cannot be undone; without it the
#                     booking stays as it is
#   --insecure        accept a certificate that is not trusted, such as the development
#                     certificate of a VM; the cookie must still be Secure
# Env:   GUEST_EMAIL  the email of the booking; when it is not set, the script asks for it,
#                     so the email is never on a command line
# Exit:  0 every check passed; 1 a check failed or the email is missing; 2 usage error
# Needs: bash 4+, curl, grep, sed, mktemp

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=ops/scripts/lib/common.sh
. "$ROOT/scripts/lib/common.sh"
readonly COOKIE=nfhotel.guest
readonly REFUSAL="find that booking"
readonly CANCELLED="Your booking is cancelled."

is_cancel=0
is_insecure=0
positional=()
while (($#)); do
  case "$1" in
    --cancel) is_cancel=1 ;;
    --insecure) is_insecure=1 ;;
    -h | --help)
      usage
      exit 0
      ;;
    -*)
      usage >&2
      exit 2
      ;;
    *) positional+=("$1") ;;
  esac
  shift
done
((${#positional[@]} == 2)) || {
  usage >&2
  exit 2
}
site="${positional[0]%/}"
number="${positional[1]}"
[[ "$site" =~ ^https?://[^/]+$ ]] || die "the site must be an address such as https://nfhotel.devmatiker.com, not $site"
email="${GUEST_EMAIL:-}"
if [[ -z "$email" && -t 0 ]]; then
  read -r -p "Email of booking $number: " email
fi
[[ -n "$email" ]] || die "set GUEST_EMAIL to the email of the booking, or run the script in a terminal"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
printf '%s' "$email" >"$work/email"
printf 'nobody-%s@example.invalid' "$RANDOM" >"$work/wrong"
curl_options=(--silent --show-error --max-time 20 --cookie "$work/jar" --cookie-jar "$work/jar")
((is_insecure)) && curl_options+=(--insecure)
failures=0

check() {
  if [[ "$3" == "$2" ]]; then
    echo "PASS  $1"
  else
    echo "FAIL  $1 (expected '$2', got '$3')"
    failures=$((failures + 1))
  fi
}

# fetch <path> <file>: GET a page into the file with the cookies of the run; print the status.
fetch() {
  curl "${curl_options[@]}" --output "$2" --write-out '%{http_code}' "$site$1" || echo "no answer"
}

# post <path> <name> <field>...: POST a form, each field name=value, or name@file to take the
# value from a file; keep the headers and the page as <name>.headers and <name>.html, and print
# the status, followed by the path that a redirect points to.
post() {
  local path="$1" name="$2" field answer code location
  local fields=()
  shift 2
  for field in "$@"; do
    fields+=(--data-urlencode "$field")
  done
  answer="$(curl "${curl_options[@]}" --dump-header "$work/$name.headers" --output "$work/$name.html" \
    --write-out '%{http_code} %{redirect_url}' "${fields[@]}" "$site$path")" || answer="no-answer"
  read -r code location <<<"$answer"
  echo "$code${location:+ ${location#"$site"}}"
}

# hidden_value <file> <name>: the value of the hidden input of that name on the page. A missing
# input gives an empty value instead of an error (|| true), and the check of the form reports it.
hidden_value() {
  { grep -o "<input[^>]*name=\"$2\"[^>]*>" "$1" || true; } | head -n 1 | sed -n 's/.* value="\([^"]*\)".*/\1/p'
}

# input_name <file> <id>: the name of the input with that id on the page, or empty, as above.
input_name() {
  { grep -o "<input[^>]*id=\"$2\"[^>]*>" "$1" || true; } | head -n 1 | sed -n 's/.* name="\([^"]*\)".*/\1/p'
}

# cookie_attributes <name>: the attributes of the guest cookie that the answer <name> sets,
# in lower case and in a fixed order, or "none".
cookie_attributes() {
  local line
  # No such line is an answer here, not an error: the check of the caller expects "none" then.
  line="$(grep -i "^set-cookie: $COOKIE=" "$work/$1.headers" | head -n 1 | tr '[:upper:]' '[:lower:]')" || true
  [[ -n "$line" ]] || {
    echo none
    return
  }
  local attribute found=()
  for attribute in secure httponly samesite=lax path=/my-booking expires=; do
    [[ "$line" == *"$attribute"* ]] && found+=("$attribute")
  done
  echo "${found[*]}"
}

echo "==> the guest sign-in on $site for booking $number"
check "the sign-in page answers" 200 "$(fetch /booking "$work/signin.html")"
token="$(hidden_value "$work/signin.html" __RequestVerificationToken)"
handler="$(hidden_value "$work/signin.html" _handler)"
number_field="$(input_name "$work/signin.html" booking-number)"
email_field="$(input_name "$work/signin.html" booking-email)"
check "the sign-in page has its form" "guest-sign-in yes" \
  "$handler $([[ -n "$token" && -n "$number_field" && -n "$email_field" ]] && echo yes)"
form=("_handler=$handler" "__RequestVerificationToken=$token" "$number_field=$number")

check "a wrong email is refused on the page" 200 "$(post /booking wrong "${form[@]}" "$email_field@$work/wrong")"
check "the refusal says that the booking was not found" yes "$(grep -qF "$REFUSAL" "$work/wrong.html" && echo yes)"
check "a wrong email sets no guest cookie" none "$(cookie_attributes wrong)"

check "the number and the email sign the guest in" "302 /my-booking" \
  "$(post /booking right "${form[@]}" "$email_field@$work/email")"
check "the guest cookie is Secure, HttpOnly, SameSite=Lax, for /my-booking, and ends" \
  "secure httponly samesite=lax path=/my-booking expires=" "$(cookie_attributes right)"

check "the own booking opens" 200 "$(fetch /my-booking "$work/own.html")"
check "the own booking shows its number" yes "$(grep -qF "$number" "$work/own.html" && echo yes)"
token="$(hidden_value "$work/own.html" __RequestVerificationToken)"

if ((is_cancel)); then
  check "the guest cancels the booking" "302 /my-booking?done=cancelled" \
    "$(post /my-booking cancel _handler=guest-cancel "__RequestVerificationToken=$token")"
  check "the own booking says that it is cancelled" 200 "$(fetch '/my-booking?done=cancelled' "$work/cancelled.html")"
  check "the notice of the cancellation is shown" yes "$(grep -qF "$CANCELLED" "$work/cancelled.html" && echo yes)"
fi

check "the guest signs out" "302 /booking" "$(post /my-booking/sign-out signout "__RequestVerificationToken=$token")"
check "the sign-out removes the guest cookie" yes \
  "$(grep -i "^set-cookie: $COOKIE=;" "$work/signout.headers" | grep -qi 'expires=thu, 01 jan 1970' && echo yes)"
check "after the sign-out the own booking asks for the sign-in" 302 "$(fetch /my-booking "$work/after.html")"

((failures == 0)) || die "$failures check(s) failed"
echo "==> the guest sign-in works on $site"
