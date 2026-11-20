# RC-435 Review of the documents of MIL-088: a booking verified by email

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-435 |
| CrossReference | [UC-011], [SSD-002], [OC-002], [SD-001], [UCD-001], [US-001], [DM-004], [DICT-001], [UC-001], [UC-010], [SSD-001], [OC-001], [ADR-0052], [ADR-0053], [ADR-0051], [QC-UC-001], [QC-SSD-001], [QC-OC-001], [QC-SD-001], [QC-UCD-001], [QC-US-001], [QC-DM-001], [QC-DICT-001], [QC-ADR-001], [QC-WIKI-001], [QC-LANG-001], [MIL-088], [BC-001], [SA-001], [RC-413], [RC-417], [RC-427], [RC-433], [RC-434], [RC-436] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version: review of the documents of MIL-088 (task 3, and the ledger and the wiki pages of task 8) and of the second versions of ADR-0052 and ADR-0053; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-20 on the branch `mil-088-booking-verified-by-email` (from `main` at
  3b35a3d, the code at 733a5b2), before its pull request:
  - New, task 3 of [MIL-088]:
    - [UC-011] Book a Room and Verify It by Email, Fully Dressed: nine steps, the extensions 2a to 9a, \*a and \*b, a
      business rule for every step and extension that has one, and three open issues.
    - [SSD-002]: five diagrams (the main success scenario, the mail asked for again, the refused verification, the
      request that nobody verifies, and the booking at once while the verification is off) and six system operations.
    - [OC-002]: the contracts CO1 to CO6 of the six operations.
    - [SD-001]: the design of verifyBooking (CO4) in three diagrams, the first sequence diagram of the project.
  - Amended, task 3:
    - [UCD-001]: the use case Book a Room and Verify It by Email of the Guest in the diagram, the tables and the matrix
      of roles; the diagram laid out again with three crossings.
    - [US-001]: the story US-001.14 and the notes on INVEST.
    - [DM-004]: the concept Booking request with its association to Booking, and the attribute made on the public site
      of Booking.
    - [DICT-001]: the terms Asked at, Booking request, Mailbox, Number of guests and Verify, and the rows Guest sign-in
      and Made on the public site.
    - [UC-001]: business rule 1. [UC-010]: extension 1a and its rule. [SSD-001]: the scope and the lifecycle of
      extension 1a. [OC-001]: [SD-001] in its CrossReference.
  - The second versions of [ADR-0052] (decision 5: the third lock `public_booking`, and whatever fails in the second
    mail ends as the event of a mail not sent; the consequence of a commit whose answer is lost; [UC-011] and its
    documents under Affected Artifacts) and of [ADR-0053] (decision 1: one bound of 15 seconds for a send).
- Checklists used: [QC-UC-001] (Fully Dressed) for UC-011, UC-001 and UC-010; [QC-SSD-001] for SSD-002 and SSD-001;
  [QC-OC-001] for OC-002 and OC-001; [QC-SD-001] for SD-001; [QC-UCD-001] for UCD-001; [QC-US-001] for US-001.14;
  [QC-DM-001] for DM-004; [QC-DICT-001] for DICT-001; [QC-ADR-001] for the two ADRs; [QC-WIKI-001] for the wiki pages
  of task 8; and [QC-LANG-001] for the nine documents written in the PO language.
- Scope: full review of the four new documents against every criterion of their checklists. Delta review of the
  amended documents for what they changed; the rest of each stands as its latest record left it: [RC-427] for UC-001,
  UC-010 and DICT-001, [RC-417] for UCD-001, US-001, DM-004, SSD-001 and OC-001, and [RC-433] and [RC-434] for the
  first versions of the two ADRs.
- Not under a checklist, read against the deliverable of [MIL-088], the two ADRs and the code of the branch: the rule
  ledger `docs/refactor/BusinessRules.md` (the rows of the limits that task 8 adds or changes), the class diagram
  `docs/dcd.md` with its copy `docs/dcd.puml`, the picture of the network of the wiki, the artifact registry and the
  traceability matrix. The evidence of the code belongs to the code review [RC-436].
- Each PlantUML block of the new and changed documents, 13 diagrams, and the picture of the network were rendered with
  the image `plantuml/plantuml:1.2024.7` without a network; none failed. `framework/scripts/check-languages.sh` exits 0.
