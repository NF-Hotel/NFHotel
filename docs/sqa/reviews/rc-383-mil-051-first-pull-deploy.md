# RC-383 Code review of the first change deployed by the pull of MIL-051: TaskReportMissing watches backup_check

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-383 |
| CrossReference | [MIL-051], [MIL-076], [MIL-080], [ADR-0020], [ADR-0044], [RC-346], [RC-364], [RC-366], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go | pending |

---

## Artifact Under Review

- Instance reviewed: commit [93f5514] on the branch `mil-051-first-pull-deploy` (pull request 759), the first change
  of the monitoring that the pull of [ADR-0044] deploys by itself ([MIL-051] task 7, criterion 2):
  - `ops/deploy/monitoring/prometheus/rules/nfhotel.yml`: `backup_check` added to the reports that
    `TaskReportMissing` watches, and the comment above the rule says why;
  - `ops/deploy/monitoring/prometheus/rules-test.yml`: the missing-report test expects the alert for `backup_check`
    too, and a new test shows that a `backup_check` of 0 (no dump read back yet) is a report.
- Checklist used: [QC-YML-001] (`qc-yaml`) for both files. The rule itself is weighed against [ADR-0020] decision 4
  and finding F3 of [RC-364].
- Scope: the change of the two files; the rest of both files is unchanged since [RC-364] and [RC-366].
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig"; the reviewer wrote the change).

## Checklist Results

### QC-YML-001: `nfhotel.yml` and `rules-test.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); promtool reads both files |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte in either file |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 with `ops/.yamllint` through `ops/tests/lint.sh` on 2026-11-15: clean, as are the layout check, actionlint, shellcheck and shfmt |
| 4 | Scalars YAML would coerce are quoted | Pass | The series names and values of the new test are quoted, as in the other tests |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | |
| 7 | Embedded program text of five lines at most | Pass | The expression of `TaskReportMissing` has seven lines of PromQL; see F2 |
| 8 | A repeated block defined once | Pass | The reports are listed once, in the one expression; the new test follows the form of the missing-report tests |
| 9 | Anchors only where supported | N-A | None used |
| 10 | Validates against the schema of its tool | Pass | `promtool check config`, which reads every rule file, and `promtool test rules`, with `prom/prometheus:v3.15.1` pinned by digest, through `ops/tests/check-monitoring.sh` on 2026-11-15: SUCCESS, with the other five checks of the script |
| 11 | A header comment states purpose, tool and decision | Pass | Both headers unchanged ([ADR-0020], MIL-023 task 9); the comment above `TaskReportMissing` now names `backup_check` and [MIL-076] |
| 12 | Key names follow the tool, one case style | Pass | |
| 13 | Block scalars for multi-line strings | Pass | Folded block scalars for the expression and for the long series names of the new test |
| 14 | Lowercase kebab-case file names | N-A | No file added or renamed |
| 15 | A check runs without the real target | Pass | The rule tests run in Ops CI and in `check-monitoring.sh` without the LXC. The new tests fail against the rule of `main` (no alert for `backup_check`) and pass with the change |

## Findings of the review

- **F1, design: F3 of [RC-364] no longer applies.** The review of [MIL-076] left `backup_check` out of
  `TaskReportMissing` on purpose, for two reasons. First, the alert would have fired between the deploy of the rules
  and the first backup with the read-back. That window closed with the backup of 2026-11-14: the backups of 14 and 15
  November both wrote `backup_check`, and on 2026-11-15 at 06:13Z Prometheus held it at the time of the backup
  (02:03:22Z), the same as `backup`, with the six reports of nf-hotel present. Second, the backup writes the report
  on every run; that holds for `backup` as well, which the rule has watched since MIL-023. So only the window kept
  `backup_check` out. Without the report, `BackupUnreadable` has nothing to compare and stays silent, which is the
  case that [ADR-0020] decision 4 gives to `TaskReportMissing`: a read-back that never reported, or a report that
  was removed.
- **F2, note: seven lines of PromQL.** Criterion 7 limits embedded program text to five lines and asks for a longer
  one to move to a file of its own. A Prometheus rule holds its expression inline, and no rule can call a file, so
  the remedy is not open here; what the criterion aims at, a check in the program text's own language, is met:
  `promtool check config` parses the expression and `promtool test rules` runs it. The expression was six lines
  since [MIL-080] added `container_check`; the review of that change counted the expressions of its new alerts
  (three lines, [RC-346]) and not this one. It grows by one line per report; five alert rules of one line each
  would repeat the same matching five times (criterion 8).
- **F3, note: the wiki lists the reports.** The row of `TaskReportMissing` in `docs/wiki/Alerts.md` names the four
  reports. It gets `backup_check` in the documents pull request of the gate of [MIL-051], because criterion 2 asks
  for a pull request that changes only files of `ops/deploy/monitoring/`.
- **F4, note: the deploy raises no alert.** With `backup_check` present, the changed rule has no report to miss
  when it reaches the monitoring host; a report that goes missing later raises the alert after 30 minutes, while
  nf-hotel sends metrics.

## Overall Verdict

Go. Every applicable criterion of [QC-YML-001] passes, and the rule follows [ADR-0020] decision 4 now that the reason
of F3 of [RC-364] has passed. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green | S01 (the AI as stand-in) | At the merge |
| Add `backup_check` to the row of `TaskReportMissing` in `docs/wiki/Alerts.md` (F3) | S01 (the AI as stand-in) | With the gate of MIL-051 |

---

[MIL-051]: ../../milestones/mil-051-monitoring-continuous-deployment.md
[MIL-076]: ../../milestones/mil-076-every-nightly-backup-read-back.md
[MIL-080]: ../../milestones/mil-080-every-container-runs-with-the-rights-of-its-profile.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0044]: ../../adr/adr-0044-monitoring-deployed-by-a-pull-on-its-host.md
[RC-346]: ./rc-346-mil-080-code.md
[RC-364]: ./rc-364-mil-076-code.md
[RC-366]: ./rc-366-mil-051-code.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[93f5514]: https://github.com/NF-Hotel/NFHotel/commit/93f5514f5986fabfd8e84eeea3c79d6e2cf5598a
