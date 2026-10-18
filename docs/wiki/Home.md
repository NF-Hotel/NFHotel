# NF Hotel

NF Hotel is a web-based hotel management system built with Blazor, PostgreSQL and a
clean-architecture .NET solution. This wiki is written for the people who build and run it.

## Pages

- [Proxmox Deployment](Proxmox-Deployment): set up the Proxmox virtual machine and the Docker
  containers that run the whole system (development environment).
- [CI/CD](CI-CD): how a release is built, approved, deployed, rolled back, and how the
  database is backed up and restored; the one-time setup.
- [Appendix: Project Documents](Appendix): links to every document under `docs/`.

## Where things live

| What | Where |
| --- | --- |
| Source code | `src/` (Domain, Application, Infrastructure, Web, DataAnalysis) |
| Tests | `tests/` |
| Container definitions | `deploy/` |
| Deploy scripts | `scripts/deploy.sh`, `scripts/deploy-services.sh`, `scripts/create-docker-vm.sh` |
| Project documents | `docs/` |
