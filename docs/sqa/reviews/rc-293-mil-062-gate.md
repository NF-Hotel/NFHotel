# RC-293 Gate review of MIL-062 Pruning the images of old releases on the VM

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-293 |
| CrossReference | [MIL-062], [RC-290], [RC-291], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the gate of MIL-062 after the first deploy with the prune | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-062] at its gate: the six Go/No-Go criteria, with the evidence of the pull request #521 (merged as b645ef9 on 2026-11-05 at 17:43 UTC), the CD run 42078898723 that deployed it, the monitoring read over the tailnet and a read-only check of `nf-hotel`. Before the gate: [RC-290] (the plan, Go) and [RC-291] (the code, Go).
- Checklist used: the Go/No-Go criteria of [MIL-062]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: the gate; criteria 2, 3 and 6 cite RC-291 and the green runs.
- Language and domain: en / it (the Metadata rows of MIL-062); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. The AI wrote the milestone, the code, RC-290, RC-291 and this record; S01 asked for the fix of the alert in chat on 2026-11-05 ("Fix."), chose the plan ("Planlæg oprydning"), accepted it ("Accepter planen"), said when the checks were green ("Grøn, merge"), approved the CD run under "Review deployments" (ADR-0038) and approved the read-only check of the VM in chat ("Ja, kør tjekket"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on 2026-11-05:**

- **The pull request.** #521, head 4d4147b: Ops CI run 42076568878 with the job `Ops script tests (test-deploy-release.sh)` (76 checks, the nine of the prune among them), CI 42076568976, E2E and DAST 42076569023 and Release check 42076568870, all green; 49 checks passed and 2 skipped. Merge commit b645ef9; on `main` the same workflows passed again, the Wiki run 42077484134 published the three pages, and the Release run 42078108918 built the images.
- **The deploy.** CD run 42078898723 (b645ef9), created at 17:52 UTC, approved by S01 and run from 18:30:37 to 18:33:21 UTC: the `update` step installed the operations commands of b645ef9 (ADR-0018), so the deploy ran with the new `nfhotel-deploy`; every service deployed, the smoke test answered 301 on 80 and 200 on 443, "deployed b645ef9"; then the last line: "prune: removed 352 image(s) that no container uses and no release of the last two needs; / had 21G free, has 72G". The 352 are the 348 images counted at 17:14 UTC plus the four of the release 90ef30a, which this deploy made the release before the previous one.
- **The monitoring**, over the tailnet from the PC of S01 at 18:46 UTC: Prometheus gives 0.355 for the used share of `/` on `nf-hotel` (`1 - node_filesystem_avail_bytes / node_filesystem_size_bytes`), the expression of the rule `DiskAlmostFull` (the same, `> 0.8`) returns no series, and the Alertmanager API (`/api/v2/alerts?active=true`) lists no active `DiskAlmostFull`.
- **The VM**, read over Tailscale SSH in check mode as `root@nf-hotel` at 18:47 UTC, read-only: `/` has 111 GB, 34 GB used, 72 GB free, 33 % (80.4 % at 17:14 UTC); `docker system df` shows 15 images, 11 active, 11.55 GB; `/var/lib/containerd` is 12 GB (61 GB at 17:14 UTC). The 15 images are the 11 of the containers (the web application, the API, the market analysis, the benchmark with its one-shot service, nginx, PostgreSQL, the relay, Ollama, cloudflared, Alloy and the socket proxy) and the four of the release before the latest one, 0bdcc55; the eight digests of the two latest release lines of `/var/lib/nfhotel-deploy/releases.log` (0bdcc55 at 17:28 UTC and b645ef9 at 18:32 UTC) are all present. `nfhotel-deploy prune` without `--apply`, the installed command: "0 image(s) would be removed (run with --apply); / has 72G free". No secret was read and no device address is copied here.

## Checklist Results

| # | Criterion (MIL-062) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | After a deploy whose smoke test passed, the only images left are those a container uses and those of the two latest releases; proven by the test in Ops CI and on `nf-hotel` after the first deploy with it | Pass | The job `Ops script tests (test-deploy-release.sh)` green on #521 and on `main`; on the VM 15 images: 11 of the containers and the 4 of 0bdcc55, the 8 digests of the two latest releases present, and the dry run of the installed command finds nothing more to remove |
| 2 | A failed deploy with its rollback and a drill remove no image; `prune` without `--apply` removes nothing and prints what it would remove | Pass | The three checks of the test (RC-291); on the VM the dry run printed the count and removed nothing |
| 3 | A failure inside the prune leaves a passed deploy green and is a warning in its log | Pass | The two checks of the test (RC-291); the deploy of b645ef9 removed every image it tried, so no warning was needed |
| 4 | On `nf-hotel` after the first deploy with the prune, `/` is below 80 % and `DiskAlmostFull` is resolved | Pass | 33 % on the VM at 18:47 UTC; 0.355 in Prometheus, the rule's expression empty and no active `DiskAlmostFull` in Alertmanager at 18:46 UTC |
| 5 | The runbook names `nfhotel-deploy prune`, CI-CD describes the step, Proxmox Deployment says what the disk keeps, `test-wiki-coverage.sh` passes | Pass | RC-291 (the pages); the Wiki run 42077484134 published them; the coverage test 91 of 91 locally and `Ops wiki coverage` green on #521 |
| 6 | The code review against `QC-SH-001` is Go, `lint.sh` is clean, the job `Ops script tests (test-deploy-release.sh)` is green | Pass | RC-291 (Go); `Ops linters` and the job green on #521 (run 42076568878) and on `main` |

## Findings

- **One deploy did the cleanup, as planned.** The first deploy with the new command took the disk from 80.4 % to 33 % and the image store from 61 GB to 12 GB; nobody ran the one-line command by hand, and nothing else changed on the VM.
- **The rule holds on the first real run.** The removed count (352) equals the 348 unused images of the morning plus the four of the release that the deploy pushed out of the last two; the previous release 0bdcc55 is still on the VM for `rollback`.
- **The alert resolved by itself.** `DiskAlmostFull` has a `for` of 10 minutes; thirteen minutes after the deploy it was no longer active, and no silence was set.
- **The build cache stays.** `docker system df` still shows 438 MB of reclaimable build cache from before releases were pulled; the prune removes images only. It is small and outside this milestone; `docker builder prune` is housekeeping for S01 if it ever matters.

## Overall Verdict

Go: the six criteria pass on the green runs, the CD run of b645ef9, the monitoring and the read of `nf-hotel` after the deploy. The milestone closes: the issues #519 and #520 and the GitHub milestone #44 close through the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Watch the disk share on the Grafana dashboard once after a few more deploys; the alert stays the guard | S01 | 2026-11-20 |
| Remove the build cache (`docker builder prune`) and the two empty old networks (`nfhotel-backend`, `nfhotel-database`) on the VM, when convenient | S01 | Open |

---

[MIL-062]: ../../milestones/mil-062-prune-old-release-images.md
[RC-290]: ./rc-290-mil-062.md
[RC-291]: ./rc-291-mil-062-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
