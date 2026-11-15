# ADR-0027 The tailnet policy and the Proxmox definitions in git

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0027 |
| CrossReference | [ADR-0003], [ADR-0020], [MIL-038], [RC-161], [ADR-0046] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25 | Deprecated | mikemike | AI as stand-in for S01 | Decision 4: the work is deferred until a trigger; S01 decided in chat on 2026-10-25 ("Ja og begrunde med et acceptabelt hvorfor og valg") | [199bc05] |
| 2026-11-15 | Accepted | mikemike | AI as stand-in for S01 | Context: ADR-0046 brings a third setting of a console into git, the routes of the Cloudflare tunnel and the Access application of the back office, with OpenTofu and without a state store (MIL-078); the two parts of decision 4 stay deferred | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** S01 asked on 2026-10-24 for the gaps of the DevOps review to be closed. One gap was that two pieces of the setup live only in a console: the access policy of the tailnet, and the definitions of the VM and the LXC on the Proxmox node. The options and the decision below were written by the AI assistant.

The tailnet policy decides who reaches `nf-hotel` and the monitoring, and over which ports; since MIL-023 the only SSH to the VM is over the tailnet. It is edited in the Tailscale admin console, so a change has no review, no history in git and no test. Today it allows every member everything (`autogroup:member`), which was noted in MIL-023 and not changed. The VM (100) and the LXC (102) were created by scripts (`create-docker-vm.sh`, `create-monitoring-lxc.sh`), but later changes on the node (memory, disks, start at boot) are only visible with `qm config` and `pct config`.

Options considered:

1. **Keep the consoles,** and write changes in the wiki. Nothing to build, but a change and its reason can drift apart, and nobody is told.
2. **The tailnet policy in git, tested and applied by the Tailscale GitOps action** (`tailscale/gitops-acl-action`). A pull request runs the policy's own `tests` against the policy; a merge to main applies it with an OAuth client that may only write the policy file. The console then shows a banner that the policy is managed in git.
3. **Terraform** for the tailnet and for Proxmox (the providers `tailscale/tailscale` and `bpg/proxmox`). One tool for both, but it needs a state store and a token with write access to the whole node, which is the root of every VM on it, including `sports-club`, which is not ours.

**2026-11-15 ([ADR-0046], MIL-078).** A third setting that lived only in a console, the routes of the Cloudflare tunnel and the Access application of the back office, is now code in `ops/infra/cloudflare/`, with OpenTofu, the open fork of Terraform. It meets the two objections to option 3 for its own case: it keeps no state store, because every plan imports the live objects, and the token that writes stays on the machine of S01 or S03. The tailnet policy and the Proxmox definitions stay deferred as decision 4 says.

## Decision

1. **The tailnet policy follows option 2.** It lives in `ops/tailnet/policy.hujson` with `tests` that state what must stay true: the CD runner reaches `nf-hotel` on SSH, the monitoring LXC reaches `nf-hotel` for its probes, `nf-hotel` reaches the LXC for its metrics and logs, and nothing else reaches the VM's SSH. A pull request runs the test step; a merge to main applies it. The OAuth client has the scope `policy_file` only, and its id and secret are repository secrets.
2. **The first policy in git is the policy that is live,** exported from the console by S01, unchanged. *Why:* a policy written from memory could lock the CD or the team out on the first merge; tightening `autogroup:member` is a separate, reviewed change after that.
3. **The Proxmox definitions are recorded, not applied.** `ops/proxmox/vm-100.conf` and `ops/proxmox/lxc-102.conf` hold the output of `qm config 100` and `pct config 102`, without the volatile lines (the `digest`, the MAC addresses). A script compares them with the node and reports a difference; S03 runs it, because reading the node needs its root login. *Why not apply (option 3):* a token that can change VMs can change every VM of the node, and the node is S03's.
4. **What waits for whom.** The policy needs S01 to export it and to create the OAuth client; the definitions need S03 to run `qm config` and `pct config` once. Until then this ADR holds, and nothing is applied. **On 2026-10-25 S01 deferred both parts:** with one administrator and a policy that changes a few times a year, and a VM and an LXC that have not changed, they cost more to keep than the drift they would find. They are built when the tailnet gets a second administrator, when a policy change causes an incident, or when the node gets more machines of NF Hotel.

## Consequences

**Positive:**

- A change of who may reach the VM gets a review, a history and a test before it is live.
- A change on the node is found by a comparison instead of by surprise.

**Negative:**

- The console can still be edited by an admin; the GitOps action overwrites such a change at the next merge, which is the point but may surprise.
- The Proxmox comparison is a manual step of S03, not part of CI.

## Affected Artifacts

- [ADR-0020]: the paths of the monitoring become tests of the policy.
- [MIL-038]: tasks 2 and 3, waiting for S01 and S03.

---

[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[MIL-038]: ../milestones/mil-038-configuration-as-code.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
[ADR-0046]: ./adr-0046-the-tunnel-routes-and-access-in-opentofu.md
[199bc05]: https://github.com/NF-Hotel/NFHotel/commit/199bc05466d450b92157aa4daead871e7e6d76ce
