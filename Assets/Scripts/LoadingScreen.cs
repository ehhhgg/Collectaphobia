using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// First screen in the build. Fills a bar, then opens the Title scene.
// Parent Ryan's animation under RyanAnimation. This screen does not animate that slot.
public class LoadingScreen : MonoBehaviour
{
    public const string TitleSceneName = "Title";
    public const string AnimationSlotName = "RyanAnimation";

    [SerializeField] Image progressFill;
    [SerializeField] Text statusText;
    [Tooltip("Empty slot for Ryan's loading animation. Parent it here.")]
    [SerializeField] RectTransform ryanAnimation;
    [SerializeField] string titleSceneName = TitleSceneName;
    [SerializeField] float minimumSeconds = 1.5f;

    void Awake()
    {
        ResolveRefs();
        if (progressFill != null)
        {
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (progressFill.sprite == null)
                progressFill.sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");
        }

        if (ryanAnimation == null)
            Debug.LogWarning("LoadingScreen has no RyanAnimation slot. Parent the loading animation under a child named RyanAnimation.", this);
        if (statusText != null)
            statusText.text = "Loading";
        SetProgress(0f);
    }

    void Start()
    {
        StartCoroutine(LoadTitle());
    }

    void ResolveRefs()
    {
        if (ryanAnimation == null)
            ryanAnimation = transform.Find(AnimationSlotName) as RectTransform;

        if (statusText == null)
        {
            var status = transform.Find("Status");
            if (status != null)
                statusText = status.GetComponent<Text>();
        }

        if (progressFill == null)
        {
            var fill = transform.Find("Progress/Fill");
            if (fill != null)
                progressFill = fill.GetComponent<Image>();
        }
    }

    IEnumerator LoadTitle()
    {
        if (string.IsNullOrEmpty(titleSceneName))
            titleSceneName = TitleSceneName;

        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                $"LoadingScreen finished, but scene '{titleSceneName}' is not in the build. That scene is the title-screen handoff.",
                this);
            if (statusText != null)
                statusText.text = "Title scene missing";
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(titleSceneName);
        if (op == null)
        {
            Debug.LogError($"LoadingScreen could not load '{titleSceneName}'.", this);
            yield break;
        }

        op.allowSceneActivation = false;
        var elapsed = 0f;
        while (op.progress < 0.9f && elapsed < 8f)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(DisplayedProgress(elapsed, op.progress));
            yield return null;
        }

        while (elapsed < minimumSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(Mathf.Clamp01(elapsed / minimumSeconds));
            yield return null;
        }

        SetProgress(1f);
        op.allowSceneActivation = true;
    }

    float DisplayedProgress(float elapsed, float loadProgress)
    {
        var timeT = minimumSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / minimumSeconds);
        var loadT = Mathf.Clamp01(loadProgress / 0.9f);
        return Mathf.Min(timeT, loadT);
    }

    void SetProgress(float value)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(value);
    }
}
