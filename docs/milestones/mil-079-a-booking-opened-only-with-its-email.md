# MIL-079 A booking opened only with its email

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-079 |
| CrossReference | [BC-001], [ADR-0004], [ADR-0005], [MIL-016], [MIL-018] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | S01 | Initial version: the public receipt page opens any booking by its number alone, checked on the live site; planned at S01's request in chat ("teste om det er rigtigt i virkeligheden (i projektet) om det kan lade sig gøre og dokumentere det samt fixe så det ikke kan lade sig gøre"), with screenshots and other evidence ("husk at inkludere screenshots og andet bevis") | pending |

---

## Purpose

Close the open read of a booking. Today anyone who counts the booking numbers on the public receipt page sees every
booking of the hotel, and the API gives any booking, with its guest, to every caller that holds the API token. After
this milestone a guest opens a booking with its number and the email it was made with, the answer holds only what the
receipt shows, and only staff with `Bookings.View` read a booking by its id.

[MIL-018] planned this on 2026-10-18 as part of a guest sign-in, and it was never built. This milestone takes the part
of MIL-018 that closes the read (its decisions 1, 3 and 5, without the guest token); the guest sign-in, the guest token
and the change pages stay with MIL-018.

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | The receipt page `/booking/{number}` looks a booking up by its number alone, and the number is `FLZ-` plus the id of the booking (BR-109) | `src/NFHotel.Web/Features/Booking/BookingReceipt.razor`, `BookingReceiptViewModel.cs` | Counting from `FLZ-000001` lists every booking with its dates, nights, room type, status and payment text, and tells which numbers exist. Checked on the live site on 2026-11-12 without signing in: `FLZ-000001` showed a stay of one night in July 2018 in a single room, checked out, and `FLZ-999999` showed the not-found text. The page shows no name, email, phone or passport number, by design (BR-C-06) |
| 2 | `GET /api/bookings/{id}` has no `RequiresPermission`, and `BookingService.GetByIdAsync` no permission check, unlike the other booking reads | `src/NFHotel.Api/Controllers/BookingsController.cs`, `src/NFHotel.Application/UseCases/Bookings/BookingService.cs` | Any caller with the API token reads any booking with the name, email and phone of its guest. Today only the web application holds the token ([ADR-0004]), and the receipt page is its only caller of this endpoint |
| 3 | The receipt view model calls the limitation deliberate until access control arrives; MIL-018 was accepted, but has no issues and no code | `BookingReceiptViewModel.cs`, [MIL-018] | The limitation stayed for more than three weeks after the plan that should end it |

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | A guest opens a booking with the booking number and the email of the booking; no account, no password, no token | The guest already holds both, and there is nothing new to keep or to store (MIL-018 decision 1) |
| 2 | `POST /api/bookings/public/receipt` takes the number and the email in the body and answers only the fields of the receipt card: number, dates, nights, room type, status and payment text | The email stays out of addresses and access logs, and the answer holds nothing that the page does not show |
| 3 | An unknown number, an unreadable number and a wrong email get the same 404 answer with the same text; the email is compared without letter case and surrounding spaces, and the booking is read also when the email turns out wrong | Nothing tells a guesser whether a number exists (MIL-018 decision 3) |
| 4 | After 5 failed lookups for one booking number within 15 minutes, the next lookup for that number gets 429: the throttle of [ADR-0005], keyed by the booking | The email of a known number cannot be found by trying |
| 5 | `GET /api/bookings/{id}` requires `Bookings.View`, at the endpoint and in the use case: 401 without a staff token, 403 without the permission | The API is the authority and the page a convenience (MIL-018 decision 5); the same check as the other booking reads |
| 6 | Right after a booking the guest sees the receipt without typing the email, and the page tells how to find the booking again | The receipt after booking keeps working |

## Deliverable

- API: the receipt lookup (use case, answer type, endpoint and throttle per booking number) and `Bookings.View` on the
  read by id, at the endpoint and in the use case.
- Web: the receipt page asks for the email and takes the number from the address; the booking page shows the receipt
  right after the booking; the texts of both pages.
- Tests, written first: API tests of the read by id and of the lookup, unit tests of the use case and tests of the
  receipt view model; the API tests fail on the code before the fix.
- Documents: BR-C-06 and the test plan in `docs/refactor/`, the wiki pages on the booking flow and the security
  controls, and the review records.
- The check on the live site, before and after the deploy.
- Evidence in `docs/sqa/evidence/mil-079/`, linked from the review records: the screenshots of the receipt page on the
  live site before the fix (taken 2026-11-12 10:47Z, without the address bar and without personal data) and after the
  deploy, and the transcripts of the code, the receipt, the counting of numbers and the test runs, failing before the
  fix and passing after it. The public address stays in a variable and appears in none of them.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A read of a booking by id with the API token and no staff token gets 401, with a staff token without `Bookings.View` 403, and with the permission 200; the review record shows the test failing on the code before the fix and passing after it, as transcripts in the evidence folder | Yes | No |
| 2 | A lookup with the right number and email, in any letter case, gets 200 and a body without name, email, phone or passport number; a wrong email, an unknown number and an unreadable number get the same 404 body; the sixth failed lookup for one number within 15 minutes gets 429; creating a booking and listing free rooms still work without a staff token | Yes | No |
| 3 | The receipt page without the email shows no booking, also in the first HTML that the server sends; with the right email it shows the card; right after a booking the guest sees the receipt; the tests of the view models pass | Yes | No |
| 4 | On the live site after the deploy, `/booking/FLZ-000001` without the email shows no booking data, and a booking that S01 makes for the test opens with its number and email; the screenshots and transcripts of the page before the fix and after the deploy are in the evidence folder | Yes | No |
| 5 | The documents say what the deliverable says, the code review against `QC-CS-001` is Go, and every required check of the pull request passes | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The staff sign-in and its token |
| MIL-018 | The plan of the guest sign-in; this milestone delivers its decisions 1, 3 and 5 for reading |
| ADR-0004 | The API behind its token, with the web application as its caller |
| ADR-0005 | The staff token and the throttle |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 21: a guest reaches only their own booking and guest details and nobody else's | Criteria 1 to 4 |
| BR-C-06: the customer cannot see other guests' personal data | Criteria 2 and 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-19. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The tests first | API tests in `tests/NFHotel.Api.Tests` of the read by id (401, 403, 200) and of the lookup (200, the same 404, 429, the public endpoints still open); they fail on the code before the fix, and the transcript of the failing run goes into the evidence folder | No | criteria 1 and 2 |
| 2 | The API and the use case | `Bookings.View` on the read by id at the endpoint and in `BookingService`; the lookup use case with its answer type, the endpoint and the throttle per booking number; unit tests of every allowed and refused case | No: it narrows the receipt of BR-C-06; the guest use case stays with MIL-018 task 3 | criteria 1 and 2 |
| 3 | The web pages | The receipt page with the email field and the number from the address; the receipt on the booking page right after the booking; the texts; tests of the view models | No | criterion 3 |
| 4 | The documents, code review and pull request | BR-C-06 and the test plan, the wiki pages, the evidence folder with the screenshots and transcripts taken before the fix, the review record against `QC-CS-001`; the pull request, which closes the issues of tasks 1 to 4 | No | criterion 5 |
| 5 | Deploy check and gate | After S01's merge and approval of the CD run: the check on the live site of criterion 4 with its screenshots and transcripts, the gate review record against criteria 1 to 5, the row of MIL-079 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criterion 4 |

---

[BC-001]: ../business-case.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[MIL-016]: ./mil-016-backoffice-login.md
[MIL-018]: ./mil-018-guest-login.md
