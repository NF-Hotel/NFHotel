# RC-398 Review of the amendments of ADR-0045 and MIL-052 to S01's decisions of 2026-11-17

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-398 |
| CrossReference | [ADR-0045], [MIL-052], [QC-ADR-001], [QC-MIL-001], [QC-LANG-001], [QC-WIKI-001], [RC-357], [RC-385], [RC-399] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-17 | Accepted | mikemike | AI as stand-in for S01 | Initial version: delta review of the amendments of ADR-0045 and MIL-052 to S01's decisions of 2026-11-17, the Editor role, no expiry date and the account disabled to stop the access; Go; mode autonomous, not independent | pending |

---

## Artifact Under Review

- Instances reviewed, each its row of 2026-11-17 on the branch `mil-052-editor-decision` (from `main` at 747c429)
  before its pull request:
  - [ADR-0045]: the decision, with a paragraph on what S01 changed; point 3 (the Editor role, and what it allows),
    point 4 (no expiry date, and the copies of the settings file that hold the token), point 5 (`--disable-write` as
    the limit, with the tools of 2026-11-17) and point 6 (the account disabled to stop the access, deleted to stop it
    for good); the consequences and the quality attributes.
  - [MIL-052]: the purpose, the deliverable, criteria 3, 5 and 7, and tasks 4 and 5; [ADR-0045] in the
    CrossReference; the Version History cut to its two latest rows.
- Checklists used: [QC-ADR-001] for ADR-0045; [QC-MIL-001] and [QC-LANG-001] for MIL-052, which is written in the PO
  language.
- Scope: delta review, against every criterion of each checklist for what the amendment changed. The rest of each
  document stands as its latest record left it: [RC-385] for ADR-0045 and [RC-357] for MIL-052. The Context of
  ADR-0045, its options and its forces stay as they were weighed on 2026-11-15 (F1).
- Not reviewed here: the section "AI read access" of the wiki page Monitoring, changed on the same branch to the same
  decisions. Task 1 of MIL-052 changes the same page, so the page is reviewed against [QC-WIKI-001] once, at the delta
  re-review of the gate ([RC-399]).
- S01's decisions, in S01's words in chat on 2026-11-17:
  - "Rollen er Editor og ja Claude er genstartet. Du må gerne ha Editor adgang. Det bestemmer jeg." The service
    account, which S01 created as Editor, keeps that role.
  - "du må gerne beholde adgangen til grafana. det bestemmer jeg", and then "kan jeg ikke bare disable service
    account?". The assistant keeps the access; S01 stops it by disabling the account, and no token was deleted for
    the check of criterion 7.
  - "Ingen udløbsdato" and "Behold uden udløb", S01's answers to the questions on the expiry of the token. The token
    keeps no expiry date.
