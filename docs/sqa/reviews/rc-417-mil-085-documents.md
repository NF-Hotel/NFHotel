# RC-417 Review of the amendments of MIL-085 to the documents that named the booking number

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-417 |
| CrossReference | [UC-010], [SSD-001], [OC-001], [UC-001], [DM-001], [DM-004], [DICT-001], [US-001], [UCD-001], [ADR-0047], [ADR-0005], [QC-UC-001], [QC-SSD-001], [QC-OC-001], [QC-DM-001], [QC-DICT-001], [QC-US-001], [QC-UCD-001], [QC-ADR-001], [QC-LANG-001], [MIL-085], [ADR-0049], [BC-001], [SA-001], [RC-416], [RC-418], [RC-388], [RC-389], [RC-390] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the documents that MIL-085 amends (task 5); Go; mode autonomous, not independent | [e551458] |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-19 on the branch `mil-085-booking-reference` (from `main` at 04c0a87,
  with `main` at 46b29fb merged in) before its pull request:
  - [UC-010]: the booking reference in the primary actor, the preconditions, the postconditions, steps 1 and 2 and the
    extensions 1a, 2a and 2b; the interest of S03; the business rules of steps 1 and 2, 2a and *a, 2b, 2 and all,
    which name the decisions of [ADR-0049]; [MIL-085] and [ADR-0049] in the CrossReference.
  - [SSD-001]: `signIn(bookingReference, email)` in both diagrams, the refused sign-in counted per reference and per
    address, and the row of signIn in the operations table.
  - [OC-001]: CO1 with the reference, the receipt as its answer, the counts per reference and per caller address, and
    the refusal of a reference that cannot be read; the `BookingReceipt` defined; CO2 and CO3 answer with the receipt
    and lose the refusals of another booking or room, because they name neither; [ADR-0049] in the CrossReference.
  - [UC-001]: step 3 gives a new booking a booking reference.
  - [DM-001] and [DM-004]: the attribute booking reference of Booking in the diagram and the table, with its glossary
    entry.
  - [DICT-001]: the row Booking reference (`BookingReference`) in place of Booking number, at the same place in the
    order; the Guest sign-in opened with the reference.
  - [US-001]: the story US-001.13 and three of its acceptance criteria (the reference shown, the old number refused
    like an unknown reference, the limit per address).
  - [UCD-001]: the row of View and Change Own Booking in the use case table.
  - [ADR-0047] and [ADR-0005]: a current-state paragraph that names [ADR-0049]; no decision of either changed.
- Checklists used: [QC-UC-001] (Fully Dressed), [QC-SSD-001], [QC-OC-001], [QC-DM-001], [QC-DICT-001],
  [QC-US-001], [QC-UCD-001], [QC-ADR-001] for the two current-state rows, and [QC-LANG-001] for the eight documents
  written in the PO language (UC-010, SSD-001, UC-001, DM-001, DM-004, DICT-001, US-001, UCD-001).
- Scope: delta review against every criterion of each checklist for what the amendment changed. The rest of each
  document stands as its latest record left it: [RC-389] for UC-010, SSD-001 and OC-001; [RC-390] for US-001, UCD-001,
  DM-004 and DICT-001; RC-030 for UC-001; RC-033 for DM-001; [RC-388] for ADR-0047 and ADR-0005.
- Not under a checklist, read against the deliverable of [MIL-085] and the code of the branch ([RC-418]): the working
  documents without an ID, which are the rule ledger `docs/refactor/BusinessRules.md` (BR-49, BR-109, BR-C-05, BR-C-06
  and the new note On BR-109), the Access Control Matrix (its Guest row and two notes; no cell changed), and the test
  plan of the site `docs/refactor/Testplan.md` (steps 4 to 7 of the booking flow and what to look for); the class
  diagram `docs/dcd.md` with its copy `docs/dcd.puml`, which have no ID and no Version History; the wiki pages
  Security-Controls, Public-Access and Proxmox-Deployment; and the traceability matrix.
- Left as they are, with the reason (F2, F3): objective 21 and a scope line of [BC-001], the conflict row of [SA-001],
  the decision records in the words of their day, and the analysis documents of the refactor that are marked as
  history.
- Language and domain: en / it (the Metadata rows of the eight documents in the PO language); OC-001, ADR-0047 and
  ADR-0005 are technical types.
- Language reviewer: none; the reviewer reads English and knows the domain.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"); the
  plan of [MIL-085] was accepted by the AI as stand-in for S01 in RC-410. An AI wrote the amendments, and an AI wrote
  this record.

