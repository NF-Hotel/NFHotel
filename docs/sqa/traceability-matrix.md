# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 |
| CrossReference | [BC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Proposed | mikemike | AI as stand-in for S01 | The rows of MIL-072 and of its code at the gate: the check inside the VM made at S01's word in chat; gate passed (RC-339) | [d727221] |
| 2026-11-14 | Proposed | mikemike | AI as stand-in for S01 | The rows of MIL-080 and of its code at the gate: the monitoring deployed at S01's word in chat, the check over its six containers and the two rules loaded; gate passed (RC-347) | [60b8df3] |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003], [MIL-004], [MIL-005], [MIL-006], [MIL-007], [MIL-008], [MIL-009], [MIL-010], [MIL-021], [MIL-022], [MIL-023], [UCD-001], [US-001], [MIL-055], [MIL-071] | [RC-021], [RC-024], [RC-037], [RC-040], [RC-044], [RC-088] |
| [SA-001] | Stakeholder Analysis | en | it | [BC-001] | - | [RC-009], [RC-026] |
| [MIL-001] | Milestone | en | it | [BC-001] | - | [RC-003] |
| [MIL-002] | Milestone | en | it | [BC-001] | - | [RC-006] |
| [MIL-003] | Milestone | en | it | [BC-001] | - | [RC-010] |
| [MIL-004] | Milestone | en | it | [BC-001], [US-001] | - | [RC-015] |
| [MIL-005] | Milestone | en | it | [BC-001] | - | [RC-018] |
| [MIL-006] | Milestone | en | it | [BC-001] | - | [RC-022], [RC-023] |
| [MIL-007] | Milestone | en | it | [BC-001] | [DM-004] | [RC-025] |
| [MIL-008] | Milestone | en | it | [BC-001], [MIL-005] | - | [RC-038], [RC-039] |
| [MIL-009] | Milestone | en | it | [BC-001], [ADR-0002], [MIL-008] | - | [RC-041], [RC-043] |
| [MIL-010] | Milestone | en | it | [BC-001], [MIL-006] | - | [RC-045], [RC-046] |
| [ADR-0002] | ADR | en | it | [BC-001], [ADR-0001], [DM-004] | [MIL-009] | [RC-042] |
| [ADR-0003] | ADR | en | it | [BC-001], [MIL-004], [US-001] | [MIL-004], [ADR-0010] | [RC-047], [RC-098] |
| [ADR-0001] | Architecture Decision Record | - | - | [BC-001] | [MIL-005] | [RC-019] |
| `src/` (Domain, Application, Infrastructure) | Source code | - | - | [ADR-0001], [MIL-005] | - | [RC-020] |
| [PP-001] | Project Plan | en | it | [BC-001], [SA-001] | [MIL-001], [MIL-002], [MIL-003], [MIL-021], [MIL-022], [MIL-023] | [RC-088] |
| [DSA-001] | CI/CD and DevSecOps Assessment (no catalog type) | en | it | [BC-001], [ADR-0003], [ADR-0006] | [ADR-0007], [ADR-0008], [ADR-0009], [MIL-021], [MIL-022], [MIL-023] | - (no checklist; decisions approved in chat) |
| [MIL-021] | Milestone | en | it | [BC-001], [DSA-001], [ADR-0007], [ADR-0008], [ADR-0010] | [MIL-022] | [RC-089], [RC-094], [RC-095], [RC-097] |
| [MIL-022] | Milestone | en | it | [BC-001], [MIL-021] | [MIL-023] | [RC-090] |
| [MIL-023] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-294) | en | it | [BC-001], [MIL-022], [ADR-0020] | CIS-001, SAMM-001, DORA-001, the host hardening and the firewall of `ops/deploy/host`, the monitoring stack and Alloy of `ops/deploy/monitoring` and `ops/deploy/alloy`, the wiki pages Host-Hardening, Monitoring and Alerts | [RC-091], [RC-169], [RC-172], [RC-176], [RC-177], [RC-179], [RC-189], [RC-191], [RC-192], [RC-193], [RC-244], [RC-505] (drill 1), [RC-284] (drill 2), [RC-294] (gate) |
| [ADR-0007] | ADR (Accepted, amended by ADR-0019, ADR-0021 and ADR-0038) | en | it | [BC-001], [DSA-001], [ADR-0003] | [MIL-021], [MIL-022], [ADR-0038] | [RC-092], [RC-281] |
| [ADR-0008] | ADR | en | it | [BC-001], [DSA-001], [ADR-0004] | [MIL-021], [MIL-022] | [RC-093] |
| [ADR-0009] | ADR | en | it | [ADR-0006], [DSA-001] | [MIL-021], [MIL-023] | [RC-096] |
| [ADR-0010] | ADR | en | it | [ADR-0003], [ADR-0004], [DSA-001] | [MIL-021], [ADR-0011] | [RC-098] |
| [MIL-024] | Milestone | en | it | [BC-001], [ADR-0010], [ADR-0011], [MIL-021] | - | [RC-099], [RC-101] |
| [ADR-0012] | ADR | en | it | [BC-001], [ADR-0003], [ADR-0006] | [MIL-025] | [RC-102] |
| [MIL-025] | Milestone | en | it | [BC-001], [ADR-0012] | - | [RC-103] |
| [ADR-0011] | ADR | en | it | [ADR-0004], [ADR-0010], [DSA-001] | [MIL-024] | [RC-100] |
| [UCD-001] | Use Case Diagram | en | it | [SA-001], [BC-001] | [US-001], [UC-007] | [RC-013], [RC-028] |
| [US-001] | User Story | en | it | [UCD-001], [BC-001] | [MIL-055], [UC-007] (US-001.12) | [RC-012], [RC-027], [RC-263] |
| [DICT-001] | Domain Dictionary | en | it | [BC-001], [SA-001], [UC-007] | [DM-004] | [RC-029], [RC-264] |
| [UC-001] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-001] | [RC-030] |
| [UC-002] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-002] | [RC-031] |
| [UC-003] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-003] | [RC-032] |
| [DM-001] | Domain Model | en | it | [UC-001], [UCD-001], [DICT-001] | [DM-004] | [RC-033] |
| [DM-002] | Domain Model | en | it | [UC-002], [UCD-001], [DICT-001] | [DM-004] | [RC-034] |
| [DM-003] | Domain Model | en | it | [UC-003], [UCD-001], [DICT-001] | [DM-004] | [RC-035] |
| [DM-004] | Domain Model | en | it | [UC-001], [UC-002], [UC-003], [DM-001], [DM-002], [DM-003], [DICT-001], [UC-007] | - | [RC-036], [RC-264] |
| `tests/NFHotel.Proxmox.Tests`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-001] | - | [RC-004] |
| `deploy/`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-002] | - | [RC-007] |
| `deploy/proxmox`, `scripts/pve-token.sh`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-003] | - | [RC-011] |
| `deploy/market-analysis`, `deploy/nginx`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-004] | - | [RC-016] |
| [MIL-055] | Milestone (Accepted, Go; its rule for the suggestions is replaced by [MIL-071]) | en | it | [BC-001], [US-001], [MIL-026], [MIL-028], [MIL-030] | [UC-007], [MIL-071], `docs/wiki/Price-Suggestions.md`, code and tests below | [RC-262] |
| [MIL-058] | Milestone (Accepted, Go) | en | it | [BC-001], [DSA-001], [ADR-0032], [MIL-022], [MIL-044], [MIL-053] | `docs/wiki/Security-Controls.md` (the code quality row), the code and tests of the findings below | [RC-277] |
| The code of MIL-058: the back-office view models of `src/NFHotel.Web/Features/Admin` (bookings, guests, rooms) and `Booking/BookingFlowViewModel`, `BenchmarkExport` and the Razor loops of `Benchmark` and `Pricing`, `src/NFHotel.External/MarketAnalysis` (`FindingsSeries`, `MarketAnalysisResultMapper`), the test paths and the disposables of `tests/`, `StubHandler`, and `JsonClient.get_json` of the scan exporter | Source and test code (built, not yet merged) | - | - | [MIL-058] tasks 2 to 5 | - | [RC-278] (the findings report of the 57 findings) |
| [MIL-059] | Milestone (Accepted, Go) | en | it | [BC-001], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032], [MIL-022], [MIL-044] | [ADR-0037], [SAR-001], `docs/wiki/Security-Controls.md` and `docs/wiki/Workflows.md` (the policy rows), the code below | [RC-279] |
| [ADR-0037] | ADR (Accepted) | - | - | [MIL-059], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032] | `ops/tests/test-workflow-policy.sh`, the repository settings of SAR-001 section 11 | [RC-280] |
| [SAR-001] | Security audit report (Accepted) | en | it | [MIL-059], [ADR-0037], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032], [MIL-022] | the manual actions of S01 (its section 14) | [RC-280] |
| The code of MIL-059: `.github/workflows/ci.yaml`, `.github/workflows/running-image-scan.yaml`, `.github/workflows/ops-ci.yaml`, `.github/dependabot.yml`, `ops/tests/test-workflow-policy.sh` | Workflows, configuration and test code (built, not yet merged) | - | - | [MIL-059] tasks 2 to 4, [ADR-0037] | - | [RC-280] |
| [ADR-0019] | ADR (Accepted, amended by ADR-0021 and ADR-0038) | en | it | [SA-001], [ADR-0007], [ADR-0018] | [ADR-0021], [ADR-0038] | [RC-167], [RC-281] |
| [ADR-0021] | ADR (Accepted, amended by ADR-0038) | en | it | [ADR-0007], [ADR-0018], [ADR-0019], [MIL-021] | [ADR-0038], `ops/scripts/cd-steps.sh gate` | [RC-170], [RC-281] |
| [ADR-0038] | ADR (Accepted) | - | - | [ADR-0021], [ADR-0019], [ADR-0007], [ADR-0018], [SAR-001], [MIL-021], [SA-001] | the header of `.github/workflows/cd.yaml`, the wiki pages CI-CD, Security-Controls, Workflows, DevOps-Lifecycle and Technology-Choices, the required reviewer of the environment `production` | [RC-281] |
| [MIL-060] | Milestone (Accepted, Go) | en | it | [BC-001], [ADR-0038], [MIL-022], [MIL-044] | `docs/wiki/CI-CD.md` (the section "Dependency updates"), the code below | [RC-282] |
| The code of MIL-060: `src/NFHotel.Web/NFHotel.Web.csproj` and `src/NFHotel.Web/packages.lock.json` (the commit of Dependabot from pull request #489) and `tests/NFHotel.Web.Tests/packages.lock.json` (three lines) | Project file and lock files (built, not yet merged) | - | - | [MIL-060] task 1 | - | [RC-282] (the evidence of criteria 1 and 2) |
| [MIL-061] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-289) | en | it | [BC-001], [ADR-0004], [ADR-0006], ADR-0015, ADR-0028, [ADR-0038], MIL-015, MIL-020, [MIL-026] | ADR-0039, the network `nfhotel-python` in `ops/scripts/deploy-services.sh` and `ops/scripts/lib/stack.sh`, the compose files of the web application, the API, the market analysis and the benchmark, nginx without the listener on 8081, `ops/tests/test-service-networks.sh` and its Ops CI job, the wiki pages Network Isolation, Containers and Images, Environment Files and Proxmox Deployment | [RC-287], [RC-288] (the code), [RC-289] (the gate) |
| [ADR-0039] | ADR (Accepted) | - | - | [MIL-061], [ADR-0003], [ADR-0004], [ADR-0006], ADR-0015, ADR-0028 | the network `nfhotel-python` in `ops/scripts` and `ops/deploy`, nginx without the listener on 8081, `ops/tests/test-service-networks.sh` | [RC-288] |
| The code of MIL-061: `ops/scripts/deploy-services.sh`, `ops/scripts/lib/stack.sh`, the compose files and settings examples of `ops/deploy/nfhotel-web`, `nfhotel-api`, `market-analysis`, `benchmark` and `nginx`, `ops/tests/test-service-networks.sh` with its job in `.github/workflows/ops-ci.yaml`, and the tests that follow the move | Shell, compose and workflow files (deployed on `nf-hotel` by CD run 42066748612 on 2026-11-05) | - | - | [MIL-061] tasks 2 to 5 | - | [RC-288], [RC-289] |
| [MIL-062] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-293) | en | it | [BC-001], [ADR-0018], [ADR-0021], [ADR-0038], [MIL-021], [MIL-023] | the command `prune` of `ops/scripts/deploy-release.sh`, its tests in `ops/tests/test-deploy-release.sh`, the wiki pages Alerts, CI-CD and Proxmox Deployment | [RC-290], [RC-291] (the code), [RC-293] (the gate) |
| The code of MIL-062: the command `prune` of `ops/scripts/deploy-release.sh` and its nine checks in `ops/tests/test-deploy-release.sh` | Shell (deployed on `nf-hotel` by CD run 42078898723 on 2026-11-05, which removed 352 images) | - | - | [MIL-062] tasks 1 and 2 | - | [RC-291], [RC-293] |
| [MIL-063] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-297) | en | it | [BC-001], [ADR-0007], [ADR-0021], [ADR-0038], [MIL-021] | the wait for port 22 in `scope` of `ops/scripts/cd-steps.sh`, its tests in `ops/tests/test-cd-steps.sh`, the "Does" row of the CD workflow on the wiki page Workflows | [RC-295], [RC-296] (the code), [RC-297] (the gate) |
| The code of MIL-063: `wait_reach` and the wait in `scope` of `ops/scripts/cd-steps.sh`, and its checks in `ops/tests/test-cd-steps.sh` | Shell (run by CD run 42103015481 on 2026-11-05: "answers (after 0 s)") | - | - | [MIL-063] tasks 1 and 2 | - | [RC-296], [RC-297] |
| [MIL-064] | Milestone (Accepted, Go; plan reviewed 2026-11-08, RC-298; task 1 done 2026-11-08: ADR-0040, RC-299; tasks 2 to 7 done and reviewed 2026-11-08, RC-300; deployed 2026-11-08 by CD run 42442894208; gate passed 2026-11-08, RC-303, after RC-302 Go-with-conditions) | en | it | [BC-001], [ADR-0007], [ADR-0030], [ADR-0038], [MIL-022], [MIL-036], [MIL-059] | [ADR-0040]; the patched image `nfhotel-nginx` in the release and its deploy by name (`ops/deploy/nginx/Dockerfile`, `ops/scripts/build-release-images.sh`, `ops/scripts/cd-steps.sh`, `ops/scripts/deploy-release.sh`); the daily scan of the pinned third-party images (`ops/scripts/scan-running-release.sh`, `ops/scripts/running-image-scan-steps.sh`); seven wiki pages; the follow-up [MIL-065] for the other third-party images | [RC-298], [RC-300] (the code), [RC-302] (the gate, Go-with-conditions), [RC-303] (the re-review, Go) |
| The code of MIL-064: `ops/deploy/nginx/Dockerfile`, the fifth image in `ops/scripts/build-release-images.sh` and `.github/actions/release-images/action.yml`, the references by name in `ops/scripts/cd-steps.sh` and `ops/scripts/deploy-release.sh`, the scan of the third-party images in `ops/scripts/scan-running-release.sh`, `ops/scripts/running-image-scan-steps.sh` and `.github/workflows/running-image-scan.yaml`, and their tests | Shell, GitHub Actions, Dockerfile (deployed 2026-11-08 by CD run 42442894208; the daily scan run 42443407440 covers the third-party images) | - | - | [MIL-064] tasks 2 to 6, [ADR-0040] | - | [RC-300], [RC-302] |
| [MIL-065] | Milestone (Accepted, Go; plan reviewed 2026-11-08, RC-301 Go-with-conditions; accepted by S01 in chat the same day with the tag check and decision D2, RC-304; deployed 2026-11-08 by CD run 42478558104 and on the monitoring container; gate passed 2026-11-08, RC-307) | en | it | [BC-001], [DSA-001], [ADR-0007], [ADR-0038], [ADR-0040], [MIL-022], [MIL-036], [MIL-064] | planned: the pins of tasks 1 to 11 in the compose files of `ops/deploy`, the patched socket proxy `nfhotel-socket-proxy` from the release, ignore entries in `ops/security/trivyignore.yaml` narrowed to the third-party images, four wiki pages and the tag check of `test-wiki-coverage.sh`, decision D2 in section 15 of DSA-001 | [RC-301], [RC-304], [RC-307] |
| The code of MIL-065: the pins of the compose files of `ops/deploy`, the ignore entries of `ops/security/trivyignore.yaml`, `ops/deploy/alloy/socket-proxy/Dockerfile`, the sixth image in `ops/scripts/build-release-images.sh` and `.github/actions/release-images/action.yml`, the sixth name in `ops/scripts/cd-steps.sh` and `ops/scripts/deploy-release.sh`, `--no-build` in `ops/scripts/deploy-services.sh`, the tag check of `ops/tests/test-wiki-coverage.sh`, and their tests | Shell, GitHub Actions, Dockerfile, YAML (reviewed 2026-11-08, Go after S01 approved the risk exceptions in chat) | - | - | [MIL-065] tasks 1 to 13, [ADR-0040], [DSA-001] | - | [RC-305] |
| [MIL-066] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-08, RC-306; deployed 2026-11-08 by CD run 42498483148, on the monitoring container and on the backup host; gate passed 2026-11-08, RC-313) | en | it | [BC-001], [DSA-001], [ADR-0008], [ADR-0011], ADR-0025, [ADR-0034], [ADR-0038], [MIL-021], [MIL-022], MIL-027, MIL-037, [MIL-044], [MIL-065] | planned: the backup target on Debian 13 with `rrsync -wo -no-del -no-overwrite`, the copy off the VM without `--partial`, a settings archive that is complete or fails, the migration history read-only for `nfhotel_app`, the ignore check of `scan-gate.sh`, the ZAP rules renewed before 2026-11-25, the versions, the retention of Prometheus in its configuration file, and the rebuild runbook in the tested order | [RC-306], [RC-313] |
| The code of MIL-066: the backup target in `ops/deploy/backup-target`, the copy and the settings archive of `ops/scripts/backup-database.sh`, the rights on the migration history in `ops/scripts/db/roles.sql`, `ops/scripts/deploy-release.sh` and `ops/scripts/verify-database.sh`, the format check of `ops/scripts/scan-gate.sh`, the retention in `ops/deploy/monitoring/prometheus/prometheus.yml`, the versions of the CD, scan and build tools, the Debian release of `ops/scripts/create-docker-vm.sh`, the section "Rebuild the VM" of the wiki page Disaster-Recovery, and their tests | Shell, SQL, GitHub Actions, Dockerfile, YAML (reviewed 2026-11-08, Go-with-conditions; the condition met on #587, and the check of the settings guard added at the gate, RC-313) | - | - | [MIL-066] tasks 1 to 6, 8 and 9, [ADR-0008], [ADR-0011], [DSA-001] | - | [RC-308], [RC-313] |
| [MIL-067] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-08, RC-309; task 7 accepted by the AI as stand-in for S01 at S01's request, RC-310; deployed 2026-11-08 by CD run 42498483148; gate passed 2026-11-08, RC-312) | en | it | [BC-001], [US-001], UC-004, UC-005, [DSA-001], [ADR-0038], MIL-027, [MIL-044], [MIL-066] | planned: the guests of the booking page and the run of the benchmark page read as text, a test that refuses a typed query parameter in `NFHotel.Web`, and a login form and a change-password form that never write a posted password back | [RC-309], [RC-310], [RC-312] |
| The code of MIL-067: the guests of `Book.razor.cs` and `BookingFlowViewModel.Prefill`, the run of `BenchmarkStart.razor.cs`, the passwords of `Login.razor` and `ChangePassword.razor` with their code-behind files, and their tests | C#, Razor (reviewed 2026-11-08, Go-with-conditions; the condition met on #596, RC-312) | - | - | [MIL-067] tasks 1 to 4 and 7, UC-004, UC-005 | - | [RC-311], [RC-312] |
| [MIL-068] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-08, RC-314; the desktop removed on the VM 2026-11-08; gate passed 2026-11-08, RC-319) | en | it | [BC-001], [CIS-001], [DSA-001], [ADR-0010], [MIL-023], [MIL-066], [RC-313] | planned: the removal list and `ops/scripts/remove-desktop.sh` with its test, the VM started as `multi-user.target` without the desktop, the wiki page Host-Hardening and rows 2.1.x and 2.2.x of CIS-001 | [RC-314], [RC-319] |
| [MIL-069] | Milestone (Accepted, Go; plan and retention reviewed and accepted by S01 in chat 2026-11-08, RC-315; the prune redeployed on the backup host 2026-11-08; gate passed 2026-11-09, RC-322) | en | it | [BC-001], [DSA-001], [ADR-0008], [MIL-021], [MIL-066], [RC-313] | planned: `ops/deploy/backup-target/prune-backups.sh` with its tests, the service `prune` of the backup target, and the wiki page Disaster-Recovery | [RC-315], [RC-322] |
| The code of MIL-069: the prune of the backup target in `ops/deploy/backup-target/prune-backups.sh`, the service `prune` in its `docker-compose.yml`, `Dockerfile` and `backup-target.env.example`, the section "How long the backup target keeps the copies" of the wiki page Disaster-Recovery, and their tests in `ops/tests/test-backup-target.sh` | Shell, Dockerfile, YAML (reviewed 2026-11-08, Go; deployed to the backup host 2026-11-08, RC-322) | - | - | [MIL-069] tasks 1 to 3, [ADR-0008], [DSA-001] | - | [RC-316], [RC-322] |
| The code of MIL-068: the removal of the desktop in `ops/scripts/remove-desktop.sh`, its test `ops/tests/test-remove-desktop.sh` and its entry in the matrix of `.github/workflows/ops-ci.yaml`, and the section "Without a desktop" of the wiki page Host-Hardening with its update command | Shell, YAML (reviewed 2026-11-08, Go; run on the VM 2026-11-08, RC-319) | - | - | [MIL-068] tasks 1 to 3, [CIS-001] | - | [RC-317], [RC-319] |
| [MIL-070] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-08, RC-318; the fix deployed and the new password on the VM 2026-11-08; gate passed 2026-11-09, RC-321) | en | it | [BC-001], [CIS-001], [DSA-001], [ADR-0011], [ADR-0024], [MIL-023], [MIL-068] | planned: `as_app` of `ops/scripts/deploy-release.sh` without the connection string in the log of `sudo`, ADR-0041, a regression test, a new password for `nfhotel_app`, and the wiki pages Database-Preparation and Host-Hardening | [RC-318], [RC-321] |
| [MIL-071] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-11, RC-323; the reviews of tasks 3 and 4 and the gate accepted 2026-11-14 by the AI as stand-in for S02, and of task 5 as stand-in for S01, at S01's word in chat; the published wiki checked 2026-11-14; gate passed, RC-329) | en | it | [BC-001], [US-001], [MIL-055], [MIL-028], notebook-market-analysis MIL-002 | the contract and client (`src/NFHotel.External/MarketAnalysis`, `docs/test-data/market-contract.yaml`), the presentation of the suggestions (`PriceSuggestionService`, the page `/admin/pricing/suggestions`), `NoPriceRuleTests`, the wiki page Price Suggestions | [RC-323], [RC-324], [RC-325], [RC-326], [RC-327], [RC-328], [RC-329] |
| [MIL-072] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-11, RC-330; the routes of the tunnel changed in the Cloudflare dashboard 2026-11-11; the code reviewed and accepted by S01 2026-11-11, RC-331; deployed 2026-11-12 by CD run 43262765251; the check inside the VM made 2026-11-14 at S01's word in chat; gate passed 2026-11-14, RC-339) | en | it | [BC-001], [DSA-001], ADR-0028, ADR-0029, MIL-040, MIL-041 | done 2026-11-11: the validating route of the tunnel first, with the path `(?i)^/admin` (task 1); reviewed: `location ~* ^/admin` in `ops/deploy/nginx/templates/public.conf.template` with its test in `ops/tests/test-nginx.sh`, a row of ADR-0029 and the wiki page Public-Access (tasks 2 to 4); ADR-0029 decision 1 and the wiki say the session lasts 1 week, as the Access application is set; deployed 2026-11-12 and checked inside the VM 2026-11-14 (task 5) | [RC-330], [RC-331], [RC-339] |
| The code of MIL-070: `as_app` of `ops/scripts/deploy-release.sh`, its test `ops/tests/test-sudo-log.sh` and its entry in the matrix of `.github/workflows/ops-ci.yaml`, the section "No secret in the log of sudo" of the wiki page Host-Hardening, the section "A new password for `nfhotel_app`" of the wiki page Database-Preparation, and the script tests of the wiki page Workflows | Shell, YAML (reviewed 2026-11-08, Go; deployed to the VM 2026-11-08, RC-321) | - | - | [MIL-070] tasks 1 to 3, [ADR-0041], [CIS-001] | - | [RC-320], [RC-321] |
| The code of MIL-072: `location ~* ^/admin` in `ops/deploy/nginx/templates/public.conf.template`, its regression test in `ops/tests/test-nginx.sh`, which Ops CI runs, the rows of 2026-11-11 of ADR-0029, and the table, the setup of the back office and "Check it" of the wiki page Public-Access | Shell, nginx (reviewed 2026-11-11, Go, accepted by S01; deployed 2026-11-12 by CD run 43262765251 and checked inside the VM 2026-11-14, RC-339) | - | - | [MIL-072] tasks 2 to 4, ADR-0029 | - | [RC-331], [RC-339] |
| [MIL-079] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-340; the gate follows the deploy) | en | it | [BC-001], [ADR-0004], ADR-0005, MIL-016, MIL-018, UC-004, BR-C-06 | planned: `Bookings.View` on the read of a booking by id, the lookup `POST api/bookings/public/receipt` by the number and the email of the booking with the throttle per booking, the page "Find your booking", the receipt on the booking page, BR-C-06 and the test plan, the wiki pages Security-Controls and Public-Access, and the evidence folder `docs/sqa/evidence/mil-079` | [RC-340], [RC-341] |
| The code of MIL-079: the read by id in `BookingsController` and `BookingService`, the lookup `LookUpReceiptAsync` in `IBookingService`, `BookingService`, `BookingsController` and `ApiBookingService` with `BookingReceiptDto` and `BookingReceiptRequest`, the sweep of `InMemoryLoginThrottle`, the pages `BookingReceipt.razor`, `BookingReceiptCard.razor` and `Book.razor` with their view models, the footer link of `PublicLayout.razor`, and their tests | C#, Razor (reviewed 2026-11-12, Go; criterion 21 on the risk exception of [RC-278]) | - | - | [MIL-079] tasks 1 to 4, UC-004, BR-C-06 | - | [RC-341] |
| [MIL-080] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-343; deployed 2026-11-13 by CD run 43498058217; the monitoring deployed 2026-11-14 at S01's word in chat; gate passed 2026-11-14, RC-347) | en | it | [BC-001], [CIS-001], [DSA-001], ADR-0018, ADR-0020, MIL-023, MIL-031 | planned: the profiles `ops/security/container-profiles.tsv`, the check `ops/scripts/check-container-runtime.sh` (configuration and kernel view of each main process), the deploy gate and the timer every 15 minutes, the alerts, the check in the monitoring LXC, the unit, live and end-to-end tests, [CIS-001] and the wiki pages Host-Hardening and Monitoring | [RC-343], [RC-347] |
| The code of MIL-080: the profiles `ops/security/container-profiles.tsv`, the check `ops/scripts/check-container-runtime.sh` with its unit and live tests and its step in the end-to-end action, the deploy gate, the service and the timer (`ops/scripts/deploy-release.sh`, `ops/deploy/systemd/`), the alerts `ContainerRuntimeViolation` and `ContainerCheckStale` with their tests, the check in `ops/scripts/deploy-monitoring.sh`, and the limits of `results-owner` of the benchmark | Shell, YAML (reviewed 2026-11-13, Go-with-conditions, met: every check of #674 green; deployed to the VM 2026-11-13 and to the monitoring LXC 2026-11-14, RC-347) | - | - | [MIL-080] tasks 1 to 6, [CIS-001] | - | [RC-346], [RC-347] |
| [MIL-081] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-344; deployed 2026-11-13 by CD run 43506763115; gate passed 2026-11-13, RC-349) | en | it | [BC-001], [CIS-001], ADR-0020, ADR-0024, ADR-0040, MIL-065, MIL-080 | planned: Alloy's calls measured, the allowlist of the socket proxy in its own HAProxy configuration, the test matrix of `ops/tests/test-alloy-start.sh`, the probe of the proxy in the runtime check, ADR-0042, [CIS-001] row 5.31 and the wiki page Containers-and-Images | [RC-344], [RC-349] |
| The code of MIL-081: the allowlist `ops/deploy/alloy/socket-proxy/haproxy.cfg` in the image of the proxy (its `Dockerfile`), the environment of the proxy in `ops/deploy/alloy/docker-compose.yml`, the measurement and the matrix of `ops/tests/test-alloy-start.sh`, and the rule `proxy_allowlist` of `ops/scripts/check-container-runtime.sh` with its unit test | Shell, YAML, HAProxy (reviewed 2026-11-13, Go; deployed to the VM 2026-11-13, RC-349) | - | - | [MIL-081] tasks 1 to 5, [ADR-0042], [CIS-001] | - | [RC-348], [RC-349] |
| [MIL-082] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-345; deployed 2026-11-13 by CD run 43513939028; gate 2026-11-13 Go-with-conditions, RC-351: the deploy of the monitoring is pending) | en | it | [BC-001], [CIS-001], [DSA-001], MIL-045, MIL-080, MIL-081 | planned: the host-access map from the runtime check, the wiki page Container-Escape, Ollama as a non-root user with its CI test, the runtime versions in the check, [CIS-001] rows 2.1 and 4.1 | [RC-345], [RC-351] |
| The code of MIL-082: Ollama as 10001 with the one-shot service `models-owner` in `ops/deploy/ollama/docker-compose.yml` and its rows in `ops/security/container-profiles.tsv`, the test `ops/tests/test-ollama-user.sh` and its job in `.github/workflows/ops-ci.yaml`, the rule `system_paths` and the metric `nfhotel_container_runtime_info` of `ops/scripts/check-container-runtime.sh` with their unit and live tests, the lines of the check in `ops/tests/test-monitoring-stack.sh`, and the wiki page Container-Escape | Shell, YAML (reviewed 2026-11-13, Go-with-conditions, met: every check of #676 green; deployed to the VM 2026-11-13, RC-351) | - | - | [MIL-082] tasks 1 to 5, [CIS-001] | - | [RC-350], [RC-351] |
| [ADR-0040] | ADR (Accepted) | - | - | [MIL-064], [ADR-0007], [ADR-0030], [MIL-022], [MIL-036], [MIL-059] | the patch layer of the proxy in `ops/deploy/nginx/Dockerfile` and its entry in `ops/scripts/build-release-images.sh`, the release references by name in `ops/scripts/cd-steps.sh` and `ops/scripts/deploy-release.sh`, the `docker` entry of `.github/dependabot.yml` (MIL-064 tasks 2 to 7, RC-300) | [RC-299], [RC-300] (the code) |
| [ADR-0041] | ADR (Accepted) | - | - | [MIL-070], [ADR-0011], [ADR-0024], [MIL-023], [CIS-001] | `as_app` of `ops/scripts/deploy-release.sh`, the second check of `ops/tests/test-sudo-log.sh`, and the sections of the wiki pages Host-Hardening and Database-Preparation (MIL-070 tasks 1 to 3, RC-320) | [RC-320] |
| [ADR-0042] | ADR (Accepted) | - | - | [MIL-081], [ADR-0020], [ADR-0024], [ADR-0040], [MIL-080], [CIS-001] | `ops/deploy/alloy/socket-proxy/haproxy.cfg` and its `Dockerfile`, the environment of the proxy in `ops/deploy/alloy/docker-compose.yml`, the matrix of `ops/tests/test-alloy-start.sh` and the rule `proxy_allowlist` of `ops/scripts/check-container-runtime.sh` (MIL-081 tasks 1 to 4, RC-348) | [RC-348] |
| [MIL-046] | Milestone (Accepted, Go; gate passed 2026-11-05) | en | it | [BC-001], [ADR-0031], MIL-043, MIL-045, US-001.04 | [ADR-0031] (decisions 2 and 3), `BenchmarkRequest` and `BenchmarkRunTracker` of its task 2 | [RC-238], [RC-239], [RC-283] |
| [ADR-0031] | ADR (Accepted; decisions 2 and 3 added 2026-10-26) | - | - | ADR-0005, ADR-0013, [ADR-0004], ADR-0024, MIL-020, MIL-043 | the sign-in keys and the benchmark run tracker of `src/NFHotel.Web`, [MIL-046] | [RC-227], [RC-239] |
| [MIL-056] | Milestone (Accepted, Go; gate passed 2026-11-05 after the amendment of criterion 3; the run on a Mac by S02 is recorded at S02's onboarding) | en | it | [BC-001], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | [ADR-0035], `scripts/dev-up.sh --lite`, `ops/scripts/lib/`, the Ops CI test and the macOS job, the wiki page Local Development Deploy | [RC-271], [RC-272], [RC-286] |
| [ADR-0035] | ADR (Accepted) | - | - | [MIL-056], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | the light mode of `scripts/dev-up.sh` | [RC-273] |
| [MIL-048] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-292, after RC-285 Go-with-conditions) | en | it | [BC-001], [ADR-0011], [ADR-0034], [MIL-023], CIS-001, [DSA-001] | [ADR-0034], `ops/scripts/db/team-roles.sql`, `ops/scripts/manage-team-db-users.sh`, `ops/deploy/postgres-tailnet/`, the rule for 5432 in `nfhotel.nft`, the wiki page Database-Access | [RC-245], [RC-247], [RC-285] (Go-with-conditions), [RC-292] (the re-review, Go) |
| [ADR-0034] | ADR (Accepted) | - | - | [MIL-048], [ADR-0011], ADR-0024, CIS-001, [DSA-001] | the team roles, the relay on the tailnet and the firewall rule of MIL-048 | [RC-247] |
| [UC-007] | Use Case (Accepted) | en | it | [UCD-001], [US-001], [SA-001], [MIL-055] | [DM-004], [DICT-001], code and tests below | [RC-264] (Go with conditions) |
| `docs/wiki/Price-Suggestions.md` | Wiki page (Accepted 2026-11-14 by the AI as stand-in for S01, RC-328; reworded by [MIL-071] task 5; checked on the published wiki 2026-11-14) | en | - | [MIL-055] task 10, [MIL-071] task 5, [US-001], [UC-007] | - | [RC-328] |
| Dictionary terms and domain concepts: Room rate, Price change, Price suggestion, Season, Pricing settings; Audit event actions `PriceChangeApproved`, `PriceChangeReverted`, `PricingSettingsChanged`, `RoomRateChanged` | Domain terms (Accepted) | en | it | [UC-007], [US-001] | [DICT-001], [DM-004], `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`) | [RC-264] |
| `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`, `PriceChangeState`), `src/NFHotel.Application/UseCases/Pricing` (`PriceSuggestionService`, `PriceChangeService`, `PricingSettingsService`, `OccupancyObservationReader`; `SeasonRule` and `PriceSuggestionRule` of MIL-055 are removed by MIL-071 task 3, RC-326), `src/NFHotel.Application/UseCases/RoomRates` (`RoomRateService`), the price lookup in `BookingService` | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3 to 9, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Api/Controllers` (`RoomRatesController`, `PricingSettingsController`, `PriceSuggestionsController`, `PriceChangesController`), `src/NFHotel.ApiClient` (`ApiRoomRateService`, `ApiPricingSettingsService`, `ApiPriceSuggestionService`), permission `Prices.Manage` | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 7, [US-001], [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Infrastructure` (configurations, repositories and migrations `AddRoomRateAndAuditDetail`, `AddPricingSettings`, `AddPriceChange`) | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Web/Features/Admin/Pricing` (`RoomRates`, `PricingSettings`, `PriceSuggestions` pages and view models), `AdminNav` | Source code | - | - | [MIL-055] tasks 3, 5 and 8, [MIL-071] task 4, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open), [RC-327] |
| Tests of the suggestions: `OccupancyObservationReaderTests`, `PriceSuggestionServiceTests`, `NoPriceRuleTests` (the tests `SeasonRuleTests` and `PriceSuggestionRuleTests` of MIL-055 are removed with the rule) | Test code | - | - | [MIL-055] tasks 4 and 7, [MIL-071] task 3, [US-001] | - | [RC-326] |
| Tests of the services: `RoomRateServiceTests`, `PricingSettingsServiceTests`, `PriceChangeServiceTests`, `PriceChangeAuditTests`, `BookingPriceTests` (Application.Tests); `ManagePricesPermissionTests`, `StaffRolePermissionsTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 7 and 9, [UC-007] | - | - |
| Tests of the domain: `RoomRateTests`, `PricingSettingTests`, `PriceChangeTests`, `StaffAuditEventDetailTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 6 | - | - |
| Tests of the persistence: `RoomRateRepositoryTests`, `PricingSettingRepositoryTests`, `PriceChangeRepositoryTests`, `BookingPriceTests` (Infrastructure.Tests) | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | - |
| Tests of the API and the client: `RoomRatesEndpointTests`, `PricingSettingsEndpointTests`, `PriceSuggestionsEndpointTests`, `EndpointProtectionTests`, `RoomRateRequestTests`, `PricingSettingsRequestTests`, `PriceSuggestionRequestTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 7, [UC-007] | - | - |
| Tests of the pages: `RoomRatesViewModelTests`, `PricingSettingsViewModelTests`, `PriceSuggestionsViewModelTests`, `PriceSuggestionsPageTests`, `RoomListViewModelTests`, `AdminAccessTests`, `BackOfficeAccessTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 8, [UC-007] | - | - |
| `src/NFHotel.External/MarketAnalysis` (`MarketAnalysisServiceClient`, `MarketAnalysisResultMapper`), `src/NFHotel.Application/Dto/MarketAnalysis` (the `pricing` request and the `price_suggestions` DTOs), `MarketAnalysisRunner` (sends the settings), the copies `docs/test-data/market-contract.yaml` and `docs/test-data/price-suggestions-sample.json` | Source code | - | - | [MIL-071] task 2, objective 29 of [BC-001] | - | [RC-325] |
| Tests of the contract and the client: `MarketContractTests`, `MarketAnalysisResultMapper13Tests`, `MarketAnalysisServiceClientTests`, `MarketAnalysisRunnerTests` | Test code | - | - | [MIL-071] task 2 | - | [RC-325] |

---

[BC-001]: ../business-case.md
[SA-001]: ../stakeholder-analysis.md
[MIL-001]: ../milestones/mil-001-proxmox-test-env.md
[PP-001]: ../project-plan.md
[RC-001]: ./reviews/rc-001-business-case.md
[RC-002]: ./reviews/rc-002-stakeholder-analysis.md
[RC-003]: ./reviews/rc-003-milestone-001.md
[RC-004]: ./reviews/rc-004-proxmox-tests-code.md
[RC-005]: ./reviews/rc-005-business-case-amendment.md
[RC-006]: ./reviews/rc-006-milestone-002.md
[RC-007]: ./reviews/rc-007-compose-and-smoke-test.md
[RC-008]: ./reviews/rc-008-business-case-throwaway-proxmox.md
[RC-009]: ./reviews/rc-009-stakeholder-analysis-update.md
[RC-010]: ./reviews/rc-010-milestone-003.md
[RC-011]: ./reviews/rc-011-throwaway-proxmox-code.md
[RC-012]: ./reviews/rc-012-user-stories.md
[RC-013]: ./reviews/rc-013-use-case-diagram.md
[RC-014]: ./reviews/rc-014-business-case-market-analysis.md
[RC-015]: ./reviews/rc-015-milestone-004.md
[RC-016]: ./reviews/rc-016-market-analysis-service.md
[RC-017]: ./reviews/rc-017-business-case-clean-architecture.md
[RC-018]: ./reviews/rc-018-milestone-005.md
[RC-019]: ./reviews/rc-019-adr-0001.md
[RC-020]: ./reviews/rc-020-clean-architecture-code.md
[RC-021]: ./reviews/rc-021-business-case-english-documents.md
[RC-022]: ./reviews/rc-022-milestone-006.md
[RC-023]: ./reviews/rc-023-legacy-documents-english.md
[RC-024]: ./reviews/rc-024-business-case-domain-model.md
[RC-025]: ./reviews/rc-025-milestone-007.md
[RC-026]: ./reviews/rc-026-stakeholder-analysis-hotel-staff.md
[RC-027]: ./reviews/rc-027-user-stories-hotel-staff.md
[RC-028]: ./reviews/rc-028-use-case-diagram-hotel-staff.md
[RC-029]: ./reviews/rc-029-dictionary.md
[RC-030]: ./reviews/rc-030-use-case-001.md
[RC-031]: ./reviews/rc-031-use-case-002.md
[RC-032]: ./reviews/rc-032-use-case-003.md
[RC-033]: ./reviews/rc-033-domain-model-uc001.md
[RC-034]: ./reviews/rc-034-domain-model-uc002.md
[RC-035]: ./reviews/rc-035-domain-model-uc003.md
[RC-036]: ./reviews/rc-036-domain-model.md
[RC-037]: ./reviews/rc-037-business-case-domain-folders.md
[RC-038]: ./reviews/rc-038-milestone-008.md
[RC-039]: ./reviews/rc-039-domain-rules-in-entities.md
[RC-040]: ./reviews/rc-040-business-case-validation-attributes.md
[RC-041]: ./reviews/rc-041-milestone-009.md
[RC-042]: ./reviews/rc-042-adr-0002.md
&
[RC-044]: ./reviews/rc-044-business-case-source-comments.md
[RC-045]: ./reviews/rc-045-milestone-010.md
[RC-046]: ./reviews/rc-046-source-comments-english.md
[RC-047]: ./reviews/rc-047-adr-0003.md
[DICT-001]: ../dictionary.md
[UC-001]: ../use-cases/uc-001/uc.md
[UC-002]: ../use-cases/uc-002/uc.md
[UC-003]: ../use-cases/uc-003/uc.md
[DM-001]: ../use-cases/uc-001/dm.md
[DM-002]: ../use-cases/uc-002/dm.md
[DM-003]: ../use-cases/uc-003/dm.md
[DM-004]: ../domain-model.md
[MIL-006]: ../milestones/mil-006-english-legacy-documents.md
[MIL-007]: ../milestones/mil-007-domain-model.md
[MIL-008]: ../milestones/mil-008-domain-rules-in-entities.md
[MIL-009]: ../milestones/mil-009-validation-attributes.md
[MIL-010]: ../milestones/mil-010-english-source-comments.md
[ADR-0002]: ../adr/adr-0002-validation-attributes-on-entities.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[ADR-0001]: ../adr/adr-0001-clean-architecture-structure.md
[MIL-005]: ../milestones/mil-005-clean-architecture.md
[MIL-004]: ../milestones/mil-004-market-analysis-service.md
[UCD-001]: ../use-case-diagram.md
[US-001]: ../user-stories.md
[MIL-003]: ../milestones/mil-003-throwaway-proxmox.md
[MIL-002]: ../milestones/mil-002-nginx-proxy-and-web-compose.md
[b690b57]: https://github.com/NF-Hotel/NFHotel/commit/b690b576d531e9f26709b6f21275cbc675065d03
[CIS-001]: ./cis-checklist.md
[DSA-001]: ./devsecops-assessment.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-036]: ../milestones/mil-036-security-operations.md
[MIL-023]: ../milestones/mil-023-operational-quality.md
[ADR-0004]: ../adr/adr-0004-database-api.md
[ADR-0006]: ../adr/adr-0006-tls.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0009]: ../adr/adr-0009-tls-certificates.md
[RC-088]: ./reviews/rc-088-business-case-cicd.md
[RC-089]: ./reviews/rc-089-milestone-021.md
[RC-090]: ./reviews/rc-090-milestone-022.md
[RC-091]: ./reviews/rc-091-milestone-023.md
[RC-092]: ./reviews/rc-092-adr-0007.md
[RC-093]: ./reviews/rc-093-adr-0008.md
[RC-094]: ./reviews/rc-094-mil-021-shell-code.md
[RC-095]: ./reviews/rc-095-mil-021-workflows.md
[RC-096]: ./reviews/rc-096-adr-0009.md
[RC-097]: ./reviews/rc-097-milestone-021-reconciliation.md
[RC-098]: ./reviews/rc-098-adr-0010.md
[RC-099]: ./reviews/rc-099-milestone-024.md
[RC-100]: ./reviews/rc-100-adr-0011.md
[RC-101]: ./reviews/rc-101-mil-024-code-and-gate.md
[MIL-024]: ../milestones/mil-024-postgresql-api-readiness.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0010]: ../adr/adr-0010-production-vm-as-deployed.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[RC-043]: ./reviews/rc-043-validation-attributes.md
[ADR-0012]: ../adr/adr-0012-devops-repository-ownership.md
[MIL-025]: ../milestones/mil-025-devops-repository-consolidation.md
[RC-102]: ./reviews/rc-102-adr-0012.md
[RC-103]: ./reviews/rc-103-milestone-025.md
[MIL-055]: ../milestones/mil-055-room-price-suggestions.md
[UC-007]: ../use-cases/uc-007/uc.md
[RC-262]: ./reviews/rc-262-mil-055-room-price-suggestions.md
[RC-263]: ./reviews/rc-263-mil-055-task-1-objective-and-story.md
[RC-264]: ./reviews/rc-264-mil-055-task-2-terms-and-model.md
[RC-265]: ./reviews/rc-265-mil-055-tasks-3-to-9-code.md
[RC-266]: ./reviews/rc-266-mil-055-re-review-of-rc-265.md
[MIL-026]: ../milestones/mil-026-market-analysis-report.md
[MIL-028]: ../milestones/mil-028-market-analysis-run-api.md
[MIL-030]: ../milestones/mil-030-staff-accounts-and-password.md
[MIL-044]: ../milestones/mil-044-security-overview.md
[MIL-053]: ../milestones/mil-053-security-scan-metrics.md
[MIL-058]: ../milestones/mil-058-codeql-code-quality-findings.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[QC-CS-001]: ../../framework/qc/code/qc-programming-csharp.md
[QC-PY-001]: ../../framework/qc/code/qc-programming-python.md
[RC-277]: ./reviews/rc-277-mil-058.md
[RC-278]: ./reviews/rc-278-mil-058-code.md
[MIL-059]: ../milestones/mil-059-github-actions-security-audit.md
[ADR-0019]: ../adr/adr-0019-approvals-by-the-owner.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[ADR-0037]: ../adr/adr-0037-github-actions-hardening-policy.md
[SAR-001]: ./security-audit-2026-11.md
[RC-279]: ./reviews/rc-279-mil-059.md
[RC-280]: ./reviews/rc-280-mil-059-code-and-report.md
[RC-281]: ./reviews/rc-281-adr-0038.md
[RC-170]: ./reviews/rc-170-adr-0021.md
[RC-167]: ./reviews/rc-167-adr-0019.md
[ADR-0018]: ../adr/adr-0018-deploy-command-updates-itself.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[ADR-0039]: ../adr/adr-0039-python-services-network.md
[QC-ADR-001]: ../../framework/qc/artifacts/qc-adr.md
[QC-WF-001]: ../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../framework/qc/code/qc-programming-shell.md
[MIL-060]: ../milestones/mil-060-dependabot-nuget-lock-files.md
[MIL-061]: ../milestones/mil-061-python-services-network.md
[MIL-062]: ../milestones/mil-062-prune-old-release-images.md
[MIL-063]: ../milestones/mil-063-cd-scope-waits-for-the-tailnet.md
[MIL-064]: ../milestones/mil-064-patched-nginx-and-third-party-scan.md
[MIL-065]: ../milestones/mil-065-third-party-image-remedies.md
[MIL-066]: ../milestones/mil-066-findings-of-7-november.md
[MIL-067]: ../milestones/mil-067-bad-input-on-the-web-pages.md
[MIL-068]: ../milestones/mil-068-the-vm-without-a-desktop.md
[MIL-069]: ../milestones/mil-069-retention-on-the-backup-target.md
[MIL-070]: ../milestones/mil-070-the-database-password-out-of-the-logs.md
[ADR-0040]: ../adr/adr-0040-patched-nginx-image-from-the-release.md
[ADR-0041]: ../adr/adr-0041-connection-string-over-standard-input.md
[ADR-0042]: ../adr/adr-0042-only-the-calls-of-alloy-through-the-socket-proxy.md
[RC-282]: ./reviews/rc-282-mil-060.md
[MIL-046]: ../milestones/mil-046-finished-run-not-interrupted.md
[ADR-0031]: ../adr/adr-0031-state-that-survives-a-deploy.md
[MIL-056]: ../milestones/mil-056-dev-stack-without-kvm.md
[ADR-0035]: ../adr/adr-0035-light-local-development-stack.md
[RC-227]: ./reviews/rc-227-adr-0031.md
[RC-238]: ./reviews/rc-238-mil-046.md
[RC-239]: ./reviews/rc-239-mil-046-code.md
[RC-271]: ./reviews/rc-271-mil-056-dev-stack-without-kvm.md
[RC-272]: ./reviews/rc-272-mil-056-code.md
[RC-286]: ./reviews/rc-286-mil-056-gate.md
[RC-294]: ./reviews/rc-294-mil-023-gate.md
[RC-295]: ./reviews/rc-295-mil-063.md
[RC-296]: ./reviews/rc-296-mil-063-code.md
[RC-297]: ./reviews/rc-297-mil-063-gate.md
[RC-298]: ./reviews/rc-298-mil-064.md
[RC-299]: ./reviews/rc-299-adr-0040.md
[RC-300]: ./reviews/rc-300-mil-064-code.md
[RC-301]: ./reviews/rc-301-mil-065.md
[RC-304]: ./reviews/rc-304-mil-065-acceptance.md
[RC-305]: ./reviews/rc-305-mil-065-code.md
[RC-306]: ./reviews/rc-306-mil-066.md
[RC-307]: ./reviews/rc-307-mil-065-gate.md
[RC-308]: ./reviews/rc-308-mil-066-code.md
[RC-309]: ./reviews/rc-309-mil-067.md
[RC-310]: ./reviews/rc-310-mil-067-task-7.md
[RC-311]: ./reviews/rc-311-mil-067-code.md
[RC-312]: ./reviews/rc-312-mil-067-gate.md
[RC-313]: ./reviews/rc-313-mil-066-gate.md
[RC-314]: ./reviews/rc-314-mil-068.md
[RC-315]: ./reviews/rc-315-mil-069.md
[RC-316]: ./reviews/rc-316-mil-069-code.md
[RC-317]: ./reviews/rc-317-mil-068-code.md
[RC-318]: ./reviews/rc-318-mil-070.md
[RC-319]: ./reviews/rc-319-mil-068-gate.md
[RC-320]: ./reviews/rc-320-mil-070-code.md
[RC-321]: ./reviews/rc-321-mil-070-gate.md
[RC-322]: ./reviews/rc-322-mil-069-gate.md
[MIL-071]: ../milestones/mil-071-price-suggestions-from-the-service.md
[MIL-072]: ../milestones/mil-072-the-access-check-on-every-admin-path.md
[RC-323]: ./reviews/rc-323-mil-071-price-suggestions-from-the-service.md
[RC-324]: ./reviews/rc-324-mil-071-task-1-objective-29-and-us-001-12.md
[RC-325]: ./reviews/rc-325-mil-071-task-2-code.md
[RC-326]: ./reviews/rc-326-mil-071-task-3-code.md
[RC-327]: ./reviews/rc-327-mil-071-task-4-code.md
[RC-328]: ./reviews/rc-328-mil-071-task-5-wiki-and-traceability.md
[RC-329]: ./reviews/rc-329-mil-071-gate.md
[RC-330]: ./reviews/rc-330-mil-072.md
[RC-331]: ./reviews/rc-331-mil-072-code.md
[RC-339]: ./reviews/rc-339-mil-072-gate.md
[MIL-079]: ../milestones/mil-079-a-booking-opened-only-with-its-email.md
[RC-340]: ./reviews/rc-340-mil-079.md
[RC-341]: ./reviews/rc-341-mil-079-code.md
[MIL-080]: ../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[MIL-081]: ../milestones/mil-081-only-the-calls-of-alloy-through-the-socket-proxy.md
[MIL-082]: ../milestones/mil-082-a-compromised-container-kept-away-from-the-host.md
[RC-343]: ./reviews/rc-343-mil-080.md
[RC-346]: ./reviews/rc-346-mil-080-code.md
[RC-347]: ./reviews/rc-347-mil-080-gate.md
[RC-344]: ./reviews/rc-344-mil-081.md
[RC-345]: ./reviews/rc-345-mil-082.md
[RC-348]: ./reviews/rc-348-mil-081-code.md
[RC-349]: ./reviews/rc-349-mil-081-gate.md
[RC-350]: ./reviews/rc-350-mil-082-code.md
[RC-351]: ./reviews/rc-351-mil-082-gate.md
[RC-302]: ./reviews/rc-302-mil-064-gate.md
[RC-303]: ./reviews/rc-303-mil-064-gate-re-review.md
[RC-287]: ./reviews/rc-287-mil-061.md
[RC-288]: ./reviews/rc-288-mil-061-code.md
[RC-289]: ./reviews/rc-289-mil-061-gate.md
[RC-290]: ./reviews/rc-290-mil-062.md
[RC-291]: ./reviews/rc-291-mil-062-code.md
[RC-292]: ./reviews/rc-292-mil-048-gate-re-review.md
[RC-293]: ./reviews/rc-293-mil-062-gate.md
[RC-273]: ./reviews/rc-273-adr-0035.md
[RC-283]: ./reviews/rc-283-mil-046-gate.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[RC-169]: ./reviews/rc-169-mil-023-amendment.md
[RC-172]: ./reviews/rc-172-mil-023-task-12.md
[RC-176]: ./reviews/rc-176-mil-023-host-hardening.md
[RC-177]: ./reviews/rc-177-mil-023-scorecard-samm-dora.md
[RC-179]: ./reviews/rc-179-mil-023-monitoring.md
[RC-189]: ./reviews/rc-189-alarm-drill.md
[RC-191]: ./reviews/rc-191-host-hardening-apply-part-1.md
[RC-192]: ./reviews/rc-192-firewall-tailscale-only.md
[RC-193]: ./reviews/rc-193-docker-daemon-applied.md
[RC-244]: ./reviews/rc-244-mil-023-host-settings.md
[RC-505]: ./reviews/rc-505-restore-drill-2026-10-20.md
[RC-284]: ./reviews/rc-284-restore-drill-2026-11-05.md
[MIL-048]: ../milestones/mil-048-team-database-access.md
[ADR-0034]: ../adr/adr-0034-team-database-access.md
[ADR-0030]: ../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[RC-245]: ./reviews/rc-245-mil-048-plan.md
[RC-247]: ./reviews/rc-247-mil-048-code.md
[RC-285]: ./reviews/rc-285-mil-048-gate.md
[876210f]: https://github.com/NF-Hotel/NFHotel/commit/876210f2c9a76d999896850121312db69f66da6a
[53e3a9e]: https://github.com/NF-Hotel/NFHotel/commit/53e3a9e61c8a078f9472805a6616ae0533e5c569
[d727221]: https://github.com/NF-Hotel/NFHotel/commit/d7272215314e41b10b2aa2e609b1ae910439b1f7
[60b8df3]: https://github.com/NF-Hotel/NFHotel/commit/60b8df3278ad0b07a1ac873073ef5a14d43c4bc3
