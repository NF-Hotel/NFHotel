# RC-439 Review of the documents of MIL-089: the booking flow attacked and measured

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-439 |
| CrossReference | [ADR-0051], [ADR-0052], [QC-ADR-001], [QC-WIKI-001], [MIL-089], [SAR-002], [ADR-0050], [ADR-0053], [SD-001], [OC-002], [RC-415], [RC-426], [RC-429], [RC-435], [RC-328], [QC-SD-001], [QC-OC-001], [QC-DCD-001], [DM-004], [SSD-002], [OC-001], [ADR-0002], [ADR-0047], [RC-417], [RC-442], [RC-438] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-21 | Deprecated | mikemike | AI as stand-in for S01 | Initial version: review of the documents of MIL-089 (ADR-0051 third version, ADR-0052, the business rules, the wiki pages); No-Go; mode autonomous, not independent | pending |
| 2026-11-26 | Accepted | mikemike | AI as stand-in for S01 | Delta review after the fixes: Go for ADR-0052, SD-001 and OC-002; No-Go for ADR-0051 (decision 8 is not in its Context, QC-ADR-001 criterion 2) and for the class diagram on its first review (QC-DCD-001 criteria 3, 4, 6 and 7, on content older than MIL-089)<br>Second delta review after the fixes of D12, D13 and N7 to N9 and the line of the evidence notes for C4 of RC-438: Go for ADR-0051, third version; the Go for ADR-0052 holds; the class diagram stays No-Go (D14); the wiki in RC-442; mode autonomous, not independent<br>Accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

## Artifact Under Review

- Instances reviewed, each as it stands on 2026-11-21 in the working tree of the branch `mil-089-booking-flow-attacked`
  (from `main` at 3cfe3ab, not yet committed), before its pull request:
  - [ADR-0051], third version (its row of 2026-11-21, `Proposed`): decision 6 says that the security events hold the
    caller address and no email, whole, masked or hashed, and no other typed value; the limit per email of decision 3
    is named `mailbox` in the events and the counter; a new negative consequence for the trail of an incident;
    [MIL-089] in the CrossReference.
  - [ADR-0052], its row of 2026-11-21 (`Proposed`): decision 10, the event `booking_request_made` holds the address
    and no email, as decision 6 of ADR-0051 says since MIL-089.
  - The wiki: the new page Booking-Flow-Protection, and the changes of MIL-089 to Security-Controls, Alerts, Incidents
    (the log policy), Workflows, Home and `_Sidebar`.
- Checklists used: [QC-ADR-001] for the two ADRs, in one table; [QC-WIKI-001] for the wiki. The nested folder of the
  checklists is not checked out in the working tree, so both were read at the commit that the framework of the branch
  pins.
- Scope: full review. Decision 6 of ADR-0051 changed in substance, so a delta re-review is not allowed; the latest
  records of the two ADRs are [RC-429] (the second version of ADR-0051, after [RC-426]) and [RC-435] (ADR-0052). The
  wiki is checked as a whole against QC-WIKI-001, and each fact of the new and changed pages is read against the code
  and the deploy files of the branch.
- Not under a checklist, read against the deliverable of [MIL-089], the two ADRs and the code: the rule ledger
  `docs/refactor/BusinessRules.md` (the row `GuestSignInRequest.Email` and the row of the limit renamed `mailbox`),
  and the evidence notes `docs/sqa/evidence/mil-089/README.txt` against the three transcripts beside them.
- Out of scope: [SAR-002], which has a review record of its own, and the code. Where a page cites SAR-002, the
  citation was read against it.
- Language and domain: n/a for the two ADRs, a technical type in IT Professional English. The wiki has no Metadata;
  it is in English, the PO language of the project (`docs/artifact-registry.md`), so it exists once.
- Language reviewer: none; the reviewer reads English and knows the domain.
- Reviewer: AI as stand-in for S01.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat for the hardening of the booking flow and
  handed the decisions to the AI; the plan of [MIL-089] was accepted by the AI as stand-in for S01 in [RC-415]. The
  build session of the AI wrote the documents; a separate review agent of the AI, which wrote none of them, reviewed
  them and wrote this record.
- A stand-in decision under review: the change of [ADR-0051] decision 6 was decided on 2026-11-20 by the planning
  session of the AI acting as S01, so that the five alerts of the code scan on the security events close through the
  code. This record judges the record and its fit with the code, not the choice. S01 can overrule the decision; this
  review does not make it S01's own.
- Relied on, as the author ran them on this working tree: `bash ops/tests/test-wiki-coverage.sh` (133 passed, 0
  failed), `bash ops/tests/test-publish-wiki.sh` (9 passed, 0 failed), `dotnet build -c Release -warnaserror` (clean)
  and the fast test suites, `NFHotel.Api.Tests/LimitsTableTests` among them (passed).
- Checked by the reviewer: the links of the sidebar and of the six new and changed pages (127 links to a page or a
  heading, none dead; every link to a file of the repository names a path that exists on the branch); the pages of
  the wiki and the lines of the sidebar (25 and 25); the line endings of the changed files (LF); each fact of the
  pages against the code and the deploy files (below).
- Not checked: the published wiki (QC-WIKI-001 criterion 14); that the five alerts of the code scan close, which needs
  the scan of the pull request; how GitHub renders the Mermaid diagram of the new page; the load test and the four
  tests without their control were not run again (no Docker in this review).

