# Patch layer on the frozen market-analysis image (ADR-0030, MIL-042). The source of the
# image, NF-Hotel/Notebook_Market_Analysis at 6c11cbf, was deleted, so the application
# cannot be built again (ADR-0500). This layer starts from the last good digest and adds
# only security fixes: the Debian updates, the Python packages pinned in
# requirements.txt, and the removal of bundled files that the service never uses and
# that carry findings. The application files stay those of the digest, which
# tests/check-patch-layer.sh checks on every pull request.
# The base is a private GHCR package: building needs a login that can read packages.
# Dependabot does not follow this line; the digest must stay in GHCR (ADR-0500).
FROM ghcr.io/nf-hotel/market-analysis@sha256:6bd53d9e59afe80bb3104cebd7bb6e9e03cd1617ea9b1eb89d449968b377a093

USER root

RUN apt-get update \
    && apt-get upgrade --yes --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Hashed pins replace the vulnerable versions; --no-deps, so nothing else moves. pip is
# removed afterwards: the service never installs anything, and the copies of urllib3 and
# other libraries that pip vendors carry findings of their own (as in deploy/benchmark).
COPY requirements.txt /tmp/requirements.txt
RUN pip install --require-hashes --only-binary :all: --no-deps -r /tmp/requirements.txt \
    && rm /tmp/requirements.txt \
    && pip uninstall --yes pip

# marimo bundles the Node.js packages of its Copilot language server, which only its
# editor starts, with Node.js. The image has no Node.js and runs the notebook with
# `marimo run`, so the folder is never used; its zod carries a fixable high finding
# (CVE-2023-54404).
RUN rm -rf /usr/local/lib/python3.13/site-packages/marimo/_lsp/copilot

USER app
WORKDIR /app/work
