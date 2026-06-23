using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HUDBuilder
{
    private const string PrefabPath   = "Assets/Prefabs/UI/HUD.prefab";
    private const string ScanlinesTex = "Assets/Textures/UI/HUDScanlines.asset";

    [MenuItem("Tools/Retro Space Shooter/Rebuild HUD (Arcade Full)")]
    public static void Build()
    {
        // Texture must be a saved asset so the prefab serializes the reference correctly.
        // In-memory Texture2D references are dropped when a prefab is saved.
        Texture2D scanlines = EnsureScanlineTexture();

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[HUDBuilder] Prefab not found: {PrefabPath}");
            return;
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        if (root.TryGetComponent<CanvasScaler>(out var scaler))
        {
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 640f);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        Object.DestroyImmediate(root.GetComponent<UIManager>());
        UIManager mgr = root.AddComponent<UIManager>();

        Transform t = root.transform;

        // ── Top panel ─────────────────────────────────────────────────────
        HStretch(t, "Top BG",
            new Vector2(0f, -68f), Vector2.zero,
            new Color(0.012f, 0.030f, 0.085f, 0.94f));

        HStretch(t, "Top Border",
            new Vector2(0f, -69.5f), new Vector2(0f, -68f),
            new Color(0.18f, 0.52f, 1f, 0.50f));

        // ── LEFT: Score + HP ──────────────────────────────────────────────
        Label(t, "Score Label", "SCORE",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -6f), new Vector2(70f, 13f),
            8, new Color(0.22f, 0.55f, 0.85f), TextAnchor.UpperLeft);

        Text scoreText = Label(t, "Score Value", "000000",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -20f), new Vector2(165f, 30f),
            21, new Color(0.18f, 0.88f, 1f), TextAnchor.UpperLeft);

        Label(t, "HP Label", "HP",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -55f), new Vector2(20f, 12f),
            8, new Color(0.22f, 0.52f, 0.72f), TextAnchor.UpperLeft);

        var hpTrackGo = new GameObject("HP Track");
        hpTrackGo.transform.SetParent(t, false);
        var hpTrackRt = hpTrackGo.AddComponent<RectTransform>();
        hpTrackRt.anchorMin        = hpTrackRt.anchorMax = new Vector2(0f, 1f);
        hpTrackRt.pivot            = new Vector2(0f, 1f);
        hpTrackRt.anchoredPosition = new Vector2(32f, -51f);
        hpTrackRt.sizeDelta        = new Vector2(100f, 9f);
        var hpTrackImg = hpTrackGo.AddComponent<Image>();
        hpTrackImg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);
        hpTrackImg.raycastTarget = false;
        var hpOutline = hpTrackGo.AddComponent<Outline>();
        hpOutline.effectColor    = new Color(0.12f, 0.35f, 0.65f, 0.60f);
        hpOutline.effectDistance = new Vector2(1f, -1f);

        var hpFillGo = new GameObject("HP Fill");
        hpFillGo.transform.SetParent(hpTrackGo.transform, false);
        var hpFillRt = hpFillGo.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;
        var hpFillImg = hpFillGo.AddComponent<Image>();
        hpFillImg.color = new Color(0f, 0.80f, 0.35f);
        hpFillImg.raycastTarget = false;

        // ── CENTER: Multiplier + Wave ─────────────────────────────────────
        Text multText = Label(t, "Mult Text", "x1",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -3f), new Vector2(110f, 35f),
            24, new Color(1f, 0.10f, 0.85f), TextAnchor.UpperCenter);

        Label(t, "Wave Text", "WAVE --",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -41f), new Vector2(130f, 18f),
            9, new Color(0.22f, 0.42f, 0.60f), TextAnchor.UpperCenter);

        // ── RIGHT: Lives + High Score ─────────────────────────────────────
        Text heartsText = Label(t, "Lives Hearts", "♥ ♥ ♥",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -5f), new Vector2(90f, 22f),
            15, new Color(1f, 0.25f, 0.35f), TextAnchor.UpperRight);

        Label(t, "High Label", "HIGH SCORE",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -30f), new Vector2(120f, 14f),
            8, new Color(0.60f, 0.50f, 0.15f), TextAnchor.UpperRight);

        Text highText = Label(t, "High Value", "000000",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -45f), new Vector2(140f, 20f),
            14, new Color(1f, 0.85f, 0.28f), TextAnchor.UpperRight);

        // ── BOSS BAR: vertical strip on the right edge ───────────────────
        var (bossRoot, bossSlider, bossNameText) = BuildVerticalBossBar(t);

        // ── GAME STATE OVERLAY ────────────────────────────────────────────
        var stateGo = new GameObject("State Text");
        stateGo.transform.SetParent(t, false);
        var stateRt = stateGo.AddComponent<RectTransform>();
        stateRt.anchorMin = Vector2.zero;
        stateRt.anchorMax = Vector2.one;
        stateRt.offsetMin = stateRt.offsetMax = Vector2.zero;
        Text stateText = stateGo.AddComponent<Text>();
        stateText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stateText.fontSize  = 42;
        stateText.fontStyle = FontStyle.Bold;
        stateText.alignment = TextAnchor.MiddleCenter;
        stateText.color     = new Color(1f, 0.30f, 0.18f);
        stateText.raycastTarget = false;
        stateGo.SetActive(false);

        // ── SCANLINES — use saved asset texture, always last child ────────
        AddScanlineOverlay(t, scanlines);

        // ── Wire UIManager ────────────────────────────────────────────────
        var so = new SerializedObject(mgr);
        so.FindProperty("scoreText").objectReferenceValue        = scoreText;
        so.FindProperty("highScoreText").objectReferenceValue    = highText;
        so.FindProperty("multiplierText").objectReferenceValue   = multText;
        so.FindProperty("stateText").objectReferenceValue        = stateText;
        so.FindProperty("bossBarRoot").objectReferenceValue      = bossRoot;
        so.FindProperty("bossHealthSlider").objectReferenceValue = bossSlider;
        so.FindProperty("bossNameText").objectReferenceValue     = bossNameText;
        so.FindProperty("healthBarFill").objectReferenceValue    = hpFillImg;
        so.FindProperty("livesHeartsText").objectReferenceValue  = heartsText;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        Debug.Log("[HUDBuilder] HUD prefab rebuilt — Arcade Full.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Corner UI — four small badge panels, one per corner
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Retro Space Shooter/Rebuild HUD (Corner)")]
    public static void BuildCorner()
    {
        Texture2D scanlines = EnsureScanlineTexture();

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[HUDBuilder] Prefab not found: {PrefabPath}");
            return;
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        if (root.TryGetComponent<CanvasScaler>(out var scaler))
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 640f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
        }

        Object.DestroyImmediate(root.GetComponent<UIManager>());
        UIManager mgr = root.AddComponent<UIManager>();

        Transform t = root.transform;

        // ── TOP-LEFT: Score ───────────────────────────────────────────────
        Transform tlPanel = CornerBox(t, "TL Score Panel",
            new Vector2(0f, 1f),
            new Vector2(8f, -8f), new Vector2(170f, 60f),
            new Color(0.012f, 0.030f, 0.085f, 0.88f),
            new Color(0.18f, 0.52f, 1f, 0.55f));

        Label(tlPanel, "Score Label", "SCORE",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8f, -6f), new Vector2(70f, 13f),
            8, new Color(0.22f, 0.55f, 0.85f), TextAnchor.UpperLeft);

        Text scoreText = Label(tlPanel, "Score Value", "000000",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8f, -20f), new Vector2(154f, 28f),
            20, new Color(0.18f, 0.88f, 1f), TextAnchor.UpperLeft);

        // ── TOP-RIGHT: Lives + High Score ────────────────────────────────
        Transform trPanel = CornerBox(t, "TR Lives Panel",
            new Vector2(1f, 1f),
            new Vector2(-8f, -8f), new Vector2(160f, 66f),
            new Color(0.012f, 0.030f, 0.085f, 0.88f),
            new Color(0.90f, 0.20f, 0.25f, 0.55f));

        Text heartsText = Label(trPanel, "Lives Hearts", "♥ ♥ ♥",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -6f), new Vector2(144f, 22f),
            15, new Color(1f, 0.25f, 0.35f), TextAnchor.UpperRight);

        Label(trPanel, "High Label", "HIGH SCORE",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -30f), new Vector2(144f, 13f),
            8, new Color(0.60f, 0.50f, 0.15f), TextAnchor.UpperRight);

        Text highText = Label(trPanel, "High Value", "000000",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -44f), new Vector2(144f, 20f),
            14, new Color(1f, 0.85f, 0.28f), TextAnchor.UpperRight);

        // ── BOTTOM-LEFT: HP Bar ──────────────────────────────────────────
        Transform blPanel = CornerBox(t, "BL HP Panel",
            new Vector2(0f, 0f),
            new Vector2(8f, 60f), new Vector2(150f, 40f),
            new Color(0.012f, 0.030f, 0.085f, 0.88f),
            new Color(0.12f, 0.35f, 0.65f, 0.55f));

        Label(blPanel, "HP Label", "HP",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8f, -8f), new Vector2(20f, 12f),
            8, new Color(0.22f, 0.52f, 0.72f), TextAnchor.UpperLeft);

        var hpTrackGo = new GameObject("HP Track");
        hpTrackGo.transform.SetParent(blPanel, false);
        var hpTrackRt = hpTrackGo.AddComponent<RectTransform>();
        hpTrackRt.anchorMin        = hpTrackRt.anchorMax = new Vector2(0f, 1f);
        hpTrackRt.pivot            = new Vector2(0f, 1f);
        hpTrackRt.anchoredPosition = new Vector2(30f, -9f);
        hpTrackRt.sizeDelta        = new Vector2(112f, 10f);
        var hpTrackImg = hpTrackGo.AddComponent<Image>();
        hpTrackImg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);
        hpTrackImg.raycastTarget = false;
        var hpOutline = hpTrackGo.AddComponent<Outline>();
        hpOutline.effectColor    = new Color(0.12f, 0.35f, 0.65f, 0.60f);
        hpOutline.effectDistance = new Vector2(1f, -1f);

        var hpFillGo = new GameObject("HP Fill");
        hpFillGo.transform.SetParent(hpTrackGo.transform, false);
        var hpFillRt = hpFillGo.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;
        var hpFillImg = hpFillGo.AddComponent<Image>();
        hpFillImg.color = new Color(0f, 0.80f, 0.35f);
        hpFillImg.raycastTarget = false;

        // ── BOTTOM-RIGHT: Multiplier ─────────────────────────────────────
        Transform brPanel = CornerBox(t, "BR Mult Panel",
            new Vector2(1f, 0f),
            new Vector2(-8f, 60f), new Vector2(90f, 40f),
            new Color(0.012f, 0.030f, 0.085f, 0.88f),
            new Color(0.80f, 0.05f, 0.75f, 0.55f));

        Text multText = Label(brPanel, "Mult Text", "x1",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(74f, 34f),
            22, new Color(1f, 0.10f, 0.85f), TextAnchor.MiddleCenter);

        // ── BOSS BAR: vertical strip on the right edge ───────────────────
        var (bossRoot, bossSlider, bossNameText) = BuildVerticalBossBar(t);

        // ── GAME STATE OVERLAY ────────────────────────────────────────────
        var stateGo = new GameObject("State Text");
        stateGo.transform.SetParent(t, false);
        var stateRt = stateGo.AddComponent<RectTransform>();
        stateRt.anchorMin = Vector2.zero;
        stateRt.anchorMax = Vector2.one;
        stateRt.offsetMin = stateRt.offsetMax = Vector2.zero;
        Text stateText = stateGo.AddComponent<Text>();
        stateText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stateText.fontSize  = 42;
        stateText.fontStyle = FontStyle.Bold;
        stateText.alignment = TextAnchor.MiddleCenter;
        stateText.color     = new Color(1f, 0.30f, 0.18f);
        stateText.raycastTarget = false;
        stateGo.SetActive(false);

        // ── SCANLINES ─────────────────────────────────────────────────────
        AddScanlineOverlay(t, scanlines);

        // ── Wire UIManager ────────────────────────────────────────────────
        var so = new SerializedObject(mgr);
        so.FindProperty("scoreText").objectReferenceValue        = scoreText;
        so.FindProperty("highScoreText").objectReferenceValue    = highText;
        so.FindProperty("multiplierText").objectReferenceValue   = multText;
        so.FindProperty("stateText").objectReferenceValue        = stateText;
        so.FindProperty("bossBarRoot").objectReferenceValue      = bossRoot;
        so.FindProperty("bossHealthSlider").objectReferenceValue = bossSlider;
        so.FindProperty("bossNameText").objectReferenceValue     = bossNameText;
        so.FindProperty("healthBarFill").objectReferenceValue    = hpFillImg;
        so.FindProperty("livesHeartsText").objectReferenceValue  = heartsText;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        Debug.Log("[HUDBuilder] HUD prefab rebuilt — Corner UI.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Thin Top Strip — everything in one 22 px bar at the top
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Retro Space Shooter/Rebuild HUD (Thin Top)")]
    public static void BuildThinTop()
    {
        Texture2D scanlines = EnsureScanlineTexture();

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[HUDBuilder] Prefab not found: {PrefabPath}");
            return;
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        if (root.TryGetComponent<CanvasScaler>(out var scaler))
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 640f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0f;
        }

        // Offsets top-anchored HUD elements below the device notch at runtime
        if (root.TryGetComponent<TopSafeAreaOffset>(out var oldSafe))
            Object.DestroyImmediate(oldSafe);
        root.AddComponent<TopSafeAreaOffset>();

        Object.DestroyImmediate(root.GetComponent<UIManager>());
        UIManager mgr = root.AddComponent<UIManager>();

        Transform t = root.transform;

        const float stripH = 22f;
        const float midY   = -(stripH * 0.5f); // vertical centre of the strip

        // ── Strip background ─────────────────────────────────────────────────
        HStretch(t, "Top BG",
            new Vector2(0f, -stripH), Vector2.zero,
            new Color(0.012f, 0.030f, 0.085f, 0.94f));

        HStretch(t, "Top Border",
            new Vector2(0f, -stripH - 1f), new Vector2(0f, -stripH),
            new Color(0.18f, 0.52f, 1f, 0.38f));

        // ── Score (left) ─────────────────────────────────────────────────────
        Text scoreText = Label(t, "Score Value", "000000",
            new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(10f, midY), new Vector2(130f, 16f),
            14, new Color(0.18f, 0.88f, 1f), TextAnchor.MiddleLeft);

        // ── HP bar (centre-left) ─────────────────────────────────────────────
        Label(t, "HP Label", "HP",
            new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(158f, midY), new Vector2(20f, 10f),
            7, new Color(0.22f, 0.52f, 0.72f), TextAnchor.MiddleLeft);

        var hpTrackGo = new GameObject("HP Track");
        hpTrackGo.transform.SetParent(t, false);
        var hpTrackRt = hpTrackGo.AddComponent<RectTransform>();
        hpTrackRt.anchorMin        = hpTrackRt.anchorMax = new Vector2(0f, 1f);
        hpTrackRt.pivot            = new Vector2(0f, 0.5f);
        hpTrackRt.anchoredPosition = new Vector2(180f, midY);
        hpTrackRt.sizeDelta        = new Vector2(108f, 8f);
        var hpTrackImg = hpTrackGo.AddComponent<Image>();
        hpTrackImg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);
        hpTrackImg.raycastTarget = false;
        var hpOutline = hpTrackGo.AddComponent<Outline>();
        hpOutline.effectColor    = new Color(0.12f, 0.35f, 0.65f, 0.55f);
        hpOutline.effectDistance = new Vector2(1f, -1f);

        var hpFillGo = new GameObject("HP Fill");
        hpFillGo.transform.SetParent(hpTrackGo.transform, false);
        var hpFillRt = hpFillGo.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;
        var hpFillImg = hpFillGo.AddComponent<Image>();
        hpFillImg.color = new Color(0f, 0.80f, 0.35f);
        hpFillImg.raycastTarget = false;

        // ── Lives (centre-right) ─────────────────────────────────────────────
        Text heartsText = Label(t, "Lives Hearts", "♥ ♥ ♥",
            new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(302f, midY), new Vector2(90f, 16f),
            13, new Color(1f, 0.25f, 0.35f), TextAnchor.MiddleLeft);

        // ── Multiplier (right) ───────────────────────────────────────────────
        Text multText = Label(t, "Mult Text", "x1",
            new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-10f, midY), new Vector2(52f, 16f),
            14, new Color(1f, 0.10f, 0.85f), TextAnchor.MiddleRight);

        // ── Power-up display (hidden by default, appears on collect) ─────────
        var puRoot = new GameObject("PowerUp Root");
        puRoot.transform.SetParent(t, false);
        puRoot.SetActive(false);
        var puRt = puRoot.AddComponent<RectTransform>();
        puRt.anchorMin        = puRt.anchorMax = new Vector2(0.5f, 1f);
        puRt.pivot            = new Vector2(0.5f, 1f);
        puRt.anchoredPosition = new Vector2(0f, -(stripH + 4f));
        puRt.sizeDelta        = new Vector2(170f, 20f);
        var puBg = puRoot.AddComponent<Image>();
        puBg.color = new Color(0.012f, 0.030f, 0.085f, 0.92f);
        puBg.raycastTarget = false;
        var puOutline = puRoot.AddComponent<Outline>();
        puOutline.effectColor    = new Color(0.18f, 0.52f, 0.85f, 0.45f);
        puOutline.effectDistance = new Vector2(1f, -1f);

        // Left accent bar
        var puAccent = new GameObject("PU Accent");
        puAccent.transform.SetParent(puRoot.transform, false);
        var puAccentRt = puAccent.AddComponent<RectTransform>();
        puAccentRt.anchorMin = Vector2.zero;
        puAccentRt.anchorMax = new Vector2(0f, 1f);
        puAccentRt.pivot     = new Vector2(0f, 0.5f);
        puAccentRt.offsetMin = Vector2.zero;
        puAccentRt.offsetMax = new Vector2(3f, 0f);
        puAccent.AddComponent<Image>().color = new Color(0.18f, 0.88f, 1f);

        Text puNameText = Label(puRoot.transform, "PU Name", "POWER UP",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0f), new Vector2(94f, 14f),
            8, new Color(0.18f, 0.88f, 1f), TextAnchor.MiddleLeft);

        // Timer track + fill
        var puTrackGo = new GameObject("PU Timer Track");
        puTrackGo.transform.SetParent(puRoot.transform, false);
        var puTrackRt = puTrackGo.AddComponent<RectTransform>();
        puTrackRt.anchorMin        = puTrackRt.anchorMax = new Vector2(1f, 0.5f);
        puTrackRt.pivot            = new Vector2(1f, 0.5f);
        puTrackRt.anchoredPosition = new Vector2(-8f, 0f);
        puTrackRt.sizeDelta        = new Vector2(56f, 7f);
        var puTrackImg = puTrackGo.AddComponent<Image>();
        puTrackImg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);
        puTrackImg.raycastTarget = false;
        var puTrackOutline = puTrackGo.AddComponent<Outline>();
        puTrackOutline.effectColor    = new Color(0.18f, 0.52f, 0.85f, 0.45f);
        puTrackOutline.effectDistance = new Vector2(1f, -1f);

        var puFillGo = new GameObject("PU Timer Fill");
        puFillGo.transform.SetParent(puTrackGo.transform, false);
        var puFillRt = puFillGo.AddComponent<RectTransform>();
        puFillRt.anchorMin = Vector2.zero;
        puFillRt.anchorMax = Vector2.one;
        puFillRt.offsetMin = puFillRt.offsetMax = Vector2.zero;
        var puFillImg = puFillGo.AddComponent<Image>();
        puFillImg.color = new Color(0.18f, 0.88f, 1f);
        puFillImg.raycastTarget = false;

        // ── Boss bar: vertical strip on the right edge ───────────────────────
        var (bossRoot, bossSlider, bossNameText) = BuildVerticalBossBar(t);

        // ── Game state overlay ────────────────────────────────────────────────
        var stateGo = new GameObject("State Text");
        stateGo.transform.SetParent(t, false);
        var stateRt = stateGo.AddComponent<RectTransform>();
        stateRt.anchorMin = Vector2.zero;
        stateRt.anchorMax = Vector2.one;
        stateRt.offsetMin = stateRt.offsetMax = Vector2.zero;
        Text stateText = stateGo.AddComponent<Text>();
        stateText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stateText.fontSize  = 42;
        stateText.fontStyle = FontStyle.Bold;
        stateText.alignment = TextAnchor.MiddleCenter;
        stateText.color     = new Color(1f, 0.30f, 0.18f);
        stateText.raycastTarget = false;
        stateGo.SetActive(false);

        // ── Scanlines ─────────────────────────────────────────────────────────
        AddScanlineOverlay(t, scanlines);

        // ── Wire UIManager ────────────────────────────────────────────────────
        var so = new SerializedObject(mgr);
        so.FindProperty("scoreText").objectReferenceValue        = scoreText;
        so.FindProperty("multiplierText").objectReferenceValue   = multText;
        so.FindProperty("stateText").objectReferenceValue        = stateText;
        so.FindProperty("bossBarRoot").objectReferenceValue      = bossRoot;
        so.FindProperty("bossHealthSlider").objectReferenceValue = bossSlider;
        so.FindProperty("bossNameText").objectReferenceValue     = bossNameText;
        so.FindProperty("healthBarFill").objectReferenceValue    = hpFillImg;
        so.FindProperty("livesHeartsText").objectReferenceValue  = heartsText;
        so.FindProperty("powerUpRoot").objectReferenceValue      = puRoot;
        so.FindProperty("powerUpNameText").objectReferenceValue  = puNameText;
        so.FindProperty("powerUpTimerFill").objectReferenceValue = puFillImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();
        Debug.Log("[HUDBuilder] HUD prefab rebuilt — Thin Top Strip.");
    }

    // ── Scanlines texture saved as a proper disk asset ────────────────────

    private static Texture2D EnsureScanlineTexture()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScanlinesTex));
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(ScanlinesTex) != null)
            AssetDatabase.DeleteAsset(ScanlinesTex);

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

        AssetDatabase.CreateAsset(tex, ScanlinesTex);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ScanlinesTex);
    }

    private static void AddScanlineOverlay(Transform parent, Texture2D tex)
    {
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

    // ── Boss bar: vertical strip on the right edge ───────────────────────

    private static (GameObject root, Slider slider, Text nameText) BuildVerticalBossBar(Transform parent)
    {
        var bossRoot = new GameObject("Boss Bar Root");
        bossRoot.transform.SetParent(parent, false);
        bossRoot.SetActive(false);
        var bossRt = bossRoot.AddComponent<RectTransform>();
        bossRt.anchorMin        = new Vector2(1f, 0.06f);
        bossRt.anchorMax        = new Vector2(1f, 0.94f);
        bossRt.pivot            = new Vector2(1f, 0.5f);
        bossRt.sizeDelta        = new Vector2(22f, 0f);
        bossRt.anchoredPosition = Vector2.zero;
        var bgImg = bossRoot.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.01f, 0.07f, 0.95f);
        bgImg.raycastTarget = false;

        // Red accent on the left edge of the bar
        var borderGo = new GameObject("Boss Border");
        borderGo.transform.SetParent(bossRoot.transform, false);
        var borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = new Vector2(0f, 1f);
        borderRt.pivot     = new Vector2(0f, 0.5f);
        borderRt.offsetMin = Vector2.zero;
        borderRt.offsetMax = new Vector2(1.5f, 0f);
        borderGo.AddComponent<Image>().color = new Color(0.80f, 0.12f, 0.12f, 0.90f);

        // Boss name rotated 90° — reads bottom-to-top along the bar
        var nameLabelGo = new GameObject("Boss Name");
        nameLabelGo.transform.SetParent(bossRoot.transform, false);
        nameLabelGo.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        var nameRt = nameLabelGo.AddComponent<RectTransform>();
        nameRt.anchorMin        = new Vector2(0.5f, 0.5f);
        nameRt.anchorMax        = new Vector2(0.5f, 0.5f);
        nameRt.pivot            = new Vector2(0.5f, 0.5f);
        nameRt.sizeDelta        = new Vector2(120f, 16f);
        nameRt.anchoredPosition = Vector2.zero;
        var nameText = nameLabelGo.AddComponent<Text>();
        nameText.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize      = 9;
        nameText.fontStyle     = FontStyle.Bold;
        nameText.alignment     = TextAnchor.MiddleCenter;
        nameText.color         = new Color(1f, 0.40f, 0.40f);
        nameText.text          = "BOSS";
        nameText.raycastTarget = false;

        // Vertical slider — fills from bottom (full HP) draining downward as HP decreases
        var sliderGo = new GameObject("Boss Health Slider");
        sliderGo.transform.SetParent(bossRoot.transform, false);
        var sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(3f, 4f);
        sliderRt.offsetMax = new Vector2(-3f, -4f);

        var slider       = sliderGo.AddComponent<Slider>();
        slider.direction = Slider.Direction.BottomToTop;
        slider.minValue  = 0;
        slider.maxValue  = 100;
        slider.value     = 100;

        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderGo.transform, false);
        FullStretch(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = new Color(0.05f, 0.02f, 0.08f, 0.95f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        FullStretch(fillArea.AddComponent<RectTransform>());

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.08f);

        slider.fillRect = fillRt;
        return (bossRoot, slider, nameText);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // Semi-transparent badge anchored to a corner. Pivot matches the anchor corner.
    private static Transform CornerBox(Transform parent, string name,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size,
        Color bgColor, Color outlineColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor    = outlineColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return go.transform;
    }

    private static void HStretch(Transform parent, string name,
        Vector2 offMin, Vector2 offMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private static void Panel(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 offMin, Vector2 offMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private static Text Label(Transform parent, string name, string content,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
        int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var text = go.AddComponent<Text>();
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = align;
        text.color     = color;
        text.text      = content;
        text.raycastTarget = false;
        return text;
    }

    private static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
