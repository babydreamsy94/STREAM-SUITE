# Publishing Stream Suite Updates

This guide separates the one-time updater setup from the process used for every
future Stream Suite release.

## Part 1: One-time setup

### Step 1 — Create the Version 4.0 GitHub Release

1. Open the `babydreamsy94/STREAM-SUITE` repository on GitHub.
2. Open **Releases**.
3. Select **Draft a new release**.
4. Create the tag `v4.0.0`.
5. Set the title to `Stream Suite 4.0 FINAL`.
6. Attach the exact file named:

   ```text
   Stream_Suite_4.0_FINAL_PUBLIC_RELEASE.zip
   ```

7. Publish the release.

The included production catalog already contains that file's verified metadata:

```text
Size:    231508 bytes
SHA-256: 44df08f77fb54db232841cc4ecc8c7ef48666f9a51bddb8013384ebf6f06bc6a
```

If the uploaded ZIP is rebuilt or changed, recalculate both values before publishing
the catalog.

### Step 2 — Add this project to the repository

Merge the contents of this project into the repository root. The existing
`STREAMSUITE` folder can remain where it is.

Important paths after the merge:

```text
deployment/release-catalog.json
src/stream_suite_updater.py
.github/workflows/build-updater.yml
```

Commit and push those files to the `main` branch.

### Step 3 — Confirm the public catalog

Open this URL in a browser:

```text
https://raw.githubusercontent.com/babydreamsy94/STREAM-SUITE/main/deployment/release-catalog.json
```

It should display the catalog JSON rather than a 404 page.

### Step 4 — Build the Windows application

1. Open the GitHub repository's **Actions** tab.
2. Select **Build Stream Suite Update Center**.
3. Select **Run workflow**.
4. Wait for the Windows build to complete.
5. Download the `Stream-Suite-Update-Center-Windows` artifact.
6. Extract it and test `StreamSuiteUpdateCenter.exe`.

The build runs all automated tests before creating the application.

### Step 5 — Test before Ko-fi distribution

On a separate or backed-up Windows setup:

1. Open Update Center.
2. Set Installed Version to `4.0.0`.
3. Confirm it reports that Version 4.0 is current.
4. Download Version 4.0 again.
5. Confirm that verification reaches 100%.
6. Open the extracted setup guide.
7. Confirm that both `.sb` package files are present.
8. Do not perform the import unless the test Streamer.bot installation is backed up.

### Step 6 — Change Ko-fi once

Replace the Ko-fi download with a ZIP containing:

- `StreamSuiteUpdateCenter.exe`
- A short `START_HERE.txt`
- An optional direct GitHub fallback link

After that, ordinary Stream Suite package updates go through GitHub Releases and the
catalog. Ko-fi no longer needs a new package for every Stream Suite version.

## Part 2: Every future Stream Suite release

### Step 1 — Finish and test the package

Create the final public ZIP and test its `.sb` imports on the exact Streamer.bot and
platform versions you intend to mark as verified.

Never calculate release metadata from a draft ZIP that will be changed afterward.

### Step 2 — Prepare the next catalog safely

Run the included creator helper. Example:

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

The helper:

- Checks that the ZIP contains the requested guide
- Checks that at least one `.sb` file exists
- Calculates the exact byte size
- Calculates SHA-256
- Builds the GitHub Release URL
- Adds the release without removing previous versions
- Validates the complete catalog
- Writes `release-catalog.next.json` instead of changing the live file

### Step 3 — Review the prepared catalog

Open `deployment/release-catalog.next.json` and verify:

- Version
- Display name
- Release date
- Streamer.bot compatibility
- Platform statuses
- Release notes
- Download URL
- Breaking-change setting
- Guide path

Run diagnostics against it:

```powershell
py -3.12 src\stream_suite_updater.py --diagnostics `
  --manifest deployment\release-catalog.next.json
```

### Step 4 — Publish the GitHub Release first

1. Create a draft GitHub Release using the same tag passed to the helper.
2. Attach the exact ZIP used by the helper.
3. Add the release notes.
4. Publish the GitHub Release.
5. Confirm that its asset downloads successfully.

Do this before changing the live catalog. Otherwise, users could see an update whose
package does not exist yet.

### Step 5 — Publish the catalog last

1. Replace `deployment/release-catalog.json` with the reviewed
   `release-catalog.next.json` content.
2. Remove the `.next` review file.
3. Commit and push the live catalog.
4. Open Update Center and select **Check Again**.

The new version should now appear automatically. Every earlier catalog entry remains
available in Version Library.

## Correcting a release

Do not silently replace a published ZIP while keeping its old version number.

If Version `4.1.0` needs a correction:

1. Fix the package.
2. Release it as `4.1.1`.
3. Add a catalog notice explaining the correction.
4. If necessary, temporarily remove the broken entry from the catalog so users
   cannot select it.

## Downgrade responsibility

The Version Library preserves access to old packages. It does not promise that every
newer data format can be read by every older release.

Each release should document known downgrade limitations. For serious failures,
direct users to restore the Streamer.bot backup created before the failed upgrade.

