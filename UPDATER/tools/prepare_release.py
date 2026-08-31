"""Prepare a reviewed release-catalog entry for a new Stream Suite package.

This creator-only helper never uploads anything. It calculates the package hash and
size, checks the ZIP layout, copies compatibility defaults from the newest release,
and writes a separate catalog file for review.
"""

from __future__ import annotations

import argparse
import copy
import json
import sys
import zipfile
from datetime import UTC, datetime
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from updater_core import (
    ManifestError,
    ReleaseCatalog,
    SemanticVersion,
    sha256_file,
)

OWNER = "babydreamsy94"
REPOSITORY = "STREAM-SUITE"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Prepare a Stream Suite release catalog entry."
    )
    parser.add_argument("package", type=Path, help="Final public Stream Suite ZIP.")
    parser.add_argument("--version", required=True, help="Semantic version, such as 4.1.0.")
    parser.add_argument("--display-version", required=True, help="Friendly UI version, such as 4.1.")
    parser.add_argument("--release-name", required=True, help="Full release title.")
    parser.add_argument("--tag", required=True, help="GitHub Release tag, such as v4.1.0.")
    parser.add_argument(
        "--release-date",
        default=datetime.now(UTC).date().isoformat(),
        help="Release date in YYYY-MM-DD form.",
    )
    parser.add_argument(
        "--guide-file",
        required=True,
        help="START_HERE.txt path inside the ZIP.",
    )
    parser.add_argument(
        "--streamer-bot-tested",
        required=True,
        help="Streamer.bot version used for release testing.",
    )
    parser.add_argument(
        "--note",
        action="append",
        required=True,
        help="Release note. Repeat this option for each bullet.",
    )
    parser.add_argument(
        "--notice",
        default="",
        help="Optional important notice shown above the release-note bullets.",
    )
    parser.add_argument("--breaking", action="store_true", help="Mark setup changes as breaking.")
    parser.add_argument(
        "--catalog",
        type=Path,
        default=PROJECT_ROOT / "deployment" / "release-catalog.json",
        help="Current release catalog used as the base.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=PROJECT_ROOT / "deployment" / "release-catalog.next.json",
        help="Separate review file to create. Existing files are not overwritten.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    version = str(SemanticVersion.parse(args.version))
    streamer_bot_tested = str(SemanticVersion.parse(args.streamer_bot_tested))
    package = args.package.resolve()
    if not package.is_file() or package.suffix.lower() != ".zip":
        raise SystemExit("The package must be an existing ZIP file.")
    if args.output.exists():
        raise SystemExit(f"Refusing to overwrite existing review file: {args.output}")

    try:
        current_data = json.loads(args.catalog.read_text(encoding="utf-8"))
        current_catalog = ReleaseCatalog.from_mapping(current_data)
    except (OSError, json.JSONDecodeError, ValueError, ManifestError) as exc:
        raise SystemExit(f"Could not read the current release catalog: {exc}") from exc
    if any(release.suite_version == version for release in current_catalog.releases):
        raise SystemExit(f"Version {version} already exists in the release catalog.")

    guide_normalized = args.guide_file.replace("\\", "/").lstrip("/")
    try:
        with zipfile.ZipFile(package, "r") as archive:
            names = {name.replace("\\", "/").lstrip("/") for name in archive.namelist()}
    except zipfile.BadZipFile as exc:
        raise SystemExit(f"The package is not a valid ZIP: {exc}") from exc
    if guide_normalized not in names:
        raise SystemExit(f"Guide file was not found inside the package: {guide_normalized}")
    if not any(name.lower().endswith(".sb") for name in names):
        raise SystemExit("The package contains no Streamer.bot .sb import files.")

    newest_raw = max(
        current_data["releases"],
        key=lambda item: SemanticVersion.parse(item["suiteVersion"]),
    )
    new_entry = copy.deepcopy(newest_raw)
    new_entry.update(
        {
            "schemaVersion": 1,
            "suiteVersion": version,
            "displayVersion": args.display_version,
            "releaseName": args.release_name,
            "releaseDate": args.release_date,
            "channel": "stable",
            "package": {
                "fileName": package.name,
                "url": (
                    f"https://github.com/{OWNER}/{REPOSITORY}/releases/download/"
                    f"{args.tag}/{package.name}"
                ),
                "sha256": sha256_file(package),
                "sizeBytes": package.stat().st_size,
            },
            "installation": {
                "mode": "guided-import",
                "guideFile": guide_normalized,
                "requiresBackup": True,
            },
            "releaseNotes": args.note,
            "releaseNotesUrl": (
                f"https://github.com/{OWNER}/{REPOSITORY}/releases/tag/{args.tag}"
            ),
            "breakingChanges": bool(args.breaking),
            "notice": args.notice,
        }
    )
    new_entry["streamerBot"]["testedVersion"] = streamer_bot_tested

    next_data = copy.deepcopy(current_data)
    next_data["releases"].append(new_entry)
    # Validate the complete result before writing it.
    ReleaseCatalog.from_mapping(next_data)
    next_data["releases"].sort(
        key=lambda item: SemanticVersion.parse(item["suiteVersion"]), reverse=True
    )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(next_data, indent=2) + "\n", encoding="utf-8")
    print(f"Prepared {args.output}")
    print(f"Version: {version}")
    print(f"Package size: {package.stat().st_size} bytes")
    print(f"SHA-256: {sha256_file(package)}")
    print("Review the file before replacing the live release-catalog.json.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
