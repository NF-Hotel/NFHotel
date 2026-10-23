# RC-184 Review of the Alloy user, the journal group and the Alloy start test (MIL-023 task 8)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-184 |
| CrossReference | [MIL-023], [ADR-0020], [ADR-0018], [CIS-001], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - `ops/deploy/alloy/docker-compose.yml` (user, group, health check);
  - `ops/scripts/deploy-services.sh` (the journal group, Alloy's failure handling, the Deployed line);
  - `ops/tests/test-alloy-start.sh` (new) and its step in the job "Ops monitoring configuration";
  - the pipe fix in `ops/tests/test-monitoring-stack.sh`;
  - a troubleshooting row of the wiki page Monitoring.
- **Checklists used:** [QC-SH-001], [QC-WF-001] and [QC-YML-001]; [CIS-001] for the hardening.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

**The failure.** S03 found Alloy restarting on `nf-hotel` with `mkdir /var/lib/alloy/data: permission denied`. S03 verified on the VM:

- the image has no `USER`, so Alloy ran as root (uid 0);
- `/var/lib/alloy` and the data volume are 473:473 with mode 0770;
- with `cap_drop: [ALL]`, root has no `DAC_OVERRIDE` and cannot pass that folder.

This is the third defect of the same kind: nginx (#252), the monitoring secrets (RC-180), and now Alloy.

**Why CD missed it.** Alloy had no health check, so `up --wait` returned as soon as the container ran, and CD reported "Deployed: … alloy".

**The fix:**

- Alloy runs as 473:473, the image's own user, which already owns the data folder and the volume. So no ownership or mode changes, and nothing is opened wider.
- It gets the host's `systemd-journal` group, because the journal is 2755 root:systemd-journal with 0640 files. The group's number differs between hosts, so `deploy-services.sh` reads it with `getent` once, before any compose call. Compose needs it for `up`, `ps` and `down` alike.
- The other mounts need nothing:
  - the job reports are 0644 in a 0755 folder;
  - `/proc` and `/sys` are world-readable;
  - the root filesystem is read only for `statfs`.
- The hardening stays: `read_only`, `cap_drop: [ALL]`, `no-new-privileges`. No `chmod 777`, and no capability is added.
- A health check asks Alloy's `/-/ready` with `bash`, since the image has no curl or wget.

**Why Alloy's failure no longer rolls back the hotel.** When Alloy does not turn healthy, the deploy warns, prints its log, and keeps the release.

- A broken monitoring agent must not take the site down.
- `NoMetricsFromNfHotel` alerts within 10 minutes.
- The start test in CI now catches this kind of defect before the merge.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The test catches the defect | Pass | **Probe:** with the compose file of `main`, `test-alloy-start.sh` failed 6 of 9 checks with the same `mkdir /var/lib/alloy/data: permission denied` as on `nf-hotel`. **With the fix:** all checks pass on the CI runner, as root, with the pinned image |
| SH 11 | What the test checks | Pass | Healthy and no restart; uid 473 with the runner's journal group; the hardening; the journal and host components loaded, none unhealthy; a job report in `/var/lib/nfhotel-metrics` readable; no permission error; healthy again after a restart with its data volume |
| SH 2 | Pipes | Fixed | The job-report check first failed although Alloy read the report (shown locally with the pinned image). Under `pipefail`, `grep -q` ends the pipe early and fails it. With `! … \| grep -q`, the same pattern could even pass falsely. The output is now read into a variable before matching, in both stack tests |
| SH 5 | Errors | Pass | A host without `systemd-journal` stops the deploy with a message before any container changes |
| SH 12, 4 | shfmt, shellcheck, yamllint | Pass | `ops/tests/lint.sh` is clean |
| Existing tests | No regression | Pass | `test-deploy-services-env.sh` 7, `test-deploy-release.sh` 59, `check-monitoring.sh` 7 |

## Overall Verdict

Go. After the merge, CD redeploys Alloy on `nf-hotel`; RC-179 action 1 continues with part D2 of the wiki page.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Check the CD run after the merge: Alloy healthy on `nf-hotel`, and data in Grafana (wiki page Monitoring, D2) | AI with S03 | After the merge |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0020]: ../../adr/adr-0020-monitoring-stack.md
[ADR-0018]: ../../adr/adr-0018-deploy-command-updates-itself.md
[CIS-001]: ../cis-checklist.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
