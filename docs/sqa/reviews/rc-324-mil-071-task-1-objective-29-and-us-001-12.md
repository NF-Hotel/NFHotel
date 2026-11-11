# RC-324 Review of objective 29 of BC-001 and US-001.12 (MIL-071 task 1)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-324 |
| CrossReference | [BC-001], [US-001], [QC-BC-001], [QC-US-001], [MIL-071] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; accepted by S01 in chat on 2026-11-11 | pending |

---

## Artifact Under Review

- Instance reviewed: [BC-001] (objective 29) and [US-001] (US-001.12), with the note added to MIL-055
- Checklist used: [QC-BC-001] and [QC-US-001]
- Scope: delta re-review of the criteria that the change touches (QC-BC-001 criteria 3, 4 and 8; QC-US-001 criteria 1 to 4 and 7); the criteria of the earlier reviews (RC-262, RC-263) are unchanged
- Language and domain: en / it
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: supervised (the AI wrote the change and the pre-review; S01 reviews and accepts in chat)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| BC 3 | Success criteria are measurable | Pass | Objective 29 names the 80 percent threshold, the horizon of two months and the approval by a Manager; its checks are the Go/No-Go criteria of MIL-071 |
| BC 4 | Scope separates In Scope vs Out of Scope | Pass | Unchanged; the suggestions stay inside the market analysis scope |
| BC 8 | Clear, unambiguous recommendation | Pass | Objective 29 states who makes the suggestions (the service), who presents them (the back office) and who decides (the Manager) |
| US 1 | Follows INVEST | Pass | Still one story: approve a price suggestion; testable by the acceptance criteria |
| US 2 | "As a / I want / So that" form | Pass | Role S05 Manager kept |
| US 3 | Clear, testable acceptance criteria | Pass | 8 criteria; the new ones state the holiday name, the history and the 80 percent threshold, the case of too few years, and the empty page when the service made nothing |
| US 4 | Traceable to a use case or epic | Pass | Traces to objective 29 and MIL-071 |
| US 7 | Role matches an actor of the Use Case Diagram | Pass | The Manager is unchanged |

## Overall Verdict

Go: every criterion that the change touches passes. The change replaces the wording of MIL-055 ("calculated by a fixed rule in the back office from the occupancy of a month") with a rule in the service over holidays and weeks of the next two months; S01 chose this in chat on 2026-11-11. The page, the approval, the undo and the audit trail are unchanged.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the change of objective 29, US-001.12 and the note in MIL-055 | S01 | 2026-11-13 |
| Agree the contract with notebook-market-analysis MIL-002 task 1 (done in that repository; read it with this change) | S01 | before MIL-071 task 2 |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[QC-BC-001]: ../../framework/qc/artifacts/qc-business-case.md
[QC-US-001]: ../../framework/qc/artifacts/qc-user-story.md
