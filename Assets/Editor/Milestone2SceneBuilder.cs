using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Milestone2SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Level02_DesertCanyon.unity";
    private const string DataPath = "Assets/Settings/Data/Level02_DesertCanyon.asset";
    private const string SessionKey = "RetroSpaceShooter.Milestone2SceneBuilt.v2";

    static Milestone2SceneBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Retro Space Shooter/Rebuild Milestone 2 Scene")]
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

        if (AssetDatabase.LoadAssetAtPath<LevelData>(DataPath) == null)
        {
            EditorApplication.delayCall += BuildOnce;
            return;
        }

        SessionState.SetBool(SessionKey, true);
        BuildScene();
    }

    private static void BuildScene()
    {
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

        GameObject bootstrap = new("Milestone 2 Game");
        Milestone1Game game = bootstrap.AddComponent<Milestone1Game>();
        SerializedObject serialized = new(game);
        SetAsset(serialized, "levelData", DataPath);
        SetAsset(serialized, "playerPrefab", "Assets/Prefabs/Player/Player.prefab");
        SetAsset(serialized, "hudPrefab", "Assets/Prefabs/UI/HUD.prefab");
        SetAsset(serialized, "smallExplosionPrefab", "Assets/Prefabs/VFX/ExplosionSmall.prefab");
        SetAsset(serialized, "bigExplosionPrefab", "Assets/Prefabs/VFX/ExplosionBig.prefab");
        serialized.FindProperty("loopWaves").boolValue = false;
        SetPowerUps(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();

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
        Debug.Log($"Retro Space Shooter: Milestone 2 scene created at {ScenePath}");
    }

    private static void SetAsset(SerializedObject target, string propertyName, string path)
    {
        target.FindProperty(propertyName).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Object>(path);
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
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == ScenePath)
            {
                return;
            }
        }

        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[current.Length + 1];
        current.CopyTo(updated, 0);
        updated[^1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }
}
