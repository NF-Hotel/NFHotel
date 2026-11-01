"""Tests of the pure conversion from API answers to samples and text (MIL-053 task 1)."""

import pytest

from scan_exporter.domain import samples as metrics
from scan_exporter.domain.samples import InvalidAnswerError, Json, Sample
from scan_exporter.presentation.prometheus_text import render


def _alert(
    tool: str, category: str, security: str | None, severity: str = "warning"
) -> dict[str, Json]:
    return {
        "tool": {"name": tool},
        "most_recent_instance": {"category": category},
        "rule": {"security_severity_level": security, "severity": severity},
    }


def test_open_alert_samples_counts_by_tool_category_and_severity() -> None:
    alerts = [
        _alert("Trivy", "trivy-nfhotel-web", "high"),
        _alert("Trivy", "trivy-nfhotel-web", "high"),
        _alert("Trivy", "trivy-nfhotel-api", "critical"),
        _alert("OWASP ZAP", "zap-baseline-public", None, "warning"),
    ]

    samples = metrics.open_alert_samples(alerts)

    assert samples == [
        Sample(
            "nfhotel_code_scanning_open_alerts",
            (("tool", "OWASP ZAP"), ("category", "zap-baseline-public"), ("severity", "warning")),
            1.0,
        ),
        Sample(
            "nfhotel_code_scanning_open_alerts",
            (("tool", "Trivy"), ("category", "trivy-nfhotel-api"), ("severity", "critical")),
            1.0,
        ),
        Sample(
            "nfhotel_code_scanning_open_alerts",
            (("tool", "Trivy"), ("category", "trivy-nfhotel-web"), ("severity", "high")),
            2.0,
        ),
    ]


def test_open_alert_samples_when_empty_gives_no_samples() -> None:
    assert metrics.open_alert_samples([]) == []


def test_alert_severity_when_rule_has_no_severity_is_none() -> None:
    assert metrics.alert_severity({"rule": {}}) == "none"


def test_open_alert_samples_when_tool_is_missing_raises() -> None:
    with pytest.raises(InvalidAnswerError):
        metrics.open_alert_samples([{"rule": {}}])


def test_last_analysis_samples_keeps_the_newest_per_tool_and_category() -> None:
    analyses = [
        {
            "tool": {"name": "CodeQL"},
            "category": "/language:csharp",
            "created_at": "2026-11-01T06:00:00Z",
        },
        {
            "tool": {"name": "CodeQL"},
            "category": "/language:csharp",
            "created_at": "2026-10-31T06:00:00Z",
        },
        {
            "tool": {"name": "OWASP ZAP"},
            "category": "zap-baseline-public",
            "created_at": "2026-10-26T03:30:00Z",
        },
    ]

    samples = metrics.last_analysis_samples(analyses)

    assert [(dict(s.labels)["tool"], s.value) for s in samples] == [
        ("CodeQL", metrics.parse_timestamp("2026-11-01T06:00:00Z")),
        ("OWASP ZAP", metrics.parse_timestamp("2026-10-26T03:30:00Z")),
    ]


def test_parse_timestamp_when_not_a_time_raises() -> None:
    with pytest.raises(InvalidAnswerError):
        metrics.parse_timestamp("yesterday")


def test_workflow_run_samples_gives_conclusion_and_end_time() -> None:
    answer = {"workflow_runs": [{"conclusion": "failure", "updated_at": "2026-11-01T05:17:00Z"}]}

    samples = metrics.workflow_run_samples("e2e-dast.yaml", answer)

    assert samples[0].labels == (("workflow", "e2e-dast.yaml"), ("conclusion", "failure"))
    assert samples[1].value == metrics.parse_timestamp("2026-11-01T05:17:00Z")


def test_workflow_run_samples_when_never_run_gives_no_samples() -> None:
    assert metrics.workflow_run_samples("scorecard.yaml", {"workflow_runs": []}) == []


def test_workflow_run_samples_when_answer_has_no_runs_raises() -> None:
    with pytest.raises(InvalidAnswerError):
        metrics.workflow_run_samples("ci.yaml", {"message": "Not Found"})


def test_sonar_samples_exports_measures_and_quality_gate() -> None:
    answer = {
        "component": {
            "measures": [
                {"metric": "coverage", "value": "76.9"},
                {"metric": "bugs", "value": "1"},
                {"metric": "alert_status", "value": "ERROR"},
            ]
        }
    }

    samples = metrics.sonar_samples(answer)

    assert Sample("nfhotel_sonar_measure", (("metric", "coverage"),), 76.9) in samples
    assert Sample("nfhotel_sonar_measure", (("metric", "bugs"),), 1.0) in samples
    assert Sample("nfhotel_sonar_quality_gate_passed", (), 0.0) in samples


def test_sonar_samples_when_value_is_not_a_number_raises() -> None:
    answer = {"component": {"measures": [{"metric": "bugs", "value": "many"}]}}

    with pytest.raises(InvalidAnswerError):
        metrics.sonar_samples(answer)


def test_render_prints_help_type_and_escaped_labels() -> None:
    samples = [
        Sample("nfhotel_sonar_quality_gate_passed", (), 1.0),
        Sample("nfhotel_sonar_measure", (("metric", 'a"b'),), 76.9),
    ]

    text = render(samples)

    assert text == (
        "# HELP nfhotel_sonar_measure A measure of the SonarQube Cloud project.\n"
        "# TYPE nfhotel_sonar_measure gauge\n"
        'nfhotel_sonar_measure{metric="a\\"b"} 76.9\n'
        "# HELP nfhotel_sonar_quality_gate_passed 1 when the quality gate of the SonarQube "
        "Cloud project passes, else 0.\n"
        "# TYPE nfhotel_sonar_quality_gate_passed gauge\n"
        "nfhotel_sonar_quality_gate_passed 1\n"
    )


def test_render_when_no_samples_is_empty() -> None:
    assert render([]) == ""
