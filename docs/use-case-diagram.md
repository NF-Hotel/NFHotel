# Use Case Diagram

## Metadata
| Key | Value |
| --- | --- |
| ID | UCD-001 |
| CrossReference | [SA-001], [BC-001], [US-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Deprecated | mikemike | AI as stand-in for S01 | View and Change Own Booking: the guest signs in with the booking reference in place of the booking number (MIL-085, ADR-0049)<br>Go in RC-417; accepted by the AI as stand-in for S01 in autonomous mode | [e551458] |
| 2026-11-20 | Accepted | mikemike | AI as stand-in for S01 | The use case Book a Room and Verify It by Email of the Guest (UC-011, US-001.14) added for MIL-088: a booking on the website is inside the boundary now, verified through a link mailed to its email (ADR-0052, ADR-0053); the diagram laid out again with the fewest crossings<br>Go in RC-435; accepted by the AI as stand-in for S01 in autonomous mode | pending |

---

## Purpose and Scope

The system boundary is the NFHotel platform as the owner and DevOps use it: a
market analysis that shows diagrams and an AI analysis, a benchmark that
compares local AI models on time and answer quality, and the hosting of both on
Proxmox VE virtual machines as Docker containers behind one nginx reverse proxy.
The back office of the hotel (staff managing bookings, rooms and guests, added for MIL-007) is inside it, and so are a guest's view and change of the own booking (MIL-018) and a guest's booking on the
website, verified through a link mailed to the email (MIL-088); the screens are outside this diagram. The two source services are the repositories
`NF-Hotel/notebook-market-analysis` and `NF-Hotel/notebook-benchmark`.

## Diagram

```plantuml
@startuml
skinparam linetype polyline
actor "DevOps" as DevOps <<Actor>>
actor "Guest" as Guest <<Actor>> #1F77B4
actor "Owner" as Owner <<Actor>> #E67E22
actor "Hotel staff" as Staff <<Actor>> #D62728
rectangle "NFHotel platform" <<System>> {
  usecase "Run Market Analysis Service" as UC5
  usecase "Run Time and Quality Benchmark" as UC4
  usecase "Route Services Through Nginx" as UC3
  usecase "View and Change Own Booking" as UC14
  usecase "Book a Room and Verify It by Email" as UC15
  usecase "View Market Analysis" as UC1
  usecase "Choose Local AI Model" as UC2
  usecase "Compare Models by Time and Quality" as UC6
  usecase "Manage Bookings" as UC7
  usecase "Manage Rooms" as UC8
  usecase "Manage Guests" as UC9
  usecase "Change Own Password" as UC11
  usecase "Manage Staff Accounts" as UC12
  usecase "Approve a Price Suggestion" as UC13
  usecase "Sign In To The Back Office" as UC10
}
Owner -[#E67E22]-> UC1
Owner -[#E67E22]-> UC2
DevOps --> UC3
DevOps --> UC4
DevOps --> UC5
Staff -[#D62728]-> UC4
Staff -[#D62728]-> UC6
Staff -[#D62728]-> UC7
Staff -[#D62728]-> UC8
Staff -[#D62728]-> UC9
Staff -[#D62728]-> UC10
Staff -[#D62728]-> UC11
Staff -[#D62728]-> UC12
Staff -[#D62728]-> UC13
Guest -[#1F77B4]-> UC14
Guest -[#1F77B4]-> UC15
UC7 ..> UC10 : <<include>>
UC8 ..> UC10 : <<include>>
UC11 ..> UC10 : <<include>>
UC12 ..> UC10 : <<include>>
UC13 ..> UC10 : <<include>>
UC6 ..> UC10 : <<include>>
UC9 ..> UC10 : <<include>>
UC2 ..> UC6 : <<include>>
UC4 ..> UC3 : <<include>>
UC5 ..> UC3 : <<include>>
@enduml
```

Crossings: 3, all of the arrow from Hotel staff to Run Time and Quality Benchmark, which crosses the arrow of the
Guest to Book a Room and Verify It by Email and the two arrows of the Owner. Hotel staff, the Guest and the Owner are
drawn in red, blue and orange, so that each line can be followed. Of the layouts tried (the orders of the actors and
the use cases, top to bottom and left to right, curved and straight lines), the few with two crossings ran arrows of
different actors on top of each other.

## Actor Table

| Actor | Stereotype | Stakeholder ID (SA) | Goals (use cases) |
| --- | --- | --- | --- |
| Owner | `<<Actor>>` | S01 | View Market Analysis; Choose Local AI Model |
| DevOps | `<<Actor>>` | S02 | Route Services Through Nginx; Run Time and Quality Benchmark; Run Market Analysis Service |
| Hotel staff | `<<Actor>>` | S05 | Manage Bookings; Manage Rooms; Manage Guests; Sign In To The Back Office; Change Own Password; Manage Staff Accounts; Approve a Price Suggestion; Run Time and Quality Benchmark (a Manager starts a run); Compare Models by Time and Quality |
| Guest | `<<Actor>>` | S06 | View and Change Own Booking; Book a Room and Verify It by Email |

## Use Case Table

| Use Case | Actor(s) | Goal | Source story | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- | --- |
| View Market Analysis | Owner | See the market analysis as diagrams with an AI analysis | [US-001].01 | Must | Direct value to the owner; the analysis application already exists |
| Choose Local AI Model | Owner | Pick the local AI model with the best balance of response time and answer quality | [US-001].02 | Should | Depends on benchmark results (UC Run Time and Quality Benchmark) |
| Route Services Through Nginx | DevOps | Reach every web and API service only through the nginx proxy | [US-001].03 | Must | Already started in MIL-002; the other services depend on it |
| Run Time and Quality Benchmark | DevOps; Hotel staff (Manager) | DevOps runs the time and answer-quality test as a service on a Proxmox VM behind the proxy; a Manager starts a run of the chosen models from the back office and follows it until its result is stored ([UC-009]) | [US-001].04 | Should | Hardware dependent (GPU access from a VM is the main risk); MIL-020 lets a Manager start a run |
| Run Market Analysis Service | DevOps | Run the market analysis service on a Proxmox VM through Docker Compose behind the proxy | [US-001].05 | Must | Needed before the owner can view the analysis |
| Compare Models by Time and Quality | Hotel staff (Manager) | See models side by side by response time and answer quality ([UC-008]) | [US-001].02 | Should | Part of choosing a model; MIL-019 shows the stored runs only to the permission `Benchmark.View`, which only the Manager holds |
| Manage Bookings | Hotel staff | Create, confirm, check in, check out, cancel and move bookings without double booking | [US-001].06 | Must | The core of the back office |
| Manage Rooms | Hotel staff | Keep the rooms, their status and their cleaning state correct | [US-001].07 | Must | Bookings need rooms |
| Manage Guests | Hotel staff | Find, register, change and delete guests | [US-001].08 | Must | Bookings need guests |
| Sign In To The Back Office | Hotel staff | Sign in with an own account and see only what the role needs | [US-001].09 | Must | The back office must not run on the public port without it |
| Change Own Password | Hotel staff | Change the own password with the current one, so that older sessions end | [US-001].10 | Must | A password that others have seen must be replaceable |
| Manage Staff Accounts | Hotel staff (Manager) | Create, disable, enable and reset accounts and read the audit trail | [US-001].11 | Must | Accounts are made by a command on the server today |
| Approve a Price Suggestion | Hotel staff (Manager) | Approve or revert a suggested price change for the high or the low season | [US-001].12 | Should | The owner wants the market analysis to lead to a price |
| View and Change Own Booking | Guest | Sign in with the booking reference and the email of the booking, see the own booking, move its dates or cancel it ([UC-010]) | [US-001].13 | Should | Objective 21 of the Business Case; no guest reaches another guest's booking |
| Book a Room and Verify It by Email | Guest | Ask for a room on the website and book it through the link that the hotel mails to the email given ([UC-011]) | [US-001].14 | Must | Objective 43 and success criterion 48 of the Business Case: a booking on the website takes a room only after its email is verified |

## Role Matrix

Suggestion for the Product Owner to approve. There are four primary actors, so
roles follow the actors: the Owner role decides on analysis and model choice;
the DevOps role operates the services; the Hotel staff role runs the back office;
the Guest role books a room on the website and sees and changes only the own booking. Data visibility is not covered.

| Use Case | Owner | DevOps | Hotel staff | Guest |
| --- | --- | --- | --- | --- |
| View Market Analysis | performs | - | - | - |
| Choose Local AI Model | performs | - | - | - |
| Compare Models by Time and Quality | - | - | performs (Manager only) | - |
| Route Services Through Nginx | - | performs | - | - |
| Run Time and Quality Benchmark | - | performs (runs the service) | performs (Manager only: starts a run) | - |
| Run Market Analysis Service | - | performs | - | - |
| Manage Bookings | - | - | performs | - |
| Manage Rooms | - | - | performs | - |
| Manage Guests | - | - | performs | - |
| Sign In To The Back Office | - | - | performs | - |
| Change Own Password | - | - | performs | - |
| Manage Staff Accounts | - | - | performs (Manager only) | - |
| Approve a Price Suggestion | - | - | performs (Manager only) | - |
| View and Change Own Booking | - | - | - | performs (the own booking only) |
| Book a Room and Verify It by Email | - | - | - | performs |

One use case is shared by two roles: in Run Time and Quality Benchmark, DevOps
runs the service and a Manager starts a run (MIL-020). The Owner compares the
models and starts runs through an account with the role Manager, which alone
holds `Benchmark.View` and `Benchmark.Run` (MIL-019, decision 4). Whether DevOps
may also view the analysis is a question for the Product Owner.

## Relationships

| From | Relationship (`<<include>>` / `<<extend>>`) | To | Justification |
| --- | --- | --- | --- |
| Choose Local AI Model | `<<include>>` | Compare Models by Time and Quality | A model choice always needs the comparison; it is a mandatory part of the goal |
| Run Time and Quality Benchmark | `<<include>>` | Route Services Through Nginx | The benchmark service is only reachable behind the proxy |
| Run Market Analysis Service | `<<include>>` | Route Services Through Nginx | The analysis service is only reachable behind the proxy |
| Manage Bookings, Manage Rooms, Manage Guests | `<<include>>` | Sign In To The Back Office | Every back office function needs a signed-in member of staff |
| Change Own Password, Manage Staff Accounts, Approve a Price Suggestion | `<<include>>` | Sign In To The Back Office | Both need a signed-in member of staff |
| Compare Models by Time and Quality | `<<include>>` | Sign In To The Back Office | The stored runs are in the back office, for a signed-in Manager. Run Time and Quality Benchmark does not include it: DevOps runs the service outside the back office, and a Manager who starts a run has signed in first (a precondition of [UC-009]) |

Book a Room and Verify It by Email has no relationship with View and Change Own Booking: the guest who verifies a
booking is signed in to it at the end of the one and goes on with the other from there (extension 1a of [UC-010]),
which is two goals in a row, not an `<<include>>` or an `<<extend>>`.

---

[SA-001]: ./stakeholder-analysis.md
[BC-001]: ./business-case.md
[US-001]: ./user-stories.md
[UC-008]: ./use-cases/uc-008/uc.md
[UC-009]: ./use-cases/uc-009/uc.md
[UC-010]: ./use-cases/uc-010/uc.md
[UC-011]: ./use-cases/uc-011/uc.md
[e551458]: https://github.com/NF-Hotel/NFHotel/commit/e551458c167261cad75f9ffd2902fb6163a78b81
