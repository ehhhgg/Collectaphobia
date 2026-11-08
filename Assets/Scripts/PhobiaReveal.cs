using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhobiaReveal : MonoBehaviour
{
    [SerializeField] ClickerGame game;
    [Header("Scene UI")]
    [SerializeField] GameObject overlay;
    [SerializeField] Text bannerText;
    [SerializeField] Text nameText;
    [SerializeField] Text rarityText;
    [SerializeField] Text artistText;
    [SerializeField] Text descriptionText;
    [SerializeField] RawImage modelView;
    [SerializeField] RawImage artView;
    [SerializeField] Button closeButton;
    [Header("Reveal Stage")]
    [SerializeField] Transform pivot;
    [SerializeField] Camera revealCamera;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    readonly Queue<PhobiaDefinition> _queue = new();
    GameObject _model;
    bool _open;

    public bool IsOpen => _open;

    void Awake()
    {
        if (game == null)
            game = GetComponentInParent<ClickerGame>();
        ResolveSceneRefs();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (overlay != null)
            overlay.SetActive(false);
        else
            Debug.LogError("PhobiaReveal has no overlay in the scene.", this);

        if (pivot == null)
            Debug.LogError("PhobiaReveal has no RevealStage pivot in the scene.", this);

        BindModelView();
    }

    void Update()
    {
        if (pivot != null && _open)
            pivot.Rotate(0f, 48f * Time.deltaTime, 0f, Space.Self);
    }

    void OnDestroy()
    {
        TearDownModel();
    }

    public void Show(PhobiaDefinition def, bool firstUnlock = true)
    {
        if (def == null)
            return;

        ResolveSceneRefs();
        BindModelView();

        if (_open && firstUnlock)
        {
            _queue.Enqueue(def);
            return;
        }

        Display(def, firstUnlock ? UnlockBanner(def) : "INSPECT");
    }

    public void ShowMerge(PhobiaDefinition def, bool firstUnlock)
    {
        if (def == null)
            return;

        ResolveSceneRefs();
        BindModelView();
        Display(def, firstUnlock ? UnlockBanner(def) : "MERGE RESULT");
    }

    public void Close()
    {
        TearDownModel();
        if (_queue.Count > 0)
        {
            Display(_queue.Dequeue(), "NEW PHOBIA");
            return;
        }

        _open = false;
        if (overlay != null)
            overlay.SetActive(false);
    }

    static string UnlockBanner(PhobiaDefinition def)
    {
        return def != null && def.IsSecret ? "SECRET PHOBIA" : "NEW PHOBIA";
    }

    void Display(PhobiaDefinition def, string banner)
    {
        if (overlay == null)
            return;

        _open = true;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();

        if (bannerText != null)
        {
            bannerText.text = banner;
            bannerText.color = Accent;
        }

        if (nameText != null)
        {
            nameText.text = def.Label;
            nameText.color = Color.white;
        }

        if (rarityText != null)
        {
            rarityText.text = RarityStyle.Label(def.rarity);
            rarityText.color = RarityStyle.Color(def.rarity);
        }

        if (artistText != null)
        {
            var hasArtist = !string.IsNullOrWhiteSpace(def.artist);
            artistText.gameObject.SetActive(hasArtist);
            artistText.text = hasArtist ? $"Art by {def.artist.Trim()}" : "";
        }

        if (descriptionText != null)
        {
            var hasDescription = !string.IsNullOrWhiteSpace(def.description);
            descriptionText.gameObject.SetActive(hasDescription);
            descriptionText.text = hasDescription ? def.description.Trim() : "";
        }

        if (artView != null)
        {
            var hasArt = def.art != null;
            artView.gameObject.SetActive(hasArt);
            artView.texture = def.art;
        }

        SpawnModel(def);
    }

    void SpawnModel(PhobiaDefinition def)
    {
        TearDownModel();
        if (def.prefab == null || pivot == null)
            return;

        _model = Instantiate(def.prefab, pivot);
        _model.name = def.Label;
        foreach (var col in _model.GetComponentsInChildren<Collider>())
            col.enabled = false;

        FitToView(_model);
    }

    void TearDownModel()
    {
        if (_model == null)
            return;
        Destroy(_model);
        _model = null;
        if (pivot != null)
            pivot.localRotation = Quaternion.identity;
    }

    static void FitToView(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        var bounds = Encapsulate(renderers);
        var extent = Mathf.Max(bounds.size.x, bounds.size.y, 0.01f);
        go.transform.localScale *= 1.1f / extent;

        bounds = Encapsulate(go.GetComponentsInChildren<Renderer>());
        var parent = go.transform.parent;
        if (parent == null)
            return;
        go.transform.position += parent.position - bounds.center;
    }

    static Bounds Encapsulate(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    void BindModelView()
    {
        if (modelView == null || revealCamera == null || revealCamera.targetTexture == null)
            return;
        modelView.texture = revealCamera.targetTexture;
    }

    void ResolveSceneRefs()
    {
        if (overlay == null)
        {
            var found = transform.Find("PhobiaReveal");
            if (found != null)
                overlay = found.gameObject;
        }

        if (pivot == null && game != null)
        {
            var found = game.transform.Find("RevealStage/Pivot");
            if (found != null)
                pivot = found;
        }

        if (revealCamera == null && pivot != null)
        {
            var cam = pivot.parent != null ? pivot.parent.GetComponentInChildren<Camera>(true) : null;
            if (cam != null)
                revealCamera = cam;
        }

        if (overlay == null)
            return;

        var root = overlay.transform;
        if (bannerText == null)
            bannerText = FindNamed<Text>(root, "Banner");
        if (nameText == null)
            nameText = FindNamed<Text>(root, "Name");
        if (rarityText == null)
            rarityText = FindNamed<Text>(root, "Rarity");
        if (artistText == null)
            artistText = FindNamed<Text>(root, "Artist");
        if (descriptionText == null)
            descriptionText = FindNamed<Text>(root, "Description");
        if (modelView == null)
            modelView = FindNamed<RawImage>(root, "ModelView");
        if (artView == null)
            artView = FindNamed<RawImage>(root, "Art");
        if (closeButton == null)
            closeButton = FindNamed<Button>(root, "Close");
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
