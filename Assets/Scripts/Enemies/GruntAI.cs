using UnityEngine;
using UnityEngine.AI;

public class GruntAI : MonoBehaviour
{
    void Awake()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enemyType = EnemyType.Grunt;
            ai.attackDamage = 12f;
            ai.attackRange = 1.5f;
            ai.attackCooldown = 1.5f;
        }

        var health = GetComponent<EnemyHealth>();
        if (health != null) health.maxHealth = 40f;

        var navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null) navAgent.speed = 3.5f;
    }
}
