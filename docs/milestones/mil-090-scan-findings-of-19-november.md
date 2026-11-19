# MIL-090 The scan findings of 19 November closed with evidence

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-090 |
| CrossReference | [BC-001], [ADR-0017], [ADR-0032], [ADR-0040], [MIL-027], [MIL-036], [MIL-065], [MIL-066], [MIL-077], [MIL-086], [MIL-087], [MIL-089], [RC-313], [RC-363], [RC-395] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | S01 | Initial version: planned at S01's request in chat on 2026-11-19 ("Ja, planlæg"), after the read-only analysis of the open alerts of code scanning that S01 asked for the same day ("kan du check up på disse og fix de issues og beskytte så de ikke sker igen fremover"); supervised mode; accepted as it stands by S01 in chat on 2026-11-19 ("Ja, accepter") after RC-445, Go | pending |

---

## Purpose

Close every open finding of the scanners of 2026-11-19 with evidence, and keep the scans from stopping releases and merges on 2026-12-09. Each alert in code scanning is fixed, or shown false by a test and accepted by a line of the rules file that S01 approves. The nine third-party images in production lose CVE-2026-97054, or carry an exception whose statement rests on a check of the binary. The 41 exceptions of the image scan are checked again and renewed or removed before they lapse, and from now on a warning comes seven days before a lapse.

What is known on 2026-11-19, before the work. The source is a read-only analysis of `main` at bac28baf, of the reports of the Zed Attack Proxy (ZAP) of the Open Worldwide Application Security Project (OWASP) from runs 43855866018 (the weekly full scan and API scan of 2026-11-16) and 44545813575 (the baseline of 2026-11-19), of issue #840 and of the API of code scanning:

