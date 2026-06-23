using UnityEngine;

// Shifts direct canvas children anchored to the top edge downward by the device's
// top safe-area inset (notch / status-bar overhang). The game camera is unaffected
// because sprites live on the Camera, not on the Canvas hierarchy.
[RequireComponent(typeof(Canvas))]
public class TopSafeAreaOffset : MonoBehaviour
{
    void Start()
    {
        float topInsetPx = Screen.height - Screen.safeArea.yMax;
        if (topInsetPx <= 0.5f) return;

        float offset = topInsetPx / GetComponent<Canvas>().scaleFactor;

        foreach (RectTransform child in transform)
        {
            if (child.anchorMin.y >= 0.9f && child.anchorMax.y >= 0.9f)
                child.anchoredPosition += new Vector2(0f, -offset);
        }
    }
}
