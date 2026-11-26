# RC-442 Review of the wiki of MIL-089: the booking flow attacked and measured

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-442 |
| CrossReference | [QC-WIKI-001], [MIL-089], [RC-439], [RC-415], [SAR-002], [ADR-0051], [ADR-0052], [RC-438] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the wiki of MIL-089 against QC-WIKI-001; Go-with-conditions<br>Second delta review after the fixes of W1 and W2, the lines of BF-01 and the line of the load test for C4 of RC-438: Go-with-conditions, criterion 14 Pending until the gate; W3 and W4 to correct; mode autonomous, not independent<br>Accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

## Artifact Under Review

- Instance reviewed: the wiki, `docs/wiki/`, as it stands on 2026-11-26 in the working tree of the branch
  `mil-089-booking-flow-attacked` (from `main` at 3cfe3ab, not yet committed; its files last changed on 2026-11-21),
  before its pull request: 25 pages and `_Sidebar.md`. MIL-089 adds the page Booking-Flow-Protection, changes the
  content of Alerts, Home, Incidents, Security-Controls and Workflows, gives the sidebar its title, the new page and a
  `**Languages**` section, and starts every page with the language line.
- Checklist used: [QC-WIKI-001], read at the commit that the framework of the branch pins.
- Scope: full review of the whole wiki against every criterion, after the No-Go of the wiki in [RC-439]; each fact of
  the new and changed pages is read against the code, the deploy files and the decision records of the branch.
  RC-439 keeps its other documents; this record closes its findings D1 to D9.
- Out of scope: [SAR-002] and the code, which have review records of their own. Where a page cites SAR-002, the
  citation was read against it.
- Language and domain: the wiki has no Metadata; it is in English, the PO language of the project
  (`docs/artifact-registry.md`), so it exists once.
- Language reviewer: none; the reviewer reads English and knows the domain.
- Reviewer: AI as stand-in for S01.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat for the hardening of the booking flow and
  handed the decisions to the AI; the plan of [MIL-089] was accepted by the AI as stand-in for S01 in [RC-415]. The
  build session of the AI wrote the pages; a separate review agent of the AI, which wrote none of them, reviewed them
  and wrote this record.
- Checked by the reviewer: every link of the 26 files outside code blocks (351 to a page or a heading of the wiki, 235
  to a file or a folder of the repository and one to a picture, none of them dead; the 12 links to issues and pull
  requests and the 108 to other addresses were not opened); the sidebar against the pages; the first lines of every
  page; no carriage return in any file; `bash ops/tests/test-wiki-coverage.sh`, run on 2026-11-26: 133 passed, 0
  failed.
- Not checked: the published wiki (criterion 14); how GitHub renders the Mermaid diagram of Booking-Flow-Protection;
  what a page shows when its circuit is refused (W1).

## Checklist Results (QC-WIKI-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The wiki has a `_Sidebar.md`, because the host lists subpages only through it | Pass | `docs/wiki/_Sidebar.md` |
| 2 | The sidebar starts with the wiki's title in bold, then one list of the pages in the primary language (English), `Home` first | Pass | Line 1 is `**NF Hotel**`, the title of Home, then a blank line and one list of the pages in English, `Home` first (line 3); the Fail of [RC-439] is fixed |
| 3 | The page list holds a link to every English page of the wiki, and no link points to a page that does not exist | Pass | 25 pages in `docs/wiki` and 25 links in the list (lines 3 to 27), the same set, the new page at line 23 |
| 4 | A `**Related**` section follows the page list only when the wiki has related wikis (for example the checklists wiki for a framework wiki); each entry is a link to that wiki's Home. A wiki with nothing related has no such section | N-A | No related wiki, and no such section |
| 5 | A `**Languages**` section is the last section of the sidebar. It has one line per language, the language written in that language, linking to that language's `Home` page | Pass | `**Languages**` (line 29) is the last section, with the one line `- English: [Home](Home)` (line 31), the shape that `framework/process/wiki.md` gives a wiki in English alone; the Fail of RC-439 is fixed |
| 6 | The sidebar does not repeat the page list per language | Pass | One list |
| 7 | A sidebar produced by a generator carries a comment that says so and names the generator; the generator is changed, not the page | N-A | Written by hand; `ops/scripts/publish-wiki.sh` copies the pages as they are |
| 8 | The wiki has a `Languages` line on each page (criterion 11), so the sidebar is not the only way to switch language | Pass | Optional. As criterion 11 |
| 9 | Every wiki exists in English and, as a minimum, in the language the project's registry names as the PO language; further languages are added on top, never instead of English | Pass | The PO language is English, and the wiki is in English |
| 10 | Every English page has a page in each other language of the wiki: the page sets are identical | N-A | No other language |
| 11 | Every page starts with the language line, with the current language in bold and a link to the same page in each other language | Pass | Each of the 25 pages has its title, a blank line, `**English**` (line 3) and a blank line, the form of the three pages that had it and of the pages of the framework; in a wiki of one language the line is the bold language alone. 16 pages gained these two lines and nothing else. The Fail of RC-439 is fixed |
| 12 | A page in a language other than English is named `<Page>-<code>.md`, with a BCP 47 code; English has no suffix | Pass | No page in another language, and no page has a suffix |
| 13 | Code blocks, commands, file names and IDs stay in English in every language | Pass | |
| 14 | The sidebar, every page and every link were checked on the published wiki after the change, not only in the repository | Pending | The wiki is published from `main` after the merge (`.github/workflows/publish-wiki.yaml`); the gate of [MIL-089] (task 7, RC-441) checks the sidebar, every page and every link there |
| 15 | A change to the layout rules (sections, order, naming) is made in the generator or template and in this checklist's source rules together | N-A | Optional. No rule of the layout changed: the sidebar and the pages take the shape that the framework already gives |
| 16 | The wiki folder (`docs/wiki/`) is flat: every page, `Home.md` and `_Sidebar.md` included, is directly in it, and languages are told apart by file name, never by a subfolder | Pass | The 26 files are directly in `docs/wiki`; its one subfolder, `images/`, holds pictures, not pages |

