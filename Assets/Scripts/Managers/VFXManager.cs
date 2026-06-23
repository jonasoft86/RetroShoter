using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    [SerializeField] private GameObject smallExplosionPrefab;
    [SerializeField] private GameObject bigExplosionPrefab;

    private void Awake() => Instance = this;
    public void Initialize(GameObject small, GameObject big = null)
    {
        smallExplosionPrefab = small;
        bigExplosionPrefab = big != null ? big : small;
    }

    public void SpawnSmallExplosion(Vector3 position) => Spawn(position, smallExplosionPrefab, 0.75f);
    public void SpawnBigExplosion(Vector3 position) => Spawn(position, bigExplosionPrefab, 1f);

    private static void Spawn(Vector3 position, GameObject prefab, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }
        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        Destroy(effect, lifetime);
    }
}
