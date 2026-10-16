# User Story

## Metadata
| Key | Value |
| --- | --- |
| ID | US-001 |
| CrossReference | [UCD-001], [BC-001], [MIL-002] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-012 Go | [469ff7a] |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Added US-001.06 to US-001.08, the hotel staff stories (MIL-007) | pending |

---

## Purpose and Scope

Five stories from the owner (S01) and DevOps (S02): a market analysis with
diagrams and an AI analysis, the choice of a local AI model by response time
against answer quality, nginx as the proxy for every web and API service, and
two services on Proxmox VE virtual machines behind that proxy
(`NF-Hotel/notebook-benchmark` and `NF-Hotel/Notebook_Market_Analysis`). The
stories were written in Danish by the user and are kept here in English, the
language of the project artifacts. They are candidates: the use cases and
priorities are proposals for the Product Owner to accept, merge or drop. They
extend the scope of [BC-001], which covers only the Proxmox tests and the nginx
and web compose files; the Business Case needs a new objective before a
milestone for them is planned.

## Story List

### US-001.01 — View the market analysis

**As an** Owner (S01), **I want** to see the market analysis, **so that** I can
understand the market with diagrams and an AI analysis.

**Acceptance Criteria**

- Given the analysis service has results, when the owner opens the market analysis, then diagrams are shown.
- Given the analysis service has results, when the owner opens the market analysis, then an AI analysis of the same data is shown next to the diagrams.
- Given the service has no results yet, when the owner opens the market analysis, then a clear message says so instead of an empty page.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "View Market Analysis" | fits one iteration | none | Must | Direct value to the owner; the analysis application already exists |

### US-001.02 — Choose the right local AI model

**As an** Owner (S01), **I want** to be sure that the right local AI is chosen,
weighing time against answer quality, **so that** the analysis is both fast
enough and good enough.

**Acceptance Criteria**

- Given at least two local models have been benchmarked, when the owner opens the comparison, then each model is shown with its response time and an answer-quality result side by side.
- Given a quality threshold and a time limit are set, when the comparison is shown, then models that miss either are marked.
- Given the benchmark ran on a stated machine, when the owner reads the result, then that machine (CPU, memory, GPU) is named, because time depends on the hardware.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use cases "Choose Local AI Model" and "Compare Models by Time and Quality" | fits one iteration | Depends on US-001.04 for the data | Should | Needs benchmark results first; the threshold values are for the Product Owner to set |

### US-001.03 — nginx in front of every web and API service

**As a** DevOps engineer (S02), **I want** nginx to be the proxy for all web
and API services, **so that** every service is reached through one entry point.

**Acceptance Criteria**

- Given a web or API service runs as a container, when it is started, then it publishes no host port and is reachable only through nginx.
- Given the service is stopped, when a request arrives at nginx, then nginx answers with an error and keeps running.
- Given a new service is added, when its upstream name is added to the nginx settings, then it is reachable through nginx without changing other services.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| [MIL-002] and UCD-001 use case "Route Services Through Nginx" | fits one iteration | none | Must | Started in MIL-002 for the web application; the other two services depend on it |

### US-001.04 — A service that tests time and answer quality

**As a** DevOps engineer (S02), **I want** a service that tests the time and
answer-quality requirements, running on a Proxmox virtual machine as a Docker
container (from `NF-Hotel/notebook-benchmark`) behind the proxy, **so that** the
model choice rests on measured results.

**Acceptance Criteria**

- Given the Proxmox VM exists, when the service is started with Docker, then it runs as a container on that VM and is reachable only through the nginx proxy.
- Given the service runs, when a benchmark is started, then response time and answer quality are measured per model and kept as results.
- Given the local model servers (Ollama or LM Studio) are not reachable, when a benchmark is started, then the service reports which one is unavailable and does not hang.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "Run Time and Quality Benchmark" | does not yet fit one iteration | Small: splits into VM creation, container and proxy route | Should | Value is the model choice (US-001.02); risk is GPU access from a virtual machine |

### US-001.05 — The market analysis service on a Proxmox VM

**As a** DevOps engineer (S02), **I want** the service
`NF-Hotel/Notebook_Market_Analysis` to run on a Proxmox virtual machine through
Docker Compose behind the proxy, **so that** the owner can reach the market
analysis without anyone starting it by hand.

