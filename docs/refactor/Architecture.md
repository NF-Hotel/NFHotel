# Architecture.md — NFHotel 2.0

> Fase 1 (Opgave 1b). Udarbejdet 2026-09-07 af tre agenter (`design:domain`, `design:layers`, `design:diagrams`) og konsolideret her.
> **Dette dokument har forrang.** Bilagene `Architecture-Domain.md`, `Architecture-Layers.md` og `Architecture-Diagrams.md` er agenternes fulde output og er kun gældende hvor de ikke modsiger dette dokument.
>
> Status: **klar til review.** Fase 2 starter ikke før Valdemar godkender.

---

## 1. Hvad der bygges

Personale-backoffice på web, porteret fra WPF, oven på Clean Architecture med EF Core og PostgreSQL.

**24 use cases** fordelt på tre services: `IBookingService` (9), `IRoomService` (9 → se A-07), `IGuestService` (6).

Ikke i denne omgang: API-projektet som kode, Flutter-app, kundeside, auth-implementering, roller, priser, betaling. Jf. `Fase1-Scope.md`.

---

## 2. Projektstruktur

> **Opdateret 2026-10-16 (ADR-0001):** mapperne følger nu skillets struktur (Entities, ValueObjects, Repositories, Errors, Rules i Domain; UseCases, Dto, Ports, Errors i Application). English summary: see `docs/adr/adr-0001-clean-architecture-structure.md`.

```
NFHotel.sln
├── src/
│   ├── NFHotel.Domain/          # nul projektreferencer, nul NuGet-pakker
│   │   ├── Entities/                  Entity<TKey>, Booking, Guest, Room
│   │   ├── ValueObjects/              DateRange, BookingStatus, RoomStatus, HousekeepingStatus, RoomSize
│   │   ├── Repositories/              IRepository<TEntity,TKey>, IBookingRepository, IGuestRepository, IRoomRepository
│   │   ├── Errors/                    DomainException, InvalidStateTransitionException
│   │   └── Rules/                     BookingRules, GuestRules, RoomRules
│   ├── NFHotel.Application/     # må ikke referere EF Core, Npgsql, ASP.NET
│   │   ├── UseCases/                  Result, services og mapping pr. entitet (Bookings/, Rooms/, Guests/)
│   │   ├── Dto/                       DTOer og requests, én mappe pr. entitet, én type pr. fil
│   │   ├── Ports/                     IClock, IUnitOfWork, IHolidayCalendarClient, IBookingQueries, IRoomQueries, IGuestQueries
│   │   └── Errors/                    ErrorCodes og applikationsfejl
│   ├── NFHotel.Infrastructure/  # EF Core og Npgsql bor KUN her
│   │   ├── Persistence/               HotelDbContext, Configurations/ (EntityConfiguration: <tabel>_id), Migrations/
│   │   ├── Repositories/              Repository<TEntity,TKey> og de tre repositories
│   │   ├── Config/                    options for kryptering, tid og analysetjenesten
│   │   ├── Security/                  AesGcmStringEncryptor
│   │   └── Time/                      SystemClock
│   └── NFHotel.Web/             # Blazor Server, feature-først, composition root
└── tests/
    ├── NFHotel.Domain.Tests/
    ├── NFHotel.Application.Tests/       # + arkitekturtest
    └── NFHotel.Infrastructure.Tests/    # Testcontainers, postgres:16

# Del af målarkitekturen, bygges IKKE i Fase 1:
#   src/NFHotel.Api/       REST over Application (mobil-guilden)
#   src/.../Web/Customer/        kundeside (frontend-guilden)
#   mobile/nfhotel_app/    Flutter (mobil-guilden)
```

Afhængighedsretningen er `Web → Application → Domain` og `Web → Infrastructure → Application → Domain`. Domain har **udgrad nul**. Det håndhæves af en arkitekturtest, ikke af disciplin.

---

## 3. Besluttede præmisser (B)

