using System;
using UnityEngine;

public enum ProjectileOwner
{
    Player,
    Enemy
}

public enum PowerUpType
{
    WeaponUpgrade,
    Shield,
    SpeedBoost,
    ExtraLife
}

public enum GameState
{
    Boot,
    Playing,
    Paused,
    GameOver,
    Victory
}

public enum BossAttackPattern
{
    Spread,
    AimedBurst,
    Spiral
}

public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int amount);
}

public static class GameEvents
{
    public static event Action<int> ScoreChanged;
    public static event Action<int> HighScoreChanged;
    public static event Action<int> MultiplierChanged;
    public static event Action<int> LivesChanged;
    public static event Action<int, int> HealthChanged;
    public static event Action<GameState> StateChanged;
    public static event Action<string, int, int> BossHealthChanged;
    public static event Action<bool> BossVisibilityChanged;
    public static event Action<PowerUpType, float> PowerUpCollected;

    public static void RaiseScoreChanged(int value) => ScoreChanged?.Invoke(value);
    public static void RaiseHighScoreChanged(int value) => HighScoreChanged?.Invoke(value);
    public static void RaiseMultiplierChanged(int value) => MultiplierChanged?.Invoke(value);
    public static void RaiseLivesChanged(int value) => LivesChanged?.Invoke(value);
    public static void RaiseHealthChanged(int current, int maximum) =>
        HealthChanged?.Invoke(current, maximum);
    public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
    public static void RaiseBossHealthChanged(string name, int current, int maximum) =>
        BossHealthChanged?.Invoke(name, current, maximum);
    public static void RaiseBossVisibilityChanged(bool visible) =>
        BossVisibilityChanged?.Invoke(visible);
    public static void RaisePowerUpCollected(PowerUpType type, float duration) =>
        PowerUpCollected?.Invoke(type, duration);
}

public static class RunSession
{
    public static int Lives { get; set; } = 3;
    public static int Score { get; set; }
    public static int Multiplier { get; set; } = 1;

    public static void Reset()
    {
        Lives = 3;
        Score = 0;
        Multiplier = 1;
    }
}
