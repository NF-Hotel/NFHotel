# NFHotel — Analysis (Phase 0)

> Generated 2026-09-02 by 5 parallel read-only analysis agents.
> Source: `4. Semester\NFHotel-Hotel-main\` (unchanged).
> No code has been written. No files in the source project have been changed.

---

## Summary

- **90 business rules** were found in the ViewModel layer alone; the ledger in `BusinessRules.md` counts **126** when model validation, converter rules, repo rules and test-proven rules are added.
- **All business logic lives in `BookingOverviewViewModel.cs`** (BR-02 to BR-33) — state transitions, overlap check, filtering, sorting and calendar calculation in one class.
- **Two different overlap rules exist side by side:** on creation only `Cancelled` is excluded from the overlap check, on editing both `Cancelled` and `CheckedOut` are excluded. The same room/period can be rejected in one place and accepted in another.
- **There is no overlap check on the creation itself** (`CreateBooking`) — only in the list filtering beforehand. Double booking is possible if dates are changed after the room is chosen.
- **Revenue is never calculated.** `UpdateRevenue()` always sets 0. There is no price anywhere in the domain — `Room` has `RoomSize` as free text, not a price.
- **Check-in is allowed directly from `Pending`**, which contradicts the confirmation dialog's own text ("Payment has been received"). Confirmed by test T-21. Must be clarified before the state machine is locked.
- **Week and month views do not filter by period** — only the year view does. Looks unintended.
- **`RoomNumber` is `string` in C# but `INT` in both create scripts**; only a loose patch script (`03_AlterRoomNumberToString.sql`, which exists in only one of the two SQL sets) fixes it.
- **There are two mutually incompatible SQL sets** (`Database/` and `NFHotel/SQL/`) with the same file names and completely different content. Running one after the other deletes data.
- **No DI:** `DatabaseConfig` is a static mutable global; all three repos read it in the constructor. Everything is synchronous — zero `async` in the whole data access layer.
- **RoomRepo uses stored procedures, BookingRepo and GuestRepo use inline SQL** without explanation. `GuestRepo` maps via hard-coded column indexes 0-6.
- **Business rules have leaked down into the repo layer** (e.g. "rooms with bookings must not be deleted" as a `catch` on SQL error 547) and up into the view layer (status buttons controlled by XAML triggers).
- **`GuestPolicyViewModel` and `SalesOverviewViewModel` are completely empty classes** — the two menu items lead to placeholder screens.
- **128 tests, not 129 as the README claims.** 40 hit a shared live SQL Server (`HotelBooking` — the same DB as the app), and `appsettings.json` is missing from the repo, so the suite is 0 % runnable out of the box.
- **The 22 tests in `BookingOverviewViewModelTests` are the only real source of truth** — they encode the booking's state machine with mocks and can be reused directly. Two positive tests (delete booking / update room) appear to have been removed.

---

## 1. analyse:models

### Entities

#### Booking

Sources: `NFHotel/Models/Booking.cs`, `Database/01_CreateSchema.sql:43-54`, `NFHotel/SQL/01_CreateSchema.sql:57-68`, `NFHotel/SQL/fix-booking-identity.sql:12-23`

| Field | C# type | SQL type | In diagram? | Remark |
|---|---|---|---|---|
| BookingID | `int` (`Booking.cs:10`) | `INT PRIMARY KEY IDENTITY(1,1)` | Yes (DCD_Domain, DCD_Architecture, ERD as 🔑) | PK. Identity was originally missing in the running DB — see `fix-booking-identity.sql` |
| BookingNumber | `string` read-only, `$"FLZ-{BookingID:D6}"` (`Booking.cs:13`) | — (no column) | No | Purely derived display field; exists neither in SQL nor in a diagram |
| StartDate | `DateTime` (`Booking.cs:15`) | `DATE NOT NULL` in both 01 scripts; `DATETIME2 NOT NULL` in `fix-booking-identity.sql:14` | Yes (all 3 diagrams) | The type depends on which script was run last |
| EndDate | `DateTime` (`Booking.cs:16`) | `DATE NOT NULL` / `DATETIME2 NOT NULL` (the fix script) | Yes | Same type conflict as StartDate |
| CheckInTime | `DateTime?` (`Booking.cs:17`) | `DATETIME2 NULL` | Yes | Consistent |
| CheckOutTime | `DateTime?` (`Booking.cs:18`) | `DATETIME2 NULL` | Yes | Consistent |
| Status | `BookingStatus` (enum) (`Booking.cs:19`) | `INT NOT NULL DEFAULT 0` (in the fix script: `INT NOT NULL` without default) | Yes in DCD_Domain/DCD_Architecture, **no** in ERD_DB | Stored as ordinal value; no CHECK constraint |
| RoomID | `int` (`Booking.cs:22`) | `INT NOT NULL`, FK → `ROOM(RoomID)` | Only in ERD_DB (🗝️); not as a field in the DCDs | The DCDs show the relation as an association, not as an FK field |
| GuestID | `int` (`Booking.cs:23`) | `INT NOT NULL`, FK → `GUEST(GuestID)` | Only in ERD_DB (🗝️) | Same as above |
| Room | `Room` (navigation) (`Booking.cs:25`) | — | Association `Booking "0..*" --> "1" Room` | Object reference, no column |
| Guest | `Guest` (navigation) (`Booking.cs:26`) | — | Association `Booking "0..*" --> "1" Guest` | Object reference, no column |
| NumberOfNights | `int` calculated (`Booking.cs:30-39`) | — | Yes (DCD_Domain + DCD_Architecture) | `(EndDate - StartDate).Days`, 0 if a date is `default` |

Indexes on BOOKING (both 01 scripts): `IX_Booking_StartDate`, `IX_Booking_EndDate`, `IX_Booking_Status`, `IX_Booking_RoomID`, `IX_Booking_GuestID`. These disappear if `fix-booking-identity.sql` is run (the table is dropped and recreated without indexes and without named FK constraints).

#### Guest

Sources: `NFHotel/Models/Guest.cs`, `Database/01_CreateSchema.sql:29-37`, `NFHotel/SQL/01_CreateSchema.sql:43-51`

| Field | C# type | SQL type | In diagram? | Remark |
|---|---|---|---|---|
| GuestID | `int` (`Guest.cs:16`) | `INT PRIMARY KEY IDENTITY(1,1)` | Yes (all 3) | PK |
| FirstName | `string` (`Guest.cs:17`) | `NVARCHAR(100) NOT NULL` | Yes | C# has no length limit |
| LastName | `string` (`Guest.cs:18`) | `NVARCHAR(100) NOT NULL` | Yes | — |
| Email | `string` (`Guest.cs:19`) | `NVARCHAR(100) NOT NULL` | Yes | No UNIQUE constraint in SQL |
| PhoneNumber | `string` (`Guest.cs:20`) | `NVARCHAR(50) NOT NULL` | Yes | — |
| Country | `string` (`Guest.cs:21`) | `NVARCHAR(50) NOT NULL` | Yes | — |
| PassportNumber | `string` (`Guest.cs:22`) | `NVARCHAR(50) NULL` | Yes | The only nullable Guest column; not validated in C# either |

Column order in SQL: GuestID, FirstName, LastName, PassportNumber, Email, Country, PhoneNumber — ERD_DB follows this order, while C#/DCD has PassportNumber last.

#### Room

Sources: `NFHotel/Models/Room.cs`, `Database/01_CreateSchema.sql:16-23`, `NFHotel/SQL/01_CreateSchema.sql:30-37`, `NFHotel/SQL/03_AlterRoomNumberToString.sql`

| Field | C# type | SQL type | In diagram? | Remark |
|---|---|---|---|---|
| RoomId | `int` (`Room.cs:15`) | `RoomID INT PRIMARY KEY IDENTITY(1,1)` | Yes (DCD as `RoomId`, ERD as `RoomID`) | Casing differs: C# `RoomId` vs. SQL/ERD `RoomID` (unlike Booking/Guest where C# uses `ID`) |
| RoomNumber | `string` (default `""`) (`Room.cs:16`) | `INT NOT NULL` in both 01 scripts; changed to `NVARCHAR(20) NOT NULL` by `03_AlterRoomNumberToString.sql` | Yes — DCD says `string`, ERD untyped | Core conflict: the create scripts were never fixed, only patched afterwards |
| Floor | `int` (`Room.cs:17`) | `INT NOT NULL` | Yes | C# requires > 0; SQL has no CHECK |
| RoomSize | `string` (default `""`) (`Room.cs:18`) | `NVARCHAR(20) NOT NULL` | Yes | Free text ('Single'/'Double'/'Suite' in test data), not enum/lookup |
| Capacity | `int` (`Room.cs:19`) | `INT NOT NULL` | Yes | C# requires > 0; no CHECK in SQL |
| Status | `RoomStatus`, default `Available` (`Room.cs:20`) | `INT NOT NULL DEFAULT 0` | Yes in DCD_Domain/DCD_Architecture, **no** in ERD_DB | Stored as ordinal |

### Enums

#### BookingStatus (`NFHotel/Models/Enums/BookingStatus.cs:6-13`)

No explicit values, so implicit `int` backing from 0:

| Value | Number | Comment in the code |
|---|---|---|
| Pending | 0 | Booking created but not confirmed |
| Confirmed | 1 | Booking confirmed by guest/payment |
| CheckedIn | 2 | Guest has checked in |
| CheckedOut | 3 | Guest has checked out |
| Cancelled | 4 | Booking cancelled |

Storage: `BOOKING.Status INT NOT NULL DEFAULT 0`, with the same mapping documented in a comment in `Database/01_CreateSchema.sql:49`, `NFHotel/SQL/01_CreateSchema.sql:63` and in `Database/README.md:64`. Test data uses 0–4 directly. There is no CHECK constraint or lookup table, so an arbitrary int can be inserted.

#### RoomStatus (`NFHotel/Models/Enums/RoomStatus.cs:9-14`)

| Value | Number |
|---|---|
| Available | 0 |
| OutOfService | 1 |
| Maintenance | 2 |

Storage: `ROOM.Status INT NOT NULL DEFAULT 0`, mapping in a comment in both 01 scripts and in `Database/README.md:63`. `Database/02_InsertTestData.sql:14` uses value 1 for one room; `NFHotel/SQL/02_InsertTestData.sql` uses only 0. No CHECK constraint.

Note: the files are in `Models/Enums`, but the namespace is `NFHotel.Models` — folder and namespace do not match.

### SQL scripts

Set A = `Database/` (the Azure set), Set B = `NFHotel/SQL/` (the project/local set).

| # | File | Set | Purpose |
|---|---|---|---|
| 1 | `Database/01_CreateSchema.sql` | A | Drops and creates ROOM, GUEST, BOOKING with PK/FK and 5 indexes on BOOKING. Assumes the database `HotelBooking` already exists on Azure. |
| 2 | `Database/02_InsertTestData.sql` | A | Inserts fixed test data: 8 rooms, 5 guests, 7 bookings (December 2024) with status values 0–4. |
| 3 | `Database/03_InsertExtendedTestData.sql` | A | Extends the data set: 15 extra guests (20 in total) and ~99 bookings spread over Dec. 2025 – Jan. 2028. Assumes scripts 1+2 have been run. |
| 4 | `NFHotel/SQL/01_CreateSchema.sql` | B | Same schema as #1, but first creates the database `HotelBooking` if it does not exist. Otherwise identical. |
| 5 | `NFHotel/SQL/02_InsertTestData.sql` | B | Completely different content from #2: clears all three tables (DELETE + RESEED), inserts 10 rooms and 100 guests and generates random bookings per room 5 years ahead via a cursor. |
| 6 | `NFHotel/SQL/03_AlterRoomNumberToString.sql` | B | Patch: `ROOM.RoomNumber` from `INT` to `NVARCHAR(20) NOT NULL`. |
| 7 | `NFHotel/SQL/fix-booking-identity.sql` | B | Patch (2025-12-13): recreates BOOKING with `IDENTITY(1,1)`. Sets Start/EndDate to `DATETIME2`; does not recreate indexes or named FK constraints. |
| 8 | `NFHotel/SQL/generate room storedprocedures.sql` | B | Creates `uspGetAllRooms`, `uspGetRoomById`, `uspGetRoomsFromCriteria`, `uspUpdateRoom`, `uspDeleteRoom`. |
| 9 | `NFHotel/SQL/uspCreateRoom StoredProcedure.sql` | B | Creates `uspCreateRoom`. Lies separately from the other room procedures. |

Overall: A is Azure-oriented, three steps, no stored procedures, no patches. B is local-oriented, one (completely different) data script, both patch scripts and all stored procedures. Only ROOM has stored procedures — GUEST and BOOKING have none.

### Discrepancies

**U-01 — RoomNumber: int vs. string.** `Room.cs:16` has `string`; both `01_CreateSchema.sql` create `INT`. Only patched in set B. The Azure set has INT.

**U-02 — Test data inserts RoomNumber as a number.** `(101, 1, 'Single', ...)` without apostrophes in both 02 scripts.

**U-03 — uspUpdateRoom still uses INT for RoomNumber.** `generate room storedprocedures.sql:35` declares `@RoomNumber INT`, while `uspCreateRoom StoredProcedure.sql:4` uses `NVARCHAR(50)`.

**U-04 — RoomSize length differs.** The column is `NVARCHAR(20)`; `uspCreateRoom` takes `NVARCHAR(50)`.

**U-05 — StartDate/EndDate: DATE vs. DATETIME2.** Both 01 scripts use `DATE`; `fix-booking-identity.sql:14-15` recreates them as `DATETIME2`.

**U-06 — BookingID IDENTITY.** The 01 script has IDENTITY, but the fix script describes that BookingID in practice did not auto-increment. The running DB has deviated from the checked-in schema.

**U-07 — Indexes and named FK constraints are lost** at `fix-booking-identity.sql:32-33`.

**U-08 — ERD_DB.md lacks the Status column** on both ROOM and BOOKING.

**U-09 — ERD_DB.md is syntactically incomplete.** The Mermaid block is never closed; cardinalities are missing.

**U-10 — The DCD diagrams lack the FK fields** `RoomID`/`GuestID` on Booking.

**U-11 — BookingNumber exists nowhere beyond the code.**

**U-12 — DCD_Domain shows `Validate()` but not `ValidateEdit()`;** DCD_Architecture omits `Validate()` entirely.

**U-13 — The sequence diagram uses a Booking constructor that does not exist** (`SD_UC01_RegisterBooking.md:22`). `Booking.cs` has no declared constructors.

**U-14 — The sequence diagram assumes a combined INSERT of Booking + Guest;** there is no stored procedure for BOOKING or GUEST.

**U-15 — The two 02 scripts are mutually incompatible.** Same file name, completely different content. Running B after A deletes A's data.

**U-16 — Room stock differs between the sets.** A: 8 rooms over 3 floors. B: 10 rooms over 2 floors. `03_InsertExtendedTestData.sql` assumes A.

**U-17 — Column order in the GUEST INSERTs differs** between A and B.

**U-18 — Enum values have no enforcement in the database.** No CHECK constraint; nothing prevents the value 7.

**U-19 — Casing of the Room PK.** C# `Room.RoomId` versus SQL/ERD `RoomID` and C# `Booking.RoomID`.

**U-20 — The README describes a repository API that does not match DCD_Architecture** (`GetAllRooms()` vs. `GetAll()` etc.).

**U-21 — The README mentions files that do not exist in the read set** and omits `03_InsertExtendedTestData.sql`.

**U-22 — Guest.PassportNumber:** nullable in SQL, not validated in C#, but always filled in test data.

### Validation on the models

| Rule | Method/attribute | File:line |
|---|---|---|
| `FirstName` must not be null/empty/whitespace | `Guest.Validate()` | `NFHotel/Models/Guest.cs:49-50` |
| `LastName` must not be null/empty/whitespace | `Guest.Validate()` | `NFHotel/Models/Guest.cs:51-52` |
| `Email` must be filled in and contain both `@` and `.` | `Guest.Validate()` | `NFHotel/Models/Guest.cs:53-54` |
| `PhoneNumber` must not be empty (no format requirement) | `Guest.Validate()` | `NFHotel/Models/Guest.cs:55-56` |
| `Country` must not be empty | `Guest.Validate()` | `NFHotel/Models/Guest.cs:57-58` |
| `RoomNumber` must not be empty/whitespace | `Room.Validate()` | `NFHotel/Models/Room.cs:40-41` |
| `Floor` must be > 0 | `Room.Validate()` | `NFHotel/Models/Room.cs:43-44` |
| `RoomSize` must not be empty (no whitelist) | `Room.Validate()` | `NFHotel/Models/Room.cs:46-47` |
| `Capacity` must be > 0 | `Room.Validate()` | `NFHotel/Models/Room.cs:49-50` |
| `StartDate` must not be `default(DateTime)` | `Booking.Validate()` | `NFHotel/Models/Booking.cs:47-48` |
| `EndDate` must not be `default(DateTime)` | `Booking.Validate()` | `NFHotel/Models/Booking.cs:50-51` |
| `EndDate` must be strictly > `StartDate` | `Booking.Validate()` | `NFHotel/Models/Booking.cs:53-54` |
| `StartDate.Date` must not be before today | `Booking.Validate()` | `NFHotel/Models/Booking.cs:56-57` |
| Error only if `Room` is null AND `RoomID` is 0 | `Booking.Validate()` | `NFHotel/Models/Booking.cs:59-60` |
| Error only if `Guest` is null AND `GuestID` is 0 | `Booking.Validate()` | `NFHotel/Models/Booking.cs:62-63` |
| `CheckOutTime` must be strictly > `CheckInTime` when both are set | `Booking.Validate()` | `NFHotel/Models/Booking.cs:65-66` |
| `newEndDate > newStartDate` on submitted dates | `Booking.ValidateEdit()` | `NFHotel/Models/Booking.cs:75-76` |
| `newStartDate.Date` must not be before today | `Booking.ValidateEdit()` | `NFHotel/Models/Booking.cs:78-79` |
| The decorated DateTime must be strictly > the property the constructor names | `DateGreaterThanAttribute.IsValid()` | `NFHotel/Validation/DateGreaterThanAttribute.cs:20-47` |

Remarks:
- `DateGreaterThanAttribute` is not used on any of the three models — no DataAnnotations attributes at all.
- `IsValid` returns Success when the value is not a `DateTime` or the comparison value is null — absence is treated as valid.
- No `Validate()` method is called from the models themselves; they merely return `List<string>`.
- No validation of `PassportNumber`, no length check against NVARCHAR limits, no overlap check in the model layer.

### Unclear/contradictory (models)

1. Which SQL set is canonical? `Database/README.md` does not mention `NFHotel/SQL/` with a single word.
2. The actual column type of `RoomNumber` and `StartDate`/`EndDate` cannot be determined from the files alone.
3. The run order for `03_InsertExtendedTestData.sql` is undocumented.
4. Has `fix-booking-identity.sql` been run or not? Why the problem arose is not stated.
5. `RoomSize` is de facto an enum in data, but exists neither as a C# enum, a lookup table nor a CHECK constraint.
6. `Room.cs:8` imports `NFHotel.Core` without using anything from it.
7. `DCD_Architecture.md` draws only `RoomRepo` and `GuestRepo` against `DatabaseConfig`, not `BookingRepo`.
8. `RoomRepo.GetAllByAvailability()` appears in the diagram, but not on `IRoomRepo`.
9. ERD_DB states neither types, nullability nor cardinalities.
10. `Database/README.md:18` contains a database password in plain text — contrary to the file's own security section (`:77-91`).

---

## 2. analyse:repos

### Repo methods

| Repo | Method | SQL (short) | Returns | Error handling |
|---|---|---|---|---|
| BookingRepo | `void Create(Booking)` (`BookingRepo.cs:19`) | Inline `INSERT INTO BOOKING (...) VALUES (@...); SELECT CAST(SCOPE_IDENTITY() as int)` | void — sets `booking.BookingID` | `ArgumentNullException` on null (`:21`). No try/catch. `(int)cmd.ExecuteScalar()` unboxes without null check (`:41`) |
| BookingRepo | `List<Booking> GetAll()` (`:47`) | Inline `SELECT b.*, r.*, g.* FROM BOOKING b INNER JOIN ROOM r ... INNER JOIN GUEST g ...` | `List<Booking>` with `Room` and `Guest` filled in | None |
| BookingRepo | `Booking? GetById(int)` (`:109`) | Same join + `WHERE b.BookingID = @BookingID` | `Booking?` — null if not found (`:167`) | None |
| BookingRepo | `List<Booking> GetByStatus(BookingStatus)` (`:170`) | No own SQL — `GetAll()` + in-memory filter | `List<Booking>` | None |
| BookingRepo | `List<Booking> GetByRoomID(int)` (`:176`) | No own SQL — `GetAll()` + in-memory filter | `List<Booking>` | None |
| BookingRepo | `List<Booking> GetByGuestID(int)` (`:182`) | No own SQL — `GetAll()` + in-memory filter | `List<Booking>` | None |
| BookingRepo | `void Update(Booking)` (`:188`) | Inline `UPDATE BOOKING SET ... WHERE BookingID = @BookingID` | void | `ArgumentNullException` (`:190`); pre-`GetById` + `ArgumentException` (`:195`); `InvalidOperationException` if rowsAffected == 0 (`:227`) |
| BookingRepo | `void Delete(int)` (`:233`) | Inline `DELETE FROM BOOKING WHERE BookingID = @BookingID` | void | Pre-`GetById` + `ArgumentException` (`:238`); `ArgumentException` if rowsAffected == 0 (`:251`) |
| GuestRepo | `int AddGuest(Guest)` (`GuestRepo.cs:21`) | Inline INSERT + SCOPE_IDENTITY | `int` (new GuestID) | None. No null check; unboxes directly (`:40`) |
| GuestRepo | `Guest? GetByID(int)` (`:48`) | Inline SELECT ... WHERE GuestID = @GuestID | `Guest?` — null if not found (`:79`); mapped via ordinal index 0-6 | None |
| GuestRepo | `List<Guest> GetAll()` (`:86`) | Inline SELECT (no WHERE, no ORDER BY) | `List<Guest>` | None |
| GuestRepo | `List<Guest> GetAllByName(string)` (`:119`) | `WHERE FirstName LIKE @Name OR LastName LIKE @Name`, parameter `$"%{name}%"` (`:130`) | `List<Guest>` | None; a null name gives `"%%"` = everything |
| GuestRepo | `void UpdateGuest(Guest)` (`:155`) | Inline UPDATE | void | None — rowsAffected is ignored, updating a non-existent guest fails silently (`:176`) |
| GuestRepo | `void DeleteGuest(int)` (`:184`) | Inline DELETE | void | None — FK violation bubbles up as a raw `SqlException` (`:195`) |
| RoomRepo | `void CreateRoom(Room)` (`RoomRepo.cs:19`) | SP `uspCreateRoom` | void — the new RoomId is not read back | `room.Validate()` → `ArgumentException` (`:21-23`) |
| RoomRepo | `List<Room> GetAll()` (`:46`) | SP `uspGetAllRooms` | `List<Room>` via `GetOrdinal` | None |
| RoomRepo | `Room? GetById(int)` (`:84`) | SP `uspGetRoomById` | `Room?` — null if empty (`:101`) | None |
| RoomRepo | `List<Room> GetRoomsFromCriteria(int?, string, RoomStatus?)` (`:120`) | SP `uspGetRoomsFromCriteria` (DBNull on null/blank) | `List<Room>` | None; filter logic in the procedure |
| RoomRepo | `void UpdateRoom(Room)` (`:162`) | SP `uspUpdateRoom` | void | `room.Validate()` → `ArgumentException` (`:164-166`). rowsAffected is ignored |
| RoomRepo | `void DeleteRoom(int)` (`:190`) | SP `uspDeleteRoom` | void | The only explicit catch in the whole layer: `catch (SqlException ex) when (ex.Number == 547)` → `InvalidOperationException` (`:208-211`) |
| RoomRepo | `List<Room> GetAllByAvailability()` (`:217`) | No own SQL — `GetAll()` + LINQ `Status == Available` | `List<Room>` | None |
| DatabaseConfig | `static bool TestConnection()` (`DatabaseConfig.cs:23`) | Only `connection.Open()` | `bool` | Swallow-all: `catch (Exception) { return false; }` (`:33-36`) |
| DatabaseConfig | `static void TestConnectionWithDetails()` (`:42`) | `Open()` + `Console.WriteLine` | void | No catch |

### Interfaces vs. implementation

**IBookingRepo** — all 8 methods (`Create`, `GetAll`, `GetById`, `GetByStatus`, `GetByRoomID`, `GetByGuestID`, `Update`, `Delete`) are implemented. No extra public methods. **Signature deviation:** the interface promises `Booking GetById(int)` (`IBookingRepo.cs:12`, non-nullable), the class returns `Booking?` and can give null.

**IGuestRepo** — all 6 methods implemented. **Signature deviation:** `Guest GetByID(int id)` (`IGuestRepo.cs:17`) vs. `Guest? GetByID(int guestId)` (`GuestRepo.cs:48`); the parameter name also differs (`id` vs. `guestId`), which breaks named arguments.

**IRoomRepo** — all 6 methods implemented. **Only in the class:** `GetAllByAvailability()` (`RoomRepo.cs:217`) — callers programming against the interface cannot reach it without a cast. **Signature deviation:** `string? roomSize` in the interface (`IRoomRepo.cs:17`) vs. `string roomSize` in the class (`RoomRepo.cs:120`).

### Connection string and DatabaseConfig

- `DatabaseConfig` is a `static class` with `public static string ConnectionString { get; set; }` (`NFHotel/Database/DatabaseConfig.cs:13-17`) — global mutable state without `readonly`, without validation, without initialisation.
- The value is set in one place: `App.OnStartup` builds a `ConfigurationBuilder` (base path = `AppDomain.CurrentDomain.BaseDirectory`), reads `appsettings.json` (`optional: false`) and assigns `GetConnectionString("DefaultConnection")` (`NFHotel/App.xaml.cs:18-23`).
- No hard-coded credentials in the files read; the string itself is in `appsettings.json`, which was not in the reading area.
- Each repo reads the static property **once in the constructor** and stores it in `readonly string _connectionString`: `BookingRepo.cs:14-17`, `GuestRepo.cs:13-16`, `RoomRepo.cs:12-15`. A repo instantiated before `OnStartup` holds null permanently; `reloadOnChange: true` has no effect.
- `TestConnection()`/`TestConnectionWithDetails()` are not called from repos or `App.xaml.cs`.
- Unused `using System.Configuration;` in both `DatabaseConfig.cs:3` and `App.xaml.cs:1`.

### Dependencies (who news up what)

- `App.OnStartup` news up `ConfigurationBuilder` (`App.xaml.cs:18`) and writes to the static `DatabaseConfig` (`:23`). **No DI container, no ServiceCollection, no registration of repo interfaces.**
- All three repos have a parameterless constructor and fetch the dependency themselves from global state (`BookingRepo.cs:14`, `GuestRepo.cs:13`, `RoomRepo.cs:12`). They cannot be constructed with a different connection string.
- ADO.NET objects are newed up directly per call (`new SqlConnection`, `new SqlCommand`) — no connection factory.
- `BookingRepo.GetAll`/`GetById` themselves new up `Room` and `Guest` objects from the join (`:78`, `:87`, `:140`, `:149`) — so they do not use `RoomRepo`/`GuestRepo`. The mapping is duplicated.
- `RoomRepo.CreateRoom`/`UpdateRoom` call `room.Validate()` (`:21`, `:164`) — the repo depends on model validation.
- Internal reuse: `GetByStatus/GetByRoomID/GetByGuestID` → `GetAll()`; `Update`/`Delete` → `GetById()`; `GetAllByAvailability` → `GetAll()`.
- **Who instantiates the repos themselves cannot be established** from here — the call sites are in ViewModels/Views (see section 3).

### Sync/async, resource handling and SQL injection

**Synchronous all the way** — not one `async`/`await` or `...Async` method in the three repos or `DatabaseConfig`. All DB calls block the calling thread. `Open()`: `BookingRepo.cs:25,52,113,200,243`; `GuestRepo.cs:25,52,92,124,159,188`; `RoomRepo.cs:27,52,88,126,170,196`. No `CommandTimeout` is set anywhere.

**`using` is used consistently.** Connection, command and reader are all in `using` blocks in every method. No leaked ADO.NET objects found.

**SQL injection: no risk.** No string interpolation or concatenation into SQL text. All inline queries use named parameters; all of `RoomRepo` goes through `CommandType.StoredProcedure`. The one place with interpolation (`GuestRepo.cs:130`) builds `$"%{name}%"` as a **parameter value** — safe, but `%` and `_` in user input are not escaped and act as wildcards.

**Other robustness problems:**
- No transactions. `BookingRepo.Update` (`:192` + `:198`) and `Delete` (`:235` + `:241`) do read-then-write over two separate connections — the row can be deleted in between.
- `AddWithValue` everywhere → type inference, plan-cache bloat, implicit conversions.
- `(int)cmd.ExecuteScalar()` (`BookingRepo.cs:41`, `GuestRepo.cs:40`) throws NullReferenceException/InvalidCastException instead of a meaningful error.
- `DatabaseConfig.TestConnection` swallows every exception (`:33`) — a wrong password and a down server cannot be told apart.

### Business rules found in repos

- **Only free rooms count as available:** the filter `Status == Available` lives in the data access layer — `RoomRepo.cs:220`.
- **Status filter on bookings** as an in-memory filter after a full table pull — `BookingRepo.cs:173`.
- **A room with existing bookings must not be deleted** (it must instead be set to Out of service) — the rule including the user-facing text is coded in the catch of FK violation 547 — `RoomRepo.cs:208-210`.
- **A room must be valid before persisting** — `RoomRepo.cs:21-23` and `:164-166`.
- **A booking must exist before it may be updated/deleted** — `BookingRepo.cs:192-196` and `:235-239`.
- **Name search on guests is "contains" on first or last name** (wildcards on both sides, OR between columns) — `GuestRepo.cs:127,130`.
- **Not found:** there is **no overlap check** anywhere in `BookingRepo` — neither in `Create` nor in `Update`.

### Unclear/contradictory (repos)

- **App.config vs. appsettings.json:** the XML doc says "fetched from App.config" (`DatabaseConfig.cs:9`), but the source is `appsettings.json` (`App.xaml.cs:20-23`). Unused `using System.Configuration;` in both places suggests a half-finished switch.
- **Nullability is inconsistent between interface and class** in all three repos.
- **`ConnectionString` is `string` (not `string?`)** but is never initialised; if the key `DefaultConnection` is missing, it is silently set to null.
- **Two different data access styles side by side** without explanation: `RoomRepo` = stored procedures, the other two = inline SQL. The consequence is that the filter logic in `uspGetRoomsFromCriteria` cannot be verified from the C# code.
- **Mapping of Room and Guest exists in two places** (`BookingRepo.cs:78-96`, `:140-158` in parallel with `RoomRepo.GetAll` and `GuestRepo.GetAll`). `BookingRepo` uses `GetOrdinal`, `GuestRepo` hard-coded indexes 0-6 — the latter breaks silently if the column order changes.
- **Column name collision is handled for only one column:** `r.Status` is aliased to `RoomStatus` (`BookingRepo.cs:57`, `:117`), while `b.RoomID`/`r.RoomID` and `b.GuestID`/`g.GuestID` are selected without an alias. Works only because of the join condition.
- **`GetAllByAvailability` is left outside `IRoomRepo`;** the comment `// ✅ Implement interface method` on `GetAll` (`:46`) suggests manual and unfinished interface synchronisation.
- **`TestConnection`/`TestConnectionWithDetails` are not used** by the files read; `TestConnectionWithDetails` writes to `Console`, which is not visible in a WPF app.

