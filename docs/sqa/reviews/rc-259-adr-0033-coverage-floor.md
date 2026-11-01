# RC-259 Review of the ADR-0033 amendment for the coverage floor

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-259 |
| CrossReference | [ADR-0033], [MIL-054], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [4395227] |

---

## Artifact Under Review

- Instance reviewed: [ADR-0033], the row of 2026-11-01 (decision 5, "Target and floor", and the renumbered decision 6)
- Checklist used: [QC-ADR-001]
- Scope: delta re-review of criteria 1 to 11 for the amendment; the rest of the record was reviewed in RC-241
- Language and domain: en, it
- Language reviewer: none
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked in chat on 2026-11-01 to plan for close to 100 % coverage and accepted MIL-054 ("Ja, godkend og byg"); the floor and the exclusions were written by the AI under MIL-054 task 1.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ID matches the filename, unique and sequential | Pass | Unchanged, `adr-0033-…` |
| 2 | Context states the problem, the forces and the options | Pass | Unchanged; the reason for the floor is in MIL-054 (77.0 % on `main`, the web front end at 58 %) |
| 3 | At least two options considered | Pass | Unchanged; the floor adds to option 3 and does not replace it |
| 4 | Decision stated unhedged and matches an option | Pass | Decision 5 states the two floors, where the check runs and the two exclusions |
| 5 | Positive and negative consequences | Pass | The existing consequences still hold; the cost of the floor (a red build when coverage drops) follows from decision 5 and the gate of decision 6 |
| 6 | Quality-attribute impacts named | Pass | Maintainability (testability) and functional suitability, unchanged |
| 7 | Affected Artifacts as links, also in CrossReference | Pass | MIL-054 added to both |
| 8 | Status changes as new rows | Pass | A new row of 2026-11-01; the row of 2026-10-27 is kept as it was |
| 9 | A superseded ADR names its successor | N-A | Not superseded |
| 10 | Domain language, no unexplained abbreviations | Pass | Cobertura and CI are terms of the domain |
| 11 | Status values allowed | Pass | `Accepted` |

## Overall Verdict

Go: every Mandatory criterion passes. The two exclusions match criterion 5 of MIL-054 and stand with their reasons in `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml`.

## Action Items

None.

---

[ADR-0033]: ../../adr/adr-0033-sonarqube-cloud-coverage.md
[MIL-054]: ../../milestones/mil-054-test-coverage.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[4395227]: https://github.com/NF-Hotel/NFHotel/commit/4395227efef641b6f8939f00e52c3640334b1032
