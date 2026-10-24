# RC-191 Host hardening on nf-hotel, part 1: baseline and patching (MIL-023 tasks 1 and 4)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-191 |
| CrossReference | [MIL-023], [CIS-001], [RC-176], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [dd22a11] |

---

## Artifact Under Review

- **Instances reviewed:**
  - steps 1 and 2 of the wiki page Host-Hardening, run on `nf-hotel`;
  - two fixes of the scripts that those steps found: `ops/scripts/cis-evidence.sh` and `ops/scripts/install-patching.sh`.
- **Who did what:** the AI ran the steps as root over Tailscale SSH, at release `8ce9452`. S01 gave the permission and approved the session in chat on 2026-10-24.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).

## What was done on nf-hotel

1. **Baseline (step 1).** `cis-evidence.sh` wrote `/root/cis-evidence-before.txt` (655 lines). It shows:
   - 65 packages waiting for updates, among them `openssl`, `libssl3t64`, `tailscale` and `docker-ce`. `unattended-upgrades` was not installed.
   - The Docker daemon has no `daemon.json`: `live-restore=false`, log driver `json-file`.
   - **A firewall is already in place.** The table `inet nfhotel` was set up by hand during MIL-021 and is loaded by `nfhotel-firewall.service` from `/etc/nftables.d/nfhotel.nft`. It differs from `ops/deploy/host/nftables/nfhotel.nft`:
     - it allows SSH from the LAN (`192.168.10.0/24`) as well as over Tailscale;
     - it allows port 80 only on `tailscale0`;
     - it has no `docker-user` chain.
2. **Patching and logs (step 2).** `install-patching.sh --apply`:
   - installed `unattended-upgrades` 2.12, with the Debian security origin only and no automatic reboot;
   - made the journal persistent with at most 1 GB (651.5 MB in use);
   - enabled the daily timers. The next run is 2026-10-25 06:05 CET.

   The first apply failed at the check: `unattended-upgrade --dry-run` asked the mirror for `libgs10 …deb13u2`, which a newer security update had replaced, and got 404. The package lists were out of date. After `apt-get update`, the apply passed.

## Fixes of the scripts

- **`cis-evidence.sh`** stopped in the container section with `map has no entry for key "Health"` on the container without a health check (the socket proxy of Alloy). Docker 29.8 fails on the missing key inside this longer template, though not in a short one. The template now uses `{{with index .State "Health"}}`, which works on 29.7 and 29.8. The fixed script was run on `nf-hotel` from this branch: all ten sections were written, exit 0.
- **`install-patching.sh`** now runs `apt-get update` before it installs, so its check never meets old package lists. `test-host-setup.sh` passes 16.

## Not done, and why

- **The first security upgrade by hand (`unattended-upgrade -v`) was refused** by the safety check of the AI's tool. The daily timer installs the same updates by itself on 2026-10-25, which gives the log entry of MIL-023 criterion 4.
- **Step 3, the firewall, waits for S01.** Applying the planned ruleset changes the access paths of the VM:
  - SSH from the LAN stops;
  - 80 and 443 open on every interface (nginx listens on the Tailscale address only);
  - forwarded traffic from outside is limited to 80 and 443.

  It reverts by itself unless it is confirmed, but whether to drop SSH from the LAN is S01's decision.
- **Step 4, the Docker daemon,** restarts Docker once, which stops every container for about a minute. It waits for S01's window.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | The fixes are tested | Pass | `test-host-setup.sh` 16 passed; the fixed `cis-evidence.sh` ran on `nf-hotel`, all sections, exit 0 |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |
| MIL-023 4 | Security updates installed by unattended-upgrades | Pending | The timer of 2026-10-25 06:05 CET; to be read in `/var/log/unattended-upgrades/` |

## Overall Verdict

Go for the work done. MIL-023 tasks 1 to 4 stay open for steps 3 to 6.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | Decide on step 3: drop SSH from the LAN as planned, or keep it in the ruleset | S01 | Before step 3 |
| 2 | Choose a window for step 4, the Docker restart | S01 | Before step 4 |
| 3 | Read the log of the first unattended upgrade | AI | 2026-10-25 |

---

[MIL-023]: ../../milestones/mil-023-operational-quality.md
[CIS-001]: ../cis-checklist.md
[RC-176]: rc-176-mil-023-host-hardening.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[dd22a11]: https://github.com/NF-Hotel/NFHotel/commit/dd22a116927d5653b130e3d9762bdea6060468f9
