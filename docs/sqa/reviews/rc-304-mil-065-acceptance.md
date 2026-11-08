# RC-304 Re-review of MIL-065 A remedy for each third-party image that the daily scan fails

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-304 |
| CrossReference | [MIL-065], [RC-301], [RC-302], [RC-303], [DSA-001], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the re-review that RC-301 asked for: S01 accepted MIL-065 in chat with its language and terms, and the plan holds the two additions S01 chose with it (the tag check in task 12, decision D2 as task 13 with criterion 9) | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-065], the plan, a second time: after the answer of S01 in chat on 2026-11-08 and with the two additions S01 chose in that answer
- Checklist used: [QC-MIL-001] and [QC-LANG-001]
- Scope: delta re-review of criteria 1, 2 and 3 of QC-MIL-001, because the additions change the deliverable, the criteria and the dependencies, and of criteria 3, 5 and 9 of QC-LANG-001, which [RC-301] left Pending; the earlier record is [RC-301] (Go-with-conditions). Its other rows still hold: the purpose, the traceability, the ownership, the target date and tasks 1 to 11 did not change.
- Language and domain: en / it (the Metadata rows of MIL-065)
- Language reviewer: S01, in chat (criteria 3, 5 and 9 of QC-LANG-001)
- Mode: autonomous, not independent. On 2026-11-08 S01 asked "Fortsæt så med at lave de sidste milestones i automode on" (go on with the last milestones in autonomous mode). The AI answered that MIL-065 is the only milestone left, sent S01 the document and the dry run of `sync-project.sh --milestone MIL-065`, said that an acceptance also confirms that the plan is in English and that its terms (image, digest, pin, release, gate, patch layer, ignore entry, accepted finding, daily scan) are the right ones, and offered three choices. The first was "Godkend alt: MIL-065 med wiki-tjekket, D2 som private, og jeg planlægger fundene fra 7. november som MIL-066 og viser dig planen" (accept all: MIL-065 with the wiki check, D2 as private, and the findings of 7 November planned as MIL-066 and shown to S01). S01 answered "godkendt" (accepted). The acceptance and the language rows are S01's; the stand-in walked the rows that the additions change.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Two outputs join the six of RC-301: a check in `test-wiki-coverage.sh` that Containers-and-Images names the tag that each compose file and Dockerfile pins (in the wiki bullet), and decision D2 recorded in section 15 of DSA-001 |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Criterion 7 now also asks that the test fail on a copy of Containers-and-Images with a stale tag; criterion 9 is new: the D2 row of DSA-001, and an anonymous request for the manifest of each of the six release images, refused. Task 15, the gate, names criteria 1 to 9 |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | The DSA-001 row names section 15 (decision D2) beside section 8 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged since RC-301 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01, approving reviewer S01, who accepted the plan; the Reviewer cell of the accepted row says S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-15, unchanged; S01 accepted the plan before it |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | Unchanged: en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | Unchanged |
| 3 | Content in the stated language | Pass | S01 confirmed it with the acceptance; the additions are in English as well, with the words of S01 quoted in Danish in the Version History |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | The additions follow the register of RC-301: the reason in the task, the script and the file where the task names them |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | S01 confirmed the terms with the acceptance, one word for each: image, digest, pin, release, gate, patch layer, ignore entry, accepted finding, daily scan; the additions use two more, tag and package, one word each |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | The language and the domain did not change |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01, the owner of the platform, accepted the plan in chat on 2026-11-08 after the AI had said that the acceptance also confirms the terms |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | GitHub Container Registry (GHCR), new in the Deliverable, is spelled out there |

## Findings

- **The conditions of RC-301 are met.** Criteria 3, 5 and 9 of QC-LANG-001 were Pending only because the language reviewer was a stand-in, and the plan waited for the acceptance of S01; S01 has accepted it and confirmed them.
- **The two additions are the choices of S01.** [RC-302] and [RC-303] left both to S01, the tag check "with the acceptance of MIL-065". The plan holds them in task 12 and as task 13, each with a criterion (7 and 9), and the two tasks after them move up by one. No issue of MIL-065 exists yet, so the new numbers break no link.
- **The accepted findings still need S01.** The acceptance of the plan is not the approval of its ignore entries: section 8 of [DSA-001] names S01 as their approver, and a stand-in may not approve a risk exception. They are approved in the review of the pull request (action item 1).
- **The milestone is published by hand.** The repository has no publish workflow (`sync-milestones.yml`), so the milestone and its fifteen issues are created with `sync-project.sh --milestone MIL-065 --apply` after the merge of this record, as the accepted dry run shows.

## Overall Verdict

Go. Every Mandatory criterion of QC-MIL-001 passes, and criteria 3, 5 and 9 of QC-LANG-001 pass with the confirmation of S01. MIL-065 is Accepted: its row of 2026-11-08 with S01 as reviewer is Accepted, and the row before it is Deprecated. The stand-in that walked the changed rows wrote the plan, so the review is not independent of the author; the acceptance is S01's.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Approve the accepted findings in `ops/security/trivyignore.yaml` (DSA-001 section 8) in the review of the pull request | S01 | Before the merge |
| Approve the CD run of the merge (ADR-0038) and the Tailscale check of `deploy-monitoring.sh --apply` on the monitoring container | S01 | At the merge |

---

[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[RC-301]: ./rc-301-mil-065.md
[RC-302]: ./rc-302-mil-064-gate.md
[RC-303]: ./rc-303-mil-064-gate-re-review.md
[DSA-001]: ../devsecops-assessment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
