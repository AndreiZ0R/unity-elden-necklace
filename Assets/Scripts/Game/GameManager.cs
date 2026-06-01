using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public int CurrentWave { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnWaveChanged;
    public event System.Action OnGameOver;
    public event System.Action<bool> OnPauseChanged;

    private PlayerInputActions _inputActions;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> _pauseHandler;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _inputActions = new PlayerInputActions();
        _pauseHandler = _ => TogglePause();
    }

    void Start()
    {
        var playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.OnDeath += HandlePlayerDeath;
    }

    void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Pause.performed += _pauseHandler;
    }

    void OnDisable()
    {
        _inputActions.Player.Pause.performed -= _pauseHandler;
        _inputActions.Player.Disable();
    }

    void HandlePlayerDeath()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        OnGameOver?.Invoke();
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    public void SetWave(int wave)
    {
        CurrentWave = wave;
        OnWaveChanged?.Invoke(CurrentWave);
    }

    public void TriggerGameOver() => HandlePlayerDeath();

    public void TogglePause()
    {
        if (IsGameOver) return;
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;
        OnPauseChanged?.Invoke(IsPaused);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsGameOver = false;
        IsPaused = false;
        Score = 0;
        CurrentWave = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
