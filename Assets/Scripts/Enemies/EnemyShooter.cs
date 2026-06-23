using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private Sprite projectileSprite;
    [SerializeField, Min(0.2f)] private float fireInterval = 2f;
    [SerializeField, Min(1)] private int damage = 1;
    private float nextShotTime;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing ||
            Time.time < nextShotTime)
        {
            return;
        }

        nextShotTime = Time.time + fireInterval;
        Vector2 direction = Vector2.down;
        PlayerHealth target = FindFirstObjectByType<PlayerHealth>();
        if (target != null)
        {
            direction = (target.transform.position - transform.position).normalized;
        }
        Projectile.Create(transform.position, ProjectileOwner.Enemy, direction, 5f, damage, projectileSprite);
        AudioManager.Instance?.PlayShot();
    }
}