- Language and domain: en / it (the Metadata rows of MIL-052). ADR-0045 is a technical type.
- Language reviewer: none; the reviewer reads the language and knows the domain.
- Mode: autonomous, not independent. S01 asked in chat on 2026-11-17 that MIL-052 be finished ("kan du gøre denne
  færdig"), and gave the AI the acceptance of the plan of 2026-11-14 ("Du godkender for mig"). An AI wrote the
  amendments, and an AI wrote this record.

## Checklist Results: ADR-0045 (QC-ADR-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The ID (`ADR-NNNN`, 4-digit) matches the filename number and is unique and sequential | Pass | Unchanged |
| 2 | Context states the problem, the forces (cost, risk, constraints) and the options evaluated | Pass | Unchanged, as weighed on 2026-11-15; the decision says what S01 changed after it (F1) |
| 3 | At least two options were considered, or the absence of alternatives is justified | Pass | Unchanged: five options |
| 4 | Decision is stated in one or two unhedged sentences and matches one of the evaluated options | Pass | One sentence, still option 3: Grafana through `mcp-grafana`, its tools for metrics, alerts and dashboards, and no logs. Option 3 was weighed with the Viewer role; the paragraph after the sentence and point 3 say that S01 kept the Editor role and what that changes (F1) |
| 5 | Consequences list both Positive and Negative outcomes | Pass | Four positive and six negative. Changed: the path of the assistant, not the role, keeps reading and changing apart, and the account can be switched off and on again. Added: the Editor token can write in any other client, and an alert rule with a contact point could send what it reads, Loki included, to any address; the flags are in a file that the tools of the assistant could change (F2, F3); a copy of the token works until it is stopped. Removed: the renewal every 30 days |
| 6 | Quality-attribute impacts (performance, security, portability, etc.) are named where the decision affects them | Pass | Integrity now rests on the configuration of the server rather than on the role, and no expiry limits a copy of the token; the renewal is gone from the cost in maintainability |
| 7 | Affected Artifacts are listed as links, and each is also in CrossReference | Pass | Unchanged: [MIL-052], ADR-0020, ADR-0044, MIL-051 and RC-384, each in both |
| 8 | Status changes are recorded as new Version History rows with a Change summary and Commit link; retained rows are never rewritten | Pass | A row of 2026-11-17, `Accepted` after this Go, its commit link resolved after the commit; the row of 2026-11-15 is `Deprecated`, its content unchanged |
| 9 | A superseded or deprecated ADR names its successor, and the successor links back | N-A | Amended, not superseded (F1) |
| 10 | Written in business or technical language of the domain, with no unexplained abbreviations | Pass | No new abbreviation; Viewer, Editor and Admin are the names of the roles of Grafana |
| 11 | Status is one of `Proposed`, `Accepted`, `Rejected`, `Deprecated` or `Superseded by ADR-NNNN`, and every Version History row uses only these | Pass | `Deprecated` and `Accepted` |

## Checklist Results: MIL-052 (QC-MIL-001)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Still the ADR, the tailnet policy reconciled, the service account and the MCP server set up by S01, and a check that the access is read-only, now the list of the tools of the server; the planned Viewer role and expiry date are named beside S01's decisions |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | Seven criteria. Criteria 3, 5 and 7 changed, and each is still settled by a fact: the role in Grafana and the list of the tools of the server (3); the expiry date, or S01's decision recorded in [ADR-0045] point 4, and the search for the token (5); a refused request after the account is disabled or the token deleted (7). Criterion 3 no longer asks for refused writes (F4) |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged: ADR-0020 and MIL-051 |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: objectives 25 and 23 of BC-001 |
| 5 | Milestone owner and approving reviewer are identified | Pass | Unchanged: S01 and S01 |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | Unchanged: 2026-12-02 |

## Language and Domain Results (MIL-052)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | The Metadata table has a `Language` row and a `Domain` row, and neither is a placeholder | Pass | en and it |
| 2 | `Language` is a BCP 47 code and `Domain` is a value from the registry's domain list | Pass | Unchanged |
| 3 | The content (prose and table cells) is written in the stated language | Pass | English |
| 4 | The register matches the one the registry gives for the artifact type | Pass | IT Executive English |
| 5 | Domain terms are the PO terms of the domain's dictionary, with no synonyms | Pass | No new term; Viewer, Editor and service account are the names Grafana uses |
| 6 | Metadata keys, section headings, IDs and statuses are in English | Pass | |
| 7 | No translated twin (`<name>.<language>.md`) exists beside the document | Pass | One file |
| 8 | A change of language or domain since the previous accepted version has a Version History row and was reviewed again | N-A | No change of language or domain |
| 9 | A reviewer competent in the domain, and in the language, has confirmed that the domain terms are used correctly | Pass | The AI as stand-in for S01; not independent |
| 10 | Abbreviations are spelled out on first use, in the stated language | Pass | No new abbreviation |

## Findings

- **F1, note: amended in place, away from option 3 as it was weighed.** Option 3 was weighed with the Viewer role,
  and the forces asked for "read only, one identity, an expiry, and a revocation that works at once". S01, the owner
  and approving reviewer of [MIL-052], kept the Editor role and no expiry date. The record keeps its Context, its
  options and its forces as weighed on 2026-11-15, and says in the decision, in points 3, 4 and 6 and in the
  consequences what changed and what it costs. It is amended rather than superseded because the data, the path, the
  server and its flags, and the person stand; only the role, the expiry and the way to stop the access changed. Under
  the forces as weighed, the Viewer role with an expiry was the stronger choice; the choice is S01's, and the record
  now states its cost.
- **F2, note: what the Editor role allows, read from the token.** The permissions of the token, read on 2026-11-17
  with `GET /api/access-control/user/permissions`, include creating, changing and deleting dashboards, folders,
  annotations and Grafana-managed alert rules in every folder (`alert.rules:create`), creating silences
  (`alert.silences:create`) and contact points (`alert.notifications.receivers:create`), and changing the
  notification policies. They do not include `datasources:create`, `datasources:write` or
  `alert.provisioning:write`. Point 3 and the negative consequences say this, and that an alert rule with a contact
  point could send what it reads from any data source, Loki included, to an address outside. Grafana has no data
  source for the Alertmanager of the stack, so a silence made in Grafana stops no alert in Discord.
- **F3, note: the read-only path rests on rules on S01's PC.** With the Viewer role Grafana refused a write from any
  client; with the Editor role nothing in Grafana does. Two things keep the assistant from writing: the server
  registers no tool that writes (point 5), and the token stays in the settings of Claude Code (point 4). Both live on
  S01's PC: the flags are in a file that the tools of the assistant could change, and a copy of the token works in
  any client until S01 disables the account. The negative consequences of ADR-0045 say both. The assistant does not
  change the entry `grafana` of `~/.claude.json`, and the checks of [RC-399] read the token only in memory and print
  status codes, counts and booleans.
- **F4, note: the refused writes were left out, and none was sent.** Criterion 3 as planned asked for four writes
  refused with HTTP 403. With the Editor role the write of a dashboard and of a silence would have succeeded and
  changed Grafana, so the check of the entry read the permissions first and stopped before any write: no write
  request was sent with the token. Criterion 3 now asks for the role that S01 decided and the list of the tools of
  the server.
- **F5, note: not independent.** An AI wrote the amendments and this record, and the AI accepted them as stand-in for
  S01, whose decisions they record. S01 can challenge them at the review of the pull request.

## Overall Verdict

Go. Every Mandatory criterion of QC-ADR-001, QC-MIL-001 and QC-LANG-001 passes for the amendments, and the rest of
each document stands as its latest record left it. The amendments record S01's decisions of 2026-11-17, quoted above,
and what they cost (F1 to F3). The AI accepted both amendments and this record as stand-in for S01: the row of
2026-11-17 of each is `Accepted` and the row before it `Deprecated`. The reviewer is not independent (F5).

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Accept this record and the two amendments, or return them | S01; done on 2026-11-17 by the AI as stand-in, at S01's word in chat | At the review of the pull request of `mil-052-editor-decision` |
| Delete the token `claude-code` and the service account `ai-assistant-read` when the project ends, or sooner if the access is no longer used ([ADR-0045] point 6) | S01 | When the project ends |
| Review the section "AI read access" of the wiki page Monitoring against [QC-WIKI-001] | S01 (the AI as stand-in) | At the delta re-review of [RC-399] |

---

[ADR-0045]: ../../adr/adr-0045-read-only-monitoring-access-for-the-ai-assistant.md
[MIL-052]: ../../milestones/mil-052-read-only-grafana-for-the-ai-assistant.md
[QC-ADR-001]: ../../../framework/qc/artifacts/qc-adr.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
[QC-WIKI-001]: ../../../framework/qc/artifacts/qc-wiki.md
[RC-357]: ./rc-357-mil-052-revision.md
[RC-385]: ./rc-385-adr-0045.md
[RC-399]: ./rc-399-mil-052-gate.md
