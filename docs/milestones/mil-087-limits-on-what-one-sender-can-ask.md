# MIL-087 Limits on what one sender can ask of the booking flow

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-087 |
| CrossReference | [BC-001], [US-001], [ADR-0004], [ADR-0005], [ADR-0015], [MIL-036], [MIL-079] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: planned at S01's request in chat on 2026-11-19 ("således man ikke kan booke alle rum eller lave booking overbelastning", "Sikre også API requests mod hacker angreb"); the decisions are the AI's for S01 ("Du skal tage beslutningerne for mig"), and the plan is accepted by the AI as stand-in after RC-412, Go, for a run in autonomous mode ("Når din plan er lavet skal Opus 5.5 lave det hele i automode") | [9f5c208] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Gate passed: RC-430 Go on criteria 1 to 8; tasks 1 to 7 in pull request 855 (RC-426 Go, RC-429 Go, RC-427 Go, RC-428 Go-with-conditions with its conditions met); the CD run 44624470820 of 6f7bb80, approved from S01's account at S01's word in chat, deployed it; task 8: on the live site the 61st search for free rooms within a minute from one address is refused with the text of the limits and counted in `nfhotel_security_events_total`, and the throwaway stack refuses the 6th booking request with the numbers of production | pending |

---

## Purpose

Decide that the booking flow counts what one caller address and one email ask for, refuses what goes over a written limit, and tells the team when the limits are hit; and that the address it counts is the one the proxy saw, not one the caller wrote.

The code at 3b973c7 was read on 2026-11-19:

| # | Finding | Where | What it allows |
| --- | --- | --- | --- |
| 1 | No public operation of the booking flow has a limit: not the booking, not the search for free rooms. nginx has none either, and the one limiter of the API covers the writes of staff | `src/NFHotel.Api/Controllers/BookingsController.cs` (`CreatePublicAsync`, `available-rooms`), `src/NFHotel.Api/Security/StaffWriteRateLimit.cs`, `ops/deploy/nginx/templates/public.conf.template` | One script books every room for every night, or keeps the API busy with searches |
| 2 | Nothing counts the bookings of one email: each public booking stores a new guest | `src/NFHotel.Application/UseCases/Bookings/BookingService.cs` (`CreatePublicAsync`) | One sender holds as many rooms as the hotel has |
| 3 | For the address of a caller the web application passes on the first entry of `X-Forwarded-For`, which the caller can write, because nginx only appends the address it saw; the API trusts every proxy and writes the value into the audit trail | `src/NFHotel.Web/Common/UseCaseRunner.cs`, `ops/deploy/nginx/snippets/proxy.conf`, `src/NFHotel.Api/Program.cs`, `src/NFHotel.Api/Security/HttpStaffCallContext.cs` | The audit trail can be made to name another address, and a limit per address on that value could be dodged |
| 4 | The booking flow writes no event and counts nothing: no log line for a booking, a lookup or a refusal, no counter, no alert. The only alert on a sign-in is the one for staff | `src/NFHotel.External/Telemetry/TelemetryRegistration.cs`, `ops/deploy/monitoring/prometheus/rules/security.yml` | Abuse of the flow is seen when a guest complains, not before |

Read for this plan on 2026-11-19: the Application Security Verification Standard (ASVS) 5.0 of the Open Worldwide Application Security Project (OWASP), requirements 2.1.3 and 2.3.2 (business limits written down, per user and for the whole application, and built as written), 2.4.1 (controls against automation), 4.1.3 and 15.3.4 (a header that a proxy sets cannot be overridden by the caller, and the address used for limits is the one the proxies carry in a trusted field), 6.1.1 (the limits are documented) and section V16.3 (security events are logged, among them attempts to get around a limit); the OWASP API Security Top 10 of 2023, items API4 (a limit on how often a client may call) and API6 (a flow such as making a reservation needs a limit of its own); the OWASP automated threat OAT-021, denial of inventory; and Microsoft's documentation of the rate limiting middleware.

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | One table, "Limits of the booking flow", names for every limit who is counted, the window, the number and the refusal. The code reads its numbers from settings whose defaults are the table | ASVS 2.1.3 and 6.1.1 |
| 2 | Per caller address: 5 booking requests per hour and 20 per day on the public site; 60 searches for free rooms per minute; the lookups and guest sign-ins of MIL-085. A refusal is status 429 with one text and the time to wait | API4:2023; the numbers leave room for a family or an office behind one address |
| 3 | Per email: at most 3 coming bookings made on the public site, where coming means not cancelled and with its departure today or later; the next one is refused with the same status and text as the other limits. Staff can always book | Finding 2; a guest with more than three stays ahead calls the hotel |
| 4 | The address of a caller is the one nginx saw. nginx writes it into a header of its own; the web application passes that value on to the API in a header that the API reads only on a call with the service token; the API trusts forwarded headers only from the networks of the stack; the audit trail uses the same value | ASVS 4.1.3 and 15.3.4; finding 3 |
| 5 | The counts per address live in the memory of the API, in a store with a bound, and a restart clears them; the count per email is read from the database | One instance of the API runs; a shared store would be one more service to run and to secure |
| 6 | Each booking made on the public site, each failed lookup and each refusal by a limit is a security event in the log, with its kind, the address and a masked email, and without any other typed value; each kind has a counter. Two alerts go where the others go: more than 20 refusals by the limits in 10 minutes, and more than 10 public bookings in an hour | ASVS V16.3; finding 4. The second number is far above a normal hour of this hotel |
| 7 | No puzzle for humans and no rule at Cloudflare in this milestone | Both need an action of S01 in an account. The decision record names them as the next step when the alerts show that the limits are not enough |

