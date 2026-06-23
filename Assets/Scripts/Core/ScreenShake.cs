using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }
    private Vector3 origin;
    private Coroutine activeShake;

    private void Awake()
    {
        Instance = this;
        origin = transform.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (activeShake != null)
        {
            StopCoroutine(activeShake);
        }
        activeShake = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            transform.localPosition = origin + (Vector3)Random.insideUnitCircle * strength;
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localPosition = origin;
        activeShake = null;
    }
}
