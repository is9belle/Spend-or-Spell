using UnityEngine;

public class NovaEffect : MonoBehaviour
{
    Sprite[] frames;
    float duration, diameter, startScale = 0.3f;
    float t, baseScale;
    SpriteRenderer sr;

    public static void Spawn(Vector3 pos, Sprite[] frames, float diameter, float duration)
    {
        var go = new GameObject("NovaEffect");
        go.transform.position = pos;
        var fx = go.AddComponent<NovaEffect>();
        fx.frames = frames;
        fx.diameter = diameter;
        fx.duration = Mathf.Max(0.05f, duration);
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.sortingOrder = 60;
        fx.sr.sprite = frames[0];
        fx.baseScale = diameter / frames[0].bounds.size.x;
        fx.Apply(0f);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        Apply(k);
        if (k >= 1f) Destroy(gameObject);
    }

    void Apply(float k)
    {
        float grow = 1f - Mathf.Pow(1f - k, 2f);                      // fast at first, then eases out
        transform.localScale = Vector3.one * baseScale * Mathf.Lerp(startScale, 1f, grow);
        sr.sprite = frames[Mathf.Min((int)(k * frames.Length), frames.Length - 1)];
        float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;             // fades over the last 40%
        sr.color = new Color(1f, 1f, 1f, a);
    }
}