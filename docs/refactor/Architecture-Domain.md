# Architecture-Domain.md — appendix

> Full output from the agent `design:domain` (Phase 1). **Appendix.** `Architecture.md` takes precedence where they contradict each other — see especially the rulings A-01 to A-15.

# Domain design — `NFHotel.Domain`

> Phase 1, `design:domain`. Read-only design document. No code written.
> Input: `BusinessRules.md` (126 rules), `Analysis.md` §1, `Conventions.md`, `Decisions.md`, `Phase1-Scope.md`.
> Premises B-01..B-07 are taken as given, unchanged.

---

## 0. Project structure

Feature-first, not type-first (Conventions.md: *"Organise by feature first"*, *"Keep namespaces identical to the folder structure"*, *"1 public class per file"*).

```
NFHotel.Domain/
├── Common/
│   ├── DateRange.cs                 // value object
│   ├── DomainException.cs           // base
│   └── InvalidStateTransitionException.cs
├── Bookings/
│   ├── Booking.cs
│   ├── BookingStatus.cs
│   └── BookingRules.cs              // pure functions, no state
├── Guests/
│   ├── Guest.cs
│   └── GuestRules.cs
└── Rooms/
    ├── Room.cs
    ├── RoomStatus.cs                // bookability
    ├── HousekeepingStatus.cs        // cleaning process
    ├── RoomSize.cs
    └── RoomRules.cs
```

> Note 2026-10-16: this layout was the Phase 1 design. Since ADR-0001 the folders are `Entities`, `ValueObjects`, `Repositories`, `Errors` and `Rules`; see `Architecture.md` section 2.

Namespaces: `NFHotel.Domain.Bookings` etc. No `using` towards other projects — no NuGet packages at all. `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>` recommended for exactly this project.

**Enforcement of "no dependencies":** no `DateTime.Now`/`DateTimeOffset.UtcNow` anywhere in Domain. All rules that need "today" get it as a parameter. It is the only way the state machine can be unit-tested deterministically, and it is a direct consequence of Conventions.md's requirement that Domain must not know the framework/outside world.

---

## 1. Enums

### 1.1 `BookingStatus` — unchanged (B-06)

```csharp
namespace NFHotel.Domain.Bookings;

/// <summary>
/// The booking's lifecycle. The ordinal values 0-4 are kept from the old system,
/// because they already exist as int in existing data (cf. Phase1-Scope item 4).
/// The values must never be renumbered without a data migration.
/// </summary>
public enum BookingStatus
{
    /// <summary>Created, but not payment-guaranteed. Initial status of every booking (BR-01).</summary>
    Pending = 0,

    /// <summary>Payment guaranteed. Is NOT a precondition for check-in (B-01).</summary>
    Confirmed = 1,

    /// <summary>The guest has checked in.</summary>
    CheckedIn = 2,

    /// <summary>The guest has checked out. Does not release the room in the period (B-02).</summary>
    CheckedOut = 3,

    /// <summary>Cancelled. Soft delete — the row is never deleted (BR-13). The only status that releases the room (B-02).</summary>
    Cancelled = 4
}
```

### 1.2 `RoomStatus` — bookability (B-04)

The ordinal values 0-2 are kept from `Models/Enums/RoomStatus.cs`. Only the **meaning** is narrowed: the enum now answers solely the question *"may the room be let?"*. It costs zero data migration, and avoiding exactly that is the whole point of F-01.

```csharp
namespace NFHotel.Domain.Rooms;

/// <summary>
/// The room's bookability — solely what prevents letting.
/// Cleaning state belongs in <see cref="HousekeepingStatus"/> (B-04).
/// The ordinal values 0-2 are kept from the old system.
/// </summary>
public enum RoomStatus
{
    /// <summary>Can be booked. Says nothing about whether the room is clean right now.</summary>
    Available = 0,

    /// <summary>Taken out of operation indefinitely (renovation, converted to storage). An administrative decision.</summary>
    OutOfService = 1,

    /// <summary>Temporarily blocked by a fault that makes the room uninhabitable. Expected to return to Available.</summary>
    Maintenance = 2
}
```

**What has been dropped from the old XAML dropdown (BR-126 is resolved here):** `Cleaning` was never a bookability state and moves to `HousekeepingStatus`. `Maintanance` was a typo for `Maintenance`. `Disabled` is semantically identical to `OutOfService` and gets no value of its own. That leaves one set of allowed values, and the UI can bind directly to the enum instead of to a hard-coded string list.

### 1.3 `HousekeepingStatus` — cleaning and service process (B-04, D-10)

A new enum. There is no existing data to consider, so the numbering is free; I start at 0 = `Clean`, so `default(HousekeepingStatus)` and the database's `DEFAULT 0` mean the same as `RoomStatus.Available`: "nothing in the way".

```csharp
namespace NFHotel.Domain.Rooms;

/// <summary>
/// The room's cleaning and service state. Updated by cleaning staff and
/// service technicians (D-10). Does NOT affect bookability — a room can be
/// <see cref="RoomStatus.Available"/> and at the same time await cleaning (B-04).
/// </summary>
public enum HousekeepingStatus
{
    /// <summary>Clean and ready for a guest. Initial value for a newly created room.</summary>
    Clean = 0,

    /// <summary>Occupied room awaiting daily cleaning.</summary>
    DailyCleaningDue = 1,

    /// <summary>The guest has left; the room awaits final cleaning.</summary>
    DepartureCleaningDue = 2,

    /// <summary>The cleaning staff have acknowledged and are working.</summary>
    CleaningInProgress = 3,

    /// <summary>A problem has been reported; the room awaits a service technician.</summary>
    ServiceRequired = 4,

    /// <summary>The service technician has acknowledged and is working.</summary>
    ServiceInProgress = 5
}
```

**Why service is in the same enum as cleaning, and not in `RoomStatus`:**
`ServiceRequired` means "someone has to look at it" — a dripping tap, a bulb that has gone. It does not prevent letting. If the room must be blocked, it is a *separate, deliberate* action: `RoomStatus` is set to `Maintenance`. Exactly that separation is what the old system never got made, and that is why `Available/Maintenance/OutOfService` was incoherent. Two enums, two questions, no overlap.

**Deliberately omitted:** an `Inspected`/approval step for owner/manager. D-10 mentions three activities (daily cleaning, service, final cleaning), not a fourth inspection step. Added as a new enum value when the mobile guild asks for it — new enum values at the end are cheap, renumbering is expensive. See §8.

### 1.4 `RoomSize` — new enum (Phase1-Scope, "cheap insurance")

