using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AlertNearby(Vector3 origin, float radius, EnemyAI source)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("Enemy"));
        foreach (var hit in hits)
        {
            var ai = hit.GetComponent<EnemyAI>();
            if (ai == null || ai == source) continue;
            if (ai.State == AIState.Patrol || ai.State == AIState.Alert || ai.State == AIState.Idle)
                ai.TakeDamage(0f);
        }
    }
}
