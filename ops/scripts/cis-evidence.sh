#!/usr/bin/env bash
# Collect the evidence of the CIS checklist (docs/sqa/cis-checklist.md, CIS-001; MIL-023 task 1
# and criterion 1) on the NF Hotel VM. S01 or S03 runs it as root on nf-hotel, and records
# the output with the checklist. Every section is named ev:<name>, as the checklist cites it.
#
# It is read-only: it only reads configuration and state (docker info and inspect, nft list,
# sshd -T, sysctl, stat, systemctl, apt-config) and changes nothing. It prints no secret: no
# environment of a container, no settings file of deploy/, no key. A command that is missing
# prints "(not available)", so the script runs before and after the hardening.
#
# Usage: cis-evidence.sh > cis-evidence-<date>.txt
# Exit:  0 always, when it runs as root; 1 not root
# Needs: bash 4+, root (sshd -T and the Docker socket need it)

# shellcheck disable=SC2016 # the awk programs and format strings in single quotes are
# for the command they are given to, not for the shell.
set -euo pipefail

[[ "$(id -u)" == 0 ]] || {
  echo "error: run it as root (sudo)" >&2
  exit 1
}

section() { printf '\n===== %s\n' "$*"; }
# show <command...>: run a read-only command; a missing command or a failure is noted, not fatal.
show() {
  printf -- '--- %s\n' "$*"
  if command -v "$1" >/dev/null 2>&1; then
    "$@" 2>&1 || echo "(exit $?)"
  else
    echo "(not available)"
  fi
}

section "ev:host (Debian 1.2, Docker 1.2.2)"
echo "date: $(date -u +%FT%TZ)"
show grep PRETTY_NAME /etc/os-release
show uname -r
[[ -f /var/run/reboot-required ]] && echo "reboot required: $(cat /var/run/reboot-required.pkgs 2>/dev/null | tr '\n' ' ')" ||
  echo "reboot required: no"
show apt-get -s -o Debug::NoLocking=1 upgrade
show apt-cache policy

section "ev:docker (Docker 1.1, 2, 3, 7)"
show docker version --format 'client {{.Client.Version}} server {{.Server.Version}}'
show docker info --format 'live-restore={{.LiveRestoreEnabled}} log-driver={{.LoggingDriver}} storage={{.Driver}} cgroup={{.CgroupVersion}} security={{.SecurityOptions}} experimental={{.ExperimentalBuild}} insecure-registries={{.RegistryConfig.InsecureRegistryCIDRs}} swarm={{.Swarm.LocalNodeState}}'
show cat /etc/docker/daemon.json
show stat -c '%n %U:%G %a' /etc/docker /etc/docker/daemon.json /var/run/docker.sock /lib/systemd/system/docker.service /lib/systemd/system/docker.socket /run/containerd/containerd.sock
show getent group docker
show findmnt -T /var/lib/docker

section "ev:containers (Docker 4, 5, 6)"
for id in $(docker ps --quiet 2>/dev/null); do
  docker inspect --format '{{.Name}}
  image={{.Config.Image}} user={{if .Config.User}}{{.Config.User}}{{else}}(image default){{end}} health={{with index .State "Health"}}{{.Status}}{{else}}none{{end}}
  cap_drop={{.HostConfig.CapDrop}} cap_add={{.HostConfig.CapAdd}} security_opt={{.HostConfig.SecurityOpt}} apparmor={{.AppArmorProfile}} privileged={{.HostConfig.Privileged}}
  read_only={{.HostConfig.ReadonlyRootfs}} memory={{.HostConfig.Memory}} nano_cpus={{.HostConfig.NanoCpus}} pids_limit={{.HostConfig.PidsLimit}}
  network={{.HostConfig.NetworkMode}} pid={{.HostConfig.PidMode}} ipc={{.HostConfig.IpcMode}} uts={{.HostConfig.UTSMode}} userns={{.HostConfig.UsernsMode}}
  restart={{.HostConfig.RestartPolicy.Name}} log={{.HostConfig.LogConfig.Type}} {{.HostConfig.LogConfig.Config}} devices={{.HostConfig.Devices}}
  ports={{range $p, $b := .NetworkSettings.Ports}}{{$p}}->{{range $b}}{{.HostIp}}:{{.HostPort}} {{end}}{{end}}
  mounts={{range .Mounts}}{{.Type}}:{{.Source}}->{{.Destination}}({{if .RW}}rw{{else}}ro{{end}},{{.Propagation}}) {{end}}' "$id"
