# MIL-026 Market analysis report in the back office

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-026 |
| CrossReference | [BC-001], [US-001], [MIL-004], [MIL-019], [MIL-020], [MIL-028] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | Claude (AI) | AI at the request of the user, not independent | Tasks 20 and 21 and criteria 20 and 21: the number of rooms goes to the service for the occupancy analyses, a correlation matrix is drawn as a heat map, and each chart says what it shows and why that kind of chart was chosen (the course assignment of S01); accepted by the user in chat on 2026-10-20 after RC-128 Go | [5bc5cf0] |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of the user, not independent | Task 13 uses short timeouts, because a run is followed by its run id and no HTTP request stays open; `MarketAnalysisService__TimeoutSeconds` is set back to its default; accepted by the user in chat on 2026-10-20 after RC-130 Go | [c9f8d5c] |

---

## Purpose

Let a Manager start a market analysis of the hotel bookings and read its report in the back office, with the AI executive summary of every analysis. The website API (API to API) reads the bookings of the database, sends them to the run API of the market analysis service (`NF-Hotel/notebook-market-analysis`, built by [MIL-028]), stores the result and a Blazor page shows it. The second version called `Notebook_Market_Analysis`, which has since been replaced by `notebook-market-analysis`; the third version (this one) calls the run API that [MIL-028] adds to it, with nine analyses and insights. The bookings need fields that the database does not have yet (booking date, price, guests, market segment and more), so the milestone adds them and loads the fake data of the repository (`nf_hotel_bookings.csv`) into the database. It delivers [US-001] story US-001.01 (View the market analysis).

## Deliverable