| # | Finding | Where | What it means |
| --- | --- | --- | --- |
| 1 | The daily scan of the running release fails nine third-party images on CVE-2026-97054: a race in `net/http` of Go can crash an HTTP/1 server that limits a request body with `MaxBytesReader` or `MaxBytesHandler`. Go 1.26.11 and 1.27.4 fix it. Issue #840 | `grafana/alloy` v1.21.3, `cloudflare/cloudflared` 2026.10.7, `prom/prometheus` v3.16.0, `prom/alertmanager` v0.34.2, `prom/blackbox-exporter` v0.29.1, `grafana/loki` 3.7.9, `grafana/grafana` 13.3.0, `ollama/ollama` 0.40.15, `postgres:16-alpine` (in `gosu`) | Availability only: a service crashes and restarts. Trivy rates it high, the advisory of GitHub medium (5.9 in the Common Vulnerability Scoring System); `SECURITY.md` aims at a fix of a high finding within 30 days, so by 2026-12-19. Nobody has checked yet whether the affected code is in each binary, or who could reach it |
| 2 | All 41 entries of the ignore file of the image scan lapse on 2026-12-08, and no plan renews them | `ops/security/trivyignore.yaml` | From 2026-12-09 `scan-gate.sh --check-ignore` refuses the file. The gate of the images fails, so no release can be built, and the daily scan fails |
| 3 | The nine lines of the rules file of ZAP lapse on the same day. Task 3 of [MIL-089] (#829) renews them, after MIL-086 to MIL-088 | `ops/security/zap-rules.tsv` | From 2026-12-09 the required check "DAST baseline" fails, so no pull request can be merged, a security fix included |
| 4 | 18 alerts of ZAP are open in code scanning, all from the throwaway stack of continuous integration (CI), never from production. 12 are false by the code: rule 40014 (alerts 651 to 654), 10029 (alert 647), 10058 (alert 668), 10027 (alert 639) and 90027 (alerts 73, 74, 75, 635 and 648). 6 are choices made on purpose: rule 2 (alert 655, [ADR-0017]) and 40025 (alerts 68, 632, 633, 643 and 650) | Code scanning of the repository | None is a vulnerability today. But no test shows why, so a change can make one true without anyone noticing, and a real alert drowns among them |
| 5 | Five rules have no line in the rules file: 10027, 10029, 10058, 40025 and 90027 ([RC-363] F9, [RC-395] F2) | `ops/security/zap-rules.tsv` | 13 of the 18 alerts have no reason, no owner and no date |
| 6 | The theme endpoint of the back office has no test, and no test sends the fields of the guest sign-in with a GET | `src/NFHotel.Web/Security/AdminTheme.cs`, `src/NFHotel.Web/Features/Booking/GuestSignIn.razor.cs` | The controls that make alerts 647 and 668 false can go away without anyone noticing |
| 7 | Every drawing in the image folder of the web application holds comments, and the word "from" in one of them matches the pattern of rule 10027, which looks for database queries | `src/NFHotel.Web/wwwroot/img/*.svg` | A false alert, and bytes that every visitor downloads |
| 8 | `to_sarif`, which writes the Static Analysis Results Interchange Format (SARIF) file for code scanning, gives the Informational alerts of ZAP the level `none`, which code scanning shows as Warning | `ops/scripts/dast-scan.sh` | Alerts 639, 647 and 668 and the five alerts of 90027 look like warnings, though ZAP calls them Informational |
| 9 | Four places in the back office build a `MarkupString`, the usual way past the encoding of Blazor: the base of the charts, two charts of their own and the Markdown text. Each encodes its text, but nothing finds a fifth | `src/NFHotel.Web/Features/Admin/Shared/` | A new one could show the text of a guest as markup, unreviewed |
| 10 | Nothing warns before a line of the two files lapses, and every renewal sets a new last day at most 30 days ahead | `ops/scripts/scan-gate.sh`, `ops/scripts/dast-scan.sh` | The stop of 2026-12-09 comes back every month |

## Decisions

| # | Decision | Reason |
| --- | --- | --- |
| 1 | The nine images follow decision 5 of [ADR-0040]. The pin moves to an upstream release whose scan passes the gate, when one exists. Otherwise the finding gets an ignore entry of at most 30 days, narrowed to the files of the image, with a statement that rests on a check of the binary: whether the affected functions are compiled into it, whether it serves HTTP/1, and who can reach that server. No patch layer | The finding is in the standard library of Go, compiled into each binary, which a layer of packages cannot change. The check follows [MIL-065] |
| 2 | The exceptions are checked again, not only given a new date. An entry whose finding is gone is removed; any other is renewed with its statement checked against the image that runs | Finding 2; an exception is a decision with a reason, and the reason can change in 30 days |
| 3 | The image findings come first. Tasks 1 to 3 go in one pull request, open for S01's review by 2026-12-01, a week before the lapse; the ZAP side follows in a second pull request | Findings 2 and 3: the stop on 2026-12-09 hits merges and releases alike |
| 4 | The nine lines of the rules file of ZAP stay with task 3 of MIL-089 (#829). If #829 is not merged by 2026-12-01, the pull request of decision 3 renews or removes them as well, and says so on #829 | No work is done twice, and the merges do not stop |
| 5 | The daily scan opens one issue seven days before a line of either file lapses, and closes it when no line is that close | Finding 10 |
| 6 | A false positive gets a test first, and its line in the rules file after; the reason of the line names the test | The day a change makes the finding true, the test fails at once, not a scan a week later |
| 7 | An active rule keeps running. 10058, 90027 and 40025 get WARN lines, because an IGNORE line stops an active rule in the full scan, as it stopped 40025 until task 7 of [MIL-066] ([RC-313]); a new case at another address would then go unseen. The passive rule 10029 gets an IGNORE line: its alerts leave code scanning and stay in the report of ZAP. 10027 gets no line: the comments go instead, because an IGNORE line would hide real comments too | A passive rule only reads answers. An active rule sends changed requests, and only it finds what a page does with them |
| 8 | No alert is dismissed in code scanning without S01's word. The known alerts of the three WARN rules stay open until S01 dismisses them, or tells the artificial intelligence (AI) assistant in chat to, each with the reason of its line | S01's condition of 2026-11-19 ("Ingen lukning eller dismissal af alerts") |
| 9 | 40025 is accepted as it is. Open-source nginx always sends its name in `Server`, and `server_tokens off` hides the version. A test keeps the version hidden and shows that TRACE and TRACK are refused; nginx changes only if the test shows a method that reaches the application | Fingerprinting only: the name tells an attacker that nginx is in front, not which version |
| 10 | Informational alerts go to code scanning with the level `note` | The level that ZAP gives them (finding 8) |
| 11 | The alerts of rules 2 and 40014 (alerts 651 to 655) are not changed here. The rules of [MIL-086], merged on 2026-11-19, refuse markup in the four fields of alerts 651 to 654; [MIL-087] changes the address of alert 655; task 3 of MIL-089 renews or removes their lines after the scan of the commit with MIL-085 to MIL-088. The gate record of this milestone states what the last weekly scan shows for them | Each already has an owner |
| 12 | Two points of the analysis are not planned. A warning for an analysis of code scanning that ends with an error: 2 of 1,008 uploads of the image scan on `main` failed so, both errors of GitHub after a complete upload, and nothing was lost; analysis 2204491197 stays. The query suite of CodeQL: a setting of the repository, which is S01's to change | Small value for the work; a setting is S01's own |

## Deliverable

- A record of the check of each of the nine images in `docs/sqa/evidence/mil-090/`, and for each image a new pin or an ignore entry with its statement.
- `ops/security/trivyignore.yaml` with every entry checked again: removed, or renewed with an expiry at most 30 days ahead.
- A warning seven days before a line of `trivyignore.yaml` or `zap-rules.tsv` lapses, as an issue of the daily scan, with its tests.
- Tests that show alerts 647 and 668 false, a test that keeps comments out of the drawings, and a test that finds a new `MarkupString`.
- The drawings without comments.
- Informational alerts as `note` in the SARIF file of ZAP, with its test.
- Lines for 10029, 10058, 90027 and 40025 in `ops/security/zap-rules.tsv`, and the checks of 40025 in `ops/tests/test-nginx.sh`.
- The wiki page on the security controls, the review records, and the gate record with the state of the 18 alerts.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The record of the check of each of the nine images is in the evidence folder. Each image runs a version whose scan passes the gate, or its finding has an ignore entry narrowed to its files, with the statement from the check and an expiry at most 30 days ahead, approved by S01 in the review. By 2026-12-19 the daily scan of the running release passes and has closed #840 | Yes | No |
| 2 | Each entry of `trivyignore.yaml` is removed, or renewed with its statement checked again, approved by S01, in a pull request merged before 2026-12-08. `scan-gate.sh --check-ignore` and `dast-scan.sh --check-rules` pass on `main` for the date 2026-12-09, and the daily scan of 2026-12-09 passes the check of the ignore file | Yes | No |
| 3 | The tests of the two checks show the warning for a line that lapses in 7 days and none for a line that lapses in 8 days; a test or a run of the daily scan shows the issue opened for such a line and closed after its renewal | Yes | No |
| 4 | The tests of the theme endpoint, of the GET of the guest sign-in, of the drawings and of `MarkupString` pass, and each fails when its control is taken out; the transcripts are in the evidence folder | Yes | No |
| 5 | In the SARIF file an Informational alert has the level `note`; `ops/tests/test-dast-rules.sh` shows it, and the required check that runs it is green | Yes | No |
| 6 | `zap-rules.tsv` has an IGNORE line for 10029 and WARN lines for 10058, 90027 and 40025, each with a reason that names its test or decision and an expiry at most 30 days ahead, approved by S01 in the review; `test-nginx.sh` shows TRACE and TRACK refused and no version in `Server` | Yes | No |
| 7 | The first baseline, full scan and API scan on `main` after the merges: alert 639 is no longer reported; alert 647 is out of code scanning and still in the report of ZAP; no new alert of the five rules; no High alert. The gate record lists each of the 18 alerts with its state (fixed, a line, or the milestone that owns it), and no alert was dismissed without S01's word | Yes | No |
| 8 | The code review against the checklists of the changed files is Go; every required check of each pull request is green; the wiki page Security-Controls says how a finding of a scan is settled | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-086 | Its rules refuse markup in the four fields of alerts 651 to 654; the first weekly scan after its merge shows whether the alerts go |
| MIL-087 | It changes the address that the audit trail stores (alert 655) |
| MIL-089, task 3 (#829) | It renews or removes the nine lines that lapse on 2026-12-08, among them those of rules 2 and 40014 (decision 4) |
| MIL-027, MIL-077 | The scans of ZAP, the rules file and `dast-scan.sh`, which this milestone changes |
| MIL-036 | The daily scan of the images that run, which opened #840 and gets the warning |
| MIL-065, ADR-0040 | The remedies of a third-party image and the form of an ignore entry |
| ADR-0032 | What code scanning shows |
| MIL-066 | Its task 7 took the IGNORE line of 40025 out so that the active rule runs; decision 7 keeps that |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 24 and success criterion 27: 0 critical and 0 fixable high findings in the images that run, except findings accepted under ADR-0040 with a statement and an expiry at most 30 days ahead | Criteria 1 and 2 |
| BC-001 objective 31 and success criterion 34: every ignored or warned rule of ZAP has a reason and an expiry at most 30 days ahead | Criteria 4 to 7 |
| BC-001 objective 40 and success criterion 43: the daily scan opens an issue on a finding that blocks a release and closes it when fixed | Criteria 1 and 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-18, the day before the 30 days of `SECURITY.md` for #840 run out. The pull request of tasks 1 to 3 is open for review by 2026-12-01, a week before the exceptions lapse. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The check of the nine images of #840 | For each image: the binaries that the scan names; whether `net/http.MaxBytesReader` or `MaxBytesHandler` is compiled into them; whether they serve HTTP/1, and who can reach that server. The record goes in `docs/sqa/evidence/mil-090/`. Then, as decision 1 says: a new pin whose scan passes the gate, or an ignore entry narrowed to the files, with the statement from the check and an expiry at most 30 days ahead. The images on the virtual machine (VM) are deployed by the run of continuous deployment (CD) that S01 approves, the monitoring stack by its pull on `nf-monitoring` | No | criterion 1 |
| 2 | The exceptions that lapse on 2026-12-08 | Check each of the 41 entries of `ops/security/trivyignore.yaml` against the image that runs: remove an entry whose finding is gone, and renew the others with the statement checked again and an expiry at most 30 days ahead. If #829 is not merged by 2026-12-01, renew or remove the nine lines of `ops/security/zap-rules.tsv` here too, and say so on #829 (decision 4). With tasks 1 and 3, one pull request open for S01's review by 2026-12-01; each line waits for S01's approval | No | criterion 2 |
| 3 | A warning seven days ahead | `scan-gate.sh --check-ignore` and `dast-scan.sh --check-rules` name each line that lapses within 7 days; the daily scan opens one issue that lists them and closes it when no line is that close. Tests at 8, 7 and 0 days, and of the issue | No | criterion 3 |
| 4 | The tests that show alerts 647 and 668 false | In `tests/NFHotel.Web.Tests`: the theme endpoint writes only `light` or `dark`, with HttpOnly, Secure, SameSite=Lax and the path `/admin`; it deletes the cookie for any other value, refuses a request without antiforgery or without a sign-in, and sends a return address of another site to `/admin`. A GET of `/booking` with the fields of the guest sign-in shows the empty form, signs nobody in and sets no cookie. Each test fails when its control is taken out; keep the transcript | No | criterion 4 |
| 5 | The drawings without comments | Remove the comments from the drawings in `src/NFHotel.Web/wwwroot/img` (alert 639), and add a test that fails when a file there holds a comment | No | criteria 4 and 7 |
| 6 | A test that finds a new MarkupString | A test that lists the places in `src/NFHotel.Web` that build a `MarkupString` and fails on one more, so that a new one gets a review and a test of its encoding | No | criterion 4 |
| 7 | The level of an Informational alert | `to_sarif` in `ops/scripts/dast-scan.sh` gives the Informational alerts of ZAP the level `note`; `ops/tests/test-dast-rules.sh` shows it | No | criterion 5 |
| 8 | The lines of the five rules | After tasks 4 and 5: an IGNORE line for 10029 and WARN lines for 10058, 90027 and 40025 in `ops/security/zap-rules.tsv`, each with the reason that names its test or decision and an expiry at most 30 days ahead; no line for 10027. In `ops/tests/test-nginx.sh`: TRACE and TRACK are refused, and `Server` has no version. Each line waits for S01's approval in the review | No | criterion 6 |
| 9 | The documents, the code review and the pull requests | The wiki page Security-Controls says how a finding of a scan is settled: a fix, or a test and a line, or an exception of the image scan; the traceability matrix; the code review records against `QC-CS-001`, `QC-SH-001`, `QC-YML-001` and `QC-WF-001`; the commits and the two pull requests, which close the issues of tasks 1 to 9 | No | criterion 8 |
| 10 | The scans after the deploy and the gate | After the merges and S01's approval of the CD runs: the daily scan of the running release, and the first baseline, full scan and API scan on `main`; the state of the 18 alerts in the gate record; the gate review record against criteria 1 to 8, the row of MIL-090 in the traceability matrix, and the closing of the GitHub milestone | No | criteria 1 and 7 |

---

[BC-001]: ../business-case.md
[ADR-0017]: ../adr/adr-0017-staff-passwords-and-audit.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[ADR-0040]: ../adr/adr-0040-patched-nginx-image-from-the-release.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[MIL-036]: ./mil-036-security-operations.md
[MIL-065]: ./mil-065-third-party-image-remedies.md
[MIL-066]: ./mil-066-findings-of-7-november.md
[MIL-077]: ./mil-077-hostile-input-against-the-api.md
[MIL-086]: ./mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ./mil-087-limits-on-what-one-sender-can-ask.md
[MIL-089]: ./mil-089-the-booking-flow-attacked-and-measured.md
[RC-313]: ../sqa/reviews/rc-313-mil-066-gate.md
[RC-363]: ../sqa/reviews/rc-363-mil-077-code.md
[RC-395]: ../sqa/reviews/rc-395-mil-077-gate.md
