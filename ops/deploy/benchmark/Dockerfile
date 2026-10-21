# Image of the benchmark service of NF-Hotel/notebook-benchmark (ADR-0004, MIL-019, MIL-020 of
# NFHotel): the HTTP API that starts runs against Ollama and delivers the result to the NFHotel
# API. That repository has a Dockerfile of its own, but it installs with pip from version
# ranges; this one fetches the source at a pinned commit and installs locked, hashed
# dependencies instead. Change BENCHMARK_REF to upgrade. Build context is this folder.
# The package is not installed: its source stays at /opt/benchmark/src on PYTHONPATH, so no
# build backend runs. The notebook and its libraries (extra "notebook") are not installed.
# requirements.lock pins every dependency with hashes for the pinned commit. After
# changing BENCHMARK_REF, regenerate it from that commit's pyproject.toml:
#   uv pip compile pyproject.toml --generate-hashes --python-platform linux \
#     --python-version 3.13 --no-header -o requirements.lock
# Pinned by digest (MIL-022 task 4); Dependabot proposes the next one.
FROM python:3.13-slim@sha256:70729b46c69b4f1e97c4822c1af3df53a1476cf5ddc6c087c0c10bc3a5678c2f

ARG BENCHMARK_REPO
ARG BENCHMARK_REF
ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

RUN apt-get update \
    && apt-get install --yes --no-install-recommends git ca-certificates \
    && git init /opt/benchmark \
    && git -C /opt/benchmark remote add origin "$BENCHMARK_REPO" \
    && git -C /opt/benchmark fetch --depth 1 origin "$BENCHMARK_REF" \
    && git -C /opt/benchmark checkout FETCH_HEAD \
    && rm -rf /opt/benchmark/.git /opt/benchmark/framework \
    && apt-get purge --yes --auto-remove git \
    && rm -rf /var/lib/apt/lists/*

# Locked, hashed dependencies; no setup script runs at build time. pip is removed
# afterwards: the service never installs anything, and the copies of urllib3, msgpack
# and setuptools that pip vendors carry fixable high findings (MIL-022 task 1).
COPY requirements.lock /tmp/requirements.lock
RUN pip install --require-hashes --only-binary :all: -r /tmp/requirements.lock \
    && rm /tmp/requirements.lock \
    && pip uninstall --yes pip
ENV PYTHONPATH=/opt/benchmark/src \
    NFBENCH_DATA_DIR=/data \
    NFBENCH_DATASET=/opt/benchmark/data/example/synthetic_hotel_bookings.csv

RUN useradd --create-home --uid 10001 app \
    && mkdir -p /data \
    && chown app /data
USER app
WORKDIR /opt/benchmark
EXPOSE 8000
CMD ["python", "-m", "uvicorn", "--factory", "nfbench.bootstrap:create_service_app", "--host", "0.0.0.0", "--port", "8000"]
