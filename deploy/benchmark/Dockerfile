# Image for NF-Hotel/notebook-benchmark (the local LLM hotel benchmark, a marimo
# notebook). The repository has no Dockerfile, so this one fetches it at a pinned
# commit; change BENCHMARK_REF to upgrade. Build context is this folder.
# The notebook calls Ollama at localhost:11434 and cannot be configured, so the
# container forwards that port to the ollama container (see docker-compose.yml).
# The notebook finds data/ and output/ relative to the source tree, so the
# repository stays at /opt/benchmark and is put on PYTHONPATH.
# requirements.lock pins every dependency with hashes for the pinned commit. After
# changing BENCHMARK_REF, regenerate it from that commit's pyproject.toml:
#   uv pip compile pyproject.toml --generate-hashes --python-platform linux \
#     --python-version 3.13 --no-header -o requirements.lock
FROM python:3.13-slim

ARG BENCHMARK_REPO
ARG BENCHMARK_REF
ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

RUN apt-get update \
    && apt-get install --yes --no-install-recommends git ca-certificates socat \
    && rm -rf /var/lib/apt/lists/*

RUN git init /opt/benchmark \
    && git -C /opt/benchmark remote add origin "$BENCHMARK_REPO" \
    && git -C /opt/benchmark fetch --depth 1 origin "$BENCHMARK_REF" \
    && git -C /opt/benchmark checkout FETCH_HEAD \
    && rm -rf /opt/benchmark/.git /opt/benchmark/framework

# Locked, hashed dependencies; no setup script runs at build time.
COPY requirements.lock /tmp/requirements.lock
RUN pip install --require-hashes --only-binary :all: -r /tmp/requirements.lock \
    && rm /tmp/requirements.lock
ENV PYTHONPATH=/opt/benchmark/src

RUN useradd --create-home app \
    && mkdir -p /opt/benchmark/output \
    && chown -R app /opt/benchmark/output
USER app
WORKDIR /opt/benchmark
