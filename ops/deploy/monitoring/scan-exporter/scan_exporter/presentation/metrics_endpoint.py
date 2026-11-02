"""The /metrics endpoint that Prometheus scrapes (MIL-053)."""

from http.server import BaseHTTPRequestHandler

from scan_exporter.application.collector import Collector
from scan_exporter.presentation.prometheus_text import render

CONTENT_TYPE = "text/plain; version=0.0.4; charset=utf-8"


def make_handler(collector: Collector) -> type[BaseHTTPRequestHandler]:
    """A request handler that serves the samples of the collector on /metrics."""

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:
            if self.path != "/metrics":
                self.send_error(404)
                return
            body = render(collector.samples()).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", CONTENT_TYPE)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, format: str, *args: object) -> None:  # noqa: A002 - fixed name
            # Prometheus scrapes every 30 seconds; a line per scrape would only fill the log.
            return

    return Handler
