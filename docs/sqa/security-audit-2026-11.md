# SAR-001 Security audit of GitHub Actions and the CI/CD supply chain, November 2026

## Metadata
| Key | Value |
| --- | --- |
| ID | SAR-001 |
| CrossReference | [MIL-059], [ADR-0037], [DSA-001], [ADR-0007], [ADR-0019], [ADR-0021], [ADR-0032], [MIL-022] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Deprecated | Claude (AI) | AI as stand-in for S01 | Initial version: the audit of 2026-11-04 under MIL-059, which S01 accepted in chat ("Ja, inkl. indstillinger"); accepted after RC-280 Go | [742ce19] |
| 2026-11-04 | Accepted | Claude (AI) | AI as stand-in for S01 | Section 12 corrected: the new script was first linted by the pull request's Ops linters job, which found SC2016 in a self-test fixture, rewritten in the commit of this row (RC-280) | [8ef74c6] |

---

## 1. Scope and method

The audit covers the repository `NF-Hotel/NFHotel` at commit `b5cf8dc` (the head of `main` on 2026-11-04): the ten workflows of `.github/workflows/`, the four composite actions of `.github/actions/`, `.github/dependabot.yml`, the scripts of `ops/` that the workflows run, the git history, the workflow runs since 2026-10-01, and the settings of the repository and the organisation that the owner's `gh` token can read. The techniques looked for are those of the GhostAction campaign of 2026, as described by The New Stack: accounts taken over, then workflows added or changed to send secrets to the attacker, actions pointed at commits the attacker controls, and privileged workflows run on untrusted code.

Method: read-only, with `git`, `gh api` and `grep`; no run was started, nothing was deployed, no credential was read or printed. Every setting changed afterwards (section 11) was named in chat and agreed by S01 before the change, and is recorded with its value before and after. The report follows ADR-0032: a finding is shown where someone can act on it, with its reason.

## 2. Conclusion on compromise

**There is no evidence that the repository, its workflows or its releases were compromised.** Every change of `.github/` since 2026-09-15 came from a known team member or from Dependabot and reached `main` through a pull request; every action is pinned to the commit of the version in its comment; no workflow run came from a fork; no secret has left the repository as far as secret scanning, the workflow logs' design and the code show.

**What stays inconclusive**, because the token cannot read it: the organisation audit log (the API needs an Enterprise plan), the organisation's Actions policy (needs `admin:org`), and the repositories each GitHub App is installed on. An account-level compromise cannot be ruled out from the repository alone; section 9 gives the steps S01 takes in the GitHub interface, and SEC-01 gives the control that matters most.

## 3. Indicators investigated

