# CI/CD

How a change reaches the NF Hotel server, how a deploy is approved and undone, and how the database is backed up and restored. The decisions behind it are in ADR-0007 and ADR-0008. The assessment and the quality gates are in DSA-001 (`docs/sqa/devsecops-assessment.md`).

## How a release flows

Two repositories take part (ADR-0012 in NF-Hotel/NFHotel):
- **NF-Hotel/NFHotel** holds the application and the workflow entry points `ci.yaml`, `release.yaml` and `cd.yaml`, which keep the triggers, permissions, the `production` environment and the OIDC claims.
- **NF-Hotel/DevOps** (this repository) holds the steps: the composite actions `release-images` and `deploy-release`, which NFHotel pins by full commit SHA, and everything that runs on the VM.

1. **Pull request.** `NF Hotel CI` builds, tests and checks the formatting on every pull request, with a read-only token and no secrets. One approving review is required.
2. **Merge to `main`.** CI runs again on `main`. When it is green, `NF Hotel Release` in NFHotel calls the `release-images` action. The action builds four images from that exact NFHotel commit, with the compose files and scripts of the pinned DevOps commit, and pushes them to the GitHub Container Registry:
   - `ghcr.io/nf-hotel/nfhotel-web`
   - `ghcr.io/nf-hotel/nfhotel-api`
   - `ghcr.io/nf-hotel/market-analysis`
   - `ghcr.io/nf-hotel/benchmark`

   Each image is tagged `sha-<NFHotel commit>` and labelled with that commit (`org.opencontainers.image.revision`). The run summary and the artifact `release-manifest` (`release.json`) hold the NFHotel commit, the DevOps commit and one digest per image.
3. **Approval.** `NF Hotel CD` starts and waits in the environment `production` until a required reviewer approves it. The person who started the run cannot approve it.
4. **Deploy.** After the approval:
   - the job joins the tailnet as a temporary node tagged `tag:ci`;
   - it logs in to the VM with Tailscale SSH as the user `deploy`;
   - it runs `sudo -n /usr/local/sbin/nfhotel-deploy deploy <NFHotel commit> <DevOps commit> <four digests>` (the `deploy-release` action).
5. **On the VM**, the deploy command (`scripts/deploy-release.sh`):
   - backs up the database;
   - checks out the DevOps commit in `/opt/nfhotel`, a checkout of `NF-Hotel/DevOps`. The application is in the images, so the VM needs no NFHotel checkout;
   - starts the services from the digests without building anything;
   - checks that port 80 answers 301 and HTTPS answers 200.

   If the check fails, it deploys the previous release again and the run turns red.

Services whose image and settings did not change are not restarted. Docker volumes, and so the database, are never removed by a deploy.

## Approve a deploy

Open the waiting `NF Hotel CD` run, choose **Review deployments**, tick `production` and approve. Before approving:

- read the release summary of the `NF Hotel Release` run it came from;
- check that the commit is the one you expect.

## Roll back

- **To the release before the latest one:** on the VM, run `sudo /usr/local/sbin/nfhotel-deploy rollback`. Running it again switches back to where you came from.
- **To any earlier release:** start `NF Hotel CD` by hand (Actions, NF Hotel CD, Run workflow) with the run ID of the `NF Hotel Release` run you want to deploy. It is approved like any other deploy.
- **To see what is deployed:** `sudo /usr/local/sbin/nfhotel-deploy status` prints the release log (`/var/lib/nfhotel-deploy/releases.log`).

A rollback changes the images and the compose files, not the database. Migrations are written so that the previous release still runs on the new schema (ADR-0008, expand then contract).

## Change an action or a deploy script

1. Change it in NF-Hotel/DevOps by a pull request; the DevOps CI must be green and one other person must approve.
2. In NF-Hotel/NFHotel, open a pull request that moves the pinned SHA in `release.yaml` and `cd.yaml` to the merged DevOps commit. Dependabot proposes this bump once it is set up (MIL-022).
3. A script that runs as root on the VM (`deploy-release.sh`, `backup-database.sh`) takes effect only after S03 installs it again with `install-deploy-user.sh --apply` or `install-backup.sh --apply`.

## Back up and restore

- The timer `nfhotel-backup.timer` runs `/usr/local/sbin/nfhotel-backup` every day at 02:00 UTC, and every deploy runs it first.
- Each backup is a `pg_dump` encrypted with `age` to the public key of S03, in `/var/backups/nfhotel`. It is copied to `BACKUP_TARGET` when that is set in `/etc/nfhotel/backup.env`.
- 14 daily and 8 weekly backups are kept on the VM.
- **Restore drill (monthly):** copy a backup to a machine with Docker and the private key, then run:

  ```bash
  bash scripts/restore-drill.sh --identity nfhotel-backup.key nfhotel-daily-<stamp>.dump.age
  ```

  It restores into a throwaway container and lists the tables, row counts and the latest migration. Record the output in an RC.
- **A real restore** is done by S03. Stop the API, restore the decrypted dump with `pg_restore --clean` into the `postgres` container, then start the API again.

## One-time setup

S03 and a repository administrator do this once, in this order.

### On the VM (S03)

