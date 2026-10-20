# Artifact Registry

The artifact state of the NF Hotel infrastructure and operations work. Types, short names and `CrossReference Candidates` come from the framework catalog (`framework/registry/artifact-catalog.md`). This file records where each document lives in this repository and the next version to use.

The project baseline is in [NF-Hotel/NFHotel](https://github.com/NF-Hotel/NFHotel): the Business Case (BC-001), the Stakeholder Analysis (SA-001) and the Project Plan (PP-001). Documents here refer to it by absolute links. `framework/scripts/new-artifact.sh` checks the `Requires` column against this repository only, so it refuses a MIL here; create one from `framework/.agents/skills/artifact/templates/MIL.md` by hand, with the ID below.

## ID ranges (ADR-0012 of NFHotel, decision 5)

The documents moved from NFHotel keep their IDs:
- MIL-021 to MIL-024;
- ADR-0007 to ADR-0011;
- RC-089 to RC-101;
- DSA-001.

New documents in this repository take the next number **from 500**, so that NFHotel, which continues below 500, never collides with this repository.

| Short Name | Artifact Type | Primary File | Next Available Version |
| --- | --- | --- | --- |
| MIL | Milestone / Gateway | docs/milestones/*.md | 500 |
| RC | SQA Review Record | docs/sqa/reviews/rc-*.md | 508 |
| TM | Traceability Matrix | docs/sqa/traceability-matrix.md | 002 |
| ADR | Architecture Decision Record | docs/adr/adr-NNNN-*.md | 0501 |
| DSA | CI/CD and DevSecOps Assessment (no catalog type, proposed upstream) | docs/sqa/devsecops-assessment.md | 002 |

## Languages

The same settings as NFHotel.

| Setting | Value |
| --- | --- |
| PO language | en |
| PO domain | it |
| High-level register | IT Executive English |
| Technical register | IT Professional English |
| Wiki languages | en and the PO language, at least (`framework/process/wiki.md`) |

| Domain | Meaning |
| --- | --- |
| it | Software and IT |

| Artifact types | Register | Written in the PO language |
| --- | --- | --- |
| MIL | IT Executive English | Yes |
| OC, SD, DCD, ERD, ADR, TM, RC, QC, source code | IT Professional English | No |

## Notes

- "Next Available Version" is the zero-padded version to use the next time a new document of that type is created. `ADR` uses 4 digits; `RC` is sequential across all types.
