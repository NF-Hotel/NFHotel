# RC-502 Review of ADR-0500 and the frozen-image change

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-502 |
| CrossReference | [ADR-0500], [QC-ADR-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [150af89] |

---

## Artifact Under Review

- **Instances reviewed:**
  - [ADR-0500];
  - the frozen-image branch of `scripts/build-release-images.sh` and `tests/check-release-build.sh`;
  - `deploy/market-analysis/frozen-image`.
- **Checklists used:** [QC-ADR-001] for the ADR; [QC-SH-001] for the changed lines.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The decision uses the technical authority S01 delegated in chat on 2026-10-19; the product decision about the new repository stays with S01 (issue #2).

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| ADR 1 | ID matches the file and is sequential | Pass | ADR-0500, the first of the DevOps range (registry) |
| ADR 2 | Context states the problem, the forces and the options | Pass | The deleted source repository, the blocked releases, the last good digest; three options |
| ADR 3 | At least two options | Pass | Three |
| ADR 4 | Decision in one or two unhedged sentences, matching an option | Pass | "We freeze `market-analysis` at its last good digest until S01 decides the service of the new repository": option 3 |
| ADR 5 | Positive and negative consequences | Pass | Three positive, two negative (no security rebuilds, dependence on the GHCR version) |
| ADR 6 | Quality attributes named | Pass | Reliability up; maintainability and security of the one image frozen |
| ADR 7 | Affected Artifacts linked and in CrossReference | Pass | ADR-0007, DSA-001, MIL-021 |
| ADR 11 | Status values | Pass | `Proposed`, then `Accepted` |
| SH 2, 4, 12 | Quoting, shellcheck, shfmt | Pass | `tests/lint.sh` clean |
| SH 5 | Errors | Pass | A malformed `frozen-image`, or a digest missing from the registry with `--push`, stops the release with `error:` |
| SH 8 | Explicit flag for external changes | Pass | The registry is read only with `--push`; nothing is written for a frozen image |
| SH 11 | Tested | Pass | A local build of NFHotel `main` builds three images and reports the frozen one; the CI check passes it; the push path is verified by the next NFHotel release |

## Overall Verdict

Go: the change unblocks releases without changing what the VM runs, and states its cost.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide the market analysis service and end the freeze (issue #2) | S01 | Before MIL-022 (Trivy will flag the frozen image) |
| Keep the frozen package version in GHCR (no clean-up policy may delete it) | S03 | While frozen |

---

[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[150af89]: https://github.com/NF-Hotel/DevOps/commit/150af89418a891c0fe10d6da3e519ab54290b7cc
