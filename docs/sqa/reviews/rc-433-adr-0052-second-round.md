# RC-433 Review of ADR-0052 A booking takes a room once its email is verified, second round

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-433 |
| CrossReference | [ADR-0052], [QC-ADR-001], [MIL-088], [MIL-087], [ADR-0051], [ADR-0053], [DM-004], [RC-431], [RC-434] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the second review of ADR-0052 by a second AI, after the rework that RC-431 asked for; Go-with-conditions, the conditions closed in the text accepted here; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instance reviewed: [ADR-0052], reworked after [RC-431], in the worktree of the branch
  `mil-088-booking-verified-by-email` (from `main` at 6f7bb80), before any code of [MIL-088].
- Checklist used: [QC-ADR-001], with the decisions of [MIL-088] and the code at 6f7bb80.
- Scope: delta re-review. The second AI checked each finding of [RC-431] against the reworked text and every criterion
  over the whole record; the earlier record is [RC-431] (No-Go).
- Language and domain: n/a (a technical type, IT Professional English)
- Language reviewer: none
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"). An
  AI wrote the ADR, [RC-431] and this record. The second AI of [RC-431] reviewed the reworked text, read the code at
  6f7bb80 again and the texts it cites, and changed nothing; it reviewed [ADR-0053] in the same pass, and its
  findings there are in [RC-434]. The AI that wrote the ADR closed the conditions below and checked them as stand-in
  for S01; the second AI did not review the text a third time.

**The findings of [RC-431]**, as the second AI found them in the reworked text:

- Closed: 1 (one save through `Booking.CreateForNewGuest`), 3 (the key of the mailbox and the one lock), 5, 7, 9 and
  15 (every listed artifact a link and in the CrossReference; [DM-004] exists).
- Closed with a gap left: 2 (see condition 6), 4 (see condition 8) and 8 (see condition 9).
- Two fixes differ from the ones the second AI had in mind, and both close their findings: the claim is committed
  before the booking instead of inside its transaction, and every request takes one lock of the database instead of
  one lock per mailbox. Both decide the races the same way every time, and the record states their costs.

## Checklist Results

The results of the text accepted here, after the conditions below were closed.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | The problem now names the counting by email of [ADR-0051]; the texts add the help pages of Gmail on dots and on `+`, with no rule for `googlemail.com` |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Unchanged: five options, four lost with a reason each |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | Option E, in ten numbered decisions |
| 5 | Consequences list both Positive and Negative outcomes | Pass | The negative list now holds the catch-all domains, the request replaced by someone who knows the email, the copies of the link outside the server, the one lock, the hashes of the mailboxes kept for a day, and the rooms lost in a burst beyond five |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Security, reliability, interaction capability and maintainability, with the requirements of ASVS each rests on |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | Nine artifacts, each a link and in the CrossReference; UC-011 and the rule ledger, which do not exist yet, are named in prose with the task that writes them |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | One row, set to `Accepted` with this review; it cites [RC-431] and this record; its commit `pending` until the commit |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Nothing is superseded |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | OWASP, ASVS, AI and SHA-256 are spelled out on first use; OAT-021 is the name of a threat and follows the words "automated threat" |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Accepted` |

## Findings

The conditions of the second AI, numbered as it numbered them, and how each is closed in the text accepted here:

- **6. A guest loses the request while rooms are still free.** The claim is gone whatever follows, and the public
  booking chooses the same first room for every guest of a size, so guests who verify at once for a size with several
  free rooms all aim at one room, and the losers lose their requests. Closed: decision 5 takes back the booking and
  its guest after an exclusion conflict and tries the next free room of the size that sleeps the party, up to five
  rooms in all; the negative consequence says that only the last rooms, or a burst beyond five rooms, are lost.
- **7. How a booking is counted by mailbox.** Bookings hold no key of a mailbox, and the count of [MIL-087] matches the
  email of the guest without letter case. Closed: decision 6 says that the query reads the emails of the coming public
  bookings whose email ends in the same domain, both Gmail domains for a Gmail address, and that the API compares
  their mailbox forms, with no new column, so the bookings made since [MIL-087] count at once; the entry of [DM-004]
  says the same.
- **8. `googlemail.com`.** Its dot forms counted as separate mailboxes, and "other providers deliver the `+` form" had
  no source. Closed: the mailbox form treats `googlemail.com` as `gmail.com`, on the rule of the record that counting
  two forms as one only makes a limit stricter; the context says "many providers" and that Google's help names no rule
  for `googlemail.com`.
- **9. The path with the setting off.** It now stores the guest and the booking in one save, but only the consequences
  said so. Closed: decision 8 says it, and that the path moves on to the next free room as decision 5 does.
- **10. A second code of 429.** `booking.too_many_mails` departs from the one code of [ADR-0051] decision 2. Closed:
  decision 6 gives the reason (the links are already in the mailbox, so the page sends the guest to the newest mail
  instead of asking the guest to wait), says that the refusal has no `Retry-After`, as the limit per email of
  [ADR-0051] decision 3 has none, and the entry of [ADR-0051] names the code.
- **11. No record of the first review.** Closed: [RC-431] and this record exist, and the Version History row cites
  both.

## Overall Verdict

Go-with-conditions, with the conditions closed. The second AI found the reworked decisions able to hold and the
checklist passed; conditions 6 to 11 are closed in the text that this record accepts, as the findings above show.
The AI accepted [ADR-0052] as stand-in for S01 in autonomous mode, at S01's word in chat on 2026-11-19, and set its
row to `Accepted`. The reviewer is not independent: an AI wrote the ADR and this record and closed the conditions,
and a second AI gave the findings. S01 may challenge any decision of the record, the five rooms of condition 6 and the
code of condition 10 above all.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Close conditions 6 to 11 in the text of [ADR-0052] | mikemike; done on 2026-11-19 | Before the acceptance |
| Carry out the next free room after a lost race, the count of the bookings by mailbox with both Gmail domains, and the code `booking.too_many_mails` without `Retry-After`, each with a test written first | mikemike | The code of [MIL-088], in its code review |

---

[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[DM-004]: ../../domain-model.md
[RC-431]: ./rc-431-adr-0052.md
[RC-434]: ./rc-434-adr-0053-second-round.md
