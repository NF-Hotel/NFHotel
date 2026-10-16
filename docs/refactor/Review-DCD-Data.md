# Review — Jens' DCD for the data part

> 2026-09-09. A review of the draft held against the code that has already been built
> (`src/NFHotel.Domain/`) and against the ledger in `BusinessRules.md`.
>
> Short version: **the price model is strong and solves a problem we had parked.** There are four
> places where the draft and the built code disagree, and they must be settled before anyone writes
> a migration — not afterwards.

---

## 1. What is strong

**`RoomPriceSnapshot` is the right solution to a problem we have had open since Phase 0.**

The ledger's BR-31 says that revenue is always set to 0, because there is no price anywhere
in the domain. `SalesService` was left out for the same reason, and `/admin/revenue` is a
placeholder today. A per-night, immutable copy of the price as it looked at the time of booking solves
it properly: revenue can be reconciled backwards even when `RoomPrice` changes later, and
weekend surcharges fall naturally out of the model.

It is not just "a price class on the room" — it is the version that holds up to audit.
Keep it.

**`RoomPrice` with `validFrom`/`validUntil` + `PriceType`** is likewise well thought out.
Temporal pricing without overwriting history.

**`CleaningTask` and `MaintenanceTask` with assignment to `Staff`** give the mobile app something to
work with that the current model does not have — see point 4, where the two must be reconciled.

**`Guest.nationality` and `passport`** are relevant for a hotel in Cambodia, where
immigration reporting is a real requirement. Well caught.

---

## 2. Four conflicts that must be settled

### K-1 · `boolean isCancelled` vs. `BookingStatus` — most important

The draft has `boolean isCancelled` plus `checkInAt`/`checkOutAt`.
The built code has an enum with five states: `Pending`, `Confirmed`, `CheckedIn`,
`CheckedOut`, `Cancelled` — and **18 status transition rules** from the ledger, enforced in the
domain and covered by 68 tests.

A boolean plus two nullable timestamps can express *what has happened*, but not *what may
happen now*. "May this booking check in?" becomes a derivation from three fields instead of
one question to the domain — and that was exactly the mistake the old WPF system made.

**Recommendation:** keep the `BookingStatus` enum. `isCancelled` is redundant (`Status ==
Cancelled`). `checkInAt`/`checkOutAt` are kept as timestamps, not as the source of state.

### K-2 · UUID vs. int

The draft uses `UUID` as the primary key everywhere. The built code uses `int` identity, and
`BookingNumber` is derived as `FLZ-{BookingId:D6}` — the number the customer gets on the receipt
(BR-C-05).

Jens' `int BookingRef` next to `UUID id` is in effect the same idea as our booking number, so
the models are not incompatible. But the choice hits every single table, every foreign key and
the migration that has already been run.

**It is cheap to decide now and expensive in a fortnight.** The arguments: UUID is better if
data must be merged from several sources or generated client-side; int is easier to read in
logs and URLs and takes up less space in indexes. Both work.

### K-3 · Two truths about cleaning

The built code has `Room.HousekeepingStatus` with six states and seven transitions
(BR-N-06) — designed from the requirement that the mobile app must control the cleaning process:
daily cleaning, service, final cleaning.

The draft has `CleaningTask` with its own `CleaningStatus`, assigned to a `Staff`.

**If both exist without a rule about who decides, they will sooner or later
disagree.** A room can stand as `Clean` while an open `CleaningTask` exists on it.

Two ways, choose one:
- **(a) The task is the truth.** `Room.HousekeepingStatus` is derived from open tasks. A cleaner
  data model, but the room's state requires a lookup every time.
- **(b) The room's status is the truth.** `CleaningTask` is a work order and history — who
  did what when — but does not itself change the room's state without going through the
  domain method.

**Recommendation: (b).** It keeps the seven transitions and their guards, and the tasks become the
layer the mobile app assigns and logs in. But it is a decision, not a default.

