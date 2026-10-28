# DevOps Lifecycle

How NF Hotel covers the eight phases of the DevOps lifecycle (Plan, Code, Build, Test, Release, Deploy, Operate, Monitor), with the tools and techniques the project uses and the documents that decide them.

**For:** the team and anyone who wants the whole picture before reading the detailed pages.
**Related:** [CI-CD](CI-CD), [Host Hardening](Host-Hardening), [Monitoring](Monitoring), [Incidents](Incidents), [Disaster Recovery](Disaster-Recovery), [Public Access](Public-Access).

**Contents**
1. [Plan](#1-plan)
2. [Code](#2-code)
3. [Build](#3-build)
4. [Test](#4-test)
5. [Release](#5-release)
6. [Deploy](#6-deploy)
7. [Operate](#7-operate)
8. [Monitor](#8-monitor)
9. [Where NF Hotel differs from the textbook](#where-nf-hotel-differs-from-the-textbook)

---

## 1. Plan

- **The SQA and QC framework** (`framework/`, maintained by Jens): the Business Case, the Stakeholder Analysis, the Project Plan, user stories and use cases, each reviewed before the next step.
- **Milestones with Go/No-Go criteria** (`docs/milestones/`, MIL-001 onward). Each task of a milestone is synced to **GitHub Milestones and Issues** by `framework/scripts/sync-project.sh`, and a pull request closes the issues it completes.
- **Architecture Decision Records** (`docs/adr/`) record every decision with the options that were weighed.
- **Review records** (`docs/sqa/reviews/`, RC-*) record each review against its checklist, with the verdict.

## 2. Code

- **One repository on GitHub** (ADR-0016): the application (.NET 10, Blazor, an API) and its operations in `ops/`.
- **Branches and pull requests.** `main` is protected by a ruleset with required checks; CODEOWNERS covers `ops/` and `.github/`.
- **Infrastructure as Code:** Docker Compose files, the nftables firewall, the Docker daemon settings, the systemd units, the Prometheus, Alertmanager, Loki and Alloy configuration and the Grafana dashboards are files in `ops/`.
- **Conventions and architecture:** the coding conventions of the framework, Clean Architecture, and architecture tests that fail the build on a breach. Code reviews are recorded as RC-* against the `qc-programming-*` checklists.

## 3. Build

- **.NET SDK** with the version of `global.json`; NuGet packages pinned by hash in `packages.lock.json` and restored in locked mode (MIL-044).
- **Docker multi-stage builds**, base images pinned by digest, security updates installed at build time.
- **GitHub Actions** builds everything; **Dependabot** proposes updates of packages, images and actions.

## 4. Test

- **xUnit v3:** unit tests, integration tests against a real PostgreSQL (**Testcontainers**), and web tests through **WebApplicationFactory**; more than 1 500 tests.
- **End-to-end tests** with **Playwright** against a throwaway stack, and **load tests** with **k6**.
- **Security and quality:** **CodeQL** and **SonarQube Cloud** (static analysis), **OWASP ZAP** (DAST, baseline and full scan), **Trivy** (container scan), **OpenSSF Scorecard**, secret scanning with push protection and NuGet audit.
- **Operations code:** shellcheck, shfmt, actionlint, yamllint, promtool, amtool and script tests in the ops CI.

## 5. Release

- The workflow `NF Hotel Release` builds the four images after a green CI on `main`. **Trivy** stops the release on a critical finding, or a high one with a fix.
- Each image gets an **SBOM** and a signed **build provenance attestation**, and is pushed to the **GitHub Container Registry** by digest (MIL-022).
- Each release has a manifest (`release.json`) with the commit, the digests and the hash of the idempotent migration script. Documents keep their own Version History.

## 6. Deploy

- The workflow `NF Hotel CD` deploys automatically, but only when every check of the release commit is green (ADR-0021), and only after it has verified the attestations of every digest.
- It reaches the VM through **Tailscale**. The deploy command backs up the database, runs the migrations, starts the containers by digest, and runs a smoke test; a failed smoke test **rolls back** to the previous release.
- Secrets are files, never environment variables (ADR-0024); sign-ins and benchmark runs survive a deploy (ADR-0031).
- The public site goes through **Cloudflare Tunnel** (ADR-0028); the back office sits behind **Cloudflare Access** (ADR-0029).

## 7. Operate

- A **Proxmox** VM with Debian, hardened by the **CIS Benchmark** selection in `docs/sqa/cis-checklist.md`: an nftables firewall that drops by default, a hardened Docker daemon, containers with `no-new-privileges` and dropped capabilities.
- **Unattended security updates**, nightly **encrypted backups** (age) with a copy off the VM, and **restore drills** (MIL-021, MIL-023).
- An incident procedure with postmortems on [Incidents](Incidents), and a monthly **DORA** report (`docs/sqa/dora/`).
- A daily scan of the images that run in production, which opens an issue when a fixable finding appears (MIL-036).

## 8. Monitor

- **Prometheus**, **Grafana**, **Loki** and **Alloy** in their own LXC on the tailnet: metrics for 30 days, logs for 14 days (ADR-0020).
- **Alertmanager** sends the alerts to **Discord**; **UptimeRobot** checks the site from outside, and **Healthchecks.io** watches the monitoring itself.
- **Feedback into Plan:** a fault found in operation becomes a milestone. On 2026-10-26 the failed benchmark runs became MIL-045 and MIL-046.

## Where NF Hotel differs from the textbook

- **No Kubernetes or Terraform.** One VM with Docker Compose, automated by scripts and GitHub Actions, fits a project of this size.
- **No blue-green or canary deploys.** A smoke test with automatic rollback takes their place.
- **No required approving review in GitHub** (ADR-0019). Quality rests on the required checks and the review records.
- **Still open in MIL-023:** a second monthly restore drill and a port scan from outside the network.