## Checklist Results: ADR-0051, third version, and ADR-0052 (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | `ADR-0051` and `ADR-0052` match their file names; unchanged |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | Unchanged: the problem, the forces and the options A to I of ADR-0051, and the context of ADR-0052, still frame the new text. N1: the force behind the third version, the five alerts of the code scan, is named only in the Version History row |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Decision 6 names the forms it turns down, the whole email, the email masked to its first character and its domain (the choice of the first two versions) and a hash, against the one it takes, no email; why a hash lost is not argued (N1). ADR-0052 decision 10 follows ADR-0051 and makes no choice of its own |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | "Each has the address and no email: not whole, not masked [...], and not hashed. No other typed value either." It stays within option I, the chosen one. ADR-0052: "with the address and, since MIL-089, no email". The code agrees: the messages 8602 to 8606 of `SecurityEvents` take `{Address}` (8604 also `{Limit}`) and nothing else, `IBookingRequestEvents` has no parameter for an email, `LimitNames.Mailbox` is `mailbox`, and `SecurityEventValuesTests.Events_EveryLogMessage_TakesNoEmail` and `SenderLimitApiTests.Booking_Made_WritesOneEventWithTheAddressAndNoEmail` hold it |
| 5 | Consequences list both Positive and Negative outcomes | Pass | ADR-0051 adds one: the log tells the refusals apart by the address and the time only, and which mailbox a refusal was for is read in the database by staff; a log that is read or kept longer than meant holds no part of an email. N3: the database tells less than the sentence says |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Optional. Security: the confidentiality of the log, beyond the masked or hashed value that ASVS 16.2.5 allows (cited in ADR-0052), and the accountability of an incident, whose trail loses the email |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | Every entry of both lists is a link and in the CrossReference; [MIL-089] is added to the CrossReference and the link block of ADR-0051. N2: the third version changes ADR-0052 decision 10, and SD-001, OC-002 and the class diagram follow it, but ADR-0051 does not list them (F11) |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | One new row each, `Proposed`, with a Change summary and `pending` until the commit (rule 7 of `AGENTS.md`); the oldest row of each and its commit link are dropped, as the rule of the two latest rows asks; the retained rows (ADR-0051 of 2026-11-19, `Accepted`, `a574bf6`; ADR-0052 of 2026-11-20, `Accepted`, `739a25b`) are unchanged |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Optional. Neither record is superseded or deprecated; each gets a new version |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | Optional. ASVS is spelled out in the Context of ADR-0051 |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Accepted` and `Proposed`; both files keep the line of the allowed values |

## Checklist Results: the wiki (QC-WIKI-001)

The pages: Booking-Flow-Protection (new), Security-Controls, Alerts, Incidents, Workflows, Home and `_Sidebar`; the
criteria of the sidebar, the languages and the folder are read for the whole wiki.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The wiki has a `_Sidebar.md`, because the host lists subpages only through it | Pass | `docs/wiki/_Sidebar.md` |
| 2 | The sidebar starts with the wiki's title in bold, then one list of the pages in the primary language (English), `Home` first | Fail | Line 1 of `docs/wiki/_Sidebar.md` is `- [Home](Home)`: the sidebar has no title. It never had one (the history of the file in git); [RC-328] and [RC-435] passed the criterion as unchanged. The rest holds: one list, in English, `Home` first. Fix: insert `**NF Hotel**` (the title of Home) and a blank line before line 1 |
| 3 | The page list holds a link to every English page of the wiki, and no link points to a page that does not exist | Pass | 25 pages in `docs/wiki` and 25 lines in the list, the new page at line 21; every link of the sidebar resolves |
| 4 | A `**Related**` section follows the page list only when the wiki has related wikis (for example the checklists wiki for a framework wiki); each entry is a link to that wiki's Home. A wiki with nothing related has no such section | N-A | No related wiki, and no such section |
| 5 | A `**Languages**` section is the last section of the sidebar. It has one line per language, the language written in that language, linking to that language's `Home` page | Fail | The sidebar has no `**Languages**` section. The criterion has no exception, and `framework/process/wiki.md` ("The sidebar") gives every wiki the same shape, with only the `**Related**` section depending on the wiki; a wiki in English alone has one line. [RC-328] and [RC-435] read the criterion as N-A for a wiki of one language; a stand-in does not grant that exception, S01 can, and the row is then N-A with S01's decision. Fix: append after line 25 a blank line, `**Languages**`, a blank line and `- English: [Home](Home)` |
| 6 | The sidebar does not repeat the page list per language | Pass | One list |
| 7 | A sidebar produced by a generator carries a comment that says so and names the generator; the generator is changed, not the page | N-A | Written by hand |
| 8 | The wiki has a `Languages` line on each page (criterion 11), so the sidebar is not the only way to switch language | Fail | Optional, so it does not block: as criterion 11 |
| 9 | Every wiki exists in English and, as a minimum, in the language the project's registry names as the PO language; further languages are added on top, never instead of English | Pass | The PO language is English, and the wiki is in English |
| 10 | Every English page has a page in each other language of the wiki: the page sets are identical | N-A | No other language |
| 11 | Every page starts with the language line, with the current language in bold and a link to the same page in each other language | Fail | 22 of the 25 pages start with no language line, the new page among them; Appendix, Network-Isolation and Price-Suggestions have `**English**` after their title, as the framework's pages have theirs. The criterion has no exception for a wiki of one language, whose line is the bold language alone; [RC-328] and [RC-435] read it as N-A, and as for criterion 5 only S01 can grant that reading (N5). Fix: insert a blank line and `**English**` after the title (line 1) of the other 22 pages |
| 12 | A page in a language other than English is named `<Page>-<code>.md`, with a BCP 47 code; English has no suffix | Pass | No page in another language; the new page has no suffix |
| 13 | Code blocks, commands, file names and IDs stay in English in every language | Pass | |
| 14 | The sidebar, every page and every link were checked on the published wiki after the change, not only in the repository | Pending | The wiki is published from `main` after the merge (`.github/workflows/publish-wiki.yaml`); the gate of [MIL-089], task 7, checks the sidebar, the new and changed pages and their links there |
| 15 | A change to the layout rules (sections, order, naming) is made in the generator or template and in this checklist's source rules together | N-A | Optional. No rule of the layout changed |
| 16 | The wiki folder (`docs/wiki/`) is flat: every page, `Home.md` and `_Sidebar.md` included, is directly in it, and languages are told apart by file name, never by a subfolder | Pass | The new page is directly in `docs/wiki` |

What holds, read against the code and the deploy files of the branch. On Booking-Flow-Protection: the layers (nginx
takes the caller address only from the docker ranges, writes it over whatever the caller sent and refuses a body over
64 KB; the web application sends its content security policy without inline script, `Referrer-Policy: same-origin` on
every answer and `no-referrer` with no store on `/booking/verify`, registered first so that it wins; the service
token), the table of the limits against the ledger and `LimitNames`, the five alerts and their thresholds against
`ops/deploy/monitoring/prometheus/rules/security.yml`, the queries of Loki, the stack check
`ops/tests/check-e2e-stack.sh` (it runs in CI through `.github/actions/e2e-tests/action.yml` and checks that the events
have their address and no `@`), and the twelve known gaps against section 8 of [SAR-002] and their issues. On
Security-Controls: the rows of the personal data in the logs, of the security events and of the weekly load test. On
Alerts: the names of the limits, `mailbox` and `verification_mail`, as `LimitNames` has them; no limit `email` is left
under `ops/deploy/monitoring`, and `rules-test.yml` tests `mailbox`. On Incidents: the new row of the log policy, which
matches decision 6 and the messages of `SecurityEvents`. On Workflows: the row of the load test, with its schedule
(Wednesday at 02:40 UTC), `contents: read`, the token and the account in a file that only the user of the runner can
read, the two runs with the verification off and on, and the thresholds. On Home: the line to the new page. What does
not hold is D1 to D9 below.

## The documents without a checklist, against the deliverable of MIL-089

- **Rule ledger.** The row `GuestSignInRequest.Email` (line 292) now says 1 to 1,000 characters with no length of its
  own: the request type has no `StringLength`, so the bound of every text of the API applies
  (`InputRules.DefaultMaxTextLength`, 1,000, answered with `api.invalid_request`, [ADR-0050] decision 4), and an
  unknown member is refused; the row said 1 to 100, which the code did not do. The row of the limit per email is
  renamed `mailbox` (line 350), as `LimitNames.Mailbox`, with its setting, number and refusal unchanged;
  `LimitsTableTests` holds the table against the code. D10: four sentences of the notes still use the old names.
- **Evidence notes.** `README.txt` matches its three transcripts: in each run 3,445 requests of the guests and none
  failed, 95 percent within 15.3 ms with the verification off and 19.9 ms with it on, every booking answered 200 and
  every booking request 202, and the burst 65 answers, 23 refusals of the limits and no answer of the 500 kind; four
  tests without their control (cases 8, 7, 2 and 6), each failing without it and passing with it, one more than
  criterion 2 of [MIL-089] asks. The notes hold no public address and no personal data: the emails of the load test
  are of a reserved example domain, and its addresses are of the IPv6 documentation range. N6.

## Findings

- **D1, to correct: the masked email on Alerts.** `docs/wiki/Alerts.md` line 99, changed in MIL-089 for the names of
  the limits, still says that each event is a line "with the address that nginx saw and the email masked to its first
  character and its domain". Since MIL-089 the events hold no email ([ADR-0051] decision 6; `SecurityEvents` 8602 to
  8606). Fix: "with the address that nginx saw and no email, whole, masked or hashed (since MIL-089)". The list of
  kinds on the same line also lacks `booking_request_made`, `verification_failed`, `mail_not_sent` and
  `mail_fuse_blown`, which the counter has had since MIL-088.
- **D2, to correct: the back office on Booking-Flow-Protection.** `docs/wiki/Booking-Flow-Protection.md` line 65 says
  that staff read the mailbox of a refusal "in the back office, from the bookings and the requests". The back office
  shows no booking request ([ADR-0052] decision 9), and [ADR-0051] says the database. Fix: "staff read it in the
  database, from the bookings and the requests, as ADR-0051 says; the back office shows no booking request".
- **D3, to correct: every abuse case.** `docs/wiki/Booking-Flow-Protection.md` line 90 says that each abuse case has
  tests that fail when its control is taken out, and ends the list with many slow requests. Section 4 of [SAR-002]
  says of case 11 "None fails without a control" (partly controlled, BF-02). Fix: "Ten of the eleven abuse cases have
  tests that fail when their control is taken out [...] and a body that is too large; the eleventh, many slow
  requests, has none, because nothing limits it yet (BF-02)."
- **D4, should be corrected: the events on Booking-Flow-Protection.** Lines 55 to 63 say that each event is counted in
  `nfhotel_security_events_total` and holds the address of the caller. `input_refused`, a row of the table, is not
  counted (only the events of `BookingFlowEvents` are) and holds the method, the route, the status and the code, not
  the address; the table lacks `verification_failed`, which is counted and holds the address, and the two mail events,
  which hold no address. Fix: say that the events but `input_refused` are counted and hold the address, that
  `input_refused` holds the route and the code, and add a row `verification_failed`, a link of a booking request that
  does not work.
- **D5, should be corrected: the public bookings in the back office.** Line 77 says "In the back office, open the new
  pending bookings made on the public site". The back office shows neither which booking was made on the public site
  nor when a booking was made (`BookingListItemDto` and `BookingDetailsDto` have neither). Fix: say that the back
  office does not mark them and that they are found in the database by `MadeOnPublicSite`, or name the gap.
- **D6, should be corrected: the alert MailNotSent.** Line 80 calls it "a verification mail that failed". The rule in
  `security.yml` counts every `mail_not_sent`, the receipt after a verified booking too, and a receipt that does not go
  leaves the booking standing ([ADR-0052] decision 5; [ADR-0053] decision 5). Fix: "a mail of the booking flow, the
  link or the receipt, that did not go: while the verification is on and the relay takes no mail, no guest can book
  on the public site".
- **D7, nit: one code per limit.** Line 51 says "Every limit answers `429` with a code of its own"; five limits share
  `booking.too_many_requests`. Fix: "with one of three codes".
- **D8, should be corrected: the headers on Security-Controls.** `docs/wiki/Security-Controls.md` line 99, the row
  "Security headers and cookie flags", names CSP, COOP, COEP, CORP, Permissions-Policy and nosniff, but not the
  `Referrer-Policy: same-origin` that MIL-089 adds to every answer (`SecurityHeaders.ReferrerPolicy`), nor
  `no-referrer` on `/booking/verify`. Line 3: the list "Work" ends at MIL-088, and "Assessment" does not name SAR-002.
  Fix: add them.
- **D9, should be corrected: the burst on Workflows.** `docs/wiki/Workflows.md` line 152 says that the burst "sends 80
  searches and 8 bookings at once". One virtual user sends them one after the other, from 45 seconds into the run,
  while the guests book (`ops/load/nfhotel.js`, `burst`). Fix: "one after the other, while the guests book".
- **D10, should be corrected: the old names in the ledger.** In `docs/refactor/BusinessRules.md`, "the limit per
  mailbox" at line 360 (it "has no setting: its numbers are those of ADR-0052 decision 6", `MailsPerHour` and
  `MailsPerDay`) and at line 330 ("with the limit per mailbox of MIL-088") means the limit `verification_mail`, while
  the limit named `mailbox` (line 350) has the setting `SenderLimits:ComingPublicBookingsPerEmail`; lines 343 ("The
  count per email") and 358 ("The refusal per email") name the limit `mailbox` by its old sense. Fix: "The limit
  `verification_mail` has no setting" (360), "the limit `verification_mail` of MIL-088" (330), and "the limit
  `mailbox`" (343 and 358).
- **F11, outside the documents of this record: the design documents.** They still give the email to the events and
  name the limit `email`: [SD-001] (`docs/use-cases/uc-011/sd.md`) line 101, `LimitRefused("email", claimed.Email)`,
  and line 111, `PublicBookingMade(claimed.Email)`; [OC-002] (`docs/use-cases/uc-011/oc.md`) lines 97 to 98 and 163
  to 164, "the limit `email`"; the class diagram, `docs/dcd.md` lines 409, 411 and 412 and `docs/dcd.puml` lines 396,
  398 and 399, `+requestMade(email)`, `+publicBookingMade(email)` and `+limitRefused(limit, email)`. The port has
  `RequestMade()`, `PublicBookingMade()` and `LimitRefused(string limit)`. ADR-0052 names SD-001, OC-002 and the class
  diagram as documents that follow it. Fix: `LimitRefused("mailbox")`, `PublicBookingMade()`, "the limit `mailbox`",
  `+requestMade()`, `+publicBookingMade()` and `+limitRefused(limit)`; SD-001 and OC-002 each with a new row and a
  review.
- **N1, note: the reason of the third version.** Why the events drop the email, the five alerts of the code scan, and
  why a masked or hashed email is not kept although ASVS 16.2.5 allows it, is only in the Version History row of
  ADR-0051. The record keeps two rows, so the reason leaves it after two more versions; one sentence in decision 6
  would keep it.
- **N2, note: what the third version affects.** The Affected Artifacts of ADR-0051 could name [ADR-0052], whose
  decision 10 follows decision 6, and [MIL-089]; ADR-0052 is not in the CrossReference of ADR-0051.
- **N3, note: what the database can tell.** The new consequence of ADR-0051 (lines 242 to 244) says that which mailbox
  a refusal was for "is read in the database, from the bookings and the requests". A refused request is not stored,
  and the database holds no caller address, so staff can only list the mailboxes that were at their limit at that time
  (three coming public bookings; the open request and the records of the mails); a refusal cannot be tied to a
  mailbox. A sentence that says so would state the cost of the decision in full; BF-07 (no request id in the security
  log) is the open finding nearest to it.
- **N4, note: the earlier records of the wiki.** [RC-328] and [RC-435] passed criterion 2 as unchanged and read
  criteria 5 and 11 as N-A for a wiki of one language; RC-435 says of criterion 8 that no page has a language line,
  while three have. RC-435 also gave Go with criterion 14 Pending, which the review process allows only as
  Go-with-conditions.
- **N5, note: one form for the language line.** If S01 decides that a wiki of one language has no language line and
  no `**Languages**` section, the bare `**English**` of Appendix, Network-Isolation and Price-Suggestions (line 3 of
  each) should go too, so that the wiki has one form.
- **N6, note: the split of the burst.** The evidence notes say that the burst got the 60 searches and the 5 bookings
  that the limits allow; the transcripts count 65 answers and 23 refusals in all. No other split of 65 fits 60
  searches a minute and 5 booking requests an hour, so the notes hold. The row of MIL-087 in the traceability matrix
  still names a masked email: it describes MIL-087 as built and needs no change.

## Overall Verdict

No-Go. Three Mandatory criteria of [QC-WIKI-001] fail, all in the shape of the wiki and all older than MIL-089: the
sidebar has no title (2) and no `**Languages**` section (5), and 22 of the 25 pages do not start with the language
line (11). Each is a blocking finding, and a stand-in may not approve a risk exception. Criteria 5 and 11 fail on the
text of the checklist; if S01 decides that a wiki of one language is exempt, they become N-A and criterion 2 alone
blocks. The two ADRs pass every Mandatory criterion of [QC-ADR-001] and agree with the code; N1 to N3 would make the
third version of ADR-0051 clearer and block nothing. Three facts of the wiki are wrong and are to be corrected before
the re-review (D1 to D3), and seven smaller points should be (D4 to D10). After the fixes, criterion 14 stays Pending
until the gate checks the published wiki, so a re-review gives Go-with-conditions at most, and the rows of the two
ADRs then stay `Proposed` until that condition closes, unless the re-review records the ADRs and the wiki apart, so
that the ADRs can take a Go of their own. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Insert the title `**NF Hotel**` and a blank line before line 1 of `docs/wiki/_Sidebar.md` (QC-WIKI-001 2) | S01 | Before the re-review, in the pull request of `mil-089-booking-flow-attacked` |
| Append a `**Languages**` section with the one line `- English: [Home](Home)` after the page list of `docs/wiki/_Sidebar.md`, or record S01's decision that a wiki of one language has none (QC-WIKI-001 5) | S01 | Before the re-review, in the same pull request |
| Start each of the 22 pages without it with `**English**` after its title, as Appendix, Network-Isolation and Price-Suggestions do, or record S01's decision that a wiki of one language has no language line and remove the three (QC-WIKI-001 11 and 8; N5) | S01 | Before the re-review, in the same pull request |
| Correct D1 to D3 in `docs/wiki/Alerts.md` and `docs/wiki/Booking-Flow-Protection.md` | S01 | Before the re-review, in the same pull request |
| Correct D4 to D10 in the wiki pages and the rule ledger | S01 | Before the re-review, in the same pull request |
| Bring SD-001, OC-002 and the class diagram (`docs/dcd.md`, `docs/dcd.puml`) in line with ADR-0051 decision 6 and the code, SD-001 and OC-002 each with a new row and a review (F11) | S01 | Before the gate of [MIL-089], task 7 |
| Re-review the fixed documents, in a new row of this record or a new record | S01 | Before the merge of the pull request |
| Check the sidebar, every page and every link on the published wiki (QC-WIKI-001 14) | S01 | At the gate of [MIL-089], task 7 |
| Confirm or overrule the stand-in decision of ADR-0051 decision 6: no email in the security events, whole, masked or hashed | S01 | At S01's review of the summary of the run |
| Consider N1 to N3 for the text of ADR-0051 (optional) | S01 | With the fixes above |

## Delta review

The second review of this record, on 2026-11-26, after the build session fixed the findings above in the same working
tree; the documents below last changed there on 2026-11-21. "Delta" names what is read again, not a delta re-review
of the review process: the content of each document changed, so each table holds every criterion of its checklist,
and a row whose text the change did not touch carries the evidence of the record that it names. The wiki has a full
review of its own, [RC-442].

- Instances reviewed, in the working tree of the branch `mil-089-booking-flow-attacked`, not yet committed:
  - [ADR-0051], third version, its row of 2026-11-21 (`Proposed`): the reason of decision 6 now in the decision (N1),
    [ADR-0052] and [MIL-089] among the Affected Artifacts (N2), the consequence for the trail of an incident
    rewritten (N3), and a new decision 8, at most 50 open circuits per caller in the web application (finding BF-02
    and abuse case 11 of [SAR-002]), with a positive and a negative consequence and ASVS 15.1.3 and 15.2.2 among the
    quality attributes. Decision 8 is a stand-in decision of the build session, which S01 can overrule; this review
    judges the record and its fit with the code, not the choice.
  - [ADR-0052], its row of 2026-11-21 (`Proposed`): decision 10 now also says that the route of the verification has
    no limit per caller, and why (SAR-002, ASVS 6.6.3); decision 2 weighs the 30 minutes of the link against the 10
    of ASVS 6.5.5, a stand-in judgement that S01 can overrule.
  - [SD-001] and [OC-002], each with a new row of 2026-11-21 (`Proposed`): the fix of F11.
  - The class diagram, `docs/dcd.md` with its copy `docs/dcd.puml`: the three members of `IBookingRequestEvents`
    (F11). The diagram has no ID, no Metadata and no Version History, as [RC-417] and [RC-435] left it; this is its
    first review against [QC-DCD-001].
  - Not under a checklist: the rule ledger `docs/refactor/BusinessRules.md` (D10, and a new paragraph on the circuits)
    and the evidence notes in `docs/sqa/evidence/mil-089/` (a fifth test without its control).
- Checklists used: [QC-ADR-001] for the two ADRs, in one table; [QC-SD-001] for SD-001; [QC-OC-001] for OC-002;
  [QC-DCD-001] for the class diagram; each read at the commit that the framework of the branch pins.
- Read against the code of the branch: `CircuitLimit` and `CircuitLimitMiddleware` with their place in `Program.cs`,
  `CallerAddressKey`, the alert `BookingFlowRefusals` with its rule test, the client timeouts of the public listener
  of nginx with their check, the tests of `CircuitLimitTests`, the port `IBookingRequestEvents` and its calls in
  `BookingRequestService`, the route of the verification and the lifetime of its link, and each member of the class
  diagram that is not public. The two copies of the class diagram are the same text. The two circuit files and their
  tests changed in the working tree during this review (a test of the meter was added); they were read again after
  that, and the facts below hold for them.
- Not checked: the tests were not run again and the diagrams were not rendered (no Docker in this review); the changed
  lines are two messages and three members, in forms that the diagrams already use. Nothing tests what a page shows
  when its circuit is refused (D13).

### ADR-0051, third version with decision 8, and ADR-0052 (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Fail | ADR-0052 passes: its Context is unchanged, and both of its changes decide details within what it chose, how long the link lives (decision 2) and that the route of the verification has no limit per caller (decision 10). ADR-0051 fails (D12): its Context (lines 19 to 107) states the problem of the counts of the bookings and the searches, and its options end with I, counts in the API; decision 8 counts the circuits of the pages in the web application. Its problem, its forces and the option it turns down stand only in decision 8 (lines 225 to 244). [RC-435] passed the second versions of ADR-0052 and ADR-0053 on this criterion because they decided details within the options already chosen; decision 8 is a new control in another component |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | ADR-0051 decision 8 weighs `limit_conn` in nginx (lost: it would count a whole IPv6 address and the connections of every path, and write a refusal only to the error log of nginx) against a count in the web application (chosen); decision 6 names the forms of an email that it turns down, and now why. ADR-0052 weighs the 30 minutes of the link against the 10 of ASVS 6.5.5 (decision 2), and justifies no limit per caller on the route of the verification: a token of 256 random bits that works once, and requests for links that are limited per address and per mailbox (decision 10) |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | ADR-0051: "At most 50 open circuits per caller"; the web application answers a request to `/_blazor` over 50 with 429 and no body, the option chosen in decision 8. The code agrees: `CircuitLimit.OpenPerCaller` is the constant 50, and `TryOpen` refuses a caller with 50 open; `CircuitLimitMiddleware` counts only the paths under `/_blazor`, under `CallerAddressKey.Of` (an IPv6 address by its first 64 bits), answers 429 with no body, logs `limit_refused` (event 8604) with the limit `circuits` and the address, adds to `nfhotel.security.events`, and gives the place back when the request ends; `Program.cs` adds it after the redirect to HTTPS and before the authentication; `BookingFlowRefusals` counts the jobs `nfhotel-api` and `nfhotel-web`, with a rule test for `circuits`; the public listener keeps its client timeouts at 60 seconds, which `ops/tests/test-nginx.sh` checks. ADR-0052: "The link works once, for 30 minutes from the request" (`BookingRequest.LinkLifetime`) and "The route of the verification has no limit per caller" (`VerifyPublicBookingAsync` has no `SenderLimit`) |
| 5 | Consequences list both Positive and Negative outcomes | Pass | ADR-0051 adds for decision 8 a positive (line 257) and a negative (lines 274 to 277: many addresses, and an alert that sees only the refusals), and rewrites the consequence for the trail of an incident (lines 269 to 273). D13: the last sentence of the new negative says what a page shows, which nothing tests and the wiki says otherwise. The consequences of ADR-0052 are unchanged (N7) |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Optional. ADR-0051 names availability and adds ASVS 15.1.3 and 15.2.2 (line 287). The line of ADR-0052 is unchanged; the two requirements that its new text weighs, 6.5.5 and 6.6.3, are cited in decisions 2 and 10 |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | ADR-0051 lists [ADR-0052] and [MIL-089] (lines 309 to 310), both links and in its CrossReference; the list of ADR-0052 is unchanged. N8: [SAR-002] is cited by name in both, not as a link, and is in neither CrossReference |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | Each has one row for its new version, `Proposed`, with a Change summary and `pending`: the row that the first review read, amended before any commit. The retained rows are unchanged: ADR-0051 of 2026-11-19, `Accepted`, `a574bf6`; ADR-0052 of 2026-11-20, `Accepted`, `739a25b` |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Optional. Neither is superseded or deprecated |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | Optional. ASVS is spelled out in both Contexts (ADR-0051 line 47, ADR-0052 line 57) |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Accepted` and `Proposed` |

