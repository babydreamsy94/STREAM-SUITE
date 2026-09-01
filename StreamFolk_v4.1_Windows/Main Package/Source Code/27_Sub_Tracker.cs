using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    private const string SubsKey = "analytics.subs";
    private const string SubsDetailedKey = "analytics.subsDetailed";
    private const string TotalSubsKey = "analytics.totalSubs";
    private static readonly string[] ExcludedUsers =
    {
        "streamername",
        "botname",
        "nightbot",
        "streamelements",
        "streamlabs",
        "sery_bot"
    };
    public bool Execute()
    {
        try
        {
            if (args == null)
            {
                CPH.LogWarn("Sub Tracking: args is null.");
                return true;
            }

            CPH.LogInfo("Sub Tracking RAW: " + JsonConvert.SerializeObject(args));
            string trigger = GetTriggerName();
            CPH.LogInfo($"Detected event type: {trigger}");
            switch (trigger)
            {
                case "Subscription":
                    HandleSubscription();
                    break;
                case "Gift Subscription":
                    HandleGiftSubscription();
                    break;
                case "ReSubscription":
                case "Resubscription":
                case "Resub":
                    HandleResubscription();
                    break;
                case "Gift Bomb":
                    HandleGiftBomb();
                    break;
                default:
                    CPH.LogWarn($"Unknown trigger type: {trigger}");
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogError($"Sub Tracking Error: {ex}");
            return true;
        }
    }

    private void HandleSubscription()
    {
        string user = Normalize(GetFirst("userName", "displayName", "user", "userLogin"));
        if (string.IsNullOrWhiteSpace(user) || IsExcluded(user))
            return;
        string tier = NormalizeTier(GetFirst("tier", "subTier", "plan", "subPlan"));
        string subType = tier == "Prime" ? "Prime" : "Sub";
        UpdateSubData(user, BuildLabel(subType, tier));
        IncrementTotal(1);
        CPH.LogInfo($"Subscription tracked: {user} ({tier})");
    }

    private void HandleResubscription()
    {
        string user = Normalize(GetFirst("userName", "displayName", "user", "userLogin"));
        if (string.IsNullOrWhiteSpace(user) || IsExcluded(user))
            return;
        string tier = NormalizeTier(GetFirst("tier", "subTier", "plan", "subPlan"));
        int months = GetInt("cumulative_months", "cumulativeMonths", "monthsSubscribed", "streakMonths", "streak_months", "months", "monthCount");
        UpdateSubData(user, BuildLabel("Resub", tier, months));
        IncrementTotal(1);
        CPH.LogInfo($"Resub tracked: {user} ({tier}, {months} months)");
    }

    private void HandleGiftSubscription()
    {
        bool fromGiftBomb = GetBool("fromGiftBomb");
        string recipient = Normalize(GetFirst("recipientUserName", "recipientName", "recipientDisplayName"));
        string gifter = Normalize(GetFirst("gifterUserName", "gifterName", "gifterDisplayName"));
        string tier = NormalizeTier(GetFirst("tier", "subTier", "plan", "subPlan"));
        if (!string.IsNullOrWhiteSpace(recipient) && !IsExcluded(recipient))
        {
            UpdateSubData(recipient, BuildLabel("Gifted", tier));
        }

        if (!fromGiftBomb)
        {
            if (!string.IsNullOrWhiteSpace(gifter) && !IsExcluded(gifter))
            {
                UpdateSubData(gifter, BuildLabel("Gift", tier));
            }

            IncrementTotal(1);
        }
    }

    private void HandleGiftBomb()
    {
        int gifts = GetInt("gifts", "giftCount", "massGiftCount");
        if (gifts <= 0)
            gifts = 1;
        string gifter = Normalize(GetFirst("gifterUserName", "gifterName", "gifterDisplayName"));
        string tier = NormalizeTier(GetFirst("tier", "subTier", "plan", "subPlan"));
        bool anonymous = string.IsNullOrWhiteSpace(gifter) || gifter.Contains("anonymous");
        if (!anonymous && !IsExcluded(gifter))
        {
            UpdateSubData(gifter, BuildLabel("GiftBomb", tier, 0, gifts));
        }

        for (int i = 0; i < gifts; i++)
        {
            string recipient = Normalize(GetString($"gift.recipientUserName{i}"));
            if (string.IsNullOrWhiteSpace(recipient) || IsExcluded(recipient))
                continue;
            UpdateSubData(recipient, BuildLabel("Gifted", tier));
        }

        IncrementTotal(gifts);
        CPH.LogInfo($"Gift bomb processed ({gifts} gifts)");
    }

    private string GetIcon(string type, string tier)
    {
        switch (type)
        {
            case "Sub":
                return "🌟";
            case "Prime":
                return "📦";
            case "Resub":
                return "🔃";
            case "Gift":
            case "Gifted":
                return "🎁";
            case "GiftBomb":
                return "💣";
            default:
                return "✨";
        }
    }

    private string DetermineTier()
    {
        string tier = GetFirst("tier", "subTier")?.Trim()?.ToLowerInvariant();
        switch (tier)
        {
            case "prime":
                return "Prime";
            case "tier 1":
            case "1000":
                return "Tier 1";
            case "tier 2":
            case "2000":
                return "Tier 2";
            case "tier 3":
            case "3000":
                return "Tier 3";
            default:
                CPH.LogWarn($"Unknown tier: '{tier}'");
                return "Tier 1";
        }
    }

    private void UpdateSubData(string user, string label)
    {
        var totals = GetDictInt(SubsKey);
        if (!totals.ContainsKey(user))
            totals[user] = 0;
        totals[user]++;
        SaveDictInt(SubsKey, totals);
        var details = GetDictList(SubsDetailedKey);
        if (!details.ContainsKey(user))
            details[user] = new List<string>();
        details[user].Add(label);
        SaveDictList(SubsDetailedKey, details);
    }

    private string BuildLabel(string type, string tier, int months = 0, int count = 1)
    {
        string icon = GetIcon(type, tier);
        string label;
        if (type == "Resub")
        {
            label = $"{icon} Resub: {tier}";
        }
        else if (type == "Prime")
        {
            label = $"{icon} Prime";
        }
        else if (type == "Gifted")
        {
            label = $"{icon} Gifted: {tier}";
        }
        else if (type == "GiftBomb")
        {
            label = $"{icon} Gift Bomb: {tier}";
        }
        else
        {
            label = $"{icon} {tier}";
        }

        if (months > 0)
            label += $" • {months}m";
        if (count > 1)
            label += $" • x{count}";
        return label;
    }

    private void IncrementTotal(int amount)
    {
        int total = CPH.GetGlobalVar<int>(TotalSubsKey, true);
        total += amount;
        if (total < 0)
            total = amount;
        CPH.SetGlobalVar(TotalSubsKey, total, true);
        CPH.LogInfo($"Total subs updated: {total}");
    }

    private bool IsExcluded(string user)
    {
        return ExcludedUsers.Contains(user, StringComparer.OrdinalIgnoreCase);
    }

    private string Normalize(string value)
    {
        return (value ?? "").Trim().TrimStart('@').ToLowerInvariant();
    }

    private string NormalizeTier(string tier)
    {
        tier = (tier ?? "").Trim().ToLowerInvariant();
        switch (tier)
        {
            case "prime":
            case "primegaming":
                return "Prime";
            case "1000":
            case "tier 1":
            case "tier1":
            case "1":
                return "Tier 1";
            case "2000":
            case "tier 2":
            case "tier2":
            case "2":
                return "Tier 2";
            case "3000":
            case "tier 3":
            case "tier3":
            case "3":
                return "Tier 3";
            default:
                return "Tier 1";
        }
    }

    private string GetTriggerName()
    {
        return GetFirst("triggerName", "eventSource", "eventType") ?? "";
    }

    private string GetFirst(params string[] keys)
    {
        foreach (string key in keys)
        {
            if (args.ContainsKey(key))
            {
                string value = args[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    private string GetString(string key)
    {
        if (!args.ContainsKey(key))
            return null;
        return args[key]?.ToString();
    }

    private bool GetBool(string key)
    {
        if (!args.ContainsKey(key))
            return false;
        bool.TryParse(args[key]?.ToString(), out bool result);
        return result;
    }

    private int GetInt(params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!args.ContainsKey(key))
                continue;
            if (int.TryParse(args[key]?.ToString(), out int result))
            {
                return result;
            }
        }

        return 0;
    }

    private Dictionary<string, int> GetDictInt(string key)
    {
        string json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var raw = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        return raw ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private void SaveDictInt(string key, Dictionary<string, int> dict)
    {
        CPH.SetGlobalVar(key, JsonConvert.SerializeObject(dict), true);
    }

    private Dictionary<string, List<string>> GetDictList(string key)
    {
        string json = CPH.GetGlobalVar<string>(key, true);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        var raw = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
        return raw ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    private void SaveDictList(string key, Dictionary<string, List<string>> dict)
    {
        CPH.SetGlobalVar(key, JsonConvert.SerializeObject(dict), true);
    }
}
