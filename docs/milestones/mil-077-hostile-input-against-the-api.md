# MIL-077 Hostile input against the API

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-077 |
| CrossReference | [BC-001], [ADR-0002], [ADR-0004], [MIL-015], [MIL-027], [MIL-044], [MIL-067] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | S01 | Initial version: the scans reach the web pages through nginx but not the JSON endpoints of the database API, and no test sends every endpoint malformed or oversized input; planned at S01's request in chat ("Skriv 1–4", to the plan of 2026-11-12) | [62301c4] |

---

## Purpose

Prove that the database API answers hostile input with a refusal and never with a fault: every endpoint, sent malformed, mistyped, oversized or unusual Unicode input, answers in the 400 range and shows nothing of the server; and let OWASP ZAP attack the API itself, not only the pages in front of it.

The API ([ADR-0004], [MIL-015]) serves the web application and the mobile app; the entities hold the field rules ([ADR-0002]); the dynamic application security testing (DAST) of [MIL-027] and [MIL-044] scans a throwaway stack; and [MIL-067] proved the web pages against bad input. Read on 2026-11-12 at 1c2726b:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The ZAP scans target nginx: the public pages and `/admin` signed in on every pull request, and the whole site in the weekly full scan. The JSON endpoints of the API sit behind the web and are not scanned | `ops/scripts/dast-scan.sh` | The input handling of the API is tested only by the tests that were written with it |
| 2 | The API answers an `ArgumentException` with 400, and any other exception with 500 and `api.unexpected_error`, without a stack trace | `src/NFHotel.Api/Program.cs`, `UseExceptionHandler` | Input that makes the domain or the database layer throw another exception gives 500: a fault in the log, and an answer that an attacker can use to find a weak spot |
| 3 | The request objects carry no validation attributes, because the entities validate. SonarQube Cloud measures 100 percent line coverage on `EntityValidation.cs` and on the request objects, and 95.9 percent on the whole code (2026-11-12) | `src/NFHotel.Application/Dto`, `src/NFHotel.Domain/Errors/EntityValidation.cs` | Coverage shows that lines ran, not that every endpoint refuses every kind of bad input; a field that never reaches an entity is checked by nothing |
| 4 | The API serves no OpenAPI document | `src/NFHotel.Api` | The ZAP API scan needs a description of the endpoints |

## Deliverable

- **One test over every endpoint.** A test in `tests/NFHotel.Api.Tests` takes the endpoints from the test API itself (`ApiFactory`), so that a new endpoint is covered without a change of the test, and sends each one, with the API token and a staff token where it needs them, every kind of hostile input: a body that is not JSON, JSON of the wrong type, no body, `null` fields, a string of one megabyte, numbers above `Int32.MaxValue` and below zero, dates out of range, and Unicode edge cases (a NUL character, a right-to-left override, a lone surrogate). Every answer is in the 400 range, never 500, and its body holds no exception name, stack trace or setting.
- **The fixes.** Every 500 or leak that the test or the scan finds is fixed in the API in this milestone, each with a regression test.
- **The OpenAPI document, made at build time.** The document of the API is generated when the API is built; the running API does not serve it, and a test fails if `/openapi/v1.json` answers in Production. The packages it needs are pinned in the lock files.
- **ZAP against the API.** `dast-scan.sh` runs `zap-api-scan.py` with that document against the API of the throwaway stack, with the API token as a header, in the weekly full run and in a run by hand; its alerts pass the same gate and the same rules file as the other scans.
- **The documents.** The wiki pages Security-Controls (its section on the application) and Workflows (the API scan), and in `ops/security/zap-rules.tsv` a reason and an expiry for every rule that the API scan ignores or warns.

Order: the review of this document first (QC-MIL-001 and QC-LANG-001), then tasks 1 to 4 on one branch with their pull request, and task 5 after the first weekly run with the API scan.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The test covers every endpoint that the API maps (it compares its own count with the endpoints of the API) and every kind of input, and it passes; the review record shows it failing against an endpoint changed to throw `InvalidOperationException` on its input | Yes | No |
| 2 | Every defect that the test or the scan found has a regression test that failed before its fix | Yes | No |
| 3 | The build output holds the OpenAPI document with every endpoint, and a test proves that the API in Production answers 404 to `/openapi/v1.json` | Yes | No |
| 4 | The weekly run and a run by hand of the end-to-end and DAST workflow scan the API with its document: 0 High alerts that are not accepted, and every ignored or warned rule with a reason and an expiry at most 30 days ahead | Yes | No |
| 5 | Security-Controls, Workflows and the rules file say what the deliverable says, `test-wiki-coverage.sh` and `dast-scan.sh --check-rules` pass, the code review against `QC-CS-001`, `QC-SH-001` and `QC-WF-001` is Go, and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-015 | The database API and its endpoints |
| MIL-027 | The throwaway stack and the ZAP scans |
| MIL-044 | The gate and the rules file of the scans |
| MIL-067 | The same method, for the web pages |
| ADR-0002 | The field rules on the entities |
| ADR-0004 | The API in front of the database |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 31 and success criterion 34: the running application tested as an attacker would, the endpoints of the API included (in the Proposed revision of BC-001, RC-332) | Criteria 1, 2 and 4 |
| BC-001 objective 12: the field rules of the entities | Criteria 1 and 2: the rules hold at the edge of the API, for every endpoint |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-27. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The hostile-input test and its fixes | Written first: the test over every endpoint in `tests/NFHotel.Api.Tests`, with the endpoints taken from the test API and the kinds of input of the deliverable; it fails where an endpoint answers 500 or leaks a detail. Then each fix in the API, with its regression test | No | criteria 1 and 2 |
| 2 | The OpenAPI document at build time | The packages for a document made at build time, pinned in the lock files; the document in the build output; a test that the API in Production does not serve it | No | criterion 3 |
| 3 | ZAP against the API | `zap-api-scan.py` in `dast-scan.sh`, with the document and the API token of the throwaway stack, in the weekly full run and in a run by hand of `e2e-dast.yaml`; the gate and the rules file for its alerts | No | criterion 4 |
| 4 | The documents, code review and pull request | Security-Controls, Workflows and the rules file; the review record against `QC-CS-001`, `QC-SH-001` and `QC-WF-001`; the pull request, which closes the issues of tasks 1 to 4 | No | criterion 5 |
| 5 | Gate and review record | After the first weekly run with the API scan: the gate review record against criteria 1 to 5, the row of MIL-077 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 5 |

---

[BC-001]: ../business-case.md
[ADR-0002]: ../adr/adr-0002-validation-attributes-on-entities.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[MIL-015]: ./mil-015-database-api.md
[MIL-027]: ./mil-027-dast-and-end-to-end-tests.md
[MIL-044]: ./mil-044-security-overview.md
[MIL-067]: ./mil-067-bad-input-on-the-web-pages.md
[62301c4]: https://github.com/NF-Hotel/NFHotel/commit/62301c485e2ba5305b590865c00d02ad24988e59
