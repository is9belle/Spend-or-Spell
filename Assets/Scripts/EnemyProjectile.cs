using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    Vector3 dir;
    float speed, life = 5f;
    int damage;
    static Sprite fallback;

    public static void Spawn(Vector3 pos, Vector3 dir, Sprite sprite, float scale, float speed, int damage)
    {
        var go = new GameObject("EnemyArrow");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        go.transform.localScale = Vector3.one * scale;

        var p = go.AddComponent<EnemyProjectile>();
        p.dir = dir.normalized;
        p.speed = speed;
        p.damage = damage;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : Fallback();
        sr.sortingOrder = 70;

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.25f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    static Sprite Fallback()
    {
        if (fallback != null) return fallback;
        var tex = new Texture2D(16, 5, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < 5; y++) for (int x = 0; x < 16; x++) tex.SetPixel(x, y, clear);
        Color shaft = new Color(0.55f, 0.36f, 0.2f), head = new Color(0.85f, 0.88f, 0.95f), fletch = new Color(0.8f, 0.2f, 0.2f);
        for (int x = 1; x < 13; x++) tex.SetPixel(x, 2, shaft);
        for (int x = 0; x < 3; x++) { tex.SetPixel(x, 1, fletch); tex.SetPixel(x, 3, fletch); }
        tex.SetPixel(12, 1, head); tex.SetPixel(12, 3, head);
        for (int x = 12; x < 16; x++) tex.SetPixel(x, 2, head);
        tex.SetPixel(13, 1, head); tex.SetPixel(13, 3, head);
        tex.Apply();
        fallback = Sprite.Create(tex, new Rect(0, 0, 16, 5), new Vector2(0.5f, 0.5f), 24f);
        return fallback;
    }

    void Update()
    {
        transform.position += dir * speed * Time.deltaTime;
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider o)
    {
        if (o.isTrigger) return;                                   // ignore pickups
        if (o.GetComponentInParent<Enemy>() != null) return;       // don't hit other enemies
        if (o.GetComponentInParent<Projectile>() != null) return;  // pass through your bolts

        var hp = o.GetComponentInParent<PlayerHealth>();
        if (hp != null) hp.TakeHit(damage);
        Destroy(gameObject);                                       // stops on the player and on obstacles
    }
}