# RC-310 Review of MIL-067 Bad input on the booking page and the staff pages: task 7

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-310 |
| CrossReference | [MIL-067], [QC-MIL-001], [QC-LANG-001], [RC-309], [UC-005] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the change of the plan (finding 4, criterion 7, task 7) accepted by the AI as stand-in for S01 at S01's request in chat ("Ta selv stilling til alt"); Go | [6523bd3] |

---

## Artifact Under Review

- Instance reviewed: [MIL-067], the change of 2026-11-08 after [RC-309]: finding 4 in the Purpose, the change-password form in the Deliverable, the order of the tasks, criterion 7, the row of US-001.10 in the Traceability, task 7, and the gate against criteria 1 to 7
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: delta re-review of every criterion of both checklists against the change; the reason is finding 4, found while task 4 was done; the earlier record is [RC-309], whose verdict on the rest of the plan stands
- Language and domain: en / it (the Metadata rows of MIL-067)
- Language reviewer: the AI as stand-in for S01, at S01's request (criteria 3, 5 and 9 of QC-LANG-001)
- Mode: autonomous, not independent. While task 4 was done, the AI read the other forms of the web application with a password field. The change-password form ([UC-005]) clears its three passwords only in `ChangeAsync`, which `OnValidSubmit` runs, so a refused form writes them back. A test that was not kept posted the form with two different new passwords and found the current password in its field. The page is in no task of the plan, so the AI stopped before the merge of the plan and the pull request of the code and asked S01 whether to add it as task 7 or to plan it as MIL-068. S01 answered in chat "Ta selv stilling til alt" (decide everything yourself). The AI added it as task 7, as it had proposed, and walked both checklists as stand-in for S01.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | A sixth named output: the change-password form, which never writes a posted password back into the page. The other five are those of [RC-309] |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 7: a test through `WebFactory` posts the change-password form with two different new passwords and finds none of the three passwords in the answer, and fails against the code before the change; criteria 1 to 6 are unchanged |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged: task 7 needs no other milestone. The change-password page and its use case [UC-005] exist (MIL-030), and its tests in `StaffPasswordPageTests` are where the new test goes |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | US-001.10 (change my password) through task 7; BC-001 success criterion 22 (the back office login) through criteria 4 and 7 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01, approving reviewer S01. The change was accepted by the AI as stand-in for S01 at S01's request, and the Reviewer cell of its row says so |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-15, unchanged: task 7 is one page with the fix of task 4, and it goes in the same pull request, CD run and DAST scan |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it, unchanged |
| 2 | BCP 47 code and a domain from the registry list | Pass | `check-languages.sh --list` reads `MIL-067 en it` |
| 3 | Content in the stated language | Pass | English, with the words of S01 quoted in Danish in the Version History, as the project's rows do |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | Finding 4 states where it is and what it allows, as findings 1 to 3 do; class names and the test are in the criterion and the task |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | Staff member and password are the terms of the dictionary; the current password and the new password are the words of US-001.10; change-password form is used once for the page, as login form is for the other |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | The language and the domain did not change |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | The AI as stand-in for S01, at S01's request in chat; S01 can challenge the terms in the summary of the run |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | The change adds no abbreviation; `WebFactory`, `OnValidSubmit` and `ChangeAsync` are code literals |

## Findings

- **Finding 4 came from the work, not from a scan.** The scans do not sign in, so they do not reach the change-password page. Task 7 reproduces the finding with its test before the change, as task 4 did for finding 3, so the fix rests on a test and not on the reading.
- **The test that found it was not kept.** It changed no code; the plan-first gate held, and no code of task 7 is written before this record.
- **Task 7 has the number S01 was shown.** It was added when tasks 1 to 4 were committed with their trailers; the order sentence of the plan puts it before task 5, so the gate stays last without renumbering tasks 5 and 6.
- **One pull request, one CD run and one DAST scan** for tasks 1 to 4 and 7, so the change does not add a deploy for S01 to approve.
- **The other password fields are not the same case.** The fields for a new password and for a reset on the staff page are part of an interactive page (its circuit), not of a form whose answer is a new page, so a refused value is not written into an answer.

## Overall Verdict

Go. Every Mandatory criterion of QC-MIL-001 passes for the change, and criteria 3, 5 and 9 of QC-LANG-001 pass with the stand-in acceptance. The row of the change in MIL-067 is Accepted and the row of [RC-309] is Deprecated. The stand-in that walked the checklists wrote the change, so the review is not independent of the author; the decision to accept it was handed to the AI by S01.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Sync the milestone and its seven issues to GitHub after the merge of the plan | AI as stand-in for S01, at S01's request ("Ta selv stilling til alt") | After the merge |
| Do task 7 test first, on the branch of tasks 1 to 4 | AI as stand-in for S01 | 2026-11-15 |
| Approve the CD run of the merge of the code (ADR-0038) | S01 | At the merge |

---

[MIL-067]: ../../milestones/mil-067-bad-input-on-the-web-pages.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[RC-309]: ./rc-309-mil-067.md
[UC-005]: ../../use-cases/uc-005/uc.md
[6523bd3]: https://github.com/NF-Hotel/NFHotel/commit/6523bd34d78cf3c019b6a60cac3adf1c42c1b0e4
