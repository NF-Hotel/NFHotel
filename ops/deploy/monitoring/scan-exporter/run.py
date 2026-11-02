"""Start the scan exporter; the compose file runs ``python -B -E -s /app/run.py``."""

from scan_exporter.main import main

if __name__ == "__main__":
    main()
