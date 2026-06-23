using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuAnimator : MonoBehaviour
{
    public Text titleText;
    public Text blinkText;

    private Image[] _stars;
    private float[] _starSpeeds;
    private float _blinkTimer;

    private static readonly Color CyanDim    = new Color(0.10f, 0.72f, 1f);
    private static readonly Color CyanBright = new Color(0.72f, 0.96f, 1f);

    private void Awake() => SpawnStars();

    private void Start()
    {
        if (titleText == null) titleText = FindChild("Title");
        if (blinkText  == null) blinkText  = FindChild("Blink");
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null) StartCoroutine(FadeIn(group));
    }

    private void Update()
    {
        PulseTitle();
        BlinkEnter();
        ScrollStars();
    }

    private void SpawnStars()
    {
        const int   count = 80;
        const float refW  = 480f;
        const float refH  = 640f;
        _stars      = new Image[count];
        _starSpeeds = new float[count];
        var rng = new System.Random(42);

        for (int i = 0; i < count; i++)
        {
            float size  = (float)(rng.NextDouble() * 2.6f + 0.3f);
            float speed = size * 18f + (float)(rng.NextDouble() * 26f);
            float alpha = Mathf.Clamp01(size / 3f * 0.85f + 0.12f);

            var go = new GameObject("_Star");
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                (float)(rng.NextDouble() * refW - refW * 0.5f),
                (float)(rng.NextDouble() * refH - refH * 0.5f));
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.80f, 0.92f, 1f, alpha);
            img.raycastTarget = false;
            _stars[i]      = img;
            _starSpeeds[i] = speed;
        }
    }

    private void PulseTitle()
    {
        if (titleText == null) return;
        float t = (Mathf.Sin(Time.time * 1.1f) + 1f) * 0.5f;
        titleText.color = Color.Lerp(CyanDim, CyanBright, t);
    }

    private void BlinkEnter()
    {
        if (blinkText == null) return;
        _blinkTimer += Time.deltaTime;
        blinkText.enabled = (_blinkTimer % 1.4f) < 0.8f;
    }

    private void ScrollStars()
    {
        if (_stars == null) return;
        for (int i = 0; i < _stars.Length; i++)
        {
            if (_stars[i] == null) continue;
            var rt  = _stars[i].rectTransform;
            var pos = rt.anchoredPosition;
            pos.y -= _starSpeeds[i] * Time.deltaTime;
            if (pos.y < -330f)
            {
                pos.y = 330f;
                pos.x = Random.Range(-240f, 240f);
            }
            rt.anchoredPosition = pos;
        }
    }

    private IEnumerator FadeIn(CanvasGroup group)
    {
        group.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.8f;
            group.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    private Text FindChild(string childName)
    {
        Transform found = transform.Find(childName);
        return found != null ? found.GetComponent<Text>() : null;
    }
}
