"""Reads every source once and keeps the latest samples of each (MIL-053).

A source that fails keeps the samples of its last successful read and reports
``nfhotel_scan_source_up 0``, so one unreachable API does not blank the dashboard.
"""

import logging
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from typing import Protocol

from scan_exporter.domain import samples as scan_samples
from scan_exporter.domain.samples import Json, Sample

LOG = logging.getLogger(__name__)

SOURCE_CODE_SCANNING = "github_code_scanning"
SOURCE_WORKFLOWS = "github_actions"
SOURCE_SONAR = "sonarcloud"

# A page of the GitHub API holds at most 100 items; the guard stops a loop that never ends.
PAGE_SIZE = 100
MAX_PAGES = 20
# The newest analyses come first, so a few pages cover every tool and category.
ANALYSIS_PAGES = 3


class SourceError(Exception):
    """A source could not be read: the network, the status code or the answer."""


class JsonClient(Protocol):
    """Fetches a URL and returns its decoded JSON, or raises SourceError."""

    def get_json(self, url: str, headers: Mapping[str, str]) -> Json: ...


@dataclass(frozen=True)
class Settings:
    """What to read: the repository, its scan workflows and the SonarQube Cloud project."""

    github_api: str
    repository: str
    workflows: Sequence[str]
    sonar_api: str
    sonar_project: str
    github_token: str | None


class Collector:
    """Reads the sources and holds the samples of the latest successful read of each."""

    def __init__(self, settings: Settings, client: JsonClient, clock: Callable[[], float]) -> None:
        self._settings = settings
        self._client = client
        self._clock = clock
        self._samples: dict[str, list[Sample]] = {}
        self._is_up: dict[str, bool] = {}
        self._last_success: dict[str, float] = {}

    def collect(self) -> None:
        """Read every source once."""
        self._read(SOURCE_CODE_SCANNING, self._read_code_scanning)
        self._read(SOURCE_WORKFLOWS, self._read_workflows)
        self._read(SOURCE_SONAR, self._read_sonar)

    def samples(self) -> list[Sample]:
        """The samples to export now."""
        result: list[Sample] = []
        for source in (SOURCE_CODE_SCANNING, SOURCE_WORKFLOWS, SOURCE_SONAR):
            result.extend(self._samples.get(source, []))
            if source in self._is_up:
                result.extend(
                    scan_samples.source_samples(
                        source, self._is_up[source], self._last_success.get(source)
                    )
                )
        return result

    def _read(self, source: str, reader: Callable[[], list[Sample]]) -> None:
        try:
            samples = reader()
        except (SourceError, scan_samples.InvalidAnswerError) as err:
            LOG.warning("cannot read %s: %s", source, err)
            self._is_up[source] = False
            return
        self._samples[source] = samples
        self._is_up[source] = True
        self._last_success[source] = self._clock()

    def _github_headers(self) -> dict[str, str]:
        headers = {
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
        }
        if self._settings.github_token:
            headers["Authorization"] = f"Bearer {self._settings.github_token}"
        return headers

    def _repo_url(self, path: str) -> str:
        return f"{self._settings.github_api}/repos/{self._settings.repository}/{path}"

    def _pages(self, path: str, max_pages: int) -> list[Json]:
        items: list[Json] = []
        for page in range(1, max_pages + 1):
            separator = "&" if "?" in path else "?"
            url = self._repo_url(f"{path}{separator}per_page={PAGE_SIZE}&page={page}")
            answer = self._client.get_json(url, self._github_headers())
            if not isinstance(answer, list):
                raise SourceError(f"{path} did not answer with a list")
            items.extend(answer)
            if len(answer) < PAGE_SIZE:
                break
        return items

    def _read_code_scanning(self) -> list[Sample]:
        # Code scanning needs a token even on a public repository.
        if not self._settings.github_token:
            raise SourceError("no GitHub token, so code scanning is not read")
        alerts = self._pages("code-scanning/alerts?state=open", MAX_PAGES)
        analyses = self._pages("code-scanning/analyses", ANALYSIS_PAGES)
        return scan_samples.open_alert_samples(alerts) + scan_samples.last_analysis_samples(
            analyses
        )

    def _read_workflows(self) -> list[Sample]:
        samples: list[Sample] = []
        for workflow in self._settings.workflows:
            url = self._repo_url(
                f"actions/workflows/{workflow}/runs?branch=main&status=completed&per_page=1"
            )
            answer = self._client.get_json(url, self._github_headers())
            if not isinstance(answer, dict):
                raise SourceError(f"the runs of {workflow} are not a JSON object")
            samples.extend(scan_samples.workflow_run_samples(workflow, answer))
        return samples

    def _read_sonar(self) -> list[Sample]:
        metric_keys = ",".join(
            (*scan_samples.SONAR_NUMERIC_METRICS, scan_samples.SONAR_QUALITY_GATE_METRIC)
        )
        url = (
            f"{self._settings.sonar_api}/measures/component"
            f"?component={self._settings.sonar_project}&metricKeys={metric_keys}"
        )
        answer = self._client.get_json(url, {"Accept": "application/json"})
        if not isinstance(answer, dict):
            raise SourceError("SonarQube Cloud did not answer with a JSON object")
        return scan_samples.sonar_samples(answer)
