# ADR-0050 Where the rules of the booking input live

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0050 |
| CrossReference | [MIL-086], [MIL-085], [ADR-0002], [ADR-0004], [ADR-0049], [MIL-067], [MIL-077], [MIL-087], [MIL-088], [UC-001], [UC-010], [BC-001], [RC-420] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-086, written by the AI before any code<br>Go in RC-420; accepted by the AI as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19 ("Du skal tage beslutningerne for mig") | [30b3aae] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-086] lists eight decisions and asks this record to fix where the rules live, how the page and
the API share them, where the table is kept, and any number that the reading of the code changes, with the
alternatives and the reason each lost. S01 asked in chat on 2026-11-19 for better validation of the input fields, for
no booking more than five years ahead and for protection against cross-site scripting (XSS) and SQL injection, and
handed the decisions to the AI ("Du skal tage beslutningerne for mig").

**The problem**, as the code after [MIL-085] has it (read on 2026-11-19):

- A field of a guest is checked in up to three places. The domain holds attributes on `Guest` (`Required`,
  `StringLength`, and for the email a pattern that only asks for an `@` and a `.` somewhere, so spaces and line breaks
  pass) and checks them in `Guest.Create` and `Guest.UpdateDetails` ([ADR-0002]). `GuestService.Validate` builds a
  guest to read those codes, and the pages run it in their own process before a call. The booking page trims the six
  fields first; the booking form and the guest form of the back office do not; the domain never trims. Nothing brings
  text to one Unicode form.
- The phone number has no format, the country is free text of up to 100 characters, and the passport number has
  only a length (50). The country goes unchanged to the market analysis service, whose artificial intelligence (AI)
  writes texts for the back office.
- The arrival may not lie before today. Beyond that, the only limit on a date is the range 1900 to 2199 of the API
  (`InputRules`, [MIL-077]); no rule limits the number of nights or how far ahead a stay lies.
- `CreatePublicBookingRequest.Guests` takes any whole number from 1; only the page offers 1 to 4.
- The API has no length limit on a text that carries no attribute, keeps Kestrel's default body limit of
  30,000,000 bytes, ignores a field that its type does not have, and writes no log line when it refuses a value.
  nginx keeps its default body limit of 1 MB, and every location gets read and send timeouts of one hour from
  `snippets/proxy.conf`, which only the web socket of the pages needs.
- `BookingService` saves a new guest before `Booking.Create` checks the dates, so a booking refused by a rule of its
  dates leaves the new guest behind.
- Three messages of the infrastructure are in Danish.

**The data that exists.** The market data of the hotel (`src/NFHotel.DataAnalysis/data/nf_hotel_bookings.csv`,
40,060 bookings, seeded into the database by `seed-market-data`) holds the country of a guest as the English short
name of ISO 3166-1: "Portugal" 94 times, "United Kingdom of Great Britain and Northern Ireland (the)" 29, "Spain" 23,
"Ireland" 9, "United States of America (the)" 8, seven other countries 1 to 3 times, and "Unknown" for the other
39,886. The developer seed holds Danish names ("Danmark", "Norge"), and the tests mostly "Denmark".

**What the texts say** (read on 2026-11-19; ASVS is the Application Security Verification Standard of the Open
Worldwide Application Security Project, OWASP):

| Source | What it says |
| --- | --- |
| OWASP ASVS 5.0, 2.1.1, 2.2.1, 2.2.2 | Document the rules of each input; validate against a positive list; validate on a trusted service layer, not only in the client |
| OWASP ASVS 5.0, 15.3.3 | A request carries only the fields that the action expects (mass assignment) |
| OWASP ASVS 5.0, 16.3.3 | Log attempts to get around the security controls, such as input validation |
| OWASP API Security Top 10 2023, API4 | A maximum size for every parameter and payload |
| OWASP Input Validation Cheat Sheet, email | Two parts around one `@`; no dangerous characters such as quotes or backticks; a domain of letters, digits, hyphens and dots; a local part of reasonable length |
| OWASP Cross Site Scripting Prevention Cheat Sheet | Encoding at the output is the defence; cleaning the input is not |
| RFC 5321, 4.5.3.1.1 | The local part of an address is at most 64 octets |
| .NET, `DateOnly.AddYears` | When the day does not exist in the target year (29 February), the last day of that month is used |
| .NET, `System.Net.Mail.MailAddress.TryCreate` | Reads an address of RFC 5322, with or without a display name, without throwing |

**Forces.**

- The API is the authority ([ADR-0004]) and is called without the page: the hostile-input tests of [MIL-077] do so
  for every endpoint, and a stranger can.
