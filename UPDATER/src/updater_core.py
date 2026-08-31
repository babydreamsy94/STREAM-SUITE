"""Core update logic for Stream Suite Update Center.

This module deliberately contains no GUI code so its security-sensitive pieces can
be tested on every platform. The updater downloads a ZIP identified by its published
SHA-256 value and extracts it to a new folder. It never modifies Streamer.bot's data
directory.
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import shutil
import stat
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from collections.abc import Callable, Iterable, Mapping
from dataclasses import dataclass, field
from datetime import date
from functools import total_ordering
from pathlib import Path, PurePosixPath
from typing import Any

APP_USER_AGENT = "Stream-Suite-Update-Center/0.1"
MAX_MANIFEST_BYTES = 1 * 1024 * 1024
MAX_DOWNLOAD_BYTES = 100 * 1024 * 1024
MAX_ARCHIVE_FILES = 5_000
MAX_EXTRACTED_BYTES = 500 * 1024 * 1024

# The remote manifest cannot add a new trusted host. Changing this list requires a
# new updater build, preventing a modified manifest from silently moving downloads
# to an unrelated server.
TRUSTED_REMOTE_HOSTS = frozenset(
    {
        "github.com",
        "api.github.com",
        "raw.githubusercontent.com",
        "objects.githubusercontent.com",
        "release-assets.githubusercontent.com",
    }
)


class UpdaterError(Exception):
    """Base class for errors that can be safely shown to an updater user."""


class ManifestError(UpdaterError):
    """Raised when release metadata is missing, malformed, or untrusted."""


class DownloadError(UpdaterError):
    """Raised when an update package cannot be downloaded safely."""


class VerificationError(UpdaterError):
    """Raised when a downloaded file does not match its release metadata."""


class UnsafeArchiveError(UpdaterError):
    """Raised when a ZIP contains unsafe paths or unsupported entries."""


@total_ordering
@dataclass(frozen=True)
class SemanticVersion:
    """Small SemVer implementation sufficient for Stream Suite releases."""

    major: int
    minor: int
    patch: int
    prerelease: tuple[int | str, ...] = field(default_factory=tuple)

    @classmethod
    def parse(cls, value: str) -> SemanticVersion:
        if not isinstance(value, str) or not value.strip():
            raise ValueError("Version cannot be empty.")

        cleaned = value.strip()
        if cleaned.lower().startswith("v"):
            cleaned = cleaned[1:].strip()

        # Accept friendly values such as "4.0 FINAL" while storing/comparing them
        # as ordinary stable semantic versions.
        cleaned = re.sub(r"\s+FINAL$", "", cleaned, flags=re.IGNORECASE).strip()
        match = re.fullmatch(
            r"(?P<major>0|[1-9]\d*)"
            r"(?:\.(?P<minor>0|[1-9]\d*))?"
            r"(?:\.(?P<patch>0|[1-9]\d*))?"
            r"(?:-(?P<pre>[0-9A-Za-z.-]+))?"
            r"(?:\+[0-9A-Za-z.-]+)?",
            cleaned,
        )
        if not match:
            raise ValueError(
                f"'{value}' is not a valid version. Use a value such as 4.0.0."
            )

        prerelease: list[int | str] = []
        raw_pre = match.group("pre")
        if raw_pre:
            for part in raw_pre.split("."):
                prerelease.append(int(part) if part.isdigit() else part.lower())

        return cls(
            int(match.group("major")),
            int(match.group("minor") or 0),
            int(match.group("patch") or 0),
            tuple(prerelease),
        )

    def __str__(self) -> str:
        result = f"{self.major}.{self.minor}.{self.patch}"
        if self.prerelease:
            result += "-" + ".".join(str(item) for item in self.prerelease)
        return result

    def __lt__(self, other: object) -> bool:
        if not isinstance(other, SemanticVersion):
            return NotImplemented
        left = (self.major, self.minor, self.patch)
        right = (other.major, other.minor, other.patch)
        if left != right:
            return left < right
        if not self.prerelease:
            return False
        if not other.prerelease:
            return True

        for left_part, right_part in zip(self.prerelease, other.prerelease):
            if left_part == right_part:
                continue
            if isinstance(left_part, int) and isinstance(right_part, str):
                return True
            if isinstance(left_part, str) and isinstance(right_part, int):
                return False
            return left_part < right_part
        return len(self.prerelease) < len(other.prerelease)


@dataclass(frozen=True)
class PlatformSupport:
    name: str
    status: str
    note: str = ""


@dataclass(frozen=True)
class PackageInfo:
    file_name: str
    url: str
    sha256: str
    size_bytes: int | None = None


@dataclass(frozen=True)
class InstallationInfo:
    mode: str
    guide_file: str
    requires_backup: bool


@dataclass(frozen=True)
class ReleaseManifest:
    schema_version: int
    suite_version: str
    display_version: str
    release_name: str
    release_date: str
    channel: str
    streamer_bot_minimum: str
    streamer_bot_tested: str
    platforms: tuple[PlatformSupport, ...]
    package: PackageInfo
    installation: InstallationInfo
    release_notes: tuple[str, ...]
    release_notes_url: str
    breaking_changes: bool
    notice: str = ""

    @property
    def semantic_version(self) -> SemanticVersion:
        return SemanticVersion.parse(self.suite_version)

    @classmethod
    def from_mapping(cls, data: Mapping[str, Any]) -> ReleaseManifest:
        if not isinstance(data, Mapping):
            raise ManifestError("The update manifest must contain a JSON object.")

        schema_version = data.get("schemaVersion")
        if schema_version != 1:
            raise ManifestError(
                f"Unsupported manifest schema '{schema_version}'. This updater supports schema 1."
            )

        suite_version = _required_string(data, "suiteVersion")
        try:
            SemanticVersion.parse(suite_version)
        except ValueError as exc:
            raise ManifestError(str(exc)) from exc

        release_date = _required_string(data, "releaseDate")
        try:
            date.fromisoformat(release_date)
        except ValueError as exc:
            raise ManifestError("releaseDate must use YYYY-MM-DD format.") from exc

        channel = _required_string(data, "channel").lower()
        if channel not in {"stable", "beta"}:
            raise ManifestError("channel must be either 'stable' or 'beta'.")

        package_data = _required_mapping(data, "package")
        package_url = _required_string(package_data, "url")
        validate_remote_url(package_url)

        file_name = _required_string(package_data, "fileName")
        if Path(file_name).name != file_name or not file_name.lower().endswith(".zip"):
            raise ManifestError("package.fileName must be a plain ZIP filename.")

        sha256 = _required_string(package_data, "sha256").lower()
        if not re.fullmatch(r"[0-9a-f]{64}", sha256):
            raise ManifestError("package.sha256 must be a 64-character SHA-256 value.")

        size_bytes_raw = package_data.get("sizeBytes")
        size_bytes: int | None = None
        if size_bytes_raw is not None:
            if not isinstance(size_bytes_raw, int) or isinstance(size_bytes_raw, bool):
                raise ManifestError("package.sizeBytes must be a whole number.")
            if size_bytes_raw <= 0 or size_bytes_raw > MAX_DOWNLOAD_BYTES:
                raise ManifestError(
                    f"package.sizeBytes must be between 1 and {MAX_DOWNLOAD_BYTES}."
                )
            size_bytes = size_bytes_raw

        streamer_bot = _required_mapping(data, "streamerBot")
        minimum = _required_string(streamer_bot, "minimumVersion")
        tested = _required_string(streamer_bot, "testedVersion")
        for version in (minimum, tested):
            try:
                SemanticVersion.parse(version)
            except ValueError as exc:
                raise ManifestError(f"Invalid Streamer.bot version: {exc}") from exc

        platform_entries = data.get("platforms", [])
        if not isinstance(platform_entries, list) or not platform_entries:
            raise ManifestError("platforms must contain at least one supported environment.")
        platforms: list[PlatformSupport] = []
        allowed_statuses = {"verified", "experimental", "not-verified", "unsupported"}
        for entry in platform_entries:
            if not isinstance(entry, Mapping):
                raise ManifestError("Each platforms entry must be a JSON object.")
            status = _required_string(entry, "status")
            if status not in allowed_statuses:
                raise ManifestError(f"Unknown platform status '{status}'.")
            platforms.append(
                PlatformSupport(
                    name=_required_string(entry, "name"),
                    status=status,
                    note=_optional_string(entry, "note"),
                )
            )

        installation_data = _required_mapping(data, "installation")
        install_mode = _required_string(installation_data, "mode")
        if install_mode != "guided-import":
            raise ManifestError("Only the safe 'guided-import' installation mode is supported.")
        guide_file = _required_string(installation_data, "guideFile")
        _validate_relative_archive_path(guide_file, "installation.guideFile")
        requires_backup = installation_data.get("requiresBackup")
        if not isinstance(requires_backup, bool):
            raise ManifestError("installation.requiresBackup must be true or false.")

        notes_raw = data.get("releaseNotes", [])
        if isinstance(notes_raw, str):
            notes = (notes_raw.strip(),) if notes_raw.strip() else ()
        elif isinstance(notes_raw, list) and all(
            isinstance(note, str) and note.strip() for note in notes_raw
        ):
            notes = tuple(note.strip() for note in notes_raw)
        else:
            raise ManifestError("releaseNotes must be a string or a list of non-empty strings.")

        notes_url = _optional_string(data, "releaseNotesUrl")
        if notes_url:
            validate_remote_url(notes_url)

        breaking_changes = data.get("breakingChanges", False)
        if not isinstance(breaking_changes, bool):
            raise ManifestError("breakingChanges must be true or false.")

        return cls(
            schema_version=1,
            suite_version=str(SemanticVersion.parse(suite_version)),
            display_version=_required_string(data, "displayVersion"),
            release_name=_required_string(data, "releaseName"),
            release_date=release_date,
            channel=channel,
            streamer_bot_minimum=str(SemanticVersion.parse(minimum)),
            streamer_bot_tested=str(SemanticVersion.parse(tested)),
            platforms=tuple(platforms),
            package=PackageInfo(file_name, package_url, sha256, size_bytes),
            installation=InstallationInfo(
                mode=install_mode,
                guide_file=guide_file,
                requires_backup=requires_backup,
            ),
            release_notes=notes,
            release_notes_url=notes_url,
            breaking_changes=breaking_changes,
            notice=_optional_string(data, "notice"),
        )


@dataclass(frozen=True)
class ReleaseCatalog:
    """Ordered collection of installable Stream Suite releases."""

    schema_version: int
    default_channel: str
    releases: tuple[ReleaseManifest, ...]

    @classmethod
    def from_mapping(cls, data: Mapping[str, Any]) -> ReleaseCatalog:
        # A single-release manifest remains supported so early deployments do not
        # break when the Version Library is introduced.
        if "releases" not in data:
            release = ReleaseManifest.from_mapping(data)
            return cls(1, release.channel, (release,))

        if data.get("schemaVersion") != 1:
            raise ManifestError("Unsupported release catalog schema.")
        default_channel = _required_string(data, "defaultChannel").lower()
        if default_channel not in {"stable", "beta"}:
            raise ManifestError("defaultChannel must be either 'stable' or 'beta'.")
        raw_releases = data.get("releases")
        if not isinstance(raw_releases, list) or not raw_releases:
            raise ManifestError("The release catalog must contain at least one release.")

        releases: list[ReleaseManifest] = []
        versions: set[str] = set()
        for raw_release in raw_releases:
            if not isinstance(raw_release, Mapping):
                raise ManifestError("Each release catalog entry must be a JSON object.")
            release = ReleaseManifest.from_mapping(raw_release)
            if release.suite_version in versions:
                raise ManifestError(
                    f"The release catalog contains duplicate version {release.suite_version}."
                )
            versions.add(release.suite_version)
            releases.append(release)

        releases.sort(key=lambda item: item.semantic_version, reverse=True)
        if not any(item.channel == default_channel for item in releases):
            raise ManifestError(
                f"The catalog contains no releases in its default '{default_channel}' channel."
            )
        return cls(1, default_channel, tuple(releases))

    def latest(self, channel: str | None = None) -> ReleaseManifest:
        wanted = (channel or self.default_channel).lower()
        for release in self.releases:
            if release.channel == wanted:
                return release
        raise ManifestError(f"No releases are available in the '{wanted}' channel.")

    def get(self, version: str) -> ReleaseManifest:
        normalized = str(SemanticVersion.parse(version))
        for release in self.releases:
            if release.suite_version == normalized:
                return release
        raise ManifestError(f"Version {normalized} is not available in the release catalog.")


@dataclass(frozen=True)
class ExtractedPackage:
    root: Path
    guide_file: Path | None
    streamer_bot_imports: tuple[Path, ...]


@dataclass
class AppSettings:
    installed_version: str | None
    download_directory: str

    @classmethod
    def defaults(cls) -> AppSettings:
        return cls(None, str(Path.home() / "Downloads" / "Stream Suite Updates"))

    @classmethod
    def from_mapping(cls, data: Mapping[str, Any]) -> AppSettings:
        defaults = cls.defaults()
        installed = data.get("installedVersion")
        if installed is not None:
            installed = str(SemanticVersion.parse(str(installed)))
        directory = data.get("downloadDirectory", defaults.download_directory)
        if not isinstance(directory, str) or not directory.strip():
            directory = defaults.download_directory
        return cls(installed, directory)

    def as_mapping(self) -> dict[str, Any]:
        return {
            "installedVersion": self.installed_version,
            "downloadDirectory": self.download_directory,
        }


def _required_mapping(data: Mapping[str, Any], key: str) -> Mapping[str, Any]:
    value = data.get(key)
    if not isinstance(value, Mapping):
        raise ManifestError(f"'{key}' must be a JSON object.")
    return value


def _required_string(data: Mapping[str, Any], key: str) -> str:
    value = data.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ManifestError(f"'{key}' must be a non-empty string.")
    return value.strip()


def _optional_string(data: Mapping[str, Any], key: str) -> str:
    value = data.get(key, "")
    if value is None:
        return ""
    if not isinstance(value, str):
        raise ManifestError(f"'{key}' must be a string when provided.")
    return value.strip()


def _validate_relative_archive_path(value: str, field_name: str) -> None:
    normalized = value.replace("\\", "/")
    path = PurePosixPath(normalized)
    if path.is_absolute() or ".." in path.parts or any(":" in part for part in path.parts):
        raise ManifestError(f"{field_name} must be a safe relative path.")


def validate_remote_url(url: str, allowed_hosts: Iterable[str] = TRUSTED_REMOTE_HOSTS) -> None:
    try:
        parsed = urllib.parse.urlsplit(url)
    except ValueError as exc:
        raise ManifestError(f"Invalid update URL: {exc}") from exc
    allowed = {host.lower() for host in allowed_hosts}
    try:
        port = parsed.port
    except ValueError as exc:
        raise ManifestError(f"Invalid update URL port: {exc}") from exc
    if parsed.scheme.lower() != "https" or not parsed.hostname:
        raise ManifestError("Remote update URLs must use HTTPS.")
    if parsed.username or parsed.password or port not in (None, 443):
        raise ManifestError("Remote update URLs cannot contain credentials or custom ports.")
    if parsed.hostname.lower() not in allowed:
        raise ManifestError(
            f"Update host '{parsed.hostname}' is not trusted by this updater build."
        )


def _load_json_source(source: str | os.PathLike[str], timeout: int = 15) -> Mapping[str, Any]:
    source_text = os.fspath(source)
    if source_text.lower().startswith(("https://", "http://")):
        validate_remote_url(source_text)
        request = urllib.request.Request(
            source_text,
            headers={"Accept": "application/json", "User-Agent": APP_USER_AGENT},
        )
        try:
            with urllib.request.urlopen(request, timeout=timeout) as response:
                validate_remote_url(response.geturl())
                raw = response.read(MAX_MANIFEST_BYTES + 1)
        except (urllib.error.URLError, OSError, ManifestError) as exc:
            raise ManifestError(f"Could not retrieve update information: {exc}") from exc
        if len(raw) > MAX_MANIFEST_BYTES:
            raise ManifestError("The update manifest is unexpectedly large.")
        try:
            payload = json.loads(raw.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError) as exc:
            raise ManifestError(f"The update manifest is not valid JSON: {exc}") from exc
    else:
        path = Path(source_text).expanduser()
        try:
            if path.stat().st_size > MAX_MANIFEST_BYTES:
                raise ManifestError("The update manifest is unexpectedly large.")
            payload = json.loads(path.read_text(encoding="utf-8"))
        except ManifestError:
            raise
        except (OSError, json.JSONDecodeError) as exc:
            raise ManifestError(f"Could not read the update manifest: {exc}") from exc

    if not isinstance(payload, Mapping):
        raise ManifestError("Release information must contain a JSON object.")
    return payload


def load_release_catalog(
    source: str | os.PathLike[str], timeout: int = 15
) -> ReleaseCatalog:
    return ReleaseCatalog.from_mapping(_load_json_source(source, timeout))


def load_manifest(source: str | os.PathLike[str], timeout: int = 15) -> ReleaseManifest:
    """Load the latest stable release from either a manifest or a catalog."""

    return load_release_catalog(source, timeout).latest()


def update_available(installed: str | None, latest: str) -> bool:
    if not installed:
        return True
    return SemanticVersion.parse(installed) < SemanticVersion.parse(latest)


def classify_selection(installed: str | None, selected: str) -> str:
    """Return package, update, reinstall, or downgrade for a UI selection."""

    selected_version = SemanticVersion.parse(selected)
    if not installed:
        return "package"
    installed_version = SemanticVersion.parse(installed)
    if selected_version > installed_version:
        return "update"
    if selected_version < installed_version:
        return "downgrade"
    return "reinstall"


def sha256_file(path: str | os.PathLike[str]) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def download_package(
    manifest: ReleaseManifest,
    destination_directory: str | os.PathLike[str],
    progress: Callable[[int, int | None], None] | None = None,
    timeout: int = 30,
) -> Path:
    """Download and verify the package described by *manifest*.

    The final ZIP is placed only after the complete file passes size and SHA-256
    checks. A failed partial download is removed.
    """

    validate_remote_url(manifest.package.url)
    destination = Path(destination_directory).expanduser()
    destination.mkdir(parents=True, exist_ok=True)
    final_path = destination / manifest.package.file_name

    # Reuse an already verified package. If a different file exists under the same
    # name, preserve it and choose a new path rather than overwriting user data.
    if final_path.is_file() and not final_path.is_symlink():
        existing_size_ok = (
            manifest.package.size_bytes is None
            or final_path.stat().st_size == manifest.package.size_bytes
        )
        if existing_size_ok and sha256_file(final_path) == manifest.package.sha256:
            if progress:
                size = final_path.stat().st_size
                progress(size, manifest.package.size_bytes or size)
            return final_path
        final_path = _unique_file_path(final_path)
    elif final_path.exists() or final_path.is_symlink():
        final_path = _unique_file_path(final_path)

    partial_path = final_path.with_name(f"{final_path.name}.part")

    request = urllib.request.Request(
        manifest.package.url,
        headers={"Accept": "application/octet-stream", "User-Agent": APP_USER_AGENT},
    )
    digest = hashlib.sha256()
    received = 0

    try:
        if partial_path.exists():
            partial_path.unlink()
        with urllib.request.urlopen(request, timeout=timeout) as response:
            validate_remote_url(response.geturl())
            header_size: int | None = None
            content_length = response.headers.get("Content-Length")
            if content_length and content_length.isdigit():
                header_size = int(content_length)
                if header_size > MAX_DOWNLOAD_BYTES:
                    raise DownloadError("The update package exceeds the allowed download size.")

            expected_total = manifest.package.size_bytes or header_size
            if (
                manifest.package.size_bytes is not None
                and header_size is not None
                and manifest.package.size_bytes != header_size
            ):
                raise VerificationError(
                    "The server-reported package size does not match the release manifest."
                )

            with partial_path.open("wb") as output:
                while True:
                    chunk = response.read(64 * 1024)
                    if not chunk:
                        break
                    received += len(chunk)
                    if received > MAX_DOWNLOAD_BYTES:
                        raise DownloadError("The update package exceeds the allowed download size.")
                    output.write(chunk)
                    digest.update(chunk)
                    if progress:
                        progress(received, expected_total)

        if manifest.package.size_bytes is not None and received != manifest.package.size_bytes:
            raise VerificationError(
                f"Package size mismatch: expected {manifest.package.size_bytes} bytes, "
                f"received {received}."
            )
        if digest.hexdigest().lower() != manifest.package.sha256.lower():
            raise VerificationError(
                "The downloaded package failed SHA-256 verification and was discarded."
            )

        os.replace(partial_path, final_path)
        return final_path
    except (urllib.error.URLError, OSError) as exc:
        raise DownloadError(f"Could not download the update package: {exc}") from exc
    finally:
        if partial_path.exists():
            try:
                partial_path.unlink()
            except OSError:
                pass


def _zip_member_path(info: zipfile.ZipInfo) -> PurePosixPath:
    if "\x00" in info.filename:
        raise UnsafeArchiveError("The package contains a filename with a null character.")
    normalized = info.filename.replace("\\", "/")
    path = PurePosixPath(normalized)
    if (
        not path.parts
        or path.is_absolute()
        or ".." in path.parts
        or any(":" in part for part in path.parts)
    ):
        raise UnsafeArchiveError(f"Unsafe archive path: {info.filename}")
    if any(len(part) > 180 for part in path.parts):
        raise UnsafeArchiveError(f"Archive path component is too long: {info.filename}")
    return path


def _is_zip_symlink(info: zipfile.ZipInfo) -> bool:
    unix_mode = (info.external_attr >> 16) & 0xFFFF
    return stat.S_IFMT(unix_mode) == stat.S_IFLNK


def safe_extract_zip(
    zip_path: str | os.PathLike[str],
    destination_directory: str | os.PathLike[str],
    guide_file: str = "START_HERE.txt",
    progress: Callable[[int, int], None] | None = None,
) -> ExtractedPackage:
    """Extract a verified package while blocking path traversal and ZIP bombs."""

    source = Path(zip_path)
    root = Path(destination_directory).expanduser()
    if root.is_symlink():
        raise UnsafeArchiveError("The extraction destination cannot be a symbolic link.")
    root.mkdir(parents=True, exist_ok=True)
    root_resolved = root.resolve()

    try:
        archive = zipfile.ZipFile(source, "r")
    except (OSError, zipfile.BadZipFile) as exc:
        raise UnsafeArchiveError(f"The update package is not a readable ZIP file: {exc}") from exc

    with archive:
        entries = archive.infolist()
        if len(entries) > MAX_ARCHIVE_FILES:
            raise UnsafeArchiveError("The package contains too many files.")
        total_uncompressed = sum(info.file_size for info in entries)
        if total_uncompressed > MAX_EXTRACTED_BYTES:
            raise UnsafeArchiveError("The extracted package would be unexpectedly large.")

        seen: set[str] = set()
        planned: list[tuple[zipfile.ZipInfo, PurePosixPath]] = []
        for info in entries:
            path = _zip_member_path(info)
            normalized_key = path.as_posix().casefold()
            if normalized_key in seen:
                raise UnsafeArchiveError(f"The package contains a duplicate path: {info.filename}")
            seen.add(normalized_key)
            if info.flag_bits & 0x1:
                raise UnsafeArchiveError("Password-protected update packages are not supported.")
            if _is_zip_symlink(info):
                raise UnsafeArchiveError("Symbolic links are not allowed in update packages.")

            target = root.joinpath(*path.parts)
            target_resolved = target.resolve(strict=False)
            try:
                target_resolved.relative_to(root_resolved)
            except ValueError as exc:
                raise UnsafeArchiveError(f"Unsafe archive path: {info.filename}") from exc
            planned.append((info, path))

        for index, (info, path) in enumerate(planned, start=1):
            target = root.joinpath(*path.parts)
            if info.is_dir() or info.filename.endswith("/"):
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                if target.exists() and target.is_symlink():
                    raise UnsafeArchiveError(f"Refusing to overwrite a link: {target}")
                with archive.open(info, "r") as source_file, target.open("wb") as output_file:
                    shutil.copyfileobj(source_file, output_file, length=1024 * 1024)
            if progress:
                progress(index, len(planned))

    configured_guide = root.joinpath(*PurePosixPath(guide_file.replace("\\", "/")).parts)
    found_guide: Path | None = configured_guide if configured_guide.is_file() else None
    if found_guide is None:
        found_guide = next(root.rglob("START_HERE.txt"), None)
    imports = tuple(sorted(root.rglob("*.sb"), key=lambda item: str(item).casefold()))
    if not imports:
        raise UnsafeArchiveError("The update package contains no Streamer.bot .sb imports.")
    return ExtractedPackage(root=root, guide_file=found_guide, streamer_bot_imports=imports)


def unique_directory(parent: str | os.PathLike[str], preferred_name: str) -> Path:
    parent_path = Path(parent).expanduser()
    parent_path.mkdir(parents=True, exist_ok=True)
    candidate = parent_path / preferred_name
    counter = 2
    while candidate.exists():
        candidate = parent_path / f"{preferred_name} ({counter})"
        counter += 1
    return candidate


def _unique_file_path(preferred: Path) -> Path:
    candidate = preferred
    counter = 2
    while candidate.exists() or candidate.is_symlink():
        candidate = preferred.with_name(f"{preferred.stem} ({counter}){preferred.suffix}")
        counter += 1
    return candidate


def default_settings_path() -> Path:
    if os.name == "nt":
        base = Path(os.environ.get("LOCALAPPDATA", Path.home() / "AppData" / "Local"))
    else:
        base = Path(os.environ.get("XDG_CONFIG_HOME", Path.home() / ".config"))
    return base / "Stream Suite Update Center" / "settings.json"


def load_settings(path: str | os.PathLike[str] | None = None) -> AppSettings:
    settings_path = Path(path) if path else default_settings_path()
    if not settings_path.exists():
        return AppSettings.defaults()
    try:
        payload = json.loads(settings_path.read_text(encoding="utf-8"))
        if not isinstance(payload, Mapping):
            raise TypeError("Settings must be a JSON object.")
        return AppSettings.from_mapping(payload)
    except (OSError, json.JSONDecodeError, TypeError, ValueError) as exc:
        raise UpdaterError(f"Could not read updater settings: {exc}") from exc


def save_settings(
    settings: AppSettings, path: str | os.PathLike[str] | None = None
) -> Path:
    settings_path = Path(path) if path else default_settings_path()
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    fd, temporary_name = tempfile.mkstemp(
        prefix="settings-", suffix=".tmp", dir=settings_path.parent
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(settings.as_mapping(), handle, indent=2)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary_name, settings_path)
    finally:
        temporary = Path(temporary_name)
        if temporary.exists():
            temporary.unlink()
    return settings_path


def format_bytes(size: int | None) -> str:
    if size is None:
        return "Unknown size"
    value = float(size)
    for unit in ("bytes", "KB", "MB", "GB"):
        if value < 1024 or unit == "GB":
            return f"{value:.0f} {unit}" if unit == "bytes" else f"{value:.1f} {unit}"
        value /= 1024
    return f"{size} bytes"
