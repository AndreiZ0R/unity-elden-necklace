using UnityEngine;

[RequireComponent(typeof(PerceptionSystem))]
public class HearingSystem : MonoBehaviour
{
    [SerializeField] public float hearingRange = 5f;
    [SerializeField] public float combatHearingRange = 8f;
    [SerializeField] public float minimumNoiseRadius = 1f;

    public event System.Action<Vector3, NoiseEmitter.NoiseType> OnNoiseHeard;

    void OnEnable() => NoiseEmitter.OnNoiseEmitted += HandleNoise;
    void OnDisable() => NoiseEmitter.OnNoiseEmitted -= HandleNoise;

    void HandleNoise(Vector3 worldPos, float radius, NoiseEmitter.NoiseType type, GameObject source)
    {
        if (source == gameObject) return;
        if (radius < minimumNoiseRadius) return;

        float range = hearingRange;
        var perception = GetComponent<PerceptionSystem>();
        if (perception != null && perception.CurrentState >= PerceptionState.Combat)
            range = combatHearingRange;

        float dist = Vector3.Distance(transform.position, worldPos);
        if (dist <= range)
            OnNoiseHeard?.Invoke(worldPos, type);
    }
}
