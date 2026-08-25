using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string PatsKey = "PatCounts";
    private const string PatsGivenKey = "PatGiven";
    private const string SeenUsersKey = "SeenUsers";
    public bool Execute()
    {
        var invoker = (string)args["user"];
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"]?.ToString() ?? "" : "";
        var parts = rawInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        // Default: no mode
        string mode = "";
        // If user typed a mode, grab the next word
        if (parts.Length > 1)
            mode = parts[1].ToLowerInvariant();
        // ❗ NEW: Reject if missing or invalid
        if (mode != "givers" && mode != "receivers")
        {
            CPH.SendMessage($"Please specify either 'givers' or 'receivers'. Example: !pboard givers");
            return true;
        }

        var patsReceived = GetDict(PatsKey);
        var patsGiven = GetDict(PatsGivenKey);
        var seen = GetDict(SeenUsersKey);
        var giverList = new List<KeyValuePair<string, int>>();
        foreach (var kv in patsGiven)
            if (seen.ContainsKey(kv.Key))
                giverList.Add(kv);
        giverList.Sort((a, b) => b.Value.CompareTo(a.Value));
        var receiverList = new List<KeyValuePair<string, int>>();
        foreach (var kv in patsReceived)
            if (seen.ContainsKey(kv.Key))
                receiverList.Add(kv);
        receiverList.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (giverList.Count == 0 && receiverList.Count == 0)
        {
            CPH.SendMessage($"@{invoker}, I tried to show the board but everyone’s hiding under a mountain of plushies! 🧸");
            return true;
        }

        var giverParts = BuildTop3(giverList);
        var receiverParts = BuildTop3(receiverList);
        string giverBoard = giverParts.Count > 0 ? string.Join(" | ", giverParts) : "No pats given yet";
        string receiverBoard = receiverParts.Count > 0 ? string.Join(" | ", receiverParts) : "No pats received yet";
        if (mode == "givers")
            CPH.SendMessage($"Pat Giver Leaderboard (Top 3): {giverBoard}");
        else if (mode == "receivers")
            CPH.SendMessage($"Pat Receiver Leaderboard (Top 3): {receiverBoard}");
        return true;
    }

    private List<string> BuildTop3(List<KeyValuePair<string, int>> list)
    {
        var parts = new List<string>();
        for (int i = 0; i < list.Count && i < 3; i++)
            parts.Add($"{i + 1}) @{list[i].Key} — {list[i].Value}");
        return parts;
    }

    private Dictionary<string, int> GetDict(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}
