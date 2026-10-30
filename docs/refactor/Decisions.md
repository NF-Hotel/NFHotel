# Decision Log

> Decisions made, not questions. Replaces the corresponding entries in `OpenQuestions.md` and `OpenQuestions-Tracks.md`.
> Format: what was decided · what it means for the code · when it applies.

---

## 2026-09-07 — round 1

### Security and access

**D-01 (Q-08) — Not ASP.NET Identity.**
A lighter auth solution is used (e.g. `simpleauth.net` — an example, not a chosen vendor).

**"Auth first, middleware" means order *within* the Cybersecurity track:** when the auth feature is built, it is built before roles and authorisation, and it is exposed as middleware. It does **not** mean that auth must be built before the rest of the refactoring. *(Confirmed 2026-09-07.)*
→ *Code:* auth is a separate layer, not something woven into the Web project. Application services must be callable without knowing the auth implementation.
→ *Applies:* the auth implementation is postponed, see `Phase1-Scope.md`. Only the interface has to be considered in Phase 1b, so the layer can be attached later without a rebuild.

**D-02 (Q-36) — JWT is implemented.**
→ *Code:* token issuing in the auth middleware, validation in both Web and the later Api.


**D-03 (Q-39) — Role-based access, four roles.**

NFHotel uses four roles for role-based access control (RBAC), aligned with the current implementation.

| Role | Responsibility |
|---|---|
| Guest | Hotel guest. Guest login and authorisation are planned for a later phase. |
| Receptionist | Handles guests and bookings. |
| Manager | Has extended administrative permissions. |
| Housekeeping | Handles cleaning and housekeeping tasks. |

The original role definitions have been updated to reflect the current RBAC implementation. Service technician is not a separate role in the current RBAC model.

Role-based access controls permissions for bookings, guest information, passport data, prices and housekeeping operations.

→ *Code:* JWT authentication identifies the staff member. Authorisation is enforced in the Application layer through `StaffAuthorizationService` and `RolePermissions`, not only in the UI.

→ *Access rules:* See `docs/refactor/Access-Control-Matrix.md`. Permissions follow the principle of least privilege.




**D-04 (Q-38) — Privacy by design. Passport numbers and similar personal data are encrypted. A delete policy is required.**
→ *Code:* **this is a Phase 1 decision**, not a later addition. An encrypted column cannot be searched or sorted normally, so it changes both the EF configuration and every query that touches the field. It must go into `Architecture.md`.

**D-05 (Q-41) — Deletion is handled in two separate tracks.**
- Booking data is deleted after X months.
- Personal data is kept for X years, or until the guest asks for deletion.
→ *Code:* two independent retention mechanisms. Soft delete of a booking (BR-13) is **not** the same as deletion of personal data. The X values are set later.

**D-06 (Q-09) — The credit leak in the old README is a finding, not a task.**
Lesson: read output before pasting it.

**D-07 (Q-40) — Secrets are stored on GitHub for now.**
The decision is deliberately temporary — the team does not judge that it has enough technology knowledge for a final choice yet.
→ ⚠️ **Must be made precise:** *GitHub Actions Secrets* (encrypted, correct) or *files in the repository* (= exactly the mistake from D-06). See the note at the bottom.

### Mobile app

**D-08 (Q-33) — Flutter.**

**D-09 (Q-35) — `NFHotel.Api` is part of the target architecture.**
Drawn in `Architecture.md` as part of the target picture. Not built in this round — see `Phase1-Scope.md`.

**D-10 (Q-34) — The purpose of the app is room status, not check-in.**
Users: cleaning staff, service technician, owner. Same app, different access by role (D-03).
Function: updating the room status through the cleaning process — **daily cleaning, service, final cleaning**.
→ ⚠️ *Consequence:* see "Findings" below. The current `RoomStatus` enum cannot carry it.

**D-11 (Q-37) — No offline support.**
Reason: status must be updatable from several devices without two versions of reality arising — one place shows the room as "ready", another as "needs service".
→ *Code:* all status changes go directly to the server. No local queue, no conflict resolution. In return the app must handle network errors visibly (not as in the old project, where check-in errors only went to `Debug.WriteLine` — BR-07/BR-10, Q-25).

---

## Findings that follow from the decisions

**F-01 — `RoomStatus` cannot carry the purpose of the mobile app.**

| Source | Values |
|---|---|
| `Models/Enums/RoomStatus.cs` | `Available` (0), `OutOfService` (1), `Maintenance` (2) |
| Old XAML dropdown | Available, Maintanance *(typo)*, Cleaning, Disabled |
| **D-10 requires** | daily cleaning, service, final cleaning, ready |

The two old sources never agreed (BR-126, Q-15), and neither covers the cleaning process the app has to control.

→ **This moves `RoomStatus` from "cheap insurance" to a Phase 1 decision.** Enum values are persisted as int in the database, so a later rearrangement is a data migration. The room status process should be designed completely in Phase 1 — even though the app is built later.

**F-02 — The mobile app rests on room status, not booking status.**
The ledger contains 18 status transition rules for `BookingStatus` and in practice **none** for `RoomStatus` (only BR-81: new rooms are `Available`, and BR-126: the XAML mismatch). The room status process is therefore a new small rule set that must be defined — but that is the mobile guild's work when they build it.
*Side benefit:* the app does not touch bookings, so it is independent of Q-01, Q-02 and Q-03.

**F-03 — Authorisation must be in the Application layer.**
D-03 gives four roles across two clients. If the role is enforced only in the Blazor UI, the API does not inherit it, and the Flutter app can call around it. Same principle as the business logic.

---

## Note on D-07

The irony is worth catching: D-06 is about credentials that were in plain text in a README, and D-07 says "secrets on GitHub".

- **GitHub Actions Secrets** → encrypted, correct, standard practice. No objection.
- **Secrets in files in the repository** → the same mistake as D-06, only in a newer repo.

If it is the first, the decision is fine as it is and can stand until more is known. If it is the second, `dotnet user-secrets` locally + Actions Secrets in CI is a free upgrade that needs no technology choice.

---

## Status

**Decided:** Q-08, Q-09, Q-33, Q-34, Q-35, Q-36, Q-37, Q-38, Q-39, Q-40, Q-41.

**Remaining before Phase 1:** Q-01, Q-02, Q-03 (the booking rules) · the room status process (F-01) · the five technical conventions in `Phase1-Scope.md`.
