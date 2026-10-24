# RC-190 Review of the readable settings of the market analysis (MIL-031 criterion 5)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-190 |
| CrossReference | [MIL-031], [MIL-023], [ADR-0016], [QC-SH-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-24 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [f494ab3] |

---

## Artifact Under Review

- **Instances reviewed:**
  - the analysis step of `ops/scripts/deploy-services.sh`;
  - the settings copy of `ops/scripts/switch-vm-checkout.sh`;
  - their tests in `test-deploy-services-env.sh` and `test-switch-vm-checkout.sh`.
- **Checklists used:** [QC-SH-001], the rows that the change touches.
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). The AI read the cause on `nf-hotel` over Tailscale SSH as root, with S01's permission and approval. Nothing was changed there by hand.

## Why

The market analysis page failed: "The market analysis service refused the bookings" (#206, MIL-031 criterion 5). The service answered every `POST /analyze` with HTTP 422.

A probe on `nf-hotel` sent two valid bookings, and the service refused them too. The full answer gave the reason:

`CONFIGURATION_ERROR: The configuration file hotel_analysis.toml cannot be read: [Errno 13] Permission denied: '/app/work/hotel_analysis.toml'`

Two changes met:

1. **The settings file was made unreadable.** `switch-vm-checkout.sh` (MIL-031 task 6, 2026-10-21) copied every git-ignored settings file with `install -m 0600`, `market-analysis.toml` included. On the VM it is `-rw------- 997:985`, which is the `nfhotel` user.
2. **The service stopped running as root.** The hardening of MIL-023 task 2 runs it as its own user, `app` (uid 1000), with `cap_drop: [ALL]`.

So the service could no longer read its own configuration. The `.env` files are read by compose as root, which is why only the analysis broke.

**The fix:**

- `deploy-services.sh` makes `market-analysis.toml` 0644 before it starts the analysis. The next CD repairs `nf-hotel` with no step by hand.
- `switch-vm-checkout.sh` now copies a `.toml` file with 0644 and the `.env` files with 0600, as before.
- The file holds no secret: the addresses of Ollama and LM Studio, the history size, the timeouts and the model name. Making it readable to others on the VM shows nothing that the repository's example does not.

This is the fourth defect of the same kind since the hardening: the nginx key (#252), the monitoring secrets (RC-180), Alloy (RC-184), and now this one. Each time, a file that root could read under the old setup became unreadable for a container that no longer runs as root. RC-189 already lists a start test per service as the way to find them early. Here the end-to-end stack missed it, because it writes the settings itself from the example, at 0644.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 11 | Behaviour is tested | Pass | `test-deploy-services-env.sh` passes 8: a file at 0600 becomes 0644. `test-switch-vm-checkout.sh` passes 14: the `.toml` is copied with 0644, and an `.env` file still with 0600 |
| SH 5 | Errors | Pass | A failed `chmod` stops the deploy with the path |
| SH 7 | No secret exposed | Pass | The file's keys were checked against `market-analysis.toml.example`; none is a secret |
| SH 12, 4 | shfmt, shellcheck | Pass | `ops/tests/lint.sh` is clean |

## Overall Verdict

Go.

## Action Items

| # | Action | Owner | Due |
| --- | --- | --- | --- |
| 1 | After the CD run, probe `/analyze` on `nf-hotel` again (expect HTTP 200), and run the analysis from the back office (MIL-031 criterion 5) | AI, then S01 in the back office | After the merge |

---

[MIL-031]: ../../milestones/mil-031-consolidate-devops-into-nfhotel.md
[MIL-023]: ../../milestones/mil-023-operational-quality.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[f494ab3]: https://github.com/NF-Hotel/NFHotel/commit/f494ab329f7b402b92e9c0fae17fbfb4068b5155