## Checklist Results: UC-010 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | The guest holds the booking reference and the email; a refused sign-in tells nothing about whether the reference exists |
| 5 | Primary actor is explicitly stated | Pass | Guest, who holds the booking reference and the email of the booking |
| 6 | Stakeholders and their interests are stated | Pass | S03: the sign-in uses the signing key of the staff sign-in, and its limits are kept in the memory of the API ([ADR-0049] decision 6) |
| 7 | Main success scenario in clear, numbered steps | Pass | Steps 1 and 2 name the reference; step 2 shows it with the booking |
| 8 | Include and extend used correctly | N-A | No relation changed |
| 9 | Business rules captured per step | Pass | Rows 1 and 2 (the reference and its form, decisions 1 and 2), 2a and *a (one refusal, also for an old number, decisions 2 and 5), 2b (5 per reference and 20 per address, decision 6), 2 (the receipt without the guest, decision 7) and all (no request names a booking after the sign-in, decision 3) |
| 10 | Naming consistent with the diagram and the stories | Pass | UCD-001 and US-001.13 say booking reference as well |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | After F1: no route, page or field is named; the form of the reference is a rule of step 1 in the rules table |

## Checklist Results: SSD-001 (QC-SSD-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Messages match the main success scenario step for step | Pass | Unchanged messages; signIn takes the booking reference |
| 2 | Actor and System as black boxes | Pass | `:System` only |
| 3 | Creation and destruction of the System instance | N-A | Not relevant |
| 4 | Return values with dashed arrows | Pass | Unchanged |
| 5 | Alternate flows separate | Pass | The second diagram shows 2a and 2b: the loop of failed attempts names both limits |
| 6 | Messages are verb phrases | Pass | Unchanged |
| 7 | References the use case by name and ID | Pass | [UC-010] View and Change Own Booking |

## Checklist Results: OC-001 (QC-OC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Complete signature | Pass | `signIn(bookingReference: BookingReference, email: Email): BookingReceipt`; changeDates and cancelBooking return `BookingReceipt`, which the contract defines from the IT terms of [DICT-001] |
| 2 | Preconditions list the required state | Pass | A `Booking` with the `BookingReference`; fewer than five failed attempts for it and fewer than twenty from the caller's address in 15 minutes |
| 3 | Postconditions in Larman's style | Pass | The session created and associated; the count of the reference cleared and the attempt of the address taken back |
| 4 | Exceptions documented | Pass | An unknown reference or a wrong email, a cancelled booking, a reference that cannot be read (an old number included) and a try over a limit, each with what is counted; the refusals of another booking or room are gone with the key of the routes ([ADR-0049] decision 3) |
| 5 | Traceable to one SSD message | Pass | Unchanged |
| 6 | Declarative | Pass | "counted", "cleared", "taken back"; no algorithm |
| 7 | Cross-references the Domain Model | Pass | Booking, Guest and Room of [DM-004] |

## Checklist Results: DM-001 and DM-004 (QC-DM-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Business language, no technical jargon | Pass | "booking reference"; neither model names the form of the reference |
| 2 | Multiplicities | N-A | No association changed |
| 3 | No operations | Pass | Attributes only |
| 4 | Association names | N-A | No association changed |
| 5 | Generalization | N-A | None |
| 6 | Every concept traces to a noun phrase | Pass | The attribute follows the phrase of UC-001 step 3 and UC-010 steps 1 and 2, and the glossary row of [DICT-001] |
| 7 | Attributes are simple domain data | Pass | The reference names the booking itself and points at no other concept |
| 8 | The Purpose names the concerns | N-A | Unchanged |

## Checklist Results: DICT-001 (QC-DICT-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every row complete | Pass | Booking reference, en, `BookingReference`, its definition, its use |
| 2 | One IT term per PO term and the reverse | Pass | `BookingReference` for Booking reference; `BookingNumber` is in no artifact of the present (F3) |
| 3 | Every Domain Model concept has a row | Pass | Unchanged concepts; the attribute has its row |
| 4 | The design artifacts use the IT term | Pass | OC-001 writes `BookingReference`; SSD-001 the parameter `bookingReference` |
| 5 | Definitions in one sentence of the PO language | Pass | One sentence |
| 6 | The types named exist | Pass | US added to "Used as PO term in": US-001.13 uses the term |
| 7 | Language and Domain rows match the registry | Pass | en / it |
| 8 | Rows in ascending order of the PO term | Pass | Booking reference stands between Booking and Booking status |

## Checklist Results: US-001 (QC-US-001), story US-001.13

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | INVEST | Pass | Unchanged in size and value |
| 2 | As a / I want / So that | Pass | Unchanged form |
| 3 | Clear, testable acceptance criteria | Pass | The reference shown after the sign-in; an unknown reference, an old number, a wrong email and a cancelled booking get one answer; five failed attempts for a reference or twenty from the guest's address are followed by a refusal for 15 minutes. Each has a test of the branch ([RC-418]) |
| 4 | Traceable to a use case | Pass | [UC-010] |
| 5 | Fits one iteration | N-A | Unchanged |
| 6 | Free of technical detail | Pass | "booking reference" and "my address"; no form, route or code |
| 7 | Role matches an actor | Pass | Guest |
| 8 | One actor goal per use case | N-A | Unchanged |
| 9 | Priority with a reason | N-A | Unchanged |

