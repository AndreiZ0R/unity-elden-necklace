using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] public Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] public float flashDuration = 0.15f;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;
    private float _timer;
    private bool _flashing;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _block = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null) ph.OnDamaged += Flash;
        var eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDamaged += Flash;
    }

    void OnDisable()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null) ph.OnDamaged -= Flash;
        var eh = GetComponent<EnemyHealth>();
        if (eh != null) eh.OnDamaged -= Flash;
    }

    public void Flash()
    {
        _timer = flashDuration;
        if (!_flashing)
        {
            ApplyTint(flashColor);
            _flashing = true;
        }
    }

    void Update()
    {
        if (!_flashing) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            ClearTint();
            _flashing = false;
        }
    }

    void ApplyTint(Color c)
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            _block.SetColor(ColorId, c);
            r.SetPropertyBlock(_block);
        }
    }

    void ClearTint()
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            _block.Clear();
            r.SetPropertyBlock(_block);
        }
    }
}
