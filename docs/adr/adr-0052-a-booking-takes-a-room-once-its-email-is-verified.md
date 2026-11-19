# ADR-0052 A booking takes a room once its email is verified

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0052 |
| CrossReference | [MIL-088], [MIL-085], [MIL-086], [MIL-087], [MIL-018], [MIL-033], [ADR-0004], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0053], [UC-001], [UC-010], [DM-004], [BC-001], [RC-431], [RC-433] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-088, written by the AI before any code<br>Reworked after the review by a second AI: No-Go in RC-431 (the claim before the booking, the limits per mailbox under one lock, the claims narrowed), then Go with conditions in RC-433 (the next free room after a lost race, the count of the bookings by mailbox, googlemail.com, the code of the mail limit), conditions closed; accepted by the AI as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19 ("Du skal tage beslutningerne for mig") | [baaf79b] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-088] lists eleven decisions and asks this record to fix the three steps, the token and its
link, the page with the button, the limits of the mails, the clean-up, what staff see and the setting, with the
alternatives and the reason each lost. S01 asked in chat on 2026-11-19 that a guest verifies each booking by mail, so
that nobody can book every room or overload the booking ("så hotel konkurrenter f.eks. ikke kan booke alt så der er
udsolgt"), and handed the decisions to the artificial intelligence (AI) that builds the milestone ("Du skal tage
beslutningerne for mig"). [ADR-0053] decides how the mail leaves the application.

**The problem**, as the code after [MIL-087] has it (read on 2026-11-19 at 6f7bb80):

- A booking on the public site is made at once. `POST api/bookings/public` stores a new guest, then a `Pending`
  booking with a room, and the room stays taken until staff cancel the booking; nothing lets a `Pending` booking
  lapse (`BookingService.CreatePublicAsync`). The limits of [ADR-0051] slow one sender down (5 booking requests an
  hour and 20 a day per address, 3 coming bookings per email), but a sender with many addresses and emails can still
  take every room: the automated threat OAT-021 of the Open Worldwide Application Security Project (OWASP), denial of
  inventory.
- Nothing shows that the email of a booking belongs to the person who typed it. The guest is signed in right after
  the booking ([ADR-0047]), and the booking page says under the email field "We send the confirmation here.", which no
  code does: the application cannot send mail.
- The new guest is saved before the booking, in a save of its own (`ResolveGuestIdAsync`), because `Booking.Create`
  takes the key of a stored guest. A booking that then fails, for example on the last room, leaves a guest with
  personal data and no booking behind. `IUnitOfWork` has no transaction of its own: one `SaveChanges` is one
  transaction.
- The limit per email of [ADR-0051] compares the emails in their normal form without letter case, so
  `anna+1@gmail.com` and `a.nna@gmail.com` count as two emails, although both reach the mailbox of
  `anna@gmail.com`.
- The API has one hosted service, `MarketAnalysisRunWorker`, which waits on a queue; nothing runs on a timer.

**What the texts say** (read on 2026-11-19):

- OWASP, automated threat OAT-021: the selection and holding of items from a limited inventory that are never
  bought, paid for or confirmed.
- OWASP cheat sheet "Email Validation and Verification": a random token sent to the address, used once and for a
  limited time; nothing that depends on the address is active before the verification.
- OWASP cheat sheet "Forgot Password": the link is built from configuration, not from the `Host` header of the
  request; the page of the link sends no referrer; the token is stored like a password, so a copy of the database
  gives no working link.
- OWASP Application Security Verification Standard (ASVS) 5.0: 2.3.1 (the steps of a flow in their order, none
  skipped), 2.3.3 (an operation succeeds whole or is rolled back), 2.3.4 (a lock, so that a limited thing such as a
  seat cannot be booked twice), 3.5.3 (sensitive functions use POST, not a safe method such as GET), 11.5.1 (at least
  128 bits from a cryptographically secure generator for what must not be guessed), 14.2.1 (sensitive data only in
  the body or the header fields, never in the address or the query string), 16.2.5 (sensitive data in a log only
  masked or hashed).
- Gmail Help, "Dots don't matter in Gmail addresses" and "Send emails from a different address or alias": the dots
  before the `@` of a `gmail.com` address do not matter, and mail to `name+word@gmail.com` goes to `name@gmail.com`.
  Many providers deliver the `+` form to the same mailbox as well, and Google's help names no rule for
  `googlemail.com`, the older name of Gmail in some countries. Where two forms reach two mailboxes, counting them as
  one mailbox only makes a limit stricter.

**Forces.**

- A guest books one stay in a few minutes and reads mail on the same device most of the time; the extra step must be
  short and must work in any mail program.
- A booking must hold a room only when somebody stands behind it, and what is not yet a booking must cost the hotel
  nothing: no room, no guest, no data kept longer than needed.
- Two requests can arrive at the same moment: two presses of one link, twenty guests for the last room, a burst of
  requests for one email. Each race must have one answer that does not depend on timing.
- Staff work as today: they confirm, check in and cancel bookings, and book for guests in the back office
  ([MIL-088] decision 11).
- Production has no mail relay until S01 opens one ([ADR-0053]); until then the public booking must work as it does
  today, behind the limits of [ADR-0051].
- One instance of the API runs ([ADR-0004]), and PostgreSQL holds the shared state.

**Options considered:**

- **A. A room held for a limited time**: the booking is made at once and lapses after some minutes unless verified.
  Easy to explain, but the held room is what OAT-021 abuses: a script that asks again before each hold lapses keeps
  every room, and a cap on holds only passes the harm to the next guest. Lost.
- **B. A puzzle for humans** (Cloudflare Turnstile on the booking page). Stops simple scripts, not people paid to
  solve puzzles, and proves nothing about the email; it needs S01's account at Cloudflare and a script of Cloudflare
  on the page. Kept by [ADR-0051] as a next step against floods, not as a proof of the email. Lost here.
- **C. A payment or a card guarantee.** Stops most abuse, but the hotel has no payment provider, and card data would
  bring the payment card rules into scope. Lost.
- **D. An account for every guest**, with a verified email at sign-up. Gives the same proof once, but asks every guest
  to make and keep an account for one stay; the verification of one booking is the same mail without the account.
  Lost.
- **E. A request that holds nothing, verified through a link sent to its email.** The API keeps the request with the
  answers of the guest; the booking, with its guest, is made when the guest presses the button behind the link.
  Chosen.

## Decision

A booking asked for on the public site takes a room only when the guest has verified it through a single-use link
sent to the email of the booking; until then the API keeps a booking request that holds no room and lapses after 30
minutes (option E). A setting switches the verification on; with it off, the public booking works as before. In
detail:

**1. Three steps: ask, verify, booked.** `POST api/bookings/public/requests` checks the answers of the guest as a
public booking is checked: the fields of the guest and the stay by the rules of [ADR-0050], the 30 nights and the
party of the public site, the limit of 3 coming public bookings per mailbox of [ADR-0051], and a free room of the
chosen size for the dates. It stores a booking request with the dates, the room size, the number of guests and the
fields of the guest, sends the mail of decision 4, and answers 202 with the time the link lapses. It takes no room and
stores no guest. The limits per address of [ADR-0051] count it as a booking request (5 an hour, 20 a day). The words
follow [MIL-088]: the guest asks and verifies; staff confirm, and `Confirmed` keeps its meaning.

**2. The token and the link.** The token is 32 bytes from `RandomNumberGenerator`, 256 bits, written as base64url
(the form of base64 that is safe in an address; 43 characters). The database keeps only its SHA-256 hash (Secure
Hash Algorithm, 256 bits), under a unique index, so a copy of the database holds no working link. The link is
`<site address>/booking/verify#<token>`: the site address is the setting `BookingVerification:SiteAddress`, never
the address of the request, and the token stands after `#`, the part of an address that a browser does not send, so
no access log of nginx, Cloudflare or the web application holds it. The link works once, for 30 minutes from the
request.

**3. The page with the button.** `/booking/verify` is a static server-rendered page with one button. A script of the
site's own (a file, no inline code, as the content security policy of [MIL-033] asks) copies the token from the
address into a hidden field of the form, takes it out of the address bar, and disables the button once it is pressed.
Only the button sends the token: a POST with the token in its body and the antiforgery token, which the web
application passes to `POST api/bookings/public/requests/verify` in the body (ASVS 3.5.3 and 14.2.1). The page answers
with `Referrer-Policy: no-referrer` and `Cache-Control: no-store`. Opening the link changes nothing, so a mail
scanner that opens it verifies nothing. An unknown token, a used one and a lapsed one get the same answer: the link
does not work any more, ask again. Without script the hidden field stays empty, and the page says that it needs
script.

**4. The mail of the request** is plain text with a fixed subject: the dates, the room size, the number of guests,
the link, the time it lapses, and one sentence for a receiver who did not ask: do nothing, and it lapses. It holds
no text that the sender typed: no name, no phone, no country, and the email only as the address it goes to; the
dates, the size and the number come from values that the API has checked. The email rule of [ADR-0050] admits no white
space and no control character, so no line break, and it runs before any mail is built; the adapter of [ADR-0053]
refuses a line break again (ASVS 1.3.11).

**5. Verify.** The API hashes the token and claims the request: one statement deletes the request of that hash whose
link has not lapsed and returns it. A press of a used, unknown or lapsed link claims nothing and gets the answer of
decision 3. Then the API checks the limit of 3 coming public bookings per mailbox again, chooses a free room of the
size, as the public booking chooses one, and in one transaction, one `SaveChanges`, stores the guest and the booking
`Pending` with its reference ([ADR-0049]) and its mark of the public site ([ADR-0051] decision 3). The booking takes
the new guest itself, not the key of a stored guest (`Booking.CreateForNewGuest`), so the database gives the guest its
key in the same save. Two locks of the database decide the races (ASVS 2.3.3 and 2.3.4):

- the delete of the claim decides a link pressed twice at once: one statement finds the row, the other finds none and
  gets the answer of a used link, before any room is chosen;
- the exclusion constraint `ex_booking_room_period` decides each room. The public booking chooses the same first room
  for every guest of a size, so when the save finds that room taken by a booking that committed in between, the API
  takes back the booking and its guest and tries the next free room of the size that sleeps the party, in the order of
  the choice, up to five rooms in all. Of 20 verifications at once for the last room of a size one commits, and the
  other 19 are told that the room is gone.

A claimed request is gone whatever follows, so a verification that loses every room it tried keeps nothing: no
request, no guest, no booking; the guest may ask again for other dates. A transaction that fails stores nothing.
After the commit the API issues the guest session as after a public booking ([ADR-0047]); the web application signs
the guest in and shows the receipt with the reference; and a second mail carries the reference and the dates. A
second mail that fails or that the fuse of [ADR-0053] holds back does not undo the booking: the receipt is on the
screen, and the guest can find the booking with the reference.

**6. One open request per mailbox, and the mails per mailbox.** The limits per email count by mailbox: the email in
the normal form of [ADR-0050], without letter case, without a `+` and what follows it before the `@`, and for
`gmail.com` and `googlemail.com` without the dots before the `@` and with the domain `gmail.com`
(`Mailbox.NormalFormOf`). The database keeps the SHA-256 of that form as the key of a request and of the record of a
mail, never the form itself. The limit of 3 coming public bookings of [ADR-0051] counts by the same mailbox from now
on, without a new column: the query reads the emails of the coming public bookings whose email ends in the same
domain (both Gmail domains for a Gmail address), and the API compares their mailbox forms with the one asked for. So
the bookings made since [MIL-087] count at once, and the query reads at most the coming public bookings of one
domain.

- A mailbox has at most one open request, under a unique index of its key: a new request replaces the old one, and
  the old link stops working.
- The verification mails to one mailbox are limited to 3 in an hour and 5 in a day, both windows rolling, counted in
  the database from a record of each mail: the key of the mailbox, the kind of the mail and the time, never the email.
  A refusal answers 429 with a code of its own, `booking.too_many_mails`, and without `Retry-After`. [ADR-0051]
  decision 2 gives every limit of the sender one code, because waiting is the answer to each; here the links are
  already in the mailbox, so the page sends the guest to the newest mail instead of asking the guest to wait, and the
  code tells the page which text to show. Like the limit per email of [ADR-0051] decision 3, the refusal comes from the
  use case, which hands the API no time to wait, and the pages do not read the header. The code tells a sender
  nothing new: only mails that somebody asked for count, and the sender sees the refusal of a request that the sender
  sent.
- A request counts, checks and writes in one transaction that first takes one lock of the database (a transaction
  lock of PostgreSQL that every request takes), so two requests never count at the same time: the limits per mailbox
  and the fuse of [ADR-0053] are checked before the old request is replaced, and the record of the mail is written
  before the mail is sent. When the relay does not take the mail, the request and its record are removed.
- "Send again" on the page asks again with the same answers: a new link under the same limits.

**7. The clean-up.** A timed job in the API runs every 10 minutes, each run in a scope of its own, and deletes the
requests whose link has lapsed and the records of mails older than 24 hours. A request with its personal data is so
gone at the latest 10 minutes after its link lapsed, inside the hour of [MIL-088] decision 7. The job reads the time
from the clock of the application, so a test runs it with a fixed clock. The passport number of a request is
encrypted with the passport key, as the passport number of a guest is.

**8. The setting.** `BookingVerification:Enabled`, off by default:

- off: `POST api/bookings/public` books at once with the checks of today, and the operations of the request and of
  the verification answer 404. One thing changes on this path too: it stores the guest and the booking in one save and
  moves on to the next free room after a lost race, as decision 5 does, through the same code;
- on: `POST api/bookings/public` answers 409 `booking.verification_required`, so no caller books around the
  verification, and the request and the verification work.

The web application asks the API which way holds (`GET api/bookings/public/mode`) and shows that flow. With the
setting on, the API refuses to start, and names the setting, when a setting of the relay of [ADR-0053] is missing or
`BookingVerification:SiteAddress` is not an absolute `https` address. [MIL-088] decision 10 asks for the verification
wherever a relay is set: that is how the stacks of the tests are set, and how production is set at the end of task 9
of [MIL-088]. The start checks only the other direction, because a verification without a relay would refuse every
guest, while a relay without the verification is the state between the two steps of task 9: S01 sets the relay first
and switches the setting on with the next deploy.

**9. Staff book as today**, without a request and without a mail. A booking request is not a booking: the back office
does not show it, and no report counts it.

**10. The security events.** Beside the events of [ADR-0051] decision 6, the API logs and counts a request that was
made (`booking_request_made`, with the address and the masked email) and a verification with a link that does not
work (`verification_failed`, with the address). A refusal of the mails per mailbox is a `limit_refused` event of the
limit `verification_mail`. A booking made by a verification is the event `public_booking_made` of [ADR-0051], so the
alert `PublicBookingSurge` counts the bookings in both ways. The use case writes these events through a port of the
application layer, because only it knows which limit refused and whether a mail went; the API writes them to its log
and its counter. The events of the mail are in [ADR-0053].

## Consequences

**Positive:**

- A request holds no room, so every booking on the public site has an email whose mailbox answered, and a mailbox
  counts once for the limits whatever `+` form or Gmail dots it is written with (OAT-021).
- The token is in no log of the server, no access log and no database in a form that works, and the button, not the
  opening of the link, verifies.
- A failed booking leaves nothing behind: the guest and the booking are stored in one transaction. The public booking
  with the setting off is stored the same way, so it no longer leaves a guest behind either, and a request that never
  becomes a booking is gone with its personal data within the hour.
- Each race has one answer that the database gives: one press of a link books, one guest gets the last room, and the
  limits per mailbox hold under a burst.
- The page no longer promises a mail that never comes.
- With the setting off, nothing changes for the guests until the relay of production exists.

**Negative:**

- A guest needs the mail within 30 minutes; a mail that comes late or lands in spam costs the booking, and the guest
  asks again under the limits of decision 6.
- A guest can lose the last room between the mail and the button: the request holds nothing by design. With more free
  rooms of the size, a lost race moves on to the next room, so only the last rooms, or a burst beyond five rooms, are
  lost. A guest whose verification fails for another reason, such as a lost connection to the database, has to ask
  again too, because the claim is gone.
- A domain that takes mail for every address before its `@` gives a sender endless mailboxes; the limits per address
  of [ADR-0051] and the alert `PublicBookingSurge` remain the answer to that gap.
- Someone who knows the email of a guest can replace the open request of that guest, whose link then stops working,
  and can use up its 5 mails of a day; the guest can ask again the next day or call the hotel.
- A copy of the link can still live outside the server: the history of the browser keeps the address with its `#`,
  and a mail filter that rewrites links (such as Microsoft's Safe Links) carries the whole link in its own address.
  That the link works once and for 30 minutes limits what such a copy is worth.
- The page of the link needs script; without it the link cannot verify, and the page says so.
- The booking flow now depends on a mail relay and on its limits; [ADR-0053] decides what happens when it fails.
- Every request waits for the one lock while it counts and writes, a few milliseconds; the mail is sent after the
  lock is released.
- The records of the mails keep a hash of each mailbox for 24 hours: pseudonymous personal data, kept only for the
  limits.
- The timed job is a new moving part of the API.

Quality attributes: security (the integrity of the room inventory, no token in a log of the server, ASVS 2.3.1 to
2.3.4, 3.5.3, 11.5.1, 14.2.1 and 16.2.5); reliability (one transaction for the guest and the booking, each race
decided by the database); interaction capability (one more step for the guest, the same work for staff);
maintainability (one switch, one place that chooses the flow, one set of checks for both ways of the public booking).

## Affected Artifacts

- [MIL-088] — tasks 3 to 7 build this record.
- [ADR-0047] — the guest session after a public booking comes after the verification when the setting is on.
- [ADR-0049] — the booking gets its reference when the guest verifies, not when the guest asks.
- [ADR-0050] — its rules check the answers when the guest asks, and again when the booking is made.
- [ADR-0051] — its limits per address count booking requests; its limit per email counts by mailbox; one new name of
  a limit, two new events, and a second code of 429, `booking.too_many_mails`, with its own text and no
  `Retry-After`.
- [ADR-0053] — sends the two mails, and decides the relay, the fuse and the mail catcher.
- [UC-001] — staff book as before; a booking request is not a booking of the back office.
- [UC-010] — the guest finds the booking with the reference of the receipt and of the second mail.
- [DM-004] — the booking request and the record of a mail sent, each with the key of its mailbox; a booking takes its
  new guest in the same save; the bookings of a mailbox are counted from the emails of their guests, with no new
  column.

Task 3 of [MIL-088] writes the use case UC-011 with its system sequence diagram, operation contracts, sequence
diagram, domain model and class diagram, and task 8 updates the rule ledger `docs/refactor/BusinessRules.md`; they
cite this record when they exist.

---

[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-086]: ../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-018]: ../milestones/mil-018-guest-login.md
[MIL-033]: ../milestones/mil-033-security-headers.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0047]: ./adr-0047-guest-login.md
[ADR-0049]: ./adr-0049-booking-reference.md
[ADR-0050]: ./adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0051]: ./adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0053]: ./adr-0053-mail-from-the-application-through-a-relay.md
[UC-001]: ../use-cases/uc-001/uc.md
[UC-010]: ../use-cases/uc-010/uc.md
[DM-004]: ../domain-model.md
[BC-001]: ../business-case.md
[RC-431]: ../sqa/reviews/rc-431-adr-0052.md
[RC-433]: ../sqa/reviews/rc-433-adr-0052-second-round.md
[baaf79b]: https://github.com/NF-Hotel/NFHotel/commit/baaf79babbf7d3ff7c52b39b60f1c31def2fdccb
