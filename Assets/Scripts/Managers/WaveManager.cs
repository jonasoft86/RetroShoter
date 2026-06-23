using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveData[] waves;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BossController bossPrefab;
    [SerializeField] private bool loopWaves;
    private Camera gameplayCamera;

    public void Initialize(LevelData level, Camera camera, bool loop)
    {
        waves = level != null ? level.waves : null;
        bossPrefab = level != null ? level.bossPrefab : null;
        gameplayCamera = camera;
        loopWaves = loop;
    }

    private IEnumerator Start()
    {
        if (waves == null || waves.Length == 0)
        {
            yield break;
        }

        do
        {
            foreach (WaveData wave in waves)
            {
                yield return SpawnWave(wave);
                yield return new WaitUntil(() =>
                    FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length == 0);
                yield return new WaitForSeconds(wave.delayBeforeNextWave);
            }
        }
        while (loopWaves && GameManager.Instance != null &&
               GameManager.Instance.State == GameState.Playing);

        if (!loopWaves && bossPrefab != null)
        {
            Instantiate(bossPrefab, new Vector3(0f, 3.5f), Quaternion.identity);
        }
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        for (int index = 0; index < wave.enemyCount; index++)
        {
            Vector3 position = GetSpawnPosition(wave, index);
            Instantiate(wave.enemyPrefab, position, Quaternion.Euler(0f, 0f, 180f));
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    private Vector3 GetSpawnPosition(WaveData wave, int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[index % spawnPoints.Length].position;
        }

        Camera cameraToUse = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (cameraToUse == null)
        {
            return transform.position;
        }

        float progress = wave.enemyCount <= 1 ? 0.5f : index / (float)(wave.enemyCount - 1);
        float normalizedX = Mathf.Clamp01(wave.horizontalPattern.Evaluate(progress));
        Vector3 worldPosition = cameraToUse.ViewportToWorldPoint(
            new Vector3(Mathf.Lerp(0.12f, 0.88f, normalizedX), 1.08f, 0f));
        worldPosition.z = 0f;
        return worldPosition;
    }
}
