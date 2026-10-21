# ADR-0007 CI, release and CD through GHCR and Tailscale

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0007 |
| CrossReference | [BC-001], [ADR-0003], [ADR-0006], [MIL-021], [MIL-022] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | S01; S03 | Initial version | [f4649ad] |
| 2026-10-18 | Accepted | mikemike | AI as stand-in for S01 | Accepted after RC-092 Go | [f4649ad] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

Today a person deploys by running `scripts/deploy.sh` on a developer machine. The script ships a working tree or a clone, plus the settings files, to the Proxmox VM over ssh, and the VM builds the images (ADR-0003). What runs in production therefore depends on that machine. No image has a digest, a scan or provenance, and continuous integration (CI) is not a required check (DSA-001 section 2).

The team wants deployment to happen only after CI has passed and someone has approved it, without opening a port to the internet. The required standards are NIST Secure Software Development Framework (SSDF) v1.1, SLSA (Supply-chain Levels for Software Artifacts) v1.2 Build L2 as a target, and OpenSSF Scorecard as an indicator (DSA-001 section 3).

Options considered:

1. **Build on the VM** (keep `deploy-services.sh`) and let CD only trigger it. Small change, but the deployed artefact is never the one CI tested, and SLSA provenance is impossible.
2. **Build once in a hosted Release workflow, push to the GitHub Container Registry (GHCR), and deploy by digest.** The VM never builds.
3. **A self-hosted runner in the VM.** No inbound access is needed, but untrusted code would run next to production, and the VM would become a build host.

For access: Tailscale with an OAuth secret, Tailscale with workload identity federation (OpenID Connect, OIDC), ssh to a public address, or a self-hosted runner.

## Decision

We follow option 2, with Tailscale workload identity federation and Tailscale SSH.

1. **Three workflows.**
   - `ci.yaml` runs on pull requests and on `main`, with a read-only token and no secrets.
   - `release.yaml` runs on `main` after CI succeeds. It builds `nfhotel-web` and `nfhotel-api`, pushes them to `ghcr.io/nf-hotel/`, and writes a software bill of materials (SBOM), a Trivy result and a build provenance attestation (`actions/attest`) for each digest.
   - `cd.yaml` starts on a successful Release run, or by hand for an earlier release.
2. **Approval first.** The CD job runs in the GitHub Environment `production`, which has required reviewers and a deployment branch rule for `main` only. No credential is issued before the approval.
3. **Verify before deploy.** CD runs `gh attestation verify` for each digest against the repository and the release workflow. It refuses a digest that has no valid attestation.
4. **Private connectivity.** The CD job joins the tailnet with `tailscale/github-action` v4, using GitHub OIDC (workload identity federation). It becomes an ephemeral node tagged `tag:ci`. The tailnet policy lets `tag:ci` reach only `tag:nfhotel-vm` on port 22. No port is opened to the internet.
5. **Restricted deploy identity.** CD logs in with Tailscale SSH as the user `deploy`, allowed by an ACL `ssh` rule for `tag:ci`. The user may run only `scripts/deploy-release.sh`. No ssh private key is stored in GitHub.
6. **Deploy by digest, no rebuild.** `deploy-release.sh` writes the digests into a git-ignored `deploy/release.env`, then runs `docker compose pull` and `docker compose up -d`. Unchanged services are not recreated, named volumes are never removed, and third-party images are pinned by digest in the compose files.
7. **Health, smoke test and rollback.**
   - Every container must be healthy within 3 minutes.
   - Port 80 must answer 301 and `https://` must answer 200.
   - When a check fails, CD deploys the previous release from the release log and the run fails.
8. **One deploy at a time.** `concurrency: production` without cancelling a run that is in progress.
9. **Least privilege.** Each job declares only the permissions it needs. Third-party actions are pinned by commit SHA and updated by Dependabot. `pull_request_target` is not used.

## Consequences

**Positive:**

- The artefact that runs in production is the one CI built and tested, identified by digest, with provenance aimed at SLSA Build L2.
- No inbound port, stored Tailscale secret or stored ssh key is needed.
- Rollback means redeploying a known digest, and the VM no longer needs build tools or the source of the private framework submodule.

**Negative:**

- GHCR becomes a dependency: an outage blocks deploys, but not the running service.
- Tailscale workload identity federation and Tailscale SSH must be configured by S03. A mistake in the tailnet policy could widen access, so the policy is reviewed like code.
- The Python services and the third-party images still come from upstream builds. Their provenance is out of our control, and pinning by digest only fixes what we run.
- More workflows to maintain.

**Quality:** security, reliability and maintainability improve (ISO/IEC 25010:2023). The deploy takes slightly longer because of the pull and the verification.

## Affected Artifacts

- [MIL-021]: phase 1 implements decisions 1, 2 and 4 to 8.
- [MIL-022]: phase 2 adds the Trivy gate, the SBOM, the attestation and its verification (decision 3), and SHA pinning (decision 9).
- [ADR-0003]: the VM no longer builds images; `deploy.sh` stays for creating the VM.

---

[BC-001]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/business-case.md
[ADR-0003]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md
[ADR-0006]: https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0006-tls.md
[MIL-021]: ../milestones/mil-021-secure-cicd-foundation.md
[MIL-022]: ../milestones/mil-022-supply-chain-security.md
[f4649ad]: https://github.com/NF-Hotel/NFHotel/commit/f4649adfd68aa312e04533632f0f66ee32df245f
