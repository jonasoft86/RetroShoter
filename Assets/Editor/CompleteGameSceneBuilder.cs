using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[InitializeOnLoad]
public static class CompleteGameSceneBuilder
{
    private const string SessionKey = "RetroSpaceShooter.CompleteScenes.v1";

    static CompleteGameSceneBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Retro Space Shooter/Rebuild Complete Game Scenes")]
    public static void BuildFromMenu()
    {
        HUDBuilder.Build();
        SessionState.EraseBool(SessionKey);
        BuildOnce();
    }

    private static void BuildOnce()
    {
        if (SessionState.GetBool(SessionKey, false) ||
            EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<LevelData>(
                "Assets/Settings/Data/Level03_RiverValley.asset") == null)
        {
            EditorApplication.delayCall += BuildOnce;
            return;
        }

        SessionState.SetBool(SessionKey, true);
        Scene active = SceneManager.GetActiveScene();
        string previous = active.path;
        if (active.isDirty)
        {
            EditorSceneManager.SaveScene(active);
        }

        BuildBoot();
        BuildMenu();
        BuildLevel3();
        BuildEndScene("GameOver", "GAME OVER", new Color(1f, 0.25f, 0.15f));
        BuildEndScene("Win", "VICTORY!", new Color(0.3f, 1f, 0.65f));
        ConfigureBuildSettings();

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
        {
            EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
        else
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
        }
        Debug.Log("Retro Space Shooter: complete Milestone 3 scenes generated.");
    }

