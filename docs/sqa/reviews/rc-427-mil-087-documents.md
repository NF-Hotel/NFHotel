# RC-427 Review of the documents of MIL-087: the limits of the booking flow

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-427 |
| CrossReference | [UC-001], [UC-010], [DM-001], [DICT-001], [QC-UC-001], [QC-DM-001], [QC-DICT-001], [QC-LANG-001], [MIL-087], [ADR-0051], [BC-001], [SA-001], [RC-412], [RC-417], [RC-423], [RC-426], [RC-428], [ADR-0049] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the documents that MIL-087 adds and amends (tasks 2 and 7); Go; mode autonomous, not independent | [a574bf6] |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-19 on the branch `mil-087-sender-limits` (from `main` at 886c8e8)
  before its pull request:
  - [UC-001]: the extension 1a, a search for free rooms that goes over the limit of one address is refused and shows
    no rooms; business rule 1, the limits of the public site on booking requests per address and on coming bookings
    per email neither count nor limit a booking by hotel staff; business rule 1a, 60 searches a minute per address of
    the caller, the address from which the search reached the site, and the one text of a refusal.
  - [UC-010]: business rule 2b, the failed attempts from one address are counted under the address from which they
    reached the site, which no caller can set; [MIL-087] and [ADR-0051] in the CrossReference.
  - [DM-001]: the attribute "made on the public site" of Booking, in the diagram and in the concept table.
  - [DICT-001]: the row "Made on the public site" (`MadeOnPublicSite`).
- Checklists used: [QC-UC-001] (Fully Dressed) for the two use cases, [QC-DM-001] for DM-001, [QC-DICT-001] for
  DICT-001, and [QC-LANG-001] for all four, because each is written in the PO language.
- Scope: delta review against every criterion of each checklist for what the amendment changed. The rest of each
  document stands as its latest record left it: [RC-423] for UC-001 and UC-010, and [RC-417] for DM-001 and DICT-001.
- Not under a checklist, read against the deliverable of [MIL-087], [ADR-0051] and the code of the branch (the code
  review of MIL-087): the rule ledger `docs/refactor/BusinessRules.md` (the table "Limits of the booking flow" of task
  2 and the three notes under it), the wiki pages Security-Controls and Alerts, the class diagram `docs/dcd.md` with
  its copy `docs/dcd.puml`, the evidence in `docs/sqa/evidence/mil-087/`, and the traceability matrix.
- Stand-in decision: DM-001, DICT-001 and the class diagram are in this review although task 7 of [MIL-087] names the
  ledger, UC-001, UC-010 and the two wiki pages only. The code gives Booking an attribute that the limit per email
  reads; left alone, the models would say less than the code, as [RC-417] and [RC-423] found for the two milestones
  before.
- Language and domain: en / it (the Metadata rows of the four documents).
- Language reviewer: none; the reviewer reads English and knows the domain.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"); the
  plan of [MIL-087] was accepted by the AI as stand-in for S01 in [RC-412]. An AI wrote the amendments, and an AI
  wrote this record.

## Checklist Results: UC-001 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged: the limit is a rule of a step and an extension, not a new condition |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged; 60 searches a minute from one address do not stop the work of hotel staff (ADR-0051 decision 2) |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps are unchanged; the refused search is extension 1a |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Row 1: the limits of the public site neither count nor limit a booking by hotel staff (ADR-0051 decisions 2 and 3); row 1a: 60 searches a minute per address of the caller and the one text of a refusal |
| 10 | Naming consistent with the diagram and the stories | Pass | Hotel staff, booking, room and public site, as in UCD-001, US-001 and the rows of [RC-423] |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | After F1: no component, header or status code is named |

