# Open Questions — Phase 0

> Collected from all five analysis agents. To be answered (or deliberately parked) before Phase 1 / Task 1b.
> Q-01 to Q-08 are **blocking** for the target architecture. The rest are noted so they are not lost.
> **Split by elective track: see `OpenQuestions-Tracks.md`** (DevOps, Cybersecurity, Mobile app, Frontend, AI Integration) — same Q-ids, plus Q-33 to Q-51 raised by the track split.

---

## Blocking — require your decision

**Q-01 — Must `Confirmed` be a precondition for check-in?**
Today a booking can be checked in directly from `Pending` (BR-06, confirmed by test T-21), while the confirmation dialog itself says "Payment has been received / Room is guaranteed" (`BookingOverviewViewModel.cs:499-508`). Either it is a deliberate walk-in rule, or it is a hole. The state machine cannot be locked without the answer.

**Q-02 — Which overlap rule is the right one?**
Creation excludes only `Cancelled` from the overlap check (BR-39, `NewBookingViewModel.cs:204-209`); editing excludes `Cancelled` AND `CheckedOut` (BR-19, `BookingOverviewViewModel.cs:768-775`). The same room and period can be rejected in one place and accepted in another. Should a `CheckedOut` booking release the room for the period or not?

**Q-03 — Where should the overlap check live, and should it be transactional?**
Today there is **no** overlap check in `CreateBooking()` or in `BookingRepo` — only in the list filtering beforehand. Double booking is possible. In new code the rule belongs in `Domain/Rules/BookingRules` + a uniqueness guard in the database; it must be decided whether a constraint/transaction is needed.

**Q-04 — Should `RoomType` with a price be included now?**
There is no price anywhere in the domain. `Room.RoomSize` is free text ('Single'/'Double'/'Suite' in data) without an enum, lookup table or CHECK constraint. Consequences: BR-31 (revenue) cannot be implemented, and "Sales overview" cannot be built meaningfully. This is questions 1 and 3 in section 7 of the plan.

