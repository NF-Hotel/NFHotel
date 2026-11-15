# ADR-0046 The tunnel routes and Access as code in OpenTofu

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0046 |
| CrossReference | [MIL-078], [ADR-0027], [ADR-0028], [ADR-0029], [ADR-0037], [ADR-0021], [MIL-072], [RC-338], [RC-339] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Initial version; task 1 of MIL-078, whose plan S01 accepted in chat on 2026-11-12 (RC-338 Go); written by the AI | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decides.** S01 asked on 2026-11-12 for the parts that are missing to be planned where they make sense, and
accepted [MIL-078] that day ([RC-338], Go). This record is task 1 of [MIL-078]; its design was written by the
artificial intelligence (AI) assistant. S01 owns the Cloudflare account; S01 and S03 are the people who may change it.

**The problem.** The public edge of NF Hotel lives only in the Cloudflare dashboard: the routes of the tunnel
`nf-hotel` and their order ([ADR-0028]), and the Access application `NF Hotel back office` with its policy `Team`
([ADR-0029]). They were set by hand from the steps of the wiki page Public-Access, and nothing compares the live
setting with those steps. [MIL-072] found the route that checks the Access token second in the list, where
`cloudflared` never used it: Access at the edge still stood in front of `/admin`, but the check at the tunnel was not
in effect until a review found it ([RC-339]). [ADR-0027] planned two other settings of a console in git, and S01
deferred both; the edge is the part that has drifted, and its drift was a security finding.

**Forces.**

- The repository is public, and so are the logs of its workflows. The identifiers of the account, the tunnel, the
  application and the policy, and the e-mail addresses of the staff in the policy, must be neither in git nor in a log.
- A Cloudflare token that can edit the tunnel or Access can open the back office to anyone. Such a token must never be
  in continuous integration (CI); a comparison needs only a token that reads.
- [ADR-0037] pins every action by commit, keeps the token read-only at the top and gives a secret to the step that
  needs it only. A new publisher of actions is one more party to trust.
- A tool that keeps a state file needs a place for it. The state of the Cloudflare provider would hold the identifiers
  and the addresses in clear text, and the project has no store for it: git is public, and there is no bucket.
- A scheduled run is a check of the head of `main`, and the deploy gate of [ADR-0021] refuses a commit with a red
  check, so a finding must not turn the run red.
- The dashboard must stay usable at once in an emergency: to take the site off the internet, or to take a member of
  staff off the list.

**Options considered.**

1. **Keep the dashboard,** set by hand from the wiki. Nothing to build; a change leaves no trace in git, has no
   review, and is found only by a person who looks.
2. **A tunnel managed by a local file.** `cloudflared` reads its routes from a `config.yml` in git, deployed with the
   release. That brings the routes into git, but not the Access application and its policy; it needs a tunnel run
   with a credentials file instead of the token ([ADR-0028] decision 5), and the live routes become whatever the
   virtual machine (VM) last deployed, with nothing that compares them with the account.
3. **OpenTofu with the Cloudflare provider.** The routes, the application and the policy are declared in HashiCorp
   Configuration Language (HCL); a plan compares them with the live account, and an apply changes the account to
   match. OpenTofu is the fork of Terraform under the Linux Foundation, under the Mozilla Public License 2.0.
   Terraform, under the Business Source License since August 2023, would do the same under a license that limits
   competing uses; Pulumi needs a general-purpose language and a state backend of its own; a script against the
   application programming interface (API) of Cloudflare would have to build the plan and the comparison itself.

## Decision

The public edge is code in `ops/infra/cloudflare/`, in OpenTofu with the Cloudflare provider (option 3) and without a
stored state; a weekly plan with a token that reads compares it with the live account, and a change is a pull
request that S01 or S03 applies from their own machine.

