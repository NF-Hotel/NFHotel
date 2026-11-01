# MIL-053 Security scan results in Grafana

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-053 |
| CrossReference | [BC-001], [ADR-0007], [ADR-0020], [MIL-051] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-01 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-11-01 to see the results of the SAST and DAST scans of the workflows in Grafana, and to have it planned and implemented ("Ja og implementere det også"); accepted after RC-256 Go | pending |

---

## Purpose

Decide whether the team can see in Grafana what the security scans of the pipeline find, and get an alert when a scan finds something serious or stops running. Today the results stay where each tool puts them: CodeQL, Trivy, OWASP ZAP and OpenSSF Scorecard in GitHub code scanning, and SonarQube Cloud on its own site. Grafana shows the operation of the server only, so a new high finding or a weekly scan that stopped is seen only by someone who opens those pages.

## Deliverable

- **A scan exporter** in `ops/deploy/monitoring/scan-exporter/`: a Python program with the standard library only, run in the pinned `python:3.13-slim` image with the code mounted read-only (no image to build). Every 15 minutes it reads:
  - the open alerts of GitHub code scanning, counted by tool, category and severity;
  - the time of the latest analysis of each tool and category;
  - the latest completed run on `main` of each scan workflow (`ci.yaml`, `release.yaml`, `e2e-dast.yaml`, `running-image-scan.yaml`, `scorecard.yaml`) and whether it succeeded;
  - the measures of the public SonarQube Cloud project (quality gate, bugs, vulnerabilities, security hotspots, code smells, coverage, duplication);
  - and serves them as Prometheus metrics on the compose network only, with an up metric per source.
- **The token** for GitHub is a fine-grained token of S01, read-only (code scanning alerts, actions and metadata of `NF-Hotel/NFHotel`), in the git-ignored secret file `secrets/github-scan-token`, owned by the exporter's user, mode 0400. Without the file the exporter still reports SonarQube Cloud and the workflow runs, and says that code scanning is not read.
- **Prometheus** scrapes the exporter (job `scans`).
- **Alert rules** in `prometheus/rules/scans.yml` with unit tests in `rules-test.yml`: open critical or high findings, a weekly scan that has not run for 8 days, a failed quality gate, a scan workflow whose latest run failed, and a source that cannot be read.
- **A Grafana dashboard** "Security scans": the open findings by tool and severity over time, the SonarQube Cloud measures, the age of each tool's latest analysis and the latest run of each scan workflow.
- **Tests**: unit tests of the exporter with recorded API answers (no network), run in Ops CI; the stack test starts the exporter with the other services and checks that Prometheus scrapes it.
- **Documentation**: `docs/wiki/Monitoring.md` (the token and the dashboard), `docs/wiki/Alerts.md` (the new rules) and `docs/wiki/Containers-and-Images.md` (the new service).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The unit tests of the exporter pass in Ops CI, with no network call, and cover the counting by tool and severity, pagination, a failed source and a missing token | Yes | No |
| 2 | `promtool check config` and `promtool test rules` pass with the new job and rules, and every new rule has a test that fires and one that does not | Yes | No |
| 3 | The stack test starts six services, and Prometheus reports `up{job="scans"} == 1` | Yes | No |
| 4 | The exporter runs read-only, as a user of its own, with every capability dropped and no published port; the token file is readable by the exporter's user only | Yes | No |
| 5 | No token appears in the repository, a log line of the exporter, a metric or a workflow log | Yes | No |
| 6 | After the deploy on `nf-monitoring`, the dashboard "Security scans" shows the open findings and the SonarQube Cloud measures, and the numbers match GitHub code scanning and sonarcloud.io | Yes | No |
| 7 | `test-wiki-coverage.sh` passes (new service and alerts on their pages), and the code review against `QC-PY-001` and `QC-SH-001` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0020 | The monitoring stack this adds a service, a job, rules and a dashboard to |
| ADR-0007 | The scans of the pipeline whose results are read |
| MIL-051 | Not required: until it is done, S03 deploys the change with `deploy-monitoring.sh --apply` as today |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 24: every released image has a vulnerability scan; here its findings are watched | Tasks 1, 3, 4 |
| BC-001 objective 25: observe the server and measure the posture | Tasks 1 to 6 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-08. It changes no application code, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Scan exporter | The Python exporter of GitHub code scanning, the scan workflow runs and SonarQube Cloud, with unit tests on recorded answers. | No | criteria 1, 5 |
| 2 | Service, secret and scrape job | The `scan-exporter` service in the compose file with the hardening of the other services, the optional token file in `deploy-monitoring.sh`, and the `scans` job in Prometheus. | No | criteria 3 to 5 |
| 3 | Alert rules | `prometheus/rules/scans.yml` and its unit tests in `rules-test.yml`. | No | criterion 2 |
| 4 | Dashboard "Security scans" | The provisioned dashboard of findings, measures, scan ages and workflow runs. | No | criterion 6 |
| 5 | Tests in Ops CI | The exporter's unit tests in the `monitoring` job, and the stack test with six services and the scrape. | No | criteria 1, 3 |
| 6 | Wiki, review and deploy | Monitoring, Alerts and Containers-and-Images pages; code review record; S01 creates the token, S03 deploys, and the dashboard is checked against GitHub and sonarcloud.io. | No | criteria 6, 7 |

---

[BC-001]: ../business-case.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0020]: ../adr/adr-0020-monitoring-stack.md
[MIL-051]: mil-051-monitoring-continuous-deployment.md
