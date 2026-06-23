using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";
    public static ScoreManager Instance { get; private set; }

    public int Score { get; private set; }
    public int Multiplier { get; private set; } = 1;
    public int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);

    private void Awake()
    {
        Instance = this;
        Score = RunSession.Score;
        Multiplier = RunSession.Multiplier;
    }

    private void Start()
    {
        GameEvents.RaiseScoreChanged(Score);
        GameEvents.RaiseHighScoreChanged(HighScore);
        GameEvents.RaiseMultiplierChanged(Multiplier);
    }

    public void AddScore(int baseValue)
    {
        Score += Mathf.Max(0, baseValue) * Multiplier;
        Multiplier = Mathf.Min(4, Multiplier + 1);
        RunSession.Score = Score;
        RunSession.Multiplier = Multiplier;
        if (Score > HighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, Score);
            PlayerPrefs.Save();
        }
        GameEvents.RaiseScoreChanged(Score);
        GameEvents.RaiseHighScoreChanged(HighScore);
        GameEvents.RaiseMultiplierChanged(Multiplier);
    }

    public void ResetMultiplier()
    {
        Multiplier = 1;
        RunSession.Multiplier = Multiplier;
        GameEvents.RaiseMultiplierChanged(Multiplier);
    }
}