| Indicator | What was checked | Result |
| --- | --- | --- |
| Unauthorized workflow files | `git log --diff-filter=A` on `.github/workflows` and `.github/actions`; the workflows GitHub has registered | Ten workflows and four actions, each added by a milestone commit (MIL-021 to MIL-036, ADR-0022); GitHub registers the same ten plus the four of Dependabot and CodeQL; nothing else |
| Suspicious workflow modifications | Every commit touching `.github/` since 2026-09-15: author, date, subject | 62 commits, all by mikemike/codemikemike (S01), Jens Tirsvad Nielsen or `dependabot[bot]`; each matches a milestone or a Dependabot bump; none in a pattern of a hidden change |
| Commits that bypassed review | `git log --first-parent --no-merges` on `main` since 2026-09-15 | None: every commit on `main` is a merge of a pull request |
| Unexpected `workflow_dispatch` runs | Every run of the event since the start | 16 runs, all by `codemikemike`: CD dry runs and drills (20 to 23 October), E2E and DAST, the load test and the running image scan (24 to 26 October) |
| Runs from forks or unknown actors | The 1000 most recent runs since 2026-10-01 by event, actor and head repository | Actors: the three team members with write access, `dependabot[bot]`, the CodeQL and code quality bots; every run from `NF-Hotel/NFHotel`; no fork |
| Secrets sent to an endpoint; `curl`, `wget`, PowerShell, base64 | `git grep` over `.github/`, `ops/scripts`, `ops/tests`, `scripts/`, `ops/deploy` | `curl` only for health checks of the stacks under test, the Docker and Tailscale apt keys in the VM and LXC setup scripts, and the smoke test of the deploy; `base64` only to encode the job token for the wiki push and to generate test keys; no download piped to a shell; no encoded command |
| Expression injection | Every `${{ }}` in a `run:` block of the workflows and actions | One, `${{ github.workspace }}` in `ci.yaml`, a runner value, not event data; replaced by `$GITHUB_WORKSPACE` (SEC-06). Event data reaches scripts through `env:` everywhere |
| Pinned actions pointed at other commits | Every `uses:` compared with the tag of its comment (`git/ref/tags`, dereferenced for annotated tags) and with the publisher's default branch | Nine external actions, nine matches; every commit is on the publisher's default branch (section 4) |
| Suspicious dependency updates | The three Dependabot action bumps of 21, 25 and 29 October; the NuGet lock files; `dotnet list package --vulnerable --include-transitive`; the Dependabot alerts | The bumps are the verified pins; no vulnerable NuGet package in 17 projects; no open Dependabot alert |
| Secrets in git | Secret scanning (provider patterns, with push protection) over the history; the tracked `.env.example` and `.env.dev` files | No alert; the tracked files hold placeholders and the development mode's `ASPNETCORE_ENVIRONMENT`, `CORS_ALLOWED_ORIGINS` and `COMPOSE_FILE` only; every real `.env` is git-ignored |
| Permissions changed | The top-level `permissions:` of every workflow and the job-level additions, in the files and in their history | `contents: read` at the top of all ten; write access only where documented (section 6) |
| Unexpected third-party actions | The inventory of section 4 | Two third-party publishers, both expected: OpenSSF (Scorecard) and Tailscale (the tailnet join) |

## 4. Third-party action inventory

Nine external actions and four local composite actions. Each external pin was checked on 2026-11-04 against the tag in its comment and against the publisher's default branch.

| Action | Publisher | Version | Commit of the tag | On the default branch | Used by |
| --- | --- | --- | --- | --- | --- |
| `actions/checkout` | GitHub | v7.0.1 | yes (`3d3c42e5`) | yes | every workflow (28 steps) |
| `actions/setup-dotnet` | GitHub | v6.0.0 | yes (`a98b5685`) | yes | ci, ops-ci, e2e-tests, release-images |
| `actions/upload-artifact` | GitHub | v7.0.2 | yes (`cf430e03`) | yes | ci, release, running-image-scan, load-test, scorecard, e2e-tests, dast-scan |
| `actions/download-artifact` | GitHub | v8.0.2 | yes (`9000827c`) | yes | deploy-release |
| `actions/attest-build-provenance` | GitHub | v4.2.2 | yes (`4d101475`) | yes | release-images |
| `actions/attest` | GitHub | v4.2.2 | yes (`1e69f48a`) | yes | release-images |
| `github/codeql-action/upload-sarif` | GitHub | v4.39.1 | yes (`9b325408`) | yes | scorecard, release-images, dast-scan |
| `ossf/scorecard-action` | OpenSSF (third party) | v2.5.0 | yes (`fa7117db`) | yes | scorecard |
| `tailscale/github-action` | Tailscale (third party) | v4.3.0 | yes (`99a83336`) | yes | deploy-release |
| `./.github/actions/release-images` | local | the checkout | n/a | n/a | release, release-check |
| `./.github/actions/deploy-release` | local | the checkout | n/a | n/a | cd |
| `./.github/actions/e2e-tests` | local | the checkout | n/a | n/a | e2e-dast |
| `./.github/actions/dast-scan` | local | the checkout | n/a | n/a | e2e-dast |

Every external pin follows the repository's policy (a full SHA with the version in a comment), Dependabot groups the updates of all four action folders with a seven-day cooldown, and no obsolete action remains. Since 2026-11-04 the repository setting requires the SHA and allows only GitHub-owned, verified and the two listed publishers (section 11), and the policy test refuses an unpinned action before a merge (ADR-0037).

## 5. Credential exposure assessment

