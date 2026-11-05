# Traceability Matrix

## Metadata
| Key | Value |
| --- | --- |
| ID | TM-001 |
| CrossReference | [BC-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Proposed | Claude (AI) | S01 | Added MIL-061 (plan review RC-287, Go) | [ee31f9e] |
| 2026-11-05 | Proposed | Claude (AI) | S01 | The rows of ADR-0039 and of the code of MIL-061 (code review RC-288, Go); RC-288 on the row of MIL-061 | pending |

---

## Purpose

Tracks backward/forward links between artifact instances so that the Business Case's
cross-artifact traceability success criterion is measurable. A row is added or
updated whenever an artifact instance is created or reviewed.

## Traceability Table

| Artifact Instance | Type | Language | Domain | Upstream (Backward Link) | Downstream (Forward Link) | Last Reviewed (RC-ID) |
| --- | --- | --- | --- | --- | --- | --- |
| [BC-001] | Business Case | en | it | - | [SA-001], [MIL-001], [MIL-002], [MIL-003], [MIL-004], [MIL-005], [MIL-006], [MIL-007], [MIL-008], [MIL-009], [MIL-010], [MIL-021], [MIL-022], [MIL-023], [UCD-001], [US-001], [MIL-055] | [RC-021], [RC-024], [RC-037], [RC-040], [RC-044], [RC-088] |
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
| [MIL-023] | Milestone (Accepted; tasks 6 and 11 open at the gate) | en | it | [BC-001], [MIL-022], [ADR-0020] | CIS-001, SAMM-001, DORA-001, the host hardening and the firewall of `ops/deploy/host`, the monitoring stack and Alloy of `ops/deploy/monitoring` and `ops/deploy/alloy`, the wiki pages Host-Hardening, Monitoring and Alerts | [RC-091], [RC-169], [RC-172], [RC-176], [RC-177], [RC-179], [RC-189], [RC-191], [RC-192], [RC-193], [RC-244], [RC-505] (drill 1), [RC-284] (drill 2) |
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
| [MIL-055] | Milestone (Accepted, Go) | en | it | [BC-001], [US-001], [MIL-026], [MIL-028], [MIL-030] | [UC-007], `docs/wiki/Price-Suggestions.md`, code and tests below | [RC-262] |
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
| [MIL-061] | Milestone (Accepted, Go) | en | it | [BC-001], [ADR-0004], [ADR-0006], ADR-0015, ADR-0028, [ADR-0038], MIL-015, MIL-020, [MIL-026] | ADR-0039, the network `nfhotel-python` in `ops/scripts/deploy-services.sh` and `ops/scripts/lib/stack.sh`, the compose files of the web application, the API, the market analysis and the benchmark, nginx without the listener on 8081, `ops/tests/test-service-networks.sh` and its Ops CI job, the wiki pages Network Isolation, Containers and Images, Environment Files and Proxmox Deployment | [RC-287], [RC-288] (the code) |
| [ADR-0039] | ADR (Accepted) | - | - | [MIL-061], [ADR-0003], [ADR-0004], [ADR-0006], ADR-0015, ADR-0028 | the network `nfhotel-python` in `ops/scripts` and `ops/deploy`, nginx without the listener on 8081, `ops/tests/test-service-networks.sh` | [RC-288] |
| The code of MIL-061: `ops/scripts/deploy-services.sh`, `ops/scripts/lib/stack.sh`, the compose files and settings examples of `ops/deploy/nfhotel-web`, `nfhotel-api`, `market-analysis`, `benchmark` and `nginx`, `ops/tests/test-service-networks.sh` with its job in `.github/workflows/ops-ci.yaml`, and the tests that follow the move | Shell, compose and workflow files (built, not yet deployed) | - | - | [MIL-061] tasks 2 to 5 | - | [RC-288] |
| [MIL-046] | Milestone (Accepted, Go; gate passed 2026-11-05) | en | it | [BC-001], [ADR-0031], MIL-043, MIL-045, US-001.04 | [ADR-0031] (decisions 2 and 3), `BenchmarkRequest` and `BenchmarkRunTracker` of its task 2 | [RC-238], [RC-239], [RC-283] |
| [ADR-0031] | ADR (Accepted; decisions 2 and 3 added 2026-10-26) | - | - | ADR-0005, ADR-0013, [ADR-0004], ADR-0024, MIL-020, MIL-043 | the sign-in keys and the benchmark run tracker of `src/NFHotel.Web`, [MIL-046] | [RC-227], [RC-239] |
| [MIL-056] | Milestone (Accepted, Go; gate passed 2026-11-05 after the amendment of criterion 3; the run on a Mac by S02 is recorded at S02's onboarding) | en | it | [BC-001], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | [ADR-0035], `scripts/dev-up.sh --lite`, `ops/scripts/lib/`, the Ops CI test and the macOS job, the wiki page Local Development Deploy | [RC-271], [RC-272], [RC-286] |
| [ADR-0035] | ADR (Accepted) | - | - | [MIL-056], [ADR-0003], ADR-0015, ADR-0024, MIL-027 | the light mode of `scripts/dev-up.sh` | [RC-273] |
| [MIL-048] | Milestone (Accepted, Go; tasks 4 and 6 open: the Tailscale grant and the people of the team) | en | it | [BC-001], [ADR-0011], [ADR-0034], [MIL-023], CIS-001, [DSA-001] | [ADR-0034], `ops/scripts/db/team-roles.sql`, `ops/scripts/manage-team-db-users.sh`, `ops/deploy/postgres-tailnet/`, the rule for 5432 in `nfhotel.nft`, the wiki page Database-Access | [RC-245], [RC-247], [RC-285] (Go-with-conditions) |
| [ADR-0034] | ADR (Accepted) | - | - | [MIL-048], [ADR-0011], ADR-0024, CIS-001, [DSA-001] | the team roles, the relay on the tailnet and the firewall rule of MIL-048 | [RC-247] |
| [UC-007] | Use Case (Accepted) | en | it | [UCD-001], [US-001], [SA-001], [MIL-055] | [DM-004], [DICT-001], code and tests below | [RC-264] (Go with conditions) |
| `docs/wiki/Price-Suggestions.md` | Wiki page (Proposed) | en | - | [MIL-055] task 10, [US-001], [UC-007] | - | - (not yet reviewed) |
| Dictionary terms and domain concepts: Room rate, Price change, Price suggestion, Season, Pricing settings; Audit event actions `PriceChangeApproved`, `PriceChangeReverted`, `PricingSettingsChanged`, `RoomRateChanged` | Domain terms (Accepted) | en | it | [UC-007], [US-001] | [DICT-001], [DM-004], `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`) | [RC-264] |
| `src/NFHotel.Domain` (`RoomRate`, `PriceChange`, `PricingSetting`, `PriceChangeState`), `src/NFHotel.Application/UseCases/Pricing` (`SeasonRule`, `PriceSuggestionRule`, `PriceSuggestionService`, `PriceChangeService`, `PricingSettingsService`, `OccupancyObservationReader`), `src/NFHotel.Application/UseCases/RoomRates` (`RoomRateService`), the price lookup in `BookingService` | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3 to 9, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Api/Controllers` (`RoomRatesController`, `PricingSettingsController`, `PriceSuggestionsController`, `PriceChangesController`), `src/NFHotel.ApiClient` (`ApiRoomRateService`, `ApiPricingSettingsService`, `ApiPriceSuggestionService`), permission `Prices.Manage` | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 7, [US-001], [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Infrastructure` (configurations, repositories and migrations `AddRoomRateAndAuditDetail`, `AddPricingSettings`, `AddPriceChange`) | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| `src/NFHotel.Web/Features/Admin/Pricing` (`RoomRates`, `PricingSettings`, `PriceSuggestions` pages and view models), `AdminNav` | Source code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 8, [UC-007] | - | [RC-265] (No-Go), [RC-266] (Go-with-conditions, Proposed; criteria 3, 13 and 21 open) |
| Tests of the rule: `SeasonRuleTests`, `PriceSuggestionRuleTests`, `OccupancyObservationReaderTests`, `PriceSuggestionServiceTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 4 and 7, [US-001] | - | - |
| Tests of the services: `RoomRateServiceTests`, `PricingSettingsServiceTests`, `PriceChangeServiceTests`, `PriceChangeAuditTests`, `BookingPriceTests` (Application.Tests); `ManagePricesPermissionTests`, `StaffRolePermissionsTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 7 and 9, [UC-007] | - | - |
| Tests of the domain: `RoomRateTests`, `PricingSettingTests`, `PriceChangeTests`, `StaffAuditEventDetailTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 6 | - | - |
| Tests of the persistence: `RoomRateRepositoryTests`, `PricingSettingRepositoryTests`, `PriceChangeRepositoryTests`, `BookingPriceTests` (Infrastructure.Tests) | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5, 6 and 9 | - | - |
| Tests of the API and the client: `RoomRatesEndpointTests`, `PricingSettingsEndpointTests`, `PriceSuggestionsEndpointTests`, `EndpointProtectionTests`, `RoomRateRequestTests`, `PricingSettingsRequestTests`, `PriceSuggestionRequestTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 7, [UC-007] | - | - |
| Tests of the pages: `RoomRatesViewModelTests`, `PricingSettingsViewModelTests`, `PriceSuggestionsViewModelTests`, `RoomListViewModelTests`, `AdminAccessTests`, `BackOfficeAccessTests` | Test code (built, not yet merged) | - | - | [MIL-055] tasks 3, 5 and 8, [UC-007] | - | - |

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
[DSA-001]: ./devsecops-assessment.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
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
[RC-287]: ./reviews/rc-287-mil-061.md
[RC-288]: ./reviews/rc-288-mil-061-code.md
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
[RC-245]: ./reviews/rc-245-mil-048-plan.md
[RC-247]: ./reviews/rc-247-mil-048-code.md
[RC-285]: ./reviews/rc-285-mil-048-gate.md
[ee31f9e]: https://github.com/NF-Hotel/NFHotel/commit/ee31f9e0e0468453f3b2690a4571c2719b7cf3d0
