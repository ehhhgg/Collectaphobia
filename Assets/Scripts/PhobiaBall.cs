using UnityEngine;

public class PhobiaBall : MonoBehaviour
{
    [SerializeField] Renderer ballRenderer;
    [SerializeField] Color sealedColor = new(0.95f, 0.42f, 0.55f);

    Material _mat;
    Vector3 _baseScale;
    bool _landed;

    public string ContainedId { get; private set; }
    public Rarity ContainedRarity { get; private set; }
    public bool CanOpen => _landed;

    void Awake()
    {
        if (ballRenderer == null)
            ballRenderer = GetComponent<Renderer>();
        if (ballRenderer != null)
            _mat = ballRenderer.material;
        _baseScale = transform.localScale;
        ApplySealedLook();
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
        ApplySealedLook();
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

    void ApplySealedLook()
    {
        if (_mat == null)
            return;

        if (_mat.HasProperty("_BaseColor"))
            _mat.SetColor("_BaseColor", sealedColor);
        if (_mat.HasProperty("_Color"))
            _mat.SetColor("_Color", sealedColor);
        _mat.color = sealedColor;
        if (_mat.HasProperty("_EmissionColor"))
        {
            _mat.EnableKeyword("_EMISSION");
            _mat.SetColor("_EmissionColor", sealedColor * 1.2f);
        }
    }
}
