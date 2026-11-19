# ADR-0047 Guest sign-in with the booking number and the email

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0047 |
| CrossReference | [MIL-018], [MIL-079], [MIL-016], [MIL-017], [ADR-0004], [ADR-0005], [ADR-0006], [BC-001], [UC-010], [RC-358], [ADR-0049], [MIL-085], [RC-417] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; task 2 of MIL-018, written by the AI<br>Go in RC-388; accepted by the AI as stand-in for S01 | [9e72b0e] |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | The current state names ADR-0049: the booking reference in place of the number, limits of the guest sign-in per reference and per address, guest routes without a key, and one lookup (MIL-085)<br>Go in RC-417; accepted by the AI as stand-in for S01 in autonomous mode | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-018] lists the seven decisions that this record takes (task 2). The AI accepted the milestone on
2026-11-14 as stand-in for S01, at S01's word in chat ([RC-358]); S01 owns it and holds the approvals of S03
for the token, the cookie and the deploy changes. This record is reviewed with the
code of the milestone.

**The problem.** Objective 21 of [BC-001] asks that a guest reaches the own booking and the own guest details, and
nobody else's, with no account and no password to keep. Since [MIL-079] (2026-11-12) the receipt page opens a booking
only with its number and the email of the booking, and the application programming interface (API) reads a booking by
its key only with `Bookings.View`. A guest can see the receipt but cannot change the booking: cancel and reschedule
need `Bookings.EditOthers`, which only staff hold. [ADR-0005] left the check that a guest touches only the own booking
to a later milestone.

**Forces.**

- The booking number is `FLZ-` and the key of the booking (BR-109), so it can be guessed; the email is the part the
  guest proves. An email is not a strong secret, so the number of attempts and the life of a sign-in must be small.
- [MIL-079] already took three decisions that this record keeps as taken: the guest gives the number and the email of
  the booking, and the email is compared without letter case and surrounding spaces; an unknown number and a wrong
  email get the same answer, and the booking is read also when the email turns out wrong; the throttle of [ADR-0005]
  counts failed lookups per booking number and refuses the sixth in 15 minutes with 429 (Too Many Requests).
- The API is the authority, and the web application is its only caller with the API token ([ADR-0004],
  [ADR-0005]). The staff token is a JSON Web Token (JWT) signed with HS256 (a hash-based message
  authentication code with SHA-256) and the key `Staff__Token__SigningKey` ([MIL-016]); every new secret is one more to create and rotate by hand.
- The public port uses Transport Layer Security (TLS) since [MIL-017] ([ADR-0006]), so a cookie can be `Secure`.
- The passport number is personal data (D-04): only Receptionist and Manager read it.
- One browser can hold a staff sign-in and a guest sign-in at once, at the reception desk or in a test, and neither
  may act for the other.
- No mail service runs on the virtual machine, and [MIL-018] leaves the database unchanged.

**Options considered** for the identity of a guest:

1. **Booking number and email.** The guest proves what the booking already holds, and nothing new is stored. As a
   secret it is weak, so it needs the throttle and a short session.
2. **Guest accounts with a password.** A stronger secret that the guest chooses, but a password store, a reset flow
   and a mail service for the reset; most guests book once and would not remember the password.
3. **A link sent by email.** It proves that the guest reads the mailbox, but needs a mail service and its secrets on
   the server, the link passes through mail systems, and the guest waits for the mail.

## Decision

A guest signs in with the booking number and the email of the booking (option 1) and gets a guest token for that one
booking, valid for 2 hours, which the API accepts only for that booking and its guest; the web application keeps the
token in a guest cookie apart from the staff sign-in.

1. **No account.** There is no guest account, no password and no mail. The sign-in takes the booking number and the
   email of the booking.
2. **The guest token.** `POST /api/auth/guest-login` takes the booking (the number without its prefix, as the receipt
   lookup does) and the email, and answers with the token, its end and the booking key. The token is a JWT signed with
   HS256 and the key of [ADR-0005], issuer `nfhotel-api` and audience `nfhotel-guest`, so a guest token is never taken
   for a staff token (audience `nfhotel-staff`) or the other way round. It holds the role `Guest` and the claim
   `booking` with the booking key, no subject and no name, and ends 2 hours after the sign-in without renewal. The
   web application sends it in the header `X-Guest-Token`, apart from `X-Staff-Token`. The answer to a public booking
   (`POST /api/bookings/public`) holds the booking and a guest session for it, so the guest who has just booked is
   signed in.
3. **One refusal.** An unknown or unreadable number, a wrong email, a cancelled booking (decision 7), and an expired,
   tampered, wrongly signed or unsigned guest token get 401 (Unauthorized) with one body, which holds only the code
   `api.guest_unauthorized`. The sign-in reads the booking also when the email is wrong, so an unknown number and a
   wrong email cost one read each; a token is checked without the database, valid or not. Failed sign-ins and failed
   receipt lookups share the throttle per booking number of [MIL-079]: the sixth failure for a number in 15 minutes
   gets 429. Each attempt is counted before the booking is read, in one step with the check, so attempts sent at the
   same time cannot pass the check together. Only a success clears the count, and the right email of a cancelled
   booking does not, so the count tells nothing that the answer hides.
4. **Permissions from the one table.** The role `Guest` of the permission table holds `Bookings.Create`,
   `Bookings.EditOwn` and `Guests.ViewOwn`, as it has since [MIL-016]. `Guests.Passport.ViewOwn` is held by no role,
   so the guest read leaves the passport number empty and no answer to a guest token holds one. The Access Control
   Matrix (`docs/refactor/Access-Control-Matrix.md`) shows the table with a column per role, and a test compares the
   two cell by cell.
