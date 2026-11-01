# GPU Passthrough

This page shows how to give an NVIDIA GPU to the NFHotel VM on a Proxmox VE host, so that Ollama (the AI models of the
market analysis and the benchmark) runs on the GPU instead of the CPU. It follows ADR-0003, where the services run in one
VM on Proxmox VE.

## When it works

Passing a PCI device to a VM (VFIO passthrough) needs real hardware under Proxmox VE:

| Setup | PCI passthrough | What to do instead |
| --- | --- | --- |
| Proxmox VE installed on a Linux machine with a GPU | Yes, follow this page | |
| The development deploy (`scripts/dev-up.sh`) on Linux | Not supported. Proxmox VE runs inside a Docker container there, and the VM is nested | Run Ollama on the host and point the VM at it (see [Local Development Deploy](Local-Development-Deploy)) |
| The development deploy on Windows (Docker Desktop, WSL2) | No. Docker Desktop shares the GPU with containers as a paravirtual device (`/dev/dxg`), not as a PCI device a VM can take | Run Ollama on Windows and point the VM at it (see [Local Development Deploy](Local-Development-Deploy)) |

The host loses the GPU while the VM owns it: the host console cannot use it. Use a second GPU, an integrated GPU or no
local display (manage the host over SSH and the web interface).

## What you need

- A CPU and a board with IOMMU: Intel VT-d or AMD-Vi. Turn both IOMMU and virtualization on in the BIOS (on some boards
  the options are called "VT-d", "IOMMU" or "SVM"). Turn on "Above 4G decoding" and, when it exists, "Resizable BAR"
  support as the board's manual says.
- A UEFI VM (OVMF) with the q35 machine type. The VM of the deploy is a Debian cloud image; check its firmware with
  `qm config <vmid>`.
- The GPU in an IOMMU group of its own, or only with its own audio function (see "Check the IOMMU groups").
- Enough video memory for the model: the models only need a few GiB for the small models of the deploy, for example
  `llama3.2:3b` (about 2 GB) or `phi3.5` (about 2.5 GB).

## 1. Turn on IOMMU on the Proxmox VE host

Edit the kernel command line. Which file depends on how the host boots (`proxmox-boot-tool status` tells).

With GRUB, add the options to `GRUB_CMDLINE_LINUX_DEFAULT` in `/etc/default/grub`, then run `update-grub`:

```text
GRUB_CMDLINE_LINUX_DEFAULT="quiet intel_iommu=on iommu=pt"
```

On AMD the IOMMU is on by default; `iommu=pt` is enough.

With systemd-boot (ZFS root with UEFI), edit `/etc/kernel/cmdline` as one line and run `proxmox-boot-tool refresh`.

Load the VFIO modules at boot by adding these lines to `/etc/modules`:

```text
vfio
vfio_iommu_type1
vfio_pci
```

Reboot, then check that the IOMMU is on:

```bash
dmesg | grep -e DMAR -e IOMMU
```

You should see a line such as `DMAR: IOMMU enabled` (Intel) or `AMD-Vi: ... enabled`.

## 2. Check the IOMMU groups

The device that you pass on must not share its group with something the host needs (the SATA controller, for example).

```bash
for g in /sys/kernel/iommu_groups/*; do
  echo "group ${g##*/}:"
  for d in "$g"/devices/*; do echo "  $(lspci -nns "${d##*/}")"; done
done
```

A GPU normally has two functions in one group: the video function (`01:00.0`) and its audio function (`01:00.1`). Pass
both. When the GPU shares a group with other devices, try another PCI slot before you reach for ACS override patches,
which weaken the isolation between devices.

## 3. Give the GPU to VFIO and keep the host driver away

Find the vendor and device IDs of both functions:

```bash
lspci -nn | grep -i nvidia
```

An example line is `01:00.0 VGA compatible controller [0300]: NVIDIA ... [10de:2507]`. Put the IDs in
`/etc/modprobe.d/vfio.conf`:

```text
options vfio-pci ids=10de:2507,10de:228e
softdep nouveau pre: vfio-pci
softdep nvidia pre: vfio-pci
```

Stop the host from loading its own driver for the card, in `/etc/modprobe.d/blacklist-gpu.conf`:

```text
blacklist nouveau
blacklist nvidia
blacklist nvidiafb
```

Rebuild the initial RAM disk and reboot:

```bash
update-initramfs -u -k all
reboot
```

Check that VFIO owns the card:

```bash
lspci -nnk -s 01:00
```

Both functions must say `Kernel driver in use: vfio-pci`.

## 4. Add the GPU to the VM

Use the q35 machine type and OVMF firmware. If the VM was made with the default `i440fx` and SeaBIOS, change both (the
disk needs an EFI disk added, `qm set <vmid> --efidisk0 <storage>:1`). Then add the device:

```bash
qm set <vmid> --machine q35 --bios ovmf
qm set <vmid> --cpu host
qm set <vmid> --hostpci0 0000:01:00,pcie=1
qm set <vmid> --balloon 0
```

`0000:01:00` without a function number passes all functions of the device (video and audio). `--balloon 0` is needed
because a VM with a passed device keeps all of its memory pinned. Start the VM and check the card inside it:

```bash
lspci | grep -i nvidia
```

## 5. Install the driver and the container toolkit in the VM

The VM is Debian. Enable `contrib`, `non-free` and `non-free-firmware` in `/etc/apt/sources.list.d/`, then:

```bash
sudo apt install linux-headers-amd64 nvidia-driver
sudo reboot
nvidia-smi
```

`nvidia-smi` must list the GPU. Then install the NVIDIA Container Toolkit as NVIDIA's installation guide says for Debian
(add their apt repository, then `sudo apt install nvidia-container-toolkit`) and make Docker use it:

```bash
sudo nvidia-ctk runtime configure --runtime=docker
sudo systemctl restart docker
docker run --rm --gpus all debian:12-slim nvidia-smi
```

The last command must print the same table as `nvidia-smi` on the VM.

## 6. Let Ollama use the GPU

In `ops/deploy/ollama/docker-compose.yml`, add this under the `ollama` service (the file has it as a comment):

```yaml
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
```

Then recreate Ollama (from `/opt/nfhotel/deploy/ollama` in the VM):

```bash
docker compose --env-file ollama.env up -d --force-recreate
docker logs ollama-ollama-1 2>&1 | grep -i -e gpu -e cuda
```

The log must name the GPU. Run a model and watch `nvidia-smi` on the VM: the memory use grows while it answers. The
market analysis runs then take seconds to minutes, not the 12 minutes or more of a CPU, so the
`MarketAnalysisService__RunDeadlineSeconds` of `nfhotel-api.env` can go back to its default.

## Things to know

- **One VM per GPU.** A passed device belongs to one running VM at a time.
- **Backups and snapshots.** A VM with a passed device cannot be live-migrated, and a snapshot that includes the RAM is
  not possible. A `vzdump` backup in stop mode works; see [Disaster Recovery](Disaster-Recovery).
- **Error 43 and consumer cards.** Older NVIDIA drivers refused GeForce cards inside a VM. Current drivers (R465 and
  later) do not. If an old driver does, set `--cpu host,hidden=1` on the VM.
- **Reset problems.** Some cards do not reset cleanly when the VM stops, and the host must then be rebooted to start the
  VM again. Stop and start the VM once after the first setup to find out.
- **Security.** A passed GPU can read the memory of the VM and runs the driver's firmware. Pass it only to the VM of
  this deploy and keep [Host Hardening](Host-Hardening) in place.
- **The host steps are manual.** The `ops/` deploy scripts change the VM only; the steps on the Proxmox VE host are done
  by hand.