Names and locations only; no value was read.

| Credential | Where | Assessment |
| --- | --- | --- |
| `SONAR_TOKEN` | Repository secret, updated 2026-10-27 | The only repository secret. Used by `ci.yaml` to send the analysis to SonarQube Cloud; absent on a pull request from a fork and in Dependabot runs, which skip the analysis. Until this audit it was in the environment of the whole build job, including the tests of the pull request (SEC-06); now only the two scanner steps receive it |
| `GITHUB_TOKEN` (the job token) | Every job; default permissions `read` for the repository | `contents: read` at the top of every workflow; write access only on the job that needs it (section 6); never on a command line; `docker login` reads it from standard input; the wiki push gives it to git through `GIT_CONFIG_*` environment variables. Four checkouts of `ci.yaml` kept it in `.git/config` during the build and the tests (SEC-05); now none does |
| Tailscale | Environment `production`: the variables `TS_OIDC_CLIENT_ID`, `TS_OIDC_AUDIENCE`, `NFHOTEL_VM_HOST`; no secret | The CD job joins the tailnet through GitHub OIDC (`id-token: write` on that job only), as an ephemeral node tagged `tag:ci`; no stored Tailscale key exists |
| SSH to the VM | None stored | Tailscale SSH as the user `deploy`, whose only `sudo` right is `/usr/local/sbin/nfhotel-deploy` (`install-deploy-user.sh`); the policy of the tailnet lets `tag:ci` reach port 22 of the VM only, and the step "Check the tailnet scope" records what the node reaches |
| GHCR | The job token, `packages: write` in the release job, `packages: read` in CD and the running image scan | Logged in from standard input and logged out when the release ends; the running image scan did not log out (SEC-10, fixed); the runner is thrown away after every job |
| Deploy keys, webhooks | No deploy key; one webhook, to Discord, for pull request events | A webhook address is a bearer token for its channel; it is readable by every administrator of the repository (SEC-02) and posts only to that channel |
| The VM's secrets | `/etc/nfhotel/secrets/`, root-only files, mounted as files into the containers (ADR-0024) | Not in the repository, not in the environment of a container (`check-secret-env.sh` runs after every deploy) |

No credential exposure was found, so no rotation is asked for. The logs of the workflows are designed not to carry a secret: the scripts never echo one, and the deploy log is the output of `nfhotel-deploy` on the VM.

## 6. CI/CD permission assessment

| Workflow | Triggers | Top-level permissions | Write access of a job | Checkouts keep credentials | Secrets |
| --- | --- | --- | --- | --- | --- |
| NF Hotel CI | `pull_request`, `push` to main, by hand | `contents: read` | none | before: yes (4 checkouts); after: no | `SONAR_TOKEN`, now on two steps |
| NF Hotel Release | `workflow_run` of CI, success, push to main of this repository | `contents: read` | `packages`, `id-token`, `attestations`, `security-events` (release job) | no | the job token |
| NF Hotel CD | `workflow_run` of Release, success, main of this repository; by hand with a run id (dry run unless `apply`) | `contents: read` | `id-token` (deploy job); reads of actions, attestations, packages, checks | no | none; the environment `production` holds variables only |
| NF Hotel Release check | `pull_request` | `contents: read` | none | no | none |
| NF Hotel E2E and DAST | `pull_request`, `push`, weekly, by hand | `contents: read` | `security-events` (baseline, full); `issues` (full) | no | none |
| NF Hotel Ops CI | `pull_request`, `push`, by hand | `contents: read` | none | no | none |
| NF Hotel Wiki | `push` to main of `docs/wiki`, by hand | `contents: read` | `contents: write` (the wiki push, required by GitHub) | no | the job token, through `env:` |
| NF Hotel running image scan | daily, by hand | `contents: read` | `issues: write`; reads of actions and packages | no | the job token |
| NF Hotel load test | weekly, by hand | `contents: read` | none | no | none |
| NF Hotel Scorecard | `push` to main, branch protection changes, weekly, by hand | `contents: read` | `security-events`, `id-token` (publishing the results) | no | none |

