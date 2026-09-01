using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string HugStatsKey = "HugStats";
    private const string SeenUsersKey = "SeenUsers";
    public bool Execute()
    {
        var invokerRaw = (string)args["user"];
        var invoker = Normalize(invokerRaw);
        // ⭐ FIXED TARGET PARSING ⭐
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"]?.ToString() ?? "" : "";
        var parts = rawInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        // Default: self lookup
        string targetRaw = invoker;
        // If user typed a username, grab the next word
        if (parts.Length > 1)
            targetRaw = parts[1].Trim().TrimStart('@');
        var target = Normalize(targetRaw);
        var seen = GetDict(SeenUsersKey);
        if (!seen.ContainsKey(target))
        {
            CPH.SendMessage($"@{invokerRaw}, @{targetRaw} hasn’t chatted yet, so they don’t have hug stats 🧸✨");
            return true;
        }

        var stats = GetDict(HugStatsKey);
        string givenKey = target + "_given";
        string receivedKey = target + "_received";
        int hugsGiven = stats.ContainsKey(givenKey) ? stats[givenKey] : 0;
        int hugsReceived = stats.ContainsKey(receivedKey) ? stats[receivedKey] : 0;
        CPH.SendMessage($"@{targetRaw} has given {hugsGiven} hugs 💖 and received {hugsReceived} hugs 🧸");
        return true;
    }

    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();
    private Dictionary<string, int> GetDict(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}
