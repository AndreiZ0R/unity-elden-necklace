using UnityEngine;
using UnityEngine.AI;

public class GuardianAI : MonoBehaviour
{
    [SerializeField] public Transform[] patrolWaypoints;
    public int currentWaypoint { get; private set; }
    private float _waypointPauseTimer;
    private const float WaypointPauseDuration = 1f;

    void Awake()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enemyType = EnemyType.Guardian;
            ai.attackDamage = 20f;
            ai.attackRange = 1.8f;
            ai.attackCooldown = 2.5f;
        }

        var health = GetComponent<EnemyHealth>();
        if (health != null) health.maxHealth = 100f;

        var navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null) navAgent.speed = 2.0f;
    }

    public void PatrolTick(NavMeshAgent agent)
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        if (_waypointPauseTimer > 0f)
        {
            _waypointPauseTimer -= Time.deltaTime;
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        Transform target = patrolWaypoints[currentWaypoint % patrolWaypoints.Length];
        if (target == null) return;

        agent.SetDestination(target.position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentWaypoint = (currentWaypoint + 1) % patrolWaypoints.Length;
            _waypointPauseTimer = WaypointPauseDuration;
        }
    }
}
