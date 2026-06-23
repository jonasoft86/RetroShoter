using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0.1f)] private float invincibilityDuration = 1.5f;

    private int currentHealth;
    private bool invincible;
    private SpriteRenderer spriteRenderer;

    public bool IsAlive => currentHealth > 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start() => GameEvents.RaiseHealthChanged(currentHealth, maxHealth);

    public void TakeDamage(int amount)
    {
        if (invincible || !IsAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
        ScoreManager.Instance?.ResetMultiplier();
        AudioManager.Instance?.PlayDamage();
        ScreenShake.Instance?.Shake(0.25f, 0.13f);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
        VFXManager.Instance?.SpawnSmallExplosion(transform.position);

        if (!IsAlive)
        {
            GameManager.Instance?.LoseLife(this);
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        currentHealth = maxHealth;
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
        StartCoroutine(InvincibilityRoutine());
    }

    public void AddHealth(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }

    public void ActivateShield(float duration) =>
        StartCoroutine(TemporaryInvincibility(duration));

    private IEnumerator InvincibilityRoutine() =>
        TemporaryInvincibility(invincibilityDuration);

    private IEnumerator TemporaryInvincibility(float duration)
    {
        invincible = true;
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        spriteRenderer.enabled = true;
        invincible = false;
    }
}
