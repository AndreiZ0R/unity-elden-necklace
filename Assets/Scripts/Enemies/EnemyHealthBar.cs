using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] public float heightOffset = 2.2f;
    [SerializeField] public Vector2 size = new Vector2(1.2f, 0.15f);

    private EnemyHealth _health;
    private Transform _camera;
    private Transform _root;
    private RectTransform _fill;

    void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        BuildBar();
    }

    void OnEnable()
    {
        if (_health != null) _health.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (_health != null) _health.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        var cam = Camera.main;
        if (cam != null) _camera = cam.transform;
    }

    void LateUpdate()
    {
        if (_root == null || _camera == null) return;
        _root.rotation = Quaternion.LookRotation(_root.position - _camera.position);
    }

    void HandleHealthChanged(float current, float max)
    {
        if (_fill == null) return;
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _fill.localScale = new Vector3(ratio, 1f, 1f);
        if (current <= 0f && _root != null) _root.gameObject.SetActive(false);
    }

    void BuildBar()
    {
        var canvasGO = new GameObject("HealthBar");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        _root = canvasGO.transform;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 12f);
        rt.localScale = new Vector3(size.x / 100f, size.y / 12f, 1f);

        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(canvasGO.transform, false);
        _fill = (RectTransform)fill.transform;
        _fill.anchorMin = new Vector2(0f, 0f);
        _fill.anchorMax = new Vector2(1f, 1f);
        _fill.offsetMin = new Vector2(2f, 2f);
        _fill.offsetMax = new Vector2(-2f, -2f);
        _fill.pivot = new Vector2(0f, 0.5f);
        fill.GetComponent<Image>().color = new Color(0.85f, 0.1f, 0.1f, 1f);
    }

}
