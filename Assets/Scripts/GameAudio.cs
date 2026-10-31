using System;
using UnityEngine;

public static class GameAudio
{
    const string MasterKey = "Audio.Master";
    const string MusicKey = "Audio.Music";
    const string SfxKey = "Audio.Sfx";

    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 0.6f;
    public static float Sfx { get; private set; } = 1f;

    public static float MusicOutput => Mathf.Clamp01(Master * Music);
    public static float SfxOutput => Mathf.Clamp01(Master * Sfx);

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Master = Load(MasterKey, 1f);
        Music = Load(MusicKey, 0.6f);
        Sfx = Load(SfxKey, 1f);
    }

    public static void SetMaster(float value) => Set(MasterKey, value, Master, v => Master = v);
    public static void SetMusic(float value) => Set(MusicKey, value, Music, v => Music = v);
    public static void SetSfx(float value) => Set(SfxKey, value, Sfx, v => Sfx = v);

    static void Set(string key, float value, float current, Action<float> assign)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(current, value))
            return;

        assign(value);
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    static float Load(string key, float fallback)
    {
        return Mathf.Clamp01(PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : fallback);
    }
}
