# RC-234 Code review of MIL-044

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-234 |
| CrossReference | [MIL-044], [ADR-0032], [QC-SH-001], [QC-YML-001], [QC-WF-001], [QC-CS-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** `ops/scripts/scan-gate.sh` (`--fixable`), `ops/scripts/scan-image.sh` (the SARIF from the fixable findings), `ops/tests/test-scan-gate.sh`; `Directory.Build.props` (`RestorePackagesWithLockFile`), the 17 `packages.lock.json`, the two restores of `.github/workflows/ci.yaml` and the one of `ops/scripts/make-migration-script.sh` in locked mode; `ops/security/zap-rules.tsv`; `SECURITY.md`; `LICENSE`.
- **Actions outside the repository, under the accepted plan:** the 9 analyses of the stale CodeQL configuration `/language:python` on `main` deleted through the API (stale data of 2026-10-18 and 19; nothing else touched); private vulnerability reporting enabled; the Scorecard alerts Code-Review, Fuzzing and CII-Best-Practices dismissed as "won't fix" with their reasons; a ZAP full scan run on `main` (run 40285164909).
- **Checklists used:** [QC-SH-001], [QC-YML-001], [QC-WF-001], [QC-CS-001], the rows that the change touches.
- **Scope:** delta review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `ops/tests/lint.sh`; `test-scan-gate.sh` in Debian 12 (26 of 26, 6 new); `dast-scan.sh --check-rules`; `dotnet restore --locked-mode`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2, 4, 12 | Strict mode, quoting, shellcheck, shfmt | Pass | 0 findings |
| SH 5 | Errors and usage | Pass | `--fixable` without a readable report exits 2; a failed filter stops the scan |
| SH 11 | Behaviour is tested | Pass | `--fixable` keeps the findings with a fix and every secret, drops those without, accepts a report without results, refuses a missing one |
| SH 15 | Defined once | Pass | The filter lives in `scan-gate.sh` beside the gate that reads the same fields |
| WF 7–8 | Permissions, pinning | Pass | No new action; restores in locked mode |
| YML 1–3 | Parses, yamllint | Pass | 0 findings |
| CS 3 | Builds, no suppressions | Pass | Lock files only; `--locked-mode` restores the solution |

## Findings

- **The ZAP header findings were stale.** The fresh full scan of `main` no longer reports Cross-Origin-Embedder-Policy, the CSP fallback, X-Content-Type-Options, the Server version or Permissions-Policy; MIL-033 had fixed them after the scan of 2026-10-20. What remains is informational, and each rule is set to IGNORE with its reason until 2026-11-25.
- **Trivy alerts close at the next release.** The upload of each image replaces its category, so the alerts without a fix close when the release after this merge uploads its SARIF.
- **The License and Security-Policy checks** of Scorecard are answered by `LICENSE` and the links in `SECURITY.md` at its next run.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22, which closes when CI of the pull request is green.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Renew or drop the ZAP IGNORE lines before they expire | S01 | 2026-11-25 |

---

[MIL-044]: ../../milestones/mil-044-security-overview.md
[ADR-0032]: ../../adr/adr-0032-what-code-scanning-shows.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
