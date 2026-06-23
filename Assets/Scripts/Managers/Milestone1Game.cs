using UnityEngine;

public class Milestone1Game : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private LevelData levelData;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject hudPrefab;
    [SerializeField] private GameObject smallExplosionPrefab;
    [SerializeField] private GameObject bigExplosionPrefab;
    [SerializeField] private PowerUp[] powerUpPrefabs;

    [Header("Gameplay")]
    [SerializeField, Min(1)] private int playerLives = 3;
    [SerializeField] private bool loopWaves = true;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        ConfigureCamera();

        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        CreateManagers();
        CreateBackground();
        Instantiate(playerPrefab, new Vector3(0f, -3.5f), Quaternion.identity);
        Instantiate(hudPrefab);
    }

    private bool ValidateConfiguration()
    {
        bool valid = true;
        if (mainCamera == null)
        {
            Debug.LogError("Milestone1Game requires a camera tagged MainCamera.");
            valid = false;
        }
        if (levelData == null)
        {
            Debug.LogError("Milestone1Game requires LevelData.");
            valid = false;
        }
        else if (levelData.background == null)
        {
            Debug.LogError("Milestone1Game LevelData requires a background sprite.");
            valid = false;
        }
        if (playerPrefab == null || hudPrefab == null)
        {
            Debug.LogError("Milestone1Game requires Player and HUD prefabs.");
            valid = false;
        }
        return valid;
    }

    private void ConfigureCamera()
    {
        if (mainCamera == null)
        {
            return;
        }
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.backgroundColor = new Color(0.01f, 0.01f, 0.04f);
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void CreateManagers()
    {
        GameObject systems = new("Game Systems");
        GameManager gameManager = systems.AddComponent<GameManager>();
        gameManager.InitializeLives(playerLives);
        gameManager.InitializeProgression(levelData.nextSceneName);
        systems.AddComponent<ScoreManager>();
        systems.AddComponent<PowerUpManager>().Initialize(powerUpPrefabs);
        AudioManager audio = systems.AddComponent<AudioManager>();
        audio.PlayMusic(levelData.musicTheme);
        mainCamera.gameObject.AddComponent<ScreenShake>();

        VFXManager vfx = systems.AddComponent<VFXManager>();
        vfx.Initialize(smallExplosionPrefab, bigExplosionPrefab);

        WaveManager waves = systems.AddComponent<WaveManager>();
        waves.Initialize(levelData, mainCamera, loopWaves);
    }

    private void CreateBackground()
    {
        float visibleHeight = mainCamera.orthographicSize * 2f;
        float scale = visibleHeight / levelData.background.bounds.size.y;
        for (int index = 0; index < 2; index++)
        {
            GameObject layer = new($"Background_{index}");
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = levelData.background;
            renderer.sortingOrder = -100;
            layer.transform.localScale = Vector3.one * scale;
            layer.transform.position = new Vector3(0f, index * visibleHeight, 2f);
            layer.AddComponent<BackgroundScroller>().Initialize(visibleHeight, 0.75f);
        }
    }
}
