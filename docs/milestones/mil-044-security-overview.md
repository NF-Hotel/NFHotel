# MIL-044 A security overview that shows what to act on

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-044 |
| CrossReference | [BC-001], [MIL-022], [MIL-036], [ADR-0019], [ADR-0030] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-26 to fix the security overview so nothing shows red ("Kan du fixe alt det her så det ikke viser rødt"), as a GitHub and security professional ; S01 accepted it in chat ("Jeg accepterer planerne for MIL-044 og MIL-045"; S01 chose the MIT licence; accepted after RC-230 Go) | pending |

---

## Purpose

Decide whether NF Hotel may claim that its GitHub security overview shows no error and no open alert that nobody acts on. On 2026-10-26 it shows:

- **A red CodeQL configuration error.** It comes from a stale configuration, `/language:python`, last analysed on 2026-10-19 with "unsuccessful execution". The repository has no Python code since ADR-0016, so it never runs again, and the error stays.
- **565 open code scanning alerts.** 470 are Trivy findings in the four images with no fixed version yet (Debian or Ubuntu has not shipped one); the release gate already accepts those (DSA-001 section 8). 72 are OWASP ZAP findings, many from a full scan of 2026-10-20 that predates the header fixes of MIL-033. 8 are Scorecard checks, some of which are decisions already made (ADR-0019: no required review).
- **Private vulnerability reporting off,** so `SECURITY.md` cannot point to a private channel.

## Deliverable

- ADR-0032: what the code scanning tab shows (findings someone can act on) and where the rest lives (the Trivy report and SBOM of every release, the daily scan), with the reasons.
- The stale CodeQL configuration is removed, and the overview shows no configuration error.
- Trivy uploads only the findings with a fixed version to code scanning; the full report and the SBOM stay as artifacts and attestations; the gate does not change.
- ZAP: a fresh full scan; the findings that are real are fixed in the headers, and the informational rules are set in `ops/security/zap-rules.tsv` with a reason each.
- Scorecard: the NuGet restores are locked (`packages.lock.json`, `--locked-mode`), `SECURITY.md` links the private reporting form, a `LICENSE` file with the MIT licence that S01 chose, and the checks that are decisions (Code-Review, Fuzzing, CII badge) are dismissed with the reason.
- Private vulnerability reporting enabled.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0032 records the policy and the alternatives, with the reasons | Yes | No |
| 2 | The security overview shows no code scanning configuration error, and the analyses API lists no `/language:python` configuration | Yes | No |
| 3 | After the next release, no open Trivy alert lacks a fixed version; every image still has its full Trivy JSON report and SBOM | Yes | No |
| 4 | After a fresh ZAP full scan, every open ZAP alert is a rule that is not set to IGNORE, and each IGNORE has a reason in `zap-rules.tsv` | Yes | No |
| 5 | Scorecard Pinned-Dependencies has no "nugetCommand not pinned"; every other open Scorecard alert is either fixed or dismissed with a reason | Yes | No |
| 6 | Private vulnerability reporting is enabled and `SECURITY.md` links it | Yes | No |
| 7 | CI is green and the review against `qc-yaml`, `qc-workflow`, `qc-programming-shell` and `qc-programming-csharp` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-043 | Merged first; this work starts from it |
| S01 | Chose the MIT licence in chat on 2026-10-26 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Criterion 27 (verified supply chain): findings are visible where they can be acted on | Tasks 1 to 5 |
| Criterion 28 (hardened server): the Scorecard result is current and explained | Task 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-02.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0032 | What code scanning shows and where the rest lives: fixable findings only, against every finding or dismissing by hand; DSA-001 section 8 gets a pointer. | No | criterion 1 |
| 2 | CodeQL configuration | Delete the analyses of the stale `/language:python` configuration through the API (they are stale data; nothing else is touched), check the languages of the default setup. | No | criterion 2 |
| 3 | Trivy in code scanning | `scan-image.sh` writes the SARIF from the fixable findings; the JSON report, the gate and the SBOM keep everything; tests; the alerts without a fix close at the next release. | No | criterion 3 |
| 4 | ZAP findings | Run the full scan on `main`; fix the real header findings in the web application (with tests); set the informational rules to IGNORE in `zap-rules.tsv` with reasons (`test-dast-rules.sh`). | No | criterion 4 |
| 5 | Scorecard and reporting | NuGet lock files and `--locked-mode` in CI and `make-migration-script.sh`; enable private vulnerability reporting and link it in `SECURITY.md`; add `LICENSE` (MIT, S01's choice); dismiss Code-Review (ADR-0019), Fuzzing and CII-Best-Practices, each with the reason. | No | criteria 5, 6 |
| 6 | Release and check | Merge, follow the release, prove the criteria on the overview; reviews. | No | criteria 2 to 7 |

---

[BC-001]: ../business-case.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-036]: ./mil-036-security-operations.md
[ADR-0019]: ../adr/adr-0019-approvals-by-the-owner.md
[ADR-0030]: ../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
