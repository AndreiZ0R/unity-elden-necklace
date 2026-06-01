using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PerceptionSystem))]
[RequireComponent(typeof(EnemyHealth))]
public class KnightBT : MonoBehaviour
{
    [SerializeField] public float berserkThreshold = 0.3f;
    [SerializeField] public float attackRange = 2.0f;
    [SerializeField] public float attackDamage = 25f;
    [SerializeField] public float attackCooldown = 2.0f;
    [SerializeField] public float attackLockDuration = 0.7f;

    private NavMeshAgent _agent;
    private PerceptionSystem _perception;
    private EnemyHealth _health;
    private FlockingBehavior _flocking;
    private Animator _animator;
    private BTNode _root;
    private float _attackTimer;
    private float _attackLockTimer;
    private Transform _playerTransform;
    private float _baseSpeed;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<PerceptionSystem>();
        _health = GetComponent<EnemyHealth>();
        _flocking = GetComponent<FlockingBehavior>();
        _animator = GetComponent<Animator>();
        _health.maxHealth = 150f;
        _agent.speed = 2.5f;
        _baseSpeed = _agent.speed;

        _health.OnDeath += OnDied;
        BuildBT();
    }

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) _playerTransform = player.transform;

        var vision = GetComponent<VisionSystem>();
        if (vision != null) vision.visionRange = Mathf.Max(vision.visionRange, 80f);

        if (_playerTransform != null) _perception.EscalateTo(PerceptionState.Combat);
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnDeath -= OnDied;
    }

    void BuildBT()
    {
        var deadSeq = new Sequence(new List<BTNode>
        {
            new ConditionLeaf(() => _health.IsDead),
            new ActionLeaf(StopAndDie)
        });

        var berserkAttackSeq = new Sequence(new List<BTNode>
        {
            new ConditionLeaf(InAttackRange),
            new ActionLeaf(BerserkAttack)
        });
        var berserkSelector = new Selector(new List<BTNode>
        {
            berserkAttackSeq,
            new ActionLeaf(ChaseFast)
        });
        var berserkSeq = new Sequence(new List<BTNode>
        {
            new ConditionLeaf(() => _health.CurrentHealth / _health.maxHealth < berserkThreshold),
            new ConditionLeaf(() => _perception.HasTarget),
            berserkSelector
        });

        var standardAttackSeq = new Sequence(new List<BTNode>
        {
            new ConditionLeaf(InAttackRange),
            new ActionLeaf(StandardAttack)
        });
        var standardSelector = new Selector(new List<BTNode>
        {
            standardAttackSeq,
            new ActionLeaf(Chase)
        });
        var standardSeq = new Sequence(new List<BTNode>
        {
            new ConditionLeaf(() => _perception.HasTarget),
            standardSelector
        });

        _root = new Selector(new List<BTNode> { deadSeq, berserkSeq, standardSeq });
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;
        if (_attackLockTimer > 0f) _attackLockTimer -= Time.deltaTime;

        if (_attackLockTimer > 0f)
        {
            _agent.isStopped = true;
            if (_animator != null) _animator.SetFloat(SpeedHash, 0f);
            return;
        }
        _agent.isStopped = false;

        _root?.Tick();
        if (_animator != null)
            _animator.SetFloat(SpeedHash, _agent.velocity.magnitude);
    }

    bool InAttackRange()
    {
        if (_playerTransform == null) return false;
        return Vector3.Distance(transform.position, _playerTransform.position) <= attackRange;
    }

    BTNode.Status Chase()
    {
        if (_playerTransform == null) return BTNode.Status.Failure;
        _agent.speed = _baseSpeed;
        Vector3 dest = _playerTransform.position;
        if (_flocking != null) { _flocking.Tick(); dest = _flocking.ApplyTo(dest); }
        _agent.SetDestination(dest);
        return BTNode.Status.Running;
    }

    BTNode.Status ChaseFast()
    {
        if (_playerTransform == null) return BTNode.Status.Failure;
        _agent.speed = _baseSpeed * 1.4f;
        Vector3 dest = _playerTransform.position;
        if (_flocking != null) { _flocking.Tick(); dest = _flocking.ApplyTo(dest); }
        _agent.SetDestination(dest);
        return BTNode.Status.Running;
    }

    BTNode.Status StandardAttack()
    {
        if (_attackTimer > 0f) return BTNode.Status.Running;
        _attackTimer = attackCooldown;
        PerformAttack(attackDamage);
        return BTNode.Status.Success;
    }

    BTNode.Status BerserkAttack()
    {
        if (_attackTimer > 0f) return BTNode.Status.Running;
        _attackTimer = attackCooldown * 0.55f;
        PerformAttack(attackDamage);
        return BTNode.Status.Success;
    }

    void PerformAttack(float damage)
    {
        _attackLockTimer = attackLockDuration;
        if (_animator != null) _animator.SetTrigger(AttackHash);
        if (_playerTransform != null)
        {
            var ph = _playerTransform.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }
    }

    BTNode.Status StopAndDie()
    {
        _agent.isStopped = true;
        _agent.enabled = false;
        if (_animator != null) _animator.SetTrigger(DeathHash);
        Destroy(gameObject, 3f);
        return BTNode.Status.Success;
    }

    void OnDied(EnemyHealth _)
    {
        // BT's Dead branch will handle cleanup next tick
    }

    public void TakeDamage(float amount)
    {
        _health.TakeDamage(amount);
        _perception.EscalateTo(PerceptionState.Combat);
    }
}
