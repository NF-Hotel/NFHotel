# Market analysis service

Runs `NF-Hotel/notebook-market-analysis` as one container behind the NFHotel web application. It serves the holiday
calendar (`/api/v1/holidays`) and the market analysis runs (`/api/v1/market-analysis/runs`); every path except `/health`
needs the header `X-API-Key`. It publishes no host port: the web application and the API reach it as
`http://market-analysis-api:8000` on the internal network `nfhotel-api`, and nginx publishes it on the internal listener
(`ANALYSIS_API_SERVER_NAME`).

## The image

`Dockerfile` fetches the source of the repository at the commit `MARKET_ANALYSIS_REF` and installs the dependencies from
`requirements.lock`, with hashes (the same method as `deploy/benchmark`). Nothing is built from a version range and no
private package is needed. The base image is pinned by digest and Dependabot proposes the next one.

To upgrade, set `MARKET_ANALYSIS_REF` in `market-analysis.env.example` and in the default of `docker-compose.yml`,
then regenerate the lock file from that commit's `pyproject.toml`:

```bash
uv pip compile pyproject.toml --generate-hashes --python-platform linux --python-version 3.13 --no-header -o requirements.lock
```

## Start

```bash
cp market-analysis.env.example market-analysis.env     # then set API_TOKEN (openssl rand -hex 32)
docker compose --env-file market-analysis.env up -d --build
```

Put the same value as `HolidayApi__ApiKey` in `deploy/nfhotel-web/nfhotel-web.env`. `scripts/dev-up.sh` writes both.
`scripts/deploy-services.sh` writes `OLLAMA_MODEL` from the default AI model chosen in the back office (ADR-0014).

## Not here any more

The frozen service (ADR-0030, ADR-0500) had `POST /analyze`, a history notebook and a `market-analysis.toml`. The new
service has none of them. The NFHotel API starts a run with `POST /api/v1/market-analysis/runs`, follows its state and
reads the result (the client in `src/NFHotel.External/MarketAnalysis`); it sends the key as `MarketAnalysisService__ApiKey`,
which must equal `API_TOKEN` of this service. `MARKET_ANALYSIS_REF` must be a commit that has the runs API (the merge of
pull request 14 or later). Until the AI adapters of the service are built, every insight is `AI_NOT_CONFIGURED`.
