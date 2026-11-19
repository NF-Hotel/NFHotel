# MIL-086 Booking input checked by one set of rules

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-086 |
| CrossReference | [BC-001], [US-001], [ADR-0004], [MIL-067], [MIL-077] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: planned at S01's request in chat on 2026-11-19 ("bedre validering på input felterne og laves så man ikke kan booke mere end 5 år frem", "sikret mod forskellige typer XSS samt SQL injection"); the decisions are the AI's for S01 ("Du skal tage beslutningerne for mig"), and the plan is accepted by the AI as stand-in after RC-411, Go, for a run in autonomous mode ("Når din plan er lavet skal Opus 5.5 lave det hele i automode") | [9f5c208] |

---

## Purpose

Decide that every value that reaches the booking flow is checked on the server against one written table of rules, that no booking lies more than five years ahead, and that markup and query text in a field stay text.

The code at 3b973c7 was read on 2026-11-19. What is weak:

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | The email of a guest only has to hold an `@` and a `.` somewhere; spaces and line breaks pass | `src/NFHotel.Domain/Entities/Guest.cs` (`EmailPattern`) | A booking with an address that no mail reaches; and, once the site sends mail (MIL-088), a line break in what becomes a mail header |
| 2 | The phone number has no format, and the country is free text of 100 characters, which goes on to the market analysis service, whose artificial intelligence (AI) writes texts for the back office | `Guest.cs`; `src/NFHotel.Infrastructure/Repositories/MarketAnalysisBookingQueries.cs` | The words of a stranger in front of an AI that writes for the Manager |
| 3 | The arrival may not lie before today; beyond that the only date limit is the year 2199 of the API. No stay is too long | `src/NFHotel.Domain/ValueObjects/DateRange.cs`, `Booking.cs`, `src/NFHotel.Api/Security/InputRules.cs` | One booking takes a room for a hundred years |
| 4 | The API takes any number of guests up to the largest whole number; only the page offers 1 to 4 | `src/NFHotel.Application/Dto/Bookings/CreatePublicBookingRequest.cs` | A caller of the API meets other rules than a guest of the page |
| 5 | The API has no limit on the length of a text and none of its own on the size of a request, and nginx gives every location timeouts of one hour | `src/NFHotel.Api/Security/InputGuardFilter.cs`, `src/NFHotel.Api/Program.cs`, `ops/deploy/nginx/snippets/proxy.conf` | Large and slow requests cost memory and connections |
| 6 | The rules of one field are in three places: the page, the service and the attributes of the domain; the page trims a text, the domain does not | `src/NFHotel.Web/Features/Booking/BookingFlowViewModel.cs`, `src/NFHotel.Application/UseCases/Guests/GuestService.cs`, `Guest.cs` | The three drift apart, and nobody can say in one place what a field accepts |
| 7 | Three messages of the infrastructure are in Danish | `src/NFHotel.Infrastructure/Persistence/UnitOfWork.cs`, `src/NFHotel.Infrastructure/DependencyInjection.cs` | The repository is in English |

What is sound and stays: every query goes through the entity framework with parameters, and the two searches of the back office escape their pattern; no page puts the text of a guest into markup, the charts encode their labels and the Markdown of the AI texts is rendered with HTML off; errors answer a code and no detail; the content security policy allows scripts from the site only; forms carry an antiforgery token; and every endpoint of the API is tested with hostile input ([MIL-067], [MIL-077]). This milestone adds rules and tests to that; it does not rebuild it.