- Booking fields for the analysis (booking date, price per night, adults, children, babies, meal, market segment, customer type, deposit type, repeated guest, previous cancellations, booking changes, special requests, parking spaces, room type), a migration, and a command that loads the fake data of `nf_hotel_bookings.csv` (rooms, guests and bookings) into the database.
- A client of the market analysis service (a port in the Application layer and an adapter) that starts a run with the bookings as the JSON of its contract (`openapi/market-contract.yaml`), asks for the state of the run and reads result schema 1.1; the base address and the `X-API-Key` are in the env file, and no key is in git.
- A run of the analysis in the background of the API, because a run with AI insights takes up to 12 minutes: a start returns at once, the page asks for the state, and the API follows the run of the service by its run id instead of one long request.
- Website API endpoints: start a run (Manager only), read the state of a run, and read the latest report. The data analysis service does not push a result, so the API reads it when a completed run is first asked for, and stores it in the website database with its `schema_version`.
- The number of rooms of the hotel in the run request (the occupancy analyses of MIL-028 need it), a correlation matrix drawn as a heat map with a diverging colour scale and the value in each cell, and under every chart a short text of what it shows, why that kind of chart was chosen and what it cannot say, and what the AI sees in that chart (its observation from the service, labelled as AI-generated).
- Charts that fit the data: the kind of chart is chosen from the shape of each series by one rule (a pie or donut chart only for two to four parts of a whole, ranked bars to compare many categories, columns for ordered bands such as lead time, a line for a time series such as the months, a heat map for month against year, tiles for single figures), all as SVG without a script library.
- A back office page `/admin/market-analysis` for a Manager: start a run, follow the state (`queued`, `running`, `completed`, `failed`), and read the report: charts of the market data and the AI analysis next to them, or a clear message when there is no result yet.
- A fake of the Python service for the tests, so that no test needs Python, Ollama or the VM.
- Deploy files: the service of `notebook-market-analysis` joins the internal network `nfhotel-api` as `market-analysis-api`; `deploy/market-analysis/` of this repository, whose Dockerfile fetched the vanished repository, is removed and its README points to the deploy folder of the service; the release check builds the image from the service repository.
- Unchanged: the benchmark pages, the staff and guest sign-in, TLS.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A Manager starts a run on the page and the fake markets API receives `POST /analyze?insights=true` with the bookings of the database as the JSON array of its input contract (snake_case fields, ISO dates, `price_per_night` as a number) | Yes | No |
| 2 | A Receptionist and a Housekeeping user get 403 on every market analysis endpoint and cannot open the page | Yes | No |
| 3 | When the markets API is down, answers 422 or 500, or a run is already active, the page shows a clear message; a second start while a run is active is refused | Yes | No |
| 4 | A start returns at once and the state follows `queued`, `running`, `completed`; a failed run shows a sanitised error and keeps the last report | Yes | No |
| 5 | A completed result shows charts and, next to them, the AI executive summary and the suggestions of each analysis that has one; an analysis without an insight says why (for example `GUARDRAIL_REJECTED`); with no result the page says so instead of showing an empty page (US-001.01 acceptance criteria) | Yes | No |
| 6 | A result with an unknown `schema_version` is refused and not stored; a completed run is stored once, however often the page asks | Yes | No |
| 7 | The markets API publishes no host port and is reachable by the website API on `nfhotel-api` only; no token is in git | Yes | No |
| 8 | `dotnet build` has 0 warnings, format is clean, all tests pass; `qc-programming-csharp` and `qc-yaml` reviews have the verdict `Go` | Yes | No |
| 9 | The booking has the new fields in the database; a booking made before them has them empty and still works everywhere; the migration applies and reverts | Yes | No |
| 10 | The seed command loads the rows of `nf_hotel_bookings.csv` into rooms, guests and bookings without breaking the overlap constraint, refuses a database that already has bookings unless told to replace them, and a second run with the same file gives the same data | Yes | No |
| 11 | The bookings sent to the markets API are exactly the rows of the database: counts, cancellations, dates and prices match a SQL count made by the test | Yes | No |
| 12 | A Manager chooses a first and a last arrival date; only the bookings that arrive in the period, both days included, are sent; a first date after the last date is refused; no date means every booking; a period with no booking fails the run with a clear message | Yes | No |
| 13 | The seed command with `--through 2026-12-31` repeats the file back to back up to that arrival date, keeps the booking numbers unique, leaves out later arrivals, and gives each booking the status of today's date (checked out, checked in, confirmed or cancelled) with the overlap constraint on | Yes | No |
| 14 | The client starts a run with `POST /api/v1/market-analysis/runs` and the header `X-API-Key`, follows `GET .../runs/{id}` until `completed` or `failed`, and reads `GET .../runs/{id}/result`; a test against a fake of the service, built from the contract file of `notebook-market-analysis`, passes; no call of the client has a timeout above 60 seconds | Yes | No |
| 15 | A wrong key (401), a refused body (400), a run that fails and a service that is down each show a clear message and keep the last report | Yes | No |
| 16 | The sample result of `docs/test-data/` is the same file as the one used by the service's contract test, or a test fails when they differ | Yes | No |
| 17 | The release check builds the image of the service without fetching another repository and passes; `deploy/market-analysis/` no longer contains a Dockerfile that clones a repository | Yes | No |
| 18 | The kind of chart follows the shape of the series by one rule that a test states: two to four parts of a whole give a donut, more than four categories give ranked bars (largest first), ordered bands give columns in their order, a series of months gives a line, months against years give a heat map, a single figure gives a tile; a series that fits none keeps the bar chart | Yes | No |
| 19 | A donut is never drawn for more than four parts or for figures that are not parts of one whole; every chart has a text alternative (title and the values in a table or `aria-label`), and the colours do not carry the meaning alone | Yes | No |
| 20 | The run request carries `room_count`, the number of rooms in the database, and the result with the occupancy analyses is shown: a month chart of the occupancy and a comparison of the occupancy on holidays and on other days | Yes | No |
| 21 | The correlation matrix is drawn as a heat map with a diverging scale from -1 to +1, the value in every cell and a legend; a chart of every kind has a caption with what it shows, why that kind was chosen and its limits, taken from the `limitations` of the analysis where it has them, and the AI observation of that chart labelled as AI-generated; a chart with no observation says why (for example the insight was rejected or timed out) and never shows an empty box | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-019 | The result storage pattern and API conventions this milestone repeats |
| MIL-020 | The service client pattern, Manager policy, tokens and deploy files |
| MIL-004 | The deployment pattern of the market analysis service behind nginx |
| MIL-028 | The run API of the market analysis service that the client calls; tasks 13 to 16 start when MIL-028 is accepted and its run API is merged |
| ADR-0003 | The VM and the network rules |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 6 of BC-001 (market analysis service) | All tasks |
| US-001.01 View the market analysis | Tasks 4, 5, 17 and 18 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the container and the ports |

## Target Date

