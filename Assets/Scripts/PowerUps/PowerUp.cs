using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerUp : MonoBehaviour
{
    [SerializeField] private PowerUpData data;
    [SerializeField] private PowerUpType fallbackType;
    [SerializeField] private float fallbackDuration = 5f;

    public void Initialize(PowerUpData powerUpData)
    {
        data = powerUpData;
        if (data != null)
        {
            GetComponent<SpriteRenderer>().sprite = data.sprite;
        }
    }

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;
    private void Update()
    {
        transform.position += Vector3.down * 1.4f * Time.deltaTime;
        if (transform.position.y < -6.5f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth health))
        {
            return;
        }

        PowerUpType type = data != null ? data.type : fallbackType;
        float duration = data != null ? data.duration : fallbackDuration;
        switch (type)
        {
            case PowerUpType.WeaponUpgrade:
                other.GetComponent<PlayerWeapon>()?.Upgrade();
                break;
            case PowerUpType.Shield:
                health.ActivateShield(duration);
                break;
            case PowerUpType.SpeedBoost:
                PowerUpManager.Instance?.ApplySpeedBoost(other.GetComponent<PlayerController>(), duration);
                break;
            case PowerUpType.ExtraLife:
                GameManager.Instance?.AddLife();
                break;
        }
        AudioManager.Instance?.PlayPowerUp();
        float displayDuration = type == PowerUpType.ExtraLife ? 2f : duration;
        GameEvents.RaisePowerUpCollected(type, displayDuration);
        Destroy(gameObject);
    }
}