### SD-001 (QC-SD-001)

The change: `LimitRefused("mailbox")` (line 102) and `PublicBookingMade()` (line 112), and a new row of 2026-11-21
(`Proposed`, `pending`); the retained row of 2026-11-20 (`Accepted`, `739a25b`) is unchanged. The code agrees:
`BookingRequestService` calls `PublicBookingMade()` (line 212), and `LimitRefused(LimitNames.Mailbox)` (line 348)
only for the code `booking.too_many_requests`; the port has `PublicBookingMade()` and `LimitRefused(string limit)`,
with no email.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Message passing strictly follows UML sync/async/return arrow syntax | Pass | Unchanged since [RC-435]: solid arrows for the calls, dashed for the returns, the statements to PostgreSQL included. The two changed messages are solid calls to the port of the events, as `VerificationFailed()` is; the port returns nothing |
| 2 | GRASP/GoF patterns applied and explicitly annotated where used (e.g. Controller, Observer, Mediator, Factory) | Pass | Optional. The table Pattern Annotations, unchanged |
| 3 | Lifelines show activation bars matching actual processing time/call nesting | Pass | Optional. Unchanged; the calls to the port of the events have no bar, as [RC-435] read them |
| 4 | Object creation and destruction shown with correct UML notation (`create`/`destroy` messages, X on lifeline) | Pass | Unchanged: `create` with `<<create>>` for the claimed request, the guest and the booking; `destroy` where the exclusion constraint refuses the room and where the relay refuses a mail |
| 5 | Diagram realizes the postconditions of a specific Operation Contract | Pass | CO4 of [OC-002], with the table Postcondition Coverage; unchanged |
| 6 | Responsibility assignment favors low coupling/high cohesion (no god-object receiving all messages) | Pass | Optional. The section Responsibility Check, unchanged |
| 7 | Loop, alt, and opt combined fragments used correctly for conditional/repeated behavior | Pass | Unchanged; `LimitRefused("mailbox")` stays in the `opt booking.too_many_requests` of the refusal, as the code writes it |
| 8 | Each exception of the realized Operation Contract is shown as an `alt` or `opt` fragment, or its absence is justified | Pass | Optional. Unchanged since [RC-435], after its fixes |
| 9 | The Purpose names the concerns (`CN-NNN`) the Sequence Diagram answers and the stakeholders it serves, so its viewpoint is stated | Pass | Optional. CN-006 of S05, CN-007 of S06 and CN-005 of S04 |
| 10 | When the design uses a layered architecture (Clean, Hexagonal, Onion or similar), the lifelines are grouped by layer and calls across a layer boundary go through the inner layer's interface | Pass | Unchanged: the boxes Delivery, Application, Domain and Infrastructure; the service writes the events through the port `IBookingRequestEvents` of Application, which `ApiBookingRequestEvents` of the API implements |
| 11 | When the design uses a layered architecture, each layer is drawn in its fixed pastel colour: Delivery `#FADADD`, Application `#FFF3B0`, Domain `#CDEFD3`, Infrastructure `#D4E4F7` | Pass | Optional. The four colours, unchanged |

