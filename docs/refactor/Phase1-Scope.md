# Phase 1 — scope and extension guarantee

> Written 2026-09-02 after a scope clarification: **the electives' features must not be built now.**
> The task of the refactoring is to leave the project in a state where they *can* be built when each guild gets to it.

---

## The principle

Phases 1 to 3 deliver **one running system: the staff back office on the web, ported from WPF.** Nothing else.

Everything the electives are to build — API, Flutter app, customer site, payment, roles — is built later by the respective guilds. Phase 1's responsibility toward them is **not to block the way**, not to pave it.

That gives two kinds of decisions, and only one of them has to be made now:

| | Decision | When |
|---|---|---|
| **Expensive to change later** | Touches all files or requires data migration | **Phase 1** |
| **Cheap to add later** | New project, new folder, new migration, middleware | When the guild builds it |

---

## Expensive decisions — must be made in Phase 1

These cost practically nothing to decide now and are expensive to change afterwards.

**1. All business logic in `Application` services — never in ViewModels.**
This is the only decision that really matters. If a rule lives in a Blazor ViewModel, the API must duplicate it, and the Flutter app inherits the duplicate. If it lives in a service, all later clients get it for free. It is exactly the mistake the old project made: 823 lines of business logic in `BookingOverviewViewModel.cs`.

*Test of whether it holds:* can a console app call `BookingService.CheckIn(id)` and have all the rules enforced? If yes, an API can too.

**2. Async all the way down.**
The old project is 100 % synchronous — zero `async` in the entire data access layer. Retrofitting async through repos, services and ViewModels later is a mechanical change that touches every single file. Conventions.md already requires it.

**3. Database naming: snake_case via `EFCore.NamingConventions`.**
Changing it later = all migrations and all raw queries. Costs a one-line setup now. (Q-14, Q-29)

**4. Enum storage: int, string or PostgreSQL enum.**
Changing it later = data migration. Recommendation: **int**, because the ordinal values 0-4 already exist in existing data. (Q-13)

**5. Date types: `date` vs. `timestamptz`, and the choice of time zone.**
Changing it later = data migration plus a review of all date comparisons. `StartDate`/`EndDate` are used as pure dates, `CheckInTime`/`CheckOutTime` as timestamps. (Q-05b)

**6. The booking rules themselves — Q-01, Q-02, Q-03.**
They are written into `Domain/Rules/BookingRules` and are what the ledger audit in Phase 3 measures against. If the system is built on a guess, you audit against a guess.

**7. The `RoomStatus` process.** *(added 2026-09-07)*
The mobile app has to control the cleaning process — daily cleaning, service, final cleaning (D-10). The current enum (`Available`, `OutOfService`, `Maintenance`) cannot carry it, and the old XAML dropdown already disagreed with it (BR-126). Enum values are persisted as int, so a rearrangement later is a data migration. The process is designed completely now, even though the app is built later. See `Decisions.md` F-01.

**8. Encryption of personal data.** *(added 2026-09-07)*
Passport number and similar must be encrypted (D-04). An encrypted column cannot be searched or sorted normally, so it changes the EF configuration and every query that touches the field. Cannot be retrofitted cheaply.

---

## Cheap insurance — costs nothing now, keeps the door open

Not features. Just decisions about *not* painting yourself into a corner.

| What | Why it is free now | What it would cost later |
|---|---|---|
| **Keep `Guest` a pure domain entity** — not a login entity | We are not building login now; we just avoid `Guest` becoming the account by accident | If `Guest` gets password fields and login responsibility, it has to be pulled apart again when Identity arrives |
| **Let `RoomSize` be an enum or lookup, not a free string** | The same work as porting it as a string | `RoomType` with a price can outgrow an enum; a magic string must be migrated |
| **Enforce role authorisation in `Application`, not only in the UI** | Follows from decision 1 | If the role is enforced only in Blazor, the API does not inherit it, and the Flutter app can call around it (D-03, F-03) |
| **Keep `Web` free of all logic — including presentation-near logic** | Follows from decision 1 | An API added later would have to recreate it |
| **Define DTOs in `Application`, not in `Web`** | The same work | DTOs in Web cannot be reused by an API without moving them |
| **Register services via interfaces in DI from the start** | Conventions.md requires it | An API's composition root must be able to reuse the registrations |

That is the whole list. None of the items is a feature.

---

## Deliberately postponed — built by the guild, not now

All of these are **cheap to add** when decision 1 holds. They are not to be designed in Phase 1.

| What | What it costs when it comes | Track |
|---|---|---|
| `NFHotel.Api` | New project referencing `Application`. Endpoints are thin calls to services that already exist | Mobile |
| Flutter app | Independent code base. Does not touch the .NET solution | Mobile |
| Customer site on the website | New folder in `Web`. New rules, yes — but they have to be defined anyway when the feature is built | Frontend |
| Auth implementation + JWT | Middleware, a migration, `[Authorize]` attributes. **Not** ASP.NET Identity — see D-01 | Cybersecurity |
| Roles and authorisation | Comes with Identity | Cybersecurity |
| `RoomType` with price | Migration + backfill. The enum from "cheap insurance" makes it easier | Domain |
| `Payment`/`Invoice` | New entities + migration | Domain |
| Blazor render modes | Set per component in .NET 8+. Retrofitted without problems | Frontend |
| CORS, rate limiting | Middleware configuration | DevOps / Cybersecurity |
| CI pipeline for Flutter | Separate pipeline. Does not touch the .NET side | DevOps |

**Consequence for `OpenQuestions-Tracks.md`:** Q-33 to Q-69 are still real questions, but they are **the guilds' questions when they build** — not blockers for Phase 1. The four real blockers are Q-01, Q-02, Q-03 and the five technical conventions above.

---

## What Phase 1 must deliver

1. `docs/Architecture.md` — folder structure (incl. `Api` as the target picture, cf. D-09), interfaces, DTO signatures, the eight decisions above decided and justified.
2. Empty projects that build: `Domain`, `Application`, `Infrastructure`, `Web` + the three test projects.
3. Answers to Q-01, Q-02, Q-03 written into the ledger, so Phase 2 builds against a fixed state machine.
4. A short "extension note" in `Architecture.md`: where an API, a customer site and Identity will attach — **as a description, not as code.**

Not in Phase 1: the `Api` project as code (it is drawn but not built), the customer side's rules, the auth implementation, prices, Flutter.

---

## One thing worth keeping from the previous round

Even though features are postponed, one finding is genuine and should be in `Architecture.md` as a note: **Blazor Server has no HTTP surface.** When the mobile guild arrives, they have to use an API — it cannot be avoided by "calling Blazor". It is not a problem to solve now, but it is worth having written down so it is not discovered as a surprise in the middle of the mobile course.