---

## 3. analyse:viewmodels

The full BR table (BR-01 to BR-90) and the state diagram have been moved to `BusinessRules.md`. Below are the agent's method coverage and observations.

### Method coverage

**BookingOverviewViewModel** (`NFHotel/ViewModels/BookingOverviewViewModel.cs`)
- `BookingOverviewViewModel()` (parameterless ctor) — no rules
- `BookingOverviewViewModel(IBookingRepo, IRoomRepo)` — BR-02, BR-06, BR-09, BR-11, BR-14, BR-16, BR-26, BR-28
- `LoadData()` — BR-34, BR-23, BR-31
- `RefreshData()` — no rules
- `ChangeMonth(int)` — BR-28, BR-29
- `UpdateDaysInMonth()` — BR-29
- `MatchesSearch(Booking)` — BR-25
- `FilterBookings()` — BR-23, BR-24, BR-25
- `UpdateRevenue()` — BR-31
- `OpenNewBooking()` — BR-33
- `SortBookings(object)` — BR-32
- `GetPropValue(object, string)` — no rules
- `CanExecuteConfirm()` — BR-02 · `ConfirmBooking()` — BR-03, BR-04, BR-05
- `CanExecuteCheckIn()` — BR-06 · `CheckInBooking()` — BR-07, BR-08
- `CanExecuteCheckOut()` — BR-09 · `CheckOutBooking()` — BR-10
- `CanExecuteCancel()` — BR-11 · `CancelBooking()` — BR-12, BR-13
- `CanExecuteEdit()` — BR-14 · `EnterEditMode()` — BR-15
- `SaveEdit()` — BR-17, BR-18, BR-19, BR-20 · `CancelEdit()` — BR-21
- Properties: `ViewStartDate` — BR-27; `SelectedBooking` (set) — BR-22; `SearchText` (set) — BR-26; `ViewDuration` (set) — BR-29; `CurrentMonth` (set) — BR-23, BR-31; `CurrentMonthDisplay` — BR-30; `IsEditMode` (set) — BR-16; `DayCount`, `IsViewMode`, `RevenueThisMonth`, `EditRoom`, `EditCheckInDate`, `EditCheckOutDate` — no rules

