# Technology Choices

**Work:** MIL-050. **Related pages:** [Workflows](Workflows), [Containers and Images](Containers-and-Images), [Security Controls](Security-Controls), [Alerts](Alerts).

The other reference pages explain how each product is set up and why that setup was chosen. This page explains why NF Hotel uses the product at all: why PostgreSQL and not MySQL, Docker and not Podman, Grafana and not Kibana. Each entry gives:

- what we use the product for, and where it is set up;
- the alternatives that were, or could have been, chosen;
- the advantages and drawbacks against those alternatives;
- what practitioners say: the recurring arguments in public discussions, summarised in our own words and linked;
- our own reason, with the ADR that records it, or **Not decided in an ADR** where the product was taken when the project started and no decision record exists;
- when an alternative would be the better choice.

The page follows the eight steps of the DevOps loop (Plan, Code, Build, Test, Release, Deploy, Operate, Monitor). Two sections then go through the CI/CD pipeline and the backup step by step, and a last section covers the security products.

## Sources and how to read them

- **Stack Overflow Developer Survey 2025** ([technology section](https://survey.stackoverflow.co/2025/technology)). The share of all respondents who used a technology in the past year. It shows what most practitioners know, not what is best.
- **Hacker News discussions** (news.ycombinator.com). The threads linked are the most discussed ones about each product. The summary is taken from their highest-ranked comments, and it gives the arguments on both sides; one comment is an opinion, a pattern across threads is a signal.
- **Stack Exchange threads.** Stack Overflow closes most "X or Y" questions as opinion-based, so few comparisons exist there. The answers of the classic threads (for example NUnit or xUnit, cron or systemd timers, environment variables for secrets) are added to the entries as their research is finished (MIL-050 task 1).
- **Reddit** could not be used: its pages refuse the research tools of this project, so no Reddit thread is cited or summarised here.
- **The ADRs** of this repository, for our own reasons. Where an ADR lists options, the entry follows it and links it.

## The choices at a glance

| DevOps step | We use | Main alternatives |
| --- | --- | --- |
| Plan | GitHub Issues and Milestones, Discord | Jira, Linear, Azure Boards; Slack, Microsoft Teams |
| Code | Git and GitHub, C# and .NET, Blazor, Bootstrap, Python, PostgreSQL, Entity Framework Core with Npgsql, Ollama | GitLab, Gitea or Forgejo; Java, Node.js; React, Angular; Tailwind; R; MySQL, SQL Server, SQLite; Dapper; llama.cpp, vLLM, a hosted API |
| Build | GitHub Actions, Docker, GHCR, Microsoft's .NET images, Debian slim and Alpine images | Jenkins, GitLab CI; Podman; Docker Hub; distroless, Chainguard |
| Test | xUnit, Testcontainers, Playwright, k6, SonarQube Cloud, CodeQL, OWASP ZAP, Trivy | NUnit, MSTest; Selenium, Cypress; JMeter, Locust; Codacy; Semgrep; Burp Suite; Grype, Docker Scout |
| Release | Dependabot, GitHub artifact attestations and SBOMs, OpenSSF Scorecard | Renovate; cosign with Sigstore directly |
| Deploy | Docker Compose, Proxmox VE, Debian, nginx, HAProxy, Cloudflare Tunnel, Cloudflare Access, Tailscale, Let's Encrypt | Kubernetes, k3s, Nomad; VMware ESXi, Hyper-V; Ubuntu Server; Caddy, Traefik; port forwarding, Tailscale Funnel; Authelia; WireGuard, OpenVPN, ZeroTier |
| Operate | pg_dump, age, rsync and rrsync, systemd timers, nftables, unattended-upgrades, secrets as files | pgBackRest, Barman; GPG; restic, Borg; cron; ufw, iptables; Vault, SOPS |
| Monitor | Prometheus, Grafana, Loki, Grafana Alloy, Alertmanager, Healthchecks.io, OpenTelemetry, the blackbox exporter | Zabbix, InfluxDB; Kibana; Elasticsearch, OpenSearch; Promtail, Vector, Fluent Bit; PagerDuty; Uptime Kuma, Gatus; vendor agents |
| Security | GitHub secret scanning, the Docker socket proxy, and the products above | gitleaks, TruffleHog; mounting the Docker socket |

---

## 1. Plan

### GitHub Issues and Milestones

- **Used for:** each phase is a `MIL-*` document, and `framework/scripts/sync-project.sh` turns it into a GitHub Milestone with one Issue per task. A pull request closes its issues with `Closes #N`.
- **Alternatives:** Jira, Linear, Azure Boards, a Kanban board in GitHub Projects only.
- **For it:** the plan lives next to the code, so an issue, its commits and its pull request link to each other with no integration; it is free; the sync is scripted, because the API is open.
- **Against it:** weaker reporting, no time tracking and no workflow states beyond open and closed; boards (Projects) are a separate feature.
- **What practitioners say:** Jira is the default in companies, but its most discussed threads are complaints that tickets cut work into pieces nobody sees as a whole, and that the tool rewards process over the goal ([JIRA is an antipattern](https://news.ycombinator.com/item?id=18642336)). Others praise GitHub Issues as simple and searchable enough for far more than code ([GitHub issues is almost the best notebook in the world](https://news.ycombinator.com/item?id=44094980)). In the 2025 survey, 81.1% of respondents used GitHub and 46.4% Jira.
- **Our reason:** the framework of the project plans in `MIL-*` documents and syncs them to the git host (`framework/process/plan-first-gate.md`). Not decided in an ADR.
- **Choose the alternative when:** several teams share one backlog with reports for management, or the work is not in one repository.

### Discord

- **Used for:** the team's chat, and the channel `#nf-hotel-alerts`, where Alertmanager posts every alert ([Alerts](Alerts)).
- **Alternatives:** Slack, Microsoft Teams, e-mail, a self-hosted Mattermost or Matrix.
- **For it:** free with no message limit, the team already used it, and a webhook is enough for alerts (Alertmanager has a native Discord receiver).
- **Against it:** a consumer product with no data processing agreement for a business, no retention control and an account per person; tables in messages are not rendered (one reason the PR bodies use bullets).
- **What practitioners say:** a much-discussed thread argues that chat is a poor place for knowledge, because answers are not searchable and vanish, and that a forum or the issue tracker keeps them ([Use forums rather than Slack/Discord](https://news.ycombinator.com/item?id=29154216)). In the 2025 survey, 38.9% of respondents used Discord.
- **Our reason:** the team's existing channel; the alert route is decided in [ADR-0020](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0020-monitoring-stack.md). The chat tool itself is not decided in an ADR. Decisions are written in the repository, not in chat, which answers the main criticism.
- **Choose the alternative when:** the hotel's staff or a customer must be in the channel (Teams or Slack with an agreement), or an on-call rota needs paging (PagerDuty, see Alertmanager).

## 2. Code

### Git and GitHub

- **Used for:** the repository `NF-Hotel/NFHotel`, pull requests with a ruleset, Actions, the container registry, code scanning, Dependabot and the wiki.
- **Alternatives:** GitLab (hosted or self-hosted), Gitea or Forgejo self-hosted, Bitbucket, Azure DevOps.
- **For it:** one place for code, CI, packages, security findings and the wiki; free for the team; the largest community, so most actions and examples assume it.
- **Against it:** a single vendor for everything (an outage of GitHub stops CI and deploys); the hosted runners and storage are priced by Microsoft; the code is on a US service.
- **What practitioners say:** GitHub is the default (81.1% in the 2025 survey, GitLab 35.6%). People who have used both often rate GitLab's CI higher but GitHub's ecosystem bigger ([The Pain That Is GitHub Actions](https://news.ycombinator.com/item?id=43419701)). A growing group moves to self-hosted Forgejo for independence, and accepts losing the network of users and integrations ([Leaving GitHub for Forgejo](https://news.ycombinator.com/item?id=48121266)).
- **Our reason:** the organisation NF-Hotel was created on GitHub when the project started; [ADR-0016](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0016-one-repository-for-application-and-operations.md) keeps the application and its operations in one repository there. The host itself is not decided in an ADR.
- **Choose the alternative when:** the code may not leave the company (self-hosted GitLab or Forgejo), or CI minutes become the main cost.

### C# and .NET

- **Used for:** the web application `NFHotel.Web`, the API `NFHotel.Api`, the domain and the tests (`src/`, `tests/`), on .NET 10.
- **Alternatives:** Java with Spring Boot, TypeScript with Node.js, Python with Django or FastAPI, Go.
- **For it:** one language from the database to the browser (with Blazor); a strong type system and tooling; fast on Linux; long-term support releases; Entity Framework Core and ASP.NET Core Identity primitives in the box.
- **Against it:** a smaller open-source web community than JavaScript or Python; much of the ecosystem is driven by one company; the old reputation of "Windows only" still costs hiring and trust.
- **What practitioners say:** the recurring argument for .NET is productivity: one toolset for web, API, desktop and mobile, and an ORM that people coming from other stacks call one of the best ([.NET Myths Dispelled](https://news.ycombinator.com/item?id=30654114), [The Case for C# and .NET](https://news.ycombinator.com/item?id=32219836)). The criticism is heavy object-oriented layering and that going off the beaten path needs obscure framework interfaces. In the 2025 survey, 27.8% of respondents used C#.
- **Our reason:** the language the team is taught and works in; the architecture is set in [ADR-0001](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0001-clean-architecture-structure.md). The language itself is not decided in an ADR.
- **Choose the alternative when:** the team knows another stack better; that argument wins in nearly every thread.

### Blazor

- **Used for:** the guest pages and the back office of `NFHotel.Web` (Blazor with server rendering and interactive server components).
- **Alternatives:** React, Angular or Vue with a separate API; ASP.NET Core MVC or Razor Pages; HTMX.
- **For it:** the user interface is written in C#, so validation and models are shared with the server, and there is no JavaScript build chain; well suited to forms-heavy business applications.
- **Against it:** a server connection per user in interactive server mode (reconnects, memory per circuit); a smaller component ecosystem; developers outside .NET rarely know it; fear that Microsoft drops it, as it dropped Silverlight.
- **What practitioners say:** developers who left a Vue or React front end describe Blazor as a relief from the JavaScript plumbing; others who built a large Blazor Server application report slow tooling and high idle CPU, and would not use it again ([.NET Blazor](https://news.ycombinator.com/item?id=38360582)). It is used by 7% of respondents in the 2025 survey.
- **Our reason:** not decided in an ADR. It follows from C# for the whole stack.
- **Choose the alternative when:** the front end must work offline or be very interactive, or a separate front-end team works in JavaScript.

### Bootstrap

- **Used for:** the CSS of `NFHotel.Web` (`wwwroot/lib/bootstrap`, from the Blazor template).
- **Alternatives:** Tailwind CSS, a component library such as MudBlazor or Fluent UI Blazor, plain CSS.
- **For it:** comes with the template, ready-made components (forms, tables, navigation), no build step.
- **Against it:** sites look alike; overriding its styles can be harder than writing them.
- **What practitioners say:** the debate is mostly Tailwind against hand-written CSS: Tailwind's fans value speed in component frameworks, its critics find the class lists verbose and say you trade CSS files for its documentation ([Why Tailwind CSS Won](https://news.ycombinator.com/item?id=37143837)). Both sides agree that a framework such as Bootstrap or Tailwind is chosen to avoid hand-written responsive media queries.
- **Our reason:** not decided in an ADR: it came with the Blazor template.
- **Choose the alternative when:** a designer delivers a custom design system (Tailwind), or the back office needs rich data grids (a Blazor component library).

### Python

- **Used for:** the market analysis and benchmark services (`NF-Hotel/notebook-market-analysis`, `NF-Hotel/notebook-benchmark`), built from `python:3.13-slim` and `python:3.14-slim`.
- **Alternatives:** R; C# for the analysis too.
- **For it:** the language of data and AI work: notebooks, pandas, model clients; the services started as notebooks.
- **Against it:** a second language and runtime to patch; packaging is slower to make reproducible than .NET.
- **What practitioners say:** Python is used by 57.9% of respondents in the 2025 survey. The reference thread for the choice against R is the Data Science Stack Exchange question "Python vs R for machine learning" ([Data Science Stack Exchange](https://datascience.stackexchange.com/questions/326)); its answers are summarised here when the Stack Exchange research is finished.
- **Our reason:** not decided in an ADR: the notebooks were written in Python, and their images are built and frozen as described on [Containers and Images](Containers-and-Images) ([ADR-0500](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0500-freeze-market-analysis-image.md)).
- **Choose the alternative when:** the analysis becomes part of the booking logic (move it into C#).

### PostgreSQL

- **Used for:** the production database (`postgres:16-alpine`, `ops/deploy/postgres/`), its roles ([ADR-0011](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0011-postgresql-access-for-the-api.md)), migrations and backups ([ADR-0008](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0008-database-migration-and-backup.md)) and the team's logins ([ADR-0034](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0034-team-database-access.md)).
- **Alternatives:** MySQL or MariaDB, Microsoft SQL Server, SQLite.
- **For it:**
  - Strict by default: it rejects bad data and invalid queries where MySQL has historically truncated or guessed.
  - Rich SQL: transactional DDL (a failed migration rolls back completely), `CHECK` constraints, JSON, full-text search, window functions, extensions.
  - Fine-grained roles and default privileges, which the role design of ADR-0011 and ADR-0034 depends on.
  - Free and community-owned (no single company can relicense it); very good tooling (`pg_dump`, `psql`, DBeaver).
- **Against it:**
  - One process per connection: memory per connection is higher than MySQL's threads, so many clients need a pooler.
  - `VACUUM` and bloat need attention under heavy updates.
  - Replication and major upgrades are more work than in MySQL.
- **What practitioners say:**
  - In "how do you choose between MySQL and Postgres", the top answers pick PostgreSQL for being full-featured and rock-solid, and several cite MySQL returning nonsense instead of an error. The strongest counter-answer prefers MySQL for simpler maintenance, simpler replication and query hints. Another common answer: choose what the team knows, both scale ([Ask HN, 2023](https://news.ycombinator.com/item?id=35906604)).
  - A long-time MySQL developer leaving the project wrote that people should strongly consider PostgreSQL instead; the comments add that MySQL users often do not know what they miss ([Leaving MySQL](https://news.ycombinator.com/item?id=29455852)).
  - The famous counter-example is Uber moving from PostgreSQL to MySQL for its write-heavy replication at very large scale ([Why Uber Engineering Switched](https://news.ycombinator.com/item?id=12166585)).
  - Against SQL Server, the argument is cost and the open ecosystem; its defenders point to the bundled BI tools and ease of use ([Postgres over SQL Server](https://news.ycombinator.com/item?id=5512959)).
  - In the 2025 survey, 55.6% of respondents used PostgreSQL, 40.5% MySQL, 37.5% SQLite and 30.1% SQL Server.
- **Our reason:** not decided in an ADR. The design that ADR-0011 and ADR-0034 build on (group roles, default privileges, per-role settings such as `log_statement` and `lock_timeout`) is PostgreSQL's, and EF Core supports it through Npgsql.
- **Choose the alternative when:** the hosting offers only MySQL (shared web hosting), the team is a MySQL team, or the data is one user's (SQLite).

### Entity Framework Core and Npgsql

- **Used for:** data access of `NFHotel.Api` (`Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions` for snake_case names) and the migrations, delivered as an idempotent SQL script per release ([ADR-0008](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0008-database-migration-and-backup.md)).
- **Alternatives:** Dapper with hand-written SQL, plain ADO.NET, a migration tool such as DbUp or Flyway.
- **For it:** LINQ queries checked by the compiler, change tracking, and migrations generated from the model; a script can be reviewed before production.
- **Against it:** generated SQL can surprise; performance tuning means reading what EF produced; Dapper is faster for hot read paths.
- **What practitioners say:** EF Core is called one of the best ORMs by developers from other stacks ([.NET Myths Dispelled](https://news.ycombinator.com/item?id=30654114)). On migrations, the advice that recurs is that the tool matters less than running migrations automatically in the deploy, after a backup, and only forward ([How does your team handle database migrations?](https://news.ycombinator.com/item?id=19880334)); that is the design of ADR-0008.
- **Our reason:** the migration path is decided in ADR-0008; the ORM itself is not decided in an ADR.
- **Choose the alternative when:** a query is hot and simple (Dapper for that query only).

### Ollama

- **Used for:** the local model server (`ollama/ollama`) of the benchmark and the market analysis; the default model is in [ADR-0014](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0014-default-ai-model.md).
- **Alternatives:** llama.cpp server, vLLM, LM Studio, a hosted API (OpenAI, Anthropic, Azure).
- **For it:** one command to pull and run a model, an HTTP API, a Docker image with GPU support; data stays on the node.
- **Against it:** a wrapper around llama.cpp that lags it in features and back ends; criticised for unclear model names and quantisation; slower than vLLM for many parallel users.
- **What practitioners say:** two large threads show both sides: Ollama solved the user-experience problem of local models, while llama.cpp is described as powerful but unfriendly; the critics say Ollama gives too little credit to llama.cpp and confuses users with model names ([The local LLM ecosystem doesn't need Ollama](https://news.ycombinator.com/item?id=47788385), [Llama.cpp supports Vulkan, why doesn't Ollama?](https://news.ycombinator.com/item?id=42886680)). In the 2025 survey, 15.3% of respondents used Ollama.
- **Our reason:** a local model keeps hotel data on the node, and the benchmark compares local models. The server is not decided in an ADR.
- **Choose the alternative when:** many users call the model at once (vLLM), or a model that does not fit the GPU is needed (a hosted API, with a data agreement).

## 3. Build

### GitHub Actions

- **Used for:** the ten workflows and four composite actions on [Workflows](Workflows): CI, the release, the deploy, the scans and the wiki.
- **Alternatives:** Jenkins, GitLab CI, Azure Pipelines, a self-hosted runner, Woodpecker or Drone.
- **For it:** built into GitHub, so a check is on the pull request with no integration; hosted Linux runners with no server to maintain; OIDC tokens for Tailscale and for attestations; free for the project's usage.
- **Against it:** the feedback loop is push and wait; YAML logic is hard to test locally; third-party actions are a supply-chain risk (they must be pinned); its price and minutes are Microsoft's to change.
- **What practitioners say:**
  - The most discussed complaints are the slow feedback loop and the lack of a way to run a workflow locally ([The Pain That Is GitHub Actions](https://news.ycombinator.com/item?id=43419701), [I hate GitHub Actions with passion](https://news.ycombinator.com/item?id=46614558)).
  - The advice that recurs in both threads: keep the logic in scripts that run locally and keep the workflow a thin caller. That is how NF Hotel is built (`ops/scripts/*.sh` called by short `run:` steps).
  - Jenkins is described as working but GUI-oriented, slow, hard to back up and full of plugins ([Jenkins Is Getting Old](https://news.ycombinator.com/item?id=19781251)). GitLab CI is often rated better by people who know both.
  - The supply-chain thread on the compromised Trivy action shows why actions are pinned by full commit SHA ([Trivy under attack again](https://news.ycombinator.com/item?id=47475888)).
- **Our reason:** [ADR-0007](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0007-cicd-and-supply-chain.md) chose hosted workflows that build once and deploy by digest, and rejected a self-hosted runner on the VM because untrusted code would run next to production.
- **Choose the alternative when:** the code is on GitLab, or builds need hardware the hosted runners lack.

### Docker

- **Used for:** every service runs as a container with the Docker Engine and the compose plugin on the VM and the monitoring LXC ([Containers and Images](Containers-and-Images)); the images are built with `docker build`.
- **Alternatives:** Podman (daemonless, rootless by default), containerd with nerdctl, plain systemd services with no containers.
- **For it:** the standard: every image, guide and CI service assumes it; Compose is mature; the daemon's `userns`, `no-new-privileges` and logging defaults are set in one file (`ops/deploy/host/`).
- **Against it:** a root daemon (a container breakout or the socket gives root); Docker Desktop's licence for large companies (not used here); Docker Hub limits.
- **What practitioners say:** the largest thread about switching to Podman shows the split: those who switched praise the daemonless, rootless design and having no licence questions, while many report containers and compose files that work in Docker and fail in Podman, and old Podman versions in Ubuntu and on the CI runners ([I ditched Docker for Podman](https://news.ycombinator.com/item?id=45137525), [Transitioning from Docker to Podman](https://news.ycombinator.com/item?id=25165195)). In the 2025 survey, 71.1% of respondents used Docker and 11.1% Podman.
- **Our reason:** not decided in an ADR as a product; [ADR-0003](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md) runs "Docker and the compose plugin inside the VM". The daemon's weaknesses are covered by the hardening on [Security Controls](Security-Controls).
- **Choose the alternative when:** the host is RHEL or Fedora (Podman is native), or containers must run rootless without a daemon.

### GitHub Container Registry (GHCR)

- **Used for:** the four own images, `ghcr.io/nf-hotel/nfhotel-web`, `nfhotel-api`, `market-analysis` and `benchmark`, pushed by Release and pulled by digest on the VM.
- **Alternatives:** Docker Hub, Azure Container Registry, a self-hosted Harbor or a registry on the node.
- **For it:** the same login and permissions as the repository (`GITHUB_TOKEN`, no extra secret); attestations are stored next to the images; private images within the plan.
- **Against it:** the images and their availability depend on GitHub; private storage beyond the plan costs.
- **What practitioners say:** when GHCR launched, the concern was the price of transfer and storage for private images ([GitHub Container Registry](https://news.ycombinator.com/item?id=24343937)). When Docker Hub limited unauthenticated pulls to ten an hour per address, many moved images to GHCR or a mirror ([Docker limits unauthenticated pulls](https://news.ycombinator.com/item?id=43125089)).
- **Our reason:** [ADR-0007](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0007-cicd-and-supply-chain.md), option 2: build once, push to GHCR, deploy by digest.
- **Choose the alternative when:** the images must stay inside the company network (Harbor).

### Base images: Microsoft's .NET images, Debian slim and Alpine

- **Used for:** `mcr.microsoft.com/dotnet/sdk` (build stage) and `mcr.microsoft.com/dotnet/aspnet` (final stage) for the web and the API; `python` slim for the Python services; `debian` slim for the backup target; the Alpine variants of `postgres`, `nginx` and `haproxy`. All are pinned by digest.
- **Alternatives:** distroless images (Google), Chainguard images, Ubuntu Chiseled .NET images, building from `scratch`.
- **For it:** Microsoft's images are the supported way to run .NET and are patched with each .NET release; Debian slim uses glibc, so binary wheels and tools work; Alpine images are small.
- **Against it:** full images carry a shell and a package manager an attacker could use; Alpine uses musl, which breaks some binaries and changes DNS behaviour.
- **What practitioners say:** the Alpine maintainers themselves say there is no "glibc-based Alpine", and that wanting glibc means a mainstream distribution ([There is no such thing as a glibc based alpine image](https://news.ycombinator.com/item?id=28311817)). Distroless is praised as the right deployment artefact but criticised as still built from Debian packages and harder to debug without a shell ([Distroless](https://news.ycombinator.com/item?id=15415992)).
- **Our reason:** not decided in an ADR. Microsoft's images are the vendor's; Alpine is used only where the upstream project publishes it (PostgreSQL, nginx, HAProxy). The risk of a shell in the image is reduced by `read_only`, `cap_drop: [ALL]` and non-root users ([Containers and Images](Containers-and-Images)).
- **Choose the alternative when:** the scanner finds more CVEs in the base than the team can follow (Chiseled or Chainguard), or an image needs no shell at all.

## 4. Test

### xUnit

- **Used for:** the unit and integration tests in `tests/` (`xunit.v3`, with NSubstitute for fakes and coverlet or `dotnet-coverage` for coverage).
- **Alternatives:** NUnit, MSTest.
- **For it:** the default of modern .NET templates; a fresh test class instance per test, so tests do not share state by accident; parallel by default; used by the .NET team itself.
- **Against it:** less built-in for parameterised and setup-heavy suites than NUnit (fixtures instead of `[SetUp]`); fewer assertion helpers.
- **What practitioners say:** the long-standing Stack Overflow comparison "NUnit vs. MbUnit vs. MSTest vs. xUnit.net" (over 400 votes) is the reference thread for this choice ([Stack Overflow](https://stackoverflow.com/questions/261139)). Its answers are summarised here when the Stack Exchange research is finished.
- **Our reason:** not decided in an ADR; the convention of the `coding-conventions` skill for C#.
- **Choose the alternative when:** the team knows NUnit and its fixtures.

### Testcontainers

- **Used for:** the integration tests start a real PostgreSQL in Docker (`Testcontainers.PostgreSql`), reset between tests with Respawn; CI checks Docker first ("Check Docker for the integration tests").
- **Alternatives:** an in-memory provider (EF Core InMemory, used only for unit tests), SQLite, a shared test database, Docker Compose started by the CI script.
- **For it:** tests run against the same PostgreSQL version as production, with its constraints and SQL; each run gets a clean database.
- **Against it:** needs Docker on the developer machine and the runner; slower than in-memory tests.
- **What practitioners say:** the large thread is mostly praise: integration tests against real databases are worth far more than mocks, and Testcontainers makes them easy ([Testcontainers](https://news.ycombinator.com/item?id=39531536)). The criticism is that Docker is a leaky abstraction, and that it plays badly inside other containers (Docker in Docker).
- **Our reason:** not decided in an ADR.
- **Choose the alternative when:** the tests must run where Docker is not allowed.

### Playwright

- **Used for:** the end-to-end tests of the guest booking and the back office against a throwaway stack (`Microsoft.Playwright`, composite action `e2e-tests`), in a container, never on a desktop.
- **Alternatives:** Selenium, Cypress, bUnit (component tests only).
- **For it:** auto-waiting, so fewer flaky tests; Chromium, Firefox and WebKit from one API; a supported .NET library; traces for failed tests.
- **Against it:** driven by one company (Microsoft); browsers are downloaded per version, which makes the image large.
- **What practitioners say:** "Don't use Selenium, use Playwright" captures the common view: after a decade of fragile browser tests, Playwright is the first that makes them reliable enough to be worth it; the counterpoint is that a community project (Selenium) is more likely to last than one company's tool ([Web automation](https://news.ycombinator.com/item?id=33537772)). Cypress is praised for its step-by-step debugger, and criticised for its chained command style and for being JavaScript only ([Cypress](https://news.ycombinator.com/item?id=25381242)).
- **Our reason:** not decided in an ADR. Cypress has no .NET API, so Playwright and Selenium were the options in C#.
- **Choose the alternative when:** the tests must drive a grid of real devices (Selenium Grid).

### k6

- **Used for:** the weekly load test of a throwaway stack (`load-test.yaml`, `grafana/k6`).
- **Alternatives:** Apache JMeter, Locust, Gatling, NBomber (.NET).
- **For it:** scripts in JavaScript as code in git; a single binary or container; thresholds make the run pass or fail; from Grafana Labs, like the monitoring.
- **Against it:** JavaScript, not C#; no browser-level load (it measures HTTP).
- **What practitioners say:** k6 is described as a go-to for performance tests because a script runs within a minute and Postman collections convert to it ([K6: Like unit testing, for performance](https://news.ycombinator.com/item?id=25234746)). JMeter is the long-time standard, but its tests are XML made in a GUI.
- **Our reason:** not decided in an ADR.
- **Choose the alternative when:** the team wants the load test in C# (NBomber).

### SonarQube Cloud

- **Used for:** quality and coverage analysis on every pull request and push to `main`, with the quality gate `Sonar way` ([ADR-0033](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0033-sonarqube-cloud-coverage.md)).
- **Alternatives:** a self-hosted SonarQube server, Codacy, Code Climate, the .NET analysers alone.
- **For it:** free for public projects; a gate on new code ("coverage on new code at least 80 %"); duplication and maintainability findings that the compiler does not give.
- **Against it:** a second build in the CI job; findings can be noisy; the analysis is hosted outside the team.
- **What practitioners say:** few public discussions; the Stack Overflow questions are about setting it up for pull requests and monorepos rather than whether to use it. Product documentation (no community source): [SonarQube Cloud documentation](https://docs.sonarsource.com/sonarqube-cloud/).
- **Our reason:** [ADR-0033](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0033-sonarqube-cloud-coverage.md), option 3: the scanner in the existing build job, so one build and one test run.
- **Choose the alternative when:** the code is private and must not leave the company (self-hosted SonarQube).

### CodeQL

- **Used for:** code scanning of C# and of the workflows by GitHub's default setup; what it shows is set in [ADR-0032](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0032-what-code-scanning-shows.md).
- **Alternatives:** Semgrep, Snyk Code, SonarQube's security rules.
- **For it:** free for public repositories, with no setup beyond the switch; deep data-flow analysis (taint tracking from input to sink); results in the Security tab and on the pull request.
- **Against it:** slow on large code bases; writing own queries needs its query language; GitHub only.
- **What practitioners say:** a thread on Dependabot noise singles out CodeQL as a tool that genuinely impressed, because it reports reachable problems ([Turn Dependabot off](https://news.ycombinator.com/item?id=47094192)). Semgrep is praised for fast custom rules written in the language's own syntax ([Semgrep](https://news.ycombinator.com/item?id=26904951)).
- **Our reason:** ADR-0032 for what is shown; the scanner itself is GitHub's default and not decided in an ADR.
- **Choose the alternative when:** the team wants its own project-specific rules (Semgrep).

### OWASP ZAP

- **Used for:** the dynamic scan (DAST) of a throwaway stack: a baseline signed out and signed in on every pull request, a weekly full scan (composite action `dast-scan`, `ghcr.io/zaproxy/zaproxy`).
- **Alternatives:** Burp Suite (Community or Professional), Nuclei, Nikto.
- **For it:** free and open source; a baseline mode made for CI; rules can be tuned per finding; results go to code scanning.
- **Against it:** more false positives and a slower full scan than Burp Professional; a scanner cannot find logic flaws.
- **What practitioners say:** in the Information Security Stack Exchange question "What are the differences between Burp and OWASP ZAP?", the comparison is free and automatable against the stronger manual tooling of Burp Professional ([Security Stack Exchange](https://security.stackexchange.com/questions/215553)); its answers are added with the Stack Exchange research.
- **Our reason:** not decided in an ADR; set up in MIL-027 and MIL-036.
- **Choose the alternative when:** a manual penetration test is done (Burp Professional).

### Trivy

- **Used for:** scanning every image before it is pushed (the gate `ops/scripts/scan-gate.sh`), the daily scan of the images running in production (`running-image-scan.yaml`), and the SPDX SBOMs that are attested (`aquasec/trivy`, pinned by digest).
- **Alternatives:** Grype with Syft, Docker Scout, Snyk Container.
- **For it:** one tool for vulnerabilities, misconfiguration and SBOMs; free; SARIF for code scanning; fast database updates.
- **Against it:** a scanner that runs everywhere is itself a target; in March 2026 Trivy's releases and its GitHub Action tags were compromised.
- **What practitioners say:** the thread on the compromise draws the lesson that supply-chain tools are no safer than what they protect, and that only a full commit SHA or digest is immutable ([Trivy under attack again](https://news.ycombinator.com/item?id=47475888)).
- **Our reason:** not decided in an ADR. The image is pinned by digest and run as a container, not as a moving action tag, which is the protection the thread recommends.
- **Choose the alternative when:** a second opinion is wanted (Grype gives different results on the same image).

## 5. Release

### Dependabot

- **Used for:** pull requests that update NuGet packages, the actions and the base images, and alerts for known vulnerabilities.
- **Alternatives:** Renovate, manual updates on a schedule.
- **For it:** built into GitHub with no app to install; security alerts and updates in one place.
- **Against it:** noisy, because it does not know whether the vulnerable code is reachable; fewer grouping and scheduling options than Renovate; a pull request from Dependabot gets no secrets (so SonarQube Cloud does not run on it).
- **What practitioners say:** Renovate is recommended for its grouping, scheduling and configurability, with the complaint that it proposes updates that conflict ([Why I recommend Renovate](https://news.ycombinator.com/item?id=40011111)). A large 2026 thread argues for turning Dependabot's version noise off and fixing only vulnerabilities that are reached ([Turn Dependabot off](https://news.ycombinator.com/item?id=47094192)).
- **Our reason:** not decided in an ADR; the GitHub default. Every update still passes all required checks before it merges.
- **Choose the alternative when:** the number of update pull requests becomes a burden (Renovate with grouping).

### GitHub artifact attestations and SBOMs

- **Used for:** every image gets a build provenance attestation and an SPDX SBOM attestation (`actions/attest-build-provenance`, `actions/attest`), and CD refuses a digest without a valid attestation (`gh attestation verify`).
- **Alternatives:** cosign with Sigstore directly, Notation, no signing.
- **For it:** keyless signing with the workflow's OIDC identity (no private key to protect), stored with GitHub; verification in one command; reaches SLSA build level 2 and above.
- **Against it:** tied to GitHub; verification needs the `gh` CLI or the Sigstore tools.
- **What practitioners say:** the Sigstore launch thread asks whether signing is needed at all when a hash exists; the answer is that a hash proves the bytes, not who built them ([Sigstore](https://news.ycombinator.com/item?id=28121060)).
- **Our reason:** [ADR-0007](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0007-cicd-and-supply-chain.md), decision 3: verify before deploy.
- **Choose the alternative when:** the images are published outside GitHub (cosign).

### OpenSSF Scorecard

- **Used for:** a weekly and on-push check of the repository's security practices (`scorecard.yaml`, `ossf/scorecard-action`), shown in code scanning.
- **Alternatives:** none in the same niche; manual review against the CIS and SAMM checklists ([Security Controls](Security-Controls)).
- **For it:** an outside, repeatable score for pinned actions, branch protection, token permissions and more.
- **Against it:** some checks do not fit a small team (for example code review by a second person).
- **What practitioners say:** few public discussions; it is a standard of the Open Source Security Foundation. Product documentation (no community source): [OpenSSF Scorecard](https://github.com/ossf/scorecard).
- **Our reason:** not decided in an ADR.
- **Choose the alternative when:** not applicable.

## 6. Deploy

### Docker Compose

- **Used for:** each service is a compose project in `ops/deploy/<service>/`, deployed by `deploy-services.sh`.
- **Alternatives:** Kubernetes (k3s, MicroK8s), HashiCorp Nomad, Docker Swarm, Podman Quadlets with systemd.
- **For it:** the files are short and readable; the same files run in CI and in production; no control plane to run on one VM.
- **Against it:** one host only, no rolling update across nodes, no self-healing beyond `restart`; Docker's firewall rules must be lived with.
- **What practitioners say:**
  - "Should I run plain Docker Compose in production" is answered mostly yes: production-ready for light needs on one host. The warning is not to rebuild Kubernetes by hand ([2026 thread](https://news.ycombinator.com/item?id=47962032)).
  - "Kubernetes is a red flag signalling premature optimisation" calls it "what-if engineering" for small teams. Its defenders say a managed cluster is easy ([thread](https://news.ycombinator.com/item?id=31974420)).
  - Nomad is praised as simpler to run than Kubernetes ([Nomad vs. Kubernetes](https://news.ycombinator.com/item?id=28875764)).
  - In the 2025 survey, 28.5% of respondents used Kubernetes.
- **Our reason:** [ADR-0003](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md) keeps "the compose files … unchanged" inside one VM; [ADR-0026](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0026-no-staging-environment.md) explains why the throwaway compose stack of CI replaces a staging environment.
- **Choose the alternative when:** the hotel needs more than one host for uptime (k3s or Nomad).

### Proxmox VE

- **Used for:** the hypervisor of the team's node: the VM `nf-hotel` and the monitoring LXC ([ADR-0003](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0003-proxmox-vm-for-services.md)); `dockurr/proxmox` runs a throwaway Proxmox for the integration tests.
- **Alternatives:** VMware ESXi, Microsoft Hyper-V, XCP-ng, plain KVM with libvirt, a cloud VM.
- **For it:** free and open source (support subscription optional); KVM VMs and LXC containers in one web interface; ZFS, snapshots and backups built in; an API for automation.
- **Against it:** smaller ecosystem of backup and enterprise tools than VMware; the free repository is not the tested enterprise one.
- **What practitioners say:** after Broadcom bought VMware, ended the free ESXi and raised prices many times over, threads are full of companies leaving VMware; Proxmox users describe it as reliable and quietly improving ([Import Wizard for Migrating VMware ESXi VMs](https://news.ycombinator.com/item?id=39841363), [1k% price hike](https://news.ycombinator.com/item?id=42304955)). VMware's remaining strength is its ecosystem of certified staff and backup vendors.
- **Our reason:** the team's own node runs Proxmox; ADR-0003 chose a KVM VM over an LXC container or Docker on the host. The hypervisor itself is not decided in an ADR.
- **Choose the alternative when:** the hotel's IT already runs VMware or Hyper-V with support.

### Debian

- **Used for:** the VM `nf-hotel` (Debian 13), the monitoring LXC, the Proxmox host (Proxmox VE 9 is built on Debian 13), and `debian:12-slim` in the script tests and the backup target.
- **Alternatives:** Ubuntu Server, Rocky Linux or AlmaLinux, Windows Server.
- **For it:** stable releases with long support, community-run with no company behind it, the base of Proxmox, no snaps, small default install; the CIS Benchmark exists for it.
- **Against it:** older package versions within a release; no paid vendor support of its own.
- **What practitioners say:** Ubuntu's push of snaps, which update automatically and could not be turned off, drove many to Debian ([snap obsession](https://news.ycombinator.com/item?id=23052108)). Debian users praise its measured pragmatism and long stability ([Debian 13 "Trixie"](https://news.ycombinator.com/item?id=44848782)). Ubuntu's strengths are newer kernels, commercial support and that most cloud guides assume it.
- **Our reason:** not decided in an ADR. Proxmox is Debian, so the whole node uses one distribution and one set of tools; the hardening follows the CIS Debian benchmark ([Host Hardening](Host-Hardening)).
- **Choose the alternative when:** a vendor supports its software only on Ubuntu or RHEL.

### nginx

- **Used for:** the reverse proxy and TLS end (`nginx:1.29-alpine`): HTTPS on the tailnet, the public listener for the tunnel, the internal listener for the analysis ([ADR-0006](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0006-tls.md)).
- **Alternatives:** Caddy, Traefik, HAProxy, Apache httpd, YARP in .NET.
- **For it:** the most used proxy, so every question has an answer; fast and light; plain configuration files tested in Ops CI (`test-compose-proxy.sh`).
- **Against it:** no automatic certificates (until its recent ACME support); configuration has traps (`if`, inherited headers); the open-source edition shows few metrics; its core developer forked it to freenginx in 2024.
- **What practitioners say:** Caddy's fans value automatic HTTPS and a simple Caddyfile; the counterpoint is that its only advantage over nginx is easier certificates ([Caddy](https://news.ycombinator.com/item?id=43070025), [Caddy vs. Nginx benchmark](https://news.ycombinator.com/item?id=32865497)). Traefik's container discovery is liked, but production users complain about its documentation ([Take a look at Traefik](https://news.ycombinator.com/item?id=40264042)). The freenginx fork raised the question of nginx's future ([Freenginx](https://news.ycombinator.com/item?id=39373327)).
- **Our reason:** ADR-0006 put TLS in nginx; certificates come from `tailscale cert` and Cloudflare ([ADR-0009](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0009-tls-certificates.md)), so Caddy's main advantage does not apply. The proxy itself is not decided in an ADR.
- **Choose the alternative when:** the site needs its own public certificate with no tunnel (Caddy), or routes change with containers (Traefik).

### HAProxy

- **Used for:** the relay `postgres-tailnet` (`haproxy:3.2-alpine`) in TCP mode, which gives the team's DBeaver access to PostgreSQL on the tailnet only ([ADR-0034](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0034-team-database-access.md)).
- **Alternatives:** nginx's `stream` module, socat, publishing the PostgreSQL port directly.
- **For it:** made for TCP proxying, with health checks and timeouts; a small Alpine image; keeps the database container off a published port.
- **Against it:** one more product to know besides nginx.
- **What practitioners say:** in the reverse-proxy debates HAProxy is the load balancer that "just works" and is rarely benchmarked ([HAProxy vs. Nginx for load balancing](https://news.ycombinator.com/item?id=15345846), [Caddy vs. Nginx benchmark](https://news.ycombinator.com/item?id=32865497)); the Server Fault question "What are the differences between HAProxy and Nginx in reverse proxy mode?" is the reference ([Server Fault](https://serverfault.com/questions/229945)).
- **Our reason:** ADR-0034.
- **Choose the alternative when:** only HTTP is proxied (nginx alone).

### Cloudflare Tunnel

- **Used for:** the public name `nfhotel.devmatiker.com`: `cloudflared` (`cloudflare/cloudflared`) makes an outbound connection to Cloudflare and forwards to the public listener of nginx ([ADR-0028](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0028-public-access-through-cloudflare-tunnel.md)).
- **Alternatives:** open ports 80 and 443 in the home router, Tailscale Funnel, a small VPS with WireGuard and a proxy, ngrok, Pangolin (self-hosted).
- **For it:** no open port and the home address stays hidden; a free certificate; DDoS protection, a rate-limit rule and Access in the same dashboard; turned off by removing one file.
- **Against it:** Cloudflare ends TLS, so it sees the traffic (a processor under GDPR, decision 9 of ADR-0028); lock-in to one company; the free plan's terms limit some uses; attackers also use tunnels, so some networks block them.
- **What practitioners say:**
  - People who list ways to expose a home service recommend Cloudflare Tunnel as the easiest. The named downsides are no end-to-end encryption and the terms of the free plan ([Exposing a web service with Cloudflare Tunnel](https://news.ycombinator.com/item?id=30257978)).
  - The do-it-yourself route is a cheap VPS with WireGuard and a proxy. Its critics answer that the tunnel's value is that there is no extra server to maintain ([You may not need Cloudflare Tunnel](https://news.ycombinator.com/item?id=30953744)).
  - Open-source alternatives such as Pangolin exist for those who dislike the lock-in ([Pangolin](https://news.ycombinator.com/item?id=44526015)).
  - Cloudflare's undocumented limits and support on the business plan are a warning in several threads.
- **Our reason:** [ADR-0028](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0028-public-access-through-cloudflare-tunnel.md), option 3, over open ports and Tailscale Funnel.
- **Choose the alternative when:** guests' data may not pass a third party in clear text (Tailscale Funnel ends TLS on the VM, or a VPS the team controls).

### Cloudflare Access

- **Used for:** `/admin` on the public name: a one-time PIN to a listed e-mail address before the hotel's own sign-in; the tunnel validates the token ([ADR-0029](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0029-back-office-behind-cloudflare-access.md)).
- **Alternatives:** the back office on the tailnet only, a hidden path, Authelia or Authentik in front of nginx, a VPN for staff.
- **For it:** a second lock with no new service to run; free for a small team; the policy is in the same place as the tunnel.
- **Against it:** the same dependency on Cloudflare; e-mail one-time codes are weaker than a hardware key.
- **What practitioners say:** a guide to Cloudflare's Zero Trust tunnels is praised, while comments point out that Cloudflare ends TLS where Tailscale does not ([I finally understand Cloudflare Zero Trust tunnels](https://news.ycombinator.com/item?id=45946865)). Self-hosted Authelia is liked for putting authentication in the proxy, but it was not yet a full identity provider ([Authelia](https://news.ycombinator.com/item?id=26409820)).
- **Our reason:** [ADR-0029](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0029-back-office-behind-cloudflare-access.md), option 3.
- **Choose the alternative when:** the tunnel is replaced (then Authelia or a VPN).

### Tailscale

- **Used for:** the private network (tailnet) of the team, the VM and the monitoring LXC; CD joins it as an ephemeral node with OIDC and deploys over Tailscale SSH ([ADR-0007](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0007-cicd-and-supply-chain.md)); `tailscale cert` gives the tailnet certificate ([ADR-0009](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0009-tls-certificates.md)); the policy is in git ([ADR-0027](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0027-configuration-as-code.md)).
- **Alternatives:** plain WireGuard, Headscale (a self-hosted control server), OpenVPN, ZeroTier, Nebula, NetBird.
- **For it:** WireGuard underneath, with key distribution, NAT traversal, access rules, SSH and certificates added; free for the team; works from the GitHub runner with no stored secret.
- **Against it:** the control plane is a company's service (it distributes the keys); one compromised node reaches what the policy allows.
- **What practitioners say:** the common summary is that Tailscale is WireGuard "with all the reasonable management added", and that plain WireGuard is a hassle for most ([Tailscale is pretty useful](https://news.ycombinator.com/item?id=43270835)). The open question in those threads is trust in the hosted control plane, answered with Tailnet Lock or Headscale ([Headscale](https://news.ycombinator.com/item?id=28572013)).
- **Our reason:** ADR-0007 (the deploy path) and ADR-0009; the product itself was in use before them and is not decided in an ADR.
- **Choose the alternative when:** the key distribution must be under the team's control (Headscale).

### TLS certificates: Let's Encrypt through tailscale cert, and Cloudflare

- **Used for:** the tailnet name has a Let's Encrypt certificate issued through `tailscale cert`, renewed by a systemd timer; the public name has Cloudflare's edge certificate ([ADR-0009](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0009-tls-certificates.md)).
- **Alternatives:** certbot on the VM, a paid certificate, a self-signed certificate (used only for development).
- **For it:** free, automated, trusted by every browser.
- **Against it:** certificates of 90 days (shorter in future) need working renewal; a failed renewal is silent unless watched (now an alert, [Alerts](Alerts)).
- **What practitioners say:** ten years on, people recall paying per host name and renewing by hand, and credit Let's Encrypt with making HTTPS the baseline ([10 Years of Let's Encrypt](https://news.ycombinator.com/item?id=46208962)).
- **Our reason:** ADR-0009.
- **Choose the alternative when:** a certificate with organisation validation is required.

## 7. Operate

### pg_dump

- **Used for:** the daily logical backup and the backup before every deploy (`backup-database.sh`, [ADR-0008](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0008-database-migration-and-backup.md)).
- **Alternatives:** pgBackRest, Barman or WAL-G (physical backups with point-in-time recovery), Proxmox snapshots alone.
- **For it:** part of PostgreSQL; a custom-format dump restores into any version at or above it; small for this database; easy to test with `pg_restore`.
- **Against it:** restores only to the time of the dump (up to a day of data can be lost); slow for very large databases.
- **What practitioners say:** pgBackRest was seen as the leading PostgreSQL backup tool, and the one that took restore testing seriously; its maintainer announced in 2026 that it is no longer maintained, and the thread asks what to move to (WAL-G, Barman) ([Pgbackrest is no longer being maintained](https://news.ycombinator.com/item?id=47919997)).
- **Our reason:** ADR-0008, with the recovery objectives in [ADR-0025](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0025-recovery-objectives.md).
- **Choose the alternative when:** the hotel takes real bookings and a day of data loss is too much (WAL archiving with WAL-G or Barman).

### age

- **Used for:** encrypting every dump and the settings archive to the public key of S03 before it leaves the database container; the VM cannot read its own backups.
- **Alternatives:** GPG, OpenSSL, the encryption of restic or Borg.
- **For it:** one small tool with one job; modern cryptography with no options to get wrong; short keys that fit in a settings file.
- **Against it:** encryption only, no signing; no key server or agent.
- **What practitioners say:** age was welcomed as filling the last legitimate use of PGP, file encryption, without its complexity ([Age](https://news.ycombinator.com/item?id=21895671)); those who stay with GPG point to its signing and key management, which age deliberately leaves out ([Switching from GPG to Age](https://news.ycombinator.com/item?id=45825040)).
- **Our reason:** ADR-0008, decision 1.
- **Choose the alternative when:** the backups must also be signed (GPG or minisign beside age).

### rsync over SSH, restricted by rrsync

- **Used for:** copying each encrypted backup off the VM to the backup target (`ops/deploy/backup-target`), where the key may only write into one folder.
- **Alternatives:** restic or Borg (deduplicating, encrypted repositories), rclone to object storage, scp.
- **For it:** on every Linux system; `--ignore-existing` copies only what the target lacks; `rrsync` limits the key to one write-only folder, so a stolen VM cannot delete the copies.
- **Against it:** no deduplication or snapshots of its own; the retention on the target is separate.
- **What practitioners say:** restic and Borg are praised for deduplicated, encrypted, versioned backups that saved their users ([Restic](https://news.ycombinator.com/item?id=29209455), [BorgBackup](https://news.ycombinator.com/item?id=34152369)); a Server Fault question shows how to restrict an SSH key to rsync only ([Server Fault](https://serverfault.com/questions/965053)).
- **Our reason:** ADR-0008 and decision D7 of the DevSecOps assessment (DSA-001). The files are already compressed and encrypted, so deduplication would gain little.
- **Choose the alternative when:** the backups grow large or go to cloud storage (restic).

### systemd timers

- **Used for:** the backup, the certificate renewal and the health report (`ops/deploy/systemd/`).
- **Alternatives:** cron, a scheduler container.
- **For it:** a missed run runs at the next boot (`Persistent=true`); the output goes to the journal and from there to Loki; a run can be started by hand with `systemctl start`; dependencies and timeouts.
- **Against it:** two files per job instead of one crontab line.
- **What practitioners say:** the 2026 thread "Love systemd timers" lists the same points (runs after a missed schedule, journal logging, manual start for debugging), while cron's defenders value its single file and decades of reliability ([Love systemd timers](https://news.ycombinator.com/item?id=48367904), [Systemd.timer, an alternative to cron](https://news.ycombinator.com/item?id=33911910)); the Unix and Linux Stack Exchange question "Cron vs systemd timers" is the reference ([Unix & Linux](https://unix.stackexchange.com/questions/278564)).
- **Our reason:** ADR-0008 and ADR-0009 name the timers; the choice over cron is not decided in an ADR.
- **Choose the alternative when:** the host has no systemd.

### nftables

- **Used for:** the host firewall of the VM (`apply-firewall.sh`, service `nfhotel-firewall`), tested in network namespaces in Ops CI.
- **Alternatives:** ufw, firewalld, iptables.
- **For it:** the firewall of current Debian; one rule set, atomic reloads, sets for addresses; it can sit next to Docker's own rules with a known order.
- **Against it:** less known than iptables; Docker still writes iptables-nft rules of its own.
- **What practitioners say:** nftables is advocated because poorly configured iptables is common, and simplifying front ends lead to more misconfiguration ([New in Debian Stretch: nftables](https://news.ycombinator.com/item?id=14286016)); the Docker compose thread asks exactly how to manage nftables next to Docker's rules ([2026 thread](https://news.ycombinator.com/item?id=47962032)).
- **Our reason:** MIL-023 (host hardening); not decided in an ADR.
- **Choose the alternative when:** the team wants a simpler front end and accepts its limits (ufw).

### unattended-upgrades

- **Used for:** automatic security updates on the VM (`install-patching.sh`).
- **Alternatives:** manual patching on a schedule, Ansible runs, Livepatch.
- **For it:** security fixes arrive within a day without anyone logging in; limited to the security repository.
- **Against it:** an update can restart a service at an unplanned time.
- **What practitioners say:** few comparison threads; the questions on Server Fault are about how to configure it, not whether. Product documentation (no community source): [Debian wiki, UnattendedUpgrades](https://wiki.debian.org/UnattendedUpgrades).
- **Our reason:** MIL-023 and the CIS checklist; not decided in an ADR.
- **Choose the alternative when:** several hosts must be patched in a controlled order (Ansible).

### Secrets as files

- **Used for:** each secret is a file mounted by Compose at `/run/secrets`, written from the settings files at each deploy, never an environment variable ([ADR-0024](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0024-secrets-as-files.md)).
- **Alternatives:** environment variables, HashiCorp Vault or OpenBao, Infisical, SOPS-encrypted files in git, Docker Swarm secrets.
- **For it:** not in `docker inspect`, `/proc/1/environ` or crash dumps; nothing new to run, unseal or back up.
- **Against it:** the secrets are still plain files on the VM; no central rotation or audit.
- **What practitioners say:** "Environment variables considered harmful for your secrets" started the debate; its critics answer that files are not safer by default ([thread](https://news.ycombinator.com/item?id=8826024)). For Vault against SOPS, Vault's own creator says it is worth it for dynamic secrets and policies, but for a small team it is infrastructure to maintain ([SOPS is underrated](https://news.ycombinator.com/item?id=23031628)). The Information Security Stack Exchange question "Is it unsafe to use environmental variables for secret data?" is the reference ([Security Stack Exchange](https://security.stackexchange.com/questions/197784)).
- **Our reason:** ADR-0024, option 2, over environment variables, a vault and Swarm secrets.
- **Choose the alternative when:** secrets must rotate automatically or be leased (OpenBao).

## 8. Monitor

The stack was chosen in [ADR-0020](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0020-monitoring-stack.md) over Gatus, Uptime Kuma, a hosted service and nothing, after a comparison that S01 accepted in chat. The alerts are on [Alerts](Alerts).

### Prometheus

- **Used for:** the metrics store and the alert rules (`prom/prometheus`, on the monitoring LXC).
- **Alternatives:** Zabbix, InfluxDB with Telegraf, VictoriaMetrics, Datadog.
- **For it:** the standard for metrics; rules and alerts as files tested in CI (`promtool test rules`); pull model with health per target.
- **Against it:** one node only; long retention needs another store (Mimir, Thanos, VictoriaMetrics); its query language takes learning.
- **What practitioners say:** InfluxDB users report it freezing and weak functions as needs grew; others say a startup that can afford Datadog should buy it rather than run its own ([Grafana, InfluxDB and CollectD](https://news.ycombinator.com/item?id=23906165)). VictoriaMetrics is named as a lighter alternative ([Can I have a smaller Prometheus](https://news.ycombinator.com/item?id=30117700)). In the 2025 survey, 11.8% of respondents used Prometheus.
- **Our reason:** ADR-0020, option 1.
- **Choose the alternative when:** many network devices are watched over SNMP (Zabbix).

### Grafana

- **Used for:** dashboards and log search, provisioned from files (`grafana/grafana`).
- **Alternatives:** Kibana, the Prometheus web interface alone, a hosted service.
- **For it:** reads Prometheus and Loki in one place; dashboards in git.
- **Against it:** licensed AGPLv3 since 2021; fast-moving, so dashboards and alert definitions change with versions.
- **What practitioners say:** the relicensing to AGPLv3 was welcomed by many as fair against cloud providers, while some companies feared legal reviews ([Grafana, Loki, and Tempo relicensed](https://news.ycombinator.com/item?id=26877528)). A 2025 post "I can't recommend Grafana anymore" complains of yearly rebuilds of dashboards and alerts ([thread](https://news.ycombinator.com/item?id=45934940)). The alerts of NF Hotel are in Prometheus and Alertmanager, not in Grafana, which avoids that churn.
- **Our reason:** ADR-0020.
- **Choose the alternative when:** the logs are already in Elasticsearch (Kibana).

### Loki

- **Used for:** the log store, kept 14 days, with one log alert (`grafana/loki`).
- **Alternatives:** Elasticsearch or OpenSearch with Kibana, Graylog, VictoriaLogs, journald alone.
- **For it:** indexes labels only, so it is light; queried in Grafana next to the metrics.
- **Against it:** full-text search over long periods is slow; it punishes high-cardinality labels.
- **What practitioners say:** at launch Loki was welcomed as a light alternative to running an Elasticsearch cluster ([Grafana Loki](https://news.ycombinator.com/item?id=18663996)); a later post warned against serious use of an all-in-one local setup, and Loki's lead answered that the criticism was fair but the setup can be run well ([I can't recommend serious use of an all-in-one local Grafana Loki setup](https://news.ycombinator.com/item?id=35741922)). Benchmarks between Elastic, Loki and others are disputed by each side ([benchmark](https://news.ycombinator.com/item?id=34500822)).
- **Our reason:** ADR-0020, sub-choice for logs: Elastic or OpenSearch needs several GB of memory, Graylog needs OpenSearch and MongoDB.
- **Choose the alternative when:** logs must be searched in full text over months (OpenSearch).

### Grafana Alloy

- **Used for:** the agent on the VM (`grafana/alloy`): logs of every container and the journal to Loki; host, container and application metrics to Prometheus; the OTLP receiver of the applications.
- **Alternatives:** Promtail with node_exporter and cAdvisor, Vector, Fluent Bit, the OpenTelemetry Collector.
- **For it:** one agent for logs, metrics and OTLP; Promtail is deprecated in its favour.
- **Against it:** its own configuration language; young.
- **What practitioners say:** few discussions yet; it was announced as an OpenTelemetry Collector distribution with Prometheus pipelines ([Grafana Alloy](https://news.ycombinator.com/item?id=39979016)). Log shipping in general is described as harder than it should be ([Loki thread](https://news.ycombinator.com/item?id=35741922)).
- **Our reason:** ADR-0020 and [ADR-0023](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0023-application-telemetry.md).
- **Choose the alternative when:** the logs go to a store other than Loki (Vector or Fluent Bit).

### Alertmanager

- **Used for:** routing, grouping and inhibiting alerts, and sending them to Discord; the watchdog ping to Healthchecks.io (`prom/alertmanager`).
- **Alternatives:** Grafana alerting, PagerDuty or Opsgenie, e-mail.
- **For it:** the partner of Prometheus; configuration as a file checked by `amtool`; inhibit rules stop alert storms.
- **Against it:** no on-call schedule or escalation.
- **What practitioners say:** solo founders asked how they handle monitoring and paging answer mostly with "choose boring, robust technology" and well-tuned alert sensitivity, so that alerts are rare and real; a paging service comes when someone must be woken up ([Ask HN: How do solo SaaS founders handle monitoring/PagerDuty?](https://news.ycombinator.com/item?id=26203074)).
- **Our reason:** ADR-0020.
- **Choose the alternative when:** someone is on call at night (PagerDuty).

### Healthchecks.io

- **Used for:** the outside watchdog: Alertmanager pings it every minute, and when the pings stop it tells Discord ([Alerts](Alerts)).
- **Alternatives:** Dead Man's Snitch, Cronitor, Uptime Kuma or Gatus on another machine.
- **For it:** free, receives only a ping (no data leaves), and watches the monitoring from outside, which the monitoring cannot do for itself.
- **Against it:** an outside service the team does not control.
- **What practitioners say:** its one-person, open-source setup is admired as pragmatic engineering, and users rely on its free tier for years ([What One-person SaaS Healthchecks.io uses](https://news.ycombinator.com/item?id=31488910)).
- **Our reason:** ADR-0020, sub-choice for the watchdog (the laptop alternative is often off).
- **Choose the alternative when:** a self-hosted watchdog exists outside the node.

### OpenTelemetry

- **Used for:** the applications push their meters over OTLP to Alloy ([ADR-0023](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0023-application-telemetry.md)).
- **Alternatives:** a `/metrics` endpoint with prometheus-net, a vendor SDK (Datadog, Application Insights), an error tracker (Sentry, GlitchTip).
- **For it:** vendor-neutral, so the back end can change without new code; built into .NET's meters.
- **Against it:** complex specification and libraries; more CPU than a simple counter library in some languages.
- **What practitioners say:** "I got OpenTelemetry to work. But why was it so complicated?" collects the complaints (jargon, poor discoverability), while others answer that the complexity is the price of not being locked to a vendor ([thread](https://news.ycombinator.com/item?id=42655102), [The problem with OpenTelemetry](https://news.ycombinator.com/item?id=40680291)). .NET is singled out as one of the easier ecosystems.
- **Our reason:** ADR-0023, option 2.
- **Choose the alternative when:** traces are needed end to end (add Tempo on the same OTLP path).

### Blackbox exporter

- **Used for:** probing the site over HTTPS, the redirect and the public name from the LXC (`prom/blackbox-exporter`).
- **Alternatives:** Uptime Kuma, Gatus, a hosted uptime service.
- **For it:** results are Prometheus metrics, so the same rules and routes alert on them.
- **Against it:** only as far away as the LXC: it does not see the site from the internet (the public probe goes through Cloudflare).
- **What practitioners say:** few comparison threads; the questions are about configuration. Product documentation (no community source): [blackbox_exporter](https://github.com/prometheus/blackbox_exporter).
- **Our reason:** ADR-0020.
- **Choose the alternative when:** the site must be checked from several places in the world (a hosted service).

---

## The CI/CD pipeline, step by step

The order a change takes from a pull request to production. The details of each workflow are on [Workflows](Workflows); here each step names its tool and why that tool.

| # | Step (workflow, job) | Tool | Why this tool | Alternatives |
| --- | --- | --- | --- | --- |
| 1 | Comments are in English (`ci.yaml`, `comment-language`) | `scripts/check-comment-language.sh`: Danish letters and a word list, in comments only | Rule of the repository; no product checks the language of comments | A spell checker such as cspell (checks words, not the language) |
| 2 | Restore and NuGet vulnerability check (`ci.yaml`, `build`) | `dotnet restore` with NuGetAudit | Built into .NET; fails on a known vulnerable package before anything is built | `dotnet list package --vulnerable`, Snyk |
| 3 | Build (`build`) | `dotnet build` | The SDK of `global.json` | none |
| 4 | SonarQube Cloud analysis (`build`) | `dotnet-sonarscanner` | ADR-0033: wraps the build, so one build and one test run | A separate workflow (a second build) |
| 5 | Check Docker, tests and coverage (`build`) | xUnit, Testcontainers, `dotnet-coverage`, Cobertura | Real PostgreSQL in the tests; coverage in the format SonarQube reads | coverlet only |
| 6 | Formatting (`formatting`) | `dotnet format whitespace --verify-no-changes` | Built into the SDK, reads `.editorconfig` | CSharpier |
| 7 | Docker build (`container`) | `docker build` | Proves the image builds on every pull request | Buildah, Kaniko |
| 8 | Release build without push (`release-check.yaml`) | composite action `release-images` | The same action as the release, so a broken release is found before the merge | none |
| 9 | Code scanning (GitHub default setup) | CodeQL | Free, data-flow analysis, on the pull request | Semgrep |
| 10 | Ops linters (`ops-ci.yaml`, `lint`) | actionlint, shellcheck, shfmt, yamllint, in pinned containers | The standard linter of each file type; containers give the same versions locally and in CI | super-linter (one large image) |
| 11 | Ops tests (`ops-ci.yaml`, the other jobs) | Bash tests in Debian 12 and 13 containers, network namespaces, `promtool`, `amtool`, a real PostgreSQL 16, a throwaway stack | Each script is tested where it runs, without touching a real host | bats-core (a test framework for Bash) |
| 12 | Wiki coverage (`ops-ci.yaml`, `wiki-coverage`) | `ops/tests/test-wiki-coverage.sh` | Keeps the reference pages, this one too, true | none |
| 13 | End-to-end tests (`e2e-dast.yaml`, `e2e`) | Playwright on a throwaway stack; a log privacy check | Real browsers against the real images | Selenium |
| 14 | DAST baseline, signed out and in (`dast-baseline`) | OWASP ZAP | Free, made for CI | Burp Suite |
| 15 | Build, scan and push the images (`release.yaml`) | `docker build`, Trivy (gate), GHCR | ADR-0007: build once, scan before push, deploy by digest | Grype, Docker Hub |
| 16 | Attest provenance and SBOM (`release-images`) | `actions/attest-build-provenance`, `actions/attest`, Trivy SPDX | Keyless, verified by CD | cosign |
| 17 | Keep the release manifest and the migration script (`release.yaml`) | `actions/upload-artifact`; EF Core idempotent script | The digests and the reviewed script travel together to CD (ADR-0008) | Migrations at application start (rejected in ADR-0008) |
| 18 | Wait until every check is green (`cd.yaml`, composite action `deploy-release`) | `ops/scripts/cd-steps.sh gate` with the GitHub API | ADR-0021: deploy only when every check of the commit is green | A manual approval (ADR-0019, replaced) |
| 19 | Verify the attestations (`deploy-release`) | `gh attestation verify` | ADR-0007 decision 3 | cosign verify |
| 20 | Join the tailnet and check its scope (`deploy-release`) | `tailscale/github-action` with OIDC | No stored secret; the node is ephemeral and tagged `tag:ci` | A VPN secret, an open SSH port |
| 21 | Deploy on the VM (`deploy-release`) | Tailscale SSH as `deploy`, which may only run `nfhotel-deploy` | ADR-0007 and ADR-0018: a forced command, updated only from `main` | Ansible, a pull agent on the VM |
| 22 | On the VM: lock, backup, migrate, pull by digest, recreate, secret check, smoke test, rollback (`deploy-release.sh`) | Bash, `pg_dump`, `psql`, Docker Compose, `curl` | Every step testable in Ops CI; the rollback deploys the previous release from the log | Blue-green with two stacks (needs twice the memory) |
| 23 | Scan of the running release, daily (`running-image-scan.yaml`) | Trivy | A new CVE in a running image opens an issue | Docker Scout |
| 24 | DAST full scan, weekly (`dast-full`) | OWASP ZAP | Deeper than the baseline, too slow for each pull request | none |
| 25 | Load test, weekly (`load-test.yaml`) | k6 | Thresholds as pass or fail | JMeter |
| 26 | Scorecard (`scorecard.yaml`) | OpenSSF Scorecard | An outside score of the repository's practices | none |
| 27 | Publish the wiki (`publish-wiki.yaml`) | Own script | ADR-0022: the wiki is reviewed in `docs/wiki` like code | Editing the wiki by hand |

The housekeeping steps of every job (check out, install the .NET SDK, log in to and out of GHCR, keep artifacts and reports, upload SARIF to code scanning) use GitHub's own actions: `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`, `actions/download-artifact` and `github/codeql-action/upload-sarif`, each pinned by commit SHA. They are the platform's standard, and no alternative is worth weighing.

## Backup and restore, step by step

From `backup-database.sh`, `install-backup.sh` and `restore-drill.sh` ([ADR-0008](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0008-database-migration-and-backup.md), [ADR-0025](https://github.com/NF-Hotel/NFHotel/blob/main/docs/adr/adr-0025-recovery-objectives.md)).

| # | Step | Tool | Why this tool | Alternatives |
| --- | --- | --- | --- | --- |
| 1 | Install the script, the settings file and the timer | `install-backup.sh`, systemd | One installer, idempotent; the settings file is root-only (0600) | Ansible |
| 2 | Start every day, and before every deploy and migration | `nfhotel-backup.timer`; called by `deploy-release.sh` | A missed run runs after boot; a fresh dump before each change | cron |
| 3 | Dump inside the PostgreSQL container | `pg_dump --format=custom` as the backup role | No database port is opened; the custom format restores selectively with `pg_restore` | pgBackRest, Barman, WAL-G |
| 4 | Encrypt to the key of S03 | `age` | The VM holds only the public key, so a stolen disk does not reveal the backups | GPG |
| 5 | Archive the settings and secrets needed to rebuild the VM | `tar` piped to `age` | The rebuild needs more than the data (MIL-037) | A Proxmox backup of the VM |
| 6 | Copy off the VM | `rsync` over SSH, key restricted by `rrsync` on the target | Copies only what the target lacks; the key can only write | restic, rclone |
| 7 | Keep 14 daily and 8 weekly files on the VM | Own prune function | A simple rule the restore drill can check | restic `forget` |
| 8 | Report the time of the backup and of the copy | Prometheus text files read by Alloy | An alert when a backup or a copy is late ([Alerts](Alerts)) | Healthchecks.io pings per job |
| 9 | Restore drill, monthly and before a major migration | `restore-drill.sh`: `age` decrypt, a throwaway PostgreSQL container, `pg_restore`, row counts | A backup counts only when a restore has been proven; the drill runs on any machine with Docker | Restoring into the production server |
| 10 | Test the whole chain in CI | Ops CI job "Ops PostgreSQL preparation": backup and restore against a real PostgreSQL 16 | Each change of the scripts is tested before it reaches the VM | none |

## Security products

The controls themselves are on [Security Controls](Security-Controls). Most security products are already above: CodeQL, OWASP ZAP and Trivy (Test), Dependabot, attestations and Scorecard (Release), Tailscale, Cloudflare Tunnel and Access (Deploy), age, nftables, unattended-upgrades and secrets as files (Operate). Two more:

### GitHub secret scanning and push protection

- **Used for:** blocks a push that contains a known secret format and alerts on secrets in the history.
- **Alternatives:** gitleaks or TruffleHog in a pre-commit hook or CI job.
- **For it:** free for public repositories, no setup, partner formats are revoked by the provider.
- **Against it:** finds only formats it knows; own passwords are not found.
- **What practitioners say:** a 2025 report showed a token exposed in public workflow logs that could have been used for a supply-chain attack on GitHub's own CodeQL action; the comments ask for immutable actions and for CI and CD to be separate, both of which NF Hotel does (pinned SHAs, a separate deploy identity) ([Public secrets exposure leads to supply chain attack on GitHub CodeQL](https://news.ycombinator.com/item?id=43527044)).
- **Our reason:** the GitHub default; the secrets themselves never reach git (ADR-0024). Not decided in an ADR.
- **Choose the alternative when:** the code is on a host without secret scanning (gitleaks in CI).

### Docker socket proxy

- **Used for:** Alloy reads container names and health through `tecnativa/docker-socket-proxy`, which allows only GET of containers and networks.
- **Alternatives:** mounting `/var/run/docker.sock` into Alloy, cAdvisor with the socket, no container discovery.
- **For it:** the Docker socket is root on the host; the proxy gives read-only access to two endpoints.
- **Against it:** one more container.
- **What practitioners say:** "Don't expose the docker socket, even to a container" is the classic warning ([thread](https://news.ycombinator.com/item?id=10269200)); the Information Security Stack Exchange question "Is adding docker group not a good idea?" makes the same point for users ([Security Stack Exchange](https://security.stackexchange.com/questions/178542)).
- **Our reason:** MIL-023 hardening; not decided in an ADR.
- **Choose the alternative when:** none for production.

## Index of third-party images

Every image from outside NF Hotel in the compose files and Dockerfiles, and the entry that explains it. `ops/tests/test-wiki-coverage.sh` fails when an image is added without a line here.

| Image | Entry |
| --- | --- |
| `cloudflare/cloudflared` | Cloudflare Tunnel |
| `nginx` | nginx |
| `postgres` | PostgreSQL |
| `haproxy` | HAProxy |
| `ollama/ollama` | Ollama |
| `grafana/alloy` | Grafana Alloy |
| `tecnativa/docker-socket-proxy` | Docker socket proxy |
| `prom/prometheus` | Prometheus |
| `prom/alertmanager` | Alertmanager |
| `prom/blackbox-exporter` | Blackbox exporter |
| `grafana/loki` | Loki |
| `grafana/grafana` | Grafana |
| `dockurr/proxmox` | Proxmox VE |
| `mcr.microsoft.com/dotnet/sdk` | Base images |
| `mcr.microsoft.com/dotnet/aspnet` | Base images |
| `python` | Python, Base images |
| `debian` | Debian, Base images |
