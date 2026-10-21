# ADR-0003 Services run in a virtual machine on Proxmox VE

## Metadata
| Key | Value |
| --- | --- |
| ID | ADR-0003 |
| CrossReference | [BC-001], [MIL-004], [US-001], [ADR-0010], [ADR-0016], [RC-161] |

## Version History
| Date | Status | Author | Reviewer | Change | Commit |
| --- | --- | --- | --- | --- | --- |
| 2026-10-18 | Deprecated | mikemike | AI as stand-in for S01 | Decision unchanged; its statements on how the VM is made, the deploy layout and the published ports are replaced by ADR-0010 (RC-098) | [1b94761] |
| 2026-10-21 | Accepted | mikemike | AI as stand-in for S01 | Brought up to date with the repository and nf-hotel (RC-161): path under ops/, size of the production VM | [12a64c3] |

Allowed Status values: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, `Superseded by ADR-NNNN`.

---

## Context

MIL-004 puts the market analysis service (`NF-Hotel/Notebook_Market_Analysis`)
and later the benchmark service (`NF-Hotel/notebook-benchmark`) on Proxmox VE,
as Docker containers behind the nginx proxy (US-001.03, US-001.04, US-001.05).
Three things were open: where the Proxmox host lives, whether the services run
in a virtual machine (VM) or an LXC container, and how a GPU would reach them
for the benchmark.

The throwaway Proxmox VE container of MIL-003 is for tests only and is not the
target. The user decided in chat: use a VM.

Options considered:

1. **A KVM virtual machine** with Docker and the compose plugin inside it.
2. **An LXC container** with Docker inside it. Lighter, but Docker in an
   unprivileged LXC needs nesting and keyctl and weakens the isolation, and the
   container shares the kernel of the host.
3. **Docker directly on the Proxmox host.** No isolation from the hypervisor and
   it changes the host itself, which S03 does not accept.

## Decision

We follow option 1. Each service host is a KVM VM on the Proxmox VE server of
Team 2 (S01, with S03 as its administrator). Docker and the compose plugin run
inside the VM, so the compose files of MIL-002 and MIL-004 work unchanged: one
compose file and one `<service>.env` per service, an external network
`nfhotel-proxy`, and no host port published except by the nginx proxy.

- **Host.** The address of the Proxmox server and its credentials are not in the
  repository. They stay in the git-ignored `pve.env` and in the environment, as
  for the tests (D-07). The VM is created by hand or with `iac-node-forge-py`
  (MIL-004 task 3), and how it was created is recorded there.
- **GPU.** A GPU reaches the VM by PCI passthrough (IOMMU, `vfio-pci`), which is
  a property of VM option 1. It is needed only for the benchmark (US-001.04) and
  is decided with that service; the benchmark result names the machine (CPU,
  memory, GPU) as US-001.04 requires. The market analysis service needs no GPU:
  its local model runs on a host that the container can reach through
  `host.docker.internal`, as in `ops/deploy/market-analysis`.
- **Sizing.** Start with 4 vCPU, 8 GiB memory and 60 GiB disk for the analysis
  VM; the benchmark VM is sized when its GPU is known.

**Current state (2026-10-21, [RC-161]).** The compose files are under `ops/deploy/` (ADR-0016). The production VM is `nf-hotel` (ADR-0010): 4 vCPU, 20 GiB memory, a 111 GB disk, Debian 13 and no GPU, read on the VM. The sizing above was the starting point for the first VM; the default of a new VM is in the wiki page Proxmox Deployment.

## Consequences

**Positive:**

- The services get their own kernel and disk, so a faulty container cannot touch the Proxmox host.
- Docker needs no special LXC settings, and the compose files run the same on a developer machine and on the VM.
- GPU passthrough is possible later without changing the services.

**Negative:**

- A VM uses more memory and disk than an LXC container and takes longer to start.
- GPU passthrough ties the benchmark VM to one physical GPU and needs IOMMU support on the host, which is not yet checked.
- Someone must keep the operating system of the VM up to date.
- Maintainability and security improve (isolation from the host); performance is slightly lower than on an LXC container.

## Affected Artifacts

- [MIL-004] — task 2 is decided here; task 3 creates the VM
- [US-001] — US-001.04 and US-001.05 name the VM
- [ADR-0010] — records the production VM `nf-hotel` as deployed and replaces the out-of-date statements of fact

---

[BC-001]: ../business-case.md
[MIL-004]: ../milestones/mil-004-market-analysis-service.md
[US-001]: ../user-stories.md
[ADR-0010]: ./adr-0010-production-vm-as-deployed.md
[1b94761]: https://github.com/NF-Hotel/NFHotel/commit/1b94761ddb5113cd0d34915c01fe7f0f278bba60
[ADR-0016]: ./adr-0016-one-repository-for-application-and-operations.md
[RC-161]: ../sqa/reviews/rc-161-adr-refresh.md
[12a64c3]: https://github.com/NF-Hotel/NFHotel/commit/12a64c3225c75ebc1b83a0e6d0ee08115ec1a493
