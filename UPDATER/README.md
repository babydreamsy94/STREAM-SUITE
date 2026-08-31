# Stream Suite Update Center

> **Built By Streamers. Powered by Community.**
>
> Created for Stream Suite by [babydreamsy](https://www.twitch.tv/babydreamsy)

Stream Suite Update Center is a safe companion application for discovering,
downloading, verifying, and unpacking official Stream Suite releases.

It replaces the need for existing users to return to Ko-fi every time a new
package is published. Ko-fi can remain the initial download and information page,
while versioned packages live in GitHub Releases.

## Current status

This repository contains the complete **0.1.0 MVP source project**:

- Working desktop interface
- Latest-version checks
- Version Library and previous-version downloads
- Upgrade, reinstall, and downgrade detection
- Streamer.bot compatibility information
- Release notes and important notices
- Download progress
- Required SHA-256 verification
- Safe ZIP extraction with traversal, symlink, duplicate-path, file-count, and
  expanded-size defenses
- Persistent installed-version and download-folder settings
- Guided setup handoff without automatic Streamer.bot modification
- Automated tests
- Automated portable Windows `.exe` build through GitHub Actions
- Creator-side release preparation helper

The production feed will not become live until the included release catalog is
committed to the public repository and the matching Version 4.0 package is attached
to a GitHub Release. See [Publishing an Update](docs/PUBLISHING_AN_UPDATE.md).

## What users experience

When the program opens, it displays:

- Their recorded installed version
- The newest stable Stream Suite version
- The selected release channel
- Streamer.bot and operating-environment compatibility
- Release notes
- A selectable Version Library

The main action changes according to the selected version:

| Selection | Action shown |
|---|---|
| Newer than installed | **Download Update** |
| Same as installed | **Download Again** |
| Older than installed | **Download Downgrade** |
| Installed version not set | **Download Package** |

After a package is downloaded, verified, and extracted, the user can open its
folder and setup guide. The program only marks a version as installed after the
user confirms that they completed the Streamer.bot import.

## Why imports remain guided

Streamer.bot's supported import flow lets the user inspect included items and
choose whether matching actions should be overwritten. Imported items with matching
names are overwritten by default, which means a silent replacement could remove
personal configuration. See the official
[Streamer.bot Import & Export guide](https://docs.streamer.bot/guide/core/import-export).

For that reason, Update Center does **not**:

- Edit Streamer.bot's data directory
- Trigger imports automatically
- Overwrite actions silently
- Delete a user's variables or analytics history
- Claim a downloaded package is installed
- Store Twitch credentials, GitHub tokens, or Streamer.bot authentication

## Downgrades and recovery

The Version Library keeps previous packages available instead of replacing the
same file for every release.

A downgrade is intentionally treated as a recovery operation. Older actions may
not understand data or variables written by a newer version. When a recent update
caused a problem, restoring the Streamer.bot backup created before that update is
usually safer than importing older actions over newer ones.

Read [Recovery and Downgrades](docs/RECOVERY_AND_DOWNGRADES.md) before testing this
feature.

## Project structure

```text
.
├── src/
│   ├── stream_suite_updater.py       Desktop interface
│   └── updater_core.py               Version, download, verification, and ZIP safety
├── tests/
│   └── test_updater_core.py          Automated core and security tests
├── deployment/
│   ├── release-catalog.json          Production Version Library
│   ├── update-manifest.json          Single-release compatibility example
│   └── *.schema.json                 Catalog and manifest schemas
├── tools/
│   └── prepare_release.py            Creator-only release metadata helper
├── docs/
│   ├── PUBLISHING_AN_UPDATE.md
│   ├── RECOVERY_AND_DOWNGRADES.md
│   └── RELEASE_CATALOG_REFERENCE.md
├── packaging/
│   └── windows_version_info.txt
├── .github/workflows/
│   └── build-updater.yml             Windows EXE build
├── StreamSuiteUpdateCenter.spec      PyInstaller build definition
├── build_windows.ps1                 Local Windows build script
└── run_source.bat                    Run from source on Windows
```

## Run from source

Requirements:

- Python 3.12
- Tk support, included with normal Python.org Windows installations
- No third-party runtime packages

On Windows, double-click `run_source.bat`, or run:

```powershell
py -3.12 src\stream_suite_updater.py
```

To validate a local catalog without opening the interface:

```powershell
py -3.12 src\stream_suite_updater.py --diagnostics --manifest deployment\release-catalog.json
```

## Build the portable Windows application

### GitHub method

1. Commit this project's files to the root of the `STREAM-SUITE` repository.
2. Open the repository's **Actions** tab.
3. Select **Build Stream Suite Update Center**.
4. Select **Run workflow**.
5. When it finishes, download the `Stream-Suite-Update-Center-Windows` artifact.

The workflow runs the test suite before producing `StreamSuiteUpdateCenter.exe`.
It also produces a SHA-256 file for the executable.

### Local Windows method

Open PowerShell inside this project and run:

```powershell
.\build_windows.ps1
```

The finished application is created at:

```text
dist\StreamSuiteUpdateCenter.exe
```

The build uses [PyInstaller](https://pypi.org/project/pyinstaller/) only while
creating the executable. End users do not need Python or PyInstaller.

## Tests

The application uses Python's built-in `unittest` framework:

```powershell
py -3.12 -m unittest discover -s tests -v
```

The current suite covers:

- Friendly and semantic version parsing
- Version ordering
- Update detection
- Release-manifest validation
- Trusted download hosts
- Required SHA-256 values
- Release-catalog ordering and lookup
- ZIP path traversal using `/` and `\`
- ZIP symlinks
- Duplicate case-insensitive paths
- Guide and `.sb` discovery
- Settings round trips
- Unique extraction directories

## Distribution model

| Location | Responsibility |
|---|---|
| Ko-fi | Initial download, project story, and public landing page |
| GitHub Releases | Permanent versioned Stream Suite ZIP packages |
| Release catalog | Latest-version and Version Library metadata |
| Update Center | Checks, downloads, verifies, and extracts |
| Streamer.bot | User-reviewed action import and configuration |

Published GitHub Releases can be read publicly without putting a GitHub token in
the application. GitHub documents public release access in its
[Releases API documentation](https://docs.github.com/en/rest/releases/releases).

## Known MVP limitations

- The app records the installed version only after user confirmation; it does not
  interrogate Streamer.bot.
- It cannot prove that every imported action was configured correctly.
- It downloads older packages but does not reverse data-schema changes.
- Windows is the intended executable target. Linux/Wine remains explicitly marked
  as unverified because Stream Suite's imported .NET reference paths may behave
  differently there.
- The first unsigned Windows executable may display an **Unknown publisher** warning.
- Updating the Update Center application itself is not automatic in Version 0.1.0.

## Suggested next milestones

1. Publish and test the Version 4.0 GitHub Release and live catalog.
2. Build and test the portable executable on a clean Windows account.
3. Add a Stream Suite-branded application icon.
4. Add Update Center self-update notifications.
5. Separate creator configuration from action code before considering more
   automatic Streamer.bot updates.

