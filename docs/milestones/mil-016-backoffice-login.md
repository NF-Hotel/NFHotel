# MIL-016 Login and roles for the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-016 |
| CrossReference | [BC-001], [MIL-015], [ADR-0004] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Proposed | Jens Tirsvad Nielsen | S01 | Initial version | [ed2e4b9] |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-068 Go | [bbef3bf] |

---

## Purpose

Decide whether the back office can go into operation on the public port. The pages under `/admin/**`
(dashboard, bookings, guests, rooms, sales) are reachable by anyone who can open the site, and they show guest
names, contact details and, on the guest page, the passport number. `BusinessRules.md` already says that they
must not go into operation without authentication, and D-01 to D-03 in `docs/refactor/Decisions.md` and the
Access Control Matrix (`docs/refactor/Access-Control-Matrix.md`) fix the intent: a staff member signs in, carries
a role as a claim, and may do only what the role allows (least privilege). This milestone makes that true for
the staff roles Receptionist, Manager and Housekeeping. The guest role (a guest looking at their own booking) is
left to a later milestone.

## Decisions to record in ADR-0005

| # | Decision | Why |
| --- | --- | --- |
| 1 | Staff accounts are stored in the database (user name, password hash, role, active flag) and reached through the API like all data. Passwords are hashed with a slow, salted algorithm (PBKDF2 from the platform library, never a fast hash); the plain password is never stored or logged | D-01: not ASP.NET Identity, a light own solution; the database stays reachable only through the API (ADR-0004) |
| 2 | The API gets a login endpoint that checks the password and issues a signed token (JWT) with the staff id and the role as claims and a short lifetime. The signing key is a secret in the API environment file, never in git | D-02: JWT, the role is a claim |
| 3 | The web application keeps the token in an HttpOnly, Secure, SameSite cookie after the sign-in and builds the signed-in user from it; `/admin/**` requires a signed-in user and shows the login page otherwise | The browser script cannot read the token; the pages need no change to ask who is signed in |
| 4 | The web application sends the staff token to the API together with its service token; the API refuses a staff endpoint without a valid staff token and checks the role. The Application layer holds the permission rule per use case (a role-to-operation table), so the same rule protects the API and the pages | Access Control Matrix: enforce in the Application layer, not only in the user interface |
| 5 | Roles and what they may do follow the matrix: Receptionist runs bookings, check-in and out and guests; Manager does that and also rooms, sales and staff accounts; Housekeeping sees rooms and their status and sets cleaning and out-of-service state only. Passport numbers are shown to Receptionist and Manager only | Least privilege; the passport number is the most sensitive field (D-04) |
| 6 | Sign-in is rate limited per user name and address, and a failed sign-in gives one message for an unknown user and a wrong password; the first Manager account is created by a deploy script from a secret in the environment file | Brute force protection and no user enumeration; no default password in git |

## Deliverable

- ADR-0005 recording the decisions above, reviewed.
- Domain and Application: a staff account and role model, the permission table, a sign-in use case, and a check of permission in each use case that the back office uses.
- Infrastructure and Api: the staff table with its migration, the password hasher, the token issuer and validator, the login endpoint, authorization on the staff endpoints, the rate limit.
- Web: the login and logout pages, the cookie, authorization of `/admin/**` by role, and navigation that shows only what the role may open.
- Deploy: the signing key and the first Manager in the env examples without secrets, `deploy-services.sh` passing them to the API only, the wiki page updated, and the EF migration applied by the deploy (closing the open item of RC-065).
- Tests: permission table tests for every role and operation, hasher and token tests (expired, tampered, wrong role), API tests for 401 and 403, page tests for the redirect to the login, a smoke check that `/admin` answers the login page without a session.
- Unchanged: the public booking pages and the guest-facing flow.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A request to any `/admin/**` page without a session is redirected to the login page, and an API staff endpoint without a staff token gets 401, in the deployed VM | Yes | No |
| 2 | For every role and every back office operation, a test states allowed or refused, and the table in the code equals the Access Control Matrix | Yes | No |
| 3 | A signed-in Housekeeping user gets 403 on bookings, guests and sales, and cannot see a passport number; a Receptionist gets 403 on rooms management and sales | Yes | No |
| 4 | A tampered, expired or wrongly signed token is refused; the signing key and every password hash are absent from git, logs and API responses | Yes | No |
| 5 | A wrong password and an unknown user give the same answer; the sixth failed attempt in the window is refused with 429 | Yes | No |
| 6 | The public booking flow works unchanged without signing in | Yes | No |
| 7 | The deploy applies the migration and creates the first Manager from the environment file; a second run changes nothing | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp`, `qc-yaml` and `qc-programming-shell` reviews have the verdict `Go`; ADR-0005 has a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-015 | The accounts live behind the database API, and the web application already carries the service token |
| ADR-0004 | The network rules stay as they are; login adds a second token on top of the service token |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 19 of an amended BC-001: the back office is available only to signed-in staff and each role sees only what it needs | All tasks |
| Success criterion 22 | Criteria 1 to 5 |
| `docs/refactor/Decisions.md` D-01 to D-04, `Access-Control-Matrix.md`, GitHub issue 24 (RBAC) | Tasks 2 to 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the deploy and secret handling |

## Target Date

2027-02-19 — four weeks after MIL-015 (2027-01-22): a new aggregate, token handling, a rule per use case and a deploy change.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 19, the scope items, success criterion 22 and the MIL-016 schedule row; review again. | No | |
| 2 | Record the decisions in ADR-0005 | The six decisions above, with the options considered (ASP.NET Identity, an external identity provider, cookie sessions without JWT). | No | |
| 3 | Sign in as staff | A staff member signs in with user name and password and signs out; the wireframe of the login page is kept with the use case. | Yes | UC-004 |
| 4 | Domain, Application and permission table | Staff account, role, the sign-in use case and the permission check in each back office use case; tests for every role and operation. | No | |
| 5 | Infrastructure and API | Staff table and migration, hasher, token issuer and validator, login endpoint, 401, 403 and rate limit; tests. | No | |
| 6 | Web | Login and logout pages, cookie, `/admin/**` authorization, navigation by role, the passport number hidden by role; page tests. | No | |
| 7 | Deploy | Signing key and first Manager in the env examples, migration in the deploy, smoke check for `/admin`, the wiki. | No | |
| 8 | Prove it in the VM | Deploy to the Proxmox VE container; check criteria 1, 3 and 7 and record the commands. | No | |
| 9 | Check and review | Build, format, test; reviews against `qc-programming-csharp`, `qc-yaml`, `qc-programming-shell` and `qc-adr`. | No | |

---

[BC-001]: ../business-case.md
[MIL-015]: ./mil-015-database-api.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ed2e4b9]: https://github.com/NF-Hotel/NFHotel/commit/ed2e4b984204da2953d3226666a063769998ff81
[bbef3bf]: https://github.com/NF-Hotel/NFHotel/commit/bbef3bf556044704b80b76a7817432bc2b6527cd