1. The production VM is `nf-hotel` (Proxmox VMID 100, Debian 13, `nf-hotel.taildb84b5.ts.net`); its host key `SHA256:Ea/3dv5VefOgI6rlseLuRBtDBmB28zebBp2hZsXSi1w` was verified over the tailnet (DSA-001 section 2.4.2, ADR-0010).
   - **It already runs NF Hotel** from `/opt/nfhotel/deployment/compose.yaml`, which is not in git. **Do not stop or replace anything on it** before the deployment is reconciled with the repository (MIL-021 task 10, ADR-0010). Until then the deploy command refuses to run.
   - The reconciliation, done by S03 with S01:
     - copy `/opt/nfhotel/deployment/compose.yaml` into a pull request, with every secret replaced by a reference to a git-ignored settings file;
     - map its services to the release images, and its volumes, PostgreSQL included, to names the release keeps;
     - take and test a backup of the database first.
   - Turn off SSH password login (risk R14): set `PasswordAuthentication no` and `KbdInteractiveAuthentication no` in `/etc/ssh/sshd_config.d/10-nfhotel.conf`, then run `systemctl reload ssh`.
2. In `/opt/nfhotel`, run `sudo bash scripts/install-deploy-user.sh --apply` (without `--apply` it only shows what it would do). It creates the user `deploy`, the deploy command and its single sudo rule, then prints the Tailscale steps.
3. Run `sudo tailscale set --ssh`.
4. On your own machine, create the backup key with `age-keygen -o nfhotel-backup.key`. Keep it offline, and never copy it to the VM.
5. Prepare the off-VM copy on the Proxmox node (decision D7). The copy is write-only and limited to one folder.
   - On the Proxmox node, create the user and its folder:

     ```bash
     useradd -m -s /bin/sh nfhotel-backup
     install -d -o nfhotel-backup -m 700 /var/lib/nfhotel-backups
     ```

   - On the VM, create the key for the copy:

     ```bash
     sudo ssh-keygen -t ed25519 -N '' -f /root/.ssh/nfhotel-backup
     ```

   - On the Proxmox node, add that public key to `~nfhotel-backup/.ssh/authorized_keys` with the prefix `command="rrsync -wo /var/lib/nfhotel-backups",restrict `.
   - On the VM, pin the host key of the Proxmox node for root. Its fingerprint is `SHA256:jMKkCdkKIuQLFYel6OZcY1uOTj3LA49yTVeRR2JiPDI`, verified over the tailnet:

     ```bash
     sudo sh -c 'ssh-keyscan -t ed25519 100.112.20.37 >> /root/.ssh/known_hosts'
     ```

   - In Proxmox, add a weekly `vzdump` backup job for the VM to a storage other than its own disk.
6. On the VM, install the backup:

   ```bash
   sudo bash scripts/install-backup.sh --recipient <age1... public key> --target nfhotel-backup@100.112.20.37: --apply
   ```

   Then test it once with `sudo systemctl start nfhotel-backup.service`.

### In Tailscale (S03)

1. Give the VM the tag `tag:nfhotel-vm` (Machines, Edit ACL tags).
2. Add to the tailnet policy:

   ```json
   "tagOwners": { "tag:ci": ["autogroup:admin"], "tag:nfhotel-vm": ["autogroup:admin"] },
   "grants":    [ { "src": ["tag:ci"], "dst": ["tag:nfhotel-vm"], "ip": ["tcp:22"] } ],
   "ssh":       [ { "action": "accept", "src": ["tag:ci"], "dst": ["tag:nfhotel-vm"], "users": ["deploy"] } ]
   ```

3. Create a trust credential (workload identity federation) for GitHub Actions:
   - scope `auth_keys`;
   - tag `tag:ci`;
   - limited to the repository `NF-Hotel/NFHotel` and the environment `production`.

   Note its client ID and audience.

### In GitHub (repository administrator)

1. **Settings, Environments, New environment `production`:**
   - required reviewers (for example `codemikemike` and `Tirsvad`);
   - tick "Prevent self-review";
   - deployment branches: selected branches, `main`.
2. **Environment variables of `production`:**
   - `TS_OIDC_CLIENT_ID` and `TS_OIDC_AUDIENCE` (from Tailscale);
   - `NFHOTEL_VM_HOST` (the tailnet name of the VM, for example `nf-hotel`).

   No secret is needed.
3. **After the first release, make the packages public (decision D2):**
   - Packages, each of the four images, Package settings, Change visibility.
   - Link each package to this repository if it is not linked.
4. **After PR #22 is merged (decision D5),** edit the ruleset `Approve merges`:
   - require the status checks `Build, tests og sikkerhed`, `Formatering` and `Docker-build`;
   - dismiss stale approvals;
   - require approval of the most recent push;
   - restrict deletions.

## Which tool checks what

| Gate | Where | Blocks |
| --- | --- | --- |
| Build, tests, NuGet audit, formatting | `NF Hotel CI` | the merge (once the checks are required) |
| Secret scanning with push protection, Dependabot alerts, CodeQL | repository settings | a push with a secret; alerts are reviewed |
| Approval | environment `production` | the deploy |
| Health and smoke test | `nfhotel-deploy` on the VM | the release; it rolls back |

Phase 2 (MIL-022) adds Trivy, SBOMs, build provenance and its verification before the deploy. Phase 3 (MIL-023) adds the hardening of the VM.
