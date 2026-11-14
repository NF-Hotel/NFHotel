# RC-358 Review of the revision of MIL-018 of 2026-11-14

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-358 |
| CrossReference | [MIL-018], [MIL-079], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; accepted at S01's word in chat ("Du godkender for mig") | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-018], its row of 2026-11-14: the read that MIL-079 closed noted in the Purpose, the ADR of task 2 without a fixed number, and the target date moved from 2027-03-19 to 2026-12-11
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: partial review, the changed parts; the plan as a whole was reviewed in RC-084
- Language and domain: en / it (the Metadata rows of MIL-018)
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-14 for every unfinished milestone to be done ("Tjek nu og lav alle milestones") and then gave the acceptance of the plan to the AI ("Du godkender for mig"); the AI walked the checklists and accepted as stand-in for S01.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Unchanged: the ADR, the guest sign-in and the ownership rule, the API endpoints, the guest pages, the smoke test, the proof in the VM; the ADR takes the next free number, because ADR-0007 went to CI/CD |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Unchanged; criterion 8 names the ADR of task 2 instead of ADR-0007 |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | MIL-016, MIL-017 and ADR-0005, unchanged; MIL-079 is now named in the Purpose and the CrossReference, because the lookup it built is the start of the guest sign-in |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objective 21 and success criterion 24 (task 1, RC-083) |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-12-11, the last wave of the plan of 2026-11-14; earlier than before, because MIL-079 already built the lookup |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | Unchanged |
| 3 | Content in the stated language | Pass | English |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | The new paragraph states what is closed and what this milestone adds |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | The booking number, the email of the booking, the guest |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | No change of language or domain |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | The AI as stand-in for S01, not independent |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | No new abbreviation |

## Findings

- **No open hole is left for this milestone to close.** Since MIL-079 the receipt page opens a booking only with its number and its email, and the read of a booking by its key needs `Bookings.View`; cancel and reschedule need `EditOtherBookings` (read in `BookingsController` on main 691fcb5). What remains is a feature: the guest signs in and changes their own booking.
- **The ADR records what MIL-079 decided.** The identity by number and email and the throttle per booking number are taken; the ADR of task 2 records them as taken and decides the rest (the guest token, the cookie, which changes a guest may make).
- **Not independent.** The AI wrote the change and walked the checklists; S01 gave the acceptance to the AI in chat.

## Overall Verdict

Go: every Mandatory criterion of QC-MIL-001 passes for the changed parts. The row of 2026-11-14 of MIL-018 is Accepted and the row of 6b606da Deprecated.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Publish the milestone and its eight issues (`sync-project.sh --apply --accepted-only --milestone MIL-018`) after the merge, and close the issue of task 1 as done (RC-083) | The AI, in autonomous mode | 2026-11-14 |

---

[MIL-018]: ../../milestones/mil-018-guest-login.md
[MIL-079]: ../../milestones/mil-079-a-booking-opened-only-with-its-email.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
