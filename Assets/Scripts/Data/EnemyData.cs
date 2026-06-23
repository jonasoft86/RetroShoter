using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    [Min(1)] public int maxHealth = 1;
    [Min(0f)] public float speed = 2f;
    [Min(0)] public int scoreValue = 100;
    [Min(0)] public int contactDamage = 1;
    [Min(0f)] public float fireRate;
    [Range(0f, 1f)] public float dropChance = 0.05f;
    public Projectile projectilePrefab;
}