Read for this plan on 2026-11-19: the Application Security Verification Standard (ASVS) 5.0 of the Open Worldwide Application Security Project (OWASP), sections V1.2 and V1.3 (injection and sanitisation), V2.1 and V2.2 (documented rules, validation on the server, positive lists), requirement 15.3.3 (no field that the action does not expect) and section V4.1; the OWASP cheat sheets on input validation, on cross-site scripting (XSS), on injection into the structured query language (SQL), on mass assignment and on email validation; and item API4:2023 of the OWASP API Security Top 10 (a maximum size for every parameter and payload).

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | One table, "Validation rules of the booking flow", names every field with its type, whether it is required, its length, its allowed characters or values, how it is normalised, the rules that join it to other fields, its error code and the source of the rule. The code has one implementation of each rule, which the API uses as the authority and the page for early feedback | ASVS 2.1.1, 2.1.2 and 2.2.2; finding 6 |
| 2 | Names: 1 to 100 characters after trimming, letters with their marks, spaces, apostrophe, hyphen and full stop. Email: one address of at most 100 characters, read by a maintained parser, without a display name, without spaces or line breaks, with a domain that has a dot; compared as today, without letter case. Phone: an optional plus sign and 7 to 15 digits, with spaces, hyphens and brackets allowed between them. Country: one of the countries of ISO 3166-1, chosen from a list. Passport number: optional, letters and digits, at most 20 characters. Guests: 1 to 4. Room size: one of the sizes that exist | Positive lists (ASVS 2.2.1); findings 1, 2 and 4. The decision record may change a number with its reason |
| 3 | Dates: the arrival is not before today on the clock of the hotel; the departure is after the arrival; the stay ends at most five years after today, counted in calendar years, for a guest and for staff, when a booking is made and when it is moved; on the public site a stay has at most 30 nights | S01 asked for five years. A longest stay closes finding 3 for the public site; staff can still book a long stay |
| 4 | Every text is trimmed and brought to Unicode normal form C before it is checked; a text without a rule of its own has at most 1,000 characters; a request body has at most 64 kilobytes, at the API and at the public listener; a request with a field that its type does not have is refused; a list has a largest size | ASVS 15.3.3 and API4:2023; finding 5 |
| 5 | Markup and query text are data. No rule strips or rewrites a value: a value meets its rule or is refused, and what is stored is shown encoded. The tests carry a list of hostile strings through every text field to the database and to every page that shows the value | The OWASP cheat sheets: encoding at the output and parameters in the query are the defence, and cleaning the input is not |
| 6 | A refused value gets an answer that names the field by a code and nothing else, and a security event in the log without the value | ASVS 16.3.3; the answer helps the guest and tells a prober nothing |
| 7 | The timeout of one hour stays only where the web socket of the pages needs it; every other location gets 60 seconds | Finding 5 |
| 8 | Guests that are stored already are not rewritten: the rules hold for what comes in from now on | A rule must not break the bookings that exist |

## Deliverable

