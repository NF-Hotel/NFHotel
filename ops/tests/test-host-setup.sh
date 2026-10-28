#!/usr/bin/env bash
# Tests of the host setup scripts of MIL-023 tasks 2 and 4: install-docker-daemon.sh (a dry
# run by default; --apply installs daemon.json with its settings and keeps the old file) and
# install-patching.sh (a dry run by default; --apply allows Debian security updates only, and
# a second run changes nothing), install-host-settings.sh (a dry run by default; --apply gives
# sshd, sudo and sysctl the settings of CIS-001, takes back a configuration that sshd refuses,
# and a second run changes nothing), and cis-evidence.sh (it runs to the end on a system
# without Docker or the hardening).
#
# Run it in Debian 13, the system of the VM, with the repository at /src:
#   docker run --rm -v "$PWD:/src:ro" debian:13-slim bash /src/tests/test-host-setup.sh
# Exit:  0 every check passed; 1 a check failed
# Needs: Debian 13 (apt-get)

# shellcheck disable=SC2015 # ok and ko always return 0, so `test && ok || ko` runs
# exactly one of them; the short form keeps each check on one line.
set -uo pipefail

apt-get -qq update >/dev/null
apt-get -qq install -y jq openssh-server sudo procps >/dev/null 2>&1
pass=0
fail=0
ok() {
  echo "PASS  $1"
  pass=$((pass + 1))
}
ko() {
  echo "FAIL  $1"
  fail=$((fail + 1))
}

echo "== install-docker-daemon.sh"
export NFHOTEL_DOCKER_DAEMON_JSON=/tmp/docker/daemon.json
readonly DAEMON=/src/scripts/install-docker-daemon.sh
out="$(bash "$DAEMON")"
[[ "$out" == *"+  \"live-restore\": true,"* && ! -e "$NFHOTEL_DOCKER_DAEMON_JSON" ]] &&
  ok "the default is a dry run that shows the change and writes nothing" || ko "dry run: $out"
bash "$DAEMON" --restart >/dev/null 2>&1 && ko "accepted --restart without --apply" || ok "--restart needs --apply"
bash "$DAEMON" --apply >/dev/null && ok "--apply installs" || ko "--apply"
[[ "$(jq -c '[."live-restore", ."no-new-privileges", ."userland-proxy", ."log-driver", ."log-opts"."max-size", .icc]' "$NFHOTEL_DOCKER_DAEMON_JSON")" == '[true,true,false,"local","10m",false]' ]] && ok "daemon.json has the settings of CIS-001" || ko "settings"
[[ "$(stat -c '%U %a' "$NFHOTEL_DOCKER_DAEMON_JSON")" == "root 644" ]] && ok "it is root's, mode 644" || ko "mode"
[[ "$(bash "$DAEMON" --apply)" == *"nothing to install"* ]] && ok "a second run changes nothing" || ko "second run"
echo '{"debug": true}' >"$NFHOTEL_DOCKER_DAEMON_JSON"
bash "$DAEMON" --apply >/dev/null && [[ "$(cat /tmp/docker/daemon.json.*.bak)" == '{"debug": true}' ]] &&
  ok "a changed file is kept as a .bak before it is replaced" || ko "backup"

echo "== install-patching.sh"
readonly PATCHING=/src/scripts/install-patching.sh
[[ "$(bash "$PATCHING" | grep -c '^would')" -ge 4 && ! -e /etc/apt/apt.conf.d/52nfhotel-unattended-upgrades ]] &&
  ok "the default is a dry run that installs nothing" || ko "dry run"
bash "$PATCHING" --apply >/tmp/patching.log 2>&1 && ok "--apply installs and its check passes" || ko "--apply: $(tail -3 /tmp/patching.log)"
origins="$(apt-config dump Unattended-Upgrade::Origins-Pattern | grep '^Unattended-Upgrade::Origins-Pattern:: ')"
[[ "$origins" == *"-security,label=Debian-Security"* && "$(wc -l <<<"$origins")" == 1 ]] &&
  ok "only the Debian security origin is allowed" || ko "origins: $origins"