### OC-002 (QC-OC-001)

The change: the exceptions of CO2 (lines 98 to 99) and CO4 (lines 164 to 165) for three coming public bookings of a
mailbox name the limit `mailbox`, and a new row of 2026-11-21 (`Proposed`, `pending`); the retained row of 2026-11-20
(`Accepted`, `739a25b`) is unchanged.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Method signature is complete: operation name, parameter types, and return type | Pass | Unchanged: CO1 to CO6, each with its name, typed parameters and return type, as the messages of [SSD-002] |
| 2 | Preconditions explicitly list required state before execution | Pass | Unchanged, in the concepts of [DM-004] and the terms defined before the contracts |
| 3 | Postconditions explicitly describe resulting state using Larman's "instance created/associated/attribute modified" style | Pass | Unchanged |
| 4 | Exceptions and error conditions are documented, including the triggering precondition failure | Pass | The two changed exceptions still name the failing precondition (three coming bookings made on the public site for the mailbox), the code `booking.too_many_requests` and what stays; the limit is now `mailbox`, as `LimitNames.Mailbox` and the ledger (line 350) name it |
| 5 | Operation is explicitly traceable to a single SSD message | Pass | Unchanged: one contract for each of the six messages of [SSD-002] |
| 6 | Contract avoids specifying implementation/algorithmic details (declarative, not procedural) | Pass | Optional. Unchanged |
| 7 | Cross-references the Domain Model classes/associations affected by pre/postconditions | Pass | Optional. Unchanged |