1. **What is in code.** The routes of the tunnel `nf-hotel` in their order: first `nfhotel.devmatiker.com` with the
   path `(?i)^/admin` and the Access check on, with the team name and the Application Audience (AUD) tag of the
   application; then the same name without a path; then the rule that answers 404. Both routes go to the public
   listener `http://nginx:8082`. The Access application `NF Hotel back office` for `nfhotel.devmatiker.com/admin`,
   self-hosted, with a session of one week (`168h`, [ADR-0029] decision 1), and its policy `Team`, which allows the
   listed addresses. That is the configuration that `cloudflared` logged as its version 4 on 2026-11-11 ([RC-339]).
   The tunnel itself and its token, the zone settings (the SSL mode, "Always Use HTTPS", the rate-limiting rule) and
   the Domain Name System (DNS) records stay in the dashboard; a route for a new name would need its DNS record made
   there too.
2. **The tool and its pins.** OpenTofu 1.13.9 runs from its official image `ghcr.io/opentofu/opentofu`, the minimal
   variant, pinned by the digest of its index for every platform in `ops/scripts/edge-tofu.sh`; no action is used, so
   no new publisher of actions is trusted. The minimal image is the `tofu` binary on an empty base. The script lends
   it the certificate authorities of the host, read-only, and a `/tmp` in memory, and runs it as the calling user,
   without capabilities, with a read-only root and the code read-only (writable only to format it or to write the lock
   file). *Why the minimal image:* since OpenTofu 1.10 the project no longer supports the full image for direct use,
   and its base, Alpine 3.20, is past its end of support (2026-04-01); the project now asks users to build their own
   image from the minimal one, which for one static binary would add a base image to patch and nothing else. The
   Cloudflare provider is pinned to 5.29.0 in `versions.tf` and by its checksums in `.terraform.lock.hcl`; every
   `tofu init` but the one that writes the lock file (`edge-tofu.sh lock`) reads it read-only, so a provider that does
   not match is refused. *Why 5.29.0, released on 2026-11-11 and so inside the week of cooldown that Dependabot
   keeps:* it ends a drift of an empty `origin_request` to null in the configuration of a tunnel, which 5.28.1 would
   show as a change in every weekly plan, and it fixes the import and the refresh of Access applications and policies.
3. **No stored state.** There is no backend. Every plan imports the three objects, through one `import` block each
   with the identifiers from variables, and the state of a plan or an apply lives in a file system in memory inside
   its container, gone when it ends. No state file with the identifiers and the addresses exists anywhere, and the
   plan of an account that matches the code shows 3 to import and 0 to add, change or destroy. So an object must
   exist before it is in the code: a new object is made in the dashboard, then written as code with its import block,
   and the plan must then show 0 changes; an object taken out of the code is not deleted by `tofu`, but by hand in the
   dashboard. Encrypted remote state, which OpenTofu offers, is the fallback if this does not work; it would need a
   store and a key, and an ADR of its own.
4. **The identifiers and the addresses stay outside git.** The identifiers of the account, the tunnel, the application
   and the policy, the team name and the addresses of the policy are variables (`variables.tf`). The addresses are
   marked sensitive, so `tofu` never shows them. The identifiers cannot be, because an import block refuses a
   sensitive identifier, so the log is kept clean of them as decision 6 says. No zone identifier is needed: the three
   objects belong to the account. The AUD tag is no variable: the route reads it from the application, so the two
   cannot disagree. On a person's machine the values are in `ops/infra/cloudflare/edge.env`, which git ignores; in
   GitHub they are secrets of the environment `cloudflare-read`.
5. **Two tokens.** The token that reads (for this account only: Cloudflare Tunnel, Read, and Access: Apps and
   Policies, Read) is the secret `CLOUDFLARE_READ_TOKEN` of the environment `cloudflare-read`, which only `main` may
   use; S01 creates both. The token that writes (the same two permissions as Edit) is created by S01 or S03 for their
   own machine, typed at the prompt of `edge-tofu.sh` without echo, and never written to a file of the repository, to
   CI or to a chat. The AI assistant never sees either token.
6. **The weekly comparison.** `edge-comparison.yaml` runs every Monday at 04:23 UTC and by hand. It runs the plan in
   the pinned image with the token that reads, which only the step of the plan gets, and reduces the JSON output of
   `tofu` to the counts and to the address and action of each object that differs; the plan itself is never printed
   or kept. Every identifier is a secret, which GitHub masks in the log, each address is masked on its own, and the
   messages of a failed plan are shown with identifiers and addresses replaced. A difference opens the issue "The
   public edge differs from its code", or comments on the open one, and a clean plan closes it, as the daily scan of
   the running images does. A difference leaves the run green, because a red check on the head of `main` would block
   its deploy ([ADR-0021]); a plan that fails, fails the run. While a secret of the environment is missing, the run
   names it in a warning, compares nothing and stays green. The job has `contents: read` and `issues: write`, and the
   workflow `contents: read` ([ADR-0037]).
