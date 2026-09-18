using UnityEngine;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public static class RarityStyle
{
    public static Color Color(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.72f, 0.74f, 0.78f),
            Rarity.Uncommon => new Color(0.35f, 0.82f, 0.48f),
            Rarity.Rare => new Color(0.32f, 0.58f, 1f),
            Rarity.Epic => new Color(0.78f, 0.38f, 1f),
            Rarity.Legendary => new Color(1f, 0.78f, 0.22f),
            _ => UnityEngine.Color.white
        };
    }

    public static string Label(Rarity rarity) => rarity.ToString().ToUpperInvariant();
}
