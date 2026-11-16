using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip[] musicClips;  // drag all 3 songs in here
    [SerializeField] private bool shuffle = false;    // tick for random order

    private static BackgroundMusic instance;
    private AudioSource source;
    private int currentIndex = -1;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = false;          // must be off so the song can end and the next one start
        source.playOnAwake = false;

        if (musicClips == null || musicClips.Length == 0)
        {
            Debug.LogError("BackgroundMusic: no songs assigned.");
            return;
        }

        ApplyVolume();
        PlayNext();
    }

    private void Update()
    {
        if (source == null || musicClips == null || musicClips.Length == 0)
            return;

        // when the current song finishes, start the next one
        if (!source.isPlaying)
            PlayNext();
    }

    private void PlayNext()
    {
        if (shuffle && musicClips.Length > 1)
        {
            int next;
            do { next = Random.Range(0, musicClips.Length); }
            while (next == currentIndex);   // avoid repeating the same song twice in a row
            currentIndex = next;
        }
        else
        {
            currentIndex = (currentIndex + 1) % musicClips.Length;  // 0, 1, 2, 0, 1, 2...
        }

        source.clip = musicClips[currentIndex];
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
        var src = GetComponent<AudioSource>();
        if (src != null)
            src.volume = GameAudio.MusicOutput;
    }
}