    private static void BuildBoot()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Boot Loader").AddComponent<BootLoader>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
    }

    private static void BuildMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.01f, 0.015f, 0.06f));
        Canvas canvas = CreateCanvas("Menu Canvas");
        canvas.gameObject.AddComponent<CanvasGroup>();
        canvas.gameObject.AddComponent<MenuAnimator>();
        MenuController controller = canvas.gameObject.AddComponent<MenuController>();

        // Versión — esquina superior derecha
        RuntimeScreenUI.CreateCornerLabel(canvas.transform, "v1.0",
            new Vector2(1f, 1f), new Vector2(-12f, -10f), 11,
            new Color(0.20f, 0.38f, 0.55f));

        // Marco del título (estira)
        RuntimeScreenUI.CreateTitleFrame(canvas.transform, 0.76f);

        // Texto del título (estira dentro del marco)
        Text title = RuntimeScreenUI.CreateStretchLabel(canvas.transform,
            "RETRO\nSPACE SHOOTER",
            0.05f, 0.95f, 0.76f, 148f, 44, new Color(0.10f, 0.72f, 1f));
        title.gameObject.name = "Title";

        // Separador (estira)
        RuntimeScreenUI.CreateStretchSeparator(canvas.transform, 0.12f, 0.88f, 0.615f);

        // High score centrado con ◆ incrustados
        Text high = CreateLabel(canvas.transform, "◆  HIGH SCORE  000000  ◆",
            new Vector2(0.5f, 0.565f), new Vector2(370f, 38f), 19,
            new Color(1f, 0.88f, 0.42f));
        SetReference(controller, "highScoreText", high);

        // Botones (estiran)
        Button play = RuntimeScreenUI.CreateStretchGlowButton(canvas.transform,
            "▶  PLAY GAME", 0.10f, 0.90f, 0.445f, 56f);
        Button quit = RuntimeScreenUI.CreateStretchGlowButton(canvas.transform,
            "QUIT", 0.10f, 0.90f, 0.325f, 56f);
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

        // Press Enter parpadeante
        Text blink = CreateLabel(canvas.transform, "— PRESS ENTER TO START —",
            new Vector2(0.5f, 0.195f), new Vector2(380f, 28f), 14,
            new Color(0.50f, 0.82f, 1f));
        blink.gameObject.name = "Blink";

        // Controles
        CreateLabel(canvas.transform, "WASD / ARROWS / TOUCH  •  SPACE / FIRE",
            new Vector2(0.5f, 0.085f), new Vector2(450f, 28f), 12,
            new Color(0.32f, 0.50f, 0.65f));

        // Scanlines CRT — siempre el último hijo
        RuntimeScreenUI.CreateScanlineOverlay(canvas.transform);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Menu.unity");
    }

    private static void BuildLevel3()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.01f, 0.04f, 0.05f));
        GameObject bootstrap = new("Milestone 3 Game");
        Milestone1Game game = bootstrap.AddComponent<Milestone1Game>();
        SerializedObject serialized = new(game);
        SetAsset(serialized, "levelData", "Assets/Settings/Data/Level03_RiverValley.asset");
        SetAsset(serialized, "playerPrefab", "Assets/Prefabs/Player/Player.prefab");
        SetAsset(serialized, "hudPrefab", "Assets/Prefabs/UI/HUD.prefab");
        SetAsset(serialized, "smallExplosionPrefab", "Assets/Prefabs/VFX/ExplosionSmall.prefab");
        SetAsset(serialized, "bigExplosionPrefab", "Assets/Prefabs/VFX/ExplosionBig.prefab");
        SetPowerUps(serialized);
        serialized.FindProperty("loopWaves").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Level03_RiverValley.unity");
    }

    private static void BuildEndScene(string sceneName, string title, Color titleColor)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Color(0.015f, 0.01f, 0.04f));
        Canvas canvas = CreateCanvas($"{sceneName} Canvas");
        EndScreenController controller = canvas.gameObject.AddComponent<EndScreenController>();

        CreateLabel(canvas.transform, title, new Vector2(0.5f, 0.7f),
            new Vector2(420f, 90f), 48, titleColor);
        Text score = CreateLabel(canvas.transform, "SCORE  000000",
            new Vector2(0.5f, 0.53f), new Vector2(360f, 45f), 24, Color.white);
        Text high = CreateLabel(canvas.transform, "HIGH SCORE  000000",
            new Vector2(0.5f, 0.46f), new Vector2(360f, 45f), 21, Color.white);
        SetReference(controller, "scoreText", score);
        SetReference(controller, "highScoreText", high);

        Button retry = CreateButton(canvas.transform, "PLAY AGAIN", new Vector2(0.5f, 0.31f));
        Button menu = CreateButton(canvas.transform, "MAIN MENU", new Vector2(0.5f, 0.19f));
        UnityEventTools.AddPersistentListener(retry.onClick, controller.Retry);
        UnityEventTools.AddPersistentListener(menu.onClick, controller.MainMenu);
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }

    private static Camera CreateCamera(Color background)
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = background;
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480f, 640f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;   // mezcla ancho/alto → adaptable a 4:3, 16:9, 16:10
        canvasObject.AddComponent<GraphicRaycaster>();
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
        return canvas;
    }

    private static Text CreateLabel(
        Transform parent, string content, Vector2 anchor,
        Vector2 size, int fontSize, Color color)
    {
        GameObject item = new(content.Replace("\n", " "));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        Text text = item.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = content;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchor)
    {
        GameObject item = new($"{label} Button");
        item.transform.SetParent(parent, false);
        RectTransform rect = item.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = new Vector2(260f, 58f);
        Image image = item.AddComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.32f, 0.95f);
        Button button = item.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = CreateLabel(item.transform, label, new Vector2(0.5f, 0.5f),
            new Vector2(250f, 54f), 23, Color.white);
        text.raycastTarget = false;
        return button;
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAsset(SerializedObject target, string property, string path) =>
        target.FindProperty(property).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(path);

    private static void SetPowerUps(SerializedObject target)
    {
        string[] paths =
        {
            "Assets/Prefabs/PowerUps/PowerUpWeaponUpgrade.prefab",
            "Assets/Prefabs/PowerUps/PowerUpShield.prefab",
            "Assets/Prefabs/PowerUps/PowerUpSpeedBoost.prefab",
            "Assets/Prefabs/PowerUps/PowerUpExtraLife.prefab",
        };
        SerializedProperty array = target.FindProperty("powerUpPrefabs");
        array.arraySize = paths.Length;
        for (int index = 0; index < paths.Length; index++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
            array.GetArrayElementAtIndex(index).objectReferenceValue =
                prefab != null ? prefab.GetComponent<PowerUp>() : null;
        }
    }

    private static void ConfigureBuildSettings()
    {
        string[] paths =
        {
            "Assets/Scenes/Boot.unity",
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/Level01_DeepSpace.unity",
            "Assets/Scenes/Level02_DesertCanyon.unity",
            "Assets/Scenes/Level03_RiverValley.unity",
            "Assets/Scenes/GameOver.unity",
            "Assets/Scenes/Win.unity",
        };
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[paths.Length];
        for (int index = 0; index < paths.Length; index++)
        {
            scenes[index] = new EditorBuildSettingsScene(paths[index], true);
        }
        EditorBuildSettings.scenes = scenes;
    }
}