| Id | Beslutning | Begrundelse |
|---|---|---|
| **B-01** | Walk-in tilladt: `Pending → CheckedIn` uden om `Confirmed`. `Confirmed` betyder "betaling garanteret", ikke "må tjekke ind" | Bevarer BR-06 og test T-21. Betalingsgating hører til `Payment`, ikke til check-in |
| **B-02** | *(revideret, se A-01)* En booking blokerer rummet fra `StartDate` til sin **effektive slutdato**, medmindre den er `Cancelled` | Løser modsigelsen BR-19 ↔ BR-39 uden at spærre for genudlejning efter tidlig udtjekning |
| **B-03** | Overlap er en ren funktion i Domain **plus** en PostgreSQL exclusion constraint | Det gamle system havde intet overlapstjek ved oprettelse. Constrainten gør dobbeltbooking umulig under samtidighed |
| **B-04** | `RoomStatus` (bookbarhed) og `HousekeepingStatus` (rengøringsforløb) er to separate enums | Den gamle enum blandede de to og var uenig med sin egen XAML-dropdown (BR-126, U-15) |
| **B-05** | Async hele vejen ned. Ingen `.Result`, ingen `.Wait()` | Conventions.md. Det gamle system var 100 % synkront |
| **B-06** | snake_case i databasen via `EFCore.NamingConventions`; PascalCase i C# | Løser U-19 og PostgreSQLs lowercase-folding |
| **B-07** | Enums lagres som int. `BookingStatus` 0-4 bevares | Ordinalværdierne findes allerede i data |
| **B-08** | `DateOnly` → `date`, `DateTimeOffset` → `timestamptz` lagret UTC | Bookingperioden er datoer, ikke tidsstempler. Fjerner en klasse af fejl |
| **B-09** | Pasnummer krypteres at-rest, indekseres aldrig | D-04. Ingen af de 126 regler søger på pasnummer, så det koster ingen funktionalitet |
| **B-10** | Al forretningslogik i Application-services eller Domain — aldrig i ViewModels | Den eneste beslutning der virkelig betyder noget. 823 linjer logik i én ViewModel var det gamle systems kernefejl |

---

## 4. Afgørelser (A) — modsigelser mellem de tre designs

Diagram-agenten fandt 12 uoverensstemmelser ved at tegne domæne- og lagdesignet i samme figur. Hver er afgjort her. **Bilagene er forkerte hvor de modsiger dette afsnit.**

### A-01 — Overlapsprædikatet *(vigtigst)*

`design:layers` påviste at min oprindelige B-02 havde en konkret driftsfejl: hvis en `CheckedOut` booking blokerer hele sin bookede periode, kan et rum ikke genudlejes efter tidlig udtjekning. Booking 1.–10. januar, gæsten rejser den 3., ny booking 4.–8. januar ville blive afvist af databasen selv om rummet står tomt.

**Afgjort — effektiv slutdato, uden at røre `EndDate`:**

```
EffectiveEndDate = GREATEST(StartDate + 1 dag, COALESCE(CheckOutDate, EndDate))
En booking blokerer rummet i [StartDate, EffectiveEndDate) medmindre Status = Cancelled.
```

Den bookede periode bevares som historik; kun den effektive belægning styrer overlap. Tidlig udtjekning frigiver automatisk de resterende nætter. `GREATEST(StartDate + 1, ...)` sikrer at samme-dags-udtjekning ikke giver en tom periode.

Konsekvenser: `Booking.EffectiveEndDate` og `Booking.EffectivePeriod` tilføjes; `BookingRules.Conflicts` bruger `EffectivePeriod`, ikke `Period`; repository-grovfiltret filtrerer på `effective_end_date`.

### A-02 — `check_out_date` er en selvstændig kolonne

Konverteringen `timestamptz → hotel-lokal dato` er `STABLE`, ikke `IMMUTABLE`, og kan derfor hverken indgå i en genereret kolonne eller i en exclusion constraint. `check_out_date` sættes eksplicit af `BookingService.CheckOutAsync`. `effective_end_date` er en `GENERATED ALWAYS ... STORED`-kolonne beregnet af `start_date`, `end_date` og `check_out_date`.

### A-03 — `CheckOut` får en dato-parameter

`Booking.CheckOut(DateTimeOffset occurredAt, DateOnly checkOutDate)`. Domain må ikke kende tidszoner, så den hotel-lokale dato leveres udefra — parallelt med at `CheckIn` allerede får både `occurredAt` og `today`.

### A-04 — Ét sted for overlapsdefinitionen

`BookingRules` ejer definitionen. Infrastructure genererer constraint-prædikatet ud fra den, og en test i `Infrastructure.Tests` asserter at C#-funktionen og SQL-constrainten er enige om et sæt kendte tilfælde. Uden den test driver de fra hinanden.

