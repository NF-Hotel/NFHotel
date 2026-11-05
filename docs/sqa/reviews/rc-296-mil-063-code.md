# RC-296 Code review of MIL-063: the wait of the CD scope step and its tests

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-296 |
| CrossReference | [MIL-063], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the review of tasks 1 to 3 of MIL-063 before its pull request | pending |

---

## Artifact Under Review

- **Instances reviewed** (the branch `mil-063-cd-scope-waits-for-the-tailnet` against `main` at 98ba991):
  - `ops/scripts/cd-steps.sh`: the helper `wait_reach`, its use in `scope` for the SSH port, the log line with the seconds, the failure message with the deadline, and the header (task 1);
  - `ops/tests/test-cd-steps.sh`: the check of the logged seconds, the failing port with `SCOPE_WAIT_SECONDS=3` and its message, and the listener that starts three seconds late (task 2);
  - the wiki page Workflows, the "Does" row of `NF Hotel CD` (task 3).
- **Checklist used:** [QC-SH-001].
- **Scope:** delta review of the changed files against `main`.
- **Mode:** autonomous, not independent. The AI wrote the code and this record; S01 accepted the plan of MIL-063 in chat on 2026-11-05 ("Accepter planen"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Tools and runs of 2026-11-05, on the PC of S01 (Docker Desktop, Docker 29.7.2):** `ops/tests/test-cd-steps.sh` in `debian:12-slim`, the way the Ops CI job `Ops script tests (test-cd-steps.sh)` runs it: 78 checks, 0 failed, the four of the wait among them; `ops/tests/lint.sh` in containers (layout, actionlint 1.7.7, shellcheck v0.11.0, shfmt v3.14.1, yamllint 1.35.1) clean; `ops/tests/test-wiki-coverage.sh`, 91 of 91.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1, 2, 3 | Header line, `set` options, quoting, names | Pass | The script keeps `set -euo pipefail`; every expansion of the new function is quoted; `snake_case` names (`wait_reach`, `wait_seconds`, `waited`) |
| SH 4, 12 | shellcheck, `bash -n`, shfmt | Pass | `lint.sh` clean at the first run; no `disable` comment added |
| SH 5 | Errors | Pass | The deadline failure goes through `fail`, which prints an `::error::` line and exits 1, and names the port, the host and the seconds (tested) |
| SH 6, 7, 8 | Temporary files, secrets, state | Pass | None of the three: the function opens TCP connections and prints a number |
| SH 9 | Header | Pass | The header of `scope` names the wait, its variable and its default, and the reason; the function's comment names the incident of 2026-11-05 |
| SH 10 | Implements the task it cites | Pass | MIL-063 tasks 1 and 2: the wait on the SSH port only, the other ports tried once, the seconds in the log and in the failure |
| SH 11 | Tested for success, failure and the bypass path | Pass | A port that answers at once logs `after 0 s`; a port that never answers fails after the short deadline with the seconds in the message; a listener that starts three seconds late passes with a non-zero number of seconds logged; the four earlier checks of the step (closed ports, an open port as a warning, another peer listed) are unchanged and pass |
| SH 13 | Safe to re-run | Pass | The function has no side effect; a second run gives the same answer |
| SH 14 | Needs stated | Pass | `date +%s`, `timeout` and bash's `/dev/tcp`, all in the Debian image the test runs in and on the GitHub runner |
| SH 15 | Knowledge once | Pass | `reach` stays the one place that opens a connection; `wait_reach` only repeats it; the deadline is one variable with one default |
| MIL-063 1 | The wait, its default and its log line | Pass (in test) | The three new checks; the default of 60 is in the script and the header |
| MIL-063 2 | The other ports once each, an open one a warning | Pass | The unchanged checks "an open port is a warning, not a failure" and "the scope records the closed ports" |
| MIL-063 4 | The wiki row | Pass | The "Does" row names the wait of up to a minute and why; the coverage test passes |
| MIL-063 5 | Review Go, lint clean, the job green | Pass (locally) | This record; `lint.sh` clean; the job of the pull request is the proof on the host |

## Findings

- **The elapsed time is measured before the successful try.** `wait_reach` prints the seconds between its start and the beginning of the try that answered, so a port that answers at once logs `after 0 s`; a try that itself takes up to five seconds is not counted. The number is a floor, which is what the log needs: it says how long the path was down, not how long the connect took.
- **The deadline can be overshot by one try.** The loop checks the deadline after a failed try and sleeps one second between tries, so a run can last up to five seconds longer than `SCOPE_WAIT_SECONDS`. With the default of 60 that is nothing against the 45 minutes the gate step may wait, and the test with a deadline of 3 seconds passes in a few seconds.
- **The test of the late listener is timing-based.** It starts the listener three seconds after the step starts and allows twenty; a slow runner makes the step wait longer, not fail. The check asks for a non-zero number of seconds, so it would catch a step that no longer waits.

## Overall Verdict

Go. Every Mandatory criterion of QC-SH-001 passes, and criteria 1, 2 and 4 of MIL-063 are met in the test and the page; criterion 3 closes with the CD run of the merge, criterion 5 with its green job. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge the pull request only when every required check is green | S01 (AI as stand-in) | At the merge |
| Approve the CD run of the merge (ADR-0038), or ask the AI in chat to do it through the API; its scope step is the proof of criterion 3 | S01 | At the merge |

---

[MIL-063]: ../../milestones/mil-063-cd-scope-waits-for-the-tailnet.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
