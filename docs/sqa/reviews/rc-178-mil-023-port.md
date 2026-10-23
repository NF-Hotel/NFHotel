# RC-178 Review of the port of the MIL-023 work from NF-Hotel/DevOps

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-178 |
| CrossReference | [MIL-023], [ADR-0016], [RC-176], [RC-177], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [717dc18] |

---

## Artifact Under Review

- **Instance reviewed:** the port of NF-Hotel/DevOps#12 (MIL-023 tasks 1 to 4) and #10 (task 5) into `ops/` and `docs/`. Both were reviewed there, as RC-507 (now RC-176 and RC-177), and closed unmerged when DevOps was archived (MIL-031 task 7).
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones on 2026-10-23.

## What the port did

- Applied the two branches against DevOps `b63fbed` with their paths mapped: `deploy/`, `scripts/` and `tests/` became `ops/…`, and `docs/` stayed as it was. The Scorecard workflow and the CI changes were left out: Scorecard is in NFHotel since #197, and the host-hardening job is added to `ops-ci.yaml`.
- Resolved two conflicts with the work since `b63fbed`:
  - **benchmark compose.** The ported command of the old benchmark notebook (socat and marimo) is dropped, because the service is now an API (MIL-019, MIL-020). The service keeps its hardening and gets the memory, CPU and process limits.
  - **nginx compose.** `NGINX_CONFIG_HASH` (RC-165) is kept beside the ported hardening and health check.
- Renumbered the IDs into NFHotel's sequences:
  - CIS-500 became CIS-001, SAMM-500 became SAMM-001, DORA-500 became DORA-001; the three types are added to the registry, as types with no catalog entry, like DSA;
  - the two RC-507 records became RC-176 and RC-177, because NFHotel already has an RC-507 (MIL-025).
- Fixed the shellcheck `source=` paths and the documentation link of the firewall unit, and pointed the commands of the wiki page Host-Hardening and the CIS checklist at `ops/`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Lint | Pass | `ops/tests/lint.sh` clean over all of `ops/` and `.github/`, check-layout included |
| SH 11 | Tests | Pass | In Docker on the legion laptop: `test-host-setup.sh` 16 passed (Debian 13), `test-nftables.sh` 28 passed (network namespaces, `NET_ADMIN`), `test-dora-metrics.sh` 25 passed. The first two run in the new job "Ops host hardening (Debian 13, MIL-023)", the third in the script tests |
| YML | Compose files | Pass | `docker compose config` passes for all seven services with their example settings |
| WF 7, 14 | The new CI job | Pass | Read-only, a time limit, actions pinned; the capabilities are given to the test container only |
| WF 22 | Verified on GitHub | Not yet | Closes with the green Ops CI of this pull request; this also closes the condition of RC-176 |

## Findings

- **The apply steps on `nf-hotel` are not done.** MIL-023 criteria 1 to 4 need S03 on the VM, in a window:
  - the evidence before and after;
  - the patching;
  - the firewall with its automatic revert;
  - the Docker daemon, which restarts Docker.

  The wiki page Host-Hardening has the order.
- **The SAMM baseline and the first DORA report are dated 2026-10-20,** from when the operations lived in NF-Hotel/DevOps. They are kept as they were assessed. The next DORA report (October in full, due 2026-11-02) runs on NFHotel alone.

## Overall Verdict

Go-with-conditions: QC-WF-001 criterion 22 closes with the green Ops CI of this pull request.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run the apply steps of the wiki page Host-Hardening on `nf-hotel` with S03 present, and record them | S03 | A planned window |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[RC-176]: ./rc-176-mil-023-host-hardening.md
[RC-177]: ./rc-177-mil-023-scorecard-samm-dora.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[717dc18]: https://github.com/NF-Hotel/NFHotel/commit/717dc18e7bd3a6064272ef8fc9eb9ae123b89e55
