using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveData[] waves;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BossController bossPrefab;
    [SerializeField] private bool loopWaves;
    private Camera gameplayCamera;

    private enum MovementPattern { None, Zigzag, Sweep, Homing, Bounce, Dive, StopAndShoot, Circle, Linger }

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
            yield break;

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
            Instantiate(bossPrefab, new Vector3(0f, 3.5f), Quaternion.identity);
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        MovementPattern pattern = PickPattern(wave);
        float zigzagPhaseStep = wave.enemyCount > 1 ? 1f / wave.enemyCount : 0f;
        float lateralDirection = Random.value > 0.5f ? 1f : -1f;

        for (int index = 0; index < wave.enemyCount; index++)
        {
            Vector3 position = GetSpawnPosition(wave, index);
            Enemy enemy = Instantiate(wave.enemyPrefab, position, Quaternion.Euler(0f, 0f, 180f));
            switch (pattern)
            {
                case MovementPattern.Zigzag:
                    float amplitude = Random.Range(wave.zigzagAmplitudeMin, wave.zigzagAmplitudeMax);
                    enemy.SetZigzag(amplitude, wave.zigzagFrequency, index * zigzagPhaseStep);
                    break;
                case MovementPattern.Sweep:
                    enemy.SetSweep(wave.sweepSpeed * lateralDirection);
                    break;
                case MovementPattern.Homing:
                    enemy.SetHoming(wave.homingStrength);
                    break;
                case MovementPattern.Bounce:
                    enemy.SetBounce(wave.bounceSpeed * lateralDirection);
                    break;
                case MovementPattern.Dive:
                    enemy.SetDive(wave.diveThresholdY, wave.diveSpeed);
                    break;
                case MovementPattern.StopAndShoot:
                    enemy.SetStopAndShoot(wave.stopThresholdY, wave.stopDuration);
                    break;
                case MovementPattern.Circle:
                    enemy.SetCircle(GetScreenCenter(), wave.circleAngularSpeed, wave.circleOrbits);
                    break;
                case MovementPattern.Linger:
                    enemy.SetLinger(wave.lingerThresholdY, wave.lingerDuration, wave.lingerLateralSpeed);
                    break;
            }
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    // Each chance field is the exact probability of that pattern.
    // Chances are checked in order; remaining probability (1 - sum) yields straight movement.
    private MovementPattern PickPattern(WaveData wave)
    {
        float roll = Random.value;
        float cumulative = 0f;

        cumulative += wave.zigzagChance;
        if (roll < cumulative) return MovementPattern.Zigzag;
        cumulative += wave.sweepChance;
        if (roll < cumulative) return MovementPattern.Sweep;
        cumulative += wave.homingChance;
        if (roll < cumulative) return MovementPattern.Homing;
        cumulative += wave.bounceChance;
        if (roll < cumulative) return MovementPattern.Bounce;
        cumulative += wave.diveChance;
        if (roll < cumulative) return MovementPattern.Dive;
        cumulative += wave.stopShootChance;
        if (roll < cumulative) return MovementPattern.StopAndShoot;
        cumulative += wave.circleChance;
        if (roll < cumulative) return MovementPattern.Circle;
        cumulative += wave.lingerChance;
        if (roll < cumulative) return MovementPattern.Linger;

        return MovementPattern.None;
    }

    private Vector3 GetScreenCenter()
    {
        Camera cam = gameplayCamera != null ? gameplayCamera : Camera.main;
        return cam != null ? cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f)) : Vector3.zero;
    }

    private Vector3 GetSpawnPosition(WaveData wave, int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[index % spawnPoints.Length].position;

        Camera cameraToUse = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (cameraToUse == null)
            return transform.position;

        float progress = wave.enemyCount <= 1 ? 0.5f : index / (float)(wave.enemyCount - 1);
        float normalizedX = Mathf.Clamp01(wave.horizontalPattern.Evaluate(progress));
        Vector3 worldPosition = cameraToUse.ViewportToWorldPoint(
            new Vector3(Mathf.Lerp(0.12f, 0.88f, normalizedX), 1.08f, 0f));
        worldPosition.z = 0f;
        return worldPosition;
    }
}
