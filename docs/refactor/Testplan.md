# Test plan — manual testing

> Updated 2026-09-07 after the UI overhaul. Covers the front page, customer booking and admin.
> The purpose is not to click through everything, but to prove the five things that are hard to get right.

---

## 0. Setup

```powershell
cd "C:\Users\Outlashed\Development\New folder\NFHotel"

# 1. Database: a PostgreSQL 16 instance you can reach.
#    How it is hosted is DevOps' area — the project only knows a connection string.
$env:NFHOTEL_CONNECTION = "Host=...;Port=5432;Database=nfhotel;Username=...;Password=..."

# 2. Schema
dotnet ef database update --project src\NFHotel.Infrastructure --startup-project src\NFHotel.Web

# 3. Test data
psql $env:NFHOTEL_CONNECTION -f db\seed_dev.sql

# 4. Run
dotnet run --project src\NFHotel.Web
```

The same connection string must be in `src\NFHotel.Web\appsettings.Development.json`.
The database user must be able to create extensions — the first migration creates `btree_gist`.

10 rooms, 8 guests, 12 bookings. All dates are relative to today, so check-in can always be tried out.

---

## 1. Automatic tests first

```powershell
dotnet test
```

Expected: **291 green** — 285 domain tests and 6 architecture tests.

The 6 architecture tests are worth a look: they fail if anyone references EF Core from Application, gives Domain a dependency, or lets a service return a domain entity instead of a DTO. It is the kind of thing that is otherwise discovered only three weeks later.

---

## 2. The front page — `/`

Must work **without** a database. Stop the container and reload if you want to prove it:

Cut the connection — stop the instance, or point the connection string at a port that does not answer.
The front page must still respond normally.

- [ ] Hero, three sections, button to `/book`
- [ ] No side menu (it is `PublicLayout`)
- [ ] Window narrowed to ~400px: no horizontal scroll

---

## 3. The customer's booking flow — `/book`

**The most important flow in the whole system.** It writes real data and hits the whole stack: Blazor → ViewModel → Application → Domain → EF → PostgreSQL.

**Happy path:**

1. Arrival today, departure in three days → the number of nights must count
2. Next → available rooms are shown. **Rooms 203 and 204 must not be included** — they are under maintenance and out of service (BR-C-02)
3. Choose a room → fill in details → submit
4. Receipt with booking number `FLZ-0000xx`
5. Copy the address, open it in a new tab → **the same receipt** (it is bookmarkable)

**What to look for on the receipt:** it shows room, dates, nights and status — but **neither name, e-mail, phone nor passport number**. That is BR-C-06, and it is deliberate: without login `FLZ-000123` is trivially guessable, so the page must not leak personal data to whoever guesses.

**Negative tests — these must be rejected:**

| Try | Expected |
|---|---|
| Departure before arrival | Error, cannot proceed |
| Arrival yesterday | Error — the past cannot be booked (BR-44) |
| Empty e-mail, or `anne` without `@` and `.` | Field error on the right field (BR-55) |
| Go back to step 1 and change the date | Entered details must **not** be gone |

---

## 4. Admin — `/admin`

**Dashboard:** today's arrivals, departures, rooms that need cleaning. The numbers must match what you see on the sub-pages — they come from the same source.

### `/admin/bookinger`

The seed data is made so the buttons can be tried out. This is where the state machine is proven:

| Booking | Try | Expected |
|---|---|---|
| Pending, arrival today | **Check in** | Works — without confirming first. That is B-01, walk-in |
| Pending, arrival in 7 days | — | **The check-in button does not exist.** Too early (BR-06) |
| CheckedIn | **Check out** | Works. Confirm and Cancel do not exist (BR-11) |
| CheckedOut | — | No buttons. Final status |
| Pending | **Cancel** | Confirmation first (BR-04), then status `Cancelled` — the row is not deleted (BR-13) |

**Important:** the buttons come from `Actions` on each booking, computed in the domain. If you see a button that gives an error when you press it, it is an error worth reporting — then UI and domain disagree.

**Create booking:** dates → available rooms → existing guest *or* new. Try both ways.

**Reschedule booking:** choose a `Pending`, move the dates. Try moving it on top of another booking's period on the same room → must be rejected.

### `/admin/rum`

- [ ] Filters on floor, size, status
- [ ] Room 104 is "cleaning in progress" → **Finish cleaning** works
- [ ] Room 101 is already clean → **the button does not exist** (no reason to finish a cleaning that is not in progress)
- [ ] Create a room. Try floor `0` → rejected (BR-98). Try an existing room number → rejected (BR-N-01)
- [ ] Edit a room: **there is no status field** — status is changed with its own buttons (A-08)

### `/admin/gaester`

- [ ] Create a guest **with a passport number**. It is the only way to see the encryption work
- [ ] Confirm in the database that it is encrypted:

```powershell
psql $env:NFHOTEL_CONNECTION -c "SELECT first_name, passport_number FROM guest WHERE passport_number IS NOT NULL;"
```

The field must be unreadable base64 — not what you typed. Open the same guest in the UI: the real value is shown. That is B-09.

- [ ] Search on a first name, a last name and a country — all three must give hits (BR-68)

---

## 5. What the UI cannot test

```powershell
psql $env:NFHOTEL_CONNECTION -f db\constraint_check.sql
```

Five inserts directly against the database, bypassing the application.

- **Test 1 must fail** with `ex_booking_room_period`
- **Tests 2 to 5 must give `INSERT 0 1`**

Test 4 is the most important: a booking on room 10 runs four days ahead, but the guest checked out yesterday. The room must be re-lettable from today. If it fails, A-01 is broken — and that was exactly the bug caught in the design phase.

Test 1 proves that double booking is **physically impossible**, even if two receptionists press at the same time. The old WPF system had no overlap check on creation at all.

---

## 6. Robustness

**The database disappears while the app is running.** Stop the instance and reload `/admin/rum`. Expected: a neutral Danish error message within a few seconds — **not** a stack trace, and not a page that hangs for a minute. Start the database again; the page must work on the next reload.

**Two tabs at the same time:** open `/admin/bookinger` in two tabs and perform actions in both. Each tab has its own state, and each call its own `DbContext`.

---

## 7. What you CANNOT test yet

- **Access control.** `/admin/**` is open to anyone who knows the address. Auth is postponed (D-01 to D-03). The band at the top of admin says so
- **Prices and revenue.** `/admin/omsaetning` is a placeholder. `RoomSize` is a category without a price
- **The mobile app.** Not built. The cleaning process in `/admin/rum` is the surface it will later call
- **Personal data deletion.** Retention (D-05) is not implemented; the X values are not fixed

---

## 8. Errors worth reporting

Not everything is equally interesting. These point to something real:

- A button that gives an error when pressed → UI and domain disagree about what is legal
- A raw exception or an English error code in the browser → error handling has a hole
- `constraint_check.sql` test 1 succeeds → the constraint is not active, the migration has not been run
- `constraint_check.sql` test 4 fails → `effective_end_date` is wrong
- Personal data on the receipt page → BR-C-06 is broken
- A passport number in plain text in the database → B-09 is broken