- Language and domain: en / it (the Metadata rows of UC-011, SSD-002, UCD-001, US-001, DM-004, DICT-001, UC-001,
  UC-010 and SSD-001); OC-002, SD-001, OC-001 and the two ADRs are technical types in IT Professional English.
- Language reviewer: none; the reviewer reads English and knows the domain.
- Mode: autonomous, not independent. S01 chose autonomous mode in chat on 2026-11-19 ("Når din plan er lavet skal
  Opus 5.5 lave det hele i automode") and handed the decisions to the AI ("Du skal tage beslutningerne for mig"); the
  plan of [MIL-088] was accepted by the AI as stand-in for S01 in [RC-413]. An AI agent of the build session wrote the
  documents of task 3 from the code of the branch, the milestone and the ADRs; the AI of the build session wrote the
  second versions of the ADRs, the ledger and the wiki pages, reviewed them all and wrote this record.

## Checklist Results: UC-011 (QC-UC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed |
| 2 | An informal narrative | N-A | Fully Dressed |
| 3 | All standard sections are present | Pass | Scope, level, primary actor, stakeholders and interests, preconditions, success guarantee, main success scenario, extensions, business rules per step, open issues |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Preconditions: the guest reads the mail of the email given, and the verification is on (\*a covers it off). Success guarantee: the booking pending with its reference in a free room of the size, stored with the guest, the guest signed in, the request gone. Whatever happens: a request holds no room and is deleted within ten minutes after its link lapsed |
| 5 | Primary actor is explicitly stated | Pass | Guest (S06), as in UCD-001 and US-001.14 |
| 6 | Stakeholders and their interests are stated | Pass | S06, S01, S05, S03 and S04, each with the interest that the flow serves; the interest of S01 cites objective 43 of [BC-001] |
| 7 | Main success scenario in clear, numbered steps | Pass | Nine steps, guest and system in turn: ask, the mail, open, verify, booked and signed in |
| 8 | Include and extend used correctly | Pass | None: step 9 hands the guest to the own booking of [UC-010], whose extension 1a takes it up, and UCD-001 says why the two goals in a row are no `<<include>>` or `<<extend>>` |
| 9 | Business rules captured per step | Fail, then Pass | A row for every step and extension that has a rule, each with its decision or the table of the ledger: the rules of the stay (BR-C-07, BR-N-08), the limits per address and per mailbox, the normal form of a mailbox, the mail, the link, the claim and the lock, the fuse, the setting. D1: extension 9a and its rule named only the relay and the fuse |
| 10 | Naming consistent with the diagram and the stories | Pass | Book a Room and Verify It by Email, Guest, booking request, booking reference and verify, as in UCD-001, US-001.14 and DICT-001 |
| 11 | Scope and level stated | Pass | NFHotel platform, the website of the hotel; user-goal |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | No component, route, code or status: the link, the mail, the mail relay and the website are what the guest and the hotel see; the rows name the decisions that hold the details |

## Checklist Results: SSD-002 (QC-SSD-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Input and output messages match the main success scenario step for step | Pass | findFreeRoomSizes (steps 1 and 2), askForBooking (3 and 4), openLink (5 and 6), verifyBooking (7 to 9); the table of the operations maps each message to its steps, and the two mails are the outputs of steps 4 and 9 |
| 2 | Actor and System are black boxes | Pass | `:System` only; the lifecycle notes say what it holds (the website, the API and the mails through the relay) without drawing it |
| 3 | Creation and destruction of the System instance where relevant | N-A | The system lives across interactions; the lifecycle notes say what each message starts and ends: the booking request and the guest's sign-in |
| 4 | Return values with dashed return arrows | Pass | Every message of the guest has its dashed return; removeLapsedRequests, which time starts, returns nothing to an actor |
| 5 | Alternate and exceptional flows drawn apart or named out of scope | Pass | Four diagrams beside the main one: 5a with 4e, 7a to 7c, 5b, and \*a; the other extensions are listed by message as exceptions of [OC-002], and 9a is named |
| 6 | Message names are verb phrases | Pass | findFreeRoomSizes, askForBooking, openLink, verifyBooking, removeLapsedRequests and bookAtOnce |
| 7 | The diagram names its use case | Pass | Source Use Case: [UC-011] Book a Room and Verify It by Email |

## Checklist Results: OC-002 (QC-OC-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Complete signature | Pass | Each of CO1 to CO6 has its name, typed parameters and return type, as the messages of [SSD-002]; removeLapsedRequests returns the numbers of requests and records deleted |
| 2 | Preconditions list the state before, in Domain Model concepts | Pass | `BookingRequest`, `Room`, `Booking` and `Guest`; the terms that are no concepts of [DM-004] (`GuestFields`, `Mailbox`, the token, `SentMail`, the receipts) are defined before the contracts, each with why it exists only in the software |
| 3 | Postconditions in Larman's style | Pass | "was created", "became", "was associated with", "was renewed", "was deleted"; CO1 and CO3 say that no instance changed |
| 4 | Exceptions and their failing preconditions | Fail, then Pass | Each exception names the precondition that fails, its code and what stays. D1: CO4 had no exception for a second mail that fails other than by the relay or the fuse |
| 5 | One contract per SSD message | Pass | CO1 to CO6 for messages 1 to 6 of [SSD-002], each naming its message and its steps |
| 6 | Declarative, not procedural | Pass | State changes only; the order of the checks, the locks and the SQL are in [SD-001] and the ADRs. The note under CO2 on which refusals give the count of an address back is a rule of [ADR-0051], held against the code: only 400 and 422 give it back, and `booking.room_type_not_available` answers 409 |
| 7 | Cross-references to the Domain Model | Pass | Each contract names the concepts of [DM-004] it touches; CO3 says it touches none |

## Checklist Results: SD-001 (QC-SD-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | UML arrow syntax | Pass | Solid arrows for the calls, dashed for the returns, the statements to PostgreSQL included |
| 2 | Patterns annotated | Pass | The table Pattern Annotations: Controller, Pure Fabrication, Information Expert, Creator with Factory Method, Repository and Unit of Work, Command, Adapter with Protected Variations |
| 3 | Activation bars | Pass | Each call activates its receiver until its return; the nested planning and the work under the lock show the call stack |
| 4 | Creation and destruction | Pass | `create` with `<<create>>` for the claimed request, the guest and the booking; `destroy` of the booking and the guest when the exclusion constraint refuses the room, and of the record of a mail that the relay refused |
| 5 | Realizes the postconditions of one contract | Pass | CO4; the table Postcondition Coverage maps each postcondition to its messages |
| 6 | Responsibility assignment | Pass | The section Responsibility Check: the controller turns HTTP into one call, the service coordinates and decides nothing that another object knows, `BookingService` keeps every public booking, the repositories the queries, the unit of work the transaction and the locks |
| 7 | loop, alt and opt | Pass | alt for the claim, the refusal and the booking, the room refused by the constraint, the fuse and the relay; loop over at most five rooms; opt for the row that came back and the event of the limit |
| 8 | Exceptions of the contract shown or justified | Fail, then Pass | The verification switched off and the token over 64 characters are named as not drawn, with why; the other exceptions of CO4 are alt branches. D1 and D2: the second mail that fails in another way is a note at its reference |
| 9 | Purpose names the concerns and the stakeholders | Pass | CN-006 of S05, CN-007 of S06 and CN-005 of S04, as in [SA-001] |
| 10 | Lifelines by layer, calls across a boundary through the inner interface | Pass | Boxes Delivery, Application, Domain and Infrastructure; a lifeline that implements a port is named after the port first (`IMailSender` and `:SmtpMailSender`); each box matches the project of its class under `src/` |
| 11 | Layer colours | Pass | `#FADADD`, `#FFF3B0`, `#CDEFD3` and `#D4E4F7` |

## Checklist Results: UCD-001 (QC-UCD-001), delta

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Actors with UML stereotypes | Pass | `<<Actor>>` for the four actors; the colours only help to follow the lines |
| 2 | System boundary drawn and labelled | Pass | The new use case is inside "NFHotel platform"; the text of the scope says that the booking on the website is now inside |
| 3 | Include and extend | Pass | No new relationship; the section Relationships says why the new use case has none with View and Change Own Booking |
| 4 | No orphan actors | Pass | The Guest has two use cases |
| 5 | Traceable to a stakeholder need | Pass | S06 in the actor table; objective 43 and success criterion 48 of [BC-001] in the use case table |
| 6 | Use case names are actor goals | Pass | Book a Room and Verify It by Email |
| 7 | No implementation detail | Pass | |
| 8 | Naming consistent with the use case and the story | Pass | The title of UC-011 and the trace of US-001.14 |
| 9 | Each use case lists its story and its priority with a reason | Pass | US-001.14, Must, objective 43 and success criterion 48 |
| 10 | A role matrix when there is more than one primary actor | Pass | A new row: the Guest performs it, the other roles take no part; the matrix stays a suggestion for the Product Owner to approve |

The layout: three crossings, all of the arrow from Hotel staff to Run Time and Quality Benchmark, as the text under the
diagram says; the picture rendered with the image above shows them so.

## Checklist Results: US-001.14 (QC-US-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Follows INVEST | Pass | Independent of the stories of staff; negotiable (no page named, the numbers are settings); valuable to the guest and to the hotel; estimable from the tasks of [MIL-088]; Small is an exception named in the story's table, as for US-001.12 and US-001.13; testable by eight criteria |
| 2 | As a, I want, so that | Pass | |
| 3 | Testable acceptance criteria | Pass | Eight criteria in Given, When and Then, each with a number or a state that a test reads: 30 minutes, three mails in an hour and five in a day, ten minutes, 400 mails |
| 4 | Traceable to a use case | Pass | The use case "Book a Room and Verify It by Email" of UCD-001 (UC-011) |
| 5 | Sized to one iteration | Fail (Optional) | Larger than one iteration; it splits into tasks 4 to 7 of [MIL-088], as its table says |
| 6 | No technical detail in the statement | Pass | The website, the link and the mail |
| 7 | The role matches an actor of the diagram | Pass | A guest (S06), the actor Guest |
| 8 | One actor goal, or split | Pass | One goal of one actor |
| 9 | Priority with its reason | Pass | Must: objective 43 and success criterion 48 of [BC-001] |

## Checklist Results: DM-004 (QC-DM-001), delta

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Business language | Pass | Booking request, asked at, number of guests, made on the public site; no column, flag or hash |
| 2 | Multiplicities | Pass | Booking request 0..1 becomes Booking 0..1, the reason of each end in the association table |
| 3 | No operations | Pass | Attributes only |
| 4 | Association names | Pass | becomes, with its direction |
| 5 | Generalization | N-A | None added |
| 6 | Every concept traces to a noun phrase | Pass | Booking request: UC-011 steps 3 to 8, 5a and 5b; made on the public site: UC-011 steps 8 and \*a, and UC-001 rule 1 |
| 7 | Attributes are simple domain data | Pass | Dates, a room size, a number, the details of a guest and a time; the request points at no room and no guest, as it holds none |
| 8 | The Purpose names the concerns | N-A | The purpose names the booking on the website; its concerns are unchanged |

The record of a mail sent and the mailbox are no concepts of the model: the first exists only for the limits of the
software, and the second is a term of [DICT-001] that the contracts use. That is the writer's choice, and it holds.

## Checklist Results: DICT-001 (QC-DICT-001), delta

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every row complete | Pass | The five new rows have the PO term, en, the IT term, the definition and both columns of types |
| 2 | One IT term per PO term and the reverse | Pass | `CreatedAt`, `BookingRequest`, `Mailbox`, `Guests` and `Verify`, each once |
| 3 | Every Domain Model concept has a row | Pass | Booking request with its attributes asked at and number of guests; made on the public site has its row since [RC-427] |
| 4 | The design artifacts use the IT term | Pass | The class diagram writes `BookingRequest`, `createdAt`, `guests` and `Mailbox`; OC-002 and SD-001 write `BookingRequest`, `CreatedAt`, `Guests` and `Mailbox` |
| 5 | Definitions in one sentence of the PO language | Pass | Each new and amended definition is one sentence |
| 6 | The types named exist | Pass | DM, UC and US as PO term; OC, SD and DCD as IT term, SD since [SD-001] |
| 7 | Language and Domain rows match the registry | Pass | en / it |
| 8 | Rows in ascending order of the PO term | Pass | Asked at after Arrival date, Booking request between Booking reference and Booking status, Mailbox after Made on the public site, Number of guests before Number of nights, Verify last |

## Checklist Results: UC-001 and UC-010 (QC-UC-001), delta

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A single paragraph of the primary success scenario | N-A | Fully Dressed, both |
| 2 | An informal narrative | N-A | Fully Dressed, both |
| 3 | All standard sections are present | Pass | No section added or removed |
| 4 | Preconditions and postconditions are explicitly defined | Pass | Unchanged in both |
| 5 | Primary actor is explicitly stated | Pass | Hotel staff and Guest, unchanged |
| 6 | Stakeholders and their interests are stated | Pass | Unchanged |
| 7 | Main success scenario in clear, numbered steps | Pass | The steps are unchanged; UC-010 extension 1a says how the verification signs the guest in, and how the offer after the booking does while the verification is off |
| 8 | Include and extend used correctly | N-A | No relationship changed; extension 1a of UC-010 names [UC-011] as where the guest comes from |
| 9 | Business rules captured per step | Pass | UC-001 rule 1: the coming bookings of the public site per mailbox, staff book without a request and a mail, a request is not a booking and holds no room (ADR-0052 decisions 1, 6 and 9). UC-010 rule 1a: either sign-in lasts two hours (ADR-0047 decision 2; ADR-0052 decisions 5 and 8) |
| 10 | Naming consistent with the diagram and the stories | Pass | Booking request, mailbox and verify, as in UC-011 and DICT-001 |
| 11 | Scope and level stated | Pass | Unchanged |
| 12 | Written from the actor's goal, free of UI and implementation detail | Pass | No component is named |

## Checklist Results: SSD-001 (QC-SSD-001) and OC-001 (QC-OC-001), delta

- SSD-001: criterion 5 passes, because the scope note now says where extension 1a goes (verifyBooking of [SSD-002],
  or the offer after its bookAtOnce while the verification is off), and the lifecycle note says that verifyBooking
  starts the same sign-in without signIn; the diagrams and the other criteria are unchanged since [RC-417].
- OC-001: no contract changed. [SD-001] is in its CrossReference because it is the first instance of a type that the
  catalog lists for an operation contract, as the artifact rule asks; criterion 7 of QC-OC-001 still passes.

## Checklist Results: ADR-0052 and ADR-0053, second versions (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID matches the file name and is unique | Pass | Unchanged |
| 2 | Context: the problem, the forces and the options | Pass | Unchanged; both second versions decide details within the options already chosen |
| 3 | Two options at least, or why not | Pass | ADR-0052 decision 5 says what the lock replaced and why: without it, 20 verifications at once inserted into one room, and the exclusion constraint made PostgreSQL end some as a deadlock that the retries did not always get past. ADR-0053 decision 1 replaces the 30 seconds with the 15 and says why: the web application waits 30 seconds for the API |
| 4 | The decision unhedged | Pass | |
| 5 | Positive and negative consequences | Pass | ADR-0052 adds two negative points, the wait for the lock `public_booking` and a commit whose answer is lost on the network; ADR-0053's bound makes a slow relay a failed send instead of an error page after the booking |
| 6 | Quality attributes named | Pass | Reliability (no deadlock; the booking stands whatever the second mail does), performance efficiency (a few milliseconds of the lock), and the time behaviour of the answer (the bound) |
| 7 | Affected Artifacts as links, each in CrossReference | Fail, then Pass | D5: ADR-0052 named UC-011 and its documents as documents to come, without a link; they are links now and in the CrossReference |
| 8 | Status changes as new rows; retained rows unchanged | Pass | One new row each, Accepted after this review; the first row of each Deprecated and otherwise unchanged |
| 9 | A superseded record names its successor | N-A | Neither is superseded |
| 10 | Language of the domain, no unexplained abbreviation | Pass | |
| 11 | Status values | Pass | `Deprecated` and `Accepted` |

What the second versions rest on, read in the code of the branch: `BookingService.PublicBookingLock` and its use in
`BookPublicAsync` for the booking at once and for `BookVerifiedAsync`, with the test of criterion 4 of [MIL-088] (20
verifications at once for the last room, one booking) that found the deadlocks; the catch of `VerifyAsync` around the
second mail, held by three tests (F1 of [RC-436]); the bound of `SmtpMailSender` with its tests of a relay that never
answers and one that never answers the quit (F3 of [RC-436]); the remarks of `UnitOfWork.RunUnderLockAsync` on a lost
commit (F4 of [RC-436]).

## Checklist Results: the wiki pages of task 8 (QC-WIKI-001)

The pages: Mail-Relay (new), Alerts, Containers-and-Images, Environment-Files, Home, Local-Development-Deploy,
Network-Isolation, Proxmox-Deployment, Security-Controls, Technology-Choices, Workflows, `_Sidebar` and the picture of
the network.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The wiki has a `_Sidebar.md` | Pass | Unchanged |
| 2 | The sidebar starts with the title, then one list, `Home` first | Pass | Unchanged; the new page is a line of the one list |
| 3 | The page list links every English page, and no link is dead | Pass | The sidebar lists the 24 pages of `docs/wiki` and nothing else; the 183 links of the changed pages to a page or to a heading resolve |
| 4 | A `Related` section only with related wikis | N-A | Unchanged |
| 5 | A `Languages` section last | N-A | One language |
| 6 | No page list per language | Pass | |
| 7 | A generated sidebar names its generator | N-A | Written by hand |
| 8 | A language line on each page | N-A | One language: the PO language is English, and no page of the wiki has a language line |
| 9 | English and the PO language | Pass | The PO language is English |
| 10 | The same pages in every language | N-A | One language |
| 11 | Every page starts with the language line | N-A | One language, as for criterion 8 |
| 12 | Page names carry the language code | N-A | English has no suffix |
| 13 | Code, commands, file names and IDs in English | Pass | |
| 14 | Checked on the published wiki | Pending | The wiki is published from `main` after the merge; the gate of task 10 checks the pages and their links there |
| 15 | A change of the layout rules in the template | N-A | No change of the layout |
| 16 | A flat wiki folder | Pass | Every page directly in `docs/wiki`; the pictures stay in `images/`, as before |

The facts of the pages were read against the code and the deploy files of the branch: every setting that the API reads
for the mail (`Smtp__Host`, `Smtp__Port`, `Smtp__Security`, `Smtp__UserName`, `Smtp__Password`, `Smtp__FromAddress`,
`Smtp__FromName`, `Smtp__CertificateThumbprint`, `Smtp__MaxMailsPerDay`, `Smtp__Forwarder` and the two of
`BookingVerification`) is on the page Mail-Relay with its value or its default, and the steps of S01 write the app
password with `read -s`, never on a command line. The test of the wiki coverage passes, 133 of 133.

## Language and Domain Results (the nine documents in the PO language)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Language and Domain rows present | Pass | en / it in each, the new UC-011 and SSD-002 included |
| 2 | BCP 47 code and a registry domain | Pass | |
| 3 | Content in the stated language | Pass | English |
| 4 | The register of the type | Pass | The register of the earlier rows is kept |
| 5 | Domain terms are the PO terms of the dictionary | Pass | Booking, booking request, booking reference, guest, mailbox, number of guests, verify, made on the public site |
| 6 | Structural vocabulary in English | Pass | |
| 7 | No translated twin | Pass | |
| 8 | A change of language reviewed again | N-A | No change of language |
| 9 | A reviewer competent in the domain and the language confirmed the terms | Pass | The AI as stand-in for S01, who reads English and knows the domain; not independent |
| 10 | Abbreviations spelled out on first use | Pass | No new abbreviation |

## The documents without a checklist, against the deliverable of MIL-088

- **Rule ledger.** The table "Limits of the booking flow" has the two rows of the limit `verification_mail`, 3
  mails with a link to a mailbox in an hour and 5 in a day, with who is counted, what, the window, the number, the
  setting and the refusal; the row `booking_hour` counts the booking requests of the verification too, and not the
  verification itself. The notes under the table say why `booking.too_many_mails` has no `Retry-After`, that the
  fuse of 400 mails a day is no limit of one sender and answers 503 with `booking.mail_paused`, and the events of the
  verification. Read against the code of the branch: `MailsPerHour` 3 and `MailsPerDay` 5 of
  `BookingRequestService`, `DefaultMaxMailsPerDay` 400 of `SmtpOptions`, `LimitNames.VerificationMail` and the codes
  of `ErrorCodes`. D3: the rows `email` and `verification_mail` named an email in its normal form as who is counted,
  while the code counts by mailbox, the form of [ADR-0052] decision 6 without the `+` part and, for Gmail, without
  the dots. `LimitsTableTests` holds the table against the code and passes, 5 of 5, after the fix.
- **Class diagram.** The writer laid it out again in the four layers with their colours and the dependencies between
  them, so that QC-DCD-001 criteria 11 and 12 and the lifelines of SD-001 have a layered design to follow, and added
  every class, port and member of MIL-088 that SD-001 and OC-002 name. Each new member was read against the code: the
  signatures of `BookingRequest`, `SentMail`, `Mailbox`, the two repositories, `BookingRequestService`,
  `BookingService`, `BookingRequestCleanup` and its worker match. The PlantUML block of `dcd.md` and `dcd.puml` are the
  same text. The diagram has no ID and no Version History, as [RC-417] left it.
- **Picture of the network.** `images/network-communication.puml` draws the network `mail` of the API and the
  forwarder, and the way out to the relay over TLS; it renders, and its PNG is the render of it.
- **Artifact registry.** UC 012, SSD 003, OC 003 and the new row SD with 002.
- **Traceability matrix.** The rows of UC-011, SSD-002, OC-002 and SD-001, the row of [MIL-088] with what was built,
  the rows of the two ADRs with their reviews, and this record on the rows of the amended documents.

## Findings

- **D1, corrected while reviewing: a second mail that fails in another way.** After the fix F1 of [RC-436],
  `VerifyAsync` catches whatever fails in the mail with the reference, its record in the database included, writes the
  event of a mail not sent with the reason `other` and answers the receipt ([ADR-0052] decision 5). UC-011 9a and its
  rule named only the relay and the fuse, and CO4 of OC-002 had no exception for it (QC-UC-001 9, QC-OC-001 4). 9a and
  its rule now say "or the sending fails in another way" and that the booking stands, and CO4 has the exception: the
  booking, the guest and the sign-in stand, the answer is the receipt, and a record of the mail stored before the
  failure stays until it is 24 hours old, counted by the fuse only.
- **D2, corrected while reviewing: the second and third diagrams of SD-001.** The first diagram now has a note at the
  reference to the mail: whatever fails there ends in a catch as `MailNotSent(BookingReceipt, "other")`, the booking
  stands, and the answer is the receipt; the table Postcondition Coverage says the same. The message to the relay in
  the third diagram now has the one bound of 15 seconds, the quit included ([ADR-0053] decision 1).
- **D3, corrected while reviewing: the ledger counted by email.** See the ledger above.
- **D4, corrected while reviewing: the wiki said "per email".** The row Rate limits of Security-Controls said 3 coming
  bookings per email and a limit per email; it now says per mailbox, as the code counts.
- **D5, corrected while reviewing: ADR-0052 did not link its new documents.** Its Affected Artifacts said that task 3
  writes UC-011 and its documents and that they would cite the record. They exist now: [UC-011] with [SSD-002],
  [OC-002] and [SD-001] is a line of the list and in the CrossReference, and the class diagram and the ledger are
  named under the list (QC-ADR-001 7).
- **D6, corrected while reviewing: OC-001 did not cite the first sequence diagram.** The writer left OC-001 as it was
  because none of its contracts changed. The artifact rule asks that the first instance of a type goes into the
  CrossReference of every artifact whose type lists it, with a new row; OC-001 has [SD-001] now. The class diagram,
  whose type lists it too, has no Metadata.
- **D7, reported by the writer, corrected in the code: the remarks of the lock.** The summary of
  `IUnitOfWork.RunUnderLockAsync` named only the lock `booking_request`; it names both locks now (733a5b2), recorded
  as F16 of [RC-436].
- **N1, note: the fuse of 400, not 1,000.** Decision 8 of [MIL-088] says that a fuse stops the site at 1,000 mails a
  day; [ADR-0053] decision 6 chose 400, under the 500 mails a day of Gmail, and says why. Criterion 1 of [MIL-088]
  asks for the decisions "with any changed number and its reason", so the milestone stays as accepted, and its gate
  reads the number from the ADR.
- **N2, note: success criterion 48 of [BC-001].** It counts per email and refuses a request over a limit with 429. The
  code counts per mailbox, which is stricter, because every limit per mailbox is also a limit per email, and answers
  429 for every limit that the criterion names. The 503 of the fuse and of a relay that does not take a mail are not
  limits of the criterion. The Business Case stays as it is.
- **N3, note: a refused mail after a renewal.** When the relay does not take the mail of a request that renewed the
  open request of the mailbox, the request is removed, so the link of the earlier mail no longer works either. That is
  [ADR-0052] decision 6 ("the request and its record are removed"), and UC-011 4g and CO2 of OC-002 say it.
- **N4, note: DM-004 lacked made on the public site.** DM-001 has had the attribute since [RC-427]; the writer added it
  to the project model.
- **N5, note: a stay that lapses before the verification.** A request whose arrival date has passed is refused after
  the claim deleted it, as CO4 says: a claimed request is gone whatever follows ([ADR-0052] decision 5).
- **N6, note: the writer's choices, kept as stand-in decisions.** SD-001 realizes only verifyBooking, the core of the
  milestone, and says that bookAtOnce books through the frame of its second diagram; asking again is the same message
  askForBooking; the clean-up is an operation that time starts; the use case diagram keeps three crossings, because
  the layouts with two put arrows of different actors on top of each other.

## Overall Verdict

Go, accepted by the AI as stand-in for S01 in autonomous mode. Every applicable criterion of [QC-UC-001],
[QC-SSD-001], [QC-OC-001], [QC-SD-001], [QC-UCD-001], [QC-US-001], [QC-DM-001], [QC-DICT-001], [QC-ADR-001] and
[QC-LANG-001] passes after D1, D2, D5 and D6, but criterion 5 of [QC-US-001], an optional one, as for the stories
before it. [QC-WIKI-001] passes but criterion 14, which waits for the publish of the wiki after the merge. The
documents without a checklist say what the deliverable of [MIL-088] and the two ADRs say and what the code of the
branch does. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the rows of the documents it reviewed, or return them | S01; done on 2026-11-20 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-088-booking-verified-by-email` |
| Check the pages and links of the wiki on the published wiki (QC-WIKI-001 14) | mikemike | At the gate of [MIL-088], task 10 |

---

[UC-011]: ../../use-cases/uc-011/uc.md
[SSD-002]: ../../use-cases/uc-011/ssd.md
[OC-002]: ../../use-cases/uc-011/oc.md
[SD-001]: ../../use-cases/uc-011/sd.md
[UCD-001]: ../../use-case-diagram.md
[US-001]: ../../user-stories.md
[DM-004]: ../../domain-model.md
[DICT-001]: ../../dictionary.md
[UC-001]: ../../use-cases/uc-001/uc.md
[UC-010]: ../../use-cases/uc-010/uc.md
[SSD-001]: ../../use-cases/uc-010/ssd.md
[OC-001]: ../../use-cases/uc-010/oc.md
[ADR-0052]: ../../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../../adr/adr-0053-mail-from-the-application-through-a-relay.md
[QC-UC-001]: ../../../framework/qc/artifacts/qc-use-case.md
[QC-SSD-001]: ../../../framework/qc/artifacts/qc-ssd.md
[QC-OC-001]: ../../../framework/qc/artifacts/qc-operation-contract.md
[QC-SD-001]: ../../../framework/qc/artifacts/qc-sequence-diagram.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
[QC-US-001]: ../../../framework/qc/artifacts/qc-user-story.md
[QC-DM-001]: ../../../framework/qc/artifacts/qc-domain-model.md
[QC-DICT-001]: ../../../framework/qc/artifacts/qc-dictionary.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[BC-001]: ../../business-case.md
[SA-001]: ../../stakeholder-analysis.md
[RC-413]: ./rc-413-mil-088.md
[RC-417]: ./rc-417-mil-085-documents.md
[RC-427]: ./rc-427-mil-087-documents.md
[RC-433]: ./rc-433-adr-0052-second-round.md
[RC-434]: ./rc-434-adr-0053-second-round.md
[RC-436]: ./rc-436-mil-088-code.md
[ADR-0051]: ../../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
