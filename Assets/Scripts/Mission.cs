using System;
using UnityEngine;

[Serializable]
public class MissionRecord
{
    public string missionId;
    public string phobiaId;
    public long startedUtcTicks;
    public float durationSeconds;
    public int collectedAtStart;
    public int rarity;

    public DateTime StartedAt => new(startedUtcTicks, DateTimeKind.Utc);

    public DateTime EndsAt => StartedAt.AddSeconds(durationSeconds);

    public bool IsComplete => DateTime.UtcNow >= EndsAt;

    public TimeSpan Remaining
    {
        get
        {
            var left = EndsAt - DateTime.UtcNow;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }
    }
}

public static class MissionRules
{
    public const int MaxConcurrent = 1;
    public const string GatherId = "gather";
    public static readonly TimeSpan GatherDuration = TimeSpan.FromSeconds(30);

    public static double Reward(Rarity rarity, int collected)
    {
        var rarityMult = rarity switch
        {
            Rarity.Common => 1f,
            Rarity.Uncommon => 1.6f,
            Rarity.Rare => 2.4f,
            Rarity.Epic => 3.6f,
            Rarity.Legendary => 5.5f,
            _ => 1f
        };

        var countMult = Mathf.Sqrt(Mathf.Max(1, collected));
        return Math.Floor(48d * rarityMult * countMult);
    }

    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1d)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes:00}m";
        if (remaining.TotalMinutes >= 1d)
            return $"{(int)remaining.TotalMinutes}m {remaining.Seconds:00}s";
        return $"{Mathf.Max(0, (int)Math.Ceiling(remaining.TotalSeconds))}s";
    }
}
