"""Tests of the collector with a fake client in place of GitHub and SonarQube Cloud (MIL-053)."""

from collections.abc import Mapping
from dataclasses import replace
from pathlib import Path

from scan_exporter.application.collector import (
    PAGE_SIZE,
    SOURCE_CODE_SCANNING,
    SOURCE_SONAR,
    SOURCE_WORKFLOWS,
    Collector,
    Settings,
    SourceError,
)
from scan_exporter.domain.samples import Json, Sample
from scan_exporter.infrastructure.settings import read_token

SETTINGS = Settings(
    github_api="https://github.test",
    repository="NF-Hotel/NFHotel",
    workflows=("ci.yaml",),
    sonar_api="https://sonar.test/api",
    sonar_project="NF-Hotel_NFHotel",
    github_token="stand-in-token",
)
NOW = 1_800_000_000.0


class FakeClient:
    """Answers by the start of the URL path; records each URL and its headers."""

    def __init__(self, answers: dict[str, Json]) -> None:
        self.answers = answers
        self.requests: list[tuple[str, dict[str, str]]] = []

    def get_json(self, url: str, headers: Mapping[str, str]) -> Json:
        self.requests.append((url, dict(headers)))
        for prefix, answer in self.answers.items():
            if url.startswith(prefix):
                if isinstance(answer, Exception):
                    raise answer
                return answer(url) if callable(answer) else answer
        raise SourceError(f"no fake answer for {url}")


def _alert(severity: str) -> dict[str, Json]:
    return {
        "tool": {"name": "Trivy"},
        "most_recent_instance": {"category": "trivy-nfhotel-web"},
        "rule": {"security_severity_level": severity},
    }


ALERTS = "https://github.test/repos/NF-Hotel/NFHotel/code-scanning/alerts"
ANALYSES = "https://github.test/repos/NF-Hotel/NFHotel/code-scanning/analyses"
RUNS = "https://github.test/repos/NF-Hotel/NFHotel/actions/workflows/ci.yaml/runs"
SONAR = "https://sonar.test/api/measures/component"


def _answers() -> dict[str, Json]:
    return {
        ALERTS: [_alert("high")],
        ANALYSES: [
            {
                "tool": {"name": "Trivy"},
                "category": "trivy-nfhotel-web",
                "created_at": "2026-11-01T06:00:00Z",
            }
        ],
        RUNS: {"workflow_runs": [{"conclusion": "success", "updated_at": "2026-11-01T06:33:51Z"}]},
        SONAR: {"component": {"measures": [{"metric": "alert_status", "value": "OK"}]}},
    }


def _value(samples: list[Sample], name: str, **labels: str) -> float | None:
    for sample in samples:
        if sample.name == name and all(dict(sample.labels).get(k) == v for k, v in labels.items()):
            return sample.value
    return None


def test_collect_reads_every_source_and_marks_it_up() -> None:
    collector = Collector(SETTINGS, FakeClient(_answers()), lambda: NOW)

    collector.collect()
    samples = collector.samples()

    assert _value(samples, "nfhotel_code_scanning_open_alerts", severity="high") == 1.0
    assert _value(samples, "nfhotel_scan_workflow_last_run_info", conclusion="success") == 1.0
    assert _value(samples, "nfhotel_sonar_quality_gate_passed") == 1.0
    for source in (SOURCE_CODE_SCANNING, SOURCE_WORKFLOWS, SOURCE_SONAR):
        assert _value(samples, "nfhotel_scan_source_up", source=source) == 1.0
        assert (
            _value(samples, "nfhotel_scan_source_last_success_timestamp_seconds", source=source)
            == NOW
        )


def test_collect_sends_the_token_to_github_only() -> None:
    client = FakeClient(_answers())

    Collector(SETTINGS, client, lambda: NOW).collect()

    for url, headers in client.requests:
        has_token = headers.get("Authorization") == "Bearer stand-in-token"
        assert has_token == url.startswith("https://github.test/")


def test_collect_follows_full_pages_of_alerts() -> None:
    def pages(url: str) -> list[dict[str, Json]]:
        return [_alert("critical")] * (PAGE_SIZE if url.endswith("&page=1") else 3)

    answers = _answers()
    answers[ALERTS] = pages
    collector = Collector(SETTINGS, FakeClient(answers), lambda: NOW)

    collector.collect()

    total = _value(collector.samples(), "nfhotel_code_scanning_open_alerts", severity="critical")
    assert total == PAGE_SIZE + 3


def test_collect_without_token_skips_code_scanning_and_reads_the_rest() -> None:
    client = FakeClient(_answers())
    collector = Collector(replace(SETTINGS, github_token=None), client, lambda: NOW)

    collector.collect()
    samples = collector.samples()

    assert not any("code-scanning" in url for url, _ in client.requests)
    assert all("Authorization" not in headers for _, headers in client.requests)
    assert _value(samples, "nfhotel_scan_source_up", source=SOURCE_CODE_SCANNING) == 0.0
    assert _value(samples, "nfhotel_scan_source_up", source=SOURCE_SONAR) == 1.0


def test_collect_when_a_source_fails_keeps_its_last_samples_and_marks_it_down() -> None:
    answers = _answers()
    clock = [NOW]
    collector = Collector(SETTINGS, FakeClient(answers), lambda: clock[0])
    collector.collect()

    clock[0] = NOW + 900
    answers[SONAR] = SourceError("/api/measures/component answered 503")
    collector.collect()
    samples = collector.samples()

    assert _value(samples, "nfhotel_sonar_quality_gate_passed") == 1.0
    assert _value(samples, "nfhotel_scan_source_up", source=SOURCE_SONAR) == 0.0
    assert (
        _value(samples, "nfhotel_scan_source_last_success_timestamp_seconds", source=SOURCE_SONAR)
        == NOW
    )
    assert (
        _value(
            samples, "nfhotel_scan_source_last_success_timestamp_seconds", source=SOURCE_WORKFLOWS
        )
        == NOW + 900
    )


def test_collect_when_an_answer_is_malformed_marks_the_source_down() -> None:
    answers = _answers()
    answers[RUNS] = {"message": "Not Found"}
    collector = Collector(SETTINGS, FakeClient(answers), lambda: NOW)

    collector.collect()

    assert _value(collector.samples(), "nfhotel_scan_source_up", source=SOURCE_WORKFLOWS) == 0.0


def test_samples_before_the_first_read_are_empty() -> None:
    assert Collector(SETTINGS, FakeClient({}), lambda: NOW).samples() == []


def test_read_token_strips_the_line_end(tmp_path: Path) -> None:
    token_file = tmp_path / "github-scan-token"
    token_file.write_text("stand-in-token\n", encoding="utf-8")

    assert read_token(token_file) == "stand-in-token"


def test_read_token_when_missing_or_empty_is_none(tmp_path: Path) -> None:
    empty = tmp_path / "empty"
    empty.write_text("\n", encoding="utf-8")

    assert read_token(tmp_path / "missing") is None
    assert read_token(empty) is None