**NewBookingViewModel**
- ctor `(Guest?)` — BR-34, BR-35 · `LoadAllAvailableRooms()` — BR-34, BR-35
- `UpdateAvailableRooms()` — BR-37, BR-38, BR-39, BR-40 · `LoadFromGuest(Guest)` — BR-35
- `CreateBooking(object)` — BR-41 to BR-51
- `CloseWindow()` — no rules · `ClearForm()` — no rules (dead code)
- Properties: `CheckInDate`/`CheckOutDate` (set) — BR-36; others — no rules

**NewGuestViewModel**
- ctor — BR-58, BR-59, BR-62, BR-63 · `ValidateAll()` — BR-59
- `ValidateFirstName/LastName/PhoneNumber/Email/Country()` — BR-52 to BR-56
- `UpdateCanSave()` — BR-58 · `SaveGuest()` — BR-60, BR-61, BR-62 · `Cancel()` — BR-64
- Properties: the five field setters — BR-59; `PassportNumber` (set) — BR-57; others — no rules

**GuestOverviewViewModel**
- ctor — BR-65 · `LoadGuests()` — no rules
- `FilterAndSortGuests()` — BR-68, BR-69, BR-70 · `MatchSearchGuest()` — BR-68
- `OnNewGuest()` — no rules · `OnEditGuest()` — BR-66, BR-67 · `ClearGuest()` — BR-71
- `OnBookingRequestedForGuest()` — BR-73 · `AddGuestToOverview(Guest)` — no rules
- `UpdateSelectedGuest(Guest)` — BR-72 · `SortGuestsByName()` — BR-69
- Properties: `SelectedGuest` (set) — BR-65; `SearchGuest` (set) — BR-68, BR-69, BR-70; others — no rules

