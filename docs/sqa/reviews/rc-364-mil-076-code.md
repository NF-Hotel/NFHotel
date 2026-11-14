# RC-364 Code review of MIL-076: every nightly backup read back

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-364 |
| CrossReference | [MIL-076], [RC-336], [ADR-0008], [ADR-0020], [ADR-0025], [MIL-074], [QC-SH-001], [QC-YML-001], [QC-WF-001], [QC-ADR-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green, among them the read-back against PostgreSQL 16 in the job of the real migrations | [c48b8db] |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-076] tasks 1 to 3 on the branch `mil-076-backup-read-back`:
  - task 1: the read-back, the report `backup_check` and the metrics `nfhotel_backup_tables` and
    `nfhotel_backup_bytes` in `ops/scripts/backup-database.sh`; the new test `ops/tests/test-backup-read-back.sh`;
    the read-back against PostgreSQL 16 in `ops/tests/test-database.sh`; and the new test in the matrix of the job
    `script-tests` of `.github/workflows/ops-ci.yaml`;
  - task 2: the rules `BackupUnreadable` and `BackupShrunk` in `ops/deploy/monitoring/prometheus/rules/nfhotel.yml`
    and their six tests in `rules-test.yml`;
  - task 3: the wiki pages Disaster-Recovery, Alerts, Monitoring, CI-CD, Technology-Choices and Workflows, and the
    row of 2026-11-14 of [ADR-0008] that cites this review.
- Checklist used: [QC-SH-001] (`qc-programming-shell`) for the three shell files, [QC-YML-001] (`qc-yaml`) for the
  two rule files, [QC-WF-001] (`qc-workflow`) for the changed job of `ops-ci.yaml`, and [QC-ADR-001] (`qc-adr`) for
  the changed parts of [ADR-0008].
- Scope: full review of the changed files; partial review of `ops-ci.yaml` (the job `script-tests`) and of
  [ADR-0008] (decisions 1 and 3, its current state, its affected artifacts and its row of 2026-11-14)
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig", with the merge and the approval of its CD run).

## Checklist Results

### QC-SH-001: `backup-database.sh`, `test-backup-read-back.sh` and `test-database.sh`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `#!/usr/bin/env bash` and `set -euo pipefail`, or a comment explains the exception | Pass | `backup-database.sh` keeps both; the two tests keep the convention of `ops/tests`, `set -uo pipefail`, so one failed check does not stop the others |
| 2 | Expansions quoted, lists as arrays, `[[ ]]` and `$(...)` | Pass | The command of the container is the array `in_postgres`, and the pipes and the readers are arrays, so `"${pipes[@]}"` gives `tee` no argument when the read-back cannot start; every new expansion is quoted; shellcheck clean |
| 3 | Naming | Pass | `start_reader`, `write_report`, `read_back_dir`, `read_back`, `postgres_dir`, `size_reported` in snake_case; `READ_BACK_SECONDS` as a constant |
| 4 | shellcheck and `bash -n`, every `disable` explained | Pass | shellcheck v0.11.0 `-x` over the three files: clean; the one new `disable` (SC2015 in the new test) says why |
| 5 | Errors on standard error with `error:`, a usage line for bad arguments | Pass | A failed read-back is one `warning:` line on standard error with both exit codes and the number of tables, followed by the last two lines of each reader; `die` and the usage are unchanged |
| 6 | `mktemp` and `trap ... EXIT`, no fixed `/tmp` names | Pass | `mktemp -d "${TMPDIR:-/tmp}/nfhotel-read-back.XXXXXX"`, pipes of mode 0600, and `trap 'rm -rf ...' EXIT`, the only trap of the script; the new test checks that the temporary folder is left empty |
| 7 | No secret in the script, its output or a command line | Pass | The readers read the stream only and do not connect to the database; the log names exit codes, a count of tables and the last lines of `pg_restore`, which name the archive, not its rows. The test writes a marker row into the dump and finds it in no file |
| 8 | A change of state outside its folder needs an explicit flag | Pass | Unchanged: the backup runs only with `--apply`. The read-back adds a temporary folder, which the trap removes, and three reports beside the others in the folder of the metrics |
| 9 | Header: purpose, usage, options, environment, exit codes | Pass | Step 1b names the readers, the pipes, what a failed read-back does and the stop of a reader after 30 minutes (added with this review, F5); step 5 names the three reports; Needs names `tee -p`, `timeout` and `mkfifo` |
| 10 | Implements a cited task; deviations recorded | Pass | [MIL-076] tasks 1 and 2; the findings below record the design choices and what was fixed in the review |
| 11 | Success, failure and bypass paths tested | Pass | `test-backup-read-back.sh` in `debian:13-slim`: 21 of 21. A whole dump is read back with 3 tables and reported at the time of the backup, with its size; `age` gets the bytes of the whole dump; both readers read pipes; no file holds the dump. A dump cut short and an unreadable archive fail the read-back, keep `backup_check` at its old time and keep the tables, while the backup succeeds and keeps the dump. Readers that cannot run keep the dump whole; a reader that hangs is stopped (exit 124) and the run ends within 30 seconds. Without the read-back (no temporary folder) the dump is the same, and `backup_check` is 0. The compose project of the VM runs the dump and both readers in its folder. Against the script of `main`, 18 of the 21 checks fail; the 3 that pass check that the backup goes on as before. `test-backup-settings.sh` (12 of 12) and `test-backup-target.sh` (44 of 44) are unchanged. The three new checks of `test-database.sh` run in CI only (F6) |
| 12 | Formatted with `shfmt` | Pass | shfmt v3.14.1 `-i 2 -ci` over the three files: no difference |
| 13 | Safe to re-run | Pass | Each run makes its own folder and pipes; each report is written to a temporary file and moved into place, so Alloy never reads half a file |
| 14 | Bash version and tools stated; GNU-only options named | Pass | `tee -p`, `timeout --kill-after` and `mkfifo -m` are GNU coreutils, named under Needs; Debian 12 and 13 have them, and the VM runs Debian 13 |
| 15 | Each sequence, constant, path or message defined once | Pass | One `write_report` writes every report, and `report_success` uses it; one `start_reader` starts both readers; the dump and both readers share `in_postgres` and `postgres_dir`; the time of a reader is the constant `READ_BACK_SECONDS` |

