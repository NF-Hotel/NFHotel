# RC-171 Gate check of MIL-022 (software supply chain security)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-171 |
| CrossReference | [MIL-022], [ADR-0007], [ADR-0008], [ADR-0019], [ADR-0500], [QC-MIL-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instance reviewed:** the seven Go/No-Go criteria of [MIL-022], and the change that closes its last gap.
- **Checklist used:** [QC-MIL-001], the criteria of the milestone itself.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). S01 accepted the plan for the remaining milestones in chat on 2026-10-23.

## Go/No-Go Criteria

| # | Criterion | Result | Evidence |
| --- | --- | --- | --- |
| 1 | A PR that adds a known critical vulnerability fails CI | Go | Proof PR #242 added `System.Text.Encodings.Web` 4.5.0. The required check "Build, tests and security" failed with `NU1904: … has a known critical severity vulnerability` (run 39922467326), and the PR was `BLOCKED`. It was closed unmerged and its branch deleted |
| 2 | Every released digest has an SBOM and a provenance attestation | Go | `GET /orgs/NF-Hotel/attestations/<digest>` for the deployed web (`ff94bf79…`), API (`60a9463d…`) and benchmark (`3848af4e…`) digests: each has `https://spdx.dev/Document/v2.3` and `https://slsa.dev/provenance/v1`. `market-analysis` is frozen, without attestations, and accepted by its pinned digest only (ADR-0500) |
| 3 | CD verifies each digest and refuses one without attestations (tested) | Go | Every CD run logs "build provenance and SBOM verified" per image (for example run 39916108175). `test-cd-steps.sh` checks that a digest with no attestation, or with provenance but no SBOM, is refused |
| 4 | Scorecard Pinned-Dependencies reports no unpinned action, and no compose file or Dockerfile uses a tag without a digest | Go | Every `uses:` in `.github/` is pinned by a full SHA, and every `image:` and `FROM` is pinned by digest. This change pins the last tag, the SDK image of the Proxmox tests (Scorecard alert #605). It also restores that project in locked mode from a lock file with content hashes (#607). The pip install of the frozen market-analysis Dockerfile (#606) is dismissed "won't fix": that file is never built (ADR-0500) |
| 5 | CodeQL runs on every PR, and no open high or critical alert is without a recorded decision | Go | CodeQL runs on every PR (check "CodeQL" and "Analyze (csharp)", "Analyze (actions)"), and has no open alert. The decisions below cover the other high and critical alerts |
| 6 | A release with a migration applies the reviewed script after a backup, and the API starts with `MigrateOnStart=false` | Go | Every CD run logs "backing up the database before the migrations", "applying the migration script <sha256> as nfhotel_migrator", "migrations applied", and "the API connects as nfhotel_app and does not migrate at start" |
| 7 | Reviews against `qc-yaml` and `qc-adr` are `Go` | Go | RC-090 (MIL-022), RC-092 and RC-093 (ADR-0007, ADR-0008), RC-503 (the scan, SBOM and pinning code), RC-170 (ADR-0021 and the gate) |

## Decisions on open high and critical alerts (criterion 5)

| Alert | Tool | Decision | Reason |
| --- | --- | --- | --- |
| #603, #604 DangerousWorkflow (critical) | Scorecard | Dismissed: false positive | `cd.yaml` and `release.yaml` check out `workflow_run.head_sha` only for runs of `main` in this repository. Their `if` excludes forks and other branches, so the code is reviewed `main` |
| #601 BranchProtection (high) | Scorecard | Dismissed: won't fix | No required approver on `main` is S01's decision (ADR-0019). The 19 required checks, no bypass and no direct push remain |
| #613 Maintained (high) | Scorecard | Dismissed: won't fix | The repository is younger than 90 days (created 2026-08-26). This passes by itself after 2026-11-24 |
| High CVEs without a fixed version (for example CVE-2025-69720 in ncurses, CVE-2026-16742 in systemd) | Trivy | Accepted by policy | DSA-001 section 8: the gate fails on critical, and on high with a fix. A high with no fix is reported, and the monthly rebuild (MIL-023 task 4) picks up the fix when Debian ships one |
| CVE-2025-47273 (high, setuptools 70.3.0, fix 78.1.1) | Trivy, `market-analysis` | Accepted, report only | The image is frozen and cannot be rebuilt (ADR-0500). The risk ends with S01's decision on the new market analysis service |

## Findings

- **The comment of `src/NFHotel.Domain/NFHotel.Domain.csproj` is in Danish.** The English-only check reads source files, not project files. It is a small fix for the next change under `src/`.
- **Medium and low Scorecard alerts** (Fuzzing, CII Best Practices, License) are not in the criteria of MIL-022. They stay open as information.

## Overall Verdict

Go. MIL-022 is complete.

## Action Items

None.

---

[MIL-022]: ../../milestones/mil-022-supply-chain-security.md
[ADR-0007]: ../../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../../adr/adr-0008-database-migration-and-backup.md
[ADR-0019]: ../../adr/adr-0019-approvals-by-the-owner.md
[ADR-0500]: ../../adr/adr-0500-freeze-market-analysis-image.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