**RoomOverviewViewModel**
- ctor — BR-75 · `LoadRooms()` — no rules · `LoadFilterOptions()` — BR-75
- `ApplyFilter()` — BR-74 · `ClearFilter()` — BR-76
- `OpenCreateRoom()` — BR-81, BR-83 · `OpenEditRoom(Room)` — BR-79, BR-83
- `DeleteRoom(Room)` — BR-77, BR-78, BR-79 · `OnRoomSaved()` — BR-80 · `ClosePanel()` — no rules
- Properties — no rules

**RoomFormViewModel** — ctor — BR-81, BR-83 · `Save()` — BR-82, BR-84

**MainViewModel** — ctor — BR-85, BR-86 · `CurrentView` — no rules

**GuestPolicyViewModel** — no methods (the class is completely empty)

**SalesOverviewViewModel** — no methods (the class is completely empty)

**RelayCommand** — ctor — no rules · `CanExecute(object)` — BR-87 · `Execute(object)` — no rules · `RaiseCanExecuteChanged()` — no rules

**DateGreaterThanAttribute** — ctor — no rules · `IsValid(object, ValidationContext)` — BR-88, BR-89, BR-90

**ObservableObject** — `OnPropertyChanged(string)` — no rules

### Unclear/contradictory (viewmodels)

