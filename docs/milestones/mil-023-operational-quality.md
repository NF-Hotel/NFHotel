# MIL-023 Operational quality and maturity

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-023 |
| CrossReference | [BC-001], [ADR-0003], [MIL-022], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01 | Initial version | [f4649ad] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-091 Go | [f4649ad] |

---

## Purpose

Decide whether the VM and the containers are hardened and observed well enough to serve the internet. The work covers four things:

- harden Docker and Debian along selected Center for Internet Security (CIS) Benchmark Level 1 items;
- manage the firewall and patching as code;
- measure the repository posture and the delivery performance;
- set a security maturity baseline.

## Deliverable

- **CIS checklist.** A list of the selected CIS Docker Benchmark v1.9.0 and CIS Debian Linux 12 Benchmark v2.0.0 Level 1 items, each with applied, not applicable (with the reason) or deferred, and the evidence.
- **Docker daemon.** A `daemon.json` with `live-restore`, `no-new-privileges`, the `local` log driver with size limits and `userland-proxy: false`, applied by a reviewed script.
- **Compose hardening, per service where it works:**
  - `cap_drop: [ALL]` with only the capabilities that are needed;
  - `no-new-privileges`;
  - `read_only` with `tmpfs`;
  - memory and CPU limits;
  - an unprivileged nginx image, if it is compatible.
- **nftables.**
  - The input policy drops by default; 80 and 443 are open to the internet, and 22 only on `tailscale0`.
  - Docker traffic is filtered in `DOCKER-USER`.
  - The ruleset is in the repository and tested after a Docker restart.
- **Patching.** `unattended-upgrades` installs Debian security updates, the images are rebuilt monthly, and the reboots are planned.
- **Observability.** Log rotation, an external uptime check of `https://`, and a deployment log that is kept for 90 days.
- **Posture and maturity.**
  - An OpenSSF Scorecard workflow (weekly and on `main`), with its results in code scanning.
  - An OWASP Software Assurance Maturity Model (SAMM) self-assessment (baseline).
  - A monthly report of the DevOps Research and Assessment (DORA) metrics (change lead time, deployment frequency, failed deployment recovery time, change failure rate, deployment rework rate).
  - A monthly restore drill.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The CIS checklist covers every selected item, each with a status and evidence; no "applied" item is without evidence | Yes | No |
| 2 | From outside the tailnet, only 80 and 443 of the VM answer (nmap); 22 answers only over Tailscale; this holds after a Docker restart | Yes | No |
| 3 | `docker inspect` shows `no-new-privileges` and dropped capabilities for every service on the checklist, and all services stay healthy | Yes | No |
| 4 | `unattended-upgrades` has installed security updates (log entry), and container logs are size-limited | Yes | No |
| 5 | The Scorecard workflow runs and its result is recorded; the SAMM baseline and the first DORA report exist | Yes | No |
| 6 | Two monthly restore drills are recorded | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-022 | The pinned and scanned images that the hardening applies to |
| S03 | Changes to the VM, the firewall and the reboot schedule |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 25 and success criterion 28 of BC-001 | All tasks |
| DSA-001 risks R10, R11, R12 | All tasks |
| CIS Docker and Debian; OpenSSF Scorecard; OWASP SAMM; DORA | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01; S03 for the VM and the firewall |

## Target Date

2027-01-17, six weeks after MIL-022. The two restore drills need two months.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | CIS selection | Choose and record the Level 1 items for Docker and Debian 12 with their applicability. | No | |
| 2 | Docker daemon and compose hardening | `daemon.json`; per-service privileges, filesystem and limits; tested per service. | No | |
| 3 | nftables | Ruleset as code with `DOCKER-USER`; tested after a Docker restart; port scan from outside. | No | |
| 4 | Patching and logs | `unattended-upgrades`, monthly rebuild, log rotation, uptime check. | No | |
| 5 | Scorecard, SAMM, DORA | Scorecard workflow; SAMM baseline; DORA monthly report. | No | |
| 6 | Restore drills | Two monthly drills recorded. | No | |
| 7 | Check and review | Prove the criteria; reviews against `qc-programming-shell`, `qc-yaml`. | No | |

---

[BC-001]: ../business-case.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[MIL-022]: ./mil-022-supply-chain-security.md
[DSA-001]: ../sqa/devsecops-assessment.md
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