What holds, read against the code, the deploy files and the decision records of the branch. On
Booking-Flow-Protection: the layers (nginx takes the caller address only from the docker ranges, writes it over
whatever the caller sent, refuses a body over 64 KB and waits 60 seconds for a slow client; the web application sends
its content security policy without inline code, `Referrer-Policy: same-origin` and `no-referrer` on
`/booking/verify`, and keeps at most 50 circuits per caller; the service token); the table of the limits against the
ledger, `LimitNames` and `CircuitLimit.OpenPerCaller`, and the three codes of the limits of the API; the events and
what each line holds against `SecurityEvents` and the middleware of the circuits, with `input_refused` not counted and
`booking_request_made` without an email ([ADR-0052] decision 10); the five alerts and their thresholds against
`ops/deploy/monitoring/prometheus/rules/security.yml`, with `BookingFlowRefusals` over the API and the web
application; the steps of each alert, among them the public bookings found in the database by `MadeOnPublicSite` and
a receipt that does not go leaving its booking standing; the tests of the abuse cases against section 4 of [SAR-002];
and the twelve known gaps, each with its severity (the OWASP Risk Rating of section 1 of SAR-002) and its issue. On
Alerts: the kinds and the limits of the counter, the refusals of the circuits in the same counter, no email in the
events, and the link to the new page (line 99). On Security-Controls: the assessment and the work (line 5), the link to
the new page (line 88), the rate limits with the limit of the circuits (line 95), the headers with `Referrer-Policy`
(line 101), the personal data of the logs (line 112) and the security events with `circuits` (line 119). On
Incidents: the row of the log policy, which matches decision 6 of [ADR-0051]. On Workflows: the load test (line 154),
with its burst one after the other from 45 seconds into the run, its thresholds and its two runs. On Home: the line to
the new page (line 22). What does not hold is W1 and W2.

## Findings

- **W1, should be corrected: the 51st tab.** `docs/wiki/Booking-Flow-Protection.md` line 54 says that the 51st open
  tab of one caller "gets its page but no circuit, and the page does not react until another tab is closed". The test
  `CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` shows that the page opens while the
  negotiation of its circuit gets 429, and that the negotiation passes once a place is free; nothing shows what the
  page does meanwhile, or that it gets its circuit without being loaded again. [ADR-0051] says instead that the page
  "says that it cannot connect" ([RC-439] D13). Fix, with ADR-0051 in the same words: "the 51st open tab of one
  caller gets its page but no circuit, so the page does not work; once another tab is closed, it gets its circuit
  when it is loaded again".
- **W2, nit: the mail events on Alerts.** `docs/wiki/Alerts.md` line 101 says that "the two mail events hold the mail
  and the reason instead of an address"; `mail_fuse_blown` (event 8608) holds neither, as the table of
  Booking-Flow-Protection says ("Nothing more", line 68). Fix: "`mail_not_sent` holds the mail and the reason instead
  of an address, and `mail_fuse_blown` nothing more".
- **Closed: D1 to D9 of [RC-439].** D1: Alerts line 101 holds no email and lists the kinds since MIL-088. D2:
  Booking-Flow-Protection line 71, the database, and the back office shows no booking request. D3: line 97, true now
  that case 11 has a test that fails without its limit, with BF-02 named. D4: the table of lines 60 to 69, with
  `verification_failed`, the two mail events and `input_refused`, not counted. D5: line 84. D6: line 87. D7: line 54,
  one of three codes. D8: Security-Controls lines 101 and 5. D9: Workflows line 154. N5 of RC-439 is moot: every page
  has the language line.

