using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClickerHud : MonoBehaviour
{
    [Serializable]
    public class RarityCountLabel
    {
        public Rarity rarity;
        public Text label;
    }

    [SerializeField] ClickerGame game;
    [SerializeField] RectTransform canvasRect;
    [SerializeField] Text courageText;
    [SerializeField] Text cpsText;
    [SerializeField] Text relicEtaText;
    [SerializeField] Text toastText;
    [SerializeField] Text hintText;
    [SerializeField] Image relicFill;
    [SerializeField] Button clickCatcher;
    [SerializeField] Text clickPopupTemplate;
    [SerializeField] AutoClickerShopRow[] shopRows;
    [SerializeField] RarityCountLabel[] rarityCounts;
    [SerializeField] PhobiaReveal reveal;

    static readonly Color Accent = new(0.95f, 0.42f, 0.55f);

    Coroutine _toastRoutine;
    bool _hintHidden;

    void Start()
    {
        if (clickCatcher != null)
            clickCatcher.onClick.AddListener(game.ManualClick);

        if (shopRows != null)
        {
            foreach (var row in shopRows)
            {
                if (row == null || row.buyButton == null)
                    continue;
                var id = row.autoId;
                row.buyButton.onClick.AddListener(() => game.TryBuy(id));
            }
        }

        if (clickPopupTemplate != null)
            clickPopupTemplate.gameObject.SetActive(false);

        if (reveal == null)
            reveal = GetComponent<PhobiaReveal>();

        game.StateChanged += Refresh;
        Refresh();
    }

    public void SpawnClickPopup()
    {
        if (clickPopupTemplate == null || canvasRect == null)
            return;

        var popup = Instantiate(clickPopupTemplate, canvasRect);
        popup.gameObject.SetActive(true);
        popup.color = Accent;

        if (UnityEngine.InputSystem.Pointer.current != null)
        {
            var screen = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screen,
                popup.canvas.worldCamera,
                out var local);
            popup.rectTransform.anchoredPosition = local + Vector2.up * 40f;
        }

        StartCoroutine(FloatAndFade(popup));
    }

    public bool IsRevealOpen => reveal != null && reveal.IsOpen;

    public void ShowFirstUnlock(PhobiaDefinition def)
    {
        HideHint();
        if (reveal != null)
            reveal.Show(def);
        RefreshInventory();
    }

    public void ShowRelicToast(PhobiaDefinition def)
    {
        HideHint();
        if (def == null)
            return;
        ShowToast($"{def.Label}  ·  {RarityStyle.Label(def.rarity)}", RarityStyle.Color(def.rarity));
        RefreshInventory();
    }

    public void ShowOfflineGain(double amount, TimeSpan elapsed)
    {
        if (amount < 1d)
            return;
        ShowToast($"Away {FormatDuration(elapsed)}  ·  +{Format(amount)} Courage", Accent, 2.6f);
    }

    public void ShowToast(string message, Color color, float hold = 1.8f)
    {
        if (toastText == null)
            return;
        if (_toastRoutine != null)
            StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(message, color, hold));
    }

    void Refresh()
    {
        if (courageText == null || game == null)
            return;

        HideHint();
        courageText.text = $"Courage  {Format(game.State.Courage)}";
        cpsText.text = $"{game.State.ClicksPerSecond(game.Catalog):0.0} / sec";
        relicEtaText.text = game.PendingCount > 0
            ? $"Ball  {game.PendingCount}"
            : $"Relic in {game.State.ClicksUntilRelic}";
        if (relicFill != null)
            relicFill.fillAmount = Mathf.Clamp01(1f - game.State.ClicksUntilRelic / 15f);
        RefreshBallHint();

        if (shopRows != null && game.Catalog != null)
        {
            foreach (var row in shopRows)
            {
                if (row == null)
                    continue;
                var def = game.Catalog.AutoById(row.autoId);
                if (def == null)
                    continue;

                var owned = game.State.AutoCount(def.id);
                var cost = GameState.CostFor(def, owned);
                if (row.costLabel != null)
                    row.costLabel.text = owned > 0 ? $"{Format(cost)}\n×{owned}" : Format(cost);
                if (row.buyButton != null)
                    row.buyButton.interactable = game.State.Courage >= cost;
            }
        }

        RefreshInventory();
    }

    void RefreshInventory()
    {
        if (rarityCounts == null)
            return;

        foreach (var binding in rarityCounts)
        {
            if (binding == null || binding.label == null)
                continue;
            var count = game.State.RelicCount(binding.rarity);
            binding.label.text = $"{ShortLabel(binding.rarity)} {count}";
            var color = RarityStyle.Color(binding.rarity);
            binding.label.color = count > 0 ? color : new Color(color.r, color.g, color.b, 0.35f);
        }
    }

    void RefreshBallHint()
    {
        if (hintText == null)
            return;

        if (game.PendingCount > 0)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = game.PendingCount == 1 ? "click a ball" : $"click a ball  ×{game.PendingCount}";
            hintText.color = new Color(1f, 1f, 1f, 0.7f);
            return;
        }

        if (_hintHidden)
            hintText.gameObject.SetActive(false);
    }

    void HideHint()
    {
        if (_hintHidden || hintText == null || game.State.TotalClicks <= 0)
            return;
        _hintHidden = true;
        hintText.gameObject.SetActive(false);
    }

    IEnumerator ToastRoutine(string message, Color color, float hold)
    {
        toastText.text = message;
        toastText.color = color;
        yield return new WaitForSeconds(hold);
        var t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            toastText.color = Color.Lerp(color, Color.clear, t / 0.4f);
            yield return null;
        }

        toastText.color = Color.clear;
        _toastRoutine = null;
    }

    IEnumerator FloatAndFade(Text label)
    {
        var t = 0f;
        var start = label.rectTransform.anchoredPosition;
        var color = label.color;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            var u = t / 0.6f;
            label.rectTransform.anchoredPosition = start + Vector2.up * (80f * u);
            color.a = 1f - u;
            label.color = color;
            yield return null;
        }

        Destroy(label.gameObject);
    }

    static string Format(double value)
    {
        if (value < 1000d)
            return Mathf.FloorToInt((float)value).ToString();
        if (value < 1_000_000d)
            return $"{value / 1000d:0.0}K";
        return $"{value / 1_000_000d:0.00}M";
    }

    static string FormatDuration(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1d)
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
        if (elapsed.TotalMinutes >= 1d)
            return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
        return $"{Mathf.Max(1, elapsed.Seconds)}s";
    }

    static string ShortLabel(Rarity rarity) => rarity switch
    {
        Rarity.Common => "C",
        Rarity.Uncommon => "U",
        Rarity.Rare => "R",
        Rarity.Epic => "E",
        Rarity.Legendary => "L",
        _ => "?"
    };
}
