# Image for NF-Hotel/Notebook_Market_Analysis (MIL-004 task 4). The repository has
# no Dockerfile, so this one fetches it at a pinned commit; change ANALYSIS_REF to
# upgrade. Build context is this folder.
FROM python:3.13-slim

ARG ANALYSIS_REPO
ARG ANALYSIS_REF
ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

RUN apt-get update \
    && apt-get install --yes --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN git init /opt/analysis \
    && git -C /opt/analysis remote add origin "$ANALYSIS_REPO" \
    && git -C /opt/analysis fetch --depth 1 origin "$ANALYSIS_REF" \
    && git -C /opt/analysis checkout FETCH_HEAD \
    && rm -rf /opt/analysis/.git \
    && pip wheel --only-binary :all: --no-deps --wheel-dir /tmp/wheels /opt/analysis \
    && pip install --only-binary :all: /tmp/wheels/*.whl \
    && rm -rf /tmp/wheels /opt/analysis

RUN useradd --create-home app \
    && mkdir -p /app/work/output \
    && chown -R app /app/work
USER app
WORKDIR /app/work
