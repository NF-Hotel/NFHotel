# Architecture-Layers.md — appendix

> Full output from the agent `design:layers` (Phase 1). **Appendix.** `Architecture.md` takes precedence where they contradict each other — see especially the rulings A-01 to A-15.

# Design document — `Application` and `Infrastructure` (Phase 1)

Agent: `design:layers`. Sources read: `BusinessRules.md` (126 rules), `Analysis.md` sections 1–5, `Conventions.md`, `NFHotel-Refactor-Plan.md` sections 1–3 + 7, `Phase1-Scope.md`, `Decisions.md`.

The premises B-03, B-05, B-06, B-07, B-08, B-09 are taken as given and not reconsidered. Entities, enums and domain rules are `design:domain`'s area; where I depend on their choices, it is stated as an explicit dependency in section 10.

---

## 1. Project structure

### 1.1 Solution

```
NFHotel.sln
├── src/
│   ├── NFHotel.Domain/          (design:domain)
│   ├── NFHotel.Application/     ← this document
│   ├── NFHotel.Infrastructure/  ← this document
│   ├── NFHotel.Web/             (design:web — only the DI contract here)
│   └── NFHotel.Api/             DRAWN, NOT BUILT (D-09)
└── tests/
    ├── NFHotel.Domain.Tests/
    ├── NFHotel.Application.Tests/
    └── NFHotel.Infrastructure.Tests/
```

References, one way: `Web → Application → Domain` and `Web → Infrastructure → Application → Domain`. **`Application` must not reference EF Core, Npgsql or ASP.NET.** That is enforced by an architecture test, not by discipline (section 9).

### 1.2 Folder principle

`Conventions.md` says: *"Organise by feature first, not only by type"* and *"Keep namespaces identical to the folder structure"*. The target architecture in the refactoring plan (section 3), on the other hand, draws `Interfaces/`, `Services/`, `DTOs/` — that is type-first. Conventions is the source of truth, so **feature-first wins**; see section 10, point 1 for the conflict written out.

Consequence: one feature folder holds its service interface, its service, its repository interface, its DTOs and its mapping. A developer who has to change "create booking" opens one folder.

> Note 2026-10-16: this was the Phase 1 design. Since ADR-0001 the Application project is organised as `UseCases`, `Dto/<Entity>`, `Ports` and `Errors`, and the repository interfaces are in `Domain/Repositories`; see `Architecture.md` section 2.

### 1.3 `NFHotel.Application`

```
src/NFHotel.Application/
├── NFHotel.Application.csproj
│     ProjectReference: NFHotel.Domain
│     PackageReference: Microsoft.Extensions.DependencyInjection.Abstractions
│                       Microsoft.Extensions.Options
│     (NO EF Core, NO Npgsql)
├── DependencyInjection.cs                      → AddApplication()
├── Common/
│   ├── Results/
│   │   ├── Error.cs
│   │   ├── ErrorCodes.cs
│   │   ├── Result.cs
│   │   └── ResultOfT.cs                        → Result<TValue>
│   ├── Exceptions/
│   │   ├── BookingOverlapConflictException.cs  → translated from the DB safeguard, B-03
│   │   ├── ConcurrencyConflictException.cs
│   │   └── UniqueConstraintViolationException.cs
│   ├── Sorting/
│   │   └── SortDirection.cs
│   └── Time/
│       ├── IClock.cs
│       └── HotelTimeOptions.cs
├── Abstractions/
│   └── Persistence/
│       └── IUnitOfWork.cs
├── Bookings/
│   ├── IBookingRepository.cs
│   ├── IBookingService.cs
│   ├── BookingService.cs
│   ├── BookingSortField.cs
│   ├── Dtos/
│   │   ├── AvailableRoomDto.cs
│   │   ├── BookingActionsDto.cs
│   │   ├── BookingDetailsDto.cs
│   │   └── BookingListItemDto.cs
│   ├── Requests/
│   │   ├── BookingOverviewQuery.cs
│   │   ├── CreateBookingRequest.cs
│   │   └── RescheduleBookingRequest.cs
│   └── Mapping/
│       └── BookingMappings.cs
├── Rooms/
│   ├── IRoomRepository.cs
│   ├── IRoomService.cs
│   ├── RoomService.cs
│   ├── Dtos/
│   │   ├── RoomDetailsDto.cs
│   │   ├── RoomFilterOptionsDto.cs
│   │   └── RoomListItemDto.cs
│   ├── Requests/
│   │   ├── CreateRoomRequest.cs
│   │   ├── RoomFilter.cs
│   │   └── UpdateRoomRequest.cs
│   └── Mapping/
│       └── RoomMappings.cs
└── Guests/
    ├── IGuestRepository.cs
    ├── IGuestService.cs
    ├── GuestService.cs
    ├── Dtos/
    │   ├── GuestDetailsDto.cs
    │   └── GuestListItemDto.cs
    ├── Requests/
    │   ├── CreateGuestRequest.cs
    │   ├── GuestFields.cs
    │   └── UpdateGuestRequest.cs
    └── Mapping/
        └── GuestMappings.cs
```

Namespaces follow the folders: `NFHotel.Application.Bookings.Dtos` etc. One public type per file, file name = type name (Conventions).

`GuestFields` lives in `Guests/Requests/` and is also used by `Bookings` (BR-47, the guest is created together with the booking). It is the only permitted cross-reference between feature folders, and it always goes toward a request/DTO — never toward another feature's service.

### 1.4 `NFHotel.Infrastructure`

```
src/NFHotel.Infrastructure/
├── NFHotel.Infrastructure.csproj
│     ProjectReference: NFHotel.Application
│     PackageReference: Npgsql.EntityFrameworkCore.PostgreSQL
│                       EFCore.NamingConventions
│                       Microsoft.EntityFrameworkCore.Design (PrivateAssets=all)
│                       Microsoft.Extensions.Options.ConfigurationExtensions
├── DependencyInjection.cs                     → AddInfrastructure(IConfiguration)
├── Persistence/
│   ├── HotelDbContext.cs
│   ├── HotelDbContextDesignTimeFactory.cs     → so `dotnet ef` does not have to start Web
│   ├── UnitOfWork.cs
│   ├── DbUpdateExceptionTranslator.cs         → SQLSTATE → Application exception
│   ├── Configurations/
│   │   ├── BookingConfiguration.cs
│   │   ├── GuestConfiguration.cs
│   │   └── RoomConfiguration.cs
│   ├── Converters/
│   │   └── EncryptedStringConverter.cs        → B-09
│   └── Seed/
│       └── DevelopmentSeedData.cs             (Development only)
├── Repositories/
│   ├── BookingRepository.cs
│   ├── GuestRepository.cs
│   └── RoomRepository.cs
├── Security/
│   ├── IStringEncryptor.cs
│   ├── AesGcmStringEncryptor.cs
│   └── EncryptionOptions.cs
├── Time/
│   └── SystemClock.cs
└── Migrations/                                (generated)
```

`IStringEncryptor` lives in **Infrastructure**, not Application. Application must never know that a field is encrypted — it sees plain text, EF does the rest. That is the whole point of a `ValueConverter` instead of encrypting in the service.

---

## 2. Repository interfaces

### 2.1 Design rules for the layer

Four rules, all direct answers to findings in `Analysis.md` section 2:

1. **Repositories do not decide.** The old repos contained BR-110, BR-111, BR-112, BR-113, BR-114, BR-116 (`Analysis.md`, "Business rules found in repos"). New layer: filters are *parameters*, not hard-coded choices. When `BookingService` wants to know which bookings block a room, it passes the status set itself — taken from `Domain`. The rule thus lives in one place, not in SQL.
2. **Nullability is honest.** The old `IBookingRepo.GetById` promised `Booking`, the class returned `Booking?`. All `GetById*Async` return `T?`, and the service translates `null` to `Result.Failure(NotFound)` (BR-115).
3. **No `SaveChanges` in repositories.** Write methods are synchronous (`Add`/`Update`/`Remove`) because EF's `Add` does no IO — `Conventions.md`: *"Methods with no real async work must not be async just for show"*. Only `SaveChangesAsync` on `IUnitOfWork` touches the database.
4. **`CancellationToken` on everything that touches IO** (B-05), always the last parameter with a default.

### 2.2 `IBookingRepository`

```csharp
namespace NFHotel.Application.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByPeriodAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> includedStatuses,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByStatusAsync(
        BookingStatus status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByRoomIdAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetByGuestIdAsync(
        int guestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetCandidatesForOverlapAsync(
        int roomId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetOccupiedRoomIdsAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        CancellationToken cancellationToken = default);

    void Add(Booking booking);
    void Update(Booking booking);
    void Remove(Booking booking);
}
```

