using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Milestone1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Level01_DeepSpace.unity";
    private const string SessionKey = "RetroSpaceShooter.Milestone1SceneBuilt.v6";

    static Milestone1SceneBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Retro Space Shooter/Rebuild Milestone 1 Scene")]
    public static void BuildFromMenu()
    {
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

        SessionState.SetBool(SessionKey, true);
        BuildScene();
    }

    private static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<LevelData>(
                "Assets/Settings/Data/Level01_DeepSpace.asset") == null)
        {
            SessionState.EraseBool(SessionKey);
            EditorApplication.delayCall += BuildOnce;
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        string previousPath = previousScene.path;
        if (previousScene.isDirty)
        {
            EditorSceneManager.SaveScene(previousScene);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject gameObject = new("Milestone 1 Game");
        Milestone1Game game = gameObject.AddComponent<Milestone1Game>();
        SerializedObject serializedGame = new(game);
        SetAsset(serializedGame, "levelData",
            "Assets/Settings/Data/Level01_DeepSpace.asset");
        SetAsset(serializedGame, "playerPrefab",
            "Assets/Prefabs/Player/Player.prefab");
        SetAsset(serializedGame, "hudPrefab",
            "Assets/Prefabs/UI/HUD.prefab");
        SetAsset(serializedGame, "smallExplosionPrefab",
            "Assets/Prefabs/VFX/ExplosionSmall.prefab");
        SetAsset(serializedGame, "bigExplosionPrefab",
            "Assets/Prefabs/VFX/ExplosionBig.prefab");
        SetPowerUps(serializedGame);
        serializedGame.FindProperty("loopWaves").boolValue = false;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        if (!string.IsNullOrEmpty(previousPath) && File.Exists(previousPath))
        {
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
        else
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Debug.Log($"Retro Space Shooter: Milestone 1 scene created at {ScenePath}");
    }

    private static void SetAsset(SerializedObject target, string propertyName, string assetPath)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
    }

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

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        foreach (EditorBuildSettingsScene scene in currentScenes)
        {
            if (scene.path == ScenePath)
            {
                return;
            }
        }

        EditorBuildSettingsScene[] updatedScenes =
            new EditorBuildSettingsScene[currentScenes.Length + 1];
        currentScenes.CopyTo(updatedScenes, 0);
        updatedScenes[^1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updatedScenes;
    }
}
