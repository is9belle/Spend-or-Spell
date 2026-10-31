using UnityEngine;

public class TorchFlame : MonoBehaviour
{
    public Sprite flameSprite;
    public Vector3 flameBase = new Vector3(0f, 0.45f, 0f);   // where the flame sits, relative to the pillar
    public float flameScale = 2f;
    public Color glowColor = new Color(1f, 0.55f, 0.2f, 0.25f);
    public float glowSize = 4f;
    public int sortingOrder = 101;                            // above the PlayerLight darkness (100)

    Transform flame, glow;
    SpriteRenderer glowSr;
    float seed;

    public AudioClip crackle;
    public float crackleVolume = 0.35f;
    public float crackleRange = 9f;
    AudioSource src;

    void Start()
    {
        seed = Random.Range(0f, 100f);

        var f = new GameObject("Flame");
        flame = f.transform;
        flame.SetParent(transform, false);
        var fsr = f.AddComponent<SpriteRenderer>();
        fsr.sprite = flameSprite;
        fsr.sortingOrder = sortingOrder + 1;

        var g = new GameObject("Glow");
        glow = g.transform;
        glow.SetParent(transform, false);
        glowSr = g.AddComponent<SpriteRenderer>();
        glowSr.sprite = MakeGlow();
        glowSr.sortingOrder = sortingOrder;

        if (crackle != null)
        {
            src = gameObject.AddComponent<AudioSource>();
            src.clip = crackle; src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f;
            src.time = Random.value * crackle.length;     // so torches aren't in sync
            src.Play();
        }
    }

    Sprite MakeGlow()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
            float a = Mathf.Clamp01(1f - d);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / glowSize);
    }

    void Update()
    {
        float t = Time.time;
        float n1 = Mathf.PerlinNoise(t * 7f, seed);
        float n2 = Mathf.PerlinNoise(t * 5f, seed + 5f);

        float sx = flameScale * (1f + (n2 - 0.5f) * 0.18f);
        float sy = flameScale * (1f + (n1 - 0.5f) * 0.35f);
        flame.localScale = new Vector3(sx, sy, 1f);

        // keep the flame's base fixed, so it grows upward
        float half = flameSprite.bounds.extents.y;
        flame.localPosition = flameBase + new Vector3(0f, half * sy, 0f);

        glow.localPosition = flameBase + new Vector3(0f, half * flameScale, 0f);
        glow.localScale = Vector3.one * (1f + (n1 - 0.5f) * 0.2f);
        Color c = glowColor;
        c.a *= 0.8f + n1 * 0.4f;
        glowSr.color = c;

        if (src != null)
        {
            src.volume = crackleVolume * AudioManager.SfxVolume * AudioManager.DistanceVolume(transform.position, crackleRange);
            src.panStereo = AudioManager.Pan(transform.position);
        }
    }
}