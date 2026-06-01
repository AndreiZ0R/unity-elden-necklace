using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float attackDamage = 25f;
    [SerializeField] public float attackCooldown = 1f;
    [SerializeField] public float attackLockDuration = 0.9f;
    [SerializeField] public float hitboxDelay = 0.7f;
    [SerializeField] public LayerMask enemyLayerMask;

    public bool IsAttacking => _lockTimer > 0f;

    private float _attackTimer;
    private float _lockTimer;
    private PlayerStamina _stamina;
    private Animator _animator;
    private PlayerInputActions _inputActions;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> _attackHandler;

    private static readonly int AttackHash = Animator.StringToHash("LightAttack");

    void Awake()
    {
        _stamina = GetComponent<PlayerStamina>();
        _animator = GetComponent<Animator>();
        _inputActions = new PlayerInputActions();
        _attackHandler = _ => TryAttack();
    }

    void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Attack.performed += _attackHandler;
    }

    void OnDisable()
    {
        _inputActions.Player.Attack.performed -= _attackHandler;
        _inputActions.Player.Disable();
    }

    void Update()
    {
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
        if (_lockTimer > 0f) _lockTimer -= Time.deltaTime;
    }

    void TryAttack()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
            return;
        if (_attackTimer > 0f) return;
        if (IsAttacking) return;
        if (_stamina != null && !_stamina.Spend(_stamina.attackCost)) return;

        _attackTimer = attackCooldown;
        _lockTimer = attackLockDuration;

        if (_animator != null)
            _animator.SetTrigger(AttackHash);

        StartCoroutine(DelayedHitbox());
    }

    IEnumerator DelayedHitbox()
    {
        yield return new WaitForSeconds(hitboxDelay);
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) yield break;
        PerformHitbox();
        NoiseEmitter.EmitNoise(transform.position, 8f, NoiseEmitter.NoiseType.Attack, gameObject);
    }

    void PerformHitbox()
    {
        Vector3 hitCenter = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        Collider[] hits = Physics.OverlapSphere(hitCenter, attackRange, enemyLayerMask);
        foreach (var hit in hits)
        {
            var ai = hit.GetComponent<EnemyAI>();
            if (ai != null) { ai.TakeDamage(attackDamage); continue; }
            var bt = hit.GetComponent<KnightBT>();
            if (bt != null) { bt.TakeDamage(attackDamage); continue; }
            var hp = hit.GetComponent<EnemyHealth>();
            if (hp != null) hp.TakeDamage(attackDamage);
        }
    }
}