**Acceptance Criteria**

- Given the Proxmox VM exists, when `docker compose up` is run, then the analysis service starts with its own compose file and its own `<service>.env` file.
- Given the service runs, when a request arrives at the nginx proxy, then it is answered by the analysis service and the service publishes no host port.
- Given the settings are in `<service>.env`, when the repository is searched, then no secret is committed.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "Run Market Analysis Service" | fits one iteration | none | Must | Needed before the owner can view the analysis (US-001.01); reuses the compose pattern of MIL-002 |

### US-001.06 — Manage bookings

**As** hotel staff (S05), **I want** to create, confirm, check in, check out, cancel and move a booking, **so that** every guest has a room for the nights they booked and no room is booked twice.

**Acceptance Criteria**

- Given a room and a period, when staff create a booking, then the booking is stored as pending, unless the room is already booked for any night of the period.
- Given a booking in a status that does not allow an action, when staff look at the booking, then the action is not offered.
- Given a booking is cancelled, when staff list the bookings, then it is still listed with the status cancelled.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "Manage Bookings" | larger than one iteration; the code already exists | Small | Must | The core of the back office; its rules are the 126-rule ledger in `docs/refactor/BusinessRules.md` |

### US-001.07 — Manage rooms

**As** hotel staff (S05), **I want** to create and change rooms, take them out of service and record their cleaning state, **so that** only rooms that can be let are offered to guests.

**Acceptance Criteria**

- Given a new room, when staff save it, then its number, floor, size and capacity are stored, and a number already in use is refused.
- Given a room with bookings, when staff delete it, then the delete is refused.
- Given a room that is out of service, when staff search for a free room, then it is not offered.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "Manage Rooms" | fits one iteration | none | Must | Bookings need rooms |

### US-001.08 — Manage guests

**As** hotel staff (S05), **I want** to find, register, change and delete guests, **so that** a booking has a guest and the guest's details are correct.

**Acceptance Criteria**

- Given a name, a country or an email, when staff search, then all guests that match every search word are listed.
- Given a new guest, when staff save it without a required detail, then every missing or invalid detail is reported at once.
- Given a guest with bookings, when staff delete the guest, then the delete is refused.

| Traces to | Size | INVEST exceptions | Priority (MoSCoW) | Reason |
| --- | --- | --- | --- | --- |
| UCD-001 use case "Manage Guests" | fits one iteration | none | Must | Bookings need guests; the passport number is personal data (D-04) |

## INVEST Check

- Independent: US-001.01 needs US-001.05; US-001.02 needs US-001.04; US-001.04 and US-001.05 need US-001.03. The order is stated in the Reason column.
- Negotiable: no implementation is fixed beyond what the user named (Proxmox VM, Docker, nginx, the two repositories).
- Valuable: each story names the benefit to the owner or to DevOps.
- Estimable: US-001.01, .03, .05, .07 and .08 can be estimated now; .02 and .04 need the hardware question answered; .06 is already built.
- Small: US-001.04 and US-001.06 are larger than one iteration and are flagged above.
- Testable: every story has Given/When/Then criteria.

Stories US-001.06 to US-001.08 describe the hotel back office that exists in `src/`; they were derived from the rule ledger in `docs/refactor/BusinessRules.md` by the AI and are for S01 and S04 to confirm, merge or drop.

Open questions for the Product Owner: where the Proxmox host lives (the
throwaway container of MIL-003 is for tests only), a virtual machine or an LXC
container, how a GPU reaches the VM, and the time limit and quality threshold
for US-001.02.

---

[UCD-001]: ./use-case-diagram.md
[BC-001]: ./business-case.md
[MIL-002]: ./milestones/mil-002-nginx-proxy-and-web-compose.md
[SA-001]: ./stakeholder-analysis.md
[fc8ec78]: https://github.com/NF-Hotel/NFHotel/commit/fc8ec78b24b805b10d5c056b19ed2785697ee8ff
[469ff7a]: https://github.com/NF-Hotel/NFHotel/commit/469ff7a7871bb900155dcaf0639b5db1371681aa
