# MIL-005 Clean architecture structure for NFHotel

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-005 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Initial version | pending |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-018 Go | pending |

---

## Purpose

Decide whether the NFHotel source follows the clean architecture structure of
the `clean-architecture` and `coding-conventions` skills: a common key base type
for every entity, the folders of each layer, repository interfaces in the
domain, key columns named `<table>_id`, and data transfer objects (DTOs) in one
folder per entity. The work goes layer by layer, domain first, with no change
to behaviour.

## Deliverable

- `Entity<TKey>` in `NFHotel.Domain` and every entity (`Booking`, `Guest`, `Room`) derived from it, in the folders of the skill.
- Repository interfaces in `NFHotel.Domain/Repositories`, implemented in `NFHotel.Infrastructure/Repositories` on a generic repository.
- One `EntityConfiguration<TEntity, TKey>` base in `NFHotel.Infrastructure/Persistence` that names every key column `<table>_id`.
- DTOs and requests in `NFHotel.Application/Dto/<Entity>/`, one type per file.
- Architecture tests that enforce these rules, an ADR that records the decisions, and an updated architecture note.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Every entity class derives from `Entity<TKey>`; an architecture test fails otherwise | Yes | No |
| 2 | `NFHotel.Domain` has the folders `Entities`, `ValueObjects`, `Repositories`, `Errors` (and `Rules`, as the ADR records) and references no project or framework package | Yes | No |
| 3 | The repository interfaces are in `NFHotel.Domain/Repositories` and no Domain type references a DTO | Yes | No |
| 4 | A model test shows that every key column of the EF model is named `<table>_id`, and `has-pending-model-changes` reports no new migration | Yes | No |
| 5 | Every DTO is in `NFHotel.Application/Dto/<Entity>/`, one top-level type per file | Yes | No |
| 6 | `dotnet build` has 0 warnings and all unit, architecture and integration tests (PostgreSQL in a container) pass, as before the work | Yes | No |
| 7 | The C# code has a `Go` review against `qc-programming-csharp` (criteria 13 to 18 included) | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| None | Independent of MIL-001 to MIL-004; no work on `src/` is open elsewhere |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 8 of an amended BC-001: the NFHotel source follows the clean architecture structure of the skills | All tasks |
| `docs/refactor/Fase1-Scope.md`, decisions 1 and 2 (all logic in the application layer, async throughout) | Tasks 4 and 5 keep them |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-10-30 — two weeks, one task at a time, because each step changes many files and must keep the tests green.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 8 (clean architecture structure), the scope items and the MIL-005 schedule row; review again. | No | |
| 2 | Record the structure decisions in an ADR | One ADR for: `Entity<TKey>` as an abstract base class with the property `Id` (the skill defines a base class, not an interface); the folders per layer, including `Rules`; repository interfaces split into entity repositories in the Domain and read-side query ports (which return DTOs) in the Application; DTO folder per entity; the key column rule. | No | |
| 3 | Domain: key base type and folders | Add `Entity<TKey>`, derive `Booking`, `Guest` and `Room` from it (the key `RoomId`, `GuestId`, `BookingId` becomes `Id`), and move files to `Entities`, `ValueObjects` (value types, enums, `DateRange`), `Errors` (domain exceptions) and `Rules`. Behaviour does not change. | No | |
| 4 | Domain: repository interfaces | Add a generic `IRepository<TEntity, TKey>` and the entity repository interfaces in `Domain/Repositories`; keep the DTO-returning read queries as narrow ports in `Application/Ports`, because the Domain must not reference DTOs. | No | |
| 5 | Infrastructure: repositories and folders | Implement the repositories on a generic `Repository<TEntity, TKey>` in `Infrastructure/Repositories`; place options and settings in `Infrastructure/Config`; keep `Persistence` for the context, configurations, converters and migrations. | No | |
| 6 | Persistence: key columns as `<table>_id` | Add `EntityConfiguration<TEntity, TKey>` that sets the table and names the key column `<table>_id`; derive the three configurations from it; add a model test over all key and foreign key columns; confirm that no new migration is needed. | No | |
| 7 | Application: DTOs per entity folder | Move every DTO and request to `Application/Dto/<Entity>/`, one type per file, and update the mappings; place `UseCases`, `Ports` and error types in the folders of the skill. | No | |
| 8 | Tests and architecture rules | Update the unit and integration tests for the new names and namespaces; extend `LayeringTests` with the rules of the Go criteria 1 to 5; run the whole suite including the PostgreSQL tests. | No | |
| 9 | Review code, update the architecture note | Review the C# against `qc-programming-csharp`, record the RC, and update the folder structure in `docs/refactor/Architecture.md` (the Danish note) with a short English pointer to the ADR. | No | |

---

[BC-001]: ../business-case.md
