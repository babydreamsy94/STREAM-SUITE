# Release Catalog Reference

`deployment/release-catalog.json` is the source of truth used by Stream Suite
Update Center.

The application downloads it from:

```text
https://raw.githubusercontent.com/babydreamsy94/STREAM-SUITE/main/deployment/release-catalog.json
```

## Catalog structure

```json
{
  "schemaVersion": 1,
  "defaultChannel": "stable",
  "releases": []
}
```

`releases` contains complete release-manifest objects. Keeping historical entries
is what makes the Version Library and downgrades possible.

## Release fields

| Field | Purpose |
|---|---|
| `schemaVersion` | Manifest format understood by the updater |
| `suiteVersion` | Comparison version, such as `4.1.0` |
| `displayVersion` | Friendly label shown to users |
| `releaseName` | Full release title |
| `releaseDate` | ISO date in `YYYY-MM-DD` form |
| `channel` | `stable` or `beta` |
| `streamerBot` | Minimum and tested Streamer.bot versions |
| `platforms` | Per-environment verification status and note |
| `package` | Filename, GitHub Release URL, SHA-256, and byte size |
| `installation` | Guided-import mode, guide path, and backup requirement |
| `releaseNotes` | Bullets displayed in Update Center |
| `releaseNotesUrl` | Full GitHub Release page |
| `breakingChanges` | Adds a prominent setup-change warning |
| `notice` | Optional release-specific message |

## Package security fields

Every package requires:

```json
{
  "fileName": "Stream_Suite_4.1_PUBLIC_RELEASE.zip",
  "url": "https://github.com/.../releases/download/v4.1.0/...zip",
  "sha256": "64 lowercase hexadecimal characters",
  "sizeBytes": 123456
}
```

The updater rejects:

- Non-HTTPS package URLs
- Hosts not built into the updater's trust list
- Missing or malformed hashes
- Downloads larger than the configured safety limit
- Byte-size mismatches
- SHA-256 mismatches

## Platform statuses

The accepted values are:

- `verified`
- `experimental`
- `not-verified`
- `unsupported`

Do not mark an environment `verified` unless that exact package was exercised there.

## Adding a release

Use `tools/prepare_release.py` to calculate the size and hash and create a separate
catalog for review. The helper refuses to overwrite the live catalog.

Example:

```powershell
py -3.12 tools\prepare_release.py "C:\Releases\Stream_Suite_4.1_PUBLIC_RELEASE.zip" `
  --version 4.1.0 `
  --display-version "4.1" `
  --release-name "Stream Suite 4.1" `
  --tag v4.1.0 `
  --release-date 2026-09-15 `
  --guide-file "Stream_Suite_4.1/START_HERE.txt" `
  --streamer-bot-tested 1.0.7 `
  --note "Added Raid Insights." `
  --note "Improved update documentation."
```

The result is written to:

```text
deployment/release-catalog.next.json
```

Review and test that file before replacing `release-catalog.json`.

## Historical-release rule

Never replace a historical GitHub Release asset with a different file while keeping
the old version number and checksum. Create a new patch version instead.

For example, fix a broken `4.1.0` package as `4.1.1`.