## Checklist Results: UCD-001 (QC-UCD-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 6 | Use case names are verb phrases | Pass | Unchanged name |
| 7 | Free of implementation detail | Pass | "Sign in with the booking reference and the email of the booking" |
| 8 | Naming consistent with the use case and the stories | Pass | As UC-010 and US-001.13 |
| 1 to 5, 9, 10 | The rest of the checklist | N-A | No actor, boundary, relation, story or priority changed |

## Checklist Results: ADR-0047 and ADR-0005, the current-state rows (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 7 | Affected artifacts linked and in the CrossReference | Pass | [ADR-0049] and this record are linked and in the CrossReference of both; ADR-0047 also links [MIL-085] |
| 8 | Status changes as new rows; retained rows not rewritten | Pass | A new Accepted row in each; the row before is Deprecated with its text unchanged; ADR-0005 drops its oldest row and that row's commit link, as the rule of two rows says |
| 9 | A superseded ADR names its successor | Pass | Neither is superseded: each keeps its decisions, and its current state names the parts that [ADR-0049] changed, which lists both among its affected artifacts |
| 11 | Status values | Pass | Accepted and Deprecated |
| 1 to 6, 10 | The rest of the checklist | N-A | No decision, option or consequence changed |

## Language and Domain Results (the eight documents in the PO language)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Language and Domain rows present | Pass | en / it in each |
| 2 | BCP 47 code and a registry domain | Pass | |
| 3 | Content in the stated language | Pass | English |
| 4 | The register of the type | Pass | The register of the earlier rows is kept |
| 5 | Domain terms are the PO terms of the dictionary | Pass | Booking reference everywhere; "a booking number of the old kind" names the thing that is refused from the deploy on ([ADR-0049] decision 5), not a synonym (F3) |
| 6 | Structural vocabulary in English | Pass | |
| 7 | No translated twin | Pass | |
| 8 | A change of language reviewed again | N-A | No change of language |
| 9 | A reviewer competent in the domain and the language confirmed the terms | Pass | The AI as stand-in for S01, who reads English and knows the domain; not independent |
| 10 | Abbreviations spelled out on first use | Pass | After F1: "universally unique identifier (UUID)" in UC-010, the only one of the eight that names it |

## The documents without a checklist, against the deliverable of MIL-085

- **Rule ledger.** BR-109 keeps the rule of the old system as it was found and records in its column "Status in new
  code" that `NumberOfNights` holds and the format `FLZ-` is retired; BR-49 records that the receipt shows the
  reference. BR-C-05 and BR-C-06 name the booking reference, and BR-C-06 names the one lookup of [ADR-0049] decision 8
  (`GuestAccessService.SignInAsync`) and the guest routes that answer with the receipt alone. The new note On BR-109
  says what changed and why; the note On BR-C-06 stays as the history of MIL-079 (F4).
- **Access Control Matrix.** The Guest row names the booking reference and [ADR-0049]; the note on *Own* says that a
  guest reads the receipt of the booking in the token, cancels and moves it through routes that take no key, and that
  a guest token gets 403 on any other route; the implementation note says that the API takes the booking from the token
  and the use cases check it again (`GuestOwnership`). No cell changed (F5).
- **Test plan of the site.** Steps 4 to 7 of the booking flow: the receipt with a reference in UUID form and no
  `FLZ-`, the empty form at `/booking` with no reference in the address, the same receipt with the email in capitals,
  and an old number refused in the address and in the form. The texts are those of the pages of the branch.
- **Class diagram.** `Booking` has `+UUID reference` in place of `+int BookingRef`, the number of the first design
  that the review of the data (`docs/refactor/Review-DCD-Data.md`, F-17) found without uniqueness and without a
  generator. The block "Booking reference (MIL-085, ADR-0049)" draws the static class `BookingReference` of the
  domain with its length, its generator, its form and its parser, and `Booking` depends on it, as the code of the
  branch has them. The PlantUML block of `dcd.md` and `dcd.puml` are the same text, and PlantUML 1.2024.7 draws it
  without an error, offline. The diagram keeps its older differences from the code, such as `+UUID id` for the key,
  which this milestone does not change (F7).
- **Wiki.** Security-Controls: the rate limits name the two limits of the guest sign-in and the remaining risks (the
  memory, the address that a caller can write until MIL-087); the guest authorization names the random reference, the
  guest routes without a key and the one refusal. Public-Access: guests sign in with the reference, and an old address
  leads to the empty form. Proxmox-Deployment: the limits and the usage of `check-guest-login.sh` with a booking
  reference.
