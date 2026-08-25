using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string SeenUsersKey = "SeenUsers";
    private static readonly System.Random rng = new System.Random();
    private static readonly string[] bonkPhrases = new[]
    {
        "@{invokerRaw} bonks @{targetRaw} on the head with a hammer!",
        "@{invokerRaw} hits @{targetRaw} with a rolled up newspaper!",
        "@{invokerRaw} cracks @{targetRaw} with a baseball bat!",
        "@{invokerRaw} taps @{targetRaw} with a wooden stick!",
    };
    // ⭐ ONE self‑bonk phrase
    private static readonly string[] selfBonkPhrases = new[]
    {
        "@{invokerRaw} bonked themselves...but why tho?"
    };
    public bool Execute()
    {
        var invokerRaw = (string)args["user"];
        var invoker = Normalize(invokerRaw);
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"].ToString() : "";
        var targetRaw = rawInput.Replace("!bonk", "").Trim().TrimStart('@');
        var target = Normalize(targetRaw);
        // 🔒 SeenUsers validation
        var seen = GetDict(SeenUsersKey);
        if (!seen.ContainsKey(invoker))
        {
            CPH.SendMessage($"🛑 BONK FAILED! @{invokerRaw} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        if (!string.IsNullOrEmpty(target) && !seen.ContainsKey(target))
        {
            CPH.SendMessage($"🛑 @{invokerRaw} BONK FAILED! @{targetRaw} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        string phrase;
        // Self-bonk
        if (string.IsNullOrEmpty(target) || target == invoker)
        {
            phrase = selfBonkPhrases[0].Replace("@{invokerRaw}", invokerRaw);
        }
        else
        {
            phrase = bonkPhrases[rng.Next(bonkPhrases.Length)].Replace("@{invokerRaw}", invokerRaw).Replace("@{targetRaw}", targetRaw);
        }

        CPH.SendMessage(phrase);
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
