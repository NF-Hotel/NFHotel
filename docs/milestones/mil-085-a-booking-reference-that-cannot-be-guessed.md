# MIL-085 A booking reference that cannot be guessed

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-085 |
| CrossReference | [BC-001], [US-001], [UC-010], [ADR-0004], [ADR-0005], [ADR-0047], [MIL-018], [MIL-079] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: planned at S01's request in chat on 2026-11-19, after a capture-the-flag team told S01 that the next and the previous booking number can be worked out ("booking numre er unikke og ikke kommer lige efter hinanden. Det skal gerne være GUID"); the decisions are the AI's for S01 ("Du skal tage beslutningerne for mig"), and the plan is accepted by the AI as stand-in after RC-410, Go, for a run in autonomous mode ("Når din plan er lavet skal Opus 5.5 lave det hele i automode") | pending |

---

## Purpose

Decide that a booking of NF Hotel is known by a random reference: no page, no address and no answer to a guest shows a number from which the next or the previous booking follows.

A capture-the-flag (CTF) team that S01 is a member of told S01 that the booking numbers of the site can be worked out. The code at 3b973c7 confirms it:

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | The booking number is `FLZ-` plus the key of the booking, and the database counts that key up by one (BR-109). The number is not stored; it is the key, formatted | `src/NFHotel.Domain/Entities/Booking.cs` (`BookingNumber`), `src/NFHotel.Application/UseCases/Bookings/BookingMapping.cs`, the identity column `booking_id` | From one number a stranger knows the next and the previous one, how many bookings the hotel has taken, and how many came in between two bookings of their own |
| 2 | The number is in the address of the sign-in page (`/booking/{number}`), on the receipt card and in the link under the receipt; the form takes `FLZ-000123`, `flz-123` and `123` alike | `src/NFHotel.Web/Features/Booking/GuestSignIn.razor`, `BookingReceiptCard.razor`, `Book.razor.cs`, `BookingNumberText.cs` | The number is written to browser histories and proxy logs, and every number that exists can be named |
| 3 | Since [MIL-079] a booking opens only with its number and its email, and five wrong tries for one number are followed by a wait of 15 minutes. The count is per booking number, kept in the memory of one process and shared with the staff sign-in; there is no count per caller | `src/NFHotel.Application/UseCases/Bookings/BookingService.cs` (`LookUpReceiptAsync`), `src/NFHotel.Infrastructure/Security/InMemoryLoginThrottle.cs`, `src/NFHotel.Api/Controllers/GuestAuthController.cs` | One caller can try an email against every number in turn, five tries each; a restart clears the counts; a caller who names many numbers grows the store without a bound |
| 4 | The answer to a public booking carries the whole booking, with its guest and its room, to the web application, which uses the receipt only | `src/NFHotel.Application/Dto/Bookings/PublicBookingDto.cs`, `BookingDetailsDto.cs` | More personal data crosses the wire than the page shows |
| 5 | The older lookup `POST api/bookings/public/receipt` still exists beside the guest sign-in and does the same | `src/NFHotel.Api/Controllers/BookingsController.cs` | Two doors that have to be kept equal |

Read for this plan on 2026-11-19, and used in the decisions:

