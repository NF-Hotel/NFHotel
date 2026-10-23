# RC-173 Gate check of MIL-004 (market analysis service on a Proxmox VM behind nginx)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-173 |
| CrossReference | [MIL-004], [ADR-0003], [ADR-0010], [ADR-0500], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the six Go/No-Go criteria of [MIL-004].
- **Checklist used:** [QC-MIL-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones on 2026-10-23.

## Why now

The work of MIL-004 was done and reviewed on 2026-10-16. RC-051 found "all six Go criteria of MIL-004 met", but the issues #50 to #55 and the milestone were never closed. This record checks the criteria against the repository and production as they are today, and closes them.

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | An ADR records the Proxmox host and VM versus LXC, and is reviewed | Go | ADR-0003, reviewed in RC-047. ADR-0010 records the production VM `nf-hotel` |
| 2 | The analysis service starts with `docker compose up` from its own compose file and `market-analysis.env` | Go | `ops/deploy/market-analysis/docker-compose.yml` with `market-analysis.env`, started by `deploy-services.sh` in every deploy. On `nf-hotel` both containers run healthy (`docker ps`, 2026-10-22) |
| 3 | The smoke test gets 200 for the analysis through nginx, and the service publishes no host port | Go | `ops/scripts/test-compose-proxy.sh`: 200 for the notebook on `analysis.localhost` and for `/health` on `analysis-api.localhost`, through the proxy (RC-016). The compose file publishes no port; nginx serves the analysis only on its loopback port 8081 |
| 4 | The web application and the analysis are reachable through the same nginx, each by its own route | Go | `default.conf.template` serves the web; `market-analysis.conf.template` serves the analysis by host name on the internal port |
| 5 | The gaps against US-001.01 are listed with an owner | Go | MIL-004 task 6: the notebook shows tables, not charts. The charts were owned by the source repository, which is gone (ADR-0500); they are the subject of MIL-028 |
| 6 | The Dockerfile, compose, YAML and shell files have a `Go` review | Go | RC-016 and RC-051 (`qc-yaml`, `qc-programming-shell`) |

## Findings

- **The market analysis page fails in production** (S01, 2026-10-23): "The market analysis service refused the bookings". The service answers 422 to `POST /analyze?insights=true`. This is the website feature of MIL-026 calling the frozen service of ADR-0500, not the deployment of MIL-004. It is investigated under MIL-031 criterion 5 and MIL-026.

## Overall Verdict

Go. MIL-004 is complete.

## Action Items

None.

---

[MIL-004]: ../../milestones/mil-004-market-analysis-service.md
[ADR-0003]: ../../adr/adr-0003-proxmox-vm-for-services.md
[ADR-0010]: ../../adr/adr-0010-production-vm-as-deployed.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
