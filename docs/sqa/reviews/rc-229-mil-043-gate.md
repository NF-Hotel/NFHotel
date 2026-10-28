# RC-229 Gate review of MIL-043

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-229 |
| CrossReference | [MIL-043], [RC-226], [RC-228], [ADR-0031], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [be56726] |

---

## Artifact Under Review

- **Instance reviewed:** the six Go/No-Go criteria of [MIL-043], after #358 was merged (`ca29537`), released (run 40252438998) and deployed by CD (run 40252824430).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 started the benchmark run of criteria 4 and 5 and confirmed the start page.

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | ADR-0031 records the decisions and the alternatives | Go | ADR-0031 (RC-227 Go): four options for the keys, three for the runs |
| 2 | After a restart of the web container, a form opened before it is sent without an error; 0 key ring errors | Go | On `nf-hotel`: the login form was fetched through nginx, `nfhotel-web-nfhotel-web-1` was restarted, the form was posted: 200 with the validation messages, no antiforgery failure; the same key file before and after; 0 "key ring" lines in the web log |
| 3 | The keys are in a named volume readable only by `app`; the hardening holds | Go | Mount `nfhotel-web_keys:/app/keys`; folder 700 and key file 600, owned by `app`; read-only root, `cap_drop: ALL`, `no-new-privileges` |
| 4 | A started run is stored at once; after a restart of the benchmark container it is shown as interrupted and can be started again with the same choice | Go | Run `6d6cbbb8…` (llama3.1 and phi4-mini) was stored at 06:50:10 UTC as running; `benchmark-benchmark-1` was restarted at 06:51:35; at 06:51:50 the record was `interrupted` with the reason. The start page showed the text and kept the choice with "Start the run" enabled (S01's screenshot). The list's "Start again" is covered by `BenchmarkRunListViewModelTests` and `BenchmarkRunTrackerTests` |
| 5 | Leaving the start page and opening it again shows the run that is still going | Go | S01 reopened the start page during the run: "State: running", the choice shown and locked |
| 6 | Unit and integration tests; CI green; code review Go | Go | #358: every check green (1 557 tests, 12 live tests skipped); RC-228 Go. The first CI run found two faults, both fixed in the pull request: `check-secret-env.sh` took `DataProtection__KeysPath` for a secret, and three architecture rules |

## Overall Verdict

Go. MIL-043 is complete; this pull request closes #357.

## Action Items

None.

---

[MIL-043]: ../../milestones/mil-043-state-across-deploys.md
[RC-226]: rc-226-mil-043.md
[RC-228]: rc-228-mil-043-code.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[be56726]: https://github.com/NF-Hotel/NFHotel/commit/be56726511eb9fd7f588bf89349166c342aa8ce2
