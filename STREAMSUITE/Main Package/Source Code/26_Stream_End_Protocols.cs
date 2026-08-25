using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Newtonsoft.Json;

public class CPHInline
{
    private const string SeenUsersKey = "SeenUsers";
    private const string AttendanceHistoryKey = "AttendanceHistory";
    private const string AttendanceSummaryKey = "analytics.attendanceSummaryJson";
    private const string FinalSummaryKey = "analytics.finalSummaryJson";
    // RENAMED — now stores LOCAL time
    private const string SessionStartKey = "analytics.sessionStartLocal";
    private const string SubsDetailedKey = "analytics.subsDetailed";
    private const string StreamerUser = "streamername";
    private static readonly string[] ExcludedUsers =
    {
        "streamername",
        "botname",
        "nightbot",
        "streamelements",
        "streamlabs",
        "sery_bot"
    };
    private bool IsExcluded(string user)
    {
        if (string.IsNullOrWhiteSpace(user))
            return false;
        var n = Normalize(user);
        foreach (var u in ExcludedUsers)
            if (string.Equals(n, Normalize(u), StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // SECURITY: Leave false until every SMTP and carrier placeholder is replaced.
    private const bool SendSmsEnabled = false;
    private const string SenderGmailAddress = "YOUR_GMAIL_ADDRESS";
    private const string GoogleAppPassword = "YOUR_16_CHARACTER_GOOGLE_APP_PASSWORD";
    private const string SmsGatewayAddress = "YOUR_10_DIGIT_NUMBER@YOUR_CARRIER_GATEWAY";
    public bool Execute()
    {
        CPH.SendMessage($"/me Great stream as always, @{StreamerUser}! Now let's reset everything for next time...");
        CPH.Wait(5000);
        CPH.TwitchClearChatMessages(true);
        CPH.TwitchEmoteOnly(true);
        CPH.TwitchSubscriberOnly(true);
        CPH.TwitchSlowMode(true, 120);
        AttendanceSummary attendance = BuildAttendanceSummary();
        FinalSummary finalSummary = BuildFinalSummary(attendance);
        string finalJson = JsonConvert.SerializeObject(finalSummary, Formatting.Indented);
        CPH.SetGlobalVar(FinalSummaryKey, finalJson, true);
        string smsBody = BuildSmsBody(finalSummary);
        SaveSummaryToDocuments(smsBody);
        if (SendSmsEnabled)
            SendSmsSummary(smsBody);
        var emptyDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        CPH.SetGlobalVar(SeenUsersKey, JsonConvert.SerializeObject(emptyDict), true);
        CPH.SendMessage("Alright everyone! The attendance sheet and chat have been cleared, and the chat is locked down until next time! Thank you all so much for joining @" + StreamerUser + "'s stream tonight! 🥰");
        return true;
    }

    private class AttendanceSummary
    {
        public string Date { get; set; }
        public int TotalAttendees { get; set; }
        public List<string> Returning { get; set; }
        public List<string> Newly { get; set; }
    }

    private class FinalSummary
    {
        public string GeneratedAtLocal { get; set; }
        public double DurationMinutes { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueChatters { get; set; }
        public string TopChatter { get; set; }
        public int TotalSubs { get; set; }
        public Dictionary<string, int> SubsPerUser { get; set; }
        public Dictionary<string, List<string>> SubsPerUserDetailed { get; set; }
        public int TotalFollows { get; set; }
        public Dictionary<string, int> FollowsPerUser { get; set; }
        public int TotalRaids { get; set; }
        public List<string> Raiders { get; set; } // Changed from Dictionary<string, int> RaidsPerRaiderViewers
        public int TotalBits { get; set; }
        public Dictionary<string, int> BitsPerUser { get; set; }
        public AttendanceSummary Attendance { get; set; }
        public double RetentionRate { get; set; }
        public double MessagesPerMinute { get; set; }
    }

    private AttendanceSummary BuildAttendanceSummary()
    {
        var seen = GetSeen();
        var history = GetHistory(); // READ-ONLY
        var today = DateTime.Now.Date;
        string todayStr = today.ToString("yyyy-MM-dd");
        List<string> returning = new();
        List<string> newly = new();
        foreach (var raw in seen.Keys)
        {
            var user = Normalize(raw);
            if (IsExcluded(user))
                continue;
            if (!history.ContainsKey(user))
            {
                newly.Add(user);
                continue;
            }

            var parsed = history[user].Select(d => DateTime.TryParse(d, out var dt) ? dt.Date : (DateTime? )null).Where(dt => dt != null).Select(dt => dt.Value).ToList();
            bool hasPast = parsed.Any(dt => dt < today);
            if (hasPast)
                returning.Add(user);
            else
                newly.Add(user);
        }

        var summary = new AttendanceSummary
        {
            Date = todayStr,
            TotalAttendees = seen.Keys.Count(k => !IsExcluded(k)),
            Returning = returning.OrderBy(x => x).ToList(),
            Newly = newly.OrderBy(x => x).ToList()
        };
        CPH.SetGlobalVar(AttendanceSummaryKey, JsonConvert.SerializeObject(summary, Formatting.Indented), true);
        return summary;
    }

    private FinalSummary BuildFinalSummary(AttendanceSummary attendance)
    {
        string startStr = CPH.GetGlobalVar<string>(SessionStartKey, true);
        // LOCAL TIME — no UTC conversion
        DateTime.TryParse(startStr, out var startLocal);
        var duration = DateTime.Now - startLocal;
        var chatByUser = GetDictInt("analytics.chatMessagesByUser");
        var subs = GetDictInt("analytics.subs");
        var subsDetailed = GetDictList(SubsDetailedKey);
        var follows = GetDictInt("analytics.follows");
        var raids = GetList("analytics.raids"); // Changed to List<string>
        var bits = GetDictInt("analytics.bits");
        foreach (var bot in ExcludedUsers)
        {
            var n = Normalize(bot);
            foreach (var k in chatByUser.Keys.Where(x => Normalize(x) == n).ToList())
                chatByUser.Remove(k);
            foreach (var k in subs.Keys.Where(x => Normalize(x) == n).ToList())
                subs.Remove(k);
            foreach (var k in subsDetailed.Keys.Where(x => Normalize(x) == n).ToList())
                subsDetailed.Remove(k);
            foreach (var k in follows.Keys.Where(x => Normalize(x) == n).ToList())
                follows.Remove(k);
            raids.RemoveAll(x => Normalize(x) == n); // Changed from dictionary key removal
            foreach (var k in bits.Keys.Where(x => Normalize(x) == n).ToList())
                bits.Remove(k);
        }

        int chatTotal = chatByUser.Values.Sum();
        // FIX: Only sum actual sub slices added to the channel (exclude the "Gift" action tag to prevent double-counting)
        int totalSubs = subs.Values.Sum();
        int totalFollows = follows.Values.Sum();
        int totalBits = bits.Values.Sum();
        string topChatter = chatByUser.Count > 0 ? chatByUser.OrderByDescending(kv => kv.Value).First().Key : "N/A";
        double messagesPerMinute = duration.TotalMinutes > 0 ? Math.Round(chatTotal / duration.TotalMinutes, 2) : 0;
        return new FinalSummary
        {
            GeneratedAtLocal = DateTime.Now.ToString("o"),
            DurationMinutes = Math.Round(duration.TotalMinutes, 1),
            TotalMessages = chatTotal,
            UniqueChatters = chatByUser.Count,
            TopChatter = topChatter,
            TotalSubs = totalSubs,
            SubsPerUser = subs,
            SubsPerUserDetailed = subsDetailed,
            TotalFollows = totalFollows,
            FollowsPerUser = follows,
            TotalRaids = raids.Count, // Changed from raids.Count on dict
            Raiders = raids, // Changed from RaidsPerRaiderViewers
            TotalBits = totalBits,
            BitsPerUser = bits,
            Attendance = attendance,
            RetentionRate = CalculateRetention(attendance),
            MessagesPerMinute = messagesPerMinute
        };
    }

    private double CalculateRetention(AttendanceSummary attendance)
    {
        var history = GetHistory();
        DateTime currentDate;
        if (attendance == null || string.IsNullOrWhiteSpace(attendance.Date) || !DateTime.TryParse(attendance.Date, out currentDate))
            return 0;
        currentDate = currentDate.Date;
        var allPastDates = history.SelectMany(kv => kv.Value ?? new List<string>()).Select(d =>
        {
            DateTime parsed;
            return DateTime.TryParse(d, out parsed) ? parsed.Date : (DateTime? )null;
        }).Where(d => d.HasValue && d.Value < currentDate).Select(d => d.Value).Distinct().OrderByDescending(d => d).ToList();
        if (allPastDates.Count == 0)
            return 0;
        DateTime previousDate = allPastDates.First();
        var previousAttendees = history.Where(kv => (kv.Value ?? new List<string>()).Any(d =>
        {
            DateTime parsed;
            return DateTime.TryParse(d, out parsed) && parsed.Date == previousDate;
        })).Select(kv => Normalize(kv.Key)).Where(u => !string.IsNullOrWhiteSpace(u) && !IsExcluded(u)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (previousAttendees.Count == 0)
            return 0;
        var currentAttendees = (attendance.Returning ?? new List<string>()).Concat(attendance.Newly ?? new List<string>()).Select(Normalize).Where(u => !string.IsNullOrWhiteSpace(u) && !IsExcluded(u)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        int retainedViewers = previousAttendees.Intersect(currentAttendees, StringComparer.OrdinalIgnoreCase).Count();
        return Math.Round((double)retainedViewers / previousAttendees.Count, 3);
    }

    private string BuildSupportOverview(FinalSummary summary)
    {
        var subs = summary.SubsPerUser ?? new Dictionary<string, int>();
        var subsDetailed = summary.SubsPerUserDetailed ?? new Dictionary<string, List<string>>();
        var follows = summary.FollowsPerUser ?? new Dictionary<string, int>();
        var raiders = summary.Raiders ?? new List<string>(); // Changed from dictionary
        var bits = summary.BitsPerUser ?? new Dictionary<string, int>();
        // SUBS
        string subBlock = $"• Subscriptions ({summary.TotalSubs}):\n";
        if (subs.Count == 0)
        {
            subBlock += "    - None\n";
        }
        else
        {
            foreach (var kvp in subs.OrderByDescending(x => x.Value))
            {
                string user = kvp.Key;
                int total = kvp.Value;
                List<string> types = subsDetailed.ContainsKey(user) ? CollapseTypes(subsDetailed[user]) : new List<string>();
                string typeText = types.Count > 0 ? string.Join(", ", types) : "Unknown";
                subBlock += $"    - {user} — {total} ({typeText})\n";
            }
        }

        // FOLLOWS
        string followBlock = $"• Follows ({summary.TotalFollows}):\n";
        if (follows.Count == 0)
        {
            followBlock += "    - None\n";
        }
        else
        {
            foreach (var f in follows.Keys.OrderBy(x => x))
                followBlock += $"    - {f}\n";
        }

        // RAIDS — Changed to simple list, no viewer counts
        string raidBlock = $"• Raids ({summary.TotalRaids}):\n";
        if (raiders.Count == 0)
        {
            raidBlock += "    - None\n";
        }
        else
        {
            foreach (var r in raiders.OrderBy(x => x))
                raidBlock += $"    - {r}\n";
        }

        // BITS
        string bitsBlock = $"• Bits ({summary.TotalBits}):\n";
        if (bits.Count == 0)
        {
            bitsBlock += "    - None\n";
        }
        else
        {
            int top = bits.Values.Max();
            foreach (var b in bits.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
            {
                string trophy = b.Value == top ? "🏆 " : "";
                bitsBlock += $"    - {trophy}{b.Key} — {b.Value} bits\n";
            }
        }

        return $"{subBlock}\n{followBlock}\n{raidBlock}\n{bitsBlock}";
    }

    private void AddBreakdown(List<string> parts, List<string> list, string type, string label)
    {
        int count = list.Count(x => x.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (count > 0)
            parts.Add($"{label} x{count}");
    }

    private List<string> CollapseTypes(List<string> types)
    {
        return types.GroupBy(t => t).Select(g =>
        {
            int count = g.Count();
            return count == 1 ? $" {g.Key}" : $" {g.Key} ×{count}";
        }).ToList();
    }

    private string FormatDuration(double totalMinutes)
    {
        int roundedMinutes = Math.Max(0, (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero));
        int hours = roundedMinutes / 60;
        int minutes = roundedMinutes % 60;
        if (hours == 0)
            return minutes + "m";
        if (minutes == 0)
            return hours + "h";
        return hours + "h " + minutes + "m";
    }

    private string BuildSmsBody(FinalSummary summary)
    {
        string retentionText = (summary.RetentionRate * 100).ToString("0.0") + "%";
        var allAttendees = (summary.Attendance?.Returning ?? new List<string>()).Concat(summary.Attendance?.Newly ?? new List<string>()).OrderBy(x => x).ToList();
        string attendeesBlock = string.Join("\n", allAttendees.Select(a => (summary.Attendance?.Newly ?? new List<string>()).Contains(a, StringComparer.OrdinalIgnoreCase) ? "- " + a + " 💖" : "- " + a));
        string supportOverview = BuildSupportOverview(summary);
        string sms = $@"────────────────────────────────
      📊 STREAM PERFORMANCE REPORT
────────────────────────────────

📅 Event Date: {DateTime.Now:MMM dd, yyyy}
🕒 Report Generated: {DateTime.Now:h:mm tt}
⏱️ Duration: {FormatDuration(summary.DurationMinutes)}

👥 Audience Overview
• Total Attendees: {summary.Attendance?.TotalAttendees ?? 0}
• Returning Attendees: {summary.Attendance?.Returning?.Count ?? 0}
• New Attendees: {summary.Attendance?.Newly?.Count ?? 0}
• Retention Rate: {retentionText}

💬 Engagement Overview
• Total Messages: {summary.TotalMessages}
• Messages per Minute: {summary.MessagesPerMinute}
• Unique Chatters: {summary.UniqueChatters}
• Top Chatter: {summary.TopChatter}

💙 Support Overview
{supportOverview}

📋 Attendee Roster
{attendeesBlock}

────────────────────────────────
💙 Stream Suite: Built by Streamers. Powered by Community.
────────────────────────────────";
        return sms.Trim();
    }

    private void SendSmsSummary(string smsBody)
    {
        if (HasSmsPlaceholder(SenderGmailAddress) ||
            HasSmsPlaceholder(GoogleAppPassword) ||
            HasSmsPlaceholder(SmsGatewayAddress))
        {
            CPH.LogWarn("Stream Suite End: SMS summary is enabled, but one or more email-to-SMS placeholders are not configured.");
            return;
        }

        try
        {
            using (var client = new SmtpClient("smtp.gmail.com", 587))
            using (var mail = new MailMessage())
            {
                client.Credentials = new NetworkCredential(SenderGmailAddress, GoogleAppPassword);
                client.EnableSsl = true;
                mail.From = new MailAddress(SenderGmailAddress);
                mail.To.Add(SmsGatewayAddress);
                mail.Body = smsBody;
                client.Send(mail);
            }
        }
        catch (Exception ex)
        {
            CPH.LogError("Error sending analytics SMS: " + ex.Message);
        }
    }

    private bool HasSmsPlaceholder(string value)
    {
        return string.IsNullOrWhiteSpace(value) ||
            value.IndexOf("YOUR_", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SaveSummaryToDocuments(string text)
    {
        try
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string folder = System.IO.Path.Combine(documents, "StreamSummaries");
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);
            string filePath = System.IO.Path.Combine(folder, $"Summary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            System.IO.File.WriteAllText(filePath, text);
        }
        catch (Exception ex)
        {
            CPH.LogError("Error saving summary to Documents: " + ex.ToString());
        }
    }

    private Dictionary<string, int> GetSeen()
    {
        var json = CPH.GetGlobalVar<string>(SeenUsersKey, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value;
            return dict;
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, List<string>> GetHistory()
    {
        var json = CPH.GetGlobalVar<string>(AttendanceHistoryKey, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value ?? new List<string>();
            return dict;
        }
        catch
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, int> GetDictInt(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value;
            return dict;
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private Dictionary<string, List<string>> GetDictList(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value ?? new List<string>();
            return dict;
        }
        catch
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    // Added to support the raids list format
    private List<string> GetList(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"GetList: Failed to deserialize '{key}', starting fresh. Error: {ex.Message}");
            return new List<string>();
        }
    }

    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();
}