### QC-YML-001: `rules/nfhotel.yml` and `rules-test.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool reads both |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`, as `lint.sh` runs it: clean |
| 4 | Scalars YAML would coerce are quoted | Pass | The values of the series and the summaries are quoted |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The expression of `BackupUnreadable` is three lines, that of `BackupShrunk` five |
| 8 | A repeated block defined once | Pass | The new tests follow the groups of `BackupTargetDown` |
| 9 | Anchors only where supported | N-A | |
| 10 | Validates against the schema of its tool | Pass | `promtool check rules` (16 rules) and `promtool test rules rules-test.yml` with `prom/prometheus:v3.15.1`, the pin of the monitoring: both SUCCESS |
| 11 | A header comment states purpose, tool and decision | Pass | The header of `nfhotel.yml` names the two new metrics; the comment above each rule says what it compares and why |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | The expressions and the descriptions are folded block scalars, as in the other rules |
| 14 | Lowercase kebab-case file names | Pass | Unchanged names |
| 15 | A check runs without the real target | Pass | The rule tests run in CI without the monitoring |

### QC-WF-001: the job `script-tests` of `ops-ci.yaml`

The change is one entry of the matrix, one entry of `include` and the comment above it; the rest of the job and the
workflow are unchanged.

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Meets [QC-YML-001] | Pass | yamllint through `lint.sh`: clean |
| 2 | In `.github/workflows/`, `.yaml`, not in both directories | Pass | Unchanged |
| 3 | `name`, `on` and `jobs`; explicit events | Pass | Unchanged |
| 4 | Header comment: purpose, triggers, inputs, secrets, outside changes, decision | Pass | Unchanged; the comment above `include` now says why the new test runs in Debian 13 |
| 5 | Every job and step named | Pass | The check is named `Ops script tests (test-backup-read-back.sh)` |
| 6 | Passes `actionlint`, version stated | Pass | actionlint 1.7.7 through `lint.sh`: clean |
| 7 | Explicit least `permissions` | Pass | `contents: read` for the workflow, unchanged |
| 8 | Actions pinned to a full commit SHA | Pass | Unchanged |
| 9 | No secret in the file | Pass | |
| 10 | Untrusted text passed through `env:` | Pass | The name of the test and the image reach the script through `env:`, unchanged |
| 11 | No checkout of untrusted code with secrets | N-A | No `pull_request_target` or `workflow_run` |
| 12 | A change outside the repository needs a manual or protected start | N-A | The job changes nothing outside the runner |
| 13 | Token of least scope | Pass | The built-in token, read only |
| 14 | `runs-on` and `timeout-minutes` | Pass | `ubuntu-24.04` and 15 minutes, unchanged |
| 15 | `concurrency` | Pass | Cancels the older run of the same ref, unchanged |
| 16 | Triggers filtered | Pass | On every pull request on purpose, because its jobs are required checks (the header), unchanged |
| 17 | Tools at pinned versions | Pass | The test runs in `debian:13-slim`, like the other Debian 13 tests of the matrix, and installs `age` from that release |
| 18 | A dependency cache where it matters | N-A | |
| 19 | A failing step names the tool, file and reason | Pass | The test prints `FAIL` with the name of the check and the log of the run |
| 20 | `run:` within the limit of embedded text | Pass | One line, unchanged |
| 21 | Shared logic as a reusable workflow or composite action | N-A | |
| 22 | Features verified on the host that runs them | Pass | GitHub Actions on github.com, the same features as the other entries of the matrix |
| 23 | A reusable workflow states how a caller pins it | N-A | |
| 24 | A static test of the workflows on every pull request | Pass | `test-workflow-policy.sh`: the 14 workflows and actions follow the policy (ADR-0037) |

