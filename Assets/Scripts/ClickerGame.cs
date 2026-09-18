using System;
using UnityEngine;

public class ClickerGame : MonoBehaviour
{
    [SerializeField] ClickerWorld world;
    [SerializeField] ClickerHud hud;
    [SerializeField] ClickerCatalog catalog;

    public readonly GameState State = new();
    public ClickerCatalog Catalog => catalog;
    public int PendingCount => world != null ? world.PendingCount : 0;

    float _autoBank;

    public event System.Action StateChanged;

    void Awake()
    {
        State.ClicksUntilRelic = RollRelicThreshold();
    }

    void Start()
    {
        Notify();
    }

    void Update()
    {
        var cps = State.ClicksPerSecond(catalog);
        if (cps <= 0f)
            return;

        _autoBank += cps * Time.deltaTime;
        var autoPulse = false;
        while (_autoBank >= 1f)
        {
            _autoBank -= 1f;
            RegisterClick();
            autoPulse = true;
        }

        if (autoPulse)
            world.PulseAutoClickers();
    }

    public void ManualClick()
    {
        if (world != null && world.TryOpenBall(out var rarity))
        {
            State.AddRelic(rarity);
            hud.ShowRelicToast(rarity);
            Notify();
            return;
        }

        RegisterClick();
        hud.SpawnClickPopup();
    }

    public double GrantOfflineCourage(double seconds)
    {
        if (seconds <= 0d)
            return 0d;

        var cps = State.ClicksPerSecond(catalog);
        if (cps <= 0f)
            return 0d;

        var exact = cps * seconds;
        var gained = Math.Floor(exact);
        State.Courage += gained;
        _autoBank += (float)(exact - gained);
        return gained;
    }

    public void AfterSaveRestored()
    {
        if (world != null)
            world.RefreshAutoClickers(State);
        Notify();
    }

    public bool TryBuy(string autoId)
    {
        var def = catalog.AutoById(autoId);
        if (def == null)
            return false;

        var cost = GameState.CostFor(def, State.AutoCount(def.id));
        if (State.Courage < cost)
            return false;

        State.Courage -= cost;
        State.AddAuto(def.id);
        world.RefreshAutoClickers(State);
        Notify();
        return true;
    }

    void RegisterClick()
    {
        State.Courage += 1d;
        State.TotalClicks += 1;
        State.ClicksUntilRelic -= 1;

        if (State.ClicksUntilRelic <= 0)
            DropRelic();

        Notify();
    }

    void DropRelic()
    {
        var rarity = RollRarity();
        State.ClicksUntilRelic = RollRelicThreshold();
        world.StorePhobia(rarity);
    }

    static int RollRelicThreshold()
    {
        return UnityEngine.Random.Range(8, 16);
    }

    static Rarity RollRarity()
    {
        var roll = UnityEngine.Random.value;
        if (roll < 0.02f)
            return Rarity.Legendary;
        if (roll < 0.08f)
            return Rarity.Epic;
        if (roll < 0.20f)
            return Rarity.Rare;
        if (roll < 0.45f)
            return Rarity.Uncommon;
        return Rarity.Common;
    }

    void Notify() => StateChanged?.Invoke();
}
