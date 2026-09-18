using System;
using UnityEngine;

[Serializable]
public class RelicDefinition
{
    public Rarity rarity;
    public GameObject prefab;
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
    public RelicDefinition[] relics = Array.Empty<RelicDefinition>();
    public AutoClickerDefinition[] autoClickers = Array.Empty<AutoClickerDefinition>();

    public RelicDefinition RelicOf(Rarity rarity)
    {
        for (var i = 0; i < relics.Length; i++)
        {
            if (relics[i].rarity == rarity)
                return relics[i];
        }

        return relics.Length > 0 ? relics[0] : null;
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
