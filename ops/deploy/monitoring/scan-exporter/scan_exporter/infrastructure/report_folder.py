"""Reads the reports of the folder that the compose file mounts read-only (MIL-051)."""

import logging
from collections.abc import Set
from pathlib import Path

from scan_exporter.domain.reports import served_reports

LOG = logging.getLogger(__name__)

# A report holds a few lines; the limits keep a stray file from filling every scrape.
MAX_REPORT_BYTES = 64 * 1024
MAX_REPORTS = 32


class ReportFolder:
    """The reports (*.prom) of a folder, checked, for the end of each scrape.

    Prometheus scrapes every minute, so a report that is left out is logged once when the set
    of problems changes, not at every scrape.
    """

    def __init__(self, folder: Path, taken: Set[str]) -> None:
        self._folder = folder
        self._taken = taken
        self._logged: frozenset[str] = frozenset()

    def text(self) -> str:
        """The lines of every valid report, in the order of the file names."""
        reports, problems = self._read()
        text, invalid = served_reports(reports, self._taken)
        self._log(problems + invalid)
        return text

    def _read(self) -> tuple[list[tuple[str, str]], list[str]]:
        reports: list[tuple[str, str]] = []
        problems: list[str] = []
        paths = sorted(self._folder.glob("*.prom"))
        if len(paths) > MAX_REPORTS:
            problems.append(f"{len(paths)} reports, only the first {MAX_REPORTS} are read")
        for path in paths[:MAX_REPORTS]:
            try:
                if path.stat().st_size > MAX_REPORT_BYTES:
                    problems.append(f"{path.name}: larger than {MAX_REPORT_BYTES} bytes")
                    continue
                reports.append((path.name, path.read_text(encoding="utf-8")))
            except (OSError, UnicodeDecodeError) as err:
                problems.append(f"{path.name}: cannot be read ({type(err).__name__})")
        return reports, problems

    def _log(self, problems: list[str]) -> None:
        current = frozenset(problems)
        for problem in sorted(current - self._logged):
            LOG.warning("report left out: %s", problem)
        self._logged = current
