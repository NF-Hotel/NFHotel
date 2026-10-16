# Open Questions by Elective Track

> Updated 2026-09-02. Supplement to `OpenQuestions.md` — same Q-ids, grouped by who owns the decision.
> Q-01 to Q-32 come from the Phase 0 analysis. Q-33 and up are new, raised by the track split itself.
>
> ✅ **11 questions were answered 2026-09-07** — see `Decisions.md` (D-01 to D-11): Q-08, Q-09, Q-33 to Q-41. The decision log takes precedence over this file.
>
> ⚠️ **Read `Phase1-Scope.md` first.** The electives' features are not built in this round. Most questions here are **the guilds' questions when they build their feature** — not blockers for Phase 1. Only Q-01, Q-02, Q-03 plus five technical conventions must be answered before Phase 1. The rest are noted so they are not lost.

---

## ⚠️ Read first: two things the track split reveals

**1. The four heaviest blockers belong to none of the electives.**
Q-01 to Q-04 are domain questions — what *is* a booking, when may one check in, does a room cost money. They cannot be delegated to a guild, because all five tracks build on the answer. They must be decided jointly, by the whole team, before Phase 1. See the section "Shared domain" at the bottom.

**2. The mobile app in Flutter is settled — and changes the target architecture. [updated 2026-09-02]**
The electives are requirements, not choices. The mobile app is built in **Flutter**. That settles Q-33 and Q-35, and section 3 of the plan is updated to five projects + a `mobile/` folder.

- **Blazor Server has no HTTP API** — it runs over SignalR between browser and server. A mobile app cannot use it. `NFHotel.Api` is therefore mandatory, not optional.
- **Flutter is Dart.** Not one line of C# can be shared with the backend — unlike .NET MAUI, where `Domain` and `Application` could be referenced directly. **The API contract is thus the only integration point**, and must be treated as an independent artifact: an OpenAPI spec from `Api`, from which the Dart client is generated.
- **JWT from the start.** Cookies are not enough. **Q-08 (Identity) can no longer be postponed** — it moves from "later" to Phase 1b.
- **Serialisation must be decided deliberately.** Dart and C# do not agree on defaults for enums and dates. See Q-53 and Q-54.
- **Two CI pipelines.** .NET and Flutter share nothing beyond the spec. See Q-42.
- **Phase 2 gets one more agent:** `Api` is built in parallel with the Web features, since both depend only on `Application`.

---

## DevOps

| Id | Question | Why it is urgent |
|---|---|---|
| Q-30 | Test database for `Infrastructure.Tests`: Testcontainers, a fixed instance, or EF InMemory? | **DevOps owns the answer.** Testcontainers requires Docker on developer and CI machines — an infrastructure decision, not a project decision |
| Q-31 | Hosting target for database and app | **DevOps owns the answer.** The project knows only a connection string; the topology locks the deployment diagram in Phase 2 |
| Q-07 | What do we do with the 40 DB-dependent repo tests? | They cannot run in CI as they are |
| Q-06 | MSTest or xUnit? | Structure of the test projects in Phase 1b |
| **Q-42** | Which CI platform, and when is it set up? | See the note below about the Windows lock |
| **Q-43** | Are EF migrations run automatically on deploy, or manually? | Affects `Program.cs` and the pipeline |
| **Q-44** | `git init` in the new solution — when, and which branch strategy? | The plan says: **before the first agent writes anything**. That has not happened yet |
| **Q-45** | How many environments? (local / staging / prod, or local only) | Configuration model and secrets |

**Note — CI gets two pipelines.** The .NET solution and the Flutter app are built with separate toolchains and share nothing beyond the OpenAPI spec. Consider letting the .NET pipeline publish the spec as an artifact, so the Flutter pipeline can fail if the contract is broken — it is the cheapest way to catch contract breaks.

**Note — the Windows lock disappears.** The old test project is `net8.0-windows`, because it references the WPF project directly. That ties CI to Windows runners. The new solution has no WPF dependency, so everything can run on Linux runners — faster and cheaper. Worth mentioning as a concrete gain.

**Note — there is no `.git` in the old project** (it is an unpacked zip). So there is no history to inherit, and no diff trail on the old team. The plan's point about `git init` before the first build agent is more important than it sounds: without it you cannot see what the agents have changed.

