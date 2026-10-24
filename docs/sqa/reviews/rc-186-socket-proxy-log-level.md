# RC-186 Review of the log level of Alloy's socket proxy (MIL-023 task 8)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-186 |
| CrossReference | [MIL-023], [ADR-0020], [RC-185], [QC-YML-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [18da801] |

---

## Artifact Under Review

- **Instances reviewed:** `LOG_LEVEL` of the socket proxy in `ops/deploy/alloy/docker-compose.yml`, and its check in `ops/tests/test-alloy-start.sh`.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

After #259, container logs reach Loki. S03's count per container over one hour, run in Grafana, showed that `nfhotel-alloy-socket-proxy-1` wrote about 6,800 lines an hour, far more than every other container together. That is about 160,000 lines a day, kept for 14 days.

These lines are HAProxy's access log: one line for each poll of Alloy. They fill Loki and hide the logs that matter.

The pinned image was run with the two levels:
- at `info` (the default), every request was logged, allowed and refused;
- at `warning`, no request was logged.

The cost: a refused call is no longer logged by the proxy. It is still logged by Alloy itself, as the `403 Forbidden` of 2026-10-23 showed, so the signal stays.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| YML | Setting explained | Pass | The comment gives the measured volume and the remaining signal |
| SH 11 | Tested | Pass | The test makes allowed and refused calls through the proxy, then checks that the proxy logged none of them. The delivery and refusal checks of RC-185 stay |
| Data | Loki volume | Expected | About 160,000 fewer lines a day |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, check the count per container in Grafana again: the proxy should have no new lines | AI with S03 | After the merge |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[RC-185]: rc-185-alloy-docker-discovery.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[18da801]: https://github.com/NF-Hotel/NFHotel/commit/18da801063f1f1c8b8b51701b63b42b88f06e839
