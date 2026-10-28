# RC-192 Firewall on nf-hotel: SSH over Tailscale only (MIL-023 task 3, criterion 2)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-192 |
| CrossReference | [MIL-023], [CIS-001], [RC-176], [RC-191], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [d9d9852] |

---

## Artifact Under Review

- **Instances reviewed:** the change of the host firewall on `nf-hotel` to the ruleset in git, `ops/deploy/host/nftables/nfhotel.nft` (reviewed in RC-176), with `ops/scripts/apply-firewall.sh` at release `65e28de`; the wiki page Host-Hardening, section "Firewall: SSH over Tailscale only"; CIS-001 rows 4.1.1, 4.3.1, 4.3.2 and 4.3.4 to 4.3.8.
- **Decision:** S01 in chat on 2026-10-24: SSH (TCP 22) only on `tailscale0`; SSH from the LAN (`192.168.10.0/24`) and from the internet dropped; the default drop policy kept. Reason: the server is in S01's home, and the Proxmox console is the way in when Tailscale is down, so SSH from the LAN is not needed as a fallback.
- **Who did what:** the AI ran every step as root over Tailscale SSH, with S01's permission (autonomous mode, 2026-10-24).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Times:** UTC.

## Before the change

- **Backup** in `/root/firewall-backup-20261024T153330Z/` on the VM: the complete ruleset (`nft list ruleset`), the table `inet nfhotel`, `/etc/nftables.d/` and the old `nfhotel-firewall.service`.
- **The old state**, set up by hand during MIL-021: the table `inet nfhotel`, loaded from `/etc/nftables.d/nfhotel.nft` by a unit whose `ExecStop` removed the whole table. It allowed SSH from `192.168.10.0/24` on `ens18` as well as on `tailscale0`, and had no forward chain.
- **Access paths checked:** Tailscale SSH as root worked. The LAN path was tested from the monitoring LXC (`192.168.10.116`, on the same bridge): port 22 of `192.168.10.72` was open, and ping answered. The laptop on Wi-Fi could not be used as a LAN client: its packets did not reach the VM at all (no ARP answer), which points to client isolation of the Wi-Fi.
- **Impact checked:** the only published ports are nginx's 80 and 443 on the Tailscale address, and 8081 on the loopback. CD reaches port 22 over the tailnet. Outbound traffic (backup to legion:2222, Alloy, apt, GHCR) is not filtered.
- **Recovery:** S01 has the Proxmox console of VM 100. The AI cannot check that path (no access to the node); it rests on S01's statement.

## The change

| Time | Step | Result |
| --- | --- | --- |
| 15:36:32 | `apply-firewall.sh --apply --timeout 300` | New table loaded; the old one would come back at 15:41:32 without a confirmation |
| 15:36:45 | New Tailscale SSH session | Works; `iifname "tailscale0" tcp dport 22 accept` is the only rule for 22 |
| 15:36:4x | Port 22 of `100.72.194.127` over the tailnet (the CD path) | Open |
| 15:36:4x | From the LAN (`192.168.10.116`) | Port 22 **closed**; ping still answers |
| 15:36:4x | The site | HTTPS 200, HTTP 301 |
| 15:37:02 | `apply-firewall.sh --confirm` from a new session | Permanent: `/etc/nfhotel/nftables.nft`, `nfhotel-firewall.service` from git, enabled |
| 15:41:56 | After the revert deadline | No LAN rule, the `docker-user` chain present; every container healthy (the socket proxy has no health check and runs); legion:2222 reachable; the four probes of the monitoring 1; the last sample from `nf-hotel` 26 s old; only the Watchdog firing |

`cis-evidence.sh` after the change: `/root/cis-evidence-after-firewall.txt` (exit 0). `nftables.service` disabled, `ufw` not installed.

## Rollback

In the wiki page Host-Hardening, section "Rollback": load `nftables.d/nfhotel.nft` from the backup and put the old unit back. A recovery through the Proxmox console when Tailscale is down is described there too.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| MIL-023 2 | 22 answers only over Tailscale | Pass | LAN test above; the internet never reached 22 (no port forward, and now no rule) |
| MIL-023 2 | Only 80 and 443 answer from outside the tailnet | Partly | Holds by the ruleset and the listeners; the external scan and the check across a Docker restart come with step 4 |
| No lockout | Automatic revert, backup, confirm from a new session | Pass | Table above |
| Services | Docker, CD, monitoring, backup | Pass | Table above |

## Overall Verdict

Go. CIS-001 rows 4.1.1, 4.3.1, 4.3.2 and 4.3.4 to 4.3.8 are Applied.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Step 4 (Docker daemon) and the check that the firewall stays across a Docker restart | S01 decides the window; the AI runs it | Next window |
| 2 | Scan the public address from outside the tailnet | S01 | With step 4 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[RC-176]: rc-176-mil-023-host-hardening.md
[RC-191]: rc-191-host-hardening-apply-part-1.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[d9d9852]: https://github.com/NF-Hotel/NFHotel/commit/d9d985227a317d6668c07115d37a8473c223be09
