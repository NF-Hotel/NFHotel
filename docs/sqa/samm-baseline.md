# OWASP SAMM v2 Self-Assessment (Baseline)

## Metadata
| Key | Value |
| --- | --- |
| ID | SAMM-001 |
| CrossReference | [MIL-023], [DSA-001], [ADR-0007], [ADR-0008], [ADR-0011], [ADR-0012] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | AI as stand-in for S01 | Initial version (MIL-023 task 5); reviewed in RC-177 | pending |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Ported from NF-Hotel/DevOps#10 into NF-Hotel/NFHotel under ops/ (MIL-031 task 7, ADR-0016); IDs renumbered; accepted after RC-177 Go, ported in RC-178 | pending |

This document type is not in `framework/registry/artifact-catalog.md`; like DSA-001, a catalog type for it is proposed upstream before it is accepted.

---

## 1. Scope and method

- **Scope.** The NF Hotel software and its delivery: [NF-Hotel/NFHotel] (the Blazor web app, the API, the tests and the workflow entry points) and [NF-Hotel/DevOps] (the actions, scripts and deploy files), with their GitHub settings, as of 2026-10-20.
- **Model.** OWASP SAMM v2: 5 business functions, 15 security practices, 2 streams per practice, maturity 0 to 3 per stream.
- **Scoring.** Each stream is scored against the SAMM v2 maturity criteria of its levels, with the quality answers of the SAMM toolbox: 0, 0.25 (some), 0.5 (at least half), 1 (most or all) per level. A stream score is the sum of its levels; a practice score is the mean of its two streams. The 90 toolbox questions were not answered one by one; that is the next assessment (section 5).
- **Evidence.** Every score rests on a file, a setting read through the GitHub API, or a run, all on 2026-10-20. What could not be checked is marked **not verified**. A score is never given for a control that is only planned; planned work is named under "Next level".
- **Independence.** This is a self-assessment by the team, with an AI as stand-in for S01. It is not an independent audit and claims no compliance.

## 2. Evidence base

