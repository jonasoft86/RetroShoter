using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName = "Space Mothership";
    [Min(1)] public int maxHealth = 100;
    [Min(1)] public int phaseCount = 3;
    [Min(0)] public int scoreValue = 3000;
    [Min(0.1f)] public float attackInterval = 2f;
    public BossAttackPattern attackPattern = BossAttackPattern.Spread;
    [Min(1)] public int projectileCount = 5;
    [Min(0.1f)] public float projectileSpeed = 4.5f;
    public Color hitFlashColor = Color.white;

    [Header("Movement")]
    [Min(0f)] public float patrolSpeed = 1.5f;
    [Range(0f, 0.5f)] public float patrolMarginX = 0.12f;
    public float entryY = 3.0f;
    [Min(0.5f)] public float entrySpeed = 2f;
}
