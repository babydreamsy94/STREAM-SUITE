<img width="1920" height="1080" alt="StreamFolk" src="https://github.com/user-attachments/assets/f164cd9c-622b-4bfe-8df6-92f370c38455" />

# STREAMFOLK [formerly known as Stream Suite)

> **Built By Streamers. Powered by Community.**
>
> Created by **[babydreamsy](https://www.twitch.tv/babydreamsy)**

[![Version](https://img.shields.io/badge/version-4.0%20FINAL-856ed6)](./CHANGELOG.txt)
[![Streamer.bot](https://img.shields.io/badge/Streamer.bot-v1.0.7+-01d7fb)](https://streamer.bot/downloads)
[![Platform](https://img.shields.io/badge/platform-Twitch-9146FF)](https://www.twitch.tv/)
[![Download](https://img.shields.io/badge/download-100%25%20FREE-2ea44f)](https://ko-fi.com/s/58ec72e935)

> Introducing **STREAMFOLK**! Formerly known as "StreamFolk", STREAMFOLK is a comprehensive analytics & fun commands system designed to upgrade your streams while also providing a much clearer & more **REALISTIC** insight into your stream's performance!

STREAMFOLK is a community-focused collection of Streamer.bot actions, commands, triggers, queues, and C# scripts for Twitch streamers.

It records observable participation throughout each broadcast, preserves selected community history between streams, creates individual stream summaries, and generates weekly, monthly, and yearly reports. It also includes fun commands and community statistics designed to help a channel develop its own history and personality.

It does **NOT** replace Twitch's official financial, payout, advertisement, or tax information. Instead, it provides an alternative, locally managed view of the people and activity that helped make each stream what it was.

## Quick Links

- **[Download StreamFolk for free](https://ko-fi.com/s/58ec72e935)**
- **[Read the Quick Start Guide](./START_HERE.txt)**
- **[Read the complete Changelog](./CHANGELOG.txt)**
- **[Browse the Feature Summaries](./Main%20Package/Feature%20Summaries/)**
- **[Browse the readable C# source](./Main%20Package/Source%20Code/)**
- **[Download Streamer.bot](https://streamer.bot/downloads)**

## Table of Contents

- [Why StreamFolk Exists](#why-streamfolk-exists)
- [What Questions It Answers](#what-questions-it-answers)
- [Core Features](#core-features)
- [How It Compares](#how-it-compares)
- [Requirements and Compatibility](#requirements-and-compatibility)
- [Package Contents](#package-contents)
- [Main Package Overview](#main-package-overview)
- [Commands](#commands)
- [Twitch Event Triggers](#twitch-event-triggers)
- [Optional Diaper Check Package](#optional-diaper-check-package)
- [Installation](#installation)
- [Required Configuration](#required-configuration)
- [Attendance and Retention](#attendance-and-retention)
- [Data Storage and Privacy](#data-storage-and-privacy)
- [Troubleshooting](#troubleshooting)
- [Upgrading from an Earlier Version](#upgrading-from-an-earlier-version)
- [Frequently Asked Questions](#frequently-asked-questions)
- [Download and Support](#download-and-support)
- [Project History](#project-history)
- [Disclaimer](#disclaimer)

---

## Why StreamFolk Exists

StreamFolk began as a small attendance-tracking system.

It was created after I realized that Twitch's standard viewer metrics did not always reflect how my streams actually felt. A broadcast could be active, social, and memorable because different people stopped by, chatted, followed, subscribed, cheered, gifted, or raided at different times. The final dashboard could still make that same stream appear disappointing when most of the attention was placed on average and peak viewers.

Those numbers can be useful, but they do not tell the entire story of a community.

StreamFolk was built to preserve more of that story. Its purpose is not to manufacture a performance score or declare whether a stream was "good." Its purpose is to help the streamer understand who observably participated, how they interacted, and whether people returned over time.

---

## What Questions It Answers

Depending on the enabled modules, StreamFolk can help answer questions such as:

### Attendance

- Who observably attended this stream?
- How many attendees were new?
- How many attendees had been recorded previously?
- Which members of the community keep returning?
- Which stream or reporting period had the greatest attendance?

### Retention

- How many people from one stream returned during the next stream?
- How many people from one reporting period returned during the next period?
- Is attendee-overlap retention improving over time?
- Are new attendees becoming returning community members?

### Engagement

- How many chat messages were sent?
- Who was the most active chatter?
- How many messages were sent per minute?
- Who followed, raided, cheered, subscribed, resubscribed, or gifted subscriptions?
- Which community commands are people using?

### Long-Term Activity

- What happened during each completed stream?
- How did this week, month, or year compare with the previous period?
- Which categories were streamed?
- How have attendance, chat, support events, and community interactions changed over time?

The simplest distinction is:

> **Twitch tells you how many people were watching. StreamFolk helps record who observably participated, how they participated, and whether they returned.**

---

## Core Features

- Event-based stream attendance
- New and returning attendee classification
- Long-term attendance history
- True attendee-overlap retention
- Per-user and total chat-message tracking
- Messages-per-minute calculations
- Follow tracking
- Raid tracking
- Bits and cheer tracking
- Standard subscription tracking
- Prime and paid-tier identification
- Resubscription month tracking
- Individual gift and gift-bomb tracking
- Stream-category history
- Local stream duration and session summaries
- Weekly, monthly, and yearly reports
- FIRST! winner, total, streak, milestone, and statistics system
- Hugs and Pats trackers, statistics, and leaderboards
- BITE! and BONK! community commands
- Broadcaster-controlled reset utilities
- Optional email-to-SMS notifications
- Readable C# source files for every module
- Matching Feature Summaries with configuration and troubleshooting guidance

---

## How It Compares

| Tool | Best used for |
|---|---|
| **Twitch Analytics** | Official viewer statistics, traffic sources, followers, subscriptions, advertisements, revenue, payouts, and other platform-controlled information |
| **Public tracking sites** | Public stream history, viewership trends, rankings, games, categories, follower growth, and comparisons between channels |
| **StreamFolk** | Locally recorded attendance, returning attendees, community activity, Twitch events, fun commands, attendee-overlap retention, and customizable stream-by-stream reporting |

StreamFolk complements Twitch and public tracking sites. It is not an official replacement for either one.

---

## Requirements and Compatibility

StreamFolk is **not** a standalone application.

You need:

- A Windows computer capable of running Streamer.bot
- The latest STABLE version of Streamer.bot
- A Twitch broadcaster account connected to Streamer.bot
- An active internet connection for Twitch events and account features
- A safe backup of your current Streamer.bot setup

### Supported Environment

The verified target for StreamFolk 4.0 is Streamer.bot v1.0.7 on Windows.

Streamer.bot setups running through Linux compatibility layers, macOS workarounds, alpha builds, or later schema versions may behave differently and are not guaranteed by this release.

### Import Format

The main package uses the complete Streamer.bot v1.0.7 export envelope and schema 24.

---

## Package Contents

```text
Stream_Suite_4.0_FINAL/
├── README.md
├── START_HERE.txt
├── CHANGELOG.txt
├── Main Package/
│   ├── Stream_Suite_4.0_Main_Package.sb
│   ├── Feature Summaries/
│   └── Source Code/
└── Optional Packages/
    └── Diaper Check/
        ├── Stream_Suite_4.0_Optional_Diaper_Check.sb
        ├── Feature Summaries/
        └── Source Code/
```

### Main Package

- **28 Streamer.bot actions**
- **16 commands**, disabled by default
- **1 blocking action queue**
- **29 readable C# source files**
- **29 matching Feature Summaries**

Stream End Protocols contains two C# modules: the main end-of-stream workflow and the completed-stream archive used by long-term reports.

### Optional Package

- **1 Diaper Check action**, disabled by default
- **1 `!check` command**, disabled by default
- **1 readable C# source file**
- **1 matching Feature Summary**

The Main Package works without the optional package.

---

## Main Package Overview

| Area | Included actions | Purpose |
|---|---|---|
| Attendance | Attendance Check | Records observable attendance through First Words or a connected Attendance Check reward |
| Live analytics | Bits Tracker, Chat Message Tracker, Follow Tracker, Raid Tracker, Sub Tracker, Track Stream Category | Records stream events and per-session activity |
| Session and reports | Stream Start Protocols, Stream End Protocols, Generate Weekly Report, Generate Monthly Report, Generate Yearly Report | Initializes sessions, builds summaries, archives completed streams, and produces long-term reports |
| FIRST! | FIRST! Tracker, FIRST! Stats | Records the first eligible participant, totals, streaks, and milestones |
| Hugs | Hugs Tracker, Hugs Stats, Hugs Leaderboard | Records community hugs and displays personal or leaderboard statistics |
| Pats | Pats Tracker, Pats Stats, Pats Leaderboard | Records community pats and displays personal or leaderboard statistics |
| Fun commands | BITE!, BONK! | Provides attendance-aware randomized community interactions |
| Maintenance | Reset Attendance, Reset FIRST!, Reset Hugs, Reset Pats, Reset Variables | Clears selected session or long-term data under broadcaster control |
| Optional messaging | Send Streamer a Text | Sends a configured email-to-SMS notification and can play a confirmation sound |

Detailed behavior, variables, permissions, placeholders, safety notes, and troubleshooting information are documented in each matching Feature Summary.

---

## Commands

All Main Package commands are **disabled by default**. Review their permissions, cooldowns, messages, and intended users before enabling them.

### Community Commands

| Command | Action |
|---|---|
| `!bite` | BITE! |
| `!bonk` | BONK! |
| `!hug` | Hugs Tracker |
| `!hstats` | Hugs Stats |
| `!hboard` | Hugs Leaderboard |
| `!pat` | Pats Tracker |
| `!pstats` | Pats Stats |
| `!pboard` | Pats Leaderboard |
| `!fstats` | FIRST! Stats |

### Report Commands

| Command | Action |
|---|---|
| `!wreport` | Generate Weekly Report |
| `!mreport` | Generate Monthly Report |
| `!yreport` | Generate Yearly Report |

### Administrative Reset Commands

| Command | Action |
|---|---|
| `!resetattendance` | Reset Attendance |
| `!resetfirst` | Reset FIRST! |
| `!resethugs` | Reset Hugs |
| `!resetpats` | Reset Pats |

Administrative reset commands should be limited to the broadcaster or explicitly trusted moderators.

Reset Variables does not include a public command. Run it manually or connect it to a private control after reading its Feature Summary.

---

## Twitch Event Triggers

The Main Package includes connections for:

- **First Words** → automatic attendance
- **Chat Message** → chat-message tracking
- **Follow** → follow tracking
- **Raid** → raid tracking
- **Cheer** → Bits tracking
- **Subscription** → subscription tracking
- **Resubscription** → subscription tracking
- **Gift Subscription** → subscription tracking
- **Gift Bomb** → subscription tracking
- **Stream Online** → Stream Start Protocols and category tracking
- **Stream Offline** → Stream End Protocols
- **Channel Updated** → category tracking

Channel Point Reward IDs are not included because every Twitch channel has its own reward IDs.

---

## Optional Diaper Check Package

**Diaper Check is completely optional and separate from the Main Package.**

It is an ABDL-themed community command intended only for consenting adult communities where the interaction fits the channel.

- It is not required for attendance, analytics, retention, reporting, or any other Main Package feature.
- It depends on the Main Package's `SeenUsers` attendance variable.
- Its action and `!check` command are disabled after import.
- It should not be imported or enabled unless it is appropriate for the destination channel.

Read its Feature Summary before changing or enabling it.

---

## Installation

### 1. Back Up Streamer.bot

Before importing anything, create or confirm a recent backup of your existing Streamer.bot installation.

### 2. Download StreamFolk

Download the complete public package from Ko-fi:

**https://ko-fi.com/s/58ec72e935**

Ko-fi may display the download as a purchase, but StreamFolk is **100% free**.

### 3. Extract the Package

Extract the ZIP file into a normal local folder, such as your Documents folder.

Do not run Streamer.bot or store its working installation inside OneDrive, Dropbox, Google Drive, or another cloud-synchronized folder.

### 4. Import the Main Package

1. Open Streamer.bot v1.0.7.
2. Click **Import** on the toolbar.
3. Open the `Main Package` folder.
4. Drag the included .sb files into the **Import String** area.
5. Confirm that the preview shows:
   - Title: **StreamFolk**
   - Author: **babydreamsy**
   - 28 actions
   - 16 commands
   - 1 action queue
6. Review the import preview.
7. Click **Import**.

For more information about Streamer.bot imports, read the official [Import and Export Guide](https://docs.streamer.bot/guide/core/import-export).

### 5. Read the Feature Summaries

Before enabling an action or command, open its matching Feature Summary and review:

- Required placeholders
- Twitch event connections
- Permissions
- Cooldowns
- Global variables
- File paths
- Privacy and security warnings
- Testing instructions

### 6. Configure Your Channel

Replace the public placeholders, create any required Channel Point Rewards, reconnect channel-specific reward IDs, review every imported command, and configure report paths or optional messaging only when needed.

### 7. Test Before Going Live

Use a controlled test stream, test Twitch account, or duplicated Streamer.bot setup.

Test:

- Attendance through First Words
- Attendance Check reward handling
- Chat-message tracking
- Community commands
- Follow, raid, Bits, and subscription events
- Stream Start Protocols
- Stream End Protocols
- Saved reports
- Weekly, monthly, and yearly reports
- Any private messaging or Stream Deck connections you add

Confirm that every **Execute C# Code** sub-action compiles successfully before relying on the package during a production stream.

---

## Required Configuration

### Broadcaster and Bot Names

Replace `streamername` with your lowercase Twitch login wherever a Feature Summary instructs you to do so.

Replace `botname` with the login of your separate bot account, or remove that placeholder if you do not use one.

Review each `ExcludedUsers` collection. These lists prevent the broadcaster, bot accounts, or services from being counted in analytics where appropriate. Being listed in `ExcludedUsers` does **not** prevent Stream Start or Stream End actions from being triggered.

### Channel Point Rewards

Create and connect your own rewards when desired:

- **Attendance Check** → `StreamFolk - Attendance Check`
- **FIRST!** → `StreamFolk - FIRST! Tracker`
- **Send Streamer a Text** → `StreamFolk - Send Streamer a Text`

Attendance Check already includes a First Words trigger, so ordinary first chat participation can record attendance without the optional reward.

### Actions Disabled for Safety

The following actions remain disabled until their private settings are reviewed:

- Stream Start Protocols
- Stream End Protocols
- Send Streamer a Text

The optional Diaper Check action also remains disabled.

### Email-to-SMS and Report Paths

Send Streamer a Text and Stream End Protocols may contain placeholders for sender accounts, Google App Passwords, email-to-SMS gateways, report folders, and other private settings.

Never enable these features until their Feature Summaries have been read completely.

---

## Attendance and Retention

### Attendance Is Not Viewer Count

StreamFolk cannot identify every silent viewer.

An account is recorded as attending only after Streamer.bot receives a supported observable event, such as:

- A First Words message
- An Attendance Check reward redemption
- Another interaction that you intentionally connect to the attendance action

After an account is recorded, it remains part of that session's attendance record even if the person leaves later.

Therefore, StreamFolk attendance answers:

> **Who was observably present at some point during the stream?**

It does not represent the number of people watching simultaneously.

### True Retention

StreamFolk 4.0 calculates retention through attendee overlap.

Instead of comparing only attendance totals, it compares the actual eligible attendees from one stream or reporting period with the people who returned during the next one.

That helps distinguish between:

- A community whose total size stayed similar but whose individual members changed
- A community whose attendees genuinely returned

Retention remains dependent on observable attendance. A person who silently watches without triggering attendance cannot be included in the calculation.

---

## Data Storage and Privacy

StreamFolk stores its core information locally through Streamer.bot global variables, JSON-formatted dictionaries, and generated report files.

Depending on the modules enabled, stored information may include:

- Twitch usernames
- Attendance dates
- Per-user chat totals
- Follow and raid records
- Bits and subscription activity
- Community-command statistics
- Stream categories and durations
- Weekly, monthly, and yearly reports

### Important Safety Warning

> **Never publicly upload or share a configured `.sb` export without inspecting it first.**

A customized export may contain:

- Personal email addresses
- Google App Passwords
- Phone or carrier gateway addresses
- Webhooks
- Local file paths
- Channel Point Reward IDs
- Stream Deck identifiers
- Private usernames
- Community attendance or activity histories

If an App Password is exposed, revoke it immediately and create a new one.

Store generated reports carefully. Attendance histories and per-user activity can reveal community participation patterns even when they do not contain message contents.

---

## Troubleshooting

### The `.sb` File Will Not Import

- Confirm that you are using **Streamer.bot v1.0.7**.
- Confirm that you selected tje "Main Package".sb file
- Do not paste or edit the encoded `.sb` file manually.
- Re-download the package if the file may have been damaged or altered.
- Back up Streamer.bot before retrying the import.

### An Action Appears in Action History but Has No Visible Effect

If the action appears in Action History, its trigger fired. Check:

- Whether the action completed or produced a runtime error
- Whether the required Twitch account is connected
- Whether the action or parent action group is enabled
- Whether every Execute C# Code sub-action compiled successfully
- Whether the public placeholders were replaced correctly
- Whether a required Channel Point Reward was connected to the destination channel
- Whether a report folder, sound file, email account, or other private dependency exists
- The matching Feature Summary and Streamer.bot log

An account appearing in an `ExcludedUsers` list affects whether that account is counted by selected trackers. It does not normally stop the Stream Online or Stream Offline event from firing.

### Stream Start or Stream End Does Not Run

Check:

1. The action itself is enabled.
2. The Stream Online or Stream Offline trigger is attached to the correct action.
3. The connected Twitch account is the broadcaster account that went live or offline.
4. Streamer.bot shows the expected event in Action History.
5. Every code sub-action compiles under v1.0.7.
6. The Streamer.bot log does not show a runtime exception.

If the action appears as **Completed** and creates its variables, then the protocol fired. Investigate the specific sub-action that produced no result, such as chat, pinning, file saving, SMTP, or email-to-SMS delivery.

### Email-to-SMS Does Not Arrive

- Confirm that the sender address and App Password are private and correct.
- Confirm that the carrier still supports email-to-SMS for the destination number.
- Check spam filtering, carrier delays, and gateway changes.
- Test the sender account separately before relying on it live.
- Remember that successful action completion does not guarantee carrier delivery.

Email-to-SMS support can be delayed, filtered, changed, or discontinued by a carrier.

### Reports Are Empty or Incomplete

- Confirm that Stream Start Protocols ran before the tracked session.
- Confirm that the relevant tracker actions were enabled during the stream.
- Confirm that Stream End Protocols completed and archived the stream.
- Check `analytics.streamHistory` and any configured report path.
- Preserve older `analytics.monthlyHistory` data during migration.
- Read the matching report Feature Summary before manually editing variables.

---

## Upgrading from an Earlier Version

1. Back up Streamer.bot.
2. Export your existing StreamFolk actions as an additional recovery copy.
3. Preserve long-term variables unless you intentionally want a complete reset.
4. Import Version 4.0.
5. Reconnect channel-specific rewards.
6. Review commands, permissions, cooldowns, action states, and placeholders.
7. Test the complete session workflow before your next production stream.

Long-term variables that may need to be preserved include:

- `AttendanceHistory`
- `analytics.streamHistory`
- `analytics.monthlyHistory`
- `FirstStats`
- `FirstDates`
- `HugStats`
- `PatCounts`
- `PatGiven`

Do not overwrite or delete those values unless a full reset is intended.

---

## Frequently Asked Questions

### Is StreamFolk free?

Yes. StreamFolk 4.0 FINAL is a **100% free download** through Ko-fi.

### Does StreamFolk replace Twitch Analytics?

No. Twitch remains the official source for platform viewer statistics, traffic, advertisements, revenue, payouts, taxes, and account-controlled information.

### Does StreamFolk count every lurker?

No. It records event-based attendance. A completely silent viewer who never activates a supported event may not be recorded.

### Is Attendance Check required?

No. First Words can automatically record a chatter's attendance. The Channel Point Reward provides another way to check in.

### Is a Stream Deck required?

No. StreamFolk works through Streamer.bot. You may connect private Stream Deck controls to selected actions if you choose.

### Do I have to install Diaper Check?

No. It is a separate optional package and has no effect on the Main Package when left uninstalled.

### Can I publish my configured version?

Do not publish a customized export until you have inspected every action and removed all credentials, contact details, private paths, reward IDs, identifiers, and community-history data.

### Is Linux supported?

The verified release target is Streamer.bot v1.0.7 on Windows. Linux compatibility-layer setups are outside the guaranteed configuration for StreamFolk 4.0.

---

## Download and Support

### Official Download

**[Download StreamFolk 4.0 FINAL from Ko-fi](https://ko-fi.com/s/58ec72e935)**

### Reporting a Problem

When reporting an issue, include:

- Your Streamer.bot version
- The affected StreamFolk action
- Whether the C# code compiled
- Whether the action appeared in Action History
- The expected result
- The actual result
- Reproduction steps
- A sanitized log excerpt when useful

Never post a complete configured `.sb` export, App Password, email address, phone gateway, private path, webhook, or unredacted viewer history in an issue.

---

## Project History

| Version | Main actions | Main commands | Primary focus |
|---|---:|---:|---|
| 1.0 | 24 | 14 | Initial attendance, analytics, community commands, reporting, and resets |
| 2.0 | 25 | 14 | Added the optional Diaper Check interaction and simplified several modules |
| 3.0 | 25 | 14 | Reliability, automatic attendance, subscription improvements, templates, and privacy documentation |
| **4.0 FINAL** | **28** | **16** | True retention, expanded reports, safer public packaging, readable source, and v1.0.7 compatibility |

Read [CHANGELOG.txt](./CHANGELOG.txt) for the complete release history.

---

## Disclaimer

StreamFolk is an independent community project created by **babydreamsy**. It is not an official Twitch, Streamer.bot, TwitchTracker, SullyGnome, or Streams Charts product.

Twitch remains the authoritative source for official account, revenue, advertisement, payout, & tax information.

---

## Final Message

A stream is more than an average viewer number. It is the people who showed up, returned, interacted, supported one another, and helped create the community around it.

**StreamFolk: Built By Streamers. Powered by Community.**
