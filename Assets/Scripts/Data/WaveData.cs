using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Wave Data")]
public class WaveData : ScriptableObject
{
    public Enemy enemyPrefab;
    [Min(1)] public int enemyCount = 5;
    [Min(0.05f)] public float spawnInterval = 1f;
    [Min(0f)] public float delayBeforeNextWave = 2f;
    public AnimationCurve horizontalPattern = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
}
