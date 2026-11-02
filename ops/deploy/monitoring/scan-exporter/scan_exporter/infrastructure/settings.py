"""The settings of the exporter from the environment of the compose file (MIL-053)."""

import os
from pathlib import Path

from scan_exporter.application.collector import Settings

DEFAULT_WORKFLOWS = "ci.yaml,release.yaml,e2e-dast.yaml,running-image-scan.yaml,scorecard.yaml"


def read_token(path: Path) -> str | None:
    """The token in the file, or None when the file is missing or empty."""
    try:
        token = path.read_text(encoding="utf-8").strip()
    except (FileNotFoundError, IsADirectoryError):
        return None
    return token or None


def settings_from_environment() -> Settings:
    """The settings of the compose file, with the defaults of NF Hotel."""
    token_file = Path(os.environ.get("GITHUB_TOKEN_FILE", "/run/secrets/github-scan-token"))
    return Settings(
        github_api=os.environ.get("GITHUB_API_URL", "https://api.github.com"),
        repository=os.environ.get("GITHUB_REPOSITORY", "NF-Hotel/NFHotel"),
        workflows=tuple(
            name.strip()
            for name in os.environ.get("SCAN_WORKFLOWS", DEFAULT_WORKFLOWS).split(",")
            if name.strip()
        ),
        sonar_api=os.environ.get("SONAR_API_URL", "https://sonarcloud.io/api"),
        sonar_project=os.environ.get("SONAR_PROJECT", "NF-Hotel_NFHotel"),
        github_token=read_token(token_file),
    )
