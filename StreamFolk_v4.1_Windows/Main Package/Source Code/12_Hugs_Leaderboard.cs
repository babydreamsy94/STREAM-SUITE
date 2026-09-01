using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string HugStatsKey = "HugStats";
    private const string SeenUsersKey = "SeenUsers";
    public bool Execute()
    {
        string invokerRaw = args["user"]?.ToString();
        string invoker = Normalize(invokerRaw);
        // Match the pat leaderboard behavior exactly
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"]?.ToString() ?? "" : "";
        var parts = rawInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string mode = "";
        if (parts.Length > 1)
            mode = parts[1].ToLowerInvariant();
        // Reject if missing or invalid
        if (mode != "givers" && mode != "receivers")
        {
            CPH.SendMessage($"Please specify either 'givers' or 'receivers'. Example: !hboard givers");
            return true;
        }

        var stats = GetDict(HugStatsKey);
        var seen = GetDict(SeenUsersKey);
        var giverList = new List<KeyValuePair<string, int>>();
        var receiverList = new List<KeyValuePair<string, int>>();
        foreach (var kv in stats)
        {
            if (kv.Key.EndsWith("_given"))
            {
                string user = kv.Key.Replace("_given", "");
                if (seen.ContainsKey(user))
                    giverList.Add(new KeyValuePair<string, int>(user, kv.Value));
            }
            else if (kv.Key.EndsWith("_received"))
            {
                string user = kv.Key.Replace("_received", "");
                if (seen.ContainsKey(user))
                    receiverList.Add(new KeyValuePair<string, int>(user, kv.Value));
            }
        }

        giverList.Sort((a, b) => b.Value.CompareTo(a.Value));
        receiverList.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (giverList.Count == 0 && receiverList.Count == 0)
        {
            CPH.SendMessage($"@{invoker}, no hugs have been recorded yet!");
            return true;
        }

        var giverParts = BuildTop3(giverList);
        var receiverParts = BuildTop3(receiverList);
        string giverBoard = giverParts.Count > 0 ? string.Join(" | ", giverParts) : "No hugs given yet";
        string receiverBoard = receiverParts.Count > 0 ? string.Join(" | ", receiverParts) : "No hugs received yet";
        if (mode == "givers")
            CPH.SendMessage($"💖 Hug Giver Leaderboard (Top 3): {giverBoard}");
        else
            CPH.SendMessage($"💖 Hug Receiver Leaderboard (Top 3): {receiverBoard}");
        return true;
    }

    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();
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