## Overall Verdict

Go-with-conditions. Every criterion of [QC-WIKI-001] that the repository can show passes: the Fails of [RC-439] on
criteria 2, 5, 11 and 8 are fixed, and so are its findings D1 to D9. Criterion 14 is Pending until the gate of
[MIL-089] reads the published wiki after the merge, so the verdict cannot be Go. W1 and W2 do not block; they should be
fixed before the merge. The wiki has no Version History row of its own, so the condition closes at the gate, RC-441.
The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Correct W1 in `docs/wiki/Booking-Flow-Protection.md` line 54, in the words that [ADR-0051] takes for D13 of [RC-439] | S01 | Before the merge, in the pull request of `mil-089-booking-flow-attacked` |
| Correct W2 in `docs/wiki/Alerts.md` line 101 | S01 | Before the merge, in the same pull request |
| Check the sidebar, every page and every link on the published wiki, and record it (QC-WIKI-001 14) | S01 | At the gate of [MIL-089], task 7 (RC-441) |

## Second delta review

The second reading of the wiki, on 2026-11-26, after the build session fixed W1 and W2, rewrote the lines of BF-01 on
Booking-Flow-Protection and, for C4 of [RC-438], the line of the load test on Workflows, all in the same working
tree. Each changed page was compared with the copy that the first version of this record read: five lines changed,
in three pages, and no other file of `docs/wiki/`. As in [RC-439], the table holds every criterion of the checklist.
The line numbers above are those of the version that they read; this section cites the current lines.

- Changed: `docs/wiki/Booking-Flow-Protection.md` lines 54 (W1), 105 and 109 (BF-01); `docs/wiki/Alerts.md` line 101
  (W2); `docs/wiki/Workflows.md` line 154 (C4 of RC-438).
- Read against: [ADR-0051] lines 291 to 292 and the test of the circuits; event 8608 of `SecurityEvents`; sections 1,
  2, 8 and 11 of [SAR-002] and its issue of BF-01, #867; and the counter of the security events of the API in
  production, read on 2026-11-26.
- Checked by the reviewer: no carriage return in the three pages; `bash ops/tests/test-wiki-coverage.sh`, run again on
  2026-11-26 after the last change: 133 passed, 0 failed; the whole wiki searched for other lines that speak of
  production as if the verification were off (W3 and W4).
- Not checked: the published wiki (criterion 14).

### Checklist Results (QC-WIKI-001), second delta

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The wiki has a `_Sidebar.md`, because the host lists subpages only through it | Pass | Unchanged: `docs/wiki/_Sidebar.md` |
| 2 | The sidebar starts with the wiki's title in bold, then one list of the pages in the primary language (English), `Home` first | Pass | Unchanged: `**NF Hotel**`, a blank line and one list in English, `Home` first |
| 3 | The page list holds a link to every English page of the wiki, and no link points to a page that does not exist | Pass | Unchanged: 25 pages in `docs/wiki` and 25 links in the list, the same set |
| 4 | A `**Related**` section follows the page list only when the wiki has related wikis (for example the checklists wiki for a framework wiki); each entry is a link to that wiki's Home. A wiki with nothing related has no such section | N-A | No related wiki, and no such section |
| 5 | A `**Languages**` section is the last section of the sidebar. It has one line per language, the language written in that language, linking to that language's `Home` page | Pass | Unchanged: `**Languages**` is the last section, with the one line `- English: [Home](Home)` |
| 6 | The sidebar does not repeat the page list per language | Pass | One list |
| 7 | A sidebar produced by a generator carries a comment that says so and names the generator; the generator is changed, not the page | N-A | Written by hand |
| 8 | The wiki has a `Languages` line on each page (criterion 11), so the sidebar is not the only way to switch language | Pass | Optional. As criterion 11 |
| 9 | Every wiki exists in English and, as a minimum, in the language the project's registry names as the PO language; further languages are added on top, never instead of English | Pass | The PO language is English, and the wiki is in English |
| 10 | Every English page has a page in each other language of the wiki: the page sets are identical | N-A | No other language |
| 11 | Every page starts with the language line, with the current language in bold and a link to the same page in each other language | Pass | Unchanged: each page starts with its title and `**English**` (line 3); the changed lines are further down |
| 12 | A page in a language other than English is named `<Page>-<code>.md`, with a BCP 47 code; English has no suffix | Pass | No page in another language, and no page has a suffix |
| 13 | Code blocks, commands, file names and IDs stay in English in every language | Pass | The changed lines keep their names in code: `CircuitLimit.OpenPerCaller`, `429`, `mail_not_sent` and `mail_fuse_blown` |
| 14 | The sidebar, every page and every link were checked on the published wiki after the change, not only in the repository | Pending | As above: the gate of [MIL-089] (task 7, RC-441) checks the published wiki after the merge |
| 15 | A change to the layout rules (sections, order, naming) is made in the generator or template and in this checklist's source rules together | N-A | Optional. No rule of the layout changed |
| 16 | The wiki folder (`docs/wiki/`) is flat: every page, `Home.md` and `_Sidebar.md` included, is directly in it, and languages are told apart by file name, never by a subfolder | Pass | Unchanged: the 26 files are directly in `docs/wiki` |