### The class diagram (QC-DCD-001)

The change of MIL-089 is right: `+requestMade()`, `+publicBookingMade()` and `+limitRefused(limit)` (lines 409 to 412
of `docs/dcd.md`), as the port of the code. The criteria are read for the whole diagram; what fails is older than
MIL-089, most of it in the part of the Domain package that is older than the classes of MIL-055 to MIL-088.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | SOLID principles applied; no god classes with excessive responsibilities | Pass | No class takes over the work of another: the largest, `BookingRequestService`, coordinates one use case through eight ports, and `Booking` holds the rules of a stay as their information expert |
| 2 | Visibility markers correct and consistent (`+` public, `-` private, `#` protected) | Pass | `+` for the public members, `-` for the private ones and `~` (package) for `GuestService.newGuest`; the five members that are not public match the code (`private` and `internal`); enum literals carry none |
| 3 | Relationships correctly distinguished: Association vs Aggregation vs Composition vs Dependency | Fail | `Room "1" *-- "0..*" PriceList` (line 488) is a composition, so the room would own its price lists, while its label says that the room belongs to a list; `RoomPrice "1" --> "1" PriceList : part of` (line 491) draws a part as a plain association and leaves the choice open in a comment ("if you want strict association"); `PricingSetting "1" ..> "0..*" PriceChange` (line 509) gives a dependency multiplicities, which only the ends of an association have. Part of D14 |
| 4 | Multiplicities and navigability specified on all associations | Fail | The 26 directed associations have both; the three compositions, `Countries *-- IsoCountry` (line 485), `Room *-- RoomPrice` (487) and `Room *-- PriceList` (488), have their multiplicities but no navigability. Part of D14 |
| 5 | Applied design patterns are annotated explicitly (e.g. Singleton, Factory, Strategy) | Fail | Optional. No pattern is annotated: the stereotypes `<<static>>`, `<<record>>`, `<<attribute>>` and `<<web page>>` name kinds of class; the repositories, the unit of work and the adapters of the ports are named as patterns only in the table Pattern Annotations of [SD-001] |
| 6 | Method signatures are traceable to Operation Contracts and/or design Sequence Diagrams | Fail | The members of MIL-088 trace to [OC-002] and [SD-001] (for example `claimOpenAsync`, `issue`, `Booking.createForNewGuest`, `markMadeOnPublicSite`, `GuestService.newGuest` and the port of the events). The older ones trace to no contract and no sequence diagram: `RoomRate.change`, `PriceChange.countsOn` and `revert`, the static checks of `Booking` (`latestDeparture`, `latestPublicDeparture`, `checkPeriod`, `checkPublicNights`, `checkPublicParty`), the three of `BookingReference`, the two of `TextNormalForm`, `Countries.isName` and the five `accepts`. The project has two operation contracts, [OC-001] and OC-002, and one sequence diagram. Part of D14 |
| 7 | Class names and structure remain consistent with the Domain Model concepts they refine | Fail | Against [DM-004]: the concepts Audit event and Role have no class (the code has `StaffAuditEvent` and `StaffRole`), and Staff member is drawn as `Staff` with a first and last name, a phone number and initials, where DM-004 and the code's `StaffMember` have a user name, a password hash, active, must change password and a security stamp; `PriceList`, `RoomPrice`, `RoomPriceSnapshot`, `PriceType`, `CleaningTask`, `MaintenanceTask` and their two statuses are neither concepts of DM-004 nor classes of the code; and two multiplicities contradict it: `Guest "1" --> "1..*" Booking` (line 474; DM-004: Booking 0..*) and `Booking "1" --> "1" Room` (line 476; DM-004: Booking 0..*). Part of D14 |
| 8 | No circular dependencies between classes/packages unless explicitly justified | Fail | Optional. `Booking "1" --> "0..*" RoomPriceSnapshot` (line 475) and `RoomPriceSnapshot "0..*" --> "1" Booking` (line 495) make a cycle of two associations, with no reason given; the packages have none |
| 9 | Dependencies between classes and packages point inward: domain classes do not depend on infrastructure, delivery or framework classes | Pass | No line goes from a class of the Domain package to another package, and the repository interfaces sit in Domain. The validation attributes rest on `System.ComponentModel.DataAnnotations` of the base class library, which [ADR-0002] accepts in the domain, as [ADR-0050] says |
| 10 | The Purpose names the concerns (`CN-NNN`) the Design Class Diagram answers and the stakeholders it serves, so its viewpoint is stated | Fail | Optional. The diagram has no Purpose; it opens with notes on the old pricing and ends with an example of it in US dollars |
| 11 | When the design uses a layered architecture (Clean, Hexagonal, Onion or similar), the diagram has one package per layer, every class sits in exactly one package, the package dependencies are shown, and the inner layers own the interfaces the outer layers implement | Pass | The packages Domain, Delivery, Application and Infrastructure; the 64 classes, enums and interfaces each in one of them (32, 5, 20 and 7); the dependencies Delivery to Application, Application to Domain, and Infrastructure to Application and Domain (lines 513 to 516); every interface that a class of Delivery or Infrastructure implements belongs to Application or Domain (lines 519 to 560) |
| 12 | When the design uses a layered architecture, each layer is drawn in its fixed pastel colour: Delivery `#FADADD`, Application `#FFF3B0`, Domain `#CDEFD3`, Infrastructure `#D4E4F7` | Pass | Optional. The four packages in their colours |

