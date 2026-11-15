# RC-382 Gate review of MIL-028 Market analysis run API in the market analysis service

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-382 |
| CrossReference | [MIL-028], [MIL-004], [MIL-026], [MIL-071], [RC-124], [RC-127], [RC-129], [RC-380], [RC-381], [PRC-001], [QC-MIL-001], [QC-PY-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Deprecated | mikemike | AI as stand-in for S01 and S03 | Initial version: gate of MIL-028 on the service at the pin 97ca890 with the fixes merged in d46df37; No-Go for now, criterion 9 fails on QC-PY-001 (RC-381); mode autonomous, not independent | [b8c8a9c] |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 and S03 | The verdict: a risk exception is approved by S04, the escalation role, never by its owner (PRC-001); it said that S01 grants one; mode autonomous, not independent | [9da77ab] |

---

## Artifact Under Review

- Instance reviewed: [MIL-028] at its gate. It covers the nine Go/No-Go criteria, with this evidence:
  - the code of NF-Hotel/notebook-market-analysis at 97ca890, the commit that NFHotel pins and deploys ([RC-380]), and the five commits of pull request 36 of that repository, merged as d46df37 on 2026-11-15, which change the tests, the YAML files and one comment in `src/`;
  - the code review of both, with the runs of 2026-11-15 (the tools, the 638 tests, the load tests under the memory limit, the image), in [RC-381].

  Before the gate: [RC-124] (the plan, Go), [RC-127] (the nine analyses, Go) and [RC-129] (the two time limits, Go).
- Checklist used: the Go/No-Go criteria of [MIL-028]. [QC-MIL-001] criterion 2 makes them the checklist of the gate.
- Scope: the gate. The service has moved on since the plan, in milestones of its own: its MIL-001 (the plan of this milestone in that repository) raised the result to schema 1.2 (its criterion 10) and asks the model once per chart instead of once per analysis (its criterion 11), and its MIL-002, the price suggestions of [MIL-071], raised it to 1.3. Each version adds fields. The criteria are scored on what they ask, with these changes noted.
- Language and domain: en / it (the Metadata rows of MIL-028). The gate adds no row to the milestone. This record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the gate of MIL-028 in its third wave, to the AI in chat ("Du godkender for mig"). The reviewer stands in for S01 and, for the container and the ports, for S03. The AI made the fixes of pull request 36 and wrote this record.

## Checklist Results

| # | Criterion (MIL-028) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The contract describes the three run paths and result schema 1.1, and a test fails when the implementation and the contract differ | Go, with notes | `openapi/market-contract.yaml` has `POST /api/v1/market-analysis/runs`, `GET .../runs/{id}` and `GET .../runs/{id}/result` (`test_contract_describes_the_run_paths`). `test_every_contract_path_is_a_route` fails when a path of the contract has no route, and `test_the_result_validates_against_the_contract` and `test_a_run_with_pricing_gives_a_result_that_the_contract_accepts` fail when a result does not match the schema. The schema is 1.3 now (see Scope). Not compared: a route that the contract does not have (the check goes one way), the request models, and the bodies of the state and of a problem |
| 2 | A start returns at once with a run id; the state follows `queued`, `running`, `completed`; a second start while a run is active is refused with a stable code | Go, with notes | `test_start_returns_202_at_once_with_the_run_id_and_state_queued`, `test_the_state_follows_queued_running_completed`, and `test_a_second_start_while_a_run_is_active_is_refused_with_run_active` (409, code `run_active`, one run stored). The second start of both tests comes while the first run is queued; none starts one while a run is running. The bookings are those of the test factory of the service: the sample of NFHotel is a result, not bookings |
| 3 | The result has the nine analyses; each figure is compared with a hand calculation, among them an occupancy of 50 %, an occupancy on holidays that differs, and a correlation of +1 and -1 | Go, with notes | The hand calculations are tests of the domain functions, among them `test_one_room_taken_for_the_whole_period_of_two_rooms_is_fifty_percent`, `test_occupancy_on_a_holiday_differs_from_occupancy_on_other_days`, `test_spearman_is_plus_one_for_series_that_move_together` and `test_spearman_is_minus_one_for_series_that_move_apart`; the run tests check the nine analyses of a completed run. Schema 1.3, not 1.1 (see Scope) |
| 4 | The stall timeout and the total limit; with a fake AI an insight and observations for every analysis, their numbers found in the series; `GUARDRAIL_REJECTED` for that analysis only; no insight and no AI call with insights off; 32768 maximum output tokens | Go, with notes | `test_a_model_without_a_token_for_the_stall_timeout_times_out_for_that_analysis_only` and `test_a_model_that_keeps_sending_tokens_is_not_stopped_before_the_total_limit` (a fake model and a fake clock each); `test_evidence_with_numbers_of_the_series_is_in_the_series` and `test_a_chart_text_with_a_cause_a_promise_or_a_figure_not_in_the_series_is_rejected`; `test_a_rejected_analysis_does_not_touch_the_others_or_the_numbers`; `test_insights_false_gives_no_insight_key`; the requests carry 32768 (`test_model_insights.py`, and `num_predict` and `max_tokens` in the tests of the two adapters), and the setting reads `AI_MAX_OUTPUT_TOKENS` (`test_config.py`). Since d46df37 the run of 55,484 bookings also checks an insight for every available analysis with the fake AI. Not tested: the hand-over of the setting in the composition root (`main.py:49`). The model is asked once per chart, not in one request per analysis as the deliverable says (see Scope) |
| 5 | A missing or wrong `X-API-Key` gives 401 on every run path; `/health` needs no key | Go | `test_every_run_path_needs_the_key`, `test_health_needs_no_key` and `test_a_missing_key_wins_over_an_invalid_body`; the key is compared in constant time (`hmac.compare_digest`) |
| 6 | An invalid booking, an unknown field type or an empty list gives 400 with a stable code, and nothing is stored | Go | `test_an_invalid_body_is_400_invalid_request_and_nothing_is_stored`, `test_a_body_that_is_not_json_is_400` and `test_pricing_out_of_range_is_refused_with_400_and_nothing_is_stored`. A pricing threshold of `1e999` gives 500 instead of 400 ([RC-381] F6) |
| 7 | 55,484 bookings with the fake AI in under 60 seconds, and the container within the `mem_limit` of its compose file, measured in the same test run | Go, after d46df37 | At 97ca890 the criterion was not met: the test posted `insights=false`, so no AI code ran, and nothing measured memory. Since 89e0d17 the run is made with insights on and the fake AI, and a second test of the same run compares the peak resident set with the `mem_limit` (2g). In a container with `--memory 2g`: the run took 3.7 s, both tests passed, cgroup `memory.peak` 344,985,600 bytes, `oom_kill` 0. The code of `src/` is the same at the pin, so the result holds for the deployed service |
| 8 | The image builds from the repository alone, publishes no host port and runs as a non-root user with a read-only file system | Go | The image builds from the committed tree with the package index only; it runs as uid 10001 with `--read-only`, `--cap-drop ALL` and `no-new-privileges:true`; the compose service exposes 8000 and publishes no port. Since 89e0d17 `tests/unit/test_deploy_files.py` checks these settings and that the Dockerfile fetches no other repository |
| 9 | Tests pass, the linters are clean, and the `qc-programming-python` and `qc-yaml` reviews are Go | No-Go | 638 tests pass; ruff, mypy strict, import-linter and yamllint are clean; [QC-YML-001] is Go. [QC-PY-001] is No-Go in [RC-381]: criteria 5, 10, 14 and 17 fail |

## Findings

- **Criterion 9 fails.** The design of the service is not recorded (QC-PY-001 criterion 10) and its folders deviate from the clean architecture structure without an accepted decision (criterion 17); criteria 5 and 14 are small fixes of the code. Meeting criteria 10 and 17 needs decisions on the design and the structure of the service, which no task of MIL-028 plans, so the AI stops there: a milestone for the rework or a risk exception is S01's decision, and the stand-in may not approve an exception ([PRC-001]).
- **Criterion 7 was not met until this gate.** The test that the gate of the MIL-002 of the service cited ran without the AI ([RC-381] F14). The fix is merged in the service; the pin of NFHotel need not move for it, because the code of `src/` is unchanged.
- **The service has no gate record of its own MIL-001**, the milestone that plans this work in that repository; its reviews RC-003 to RC-012 are of the plan.
- **The target date, 2026-11-13, has passed.**

## Overall Verdict

No-Go for now. Criteria 1 to 8 pass, criterion 7 after the fixes of d46df37; criterion 9 fails on QC-PY-001 ([RC-381]). [MIL-028] is unchanged until a review of the service gives QC-PY-001 Go or S04, the escalation role, approves a risk exception that S01 asks for ([PRC-001]); then the gate is checked again. MIL-028 has no synced issue to keep open.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Decide how QC-PY-001 of the service is to be met ([RC-381]): a milestone for the rework, or a risk exception | S01 | Before the gate of MIL-028 is checked again |
| Check the gate of MIL-028 again after that decision | AI as stand-in for S01 | After S01's decision |
| Record the gate of the MIL-001 of the service | S01 | With the rework |

---

[MIL-028]: ../../milestones/mil-028-market-analysis-run-api.md
[MIL-004]: ../../milestones/mil-004-market-analysis-service.md
[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[MIL-071]: ../../milestones/mil-071-price-suggestions-from-the-service.md
[RC-124]: ./rc-124-mil-028-market-analysis-run-api.md
[RC-127]: ./rc-127-mil-028-nine-analyses.md
[RC-129]: ./rc-129-mil-028-timeouts.md
[RC-380]: ./rc-380-mil-026-service-pin.md
[RC-381]: ./rc-381-mil-028-service-code.md
[PRC-001]: ../../../framework/process/review-checklist-process.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-PY-001]: ../../../framework/qc/code/qc-programming-python.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[b8c8a9c]: https://github.com/NF-Hotel/NFHotel/commit/b8c8a9c1ec6cde722a4d6aeb89648fd7c743bfd8
[9da77ab]: https://github.com/NF-Hotel/NFHotel/commit/9da77ab7a72173754403f906237c210c1874d44c
