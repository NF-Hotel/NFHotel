# RC-107 Review of deploy/nfhotel-web/docker-compose.yml (MIL-020)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-107 |
| CrossReference | [MIL-020], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Accepted | Claude (AI) | AI at the request of S01, not independent | Initial version | [847b639] |

---

## Artifact Under Review

- Instance reviewed: [MIL-020]: `deploy/nfhotel-web/docker-compose.yml`, whose change is a comment that names the benchmark service; the settings are in `nfhotel-web.env.example`, which is not YAML
- Checklist used: [QC-YML-001]
- Scope: full review
- Language and domain: n/a for a technical type
- Language reviewer: none
- Mode: supervised, reviewed by the AI at the request of the user in chat; not independent

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The file parses with a safe YAML loader and has no duplicate keys | Pass | `docker compose config -q` accepts it (Docker Compose v2, Docker 29.8.2) with the example env file |
| 2 | Indentation is 2 spaces, LF line ends, one final newline, no trailing spaces | Pass | yamllint 1.38.0 clean |
| 3 | Passes `yamllint` with its configuration committed next to the code | Pass | `.yamllint` of the repository; a comment line that was too long was wrapped during this review |
| 4 | A scalar that YAML would coerce is quoted | Pass | No scalar changed |
| 5 | Only safe, plain types are used | Pass | No tags |
| 6 | No secret is written in the file | Pass | The benchmark token is only in the git-ignored env file; the example holds an empty value |
| 7 | Embedded program text in a value is at most five lines | Pass | The healthcheck is one line |
| 8 | A block of keys or values repeated is defined once | Pass | None |
| 9 | Anchors, aliases and merge keys are used only where the tool supports them | Pass | None |
| 10 | The file validates against the schema of the consuming tool | Pass | `docker compose config -q` |
| 11 | A header comment states what the file is for, which tool reads it, and where its decision is recorded | Pass | The header names Docker Compose, MIL-002 and ADR-0004; the new lines name MIL-020 |
| 12 | Key names follow the convention of the consuming tool | Pass | Compose keys |
| 13 | Multi-line strings use a block scalar | Pass | None |
| 14 | The file name is lowercase kebab-case with one extension style | Pass | `docker-compose.yml` |
| 15 | A change is exercised by a check that runs without the real target | Pass | `docker compose config -q`; the container was not started |

## Overall Verdict

Go — every mandatory criterion passes. The change is a comment; the web service reads the benchmark settings from its env file. The review is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Start the web and benchmark services together in the VM and open the Start page | S01 | notebook-benchmark MIL-004 |

---

[MIL-020]: ../../milestones/mil-020-start-benchmark-run.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[847b639]: https://github.com/NF-Hotel/NFHotel/commit/847b639fa32cd4699fbfb154011ea93b76fdc545
