# Architecture.md — NFHotel 2.0

> Phase 1 (Task 1b). Prepared 2026-09-07 by three agents (`design:domain`, `design:layers`, `design:diagrams`) and consolidated here.
> **This document takes precedence.** The appendices `Architecture-Domain.md`, `Architecture-Layers.md` and `Architecture-Diagrams.md` are the agents' full output and apply only where they do not contradict this document.
>
> Status: **ready for review.** Phase 2 does not start until Valdemar approves.

---

## 1. What is being built

A staff back office on the web, ported from WPF, on top of Clean Architecture with EF Core and PostgreSQL.

**24 use cases** spread over three services: `IBookingService` (9), `IRoomService` (9 → see A-07), `IGuestService` (6).

Not in this round: the API project as code, Flutter app, customer site, auth implementation, roles, prices, payment. Cf. `Phase1-Scope.md`.

---

## 2. Project structure

> **Updated 2026-10-16 (ADR-0001):** the folders now follow the structure of the skills (Entities, ValueObjects, Repositories, Errors, Rules in Domain; UseCases, Dto, Ports, Errors in Application). See `docs/adr/adr-0001-clean-architecture-structure.md`.

```
NFHotel.sln
├── src/
│   ├── NFHotel.Domain/          # zero project references, zero NuGet packages
│   │   ├── Entities/                  Entity<TKey>, Booking, Guest, Room
│   │   ├── ValueObjects/              DateRange, BookingStatus, RoomStatus, HousekeepingStatus, RoomSize
│   │   ├── Repositories/              IRepository<TEntity,TKey>, IBookingRepository, IGuestRepository, IRoomRepository
│   │   ├── Errors/                    DomainException, InvalidStateTransitionException
│   │   └── Rules/                     BookingRules, GuestRules, RoomRules
│   ├── NFHotel.Application/     # must not reference EF Core, Npgsql, ASP.NET
│   │   ├── UseCases/                  Result, services and mapping per entity (Bookings/, Rooms/, Guests/)
│   │   ├── Dto/                       DTOs and requests, one folder per entity, one type per file
│   │   ├── Ports/                     IClock, IUnitOfWork, IHolidayCalendarClient, IBookingQueries, IRoomQueries, IGuestQueries
│   │   └── Errors/                    ErrorCodes and application errors
│   ├── NFHotel.Infrastructure/  # EF Core and Npgsql live ONLY here
│   │   ├── Persistence/               HotelDbContext, Configurations/ (EntityConfiguration: <table>_id), Migrations/
│   │   ├── Repositories/              Repository<TEntity,TKey> and the three repositories
│   │   ├── Config/                    options for encryption, time and the analysis service
│   │   ├── Security/                  AesGcmStringEncryptor
│   │   └── Time/                      SystemClock
│   └── NFHotel.Web/             # Blazor Server, feature-first, composition root
└── tests/
    ├── NFHotel.Domain.Tests/
    ├── NFHotel.Application.Tests/       # + architecture test
    └── NFHotel.Infrastructure.Tests/    # Testcontainers, postgres:16

# Part of the target architecture, NOT built in Phase 1:
#   src/NFHotel.Api/       REST over Application (mobile guild)
#   src/.../Web/Customer/        customer site (frontend guild)
#   mobile/nfhotel_app/    Flutter (mobile guild)
```

The direction of dependencies is `Web → Application → Domain` and `Web → Infrastructure → Application → Domain`. Domain has **out-degree zero**. That is enforced by an architecture test, not by discipline.

---

## 3. Decided premises (B)

