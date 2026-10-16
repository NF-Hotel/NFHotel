# MIL-009 Validation attributes on the entities

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-009 |
| CrossReference | [BC-001], [ADR-0002], [MIL-008] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-041 Go | [4cab5fc] |

---

## Purpose

Decide whether the field rules of `Room` and `Guest` can be expressed as
attributes on their properties (ADR-0002), so that the entities hold no
validation code, with no change of behaviour.

## Deliverable

- Attributes on the properties of `Room` and `Guest`, each carrying the existing error code.
- A helper in `NFHotel.Domain/Entities` that runs the attributes and a `DomainException` that carries all error codes.
- `Room.Validate`, `Room.IsValid…`, `Guest.Validate` and `Guest.IsValidEmail` deleted; the services and tests updated.
- Tests that fix the codes, the rules and the order of the errors.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `Room` and `Guest` have no method named `Validate` or `IsValid…`, and no private validation helper | Yes | No |
| 2 | Every field rule of the old methods is an attribute, and a test per rule asserts the same code as before | Yes | No |
| 3 | A test asserts that several invalid fields give every code, in the old order | Yes | No |
| 4 | `dotnet build` has 0 warnings, `dotnet format --verify-no-changes` is clean, and all tests pass | Yes | No |
| 5 | The `Domain` project references no NuGet package and no project | Yes | No |
| 6 | ADR-0002 is `Accepted` and a review record against `qc-programming-csharp` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-008 | The validation methods it removes are the ones moved into the entities there |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 12 of an amended BC-001: the entities hold no validation code | All tasks |
| Success criterion 15 | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-20 — two entities, about 170 lines moved into attributes.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case, Project Plan and accept ADR-0002 | Add objective 12, the scope item, success criterion 15 and the MIL-009 schedule row; review ADR-0002. | No | |
| 2 | Add the helper and the exception with all codes | The entity validation helper using `Validator.TryValidateObject`; `DomainException` carries the list of codes. | No | |
| 3 | Put attributes on `Guest` | `Required`, `StringLength`, `RegularExpression` with the existing codes; `Create` and `UpdateDetails` use the helper; delete `Guest.Validate`. | No | |
| 4 | Put attributes on `Room` | `Required`, `StringLength`, `Range`, `EnumDataType`; delete `Room.Validate` and `Room.IsValid…`. | No | |
| 5 | Update the services and the tests | The services build the entity and turn a `DomainException` into a failed `Result` with all codes; the tests assert codes, rules and order. | No | |
| 6 | Build, test and review | `dotnet build`, `dotnet format`, `dotnet test`, then a review against `qc-programming-csharp`. | No | |

---

[BC-001]: ../business-case.md
[ADR-0002]: ../adr/adr-0002-validation-attributes-on-entities.md
[MIL-008]: ./mil-008-domain-rules-in-entities.md
[4cab5fc]: https://github.com/NF-Hotel/NFHotel/commit/4cab5fc64b8358e18f9fec630d27d8bc267e6787
