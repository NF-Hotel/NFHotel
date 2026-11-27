# RC-441 Gate review of MIL-089 The booking flow attacked and measured

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-441 |
| CrossReference | [MIL-089], [SAR-002], [ADR-0051], [ADR-0052], [RC-415], [RC-438], [RC-439], [RC-440], [RC-442], [QC-MIL-001], [QC-WIKI-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-089 after the deploy of dcbc88ce (CD run 45889641080) and the check of criterion 8 on the live site; the conditions of RC-438, RC-440 and RC-442 closed; SAR-002 accepted with one sentence corrected; Go on criteria 1 to 8; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-089] at its gate, and [SAR-002] for its acceptance. The gate covers the eight Go/No-Go
  criteria, with this evidence:
  - the documents and the code of tasks 1 to 6 on `main` since dcbc88ce (pull request 882, merged on 2026-11-26 at
    20:03:19 UTC), reviewed in [RC-438] ([SAR-002]), [RC-439] (the documents), [RC-440] (the code) and [RC-442] (the
    wiki);
  - the checks of pull request 882 on its last head (ce1beba9): 60 passed and 2 skipped, which skip by design (OWASP
    ZAP and DAST full scan). The runs of that head all passed: NF Hotel CI 45852243847, NF Hotel E2E and DAST
    45852244556, NF Hotel Ops CI 45852243971, NF Hotel Release check 45852243642, CodeQL 45852239627, code quality
    45852239636, and NF Hotel load test 45852269014, started by hand;
  - the runs of the merge commit dcbc88ce on `main` (parents 3cfe3ab and ce1beba9), which all passed: NF Hotel CI
    45888194194, NF Hotel E2E and DAST 45888194103, NF Hotel Ops CI 45888194209, NF Hotel Wiki 45888194147, NF Hotel
    Scorecard 45888194157, NF Hotel Release 45888898155, CodeQL 45888193503 and code quality 45888193544, and the
    scheduled run of CodeQL 45891706159;
  - the test results that those runs keep: the artifact `test-results-45888194194-1` of NF Hotel CI (nine result
    files) and `e2e-results` of NF Hotel E2E and DAST (the browser tests with the verification off and on);
  - the run 45889641080 of NF Hotel CD at dcbc88ce, which deployed MIL-089 (below);
  - the check of criterion 8 on the live site (below, and `docs/sqa/evidence/mil-089/live-check.txt`);
  - the published wiki (below).

  Before the gate: [RC-415] (the plan, Go).
- Checklist used: the Go/No-Go criteria of [MIL-089]; [QC-MIL-001] criterion 2 makes them the checklist of the gate.
  For the one sentence of [SAR-002] that the gate corrects, criterion 20 of [RC-438] (the facts and the citations are
  accurate); for the published wiki, criterion 14 of [QC-WIKI-001], which [RC-442] left Pending until this gate.
- Scope: the gate (task 7), the conditions that the reviews of tasks 1 to 6 left to it, and the acceptance of SAR-002.
- Language and domain: en / it (the Metadata rows of MIL-089 and SAR-002). The gate adds a row to the Version History
  of MIL-089, and a row to that of SAR-002 with one corrected sentence. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). The
  AI that built the milestone made the checks of the gate and wrote this record; no other reviewer read it.

**The run of CD.** The AI that built this milestone approves no run of CD; S01 approves each one.

- Run 45889641080, at dcbc88ce, created on 2026-11-26 at 20:14:58 UTC by the event `workflow_run` after NF Hotel
  Release 45888898155. S01 approved it on GitHub from their own account, with an empty comment, for the environment
  `production`, and told the build session in chat at about 03:58 UTC on 2026-11-27: "Godkendt og er grøn". The job
  "Deploy to production" ran from 03:37:30 to 03:41:31 UTC and passed.
- Its log: the six images of the release verified by their digests; the operations commands of dcbc88ce installed; a
  backup of the database and of the settings before the migrations and again before the deploy; the migration script
  applied as `nfhotel_migrator`; the services started from the release images; the runtime of the 12 containers
  checked against `container-profiles.tsv` (MIL-080), 12 passed; "deployed dcbc88ce"; and the container check
  service passed.

**The check of criterion 8.** The AI that built this milestone made it on 2026-11-27 between 04:08 and 04:14 UTC. It
booked nothing, asked for no booking and sent no mail; what it left are four security events and their log lines.
The output is `docs/sqa/evidence/mil-089/live-check.txt`; the site stays in a variable there, the address of the
workstation is not printed, and it holds no personal data.

