# The write-only target of the off-VM backup copy (DSA-001 decision D7, ADR-0008,
# MIL-021 task 7). It runs on the backup host, for example the legion laptop of S03, and
# accepts one key: the one of root on nf-hotel, restricted by rrsync to writing new
# files into /backups. It can neither replace, delete nor read back a file, nor open a shell.
# Debian 13, because only its rrsync has -no-overwrite (MIL-066 task 1). The same image runs
# prune-backups.sh in the service prune, which deletes the copies that the retention no
# longer keeps (MIL-069).
FROM debian:13-slim@sha256:a72b4433de1cf7d6bae0c4cee74d0547f58601a643b3375f070c51600d63b6b2

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssh-server rsync python3 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/sh nfhotel-backup \
    && passwd --lock nfhotel-backup \
    && install -d -o nfhotel-backup -m 0700 /backups

# No EXPOSE: the compose file publishes ssh on the tailnet address of the host only
# (SonarCloud docker:S6473, MIL-023 task 12).
# --chmod: the scripts are executable whatever the mode of the checkout they are built from.
COPY --chmod=0755 entrypoint.sh /usr/local/sbin/backup-target
COPY --chmod=0755 prune-backups.sh /usr/local/sbin/prune-backups
ENTRYPOINT ["/usr/local/sbin/backup-target"]
