# RC-303 Re-review of the gate of MIL-064 A patched nginx image and a daily scan of the third-party images

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-303 |
| CrossReference | [MIL-064], [ADR-0040], [RC-302], [RC-292], [DSA-001], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-08 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; the re-review that RC-302 asked for: criterion 2 read with `docker inspect` on the VM at 07:38 UTC under the Tailscale check that S01 approved, and criteria 3 and 4 read again for the digest that runs since the release of 48fbeda | [77f8a2b] |

---

## Artifact Under Review

- Instance reviewed: [MIL-064] at its gate, the second time: criterion 2, which [RC-302] left Pending on 2026-11-08, with the evidence read on the VM `nf-hotel` at 07:38 UTC the same day, and criteria 3 and 4, because the release of 48fbeda replaced the digest that RC-302 read them on. Criteria 1 and 5 to 10 cite RC-302.
- Checklist used: the Go/No-Go criteria of [MIL-064]; [QC-MIL-001] criterion 2 is what makes them the checklist of the gate.
- Scope: delta re-review of criteria 2, 3 and 4; the earlier record is [RC-302] (Go-with-conditions). Criterion 2 is the condition of RC-302; criteria 3 and 4 name "that digest", and the digest changed with the release of 48fbeda (finding 1).
- Language and domain: en / it (the Metadata rows of MIL-064); this record is a technical type.
- Language reviewer: none.
- Mode: autonomous, not independent. S01 asked the AI on 2026-11-08 to decide as S01 in autonomous mode (the request that RC-302 quotes). Later the same day the AI asked in chat "Må jeg starte Tailscale-tjekket, så `docker inspect` kan opfylde kriterium 2 og lukke MIL-064?" (may I start the Tailscale check, so that `docker inspect` can pass criterion 2 and close MIL-064?), and S01 answered "Ja" (yes) and approved the check in the browser. Criterion 2 was Pending in RC-302 because its evidence needed that check, which only S01 can give, not because only a person can judge it: the reading is a fact of the VM. With the check given, the stand-in reads it, as [RC-292] did for the gate of MIL-048. The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

**Evidence read on 2026-11-08** (Tailscale SSH in check mode to `nf-hotel` at 07:38 UTC, under the check that S01 approved just before; read-only: `docker inspect` of the container, `docker image inspect` of its image and `nfhotel-deploy status`, which prints the release log; no tailnet address is copied here):

