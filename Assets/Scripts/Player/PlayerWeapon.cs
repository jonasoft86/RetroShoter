using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] private Sprite projectileSprite;
    [SerializeField, Min(0.05f)] private float fireInterval = 0.2f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private int weaponLevel = 1;

    private InputAction      fireAction;
    private float            nextShotTime;
    private PlayerController controller;

    public void Initialize(Sprite sprite) => projectileSprite = sprite;
    public void Upgrade() => weaponLevel = Mathf.Min(3, weaponLevel + 1);

    private void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button, "<Keyboard>/space");
        fireAction.AddBinding("<Gamepad>/buttonSouth");
        // Touch fire is routed through PlayerController.FireTouchActive (multi-touch safe)
        controller = GetComponent<PlayerController>();
    }

    private void OnEnable()  => fireAction.Enable();
    private void OnDisable() => fireAction.Disable();
    private void OnDestroy() => fireAction.Dispose();

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            return;

        bool fire = fireAction.IsPressed()
                 || (controller != null && controller.FireTouchActive);

        if (fire && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireInterval;
            Fire();
        }
    }

    private void Fire()
    {
        Vector3 origin = transform.position + Vector3.up * 0.85f;
        Projectile.Create(origin, ProjectileOwner.Player, Vector2.up, 9f, damage, projectileSprite);
        AudioManager.Instance?.PlayShot();

        if (weaponLevel >= 2)
        {
            Projectile.Create(origin + Vector3.left  * 0.25f, ProjectileOwner.Player,
                new Vector2(-0.18f, 1f), 9f, damage, projectileSprite);
            Projectile.Create(origin + Vector3.right * 0.25f, ProjectileOwner.Player,
                new Vector2(0.18f, 1f), 9f, damage, projectileSprite);
        }
    }
}
