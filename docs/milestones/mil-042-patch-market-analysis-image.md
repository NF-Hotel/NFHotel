# MIL-042 A patch layer on the frozen market-analysis image

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-042 |
| CrossReference | [BC-001], [ADR-0500], [ADR-0030], [MIL-022], [MIL-036] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | S01 | Initial version; S01 asked in chat on 2026-10-26 for a patch layer on the frozen image with certifi 2026.10.19 and the Debian security updates, built by the release, with an ADR that replaces the freeze of ADR-0500 ("Lav et patch-lag oven på det låste image"); S01 accepted it in chat ("Jeg accepterer planen for MIL-042"); accepted after RC-222 Go | pending |

---

## Purpose

Decide whether NF Hotel may claim again that its deployed images have no critical finding and no fixable high one (Business Case criterion 27). The frozen `market-analysis` image (ADR-0500) carries a critical finding with a fix, GHSA-6q48-6c7w-9gw2 in `certifi 2026.6.17`, and cannot be rebuilt from its source. A patch layer on the frozen digest fixes it without changing the application.

## Deliverable

- ADR-0030: a patch layer on the frozen digest, compared with accepting the finding and with removing the service, with the reasons.
- `ops/deploy/market-analysis/Dockerfile` `FROM` the frozen digest, with the Debian security updates and `requirements-patch.txt` (`certifi==2026.10.19`, hash-pinned); `frozen-image` removed.
- The release builds, scans, attests and pushes the image like the other three; the release check pulls the private base; CD verifies its attestations.
- The tests and documents that named the freeze follow: `check-release-build.sh`, `test-cd-steps.sh`, Dependabot, the wiki page CI-CD.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | ADR-0030 records the change and the alternatives, with the reasons | Yes | No |
| 2 | The release log shows `PASS` for `market-analysis` from the gate, with no `report only` line; the Trivy result has no GHSA-6q48-6c7w-9gw2 and no critical finding | Yes | No |
| 3 | The release attests `market-analysis`, and CD verifies its provenance and SBOM | Yes | No |
| 4 | After the deploy, `market-analysis-api` and `market-analysis-notebook` are healthy and `/health` answers 200; the image's `hotel_booking_analysis` files equal those of the frozen digest | Yes | No |
| 5 | The code scanning alerts of `trivy-market-analysis` have no open critical alert after the release | Yes | No |
| 6 | CI and the release check of the pull request are green, and the review against `qc-programming-shell`, `qc-yaml` and `qc-workflow` is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| ADR-0500 | The frozen digest is the base and must stay in GHCR |
| MIL-022, MIL-036 | The gate, the attestations and the daily scan that the image joins |
| The node | The deploy and criterion 4 need `nf-hotel` online |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| Criterion 27 (verified supply chain): 0 critical and 0 fixable high findings in the deployed images; every deployed digest attested | Tasks 2 to 5 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-01.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR-0030 | Review and accept the ADR that ends the "no rebuilds" part of ADR-0500, with the alternatives. | No | criterion 1 |
| 2 | Patch layer | The Dockerfile `FROM` the frozen digest, `apt-get upgrade`, `requirements-patch.txt` with the hash of `certifi 2026.10.19`, back to the user `app`; the compose build without the old source arguments; `frozen-image` removed. | No | criteria 2, 4 |
| 3 | Release, release check and CD | The release check logs in to GHCR with `packages: read`; the release builds, scans and attests the image; CD verifies it; `check-release-build.sh` and `test-cd-steps.sh` without the frozen pin of market-analysis. | No | criteria 3, 6 |
| 4 | Dependabot and documents | A pip entry for `ops/deploy/market-analysis`; the wiki page CI-CD and the README of the folder say how a next finding is fixed. | No | criterion 6 |
| 5 | Release, deploy and check | Merge, follow the release and the deploy, prove the criteria, close the alerts that are gone; reviews. | No | criteria 2 to 6 |

---

[BC-001]: ../business-case.md
[ADR-0500]: ../adr/adr-0500-freeze-market-analysis-image.md
[ADR-0030]: ../adr/adr-0030-patch-layer-on-frozen-market-analysis-image.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-036]: ./mil-036-security-operations.md
