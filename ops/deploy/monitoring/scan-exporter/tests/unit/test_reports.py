"""Tests of the check of the reports that other jobs of the host write (MIL-051)."""

import pytest

from scan_exporter.domain.reports import InvalidReportError, checked_report, served_reports

PULL_REPORT = """\
# HELP nfhotel_task_last_success_timestamp_seconds Time the job last ended well.
# TYPE nfhotel_task_last_success_timestamp_seconds gauge
nfhotel_task_last_success_timestamp_seconds{host="nf-monitoring",task="monitoring_pull"} 1763130000
# HELP nfhotel_monitoring_deploy_failed 1 when the last deploy failed, else 0.
# TYPE nfhotel_monitoring_deploy_failed gauge
nfhotel_monitoring_deploy_failed{host="nf-monitoring"} 0
"""
TAKEN = frozenset({"nfhotel_scan_source_up"})


def test_checked_report_of_a_valid_report_returns_its_lines_and_names() -> None:
    lines, names = checked_report(PULL_REPORT, TAKEN)

    assert lines == PULL_REPORT.splitlines()
    assert names == {
        "nfhotel_task_last_success_timestamp_seconds",
        "nfhotel_monitoring_deploy_failed",
    }


@pytest.mark.parametrize(
    "line",
    [
        "a 1",
        "a{} 0",
        'a{b="c",d="e"} -2.5',
        'a{b="quote \\" backslash \\\\ newline \\n"} 1e3',
        "a +Inf",
        "a NaN",
        "a .5 1763130000000",
        "a:b_c 1",
    ],
)
def test_checked_report_accepts_the_forms_of_a_sample(line: str) -> None:
    assert checked_report(line + "\n", TAKEN) == (
        [line],
        frozenset({line.split("{")[0].split()[0]}),
    )


def test_checked_report_leaves_out_comments_and_empty_lines() -> None:
    lines, _ = checked_report("# written by the pull\n\na 1\n", TAKEN)

    assert lines == ["a 1"]


@pytest.mark.parametrize(
    ("text", "reason"),
    [
        ("a\n", "not in the text format"),
        ("a 1 2 3\n", "not in the text format"),
        ("a one\n", "not in the text format"),
        ("a{b=c} 1\n", "not in the text format"),
        ('a{b="c"\n', "not in the text format"),
        ("1a 1\n", "not in the text format"),
        ("# TYPE a histogram\na_bucket 1\n", "not in the text format"),
        ("# HELP a\n", "not in the text format"),
        ("nfhotel_scan_source_up 1\n", "which is taken"),
        ("a 1\nb 1\na 2\n", "do not follow each other"),
        ("# TYPE a gauge\n# TYPE a gauge\na 1\n", "a second TYPE"),
        ("a 1\n# HELP a late\n", "one after the samples"),
    ],
)
def test_checked_report_refuses_a_report_that_could_break_the_scrape(
    text: str, reason: str
) -> None:
    with pytest.raises(InvalidReportError, match=reason):
        checked_report(text, TAKEN)


def test_served_reports_leaves_out_an_invalid_report_and_keeps_the_others() -> None:
    text, problems = served_reports(
        [("a.prom", "a 1\n"), ("b.prom", "b{\n"), ("c.prom", "c 3\n")], TAKEN
    )

    assert text == "a 1\nc 3\n"
    assert problems == ["b.prom: line 1 is not in the text format"]


def test_served_reports_refuses_a_name_that_an_earlier_report_uses() -> None:
    text, problems = served_reports([("a.prom", "a 1\n"), ("b.prom", "a 2\n")], TAKEN)

    assert text == "a 1\n"
    assert problems == ["b.prom: line 1 uses a, which is taken"]


def test_served_reports_without_reports_is_empty() -> None:
    assert served_reports([], TAKEN) == ("", [])
