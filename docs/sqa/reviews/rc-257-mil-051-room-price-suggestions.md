# RC-257 Review of MIL-051 Room price suggestions

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-257 |
| CrossReference | [MIL-051], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; accepted by S01 in chat on 2026-11-01 | [3a17eee] |

---

## Artifact Under Review

- Instance reviewed: [MIL-051]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it (the Metadata rows of MIL-051)
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: supervised (the AI wrote the milestone and the pre-review; S01 reviews and accepts in chat)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Deliverable lists the rule, the suggestions, the settings page, the rates and price changes, the approval page, the audit events, the permission, tests and documents |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 11 criteria, each with figures (80/40 percent, 100 to 120, dates around the period) or a check that can be run |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | MIL-026, MIL-028 and MIL-030, each with its reason |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass, on condition | Traces to objective 29 and US-001.12, which task 1 adds; the plan-first gate keeps code back until task 1 is accepted |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01; approving reviewer S01, and S04 for the Business Case text |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-27, marked for S01 to confirm; the Business Case sets no deadline for this work |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | |
| 3 | Content in the stated language | Pass | English; the owner's request is stated in English |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass, on condition | Season, price suggestion, price change and room rate are new terms; task 2 adds them to the dictionary |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | First version |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01 reads and confirms in chat |

## Overall Verdict

Go: every Mandatory criterion passes; two pass on condition (criterion 4 of QC-MIL-001 and criterion 5 of QC-LANG-001), which tasks 1 and 2 fulfil before any code. S01 accepted MIL-051 in chat on 2026-11-01.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Confirm the open points of task 2: thresholds 80 and 40 percent, discount 15 percent, cap 30 percent, years of the report read, effect on bookings already made | S01 | before task 3 |
| Accept MIL-051 or ask for changes | S01 | 2026-11-06 |
| Merge MIL-026 first, since the page reads its report | S01 | 2026-11-13 |

---

[MIL-051]: ../../milestones/mil-051-room-price-suggestions.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
