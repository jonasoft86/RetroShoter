using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelName = "Deep Space";
    public Sprite background;
    public WaveData[] waves;
    public BossController bossPrefab;
    public AudioClip music;
    public int musicTheme;
    public string nextSceneName;
}
