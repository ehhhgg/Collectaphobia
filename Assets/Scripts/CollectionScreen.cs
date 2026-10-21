using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionScreen : MonoBehaviour
{
    [SerializeField] ClickerGame game;
    [Header("Scene UI")]
    [SerializeField] GameObject overlay;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] Text collectionTab;
    [SerializeField] Text progressText;
    [SerializeField] RectTransform content;
    [SerializeField] Text emptyText;
    [SerializeField] Text detailText;
    [SerializeField] Text actionLabel;
    [SerializeField] Button actionButton;
    [SerializeField] Image actionImage;

    readonly List<CollectionRow> _rows = new();
    string _selectedId;
    bool _open;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    void Awake()
    {
        if (game == null)
            game = GetComponentInParent<ClickerGame>();
        ResolveSceneRefs();
    }

    void Start()
    {
        BindChrome();
        CacheRows();
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
        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }

        Refresh();
    }

    public void Close()
    {
        _open = false;
        if (overlay != null)
            overlay.SetActive(false);
        RefreshTab();
    }

    public void Select(string id)
    {
        if (game == null || game.State.PhobiaCount(id) <= 0)
            return;
        _selectedId = id;
        Refresh();
    }

    public void Inspect(string id)
    {
        if (game == null || game.State.PhobiaCount(id) <= 0)
            return;
        var def = game.Catalog != null ? game.Catalog.PhobiaById(id) : null;
        var hud = GetComponent<ClickerHud>();
        if (hud != null)
            hud.ShowInspect(def);
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

        if (progressText != null)
            progressText.text = $"{unlocked} / {total} unlocked";

        foreach (var row in _rows)
            RefreshRow(row, catalog);

        if (emptyText != null)
            emptyText.gameObject.SetActive(total == 0);

        RefreshMissionUi();
        RefreshTab();
    }

    void RefreshRow(CollectionRow row, ClickerCatalog catalog)
    {
        if (row == null)
            return;

        var def = catalog != null ? catalog.PhobiaById(row.phobiaId) : null;
        var count = def != null ? game.State.PhobiaCount(def.id) : 0;
        var found = count > 0;
        var selected = found && row.phobiaId == _selectedId;
        var color = def != null ? RarityStyle.Color(def.rarity) : Color.white;
        var mission = game.State.MissionFor(row.phobiaId);

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
        if (detailText == null || actionButton == null || game == null)
            return;

        var def = game.Catalog != null ? game.Catalog.PhobiaById(_selectedId) : null;
        var owned = def != null && game.State.PhobiaCount(_selectedId) > 0;
        if (!owned)
            _selectedId = null;

        var selectedMission = game.State.MissionFor(_selectedId);
        var slotFull = game.State.Missions.Count >= MissionRules.MaxConcurrent;

        if (def == null || !owned)
        {
            detailText.text = "Select an unlocked phobia to send gathering.";
            SetAction("SEND", false);
            return;
        }

        var reward = selectedMission != null
            ? MissionRules.Reward((Rarity)selectedMission.rarity, selectedMission.collectedAtStart)
            : game.PreviewGatherReward(_selectedId);

        if (selectedMission != null && selectedMission.IsComplete)
        {
            detailText.text = $"{def.Label} is back.\n+{Mathf.FloorToInt((float)reward)} Courage";
            SetAction("CLAIM", true);
            return;
        }

        if (selectedMission != null)
        {
            detailText.text = $"{def.Label} is gathering.\n{MissionRules.FormatRemaining(selectedMission.Remaining)} left";
            SetAction("WAIT", false);
            return;
        }

        if (slotFull)
        {
            detailText.text = "A phobia is already gathering.\nWait or claim it first.";
            SetAction("BUSY", false);
            return;
        }

        var seconds = (int)MissionRules.GatherDuration.TotalSeconds;
        detailText.text = $"{def.Label}  ·  {RarityStyle.Label(def.rarity)} ×{game.State.PhobiaCount(def.id)}\nEst. {Mathf.FloorToInt((float)reward)} Courage  ·  {seconds}s";
        SetAction("SEND", true);
    }

    void RefreshTab()
    {
        if (collectionTab == null || game == null)
            return;

        if (game.State.FirstCompletableMission() != null)
            collectionTab.text = "CLAIM!";
        else if (game.State.Missions.Count > 0)
            collectionTab.text = "GATHERING";
        else
            collectionTab.text = "COLLECTION";
    }

    void SetAction(string label, bool enabled)
    {
        if (actionLabel != null)
            actionLabel.text = label;
        if (actionButton != null)
            actionButton.interactable = enabled;
        if (actionImage != null)
            actionImage.color = enabled ? Accent : new Color(0.3f, 0.28f, 0.34f, 1f);
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

    void BindChrome()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionPressed);

        if (overlay == null)
            Debug.LogError("CollectionScreen has no overlay in the scene. Run Phobia/Sync Collection Screen.", this);
    }

    void CacheRows()
    {
        _rows.Clear();
        if (content == null)
            return;
        content.GetComponentsInChildren(true, _rows);
    }

    void ResolveSceneRefs()
    {
        if (overlay == null)
        {
            var found = transform.Find("CollectionOverlay");
            if (found != null)
                overlay = found.gameObject;
        }

        if (openButton == null)
        {
            var inventory = transform.Find("Inventory");
            if (inventory != null)
                openButton = inventory.GetComponent<Button>();
        }

        if (collectionTab == null)
        {
            var label = transform.Find("Inventory/RelicsLabel");
            if (label != null)
                collectionTab = label.GetComponent<Text>();
        }

        if (overlay == null)
            return;

        var overlayTransform = overlay.transform;
        if (closeButton == null)
            closeButton = FindNamed<Button>(overlayTransform, "Panel/Close");
        if (progressText == null)
            progressText = FindNamed<Text>(overlayTransform, "Panel/Progress");
        if (content == null)
        {
            var contentTransform = overlayTransform.Find("Panel/Viewport/Content");
            if (contentTransform != null)
                content = contentTransform as RectTransform;
        }

        if (emptyText == null)
            emptyText = FindNamed<Text>(overlayTransform, "Panel/Viewport/Empty");
        if (detailText == null)
            detailText = FindNamed<Text>(overlayTransform, "Panel/MissionBar/Detail");
        if (actionButton == null)
            actionButton = FindNamed<Button>(overlayTransform, "Panel/MissionBar/Action");
        if (actionImage == null && actionButton != null)
            actionImage = actionButton.GetComponent<Image>();
        if (actionLabel == null)
            actionLabel = FindNamed<Text>(overlayTransform, "Panel/MissionBar/Action/Label");
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
