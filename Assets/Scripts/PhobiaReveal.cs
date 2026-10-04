using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PhobiaReveal : MonoBehaviour
{
    [SerializeField] ClickerGame game;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);
    static readonly Vector3 StageOffset = new(180f, 0f, 0f);

    readonly Queue<PhobiaDefinition> _queue = new();
    GameObject _overlay;
    GameObject _stage;
    RawImage _modelView;
    RawImage _artView;
    Text _bannerText;
    Text _nameText;
    Text _rarityText;
    Text _artistText;
    Text _descriptionText;
    Transform _pivot;
    GameObject _model;
    RenderTexture _rt;
    bool _open;

    public bool IsOpen => _open;

    void Awake()
    {
        if (game == null)
            game = GetComponentInParent<ClickerGame>();
    }

    void Start()
    {
        Build();
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

        if (_overlay == null)
            Build();

        if (_open && firstUnlock)
        {
            _queue.Enqueue(def);
            return;
        }

        Display(def, firstUnlock);
    }

    public void Close()
    {
        TearDownModel();
        if (_queue.Count > 0)
        {
            Display(_queue.Dequeue(), true);
            return;
        }

        _open = false;
        if (_overlay != null)
            _overlay.SetActive(false);
    }

    void Display(PhobiaDefinition def, bool firstUnlock)
    {
        _open = true;
        _overlay.SetActive(true);
        _overlay.transform.SetAsLastSibling();

        if (_bannerText != null)
        {
            _bannerText.text = firstUnlock ? "NEW PHOBIA" : "INSPECT";
            _bannerText.color = Accent;
        }

        if (_nameText != null)
        {
            _nameText.text = def.Label;
            _nameText.color = Color.white;
        }

        if (_rarityText != null)
        {
            _rarityText.text = RarityStyle.Label(def.rarity);
            _rarityText.color = RarityStyle.Color(def.rarity);
        }

        if (_artistText != null)
        {
            var hasArtist = !string.IsNullOrWhiteSpace(def.artist);
            _artistText.gameObject.SetActive(hasArtist);
            _artistText.text = hasArtist ? $"Art by {def.artist.Trim()}" : "";
        }

        if (_descriptionText != null)
        {
            var hasDescription = !string.IsNullOrWhiteSpace(def.description);
            _descriptionText.gameObject.SetActive(hasDescription);
            _descriptionText.text = hasDescription ? def.description.Trim() : "";
        }

        if (_artView != null)
        {
            var hasArt = def.art != null;
            _artView.gameObject.SetActive(hasArt);
            _artView.texture = def.art;
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

    void Build()
    {
        var canvas = transform as RectTransform;
        BuildStage();
        BuildOverlay(canvas);
        _overlay.SetActive(false);
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

        var lightGo = new GameObject("RevealLight");
        lightGo.transform.SetParent(stage, false);
        lightGo.transform.localPosition = new Vector3(-0.6f, 1.1f, -1.2f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 4.2f;
        light.color = new Color(1f, 0.92f, 0.95f);
    }

    void BuildOverlay(RectTransform canvas)
    {
        var overlay = Create("PhobiaReveal", canvas);
        Stretch(overlay);
        overlay.SetAsLastSibling();
        var dim = overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0.03f, 0.02f, 0.05f, 0.94f);
        dim.raycastTarget = true;
        _overlay = overlay.gameObject;

        var banner = CreateText("Banner", overlay, "NEW PHOBIA", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        banner.color = Accent;
        Stretch(banner.rectTransform, new Vector2(0.08f, 0.9f), new Vector2(0.92f, 0.96f));
        _bannerText = banner;

        _nameText = CreateText("Name", overlay, "", 44, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(_nameText.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.9f));

        _rarityText = CreateText("Rarity", overlay, "", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(_rarityText.rectTransform, new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.82f));

        var view = Create("ModelView", overlay);
        Stretch(view, new Vector2(0.16f, 0.42f), new Vector2(0.84f, 0.75f));
        _modelView = view.gameObject.AddComponent<RawImage>();
        _modelView.texture = _rt;
        _modelView.raycastTarget = false;
        _modelView.color = Color.white;

        var artRt = Create("Art", overlay);
        Stretch(artRt, new Vector2(0.08f, 0.3f), new Vector2(0.24f, 0.4f));
        _artView = artRt.gameObject.AddComponent<RawImage>();
        _artView.raycastTarget = false;
        _artView.color = Color.white;

        _artistText = CreateText("Artist", overlay, "", 20, TextAnchor.MiddleLeft);
        _artistText.color = new Color(0.86f, 0.82f, 0.9f, 1f);
        Stretch(_artistText.rectTransform, new Vector2(0.27f, 0.32f), new Vector2(0.92f, 0.4f));

        _descriptionText = CreateText("Description", overlay, "", 20, TextAnchor.UpperLeft);
        _descriptionText.color = new Color(0.8f, 0.78f, 0.86f, 1f);
        _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(_descriptionText.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.31f));

        var close = Create("Close", overlay);
        Stretch(close, new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.14f));
        var closeImage = close.gameObject.AddComponent<Image>();
        closeImage.color = Accent;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);
        var closeLabel = CreateText("Label", close, "CLOSE", 32, TextAnchor.MiddleCenter, FontStyle.Bold);
        closeLabel.color = Color.white;
        closeLabel.raycastTarget = false;
        Stretch(closeLabel.rectTransform);
    }

    static RectTransform Create(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Text CreateText(string name, Transform parent, string value, int size, TextAnchor align, FontStyle style = FontStyle.Normal)
    {
        var rt = Create(name, parent);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = UiFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = Color.white;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Font UiFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static void Stretch(RectTransform rt)
    {
        Stretch(rt, Vector2.zero, Vector2.one);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
