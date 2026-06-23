using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData data;
    [SerializeField, Min(1)] private int fallbackHealth = 1;
    [SerializeField, Min(0f)] private float fallbackSpeed = 5.2f;
    [SerializeField, Min(0)] private int fallbackScore = 100;
    [SerializeField, Min(0)] private int fallbackContactDamage = 1;

    private int currentHealth;
    private bool destroyed;

    public bool IsAlive => !destroyed && currentHealth > 0;
    public float Speed => data != null ? data.speed : fallbackSpeed;
    public int ScoreValue => data != null ? data.scoreValue : fallbackScore;
    public float DropChance => data != null ? data.dropChance : 0.05f;

    private void Awake()
    {
        currentHealth = data != null ? data.maxHealth : fallbackHealth;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        transform.Translate(Vector3.up * Speed * Time.deltaTime);
        if (transform.position.y < -6.5f)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        currentHealth -= Mathf.Max(1, amount);
        if (currentHealth <= 0)
        {
            Die(true);
        }
    }

    public void DestroyWithoutScore() => Die(false);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            int damage = data != null ? data.contactDamage : fallbackContactDamage;
            player.TakeDamage(damage);
            Die(false);
        }
    }

    private void Die(bool awardScore)
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;
        if (awardScore)
        {
            ScoreManager.Instance?.AddScore(ScoreValue);
            PowerUpManager.Instance?.TryDrop(transform.position, DropChance);
        }
        VFXManager.Instance?.SpawnSmallExplosion(transform.position);
        AudioManager.Instance?.PlayExplosion();
        Destroy(gameObject);
    }
}
