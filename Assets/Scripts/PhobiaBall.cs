using UnityEngine;

public class PhobiaBall : MonoBehaviour
{
    Vector3 _baseScale;
    bool _landed;

    public string ContainedId { get; private set; }
    public Rarity ContainedRarity { get; private set; }
    public bool CanOpen => _landed;

    void Awake()
    {
        // Spawned balls use the capsule materials. GachaponBallColor picks the color.
        _baseScale = transform.localScale;
    }

    void Update()
    {
        if (!_landed)
            return;

        transform.Rotate(0f, 42f * Time.deltaTime, 0f, Space.World);
        var breathe = 1f + Mathf.Sin(Time.time * 3.6f) * 0.06f;
        transform.localScale = _baseScale * breathe;
    }

    public void Setup(PhobiaDefinition def)
    {
        ContainedId = def != null ? def.id : null;
        ContainedRarity = def != null ? def.rarity : default;
        _landed = false;
    }

    public void MarkLanded()
    {
        _landed = true;
        _baseScale = transform.localScale;
    }

    public bool TryHit(Ray ray, out float distance)
    {
        distance = 0f;
        var collider = GetComponent<Collider>();
        if (collider == null || !_landed)
            return false;
        return collider.Raycast(ray, out var hit, 80f) && (distance = hit.distance) >= 0f;
    }
}