`GetCandidatesForOverlapAsync` returns *candidates* — it coarse-filters on room, status set and date window in SQL. The overlap decision itself is made by the pure function in `Domain` (B-03), so repository and domain can never come to disagree about `<` versus `<=`. It is the same function used on creation and on update, which closes the contradiction BR-19 against BR-39.

`GetOccupiedRoomIdsAsync` exists because the alternative — fetching all bookings in the period and filtering in memory — is exactly BR-111's mistake. It returns ids, not entities, because the call is only used as a negation in the availability lookup.

**Comparison with `IBookingRepo`:**

| Old | New | Change and reason |
|---|---|---|
| `void Create(Booking)` | `void Add(Booking)` | Renamed. EF semantics: the method does not persist, it marks. `Create` lied about when the row existed. |
| `List<Booking> GetAll()` | `Task<IReadOnlyList<Booking>> GetAllAsync(ct)` | Async (B-05). `IReadOnlyList` because the caller must not mutate the result; `List<T>` invited it. |
| `Booking GetById(int)` | `Task<Booking?> GetByIdAsync(int, ct)` | Nullability corrected — the interface lied (`Analysis.md` section 2). BR-115 kept as a contract. |
| `List<Booking> GetByStatus(BookingStatus)` | `Task<IReadOnlyList<Booking>> GetByStatusAsync(...)` | Kept, but now implemented as `WHERE status = @p` instead of a full table pull plus LINQ. BR-111 dies as a rule. |
| `List<Booking> GetByRoomID(int)` | `GetByRoomIdAsync` | Casing corrected (U-19). |
| `List<Booking> GetByGuestID(int)` | `GetByGuestIdAsync` | Same. |
| `void Update(Booking)` | `void Update(Booking)` | Kept, but without the old pre-`GetById` and `ArgumentException` — the existence check (BR-114) moves to the service, where it can become a `Result`, and the read-then-write over two connections disappears. |
| `void Delete(int)` | `void Remove(Booking)` | Takes the entity, not the id, because the service has fetched it anyway to be able to reject "does not exist" nicely. Deletion is in practice not used on bookings — BR-13 is a soft delete via status. |
| — | `ExistsAsync` | **Added.** BR-114 without fetching the whole graph. |
| — | `GetByPeriodAsync` | **Added.** BR-24 and the overview's period filtering move into SQL. Also fixes the error from `Analysis.md` section 3 point 2, where week and month views did not filter at all. |
| — | `GetCandidatesForOverlapAsync` | **Added.** B-03. The old layer had *no* overlap check. |
| — | `GetOccupiedRoomIdsAsync` | **Added.** Carries BR-39/BR-34 without moving the rule down into SQL. |

### 2.3 `IRoomRepository`

```csharp
namespace NFHotel.Application.Rooms;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int roomId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetByStatusAsync(
        RoomStatus status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetByFilterAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetByIdsAsync(
        IReadOnlyCollection<int> roomIds,
        CancellationToken cancellationToken = default);

    Task<bool> RoomNumberExistsAsync(
        string roomNumber,
        int? excludeRoomId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetDistinctFloorsAsync(CancellationToken cancellationToken = default);

    void Add(Room room);
    void Update(Room room);
    void Remove(Room room);
}
```

| Old | New | Change and reason |
|---|---|---|
| `void CreateRoom(Room)` | `void Add(Room)` | Renamed; the type name is already in the interface, the `Room` suffix was noise (Conventions: avoid repetition). Validation (BR-113) removed from the repo. |
| `List<Room> GetAll()` | `GetAllAsync` | Async. |
| `Room? GetById(int)` | `GetByIdAsync` | Nullability now the same in interface and class. |
| `List<Room> GetRoomsFromCriteria(int?, string, RoomStatus?)` | `GetByFilterAsync(RoomFilter, ct)` | Renamed and gathered in a filter object. The three loose parameters were heading toward five (BR-74 plus housekeeping). `uspGetRoomsFromCriteria` disappears; the filter logic becomes readable C# instead of unverifiable T-SQL (`Analysis.md` section 2, "Unclear"). The signature deviation `string?` against `string` dies with it. |
| `void UpdateRoom(Room)` | `void Update(Room)` | Validation out of the repo. |
| `void DeleteRoom(int)` | `void Remove(Room)` | `catch (SqlException 547)` is replaced by an explicit `HasAnyBookingsAsync` check in the service. The FK is kept as the last safeguard, but the rule is no longer in a catch block. |
| `List<Room> GetAllByAvailability()` | **Removed** | The method was the clearest example of BR-110: the repo decided what "available" meant. Replaced by `GetByStatusAsync(RoomStatus.Available)`, where the *caller* chooses the status, plus the overlap lookup in the service. Bonus: it was not on the interface at all in the old system, so callers had to cast. |
| — | `GetByIdsAsync` | **Added.** The availability lookup needs to fetch a set of rooms by id without N+1. |
| — | `RoomNumberExistsAsync` | **Added.** See section 10, point 5: a unique room number is a *new* rule and requires approval. |
| — | `HasAnyBookingsAsync` | **Added.** Lifts BR-112 up into the service. |
| — | `GetDistinctFloorsAsync` | **Added.** BR-75's filter options. Only floor requires a DB lookup; size and status come from enums and are not fetched from data. |

### 2.4 `IGuestRepository`

```csharp
namespace NFHotel.Application.Guests;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(int guestId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int guestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guest>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns guests where EACH of the given search terms (case-insensitively) is found in
    /// either first name, last name, country or e-mail. An empty set returns all guests.
    /// The passport number is never part of the search — the field is encrypted at rest (B-09).
    /// </summary>
    Task<IReadOnlyList<Guest>> SearchAsync(
        IReadOnlyCollection<string> searchTerms,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyBookingsAsync(int guestId, CancellationToken cancellationToken = default);

    void Add(Guest guest);
    void Update(Guest guest);
    void Remove(Guest guest);
}
```

| Old | New | Change and reason |
|---|---|---|
| `int AddGuest(Guest)` | `void Add(Guest)` | Returned the id from `SCOPE_IDENTITY` with an unchecked unbox (`Analysis.md`: `(int)cmd.ExecuteScalar()`). EF sets the id on the entity after `SaveChangesAsync`; the service reads it from there. |
| `Guest? GetByID(int id)` | `Task<Guest?> GetByIdAsync(int guestId, ct)` | Casing corrected, parameter name aligned (it differed between interface and class and broke named arguments). |
| `List<Guest> GetAll()` | `GetAllAsync` | Async. |
| `List<Guest> GetAllByName(string)` | `SearchAsync(IReadOnlyCollection<string> terms, ct)` | Renamed and extended. The old `%name%` on first OR last name (BR-116) was only half of the search the user actually got: BR-68 in the ViewModel split the text into words and required that ALL words be found in name, country or e-mail. Two searches, two semantics. New: one. The tokenisation (the rule) happens in the service, the matching (the data access) in the repo, and the contract is in the XML doc and pinned by an Infrastructure test. See section 10, point 6. |
| `void UpdateGuest(Guest)` | `void Update(Guest)` | The old one ignored `rowsAffected`, so updating a non-existent guest failed silently. Now it is caught by EF's `DbUpdateConcurrencyException`, which is translated to an error code. |
| `void DeleteGuest(int)` | `void Remove(Guest)` | An FK violation bubbled up as a raw `SqlException`. The service checks `HasAnyBookingsAsync` first. |
| — | `ExistsAsync`, `HasAnyBookingsAsync` | **Added.** Guard for deletion and for D-05's personal data track. |

Note what was *not* added: no search or sorting on `PassportNumber`. That is verified against the ledger in section 6.4.

### 2.5 `IUnitOfWork` — necessary or superfluous?

**Necessary.** Not because `DbContext` lacks unit-of-work semantics — it has them — but because the Application layer must be able to *control* the transaction boundary without knowing EF.

The alternative is self-saving repositories, where `Add` internally calls `SaveChangesAsync`. That fails on three points:

1. **BR-47 requires atomicity.** "Create booking for new guest" writes a `Guest` and a `Booking`. If the repo saves each separately, the booking can fail on the exclusion constraint (B-03) after the guest has been committed — and the system is left with an orphaned guest. With one `SaveChangesAsync`, both inserts are in the same implicit transaction, and EF itself sorts the insert order by the navigation.
2. **Self-saving repos lie.** The three repositories share the same scoped `DbContext`. If `GuestRepository.Add` therefore calls `SaveChangesAsync`, it simultaneously commits everything `BookingRepository` and `RoomRepository` may have pending. It is an invisible coupling between three classes that apparently do not know each other. That is worse than the explicit dependency.
3. **Without a UoW the service cannot write two entities and roll back.** And the service is exactly the layer that owns the use case.

The interface is kept minimal:

