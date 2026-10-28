using UnityEngine;

public class Breakable : MonoBehaviour
{
    public int hits = 2;
    public string[] hitByNames = { "fireball", "ice" };  // projectile names that damage it
    public GameObject[] drops;                            // e.g. mana crystal, heart
    [Range(0f, 1f)] public float dropChance = 0.5f;
    public Color flashColor = new Color(1f, 0.55f, 0.55f);

    SpriteRenderer sr;
    Color baseColor;
    Vector3 baseLocal;
    float flashUntil, shakeUntil, nextHit;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseLocal = sr.transform.localPosition;
    }

    void OnCollisionEnter(Collision c) { Check(c.gameObject); }
    void OnTriggerEnter(Collider o) { Check(o.gameObject); }

    void Check(GameObject o)
    {
        var proj = o.GetComponentInParent<Projectile>();
        if (proj != null)
        {
            Destroy(proj.gameObject);   // the bolt stops here
            Hit();
            return;
        }

        string n = o.name.ToLower();
        foreach (var s in hitByNames)
            if (n.Contains(s)) { Hit(); return; }
    }

    public void Hit()
    {
        if (Time.time < nextHit) return;     // ignore double-triggers
        nextHit = Time.time + 0.05f;

        if (Time.time >= flashUntil) baseColor = sr.color;
        flashUntil = Time.time + 0.1f;
        shakeUntil = Time.time + 0.15f;

        if (--hits <= 0) Break();
    }

    void Break()
    {
        if (drops != null && drops.Length > 0 && Random.value < dropChance)
            Instantiate(drops[Random.Range(0, drops.Length)], transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void Update()
    {
        if (sr == null) return;
        if (Time.time < flashUntil) sr.color = flashColor;
        else if (flashUntil > 0f && sr.color == flashColor) sr.color = baseColor;

        sr.transform.localPosition = Time.time < shakeUntil
            ? baseLocal + (Vector3)Random.insideUnitCircle * 0.03f
            : baseLocal;
    }
}