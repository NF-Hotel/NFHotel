# The socket proxy of Alloy (ADR-0040 decision 5, MIL-065 task 4): the pinned upstream image with the
# security fixes of its Alpine packages. The release builds it once, scans it with the gate of
# scan-gate.sh and attests its SBOM and build provenance; CD deploys that digest, and the VM never
# builds it (ADR-0007). apk upgrade installs the packages of the day of the build, so two builds of
# this file can differ: only the scanned digest runs. Dependabot proposes a new build of the tag as
# a change of the FROM line (the docker entry of .github/dependabot.yml). Nothing is copied in: the
# settings stay in the environment of docker-compose.yml, as before.
FROM tecnativa/docker-socket-proxy:v0.5.0@sha256:1f5038b54f06c3e18422902cf00ba21803d1c97805aae032e5e6673d532d3459

# HAProxy is the build of the upstream haproxy image in /usr/local/sbin, not an Alpine package, so
# the upgrade leaves it as it is. The build fails if the upgrade ever replaces it.
RUN before="$(haproxy -v | head -n 1)" \
    && apk upgrade --no-cache \
    && after="$(haproxy -v | head -n 1)" \
    && if [ "$after" != "$before" ]; then echo "apk upgrade replaced $before with $after" >&2; exit 1; fi