```csharp
namespace NFHotel.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

No `BeginTransactionAsync`, no `Rollback`, no `IRepository<T>` property collection. All Phase 1 use cases manage with one `SaveChangesAsync` call; an explicit transaction API is added the day a use case writes in two rounds, and not before. A generic `IUnitOfWork` with repository properties would also make the services' constructor dependencies invisible to tests.

The implementation in Infrastructure is a two-line adapter over `HotelDbContext`, which is also where Postgres error codes are translated to Application exceptions (section 5.4).

---

## 3. Services

Common form: constructor injection (Conventions), `CancellationToken` last, `Result`/`Result<T>` return, no UI types, no `MessageBox`, no `Debug.WriteLine`. The test from `Phase1-Scope.md` applies to every single method: *can a console app call it and have all rules enforced?*

### 3.1 `IBookingService`

```csharp
namespace NFHotel.Application.Bookings;

public interface IBookingService
{
    Task<Result<BookingDetailsDto>> GetByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(
        BookingOverviewQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AvailableRoomDto>>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> ConfirmAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> CheckInAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> CheckOutAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> CancelAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDetailsDto>> RescheduleAsync(
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed class BookingService : IBookingService
{
    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<BookingService> logger);
}
```

| Method | Enforces | Notes |
|---|---|---|
| `GetByIdAsync` | BR-115 | `null` from the repo → `Result.Failure(Booking.NotFound)`. |
| `GetOverviewAsync` | BR-23, BR-24, BR-25, BR-32, BR-111 | Period filter always active (fixes that week/month did not filter). Sorting via the `BookingSortField` enum, not magic strings. |
| `GetAvailableRoomsAsync` | BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-110 | `excludeBookingId` is set on editing, so the booking does not block itself (it was missing in the old system and made BR-19 needlessly complicated). |
| `CreateAsync` | BR-01, BR-41, BR-42, BR-43, BR-44, BR-45, BR-46, BR-47, BR-48, BR-49, BR-51, BR-101–BR-106, plus overlap check (B-03) | The overlap check is new — it existed neither in `CreateBooking` nor in the repo. All validation errors are collected (BR-46's "only the first error" dies, consolidated with BR-60/BR-96). |
| `ConfirmAsync` | BR-02, BR-03, BR-05, BR-121 | The confirmation dialog (BR-04) is UI and stays in Web. |
| `CheckInAsync` | BR-06, BR-07, BR-08 | `CheckInTime = _clock.UtcNow`; "today" is the hotel-local date via `IClock`, not `DateTime.Now`. |
| `CheckOutAsync` | BR-09, BR-10, BR-107 | |
| `CancelAsync` | BR-11, BR-13, BR-122 | Soft delete: the status is set, the row stays. |
| `RescheduleAsync` | BR-14, BR-17, BR-18, BR-19, BR-20, BR-108, BR-123, plus B-03 | The same overlap function as `CreateAsync`. The name is deliberately not `UpdateAsync`: the use case is "move the booking", not "set arbitrary fields". |

BR-22 is handled not as a method, but as `BookingActionsDto` on each booking DTO — see section 4.2.

Note the placement of `GetAvailableRoomsAsync`: it returns rooms, but the rule is a *booking rule* (overlap in a period). If it were put on `RoomService`, `RoomService` would have to know `IBookingRepository`, and then both services would have both repositories. With the present distribution `RoomService` is free of bookings except for the delete guard, which uses a room-side lookup.

### 3.2 `IRoomService`

```csharp
namespace NFHotel.Application.Rooms;

public interface IRoomService
{
    Task<Result<RoomDetailsDto>> GetByIdAsync(
        int roomId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RoomListItemDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RoomListItemDto>>> SearchAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<RoomFilterOptionsDto>> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<RoomDetailsDto>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RoomDetailsDto>> UpdateAsync(
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RoomDetailsDto>> ChangeStatusAsync(
        int roomId,
        RoomStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<Result<RoomDetailsDto>> ChangeHousekeepingStatusAsync(
        int roomId,
        HousekeepingStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        int roomId,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed class RoomService : IRoomService
{
    public RoomService(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<RoomService> logger);
}
```

| Method | Enforces | Notes |
|---|---|---|
| `GetByIdAsync` | BR-115 | |
| `GetAllAsync` | — | |
| `SearchAsync` | BR-74, BR-76 | "Clear filter" = an empty `RoomFilter`; no separate method. Also fixes that the filter ran twice per popup opening (view bug). |
| `GetFilterOptionsAsync` | BR-75 | Floors from data; size, status and housekeeping from enums — which automatically resolves BR-126, where XAML and enum disagreed. |
| `CreateAsync` | BR-81, BR-84, BR-97, BR-98, BR-99, BR-100, BR-113 | BR-84 (validation was ignored in the form) cannot be repeated: the entity cannot be constructed invalid, and the service returns `Result.Failure` instead of saving. |
| `UpdateAsync` | BR-82, BR-84, BR-97–BR-100, BR-113 | BR-82 ("RoomId == 0 means create") dies: two methods, two use cases. |
| `ChangeStatusAsync` | BR-126, F-01 | Its own use case, because a status change should not require the whole room to be sent along. |
| `ChangeHousekeepingStatusAsync` | F-01, D-10 | The back office must be able to mark a room cleaned. The method is also the surface the mobile guild later hangs its API on — without anything mobile-specific being built now. |
| `DeleteAsync` | BR-78, BR-112 | Explicit `HasAnyBookingsAsync` before deletion. The FK is still the last safeguard, but the rule is not in a catch. BR-77's warning dialog is UI. |

### 3.3 `IGuestService`

```csharp
namespace NFHotel.Application.Guests;

public interface IGuestService
{
    Task<Result<GuestDetailsDto>> GetByIdAsync(
        int guestId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken = default);

    Task<Result<GuestDetailsDto>> CreateAsync(
        CreateGuestRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<GuestDetailsDto>> UpdateAsync(
        UpdateGuestRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        int guestId,
        CancellationToken cancellationToken = default);

    Result ValidateFields(GuestFields fields);
}
```

```csharp
public sealed class GuestService : IGuestService
{
    public GuestService(
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        ILogger<GuestService> logger);
}
```

| Method | Enforces | Notes |
|---|---|---|
| `GetByIdAsync` | BR-115, BR-35 | Also used to prefill a new booking for a known guest. |
| `SearchAsync` | BR-68, BR-69, BR-70, BR-116 | Tokenisation and AND logic here; sorting on "first name last name" in lowercase (BR-69) happens as part of the use case, not in the UI. An empty/whitespace search gives all (BR-70). |
| `CreateAsync` | BR-52–BR-57, BR-60, BR-91–BR-96 | All errors collected in one `Result` (BR-60/BR-96). BR-46's "only the first error" is thereby consolidated away. |
| `UpdateAsync` | BR-62, BR-66, BR-72, BR-91–BR-96 | `GuestId` is required and non-zero in the request, which closes the error from `Analysis.md` section 3 point 7, where an edited guest lost its id and was updated on id 0. BR-66's "work on a copy" becomes superfluous: the request *is* the copy. |
| `DeleteAsync` | D-05 (new), guard against existing bookings | Not in the ledger; marked as a new rule that requires approval. |
| `ValidateFields` | BR-58, BR-59 | **Deliberately not async** — there is no IO (Conventions). Lets the UI give feedback per keystroke and enable/disable the save button without duplicating a single validation rule. It is the method that means BR-52..BR-56 never have to exist in the Web layer. |

**No `SalesService` in Phase 1.** Section 3 of the refactoring plan mentions it, but BR-31 establishes that revenue is always set to 0, and there is no price anywhere in the domain. A service that returns 0 is worse than no service: it looks like an implementation. See section 10, point 7.

---

## 4. DTOs and mapping

### 4.1 Principle

The DTOs are defined in `Application` (Phase1-Scope, "cheap insurance") as `sealed record` types with init-only properties. They contain no formatting: dates are `DateOnly`/`DateTimeOffset`, not strings; status is an enum, not a colour. `Conventions.md`: *"Domain models must not be filled with UI formatting"* — that also applies to the DTOs, otherwise the problem just moves one layer.

### 4.2 Bookings

```csharp
public sealed record BookingActionsDto(
    bool CanConfirm,
    bool CanCheckIn,
    bool CanCheckOut,
    bool CanCancel,
    bool CanEdit);

public sealed record BookingListItemDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    int RoomId,
    string RoomNumber,
    int GuestId,
    string GuestFullName,
    string GuestCountry,
    string GuestEmail,
    BookingActionsDto Actions);

public sealed record BookingDetailsDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    RoomListItemDto Room,
    GuestListItemDto Guest,
    BookingActionsDto Actions);

public sealed record AvailableRoomDto(
    int RoomId,
    string RoomNumber,
    int Floor,
    RoomSize Size,
    int Capacity);
```

`BookingActionsDto` is the most important single part of the whole DTO design. It is computed in `Application` from `Domain`'s state rules and means the Web layer can draw buttons without knowing a single status transition. That is how BR-121, BR-122 and BR-123 (XAML triggers) and BR-02, BR-11, BR-14, BR-22 (ViewModel) become **one** implementation instead of six.

**Requests:**

```csharp
public sealed record CreateBookingRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int RoomId,
    int? GuestId,
    GuestFields? NewGuest);

public sealed record RescheduleBookingRequest(
    int BookingId,
    DateOnly NewStartDate,
    DateOnly NewEndDate,
    int RoomId);

public sealed record BookingOverviewQuery(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? SearchText,
    BookingSortField SortBy,
    SortDirection Direction,
    bool IncludeCancelled);

public enum BookingSortField
{
    BookingNumber, GuestName, RoomNumber, StartDate, EndDate, Status, Nights
}
```

`CreateBookingRequest` carries BR-47: either `GuestId` or `NewGuest`, exactly one of them. That both or neither is set is a validation error from the service — not a `NullReferenceException`.

### 4.3 Rooms and Guests

```csharp
public sealed record RoomListItemDto(
    int RoomId, string RoomNumber, int Floor, RoomSize Size, int Capacity,
    RoomStatus Status, HousekeepingStatus HousekeepingStatus);

public sealed record RoomDetailsDto(
    int RoomId, string RoomNumber, int Floor, RoomSize Size, int Capacity,
    RoomStatus Status, HousekeepingStatus HousekeepingStatus, bool HasBookings);

public sealed record RoomFilterOptionsDto(
    IReadOnlyList<int> Floors,
    IReadOnlyList<RoomSize> Sizes,
    IReadOnlyList<RoomStatus> Statuses,
    IReadOnlyList<HousekeepingStatus> HousekeepingStatuses);

public sealed record RoomFilter(
    int? Floor, RoomSize? Size, RoomStatus? Status, HousekeepingStatus? HousekeepingStatus);

public sealed record CreateRoomRequest(
    string RoomNumber, int Floor, RoomSize Size, int Capacity);

public sealed record UpdateRoomRequest(
    int RoomId, string RoomNumber, int Floor, RoomSize Size, int Capacity, RoomStatus Status);

public sealed record GuestFields(
    string FirstName, string LastName, string Email,
    string PhoneNumber, string Country, string? PassportNumber);

public sealed record GuestListItemDto(
    int GuestId, string FirstName, string LastName, string FullName,
    string Email, string PhoneNumber, string Country, bool HasPassportNumber);

public sealed record GuestDetailsDto(
    int GuestId, string FirstName, string LastName, string FullName,
    string Email, string PhoneNumber, string Country, string? PassportNumber);

public sealed record CreateGuestRequest(GuestFields Fields);

public sealed record UpdateGuestRequest(int GuestId, GuestFields Fields);
```

`GuestListItemDto` has `HasPassportNumber` (bool), not the number itself. Lists and overviews never need the value, and with B-09 every display of the field is a decryption. Only `GuestDetailsDto` carries plain text — and only it is used in the guest's edit form. It is privacy by design (D-04) without costing functionality: none of the 126 rules shows the passport number in a list.

### 4.4 Mapping strategy

**Explicit, hand-written, one static mapper class per feature**, placed in `<Feature>/Mapping/`, with extension methods:

```csharp
namespace NFHotel.Application.Bookings.Mapping;

public static class BookingMappings
{
    public static BookingListItemDto ToListItem(this Booking booking, DateOnly today);
    public static BookingDetailsDto ToDetails(this Booking booking, DateOnly today);
    public static BookingActionsDto ToActions(this Booking booking, DateOnly today);
}
```

**No AutoMapper.** Reasons:

- `Conventions.md`: *"Mapping must be explicit and easy to find"*. A profile class with convention-based name matching is neither.
- The scope is three entities and nine DTOs. AutoMapper would cost configuration, a package, a start-up validation and a learning threshold to save perhaps 60 lines of trivial assignment.
- The field choices are *decisions*, not boilerplate: that `GuestListItemDto` carries `HasPassportNumber` instead of the value is a privacy decision (B-09/D-04). Convention-based mapping would try to copy `PassportNumber` automatically and thereby decrypt the field in every list — silently and by itself.
- `ToActions` is not mapping, it is rule evaluation, and it must be readable and testable.

`today` is passed in from the service (which has `IClock`), so the mappers are pure functions and can be unit-tested without fake time.

**The opposite direction — request to entity — does not exist as mapping.** Services construct entities through `Domain`'s constructors/factory methods and change them through domain methods. Properties are never bulk-copied into an entity, because that is exactly where invariants tend to disappear.

---

## 5. Error handling

### 5.1 The choice

**`Result` / `Result<T>` for rule violations. Exceptions for programmer errors and infrastructure errors.**

Reasons:

- A rule violation is an **expected outcome**, not an exception. "This booking is already checked in" happens every day. Exceptions for expected outcomes are expensive, make stack traces noisy and — most importantly — can be ignored by a caller who does not know they exist.
- `Result` is visible in the signature. The caller cannot fail to deal with it without it showing in the code. It is the direct antidote to BR-07/BR-10, where check-in and check-out failed down into `Debug.WriteLine` while the status had already been changed in memory (Q-25).
- Several errors at once are required by BR-60 and BR-96 ("collect all errors in one list"). Exceptions are bad at that; an error list is natural in a `Result`.
- Web/Api are both clients of the same `Result`. Blazor shows an `Alert`, a later Api maps the error code to 409/422. If we had thrown, both layers would have to reinvent the translation.

### 5.2 The types

```csharp
namespace NFHotel.Application.Common.Results;

public sealed record Error(string Code, string Message);

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors);

    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public IReadOnlyList<Error> Errors { get; }
    public Error? FirstError { get; }

    public static Result Success();
    public static Result Failure(Error error);
    public static Result Failure(IReadOnlyList<Error> errors);
}

public sealed class Result<TValue> : Result
{
    public TValue Value { get; }

    public static Result<TValue> Success(TValue value);
    public static new Result<TValue> Failure(Error error);
    public static new Result<TValue> Failure(IReadOnlyList<Error> errors);
}
```

```csharp
public static class ErrorCodes
{
    public static class Booking
    {
        public const string NotFound            = "booking.not_found";
        public const string NotPending          = "booking.not_pending";
        public const string CheckInNotAllowed   = "booking.check_in_not_allowed";
        public const string CheckOutNotAllowed  = "booking.check_out_not_allowed";
        public const string CancelNotAllowed    = "booking.cancel_not_allowed";
        public const string EditNotAllowed      = "booking.edit_not_allowed";
        public const string RoomNotAvailable    = "booking.room_not_available";
        public const string StartDateRequired   = "booking.start_date_required";
        public const string EndDateRequired     = "booking.end_date_required";
        public const string EndBeforeStart      = "booking.end_before_start";
        public const string StartInPast         = "booking.start_in_past";
        public const string RoomRequired        = "booking.room_required";
        public const string GuestRequired       = "booking.guest_required";
        public const string GuestSelectionInvalid = "booking.guest_selection_invalid";
    }

    public static class Room { /* ... */ }
    public static class Guest { /* ... */ }
}
```

**The error code is the contract, the text is a convenience.** The UI chooses text from the code — so the field can be translated, and the old user-facing English strings disappear from the business layer. The old system had user-facing text all the way down in `RoomRepo`'s catch block (BR-112).

### 5.3 What is still thrown

| Situation | Reaction |
|---|---|
| `null` argument, negative id, empty required request | `ArgumentNullException` / `ArgumentException` as a guard clause. It is a programmer error, not a user outcome. Preserves BR-114's `ArgumentNullException` contract (T-32). |
| The database is down, timeout, network error | Bubbling exception. Caught centrally in Web (`ErrorBoundary` + `ILogger`), logged technically, the user sees a neutral message (Conventions: *"Log technical errors centrally. Show the user neutral error messages"*). |
| Optimistic concurrency conflict | `DbUpdateConcurrencyException` is translated in Infrastructure to `ConcurrencyConflictException` (Application), which the service catches and returns as `Result.Failure`. |
| Exclusion constraint hits (B-03) | See below. |

**No empty catch blocks. No `catch (Exception) { return false; }`** as in `DatabaseConfig.TestConnection`. Every catch logs or translates.

### 5.4 The interplay between service checks and database safeguard (B-03)

Overlap is rejected in two places, and the two places do not do the same:

1. `BookingService` calls the pure function in `Domain` against the candidates from `GetCandidatesForOverlapAsync`. It is **the normal path**, it gives a nice error code and a usable message, and it is covered by unit tests without a database.
2. PostgreSQL's exclusion constraint catches **the race**: two simultaneous creations that both saw a free room. `SaveChangesAsync` then fails with `PostgresException` and `SqlState = 23P01`.

`UnitOfWork.SaveChangesAsync` translates `23P01` (with the constraint name as discriminator) to `BookingOverlapConflictException`, defined in `Application.Common.Exceptions`. `BookingService.CreateAsync` and `RescheduleAsync` catch exactly that one and return `Result.Failure(ErrorCodes.Booking.RoomNotAvailable)` — **the same error code as the service check**. The user sees the same thing whichever of the two safeguards stopped her; the difference is visible only in the log.

It is also the reason the translation happens in Infrastructure and not in the service: Application must not know `PostgresException`, and the service should not need to know what `23P01` means.
Similarly: `23505` (unique violation) → `UniqueConstraintViolationException` → `ErrorCodes.Room.RoomNumberAlreadyExists`. `23503` (FK violation on deletion) → translated to the error code of the room/guest delete guard, as the last safeguard behind the explicit `HasAnyBookingsAsync` check.

---

## 6. EF Core configuration

### 6.1 `HotelDbContext`

```csharp
namespace NFHotel.Infrastructure.Persistence;

public sealed class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options);

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder);
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder);
}
```

`OnModelCreating` calls `HasPostgresExtension("btree_gist")` and `ApplyConfigurationsFromAssembly(typeof(HotelDbContext).Assembly)`. No entity configuration lives in `OnModelCreating` itself — one configuration class per entity, one responsibility per file (Conventions).

Providers and conventions are set at registration:

```csharp
options.UseNpgsql(connectionString, npgsql => npgsql
        .MigrationsAssembly(typeof(HotelDbContext).Assembly.FullName)
        .EnableRetryOnFailure())
       .UseSnakeCaseNamingConvention();   // EFCore.NamingConventions — B-06