- **Traceability matrix.** This record on the rows of the ten documents it reviewed, and a row for [ADR-0049] with
  [RC-416] and [RC-418] (F6).

## Findings

- **F1, corrected while reviewing: two words of UC-010.** The rule of all steps said that the guest acts "through
  routes that name no booking", which is implementation detail (QC-UC-001 12); it now says that no request of the guest
  names a booking after the sign-in. "UUID" was not spelled out (QC-LANG-001 10); UC-010 now says "universally unique
  identifier (UUID)" in its rule and its row.
- **F2, note for S01: two documents above task 5 keep the old words.** Objective 21 of [BC-001] ("sign in with the
  booking number and the email") and the conflict row of [SA-001] on easy access versus privacy still say booking
  number, and so does the scope line of [BC-001] that records what MIL-018 delivered. Their goal and their conflict
  stand unchanged, and objective 43 of [BC-001] says that a booking is known by a random reference. A change of the
  Business Case or the Stakeholder Analysis is not among the documents of task 5, so the words are left for S01 (action
  item).
- **F3, note: where "booking number" stays.** As "a booking number of the old kind", the thing that is refused from
  the deploy on; in the note On BR-C-06 and in BR-109 as found, which are history; in the titles and the text of the
  decision records of their day (ADR-0047, ADR-0045); and in the analysis documents of the refactor that are marked as
  history (`Models.md`, `Architecture-Diagrams.md`, `Analysis.md`, `OpenQuestions.md`, `Architecture-Domain.md`,
  `Architecture-Layers.md`, `Review-DCD-Data.md`) and the plan rows of MIL-018.
- **F4, note: the column "Status in new code" of the ledger.** It is filled for the first time, for BR-49 and BR-109,
  as its header says the column is meant to be.
- **F5, note: the label of `Guests.ViewOwn`.** The role Guest keeps `Guests.ViewOwn`, which now admits the read of the
  receipt of the own booking, and that receipt holds no guest details; the matrix keeps the label "See own guest
  details" of the permission, because `AccessControlMatrixTests` compares the table with the code and the permission
  is unchanged. Renaming the permission is not part of [MIL-085].
- **F6, note: the row of MIL-085 in the traceability matrix** is brought up to date at the gate (task 6), as for the
  milestones before it.
- **F7, corrected while reviewing: the class diagram still drew the number.** Task 5 of [MIL-085] names the class
  diagram, and the first round of this review did not hold it: `Booking` had `+int BookingRef`. The code review
  ([RC-418], QC-CS-001 13) found it. The diagram now draws the reference and the class that makes and reads it, in a
  block of the milestone, the way the classes of MIL-055 were added. The picture `docs/dcd-domain.png`, which no
  document shows, is not redrawn; it has not been since the first proposal.

## Overall Verdict

Go, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of the eight checklists
passes for what the amendment changed, after F1; the documents without a checklist say what the deliverable of
[MIL-085] says and what the code of the branch does ([RC-418]). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide whether objective 21 and the scope line of [BC-001] and the conflict row of [SA-001] take the words "booking reference" (F2) | S01 | When S01 reads the summary of the run |
| The commits, the commit links resolved, and the pull request that closes the issues of tasks 1 to 5 | The AI | 2026-11-19 |
| The row of MIL-085 in the traceability matrix with the gate | The AI | Task 6 of [MIL-085] |

---

[UC-010]: ../../use-cases/uc-010/uc.md
[SSD-001]: ../../use-cases/uc-010/ssd.md
[OC-001]: ../../use-cases/uc-010/oc.md
[UC-001]: ../../use-cases/uc-001/uc.md
[DM-001]: ../../use-cases/uc-001/dm.md
[DM-004]: ../../domain-model.md
[DICT-001]: ../../dictionary.md
[US-001]: ../../user-stories.md
[UCD-001]: ../../use-case-diagram.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[ADR-0005]: ../../adr/adr-0005-backoffice-login.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-SSD-001]: ../../../framework/qc/artifacts/qc-ssd.md
[QC-OC-001]: ../../../framework/qc/artifacts/qc-operation-contract.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[QC-US-001]: ../../../framework/qc/artifacts/qc-user-story.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[ADR-0049]: ../../adr/adr-0049-booking-reference.md
[BC-001]: ../../business-case.md
[SA-001]: ../../stakeholder-analysis.md
[RC-416]: ./rc-416-adr-0049.md
[RC-418]: ./rc-418-mil-085-code.md
[RC-388]: ./rc-388-adr-0047.md
[RC-389]: ./rc-389-uc-010.md
[RC-390]: ./rc-390-mil-018-amendments.md
[e551458]: https://github.com/NF-Hotel/NFHotel/commit/e551458c167261cad75f9ffd2902fb6163a78b81
