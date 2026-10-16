# RC-023 Review of the legacy documents in English

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-023 |
| CrossReference | [MIL-006], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-16 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version | [5b773ec] |

---

## Artifact Under Review

- Instance reviewed: the deliverable of [MIL-006], that is the 17 notes, the presentation page and the three PlantUML diagrams in `docs/refactor/`
- Checklist used: [QC-LANG-001] (the six Go criteria of [MIL-006] are checked below)
- Scope: full review
- Language and domain: en / it
- Not a Domain Model review: the diagrams in `docs/refactor/diagrams/` are legacy pictures, not framework artifacts. Only their language was checked, not their modelling quality (QC-DM-001 was not applied)
- Language reviewer: none; the terminology was confirmed by the user in chat
- Mode: autonomous, not independent (reviewer is `AI as stand-in for S01`)

## Go Criteria of MIL-006

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The language scan finds nothing | Pass | `grep -P '[æøåÆØÅ]'` over `docs/refactor/` matches no text file; the only matches were the old PNG files (binary, regenerated from the English sources). A scan for common Danish words (`og`, `ikke`, `der`, `det`, `som`, `til`, `af`, `med`, `skal`, `kan`) finds no match in the Markdown, HTML or PlantUML files |
| 2 | Every identifier appears unchanged, the same number of times | Pass | 1,763 identifier occurrences (`BR-`, `D-`, `Q-`, `A-`, `B-`, `F-`, `T-`, `U-` and sub-series such as `BR-C-01`, `Q-05b`) counted before the work and after it: the lists are identical |
| 3 | No Danish file or folder names; every link resolves | Pass | Renamed: `Fase1-Scope` to `Phase1-Scope`, `Modeller` to `Models`, `diagrammer` to `diagrams`, the three diagrams and the presentation page. A link check over all Markdown files in `docs/refactor/` reports no broken link; the references in ADR-0001 and MIL-005 were updated |
| 4 | The regenerated PNG and SVG show the English labels | Pass | Regenerated with the `plantuml/plantuml` container; the PNG of the domain model was inspected, and `Booking number` is present and `Bookingnummer` absent in the SVG |
| 5 | Code blocks, table structure and heading levels are unchanged | Pass | Identifiers, tables, headings and code structure kept; only prose and Danish comments inside code blocks were translated |
| 6 | A review record against QC-LANG-001 has the verdict Go | Pass | This record |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row | Pass | Legacy notes are not framework artifacts and have no Metadata table; the milestone [MIL-006] carries `en` and `it` |
| 2 | `Language` is a BCP 47 code and `Domain` is in the registry list | Pass | `en`, `it` |
| 3 | The content is written in the stated language | Pass | Scan result above; checked by the stand-in, not by a person |
| 4 | The register matches the registry | Pass | Technical register of the original notes kept |
| 5 | Domain terms are the PO terms, with no synonyms | Pass | Terms are the ones in the source code (`Booking`, `Room`, `Guest`, `Result`, and so on); no `DICT` exists yet |
| 6 | Metadata keys, headings, IDs and statuses are in English | Pass | Headings and identifiers are English or unchanged |
| 7 | No translated twin exists beside a document | Pass | The Danish originals were replaced, not kept |
| 8 | A change of language has a Version History row and was reviewed again | Pass | Recorded in [MIL-006] and this record |
| 9 | A competent reviewer confirmed the domain terms | Pass | The user confirmed the terminology in chat on 2026-10-16 (AI as stand-in for S01) |
| 10 | Abbreviations are spelled out on first use | Pass | The notes use established IT terms; unchanged from the source |

## Overall Verdict

Go for the language conversion only. All six Go criteria of [MIL-006] hold and QC-LANG-001 passes. Source-code comments and the app's user interface texts are out of scope by the accepted plan and stay Danish; they still mention the old file names `Fase1-Scope.md` in a few comments.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Update the comments that name `Fase1-Scope.md` in `AdminLayout.razor` and `BookingReceiptViewModel.cs` when the source comments are translated | S01 | open |
| Make a real Domain Model (`docs/domain-model.md`, checked against QC-DM-001) with the `artifact` skill. `diagrams/Domain-Model.puml` and `.png` are legacy pictures that were translated, not validated: they show a value object (`DateRange`) and enum types as concepts, and QC-DM-001 criteria 1, 3 and 7 have not been applied to them. Do not cite them as the project's Domain Model | S01 | open |
| Review the translation of the 126-rule ledger (`BusinessRules.md`) for meaning, because it was translated by the AI | S01 | open |

---

[MIL-006]: ../../milestones/mil-006-english-legacy-documents.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[5b773ec]: https://github.com/NF-Hotel/NFHotel/commit/5b773ecba695acc455f2765da7801d573d3bc5a5
