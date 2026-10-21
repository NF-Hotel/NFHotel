# The write-only target of the off-VM backup copy (DSA-001 decision D7, ADR-0008,
# MIL-021 task 7). It runs on the backup host, for example the legion laptop of S03, and
# accepts one key: the one of root on nf-hotel, restricted by rrsync to writing new
# files into /backups. It can neither read back nor delete, nor open a shell.
FROM debian:12-slim@sha256:7ff6ba31527b9e336162fa061746c366c4b2032f4d2cff2078bea43f78efe3f8

RUN apt-get update \
    && apt-get install -y --no-install-recommends openssh-server rsync python3 \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/sh nfhotel-backup \
    && passwd --lock nfhotel-backup \
    && install -d -o nfhotel-backup -m 0700 /backups

COPY entrypoint.sh /usr/local/sbin/backup-target
EXPOSE 22
ENTRYPOINT ["/usr/local/sbin/backup-target"]
