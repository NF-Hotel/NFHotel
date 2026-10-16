# MIL-008 Domain rules live with the data they guard

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-008 |
| CrossReference | [BC-001], [MIL-005] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-038 Go | pending |

---

## Purpose

Decide whether the folder `src/NFHotel.Domain/Rules` can be dissolved, so that
`NFHotel.Domain` has only the folders of the `clean-architecture` skill
(`Entities`, `ValueObjects`, `Repositories`, `Errors`). The three static classes
`BookingRules`, `GuestRules` and `RoomRules` are the "rules in service classes"
shape that the skill warns against. The rules move to the data they guard, with
no change of behaviour. This closes the open question of RC-020.

## Deliverable

- The error codes of the rules in `NFHotel.Domain/Errors`, one static class per entity.
- The validation of a room and of a guest as static methods of `Room` and `Guest`; the overlap and blocking rules as members of `DateRange`, `BookingStatus` and `Booking`.
- `NFHotel.Domain/Rules` deleted, and every use in Application, Infrastructure and the tests updated.
- An architecture test that fails if a `Rules` namespace appears in the domain.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `src/NFHotel.Domain` has no `Rules` folder and no type whose name ends in `Rules` | Yes | No |
| 2 | `dotnet build` has 0 warnings and `dotnet test` passes with at least as many tests as before | Yes | No |
| 3 | The error code strings are unchanged (a test asserts the old and new values are equal) | Yes | No |
| 4 | The database exclusion constraint is generated from the same overlap definition, and the parity test passes | Yes | No |
| 5 | The architecture tests pass, and one new test fails when a `Rules` namespace exists in the domain | Yes | No |
| 6 | A review record against `qc-programming-csharp` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-005 | The clean architecture structure it completes |
| MIL-007 | The Domain Model (DM-004) is the reference for where each rule belongs |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 11 of an amended BC-001: the domain follows the folders of the skill | All tasks |
| Success criterion 11 (clean architecture), extended with criterion 14 | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-13 — about 340 lines of rules and 18 files that use them.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 11, the scope item, success criterion 14 and the MIL-008 schedule row; review again. | No | |
| 2 | Move the error codes | `Errors/BookingErrors`, `RoomErrors` and `GuestErrors` hold the code constants; `Application/Errors/ErrorCodes` points at them. Values stay identical. | No | |
| 3 | Move the room and guest validation | `Room.Validate` and `Guest.Validate` (static) replace `RoomRules` and `GuestRules`; the entities and the services call them. | No | |
| 4 | Move the booking rules | `BookingStatus.BlocksRoom()`, `Booking.ConflictsWith(roomId, period)` replace `BookingRules`; the infrastructure overlap constraint and the repository follow. | No | |
| 5 | Delete `Rules`, update tests and the architecture test | Update the five domain test files, remove the folder, add the architecture test, update ADR-0001. | No | |
| 6 | Build, test and review | `dotnet build` and `dotnet test`, then a review against `qc-programming-csharp` in an RC. | No | |

---

[BC-001]: ../business-case.md
[MIL-005]: ./mil-005-clean-architecture.md