1. **Revenue is never calculated.** `UpdateRevenue()` always sets 0 (`:415-418`). BR-31 is in effect an unimplemented rule.
2. **Week and month views do not filter by period** — only `ViewDuration == "Year"` filters (`:389-402`). Looks unintended.
3. **Two different overlap rules.** Creation excludes only `Cancelled` (`NewBookingViewModel.cs:204-209`); editing excludes `Cancelled` AND `CheckedOut` (`BookingOverviewViewModel.cs:768-775`).
4. **No overlap check on the creation itself.** `CreateBooking()` trusts the filtered list.
5. **Check-in from `Pending` contradicts the meaning of Confirm** — the dialog says "Payment has been received", but you can check in without it.
6. **Redundant sub-conditions** in BR-06 and BR-09 (logically superfluous given the status checks).
7. **An edited guest loses its id.** `OnEditGuest()` copies six fields but not `GuestID` → `UpdateGuest` on id 0.
8. **`AddGuestToOverview` does not update `_allGuests`** — a newly created guest disappears at the next search.
9. **Room validation is ignored.** `RoomFormViewModel.Save():58` assigns `Room.Validate()` to `errors` and never uses it.
10. **Dead code.** `NewBookingViewModel.ClearForm()` is never called; `GuestPolicyViewModel` and `SalesOverviewViewModel` are empty.
11. **`DateGreaterThanAttribute` is not used in the ViewModel layer** — date validation is hand-coded instead. Potentially double-maintained logic.
12. **Search locks the view permanently to year** — it does not switch back when the search field is cleared.
13. **Cancellation is called "permanent" but is a soft delete.**
14. **No upper time limit on check-in** — a booking whose `EndDate` has long passed can still be checked in.
15. **Changes to an existing guest are not saved on a new booking** — only `GuestID == 0` is written to the DB.
16. **Uneven error handling on status change** — Confirm/Cancel/SaveEdit show a MessageBox; CheckIn/CheckOut only write to `Debug.WriteLine` (`:592`, `:639`), even though `Status` has already been changed in memory.
17. **`NumberOfNights`, `Booking.ValidateEdit`, `Guest.Validate`, `Room.Validate`** are in the model layer — see section 1.

---

## 4. analyse:views

### 4.1 MainWindow (`NFHotel/MainWindow.xaml`)

The application's shell: a fixed left side menu with logo + 5 navigation buttons, and a `ContentControl` that shows the active view. `DataContext` is set declaratively to `MainViewModel` (`:15-17`); the code-behind contains only `InitializeComponent()`.

| Action | Command | Bound properties |
|---|---|---|
| "Booking Overview" (default, `IsChecked=True`) | `BookingOverviewViewCommand` (`:55`) | — |
| "Guest Overview" | `GuestOverviewViewCommand` (`:65`) | — |
| "Room Overview" | `RoomOverviewViewCommand` (`:74`) | — |
| "Sales Overview" | `SalesOverviewViewCommand` (`:83`) | — |
| "Guest Policy" | `GuestPolicyViewCommand` (`:92`) | — |
| (display of the active view) | — | `CurrentView` (`:109`) |

Dialogs/popups: none.

### 4.2 BookingOverviewView (1093 lines)

The main screen: a calendar timeline (rooms on the Y axis, days on the X axis) with bookings as coloured bars, plus a right-hand panel with revenue and booking details in view/edit mode. With `ViewDuration = Year` the calendar is replaced by a sortable ListView (`:405-555`). The code-behind is empty.

| Action | Command | Bound properties |
|---|---|---|
| Create new booking | `NewBookingCommand` (`:60`) | — |
| Search for guest (live) | none — `UpdateSourceTrigger=PropertyChanged` | `SearchText` (`:96`) |
| Previous period | `PreviousMonthCommand` (`:131`) | `CurrentMonthDisplay` (`:138`) |
| Next period | `NextMonthCommand` (`:143`) | `CurrentMonthDisplay` |
| Week view | `SetWeekViewCommand` (`:153`) | `ViewDuration` (`:158`) |
| Month view | `SetMonthViewCommand` (`:170`) | `ViewDuration` (`:175`) |
| Year view (list) | `SetYearViewCommand` (`:186`) | `ViewDuration` (`:191, :210, :414`) |
| Click on booking bar | `SelectBookingCommand` (MouseBinding, `:363-366`) | the booking object as CommandParameter |
| Select booking in the year list | — (SelectedItem) | `FilteredBookings`, `SelectedBooking` (`:408-409`) |
| Sort list (7 column buttons) | `SortCommand` via `BindingProxy` (`:430,457,472,487,502,517,532`) | CommandParameter = property path as string |
| Edit booking | `EditBookingCommand` (`:950`) | `IsViewMode`, `SelectedBooking.Status` |
| Save changes | `SaveEditCommand` (`:922`) | `IsEditMode`, `EditRoom`, `EditCheckInDate`, `EditCheckOutDate` |
| Cancel editing | `CancelEditCommand` (`:934`) | `IsEditMode` |
| Confirm booking | `ConfirmBookingCommand` (`:979`) | `SelectedBooking.Status` |
| Check in | `CheckInBookingCommand` (`:1006`) | (only `IsEnabled` from CanExecute) |
| Check out | `CheckOutBookingCommand` (`:1057`) | (only `IsEnabled`) |
| Cancel booking | `CancelBookingCommand` (`:1028`) | `SelectedBooking.Status` |

Other bindings: `Rooms` (`:273`), `DaysInMonth` (`:246,306`), `FilteredBookings` (`:327`), `ViewStartDate`+`DayCount` (`:349-351,357-359`), `RevenueThisMonth` (`:584`), `AllRooms` (`:764`), `SelectedBooking.*`.

Dialogs: none. The right-hand panel is an inline detail panel; "Payment Status" is a placeholder with "Coming soon..." (`:602`). Empty state: "Select a booking to view details." (`:1078-1088`).

### 4.3 GuestOverviewView

Master/detail over guests. The code-behind instantiates its own ViewModel and connects four VM events to WPF dialogs (`GuestOverviewView.xaml.cs:26-38`).

| Action | Command | Bound properties |
|---|---|---|
| Search for guest | none | `SearchGuest` (`:45`) |
| Enter in the search field → sort | code-behind `SearchTextBox_KeyDown` → `vm.SortGuestsByName()` (`.xaml.cs:68-77`) | — |
| Create new guest | `NewGuestCommand` (`:76`) | — |
| Select guest | — (SelectedItem) | `Guests`, `SelectedGuest` (`:95-96`) |
| Edit guest | `EditGuestCommand` (`:183`) | `SelectedGuest` via `NotNullToBoolConverter` (`:186`) |
| Create booking for guest | `CreateBookingForGuestCommand` (`:188`) | — |
| Clear fields | `ClearGuestCommand` (`:192`) | — |

Fields (all `IsReadOnly = !IsEditing`): `SelectedGuest.FirstName/.LastName/.PhoneNumber/.Email/.Country/.PassportNumber` (`:130-181`).

Dialogs (all from code-behind): `NewGuestView` modal for creation (callback `AddGuestToOverview`, `:41-49`), `NewGuestView` modal for editing a copy (callback `UpdateSelectedGuest`, `:52-60`), `NewBookingView` modal with the selected guest (`:61-66`), `MessageBox` via the `ShowInfoDialog` event (`:34-35`).

### 4.4 NewGuestView (modal Window)

Form for creating/editing a guest; the same window for both, controlled by `isEdit` in the constructor.

| Action | Command | Bound properties |
|---|---|---|
| Save (dynamic button text) | `SaveCommand`, `IsEnabled={Binding CanSave}` (`:55`) | `SaveButtonText`, `CanSave` |
| Cancel | `CancelCommand` (`:56`) | — |
| Fill in fields | — | `FirstName`, `LastName`, `PhoneNumber`, `Email`, `Country`, `PassportNumber` (`:22-47`) |
| (title/error) | — | `WindowTitle` (`:14`), `ErrorMessage` (`:50`) |

