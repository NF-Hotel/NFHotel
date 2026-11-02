# ADR-0035 A light local development stack without Proxmox

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0035 |
| CrossReference | [MIL-056], [ADR-0003], [ADR-0015], [ADR-0024], [MIL-027] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; written under MIL-056, which S01 accepted in chat on 2026-11-02 | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

`scripts/dev-up.sh` runs the deployment of ADR-0003 in miniature: Proxmox VE in a privileged Docker container starts a Debian VM with KVM, and every service runs as a container in that VM. It is close to production, but it needs:

- `/dev/kvm` inside Docker;
- about 14 GiB of memory for Docker;
- an x86 processor, because the VM image is x86.

Linux and Windows with WSL 2 have all three. macOS has none of them: Docker Desktop for Mac runs its own Linux VM without nested KVM, and an Apple Silicon Mac is arm64. On 2026-11-02 a developer of the mobile track (S02) could not start the API on a Mac with 8 GiB for Docker, and so could not run the mobile app against a local API.

`ops/scripts/e2e-stack.sh` (MIL-027) already starts the deployed shape without a VM for the end-to-end tests: PostgreSQL 16, the API, the web application and nginx with TLS, each from its compose file in `ops/deploy/`, on their own Docker networks, with secrets as files (ADR-0024). Every image it uses is published for both amd64 and arm64.

**Options considered:**

1. **Make the Proxmox mode run on macOS.** Not possible: Docker Desktop for Mac has no nested KVM, and the VM would need x86 emulation, far too slow for a VM that builds images.
2. **Run the API with `dotnet run` against a PostgreSQL container.** Light, but the developer then sees another shape than production: no nginx, no TLS, no `/mobile-api/` routes, no secret files, and the settings differ from the release.
3. **A light mode that runs the containers of the deployed shape straight in the developer's Docker,** with the steps `e2e-stack.sh` already has, and publishes nginx on the host.

## Decision

We follow option 3.

1. **`scripts/dev-up.sh --lite`** starts PostgreSQL, the API, the web application and nginx from the working tree, with the compose files of `ops/deploy/` as they are deployed. No privileged container, no `/dev/kvm`.
2. **The address is the same as the Proxmox mode's:** `https://localhost:8443/` (the back office) and `https://localhost:8443/mobile-api/` with the two routes of the mobile app (ADR-0015, through `docker-compose.dev.yml` of nginx). nginx publishes on `127.0.0.1` only; the debug build of the mobile app reaches it as `10.0.2.2:8443` from the Android emulator and `localhost:8443` from the iOS simulator.
3. **The deployed shape is kept:** TLS at nginx with a development certificate, the API and PostgreSQL on internal networks with no host port, the hardening of the compose files, and secrets as files under `/run/secrets` (ADR-0024).
4. **Left out:** Proxmox VE and the VM, Ollama, the market analysis service and the benchmark service. The pages that use them show that the service is not reachable; a developer who needs them uses the Proxmox mode.
5. **Settings live in `ops/.dev-lite/`,** git-ignored, readable by the owner only, made once and kept, so the database and the accounts survive a restart. `--down` stops the stack and keeps the data; `--destroy` removes the containers, the volume and the settings.
6. **One library for both stacks:** the steps `e2e-stack.sh` and the light mode share (settings and secret files, the networks, the compose calls, the staff accounts) are in `ops/scripts/lib/stack.sh`.
7. **Portable shell:** the scripts the light mode runs use no GNU-only option, so they run with the BSD tools of macOS; they need bash 4 or later (`brew install bash` on a Mac).
8. **The Proxmox mode stays** for whoever wants the whole chain of ADR-0003. `dev-up.sh --check` points to `--lite` when KVM or memory is missing.

## Consequences

**Positive:**

- A developer on macOS (Intel or Apple Silicon), or on a Windows or Linux machine without KVM or with less than 16 GiB of memory, can run the back office and the mobile API locally in about 3 GiB (portability).
- No privileged container: the light mode does not need the third-party Proxmox image that runs privileged (security).
- The light mode and the end-to-end tests share one library, so a break of the light mode shows in CI.

**Negative:**

- The light mode is less like production than the Proxmox mode: no VM, no host firewall, no deploy scripts of the VM. Those are tested by the Proxmox mode, Ops CI and CD, not by the light mode.
- Two local modes must be kept working.
- Ops CI gets two more jobs: a test of the light mode, and a job on a macOS runner (no Docker there) that runs `--check` and a check of the scripts for GNU-only options.

## Affected Artifacts

- [MIL-056]: tasks 1 to 7.
- [ADR-0003]: unchanged; the Proxmox mode still copies it.
- [MIL-027]: `e2e-stack.sh` now uses the shared library.

---

[MIL-056]: ../milestones/mil-056-dev-stack-without-kvm.md
[ADR-0003]: adr-0003-proxmox-vm-for-services.md
[ADR-0015]: adr-0015-mobile-app-staff-token-access.md
[ADR-0024]: adr-0024-secrets-as-files.md
[MIL-027]: ../milestones/mil-027-dast-and-end-to-end-tests.md
