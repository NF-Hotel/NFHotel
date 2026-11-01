"""The composition root: wires the collector, the HTTP client and the endpoint (MIL-053)."""

import logging
import os
import threading
import time
from http.server import ThreadingHTTPServer

from scan_exporter.application.collector import Collector
from scan_exporter.infrastructure.http_json_client import UrllibJsonClient
from scan_exporter.infrastructure.settings import settings_from_environment
from scan_exporter.presentation.metrics_endpoint import make_handler

LOG = logging.getLogger(__name__)


def refresh_forever(collector: Collector, interval: float, stop: threading.Event) -> None:
    """Read the sources now and then every interval, until stop is set."""
    while not stop.is_set():
        collector.collect()
        stop.wait(interval)


def main() -> None:
    """Start the refresh loop and serve /metrics until the container stops."""
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    settings = settings_from_environment()
    if settings.github_token is None:
        LOG.warning("no GitHub token file: code scanning is not read (wiki page Monitoring)")
    collector = Collector(settings, UrllibJsonClient(), time.time)
    interval = float(os.environ.get("SCAN_INTERVAL_SECONDS", "900"))
    stop = threading.Event()
    threading.Thread(target=refresh_forever, args=(collector, interval, stop), daemon=True).start()
    port = int(os.environ.get("SCAN_EXPORTER_PORT", "9120"))
    LOG.info("serving /metrics on port %d, reading every %d seconds", port, interval)
    # Plain HTTP on the compose network, as every exporter that Prometheus scrapes here: no
    # port is published, and the metrics hold no secret.
    server = ThreadingHTTPServer(("0.0.0.0", port), make_handler(collector))  # noqa: S104  # NOSONAR
    server.serve_forever()  # NOSONAR - plain HTTP on purpose, see above