| Id | Decision | Reason |
|---|---|---|
| **B-01** | Walk-in allowed: `Pending → CheckedIn` bypassing `Confirmed`. `Confirmed` means "payment guaranteed", not "may check in" | Preserves BR-06 and test T-21. Payment gating belongs to `Payment`, not to check-in |
| **B-02** | *(revised, see A-01)* A booking blocks the room from `StartDate` to its **effective end date**, unless it is `Cancelled` | Resolves the contradiction BR-19 ↔ BR-39 without blocking re-letting after an early check-out |
| **B-03** | Overlap is a pure function in Domain **plus** a PostgreSQL exclusion constraint | The old system had no overlap check on creation. The constraint makes double booking impossible under concurrency |
| **B-04** | `RoomStatus` (bookability) and `HousekeepingStatus` (cleaning process) are two separate enums | The old enum mixed the two and disagreed with its own XAML dropdown (BR-126, U-15) |
| **B-05** | Async all the way down. No `.Result`, no `.Wait()` | Conventions.md. The old system was 100 % synchronous |
| **B-06** | snake_case in the database via `EFCore.NamingConventions`; PascalCase in C# | Resolves U-19 and PostgreSQL's lowercase folding |
| **B-07** | Enums are stored as int. `BookingStatus` 0-4 is kept | The ordinal values already exist in data |
| **B-08** | `DateOnly` → `date`, `DateTimeOffset` → `timestamptz` stored UTC | The booking period is dates, not timestamps. Removes a class of errors |
| **B-09** | Passport number is encrypted at rest, never indexed | D-04. None of the 126 rules search on passport number, so it costs no functionality |
| **B-10** | All business logic in Application services or Domain — never in ViewModels | The only decision that really matters. 823 lines of logic in one ViewModel was the old system's core flaw |

---

## 4. Rulings (A) — contradictions between the three designs

The diagram agent found 12 discrepancies by drawing the domain and layer designs in the same figure. Each is settled here. **The appendices are wrong where they contradict this section.**

### A-01 — The overlap predicate *(most important)*

`design:layers` showed that my original B-02 had a concrete operational flaw: if a `CheckedOut` booking blocks its whole booked period, a room cannot be re-let after an early check-out. A booking of 1–10 January, the guest leaves on the 3rd, a new booking of 4–8 January would be rejected by the database even though the room is empty.

**Settled — effective end date, without touching `EndDate`:**

```
EffectiveEndDate = GREATEST(StartDate + 1 day, COALESCE(CheckOutDate, EndDate))
A booking blocks the room in [StartDate, EffectiveEndDate) unless Status = Cancelled.
```

The booked period is kept as history; only the effective occupancy controls overlap. Early check-out automatically releases the remaining nights. `GREATEST(StartDate + 1, ...)` ensures that a same-day check-out does not give an empty period.

Consequences: `Booking.EffectiveEndDate` and `Booking.EffectivePeriod` are added; `BookingRules.Conflicts` uses `EffectivePeriod`, not `Period`; the repository's coarse filter filters on `effective_end_date`.

### A-02 — `check_out_date` is an independent column

The conversion `timestamptz → hotel-local date` is `STABLE`, not `IMMUTABLE`, and can therefore be part of neither a generated column nor an exclusion constraint. `check_out_date` is set explicitly by `BookingService.CheckOutAsync`. `effective_end_date` is a `GENERATED ALWAYS ... STORED` column computed from `start_date`, `end_date` and `check_out_date`.

### A-03 — `CheckOut` gets a date parameter

`Booking.CheckOut(DateTimeOffset occurredAt, DateOnly checkOutDate)`. Domain must not know time zones, so the hotel-local date is supplied from outside — in parallel with `CheckIn` already getting both `occurredAt` and `today`.

### A-04 — One place for the overlap definition

`BookingRules` owns the definition. Infrastructure generates the constraint predicate from it, and a test in `Infrastructure.Tests` asserts that the C# function and the SQL constraint agree on a set of known cases. Without that test they drift apart.

