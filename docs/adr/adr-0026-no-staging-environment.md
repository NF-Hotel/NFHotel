# ADR-0026 No staging environment: the throwaway stack of CI, the smoke test and the rollback guard production

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0026 |
| CrossReference | [ADR-0021], [ADR-0023], [ADR-0024], [MIL-039] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-039, which S01 accepted in chat on 2026-10-24 | [c475cdd] |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | The capacity of the node is not needed for this decision; S01 decided in chat on 2026-10-25 ("Ja og begrunde med et acceptabelt hvorfor og valg") | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** S01 asked on 2026-10-24 for the gaps of the DevOps review to be closed. One gap was that a release goes from CI straight to production. The options, the measurements and the decision below were written by the AI assistant.

Today every release is built once, scanned, and tested on a throwaway stack of the same images and compose files in CI: the end-to-end tests in a browser and the DAST baseline (MIL-027). CD deploys only when every check of the commit is green, then runs a smoke test on the VM and rolls back to the previous release when it fails (ADR-0021). The database is dumped before every deploy (ADR-0008). Since MIL-034 and MIL-035, the deploy also fails when a secret is in a container's environment, and alerts fire when the requests get slow or fail.

**Capacity, measured on 2026-10-24.** The production VM `nf-hotel` has 4 vCPU, 20 GiB of memory (1 GiB used, 19 GiB available) and a 111 GB disk (31 GB used). The node also runs the VM `sports-club` (101) and the monitoring LXC (102, 2 cores, 4 GiB). The free capacity of the node itself could not be read: that needs the root login of Proxmox, which only S03 holds. A staging VM like production would need 4 vCPU and 12 to 20 GiB more on the node. S01 decided on 2026-10-25 not to read it now: the incidents below decide the question whatever the capacity, and it is read first when the decision is taken again (decision 4).

**What a staging environment would have caught.** The production incidents of October 2026, and whether each option would have stopped them before production:

| Incident | Cause | Throwaway stack in CI | Staging VM | Staging stack on the VM |
| --- | --- | --- | --- | --- |
| nginx did not start (#252) | the real certificate key is 0600, unreadable under `cap_drop: ALL` | No: CI makes its own certificate | Only with a copy of the real key | Yes, same key |
| The market analysis answered 422 (#264) | its settings file was 0600 after a checkout switch on the VM | No | No: a fresh VM has no such history | Yes, same files |
| The certificate was not renewed | `NGINX_SERVER_NAME=_` in the settings of production | No | No: other settings | Yes, same settings |
| The monitoring could not read its secrets | file owners in the LXC | No | No | No |

All four were state of production: file modes, settings and history that a separate environment does not have. The smoke test and the alerts found them within minutes, and the first was rolled back automatically.

Options considered:

1. **A persistent staging VM** like production, deployed first by CD. It catches what differs between CI and a real VM (the kernel, the firewall, Tailscale), but needs 4 vCPU and 12 to 20 GiB that the node may not have, a second set of secrets and certificates, and its own upkeep; and by the table above it would have caught none of the incidents.
2. **A staging stack on the production VM** (a second compose project with its own networks and database). It shares the settings and the files, so it would have caught three of the four. But it shares the resources too: a load or a leak in staging slows the hotel, and a second copy of every secret lives on the VM. It also needs a second hostname and certificate, and a copy of the data, which is personal data of guests.
3. **The throwaway stack of CI only, with checks on the VM after the deploy.** No new environment. The difference from production is closed where it shows up: on the VM, right after the deploy, with an automatic rollback.

## Decision

We follow option 3: **no staging environment.**

1. **Before production:** the throwaway stack of CI stays the test of every release: the same images and compose files, the end-to-end tests, the DAST baseline, the log privacy check and the secret check (MIL-027, MIL-036, ADR-0024).
2. **After the deploy, on the VM:** the deploy fails, and rolls back, when a container does not turn healthy, a secret is in a container's environment, or the smoke test fails; the dump before the deploy protects the data (ADR-0021, ADR-0008).
3. **The remaining risk,** named: a release that starts and answers the smoke test, but is wrong for guests in a way the end-to-end tests do not cover, reaches production. **The checks that cover it** are the alerts on the objectives of MIL-034 (`ApplicationErrors`, `ApplicationSlow`, `ApplicationExceptions`, ADR-0023) within 5 to 10 minutes, and the rollback command of the wiki page CI-CD, which takes seconds.
4. **When to decide again:** when the hotel takes real bookings, when a release needs a migration that cannot be rolled back (expand then contract, ADR-0008, no longer holds), or when S03 finds the node has room for option 1.

## Consequences

**Positive:**

- No second set of secrets, certificates or guest data, and no load beside production.
- The checks that found the real incidents (the smoke test, the alerts, the rollback) are the ones the decision relies on.

**Negative:**

- A bad release can reach the guests for the minutes between the deploy and an alert, and a rollback is a decision a person makes.
- A difference between the CI runner and the VM (kernel, firewall, Tailscale) is found only in production, by the smoke test.
- The free capacity of the node is not known; option 1 cannot be priced until S03 reads it.

## Affected Artifacts

- [ADR-0021]: the gate and the rollback stay the guard of production.
- [MIL-039]: tasks 1 and 2 (the remaining risk is recorded; nothing is built).

---

[ADR-0021]: ./adr-0021-automatic-deploy-when-green.md
[ADR-0023]: ./adr-0023-application-telemetry.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[MIL-039]: ../milestones/mil-039-staging-environment.md
[c475cdd]: https://github.com/NF-Hotel/NFHotel/commit/c475cdd6800351d6fa7e7852a39227b667296609