### QC-ADR-001: decisions 1 and 3, the current state, the affected artifacts and the row of 2026-11-14 of [ADR-0008]

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces and the options evaluated | Pass | Unchanged; the read-back adds a check to decision 1 and does not choose another option |
| 3 | At least two options were considered | Pass | Unchanged; a restore into a second PostgreSQL every night was weighed in [MIL-076] and is named in the Why of Disaster-Recovery |
| 4 | Decision stated in one or two unhedged sentences | Pass | "Each dump is read back to its end while it is made" |
| 5 | Consequences list Positive and Negative outcomes | Pass | Unchanged; decision 3 now says what the read-back does not prove, so "a restore is proven every month" still holds |
| 6 | Quality-attribute impacts named | Pass | Reliability (recoverability), unchanged |
| 7 | Affected Artifacts as links, each also in CrossReference | Pass | [MIL-076] in both |
| 8 | Status changes as new Version History rows; retained rows never rewritten | Pass | The row of 2026-11-14 for [MIL-076] is new; the row for [MIL-074] stays as it was and becomes Deprecated; the oldest row and its link left the table |
| 9 | A superseded ADR names its successor | N-A | |
| 10 | Business or technical language, no unexplained abbreviations | Pass | |
| 11 | Status values from the allowed list | Pass | `Accepted` and `Deprecated` |

### The documents against the deliverable of [MIL-076]

- **Disaster-Recovery** has the section "Every backup is read back": how, what it proves, what only the monthly drill
  proves, the reports, what to do when a read-back fails (with the decision of S01 after seven nights), and
  `BackupShrunk` after a migration; the Why list says why every night is read back and not restored. Its contents
  list all eight sections; two were missing before.
- **Alerts** and **Monitoring** have `BackupUnreadable` and `BackupShrunk` with their thresholds and what to do;
  Monitoring has the reports `backup_check.prom`, `backup_tables.prom` and `backup_bytes.prom`.
- **CI-CD** says that each dump is read back and that a failed read-back lets a deploy go on; **Technology-Choices**
  has the read-back as step 4 of the backup (steps 5 to 11 renumbered; no page cites those numbers); **Workflows**
  names the new script test.
- `test-wiki-coverage.sh` passes 119 of 119, two more than on `main` (117): the two new alerts on the page Alerts.

## The criteria of MIL-076 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pending | With a fake docker: the 21 checks of `test-backup-read-back.sh` above, 18 of which fail without the change; Ops CI runs it in the matrix. Against PostgreSQL 16: the three new checks of `test-database.sh` (the read-back with every table of the database, `backup_check` at the time of the backup with the tables, and the size of the encrypted dump) run in the job "Ops PostgreSQL preparation (real migrations)" of the pull request (F6) |
| 2 | Pass | The six new tests of `rules-test.yml`: `BackupUnreadable` fires for a backup newer than its read-back and for `backup_check` 0, and stays silent for a backup read back every night and for a host without any report; `BackupShrunk` fires for fewer tables until 8 days have passed and for a dump under half the size, and stays silent for a dump over half the size and for one that grows. Ops CI runs promtool |
| 3 | Pending | Task 4, after the deploy: the journal of the nightly backup inside the VM and its length |
| 4 | Pending | Task 4: `backup_check`, the two metrics and the rules in Prometheus, after the rules reach the monitoring host |
| 5 | Pending | Task 5: seven nights, and the decision of S01 |
| 6 | Pending | The documents and the linters above pass (`lint.sh` clean), and this review is Go-with-conditions; the checks of the pull request |

