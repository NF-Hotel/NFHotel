# MIL-060 Dependabot NuGet updates and the lock files of dependent projects

## Metadata
| Key | Value |
| --- | --- |
| ID | MIL-060 |
| CrossReference | [BC-001], [ADR-0038], [MIL-022], [MIL-044] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-11-04 | Accepted | mikemike | AI as stand-in for S01 | Initial version; S01 asked in chat on 2026-11-04, in autonomous mode, to fix the last two Dependabot pull requests (#486 and #489) after seven of the eight of the day were merged; S01 accepted the plan in chat on 2026-11-05 ("Accepter planen") after RC-282 Go | pending |

---

## Purpose

Decide how the two NuGet pull requests that Dependabot left open on 2026-11-04 are closed, and how a grouped NuGet update is checked before it is merged, so that the locked restore of CI (`dotnet restore --locked-mode`, MIL-044) keeps passing.

Dependabot updates the `packages.lock.json` of the project whose `PackageReference` it changes, but not the lock files of the projects that reference that project. Those still pin the old version of the package, which is transitive for them, and the restore fails with NU1004 ("The project references nfhotel.web whose dependencies has changed"). Two pull requests of the day showed it:

- **#486**, the group of 11 updates, added `Microsoft.Extensions.Configuration` 10.0.14 as a new direct reference of `NFHotel.ApiClient` and `NFHotel.External` and left the lock files of `NFHotel.Infrastructure` and the test projects behind. Its ten other updates reached `main` through #487 (`7885c6f`), where 10.0.14 of `Microsoft.Extensions.Configuration` is resolved transitively, so the new direct references are not needed; the pull request is in conflict with `main`.
- **#489**, Markdig 1.4.1 to 1.5.0 in `NFHotel.Web`, left `tests/NFHotel.Web.Tests/packages.lock.json` at 1.4.1; the jobs "Build, tests and security" and "Formatting" fail on the restore. `NFHotel.Web.Tests` is the only project that references `NFHotel.Web`.

## Deliverable

- **Markdig 1.5.0** in `src/NFHotel.Web/NFHotel.Web.csproj` and its lock file (the commit of Dependabot from #489), with `tests/NFHotel.Web.Tests/packages.lock.json` updated by hand to the same version and content hash: three lines, the transitive entry of Markdig and the dependency of the project entry `nfhotel.web`. No lock file is regenerated with a local SDK (the trap the wiki page Local-Development-Deploy describes: a newer patch of the SDK locks other versions of the implicit packages).
- **#486 closed** as superseded, with a comment that says why; **#489 closed** by Dependabot or by S01 once `main` has 1.5.0.
- **A subsection "Dependency updates" on the wiki page CI-CD**: how a Dependabot NuGet pull request is checked (the locked restore), what NU1004 means and the three-line fix, why nothing is regenerated locally, why the SonarQube Cloud check does not report on a run Dependabot triggers (no `SONAR_TOKEN` in a Dependabot run until it is a Dependabot secret; the owner updates the branch meanwhile), and the merge of several updates within minutes so that one Release and one CD run follow (ADR-0038).
- **RC-282**, the traceability matrix, the registry and the pull request of this milestone, with one `Closes #N` per task.

Not in this milestone: the Dependabot secret for `SONAR_TOKEN` (a setting of S01), a change of `dependabot.yml`, a change of the ruleset "Approve merges" (the macOS check is a separate decision of S01).

## Go / No-Go Criteria

| # | Criterion (objectively checkable) | Go | No-Go |
| --- | --- | --- | --- |
| 1 | The jobs "Build, tests and security" and "Formatting" of `NF Hotel CI` pass on the pull request, so `dotnet restore NFHotel.sln --locked-mode` accepts every lock file; `src/NFHotel.Web/packages.lock.json` and `tests/NFHotel.Web.Tests/packages.lock.json` both resolve Markdig 1.5.0 with the content hash of Dependabot's lock file | Yes | No |
| 2 | The diff of the pull request under `src/` and `tests/` is the commit of Dependabot (two files of `NFHotel.Web`) plus exactly three changed lines of `tests/NFHotel.Web.Tests/packages.lock.json`; no other lock file changes | Yes | No |
| 3 | #486 is closed with the reason, and #489 is closed after the merge (by Dependabot, by S01 or as superseded); each of the 11 packages of #486 is at the version it proposed, or newer, on `main` | Yes | No |
| 4 | The wiki page CI-CD has the subsection "Dependency updates" with the four points of the Deliverable, and `bash ops/tests/test-wiki-coverage.sh` passes | Yes | No |
| 5 | The review of MIL-060 (QC-MIL-001, QC-LANG-001) is Go before the first commit under `src/` or `tests/`; the pull request closes the issues of this milestone and is merged with every required check green (the AI merges in autonomous mode, as the stand-in of S01) | Yes | No |

## Dependencies

| Depends on | Reason |
| --- | --- |
| MIL-044 | The locked restore (`packages.lock.json`, `--locked-mode` in CI) that the fix must keep passing |
| MIL-022 | Dependabot as the source of the updates; the pins that must keep moving |
| ADR-0038 | The merge of this milestone gives one Release and one CD run that waits for the approval of S01 |

## Traceability

| Business Case objective / KPI / user story | Reference |
| --- | --- |
| BC-001 objective 24: every dependency pinned (the lock files are the pins of the NuGet packages, and an update moves them without breaking the locked restore) | Tasks 1, 2, 3 |

## Ownership

| Role | Stakeholder ID (SA) |
| --- | --- |
| Owner | S01 |
| Approving reviewer | S01 |

## Target Date

2026-11-06. Three lines of one lock file, one wiki subsection and the review; no date of the Business Case moves.

## Tasks

| # | Task | Summary | Needs its own Use Case/User Story? | Reference |
| --- | --- | --- | --- | --- |
| 1 | Markdig 1.5.0 with the lock file of NFHotel.Web.Tests | Take the commit of Dependabot from pull request #489 (Markdig 1.4.1 to 1.5.0 in `src/NFHotel.Web/NFHotel.Web.csproj` and `src/NFHotel.Web/packages.lock.json`) onto the branch of this milestone, and update `tests/NFHotel.Web.Tests/packages.lock.json` by hand: the transitive entry of Markdig to `1.5.0` with the content hash of the updated lock file of the web project, and the dependency `Markdig` of the project entry `nfhotel.web` to `[1.5.0, )`. Nothing is regenerated with a local SDK, because the SDK of `global.json` is what CI restores with. The locked restore of the jobs "Build, tests and security" and "Formatting" on the pull request is the proof. | No | criteria 1, 2 |
| 2 | Close the superseded pull requests | Close #486 with a comment: its ten other updates reached `main` in #487, Markdig has #489, and `Microsoft.Extensions.Configuration` 10.0.14 is resolved transitively on `main`, so its new direct references, which left the lock files of the dependent projects inconsistent (NU1004), are not needed. If the AI's tool refuses to close a pull request, S01 closes it. After the merge of this milestone, #489 is closed by Dependabot ("no longer needed") or by S01. Check that each of the 11 packages of #486 is at its proposed version or newer on `main`. | No | criterion 3 |
| 3 | Wiki: how a Dependabot NuGet pull request is checked | A subsection "Dependency updates" on the wiki page CI-CD: Dependabot's weekly pull requests; the locked restore as the check; NU1004 as the sign that the lock file of a project that references the updated project was not updated, and the three-line fix copied from the updated project's lock file, never a regeneration with a local SDK; the SonarQube Cloud check that does not report on a run Dependabot triggers until `SONAR_TOKEN` is a Dependabot secret, so the owner updates the branch meanwhile; and the merge of several updates within minutes, so that one Release and one CD run follow (ADR-0038). `bash ops/tests/test-wiki-coverage.sh` passes. | No | criterion 4 |
| 4 | Review, matrix and pull request | RC-282 for MIL-060 (QC-MIL-001, QC-LANG-001) before the first commit under `src/` or `tests/`; the lock-file change and the wiki subsection checked against criteria 1, 2 and 4 and recorded in RC-282; the traceability matrix and the registry; the pull request with one `Closes #N` per task, merged in autonomous mode once every required check is green. | No | criterion 5 |

---

[BC-001]: ../business-case.md
[ADR-0038]: ../adr/adr-0038-approval-before-a-deploy.md
[MIL-022]: ./mil-022-supply-chain-security.md
[MIL-044]: ./mil-044-security-overview.md
