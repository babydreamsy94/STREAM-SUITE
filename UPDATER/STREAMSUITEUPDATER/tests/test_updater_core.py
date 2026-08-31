from __future__ import annotations

import hashlib
import stat
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT_ROOT / "src"))

from updater_core import (
    AppSettings,
    ManifestError,
    ReleaseCatalog,
    ReleaseManifest,
    SemanticVersion,
    UnsafeArchiveError,
    classify_selection,
    load_manifest,
    load_release_catalog,
    load_settings,
    safe_extract_zip,
    save_settings,
    sha256_file,
    unique_directory,
    update_available,
)


def manifest_payload() -> dict:
    return {
        "schemaVersion": 1,
        "suiteVersion": "4.1.0",
        "displayVersion": "4.1",
        "releaseName": "Stream Suite 4.1",
        "releaseDate": "2026-09-01",
        "channel": "stable",
        "streamerBot": {"minimumVersion": "1.0.7", "testedVersion": "1.0.7"},
        "platforms": [
            {"name": "Windows", "status": "verified", "note": "Tested."}
        ],
        "package": {
            "fileName": "Stream_Suite_4.1.zip",
            "url": "https://github.com/example/project/releases/download/v4.1.0/Stream_Suite_4.1.zip",
            "sha256": "a" * 64,
            "sizeBytes": 123,
        },
        "installation": {
            "mode": "guided-import",
            "guideFile": "Stream_Suite_4.1/START_HERE.txt",
            "requiresBackup": True,
        },
        "releaseNotes": ["One", "Two"],
        "releaseNotesUrl": "https://github.com/example/project/releases/tag/v4.1.0",
        "breakingChanges": False,
        "notice": "Test release.",
    }


class SemanticVersionTests(unittest.TestCase):
    def test_friendly_and_semantic_versions(self) -> None:
        self.assertEqual(str(SemanticVersion.parse("v4.0 FINAL")), "4.0.0")
        self.assertEqual(str(SemanticVersion.parse("4.1")), "4.1.0")
        self.assertEqual(str(SemanticVersion.parse("5")), "5.0.0")

    def test_ordering(self) -> None:
        self.assertLess(SemanticVersion.parse("4.0.1"), SemanticVersion.parse("4.1.0"))
        self.assertLess(
            SemanticVersion.parse("4.1.0-beta.1"), SemanticVersion.parse("4.1.0")
        )
        self.assertTrue(update_available("4.0.0", "4.1.0"))
        self.assertFalse(update_available("4.1.0", "4.1.0"))

    def test_upgrade_reinstall_and_downgrade_classification(self) -> None:
        self.assertEqual(classify_selection(None, "4.0.0"), "package")
        self.assertEqual(classify_selection("4.0.0", "4.1.0"), "update")
        self.assertEqual(classify_selection("4.1.0", "4.1.0"), "reinstall")
        self.assertEqual(classify_selection("4.1.0", "4.0.0"), "downgrade")


class ManifestTests(unittest.TestCase):
    def test_valid_manifest(self) -> None:
        manifest = ReleaseManifest.from_mapping(manifest_payload())
        self.assertEqual(manifest.suite_version, "4.1.0")
        self.assertEqual(manifest.package.size_bytes, 123)
        self.assertTrue(manifest.installation.requires_backup)

    def test_untrusted_package_host_is_rejected(self) -> None:
        payload = manifest_payload()
        payload["package"]["url"] = "https://example.com/update.zip"
        with self.assertRaises(ManifestError):
            ReleaseManifest.from_mapping(payload)

    def test_bad_checksum_is_rejected(self) -> None:
        payload = manifest_payload()
        payload["package"]["sha256"] = "not-a-checksum"
        with self.assertRaises(ManifestError):
            ReleaseManifest.from_mapping(payload)

    def test_invalid_release_date_is_rejected(self) -> None:
        payload = manifest_payload()
        payload["releaseDate"] = "September someday"
        with self.assertRaises(ManifestError):
            ReleaseManifest.from_mapping(payload)

    def test_automatic_install_mode_is_rejected(self) -> None:
        payload = manifest_payload()
        payload["installation"]["mode"] = "silent-overwrite"
        with self.assertRaises(ManifestError):
            ReleaseManifest.from_mapping(payload)

    def test_production_manifest_parses(self) -> None:
        manifest = load_manifest(PROJECT_ROOT / "deployment" / "update-manifest.json")
        self.assertEqual(manifest.suite_version, "4.0.0")
        self.assertEqual(len(manifest.package.sha256), 64)

    def test_release_catalog_sorts_newest_first(self) -> None:
        older = manifest_payload()
        older["suiteVersion"] = "4.0.0"
        older["displayVersion"] = "4.0"
        newer = manifest_payload()
        catalog = ReleaseCatalog.from_mapping(
            {
                "schemaVersion": 1,
                "defaultChannel": "stable",
                "releases": [older, newer],
            }
        )
        self.assertEqual([item.suite_version for item in catalog.releases], ["4.1.0", "4.0.0"])
        self.assertEqual(catalog.latest().suite_version, "4.1.0")
        self.assertEqual(catalog.get("4.0").suite_version, "4.0.0")

    def test_production_catalog_parses(self) -> None:
        catalog = load_release_catalog(PROJECT_ROOT / "deployment" / "release-catalog.json")
        self.assertEqual(catalog.latest().suite_version, "4.0.0")
        self.assertEqual(len(catalog.releases), 1)


