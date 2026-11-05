# RC-297 Gate review of MIL-063 The CD scope check waits for the tailnet path

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-297 |
| CrossReference | [MIL-063], [RC-295], [RC-296], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-063 after the first CD run with the wait | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-063] at its gate: the five Go/No-Go criteria, with the evidence of the pull request #530 (merged as be6b23a on 2026-11-05 at 20:0x UTC) and the CD run 42103015481 that deployed it. Before the gate: [RC-295] (the plan, Go) and [RC-296] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-063]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 1, 2, 4 and 5 cite RC-296 and the green runs.
- Language and domain: en / it (the Metadata rows of MIL-063); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. The AI wrote the milestone, the code, RC-295, RC-296 and this record; S01 asked for the plan ("Ja, planlæg"), accepted it ("Accepter planen"), said when the checks were green ("Grøn, merge") and asked the AI to approve the CD run through the API with S01's login ("Ja, via API"), all in chat on 2026-11-05. The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence of 2026-11-05:**

- **The pull request.** #530: 49 checks passed and 2 skipped, the job `Ops script tests (test-cd-steps.sh)` among them (78 checks, the four of the wait included); merge commit be6b23a; `main` green again after it.
- **The deploy.** CD run 42103015481 (be6b23a), approved through the API on S01's instruction and finished at 20:26:46 UTC: the scope step logged "Port 22 of nf-hotel answers (after 0 s)" on its first attempt, every service deployed, the smoke test answered 301 on 80 and 200 on 443, "deployed be6b23a", and the prune of MIL-062 removed the four images of the release before the previous one (71 GB to 72 GB free).

## Checklist Results

| # | Criterion (MIL-063) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `scope` keeps trying port 22 until it answers or `SCOPE_WAIT_SECONDS` (default 60) have passed, and logs the seconds; a late listener passes, a port that never answers fails after the deadline; proven in Ops CI | Pass | The job `Ops script tests (test-cd-steps.sh)` green on #530 with the three new checks (RC-296); the default of 60 is in the script and its header |
| 2 | The other ports are tested once each after port 22 answers; an open one a warning, a closed one a pass; the existing checks unchanged | Pass | The unchanged checks of the step green in the same job (RC-296); on the host the step reported the closed ports as before |
| 3 | The CD run of the merge passes the scope step on its first attempt, and its log names the seconds waited | Pass | Run 42103015481, attempt 1: "Port 22 of nf-hotel answers (after 0 s)" at 20:25:35 UTC; the run succeeded |
| 4 | The Workflows page describes the wait, and `test-wiki-coverage.sh` passes | Pass | The "Does" row of `NF Hotel CD` (RC-296); the coverage test 91 of 91 locally and `Ops wiki coverage` green on #530 |
| 5 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, the job `Ops script tests (test-cd-steps.sh)` is green | Pass | RC-296 (Go); `Ops linters` and the job green on #530 |

## Findings

- **The path was up at once this time.** The wait logged 0 seconds: the runner's path to the VM was ready when the step ran, as in most runs. The wait costs nothing then and gives a run like the first attempt of #170 up to a minute instead of a red deploy.
- **The deploy record is complete now.** This CD run also shows the prune of MIL-062 doing its routine work (four images, the release before the previous one), so the two fixes of the day are both seen in one log.

## Overall Verdict

Go: the five criteria pass on the green pull request, RC-296 and the CD run of the merge. The milestone closes: the issues #528 and #529 and the GitHub milestone #45 close through the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| None open | - | - |

---

[MIL-063]: ../../milestones/mil-063-cd-scope-waits-for-the-tailnet.md
[RC-295]: ./rc-295-mil-063.md
[RC-296]: ./rc-296-mil-063-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
