using UnityEngine;

[CreateAssetMenu(menuName = "Retro Space Shooter/Power-Up Data")]
public class PowerUpData : ScriptableObject
{
    public PowerUpType type;
    [Min(0f)] public float duration = 5f;
    public float value = 1f;
    public Sprite sprite;
}