---

## Cybersecurity

| Id | Question | Why it is urgent |
|---|---|---|
| Q-09 | Azure SQL credentials in plain text in `Database/README.md:18` | **Must be rotated regardless.** The password is exposed in a project shared between groups |
| Q-08 | ASP.NET Identity now or later? | ✅ **Settled by Flutter: now.** Only *which* model remains — Identity + JWT, or something lighter. JWT also means: token lifetime, refresh, and where they are stored on the device |
| **Q-38** | Passport number, e-mail, phone and country are personal data. What is the GDPR position? | Affects schema, logging and who can see what |
| **Q-39** | Which authorisation model? | ✅ **No longer optional** — the customer site requires at least the roles staff and customer, and that a customer sees only their own bookings. Open: should staff be subdivided (receptionist / manager)? |
| **Q-40** | `dotnet user-secrets` locally — and what in production? (env vars, Key Vault, other) | Configuration model in Phase 1b |
| **Q-41** | Bookings are soft-deleted (BR-13). What about guests — right to erasure? | Conflict between soft delete and GDPR art. 17 |

**Note — there is no authorisation model at all.** All 23 rules in the ledger of the "access" type are about UI state (is a booking selected, are we in edit mode), not about who is logged in. There is no `User` or `Employee` concept in the domain. The system assumes one user with full access to everything — including all guests' passport numbers.

**Note — the old project is actually safe against SQL injection.** The analysis found no string interpolation into SQL; everything uses parameters or stored procedures. The one place with `$"%{name}%"` (`GuestRepo.cs:130`) builds a *parameter value*, not SQL. It is a clean result and worth mentioning in the report as a baseline that must not be degraded. (The real weakness is `%`/`_` not being escaped and acting as wildcards — a functional detail, not a vulnerability.)

---

## Mobile app

| Id | Question | Status |
|---|---|---|
| ~~Q-33~~ | Platform | ✅ **Settled: Flutter** |
| ~~Q-35~~ | Should `NFHotel.Api` be included? | ✅ **Settled: yes, mandatory.** Section 3 of the plan is updated |
| ~~Q-36~~ | Token-based auth from the start? | ✅ **Settled: yes, JWT.** Follows from Flutter. See Q-08 |
| ~~Q-34~~ | Who is the user? | ✅ **Settled: staff only.** The website covers both staff and customers |
| **Q-37** | Offline support? | Open. If yes, it changes the data model (sync, conflicts, local ids) — a big bite |
| **Q-52** | Is the Dart client generated from the OpenAPI spec, or written by hand? | Recommendation: generated (`openapi-generator`, `swagger_dart_code_generator`). Otherwise the contract drifts apart |
| **Q-53** | Enums over the API: int or string? | Dart and C# have different defaults. Connected to Q-13 (enum mapping in the DB) |
| **Q-54** | Dates over the API: ISO 8601 with or without time zone? | Connected to Q-05b. `date` and `timestamptz` must be serialised differently |
| **Q-55** | API versioning from the start (`/api/v1/`) or later? | Cheap now, expensive later |
| **Q-56** | CORS — which origins, and how in dev vs. prod? | Also affects DevOps (Q-45) |
| **Q-57** | Who owns the API contract: backend or mobile guild? | A process decision, but it determines who blocks whom in Phase 2 |
| **Q-58** | Flutter state management: Riverpod, Bloc, Provider? | The mobile guild's own choice, but should be decided before Phase 2 |
| **Q-59** | Is the design system/colours shared between Blazor and Flutter? | Connected to Q-47 (component library) |

**Consequence of Q-34 (staff app):** the app reuses BR-06 to BR-10 directly from the ledger and requires **no new business rules**. It still demonstrates the whole layering: Flutter → API → Service → Repository → DB. The mobile guild is thus not blocked by Q-04 (prices) — the website's customer side, however, is. See the section "The customer side" below.

**API scope follows:** `NFHotel.Api` needs only staff endpoints to begin with. If the customer side should at some point also use the API (for example if you later want a customer app), that is an extension — not a redesign, as long as endpoints are authorised per role from the start.

**Bonus for the AI Integration report:** a Flutter app on top of the same `Application` layer is the strongest possible proof that the Clean Architecture split holds. Two clients in two languages, zero duplicated business logic, and a ledger that can be audited against both.