### A-05 — `IClock` får `Today`

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }   // hotellets tidszone, fra HotelTimeOptions
}
```

Uden `Today` beregner hver service sin egen "i dag", og BR-06/BR-44/BR-104 er tilbage i det gamle systems problem.

### A-06 — Navne konsolideres

`Room.HousekeepingStatus` (ikke `Room.Housekeeping`). `CanReschedule` overalt (ikke `CanEdit` i DTO'en). Én regel, ét navn.

### A-07 — Rengøring får eksplicitte metoder i stedet for en mål-status

`ChangeHousekeepingStatusAsync(newStatus)` udgår. I stedet én service-metode pr. domæneovergang: `StartCleaningAsync`, `CompleteCleaningAsync`, `ReportServiceNeededAsync`, `StartServiceAsync`, `CompleteServiceAsync`, `MarkDailyCleaningDueAsync`, `MarkDepartureCleaningDueAsync`.

En generisk mål-status ville tvinge servicen til at oversætte (nuværende, ønsket) → metodekald via en tabel, altså reimplementere tilstandsmaskinen ét lag højere. `IRoomService` får dermed 15 use cases, ikke 9.

### A-08 — Statusovergange er idempotente for samme måltilstand

`TakeOutOfService()` på et rum der allerede er ude af drift er en no-op, ikke en exception. Ulovlige overgange kaster fortsat.

Begrundelse: mobilappen har ingen offline-understøttelse (D-11), så den retry'er over ustabilt netværk. En retry må ikke give en fejl til rengøringspersonalet. Konsekvens: `Status` fjernes fra `UpdateRoomRequest` — statusskift har sin egen use case.

### A-09 — Validering returnerer fejlkoder, ikke engelske strenge

`GuestRules.Validate` og `RoomRules.Validate` returnerer koder (`ErrorCodes.Guest.FirstNameRequired`). Application mapper kode → tekst; UI kan oversætte.

Det bryder tekstlig paritet med BR-52..BR-56 og BR-60, men reglerne handler om *hvad der kræves*, ikke om den engelske ordlyd — og systemet skal betjenes på dansk. Fase 3's audit måler reglen, ikke strengen.

### A-10 — Længdegrænser deles mellem lagene

Domain ejer konstanterne (`Guest.MaxNameLength` osv.) og guarder på dem; EF-konfigurationen spejler dem. Ellers fejler for lange værdier som `DbUpdateException` i stedet for som domænefejl. Registreres som ny regel, se afsnit 6.

### A-11 — Repositories returnerer projektioner til læsning

List- og detaljeforespørgsler returnerer DTO'er direkte fra repository (projektion i SQL). Entiteter hentes kun på kommandostier.

Det fjerner fælden hvor `ToListItem()` kaldes på en booking hentet uden `Include` og tavst giver tomme felter — og det er hurtigere.

### A-12 — Roller er dokumentation i Fase 1

Rollekolonnen i `HousekeepingStatus`-tabellen (rengøringspersonale, servicetekniker, ejer) beskriver *hvem der vil få lov* når autorisation bygges. I Fase 1 kan enhver kalde enhver overgang. **Skal stå eksplicit**, ellers tæller Fase 3's audit dem som implementerede regler.

### A-13 — Feature-først vinder over planens tegning

`Conventions.md` kræver feature-først; planens afsnit 3 tegnede `Interfaces/`, `Services/`, `DTOs/`. Conventions er source of truth. **Planens afsnit 3 skal rettes**, ellers modsiger to dokumenter hinanden fra dag ét.

### A-14 — DTO'er og mapping bor i Application

`Conventions.md` placerer Entity↔DTO-mapping i Infrastructure. Det kan ikke forenes med at DTO'er skal bo i Application, så et senere API kan genbruge dem — Application-servicen ville ikke kunne nå mapperen uden at vende afhængighedsretningen. Infrastructures mapping-ansvar er entitet↔database. **`Conventions.md` bør rettes på dette punkt.**

### A-15 — Primærnøgler

`BookingId`, `RoomId`, `GuestId` i C# → `booking_id`, `room_id`, `guest_id` i databasen. Løser U-19. Skal stå fast **inden** første migration.

---

## 5. Datamodel

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

`'[)'` er halvåbent, så afrejse- og ankomstdag samme dag ikke er overlap (BR-39). `status <> 4` = alt undtagen `Cancelled` blokerer.

**Check constraints:** `status BETWEEN 0 AND 4`, `end_date > start_date` (BR-103), `floor > 0` (BR-98), `capacity > 0` (BR-100).

**Indekser:** `ix_booking_room_id`, `ix_booking_guest_id`, `ix_booking_status_start_date` (BR-23 + BR-24), GiST-indekset fra exclusion constrainten, `ux_room_room_number` *(ny regel)*, `ix_room_status`, `ix_guest_last_name_first_name` (BR-68, BR-69). **Intet indeks på `passport_number`** — AES-GCM er randomiseret, så selv lighedsopslag ville ikke virke.

`xmin` bruges som optimistisk samtidighedstoken på alle tre tabeller.

---

## 6. Nye regler der kræver din godkendelse

Disse findes **ikke** i de 126 porterede regler. Fase 3's ledger-audit vil bogføre dem som "opfundet" hvis de ikke godkendes eksplicit. De føres i ledgeren som `BR-N-01` og frem.

| Id | Regel | Begrundelse |
|---|---|---|
| BR-N-01 | Rumnummer skal være unikt | To rum med samme nummer er meningsløst. Det gamle skema havde ingen constraint |
| BR-N-02 | En gæst kan slettes | Følger af D-05 (persondata slettes på anmodning). Fandtes ikke før |
| BR-N-03 | Længdegrænser håndhæves i Domain, ikke kun i databasen | A-10 |
| BR-N-04 | `floor > 0` og `capacity > 0` som databaseconstraints | Reglerne fandtes i C# (BR-98, BR-100), men ikke i skemaet |
| BR-N-05 | Gæstesøgning bruger BR-68's semantik overalt | Repo'et søgte kun på for-/efternavn (BR-116); ViewModel'en på alle ord i navn, land og e-mail (BR-68). Konsolideret til BR-68 — en søgning på et efternavn rammer nu også et land med samme navn |
| BR-N-06 | Rengøringsforløbets syv overgange | Rumstatus fandtes reelt ikke som regelsæt før (kun BR-81 og BR-126) |

---

## 7. Åbne punkter jeg ikke har afgjort

Ingen af dem blokerer Fase 2.

1. **`Floor > 0` forbyder stueetage.** Reglen er bevaret uændret fra BR-98, men et hotel med værelse 001 ville ikke kunne oprettes. Bevidst regel eller overset off-by-one?
2. **Der findes ingen fortrydelse af check-in/check-ud.** Ingen BR beder om det, så jeg har ikke opfundet den — men personale tjekker forkerte bookinger ind i praksis, og `CheckedOut` er en absolut slutstatus.
3. **`Guest.Anonymize()` er ikke designet.** D-05's retention kræver den formentlig, men X'erne (måneder/år) er ikke fastsat, og den kolliderer med invarianten "FirstName må ikke være tom" (BR-91).
4. **`RoomSize`-migrering.** Enum-omlægningen kræver en strategi for ukendte tekstværdier i eksisterende data — fejl eller fald tilbage til `Single`.
5. **`SalesService` er udeladt.** BR-31 giver altid 0, og der findes ingen pris i domænet. Sales-skærmen bør enten udgå af Fase 1 eller vises som eksplicit placeholder frem for en service der returnerer 0 og ligner en implementering.
6. **Krypteringsnøgle og rotation.** Fase 1 kan levere `EncryptionOptions` med `ValidateOnStart`, så appen nægter at starte uden nøgle. Rotation kræver en backfill-migration over hele `guest`-tabellen — ikke designet.
7. **Datamigrering fra det gamle system er ikke forudsat.** Skal produktionsdata flyttes, kommer der en engangsopgave: pasnumre skal krypteres ved indlæsning, og eksisterende bookinger skal kunne overholde exclusion constrainten. **De kan overlappe i dag, for der har aldrig været noget tjek.** Bør verificeres mod den kørende database før migrationen skrives.

---

## 8. Krav til Fase 2

- **Blazor Server skal give hver side sin egen DI-scope.** Gør den ikke det, får en bruger én `DbContext` for hele sin session, og to samtidige komponenthændelser kaster. Skal aftales, ikke opdages i drift.
- **Arkitekturtesten skrives først.** `Application` må ikke referere `Microsoft.EntityFrameworkCore` eller `Npgsql`. Testen håndhæver det; disciplin gør det ikke.
- **De 22 tests i `BookingOverviewViewModelTests` porteres først.** Tilstandsmaskinen er nu ren, så de kan køre direkte mod `Booking`-metoderne uden mocks. T-21 (`Pending → CheckedIn`) skal bestå uændret.
- **Constraint-paritetstesten** fra A-04 skrives sammen med den første migration.
- **BR-118 ("optaget fra middag") er en forretningsantagelse forklædt som pixelmatematik.** Geometrien hører i Web, men hvis hotellet har en faktisk check-in-tid, er det en domænepolitik der også burde gælde check-in-vinduet. Afklares når kalenderen bygges.

---

## 9. Rækkefølge for Fase 2

De fem filer der skal skrives først:

1. `Application/Common/Results/Result.cs` — alt andet afhænger af returtypen
2. `Application/Bookings/IBookingRepository.cs` — kontrakten både service og repository bygges mod
3. `Application/Bookings/BookingService.cs` — bærer flest regler; det Fase 3 måler op imod
4. `Infrastructure/Persistence/Configurations/BookingConfiguration.cs` — B-03, B-06, B-07 og B-08 mødes her, og migrationen genereres herfra
5. `Infrastructure/DependencyInjection.cs` — levetider, options-validering, `DbContext`-fabrik

Derefter agent-opdelingen fra planens afsnit 5: Domain → (Application ‖ Infrastructure) → fire Web-features parallelt.
