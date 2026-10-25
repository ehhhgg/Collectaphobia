using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkScreen : MonoBehaviour, IPhobiaRowHost
{
    [SerializeField] ClickerGame game;
    [Header("Scene UI")]
    [SerializeField] GameObject overlay;
    [SerializeField] Button closeButton;
    [SerializeField] Text statusText;
    [SerializeField] RectTransform content;
    [SerializeField] Text emptyText;
    [SerializeField] WorkJobOption[] jobs;

    readonly List<CollectionRow> _rows = new();
    string _selectedId;
    bool _open;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    public bool IsOpen => _open;

    void Awake()
    {
        if (game == null)
            game = GetComponentInParent<ClickerGame>();
        ResolveSceneRefs();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void Start()
    {
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
            RefreshJobs();
    }

    public void Open(string selectedId = null)
    {
        _open = true;
        if (!string.IsNullOrEmpty(selectedId) && game != null && game.State.PhobiaCount(selectedId) > 0)
            _selectedId = selectedId;
        if (overlay != null)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
        }

        CacheRows();
        Refresh();
    }

    public void Close()
    {
        _open = false;
        if (overlay != null)
            overlay.SetActive(false);
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
        var hud = GetComponentInParent<ClickerHud>();
        if (hud != null)
            hud.ShowInspect(def);
    }

    public void OnJobPressed(string jobId)
    {
        if (game == null)
            return;

        var jobMission = game.State.MissionForJob(jobId);
        if (jobMission != null && jobMission.IsComplete)
        {
            game.TryClaimMission(jobMission.phobiaId);
            return;
        }

        game.TryStartGather(_selectedId, jobId);
    }

    void Refresh()
    {
        if (game == null)
            return;

        var catalog = game.Catalog;
        foreach (var row in _rows)
            RefreshRow(row, catalog);

        if (emptyText != null)
        {
            var total = catalog != null && catalog.phobias != null ? catalog.phobias.Length : 0;
            emptyText.gameObject.SetActive(total == 0);
        }

        RefreshJobs();
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

    void RefreshJobs()
    {
        if (game == null)
            return;

        var def = game.Catalog != null ? game.Catalog.PhobiaById(_selectedId) : null;
        var owned = def != null && game.State.PhobiaCount(_selectedId) > 0;
        if (!owned)
            _selectedId = null;

        var selectedMission = game.State.MissionFor(_selectedId);
        var catalog = game.Catalog;

        if (statusText != null)
        {
            if (def == null || !owned)
                statusText.text = "Select an unlocked phobia, then pick a job.";
            else if (selectedMission != null && selectedMission.IsComplete)
                statusText.text = $"{def.Label} is back.\nClaim +{Mathf.FloorToInt((float)MissionRules.Reward((Rarity)selectedMission.rarity, selectedMission.collectedAtStart, selectedMission.missionId))} Courage";
            else if (selectedMission != null)
                statusText.text = $"{def.Label} is on {MissionRules.JobById(selectedMission.missionId).displayName}.\n{MissionRules.FormatRemaining(selectedMission.Remaining)} left";
            else
                statusText.text = $"{def.Label}  ·  {RarityStyle.Label(def.rarity)} ×{game.State.PhobiaCount(def.id)}\nPick a free job below.";
        }

        if (jobs == null)
            return;

        foreach (var option in jobs)
        {
            if (option == null)
                continue;

            var job = MissionRules.JobById(option.jobId);
            var jobMission = game.State.MissionForJob(job.id);
            var worker = jobMission != null && catalog != null ? catalog.PhobiaById(jobMission.phobiaId) : null;
            var complete = jobMission != null && jobMission.IsComplete;
            var canSend = owned && selectedMission == null && jobMission == null;
            var reward = owned
                ? game.PreviewGatherReward(_selectedId, job.id)
                : MissionRules.Reward(Rarity.Common, 1, job.id);

            if (option.titleText != null)
                option.titleText.text = job.displayName;
            if (option.durationText != null)
            {
                if (jobMission != null)
                    option.durationText.text = complete ? "DONE" : MissionRules.FormatRemaining(jobMission.Remaining);
                else
                    option.durationText.text = job.DurationLabel;
            }

            if (option.rewardText != null)
            {
                if (worker != null)
                    option.rewardText.text = worker.Label;
                else
                    option.rewardText.text = owned ? $"+{Mathf.FloorToInt((float)reward)}" : $"+{Mathf.FloorToInt((float)reward)} base";
            }

            if (option.sendLabel != null)
                option.sendLabel.text = complete ? "CLAIM" : "SEND";
            if (option.sendButton != null)
                option.sendButton.interactable = complete || canSend;
            if (option.sendImage != null)
                option.sendImage.color = complete || canSend ? Accent : new Color(0.3f, 0.28f, 0.34f, 1f);
            if (option.background != null)
            {
                option.background.color = jobMission != null
                    ? new Color(Accent.r, Accent.g, Accent.b, 0.28f)
                    : new Color(0.12f, 0.1f, 0.16f, 1f);
            }
        }
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
            overlay = gameObject;

        var root = overlay.transform;
        if (closeButton == null)
            closeButton = FindNamed<Button>(root, "Panel/Close");
        if (statusText == null)
            statusText = FindNamed<Text>(root, "Panel/Status");
        if (content == null)
        {
            var contentTransform = root.Find("Panel/Viewport/Content");
            if (contentTransform != null)
                content = contentTransform as RectTransform;
        }

        if (emptyText == null)
            emptyText = FindNamed<Text>(root, "Panel/Viewport/Empty");
        if (jobs == null || jobs.Length == 0)
            jobs = GetComponentsInChildren<WorkJobOption>(true);
    }

    static T FindNamed<T>(Transform root, string path) where T : Component
    {
        var found = root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