```

`UseSnakeCaseNamingConvention()` turns `BookingId` into `booking_id` and `Booking` into `booking`. C# stays PascalCase, PostgreSQL is spared quoted identifiers, and U-19's casing chaos (`RoomID` versus `RoomId` versus `ROOM`) has nowhere to arise.

Read strategy: all query methods in repositories use `AsNoTracking()`, because they feed DTO mapping. Only `GetByIdAsync` in write use cases fetches tracked. Lazy loading is off; navigations are fetched with an explicit `Include`. This is the technical prerequisite for T-35 (eager loading of `Room` and `Guest`).

### 6.2 `BookingConfiguration`

- Key: `BookingId`, identity (`ValueGeneratedOnAdd`).
- `StartDate`, `EndDate`: `DateOnly` → `date`. Npgsql maps `DateOnly` to `date` without a converter (B-08). No `ValueConverter` needs to be written.
- `CheckInTime`, `CheckOutTime`: `DateTimeOffset?` → `timestamptz`, nullable. Npgsql requires the value to have offset 0; this is ensured by all timestamps coming from `IClock.UtcNow` — never from `DateTime.Now` (as in BR-07/BR-10).
- `Status`: `.HasConversion<int>()` (B-07), ordinals 0–4 preserved.
- Optional, recommended: `HasCheckConstraint("ck_booking_status_range", "status BETWEEN 0 AND 4")`. The old schema had none, so an arbitrary int could be inserted (`Analysis.md` section 1).
- Relations: `HasOne(b => b.Room).WithMany().HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict)` and likewise for `Guest`. `Restrict` is what carries BR-112 as a DB safeguard.
- `BookingNumber` and `NumberOfNights`: `.Ignore(...)` — derived values, no column. Unchanged from the old system, where `BookingNumber` had no column either.
- Concurrency: `Property<uint>("xmin").IsRowVersion().HasColumnName("xmin")`. Free in PostgreSQL, and it closes the read-then-write gap that `BookingRepo.Update` had across two separate connections.

**Exclusion constraint (B-03).** EF cannot model it, so it is written in the migration:

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE booking
  ADD CONSTRAINT ex_booking_room_period
  EXCLUDE USING gist (
      room_id WITH =,
      daterange(start_date, end_date, '[)') WITH &&
  )
  WHERE (status <> ALL (ARRAY[3, 4]));
```

