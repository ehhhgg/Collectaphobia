using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameState
{
    public double Courage;
    public long TotalClicks;
    public int ClicksUntilRelic;
    public readonly Dictionary<Rarity, int> Relics = new();
    public readonly Dictionary<string, int> AutoClickers = new();
    public readonly Dictionary<string, int> PhobiaCounts = new();
    public readonly List<MissionRecord> Missions = new();

    public int RelicCount(Rarity rarity) => Relics.TryGetValue(rarity, out var n) ? n : 0;

    public int AutoCount(string id) => AutoClickers.TryGetValue(id, out var n) ? n : 0;

    public int PhobiaCount(string id) =>
        !string.IsNullOrEmpty(id) && PhobiaCounts.TryGetValue(id, out var n) ? n : 0;

    public void AddRelic(Rarity rarity)
    {
        Relics[rarity] = RelicCount(rarity) + 1;
    }

    public void AddPhobia(string id, Rarity rarity)
    {
        AddRelic(rarity);
        if (string.IsNullOrEmpty(id))
            return;
        PhobiaCounts[id] = PhobiaCount(id) + 1;
    }

    public bool TrySpendPhobias(string id, Rarity rarity, int amount)
    {
        if (amount <= 0 || PhobiaCount(id) < amount)
            return false;

        PhobiaCounts[id] = PhobiaCount(id) - amount;
        if (PhobiaCounts[id] <= 0)
            PhobiaCounts.Remove(id);

        Relics[rarity] = Mathf.Max(0, RelicCount(rarity) - amount);
        return true;
    }

    public void AddAuto(string id)
    {
        AutoClickers[id] = AutoCount(id) + 1;
    }

    public void Clear()
    {
        Courage = 0d;
        TotalClicks = 0;
        ClicksUntilRelic = 1;
        Relics.Clear();
        AutoClickers.Clear();
        PhobiaCounts.Clear();
        Missions.Clear();
    }

    public MissionRecord MissionFor(string phobiaId)
    {
        if (string.IsNullOrEmpty(phobiaId))
            return null;

        for (var i = 0; i < Missions.Count; i++)
        {
            if (Missions[i] != null && Missions[i].phobiaId == phobiaId)
                return Missions[i];
        }

        return null;
    }

    public bool IsPhobiaOnMission(string phobiaId) => MissionFor(phobiaId) != null;

    public MissionRecord MissionForJob(string jobId)
    {
        var job = MissionRules.JobById(jobId);
        for (var i = 0; i < Missions.Count; i++)
        {
            if (Missions[i] == null)
                continue;
            if (MissionRules.JobById(Missions[i].missionId).id == job.id)
                return Missions[i];
        }

        return null;
    }

    public MissionRecord FirstCompletableMission()
    {
        for (var i = 0; i < Missions.Count; i++)
        {
            if (Missions[i] != null && Missions[i].IsComplete)
                return Missions[i];
        }

        return null;
    }

    public float ClicksPerSecond(ClickerCatalog catalog)
    {
        if (catalog == null)
            return 0f;

        var cps = 0f;
        foreach (var def in catalog.autoClickers)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;
            cps += def.clicksPerSecond * AutoCount(def.id);
        }

        return cps;
    }

    public static int CostFor(AutoClickerDefinition def, int owned)
    {
        return Mathf.CeilToInt(def.baseCost * Mathf.Pow(1.18f, owned));
    }

    public void Load(
        double courage,
        long totalClicks,
        int clicksUntilRelic,
        AutoEntry[] autos,
        int[] relics,
        AutoEntry[] phobias = null,
        MissionRecord[] missions = null)
    {
        Courage = courage;
        TotalClicks = totalClicks;
        ClicksUntilRelic = Mathf.Max(1, clicksUntilRelic);
        AutoClickers.Clear();
        Relics.Clear();
        PhobiaCounts.Clear();
        Missions.Clear();

        if (autos != null)
        {
            foreach (var entry in autos)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.count <= 0)
                    continue;
                AutoClickers[entry.id] = entry.count;
            }
        }

        if (relics != null)
        {
            var names = (Rarity[])Enum.GetValues(typeof(Rarity));
            var count = Mathf.Min(relics.Length, names.Length);
            for (var i = 0; i < count; i++)
            {
                if (relics[i] > 0)
                    Relics[names[i]] = relics[i];
            }
        }

        if (phobias != null)
        {
            foreach (var entry in phobias)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.count <= 0)
                    continue;
                PhobiaCounts[entry.id] = entry.count;
            }
        }

        if (missions == null)
            return;

        foreach (var mission in missions)
        {
            if (mission == null || string.IsNullOrEmpty(mission.phobiaId))
                continue;
            Missions.Add(mission);
        }
    }

    public AutoEntry[] SaveAutos()
    {
        var entries = new AutoEntry[AutoClickers.Count];
        var i = 0;
        foreach (var pair in AutoClickers)
            entries[i++] = new AutoEntry { id = pair.Key, count = pair.Value };
        return entries;
    }

    public AutoEntry[] SavePhobias()
    {
        var entries = new AutoEntry[PhobiaCounts.Count];
        var i = 0;
        foreach (var pair in PhobiaCounts)
            entries[i++] = new AutoEntry { id = pair.Key, count = pair.Value };
        return entries;
    }

    public MissionRecord[] SaveMissions()
    {
        return Missions.ToArray();
    }

    public int[] SaveRelics()
    {
        var names = (Rarity[])Enum.GetValues(typeof(Rarity));
        var values = new int[names.Length];
        for (var i = 0; i < names.Length; i++)
            values[i] = RelicCount(names[i]);
        return values;
    }
}

[Serializable]
public class AutoEntry
{
    public string id;
    public int count;
}
