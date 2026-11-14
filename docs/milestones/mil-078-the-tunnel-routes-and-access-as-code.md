# MIL-078 The tunnel routes and Access as code

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-078 |
| CrossReference | [BC-001], [ADR-0027], [ADR-0028], [ADR-0029], [ADR-0037], [MIL-038], [MIL-040], [MIL-041], [MIL-072] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-12 | Accepted | mikemike | S01 | Initial version: the routes of the Cloudflare tunnel and the Access application of the back office are kept only in the Cloudflare dashboard, set by hand from the wiki page Public-Access; planned at S01's request in chat ("Skriv 1–4", to the plan of 2026-11-12) | pending |

---

## Purpose

Keep the public edge of NF Hotel, the routes of the tunnel and the Access application with its policy, in code that is reviewed like any other change, and know within a week when the live setting differs from it, so that a route in the wrong place or a changed policy is found by a check, not by chance.

The site reaches the internet through a Cloudflare tunnel ([ADR-0028], [MIL-040]), and the back office only through Cloudflare Access ([ADR-0029], [MIL-041], [MIL-072]). Read on 2026-11-12 at 1c2726b:

| # | Finding | Where | What it allows or breaks |
| --- | --- | --- | --- |
| 1 | The tunnel runs with a token, so its routes, their order and the Access application with its policy live only in the Cloudflare dashboard, set by hand from the steps of the wiki page Public-Access | `ops/deploy/cloudflared/docker-compose.yml` (`--token-file`), `docs/wiki/Public-Access.md` | A change in the dashboard leaves no trace in git and no review, and nothing compares the live setting with the wiki |
| 2 | MIL-072 found the route that checks the Access token below another route, and moved it to the top by hand | [MIL-072] | The kind of difference that a comparison with code finds in the week it happens |
| 3 | The compose files, the units and the scripts that set up the production virtual machine (VM) and the LXC container are in git. The two pieces that [ADR-0027] decided to bring into git, the tailnet policy and the recorded Proxmox definitions, were deferred by S01 on 2026-10-25: they cost more to keep than the drift they would find, and are built when the tailnet gets a second administrator, when a policy change causes an incident, or when the node gets more machines | ADR-0027, [MIL-038] | The edge differs from those two parts: it has drifted once, and that drift was a security finding |
| 4 | A Cloudflare token that can write the tunnel and Access can open the back office to anyone | — | Such a token must never be in continuous integration (CI) or in git; a comparison needs only a token that reads |

## Deliverable

- **The decision.** A new architecture decision record (ADR) on the tool: OpenTofu with the Cloudflare provider, pinned with its lock file and run from its official image pinned by digest, so that no new publisher of actions is needed. It also decides where the state lives: no stored state as the first choice, because every run imports the live objects through `import` blocks and keeps no state file with identifiers; encrypted remote state only if that does not work. And it decides that the identifiers of the account, the tunnel and the zone, and the listed addresses, stay out of git, and how the two tokens are kept.
- **The code.** `ops/infra/cloudflare/`: the routes of the tunnel in their order, with the route that checks the Access token first; the Access application `NF Hotel back office` with its session duration; and the policy `Team`, whose addresses come from a setting outside git, marked sensitive; with an `import` block for each of them.
- **The proof that the code matches.** A plan against the live account shows the imports and 0 to add, 0 to change and 0 to destroy, before anything is applied.
- **The weekly comparison.** A workflow runs every week and by hand. It runs the plan with a token that only reads, opens an issue, or comments on the open one, when the live setting differs from the code, and closes the issue when the plan is clean again, in the same way as the daily scan of the running images. The logs of a public repository are public, so it never prints the plan itself: it prints the counts and the names of the objects that differ. It follows [ADR-0037]: the image pinned by digest, `contents: read` at the top, `issues: write` for its job only, and the token read by the step of the plan only. `tofu fmt -check` and `tofu validate` run in Ops CI.
- **The way to change the edge.** A change is a pull request, applied by S01 or S03 from their own machine with a token that can write and never leaves it; nothing applies by itself.
- **The documents.** Public-Access (the routes and Access in code, how to change and apply them, and how to read the weekly issue), Technology-Choices (OpenTofu), and Version History rows of [ADR-0027], ADR-0028 and ADR-0029.
- **Not part of this milestone:** the Proxmox VM and LXC as code (the scripts with cloud-init make them, and S01 deferred their recorded definitions), the tailnet policy (deferred in the same decision), and the Domain Name System (DNS) records outside the tunnel.

