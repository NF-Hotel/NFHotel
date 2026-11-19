# RC-401 Review of BC-001: objective 42 and success criterion 45 (vulnerability disclosure)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-401 |
| CrossReference | [BC-001], [QC-BC-001], [QC-LANG-001], [MIL-084], [RC-332] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-17 | Accepted | mikemike | S01 | Initial version; supervised mode: the AI prepared the review; Go; S01 accepted the change in chat on 2026-11-17, with the plan of MIL-084 ("1. Ja") | [91b74ec] |

---

## Artifact Under Review

- Instance reviewed: [BC-001], the row of 2026-11-17: objective 42, success criterion 45 and one line in scope, added for [MIL-084]
- Checklist used: [QC-BC-001] and [QC-LANG-001]
- Scope: delta re-review of criteria 3 and 4 of QC-BC-001, the two the change touches; the reason is the plan of [MIL-084]; the earlier record is [RC-332]
- Language and domain: en / it (the Metadata rows of BC-001)
- Language reviewer: none (S01 reads the language and knows the domain)
- Mode: supervised. The artificial intelligence (AI) assistant wrote the change and prepared this record, so it is not an independent review; the reviewer is S01, and the verdict stands when S01 accepts the change in chat, together with the plan

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ROI/Cost-Benefit analysis is quantitative, or where qualitative, is explicitly justified | N-A | Not touched by the change; Pass in [RC-332] |
| 2 | Risks are identified with documented impact and mitigation | N-A | Not touched by the change; Pass in [RC-332] |
| 3 | Success criteria are measurable, stating explicit targets rather than vague aspirations | Pass | Success criterion 45 names the answer 200, the type `text/plain; charset=utf-8`, the five fields, a date at most one year and at least 30 days ahead, the file on the site equal to the file on `main`, and 5 working days for the confirmation of a report; its measurement names the check, the test, the request from outside and the advisories of the repository |
| 4 | Scope explicitly separates In Scope vs Out of Scope | Pass | One line in scope: the `security.txt` of the public site and the disclosure policy of the repository. Nothing moves out of scope |
| 5 | Stakeholders are cross-referenced to Stakeholder Analysis IDs rather than re-described inline | N-A | Not touched by the change; Pass in [RC-332] |
| 6 | Methodology and quality-standard foundation are stated explicitly (e.g. ISO/IEC 25010, Larman) | N-A | Not touched by the change; Pass in [RC-332] |
| 7 | Assumptions and constraints are explicit and clearly distinguished from one another | N-A | Not touched by the change; Pass in [RC-332] |
| 8 | Document supports executive decision-making with a clear, unambiguous recommendation | N-A | Not touched by the change; Pass in [RC-332] |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | en and it, unchanged |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | Unchanged |
| 3 | The content (prose and table cells) is written in the stated language | Pass | The three new lines are in English |
| 4 | The register matches the one the registry gives for the artifact type | Pass | The objective says what a person outside the team gets, before the means |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | No term of the hotel domain; the public site, the public name and the tailnet as in success criteria 31 and 37 |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | One file |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | Neither changed |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | S01 accepted the change in chat on 2026-11-17, with the plan ("1. Ja") |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | RFC 9116 is the name of the standard and stays as it is |

## Findings

- **No objective covered it.** Objective 40 and success criterion 43 are about what the team's own scans and alerts find. A report from a person outside the team had no objective, although `SECURITY.md` has promised its times since the security audit. Objective 42 names that need, and success criterion 45 makes it checkable.
- **The numbering holds.** Objectives 1 to 41 and success criteria 1 to 44 keep their numbers, as the milestones cite them.
- **The Version History keeps two rows.** The row of 2026-11-11 and its link left the table; the row of 2026-11-12 stays Accepted until this one is accepted.

## Overall Verdict

Go. Criteria 3 and 4 of QC-BC-001 pass, and every Mandatory criterion of QC-LANG-001 passes. S01 accepted the change in chat on 2026-11-17, with the plan of MIL-084 ("1. Ja"); the row of 2026-11-17 is Accepted and the row of 2026-11-12 Deprecated.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept the change in chat, with the plan of MIL-084: done 2026-11-17 | S01 | 2026-11-19 |

---

[BC-001]: ../../business-case.md
[QC-BC-001]: ../../../framework/qc/artifacts/qc-business-case.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-084]: ../../milestones/mil-084-a-security-txt-and-a-disclosure-policy.md
[RC-332]: ./rc-332-bc-001-products-of-2026-11-11.md
[91b74ec]: https://github.com/NF-Hotel/NFHotel/commit/91b74ecad8fe5599e484de8c979956a5ab7832f0
