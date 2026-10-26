# RC-237 Gate review of MIL-031, second pass

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-237 |
| CrossReference | [MIL-031], [RC-160], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [14f0f2e] |

---

## Artifact Under Review

- **Instance reviewed:** the eight Go/No-Go criteria of [MIL-031] again, after RC-160 (2026-10-21) found criteria 3 and 7 No-Go and criterion 5 not checked.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 asked in chat on 2026-10-26 to finish the open milestones.

## Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | `git log -- ops/` shows the DevOps commits up to `b63fbed` | Go | `b63fbed` is an ancestor of `main`; 90 commits touch `ops/` |
| 2 | No workflow uses `NF-Hotel/DevOps`; no deploy file outside `ops/` | Go | `.github` names NF-Hotel/DevOps only in comments and the description of an input; `check-layout.sh` passes in "Ops linters" |
| 3 | The `ops` checks pass and both CIs are required | Go | The ruleset of `main` requires the six application checks and the `NF Hotel Ops CI` jobs (Ops linters, the script tests, nginx routes, PostgreSQL preparation, Proxmox tests); all green on #372 |
| 4 | CD deploys a release from `main`; `/opt/nfhotel` is an NFHotel checkout at the release commit; the smoke test passes | Go | `/opt/nfhotel` is `https://github.com/NF-Hotel/NFHotel.git` at `f1d28a7`, the release of #372, deployed by CD with a passing smoke test |
| 5 | The market analysis answers with an analysis in production | Go | `market_analysis_run` in production holds an analysis finished 2026-10-25 22:40 UTC, which `/admin/market-analysis` shows |
| 6 | `dev-up.sh --check` passes with one clone | Go | Unchanged since RC-160 |
| 7 | NF-Hotel/DevOps is archived; its open PRs are closed with a link | Go | The repository is archived with 0 open pull requests; #1, #3, #4, #5, #7 to #10, #12 and #13 carry a link to NFHotel; #6, #11 and #14 were merged in DevOps before the archive |
| 8 | Reviews Go | Go | RC-146, RC-147 Go; RC-148 Go after its condition |

## Overall Verdict

Go. MIL-031 is complete; this pull request closes #206.

## Action Items

None.

---

[MIL-031]: ../../milestones/mil-031-consolidate-devops-into-nfhotel.md
[RC-160]: ./rc-160-mil-031-gate.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[14f0f2e]: https://github.com/NF-Hotel/NFHotel/commit/14f0f2e8186e8076b9dee03d8627e2c94edc0424