2026-11-27 — two weeks after [MIL-028] (2026-11-13), whose run API tasks 13 to 16 need; for S01 to confirm.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Agree the result contract with the markets API | Read result schema 1.1 of `Notebook_Market_Analysis` (the findings, the `insight` with `executive_summary` and `improvement_suggestions`) and the input contract of its ADR-0001, and keep a sample of each in `docs/test-data/` so that the client is tested against the same files. | No | Done: `docs/test-data/market-analysis-result.json` is a trimmed result of a real run of the markets API on the 40,060 seeded bookings (schema 1.0 findings), with the 1.1 `insights` and the insight of each analysis written by hand from the JSON Schema, because no AI model was running; the client test reads it |
| 2 | Client of the markets API | Port and adapter that post the bookings to `/analyze?insights=true` with the long timeout of ADR-0009 of that repository (12 minutes) and map result 1.1 to the report; fake of the service for the tests; base address in the env file with a tracked example. | No | Done: `IMarketAnalysisServiceClient`, `MarketAnalysisServiceClient`, `MarketAnalysisResultMapper`, `FindingsSeries`; tests against a fake HTTP service. The real markets API was run locally and accepted all 40,060 bookings (HTTP 200, no invalid or missing counts except `agent`); a run with `insights=true` was not made |
| 3 | Market analysis API, background run and storage | `Permissions.MarketAnalysisView` and `MarketAnalysisStart` (Manager only); a start returns at once and the API runs the analysis in the background, one run at a time; endpoints to start, to read the state of a run and to read the latest report; table `market_analysis_run`; reject an unknown `schema_version`. | Yes | US-001.01. Done: `MarketAnalysisController` (`POST api/market-analysis/runs`, `GET runs/{id}`, `GET latest`), `MarketAnalysisService`, `MarketAnalysisRunner`, `MarketAnalysisRunTracker`, `ChannelMarketAnalysisRunLauncher` and `MarketAnalysisRunWorker` (the background run), table `market_analysis_run`, `ApiMarketAnalysisService`; the state of a run is in memory and a restart forgets it |
| 4 | Market analysis report page | `/admin/market-analysis`: start, follow the state, show the charts next to the AI executive summary and suggestions of each analysis, and a clear message when there is no result; view model and page tests. | Yes | US-001.01. Done: `MarketAnalysisReport.razor`, `MarketAnalysisViewModel`, menu entry for the Manager; one card per analysis with its charts and AI executive summary and suggestions; not looked at in a browser |
| 5 | Charts for the market data | SVG charts of the group figures of the findings (the existing `GroupedBarChart`); no script library. | No | Done by reusing `GroupedBarChart`; `FindingsSeries` reads the group figures of the real findings (a `figure` wrapper or direct), orders the charts overview first and leaves out a series of more than 60 points |
| 6 | Deploy files | The markets API container joins `nfhotel-api`; the settings of the website API; remove `deploy/data-analysis/` of the first version; wiki section. | No | Done: the markets API joins `nfhotel-api`; `MarketAnalysisService__*` settings of `nfhotel-api.env.example`; `deploy/data-analysis/` removed; wiki section. Not applied to the VM |
| 7 | Check and review | Build, format, test; reviews against `qc-programming-csharp` and `qc-yaml`; record the RC. | No | Done: build 0 warnings, format clean, 1287 tests passed (3 Proxmox tests skipped); RC-118, RC-119 |
| 8 | Booking fields for the analysis | Add to the booking the fields that the markets API reads and the database lacks: booking date, price per night, adults, children, babies, meal, market segment, customer type, deposit type, repeated guest, previous cancellations, booking changes, special requests, parking spaces and room type. All are optional so that the existing bookings and the guest booking flow do not change; a migration; the domain checks the values. | Yes | US-001.01. Done: table `booking_market_detail` (entity `BookingMarketDetail`, migration `AddBookingMarketDetail`), one optional row per booking so that `Booking` and its state machine do not change |
| 9 | Seed the fake data | A command of the API that loads `nf_hotel_bookings.csv` (40,060 rows, 2018 to 2020) into the database: guests, as many rooms as the most bookings that are at the hotel on one night (187), and bookings placed in rooms so that the overlap constraint holds; the price per night is generated from the room type, as the file has none. It refuses a database that has bookings unless told to replace them. | Yes | US-001.01. Done: `seed-market-data <file> [--replace]` (`MarketSeedCsv`, `MarketSeedPlanner`, `MarketDataSeeder`, `MarketSeedPricing`): the real file gives 40,060 bookings, 187 rooms and about 500 guests in PostgreSQL with the overlap constraint on; the price is generated, not real |
| 10 | Bookings feed for the analysis | A read of the bookings of the database as the input contract of the markets API: nights split into weekend and week nights, lead time from the booking date, country of the guest, cancelled from the status. | Yes | US-001.01. Done: `MarketAnalysisBookingQueries`, `StayNights`; a test compares the bookings read with counts of the database |
| 11 | Choose the period of the analysis | `StartMarketAnalysisRequest` with an optional first and last arrival date (by arrival date, both included); the API refuses a first date after the last; the bookings read are those of the period; two date fields and quick choices (last 3 years, last year, all bookings) on the page. Asked for by the Manager after the first run said that the hotel had no bookings. | Yes | US-001.01. Done: `StartMarketAnalysisRequest`, `MarketAnalysisRunTracker.PeriodOf`, `IMarketAnalysisBookingQueries.GetBookingsAsync(from, to)`, the date fields of `MarketAnalysisReport.razor`, `PeriodInvalid` and `NoBookings` texts; review RC-121 |
| 12 | Fill the fake bookings up to 2026-12-31 | `seed-market-data --through 2026-12-31` repeats the 26 months of the file back to back (`MarketSeedExpander`) until the date, with unique booking numbers, and gives each booking the status of today's date (`MarketDataSeeder`): checked out, checked in, confirmed or cancelled. | Yes | US-001.01. Done: 156,686 bookings in 294 rooms in the dev VM; the repeated bookings are not a forecast; an analysis of the last 3 years (55,484 bookings) completed in 10 minutes with `phi4-mini` and was stored; review RC-121 |
| 13 | Move the client to the run API | Replace the call of `POST /analyze?insights=true` by start, state and result of the run API of [MIL-028] (`MarketAnalysisServiceClient`, `IMarketAnalysisServiceClient`), send `X-API-Key`, follow the run id instead of one long request (each call has a short timeout, and `MarketAnalysisService__TimeoutSeconds`, 90000 while the service is synchronous, goes back to a few seconds), map the errors of the contract to the messages of the page; tests against a fake built from the contract file. | No | |
| 14 | Align the result mapper and the sample | Check `MarketAnalysisResultMapper` and `FindingsSeries` against the figures that the new service returns for the nine analyses, and read the `observations` of each analysis; replace `docs/test-data/market-analysis-result.json` by a result of the new service; keep result schema 1.1 and the `schema_version` check. | No | |
| 15 | Replace the deploy files | Remove `deploy/market-analysis/` (the Dockerfile that clones `Notebook_Market_Analysis`, its compose file, env files and toml); point the README, `nfhotel-api.env.example` (`MarketAnalysisService__*`, add the key), the wiki and the release workflow input to the service's own `deploy/docker-compose.yml`; rename the old repository in the documents. | No | |
| 16 | Check and review the move to the run API | Build, format, tests, the release check of the pull request, a run on the dev VM with the real service; reviews against `qc-programming-csharp` and `qc-yaml`; record the RC. | No | |
| 17 | Choose the chart by the data | A pure rule `ChartKindSelector` that gives each series of `FindingsSeries` a kind from its shape: tile (one figure), donut (2 to 4 parts of one whole), ranked bars (more than 4 categories, sorted), columns (ordered bands: lead time, length of stay, number of guests), line (months), heat map (months against years), otherwise the grouped bars of today; the series carries what the rule needs (order, whether the figures are parts of a whole). Tests of each kind and of the limits (4 and 5 parts, 2 and 1 point, months of one year and of several). | No | |
| 18 | SVG charts for the kinds | `DonutChart`, `RankedBarChart`, `ColumnChart`, `LineChart`, `HeatmapChart` and `FigureTile` in `Admin/Shared`, in the style of `GroupedBarChart` (no script library, text alternative, colours with a second cue such as a label); the report page draws each series with the chart of its kind. Component tests (bUnit) for the markup and the empty and one-point cases. | No | |
| 19 | Check and review the charts | Build, format, tests, a look at the page in the browser pane with the sample result of `docs/test-data/`; reviews against `qc-programming-csharp`; record the RC. | No | |
| 20 | Send the number of rooms | `room_count` (the rooms of the database) in the bookings feed and the client of the service, and the occupancy analyses in the report model and the charts (a column chart per month and two columns or two tiles for holiday against other days, because they are two separate rates and not parts of one whole, so no donut). Tests against the fake of the service. | No | |
| 21 | Correlation heat map and captions | `CorrelationHeatmap` in `Admin/Shared` (diverging scale, value in each cell, legend, text alternative) added to the rule of task 17, and a caption under every chart with what it shows, why this kind of chart and the limits, and the AI observation of the chart (matched to the chart by its `series` path, with a clear text when there is none); the texts come from the result where it has them. Component tests. | No | |

---

[BC-001]: ../business-case.md
[US-001]: ../user-stories.md
[MIL-019]: ./mil-019-benchmark-results-api.md
[MIL-004]: ./mil-004-market-analysis-service.md
[MIL-020]: ./mil-020-start-benchmark-run.md
[MIL-028]: ./mil-028-market-analysis-run-api.md
[82ec2b2]: https://github.com/NF-Hotel/NFHotel/commit/82ec2b2acd49a5a76da4093e355397b6615ef9b0
[364b34c]: https://github.com/NF-Hotel/NFHotel/commit/364b34c0d17a512d3bea06d9e01ad839b2a86e77
[5bc5cf0]: https://github.com/NF-Hotel/NFHotel/commit/5bc5cf054789ce61d82e0f429092a4e485ae7c27
[c9f8d5c]: https://github.com/NF-Hotel/NFHotel/commit/c9f8d5c7a01ebdbf4c89ce2ce4a046971d85d694
