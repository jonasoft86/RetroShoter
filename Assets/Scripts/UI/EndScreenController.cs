using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private bool victory;

    private void Awake()
    {
        if (scoreText == null)
            BuildRuntimeUI();
    }

    private void Start()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE  {RunSession.Score:000000}";
        if (highScoreText != null)
            highScoreText.text = $"HIGH SCORE  {PlayerPrefs.GetInt("HighScore", 0):000000}";
    }

    private void Update()
    {
        // Buttons handle touch via EventSystem; keyboard shortcuts are extras for desktop
        if (Keyboard.current == null) return;
        if (Keyboard.current.rKey.wasPressedThisFrame)         Retry();
        else if (Keyboard.current.escapeKey.wasPressedThisFrame) MainMenu();
    }

    public void Retry()
    {
        RunSession.Reset();
        SceneManager.LoadScene("Level01_DeepSpace");
    }

    public void MainMenu() => SceneManager.LoadScene("Menu");

    private void BuildRuntimeUI()
    {
        Canvas canvas = RuntimeScreenUI.CreateCanvas(victory ? "Win Canvas" : "Game Over Canvas");

        // Safe-area panel keeps content away from notches and home bars
        Transform safe = RuntimeScreenUI.CreateSafeAreaPanel(canvas.transform);

        RuntimeScreenUI.CreateLabel(safe,
            victory ? "VICTORY!" : "GAME OVER",
            new Vector2(0.5f, 0.72f), new Vector2(420f, 90f), 48,
            victory ? new Color(0.3f, 1f, 0.65f) : new Color(1f, 0.25f, 0.15f));

        scoreText = RuntimeScreenUI.CreateLabel(safe,
            "SCORE  000000",
            new Vector2(0.5f, 0.55f), new Vector2(360f, 45f), 24, Color.white);

        highScoreText = RuntimeScreenUI.CreateLabel(safe,
            "HIGH SCORE  000000",
            new Vector2(0.5f, 0.47f), new Vector2(360f, 45f), 21, Color.white);

        // Large touch targets (320 × 70) wired to Button.onClick — touch works via EventSystem
        RuntimeScreenUI.CreateButton(safe, "PLAY AGAIN", new Vector2(0.5f, 0.33f), Retry);
        RuntimeScreenUI.CreateButton(safe, "MAIN MENU",  new Vector2(0.5f, 0.20f), MainMenu);
    }
}
