# NF Hotel

NF Hotel is a web-based hotel management system built with Blazor, PostgreSQL and a
clean-architecture .NET solution. This wiki is written for the people who build and run it.

## Pages

- [Local Development Deploy](Local-Development-Deploy): run the whole deployment on your own machine with one
  script, to see a change working. Start here.
- [Proxmox Deployment](Proxmox-Deployment): set up the Proxmox virtual machine and the Docker
  containers that run the whole system (development environment).
- [Appendix: Project Documents](Appendix): links to every document under `docs/`.
- Infrastructure and operations (ADR-0016, in `ops/`): [CI-CD](CI-CD), [Database Preparation](Database-Preparation) and [Proxmox Deployment](Proxmox-Deployment).

## Where things live

| What | Where |
| --- | --- |
| Source code | `src/` (Domain, Application, Infrastructure, Web) |
| Tests | `tests/` |
| Container definitions | `ops/deploy/` |
| Deploy scripts | `ops/scripts/deploy.sh`, `ops/scripts/deploy-services.sh`, `ops/scripts/create-docker-vm.sh` |
| Project documents | `docs/` |
