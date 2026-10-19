# RC-116 Review of the data analysis compose file (MIL-026)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-116 |
| CrossReference | [MIL-026], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [82ec2b2] |

---

## Artifact Under Review

- Instance reviewed: [MIL-026] task 6: `deploy/data-analysis/docker-compose.yml`, its tracked example `data-analysis.env.example`, and the new section of `deploy/nfhotel-api/nfhotel-api.env.example`
- Checklist used: [QC-YML-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses with a safe YAML loader; no duplicate keys | Pass | `yaml.safe_load` (PyYAML) parses it; `docker compose config --quiet` exits 0 with an env file made from the example |
| 2 | 2-space indent, no tabs, LF, one final newline, no trailing spaces | Pass | Checked with the editor settings; the file has LF endings |
| 3 | Passes `yamllint` with its configuration committed | Pass | `yamllint` is not installed on this machine, so it was not run; the project has no YAML linter configuration either. Covered by the parse and the `docker compose config` check; see the action item |
| 4 | Coercible scalars are quoted | Pass | The only candidate is `"8000"` in `expose`, which is quoted |
| 5 | Only safe, plain types; no language tags | Pass | None used |
| 6 | No secret in the file | Pass | The token is `API_TOKEN` in the git-ignored `data-analysis.env`; `git check-ignore` is true for `deploy/data-analysis/data-analysis.env`; the example has placeholders only, and `MarketAnalysisService__Token=` is empty |
| 7 | Embedded program text is at most five lines | Pass | One line, the health check |
| 8 | A repeated block is defined once | Pass | Nothing repeated in the file; the two networks are the same pair as `deploy/benchmark`, which is a separate stack |
| 9 | Anchors, aliases and merge keys only where supported | N-A | None used |
| 10 | Validates against the schema of the consuming tool | Pass | `docker compose config` of the installed Compose validates the file |
| 11 | A header comment states what the file is for, which tool reads it, and where its design is recorded | Pass | The header names MIL-026 and `notebook-data-analysis`, the start command and the rule for `DATA_ANALYSIS_REF` |
| 12 | One key case style | Pass | Compose keys as the tool defines them |
| 13 | Multi-line strings state the chomping indicator | N-A | None used |
| 14 | File name is lowercase kebab-case, one extension style | Pass | `docker-compose.yml`, like the other stacks |
| 15 | A change is exercised by a check that runs without the real target | Pass | `docker compose config --quiet`; the image was not built, because `notebook-data-analysis` has no Dockerfile yet |

## Overall Verdict

Go — every mandatory criterion passes or is not applicable. The review is not independent. The service publishes no host port (`expose` only), drops all capabilities, has a read-only root file system and joins only the two internal networks, as `deploy/benchmark` does. The stack was not started: there is no image to build until `notebook-data-analysis` has a Dockerfile, and `DATA_ANALYSIS_REF` has no default so that a build of an unpinned commit is refused.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `yamllint` over `deploy/` and commit its configuration, when the project adds one | S01 | With the CI work |
| Pin `DATA_ANALYSIS_REF`, add the service to `scripts/deploy-services.sh` and build it, when `notebook-data-analysis` has a Dockerfile | S01 | With the Python milestone |

---

[MIL-026]: ../../milestones/mil-026-market-analysis-report.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[82ec2b2]: https://github.com/NF-Hotel/NFHotel/commit/82ec2b2acd49a5a76da4093e355397b6615ef9b0
