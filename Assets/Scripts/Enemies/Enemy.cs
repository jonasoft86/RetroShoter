using System.Collections;
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

    private Transform playerTransform;

    // Zigzag
    private bool zigzagEnabled;
    private float zigzagAmplitude;
    private float zigzagFrequency;
    private float zigzagTime;
    private float zigzagOriginX;

    // Sweep
    private bool sweepEnabled;
    private float sweepSpeed;

    // Homing
    private bool homingEnabled;
    private float homingStrength;

    // Bounce
    private bool bounceEnabled;
    private float bounceVelocityX;
    private float bounceLimitLeft;
    private float bounceLimitRight;

    // Dive
    private bool diveEnabled;
    private float diveThresholdY;
    private float diveSpeed;
    private Vector3 divingDirection;
    private bool isDiving;

    // Stop & Shoot
    private bool stopShootEnabled;
    private bool isStopped;
    private float stopThresholdY;
    private float stopDuration;

    // Circle / Orbit
    private bool circleEnabled;
    private Vector3 circleCenter;
    private float circleRadius;
    private float circleAngularSpeed;
    private float circleAngle;
    private float circleAngleTraveled;
    private float circleTotalAngle;
    private bool circleExiting;

    // Linger (pausa con movimiento lateral)
    private bool lingerEnabled;
    private bool isLingering;
    private float lingerThresholdY;
    private float lingerDuration;
    private float lingerLateralSpeed;

    public bool IsAlive => !destroyed && currentHealth > 0;
    public float Speed => data != null ? data.speed : fallbackSpeed;
    public int ScoreValue => data != null ? data.scoreValue : fallbackScore;
    public float DropChance => data != null ? data.dropChance : 0.05f;

    private void Awake()
    {
        currentHealth = data != null ? data.maxHealth : fallbackHealth;
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void SetZigzag(float amplitude, float frequency, float phase)
    {
        zigzagEnabled = true;
        zigzagAmplitude = amplitude;
        zigzagFrequency = frequency;
        zigzagTime = phase;
        zigzagOriginX = transform.position.x;
    }

    public void SetSweep(float horizontalSpeed)
    {
        sweepEnabled = true;
        sweepSpeed = horizontalSpeed;
    }

    public void SetHoming(float strength)
    {
        homingEnabled = true;
        homingStrength = strength;
        CachePlayer();
    }

    public void SetBounce(float speed)
    {
        bounceEnabled = true;
        bounceVelocityX = speed;
        Camera cam = Camera.main;
        bounceLimitLeft = cam != null ? cam.ViewportToWorldPoint(new Vector3(0.05f, 0f, 0f)).x : -4f;
        bounceLimitRight = cam != null ? cam.ViewportToWorldPoint(new Vector3(0.95f, 0f, 0f)).x : 4f;
    }

    public void SetDive(float thresholdY, float speed)
    {
        diveEnabled = true;
        diveThresholdY = thresholdY;
        diveSpeed = speed;
        CachePlayer();
    }

    public void SetStopAndShoot(float thresholdY, float duration)
    {
        stopShootEnabled = true;
        stopThresholdY = thresholdY;
        stopDuration = duration;
    }

    // Enemigos orbitan alrededor del centro de pantalla N vueltas, luego salen hacia abajo.
    public void SetCircle(Vector3 center, float angularSpeed, float orbits)
    {
        circleEnabled = true;
        circleCenter = center;
        Vector3 dir = transform.position - center;
        circleRadius = dir.magnitude;
        circleAngularSpeed = angularSpeed;
        circleTotalAngle = orbits * Mathf.PI * 2f;
        circleAngle = Mathf.Atan2(dir.y, dir.x);
        circleAngleTraveled = 0f;
        circleExiting = false;
    }

    // Entra, se detiene en el umbral Y y se mueve lateralmente durante 'duration' segundos, luego continúa.
    public void SetLinger(float thresholdY, float duration, float lateralSpeed)
    {
        lingerEnabled = true;
        lingerThresholdY = thresholdY;
        lingerDuration = duration;
        lingerLateralSpeed = lateralSpeed;
    }

    private void CachePlayer()
    {
        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null) playerTransform = ph.transform;
    }

    private void Update()
    {
        // Triggers de pausa
        if (stopShootEnabled && !isStopped && transform.position.y <= stopThresholdY)
            StartCoroutine(StopRoutine());
        if (lingerEnabled && !isLingering && transform.position.y <= lingerThresholdY)
            StartCoroutine(LingerRoutine());

        if (isStopped || isLingering)
        {
            if (transform.position.y < -6.5f) Destroy(gameObject);
            return;
        }

        // Trigger de picado
        if (diveEnabled && !isDiving && transform.position.y <= diveThresholdY)
        {
            isDiving = true;
            Vector3 target = playerTransform != null
                ? playerTransform.position
                : transform.position + Vector3.down * 10f;
            divingDirection = (target - transform.position).normalized;
        }

        if (isDiving)
        {
            transform.position += divingDirection * diveSpeed * Time.deltaTime;
        }
        else if (circleEnabled)
        {
            if (!circleExiting)
            {
                float delta = circleAngularSpeed * Time.deltaTime;
                circleAngle -= delta;
                circleAngleTraveled += delta;
                transform.position = new Vector3(
                    circleCenter.x + Mathf.Cos(circleAngle) * circleRadius,
                    circleCenter.y + Mathf.Sin(circleAngle) * circleRadius,
                    0f);
                if (circleAngleTraveled >= circleTotalAngle)
                    circleExiting = true;
            }
            else
            {
                transform.position += Vector3.down * Speed * Time.deltaTime;
            }
        }
        else
        {
            transform.Translate(Vector3.up * Speed * Time.deltaTime);

            if (zigzagEnabled)
            {
                zigzagTime += Time.deltaTime * zigzagFrequency;
                float t = Mathf.PingPong(zigzagTime, 1f);
                transform.position = new Vector3(zigzagOriginX + t * zigzagAmplitude,
                                                 transform.position.y, 0f);
            }
            else if (sweepEnabled)
            {
                transform.position += new Vector3(sweepSpeed * Time.deltaTime, 0f, 0f);
            }
            else if (homingEnabled && playerTransform != null)
            {
                float dx = (playerTransform.position.x - transform.position.x)
                           * homingStrength * Time.deltaTime;
                transform.position += new Vector3(dx, 0f, 0f);
            }
            else if (bounceEnabled)
            {
                transform.position += new Vector3(bounceVelocityX * Time.deltaTime, 0f, 0f);
                if (transform.position.x >= bounceLimitRight)
                {
                    transform.position = new Vector3(bounceLimitRight, transform.position.y, 0f);
                    bounceVelocityX = -Mathf.Abs(bounceVelocityX);
                }
                else if (transform.position.x <= bounceLimitLeft)
                {
                    transform.position = new Vector3(bounceLimitLeft, transform.position.y, 0f);
                    bounceVelocityX = Mathf.Abs(bounceVelocityX);
                }
            }
        }

        bool offBottom = transform.position.y < -6.5f && (!circleEnabled || circleExiting);
        bool offSide = isDiving && (transform.position.x < -10f || transform.position.x > 10f);
        if (offBottom || offSide)
            Destroy(gameObject);
    }

    private IEnumerator StopRoutine()
    {
        isStopped = true;
        yield return new WaitForSeconds(stopDuration);
        isStopped = false;
        stopShootEnabled = false;
    }

    private IEnumerator LingerRoutine()
    {
        isLingering = true;
        Camera cam = Camera.main;
        float limitLeft = cam != null ? cam.ViewportToWorldPoint(new Vector3(0.08f, 0f, 0f)).x : -4f;
        float limitRight = cam != null ? cam.ViewportToWorldPoint(new Vector3(0.92f, 0f, 0f)).x : 4f;
        float dir = Random.value > 0.5f ? 1f : -1f;
        float elapsed = 0f;
        while (elapsed < lingerDuration)
        {
            float newX = transform.position.x + lingerLateralSpeed * dir * Time.deltaTime;
            if (newX >= limitRight) { newX = limitRight; dir = -1f; }
            else if (newX <= limitLeft) { newX = limitLeft; dir = 1f; }
            transform.position = new Vector3(newX, transform.position.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isLingering = false;
        lingerEnabled = false;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
            return;

        currentHealth -= Mathf.Max(1, amount);
        if (currentHealth <= 0)
            Die(true);
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
            return;

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
