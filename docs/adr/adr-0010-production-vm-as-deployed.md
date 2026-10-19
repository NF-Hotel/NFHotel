# ADR-0010 The production VM as deployed, and the repository as its source of truth

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0010 |
| CrossReference | [ADR-0003], [ADR-0004], [ADR-0007], [ADR-0008], [MIL-021], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version, from the server inventory given by S01 and the checks of DSA-001 section 2.4.2 | [1b94761] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-098 Go | [1b94761] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

ADR-0003 decided that the services run as Docker containers in a KVM VM on the Proxmox VE server of Team 2. That decision is still followed. Several of its statements of fact, and the files of the repository, no longer match the server that runs NF Hotel today.

S01 gave this inventory on 2026-10-18. The checks of DSA-001 section 2.4.2 confirm or leave open each item:

| Item | Inventory (S01) | Evidence | Status |
| --- | --- | --- | --- |
| Hostname | `nf-hotel` | Tailnet node `nf-hotel.taildb84b5.ts.net`, owned by the tailnet user of S01, joined 2026-09-04 (the same day as the node `proxmox`) | Confirmed |
| Tailscale IP | `100.72.194.127` | `tailscale ip -4 nf-hotel` | Confirmed |
| Operating system | Debian 13 | ssh banner `OpenSSH_10.0p2 Debian-7+deb13u4` | Confirmed |
| TLS hostname | `nf-hotel.taildb84b5.ts.net` | Let's Encrypt certificate for that name, valid to 2027-01-04; port 80 redirects to it | Confirmed |
| Nginx | deployed | `nginx/1.30.5` on 80 and 443 | Confirmed |
| PostgreSQL | deployed | Port 5432 answers the PostgreSQL SSLRequest with `N` (no TLS), reachable from the tailnet | Confirmed |
| Python API | deployed | Not reached on the probed ports and paths; it may listen on the loopback or on a name that was not probed | Not verified |
| Proxmox VMID | 100 | Needs the Proxmox API or console; no credential is held by the assessor | Not verified |
| Compose file | `/opt/nfhotel/deployment/compose.yaml` | Not in this repository or any NF-Hotel repository (`gh search code`); no access to the VM | Not verified |
| Separate VM `sports-club` | Debian 13, separate | Tailnet node `sports-club`, another host key, joined 2026-10-13 | Confirmed |

What is out of date, or differs from the repository:

- **How the VM was made.** ADR-0003 expects `scripts/create-docker-vm.sh` (Debian 12 cloud-init) or `iac-node-forge-py`, "and how it was created is recorded there". `nf-hotel` runs Debian 13, and no record of its creation exists in the repository.
- **The deploy layout.** ADR-0003 says the compose files of MIL-002 and MIL-004 work unchanged, one per service, and the deploy scripts clone the repository into `/opt/nfhotel`. The server runs one file, `/opt/nfhotel/deployment/compose.yaml`, that is not version-controlled.
- **Network exposure.** ADR-0003 and ADR-0004 say no host port is published except by the nginx proxy, and that PostgreSQL is reachable only on an internal network. On the server, PostgreSQL answers on 5432 to every tailnet device, without TLS.
- **The proofs.** The deploy proofs RC-066, RC-077 and RC-082 ran in the throwaway Proxmox VE container of MIL-003, not on this server.

An earlier pass of DSA-001 (section 2.4.1) concluded from the nginx default page that `nf-hotel` did not run NF Hotel. That conclusion was not supported: a default page at `/` says nothing about the other services. It is withdrawn in section 2.4.2.

Options considered:

1. **Keep the server as it is and point CD at it as built.** CD would deploy a stack the repository does not describe, so a release could not be reviewed, reproduced or rolled back from git.
2. **Replace the server's stack with the repository's `deploy/` files on the first CD run.** Fast, but it risks the running services and the data, and it ignores whatever the running configuration does differently.
3. **Reconcile first.** Bring the running configuration into the repository, then let CD deploy from the repository.

## Decision

We follow option 3: `nf-hotel` (Proxmox VMID 100) is the production VM of ADR-0003, and the repository becomes the source of truth for its deployment before CD deploys to it.

1. **The production VM** is `nf-hotel`, Debian 13. ADR-0003 is followed except for the facts listed above, which this ADR replaces.
2. **Reconciliation (MIL-021 task 10).**
   - S03 copies `/opt/nfhotel/deployment/compose.yaml` into a pull request, with every secret replaced by a reference to a git-ignored settings file.
   - The pull request maps its services to the release images and its volumes, PostgreSQL included, to names the release keeps.
   - It is reviewed like any other change.
3. **No deploy before that.** The deploy command refuses to run while `/opt/nfhotel` is not a git checkout of `NF-Hotel/NFHotel`, so CD cannot start a second stack next to the running one.
4. **Data first.** Before the first CD deploy, a backup of the running database is taken and restored in a drill (ADR-0008).
5. **Close the PostgreSQL port.**
   - PostgreSQL is reachable only on the Docker network of the API, as ADR-0004 decided.
   - Until the reconciliation lands, the tailnet policy limits port 5432 to the administrators (risk R15).
6. **Record the VM.** The VMID, the size and the creation method of `nf-hotel` are written into the wiki page Proxmox Deployment once S03 has read them from the Proxmox node.

## Consequences

**Positive:**

- Every change on the production server becomes reviewable, reproducible and reversible from git.
- CD cannot overwrite the running service by mistake.
- The database is no longer reachable from every tailnet device.

**Negative:**

- CD cannot deploy to production until S03 has reconciled the deployment, which delays MIL-021.
- The reconciliation may show that the running stack and the `deploy/` files differ more than expected, for example in the Python API. That needs more work.
- Until it is done, the repository describes a stack that production does not run.

**Quality:** maintainability, reliability and security improve (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0003]: its decision stands; its statements on the creation method, the deploy layout and the published ports are replaced here.
- [ADR-0004]: the PostgreSQL exposure on the server deviates from it until the reconciliation.
- [ADR-0007]: CD deploys only after the reconciliation.
- [ADR-0008]: the backup must target the running PostgreSQL container.
- [MIL-021]: new task 10, the reconciliation.
- [DSA-001]: section 2.4.2 and risks R15 and R16.

---

[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0004]: ./adr-0004-database-api.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ./adr-0008-database-migration-and-backup.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[DSA-001]: ../sqa/devsecops-assessment.md
[1b94761]: https://github.com/NF-Hotel/NFHotel/commit/1b94761ddb5113cd0d34915c01fe7f0f278bba60