### A-05 — `IClock` gets `Today`

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }   // the hotel's time zone, from HotelTimeOptions
}
```

Without `Today` each service computes its own "today", and BR-06/BR-44/BR-104 are back to the old system's problem.

### A-06 — Names are consolidated

`Room.HousekeepingStatus` (not `Room.Housekeeping`). `CanReschedule` everywhere (not `CanEdit` in the DTO). One rule, one name.

### A-07 — Cleaning gets explicit methods instead of a target status

`ChangeHousekeepingStatusAsync(newStatus)` is dropped. Instead one service method per domain transition: `StartCleaningAsync`, `CompleteCleaningAsync`, `ReportServiceNeededAsync`, `StartServiceAsync`, `CompleteServiceAsync`, `MarkDailyCleaningDueAsync`, `MarkDepartureCleaningDueAsync`.

A generic target status would force the service to translate (current, desired) → method call via a table, i.e. reimplement the state machine one layer higher. `IRoomService` thus gets 15 use cases, not 9.

### A-08 — Status transitions are idempotent for the same target state

`TakeOutOfService()` on a room that is already out of service is a no-op, not an exception. Illegal transitions still throw.

Reason: the mobile app has no offline support (D-11), so it retries over an unstable network. A retry must not give an error to the cleaning staff. Consequence: `Status` is removed from `UpdateRoomRequest` — a status change has its own use case.

### A-09 — Validation returns error codes, not English strings

`GuestRules.Validate` and `RoomRules.Validate` return codes (`ErrorCodes.Guest.FirstNameRequired`). Application maps code → text; the UI can translate.

It breaks textual parity with BR-52..BR-56 and BR-60, but the rules are about *what is required*, not about the English wording — and the system must be operated in Danish. Phase 3's audit measures the rule, not the string.

### A-10 — Length limits are shared between the layers

Domain owns the constants (`Guest.MaxNameLength` etc.) and guards on them; the EF configuration mirrors them. Otherwise too-long values fail as `DbUpdateException` instead of as a domain error. Registered as a new rule, see section 6.

### A-11 — Repositories return projections for reading

List and detail queries return DTOs directly from the repository (projection in SQL). Entities are fetched only on command paths.

That removes the trap where `ToListItem()` is called on a booking fetched without `Include` and silently gives empty fields — and it is faster.

### A-12 — Roles are documentation in Phase 1

The role column in the `HousekeepingStatus` table (cleaning staff, service technician, owner) describes *who will be allowed* when authorisation is built. In Phase 1 anyone can call any transition. **It must be stated explicitly**, otherwise Phase 3's audit counts them as implemented rules.

### A-13 — Feature-first wins over the plan's drawing

`Conventions.md` requires feature-first; section 3 of the plan drew `Interfaces/`, `Services/`, `DTOs/`. Conventions is the source of truth. **Section 3 of the plan must be corrected**, otherwise two documents contradict each other from day one.

### A-14 — DTOs and mapping live in Application

`Conventions.md` places Entity↔DTO mapping in Infrastructure. That cannot be reconciled with DTOs having to live in Application so a later API can reuse them — the Application service would not be able to reach the mapper without reversing the direction of dependencies. Infrastructure's mapping responsibility is entity↔database. **`Conventions.md` should be corrected on this point.**

### A-15 — Primary keys

`BookingId`, `RoomId`, `GuestId` in C# → `booking_id`, `room_id`, `guest_id` in the database. Resolves U-19. Must be fixed **before** the first migration.

---

## 5. Data model

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE booking
  ADD COLUMN effective_end_date date
  GENERATED ALWAYS AS (GREATEST(start_date + 1, COALESCE(check_out_date, end_date))) STORED;

ALTER TABLE booking
  ADD CONSTRAINT ex_booking_room_period
  EXCLUDE USING gist (
      room_id WITH =,
      daterange(start_date, effective_end_date, '[)') WITH &&
  )
  WHERE (status <> 4);
```

`'[)'` is half-open, so departure and arrival on the same day are not an overlap (BR-39). `status <> 4` = everything except `Cancelled` blocks.

**Check constraints:** `status BETWEEN 0 AND 4`, `end_date > start_date` (BR-103), `floor > 0` (BR-98), `capacity > 0` (BR-100).

**Indexes:** `ix_booking_room_id`, `ix_booking_guest_id`, `ix_booking_status_start_date` (BR-23 + BR-24), the GiST index from the exclusion constraint, `ux_room_room_number` *(new rule)*, `ix_room_status`, `ix_guest_last_name_first_name` (BR-68, BR-69). **No index on `passport_number`** — AES-GCM is randomised, so even an equality lookup would not work.

`xmin` is used as the optimistic concurrency token on all three tables.

---

## 6. New rules that require your approval

These do **not** exist in the 126 ported rules. Phase 3's ledger audit will book them as "invented" if they are not approved explicitly. They are carried in the ledger as `BR-N-01` onwards.

