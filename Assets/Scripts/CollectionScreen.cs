using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionScreen : MonoBehaviour, IPhobiaRowHost
{
    [SerializeField] ClickerGame game;
    [Header("Scene UI")]
    [SerializeField] GameObject overlay;
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button workButton;
    [SerializeField] Button mergeButton;
    [SerializeField] Image mergeImage;
    [SerializeField] Text mergeLabel;
    [SerializeField] WorkScreen workScreen;
    [SerializeField] GameObject mergeConfirm;
    [SerializeField] Text mergeConfirmMessage;
    [SerializeField] Button mergeConfirmButton;
    [SerializeField] Button mergeCancelButton;
    [SerializeField] Text collectionTab;
    [SerializeField] Text progressText;
    [SerializeField] RectTransform content;
    [SerializeField] Text emptyText;

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
        {
            RefreshRows();
            RefreshMerge();
        }

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
        if (workScreen != null)
            workScreen.Close();
        HideMergeConfirm();
        if (overlay != null)
            overlay.SetActive(false);
        RefreshTab();
    }

    public void OpenWork()
    {
        if (!_open)
            Open();
        if (workScreen != null)
            workScreen.Open(_selectedId);
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

        if (!string.IsNullOrEmpty(_selectedId) && game.State.PhobiaCount(_selectedId) <= 0)
            _selectedId = null;

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

        RefreshRows();
        RefreshMerge();

        if (emptyText != null)
            emptyText.gameObject.SetActive(total == 0);

        RefreshTab();
    }

    void RefreshRows()
    {
        var catalog = game != null ? game.Catalog : null;
        foreach (var row in _rows)
            RefreshRow(row, catalog);
    }

    void RefreshRow(CollectionRow row, ClickerCatalog catalog)
    {
        if (row == null || game == null)
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
                var job = MissionRules.JobById(mission.missionId);
                row.rarityText.text = $"{job.displayName.ToUpper()}  {MissionRules.FormatRemaining(mission.Remaining)}";
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

    void RefreshTab()
    {
        if (collectionTab == null || game == null)
            return;

        if (game.State.FirstCompletableMission() != null)
            collectionTab.text = "CLAIM!";
        else if (game.State.Missions.Count > 0)
            collectionTab.text = "WORKING";
        else
            collectionTab.text = "COLLECTION";
    }

    void BindChrome()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (workButton != null)
            workButton.onClick.AddListener(OpenWork);
        if (mergeButton != null)
            mergeButton.onClick.AddListener(ShowMergeConfirm);
        if (mergeConfirmButton != null)
            mergeConfirmButton.onClick.AddListener(ConfirmMerge);
        if (mergeCancelButton != null)
            mergeCancelButton.onClick.AddListener(HideMergeConfirm);
        HideMergeConfirm();

        if (overlay == null)
            Debug.LogError("CollectionScreen has no overlay in the scene. Run Phobia/Sync Collection Screen.", this);
    }

    public void ShowMergeConfirm()
    {
        if (game == null || !game.CanMerge(_selectedId, out var reason))
        {
            if (game != null)
                game.TryMerge(_selectedId);
            return;
        }

        var def = game.Catalog.PhobiaById(_selectedId);
        MergeRules.TryNextRarity(def.rarity, out var next);
        if (mergeConfirmMessage != null)
            mergeConfirmMessage.text = $"Merge {MergeRules.Cost} {def.Label} into a random {RarityStyle.Label(next)}?";
        if (mergeConfirm != null)
        {
            mergeConfirm.SetActive(true);
            mergeConfirm.transform.SetAsLastSibling();
        }
    }

    public void ConfirmMerge()
    {
        HideMergeConfirm();
        if (game != null)
            game.TryMerge(_selectedId);
    }

    public void HideMergeConfirm()
    {
        if (mergeConfirm != null)
            mergeConfirm.SetActive(false);
    }

    void RefreshMerge()
    {
        var can = game != null && game.CanMerge(_selectedId, out _);
        if (mergeButton != null)
            mergeButton.interactable = can;
        if (mergeImage != null)
            mergeImage.color = can ? Accent : new Color(0.3f, 0.28f, 0.34f, 1f);
        if (mergeLabel != null)
            mergeLabel.text = "MERGE";
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

        if (workScreen == null)
            workScreen = GetComponentInChildren<WorkScreen>(true);

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
        if (workButton == null)
        {
            workButton = FindNamed<Button>(overlayTransform, "Panel/Work");
            if (workButton == null)
                workButton = FindNamed<Button>(overlayTransform, "Panel/MissionBar/Action");
        }

        if (mergeButton == null)
            mergeButton = FindNamed<Button>(overlayTransform, "Panel/Merge");
        if (mergeImage == null && mergeButton != null)
            mergeImage = mergeButton.GetComponent<Image>();
        if (mergeLabel == null && mergeButton != null)
            mergeLabel = FindNamed<Text>(mergeButton.transform, "Label");
        if (mergeConfirm == null)
        {
            var confirm = overlayTransform.Find("MergeConfirm");
            if (confirm != null)
                mergeConfirm = confirm.gameObject;
        }

        if (mergeConfirm != null)
        {
            if (mergeConfirmMessage == null)
                mergeConfirmMessage = FindNamed<Text>(mergeConfirm.transform, "Panel/Message");
            if (mergeConfirmButton == null)
                mergeConfirmButton = FindNamed<Button>(mergeConfirm.transform, "Panel/Confirm");
            if (mergeCancelButton == null)
                mergeCancelButton = FindNamed<Button>(mergeConfirm.transform, "Panel/Cancel");
        }
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
