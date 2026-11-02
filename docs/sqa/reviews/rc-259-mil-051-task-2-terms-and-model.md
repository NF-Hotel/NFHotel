# RC-259 Review of UC-007, the dictionary and the Domain Model for price suggestions

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-259 |
| CrossReference | [UC-007], [DICT-001], [DM-004], [MIL-051], [QC-UC-001], [QC-DM-001], [QC-DICT-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | Claude (AI) | S01 | Initial version; pre-review by the AI, not independent; accepted by S01 in chat on 2026-11-01 | [3a17eee] |

---

## Artifact Under Review

- Instances reviewed: [UC-007] (new), the delta of [DICT-001] (five terms) and of [DM-004] (four concepts, three associations, four audit actions), all made by task 2 of [MIL-051].
- Checklists used: [QC-UC-001] (Fully Dressed), [QC-DM-001] and [QC-DICT-001]
- Scope: full review of UC-007; delta review of the dictionary and the Domain Model
- Language and domain: en / it (the Metadata rows)
- Language reviewer: none; the reviewer reads the language and knows the domain
- Mode: supervised (the AI wrote and pre-reviewed; S01 reviews and accepts in chat)

## Checklist Results

### UC-007 ([QC-UC-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 3 | All standard sections are present | Pass | Scope, level, actor, stakeholders, pre- and postconditions, scenario, extensions, rules, open issues |
| 4 | Pre- and postconditions defined | Pass | |
| 5 | Primary actor stated | Pass | Manager |
| 6 | Stakeholders and interests | Pass | S01, S05, S04 with an interest each |
| 7 | Numbered steps | Pass | Seven steps |
| 8 | Include and extend used correctly | Pass | The sign-in is a precondition (UC-004), the role refusal points to UC-004 6a; no include or extend is misused |
| 9 | Business rules per step | Pass | Rules table by step |
| 10 | Names match the diagram and the story | Pass | "Approve a Price Suggestion", Manager |
| 11 | Scope and level stated | Pass | |
| 12 | Actor goal, no UI detail | Pass | Pages are named only as "Price suggestions" and the room page, like UC-006 |

### Domain Model ([QC-DM-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Business language | Pass | |
| 2 | Multiplicities | Pass | Price change to Staff member 0..* to 1; Price suggestion 0..1 to Price change 0..1 |
| 3 | No operations | Pass | |
| 4 | Association reading direction | Pass | "is approved by", "becomes" |
| 6 | Every concept traces to the use cases or glossary | Pass | Each row cites UC-007 steps and the dictionary |
| 7 | Attributes are simple data | Pass | "Approved by" was removed from Price change, since the association carries it; room size is the existing attribute of Room |

### Dictionary ([QC-DICT-001])

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Each row complete | Pass | |
| 2 | One IT term per PO term | Pass | Room rate, Price change, Price suggestion, Season, Pricing settings: no synonym of an existing row |
| 3 | Every concept has a row | Pass | The four concepts and Season |
| 7 | Language and domain | Pass | en and it |
| 8 | Rows ascend by PO term | Fail | Pre-existing: the rows are in the order of introduction, not by the alphabet, so the five new rows follow the same order. Not caused by this delta; to be fixed in a separate sort of the whole table |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 to 4, 6, 7 | Rows, code, language, register, English keys, no twin | Pass | English; IT Professional English for the use case, the model and the dictionary |
| 5 | Domain terms are those of the dictionary | Pass | UC-007 and the Domain Model use the five new terms only |
| 8 | A change of language or domain has a Version History row | N-A | Unchanged |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01 reads and confirms in chat |

## Overall Verdict

Go-with-conditions: every Mandatory criterion of the delta passes. Criterion 8 of the dictionary fails on rows that were already out of order; it is listed as an action and not caused by this change. UC-007, the dictionary and the Domain Model were accepted by S01 in chat on 2026-11-01, with the defaults.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept UC-007, the five terms and the four concepts, or ask for changes | S01 | 2026-11-06 |
| Challenge or confirm the defaults the AI took on the "go" of 2026-11-01: thresholds 80 and 40 percent, discount 15 percent, cap 30 percent, season read from the period of the report (at least twelve months), a price change not touching bookings already made | S01 | before task 3 |
| Sort the dictionary rows by PO term, in a separate change | S01 (AI as stand-in) | open |

---

[UC-007]: ../../use-cases/uc-007/uc.md
[DICT-001]: ../../dictionary.md
[DM-004]: ../../domain-model.md
[MIL-051]: ../../milestones/mil-051-room-price-suggestions.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[3a17eee]: https://github.com/NF-Hotel/NFHotel/commit/3a17eeed8127f7dab532e5afb78b09e005ec3118