| Id | Rule | Reason |
|---|---|---|
| BR-N-01 | Room number must be unique | Two rooms with the same number is meaningless. The old schema had no constraint |
| BR-N-02 | A guest can be deleted | Follows from D-05 (personal data is deleted on request). Did not exist before |
| BR-N-03 | Length limits are enforced in Domain, not only in the database | A-10 |
| BR-N-04 | `floor > 0` and `capacity > 0` as database constraints | The rules existed in C# (BR-98, BR-100), but not in the schema |
| BR-N-05 | Guest search uses BR-68's semantics everywhere | The repo searched only on first/last name (BR-116); the ViewModel on all words in name, country and e-mail (BR-68). Consolidated to BR-68 — a search on a last name now also hits a country with the same name |
| BR-N-06 | The cleaning process's seven transitions | Room status did not really exist as a rule set before (only BR-81 and BR-126) |

---

## 7. Open points I have not settled

None of them blocks Phase 2.

1. **`Floor > 0` forbids a ground floor.** The rule is preserved unchanged from BR-98, but a hotel with room 001 could not be created. A deliberate rule or an overlooked off-by-one?
2. **There is no undo of check-in/check-out.** No BR asks for it, so I have not invented it — but staff check in the wrong bookings in practice, and `CheckedOut` is an absolute final status.
3. **`Guest.Anonymize()` is not designed.** D-05's retention probably requires it, but the X values (months/years) are not fixed, and it collides with the invariant "FirstName must not be empty" (BR-91).
4. **`RoomSize` migration.** The enum conversion requires a strategy for unknown text values in existing data — fail or fall back to `Single`.
5. **`SalesService` is left out.** BR-31 always gives 0, and there is no price in the domain. The sales screen should either be dropped from Phase 1 or shown as an explicit placeholder rather than a service that returns 0 and looks like an implementation.
6. **Encryption key and rotation.** Phase 1 can deliver `EncryptionOptions` with `ValidateOnStart`, so the app refuses to start without a key. Rotation requires a backfill migration over the whole `guest` table — not designed.
7. **Data migration from the old system is not assumed.** If production data must be moved, a one-off task arrives: passport numbers must be encrypted on load, and existing bookings must be able to satisfy the exclusion constraint. **They can overlap today, because there has never been a check.** Should be verified against the running database before the migration is written.

---

## 8. Requirements for Phase 2

- **Blazor Server must give each page its own DI scope.** If it does not, a user gets one `DbContext` for their whole session, and two simultaneous component events throw. Must be agreed, not discovered in operation.
- **The architecture test is written first.** `Application` must not reference `Microsoft.EntityFrameworkCore` or `Npgsql`. The test enforces it; discipline does not.
- **The 22 tests in `BookingOverviewViewModelTests` are ported first.** The state machine is now pure, so they can run directly against the `Booking` methods without mocks. T-21 (`Pending → CheckedIn`) must pass unchanged.
- **The constraint parity test** from A-04 is written together with the first migration.
- **BR-118 ("occupied from noon") is a business assumption disguised as pixel maths.** The geometry belongs in Web, but if the hotel has an actual check-in time, it is a domain policy that should also apply to the check-in window. To be clarified when the calendar is built.

---

## 9. Order for Phase 2

The five files that must be written first:

1. `Application/Common/Results/Result.cs` — everything else depends on the return type
2. `Application/Bookings/IBookingRepository.cs` — the contract both service and repository are built against
3. `Application/Bookings/BookingService.cs` — carries the most rules; what Phase 3 measures against
4. `Infrastructure/Persistence/Configurations/BookingConfiguration.cs` — B-03, B-06, B-07 and B-08 meet here, and the migration is generated from here
5. `Infrastructure/DependencyInjection.cs` — lifetimes, options validation, `DbContext` factory

Then the agent split from section 5 of the plan: Domain → (Application ‖ Infrastructure) → four Web features in parallel.

> The paths in this section are the original plan. Since ADR-0001 the repository interface is `Domain/Repositories/IBookingRepository.cs`, `Result` is `Application/UseCases/Result.cs` and the service is `Application/UseCases/Bookings/BookingService.cs`.
