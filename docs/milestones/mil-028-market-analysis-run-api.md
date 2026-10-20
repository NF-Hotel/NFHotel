# MIL-028 Market analysis run API in the market analysis service

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-028 |
| CrossReference | [BC-001], [US-001], [MIL-004], [MIL-026] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | Claude (AI) | AI at the request of S01, not independent | Initial version: the market analysis service in `NF-Hotel/notebook-market-analysis` gets the analysis run API that [MIL-026] needs; accepted by S01 in chat on 2026-10-20 after RC-124 Go | [5bc5cf0] |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Three analyses added for the UCL assignment of S01: occupancy, occupancy on holidays and the correlation matrix, so nine analyses; the run request carries the number of rooms; the result has the formulas and the limits of each analysis; accepted by the user in chat on 2026-10-20 after RC-127 Go | [5bc5cf0] |

---

## Purpose

Give the market analysis service the analysis that the website needs. [MIL-004] deployed `NF-Hotel/Notebook_Market_Analysis` and [MIL-026] was built on its `POST /analyze?insights=true`. That repository is gone from GitHub: its successor `NF-Hotel/notebook-market-analysis` is a new service of which only `/health` and the holiday calendar are built, so the image of the old pinned commit (`6c11cbf2`) can no longer be built and the release check of pull request 180 fails. The milestone builds the missing part in the new service, in its Clean Architecture layers: the nine analyses of the bookings (lead time, holidays, seasonality, cancellations, room value, guest mix, occupancy, occupancy on holidays, correlations), the AI executive summary and suggestions of each, and nine analyses with a run API (start, state, result) behind the key `X-API-Key`. The milestone is planned here because the Business Case, the stakeholders and the issues of NF Hotel are kept in this repository (objective 6 of [BC-001]); the code is written in the market analysis repository, which is told by its pull requests.

## Deliverable

- **Contract first.** `openapi/market-contract.yaml` of the market analysis service gets the run paths (`POST /api/v1/market-analysis/runs`, `GET /api/v1/market-analysis/runs/{id}`, `GET /api/v1/market-analysis/runs/{id}/result`) with the booking as input (the fields of `MarketBookingDto` of the website, an optional period) and result schema 1.1 as output (the sample `docs/test-data/market-analysis-result.json` of this repository is the first test file of both sides).
- The nine analyses over the bookings that are posted, in the domain and application layers, with unit tests; no notebook library in the image: the six of the first version (lead time, holidays, seasonality, cancellations, room value, guest mix) and three new ones, occupancy (the share of room nights that are taken, by month and overall), occupancy on holidays (the same share on holiday days against other days) and correlations (the Spearman correlation between the numeric booking fields and cancelled, as a matrix). The run request carries `room_count` (the rooms of the hotel), because the bookings do not say how many rooms there are; the result gives for each analysis the formula it used and what it cannot say (`limitations`).
- An AI port and adapters (Ollama and LM Studio, as `docs/architecture.md` of that repository plans) that write an `insight` with `executive_summary` and `improvement_suggestions` for every analysis, and one observation for every chart, what the AI sees in that series (`observations`, each with the `series` path of the group figures it reads, its text and its evidence), written in the same request as the insight of the analysis so that a run stays within its 12 minutes, with a guardrail that gives `GUARDRAIL_REJECTED` and leaves the numbers untouched.
- A background run with one run at a time, a state (`queued`, `running`, `completed`, `failed`) and the result kept in the output volume.
- Deploy: the image and `deploy/docker-compose.yml` of that repository start the service with the AI settings in its env file; the Dockerfile still needs no network at build time other than the package index.
- Unchanged: the holiday calendar paths and their contract.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The contract file describes the three run paths and result schema 1.1, and a test fails when the implementation and the contract differ | Yes | No |
| 2 | A start with the sample bookings returns at once with a run id; the state follows `queued`, `running`, `completed`; a second start while a run is active is refused with a stable error code | Yes | No |
| 3 | The result of a completed run has schema version 1.1 with the nine analyses; a test compares each figure with a hand calculation on a small booking file, among them an occupancy of 50 % for two rooms with one room taken for the whole period, an occupancy on holidays that differs from the one on other days, and a correlation of +1 and -1 for two series built to move together and apart | Yes | No |
| 4 | With a fake AI provider every analysis has an `insight` with an executive summary and suggestions and every series of its findings (at most 8) has an observation that names the numbers it rests on, which a test finds in the series; a rejected output gives `GUARDRAIL_REJECTED` for that analysis only, and an observation that names a number not in the series is dropped; with insights off there is no `insight` and no AI call; every request to the model carries the maximum output tokens, 32768 unless the env file says otherwise | Yes | No |
| 5 | A missing or wrong `X-API-Key` gives 401 on every run path; `/health` needs no key | Yes | No |
| 6 | A body with an invalid booking, an unknown field type or an empty list is refused with 400 and a stable code, and nothing is stored | Yes | No |
| 7 | A run of 55,484 bookings completes with the fake AI provider in under 60 seconds in the tests of that repository, and the container stays within the `mem_limit` that its compose file sets (measured in the same test run) | Yes | No |
| 8 | The image builds from the repository alone (no fetch of another repository), publishes no host port and runs as a non-root user with a read-only file system | Yes | No |
| 9 | Tests pass, the linters of that repository are clean, and `qc-programming-python` and `qc-yaml` reviews have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-004 | The deployment pattern of the market analysis service behind the VM network |
| ADR-0003 | The VM and the network rules |
| `NF-Hotel/notebook-market-analysis` | The repository the code goes in; it has no Business Case or milestone of its own yet, so this milestone is the plan for its pull requests until it gets them |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 6 of BC-001 (market analysis service) | All tasks |
| US-001.01 View the market analysis | Tasks 2 to 4 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the container and the ports |

