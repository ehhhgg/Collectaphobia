using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] Button openButton;
    [SerializeField] GameObject overlay;
    [SerializeField] Button closeButton;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Button creditsButton;
    [SerializeField] GameObject creditsOverlay;
    [SerializeField] Button creditsCloseButton;

    bool _open;

    public bool IsOpen => _open;

    void Awake()
    {
        ResolveSceneRefs();
    }

    void Start()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);
        if (creditsCloseButton != null)
            creditsCloseButton.onClick.AddListener(CloseCredits);

        BindSlider(masterSlider, GameAudio.Master, GameAudio.SetMaster);
        BindSlider(musicSlider, GameAudio.Music, GameAudio.SetMusic);
        BindSlider(sfxSlider, GameAudio.Sfx, GameAudio.SetSfx);

        if (overlay != null)
            overlay.SetActive(false);
        else
            Debug.LogError("SettingsScreen has no overlay in the scene.", this);

        CloseCredits();
    }

    public void Open()
    {
        _open = true;
        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }

        CloseCredits();
        RefreshSliders();
    }

    public void Close()
    {
        _open = false;
        CloseCredits();
        if (overlay != null)
            overlay.SetActive(false);
    }

    public void OpenCredits()
    {
        if (creditsOverlay == null)
            return;
        creditsOverlay.SetActive(true);
        creditsOverlay.transform.SetAsLastSibling();
    }

    public void CloseCredits()
    {
        if (creditsOverlay != null)
            creditsOverlay.SetActive(false);
    }

    void RefreshSliders()
    {
        SetSlider(masterSlider, GameAudio.Master);
        SetSlider(musicSlider, GameAudio.Music);
        SetSlider(sfxSlider, GameAudio.Sfx);
    }

    static void BindSlider(Slider slider, float value, System.Action<float> setter)
    {
        if (slider == null)
            return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => setter(v));
    }

    static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }

    void ResolveSceneRefs()
    {
        if (openButton == null)
        {
            var found = transform.Find("Settings");
            if (found != null)
                openButton = found.GetComponent<Button>();
        }

        if (overlay == null)
        {
            var found = transform.Find("SettingsOverlay");
            if (found != null)
                overlay = found.gameObject;
        }

        if (overlay == null)
            return;

        var root = overlay.transform;
        if (closeButton == null)
            closeButton = FindNamed<Button>(root, "Panel/Close");
        if (masterSlider == null)
            masterSlider = FindNamed<Slider>(root, "Panel/Master/Slider");
        if (musicSlider == null)
            musicSlider = FindNamed<Slider>(root, "Panel/Music/Slider");
        if (sfxSlider == null)
            sfxSlider = FindNamed<Slider>(root, "Panel/Sfx/Slider");
        if (creditsButton == null)
            creditsButton = FindNamed<Button>(root, "Panel/Credits");
        if (creditsOverlay == null)
        {
            var credits = root.Find("Credits");
            if (credits != null)
                creditsOverlay = credits.gameObject;
        }

        if (creditsOverlay != null && creditsCloseButton == null)
            creditsCloseButton = FindNamed<Button>(creditsOverlay.transform, "Panel/Close");
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