## Checklist Results: UC-010 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged since [RC-423] |
| 5 | Primary actor is explicitly stated | Pass | Guest, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps and extension 2b are unchanged; only the rule of 2b says which address is counted |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Row 2b: the address of the count is the one from which the attempt reached the site, which no caller can set (ADR-0051 decision 4) |
| 10 | Naming consistent with the diagram and the stories | Pass | Booking reference, guest and address, as in UCD-001 and US-001.13 |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | After F1: no component is named |

## Checklist Results: DM-001 (QC-DM-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Business language, no technical jargon | Pass | "made on the public site"; neither the flag nor the column is named |
| 2 | Multiplicities | N-A | No association changed |
| 3 | No operations | Pass | Attributes only |
| 4 | Association names | N-A | No association changed |
| 5 | Generalization | N-A | None |
| 6 | Every concept traces to a noun phrase | Pass | The attribute follows "a booking made on the public site" of UC-001 business rule 1 and the glossary row of [DICT-001] |
| 7 | Attributes are simple domain data | Pass | A yes or no of the booking itself; it points at no other concept |
| 8 | The Purpose names the concerns | N-A | Unchanged |

## Checklist Results: DICT-001 (QC-DICT-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every row complete | Pass | Made on the public site, en, `MadeOnPublicSite`, its definition, its use |
| 2 | One IT term per PO term and the reverse | Pass | `MadeOnPublicSite` for Made on the public site, and no other term for either |
| 3 | Every Domain Model concept has a row | Pass | Unchanged concepts; the new attribute has its row |
| 4 | The design artifacts use the IT term | Pass | The class diagram writes `madeOnPublicSite`, in the camel case of its other attributes |
| 5 | Definitions in one sentence of the PO language | Fail, then Pass | F2: the definition had two sentences; it is now one |
| 6 | The types named exist | Pass | DM and UC as PO term; DCD and ERD as IT term, as the other rows of a booking say |
| 7 | Language and Domain rows match the registry | Pass | en / it |
| 8 | Rows in ascending order of the PO term | Pass | Made on the public site stands between Last name and Must change password |

## Language and Domain Results (the four documents in the PO language)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Language and Domain rows present | Pass | en / it in each |
| 2 | BCP 47 code and a registry domain | Pass | |
| 3 | Content in the stated language | Pass | English |
| 4 | The register of the type | Pass | The register of the earlier rows is kept |
| 5 | Domain terms are the PO terms of the dictionary | Pass | Booking, guest, hotel staff, room and the new term Made on the public site |
| 6 | Structural vocabulary in English | Pass | |
| 7 | No translated twin | Pass | |
| 8 | A change of language reviewed again | N-A | No change of language |
| 9 | A reviewer competent in the domain and the language confirmed the terms | Pass | The AI as stand-in for S01, who reads English and knows the domain; not independent |
| 10 | Abbreviations spelled out on first use | Pass | No new abbreviation |

## The documents without a checklist, against the deliverable of MIL-087

- **Rule ledger, the table of task 2.** Six rows: the search, the booking requests per hour and per day, the email, and
  the two limits of the guest sign-in of [ADR-0049], each with who is counted, what, the window, the number, the setting
  and the refusal. Read against the code of the branch: the defaults of `SenderLimitOptions` are 60, 5, 20, 3 and 10,000
  keys; the names of the first column are the constants of `LimitNames`, which the event `limit_refused` writes; the
  code `booking.too_many_requests` is in `ErrorCodes.Booking`. `LimitsTableTests` holds the table against the defaults,
  the settings, the names and the codes, and passes. The notes under the table say what the code does: a window opens
  with its first count, a request that a limit per address refuses counts in no window, a request that the use case
  refuses with 400 or 422 gets its count back and one that the limit per email refuses keeps it, the stores hold at most
  10,000 keys, a full store drops what [ADR-0051] decision 5 says, a limit is at most 100,000, a restart clears the
  stores, the count per email comes from the database, the pages show one text and `Retry-After` gives whole seconds,
  the refusal per email has no `Retry-After`, and the three events. The notes were read again after the fixes of the
  code review [RC-428] (F2, F5 and F11), which changed what they say of a refused request, a full store and the bound of
  a limit.
