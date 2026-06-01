using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 3f;
    [SerializeField] public float mouseSensitivity = 2f;

    public bool IsBlocking => false;

    private CharacterController _cc;
    private PlayerInputActions _inputActions;
    private Animator _animator;
    private PlayerCombat _combat;
    private Vector3 _velocity;
    private float _yaw;
    private const float Gravity = -20f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _combat = GetComponent<PlayerCombat>();
        _inputActions = new PlayerInputActions();
        _yaw = transform.eulerAngles.y;
    }

    void OnEnable() => _inputActions.Player.Enable();
    void OnDisable() => _inputActions.Player.Disable();

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
            return;

        bool attacking = _combat != null && _combat.IsAttacking;

        if (!attacking)
        {
            Vector2 lookInput = _inputActions.Player.Look.ReadValue<Vector2>();
            _yaw += lookInput.x * mouseSensitivity;
        }
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        Vector2 moveInput = attacking ? Vector2.zero : _inputActions.Player.Move.ReadValue<Vector2>();
        Move(moveInput);
    }

    void Move(Vector2 input)
    {
        Vector3 moveDir = transform.forward * input.y + transform.right * input.x;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += Gravity * Time.deltaTime;

        Vector3 horizontal = moveDir * moveSpeed;
        _cc.Move((horizontal + _velocity) * Time.deltaTime);

        if (_animator != null)
            _animator.SetFloat(SpeedHash, horizontal.magnitude);
    }
}
