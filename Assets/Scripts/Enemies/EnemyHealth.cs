using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public float maxHealth = 40f;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<EnemyHealth> OnDeath;
    public event System.Action OnDamaged;

    void Awake() => CurrentHealth = maxHealth;

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnDamaged?.Invoke();
        if (CurrentHealth <= 0f)
            OnDeath?.Invoke(this);
    }
}
