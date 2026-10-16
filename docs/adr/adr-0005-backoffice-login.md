# ADR-0005 Staff sign-in and roles for the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0005 |
| CrossReference | [BC-001], [MIL-016], [ADR-0004] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-17 | Proposed | Claude (AI) | AI as stand-in for S01 | Initial version | [1593b34] |
| 2026-10-17 | Accepted | Claude (AI) | AI as stand-in for S01 | Accepted after RC-069 Go | [1593b34] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The pages under `/admin/**` show guest names, contact details and passport numbers and change bookings and
rooms, and anyone who can open the public port can use them. `BusinessRules.md` says they must not go into
operation without authentication. `docs/refactor/Decisions.md` already decided the direction: not ASP.NET Identity
(D-01), a signed token with the role as a claim (D-02), role based access (D-03), and the Access Control Matrix
gives the roles Receptionist, Manager and Housekeeping and the principle of least privilege. Since [ADR-0004]
the web application holds no database access, so the accounts must live behind the API.

Forces: the booking pages are public and must keep working without a sign-in; the web application is a Blazor
Server application with one SignalR circuit per user; every call to the API already carries the service token of
the web application; nginx serves plain HTTP on the public port today; the matrix leaves some back office
functions (rooms, revenue) unassigned.

Options considered:

1. **ASP.NET Identity** in the web application. Complete, but it needs the database in the exposed process, which ADR-0004 removed, and brings pages and tables the hotel does not need (D-01).
2. **An external identity provider** (OpenID Connect). No passwords to keep, but one more service to run in the 12 GiB VM and an account at a third party for a hotel with a handful of staff.
3. **Own light solution:** staff accounts in the database behind the API, a login endpoint that issues a signed token, a cookie in the web application, a permission table checked at the API.

## Decision

We follow option 3.

- **Accounts.** A `StaffMember` (user name, password hash, role, active flag) in the table `staff_member`. The roles are Receptionist, Manager and Housekeeping; the Guest role stays for a later milestone. Passwords are hashed with PBKDF2-HMAC-SHA256, a random salt and 600 000 iterations (the OWASP figure), stored as one self-describing string so the cost can be raised later. The plain password is never stored or logged. A password has at least 12 characters.
- **Sign-in.** `POST /api/auth/login` takes user name and password and returns a signed token (JWT, HS256) with the staff id, the user name and the role as claims, valid for 8 hours (one shift). The signing key is a secret of at least 32 characters in the API environment file (`Staff__Token__SigningKey`). An unknown user, a wrong password and an inactive account give the same answer, and the unknown user costs the same time as a wrong password.
- **Throttle.** After 5 failed sign-ins of a user name in 15 minutes the next attempt is refused with 429 until the window has passed, whatever the password. This also stops guessing against one account; the cost is that someone can lock an account for 15 minutes, which is accepted for a back office with few accounts.
- **Two tokens.** The service token of ADR-0004 stays: it says the call comes from the web application. The staff token travels in a second header, `X-Staff-Token`. A staff endpoint needs both; the public booking endpoints need only the first.
- **Permissions.** One table in the Application layer maps each role to the permissions it holds, so the rule exists once: `BookingsView`, `BookingsManage`, `GuestsView`, `GuestsManage`, `RoomsView`, `RoomsManage`, `HousekeepingUpdate`, `SalesView`. The API checks the permission of each endpoint before it calls the use case (401 without a valid staff token, 403 when the role lacks the permission); the web application builds its page policies and its menu from the same table. The API is the authority, the pages are a convenience.
- **Roles.** The matrix is extended where it was silent. Receptionist: bookings, guests and rooms to read. Manager: everything. Housekeeping: rooms to read and the cleaning and service status of rooms, and nothing about bookings, guests or revenue. Passport numbers are part of the guest details, so only Receptionist and Manager can read them. Creating, editing and deleting rooms and taking them out of service belongs to the Manager; revenue to the Manager.
- **Public flow.** Creating a booking, reading a booking by its number and listing the free rooms need no staff token, as before. The booking receipt page stays reachable by its number; an own-resource check belongs to the guest login of a later milestone.
- **Web session.** The login page is a static server-rendered form that posts to the web application, calls the API, and signs in a cookie (`HttpOnly`, `SameSite=Lax`, `Secure` when the request is HTTPS, expiry equal to the token) that holds the claims and the staff token. The cookie is encrypted by ASP.NET Core data protection. `/admin/**` requires a signed-in user and redirects to `/admin/login`. Each use-case call copies the staff token from the circuit to the call.
- **First account and schema.** The API applies the pending migrations at start when `Database__MigrateOnStart` is true, and creates a first Manager from `Staff__InitialManager__UserName` and `__Password` when the table is empty. A second start changes nothing. Further accounts are made with `dotnet NFHotel.Api.dll create-staff <user> <role>`, the password read from `STAFF_PASSWORD`.

## Consequences

**Positive:**

- The back office can be put on the public port: no page and no staff endpoint answers without a sign-in, and each role reaches only what it needs.
- The passport number is read only by two roles, and the check is in the API, so a changed page cannot bypass it.
- No new service and no third party; the web application still holds no database login.
- The migration step closes the open item of RC-065: the deploy no longer leaves the schema to a manual script.

**Negative:**

- We own password storage and token handling, which is security-critical code: it is kept small, uses platform primitives only, and has tests for tampered, expired and wrongly signed tokens.
- Without HTTPS at nginx the cookie is not `Secure` and a password crosses the network in plain text. TLS at the public port is a separate decision that this ADR does not take; until then the sign-in must not be used over an untrusted network.
- The signing key and the first Manager's password are two more secrets to create and rotate by hand.
- A stolen token works until it expires (8 hours); there is no revocation list. Deactivating an account stops new sign-ins only.
- The throttle is in memory: a restart of the API clears it.

Quality attributes: security improves (authentication, least privilege, defence in depth with the service token); usability is neutral (one sign-in per shift); maintainability is neutral (one permission table, tested against the matrix).

## Affected Artifacts

- [MIL-016] — decided here; its tasks build and deploy it
- [ADR-0004] — adds a second token on top of the service token
- [BC-001] — objective 19

---

[BC-001]: ../business-case.md
[MIL-016]: ../milestones/mil-016-backoffice-login.md
[ADR-0004]: ./adr-0004-database-api.md
[1593b34]: https://github.com/NF-Hotel/NFHotel/commit/1593b34bb7cc020a8564f1030f49e2c395803ad4