No workflow uses `pull_request_target`. Both `workflow_run` workflows check that the triggering run succeeded, came from a push (Release) or a `workflow_run` (CD) on `main`, and from this repository, before any job starts, and they check out the tested commit by SHA with `persist-credentials: false`. A pull request from a fork gets a read-only token and no secret; GitHub asks for an approval before a first-time contributor's run (SEC-13). The default token permission of the repository is `read`, and a workflow cannot approve a pull request. Event data reaches every `run:` script through `env:`, and the composite actions take their inputs through `env:` as well; `cd-steps.sh` and `deploy-release.sh` check each argument against a strict pattern before it reaches a shell.

## 7. Deployment security assessment

The path from a merge to the VM (ADR-0007, ADR-0021, MIL-022):

1. **Release** runs only after a green CI run of a push to `main` of this repository, builds the four images from the tested commit, scans each with Trivy (`aquasec/trivy` pinned by digest; a critical finding or a fixable high one stops the push), pushes them to GHCR, and attests the build provenance and the SBOM of every digest. The manifest `release.json` records the commit, the four digests and the SHA-256 of the migration script.
2. **CD** runs only for a successful Release of `main` of this repository, in the environment `production`, whose branch policy allows `main` only. Before any credential it checks the run (`check-run`), reads the manifest against strict patterns, verifies the provenance and the SBOM attestation of every digest with `gh attestation verify --signer-workflow .github/workflows/release.yaml`, and waits until every check of the release commit is green (`gate`): a red check, a cancelled one, or a commit without checks stops the deploy. Only then it joins the tailnet through OIDC, records what the node reaches (`scope`), and runs `nfhotel-deploy` on the VM.
3. **On the VM**, `nfhotel-deploy update` refuses a commit that is not reachable from `origin/main`, installs the commands from that commit with `git show`, keeps the replaced copy and logs both hashes; `migrate` applies a script only with the recorded SHA-256 and after a fresh backup; `deploy` checks every argument, takes a lock, backs up, checks out the commit, pulls the images by digest (never builds), runs `check-secret-env.sh`, and smoke-tests ports 80 and 443; on failure it redeploys the previous release from its log, and the log holds only releases that passed.
4. **Rollback** (`rollback`, or a manual CD run with an earlier run id) deploys digests that were verified when they were released, through the same checks; a release that never passed is not in the log.
5. **Logs**: the deploy log is the output of the VM command; no secret is printed; the summary holds the manifest, the checks and the tailnet scope.

A compromised CI job cannot deploy: it has no credential for the VM (OIDC is issued only to the CD job in `production`), its commit must be on `main` for `update` to install anything, and its images must carry attestations signed by the Release workflow of this repository. The gates are unchanged by this audit; `test-cd-steps.sh` (74 checks) and `test-deploy-release.sh` (59 checks) pass unchanged.

## 8. Dependencies and Dependabot

| Area | State on 2026-11-04 | Assessment |
| --- | --- | --- |
| NuGet | `packages.lock.json` per project, restored in locked mode in CI with the audit at level low and warnings as errors; `dotnet list package --vulnerable --include-transitive`: no vulnerable package in 17 projects; no open Dependabot alert | Good; Dependabot groups minor and patch updates weekly with a seven-day cooldown |
| Docker base images | Every `FROM` of the five Dockerfiles pinned by digest (.NET SDK 10.0.403, ASP.NET 10.0.14, Debian 12, Python 3.13 and 3.14) | Good; the backup-target Dockerfile was outside Dependabot's folders (SEC-04, fixed) |
| Compose images | Every third-party image pinned by digest; the application images come from GHCR by digest at deploy | Four folders with ten digest-pinned images (alloy, cloudflared, monitoring, postgres-tailnet) were outside Dependabot's folders (SEC-04, fixed); the PostgreSQL major-version ignore is kept |
| GitHub Actions | Section 4; Dependabot groups the four action folders weekly with a cooldown | Good |
| Build tools | `dotnet-sonarscanner` 11.3.0 and `dotnet-coverage` 18.12.0 installed from nuget.org at pinned versions; the linters of `lint.sh` as images at pinned tags (actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1); Trivy by digest | Acceptable: versions are pinned; NuGet packages carry the repository signature |
| Test containers | `debian:12-slim`, `debian:13-slim`, `postgres:16-alpine`, `curlimages/curl:8.10.1` by tag in `ops-ci.yaml` and the test scripts | SEC-11, accepted: official images, test only, no secret in the container, and a digest there would never be updated |
| Python of the AI services | `requirements.lock` with hashes, generated from the pinned upstream commits | Good; outside Dependabot by design (the lock follows `BENCHMARK_REF`) |

