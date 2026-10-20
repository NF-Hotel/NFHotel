# RC-145 Review of the removal of the infrastructure copies from NFHotel (MIL-025 task 7)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-145 |
| CrossReference | [MIL-025], [ADR-0012], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed:**
  - the removal of `deploy/`, `scripts/create-docker-vm.sh`, `deploy-services.sh`, `deploy.sh`, `make-dev-certificate.sh`, `pve-token.sh`, `run-proxmox-tests.sh`, `test-compose-proxy.sh`, `pve.env.example`, `deploy.env.example` and `tests/NFHotel.Proxmox.Tests` (with its entry in `NFHotel.sln`);
  - `scripts/dev-up.sh` and `scripts/dev-up.ps1`, which now run the deployment of an NF-Hotel/DevOps checkout;
  - `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `.github/CODEOWNERS`, `.gitignore`, the wiki pages Home, Local Development Deploy, Proxmox Deployment and the sidebar, and one code comment in `MarketAnalysisServiceOptions.cs`.
- **Checklists used:** [QC-SH-001], the rows that the change touches.
- **Scope:** delta review. The DevOps side of the task is reviewed in RC-507 of NF-Hotel/DevOps.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `dotnet build` and `dotnet test` of `NFHotel.sln` (Release), `dotnet format whitespace --verify-no-changes`, `scripts/check-comment-language.sh` and its test, `scripts/test-resolve-model-name.sh`, `bash -n` and `dev-up.sh --check` (with a current DevOps checkout, a missing one and one without `--app-dir`), `dev-up.ps1 --check` with and without `-DevOpsDir`.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–2 | Shebang, strict mode, quoting, `[[ ]]` | Pass | The new code in `dev-up.sh` follows the file: quoted paths, `[[ ]]`, a function for the one-time copy |
| SH 4 | shellcheck and bash -n | Pass | `bash -n` is clean; the changed lines use no construct that shellcheck flags in the rest of the file |
| SH 5 | Errors and usage | Pass | A missing or too old DevOps checkout is a PROBLEM line of the check, with the command that fixes it; the header documents `NFHOTEL_DEVOPS_DIR` |
| SH 7 | No secret on a command line or echoed | Pass | Old settings files are copied with `cp -p`, never printed; only their paths are shown |
| SH 8 | Explicit flag for external changes | Pass | Unchanged: the script changes only this machine; nothing is pushed or deployed elsewhere |
| SH 9 | Header comment | Pass | The header names the DevOps checkout, its default place and the copy of old settings files |
| SH 11 | Behaviour is tested | Partial | The check was run in all three states. The full run (Proxmox VE container, VM, seven services) was not run on this machine, because a 12 GiB VM would take the Docker memory that other work uses. `deploy.sh --app-dir` of DevOps was tested for its arguments and its dry run |

## Findings

- **Nothing that runs in production changes.** Production runs the DevOps checkout and the released images (CD run 39407807571 deployed from DevOps `24d979e`). NFHotel's own changes to its copies after `0f69a52` are merged into DevOps in NF-Hotel/DevOps#9 first, so nothing is lost by the removal.
- **The development deploy needs a DevOps checkout with `--app-dir`.** That option arrives with NF-Hotel/DevOps#9. Until it is on DevOps `main`, `dev-up.sh --check` says that the checkout is too old. So this pull request merges after NF-Hotel/DevOps#1 and the stack on it, up to #9.
- **Old local settings files keep working.** A clone made before this change still holds `deploy/*/*.env`, `pve.env` and `deploy.env`, with the passwords of the database in the existing VM. `dev-up.sh` copies each such file into the DevOps checkout once, when the checkout does not have it yet. `.gitignore` now ignores the whole `/deploy/` folder, so the old files cannot be committed.
- **`scripts/resolve-model-name.sh` stays** with its test, as asked. Its only caller here was `deploy-services.sh`, so it now has no caller in this repository. DevOps has its own copy.

## Overall Verdict

Go-with-conditions. Build, tests and format pass. QC-SH-001 criterion 11 is partial until a full `dev-up.sh` run with the DevOps checkout of NF-Hotel/DevOps#9.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `bash scripts/dev-up.sh` once with a DevOps checkout of NF-Hotel/DevOps#9 (or later) and record the result in the pull request | S01 | Before merge |
| Merge after NF-Hotel/DevOps#1 and the stack up to NF-Hotel/DevOps#9 | S01 | With the review |
| Decide whether `scripts/resolve-model-name.sh` and its test stay here without a caller | S01 | When convenient |

---

[MIL-025]: ../../milestones/mil-025-devops-repository-consolidation.md
[ADR-0012]: ../../adr/adr-0012-devops-repository-ownership.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
