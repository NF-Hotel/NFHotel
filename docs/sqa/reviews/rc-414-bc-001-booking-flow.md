# RC-414 Review of BC-001: objective 43 and success criteria 46 to 48 (the booking flow)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-414 |
| CrossReference | [BC-001], [QC-BC-001], [QC-LANG-001], [MIL-085], [MIL-086], [MIL-087], [MIL-088], [RC-332] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-19 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go; accepted by the AI as stand-in at S01's word in chat ("Du skal tage beslutningerne for mig") | pending |

---

## Artifact Under Review

- Instance reviewed: [BC-001], the row of 2026-11-19: objective 43, success criteria 46 to 48 and one line in scope, added for [MIL-085], [MIL-086], [MIL-087] and [MIL-088]
- Checklist used: [QC-BC-001] and [QC-LANG-001]
- Scope: delta re-review of criteria 3 and 4 of QC-BC-001, the two the change touches; the reason is the plan of MIL-085 to MIL-089; the earlier record is [RC-332]
- Language and domain: en / it (the Metadata rows of BC-001)
- Language reviewer: the AI as stand-in for S01 (criteria 3, 5 and 9 of QC-LANG-001)
- Mode: autonomous, not independent. The artificial intelligence (AI) assistant wrote the change and this record. S01 said in chat on 2026-11-19 that the AI takes the decisions ("Du skal tage beslutningerne for mig"); the acceptance is the AI's as stand-in and stays open to S01's challenge

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | ROI/Cost-Benefit analysis is quantitative, or where qualitative, is explicitly justified | N-A | Not touched by the change; Pass in [RC-332] |
| 2 | Risks are identified with documented impact and mitigation | N-A | Not touched by the change; Pass in [RC-332]. The risk row on an attack on the public site already names limits in the application |
| 3 | Success criteria are measurable, stating explicit targets rather than vague aspirations | Pass | Success criteria 46 to 48 name numbers and states that a test can show: a version 4 UUID with 122 random bits, 0 pages and operations with the counting number, one answer for the refusals; 5 years, 30 nights, 64 kilobytes; no room before the verification, 30 minutes, 1 hour, 1 open request, 3 coming bookings, 3 mails per hour, 5 requests per hour and 20 per day, status 429, exactly one of 20, an alert within 10 minutes |
| 4 | Scope explicitly separates In Scope vs Out of Scope | Pass | One line in scope for MIL-085 to MIL-088. Nothing moves out of scope |
| 5 | Stakeholders are cross-referenced to Stakeholder Analysis IDs rather than re-described inline | N-A | Not touched by the change; Pass in [RC-332] |
| 6 | Methodology and quality-standard foundation are stated explicitly (e.g. ISO/IEC 25010, Larman) | N-A | Not touched by the change; Pass in [RC-332] |
| 7 | Assumptions and constraints are explicit and clearly distinguished from one another | N-A | Not touched by the change; Pass in [RC-332] |
| 8 | Document supports executive decision-making with a clear, unambiguous recommendation | N-A | Not touched by the change; Pass in [RC-332] |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | en and it |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | en and it, as the registry's `PO language` and `PO domain` |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English throughout; the Danish quotations in the Version History are S01's words |
| 4 | The register matches the one the registry gives for the artifact type | Pass | The Purpose begins with the decision, and each finding says what it allows before the measures |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | No term of the hotel domain is new; the booking, the guest and the room as in the dictionary. Verify is used for the guest's step by email, and confirm stays the word for what staff do |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass |  |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | One file |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | Neither changed |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The AI as stand-in for S01, in autonomous mode |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | UUID, OWASP and ASVS are spelled out at their first use in the new lines; API stays as it is |

## Findings

- **No objective covered it.** Objective 21 lets a guest reach only the own booking, and objective 31 tests the site as an attacker would. Neither says that a booking must not be worked out from another, that a booking must have a guest behind it before it takes a room, or that one sender is bounded. Objective 43 names that need, and success criteria 46 to 48 make it checkable.
- **The numbers 42 and 45 are kept free.** They belong to the change of MIL-084 (vulnerability disclosure), which S01 has accepted and which is not merged yet. The list of objectives has a line that says so, because a numbered list is shown by position and objective 43 would otherwise be shown as 42. The line goes when the change of MIL-084 is merged.
- **The numbers of success criterion 48 are the defaults of MIL-087 and MIL-088.** When a decision record changes one, the criterion follows in the same pull request.
- **The Version History keeps two rows.** The row of 2026-11-11 and its link left the table; the row of 2026-11-12 is Deprecated.

## Overall Verdict

Go. Criteria 3 and 4 of QC-BC-001 pass, and every Mandatory criterion of QC-LANG-001 passes. The row of 2026-11-19 is Accepted as the AI's stand-in decision for S01 in autonomous mode, at S01's word in chat on 2026-11-19 ("Du skal tage beslutningerne for mig"), and the row of 2026-11-12 is Deprecated.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Replace the line that keeps number 42, and add success criterion 45, when the change of MIL-084 is merged | S01 (the AI as stand-in) | with the pull request of MIL-084 |

---

[BC-001]: ../../business-case.md
[QC-BC-001]: ../../../framework/qc/artifacts/qc-business-case.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[MIL-085]: ../../milestones/mil-085-a-booking-reference-that-cannot-be-guessed.md
[MIL-086]: ../../milestones/mil-086-booking-input-checked-by-one-set-of-rules.md
[MIL-087]: ../../milestones/mil-087-limits-on-what-one-sender-can-ask.md
[MIL-088]: ../../milestones/mil-088-a-booking-verified-by-email.md
[RC-332]: ./rc-332-bc-001-products-of-2026-11-11.md
