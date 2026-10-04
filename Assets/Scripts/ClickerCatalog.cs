using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PhobiaDefinition
{
    public string id;
    public string displayName;
    public Rarity rarity;
    public Texture art;
    public GameObject prefab;
    public string artist;
    [TextArea(3, 8)]
    public string description;
    [TextArea]
    public string notes;

    public string Label => string.IsNullOrWhiteSpace(displayName) ? RarityStyle.Label(rarity) : displayName;
}

[Serializable]
public class AutoClickerDefinition
{
    public string id;
    public string displayName;
    public int baseCost;
    public float clicksPerSecond;
    public GameObject prefab;
}

[CreateAssetMenu(menuName = "Phobia/Clicker Catalog", fileName = "ClickerCatalog")]
public class ClickerCatalog : ScriptableObject
{
    [FormerlySerializedAs("relics")]
    public PhobiaDefinition[] phobias = Array.Empty<PhobiaDefinition>();
    public AutoClickerDefinition[] autoClickers = Array.Empty<AutoClickerDefinition>();

    public PhobiaDefinition PhobiaById(string id)
    {
        if (string.IsNullOrEmpty(id) || phobias == null)
            return null;

        for (var i = 0; i < phobias.Length; i++)
        {
            if (phobias[i] != null && phobias[i].id == id)
                return phobias[i];
        }

        return null;
    }

    public PhobiaDefinition PhobiaOf(Rarity rarity)
    {
        return RandomPhobiaOf(rarity);
    }

    public PhobiaDefinition RandomPhobiaOf(Rarity rarity)
    {
        var match = Pick(rarity, true);
        if (match != null)
            return match;
        return Pick(rarity, false);
    }

    PhobiaDefinition Pick(Rarity rarity, bool requireMatch)
    {
        if (phobias == null)
            return null;

        PhobiaDefinition fallback = null;
        var matched = 0;
        PhobiaDefinition chosen = null;
        for (var i = 0; i < phobias.Length; i++)
        {
            var def = phobias[i];
            if (def == null || def.prefab == null)
                continue;
            if (requireMatch && def.rarity != rarity)
                continue;
            if (fallback == null)
                fallback = def;
            matched++;
            if (UnityEngine.Random.Range(0, matched) == 0)
                chosen = def;
        }

        return chosen != null ? chosen : fallback;
    }

    public AutoClickerDefinition AutoById(string id)
    {
        for (var i = 0; i < autoClickers.Length; i++)
        {
            if (autoClickers[i].id == id)
                return autoClickers[i];
        }

        return null;
    }
}