### K-4 · `Room.variant : String` and the other free-text fields

We made `RoomSize` an enum (`Single`, `Double`, `Suite`) precisely because the old
system's free-text `RoomSize` was one of the 22 discrepancies Phase 0 found: no
CHECK constraint, no whitelist, and an XAML dropdown that disagreed with the enum.

The draft's `String variant` reintroduces it. The same applies to `marketSegment` and `mealPlan`.

**But there is a counter-argument worth hearing:** if the price depends on the variant, and variants
must be addable without a code change, a **lookup table** is better than an enum. That is
probably the right solution here — just not a free string.

---

## 3. Three things the draft lacks

**M-1 · Effective end date.**
The booking has `checkOutAt`, but nothing captures that an **early check-out releases the room**
for the remaining nights. The built code has `EffectiveEndDate = GREATEST(StartDate + 1,
COALESCE(CheckOutDate, EndDate))` and a PostgreSQL exclusion constraint built on it.

It is relevant for Jens' part: every availability query for pricing and quotes must
use the effective period, not the booked one. Otherwise the system does not offer a room that
is actually empty.

**M-2 · Occupancy vs. capacity.**
`adults`, `children`, `babies` are new fields, and `Room.capacity` already exists. But there
is no rule that the number of guests must not exceed the capacity. If there should be, it is a
new business rule that must go into the ledger as `BR-N-xx` — not something that just applies implicitly.

**M-3 · What is `Guest.ic`?**
The field is unexplained. Identity card? If so: it is personal data on a par with the passport number,
and D-04 (privacy by design) says passport numbers must be encrypted at rest. The same should apply to `ic`.

**Consequence Jens needs to know:** encrypted columns cannot be searched or sorted normally.
Our AES-GCM is randomised, so even an equality lookup fails. If you have to be able to look up a guest
by passport number, it requires a different solution from the one we have today.

---

## 4. What I propose happens now

1. **Jens and I settle K-1 to K-4 together** — they take twenty minutes and lock the data model.
2. **The price model comes in as it is.** `RoomPrice`, `PriceList`, `PriceType` and
   `RoomPriceSnapshot` are well thought out and solve BR-31. It just has to be hooked onto the
   existing `Booking` rather than replace it.
3. **`Staff` is clarified against the auth decision.** D-03 defines four roles
   (cleaning staff, service technician, manager/owner, guest). Jens' `Staff.Role` is the same
   concept. The question is whether `Staff` *is* the login entity or a profile next to it —
   exactly the same question Q-61 asks about `Guest`. It belongs with Michael and Jakob, not
   in the data model alone.
4. **All new rules in the ledger** as the `BR-N` series, so Phase 3's audit can tell "ported
   from the old system" from "invented now". Price rules, occupancy rules and
   cleaning-task rules are all new.

---

## 5. One thing worth saying out loud

The draft is drawn from the data part's needs, and that is healthy — it is how you discover that
`RoomPriceSnapshot` has to exist. But four of the five conflicts above arise because the
data model and the domain model were drawn separately.

They must end up as **one** model. Otherwise we get two truths about what a booking is, and that is
the problem the whole refactoring began with.

---

# Part 2 — Errors in the data model itself

> Added 2026-09-09. This part looks **only** at the draft as a data model: what goes wrong when
> things have to be stored, if we run 100 % according to the DCD as drawn? The conflicts with the
> existing code in part 1 are kept out.
>
> 18 findings, sorted by when they hurt.

---

## A · Breaks immediately

**F-1 · `status = ToDo` does not exist in either of the two enums.**
`CleaningTask` has `CleaningStatus status = ToDo`, but `CleaningStatus` is
`NOT_SCHEDULED, SCHEDULED, IN_PROGRESS, COMPLETED, REJECTED`.
`MaintenanceTask` has `MaintenanceStatus status = ToDo`, but the enum is
`PENDING, IN_PROGRESS, COMPLETED, CANCELLED, FAILED`.