`RoomSize` is de facto an enum in data (`'Single'/'Double'/'Suite'`) but lives as free text in both C# and SQL (Analysis §1, unclear item 5). Phase1-Scope lists it explicitly as cheap insurance: *"Let `RoomSize` become an enum or lookup, not a free string."* The same work now, migration later.

```csharp
namespace NFHotel.Domain.Rooms;

/// <summary>
/// Room type. Replaces the old free-text field <c>Room.RoomSize</c>.
/// Ordinal values are new — there is no existing int data to preserve.
/// Later becomes the anchor for <c>RoomType</c> with a price (Phase1-Scope, postponed).
/// </summary>
public enum RoomSize
{
    Single = 0,
    Double = 1,
    Suite = 2
}
```

> **Dependency on the infrastructure agent:** existing `NVARCHAR(20)` data must be mapped to int in the migration (`'Single'→0` etc.). It is not my design, but it must be passed on.

---

## 2. Value object: `DateRange`

**Assessment (KISS check, cf. the task's item 3):** yes, it carries its weight. Without it the rule "end date must be strictly after start date" lies in four places in the old system (BR-38, BR-43, BR-88, BR-103), the overlap rule in two places with two different definitions (BR-19 vs. BR-39), and `NumberOfNights` is computed in one place and reimplemented in a converter (BR-109 vs. BR-117). One type gathers them all.

**But it is not persisted as an owned type.** `Booking` keeps `StartDate` and `EndDate` as two `DateOnly` columns exactly as today, and exposes `Period` as a derived value. That gives the rules one home without imposing an owned-type mapping on the EF agent, and without changing column names. The value object is a *place for rules and a parameter type*, not a storage structure.

```csharp
namespace NFHotel.Domain.Common;

/// <summary>
/// A booking period: arrival date (inclusive) to departure date (exclusive).
/// Half-open interval — departure and arrival on the same day is not an overlap (B-03).
/// The type cannot be constructed in an invalid state.
/// </summary>
public readonly record struct DateRange
{
    /// <summary>Arrival date, inclusive.</summary>
    public DateOnly Start { get; }

    /// <summary>Departure date, exclusive. Always strictly greater than <see cref="Start"/>.</summary>
    public DateOnly End { get; }

    /// <summary>
    /// Creates a period.
    /// </summary>
    /// <param name="start">Arrival date.</param>
    /// <param name="end">Departure date. Must be strictly after <paramref name="start"/>.</param>
    /// <exception cref="DomainException">
    /// Thrown if a date is <c>default</c>, or if <paramref name="end"/> is not after
    /// <paramref name="start"/> (0 nights is not allowed) — BR-101, BR-102, BR-103.
    /// </exception>
    public DateRange(DateOnly start, DateOnly end);

    /// <summary>Number of overnight stays. Replaces <c>Booking.NumberOfNights</c> (BR-109, BR-117).</summary>
    public int Nights => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// Half-open overlap test: <c>Start &lt; other.End &amp;&amp; End &gt; other.Start</c> (B-03).
    /// Pure function — needs no database.
    /// </summary>
    public bool Overlaps(DateRange other);

    /// <summary>True if <paramref name="date"/> lies in the interval [Start, End).</summary>
    public bool Contains(DateOnly date);

    /// <summary>True if the period starts on or after <paramref name="today"/> (BR-104, BR-44).</summary>
    public bool StartsOnOrAfter(DateOnly today) => Start >= today;
}
```

`readonly record struct` gives value equality, `ToString()` and immutability for free, without a class allocation per comparison in an overlap check that may run over hundreds of bookings.

**Value objects I deliberately do NOT make:**

| Candidate | Why not |
|---|---|
| `EmailAddress` | BR-93 is exactly "contains `@` and `.`". A VO would tempt one to tighten to a regex — which would break parity with the old system and fail test T-13's siblings. The rule lives in `GuestRules.IsValidEmail`. |
| `PassportNumber` | B-07 says the field must not be a search key and must be encryptable. A VO makes no difference either way — it only adds a layer between the entity and a column that the EF agent has to encrypt. `string?` is right. |
| `RoomNumber` | No rule beyond "not empty" (BR-97). A VO for one whitespace check is ceremony. |
| `Money`/`Price` | There is no price in the domain (BR-31, Analysis §Summary). Added when `RoomType` with a price is built. |

---

## 3. Entities

All three follow the same pattern: private setters, a private parameterless constructor for EF, a static `Create` factory that enforces the invariants, and behaviour methods instead of public setters. That is what makes BR-84 ("the validation is ignored") structurally impossible to repeat.

### 3.1 `Booking`

```csharp
namespace NFHotel.Domain.Bookings;

/// <summary>
/// A reservation of one room for one guest in one period.
/// Aggregate root. Knows only its own fields — rules that require knowledge of
/// other bookings (e.g. overlap against existing ones) belong in the Application layer.
/// </summary>
public sealed class Booking
{
    private Booking() { }   // EF

    /// <summary>Primary key. 0 until the booking is persisted.</summary>
    public int BookingId { get; private set; }

    /// <summary>Arrival date without time of day (B-05).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Departure date without time of day, exclusive (B-05).</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Actual check-in time. Null until check-in (B-05).</summary>
    public DateTimeOffset? CheckInTime { get; private set; }

    /// <summary>Actual check-out time. Null until check-out (B-05).</summary>
    public DateTimeOffset? CheckOutTime { get; private set; }

    /// <summary>The booking's lifecycle state. Changed only via the methods on the class.</summary>
    public BookingStatus Status { get; private set; }

    /// <summary>Foreign key to the room. Always &gt; 0 (BR-105).</summary>
    public int RoomId { get; private set; }

    /// <summary>Foreign key to the guest. Always &gt; 0 (BR-106).</summary>
    public int GuestId { get; private set; }

    /// <summary>Navigation. Only filled when the call asked for it; never use as a guard.</summary>
    public Room? Room { get; private set; }

    /// <summary>Navigation. Only filled when the call asked for it; never use as a guard.</summary>
    public Guest? Guest { get; private set; }

    /// <summary>The booking's period as a value object.</summary>
    public DateRange Period => new(StartDate, EndDate);

    /// <summary>Number of overnight stays (BR-109, BR-117).</summary>
    public int NumberOfNights => Period.Nights;

    /// <summary>Displayed booking number, the format <c>FLZ-000123</c> (BR-109). Derived, never persisted.</summary>
    public string BookingNumber => $"{BookingNumberPrefix}{BookingId:D6}";

    private const string BookingNumberPrefix = "FLZ-";
}
```

**Decisions and reasons per field**

| Field | Before | Now | Why |
|---|---|---|---|
| `BookingID` | `int` | `BookingId` | Conventions.md: one PK naming pattern. Resolves U-19's casing discrepancy consistently across all three entities. |
| `StartDate`/`EndDate` | `DateTime` | `DateOnly` | B-05. Removes U-05 (`DATE` vs. `DATETIME2`) and the whole family of `.Date` comparisons (BR-44, BR-104, BR-108). |
| `CheckInTime`/`CheckOutTime` | `DateTime?` | `DateTimeOffset?` | B-05. An actual timestamp with offset; no ambiguity at daylight saving time. |
| `RoomID`/`GuestID` | `int`, could be 0 if navigation was set (BR-105/106) | `int`, always > 0 | The old "either navigation or FK" rule was a symptom of a missing factory. The domain requires the FK; the navigation is a convenience. |
| `Room`/`Guest` | `Room`/`Guest` (non-nullable, but really null) | `Room?`/`Guest?` | Conventions.md: nullable reference types. The navigation *is* null on lazy/not-included load — the type must tell the truth. |
| `BookingNumber` | derived string | unchanged | U-11: exists only in code. Correct — it is a display identity, not data. |
| `NumberOfNights` | `(End - Start).Days`, 0 if default | `Period.Nights` | `default` can no longer occur, because `DateRange` rejects it. The special case disappears. |

**Factory and behaviour**

```csharp
    /// <summary>
    /// Creates a new booking with status <see cref="BookingStatus.Pending"/> (BR-01, BR-48).
    /// </summary>
    /// <param name="period">The booking period. Must start on or after <paramref name="today"/>.</param>
    /// <param name="roomId">The room's id. Must be greater than 0.</param>
    /// <param name="guestId">The guest's id. Must be greater than 0.</param>
    /// <param name="today">Today's date in the hotel's time zone. Supplied by the caller — Domain does not know the clock.</param>
    /// <returns>A new, valid booking that is not yet persisted.</returns>
    /// <exception cref="DomainException">
    /// On a start date in the past (BR-44, BR-104), a missing room (BR-45, BR-105)
    /// or a missing guest (BR-106).
    /// </exception>
    public static Booking Create(DateRange period, int roomId, int guestId, DateOnly today);

    /// <summary>True if the booking may be confirmed (BR-02, BR-121).</summary>
    public bool CanConfirm => Status == BookingStatus.Pending;

    /// <summary>True if the booking may be checked in on the given date (BR-06). See B-01: Confirmed is not a requirement.</summary>
    public bool CanCheckIn(DateOnly today) =>
        (Status is BookingStatus.Pending or BookingStatus.Confirmed)
        && CheckInTime is null
        && StartDate <= today;

    /// <summary>True if the booking may be checked out (BR-09).</summary>
    public bool CanCheckOut =>
        Status == BookingStatus.CheckedIn && CheckInTime is not null && CheckOutTime is null;

    /// <summary>True if the booking may be cancelled (BR-11, BR-122).</summary>
    public bool CanCancel => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    /// <summary>True if the booking's dates and room may be changed (BR-14, BR-123).</summary>
    public bool CanReschedule => Status is BookingStatus.Pending or BookingStatus.Confirmed;
```

The five `Can*` members are the consolidation BusinessRules.md asks for: BR-02/BR-121, BR-11/BR-122 and BR-14/BR-123 were the same rule written both in the ViewModel and as an XAML trigger. Now each exists in one place, and both the Blazor UI (to disable a button) and the Application service (to reject a call) read from the same source. BR-22 thus becomes trivial — there is nothing to "recalculate", the values are properties.

### 3.2 `Room`

```csharp
namespace NFHotel.Domain.Rooms;

/// <summary>
/// A physical room. Carries two independent states: bookability
/// (<see cref="Status"/>) and cleaning state (<see cref="Housekeeping"/>) — B-04.
/// </summary>
public sealed class Room
{
    private Room() { }   // EF

    /// <summary>Primary key.</summary>
    public int RoomId { get; private set; }

    /// <summary>Room number as text, e.g. "101" or "12B" (BR-97). Resolves U-01 in favour of the C# type.</summary>
    public string RoomNumber { get; private set; } = string.Empty;

    /// <summary>Floor. Must be greater than 0 (BR-98).</summary>
    public int Floor { get; private set; }

    /// <summary>Room type. Was free text; is now an enum.</summary>
    public RoomSize Size { get; private set; }

    /// <summary>Number of persons the room can hold. Must be greater than 0 (BR-100).</summary>
    public int Capacity { get; private set; }

    /// <summary>Bookability. Changed only via <see cref="TakeOutOfService"/>, <see cref="SendToMaintenance"/> and <see cref="ReturnToService"/>.</summary>
    public RoomStatus Status { get; private set; }

    /// <summary>Cleaning and service state. Changed only via the housekeeping methods.</summary>
    public HousekeepingStatus Housekeeping { get; private set; }

    /// <summary>
    /// True if the room may be let at all (BR-110). Depends SOLELY on
    /// <see cref="Status"/> — cleaning state is deliberately not included (B-04).
    /// </summary>
    public bool IsBookable => Status == RoomStatus.Available;
}
```

`RoomSize` → `Size`: the property name `RoomSize` on the type `Room` was a stem repetition (`room.RoomSize`), and the type is now called `RoomSize`. `room.Size` of the type `RoomSize` reads better. The column name is the infrastructure agent's decision.

```csharp
    /// <summary>
    /// Creates a new room. Status is set to <see cref="RoomStatus.Available"/> (BR-81)
    /// and cleaning state to <see cref="HousekeepingStatus.Clean"/>.
    /// </summary>
    /// <exception cref="DomainException">
    /// On an empty room number (BR-97), floor ≤ 0 (BR-98), unknown <paramref name="size"/> (BR-99)
    /// or capacity ≤ 0 (BR-100).
    /// </exception>
    public static Room Create(string roomNumber, int floor, RoomSize size, int capacity);

    /// <summary>Updates the room's master data. Touches neither status nor cleaning state.</summary>
    /// <exception cref="DomainException">The same invariants as <see cref="Create"/>.</exception>
    public void UpdateDetails(string roomNumber, int floor, RoomSize size, int capacity);
```

Note that `Create`/`UpdateDetails` throw instead of returning an error list. That is what makes BR-84 impossible to repeat: there is no path to a persisted, invalid room, and the repo's `Room.Validate()` check (BR-113) can be dropped.

### 3.3 `Guest`

```csharp
namespace NFHotel.Domain.Guests;

/// <summary>
/// A guest. A pure domain entity — deliberately NOT a login/account entity
/// (Phase1-Scope, "cheap insurance").
/// </summary>
public sealed class Guest
{
    private Guest() { }   // EF

    /// <summary>Primary key. Never changed after creation (BR-72).</summary>
    public int GuestId { get; private set; }

    /// <summary>First name. Never empty or only whitespace (BR-91).</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Last name. Never empty or only whitespace (BR-92).</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>E-mail. Always contains both '@' and '.' (BR-93).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Phone number. Never empty; no format requirement (BR-94).</summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Country. Never empty (BR-95).</summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Passport number. Optional and unvalidated (BR-57, U-22).
    /// Treated as sensitive: must not be used as a search or sort key (B-07, D-04).
    /// The domain does not know the encryption — it is the infrastructure's responsibility.
    /// </summary>
    public string? PassportNumber { get; private set; }

    /// <summary>Full name, "First name Last name". Used by the Application layer's search (BR-25, BR-68).</summary>
    public string FullName => $"{FirstName} {LastName}";
}
```

```csharp
    /// <summary>Creates a new guest.</summary>
    /// <exception cref="DomainException">On breach of BR-91..BR-95.</exception>
    public static Guest Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber);

    /// <summary>
    /// Overwrites exactly the six editable fields. <see cref="GuestId"/> is never changed (BR-72).
    /// </summary>
    /// <exception cref="DomainException">On breach of BR-91..BR-95.</exception>
    public void UpdateDetails(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string country,
        string? passportNumber);
```

**`FullName` as a computed property is deliberate.** BR-25 and BR-68 both search in "first name last name"; BR-69 sorts on it. The calculation belongs in Domain, the filtering and sorting in Application. Conventions.md's ban on UI formatting in domain models is not broken: `FullName` is a domain identity, not a display string — there is no culture, no format, no truncation.

**Length limits — new rule N-01.** Analysis §1 notes that C# has no length check against `NVARCHAR(100)`/`(50)`. I propose `public const int MaxNameLength = 100;` etc. on `Guest`/`Room` and a guard in `Create`/`UpdateDetails`, so a too-long value fails in Domain instead of as a database exception. It is a **new rule without a BR id** — flagged here, so the ledger audit in Phase 3 does not book it as an unexplained deviation. The constants can be reused by the EF configuration (the infrastructure agent's choice).

---

## 4. Rule classes (pure functions)

```csharp
namespace NFHotel.Domain.Bookings;

/// <summary>
/// Stateless booking rules. Pure functions with no database or time dependency,
/// so they can be unit-tested directly (B-03).
/// </summary>
public static class BookingRules
{
    /// <summary>
    /// Half-open overlap test between two periods: <c>aStart &lt; bEnd &amp;&amp; aEnd &gt; bStart</c>.
    /// Departure and arrival on the same day is NOT an overlap (B-03, BR-39).
    /// </summary>
    public static bool Overlaps(DateRange a, DateRange b) => a.Overlaps(b);

    /// <summary>
    /// True if a booking with the given status blocks the room in its period.
    /// Only <see cref="BookingStatus.Cancelled"/> releases the room — <see cref="BookingStatus.CheckedOut"/>
    /// does not (B-02, resolves the contradiction BR-19 vs. BR-39 in favour of BR-39).
    /// </summary>
    public static bool BlocksRoom(BookingStatus status) => status != BookingStatus.Cancelled;

    /// <summary>
    /// True if an existing booking conflicts with the desired period on the same room.
    /// The caller is responsible for excluding the booking itself on editing (BR-19).
    /// </summary>
    public static bool Conflicts(Booking existing, int roomId, DateRange desiredPeriod) =>
        existing.RoomId == roomId
        && BlocksRoom(existing.Status)
        && Overlaps(existing.Period, desiredPeriod);
}
```

`Conflicts` is the one function the Application service must call per candidate booking. The service fetches the bookings, Domain decides whether they are in the way. Thus the overlap definition exists in exactly one place in the whole code base — against two mutually disagreeing places in the old system.

```csharp
namespace NFHotel.Domain.Guests;

/// <summary>
/// Guest validation. <see cref="Guest.Create"/> fails fast on the first breach;
/// <see cref="Validate"/> collects ALL errors, so a form can show them at once (BR-60, BR-96).
/// Both paths use the same private predicates — the rule exists in one place.
/// </summary>
public static class GuestRules
{
    /// <summary>Collects all validation errors. An empty list means valid.</summary>
    /// <returns>Error messages in field order: first name, last name, e-mail, phone, country.</returns>
    public static IReadOnlyList<string> Validate(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        string? country);

    /// <summary>
    /// True if the e-mail contains both '@' and '.' (BR-93, BR-55).
    /// Deliberately not a regex — that would break parity with the old system.
    /// </summary>
    public static bool IsValidEmail(string? email);
}
```

The error messages in `Validate` are the English strings from BR-52..BR-56 ("First name is required." etc.), unchanged. **Open point:** user-facing texts in Domain are strictly speaking a localisation decision that belongs in the UI. I keep them to preserve parity and because the old system had them in `Guest.Validate()`. See §8.

`RoomRules` becomes correspondingly small: `IsValidRoomNumber`, `IsValidFloor`, `IsValidCapacity`, `Validate(...)`.

---

## 5. State machines

### 5.1 `BookingStatus`

```
                    ┌──────────── Cancel() ────────────┐
                    │                                  ▼
[Create] ──▶ Pending ──Confirm()──▶ Confirmed ──Cancel()──▶ Cancelled ●
              │  ▲                    │  ▲
              │  └─ Reschedule()      │  └─ Reschedule()
              │                       │
              └──CheckIn()──▶ CheckedIn ◀──CheckIn()──┘      (B-01: Confirmed may be skipped)
                                 │
                            CheckOut()
                                 ▼
                            CheckedOut ●
```

```csharp
    /// <summary>
    /// Confirms the booking — payment is guaranteed (BR-02, BR-03, BR-05).
    /// Note: confirmation is not a precondition for check-in (B-01).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is not exactly <see cref="BookingStatus.Pending"/>.
    /// </exception>
    public void Confirm();

    /// <summary>
    /// Checks the guest in (BR-06, BR-07). Allowed from both Pending and Confirmed (B-01, T-21).
    /// </summary>
    /// <param name="occurredAt">The actual check-in time.</param>
    /// <param name="today">
    /// Today's date in the hotel's time zone. Supplied separately, because the conversion from a
    /// timestamp to a local calendar date is a time zone policy that belongs in the Application layer.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is neither Pending nor Confirmed, if <see cref="CheckInTime"/>
    /// is already set, or if <see cref="StartDate"/> is after <paramref name="today"/>.
    /// </exception>
    public void CheckIn(DateTimeOffset occurredAt, DateOnly today);

    /// <summary>
    /// Checks the guest out (BR-09, BR-10). Does NOT release the room in the period (B-02).
    /// </summary>
    /// <param name="occurredAt">The actual check-out time. Must be strictly after <see cref="CheckInTime"/> (BR-107).</param>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is not <see cref="BookingStatus.CheckedIn"/>, if <see cref="CheckOutTime"/>
    /// is already set, or if <paramref name="occurredAt"/> is not after <see cref="CheckInTime"/>.
    /// </exception>
    public void CheckOut(DateTimeOffset occurredAt);

    /// <summary>
    /// Cancels the booking (BR-11, BR-13). Soft delete — the row is never deleted.
    /// The only transition that releases the room in the period (B-02).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown if the status is neither Pending nor Confirmed. A CheckedIn/CheckedOut booking cannot be cancelled.
    /// </exception>
    public void Cancel();

    /// <summary>
    /// Moves the booking to a new period and/or a new room (BR-14, BR-17, BR-18, BR-20).
    /// Does NOT check overlap against other bookings — that requires knowledge of other
    /// bookings and therefore belongs in the Application layer (BR-19).
    /// </summary>
    /// <param name="newPeriod">The new period. Must start on or after <paramref name="today"/> (BR-108).</param>
    /// <param name="newRoomId">The new room's id. Must be greater than 0.</param>
    /// <param name="today">Today's date in the hotel's time zone.</param>
    /// <exception cref="InvalidStateTransitionException">Thrown if the status is neither Pending nor Confirmed.</exception>
    /// <exception cref="DomainException">Thrown if the new period starts in the past.</exception>
    public void Reschedule(DateRange newPeriod, int newRoomId, DateOnly today);
```

**Guard table**

| Method | Status requirement | Other guards | BR |
|---|---|---|---|
| `Confirm()` | `Pending` | — | BR-02, BR-03, BR-05, BR-121 |
| `CheckIn(...)` | `Pending` ∨ `Confirmed` | `CheckInTime is null`; `StartDate <= today` | BR-06, BR-07, BR-08 |
| `CheckOut(...)` | `CheckedIn` | `CheckInTime is not null`; `CheckOutTime is null`; `occurredAt > CheckInTime` | BR-09, BR-10, BR-107 |
| `Cancel()` | `Pending` ∨ `Confirmed` | — | BR-11, BR-13, BR-122 |
| `Reschedule(...)` | `Pending` ∨ `Confirmed` | `newPeriod.Start >= today`; `newRoomId > 0` | BR-14, BR-17, BR-18, BR-20, BR-108, BR-123 |

No method can be reached from `CheckedOut` or `Cancelled` — both are final statuses. That is enforced by the status requirements, not by a separate table.

**Note on BR-03/BR-08/BR-17:** in the old system the rule is rechecked both in `CanExecute` and in the action itself. In the new model the action *is* checked — `Confirm()` throws if it is not allowed. The `Can*` property exists so the UI can disable the button in advance, but it is not the safeguard. The double implementation thus disappears without weakening the rule.

### 5.2 `HousekeepingStatus`

```
        MarkDailyCleaningDue()
   Clean ──────────────────▶ DailyCleaningDue ──┐
     ▲                                          │ StartCleaning()
     │  MarkDepartureCleaningDue()              ▼
     ├────────────────────▶ DepartureCleaningDue ──▶ CleaningInProgress
     │                                                     │
     │  CompleteCleaning() ◀───────────────────────────────┘
     │
     │  CompleteService()
     └──────────── ServiceInProgress ◀──StartService()── ServiceRequired
                                                              ▲
                                    ReportServiceNeeded() ────┘  (from all other states)
```

```csharp
    /// <summary>Marks that the occupied room awaits daily cleaning (D-10).</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if the room is under cleaning or service.</exception>
    public void MarkDailyCleaningDue();

    /// <summary>
    /// Marks that the room awaits final cleaning after departure (D-10).
    /// Typically called by the Application layer immediately after <see cref="Booking.CheckOut"/>.
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if the room is under cleaning or service.</exception>
    public void MarkDepartureCleaningDue();

    /// <summary>The cleaning staff acknowledge and start work.</summary>
    /// <exception cref="InvalidStateTransitionException">
    /// Thrown unless the status is DailyCleaningDue or DepartureCleaningDue.
    /// </exception>
    public void StartCleaning();

    /// <summary>The cleaning is finished; the room is ready.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown unless the status is CleaningInProgress.</exception>
    public void CompleteCleaning();

    /// <summary>
    /// Reports that the room must be seen by a service technician.
    /// Does NOT block the room for booking — that requires <see cref="SendToMaintenance"/> (B-04).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if service is already in progress.</exception>
    public void ReportServiceNeeded();

    /// <summary>The service technician acknowledges and starts work.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown unless the status is ServiceRequired.</exception>
    public void StartService();

    /// <summary>
    /// The service is finished.
    /// </summary>
    /// <param name="requiresCleaning">
    /// True if the work has left the room dirty; then sets the status to DepartureCleaningDue
    /// instead of Clean.
    /// </param>
    /// <exception cref="InvalidStateTransitionException">Thrown unless the status is ServiceInProgress.</exception>
    public void CompleteService(bool requiresCleaning);
```

**Guard table**

| Method | Allowed from | To | Role (D-03) |
|---|---|---|---|
| `MarkDailyCleaningDue()` | `Clean`, `DailyCleaningDue`, `ServiceRequired` | `DailyCleaningDue` | System/manager |
| `MarkDepartureCleaningDue()` | `Clean`, `DailyCleaningDue`, `DepartureCleaningDue`, `ServiceRequired` | `DepartureCleaningDue` | System/manager |
| `StartCleaning()` | `DailyCleaningDue`, `DepartureCleaningDue` | `CleaningInProgress` | Cleaning staff |
| `CompleteCleaning()` | `CleaningInProgress` | `Clean` | Cleaning staff |
| `ReportServiceNeeded()` | everything except `ServiceInProgress` | `ServiceRequired` | Cleaning, technician, manager |
| `StartService()` | `ServiceRequired` | `ServiceInProgress` | Service technician |
| `CompleteService(bool)` | `ServiceInProgress` | `Clean` \| `DepartureCleaningDue` | Service technician |

`MarkDailyCleaningDue`/`MarkDepartureCleaningDue` are idempotent from their own state — the mobile app has no offline queue (D-11), but network retries can still give duplicate calls.

### 5.3 `RoomStatus`

```csharp
    /// <summary>Takes the room out of operation indefinitely. The room cannot be booked.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if the room is already OutOfService.</exception>
    public void TakeOutOfService();

    /// <summary>Blocks the room temporarily because of a fault that makes it uninhabitable.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if the room is already Maintenance.</exception>
    public void SendToMaintenance();

    /// <summary>
    /// Makes the room bookable again. Does not touch the cleaning state — a room can be
    /// Available and at the same time await cleaning (B-04).
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if the room is already Available.</exception>
    public void ReturnToService();
```

All three transitions are freely allowed between each other (only "no change" is rejected). There is no business rule that forbids e.g. `OutOfService → Maintenance`.

**The central invariant of all of B-04, and the test that proves it:**
`Room.IsBookable` reads only `Status`. A test case should be called something like `IsBookable_ShouldBeTrue_WhenStatusIsAvailableAndHousekeepingIsDepartureCleaningDue()`. If that test ever fails, the split has been undone by accident.

---

## 6. Rule placement — all 126 BR ids

### 6.1 Rules that belong in Domain

| BR id | Where in Domain | Method/member |
|---|---|---|
| BR-01 | `Booking` | `Booking.Create` (sets `Pending`) |
| BR-02 | `Booking` | `CanConfirm` |
| BR-03 | `Booking` | `Confirm()` guard |
| BR-05 (the status part) | `Booking` | `Confirm()` |
| BR-06 | `Booking` | `CanCheckIn(DateOnly)` |
| BR-07 | `Booking` | `CheckIn(DateTimeOffset, DateOnly)` |
| BR-08 (the guard part) | `Booking` | `CheckIn(...)` guard |
| BR-09 | `Booking` | `CanCheckOut` |
| BR-10 (the status part) | `Booking` | `CheckOut(DateTimeOffset)` |
| BR-11 | `Booking` | `CanCancel` |
| BR-13 | `Booking` | `Cancel()` |
| BR-14 | `Booking` | `CanReschedule` |
| BR-17 | `Booking` | `Reschedule(...)` guard |
| BR-18 | `Booking` / `DateRange` | `Reschedule(...)` + `DateRange` ctor |
| BR-20 (the mutation part) | `Booking` | `Reschedule(...)` |
| BR-22 (the predicates) | `Booking` | `CanConfirm`, `CanCheckIn`, `CanCheckOut`, `CanCancel`, `CanReschedule` |
| BR-38 | `DateRange` | ctor invariant (`End > Start`) |
| BR-39 (the predicate) | `BookingRules` | `Overlaps`, `BlocksRoom`, `Conflicts` |
| BR-43 | `DateRange` | ctor invariant |
| BR-44 | `Booking` | `Create(...)` guard (`period.StartsOnOrAfter(today)`) |
| BR-45 | `Booking` | `Create(...)` guard (`roomId > 0`) |
| BR-46 (the rules) | `GuestRules` | `Validate(...)` |
| BR-48 | `Booking` | `Create(...)` |
| BR-49 (the calculations) | `Booking` | `NumberOfNights`, `BookingNumber` |
| BR-52 | `GuestRules` | `Validate(...)` — first name |
| BR-53 | `GuestRules` | `Validate(...)` — last name |
| BR-54 | `GuestRules` | `Validate(...)` — phone |
| BR-55 | `GuestRules` | `IsValidEmail` |
| BR-56 | `GuestRules` | `Validate(...)` — country |
| BR-57 | `Guest` | `PassportNumber` as `string?`, no guard |
| BR-60 (the aggregation) | `GuestRules` | `Validate(...)` returns all errors |
| BR-72 | `Guest` | `UpdateDetails(...)` — `GuestId` has no setter |
| BR-81 | `Room` | `Room.Create(...)` sets `Available` + `Clean` |
| BR-84 | `Room` | Undone structurally: `Create`/`UpdateDetails` throw, the result cannot be ignored |
| BR-88 | `DateRange` | ctor invariant (replaces `DateGreaterThanAttribute`) |
| BR-91 | `GuestRules` / `Guest` | `Validate(...)` + `Create`/`UpdateDetails` guard |
| BR-92 | `GuestRules` / `Guest` | ditto |
| BR-93 | `GuestRules` / `Guest` | `IsValidEmail` |
| BR-94 | `GuestRules` / `Guest` | ditto |
| BR-95 | `GuestRules` / `Guest` | ditto |
| BR-96 | `GuestRules` | `Validate(...)` collects all errors |
| BR-97 | `RoomRules` / `Room` | `IsValidRoomNumber` + `Create`/`UpdateDetails` |
| BR-98 | `RoomRules` / `Room` | `IsValidFloor` (`floor > 0`) |
| BR-99 | `Room` | `RoomSize` enum + `Enum.IsDefined` guard |
| BR-100 | `RoomRules` / `Room` | `IsValidCapacity` (`capacity > 0`) |
| BR-101 | `DateRange` | ctor rejects `default(DateOnly)` |
| BR-102 | `DateRange` | ditto |
| BR-103 | `DateRange` | ctor invariant (`End > Start`) |
| BR-104 | `Booking` | `Create(...)` guard |
| BR-105 | `Booking` | `Create(...)` guard (`roomId > 0`) |
| BR-106 | `Booking` | `Create(...)` guard (`guestId > 0`) |
| BR-107 | `Booking` | `CheckOut(...)` guard |
| BR-108 | `Booking` | `Reschedule(...)` guard |
| BR-109 | `Booking` / `DateRange` | `NumberOfNights` (`DateRange.Nights`), `BookingNumber` |
| BR-110 (the predicate) | `Room` | `IsBookable` |
| BR-113 | `Room` | Moved from repo to `Create`/`UpdateDetails` — the repo no longer has to validate |
| BR-117 | `DateRange` | `Nights` (same calculation as BR-109; the duplicate consolidated) |
| BR-121 | `Booking` | `CanConfirm` (duplicate of BR-02, consolidated) |
| BR-122 | `Booking` | `CanCancel` (duplicate of BR-11, consolidated) |
| BR-123 | `Booking` | `CanReschedule` (duplicate of BR-14, consolidated) |
| BR-126 | `RoomStatus` + `HousekeepingStatus` | Resolved by the split (B-04); one set of allowed values, UI binds to the enum |

**64 rules in Domain.**

### 6.2 Rules that do NOT belong in Domain

| BR id | Belongs in | Why |
|---|---|---|
| BR-04 | UI | Confirmation dialog. Pure user interaction. |
| BR-05 (persist/reload) | Application | Repository orchestration. |
| BR-08 (no dialog) | UI | Absence of a dialog is a UI decision. |
| BR-10 (persist) | Application | Ditto. |
| BR-12 | UI | Dialog text and Yes/No. |
| BR-15 | UI | Prefilling of the edit form. |
| BR-16 | UI | `IsEditMode` is presentation state. |
| BR-19 | Application (calls `BookingRules.Conflicts`) | Requires knowledge of *other* bookings. Domain supplies the predicate, the service supplies the data set. **B-02: the `CheckedOut` exception is removed.** |
| BR-20 (persist/reload) | Application | Ditto. |
| BR-21 | UI | Leaves edit mode. |
| BR-22 (recalculation) | UI | In Blazor there is no `CanExecute` to raise — properties are read at render. |
| BR-23 | Application | Query filter over many bookings. |
| BR-24 | Application | Period filtering of a list. |
| BR-25 | Application | Search over many bookings. Domain supplies `Guest.FullName` and `Room.RoomNumber`. |
| BR-26 | UI | Automatic switch of display period. |
| BR-27 | UI | Calendar calculation. |
| BR-28 | UI | Forward/back navigation. |
| BR-29 | UI | Column count. |
| BR-30 | UI | Heading formatting, culture dependent. Conventions.md forbids UI formatting in Domain. |
| BR-31 | Nowhere (yet) | Revenue is never calculated; there is no price in the domain. Reappears with `RoomType`+price (postponed). |
| BR-32 | UI | Sorting of a table view. |
| BR-33 | UI | Reload after a closed dialog. |
| BR-34 | Application | Availability query. Domain supplies `Room.IsBookable` + `BookingRules.Conflicts`. |
| BR-35 | UI | Prefilling of a form. |
| BR-36 | UI | Reactive recalculation on field change. |
| BR-37 | UI | Empty state for the room list. |
| BR-40 | UI | Error message on zero free rooms. |
| BR-41 | Application/UI | "Date is filled in" is form validation — with `DateOnly` emptiness exists only in the input model. The Domain counterpart is BR-101. |
| BR-42 | Application/UI | Ditto. (Domain counterpart: BR-102) |
| BR-47 | Application | Decides whether the guest is new; requires a repository. |
| BR-49 (receipt text) | UI | Formatting of the message. |
| BR-50 | UI | 2000 ms auto-close. |
| BR-51 | Application/UI | Exception-to-message translation. |
| BR-58 | UI | Button activation. |
| BR-59 | UI | Revalidation on every change. |
| BR-60 (the display) | UI | Domain supplies the list, UI shows it. |
| BR-61 | UI | Confirmation dialog. |
| BR-62 | Application | Chooses create vs. update; requires a repository. |
| BR-63 | UI | Title and button texts. |
| BR-64 | UI | Cancel without saving. |
| BR-65 | UI | Button guard on a selected guest. |
| BR-66 | UI | Editing on a copy. In Blazor it becomes an edit model — not the domain's responsibility. |
| BR-67 | UI | Error message on missing selection. |
| BR-68 | Application | Search over many guests (AND logic on words). |
| BR-69 | Application | Sorting of a list. |
| BR-70 | Application | Empty search shows all. |
| BR-71 | UI | "Clear guest" resets the form. |
| BR-73 | UI | Double guard on event. |
| BR-74 | Application | Composite room filter over many rooms. |
| BR-75 | Application | Derives distinct filter values from the data set. |
| BR-76 | UI | Reset of filter. |
| BR-77 | UI | Warning dialog on deletion. |
| BR-78 | Application | Exception handling from the repository. |
| BR-79 | UI | Null-parameter guard in command. |
| BR-80 | UI | Reload + closing of side panel. |
| BR-82 | Application | Create vs. update based on id. |
| BR-83 | UI | Title and button texts. |
| BR-85 | UI | Start screen. |
| BR-86 | UI | Navigation and component lifetime. |
| BR-87 | Lapses | `RelayCommand` does not exist in Blazor. |
| BR-89 | Lapses | `DateGreaterThanAttribute`'s internal error handling. The attribute is deleted; `DateRange` replaces it. |
| BR-90 | Lapses | "Non-DateTime = valid". With non-nullable `DateOnly` the case cannot occur. |
| BR-110 (the filter) | Infrastructure/Application | The query belongs in the data access layer; the predicate in Domain. |
| BR-111 | Infrastructure | In-memory filter after a full table pull — incidentally to be fixed to a real `WHERE`. |
| BR-112 | Application/Infrastructure | FK guard on deleting a room. Must move away from `catch (SqlException 547)` and up into an explicit query. |
| BR-114 | Application/Infrastructure | Existence check before update/delete. |
| BR-115 | Infrastructure | `GetById` returns null. Repository contract. |
| BR-116 | Infrastructure | SQL name search. |
| BR-118 | UI | Half-day drawing in the calendar. A pure visual convention. |
| BR-119 | UI | Clipping at the period boundary. |
| BR-120 | UI | Filtering of timeline per room. |
| BR-124 | UI | `IsReadOnly` on fields. |
| BR-125 | UI | Button guard. |

**62 rules outside Domain. 64 + 62 = 126.** ✓

---

## 7. Changes relative to the old domain

| What | Before | Now | Why |
|---|---|---|---|
| `Booking.StartDate`/`EndDate` | `DateTime` | `DateOnly` | B-05. Removes U-05 and all `.Date` comparisons. |
| `Booking.CheckInTime`/`CheckOutTime` | `DateTime?` | `DateTimeOffset?` | B-05. Real timestamps with offset. |
| Time source | `DateTime.Now` inside ViewModel/model | `DateOnly today` / `DateTimeOffset occurredAt` as parameters | Domain must not know the outside world; makes the state machine deterministically testable. |
| Overlap rule | Two disagreeing variants (BR-19 excludes `CheckedOut`, BR-39 does not) | One pure function `BookingRules.Overlaps`/`Conflicts` | B-02, B-03. The contradiction is resolved in favour of the strictest. |
| `CheckedOut` releases room | Yes on editing, no on creation | No, never | B-02. Early check-out is handled by shortening `EndDate`. |
| `RoomStatus` | Available/OutOfService/Maintenance, mixed two questions | Only bookability, the same three ordinals | B-04, F-01. Zero data migration, coherent meaning. |
| Cleaning state | Did not exist in the enum; only in the XAML dropdown (`Cleaning`) | New `HousekeepingStatus` with 6 values | B-04, D-10. The mobile app needs the process. |
| Allowed room statuses | Hard-coded string list in XAML, disagreeing with the enum (BR-126) | Enums only | BR-126 resolved. UI binds to the enum. |
| `Room.RoomSize` | `string` free text | `RoomSize` enum | Phase1-Scope "cheap insurance". Makes `RoomType`+price cheap later. |
| `Room.RoomNumber` | `string` in C#, `INT` in SQL | `string` | U-01 resolved in favour of the C# type; supports "12B". |
| PK naming | `Booking.BookingID`, `Booking.RoomID`, `Room.RoomId` | `BookingId`, `RoomId`, `GuestId` everywhere | U-19. Conventions.md: one PK pattern. |
| "Room OR FK" | Error only if both navigation null AND id 0 (BR-105/106) | FK always > 0; navigation is `Room?`/`Guest?` | The old rule was a symptom of a missing factory; the navigation is not a source of validity. |
| Validation | `Validate()` returns `List<string>`, called voluntarily, ignored in BR-84 | Invariants in `Create`/`UpdateDetails` that throw + `*Rules.Validate` for forms | BR-84 and BR-113 become structurally impossible. The repo no longer has to validate. |
| Status change | Public setter, set from ViewModel | `Confirm()`, `CheckIn()`, `CheckOut()`, `Cancel()`, `Reschedule()` with guards | Phase1-Scope item 1. A console app can call the methods and get the rules for free. |
| `Can*` rules | Duplicated in ViewModel and XAML trigger (BR-02/121, 11/122, 14/123) | One property per rule on `Booking` | Consolidation as BusinessRules.md asks for. |
| `DateGreaterThanAttribute` | Reflection-based attribute, used nowhere | Deleted; `DateRange` replaces | Conventions.md: delete dead code. BR-89/BR-90 lapse. |
| `NumberOfNights` | Calculated in model + reimplemented in converter | `DateRange.Nights`, one place | BR-109/BR-117 consolidated. |
| `Guest.PassportNumber` | `string`, always filled in data | `string?`, sensitive, never a search key | B-07, D-04, U-22. |
| Nullability | No NRT discipline | NRT switched on, navigations nullable | Conventions.md. |
| Length checks | None (Analysis §1) | `MaxLength` constants + guards (**new rule N-01**) | Errors are caught in Domain instead of as DB exceptions. Flagged as a new rule. |

---

## 8. Unclear, contradictory or deliberately left to someone else

**D-01 — Error messages in English in Domain.**
`GuestRules.Validate` returns the old English strings ("First name is required."). That preserves parity, but user-facing text in Domain is a localisation decision that really belongs in the UI. The alternative is to return error *codes* and let the UI translate — it is cleaner, but costs ledger parity on BR-52..BR-56 and BR-60. **Must be decided before Phase 2.**

**D-02 — Who marks a room for final cleaning after check-out?**
`Booking.CheckOut()` cannot call `Room.MarkDepartureCleaningDue()` — they are two aggregates, and `Booking.Room` is often null. I have put the coordination in the Application layer. The alternative is a domain event (`BookingCheckedOut`), but that is infrastructure (dispatcher, handlers) that has not been decided, and which Phase1-Scope does not ask for. **Recommendation: Application service now, domain events when/if there is a need for more listeners.**

**D-03 — `Floor > 0` (BR-98) forbids ground floor and basement.**
The rule is kept unchanged, because no U discrepancy or premise dictates otherwise. But a hotel with a reception on the ground floor may have room 001. **Is it a deliberate rule or an overlooked off-by-one?** Should be confirmed with the business.

**D-04 — `Confirmed → Cancelled` after payment.**
BR-11 allows cancellation from `Confirmed`. With B-01, `Confirmed` means "payment guaranteed", so a cancellation from here has a financial consequence (refund). The domain has no price and therefore cannot model it. **The transition is kept as it is today; refund rules are added when `Payment` is built.**

**D-05 — Reversal of an erroneous check-in/check-out does not exist.**
There is no `UndoCheckIn()` in the old system and no BR that asks for it. Staff can in practice end up checking in the wrong booking. **I have not invented the rule** — but it is probably a real need, and `CheckedOut` is today an absolute final status.

**D-06 — Early check-out by shortening `EndDate` (B-02) has no method yet.**
`Reschedule(...)` requires `Pending` ∨ `Confirmed` and therefore cannot be used on a `CheckedIn` booking. B-02 says early check-out is "handled later". If it is to be handled in Phase 2, a `ShortenStay(DateOnly newEndDate)` with status requirement `CheckedIn` is missing. **Not designed, since the premise postpones it.**

**D-07 — `HousekeepingStatus` has no inspection/approval step.**
D-03 gives "Manager/owner" access to the app, which could suggest an approval after final cleaning. D-10 does not mention it. **Deliberately omitted.** Added as `Inspected = 6` when the mobile guild asks for it — new values at the end are free.

**D-08 — Who sets `DailyCleaningDue`, and when?**
An occupied room should presumably be marked every morning. That requires either a scheduled job or a derivation from bookings. **Domain supplies the method; the trigger mechanism is not my decision.**

**D-09 — Personal data deletion (D-05).**
"Personal data is kept for X years, or until the guest asks for deletion." That could point to a `Guest.Anonymize()` in Domain (overwrite name/mail/phone/passport, keep `GuestId` so booking history survives). **I have not designed it**, because the X values are not fixed and because the retention mechanism lies outside Phase 1's deliverable. But if `Anonymize()` is to exist, it belongs in Domain — and it collides with the invariant "FirstName must not be empty" (BR-91). **It must be clarified before retention is built.**

**D-10 — `RoomSize` migration.**
The enum conversion requires that existing `NVARCHAR(20)` values be mapped to int. Test data uses `'Single'/'Double'/'Suite'`, but there is no CHECK constraint, so **unknown values may exist in production data.** Passed on to the infrastructure agent as a requirement for a `default` strategy (fail vs. `Single`).

**D-11 — `RoomStatus.OutOfService` vs. `Maintenance`.**
The old system never documents the difference. I have assigned them the meanings "administrative/long-term" vs. "technical fault/temporary". **That is my interpretation, not a found rule** — both block bookability identically, so a misinterpretation has no functional consequence in Phase 1, but it should be confirmed before the mobile app shows them differently.

---

## 9. Interface to the other agents

Passed on without my designing it:

- **Infrastructure:** the entities have private setters and a private parameterless ctor — EF must use backing fields/`UsePropertyAccessMode`. `DateRange` is **not** persisted; `StartDate`/`EndDate` are two ordinary `DateOnly` columns. `PassportNumber` must be encrypted and must not be indexed (B-07, D-04). `RoomSize` data migration from text to int. The enum ordinals for `BookingStatus` and `RoomStatus` are locked.
- **Application:** BR-19, BR-34 and BR-110 must call `BookingRules.Conflicts` / `Room.IsBookable` — not reimplement overlap. `today` and `occurredAt` must be supplied to the `Booking` methods from one time zone policy in one place.
- **Test:** the 22 tests in `BookingOverviewViewModelTests` (T-20..T-33) can be ported almost 1:1 against the `Booking` methods without mocks, because the state machine is now pure. T-21 (`Pending → CheckedIn`) must pass unchanged (B-01).

---

### Critical Files for Implementation

- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/BusinessRules.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/Analysis.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/Decisions.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/Phase1-Scope.md`
- `/mnt/user-data/uploads/Development/Conventions.md`
