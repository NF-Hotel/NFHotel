# RC-429 Review of ADR-0051 The limits of the booking flow and the address of a caller, second version

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-429 |
| CrossReference | [ADR-0051], [QC-ADR-001], [MIL-087], [RC-426], [RC-428], [ADR-0049], [ADR-0015], [RC-418], [RC-081], [MIL-089] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the second version of ADR-0051 (task 7 of MIL-087), written after the code review RC-428; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [ADR-0051], its second Version History row, on the branch `mil-087-sender-limits` (the code of
  [MIL-087] merged with `main` at 84ab63f) with the fixes of the code review [RC-428], before its pull request.
- Checklist used: [QC-ADR-001].
- Scope: delta re-review, every criterion over what the second version changed: decision 2 (a refusal by the limit
  per email keeps the counts per address), decision 4 (the forwarded headers of the web application, and the sign-in
  limit of the mobile app), decision 5 (what a full store drops for each kind of key, and the cap of a limit), two
  negative consequences and the bullet of [ADR-0015] under Affected Artifacts. The rest of the record is as [RC-426]
  reviewed it and is not reviewed again.
- Language and domain: n/a (a technical type, IT Professional English)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI wrote both versions of the ADR, [RC-426] and this record; a second AI, which had not written the code, read the
  code for [RC-428], and its points 2, 3, 5, 10 and 11 are what this version answers.

**Why there is a second version.** The code review found that the first version said too little in five places, and
in one of them chose wrong:

- Point 2: with every window of a store at its limit, the first version refused every new key. For the store of
  booking references that is the point of [ADR-0049] ([RC-418]). For a store of caller addresses it let a sender with
  more addresses than the store holds (10,000 keys, which one IPv6 network of 48 bits gives 65,536 times over) make
  the limits refuse every other caller until windows ran out: the flood that the limits are there to stop. The
  second version keeps the store of references closed and has a store of caller addresses drop the window that runs
  out first.
- Point 11: a store keeps a list for every count up to its limit, and nothing bounded the limit; a setting of
  millions would have built millions of lists, and the largest number would have overflowed at the start-up. The
  second version caps a limit at 100,000 and stops the start-up above it.
- Point 3: the web application takes forwarded headers in the deployment, which the first version did not say, so
  the check of the peer sees the address that nginx saw, not nginx. The result is the same; the text was not.
- Point 5: a request that the limit per email refuses keeps its counts per address. The code did so from the start,
  and the first version did not say it.
- Point 10: the sign-in limit of the mobile app counts the address of the connection whole, an IPv6 address too,
  while the first version called `HttpCallerAddress` the one reader.

**What the second version rests on**, read in the code of the branch:

| Change | Evidence |
| --- | --- |
| Decision 2, the refusal per email | `SenderLimitFilter` gives a count back only for 400 and 422 without an exception; the limit per email answers 429 `booking.too_many_requests` from `BookingService.CreatePublicAsync`. `Booking_RefusedByTheLimitPerEmail_KeepsItsCount` holds it at the API: after the refused fourth booking of one email, the sixth request of the address is refused by the hour. It was written with this version and passed at once, because the code did so from the start |
| Decision 4, the forwarded headers | `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in `ops/deploy/nfhotel-web/nfhotel-web.env.example` and in the throwaway stack (`ops/scripts/lib/stack.sh`); with it ASP.NET Core takes `X-Forwarded-For` from any peer and only its last entry; `snippets/proxy-headers.conf` appends the address that nginx saw to that header and writes the same address into `X-NFHotel-Client-Address`; `ops/deploy/nfhotel-web/docker-compose.yml` exposes the port of the web application to its networks and publishes none; [RC-081] accepted the forwarded headers of the web application on that ground |
| Decision 4, the mobile app | `LoginAddressLimitFilter` reads `Connection.RemoteIpAddress`, and `AddressLoginThrottle.KeyOf` keys the whole address, an IPv4-mapped address as IPv4 |
| Decision 5, the full store | `BoundedWindowStore` with `dropsUsedUpWindowsWhenFull`: false for the references of `InMemoryGuestSignInLimits`, true for its addresses and the three stores of `InMemorySenderLimits`. The tests `TryCount_FullStoreOfAddressesAtTheirLimit_CountsANewAddressAndDropsTheFirstToRunOut`, `TryCountSearch_FullStoreOfAddressesAtTheirLimit_CountsANewAddressAndDropsTheFirstToRunOut` and `TryCountBookingRequest_FullStoresOfAddressesAtTheirLimits_CountANewAddressAndDropTheFirstToRunOut` failed before the change, and the tests of [RC-418] that keep a full store of references closed still pass |
| Decision 5, the cap | `BoundedWindowStore.MaxLimit` (100,000), checked in the constructor of the store and by `ReadWindowLimit` at the start-up for the five limits of windows; `Constructor_LimitAboveTheCap_Throws` and `AddInfrastructure_WindowLimitAboveTheCap_Throws` failed before the change, and `AddInfrastructure_WindowLimitAtTheCap_Starts` holds the 100,000 that the tests of the API set |
| The two negative consequences | 10,000 references at 5 failures fill the store of references, and an address may fail 20 times in 15 minutes, so a flood needs 50,000 failures from 2,500 addresses or more; the security events count `guest_lookup_failed`, and `ops/deploy/monitoring/prometheus/rules/security.yml` alerts on `limit_refused` only |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged: ADR-0051 in `adr-0051-limits-of-the-booking-flow-and-the-caller-address.md` |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | Unchanged: the problem, the forces and the options of the first version still hold; the second version decides details within option I |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | The two choices for a full store are both stated with their reasons: closed for references, because freeing a used-up reference gives a guesser new tries; open for addresses, because a closed store lets many addresses refuse every other caller. The other fixes of the review are not choices between options: the code did what the text now says |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | The sentence of option I is unchanged; decisions 2, 4 and 5 state their additions without a hedge |
| 5 | Consequences list both Positive and Negative outcomes | Pass | Two negative points added: the flood that can still close the store of references for a while, with its size and what shows it, and the whole IPv6 address of the sign-in of the mobile app; the positive points hold, and the one of availability is now true for a sender with many addresses too |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Availability gains from the open stores of addresses; security keeps the closed store of references; the cap bounds the memory of a store (performance efficiency); the existing line of quality attributes stands |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | The bullet of [ADR-0015] says that its address is counted whole; [ADR-0015] and [ADR-0049] are in the CrossReference |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | Row 2 is new and `Accepted` after this review, its commit `pending` until the commit; row 1 is `Deprecated` and otherwise unchanged |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | The ADR is not superseded; its own earlier row is deprecated |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | No new abbreviation; `ASPNETCORE_FORWARDEDHEADERS_ENABLED` is the name of a setting, in code format |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Deprecated` and `Accepted` |

