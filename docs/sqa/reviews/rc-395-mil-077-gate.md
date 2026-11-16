# RC-395 Gate review of MIL-077 Hostile input against the API

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-395 |
| CrossReference | [MIL-077], [RC-337], [RC-363], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-16 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the gate of MIL-077 after the weekly run of 2026-11-16 with the API scan; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-077] at its gate. It covers the five Go/No-Go criteria, with this evidence:
  - the code of tasks 1 to 4 on `main` since de82d34 (pull request 749, merged on 2026-11-14), reviewed in [RC-363],
    Go-with-conditions;
  - the checks of pull request 749 on its last head (84f9cd7): 54 passed, and the DAST full scan and OWASP ZAP
    skipped, as on every pull request;
  - the runs by hand 43601706848 and 43603868538 of NF Hotel E2E and DAST, read in [RC-363];
  - the weekly run 43855866018 of NF Hotel E2E and DAST on 2026-11-16, at 772cf65 of `main`;
  - the rules file `ops/security/zap-rules.tsv` on `main`, checked on 2026-11-16.

  Before the gate: [RC-337] (the plan, Go; accepted by S01 in chat on 2026-11-12).
- Checklist used: the Go/No-Go criteria of [MIL-077]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate (task 5).
- Language and domain: en / it (the Metadata rows of MIL-077). The gate adds only a row to its Version History. This
  record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14 to the AI in chat ("Du
  godkender for mig"), and on 2026-11-16 asked it to finish the open milestones in autonomous mode and to close each
  when it is done ("du skal i automode lave alle disse milestones", "færdiggør dem alle og luk dem når de er 100%
  done"). The AI built the code, read the runs and wrote this record.

**Evidence of the weekly run (2026-11-16):** the scheduled run 43855866018 of NF Hotel E2E and DAST ran at 772cf65 of
`main`, from 11:13:39 to 11:21:30 UTC, and ended with success. Its job DAST full scan passed; the jobs End-to-end
tests and DAST baseline were skipped, as on every scheduled run (the weekly run 42609309800 of 2026-11-09 too). The
web full scan ended with FAIL-NEW 0, WARN-NEW 1 and PASS 141; its one warning, 40025 Proxy Disclosure (20 times), has
no line in the rules file (F2). The job then created the Manager account `e2e-api-scan`, and ZAP ran
`zap-api-scan.py` with the OpenAPI document against the API: FAIL-NEW 0, WARN-NEW 2, IGNORE 0, PASS 110. The two
warnings are 2 Private IP Disclosure (once) and 40014 Cross Site Scripting Weakness (Persistent in JSON Response) (4
times), both under their WARN lines in the rules file. The check of the job printed PASS: "no High alert and no
answer 500; the other alerts are in the reports", and the job uploaded the API scan to code scanning.

**Evidence of the rules file (2026-11-16):** `bash ops/scripts/dast-scan.sh --check-rules ops/security/zap-rules.tsv`
printed "every IGNORE and WARN rule has a reason and expires within 30 days" and ended with 0. The file holds nine
lines: WARN for 2 and 40014, IGNORE for 10015, 10031, 10049, 10104, 10109, 10111 and 10112; each has its reason, and
all nine expire on 2026-12-08, 22 days after the gate.

## Checklist Results

| # | Criterion (MIL-077) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The test covers every endpoint that the API maps (it compares its own count with the endpoints of the API) and every kind of input, and it passes; the review record shows it failing against an endpoint changed to throw `InvalidOperationException` on its input | Pass | [RC-363]: `HostileInputTests` takes the endpoints from the test API and asserts that it attacked as many as the API maps; it sends 898 requests and fails none. With `POST api/rooms/search` changed to throw `InvalidOperationException` on its input, it failed 1 of 898 ("plausible values -> 500, a fault"). NF Hotel CI runs it on every pull request and every push to `main`; its run 43855375074 at 772cf65, the commit of the weekly run, passed |
| 2 | Every defect that the test or the scan found has a regression test that failed before its fix | Pass | [RC-363]: the cases of `HostileInputRegressionTests` failed before their fixes (14 of 21 in the first group, then the cases of the later fixes), and `ApiSecurityHeadersTests` failed 5 of 5 before the header `nosniff` that the first API scan asked for (alert 10021) |
| 3 | The build output holds the OpenAPI document with every endpoint, and a test proves that the API in Production answers 404 to `/openapi/v1.json` | Pass | [RC-363]: the build writes `g/openapi/nfhotel-api.json`; `OpenApiDocumentTests` holds the document to every endpoint that the API maps and the API in Production to a 404 at `/openapi/v1.json` (the first test failed when the document left out `/health`, the second when `MapOpenApi` was added) |
| 4 | The weekly run and a run by hand of the end-to-end and DAST workflow scan the API with its document: 0 High alerts that are not accepted, and every ignored or warned rule with a reason and an expiry at most 30 days ahead | Pass | The weekly run 43855866018 at 772cf65 scanned the API with its OpenAPI document: no High alert and no answer 500; FAIL-NEW 0, WARN-NEW 2 (2 and 40014, each under its WARN line), IGNORE 0, PASS 110 (above). The runs by hand 43601706848 and 43603868538 scanned it the same way before the merge, with no High alert and no answer 500 ([RC-363]). Every IGNORE and WARN line of the rules file has a reason and expires on 2026-12-08, 22 days after the gate, and `dast-scan.sh --check-rules` passed (above). The warning 40025 of the web full scan has no line; it belongs to the web scan, not to the API scan (F2) |
| 5 | Security-Controls, Workflows and the rules file say what the deliverable says, `test-wiki-coverage.sh` and `dast-scan.sh --check-rules` pass, the code review against `QC-CS-001`, `QC-SH-001` and `QC-WF-001` is Go, and every required check of the pull request is green | Pass, with a note | [RC-363] read the wiki pages and the rules file against the deliverable; the job Ops wiki coverage passed on pull request 749 and on every pull request since; `dast-scan.sh --check-rules` passed on 2026-11-16 (above); every check of pull request 749 was green at its merge. [RC-363] is Go-with-conditions, and both conditions are met (F1) |

## Findings

- **F1, note: the conditions of the code review are met.** [RC-363] gave Go-with-conditions: every check of the pull
  request green before the merge, and criterion 4 read again on the weekly run of 2026-11-16. Pull request 749 merged
  with every check green, and this record reads the weekly run, so the review counts as Go for criterion 5.
- **F2, note: what stays open with S01.** The nine lines of the rules file expire on 2026-12-08 and must be renewed or
  removed before then, and the warnings of the web full scan that have no line ([RC-363] F9) wait for S01's decision.
  Neither is a criterion of [MIL-077].
- **F3, note: the weekly run came late.** The cron of the workflow is 03:17 UTC on Mondays; GitHub started the run of
  2026-11-16 at 11:13 UTC, about eight hours later, as it may delay a scheduled run. It is the first weekly run with
  the API scan, since pull request 749 merged on 2026-11-14. A scheduled run runs only the full scans; the end-to-end
  tests and the baseline scan ran on pull request 749.

## Overall Verdict

Go. Criteria 1 to 5 of [MIL-077] pass. The test attacks every endpoint of the API with every kind of input on each
pull request, every defect it or the scan found has its regression test, and the weekly run of 2026-11-16 scanned the
API with its OpenAPI document with no High alert and no answer 500, as the runs by hand did before the merge. The
conditions of [RC-363] are met (F1). The row of 2026-11-16 of [MIL-077] is Accepted and its row of 2026-11-12
Deprecated. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the gate row of MIL-077, or return them | S01; done on 2026-11-16 by the AI as stand-in, at S01's word in chat | At the review of the gate pull request |
| Read NF Hotel CI of the gate pull request before the merge | AI as stand-in for S01 | Before the merge |
| Renew or remove the nine lines of the rules file before they expire (F2) | S01 | 2026-12-08 |
| Decide on the warnings of the web full scan that have no line ([RC-363] F9) | S01 | Open |

---

[MIL-077]: ../../milestones/mil-077-hostile-input-against-the-api.md
[RC-337]: ./rc-337-mil-077.md
[RC-363]: ./rc-363-mil-077-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
