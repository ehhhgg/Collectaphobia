using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickerWorld : MonoBehaviour
{
    const float TargetAspect = 9f / 16f;
    const float ShelfBallScale = 0.32f;
    const int MaxOpenedOnShelf = 5;
    const int ShelfColumns = 5;

    [SerializeField] Camera playCamera;
    [SerializeField] Transform dropOrigin;
    [SerializeField] Transform shelf;
    [SerializeField] ClickerCatalog catalog;
    [SerializeField] PhobiaBall ballPrefab;

    readonly List<GameObject> _shelfItems = new();
    readonly List<PhobiaBall> _sealedBalls = new();
    int _nextSlot;

    public int PendingCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < _sealedBalls.Count; i++)
            {
                if (_sealedBalls[i] != null)
                    count++;
            }

            return count;
        }
    }

    void Update()
    {
        FitViewport();
    }

    public void ClearDrops()
    {
        StopAllCoroutines();
        for (var i = 0; i < _shelfItems.Count; i++)
        {
            if (_shelfItems[i] != null)
                Destroy(_shelfItems[i]);
        }

        _shelfItems.Clear();
        _sealedBalls.Clear();
        _nextSlot = 0;
    }

    public void StorePhobia(PhobiaDefinition def)
    {
        if (ballPrefab == null || def == null)
            return;
        StartCoroutine(BallFlight(def));
    }

    public bool TryOpenBall(out PhobiaDefinition def)
    {
        def = null;
        if (playCamera == null || Pointer.current == null)
            return false;

        var ray = playCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        PhobiaBall hit = null;
        var best = float.MaxValue;
        foreach (var ball in _sealedBalls)
        {
            if (ball == null || !ball.CanOpen)
                continue;
            if (!ball.TryHit(ray, out var distance) || distance >= best)
                continue;
            hit = ball;
            best = distance;
        }

        if (hit == null)
            return false;

        def = catalog != null ? catalog.PhobiaById(hit.ContainedId) : null;
        if (def == null && catalog != null)
            def = catalog.PhobiaOf(hit.ContainedRarity);
        StartCoroutine(OpenBall(hit, def));
        return true;
    }

    IEnumerator BallFlight(PhobiaDefinition def)
    {
        var start = dropOrigin != null ? dropOrigin.position : transform.position;
        var slot = NextShelfSlot();
        var ball = Instantiate(ballPrefab, start, Quaternion.identity, transform);
        ball.name = "PhobiaBall";
        ball.Setup(def);
        _shelfItems.Add(ball.gameObject);
        _sealedBalls.Add(ball);

        var from = ball.transform.position;
        var startScale = Vector3.one * 0.7f;
        var landScale = Vector3.one * ShelfBallScale;
        ball.transform.localScale = startScale;

        var t = 0f;
        while (t < 1f && ball != null)
        {
            t += Time.deltaTime / 0.45f;
            var u = Mathf.SmoothStep(0f, 1f, t);
            var mid = Vector3.Lerp(from, slot, u);
            mid.y += Mathf.Sin(u * Mathf.PI) * 0.8f;
            ball.transform.position = mid;
            ball.transform.localScale = Vector3.Lerp(startScale, landScale, u);
            ball.transform.Rotate(0f, 220f * Time.deltaTime, 0f);
            yield return null;
        }

        if (ball == null)
            yield break;

        ball.transform.SetParent(shelf, true);
        ball.transform.position = slot;
        ball.transform.localScale = landScale;
        ball.MarkLanded();
        TrimShelf();
    }

    IEnumerator OpenBall(PhobiaBall ball, PhobiaDefinition def)
    {
        var slot = ball.transform.position;
        _sealedBalls.Remove(ball);
        _shelfItems.Remove(ball.gameObject);

        GameObject relic = null;
        var fullScale = Vector3.one;
        if (def != null && def.prefab != null)
        {
            relic = Instantiate(def.prefab, slot, def.prefab.transform.rotation, shelf);
            relic.name = def.Label;
            fullScale = relic.transform.localScale;
            relic.transform.localScale = Vector3.zero;
            _shelfItems.Add(relic);
        }

        var t = 0f;
        var startScale = ball.transform.localScale;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            var u = Mathf.Clamp01(t / 0.28f);
            if (ball != null)
                ball.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, Mathf.Clamp01(u * 2.2f));
            if (relic != null)
            {
                var pop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.2f) / 0.8f));
                relic.transform.localScale = fullScale * pop;
                relic.transform.position = slot + Vector3.up * (Mathf.Sin(pop * Mathf.PI) * 0.35f);
            }

            yield return null;
        }

        if (ball != null)
            Destroy(ball.gameObject);

        if (relic != null)
        {
            relic.transform.position = slot;
            relic.transform.localScale = fullScale;
        }

        TrimOpenedPhobias();
    }

    Vector3 NextShelfSlot()
    {
        var col = _nextSlot % ShelfColumns;
        _nextSlot++;
        return shelf.TransformPoint(new Vector3((col - 2) * 0.52f, 0.22f, 0f));
    }

    void TrimOpenedPhobias()
    {
        var opened = 0;
        for (var i = 0; i < _shelfItems.Count; i++)
        {
            if (IsOpenedPhobia(_shelfItems[i]))
                opened++;
        }

        var iOldest = 0;
        while (opened > MaxOpenedOnShelf)
        {
            while (iOldest < _shelfItems.Count && !IsOpenedPhobia(_shelfItems[iOldest]))
                iOldest++;
            if (iOldest >= _shelfItems.Count)
                break;

            var oldest = _shelfItems[iOldest];
            _shelfItems.RemoveAt(iOldest);
            opened--;
            if (oldest != null)
                StartCoroutine(DespawnShelfItem(oldest));
        }
    }

    static bool IsOpenedPhobia(GameObject item)
    {
        return item != null && item.GetComponent<PhobiaBall>() == null;
    }

    IEnumerator DespawnShelfItem(GameObject go)
    {
        var start = go.transform.localScale;
        var t = 0f;
        while (t < 0.2f && go != null)
        {
            t += Time.deltaTime;
            go.transform.localScale = Vector3.Lerp(start, Vector3.zero, t / 0.2f);
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    void TrimShelf()
    {
        while (_shelfItems.Count > 10)
        {
            var oldest = _shelfItems[0];
            _shelfItems.RemoveAt(0);
            if (oldest == null)
                continue;
            var ball = oldest.GetComponent<PhobiaBall>();
            if (ball != null)
                _sealedBalls.Remove(ball);
            Destroy(oldest);
        }
    }

    void FitViewport()
    {
        if (playCamera == null)
            return;

        var window = (float)Screen.width / Mathf.Max(1, Screen.height);
        Rect rect;
        if (window > TargetAspect)
        {
            var width = TargetAspect / window;
            rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else
        {
            var height = window / TargetAspect;
            rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        playCamera.rect = rect;
    }
}
