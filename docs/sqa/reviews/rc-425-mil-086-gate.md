# RC-425 Gate review of MIL-086 Booking input checked by one set of rules

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-425 |
| CrossReference | [MIL-086], [ADR-0050], [RC-411], [RC-420], [RC-421], [RC-423], [RC-424], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-086 after the deploy of 886c8e8 (CD run 44569420887) and the check of criterion 8 on the live site; Go on criteria 1 to 8; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-086] at its gate. It covers the eight Go/No-Go criteria, with this evidence:
  - the documents and the code of tasks 1 to 7 on `main` since 886c8e8 (pull request 841, merged on 2026-11-19 at
    16:31:53 UTC), reviewed in [RC-420] and [RC-421] ([ADR-0050], both Go), [RC-423] (the documents, Go) and
    [RC-424] (the code, Go-with-conditions);
  - the checks of pull request 841 on its last head (ce759ad): 60 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 44565036569, NF Hotel E2E and DAST
    44565036742, NF Hotel Ops CI 44565036770, NF Hotel Release check 44565036677, and the two runs of CodeQL;
  - the runs of the merge commit 886c8e8 on `main`, which all passed: NF Hotel CI 44567141975, NF Hotel E2E and DAST
    44567142139, NF Hotel Ops CI 44567142580 (on its second attempt, F1), NF Hotel Wiki 44567141958, NF Hotel
    Scorecard 44567142042, NF Hotel Release 44568214304 and the two runs of CodeQL;
  - the run 44569420887 of NF Hotel CD at 886c8e8, which deployed MIL-086 (below);
  - the check of criterion 8 on the live site (below, and `docs/sqa/evidence/mil-086/live-check.txt`).

  Before the gate: [RC-411] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-086]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 8).
- Language and domain: en / it (the Metadata rows of MIL-086). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI built the milestone and wrote this record. The planning session, another session of the AI, read the deploy on
  the VM; its facts are quoted here.

**The run of CD.** The AI that built this milestone approves no run of CD; S01 approves each one.

- Run 44569420887, at 886c8e8, the merge of pull request 841 (parents bac28ba and ce759ad), created at 16:44:05 UTC
  by the event `workflow_run`. Before the approval, NF Hotel Ops CI of the merge commit (run 44567142580) first ended
  in failure at 16:47:43 UTC: GitHub cancelled its job "Ops light development mode on macOS (MIL-056)" after 15
  minutes in the queue, because no hosted macOS runner took it, while the other 43 jobs had passed. The planning
  session ran the failed job again at 16:49 UTC; the second attempt passed in 20 seconds, and the run ended in
  success at 16:49:31 UTC (F1).
- At the approval every workflow run of the commit had passed, every check run of the commit was a success, skipped
  or neutral, no other deploy ran or waited, and the merge has no migration. The planning session approved the run
  from S01's account at 16:50:10 UTC, at S01's word in chat on 2026-11-19 ("du må gerne godkende deploy."), with the
  comment "Deploy of pull request #841 (MIL-086, one set of rules for the booking input): every check of the merge
  commit 886c8e8 is green." The job "Deploy to production" ran from 16:50:13 to 16:53:46 UTC and passed.
- After it, on the VM at 16:54:14 UTC: the API, the web application, nginx, the market analysis and the benchmark are
  healthy, and the logs of the API and of the web application have no line with fail, error or exception since their
  start. The running nginx has `client_max_body_size 64k`, the `proxy_read_timeout` of one hour only in the location
  of `/_blazor`, and 60 seconds in the shared snippet (read with `nginx -T`). From outside at 16:54 UTC: `/`, `/book`,
  `/booking` and `/.well-known/security.txt` answer 200, `/booking/FLZ-000001` answers 302 to `/booking`, and a POST
  of 70,000 bytes to `/booking` answers 413, with the one line "client intended to send too large body" in the log of
  nginx for it.

**The check of criterion 8.** The AI that built this milestone made it at 20:07 UTC; it stored nothing on the live
site:

- One GET of `/book` with curl: the answer 200, and the two date fields with their earliest and latest day. The
  arrival goes from 2026-11-19, the day of the hotel, to 2031-11-18, so that a stay of one night ends at most five
  years after today. The departure goes from 2026-11-21 to 2026-12-20, one night and 30 nights after the arrival that
  the page offers first (2026-11-20).
