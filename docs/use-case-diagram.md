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
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Initial version: market analysis, local AI selection and hosting on Proxmox behind nginx | [fc8ec78] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-013 Go | pending |

---

## Purpose and Scope

The system boundary is the NFHotel platform as the owner and DevOps use it: a
market analysis that shows diagrams and an AI analysis, a benchmark that
compares local AI models on time and answer quality, and the hosting of both on
Proxmox VE virtual machines as Docker containers behind one nginx reverse proxy.
The hotel booking web application itself (staff and guest flows) is outside this
diagram. The two source services are the repositories
`NF-Hotel/Notebook_Market_Analysis` and `NF-Hotel/notebook-benchmark`.

## Diagram

```plantuml
@startuml
left to right direction
actor "Owner" as Owner <<Actor>>
actor "DevOps" as DevOps <<Actor>>
rectangle "NFHotel platform" <<System>> {
  usecase "View Market Analysis" as UC1
  usecase "Choose Local AI Model" as UC2
  usecase "Route Services Through Nginx" as UC3
  usecase "Run Time and Quality Benchmark" as UC4
  usecase "Run Market Analysis Service" as UC5
  usecase "Compare Models by Time and Quality" as UC6
}
Owner --> UC1
Owner --> UC2
DevOps --> UC3
DevOps --> UC4
DevOps --> UC5
UC2 ..> UC6 : <<include>>
UC4 ..> UC3 : <<include>>
UC5 ..> UC3 : <<include>>
@enduml
```

## Actor Table

| Actor | Stereotype | Stakeholder ID (SA) | Goals (use cases) |
| --- | --- | --- | --- |
| Owner | `<<Actor>>` | S01 | View Market Analysis; Choose Local AI Model |
| DevOps | `<<Actor>>` | S02 | Route Services Through Nginx; Run Time and Quality Benchmark; Run Market Analysis Service |

## Use Case Table

| Use Case | Actor(s) | Goal | Source story | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- | --- |
| View Market Analysis | Owner | See the market analysis as diagrams with an AI analysis | [US-001].01 | Must | Direct value to the owner; the analysis application already exists |
| Choose Local AI Model | Owner | Pick the local AI model with the best balance of response time and answer quality | [US-001].02 | Should | Depends on benchmark results (UC Run Time and Quality Benchmark) |
| Route Services Through Nginx | DevOps | Reach every web and API service only through the nginx proxy | [US-001].03 | Must | Already started in MIL-002; the other services depend on it |
| Run Time and Quality Benchmark | DevOps | Run the time and answer-quality test as a service on a Proxmox VM behind the proxy | [US-001].04 | Should | Hardware dependent (GPU access from a VM is the main risk) |
| Run Market Analysis Service | DevOps | Run the market analysis service on a Proxmox VM through Docker Compose behind the proxy | [US-001].05 | Must | Needed before the owner can view the analysis |
| Compare Models by Time and Quality | Owner | See models side by side by response time and answer quality | [US-001].02 | Should | Part of choosing a model; shown by the benchmark results |

## Role Matrix

Suggestion for the Product Owner to approve. There are two primary actors, so
roles follow the actors: the Owner role decides on analysis and model choice;
the DevOps role operates the services. Data visibility is not covered.

| Use Case | Owner | DevOps |
| --- | --- | --- |
| View Market Analysis | performs | - |
| Choose Local AI Model | performs | - |
| Compare Models by Time and Quality | performs | - |
| Route Services Through Nginx | - | performs |
| Run Time and Quality Benchmark | - | performs |
| Run Market Analysis Service | - | performs |

No use case is shared by both roles. Whether DevOps may also view the analysis
is a question for the Product Owner.

## Relationships

| From | Relationship (`<<include>>` / `<<extend>>`) | To | Justification |
| --- | --- | --- | --- |
| Choose Local AI Model | `<<include>>` | Compare Models by Time and Quality | A model choice always needs the comparison; it is a mandatory part of the goal |
| Run Time and Quality Benchmark | `<<include>>` | Route Services Through Nginx | The benchmark service is only reachable behind the proxy |
| Run Market Analysis Service | `<<include>>` | Route Services Through Nginx | The analysis service is only reachable behind the proxy |

---

[SA-001]: ./stakeholder-analysis.md
[BC-001]: ./business-case.md
[US-001]: ./user-stories.md
[fc8ec78]: https://github.com/NF-Hotel/NFHotel/commit/fc8ec78b24b805b10d5c056b19ed2785697ee8ff