The code-behind connects `ShowError`, `ShowInfo`, `ShowConfirmation`, `RequestClose`, `OnSave` (`.xaml.cs:14-19`).

### 4.5 NewBookingView (modal Window)

| Action | Command | Bound properties |
|---|---|---|
| Choose check-in date | — | `CheckInDate` (`:60`) |
| Choose check-out date | — | `CheckOutDate` (`:80`) |
| Choose room | — (SelectedItem) | `NewBookingRoomList`, `SelectedRoom` (`:96-97`) |
| Enter guest details | — | `FirstName`, `LastName`, `Email`, `PassportNumber`, `PhoneNumber`, `Country` (`:147-223`) |
| Save booking | `SaveBookingCommand` (`:238`) | — |
| (error display) | — | `ErrorMessage`, OneWay (`:259`) |

**There is no Cancel button** — the window can only be closed via the title bar's X.

### 4.6 RoomOverviewView

| Action | Command | Bound properties |
|---|---|---|
| Create room (opens side panel) | `OpenCreateRoomCommand` (`:50`) | `IsRoomFormOpen`, `CurrentSideContent` |
| Open filter popup / apply filter | `ApplyFilterCommand` on ToggleButton (`:73`) | `IsChecked` controls the popup (`:79`) |
| Choose filter values | — | `Floors`/`SelectedFloor`, `RoomSizes`/`SelectedRoomSize`, `RoomStatuses`/`SelectedRoomStatus` (`:93-107`) |
| Apply filter (button in popup) | `ApplyFilterCommand` (`:113`) | — |
| Clear filter | `ClearFilterCommand` (`:128`) | — |
| Edit room (per row) | `OpenEditRoomCommand`, param = the row's Room (`:170-171`) | — |
| Delete room (per row) | `DeleteRoomCommand`, param = the row's Room (`:183-184`) | — |
| (table) | — | `Rooms` with `RoomNumber`, `Floor`, `RoomSize`, `Capacity`, `Status` (`:143-165`) |

Popup with filter choices anchored to `FilterButton` (`:76-118`). The form is shown as an inline side panel (`:207-209`). Empty, unused handler `DataGrid_SelectionChanged` (`.xaml.cs:30-33`).

### 4.7 RoomFormView

| Action | Command | Bound properties |
|---|---|---|
| Fill in room data | — | `Room.RoomNumber`, `Room.Floor`, `Room.RoomSize`, `Room.Capacity` (`:26-38`) |
| Choose status (Available/Maintanance/Cleaning/Disabled — hard-coded in XAML) | — | `Room.Status` (`:42-46`) |
| Cancel | `CancelCommand` (`:59`) | — |
| Save (dynamic button text) | `SaveCommand` (`:63`) | `ButtonText` (`:61`) |
| (heading) | — | `Title` (`:17`) |

### 4.8 GuestPolicyView and SalesOverviewView

Both are pure placeholder screens with only a heading TextBlock. No bindings, no commands, empty code-behind.

### Rules hidden in converters/code-behind

**Real business rules**

1. **The booking's width expresses the number of overnight stays, not the number of days** — `(EndDate − StartDate).Days`. `Converters/CalendarConverters.cs:149`
2. **Half-day rule for check-in:** a booking is drawn from the middle of the check-in day (`daysOffset + 0.5`) — encodes the assumption that the room is occupied from noon. `Converters/CalendarConverters.cs:31`, `:48`
3. **Bookings started before the displayed period are clipped to the start of the period** and get `+0.5` day on the width. `Converters/CalendarConverters.cs:81`, `:139`
4. **A room's timeline shows only bookings where `Booking.Room.RoomId` matches;** bookings without a room are omitted entirely. `Converters/CalendarConverters.cs:197`
5. **The booking status flow is coded in XAML triggers:** Confirm is shown only at `Pending` (`Views/BookingOverviewView.xaml:988`); Cancel only at `Pending`/`Confirmed` (`:1037`, `:1040`); Edit is hidden at `CheckedIn`/`CheckedOut` (`:958`, `:961`).
6. **Editing of guest fields is only allowed in edit mode** — all six fields `IsReadOnly = !IsEditing`. `Views/GuestOverviewView.xaml:132` (and `:142,152,162,172,179`)
7. **"Edit Guest" requires a selected guest** (`NotNullToBoolConverter`) — `Views/GuestOverviewView.xaml:186`. The "New Booking" button right below has **no** corresponding guard (`:187-190`).
8. **When a guest is edited, a copy is worked on**, which is only written back via `UpdateSelectedGuest`. `Views/GuestOverviewView.xaml.cs:52-60`
9. **Enter in the guest search field triggers sorting, not search.** `Views/GuestOverviewView.xaml.cs:68-77`
10. **The room statuses are hard-coded as four XAML elements** instead of an enum/VM collection. `Views/Forms/RoomFormView.xaml:43-46`

**Pure presentation (no business rule)**

11. Status colours: Pending=yellow, Confirmed=blue, CheckedIn=green, CheckedOut=grey, Cancelled=red. `Converters/BookingStatusColorConverter.cs:15-34`
12. `CheckInStatusConverter` **does not decide whether check-in is possible** despite its name — it only formats text from timestamps. `Converters/CheckInStatusConverter.cs:16-27`
13. `InverseBoolConverter` — negation. `Converters/InverseBoolConverter.cs:11-13`
14. `NotNullToBoolConverter` — null check to bool. `Converters/NotNullToBoolConverter.cs:11`
15. `DateHeaderConverter` — int to string, functionally a no-op. `Converters/CalendarConverters.cs:171-178`
16. `BindingProxy` — technical helper for GridView column headers. `Core/BindingProxy.cs:5-20`
17. The `Theme/` folders contain only styles — no Binding, Command or DataTrigger.

### Navigation

- **One ContentControl as the navigation surface** — `MainWindow.xaml:104-109` binds `Content` to `CurrentView`.
- **DataContext is set declaratively** to `MainViewModel` (`MainWindow.xaml:15-17`) — no DI, no container.
- **Five RadioButtons each call their own command** (`MainWindow.xaml:55,65,74,83,92`). "Booking Overview" is the default (`:60`).
- **View lookup via implicit DataTemplates in App.xaml** (`App.xaml:19-37`).
- **Global converter registration:** `InverseBoolConverter`, `BooleanToVisibilityConverter`, `NotNullToBoolConverter` (`App.xaml:39-41`). The calendar converters are local to BookingOverviewView (`:17-25`).
- **Intra-view navigation:** RoomOverviewView uses the same pattern with a local DataTemplate (`RoomOverviewView.xaml:17-19`) and `ContentControl` (`:209`) controlled by `IsRoomFormOpen` (`:207`).
- **Modal navigation:** `NewGuestView` and `NewBookingView` are opened with `ShowDialog()` from GuestOverviewView's code-behind (`:48, 59, 64`). They are not part of the DataTemplate navigation.
- **The side menu has no entry for NewBooking/NewGuest/RoomForm** — only reachable from inside other views.

### Empty/unimplemented views

1. `Views/GuestPolicyView.xaml:11-16` — heading only. The menu item exists, though (`MainWindow.xaml:92`).
2. `Views/SalesOverviewView.xaml:11-16` — heading only (`MainWindow.xaml:83`).
3. Partly: "Payment Status" in BookingOverviewView is hard-coded "Coming soon..." (`:599-602`).
4. Dead code: empty `DataGrid_SelectionChanged` in `Views/RoomOverviewView.xaml.cs:30-33`.

### Unclear/contradictory (views)

1. **Inconsistent DataContext strategy.** `GuestOverviewView.xaml.cs:29` and `RoomOverviewView.xaml.cs:27` instantiate a ViewModel themselves, overwriting the App.xaml DataTemplate instance. A view switch resets Guest and RoomOverview, but not BookingOverview.
2. **`ApplyFilterCommand` is bound to both the ToggleButton and the Apply button** (`RoomOverviewView.xaml:73` and `:113`) — the filter runs every time the popup is opened *and* closed.
3. **RoomFormView's status ComboBox binds `SelectedItem` to `Room.Status`, but the items are `ComboBoxItem` objects** (`RoomFormView.xaml:41-47`). A classic WPF source of errors. Note the typo "Maintanance" (`:44`).
4. **The Check In/Check Out buttons have no status triggers** (`:1000-1019`, `:1052-1070`) unlike Edit/Confirm/Cancel — they are controlled only by `CanExecute`.
5. **`BookingLeftMarginConverter` contains contradictory half-finished logic** (`CalendarConverters.cs:37-48`); the outer if has in effect no effect, and the comments are a stream of thought, not a specification. Same pattern in `BookingWidthConverter` (`:86-150`).
6. **The search field binds to `SearchGuest`, but Enter calls `SortGuestsByName()`** — looks like a name/behaviour mix-up.
7. **Two parallel guest creation flows** — `NewGuestView` with validation and a `CanSave` guard, `NewBookingView` with loose guest fields without a guard.
8. **`NewBookingView` lacks a Cancel button** — unlike `NewGuestView` and `RoomFormView`.
9. **`GuestOverviewView.OpenNewBooking` does not update the overview afterwards**, even though the comment says it does (`.xaml.cs:61-65`).
10. **`OpenNewBooking` does not set `Owner`** (`.xaml.cs:63-64`), unlike the two other dialog openings.
11. **The booking bar's ToolTip shows `GuestID`** (`:345`) — probably a left-over debug binding.
12. **`SortCommand` takes magic strings as a parameter** in seven places — any renaming silently breaks sorting at runtime.
13. **`ViewDuration` is compared against the strings "Week"/"Month"/"Year"** in DataTriggers (`:158,175,191,210,414`).

