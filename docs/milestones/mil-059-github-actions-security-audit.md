# MIL-059 Security audit of GitHub Actions and the CI/CD supply chain

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-059 |
| CrossReference | [BC-001], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032], [MIL-022], [MIL-044] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-11-04, in autonomous mode, for a security audit of the GitHub Actions workflows and the CI/CD supply chain against the techniques of the GhostAction campaign, with documentation of the findings, the fixes and the changes; S01 accepted the plan in chat the same day ("Ja, inkl. indstillinger") after RC-279 Go | pending |

---

## Purpose

Decide whether the GitHub Actions workflows, the composite actions, the dependencies and the deployment path of NFHotel show the techniques of the GhostAction campaign of 2026 (compromised accounts that add or change workflows, exfiltrate secrets through `curl` and encoded commands, point actions at malicious commits, or run privileged workflows on untrusted code), and whether the weaknesses the investigation found are closed without weakening a control or changing how the pipeline works.

The read-only investigation of 2026-11-04 (the repository, its history, the workflow runs since 2026-10-01 and the settings the owner's token can read) found **no evidence of a compromise**:

- every commit that changed `.github/` since 2026-09-15 is by a known team member or Dependabot and reached `main` through a pull request; no commit was pushed to `main` directly; the ten workflow files and the four composite actions were each added by a milestone commit;
- the nine external actions are pinned to full commit SHAs, each the commit of the tag in its comment and on the publisher's default branch; the three Dependabot bumps of October are those pins;
- no workflow run since 2026-10-01 came from a fork, and the 16 manual runs are the owner's (CD dry runs, drills, scans); no unknown workflow is registered;
- no `curl | sh`, encoded command or outbound request to an unexpected host is in the workflows, the actions or the scripts they run; event data reaches a `run:` script only through `env:`;
- the only repository secret is `SONAR_TOKEN`; the environment `production` holds no secret, only the Tailscale OIDC identifiers; there is no deploy key; secret scanning with push protection is on and has no alert; Dependabot and the NuGet audit have no open finding;
- the deploy path verifies the build provenance and the SBOM of every digest with `gh attestation verify` before any credential, waits until every check of the release commit is green, joins the tailnet through OIDC as an ephemeral node, and runs one command on the VM as the user `deploy`, whose only `sudo` right is `nfhotel-deploy`, which installs commands only from a commit on `main`.

What the investigation cannot see, and the owner verifies by hand: the organisation audit log (the API needs an Enterprise plan), the organisation's Actions policy (needs an owner token with `admin:org`), and the repositories each GitHub App is installed on.

The weaknesses found, by severity: **High** SEC-01, the organisation does not require two-factor authentication and one member has none (the first step of the campaign was an account takeover). **Medium** SEC-02, every member is an administrator of the repository and no review is required (ADR-0019), so one account merges a workflow change alone; SEC-03, the ruleset requires 19 of the 48 checks of a pull request, and not CodeQL, SonarQube Cloud, the host hardening, the secret rotation drill or ten script tests; SEC-04, Dependabot does not cover five folders whose images are pinned by digest, so those pins never move. **Low** SEC-05, the CI checkouts keep the job token in `.git/config` while the tests of the pull request run; SEC-06, `SONAR_TOKEN` is in the environment of the whole build job; SEC-07, the repository allows every action and does not require SHA pinning as a setting; SEC-08, secret scanning runs without non-provider patterns and validity checks; SEC-09, GitHub Apps with `workflows: write` and `administration: write` are installed on every repository of the organisation; SEC-10, the running image scan logs in to GHCR without logging out; SEC-11, the test containers of Ops CI run official images by mutable tag; SEC-12, a disabled duplicate ruleset lingers. **Informational** SEC-13, pull requests from forks need approval only for first-time contributors; SEC-14, the audit log and the organisation policy could not be read.

## Deliverable

- **The security audit report** `docs/sqa/security-audit-2026-11.md` (type SAR, registered as DSA and CIS are): the indicators investigated with their evidence, the inventory of the thirteen actions (nine external, verified pin by pin, and four local), the credential assessment (names and locations only), the permission assessment of every workflow and job, the deployment assessment, every finding with severity, evidence, remediation and status, the implemented improvements with their tests, the test results, the remaining risks, the manual actions of the administrators, and the conclusion on compromise.
- **ADR-0037**, the hardening policy of the workflows: actions pinned to full commit SHAs, `contents: read` at the top of every workflow and write access only on the job that needs it, `persist-credentials: false` on every checkout, no `pull_request_target`, secrets only on the step that uses them, event data only through `env:`, no piped download; enforced by the policy test, with the settings that back it on the host.
- **The fixes**: `ci.yaml` (persist-credentials: false on its three checkouts; `SONAR_TOKEN` on the three scanner steps only; `$GITHUB_WORKSPACE` instead of an expression in a `run:`), `running-image-scan.yaml` (log out of GHCR), `dependabot.yml` (the compose folders alloy, cloudflared, monitoring and postgres-tailnet, and the Dockerfile of backup-target).
- **The regression test** `ops/tests/test-workflow-policy.sh`, run by the job Ops linters (a required check): every rule of ADR-0037 on `.github/`, and a self-test with fixtures that fail each rule.
- **The settings**, each applied only with S01's explicit go in chat and otherwise listed as a manual action with its command: require SHA pinning for actions; allow only GitHub-owned, verified and the two listed publishers; add the missing checks to the ruleset "Approve merges"; enable the non-provider patterns and the validity checks of secret scanning; delete the disabled ruleset "protect main". Left to S01 in the GitHub interface, because they change people's access: the two-factor requirement of the organisation, the roles of the members, the repositories and permissions of the four GitHub Apps, and the reading of the audit log.
- **The documentation**: the wiki page Security-Controls (the pinning, the policy test, the ruleset and the account controls), RC-279 and RC-280, the traceability matrix; the pull request open for S01's review.

Not in this milestone: a rotation of a credential (none is exposed), a change on the VM, a change of a security scanner, and a change of the release gates of ADR-0021.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The report holds every finding SEC-01 to SEC-14 with severity, evidence, remediation and status (Fixed, Open, Accepted or Unverified), the inventory of the thirteen actions with the verification of each pin, the conclusion on compromise with what stays inconclusive, and the manual actions with their commands | Yes | No |
| 2 | `bash ops/tests/lint.sh` and `bash ops/tests/test-workflow-policy.sh` pass locally and in the job Ops linters; the self-test of the policy test fails on each of its rules; the pull request's checks are green, with CodeQL (security and quality), SonarQube Cloud, OWASP ZAP, Trivy in the release build and the Scorecard workflow still running | Yes | No |
| 3 | No control is weakened: no scanner or workflow is disabled, no secret is printed, the gates of CD are unchanged (`test-cd-steps.sh` and `test-deploy-release.sh` pass unchanged), and the SonarQube Cloud analysis still runs on a pull request of this repository | Yes | No |
| 4 | Every setting changed on GitHub was named in chat and agreed by S01 before the change, and the report records it with the value before and after; the settings left to S01 are listed with the exact step | Yes | No |
| 5 | The reviews of MIL-059 (QC-MIL-001), of ADR-0037 (QC-ADR-001) and of the code and the report (QC-WF-001, QC-YML-001, QC-SH-001) are Go; the pull request is open for S01, and the AI merges it only on S01's word in chat | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-022 | The supply chain controls under audit: pins, Dependabot, the Trivy gate, the attestations and their verification |
| MIL-044 | ADR-0032: findings are shown where someone can act on them; the report follows it for the findings of this audit |
| ADR-0021 | The release gate (every check green, no person approves) that the audit must leave intact |
| ADR-0019 | The owner merges alone; SEC-02 weighs that decision against the campaign and leaves it to S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 23: a change reaches the server only after the required checks pass, built once in CI and deployed by digest over a private connection | Tasks 1, 2, 4, 7 |
| BC-001 objective 24: scanned and attested images, every dependency pinned | Tasks 1, 3, 4 |
| DSA-001 risks R2 (a fork steals deploy credentials), R3 (a compromised third-party action), R5 (a tampered image), R8 (the deploy identity abused) and R9 (lateral movement from the CI node) | Tasks 1, 8 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-11. The work changes two workflows, the Dependabot configuration, one test script and the documentation; the settings are S01's decision; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Record the investigation and its evidence | Write the report `docs/sqa/security-audit-2026-11.md` (type SAR, registered) from the read-only investigation of 2026-11-04: the indicators checked (history of `.github/`, direct pushes, fork runs, manual runs, registered workflows, downloads and encoded commands, expression injection), the inventory of the nine external and four local actions with each pin verified against its tag and the publisher's default branch, the credential assessment by name and location, the permissions of every workflow and job, the deployment path from the release to the VM, and the findings SEC-01 to SEC-14 with severity and evidence; the conclusion: no evidence of a compromise, inconclusive for the audit log and the organisation policy. | No | criterion 1 |
| 2 | Harden the CI and the running image scan | `ci.yaml`: `persist-credentials: false` on the three checkouts (SEC-05), `SONAR_TOKEN` on the three scanner steps instead of the job (SEC-06), `$GITHUB_WORKSPACE` in place of the expression in the scanner's `run:`; `running-image-scan.yaml`: log out of GHCR when the job ends (SEC-10). The SonarQube Cloud analysis must still run on a pull request of this repository and skip on one from a fork. | No | criteria 2, 3 |
| 3 | Complete the Dependabot coverage | `dependabot.yml`: the docker-compose folders `ops/deploy/alloy`, `ops/deploy/cloudflared`, `ops/deploy/monitoring` and `ops/deploy/postgres-tailnet`, and the Dockerfile folder `ops/deploy/backup-target`, whose eleven images are pinned by digest and got no update proposal (SEC-04); the PostgreSQL major-version ignore stays. | No | criterion 1 |
| 4 | Add the workflow policy test | `ops/tests/test-workflow-policy.sh`, run by the job Ops linters after `lint.sh`: every `uses:` of an external action pinned to a full commit SHA with the version in a comment; `permissions: contents: read` at the top of every workflow and write access only in a job; `persist-credentials: false` on every checkout; no `pull_request_target`; no `secrets.` above a step; no `${{ }}` inside a `run:`; no download piped to a shell. A self-test with fixtures that break each rule proves the test, as `test-check-comment-language.sh` does for its check (QC-WF-001 criterion 24). | No | criteria 2, 3 |
| 5 | Write ADR-0037, the hardening policy of the workflows | The rules of task 4 and the settings of task 7 as one decision, with the GhostAction techniques as the context, the options considered (a policy test in the repository, the host's settings alone, a third-party scanner such as zizmor in CI) and the consequences; QC-ADR-001. | No | criterion 5 |
| 6 | Validate locally and in CI | `bash ops/tests/lint.sh` (actionlint, shellcheck, shfmt, yamllint), the policy test and its self-test, `test-cd-steps.sh` and `test-deploy-release.sh` unchanged and green, `test-wiki-coverage.sh`; after the push, every check of the pull request, and the scanners still operational: CodeQL (both suites), SonarQube Cloud, OWASP ZAP, Trivy in the release build, Scorecard; nothing is deployed and no destructive test runs. | No | criteria 2, 3 |
| 7 | Apply the agreed settings, list the manual actions | With S01's explicit go in chat, through the API, each with its value before and after: require SHA pinning of actions and allow only GitHub-owned, verified and the two listed publishers (SEC-07); add the missing checks to the ruleset "Approve merges" (SEC-03); enable the non-provider patterns and the validity checks of secret scanning (SEC-08); delete the disabled ruleset "protect main" (SEC-12). Listed for S01 with the exact step, never applied by the AI: the two-factor requirement of the organisation after the member without it enables it (SEC-01), the roles of the members (SEC-02), the repositories and permissions of the four GitHub Apps (SEC-09), the fork approval policy (SEC-13), and the reading of the audit log for October and November (SEC-14). | No | criteria 1, 4 |
| 8 | Report, reviews, wiki and pull request | The report completed with the implemented improvements, the test results, the remaining risks and the manual actions; the wiki page Security-Controls (the pinning and policy test rows, the ruleset row, the account controls); RC-279 for MIL-059 and ADR-0037, RC-280 for the code and the report; the traceability matrix; the pull request with the summary and one `Closes #N` per task, open for S01's review. | No | criteria 1, 5 |

---

[BC-001]: ../business-case.md
[DSA-001]: ../sqa/devsecops-assessment.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0019]: ../adr/adr-0019-approvals-by-the-owner.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-044]: ./mil-044-security-overview.md
