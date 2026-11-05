# RC-286 Re-review of MIL-056 after the amendment of criterion 3, and its gate review

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-286 |
| CrossReference | [MIL-056], [ADR-0035], [RC-272], [QC-MIL-001], [QC-LANG-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-05 | Accepted | Claude (AI) | AI as stand-in for S01 | Initial version; mode autonomous, not independent; S01 decided in chat on 2026-11-05 to amend criterion 3 instead of waiting for the run on a Mac by S02 ("ændre til nu", amend it now) | pending |

---

## Artifact Under Review

- Instance reviewed: [MIL-056], twice in one record. First the amendment of 2026-11-05 (criterion 3, task 7 and the macOS tester row) against [QC-MIL-001] and [QC-LANG-001], as a delta re-review of [RC-271], the full review of 2026-11-02 (Go). Then the gate: the eight Go/No-Go criteria, with [RC-272] (the code, Go) and the workflow runs of `main` as evidence.
- Checklist used: [QC-MIL-001] (criterion 2 for the amended criterion, criterion 5 for the tester row; the other four confirmed unchanged) and [QC-LANG-001]; for the gate, the Go/No-Go criteria of [MIL-056], which [QC-MIL-001] criterion 2 makes the checklist of the gate.
- Scope: delta re-review of QC-MIL-001 criteria 2 and 5 and the nine rows of QC-LANG-001, and the gate review of criteria 1 to 8.
- Language and domain: en / it (the Metadata rows of MIL-056).
- Language reviewer: none; the reviewer reads the language and knows the domain.
- Mode: autonomous, not independent. The AI wrote the milestone, the code review and this record. S01 asked in chat on 2026-11-05 to finish the open milestones, accepted the plan ("Accepter planen") and, asked whether to wait for the run on a Mac by S02 until 2026-11-16 or to amend criterion 3, chose the amendment ("ændre til nu"). The reviewer is the AI as stand-in for S01, so every decision stays open to S01's challenge.

## The amendment

| Where | Was | Is |
| --- | --- | --- |
| Criterion 3 | A developer of S02 runs `dev-up.sh --lite` on a Mac (Apple Silicon) and signs in to the back office and to the mobile app against it; the result is written in the review record | The light mode runs on a developer's machine without KVM: the stack starts, the sign-in to the back office and to the routes of the mobile app succeeds, and every image it pulls or builds from has a `linux/arm64` manifest; the result is in the review record. The run on an Apple Silicon Mac by a developer of S02 is recorded at S02's onboarding, outside this gate |
| Task 7 | A developer of S02 runs the light mode on a Mac and signs in; the review record holds the result | The run on a developer's machine without KVM (S01, Windows 11, RC-272) and the code review; the run on a Mac moved to S02's onboarding. The title is kept, so the sync keeps issue #453 |
| Ownership | Tester on macOS: S02 | Tester on macOS, at S02's onboarding, outside the gate: S02 |

Why: no developer of S02 has run the light mode in the three days since the merge, and S01 chose not to hold the milestone until 2026-11-16 for a run that the milestone's own tests cover in part (see the findings). Nothing else of the milestone changed: deliverable, dependencies, traceability, owner, date and tasks 1 to 6 stand as RC-271 reviewed them.

## Checklist Results (QC-MIL-001, delta)

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | A concrete deliverable is defined for every gate | Pass | Unchanged since RC-271 |
| 2 | Explicit Go/No-Go criteria are stated for each gate | Pass | The amended criterion 3 is still a test that runs and a record that can be read: the stack starts, two sign-ins succeed, the manifests of four images exist, and the review record names the machine. It no longer depends on a person outside the project's control |
| 3 | Dependencies on other milestones are explicitly mapped | Pass | Unchanged |
| 4 | Each milestone is traceable to a Business Case objective or KPI | Pass | Unchanged: BC-001, the developers of the guilds (S02) |
| 5 | Milestone owner and approving reviewer are identified | Pass | Owner S01 and approving reviewer S01 unchanged; the tester on macOS (S02) stays, now for the onboarding |
| 6 | Milestone has a defined target date consistent with project constraints | Pass | 2026-11-16 unchanged; the gate closes before it |

## Language and Domain Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `Language` and `Domain` rows exist and are no placeholders | Pass | en and it |
| 2 | BCP 47 code and a domain from the registry list | Pass | |
| 3 | Content in the stated language | Pass | English; S01's words are quoted in Danish in the Version History only, each with its meaning |
| 4 | Register matches the registry (IT Executive English for MIL) | Pass | The amended rows name the machine, the test and the record, as the other criteria do |
| 5 | Domain terms are those of the dictionary, no synonyms | Pass | Light mode, Proxmox mode and back office are used as before |
| 6 | Keys, headings, IDs and statuses in English | Pass | |
| 7 | No translated twin | Pass | One file |
| 8 | A change of language or domain has a Version History row | N-A | Neither changed |
| 9 | A reviewer competent in the domain has confirmed the terms | Pass | S01 chose the amendment in chat on 2026-11-05; the AI, as stand-in, wrote and read the rows |

## Gate: Checklist Results (the Go/No-Go criteria of MIL-056)

Evidence: [RC-272] of 2026-11-02, and the workflow runs of `main` at `a35fb3f` on 2026-11-05 at 09:19 UTC, read with `gh run view` on 2026-11-05: NF Hotel Ops CI run 42001404333 (32 jobs, all green) and NF Hotel E2E and DAST run 42001404280 (green; the full DAST scan is skipped by design).

| # | Criterion (MIL-056) | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | `dev-up.sh --lite` on a clean clone starts the stack, and `https://localhost:8443/admin/login` and `POST https://localhost:8443/mobile-api/api/auth/login` answer, in an Ops CI test on Linux | Pass | The job `Ops light development mode (MIL-056)` of run 42001404333 passed. It runs `ops/tests/test-dev-lite.sh`, which starts the light mode from the checkout and calls the two addresses (RC-272: the site, the sign-in, the mobile sign-in and the rooms through `/mobile-api/`, 401 for a wrong password, 404 for another path) |
| 2 | The light mode runs with 6 GiB of memory for Docker or less, and needs no `/dev/kvm` and no privileged container | Pass | `scripts/dev-lite.sh` states about 3 GiB and `--check` warns below it; `test-dev-lite.sh` fails when a container of the stack is privileged or the start mentions KVM, and passed in the run above and on Windows 11 (RC-272) |
| 3 | (amended) The light mode runs on a developer's machine without KVM, the two sign-ins succeed, every image has a `linux/arm64` manifest; the result is in the review record. The run on a Mac by S02 is recorded at S02's onboarding, outside this gate | Pass | RC-272: `dev-up.sh --lite --seed` on Windows 11 (Git Bash, Docker Desktop) built and started the four services, created the accounts and loaded the seed; `test-dev-lite.sh` there 16 passed, 0 failed, with the sign-in to the back office and the mobile sign-in; `docker buildx imagetools inspect` showed a `linux/arm64` manifest for `dotnet/sdk`, `dotnet/aspnet`, `postgres:16-alpine` and `nginx:1.29-alpine` at their pinned digests; the certificate script was run with LibreSSL 3.8.4, which macOS has. The run on a Mac is the action item below |
| 4 | A CI job on a macOS runner runs `dev-up.sh --check` and the portability check of the scripts, and both pass | Pass | The job `Ops light development mode on macOS (MIL-056)` of run 42001404333 passed: `macos-15`, `--check` and `ops/tests/test-dev-lite-portable.sh`, with the file modes and the bash 3.2 hint that Windows skips |
| 5 | `e2e-stack.sh` and the light mode use the same library for the shared steps, and the end-to-end tests of MIL-027 still pass | Pass | `ops/scripts/lib/stack.sh` is used by `ops/scripts/e2e-stack.sh` and `scripts/dev-lite.sh` (RC-272, QC-SH-001 criterion 15); the job `End-to-end tests` of run 42001404280 passed on `main` on 2026-11-05 |
| 6 | No secret is put on a command line, in the environment of a container or outside the settings folder of the stack, which is readable by its owner only (ADR-0024); only the passwords of the local development accounts are printed, as `dev-up.sh` does | Pass | RC-272, QC-SH-001 criterion 7: the secrets come from `openssl rand` into 0600 files and are mounted as files; the test sends the password to curl in a file; the three development passwords are printed, which this criterion allows |
| 7 | The Proxmox mode works as before: its Ops CI tests pass unchanged | Pass | The job `Ops Proxmox tests (live tests skip)` of run 42001404333 passed; MIL-056 changed the Proxmox mode only in the hints of `--check` (RC-272) |
| 8 | The code review against `QC-SH-001` (and `QC-WF-001` for the workflow) is Go | Pass | RC-272: Go, every Mandatory criterion passes; two pass on condition (the printed development passwords, allowed by criterion 6; the Homebrew bash on the macOS runner) |

## Findings

- **The run on a Mac has not happened.** What covers macOS without it: the `linux/arm64` manifests of every image, the macOS runner job (`--check`, the portability check, bash 3.2, the file modes), the LibreSSL run of the certificate script, and the Docker Compose steps shared with the end-to-end stack. What it does not cover: Docker Desktop for Mac itself, its file sharing and its memory setting on an 8 GiB Mac, and the sign-in from the iOS simulator against `localhost:8443`. S01 took that risk on 2026-11-05 for a tooling milestone; the first run by a developer of S02 closes it, and a failure there is a defect of the light mode to fix, not a reason to reopen this gate.
- **Issue #453 keeps its title.** The sync matches issues by title; the summary changed, the title did not, so the pull request of this record closes #453 as the amended task.
- **The target date holds.** 2026-11-16 was set for the run on a Mac; the gate passes eleven days before it.

## Overall Verdict

Go. The amendment keeps QC-MIL-001 criterion 2 (an objectively checkable criterion) and changes no deliverable, dependency, owner or date; every Go/No-Go criterion passes on the evidence of RC-272 and the green runs of `main` on 2026-11-05. The Version History row of 2026-11-05 of MIL-056 becomes `Accepted` and the row of 2026-11-02 `Deprecated`; the GitHub milestone closes with the pull request of this record. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `bash scripts/dev-up.sh --lite --seed` on an Apple Silicon Mac, sign in to the back office and from the mobile app, and record the result in the onboarding note or a new review record; a failure is reported as an issue against the light mode | S02 | At S02's onboarding to the light mode; no date, by S01's decision of 2026-11-05 |

---

[MIL-056]: ../../milestones/mil-056-dev-stack-without-kvm.md
[ADR-0035]: ../../adr/adr-0035-light-local-development-stack.md
[RC-271]: ./rc-271-mil-056-dev-stack-without-kvm.md
[RC-272]: ./rc-272-mil-056-code.md
[QC-MIL-001]: ../../../framework/qc/artifacts/qc-milestones-gateways.md
[QC-LANG-001]: ../../../framework/qc/artifacts/qc-language-domain.md