5. **Ownership in the API.** Four operations accept a guest token: read a booking and read a guest (`Guests.ViewOwn`),
   cancel and reschedule (`Bookings.EditOwn`). The endpoint decides whether a guest token may call it at all; the use
   case compares the booking in the token with the booking asked for, and the guest with the guest of that booking.
   Another booking or guest gives 403 (Forbidden), and so does a move of the own booking to another room: a guest
   changes the dates, not the room. Every other endpoint with a permission refuses a guest token with 403. Without a
   staff or guest token the four give 401. Creating a booking and listing free rooms stay public.
6. **The guest cookie.** The web application signs a guest in with a second cookie scheme, apart from the staff
   cookie: the cookie `nfhotel.guest` is `HttpOnly`, `SameSite=Lax`, `Secure` behind TLS, sent only to the path
   `/my-booking`, ends with the token and is not renewed; it holds the token, encrypted by ASP.NET Core data protection
   as the staff cookie is. `/booking` signs a guest in; `/my-booking` shows the own booking, changes its dates, cancels
   it and signs out (`POST /my-booking/sign-out`), and answers each change with a redirect (Post/Redirect/Get). A page
   with a guest sign-in calls the API as the guest only, without the staff token, even when the same browser holds a
   staff sign-in. The back office (`/admin`) asks for the staff sign-in whatever the guest cookie, and signing out of
   one leaves the other. Right after a booking the page offers to manage the booking: a one-time ticket of 10 minutes,
   posted with an antiforgery token to `/my-booking/start`, sets the cookie from the session of the booking answer, so
   the token never appears in a page.
7. **Cancelled bookings.** A sign-in for a cancelled booking gets the one refusal. [MIL-018] asked for the refusal
   from 30 days after the cancellation; the time of a cancellation is not stored and the database stays unchanged, so
   every cancelled booking is refused, which includes the 30 days. A session opened before the cancellation lasts
   until its end, so the guest sees that the booking is cancelled.

**Current state (2026-11-19, [RC-417]).** The decision stands; [ADR-0049] ([MIL-085]) changed parts of it. A guest
signs in with the booking reference, a random version 4 UUID, in place of the booking number, which no page, address
or answer to a guest shows any more (decision 1). The answer to the sign-in and to a public booking holds the token
and its end but no booking key, and the guest cookie holds the token only (decisions 2 and 6). An unknown, unreadable
or old reference gets the one refusal (decision 3); the throttle of [MIL-079] gives way to two limits of the guest
sign-in alone, 5 failed attempts per reference and 20 per caller address in 15 minutes, and the older receipt lookup
is gone, so the sign-in is the one lookup. The four operations of decision 5 are now three guest routes,
`api/guest/booking` with `cancel` and `reschedule`, which take no key and answer with the receipt alone; every staff
route, the read of a guest included, refuses a guest token with 403.

## Consequences

**Positive:**

- A guest reads, moves and cancels the own booking without staff and without an account; no password store and no
  mail service are added.
- One key, one token format and one permission table: no new secret, and the matrix cannot drift from the table
  without a failing test.
- No answer tells whether a booking number exists, and the throttle stops the trying of emails for a known number.
- A guest token opens no staff endpoint, no other booking, no other guest and no passport number, and the API checks
  it on every call.

**Negative:**

- The email is a weak secret: whoever knows the number and the email of a booking can change it for 2 hours. The
  throttle limits guessing, not knowledge.
- The throttle is in memory, as for the staff sign-in ([ADR-0005]): a restart of the API clears it, and five wrong
  emails block the sign-in of a number for 15 minutes.
- A guest token cannot be revoked before its end. Signing out ends the cookie, but a copied token stays valid for its
  2 hours, and a booking that staff cancel stays readable by its guest until then.
- A guest cannot change the room; staff do that ([UC-010], extension 3b).
- The receipt lookup of [MIL-079] (`POST /api/bookings/public/receipt`) stays in the API with the same rules and the
  same throttle, although the web application no longer calls it.
- Decision 7 refuses more than [MIL-018] asked: a guest cannot sign in to a booking cancelled yesterday.
- Decision 7 refuses cancelled bookings only: the number and the email of a stay that ended long ago still sign the
  guest in, for as long as the booking is kept. The sign-in opens only that booking and the guest's own data; whether
  to refuse a booking some time after its departure date is left to S01.

**Quality:** security (confidentiality of the guest data, integrity of the bookings, resistance to guessing),
interaction capability (no account to create) and maintainability (one table, one key, one token format) (ISO/IEC
25010:2023).

## Affected Artifacts

- [MIL-018]: tasks 3 to 6 carry out this decision.
- [ADR-0005]: its open item, the own-resource check of the public flow, is closed; the role `Guest` of its permission
  table is used.
- [MIL-079]: the receipt page becomes the guest sign-in; its lookup decisions are kept.
- [UC-010]: the use case of the guest.
- [BC-001]: objective 21 and success criterion 24; its scope item still names ADR-0007 for this decision, a number
  that went to the decision on continuous integration and delivery.

---

[MIL-018]: ../milestones/mil-018-guest-login.md
[MIL-079]: ../milestones/mil-079-a-booking-opened-only-with-its-email.md
[MIL-016]: ../milestones/mil-016-backoffice-login.md
[MIL-017]: ../milestones/mil-017-tls.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0006]: ./adr-0006-tls.md
[BC-001]: ../business-case.md
[UC-010]: ../use-cases/uc-010/uc.md
[RC-358]: ../sqa/reviews/rc-358-mil-018-revision.md
[ADR-0049]: ./adr-0049-booking-reference.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[RC-417]: ../sqa/reviews/rc-417-mil-085-documents.md
[9e72b0e]: https://github.com/NF-Hotel/NFHotel/commit/9e72b0e99e8765b608373fe29391d33a2a8225ee
