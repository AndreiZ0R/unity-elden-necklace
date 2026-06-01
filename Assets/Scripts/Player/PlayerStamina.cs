using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] public float maxStamina = 100f;
    [SerializeField] public float regenRate = 25f;
    [SerializeField] public float regenDelay = 1.5f;
    [SerializeField] public float attackCost = 20f;

    public float CurrentStamina { get; private set; }

    public event System.Action<float, float> OnStaminaChanged;

    private float _regenDelayTimer;

    void Awake() => CurrentStamina = maxStamina;

    void Update()
    {
        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        if (CurrentStamina < maxStamina)
        {
            CurrentStamina = Mathf.Min(CurrentStamina + regenRate * Time.deltaTime, maxStamina);
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }
    }

    public bool Spend(float cost)
    {
        if (CurrentStamina < cost) return false;
        CurrentStamina -= cost;
        _regenDelayTimer = regenDelay;
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        return true;
    }
}
