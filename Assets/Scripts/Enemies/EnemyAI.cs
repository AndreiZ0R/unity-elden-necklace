using UnityEngine;
using UnityEngine.AI;

public enum EnemyType { Grunt, Guardian, Knight }
public enum AIState { Idle, Patrol, Alert, Chase, Attack, Dead }

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PerceptionSystem))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    public EnemyType enemyType;
    public AIState State { get; private set; } = AIState.Idle;

    [HideInInspector] public float attackRange = 1.5f;
    [HideInInspector] public float attackDamage = 12f;
    [HideInInspector] public float attackCooldown = 1.5f;
    [HideInInspector] public float attackLockDuration = 0.6f;

    protected NavMeshAgent agent;
    protected PerceptionSystem perception;
    protected EnemyHealth health;
    private FlockingBehavior _flocking;
    private float _attackTimer;
    private float _attackLockTimer;
    private Transform _playerTransform;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    protected Animator animator;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<PerceptionSystem>();
        health = GetComponent<EnemyHealth>();
        _flocking = GetComponent<FlockingBehavior>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        var playerCtrl = FindAnyObjectByType<PlayerController>();
        if (playerCtrl != null) _playerTransform = playerCtrl.transform;

        perception.OnStateChanged += HandlePerceptionChanged;
        health.OnDeath += OnDied;

        var vision = GetComponent<VisionSystem>();
        if (vision != null) vision.visionRange = Mathf.Max(vision.visionRange, 80f);

        if (_playerTransform != null) perception.EscalateTo(PerceptionState.Combat);
    }

    void OnDestroy()
    {
        if (perception != null) perception.OnStateChanged -= HandlePerceptionChanged;
        if (health != null) health.OnDeath -= OnDied;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
        if (_attackLockTimer > 0f) _attackLockTimer -= Time.deltaTime;

        if (_attackLockTimer > 0f)
        {
            agent.isStopped = true;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            return;
        }
        agent.isStopped = false;

        switch (State)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.Alert: UpdateAlert(); break;
            case AIState.Chase: UpdateChase(); break;
            case AIState.Attack: UpdateAttack(); break;
        }

        if (animator != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    protected virtual void UpdateIdle() { }

    protected virtual void UpdatePatrol()
    {
        var guardian = GetComponent<GuardianAI>();
        if (guardian != null)
            guardian.PatrolTick(agent);
    }

    void UpdateAlert()
    {
        agent.SetDestination(perception.LastKnownPosition);
    }

    void UpdateChase()
    {
        if (_playerTransform == null) { SetState(AIState.Patrol); return; }

        Vector3 dest = _playerTransform.position;
        if (_flocking != null)
        {
            _flocking.Tick();
            dest = _flocking.ApplyTo(dest);
        }

        agent.SetDestination(dest);

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        if (dist <= attackRange)
            SetState(AIState.Attack);
    }

    void UpdateAttack()
    {
        if (_playerTransform == null) { SetState(AIState.Patrol); return; }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        if (dist > attackRange * 1.4f)
        {
            SetState(AIState.Chase);
            return;
        }

        transform.LookAt(new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z));
        agent.SetDestination(transform.position);

        if (_attackTimer <= 0f)
        {
            _attackTimer = attackCooldown;
            _attackLockTimer = attackLockDuration;
            if (animator != null) animator.SetTrigger(AttackHash);
            var playerHealth = _playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
        }
    }

    void HandlePerceptionChanged(PerceptionState oldState, PerceptionState newState)
    {
        if (State == AIState.Dead) return;

        switch (newState)
        {
            case PerceptionState.Suspicious:
                if (State == AIState.Idle || State == AIState.Patrol)
                    SetState(AIState.Alert);
                break;
            case PerceptionState.Alert:
            case PerceptionState.Combat:
                SetState(AIState.Chase);
                if (newState == PerceptionState.Combat)
                    EnemyManager.Instance?.AlertNearby(transform.position, 10f, this);
                break;
            case PerceptionState.Unaware:
                if (State != AIState.Dead)
                    SetState(AIState.Patrol);
                break;
        }
    }

    void OnDied(EnemyHealth _)
    {
        SetState(AIState.Dead);
        agent.isStopped = true;
        agent.enabled = false;
        if (animator != null) animator.SetTrigger(DeathHash);
        Destroy(gameObject, 3f);
    }

    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);
        if (!health.IsDead && animator != null && amount > 0f)
            animator.SetTrigger(HitHash);
        perception.EscalateTo(PerceptionState.Combat);
    }

    void SetState(AIState newState)
    {
        State = newState;
        if (newState == AIState.Idle || newState == AIState.Patrol)
            agent.isStopped = false;
    }
}
