# NF Hotel

NF Hotel is a web-based hotel management system built with Blazor, PostgreSQL and a
clean-architecture .NET solution. This wiki is written for the people who build and run it.

## Pages

- [Local Development Deploy](Local-Development-Deploy): run the whole deployment on your own machine with one
  script, to see a change working. Start here.
- [Proxmox Deployment](Proxmox-Deployment): moved to NF-Hotel/DevOps; the page points there.
- [Appendix: Project Documents](Appendix): links to every document under `docs/`.
- Infrastructure and operations (CI/CD, deployment, database preparation, Proxmox): the wiki pages of [NF-Hotel/DevOps](https://github.com/NF-Hotel/DevOps/tree/main/docs/wiki) (ADR-0012).

## Where things live

| What | Where |
| --- | --- |
| Source code | `src/` (Domain, Application, Infrastructure, Web) |
| Tests | `tests/` |
| Container definitions, deploy scripts, Proxmox tests | [NF-Hotel/DevOps](https://github.com/NF-Hotel/DevOps) (`deploy/`, `scripts/`, `tests/`) |
| Development deploy on your machine | `scripts/dev-up.sh`, `scripts/dev-up.ps1` (they use a DevOps checkout) |
| Project documents | `docs/` |
