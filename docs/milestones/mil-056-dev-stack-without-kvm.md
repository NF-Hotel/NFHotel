# MIL-056 Local development stack without KVM

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-056 |
| CrossReference | [BC-001], [ADR-0003], [ADR-0015], [ADR-0024], [MIL-027] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-02 | Deprecated | mikemike | S01 | Initial version; a developer of the mobile track (S02) cannot start the API on macOS, because `scripts/dev-up.sh` needs `/dev/kvm`, and S01 asked in chat on 2026-11-02 to plan a way that works ("Ja, planlæg det"); S01 accepted the plan in chat ("Yes, accept and build") after RC-271 Go | [d566246] |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Criterion 3, task 7 and the macOS tester row amended by S01's decision in chat on 2026-11-05 ("ændre til nu", amend it now): the gate takes the run without KVM on the machine of S01 (RC-272) and the `linux/arm64` manifests of the images, and the run on a Mac by a developer of S02 moves to S02's onboarding; accepted after RC-286 Go | pending |

---

## Purpose

Decide whether every developer can run NF Hotel on their own machine, whatever its operating system. Today `scripts/dev-up.sh` is the only local deploy. It is ADR-0003 in miniature: Proxmox VE in a Docker container starts a VM with KVM, and the services run in that VM. That needs `/dev/kvm` inside Docker and about 14 GiB of memory, so it works on Linux and on Windows with WSL 2, but never on macOS: Docker Desktop for Mac has no KVM, and an Apple Silicon Mac cannot start the x86 VM at all. A developer of the mobile track (S02) on a Mac with 8 GiB for Docker gets `PROBLEM /dev/kvm is not available to Docker` and cannot reach the API the app talks to.

The answer is a second, lighter local mode: the containers of the deployed shape (PostgreSQL, the API, the web application and nginx with TLS) run straight in the developer's Docker, with no Proxmox and no VM. `ops/scripts/e2e-stack.sh` (MIL-027) already starts that shape for the end-to-end tests; the light mode reuses its steps and publishes the site on the host. The Proxmox mode stays for those who want the whole chain.

## Deliverable