- The counter `nfhotel_security_events_total`, read from Prometheus through Grafana at 04:08:42 UTC: only
  `mail_fuse_blown` and `mail_not_sent` of the API, both 0.
- Part 1, with curl from WSL, 04:09:22 to 04:09:25 UTC: 41 checks, all passed.
  - The security headers of `/`, `/book`, `/booking` and `/booking/verify`: each answers 200 with
    `Strict-Transport-Security: max-age=2592000`, the Content Security Policy of MIL-033 with `frame-ancestors 'none'`,
    `X-Content-Type-Options: nosniff`, `Permissions-Policy` and the three cross-origin policies, and with
    `Referrer-Policy: same-origin`, or `no-referrer` on `/booking/verify`, as MIL-089 set them (SAR-002 3.4.5).
  - MIL-085 without a booking: the sign-in page answers, has its form and names no booking number; an old address with
    a booking number goes to the empty sign-in (302); a made-up reference with a made-up email at `example.invalid`,
    and then the old number FLZ-000001, each get the one refusal and no guest cookie. These are two of the twenty
    failed tries that an address has in 15 minutes.
  - MIL-086: the date fields of `/book` offer an arrival from the day of the hotel to the day before five years from
    it (2026-11-27 to 2031-11-26) and a departure from a night to 30 nights after the arrival; the last step of the
    page is "Check your inbox", which the page names only when the API says that the verification is on.
  - MIL-088, whose setting is on: `/booking/verify` answers with its form, the field of the token and the button
    disabled until the script fills it; a made-up token of the right form, 43 characters of base64url, is refused with
    the text of a link that does not work, and sets no guest cookie. It claims no request and sends no mail.
- Part 2, the four stays of MIL-086 in Chromium without a window, driven by Playwright 1.62.0 in its container image
  (`mcr.microsoft.com/playwright/dotnet:v1.62.0-noble`), never on the desktop of the host
  (`framework/process/gui-testing.md`), 04:11:18 to 04:11:30 UTC; the button "Find available rooms" is only read:
  - a stay that ends five years and a day ahead is refused with "The stay must end within five years from today.",
    and one that ends exactly five years ahead is offered;
  - 31 nights are refused with "A stay booked here can be at most 30 nights.", and 30 nights are offered.
- The counter at 04:11:37 UTC: `guest_lookup_failed` 2, the two failed sign-ins, and `verification_failed` 1, the
  refused token. The page shows the same text whether the verification is on or off, but the API counts
  `verification_failed` only when it is on; with it off, it answers `booking.not_found` and writes no event
  (`BookingRequestService.VerifyAsync`). So the token went the way of a setting that is on.
- Part 3, the searches of MIL-087, in the same container, 04:12:00 to 04:13:10 UTC; no room type was chosen and the
  form of the guest was never reached:
  - searches 1 to 60 each showed the room step, the 60th 11.2 seconds after the first press;
  - search 61, 11.4 seconds after the first press: refused, and the page stays on the dates with the one text of the
    limits, "Too many requests in a short time. Please wait a while and try again, or contact the hotel.";
  - one search 65.9 seconds after the first press, when the window of a minute was over: the room step again.
- The counter at 04:13:15 UTC: a new series `limit_refused` with `limit="search"` at 1, the refusal of search 61, with
  the three series above unchanged. At 04:13:22 UTC no alert of the booking flow (`BookingFlowRefusals`,
  `PublicBookingSurge`, `MailNotSent`, `MailFuseBlown`, `MailVolumeHigh`) was pending or firing.
- Not tried on the live site, as in the gates of MIL-087 and MIL-088: the limit on booking requests and the limit per
  mailbox, because a request that passes makes a booking request and sends a mail; and the limit of 50 open circuits
  per caller, which criterion 8 does not ask for. The throwaway stack, the browser tests and the load test hold them.

**X-Frame-Options.** In part 1 only the answer to `/` carried `X-Frame-Options: SAMEORIGIN`, and `/` was the first page
of the run. Two GETs of `/booking`, `/book` and `/` each, at 04:10:43 UTC, first with an empty cookie jar and then with
the jar of the first: each first answer set the antiforgery cookie and carried `X-Frame-Options: SAMEORIGIN`, and each
second answer carried neither. So the antiforgery of ASP.NET Core writes the header on the answer that sets a new
antiforgery cookie, as it does by default; the application sets no such header of its own. Every answer carries
`frame-ancestors 'none'`, which a browser that knows it obeys over X-Frame-Options, so nothing can frame a page. But
SAR-002 said that X-Frame-Options "is not used" (F4).