Three things are worth noting:

- **`'[)'` is half-open** and corresponds exactly to the half-open test in BR-19 and BR-39 (`otherStart < newCheckOut AND otherEnd > newCheckIn`). Arrival on the same day as a departure is thus allowed, both in the domain and in the database.
- **The `WHERE` predicate must mirror `BookingRules`' blocking status set exactly.** The above omits `CheckedOut` (3) and `Cancelled` (4), i.e. BR-19's variant. If `design:domain` chooses BR-39's variant (only `Cancelled` omitted), the predicate must be `status <> 4`. The predicate must never be stricter than the domain rule — see section 10, point 3, where the consequence for early check-out is spelled out.
- The constraint creates its own GiST index on `(room_id, daterange(...))`, which also serves the overlap lookup from `GetCandidatesForOverlapAsync`.

### 6.3 `RoomConfiguration`

- Key: `RoomId`.
- `RoomNumber`: `string`, `HasMaxLength(20)`, `IsRequired()`. Resolves the conflict from `Analysis.md` section 1, where C# said `string` and both create scripts said `INT`, patched afterwards by a script that existed in only one of the SQL sets. New schema, one type, no patch.
- Unique index on `room_number` — **new rule**, see section 10, point 5.
- `Status` and `HousekeepingStatus`: `.HasConversion<int>()`.
- `RoomSize`: `.HasConversion<int>()` if `design:domain` makes it an enum (recommended by Phase1-Scope, "cheap insurance"). If it becomes a string, this line and `RoomFilter`'s field type change.
- `Floor`, `Capacity`: `int`, plus optional check constraints `floor > 0` (BR-98) and `capacity > 0` (BR-100) as DB safeguards behind the domain invariant.

### 6.4 `GuestConfiguration` and encryption (B-09)

```csharp
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IStringEncryptor encryptor);
}
```

Configured as:

```csharp
builder.Property(g => g.PassportNumber)
       .HasConversion(encryptedStringConverter)
       .HasColumnType("text");
```

The converter is instantiated in `GuestConfiguration`'s constructor with an injected `IStringEncryptor` (AES-GCM, key from `EncryptionOptions`, bound from `dotnet user-secrets` in dev and from environment variables/Actions Secrets in CI, cf. D-07). The configuration class is therefore registered explicitly in `OnModelCreating` rather than via assembly scanning, since it has a dependency.

**Consequences, concretely:**

| Consequence | Detail |
|---|---|
| No `LIKE`/`ILIKE` on the field | Ciphertext is not searchable. Any `WHERE passport_number LIKE ...` would hit encrypted text. |
| No `ORDER BY` on the field | Sort order is meaningless on ciphertext. |
| No index, not even a unique one | AES-GCM is randomised (a new nonce per encryption), so the same passport number gives different ciphertext each time. Even equality lookups do not work. |
| Column type | `text`, not `varchar(50)`. Ciphertext + nonce + tag, base64-encoded, takes up considerably more than the 50 characters the old `NVARCHAR(50)` gave. |
| `null` is preserved | EF does not call value converters for `null`, so `NULL` stays `NULL` in the database. That preserves T-41 ("passport number can be removed again, set to NULL, not an empty string") — but it requires Domain to distinguish `null` from `""`. Must be verified in an Infrastructure test. |
| Key rotation | Not solved in Phase 1. Rotation requires read-decrypt-re-encrypt across the whole table. Noted as an open task, not as an implementation. |

**Verification against the ledger.** I went through all rules that touch searching or sorting:

| Rule | Fields searched/sorted on | Hits passport number? |
|---|---|---|
| BR-116 (`GuestRepo.GetAllByName`) | first name, last name | No |
| BR-68 (guest search in VM) | full name, country, email | No |
| BR-69 (guest sorting) | "first name last name", lowercase | No |
| BR-25 (booking search) | name, country, email, room number | No |
| BR-74/BR-75 (room filter) | floor, size, status | No |
| BR-32 (booking sorting) | columns in the booking overview | No |
| BR-35 (prefill from selected guest) | lookup on `GuestId`, not search | No |
| BR-57 / T-14 (passport number optional) | no search | No |
| T-41 (passport number can be reset) | update by id | No |

**Conclusion: none of the 126 rules searches or sorts on passport number.** B-09's assumption holds. The only real consequence is the technical one (column type, no index, key handling) plus the DTO choice in section 4.3.

### 6.5 Indexes — which of the old ones are kept

The old schema had five: `IX_Booking_StartDate`, `IX_Booking_EndDate`, `IX_Booking_Status`, `IX_Booking_RoomID`, `IX_Booking_GuestID` — and they disappeared if `fix-booking-identity.sql` was run.

