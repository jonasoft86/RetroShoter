using UnityEngine;

// Keeps a RectTransform fitted to Screen.safeArea so UI content avoids
// device notches, camera punch-outs, and home-indicator bars.
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform panel;
    Rect lastSafeArea;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea)
            Apply();
    }

    void Apply()
    {
        lastSafeArea = Screen.safeArea;
        var screen = new Vector2(Screen.width, Screen.height);
        panel.anchorMin = lastSafeArea.position / screen;
        panel.anchorMax = (lastSafeArea.position + lastSafeArea.size) / screen;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
    }
}
