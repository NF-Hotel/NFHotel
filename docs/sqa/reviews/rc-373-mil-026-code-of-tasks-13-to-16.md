# RC-373 Review of the code of MIL-026 tasks 13 to 16 against QC-CS-001 and QC-YML-001

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-373 |
| CrossReference | [MIL-026], [QC-CS-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: the review of the merged code of tasks 13 to 16; No-Go; mode autonomous, not independent | [b371b85] |

---

## Artifact Under Review

- Instance reviewed: the code of [MIL-026] tasks 13 to 15 as it is merged on `main` at `9de9be4`, read in the worktree of the branch `mil-026-gate`; task 16 is this review.
  - Task 13, the move to the run API: `MarketAnalysisServiceClient` and `MarketAnalysisServiceOptions` (`src/NFHotel.External`), the port `IMarketAnalysisServiceClient`, the request DTOs `MarketAnalysisRunRequest` and `MarketBookingDto`, `AddMarketAnalysisService` in `src/NFHotel.External/DependencyInjection.cs`, the codes of `ErrorCodes.MarketAnalysisRunner` with their texts in `src/NFHotel.Web/Common/ErrorMessages.cs`, and the run in `MarketAnalysisRunner`.
  - Task 14, the mapper and the sample: `MarketAnalysisResultMapper`, `FindingsSeries`, `MarketAnalysisMatrixDto`, `MarketAnalysisObservationDto`, the sample `docs/test-data/market-analysis-result.json` and the contract copy `docs/test-data/market-contract.yaml`.
  - Task 15, the deploy files: `ops/deploy/market-analysis/` (`docker-compose.yml`, `Dockerfile`, `market-analysis.env.example`, `README.md`) and the `MarketAnalysisService__*` settings of `ops/deploy/nfhotel-api/` (its env example and the secret file of its compose file).
  - The tests: `MarketAnalysisServiceClientTests`, `FakeMarketAnalysisService`, `ManualTimeProvider`, `MarketContractFile`, `MarketContractTests`, `MarketAnalysisResultMapperTests` and `FindingsSeriesTests` (`tests/NFHotel.External.Tests/MarketAnalysis/`), `MarketAnalysisRunnerTests`, `ErrorCodeTextTests` and the failure tests of `MarketAnalysisViewModelTests`.
- Commits that built the tasks: `9b517b0` (2026-10-29, tasks 13 and 14, pull request #406, merged as `31ccd4c`); `5f8e3a3` (2026-10-29, task 15, pull request #400, merged as `241a734`) with its linter fix `dce582c`; `df900e3` (2026-10-30, the service pinned to its release with the AI insights, pull request #408, merged as `055530f`). No commit names task 16: its review was not recorded before this record. Later commits that changed the same files are part of what is reviewed: `ac89d0e` (the env files split), `052070c` (the network `nfhotel-python`, [ADR-0039]), `dfa565a` and `5756e58` (LINQ in the mapper), `3ebb51e` ([MIL-071] task 2: the `pricing` block and the contract copy 1.2.0) and `ec47437` (an explicit type in the runner).
- Checklist used: [QC-CS-001] (`framework/qc/code/qc-programming-csharp.md`, 21 criteria) for the C# code, and [QC-YML-001] (`framework/qc/code/qc-yaml.md`, 15 criteria) for `ops/deploy/market-analysis/docker-compose.yml`
- Scope: full review of the files above as they stand; where a count is given, it names the lines that the task commits wrote. The review also judges the Go / No-Go criteria 14 to 17 of [MIL-026]. Not reviewed against a checklist: the contract copy (a copy of the service's own file), the `Dockerfile`, the env files and the `README.md`; they are read for the criteria 15 to 17 and for the findings.
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: autonomous, not independent — the plan of 2026-11-14 was accepted by the AI as stand-in for S01 at S01's word in chat; the same AI built the code in October.

### Checks that were run

| Check | Result |
| --- | --- |
| CI of `main` at `9de9be4` (run 43621113638, NF Hotel CI), job Build: `dotnet build NFHotel.sln --configuration Release --no-restore`, with warnings as errors and the XML documentation generated for every project under `src/` | 0 Warning(s), 0 Error(s) |
| Same run, job Formatting: `dotnet format whitespace NFHotel.sln --verify-no-changes --no-restore` | passed; it checks white space only, not style rules such as `var` |
| Same run, job Tests and coverage | total 2689, failed 0, succeeded 2676, skipped 13 (the E2E tests, which need `E2E_BASE_URL`) |
| Ops CI of `9de9be4` (run 43621113694), job Ops linters: `ops/tests/lint.sh`, which runs yamllint 1.35.1 with `-s -c ops/.yamllint` over every tracked YAML file of `ops/` and `.github/` | all linters clean |
| Same run, job Ops service networks (MIL-061): `ops/tests/test-service-networks.sh` renders the compose file with `docker compose config` and the tracked example settings | PASS: the file is read; the service is on the networks of the wiki table and publishes no port |
| Same run, job Ops monitoring configuration (MIL-023): every compose file against the container profiles of MIL-080 | PASS: the service matches its profile |
| Release 43621604033 of `9de9be4`, and the check "Release build (no push)" of the pull requests #400, #406 and #408 | success: the image of the service is built, from the pinned commit of `NF-Hotel/notebook-market-analysis` |
| Greps over the reviewed files (`var`, `!`, `#region`, `#pragma`, `SuppressMessage`, `.Result`, `.Wait(`, `async void`), each hit traced to its commit with `git blame` | see the rows 2 to 4, 6 and 21 of [QC-CS-001] |
| Blob hashes of the sample and the contract here and in `NF-Hotel/notebook-market-analysis` (`gh api`) | the contract copy (`773e4584`) equals the service's `openapi/market-contract.yaml` on its `main` (version 1.2.0); at the pinned commit `2868898` the contract is another file (`0a5d6b95`, version 1.1.0); the sample here (`b8feaa41`) differs from the sample that the service's contract test reads (`15cbbb47`) |
| The service at the pinned commit (`src/app/adapters/api/v1/market_analysis.py`, read with `gh api`) | `RunRequestModel` holds `room_count`, `period` and `bookings` only, on a base model with `extra="forbid"` (F5) |
| Line endings and white space of the compose file | 0 CR, no tab, no trailing space, one LF at the end |
| A run on the dev VM with the real service (task 16) | Not checked: this review does not reach the VM. Pull request #406 says that the page was not run against the live service in its session |

## Checklist Results

### QC-CS-001: the C# code

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | PascalCase, camelCase, `_camelCase`, `I` prefix | Pass | The build is clean; `MarketAnalysisServiceClient`, `IMarketAnalysisServiceClient`, `_httpClient`, `_options`, `_time`, `RunsPath`, `MaxConsecutivePollFailures`, `CountRoomsAsync` |
| 2 | `Async` suffix; `Exception` suffix; one top-level type per file | Pass | `AnalyzeAsync`, `StartAsync`, `WaitAsync`, `GetStateAsync`, `ReadResultAsync`, `FailAsync`; each reviewed file holds one top-level type (the bodies of the run API are private records nested in the client); no new exception type |
| 3 | `.editorconfig`, no unexplained suppressions; locals have an explicit type, never `var` | Fail | Under `src/`, `9b517b0` wrote `var` in 29 lines of `MarketAnalysisServiceClient.cs` (`3ebb51e` one more), 3 of `MarketAnalysisResultMapper.cs` (`3ebb51e` one more; 16 older lines), `DependencyInjection.cs:122` (`out var seconds`) and `ErrorMessages.cs:212` and `:219`; the runner's `var rooms` (line 84) is the room count of task 20 and is counted in [RC-375]. The tests of `9b517b0` use `var` throughout (56 lines in `MarketAnalysisServiceClientTests`, 27 in `MarketAnalysisResultMapperTests`, 11 in `FindingsSeriesTests`, 14 in `MarketContractFile`, 13 in `FakeMarketAnalysisService`); `var` in tests waits for S01's decision on F3 of [RC-266]. `.editorconfig:106-108` sets `csharp_style_var_*` to `false:suggestion`, so neither the build nor the white-space check stops it. No `#pragma` and no suppression attribute in the reviewed files (F1) |
| 4 | Nullable enabled; `!` justified in a comment | Fail | Nullable is on (`Directory.Build.props`). `MarketAnalysisResultMapper.cs:340` (`9b517b0`) reads `item.GetString()!` after a filter on `JsonValueKind.String`, with no comment. In the tests `9b517b0` wrote 24 `!` without a comment (16 in `MarketAnalysisResultMapperTests`, 4 in `MarketAnalysisServiceClientTests`, 3 in `FakeMarketAnalysisService`, such as `request.RequestUri!` and `request.Content!`, 1 in `MarketAnalysisRunnerTests`), and `3ebb51e` 5 more. `baseUri = null!` in `MarketAnalysisServiceOptions` carries its comment (F2) |
| 5 | `IDisposable` disposed | Pass | Every `HttpRequestMessage`, `HttpResponseMessage` and `JsonDocument` of the client is in a `using`, also on the paths that return early; the `HttpClient` comes from the factory of `AddHttpClient`. The tests create an `HttpClient` over the in-memory fake with `disposeHandler: false` and do not dispose it; it holds nothing outside the process |
| 6 | `await` throughout; no `.Result`, `.Wait()`, `async void` | Pass | The grep finds none; `ConfigureAwait(false)` on every await of the client and the runner; the poll waits with `Task.Delay(interval, _time, cancellationToken)` |
| 7 | Specific exceptions; arguments validated; `throw;`; no empty `catch` | Pass | `ArgumentNullException.ThrowIfNull` in the constructor and in `AnalyzeAsync`; a refused or failed call is a `Result` failure with a code, not an exception; each `catch` is narrow (`HttpRequestException`, `JsonException`, `OperationCanceledException` only when the caller did not cancel, transient errors through a `when` filter) and returns a code. The comment-only `catch (UniqueConstraintViolationException)` of the runner (lines 135 to 138) is from `1fd620f`, before these tasks |
| 8 | Public types and members have XML documentation | Pass | Every project under `src/` generates its documentation with warnings as errors, and the build has 0 warnings; four comments are out of date (F7) |
| 9 | Properties, not fields; immutability | Pass | The DTOs are positional records; the options expose properties; the client keeps `private readonly` fields |
| 10 | Money is `decimal`; instants are `DateTimeOffset` | Pass | `MarketBookingDto.PricePerNight` is `decimal?` and the pricing thresholds and percentages are `decimal`; the deadline of a run is a `DateTimeOffset` from the `TimeProvider`, the run times come from `IClock.UtcNow` (`DateTimeOffset`). `FindingsSeries` reads money figures of the findings as `double` chart values, for drawing only |
| 11 | Public async APIs take a `CancellationToken` | Pass | `AnalyzeAsync` and `CountRoomsAsync` take it and pass it to every HTTP call, to `Task.Delay` and to the query; `AnalyzeAsync_CallerCancels_ThrowsTheCancellation` |
| 12 | Tests cover new behaviour, `Method_Condition_Expected`, no order, time or network | Pass | 27 client tests (the start, the state, the result, the key, the period, the deadline, three failed state calls in a row, each refused status, the cancellation, the cap of 60 seconds), 5 contract tests and the tests of the mapper and the series; the time is a `ManualTimeProvider` and the service an in-memory `HttpMessageHandler`, so no test waits or reaches a network. The summary of `MarketContractTests` calls the contract a byte-identical copy, but no test compares the copies with the service's files (F5, F6) |
| 13 | Classes trace to the DCD; deviations recorded | Fail (known, not new) | `docs/dcd.md` and `docs/dcd.puml` draw the domain package only and name none of the reviewed classes; no deviation is recorded. [RC-270] marked this row the same way for tasks 22 to 27 and its action item is open with S01; [RC-341] and [RC-363] read the row as Pass because the DCD draws only the domain. This record follows [RC-270], the record of the same milestone (F4) |
| 14 | Each piece of knowledge once | Pass | The run paths and the problem code are constants of the client; the cap of 60 seconds is `MarketAnalysisServiceOptions.MaxTimeoutSeconds`, which the registration uses; the texts of the codes are in `ErrorMessages` only, and `ErrorCodes_EveryConstant_HasATextInErrorMessages` keeps the codes and the texts together. The pin of the service is written twice (F8) |
| 15 | References point inward, no cycles | Pass | The port and the DTOs are in Application, the client in External; the architecture tests pass in the CI run |
| 16 | SOLID | Pass | The client has one reason to change, the protocol of the service; the runner depends on the port and gets the client by injection |
| 17 | Layers and folders of the clean architecture | Pass | The port and the DTOs in `NFHotel.Application`, the adapter in `NFHotel.External`, the room query in `NFHotel.Infrastructure`, the texts in `NFHotel.Web` |
| 18 | File and folder names | Pass | English, PascalCase, no space |
| 19 | Observers through `event` | N-A | No observer in the reviewed code |
| 20 | SIMD / CUDA | N-A | No acceleration |
| 21 | Every type under `src/` groups its members in `#region` blocks | Fail | None of the 514 C# files under `src/` holds a `#region`, and the reviewed types follow them. The risk exception of [RC-278] covers only the files of MIL-058 and was approved by the stand-in, which [PRC-001] does not allow, so it cannot carry this record; S01's decision on F4 of [RC-266] is open (F3) |

### QC-YML-001: `ops/deploy/market-analysis/docker-compose.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe loader, no duplicate keys | Pass | yamllint 1.35.1 (its safe parser and the `key-duplicates` rule of the default configuration) and `docker compose config` in two jobs of the Ops CI read the file without error |
| 2 | Two-space indent, LF, final newline, no trailing spaces | Pass | Two-space indent throughout; 0 CR, no tab, no trailing space, one LF at the end |
| 3 | Passes `yamllint` with its committed configuration | Pass | yamllint 1.35.1 with `-s -c ops/.yamllint`, the configuration committed beside the code: clean on `9de9be4`; no `disable` comment in the file |
| 4 | Coercible scalars quoted | Pass | The port `"8000"` is quoted; `read_only: true`, `external: true`, `pids_limit: 256` and `cpus: 2` are meant as their types; no value holds `: ` or ` #` |
| 5 | Only safe, plain types | Pass | No tag |
| 6 | No secret in the file | Pass | The token of the service is in the git-ignored settings file, and its example holds a placeholder; the key that the API sends is a secret file of `ops/deploy/nfhotel-api` and is empty in its example |
| 7 | Embedded program text at most five lines | N-A | No program text in a value; the start command is in the header comment |
| 8 | A repeated block defined once | Pass | The image name is one anchor; nothing else repeats in the file. The pin of the service is the default of one build argument and is repeated in `market-analysis.env.example` (F8) |
| 9 | Anchors only where the tool supports them | Pass | Compose expands YAML anchors and allows `x-` extension fields; `docker compose config` renders the image through the alias |
| 10 | Validates against the schema of its tool | Pass | `docker compose config` (the Compose Specification) in `test-service-networks.sh` and in the container profile check of MIL-080; the Compose version of the runner was not read |
| 11 | A header comment states purpose, tool and decision | Pass | Lines 1 to 7: the service and who calls it, the build from the pinned commit, the networks with [ADR-0039], the settings file and the Compose command that starts it |
| 12 | Key names follow the tool, one case style | Pass | Compose's own keys (`cap_drop`, `env_file`, `mem_limit`); the names the file chooses are kebab-case (`x-analysis-image`, the service); the build arguments are upper-case environment names, as the `Dockerfile` reads them; the labels are the OCI keys |
| 13 | Block scalars for multi-line strings | N-A | No multi-line string |
| 14 | Lowercase kebab-case file name | Pass | `docker-compose.yml`, the name Compose looks for |
| 15 | A check runs without the real target | Pass | `test-service-networks.sh` renders the file with the tracked example settings and shows with stand-in containers that the service is reached from the network of the Python services and not from the networks of the proxy and the API; the release check builds the image on the runner |

## Go / No-Go criteria of MIL-026 (14 to 17)

| # | Criterion | Status | Evidence (test class and method) |
| --- | --- | --- | --- |
| 14 | Start, follow and read a run with `X-API-Key`; a test against a fake built from the contract file passes; no call above 60 seconds | Pass | `MarketAnalysisServiceClientTests`: `AnalyzeAsync_Bookings_StartsTheRunOfTheContract`, `AnalyzeAsync_ApiKey_IsSentOnEveryCallOnlyWhenSet`, `AnalyzeAsync_RunQueuedThenRunning_FollowsTheStateThenReadsTheResult`, `AnalyzeAsync_RunNeverEnds_IsGivenUpAtTheDeadline`, `AddMarketAnalysisService_Timeout_IsNeverAbove60Seconds`; `FakeMarketAnalysisService` checks each body against the `RunRequest` schema of the contract copy, which equals the contract on the service's `main`. The service that the release builds is pinned to an older contract that refuses this body (F5) |
| 15 | 401, 400, a failed run and a service that is down each show a clear message and keep the last report | Pass | The codes: `AnalyzeAsync_StartRefused_ReturnsTheMatchingCode` (401, 403, 400, 422, 409, 500, 502, 404), `AnalyzeAsync_WrongKey_IsUnauthorizedAndTheKeyIsNotLogged`, `AnalyzeAsync_RunFailed_ReturnsTheSanitisedErrorCode`, `AnalyzeAsync_ServiceDown_ReturnsTheUnavailableCode`; the texts: `ErrorCodes_EveryConstant_HasATextInErrorMessages`, `FailureText_RunFailedInTheService_ShowsTheServiceCode`; the last report kept: `ExecuteAsync_FailureAfterASuccess_KeepsTheEarlierReport`, `RefreshAsync_RunFails_StopsFollowingAndKeepsTheOldReport`. Proven by the steps together; no single test follows one refusal from the service to the page |
| 16 | The sample is the file of the service's contract test, or a test fails when they differ | Fail | The sample here (blob `b8feaa41`, written by `9b517b0`) is not the file that the service's contract test reads (`docs/test-data/market-analysis-result.json` of `NF-Hotel/notebook-market-analysis`, blob `15cbbb47` on its `main` and at the pin), which is this repository's sample as it was before `9b517b0`. No test here or there compares the two (F6) |
| 17 | The release check builds the image without fetching another repository; no Dockerfile in `deploy/market-analysis/` clones a repository | Not met (a deviation, by decision) | `ops/deploy/market-analysis/Dockerfile` fetches `NF-Hotel/notebook-market-analysis` at the pinned commit `2868898` (`git fetch --depth 1` of `MARKET_ANALYSIS_REF`, then a checkout of it), as it has since `5f8e3a3` (2026-10-29); task 15 planned to remove the folder instead. The release check builds the image this way and passes (pull requests #400, #406 and #408; Release 43621604033). The gate records it as a deviation |

## Findings

- **F1, Fail on criterion 3 (Mandatory), blocking: `var` in the code of the tasks.** The lines are listed in row 3. They were written on 2026-10-29, when the checklist already forbade `var` (its version of 2026-10-23), and nothing has changed them since.
- **F2, Fail on criterion 4 (Mandatory), blocking: `!` without a comment.** `MarketAnalysisResultMapper.cs:340` under `src/`, and 24 lines of the tests of `9b517b0` (row 4). The mapper's filter makes the `!` safe; the rule asks for the comment that says so.
- **F3, Fail on criterion 21 (Mandatory), blocking: no `#region` in any type under `src/`.** Known for the whole of `src/`, not caused by these tasks; no valid risk exception covers these files.
- **F4, Fail on criterion 13 (Mandatory), blocking, known and not new: the DCD does not draw these classes.** The same finding as [RC-270].
- **F5, High (functional, not security; not a blocking finding by the definition of [PRC-001]): the pinned service refuses the request that the client sends.** Since `3ebb51e` ([MIL-071] task 2, 2026-11-11) `MarketAnalysisRunner` always sends a `pricing` block (the pricing settings service is registered, and its defaults are used when nothing is saved), and the contract copy and the fake are version 1.2.0. The release builds the service at `2868898`, the default of `docker-compose.yml` line 19 and the value of `market-analysis.env.example` line 37: there the contract is version 1.1.0, whose `RunRequest` allows no other property, and the service's `RunRequestModel` forbids extra fields and has no `pricing`. A run against that image is expected to be refused with 400 or 422, which the client maps to `input_rejected`, so no new report is made. Found by reading the service at the pinned commit; not observed on a running service. The tests cannot see it, because the fake is built from the contract copy and not from the contract at the pin. The README still says that a commit with the runs API is enough.
- **F6, Medium: criterion 16 of [MIL-026] is not met.** The sample of this repository and the sample of the service's contract test are different files, and no test compares them. The decision of the criterion is the gate's; this record does not block on it.
- **F7, Low: out-of-date comments.** `MarketAnalysisResultMapper.Map` calls its document "the answer to `POST /analyze`", and the class summary names result schemas 1.0 to 1.2 while the mapper also reads 1.3; `MarketBookingDto` names the deleted repository `Notebook_Market_Analysis` (task 15 asked for the old name to be replaced); the summaries of `ErrorCodes.MarketAnalysisRunner.Unauthorized` (a gateway that asks for a token) and `InputRejected` (status 422 only) predate the run API, which answers 401 or 403 and 400 or 422; `ErrorMessages.Describe` says that it returns Danish texts.
- **F8, Low: the pin of the service is written twice.** The default of the build argument in `docker-compose.yml` (read by the release build) and `MARKET_ANALYSIS_REF` in `market-analysis.env.example` (read through `--env-file` by `deploy-services.sh` and by the start command of the README). The README asks for both to be changed together, while `build-release-images.sh` says that the pinned refs are written once. They are equal today.
- **F9, Low: [ADR-0500] and [ADR-0030] still read as in force.** Both are `Accepted`, but `5f8e3a3` replaced the frozen image and its patch layer by the build from the new repository, the end that decision 4 of [ADR-0500] foresaw; neither record says so, and no ADR records the build from a pinned commit of the service.
- **F10, Low (security): the problem code of a refused call is logged as the service sent it.** `FailAsync` logs `{Problem}` from the answer body without the cleaning that `RunFailedCode` gives the code of a failed run (ASCII letters, digits and `_ . -`, at most 64 characters). The service is on an internal network and the log is structured, so the risk is low.

## Overall Verdict

No-Go. Criteria 3, 4, 13 and 21 of [QC-CS-001] fail, and each is Mandatory, so each is a blocking finding (F1 to F4). No valid risk exception covers them, and the stand-in may not approve one ([PRC-001]). The compose file passes [QC-YML-001]. Of the Go / No-Go criteria of [MIL-026], 14 and 15 pass by their tests, 16 fails (F6), and 17 is not met by decision. F5 is not a blocking finding by definition, but while it stands a run of the released service is expected to be refused. The build has 0 warnings, the white-space check passes and none of the 2676 tests that ran fails; the run on the dev VM of task 16 was not checked.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Give the locals, `foreach` variables and `out` arguments of the client, the mapper, `AddMarketAnalysisService` and `ErrorMessages` their types, and decide F3 of [RC-266] for the tests (F1) | S01 | before the gate of [MIL-026] |
| Explain `MarketAnalysisResultMapper.cs:340` and the `!` of the tests in a comment, or remove them (F2) | S01 | before the gate of [MIL-026] |
| Decide the `#region` rule for `src/` (F4 of [RC-266]): a conversion, a risk exception approved by someone who is neither the author nor the owner ([PRC-001]), or a change of the checklist upstream (F3) | S01 | before the gate of [MIL-026] |
| Bring the DCD up to date or record the classes outside it as a deviation, the open action item of [RC-270] (F4) | S01 | before the gate of [MIL-026] |
| Move `MARKET_ANALYSIS_REF` (compose default and env example) and `requirements.lock` to a service commit whose contract is 1.2.0, with S03 reviewing the image change; add a check that the contract copy equals the contract at the pin (F5) | S01 | before the next release |
| Make the sample of `docs/test-data/` the file of the service's contract test, or add a test that fails when the two differ (F6) | S01 | before the gate of [MIL-026] |
| Record criterion 17 as a deviation at the gate, and mark [ADR-0500] and [ADR-0030] as no longer in force (F9) | S01 | at the gate of [MIL-026] |
| Correct the comments of F7, keep one copy of the pin (F8) and clean the logged problem code (F10) | S01 | with the next change of these files |
| Run the analysis on the dev VM with the real service, after F5 (task 16) | S01 | before the gate of [MIL-026] |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-CS-001]: ../../../framework/qc/code/qc-programming-csharp.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[ADR-0030]: ../../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[ADR-0039]: ../../adr/adr-0039-python-services-network.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[RC-266]: ./rc-266-mil-055-re-review-of-rc-265.md
[RC-270]: ./rc-270-mil-026-code-of-tasks-22-to-27.md
[RC-278]: ./rc-278-mil-058-code.md
[RC-341]: ./rc-341-mil-079-code.md
[RC-363]: ./rc-363-mil-077-code.md
[RC-375]: ./rc-375-mil-026-code-of-tasks-20-and-21.md
[b371b85]: https://github.com/NF-Hotel/NFHotel/commit/b371b851b7cf3d83d587639cf7533afdeaa3fc6b