7. **Changing the edge.** A change is a pull request to `ops/infra/cloudflare/`, reviewed like any other. After the
   merge, S01 or S03 runs `bash ops/scripts/edge-tofu.sh apply` on `main` from their own machine with the token that
   writes, reads the plan and answers `yes`; nothing applies by itself. A change made in the dashboard, in an
   emergency, is reported by each weekly run until the code or the dashboard is brought in line. When the first plan
   shows a difference, the code is corrected until the plan shows 0 changes; nothing is applied to make it clean
   ([RC-338]).
8. **The checks.** Ops CI runs `tofu fmt -check` and `tofu validate`, the provider checked against the lock file, in
   the pinned image without a token (the job `Ops edge code (MIL-078)`), and `test-edge-comparison.sh` tests both
   scripts with stand-ins. A new version of OpenTofu is a new digest in `edge-tofu.sh`, and a new version of the
   provider a change of `versions.tf` followed by `edge-tofu.sh lock`, both made by hand: Dependabot does not read
   the script, and its `opentofu` ecosystem is not configured.

## Consequences

**Positive:**

- The order of the routes and the policy are in git: a change has a review and a history, and a change in the
  dashboard is found within a week by a check, not by chance.
- No token that writes, no identifier and no address is in git or in CI, and no state file exists that could leak.
- No new action and no new publisher: the image is pinned by digest and runs without capabilities.

**Negative:**

- Every plan imports, so `tofu` sees the live objects and the code, never what changed between two runs: it does not
  delete an object taken out of the code, and every object in the code must exist before its import block.
- The first plan may show attributes that the dashboard set and the wiki never recorded; they are written into the
  code until the plan is clean (task 5 of [MIL-078]).
- A member of staff is added or removed in two places, the settings of the person who applies and the secret
  `CLOUDFLARE_TEAM_EMAILS`; a change in the dashboard alone opens the issue.
- The pins are updated by hand, and the provider, about 200 MB, is downloaded on every run.
- The environment `cloudflare-read` records a deployment in GitHub for every run; the DevOps Research and Assessment
  (DORA) metrics read the environment `production` only, so they do not count it.
- The token that reads can expire; until S01 renews it, every weekly run fails, and a failed run is a red check on
  the head of `main` that the deploy gate counts, as a failed daily scan is.
- Running the minimal image directly is not a use that the OpenTofu project documents; it works because `tofu` is one
  static binary that needs only certificate authorities and a writable `/tmp`.

**Quality:** security (no token that writes in CI, and no identifier or address in a public file or log),
maintainability (the edge reviewed as code) and reliability (a drift found within a week) (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0027]: a third setting of a console in git, which meets the two objections to its option 3 for this case: no
  state store, and the token that writes only on a person's machine. Its two parts stay deferred.
- [ADR-0028]: decision 8: the routes of the tunnel are code; the zone settings stay in the dashboard.
- [ADR-0029]: decision 5 and two negative consequences: the routes in their order are code, compared every week, and
  the addresses of the staff are a setting outside git.
- [ADR-0037]: one more workflow under its rules.
- [MIL-078]: tasks 2 to 5 carry out this decision.

---

[MIL-078]: ../milestones/mil-078-the-tunnel-routes-and-access-as-code.md
[ADR-0027]: ./adr-0027-configuration-as-code.md
[ADR-0028]: ./adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ./adr-0029-back-office-behind-cloudflare-access.md
[ADR-0037]: ./adr-0037-github-actions-hardening-policy.md
[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[MIL-072]: ../milestones/mil-072-the-access-check-on-every-admin-path.md
[RC-338]: ../sqa/reviews/rc-338-mil-078.md
[RC-339]: ../sqa/reviews/rc-339-mil-072-gate.md
