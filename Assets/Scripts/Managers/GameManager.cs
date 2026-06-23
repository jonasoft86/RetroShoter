using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField, Min(1)] private int startingLives = 3;
    [SerializeField] private Vector3 respawnPosition = new(0f, -3.5f, 0f);
    private int lives;

    public GameState State { get; private set; } = GameState.Boot;
    public int Lives => lives;
    private string nextSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        lives = RunSession.Lives > 0 ? RunSession.Lives : startingLives;
    }

    private void Start()
    {
        SetState(GameState.Playing);
        GameEvents.RaiseLivesChanged(lives);
    }

    public void InitializeLives(int value)
    {
        startingLives = Mathf.Max(1, value);
        if (RunSession.Lives <= 0)
        {
            RunSession.Lives = startingLives;
        }
        lives = RunSession.Lives;
    }

    public void InitializeProgression(string nextScene) => nextSceneName = nextScene;

    public void LoseLife(PlayerHealth player)
    {
        lives--;
        RunSession.Lives = lives;
        GameEvents.RaiseLivesChanged(lives);
        if (lives <= 0)
        {
            SetState(GameState.GameOver);
            player.gameObject.SetActive(false);
            StartCoroutine(LoadAfterDelay("GameOver", 1.2f));
        }
        else
        {
            player.Respawn(respawnPosition);
        }
    }

    public void AddLife()
    {
        lives++;
        RunSession.Lives = lives;
        GameEvents.RaiseLivesChanged(lives);
    }

    public void CompleteLevel()
    {
        SetState(GameState.Victory);
        string destination = string.IsNullOrWhiteSpace(nextSceneName) ? "Win" : nextSceneName;
        StartCoroutine(LoadAfterDelay(destination, 1.5f));
    }

    public void TogglePause()
    {
        SetState(State == GameState.Paused ? GameState.Playing : GameState.Paused);
        Time.timeScale = State == GameState.Paused ? 0f : 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void SetState(GameState state)
    {
        State = state;
        GameEvents.RaiseStateChanged(state);
    }

    private IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadScene(sceneName);
    }
}
