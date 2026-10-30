# RC-252 Review of the Technology Choices page, the image coverage test and the MIL-050 amendment

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-252 |
| CrossReference | [MIL-050], [QC-MIL-001], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-30 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [b4329fc] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `docs/wiki/Technology-Choices.md`, against the Go/No-Go criteria of [MIL-050];
  - the links in `docs/wiki/Home.md`, `docs/wiki/_Sidebar.md`, `docs/wiki/Appendix.md` and the four MIL-049 pages;
  - the change of `ops/tests/test-wiki-coverage.sh` (workflows and third-party images on the new page), against [QC-SH-001];
  - the amendment of criterion 4 of [MIL-050], against [QC-MIL-001].
- **Checklists used:** [QC-SH-001], [QC-MIL-001], and the criteria of [MIL-050].
- **Scope:** full review.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan in chat on 2026-10-30 and chose the sources of criterion 4 ("1").

## Checklist Results

### The page (MIL-050)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Every listed product, every third-party image and every workflow file has an entry; the test passes | Pass | 51 entries, one for each product of the Deliverable list and Testcontainers. `bash ops/tests/test-wiki-coverage.sh`: 85 passed, 0 failed, with 10 workflows and 17 third-party images checked on the new page. In a copy with the backticks around `dockurr/proxmox` removed, the test failed with `third-party image dockurr/proxmox is missing from Technology-Choices.md` |
| 2 | Every named step of the workflows and composite actions, and every numbered step of the backup and the restore drill, is in a step-by-step section | Pass | 27 pipeline rows in the order of a change, one note for the housekeeping steps (checkout, SDK, GHCR login, artifacts, SARIF), 10 backup and restore rows covering steps 1 to 5 of `backup-database.sh`, the installer, the drill and its CI test |
| 3 | Each entry names at least two alternatives, advantages and drawbacks, and the project's reason or "Not decided in an ADR" | Pass | Each entry has the rows Alternatives, For it, Against it, Our reason and Choose the alternative when. 31 entries say "Not decided in an ADR" and name what the repository shows instead; the others link their ADR, and the options follow the ADR's list where it has one (ADR-0003, 0007, 0020, 0023, 0024, 0026, 0028, 0029, 0033) |
| 4 | Each entry cites a practitioner source, or the product's documentation marked as such; every link opens; no long quote | Pass | 54 Hacker News links, all checked against the Hacker News API (each a story with the title cited). The summaries were taken from the top-ranked comments of each thread; three claims written before their thread was read were corrected (Python against R, Alertmanager and paging, the CodeQL secret). 8 Stack Exchange links, each found through the Stack Exchange API; their pages answer 403 to scripts, and their answers are not summarised yet (task 1 stays open). The survey page answers 200. Four entries with no community source cite the product's documentation, marked "no community source" (SonarQube Cloud, OpenSSF Scorecard, unattended-upgrades, blackbox exporter). No sentence is quoted; titles of threads are the only verbatim text |
| 5 | No secret, password, token or private address | Pass | A search for private address ranges, the tailnet domain, `hc-ping`, Discord webhook URLs, `token=` and `password=` found nothing |
| 6 | Linked from Home, the sidebar, Appendix and the MIL-049 pages | Pass | One line in Home, one in `_Sidebar.md`, one in Appendix (CRLF of the working copy kept), and "Why each product" on the header line of Workflows, Containers-and-Images, Security-Controls and Alerts. The published wiki is checked after the merge (task 8) |

### `ops/tests/test-wiki-coverage.sh` (QC-SH-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 2 | Quoted expansions, arrays, `[[ ]]` and `$(...)` | Pass | The images are an array from `mapfile`; `third_party_images` uses no unquoted expansion |
| 3 | Naming | Pass | `third_party_images` in `snake_case`, like `compose_services` |
| 4 | shellcheck and `bash -n` | Pass | `ops/tests/lint.sh`: actionlint, shellcheck, shfmt and yamllint clean |
| 9 | Header comment | Pass | The header names the new page and what it must contain |
| 11 | Success and failure tested | Pass | The run and the failing copy in criterion 1 of the page |
| 12 | shfmt | Pass | Clean |
| 14 | Needs stated; GNU-only options named | Pass | Unchanged: bash, awk, grep; `tr` and `sed -E` are POSIX |
| 15 | Defined once | Pass | `check_names` is reused for both new checks |

The other criteria of QC-SH-001 are unchanged by this change and were reviewed in RC-250.

### The amendment of MIL-050 (QC-MIL-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 2 | Explicit Go/No-Go criteria | Pass | Criterion 4 now names the sources that can be reached (Hacker News, Stack Exchange, the Stack Overflow survey), keeps the documentation fallback and says why Reddit is not used. The change is S01's choice in chat, recorded in the Version History |

## Overall Verdict

Go: every Mandatory criterion passes. Task 1 (the research) stays open until the answers of the eight Stack Exchange threads are read and summarised on the page; the Stack Exchange API allows that after its daily quota resets.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Summarise the answers of the eight Stack Exchange threads on the page, and close MIL-050 task 1 | S01 (AI as stand-in) | 2026-11-06 |

---

[MIL-050]: ../../milestones/mil-050-technology-choices.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[b4329fc]: https://github.com/NF-Hotel/NFHotel/commit/b4329fcf8655a3ab43a0a8e1eb9da93217c32349
