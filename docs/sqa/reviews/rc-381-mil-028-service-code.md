# RC-381 Code review of the market analysis service at the gate of MIL-028

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-381 |
| CrossReference | [MIL-028], [MIL-004], [MIL-026], [RC-124], [RC-127], [RC-129], [RC-380], [PRC-001], [QC-PY-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 and S03 | Initial version: review of NF-Hotel/notebook-market-analysis for the gate of MIL-028, at the pin 97ca890 with the fixes merged in d46df37; QC-PY-001 No-Go, QC-YML-001 Go after the fixes; mode autonomous, not independent | [b8c8a9c] |

---

## Artifact Under Review

- Instance reviewed: the code of NF-Hotel/notebook-market-analysis that [MIL-028] asks for (the run API, the nine analyses, the AI insights, the image and the compose file):
  - at 97ca890f58e01bb2270c45b59cee038dd3c7a9d2, the merge of the gate of the MIL-002 of the service, which NFHotel pins since [RC-380] and deploys;
  - with the five commits of pull request 36 of that repository, merged as d46df37cd6d8f0bee14725cb97d8053f0e27f2f7 on 2026-11-15, which this review led to (see "Changes made during the review"). In `src/` they change one comment, so the behaviour reviewed here is the behaviour of the pin.
- Checklist used: [QC-PY-001] (`qc-programming-python`) for `src/app` (71 files) and `tests/`; [QC-YML-001] (`qc-yaml`) for `deploy/docker-compose.yml`, `openapi/market-contract.yaml` and, after the fixes, `.yamllint.yaml`. For criterion 10 of QC-PY-001 also the design documents of the service: `docs/architecture.md` and the eight ADRs in `docs/decisions`.
- Scope: full review of the files above. The Go/No-Go criteria of MIL-028 are scored in RC-382.
- Language and domain: n/a (technical type).
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-028 in its third wave, to the AI in chat ("Du godkender for mig"). The reviewer stands in for S01 and, for the container and the ports, for S03, the approving reviewers that MIL-028 names. The AI made the fixes of pull request 36 and wrote this record.

**Evidence of 2026-11-15, on the committed tree of 5f0e99a (which d46df37 merges), in a dev image with Python 3.13:**

- `ruff check`, `ruff format --check` (186 files), mypy strict (71 source files), `lint-imports` (three contracts kept) and yamllint 1.38.0 (`yamllint -s .`): all clean.
- pytest: 638 passed, coverage 98 % (lines and branches).
- The three tests of 55,484 bookings in a container with `--memory 2g`, the `mem_limit` of the compose file: passed; cgroup `memory.peak` 344,985,600 bytes of 2,147,483,648; `oom_kill` 0.
- `docker compose config -q` (Compose v5.5.0) of `deploy/docker-compose.yml`, with the env example as `market-analysis.env`: exit 0.
- The image builds from the tree alone and runs as `app` (uid 10001) with `--read-only`, `--cap-drop ALL` and `no-new-privileges:true`; it exposes 8000, declares the volume `/data` and imports the app.
- The same tools at 97ca890, before the fixes: all clean, 632 passed, 98 %; yamllint was neither configured nor run.

## Checklist Results

### QC-PY-001: `src/app` and `tests/`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PEP 8 casing | Pass | ruff selects `N` (pep8-naming): clean |
| 2 | Names state purpose; no unexplained abbreviations or single letters outside tiny scopes | Pass | The names follow the analyses and the run (`StartRun`, `ExecuteRun`, `ModelInsightProvider`, `FileRunStore`, `ThreadTaskRunner`). Single letters only in comprehensions, in short tests, and in `easter_sunday` (`domain/holidays.py`), whose docstring names the published algorithm that has those variables |
| 3 | Formatter and linter, no unexplained suppressions | Pass after the fix | At 97ca890 seven suppressions gave no reason: six `ANN401` (`holidays.py:30`, `booking_factory.py:28`, `run_fakes.py:98` and `:112`, two in `test_market_analysis_api.py`) and one `UP017` in `test_market_result.py`. 5f0e99a gives each `ANN401` its reason and uses `datetime.UTC` instead of the `UP017`. ruff check and format are clean |
| 4 | Every signature annotated | Pass | ruff `ANN` over `src` and `tests`, and mypy strict over `src`: clean |
| 5 | No bare `except`, no swallowed exceptions, the cause kept | Fail | No bare `except`. But nine `raise ... from None` drop the cause: `lm_studio.py:26`, `ollama.py:19`, `market_analysis.py:116`, `model_insights.py:172`, `:255` and `:258`, `config.py:87`, `:110` and `:276`. Two log it first (`model_insights.py:172` and `:258`), and `:255` turns a timeout into the state `TIMEOUT`; the other six keep no trace of it and give no reason. `FileRunStore.load_result` returns `None` for a result file that cannot be read or is not JSON, as for a run without a result, and logs nothing (`run_file_store.py:85-86`); the holiday store does the same for a damaged calendar (`holiday_file_store.py:34-36`), which a comment explains (a new calendar is made) but nothing logs. `lm_studio.py:32` calls `.get` on the first choice without checking that it is an object, so such an event raises `AttributeError`, not the `ModelFailedError` of the port (F3) |
| 6 | No mutable defaults, no shadowed builtins | Pass | ruff `B` and `A`: clean |
| 7 | Files, locks and connections in context managers | Pass with note | ruff `SIM115` is clean; files are read and written whole with `Path.read_text` and `Path.write_text`. The thread pool of `ThreadTaskRunner` has a `shutdown` (`thread_task_runner.py:18`) that nothing calls (F7) |
| 8 | Docstrings (Optional) | Fail, Optional | Every module has one; 152 of the 274 public classes, functions and methods have none (F11) |
| 9 | `logging`, not `print`; no secret or personal data in the log | Pass with note | No `print` (ruff `T20`); the log calls name analyses, run ids and failure reasons, and no secret or booking. `configure_logging`, which sets the format and redacts fields that look like secrets, is called only by its tests, so the redaction is not in effect in the service (F5) |
| 10 | Classes and operations trace to the DCD; deviations recorded | Fail | The service has no DCD. `docs/architecture.md` describes the notebook design of the old service (`MarimoExecutor`, `RunNotebook`, `JobStore`, `FileJobStore`, `PdfRenderer`, `MarketDataReader`, `AnalyzeMarket`) and names none of the classes of the code (`StartRun`, `ExecuteRun`, `ModelInsightProvider`, `FileRunStore`, `ThreadTaskRunner`) or of its ports (`RunStore`, `TaskRunner`, `ModelClient`, `InsightProvider`); the eight ADRs of `docs/decisions` say "Status: Proposed", and ADR-001 and ADR-005 run notebooks as subprocesses. The settings `DATA_SOURCE` and `MARKET_DATA_*` are validated but read by no code (F1, F13) |
| 11 | Tests for new behaviour, named for it, independent of order and network | Pass after the fix, with note | 638 tests named for the behaviour; the model and the HTTP calls are faked (`respx`, fake clients). At 97ca890 two tests depended on their surroundings: the holiday shape test read the calendar of the years around today and would fail from 1 January 2027, and the route test of the contract built the app on `./output`; 89e0d17 fixes both. The 15-second limit of one load test depends on the speed of the machine (F8) |
| 12 | mypy strict; `Any` justified (Optional) | Fail, Optional | mypy strict is clean; 15 of the 20 lines of `src` that use `Any` give no reason, nor does the `type: ignore[arg-type]` of `price_suggestions.py:111` (F11) |
| 13 | Dependencies declared and pinned, none unused (Optional) | Fail, Optional | The image installs `requirements.lock` with hashes, but `pyproject.toml` declares `polars`, `marimo`, `reportlab` and `matplotlib`, which no code imports, and gives the dev tools lower bounds only (F11) |
| 14 | Each piece of knowledge in one place | Fail | "At least one booking" is checked in `StartRun` (`runs.py:59`) and in `analyse_market` (`analyse_market.py:56`); a `room_count` below 1 in `runs.py:60` and in the occupancy analysis (`occupancy.py:79`); the status `"available"` is defined in `findings.py:37` and again in `overall_insight.py:21` (F4) |
| 15 | Imports point inward, no cycles | Pass | `lint-imports`: the layers point inward, the adapters are independent of each other, and the domain and the application import no framework and no dataframe library |
| 16 | SOLID | Pass with note | The use cases receive their ports from the composition root (`infrastructure/main.py`); `ExecuteRun` builds its fallback `PriceExplainer` itself (`runs.py:82`) instead of receiving it (F9) |
| 17 | The folders of the clean architecture structure | Fail | `src/app` has `domain`, `application`, `adapters` and `infrastructure`: the delivery code is in `adapters/api`, not in a `presentation` package, and ADR-001, which chose `adapters`, is still Proposed. `tests/integration` holds only `__init__.py` and `tests/e2e` is empty, while the file stores, the AI adapters and the API are tested in `tests/unit`; `adapters/data`, `adapters/reports` and `tests/unit/application/ports` are empty packages (F2) |
| 18 | File and folder names | Pass | `git ls-files`: English names, no space and no other character |

### QC-YML-001: `deploy/docker-compose.yml`, `openapi/market-contract.yaml` and `.yamllint.yaml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Safe loader, no duplicate keys | Pass | The tests read both files with `yaml.safe_load`; the `key-duplicates` rule of yamllint is clean |
| 2 | 2 spaces, LF, one final newline, no trailing spaces | Pass after the fix | At 97ca890 the compose file had CRLF line endings; e052947 stores it with LF and `.gitattributes` keeps every text file LF; yamllint is clean |
| 3 | yamllint with its committed configuration | Pass after the fix | At 97ca890 the repository had no yamllint configuration and did not run it. `.yamllint.yaml` (the defaults, line length 100 as for ruff) and the dev dependency are added, and the README and the wiki name the check; yamllint 1.38.0 `-s .` exits 0 |
| 4 | Coercible scalars quoted | Pass | Nothing was coerced at 97ca890: `openapi: 3.1.0` and `version: 1.2.0` have two dots and stay strings, and `schema_version` lists `"1.2"` and `"1.3"` quoted. e052947 quotes the two versions as well |
| 5 | Only safe, plain types | Pass | No tag in either file |
| 6 | No secret in the file | Pass | The compose file names the env file, and the contract names the header `X-API-Key`. The env files of `deploy/` were not ignored by git, only the root `.env`; e052947 ignores `deploy/*.env` in git and Docker. No env file other than the examples was ever committed |
| 7 | Embedded program text of at most five lines | N-A | None |
| 8 | A repeated block defined once | Pass after the fix | At 97ca890 the contract wrote the year schema three times, the 422 response twice, the limitations three times and the recommendation twice. e052947 defines each once in `components`; after every `$ref` is resolved the paths and every schema are the same as before, and the same valid and invalid instances get the same verdict (jsonschema, Draft 2020-12) |
| 9 | Anchors and aliases only where supported | N-A | None |
| 10 | Validates against the schema of the tool (Optional) | Pass | `docker compose config -q` (v5.5.0) exits 0; the tests validate results against the contract with jsonschema |
| 11 | Header comment | Pass after the fix | The contract had none at 97ca890; e052947 adds one (what the file is, who reads it, where its plan is). The compose file and `.yamllint.yaml` have one |
| 12 | Key names follow the tool | Pass | Compose keys and OpenAPI keys as the tools define them |
| 13 | Block scalars (Optional) | Pass | The long descriptions are folded scalars (`>-`) |
| 14 | One extension style (Optional) | Fail, Optional | `docker-compose.yml` uses `.yml`, the other two `.yaml` |
| 15 | Exercised by a check without the real target (Optional) | Pass | `test_contract.py`, `test_contract_schema.py` and `test_market_result.py` read the contract; `test_deploy_files.py` reads the compose file |

## Changes made during the review

The five commits of pull request 36 of the service, merged as d46df37:

- edaa521: `.gitattributes` (`* text=auto eol=lf`) and `.editorconfig`; `src/app/infrastructure/logging.py` and five prompt logs renormalized from CRLF (line endings only).
- 89e0d17: the test of 55,484 bookings runs once in a module fixture with insights on, the real `ModelInsightProvider` and `FakeModelClient`, and checks the time, the state, the count and an insight for every available analysis; a second test compares the peak resident set of the test process with the `mem_limit` of the compose file. `tests/unit/test_deploy_files.py` checks the compose service and the Dockerfile (no host port, read-only, no capabilities, no new privileges, a memory limit, a user that is not root, no fetch of another repository). The holiday shape test and the route test no longer depend on the date and on `./output`.
- e052947: `.yamllint.yaml` and yamllint as a dev dependency; the contract with a header comment, the document start, quoted versions, folded lines, version 1.3.0 (its result schema already had 1.3) and its repeated blocks in `components`; `deploy/*.env` ignored by git and Docker; the wiki names the schema `PriceSuggestions`, as the contract does.
- 7d5909c: the answer schema test resolves the shared `Recommendation` schema; it failed after e052947.
- 5f0e99a: every `noqa` gives its reason.

## Findings

- **F1, Major (maintainability): the design of the service is not recorded** (criterion 10). The only design document describes the old notebook service, no DCD exists, and every ADR is Proposed, so no class of the code traces to a design and no deviation is recorded.
- **F2, Major (maintainability): the folders deviate from the clean architecture structure without an accepted decision** (criterion 17): no `presentation` package, no integration or end-to-end tests in their folders, and three empty packages.
- **F3, Minor (reliability): exceptions lose their cause or are silent** (criterion 5): six `from None` without a reason or a log, two stores that turn a damaged file into "nothing there" without a log, and an `AttributeError` from an event of LM Studio.
- **F4, Minor (maintainability): three rules are written twice** (criterion 14): at least one booking, at least one room, and the value `"available"`.
- **F5, Minor (security, defence in depth): the redaction of secrets in the log is not in effect.** `configure_logging` is never called by the service, so its JSON format and its redaction of fields such as `token` or `api_key` do not apply. No log call was found that writes a secret.
- **F6, Minor (reliability): a pricing threshold of `1e999` is a 500, not a 400.** `Fraction('Infinity')` raises an unhandled `ValueError` (`price_suggestions.py:72`), reproduced in the review. The request needs the API key, and the answer shows nothing of the server.
- **F7, Minor (reliability): the run executor is never shut down.** The app has no lifespan hook, so a run in progress is neither awaited nor stopped by the app when the server stops.
- **F8, Minor (reliability): the 15-second limit of `test_a_run_of_55484_bookings_finishes_in_a_few_seconds` depends on the speed of the machine.** It failed three times under coverage at 23 s while the machine was slow, and took 7 s under coverage and 3 s without once it was idle. The test of MIL-028 criterion 7 (60 seconds) passed in every run.
- **F9, Note (maintainability): `ExecuteRun` builds its fallback `PriceExplainer` itself** (`runs.py:82`, criterion 16).
- **F10, Note (security): `/openapi.json` is served without the key** (`docs_url` and `redoc_url` are off, `openapi_url` is not). The same contract is public in the repository, and the service has no host port.
- **F11, Note (the Optional criteria 8, 12 and 13):** the missing docstrings, the `Any` and the `type: ignore` without a reason, and the unused and unpinned dependencies.
- **F12, Note (maintainability): the README says that the insights are written "all in one request per analysis"**; the service asks once per chart since its MIL-001 criterion 11.
- **F13, Note (maintainability): `DATA_SOURCE` and the `MARKET_DATA_*` settings are read by no code.** With the default `api` the service refuses to start without `MARKET_DATA_API_URL`, so the env example sets `DATA_SOURCE=csv`.
- **F14, Note: two records of the service score evidence that does not hold.** RC-018 of the service (the gate of its MIL-002) passes its row 7, 55,484 bookings with the fake provider, on `test_a_run_of_55484_bookings_completes_in_under_60_seconds`, which posted `insights=false`, so no provider ran; 89e0d17 replaces that test. RC-015 to RC-017 of the service score criterion 10 "Pass, on condition", a status that QC-PY-001 does not have. The records of the service are not changed by this review.

## Overall Verdict

QC-PY-001: No-Go. The Mandatory criteria 5, 10, 14 and 17 fail. Criteria 10 and 17 need decisions, not only code: the design of the service recorded (a DCD or an architecture description of the classes it has, and its ADRs decided) and its folder structure decided (a `presentation` package, or an accepted ADR for `adapters`, and integration tests in `tests/integration`). No task of [MIL-028] plans that work, so it is left to S01: a milestone for the rework of the service, or a risk exception, which the stand-in may not approve ([PRC-001]). Criteria 5 and 14 are small fixes of the code that belong with that rework.

QC-YML-001: Go. Every Mandatory criterion passes after pull request 36; criteria 2, 3, 8 and 11 failed at 97ca890 and are fixed there.

The reviewer is not independent: the AI made the fixes and wrote this record, as stand-in for S01 and S03 (mode autonomous, not independent).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide how QC-PY-001 is to be met: a milestone for the rework of the service (criteria 5, 10, 14 and 17, F5 to F13), or a risk exception | S01 | Before the gate of MIL-028 is checked again |
| Copy `openapi/market-contract.yaml` of the service to `docs/test-data/market-contract.yaml` of NFHotel when the pin moves past d46df37, or the pin check of MIL-026 fails | AI as stand-in for S01 | With the next move of the pin |
| Correct row 7 of RC-018 and the status of criterion 10 in RC-015 to RC-017 of the service (F14) | S01 | With the rework |

---

[MIL-028]: ../../milestones/mil-028-market-analysis-run-api.md
[MIL-004]: ../../milestones/mil-004-market-analysis-service.md
[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[RC-124]: ./rc-124-mil-028-market-analysis-run-api.md
[RC-127]: ./rc-127-mil-028-nine-analyses.md
[RC-129]: ./rc-129-mil-028-timeouts.md
[RC-380]: ./rc-380-mil-026-service-pin.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[b8c8a9c]: https://github.com/NF-Hotel/NFHotel/commit/b8c8a9c1ec6cde722a4d6aeb89648fd7c743bfd8
