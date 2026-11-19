# RC-423 Review of the documents of MIL-086: the rules of the booking input

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-423 |
| CrossReference | [UC-001], [UC-003], [UC-010], [QC-UC-001], [QC-LANG-001], [MIL-086], [ADR-0050], [BC-001], [SA-001], [RC-411], [RC-417], [RC-420], [RC-421], [RC-424] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the documents that MIL-086 adds and amends (tasks 2 and 7); Go; mode autonomous, not independent | [7de27c7] |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-19 on the branch `mil-086-validation-rules` (from `main` at bac28ba)
  before its pull request:
  - [UC-001]: business rule 1, which adds the five years for every booking, also one that hotel staff make, keeps the
    30 nights to the public site and sends the details of a new guest to the table of the rules; business rule 5b,
    which adds the five years when a booking is moved.
  - [UC-003]: business rule 4, which sends each detail of a guest to its row of the table and says what each row
    allows, and a second row 4: a guest stored before MIL-086 keeps its details until hotel staff edit it, and the edit
    meets the rules ([ADR-0050] decision 8).
  - [UC-010]: business rule 1, 2, the email compared in its normal form, and neither value longer than a booking
    reference or the email of a booking can be; business rule 3, 4, new dates that end at most five years after today
    and have at most 30 nights, only the allowed days offered, and a move that breaks a rule refused with the text of
    the rule; [MIL-086] and [ADR-0050] in the CrossReference.
- Checklists used: [QC-UC-001] (Fully Dressed) for the three use cases, and [QC-LANG-001] for them, because each is
  written in the PO language.
- Scope: delta review against every criterion of each checklist for what the amendment changed. The rest of each use
  case stands as its latest record left it: [RC-417] for UC-001 and UC-010, and RC-032 for UC-003.
- Not under a checklist, read against the deliverable of [MIL-086], [ADR-0050] and the code of the branch
  ([RC-424]): the rule ledger `docs/refactor/BusinessRules.md` (the table "Validation rules of the booking flow" of
  task 2, BR-N-08, BR-C-07, the two notes under the table, and the column "Status in new code" of the rows that the
  milestone changes), the test plan of the site `docs/refactor/Testplan.md`, the wiki page Security-Controls, the
  class diagram `docs/dcd.md` with its copy `docs/dcd.puml`, the evidence in `docs/sqa/evidence/mil-086/`, and the
  traceability matrix.
- Stand-in decision: [UC-003] is in this review although task 7 of [MIL-086] names UC-001 and UC-010 only. Its rule 4
  is the rule of the guest details, which the milestone changes; left alone, it would say less than the table and
  the code.
- Left as they are (F4): the analysis documents of the refactor that describe the old system and the first design,
  among them `docs/refactor/Analysis.md` and `docs/refactor/Architecture-Domain.md`, and the column "Rule" of the
  ledger, which records the old system as it was found.
- Language and domain: en / it (the Metadata rows of the three use cases).
- Language reviewer: none; the reviewer reads English and knows the domain.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"); the
  plan of [MIL-086] was accepted by the AI as stand-in for S01 in [RC-411]. An AI wrote the amendments, and an AI
  wrote this record.

## Checklist Results: UC-001 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged: the limits are rules of the steps, not new conditions |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged; no stakeholder gains or loses an interest |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps are unchanged; the limits are in the rules of steps 1 and 5b |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Row 1: BR-41 to BR-48, the five years of BR-N-08 for every booking, the 30 nights of BR-C-07 on the public site only, and the details of a new guest after the table; row 5b: the five years when a booking is moved |
| 10 | Naming consistent with the diagram and the stories | Pass | Hotel staff, booking and guest, as in UCD-001 and US-001 |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | The rows name the rules and the table of the ledger; no page, field or class |

## Checklist Results: UC-003 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps are unchanged; the rules of the details are rows of step 4 |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Row 4: the required details and what each row of the table allows; a second row 4: a guest stored before keeps its details until it is edited, and the edit meets the rules (decision 8) |
| 10 | Naming consistent with the diagram and the stories | Pass | Hotel staff, guest, country and passport number, as in the domain model of the use case and the dictionary |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | After F2 the rows say in plain words what a detail may hold; "chosen from the list" names the set of countries, not a control of a page |