## Findings

- **F1, the finding of this review: [RC-426] read one store and drew the rule for all.** [RC-426] checked decision 5
  against the store of [ADR-0049], whose closed rule [RC-418] chose for references, and took it over for the stores
  of addresses without asking what a flood of addresses does to a closed store. The code review found it. [QC-ADR-001]
  asks for the options of a decision, not for the abuse of each; the review of [MIL-089] (the ASVS review and its
  abuse tests) is where such a question is asked of every limit.
- **F2, note: the store of references stays closed.** The AI keeps it closed as stand-in for S01. A sender who fills
  it needs 50,000 failed sign-ins in 15 minutes from at least 2,500 addresses or networks of 64 bits, and keeping it
  full needs as many every 15 minutes; opening it would let a guesser free a used-up reference with a flood, which is
  what [RC-418] closed. No alert watches the failed sign-ins themselves yet; the action below gives that question to
  [MIL-089].
- **F3, note: the forwarded headers of the web application stay as they are.** The second AI offered two fixes: the
  options of the API (the networks of the stack, one entry) in the web application, or the text. The AI chose the
  text as stand-in for S01: both give the address that nginx saw, nginx is the only peer, and [RC-081] accepted the
  setting on that ground.
- **F4, note: the sign-in of the mobile app is outside the booking flow.** Its limit per address counts a whole IPv6
  address, so a sender with one network of 64 bits has many tries there; its limit per user name still holds. The
  second version records it, and [MIL-087] changes nothing of it.

## Overall Verdict

Go. Every applicable criterion of [QC-ADR-001] passes for the second version, which keeps every decision of the
first, makes a full store of caller addresses drop the window that runs out first, caps a limit, and says what the
code does where the first version was silent. The AI accepted the second version of ADR-0051 and this record as
stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19; row 2 of ADR-0051 is `Accepted` and row 1
`Deprecated`. The reviewer is not independent: an AI wrote the ADR, [RC-426] and this record. S01 may challenge any
decision of it, F2 and F3 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the second version of ADR-0051, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-087-sender-limits` |
| Ask of every limit what a flood of addresses does to it, and whether the failed guest sign-ins need an alert of their own (F1, F2) | mikemike | With the ASVS review and the abuse tests of [MIL-089] |

---

[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[RC-426]: ./rc-426-adr-0051.md
[RC-428]: ./rc-428-mil-087-code.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[ADR-0015]: ../../adr/adr-0015-mobile-app-staff-token-access.md
[RC-418]: ./rc-418-mil-085-code.md
[RC-081]: ./rc-081-tls-code.md
[MIL-089]: ../../milestones/mil-089-the-booking-flow-attacked-and-measured.md
