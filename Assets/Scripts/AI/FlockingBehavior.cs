using UnityEngine;
using UnityEngine.AI;

public class FlockingBehavior : MonoBehaviour
{
    [SerializeField] public float neighbourRadius = 4f;
    [SerializeField] public float separationRadius = 1.6f;
    [SerializeField] public float separationWeight = 1.4f;
    [SerializeField] public float maxOffsetDistance = 1.0f;
    [SerializeField] public float updateInterval = 0.15f;
    [SerializeField] public LayerMask enemyLayerMask;

    private float _timer;
    private Vector3 _cachedOffset;
    private static readonly Collider[] _buffer = new Collider[16];

    public Vector3 Tick()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return _cachedOffset;
        _timer = updateInterval;

        int count = Physics.OverlapSphereNonAlloc(transform.position, neighbourRadius, _buffer, enemyLayerMask);
        Vector3 separation = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            if (_buffer[i].gameObject == gameObject) continue;
            Vector3 diff = transform.position - _buffer[i].transform.position;
            float dist = diff.magnitude;
            if (dist < separationRadius && dist > 0.001f)
                separation += diff.normalized * (separationRadius - dist);
        }

        _cachedOffset = Vector3.ClampMagnitude(separation * separationWeight, maxOffsetDistance);
        return _cachedOffset;
    }

    public Vector3 ApplyTo(Vector3 target)
    {
        Vector3 raw = target + _cachedOffset;
        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, maxOffsetDistance + 1f, NavMesh.AllAreas))
            return hit.position;
        return target;
    }
}
