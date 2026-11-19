# MIL-088 A booking verified by email

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-088 |
| CrossReference | [BC-001], [US-001], [UC-010], [ADR-0004], [ADR-0024], [MIL-018], [MIL-035] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: planned at S01's request in chat on 2026-11-19 ("man skal verificere på sin mail efter hver booking således man ikke kan booke alle rum eller lave booking overbelastning … så hotel konkurrenter f.eks. ikke kan booke alt så der er udsolgt"); the decisions are the AI's for S01 ("Du skal tage beslutningerne for mig"), and the plan is accepted by the AI as stand-in after RC-413, Go, for a run in autonomous mode ("Når din plan er lavet skal Opus 5.5 lave det hele i automode") | [9f5c208] |

---

## Purpose

Decide that a booking asked for on the public site takes a room only when the guest has verified it through a link sent to the email of the booking, so that nobody fills the hotel with bookings that nobody stands behind.

The code at 3b973c7 was read on 2026-11-19:

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | A booking on the public site is made at once: it is `Pending`, it has a room, and it keeps the room until staff cancel it. Nothing ever lets a `Pending` booking lapse | `src/NFHotel.Application/UseCases/Bookings/BookingService.cs` (`CreatePublicAsync`), `src/NFHotel.Domain/Entities/Booking.cs`, the constraint `ex_booking_room_period` | A competitor books every room and the guests see a full hotel: the automated threat OAT-021, denial of inventory, of the Open Worldwide Application Security Project (OWASP) |
| 2 | The application cannot send mail: no code, no package and no setting for it. The booking page says under the email field "We send the confirmation here." | The whole of `src/` and `ops/`; `src/NFHotel.Web/Features/Booking/Book.razor` | Nothing shows that the email of a booking belongs to the person who typed it, and the page promises a mail that never comes |
| 3 | Each public booking stores a new guest before the booking; when the booking then fails, the guest stays | `BookingService.cs` (`CreateBookingInternalAsync`) | The personal data of nobody's booking is kept |
| 4 | The application has no timed job; its one background worker waits on a queue | `src/NFHotel.Api/Startup/MarketAnalysisRunWorker.cs` | Something that lapses needs a first timed job |
| 5 | The guest sign-in compares the email as typed, trimmed and without letter case, and a guest gets a session right after booking, before anybody has shown that the email is theirs | `BookingService.cs` (`LookUpReceiptAsync`), `src/NFHotel.Application/UseCases/Guests/GuestAccessService.cs` | A booking in somebody else's email is as good as one's own |

Read for this plan on 2026-11-19: the OWASP automated threat OAT-021 ("selection and holding of items from a limited inventory … which are never actually bought, or paid for, or confirmed"); item API6:2023 of the OWASP API Security Top 10, which names "making a reservation"; the OWASP cheat sheets on email validation and verification (a random token that works once and for a limited time, nothing active before the verification, no token and no full address in a log) and on a forgotten password (the link is built from configuration and not from the request, the page sends no referrer, the token is stored like a password); and the Application Security Verification Standard (ASVS) 5.0, requirements 2.3.1 (the steps of a flow in their order), 2.3.3 and 2.3.4 (a transaction, and a lock so that a limited thing is not given twice), 3.5.3 (nothing changes on a request that only reads), 11.5.1 (128 random bits for what must not be guessed), 14.2.1 (no secret in an address) and 1.3.11 (what goes into a mail is made safe for the mail system).

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | Three steps: ask, verify, booked. On the public site the guest asks for a booking; the API stores a booking request with the dates, the room size, the number of guests and the fields of the guest, and takes no room. The room is taken when the guest verifies. If the last room of the size went in between, the guest is told and nothing is booked | A request that holds a room can be used to hold every room, and a cap on held rooms would only pass the harm to the next guest (OAT-021). A request that holds nothing costs the hotel nothing |
| 2 | The link works once and for 30 minutes. Its token has 256 random bits from the random generator of the operating system; the database keeps only its SHA-256 hash; the link is built from the configured address of the site and never from the request | The cheat sheets on email verification and on a forgotten password; ASVS 11.5.1 |
| 3 | Opening the link shows a page with one button, and only the button verifies. The token reaches the server in the body of that request and is in no address that a log keeps; the page sends no referrer and is not cached; an unknown token, a used one and a lapsed one get the same answer | ASVS 3.5.3 and 14.2.1: a mail scanner that opens the link verifies nothing, and no log holds a token |
| 4 | The mail is plain text with a fixed subject: the dates, the room size, the link and the time it lapses, and one sentence for a receiver who did not ask: do nothing, and it lapses. It holds no text that the sender typed | Otherwise the form sends a message of the sender's choosing to a stranger |
| 5 | When the guest verifies, the guest and the booking are stored in one transaction, the booking is `Pending` as today and has its reference, and staff confirm it as today. The guest is signed in and sees the receipt, and one more mail carries the reference and the dates | The work of staff does not change; the email is now shown to belong to the guest, so the session comes after the verification and not before |
| 6 | One email has one open request: a new request replaces the old one and its link. An email gets at most 3 verification mails per hour and 5 per day. The limits of MIL-087 per address and per email count requests from now on | The form must not fill a stranger's mailbox |
| 7 | A timed job in the API deletes a request with its personal data within one hour after its link has lapsed. The passport number of a request is encrypted as it is for a guest | Findings 3 and 4; the data of somebody who never booked is not kept |
| 8 | Mail leaves through a relay with the Simple Mail Transfer Protocol (SMTP) over an encrypted connection, behind a port of the application layer. Its password is a secret file like the other secrets ([ADR-0024]). When the relay does not take a mail, the guest is told, the request is removed, and a counter and an alert show it. A fuse stops the site at 1,000 mails a day, with an alert at 200 | ASVS 16.5.2: the flow fails closed when the relay is down; item API4:2023 asks for a ceiling on what a provider is sent |
| 9 | In development and in the test stack a mail catcher takes the mail, and the browser tests read the link from it | The whole flow is tested without a provider |
| 10 | The verification is switched on by a setting, and it is on wherever a relay is set. Production has no relay yet: only S01 can open the account, choose the sender address and set its records in the name system. Until then production books as today, behind the limits of MIL-087, and the gate of this milestone stays open | The AI opens no account and writes no secret. Everything else is built, tested and merged first |
| 11 | Staff book as today, without a request and without a mail | The back office is behind its own sign-in |

