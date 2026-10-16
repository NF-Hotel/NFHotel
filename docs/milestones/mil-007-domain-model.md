# MIL-007 Domain Model of the hotel back office

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-007 |
| CrossReference | [BC-001], [US-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-025 Go | pending |

---

## Purpose

Decide whether the business concepts of the hotel back office (bookings, rooms,
guests and their rules) are captured as a Domain Model that follows the
framework rules (`QC-DM-001`), so that the design class diagram, the database
design and the code can be checked against it. Today the project has only
legacy pictures of the domain in `docs/refactor/diagrams/` and an unreviewed
`docs/dm.md`; neither is a framework artifact, and neither traces its
concepts to a use case or a glossary.

## Deliverable

- A Domain Dictionary (`docs/dictionary.md`) with the PO and IT terms of the back office.
- Brief use cases `UC-001` to `UC-003` (bookings, rooms, guests) in `docs/use-cases/uc-NNN/uc.md`, each with a `dm.md` for the concepts it introduces.
- A consolidated project Domain Model `docs/domain-model.md`: concepts, attributes and named associations with multiplicities; no operations, no foreign-key attributes, no implementation terms.
- A traceability table from every concept to the use case or dictionary term it comes from.
- The legacy diagrams in `docs/refactor/` marked as legacy, with a pointer to the Domain Model.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `docs/domain-model.md` is `Accepted` and its review record against `QC-DM-001` has the verdict `Go`, with every Mandatory criterion passing | Yes | No |
| 2 | Every concept in the model traces to a noun phrase in a use case or to a dictionary term (criterion 6 of `QC-DM-001`) | Yes | No |
| 3 | No concept lists an operation or a key such as `RoomId` as an attribute | Yes | No |
| 4 | Every association has a name, a reading direction and multiplicities at both ends | Yes | No |
| 5 | Each concept of the model is compared with the C# entities in `src/NFHotel.Domain`; every difference is listed in the review record as a model error, a code error or an intended design difference | Yes | No |
| 6 | The three use cases and the dictionary have review records with the verdict `Go` | Yes | No |
| 7 | The legacy diagrams state that they are not the Domain Model, and no document cites them as such | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-006 | The legacy notes (`BusinessRules.md`, `Analysis.md`) are the source of the terms and are now in English |
| MIL-005 | The entities in `src/NFHotel.Domain` are compared with the model |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 10 of an amended BC-001: a framework Domain Model of the hotel back office | All tasks |
| New success criterion 13: the Domain Model passes `QC-DM-001` | Criterion 1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-06 — one dictionary, three brief use cases and one model, each reviewed.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 10, the scope item, success criterion 13 and the MIL-007 schedule row; review again. | No | |
| 2 | Write the Domain Dictionary | Collect the PO and IT terms of bookings, rooms and guests from `docs/refactor/BusinessRules.md`, `Analysis.md` and the code, with one definition per term and no synonyms; review against `QC-DICT-001`. | No | |
| 3 | Write the brief use cases | `UC-001` manage bookings (create, confirm, check in, check out, cancel, reschedule), `UC-002` manage rooms (create, change, status, housekeeping, delete) and `UC-003` manage guests (search, create, change, delete), each in its own folder; derived from the rule ledger and marked for S01 to confirm. | Yes | UC-001, UC-002, UC-003 |
| 4 | Write a domain model per use case | One `dm.md` in each use-case folder with the concepts the use case introduces, as the `artifact` skill orders. | Yes | UC-001, UC-002, UC-003 |
| 5 | Consolidate the project Domain Model | Create `docs/domain-model.md` from the three scoped models, keeping names identical, and review it against `QC-DM-001`. | No | |
| 6 | Compare the model with the code | List every difference between the model and `src/NFHotel.Domain` in the review record; propose, do not make, any code change. | No | |
| 7 | Mark the legacy diagrams and review | Add a note to `docs/refactor/Architecture-Diagrams.md` and to the diagram sources that they are legacy pictures; update the traceability matrix; record the reviews. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
