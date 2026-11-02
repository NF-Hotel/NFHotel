# RC-258 Review of objective 29, US-001.12 and the use case Approve a Price Suggestion

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-258 |
| CrossReference | [BC-001], [US-001], [UCD-001], [MIL-051], [QC-BC-001], [QC-US-001], [QC-UCD-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; accepted by S01 in chat on 2026-11-01 | pending |

---

## Artifact Under Review

- Instances reviewed: the delta of [BC-001] (objective 29, success criterion 32, one In Scope line), of [US-001] (US-001.12) and of [UCD-001] (the use case Approve a Price Suggestion, the Manager role), all made by task 1 of [MIL-051].
- Checklists used: [QC-BC-001], [QC-US-001] and [QC-UCD-001]
- Scope: delta review of the added text; the rest of each document is unchanged
- Language and domain: en / it (the Metadata rows)
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: supervised (the AI wrote and pre-reviewed; S01 reviews and accepts in chat)

## Checklist Results

### BC-001 ([QC-BC-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | Success criteria are measurable | Pass | Criterion 32 states 20 percent, single rooms only, one change per approval, 403 for two roles, one audit row per event, and the tests that check them |
| 4 | Scope separates In Scope from Out of Scope | Pass | The In Scope line is added; it states that a booking already made keeps its price (proposed in MIL-051 task 2, for S01 to confirm) |
| 5 | Stakeholders cross-referenced by ID | Pass | No stakeholder is described inline |
| 1, 2, 7, 8 | Cost-benefit, risks, assumptions, recommendation | N-A | Not changed by this delta |

### US-001.12 ([QC-US-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Follows INVEST | Pass | Independent of other stories except the market analysis report (US-001.01) and the roles (US-001.09, US-001.11); the split into tasks is stated |
| 2 | As a / I want / So that | Pass | |
| 3 | Testable acceptance criteria | Pass | Seven Given/When/Then criteria |
| 4 | Traceable to a use case | Pass | UCD-001 "Approve a Price Suggestion" |
| 5 | Fits one iteration | Fail (Optional) | Flagged in the Size column; MIL-051 tasks 3 to 8 are the split |
| 6 | No technical implementation detail | Pass | The criteria name the thresholds and the roles, not tables or classes |
| 7 | The role matches an actor of the Use Case Diagram | Pass | Hotel staff (Manager) |
| 9 | Priority with a reason | Pass | Should, with the reason |

### UCD-001 ([QC-UCD-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | Include relationships used correctly | Pass | The use case includes Sign In To The Back Office, like the other staff use cases |
| 4 | No orphan actor | Pass | Hotel staff performs it |
| 6 | Verb phrase name | Pass | Approve a Price Suggestion |
| 9 | Story and priority listed | Pass | US-001.12, Should |
| 10 | Role matrix marked as a suggestion | Pass | The row is added to the existing matrix, Manager only |
| 1, 2, 5, 7, 8 | Other criteria | Pass | Unchanged by the delta; the new use case follows the naming of the others |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 to 4, 6, 7 | Rows, code, language, register, English keys, no twin | Pass | English, IT Executive English for the Business Case and IT Professional English for the story and the diagram |
| 5 | Domain terms are those of the dictionary | Pass, on condition | Season, price suggestion, price change and room rate are added to the dictionary by MIL-051 task 2 |
| 8 | A change of language or domain has a Version History row | N-A | Unchanged |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01 reads and confirms in chat |

## Overall Verdict

Go: every Mandatory criterion passes; one Optional criterion (the size of US-001.12) is flagged and split by the tasks. S01 accepted the three documents in chat on 2026-11-01.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept objective 29, US-001.12 and the use case, or ask for changes | S01 | 2026-11-06 |
| Confirm that a booking already made keeps its price | S01 | before task 3 |

---

[BC-001]: ../../business-case.md
[US-001]: ../../user-stories.md
[UCD-001]: ../../use-case-diagram.md
[MIL-051]: ../../milestones/mil-051-room-price-suggestions.md
[QC-BC-001]: ../../../framework/qc/artifacts/qc-business-case.md
[QC-US-001]: ../../../framework/qc/artifacts/qc-user-story.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
