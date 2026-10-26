# ADR-0030 A patch layer on the frozen market-analysis image

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0030 |
| CrossReference | [ADR-0500], [ADR-0007], [MIL-022], [MIL-036], [MIL-042] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-10-26 for a patch layer on the frozen image with certifi 2026.10.19 and the Debian security updates, built by the release; written by the AI under MIL-042, which S01 accepted in chat on 2026-10-26; accepted after RC-223 Go | [4faf283] |
| 2026-10-26 | Accepted | mikemike | AI as stand-in for S01 | Decision 2: the layer also removes the Copilot language server that marimo bundles (zod, CVE-2023-54404), found by the release check; no Node.js runs it | [070484b] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who decided what.** S01 chose on 2026-10-26, in chat, to put a patch layer on the frozen image instead of removing the service or accepting the finding. The design of this record was written by the AI assistant.

ADR-0500 froze `market-analysis` at `ghcr.io/nf-hotel/market-analysis@sha256:6bd53d9e59afe80bb3104cebd7bb6e9e03cd1617ea9b1eb89d449968b377a093`, because its source repository was deleted and the image can no longer be built. The release carries that digest unchanged, and its Trivy scan only reports (ADR-0500, consequences).

The scan of 2026-10-25 reports one critical finding with a fix in that image: GHSA-6q48-6c7w-9gw2, `certifi 2026.6.17`, which trusts root certificates whose intermediate keys were compromised; fixed in `2026.10.19`. Business Case criterion 27 asks for 0 critical and 0 fixable high findings in the deployed images, so the frozen image now breaks it. The other images pass.

Options considered:

1. **Accept the finding** in `ops/security/trivyignore.yaml`. It expires after 30 days at most, and the service keeps trusting the compromised certificates.
2. **Remove the service** (ADR-0500 option 2). The API and the history notebook stop, and the release manifest, the deploy and their tests change shape.
3. **A patch layer.** A Dockerfile starts `FROM` the frozen digest, installs the Debian security updates and a hash-pinned `certifi 2026.10.19`, and changes nothing else. The application files stay byte for byte those of the frozen image.

## Decision

We follow option 3. It ends the "no rebuilds" part of ADR-0500; the frozen digest stays, as the base.

1. **The base stays frozen.** `ops/deploy/market-analysis/Dockerfile` starts `FROM` the frozen digest, so the application is still exactly the one ADR-0500 kept. The file `frozen-image` is removed, and the release builds the image like the other three.
2. **What the layer changes.** As root, `apt-get upgrade` for the Debian security updates, then `pip install --require-hashes` from `requirements-patch.txt`, which pins `certifi==2026.10.19` with its hash. It removes `marimo/_lsp/copilot`, the Node.js packages of the Copilot language server of marimo's editor: the image has no Node.js and runs the notebook with `marimo run`, so they never run, and their `zod` carries a fixable high finding. Then the user is `app` again. The command, the entry point, the user, the working folder and the hardening of the compose file do not change.
3. **The full release path.** The image is scanned by the release gate (a critical finding, or a high one with a fix, stops the release), gets an SBOM and a build provenance attestation, and CD verifies them like the others. CD no longer accepts the frozen pin for `market-analysis`.
4. **Access to the base.** The base is a private package in GHCR. The release already logs in; the release check of a pull request logs in with its read-only job token (`packages: read`) to pull it.
5. **Next findings.** A Python package with a fixable finding is added to `requirements-patch.txt` with its hash; Dependabot proposes newer versions of the pinned packages. A Debian finding is fixed by the next release, which upgrades again.
6. **The base must stay.** The frozen digest must not be deleted from GHCR, as ADR-0500 says.

## Consequences

**Positive:**

- The critical certifi finding and the fixable Debian findings leave the deployed image, so criterion 27 holds again.
- The service, its settings and the deploy stay the same.
- The image gets the same attestations and checks as the other three.

**Negative:**

- A new fixable high finding in a Python package of the frozen application now stops the release of every image until it is pinned in `requirements-patch.txt` or accepted for 30 days. ADR-0500 avoided that coupling; we accept it for the security gain.
- A newer pinned package could break the frozen application, whose own tests are gone. The deploy's smoke test and the health checks of both services catch a service that does not start; a wrong result would not be caught.
- The pull request check needs `packages: read`.

**Quality:** security improves (the known critical finding and the fixable Debian findings leave the image); reliability of the release path is slightly lower, because one more image can stop it (ISO/IEC 25010:2023).

## Affected Artifacts

- [ADR-0500]: the "no rebuilds" part ends; the frozen digest stays, as the base of the layer.
- [ADR-0007]: the release builds all four images again.
- [MIL-042]: tasks 1 to 5.

---

[ADR-0500]: ./adr-0500-freeze-market-analysis-image.md
[ADR-0007]: ./adr-0007-cicd-and-supply-chain.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[MIL-036]: ../milestones/mil-036-security-operations.md
[MIL-042]: ../milestones/mil-042-patch-market-analysis-image.md
[4faf283]: https://github.com/NF-Hotel/NFHotel/commit/4faf2836048366b52d0f6b4e995153384f6bb908
[070484b]: https://github.com/NF-Hotel/NFHotel/commit/070484b6e8d0d987c38a150633acbc3b5700feb6
