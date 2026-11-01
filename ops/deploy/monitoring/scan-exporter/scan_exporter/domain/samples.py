"""Turns the answers of GitHub and SonarQube Cloud into samples of metrics.

Everything here is pure: it takes the decoded JSON of an API and returns samples, so it is
tested without a network.
"""

from collections import Counter
from collections.abc import Iterable, Mapping
from dataclasses import dataclass
from datetime import datetime
from typing import Any

# The JSON of the APIs is untyped by nature; each reader below checks the fields it uses.
Json = Any

# The SonarQube Cloud measures that are exported as numbers (alert_status is the quality gate).
SONAR_NUMERIC_METRICS = (
    "bugs",
    "vulnerabilities",
    "security_hotspots",
    "code_smells",
    "coverage",
    "duplicated_lines_density",
)
SONAR_QUALITY_GATE_METRIC = "alert_status"


@dataclass(frozen=True)
class Sample:
    """One value of one metric with its labels."""

    name: str
    labels: tuple[tuple[str, str], ...]
    value: float


class InvalidAnswerError(ValueError):
    """An API answered with JSON that does not have the fields this exporter reads."""


def parse_timestamp(text: str) -> float:
    """Return the Unix time of an ISO 8601 time such as ``2026-11-01T06:31:37Z``."""
    try:
        return datetime.fromisoformat(text.replace("Z", "+00:00")).timestamp()
    except (AttributeError, ValueError) as err:
        raise InvalidAnswerError(f"not a time: {text!r}") from err


def alert_severity(alert: Mapping[str, Json]) -> str:
    """The security severity of a code scanning alert, or its rule severity when it has none.

    CodeQL and Trivy give a security severity (critical, high, medium, low); ZAP and Scorecard
    often give only the rule severity (error, warning, note).
    """
    rule = alert.get("rule") or {}
    level = rule.get("security_severity_level") or rule.get("severity") or "none"
    return str(level)


def open_alert_samples(alerts: Iterable[Mapping[str, Json]]) -> list[Sample]:
    """Count the open code scanning alerts by tool, category and severity."""
    counts: Counter[tuple[str, str, str]] = Counter()
    for alert in alerts:
        try:
            tool = str(alert["tool"]["name"])
            category = str((alert.get("most_recent_instance") or {}).get("category") or "")
        except (KeyError, TypeError) as err:
            raise InvalidAnswerError("a code scanning alert has no tool name") from err
        counts[(tool, category, alert_severity(alert))] += 1
    return [
        Sample(
            "nfhotel_code_scanning_open_alerts",
            (("tool", tool), ("category", category), ("severity", severity)),
            float(count),
        )
        for (tool, category, severity), count in sorted(counts.items())
    ]


def last_analysis_samples(analyses: Iterable[Mapping[str, Json]]) -> list[Sample]:
    """The time of the newest analysis of each tool and category."""
    newest: dict[tuple[str, str], float] = {}
    for analysis in analyses:
        try:
            key = (str(analysis["tool"]["name"]), str(analysis.get("category") or ""))
            created = parse_timestamp(analysis["created_at"])
        except (KeyError, TypeError) as err:
            raise InvalidAnswerError("a code scanning analysis has no tool or time") from err
        newest[key] = max(created, newest.get(key, created))
    return [
        Sample(
            "nfhotel_code_scanning_last_analysis_timestamp_seconds",
            (("tool", tool), ("category", category)),
            created,
        )
        for (tool, category), created in sorted(newest.items())
    ]


def workflow_run_samples(workflow: str, answer: Mapping[str, Json]) -> list[Sample]:
    """The conclusion and end time of the latest completed run of a workflow, if it has one."""
    runs = answer.get("workflow_runs")
    if not isinstance(runs, list):
        raise InvalidAnswerError(f"the runs of {workflow} have no workflow_runs list")
    if not runs:
        return []
    run = runs[0]
    try:
        conclusion = str(run.get("conclusion") or "none")
        ended = parse_timestamp(run["updated_at"])
    except (KeyError, AttributeError) as err:
        raise InvalidAnswerError(f"the latest run of {workflow} has no end time") from err
    return [
        Sample(
            "nfhotel_scan_workflow_last_run_info",
            (("workflow", workflow), ("conclusion", conclusion)),
            1.0,
        ),
        Sample(
            "nfhotel_scan_workflow_last_run_timestamp_seconds",
            (("workflow", workflow),),
            ended,
        ),
    ]


def sonar_samples(answer: Mapping[str, Json]) -> list[Sample]:
    """The numeric measures and the quality gate of a SonarQube Cloud project."""
    try:
        measures = {
            str(measure["metric"]): str(measure["value"])
            for measure in answer["component"]["measures"]
        }
    except (KeyError, TypeError) as err:
        raise InvalidAnswerError("the SonarQube Cloud answer has no measures") from err
    samples = []
    for metric in SONAR_NUMERIC_METRICS:
        if metric not in measures:
            continue
        try:
            value = float(measures[metric])
        except ValueError as err:
            raise InvalidAnswerError(f"the measure {metric} is not a number") from err
        samples.append(Sample("nfhotel_sonar_measure", (("metric", metric),), value))
    if SONAR_QUALITY_GATE_METRIC in measures:
        is_passed = measures[SONAR_QUALITY_GATE_METRIC] == "OK"
        samples.append(Sample("nfhotel_sonar_quality_gate_passed", (), 1.0 if is_passed else 0.0))
    return samples


def source_samples(source: str, is_up: bool, last_success: float | None) -> list[Sample]:
    """Whether a source was read, and when it was last read successfully."""
    samples = [Sample("nfhotel_scan_source_up", (("source", source),), 1.0 if is_up else 0.0)]
    if last_success is not None:
        samples.append(
            Sample(
                "nfhotel_scan_source_last_success_timestamp_seconds",
                (("source", source),),
                last_success,
            )
        )
    return samples
