# RC-131 Review of the MIL-022 wiring in NFHotel

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-131 |
| CrossReference | [QC-WF-001], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-20 | Accepted | mikemike | AI as stand-in for S01 | Initial version | [53ab6fb] |

---

## Artifact Under Review

- **Instances reviewed:**
  - `Dockerfile` and `Dockerfile.api`: base images pinned by tag and digest;
  - `.github/workflows/ci.yaml`: every action pinned by commit SHA;
  - `.github/dependabot.yml` (new): `nuget`, `docker` and `github-actions`;
  - `.github/workflows/release.yaml`: `id-token`, `attestations` and `security-events: write`, `upload-sarif`, and `migrations.sql` in the release artifact;
  - `.github/workflows/cd.yaml`: `attestations: read` and `packages: read`;
  - every DevOps action pin moved to `b63fbed` of NF-Hotel/DevOps#13 (the combined head of #1, #3, #4, #8, #9 and #11).
- **Checklists used:** [QC-WF-001], [QC-YML-001].
- **Mode:** autonomous, not independent (reviewer is `AI as stand-in for S01`). MIL-022 is accepted (RC-090); this is its NFHotel part, tasks 1 to 4 and 6.
- **Tools:** actionlint 1.7.7 and yamllint 1.35.1. The images were checked with `docker buildx imagetools inspect`; `aspnet:10.0.13` has the same digest as `aspnet:10.0` today.

## Checklist Results

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| WF 4 | Header comments | Pass | Each new permission and the artifact say why, with the MIL-022 task |
| WF 7 | Least permissions | Pass | Write permissions only in the release job, which signs and pushes. CD only reads the attestations and the packages. The workflow level stays `contents: read` |
| WF 8 | Actions pinned by full SHA | Pass | No `uses:` without a 40-character SHA is left in `.github/workflows` (checked by a search) |
| WF 22 | Verified on the instance that runs it | Fail | Not yet: the release with attestations runs first on the next push to `main` after the merge |
| YML 1–3 | Parses, yamllint | Pass | 0 findings |
| YML 11 | Header comment | Pass | `dependabot.yml` states its purpose and the cooldown |

## Findings

- **Order of the merges.** The pins name a commit of NF-Hotel/DevOps#8, which is stacked on #4, #3 and #1. A commit stays reachable after a merge commit, so merging this PR first works. After the DevOps merges, Dependabot moves the pins to `main` of NF-Hotel/DevOps.
- **The first deploy after the merge** checks the attestations, so an older release without attestations is refused by CD. A rollback on the VM itself (`nfhotel-deploy rollback`) does not need them.
- **Before migrations by CD work,** S03 installs `nfhotel-deploy` again from the DevOps checkout (`scripts/install-deploy-user.sh`). Until then the VM's command has no `migrate`, and a release with a migration script fails at the migrate step. Nothing is deployed in that case.

## Overall Verdict

Go-with-conditions: QC-WF-001 criterion 22 closes with the first release and deploy after the merge.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Install `nfhotel-deploy` again on `nf-hotel` after NF-Hotel/DevOps#8 | S03 | Before the next deploy |
| Record the first release with attestations and the first CD that verifies them | S01 | MIL-022 task 7 |

---

[QC-WF-001]: ../../../framework/qc/code/qc-workflow.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[53ab6fb]: https://github.com/NF-Hotel/NFHotel/commit/53ab6fbb63acce4e76792739a84149ff41070619
