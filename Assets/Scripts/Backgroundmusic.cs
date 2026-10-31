using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
   
    [SerializeField] private AudioClip musicClip;

    private const string DefaultClipName = "ES_They_Come_at_Night_edit_-_Etienne_Roussel";

    private static BackgroundMusic instance;

    private void Awake()
    {
       
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicClip == null)
        {
            musicClip = Resources.Load<AudioClip>(DefaultClipName);
        }

        if (musicClip == null)
        {
            Debug.LogError("BackgroundMusic: no clip assigned and none found in Resources.");
            return;
        }

        AudioSource source = GetComponent<AudioSource>();
        source.clip = musicClip;
        source.loop = true;
        source.playOnAwake = false;
        ApplyVolume();
        source.Play();
    }

    void OnEnable()
    {
        GameAudio.Changed += ApplyVolume;
        ApplyVolume();
    }

    void OnDisable()
    {
        GameAudio.Changed -= ApplyVolume;
    }

    void ApplyVolume()
    {
        var source = GetComponent<AudioSource>();
        if (source != null)
            source.volume = GameAudio.MusicOutput;
    }
}