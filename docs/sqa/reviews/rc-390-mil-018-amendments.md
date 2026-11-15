# RC-390 Review of the amendments of MIL-018 to SA-001, US-001, UCD-001, UC-004, DM-004 and DICT-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-390 |
| CrossReference | [SA-001], [US-001], [UCD-001], [UC-004], [DM-004], [DICT-001], [QC-SA-001], [QC-US-001], [QC-UCD-001], [QC-UC-001], [QC-DM-001], [QC-DICT-001], [QC-LANG-001], [MIL-018], [UC-010], [SSD-001], [ADR-0047], [RC-154], [RC-263], [RC-264], [RC-324], [RC-368], [RC-389], [RC-391] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the six documents that MIL-018 amends; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-15 on the branch `mil-018-guest-login` (on `main` at 6dd1817) before
  its pull request:
  - [SA-001]: the stakeholder S06, the hotel guests, in every table, the rationale and the conflicts; the concerns as
    a register with the IDs CN-001 to CN-007; the commit links that no row used removed.
  - [US-001]: the story US-001.13; the Version History cut to its two latest rows, with the blank line that split the
    table removed; [MIL-018] and [SA-001] in the CrossReference.
  - [UCD-001]: the actor Guest and the use case View and Change Own Booking in the boundary text, the diagram, the
    actor table, the use case table and the role matrix.
  - [UC-004]: its open issue on the Guest role closed by [UC-010].
  - [DM-004]: [UC-010] and [SSD-001] cited; no concept added or changed.
  - [DICT-001]: the row Guest sign-in (`GuestSession`); the rows sorted by the PO term.
- Checklists used: [QC-SA-001], [QC-US-001], [QC-UCD-001], [QC-UC-001] (Fully Dressed), [QC-DM-001],
  [QC-DICT-001], and [QC-LANG-001] for all six, which are written in the PO language.
- Scope: delta review, against every criterion of each checklist for what the amendment changed. The rest of each
  document stands as its latest record left it ([RC-368] for UCD-001, [RC-324] for US-001, [RC-154] for UC-004,
  [RC-264] for DM-004 and DICT-001).
  Two criteria that the earlier records did not meet are met now and are reviewed for the whole document: criterion 9
  of QC-SA-001 (the concern register) and criterion 8 of QC-DICT-001 (the order of the rows, the action item of
  [RC-264]).
- Not under a checklist: the Access Control Matrix (`docs/refactor/Access-Control-Matrix.md`) is a working document
  without an ID. Its new table is checked against the code by `AccessControlMatrixTests`, which reads the file and
  fails on any cell that differs from `RolePermissions`; the test passes on the branch ([RC-391]).
- Language and domain: en / it (the Metadata rows of the six documents)
- Language reviewer: none; the reviewer reads the language and knows the domain.
- Mode: autonomous, not independent, as in [RC-389]. An AI wrote the amendments, and an AI wrote this record.
- Render: the diagram of UCD-001 was rendered on 2026-11-15 with `plantuml/plantuml:1.2024.7` in Docker under WSL,
  without a network: exit 0, no syntax error, an SVG of 20,394 bytes with the actor Guest and the new use case.

## Checklist Results: SA-001 (QC-SA-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Power/Interest grid is filled for every stakeholder, with no gaps or unclassified entries | Pass | S06: LOW power, HIGH interest, Keep Informed |
| 2 | Each stakeholder is assigned a unique, stable ID (e.g. S01-S11 style) reusable for RACI assignments in other artifacts | Pass | S06 is the next free ID; S01 to S05 are unchanged |
| 3 | Roles and organizational context are defined with explicit Power and Interest levels, not just narrative description | Pass | Role, organization (NF Hotel) and levels in the summary row, and a rationale bullet |
| 4 | Communication needs (channel, frequency, deliverable type) are mapped to project phases or milestones | Pass | Through S01 and S04, on change, the guest pages and the sign-in rules of UC-010, MIL-018 |
| 5 | Conflicting stakeholder interests are identified with documented mitigation or resolution strategies | Pass | Easy access to the own booking against the privacy of every other guest (S06, S01), mitigated by the rules of [ADR-0047] |
| 6 | Stakeholder concerns are explicitly traced to Business Case objectives | Pass | CN-007 to objective 21 |
| 7 | Primary concerns are expressed in both business language and a recognized quality-attribute mapping (e.g. FURPS+) | Pass | CN-007: usability, functionality (security) |
| 8 | Document is understandable and navigable by non-technical stakeholders reviewing their own entry | Pass | The guest's row says what the guests want in plain words |
| 9 | Every concern has a stable ID (`CN-NNN`) in the concern register, with the stakeholder who raised it; IDs are never renumbered or reused | Pass | Met now for the whole document: CN-001 to CN-007, each with its stakeholder, in the order of the earlier table; no other document used a concern ID before (F2) |