### W1, W2, the lines of BF-01 and the load test

- **W1, fixed.** Booking-Flow-Protection line 54 now says that the 51st open tab of one caller "gets its page but no
  circuit, so the page does not work; once another tab is closed, it gets its circuit when it is loaded again": the
  words of [ADR-0051] (lines 291 to 292, D13 of [RC-439]) and what
  `CircuitLimitTests.WebApplication_CallerAtTheLimit_GetsNoCircuitButThePage` shows.
- **W2, fixed.** Alerts line 101: "`mail_not_sent` holds the mail and the reason instead of an address, and
  `mail_fuse_blown` nothing more", as event 8608 of `SecurityEvents` writes its kind alone and the table of
  Booking-Flow-Protection says (line 68).
- **The lines of BF-01.** Line 105 says that no finding is High or Critical as production runs since 2026-11-22, with
  the verification by email on, and that BF-01 is High with it off (likelihood High, impact Medium); the row of line
  109 gives BF-01 the title of [SAR-002], Medium since then and High with it off. Both agree with SAR-002 (lines 61 to
  62 and 80 to 82, section 8 at lines 437 to 438 and 442, and action 2 at line 536) and with #867. That production
  runs with the verification on was read on 2026-11-26 from the counter of the security events of the API: since the
  start of its process on 2026-11-22 it counts booking requests, which exist only with the verification on, and no
  mail that the relay did not take and no blown fuse.
- **The load test.** Workflows line 154 now says that the test runs with the verification off "as production ran
  until 2026-11-22" and on "as production runs since", where it said "until its relay is set"; the same facts as
  above.

### Findings of the second delta review

- **W3, should be corrected: the largest risk.** `docs/wiki/Booking-Flow-Protection.md` line 122, unchanged and missed
  by the first version of this record: "Until the relay is set up and the verification is on, BF-01 is the largest
  risk: watch `PublicBookingSurge`." Both have been so since 2026-11-22, as lines 105 and 109 now say, so the sentence
  reads as if BF-01 stopped being the largest risk then; [SAR-002] calls it the largest risk in production today, with
  the verification on (line 90), and says that about 20 addresses can use up the mails of a day, which the alert
  `MailVolumeHigh` reports (line 517). Fix: "BF-01 is the largest risk in production, with the verification on too
  (section 2 of SAR-002): watch `PublicBookingSurge`, and `MailVolumeHigh` for the mails of a day."
- **W4, should be corrected: the verification off in production.** `docs/wiki/Security-Controls.md` line 99, the row
  "A booking verified by email", ends: "Off in production until S01 has set the relay (task 9 of MIL-088): until then
  the public site books at once, behind the limits of MIL-087." The row is older than MIL-089 (line 95 at 3cfe3ab),
  and the first version of this record missed it; since 2026-11-22 the verification is on in production. Fix: "On in
  production since 2026-11-22, when S01 turned it on with the relay (task 9 of MIL-088); with it off, the public site
  books at once, behind the limits of MIL-087."

### Second delta verdict

Go-with-conditions, as before. Every criterion of [QC-WIKI-001] that the repository can show passes; W1 and W2 are
fixed, and the lines of BF-01 and of the load test agree with [SAR-002] and with production. Criterion 14 stays
Pending until the gate of [MIL-089] reads the published wiki after the merge (RC-441), so the verdict cannot be Go.
W3 and W4 do not block; they should be fixed before the merge. The reviewer is not independent.

### Second delta action items

The first two action items above, W1 and W2, are done.

| Action | Owner | Due |
| --- | --- | --- |
| Correct W3 in `docs/wiki/Booking-Flow-Protection.md` line 122 and W4 in `docs/wiki/Security-Controls.md` line 99 | S01 | Before the merge, in the pull request of `mil-089-booking-flow-attacked` |
| Check the sidebar, every page and every link on the published wiki, and record it (QC-WIKI-001 14) | S01 | At the gate of [MIL-089], task 7 (RC-441) |

---

[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[RC-439]: ./rc-439-mil-089-documents.md
[RC-415]: ./rc-415-mil-089.md
[SAR-002]: ../security-audit-2026-12-booking-flow.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[RC-438]: ./rc-438-sar-002.md
