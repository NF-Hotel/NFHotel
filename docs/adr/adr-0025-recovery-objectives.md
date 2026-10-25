# ADR-0025 Recovery objectives of NF Hotel, and the single points of failure that are accepted

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0025 |
| CrossReference | [ADR-0003], [ADR-0008], [ADR-0020], [ADR-0024], [MIL-037] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Deprecated | mikemike | AI as stand-in for S01 | Initial version; written by the AI in autonomous mode under MIL-037, which S01 accepted in chat on 2026-10-24 | [0a76a9e] |
| 2026-10-25 | Accepted | mikemike | AI as stand-in for S01 | Decision 5: the rebuild drill is postponed and the proof rests on the drilled parts; S01 decided in chat on 2026-10-25 ("Ja og begrunde med et acceptabelt hvorfor og valg") | [199bc05] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** S01 asked on 2026-10-24 for the gaps of the DevOps review to be closed. One gap was that only a restore of the database is drilled, not a rebuild of the service after the loss of its VM, its monitoring or its Proxmox node. The objectives and the decision below were written by the AI assistant.

What exists today:

- The database is dumped every night and before every deploy, encrypted with age to the key of S03, kept 14 daily and 8 weekly on the VM, and copied to the legion laptop when it is on the tailnet (ADR-0008). `BackupStale` alerts after 26 hours without a backup, and `OffVmCopyStale` after 72 hours without a copy (ADR-0020). The restore is drilled with `restore-drill.sh`.
- The VM is built by `create-docker-vm.sh` from the Debian cloud image and cloud-init, and the application arrives as released images from GHCR through the CD (ADR-0003, ADR-0018). Nothing on the VM is installed by hand except the settings files, root's `/etc/nfhotel` and the tailnet login.
- The settings files and `/etc/nfhotel` were not backed up. Losing the VM disk lost every secret, and a rebuild had to create new ones and re-key every client.
- NF Hotel is a school project for one hotel. It has no paying guests yet, and no contract states an availability.

Options considered:

1. **High availability:** a second node, replicated PostgreSQL and a floating address. It removes the node as a single point of failure, but doubles the hardware and the operations, for a service whose cost of a few hours down is small.
2. **Backups and a written rebuild, with objectives that fit them.** Keep one node, back up everything the rebuild needs, and state how long a rebuild may take and how much data may be lost, so that a drill can prove it.
3. **Proxmox backups of the whole VM (vzdump) to another disk.** Fast to restore, but it needs a second storage the node does not have and access to the node that the team does not have outside S03, and it copies a hardened VM image whose secrets would then sit in two places.

## Decision

We follow option 2.

1. **Recovery time objective (RTO): 4 hours** from the decision to rebuild until the site answers 200 over HTTPS with the restored data, when the Proxmox node works. *Why 4 hours:* the VM is built in about 10 minutes, the images are pulled, and the restore of a small database takes minutes; the rest is the people (S03 at the node, the tailnet login), and an afternoon is what the hotel can wait.
2. **Recovery point objective (RPO): 24 hours** of bookings when the VM disk is lost, from the nightly backup on the off-VM copy. *Why:* it is what the nightly backup gives without new parts. The worst case is **72 hours** while the laptop is off, which `OffVmCopyStale` alerts on; that case is accepted (see 4).
3. **What the rebuild needs is backed up.** The nightly backup also writes `nfhotel-settings-<kind>-<time>.tar.age`: the settings files of `ops/deploy`, the market analysis settings, the proxy certificate, root's `/etc/nfhotel` and the ssh key of the copy, encrypted to the same key of S03 and copied off the VM with the dumps. The secret files that the deploy writes from the settings (ADR-0024) are left out, because the deploy writes them again.
4. **Accepted single points of failure,** each with what happens:
   - *The Proxmox node and its home* (power, network, hardware). If the node is gone, the RTO does not hold: a new node must be bought and installed first. Option 1 was rejected above.
   - *The legion laptop as the only off-VM copy.* If the VM and the laptop are lost together, the data is lost. A second off-site target is the next step if the hotel goes live.
   - *The private age key of S03.* Every backup is encrypted to it; if it is lost, no backup can be read. S03 keeps it offline, as ADR-0008 says; a second offline copy in another place is part of the runbook.
   - *The monitoring LXC.* Its secrets are the Discord webhook, the Healthchecks.io ping address and the Grafana password, all re-issued by their providers in minutes, and its history (14 days of logs, 30 days of metrics) is accepted as lost. It is rebuilt from `create-monitoring-lxc.sh` and `deploy-monitoring.sh`, not backed up.
5. **The proof.** A drill rebuilds the service on a throwaway VM from the runbook and the latest off-VM backup, records the times in an RC, and removes the VM. It needs S03 at the node, because creating a VM needs the root login of Proxmox. **On 2026-10-25 S01 postponed the drill** until the hotel goes live or the node is replaced: every step is proven on its own (the VM build, the drilled database restore, the settings archive in CI, the daily CD), so only the whole sequence and its time stay unproven, and the RTO is a target until then.

## Consequences

**Positive:**

- A lost VM can be rebuilt with its own secrets, so the clients (the benchmark service, the mobile app, the backup target) keep working without new keys.
- The objectives are numbers a drill can check, and the alerts already watch the two that can drift (`BackupStale`, `OffVmCopyStale`).

**Negative:**

- A lost node stops the service for days, not hours.
- The settings archive holds every secret of the VM; its protection is the key of S03, as it is for the dumps.
- The monitoring history is lost with its LXC.

## Affected Artifacts

- [ADR-0008]: the nightly backup also archives the settings.
- [ADR-0020]: the monitoring LXC is rebuilt, not restored.
- [MIL-037]: tasks 1 to 4.

---

[ADR-0003]: ./adr-0003-proxmox-vm-for-services.md
[ADR-0008]: ./adr-0008-database-migration-and-backup.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
[ADR-0024]: ./adr-0024-secrets-as-files.md
[MIL-037]: ../milestones/mil-037-disaster-recovery.md
[0a76a9e]: https://github.com/NF-Hotel/NFHotel/commit/0a76a9e7558fe649506a51ce47dd9c02d80b97bf
[199bc05]: https://github.com/NF-Hotel/NFHotel/commit/199bc05466d450b92157aa4daead871e7e6d76ce