- Four stays on `/book` in Chromium without a window, driven by Playwright 1.62.0 in its container image
  (`mcr.microsoft.com/playwright/dotnet:v1.62.0-noble`), never on the desktop of the host
  (`framework/process/gui-testing.md`). The dates are typed into the fields and each field is left with Tab, as a
  guest does. The button "Find available rooms" is only read, never pressed, so no search was asked and nothing was
  booked:
  - a stay that ends five years and a day ahead (2031-11-17 to 2031-11-20): the text "The stay must end within five
    years from today." at the field, and the button disabled;
  - a stay that ends exactly five years ahead (2031-11-17 to 2031-11-19): no text, and the button enabled;
  - a stay of 31 nights (2026-12-01 to 2027-01-01): the text "A stay booked here can be at most 30 nights." at the
    field, and the button disabled;
  - a stay of 30 nights (2026-12-01 to 2026-12-31): no text, and the button enabled.
- A first run of both checks, at 19:56 and 19:58 UTC, gave the same results; its output was not kept, so the run at
  20:07 UTC is the evidence. The output is `docs/sqa/evidence/mil-086/live-check.txt`; the site stays in a variable
  there, and it holds no personal data.

## Checklist Results

| # | Criterion (MIL-086) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ADR-0050 is Accepted after its review: one implementation of each rule, the place of the table, decisions 1 to 8 with any changed number and its reason, and the alternatives with the reason each lost | Pass | [ADR-0050] is Accepted. [RC-420] is Go against QC-ADR-001 on its first version, and [RC-421] is Go on the second, which keeps every decision of the first and moves the rules to places that the architecture rules and the structure tests allow: one implementation of each rule in the domain, the table in `docs/refactor/BusinessRules.md`, decisions 1 to 8 with what the reading of the code added and why, and the alternatives with the reason each lost |
| 2 | The table names every field of the request types of the booking flow, and a test fails when a request type of the flow has a field that the table does not name | Pass | The table "Validation rules of the booking flow" is in `docs/refactor/BusinessRules.md`. `Table_EveryFieldOfTheFlow_HasARow`, `Table_EveryRow_NamesAFieldOfTheFlow`, `Table_TypeOfEachRow_IsTheTypeOfItsField`, `Table_EveryRuleInTheCode_Exists` and `FlowTypes_WhenInspected_RefuseUnknownFieldsAndBoundTheirLists` compare it with the request types both ways, and `BookingAddressRulesTableTests` holds the rows of `/book`; they passed in NF Hotel CI on the last head and on the merge |
| 3 | For every field the tests cover the value at each limit, one below and one above, and one value of each refused kind; the API refuses each with the code of the field also when it is called without the page; a request with an unknown field is refused; a body over 64 kilobytes gets 413; a booking for 5 guests is refused | Pass | `GuestFieldFormatTests`, `GuestValidationTests` and `CountriesTests` test each rule at its limits and with one value of each refused kind. Without the page, `PublicBooking_GuestField_IsAcceptedOrRefusedWithItsCode` and `GuestForm_GuestField_IsAcceptedOrRefusedWithItsCode` get the code of the field, `PublicBooking_UnknownField_IsRefused` refuses the unknown field, `PublicBooking_BodyAtAndOverTheLimit_IsReadOrRefusedWith413` and `PublicBooking_ChunkedBodyOverTheLimit_IsRefusedWith413ByTheServer` answer 413, and `PublicBooking_Party_IsOneToFour` refuses 5 guests. They passed in NF Hotel CI on the last head and on the merge. On the live site nginx answers 413 to a body of 70,000 bytes (above) |
| 4 | With a fixed clock: a stay that ends exactly five years after today is accepted and one that ends a day later is refused, for a guest and for staff, when made and when moved, also across 29 February; on the public site a stay of 30 nights is accepted and one of 31 refused, and staff can book 31; an arrival yesterday is refused | Pass | `StayLimitTests` on a fixed clock (five years and a day later, across 29 February, an arrival yesterday, 30 and 31 nights), `BookingInputRulesTests` for a guest and for staff when a booking is made and when it is moved, and at the API `Departure_FiveYearsAheadAndADayLater_IsBookedOrRefused` and `Nights_PublicThirtyAndStaffThirtyOne_AreBookedAndPublicThirtyOneIsRefused` passed in NF Hotel CI on the last head and on the merge |
| 5 | Each hostile string of the list in each text field is either refused by the rule of the field or stored unchanged, and every page that shows the value (public, guest and staff) shows it as text and creates no element from it; the two searches of the back office find a `%` and a `_` as themselves | Pass | With the shared list of hostile strings, which holds the noncharacter U+FFFE since [RC-424] F9: `GuestForm_HostileString_IsRefusedByTheFieldOrStoredUnchanged`, `Searches_HostileStrings_AreTextAndNoFault`, `Guest_HostileStringsThatMeetTheRules_AreStoredAndReadUnchanged` on PostgreSQL, the four `Render_StoredHostileValues_AreShownAsText` of the public, guest and staff pages, and the two searches that find `%` and `_` as themselves passed in NF Hotel CI on the last head and on the merge. The end-to-end tests, which [RC-424] left to CI, passed in NF Hotel E2E and DAST on the last head (44565036742) and on the merge (44567142139) |
| 6 | The hostile-input tests and the test of the public endpoints pass; the baseline scan of the pull request reports no new alert; CodeQL reports no new alert in the changed code | Pass | `HostileInputTests`, `HostileInputRegressionTests` and `EndpointProtectionTests` passed in NF Hotel CI on the last head and on the merge. The job "DAST baseline" of 44565036742 passed with no failure. ZAP calls each warning of a run new: the scan of the public site warned on rule 10027 and the scan of the back office on rules 10027 and 10029, and code scanning shows them on the merge ref of the pull request as its only two open alerts, 639 (10027) and 647 (10029), both open on `main` since 2026-11-05 and 2026-11-08, before the pull request, and taken up by [MIL-090]. The check CodeQL of the last head says "No new alerts in code changed by this pull request", and its four analyses (C#, Python, JavaScript and TypeScript, Actions) found 0 results |
| 7 | The documents say what the deliverable says; the code review against `QC-CS-001`, and against `QC-SH-001` and `QC-YML-001` for what changes under `ops/`, is Go; the coverage floor holds; every required check of the pull request is green | Pass | [RC-423] is Go on the documents. [RC-424] is Go-with-conditions against QC-CS-001 and QC-SH-001, and its condition is met (F2); QC-YML-001 does not apply, because no YAML file changed. The step "Coverage floor" of NF Hotel CI passed on the last head, and SonarCloud passed its quality gate with 92.9% coverage on the new code. 60 checks passed and 2 skipped by design |
| 8 | On the live site after the deploy: a stay that ends five years and a day ahead and a stay of 31 nights are refused with the text of the rule, the date fields of the booking page carry their earliest and latest day, and the evidence is in the evidence folder | Pass | The check above. The booking page refuses a stay that ends five years and a day ahead and a stay of 31 nights with the text of each rule at the field and the search button disabled, and offers the stays at both limits; its date fields carry their earliest and latest day (the arrival from 2026-11-19 to 2031-11-18, the departure from one night to 30 nights after the arrival). The evidence is `docs/sqa/evidence/mil-086/live-check.txt` |

## Findings

- **F1, note: a job of Ops CI that no runner took.** On the merge commit, GitHub cancelled the macOS job of NF Hotel
  Ops CI after 15 minutes in the queue, because no hosted macOS runner took it. Nothing in the code failed. The
  planning session ran the job again, it passed in 20 seconds, and the run of CD was approved only after the run was
  green.
- **F2, note: the condition of the code review is met.** [RC-424] gave Go-with-conditions: every required check of the
  pull request green before the merge. Pull request 841 merged with every check green, so the review counts as Go for
  criterion 7.
- **F3, note: the refusal by the API is not tried on the live site.** The check reads what the page does and sends no
  search, so the refusal of a stay by the API is not tried there. The tests of criteria 3 and 4 hold that refusal on
  the pull request and on the merge.
- **F4, note: three notes of the code quality tools on the tests.** SonarCloud passed its quality gate on pull request
  841 and noted one new issue of the lowest severity, Info: CA1861, a constant array as an argument, in
  `GuestServiceTests` (line 201). GitHub's code quality left two notes on the same pull request: a `ContainsKey`
  before a read of the same key in `ValidationRulesTableTests` (line 92), and a loop that could use `Where` in
  `GuestFieldErrorsTests` (line 77). None is a criterion. [MIL-087] takes them up when it edits the tests.

## Overall Verdict

Go on criteria 1 to 8, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence above:
the decision record in its second version, the table with its tests both ways, the rules at their limits through the
API without the page, the five years and the 30 nights on a fixed clock, the hostile strings stored as text or refused,
the scans with no new alert, the reviews and the checks, and the check on the live site after the deploy. The reviewer
is not independent, and each stand-in decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-086, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Take up the three notes of the code quality tools on the tests (F4) | The AI | With MIL-087 |

---

[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[RC-411]: ./rc-411-mil-086.md
[RC-420]: ./rc-420-adr-0050.md
[RC-421]: ./rc-421-adr-0050-where-the-rules-live.md
[RC-423]: ./rc-423-mil-086-documents.md
[RC-424]: ./rc-424-mil-086-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-090]: ../../milestones/mil-090-scan-findings-of-19-november.md
