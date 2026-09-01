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
        var rawInput = args.ContainsKey("rawInput") ? (string)args["rawInput"] : string.Empty;
        // Determine target
        var targetRaw = (rawInput ?? "").Trim();
        if (targetRaw.StartsWith("@"))
            targetRaw = targetRaw.Substring(1);
        if (string.IsNullOrEmpty(targetRaw))
            targetRaw = invokerRaw;
        var invokerNorm = Normalize(invokerRaw);
        var targetNorm = Normalize(targetRaw);
        bool isSelfPat = invokerNorm == targetNorm;
        var seen = GetDict(SeenUsersKey);
        // Plushie rejection stays consistent
        if (!isSelfPat && !seen.ContainsKey(targetNorm))
        {
            CPH.SendMessage($"🛑 @{invokerRaw} PAT FAILED! @{targetRaw} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        // ✅ Update counts first (only once)
        var receivedCount = IncrementCount(PatsKey, targetNorm);
        var givenCount = IncrementCount(PatsGivenKey, invokerNorm);
        // 🎲 Randomized phrase pools
        var rng = new Random();
        string[] selfPhrases =
        {
            $"@{invokerRaw} patted themselves on the back. Good job, kiddo! ✨ (You have received {receivedCount} pats now!)",
            $"@{invokerRaw} used their plushie to give themselves a pat! How cute! 🥺 (You have received {receivedCount} pats now!)",
            $"@{invokerRaw} patted their padded bum like the cute baby they are 🤭 (You have received {receivedCount} pats now!)",
            $"@{invokerRaw} touched their fluffy ears and made their tail wag! (You have received {receivedCount} pats now!)",
            $"@{invokerRaw} curls up for naptime with a cozy pat. 🌙 (You have received {receivedCount} pats now!)",
        };
        string[] otherPhrases =
        {
            $"@{invokerRaw} patted @{targetRaw} on the back! Proud of them! 😊 (They have received {receivedCount} pats now!)",
            $"@{invokerRaw} ruffled @{targetRaw}'s hair! How adorable! 🥺 (They have received {receivedCount} pats now!)",
            $"@{invokerRaw} patted @{targetRaw}'s padded bum! How cute! 🤭 (They have received {receivedCount} pats now!)",
            $"@{invokerRaw} played with @{targetRaw}'s fluffy ears and made their tail wag! (They have received {receivedCount} pats now!)",
            $"@{invokerRaw} used their plushie to pat @{targetRaw} on the head! Someone seems shy! 🤭 (They have received {receivedCount} pats now!)",
        };
        // Pick one phrase depending on self vs other
        string message = isSelfPat ? selfPhrases[rng.Next(selfPhrases.Length)] : otherPhrases[rng.Next(otherPhrases.Length)];
        // ✅ Send exactly one message
        CPH.SendMessage(message);
        return true;
    }

    // Helpers
    private int IncrementCount(string key, string name)
    {
        var dict = GetDict(key);
        if (dict.ContainsKey(name))
            dict[name]++;
        else
            dict[name] = 1;
        SaveDict(key, dict);
        return dict[name];
    }

    private Dictionary<string, int> GetDict(string key)
    {
        var json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private void SaveDict(string key, Dictionary<string, int> dict)
    {
        var json = JsonConvert.SerializeObject(dict);
        CPH.SetGlobalVar(key, json, true);
    }

    private string Normalize(string s) => (s ?? "").Trim().TrimStart('@').ToLowerInvariant();
}