## Findings of the review

- **F1, design: report first.** A failed read-back keeps the dump and lets the backup succeed, so a deploy goes on;
  `BackupUnreadable` is its report. Whether a failed read-back fails the backup, and so stops a deploy, is the
  decision of S01 at the gate (task 5), as the deliverable says.
- **F2, design: a reader cannot hold the backup.** `tee -p` goes on when a reader ends early, as `pg_restore --list`
  does after the table of contents. Each reader runs under `timeout --kill-after=10` of 30 minutes, so a reader that
  hangs cannot hold the backup, its lock or a deploy for longer; a reader that cannot start ends at once, and the
  stream to `age` goes on. When the dump or the encryption fails, the readers are stopped before the script ends.
- **F3, design: the order of the reports.** `backup_check` is written before `backup`, with the same time, so
  `BackupUnreadable` never sees a backup whose read-back is not reported yet; it is 0 until a dump has been read back,
  so the alert has a value to compare after the first backup with the change. `TaskReportMissing` does not watch
  `backup_check`: it would fire between the deploy of the rules and the first backup with the change, and the backup
  writes the report on every run.
- **F4, note: the count of tables.** A table is a line `TABLE` of the table of contents, not `TABLE DATA` and not
  `TABLE ATTACH`, which matches `pg_tables` outside the two catalog schemas. Run by hand on 2026-11-14 against
  `postgres:16-alpine`, pinned by digest, with the hardening of the compose file of the VM (all capabilities dropped
  but five, read-only, `no-new-privileges`): 15 made tables of 20,000 rows each were read back as 15 tables, through
  `docker exec` and through the compose project, in about 1 second for a dump of 12 MB.
- **F5, fixed in the review.** `test-database.sh` did not yet check the reported size against PostgreSQL 16, which
  criterion 1 asks for (added in 0a67c3b), and the header of `backup-database.sh` did not name the stop of a reader
  after 30 minutes (added with this review).
- **F6, condition: `test-database.sh` runs in CI only.** It needs the API image and the migrations of the commit,
  which `prepare-database-test.sh` builds in the job "Ops PostgreSQL preparation (real migrations)"; its three new
  checks are seen first in the checks of the pull request.
- **F7, open for S01: the new required check.** The ruleset requires 44 checks; "Ops script tests
  (test-backup-read-back.sh)" is new, and only S01 changes the ruleset.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-SH-001], [QC-YML-001], [QC-WF-001] and [QC-ADR-001] passes,
and criterion 2 of [MIL-076] passes. The row of 2026-11-14 of [ADR-0008] for [MIL-076] is Accepted and its row for
[MIL-074] Deprecated. The condition: every check of the pull request is green before the merge, among them the
read-back against PostgreSQL 16 (F6). The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Approve the CD run of the merge; inside the VM over Tailscale SSH, read the journal of the next nightly backup and its length against the night before, and count the tables of the live database | S01 (the AI as stand-in) | 2026-11-16 |
| Bring the rules to the monitoring host and read `backup_check`, the two metrics and both rules in Prometheus | S01 (the AI on S01's words, by hand until MIL-051) | 2026-11-16 |
| Add "Ops script tests (test-backup-read-back.sh)" to the required checks (F7) | S01 | 2026-11-28 |
| After seven nights, decide whether a failed read-back fails the backup (F1, task 5) | S01 | 2026-11-28 |

---

[MIL-076]: ../../milestones/mil-076-every-nightly-backup-read-back.md
[RC-336]: ./rc-336-mil-076.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0025]: ../../adr/adr-0025-recovery-objectives.md
[MIL-074]: ../../milestones/mil-074-the-off-vm-copy-within-its-time.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[c48b8db]: https://github.com/NF-Hotel/NFHotel/commit/c48b8dbd50d448d02e879f37a241c1443ae35207
