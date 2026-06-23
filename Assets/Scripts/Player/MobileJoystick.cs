using UnityEngine;
using UnityEngine.UI;

// Screen-space joystick overlay.
// Outer ring is centered at the ship's current screen position so the
// visual always "sticks to the player". Inner knob tracks the finger.
public class MobileJoystick : MonoBehaviour
{
    // Screen-pixel radius that equals full deflection (set in Start after canvas init).
    public float ScreenRadius { get; private set; }

    private Canvas        _canvas;
    private RectTransform _canvasRt;
    private RectTransform _knobRt;

    // Canvas-unit sizes (scaled by CanvasScaler automatically)
    private const float RingDiamU = 130f;
    private const float KnobDiamU =  52f;

    // ── Factory ───────────────────────────────────────────────────────────────
    public static MobileJoystick Create()
    {
        var go = new GameObject("_MobileJoystick");
        return go.AddComponent<MobileJoystick>();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;  // above HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 640f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.Expand;

        _canvasRt = GetComponent<RectTransform>();

        // Knob only — no outer ring
        _knobRt = MakeImage("Knob", BuildDisk(64,  new Color(0.28f, 0.72f, 1f, 0.72f)), KnobDiamU);

        // Pre-set a safe fallback radius before Start()
        ScreenRadius = Screen.height * 0.10f;

        gameObject.SetActive(false);
    }

    private void Start()
    {
        // scaleFactor is valid from first frame — sync radius to actual ring size
        ScreenRadius = (RingDiamU * 0.5f) * _canvas.scaleFactor;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Call every frame while a movement touch is active.
    // ringScreen  = player's current screen position (ring center)
    // knobScreen  = clamped finger position (within ring)
    public void Show(Vector2 ringScreen, Vector2 knobScreen)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        ToCanvas(knobScreen, _knobRt);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private RectTransform MakeImage(string goName, Texture2D tex, float diamU)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(diamU, diamU);
        var img = go.AddComponent<RawImage>();
        img.texture       = tex;
        img.raycastTarget = false;
        return rt;
    }

    // Converts a screen-space position to canvas local position and sets anchoredPosition.
    private void ToCanvas(Vector2 screenPos, RectTransform rt)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, screenPos, null, out var local))
            rt.anchoredPosition = local;
    }

    // ── Texture generators ────────────────────────────────────────────────────

    private static Texture2D BuildDisk(int size, Color col)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear };
        float r = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - r + .5f) * (x - r + .5f) + (y - r + .5f) * (y - r + .5f));
            float a = Mathf.Clamp01((r - d) * 2f) * col.a;
            px[y * size + x] = Tint(col, a);
        }
        tex.SetPixels32(px); tex.Apply(); return tex;
    }

    private static Color32 Tint(Color c, float a) =>
        new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(a * 255));
}
