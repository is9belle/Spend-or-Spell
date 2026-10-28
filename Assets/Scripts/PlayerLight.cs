using UnityEngine;

public class PlayerLight : MonoBehaviour
{
    public bool layFlat = false;                         // tick if your camera looks down at the floor
    [Range(0f, 1f)] public float darkness = 0.3f;        // how dark it gets away from the player
    public float lightRadius = 7f;                       // size of the lit area, in world units
    [Range(0.05f, 0.95f)] public float softness = 0.6f;  // higher = softer edge
    public Color darkColor = new Color(0.02f, 0.03f, 0.1f);
    public float coverSize = 60f;                        // how big the dark sprite is
    [Range(0f, 0.1f)] public float flicker = 0.03f;

    SpriteRenderer sr;
    Transform t;
    string last;

    void Start()
    {
        var go = new GameObject("PlayerLight");
        t = go.transform;
        t.SetParent(transform, false);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;                           // draws above sprites, below the UI
        Build();
    }

    string Key() => $"{darkness}|{lightRadius}|{softness}|{darkColor}|{coverSize}|{layFlat}";

    void Build()
    {
        last = Key();
        int n = 512;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float radiusPx = lightRadius / coverSize * n;
        float inner = 1f - softness;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / radiusPx;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, 1f, d)) * darkness;
            tex.SetPixel(x, y, new Color(darkColor.r, darkColor.g, darkColor.b, a));
        }
        tex.Apply();

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / coverSize);

        t.localPosition = layFlat ? new Vector3(0f, 1f, 0f) : Vector3.zero;
        t.localRotation = layFlat ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
    }

    void Update()
    {
        if (Key() != last) Build();
        float f = 1f + (Mathf.PerlinNoise(Time.time * 3f, 0f) - 0.5f) * 2f * flicker;
        Vector3 ls = transform.lossyScale;
        t.localScale = new Vector3(f / ls.x, f / ls.y, f / ls.z);
    }
}