- **The container.** `docker inspect nfhotel-nginx-nginx-1`: the image reference of the container (`.Config.Image`) is `ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb488240275558b1f8ca98baaacee71cd37f7cf4e69004042f150d`, and its image ID (`.Image`) is the same `sha256:24c658f70acb…`; state `running`, health `healthy`, started at 04:38:42 UTC. `docker image inspect` of that image lists one repository digest, the same reference.
- **The release log.** `nfhotel-deploy status`: the latest line is the release of 48fbeda at 04:38:51 UTC with five references, among them `ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…`; the line before it is the release of b9ab513 at 03:33:35 UTC, the target of a rollback.
- **The release.** Release run 42447081432 of 48fbeda (the merge of #554) logged "PASS  ghcr.io/nf-hotel/nfhotel-nginx:sha-48fbeda…: no secret, no critical finding and no high finding with a fix" at 04:33:24 UTC, pushed the image as `ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…` at 04:34:01, attested its build provenance and its software bill of materials (SBOM) at 04:35 and wrote `release.json`. The `release.json` of its artifact `release-manifest`, downloaded on 2026-11-08, names `"nfhotel-nginx": "ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…"`.
- **The deploy.** CD run 42447431898 of 48fbeda, approved for S01 and finished at 04:38:56 UTC: "==> verifying ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…" at 04:37:06, then "build provenance and SBOM verified", as for the four other digests; the container was recreated at 04:38:42 and healthy at 04:38:48, and `docker compose ps` lists it with the same reference; the smoke test answered 301 on port 80 and 200 on port 443; the log ends "==> deployed 48fbeda…", and the prune removed 4 images that no release of the last two needs.
- **The pull request of 48fbeda.** #554 changed documents and wiki pages only, and its checks `Ops nginx routes`, `Ops script tests (test-cd-steps.sh)` and `Release build (no push)` passed.

## Checklist Results

| # | Criterion (MIL-064) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 2 | The container `nfhotel-nginx-nginx-1` on the VM runs `ghcr.io/nf-hotel/nfhotel-nginx@sha256:<digest>`, the digest in the `release.json` of the CD run of the merge; the deploy log and `docker inspect` on the VM show the same digest | Pass | AI as stand-in for S01, 2026-11-08: `docker inspect` on the VM at 07:38 UTC, the `release.json` of release run 42447081432 and the log of CD run 42447431898 all name `ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…`, and the release log on the VM records it in the running release. The CD run of the merge of #542 deployed `c857c6ad3edb…` (RC-302); the CD run of the merge of #554, the latest merge of MIL-064, replaced it (finding 1) |
| 3 | The release scan of that digest has 0 critical and 0 high findings with a fix (`scan-gate.sh`), against 1 critical and 54 high with a fix in the pinned upstream digest on 2026-11-08; the smoke test of the deploy (port 80 answers 301, HTTPS answers 200) and `test-nginx.sh` pass | Pass | For the digest that runs, `24c658f70acb…`: "PASS … no secret, no critical finding and no high finding with a fix" in release run 42447081432; the smoke test of CD run 42447431898 answered 301 and 200; `Ops nginx routes` green on #554, as on #542 |
| 4 | The verify step of CD accepts the build provenance and the SBOM of that digest with `gh attestation verify`, as for the four other images; a manifest without a valid `nfhotel-nginx` digest is refused, proven by `test-cd-steps.sh` | Pass | CD run 42447431898: "==> verifying ghcr.io/nf-hotel/nfhotel-nginx@sha256:24c658f70acb…", then "build provenance and SBOM verified", as for the four other digests; `Ops script tests (test-cd-steps.sh)` green on #554, as on #542 |

## Findings

- **Every release has a new proxy digest.** The release of each merge to `main` rebuilds the proxy from the pinned upstream digest with `apk upgrade` (ADR-0040), so the digest changes with every release even when nothing in the proxy changed, as the merge of #554 (documents only) shows. Criterion 2 names the digest of "the CD run of the merge"; MIL-064 had two merges, and this record reads the latest, the one that runs. The check is the same for each release: the container runs the reference of the latest `release.json`, and the deploy log and `docker inspect` name it. The merge of this record starts a release of its own, so the VM will run a newer digest after it; this record is the evidence of 07:38 UTC.
- **The daily scan has not read this digest yet.** The scheduled run of `running-image-scan.yaml` (05:17 UTC) had not started by 07:45 UTC; the scheduled runs of 6 and 7 November started six to seven hours late. Criterion 3 rests on the release scan, the gate of each release; the daily scan is the watch afterwards.
- **Two action items of RC-302 stay open.** Decision D2 of [DSA-001] and the choice about the tags in `test-wiki-coverage.sh` are S01's and block no criterion, so they are carried forward below. The third, the Tailscale check, is done.

## Overall Verdict

Go: criteria 2, 3 and 4 pass on the evidence of 2026-11-08 for the digest that runs, and the rows 1 and 5 to 10 of RC-302 still hold, because the merge of #554 changed documents and wiki pages only and its checks passed. The gate of MIL-064 is passed: issue #541 closes through the pull request of this record, and the GitHub milestone #46 is closed after the merge. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Record decision D2 of DSA-001 as taken: private packages that the VM pulls with its own read credential, or make the packages public (from RC-302) | S01 | 2026-11-15 |
| Choose whether `test-wiki-coverage.sh` also checks the tags of the pinned images on Containers-and-Images (from RC-302) | S01 | With the acceptance of MIL-065 |

---

[MIL-064]: ../../milestones/mil-064-patched-nginx-and-third-party-scan.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[RC-302]: ./rc-302-mil-064-gate.md
[RC-292]: ./rc-292-mil-048-gate-re-review.md
[DSA-001]: ../devsecops-assessment.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[77f8a2b]: https://github.com/NF-Hotel/NFHotel/commit/77f8a2be1bf561ad0b42574fd3c980c75018995d