- The page should tell a guest what is wrong before a round trip, and must not tell it differently from the API.
- `NFHotel.Domain` references no package; the base class library is allowed there ([ADR-0002]).
- A field has one meaning in the whole flow: the guest fields of the public booking, of the booking form and of the
  guest form of the back office are the same six fields (`GuestFields`).
- Rules join fields: the dates against the clock of the hotel, the number of nights on the public site only.
- Stored guests must keep working ([MIL-086] decision 8), and the market analysis groups by the stored country.
- Other callers of the API have contracts of their own: the benchmark service posts a run with model outputs of up to
  1,000,000 characters each, and the app of ADR-0015 calls a few endpoints.

**Options considered:**

| Option | Why it lost, or why it was chosen |
| --- | --- |
| A. The rules on the pages only | Lost: the API is called without the page; ASVS 2.2.2 asks for the check on the server |
| B. A validation library in the application (for example FluentValidation) | Lost: one more package to patch for rules of a few lines each; the entity could not use it without a package in the domain ([ADR-0002] option 4), so the entity and the request would hold the rules twice |
| C. Data annotations as today, with the pattern made stricter | Lost: a pattern cannot read an address as a parser does, cannot join fields (the dates, the 30 nights) or normalise, and the page would keep its own date checks |
| D. One implementation per rule in the domain: the field rules as methods of a static class, called by custom attributes on the entity, and the joined rules as methods of a second static class; the API and the pages call the same methods | Chosen |
| E. A rules service behind a port, injected into the services and the pages | Lost: the rules read nothing and write nothing; an injected service adds wiring and a place to mock a rule away, for no gain over a static method that the tests call directly |

## Decision

Each rule of the booking input is written once, in the domain: the field rules of a guest are methods of the static
class `GuestFieldRules`, which the attributes of `Guest` call, and the rules that join the dates, the clock and the
party are methods of the static class `StayRules`. The API is the authority and answers the code of each broken
rule, the pages call the same methods for their early feedback, and the table "Validation rules of the booking flow"
in `docs/refactor/BusinessRules.md` names every field of the flow with its rule, its code and its method, which a test
holds against the request types.

The details, numbered as the decisions of [MIL-086]:

1. **The table.** The section "Validation rules of the booking flow" of the rule ledger has one row per field of the
   request types of the flow (`CreatePublicBookingRequest`, `GuestFields`, `GuestSignInRequest`,
   `GuestRescheduleRequest`, `CreateBookingRequest`, `RescheduleBookingRequest`, `CreateGuestRequest`,
   `UpdateGuestRequest`, `BookingOverviewQuery`), of the query values of the available rooms and of the guest search,
   and of the two addresses of the booking page (`/book?guests=`, `/book?checkIn=&checkOut=&guests=`, [MIL-067]). Its
   columns: field, type, required, length, characters or values, normalisation, joined rules, code, source, and the
   method that implements it. A test of the API reads the table and fails when a field of a request type of the flow
   has no row, when a row names a field that does not exist, when a text field has no length rule, when a request
   type of the flow lets an unknown field through, and when a list field has no largest size.
