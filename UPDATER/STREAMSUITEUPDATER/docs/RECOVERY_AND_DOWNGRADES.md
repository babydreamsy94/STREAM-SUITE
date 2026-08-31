# Stream Suite Recovery and Downgrades

The Version Library can download an older Stream Suite package, but a package
downgrade and a complete restoration are not the same operation.

## The safest recovery rule

If an update has just caused a serious problem and a Streamer.bot backup exists from
before that update, prefer restoring that backup.

Streamer.bot backups include actions, commands, stored user data, and other
configuration. The official recovery process requires Streamer.bot to be closed
before its data is restored. Follow the current
[Streamer.bot Backup & Restore documentation](https://docs.streamer.bot/guide/core/backup).

## Why importing an older package may not be enough

An older Stream Suite release can replace older versions of its actions, but it may
not reverse changes already made to:

- Persistent global variables
- Attendance history
- Completed-stream history
- Report formats
- User statistics
- Trigger connections
- Renamed or removed actions
- Streamer.bot's own schema

An older action may encounter a value created by a newer action that did not exist
when the older release was written.

## Scenario A: You downloaded an update but did not import it

Nothing needs to be downgraded. Update Center never modifies Streamer.bot during a
download.

You can delete the downloaded folder or leave it in place for later.

## Scenario B: You imported an update but have not streamed with it

1. Close Streamer.bot.
2. Locate the most recent backup created before the import.
3. Restore that backup using Streamer.bot's official instructions.
4. Restart Streamer.bot.
5. Test Stream Start, attendance, chat, reports, and Stream End before going live.

This normally provides the cleanest rollback because it restores both actions and
their prior configuration state.

## Scenario C: You imported an update and generated new stream data

Do not immediately overwrite everything with an older package.

1. Close Streamer.bot and make a copy of the current backup/data state.
2. Preserve any new reports you need.
3. Identify whether the failure affects code, configuration, or stored data.
4. Review the release's downgrade notes.
5. Decide whether to restore the older backup and accept losing the newer session
   data, or repair the affected action in place.

This situation may require a version-specific migration instead of a normal
downgrade.

## Scenario D: You intentionally want an older package

1. Open **Version Library** in Stream Suite Update Center.
2. Select the older release.
3. Read its compatibility and release notes.
4. Select **Download Downgrade**.
5. Confirm the downgrade warning.
6. Back up and export the Stream Suite actions currently installed.
7. Open the older package's setup guide.
8. Review every overwrite choice inside Streamer.bot's Import dialog.
9. Reconnect creator-specific triggers if necessary.
10. Test the downgraded actions before the next live stream.

## What Update Center guarantees

- The selected archived package came from the release catalog.
- The download uses HTTPS from an updater-approved GitHub host.
- The ZIP matches its expected SHA-256 value.
- Unsafe archive paths and symbolic links are rejected.
- The package is extracted to a new folder.
- Streamer.bot is not edited automatically.

## What Update Center cannot guarantee

- That old and new persistent variables are compatible
- That an older release supports the installed Streamer.bot version
- That personal action edits survive an overwrite
- That a manual import completed successfully
- That restoring an old backup preserves data recorded after that backup

