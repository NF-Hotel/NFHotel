"""Fetches the JSON of the GitHub and SonarQube Cloud APIs over HTTPS (MIL-053)."""

import json
import urllib.error
import urllib.parse
import urllib.request
from collections.abc import Mapping

from scan_exporter.application.collector import SourceError
from scan_exporter.domain.samples import Json

REQUEST_TIMEOUT_SECONDS = 30


class UrllibJsonClient:
    """Fetches JSON over HTTPS with the standard library."""

    def get_json(self, url: str, headers: Mapping[str, str]) -> Json:
        """Return the decoded JSON of the URL, or raise SourceError."""
        # The token goes in the headers, so it is never sent over plain HTTP or to a file: URL.
        if urllib.parse.urlparse(url).scheme != "https":
            raise SourceError(f"{_path(url)} is not an https URL")
        request = urllib.request.Request(url, headers=dict(headers))  # noqa: S310 - https checked above
        try:
            with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as answer:  # noqa: S310
                return json.load(answer)
        # The message names the path and the status only: the headers hold the token.
        except urllib.error.HTTPError as err:
            raise SourceError(f"{_path(url)} answered {err.code}") from err
        except (urllib.error.URLError, TimeoutError, OSError) as err:
            raise SourceError(
                f"{_path(url)} could not be reached: {err.__class__.__name__}"
            ) from err
        except json.JSONDecodeError as err:
            raise SourceError(f"{_path(url)} did not answer with JSON") from err


def _path(url: str) -> str:
    return urllib.parse.urlparse(url).path
