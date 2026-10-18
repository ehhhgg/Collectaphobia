using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
   
    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)][SerializeField] private float volume = 0.6f;

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
        source.volume = volume;
        source.playOnAwake = false;
        source.Play();
    }
}