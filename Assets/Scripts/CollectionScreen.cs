using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionScreen : MonoBehaviour
{
    [SerializeField] ClickerGame game;

    ClickerHud _hud;
    RectTransform _canvas;
    GameObject _overlay;
    Text _progressText;
    Text _collectionTab;
    RectTransform _content;
    Text _emptyText;
    Text _detailText;
    Text _actionLabel;
    Button _actionButton;
    Image _actionImage;
    readonly List<Row> _rows = new();
    string _selectedId;
    bool _open;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    class Row
    {
        public string id;
        public Image background;
        public RawImage art;
        public Text nameText;
        public Text rarityText;
        public Text countText;
        public Button inspectButton;
        public Image inspectImage;
        public Text inspectLabel;
    }

    void Awake()
    {
        if (game == null)
            game = GetComponentInParent<ClickerGame>();
        _hud = GetComponent<ClickerHud>();
        _canvas = transform as RectTransform;
    }

    void Start()
    {
        Build();
        if (game != null)
            game.StateChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (game != null)
            game.StateChanged -= Refresh;
    }

    void Update()
    {
        if (_open)
            RefreshMissionUi();
        else
            RefreshTab();
    }

    public void Open()
    {
        _open = true;
        if (_overlay != null)
        {
            _overlay.SetActive(true);
            _overlay.transform.SetAsLastSibling();
        }

        Refresh();
    }

    public void Close()
    {
        _open = false;
        if (_overlay != null)
            _overlay.SetActive(false);
        RefreshTab();
    }

    void Refresh()
    {
        if (game == null)
            return;

        var catalog = game.Catalog;
        var total = catalog != null && catalog.phobias != null ? catalog.phobias.Length : 0;
        var unlocked = 0;
        if (catalog != null && catalog.phobias != null)
        {
            foreach (var def in catalog.phobias)
            {
                if (def != null && game.State.PhobiaCount(def.id) > 0)
                    unlocked++;
            }
        }

        if (_progressText != null)
            _progressText.text = $"{unlocked} / {total} unlocked";

        foreach (var row in _rows)
            RefreshRow(row, catalog);

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(_rows.Count == 0);

        RefreshMissionUi();
        RefreshTab();
    }

    void RefreshRow(Row row, ClickerCatalog catalog)
    {
        var def = catalog != null ? catalog.PhobiaById(row.id) : null;
        var count = def != null ? game.State.PhobiaCount(def.id) : 0;
        var found = count > 0;
        var selected = found && row.id == _selectedId;
        var color = def != null ? RarityStyle.Color(def.rarity) : Color.white;
        var mission = game.State.MissionFor(row.id);

        if (row.background != null)
        {
            if (selected)
                row.background.color = new Color(color.r, color.g, color.b, 0.42f);
            else if (found)
                row.background.color = new Color(color.r, color.g, color.b, 0.18f);
            else
                row.background.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
        }

        if (row.nameText != null)
        {
            row.nameText.text = found && def != null ? def.Label : "????";
            row.nameText.color = found ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (row.rarityText != null)
        {
            if (mission != null && mission.IsComplete)
            {
                row.rarityText.text = "READY TO CLAIM";
                row.rarityText.color = Accent;
            }
            else if (mission != null)
            {
                row.rarityText.text = $"GATHERING  {MissionRules.FormatRemaining(mission.Remaining)}";
                row.rarityText.color = color;
            }
            else
            {
                row.rarityText.text = found && def != null ? RarityStyle.Label(def.rarity) : "LOCKED";
                row.rarityText.color = found ? color : new Color(1f, 1f, 1f, 0.28f);
            }
        }

        if (row.countText != null)
        {
            row.countText.text = found ? $"×{count}" : "";
            row.countText.color = found ? color : Color.clear;
        }

        if (row.art != null)
        {
            var art = found && def != null ? def.art : null;
            row.art.texture = art;
            row.art.color = art != null ? Color.white : new Color(0.2f, 0.18f, 0.24f, 1f);
        }

        if (row.inspectButton != null)
            row.inspectButton.interactable = found;
        if (row.inspectImage != null)
            row.inspectImage.color = found ? Accent : new Color(0.3f, 0.28f, 0.34f, 1f);
        if (row.inspectLabel != null)
            row.inspectLabel.color = found ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    void RefreshMissionUi()
    {
        if (_detailText == null || _actionButton == null || game == null)
            return;

        var def = game.Catalog != null ? game.Catalog.PhobiaById(_selectedId) : null;
        var owned = def != null && game.State.PhobiaCount(_selectedId) > 0;
        if (!owned)
            _selectedId = null;

        var selectedMission = game.State.MissionFor(_selectedId);
        var slotFull = game.State.Missions.Count >= MissionRules.MaxConcurrent;

        if (def == null || !owned)
        {
            _detailText.text = "Select an unlocked phobia to send gathering.";
            SetAction("SEND", false);
            return;
        }

        var reward = selectedMission != null
            ? MissionRules.Reward((Rarity)selectedMission.rarity, selectedMission.collectedAtStart)
            : game.PreviewGatherReward(_selectedId);

        if (selectedMission != null && selectedMission.IsComplete)
        {
            _detailText.text = $"{def.Label} is back.\n+{Mathf.FloorToInt((float)reward)} Courage";
            SetAction("CLAIM", true);
            return;
        }

        if (selectedMission != null)
        {
            _detailText.text = $"{def.Label} is gathering.\n{MissionRules.FormatRemaining(selectedMission.Remaining)} left";
            SetAction("WAIT", false);
            return;
        }

        if (slotFull)
        {
            _detailText.text = "A phobia is already gathering.\nWait or claim it first.";
            SetAction("BUSY", false);
            return;
        }

        var seconds = (int)MissionRules.GatherDuration.TotalSeconds;
        _detailText.text = $"{def.Label}  ·  {RarityStyle.Label(def.rarity)} ×{game.State.PhobiaCount(def.id)}\nEst. {Mathf.FloorToInt((float)reward)} Courage  ·  {seconds}s";
        SetAction("SEND", true);
    }

    void RefreshTab()
    {
        if (_collectionTab == null || game == null)
            return;

        if (game.State.FirstCompletableMission() != null)
            _collectionTab.text = "CLAIM!";
        else if (game.State.Missions.Count > 0)
            _collectionTab.text = "GATHERING";
        else
            _collectionTab.text = "COLLECTION";
    }

    void SetAction(string label, bool enabled)
    {
        if (_actionLabel != null)
            _actionLabel.text = label;
        if (_actionButton != null)
            _actionButton.interactable = enabled;
        if (_actionImage != null)
            _actionImage.color = enabled ? Accent : new Color(0.3f, 0.28f, 0.34f, 1f);
    }

    void OnActionPressed()
    {
        if (game == null || string.IsNullOrEmpty(_selectedId))
            return;

        var mission = game.State.MissionFor(_selectedId);
        if (mission != null && mission.IsComplete)
        {
            game.TryClaimMission(_selectedId);
            return;
        }

        game.TryStartGather(_selectedId);
    }

    void Select(string id)
    {
        if (game == null || game.State.PhobiaCount(id) <= 0)
            return;
        _selectedId = id;
        Refresh();
    }

    void Inspect(string id)
    {
        if (game == null || game.State.PhobiaCount(id) <= 0)
            return;
        var def = game.Catalog != null ? game.Catalog.PhobiaById(id) : null;
        if (_hud != null)
            _hud.ShowInspect(def);
    }

    void Build()
    {
        WireInventoryButton();
        BuildOverlay();
        BuildRows();
    }

    void WireInventoryButton()
    {
        var inventory = transform.Find("Inventory");
        if (inventory == null)
            return;

        if (inventory.TryGetComponent<Image>(out var image))
            image.raycastTarget = true;

        var button = inventory.GetComponent<Button>();
        if (button == null)
            button = inventory.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Open);

        var label = inventory.Find("RelicsLabel");
        if (label != null && label.TryGetComponent<Text>(out var text))
        {
            _collectionTab = text;
            text.text = "COLLECTION";
            text.raycastTarget = false;
        }
    }

    void BuildOverlay()
    {
        var overlay = Create("CollectionOverlay", _canvas);
        Stretch(overlay);
        overlay.SetAsLastSibling();
        var dim = overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.06f, 0.92f);
        dim.raycastTarget = true;
        _overlay = overlay.gameObject;
        _overlay.SetActive(false);

        var panel = Create("Panel", overlay);
        panel.anchorMin = new Vector2(0.06f, 0.08f);
        panel.anchorMax = new Vector2(0.94f, 0.9f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        var panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.06f, 0.12f, 0.98f);
        panelImage.raycastTarget = true;

        var title = CreateText("Title", panel, "COLLECTION", 36, TextAnchor.MiddleCenter, FontStyle.Bold);
        title.color = Color.white;
        Stretch(title.rectTransform, new Vector2(0.08f, 0.91f), new Vector2(0.78f, 1f));

        _progressText = CreateText("Progress", panel, "0 / 0 unlocked", 22, TextAnchor.MiddleLeft);
        _progressText.color = new Color(0.7f, 0.68f, 0.76f, 1f);
        Stretch(_progressText.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.7f, 0.92f));

        var close = Create("Close", panel);
        Stretch(close, new Vector2(0.78f, 0.91f), new Vector2(0.98f, 0.99f), new Vector2(-8f, -8f), new Vector2(-8f, -8f));
        var closeImage = close.gameObject.AddComponent<Image>();
        closeImage.color = Accent;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);
        var closeLabel = CreateText("Label", close, "CLOSE", 22, TextAnchor.MiddleCenter, FontStyle.Bold);
        closeLabel.color = Color.white;
        closeLabel.raycastTarget = false;
        Stretch(closeLabel.rectTransform);

        var viewport = Create("Viewport", panel);
        Stretch(viewport, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.85f));
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        _content = Create("Content", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(0f, 0f);
        var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = _content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        _emptyText = CreateText("Empty", viewport, "No phobias in the catalog yet.", 24, TextAnchor.MiddleCenter);
        _emptyText.color = new Color(1f, 1f, 1f, 0.4f);
        Stretch(_emptyText.rectTransform);
        _emptyText.raycastTarget = false;

        var action = Create("MissionBar", panel);
        Stretch(action, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.22f));
        var actionBg = action.gameObject.AddComponent<Image>();
        actionBg.color = new Color(0.12f, 0.1f, 0.16f, 1f);

        _detailText = CreateText("Detail", action, "Select an unlocked phobia to send gathering.", 20, TextAnchor.MiddleLeft);
        _detailText.color = new Color(0.86f, 0.84f, 0.9f, 1f);
        _detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailText.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(_detailText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.62f, 0.92f));

        var send = Create("Action", action);
        Stretch(send, new Vector2(0.66f, 0.16f), new Vector2(0.97f, 0.84f));
        _actionImage = send.gameObject.AddComponent<Image>();
        _actionImage.color = Accent;
        _actionButton = send.gameObject.AddComponent<Button>();
        _actionButton.targetGraphic = _actionImage;
        _actionButton.onClick.AddListener(OnActionPressed);
        _actionLabel = CreateText("Label", send, "SEND", 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        _actionLabel.color = Color.white;
        _actionLabel.raycastTarget = false;
        Stretch(_actionLabel.rectTransform);
    }

    void BuildRows()
    {
        var catalog = game != null ? game.Catalog : null;
        if (catalog == null || catalog.phobias == null || _content == null)
            return;

        var defs = new List<PhobiaDefinition>(catalog.phobias);
        defs.Sort((a, b) =>
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;
            var rarity = b.rarity.CompareTo(a.rarity);
            if (rarity != 0)
                return rarity;
            return string.CompareOrdinal(a.Label, b.Label);
        });

        foreach (var def in defs)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;
            _rows.Add(CreateRow(def));
        }
    }

    Row CreateRow(PhobiaDefinition def)
    {
        var row = Create($"Row_{def.id}", _content);
        var layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 92f;
        layout.preferredHeight = 92f;
        var bg = row.gameObject.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
        bg.raycastTarget = false;
        var id = def.id;

        var select = Create("Select", row);
        Stretch(select, new Vector2(0f, 0f), new Vector2(0.7f, 1f));
        var selectImage = select.gameObject.AddComponent<Image>();
        selectImage.color = Color.clear;
        var selectButton = select.gameObject.AddComponent<Button>();
        selectButton.transition = Selectable.Transition.None;
        selectButton.targetGraphic = selectImage;
        selectButton.onClick.AddListener(() => Select(id));

        var artRt = Create("Art", row);
        Stretch(artRt, new Vector2(0.03f, 0.12f), new Vector2(0.18f, 0.88f));
        var art = artRt.gameObject.AddComponent<RawImage>();
        art.color = new Color(0.2f, 0.18f, 0.24f, 1f);
        art.raycastTarget = false;

        var name = CreateText("Name", row, def.Label, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        Stretch(name.rectTransform, new Vector2(0.19f, 0.46f), new Vector2(0.56f, 0.9f));
        name.raycastTarget = false;

        var rarity = CreateText("Rarity", row, RarityStyle.Label(def.rarity), 18, TextAnchor.MiddleLeft);
        Stretch(rarity.rectTransform, new Vector2(0.19f, 0.1f), new Vector2(0.56f, 0.5f));
        rarity.raycastTarget = false;

        var count = CreateText("Count", row, "×0", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
        Stretch(count.rectTransform, new Vector2(0.56f, 0.12f), new Vector2(0.7f, 0.88f));
        count.raycastTarget = false;

        var inspect = Create("Inspect", row);
        Stretch(inspect, new Vector2(0.72f, 0.18f), new Vector2(0.97f, 0.82f));
        var inspectImage = inspect.gameObject.AddComponent<Image>();
        inspectImage.color = Accent;
        var inspectButton = inspect.gameObject.AddComponent<Button>();
        inspectButton.targetGraphic = inspectImage;
        inspectButton.transition = Selectable.Transition.None;
        inspectButton.onClick.AddListener(() => Inspect(id));
        var inspectLabel = CreateText("Label", inspect, "INSPECT", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
        inspectLabel.color = Color.white;
        inspectLabel.raycastTarget = false;
        Stretch(inspectLabel.rectTransform);

        return new Row
        {
            id = def.id,
            background = bg,
            art = art,
            nameText = name,
            rarityText = rarity,
            countText = count,
            inspectButton = inspectButton,
            inspectImage = inspectImage,
            inspectLabel = inspectLabel
        };
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
        Stretch(rt, min, max, Vector2.zero, Vector2.zero);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