done

section "ev:firewall (Debian 2.1, 4; Docker 2.7, 5.13; MIL-023 criterion 2)"
show nft list tables
show nft list table inet nfhotel
for unit in nfhotel-firewall nftables ufw docker tailscaled; do
  echo "$unit: enabled=$(systemctl is-enabled "$unit" 2>/dev/null || true) active=$(systemctl is-active "$unit" 2>/dev/null || true)"
done
show ss -tlnH
show ss -ulnH

section "ev:patching (Debian 1.2.2.1, 6.1; MIL-023 criterion 4)"
show dpkg-query -W -f '${Package} ${Version}\n' unattended-upgrades logrotate
show apt-config dump Unattended-Upgrade::Origins-Pattern APT::Periodic Unattended-Upgrade::Automatic-Reboot
show tail -n 20 /var/log/unattended-upgrades/unattended-upgrades.log
show systemctl list-timers apt-daily.timer apt-daily-upgrade.timer --no-pager
show systemd-analyze cat-config systemd/journald.conf
show journalctl --disk-usage

section "ev:ssh (Debian 5.1, 5.3, 5.4.1)"
show sshd -T
show stat -c '%n %U:%G %a' /etc/ssh/sshd_config
show tailscale status --self --peers=false

section "ev:sysctl (Debian 1.5, 3.3)"
show sysctl kernel.randomize_va_space kernel.yama.ptrace_scope fs.suid_dumpable net.ipv4.ip_forward \
  net.ipv4.conf.all.send_redirects net.ipv4.conf.default.send_redirects net.ipv4.icmp_ignore_bogus_error_responses \
  net.ipv4.icmp_echo_ignore_broadcasts net.ipv4.conf.all.accept_redirects net.ipv4.conf.default.accept_redirects \
  net.ipv4.conf.all.secure_redirects net.ipv4.conf.all.rp_filter net.ipv4.conf.default.rp_filter \
  net.ipv4.conf.all.accept_source_route net.ipv4.conf.all.log_martians net.ipv4.tcp_syncookies \
  net.ipv6.conf.all.accept_ra net.ipv6.conf.all.accept_redirects net.ipv6.conf.all.forwarding

section "ev:mac-fs (Debian 1.1, 1.3; Docker 5.1)"
show cat /sys/module/apparmor/parameters/enabled
# aa-status of Debian 13 has no --summary; its counts are the lines before the profile lists.
printf -- '--- %s
' "aa-status (counts)"
aa-status 2>&1 | grep -E 'module is loaded|^[0-9]+ (profiles|processes)' || echo "(not available)"
show findmnt -n -o TARGET,SOURCE,FSTYPE,OPTIONS /tmp /dev/shm /var /var/log /home
for module in cramfs freevxfs hfs hfsplus jffs2 udf usb-storage dccp tipc rds sctp; do
  echo "module $module: loaded=$(grep -qw "^${module//-/_}" /proc/modules && echo yes || echo no) install=$(modprobe -n -v "$module" 2>&1 | tail -n 1)"
done

section "ev:services (Debian 2.1 to 2.3)"
show systemctl list-units --type=service --state=running --no-pager --plain --no-legend
show timedatectl show -p NTP -p NTPSynchronized

section "ev:accounts (Debian 5.2, 5.4.2, 7)"
show grep -rhE '^Defaults.*(use_pty|logfile)' /etc/sudoers /etc/sudoers.d
show stat -c '%n %U:%G %a' /etc/sudoers.d/nfhotel-logging /etc/ssh/sshd_config.d/nfhotel.conf /etc/sysctl.d/60-nfhotel.conf
show awk -F: '$3 == 0 { print "uid 0: " $1 }' /etc/passwd
show awk -F: '$2 == "" { print "empty password: " $1 }' /etc/shadow
show stat -c '%n %U:%G %a' /etc/passwd /etc/shadow /etc/group /etc/gshadow /etc/passwd- /etc/shadow-
show find / -xdev -type f -perm -0002 -not -path '/proc/*' -not -path '/sys/*' -print -quit
show find / -xdev \( -nouser -o -nogroup \) -not -path '/var/lib/docker/*' -print -quit
echo
echo "===== end of evidence"