- ADR-0050: where the rules live and how the page and the API share them, with the alternatives and the reason each lost.
- The table of the validation rules in `docs/refactor/BusinessRules.md`, and the business rules for the five years and the 30 nights.
- The rules in the domain and the application, used by the API and by the pages: the public booking, the guest sign-in, the move of a booking by a guest, the booking form and the guest form of the back office, and the two address forms of the site.
- The size and time limits at the API and in nginx.
- Tests, written first: at the limits of every field, one per kind of refused value, the hostile strings through every text field, the dates with a fixed clock, and the pages that show a value.
- Documents: the rule ledger, the test plan, the wiki page Security-Controls, the review records; the three Danish messages in English.
- The check on the live site after the deploy, with its evidence in `docs/sqa/evidence/mil-086/`.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0050 is Accepted after its review: one implementation of each rule, the place of the table, decisions 1 to 8 with any changed number and its reason, and the alternatives with the reason each lost | Yes | No |
| 2 | The table names every field of the request types of the booking flow, and a test fails when a request type of the flow has a field that the table does not name | Yes | No |
| 3 | For every field the tests cover the value at each limit, one below and one above, and one value of each refused kind; the API refuses each with the code of the field also when it is called without the page; a request with an unknown field is refused; a body over 64 kilobytes gets 413; a booking for 5 guests is refused | Yes | No |
| 4 | With a fixed clock: a stay that ends exactly five years after today is accepted and one that ends a day later is refused, for a guest and for staff, when made and when moved, also across 29 February; on the public site a stay of 30 nights is accepted and one of 31 refused, and staff can book 31; an arrival yesterday is refused | Yes | No |
| 5 | Each hostile string of the list (script elements, event attributes, `javascript:` addresses, SQL fragments, format strings, line breaks) in each text field is either refused by the rule of the field or stored unchanged, and every page that shows the value (public, guest and staff) shows it as text and creates no element from it; the two searches of the back office find a `%` and a `_` as themselves | Yes | No |
| 6 | The hostile-input tests and the test of the public endpoints pass; the baseline scan of the pull request reports no new alert; CodeQL reports no new alert in the changed code | Yes | No |
| 7 | The documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the coverage floor holds; every required check of the pull request is green | Yes | No |
| 8 | On the live site after the deploy: a stay that ends five years and a day ahead and a stay of 31 nights are refused with the text of the rule, the date fields of the booking page carry their earliest and latest day, and the evidence is in the evidence folder | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-067 | The refusal of bad input on the booking page and the staff pages, which this milestone extends |
| MIL-077 | The hostile-input tests of every endpoint of the API, which must keep passing |
| ADR-0004 | The API as the authority behind the web application |
| MIL-085 | The contracts of the public and guest operations as they are after the reference |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 43 and success criterion 47: the input of a booking is checked against one written set of rules | Criteria 2 to 5 and 8 |
| BC-001 objective 31: every endpoint of the API refuses hostile input | Criteria 3, 5 and 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-26. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0050 Where the rules of the booking input live | Write the decision record before any code: one implementation of each rule in the domain and the application, used by the API as the authority and by the pages for early feedback; where the table is kept; the eight decisions with any number that the reading of the code changes, and its reason; the alternatives with the reason each lost (rules in the page only, a validation library, attributes as today). Its review record against `QC-ADR-001` | No | criterion 1 |
| 2 | The table of the validation rules | Write the table "Validation rules of the booking flow" in `docs/refactor/BusinessRules.md`: every field of the public booking, the guest sign-in, the move of a booking by a guest, the booking form and the guest form of the back office and the two address forms, each with its type, length, characters or values, normalisation, joined rules, error code and source; and the new business rules for the five years and the 30 nights. Read the data that exists first, so that the list of countries stores what the hotel's data already uses | No | criterion 2 |
| 3 | The tests of the rules first | Before the code: for every field the tests at its limits and one per refused kind; the list of hostile strings through every text field; the dates with a fixed clock, with 29 February; the unknown field and the body over 64 kilobytes; the test that compares the request types with the table; page tests of the fields and of the pages that show a value. Keep the transcript of the failing run for the review record | No | criteria 2 to 5 |
| 4 | The rules in the domain and the API | One implementation of each rule; the API refuses a value outside its rule with the code of the field, refuses an unknown field and a text over its length, and writes a security event without the value; the five years in the domain for making and moving a booking, the 30 nights for the public site; the three Danish messages in English. No value is stripped or rewritten beyond trimming and the normal form | No | criteria 3 to 5 |
| 5 | The rules on the pages | The booking page, the page that finds a booking, the guest pages, and the booking form and the guest form of the back office use the same rules for early feedback: the country as a list, the earliest and the latest day on the date fields, the texts of the refusals. Tests of the view models and of the pages | No: the fields and their meaning stay; UC-001 and UC-010 get the new rules in task 7 | criteria 4 and 5 |
| 6 | The size and time limits of a request | The largest body at the API and at the public listener of nginx, and the timeout of one hour only for the web socket of the pages; `ops/tests/test-nginx.sh` shows both, and every check that was there before still passes | No | criterion 3 |
| 7 | The documents, the code review and the pull request | The rule ledger and the test plan in `docs/refactor/`, UC-001 and UC-010 where a rule changes a step, the wiki page Security-Controls; the code review records against `QC-CS-001`, `QC-SH-001` and `QC-YML-001`; the commits and the pull request, which closes the issues of tasks 1 to 7 | No | criteria 6 and 7 |
| 8 | The deploy check and the gate | After the merge and S01's approval of the run of continuous deployment (CD): the check on the live site of criterion 8 with its evidence, the gate review record against criteria 1 to 8, the row of MIL-086 in the traceability matrix and the closing of the GitHub milestone | No | criterion 8 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[MIL-067]: ./mil-067-bad-input-on-the-web-pages.md
[MIL-077]: ./mil-077-hostile-input-against-the-api.md
[9f5c208]: https://github.com/NF-Hotel/NFHotel/commit/9f5c208a73c7fbd268341b25501f518f39ad568e
