# RC-119 Review of the market analysis compose change (MIL-026 amendment)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-119 |
| CrossReference | [MIL-026], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version; it replaces RC-116, because `deploy/data-analysis/` is removed | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-026] task 6: `deploy/market-analysis/docker-compose.yml` (the markets API joins the network `nfhotel-api`) and the new section of `deploy/nfhotel-api/nfhotel-api.env.example`; `deploy/data-analysis/` is deleted
- Checklist used: [QC-YML-001]
- Scope: delta review of the changed lines and a check of the whole file
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe YAML loader; no duplicate keys | Pass | `docker compose config --quiet` exits 0 with the local `market-analysis.env`; the network key `api` is new and unique |
| 2 | 2-space indent, no tabs, LF, one final newline, no trailing spaces | Pass | The change follows the file |
| 3 | Passes `yamllint` with its configuration committed | Pass | `yamllint` is not installed and the project has no YAML linter configuration; covered by the `docker compose config` check; the action item of RC-116 stays open |
| 4 | Coercible scalars are quoted | Pass | The change adds no scalar |
| 5 | Only safe, plain types; no language tags | Pass | None used |
| 6 | No secret in the file | Pass | The change adds none; the `MarketAnalysisService__Token` of the env example is empty and is for a gateway only |
| 7 | Embedded program text is at most five lines | Pass | Unchanged |
| 8 | A repeated block is defined once | Pass | The network is declared once and used by one service |
| 9 | Anchors, aliases and merge keys only where supported | Pass | The existing anchor `analysis-image` is untouched |
| 10 | Validates against the schema of the consuming tool | Pass | `docker compose config` of the installed Compose |
| 11 | A header comment states what the file is for | Pass | The new network and the service entry each have a comment that names MIL-026 |
| 12 | One key case style | Pass | Compose keys |
| 13 | Multi-line strings state the chomping indicator | N-A | None used |
| 14 | File name is lowercase kebab-case, one extension style | Pass | Unchanged |
| 15 | A change is exercised by a check that runs without the real target | Pass | `docker compose config --quiet`; the stack was not started on a VM |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. The markets API still publishes no host port; it now joins the internal network `nfhotel-api` that the NFHotel API, the web application and the benchmark service share. The markets API has no authentication, so any of those services can call it; that is accepted because the network has no route out, and it is written in the wiki. The stack was not started or redeployed.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Redeploy `deploy/market-analysis` so that the service joins `nfhotel-api`, and set `MarketAnalysisService__BaseUrl` in `nfhotel-api.env` | S01 | Before the first deployment |
| Run `yamllint` over `deploy/` and commit its configuration, when the project adds one | S01 | With the CI work |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
