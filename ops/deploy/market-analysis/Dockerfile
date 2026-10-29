# Image of the market analysis service of NF-Hotel/notebook-market-analysis (MIL-001 there): the HTTP API
# that the NFHotel web application calls for the holiday calendar and the market analysis runs. That
# repository has a Dockerfile of its own, but it installs with pip from version ranges; this one fetches
# the source at a pinned commit and installs locked, hashed dependencies instead (as deploy/benchmark
# does). Change MARKET_ANALYSIS_REF to upgrade. Build context is this folder.
# The package is not installed: its source stays at /opt/market-analysis/src on PYTHONPATH, so no build
# backend runs. requirements.lock pins every dependency with hashes for the pinned commit. After changing
# MARKET_ANALYSIS_REF, regenerate it from that commit's pyproject.toml:
#   uv pip compile pyproject.toml --generate-hashes --python-platform linux \
#     --python-version 3.13 --no-header -o requirements.lock
# Pinned by digest; Dependabot proposes the next one.
FROM python:3.13-slim@sha256:2d44738ebf4ce5f63b9d9ea871e9c7cfdc0426f7b0c97cac915fd88ded1d86bb

ARG MARKET_ANALYSIS_REPO
ARG MARKET_ANALYSIS_REF
ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

# apt-get upgrade installs the Debian security updates published since the base image was built
# (ADR-0032: a finding with a fix is acted on).
RUN apt-get update \
    && apt-get upgrade --yes --no-install-recommends \
    && apt-get install --yes --no-install-recommends git ca-certificates \
    && git init /opt/market-analysis \
    && git -C /opt/market-analysis remote add origin "$MARKET_ANALYSIS_REPO" \
    && git -C /opt/market-analysis fetch --depth 1 origin "$MARKET_ANALYSIS_REF" \
    && git -C /opt/market-analysis checkout FETCH_HEAD \
    && rm -rf /opt/market-analysis/.git /opt/market-analysis/framework \
    && apt-get purge --yes --auto-remove git \
    && rm -rf /var/lib/apt/lists/*

# Locked, hashed dependencies; no setup script runs at build time. pip is removed afterwards: the
# service never installs anything, and the libraries that pip vendors carry findings of their own.
COPY requirements.lock /tmp/requirements.lock
RUN pip install --require-hashes --only-binary :all: -r /tmp/requirements.lock \
    && rm /tmp/requirements.lock \
    && pip uninstall --yes pip
ENV PYTHONPATH=/opt/market-analysis/src \
    OUTPUT_DIRECTORY=/data

RUN useradd --create-home --uid 10001 app \
    && mkdir -p /data \
    && chown app /data
USER app
WORKDIR /opt/market-analysis
EXPOSE 8000

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=6 \
    CMD ["python", "-c", "import urllib.request as u; u.urlopen('http://127.0.0.1:8000/health', timeout=2)"]

CMD ["python", "-m", "uvicorn", "--factory", "app.infrastructure.main:create_app", "--host", "0.0.0.0", "--port", "8000"]
