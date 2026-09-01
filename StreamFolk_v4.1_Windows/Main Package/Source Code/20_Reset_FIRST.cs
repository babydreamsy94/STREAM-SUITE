using System;

public class CPHInline
{
    public bool Execute()
    {
        // 👇 Change this to your Twitch username
        string allowedUser = "streamername";  

        string invokerRaw = (string)args["user"];

        // Only allow broadcaster
        if (!invokerRaw.Equals(allowedUser, StringComparison.OrdinalIgnoreCase))
        {
            CPH.SendMessage($"⛔ Sorry @{invokerRaw}, but only {allowedUser} can use this command.");
            return true;
        }

        // Clear both dictionaries
        CPH.SetGlobalVar("FirstStats", "", true);
        CPH.SetGlobalVar("FirstDates", "", true);

        CPH.SendMessage("💣 Reset complete — all FIRST! wins, streaks, and last claim dates wiped.");
        return true;
    }
}