---

## ⚠️ The customer side — a new area with no coverage in the ledger

The website has **two audiences**: staff and customers. This is the biggest scope extension so far, and it is not visible in any Phase 0 material.

**All 126 rules in the ledger are staff rules.** The old WPF project was a pure back-office app — all 7 screens (calendar, guest overview, room overview, sales, guest policy) are staff screens. There are **zero** customer rules in the old system, because there was never a customer-facing surface.

Consequences:

- **The customer side's business rules cannot be derived from the old project.** They must be defined from scratch in Phase 1 and added to the ledger as a new series (e.g. BR-C-01 onwards), clearly separated from the ported rules. `verify:ledger-audit` in Phase 3 must be able to tell "rule kept from the old system" from "rule invented now".
- **Q-04 (RoomType with price) is effectively settled.** A customer cannot book without seeing a price. The price model goes from an open question to a precondition — and thereby also `SalesService` and the revenue calculation (BR-31, which today always returns 0).
- **Q-39 (authorisation model) is no longer optional.** At least two roles: staff and customer. A customer may only see their own bookings — that is an authorisation rule, not a UI state, and it does not exist in the system today.
- **The attack surface becomes public.** See the Cybersecurity track: rate limiting, account enumeration, booking spam.

| Id | Question | Owner |
|---|---|---|
| **Q-60** | What can a customer do on the website? Search + book, see own bookings, cancel, edit profile? | Frontend + domain |
| **Q-61** | Is `Guest` the same as a customer account, or is `User` a separate entity linked to `Guest`? | Domain — **blocking for Phase 1** |
| **Q-62** | May a customer cancel or change their own booking, and how close to arrival? | Domain. New rule — does not exist in the ledger |
| **Q-63** | Payment on the customer side, or "pay on arrival"? | Domain. Payment would pull `Payment`/`Invoice` in (section 7 of the plan) |
| **Q-64** | Blazor render modes: static SSR for public pages + interactive server for staff? | Frontend — see the note below |
| **Q-65** | Should the customer side be publicly accessible and findable (SEO)? | Frontend + DevOps |
| **Q-66** | Can you book without an account (guest checkout), or is login required? | Domain + Cybersecurity |
| **Q-67** | Rate limiting and bot protection on public booking? | Cybersecurity |
| **Q-68** | Who may see passport numbers? The customer their own, staff everyone's? | Cybersecurity (sharpens Q-38) |
| **Q-69** | E-mail confirmations on booking? | Domain + DevOps (SMTP in the pipeline) |

**Note — Blazor Server and a public site.** Blazor Server holds a SignalR connection per visitor. For 10 simultaneous receptionists that is unproblematic. For a public customer site it means server state per anonymous visitor, sensitivity to poor connections, and no indexable HTML out of the box. .NET 8+ solves it with render modes per component: static SSR on the public pages, interactive server on the staff screens. It should be decided in Phase 1b, not discovered in Phase 2.

---

## Frontend