- **An ADR** that records the light mode beside ADR-0003: what it runs, what it leaves out (Proxmox, the VM, Ollama, the market analysis and the benchmark service), and why the deployed shape (TLS at nginx, the API and the database on internal networks, secrets as files per ADR-0024) is kept.
- **`scripts/dev-up.sh --lite`**: builds the API and the web application from the working tree, starts PostgreSQL, the API, the web application and nginx with Docker Compose, and publishes `https://localhost:8443/` with the back office and `https://localhost:8443/mobile-api/` with the two routes of the mobile app (ADR-0015), the same address the mobile app's debug build uses (`10.0.2.2:8443` from the Android emulator, `localhost:8443` from the iOS simulator). It prints the development accounts, and takes `--seed`, `--down`, `--destroy` and `--check` as the Proxmox mode does.
- **Shared steps, not a copy**: the steps both `e2e-stack.sh` and the light mode need (writing the settings and secret files, the compose calls, creating the staff accounts) move into one library under `ops/scripts/lib/`, used by both.
- **Portable shell**: the scripts the light mode runs use no GNU-only option (`sed -i` without a suffix, `stat -c`, `base64 -w`, `find -printf`, `timeout`, `readlink -f`), and `--check` tells a macOS user how to get bash 4 (`brew install bash`) when the system bash 3.2 runs it.
- **`dev-up.sh --check` points to the light mode** when KVM is missing or Docker has less memory than the Proxmox mode needs, instead of only saying how to fix KVM.
- **Docs**: the wiki page Local Development Deploy gets a macOS column, the light mode and the address the mobile app uses; `README.md` names both modes.

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | `dev-up.sh --lite` on a clean clone starts the stack, and `https://localhost:8443/admin/login` and `POST https://localhost:8443/mobile-api/api/auth/login` answer, in an Ops CI test on Linux | Yes | No |
| 2 | The light mode runs with 6 GiB of memory for Docker or less, and needs no `/dev/kvm` and no privileged container | Yes | No |
| 3 | The light mode runs on a developer's machine without KVM: `dev-up.sh --lite --seed` starts the stack, the sign-in to the back office and to the routes of the mobile app succeeds there, and every image the light mode pulls or builds from has a `linux/arm64` manifest; the result is written in the review record. The run on an Apple Silicon Mac by a developer of S02 is recorded at S02's onboarding to the light mode, outside this gate (S01's decision in chat on 2026-11-05) | Yes | No |
| 4 | A CI job on a macOS runner runs `dev-up.sh --check` and the portability check of the scripts, and both pass | Yes | No |
| 5 | `e2e-stack.sh` and the light mode use the same library for the shared steps, and the end-to-end tests of MIL-027 still pass | Yes | No |
| 6 | No secret is put on a command line, in the environment of a container or outside the settings folder of the stack, which is readable by its owner only (ADR-0024); only the passwords of the local development accounts are printed, as `dev-up.sh` does | Yes | No |
| 7 | The Proxmox mode works as before: its Ops CI tests pass unchanged | Yes | No |
| 8 | The code review against `QC-SH-001` (and `QC-WF-001` for the workflow) is Go | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-027 | `ops/scripts/e2e-stack.sh` already starts the deployed shape without a VM; the light mode reuses its steps |
| ADR-0003 | The deployment that the Proxmox mode copies; the new ADR records why the light mode leaves the VM out |
| ADR-0015 | The routes of the mobile app that nginx publishes under `/mobile-api/` |
| ADR-0024 | Secrets are files, never environment variables, in the light mode too |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001: every developer of the guilds (S02) can build and see a change before it is deployed | Tasks 1 to 7 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |
| Tester on macOS, at S02's onboarding, outside the gate | S02 |

## Target Date

2026-11-16. It changes only developer tooling and documentation, so it does not move a date of the Business Case.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | ADR for the light local mode | Record the light mode beside ADR-0003: the services it runs and leaves out, the deployed shape it keeps, and why Proxmox mode stays. | No | criteria 2, 6 |
| 2 | Shared library of the stack steps | Move the settings, secret-file, compose and staff-account steps of `e2e-stack.sh` into `ops/scripts/lib/`, used by `e2e-stack.sh`; the end-to-end tests still pass. | No | criterion 5 |
| 3 | `dev-up.sh --lite` | Start PostgreSQL, the API, the web application and nginx from the working tree with the shared library, publish `https://localhost:8443/` and `/mobile-api/`, print the development accounts, and take `--seed`, `--down`, `--destroy` and `--check`. | No | criteria 1, 2, 6 |
| 4 | Portable scripts and a better check | Replace the GNU-only options in the scripts the light mode runs, tell a macOS user how to get bash 4, and point `--check` to `--lite` when KVM or memory is missing. | No | criteria 4, 7 |
| 5 | Tests in Ops CI and on macOS | An Ops CI test that starts the light mode and calls the two addresses of criterion 1; a job on a macOS runner that runs `--check` and a portability check (no Docker there). | No | criteria 1, 4, 7 |
| 6 | Docs | The macOS column and the light mode on the wiki page Local Development Deploy, the address of the mobile app, and both modes in `README.md`. | No | criterion 3 |
| 7 | Test on a Mac and code review | The light mode is run on a developer's machine without KVM and the sign-ins from the back office and the routes of the mobile app are checked (S01, Windows 11 with Docker Desktop, RC-272); the review record against `QC-SH-001` and `QC-WF-001` holds the result. The run on a Mac by a developer of S02 moved to S02's onboarding by S01's decision of 2026-11-05; the title is kept so the sync keeps the issue. | No | criteria 3, 8 |

---

[BC-001]: ../business-case.md
[ADR-0003]: ../adr/adr-0003-proxmox-vm-for-services.md
[ADR-0015]: ../adr/adr-0015-mobile-app-staff-token-access.md
[ADR-0024]: ../adr/adr-0024-secrets-as-files.md
[MIL-027]: mil-027-dast-and-end-to-end-tests.md
[d566246]: https://github.com/NF-Hotel/NFHotel/commit/d5662462084d1fd23301de4d0e07db569a182d66
