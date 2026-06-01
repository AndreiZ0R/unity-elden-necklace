using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] public Vector3 offset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] public float defaultDistance = 5f;
    [SerializeField] public float pitch = 15f;
    [SerializeField] public float followSpeed = 6f;
    [SerializeField] public float collisionPadding = 0.3f;
    [SerializeField] public LayerMask collisionMask;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver))
            return;

        float yaw = target.eulerAngles.y;
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + offset;
        Vector3 desiredDir = desiredRot * Vector3.back;

        float armLength = defaultDistance;
        if (Physics.SphereCast(pivot, collisionPadding, desiredDir, out RaycastHit hit, defaultDistance, collisionMask))
            armLength = Mathf.Max(hit.distance - collisionPadding, 0.5f);

        Vector3 desiredPos = pivot + desiredDir * armLength;
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, t);
    }
}