### The documents without a checklist

- **Rule ledger.** D10 is fixed: the note under the table's heading names "the limit `verification_mail` of MIL-088"
  (line 330), the count and the refusal of the limit `mailbox` are named so (lines 343 and 358 to 359), the row
  `booking_hour` names the limit `mailbox` (line 348), and "The limit `verification_mail` has no setting" (line 360).
  The new paragraph "The circuits of the pages" (lines 373 to 378) matches decision 8 and the code: no row of the
  table, since it is no limit of the API; the constant `CircuitLimit.OpenPerCaller`; 429 with no body; the event
  `limit_refused` with the limit `circuits` in the log of the web application; and `BookingFlowRefusals`, which adds
  its refusals to those of the API. The row `GuestSignInRequest.Email` stands as the first review read it. N9.
- **Evidence notes.** `README.txt` (lines 16 to 21) now names five tests without their control, the fifth case 11, run
  on 2026-11-21 in the worktree of the branch. Its block in `without-their-control.txt` takes out
  `app.UseCircuitLimit();`, and `CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` fails
  (429 expected, 200 got) and passes with it; criterion 2 of [MIL-089] asks for three. The load test ran on
  2026-11-20, before the limit, and opens no circuit: `ops/load/nfhotel.js` reads three pages over HTTP and books on
  the API, with no request to `/_blazor`, so the limit does not touch its evidence. The notes hold no public address
  and no personal data: the emails are of a reserved example domain, the addresses of the IPv6 documentation range.

### The findings of the first review

- D1 to D9, in the wiki: fixed, as [RC-442] reads them: Alerts line 101 (D1); Booking-Flow-Protection lines 71 (D2),
  97 (D3: true now that case 11 has a test that fails without its limit, with BF-02 named), 60 to 69 (D4), 84 (D5),
  87 (D6) and 54 (D7); Security-Controls lines 5 and 101 (D8); Workflows line 154 (D9).
- D10, in the ledger: fixed, above.
- F11: fixed in SD-001 (lines 102 and 112), OC-002 (lines 98 to 99 and 164 to 165) and the class diagram (lines 409
  to 412 of `docs/dcd.md`, and its copy); SD-001 and OC-002 each have a new row, reviewed above.
- N1 to N3: resolved in ADR-0051: the reason in decision 6 (lines 209 to 214); ADR-0052 and MIL-089 in the Affected
  Artifacts (lines 309 to 310) and the CrossReference; the consequence for the trail (lines 269 to 273).
- N4 and N6 stand as notes, with nothing to change. N5 is moot: every page now has the language line.
- The Fails of QC-WIKI-001 criteria 2, 5, 11 and 8: fixed; see [RC-442].
- The Action Items above: the first six are done; the re-review is this section and [RC-442]; the check of the
  published wiki and S01's word on decision 6 stay open; N1 to N3 were taken.

### Findings of the delta review

- **D12, blocking for ADR-0051: decision 8 is not in the Context** (QC-ADR-001 criterion 2). The Context (lines 19 to
  107) states the problem of the counts of the bookings and the searches, and its options end with I, counts in the
  API; decision 8 counts the circuits of the pages in the web application, a new control in another component. Its
  problem, its forces and the option it turns down stand only in decision 8 (lines 225 to 244). Fix: in the Context,
  a bullet under "The problem" (one caller can hold circuits of the pages, each a request to `/_blazor` with memory
  in the one container of the web application, until the site stops, the back office included; abuse case 11 and
  BF-02 of SAR-002), the force (the guests and the staff behind one address, the network of the hotel among them,
  need room), ASVS 15.1.3 and 15.2.2 under "What the texts say", and two options: J, `limit_conn` in nginx, lost for
  the reasons that decision 8 gives, and K, a count of the open circuits per caller in the web application, chosen.
  Decision 8 then keeps the decision and its details.
- **D13, should be corrected: what a refused page shows.** ADR-0051 lines 276 to 277 say that more than 50 tabs behind
  one address "get no circuit, and the page says that it cannot connect"; the wiki says that the page "does not react
  until another tab is closed" (`docs/wiki/Booking-Flow-Protection.md` line 54, [RC-442] W1). The test
  `CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` shows only that the page opens (200)
  while the negotiation of its circuit gets 429, and that the negotiation passes once a place is free. Fix: in both,
  what the test shows (the page opens but gets no circuit, so it does not work; once another tab is closed, it gets
  its circuit when it is loaded again), or a test of what the page shows, and its words.
