# NF Hotel

NF Hotel is a web-based hotel management system built with Blazor, PostgreSQL and a
clean-architecture .NET solution. This wiki is written for the people who build and run it.

## Pages

- [Local Development Deploy](Local-Development-Deploy): run the whole deployment on your own machine with one
  script, to see a change working. Start here.
- [Proxmox Deployment](Proxmox-Deployment): set up the Proxmox virtual machine and the Docker
  containers that run the whole system (development environment).
- [Appendix: Project Documents](Appendix): links to every document under `docs/`.
- Infrastructure and operations (CI/CD, deployment, database preparation, Proxmox): the wiki pages of [NF-Hotel/DevOps](https://github.com/NF-Hotel/DevOps/tree/main/docs/wiki) (ADR-0012).

## Where things live

| What | Where |
| --- | --- |
| Source code | `src/` (Domain, Application, Infrastructure, Web, DataAnalysis) |
| Tests | `tests/` |
| Container definitions | `deploy/` |
| Deploy scripts | `scripts/deploy.sh`, `scripts/deploy-services.sh`, `scripts/create-docker-vm.sh` |
| Project documents | `docs/` |
