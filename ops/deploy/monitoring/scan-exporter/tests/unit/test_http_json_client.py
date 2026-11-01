"""Tests of the HTTP client with urlopen replaced, so no request leaves the test (MIL-053)."""

import io
import urllib.error
import urllib.request
from typing import Any

import pytest

from scan_exporter.application.collector import SourceError
from scan_exporter.infrastructure.http_json_client import UrllibJsonClient

URL = "https://api.github.test/repos/NF-Hotel/NFHotel/code-scanning/alerts?page=1"
TOKEN_HEADERS = {"Authorization": "Bearer stand-in-token"}


def _fake_urlopen(answer: bytes | Exception) -> Any:
    def urlopen(request: urllib.request.Request, timeout: float) -> io.BytesIO:
        if isinstance(answer, Exception):
            raise answer
        return io.BytesIO(answer)

    return urlopen


def test_get_json_returns_the_decoded_answer(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(urllib.request, "urlopen", _fake_urlopen(b'[{"a": 1}]'))

    assert UrllibJsonClient().get_json(URL, TOKEN_HEADERS) == [{"a": 1}]


def test_get_json_when_not_https_refuses_without_a_request(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(urllib.request, "urlopen", _fake_urlopen(AssertionError("sent")))

    with pytest.raises(SourceError, match="not an https URL"):
        UrllibJsonClient().get_json("http://api.github.test/x", TOKEN_HEADERS)


def test_get_json_when_status_is_an_error_names_path_and_status_only(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    error = urllib.error.HTTPError(URL, 401, "Unauthorized", {}, None)  # type: ignore[arg-type]
    monkeypatch.setattr(urllib.request, "urlopen", _fake_urlopen(error))

    with pytest.raises(SourceError) as raised:
        UrllibJsonClient().get_json(URL, TOKEN_HEADERS)

    assert str(raised.value) == "/repos/NF-Hotel/NFHotel/code-scanning/alerts answered 401"
    assert "stand-in-token" not in str(raised.value)


def test_get_json_when_unreachable_raises(monkeypatch: pytest.MonkeyPatch) -> None:
    error = urllib.error.URLError("no route")
    monkeypatch.setattr(urllib.request, "urlopen", _fake_urlopen(error))

    with pytest.raises(SourceError, match="could not be reached: URLError"):
        UrllibJsonClient().get_json(URL, TOKEN_HEADERS)


def test_get_json_when_answer_is_not_json_raises(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr(urllib.request, "urlopen", _fake_urlopen(b"<html>"))

    with pytest.raises(SourceError, match="did not answer with JSON"):
        UrllibJsonClient().get_json(URL, TOKEN_HEADERS)