## Target Date

2026-11-13 — two weeks from the plan, so that [MIL-026] can follow by 2026-11-27; for S01 to confirm.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Agree the run contract | Add the three run paths and result schema 1.1 to `openapi/market-contract.yaml` of the market analysis service, from the input of `MarketBookingDto` and the sample result of this repository, and add a contract test that reads the file. Why: the website and the service are built against the same file. | No | |
| 2 | Domain and the nine analyses | Write the booking and the nine analyses (lead time, holidays, seasonality, cancellations, room value, guest mix, occupancy (the share of room nights that are taken, by month and overall), occupancy on holidays (the same share on holiday days against other days) and correlations (the Spearman correlation between the numeric booking fields and cancelled, as a matrix)) in the domain and application layers with unit tests against hand calculated figures. The holidays analyses read the holiday calendar the service already has. Occupancy is the room nights taken divided by `room_count` times the nights of the period; a night is taken from arrival to the day before departure. Each analysis returns its formula and its limitations. | Yes | US-001.01 |
| 3 | AI insights | An AI port and the Ollama and LM Studio adapters; one insight per analysis with executive summary and suggestions and one observation per chart (what the AI sees in the series, with the numbers it rests on), asked in the same request as the insight; the guardrail and `GUARDRAIL_REJECTED`; a fake provider for the tests; settings in the env file, among them the maximum output tokens of a model answer, 32768 by default (a reasoning model needs the room to think and still write the answer). | Yes | US-001.01 |
| 4 | Run API and result storage | The three run paths, one run at a time in the background, state and result in the output volume, `X-API-Key` on every path, stable error codes. | Yes | US-001.01 |
| 5 | Image and compose | Keep the Dockerfile free of other repositories, add the AI settings to `deploy/market-analysis.env.example`, check the limits for a run of 55,484 bookings and update the README of that repository. | No | |
| 6 | Check and review | Tests, linters and the reviews against `qc-programming-python` and `qc-yaml`; record the RC. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-004]: ./mil-004-market-analysis-service.md
[MIL-026]: ./mil-026-market-analysis-report.md
[5bc5cf0]: https://github.com/NF-Hotel/NFHotel/commit/5bc5cf054789ce61d82e0f429092a4e485ae7c27
