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
    string _missionReadyToastId;

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
        TickMissions();

        var cps = State.ClicksPerSecond(catalog);
        if (cps <= 0f)
            return;

        _autoBank += cps * Time.deltaTime;
        while (_autoBank >= 1f)
        {
            _autoBank -= 1f;
            RegisterClick();
        }
    }

    public void ManualClick()
    {
        if (hud != null && hud.IsRevealOpen)
            return;

        if (world != null && world.TryOpenBall(out var def) && def != null)
        {
            var firstTime = State.PhobiaCount(def.id) == 0;
            State.AddPhobia(def.id, def.rarity);
            if (firstTime)
                hud.ShowFirstUnlock(def);
            else
                hud.ShowRelicToast(def);
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
        Notify();
    }

    public void ResetProgress()
    {
        _autoBank = 0f;
        _missionReadyToastId = null;
        State.Clear();
        State.ClicksUntilRelic = RollRelicThreshold();
        if (world != null)
            world.ClearDrops();

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
        Notify();
        return true;
    }

    public bool TryStartGather(string phobiaId)
    {
        if (catalog == null || string.IsNullOrEmpty(phobiaId))
            return false;

        var def = catalog.PhobiaById(phobiaId);
        if (def == null || State.PhobiaCount(phobiaId) <= 0)
            return false;
        if (State.IsPhobiaOnMission(phobiaId))
            return false;
        if (State.Missions.Count >= MissionRules.MaxConcurrent)
            return false;

        State.Missions.Add(new MissionRecord
        {
            missionId = MissionRules.GatherId,
            phobiaId = phobiaId,
            startedUtcTicks = DateTime.UtcNow.Ticks,
            durationSeconds = (float)MissionRules.GatherDuration.TotalSeconds,
            collectedAtStart = State.PhobiaCount(phobiaId),
            rarity = (int)def.rarity
        });
        Notify();
        return true;
    }

    public bool TryClaimMission(string phobiaId)
    {
        var mission = State.MissionFor(phobiaId);
        if (mission == null || !mission.IsComplete)
            return false;

        var reward = MissionRules.Reward((Rarity)mission.rarity, mission.collectedAtStart);
        State.Courage += reward;
        State.Missions.Remove(mission);
        if (_missionReadyToastId == phobiaId)
            _missionReadyToastId = null;

        var def = catalog != null ? catalog.PhobiaById(phobiaId) : null;
        var name = def != null ? def.Label : "Phobia";
        if (hud != null)
            hud.ShowToast($"{name} gathered  +{FormatCourage(reward)} Courage", RarityStyle.Color((Rarity)mission.rarity), 2.4f);
        Notify();
        return true;
    }

    public double PreviewGatherReward(string phobiaId)
    {
        if (catalog == null)
            return 0d;
        var def = catalog.PhobiaById(phobiaId);
        if (def == null)
            return 0d;
        return MissionRules.Reward(def.rarity, State.PhobiaCount(phobiaId));
    }

    void TickMissions()
    {
        var ready = State.FirstCompletableMission();
        if (ready == null)
        {
            _missionReadyToastId = null;
            return;
        }

        if (_missionReadyToastId == ready.phobiaId)
            return;

        _missionReadyToastId = ready.phobiaId;
        if (hud != null)
            hud.ShowToast("Mission complete  ·  open Collection", new Color(0.95f, 0.42f, 0.55f), 2.4f);
        Notify();
    }

    static string FormatCourage(double value)
    {
        if (value < 1000d)
            return Mathf.FloorToInt((float)value).ToString();
        if (value < 1_000_000d)
            return $"{value / 1000d:0.0}K";
        return $"{value / 1_000_000d:0.00}M";
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
        var def = catalog != null ? catalog.RandomPhobiaOf(rarity) : null;
        if (def != null)
            world.StorePhobia(def);
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
