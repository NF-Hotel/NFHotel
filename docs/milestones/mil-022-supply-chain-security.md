# MIL-022 Software supply chain security

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-022 |
| CrossReference | [BC-001], [ADR-0007], [ADR-0008], [MIL-021], [DSA-001] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01 | Initial version | [f4649ad] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-090 Go | [f4649ad] |

---

## Purpose

Decide whether NF Hotel may claim that only scanned and attested images reach production. This phase adds the following to the pipeline of MIL-021, aiming at SLSA (Supply-chain Levels for Software Artifacts) v1.2 Build Level 2:

- vulnerability scanning;
- a software bill of materials;
- signed build provenance, verified before deploy;
- pinned dependencies;
- static analysis;
- reviewed migrations.

The level is claimed only when criterion 3 holds.

## Deliverable

- **Release scanning.** Trivy scans each pushed digest, with the thresholds of DSA-001 section 8, and the results go to code scanning as SARIF.
- **SBOM.** An SPDX software bill of materials (SBOM) is made for each digest and attested.
- **Provenance.** Each digest gets a build provenance attestation (`actions/attest`). CD verifies it with `gh attestation verify` and refuses an unverified digest.
- **Pinning.**
  - All third-party actions are pinned by commit SHA, and the Actions policy requires SHA pinning.
  - Base and third-party images are pinned by digest: .NET SDK and runtime, Python, nginx, postgres and ollama (no `latest`).
  - Dependabot is configured for `nuget`, `docker`, `github-actions` and `pip`.
- **Static analysis.** CodeQL default setup runs for C#, Python and Actions, and Trivy runs in CI on pull requests without pushing.
- **Coverage.** The coverage report is sent to SonarQube Cloud.
- **Migrations.** Migrations become reviewed idempotent scripts, applied by CD after a fresh backup, and `Database__MigrateOnStart=false` is set in production (ADR-0008).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | A pull request that adds a known critical vulnerability fails CI (test with a pinned vulnerable package, then reverted) | Yes | No |
| 2 | Every released digest has an SBOM and a provenance attestation listed by `gh attestation list` | Yes | No |
| 3 | `gh attestation verify oci://ghcr.io/nf-hotel/nfhotel-web@<digest> --owner NF-Hotel` succeeds in CD, and CD refuses a digest without one (tested) | Yes | No |
| 4 | Scorecard `Pinned-Dependencies` reports no unpinned action, and no compose file or Dockerfile uses a tag without a digest | Yes | No |
| 5 | CodeQL runs on every pull request, and no open alert of severity high or critical is without a recorded decision | Yes | No |
| 6 | A release with a migration applies the reviewed script after a backup, and the API starts with `MigrateOnStart=false` | Yes | No |
| 7 | Reviews against `qc-yaml` and `qc-adr` have the verdict `Go` | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-021 | The Release and CD workflows that this phase extends |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Objective 24 and success criterion 27 of BC-001 | All tasks |
| DSA-001 risks R3, R4, R5 | All tasks |
| SSDF PS.2, PS.3, PW.4, PW.7, RV.1; SLSA Build L2 | Criteria 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-12-06, three weeks after MIL-021.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Trivy in CI and Release | Scan on PRs without push; scan the digest in Release; thresholds and `.trivyignore` with expiry. | No | |
| 2 | SBOM and provenance | SPDX SBOM and build provenance attestation per digest. | No | |
| 3 | Verify in CD | `gh attestation verify` before deploy; refuse unverified digests; test it. | No | |
| 4 | Pin everything | SHA pins for actions, digests for images, Dependabot for four ecosystems, Actions policy. | No | |
| 5 | CodeQL and coverage | CodeQL default setup; coverage report to SonarQube Cloud. | No | |
| 6 | Migrations by CD | Idempotent script in Release; applied by CD after a backup; `MigrateOnStart` off. | No | |
| 7 | Check and review | Prove criteria 1 to 6; reviews. | No | |

---

[BC-001]: ../business-case.md
[ADR-0007]: ../adr/adr-0007-cicd-and-supply-chain.md
[ADR-0008]: ../adr/adr-0008-database-migration-and-backup.md
[MIL-021]: ./mil-021-secure-cicd-foundation.md
[DSA-001]: ../sqa/devsecops-assessment.md
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
