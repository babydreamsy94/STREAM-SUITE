using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class CPHInline
{
    private const string SeenUsersKey = "SeenUsers";
    private const string AttendanceHistoryKey = "AttendanceHistory";

    private static readonly HashSet<string> ExcludedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "streamername",
        "botname",
        "nightbot",
        "streamelements",
        "streamlabs",
        "sery_bot"
    };

    public bool Execute()
    {
        // FIX 3: Guard against null args
        if (args == null)
        {
            CPH.LogInfo("Attendance Check: args is null, exiting.");
            return true;
        }

        // FIX 4: Extracted username lookup into a helper; added userDisplayName key
        string userRaw = GetArg("userLogin", "userName", "userDisplayName", "user");
        if (string.IsNullOrWhiteSpace(userRaw))
        {
            CPH.LogInfo("Attendance Check: no username provided, exiting.");
            return true;
        }

        string userNorm = Normalize(userRaw);

        if (ExcludedUsers.Contains(userNorm))
        {
            CPH.LogInfo($"Attendance Check: excluded user '{userNorm}' detected, exiting.");
            return true;
        }

        bool triggeredByFirstWords = args.ContainsKey("message") && args["message"] != null;
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        DateTime todayDate = DateTime.Now.Date;

        CPH.LogInfo($"Attendance Check fired. Raw: {userRaw}, Normalized: {userNorm}, TriggeredByFirstWords: {triggeredByFirstWords}");

        // --- 2) Update per-stream attendance (SeenUsers) ---
        var seen = GetSeen();
        bool isFirstThisStream = !seen.ContainsKey(userNorm);

        if (triggeredByFirstWords)
        {
            if (!isFirstThisStream)
            {
                // Already checked in this stream — stay silent
                CPH.LogInfo($"Attendance Check: (chat) {userNorm} already checked in this stream.");
                return true;
            }

            seen[userNorm] = 1;
            SaveSeen(seen);
            CPH.LogInfo($"Attendance Check: (chat) added {userNorm} to SeenUsers for this stream.");
        }
        else
        {
            // Redeem path
            if (!isFirstThisStream)
            {
                string rewardId = args.ContainsKey("rewardId") ? args["rewardId"]?.ToString() : null;
                string redemptionId = args.ContainsKey("redemptionId") ? args["redemptionId"]?.ToString() : null;

                if (!string.IsNullOrEmpty(rewardId) && !string.IsNullOrEmpty(redemptionId))
                {
                    CPH.TwitchRedemptionCancel(rewardId, redemptionId);
                    CPH.LogInfo($"Attendance Check: duplicate redeem by {userNorm}; refunded redemptionId {redemptionId}.");
                }
                else
                {
                    CPH.LogInfo($"Attendance Check: duplicate redeem by {userNorm}; no refund (missing ids).");
                }

                return true;
            }

            // FIX 1: Removed redundant inner isFirstThisStream check — guaranteed true here
            seen[userNorm] = 1;
            SaveSeen(seen);
            CPH.LogInfo($"Attendance Check: (redeem) added {userNorm} to SeenUsers for this stream.");
        }

        // --- 3) Update permanent attendance history (AttendanceHistory) ---
        var history = GetHistory();
        if (!history.ContainsKey(userNorm))
            history[userNorm] = new List<string>();

        var parsedDates = new List<DateTime>();
        foreach (var d in history[userNorm])
        {
            if (DateTime.TryParse(d, out DateTime dt))
                parsedDates.Add(dt.Date);
            else
                CPH.LogInfo($"Attendance Check: WARNING — could not parse date '{d}' for user {userNorm}");
        }

        // FIX 5: Note — if SeenUsers was cleared mid-stream (e.g. stream restart) but
        //         AttendanceHistory was not, a returning viewer re-triggering on the same
        //         calendar day would be welcomed again. Acceptable edge case but documented.
        bool hasPastDates = parsedDates.Any(dt => dt < todayDate);
        bool isNew = !hasPastDates;
        bool isReturning = hasPastDates;

        // FIX 2: Use parsed dates for duplicate check instead of raw string comparison
        //         to avoid misses from format variations (e.g. "2025-6-8" vs "2025-06-08")
        bool alreadyRecordedToday = parsedDates.Any(dt => dt == todayDate);
        if (!alreadyRecordedToday)
        {
            history[userNorm].Add(today);
            SaveHistory(history);
            CPH.LogInfo($"Attendance Check: added {today} to attendance history for {userNorm}.");
        }

        // --- 4) Messaging ---
        if (isNew)
        {
            CPH.SendMessage($"/me Welcome @{userNorm}! It looks like this is your first time here! 💖");
        }
        else if (isReturning)
        {
            CPH.SendMessage($"/me Welcome back, @{userNorm}! It's good to see you here again! 🤗");
        }

        return true;
    }

    // FIX 4: Helper to pull the first non-empty value from a list of arg keys
    private string GetArg(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (args.ContainsKey(key))
            {
                var val = args[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
        }
        return null;
    }

    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();

    private Dictionary<string, int> GetSeen()
    {
        var json = CPH.GetGlobalVar<string>(SeenUsersKey, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raw = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in raw) dict[kv.Key] = kv.Value;
            return dict;
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"GetSeen: failed to deserialize '{SeenUsersKey}', NOT resetting. Error: {ex.Message}");
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveSeen(Dictionary<string, int> dict)
    {
        CPH.SetGlobalVar(SeenUsersKey, JsonConvert.SerializeObject(dict), true);
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
            foreach (var kv in raw) dict[kv.Key] = kv.Value ?? new List<string>();
            return dict;
        }
        catch (Exception ex)
        {
            CPH.LogInfo($"GetHistory: FAILED to deserialize AttendanceHistory. NOT resetting. Error: {ex.Message}");
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveHistory(Dictionary<string, List<string>> dict)
    {
        CPH.SetGlobalVar(AttendanceHistoryKey, JsonConvert.SerializeObject(dict, Formatting.Indented), true);
    }
}