## Deliverable

- ADR-0051: the limits of the booking flow and the address of a caller, with the alternatives and the reason each lost (a limit in nginx, a rule at Cloudflare, a puzzle for humans, a shared store).
- The table of the limits in `docs/refactor/BusinessRules.md`.
- The address of a caller from nginx through the web application to the API and into the audit trail.
- The limits per address and per email in the API, with their settings.
- The security events, the counters and the two alerts with their rule tests.
- Tests, written first: every limit at its number with a fixed clock, the address that a caller writes, the bound of the stores, the events without typed values.
- Documents: the rule ledger, the wiki pages Security-Controls and Alerts, the review records.
- The check on the live site after the deploy, with its evidence in `docs/sqa/evidence/mil-087/`.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0051 is Accepted after its review: decisions 1 to 7 with any changed number and its reason, and the four alternatives with the reason each lost | Yes | No |
| 2 | The table of the limits is in the rule ledger, and a test fails when a default of the settings differs from the table | Yes | No |
| 3 | For every limit, with a fixed clock: the request at the number is accepted, the next one gets 429 with the one text and the time to wait, and after the window the count starts again; the fourth coming booking of one email on the public site is refused and the same booking by staff is accepted | Yes | No |
| 4 | Through nginx in the test stack, a request that writes its own `X-Forwarded-For` and `X-Real-IP` is counted and audited under the address that nginx saw; a call to the API without the service token cannot set the address; forwarded headers from outside the networks of the stack are ignored | Yes | No |
| 5 | After 100,000 requests from different addresses the stores are within their bound, and the requests of one address do not use up the count of another | Yes | No |
| 6 | Each event kind is written once, with a masked email and without another typed value, and the check of the logs of the test stack passes; the counters exist; the rule tests show both alerts firing at their numbers; the wiki page Alerts names them | Yes | No |
| 7 | The browser tests and the weekly load test pass with the numbers of production; the documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the coverage floor holds; every required check of the pull request is green | Yes | No |
| 8 | On the live site after the deploy, without a booking being made: the search for free rooms over its limit from one address gets the refusal, and the refusals show in the counter; the limit on booking requests is shown on the throwaway stack with the numbers of production; the evidence is in the evidence folder | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-085 | The limit on wrong tries per address, which uses the address of this milestone |
| MIL-086 | The rules of the fields, so that a refused value is not counted as a booking request |
| MIL-036 | The alerts on failed sign-ins and the way an alert reaches the team |
| ADR-0004, ADR-0005, ADR-0015 | The API behind the web application, the limit on wrong tries, and the limit per address of the mobile sign-in |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 43 and success criterion 48: limits per email and per address bound what one sender can ask for, and an alert tells the team | Criteria 2 to 6 and 8 |
| BC-001 objective 40: alerts before guests notice | Criterion 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-28. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0051 The limits of the booking flow and the address of a caller | Write the decision record before any code: the seven decisions with their numbers, where each count lives and what a restart does to it, how the address travels from nginx to the API and why the caller cannot write it, and the alternatives with the reason each lost (a limit in nginx, a rule at Cloudflare, a puzzle for humans, a shared store). Its review record against `QC-ADR-001` | No | criterion 1 |
| 2 | The table of the limits | Write the table "Limits of the booking flow" in `docs/refactor/BusinessRules.md`: for every limit who is counted, the window, the number, the status and the text of the refusal, and the setting that holds the number | No | criterion 2 |
| 3 | The tests of the limits first | Before the code: every limit at its number, one below and one above, with a fixed clock; the address that a caller writes, through nginx; the bound of the stores; the events with a masked email; the test that compares the defaults with the table. Keep the transcript of the failing run for the review record | No | criteria 2 to 6 |
| 4 | The address of a caller from the proxy to the API | nginx writes the address it saw into its own header on both listeners; the web application passes that value to the API, which reads it only on a call with the service token and trusts forwarded headers only from the networks of the stack; the audit trail uses the same value. `ops/tests/test-nginx.sh` and the test stack show it | No | criterion 4 |
| 5 | The limits per address and per email | The limits of decision 2 in the API with their settings and one refusal; the limit of decision 3, counted in the database; the bounded stores. The pages show the refusal as a text and stay usable | No: the limits add refusals to steps that exist; the rule ledger and the use cases get them in task 7 | criteria 3 and 5 |
| 6 | The security events, the counters and the alerts | The events of decision 6 in the log, the counters, the two alert rules with their rule tests under `ops/deploy/monitoring/`, and the check of the logs of the test stack for typed values | No | criterion 6 |
| 7 | The documents, the code review and the pull request | The rule ledger, UC-001 and UC-010 where a refusal is a new step, the wiki pages Security-Controls and Alerts; the code review records against `QC-CS-001`, `QC-SH-001` and `QC-YML-001`; the commits and the pull request, which closes the issues of tasks 1 to 7 | No | criterion 7 |
| 8 | The deploy check and the gate | After the merge and S01's approval of the run of continuous deployment (CD): the check on the live site of criterion 8 with its evidence, the gate review record against criteria 1 to 8, the row of MIL-087 in the traceability matrix and the closing of the GitHub milestone | No | criterion 8 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[MIL-036]: ./mil-036-security-operations.md
[MIL-079]: ./mil-079-a-booking-opened-only-with-its-email.md
[9f5c208]: https://github.com/NF-Hotel/NFHotel/commit/9f5c208a73c7fbd268341b25501f518f39ad568e
