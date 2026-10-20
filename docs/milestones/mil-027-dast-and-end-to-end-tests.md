# MIL-027 DAST and end-to-end tests

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-027 |
| CrossReference | [BC-001], [US-001], [MIL-016], [MIL-017], [ADR-0005], [ADR-0012] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | S01 | Initial version | pending |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-104 Go (plan accepted by S01 in chat) | pending |

---

## Purpose

Decide whether NF Hotel may claim that every change is tested end to end in a running stack and scanned by dynamic application security testing (DAST) before it can be released.

Today the tests stop at the process boundary. Unit and integration tests and `WebApplicationFactory` page tests run inside one process. Nothing opens the real pages in a browser against the web application, the API and PostgreSQL together, and nothing attacks the running application. A broken page, a missing security header or an unprotected route in the deployed shape is found only by hand.

This milestone adds both, in CI, against a throwaway stack. Production and the Proxmox VM are never a target.

## Deliverable

- **Test stack.** A composite action in NF-Hotel/DevOps (ADR-0012) starts nginx with a self-signed certificate, the web application, the API and PostgreSQL. The web application and the API come from the images built in the pull request, so the tests and the scan see the deployed shape: TLS, forwarded headers and cookie flags.
  - It uses throwaway secrets, seeded bookings and one test staff account per role, on an isolated Docker network.
  - It removes everything afterwards.
- **End-to-end tests.** A new project, `tests/NFHotel.E2E.Tests`, uses xUnit v3 with Microsoft Playwright for .NET (headless Chromium).
  - It runs in the Playwright Docker image, never on a host desktop (`framework/process/gui-testing.md`).
  - Each run writes the test results, a trace and a screenshot of every failure to a folder, uploaded as a workflow artifact.
- **DAST baseline.**
  - The OWASP ZAP baseline scan (passive) runs on every pull request against the public pages and the back office login page.
  - It also runs signed in, as a test Manager, against `/admin/**`.
  - Rules that are accepted live in `security/zap-rules.tsv`, each with a reason and an expiry.
  - The report is kept as HTML and SARIF.
- **DAST active scan.** The ZAP full (active) scan runs every week on `main`, and by hand, against the same throwaway stack. Its findings go to code scanning.
- **Required checks.** "End-to-end tests" and "DAST baseline" are required by the ruleset "Approve merges".

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A pull request that breaks the booking flow (the receipt page answers 500) fails "End-to-end tests". The test is made with a temporary change, then reverted | Yes | No |
| 2 | The end-to-end tests cover these flows in the browser. A guest books a room from the home page and sees the receipt, and an incomplete guest form shows its field errors. `/admin` without a session goes to the login page. Each of Receptionist, Manager and Housekeeping opens one allowed page and gets the access-denied page on one refused page (Access Control Matrix). Housekeeping never sees a passport number. The sixth failed sign-in gets 429 | Yes | No |
| 3 | The ZAP baseline runs on every pull request, signed out and signed in. It reports 0 High alerts. Every rule set to IGNORE or WARN in `security/zap-rules.tsv` has a reason and an expiry date at most 30 days ahead | Yes | No |
| 4 | The weekly active scan has run on `main` and its report is stored. Its target is the job's own Docker network: 0 requests go to the VM, the tailnet or any public NF Hotel host | Yes | No |
| 5 | The tests are repeatable. 0 fixed sleeps (Playwright waits for the page). Test data is seeded on each run. 0 real secrets. Three reruns of the same commit are all green | Yes | No |
| 6 | "End-to-end tests" and "DAST baseline" are required status checks of the ruleset "Approve merges" | Yes | No |
| 7 | The reviews against `qc-programming-csharp`, `qc-programming-shell`, `qc-workflow` and `qc-yaml` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The roles, the login page, the access-denied page and the rate limit that the tests check |
| MIL-017 | The security headers and the cookie flags that the DAST baseline checks |
| MIL-021 (NF-Hotel/DevOps) | The release-images action that builds the images the stack runs |
| MIL-025 | The DevOps repository that holds the stack and scan actions (ADR-0012) |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Success criterion 22 (back office login): proven in a browser against a running stack | Task 5 |
| Success criterion 23 (TLS, cookie flags) and 27 (verified supply chain): checked by DAST | Tasks 6, 7 |
| Success criterion 26 (approved pipeline): two more required checks | Task 8 |
| US-001.06 to US-001.09: the staff stories, seen through the roles | Task 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-17, four weeks. It runs beside MIL-022 (target 2026-12-06) and needs no change to the VM, so it does not wait for CD.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Throwaway test stack action | Add the composite action `e2e-stack` in NF-Hotel/DevOps. It starts nginx with a self-signed certificate (`make-dev-certificate.sh`), nfhotel-web, nfhotel-api from the images built in the PR, and PostgreSQL 16. Everything runs on an isolated Docker network with generated secrets, and the action waits for the health checks. Tested in DevOps CI; it never reaches the VM or the tailnet. | No | ADR-0012 |
| 2 | Seeded test data | Seed the bookings with `seed-market-data` and one staff account per role (Receptionist, Manager, Housekeeping) with `create-staff`, using throwaway passwords. Nothing is written to a real database, and no password is in git. | No | MIL-016 |
| 3 | End-to-end test project | Create `tests/NFHotel.E2E.Tests` (xUnit v3, Microsoft.Playwright). It is run by a script in the Playwright Docker image, headless Chromium (environment: Docker container), which writes results, traces and screenshots to a mapped folder. It is excluded from the normal `dotnet test` run. | No | |
| 4 | Guest booking flow in the browser | Tests that a guest opens the home page, books a free room and sees the receipt, and that an incomplete guest form shows its field errors. The public booking pages have no user story yet. Environment: Docker container. | No | |
| 5 | Back office roles in the browser | Tests the redirect to the login without a session, sign-in and sign-out, one allowed and one refused page per role, no passport number for Housekeeping, and 429 on the sixth failed sign-in. Environment: Docker container. | No | US-001.09 |
| 6 | ZAP baseline on pull requests | Add the composite action `dast-scan` in NF-Hotel/DevOps with ZAP pinned by digest. It runs the baseline signed out and signed in against the stack, reads `security/zap-rules.tsv`, fails on a High alert, and uploads HTML and SARIF. Entries in the rules file need a reason and an expiry. | No | |
| 7 | Weekly ZAP active scan | A scheduled workflow on `main` (and by hand) starts the stack and runs the ZAP full scan against it only. The report is stored and the SARIF goes to code scanning. A new High alert opens an issue. | No | |
| 8 | Wire into NFHotel and require the checks | An NFHotel workflow calls both actions on pull requests, pinned by SHA. S01 adds "End-to-end tests" and "DAST baseline" to the ruleset "Approve merges". | No | |
| 9 | Check and review | Prove criteria 1 to 6: the broken-receipt PR, the three reruns, the scan reports and the ruleset. Review records against the four checklists. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-016]: ./mil-016-backoffice-login.md
[MIL-017]: ./mil-017-tls.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0012]: ../adr/adr-0012-devops-repository-ownership.md