No unexpected publisher change was seen: every action comes from GitHub, OpenSSF or Tailscale, every image from Docker Hub official images, Microsoft, Grafana, Prometheus, Tailscale, Cloudflare or HAProxy as before.

## 9. Account and organisation security

What the owner's token could read on 2026-11-04:

| Setting | Value | Assessment |
| --- | --- | --- |
| Two-factor authentication required by the organisation | no; one of the five members has none | SEC-01, High |
| Members and their repository role | five members, all `admin` on NFHotel; no outside collaborator; one fork, by a member | SEC-02, Medium |
| Ruleset "Approve merges" on `main` | active; a pull request required, 0 approving reviews (ADR-0019), no bypass actor, deletions and force pushes blocked, 19 required checks | SEC-03, fixed: 41 required checks |
| Ruleset "protect main" | disabled, a duplicate | SEC-12, fixed: deleted |
| Classic branch protection | none (rulesets are used) | fine |
| Environment `production` | branch policy `main` only; no reviewer (ADR-0021); variables only, no secret | fine |
| Actions policy of the repository | all actions allowed; SHA pinning not required; default token `read`; workflows cannot approve pull requests; fork runs of first-time contributors need approval | SEC-07, fixed: selected actions and SHA pinning required; SEC-13, open |
| Secret scanning | enabled with push protection; non-provider patterns and validity checks disabled; no alert | SEC-08, open |
| Dependabot | security updates enabled; version updates configured; no alert | fine |
| GitHub Apps of the organisation | `chatgpt-codex-connector` (all repositories; `actions`, `contents`, `workflows`, `pull_requests`: write), `claude` (all repositories; also `repository_hooks`: write), `sonarqubecloud` (all repositories; `security_events`: write), `cloudflare-workers-and-pages` (selected repositories; `administration`, `contents`, `deployments`: write) | SEC-09, open |
| Deploy keys | none | fine |
| Webhooks | one, Discord, pull request events | fine; readable by every administrator |
| Organisation audit log | not readable (API: Enterprise only) | SEC-14, unverified |
| Organisation Actions policy, PAT policies | not readable (`admin:org` needed) | SEC-14, unverified |

**Manual verification of the account level** (S01, in the GitHub interface): Organization settings, Audit log: filter `action:workflows` and `action:repo.actions_enabled`, `action:org.enable_oauth_app_restrictions`, `action:integration_installation`, `action:org.add_member`, `action:repo.add_member`, `action:protected_branch` for October and November 2026; the Security tab of the organisation, Authentication security, for the two-factor state of each member; each member's own Settings, Sessions and Applications, for sessions and authorized OAuth apps they do not recognise; Settings, Developer settings, for personal access tokens with `repo` or `workflow` scope that are no longer needed. No team member's account is claimed to be compromised; the audit cannot tell, and these steps are how S01 can.

## 10. Findings

Severity: Critical, High, Medium, Low, Informational. Status: Fixed (in this milestone, with the commit or the setting), Open (an action of S01 outside the repository), Accepted (left as it is, with the reason), Unverified (could not be read).

