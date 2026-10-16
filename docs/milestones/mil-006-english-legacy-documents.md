# MIL-006 Legacy project documents in English

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-006 |
| CrossReference | [BC-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Deprecated | Claude (AI) | AI as stand-in for S01 | Initial version | pending |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-022 Go | pending |

---

## Purpose

Decide whether the legacy project documents in `docs/refactor/` can be
converted from Danish to English, the Product Owner language of the project
(`docs/artifact-registry.md`), without changing their meaning, their
identifiers or their links. They stay legacy notes: this milestone changes
their language and the names of their files and folders, not their type.

## Deliverable

- The 17 Markdown notes, the presentation page and the three PlantUML diagrams of `docs/refactor/` in English, with their rendered PNG and SVG files regenerated.
- English file and folder names (`Phase1-Scope.md`, `Models.md`, `diagrams/`, and so on) with every cross-reference updated.
- A language scan that finds no Danish text outside quotations of source code.
- An updated Business Case sentence that now says the legacy notes are in English.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A scan for Danish letters (æ, ø, å) and common Danish words in `docs/refactor/` finds nothing, except quoted source text that is listed in the review record | Yes | No |
| 2 | Every identifier (`BR-nn`, `D-nn`, `Q-nn`, `A-nn`, `B-nn`, `F-nn`) in the old files appears unchanged in the new files, the same number of times | Yes | No |
| 3 | No file or folder under `docs/refactor/` has a Danish name, and every Markdown link and file reference resolves | Yes | No |
| 4 | The regenerated PNG and SVG files show the English labels of their PlantUML sources | Yes | No |
| 5 | Code blocks, table structure and heading levels of each note are unchanged | Yes | No |
| 6 | A review record against `QC-LANG-001` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| None | Documents only; no `src/` file changes |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 9 of an amended BC-001: project documents are in the Product Owner language | All tasks |
| Registry `Languages`: PO language `en` | Criterion 1 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-10-26 — about 6,000 lines of text, translated and checked in groups.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 9 (legacy documents in the PO language), the scope items, and the MIL-006 schedule row; update the Business Case sentence that calls the notes Danish; review again. | No | |
| 2 | Translate the decision and scope notes | `Decisions`, `Fase1-Scope`, `OpenQuestions`, `OpenQuestions-Tracks`, `Access-Control-Matrix`, `Guest-Anonymization-Strategy`, `Encryption-Key-Management`, `README-backend` and `Testplan`: about 1,100 lines. Identifiers, tables and code blocks stay as they are. | No | |
| 3 | Translate the analysis notes | `BusinessRules` (the 126-rule ledger), `Analysis`, `Review-DCD-Data` and `Modeller`: about 1,700 lines. The rule identifiers `BR-nn` must not change. | No | |
| 4 | Translate the architecture notes | `Architecture`, `Architecture-Domain`, `Architecture-Layers` and `Architecture-Diagrams`: about 2,800 lines, the largest group. | No | |
| 5 | Translate the diagrams and the presentation | The three `.puml` sources and the presentation page; regenerate the PNG and SVG files with a local PlantUML container; rename the Danish file and folder names to English and update every reference. | No | |
| 6 | Scan and review | Run the language scan and the identifier and link checks, record the findings in an RC against `QC-LANG-001`, and update the Danish-note mentions in ADR-0001 and the traceability matrix. | No | |

---

[BC-001]: ../business-case.md
