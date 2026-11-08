# The reverse proxy of NFHotel (ADR-0040, MIL-064 task 2): the pinned upstream image with the
# security fixes of its Alpine packages. The release builds it once, scans it with the gate of
# scan-gate.sh and attests its SBOM and build provenance; CD deploys that digest, and the VM never
# builds it (ADR-0007). apk upgrade installs the packages of the day of the build, so two builds of
# this file can differ: only the scanned digest runs. Dependabot proposes a new build of the tag as
# a change of the FROM line (the docker entry of .github/dependabot.yml). Nothing is copied in: the
# templates, snippets and certificates are mounted by docker-compose.yml, as before.
FROM nginx:1.29-alpine@sha256:5616878291a2eed594aee8db4dade5878cf7edcb475e59193904b198d9b830de

# nginx itself is the build of nginx.org, and the Alpine repositories of the image carry an older
# nginx, so the upgrade leaves it as it is. The build fails if the upgrade ever replaces it.
RUN before="$(nginx -v 2>&1)" \
    && apk upgrade --no-cache \
    && after="$(nginx -v 2>&1)" \
    && if [ "$after" != "$before" ]; then echo "apk upgrade replaced $before with $after" >&2; exit 1; fi
