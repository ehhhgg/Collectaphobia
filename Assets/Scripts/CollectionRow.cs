using UnityEngine;
using UnityEngine.UI;

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

    CollectionScreen _screen;

    void Awake()
    {
        _screen = GetComponentInParent<CollectionScreen>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectPressed);
        if (inspectButton != null)
            inspectButton.onClick.AddListener(OnInspectPressed);
    }

    public void OnSelectPressed()
    {
        if (_screen != null)
            _screen.Select(phobiaId);
    }

    public void OnInspectPressed()
    {
        if (_screen != null)
            _screen.Inspect(phobiaId);
    }
}
