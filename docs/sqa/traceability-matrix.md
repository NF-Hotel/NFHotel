# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 |
| CrossReference | [BC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-27 | Proposed | mikemike | AI as stand-in for S01 | The row of the code of [MIL-091]: the test is `start-target.test.js` again, its checks run as tests of `node:test`, and the second analysis of SonarQube Cloud is answered (RC-451 F9); mode autonomous, not independent | [400486f] |
| 2026-11-27 | Proposed | mikemike | AI as stand-in for S01 | The row of the code of [MIL-091]: `register-start-task.ps1` sets the default of its folder in its body, found at the installation of task 3 (RC-451 F10); mode autonomous, not independent | pending |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003], [MIL-004], [MIL-005], [MIL-006], [MIL-007], [MIL-008], [MIL-009], [MIL-010], [MIL-021], [MIL-022], [MIL-023], [UCD-001], [US-001], [MIL-055], [MIL-071], [MIL-073], [MIL-074], [MIL-075], [MIL-076], [MIL-077], [MIL-078], [MIL-084], [MIL-085], [MIL-086], [MIL-087], [MIL-088], [MIL-089], [MIL-090], [MIL-091] | [RC-021], [RC-024], [RC-037], [RC-040], [RC-044], [RC-088], [RC-332], [RC-401], [RC-414] |
| [SA-001] | Stakeholder Analysis | en | it | [BC-001] | - | [RC-009], [RC-026], [RC-390] |
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
| [UCD-001] | Use Case Diagram | en | it | [SA-001], [BC-001] | [US-001], [UC-007], [UC-008], [UC-009], [UC-010], [UC-011] | [RC-013], [RC-028], [RC-368], [RC-390], [RC-417], [RC-435] |
| [US-001] | User Story | en | it | [UCD-001], [BC-001] | [MIL-055], [UC-007] (US-001.12), [UC-010] (US-001.13), [UC-011] (US-001.14) | [RC-012], [RC-027], [RC-263], [RC-390], [RC-417], [RC-435] |
| [DICT-001] | Domain Dictionary | en | it | [BC-001], [SA-001], [UC-007], [UC-010], [UC-011] | [DM-004], [OC-001], [OC-002], [SD-001] | [RC-029], [RC-264], [RC-390], [RC-417], [RC-427], [RC-435] |
| [UC-001] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-001] | [RC-030], [RC-417], [RC-423], [RC-427], [RC-435] |
| [UC-002] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-002] | [RC-031] |
| [UC-003] | Use Case | en | it | [UCD-001], [US-001], [SA-001] | [DM-003] | [RC-032], [RC-423] |
| [DM-001] | Domain Model | en | it | [UC-001], [UCD-001], [DICT-001] | [DM-004] | [RC-033], [RC-417], [RC-427] |
| [DM-002] | Domain Model | en | it | [UC-002], [UCD-001], [DICT-001] | [DM-004] | [RC-034] |
| [DM-003] | Domain Model | en | it | [UC-003], [UCD-001], [DICT-001] | [DM-004] | [RC-035] |
| [DM-004] | Domain Model | en | it | [UC-001], [UC-002], [UC-003], [DM-001], [DM-002], [DM-003], [DICT-001], [UC-007], [UC-010], [SSD-001], [UC-011], [SSD-002] | [OC-001], [OC-002] | [RC-036], [RC-264], [RC-390], [RC-417], [RC-435] |
| `tests/NFHotel.Proxmox.Tests`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-001] | - | [RC-004] |
| `deploy/`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-002] | - | [RC-007] |
| `deploy/proxmox`, `scripts/pve-token.sh`, `scripts/run-proxmox-tests.sh` | Source code | - | - | [MIL-003] | - | [RC-011] |
| `deploy/market-analysis`, `deploy/nginx`, `scripts/test-compose-proxy.sh` | Source code | - | - | [MIL-004], [MIL-026] (the pin of the service) | - | [RC-016], [RC-380] |
| [MIL-026] | Milestone (Accepted; its code reviewed again after the fixes, RC-376 Go; the gate is open) | en | it | [BC-001], [US-001], [MIL-004], MIL-019, MIL-020, [MIL-028] | [MIL-055], code and tests below | [RC-114], [RC-117], [RC-120], [RC-125], [RC-126], [RC-128], [RC-130], [RC-253], [RC-267], [RC-268], [RC-269] |
| `src/` code of [MIL-026]: the Domain types `MarketAnalysisRun`, `BookingMarketDetail`, `BookingMarketValues` and `MarketAnalysisErrors`; the ports, DTOs and use cases of the market analysis in `NFHotel.Application`; `NFHotel.External/MarketAnalysis` (client, options, mapper, series); the configurations, the repositories, `MarketAnalysisBookingQueries` and `Seeding/MarketSeed*` in `NFHotel.Infrastructure`; `MarketAnalysisController` and the run launcher and worker in `NFHotel.Api`; `ApiMarketAnalysisService`; `Features/Admin/MarketAnalysis` and the charts of `Features/Admin/Shared` in `NFHotel.Web` | Source code | - | - | [MIL-026] tasks 1 to 27 | - | [RC-115], [RC-118], [RC-121], [RC-270], [RC-373] (No-Go), [RC-374] (No-Go), [RC-375] (No-Go), [RC-376] (Go) |
| Tests of [MIL-026]: `tests/NFHotel.External.Tests/MarketAnalysis`, the market analysis tests of `NFHotel.Application.Tests`, `NFHotel.Api.Tests`, `NFHotel.Infrastructure.Tests` and `NFHotel.Web.Tests`, and its test doubles in `NFHotel.TestSupport` | Test code | - | - | [MIL-026] tasks 1 to 27 | - | [RC-270], [RC-373] (No-Go), [RC-374] (No-Go), [RC-375] (No-Go), [RC-376] (Go) |
| `ops/tests/test-market-analysis-pin.sh` and the job `Ops market analysis pin (MIL-026)` of `.github/workflows/ops-ci.yaml` | Source code | - | - | [MIL-026] task 16 and criterion 16, [RC-380] (F3) | - | [RC-376] (Go) |
| [MIL-028] | Milestone (Accepted; gate No-Go for now, 2026-11-15, RC-382: criterion 9 fails on QC-PY-001 of the service, RC-381; S01 decides on a rework milestone or a risk exception) | en | it | [BC-001], [US-001], [MIL-004] | the code of NF-Hotel/notebook-market-analysis below, [MIL-026], [MIL-055], [MIL-071] | [RC-124], [RC-127], [RC-129], [RC-382] |
| The code of NF-Hotel/notebook-market-analysis for [MIL-028]: `src/app`, `tests`, `openapi/market-contract.yaml`, `deploy/` and the Dockerfile, at the pin 97ca890 with the tests and YAML fixes merged in d46df37 | Source code | - | - | [MIL-028] tasks 1 to 6 | the pin of [MIL-026] (`deploy/market-analysis`) | [RC-381] ([QC-PY-001] No-Go, [QC-YML-001] Go) |
| [MIL-055] | Milestone (Accepted, Go; its rule for the suggestions is replaced by [MIL-071]; gate passed 2026-11-14, RC-379, accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [US-001], [MIL-026], [MIL-028], [MIL-030] | [UC-007], [MIL-071], `docs/wiki/Price-Suggestions.md`, code and tests below | [RC-262], [RC-265], [RC-266], [RC-377], [RC-378], [RC-379] |
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
| [MIL-073] | Milestone (Accepted, Go; plan reviewed in RC-333 and accepted 2026-11-14 by the AI as stand-in for S01, at S01's word in chat; deployed 2026-11-14 by CD run 43590287146; gate passed 2026-11-14, RC-362: Go, accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [DSA-001], [ADR-0031], [ADR-0040], MIL-024, MIL-064, MIL-065 | planned: the pin of the upstream build of `postgres:16-alpine` with the fixed `krb5-libs` and the documents (task 1), the code review and the pull request (task 2), the deploy with the row counts (task 3) and the gate (task 4) | [RC-333], [RC-362] |
| The code of MIL-073: the pin of `postgres:16-alpine` in `ops/deploy/postgres/docker-compose.yml` and the wiki page Containers-and-Images | YAML (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request; met on #693, merged as 606698e and deployed by CD run 43590287146) | - | - | [MIL-073] tasks 1 and 2, [ADR-0040], [DSA-001] | - | [RC-359], [RC-362] |
| [MIL-074] | Milestone (Accepted, Go; plan reviewed in RC-334 and accepted 2026-11-14 by the AI as stand-in for S01, at S01's word in chat; tasks 1 to 3 reviewed 2026-11-14, RC-361, Go-with-conditions; gate passed 2026-11-16, RC-394, Go, after the restart of the backup host, and accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [DSA-001], [ADR-0008], [ADR-0020], MIL-021, MIL-023, MIL-069 | `backup-database.sh --copy-only` with a lock and the units `nfhotel-offvm-copy.service` and `.timer` (task 1), the report `backup_host_seen` and the rule `BackupTargetDown` (task 2) and the documents (task 3): the code below; the scheduled task on the backup host and the start of Docker Desktop at sign-in (task 4), the deploy and its checks (task 5) and the gate (task 6) | [RC-334], [RC-361], [RC-394] |
| The code of MIL-074: `--copy-only`, the lock and the report `backup_host_seen` of `ops/scripts/backup-database.sh`, the units `nfhotel-offvm-copy.service` and `.timer`, `install-backup.sh` and the update command of `deploy-release.sh`, their tests, the rule `BackupTargetDown` and its rule tests, `start-target.js` and `register-start-task.ps1` of the backup target, the wiki pages Disaster-Recovery, Alerts, Monitoring and CI-CD, and the row of 2026-11-14 of ADR-0008 | Shell, YAML, systemd, JScript, PowerShell, ADR (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request and the restart by S01; the conditions met at the gate 2026-11-16, RC-394) | - | - | [MIL-074] tasks 1 to 3, [ADR-0008], [ADR-0020] | - | [RC-361], [RC-394] |
| [MIL-075] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-335; tasks 1 to 3 reviewed 2026-11-14, RC-360, Go-with-conditions; gate passed 2026-11-16, RC-396, Go, with criterion 4 waived by S01 in chat, and accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [ADR-0004], [ADR-0024], MIL-015, MIL-035 | the passport key off the web's list, loop and compose file, the secrets of every service left out of every settings file with a warning, and the deletion of a secret file that a service no longer lists (task 1), the web started without the key (task 2) and the documents (task 3): the code below; the deploy with the checks in the VM (task 4) and the gate (task 5) | [RC-335], [RC-360], [RC-396] |
| The code of MIL-075: the lists and the filter of `ops/scripts/write-secret-files.sh` and its test, `ops/deploy/nfhotel-web/docker-compose.yml` and `nfhotel-web.env.example`, the comment in `src/NFHotel.Web/Program.cs`, `tests/NFHotel.Web.Tests/PassportKeyTests.cs`, `scripts/dev-up.sh`, the wiki pages Environment-Files and Proxmox-Deployment, and the row of 2026-11-14 of ADR-0024 | Shell, C#, YAML, ADR (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request and task 4; the conditions met at the gate 2026-11-16, RC-396, with criterion 4 waived by S01) | - | - | [MIL-075] tasks 1 to 3, [ADR-0004], [ADR-0024] | - | [RC-360], [RC-396] |
| [MIL-076] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-336) | en | it | [BC-001], [ADR-0008], [ADR-0020], ADR-0025, MIL-021, MIL-023, MIL-074 | planned: the read-back of every dump with two `pg_restore` readers and the reports `backup_check`, `nfhotel_backup_tables` and `nfhotel_backup_bytes` (task 1), the rules `BackupUnreadable` and `BackupShrunk` (task 2), the documents (task 3), the deploy and the checks (task 4), seven nights and the gate (task 5) | [RC-336] |
| The code of MIL-076: the read-back, the report `backup_check` and the metrics `nfhotel_backup_tables` and `nfhotel_backup_bytes` of `ops/scripts/backup-database.sh`, the test `ops/tests/test-backup-read-back.sh` and the read-back in `ops/tests/test-database.sh`, the new script test of Ops CI, the rules `BackupUnreadable` and `BackupShrunk` and their rule tests, the wiki pages Disaster-Recovery, Alerts, Monitoring, CI-CD, Technology-Choices and Workflows, and the row of 2026-11-14 of ADR-0008; since 2026-11-15 `TaskReportMissing` watches `backup_check` (F3 of RC-364 no longer applies) | Shell, YAML, workflow, ADR (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request; the change of 2026-11-15 reviewed, Go) | - | - | [MIL-076] tasks 1 to 3, [ADR-0008], [ADR-0020] | - | [RC-364], [RC-383] |
| [MIL-077] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-337; tasks 1 to 4 reviewed 2026-11-14, RC-363, Go-with-conditions; gate passed 2026-11-16, RC-395, Go, after the first weekly run with the API scan, and accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [ADR-0002], [ADR-0004], MIL-015, MIL-027, MIL-044, MIL-067 | the test of hostile input over every endpoint of the API and its fixes (task 1), the OpenAPI document made at build time and not served (task 2), the ZAP API scan in the weekly run (task 3) and the documents (task 4): the code below; the gate after the first weekly run (task 5) | [RC-337], [RC-363], [RC-395] |
| The code of MIL-077: `InputGuardFilter`, `InputRules` and `ApiSecurityHeaders` of the API, `ApiErrorCodes`, the limits of the request records and the controllers, the OpenAPI document at build time (`OpenApiDocumentBuild` and the project file), the two search boxes of the back office, their tests, the API scan in `ops/scripts/dast-scan.sh` and `e2e-stack.sh` with their tests, the action `dast-scan` and `e2e-dast.yaml`, `ops/security/zap-rules.tsv`, and the wiki pages Security-Controls, Workflows, DevOps-Lifecycle and Technology-Choices | C#, Razor, Shell, YAML, workflow (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request and the weekly run; the conditions met at the gate 2026-11-16, RC-395) | - | - | [MIL-077] tasks 1 to 4, [ADR-0002], [ADR-0004], [ADR-0032] | - | [RC-363], [RC-395] |
| [MIL-078] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-338) | en | it | [BC-001], [ADR-0037], ADR-0027, ADR-0028, ADR-0029, MIL-038, MIL-040, MIL-041, MIL-072 | [ADR-0046] (task 1, Accepted, RC-386); the routes and Access in `ops/infra/cloudflare/` with `import` blocks (task 2), the weekly comparison that opens and closes an issue (task 3) and the documents (task 4), reviewed in [RC-387] (Go-with-conditions); planned: the two tokens, the first plan and the drill with S01 (task 5) and the gate (task 6) | [RC-338] |
| [MIL-051] | Milestone (Accepted, Go; plan reviewed in RC-254; revised 2026-11-14, the ADR without a fixed number and the target 2026-11-27, and accepted by the AI as stand-in for S01 at S01's word in chat, RC-356; gate No-Go for now, 2026-11-15, RC-384: criterion 7 fails on the live tailnet filter, and the risk exception that S01 chose waits for the approval of S04) | en | it | [BC-001], ADR-0007, ADR-0020, MIL-023 | planned: the ADR on a push or pull deploy (task 1), the automatic deploy with its health check and rollback (tasks 2 and 3), the alert on a failed or lagging deploy (task 4), the tailnet policy (task 5), the tests in Ops CI (task 6), the wiki and the first deploy through the new flow (task 7) | [RC-254], [RC-356], [RC-384] |
| The code of MIL-051: the pull `ops/scripts/pull-monitoring.sh` with its units in `ops/deploy/monitoring/systemd/` and the fourth step of `ops/scripts/create-monitoring-lxc.sh`, the health check of `ops/scripts/deploy-monitoring.sh`, the report folder of the scan exporter, the rules of `prometheus/rules/monitoring.yml` with their rule tests, the tests `ops/tests/test-pull-monitoring.sh`, `test-monitoring-stack.sh` and `test-create-monitoring-lxc.sh`, the new script test of Ops CI, and the wiki pages Monitoring, Alerts, CI-CD, Disaster-Recovery, Container-Escape and Workflows; the first change deployed by the pull, `backup_check` in `TaskReportMissing` of `prometheus/rules/nfhotel.yml` with its rule tests (pull request 759) | Shell, Python, YAML, workflow (reviewed 2026-11-14, Go-with-conditions: the checks of the pull request; the change of pull request 759 reviewed 2026-11-15, Go) | - | - | [MIL-051] tasks 2 to 7, [ADR-0044], [ADR-0020], [MIL-053] | - | [RC-366], [RC-383] |
| [MIL-052] | Milestone (Accepted, Go; plan reviewed in RC-255; revised 2026-11-14, the ADR without a fixed number and the target 2026-12-02, and accepted by the AI as stand-in for S01 at S01's word in chat, RC-357; amended 2026-11-17 to S01's decisions, the Editor role, no expiry date and the account disabled to stop the access, RC-398; gate No-Go for now, 2026-11-17, RC-399: criterion 6 fails on the live tailnet filter) | en | it | [BC-001], ADR-0020, [MIL-051], [ADR-0045] | the logs checked for personal data (task 2, 2026-11-15) and [ADR-0045] (task 3, Accepted, RC-385, amended RC-398), with the section on the AI read access in `docs/wiki/Monitoring.md`; the service account and `mcp-grafana` set up by S01 (task 4, 2026-11-17); the tools that only read, a question about a past alert and the stop of the access shown (task 5, 2026-11-17, RC-399); planned: the tailnet policy reconciled with the wiki (task 1, with F5 of RC-384), the gate (task 6) | [RC-255], [RC-357], [RC-398], [RC-399] |
| [MIL-053] | Milestone (Accepted, Go; plan reviewed in RC-256; gate passed 2026-11-15, RC-372: Go, accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [ADR-0007], [ADR-0020], [MIL-051] | the scan exporter, its service, token and scrape job, the rules `scans.yml`, the dashboard "Security scans" and their tests (the row below); the wiki pages Monitoring, Alerts and Containers-and-Images | [RC-256], [RC-257], [RC-372] |
| The code of MIL-053: the scan exporter in `ops/deploy/monitoring/scan-exporter/` with its unit and integration tests, the service `scan-exporter` of `ops/deploy/monitoring/docker-compose.yml`, the token file of `ops/scripts/deploy-monitoring.sh`, the job `scans` of Prometheus, `prometheus/rules/scans.yml` with its tests in `rules-test.yml`, the dashboard `grafana/dashboards/nfhotel-security-scans.json`, `ops/tests/test-scan-exporter.sh` and the stack test `ops/tests/test-monitoring-stack.sh`, and the wiki pages Monitoring, Alerts and Containers-and-Images | Python, Shell, YAML (reviewed 2026-11-01, Go; deployed to `nf-monitoring`; gate 2026-11-15, RC-372) | - | - | [MIL-053] tasks 1 to 6, [ADR-0020] | - | [RC-257], [RC-277], [RC-366], [RC-372] |
| [MIL-018] | Milestone (Accepted, Go; plan reviewed in RC-084; task 1 done, RC-083; revised 2026-11-14, the read closed by MIL-079 noted, the ADR without a fixed number and the target 2026-12-11, and accepted by the AI as stand-in for S01 at S01's word in chat, RC-358; tasks 2 to 6 reviewed 2026-11-15, RC-388 to RC-391, Go; gate passed 2026-11-15, RC-392, Go, with task 7 on the deployed site; accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], MIL-016, MIL-017, ADR-0005, [MIL-079] | [ADR-0047] (task 2), [UC-010] with [SSD-001] and [OC-001] (task 3), the code below (tasks 3 to 6), the amendments of [SA-001], [US-001], [UCD-001], UC-004, [DM-004] and [DICT-001]; the proof on the deployed site (task 7) and the gate (task 8) | [RC-083], [RC-084], [RC-358], [RC-388], [RC-389], [RC-390], [RC-391], [RC-392] |
| The code of MIL-018: the guest sign-in `GuestAccessService` and the rule `GuestOwnership` with the ports `IGuestTokenIssuer` and `IGuestCallContext`, `JwtGuestTokenIssuer`, and the throttle that counts an attempt before the read (`ILoginThrottle.TryCountAttempt`); `GuestAuthController`, `GuestAuthentication`, `ApiTokenValidation`, `HttpGuestCallContext` and the guest permission of `RequiresPermissionAttribute`; the pages `GuestSignIn.razor` and `MyBooking.razor` with their view models, the guest cookie of `GuestSignInRegistration`, `GuestSessionTickets`, `GuestTokenHolder` and `UseCaseRunner`; `ApiGuestAccessService`; `ops/scripts/check-guest-login.sh` and the guest part of `ops/tests/check-e2e-stack.sh`; and their tests | C#, Razor, shell (reviewed 2026-11-15, Go, RC-391; the fixes for the two notes of GitHub code quality reviewed 2026-11-15, Go, RC-393) | - | - | [MIL-018] tasks 3 to 6, [ADR-0047], [UC-010], [OC-001] | - | [RC-391], [RC-392], [RC-393] |
| The code of MIL-070: `as_app` of `ops/scripts/deploy-release.sh`, its test `ops/tests/test-sudo-log.sh` and its entry in the matrix of `.github/workflows/ops-ci.yaml`, the section "No secret in the log of sudo" of the wiki page Host-Hardening, the section "A new password for `nfhotel_app`" of the wiki page Database-Preparation, and the script tests of the wiki page Workflows | Shell, YAML (reviewed 2026-11-08, Go; deployed to the VM 2026-11-08, RC-321) | - | - | [MIL-070] tasks 1 to 3, [ADR-0041], [CIS-001] | - | [RC-320], [RC-321] |
| The code of MIL-072: `location ~* ^/admin` in `ops/deploy/nginx/templates/public.conf.template`, its regression test in `ops/tests/test-nginx.sh`, which Ops CI runs, the rows of 2026-11-11 of ADR-0029, and the table, the setup of the back office and "Check it" of the wiki page Public-Access | Shell, nginx (reviewed 2026-11-11, Go, accepted by S01; deployed 2026-11-12 by CD run 43262765251 and checked inside the VM 2026-11-14, RC-339) | - | - | [MIL-072] tasks 2 to 4, ADR-0029 | - | [RC-331], [RC-339] |
| [MIL-079] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-12, RC-340; deployed 2026-11-12 by CD run 43256465429; gate passed 2026-11-14, RC-342: reviewed 2026-11-12, Go, and accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [ADR-0004], ADR-0005, MIL-016, MIL-018, UC-004, BR-C-06 | planned: `Bookings.View` on the read of a booking by id, the lookup `POST api/bookings/public/receipt` by the number and the email of the booking with the throttle per booking, the page "Find your booking", the receipt on the booking page, BR-C-06 and the test plan, the wiki pages Security-Controls and Public-Access, and the evidence folder `docs/sqa/evidence/mil-079` | [RC-340], [RC-341], [RC-342] |
| The code of MIL-079: the read by id in `BookingsController` and `BookingService`, the lookup `LookUpReceiptAsync` in `IBookingService`, `BookingService`, `BookingsController` and `ApiBookingService` with `BookingReceiptDto` and `BookingReceiptRequest`, the sweep of `InMemoryLoginThrottle`, the pages `BookingReceipt.razor`, `BookingReceiptCard.razor` and `Book.razor` with their view models, the footer link of `PublicLayout.razor`, and their tests | C#, Razor (reviewed 2026-11-12, Go; criterion 21 on the risk exception of [RC-278]; deployed 2026-11-12 by CD run 43256465429, RC-342) | - | - | [MIL-079] tasks 1 to 4, UC-004, BR-C-06 | - | [RC-341], [RC-342] |
| [MIL-080] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-343; deployed 2026-11-13 by CD run 43498058217; the monitoring deployed 2026-11-14 at S01's word in chat; gate passed 2026-11-14, RC-347) | en | it | [BC-001], [CIS-001], [DSA-001], ADR-0018, ADR-0020, MIL-023, MIL-031 | planned: the profiles `ops/security/container-profiles.tsv`, the check `ops/scripts/check-container-runtime.sh` (configuration and kernel view of each main process), the deploy gate and the timer every 15 minutes, the alerts, the check in the monitoring LXC, the unit, live and end-to-end tests, [CIS-001] and the wiki pages Host-Hardening and Monitoring | [RC-343], [RC-347] |
| The code of MIL-080: the profiles `ops/security/container-profiles.tsv`, the check `ops/scripts/check-container-runtime.sh` with its unit and live tests and its step in the end-to-end action, the deploy gate, the service and the timer (`ops/scripts/deploy-release.sh`, `ops/deploy/systemd/`), the alerts `ContainerRuntimeViolation` and `ContainerCheckStale` with their tests, the check in `ops/scripts/deploy-monitoring.sh`, and the limits of `results-owner` of the benchmark | Shell, YAML (reviewed 2026-11-13, Go-with-conditions, met: every check of #674 green; deployed to the VM 2026-11-13 and to the monitoring LXC 2026-11-14, RC-347) | - | - | [MIL-080] tasks 1 to 6, [CIS-001] | - | [RC-346], [RC-347] |
| [MIL-081] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-344; deployed 2026-11-13 by CD run 43506763115; gate passed 2026-11-13, RC-349) | en | it | [BC-001], [CIS-001], ADR-0020, ADR-0024, ADR-0040, MIL-065, MIL-080 | planned: Alloy's calls measured, the allowlist of the socket proxy in its own HAProxy configuration, the test matrix of `ops/tests/test-alloy-start.sh`, the probe of the proxy in the runtime check, ADR-0042, [CIS-001] row 5.31 and the wiki page Containers-and-Images | [RC-344], [RC-349] |
| The code of MIL-081: the allowlist `ops/deploy/alloy/socket-proxy/haproxy.cfg` in the image of the proxy (its `Dockerfile`), the environment of the proxy in `ops/deploy/alloy/docker-compose.yml`, the measurement and the matrix of `ops/tests/test-alloy-start.sh`, and the rule `proxy_allowlist` of `ops/scripts/check-container-runtime.sh` with its unit test | Shell, YAML, HAProxy (reviewed 2026-11-13, Go; deployed to the VM 2026-11-13, RC-349) | - | - | [MIL-081] tasks 1 to 5, [ADR-0042], [CIS-001] | - | [RC-348], [RC-349] |
| [MIL-082] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-12, RC-345; deployed 2026-11-13 by CD run 43513939028; gate passed 2026-11-14, RC-351: the map of the monitoring LXC from its deploy after MIL-083) | en | it | [BC-001], [CIS-001], [DSA-001], MIL-045, MIL-080, MIL-081 | planned: the host-access map from the runtime check, the wiki page Container-Escape, Ollama as a non-root user with its CI test, the runtime versions in the check, [CIS-001] rows 2.1 and 4.1 | [RC-345], [RC-351] |
| The code of MIL-082: Ollama as 10001 with the one-shot service `models-owner` in `ops/deploy/ollama/docker-compose.yml` and its rows in `ops/security/container-profiles.tsv`, the test `ops/tests/test-ollama-user.sh` and its job in `.github/workflows/ops-ci.yaml`, the rule `system_paths` and the metric `nfhotel_container_runtime_info` of `ops/scripts/check-container-runtime.sh` with their unit and live tests, the lines of the check in `ops/tests/test-monitoring-stack.sh`, and the wiki page Container-Escape | Shell, YAML (reviewed 2026-11-13, Go-with-conditions, met: every check of #676 green; deployed to the VM 2026-11-13, RC-351) | - | - | [MIL-082] tasks 1 to 5, [CIS-001] | - | [RC-350], [RC-351] |
| [MIL-083] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-14, RC-352; S01 accepted the plan in chat 2026-11-14; deployed 2026-11-14 by CD run 43574446338; gate passed 2026-11-14, RC-355) | en | it | [BC-001], [ADR-0020], [CIS-001], [MIL-023], MIL-080, MIL-082 | planned: ADR-0043, Docker's packages and `unattended-upgrades` in the monitoring LXC, the script that builds the LXC, a floor for `runc` in the runtime check, [CIS-001] rows 1.2.2 and 1.2.2.1 | [RC-352], [RC-355] |
| [MIL-085] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-19, RC-410, at S01's word in chat; gate passed 2026-11-19, RC-422 Go) | en | it | [BC-001], [ADR-0004], ADR-0005, [ADR-0047], [MIL-018], [MIL-079], [UC-010] | [ADR-0049] (task 1, Accepted, RC-416 and RC-419), the booking reference (a version 4 UUID) on the booking with a migration that only adds, the public and guest contracts and every page without the counting number, one lookup with the limit on wrong tries per reference and per address (tasks 2 to 4, RC-418), BR-109 and the documents that name the booking number (task 5, RC-417); the deploy and the check on the live site (task 6) and the gate, reviewed in [RC-422] (Go) | [RC-410], [RC-422] |
| [MIL-086] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-19, RC-411, at S01's word in chat; gate passed 2026-11-19, RC-425 Go) | en | it | [BC-001], [ADR-0004], [MIL-067], [MIL-077], [MIL-085] | [ADR-0050] (task 1, Accepted, RC-420 and RC-421), the table "Validation rules of the booking flow" in `docs/refactor/BusinessRules.md` (task 2), the tests written first with the hostile strings through every text field (task 3), one implementation of each rule in the domain used by the API and the pages: the guest fields, the country from a list, the normal form, the five years and the 30 nights, 1 to 4 guests, unknown fields and bodies over 64 kilobytes refused with a security event (tasks 4 and 5), the body limit and the timeouts in nginx (task 6), reviewed in RC-424; UC-001, UC-003, UC-010, the test plan, the rule ledger and the wiki page Security-Controls (task 7), reviewed in RC-423; the deploy and the check on the live site (task 8) and the gate, reviewed in [RC-425] (Go) | [RC-411], [RC-425] |
| [MIL-087] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-19, RC-412, at S01's word in chat; gate passed 2026-11-20, RC-430 Go) | en | it | [BC-001], [ADR-0004], ADR-0005, ADR-0015, [MIL-036], [MIL-085], [MIL-086] | [ADR-0051] (task 1, Accepted, RC-426; second version after the code review, RC-429), the table "Limits of the booking flow" in `docs/refactor/BusinessRules.md` (task 2), the tests written first (task 3), the address that nginx saw in `X-NFHotel-Client-Address`, carried by the web application and taken by the API only from a call with the service token (task 4), 60 searches a minute and 5 booking requests an hour and 20 a day per address in bounded stores and 3 coming public bookings per email in the database (task 5), the events `public_booking_made`, `guest_lookup_failed` and `limit_refused` with a masked email, the counter `nfhotel_security_events_total` and the alerts `BookingFlowRefusals` and `PublicBookingSurge` (task 6), reviewed in RC-428; UC-001, UC-010, DM-001, DICT-001, the class diagram and the wiki pages Security-Controls and Alerts (task 7), reviewed in RC-427; the deploy at 6f7bb80 and the check on the live site (task 8) and the gate, reviewed in [RC-430] (Go) | [RC-412], [RC-430] |
| [MIL-088] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-19, RC-413, at S01's word in chat; gate passed 2026-11-27, RC-437 Go) | en | it | [BC-001], [ADR-0004], [ADR-0024], [MIL-018], MIL-035, [MIL-085], [MIL-086], [MIL-087] | [ADR-0052] and [ADR-0053] (tasks 1 and 2, Accepted after two rounds of review each, RC-431 to RC-434; second versions after the code, RC-435); [UC-011] with [SSD-002], [OC-002] and [SD-001], US-001.14 and the amended UCD-001, DM-004, DICT-001, UC-001, UC-010, SSD-001 and OC-001 (task 3, RC-435); the tests written first (task 4); the booking request with its link of 30 minutes, the claim, the guest and the booking in one save under the lock `public_booking`, the mails per mailbox and the fuse, the clean-up every 10 minutes, the mail port with `SmtpMailSender`, the forwarder `mail-relay` and the mail catcher in the stacks, the pages to ask, verify and send again, and the events, the counter and the three alerts of the mail (tasks 5 to 8), reviewed in RC-436; the ledger and the wiki pages with the new page Mail-Relay (task 8), reviewed in RC-435; the relay in production, set by S01 on 2026-11-22 (task 9); the deploys of 3cfe3ab5 with the verification off and then on, S01's check on the live site, the corrected wiki page Mail-Relay and the gate (task 10), reviewed in [RC-437] (Go) | [RC-413], [RC-431], [RC-432], [RC-433], [RC-434], [RC-435], [RC-436], [RC-437] |
| [MIL-089] | Milestone (Accepted, Go; plan reviewed in autonomous mode by the AI as stand-in for S01 2026-11-19, RC-415, at S01's word in chat; gate passed 2026-11-27, RC-441 Go) | en | it | [BC-001], ADR-0015, [ADR-0047], MIL-027, [MIL-036], [MIL-054], [MIL-077], [MIL-085], [MIL-086], [MIL-087], [MIL-088], [SAR-001] | [SAR-002] (task 1, RC-438); the tests of the abuse cases with five transcripts of tests that fail without their control, and the fixes of the flow: the security events without an email ([ADR-0051] third version), the limit of the open circuits per caller in the web application with the client timeouts of nginx ([ADR-0051] decision 8, SAR-002 BF-02), `Referrer-Policy: same-origin`, the tests of the lock `public_booking` (task 2); the scans on 3cfe3ab and the rules of the scan renewed to 2026-12-18 (task 3); the load test with the booking flow, with the verification off and on (task 4); the verdicts on findings 3 to 6 with two tests of a guest token after a cancellation (task 5); version 0.3.0 and the wiki page Booking-Flow-Protection, the wiki reviewed in RC-442 (task 6); the code reviewed in RC-440; the deploy at dcbc88ce, the check on the live site and the gate (task 7), reviewed in [RC-441] (Go) | [RC-415], [RC-438], [RC-439], [RC-440], [RC-442], [RC-441] |
| [SAR-002] | Security audit report (Accepted) | en | it | [MIL-089], [MIL-085], [MIL-086], [MIL-087], [MIL-088], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0052], [ADR-0053], [SAR-001] | the findings BF-01 to BF-12 for S01 as the issues #867 to #878, rated by the OWASP Risk Rating; the tests and the fixes of [MIL-089] | [RC-438], [RC-441] |
| [MIL-090] | Milestone (Accepted, Go; plan prepared by the AI in supervised mode 2026-11-19 and accepted as it stands by S01 in chat the same day, RC-445) | en | it | [BC-001], ADR-0017, [ADR-0032], [ADR-0040], MIL-027, [MIL-036], [MIL-065], [MIL-066], [MIL-077], [MIL-086], [MIL-087], [MIL-089], [RC-313], [RC-363], [RC-395] | planned: the check of the nine third-party images of issue #840 (CVE-2026-97054) with a new pin or an ignore entry for each, the 41 exceptions of the image scan checked again before they lapse on 2026-12-08, a warning seven days before a lapse, tests that show alerts 647 and 668 of ZAP false, the drawings without comments, a test that finds a new `MarkupString`, Informational alerts as `note` in code scanning, and lines for the rules 10029, 10058, 90027 and 40025 | [RC-445] |
| [MIL-091] | Milestone (Accepted, Go; plan prepared by the AI in supervised mode 2026-11-27 and accepted as it stands by S01 in chat the same day, RC-450) | en | it | [BC-001], [DSA-001], [ADR-0008], [MIL-069], [MIL-074], [RC-361] | planned: the check of the port of the backup target and one restart in `start-target.js` when nothing listens while the host has its tailnet address, a delay of 5 minutes at sign-in in `register-start-task.ps1`, tests in Node.js in Ops CI, the installation and a drill on the backup host, and the description of `BackupTargetDown` with the wiki pages Disaster-Recovery, Alerts and Containers-and-Images | [RC-450] |
| The code of MIL-091: the check of the port and the one restart in `ops/deploy/backup-target/start-target.js`, the delay of 5 minutes at sign-in in `register-start-task.ps1`, the test `ops/tests/start-target.test.js` with `ops/tests/test-start-target.sh` and its entry among the script tests of `.github/workflows/ops-ci.yaml`, the description of `BackupTargetDown` with its rule test, and the wiki pages Disaster-Recovery, Alerts, Containers-and-Images and Monitoring | JScript, PowerShell, JavaScript (Node.js), Shell, YAML (reviewed 2026-11-27, Go-with-conditions: every required check of the pull request and the new script test green; the findings of SonarQube Cloud on the pull request fixed in the code, F8 and F9; the folder of the registration under Windows PowerShell 5.1, F10) | - | - | [MIL-091] tasks 1 and 2, [ADR-0008] | - | [RC-451] |
| The code of MIL-083: steps 2 and 3 of `ops/scripts/create-monitoring-lxc.sh` (Docker's packages from Docker's signed repository, the checkout and `install-patching.sh`) with its test, the floor of `runc` in `ops/scripts/check-container-runtime.sh` with its unit test and the metric `nfhotel_container_runtime_runc_below_floor`, the rule `ContainerRuntimeKnownEscape` with its promtool test, and the wiki pages Container-Escape, Monitoring, Host-Hardening and Alerts | Shell, YAML (reviewed 2026-11-14, Go-with-conditions; the LXC switched by hand 2026-11-14, RC-354; the conditions met at the gate, RC-355) | - | - | [MIL-083] tasks 3 to 6, [ADR-0043], [CIS-001] | - | [RC-354], [RC-355] |
| [ADR-0040] | ADR (Accepted) | - | - | [MIL-064], [ADR-0007], [ADR-0030], [MIL-022], [MIL-036], [MIL-059] | the patch layer of the proxy in `ops/deploy/nginx/Dockerfile` and its entry in `ops/scripts/build-release-images.sh`, the release references by name in `ops/scripts/cd-steps.sh` and `ops/scripts/deploy-release.sh`, the `docker` entry of `.github/dependabot.yml` (MIL-064 tasks 2 to 7, RC-300) | [RC-299], [RC-300] (the code) |
| [ADR-0041] | ADR (Accepted) | - | - | [MIL-070], [ADR-0011], [ADR-0024], [MIL-023], [CIS-001] | `as_app` of `ops/scripts/deploy-release.sh`, the second check of `ops/tests/test-sudo-log.sh`, and the sections of the wiki pages Host-Hardening and Database-Preparation (MIL-070 tasks 1 to 3, RC-320) | [RC-320] |
| [ADR-0042] | ADR (Accepted) | - | - | [MIL-081], [ADR-0020], [ADR-0024], [ADR-0040], [MIL-080], [CIS-001] | `ops/deploy/alloy/socket-proxy/haproxy.cfg` and its `Dockerfile`, the environment of the proxy in `ops/deploy/alloy/docker-compose.yml`, the matrix of `ops/tests/test-alloy-start.sh` and the rule `proxy_allowlist` of `ops/scripts/check-container-runtime.sh` (MIL-081 tasks 1 to 4, RC-348) | [RC-348] |
| [ADR-0043] | ADR (Accepted) | - | - | [MIL-083], [ADR-0020], [MIL-023], [MIL-080], [MIL-082], [CIS-001] | Docker's packages and `unattended-upgrades` in the monitoring LXC (2026-11-14), steps 2 and 3 of `ops/scripts/create-monitoring-lxc.sh`, the floor of `runc` in `ops/scripts/check-container-runtime.sh` with its alert (MIL-083 tasks 3 to 6, RC-354) | [RC-353], [RC-354] (the code) |
| [ADR-0044] | ADR (Accepted) | - | - | [MIL-051], [ADR-0020], [ADR-0007], [ADR-0018], [ADR-0038], [MIL-023], [MIL-053] | the deploy of the monitoring by a pull on its LXC (2026-11-14): `ops/scripts/pull-monitoring.sh` and its timer, the health check of `ops/scripts/deploy-monitoring.sh` and the alerts of `prometheus/rules/monitoring.yml` (MIL-051 tasks 2 to 7, RC-366) | [RC-365], [RC-366] (the code) |
| [ADR-0045] | ADR (Accepted) | - | - | [MIL-052], [ADR-0020], [ADR-0044], [MIL-051], [RC-384] | the read-only access of the AI assistant to the monitoring (2026-11-15; amended 2026-11-17 to S01's decisions: the Editor role, no expiry date, the account disabled to stop the access): Grafana with a service account through `mcp-grafana` v2.1.1 limited to its tools for metrics, alerts and dashboards and to tools that only read, and no logs, after the check of the logs (MIL-052 task 2); the set-up in `docs/wiki/Monitoring.md` | [RC-385], [RC-398] |
| [ADR-0046] | ADR (Accepted) | - | - | [MIL-078], ADR-0027, ADR-0028, ADR-0029, [ADR-0037], [ADR-0021], [MIL-072] | the routes of the Cloudflare tunnel and the Access application of the back office as code in OpenTofu, without a stored state (2026-11-15): `ops/infra/cloudflare/`, applied by S01 or S03 from their own machine, and the weekly comparison `edge-comparison.yaml` with a token that reads; the set-up in `docs/wiki/Public-Access.md` | [RC-386] |
| [ADR-0047] | ADR (Accepted) | - | - | [MIL-018], [BC-001], ADR-0005, [MIL-079] | the guest sign-in with the booking number and the email (2026-11-15): no account, the guest token, the one refusal and the throttle that counts each attempt first, the role Guest of the one table, ownership in the API, the guest cookie, and cancelled bookings refused; the code of MIL-018 | [RC-388], [RC-417] |
| [ADR-0048] | ADR (Accepted with the exceptions E1 to E4 by the AI as stand-in for S01; RC-402 Go; decision 6 changed 2026-11-19, RC-405 Go) | - | - | [MIL-084], ADR-0028, ADR-0029, [ADR-0037], [ADR-0021], [ADR-0046], [MIL-066], [DSA-001] | where `security.txt` is served (2026-11-18): one file in git on the public listener of nginx only, with the redirect of `/security.txt` and its own headers; the date as a value in git; the check in three kinds (RFC 9116, rule of the ADR, warning); the weekly look, without the front page since 2026-11-19; the policy in `SECURITY.md`; the code of MIL-084 | [RC-402], [RC-405] |
| [ADR-0049] | ADR (Accepted) | - | - | [MIL-085], [ADR-0047], ADR-0005, [MIL-079], [MIL-018], [MIL-087] | the booking reference (2026-11-19): a version 4 UUID in a column of its own, the counting key kept inside, one lookup, 5 failed attempts per reference and 20 per caller address in 15 minutes in bounded stores that never drop a used-up key, and the receipt as the only answer to a guest; the code of MIL-085, deployed 2026-11-19 at 7fd5bea | [RC-416], [RC-418], [RC-419], [RC-422] |
| [ADR-0050] | ADR (Accepted) | - | - | [MIL-086], [ADR-0001], [ADR-0002], [ADR-0004], [ADR-0049], [MIL-008], [MIL-067], [MIL-077] | where the rules of the booking input live (2026-11-19): one implementation of each rule in the domain (the format of each guest field as an attribute in Domain/Attributes, the limits of a stay as static members of Booking, the normal form and the countries as value objects), used by the API as the authority and by the pages for early feedback, with the table in `docs/refactor/BusinessRules.md`; the code of MIL-086, deployed 2026-11-19 at 886c8e8 | [RC-420], [RC-421], [RC-424], [RC-425] |
| [ADR-0051] | ADR (Accepted) | - | - | [MIL-087], [ADR-0049], [ADR-0050], [MIL-036], [MIL-088] | the limits of the booking flow and the address of a caller (2026-11-19): one table of the limits, 60 searches a minute and 5 booking requests an hour and 20 a day per caller address, 3 coming public bookings per email counted in the database, the address that nginx saw as the only address of a caller, bounded stores in the memory of the API, and the security events and their counter; the code of MIL-087, deployed 2026-11-19 at 6f7bb80; second version after the code review (2026-11-19): what a full store drops for each kind of key, a cap of 100,000 on a limit, the forwarded headers of the web application and the sign-in limit of the mobile app; third version in MIL-089 (2026-11-21): the security events hold the caller address and no email, and the limit per email is named `mailbox`, a stand-in decision that S01 can overrule; decision 8, at most 50 open circuits per caller in the web application (SAR-002 BF-02), a stand-in decision of the build session | [RC-426], [RC-428], [RC-429], [RC-430], [RC-439] |
| [ADR-0052] | ADR (Accepted) | - | - | [MIL-088], [MIL-085], [MIL-086], [MIL-087], [ADR-0047], [ADR-0049], [ADR-0050], [ADR-0051], [ADR-0053] | a booking takes a room once its email is verified (2026-11-19): a request that holds nothing, a single-use link of 30 minutes with the token after `#` and only its hash stored, a page whose button verifies, the request claimed before the booking, the guest and the booking in one save, the next free room after a lost race, one open request and 3 mails an hour and 5 a day per mailbox under one lock, the clean-up every 10 minutes, and the setting that keeps the booking at once until the relay exists; second version after the code (2026-11-20): the lock `public_booking` for every public booking, whatever fails in the second mail ends as the event of a mail not sent, and the risk of a commit whose answer is lost; decision 10 follows decision 6 of [ADR-0051] since MIL-089 (2026-11-21) and says why the route of the verification has no limit per caller, and decision 2 weighs the 30 minutes of the link against ASVS 6.5.5 | [RC-431], [RC-433], [RC-435], [RC-436], [RC-439] |
| [ADR-0053] | ADR (Accepted) | - | - | [MIL-088], [ADR-0024], [ADR-0034], [ADR-0035], [ADR-0047], [ADR-0051], [ADR-0052], MIL-027, [MIL-064] | mail from the application through a relay (2026-11-19): SMTP over TLS with MailKit behind a port, Gmail as the relay of production at S01's choice, a forwarder of HAProxy on a network of its own with the API as the only way out, the fuse of 400 mails a day with the alerts, and Mailpit with TLS from the first byte in the stacks; second version after the code (2026-11-20): one bound of 15 seconds for a send, the quit included | [RC-432], [RC-434], [RC-435], [RC-436] |
| [MIL-046] | Milestone (Accepted, Go; gate passed 2026-11-05) | en | it | [BC-001], [ADR-0031], MIL-043, MIL-045, US-001.04 | [ADR-0031] (decisions 2 and 3), `BenchmarkRequest` and `BenchmarkRunTracker` of its task 2 | [RC-238], [RC-239], [RC-283] |
| [ADR-0031] | ADR (Accepted; decisions 2 and 3 added 2026-10-26) | - | - | ADR-0005, ADR-0013, [ADR-0004], ADR-0024, MIL-020, MIL-043 | the sign-in keys and the benchmark run tracker of `src/NFHotel.Web`, [MIL-046] | [RC-227], [RC-239] |
| [MIL-056] | Milestone (Accepted, Go; gate passed 2026-11-05 after the amendment of criterion 3; the run on a Mac by S02 is recorded at S02's onboarding) | en | it | [BC-001], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | [ADR-0035], `scripts/dev-up.sh --lite`, `ops/scripts/lib/`, the Ops CI test and the macOS job, the wiki page Local Development Deploy | [RC-271], [RC-272], [RC-286] |
| [ADR-0035] | ADR (Accepted) | - | - | [MIL-056], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | the light mode of `scripts/dev-up.sh` | [RC-273] |
| [MIL-048] | Milestone (Accepted, Go; gate passed 2026-11-05, RC-292, after RC-285 Go-with-conditions) | en | it | [BC-001], [ADR-0011], [ADR-0034], [MIL-023], CIS-001, [DSA-001] | [ADR-0034], `ops/scripts/db/team-roles.sql`, `ops/scripts/manage-team-db-users.sh`, `ops/deploy/postgres-tailnet/`, the rule for 5432 in `nfhotel.nft`, the wiki page Database-Access | [RC-245], [RC-247], [RC-285] (Go-with-conditions), [RC-292] (the re-review, Go) |
| [ADR-0034] | ADR (Accepted) | - | - | [MIL-048], [ADR-0011], ADR-0024, CIS-001, [DSA-001] | the team roles, the relay on the tailnet and the firewall rule of MIL-048 | [RC-247] |
| [UC-007] | Use Case (Accepted; revised 2026-11-14 after [MIL-071], RC-377, accepted by the AI as stand-in for S01) | en | it | [UCD-001], [US-001], [SA-001], [MIL-055], [MIL-071] | [DM-004], [DICT-001], code and tests below | [RC-264] (Go with conditions), [RC-377] (Go) |
| `docs/wiki/Price-Suggestions.md` | Wiki page (Accepted 2026-11-14 by the AI as stand-in for S01, RC-328; reworded by [MIL-071] task 5; checked on the published wiki 2026-11-14) | en | - | [MIL-055] task 10, [MIL-071] task 5, [US-001], [UC-007] | - | [RC-328] |
| Dictionary terms and domain concepts: Room rate, Price change, Price suggestion, Season, Pricing settings; Audit event actions `PriceChangeApproved`, `PriceChangeReverted`, `PricingSettingsChanged`, `RoomRateChanged` | Domain terms (Accepted) | en | it | [UC-007], [US-001] | [DICT-001], [DM-004], `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`) | [RC-264] |
| `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`, `PriceChangeState`), `src/NFHotel.Application/UseCases/Pricing` (`PriceSuggestionService`, `PriceChangeService`, `PricingSettingsService`, `OccupancyObservationReader`; `SeasonRule` and `PriceSuggestionRule` of MIL-055 are removed by MIL-071 task 3, RC-326), `src/NFHotel.Application/UseCases/RoomRates` (`RoomRateService`), the price lookup in `BookingService` | Source code | - | - | [MIL-055] tasks 3 to 9, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions; Accepted 2026-11-14), [RC-378] (Go) |
| `src/NFHotel.Api/Controllers` (`RoomRatesController`, `PricingSettingsController`, `PriceSuggestionsController`, `PriceChangesController`), `src/NFHotel.ApiClient` (`ApiRoomRateService`, `ApiPricingSettingsService`, `ApiPriceSuggestionService`), permission `Prices.Manage` | Source code | - | - | [MIL-055] tasks 3, 5 and 7, [US-001], [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions; Accepted 2026-11-14), [RC-378] (Go) |
| `src/NFHotel.Infrastructure` (configurations, repositories and migrations `AddRoomRateAndAuditDetail`, `AddPricingSettings`, `AddPriceChange`) | Source code | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions; Accepted 2026-11-14), [RC-378] (Go) |
| `src/NFHotel.Web/Features/Admin/Pricing` (`RoomRates`, `PricingSettings`, `PriceSuggestions` pages and view models, the tabs `PricingTabs`), `AdminNav` | Source code | - | - | [MIL-055] tasks 3, 5 and 8, [MIL-071] task 4, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions; Accepted 2026-11-14), [RC-378] (Go), [RC-327] |
| Tests of the suggestions: `OccupancyObservationReaderTests`, `PriceSuggestionServiceTests`, `NoPriceRuleTests` (the tests `SeasonRuleTests` and `PriceSuggestionRuleTests` of MIL-055 are removed with the rule) | Test code | - | - | [MIL-055] tasks 4 and 7, [MIL-071] task 3, [US-001] | - | [RC-326], [RC-378] |
| Tests of the services: `RoomRateServiceTests`, `PricingSettingsServiceTests`, `PriceChangeServiceTests`, `PriceChangeAuditTests`, `BookingPriceTests` (Application.Tests); `ManagePricesPermissionTests`, `StaffRolePermissionsTests` | Test code | - | - | [MIL-055] tasks 3, 5, 7 and 9, [UC-007] | - | [RC-265], [RC-266], [RC-378] |
| Tests of the domain: `RoomRateTests`, `PricingSettingTests`, `PriceChangeTests`, `StaffAuditEventDetailTests` | Test code | - | - | [MIL-055] tasks 3, 5 and 6 | - | [RC-265], [RC-266] |
| Tests of the persistence and the warning log: `RoomRateRepositoryTests`, `PricingSettingRepositoryTests`, `PriceChangeRepositoryTests`, `BookingPriceTests` (Infrastructure.Tests), `PricingMigrationTests`, the region "Unique indexes of the pricing tables" of `ConflictPathTests`, `LoggerWarningLogTests` | Test code | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | [RC-265], [RC-266], [RC-378] |
| Tests of the API and the client: `RoomRatesEndpointTests`, `PricingSettingsEndpointTests`, `PriceSuggestionsEndpointTests`, `EndpointProtectionTests`, `RoomRateRequestTests`, `PricingSettingsRequestTests`, `PriceSuggestionRequestTests` | Test code | - | - | [MIL-055] tasks 3, 5 and 7, [UC-007] | - | [RC-265], [RC-266], [RC-378] |
| Tests of the pages: `RoomRatesViewModelTests`, `PricingSettingsViewModelTests`, `PriceSuggestionsViewModelTests`, `PriceSuggestionsPageTests`, `RoomListViewModelTests`, `AdminAccessTests`, `BackOfficeAccessTests` | Test code | - | - | [MIL-055] tasks 3, 5 and 8, [UC-007] | - | [RC-265], [RC-266] |
| `src/NFHotel.External/MarketAnalysis` (`MarketAnalysisServiceClient`, `MarketAnalysisResultMapper`), `src/NFHotel.Application/Dto/MarketAnalysis` (the `pricing` request and the `price_suggestions` DTOs), `MarketAnalysisRunner` (sends the settings), the copies `docs/test-data/market-contract.yaml` and `docs/test-data/price-suggestions-sample.json` | Source code | - | - | [MIL-071] task 2, objective 29 of [BC-001] | - | [RC-325] |
| Tests of the contract and the client: `MarketContractTests`, `MarketAnalysisResultMapper13Tests`, `MarketAnalysisServiceClientTests`, `MarketAnalysisRunnerTests` | Test code | - | - | [MIL-071] task 2 | - | [RC-325] |
| [UC-008] | Use Case (Accepted) | en | it | [UCD-001], [US-001], [SA-001], [MIL-019] | the reads of `BenchmarkController`, the pages `BenchmarkRuns` and `BenchmarkRunAnalysis` of `src/NFHotel.Web/Features/Admin/Benchmark` | [RC-369] |
| [UC-009] | Use Case (Accepted) | en | it | [UCD-001], [US-001], [SA-001], [MIL-020] | the pages `BenchmarkStart` and `ModelsPanel` and `BenchmarkRunTracker` of `src/NFHotel.Web/Features/Admin/Benchmark`, `BenchmarkServiceClient` of `src/NFHotel.External/Benchmark`, the record of a run in `BenchmarkController` and the default AI model in `AiDefaultsController` | [RC-370] |
| [UC-010] | Use Case (Accepted) | en | it | [UCD-001], [US-001] (US-001.13), [SA-001], [MIL-018], [ADR-0047], [ADR-0052] | [SSD-001], [OC-001], the code of MIL-018 | [RC-389], [RC-417], [RC-423], [RC-427], [RC-435] |
| [SSD-001] | System Sequence Diagram (Accepted) | en | it | [UC-010] | [OC-001] | [RC-389], [RC-417], [RC-435] |
| [OC-001] | Operation Contract (Accepted) | - | - | [SSD-001], [DM-004], [DICT-001] | the code of MIL-018 | [RC-389], [RC-417], [RC-435] |
| [UC-011] | Use Case (Accepted) | en | it | [UCD-001], [US-001] (US-001.14), [SA-001], [MIL-088], [ADR-0052], [ADR-0053] | [SSD-002], [OC-002], [SD-001], the code of MIL-088 | [RC-435] |
| [SSD-002] | System Sequence Diagram (Accepted) | en | it | [UC-011] | [OC-002] | [RC-435] |
| [OC-002] | Operation Contract (Accepted) | - | - | [SSD-002], [DM-004], [DICT-001] | [SD-001], the code of MIL-088 | [RC-435], [RC-439] |
| [SD-001] | Sequence Diagram (Accepted) | - | - | [OC-002], [SSD-002], [UC-011], [DICT-001] | the code of MIL-088: `BookingRequestService.VerifyAsync`, `BookingService.BookVerifiedAsync` and the mail with the reference | [RC-435], [RC-439] |
| [MIL-019] | Milestone (Accepted, Go; plan reviewed in RC-086; tasks 1 to 7 marked done after RC-104 and RC-105) | en | it | [BC-001], [MIL-018], [ADR-0004], ADR-0005 | ADR-0013, the code of its tasks 3 to 6 (`BenchmarkRun`, `BenchmarkService`, `BenchmarkController`, the page `/admin/benchmark`), [UC-008] | [RC-086], [RC-104], [RC-105] |
| [MIL-020] | Milestone (Accepted, Go; plan reviewed in RC-087; tasks 1 to 11 marked done after RC-106 to RC-113 and RC-249) | en | it | [BC-001], [MIL-019], [ADR-0003] | ADR-0014, the code and the deploy files of its tasks 1 to 3 and 5 to 11 (`BenchmarkServiceClient`, the page `/admin/benchmark/start` with its panel of the installed models, `AiDefault`, `BenchmarkRunAnalysis`), [UC-009] | [RC-087], [RC-106], [RC-107], [RC-108], [RC-109], [RC-110], [RC-111], [RC-112], [RC-113], [RC-249] |
| [MIL-054] | Milestone (Accepted, Go; plan reviewed and accepted by S01 in chat 2026-11-01, RC-258; gate passed 2026-11-14, RC-367: Go, accepted by the AI as stand-in for S01 at S01's word in chat) | en | it | [BC-001], [ADR-0033], MIL-047 | the tests of pull request #435, the coverage floor `scripts/check-coverage-floor.sh` and its test in `ci.yaml`, the coverage scope in `dotnet-coverage.config.xml` and `SonarQube.Analysis.xml` | [RC-258], [RC-260], [RC-367] |
| The code of `update` in `ops/scripts/deploy-release.sh` for a release older than the shallow checkout of the VM: the fetch of main back to a day before that release, the helpers `fetch_main` and `on_main` with the constants `MAIN_REF` and `DEEPEN_MARGIN_SECONDS`, its three checks in `ops/tests/test-deploy-release.sh`, and the section "Roll back" of the wiki page CI-CD | Shell (reviewed 2026-11-17, Go; not yet deployed) | - | - | [ADR-0018], [MIL-032], [MIL-021] criterion 6 | - | [RC-397] |
| [MIL-084] | Milestone (Accepted, Go; plan prepared by the AI in supervised mode 2026-11-17 and amended the same day for the standards it rests on; S01 accepted the amended plan in chat 2026-11-18, RC-400; autonomous mode since S01's word in chat 2026-11-19; gate passed 2026-11-19, RC-404 Go) | en | it | [BC-001], ADR-0028, ADR-0029, [ADR-0037], MIL-040, MIL-041, [MIL-066], [MIL-072], [MIL-078], [DSA-001] | [ADR-0048] (task 1, Accepted, RC-402) with its table of the requirements of RFC 9116 and RFC 8615; the extended `SECURITY.md` (task 2); the code below (tasks 3 to 5) and the wiki pages Public-Access, Incidents, Security-Controls and Workflows (task 6), reviewed in [RC-403] (Go-with-conditions), and the weekly look changed after its first run, reviewed in [RC-405] (Go); the deploy, the look from outside and the weekly look run by hand (task 7) and the gate (task 8), reviewed in [RC-404] (Go) | [RC-400], [RC-404] |
| The code of MIL-084: `ops/deploy/nginx/well-known/security.txt`, its location and the redirect of `/security.txt` in `ops/deploy/nginx/templates/public.conf.template`, the mount in `ops/deploy/nginx/docker-compose.yml`; `ops/scripts/check-security-txt.sh` with `ops/tests/test-check-security-txt.sh`, and the checks of `security.txt` in `ops/tests/test-nginx.sh`; the subcommands `look` and `look-report` of `ops/scripts/edge-comparison-steps.sh` with their tests in `ops/tests/test-edge-comparison.sh`; the job `look` of `.github/workflows/edge-comparison.yaml`, and the step and the script test of `.github/workflows/ops-ci.yaml` | Shell, nginx, YAML (reviewed 2026-11-19, Go-with-conditions; the weekly look without the front page reviewed 2026-11-19, Go; deployed 2026-11-19 at 46b29fb, and the look runs on GitHub from `main`) | - | - | [MIL-084] tasks 3 to 5, [ADR-0048] | - | [RC-403], [RC-405] |

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
[MIL-032]: ../milestones/mil-032-self-updating-deploy-command.md
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
[ADR-0043]: ../adr/adr-0043-docker-packages-and-security-updates-in-the-monitoring-lxc.md
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
[MIL-083]: ../milestones/mil-083-the-monitoring-lxc-patched-as-the-vm-is.md
[RC-343]: ./reviews/rc-343-mil-080.md
[RC-346]: ./reviews/rc-346-mil-080-code.md
[RC-347]: ./reviews/rc-347-mil-080-gate.md
[RC-344]: ./reviews/rc-344-mil-081.md
[RC-345]: ./reviews/rc-345-mil-082.md
[RC-348]: ./reviews/rc-348-mil-081-code.md
[RC-349]: ./reviews/rc-349-mil-081-gate.md
[RC-350]: ./reviews/rc-350-mil-082-code.md
[RC-351]: ./reviews/rc-351-mil-082-gate.md
[RC-352]: ./reviews/rc-352-mil-083.md
[RC-353]: ./reviews/rc-353-adr-0043.md
[RC-354]: ./reviews/rc-354-mil-083-code.md
[RC-355]: ./reviews/rc-355-mil-083-gate.md
[RC-342]: ./reviews/rc-342-mil-079-gate.md
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
[MIL-073]: ../milestones/mil-073-database-image-that-passes-the-gate.md
[MIL-074]: ../milestones/mil-074-the-off-vm-copy-within-its-time.md
[RC-332]: ./reviews/rc-332-bc-001-products-of-2026-11-11.md
[RC-333]: ./reviews/rc-333-mil-073.md
[RC-359]: ./reviews/rc-359-mil-073-code.md
[RC-334]: ./reviews/rc-334-mil-074.md
[MIL-075]: ../milestones/mil-075-the-passport-key-out-of-the-web-container.md
[MIL-076]: ../milestones/mil-076-every-nightly-backup-read-back.md
[MIL-077]: ../milestones/mil-077-hostile-input-against-the-api.md
[MIL-078]: ../milestones/mil-078-the-tunnel-routes-and-access-as-code.md
[RC-335]: ./reviews/rc-335-mil-075.md
[RC-360]: ./reviews/rc-360-mil-075-code.md
[RC-361]: ./reviews/rc-361-mil-074-code.md
[RC-362]: ./reviews/rc-362-mil-073-gate.md
[RC-363]: ./reviews/rc-363-mil-077-code.md
[RC-364]: ./reviews/rc-364-mil-076-code.md
[ADR-0044]: ../adr/adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[ADR-0045]: ../adr/adr-0045-read-only-monitoring-access-for-the-ai-assistant.md
[ADR-0046]: ../adr/adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[RC-365]: ./reviews/rc-365-adr-0044.md
[RC-366]: ./reviews/rc-366-mil-051-code.md
[RC-383]: ./reviews/rc-383-mil-051-first-pull-deploy.md
[RC-384]: ./reviews/rc-384-mil-051-gate.md
[RC-385]: ./reviews/rc-385-adr-0045.md
[RC-386]: ./reviews/rc-386-adr-0046.md
[RC-387]: ./reviews/rc-387-mil-078-code.md
[RC-256]: ./reviews/rc-256-mil-053-plan.md
[RC-257]: ./reviews/rc-257-mil-053-code.md
[RC-372]: ./reviews/rc-372-mil-053-gate.md
[RC-336]: ./reviews/rc-336-mil-076.md
[RC-337]: ./reviews/rc-337-mil-077.md
[RC-338]: ./reviews/rc-338-mil-078.md
[MIL-018]: ../milestones/mil-018-guest-login.md
[MIL-051]: ../milestones/mil-051-monitoring-continuous-deployment.md
[MIL-052]: ../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[RC-083]: ./reviews/rc-083-business-case-guest-login.md
[RC-084]: ./reviews/rc-084-milestone-018.md
[RC-254]: ./reviews/rc-254-mil-051-plan.md
[RC-255]: ./reviews/rc-255-mil-052-plan.md
[RC-356]: ./reviews/rc-356-mil-051-revision.md
[RC-357]: ./reviews/rc-357-mil-052-revision.md
[RC-358]: ./reviews/rc-358-mil-018-revision.md
[ADR-0047]: ../adr/adr-0047-guest-login.md
[UC-010]: ../use-cases/uc-010/uc.md
[SSD-001]: ../use-cases/uc-010/ssd.md
[OC-001]: ../use-cases/uc-010/oc.md
[UC-011]: ../use-cases/uc-011/uc.md
[SSD-002]: ../use-cases/uc-011/ssd.md
[OC-002]: ../use-cases/uc-011/oc.md
[SD-001]: ../use-cases/uc-011/sd.md
[RC-388]: ./reviews/rc-388-adr-0047.md
[RC-389]: ./reviews/rc-389-uc-010.md
[RC-390]: ./reviews/rc-390-mil-018-amendments.md
[RC-391]: ./reviews/rc-391-mil-018-code.md
[RC-392]: ./reviews/rc-392-mil-018-gate.md
[RC-393]: ./reviews/rc-393-mil-018-code-quality.md
[RC-394]: ./reviews/rc-394-mil-074-gate.md
[RC-395]: ./reviews/rc-395-mil-077-gate.md
[RC-396]: ./reviews/rc-396-mil-075-gate.md
[RC-397]: ./reviews/rc-397-deploy-update-older-release.md
[RC-398]: ./reviews/rc-398-mil-052-amendments.md
[RC-399]: ./reviews/rc-399-mil-052-gate.md
[UC-008]: ../use-cases/uc-008/uc.md
[UC-009]: ../use-cases/uc-009/uc.md
[MIL-019]: ../milestones/mil-019-benchmark-results-api.md
[MIL-020]: ../milestones/mil-020-start-benchmark-run.md
[RC-368]: ./reviews/rc-368-ucd-001-benchmark-actors.md
[RC-369]: ./reviews/rc-369-uc-008.md
[RC-370]: ./reviews/rc-370-uc-009.md
[RC-086]: ./reviews/rc-086-milestone-019.md
[RC-087]: ./reviews/rc-087-milestone-020.md
[RC-104]: ./reviews/rc-104-adr-0013.md
[RC-105]: ./reviews/rc-105-benchmark-results-code.md
[RC-106]: ./reviews/rc-106-start-benchmark-run-code.md
[RC-107]: ./reviews/rc-107-web-compose-yaml.md
[RC-108]: ./reviews/rc-108-delete-benchmark-run-code.md
[RC-109]: ./reviews/rc-109-adr-0014.md
[RC-110]: ./reviews/rc-110-default-ai-model-code.md
[RC-111]: ./reviews/rc-111-benchmark-analysis-view-code.md
[RC-112]: ./reviews/rc-112-analysis-model-name-code.md
[RC-113]: ./reviews/rc-113-manage-models-code.md
[RC-249]: ./reviews/rc-249-live-runs-code.md
[MIL-054]: ../milestones/mil-054-test-coverage.md
[ADR-0033]: ../adr/adr-0033-sonarqube-cloud-coverage.md
[RC-258]: ./reviews/rc-258-mil-054-plan.md
[RC-260]: ./reviews/rc-260-mil-054-code.md
[RC-367]: ./reviews/rc-367-mil-054-gate.md
[RC-377]: ./reviews/rc-377-uc-007-revision.md
[RC-378]: ./reviews/rc-378-mil-055-code-at-the-gate.md
[RC-379]: ./reviews/rc-379-mil-055-gate.md
[RC-380]: ./reviews/rc-380-mil-026-service-pin.md
[RC-114]: ./reviews/rc-114-mil-026.md
[RC-115]: ./reviews/rc-115-market-analysis-code.md
[RC-117]: ./reviews/rc-117-mil-026-amendment.md
[RC-118]: ./reviews/rc-118-market-analysis-bookings-code.md
[RC-120]: ./reviews/rc-120-mil-026-period-amendment.md
[RC-121]: ./reviews/rc-121-period-and-seed-code.md
[RC-125]: ./reviews/rc-125-mil-026-run-api-amendment.md
[RC-126]: ./reviews/rc-126-mil-026-chart-amendment.md
[RC-128]: ./reviews/rc-128-mil-026-occupancy-and-captions.md
[RC-130]: ./reviews/rc-130-mil-026-short-timeouts.md
[RC-253]: ./reviews/rc-253-mil-026-model-download.md
[RC-267]: ./reviews/rc-267-mil-026-markdown-interpretation-and-forecast.md
[RC-268]: ./reviews/rc-268-mil-026-follow-the-run.md
[RC-269]: ./reviews/rc-269-mil-026-markdown-evidence.md
[RC-270]: ./reviews/rc-270-mil-026-code-of-tasks-22-to-27.md
[RC-373]: ./reviews/rc-373-mil-026-code-of-tasks-13-to-16.md
[RC-374]: ./reviews/rc-374-mil-026-code-of-tasks-17-to-19.md
[RC-375]: ./reviews/rc-375-mil-026-code-of-tasks-20-and-21.md
[RC-376]: ./reviews/rc-376-mil-026-re-review-after-the-fixes.md
[RC-124]: ./reviews/rc-124-mil-028-market-analysis-run-api.md
[RC-127]: ./reviews/rc-127-mil-028-nine-analyses.md
[RC-129]: ./reviews/rc-129-mil-028-timeouts.md
[RC-381]: ./reviews/rc-381-mil-028-service-code.md
[RC-382]: ./reviews/rc-382-mil-028-gate.md
[MIL-084]: ../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[RC-400]: ./reviews/rc-400-mil-084.md
[RC-401]: ./reviews/rc-401-bc-001-disclosure.md
[ADR-0048]: ../adr/adr-0048-where-security-txt-is-served.md
[RC-402]: ./reviews/rc-402-adr-0048.md
[RC-403]: ./reviews/rc-403-mil-084-code.md
[RC-404]: ./reviews/rc-404-mil-084-gate.md
[RC-405]: ./reviews/rc-405-the-weekly-look-without-the-front-page.md
[MIL-085]: ../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-086]: ../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-088]: ../milestones/mil-088-a-booking-verified-by-email.md
[MIL-089]: ../milestones/mil-089-the-booking-flow-attacked-and-measured.md
[MIL-090]: ../milestones/mil-090-scan-findings-of-19-november.md
[MIL-091]: ../milestones/mil-091-the-backup-target-answers-on-its-port.md
[RC-410]: ./reviews/rc-410-mil-085.md
[RC-411]: ./reviews/rc-411-mil-086.md
[RC-412]: ./reviews/rc-412-mil-087.md
[RC-413]: ./reviews/rc-413-mil-088.md
[RC-414]: ./reviews/rc-414-bc-001-booking-flow.md
[RC-415]: ./reviews/rc-415-mil-089.md
[SAR-002]: ./security-audit-2026-12-booking-flow.md
[RC-438]: ./reviews/rc-438-sar-002.md
[RC-439]: ./reviews/rc-439-mil-089-documents.md
[RC-440]: ./reviews/rc-440-mil-089-code.md
[RC-441]: ./reviews/rc-441-mil-089-gate.md
[RC-442]: ./reviews/rc-442-mil-089-wiki.md
[ADR-0049]: ../adr/adr-0049-booking-reference.md
[RC-416]: ./reviews/rc-416-adr-0049.md
[RC-417]: ./reviews/rc-417-mil-085-documents.md
[RC-418]: ./reviews/rc-418-mil-085-code.md
[RC-419]: ./reviews/rc-419-adr-0049-after-the-code-review.md
[RC-422]: ./reviews/rc-422-mil-085-gate.md
[ADR-0050]: ../adr/adr-0050-where-the-rules-of-the-booking-input-live.md
[ADR-0051]: ../adr/adr-0051-limits-of-the-booking-flow-and-the-caller-address.md
[RC-426]: ./reviews/rc-426-adr-0051.md
[RC-427]: ./reviews/rc-427-mil-087-documents.md
[RC-428]: ./reviews/rc-428-mil-087-code.md
[RC-429]: ./reviews/rc-429-adr-0051-second-version.md
[ADR-0052]: ../adr/adr-0052-a-booking-takes-a-room-once-its-email-is-verified.md
[ADR-0053]: ../adr/adr-0053-mail-from-the-application-through-a-relay.md
[RC-431]: ./reviews/rc-431-adr-0052.md
[RC-432]: ./reviews/rc-432-adr-0053.md
[RC-433]: ./reviews/rc-433-adr-0052-second-round.md
[RC-434]: ./reviews/rc-434-adr-0053-second-round.md
[RC-435]: ./reviews/rc-435-mil-088-documents.md
[RC-436]: ./reviews/rc-436-mil-088-code.md
[RC-437]: ./reviews/rc-437-mil-088-gate.md
[RC-430]: ./reviews/rc-430-mil-087-gate.md
[RC-420]: ./reviews/rc-420-adr-0050.md
[RC-421]: ./reviews/rc-421-adr-0050-where-the-rules-live.md
[RC-423]: ./reviews/rc-423-mil-086-documents.md
[RC-424]: ./reviews/rc-424-mil-086-code.md
[RC-425]: ./reviews/rc-425-mil-086-gate.md
[RC-445]: ./reviews/rc-445-mil-090.md
[RC-450]: ./reviews/rc-450-mil-091.md
[RC-451]: ./reviews/rc-451-mil-091-code.md
[400486f]: https://github.com/NF-Hotel/NFHotel/commit/400486fc9a614d9b0f3a794e62dc0d3c2b678162
