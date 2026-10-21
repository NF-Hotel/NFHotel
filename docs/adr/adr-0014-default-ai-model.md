# ADR-0014 Default AI model for the website and the market analysis

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0014 |
| CrossReference | [BC-001], [MIL-020], [ADR-0005], [ADR-0013], [ADR-0016], [ADR-0500], [RC-161] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-19 | Deprecated | Claude (AI) | AI at the request of S01, not independent | Accepted after RC-109 Go | [b1daae2] |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Path under ops/ (RC-161) | [12a64c3] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

The benchmark results of [ADR-0013] say which local model works well on this machine: valid answers, tokens per second and memory. A Manager needs to turn that result into a choice: which model the website uses for its AI features, and which model the market analysis service uses. Today the market analysis picks a model by discovery (`[llm] model = ""` in `market-analysis.toml`), and the website has no AI feature yet that takes a model setting.

Forces: the choice is made by a person with the benchmark in view; the two services run in other containers and must be able to read it; the market analysis service lives in its own repository, which this project does not change; the permission table of [ADR-0005] is the one place that says who may do what.

Options considered:

1. **A setting in an env file** edited by hand. No code, but nobody sees the benchmark next to it, a change needs a deploy by someone with file access, and there is no record of who chose what.
2. **A table with one row per purpose** in the database of the web application, a Manager-only endpoint and a field on the benchmark page, readable by the services over the API.
3. **A setting inside the benchmark service.** The benchmark service would then decide for the other services, and it has no user and no permission model.

## Decision

We follow option 2. The API stores one default model per purpose, `website` and `market_analysis`, and only a Manager sets it.

- **Table.** `ai_default` holds the purpose (unique, `ux_ai_default_purpose`), the model name (at most 100 characters), the user name of the staff member who set it, when, and the benchmark run it was chosen from (or none when it was typed in). Setting a purpose again replaces its row.
- **Permission.** `AiDefaults.Manage` is a row of the permission table, held by the Manager only, with a test for every role.
- **Endpoints.** `GET /api/ai-defaults` needs the service token only, so the market analysis service can read it; it is on the list of public actions of the endpoint protection test, with a comment. `PUT /api/ai-defaults/{purpose}` needs the staff token and `AiDefaults.Manage`. The name of the staff member comes from the staff token, never from the body.
- **Page.** `/admin/benchmark` shows the current defaults with a field that takes any model name (with the models of the stored runs as suggestions) and a button on every result row, "Use for the website" and "Use for the market analysis", which sets the default to the model and the run of that row.
- **Market analysis.** `ops/scripts/deploy-services.sh` writes the saved model as `model` in the `[llm]` section of `market-analysis.toml` just before it starts the analysis service. The service picks the new model up at its next deployment. Reading the default live from the API needs a change in the market analysis repository and is not part of this decision.
- **Website.** The website gets the stored setting and the endpoint. No AI call is added to the website by this decision.
- **Validation.** The purpose must be one of the two known ones, the model is trimmed and must not be blank. The database holds only the unique index; the other rules are in the domain.

**Current state (2026-10-21, [RC-161]).** The script is `ops/scripts/deploy-services.sh` ([ADR-0016]). It also writes the model for the frozen `market-analysis` image of [ADR-0500], which reads the same `market-analysis.toml`.

## Consequences

**Positive:**

- The choice is made next to the evidence, and the record says who chose what and from which run.
- Any service with the service token can read the defaults; no new secret.
- Only a Manager changes them, and the check is in the API, so a changed page cannot bypass it.

**Negative:**

- A model that is not installed can be saved; nothing checks the name against the model server, so the market analysis would then fall back to its own error handling.
- The market analysis uses a new default only after the next deployment of that service.
- Anyone who holds the service token can read the defaults (model names only, no secret).

Quality attributes: usability improves (one place, next to the results); security is kept (least privilege, the check in the API, the user from the token); maintainability is neutral (one more aggregate that follows the existing pattern).

## Affected Artifacts

- [BC-001]
- [MIL-020]
- [ADR-0005]
- [ADR-0013]

---

[BC-001]: ../business-case.md
[MIL-020]: ../milestones/mil-020-start-benchmark-run.md
[ADR-0005]: ./adr-0005-backoffice-login.md
[ADR-0013]: ./adr-0013-benchmark-results.md
[b1daae2]: https://github.com/NF-Hotel/NFHotel/commit/b1daae275e05b207cd5403a3ec47e46fc76f33d0
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[ADR-0500]: ./adr-0500-freeze-market-analysis-image.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
[12a64c3]: https://github.com/NF-Hotel/NFHotel/commit/12a64c3225c75ebc1b83a0e6d0ee08115ec1a493