| Finding | Severity | Evidence | Remediation | Status |
| --- | --- | --- | --- | --- |
| SEC-01 The organisation does not require two-factor authentication, and the member `Outlashed` has none (`two_factor_requirement_enabled: false`; `members?filter=2fa_disabled`) | High | An account takeover was the first step of the GhostAction campaign; a member with write access and no second factor is the easiest door; all five members are administrators (SEC-02) | S01: ask the member to enable two-factor authentication, then Organization settings, Authentication security, "Require two-factor authentication for everyone"; GitHub removes a member who has none at that moment, so the order matters | Open |
| SEC-02 Every member is an administrator of the repository, and no approving review is required (ADR-0019) | Medium | `collaborators?affiliation=direct`: five `admin`; the ruleset requires 0 reviews; CODEOWNERS is advisory. One taken-over account can change a workflow, merge it after the checks (which run its own code), and change every setting | S01: give `write` to the team and keep `admin` for the owner (Settings, Collaborators and teams); then, if wanted, a push ruleset that restricts changes of `.github/workflows/` to administrators. ADR-0019 stays S01's decision; the policy test (ADR-0037) and the settings of SEC-07 narrow what a merged workflow can do | Open |
| SEC-03 The ruleset required 19 of the 47 checks of a pull request: not CodeQL, SonarQube Cloud, the host hardening, the monitoring configuration, the secret rotation drill, the wiki coverage, the comment language, the team database roles, the benchmark volume, the two development modes, the F5 test, and ten script tests | Medium | `rulesets/21918874`, `required_status_checks` compared with the check runs of the head of pull request 470; a change with a red security test could merge (the CD gate would still refuse to deploy it) | Applied 2026-11-04 10:45 UTC with S01's go: 41 required checks, every check that runs on a pull request, plus `CodeQL` and `SonarCloud Code Analysis`; left out on purpose: the weekly `DAST full scan` (skipped on a pull request), the four `Analyze (...)` runs (two share a name) and `OWASP ZAP` (a neutral upload) | Fixed |
| SEC-04 Dependabot did not cover `ops/deploy/alloy`, `ops/deploy/cloudflared`, `ops/deploy/monitoring`, `ops/deploy/postgres-tailnet` (compose) and `ops/deploy/backup-target` (Dockerfile): eleven images pinned by digest without an update proposal | Medium | `dependabot.yml` folders against `git grep sha256: -- 'ops/deploy/*/docker-compose*.yml'` and the Dockerfiles; a security fix of Grafana, Loki, Prometheus, Alertmanager, blackbox, Alloy, cloudflared, HAProxy or Debian would not be proposed | The five folders added to `dependabot.yml` (commit `fd4a09a`); the PostgreSQL major-version ignore kept | Fixed |
| SEC-05 The four checkouts of `ci.yaml` kept the job token in `.git/config` while the build and the tests of a pull request ran (`persist-credentials` default) | Low | Every other workflow sets `persist-credentials: false`; a test or a package of a pull request from a member could read a `contents: read` token of this repository | `persist-credentials: false` on the four checkouts (commit `ab756e9`); rule 3 of the policy test | Fixed |
| SEC-06 `SONAR_TOKEN` was in the environment of the whole build job, including the restore, the build, the tests and the coverage steps | Low | `ci.yaml` job `env:`; a step that does not need the token could read it; one `${{ github.workspace }}` in a `run:` script | The job holds only whether the token exists (`secrets.SONAR_TOKEN != ''`); the two scanner steps receive it; `$GITHUB_WORKSPACE` in the script (commit `ab756e9`); rules 5 and 6 of the policy test | Fixed |
| SEC-07 The repository allowed every action and did not require SHA pinning as a setting (`allowed_actions: all`, `sha_pinning_required: false`) | Low | The workflows pin every action, but nothing refused an unpinned or foreign action in a pull request before the Scorecard ran after the merge | Applied 2026-11-04 with S01's go: `allowed_actions: selected` with GitHub-owned, verified, `ossf/scorecard-action@*` and `tailscale/github-action@*`; `sha_pinning_required: true`; rule 1 of the policy test | Fixed |
| SEC-08 Secret scanning runs without non-provider patterns and without validity checks | Low | `security_and_analysis` of the repository; a generic secret (a private key, a high-entropy string) is not reported, and a found provider secret is not checked for validity | S01: Settings, Code security, Secret scanning: enable "Non-provider patterns" and "Validity checks" (the API PATCH of 2026-11-04 answered 200 and 502 and changed nothing, so the setting is made in the interface) | Open |
| SEC-09 GitHub Apps with `workflows: write` and `contents: write` are installed on every repository of the organisation (`chatgpt-codex-connector`, `claude`), and one with `administration: write` on selected repositories (`cloudflare-workers-and-pages`) | Low | `orgs/NF-Hotel/installations`; an app token with `workflows: write` can change a workflow without a person's account; the apps are the team's own tools, installed by S01 on 2026-09-29 and 2026-10-25 | S01: Organization settings, GitHub Apps: limit each app to the repositories it serves, and review whether `workflows: write` and `repository_hooks: write` are needed; keep the installation list under review | Open |
| SEC-10 The running image scan logged in to GHCR and did not log out | Low | `running-image-scan.yaml` against `release.yaml`, which logs out; the runner is thrown away, so the window is the job | A `docker logout ghcr.io` step with `if: always()` (commit `ab756e9`) | Fixed |
| SEC-11 The test containers of Ops CI and of the test scripts use official images by mutable tag (`debian:12-slim` 18 times, `debian:13-slim`, `postgres:16-alpine`, `curlimages/curl:8.10.1`) | Low | `git grep` over `ops/tests` and `ops-ci.yaml`; a moved tag of an official image would change what the tests run in | Accepted: the containers run tests only, hold no secret and reach no production system; a digest in a `docker run` line is not updated by Dependabot and would go stale, which the project's own practice (every production image by digest with Dependabot) avoids on purpose | Accepted |
| SEC-12 A disabled ruleset "protect main" duplicated the active one | Low | `rulesets`: `protect main`, `disabled`; a second ruleset invites confusion about which one holds | Deleted 2026-11-04 with S01's go; its JSON is kept with the audit records | Fixed |
| SEC-13 Workflow runs of pull requests from forks need approval only for first-time contributors (`approval_policy: first_time_contributors`) | Informational | The default; a fork gets a read-only token and no secret, so the exposure is the runner's time and the public logs | S01, optional: Settings, Actions, "Require approval for all external contributors" | Open |
| SEC-14 The organisation audit log, the organisation's Actions policy and the repositories of each app could not be read | Informational | HTTP 404 (audit log: Enterprise only) and 403 (`admin:org` needed) | S01: the manual verification of section 9 | Unverified |

