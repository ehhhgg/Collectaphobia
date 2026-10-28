using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);
    static readonly Vector3 StageOffset = new(180f, 0f, 0f);

    readonly Queue<PhobiaDefinition> _queue = new();
    GameObject _stage;
    Transform _pivot;
    GameObject _model;
    RenderTexture _rt;
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
            Debug.LogError("PhobiaReveal has no overlay in the scene. Run Phobia/Sync Collection Screen.", this);
    }

    void Start()
    {
        EnsureStage();
    }

    void Update()
    {
        if (_pivot != null && _open)
            _pivot.Rotate(0f, 48f * Time.deltaTime, 0f, Space.Self);
    }

    void OnDestroy()
    {
        TearDownModel();
        if (_stage != null)
            Destroy(_stage);
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
    }

    public void Show(PhobiaDefinition def, bool firstUnlock = true)
    {
        if (def == null)
            return;

        ResolveSceneRefs();
        EnsureStage();

        if (_open && firstUnlock)
        {
            _queue.Enqueue(def);
            return;
        }

        Display(def, firstUnlock ? "NEW PHOBIA" : "INSPECT");
    }

    public void ShowMerge(PhobiaDefinition def, bool firstUnlock)
    {
        if (def == null)
            return;

        ResolveSceneRefs();
        EnsureStage();
        Display(def, firstUnlock ? "NEW PHOBIA" : "MERGE RESULT");
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
        if (def.prefab == null || _pivot == null)
            return;

        _model = Instantiate(def.prefab, _pivot);
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
        if (_pivot != null)
            _pivot.localRotation = Quaternion.identity;
    }

    static void FitToView(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var size = Mathf.Max(0.01f, bounds.size.magnitude);
        go.transform.localScale *= 1.45f / size;

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        go.transform.position += go.transform.parent.position - bounds.center;
    }

    void EnsureStage()
    {
        if (_pivot == null)
            BuildStage();
    }

    void BuildStage()
    {
        var parent = game != null ? game.transform : transform.root;
        var stage = new GameObject("RevealStage").transform;
        stage.SetParent(parent, true);
        stage.position = StageOffset;
        stage.rotation = Quaternion.identity;
        stage.localScale = Vector3.one;
        _stage = stage.gameObject;

        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(stage, false);

        var camGo = new GameObject("RevealCamera");
        camGo.transform.SetParent(stage, false);
        camGo.transform.localPosition = new Vector3(0f, 0.15f, -2.5f);
        camGo.transform.LookAt(_pivot);

        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.07f, 0.14f, 1f);
        cam.fieldOfView = 28f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 12f;
        cam.depth = -10;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        var extra = cam.GetUniversalAdditionalCameraData();
        extra.renderType = CameraRenderType.Base;
        extra.renderPostProcessing = false;

        _rt = new RenderTexture(1024, 1024, 16);
        _rt.Create();
        cam.targetTexture = _rt;
        if (modelView != null)
            modelView.texture = _rt;

        var lightGo = new GameObject("RevealLight");
        lightGo.transform.SetParent(stage, false);
        lightGo.transform.localPosition = new Vector3(-0.6f, 1.1f, -1.2f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 4.2f;
        light.color = new Color(1f, 0.92f, 0.95f);
    }

    void ResolveSceneRefs()
    {
        if (overlay == null)
        {
            var found = transform.Find("PhobiaReveal");
            if (found != null)
                overlay = found.gameObject;
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
