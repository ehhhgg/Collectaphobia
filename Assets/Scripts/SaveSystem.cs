using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    const string SaveTimeKey = "LastSaveTime";
    const string StateKey = "ClickerState";
    const double MaxOfflineSeconds = 7d * 24d * 60d * 60d;

    [SerializeField] ClickerGame game;
    [SerializeField] ClickerHud hud;

    bool _sessionOpen;
    double _offlineGain;
    TimeSpan _offlineTime;

    [Serializable]
    class SaveData
    {
        public double courage;
        public long totalClicks;
        public int clicksUntilRelic;
        public AutoEntry[] autos;
        public int[] relics;
        public AutoEntry[] phobias;
        public MissionRecord[] missions;
    }

    void Awake()
    {
        if (game == null)
            game = GetComponent<ClickerGame>();
        if (hud == null)
            hud = GetComponentInChildren<ClickerHud>(true);

        LoadState();
        CollectOffline();
    }

    IEnumerator Start()
    {
        if (game != null)
            game.AfterSaveRestored();

        _sessionOpen = true;
        WriteSave();

        if (_offlineGain < 1d)
            yield break;

        yield return null;
        if (hud != null)
            hud.ShowOfflineGain(_offlineGain, _offlineTime);
    }

    void OnApplicationPause(bool paused)
    {
        if (!_sessionOpen || game == null)
            return;

        if (paused)
        {
            WriteSave();
            return;
        }

        CollectOffline();
        game.AfterSaveRestored();
        WriteSave();
        if (_offlineGain >= 1d && hud != null)
            hud.ShowOfflineGain(_offlineGain, _offlineTime);
    }

    void OnApplicationQuit()
    {
        WriteSave();
    }

    void OnDisable()
    {
        WriteSave();
    }

    void LoadState()
    {
        if (game == null || !PlayerPrefs.HasKey(StateKey))
            return;

        var json = PlayerPrefs.GetString(StateKey);
        if (string.IsNullOrEmpty(json))
            return;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
            return;

        game.State.Load(data.courage, data.totalClicks, data.clicksUntilRelic, data.autos, data.relics, data.phobias, data.missions);
    }

    void CollectOffline()
    {
        _offlineGain = 0d;
        _offlineTime = TimeSpan.Zero;
        if (game == null || !PlayerPrefs.HasKey(SaveTimeKey))
            return;

        if (!DateTime.TryParse(
                PlayerPrefs.GetString(SaveTimeKey),
                null,
                DateTimeStyles.RoundtripKind,
                out var lastSave))
            return;

        if (lastSave.Kind == DateTimeKind.Unspecified)
            lastSave = DateTime.SpecifyKind(lastSave, DateTimeKind.Utc);

        var elapsed = DateTime.UtcNow - lastSave.ToUniversalTime();
        var seconds = Math.Clamp(elapsed.TotalSeconds, 0d, MaxOfflineSeconds);
        if (seconds < 1d)
            return;

        _offlineTime = TimeSpan.FromSeconds(seconds);
        _offlineGain = game.GrantOfflineCourage(seconds);
    }

    void WriteSave()
    {
        if (!_sessionOpen || game == null)
            return;

        var state = game.State;
        var data = new SaveData
        {
            courage = state.Courage,
            totalClicks = state.TotalClicks,
            clicksUntilRelic = state.ClicksUntilRelic,
            autos = state.SaveAutos(),
            relics = state.SaveRelics(),
            phobias = state.SavePhobias(),
            missions = state.SaveMissions()
        };

        PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(data));
        PlayerPrefs.SetString(SaveTimeKey, DateTime.UtcNow.ToString("O"));
        PlayerPrefs.Save();
    }
}
