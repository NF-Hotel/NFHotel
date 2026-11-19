# RC-419 Review of the amendment of ADR-0049 after the code review of MIL-085

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-419 |
| CrossReference | [ADR-0049], [QC-ADR-001], [RC-416], [RC-418], [MIL-085], [MIL-077], [MIL-087] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the second row of ADR-0049 (decisions 2 and 6 and the consequences after the code review RC-418); Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [ADR-0049], its second Version History row of 2026-11-19, on the branch
  `mil-085-booking-reference` before its pull request. The first row was reviewed in [RC-416] and is `Deprecated` by
  this one.
- Checklist used: [QC-ADR-001].
- Scope: delta re-review of every criterion, for what the amendment changed: decision 2 (the time of the answer, and
  the reference that the input rules of [MIL-077] refuse first), decision 6 (the key of an IPv6 caller, and what a full
  store drops), the consequences, the CrossReference and the Version History. The reason: the code review [RC-418]
  found that the ADR said more than the code did (F4, F6 and F10 of that record), and that an IPv6 caller could change
  its key for each try (F5). Decisions 1, 3, 4, 5, 7 and 8, the context and the options are unchanged and stand as
  [RC-416] left them.
- Language and domain: n/a (a technical type, IT Professional English)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI wrote the amendment and this record; a second AI, which had not written the code, found the points it answers.

**What each changed sentence rests on**, read in the code of the branch after the fixes of [RC-418]:

| Change | Evidence |
| --- | --- |
| Decision 2, the time of the answer | `GuestAccessService.SignInAsync` reads the booking by its reference, and compares the email with `CryptographicOperations.FixedTimeEquals`, against a stand-in when there is no booking. That method compares two inputs of one length in a time that does not depend on their contents and returns false at once when the lengths differ; a reference that exists costs the read of the booking with its guest and room |
| Decision 2, hostile input first | `InputGuardFilter` applies `InputRules` to every value bound from a request before an action runs: no control character other than tab, line feed and carriage return, no bidirectional control, no lone surrogate (MIL-077). A request that cannot be bound gets 400 with `api.invalid_request` from `InvalidModelStateResponseFactory` (`Program.cs`). `SignIn_ReferenceWithAControlOrDirectionCharacter_GetsTheRefusalOfHostileInputAndCountsNothing` (U+0000 and U+202E) and `SignIn_ReferenceThatIsNoText_GetsTheRefusalOfHostileInputAndCountsNothing` pass, and neither adds a window to the store of addresses |
| Decision 6, the key of an IPv6 caller | RFC 4291 section 2.5.1: the interface identifier of an IPv6 unicast address outside the range that starts with binary 000 is 64 bits long; RFC 8981 lets a host form temporary random interface identifiers and change them. `GuestAccessService.AddressKey` clears the last 64 bits; `SignInAsync_CallerAddressInAnyForm_IsCountedUnderTheKeyOfItsCaller` passes for IPv4, IPv4 in IPv6 form, two addresses of one /64, another /64 and text that is no address |
| Decision 6, what a full store drops | `InMemoryGuestSignInLimits.Store` keeps its windows in the order of their start and in lists by their failed tries, drops the run-out windows first, then the first window of the lowest list below the limit, and refuses a new key when there is none. `TryCount_FullStoreAndAFloodOfNewReferences_KeepsAReferenceAtItsLimitLocked`, `TryCount_FullStore_DropsTheWindowWithTheFewestFailuresFirst`, `TryCount_FullStoreOfReferencesAtTheirLimit_RefusesANewReferenceAndCountsNothing` and `TryCount_FullStoreAndAFloodFromNewAddresses_KeepsAnAddressAtItsLimitLocked` failed on the code before the fix and pass after it (`docs/sqa/evidence/mil-085/review-tests-before.txt` and `review-tests-after.txt`) |
| The consequences | 50,000 = 10,000 windows of five failed tries, and 200,000 = 10,000 windows of twenty; a reference with fewer failed tries goes only after every window with fewer, so after at least the 10,000 new keys that fill the store; [MIL-087] decision 6 sends an alert after more than 20 refusals by the limits in 10 minutes |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged: ADR-0049 in `adr-0049-booking-reference.md` |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | Unchanged. The force "the address a caller can write" covers the new consequence; the flood was among the options of the store of the count, which [RC-416] reviewed |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Unchanged. The way a full store chooses what to drop is a detail of the chosen option (stores of their own with a bound), not a new option |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | Unchanged sentence; decisions 2 and 6 now say what the code does, without a claim that the code does not keep (the cost of the answer, the protection of a full store) |
| 5 | Consequences list both Positive and Negative outcomes | Pass | The positive outcome adds that a flood cannot free a used-up reference. Two negative outcomes are rewritten: the address a caller can write, with what it allows (dodge the limit per address, use up another address, hold more than one /64), and the flood, with its numbers, the closed failure mode, the smaller flood, the restart, and the alert of [MIL-087] |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Security (a used-up reference stays refused; the time of the answer told as it is) against availability (a store full of used-up keys refuses new keys for up to 15 minutes); performance efficiency stays bounded: a new window looks at most at the 20 lists of one store |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | The list is unchanged and each is in the CrossReference, which now also names [MIL-077] (cited in decision 2), the code review [RC-418] and this record |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | A second row, `Accepted` after this review, with a Change summary and `pending` until its commit; the first row keeps its text, is `Deprecated`, and its link points at f8cd896, the commit that made it |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | The ADR is amended, not superseded |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | `/56` and `/48` are prefix lengths of IPv6 and follow the words "prefix of 64 bits"; RFC 4291 is named with its section, RFC 8981 by its number |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Deprecated` and `Accepted` |

## Findings

- **F1, note: an earlier record said more than the code did.** [RC-416] F5 accepted the address that a caller can
  write because "the limit per reference holds regardless". With the store as it was, a flood of new references from
  written addresses could push a used-up reference out of a full store ([RC-418] F4). The record keeps its words, as a
  review record does; the amendment makes the sentence true, because a full store no longer drops a window at its
  limit.
- **F2, note: the sign-in fails closed under a large flood.** When every window of the store of references is at its
  limit, a guest whose reference has no window gets the answer of the limit until windows run out. The AI chose this,
  as stand-in for S01, over a store that frees used-up references: the one guards the email of every booking, the
  other is a wait of at most 15 minutes after at least 50,000 failed tries, which the alert of [MIL-087] reports.
- **F3, note: the address of an IPv6 caller.** A /64 is the prefix of one link; a site that holds a /56 or a /48 has
  256 or 65,536 of them. The ADR names it as a negative outcome, and the limit per reference covers it. [MIL-087]
  makes the address the one that nginx saw and keeps the same key for its own limits per address (action item).

## Overall Verdict

Go. Every applicable criterion of [QC-ADR-001] passes for the amendment, and decisions 2 and 6 now say what the code
of the branch does, which the tests of [RC-418] show. The AI accepted the amendment and this record as stand-in for
S01 in autonomous mode, at S01's word in chat on 2026-11-19; the second row of ADR-0049 is `Accepted` and the first is
`Deprecated`. The reviewer is not independent: an AI wrote the amendment and this record. S01 may challenge F2 above
all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the amendment, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-085-booking-reference` |
| Count an IPv6 caller by its first 64 bits in every limit per address that ADR-0051 decides, and take the limits of the guest sign-in into its table (F3) | The AI | With task 1 of [MIL-087] |

---

[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[RC-416]: ./rc-416-adr-0049.md
[RC-418]: ./rc-418-mil-085-code.md
[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-077]: ../../milestones/mil-077-hostile-input-against-the-api.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
