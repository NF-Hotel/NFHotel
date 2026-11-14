"""Tests of the folder of reports on a real file system, in a folder of the test (MIL-051)."""

import logging
from pathlib import Path

import pytest

from scan_exporter.infrastructure import report_folder
from scan_exporter.infrastructure.report_folder import ReportFolder

TAKEN = frozenset({"nfhotel_scan_source_up"})


def test_text_serves_the_reports_in_the_order_of_their_names(tmp_path: Path) -> None:
    (tmp_path / "b.prom").write_text("b 2\n", encoding="utf-8")
    (tmp_path / "a.prom").write_text("a 1\n", encoding="utf-8")
    (tmp_path / "c.prom.new").write_text("c 3\n", encoding="utf-8")
    (tmp_path / "notes.txt").write_text("d 4\n", encoding="utf-8")

    assert ReportFolder(tmp_path, TAKEN).text() == "a 1\nb 2\n"


def test_text_of_a_missing_folder_is_empty(tmp_path: Path) -> None:
    assert ReportFolder(tmp_path / "missing", TAKEN).text() == ""


def test_text_reads_a_report_again_at_each_call(tmp_path: Path) -> None:
    folder = ReportFolder(tmp_path, TAKEN)
    (tmp_path / "a.prom").write_text("a 1\n", encoding="utf-8")
    first = folder.text()
    (tmp_path / "a.prom").write_text("a 2\n", encoding="utf-8")

    assert (first, folder.text()) == ("a 1\n", "a 2\n")


def test_text_leaves_out_a_report_that_is_too_large_or_not_text(tmp_path: Path) -> None:
    (tmp_path / "big.prom").write_text("a 1\n" * report_folder.MAX_REPORT_BYTES, encoding="utf-8")
    (tmp_path / "binary.prom").write_bytes(b"\xff\xfe 1\n")
    (tmp_path / "ok.prom").write_text("ok 1\n", encoding="utf-8")

    assert ReportFolder(tmp_path, TAKEN).text() == "ok 1\n"


def test_text_reads_no_more_than_the_limit_of_reports(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch, caplog: pytest.LogCaptureFixture
) -> None:
    monkeypatch.setattr(report_folder, "MAX_REPORTS", 2)
    for name in ("a", "b", "c"):
        (tmp_path / f"{name}.prom").write_text(f"{name} 1\n", encoding="utf-8")

    with caplog.at_level(logging.WARNING):
        text = ReportFolder(tmp_path, TAKEN).text()

    assert text == "a 1\nb 1\n"
    assert "3 reports, only the first 2 are read" in caplog.text


def test_text_logs_a_report_left_out_once_and_again_after_it_was_fixed(
    tmp_path: Path, caplog: pytest.LogCaptureFixture
) -> None:
    folder = ReportFolder(tmp_path, TAKEN)
    report = tmp_path / "pull.prom"
    report.write_text("nfhotel_scan_source_up 1\n", encoding="utf-8")

    with caplog.at_level(logging.WARNING):
        folder.text()
        folder.text()
        report.write_text("pull 1\n", encoding="utf-8")
        folder.text()
        report.write_text("nfhotel_scan_source_up 1\n", encoding="utf-8")
        folder.text()

    warnings = [record.getMessage() for record in caplog.records]
    assert warnings == [
        "report left out: pull.prom: line 1 uses nfhotel_scan_source_up, which is taken",
        "report left out: pull.prom: line 1 uses nfhotel_scan_source_up, which is taken",
    ]
