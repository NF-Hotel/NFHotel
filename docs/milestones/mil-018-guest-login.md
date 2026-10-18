# MIL-018 Guest login

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-018 |
| CrossReference | [BC-001], [MIL-016], [MIL-017], [ADR-0005], [ADR-0006] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Proposed | Claude (AI) | S01 | Initial version | pending |
| 2026-10-18 | Accepted | Claude (AI) | AI at the request of S01 | Accepted after RC-084 Go | pending |

---

## Purpose

Decide whether a guest may see and change a booking without the staff sign-in. Since MIL-016 a booking is read by its number alone: anyone who guesses or is given a number sees the booking, and its guest details, and may cancel or reschedule it. ADR-0005 left the own-resource check to a later milestone. This milestone lets a guest prove ownership with data they already hold, the booking number and the email of the booking, and makes the API refuse every guest call that is not about that guest's own booking.

## Decisions to record in ADR-0007

| # | Decision | Why |
| --- | --- | --- |
| 1 | A guest signs in with the booking number and the email of the booking; there is no guest account and no password | The guest has nothing to remember; no password storage, reset flow or mail service is needed |
| 2 | `POST /api/auth/guest-login` returns a signed token (JWT, HS256, the key of ADR-0005) with the role `Guest` and the booking id as claims, valid for 2 hours. The booking creation answer carries the same token, so the guest who has just booked is signed in | One token format and one key; the receipt after booking keeps working |
| 3 | An unknown number, a wrong email and an expired token give the same answer, at the same cost in time; the email is compared without case. After 5 failed attempts for a number in 15 minutes the next one is refused with 429 | Nothing tells an attacker whether a number exists; the throttle of ADR-0005 is reused per booking number |
| 4 | The guest permissions come from the one table (the `Guest` role): create a booking, edit an own booking, view own guest data. The passport number is not among them | Least privilege; the table stays the one place that says who may do what |
| 5 | The API checks ownership: a guest token reads, cancels or reschedules only the booking whose id is in the token and reads only the guest of that booking; any other booking or guest gives 403. Reading a booking by number without a staff or guest token gives 401. Creating a booking and listing free rooms stay public | Closes the open item of ADR-0005; the API is the authority, the pages are a convenience |
| 6 | The web application keeps the guest token in a second cookie scheme (`HttpOnly`, `Secure`, `SameSite=Lax`, expiry equal to the token), separate from the staff cookie; a guest never gets a staff page | A guest and a member of staff cannot be confused, and signing out of one does not sign out the other |
| 7 | Guest sign-in is refused for a booking that was cancelled more than 30 days ago | An old number and email must not give a long-lived way in |

## Deliverable

- ADR-0007 recording the decisions above, reviewed.
- Application and domain: the guest sign-in use case, the ownership rule, the permission table checks for the Guest role; tests that state allowed or refused for every guest operation.
- API: `POST /api/auth/guest-login`, the guest token in the booking creation answer, the ownership check on the booking and guest endpoints, the throttle per booking number.
- Web: a booking lookup page (number and email), the receipt and change pages for a signed-in guest, the guest cookie, sign-out; the receipt page no longer opens by number alone.
- Smoke test, wiki and deploy files.
- Unchanged: the staff sign-in and roles, TLS, the database and the network rules of ADR-0004.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A call to read a booking without a staff or guest token gets 401; creating a booking and listing free rooms still work without a token | Yes | No |
| 2 | A guest token for booking A gets 403 on booking B, on the guest of B, and on cancel and reschedule of B; on its own booking it is allowed | Yes | No |
| 3 | A wrong email, an unknown number and an expired or tampered token give the same refusal, with no difference in the answer text; the sixth failed attempt for a number in 15 minutes gets 429 | Yes | No |
| 4 | No answer to a guest token contains a passport number; a guest token on a staff endpoint gets 403 | Yes | No |
| 5 | In the deployed VM a guest can book, sign out, look the booking up with number and email, and cancel it; the guest cookie has `Secure` and `HttpOnly` | Yes | No |
| 6 | The permission table test states allowed or refused for every role (including Guest) and every permission, and equals the Access Control Matrix | Yes | No |
| 7 | The staff sign-in tests of MIL-016 and the TLS tests of MIL-017 still pass unchanged | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp`, `qc-programming-shell` and `qc-yaml` reviews have the verdict `Go`; ADR-0007 has a `Go` review | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-016 | The token format, the key, the throttle, the permission table and the cookie that are extended |
| MIL-017 | The guest cookie is `Secure` because the public port is TLS |
| ADR-0005 | The open item this milestone closes: the own-resource check |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 21 of an amended BC-001: a guest signs in with the booking number and the email and reaches only their own booking | All tasks |
| Success criterion 24 | Criteria 1 to 4 |
| ADR-0005 public flow and RC-076 follow-up | Tasks 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the token, the cookie and the deploy changes |

## Target Date

2027-03-19 — two weeks after MIL-017 (2027-03-05): one sign-in use case, one ownership rule in the API, a small set of guest pages and a deploy check.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 21, the scope item, success criterion 24 and the MIL-018 schedule row; review again. | No | |
| 2 | Record the decisions in ADR-0007 | The seven decisions above, with the options for the guest identity (booking number and email, guest accounts, an emailed link). | No | |
| 3 | Guest sign-in and ownership rule | The use case, the guest token, the throttle per booking number, the ownership check; unit tests for every allowed and refused case. | Yes | UC: guest views and changes own booking |
| 4 | API endpoints | `guest-login`, the guest token in the booking creation answer, 401 and 403 on the booking and guest endpoints; API tests. | No | |
| 5 | Guest pages | Lookup page, receipt and change pages, guest cookie and sign-out; page tests. | No | |
| 6 | Smoke test, wiki and deploy | The smoke test signs a guest in and checks the 403 on another booking; the wiki page. | No | |
| 7 | Prove it in the VM | Deploy to the Proxmox VE container; check criteria 1 to 5 and record the commands. | No | |
| 8 | Check and review | Build, format, test; reviews against `qc-programming-csharp`, `qc-programming-shell`, `qc-yaml` and `qc-adr`. | No | |

---

[BC-001]: ../business-case.md
[MIL-016]: ./mil-016-backoffice-login.md
[MIL-017]: ./mil-017-tls.md
[ADR-0005]: ../adr/adr-0005-backoffice-login.md
[ADR-0006]: ../adr/adr-0006-tls.md