- The Insecure Direct Object Reference Prevention Cheat Sheet of the Open Worldwide Application Security Project (OWASP): random identifiers are a second line of defence, and the check of who may see an object stays the first.
- The OWASP Application Security Verification Standard (ASVS) 5.0, requirements 8.2.2 (access to one's own data only), 11.5.1 (a value that must not be guessed has at least 128 bits from a secure random generator, and "UUIDs do not respect this condition") and 15.3.1 (an answer holds only the fields that are needed).
- Request for Comments (RFC) 9562 on universally unique identifiers (UUID), section 8: a UUID must not be used as a capability, that is, an identifier whose mere possession grants access; version 4 is the one to use near security.
- Microsoft's documentation of `Guid.NewGuid`: a version 4 UUID with 122 bits from the secure random generator of the operating system on Linux since .NET 6, and not meant for cryptographic use.

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | The booking number of NF Hotel becomes a reference: a UUID of version 4 in its usual form of 36 characters, made when the booking is made. Its 122 random bits come from the random generator of the operating system | S01 asked for a GUID, which is Microsoft's name for a UUID. Version 4 is random throughout; version 7 would carry the time and the order of creation, which is what has to go |
| 2 | The reference names a booking and never opens one. A lookup and the guest sign-in keep asking for the email of the booking, with one answer for an unknown reference, an unreadable one and a wrong email | RFC 9562, section 8, and ASVS 11.5.1: a UUID is not a secret. The secret of the sign-in stays the pair, behind the limit on wrong tries |
| 3 | The counting key stays the key of the table and is used inside the system only: in the database, between the web application and the API for the operations of staff, and towards the market analysis service. No page and no email shows it, and no public or guest operation takes it or returns it. The format `FLZ-` (BR-109) is retired, also in the back office, where staff see and search the reference | One number per booking for guest and staff, and nothing that a stranger sees counts up |
| 4 | The column `reference` is added as unique and never empty, with a default that the database fills with a version 4 UUID; the bookings that exist get theirs in the same migration. The migration only adds | The release before this one keeps working against the migrated database, so a rollback stays possible |
| 5 | From the deploy on, a number of the old kind is refused like any unreadable reference. Staff find a booking by guest and dates and tell the guest the reference | An old number that still worked would keep the hole open |
| 6 | Wrong tries are counted per reference as today and also per caller address, in a store with a bound | Finding 3. The address of a caller is the one MIL-087 makes trustworthy; until then it is the one the API sees today |
| 7 | The public and guest answers hold what their pages show and nothing more: the reference, the dates, the nights, the room size, the status and the payment text | ASVS 15.3.1 and finding 4 |
| 8 | One lookup stays. The decision record says whether the older endpoint is removed or becomes the one implementation behind the guest sign-in | Finding 5 |

## Deliverable

- ADR-0049: the booking reference, with the alternatives and the reason each lost (a shorter random code, a UUID as the key of the table, a UUID of version 7, an encrypted number).
- The domain, the database and the API: the reference on the booking, the migration that only adds, the contracts of the public and guest operations without the counting key, one lookup, and the limit on wrong tries per reference and per address.
- The pages: the receipt, the page that finds a booking and the guest pages with the reference; the back office shows and searches the reference; no page shows `FLZ-`.
- Tests, written first: unit tests of the reference and of the lookup, tests on PostgreSQL of the migration and of the unique index, API tests of the contracts and of the limit, page tests, and the browser test of booking, finding and cancelling.
- Documents: BR-109 in the rule ledger, [UC-010] and the artifacts that name the booking number, the dictionary, the wiki pages Security-Controls and Public-Access, and the review records.
- The check on the live site after the deploy, with its evidence in `docs/sqa/evidence/mil-085/`, without personal data and without the public address.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0049 is Accepted after its review: decisions 1 to 8, the four alternatives with the reason each lost, and the texts it rests on | Yes | No |
| 2 | Every booking has a unique reference that is a UUID of version 4: a test reads the version and the variant, finds 10,000 references made in a row all different, and names the generator; after the migration on PostgreSQL every booking that existed has a reference, the number of bookings is unchanged, and the release before this one passes its smoke test against the migrated database | Yes | No |
| 3 | No page (public, guest or staff) and no address shows the counting key or the text `FLZ-`, and no public or guest operation of the API takes or returns the key: a test walks the pages and the contracts, and the test of the public endpoints knows the new list | Yes | No |
| 4 | With the right reference and email, in any letter case of the email, a lookup and the guest sign-in succeed; a number of the old kind, an unknown reference, an unreadable one and a wrong email get the same status and the same body; the sixth wrong try for one reference within 15 minutes gets 429, and so does the try over the limit per address that ADR-0049 sets; 100,000 tries with different references leave the store within its bound | Yes | No |
| 5 | The public and guest answers hold only the fields of decision 7, shown by a contract test; a guest token for one booking still gets 403 for another | Yes | No |
| 6 | The browser test passes on the throwaway stack: book, see a receipt with a reference in UUID form, find the booking with reference and email, sign out, sign in, cancel; an unknown reference gets no answer of the 500 kind | Yes | No |
| 7 | The documents say what the deliverable says; the code review against `QC-CS-001` is Go; the coverage floor holds; every required check of the pull request is green | Yes | No |
| 8 | On the live site after the deploy: a number of the old kind shows no booking, a booking that S01 makes for the test, or that the AI makes on S01's word, has a reference of version 4 and opens with its email only, and the evidence is in the evidence folder | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-018 | The guest sign-in, the guest token and the guest pages, which name a booking |
| MIL-079 | The lookup by number and email and its limit on wrong tries |
| ADR-0004 | The API behind its token, with the web application as its caller |
| ADR-0005, ADR-0047 | The limit on wrong tries and the guest sign-in as decided |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 43 and success criterion 46: a booking is known by a random reference, and the reference alone opens nothing | Criteria 2 to 6 and 8 |
| BC-001 objective 21: a guest reaches only their own booking | Criteria 4 and 5 |
| US-001.13: a guest sees, moves and cancels the own booking | Criterion 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-24. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0049 The booking reference | Write the decision record before any code: the eight decisions of this milestone, the alternatives with the reason each lost (a shorter random code, a UUID as the key of the table, a UUID of version 7, an encrypted number), the limit on wrong tries per address with its number and its bounded store, and what happens to the older lookup endpoint. Verify in Microsoft's documentation that `Guid.NewGuid` draws from the secure random generator of the operating system on the platform of the images, and cite it. Its review record against `QC-ADR-001` | No | criterion 1 |
| 2 | The tests of the booking reference first | Before the code: unit tests of the reference (version, variant, 10,000 different values) and of the lookup (same answer for the four kinds of refusal), a test on PostgreSQL of the migration (every booking gets a reference, the count stays, the index is unique), API tests of the contracts without the counting key and of the limit per reference and per address, and the changed browser test. Keep the transcript of the failing run for the review record | No | criteria 2 to 6 |
| 3 | The reference in the domain, the database and the API | The reference on `Booking`, set when the booking is made; the migration that adds the column with its default and its unique index and nothing else; the public and guest contracts with the reference in place of the key and with the fields of decision 7 only; one lookup; the limit on wrong tries per reference and per address with a bounded store; the guest token and the guest pages keep working. The staff operations may keep the key between the web application and the API | No | criteria 2 to 5 |
| 4 | The reference on the guest pages and in the back office | The receipt card, the page that finds a booking (its address and its form take a reference), the link under the receipt and the guest pages; the booking list of the back office shows and searches the reference; the parser of the old number goes, and with it the text `FLZ-`. Page tests for each | No: it changes how UC-010 and UC-001 name a booking, and task 5 updates both | criteria 3 and 6 |
| 5 | The documents, the code review and the pull request | BR-109 in `docs/refactor/BusinessRules.md` and the documents that name the booking number: UC-010 with its artifacts, UC-001, the domain model, the class diagram and the dictionary (the term booking reference); the wiki pages Security-Controls and Public-Access; the code review record against `QC-CS-001`; the commits and the pull request, which closes the issues of tasks 1 to 5 | No | criterion 7 |
| 6 | The deploy check and the gate | After the merge and S01's approval of the run of continuous deployment (CD): the check on the live site of criterion 8 with its evidence, the gate review record against criteria 1 to 8, the row of MIL-085 in the traceability matrix and the closing of the GitHub milestone | No | criterion 8 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[UC-010]: ../use-cases/uc-010/uc.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0047]: ../adr/adr-0047-guest-login.md
[MIL-018]: ./mil-018-guest-login.md
[MIL-079]: ./mil-079-a-booking-opened-only-with-its-email.md
