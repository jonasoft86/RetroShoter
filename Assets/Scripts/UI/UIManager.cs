using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text multiplierText;
    [SerializeField] private Text stateText;
    [SerializeField] private GameObject bossBarRoot;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private Text bossNameText;

    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text livesHeartsText;

    [SerializeField] private GameObject powerUpRoot;
    [SerializeField] private Text powerUpNameText;
    [SerializeField] private Image powerUpTimerFill;

    private Coroutine powerUpCoroutine;

    private void OnEnable()
    {
        GameEvents.ScoreChanged += UpdateScore;
        GameEvents.LivesChanged += UpdateLives;
        GameEvents.HealthChanged += UpdateHealth;
        GameEvents.HighScoreChanged += UpdateHighScore;
        GameEvents.MultiplierChanged += UpdateMultiplier;
        GameEvents.StateChanged += UpdateState;
        GameEvents.BossHealthChanged += UpdateBossHealth;
        GameEvents.BossVisibilityChanged += UpdateBossVisibility;
        GameEvents.PowerUpCollected += ShowPowerUp;
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= UpdateScore;
        GameEvents.LivesChanged -= UpdateLives;
        GameEvents.HealthChanged -= UpdateHealth;
        GameEvents.HighScoreChanged -= UpdateHighScore;
        GameEvents.MultiplierChanged -= UpdateMultiplier;
        GameEvents.StateChanged -= UpdateState;
        GameEvents.BossHealthChanged -= UpdateBossHealth;
        GameEvents.BossVisibilityChanged -= UpdateBossVisibility;
        GameEvents.PowerUpCollected -= ShowPowerUp;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.GameOver)
            return;

        bool key   = Keyboard.current    != null && Keyboard.current.rKey.wasPressedThisFrame;
        // WebGL mobile: touch events arrive as Mouse; Touchscreen may also work on native
        bool touch = Touchscreen.current != null
                     && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (key || touch || click)
            GameManager.Instance.Restart();
    }

    public void Initialize(
        Text score, Text lives, Text health, Text highScore,
        Text multiplier, Text state, GameObject bossRoot = null,
        Slider bossSlider = null, Text bossName = null)
    {
        scoreText = score;
        livesText = lives;
        healthText = health;
        highScoreText = highScore;
        multiplierText = multiplier;
        stateText = state;
        bossBarRoot = bossRoot;
        bossHealthSlider = bossSlider;
        bossNameText = bossName;
    }

    private void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = $"{score:000000}";
    }

    private void UpdateLives(int lives)
    {
        if (livesText) livesText.text = $"LIVES  {lives}";
        if (livesHeartsText)
            livesHeartsText.text = lives >= 3 ? "♥ ♥ ♥"
                                 : lives == 2 ? "♥ ♥ ░"
                                 : lives == 1 ? "♥ ░ ░"
                                 :              "░ ░ ░";
    }

    private void UpdateHealth(int current, int maximum)
    {
        if (healthText) healthText.text = $"HP  {current}/{maximum}";
        if (healthBarFill && maximum > 0)
        {
            float pct = (float)current / maximum;
            healthBarFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            healthBarFill.color = pct > 0.5f ? new Color(0f, 0.80f, 0.35f)
                                : pct > 0.25f ? new Color(1f, 0.75f, 0f)
                                :               new Color(1f, 0.20f, 0.10f);
        }
    }

    private void UpdateHighScore(int score)
    {
        if (highScoreText) highScoreText.text = $"{score:000000}";
    }

    private void UpdateMultiplier(int value)
    {
        if (multiplierText) multiplierText.text = $"x{value}";
    }
    private void UpdateBossHealth(string bossName, int current, int maximum)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maximum;
            bossHealthSlider.value = current;
        }
        if (bossNameText != null)
        {
            bossNameText.text = bossName.ToUpperInvariant();
        }
    }
    private void UpdateBossVisibility(bool visible)
    {
        if (bossBarRoot != null)
        {
            bossBarRoot.SetActive(visible);
        }
    }
    private void UpdateState(GameState state)
    {
        stateText.gameObject.SetActive(state is GameState.GameOver or GameState.Victory);
        stateText.text = state == GameState.GameOver
            ? "GAME OVER\n\nR  ·  TAP TO RESTART"
            : state == GameState.Victory ? "VICTORY" : string.Empty;
    }

    // ── Power-up display ───────────────────────────────────────────────────
    private void ShowPowerUp(PowerUpType type, float duration)
    {
        if (powerUpRoot == null) return;
        if (powerUpCoroutine != null) StopCoroutine(powerUpCoroutine);
        powerUpCoroutine = StartCoroutine(PowerUpDisplay(type, duration));
    }

    private IEnumerator PowerUpDisplay(PowerUpType type, float displayDuration)
    {
        Color col = PowerUpColor(type);
        if (powerUpNameText != null)
        {
            powerUpNameText.text  = PowerUpLabel(type);
            powerUpNameText.color = col;
        }
        if (powerUpTimerFill != null)
        {
            powerUpTimerFill.color = col;
            powerUpTimerFill.rectTransform.anchorMax = Vector2.one;
        }
        powerUpRoot.SetActive(true);

        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            if (powerUpTimerFill != null)
                powerUpTimerFill.rectTransform.anchorMax =
                    new Vector2(1f - elapsed / displayDuration, 1f);
            yield return null;
        }

        powerUpRoot.SetActive(false);
        powerUpCoroutine = null;
    }

    private static string PowerUpLabel(PowerUpType t) => t switch
    {
        PowerUpType.WeaponUpgrade => "WEAPON UP",
        PowerUpType.Shield        => "SHIELD",
        PowerUpType.SpeedBoost    => "SPEED BOOST",
        PowerUpType.ExtraLife     => "EXTRA LIFE",
        _                         => t.ToString().ToUpperInvariant()
    };

    private static Color PowerUpColor(PowerUpType t) => t switch
    {
        PowerUpType.WeaponUpgrade => new Color(0.18f, 0.88f, 1f),
        PowerUpType.Shield        => new Color(0.40f, 0.70f, 1f),
        PowerUpType.SpeedBoost    => new Color(1f, 0.85f, 0.28f),
        PowerUpType.ExtraLife     => new Color(1f, 0.35f, 0.45f),
        _                         => Color.white
    };
}
