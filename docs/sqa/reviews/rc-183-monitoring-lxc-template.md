# RC-183 Review of the template choice and ID check of create-monitoring-lxc.sh, and the wiki corrections

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-183 |
| CrossReference | [MIL-023], [ADR-0020], [RC-182], [QC-SH-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/scripts/create-monitoring-lxc.sh`;
  - `ops/tests/test-create-monitoring-lxc.sh` (new) and its row in `ops-ci.yaml`;
  - the wiki pages Monitoring and Proxmox Deployment.
- **Checklists used:** [QC-SH-001] and [QC-WF-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

S03 followed the guide on the Proxmox node on 2026-10-23, and three things did not match:

1. **The template.**
   - Proxmox offered `debian-13-standard_13.6-1`, while the script's fixed default was `13.1-2`.
   - `--apply` stopped with "is not downloaded".
   - Nothing was created; the check worked as intended. But a fixed default goes out of date with every new template.
2. **The ID.** The guide's example `120` is free on the node, but the node's IDs run 100 (nf-hotel) and 101 (sports-club), and S03 chose `102`. The script checked only container IDs (`pct`), not VM IDs (`qm`).
3. **Grafana access.**
   - The guide said that only admins can open Grafana.
   - The tailnet policy of the team also has a grant from `autogroup:member` to every destination.
   - Grants only add access, so every member can reach port 3000. Grafana's login is what protects it.
   - The guide now says so. It does not remove the broad grant, which is a decision for the whole tailnet.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 5 | Errors | Pass | Without a downloaded Debian 13 template, `--apply` stops before anything is created and names the commands to run. A VM ID is refused too |
| SH 8 | Explicit flag | Pass | The dry run prints the chosen template first, so S03 sees it before `--apply` |
| SH 11 | Behaviour is tested | Pass | `test-create-monitoring-lxc.sh`, 9 passed: the newest template is found and wins over `13.1-2` and Debian 12; a given template wins; an invalid name, a VM ID, a container ID, a missing template and an ID below 100 are refused; the dry run calls nothing |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |
| Wiki | Matches the node | Pass | ID 102, the newest template, the `vmbr0` check, how to update an earlier clone, the IDs of the node, and the size of the LXC are in Proxmox Deployment, as ADR-0020 decision 1 asks |

## Overall Verdict

Go.

## Action Items

None.

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[RC-182]: rc-182-monitoring-wiki-guide.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