- **D14, blocking for the class diagram: its older part.** QC-DCD-001 criteria 3, 4, 6 and 7 fail (Mandatory), and 5,
  8 and 10 (Optional), on content older than MIL-089: the Domain package keeps a design of prices and housekeeping
  (`PriceList`, `RoomPrice`, `RoomPriceSnapshot`, `CleaningTask`, `MaintenanceTask`, `Staff`) that neither DM-004
  nor the code has, with its own types (`UUID`, `LocalDate`, `BigDecimal`, prices in US dollars), beside the classes
  of MIL-055 to MIL-088, which have their counterparts in the code. The diagram also has no ID, Metadata or Version
  History, which the catalog gives the type DCD. Fix, outside the plan of [MIL-089] and for S01 to plan: redraw the
  Domain package from DM-004 and the code, give the compositions that stay their navigability (`*-->`), name the
  patterns and the Purpose, trace the members to contracts or sequence diagrams as those are written, and give the
  diagram its ID and Version History.
- **N7, note: what the 30 minutes cost.** ADR-0052 decision 2 keeps the 30 minutes of the link against the 10 of ASVS
  6.5.5 because a mail can be slow (lines 121 to 123), and the consequence on a copy of the link (lines 267 to 269)
  says that working once and for 30 minutes limits what a copy is worth. Neither says what a copy pressed in time
  gives its holder: the booking that the guest asked for and, as decision 5 says, the guest session of that booking
  ([ADR-0047]), once. One sentence would state the cost of the 20 minutes more.
- **N8, note: SAR-002 is no link.** ADR-0051 (decision 8 and its negative consequence) and ADR-0052 (decisions 2 and
  10) cite SAR-002 by name only; a link and an entry in each CrossReference would let a reader follow it, as for
  MIL-089.
- **N9, nit: line lengths.** ADR-0052 line 224 has 121 characters, and the ledger lines 331 and 360 have 123 and 128,
  while the prose around them wraps at 120; nothing checks it.

### Delta verdict

Given per document, because each has its own table and its own row:

- [ADR-0052], [SD-001] and [OC-002]: Go. Every Mandatory criterion of their checklists passes, and each agrees with
  the code. Their rows of 2026-11-21 can become `Accepted`, with "Go in RC-439", and the rows before them
  `Deprecated`. N7 to N9 block nothing.
- [ADR-0051], third version: No-Go. Criterion 2 of [QC-ADR-001] fails (D12): decision 8 answers a problem that the
  Context does not state. The rest of the third version passes, the fixes of N1 to N3 included; the fix is a few
  lines in the Context, with D13, and then a re-review of ADR-0051. Its row stays `Proposed`.
- The class diagram: No-Go on its first review against [QC-DCD-001]: the Mandatory criteria 3, 4, 6 and 7 fail (D14),
  all on content older than MIL-089, while the change of MIL-089 is right. The diagram has no row to carry the
  verdict, so it holds back none of the documents above; its rework is outside the plan of [MIL-089] and is S01's
  to plan.
- The rule ledger and the evidence notes meet the deliverable of [MIL-089].

The reviewer is not independent.

### Delta action items

| Action | Owner | Due |
| --- | --- | --- |
| Add the problem, the force and the options of decision 8 to the Context of ADR-0051 (D12, QC-ADR-001 criterion 2), and correct its sentence on the refused page (D13) | S01 | Before the merge, in the pull request of `mil-089-booking-flow-attacked` |
| Re-review ADR-0051 after that fix, in a new row of this record or a new record | S01 | Before the merge, in the same pull request |
| Set the rows of 2026-11-21 of ADR-0052, SD-001 and OC-002 to `Accepted` with "Go in RC-439", and the rows before them to `Deprecated` | S01 | Before the pull request is opened |
| Plan the rework of the class diagram (D14): the Domain package from DM-004 and the code, the navigability of its compositions, its patterns and Purpose, the traceability of its members, and its ID and Version History | S01 | S01's decision, outside MIL-089 |
| Confirm or overrule the stand-in decisions of ADR-0051 decision 8 (at most 50 open circuits per caller) and of ADR-0052 decision 2 (30 minutes against the 10 of ASVS 6.5.5) | S01 | At S01's review of the summary of the run |
| Consider N7 to N9 (optional) | S01 | With the fix of D12 |

### Second delta review

The third reading of these documents, on 2026-11-26, after the build session fixed D12 and D13 and took N7 to N9 in
the same working tree, and after one line of the evidence notes changed for C4 of [RC-438]. Each changed file was
compared with the copy that the delta review read, so that every change is seen. As in the delta review, the table
holds every criterion of its checklist. The line numbers of the sections above are those of the versions that they
read; this subsection cites the current lines. One citation above was wrong and is corrected: N7 names the lines 267
to 269 of the consequence on a copy of the link, where it said 266 to 268.

- Instances reviewed, in the working tree of the branch `mil-089-booking-flow-attacked`, not yet committed:
  - [ADR-0051], third version, its row of 2026-11-21 (`Proposed`), amended again before any commit: decision 8 in the
    Context (D12), the 51st tab in the words of the test (D13), and [SAR-002] as a link and in the CrossReference
    (N8).
  - [ADR-0052], its row of 2026-11-21 (`Proposed`), amended again before any commit: what a copy of the link pressed
    in time gives its holder (N7), [SAR-002] as a link and in the CrossReference (N8), and line 224 wrapped again
    (N9). Its content changed after the Go of the delta review, so that Go is read again.
  - Not under a checklist: the rule ledger `docs/refactor/BusinessRules.md`, two passages wrapped again (N9), and the
    evidence notes `docs/sqa/evidence/mil-089/README.txt`, line 8 (C4 of RC-438).
- Checklist used: [QC-ADR-001], for the two ADRs in one table, read at the commit that the framework of the branch
  pins.
- Read against: the text of ASVS 5.0 for 15.1.3 and 15.2.2, as read for [MIL-089] on 2026-11-20; decision 5 of
  ADR-0052; the code and the tests of the circuits (`CircuitLimit`, `CircuitLimitMiddleware` and
  `CircuitLimitTests`), unchanged since the delta review read them; [SAR-002], whose citations of ADR-0051 moved with
  its lines (its rows of 15.1.3 and 16.2.5); and the counter of the security events of the API in production, read
  on 2026-11-26.
- Unchanged since the delta review: [SD-001], [OC-002] and the class diagram; their verdicts above stand.
- Not checked: the tests were not run again (no Docker in this review).