The default value is not a valid value of the type. In both places. It does not compile, and
the column cannot get that default. Probably a leftover from an earlier version of the enum.

**F-2 · `RoomPriceSnapshot` has no primary key.**
All other classes have `UUID id`. The snapshot has `bookingId`, `roomPriceId` and `date` — three
fields, no identity. As drawn it is a table you cannot point at or update a single row in.

The natural key is probably `(bookingId, date)`, and that is probably the right
choice — but it must be stated, and then it must also be enforced (see F-11).

**F-3 · `RoomStatus` is used, but never defined.**
`Room.status : RoomStatus` refers to an enum that does not exist in the diagram, unlike
`CleaningStatus`, `MaintenanceStatus` and `PriceType`, which are all drawn out. Which values
can a room have?

---

## B · Multiplicities that make real cases unstorable

**F-4 · `Booking "1" — "1" Room  ' occupies` — a room can only be booked once. Ever.**
This is the most serious one. The relation says 1-to-1 at both ends. A hotel booking is
many-to-one: one room has many bookings over time.

With that model the hotel can store exactly as many bookings as it has rooms.
It must be `Room "1" — "0..*" Booking`.

**F-5 · `Guest "1" — "1..*" Booking  ' makes` — a guest cannot be created without a booking.**
`1..*` means at least one. But guests are created independently: a walk-in is registered at
reception before the booking exists, and admin has a guest overview with creation.

It must be `0..*`.

**F-6 · Every task must have an employee assigned.**
`CleaningTask "0..*" — "1" Staff` and likewise for `MaintenanceTask`. The multiplicity 1
makes assignment mandatory.

That contradicts `CleaningStatus.NOT_SCHEDULED` and `MaintenanceStatus.PENDING`: a task that
is not yet scheduled has by definition no one assigned. You cannot store "room 204 needs cleaning,
we do not yet know by whom".

It must be `0..1`.

**F-7 · `CleaningTask.cleanStartAt` is not nullable.**
`LocalDateTime cleanStartAt` (without `?`) against `cleanEndAt ?`. But a `NOT_SCHEDULED` or
`SCHEDULED` task has no start time yet.

Either the field must be nullable, or it must mean "planned start time" —
and then a field for the actual one is missing. As it stands, you are forced to store an
invented timestamp.

---

## C · The model allows data that contradicts itself

**F-8 · Nothing prevents double booking.**
There is no constraint that prevents two bookings on the same room in overlapping
periods. It is the most important integrity rule in the whole system, and it cannot be solved in
the application code alone — two simultaneous requests will both see a free room.

It belongs in the database. PostgreSQL can do it with `btree_gist` and an exclusion constraint
on `(room_id, daterange)`.

**F-9 · Two active prices on the same room are legal.**
`RoomPrice.validUntil ? ' null = currently active` plus `Room "1" — "0..*" RoomPrice`
means nothing prevents two rows with `validUntil = null` on the same room.

Which price then applies? The model cannot answer. A unique constraint is missing
(one active per room per `priceType`) or an exclusion constraint on the period.

**F-10 · When several `PriceType`s apply at once, there is no rule for which wins.**
The note explicitly says "Overlaps allowed via validFrom/validUntil". But a Saturday in
high season with an active campaign matches `BASE`, `WEEKEND`, `SEASONAL_HIGH` and `PROMO` at
once.

There is no priority, no order, no additive/exclusive marking. The `description`
example `"Base + Weekend Surcharge - $50"` suggests that they are added together — but that is a
calculation rule that exists only as free text in a description, not as data. It cannot be
recalculated or audited.

**F-11 · Nothing guarantees that there is one snapshot per night.**
The snapshot is per night, but without a key on `(bookingId, date)` you can store:
- two rows for the same date with different prices
- a 5-night booking with 3 snapshots
- a snapshot with a date outside the booking's period