| Old index | Decision | Reason |
|---|---|---|
| `IX_Booking_RoomID` | **Kept** as `ix_booking_room_id` | PostgreSQL does not automatically create an index on the FK side. Needed for `GetByRoomIdAsync` and for the FK check on room deletion (BR-112). |
| `IX_Booking_GuestID` | **Kept** as `ix_booking_guest_id` | Same, plus `GetByGuestIdAsync`. |
| `IX_Booking_Status` | **Kept, but as a composite** `ix_booking_status_start_date (status, start_date)` | Status alone has five distinct values and is rarely selective enough. The overview always filters on status *and* period (BR-23 + BR-24), so the composite index matches the actual query pattern. |
| `IX_Booking_StartDate` | **Replaced** | Covered by the composite index above and by the GiST index. |
| `IX_Booking_EndDate` | **Dropped** | No query filters on end date alone. Period overlap goes through the GiST index. |
| — | **New:** GiST index via `ex_booking_room_period` | Comes automatically with the exclusion constraint (B-03). Serves the overlap lookup. |
| — | **New:** `ux_room_room_number` (unique) | New rule, requires approval — see section 10, point 5. |
| — | **New:** `ix_guest_last_name_first_name` | Guest search (BR-68) and sorting (BR-69). Consider a GIN/`pg_trgm` index instead if `ILIKE '%x%'` becomes slow; with the data volume in question, btree is plenty in Phase 1. |
| — | **New:** `ix_room_status` | The availability lookup (BR-110's successor). |

No index on `guest.passport_number` — not possible, cf. 6.4.

### 6.6 Migrations

Migrations are written from scratch against PostgreSQL; `01_CreateSchema.sql` is a reference, not a source. The two incompatible SQL sets and `fix-booking-identity.sql` are not ported. Naming: `20260907_InitialSchema`, `20260907_AddBookingOverlapConstraint` etc. — one thing per migration, as with commits.

The exclusion constraint, the `btree_gist` extension and any check constraints are added with `migrationBuilder.Sql(...)` in `Up` and the matching `DROP` in `Down`. `Down` must work; a migration that cannot be rolled back is not finished.

Migrations are **not** run automatically at start-up in anything but Development. `context.Database.Migrate()` in `Program.cs` is convenient and dangerous; use `dotnet ef database update` or a separate migrator step in the pipeline.

---

## 7. DI registration

### 7.1 Composition root

`NFHotel.Web/Program.cs` is the composition root (Conventions). It knows two extension methods and otherwise nothing about either EF or repositories:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebFeatures();   // ViewModels — design:web
```

### 7.2 `AddApplication`

```csharp
namespace NFHotel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services);
}
```

| Registration | Lifetime | Reason |
|---|---|---|
| `IBookingService` → `BookingService` | Scoped | Follows the `DbContext`'s lifetime through the repositories. Stateless in itself, but must not outlive its context. |
| `IRoomService` → `RoomService` | Scoped | Same. |
| `IGuestService` → `GuestService` | Scoped | Same. |

No assembly scanning. Three lines are easier to read than a convention that finds them, and a forgotten registration then fails at start-up instead of being found by chance.

### 7.3 `AddInfrastructure`

```csharp
namespace NFHotel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration);
}
```

| Registration | Lifetime | Reason |
|---|---|---|
| `HotelDbContext` (via `AddDbContextFactory` + scoped adapter) | Scoped | See 7.4. |
| `IUnitOfWork` → `UnitOfWork` | Scoped | Must be the *same* instance as the repositories' context. |
| `IBookingRepository` → `BookingRepository` | Scoped | Holds a `DbContext`. |
| `IRoomRepository` → `RoomRepository` | Scoped | Same. |
| `IGuestRepository` → `GuestRepository` | Scoped | Same. |
| `IClock` → `SystemClock` | Singleton | Stateless. |
| `IStringEncryptor` → `AesGcmStringEncryptor` | Singleton | Holds the key; `AesGcm` is instantiated per operation. |
| `EncryptionOptions` | Options + `ValidateOnStart()` | **The app must refuse to start without an encryption key.** The alternative is passport numbers stored in plain text without anyone noticing. |
| `HotelTimeOptions` (time zone) | Options + `ValidateOnStart()` | "Today" in BR-06 and BR-44 must be hotel-local, not server-local. |

The connection string is read from `configuration.GetConnectionString("HotelDatabase")` with `ValidateOnStart`. No static mutable global like `DatabaseConfig`, which could silently become `null` if the key was missing — that single class is responsible for a large part of the old system's untestability.

### 7.4 Blazor Server and `DbContext` lifetime

This is the one DI detail that can cause real production failures, and it must be coordinated with `design:web`.

In Blazor Server a scope is **the whole SignalR circuit** — potentially hours. A scoped `DbContext` would therefore: (a) accumulate tracked entities across the user's whole session, and (b) possibly be used by two concurrent component events, which throws `InvalidOperationException: A second operation started on this context`.

Design:

```csharp
services.AddDbContextFactory<HotelDbContext>(
    (sp, options) => { /* UseNpgsql + UseSnakeCaseNamingConvention */ },
    lifetime: ServiceLifetime.Scoped);