class ArchiveSafetyTests(unittest.TestCase):
    def test_safe_extract_finds_guide_and_imports(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive_path = root / "package.zip"
            with zipfile.ZipFile(archive_path, "w") as archive:
                archive.writestr("Stream_Suite/START_HERE.txt", "Hello")
                archive.writestr("Stream_Suite/Main Package/Stream_Suite.sb", "import")
            extracted = safe_extract_zip(
                archive_path,
                root / "out",
                guide_file="Stream_Suite/START_HERE.txt",
            )
            self.assertTrue(extracted.guide_file and extracted.guide_file.is_file())
            self.assertEqual(len(extracted.streamer_bot_imports), 1)

    def test_parent_traversal_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive_path = root / "bad.zip"
            with zipfile.ZipFile(archive_path, "w") as archive:
                archive.writestr("../outside.sb", "bad")
            with self.assertRaises(UnsafeArchiveError):
                safe_extract_zip(archive_path, root / "out")

    def test_backslash_traversal_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive_path = root / "bad.zip"
            with zipfile.ZipFile(archive_path, "w") as archive:
                archive.writestr("folder\\..\\outside.sb", "bad")
            with self.assertRaises(UnsafeArchiveError):
                safe_extract_zip(archive_path, root / "out")

    def test_symlink_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive_path = root / "bad.zip"
            with zipfile.ZipFile(archive_path, "w") as archive:
                link = zipfile.ZipInfo("Stream_Suite/link")
                link.create_system = 3
                link.external_attr = (stat.S_IFLNK | 0o777) << 16
                archive.writestr(link, "target")
                archive.writestr("Stream_Suite/Main.sb", "import")
            with self.assertRaises(UnsafeArchiveError):
                safe_extract_zip(archive_path, root / "out")

    def test_duplicate_case_insensitive_path_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive_path = root / "bad.zip"
            with zipfile.ZipFile(archive_path, "w") as archive:
                archive.writestr("Stream_Suite/Main.sb", "one")
                archive.writestr("stream_suite/main.sb", "two")
            with self.assertRaises(UnsafeArchiveError):
                safe_extract_zip(archive_path, root / "out")


class SettingsAndHelpersTests(unittest.TestCase):
    def test_settings_round_trip(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "settings.json"
            expected = AppSettings("4.0.0", str(Path(temporary) / "downloads"))
            save_settings(expected, path)
            actual = load_settings(path)
            self.assertEqual(actual.installed_version, "4.0.0")
            self.assertEqual(actual.download_directory, expected.download_directory)

    def test_sha256(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "file.bin"
            path.write_bytes(b"stream suite")
            self.assertEqual(sha256_file(path), hashlib.sha256(b"stream suite").hexdigest())

    def test_unique_directory_does_not_reuse_existing_folder(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            parent = Path(temporary)
            (parent / "Stream Suite 4.0").mkdir()
            result = unique_directory(parent, "Stream Suite 4.0")
            self.assertEqual(result.name, "Stream Suite 4.0 (2)")


if __name__ == "__main__":
    unittest.main()
