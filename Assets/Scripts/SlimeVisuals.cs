using UnityEngine;

public class SlimeVisuals : MonoBehaviour
{
    [Header("Hop")]
    public Sprite[] frames;               // the 8 slime frames, in order
    public float hopTime = 0.8f;          // seconds for one full hop
    public float hopHeight = 0.25f;       // how high it lifts, in world units
    public bool randomStart = true;       // so a group doesn't hop in sync

    [Header("Hit flash")]
    public Color flashColor = new Color(1f, 0.35f, 0.35f);
    public float flashTime = 0.12f;
    public float hitPunch = 0.15f;        // quick squash when hit

    [Header("Death")]
    public float deathTime = 0.35f;

    [Header("Shadow")]
    public bool shadow = true;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    public Vector2 shadowSize = new Vector2(0.7f, 0.22f);
    public float groundOffset = -0.45f;   // where the feet are (local units, at scale 1)

    SpriteRenderer sr;
    Vector3 baseLocal, baseScale;
    Color baseColor;
    float offset, flashTimer, deathT;
    bool flashing, dying;
    Transform shadowT;
    SpriteRenderer shadowSr;

    // lets a movement script move the slime only while it's in the air
    public bool IsAirborne
    {
        get { float p = Phase(); return !dying && p > 0.25f && p < 0.75f; }
    }
    public bool IsFlashing { get { return flashing; } }
    public bool IsDying { get { return dying; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseLocal = transform.localPosition;
        baseScale = transform.localScale;
        if (randomStart) offset = Random.Range(0f, 10f);
    }

    void Start()
    {
        if (!shadow) return;
        var g = new GameObject("Shadow");
        shadowT = g.transform;
        shadowT.SetParent(transform, false);
        shadowSr = g.AddComponent<SpriteRenderer>();
        shadowSr.sprite = MakeShadow();
    }

    Sprite MakeShadow()
    {
        int w = 64, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x - w / 2f) / (w / 2f), dy = (y - h / 2f) / (h / 2f);
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Sqrt(a)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);   // exactly 1 unit wide
    }

    float Phase() { return ((Time.time + offset) / hopTime) % 1f; }

    // ---- call these from your enemy script ----
    public void Flash()
    {
        if (dying) return;
        if (!flashing) baseColor = sr.color;
        flashing = true;
        flashTimer = flashTime;
    }

    GameObject deathRoot;

    public void Die(GameObject root)
    {
        if (dying) return;
        if (!flashing) baseColor = sr.color;
        dying = true;
        deathT = 0f;
        deathRoot = root != null ? root : gameObject;

        foreach (var c in deathRoot.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var mb in deathRoot.GetComponents<MonoBehaviour>())
            if (mb != this) mb.enabled = false;
        var rb = deathRoot.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Debug.Log("Slime death started on " + deathRoot.name);
    }

    void UpdateDeath()
    {
        deathT += Time.deltaTime;
        float k = Mathf.Clamp01(deathT / deathTime);

        // quick pop up first, then squash flat and fade out
        float sx, sy;
        if (k < 0.3f) { float p = k / 0.3f; sx = 1f - 0.15f * p; sy = 1f + 0.25f * p; }
        else { float p = (k - 0.3f) / 0.7f; sx = 0.85f + 0.65f * p; sy = 1.25f - 1.1f * p; }

        Color c = flashColor;
        c.a = k < 0.3f ? 1f : 1f - (k - 0.3f) / 0.7f;
        sr.color = c;

        SetSquash(sx, sy, 0f);
        UpdateShadow(0f, c.a);

        if (k >= 1f) Destroy(deathRoot);
    }

    GameObject Target()
    {
        var col = GetComponentInParent<Collider>();
        return col != null ? col.gameObject : gameObject;
    }

    // squash/stretch while keeping the feet on the ground
    void SetSquash(float sx, float sy, float lift)
    {
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        float drop = groundOffset * (1f - sy) * baseScale.y;
        transform.localPosition = baseLocal + Vector3.up * (lift + drop);
    }

    void Update()
    {
        if (sr == null) return;
        if (dying) { UpdateDeath(); return; }

        // hop
        float phase = Phase();
        float air = Mathf.Clamp01((phase - 0.25f) / 0.5f);
        float lift = Mathf.Sin(air * Mathf.PI) * hopHeight;
        if (frames != null && frames.Length > 0)
            sr.sprite = frames[Mathf.Min((int)(phase * frames.Length), frames.Length - 1)];

        // hit flash + punch
        float sx = 1f, sy = 1f;
        if (flashing)
        {
            flashTimer -= Time.deltaTime;
            float k = Mathf.Clamp01(flashTimer / flashTime);
            sr.color = flashColor;
            sx = 1f + hitPunch * k;
            sy = 1f - hitPunch * k;
            if (flashTimer <= 0f) { sr.color = baseColor; flashing = false; }
        }

        SetSquash(sx, sy, lift);
        UpdateShadow(lift, 1f);
    }

    void UpdateShadow(float lift, float fade)
    {
        if (shadowT == null) return;
        Vector3 basePos = transform.parent != null ? transform.parent.TransformPoint(baseLocal) : baseLocal;
        shadowT.position = basePos + Vector3.up * (groundOffset * baseScale.y);
        Vector3 ls = transform.lossyScale;
        float shrink = 1f - 0.4f * Mathf.Clamp01(lift / Mathf.Max(0.001f, hopHeight));
        shadowT.localScale = new Vector3(shadowSize.x * shrink / ls.x, shadowSize.y * shrink / ls.y, 1f);
        Color c = shadowColor;
        c.a *= fade * shrink;
        shadowSr.color = c;
        shadowSr.sortingOrder = sr.sortingOrder - 1;
    }
}