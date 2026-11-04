# MIL-059 Managers manage the staff accounts in the mobile app

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-059 |
| CrossReference | [BC-001], [MIL-029], [MIL-030], [ADR-0015], [ADR-0017] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Proposed | Claude (AI) | Not reviewed yet | Initial version, from the decision of S01 in chat: the administration panel of the earlier prototype app (`ZanjiOfficial/NFHotelRepo`, branch `mobilapplication`) in full in the mobile app, with the account fields, the API and the web back office it needs | pending |

---

## Purpose

The earlier prototype of the mobile app had an administration panel where a Manager created accounts and changed their name, e-mail address, password and role, or deleted them. The current app (`NF-Hotel/MobileApp`) has no such panel, and the API cannot do all of it: an account has no name or e-mail address, its role cannot be changed, it can only be disabled, and the app may not reach `/api/staff` ([ADR-0015]). This milestone brings the panel to the app on top of the rules of [ADR-0017], and makes the web back office offer the same. The gate decides whether a Manager can run the staff accounts from the phone without weakening those rules.

## Deliverable

- **Domain and database.** A staff account has an optional first and last name (always set together) and an optional e-mail address, unique when present and stored in lower case. The migration `AddStaffNameAndEmail` adds the three nullable columns and the filtered unique index `ux_staff_member_email`; existing accounts keep working without a name. The e-mail address is contact data only: the sign-in stays the user name.
- **API.** For a Manager: `POST /api/staff/{id}/name`, `POST /api/staff/{id}/email`, `POST /api/staff/{id}/role` and `DELETE /api/staff/{id}`, rate-limited like the other writes; creation takes the name and the address too; the list returns them. For every role: `POST /api/staff/me/name`, which names an account once while it has none. The sign-in session carries the name. A new role and a deletion end the sessions of the account (the security stamp, and a deleted account finds no row). The own account and the last active Manager can be neither disabled, deleted nor given another role (409). Every change is in the audit trail (`NameChanged`, `EmailChanged`, `RoleChanged`, `AccountDeleted`); the trail says that an address changed, not what it is.
- **Access from the app.** `/api/staff` joins the routes that accept the staff token alone ([ADR-0015], amended); the nginx filter of the app passes `/api/staff` and `/api/room-service-notes` (the second was missing, so the service notes of the app did not pass nginx).
- **Mobile app.** An administration screen for a Manager, reached from the rooms: a form for a new account (first and last name, user name, optional e-mail, temporary password, role) and the list of accounts with a menu per account (edit name, edit e-mail, reset password, change role, disable or enable, delete, with a confirmation before disabling and deleting). A member whose password was set by a Manager must change it before anything else; an account without a name is asked for it after the sign-in. Every member can change the own password from the rooms.
- **Web back office.** The staff page shows the name and the address, creates accounts with them, edits name, address and role in one form, and deletes an account after a confirmation.
- **Tests.** Domain, application, API, API client, web and PostgreSQL tests of the rules above; Flutter unit, repository and widget tests of the screens.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager creates an account with a name, an address and a role in the app; the new member signs in and must change the temporary password before the rooms show | Yes | No |
| 2 | A Manager renames an account, sets and clears its address, changes its role, resets its password, disables and enables it, and deletes it, in the app and in the web back office | Yes | No |
| 3 | A second account with the same address (in any case) is refused with 409 `staff.email_taken`; several accounts without an address are accepted | Yes | No |
| 4 | Deleting or changing the role of the own account, or of the last active Manager, is refused with 409; disabling them stays refused | Yes | No |
| 5 | After a new role or a deletion, an old token of that account gets 401; a deleted account cannot sign in | Yes | No |
| 6 | Each change writes one audit record with the actor and the target; the role change records the old and the new role; no record holds an e-mail address or a password | Yes | No |
| 7 | With the staff token alone, a Manager gets 200 on `GET /api/staff`, a Receptionist and Housekeeping get 403; `/api/staffXYZ` and every route outside the list stay 401 | Yes | No |
| 8 | nginx passes `/mobile-api/api/staff` and `/mobile-api/api/room-service-notes` to the API and answers 404 for other API paths (`ops/tests/test-nginx.sh`, `ops/tests/test-dev-lite.sh`) | Yes | No |
| 9 | An account without a name is asked for one after the sign-in; a second `POST /api/staff/me/name` gets 409 | Yes | No |
| 10 | The migration applies to an empty and to a seeded database, and the accounts of the seed still sign in | Yes | No |
| 11 | Tests pass, `flutter analyze` and the C# analysers are clean, and the C# and Flutter reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-029 | The app, its API client and the staff token |
| MIL-030 | The staff accounts, the password rules, the audit trail and the Manager endpoints |
| ADR-0015 | The amendment that lets the staff token alone reach `/api/staff` (must be `Accepted` before a release build) |
| ADR-0017 | The rules this milestone extends: the last Manager, the end of sessions, the audit trail |
| `NF-Hotel/MobileApp` | The repository of the Flutter screens; this milestone is the plan for its pull request |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| UC-006 Manage staff accounts | Tasks 1 to 5 |
| No objective of BC-001 covers account management in the app yet | S01 to add one, or to accept the milestone without it |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S02 for the mobile track |

## Target Date

2026-11-13 — for S01 to confirm. The code of tasks 1 to 6 exists on the branches of both repositories; review, the end-to-end check and the decision on [ADR-0015] remain.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Name and address of an account | `StaffMember` fields and rules, the migration, the repository lookups, domain and PostgreSQL tests | No | ADR-0017 |
| 2 | Manager endpoints | Rename, address, role and deletion use cases and routes, the own name, the audit actions, application and API tests | No | UC-006 |
| 3 | Access from the app | `/api/staff` in `StaffTokenRoutes` and the nginx filter (with the service notes), the ADR-0015 amendment, the access matrix, API and ops tests | No | ADR-0015 |
| 4 | Web back office | The staff page: name and address columns, the edit form, the deletion, view model tests | No | UC-006 |
| 5 | Mobile app | The administration screen, the forced password change, the name at the first sign-in, the change of the own password, unit and widget tests | Yes | UC-006 |
| 6 | End-to-end check | Run criteria 1, 2, 5 and 9 against the development server with the three development accounts | No | |
| 7 | Check and review | Tests, linters and the reviews against the C# and Flutter checklists; record the RC; update UC-006 with the new flows | No | |

---

[BC-001]: ../business-case.md
[MIL-029]: ./mil-029-mobile-app-staff-api.md
[MIL-030]: ./mil-030-staff-accounts-and-password.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0017]: ../adr/adr-0017-staff-passwords-and-audit.md
