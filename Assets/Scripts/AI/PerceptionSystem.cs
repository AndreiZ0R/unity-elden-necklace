using UnityEngine;

public enum PerceptionState { Unaware, Suspicious, Alert, Combat }

[RequireComponent(typeof(VisionSystem))]
[RequireComponent(typeof(HearingSystem))]
public class PerceptionSystem : MonoBehaviour
{
    [SerializeField] public float alertTimeout = 5f;
    [SerializeField] public float suspiciousTimeout = 8f;

    public PerceptionState CurrentState { get; private set; } = PerceptionState.Unaware;
    public bool HasTarget => CurrentState >= PerceptionState.Alert;
    public Vector3 LastKnownPosition { get; private set; }

    public event System.Action<PerceptionState, PerceptionState> OnStateChanged;

    private VisionSystem _vision;
    private HearingSystem _hearing;
    private float _degradeTimer;

    void Awake()
    {
        _vision = GetComponent<VisionSystem>();
        _hearing = GetComponent<HearingSystem>();
    }

    void OnEnable() => _hearing.OnNoiseHeard += HandleNoise;
    void OnDisable() => _hearing.OnNoiseHeard -= HandleNoise;

    void Update()
    {
        if (_vision.TargetVisible)
        {
            EscalateTo(PerceptionState.Combat);
            LastKnownPosition = _vision.LastKnownPosition;
            _degradeTimer = alertTimeout;
            return;
        }

        if (CurrentState > PerceptionState.Unaware)
        {
            _degradeTimer -= Time.deltaTime;
            if (_degradeTimer <= 0f)
            {
                if (CurrentState == PerceptionState.Suspicious)
                    SetState(PerceptionState.Unaware);
                else if (CurrentState == PerceptionState.Alert)
                    SetState(PerceptionState.Suspicious);
                else if (CurrentState == PerceptionState.Combat)
                    SetState(PerceptionState.Alert);

                _degradeTimer = suspiciousTimeout;
            }
        }
    }

    public void EscalateTo(PerceptionState newState)
    {
        if (newState <= CurrentState) return;
        SetState(newState);
    }

    void SetState(PerceptionState newState)
    {
        var old = CurrentState;
        CurrentState = newState;
        if (old != newState)
            OnStateChanged?.Invoke(old, newState);
    }

    void HandleNoise(Vector3 pos, NoiseEmitter.NoiseType type)
    {
        LastKnownPosition = pos;
        if (CurrentState < PerceptionState.Suspicious)
            EscalateTo(PerceptionState.Suspicious);
        else if (CurrentState < PerceptionState.Alert && type == NoiseEmitter.NoiseType.Attack)
            EscalateTo(PerceptionState.Alert);
    }
}
