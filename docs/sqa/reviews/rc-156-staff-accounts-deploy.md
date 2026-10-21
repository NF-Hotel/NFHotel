# RC-156 Review of the deploy files and the wiki for staff accounts (MIL-030)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-156 |
| CrossReference | [MIL-030], [ADR-0017], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [ea8a47f] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `deploy/nfhotel-api/nfhotel-api.env.example`: the new setting `Staff__InitialManager__MustChangePassword` and the instruction to delete the initial password after the first change;
  - `scripts/dev-up.sh`: writes `MustChangePassword=false` for the throw-away local stack;
  - `docs/wiki/Proxmox-Deployment.md`: the section "Staff accounts and passwords", the break-glass command and the first sign-in text.
- **Checklist used:** [QC-SH-001].
- **Scope:** full review of the changed lines.
- **Language and domain:** n/a (technical type).
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:** `bash -n scripts/dev-up.sh`; the secret scan by reading the diff.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH syntax | The script parses | Pass | `bash -n` reports nothing |
| SH quoting | Values are quoted, no secret on a command line | Pass | The reset command takes the password from `STAFF_PASSWORD` in the environment, as `create-staff` does; the example uses placeholders |
| SH idempotence | A second run changes nothing | Pass | The `sed` line matches a key and sets it to the same value |
| SH secrets | No secret in git | Pass | The env example holds no value; the real file is git-ignored |
| SH scope | A change reaches only the throw-away stack | Pass | `dev-up.sh` writes only the local dev environment file |
| DOC accuracy | The wiki matches the code | Pass | The command name, the setting name and the rules are those of `ApiStartup` and ADR-0017; the page names are those of the menu |
| DOC secrets | The wiki holds no real value | Pass | Placeholders only |

## Findings

- **The CD of NF-Hotel/DevOps needs no change for the code.** The migration runs at the start of the API (`Database__MigrateOnStart`), as for MIL-016 and MIL-019.
- **The VM environment file is changed by hand** (delete the password line after the first change). That is a step of the owner, listed in MIL-030 task 9.

## Overall Verdict

Go: every Mandatory criterion passes.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Delete `Staff__InitialManager__Password` from the VM environment file after the first change | S01 | Task 9 |

---

[MIL-030]: ../../milestones/mil-030-staff-accounts-and-password.md
[ADR-0017]: ../../adr/adr-0017-staff-passwords-and-audit.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[ea8a47f]: https://github.com/NF-Hotel/NFHotel/commit/ea8a47fc392a25ce295dae854d1727f8c8934a95
