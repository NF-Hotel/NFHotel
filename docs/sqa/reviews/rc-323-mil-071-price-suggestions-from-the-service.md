# RC-323 Review of MIL-071 Price suggestions from the market analysis service

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-323 |
| CrossReference | [MIL-071], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-11 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; accepted by S01 in chat on 2026-11-11 | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-071]
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: full review
- Language and domain: en / it (the Metadata rows of MIL-071)
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: supervised (the AI wrote the milestone and the pre-review; S01 reviews and accepts in chat)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Deliverable lists the contract and client, the removal of the rule, the limits that stay in the website, the page, the empty states, tests and documents |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | 9 criteria, each with a figure (100 to 120, the cap and the minimum price) or a check that can be run (no `SeasonRule` left, architecture test, 403 for other roles, a look on the dev VM) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | notebook-market-analysis MIL-002, MIL-055 and MIL-028, each with its reason; MIL-002 task 1 is the contract both build on |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass, on condition | Traces to objective 29 and US-001.12, which task 1 words again; the plan-first gate keeps code back until task 1 is accepted |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01; approving reviewer S01, and S04 for the Business Case text |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-12-11, two weeks after MIL-002; marked for S01 to confirm |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | |
| 3 | Content in the stated language | Pass | English; S01's request in Danish is stated in English |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | Season, price suggestion, price change and room rate are the terms of MIL-055; "reason code" is the only new word and is used in the milestone of the service as well |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | First version |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass, on condition | S01 reads and confirms in chat |

## Overall Verdict

Go: every Mandatory criterion passes; two pass on condition (criterion 4 of QC-MIL-001, which task 1 fulfils before any code, and criterion 9 of QC-LANG-001, which S01's acceptance fulfils). The milestone replaces a decision of MIL-055 ("no AI and no service decides a season"); the replacement is stated in the Purpose, S01 chose it in chat on 2026-11-11, and the AI never sets a price.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept MIL-071 or ask for changes | S01 | 2026-11-13 |
| Confirm the target date 2026-12-11 | S01 | 2026-11-13 |
| Agree the contract of MIL-002 task 1 (the `pricing` block and `price_suggestions`) before any code in either repository | S01 | before task 2 |
| Note in MIL-055 that its rule is replaced (task 1) | S01 | with task 1 |

---

[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