## Checklist Results: US-001 (QC-US-001), story US-001.13

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Follows INVEST criteria (Independent, Negotiable, Valuable, Estimable, Small, Testable) | Pass | Independent of the other stories' order; negotiable (no page named); valuable to the guest; estimable from MIL-018; Small is an exception named in the story's table, as for US-001.12 ([RC-263]); testable |
| 2 | Written in "As a / I want / So that" form | Pass | |
| 3 | Clear, testable acceptance criteria are included | Pass | Seven Given/When/Then criteria, each a check of the code's tests or of the live proof of MIL-018 criterion 5 |
| 4 | Traceable to a use case or epic | Pass | UCD-001 "View and Change Own Booking", UC-010 |
| 5 | Story is sized to fit within a single iteration | Fail (Optional) | Larger than one iteration; it splits into the tasks of MIL-018, as the table says |
| 6 | Story statement avoids technical implementation detail | Pass | Number, email, dates and cancel; no token, cookie or page |
| 7 | Role named in the story matches an actor defined in the Use Case Diagram | Pass | Guest, the new actor of [UCD-001] |
| 8 | A story with more than one actor goal is split into use cases, one per actor goal, and each use case names the story it comes from | Pass | One goal, one use case; the use case table of UCD-001 names US-001.13 |
| 9 | The story has a priority (MoSCoW: Must, Should, Could, Won't now) with the reason beside it | Pass | Should, objective 21 |

## Checklist Results: UCD-001 (QC-UCD-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Actors are defined with correct UML stereotypes (e.g. `<<System>>`, `<<Actor>>`) | Pass | `actor "Guest" as Guest <<Actor>>` |
| 2 | System boundary is clearly drawn and labeled | Pass | The new use case is inside "NFHotel platform"; the boundary text says that a guest's own booking is inside and making a booking on the website is outside |
| 3 | Include/extend relationships are used correctly per UML 2.5.1, not as generic "uses" arrows | Pass | None added: the guest's sign-in is a part of the use case, not the staff sign-in of Sign In To The Back Office |
| 4 | Every actor participates in at least one use case (no orphan actors) | Pass | Guest in View and Change Own Booking |
| 5 | Diagram is traceable to a documented stakeholder need | Pass | S06 and CN-007 of [SA-001], US-001.13 |
| 6 | Use case names are verb phrases describing actor goals, not internal system operations | Pass | View and Change Own Booking |
| 7 | Diagram is free of implementation detail (e.g. UI widgets, database tables) | Pass | Actors and goals only |
| 8 | Actor and use case naming is consistent with corresponding Use Case and User Story documents | Pass | The names of [UC-010] and of US-001.13 |
| 9 | Each use case lists the user story it comes from and its priority (MoSCoW) with a reason | Pass | US-001.13, Should, objective 21 |
| 10 | When there is more than one primary actor, a role matrix (use case by role) is present and marked as a suggestion for the Product Owner to approve | Pass | Four actors, a Guest column, still marked as a suggestion |

## Checklist Results: UC-004 (QC-UC-001), the closed open issue

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1, 2 | Brief and Casual formats | N-A | Fully Dressed |
| 3 to 12 | The other criteria | Pass | Unchanged: only the open issue on the Guest role is marked closed by UC-010, in the form the other closed issues use. The steps, the rules and the actor are as [RC-154] left them, and the actor Hotel staff and the title still match [UCD-001] |

## Checklist Results: DM-004 (QC-DM-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 to 8 | All criteria | Pass | No concept, attribute or association changed: a guest signs in with the booking number of the Booking and the email of its Guest, both already in the model. The amendment cites [UC-010] and [SSD-001] and cuts the Version History to two rows. The Guest sign-in is not a concept of the model (it is not stored), which the dictionary row and OC-001 say |

## Checklist Results: DICT-001 (QC-DICT-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every row has a PO term, its language, an IT term and a definition | Pass | Guest sign-in, en, GuestSession, one sentence |
| 2 | Each PO term maps to exactly one IT term and the reverse (no synonyms) | Pass | `GuestSession` is used for no other term; the code's `GuestSessionDto` carries it |
| 3 | Every Domain Model concept has a row, and the Domain Model uses its PO term | Pass | The ten concepts of [DM-004] (Audit event, Booking, Guest, Price change, Price suggestion, Pricing settings, Role, Room, Room rate, Staff member) each have a row |
| 4 | The Operation Contracts, Sequence Diagrams, Design Class Diagrams and ERD use the IT term, not the PO term | Pass | OC-001 uses `Booking`, `BookingNumber`, `Email`, `BookingStatus`, `StartDate`, `EndDate`, `GuestSession` |
| 5 | Definitions are written in the PO language and are one sentence | Pass | |
| 6 | "Used as PO term in" and "Used as IT term in" name artifact types that exist in the project | Pass | UC and OC, both exist since MIL-018 |
| 7 | The dictionary's `Language` and `Domain` rows, and the language of every row, match the PO language and domain in the project registry | Pass | en and it |
| 8 | The dictionary rows are in ascending order of the PO term (case-insensitive, by the alphabet of the PO language) | Pass | Met now for the whole table: the 38 rows sorted by the PO term, which closes the action item of [RC-264] (F3) |

## Language and Domain Results (the six documents)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | en and it in all six |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Professional English for the analysis documents |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Guest, booking, booking number, arrival and departure date; Guest sign-in is the new row |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The AI as stand-in for S01; not independent |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | No new abbreviation |

## Findings

- **F1, note: S06 is spoken for by S01 and S04.** No real guest has been asked; the stakeholder analysis says so, as
  it does for S05. The concern CN-007 and the story US-001.13 are the AI's reading of objective 21 and of MIL-018,
  accepted by the AI as stand-in for S01.
- **F2, fixed: the concern register.** SA-001 had its concerns without IDs, which criterion 9 of QC-SA-001 asks for
  since the framework added it. The table is now the concern register, CN-001 to CN-007 in the order of the earlier
  rows, and the business-goal table names them. No other document cited a concern ID before, so nothing is
  renumbered.
- **F3, fixed: the order of the dictionary.** [RC-264] gave Go-with-conditions because the rows were not in the
  order of the PO term. The rows are sorted now, with no row changed.
- **F4, note: what the stories' purpose text says.** The purpose of [US-001] still speaks of five stories from the
  owner and DevOps; it has thirteen since MIL-018. The text was not part of earlier changes either, and is left for
  the action item of [RC-368] on the user stories.
- **F5, note: the Access Control Matrix changed a cell for staff.** "See own passport data" is refused for every role
  now, Receptionist and Manager included: staff are not guests and have no own passport data, and they read a
  guest's passport number through the next row. The code holds the same table, and the test compares them.

## Overall Verdict

Go. Every Mandatory criterion of QC-SA-001, QC-US-001, QC-UCD-001, QC-UC-001, QC-DM-001, QC-DICT-001 and QC-LANG-001
passes for the amendments, after the fixes of F2 and F3, and the rest of each document stands as its latest record
left it. S01 asked in chat on 2026-11-15 that MIL-018 be finished in autonomous mode, and the AI accepted the six
amendments and this record as stand-in for S01: the row of 2026-11-15 of each is Accepted and the row before it
Deprecated. The reviewer is not independent: an AI wrote the amendments and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the six amendments, or return them | S01; done on 2026-11-15 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-018-guest-login` |
| Ask a real guest, or decide that S01 and S04 keep speaking for S06 (F1) | S01 | 2026-12-31 |

---

[SA-001]: ../../stakeholder-analysis.md
[US-001]: ../../user-stories.md
[UCD-001]: ../../use-case-diagram.md
[UC-004]: ../../use-cases/uc-004/uc.md
[DM-004]: ../../domain-model.md
[DICT-001]: ../../dictionary.md
[QC-SA-001]: ../../../framework/qc/artifacts/qc-stakeholder-analysis.md
[QC-US-001]: ../../../framework/qc/artifacts/qc-user-story.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-018]: ../../milestones/mil-018-guest-login.md
[UC-010]: ../../use-cases/uc-010/uc.md
[SSD-001]: ../../use-cases/uc-010/ssd.md
[ADR-0047]: ../../adr/adr-0047-guest-login.md
[RC-154]: ./rc-154-use-case-006.md
[RC-263]: ./rc-263-mil-055-task-1-objective-and-story.md
[RC-264]: ./rc-264-mil-055-task-2-terms-and-model.md
[RC-324]: ./rc-324-mil-071-task-1-objective-29-and-us-001-12.md
[RC-368]: ./rc-368-ucd-001-benchmark-actors.md
[RC-389]: ./rc-389-uc-010.md
[RC-391]: ./rc-391-mil-018-code.md
