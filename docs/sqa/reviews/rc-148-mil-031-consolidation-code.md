# RC-148 Review of the consolidation of NF-Hotel/DevOps into ops/ (MIL-031)

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-148 |
| CrossReference | [MIL-031], [ADR-0016], [QC-SH-001], [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | pending |

---

## Artifact Under Review

- **Instances reviewed (MIL-031 tasks 2 to 6):**
  - the subtree merge of NF-Hotel/DevOps `b63fbed` under `ops/`;
  - the actions moved to `.github/actions/` and their paths to `ops/scripts/`;
  - `action-ref.sh`, which resolves the commit of a local action;
  - `deploy-release.sh`, which accepts an NFHotel checkout with `ops/`;
  - `deploy.sh` and `dev-up.sh` with one working tree;
  - the new `switch-vm-checkout.sh` and its test;
  - `ops-ci.yaml`, `lint.sh`, the four workflows on local actions, CODEOWNERS, Dependabot, `.gitignore`;
  - the documents moved to `docs/` and the living documents rewritten (wiki, README, CONTRIBUTING, `ops/README.md`, the registry);
  - the old copies removed: `deploy/`, the infrastructure scripts, the Proxmox tests and the settings examples.
- **Checklists used:** [QC-SH-001], [QC-WF-001], [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`).
- **Tools:**
  - `ops/tests/lint.sh` (actionlint 1.7.7, shellcheck 0.11.0, shfmt 3.14.1, yamllint 1.35.1), now over `ops/` and `.github/`;
  - the script tests in Debian 12;
  - `dotnet build` and `dotnet format`;
  - the throwaway stack started from `ops/` with this checkout (`ops/scripts/e2e-stack.sh up --app-dir .`), `check-e2e-stack.sh` and the 9 end-to-end tests.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| SH 1–4, 12 | Quoting, shellcheck, shfmt | Pass | 0 findings over `ops/` and `.github/`. The `shellcheck source=` paths now start at the root (`ops/scripts/lib/common.sh`) |
| SH 5 | Errors and usage | Pass | The switch refuses a checkout that is not DevOps, a commit without `ops/`, a second run and a ref that is not a SHA, and prints its usage on exit 2 |
| SH 7 | No secret printed | Pass | The switch copies the settings files with mode 0600 and prints only their count; the test checks that no secret reaches the output |
| SH 8 | Explicit flag for external changes | Pass | `switch-vm-checkout.sh` is a dry run without `--apply`; it backs up first and keeps the old checkout as the way back |
| SH 11 | Behaviour is tested | Pass | test-deploy-release 37 (with the new NFHotel layout), test-cd-steps 45 (`action-ref` rewritten for local actions), test-switch-vm-checkout 13 (new), test-vm-setup 17, test-scan-gate 17, test-dast-rules 14, test-deploy-services-env 2, test-resolve-model-name 14. The stack check passes, and the end-to-end tests pass 9 of 9 from `ops/` |
| SH 15 | Defined once | Pass | One copy of every deploy file. `git ls-tree origin/main deploy` is fully contained in `ops/deploy`, including MIL-029's development route, which came after the DevOps port |
| WF 4 | Header comments | Pass | Each workflow and action names ADR-0016 and what it reads |
| WF 7 | Least permissions | Pass | Unchanged per job. CD checks out the release commit without credentials (`persist-credentials: false`) to read its action |
| WF 8 | Actions pinned | Pass | Every external action by SHA. The four NF Hotel actions are local, read from the commit under test |
| WF 22 | Verified on the instance that runs it | Fail | Not yet: the CI of this pull request runs the workflows first; the first CD needs the VM switch (task 6) |
| YML 1–3 | Parses, yamllint | Pass | 0 findings, now including the NFHotel workflows |

## Findings

- **The order in production.** After this merge, a release names an NFHotel commit. The VM can check it out only after `switch-vm-checkout.sh` has made `/opt/nfhotel` an NFHotel checkout. CD before the switch fails at the checkout, before any container changes. S03 runs the switch first.
- **NFHotel#195** pins the DevOps actions and carries the same permissions, image pins and Dependabot. This branch holds all of its changes on local actions. If #195 merges first, this branch keeps its own versions of the four workflows.
- **`ops-ci.yaml` runs on every pull request,** not only on changes of `ops/`. Its jobs are meant to be required checks, and a path filter would leave them unreported on other pull requests.

## Overall Verdict

Go-with-conditions: QC-WF-001 criterion 22 closes with green CI on this pull request and the first CD after the VM switch.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Run `ops/scripts/switch-vm-checkout.sh <merge commit> --apply` on `nf-hotel` before the first CD | S03 | After the merge |
| Make the `NF Hotel Ops CI` jobs required checks | S01 | After the merge |
| Close NF-Hotel/DevOps #1, #3, #4, #8, #9 and #13 with a link, move #5, #10 and #12, and archive NF-Hotel/DevOps | S01 | MIL-031 task 7 |

---

[MIL-031]: ../../milestones/mil-031-consolidate-devops-into-nfhotel.md
[ADR-0016]: ../../adr/adr-0016-one-repository-for-application-and-operations.md
[QC-SH-001]: ../../../framework/qc/code/qc-programming-shell.md
[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
