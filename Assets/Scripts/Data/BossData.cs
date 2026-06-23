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
}