## Deliverable

- ADR-0052 (a booking takes a room once its email is verified) and ADR-0053 (mail from the application through a relay), each with its alternatives: a room held for a limited time, a puzzle for humans, a payment, an account for every guest; and for the mail a provider's own interface, or a mail server on the VM.
- UC-011 "Book a Room and Verify It by Email" with its user story US-001.14 and the artifacts it affects, and the terms booking request and verify in the dictionary.
- The API: the booking request, the verification, the clean-up job, the mail port with its relay adapter, the settings and the secret.
- The pages: ask, check the inbox, verify, send again, and the receipt after the verification.
- The mail catcher in the development and test stacks; the counters and the alerts for mail.
- Tests, written first: the three steps, the token, the races for the last room, the limits of the mails, the clean-up, the relay that is down, and the browser tests with the mail catcher.
- Documents: the rule ledger, the wiki pages Security-Controls, Environment-Files and Alerts, the review records.
- The check on the live site once S01 has set the relay, with its evidence in `docs/sqa/evidence/mil-088/`.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0052 and ADR-0053 are Accepted after their reviews: the eleven decisions with any changed number and its reason, and the alternatives with the reason each lost | Yes | No |
| 2 | UC-011 and US-001.14 are Accepted after their reviews, with the artifacts they affect, and the dictionary has the two terms | Yes | No |
| 3 | A request stores no booking and takes no room: the same room is still offered to the next guest. The mail in the catcher holds the dates, the room size, the link and its time, and no text that was typed into the form. A request for the link that only reads changes nothing; the button verifies once; a second use, a wrong token and a lapsed one get the same answer | Yes | No |
| 4 | After the verification the booking is `Pending` with a reference, its guest was stored in the same transaction, the guest is signed in, and one mail carries the reference. Of 20 verifications at once for the last room of a size exactly one gets the booking, the other 19 are told that the room is gone, and no guest is left without a booking | Yes | No |
| 5 | With a fixed clock: a new request of one email makes the link of the old one useless; the fourth verification mail to one email within an hour and the sixth within a day are refused; a request is gone, with its personal data, within one hour after its link has lapsed | Yes | No |
| 6 | With the relay down the guest is told, no request is left and the counter counts; a relay without an encrypted connection is refused at the start; a line break in an email is refused before any mail is built; after a browser run no token, no verification address and no unmasked email is in a log of the test stack | Yes | No |
| 7 | The browser tests pass on the throwaway stack with the mail catcher: ask, read the mail, verify, see the receipt with a reference, find the booking again; a lapsed link; send again. The rule tests show the alerts for mails that fail and for 200 mails in a day | Yes | No |
| 8 | With the setting off, the public booking works as before this milestone and the tests of that path pass; with it on and no relay set, the API refuses to start and says why | Yes | No |
| 9 | The documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the new package for mail is pinned, locked and free of known vulnerabilities; the coverage floor holds; every required check of the pull request is green | Yes | No |
| 10 | On the live site, once S01 has set the relay and the setting is on: a booking that S01 asks for the test arrives as a mail in S01's mailbox, takes no room before the button is pressed, is `Pending` with a reference after it, and a second request of the same email makes the first link useless; the evidence is in the evidence folder without personal data | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-085 | The reference that the receipt and the second mail carry |
| MIL-086 | The rules of the email and of the other fields, checked before a mail is sent |
| MIL-087 | The limits per address and per email, and the address of a caller |
| MIL-018 | The guest sign-in and the guest pages |
| MIL-035, ADR-0024 | Secrets as files, the pattern for the password of the relay |
| S01 | The account at a mail relay, the sender address and its records in the name system, and the password on the VM (task 9) |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 43 and success criterion 48: a booking takes a room only after its email is verified | Criteria 3 to 8 and 10 |
| US-001.14 (new): book a room and verify it by email | Criteria 2 to 4 and 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-02 for criteria 1 to 9. Criterion 10 follows the day S01 sets the relay. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0052 A booking takes a room once its email is verified | Write the decision record before any code: the three steps, the token and its link, the page with the button, the limits of the mails, the clean-up, what staff see, and the setting; the alternatives with the reason each lost (a room held for a limited time, a puzzle for humans, a payment, an account for every guest). Its review record against `QC-ADR-001` | No | criterion 1 |
| 2 | ADR-0053 Mail from the application through a relay | Write the decision record: the port and its adapter, the library and why, the settings and the secret file, the encrypted connection, what happens when the relay is down, the fuse, the mail catcher of the stacks, and what S01 has to bring (an account, a sender address with its records for the sender policy, the signature and the reporting policy of the domain); the alternatives with the reason each lost. Its review record against `QC-ADR-001` | No | criterion 1 |
| 3 | UC-011 Book a room and verify it by email | The use case with its main and alternative flows (the room went, the link lapsed, the mail did not arrive, a stranger's email), the user story US-001.14, and the artifacts it affects in its folder: the system sequence diagram, the operation contracts, the sequence diagram, and the booking request in the domain model and the class diagram; the terms in the dictionary; each with its review record | Yes: UC-011 and US-001.14 | criterion 2 |
| 4 | The tests of the verification first | Before the code: the three steps, the token (once, 30 minutes, the same answer for the three refusals), the 20 verifications at once, the mails per email with a fixed clock, the clean-up, the relay that is down or unencrypted, the line break in an email, the setting on and off, and the browser tests with the mail catcher. Keep the transcript of the failing run for the review record | No | criteria 3 to 8 |
| 5 | The booking request, the verification and the clean-up in the API | The booking request with its table (a migration that only adds), the token and its hash, the verification that stores the guest and the booking in one transaction, the timed job that deletes a lapsed request, the limits of decision 6, the security events and counters, and the setting with its check at the start | No: task 3 holds the use case | criteria 3 to 5 and 8 |
| 6 | The mail port, the relay adapter and the mail catcher | The port in the application layer and its adapter for a relay over an encrypted connection; the two mails as plain text; the settings, the secret file in `write-secret-files.sh` and the compose files with their examples; the fuse and the counters; the mail catcher, pinned, in the development stack and the test stack | No | criteria 3, 6 and 7 |
| 7 | The pages: ask, check the inbox, verify and send again | The booking page ends with "check your inbox" in place of the receipt; the page of the link with its one button, which sends the token in the body, sends no referrer and is not cached; the receipt after the verification; sending again within the limits; the true text under the email field. Tests of the view models and of the pages | No: task 3 holds the use case | criteria 3, 4 and 7 |
| 8 | The documents, the code review and the pull request | The rule ledger, the wiki pages Security-Controls, Environment-Files and Alerts, the alert rules with their rule tests; the code review records against `QC-CS-001`, `QC-SH-001` and `QC-YML-001`; the commits and the pull request, which closes the issues of tasks 1 to 8 | No | criteria 7 and 9 |
| 9 | The mail relay in production | S01 opens the account at a mail relay, chooses the sender address, sets the records of the domain for the sender policy, the signature and the reporting policy, and puts the password into the env file on the VM; then the setting is switched on with a deploy. The AI prepares the list of what is needed and checks the records from outside; it opens no account and sees no password | No | criterion 10 |
| 10 | The deploy check and the gate | After the merge and S01's approval of the run of continuous deployment (CD): the check that the site books as before with the setting off; and, once task 9 is done, the check on the live site of criterion 10 with its evidence, the gate review record against criteria 1 to 10, the row of MIL-088 in the traceability matrix and the closing of the GitHub milestone | No | criterion 10 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[UC-010]: ../use-cases/uc-010/uc.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[MIL-018]: ./mil-018-guest-login.md
[MIL-035]: ./mil-035-secrets-as-files.md
[9f5c208]: https://github.com/NF-Hotel/NFHotel/commit/9f5c208a73c7fbd268341b25501f518f39ad568e