Order: after the gate of [MIL-072], whose routes are the ones that the code describes. The review of this document first (QC-MIL-001 and QC-LANG-001), then task 1 with its own review, tasks 2 to 4 on one branch with their pull request, task 5 with S01, and task 6 last.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The ADR is Accepted with a Go review | Yes | No |
| 2 | `tofu fmt -check` and `tofu validate` pass in Ops CI, and `test-workflow-policy.sh` passes with the new workflow | Yes | No |
| 3 | The first plan against the live account, run by hand in the new workflow with the token that reads, shows only imports and 0 to add, change or destroy; its log shows no identifier and no address, and the review record keeps the summary | Yes | No |
| 4 | On S01's yes, S01 changes a harmless field of the Access application in the dashboard (for example its message to a denied visitor); the next run of the workflow opens an issue that names the object, and after S01 undoes the change, the run after it closes the issue | Yes | No |
| 5 | One pull request changes a harmless field in the code, S01 applies it with the token that writes, and the next plan shows 0 changes | Yes | No |
| 6 | Public-Access, Technology-Choices and the three ADR rows say what the deliverable says, `test-wiki-coverage.sh` passes, the code review against `QC-WF-001` and `QC-YML-001` is Go (the framework has no checklist for OpenTofu code, so the review checks that code against the ADR and says so), and every required check of the pull request is green | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-072 | The routes and the Access check on every spelling of `/admin` that the code describes; this milestone comes after its gate |
| MIL-040 | The tunnel and the public host name |
| MIL-041 | The Access application of the back office |
| MIL-038 | Configuration as code for the VM |
| ADR-0027 | Configuration as code, and the two parts that S01 deferred |
| ADR-0037 | The rules that every workflow follows |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 34 and success criterion 37: the back office behind Access, with the route that checks the token first | Criteria 3 and 4: the order and the policy in code, and a difference found within a week |
| BC-001 objective 41 and success criterion 44: the configuration of the platform in git and reviewed (in the Proposed revision of BC-001, RC-332) | Criteria 1 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-04: after the gate of MIL-072 and the two tokens from S01. It does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | The decision | The ADR compares the edge set by hand as now, the tunnel with a local configuration file in git, and OpenTofu with the Cloudflare provider; it decides the state, the two tokens, and the identifiers and addresses outside git; with its own review record | No | criterion 1 |
| 2 | The code | `ops/infra/cloudflare/` with the provider and its lock file, the routes in their order, the Access application, the policy with its addresses from a sensitive setting, and the `import` blocks; `fmt` and `validate` in Ops CI | No | criterion 2 |
| 3 | The weekly comparison | The workflow, every week and by hand: the plan in the pinned image, only the counts and the names of the objects in its log, and the issue handling of the daily scan; `test-workflow-policy.sh` | No | criteria 2 and 3 |
| 4 | The documents, code review and pull request | Public-Access, Technology-Choices and the ADR rows; the review record against `QC-WF-001` and `QC-YML-001`; the pull request, which closes the issues of tasks 2 to 4 | No | criterion 6 |
| 5 | The tokens, the first plan and the drill | S01 creates the token that reads as a secret of the workflow's environment, and the token that writes on their own machine; the AI assistant never sees either. Then the first plan by hand (criterion 3), the drill of criterion 4 on S01's yes, and the change of criterion 5, applied by S01 | No | criteria 3, 4 and 5 |
| 6 | Gate and review record | The gate review record against criteria 1 to 6, the row of MIL-078 in the traceability matrix, the Accepted row of this document, and the closing of the GitHub milestone | No | criteria 1 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0027]: ../adr/adr-0027-configuration-as-code.md
[ADR-0028]: ../adr/adr-0028-public-access-through-cloudflare-tunnel.md
[ADR-0029]: ../adr/adr-0029-back-office-behind-cloudflare-access.md
[ADR-0037]: ../adr/adr-0037-github-actions-hardening-policy.md
[MIL-038]: ./mil-038-configuration-as-code.md
[MIL-040]: ./mil-040-public-access.md
[MIL-041]: ./mil-041-admin-behind-cloudflare-access.md
[MIL-072]: ./mil-072-the-access-check-on-every-admin-path.md
