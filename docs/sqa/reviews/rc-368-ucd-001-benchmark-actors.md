# RC-368 Review of the amendment of UCD-001 for the benchmark actors

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-368 |
| CrossReference | [UCD-001], [UC-008], [UC-009], [US-001], [SA-001], [MIL-019], [MIL-020], [ADR-0039], [RC-263], [QC-UCD-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the amendment of UCD-001 that puts Hotel staff (Manager) on the two benchmark use cases (MIL-019, MIL-020); Go; mode autonomous, not independent | [50f850d] |

---

## Artifact Under Review

- Instance reviewed: [UCD-001], the amendment of 2026-11-14, its second Version History row: Hotel staff on Compare Models by Time and Quality and on Run Time and Quality Benchmark in the diagram, the actor table, the use case table and the role matrix; the paragraph under the role matrix; the relationship to Sign In To The Back Office; the repository name `NF-Hotel/notebook-market-analysis`; the links to [UC-008] and [UC-009]; and the Version History cut to its two latest rows. The rest of the diagram is as the earlier reviews left it, the latest [RC-263].
- Checklist used: [QC-UCD-001] and [QC-LANG-001]
- Scope: delta review of the amendment, against every criterion of QC-UCD-001. Each changed statement was compared with the code on the branch `mil-019-gate` at 9de9be4, the same as `main` at 9de9be4 for these features, with [MIL-019] and [MIL-020], and with [UC-008] and [UC-009]. The rows of the earlier records still hold for the parts the amendment did not change (F4), so the verdict covers the whole diagram.
- Language and domain: en / it (the Metadata rows of UCD-001)
- Language reviewer: none; the reviewer reads the language and knows the domain.
- Mode: autonomous, not independent. S01 gave the acceptance of the plan of 2026-11-14, which has the missing use cases of MIL-019 and MIL-020 in its third wave, to the AI in chat ("Du godkender for mig"). An AI wrote the use cases and the amendment, and an AI wrote this record.
- Render: the diagram as fixed (F1) was rendered on 2026-11-14 with `plantuml/plantuml:1.2024.7` in Docker under WSL: exit 0, no syntax error, an SVG of 18,798 bytes with ten include arrows. The amendment before the fix had rendered the same way: exit 0, an SVG of 19,609 bytes with eleven.

**What each change rests on:**

| Change | Evidence |
| --- | --- |
| Hotel staff (Manager) on Compare Models by Time and Quality | [MIL-019] decisions 4 and 5; `RolePermissions` gives `Benchmark.View` to the Manager alone; `BenchmarkController` lists and reads the runs only for `Benchmark.View`; the page `/admin/benchmark` needs the policy `Benchmark.View`, and `AdminNav` shows its menu item only then; `AdminAccessTests`: a Receptionist and a Housekeeping user read "You don't have access to this page" |
| Hotel staff (Manager) beside DevOps on Run Time and Quality Benchmark | [MIL-020] task 2; `RolePermissions` gives `Benchmark.Run` to the Manager alone; the page `/admin/benchmark/start` needs the policy `Benchmark.Run`, as do the record of a run and its change of state in `BenchmarkController`; DevOps deploys the service from `ops/deploy/benchmark` with `ops/scripts/deploy-services.sh` |
| The Owner works through the role Manager | The roles of the code are Receptionist, Manager and Housekeeping (`Roles`); there is no Owner, and only the Manager holds `Benchmark.View` and `Benchmark.Run` ([MIL-019] decision 4) |
| Compare Models by Time and Quality includes Sign In To The Back Office | The page `/admin/benchmark` carries a policy, which needs a signed-in member of staff (UC-004) |
| The repository `NF-Hotel/notebook-market-analysis` | [ADR-0039]; `MARKET_ANALYSIS_REPO` in `ops/deploy/market-analysis/docker-compose.yml` |

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Actors are defined with correct UML stereotypes (e.g. `<<System>>`, `<<Actor>>`) | Pass | No actor added; Hotel staff keeps `<<Actor>>`, and the boundary `<<System>>` |
| 2 | System boundary is clearly drawn and labeled | Pass | Unchanged: "NFHotel platform"; both benchmark use cases stay inside it |
| 3 | Include/extend relationships are used correctly per UML 2.5.1, not as generic "uses" arrows | Pass | After the fix of F1: Compare Models by Time and Quality includes Sign In To The Back Office, because every comparison is made in the back office; Run Time and Quality Benchmark does not, because its DevOps part has no sign-in |
| 4 | Every actor participates in at least one use case (no orphan actors) | Pass | The Owner in two use cases, DevOps in three, Hotel staff in nine |
| 5 | Diagram is traceable to a documented stakeholder need | Pass | The two rows keep their source stories, US-001.02 and US-001.04; Hotel staff is S05 of [SA-001] |
| 6 | Use case names are verb phrases describing actor goals, not internal system operations | Pass | No use case renamed or added |
| 7 | Diagram is free of implementation detail (e.g. UI widgets, database tables) | Pass | The diagram names actors and goals only; the permission names stand in the reason column and under the role matrix, as the decision of [MIL-019] |
| 8 | Actor and use case naming is consistent with corresponding Use Case and User Story documents | Pass | [UC-008] and [UC-009] carry the names of the diagram and the primary actor Manager; US-001.02 (Owner) still traces to Choose Local AI Model, which includes the comparison, and US-001.04 (DevOps) to the part DevOps keeps (F2) |
| 9 | Each use case lists the user story it comes from and its priority (MoSCoW) with a reason | Pass | US-001.02 and US-001.04, both Should; the reasons name MIL-019 and MIL-020 |
| 10 | When there is more than one primary actor, a role matrix (use case by role) is present and marked as a suggestion for the Product Owner to approve | Pass | Still marked as a suggestion; the two rows changed, and the paragraph under it names the one use case that two roles share (F3) |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | en and it |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | en, and it from the domain list of the registry |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English; the words of S01 in the Version History, "Du godkender for mig" ("You approve for me"), are quoted source text |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Professional English, as the registry gives for UCD: the rows name the roles and the permissions and leave the steps to the use cases |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | Role and the role Manager, as the dictionary and the use cases of the back office use them; the dictionary has no benchmark terms yet (an action item of RC-105), so the benchmark words are those of [MIL-019] and [MIL-020] |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | One file; `check-languages.sh` finds no twin |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The AI as stand-in for S01, which reads English and knows the IT domain; not independent |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | Only established IT terms (AI) are abbreviated; the amendment adds none |

## Findings

- **F1, fixed: an include that held for one actor only.** The amendment let Run Time and Quality Benchmark include Sign In To The Back Office, "the start by a Manager". An include is unconditional in UML 2.5.1: every run of the base use case runs the included one. The DevOps part of this use case, running the service, has no sign-in to the back office, so the include held for one of its two actors only, which criterion 3 does not allow. Fixed in the text: the line `UC4 ..> UC10 : <<include>>` is removed; the relationship row names Compare Models by Time and Quality alone and says why Run Time and Quality Benchmark does not include the sign-in (a signed-in Manager is a precondition of [UC-009]); the Version History row says so. The diagram was rendered again (Render, above).
- **F2, note: the Owner acts through the role Manager.** The amendment moves Compare Models by Time and Quality from the Owner to Hotel staff (Manager), as the code has it, and says that the Owner works through an account with the role Manager. Choose Local AI Model stays with the Owner and includes the comparison, so US-001.02 ("As an Owner") still traces to the diagram. In the code the default AI model, the goal of Choose Local AI Model, is the Manager's too (`AiDefaults.Manage`). Whether US-001.02 and US-001.04, and the actor of Choose Local AI Model, change is S01's decision (action item); [UC-008] and [UC-009] keep it as an open issue.
- **F3, note: one use case, two roles.** DevOps runs the benchmark service and a Manager starts a run, so Run Time and Quality Benchmark has two primary actors. The paragraph under the role matrix says so, and the matrix stays a suggestion for the Product Owner.
- **F4, note: "behind the proxy" is outside the amendment.** The goal of Run Time and Quality Benchmark keeps "behind the proxy", and its relationship to Route Services Through Nginx is unchanged. Since [ADR-0039] the benchmark service is on `nfhotel-python`, a network nginx cannot see, and publishes no port; the only way to it is the back office, which is behind the proxy. The phrase and the relationship hold in that sense; they are not part of the amendment and were not reviewed again.
- **F5, note: the repository name.** `NF-Hotel/notebook-market-analysis` is the name in [ADR-0039] and in the compose file of the market analysis. US-001.05 still names `NF-Hotel/Notebook_Market_Analysis`; the user stories are not part of this review.

## Overall Verdict

Go. Every Mandatory criterion of QC-UCD-001 and QC-LANG-001 passes for the amendment after the fix of F1, and the rows of the earlier records still hold for the rest of the diagram. S01 gave the acceptance to the AI in chat on 2026-11-14 ("Du godkender for mig"), and the AI accepted the amendment and this record as stand-in for S01: the row of 2026-11-14 of UCD-001 is Accepted and the row before it Deprecated. The reviewer is not independent: an AI wrote the amendment and this record.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the amendment of UCD-001, or return them | S01; done on 2026-11-14 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-019-gate` |
| Decide whether US-001.02 and US-001.04, and the goal Choose Local AI Model, name the role Manager as their actor (F2); the same change of the user stories can take the repository name of US-001.05 (F5) | S01 | 2026-12-31 |

---

[UCD-001]: ../../use-case-diagram.md
[UC-008]: ../../use-cases/uc-008/uc.md
[UC-009]: ../../use-cases/uc-009/uc.md
[US-001]: ../../user-stories.md
[SA-001]: ../../stakeholder-analysis.md
[MIL-019]: ../../milestones/mil-019-benchmark-results-api.md
[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[ADR-0039]: ../../adr/adr-0039-python-services-network.md
[RC-263]: ./rc-263-mil-055-task-1-objective-and-story.md
[QC-UCD-001]: ../../../framework/qc/artifacts/qc-use-case-diagram.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[50f850d]: https://github.com/NF-Hotel/NFHotel/commit/50f850d890371ba0759cbba802c27c9b31cf5b69
