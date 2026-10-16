# RC-036 Review of DM-004 (project Domain Model)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-036 |
| CrossReference | [DM-004], [QC-DM-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [ec58105] |

---

## Artifact Under Review

- Instance reviewed: [DM-004]
- Checklist used: [QC-DM-001]; also [QC-LANG-001] because the type is written in the PO language
- Scope: full review
- Language and domain: en / it
- Language reviewer: none; the user's chat confirmation closes criterion 9
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Business language throughout; no technical jargon | Pass | PO terms of the dictionary; no tables, classes or keys |
| 2 | Multiplicities are correct and complete | Pass | Guest 1 to Booking 0..*; Booking 0..* to Room 1 |
| 3 | No operations or method signatures | Pass | No operations in the diagram |
| 4 | Associations are named with a reading direction | Pass | `makes >`, `reserves >` |
| 5 | Generalization is used correctly | Pass | None used |
| 6 | Every concept traces to a noun phrase in a use case or the glossary | Pass | Source column; the dictionary has a row for every concept and attribute |
| 7 | Attributes are simple domain data, no key-like references | Pass | No key attributes |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | `en` and `it` present |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | `en`; `it` is in the registry list |
| 3 | The content (prose and table cells) is written in the stated language | Pass | Whole document is English; checked by the stand-in, not by a person |
| 4 | The register matches the one the registry gives for the artifact type | Pass | Registry register for this type is used |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | The terms follow the Domain Dictionary of MIL-007 once it exists; until then the terms of the legacy ledger are used consistently |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | Template headings kept |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | No twin file in `docs/` |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | Pass | First version, no change |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The user (Team 2, S01, owner of NF Hotel) confirmed in chat on 2026-10-16 that the terminology is correct (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (API, TLS, CI) are abbreviated |

## Overall Verdict

Go — every Mandatory criterion of QC-DM-001 passes. Criterion 9 of QC-LANG-001 rests on the user's chat confirmation of 2026-10-16 (AI as stand-in for S01).

### Comparison with src/NFHotel.Domain

Each difference between the model and the C# entities is classified. The code is not changed by this milestone.

| # | Difference | Class | Note |
| --- | --- | --- | --- |
| 1 | The code has `RoomId` and `GuestId` on Booking; the model has associations | Intended design difference | Keys are how the code stores the associations; the model must not show them (QC-DM-001 criterion 7) |
| 2 | The code has a `DateRange` value object and `EffectivePeriod`; the model has arrival date, departure date and effective end date | Intended design difference | The period is a design grouping of the same attributes |
| 3 | The code has `Id` on every entity through `Entity<TKey>` | Intended design difference | Identity is design, not a business attribute |
| 4 | The code has the static classes `BookingRules`, `GuestRules`, `RoomRules` and the exceptions | Intended design difference | Operations are outside a Domain Model |
| 5 | The status values are enums in the code and attribute values in the model | Intended design difference | Same values: 5 booking statuses, 3 bookability values, 6 cleaning states, 3 sizes |
| 6 | Every attribute of the model exists in the code under its IT term | None | Checked against the dictionary: `BookingNumber`, `NumberOfNights`, `EffectiveEndDate` and the other 21 terms |
| 7 | A unique room number is enforced in the code (`RoomNumberExistsAsync`, unique index) and is stated in UC-002 only as a new rule | Rule to confirm | S01 is to confirm it, see UC-002 open issues |
| 8 | Deleting a guest or a room with bookings is refused in the code (`HasBookings`) | None | Matches UC-002 and UC-003 |

No model error and no code error was found.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Confirm the new rules of the code (unique room number, guest delete guard) | S01 | open |
| Move the legacy diagram `docs/refactor/diagrams/Domain-Model.puml` out of use once this model is accepted | S01 | MIL-007 task 7 |

---

[DM-004]: ../../domain-model.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[ec58105]: https://github.com/NF-Hotel/NFHotel/commit/ec581054576af2286ced648fbf6c605d6eddb26e
