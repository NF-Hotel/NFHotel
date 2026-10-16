# NFHotel 2.0

Staff back office on the web, ported from WPF. Blazor Server on top of Clean Architecture
with EF Core and PostgreSQL.

The architecture and its decisions are in `Architecture.md`. This file is only about getting
it to run locally.

---

## Prerequisites

- .NET SDK 9
- **A PostgreSQL 16 database you can reach** — how it is hosted is DevOps' decision, not the
  project's. The application only knows a connection string.
- `psql` or another SQL tool, if you want to run the scripts in `db/`
- `dotnet-ef`, if you need to run migrations:

```bash
dotnet tool install --global dotnet-ef
```

---

## Getting started

### 1. Get a database

The project makes one requirement: a **PostgreSQL 16** instance and a connection string to it.
How it runs — container, local installation, managed hosting — is DevOps' area and
must not be in this file.

The database needs a user with the right to create extensions. The first migration
creates `btree_gist`, on which the booking's exclusion constraint is built.

Put your connection string in `appsettings.Development.json` (see **Configuration** below).

### 2. Run the migrations

The design-time context reads its connection string from `NFHOTEL_CONNECTION` and otherwise
falls back to `Username=postgres;Password=postgres` — so set the variable:

```bash
export NFHOTEL_CONNECTION="Host=localhost;Port=5432;Database=nfhotel;Username=nfhotel;Password=nfhotel_dev"

dotnet ef database update \
  --project src/NFHotel.Infrastructure \
  --startup-project src/NFHotel.Web
```

The migration itself creates the `btree_gist` extension and the booking's exclusion constraint
(B-03), so double booking is impossible at database level.

### 3. Start the app

```bash
dotnet run --project src/NFHotel.Web
```

`http://localhost:5062` or `https://localhost:7060` (see `Properties/launchSettings.json`).

`http://localhost:5062` — front page. `/book` is the customer's booking flow, `/admin` is the staff
back office.

### 4. Test data

The database is empty after the migration, so all lists start empty. The seed script gives
10 rooms, 8 guests and 12 bookings with dates relative to today, so check-in can be tried out:

```bash
psql "$NFHOTEL_CONNECTION" -f db/seed_dev.sql
```

`db/constraint_check.sql` verifies that the exclusion constraint actually works — run it
after the seed script in the same way. See `docs/Testplan.md` for what is worth trying.

---

## Configuration

**Just cloned the repository?** `appsettings.Development.json` is deliberately not in git.
Copy the template and fill it in:

```bash
cp src/NFHotel.Web/appsettings.Development.json.example \
   src/NFHotel.Web/appsettings.Development.json
```

Generate your own encryption key:

```bash
openssl rand -base64 32
```

`appsettings.Development.json` contains three things the app refuses to start without:

| Key | Meaning |
|---|---|
| `ConnectionStrings:HotelDatabase` | PostgreSQL on `localhost:5432` |
| `Encryption:PassportKey` | 32 bytes base64. Passport numbers are encrypted at rest with AES-256-GCM (B-09) |
| `HotelTime:TimeZoneId` | The hotel's time zone. "Today" is computed from it, never from `DateTime.Now` (A-05) |

The encryption key and the password in the file are **for development only**. In production both
come from a secret store — they must never be committed.

If the key is missing, or is not valid base64, the app stops at start-up instead of at
the first guest update. That is deliberate.

---

## Build and test

```bash
dotnet build
dotnet test
```

---

## Where things live

```
src/
  NFHotel.Domain/          entities, value objects, rules. Zero dependencies
  NFHotel.Application/     use cases, DTOs, Result and error codes
  NFHotel.Infrastructure/  EF Core, Npgsql, encryption, clock. The only place with SQL
  NFHotel.Web/             Blazor Server, feature-first, composition root
```

In `Web` each screen lives in `Features/<Feature>/` as a `.razor` with markup and a
ViewModel with the logic. Components only call Application interfaces — no `DbContext`,
no repositories, no business rules (B-10).