## Checklist Results: UC-010 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged since [RC-417] |
| 5 | Primary actor is explicitly stated | Pass | Guest, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps are unchanged; the limits are rules of steps 1 and 2 and of steps 3 and 4 |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Row 1, 2: the email in its normal form ([ADR-0050] decision 4) and the length of the two values; row 3, 4: the five years and the 30 nights (BR-N-08, BR-C-07), only the allowed days offered, and a move refused with the text of the rule |
| 10 | Naming consistent with the diagram and the stories | Pass | Booking reference and guest, as in UCD-001 and US-001.13 |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | After F1: no field, page or sending of a request is named |

## Language and Domain Results (the three use cases)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Language and Domain rows present | Pass | en / it in each |
| 2 | BCP 47 code and a registry domain | Pass | |
| 3 | Content in the stated language | Pass | English |
| 4 | The register of the type | Pass | The register of the earlier rows is kept |
| 5 | Domain terms are the PO terms of the dictionary | Pass | Guest, booking reference and stay; hotel staff, the actor of UC-001 and UC-003; "public site" names the channel of UC-010 |
| 6 | Structural vocabulary in English | Pass | |
| 7 | No translated twin | Pass | |
| 8 | A change of language reviewed again | N-A | No change of language |
| 9 | A reviewer competent in the domain and the language confirmed the terms | Pass | The AI as stand-in for S01, who reads English and knows the domain; not independent |
| 10 | Abbreviations spelled out on first use | Pass | After F2 UC-003 says "the letters A to Z and the digits 0 to 9" in place of "ASCII"; "UUID" in UC-010 is spelled out since [RC-417] |

## The documents without a checklist, against the deliverable of MIL-086

- **Rule ledger, the table of task 2.** Forty rows: one per field of the request types of the flow, per query value of
  the free rooms and of the guest search, and per value of the address of the booking page, each with its type,
  whether it is required, its length, its characters or values, its normalisation, the rules that join it to other
  fields, its codes, its sources and the members that implement it. The rules above the table (the hostile-input
  rules of MIL-077, the 1,000 characters of a text without a rule, the 64 KB of a body with the benchmark intake as the
  one exception, the refusal of an unknown field, the normal form, and the answer and event of a refusal) are the
  decisions of [ADR-0050]. `ValidationRulesTableTests` holds the rows against the request types of the API (a field
  without a row, a row without a field, a wrong type, a text without a length, a member that does not exist) and
  `BookingAddressRulesTableTests` holds the rows of `/book` against the page; both pass. Read against the code of the
  branch: the five years (`Booking.LatestDeparture`, with 29 February), the 30 nights, the party of 1 to 4, the names,
  the email (100 characters, 64 before the `@`, labels of at most 63), the phone number (50 characters, 7 to 15
  digits), the 249 countries, the passport number (20 letters and digits) and the 200 characters of a search say what
  the code does.
- **Rule ledger, the rest.** BR-N-08 (the five years, counted in calendar years) and BR-C-07 (the 30 nights on the
  public site) are new; the note on the address of the booking page records that a party outside 1 to 4 is now
  ignored, not brought to the nearest of the two, and the note on the room size that a public booking without a size
  is refused. In the column "Status in new code", BR-54, BR-55, BR-57, BR-93 and BR-94 say that MIL-086 changed them,
  and, after F3, BR-52, BR-53, BR-56, BR-91, BR-92 and BR-95 say that it extends them. The row of the search text of
  the booking overview says that white space only is no search, as `TextNormalForm.NormalizeOptional` makes it.
- **Test plan of the site.** The header note; in section 3 the negative rows of the email (also two addresses), a
  first name with a digit or markup, a phone number of two digits, the country as a list, 31 nights, five years and a
  day, and a party of five in the address; in section 4 the stay of 31 nights that staff may book and the five years
  and a day that staff may not; and a check of the guest form for a stored country that is not on the list. The texts
  are those of `ErrorMessages` on the branch.
- **Wiki Security-Controls.** The row "One set of rules for the booking input" names the threats, the controls, the
  reason, the alternatives and the remaining risks. Checked against the code and the configuration of nginx: 64 KB at
  the API and at the public listener, the benchmark intake at 30,000,000 bytes, 60 seconds in the shared proxy
  settings and in `optional/api.conf` and an hour only at `/_blazor`, and the event `input_refused` in the category
  `NFHotel.Security` with the method, the route template, the status and the codes, never the value.
