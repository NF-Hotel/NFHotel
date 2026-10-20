# ADR-0500 Freeze the market-analysis image at its last good digest

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0500 |
| CrossReference | [ADR-0007], [DSA-001], [MIL-021] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Deprecated | mikemike | S01 | Initial version (risk R17, issue #2) | [150af89] |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-502 Go | [150af89] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The release builds four images (ADR-0007). `market-analysis` is built from `NF-Hotel/Notebook_Market_Analysis` at the pinned commit `6c11cbf2a32b7cd2f17b4ebb6384ac458f6b0fb2`.

On 2026-10-20 that repository was deleted, and `NF-Hotel/notebook-market-analysis` was created with a new history. The new project is a FastAPI skeleton, not the `hotel_booking_analysis` package that the compose file runs with `serve`. The pinned commit exists nowhere, so the image can no longer be built (risk R17, issue #2).

Every release therefore fails at that image. That also blocks the web application and the API, which have nothing to do with it.

The last image built from the pinned commit is still in GHCR. Images there are immutable by digest: `ghcr.io/nf-hotel/market-analysis@sha256:6bd53d9e59afe80bb3104cebd7bb6e9e03cd1617ea9b1eb89d449968b377a093`, from the release of NFHotel `00ae08e` (run 39260544072).

Options considered:

1. **Point the service at the new repository.** That is a product decision about a different application, and the compose command would have to change. It is not a pipeline fix.
2. **Take market-analysis out of the release and deploy set.** The deploy then stops running a service that works today, and the release manifest, the deploy command and their tests change shape.
3. **Freeze the image at its last good digest.** The release builds the other three and carries the frozen digest in the manifest, so the VM keeps running exactly the service it would have run.

## Decision

We freeze `market-analysis` at its last good digest until S01 decides the service of the new repository.

1. `deploy/market-analysis/frozen-image` holds the digest above. `build-release-images.sh` does not build an image that has such a file.
2. With `--push`, the build checks that the digest exists in the registry and refuses the release otherwise. It writes the digest into the manifest, so the manifest still names four images and the deploy is unchanged.
3. The CI check `tests/check-release-build.sh` checks that a frozen digest is well formed. The other three images are built and checked as before.
4. The freeze ends with a reviewed pull request that removes the file. That pull request either restores the source (option 1, with a new ref) or removes the service (option 2).

## Consequences

**Positive:**

- Releases of the web application and the API work again.
- The VM runs the same analysis service as before, by an immutable digest.
- Nothing about the deploy changes.

**Negative:**

- The frozen image gets no rebuilds, so no base-image updates and no security fixes, until the freeze ends. MIL-022's Trivy scan will report its findings without a way to fix them by rebuilding.
- The digest depends on GHCR keeping that package version; it must not be deleted while it is frozen.

**Quality:** reliability (releases unblocked) improves. Maintainability and security of that one image stay frozen, which is the stated cost (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0007]: the release builds three images and carries one frozen digest.
- [DSA-001]: risk R17 is treated by this decision until S01 decides.
- [MIL-021]: releases from `main` work again (criterion 2).

---

[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[DSA-001]: ../sqa/devsecops-assessment.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[150af89]: https://github.com/NF-Hotel/DevOps/commit/150af89418a891c0fe10d6da3e519ab54290b7cc
