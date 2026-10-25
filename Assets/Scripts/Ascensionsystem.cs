using System;
using UnityEngine;

public class AscensionSystem : MonoBehaviour
{
    const string CountKey = "AscensionCount";

    [SerializeField] ClickerGame game;
    [SerializeField] int totalPhobias = 20;          // set to your real phobia count
    [SerializeField] float bonusPerAscension = 0.5f; // +50% courage per ascension

    public int Count { get; private set; }
    public float Multiplier => 1f + Count * bonusPerAscension;
    public float NextMultiplier => 1f + (Count + 1) * bonusPerAscension;
    public long TotalClicks => game.State.TotalClicks;

    /// <summary>Raised when the collection becomes complete (and on launch if already complete).</summary>
    public event Action CollectionCompleted;

    double _lastCourage;
    int _lastOwned;

    void Awake()
    {
        if (game == null)
            game = FindFirstObjectByType<ClickerGame>();

        Count = PlayerPrefs.GetInt(CountKey, 0);
    }

    void Start()
    {
        _lastCourage = game.State.Courage;
        _lastOwned = OwnedCount();

        game.StateChanged += OnStateChanged;

        if (IsComplete())
            CollectionCompleted?.Invoke();
    }

    void OnDestroy()
    {
        if (game != null)
            game.StateChanged -= OnStateChanged;
    }

    // Boost every courage gain by (multiplier - 1). Spending lowers courage and is ignored.
    void LateUpdate()
    {
        var courage = game.State.Courage;
        var gained = courage - _lastCourage;

        if (gained > 0d && Multiplier > 1f)
        {
            courage += gained * (Multiplier - 1f);
            game.State.Courage = courage;
        }

        _lastCourage = courage;
    }

    void OnStateChanged()
    {
        var owned = OwnedCount();
        var justCompleted = owned != _lastOwned && IsComplete();
        _lastOwned = owned;

        if (justCompleted)
            CollectionCompleted?.Invoke();
    }

    int OwnedCount()
    {
        var owned = 0;
        foreach (var pair in game.State.PhobiaCounts)
            if (pair.Value > 0) owned++;
        return owned;
    }

    public bool IsComplete() => totalPhobias > 0 && OwnedCount() >= totalPhobias;

    public void Ascend()
    {
        if (!IsComplete())
            return;

        Count++;
        PlayerPrefs.SetInt(CountKey, Count);

        // Delete the saved game first so a fresh start is what loads next launch.
        SaveSystem.DeleteSavedData();
        game.ResetProgress();      // existing full reset
        game.AfterSaveRestored();  // refresh the UI

        _lastCourage = game.State.Courage;
        _lastOwned = 0;
        PlayerPrefs.Save();
    }
}