# ADR-0049 The booking reference

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0049 |
| CrossReference | [MIL-085], [ADR-0047], [ADR-0005], [ADR-0004], [ADR-0015], [MIL-079], [MIL-018], [MIL-087], [MIL-077], [UC-010], [BC-001], [RC-416], [RC-418], [RC-419] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-085, written by the AI before any code<br>Go in RC-416; accepted by the AI as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19 ("Du skal tage beslutningerne for mig") | [f8cd896] |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Decisions 2 and 6 and the consequences after the code review RC-418: a full store keeps every window at its limit and refuses a new key when all are at it; an IPv6 caller counts by its first 64 bits; the time of the answer and the refusal of hostile input as they are<br>Go in RC-419; accepted by the AI as stand-in for S01 in autonomous mode | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-085] lists eight decisions and asks this record to fix what they leave open: the alternatives
and why each lost, the limit on wrong tries per caller address with its number and its bounded store, and what
happens to the older lookup endpoint (task 1). S01 handed the decisions to the AI in chat on 2026-11-19 ("Du skal
tage beslutningerne for mig") and asked for a GUID ("Det skal gerne være GUID"). The AI accepts this record as
stand-in for S01 after its review ([RC-416]); it stays open to S01's challenge.

**The problem.** The booking number is `FLZ-` plus the key of the table, which the database counts up by one (BR-109).
Since [MIL-079] a booking opens only with its number and its email, and five wrong tries for one number are followed
by a wait of 15 minutes; since [MIL-018] the same pair signs the guest in. What is left, read in the code at 3b973c7:

- From one number a stranger knows the next and the previous one, how many bookings the hotel has, and how many came
  between two bookings of their own. The number is in the address of the sign-in page, on the receipt and in the link
  under the receipt, so it ends in browser histories and proxy logs.
- The count of wrong tries is per booking number only, in the memory store that the staff sign-in uses as well. One
  caller can try an email against every number in turn, five tries each, and a caller who names many numbers grows
  the store: it drops run-out windows every 256 failures, but it holds every window of the last 15 minutes.
- The answer to a public booking carries the whole booking with its guest and room to the web application, which
  uses the receipt only; the guest pages read the booking by its key, and the guest token opens the staff routes
  `api/bookings/{id}` for the own booking.
- Two endpoints do the lookup: the guest sign-in `POST api/auth/guest-login` and the older
  `POST api/bookings/public/receipt`, which only the client of the API still names. No page calls it.

**What the texts say** (read on 2026-11-19):

- OWASP Application Security Verification Standard (ASVS) 5.0, requirement 11.5.1 (level 2): "all random numbers and
  strings which are intended to be non-guessable must be generated using a cryptographically secure pseudo-random
  number generator (CSPRNG) and have at least 128 bits of entropy. Note that UUIDs do not respect this condition."
  Requirement 8.2.2 (level 1): access to a data item only for consumers with permission to that item. Requirement
  15.3.1 (level 1): an answer holds only the fields that are needed.
- RFC 9562, section 8: implementations "SHOULD NOT assume that UUIDs are hard to guess" and "MUST NOT" use them as
  capabilities, "identifiers whose mere possession grants access"; if a UUID is used near a security operation,
  version 4 (section 5.4: 122 random bits beside the 4 version and 2 variant bits) "SHOULD be utilized". Section 6.9
  asks for a CSPRNG.
- Microsoft's documentation of `Guid.NewGuid`: it creates a version 4 UUID; on platforms other than Windows it "calls
  the OS's underlying cryptographically secure pseudo-random number generator (CSPRNG) to generate 122 bits of strong
  entropy"; and it is not meant for cryptographic purposes. The images of the API run Linux.
- PostgreSQL 16, the version of the images: `gen_random_uuid()` "returns a version 4 (random) UUID"; its source fills
  the 16 bytes from `pg_strong_random` and then sets the version and variant bits.
- The OWASP cheat sheet on preventing insecure direct object references (IDOR): random identifiers are defence in
  depth and the access check stays essential; "adding a column with random strings in the database table" is one way,
  "UUIDs or other long random values as primary keys" another; "Avoid encrypting identifiers as it can be challenging
  to do so securely".

**Forces.**

- The reference is a name, not a secret. The secret of the sign-in stays the pair of reference and email, behind a
  limit on wrong tries; an email is a weak secret, which is why the limit has to count per caller as well.
- The deploy rolls back to the release before when its smoke test fails, and the migration is not rolled back. The
  release before must keep working against the migrated database, and it inserts bookings without knowing the new
  column.
- The counting key is the key of the table, of its foreign keys, of the exclusion constraint's rows and of the feed
  to the market analysis service. Changing the key is a large migration that cannot be rolled back.
- One instance of the API runs; a store shared between processes would be one more service to run and secure
  ([MIL-087] decision 5 takes the same view).
- The address of a caller that the API sees today is the first entry of `X-Forwarded-For` that the web application
  passes on, which a caller can write. [MIL-087] makes it the address the proxy saw; this record uses the value the
  API sees until then, as decision 6 of [MIL-085] says.

**Options considered** for the reference:

1. **A version 4 UUID in a column of its own** beside the key: 122 random bits from the generator of the operating
   system, unique by a unique index, filled for the bookings that exist by the database in the same migration.
2. **A shorter random code**, for example ten characters of an alphabet without look-alike letters (about 50 bits).
   Easier to read out on the phone. It lost: S01 asked for a GUID; a short code needs its own generator, alphabet,
   collision retry and a check for words that must not appear; and fewer bits make a guess at scale cheaper. A guest
   copies the reference from the receipt, and staff find a booking by guest and dates.
3. **A UUID as the key of the table.** The IDOR cheat sheet names it. It lost: the key is referenced by the foreign
   keys and indexes, by the exclusion constraint and by the market analysis feed; the change rewrites the table and
   every reference, the release before cannot run against it, and random keys scatter the inserts of the index. The
   counting key is harmless where nobody outside sees it.
4. **A UUID of version 7.** Ordered and good for an index, but it carries the time of creation in milliseconds and the
   order of the bookings, which is what has to go; RFC 9562 section 8 points to version 4 near security.
5. **An encrypted number**, the key encrypted into a token. It lost: the IDOR cheat sheet advises against it; it adds a
   key to keep, rotate and protect, a leaked key makes every number countable again, and a rotation changes every
   reference that guests hold.

**Options considered** for the count of wrong tries: the store of the staff sign-in, extended with a key per address
(as today); stores of their own for the guest lookup; or a store shared between processes. The first lost because a
flood of guest lookups could then push the windows of staff accounts out of a bounded store; the third because one
instance runs (above).

## Decision

A booking is known by a reference that is a version 4 UUID from `Guid.NewGuid()`, kept in a column of its own beside
the counting key, which stays inside the system; the reference never opens a booking alone, and wrong tries of the one
guest sign-in are counted per reference and per caller address in two bounded stores of their own.

In detail, numbered as the decisions of [MIL-085]:

1. **The reference.** `Booking.Create` gives every new booking a reference from `Guid.NewGuid()`. Its text form is the
   usual 36 characters, lower case, with hyphens (`3f2504e0-4f89-41d3-9a0c-0305e82c3301`). A text is read as a
   reference only in exactly that form, in any letter case and with surrounding spaces trimmed, and only when its
   version is 4 and its variant is the one of RFC 9562; anything else is unreadable.
2. **The reference names, the email opens.** The guest sign-in asks for the reference and the email of the booking.
   An unknown reference, an unreadable one, a number of the old kind and a wrong email get the same status and the
   same body: 401 with `api.guest_unauthorized`, the one refusal of [ADR-0047] decision 3. The email is compared with
   `CryptographicOperations.FixedTimeEquals`, against a stand-in when there is no booking, so the time does not depend
   on where two emails of one length differ. The time of the answer is not the same in every case: a reference that
   exists costs the read of one booking more than one that does not, and two emails of different lengths are told
   apart at once. Both tell a caller only about a reference that the caller already holds, and decision 6 allows five
   such tries in 15 minutes. A reference that holds a control or direction character, or that is no text, never
   reaches the sign-in: the input rules of every request ([MIL-077]) refuse it with 400 and `api.invalid_request`,
   the answer to any hostile input, which names no booking; nothing is read and no try is counted. The reference has
   122 random bits, fewer than the 128 that ASVS 11.5.1 asks of a value that must not be guessed, and it is not used
   as one: it names a booking (RFC 9562 section 8), and what opens a booking is the pair of reference and email behind
   the limits of decision 6.
3. **The counting key stays inside.** It is the key of the table, of the staff routes between the web application
   and the API (`api/bookings/{id}` and its actions), of the guest token's claim `booking`, and of the feed to the
   market analysis service. No public or guest operation takes it or returns it:
   - the answer to a public booking is the receipt with the reference and the guest session; the session no longer
     carries the key, and the guest cookie of the web application holds the guest token only;
   - a signed-in guest reads, moves and cancels the booking through `api/guest/booking`, which takes no key: the
     booking is the one in the guest token;
   - the staff routes no longer admit a guest token (the guest permissions of [MIL-018] move to the guest routes), and
     neither does the read of a guest by its key, which no page of a guest uses;
   - the format `FLZ-` (BR-109) is retired everywhere, the back office included: staff see the reference and search
     for it, and the back office keeps the key in the calls to the API only.
4. **The column.** The migration adds `reference uuid NOT NULL DEFAULT gen_random_uuid()` and the unique index
   `ux_booking_reference`, and nothing else. PostgreSQL fills every booking that exists with its own version 4 value
   while it adds the column, and fills every booking that the release before inserts. The model gives the property the
   same default, so a booking that the domain has not given a reference still gets one.
5. **Old numbers.** From the deploy on, `FLZ-000123`, `000123` and `123` are unreadable references and get the refusal
   of decision 2. A guest who holds an old number contacts the hotel; staff find the booking by guest and dates and
   give the reference.
6. **Wrong tries.** Every try of the guest sign-in is counted before the booking is read, as one atomic step with the
   check, and a try that succeeds is taken back. Two stores, both in memory, apart from the store of the staff sign-in:
   - per reference: at most **5** failed tries in a window of **15 minutes**, as since [MIL-079]; the key is the
     reference as read, for a reference that exists and one that does not alike, so the count does not tell whether a
     booking exists; a success clears the window, except for a cancelled booking, which gets the answer of a wrong
     email and keeps its count;
   - per caller address: at most **20** failed tries in **15 minutes**, four times the limit per reference, the
     factor that the limit per address of the staff sign-in uses since [ADR-0015]: a family or an office behind one
     address has room for typing mistakes, and a caller who tries emails against many references is stopped after 20.
     Unreadable references count here too. A success gives the try back and does not clear the window, so one's own
     booking cannot be used to reset the count. An IPv6 caller counts by the first 64 bits of its address, the
     prefix of its link, because the last 64 bits are its interface identifier (RFC 4291 section 2.5.1), which a host
     forms itself and may change at will (RFC 8981); an IPv4 address in IPv6 form counts as the IPv4 address;
   - the try over a limit gets 429 with `booking.too_many_lookups` and a text that names no booking;
   - each store holds at most **10,000** windows. When it is full, the windows that have run out are dropped first,
     then the window with the fewest failed tries, of those the one that reached its count first. A window at its
     limit is never dropped: when every window of a full store is at its limit, the try of a new key is refused with
     the answer of the limit and counted in neither store. So a flood of new keys cannot free a reference or an
     address whose tries are used up; it can push out only windows below their limit, and a window with more failed
     tries only after every window with fewer is gone. A restart clears both stores;
   - the numbers are settings (`GuestLookup:MaxFailuresPerReference`, `GuestLookup:MaxFailuresPerAddress`,
     `GuestLookup:WindowMinutes`, `GuestLookup:MaxTrackedKeys`) whose defaults are the numbers above; [MIL-087] puts them
     in its table of limits;
   - the address is the one the API sees today (the value the audit trail records), behind one port of the
     application layer, so that [MIL-087] changes one implementation when it makes the address the one the proxy saw.
7. **The answers.** The public booking and every guest operation answer with the receipt and nothing more: the
   reference, the arrival and departure dates, the nights, the room size and the status. The payment text is the
   page's own. The guest page derives what it may offer (move, cancel) from the status, with the rule of the domain,
   and no longer shows the name and email of the booking: the guest has just typed the email, and no step needs the
   name (ASVS 15.3.1). The staff answers keep the guest and the room, with the reference in place of the number.
8. **One lookup.** The guest sign-in is the one lookup. The older endpoint `POST api/bookings/public/receipt` is
   removed, with its method on the client of the API; its use case becomes the private first step of the sign-in.
   The list of public endpoints of the API loses it.

## Consequences

**Positive:**

- No page, address, mail or public answer shows a number from which another booking follows; the order and the count
  of the bookings are no longer visible (finding 1 of [MIL-085]).
- One caller can no longer try an email against every reference: 20 failed tries per address in 15 minutes, beside the
  5 per reference (finding 3); the stores cannot be grown without a bound, and a flood cannot free a reference whose
  tries are used up.
- The guest routes take no key at all, so a guest cannot even name another booking; less personal data crosses the
  wire to the web application (finding 4), and one lookup is left to keep right (finding 5).
- The migration only adds, so the release before keeps working and a rollback stays possible.

**Negative:**

- A reference of 36 characters is long to read out; a guest on the phone is found by name and dates.
- Old numbers stop working on the day of the deploy, also for guests who hold one; they have to contact the hotel.
- Until [MIL-087], the address that the limit per address counts is the first entry of `X-Forwarded-For`, which a
  caller can write: a caller can dodge the limit per address, and can use up the 20 tries of another address for 15
  minutes. A caller that holds more than one prefix of 64 bits, as a site with a /56 or a /48 does, also has as many
  addresses for the limit. The limit per reference holds regardless, because a full store never drops a window at its
  limit.
- A flood can fill a store with windows at their limit: at least 50,000 failed tries in 15 minutes for the store of
  references (10,000 windows of five), and 200,000 for the store of addresses. Until those windows run out, a try for
  a key that has no window is refused, so the sign-in fails closed rather than open. A smaller flood can push out
  windows below their limit, which gives such a reference its tries back after at least 10,000 failed tries, and a
  restart clears the counts. All three are accepted for one instance in memory; [MIL-087] alerts on refusals by the
  limits.
- The guest page shows less than before (no name and email). After a rollback to the release before, a guest whose
  cookie this release wrote signs in again, because that cookie holds no booking key.

## Affected Artifacts

- [MIL-085] — the decision record that its task 1 asks for
- [ADR-0047] — decisions 2 to 5 keep their meaning with the reference in place of the number; the guest operations
  move to routes without a key, and the guest cookie holds the token only
- [ADR-0005] — the throttle of the staff sign-in no longer holds the windows of the guest lookup
- [ADR-0004] — the staff routes between the web application and the API keep the key
- [ADR-0015] — the factor of four between the limits per key and per address is taken from it
- [MIL-087] — puts the four settings in its table of limits and makes the address the one the proxy saw
- [UC-010] — the guest finds the booking with the reference and the email
- [BC-001] — success criterion 46

---

[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0047]: ./adr-0047-guest-login.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0015]: ./adr-0015-mobile-app-staff-token-access.md
[MIL-079]: ../milestones/mil-079-a-booking-opened-only-with-its-email.md
[MIL-018]: ../milestones/mil-018-guest-login.md
[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[UC-010]: ../use-cases/uc-010/uc.md
[BC-001]: ../business-case.md
[RC-416]: ../sqa/reviews/rc-416-adr-0049.md
[MIL-077]: ../milestones/mil-077-hostile-input-against-the-api.md
[RC-418]: ../sqa/reviews/rc-418-mil-085-code.md
[RC-419]: ../sqa/reviews/rc-419-adr-0049-after-the-code-review.md
[f8cd896]: https://github.com/NF-Hotel/NFHotel/commit/f8cd8968c55bf9989d469393ec470c8df00d27bf