**The published wiki (QC-WIKI-001 criterion 14).** The workflow NF Hotel Wiki (run 45888194147) published the wiki at
the merge, as its commit 431ff29e of 2026-11-26 20:03:30 UTC, "Publish docs/wiki of NF-Hotel/NFHotel@dcbc88c".

- A clone of the published wiki holds exactly the 28 files of `docs/wiki` at dcbc88ce, 26 pages with the sidebar and
  two pictures, each with the same git blob; the wiki has no other file.
- Every page answers on GitHub: 24 with 200, and Home through GitHub's redirect to `/wiki`, then 200. Each carries the
  sidebar of the wiki, `/wiki` included, and the sidebar names every page, Booking-Flow-Protection among them.
- Every link outside code on the published pages, 707 in all, the same count as [RC-442] found in the repository:
  - 351 to a page or a heading of the wiki and one to a picture: each names a page, a heading or a file of the wiki;
  - 235 to a file or a folder of the repository on `main`: each exists at dcbc88ce;
  - 12 to issues, #867 to #878: each exists and is open;
  - 108 to other addresses, 101 different ones, opened two seconds apart from 04:19:23 to 04:26:06 UTC: 92 answered
    200, among them the 81 threads of Hacker News; the 8 questions of Stack Exchange answered 403 to a client without
    a browser and were each found through the API of Stack Exchange; and the dashboard of Cloudflare, a sign-in page,
    answered 403 to such a client too.
- On the published page Booking-Flow-Protection, GitHub renders the Mermaid diagram of the layers, from the guest
  through Cloudflare and the tunnel, nginx and the web application to the API and PostgreSQL; seen on GitHub in a
  browser at 04:19 UTC.

## Checklist Results

