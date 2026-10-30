# MIL-050 Technology choices: why each product was chosen over its alternatives

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-050 |
| CrossReference | [BC-001], [MIL-049], [ADR-0007], [ADR-0020], [ADR-0028] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-30 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-10-30 why each product was chosen over its alternatives (for example PostgreSQL rather than MySQL), with the reasons taken mainly from users on Reddit and Stack Overflow, for every step of the DevOps loop and for cybersecurity. S01 accepted the plan in chat and added every step of the CI/CD workflows and of the backup ("inkluder også alle de steps i workflowet CICD og backup osv."); accepted after RC-251 Go | pending |
| 2026-10-30 | Accepted | mikemike | AI as stand-in for S01 | Criterion 4: Reddit refuses the research tools, so S01 chose in chat on 2026-10-30 Hacker News, the Stack Overflow survey and the Stack Overflow answers instead ("1"); reviewed in RC-252 | pending |

---

## Purpose

Decide whether a member of the team, or a teacher, can see from the wiki why NF Hotel uses each product it uses, not only how it uses it. The ADRs and the MIL-049 pages explain how a product is set up and why that setup was chosen; they seldom say why PostgreSQL and not MySQL, Docker and not Podman, or Grafana and not Kibana. Many of these products were picked when the project started, so no ADR records the choice. This gate collects, for each product, the alternatives, the advantages and drawbacks, and what practitioners on Reddit and Stack Overflow give as reasons, and it says honestly where a choice was never decided in an ADR.

## Deliverable

- One wiki page, `docs/wiki/Technology-Choices.md`, in English, ordered by the eight steps of the DevOps loop (Plan, Code, Build, Test, Release, Deploy, Operate, Monitor) and a ninth section for cybersecurity. For each product:
  - what NF Hotel uses it for, and the file or ADR where it is set up;
  - the main alternatives (two or more);
  - advantages and drawbacks against those alternatives;
  - what practitioners say: the recurring reasons from Reddit and Stack Overflow threads, summarised in our own words, each with a link to the thread (no long quotes);
  - the project's own reason, the ADR that records it, or "Not decided in an ADR";
  - when an alternative would be the better choice.
- Two step-by-step sections, where each step names the tool it uses and why that tool and not its alternatives, with the community's reasons as above:
  - **The CI/CD pipeline:** every step of `ci.yaml`, `release.yaml`, `cd.yaml`, `e2e-dast.yaml`, `load-test.yaml`, `running-image-scan.yaml`, `scorecard.yaml`, `ops-ci.yaml`, `release-check.yaml` and `publish-wiki.yaml`, and of the four composite actions, in the order a change goes from a pull request to production: formatting, the NuGet vulnerability check, the tests and coverage, SonarQube Cloud, CodeQL, the language check, the image build and scan, the migration script, the push to GHCR, the attestations, the runner and the connection to the VM, the deploy, the scans and tests after it, and the wiki publish.
  - **Backup and restore:** each step of `backup-database.sh`, `install-backup.sh` and `restore-drill.sh`: the systemd timer, `pg_dump` in custom format, the encryption with age, the settings archive, the copy with rsync over SSH to a write-only target restricted by rrsync, the retention, the metrics to the monitoring, the backup before every deploy and the restore drill.
- The products covered (at least): GitHub Issues and Milestones, Discord; Git and GitHub, C# and .NET, Blazor, Bootstrap, Python, PostgreSQL, Entity Framework Core and Npgsql, Ollama; GitHub Actions, Docker, GHCR, the base images (Microsoft's .NET images, Debian slim, Alpine); xUnit, Playwright, k6, SonarQube Cloud, CodeQL, OWASP ZAP, Trivy; Dependabot, GitHub artifact attestations and SBOMs, OpenSSF Scorecard; Docker Compose, Proxmox VE, Debian, nginx, HAProxy, Cloudflare Tunnel, Cloudflare Access, Tailscale, the TLS certificates; pg_dump, age, rsync and rrsync, systemd timers, nftables, unattended-upgrades, secrets as files; Prometheus, Grafana, Loki, Grafana Alloy, Alertmanager, Healthchecks.io, OpenTelemetry, the blackbox exporter; GitHub secret scanning and the Docker socket proxy.
- Links from Home, `_Sidebar.md`, Appendix and from each of the four MIL-049 pages.
- `ops/tests/test-wiki-coverage.sh` also fails when a third-party image in the compose files has no entry on the page.
- The private documentation map gets a Danish tab "Teknologivalg" with the same products, alternatives, advantages, drawbacks and the community's reasons, linked to the sources.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | Every product in the list above, every third-party image of the compose files and every workflow file has an entry; `test-wiki-coverage.sh` passes in Ops CI | Yes | No |
| 2 | Every named step of the ten workflows and four composite actions, and every numbered step of `backup-database.sh` and `restore-drill.sh`, is in a step-by-step section with its tool and the reason for it | Yes | No |
| 3 | Each entry names at least two alternatives, advantages and drawbacks, and the project's reason or "Not decided in an ADR" | Yes | No |
| 4 | Each entry cites at least one practitioner source: a Hacker News or Stack Exchange thread, or the Stack Overflow Developer Survey 2025 (or, where none exists, the product's own documentation, marked as such); every link opens; no quote is longer than one sentence. Reddit is not used, because it refuses the research tools; the answers of the Stack Exchange threads are summarised under task 1 once they have been read | Yes | No |
| 5 | No secret, password, token or private address is on the page | Yes | No |
| 6 | The page is linked from Home, the sidebar, Appendix and the MIL-049 pages, and the wiki on GitHub shows it after the merge | Yes | No |
| 7 | The review of the page and of the test is Go, and CI and the wiki publish are green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-049 | The four reference pages this page links to, and the coverage test it extends |
| ADR-0022 | The wiki is published from `docs/wiki/` on every merge |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001: the team develops and supports NF Hotel on its own server, and its choices can be traced | Tasks 1 to 8 |

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
| 1 | Research the community's reasons | For each product and pipeline or backup step, the recurring reasons on Reddit and Stack Overflow for it and against its alternatives, with the thread links. | No | criterion 4 |
| 2 | Plan, Code and Build sections | The entries of the first three steps of the loop on `docs/wiki/Technology-Choices.md`. | No | criteria 1, 3, 4 |
| 3 | CI/CD pipeline step by step | Each step of the workflows and composite actions, in the order a change reaches production, with its tool and why. | No | criteria 2 to 4 |
| 4 | Backup and restore step by step | Each step of the backup, the copy off the VM and the restore drill, with its tool and why. | No | criteria 2 to 4 |
| 5 | Test, Release and Deploy sections | The entries of the next three steps of the loop. | No | criteria 1, 3, 4 |
| 6 | Operate, Monitor and cybersecurity sections | The entries of the last two steps and the security products. | No | criteria 1, 3, 4 |
| 7 | Links and coverage test | Home, `_Sidebar.md`, Appendix and the MIL-049 pages link the page; `test-wiki-coverage.sh` checks the third-party images and the workflow files on the new page. | No | criteria 1, 6 |
| 8 | Review and release | The review record; merge when green; check the published wiki and the links; gate review. | No | criteria 5 to 7 |

---

[BC-001]: ../business-case.md
[MIL-049]: mil-049-devops-security-reference.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