- **Wiki Security-Controls.** The row Rate limits names the new threats, the limits with their numbers and the
  address that nginx writes and the API takes only from a call with the service token; its remaining risks drop the
  first entry of `X-Forwarded-For`, which MIL-087 closes, and add what stays: a botnet, two requests for one email at
  the same moment, guests behind one address. The row of the input rules no longer says that the limits per caller
  are to come. The row of the logs says that the events hold the address and a masked email for the 14 days of Loki.
  In section 9 the row of the security alerts names the two new alerts, and a new row names the three events and the
  counter. After [RC-428] the remaining risks of the row Rate limits also say what a flood of addresses can still do to
  the store of references, that a full store of addresses drops the window that runs out first, and that the sign-in
  of the mobile app counts a whole IPv6 address.
- **Wiki Alerts.** The section "Booking flow" under the alert rules says what the counter holds and names
  `BookingFlowRefusals` and `PublicBookingSurge` with their source, threshold, severity, reason and what to do; the
  thresholds are those of `rules/security.yml`, and its descriptions point at this section. The test of the wiki
  coverage passes, 126 of 126.
- **Class diagram.** `Booking` has `+boolean madeOnPublicSite`. The PlantUML block of `dcd.md` and `dcd.puml` are the
  same text.
- **Evidence.** `README.txt` says what each file shows: the failing run of the tests written first, and the runs after
  the code and after the code review. No file holds the public address or personal data.
- **Traceability matrix.** The row of [MIL-087] with what was built, the row of [ADR-0051] with its reviews, and this
  record on the rows of UC-001 and UC-010.

## Findings

- **F1, corrected while reviewing: a component and a status code in the use cases.** Business rule 1a of UC-001 said
  "the address that nginx saw" and "the status 429 ... and the whole seconds to wait", and rule 2b of UC-010 "the one
  that nginx saw", which is implementation detail (QC-UC-001 12). The rules now say the address from which the search
  or the attempt reached the site, which the caller cannot set, and that a refused search shows the one text of a
  refusal; the change row of UC-010 says the same. The ledger and the wiki keep nginx and the header, where they
  belong.
- **F2, corrected while reviewing: two sentences in a definition of the dictionary.** The row "Made on the public site"
  defined the term and then said which bookings count, in two sentences (QC-DICT-001 5). It is now one sentence.
- **F3, note: no new use case for the public booking.** The public booking has no use case of its own; its limits are
  in the ledger and in [ADR-0051], and UC-001 says that they do not hold for hotel staff. [MIL-088] writes UC-011 for
  the booking request on the public site, which takes them up.

## Overall Verdict

Go, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of [QC-UC-001],
[QC-DM-001], [QC-DICT-001] and [QC-LANG-001] passes for what the amendment changed, after F1 and F2; the documents
without a checklist say what the deliverable of [MIL-087] and [ADR-0051] say and what the code of the branch does.
The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the rows of UC-001, UC-010, DM-001 and DICT-001, or return them | S01; done on 2026-11-19 by the AI as stand-in, at S01's word in chat | At the review of the pull request of MIL-087 |
| Take up the limits of the public booking in UC-011 (F3) | The AI | With MIL-088 |

---

[UC-001]: ../../use-cases/uc-001/uc.md
[UC-010]: ../../use-cases/uc-010/uc.md
[DM-001]: ../../use-cases/uc-001/dm.md
[DICT-001]: ../../dictionary.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[BC-001]: ../../business-case.md
[SA-001]: ../../stakeholder-analysis.md
[RC-412]: ./rc-412-mil-087.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-423]: ./rc-423-mil-086-documents.md
[RC-426]: ./rc-426-adr-0051.md
[RC-428]: ./rc-428-mil-087-code.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[a574bf6]: https://github.com/NF-Hotel/NFHotel/commit/a574bf6192840701214b20e4827c0748fb00c251
