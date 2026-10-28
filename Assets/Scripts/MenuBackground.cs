using UnityEngine;
using UnityEngine.UI;

public class MenuBackground : MonoBehaviour
{
    public int seed = 1;
    [Header("Sprites")]
    public Sprite[] floorSprites;   // your 8 floor tiles
    public Sprite torchSprite;
    public Sprite flameSprite;

    [Header("Floor")]
    public float tileSize = 150f;
    public float artFill = 0.78f;
    [Range(0f, 1f)] public float darkness = 0.55f;

    [Header("Torches")]
    public Vector2 leftTorchPos = new Vector2(-700f, -120f);
    public Vector2 rightTorchPos = new Vector2(700f, -120f);
    public float torchScale = 8f;
    public float glowSize = 1000f;
    public Color glowColor = new Color(1f, 0.55f, 0.2f, 0.35f);

    [Header("Particles")]
    public int particleCount = 50;
    public Color emberColor = new Color(1f, 0.65f, 0.25f);
    public Color dustColor = new Color(0.6f, 0.7f, 1f);

    class P
    {
        public RectTransform rt; public Image img;
        public Vector2 pos;
        public float speed, sway, phase, age, life, maxA;
        public bool ember;
    }

    RectTransform root;
    RectTransform flameL, flameR, glowL, glowR;
    Image glowImgL, glowImgR;
    Vector2[] flameTops = new Vector2[2];
    Sprite glowSprite;
    P[] ps;

    void Start()
    {
        Random.InitState(seed);
        root = (RectTransform)transform;
        glowSprite = MakeGlow();

        BuildFloor();
        Make("Darkness", null, new Vector2(5000f, 4000f), Vector2.zero, new Color(0f, 0f, 0f, darkness));

        BuildTorch(leftTorchPos, 0, out glowL, out flameL, out glowImgL);
        BuildTorch(rightTorchPos, 1, out glowR, out flameR, out glowImgR);
        BuildParticles();
    }

    Image Make(string n, Sprite s, Vector2 size, Vector2 pos, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        return img;
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
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    void BuildFloor()
    {
        if (floorSprites == null || floorSprites.Length == 0) return;
        int cols = Mathf.CeilToInt(2600f / tileSize);
        int rows = Mathf.CeilToInt(1500f / tileSize);
        float size = tileSize / artFill;

        for (int i = 0; i < cols; i++)
        for (int j = 0; j < rows; j++)
        {
            Vector2 pos = new Vector2((i - (cols - 1) / 2f) * tileSize, (j - (rows - 1) / 2f) * tileSize);
            Sprite sp = floorSprites[Random.Range(0, floorSprites.Length)];
            Make("Tile", sp, new Vector2(size, size), pos, Color.white);
        }
    }

    void BuildTorch(Vector2 pos, int index, out RectTransform glow, out RectTransform flame, out Image glowImg)
    {
        Vector2 tsize = torchSprite.rect.size * torchScale;
        Vector2 fsize = flameSprite.rect.size * torchScale;
        float topY = pos.y + tsize.y / 2f;

        glowImg = Make("Glow", glowSprite, Vector2.one * glowSize, new Vector2(pos.x, topY + fsize.y * 0.4f), glowColor);
        glow = glowImg.rectTransform;

        Make("Torch", torchSprite, tsize, pos, Color.white);

        var fimg = Make("Flame", flameSprite, fsize, Vector2.zero, Color.white);
        flame = fimg.rectTransform;
        flame.pivot = new Vector2(0.5f, 0f);                       // flicker grows upward
        flame.anchoredPosition = new Vector2(pos.x, topY - fsize.y * 0.1f);

        flameTops[index] = new Vector2(pos.x, topY + fsize.y * 0.3f);
    }

    void BuildParticles()
    {
        ps = new P[particleCount];
        for (int i = 0; i < ps.Length; i++)
        {
            var p = new P();
            p.ember = i % 2 == 0;
            p.img = Make("Particle", null, Vector2.one, Vector2.zero, Color.white);
            p.rt = p.img.rectTransform;
            Respawn(p, true);
            ps[i] = p;
        }
    }

    void Respawn(P p, bool initial)
    {
        if (p.ember)
        {
            Vector2 t = flameTops[Random.Range(0, 2)];
            p.pos = new Vector2(t.x + Random.Range(-50f, 50f), t.y);
            p.speed = Random.Range(60f, 140f);
            p.life = Random.Range(2f, 4f);
            p.sway = Random.Range(10f, 30f);
            p.maxA = 0.9f;
            float s = Random.Range(6f, 12f);
            p.rt.sizeDelta = new Vector2(s, s);
            p.img.color = emberColor;
        }
        else
        {
            p.pos = new Vector2(Random.Range(-1100f, 1100f), Random.Range(-650f, 100f));
            p.speed = Random.Range(15f, 40f);
            p.life = Random.Range(8f, 14f);
            p.sway = Random.Range(20f, 50f);
            p.maxA = 0.45f;
            float s = Random.Range(4f, 8f);
            p.rt.sizeDelta = new Vector2(s, s);
            p.img.color = dustColor;
        }
        p.phase = Random.Range(0f, 6.28f);
        p.age = initial ? Random.Range(0f, p.life) : 0f;
        p.pos.y += p.speed * p.age;     // so the first frame is already spread out
    }

    void Update()
    {
        float t = Time.unscaledTime;
        Flicker(flameL, glowL, glowImgL, t, 0f);
        Flicker(flameR, glowR, glowImgR, t, 10f);

        if (ps == null) return;
        float dt = Time.unscaledDeltaTime;
        foreach (var p in ps)
        {
            p.age += dt;
            if (p.age >= p.life) Respawn(p, false);

            p.pos.y += p.speed * dt;
            float x = p.pos.x + Mathf.Sin(p.age * 1.5f + p.phase) * p.sway;
            p.rt.anchoredPosition = new Vector2(x, p.pos.y);

            Color c = p.img.color;
            c.a = Mathf.Sin(p.age / p.life * Mathf.PI) * p.maxA;
            p.img.color = c;
        }
    }

    void Flicker(RectTransform flame, RectTransform glow, Image glowImg, float t, float seed)
    {
        float n1 = Mathf.PerlinNoise(t * 7f, seed);
        float n2 = Mathf.PerlinNoise(t * 5f, seed + 5f);
        flame.localScale = new Vector3(1f + (n2 - 0.5f) * 0.18f, 1f + (n1 - 0.5f) * 0.35f, 1f);
        glow.localScale = Vector3.one * (1f + (n1 - 0.5f) * 0.25f);
        Color c = glowColor;
        c.a *= 0.8f + n1 * 0.4f;
        glowImg.color = c;
    }
}