# NF Hotel

NF Hotel is a web-based hotel management system built with Blazor, PostgreSQL and a
clean-architecture .NET solution. This wiki is written for the people who build and run it.

The pages are written in [`docs/wiki/`](https://github.com/NF-Hotel/NFHotel/tree/main/docs/wiki) of the repository and reviewed there in pull requests. The workflow `NF Hotel Wiki` publishes them here after every merge (ADR-0022). An edit made directly in this wiki is overwritten at the next publish, so change the page in `docs/wiki/` instead.

## Pages

- [DevOps Lifecycle](DevOps-Lifecycle): how the project covers Plan, Code, Build, Test, Release, Deploy, Operate and
  Monitor, with the tools it uses; start here for the whole picture.
- [Local Development Deploy](Local-Development-Deploy): run the whole deployment on your own machine with one
  script, to see a change working. Start here.
- [Environment Files](Environment-Files): what every `*.env` file of the deployment holds, and which secrets must match.
- [Proxmox Deployment](Proxmox-Deployment): set up the Proxmox virtual machine and the Docker
  containers that run the whole system (development environment).
- [Appendix: Project Documents](Appendix): links to every document under `docs/`.
- Infrastructure and operations (ADR-0016, in `ops/`): [CI-CD](CI-CD), [Database Preparation](Database-Preparation), [Proxmox Deployment](Proxmox-Deployment), [Host Hardening](Host-Hardening), [Monitoring](Monitoring), [Disaster Recovery](Disaster-Recovery) and [Public Access](Public-Access), and the incident procedure in [Incidents](Incidents).
- Reference pages (MIL-049): every DevOps and security choice with why and its alternatives, in [Workflows](Workflows), [Containers and Images](Containers-and-Images), [Security Controls](Security-Controls) and [Alerts](Alerts).

## Where things live

| What | Where |
| --- | --- |
| Source code | `src/` (Domain, Application, Infrastructure, Web) |
| Tests | `tests/` |
| Container definitions | `ops/deploy/` |
| Deploy scripts | `ops/scripts/deploy.sh`, `ops/scripts/deploy-services.sh`, `ops/scripts/create-docker-vm.sh` |
| Project documents | `docs/` |
