<img width="1920" height="1080" alt="STREAMSUITE00086400" src="https://github.com/user-attachments/assets/f164cd9c-622b-4bfe-8df6-92f370c38455" />

> **Built for Streamers. Powered by Community.**  
> Made by **BabyDreamsy**

Stream Suite is a custom-built analytics and community-interaction package for [Streamer.bot](https://streamer.bot/). It expands the information available to Twitch streamers by tracking both standard stream events and community activity that is often difficult to review through Twitch's own dashboard alone.

The package combines attendance tracking, chat analytics, follows, raids, bits, subscriptions, stream summaries, community commands, persistent statistics, and optional add-ons into one Streamer.bot-based system.

---

## Table of Contents

- [What Stream Suite Is](#what-stream-suite-is)
- [Why I Made Stream Suite](#why-i-made-stream-suite)
- [How It Works](#how-it-works)
- [Requirements and Compatibility](#requirements-and-compatibility)
- [Package Contents](#package-contents)
- [Commands](#commands)
- [Triggers and Events](#triggers-and-events)
- [Optional Add-Ons](#optional-add-ons)
- [Installation](#installation)
- [Attendance and Analytics Notes](#attendance-and-analytics-notes)
- [Privacy and Data Safety](#privacy-and-data-safety)
- [Feature Summaries](#feature-summaries)
- [Download](#download)
- [Project History](#project-history)

---

## What Stream Suite Is

Stream Suite is a collection of Streamer.bot actions, triggers, commands, queues, global variables, and C# scripts designed for Twitch streamers.

It records activity from individual stream sessions and turns that activity into a more complete community-focused summary.

Depending on which modules are enabled, Stream Suite can track:

- Stream attendance
- Returning and first-time attendees
- Attendance history
- Chat messages by user
- Total chat activity
- Follows
- Raids
- Bits and cheers
- Standard subscriptions
- Resubscriptions
- Gift subscriptions
- Gift bombs
- Prime and paid subscription tiers
- Stream duration
- Messages per minute
- Retention-related attendance information
- FIRST! winners and streaks
- Hugs, pats, bites, bonks, and other community interactions
- Optional Stream Deck-based reset controls
- Optional report delivery through email or email-to-SMS where supported

Stream Suite is intended to act as a streamer's primary **community-focused analytics system** while Twitch remains the official source for platform-controlled information such as revenue, payouts, ad performance, and other financial totals.

---

## Why I Made Stream Suite

I created Stream Suite because Twitch's normal analytics did not always reflect how my streams actually felt.

A stream could feel active, social, and successful because different people stopped by, chatted, lurked, followed, subscribed, cheered, gifted, or raided at different points throughout the session. Even then, the final dashboard could still make the stream appear disappointing when most of the attention was placed on average viewers, peak viewers, and other view-based measurements.

Those statistics are useful, but they do not tell the entire story of a community.

I wanted a system that could answer questions such as:

- Who attended at any point during the stream?
- Who regularly returns?
- How active was chat?
- Who followed, subscribed, cheered, gifted, or raided?
- How much community activity happened throughout the full session?
- Which community commands and interactions are people using?
- How has the community changed across multiple streams?

Stream Suite was built to preserve more of that information locally and organize it around each individual stream.

Its goal is not to create an artificial performance score. Its goal is to provide a clearer and more realistic record of what actually happened.

---

## How It Works

Stream Suite runs entirely through Streamer.bot.

Streamer.bot listens for Twitch events such as chat messages, follows, raids, cheers, subscriptions, First Words, Stream Online, Stream Offline, commands, and Channel Point redemptions.

When one of those events occurs, Stream Suite:

1. Reads the relevant event arguments supplied by Streamer.bot.
2. Normalizes usernames and filters excluded accounts.
3. Updates persistent or session-based Streamer.bot variables.
4. Stores per-user and total statistics.
5. Uses the stored information in commands, leaderboards, resets, and stream summaries.
6. Produces a final report when the stream ends.

Most data is stored locally through Streamer.bot global variables and JSON-formatted dictionaries. Stream Suite does not require an external analytics website or remote database to perform its core tracking.

### Session-Based Data

Session variables are reset when a new stream begins. These include information such as:

- Current attendance
- Current chat totals
- Current follows
- Current raids
- Current bits
- Current subscriptions
- Current stream timing
- Current summary data

### Persistent Data

Persistent variables remain available between streams. These may include:

- Attendance history
- FIRST! statistics
- Hug statistics
- Pat statistics
- Command leaderboards
- Long-term community records

---

## Requirements and Compatibility

Stream Suite is not a standalone program.

You need:

- A Windows computer
- Streamer.bot
- A Twitch broadcaster account connected to Streamer.bot
- The Stream Suite `.sb` import file
- Permission to create or modify commands, triggers, actions, and variables in your Streamer.bot setup

### Stable Compatibility

The stable-compatible Stream Suite package targets **Streamer.bot v1.0.4**.

Use the stable-compatible export unless you intentionally run a newer alpha version of Streamer.bot. Exports created in newer alpha builds may contain schema or feature changes that older stable installations cannot import correctly.

Creating a Streamer.bot backup before importing is strongly recommended.

---

## Package Contents

### Stream Start Protocols

Prepares Stream Suite for a new session by recording the local start time, resetting session analytics, initializing required variables, and clearing previous stream-only data.

### Attendance Tracking

Attendance can be recorded through Twitch First Words, `!checkin`, an Attendance Check Channel Point reward, or other configured actions.

The system uses:

- `SeenUsers` for the current session
- `AttendanceHistory` for long-term attendance dates

It supports first-time and returning attendee detection, duplicate prevention, excluded-user filtering, case-insensitive usernames, and persistent attendance history.

### Chat Message Analytics

Tracks total chat messages and messages sent by each user.

Common variables include:

- `analytics.chatMessagesByUser`
- `analytics.chatTotalMessages`

### Follow Tracking

Records new follows received during the current stream for inclusion in the final report.

### Raid Tracking

Records unique channels that raid the stream during the current session.

### Bits and Cheer Tracking

Tracks total bits received and per-user contributions.

Common variables include:

- `analytics.bits`
- `analytics.totalBits`

### Subscription Tracking

Supports:

- Standard subscriptions
- Resubscriptions
- Individual gift subscriptions
- Gift bombs
- Prime subscriptions
- Tier 1, Tier 2, and Tier 3 subscriptions

The system can record subscription units, type, tier, gifted status, recipients, resubscription months, per-user details, and total subscription activity.

Common variables include:

- `analytics.subs`
- `analytics.subsDetailed`
- `analytics.totalSubs`

### Stream End Protocols

Builds a final report containing applicable session data, including:

- Stream duration
- Attendance
- First-time and returning attendees
- Chat totals
- Top chatter
- Messages per minute
- Follows
- Raids
- Bits
- Subscriptions
- Retention-related information
- Optional email or email-to-SMS delivery

Common variables include:

- `analytics.finalSummaryJson`
- `analytics.attendanceSummaryJson`

### FIRST! System

Includes daily winner locking, personal statistics, streaks, milestones, and broadcaster-controlled resets.

Common variables include:

- `FirstStats`
- `FirstDates`

### Hugs System

Tracks hugs given and received, personal statistics, leaderboards, reset actions, and optional Stream Deck reset support.

### Pats System

Tracks pats given and received, personal statistics, leaderboards, reset actions, and optional Stream Deck reset support.

### BITE and BONK! Commands

Attendance-aware community commands with random responses, self-target phrases, target normalization, and customization options.

### Reset Actions

Reset actions may include:

- Reset Attendance
- Reset Variables
- Reset FIRST!
- Reset Hugs
- Reset Pats

All reset actions can support broadcaster-only commands and optional Stream Deck buttons.

### Feature Summaries

Each major script includes a Feature Summary covering its purpose, system flow, variables, customization options, and troubleshooting information.

---

## Commands

The exact enabled commands may vary by package version and user configuration.

### Attendance

| Command | Purpose |
|---|---|
| `!checkin` | Manually records the user as attending the current stream |

### Community Commands

| Command | Purpose |
|---|---|
| `!bite` | Uses the BITE interaction system |
| `!bonk` | Uses the BONK! interaction system |
| `!hug` | Sends and records a hug |
| `!hstats` | Shows hug statistics |
| `!hboard` | Shows the hug leaderboard |
| `!pat` | Sends and records a pat |
| `!pstats` | Shows pat statistics |
| `!pboard` | Shows the pat leaderboard |
| `!fstats` | Shows FIRST! statistics |

### Reset Commands

| Command | Purpose |
|---|---|
| `!resetfirst` | Resets FIRST! data |
| `!resethugs` | Resets hug data |
| `!resetpats` | Resets pat data |
| `!resetattendance` | Resets attendance when Reset Tools is installed |
| `!resetvar` | Resets configured variables when Reset Tools is installed |

Administrative commands should be limited to the broadcaster or trusted moderators.

---

## Triggers and Events

Stream Suite may use:

- Twitch First Words
- Twitch Chat Message
- Twitch Follow
- Twitch Raid
- Twitch Cheer
- Twitch Subscription
- Twitch Resubscription
- Twitch Gift Subscription
- Twitch Gift Bomb
- Twitch Stream Online
- Twitch Stream Offline
- Twitch Commands
- Twitch Channel Point Rewards
- Stream Deck button events

Channel Point reward IDs and Stream Deck button IDs are unique to each user and may need to be replaced after import.

---

## Optional Add-Ons

### Diaper Check

**Diaper Check is a separate and 100% optional module.**

It is an ABDL-themed community command designed for playful teasing between consenting adult community members.

It is not required for Stream Suite's analytics, attendance, reporting, or other community features. Users who do not want it can simply avoid importing it or delete it from Streamer.bot.

### Reset Tools

Reset Tools is a separate optional utility package containing:

- Reset Attendance
- Reset Variables
- `!resetattendance`
- `!resetvar`
- Stream Deck support
- Configurable Stream Deck button IDs

It is useful for maintenance and testing but is not required for normal operation.

---

## Installation

### 1. Install Streamer.bot

Download and open Streamer.bot, then connect your broadcaster account under:

```text
Platforms > Twitch > Accounts
```

### 2. Back Up Your Existing Setup

Create or confirm a recent Streamer.bot backup before importing Stream Suite.

### 3. Download Stream Suite

Visit:

https://ko-fi.com/s/58ec72e935

Ko-fi may describe the process as a purchase, but **Stream Suite is 100% free**.

Complete the free checkout process and download the package.

### 4. Import Stream Suite

1. Open Streamer.bot.
2. Click **Import**.
3. Drag the main Stream Suite `.sb` file into the Import String area.
4. Review the listed actions, commands, triggers, and queues.
5. Confirm that the import will not overwrite anything important.
6. Complete the import.

Import Diaper Check or Reset Tools separately only when wanted.

### 5. Review Commands and Triggers

After import:

- Enable only the commands you plan to use.
- Restrict administrative commands.
- Confirm Twitch event triggers.
- Reconnect Channel Point rewards where necessary.
- Replace Stream Deck button IDs where necessary.

### 6. Replace Template Values

Search for placeholders such as:

- `streamername`
- `botname`
- Excluded-user names
- Email addresses
- App passwords
- Email-to-SMS addresses
- Local file paths
- Stream Deck button IDs

### 7. Read the Feature Summaries

Check the Feature Summary inside the relevant action whenever you need more information or assistance.

---

## Attendance and Analytics Notes

### Attendance Is Not Viewer Count

Stream Suite cannot identify every silent viewer.

A user is recorded as attending only after Streamer.bot receives a supported observable event, such as First Words, `!checkin`, a reward redemption, or another configured interaction.

Once recorded, the user can remain part of that stream's attendance history even after leaving.

Stream Suite attendance answers:

> Who was observably present at any point during the stream?

It does not duplicate Twitch's concurrent-viewer count.

### Twitch Analytics Still Matters

Twitch remains the official source for:

- Revenue
- Payouts
- Ads
- Official viewer statistics
- Official follower and subscription information
- Traffic sources
- Platform-controlled financial records

Stream Suite complements that data with custom local tracking and community context.

---

## Privacy and Data Safety

Before sharing or publishing a customized export, remove:

- Personal email addresses
- App passwords
- Phone numbers
- Email-to-SMS addresses
- Local file paths
- Private usernames
- Stream Deck identifiers
- Other credentials or personal information

Never upload a customized `.sb` export publicly without reviewing its scripts and arguments.

---

## Feature Summaries

Every major Stream Suite script includes a built-in Feature Summary explaining:

- What the script does
- Which action triggers it
- Which variables it reads or writes
- Which placeholders should be changed
- What can be customized
- Common troubleshooting steps

For detailed help, open the relevant Streamer.bot action and read the Feature Summary above the C# code.

---

## Download

**Stream Suite is 100% free.**

Download it from Ko-fi:

https://ko-fi.com/s/58ec72e935

---

## Project History

### Version 1.0 — January 2nd, 2026

The first public release focused on attendance and community interaction commands.

### Version 2.0 — May 5th, 2026

Version 2.0 expanded Stream Suite into a full per-stream analytics system and introduced Diaper Check as an optional add-on.

### Version 3.0 — June 23rd, 2026

Version 3.0 introduced automatic First Words attendance, a major subscription-tracking rewrite, safer public templates, improved Feature Summaries, stronger reset tools, and Stream Deck support across reset actions.

See the repository changelog for the complete release history.

---

## Project Philosophy

A stream is more than an average viewer number.

It is the people who arrived, returned, chatted, lurked, supported, interacted, joked around, and helped create the community.

**Stream Suite: Built for Streamers. Powered by Community.**

