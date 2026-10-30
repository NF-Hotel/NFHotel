# MIL-049 Reference pages for the pipelines, containers, security controls and alerts

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-049 |
| CrossReference | [BC-001], [DSA-001], [ADR-0007], [ADR-0020], [ADR-0021], [ADR-0032] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-30 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-10-30 for documentation of every DevOps choice: the workflows, the containers and where their images come from, the security controls, and the monitoring and alerts, each with why it was chosen and its alternatives. S01 accepted the plan ("Accepter planen"); accepted after RC-249 Go | [a22d404] |

---

## Purpose

Decide whether a member of the team, or a teacher, can understand every DevOps and security choice of NF Hotel from the wiki alone: what runs, why it was chosen, what the alternatives were, and what is left as a risk. Today the facts are spread over 35 ADRs, the workflow files, the compose files and the monitoring rules, and the wiki pages CI-CD, Monitoring and Host-Hardening describe how to operate them, not the whole picture. The pages also have to stay true, so a test fails when a workflow, a container or an alert is added without a line on its page.

## Deliverable

- Four wiki pages in `docs/wiki/`, in English (the PO language, so no translation):
  - **Workflows:** every workflow in `.github/workflows/` and every composite action in `.github/actions/`, with its trigger, its jobs, what it checks, its permissions, why it exists, its alternatives and the ADR behind it; and the GitHub settings that work with them (ruleset, environments, code scanning, Dependabot, secret scanning).
  - **Containers-and-Images:** every service in `ops/deploy/*/docker-compose.yml`, with its purpose, image, registry, pin, user, hardening, networks and published ports; how the own images are built, scanned and attested; and why images come from three registries.
  - **Security-Controls:** the controls layer by layer (code, dependencies, supply chain, secrets, network, host, runtime, access, data), each with the tool, what it catches, why, the alternatives and the remaining risk, mapped to CIS-001, SAMM-001 and the OWASP Top 10.
  - **Alerts:** every alert rule in `ops/deploy/monitoring/`, with what it measures, its threshold, severity and route, and what to do; the service level objectives; and the outside watchdog.
- Links to the four pages from Home, `_Sidebar.md` and Appendix.
- `ops/tests/test-wiki-coverage.sh`, run by Ops CI: it fails when a workflow, a composite action, a compose service or an alert rule is missing from its page.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `test-wiki-coverage.sh` passes in Ops CI: each of the 10 workflows and 4 composite actions is named on Workflows, each compose service on Containers-and-Images, and each alert rule on Alerts | Yes | No |
| 2 | Each choice on the four pages names why and at least one alternative, or links the ADR that does; each fact names the file it comes from | Yes | No |
| 3 | No secret, password, token or private address beyond the tailnet names already in the wiki is on the pages | Yes | No |
| 4 | The pages are linked from Home, the sidebar and Appendix, and the wiki on GitHub shows them after the merge | Yes | No |
| 5 | The review of the pages and of the test is Go, and Ops CI and the wiki publish are green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0022 | The wiki is published from `docs/wiki/` on every merge |
| DSA-001 | The assessment that the security page sums up |
| MIL-023, MIL-034, MIL-036 | The monitoring, the objectives and the alerts that the Alerts page describes |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001: the team develops and supports NF Hotel on its own server, and its choices can be traced | Tasks 1 to 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-06.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Workflows page | `docs/wiki/Workflows.md`: each workflow and composite action with trigger, jobs, checks, permissions, why, alternatives and ADR; the ruleset, the `production` environment, code scanning, Dependabot and secret scanning. | No | criteria 1, 2 |
| 2 | Containers and images page | `docs/wiki/Containers-and-Images.md`: each compose service with purpose, image, registry, pin, user, hardening, networks and ports; the build, scan and attestation of the own images; why three registries. | No | criteria 1, 2 |
| 3 | Security controls page | `docs/wiki/Security-Controls.md`: the controls layer by layer with tool, what it catches, why, alternatives and remaining risk, mapped to CIS-001, SAMM-001 and the OWASP Top 10. | No | criteria 2, 3 |
| 4 | Alerts page | `docs/wiki/Alerts.md`: each alert rule with what it measures, threshold, severity, route and action; the objectives and the watchdog. | No | criteria 1, 2 |
| 5 | Links and coverage test | Home, `_Sidebar.md` and Appendix link the pages; `ops/tests/test-wiki-coverage.sh` and its job in Ops CI. | No | criteria 1, 4 |
| 6 | Review and release | The review record of the pages and the test; merge when green; check the published wiki; gate review. | No | criteria 3 to 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[a22d404]: https://github.com/NF-Hotel/NFHotel/commit/a22d40469f62e21f27efd92e53b0867868827c2b