| # | Criterion (MIL-089) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | SAR-002 is Accepted after its review: every requirement in scope has one verdict with its evidence; no finding of severity High or Critical in the booking flow is open; every other open finding has an issue and an owner | Pass | [SAR-002] is Accepted at this gate (its row of 2026-11-27), after [RC-438] (Go-with-conditions in its second delta) and the closing of its conditions C1, C2 and C4 (F1). Each of the 179 requirements in scope has one verdict with its evidence: 92 met, 49 not applicable, 6 accepted and 32 open. No finding of severity High or Critical is open, inside or outside the flow: BF-01 is Medium as production runs, with the verification on since 2026-11-22, which the check of criterion 8 shows on; BF-02 is Medium after the fix of this milestone. Each of the twelve findings, BF-01 to BF-12, has an issue, #867 to #878, all open, and the owner S01. One sentence of requirement 3.4.6 is corrected here (F4) |
| 2 | Each abuse case of the list names a test that passes, and for three of them, chosen in the review, a transcript shows the test failing when its control is taken out | Pass | Section 4 of SAR-002 lists the eleven abuse cases, each with the tests of its control. Every test that SAR-002 names, 113 in all, passed on the merge commit (F1). `docs/sqa/evidence/mil-089/without-their-control.txt` holds five transcripts, of cases 8, 7, 2, 6 and 11: each test fails with its control taken out of the source and passes with it in place |
| 3 | On the commit that holds MIL-085 to MIL-088: the full scan and the scan of the API report 0 High alerts, and each new Medium alert has a reason; the code scan has no open alert in the changed code; the audit of the packages and the gate of the images pass; no rule of the scan lapses within 14 days | Pass | Section 5 of SAR-002, on 3cfe3ab: the full scan 0 High and 1 Medium (rule 40025, not new, with its reason; MIL-090 handles it); the scan of the API 0 High and 0 Medium; the audit of the packages (run 44692344259) and the gate of the images (run 44694949711) passed. The rules of the scan hold until 2026-12-18, 21 days after this gate. The code scan: the five alerts 677 to 681 in the changed code are closed through the code, none dismissed; the run of CodeQL on the merge (45888193503) marked them fixed at 20:06:15 UTC on 2026-11-26, and no alert of CodeQL is open on `main` or on the merge ref of pull request 882 (F5) |
| 4 | The load test with the booking flow passes its two thresholds on the throwaway stack, and the burst over the limits gets only refusals of the limits and no answer of the 500 kind | Pass | NF Hotel load test, run 45852269014, by hand on ce1beba9, the last head of pull request 882, on a throwaway stack with the numbers of production. With the verification off: 0 of 3,553 requests failed, 95 percent within 7.55 ms. With it on: 0 of 3,549 failed, 95 percent within 10.09 ms. The thresholds are 1,000 ms and 5 percent. The burst over the limits got 65 answers and 23 refusals of the limits in each run, and no answer of the 500 kind. The runs of the evidence folder (2026-11-20) agree |
| 5 | The coverage floor holds: 95 percent of the lines in total and 90 percent in each project | Pass | The step "Coverage floor" of NF Hotel CI, which checks 95 percent of the lines in total and 90 percent for each project (MIL-054), and the step that tests that check, passed on the last head (45852243847) and on the merge (45888194194) |
| 6 | Findings 3 to 6 each have a verdict in SAR-002: fixed here, accepted with its reason and the decision record it rests on, or a finding for S01 with its severity | Pass | Section 7 of SAR-002: finding 3, `/_blazor` outside the Access check, Accepted on [ADR-0029] decision 4 and [ADR-0005]; finding 4, the routes of the mobile app, a finding for S01, Medium, outside the flow (BF-03, #869); finding 5, the guest token, Accepted on [ADR-0047] decisions 2, 5 and 7, with two tests of a guest token after a cancellation; finding 6, the two operations with only the service token, Accepted on [ADR-0013], [ADR-0014] and [ADR-0004]. Each names its reason and its tests, which passed (criterion 2) |
| 7 | `Directory.Build.props` says 0.3.0; the wiki says how the booking flow is protected and what to do at an alert; the code review against the checklists of the changed files is Go; every required check of the pull request is green | Pass | `Directory.Build.props` says 0.3.0. The wiki page Booking-Flow-Protection says how the flow is protected and what to do when each of its alerts fires, and Security-Controls and Alerts link it; [RC-442] is Go-with-conditions, and its one Pending criterion, 14, passes on the published wiki (above), so it counts as Go. [RC-440] is Go-with-conditions against QC-CS-001, QC-SH-001, QC-YML-001 and QC-WF-001, and both its conditions are met (F1); the two commits after its round 3 are read in F3. Every required check of pull request 882 was green: 60 passed and 2 skipped by design |
| 8 | After the last deploy, one run on the live site repeats the checks of MIL-085 to MIL-087 that store nothing, and of MIL-088 when its setting is on, and reads the security headers of the booking pages; the evidence is in the evidence folder | Pass | The check above, after the deploy of dcbc88ce, the last deploy: the sign-in of MIL-085 without a booking, the date fields and the four stays of MIL-086, the 61 searches of MIL-087 and the page of the link of MIL-088 with a made-up token, its setting on, all as their milestones ask; the headers of the four booking pages as SAR-002 says. Nothing was booked. The evidence is `docs/sqa/evidence/mil-089/live-check.txt` |

The corrected sentence of SAR-002, against criterion 20 of [RC-438] (the facts and the citations are accurate): Pass.
Requirement 3.4.6 now says that the application sets no X-Frame-Options of its own, that the antiforgery of ASP.NET
Core writes `X-Frame-Options: SAMEORIGIN` on the answer that sets a new antiforgery cookie, and that a browser that
knows `frame-ancestors` obeys it; the check above shows each part, and the citation is the evidence of this gate. The
verdict of 3.4.6, open in BF-12 for the answers that nginx makes itself, does not change.

## Findings

- **F1, note: the conditions of the reviews are met.**
  - [RC-438] gave SAR-002 Go-with-conditions. C1: [ADR-0051] in its third version and [ADR-0052] are Accepted, after
    the second delta of [RC-439]. C2: every test that SAR-002 names, 113 in all, passed on the merge commit dcbc88ce:
    106 in the results of NF Hotel CI 45888194194, and the 7 browser tests in those of NF Hotel E2E and DAST
    45888194103, 5 with the verification off and 2 with it on (NF Hotel CI lists the browser tests as not run, because
    they need the stack). C4: lines 417 and 418 of SAR-002, and the readme of the evidence folder, say that production
    ran with the verification off until 2026-11-22 and runs with it on since. So RC-438 counts as Go, and SAR-002 is
    accepted here.
  - [RC-440] gave the code Go-with-conditions in round 3: F5, the load test on the branch, and F6, the end-to-end job
    of the pull request green. F5 is criterion 4 above; F6: NF Hotel E2E and DAST passed on ce1beba9 (45852244556)
    and on the merge (45888194103). So RC-440 counts as Go.
  - [RC-442] gave the wiki Go-with-conditions, with criterion 14 Pending until this gate; it passes above, and W3 and
    W4 are corrected on `main`. So RC-442 counts as Go.
- **F2, note: the row of RC-440 was accepted before its conditions were seen.** Its row of 2026-11-26 says
  `Accepted`, set in the commit of the review records (00d609b), while its round 3 says that the record stays Proposed
  until F5 and F6 are seen. Both were seen afterwards, on the pull request, and both hold (F1). The row stands; this
  record says so instead of a new row in RC-440.
- **F3, note: two commits after round 3 of RC-440.** c2c9580f corrects the header of `.github/workflows/load-test.yaml`,
  which still said that production runs with the verification off: it ran so until 2026-11-22. ce1beba9 takes the run
  id of the k6 script from WebCrypto instead of `Math.random`, still four hex digits at most, and takes the addresses
  of nginx and of the API only from `ops/scripts/load-test.sh`, which always sets them; without them the script stops
  before it sends anything. That fixes in the code the two findings of SonarQube Cloud on the pull request,
  javascript:S2245 and javascript:S5332, and none was dismissed; its quality gate then passed. Neither commit changes
  what the load test does when `load-test.sh` runs it, and the load test of criterion 4 ran on ce1beba9. Read here
  against the criteria of QC-CS-001 and QC-WF-001 that [RC-440] read on those two files: none fails.
- **F4, minor, fixed here: X-Frame-Options in SAR-002.** Requirement 3.4.6 said that X-Frame-Options "is not used";
  the check on the live site shows the antiforgery of ASP.NET Core writing it on the answer that sets a new
  antiforgery cookie. The sentence is corrected in SAR-002 (its row of 2026-11-27). Nothing is at risk:
  `frame-ancestors 'none'` is on every answer of the application and wins over the header.
- **F5, note: the alerts 677 to 681 of CodeQL.** They were open from 2026-11-20, on the code of MIL-088, until the
  run of CodeQL on the merge of MIL-089 marked them fixed, at 20:06:15 UTC on 2026-11-26; none was dismissed, as the
  plan asked. The alerts that are open on `main` at the gate are 14 of OWASP ZAP, 13 warnings and a note, which
  [MIL-090] handles.
- **F6, note: the class diagram.** [RC-439] D14: the part of the class diagram that is older than MIL-089 fails its
  first review (QC-DCD-001 criteria 3, 4, 6 and 7). It is no criterion of MIL-089; the rework is for S01 to plan.
- **F7, note: the Medium of BF-01 rests on a setting.** As D3 of [RC-438] says, BF-01 is Medium because
  `BookingVerification__Enabled` is on, and it is off by default; no alert tells when it goes off. The check of
  criterion 8 shows it on. Keeping it on is action 2 of SAR-002, for S01.

## Overall Verdict

Go on criteria 1 to 8, by the AI as stand-in for S01 in autonomous mode. Every criterion passes with the evidence
above: SAR-002 accepted with its conditions closed and one sentence corrected, the abuse cases with their tests and
transcripts, the scans with the alerts of the code scan closed through the code, the load test with the verification
off and on, the coverage floor, the verdicts on findings 3 to 6, the version, the wiki as published, the reviews and
the checks, and the check on the live site after the deploy. The reviewer is not independent, and each stand-in
decision is named in the summary of the run.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record, the gate row of MIL-089 and SAR-002, or return them | S01; done on 2026-11-27 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read the checks of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Decide on the findings BF-01 to BF-12 (#867 to #878) | S01 | As SAR-002 section 11 says |
| Keep `BookingVerification__Enabled` on (F7) | S01 | Ongoing |
| Plan the rework of the class diagram (F6) | S01 | When S01 decides |

---

[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[SAR-002]: ../security-audit-2026-12-booking-flow.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[RC-415]: ./rc-415-mil-089.md
[RC-438]: ./rc-438-sar-002.md
[RC-439]: ./rc-439-mil-089-documents.md
[RC-440]: ./rc-440-mil-089-code.md
[RC-442]: ./rc-442-mil-089-wiki.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[ADR-0029]: ../../adr/adr-0029-back-office-behind-cloudflare-access.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0013]: ../../adr/adr-0013-benchmark-results.md
[ADR-0014]: ../../adr/adr-0014-default-ai-model.md
[ADR-0004]: ../../adr/adr-0004-database-api.md
[MIL-090]: ../../milestones/mil-090-scan-findings-of-19-november.md
