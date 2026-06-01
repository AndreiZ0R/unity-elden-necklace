using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [SerializeField] public Slider healthSlider;
    [SerializeField] public Slider staminaSlider;
    [SerializeField] public TextMeshProUGUI waveText;
    [SerializeField] public TextMeshProUGUI scoreText;
    [SerializeField] public GameObject gameOverPanel;
    [SerializeField] public TextMeshProUGUI finalScoreText;
    [SerializeField] public TextMeshProUGUI finalWaveText;
    [SerializeField] public Button restartButton;
    [SerializeField] public GameObject pausedOverlay;

    private PlayerHealth _health;
    private PlayerStamina _stamina;

    void Start()
    {
        _health = FindAnyObjectByType<PlayerHealth>();
        _stamina = FindAnyObjectByType<PlayerStamina>();

        if (_health != null)
        {
            _health.OnHealthChanged += UpdateHealth;
            UpdateHealth(_health.CurrentHealth, _health.maxHealth);
        }

        if (_stamina != null)
        {
            _stamina.OnStaminaChanged += UpdateStamina;
            UpdateStamina(_stamina.CurrentStamina, _stamina.maxStamina);
        }

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += UpdateWave;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnGameOver += ShowGameOver;
            GameManager.Instance.OnPauseChanged += UpdatePause;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausedOverlay != null) pausedOverlay.SetActive(false);
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnHealthChanged -= UpdateHealth;
        if (_stamina != null) _stamina.OnStaminaChanged -= UpdateStamina;
        if (WaveManager.Instance != null) WaveManager.Instance.OnWaveChanged -= UpdateWave;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnGameOver -= ShowGameOver;
            GameManager.Instance.OnPauseChanged -= UpdatePause;
        }
    }

    void UpdateHealth(float current, float max)
    {
        if (healthSlider != null) healthSlider.value = current / max;
    }

    void UpdateStamina(float current, float max)
    {
        if (staminaSlider != null) staminaSlider.value = current / max;
    }

    void UpdateWave(int wave)
    {
        if (waveText != null) waveText.text = $"WAVE {wave}";
    }

    void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score}";
    }

    void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        if (finalWaveText != null && GameManager.Instance != null)
            finalWaveText.text = $"Final Wave: {GameManager.Instance.CurrentWave}";
        if (finalScoreText != null && GameManager.Instance != null)
            finalScoreText.text = $"Final Score: {GameManager.Instance.Score}";
    }

    void UpdatePause(bool isPaused)
    {
        if (pausedOverlay != null) pausedOverlay.SetActive(isPaused);
    }
}