*(Valdemar's guild)*

| Id | Question | Why it is urgent |
|---|---|---|
| Q-26 | `GuestPolicy` and `Sales` are empty screens, each with its own menu item. Built or dropped? | Sales depends on Q-04 (prices) |
| Q-27 | "Payment Status" is hard-coded "Coming soon…" | Depends on whether `Payment`/`Invoice` are included |
| Q-28 | Empty states — should all 7 screens have them in Blazor? | Component design |
| Q-17 | Week and month views do not filter by period. Bug or feature? | Is the rule kept, or fixed? |
| Q-21 | Search locks the view permanently to year | Same |
| Q-23 | Enter in the guest search field sorts instead of searching | Same |
| Q-24 | "Cancel Booking **Permanently** From System?" contradicts soft delete | Wording or behaviour? |
| Q-25 | CheckIn/CheckOut errors are invisible to the user (only `Debug.WriteLine`) | Must be fixed — status is changed in memory before the error is swallowed |
| **Q-46** | Calendar timeline: where does the converter logic (BR-117 to BR-120) end up? | See the note below |
| **Q-47** | Component library: MudBlazor, Radzen, or own components? | Locks all of Phase 2's Web work |
| **Q-48** | Blazor Server is stateful per user. What happens when two receptionists book the same room at the same time? | Real-time update or optimistic locking |

**Note — the calendar is the most expensive component.** `BookingOverviewView.xaml` is 1,093 lines, and four real business rules are hidden in its WPF converters: the width is the number of *nights* (BR-117), bookings are drawn from the middle of the check-in day (BR-118), stays that start before the period are clipped (BR-119), and bookings without a room are omitted entirely (BR-120). WPF converters do not exist in Blazor — the logic has to go somewhere, and it should end up in a ViewModel or a pure calculation class, not in Razor markup. It is the one component where "rewrite, not port" hurts most.

---

## AI Integration

| Id | Question | Why it is urgent |
|---|---|---|
| **Q-49** | What do you measure? Context use, wall-clock, number of findings per agent, errors caught by the verification agent? | Must be decided **before** Phase 1, otherwise there is no baseline |
| **Q-50** | Deterministic `Workflow` script or prompt-driven orchestration? | Section 5 of the plan. A script makes the fan-out code and is easier to document |
| **Q-51** | Is the ledger audit (Phase 3) the primary proof that the refactoring is complete? | If yes, the ledger must be frozen after Phase 1 |

**Material you already have from Phase 0** — worth writing down now while it is fresh:

- **Context isolation measurably worked.** Five agents each read their own area. `analyse:viewmodels` found 90 rules in one pass without having seen SQL, tests or views.
- **Independent verification caught real contradictions.** `analyse:tests` concluded from T-20/T-21 that you can check in without confirming — and `analyse:viewmodels` independently reached the same conclusion from the code. Two agents without shared context pointing to the same hole is stronger evidence than one agent reading everything.
- **The agents found things the old team did not know:** two deleted tests (`DELETE TESTS` and `UPDATE TESTS` with empty gaps), 22 discrepancies between model, schema and diagrams, and that the README describes a different version of the code than the one that is there.
- **Narrow contracts gave honest answers.** Four of five agents ended with "I cannot determine that within my reading area" instead of guessing — e.g. `analyse:repos`, which could not see who instantiates repositories, and said so.
- **The PostgreSQL switch became cheap.** The decision came after the analysis and hit only one section of the plan and four questions — because the analysis was layered from the start.

---

## Shared domain — cannot be delegated

These four must be decided by the whole team together, before Phase 1.

| Id | Question | Who it affects |
|---|---|---|
| **Q-01** | Is `Confirmed` a precondition for check-in? Today: no (BR-06, confirmed by test T-21) — but the dialog says "Payment has been received" | Everyone. The state machine is the core |
| **Q-02** | Does a `CheckedOut` booking release the room for the period? Two contradicting rules today (BR-19 vs. BR-39) | Backend, Frontend, Mobile |
| **Q-03** | Where does the overlap check live, and is it transactional? There is **none** today on creation | Backend, DevOps (DB constraint) |
| **Q-04** | Should `RoomType` with price be included? | ✅ **Effectively settled by the customer side: yes.** A customer cannot book without a price. What remains is *how* — price per night per `RoomType`, seasonal prices, or something else |

**Q-61 is the new most expensive one.** The relationship between `Guest` (a person in a booking) and `User` (someone who can log in) is the foundation under authorisation, the customer side and JWT issuing. If you choose wrongly, it hits Domain, Application, Api and both clients.

**Q-04 is no longer a yes/no, but a how.** The price model still changes the domain model, the ER diagram and the use case list — three of Phase 1's deliverables.

---

## Remaining questions without a fixed owner

Not blocking, but must not be lost. Full description in `OpenQuestions.md`.

- **Data model:** Q-05 (which old schema is the reference), Q-05b (`date` vs. `timestamptz`), Q-10 (`BookingNumber` persisted or derived), Q-11 (passport number mandatory — overlaps Q-38), Q-12 (deduplicate guests on e-mail), Q-13 (enum mapping), Q-14 (casing/naming), Q-15 (room statuses in XAML do not match the enum), Q-16 (seed data)
- **PostgreSQL:** Q-29 (snake_case via `EFCore.NamingConventions`), Q-32 (use `jsonb`/arrays or keep the schema portable)
- **Behaviour bugs:** Q-18 (edited guest loses its id), Q-19 (new guest disappears at the next search), Q-20 (existing guest's changes are not saved), Q-22 (no upper limit on check-in)
