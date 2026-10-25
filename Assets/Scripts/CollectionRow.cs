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
