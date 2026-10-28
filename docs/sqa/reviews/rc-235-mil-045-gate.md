# RC-235 Gate review of MIL-045

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-235 |
| CrossReference | [MIL-045], [RC-231], [RC-232], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [5d48b68] |

---

## Artifact Under Review

- **Instance reviewed:** the five Go/No-Go criteria of [MIL-045], after #370 was merged (`be42f0d`), released and deployed by CD (run 40286477218), and after the deploy of #371.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 started the benchmark runs of criterion 4.

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A volume owned by uid 1000 becomes writable and the service healthy | Go | `test-benchmark-volume.sh` in the ops CI job "Ops benchmark results volume (MIL-045)": 8 of 8, green on #370 |
| 2 | With `/data` not writable, the service is unhealthy | Go | The same test: the healthcheck of the compose file exits 1 on a volume of uid 1000 |
| 3 | The one-shot service keeps only the capabilities that change an owner; the service keeps its hardening | Go | On `nf-hotel`: `benchmark-results-owner-1` exited 0 with `CapAdd [CAP_CHOWN CAP_DAC_READ_SEARCH]`, `CapDrop [ALL]`, network `none`, read-only root; `/data` owned by 10001 |
| 4 | After the deploy, a run ends `done` and its result is listed | Go | Run `a1878e80` (07:11 to 07:20 UTC) was the first result ever stored in `benchmark_run`; run `96dcc754`, started by S01 at 07:53, ended "State: done. Result: stored" (S01's screenshot). `results.jsonl` is written by `app`, and `delivered/` holds the delivered runs |
| 5 | CI green; code review Go | Go | #370: 38 checks green after the line length fix; RC-232 Go, its condition met |

## Findings

- **A run that ended unseen is recorded as interrupted.** Run `a1878e80` finished and was stored at 07:20, but no page asked for its state before the deploy of #371 restarted the benchmark service; afterwards the tracker of MIL-043 took the forgotten run for an interrupted one. The result is safe; only its record is wrong. The fix (check for a stored result before recording `interrupted`) is outside this milestone and is put to S01.

## Overall Verdict

Go. MIL-045 is complete; this pull request closes #363.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Decide how the finding above is fixed: a follow-up of MIL-043 or a new milestone | S01 | Next session |

---

[MIL-045]: ../../milestones/mil-045-benchmark-results-volume.md
[RC-231]: rc-231-mil-045.md
[RC-232]: rc-232-mil-045-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[5d48b68]: https://github.com/NF-Hotel/NFHotel/commit/5d48b6818cdaad4d2ff233d2bb39421b67b18c1e
