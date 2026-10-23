# ADR-0022 Publish the GitHub wiki from docs/wiki on every merge

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0022 |
| CrossReference | [ADR-0016], [ADR-0018], [ADR-0020] |
| Language | en |
| Domain | it |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-23 | Accepted | mikemike | AI as stand-in for S01 | Initial version; accepted after RC-181 Go | pending |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

**Who wrote what.** On 2026-10-23, S01 could not find the page Monitoring on the [GitHub wiki](https://github.com/NF-Hotel/NFHotel/wiki). The cause, the options and the design below were written by the AI assistant.

The wiki pages are written in `docs/wiki/`. There they are reviewed in pull requests, checked against the code, and linked from review records. The GitHub wiki, however, is a separate git repository (`NFHotel.wiki.git`), and nothing copied the pages into it. The last copy was made by hand on 2026-10-17.

The result on 2026-10-23:
- five pages were missing: CI-CD, Database Preparation, Host Hardening, Local Development Deploy and Monitoring;
- Home, Proxmox Deployment, Appendix and the sidebar were six days old.

Readers of the wiki got instructions that no longer matched production, and RC-161 asked for traceability between the documents and production.

Options considered:

1. **Copy by hand after a merge.** This is what was done, and it drifted within days. It is also one more manual step, which S01 does not want ("I should not have to do anything manually").
2. **Write in the GitHub wiki only.** The wiki has its own history, but no pull request, no review, no CI and no link to the commit that changed the code. A page could describe a change that was never merged.
3. **Keep `docs/wiki/` as the source, and let a workflow publish it on every merge to `main`.** Pages are reviewed with the code they describe, and the wiki follows within a minute. An edit made in the wiki itself is overwritten, which makes clear where pages are written.
4. **Drop the GitHub wiki, and read `docs/wiki/` in the repository.** There is nothing to sync, but readers lose the wiki's sidebar and short links, and the team already points to the wiki.

## Decision

We follow option 3.

1. **The source is `docs/wiki/`.** A page is changed in a pull request like any other document, and the reviews and links stay in the repository.
2. **The workflow `NF Hotel Wiki` (`.github/workflows/publish-wiki.yaml`) publishes it.** It runs on every push to `main` that changes `docs/wiki/`, and by hand.
   - It runs `ops/scripts/publish-wiki.sh`, which makes the wiki's Markdown pages equal to those of `docs/wiki/`.
   - It adds and changes pages, and removes the pages that `docs/wiki/` no longer has.
   - It makes one commit that names the source commit, and pushes only when something changed.
3. **The token.** The job has `contents: write` on this repository only, which is what GitHub requires to push to its wiki. It uses the job's own `GITHUB_TOKEN`, so no personal token is stored. The token goes to git as an HTTP header in the environment, never in a URL or on a command line, and the checkout keeps no credentials.
4. **No gate on the checks.** A wiki page cannot break production, and a wrong page is fixed by the next merge. The pull request that changed the page was already reviewed and checked.
5. **The wiki's own history stays.** Earlier versions of each page remain in the wiki repository, so a removed or overwritten page can be recovered.

## Consequences

**Positive:**

- The wiki matches `main` within a minute of a merge, with no manual step.
- Every page on the wiki has a reviewed source and a commit, which gives the traceability that RC-161 asked for.

**Negative:**

- **An edit made directly in the GitHub wiki is lost at the next publish.** The page Home says where to edit. The wiki history keeps the lost edit.
- A relative link from a wiki page to a file in the repository (for example `../adr/...`) would not work on the wiki. Such a link must be a full `https://github.com/NF-Hotel/NFHotel/blob/main/...` URL; no page has a relative link today.

## Affected Artifacts

- [ADR-0016] — puts the documents in `docs/`; this decision adds how the wiki part reaches GitHub.
- The wiki page Home says that the wiki is published from `docs/wiki/`.

---

[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[ADR-0018]: ./adr-0018-deploy-command-updates-itself.md
[ADR-0020]: ./adr-0020-monitoring-stack.md