2. **The fields.** The numbers of [MIL-086] stand; this record adds what the code needs to say them exactly.
   - First and last name: 1 to 100 characters; letters and marks of any script (Unicode categories L and M), the
     space U+0020, the apostrophes U+0027 and U+2019 (the second is what a phone types), the hyphen-minus and the
     full stop; at least one letter. New codes `guest.first_name_invalid` and `guest.last_name_invalid`.
   - Email: 1 to 100 characters; `MailAddress.TryCreate` reads it, its address equals the text and it has no display
     name; the local part has at most 64 characters and no white space, control character, `"`, `` ` ``, `<`, `>`
     or `\`; the domain is two or more labels of letters, digits and hyphens, separated by dots, none empty, longer
     than 63 characters or starting or ending with a hyphen. The comparison at the sign-in stays without letter case
     ([ADR-0049]). The codes stay `guest.email_required`, `guest.email_too_long` and `guest.email_invalid`.
   - Phone number: 1 to 50 characters; an optional `+` first, then digits 0 to 9 with spaces, hyphens and round
     brackets between them; 7 to 15 digits; it starts after the plus with a digit or `(` and ends with a digit or
     `)`. New code `guest.phone_number_invalid`.
   - Country: one of the 249 names of the class `Countries`, exactly. The stored value is the English short name of
     ISO 3166-1, because that is what the data of the hotel holds and what the market analysis groups by. New code
     `guest.country_invalid`. The names were read from the column `iso.name.en` of the R package countrycode on
     2026-11-19 and corrected in eight entries: five that hold a replacement character in that copy (AX "Åland
     Islands", BL "Saint Barthélemy", CI "Côte d'Ivoire", CW "Curaçao", RE "Réunion"); GB, which that copy shortens
     and which the data of the hotel and the United Nations Group of Experts on Geographical Names hold as "United
     Kingdom of Great Britain and Northern Ireland (the)"; TR "Türkiye", the ISO change effective 2022-07-11 that the
     language subtag registry of the Internet Assigned Numbers Authority (IANA) records; and NL "Netherlands (Kingdom
     of the)", the short name that the United Nations recorded on 2023-03-03. The 249 alpha-2 codes are those of the
     time zone database (`iso3166.tab`, "current as of ISO/TC 46 N1127 (2024-02-29)").
   - Passport number: optional; empty after normalisation means none; otherwise 1 to 20 ASCII letters and digits.
     The limit drops from 50 to 20 (`guest.passport_number_too_long`); the column is encrypted text, so no migration
     follows. New code `guest.passport_number_invalid`.
   - Guests on the public site: 1 to 4. The attribute `Range(1, int.MaxValue)` goes; `StayRules.CheckPublicParty`
     answers the new code `booking.guests_out_of_range`, so the refusal names the field instead of
     `api.invalid_request`.
   - Room size: a member of `RoomSize`. The binding refuses any other value with `api.invalid_request` ([MIL-077]);
     the table says so, because the binding answers before any rule of a field runs.
   - Booking reference at the sign-in: as [ADR-0049] decided; a reference that is not one, and an email over its
     length, get the answer of a failed sign-in, as a wrong email does.
3. **The dates.** The arrival is not before today on the clock of the hotel (`IClock`, unchanged); the departure is
   after the arrival (`DateRange`, unchanged); the departure is at most `today.AddYears(5)`, so a stay booked on
   29 February may end on 28 February five years later. `StayRules.CheckPeriod` answers `booking.start_date_in_past`
   or the new `booking.end_date_too_far_ahead`; `Booking.Create` and `Booking.Reschedule` call it, so the rule holds
   for the public booking, the move by a guest, and the booking and the move by staff. `BookingService` calls it
   before it saves a new guest. On the public site, the booking and the move by a guest, `StayRules.CheckPublicNights`
   refuses more than 30 nights with the new code `booking.stay_too_long`; staff can book 31 nights and more within
   the five years. The range 1900 to 2199 of `InputRules` stays as the outer bound of any date.
4. **Normal form and size.**
   - `GuestFieldRules.Normalize` trims white space at both ends and brings the text to Unicode normal form C. A text
     with a lone surrogate is left as it is (normalising it would throw), and its rule refuses it. `Guest.Create` and
     `Guest.UpdateDetails` normalise each field before the attributes run, so the stored text is the normalised one.
     The sign-in normalises the email before it compares, and the two searches normalise their text. A password is
     never trimmed or normalised; it is not part of the flow.
   - `InputRules` refuses, at every endpoint, a text of more than 1,000 characters whose property or parameter
     carries no `StringLength` or `MaxLength`. The texts of a benchmark run get the `StringLength` that their entity
     and their columns already hold, so the intake accepts what it accepted.
   - Kestrel takes at most 65,536 bytes of a request body at every endpoint. The intake of a benchmark run
     (`BenchmarkController.PostResultAsync`) keeps 30,000,000 bytes with `RequestSizeLimit`: a run carries model
     outputs and comes from the benchmark service on the internal network, not through nginx. A middleware refuses a
     body whose declared length is over the limit of its endpoint before the body is read, and the handler of
     exceptions answers a body that grows over it; both answer 413 with the new code `api.request_too_large`.
     nginx takes at most 64 KB at the public listener (`client_max_body_size 64k`); the listener on port 443 of the
     local network keeps the default of nginx (1 MB), behind which the API holds its own limit.
   - Each request type of the flow carries `JsonUnmappedMemberHandling(Disallow)`, so a field that the type does not
     have makes the binding fail with 400 and `api.invalid_request`. The other request types keep their binding:
     their callers have contracts that this milestone does not test.
   - No request type of the flow holds a list; the test of the table fails when one gets a list without a
     `MaxLength`.
5. **Markup and query text are data.** No rule strips or rewrites a value beyond the normal form: a value meets its
   rule or is refused. Razor encodes every value it writes, and no page writes the text of a guest as markup. The
   tests carry one shared list of hostile strings (script and image elements, event attributes, a `javascript:`
   address, SQL fragments, format strings, template expressions, line breaks with a mail header, `%` and `_`) through
   every text field: refused by the rule of the field, or stored unchanged, and shown as text on every page that
   shows the value.
6. **The answer and the event.** A refusal answers the codes and nothing else: 422 with every broken rule, 400 with
   `api.invalid_request` when the binding or `InputRules` refuses, 413 with `api.request_too_large`. Each such answer
   writes one security event, `input_refused`, at the level Warning in the category `NFHotel.Security`, with the
   method, the route template of the endpoint, the status and the codes: never a value, never the path or the query
   as sent. An always-run result filter of the API writes it for the answers of the controllers, and the body-limit
   middleware and the handler of exceptions for theirs. [MIL-087] adds its own kinds, the address of the caller and
   the counters to the same category.
7. **The timeouts.** `snippets/proxy.conf` gives every location 60 seconds; a location `/_blazor` on both listeners
   proxies as `/` does and keeps one hour, because the web socket of the pages lives there.
8. **Stored guests stay.** No data is rewritten. A guest stored before this change keeps its values until staff edit
   it; the edit must then meet the rules, so the staff choose the country from the list. The market analysis gets
   what is stored.

The three Danish messages of `UnitOfWork` and `DependencyInjection` become English, and so do the Danish comments of
the files that this change edits.

## Consequences

**Positive:**

- One implementation of each rule: the API, the entity and the pages call the same method, and a rule cannot be
  stricter on the page than at the API or the other way round.
- The table is the one place that says what a field accepts, and a test keeps it honest against the request types.
- A stay can no longer hold a room for decades, a stranger cannot send a party of thousands, and a body of megabytes
  is refused before it is read.
- Line breaks and display names cannot reach a mail header through the email ([MIL-088]); the country that reaches
  the analysis and its AI is one of 249 names, not text of a stranger.
- A refused value is visible in the log without the value, which [MIL-087] builds its alerts on.
- A refused booking no longer leaves a new guest behind when its dates break a rule.

**Negative:**

- Stored guests keep values that the rules now refuse ("Danmark", "Unknown", a phone without digits), and an edit of
  such a guest asks staff for a country from the list.
- The positive list of names refuses digits and symbols that a real name can hold in rare cases; such a guest calls
  the hotel, and staff enter the name without them.
- The country list must follow ISO 3166-1 by hand; a change of a name or a new code needs a change of the class.
- `System.Net.Mail` is a framework namespace in the domain, accepted here as [ADR-0002] accepted
  `System.ComponentModel.DataAnnotations`.
- The refusal of an unknown field holds for the flow only; the other endpoints still ignore one.
- A request over 64 KB to any endpoint but the benchmark intake is refused; a future endpoint that needs more must
  say so with its own limit.
- Quality attributes: security (positive lists, size and time limits, events) and maintainability (one place per
  rule) gain; usability gains on the pages (the same texts before the call); availability is unchanged, and
  performance efficiency gains slightly from the smaller bodies.

## Affected Artifacts

- [MIL-086] — the milestone that carries the decision
- [ADR-0002] — stands: the guest rules stay attributes of the entity; the attributes that check a format call
  `GuestFieldRules`, and the joined rules that an attribute cannot hold live in `StayRules`
- [ADR-0004] — stands: the API is the authority
- [ADR-0049] — unchanged: the reference and the email of the sign-in keep its answers
- [MIL-077] — `InputRules` gets the default length of a text, and the hostile-input tests keep passing
- [MIL-067] — the two addresses of the booking page are rows of the table
- [MIL-087] — the security events of this record are the first of its category
- [MIL-088] — the rule of the email guards the mail headers
- [UC-001] and [UC-010] — the steps that the new rules change
- [BC-001] — objective 43 and success criterion 47

---

[MIL-086]: ../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0002]: ./adr-0002-validation-attributes-on-entities.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0049]: ./adr-0049-booking-reference.md
[MIL-067]: ../milestones/mil-067-bad-input-on-the-web-pages.md
[MIL-077]: ../milestones/mil-077-hostile-input-against-the-api.md
[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[UC-001]: ../use-cases/uc-001/uc.md
[UC-010]: ../use-cases/uc-010/uc.md
[BC-001]: ../business-case.md
[RC-420]: ../sqa/reviews/rc-420-adr-0050.md
[30b3aae]: https://github.com/NF-Hotel/NFHotel/commit/30b3aaec46795a06f6df4336b23fe7c31cda3ac3