#### ADR-0051, third version, and ADR-0052 (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged: `ADR-0051` and `ADR-0052` match their file names |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | D12 is fixed: the Context of ADR-0051 now frames decision 8. "Who decides" names its source (line 26); "The problem" states it: the booking page and the back office run in circuits, each holding a request to `/_blazor` and memory in the one container of the web application, and nothing counts them per caller (lines 46 to 50, abuse case 11 and BF-02 of [SAR-002]); "What the texts say" adds ASVS 15.1.3 and 15.2.2 as ASVS 5.0 words them, with the date they were read (lines 60 to 62); "Forces" adds the tabs of the guests and the staff behind one address (lines 83 to 85); the options add J, `limit_conn` in nginx, lost, and K, a count of the open circuits per caller in the web application, chosen (lines 118 to 123). Each new bullet carries its own "since MIL-089" or date beside the readings of 2026-11-19. Decision 8 keeps the decision and its details (lines 243 to 258). ADR-0052: its Context is unchanged, and its new sentence (N7) states a cost of decision 2, not a new choice |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | ADR-0051: J and K, each with the reason it lost or won (lines 118 to 123), beside A to I; decision 8 points to J (line 257); decision 6 as the delta review read it. ADR-0052: unchanged |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | ADR-0051: the Decision opens with option I and, since MIL-089, option K (lines 127 to 130); decision 8, "At most 50 open circuits per caller" (line 243), names option K, and the code agrees as the delta review read it. ADR-0052: unchanged; decisions 2 and 10 gain only a link |
| 5 | Consequences list both Positive and Negative outcomes | Pass | ADR-0051, for decision 8: the positive of line 271 and two negatives, many addresses with an alert that sees only the refusals (lines 288 to 290), and the 51st open tab (lines 291 to 292), which "gets its page but no circuit, so the page does not work; once another tab is closed, it gets its circuit when it is loaded again": what `CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` shows, and the words of the wiki ([RC-442]), so D13 is fixed. ADR-0052: the consequence on a copy of the link (lines 267 to 272) now says what a copy pressed in time gives its holder, once: the booking asked for, the receipt with its reference and the guest session of that booking, as decision 5 says, and that the 20 minutes beyond the 10 of ASVS 6.5.5 widen that time; N7 is taken |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Optional. ADR-0051 line 302: security, ASVS 15.1.3 and 15.2.2 among it, and availability; ADR-0052 line 282, unchanged |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | ADR-0051: every entry of its list (lines 308 to 325), [ADR-0052] and [MIL-089] among them (lines 324 and 325), is a link and in its CrossReference (line 7). ADR-0052: its list is unchanged. [SAR-002] is now a link in both (ADR-0051 lines 26, 46, 244 and 290; ADR-0052 lines 122 and 230), defined (ADR-0051 line 347, ADR-0052 line 334) and in each CrossReference, so N8 is taken; it is the source of decision 8 and of the weighings of decisions 2 and 10, not an artifact that they change, so neither list needs it |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | Each keeps one row for its new version, `Proposed`, `pending`: the row of 2026-11-21 that the delta review read, amended again before any commit with one more line ("After the delta review of RC-439: ..."). The retained rows are unchanged: ADR-0051 of 2026-11-19, `Accepted`, `a574bf6`; ADR-0052 of 2026-11-20, `Accepted`, `739a25b` |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Optional. Neither is superseded or deprecated |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | Optional. ASVS and OWASP are spelled out in the Context of ADR-0051 (lines 52 to 53), ASVS in that of ADR-0052 (line 57) |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Accepted` and `Proposed`; both files keep the line of the allowed values (line 15) |

#### The documents without a checklist, read again

- **Rule ledger.** N9 is fixed: the note under the heading "Limits of the booking flow" (lines 330 to 334) and the
  paragraph "The text of a refusal" (lines 356 to 366) are wrapped within 120 characters, and a comparison word by
  word with the copy that the delta review read finds no word changed. The text that the reviews cite keeps its
  lines (330, 343, 348, 350 and 358 to 360); the paragraph "The circuits of the pages" is now lines 374 to 379,
  unchanged, where the delta review read 373 to 378. The prose lines over 120 characters still in the ledger (lines 7
  to 206) are older than MIL-089.
- **Evidence notes.** `README.txt` line 8 now says that production ran with the verification off "until 2026-11-22",
  where it said "until its relay is set" (C4 of [RC-438]); nothing else changed. The date agrees with [SAR-002] (lines
  61 to 62) and with production: since the start of its process on 2026-11-22, the counter of the security events of
  the API counts booking requests, which exist only with the verification on. The notes still hold no public address
  and no personal data.

#### The findings of the delta review, read again

- D12: fixed; criterion 2 above.
- D13: fixed in ADR-0051 lines 291 to 292, in the words of what the test shows; the wiki says the same (W1 of
  [RC-442]).
- D14: stands. The class diagram did not change; its rework is outside the plan of [MIL-089] and S01's to plan.
- N7: taken, ADR-0052 lines 269 to 272.
- N8: taken; criterion 7 above.
- N9: fixed. ADR-0052 line 224 has 115 characters, and no prose line of the two ADRs is over 120; the ledger as above.
- The delta action items: the fix of D12 and D13 and the re-review of ADR-0051 are done (this subsection), and N7 to
  N9 were taken. The rows, the class diagram and S01's word on the stand-in decisions stay open, below.

#### Findings of the second delta review

None in ADR-0051, ADR-0052, the rule ledger or the evidence notes. The wiki has two, W3 and W4 in [RC-442], of the
kind that C4 of [RC-438] found: a line that still speaks of production as if the verification were off.

#### Second delta verdict

Given per document:

- [ADR-0051], third version: Go. Every Mandatory criterion of [QC-ADR-001] passes; D12 and D13 are fixed, and N8 is
  taken. Its row of 2026-11-21 can become `Accepted`, with "Go in RC-439", and its row of 2026-11-19 `Deprecated`.
- [ADR-0052]: the Go of the delta review holds for its row as amended again. The changes take N7 to N9, change no
  decision and fail no criterion. Its row of 2026-11-21 can become `Accepted`, with "Go in RC-439", and its row of
  2026-11-20 `Deprecated`.
- The rule ledger and the evidence notes meet the deliverable of [MIL-089].
- [SD-001] and [OC-002] keep their Go, and the class diagram its No-Go (D14), as the delta verdict says; none of them
  changed.
- The wiki: Go-with-conditions, in [RC-442].

For C1 of [RC-438], the Go that it waits for is given; the rows are still to be set. Decisions 6 and 8 of ADR-0051
and the weighings of decisions 2 and 10 of ADR-0052 (ASVS 6.5.5 and 6.6.3) stay stand-in decisions that S01 can
overrule. The reviewer is not independent.

#### Second delta action items

| Action | Owner | Due |
| --- | --- | --- |
| Set the rows of 2026-11-21 of ADR-0051, ADR-0052, SD-001 and OC-002 to `Accepted` with "Go in RC-439", and the rows before them to `Deprecated` (for the two ADRs, C1 of [RC-438]) | S01 | Before the pull request is opened |
| Plan the rework of the class diagram (D14), as the delta action items say | S01 | S01's decision, outside MIL-089 |
| Confirm or overrule the stand-in decisions: ADR-0051 decisions 6 and 8, and the weighings of ADR-0052 decisions 2 and 10 (ASVS 6.5.5 and 6.6.3) | S01 | At S01's review of the summary of the run |

---

[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[SAR-002]: ../security-audit-2026-12-booking-flow.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[SD-001]: ../../use-cases/uc-011/sd.md
[OC-002]: ../../use-cases/uc-011/oc.md
[RC-415]: ./rc-415-mil-089.md
[RC-426]: ./rc-426-adr-0051.md
[RC-429]: ./rc-429-adr-0051-second-version.md
[RC-435]: ./rc-435-mil-088-documents.md
[RC-328]: ./rc-328-mil-071-task-5-wiki-and-traceability.md
[QC-SD-001]: ../../../framework/qc/artifacts/qc-sequence-diagram.md
[QC-OC-001]: ../../../framework/qc/artifacts/qc-operation-contract.md
[QC-DCD-001]: ../../../framework/qc/artifacts/qc-dcd.md
[DM-004]: ../../domain-model.md
[SSD-002]: ../../use-cases/uc-011/ssd.md
[OC-001]: ../../use-cases/uc-010/oc.md
[ADR-0002]: ../../adr/adr-0002-validation-attributes-on-entities.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-442]: ./rc-442-mil-089-wiki.md
[RC-438]: ./rc-438-sar-002.md
