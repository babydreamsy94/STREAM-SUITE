using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const string PatsReceivedKey = "PatCounts";
    private const string PatsGivenKey = "PatGiven";
    private const string StreamerName = "streamername";

    public bool Execute()
    {
        string invoker = (args != null && args.ContainsKey("user") && args["user"] != null)
            ? args["user"].ToString().Trim().TrimStart('@').ToLowerInvariant()
            : "";

        if (!string.Equals(invoker, StreamerName, StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendMessage($"@{invoker}, only the broadcaster can use this command 🛑");
            return true;
        }

        string empty = JsonConvert.SerializeObject(new Dictionary<string, int>());
        CPH.SetGlobalVar(PatsReceivedKey, empty, true);
        CPH.SetGlobalVar(PatsGivenKey, empty, true);
        CPH.SendMessage($"@{invoker} cleared all pat stats!");
        return true;
    }
}
