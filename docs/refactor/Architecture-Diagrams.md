# Architecture-Diagrams.md — Phase 1

> **Legacy note.** This is not the framework Domain Model. The project Domain Model is [docs/domain-model.md](../domain-model.md), checked against QC-DM-001 (RC-036). This document is kept as the history of the analysis.

> Appendix to `Architecture.md`. Drawn by `design:diagrams` from the domain and layer designs.
> The diagrams follow the **revised** overlap rule (A-01) and the rulings A-06 and A-07.
>
> **Applicable overlap rule:** a booking blocks the room from `StartDate` to its effective end date, unless it is `Cancelled`.
> `EffectiveEndDate = GREATEST(StartDate + 1 day, COALESCE(CheckOutDate, EndDate))`. `EndDate` is never changed.

---

## 1. Domain model

```mermaid
classDiagram
    direction TB

    class Booking {
        +int BookingId
        +DateOnly StartDate
        +DateOnly EndDate
        +DateOnly CheckOutDate
        +DateTimeOffset CheckInTime
        +DateTimeOffset CheckOutTime
        +BookingStatus Status
        +int RoomId
        +int GuestId
        +DateRange Period
        +DateOnly EffectiveEndDate
        +DateRange EffectivePeriod
        +int NumberOfNights
        +string BookingNumber
        +bool CanConfirm
        +bool CanCheckOut
        +bool CanCancel
        +bool CanReschedule
        +Create(period, roomId, guestId, today) Booking
        +CanCheckIn(today) bool
        +Confirm() void
        +CheckIn(occurredAt, today) void
        +CheckOut(occurredAt, checkOutDate) void
        +Cancel() void
        +Reschedule(newPeriod, newRoomId, today) void
    }

    class Room {
        +int RoomId
        +string RoomNumber
        +int Floor
        +RoomSize Size
        +int Capacity
        +RoomStatus Status
        +HousekeepingStatus HousekeepingStatus
        +bool IsBookable
        +Create(roomNumber, floor, size, capacity) Room
        +UpdateDetails(roomNumber, floor, size, capacity) void
        +TakeOutOfService() void
        +SendToMaintenance() void
        +ReturnToService() void
        +MarkDailyCleaningDue() void
        +MarkDepartureCleaningDue() void
        +StartCleaning() void
        +CompleteCleaning() void
        +ReportServiceNeeded() void
        +StartService() void
        +CompleteService(requiresCleaning) void
    }

    class Guest {
        +int GuestId
        +string FirstName
        +string LastName
        +string Email
        +string PhoneNumber
        +string Country
        +string PassportNumber
        +string FullName
        +Create(firstName, lastName, email, phoneNumber, country, passportNumber) Guest
        +UpdateDetails(firstName, lastName, email, phoneNumber, country, passportNumber) void
    }

    class DateRange {
        +DateOnly Start
        +DateOnly End
        +int Nights
        +Overlaps(other) bool
        +Contains(date) bool
        +StartsOnOrAfter(today) bool
    }

    class BookingStatus {
        <<enumeration>>
        Pending
        Confirmed
        CheckedIn
        CheckedOut
        Cancelled
    }

    class RoomStatus {
        <<enumeration>>
        Available
        OutOfService
        Maintenance
    }

    class HousekeepingStatus {
        <<enumeration>>
        Clean
        DailyCleaningDue
        DepartureCleaningDue
        CleaningInProgress
        ServiceRequired
        ServiceInProgress
    }

    class RoomSize {
        <<enumeration>>
        Single
        Double
        Suite
    }

    class BookingRules {
        +Overlaps(a, b) bool
        +BlocksRoom(status) bool
        +EffectiveEndDate(booking) DateOnly
        +Conflicts(existing, roomId, desiredPeriod) bool
    }

    class GuestRules {
        +Validate(firstName, lastName, email, phoneNumber, country) errorCodes
        +IsValidEmail(email) bool
    }

    class RoomRules {
        +IsValidRoomNumber(roomNumber) bool
        +IsValidFloor(floor) bool
        +IsValidCapacity(capacity) bool
        +Validate(roomNumber, floor, size, capacity) errorCodes
    }

    Booking "0..*" --> "1" Room : RoomId
    Booking "0..*" --> "1" Guest : GuestId
    Booking ..> DateRange
    Booking ..> BookingStatus
    Room ..> RoomStatus
    Room ..> HousekeepingStatus
    Room ..> RoomSize
    BookingRules ..> Booking
    GuestRules ..> Guest
    RoomRules ..> Room
```