| # | Evidence | Source |
| --- | --- | --- |
| E1 | Ruleset `Approve merges` on `main` of NFHotel: a pull request with 1 approval, stale approvals dismissed, approval of the last push, no bypass actors, deletion and force-push blocked, 6 required checks (build and tests, formatting, Docker build, release build, end-to-end tests, DAST baseline) | `GET /repos/NF-Hotel/NFHotel/rulesets/{id}` |
| E2 | Ruleset `Protect main` on `main` of DevOps: the same review rules plus resolved threads, 7 required checks (linters, script tests, nginx, PostgreSQL, release build) | `GET /repos/NF-Hotel/DevOps/rulesets/{id}` |
| E3 | Reviews: 17 of the last 30 merged pull requests of NFHotel have an approval by someone other than the author (the others predate the ruleset); DevOps has 1 merged pull request (#6), without an approval | `gh pr list --state merged` |
| E4 | CodeQL default setup on NFHotel for C# and GitHub Actions (configured 2026-10-19), 0 open alerts; none on DevOps | `GET /code-scanning/default-setup` |
| E5 | OWASP ZAP baseline on every pull request (a required check) and a weekly full scan, SARIF in code scanning: 79 open alerts on NFHotel (47 warning, 32 note) | `e2e-dast.yaml`; `GET /code-scanning/alerts` |
| E6 | Trivy scan gate on the release images (fails on a critical finding, or a high one with a fix), SBOM and build provenance attested per image, release by digest | DevOps `scripts/scan-gate.sh`, `.github/actions/release-images`; [ADR-0007]; MIL-022 |
| E7 | Attestations verified before a deploy; migrations applied by CD as `nfhotel_migrator`, the API runs as `nfhotel_app` | DevOps `scripts/cd-steps.sh verify` and `deploy` (NF-Hotel/DevOps#8, not merged yet); [ADR-0011] |
| E8 | NuGet vulnerability check on every CI run; Dependabot security updates on in both repositories; Dependabot version updates with a 7-day cooldown in DevOps; actions pinned by full commit SHA | NFHotel `ci.yaml`; `GET /repos/{repo}` (`security_and_analysis`); DevOps `.github/dependabot.yml` |
| E9 | Secret scanning and push protection on in both repositories; settings files with secrets are git-ignored; no stored deploy secret (Tailscale through GitHub OIDC, Tailscale SSH) | `security_and_analysis`; `.gitignore`; NFHotel `cd.yaml` |
| E10 | Environment `production`: 2 required reviewers, self-review prevented, a custom branch policy | `GET /repos/NF-Hotel/NFHotel/environments` |
| E11 | Architecture decisions: NFHotel ADR-0001 (clean architecture), ADR-0005 (back-office login), ADR-0006 (TLS), ADR-0012 (repository ownership); DevOps ADR-0007 to ADR-0011, ADR-0500 | `docs/adr/` of both |
| E12 | Risk register R1 to R17 with likelihood, impact, treatment and residual risk; standards mapping; quality gate matrix | [DSA-001] sections 5, 6 and 8 |
| E13 | SQA review records for every milestone, ADR and code change (for example RC-503, RC-504, RC-505), against the framework checklists (`qc-programming-shell`, `qc-yaml`, `qc-workflow`) | `docs/sqa/reviews/` of both |
| E14 | `SECURITY.md` of NFHotel: private reporting, response times (critical 7 days, high 30 days), rotation of a leaked secret. **Private vulnerability reporting is off in both repositories**, so the channel it names does not work | `GET /repos/{repo}/private-vulnerability-reporting` answers `enabled: false` |
| E15 | Daily backup encrypted with age, restore drill passed on 2026-10-20; no copy off the VM yet | [RC-505]; [ADR-0008] |
| E16 | Tests: unit, integration and end-to-end projects in NFHotel `tests/` (login, booking guards, E2E signed in and out); script tests in DevOps `tests/` (scan gate, DAST rules, CD steps, deploy) | `tests/` of both; CI runs |
| E17 | CIS hardening, nftables, patching and uptime check are planned and in progress (MIL-023 tasks 1 to 4) | [MIL-023] |
| E18 | OpenSSF Scorecard workflow (weekly and on `main`) and the DORA metrics script with its first monthly report | MIL-023 task 5 (this change); [DORA-001] |

## 3. Scores

### 3.1 Summary

| Business function | Practice | Stream A | Stream B | Practice score |
| --- | --- | --- | --- | --- |
| Governance | Strategy and Metrics | 1.0 | 0.5 | 0.75 |
| Governance | Policy and Compliance | 1.0 | 0.5 | 0.75 |
| Governance | Education and Guidance | 0.25 | 0.5 | 0.38 |
| Design | Threat Assessment | 1.0 | 0.5 | 0.75 |
| Design | Security Requirements | 0.5 | 0.5 | 0.50 |
| Design | Security Architecture | 1.0 | 1.0 | 1.00 |
| Implementation | Secure Build | 2.0 | 1.5 | 1.75 |
| Implementation | Secure Deployment | 2.0 | 1.5 | 1.75 |
| Implementation | Defect Management | 1.0 | 0.25 | 0.63 |
| Verification | Architecture Assessment | 0.5 | 0.5 | 0.50 |
| Verification | Requirements-driven Testing | 1.0 | 0.25 | 0.63 |
| Verification | Security Testing | 1.5 | 0 | 0.75 |
| Operations | Incident Management | 0.5 | 0.25 | 0.38 |
| Operations | Environment Management | 1.0 | 1.0 | 1.00 |
| Operations | Operational Management | 1.0 | 0.5 | 0.75 |

| Business function | Score (mean of its practices) |
| --- | --- |
| Governance | 0.63 |
| Design | 0.75 |
| Implementation | 1.38 |
| Verification | 0.63 |
| Operations | 0.71 |
| **Overall** | **0.82** |

The profile is what one would expect of a small team that invested in its pipeline first: Implementation (build and deployment) is the strongest function, close to level 2. Governance, Verification of the design and Operations are below level 1.

### 3.2 Governance

| Practice / stream | Score | Evidence | Next level |
| --- | --- | --- | --- |
| Strategy and Metrics A: Create and Promote | 1.0 | BC-001 objective 25 and success criterion 28 set the security goals; [DSA-001] is the security strategy and roadmap for the delivery (E12) | A roadmap reviewed on a schedule, with budget and owners per item |
| Strategy and Metrics B: Measure and Improve | 0.5 | Scorecard and DORA are measured from this month on (E18); no targets yet, one data point | Targets per metric, a monthly review of the DORA report and the Scorecard trend |
| Policy and Compliance A: Policy and Standards | 1.0 | `SECURITY.md`, `CONTRIBUTING.md`, the framework QC checklists and coding conventions applied in every review (E13, E14) | Security standards per technology (Blazor, ASP.NET, PostgreSQL), checked automatically |
| Policy and Compliance B: Compliance Management | 0.5 | [DSA-001] section 5 maps controls to standards (SSDF, SLSA, CIS, OWASP); the passport numbers make GDPR relevant, but no record of processing or retention rule exists | A list of the obligations (GDPR) with the controls that meet them |
| Education and Guidance A: Training and Awareness | 0.25 | Reviewer training exists as a framework process; no security training is recorded for the team | A short secure-coding course for each developer (OWASP Top 10 for ASP.NET), recorded |
| Education and Guidance B: Organization and Culture | 0.5 | Two maintainers own every path through CODEOWNERS; reviews are the norm (E1, E3); no security champion is named | Name a security champion; share findings (ZAP, CodeQL) in the team |

### 3.3 Design

| Practice / stream | Score | Evidence | Next level |
| --- | --- | --- | --- |
| Threat Assessment A: Application Risk Profile | 1.0 | Risk register R1 to R17 with likelihood, impact and treatment (E12); the data at risk (guest, booking and passport data) is named | A risk profile per application (web, API, market analysis) that sets how much assurance each needs |
| Threat Assessment B: Threat Modeling | 0.5 | The register is threat-based for the pipeline and the VM; the web app and the API have no threat model (data flow, trust boundaries) | A STRIDE threat model of the web app and the API, kept with the design |
| Security Requirements A: Software Requirements | 0.5 | Security requirements appear in milestones (MIL-016 back-office login, MIL-017 TLS, MIL-028 headers), not derived systematically | Derive security requirements per user story from the risk profile (OWASP ASVS level 1) |
| Security Requirements B: Supplier Security | 0.5 | Actions pinned by SHA and released at least a week before use; ADR-0500 freezes an image whose source vanished (R17) (E8) | Criteria for accepting a dependency or an action, and a check of its maintenance |
| Security Architecture A: Architecture Design | 1.0 | ADRs for clean architecture, login, TLS, CI/CD, database access with least privilege (E11) | Reference security patterns (authentication, secrets, logging) that every new component uses |
| Security Architecture B: Technology Management | 1.0 | Technology choices are ADRs; versions pinned and updated by Dependabot (E8, E11) | A list of the approved technologies and their end of life |

### 3.4 Implementation

| Practice / stream | Score | Evidence | Next level |
| --- | --- | --- | --- |
| Secure Build A: Build Process | 2.0 | Images built once in CI, never on the VM; required checks gate the merge; build provenance and SBOM attested; released by digest (E1, E2, E6) | Level 3 needs the build integrity checked everywhere: the attestation check before a deploy (E7) once NF-Hotel/DevOps#8 is merged |
| Secure Build B: Software Dependencies | 1.5 | NuGet check in CI, Trivy gate, SBOM per image as the inventory, Dependabot (E6, E8) | A dependency policy with an owner, and the SBOMs checked against new advisories after the release |
| Secure Deployment A: Deployment Process | 2.0 | CD after an approval by a second person, deploy by digest, dry run and rollback by run ID (E10); the deploy log of MIL-023 task 4 is planned | Level 3: the attestation check in CD (E7) and a post-deploy smoke test that stops a bad release |
| Secure Deployment B: Secret Management | 1.5 | No stored deploy secret (OIDC, Tailscale SSH); secret scanning with push protection; production secrets in settings files on the VM, not in a vault (E9; their file permissions not verified in this assessment) | Rotation of the production secrets on a schedule, with a record |
| Defect Management A: Defect Tracking | 1.0 | Findings land in code scanning, Dependabot alerts and issues; response times in `SECURITY.md` (E5, E14); the 79 ZAP alerts are not triaged | Triage every alert with a severity and a due date; the reporting channel of `SECURITY.md` enabled |
| Defect Management B: Metrics and Feedback | 0.25 | Counts can be read from the API; nobody reports them | The open security findings by age and severity in the monthly report |

### 3.5 Verification

| Practice / stream | Score | Evidence | Next level |
| --- | --- | --- | --- |
| Architecture Assessment A: Architecture Validation | 0.5 | Every ADR has a review record (E13); the reviews check the decision, not its security against a threat model; mostly by an AI stand-in, not independent | A security review of the architecture against the threat model, by a person |
| Architecture Assessment B: Architecture Mitigation | 0.5 | Review findings become action items with owners (for example RC-505) | Track the action items to closure across reviews |
| Requirements-driven Testing A: Control Verification | 1.0 | Tests of the security controls: login and guest login, booking guards, end-to-end signed in and out, the scan gate and the DAST rules (E16) | A test per security requirement (traceability matrix) |
| Requirements-driven Testing B: Misuse and Abuse Testing | 0.25 | Some negative tests (refused login, guards); no abuse cases | Abuse cases from the threat model (for example booking enumeration, brute force on login) |
| Security Testing A: Scalable Baseline | 1.5 | CodeQL on C# and Actions, ZAP baseline on every pull request (required), weekly ZAP full scan, Trivy on every release (E4, E5, E6) | CodeQL on the Actions of DevOps; the ZAP findings triaged so that the baseline means "no new findings" |
| Security Testing B: Deep Understanding | 0 | No manual penetration test or expert review | A manual test of the login, the API and the headers before a wider launch |

### 3.6 Operations

| Practice / stream | Score | Evidence | Next level |
| --- | --- | --- | --- |
| Incident Management A: Incident Detection | 0.5 | Health checks of the containers; the external uptime check is planned (E17); no alerting on security events | The uptime check and alerts on failed logins and container restarts |
| Incident Management B: Incident Response | 0.25 | Response times and secret rotation in `SECURITY.md` (E14); no runbook, no roles | An incident runbook (who, how to roll back, who tells the guests), tried once |
| Environment Management A: Configuration Hardening | 1.0 | Configuration as code in DevOps (compose, nginx, VM setup); CIS items selected and applied in MIL-023 tasks 1 to 3 (E17; not verified on the VM in this assessment) | The CIS checklist complete with evidence, and drift checked after every deploy |
| Environment Management B: Patching and Updating | 1.0 | Dependabot for actions, images and compose files; Trivy fails a release with a fixable high finding (E6, E8); host patching planned | `unattended-upgrades` and the monthly rebuild in place, with a record |
| Operational Management A: Data Protection | 1.0 | Encrypted daily backup and a passed restore drill; least-privilege database roles (E15, E7); no off-VM copy, no data classification | An off-VM copy, and a retention rule for the passport data |
| Operational Management B: System Decommissioning and Legacy | 0.5 | ADR-0500 freezes a legacy image; old copies of the infrastructure in NFHotel are scheduled for removal (MIL-025 task 7) | An inventory of the services and images with their owner and end of life |

## 4. Findings that need an owner now

1. **The vulnerability reporting channel is off.** `SECURITY.md` sends reporters to "Report a vulnerability" in the Security tab, but private vulnerability reporting is disabled in both repositories (E14). S01 enables it (Settings, Code security), or the policy names another channel.
2. **The ZAP alerts are not triaged.** 79 open alerts (E5); MIL-028 addresses the headers behind most of them.
3. **DevOps has no static analysis.** CodeQL for GitHub Actions is free for public repositories and would cover the composite actions and workflows (E4).
4. **Not every merged change in DevOps was reviewed** (E3): #6 was merged without an approval. Since the ruleset `Protect main` (E2), this cannot happen again without a bypass.

## 5. Next assessment

Repeat in three months (2027-01, with the close of MIL-023), answering the 90 questions of the SAMM v2 toolbox one by one, and compare per stream. Proposed targets for that assessment: Defect Management and Incident Management at 1.0, Threat Assessment B at 1.0 (a threat model of the web app and the API), and Secure Build and Secure Deployment A at 3.0 once the attestation check is in production.

---

[NF-Hotel/NFHotel]: https://github.com/NF-Hotel/NFHotel
[NF-Hotel/DevOps]: https://github.com/NF-Hotel/DevOps
[MIL-023]: ../milestones/mil-023-operational-quality.md
[DSA-001]: ./devsecops-assessment.md
[DORA-001]: ./dora/dora-001-2026-10.md
[RC-505]: ./reviews/rc-505-restore-drill-2026-10-20.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[ADR-0011]: ../adr/adr-0011-postgresql-access-for-the-api.md
[ADR-0012]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0012-devops-repository-ownership.md
