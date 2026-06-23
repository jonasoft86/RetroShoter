using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileOwner owner;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0f)] private float speed = 9f;
    [SerializeField] private Vector2 direction = Vector2.up;

    public void Initialize(
        ProjectileOwner projectileOwner,
        int projectileDamage,
        float projectileSpeed,
        Vector2 projectileDirection,
        Sprite sprite)
    {
        owner = projectileOwner;
        damage = projectileDamage;
        speed = projectileSpeed;
        direction = projectileDirection.normalized;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    private void Awake()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Mathf.Abs(transform.position.y) > 7f || Mathf.Abs(transform.position.x) > 7f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == ProjectileOwner.Player && other.TryGetComponent(out Enemy enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (owner == ProjectileOwner.Player && other.TryGetComponent(out BossController boss))
        {
            boss.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (owner == ProjectileOwner.Enemy && other.TryGetComponent(out PlayerHealth player))
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    public static Projectile Create(
        Vector3 position,
        ProjectileOwner owner,
        Vector2 direction,
        float speed,
        int damage,
        Sprite sprite)
    {
        GameObject instance = new(owner == ProjectileOwner.Player ? "Player Projectile" : "Enemy Projectile");
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one * 0.55f;
        instance.AddComponent<SpriteRenderer>().sortingOrder = 10;
        instance.AddComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D collider = instance.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.35f, 0.8f);
        Projectile projectile = instance.AddComponent<Projectile>();
        projectile.Initialize(owner, damage, speed, direction, sprite);
        return projectile;
    }
}