Three aggregate roots with no navigable connection beyond the foreign keys. `DateRange` is not persisted — it is a place for rules and a parameter type. `CheckInTime`, `CheckOutTime`, `CheckOutDate` and `PassportNumber` are nullable; mermaid does not render `?`. The validation methods return error codes, not text (A-09).

---

## 2. State diagram — BookingStatus

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Pending : Create<br/>guard start date not in the past<br/>BR-01 BR-44 BR-45 BR-48 BR-104 BR-105 BR-106

    Pending --> Confirmed : Confirm<br/>guard status is Pending<br/>BR-02 BR-03 BR-05 BR-121

    Pending --> CheckedIn : CheckIn<br/>guard CheckInTime empty<br/>and StartDate today at the latest<br/>BR-06 BR-07 BR-08
    Confirmed --> CheckedIn : CheckIn<br/>guard CheckInTime empty<br/>and StartDate today at the latest<br/>BR-06 BR-07 BR-08

    CheckedIn --> CheckedOut : CheckOut<br/>guard CheckOutTime empty<br/>sets CheckOutDate<br/>releases the remaining nights<br/>BR-09 BR-10 BR-107

    Pending --> Cancelled : Cancel - soft delete<br/>BR-11 BR-13 BR-122
    Confirmed --> Cancelled : Cancel - soft delete<br/>BR-11 BR-13 BR-122

    Pending --> Pending : Reschedule<br/>new period and new room<br/>BR-14 BR-17 BR-18 BR-20 BR-108 BR-123
    Confirmed --> Confirmed : Reschedule<br/>new period and new room<br/>BR-14 BR-17 BR-18 BR-20 BR-108 BR-123

    CheckedOut --> [*]
    Cancelled --> [*]

    note right of CheckedOut
        Final status
        Still blocks the room but only up to the
        effective end date equal to the check-out date
        EndDate is kept unchanged as history
    end note

    note right of Cancelled
        Final status
        The only status that releases the room completely
    end note
```

`Confirmed` is not a precondition for check-in (B-01) — hence two `CheckIn` edges. `Reschedule` is a self-transition. The revised rule affects only `CheckOut`: the transition does not change *whether* the booking blocks, but moves the effective end date down to the check-out date.

---

## 3. State diagram — HousekeepingStatus

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Clean : Room.Create<br/>BR-81

    Clean --> DailyCleaningDue : MarkDailyCleaningDue
    ServiceRequired --> DailyCleaningDue : MarkDailyCleaningDue
    DailyCleaningDue --> DailyCleaningDue : idempotent on retry

    Clean --> DepartureCleaningDue : MarkDepartureCleaningDue<br/>Application after CheckOut
    DailyCleaningDue --> DepartureCleaningDue : MarkDepartureCleaningDue
    ServiceRequired --> DepartureCleaningDue : MarkDepartureCleaningDue
    DepartureCleaningDue --> DepartureCleaningDue : idempotent on retry

    DailyCleaningDue --> CleaningInProgress : StartCleaning<br/>cleaning staff
    DepartureCleaningDue --> CleaningInProgress : StartCleaning<br/>cleaning staff
    CleaningInProgress --> Clean : CompleteCleaning<br/>cleaning staff

    Clean --> ServiceRequired : ReportServiceNeeded
    DailyCleaningDue --> ServiceRequired : ReportServiceNeeded
    DepartureCleaningDue --> ServiceRequired : ReportServiceNeeded
    CleaningInProgress --> ServiceRequired : ReportServiceNeeded

    ServiceRequired --> ServiceInProgress : StartService<br/>service technician
    ServiceInProgress --> Clean : CompleteService requiresCleaning false
    ServiceInProgress --> DepartureCleaningDue : CompleteService requiresCleaning true

    note right of Clean
        None of these states affect bookability
        Room.IsBookable reads only RoomStatus
        Blocking requires SendToMaintenance
        B-04 and BR-126
    end note
```

