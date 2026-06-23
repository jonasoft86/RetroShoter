using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private Vector2 viewportPadding = new(0.06f, 0.08f);

    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2     moveInput;
    private Camera      mainCamera;
    private MobileJoystick joystick;

    /// True when a right-side touch (or WebGL auto-fire) is active.
    /// Read by PlayerWeapon to avoid duplicating touch tracking.
    public bool FireTouchActive { get; private set; }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale   = 0f;
        body.freezeRotation = true;
        mainCamera = Camera.main;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w").With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow").With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        joystick = MobileJoystick.Create();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        EnhancedTouchSupport.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        if (joystick != null) Destroy(joystick.gameObject);
    }

    // ── Per-frame ─────────────────────────────────────────────────────────────
    private void Update()
    {
        moveInput       = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        FireTouchActive = false;
        ReadMobileInput();
    }

    private void FixedUpdate()
    {
        Vector2 next = body.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        if (mainCamera != null)
        {
            Vector3 mn = mainCamera.ViewportToWorldPoint(viewportPadding);
            Vector3 mx = mainCamera.ViewportToWorldPoint(Vector2.one - viewportPadding);
            next.x = Mathf.Clamp(next.x, mn.x, mx.x);
            next.y = Mathf.Clamp(next.y, mn.y, mx.y);
        }
        body.MovePosition(next);
    }

    // ── Mobile input ──────────────────────────────────────────────────────────

    private void ReadMobileInput()
    {
        // Native touch (Android / iOS)
        if (Touch.activeTouches.Count > 0)
        {
            ReadEnhancedTouch();
            return;
        }

        // WebGL / desktop pointer — skip if keyboard is already steering
        bool keyboardSteering = moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f;
        if (!keyboardSteering && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            ReadMousePointer();
            return;
        }

        joystick.Hide();
    }

    // Multi-touch: first active touch (anywhere) = move, any additional touch = fire.
    // Movement delta is relative to the SHIP's screen position so the
    // joystick ring always centers on the player, and the full screen width is usable.
    private void ReadEnhancedTouch()
    {
        Vector2 shipScreen = mainCamera.WorldToScreenPoint(transform.position);
        bool moveTouchFound = false;

        foreach (var t in Touch.activeTouches)
        {
            if (!moveTouchFound)
            {
                moveTouchFound = true;
                ApplyMoveDelta(t.screenPosition, shipScreen);
                FireTouchActive = true; // auto-fire while touching
            }
        }

        if (!moveTouchFound)
        {
            // Keep keyboard/gamepad input; clear touch-only input
            if (moveAction.ReadValue<Vector2>().sqrMagnitude < 0.01f)
                moveInput = Vector2.zero;
            joystick.Hide();
        }
    }

    // WebGL mobile: single pointer → move toward finger, auto-fire.
    private void ReadMousePointer()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector2 shipScreen  = mainCamera.WorldToScreenPoint(transform.position);
        ApplyMoveDelta(mouseScreen, shipScreen);
        FireTouchActive = true;
    }

    // Shared delta calculation + joystick visual update.
    // fingerScreen — current finger/mouse position in screen pixels
    // shipScreen   — ship's current position in screen pixels (joystick ring center)
    private void ApplyMoveDelta(Vector2 fingerScreen, Vector2 shipScreen)
    {
        float radius = joystick.ScreenRadius;
        Vector2 delta = fingerScreen - shipScreen;

        moveInput = Vector2.ClampMagnitude(delta / radius, 1f);

        // Clamp knob to ring boundary for the visual
        Vector2 clampedKnob = shipScreen + Vector2.ClampMagnitude(delta, radius);
        joystick.Show(shipScreen, clampedKnob);
    }
}
