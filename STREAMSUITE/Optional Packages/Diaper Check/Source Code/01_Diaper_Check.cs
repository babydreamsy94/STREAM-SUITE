using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private static Random rng = new Random();
    public bool Execute()
    {
        string triggeringUser = args.ContainsKey("userName") ? args["userName"].ToString() : "";
        string rawInput = args.ContainsKey("rawInput") ? args["rawInput"].ToString() : "";
        string[] parts = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        // If no argument, default to the triggering user (self-check)
        string targetUser = string.IsNullOrWhiteSpace(rawInput) ? triggeringUser : parts[0];
        // --- Normalize targetUser inline ---
        if (targetUser.StartsWith("@"))
            targetUser = targetUser.Substring(1);
        char[] trimChars =
        {
            ',',
            '.',
            '!',
            '?',
            ':',
            ';',
            ')',
            ']',
            '}',
            '\"',
            '\''
        };
        targetUser = targetUser.TrimEnd(trimChars);
        // Load SeenUsers dictionary from global variable
        var seenJson = CPH.GetGlobalVar<string>("SeenUsers", true);
        var seenDict = string.IsNullOrWhiteSpace(seenJson) ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) : JsonConvert.DeserializeObject<Dictionary<string, int>>(seenJson) ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!seenDict.ContainsKey(targetUser))
        {
            CPH.SendMessage($"🛑 @{triggeringUser} DIAPER CHECK FAILED! @{targetUser} HAS NOT DONE ATTENDANCE CHECK REDEEM! 🛑");
            return true;
        }

        // Rolls
        int soggyRoll = rng.Next(0, 101);
        int stinkyRoll = rng.Next(0, 101);
        string playfulMsg = GetPlayfulMessage(soggyRoll, stinkyRoll);
        // Self-check only if no argument was provided
        bool noArgument = string.IsNullOrWhiteSpace(rawInput);
        if (noArgument)
        {
            CPH.SendMessage($"@{triggeringUser} is {soggyRoll}% soggy & {stinkyRoll}% stinky! {playfulMsg}");
        }
        else
        {
            CPH.SendMessage($"@{targetUser} is {soggyRoll}% soggy & {stinkyRoll}% stinky! {playfulMsg}");
        }

        return true;
    }

    private static string GetPlayfulMessage(int soggy, int stinky)
    {
        var rand = new Random();
        if (soggy == 100 && stinky == 100)
        {
            var options = new[]
            {
                "Time for a much-needed change!",
                "Good thing I caught this before it was too late!",
                "Wow this one got really filled up!"
            };
            return options[rand.Next(options.Length)];
        }
        else if (soggy >= 51 && soggy <= 100 && stinky >= 51 && stinky <= 100)
        {
            var options = new[]
            {
                "These pamps are getting pretty heavy!",
                "Someone's been putting their pamps to good use!",
                "Looks like these pamps are doing their job well!",
            };
            return options[rand.Next(options.Length)];
        }
        else
        {
            var options = new[]
            {
                "Not quite full enough, I'd say.",
                "Don't worry, these can handle much more!",
                "These have PLENTY of room left!"
            };
            return options[rand.Next(options.Length)];
        }
    }
}