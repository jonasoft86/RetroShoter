using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Text highScoreText;

    private void Awake()
    {
        if (highScoreText == null)
            BuildRuntimeUI();
    }

    private void Start()
    {
        if (highScoreText != null)
            highScoreText.text = $"◆  HIGH SCORE  {PlayerPrefs.GetInt("HighScore", 0):000000}  ◆";
    }

    private void Update()
    {
        // Guard: don't trigger Play when user is clicking/tapping a UI button
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        bool key   = Keyboard.current    != null && Keyboard.current.enterKey.wasPressedThisFrame;
        // WebGL mobile: browsers forward touches as Mouse events, not Touchscreen
        bool touch = !overUI && Touchscreen.current != null
                              && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool click = !overUI && Mouse.current != null
                              && Mouse.current.leftButton.wasPressedThisFrame;
        if (key || touch || click) Play();
    }

    public void Play()
    {
        RunSession.Reset();
        SceneManager.LoadScene("Level01_DeepSpace");
    }

    public void Quit() => Application.Quit();

    private void BuildRuntimeUI()
    {
        Canvas canvas = RuntimeScreenUI.CreateCanvas("Menu Canvas");
        canvas.gameObject.AddComponent<CanvasGroup>();
        MenuAnimator animator = canvas.gameObject.AddComponent<MenuAnimator>();

        // Version label — corner of full canvas (outside safe area intentionally)
        RuntimeScreenUI.CreateCornerLabel(canvas.transform, "v1.0",
            new Vector2(1f, 1f), new Vector2(-12f, -10f), 11,
            new Color(0.20f, 0.38f, 0.55f));

        // Safe-area panel — all game UI sits here to avoid notches / home bars
        Transform safe = RuntimeScreenUI.CreateSafeAreaPanel(canvas.transform);

        // Marco del título (estira horizontalmente)
        RuntimeScreenUI.CreateTitleFrame(safe, 0.76f);

        // Texto del título
        Text title = RuntimeScreenUI.CreateStretchLabel(safe,
            "RETRO\nSPACE SHOOTER",
            0.05f, 0.95f, 0.76f, 148f, 44, new Color(0.10f, 0.72f, 1f));
        title.gameObject.name = "Title";

        // Separador
        RuntimeScreenUI.CreateStretchSeparator(safe, 0.12f, 0.88f, 0.615f);

        // High score
        highScoreText = RuntimeScreenUI.CreateLabel(safe,
            "◆  HIGH SCORE  000000  ◆",
            new Vector2(0.5f, 0.565f), new Vector2(370f, 38f), 19,
            new Color(1f, 0.88f, 0.42f));

        // Botones — 70 px de alto para touch targets cómodos
        RuntimeScreenUI.CreateStretchGlowButton(safe,
            "▶  PLAY GAME", 0.10f, 0.90f, 0.445f, 70f, Play);

        // Application.Quit() is a no-op in WebGL — hide the QUIT button there
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            RuntimeScreenUI.CreateStretchGlowButton(safe,
                "QUIT", 0.10f, 0.90f, 0.325f, 70f, Quit);
        }

        // isMobilePlatform returns false in WebGL mobile browsers — use combined hint
        const string blinkMsg = "— TAP  /  ENTER  TO  START —";
        Text blink = RuntimeScreenUI.CreateLabel(safe,
            blinkMsg,
            new Vector2(0.5f, 0.195f), new Vector2(380f, 28f), 14,
            new Color(0.50f, 0.82f, 1f));
        blink.gameObject.name = "Blink";

        // Controles (parte baja)
        RuntimeScreenUI.CreateLabel(safe,
            "WASD / ARROWS / TOUCH  •  SPACE / FIRE",
            new Vector2(0.5f, 0.085f), new Vector2(450f, 28f), 12,
            new Color(0.32f, 0.50f, 0.65f));

        // Scanlines CRT — sobre el canvas completo, último en renderizar
        RuntimeScreenUI.CreateScanlineOverlay(canvas.transform);

        animator.titleText = title;
        animator.blinkText  = blink;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