[[ "$(apt-config dump Unattended-Upgrade::Automatic-Reboot)" == *'"false"'* ]] && ok "no automatic reboot" || ko "reboot"
[[ "$(apt-config dump APT::Periodic::Unattended-Upgrade)" == *'"1"'* ]] && ok "it runs daily" || ko "periodic"
# Captured first: grep -q would end the pipe early, and pipefail would count that as a failure.
dry="$(unattended-upgrade --dry-run --debug 2>&1)"
[[ "$dry" == *"Allowed origins are: origin=Debian,codename=trixie-security,label=Debian-Security"* ]] &&
  ok "unattended-upgrade resolves it to trixie-security" || ko "unattended-upgrade dry run"
[[ -f /etc/systemd/journald.conf.d/nfhotel.conf ]] && grep -q '^SystemMaxUse=1G$' /etc/systemd/journald.conf.d/nfhotel.conf &&
  ok "the journal is bounded" || ko "journald"
bash "$PATCHING" --apply >/dev/null 2>&1 && ok "a second run succeeds" || ko "second run"

echo "== install-host-settings.sh"
readonly SETTINGS=/src/scripts/install-host-settings.sh
[[ "$(bash "$SETTINGS" | grep -c '^would run: install')" == 4 && ! -e /etc/ssh/sshd_config.d/nfhotel.conf ]] &&
  ok "the default is a dry run that installs nothing" || ko "dry run"
bash "$SETTINGS" --apply >/tmp/settings.log 2>&1 && ok "--apply installs and sshd -t accepts it" || ko "--apply: $(tail -3 /tmp/settings.log)"
sshd_settings="$(sshd -T 2>/dev/null)"
for setting in "permitrootlogin no" "passwordauthentication no" "kbdinteractiveauthentication no" \
  "disableforwarding yes" "maxauthtries 4" "logingracetime 60" "permituserenvironment no" "loglevel VERBOSE"; do
  grep -qx "$setting" <<<"$sshd_settings" && ok "sshd: $setting" || ko "sshd: $setting"
done
grep -E '^(ciphers|macs|kexalgorithms) ' <<<"$sshd_settings" | grep -qiE 'sha1|umac-64|cbc|diffie-hellman-group(1|14)-' &&
  ko "sshd offers a weak algorithm" || ok "sshd offers no SHA-1, 64-bit MAC or CBC algorithm"
[[ "$(stat -c '%U %a' /etc/sudoers.d/nfhotel-logging)" == "root 440" ]] && visudo -cq &&
  ok "the sudo rule is root's, mode 440, and visudo accepts the whole sudoers" || ko "sudo rule"
sudo -n true && grep -q 'COMMAND=/usr/bin/true' /var/log/sudo.log && ok "sudo writes its log file" || ko "sudo log"
grep -q '^net.ipv4.conf.all.send_redirects = 0$' /etc/sysctl.d/60-nfhotel.conf && ok "the sysctl file is installed" || ko "sysctl file"
[[ "$(bash "$SETTINGS" --apply | grep -c 'is up to date')" == 4 ]] && ok "a second run changes nothing" || ko "second run"
# A changed file beside a drop-in that sshd refuses: the install must put the changed file back.
echo "# changed by hand" >>/etc/ssh/sshd_config.d/nfhotel.conf
cp /etc/ssh/sshd_config.d/nfhotel.conf /tmp/nfhotel.conf.before
echo "NoSuchSetting yes" >/etc/ssh/sshd_config.d/00-broken.conf
bash "$SETTINGS" --apply >/dev/null 2>&1 && ko "accepted a configuration that sshd refuses" ||
  { cmp -s /etc/ssh/sshd_config.d/nfhotel.conf /tmp/nfhotel.conf.before && ok "a refused configuration is taken back" || ko "take back"; }
rm /etc/ssh/sshd_config.d/00-broken.conf

echo "== cis-evidence.sh"
evidence="$(bash /src/scripts/cis-evidence.sh 2>&1)"
[[ $? == 0 && "$evidence" == *"===== end of evidence"* && "$(grep -c '^===== ' <<<"$evidence")" == 11 ]] &&
  ok "it prints every section and ends" || ko "evidence: $(tail -5 <<<"$evidence")"

echo "RESULT: $pass passed, $fail failed"
((fail == 0))
