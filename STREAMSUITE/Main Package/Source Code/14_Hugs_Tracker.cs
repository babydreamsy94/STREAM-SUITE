using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string HugStatsKey = "HugStats";
    private const string SeenUsersKey = "SeenUsers";
    private static readonly System.Random rng = new System.Random();
    private static readonly string[] hugPhrases = new[]
    {
        "@{invokerRaw} gives @{targetRaw} a big hug like a teddy bear! 🧸 (They have now received {count} hugs!)",
        "@{invokerRaw} wraps @{targetRaw} in their soft arms! 🤗 (They have now received {count} hugs!)",
        "@{invokerRaw} squeezes @{targetRaw} with all their love! 💖 (They have now received {count} hugs!)",
        "@{invokerRaw} cuddles @{targetRaw} like a soft pillow ☁️ (They have now received {count} hugs!)",
        "@{invokerRaw} nuzzles @{targetRaw} while wagging their tail! 🐶 (They have now received {count} hugs!)"
    };
    private const string selfHugError = "@{invokerRaw} tried to hug themselves but failed horribly. Awkward! 😳";
    public bool Execute()
    {
        var invokerRaw = (string)args["user"];
        var invoker = Normalize(invokerRaw);
        var rawInput = args.ContainsKey("rawInput") ? args["rawInput"].ToString() : "";
        var targetRaw = rawInput.Replace("!hug", "").Trim().TrimStart('@');
        var target = Normalize(targetRaw);
        // 🔒 SeenUsers validation
        var seen = GetDict(SeenUsersKey);
        if (!seen.ContainsKey(invoker))
        {
            CPH.SendMessage($"🛑 HUG FAILED! @{invokerRaw} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        if (!string.IsNullOrEmpty(target) && !seen.ContainsKey(target))
        {
            CPH.SendMessage($"🛑 @{invokerRaw} HUG FAILED! @{targetRaw} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        var stats = GetDict(HugStatsKey);
        string phrase;
        if (string.IsNullOrEmpty(target) || target == invoker)
        {
            phrase = selfHugError.Replace("@{invokerRaw}", invokerRaw);
        }
        else
        {
            // ✅ Separate tracking for given and received
            string invokerGivenKey = invoker + "_given";
            string targetReceivedKey = target + "_received";
            if (!stats.ContainsKey(invokerGivenKey))
                stats[invokerGivenKey] = 0;
            if (!stats.ContainsKey(targetReceivedKey))
                stats[targetReceivedKey] = 0;
            stats[invokerGivenKey]++; // hugs given
            stats[targetReceivedKey]++; // hugs received
            SaveDict(HugStatsKey, stats);
            phrase = hugPhrases[rng.Next(hugPhrases.Length)].Replace("{count}", stats[targetReceivedKey].ToString()).Replace("@{invokerRaw}", invokerRaw).Replace("@{targetRaw}", targetRaw);
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

    private void SaveDict(string key, Dictionary<string, int> dict)
    {
        var json = JsonConvert.SerializeObject(dict);
        CPH.SetGlobalVar(key, json, true);
    }
}
