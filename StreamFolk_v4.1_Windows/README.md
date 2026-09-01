# StreamFolk v4.1

**Built by Streamers. Powered by Community.**

StreamFolk is a local-first Streamer.bot toolkit for stream attendance, community activity, support events, retention, stream-by-stream summaries, and weekly/monthly/yearly reporting. Twitch remains the authoritative source for platform-controlled viewer, revenue, advertising, payout, and account analytics.

## What's new in v4.1

- Full product rebrand to **StreamFolk**.
- Separate **Windows** and **Linux/Wine** release packages.
- Stream End Protocols now writes the active Twitch category to both `analytics.finalSummaryJson` and the saved Stream Performance Report.
- The GUI-facing report header now includes `🎮 Category: <category>` directly after Duration.
- The completed-stream archive prefers the category captured in the final summary and retains compatibility with `analytics.currentCategory`.
- Linux/Wine import uses Wine-stable absolute .NET Framework reference paths rather than release-machine relative Windows paths.
- StreamFolk GUI files are rebranded and packaged for the target platform workflow.

## Main package

The main Streamer.bot import contains the existing v4 feature set: attendance, chat activity, follows, raids, Bits, subscriptions, stream category tracking, Stream Start/End lifecycle, weekly/monthly/yearly reporting, FIRST!, Hugs, Pats, BITE!, BONK!, resets, and optional private messaging.

## Installation

1. Read `START_HERE.txt` and the platform notes in this folder.
2. Back up your current Streamer.bot setup and long-term StreamFolk globals before importing.
3. Import the `.sb` file in `Main Package`.
4. Review disabled commands/actions and all placeholders before enabling anything.
5. Compile every Execute C# Code sub-action.
6. Test Stream Start, category tracking, normal event trackers, Stream End, saved reports, and the GUI before a production stream.

## Report format change

The v4.1 Stream Performance Report begins with:

```text
📅 Event Date: Aug 31, 2026
🕒 Report Generated: 10:17 PM
⏱️ Duration: 4h 7m
🎮 Category: Pokémon Unbound
```

The Category field also exists in `analytics.finalSummaryJson` as `Category`.

## Data preservation

Before replacing an existing installation, preserve any long-term globals you care about, especially attendance history, stream history, FIRST!, Hugs, and Pats data. The package is designed to be reviewed/imported rather than silently overwriting a customized setup.

## Compatibility

- Windows package: Streamer.bot v1.0.7 on Windows.
- Linux package: Streamer.bot v1.0.7 under Wine with .NET Framework 4.8. Linux support in Streamer.bot itself is experimental.

## Disclaimer

StreamFolk is an independent community project and is not an official Twitch or Streamer.bot product.