- **Class diagram.** Task 7 does not name it, and the code review found that it did not draw the new types of the
  domain ([RC-424] F3). `Booking` now has its static members of the stay (the five years, the 30 nights, the party
  of 1 to 4 and the four checks), and the block "Validation rules of the booking input (MIL-086, ADR-0050)" draws
  `TextNormalForm`, `Countries` with its `IsoCountry` records, and the five attributes of the guest details with
  their limits; `Guest` depends on the normal form and on each attribute, and `CountryNameAttribute` on the list.
  The PlantUML block of `dcd.md` and `dcd.puml` are the same text, and PlantUML 1.2024.7 draws it without an error,
  offline. The diagram keeps its older differences from the code, such as `nationality` for the country.
- **Evidence.** `README.txt` says what each file shows: the first structure tests that refused the first place of the
  rules (the finding of [RC-421]), the failing run of the tests written first, the three tests of the normal form run
  before their code, and the run after the code; the run after the fixes of the code review is listed in [RC-424].
  No file holds the public address or personal data.
- **Traceability matrix.** The row of [MIL-086] with what was built in place of what was planned, the row of
  [ADR-0050] with [RC-420], [RC-421] and [RC-424], and this record on the rows of UC-001, UC-003 and UC-010.

## Findings

- **F1, corrected while reviewing: page words in UC-010.** Rule 1, 2 said that "the two fields" take no more
  characters than a reference and an email, and rule 3, 4 that "the date fields" offer only the allowed days and that a
  move is refused "before it is sent", which is user interface and implementation detail (QC-UC-001 12). The rules now
  say that neither value may be longer than a booking reference or the email of a booking can be, that the guest is
  offered only the days that the rules allow, and that a move that breaks a rule is refused with the text of the rule.
  The change row of UC-010 says the same.
- **F2, corrected while reviewing: an abbreviation in UC-003.** Rule 4 said "ASCII letters and digits in a passport
  number" without spelling the abbreviation out (QC-LANG-001 10); it now says "the letters A to Z and the digits 0 to
  9", which is the rule in plain words.
- **F3, corrected while reviewing: six rows of the ledger did not say that MIL-086 extends them.** BR-52, BR-53,
  BR-91 and BR-92 (a name is required) and BR-56 and BR-95 (a country is required) still hold, and MIL-086 adds the
  characters of a name and the list of countries. Their column "Status in new code" was empty, so a reader of the old
  rule would not find the new one; each now names the table and its rule, as BR-54 and BR-55 do.
- **F4, note: where the old rules stay.** The column "Rule" of the ledger records the old system as it was found, and
  the analysis documents of the refactor (`Analysis.md`, `Architecture-Domain.md` and the others marked as history)
  describe the old system and the first design. They are left as they are, as [RC-417] left them for the booking
  number.
- **F5, note: the CrossReference of UC-001 and UC-003.** It keeps its three entries, as [RC-417] left UC-001 after
  MIL-085: the rules of the two use cases name MIL-086 and ADR-0050 in their text. UC-010 already cited the decision
  records behind its rules, so it takes [MIL-086] and [ADR-0050] as well.
- **F6, corrected after this review, from the code review: the codes of a key below one.** The row of
  `RescheduleBookingRequest.BookingId` promised `booking.not_found` for a key that is missing or below one; the
  service refuses such a key as an error of the caller, and the API answers 400 with `api.invalid_request` and the
  event ([RC-424] F10). The row now says so, as the rows of the other keys do.

## Overall Verdict

Go, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of [QC-UC-001] and
[QC-LANG-001] passes for what the amendment changed, after F1 and F2; the documents without a checklist say what the
deliverable of [MIL-086] and [ADR-0050] say and what the code of the branch does ([RC-424]), after F3 and F6. The
reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| The commits, the commit links resolved, and the pull request that closes the issues of tasks 1 to 7 | The AI | 2026-11-19 |
| The check on the live site, the gate review and the row of MIL-086 at the gate | The AI | Task 8 of [MIL-086] |

---

[UC-001]: ../../use-cases/uc-001/uc.md
[UC-003]: ../../use-cases/uc-003/uc.md
[UC-010]: ../../use-cases/uc-010/uc.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[ADR-0050]: ../../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[BC-001]: ../../business-case.md
[SA-001]: ../../stakeholder-analysis.md
[RC-411]: ./rc-411-mil-086.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-420]: ./rc-420-adr-0050.md
[RC-421]: ./rc-421-adr-0050-where-the-rules-live.md
[RC-424]: ./rc-424-mil-086-code.md
[7de27c7]: https://github.com/NF-Hotel/NFHotel/commit/7de27c769b753231a5cf2dc8e786c8197b4bda81
