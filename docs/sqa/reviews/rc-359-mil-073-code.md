# RC-359 Code review of MIL-073: the pin of the database image

## Metadata
| Key | Value |
| --- | --- |
| ID | RC-359 |
| CrossReference | [MIL-073], [RC-333], [ADR-0040], [ADR-0031], [DSA-001], [MIL-065], [QC-YML-001] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-14 | Accepted | mikemike | AI as stand-in for S01 | Initial version; mode autonomous, not independent; Go-with-conditions: every check of the pull request green | [26634dc] |

---

## Artifact Under Review

- Instance reviewed: the change of [MIL-073] tasks 1 and 2 on the branch `mil-073-database-image-pin`:
  - the pin of `ops/deploy/postgres/docker-compose.yml`, moved from `postgres:16-alpine@sha256:aa6cb33c…`, the build
    of 2026-11-05 with PostgreSQL 16.15, to
    `postgres:16-alpine@sha256:f377d590e5474681d4d7838635031a2b50647923b7abfa803ce9b5ff35d5c663`, the build of
    2026-11-12 with PostgreSQL 16.16;
  - the wiki page Containers-and-Images: the version in the row of `postgres`, and the row of `postgres:16-alpine`
    in the accepted findings of the third-party images.
- Checklist used: [QC-YML-001] (`qc-yaml`). No workflow and no script changed, so `qc-workflow` and
  `qc-programming-shell` do not apply. The ignore file `ops/security/trivyignore.yaml` is unchanged (F2). The wiki page
  has no checklist of its own; it is read against the deliverable of [MIL-073] below.
- Scope: full review of the changed files
- Language and domain: n/a (technical type)
- Language reviewer: none
- Mode: autonomous, not independent (the reviewer is AI as stand-in for S01, who gave the acceptance of the plan of
  2026-11-14 to the AI in chat, "Du godkender for mig", with the merge and the approval of its CD run)

## Checklist Results

### QC-YML-001: `ops/deploy/postgres/docker-compose.yml`

| # | Criterion | Status | Evidence/Notes |
| --- | --- | --- | --- |
| 1 | Parses safely, no duplicate keys | Pass | yamllint (`key-duplicates`); `docker compose config` reads the file and resolves the image to the new digest |
| 2 | Two spaces, LF, one final newline | Pass | yamllint; no CR byte in the changed files |
| 3 | Passes yamllint with its committed configuration | Pass | yamllint 1.35.1 `-s -c ops/.yamllint` in `python:3.13-slim`, as `ops/tests/lint.sh` runs it: clean (F5) |
| 4 | Scalars YAML would coerce are quoted | Pass | The changed scalar is the image reference, a plain string that YAML reads as a string |
| 5 | Plain types only | Pass | |
| 6 | No secret | Pass | A public digest of Docker Hub |
| 7 | Embedded program text of five lines at most | N-A | None changed |
| 8 | A repeated block defined once | Pass | The digest is written in this one file; the daily scan reads the third-party pins from the compose files of the release |
| 9 | Anchors only where supported | N-A | None |
| 10 | Validates against the schema of its tool | Pass | `docker compose config --images`, with an empty settings file and a dummy password file in a temporary folder, prints `postgres:16-alpine@sha256:f377d590…` |
| 11 | A header comment states purpose, tool and decision | Pass | Unchanged header |
| 12 | Key names follow the tool, one case style | Pass | Unchanged keys |
| 13 | Block scalars for multi-line strings | N-A | None changed |
| 14 | Lowercase kebab-case file names | Pass | Unchanged name |
| 15 | A check runs without the real target | Pass | The scan of the image and `test-wiki-coverage.sh` run without the VM |

### The wiki page against the deliverable of [MIL-073]

- **Containers-and-Images** names PostgreSQL 16.16 and the build of the tag of 2026-11-12 in the row of `postgres`,
  and says in the row of its accepted findings that the build of 2026-11-12 has the same `gosu` binary. The tag
  `postgres:16-alpine` is unchanged, so the tag check finds it; `test-wiki-coverage.sh` passes 116 of 116.

## The criteria of MIL-073 that this review can see

| # | Status | Evidence |
| --- | --- | --- |
| 1 | Pass | `ops/scripts/scan-image.sh` on 2026-11-14 (F4): `PASS postgres:16-alpine@sha256:f377d590…: no secret, no critical finding and no high finding with a fix`; 52 packages in the SBOM; after the ignore file 31 medium, 3 low and 2 unknown findings are left, none high. In the image: `krb5-libs` 1.22.3-r0, `libxslt` 1.1.43-r4, `libssl3` and `libcrypto3` 3.5.9-r0. The same scan of the old digest fails with 19 high findings that have a fix (F1) |
| 2 | Pending | This review is Go-with-conditions; the checks of the pull request |
| 3 | Pending | Task 3, after the merge: the rows before and after the deploy, the digest, the version and the backup |
| 4 | Pending | The next daily scan after the deploy, and issue #642 |
| 5 | Pass | The wiki page above |

