# Artifact Registry

This project's artifact state. Types, short names and `CrossReference
Candidates` come from the framework catalog
(`framework/registry/artifact-catalog.md`); this file only records where
each document lives in *this* project and the next version to use.

Delete rows for types you don't use. Add a row the first time you create a
document of a type. `Primary File` may contain a glob (e.g.
`docs/use-cases/uc-*/uc.md`); `framework/scripts/find-crossreferences.sh` reads it.

| Short Name | Artifact Type | Primary File | Next Available Version |
| --- | --- | --- | --- |
| BC | Business Case | docs/business-case.md | 002 |
| SA | Stakeholder Analysis | docs/stakeholder-analysis.md | 002 |
| PP | Project Plan | docs/project-plan.md | 002 |
| MIL | Milestone / Gateway | docs/milestones/*.md | 016 |
| RC | SQA Review Record | docs/sqa/reviews/rc-*.md | 067 |
| TM | Traceability Matrix | docs/sqa/traceability-matrix.md | 002 |
| ADR | Architecture Decision Record | docs/adr/adr-NNNN-*.md | 0005 |
| US | User Story | docs/user-stories.md | 002 |
| UCD | Use Case Diagram | docs/use-case-diagram.md | 002 |
| UC | Use Case | docs/use-cases/uc-*/uc.md | 004 |
| DM | Domain Model | docs/domain-model.md, docs/use-cases/uc-*/dm.md | 005 |
| DICT | Domain Dictionary | docs/dictionary.md | 002 |

## Languages

Set the PO language when the project starts; `project-planning` asks for it
if it is missing. An artifact of a type marked "Written in the PO language"
exists once, in that language, under its normal name (`business-case.md`); the
`artifact` skill ("One file per artifact") has the rule.

| Setting | Value |
| --- | --- |
| PO language | en |
| PO domain | it |
| High-level register | IT Executive English |
| Technical register | IT Professional English |
| Wiki languages | en and the PO language, at least (`framework/process/wiki.md`) |

Every artifact of a type marked "Yes" below states its language and domain in
its `Language` and `Domain` Metadata rows; `new-artifact.sh` fills them from
`PO language` and `PO domain`. `Language` is a BCP 47 code (`da`, `en`).
`Domain` is a value from this list; add a row to introduce a domain, so a
reviewer can see at once which professional vocabulary a document uses.

| Domain | Meaning |
| --- | --- |
| it | Software and IT |
| medical | Healthcare and medical devices |
| construction | Construction and civil engineering |

| Artifact types | Register | Written in the PO language |
| --- | --- | --- |
| BC, KPI, PP, MIL | IT Executive English | Yes |
| SA, BMC, BPMN, UCD, US, UC, SSD, DM, RA, GOV, DICT | IT Professional English | Yes |
| OC, SD, DCD, ERD, ADR, TM, RC, QC, source code | IT Professional English | No |

## Notes

- "Next Available Version" is the zero-padded (3-digit) version to use the
  *next* time a new document of that type is created. Increment it only when
  a brand-new document is created, not when an existing document's
  `## Version History` gets a row.
- `ADR` uses 4 digits (`0001`); `RC` is sequential across all artifact types.