Counts: 7 Fixed (SEC-03, 04, 05, 06, 07, 10, 12), 5 Open (SEC-01, 02, 08, 09, 13), 1 Accepted (SEC-11), 1 Unverified (SEC-14). No finding is Critical.

## 11. Implemented improvements

| Change | Finding | Where | Regression test |
| --- | --- | --- | --- |
| `persist-credentials: false` on the four checkouts of CI | SEC-05 | `ci.yaml`, commit `ab756e9` | `test-workflow-policy.sh` rule 3 |
| `SONAR_TOKEN` only on the two scanner steps; `$GITHUB_WORKSPACE` in the script | SEC-06 | `ci.yaml`, commit `ab756e9` | rules 5 and 6; the SonarQube Cloud check of the pull request shows the analysis still runs |
| Log out of GHCR when the running image scan ends | SEC-10 | `running-image-scan.yaml`, commit `ab756e9` | `lint.sh` (actionlint) |
| Five folders added to Dependabot | SEC-04 | `dependabot.yml`, commit `fd4a09a` | `lint.sh` (yamllint); the Dependabot run after the merge |
| The workflow policy test with its self-test, run by the job Ops linters | SEC-05, 06, 07 and the campaign's techniques | `ops/tests/test-workflow-policy.sh`, `ops-ci.yaml`, commit `bb3a4b4`; ADR-0037 | the self-test: seven fixtures, each fails with its rule; the repository: 14 files pass |
| The wiki pages Security-Controls and Workflows | documentation | `docs/wiki/` | `test-wiki-coverage.sh` |

Settings applied on 2026-11-04 at 10:45 UTC through the API, each named in chat and agreed by S01 ("Ja, inkl. indstillinger"); the JSON before and after, and the deleted ruleset, are kept in the prompt log of the session (`docs/ai-prompts/session-002/mil-059-settings/`, outside the public repository):

