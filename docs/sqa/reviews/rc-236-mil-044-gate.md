# RC-236 Gate review of MIL-044

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-236 |
| CrossReference | [MIL-044], [RC-230], [RC-233], [RC-234], [ADR-0032], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the seven Go/No-Go criteria of [MIL-044], after #371 and #372 were merged, released and deployed, and after a ZAP full scan of `main` (run 40292037002).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR-0032 records the policy and the alternatives | Go | ADR-0032 (RC-233 Go) |
| 2 | No code scanning configuration error; no `/language:python` configuration | Go | The analyses API lists no `/language:python` analysis; the overview shows no error (S01's screenshot) |
| 3 | No open Trivy alert lacks a fixed version; every image keeps its JSON report and SBOM | Go | 0 open Trivy alerts after the release of `f1d28a7`: the SARIF holds the fixable findings only, and `apt-get upgrade` installed the four that had a fix. The JSON report and the SBOM attestation of each image are unchanged |
| 4 | Every open ZAP alert is a rule not set to IGNORE; each IGNORE has a reason | Go | 0 open ZAP alerts after the full scan; `zap-rules.tsv` holds 9 IGNORE lines, each with its reason and the expiry 2026-11-25 (`dast-scan.sh --check-rules` passes) |
| 5 | No "nugetCommand not pinned"; every other Scorecard alert is fixed or dismissed with a reason | Go | 0 open Scorecard alerts; restores run with `--locked-mode`; Code-Review, Fuzzing and CII-Best-Practices dismissed as "won't fix" with their reasons |
| 6 | Private vulnerability reporting is enabled and `SECURITY.md` links it | Go | The API answers `enabled: true`; `SECURITY.md` links `security/advisories/new` |
| 7 | CI green; code review Go | Go | #371 (39 checks) and #372 (40 checks) green; RC-234 Go, its condition met |

## Findings

- **From 565 to 0 open alerts** in code scanning between the start of the milestone and this review.
- **Two faults found on the way and fixed in the milestone:** the NuGet lock files depend on the exact SDK (regenerated with the SDK of `global.json`, and the rule is in `Directory.Build.props`), and ZAP lists the alerts of IGNORE rules in its report (now left out of the SARIF).
- **Code quality** (GitHub Code Quality, preview) was turned on by S01 during the milestone; it is not part of the security overview and has its own findings.

## Overall Verdict

Go. MIL-044 is complete; this pull request closes #369.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Renew or drop the ZAP IGNORE lines | S01 | 2026-11-25 |

---

[MIL-044]: ../../milestones/mil-044-security-overview.md
[RC-230]: rc-230-mil-044.md
[RC-233]: rc-233-adr-0032.md
[RC-234]: rc-234-mil-044-code.md
[ADR-0032]: ../../adr/adr-0032-what-code-scanning-shows.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