---

## 5. analyse:tests

### Overview of the test files

| Test file | Number of tests | Tests | Requires DB? | Reuse value + justification |
|---|---|---|---|---|
| `NFHotel_Tests/Models/BookingTests.cs` | 16 | Model (`Booking`) | No in the test — but yes in practice (see DB section) | **High.** Pure, fast tests of `Validate()`, `ValidateEdit()`, `NumberOfNights`, `BookingNumber`. Can be used 1:1 as the source of truth. |
| `NFHotel_Tests/Models/GuestTests.cs` | 13 | Model (`Guest`) | No in the test — but yes in practice | **High.** Locks the whole validation contract including the exact error texts. 2 of 13 are constructor boilerplate. |
| `NFHotel_Tests/Models/RoomTests.cs` | 17 | Model (`Room`) | No in the test — but yes in practice | **Medium.** The core validation (5-6 tests) is valuable; about half are trivial copies. |
| `NFHotel_Tests/Repositories/BookingRepoTests.cs` | 12 | Repo | **Yes — live SQL Server** | **Medium.** The error contract and navigation property loading are real rules, but inseparably tied to a physical DB. |
| `NFHotel_Tests/Repositories/GuestRepoTests.cs` | 13 | Repo | **Yes — live SQL Server** | **Low.** Almost exclusively CRUD roundtrips. Only `GetAllByName` and nullable passport express real behaviour. No cleanup hook. |
| `NFHotel_Tests/Repositories/RoomRepoTests.cs` | 15 | Repo | **Yes — live SQL Server** | **Medium.** `GetAllByAvailability` and the four `GetRoomsFromCriteria` tests describe a real filter API. The rest is CRUD. |
| `NFHotel_Tests/ViewModels/BookingOverviewViewModelTests.cs` | 22 | ViewModel | **No — the only file with real mocks** (Moq against `IBookingRepo`/`IRoomRepo`) | **High.** The most valuable file: 20 of 22 tests encode the booking's state machine. An executable use case specification. |
| `NFHotel_Tests/ViewModels/NewBookingViewModelTests.cs` | 20 | ViewModel | **Probably yes, indirectly** — the VM is constructed parameterless without mocks (`:23`) | **Low.** 14-16 of 20 are property boilerplate. Only `CheckOutDate_BeforeCheckIn_SetsErrorMessage` (`:264`) tests logic. |

**In total 128 `[TestMethod]`** (the README claims 129).

`MSTestSettings.cs` (31 lines): `[AssemblyInitialize]` builds a `ConfigurationBuilder` from `appsettings.json` and sets the static `DatabaseConfig.ConnectionString`. Sets `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]` (`:7`).

`NFHotel_Tests.csproj` (29 lines): `net8.0-windows`, `Nullable enable`, `ImplicitUsings enable`. Packages: `Moq 4.20.72`, `MSTest 4.0.1`. Project reference to the WPF project. `<None Update="appsettings.json">` with `PreserveNewest`.

### DB dependency

**Short answer: yes — the repo tests hit a real, shared SQL Server database, and it is a local/on-prem instance, not Azure.**

**Which DB is hit**
- No hard-coded connection string in the test code. It comes exclusively from `appsettings.json` → `ConnectionStrings:DefaultConnection` (`MSTestSettings.cs:28`).
- The README's template is `Server=YOUR_SERVER_NAME;Database=HotelBooking;Trusted_Connection=True;TrustServerCertificate=True` (`README.md:56`), and the suggested server names are `COMPUTERNAME\SQLEXPRESS`, `localhost\SQLEXPRESS`, `(localdb)\MSSQLLocalDB` (`:46-50`). `Trusted_Connection=True` is not supported by Azure SQL Database. **No trace of Azure in the test project.**
- The database name is `HotelBooking` — **the same database the application itself uses**. No separate test database, no transaction rollback, no in-memory replacement.

**How it is configured**
- One global `[AssemblyInitialize]` hook (`MSTestSettings.cs:18-29`) writes to the static, global, mutable `DatabaseConfig.ConnectionString`. The only configuration surface.
- Repos are instantiated parameterless in `[TestInitialize]` (`BookingRepoTests.cs:27-29`, `GuestRepoTests.cs:22`, `RoomRepoTests.cs:22`) — **no constructor injection**, which is why they cannot be mocked.
- The ViewModel *does* have an injection surface: `new BookingOverviewViewModel(_mockBookingRepo.Object, _mockRoomRepo.Object)` (`BookingOverviewViewModelTests.cs:50`).

**What happens if it is not found**
- The file is loaded with `optional: false` (`MSTestSettings.cs:24`). **`appsettings.json` is not in the repo** — and neither is `appsettingsTemplate.json`, which the README itself says must be committed (`README.md:132`).
- Consequence: `[AssemblyInitialize]` throws `FileNotFoundException`, and a throwing assembly init fails **the whole assembly** — also the 46 pure model tests. In the committed state the suite is **0 % runnable out of the box**, despite the README's "Expected result: 129 tests passed" (`:77`).
- If `appsettings.json` exists but the server does not, the 40 repo tests fail with connection errors.

**Is there cleanup? Uneven, and not reliable:**
- **`BookingRepoTests`** is the only one with a real `[TestCleanup]` (`:56-76`) — but the whole block is wrapped in `try { } catch { }` with `// Ignore cleanup errors` (`:73-74`). Failed cleanup is invisible; rows silently pile up.
- **`GuestRepoTests` and `RoomRepoTests` have no `[TestCleanup]`.** Cleanup is inside each test method (e.g. `GuestRepoTests.cs:57, 84, 124-125`; `RoomRepoTests.cs:54, 123-124`). If an assert fails before that line, the cleanup never runs.
- Five tests do not clean up at all by design.
- **Parallelism makes it worse:** `Parallelize(MethodLevel)` (`MSTestSettings.cs:7`) runs methods concurrently against the same physical DB. `BookingRepoTests.Setup()` creates a room with the hard-coded number `"999"` for every test method and finds it with `GetAll().First(r => r.RoomNumber == "999")` (`:44-53`) — with 12 parallel methods `First(...)` can return another test's room, and `Cleanup` then deletes someone else's `_testRoomId`. Same pattern in `RoomRepoTests` (`TEST101`, `TESTAVAIL`, `TESTFLOOR5`). The tests are neither *Independent* nor *Repeatable*, despite the FIRST claim in every file header.