## Findings of the review

- **F1, note: more findings than the plan named.** The report of 2026-11-13 named seven high findings in `krb5-libs`.
  With the vulnerability database of 2026-11-14 the old build has 19 high findings with a fix: 16 in `krb5-libs`
  1.22.2-r1, fixed in 1.22.3-r0, and 3 in `libxslt` 1.1.43-r3, fixed in 1.1.43-r4. The new build has both fixed
  packages, so none of the 19 is left.
- **F2, note: `gosu` is the same binary.** `/usr/local/bin/gosu` has the same SHA-256 (`52c8749d…`) in both builds:
  `gosu` 1.19, built with Go 1.24.6. The 26 entries of [MIL-065] still match it, their statements of 2026-11-08 still
  hold, and their expiry of 2026-12-08 stays; the scan of the new build has no high finding in `gosu`. The ignore file
  is not changed.
- **F3, note: no index needs a rebuild.** The release notes of PostgreSQL 16.16 (2026-11-12) say that no dump and
  restore is needed within 16.x. They ask for a `REINDEX` of btree indexes on `bpchar` columns and of `gist_trgm_ops`
  indexes, a `REINDEX` of GIN and TOAST indexes where corruption is suspected, and a rebuild of C-language extensions.
  Read inside the VM on 2026-11-14, in a read-only transaction: PostgreSQL 16.15, 36 btree indexes, none on a `bpchar`
  column, one GiST index and no GIN index; the extensions are `btree_gist` 1.7 and `plpgsql` 1.0. `pg_trgm` is not
  installed, and `btree_gist` comes with the server in the official image, so the new image brings it rebuilt.
  Nothing is reindexed.
- **F4, note: the scan ran on the PC, and no check of the pull request scans this image.** Trivy 0.76.2 at the digest
  that `scan-image.sh` pins ran in WSL. The default mirrors of its vulnerability database timed out, so the database
  (updated 2026-11-14 07:01 UTC) came from `public.ecr.aws/aquasecurity/trivy-db:2` into the volume
  `nfhotel-trivy-cache`. The Release workflow scans the images that NF Hotel builds; the third-party images are
  scanned by the daily scan only, so criterion 1 rests on this scan and criterion 4 on the daily scan after the
  deploy. Finding 2 of [MIL-073] expected the scan of the pull request to decide.
- **F5, note: the linters ran on the changed file.** `ops/tests/lint.sh` cannot list the files of a worktree from WSL
  (its `git ls-files` sees no repository there), so yamllint ran on the changed file with the version and the
  configuration of `lint.sh`. The job Ops linters runs `lint.sh` itself.
- **F6, note: the database restarts once.** As finding 4 of [MIL-073]: CD recreates the container at the deploy, after
  the backup that `deploy-release.sh` makes first ([ADR-0031]); the data stays in its volume.

## Overall Verdict

Go-with-conditions. Every applicable criterion of [QC-YML-001] passes, and criteria 1 and 5 of [MIL-073] pass. The
conditions: every check of the pull request is green before the merge, and the deploy is checked inside the VM as task
3 says. The reviewer is not independent.

## Action Items

| Action | Owner | Due |
| --- | --- | --- |
| Merge only with every check of the pull request green, after the gate of MIL-079 (#692) | S01 (the AI as stand-in) | At the merge |
| Count the rows of every table inside the VM right before the approval of the CD run, then approve it; after the deploy read the digest, the version, the rows again and the backup in the log of the run | S01 (the AI as stand-in) | 2026-11-14 |
| Read the next daily scan and the state of issue #642 | S01 (the AI as stand-in) | 2026-11-15 |

---

[MIL-073]: ../../milestones/mil-073-database-image-that-passes-the-gate.md
[RC-333]: ./rc-333-mil-073.md
[ADR-0040]: ../../adr/adr-0040-patched-nginx-image-from-the-release.md
[ADR-0031]: ../../adr/adr-0031-state-that-survives-a-deploy.md
[DSA-001]: ../devsecops-assessment.md
[MIL-065]: ../../milestones/mil-065-third-party-image-remedies.md
[QC-YML-001]: ../../../framework/qc/code/qc-yaml.md
[26634dc]: https://github.com/NF-Hotel/NFHotel/commit/26634dc25a842119c706a2011812d891e8d4f3bb
