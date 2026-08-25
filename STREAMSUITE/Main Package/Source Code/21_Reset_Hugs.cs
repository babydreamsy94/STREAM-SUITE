using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string HugStatsKey = "HugStats";

    public bool Execute()
    {
        var invoker = (string)args["user"];

        // ✅ Replace "streamername" with your broadcaster username
        if (!string.Equals(invoker, "streamername", StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendMessage($"@{invoker}, only the broadcaster can use this command 🛑");
            return true;
        }

        // Clear HugStats
        var empty = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        var json = JsonConvert.SerializeObject(empty);
        CPH.SetGlobalVar(HugStatsKey, json, true);

        CPH.SendMessage($"@{invoker} cleared all hug stats!");
        return true;
    }
}