**Borderline case:** `NewBookingViewModelTests` construct the real `NewBookingViewModel()` without mocks (`:23, 208, 223`). `CheckOutDate_BeforeCheckIn_SetsErrorMessage` (`:264-277`) itself notes that "UpdateAvailableRooms is called automatically when dates change" (`:271`) — if that routine uses a repo internally, these 20 tests also hit the database indirectly. **Treat the file as DB-suspect.** (Confirmed by analyse:viewmodels: `NewBookingViewModel`'s ctor calls `LoadAllAvailableRooms()` → `IRoomRepo.GetAllByAvailability()`.)

### Rules the tests prove

**The Booking domain (model)**

- **T-01** — A booking must have a start date. → `Validate_MissingStartDate_ReturnsError`, `Models/BookingTests.cs:41`
- **T-02** — A booking must have an end date. → `Validate_MissingEndDate_ReturnsError`, `:61`
- **T-03** — The end date must be after the start date. → `Validate_EndDateBeforeStartDate_ReturnsError`, `:81`
- **T-04** — The start date must not be in the past. → `Validate_StartDateInPast_ReturnsError`, `:101`
- **T-05** — A booking must have a room linked. → `Validate_MissingRoom_ReturnsError`, `:121`
- **T-06** — A booking must have a guest linked. → `Validate_MissingGuest_ReturnsError`, `:142`
- **T-07** — Check-out must be after check-in. → `Validate_CheckOutBeforeCheckIn_ReturnsError`, `:163`
- **T-08** — On editing the same date rules apply via `ValidateEdit(start, end)`. → `ValidateEdit_EndDateBeforeStartDate_ReturnsError` + `ValidateEdit_StartDateInPast_ReturnsError`, `:206`, `:222`
- **T-09** — Number of nights = the difference in whole days (17th→18th Dec = 1 night), 0 for unfilled dates. → `NumberOfNights_TwoDayBooking_ReturnsOne` + `NumberOfNights_DefaultDates_ReturnsZero`, `:242`, `:276`
- **T-10** — Booking number = `FLZ-` + ID zero-padded to 6 digits. → `BookingNumber_FormatsCorrectly` (`"FLZ-000123"`), `:293`

**The Guest domain (model)**

- **T-11** — First name, last name, phone number and country are required. → `Models/GuestTests.cs:75, 117, 201, 222`
- **T-12** — Whitespace counts as empty. → `Validate_WhitespaceFirstName_ReturnsError`, `:96`
- **T-13** — E-mail must contain both `@` and `.`. → `Validate_EmailWithoutAtSign_ReturnsError` + `Validate_EmailWithoutDot_ReturnsError`, `:159`, `:180`
- **T-14** — Passport number is optional; `null` gives no error. → `Validate_PassportNumberOptional_NoError`, `:263`
- **T-15** — Validation collects all errors in one list. → `Validate_MultipleErrors_ReturnsAllErrors` (expects ≥ 5), `:243`

**The Room domain (model)**

- **T-16** — Room number (also against whitespace) and room size are required. → `Models/RoomTests.cs:75, 95, 155`
- **T-17** — Floor must be > 0. → `Validate_FloorZero_ReturnsError` + `Validate_NegativeFloor_ReturnsError`, `:115`, `:135`
- **T-18** — Capacity must be > 0. → `Validate_CapacityZero_ReturnsError`, `:175`
- **T-19** — A new room is `Available` and has an empty room number. → `Constructor_Parameterless_CreatesEmptyRoom`, `:39`

**The booking state machine (ViewModel — the most reusable group)**

- **T-20** — Only a `Pending` booking can be confirmed. → `CanExecuteConfirm_*`, `ViewModels/BookingOverviewViewModelTests.cs:36, 61, 86`
- **T-21** — Check-in is allowed from both `Pending` and `Confirmed` — confirmation is not a precondition. → `:133`, `:159`
- **T-22** — Check-in is not possible before the start date. → `CanExecuteCheckIn_BeforeStartDate_ReturnsFalse`, `:185`
- **T-23** — No check-in on an already checked-in or cancelled booking. → `:211`, `:237`
- **T-24** — Check-out requires status `CheckedIn`. → `:266`, `:293`, `:319`
- **T-25** — Cancellation allowed from `Pending`/`Confirmed`, not after check-in. → `:350, 375, 400, 426`
- **T-26** — Editing allowed from `Pending`/`Confirmed`, locked after check-in/out. → `:455, 480, 505, 531`
- **T-27** — No command is active without a selected booking. → `:112`
- **T-28** — The overview hides cancelled bookings. → `FilterBookings_ExcludesCancelledBookings`, `:562`
- **T-29** — Free-text search filters on the guest's name. → `:585`
- **T-30** — When booking for an existing guest, all guest fields are prefilled. → `Constructor_WithGuest_LoadsGuestData`, `ViewModels/NewBookingViewModelTests.cs:193`
- **T-31** — Check-out before check-in gives an error message immediately. → `:264`

**The repository contract (requires DB)**

- **T-32** — `Create`/`Update` with `null` throws `ArgumentNullException`. → `Repositories/BookingRepoTests.cs:110`, `:305`
- **T-33** — Updating/deleting a non-existent ID throws `ArgumentException`. → `:276`, `:331`
- **T-34** — `GetById` on an unknown ID returns `null` — booking, guest and room. → `BookingRepoTests.cs:192`, `GuestRepoTests.cs:156`, `RoomRepoTests.cs:157`
- **T-35** — `BookingRepo.GetById` eager-loads `Room` and `Guest`. → `:356`
- **T-36** — The repo validates the input model before writing (an invalid `Room` is rejected on create and update). → `RoomRepoTests.cs:58`, `:340`
- **T-37** — `GetAllByAvailability()` returns only `Available`. → `:170`
- **T-38** — `GetRoomsFromCriteria` treats `null` as "no filter". → `:324`, `:294`
- **T-39** — Each filter criterion is excluding, not just including. → `:213` (assert `:231`)
- **T-40** — `GetAllByName` searches across first and last name and matches a substring. → `GuestRepoTests.cs:219`
- **T-41** — A passport number can be removed again on update (set to `NULL`, not an empty string). → `:286`

### Boilerplate / low value

About 45 of the 128 tests are not worth porting:

- **Property get/set tests** (`NewBookingViewModelTests.cs:31-149`, 8 of them) — test C#'s auto-property mechanics. They do not even verify `PropertyChanged`, which is otherwise the point of an MVVM VM.
- **"Is it non-null?" tests**: `SaveBookingCommand_IsNotNull` (`:236`), `NewBookingRoomList_IsInitialized` (`:284`), `ErrorMessage_InitialValue_IsEmpty` (`:166`), `SelectedRoom_InitialValue_IsNull` (`:152`), `Constructor_Parameterless_CreatesEmptyGuest` (`GuestTests.cs:41`).
- **`CheckInDate_Null_NoErrorMessage`** (`:250`) — empty Act.
- **Duplicated model tests**: `RoomSize_AcceptsSingle/Double/Suite` (`RoomTests.cs:278, 297, 316`) are three identical copies; `Status_CanBeSetTo*` (`:248, 261`) test enum assignment.
- **`GuestData_CreatesValidGuest_*`** (`NewBookingViewModelTests.cs:298, 325`) — the VM is not part of what is tested; it is `GuestTests` by a detour.
- **"PreservesData" roundtrip tests** (`GuestRepoTests.cs:344, 374`; `RoomRepoTests.cs:454, 401`) — test ADO.NET mapping, cost 4-6 DB round trips each.
- **`GetAll_ReturnsAll*`** (`BookingRepoTests.cs:135`, `GuestRepoTests.cs:92`, `RoomRepoTests.cs:89`) — assert `Count >= 2` against a shared DB, can in practice not fail.

### Test setup

- **Framework:** MSTest, meta package **4.0.1** (`.csproj:12`). Target framework `net8.0-windows` — ties the suite to Windows-only CI, because it references the WPF project directly.
- **Mocking:** Moq **4.20.72** (`.csproj:11`). **Exactly one place really mocks:** `BookingOverviewViewModelTests.cs:19-20, 27-28`. No other test file imports Moq. No `Verify(...)` anywhere — mocks are used only as stubs.
- **Isolation strategy in three layers:** pure units (46 tests) → mocked dependencies (22) → integration against a physical DB (40) + 20 in the grey zone.
- **AAA discipline: very high.** Practically all 128 tests have explicit `// Arrange` / `// Act` / `// Assert`. Naming follows `Method_Scenario_ExpectedResult`. Almost all asserts have an explanatory message.
- **Deviations:** (a) exception tests use a manual `bool exceptionThrown` + try/catch instead of `Assert.ThrowsException<T>` — 8 places, e.g. `BookingRepoTests.cs:114-127`; a wrong exception type is not caught as such. (b) Cleanup is in a fourth `// Cleanup` section after Assert. (c) Several repo tests have Act and Assert interleaved.
- **No `[DataRow]`/`[DataTestMethod]`** anywhere — all variants are written out in full length, which explains why 3,195 lines hold only 128 tests. No test helpers or builders.

### Unclear/contradictory (tests)

1. **The test count does not match.** The README claims 129; there are 128. All eight per-file numbers in the README (`:120-130`) deviate. The README was written from a different version of the code.
2. **Two positive tests appear to have been deleted.** `BookingRepoTests.cs:325-330` has the heading `DELETE TESTS` followed by a gap; only `Delete_NonExistentBooking` is left. Same in `RoomRepoTests.cs:333-339` (`UPDATE TESTS`). **There is no test proving that a booking can be deleted or a room updated successfully.**
3. **Package versions in the README do not match `.csproj`** (MSTest 4.0.2/Moq 4.20.70 vs. 4.0.1/4.20.72).
4. **`Microsoft.Extensions.Configuration*` is not referenced in `.csproj`** — compiles only via a transitive dependency through the project reference. Hidden and fragile.
5. **`appsettingsTemplate.json` is missing** — the setup instruction cannot be followed as written.
6. **The FIRST claim is self-contradictory** — "< 5 seconds total" against 40 integration tests with `Parallelize(MethodLevel)` and hard-coded keys.
7. **The README says "Model and ViewModel tests run without database dependencies"** (`:201`) — technically true for the test code, practically false because of `optional: false`.
8. **The README is partly a pasted chat conversation** (`:209-222`) and refers to four documents that do not exist. Cannot be used as an authoritative source.
9. **`using System.Windows.Media.Media3D;` in `MSTestSettings.cs:5`** is irrelevant and locks in `net8.0-windows`.
10. **`Nullable enable` against uninitialised fields** — CS8618 warnings everywhere.
11. **`DatabaseConfig.ConnectionString` is a static mutable global** — with parallel execution, tests cannot point at different databases.
12. **Unresolved: is `NewBookingViewModel` DB-free?** Should be verified before assuming the 20 tests can run without SQL Server.
13. **Contradiction between T-20 and T-21** — a `Pending` booking can be checked in without confirmation. A deliberate walk-in rule or a missing rule? Must be clarified before the state machine is refactored.
14. **`GetAllByName_PartialMatch` is self-sabotaging** — searches for `"Test"` in a shared DB where dozens of tests insert guests named "Test".
