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

    public int RelicCount(Rarity rarity) => Relics.TryGetValue(rarity, out var n) ? n : 0;

    public int AutoCount(string id) => AutoClickers.TryGetValue(id, out var n) ? n : 0;

    public void AddRelic(Rarity rarity)
    {
        Relics[rarity] = RelicCount(rarity) + 1;
    }

    public void AddAuto(string id)
    {
        AutoClickers[id] = AutoCount(id) + 1;
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

    public void Load(double courage, long totalClicks, int clicksUntilRelic, AutoEntry[] autos, int[] relics)
    {
        Courage = courage;
        TotalClicks = totalClicks;
        ClicksUntilRelic = Mathf.Max(1, clicksUntilRelic);
        AutoClickers.Clear();
        Relics.Clear();

        if (autos != null)
        {
            foreach (var entry in autos)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.count <= 0)
                    continue;
                AutoClickers[entry.id] = entry.count;
            }
        }

        if (relics == null)
            return;

        var names = (Rarity[])Enum.GetValues(typeof(Rarity));
        var count = Mathf.Min(relics.Length, names.Length);
        for (var i = 0; i < count; i++)
        {
            if (relics[i] > 0)
                Relics[names[i]] = relics[i];
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