All three are silent data errors that are only discovered when the accounts do not add up. Requires a unique key
plus a check that `date` lies in `[startAt, endAt)`.

**F-12 · `PriceList.isDefault : Boolean` cannot express "exactly one".**
The comment says "One default per room/variant". A boolean cannot enforce that — nothing
prevents zero defaults or five.

Requires a partial unique index (`WHERE is_default`) or another model, e.g. a pointer from the
room to its default list.

**F-13 · Three paths between `Room` and `RoomPrice` — they can contradict each other.**
The diagram has at the same time:
- `Room "1" — "0..*" RoomPrice` (has active pricing history)
- `Room "1" — "0..*" PriceList` (belongs to list)
- `RoomPrice "1" — "1" PriceList` (part of)

That is a cycle. Nothing prevents a `RoomPrice` whose `room` is 101, but whose `PriceList`
belongs to room 205. Redundant relations that are not tied together always
disagree sooner or later.

In addition: a `PriceList` named `"2024 Summer Rates"` sounds like a **global** concept, but
`Room "1" — "0..*" PriceList` makes each list one room's property. So one "2024 Summer Rates"
has to be created per room.

**F-14 · `MaintenanceTask` has both `note` and `updatedNote`.**
Two free-text fields where one is a correction of the other. It can hold exactly one
update — the second time someone corrects the note, the first is gone.

Either it is a real history (its own table with timestamp and author), or it is
one field. As it stands, it is half a journal system.

---

## D · Unspecified, but must be before the migration is written

**F-15 · No precision on the amounts.**
`BigDecimal` is the right choice — never float for money. But scale and precision must be pinned
in the schema (`numeric(10,2)` or similar). Without it you get
the database default, and roundings behave differently from environment to environment.

**F-16 · `Currency` is stored per row without being tied together.**
`RoomPrice.currency` and `RoomPriceSnapshot.currency` are both `= "USD"`. If the system is really
single-currency, the field is noise. If it is not, a constraint is missing that `pricePerNight`,
`discountAmount` and `couponDiscount` in the same row have the same currency — and that the snapshot's
currency matches the `RoomPrice` it was copied from.

**F-17 · `Booking.BookingRef : int` — no uniqueness, no generator.**
If it is the human-facing reference number, it must be unique. It does not say how it is
assigned: sequence, counter, or derived from something. Two bookings with the same ref is legal as the
model stands.

**F-18 · No way to delete personal data.**
`Guest` carries `email`, `phoneNumber`, `nationality`, `passport` and `ic`. Decision D-05
says personal data must be deletable at the guest's request, while booking data has its own
retention period.

With `Guest "1" — "1..*" Booking` and no anonymisation mechanism you have only two choices when a
guest asks for deletion: cascade the bookings away (the accounts disappear) or leave
orphaned rows. A concept of an anonymised guest is missing.

In addition: `passport` and `ic` are personal data that must be encrypted at rest. Encrypted columns
cannot be searched or sorted — if reception must be able to look up a guest by passport number, that is a
separate task that has to be solved, not a detail.

---

## Summary

| Category | Count | Consequence |
|---|---|---|
| Breaks immediately | 3 | Does not compile / cannot be stored |
| Unstorable real cases | 4 | Cannot register walk-ins, unscheduled tasks, or more than one booking per room |
| Data that contradicts itself | 7 | Double bookings, ambiguous prices, accounts that do not add up |
| Must be specified | 4 | Rounding errors, duplicate references, GDPR without a way out |

**F-4 is the one that would sink the demo** — 1:1 between room and booking means the hotel
can store ten bookings in total.

**F-8, F-9 and F-11 are the dangerous ones**, because they do not fail loudly. The system keeps running and
stores data that is wrong, and it is only discovered when someone counts.

None of this means the model is bad. The price model is well thought out, and the task model
solves something we lacked. It is the kind of errors you find by reviewing a draft — which
is why it is a draft.