Three tracks: daily cleaning, final cleaning after departure, and service — the first two share `CleaningInProgress`. `MarkDepartureCleaningDue` is triggered by Application after `Booking.CheckOut`, not by `Booking` itself (two aggregates).

**The roles are documentation, not enforcement in Phase 1 (A-12).** Authorisation is postponed; anyone can in practice call any transition until Identity is built. No inspection step — added as `Inspected` when the mobile guild asks for it.

---

## 4. ER diagram

```mermaid
erDiagram
    room ||--o{ booking : "room_id"
    guest ||--o{ booking : "guest_id"

    booking {
        integer booking_id PK "identity"
        date start_date "NOT NULL"
        date end_date "NOT NULL - booked period - history"
        date check_out_date "NULL - hotel-local date set by the service"
        date effective_end_date "GENERATED STORED - controls overlap"
        timestamptz check_in_time "NULL"
        timestamptz check_out_time "NULL"
        integer status "NOT NULL - BookingStatus 0 to 4"
        integer room_id FK "NOT NULL - ON DELETE RESTRICT"
        integer guest_id FK "NOT NULL - ON DELETE RESTRICT"
    }

    room {
        integer room_id PK "identity"
        varchar room_number "NOT NULL - 20 characters - unique - BR-N-01"
        integer floor "NOT NULL - check above zero"
        integer size "NOT NULL - RoomSize 0 to 2"
        integer capacity "NOT NULL - check above zero"
        integer status "NOT NULL - RoomStatus 0 to 2 - default 0"
        integer housekeeping_status "NOT NULL - HousekeepingStatus 0 to 5 - default 0"
    }

    guest {
        integer guest_id PK "identity"
        varchar first_name "NOT NULL - 100 characters"
        varchar last_name "NOT NULL - 100 characters"
        varchar email "NOT NULL - 100 characters"
        varchar phone_number "NOT NULL - 50 characters"
        varchar country "NOT NULL - 100 characters"
        text passport_number "NULL - AES GCM encrypted - never indexed"
    }
```

All three tables also carry the system column `xmin` as the optimistic concurrency token. The exclusion constraint, check constraints and indexes are in `Architecture.md` section 5.

---

## 5. Layer and package diagram

```mermaid
flowchart TB
    subgraph LWeb [Outer layer - presentation]
        Web["NFHotel.Web<br/>Blazor Server<br/>Razor and Program.cs - composition root"]
        Api["NFHotel.Api<br/>PLANNED - not built in Phase 1"]
    end

    subgraph LInfra [Infrastructure layer]
        Infra["NFHotel.Infrastructure<br/>HotelDbContext and Configurations<br/>Repositories UnitOfWork SystemClock<br/>AesGcmStringEncryptor<br/>EF Core and Npgsql live ONLY here"]
    end

    subgraph LApp [Application layer]
        App["NFHotel.Application<br/>BookingService RoomService GuestService<br/>Repository interfaces IUnitOfWork IClock<br/>DTOs Requests Result ErrorCodes"]
    end

    subgraph LDom [Domain layer - core]
        Dom["NFHotel.Domain<br/>Booking Room Guest DateRange<br/>BookingRules GuestRules RoomRules<br/>BookingStatus RoomStatus HousekeepingStatus RoomSize<br/>ZERO project references and ZERO NuGet packages"]
    end

    Web --> App
    Web --> Infra
    Infra --> App
    App --> Dom

    Api -.-> App
    Api -.-> Infra

    classDef planned stroke-dasharray: 6 4,fill:#ffffff;
    class Api planned;
```

