using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class BossController : MonoBehaviour, IDamageable
{
    [SerializeField] private BossData data;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Animator animator;

    private int currentHealth;
    private bool defeated;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float spiralAngle;

    public bool IsAlive => !defeated && currentHealth > 0;

    private void Awake()
    {
        currentHealth = data != null ? data.maxHealth : 100;
        animator ??= GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    private void Start()
    {
        GameEvents.RaiseBossVisibilityChanged(true);
        RaiseHealth();
        StartCoroutine(AttackLoop());
        StartCoroutine(MoveLoop());
    }

    private void OnDestroy() => GameEvents.RaiseBossVisibilityChanged(false);

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
        RaiseHealth();
        StartCoroutine(HitFeedback());
        ScreenShake.Instance?.Shake(0.12f, 0.08f);
        AudioManager.Instance?.PlayBossHit();

        if (currentHealth <= 0)
        {
            defeated = true;
            ScoreManager.Instance?.AddScore(data != null ? data.scoreValue : 3000);
            VFXManager.Instance?.SpawnBigExplosion(transform.position);
            ScreenShake.Instance?.Shake(0.7f, 0.3f);
            AudioManager.Instance?.PlayBossDeath();
            GameManager.Instance?.CompleteLevel();
            Destroy(gameObject);
        }
    }

    private void RaiseHealth() => GameEvents.RaiseBossHealthChanged(
        data != null ? data.bossName : "BOSS",
        currentHealth,
        data != null ? data.maxHealth : 100);

    private IEnumerator HitFeedback()
    {
        spriteRenderer.color = data != null ? data.hitFlashColor : Color.white;
        yield return new WaitForSeconds(0.08f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1f);
        while (IsAlive)
        {
            animator?.SetTrigger("Attack");
            FirePattern();
            yield return new WaitForSeconds(data != null ? data.attackInterval : 2f);
        }
    }

    private IEnumerator MoveLoop()
    {
        float speed      = data != null ? data.patrolSpeed    : 1.5f;
        float marginX    = data != null ? data.patrolMarginX  : 0.12f;
        float targetEntryY  = data != null ? data.entryY      : 3.0f;
        float entrySpeed = data != null ? data.entrySpeed     : 2f;

        Camera cam = Camera.main;
        float leftBound  = cam != null ? cam.ViewportToWorldPoint(new Vector3(marginX,       0f, 0f)).x : -3.5f;
        float rightBound = cam != null ? cam.ViewportToWorldPoint(new Vector3(1f - marginX, 0f, 0f)).x :  3.5f;

        // Descend to entry row if spawned above it
        while (IsAlive && transform.position.y > targetEntryY)
        {
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, targetEntryY, entrySpeed * Time.deltaTime);
            transform.position = p;
            yield return null;
        }

        if (!IsAlive || speed <= 0f) yield break;

        // Patrol left <-> right
        float targetX = rightBound;
        while (IsAlive)
        {
            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, targetX, speed * Time.deltaTime);
            transform.position = p;
            if (Mathf.Abs(p.x - targetX) < 0.02f)
                targetX = (targetX >= rightBound) ? leftBound : rightBound;
            yield return null;
        }
    }

    private void FirePattern()
    {
        BossAttackPattern pattern = data != null ? data.attackPattern : BossAttackPattern.Spread;
        int count = data != null ? data.projectileCount : 5;
        float speed = data != null ? data.projectileSpeed : 4.5f;

        switch (pattern)
        {
            case BossAttackPattern.AimedBurst:
                PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
                Vector2 aimed = player != null
                    ? (player.transform.position - transform.position).normalized
                    : Vector2.down;
                for (int index = 0; index < count; index++)
                {
                    Vector2 direction = Quaternion.Euler(0f, 0f, (index - count / 2f) * 5f) * aimed;
                    Projectile.Create(transform.position, ProjectileOwner.Enemy, direction, speed, 1, projectileSprite);
                }
                break;

            case BossAttackPattern.Spiral:
                for (int index = 0; index < count; index++)
                {
                    float angle = spiralAngle + index * (360f / count);
                    Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
                    Projectile.Create(transform.position, ProjectileOwner.Enemy, direction, speed, 1, projectileSprite);
                }
                spiralAngle += 24f;
                break;

            default:
                for (int index = 0; index < count; index++)
                {
                    float t = count <= 1 ? 0.5f : index / (float)(count - 1);
                    float angle = Mathf.Lerp(-38f, 38f, t);
                    Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
                    Projectile.Create(transform.position, ProjectileOwner.Enemy, direction, speed, 1, projectileSprite);
                }
                break;
        }
    }
}
