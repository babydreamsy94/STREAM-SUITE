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
        var invokerRaw = (string)args["user"];
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"]?.ToString() ?? "" : "";
        var parts = rawInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        // Default: self lookup
        string targetRaw = invokerRaw;
        // If user typed a username, grab the next word
        if (parts.Length > 1)
            targetRaw = parts[1].Trim().TrimStart('@');
        var targetNorm = Normalize(targetRaw);
        var patsReceived = GetDict(PatsKey);
        var patsGiven = GetDict(PatsGivenKey);
        var seen = GetDict(SeenUsersKey);
        if (!seen.ContainsKey(targetNorm))
        {
            CPH.SendMessage($"@{invokerRaw}, I tried to check @{targetRaw}'s stats but they’re hiding under a mountain of plushies! 🧸");
            return true;
        }

        int receivedCount = patsReceived.ContainsKey(targetNorm) ? patsReceived[targetNorm] : 0;
        int givenCount = patsGiven.ContainsKey(targetNorm) ? patsGiven[targetNorm] : 0;
        CPH.SendMessage($"@{targetRaw} has given {givenCount} pats and received {receivedCount} pats!");
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
