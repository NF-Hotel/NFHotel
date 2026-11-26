# ADR-0051 The limits of the booking flow and the address of a caller

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0051 |
| CrossReference | [MIL-087], [MIL-085], [MIL-086], [ADR-0004], [ADR-0015], [ADR-0017], [ADR-0023], [ADR-0028], [ADR-0047], [ADR-0049], [ADR-0050], [MIL-036], [MIL-088], [UC-001], [UC-010], [BC-001], [MIL-089], [ADR-0052], [SAR-002] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | Second version, after the code review RC-428 of MIL-087: decision 2 says that a refusal by the limit per email keeps the counts per address; decision 4 says what the forwarded headers of the web application do, and that the sign-in limit of the mobile app counts a whole address; decision 5 says what a full store drops for each kind of key, and caps a limit at 100,000; two negative consequences<br>Go in RC-429; accepted by the AI as stand-in for S01 in autonomous mode | [a574bf6] |
| 2026-11-21 | Accepted | mikemike | AI as stand-in for S01 | Third version, in MIL-089: decision 6, the events hold the address and no email, whole, masked or hashed, and the limit per email is named `mailbox`, with its reason and a consequence for the trail of an incident: a stand-in decision that the planning session made as S01 on 2026-11-20, which S01 can overrule, so that the five alerts of CodeQL on the events close through the code<br>Decision 8, at most 50 open circuits per caller in the web application (SAR-002 BF-02): a stand-in decision of the build session, which S01 can overrule; ADR-0052 among the affected artifacts (RC-439)<br>After the delta review of RC-439: the problem, the force, ASVS 15.1.3 and 15.2.2 and the options J and K of decision 8 in the Context; the 51st tab as the test shows it; SAR-002 as a link<br>Go in the second delta review of RC-439; accepted by the AI as stand-in for S01 in autonomous mode | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** [MIL-087] lists seven decisions and asks this record to fix where each count lives and what a
restart does to it, how the address of a caller travels from nginx to the API and why the caller cannot write it, any
number that the reading of the code changes, and the alternatives with the reason each lost. S01 asked in chat on
2026-11-19 that nobody can book every room or overload the booking, and that the API is protected against attacks,
and handed the decisions to the artificial intelligence (AI) that builds the milestone ("Du skal tage beslutningerne
for mig"). [MIL-089] adds decision 8, for a finding of its security audit ([SAR-002]), under the same words of S01.

**The problem**, as the code after [MIL-086] has it (read on 2026-11-19):

- No public operation of the flow has a limit: not the public booking, not the search for free rooms. nginx has none,
  and the one limiter of the API (`StaffWriteRateLimit`) covers the writes of staff.
- Each public booking stores a new guest, and nothing counts the bookings of one email.
- The web application takes the address of a caller from the first entry of `X-Forwarded-For`. nginx only appends the
  address it saw to that header (`$proxy_add_x_forwarded_for`), so the first entry is whatever the caller wrote. The
  web application sends the value to the API in `X-Forwarded-For`; the API trusts forwarded headers from every peer
  (`KnownIPNetworks` and `KnownProxies` cleared), and `HttpCallerAddress` and the audit trail ([ADR-0017]) read the
  first entry. The limit of the guest sign-in per address ([ADR-0049] decision 6) counts that value, so a caller who
  writes a new address on each try is never refused, and a caller who writes another person's address uses up that
  person's tries.
- On the pages that run interactively over the web socket of Blazor (the booking page and the back office), the web
  application reads the address through `IHttpContextAccessor`, which a circuit does not reliably have. Without an
  address the API takes the address of its peer, the container of the web application, so every guest of the booking
  page would share one count.
- The flow writes no event and counts nothing: no line for a public booking, a failed lookup or a refusal, no counter,
  no alert. The one alert on sign-ins is the one for staff (`StaffSignInFailures`, [MIL-036]).
- Since MIL-089 (abuse case 11 and finding BF-02 of its security audit, [SAR-002]): the interactive pages, the
  booking page and the back office, each run in a circuit of Blazor, and nothing counts the circuits of one caller.
  A circuit holds one request to `/_blazor` open, its web socket, as long as it lives, and holds memory in the one
  container of the web application, so one caller can open circuits until the site stops, the back office
  included.

**What the texts say** (ASVS is the Application Security Verification Standard 5.0 of the Open Worldwide Application
Security Project, OWASP; read for [MIL-087] on 2026-11-19, the pages of Microsoft read again on 2026-11-19):

- ASVS 2.1.3 and 2.3.2: the limits of the business are written down, per user and for the whole application, and
  built as written; 6.1.1: the limits are documented; 2.4.1: there are controls against automation.
- ASVS 4.1.3 and 15.3.4: a header that a proxy sets cannot be overridden by the caller, and the address that limits
  use is the one the proxies carry in a field that the application trusts.
- ASVS V16.3: security events are logged, attempts to get around a limit among them.
- ASVS 15.1.3 and 15.2.2 (read for [MIL-089] on 2026-11-20): the documentation names the functions that take long or
  hold many resources, and how a loss of availability through them is prevented, limits of the parallel work per user
  and for the whole application among the means; the defences are built as documented.
- OWASP API Security Top 10 2023: API4 (a limit on how often a client may call) and API6 (a flow such as making a
  reservation needs a limit of its own); OWASP automated threat OAT-021, denial of inventory.
- RFC 6585 section 4: status 429 for too many requests, which may say how long to wait in `Retry-After`
  (RFC 9110 section 10.2.3).
- nginx, `proxy_set_header`: a header field that the configuration sets replaces the field of the same name from the
  client; the real IP module sets `$remote_addr` from `CF-Connecting-IP` only for the peers of `set_real_ip_from`.
- Microsoft, "Configure ASP.NET Core to work with proxy servers and load balancers": the forwarded headers middleware
  takes the headers only from the known proxies and networks, and by default only their last entry (`ForwardLimit` 1).
- Microsoft, "IHttpContextAccessor/HttpContext in ASP.NET Core Blazor apps": `IHttpContextAccessor` generally should
  be avoided with interactive rendering, because a valid `HttpContext` is not always available. "ASP.NET Core Blazor
  additional server-side security scenarios" shows the documented path into a circuit: a circuit handler reads the
  user from `AuthenticationStateProvider`, which Blazor fills from the user of the connection of the circuit. The web
  application already takes the staff token into a circuit this way (`AuthenticationStateStaffTokenSource`).

**Forces.**

- A guest who books one stay sends one or two booking requests and a handful of searches; a family or an office
  behind one address books a few rooms. The limits must leave them alone and stop a script.
- The interactive pages send their bookings and searches as messages over one long web socket (`/_blazor`). nginx and
  Cloudflare see the connection, not the messages, so only the web application and the API can count a booking.
- Every open tab of the booking page or the back office holds a circuit, so the guests and the staff behind one
  address, the network of the hotel among them, hold several at once; a limit of the circuits must leave them room
  (since MIL-089).
- One instance of the API runs ([ADR-0004]). A store outside the API is one more service to run, secure and watch.
- A rule of input must not use up a count: a guest who mistypes a phone number has not sent a booking request
  ([MIL-086]).
- S01 holds the accounts of Cloudflare; the AI changes nothing there in this milestone.

**Options considered:**

- **A. A limit in nginx** (`limit_req` per address). Bounded and cheap, but it sees the web socket of the pages, not
  the bookings and searches inside it, and cannot tell a refused value from a booking. Lost.
- **B. A rule at Cloudflare** (rate limiting on the public host name). Stops a flood before the tunnel, but sees the
  same connection as nginx and needs an action of S01 in the account ([MIL-087] decision 7). Kept as the next step.
- **C. A puzzle for humans** (Cloudflare Turnstile on the booking page). Stops simple scripts, but needs S01's
  account, a script of Cloudflare on the page (a change of the Content Security Policy) and a check of its token at
  the API, and asks every guest for a step. Kept as the next step.
- **D. A shared store** (a Redis service for the counts). Survives a restart of the API, but adds a service with its
  own secret, network and updates for one instance of the API. Lost.
- **E. The rate limiting middleware of ASP.NET Core** for the counts per address, as `StaffWriteRateLimit` uses it.
  Known and tested, but a partitioned limiter keeps one limiter for each key until the key has been idle, with no
  setting for the number of keys, so 100,000 addresses in one window grow it without a limit ([MIL-087] criterion 5);
  and it counts before the action, so it cannot give back a request that a rule refused. Lost to the bounded store of
  [ADR-0049], which already does both.
- **F. The address in `X-Real-IP`**, which nginx already overwrites. Works, but other software reads and writes the
  name; a header of the application's own says what it is and is set in one place. Lost.
- **G. The address into a circuit through `IHttpContextAccessor`**, as today. Microsoft advises against it, and the
  circuit can be without a request. Lost.
- **H. The address as a parameter of each interactive page, or as persisted component state of the prerender.** Both
  are documented, but each page picks its render mode and the router renders it, so every interactive page would
  need a static wrapper or its own persisted state. Lost to option I, which uses the path that the staff token takes.
- **I. Counts in the API, by the address that nginx saw.** nginx writes the address into a header of its own, the
  web application carries it in the authentication state of the request or the circuit and passes it to the API
  under the service token, and the API counts per address in the bounded stores of [ADR-0049] and per email in the
  database, logs and counts each event, and alerts. Chosen.
- **J. A limit of the connections in nginx** (`limit_conn` per address, since MIL-089). Cheap, and in front of the
  web application, but it counts a whole IPv6 address, not its network of 64 bits, counts the connections of every
  path, not the circuits, and writes a refusal only to the error log of nginx, with no event and no counter. Lost.
- **K. A count of the open circuits per caller in the web application** (since MIL-089). The web application counts
  the open requests to `/_blazor` under the key of the limits of the API, refuses one over the limit before its
  circuit starts, and writes the event and the counter of the other limits. Chosen for the circuits, beside option I.

## Decision

The API counts what one caller address and one email ask of the booking flow and refuses what goes over the limits
of one table with status 429, under the address that nginx saw, which nginx sets, the web application carries and
only a call with the service token can name (option I); since MIL-089 the web application also counts the open
circuits of each caller (option K, decision 8). In detail:

**1. One table holds the limits.** The table "Limits of the booking flow" in `docs/refactor/BusinessRules.md` names,
for every limit, who is counted, the window, the number, the status and the text of the refusal, and the setting that
holds the number. The numbers of the new limits are the settings `SenderLimits` (`SenderLimitOptions`), whose
defaults are the table; the limits of [ADR-0049] keep their settings `GuestLookup`. A test reads the table and fails
when a default differs from it.

**2. The limits per caller address**, counted under the key of [ADR-0049] decision 6 (an IPv4 address as itself, an
IPv6 address by its first 64 bits):

| Who is counted | What | Window | Number |
| --- | --- | --- | --- |
| A caller address | Booking requests on the public site | 1 hour | 5 |
| A caller address | Booking requests on the public site | 1 day | 20 |
| A caller address | Searches for free rooms, by the public site and by staff | 1 minute | 60 |
| A reference; a caller address | Failed guest sign-ins and lookups ([ADR-0049]) | 15 minutes | 5; 20 (unchanged) |

- A window opens with the first request that it counts and runs its length, as the windows of [ADR-0049] do.
- A booking request counts when it has passed the rules of the fields: the attribute rules of the request type
  refuse a bad value before the count, and a request that a rule of the use case refuses with 400 or 422 gets its
  count back. A request that the limit per email of decision 3 refuses keeps its count: it is refused by a limit, not
  by a rule, so a sender who tries one email after another is still counted per address. The check and the count are
  one atomic step, so requests sent at the same time cannot all pass the check before any of them is counted; a
  request that a limit per address refuses counts in no window.
- Staff are counted only by the search: they book through their own endpoint, and they search from the back office,
  behind other addresses than the guests, so 60 a minute does not stop them.
- A refusal is status 429 with the one code `booking.too_many_requests`, the one text of the pages, and `Retry-After`
  with the whole seconds until the window that refuses has room again. The text names no number of minutes, because
  the windows differ and the pages do not read the header. The refusals of [ADR-0049] keep `booking.too_many_lookups`
  and its text, which names its fifteen minutes.
- No number of [MIL-087] changes.

**3. The limit per email.** A public booking is refused when its email already has 3 coming bookings made on the
public site, where coming means not cancelled and with its departure today or later. The refusal has the same status,
code and text as the limits per address, without `Retry-After`, because it has no window. Staff can always book.

- The count is read from the database. A booking gets the column `made_on_public_site`, true for a booking made by
  `CreatePublicAsync` and false for every other; every booking stored before the deploy is false, so it does not
  count.
- The email is compared in its normal form ([MIL-086] decision 4) and without letter case, as the guest sign-in
  compares it.
- The count is checked after the rules of the use case and before the room is chosen; it is not one transaction with
  the insert, so two requests for one email at the same moment can both pass. The limits per address bound that.

**4. The address of a caller is the one nginx saw.**

- nginx sets `X-NFHotel-Client-Address` to `$remote_addr` in `snippets/proxy-headers.conf`, which every proxied
  location of both listeners includes, the routes of the mobile app among them. A value that the caller wrote is
  replaced. On the public listener `$remote_addr` is the address of `CF-Connecting-IP`, trusted only from the Docker
  networks ([ADR-0028]). The optional route to the API sets it too.
- The web application reads the header only from a peer in the networks of the stack (loopback and the private ranges
  that Docker uses: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16), and only a valid address; otherwise it takes the
  address of the connection, as in development without nginx. In the deployment the web application takes forwarded
  headers (`ASPNETCORE_FORWARDEDHEADERS_ENABLED`), which trust any peer and take the last entry of `X-Forwarded-For`,
  the one nginx added. So the address of its connection, and the peer that the check sees, is already the address
  that nginx saw, not the address of nginx; both ways give that address, because nginx writes both headers and is the
  only peer: the web application publishes no port. A middleware after the authentication adds the address
  to the user of every request as the one claim of an identity without authentication, so the user stays who they
  were. The address reaches a circuit as the staff token does: Blazor fills the authentication state of the circuit
  from the user of its connection. The web application reads the address from the authentication state, on a static
  page and in a circuit alike, and from the request only where no authentication state is set up.
- The web application sends the address to the API in `X-NFHotel-Client-Address`
  (`ApiAuthentication.ClientAddressHeader`), no longer in `X-Forwarded-For`.
- The API reads that header only on a call that has shown the service token: `ApiTokenMiddleware` marks the call when
  the token matches. Any other call, the mobile app's included, has the address of its connection, after forwarded
  headers that the API takes only from loopback and the three private ranges, and only their last entry, the one
  nginx added. `HttpCallerAddress` is the one reader: the audit trail, the limits of [ADR-0049] and the limits of this
  record use its value.
- The sign-in limit per address of the mobile app ([ADR-0015]) reads the address of the connection as before and
  counts it whole, an IPv6 address too, not by the key of 64 bits of [ADR-0049] decision 6. It is outside the booking
  flow and unchanged here; its limit per user name holds whatever the address.
- The key `unknown` of [ADR-0049] stays only for a host without the address of a connection, the in-memory server of
  the tests; through nginx every request has an address.

**5. Where the counts live.** The counts per address are in the memory of the API, in stores with a bound: one for
the minute of the searches, one each for the hour and the day of the booking requests, with at most 10,000 keys each
(`SenderLimits:MaxTrackedKeys`). The bounded store of [ADR-0049] becomes one shared class, so the guest sign-in and
these limits use one implementation. When a store is full it drops the window that gives back least, never a window
at its limit while another is below it. When every window is at its limit, the kind of key decides:

- The store of booking references of [ADR-0049] refuses the new key and counts it nowhere, so a flood of new
  references cannot free a reference whose tries are used up.
- A store of caller addresses (the three of this record and the one of [ADR-0049]) drops the window that runs out
  first, so a sender with more addresses than the store holds cannot make it refuse every other caller.

A store counts up to its limit in one list for each count, so a limit above 100,000 is a mistake of the setup: a
setting above it, like one below one, stops the start-up and names the setting. A restart of the API clears the
counts; the count per email lives in the database and survives it.

**6. Security events, counters and alerts.**

- Three kinds of event in the category of `SecurityEvents` ([ADR-0050] decision 6): `public_booking_made` (each
  booking made on the public site), `guest_lookup_failed` (each guest sign-in or lookup refused for a wrong reference
  or email) and `limit_refused` (each refusal by a limit of decisions 2 and 3, with the name of the limit). Each has
  the address and no email: not whole, not masked to its first character and its domain as the first two versions of
  this record said, and not hashed. No other typed value either. The limit per email of decision 3 has the name
  `mailbox` in the events and the counter, after what it counts. Why (since MIL-089): the code scan found five alerts
  on the events that wrote the masked email (CodeQL 677 to 681, `cs/exposure-of-sensitive-information`), and the
  planning session, deciding as S01 on 2026-11-20, closed them through the code. A masked email still names its
  domain and its first character, and a hash of an email is found again by hashing the emails of a list, although
  ASVS 16.2.5 allows either; the address and the time tell the refusals apart well enough for the alerts and an
  incident.
- A counter `nfhotel.security.events` on the meter `NFHotel.Security`, with the labels `kind` and `limit`, sent with
  the other metrics ([ADR-0023]); in Prometheus it is `nfhotel_security_events_total`.
- Two alerts in `ops/deploy/monitoring/prometheus/rules/security.yml`, routed as the other alerts are, each with a
  rule test: `BookingFlowRefusals`, more than 20 refusals by the limits in 10 minutes, and `PublicBookingSurge`, more
  than 10 public bookings in an hour. The wiki page Alerts names them.

**7. No puzzle and no rule at Cloudflare now.** Both need S01 in an account. When an alert of decision 6 shows that a
sender gets around the limits with many addresses or emails, the next step is a rate limiting rule at Cloudflare for
the public host name (option B), then Turnstile on the booking page (option C), each in its own milestone.

**8. At most 50 open circuits per caller** (option K, since MIL-089, for finding BF-02 and abuse case 11 of
[SAR-002]; a stand-in decision of the build session, which S01 can overrule).

- The web application counts the open requests to `/_blazor` per caller, under the key of decision 2, and answers a
  request over 50 with 429 and no body, so no circuit starts. The negotiation of a circuit counts while it runs, and
  the web socket until the circuit ends. Every other path is not counted.
- 50, because a tab of a browser holds one circuit: the guests and the staff behind one address, the network of the
  hotel among them, keep room. The number is the constant `CircuitLimit.OpenPerCaller` of the code, not a setting; the
  rule ledger names it below its table of the limits.
- The counts live in the memory of the web application, with an entry only for a caller with an open request; a
  restart ends every circuit and clears them.
- A refusal writes `limit_refused` with the limit `circuits` and the address, in the category and the words of
  decision 6, and counts in `nfhotel.security.events` of the web application. `BookingFlowRefusals` counts the
  refusals of the API and of the web application.
- nginx does not count the circuits (option J). The client timeouts of its public listener stay at the 60 seconds of
  nginx, named in its configuration so that `ops/tests/test-nginx.sh` holds them.

## Consequences

**Positive:**

- One sender cannot hold the rooms: five booking requests an hour and twenty a day per address, three coming public
  bookings per email.
- The search for free rooms cannot keep the API busy from one address.
- The audit trail and every limit name the address that nginx saw; a caller cannot write it, and the guest sign-in
  limit of [ADR-0049] now counts the real caller.
- The team hears of a flood from an alert, not from a guest.
- The limits are one table, held by a test, and the counts per address one implementation.
- One caller cannot hold the web application with circuits: 50 per address, or per network of 64 bits (decision 8).

**Negative:**

- Guests behind one shared address (an office, the shared address of a mobile network) share the counts; a refused
  guest calls the hotel. IPv6 counts per network of 64 bits, so the devices of one home share a key.
- A sender with many addresses and many emails gets past the limits; the alerts and the next steps of decision 7 are
  for that.
- A restart of the API clears the counts per address.
- Two booking requests for one email at the same moment can both pass the limit per email, and its refusal tells a
  stranger that an email has three coming bookings, nothing more.
- Bookings made before the deploy do not count per email.
- The log tells the refusals apart by the address and the time, not by the email (since MIL-089). A refused request
  is not stored, and the database holds no caller address, so no one can tie a refusal to a mailbox afterwards: staff
  can only list, in the database, the mailboxes that were at a limit at the time, from the coming public bookings, the
  open requests and the records of their mails. A log that is read or kept longer than meant holds no part of an
  email.
- A sender with many addresses can still open many circuits: an IPv6 network of 56 bits, which many home connections
  have, holds 256 networks of 64 bits, each with its 50 circuits. The alert sees the refusals, not the circuits that
  pass; the next step of decision 7 is for that ([SAR-002] BF-02).
- The 51st open tab of one caller gets its page but no circuit, so the page does not work; once another tab is
  closed, it gets its circuit when it is loaded again (decision 8).
- Every request of the web application carries one more identity in its user.
- A sender with very many addresses can still lock out the guest sign-in for a while: filling the store of references
  with used-up windows takes about 50,000 failed sign-ins in 15 minutes from 2,500 or more addresses (or networks of
  64 bits), and then a reference without a window waits until windows run out. The store of references stays
  closed, because freeing a used-up reference would give a guesser new tries. The failed sign-ins show in the counter
  as `guest_lookup_failed`, which no alert watches; `BookingFlowRefusals` fires only when more than 20 sign-ins are
  refused in 10 minutes.
- The sign-in limit of the mobile app counts a whole IPv6 address, so a sender with a network of 64 bits gets many
  addresses there; its limit per user name still holds.
- Quality attributes: security (ASVS 2.1.3, 2.3.2, 2.4.1, 4.1.3, 15.1.3, 15.2.2, 15.3.4, V16.3) and availability gain;
  interaction capability loses a little for guests behind a shared address; maintainability gains from one table and
  one store.

## Affected Artifacts

- [MIL-087] — the milestone that carries the decision
- [ADR-0049] — decision 6 stands with the address of nginx; its store becomes the shared class, and `unknown` stays
  for a host without an address
- [ADR-0050] — the category of its security events gets three kinds
- [ADR-0004] — stands: the service token now also vouches for the address header
- [ADR-0015] — the mobile app keeps its sign-in limit per address; its address comes from forwarded headers of the
  networks of the stack only, and is counted whole, not by its first 64 bits
- [ADR-0017] — the audit trail records the address that nginx saw
- [ADR-0023] — one more meter
- [ADR-0028] — nginx sets the header on the public listener
- [ADR-0047] — the guest sign-in counts under the new address
- [MIL-036] — two more alerts on the route of the others
- [MIL-085] and [MIL-086] — the limits of the sign-in, and the rules of the fields that this record counts after
- [MIL-088] — the verification by email adds its own limits to the table
- [UC-001] and [UC-010] — the refusals are new steps
- [BC-001] — objective 43 and success criterion 48
- [ADR-0052] — decision 10 holds its events to decision 6: the address and no email
- [MIL-089] — the third version: decision 6 without the email, and decision 8

---

[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-086]: ../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0015]: ./adr-0015-mobile-app-staff-token-access.md
[ADR-0017]: ./adr-0017-staff-passwords-and-audit.md
[ADR-0023]: ./adr-0023-application-telemetry.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0047]: ./adr-0047-guest-login.md
[ADR-0049]: ./adr-0049-booking-reference.md
[ADR-0050]: ./adr-0050-where-the-rules-of-the-booking-input-live.md
[MIL-036]: ../milestones/mil-036-security-operations.md
[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[UC-001]: ../use-cases/uc-001/uc.md
[UC-010]: ../use-cases/uc-010/uc.md
[BC-001]: ../business-case.md
[MIL-089]: ../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[ADR-0052]: ./adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[SAR-002]: ../sqa/security-audit-2026-12-booking-flow.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
