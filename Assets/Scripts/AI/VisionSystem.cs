using UnityEngine;

[RequireComponent(typeof(PerceptionSystem))]
public class VisionSystem : MonoBehaviour
{
    [SerializeField] public float fieldOfViewAngle = 120f;
    [SerializeField] public float visionRange = 8f;
    [SerializeField] public float peripheralRange = 2.5f;
    [SerializeField] public float checkInterval = 0.1f;
    [SerializeField] public LayerMask sightBlockers;

    public bool TargetVisible { get; private set; }
    public float TargetDistance { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public Transform LastKnownTransform { get; private set; }

    private Transform _playerTransform;
    private float _timer;

    void Awake()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) _playerTransform = player.transform;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = checkInterval;
        CheckVision();
    }

    void CheckVision()
    {
        if (_playerTransform == null) { TargetVisible = false; return; }

        Vector3 dirToPlayer = _playerTransform.position - transform.position;
        float distance = dirToPlayer.magnitude;

        if (distance <= peripheralRange)
        {
            TargetVisible = true;
            TargetDistance = distance;
            LastKnownPosition = _playerTransform.position;
            LastKnownTransform = _playerTransform;
            return;
        }

        if (distance > visionRange) { TargetVisible = false; return; }

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfViewAngle * 0.5f) { TargetVisible = false; return; }

        if (Physics.Raycast(transform.position + Vector3.up * 1f, dirToPlayer.normalized, distance, sightBlockers))
        {
            TargetVisible = false;
            return;
        }

        TargetVisible = true;
        TargetDistance = distance;
        LastKnownPosition = _playerTransform.position;
        LastKnownTransform = _playerTransform;
    }
}
