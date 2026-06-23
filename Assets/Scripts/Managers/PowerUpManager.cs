using System.Collections;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }
    [SerializeField] private PowerUp[] powerUpPrefabs;

    private void Awake() => Instance = this;

    public void Initialize(PowerUp[] prefabs) => powerUpPrefabs = prefabs;

    public void TryDrop(Vector3 position, float probability)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0 ||
            Random.value > probability)
        {
            return;
        }

        PowerUp selected = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
        Instantiate(selected, position, Quaternion.identity);
    }

    public void ApplySpeedBoost(PlayerController controller, float duration)
    {
        if (controller != null)
        {
            StartCoroutine(SpeedBoostRoutine(controller, duration));
        }
    }

    private static IEnumerator SpeedBoostRoutine(PlayerController controller, float duration)
    {
        float original = controller.MoveSpeed;
        controller.MoveSpeed = original * 1.5f;
        yield return new WaitForSeconds(duration);
        if (controller != null)
        {
            controller.MoveSpeed = original;
        }
    }
}
