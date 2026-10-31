using UnityEngine;
using UnityEngine.UI;

public interface IPhobiaRowHost
{
    void Select(string id);
    void Inspect(string id);
}

public class CollectionRow : MonoBehaviour
{
    public string phobiaId;
    public Image background;
    public RawImage art;
    public Text nameText;
    public Text rarityText;
    public Text countText;
    public Button selectButton;
    public Button inspectButton;
    public Image inspectImage;
    public Text inspectLabel;

    IPhobiaRowHost _host;

    void Awake()
    {
        _host = FindHost();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectPressed);
        if (inspectButton != null)
            inspectButton.onClick.AddListener(OnInspectPressed);
    }

    public void OnSelectPressed()
    {
        _host?.Select(phobiaId);
    }

    public void OnInspectPressed()
    {
        _host?.Inspect(phobiaId);
    }

    public void Refresh(ClickerGame game, string selectedId)
    {
        if (game == null)
            return;

        var catalog = game.Catalog;
        var def = catalog != null ? catalog.PhobiaById(phobiaId) : null;
        var count = def != null ? game.State.PhobiaCount(def.id) : 0;
        var found = count > 0;
        var selected = found && phobiaId == selectedId;
        var color = def != null ? RarityStyle.Color(def.rarity) : Color.white;
        var mission = game.State.MissionFor(phobiaId);
        var accent = new Color(0.95f, 0.42f, 0.55f);

        if (background != null)
        {
            if (selected)
                background.color = new Color(color.r, color.g, color.b, 0.42f);
            else if (found)
                background.color = new Color(color.r, color.g, color.b, 0.18f);
            else
                background.color = new Color(0.12f, 0.1f, 0.16f, 0.9f);
        }

        if (nameText != null)
        {
            nameText.text = found && def != null ? def.Label : "????";
            nameText.color = found ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (rarityText != null)
        {
            if (mission != null && mission.IsComplete)
            {
                rarityText.text = "READY TO CLAIM";
                rarityText.color = accent;
            }
            else if (mission != null)
            {
                var job = MissionRules.JobById(mission.missionId);
                rarityText.text = $"{job.displayName.ToUpper()}  {MissionRules.FormatRemaining(mission.Remaining)}";
                rarityText.color = color;
            }
            else
            {
                rarityText.text = found && def != null ? RarityStyle.Label(def.rarity) : "LOCKED";
                rarityText.color = found ? color : new Color(1f, 1f, 1f, 0.28f);
            }
        }

        if (countText != null)
        {
            countText.text = found ? $"×{count}" : "";
            countText.color = found ? color : Color.clear;
        }

        if (art != null)
        {
            var portrait = found && def != null ? def.art : null;
            art.texture = portrait;
            art.color = portrait != null ? Color.white : new Color(0.2f, 0.18f, 0.24f, 1f);
        }

        if (inspectButton != null)
            inspectButton.interactable = found;
        if (inspectImage != null)
            inspectImage.color = found ? accent : new Color(0.3f, 0.28f, 0.34f, 1f);
        if (inspectLabel != null)
            inspectLabel.color = found ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }

    IPhobiaRowHost FindHost()
    {
        var behaviours = GetComponentsInParent<MonoBehaviour>(true);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPhobiaRowHost host)
                return host;
        }

        return null;
    }
}
