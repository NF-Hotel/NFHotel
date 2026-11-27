# MIL-089 The booking flow attacked and measured

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-089 |
| CrossReference | [BC-001], [US-001], [ADR-0015], [ADR-0047], [MIL-027], [MIL-036], [MIL-054], [MIL-077], [SAR-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: planned at S01's request in chat on 2026-11-19 ("Kontroller bagefter for sårbarheder og huller samt valider kvaliteten så input og outputtet er robust, stabilt og modstandsdygtigt"); the decisions are the AI's for S01 ("Du skal tage beslutningerne for mig"), and the plan is accepted by the AI as stand-in after RC-415, Go, for a run in autonomous mode ("Når din plan er lavet skal Opus 5.5 lave det hele i automode") | [9f5c208] |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Gate passed: RC-441 Go on criteria 1 to 8; tasks 1 to 6 in pull request 882 (RC-438, RC-439, RC-440 and RC-442, their conditions met at the gate, and SAR-002 Accepted after RC-441); the CD run 45889641080 of dcbc88ce, approved from S01's account, deployed it; task 7: on the live site the checks of MIL-085 to MIL-088 that store nothing pass, the booking pages send their security headers, and the counter shows the refusals | [6981cab] |

---

## Purpose

Decide from evidence that the booking flow holds after MIL-085 to MIL-088: every requirement of the chosen chapters of the standard has a verdict with its evidence, every abuse case has a test, the scans and the load test pass, and what stays open is named with its owner.

What is known on 2026-11-19, before the work:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | The weekly load test reads three pages and makes no booking | `ops/load/nfhotel.js` | Nothing shows how the booking flow behaves under load, with or without limits |
| 2 | Every rule of the scan that ignores or only warns lapses on 2026-12-08 | `ops/security/zap-rules.tsv` | After that day the scan fails until each rule is renewed with a reason or removed |
| 3 | The web socket path of the pages, `/_blazor`, is not behind the Access check of `/admin`; staff still have to sign in | `ops/deploy/nginx/templates/public.conf.template` | Seen while reading; not judged yet |
| 4 | The two routes of the mobile app are open on the public listener, by the decision of [ADR-0015] | `ops/deploy/nginx/snippets/mobile-api.conf` | Seen while reading; to be read against the decision |
| 5 | The guest token is signed with the key of the staff token and told apart by its audience and role; it stays valid for up to two hours after its booking was cancelled | `src/NFHotel.Infrastructure/Security/JwtGuestTokenIssuer.cs`, `src/NFHotel.Application/UseCases/Guests/GuestOwnership.cs` | Seen while reading; to be read against [ADR-0047] |
| 6 | Two operations of the API need only the service token | `src/NFHotel.Api/Controllers/BenchmarkController.cs` (`POST api/benchmark/results`), `AiDefaultsController.cs` (`GET api/ai-defaults`) | Seen while reading; only the web application and the benchmark service hold that token |
| 7 | `Directory.Build.props` says version 0.1.0, and the release of 2026-11-19 is v0.2.0 | `Directory.Build.props` | The number in the code no longer follows the releases |

The standard is the Application Security Verification Standard (ASVS) 5.0 of the Open Worldwide Application Security Project (OWASP), read for this plan on 2026-11-19 in its chapters V1 (encoding and sanitisation), V2 (validation and business logic), V4 (API and web service), V8 (authorisation), V15 (secure coding) and V16 (logging and error handling), and in sections V3.4, V3.5, V6.3, V11.5 and V14.2.

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | The review is a security audit report, SAR-002, of the booking flow against ASVS 5.0 at its level 2: chapters V1, V2, V4, V8, V15 and V16 in full, and of chapters V3, V6, V7, V11 and V14 the requirements that the flow touches. Each requirement gets one verdict: met, with the test or the file that shows it; not applicable, with the reason; or open, with a finding | S01 asked for a check for holes afterwards. The flow handles personal data, among it passport numbers, which is why level 1 is not enough |
| 2 | SAR-002 lists the abuse cases of the flow, each with the test that shows its control: working out a booking, trying emails against a reference, taking the rooms, filling a stranger's mailbox, using a link twice, the race for the last room, writing the address of the caller, a field that the request type does not have, markup and query text in a field, a body that is too large, and many slow requests | A requirement that is met on paper is shown by an attack that fails |
| 3 | A defect of the booking flow that the review finds is fixed in this milestone, with its test. Anything else becomes a finding for S01 with its severity and is not built here | The plan that S01 handed over covers the booking flow; a new scope is S01's to decide |
| 4 | The scans run on the commit that holds MIL-085 to MIL-088: the full scan of the site and the scan of the API, the code scan, the audit of the packages and the gate of the images. Each rule of the scan that lapses on 2026-12-08 is renewed with its reason or removed | Finding 2 |
| 5 | The load test gets the booking flow: searches and booking requests within the limits keep the thresholds of today (95 percent of the answers under 1,000 ms, fewer than 5 percent failed), and a burst over the limits gets refusals and no answer of the 500 kind | Finding 1; "robust, stable and resilient" is measured |
| 6 | `Directory.Build.props` says 0.3.0. A release v0.3.0 is published on S01's word, not by this run | Finding 7; a release is S01's to ask for |
| 7 | The wiki tells the team in one place how the booking flow is protected and what to do when one of its alerts fires | The controls are spread over four milestones |

## Deliverable

- SAR-002: the security audit report of the booking flow, with the verdict of every requirement in scope, the abuse cases with their tests, and the findings with their severity and owner.
- The tests of the abuse cases that the review finds missing, and the fixes of the defects of the flow.
- The scans on the commit with the four milestones, and the rules of the scan renewed or removed.
- The load test with the booking flow.
- The verdicts on findings 3 to 6.
- Version 0.3.0, the wiki page on the protection of the booking flow, and the review records.
- The check on the live site after the last deploy, with its evidence in `docs/sqa/evidence/mil-089/`.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | SAR-002 is Accepted after its review: every requirement in scope has one verdict with its evidence; no finding of severity High or Critical in the booking flow is open; every other open finding has an issue and an owner | Yes | No |
| 2 | Each abuse case of the list names a test that passes, and for three of them, chosen in the review, a transcript shows the test failing when its control is taken out | Yes | No |
| 3 | On the commit that holds MIL-085 to MIL-088: the full scan and the scan of the API report 0 High alerts, and each new Medium alert has a reason; the code scan has no open alert in the changed code; the audit of the packages and the gate of the images pass; no rule of the scan lapses within 14 days | Yes | No |
| 4 | The load test with the booking flow passes its two thresholds on the throwaway stack, and the burst over the limits gets only refusals of the limits and no answer of the 500 kind | Yes | No |
| 5 | The coverage floor holds: 95 percent of the lines in total and 90 percent in each project | Yes | No |
| 6 | Findings 3 to 6 each have a verdict in SAR-002: fixed here, accepted with its reason and the decision record it rests on, or a finding for S01 with its severity | Yes | No |
| 7 | `Directory.Build.props` says 0.3.0; the wiki says how the booking flow is protected and what to do at an alert; the code review against the checklists of the changed files is Go; every required check of the pull request is green | Yes | No |
| 8 | After the last deploy, one run on the live site repeats the checks of MIL-085 to MIL-087 that store nothing, and of MIL-088 when its setting is on, and reads the security headers of the booking pages; the evidence is in the evidence folder | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-085, MIL-086, MIL-087, MIL-088 | The controls that are reviewed and attacked |
| MIL-027, MIL-077 | The scans of the site and of the API and the hostile-input tests, which are run again |
| MIL-036 | The weekly load test, which gets the booking flow |
| MIL-054 | The coverage floor |
| ADR-0015, ADR-0047 | The decisions that findings 4 and 5 are read against |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 31 and success criterion 34: the running application tested as an attacker would | Criteria 1 to 3 |
| BC-001 objective 43 and success criteria 46 to 48: the booking flow, measured again as a whole | Criteria 1, 2, 4 and 8 |
| BC-001 objective 40: the capacity known before guests notice | Criterion 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-04. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | SAR-002 The security review of the booking flow | Read ASVS 5.0 in the chapters of decision 1 and write the report `docs/sqa/security-audit-2026-12-booking-flow.md`: every requirement in scope with its verdict and its evidence (a test, a file, a setting), the abuse cases of decision 2 with their tests, and the findings with severity, owner and issue. Its review record | No | criteria 1 and 6 |
| 2 | The tests of the abuse cases | For each abuse case without a test, write it and see it fail without its control; fix each defect of the booking flow that the review finds, with its test; keep the transcripts of three tests that fail when their control is taken out | No | criterion 2 |
| 3 | The scans and the rules of the scan | Run the full scan of the site and the scan of the API on the commit with MIL-085 to MIL-088, read every alert, and record the reason of each new Medium one; read the code scan, the audit of the packages and the gate of the images; renew with its reason or remove each rule in `ops/security/zap-rules.tsv` that lapses on 2026-12-08 | No | criterion 3 |
| 4 | The load test with the booking flow | Extend `ops/load/nfhotel.js` and its workflow: searches for free rooms and booking requests within the limits under the thresholds of today, and a burst over the limits that gets only refusals; the test stack runs with the numbers of production | No | criterion 4 |
| 5 | The points seen outside the booking flow | Read findings 3 to 6 against the code, the decision records and the running test stack; give each its verdict in SAR-002; fix what is a defect of the booking flow; write what is not as a finding for S01 with its severity, without building it | No | criterion 6 |
| 6 | The version, the wiki page and the pull request | `Directory.Build.props` to 0.3.0; the wiki page on the protection of the booking flow, linked from Security-Controls and Alerts; the code review records against the checklists of the changed files; the commits and the pull request, which closes the issues of tasks 1 to 6 | No | criteria 5 and 7 |
| 7 | The deploy check and the gate | After the merge and S01's approval of the run of continuous deployment (CD): the one run on the live site of criterion 8 with its evidence, the gate review record against criteria 1 to 8, the row of MIL-089 in the traceability matrix, the closing of the GitHub milestone, and the summary of the whole run for S01 | No | criterion 8 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0047]: ../adr/adr-0047-guest-login.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[MIL-036]: ./mil-036-security-operations.md
[MIL-054]: ./mil-054-test-coverage.md
[MIL-077]: ./mil-077-hostile-input-against-the-api.md
[SAR-001]: ../sqa/security-audit-2026-11.md
[9f5c208]: https://github.com/NF-Hotel/NFHotel/commit/9f5c208a73c7fbd268341b25501f518f39ad568e
[6981cab]: https://github.com/NF-Hotel/NFHotel/commit/6981cabab6eac5b43b781c0cc8d0201c06baba55
