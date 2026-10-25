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

[Serializable]
public class JobDefinition
{
    public string id;
    public string displayName;
    public TimeSpan duration;
    public float rewardMultiplier;

    public JobDefinition(string id, string displayName, TimeSpan duration, float rewardMultiplier)
    {
        this.id = id;
        this.displayName = displayName;
        this.duration = duration;
        this.rewardMultiplier = rewardMultiplier;
    }

    public string DurationLabel
    {
        get
        {
            if (duration.TotalHours >= 1d)
                return $"{(int)duration.TotalHours}h";
            if (duration.TotalMinutes >= 1d)
                return $"{(int)duration.TotalMinutes}m";
            return $"{(int)duration.TotalSeconds}s";
        }
    }
}

public static class MissionRules
{
    public const int MaxConcurrent = 3;
    public const string GatherId = "gather";
    public const string QuickId = "quick";
    public const string ShiftId = "shift";
    public const string HaulId = "haul";

    public static readonly JobDefinition[] Jobs =
    {
        new(QuickId, "Quick Job", TimeSpan.FromSeconds(30), 1f),
        new(ShiftId, "Shift", TimeSpan.FromMinutes(5), 10f),
        new(HaulId, "Long Haul", TimeSpan.FromHours(1), 120f)
    };

    public static readonly TimeSpan GatherDuration = Jobs[0].duration;

    public static JobDefinition JobById(string id)
    {
        if (string.IsNullOrEmpty(id) || id == GatherId)
            return Jobs[0];

        for (var i = 0; i < Jobs.Length; i++)
        {
            if (Jobs[i].id == id)
                return Jobs[i];
        }

        return Jobs[0];
    }

    public static double Reward(Rarity rarity, int collected, string missionId = null)
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
        var timeMult = JobById(missionId).rewardMultiplier;
        return Math.Floor(48d * timeMult * rarityMult * countMult);
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
