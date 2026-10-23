# RC-181 Review of ADR-0022 and the wiki publish workflow

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-181 |
| CrossReference | [ADR-0022], [QC-ADR-001], [QC-SH-001], [QC-WF-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [2abf153] |

---

## Artifact Under Review

- **Instances reviewed:**
  - [ADR-0022];
  - `ops/scripts/publish-wiki.sh` and `ops/tests/test-publish-wiki.sh` (new);
  - `.github/workflows/publish-wiki.yaml` (new) and the test's row in `ops-ci.yaml`;
  - the wiki page Home.
- **Checklists used:** [QC-ADR-001], [QC-SH-001] and [QC-WF-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## Why

S01 could not find the page Monitoring on the GitHub wiki. The wiki is a separate repository that was last copied by hand on 2026-10-17. Five pages were missing there, and four were out of date.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| ADR | Context, options, decision, consequences | Pass | Four options, each with its reason. The decision says who decided what. The cost, an edit made in the wiki being overwritten, is stated, with how it is handled |
| SH 1–5 | Strict mode, quoting, errors | Pass | `REPO` and `SHA` are validated, and every git step dies with a message |
| SH 7 | No secret on a command line | Pass | The token goes to git as an `extraheader` through `GIT_CONFIG_*` environment variables, so it is not in the URL, the clone's configuration or a process list |
| SH 11 | Behaviour is tested | Pass | `test-publish-wiki.sh` (8 passed) uses a local bare repository. It checks that pages are added and changed, a removed page is deleted, a non-page file is kept, the commit names its source, a second run pushes nothing, and bad input is refused |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |
| WF 7 | Least permissions | Pass | The workflow defaults to `contents: read`. Only the job has `contents: write`, which GitHub requires for its wiki. No personal token, and `persist-credentials: false` |
| WF | Concurrency | Pass | One publish at a time and none cancelled, so the newest commit of `main` is always the last one published |

## Overall Verdict

Go. The first run after the merge must show that the job's `GITHUB_TOKEN` may push to the wiki.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Check the first run of `NF Hotel Wiki`, and that the page Monitoring is on the wiki | AI | After the merge |

---

[ADR-0022]: ../../adr/adr-0022-publish-the-wiki-from-docs.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[2abf153]: https://github.com/NF-Hotel/NFHotel/commit/2abf153a86440108d36bebadb79f07811616cd0b
