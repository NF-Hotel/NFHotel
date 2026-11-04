# The write-only target of the off-VM backup copy (DSA-001 decision D7, ADR-0008,
# MIL-021 task 7). It runs on the backup host, for example the legion laptop of S03, and
# accepts one key: the one of root on nf-hotel, restricted by rrsync to writing new
# files into /backups. It can neither read back nor delete, nor open a shell.
FROM debian:12-slim@sha256:99c0b25c46d0214d5a2091092e7908907dcaecd6a962cde0ac700acee222b6f3

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssh-server rsync python3 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/sh nfhotel-backup \
    && passwd --lock nfhotel-backup \
    && install -d -o nfhotel-backup -m 0700 /backups

# No EXPOSE: the compose file publishes ssh on the tailnet address of the host only
# (SonarCloud docker:S6473, MIL-023 task 12).
COPY entrypoint.sh /usr/local/sbin/backup-target
ENTRYPOINT ["/usr/local/sbin/backup-target"]
