using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Wave Data")]
public class WaveData : ScriptableObject
{
    public Enemy enemyPrefab;
    [Min(1)] public int enemyCount = 5;
    [Min(0.05f)] public float spawnInterval = 1f;
    [Min(0f)] public float delayBeforeNextWave = 2f;
    public AnimationCurve horizontalPattern = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);

    [Header("Zigzag Movement")]
    [Range(0f, 1f)] public float zigzagChance = 0f;
    [Min(0.1f)] public float zigzagAmplitudeMin = 1f;
    [Min(0.1f)] public float zigzagAmplitudeMax = 1.5f;
    [Min(0.1f)] public float zigzagFrequency = 1.5f;

    [Header("Sweep Movement")]
    [Range(0f, 1f)] public float sweepChance = 0f;
    [Min(0.1f)] public float sweepSpeed = 3f;

    [Header("Homing Movement")]
    [Range(0f, 1f)] public float homingChance = 0f;
    [Min(0.1f)] public float homingStrength = 3f;

    [Header("Bounce Movement")]
    [Range(0f, 1f)] public float bounceChance = 0f;
    [Min(0.1f)] public float bounceSpeed = 4f;

    [Header("Dive")]
    [Range(0f, 1f)] public float diveChance = 0f;
    public float diveThresholdY = 1f;
    [Min(0.1f)] public float diveSpeed = 8f;

    [Header("Stop and Shoot")]
    [Range(0f, 1f)] public float stopShootChance = 0f;
    public float stopThresholdY = 0f;
    [Min(0.1f)] public float stopDuration = 1.5f;

    [Header("Circle / Orbit")]
    [Range(0f, 1f)] public float circleChance = 0f;
    [Min(0.1f)] public float circleAngularSpeed = 3f;
    [Min(0.25f)] public float circleOrbits = 1f;

    [Header("Linger")]
    [Range(0f, 1f)] public float lingerChance = 0f;
    public float lingerThresholdY = 1f;
    [Min(0.1f)] public float lingerDuration = 2f;
    [Min(0.1f)] public float lingerLateralSpeed = 2f;
}
