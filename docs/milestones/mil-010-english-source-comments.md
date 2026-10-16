# MIL-010 Source comments in English

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-010 |
| CrossReference | [BC-001], [MIL-006] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 (terminology confirmed by the user) | Accepted after RC-045 Go | pending |

---

## Purpose

Decide whether every comment in the source code and the tests can be converted
from Danish to English, the Product Owner language of the project, without
changing any code. [MIL-006] left the source comments as they were; this
milestone converts them. The text of the application screens, string literals,
error code strings and test data stay as they are.

## Deliverable

- All comments in `src/` and `tests/` in English: XML documentation comments, line comments and block comments in `.cs`, `.razor` and `.css` files (about 5,600 comment lines in about 170 files).
- A check that shows the change touched comments only.
- Unchanged: UI texts shown to the user, string literals, test method names and test data, identifiers, and generated files (`obj`, `bin`, `*.Designer.cs`, `wwwroot/lib`).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A scan of the comments in `src/` and `tests/` for Danish letters (æ, ø, å) and common Danish words finds nothing, except quoted text that is listed in the review record | Yes | No |
| 2 | Every changed line in the diff against the previous commit is a comment line; the diff with comment lines removed is empty | Yes | No |
| 3 | Every identifier in the comments (`BR-nn`, `D-nn`, `Q-nn`, `A-nn`, `B-nn`, `F-nn`, `T-nn`, `U-nn`) appears unchanged, the same number of times | Yes | No |
| 4 | `dotnet build` has 0 warnings (XML documentation warnings are errors), `dotnet format --verify-no-changes` is clean, and all tests pass with the same count as before | Yes | No |
| 5 | The text shown to the user by the application is unchanged (a search of the `.razor` markup and string literals shows no edit) | Yes | No |
| 6 | A review record against `qc-programming-csharp` and `QC-LANG-001` has the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-009 | It changes many of the same files; the comments are converted after it to avoid converting twice |
| MIL-006 | The notes the comments cite are already in English |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| New objective 13 of an amended BC-001: the comments in the source are in the Product Owner language | All tasks |
| Success criterion 16 | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-04 — about 5,600 comment lines, converted and checked per project.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Amend Business Case and Project Plan | Add objective 13, the scope item, success criterion 16 and the MIL-010 schedule row; review again. | No | |
| 2 | Convert the Domain comments | `NFHotel.Domain`: about 20 files. | No | |
| 3 | Convert the Application comments | `NFHotel.Application`: about 47 files. | No | |
| 4 | Convert the Infrastructure comments | `NFHotel.Infrastructure`: about 25 files; only hand-written comments, not generated migration code. | No | |
| 5 | Convert the Web comments | `NFHotel.Web`: about 55 files, including `.razor` and `.css` comments; screen texts stay. | No | |
| 6 | Convert the test comments | `tests/`: about 21 files; test names and data stay. | No | |
| 7 | Check and review | Run the language scan, the comment-only diff check and the identifier count; build, format and test; record an RC against `qc-programming-csharp` and `QC-LANG-001`. | No | |

---

[BC-001]: ../business-case.md
[MIL-006]: ./mil-006-english-legacy-documents.md
