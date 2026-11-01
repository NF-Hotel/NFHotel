"""Tests of the settings read from the environment of the compose file (MIL-053)."""

from pathlib import Path

import pytest

from scan_exporter.infrastructure.settings import settings_from_environment

VARIABLES = (
    "GITHUB_API_URL",
    "GITHUB_REPOSITORY",
    "SCAN_WORKFLOWS",
    "SONAR_API_URL",
    "SONAR_PROJECT",
)


def test_settings_from_environment_without_variables_uses_the_defaults(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    for name in VARIABLES:
        monkeypatch.delenv(name, raising=False)
    monkeypatch.setenv("GITHUB_TOKEN_FILE", str(tmp_path / "missing"))

    settings = settings_from_environment()

    assert settings.github_api == "https://api.github.com"
    assert settings.repository == "NF-Hotel/NFHotel"
    assert "e2e-dast.yaml" in settings.workflows
    assert settings.sonar_project == "NF-Hotel_NFHotel"
    assert settings.github_token is None


def test_settings_from_environment_reads_variables_and_token(
    monkeypatch: pytest.MonkeyPatch, tmp_path: Path
) -> None:
    token_file = tmp_path / "github-scan-token"
    token_file.write_text("stand-in-token\n", encoding="utf-8")
    monkeypatch.setenv("GITHUB_TOKEN_FILE", str(token_file))
    monkeypatch.setenv("SCAN_WORKFLOWS", " ci.yaml, ,scorecard.yaml ")

    settings = settings_from_environment()

    assert settings.workflows == ("ci.yaml", "scorecard.yaml")
    assert settings.github_token == "stand-in-token"
