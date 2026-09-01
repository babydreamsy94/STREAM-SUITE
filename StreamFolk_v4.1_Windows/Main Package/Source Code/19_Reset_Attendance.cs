using System;
using System.Collections.Generic;

public class CPHInline
{
    private const string SeenUsersKey = "SeenUsers";

    // ⭐ Insert your Twitch username here (lowercase)
    private const string StreamerName = "streamername";

    public bool Execute()
    {
        string invoker = args["user"].ToString().ToLower();

        // 🔒 Protection: Only the streamer can run this command
        if (invoker != StreamerName)
        {
            CPH.SendMessage($"/me @{invoker} you do not have permission to reset attendance.");
            return true;
        }

        // ⭐ Reset SeenUsers by overwriting with an empty dictionary
        var emptyDict = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emptyDict);
        CPH.SetGlobalVar(SeenUsersKey, json, true);

        // Confirmation
        CPH.SendMessage("/me Attendance Check has been reset!");
        CPH.LogInfo("SeenUsers cleared via !resetattendance command.");

        return true;
    }
}