**Q-05 — Which of the two old schemas is the reference? [downgraded 2026-09-02]**
There are two mutually incompatible SQL sets (`Database/` and `NFHotel/SQL/`) with the same file names and different content (U-15, U-16), and the actual column type of `ROOM.RoomNumber` and `BOOKING.StartDate/EndDate` depends on which patch scripts have been run (U-01, U-05).
**After the PostgreSQL decision this is no longer blocking:** the first migration is written from scratch against PostgreSQL, so the old scripts are a reference and not a source. What remains is the semantic question: is `RoomNumber` a string (answer: yes, the C# model and the patch script agree) and do the dates need a time of day (`date` vs. `timestamptz`)? The latter is tied to Q-22 and BR-118 (the half-day rule in the calendar).

**Q-05b — Should dates be `date` or `timestamptz`?**
`Booking.StartDate`/`EndDate` are used today as pure dates (BR-39 overlap check, BR-109 number of nights), while `CheckInTime`/`CheckOutTime` are real timestamps (BR-07, BR-10). In PostgreSQL `date` + `timestamptz` is the natural choice — but `timestamptz` requires a decision about time zone (the hotel runs in Europe/Copenhagen; `DateTime.Now` in the old code is local time without zone info).

**Q-06 — Test framework: MSTest or xUnit?** (section 7 of the plan, question 2)
Relevant context: the 22 tests in `BookingOverviewViewModelTests` are the only real source of truth and use Moq against `IBookingRepo`/`IRoomRepo` — they can be ported to either framework almost unchanged. The 40 repo tests cannot be ported as they are (see Q-07).

**Q-07 — What do we do with the 40 DB-dependent repo tests?**
They hit `HotelBooking` — **the same database as the application** — without transaction rollback, with `Parallelize(MethodLevel)` and hard-coded keys (`"999"`, `TEST101`). They are neither independent nor repeatable. Options: (a) rewrite to EF InMemory, (b) keep as integration tests against a dedicated PostgreSQL container, (c) drop them and cover the rules in the Application layer instead.
**Note after the PostgreSQL decision:** SQLite as a test stand-in becomes less attractive because it does not share the type system with PostgreSQL (jsonb, arrays, `timestamptz`). Testcontainers with `postgres:16` gives the tests the same database as production and is worth considering over InMemory — the plan's original "EF InMemory/SQLite" should be reconsidered in Phase 1b.

**Q-08 — ASP.NET Identity now or later?** (section 7 of the plan, question 4)
Note that today there is not a single access rule tied to a user — all BR rules of the "access" type are about UI state, not about who is logged in. There is no `Employee`/`User` concept at all.

---

## Security — must be handled regardless

**Q-09 — Azure SQL credentials in plain text.** `Database/README.md:18` contains a database password in plain text, contrary to the file's own security section (`:77-91`). Must be removed and rotated, see section 2 of the plan. Note also that `appsettings.json` is not in the repository (neither in the WPF project nor the test project), so it is unclear whether it was ever committed.

---

## Data model and schema

- **Q-10** — `Booking.BookingNumber` (`FLZ-{BookingID:D6}`) exists only in C#, not in SQL or diagrams (U-11). Should it be a persisted field in new code, or remain derived?
- **Q-11** — `Guest.PassportNumber` is nullable in SQL, not validated in C#, but always filled in test data (U-22, BR-57, T-14). Mandatory or optional in new code?
- **Q-12** — `Guest.Email` has no UNIQUE constraint. Should guests be deduplicated on e-mail? (Relates to BR-47: a guest with `GuestID == 0` is always created as new — there is no "does the guest already exist?" logic.)
- **Q-13** — Enum values have no enforcement in the database (U-18) — neither a CHECK constraint nor a lookup table. The EF Core configuration must decide: int, string, lookup table — or a **real PostgreSQL enum** (`CREATE TYPE booking_status AS ENUM (...)`), which Npgsql supports directly and which gives enforcement in the database without an extra table. Note that the ordinal values 0-4 exist in existing data, so an int mapping is the most backward compatible.
- **Q-14** — Casing inconsistency: C# `Room.RoomId` vs. `Booking.RoomID` vs. SQL `RoomID` (U-19). Choose one standard (Conventions.md says "Primary keys: `Id` or `[Entity]Id` — choose one standard"). **Sharpened by PostgreSQL:** unquoted identifiers are folded to lowercase, so `RoomID` becomes `roomid` unless everything is quoted. Recommendation: PascalCase in C#, snake_case in the database via `EFCore.NamingConventions` — one place, no quoting.
- **Q-15** — The room statuses in the room form XAML (Available / Maintanance / Cleaning / Disabled, with a typo) do not match the `RoomStatus` enum (Available / OutOfService / Maintenance) — BR-126. Which set is right? Are there "Cleaning"/"Disabled" values in the database?
- **Q-16** — `Database/03_InsertExtendedTestData.sql` (~99 bookings over 2025-2028) is not mentioned in any README and presupposes a particular run order. Should test data be ported to EF seed data, and if so which data set?

---

## Behaviour that looks like bugs (should the rule be kept or fixed?)

- **Q-17** — **Week and month views do not filter by period.** Only the year view does (BR-24, `:389-402`). In week/month ALL non-cancelled bookings are shown. Unintended?
- **Q-18** — **An edited guest loses its id.** `OnEditGuest()` copies six fields but not `GuestID` (BR-66), which gives `UpdateGuest` on id 0. Looks like a bug.
- **Q-19** — **`AddGuestToOverview` does not update the underlying list** — a newly created guest disappears at the next search.
- **Q-20** — **Changes to an existing guest are not saved when creating a new booking** (BR-47) — if you correct an existing guest's details in the booking form, they are lost.
- **Q-21** — **Search locks the view permanently to year** (BR-26) — it does not switch back when the search field is cleared.
- **Q-22** — **No upper time limit on check-in** (BR-06) — a booking whose `EndDate` has long passed can still be checked in.
- **Q-23** — **Enter in the guest search field triggers sorting, not search** (`GuestOverviewView.xaml.cs:68-77`). Name/behaviour mix-up?
- **Q-24** — **The dialog text "Cancel Booking Permanently From System?"** contradicts the actual soft delete (BR-13). Wording or behaviour to be fixed?
- **Q-25** — **CheckIn/CheckOut errors are invisible to the user** — only written to `Debug.WriteLine` (`:592`, `:639`), even though `Status` has already been changed in memory before `Update` is called. Inconsistent state on error.

---

## PostgreSQL-specific choices (added 2026-09-02)

- **Q-29** — Naming strategy: `EFCore.NamingConventions` with snake_case, or quoted PascalCase identifiers? (See Q-14. Recommendation: snake_case.)
- **Q-30** — Test database: Testcontainers with `postgres:16`, a fixed local container, or EF InMemory? (See Q-07.)
- **Q-31** — Hosting in Phase 2/the deployment diagram: local Docker is a given, but what is the target — managed PostgreSQL (Azure Database for PostgreSQL, Supabase, Neon) or nothing beyond local? Affects the component/deployment diagram.
- **Q-32** — Should we use PostgreSQL-specific features at all (`jsonb`, arrays, full-text search), or keep the schema portable? For a hobby/exam project simplicity argues for portable — but `jsonb` could be relevant if `Service`/add-ons (section 7 of the plan) arrive.

---

## Scope

- **Q-26** — `GuestPolicyViewModel` and `SalesOverviewViewModel` are **completely empty classes**, each with its own placeholder screen and menu item. Should the two features be built in the new project, or dropped? (Sales depends on Q-04.)
- **Q-27** — The "Payment Status" box in the booking details is hard-coded "Coming soon..." (`BookingOverviewView.xaml:599-602`). Should `Payment`/`Invoice` be included? (section 7 of the plan, question 1)
- **Q-28** — None of the 7 views has a state for "no data" apart from the booking details panel. Should empty states be included in the Blazor version?

---

## Noted, not blocking

- The documentation in `NFHotel/Documentation/Diagrams/` is partly wrong: the ERD lacks `Status` on both ROOM and BOOKING (U-08) and is syntactically incomplete (U-09); the sequence diagram uses a `Booking` constructor that does not exist (U-13) and a combined INSERT that does not exist (U-14). Section 9 of the plan already says: do not make new as-is diagrams — make to-be and compare.
- `DateGreaterThanAttribute` is used **nowhere** — neither on models nor in ViewModels. Can probably be dropped in new code (its rule is BR-88, covered by BR-103/BR-108).
- `RoomRepo` uses stored procedures while `BookingRepo`/`GuestRepo` use inline SQL, without explanation. The filter logic in `uspGetRoomsFromCriteria` is thus not verifiable from the C# code — it must be read from the SQL script before `GetRoomsFromCriteria` is reimplemented in EF.
- `GuestRepo.GetByID`/`GetAll` map via hard-coded column indexes 0-6 (`GuestRepo.cs:66-72`) — breaks silently if the SELECT order changes.
- Two positive tests appear to have been **deleted** from the test suite: there is no test proving that a booking can be deleted or a room updated successfully (`BookingRepoTests.cs:325-330`, `RoomRepoTests.cs:333-339`). The gap must not be confused with "the rule does not exist".
- `NFHotel_Tests/README.md` cannot be used as an authoritative source: wrong test count, wrong package versions, refers to four documents that do not exist, and is partly a pasted chat conversation.
- `Microsoft.Extensions.Configuration*` is not referenced in the test project's `.csproj` — it builds only via a transitive dependency through the project reference to the WPF project.
