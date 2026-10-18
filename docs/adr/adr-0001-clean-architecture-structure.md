# ADR-0001 Clean architecture structure of NFHotel

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0001 |
| CrossReference | [BC-001], [MIL-005] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Initial version, accepted after RC-019 Go | [78eeb18] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Rules folder dissolved (MIL-008, RC-039) | [b274cc5] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The NFHotel source is layered (`Domain`, `Application`, `Infrastructure`, `Web`) but
differs from the structure in the `clean-architecture` and `coding-conventions`
skills:

- each entity declares its own key (`RoomId`, `GuestId`, `BookingId`), so there is
  no common key contract;
- `Domain` and `Application` are grouped by feature (`Rooms/`, `Bookings/`,
  `Guests/`, `Common/`) instead of by kind (`Entities`, `ValueObjects`,
  `Repositories`, `Errors`; `UseCases`, `Dto`, `Ports`);
- the repository interfaces are in `Application` and return DTOs (projections),
  so they cannot move into `Domain`, which must not reference DTOs;
- DTOs and requests sit several to a file (`Dtos.cs`, `Requests.cs`);
- key columns are named `room_id`, `guest_id` and `booking_id` only because the
  snake-case naming convention derives them from the property names; renaming the
  key property to `Id` would silently change them to `id`.

Options considered:

1. **Follow the skills literally.** `Entity<TKey>` as an abstract base class with
   `Id`; the folders of the skill; a generic repository contract in `Domain`;
   narrow DTO-returning query ports in `Application/Ports`; a configuration base
   class that names the key column `<table>_id`; DTOs in `Dto/<Entity>/`.
2. **Keep the feature folders and per-entity keys** and only add rules to the
   architecture tests. Least change, but the structure stays unlike the skills.
3. **A key interface `IEntity<TKey>` only**, without a base class. The skill
   defines a base class (`Entity<TKey>`), so an interface alone would duplicate
   the `Id` property on every entity.

The repository split is the one real design choice: a single repository
interface that both loads entities and returns DTOs has to live in `Application`;
splitting it lets the entity part live in `Domain` as the skill requires.

## Decision

We follow option 1. The key is `Entity<TKey>`, an abstract base class with the
property `Id`, and `Booking`, `Guest` and `Room` derive from it with `int` keys.
The layer folders are those of the skill only; the static rule classes of the first
version (`Domain/Rules/BookingRules` in `docs/refactor/Phase1-Scope.md`) were moved into the entities, value objects and `Errors` by MIL-008.
Entity repositories (`IRepository<TEntity, TKey>`, `IBookingRepository`,
`IGuestRepository`, `IRoomRepository`) are in `Domain/Repositories`; the
DTO-returning reads are narrow ports (`IBookingQueries`, `IGuestQueries`,
`IRoomQueries`) in `Application/Ports`; one infrastructure class per entity may
implement both. `EntityConfiguration<TEntity, TKey>` sets the table and names the
key column `<table>_id`; a model test enforces it. DTOs and requests are in
`Application/Dto/<Entity>/`, one type per file.

## Consequences

**Positive:**

- Every entity has the same key contract; generic code (a repository base, a
  configuration base) works for all.
- The folder layout is the same as in the skills and in any other project that
  follows them, so a reviewer finds each kind of type in one place.
- The key column name no longer depends on a property name; the rule is tested.
- The `Domain` project references nothing and no DTO; the architecture tests can
  enforce this.

**Negative:**

- Many files change namespace and folder, and `RoomId`, `GuestId` and `BookingId`
  become `Id` on the entities, so the diff is large and the tests are updated.
- Two interfaces per entity (the repository and the query port) instead of one.
- Moving type files into kind folders loses the feature grouping that
  `docs/refactor/Architecture.md` describes; that note is updated.

## Affected Artifacts

- [BC-001] — objective 8 and the success criterion 11 rest on this decision.
- [MIL-005] — the tasks 3 to 8 implement it.

---

[BC-001]: ../business-case.md
[MIL-005]: ../milestones/mil-005-clean-architecture.md
[78eeb18]: https://github.com/NF-Hotel/NFHotel/commit/78eeb18cac8d761ebf5267fca5d6b418b04b98c2
[b274cc5]: https://github.com/NF-Hotel/NFHotel/commit/b274cc599b848bc07160cfe465d01ea61ad8c8b4