| Setting | Before | After |
| --- | --- | --- |
| Actions allowed (`actions/permissions`) | `allowed_actions: all`, `sha_pinning_required: false` | `allowed_actions: selected`: GitHub-owned, verified, `ossf/scorecard-action@*`, `tailscale/github-action@*`; `sha_pinning_required: true` |
| Ruleset "Approve merges", required checks | 19 | 41: every check that runs on a pull request, `CodeQL` and `SonarCloud Code Analysis` |
| Ruleset "protect main" | disabled | deleted |
| Secret scanning non-provider patterns, validity checks | disabled | unchanged: the API did not apply it (SEC-08, for S01 in the interface) |

## 12. Test results

Run on 2026-11-04 on S01's PC (Git Bash for the policy test, WSL with Docker for the containers):

- `bash ops/tests/test-workflow-policy.sh --self-test`: 8 passed, 0 failed (the good fixture passes; each of the seven fixtures fails with its rule).
- `bash ops/tests/test-workflow-policy.sh`: the 14 workflows and actions of `.github` follow the policy.
- `bash ops/tests/lint.sh`: layout, actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1 and yamllint 1.35.1 clean on the changed workflows. The local run reads the tracked files, so the new script was first linted by the job Ops linters of the pull request, which found SC2016 (info) in a self-test fixture; the fixture was rewritten, and shellcheck and shfmt are clean on the file (RC-280).
- `test-cd-steps.sh`: 74 passed, 0 failed; `test-deploy-release.sh`: 59 passed, 0 failed; both unchanged by this milestone.
- `bash ops/tests/test-wiki-coverage.sh`: 91 passed.
- The checks of the pull request of MIL-059, with the scanners still operational, are read after the push and recorded on the pull request.

## 13. Remaining risks

- A taken-over account of a member with write access can still merge a change alone (ADR-0019); the policy test and the Actions settings limit what such a change can do with a token, and SEC-01 and SEC-02 are the controls that address the account itself.
- A compromised commit that Dependabot proposes and that is merged within the cooldown would be pinned by its SHA like any other (MIL-022's known risk).
- The organisation level is verified by S01, not by this audit (SEC-14).
- The policy test reads the layout of this repository; a workflow with another layout needs it extended (ADR-0037).

## 14. Manual actions required from the administrators

1. SEC-01: have the member without two-factor authentication enable it, then require it for the organisation (Organization settings, Authentication security).
2. SEC-02: set the roles of the members to `write` and keep `admin` for the owner (Settings, Collaborators and teams); decide whether a push ruleset on `.github/workflows/` for administrators only is wanted.
3. SEC-08: enable "Non-provider patterns" and "Validity checks" of secret scanning (Settings, Code security).
4. SEC-09: limit the four GitHub Apps to the repositories they serve and review their `workflows: write` and `repository_hooks: write` permissions (Organization settings, GitHub Apps, Installed GitHub Apps).
5. SEC-13, optional: require approval for all external contributors' workflow runs (Settings, Actions, General).
6. SEC-14: read the organisation audit log for October and November 2026 with the filters of section 9, and the sessions and authorized applications of each member.
7. Keep the required checks of the ruleset in step with the jobs: a renamed or added job is added to the ruleset, or its red result does not block a merge (the policy test does not check the ruleset).

---

[MIL-059]: ../milestones/mil-059-github-actions-security-audit.md
[ADR-0037]: ../adr/adr-0037-github-actions-hardening-policy.md
[DSA-001]: ./devsecops-assessment.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0019]: ../adr/adr-0019-approvals-by-the-owner.md
[ADR-0021]: ../adr/adr-0021-automatic-deploy-when-green.md
[ADR-0032]: ../adr/adr-0032-what-code-scanning-shows.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[742ce19]: https://github.com/NF-Hotel/NFHotel/commit/742ce193969dda9c8556362bd0c65c2d49f60c14
[8ef74c6]: https://github.com/NF-Hotel/NFHotel/commit/8ef74c665468a003562abb4e37018f36c3c99446
