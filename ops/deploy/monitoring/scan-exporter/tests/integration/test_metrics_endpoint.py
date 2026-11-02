"""The /metrics endpoint on a real HTTP server on the loopback address (MIL-053).

The sources are a fake, so nothing leaves the machine; the server and the client are real.
"""

import threading
import urllib.error
import urllib.request
from collections.abc import Iterator
from http.server import ThreadingHTTPServer

import pytest

from scan_exporter.domain.samples import Sample
from scan_exporter.main import refresh_forever
from scan_exporter.presentation.metrics_endpoint import CONTENT_TYPE, make_handler


class FakeCollector:
    """Serves fixed samples and counts how often it was asked to read."""

    def __init__(self) -> None:
        self.reads = 0

    def collect(self) -> None:
        self.reads += 1

    def samples(self) -> list[Sample]:
        return [Sample("nfhotel_sonar_quality_gate_passed", (), 1.0)]


@pytest.fixture
def base_url() -> Iterator[str]:
    server = ThreadingHTTPServer(("127.0.0.1", 0), make_handler(FakeCollector()))  # type: ignore[arg-type]
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    yield f"http://127.0.0.1:{server.server_address[1]}"
    server.shutdown()
    server.server_close()


def test_metrics_answers_the_text_format(base_url: str) -> None:
    with urllib.request.urlopen(f"{base_url}/metrics", timeout=5) as answer:  # noqa: S310 - loopback
        body = answer.read().decode("utf-8")
        content_type = answer.headers["Content-Type"]

    assert content_type == CONTENT_TYPE
    assert "nfhotel_sonar_quality_gate_passed 1\n" in body


def test_other_paths_answer_404(base_url: str) -> None:
    with pytest.raises(urllib.error.HTTPError) as raised:
        urllib.request.urlopen(f"{base_url}/", timeout=5)  # noqa: S310 - loopback

    assert raised.value.code == 404


def test_refresh_forever_reads_until_stopped() -> None:
    collector = FakeCollector()
    stop = threading.Event()

    class StopAfterTwo(FakeCollector):
        def collect(self) -> None:
            collector.collect()
            if collector.reads == 2:
                stop.set()

    refresh_forever(StopAfterTwo(), 0, stop)  # type: ignore[arg-type]

    assert collector.reads == 2
