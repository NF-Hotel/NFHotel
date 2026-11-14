"""Checks the reports that other jobs of the host write in the Prometheus text format (MIL-051).

The pull deploy of the monitoring (ADR-0044) writes its state as a file into a folder that the
exporter serves after its own metrics. One broken file must not break the scrape of the others,
so a report is served only when every line is valid and it uses no metric name that the
exporter or an earlier report uses. Only gauges, counters and untyped metrics are read: their
samples carry the name of their family, so the order of the lines can be checked.
"""

import re
from collections.abc import Iterable, Set

_NAME = r"[a-zA-Z_:][a-zA-Z0-9_:]*"
_LABEL = r'[a-zA-Z_][a-zA-Z0-9_]*="(?:[^"\\\n]|\\[\\"n])*"'
_VALUE = r"[-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][-+]?[0-9]+)?|[-+]?Inf|NaN"
_SAMPLE = re.compile(rf"({_NAME})(?:\{{(?:{_LABEL}(?:,{_LABEL})*)?\}})? (?:{_VALUE})(?: -?[0-9]+)?")
_HELP = re.compile(rf"# HELP ({_NAME}) [^\n]*")
_TYPE = re.compile(rf"# TYPE ({_NAME}) (?:gauge|counter|untyped)")
_SAMPLE_KIND = "sample"


class InvalidReportError(ValueError):
    """A line of a report is not valid, or it uses a metric name that is taken."""


def _kind_and_name(line: str) -> tuple[str, str] | None:
    """The kind of the line and its metric name; ("", "") for a comment, None when invalid."""
    for kind, pattern in (("HELP", _HELP), ("TYPE", _TYPE), (_SAMPLE_KIND, _SAMPLE)):
        match = pattern.fullmatch(line)
        if match:
            return kind, match.group(1)
    if line.startswith(("# HELP ", "# TYPE ")):
        return None
    if line.startswith("#") or not line.strip():
        return "", ""
    return None


def checked_report(text: str, taken: Set[str]) -> tuple[list[str], frozenset[str]]:
    """The lines to serve of the report and its metric names, or InvalidReportError.

    The lines of one name follow each other, its HELP and TYPE come once each and before its
    samples, and no name is in taken. Comments and empty lines are left out.
    """
    lines: list[str] = []
    kinds: dict[str, set[str]] = {}
    last = ""
    for number, line in enumerate(text.splitlines(), start=1):
        found = _kind_and_name(line)
        if found is None:
            raise InvalidReportError(f"line {number} is not in the text format")
        kind, name = found
        if not kind:
            continue
        if name in taken:
            raise InvalidReportError(f"line {number} uses {name}, which is taken")
        seen = kinds.setdefault(name, set())
        if seen and name != last:
            raise InvalidReportError(f"line {number}: the lines of {name} do not follow each other")
        if kind != _SAMPLE_KIND and (kind in seen or _SAMPLE_KIND in seen):
            raise InvalidReportError(f"line {number}: a second {kind} or one after the samples")
        seen.add(kind)
        last = name
        lines.append(line)
    return lines, frozenset(kinds)


def served_reports(reports: Iterable[tuple[str, str]], taken: Set[str]) -> tuple[str, list[str]]:
    """The text of the valid reports in their order, and why each other report is left out."""
    used = set(taken)
    lines: list[str] = []
    problems: list[str] = []
    for file_name, text in reports:
        try:
            report_lines, names = checked_report(text, used)
        except InvalidReportError as err:
            problems.append(f"{file_name}: {err}")
            continue
        used |= names
        lines.extend(report_lines)
    return ("\n".join(lines) + "\n" if lines else ""), problems
