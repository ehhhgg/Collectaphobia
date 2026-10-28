using UnityEngine;
using UnityEngine.UI;

public class WorkJobOption : MonoBehaviour
{
    public string jobId;
    public Text titleText;
    public Text durationText;
    public Text rewardText;
    public Button sendButton;
    public Image background;
    public Image sendImage;
    public Text sendLabel;

    WorkScreen _screen;

    void Awake()
    {
        _screen = GetComponentInParent<WorkScreen>(true);
        if (sendButton != null)
            sendButton.onClick.AddListener(OnPressed);
    }

    public void OnPressed()
    {
        if (_screen != null)
            _screen.OnJobPressed(jobId);
    }
}
