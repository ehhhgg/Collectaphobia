using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AscensionScreen : MonoBehaviour
{
    [SerializeField] AscensionSystem ascension;

    [SerializeField] GameObject root;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text statsText;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] Button ascendButton;
    [SerializeField] Button keepPlayingButton;

    void Awake()
    {
        ascendButton.onClick.AddListener(OnAscendPressed);
        keepPlayingButton.onClick.AddListener(Hide);
        root.SetActive(false);
    }

    void Start()
    {
        ascension.CollectionCompleted += Show;
    }

    void OnDestroy()
    {
        if (ascension != null)
            ascension.CollectionCompleted -= Show;
    }

    public void Show()
    {
        titleText.text = "ALL PHOBIAS CONQUERED";
        statsText.text =
            $"Total clicks: {ascension.TotalClicks:N0}\n" +
            $"Ascensions: {ascension.Count}";
        rewardText.text =
            $"Ascend to restart and earn <b>x{ascension.NextMultiplier:0.##}</b> courage permanently.\n" +
            $"(Currently x{ascension.Multiplier:0.##})\n\n" +
            "All courage and collected phobias will be reset.";

        root.SetActive(true);
    }

    void Hide() => root.SetActive(false);

    void OnAscendPressed()
    {
        ascension.Ascend();
        Hide();
    }
}