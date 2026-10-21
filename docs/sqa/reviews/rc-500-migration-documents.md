# RC-500 Review of the documents changed by the migration to NF-Hotel/DevOps

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-500 |
| CrossReference | [MIL-021], [ADR-0010], [DSA-001], [ADR-0012], [QC-MIL-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [c1dc5f7] |

---

## Artifact Under Review

- **Instances reviewed:** the changes of 2026-10-19 to [MIL-021] (deliverable, criterion 10), [ADR-0010] (decision 3) and [DSA-001] (section 2.4.3), made for the repository boundary of [ADR-0012]. These documents were otherwise moved unchanged, apart from their links (`MIGRATION.md`). Their earlier reviews (RC-089 to RC-101) stay valid.
- **Checklists used:** [QC-MIL-001] for MIL-021; [QC-ADR-001] for ADR-0010.
- **Scope:** the changes only.
- **Language and domain:** en / it, unchanged.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). An independent review is requested on the migration pull request.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| MIL 1 | A concrete deliverable is defined for every gate | Pass | The release and CD deliverables name the NFHotel entry workflows and the actions of this repository |
| MIL 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 10 now checks for a checkout of `NF-Hotel/DevOps`, which `deploy-release.sh` enforces (tested: `test-deploy-release.sh`) |
| MIL 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged; ADR-0012 is named in the change |
| MIL 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged (objective 23, criterion 26) |
| MIL 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| MIL 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged |
| ADR 4 | Decision is stated in one or two unhedged sentences | Pass | The decision of ADR-0010 is unchanged; decision 3 names the repository that is now checked out, and the old one in brackets |
| ADR 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | ADR-0012 is linked from decision 3 |
| ADR 8 | Status changes are recorded as new Version History rows | Pass | A new `Accepted` row; the row before is `Deprecated`; the two latest rows are kept (rule 7) |
| DSA | No catalog checklist | N-A | Section 2.4.3 states the boundary and refers to ADR-0012; the type is proposed upstream |

## Overall Verdict

Go — the changes keep MIL-021 and ADR-0010 consistent with ADR-0012 and with the code.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Independent review of the migration pull request | Tirsvad (S01) | Before the merge |

---

[MIL-021]: ../../milestones/mil-021-secure-cicd-foundation.md
[ADR-0010]: ../../adr/adr-0010-production-vm-as-deployed.md
[DSA-001]: ../devsecops-assessment.md
[ADR-0012]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0012-devops-repository-ownership.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[c1dc5f7]: https://github.com/NF-Hotel/DevOps/commit/c1dc5f74e11ce4e10f7f1dbdf5ef304e4db8429e
