"""The Prometheus text format of the samples (MIL-053)."""

from collections.abc import Iterable
from dataclasses import dataclass

from scan_exporter.domain.samples import Sample


@dataclass(frozen=True)
class MetricFamily:
    """The help text and type of a metric name, printed once before its samples."""

    name: str
    help_text: str
    kind: str


FAMILIES = (
    MetricFamily(
        "nfhotel_code_scanning_open_alerts",
        "Open alerts of GitHub code scanning by tool, category and severity.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_code_scanning_last_analysis_timestamp_seconds",
        "Time of the latest code scanning analysis of a tool and category.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_scan_workflow_last_run_info",
        "The conclusion of the latest completed run on main of a scan workflow (always 1).",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_scan_workflow_last_run_timestamp_seconds",
        "Time the latest completed run on main of a scan workflow ended.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_sonar_measure",
        "A measure of the SonarQube Cloud project.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_sonar_quality_gate_passed",
        "1 when the quality gate of the SonarQube Cloud project passes, else 0.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_scan_source_up",
        "1 when the latest read of a source succeeded, else 0.",
        "gauge",
    ),
    MetricFamily(
        "nfhotel_scan_source_last_success_timestamp_seconds",
        "Time of the latest successful read of a source.",
        "gauge",
    ),
)


def _escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace("\n", "\\n").replace('"', '\\"')


def _format_value(value: float) -> str:
    return str(int(value)) if value.is_integer() else repr(value)


def render(samples: Iterable[Sample]) -> str:
    """The Prometheus text format of the samples, grouped under their help and type lines."""
    by_name: dict[str, list[Sample]] = {}
    for sample in samples:
        by_name.setdefault(sample.name, []).append(sample)
    lines = []
    for family in FAMILIES:
        family_samples = by_name.get(family.name, [])
        if not family_samples:
            continue
        lines.append(f"# HELP {family.name} {family.help_text}")
        lines.append(f"# TYPE {family.name} {family.kind}")
        for sample in family_samples:
            labels = ",".join(f'{key}="{_escape(value)}"' for key, value in sample.labels)
            name = f"{sample.name}{{{labels}}}" if labels else sample.name
            lines.append(f"{name} {_format_value(sample.value)}")
    return "\n".join(lines) + "\n" if lines else ""
