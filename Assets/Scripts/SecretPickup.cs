using UnityEngine;
using UnityEngine.UI;

public class SecretPickup : MonoBehaviour
{
    [SerializeField] ClickerGame game;
    [SerializeField] string phobiaId;
    [SerializeField] Button button;

    void Awake()
    {
        if (game == null)
            game = FindAnyObjectByType<ClickerGame>();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(Collect);
    }

    void Start()
    {
        Refresh();
    }

    public void Collect()
    {
        if (game != null)
            game.TryCollectSecret(phobiaId);
    }

    public void Refresh()
    {
        if (game == null || string.IsNullOrEmpty(phobiaId))
            return;

        var owned = game.State.PhobiaCount(phobiaId) > 0;
        gameObject.SetActive(!owned);
    }

    public static void RefreshAll(ClickerGame game)
    {
        if (game == null)
            return;

        var pickups = FindObjectsByType<SecretPickup>(FindObjectsInactive.Include);
        for (var i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] != null)
                pickups[i].Refresh();
        }
    }
}
