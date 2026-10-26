# RC-232 Code review of MIL-045

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-232 |
| CrossReference | [MIL-045], [QC-YML-001], [QC-WF-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:** `ops/deploy/benchmark/docker-compose.yml` (the one-shot service `results-owner`, the dependency, the healthcheck that writes, the image anchor), `ops/tests/test-benchmark-volume.sh`, the job `benchmark-volume` of `.github/workflows/ops-ci.yaml`, the postmortem on the wiki page Incidents.
- **Checklists used:** [QC-YML-001], [QC-WF-001], [QC-SH-001], the rows that the change touches.
- **Scope:** delta review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `ops/tests/lint.sh` (actionlint, shellcheck, shfmt, yamllint: clean); `docker compose config`; `test-benchmark-volume.sh` in a Linux container with the Docker socket: 8 of 8.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| YML 1–3 | Parses, formatting, yamllint | Pass | 0 findings; `docker compose config` resolves both services |
| WF 7 | Least permissions | Pass | The new job has the workflow's read-only token and no secret |
| WF 8 | Actions pinned by full SHA | Pass | The same checkout SHA as the other jobs |
| WF 22 | Verified on the instance that runs it | Fail | Not yet: the job runs on github.com with this pull request |
| SH 1–2, 4, 12 | Strict mode, quoting, shellcheck, shfmt | Pass | Follows `test-nginx.sh`; 0 findings |
| SH 11 | Behaviour is tested | Pass | A volume of uid 1000 (with a file, as on the server) becomes 10001 and writable and the service healthy; the one-shot service exits 0 with `cap_drop: ALL`, only `CHOWN` and `DAC_READ_SEARCH` added, no network and a read-only root; the healthcheck fails on a volume it cannot write |
| SH 15 | Defined once | Pass | The image is one YAML anchor for both services; the uid comes from the image (`id -u app`), not from a number in the file |

## Findings

- **Docker copies the image's folder into an empty volume at every mount.** The first test run gave a false pass for that reason: an empty volume got the image's owner back. The server's volume held `.gitkeep`, so it kept uid 1000. The test now puts a file in the volume before it changes the owner.
- **The one-shot service runs as root,** which is what changing an owner needs. Its exposure is small: no network, a read-only root, no new privileges, two capabilities, and it ends before the service starts.

## Overall Verdict

Go-with-conditions. Every Mandatory criterion that the change touches passes except QC-WF-001 criterion 22, which closes when the job is green on github.com.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Confirm the job `benchmark-volume` is green on the pull request | AI | Before the merge |

---

[MIL-045]: ../../milestones/mil-045-benchmark-results-volume.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
