using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const string SeenUsersKey = "SeenUsers";
    private const string AttendanceHistoryKey = "AttendanceHistory";
    private const string SessionStartKey = "analytics.sessionStartLocal";
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
            if (u.Equals(n, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public bool Execute()
    {
        var userNorm = Normalize(StreamerUser);

        // --- Ensure SeenUsers exists and is case-insensitive ---
        var seen = GetDict(SeenUsersKey);
        if (!seen.ContainsKey(userNorm))
        {
            seen[userNorm] = 1;
            SaveDict(SeenUsersKey, seen);
            CPH.LogInfo($"Stream Online: added streamer '{userNorm}' to SeenUsers.");
        }
        else
        {
            CPH.LogInfo($"Stream Online: streamer '{userNorm}' already present in SeenUsers.");
        }

        // --- Ensure AttendanceHistory exists and is updated consistently ---
        var history = GetHistory();
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        DateTime todayDate = DateTime.Now.Date;

        if (!history.ContainsKey(userNorm))
            history[userNorm] = new List<string>();

        // FIX 4: Use parsed date comparison instead of raw string to avoid format variation
        var parsedDates = history[userNorm]
            .Select(d => DateTime.TryParse(d, out var dt) ? dt.Date : (DateTime?)null)
            .Where(d => d.HasValue)
            .Select(d => d.Value)
            .ToList();

        if (!parsedDates.Any(d => d == todayDate))
        {
            history[userNorm].Add(today);
            SaveHistory(history);
            CPH.LogInfo($"Stream Online: added today's date ({today}) to AttendanceHistory for '{userNorm}'.");
        }
        else
        {
            CPH.LogInfo($"Stream Online: AttendanceHistory already contains today's date for '{userNorm}'.");
        }

        // --- Reset per-stream analytics ---
        // FIX 1: Added analytics.subsDetailed to reset list so previous stream's
        //         sub type details don't bleed into the new stream's summary
        CPH.SetGlobalVar("analytics.chatMessagesByUser", "{}", true);
        CPH.SetGlobalVar("analytics.chatTotalMessages", 0, true);
        CPH.SetGlobalVar("analytics.subs", "{}", true);
        CPH.SetGlobalVar("analytics.subsDetailed", "{}", true);
        CPH.SetGlobalVar("analytics.totalSubs", 0, true);
        CPH.SetGlobalVar("analytics.follows", "{}", true);
        CPH.SetGlobalVar("analytics.totalFollows", 0, true);
        CPH.SetGlobalVar("analytics.raids", "[]", true);
        CPH.SetGlobalVar("analytics.bits", "{}", true);
        CPH.SetGlobalVar("analytics.totalBits", 0, true);
        CPH.SetGlobalVar("analytics.attendanceSummaryJson", "{}", true);

        // Mark session start (LOCAL TIME, ISO 8601)
        CPH.SetGlobalVar(SessionStartKey, DateTime.Now.ToString("o"), true);

        // Unlock chat + welcome
        CPH.SendMessage($"/me Hello @{userNorm}! Glad to see you here again! Unlocking chat now....");
        CPH.Wait(3000);
        CPH.TwitchEmoteOnly(false);
        CPH.TwitchSubscriberOnly(false);
        CPH.TwitchSlowMode(false);
        // Streamer.bot v1.0.7 requires a nullable duration argument here.
        // Passing null keeps the pinned message active until the stream ends.
        CPH.TwitchSendAndPinMessage($"👋 Welcome to @{userNorm}'s stream! This channel uses StreamFolk's attendance system instead of Twitch's viewer count, so please send a chat message or use the 'Attendance Check!' redeem to be counted as an attendee! Thanks for stopping by! 💙");
        CPH.TwitchUpdatePinnedMessageDuration(null);
        return true;
    
    
    }

    // --- Helpers ---
    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();

    // FIX 2: Re-wrap deserialized dict to guarantee OrdinalIgnoreCase comparer
    private Dictionary<string, int> GetDict(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, int>>(json)
                      ?? new Dictionary<string, int>();
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value;
            return dict;
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"GetDict: failed to deserialize key '{key}', resetting. Error: {ex.Message}");
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveDict(string key, Dictionary<string, int> dict)
    {
        CPH.SetGlobalVar(key, JsonConvert.SerializeObject(dict), true);
    }

    // FIX 3: Re-wrap deserialized dict to guarantee OrdinalIgnoreCase comparer
    private Dictionary<string, List<string>> GetHistory()
    {
        var json = CPH.GetGlobalVar<string>(AttendanceHistoryKey, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json)
                      ?? new Dictionary<string, List<string>>();
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw)
                dict[kv.Key] = kv.Value ?? new List<string>();
            return dict;
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"GetHistory: failed to deserialize AttendanceHistory. NOT resetting. Error: {ex.Message}");
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveHistory(Dictionary<string, List<string>> dict)
    {
        CPH.SetGlobalVar(AttendanceHistoryKey, JsonConvert.SerializeObject(dict, Formatting.Indented), true);
    }
}