services.AddScoped<HotelDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<HotelDbContext>>().CreateDbContext());
```

Plus a requirement on the Web layer, to be written into `Architecture.md`: **each page/ViewModel gets its own DI scope** (via `OwningComponentBase` or an explicit `IServiceScopeFactory` scope per use case), and two concurrent operations must never share the same context. "Scoped" then becomes "per page" in practice, not "per session".

A later `Api` project can reuse `AddApplication` and `AddInfrastructure` unchanged — there scoped = per HTTP request, which is the lifetime EF is designed for. It is one of the concrete gains of registering via interfaces from the start (Phase1-Scope, "cheap insurance").

### 7.5 Preparation for authorisation (F-03), without building it

Authorisation is deferred, but F-03 requires that it ends up in the Application layer. There are two ways to get user context into a service: as a **parameter** or as an **injected dependency**. If we choose a parameter, every single service signature must change the day Identity arrives — exactly the kind of expensive change Phase1-Scope says to avoid.

Recommendation: **inject, do not parametrise.** No auth is built now, but when it comes, `ICurrentUser` is added as a constructor dependency in the services that need it. None of the signatures in section 3 needs to change. It costs nothing to decide now.

---

## 8. Rule placement

### 8.1 Placement principle

| Layer | What belongs there |
|---|---|
| **Domain** | Invariants and pure functions: is this date valid, do these two periods overlap, how many nights, is this status transition legal |
| **Application (service)** | The use case: fetch, ask the domain, orchestrate repositories, save, translate to `Result`. Everything that requires looking in the database to decide something |
| **Infrastructure** | Translation to SQL, schema safeguards (FK, exclusion constraint, check constraints), error-code translation |
| **Web** | Rendering, dialogs, enabling buttons from flags that Application has computed |

### 8.2 BR-110 to BR-116 — rules that live in repos today

| Id | Rule today | New placement | How |
|---|---|---|---|
| BR-110 | Only `Available` counts as available — filtered in `RoomRepo.cs:220` | **Application** — `BookingService.GetAvailableRoomsAsync` | The repo gets `GetByStatusAsync(RoomStatus status)` as a neutral filter; the service chooses `Available` and combines it with the overlap lookup. `GetAllByAvailability()` is dropped. |
| BR-111 | Status filter as an in-memory filter after a full table pull — `BookingRepo.cs:173` | **Infrastructure** (as a query, not as a rule) | `GetByStatusAsync` becomes `WHERE status = @p`. The rule was never a business rule, it was an implementation defect. Ceases to be a ledger entry. |
| BR-112 | A room with bookings must not be deleted — coded as `catch (SqlException 547)` in `RoomRepo.cs:208-210` | **Application** — `RoomService.DeleteAsync`, with **Infrastructure** as the last safeguard | Explicit `HasAnyBookingsAsync` before deletion → `Result.Failure(ErrorCodes.Room.HasBookings)`. The FK (`OnDelete.Restrict`) is kept and translated to the same error code in a race. The user-facing text ("set it to Out of service") moves to Web. |
| BR-113 | `Room.Validate()` before persisting, enforced in the repo — `RoomRepo.cs:21-23, 164-166` | **Domain** (invariant) + **Application** (translation to `Result`) | The entity cannot be constructed invalid. The repo does not validate — a repository that validates is a repository that has business logic. |
| BR-114 | The booking must exist before update/delete; null → `ArgumentNullException` — `BookingRepo.cs:190-196, 235-239` | **Application** (existence) + **guard clause** (null) | The existence check becomes `Result.Failure(Booking.NotFound)` in the service, which at the same time removes the old read-then-write across two connections. `ArgumentNullException` for a null argument is kept as a guard (T-32). |
| BR-115 | `GetById` on an unknown id returns `null` — three repos | **Contract between Infrastructure and Application** | The repository interfaces return `T?` (and thus no longer lie, cf. `IBookingRepo`'s `Booking GetById`). The service translates `null` to `NotFound`. Preserved behaviour, honest signature. |
| BR-116 | Name search = "contains" on first OR last name — `GuestRepo.cs:127, 130` | **Application** — `GuestService.SearchAsync` | Consolidated with BR-68's semantics: all search words must be found in name, country or email (AND). Tokenisation happens in the service, matching in the repo according to a contract that is in the XML doc and pinned by a test. **Behaviour change** — requires approval, see section 10, point 6. |

### 8.3 BR-117 to BR-126 — rules that live in converters and views today

| Id | Rule today | New placement | How |
|---|---|---|---|
| BR-117 | Width = number of nights `(EndDate − StartDate).Days` — `CalendarConverters.cs:149` | **Domain** | Already `Booking.NumberOfNights`; the DTO carries `Nights`. The converter computed the same thing one extra time. |
| BR-118 | Half-day rule: drawn from `daysOffset + 0.5` — `CalendarConverters.cs:31, 48` | **Web** (presentation geometry) | The 0.5 offset itself is pixel maths and belongs in the calendar component. But it encodes a business assumption — "the room is occupied from noon" — which `design:domain` should write down as a policy constant if it is to be a rule. Without that it is only a drawing. See section 10, point 8. |
| BR-119 | Bookings that started before the period are clipped to the period start, `+0.5` day on the width — `CalendarConverters.cs:81, 139` | **Web** (presentation geometry) | The data window is delivered by `GetOverviewAsync` (BR-24); the clipping is pure rendering. |
| BR-120 | The timeline shows only bookings where `Booking.Room.RoomId` matches; bookings without a room are omitted — `CalendarConverters.cs:197` | **Infrastructure** (schema) + **Application** (query) | `room_id` is `NOT NULL` with an FK, so "booking without a room" becomes impossible — half the rule disappears with the schema. Grouping per room happens in the overview query; the DTO carries `RoomId`. |
| BR-121 | Confirm button only for `Pending` (XAML trigger, duplicate of BR-02) | **Application** — `BookingActionsDto.CanConfirm` + `ConfirmAsync` | One place instead of two. The UI binds to a bool flag and knows no statuses. |
| BR-122 | Cancel button only for `Pending`/`Confirmed` (duplicate of BR-11) | **Application** — `BookingActionsDto.CanCancel` + `CancelAsync` | Same. |
| BR-123 | Edit button hidden for `CheckedIn`/`CheckedOut` (duplicate of BR-14) | **Application** — `BookingActionsDto.CanEdit` + `RescheduleAsync` | Same. |
| BR-124 | Guest fields read-only outside edit mode — `IsReadOnly = !IsEditing` | **Web** (pure UI state) | No service rule. The real protection is that `GuestService.UpdateAsync` is the only write path and validates regardless of what the UI did. |
| BR-125 | "Edit Guest" requires a selected guest; "New Booking" has no corresponding guard | **Web** (button enabling) + **Application** (guard) | The inconsistency dies through the types: `UpdateGuestRequest.GuestId` and `CreateBookingRequest` cannot be filled in without a guest, and the service returns `NotFound` if the id does not exist. It is no longer possible to have a guard in one place and not the other. |
| BR-126 | Room statuses hard-coded in XAML (Available / Maintanance / Cleaning / Disabled) and out of step with the `RoomStatus` enum | **Domain** (enum = the only source) + **Application** (`GetFilterOptionsAsync`) | The UI binds to the list from Application; a hard-coded dropdown can no longer get out of step. The flow itself (F-01, daily cleaning/service/final cleaning) is determined by `design:domain`; `RoomService.ChangeHousekeepingStatusAsync` is the surface. |

### 8.4 Rules enforced in the service layer — overview

Rules that live in **Application**, i.e. not in Domain, not in the UI, not in the database:

| Service | BR ids |
|---|---|
| `BookingService` | BR-01, BR-03, BR-05, BR-08, BR-13, BR-17, BR-20, BR-22, BR-23, BR-24, BR-25, BR-32, BR-33*, BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-45, BR-46, BR-47, BR-48, BR-49, BR-51, BR-110, BR-111, BR-114, BR-115, BR-120, BR-121, BR-122, BR-123 |
| `RoomService` | BR-74, BR-75, BR-76, BR-78, BR-80*, BR-82, BR-84, BR-112, BR-113, BR-115, BR-126 |
| `GuestService` | BR-58, BR-59, BR-60, BR-62, BR-65*, BR-66, BR-67*, BR-68, BR-69, BR-70, BR-71*, BR-72, BR-73*, BR-115, BR-116, BR-125 |

\* These are partly UI behaviour (reloading, selected-item guards). The service carries the enforcing half — that the action is rejected with `NotFound` if the id is missing — while the enabling of buttons is Web.

Rules that do **not** live in the service layer, and where they live instead:

- **Domain:** BR-02, BR-06, BR-09, BR-11, BR-14, BR-18, BR-19, BR-41–BR-44, BR-52–BR-57, BR-88–BR-109, BR-117.
- **Web (pure presentation or confirmation dialogs):** BR-04, BR-12, BR-15, BR-16, BR-21, BR-26–BR-30, BR-50, BR-61, BR-63, BR-64, BR-77, BR-79, BR-83, BR-85, BR-86, BR-118, BR-119, BR-124.
- **Infrastructure (schema safeguards):** BR-112 (FK), the overlap safeguard from B-03, BR-120's "no booking without a room".
- **Cease to exist:** BR-31 (revenue was never implemented; no prices in the domain), BR-87 (a WPF `RelayCommand` detail with no counterpart in Blazor), BR-111 (a defect, not a rule).

---

## 9. Test projects

The starting point is not comfortable: 40 of the 128 old tests hit the **same physical database as the application** (`HotelBooking`), with hard-coded keys like `"999"` and `TESTAVAIL`, `Parallelize(MethodLevel)` switched on, uneven clean-up wrapped in `catch {}`, and an `appsettings.json` that is not in the repo at all — so the suite is 0 % runnable out of the box. It should not be ported, it should be replaced.

### 9.1 Distribution

| Project | Tests | Dependencies | Run time |
|---|---|---|---|
| `NFHotel.Domain.Tests` | Invariants, date rules, the overlap function, status transitions, `NumberOfNights`, `BookingNumber` | None | Milliseconds |
| `NFHotel.Application.Tests` | Use cases: services with mocked repositories and a fake `IClock` | Mocks | Milliseconds |
| `NFHotel.Infrastructure.Tests` | Schema, mapping, queries, constraints | Testcontainers `postgres:16` | Seconds |

### 9.2 `Application.Tests` — what replaces the 22 ViewModel tests

According to the analysis, the 22 tests in `BookingOverviewViewModelTests` are the only real source of truth and the only file with genuine mocks. **T-20 to T-31 are ported 1:1 as service tests** — same scenarios, new receiver: `BookingService` instead of `BookingOverviewViewModel`. That is the very proof that the business logic actually moved.

What is tested here:

- All status transitions and their rejections (T-20 to T-27), asserted on **error code**, not on error text — texts are the UI's, codes are the contract.
- Overlap rejection in both `CreateAsync` and `RescheduleAsync` (B-03), with a mocked repository that returns colliding candidates. This is new coverage; the old system had none.
- BR-47's two branches: an existing guest is reused, a new guest is created — and that `SaveChangesAsync` is called **once** (verify on the mocked `IUnitOfWork`). It is the test of atomicity.
- BR-60/BR-96: that all validation errors are included, not just the first.
- Filtering and sorting (T-28, T-29), including that the period filter now applies to all views.
- The `BookingActionsDto` flags, which are the single thing the UI layer is going to trust blindly.

Two discipline requirements: **`IClock` is always mocked** — BR-06, BR-44 and BR-104 are all about "today", and a test that uses the real system time stops working tomorrow (the old project had exactly that problem built in). And **`Verify(...)` is actually used** — the old project had Moq but used mocks purely as stubs, without a single `Verify`.

In addition, an **architecture test** (NetArchTest or equivalent) in this project: `NFHotel.Application` must not have types that reference `Microsoft.EntityFrameworkCore` or `Npgsql`. Layering enforced by the build, not by memory.

### 9.3 `Infrastructure.Tests` — Testcontainers, not a shared database

One `postgres:16` container per test collection, started in an xUnit `ICollectionFixture`. Each test runs in its own transaction that is rolled back, or against a schema reset with Respawn. **No test hits any developer's or anyone's shared database, and no test has hard-coded keys.** It is the direct antidote to the old setup's three problems at once: shared DB, hard-coded ids and parallelism.

What belongs here and nowhere else:

1. **The migrations can run from scratch**, and `Down` works.
2. **Snake_case takes effect** (B-06): the table is called `booking`, the column `start_date`.
3. **Enum mapping** (B-07): `BookingStatus.Cancelled` lands as `4` in the column. Asserted with raw SQL, not through EF — otherwise you are testing EF against itself.
4. **Date types** (B-08): `DateOnly` → `date`, `DateTimeOffset` → `timestamptz`, and a round trip preserves UTC.
5. **Encryption** (B-09): the passport number is read back correctly through EF, **and** a raw SQL read of the column shows something that is not the plain text. Plus that `null` stays `NULL` (T-41) and does not become an encrypted empty string.
6. **The exclusion constraint works under concurrency** (B-03): two parallel transactions that both insert overlapping bookings on the same room — exactly one must commit, the other must fail with `23P01`, and the translation to `BookingOverlapConflictException` must happen. It is the most important single test in the whole project, because it is the only one that proves double booking cannot happen.
7. **Query semantics:** the guest search's AND logic (T-40 extended), the room filter's exclusive criteria (T-38, T-39), eager loading of `Room` and `Guest` (T-35).
8. **Delete guards:** the FK rejects deletion of a room with bookings (T-36's successor, BR-112).

### 9.4 What is not ported

The roughly 45 boilerplate tests from the analysis: property get/set, "is it non-null", three identical `RoomSize_Accepts*`, round-trip tests that only test ADO.NET mapping. `Conventions.md` says it directly: *"Avoid unit-testing simple boilerplate."*

The 46 model tests (T-01 to T-19), on the other hand, are ported to `Domain.Tests` — they are fast, pure and encode the validation contract. That is `design:domain`'s area, but they are worth mentioning here because they cannot run today: assembly init throws `FileNotFoundException` and takes down the whole assembly, including the pure tests. With layered test projects, the Domain and Application tests can run without Docker, without a database and without configuration.

### 9.5 Framework

**Recommendation: xUnit** in all three projects, with **NSubstitute or Moq** for mocks. xUnit has the best Testcontainers integration (`IAsyncLifetime`), and there is really nothing to port: the tests worth keeping have to be rewritten for new types anyway. Target framework `net8.0` (not `net8.0-windows` — the old suite was Windows-locked solely because it referenced the WPF project).

Naming per Conventions: `CheckInAsync_ShouldFail_WhenBookingIsAlreadyCheckedIn()`. Arrange/Act/Assert consistently — that discipline was actually high in the old project and is worth taking along.

---

## 10. Unclear and contradictory

1. **Feature-first versus type-first in `Application`.** `Conventions.md` requires feature-first (`Features/Orders/Create/`); the refactoring plan's section 3 draws `Interfaces/`, `Services/`, `DTOs/`. I have chosen feature-first, because Conventions is designated as the source of truth. The decision must be confirmed, and `Architecture.md` must then correct the target-architecture drawing — otherwise two documents contradict each other from day one.

2. **Conventions places Entity↔DTO mapping in Infrastructure.** The section "SOC → Infrastructure" says `Mapping (Entity ↔ DTO)`, while `Phase1-Scope.md` under "cheap insurance" requires DTOs to be defined in `Application`, not in `Web`, so that a later Api can reuse them. The two cannot both hold: if the DTO lives in Application and the mapper in Infrastructure, the Application service cannot reach the mapper without reversing the dependency direction. I have chosen Phase1-Scope (mapping in Application), because that decision is newer and scope-specific, and because Infrastructure's mapping responsibility is in practice entity↔database, not entity↔DTO. **Should be written in as a correction to `Conventions.md`.**

3. **BR-19 versus BR-39 decides the exclusion constraint's predicate — and the choice has a concrete price.** BR-19 (editing) excludes both `Cancelled` and `CheckedOut` from the overlap check; BR-39 (creation) excludes only `Cancelled`. `design:domain` picks one set. The consequence must be part of the decision: if BR-39's set is chosen, the database blocks **re-letting after early check-out** — booking 1–10 January, the guest checks out on the 3rd, and a new booking 4–8 January will be rejected by the constraint even though the room is empty. If BR-19's set is chosen, conversely, an overlapping booking can be created on top of a checked-out booking whose period has not yet expired. The proper solution is probably that **`CheckOutAsync` truncates the booking's period to the actual check-out date**, after which BR-39's stricter set can be used without side effects — but that is a new domain rule and thus `design:domain`'s call. Whichever is chosen, the predicate in the migration must be generated from one constant in Domain, and an Infrastructure test must assert that the two agree.

4. **`RoomSize`'s type is not settled.** `Phase1-Scope.md` recommends an enum or lookup over a free string, but it is not in the list of types I may assume. I have written `RoomSize` as an enum in `RoomFilter`, DTOs and requests. If it becomes a string, four signatures plus one EF configuration line change. If it becomes a lookup table, `GetFilterOptionsAsync` also changes to a DB lookup.

5. **Unique room number is a new rule.** The old schema had no unique constraint on `RoomNumber`, and none of the 126 rules requires it. I have proposed `ux_room_room_number` plus `RoomNumberExistsAsync`, because two rooms with the same number make no sense — but it is an addition, not a preserved rule, and Phase 3's ledger audit will register it as "invented" unless it is explicitly approved. The same goes for `GuestService.DeleteAsync` (D-05) and check constraints on `floor > 0` / `capacity > 0`.

6. **Guest search changes behaviour.** BR-116 (repo: first or last name) and BR-68 (ViewModel: all words in name, country or email) are two different searches. Consolidating to BR-68's semantics means that a search on a surname now also hits a country with the same name. It is an improvement, but it is a change, and it must be approved rather than slip through.

7. **`SalesService` is in the target architecture, but there is nothing to compute.** BR-31 establishes that revenue is always set to 0, and there is no price anywhere in the domain — `Room.RoomSize` is free text, not a price. I have left the service out. The alternative is a service that returns 0 and looks like an implementation. The Sales screen should either be dropped from Phase 1 or shown as an explicit placeholder. The price question (open question 3 in the plan) is not answered anywhere.

8. **BR-118's "occupied from noon" is a business assumption disguised as pixel maths.** I have placed the geometry in Web, but if the hotel really has a check-in time and a check-out time, it is a domain policy that should also apply to the check-in window (BR-06 today allows check-in on the whole start date). Should be clarified by `design:domain`; otherwise 0.5 in the converter is the only place that knowledge exists — again.

9. **The encryption key and rotation are not solved.** D-07 is itself marked as temporary and unresolved (Actions Secrets versus files in the repo). Phase 1 can deliver `EncryptionOptions` with `ValidateOnStart`, so the app refuses to start without a key, but key rotation requires a backfill migration that reads, decrypts and re-encrypts the whole `guest` table. It is not designed here. Likewise, D-05's two retention tracks (booking data after X months, personal data for X years) are not designed — the X's do not exist.

10. **Primary-key naming must be settled.** `Conventions.md` says "Id or [Entity]Id — choose one standard", and the old system had `BookingID`, `GuestID` and `RoomId` side by side (U-19). I have assumed `BookingId`, `RoomId`, `GuestId` in C#, which become `booking_id`, `room_id`, `guest_id` in the database. They are `design:domain`'s properties, so the choice must be confirmed there — but it hits every single EF configuration and migration, so it must be confirmed **before** the first migration is generated.

11. **The Blazor Server scope must be coordinated.** My DI design presupposes that the Web layer gives each page its own DI scope. If it does not, a user gets one `DbContext` for their whole session, and two concurrent component events will throw. It must be written as a requirement in `Architecture.md` and agreed with `design:web`, not discovered in operation.

12. **No data migration from the old system is assumed.** The plan says test data is ported to EF seed data, and that the two T-SQL sets cannot be run against PostgreSQL. I have designed for an empty database. If production data actually has to be moved over, a one-off task comes on top: existing passport numbers must be encrypted on load, and existing bookings must be able to satisfy the exclusion constraint — if they overlap today (and they can, because there has never been a check), the constraint cannot be created until the data has been cleaned up. **This should be verified against the running database before the migration is written**, not afterwards.

---

### Critical files for implementation

Source documents read that govern the implementation:

- `/mnt/user-data/uploads/Development/Conventions.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/BusinessRules.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/Analysis.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel/docs/Phase1-Scope.md`
- `/mnt/user-data/uploads/Development/New folder/NFHotel-Refactor-Plan.md`

The five files to be written first, in this order (planned paths under the solution root):

1. `src/NFHotel.Application/Common/Results/Result.cs` — everything else in Application depends on the return type.
2. `src/NFHotel.Application/Bookings/IBookingRepository.cs` — defines the contract that both `BookingService` and `BookingRepository` are built against.
3. `src/NFHotel.Application/Bookings/BookingService.cs` — carries the most rules and is the metric Phase 3's ledger audit measures against.
4. `src/NFHotel.Infrastructure/Persistence/Configurations/BookingConfiguration.cs` — B-03, B-06, B-07 and B-08 meet here, and the migration is generated from it.
5. `src/NFHotel.Infrastructure/DependencyInjection.cs` — lifetimes, options validation and the `DbContext` factory, i.e. what decides whether the Blazor Server setup holds.
