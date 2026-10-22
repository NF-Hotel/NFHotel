# RC-169 Review of the amendment of MIL-023 (observability by ADR-0020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-169 |
| CrossReference | [MIL-023], [ADR-0020], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-22 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [2b09b0b] |

---

## Artifact Under Review

- **Instance reviewed:** [MIL-023], amended so its observability deliverable follows [ADR-0020].
- **Checklist used:** [QC-MIL-001].
- **Mode:** not independent (reviewer is `AI as stand-in for S01`). S01 asked in chat on 2026-10-22 to include the monitoring in the existing milestone and to change it accordingly.

## Changes

| Where | Was | Is |
| --- | --- | --- |
| Deliverable "Observability" | Log rotation, an external uptime check, a 90-day deployment log | The same logs, plus the stack of ADR-0020: Prometheus, Alertmanager, Grafana, blackbox_exporter and Loki in an LXC, Alloy on `nf-hotel`, Discord alerts and the Healthchecks.io watchdog, all configured in git |
| Task 4 | "… uptime check" | The uptime check moves to tasks 7 and 9 |
| Tasks 7–10 (new) | — | Monitoring stack, Alloy on nf-hotel, Alerts and watchdog, Alarm drill |
| Task 11 | Was task 7, Check and review | The same title, so the sync keeps issue #140; the checklists now include `qc-workflow` |
| Criteria 7 and 8 (new) | — | An alarm drill with times; logs and metrics searchable for their retention, with CI checks of the configuration |
| Dependencies | MIL-022, S03 | Plus ADR-0020, and S03 for the LXC, Discord and the Healthchecks.io account |

The sync dry run shows the seven existing issues, kept by title, and four new ones: tasks 7 to 10.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable | Pass | The stack, the alerts and the watchdog are named |
| 2 | Explicit Go/No-Go criteria | Pass | Criterion 7 names each alert and its time; criterion 8 names the retention and the CI checks |
| 3 | Dependencies mapped | Pass | ADR-0020 and the S03 actions |
| 4 | Traceable to the Business Case | Pass | Unchanged: objective 25, success criterion 28, DSA-001 R10–R12 |
| 5 | Owner and approving reviewer | Pass | Unchanged |
| 6 | Target date | Pass | 2027-01-16 is kept |

## Findings

- **Criterion 2 is not changed here.** It still says "from outside the tailnet only 80 and 443 answer". The VM is not exposed to the internet (ADR-0009). Changing that criterion is a separate decision for S01, offered with the plan of MIL-023.

## Overall Verdict

Go.

## Action Items

None.

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[2b09b0b]: https://github.com/NF-Hotel/NFHotel/commit/2b09b0bffdff14a310d4a5dcac10d719d11dd7b5