public static class RuntimeScreenUI
{
    // ── Canvas (responsive) ──────────────────────────────────────────────────
    public static Canvas CreateCanvas(string name)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 640f);
        // Expand: canvas is always AT LEAST 480×640 units — portrait gets extra height,
        // landscape gets extra width. Prevents anchor-fraction elements from overflowing
        // on WebGL landscape or ultra-tall phones.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        go.AddComponent<GraphicRaycaster>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
        return canvas;
    }

    // ── Panel ajustado al safe area del dispositivo ──────────────────────────
    public static Transform CreateSafeAreaPanel(Transform canvasTransform)
    {
        var go = new GameObject("Safe Area");
        go.transform.SetParent(canvasTransform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<SafeAreaFitter>();
        return go.transform;
    }

    // ── Label (anchor puntual — centrado) ────────────────────────────────────
    public static Text CreateLabel(
        Transform parent, string content, Vector2 anchor,
        Vector2 size, int fontSize, Color color)
    {
        var go   = new GameObject(content.Replace("\n", " "));
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        return AddText(go, content, fontSize, TextAnchor.MiddleCenter, color);
    }

    // ── Label anclado a una esquina (versión, créditos, etc.) ────────────────
    public static Text CreateCornerLabel(
        Transform parent, string content,
        Vector2 cornerAnchor, Vector2 offset,
        int fontSize, Color color)
    {
        var go   = new GameObject(content);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = cornerAnchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta        = new Vector2(70f, 22f);
        return AddText(go, content, fontSize, TextAnchor.UpperRight, color);
    }

    // ── Label que estira horizontalmente ─────────────────────────────────────
    public static Text CreateStretchLabel(
        Transform parent, string content,
        float minX, float maxX, float anchorY, float height,
        int fontSize, Color color)
    {
        var go   = new GameObject(content.Replace("\n", " "));
        go.transform.SetParent(parent, false);
        SetHStretch(go.AddComponent<RectTransform>(), minX, maxX, anchorY, height);
        return AddText(go, content, fontSize, TextAnchor.MiddleCenter, color);
    }

    // ── Marco del título: glow → borde cyan → fill oscuro ────────────────────
    public static void CreateTitleFrame(Transform parent, float anchorY)
    {
        CreateStretchPanel(parent, 0.02f, 0.98f, anchorY, 176f,
            new Color(0.08f, 0.55f, 1f, 0.12f));
        CreateStretchPanel(parent, 0.03f, 0.97f, anchorY, 162f,
            new Color(0.18f, 0.68f, 1f, 0.65f));
        CreateStretchPanel(parent, 0.033f, 0.967f, anchorY, 158f,
            new Color(0.01f, 0.04f, 0.14f, 0.94f));
    }

    // ── Separador horizontal (estira) ─────────────────────────────────────────
    public static void CreateStretchSeparator(
        Transform parent, float minX, float maxX, float anchorY)
    {
        CreateStretchPanel(parent, minX, maxX, anchorY, 2f,
            new Color(0.22f, 0.60f, 1f, 0.55f));
    }

    // ── Botón con glow (estira horizontalmente) ──────────────────────────────
    public static Button CreateStretchGlowButton(
        Transform parent, string label,
        float minX, float maxX, float anchorY, float height,
        UnityEngine.Events.UnityAction action = null)
    {
        CreateStretchPanel(parent, minX - 0.012f, maxX + 0.012f, anchorY, height + 14f,
            new Color(0.12f, 0.48f, 1f, 0.16f));

        var go   = new GameObject($"{label} Button");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        SetHStretch(rect, minX, maxX, anchorY, height);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.03f, 0.09f, 0.21f, 0.97f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var cb = button.colors;
        cb.normalColor      = new Color(0.03f, 0.09f, 0.21f, 0.97f);
        cb.highlightedColor = new Color(0.07f, 0.27f, 0.58f, 1f);
        cb.pressedColor     = new Color(0.16f, 0.52f, 1f,    1f);
        cb.selectedColor    = cb.highlightedColor;
        cb.colorMultiplier  = 1f;
        button.colors = cb;
        if (action != null) button.onClick.AddListener(action);

        CreateButtonLine(go.transform, anchorEdge: 1f, lineHeight: 2f,
            new Color(0.28f, 0.65f, 1f, 0.90f));
        CreateButtonLine(go.transform, anchorEdge: 0f, lineHeight: 1.5f,
            new Color(0.18f, 0.48f, 0.80f, 0.40f));

        var textGo   = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f,  0f);
        textRect.offsetMax = new Vector2(-12f, 0f);
        var text = AddText(textGo, label, 21, TextAnchor.MiddleCenter,
            new Color(0.88f, 0.95f, 1f));
        text.raycastTarget = false;

        return button;
    }

    // ── Botón plano (EndScreen) — touch-friendly: 320 × 70 px ───────────────
    public static Button CreateButton(
        Transform parent, string label, Vector2 anchor,
        UnityEngine.Events.UnityAction action)
    {
        var go   = new GameObject($"{label} Button");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = new Vector2(320f, 70f);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.32f, 0.95f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = CreateLabel(go.transform, label, new Vector2(0.5f, 0.5f),
            new Vector2(300f, 66f), 23, Color.white);
        text.raycastTarget = false;
        return button;
    }

    // ── Scanlines CRT (siempre crear el último — renderiza sobre todo) ────────
    public static void CreateScanlineOverlay(Transform parent)
    {
        var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Repeat
        };
        tex.SetPixels(new[]
        {
            Color.clear, Color.clear,
            new Color(0f, 0f, 0f, 0.18f),
            new Color(0f, 0f, 0f, 0.18f)
        });
        tex.Apply();

        var go = new GameObject("Scanlines");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<RawImage>();
        img.texture       = tex;
        img.uvRect        = new Rect(0f, 0f, 1f, 160f);
        img.color         = Color.white;
        img.raycastTarget = false;
    }

    // ── Helpers internos ─────────────────────────────────────────────────────

    private static Image CreateStretchPanel(
        Transform parent, float minX, float maxX, float anchorY,
        float height, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        SetHStretch(rt, minX, maxX, anchorY, height);
        var img = go.AddComponent<Image>();
        img.color        = color;
        img.raycastTarget = false;
        return img;
    }

    private static void CreateButtonLine(
        Transform parent, float anchorEdge, float lineHeight, Color color)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorEdge);
        rt.anchorMax = new Vector2(1f, anchorEdge);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, -lineHeight * 0.5f);
        rt.offsetMax = new Vector2(0f,  lineHeight * 0.5f);
        var img = go.AddComponent<Image>();
        img.color        = color;
        img.raycastTarget = false;
    }

    private static void SetHStretch(
        RectTransform rt, float minX, float maxX, float anchorY, float height)
    {
        rt.anchorMin = new Vector2(minX, anchorY);
        rt.anchorMax = new Vector2(maxX, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, -height * 0.5f);
        rt.offsetMax = new Vector2(0f,  height * 0.5f);
    }

    private static Text AddText(
        GameObject go, string content, int fontSize,
        TextAnchor alignment, Color color)
    {
        var text = go.AddComponent<Text>();
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color     = color;
        text.text      = content;
        return text;
    }
}