The proof is in the edge list: four built edges, each from an outer layer toward the core. `Domain` has **out-degree zero**. `Application` has exactly one outgoing edge. `Infrastructure` points to `Application`, never the reverse — that is why the repository *interfaces* live in Application and the *implementations* in Infrastructure.

The rule that cannot be drawn — that `Application` must not reference EF Core or Npgsql — is enforced by the architecture test.

---

## 6. Use case diagram

```mermaid
flowchart LR
    Staff(["Staff<br/>the only actor in Phase 1"])
    Customer(["Customer - POSTPONED"])
    Mobile(["Cleaning and service via mobile - POSTPONED"])

    subgraph UCB [IBookingService - 9]
        B1["GetByIdAsync"]
        B2["GetOverviewAsync<br/>BR-23 BR-24 BR-25 BR-32"]
        B3["GetAvailableRoomsAsync<br/>BR-34 BR-36 BR-39 BR-40 BR-110"]
        B4["CreateAsync<br/>BR-01 BR-41 to BR-49"]
        B5["ConfirmAsync<br/>BR-02 BR-03 BR-05"]
        B6["CheckInAsync<br/>BR-06 BR-07 BR-08"]
        B7["CheckOutAsync<br/>BR-09 BR-10 - sets CheckOutDate"]
        B8["CancelAsync<br/>BR-11 BR-13"]
        B9["RescheduleAsync<br/>BR-14 BR-17 to BR-20"]
    end

    subgraph UCR [IRoomService - 15]
        R1["GetByIdAsync"]
        R2["GetAllAsync"]
        R3["SearchAsync<br/>BR-74 BR-76"]
        R4["GetFilterOptionsAsync<br/>BR-75"]
        R5["CreateAsync<br/>BR-97 to BR-100 BR-113"]
        R6["UpdateAsync<br/>BR-82 - without status"]
        R7["DeleteAsync<br/>BR-78 BR-112"]
        R8["TakeOutOfServiceAsync"]
        R9["SendToMaintenanceAsync"]
        R10["ReturnToServiceAsync"]
        R11["MarkDailyCleaningDueAsync"]
        R12["MarkDepartureCleaningDueAsync"]
        R13["StartCleaningAsync"]
        R14["CompleteCleaningAsync"]
        R15["ReportServiceNeededAsync<br/>StartServiceAsync CompleteServiceAsync"]
    end

    subgraph UCG [IGuestService - 6]
        G1["GetByIdAsync<br/>BR-35"]
        G2["SearchAsync<br/>BR-68 BR-69 BR-70"]
        G3["CreateAsync<br/>BR-52 to BR-57 BR-91 to BR-96"]
        G4["UpdateAsync<br/>BR-62 BR-66 BR-72"]
        G5["DeleteAsync<br/>BR-N-02 requires approval"]
        G6["ValidateFields<br/>BR-58 BR-59 - synchronous"]
    end

    subgraph UCX [Postponed - drawn but not built]
        X1["Self-service booking and payment<br/>no price in the domain"]
        X2["ISalesService<br/>OMITTED - BR-31 always gives zero"]
        X3["Mobile API for cleaning and service<br/>the surface is R11 to R15"]
    end

    Staff --> UCB
    Staff --> UCR
    Staff --> UCG
    Customer -.-> X1
    Staff -.-> X2
    Mobile -.-> X3
    X3 -.-> UCR

    classDef deferred stroke-dasharray: 6 4,fill:#ffffff;
    class Customer,Mobile,X1,X2,X3,UCX deferred;
```

One use case = one service method: **30 built use cases**. `IRoomService` grew from 9 to 15, because the cleaning transitions each got their own method instead of one generic target status (A-07).

The seven cleaning methods are now built as back-office functions. They are at the same time exactly the surface the mobile app will later call — hence the dashed arrow from `X3`.
