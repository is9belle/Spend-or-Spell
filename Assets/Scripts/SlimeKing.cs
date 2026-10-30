using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class SlimeKing : MonoBehaviour
{
    [Header("Identity")]
    public string bossName = "Slime King";
    public string introSubtitle = "A boss approaches!";
    public float introTime = 2.8f;           // a tutorial/dialogue can hook in here later

    [Header("Endless mode scaling")]
    public int bossEveryNWaves = 10;         // used to work out which boss number this is
    public float healthPerLevel = 0.6f;      // +60% health each later appearance

    [Header("Minions")]
    public GameObject slimePrefab;           // your normal slime prefab
    public float smallScale = 0.8f;
    public float smallHealthMult = 1f;
    public float princeScale = 1.7f;
    public float princeHealthMult = 6f;
    public int maxMinions = 8;

    [Header("Phases (index 0 / 1 / 2)")]
    [Range(0f, 1f)] public float phase2At = 0.66f;
    [Range(0f, 1f)] public float phase3At = 0.33f;
    public float splitWindup = 1.3f;
    public int phase2Princes = 2;
    public int phase3Smalls = 4;
    public float[] sizeMult = { 1f, 0.8f, 0.65f };
    public float[] chaseSpeedMult = { 1f, 1.25f, 1.55f };
    public float[] chaseTime = { 3f, 2.4f, 1.8f };
    public float[] summonChance = { 0.4f, 0.3f, 0.3f };
    public int[] summonCount = { 3, 4, 5 };

    [Header("Slam")]
    public float slamRadius = 2.6f;
    public int slamDamage = 1;
    public float[] slamWindup = { 1.3f, 1.05f, 0.85f };
    public float jumpTime = 0.5f;
    public float jumpHeight = 1.4f;
    public float slamRecover = 0.8f;         // he stands still after slamming. This is your damage window

    [Header("Health bar")]
    public Font font;
    public float fontScale = 1.1f;
    [Range(0.2f, 0.8f)] public float barWidth = 0.46f;

    Enemy enemy;
    SlimeVisuals visuals;
    Rigidbody rb;
    FootprintCollider fp;
    Transform player;
    PlayerHealth playerHealth;
    readonly List<Enemy> minions = new List<Enemy>();

    int phase = 1;
    bool introDone, pendingSplit, splitting;
    float introStart, baseSpeed, ratioNow = 1f, ghost = 1f;
    GUIStyle introStyle, subStyle, nameStyle;

    bool Dead { get { return enemy == null || enemy.IsDead; } }
    float P(float[] a) { return a[Mathf.Min(phase - 1, a.Length - 1)]; }
    int P(int[] a) { return a[Mathf.Min(phase - 1, a.Length - 1)]; }

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        // endless mode: each later boss is tougher
        if (WaveManager.Instance != null && bossEveryNWaves > 0)
        {
            int level = Mathf.Max(1, WaveManager.Instance.Wave / bossEveryNWaves);
            enemy.maxHealth = Mathf.RoundToInt(enemy.maxHealth * (1f + healthPerLevel * (level - 1)));
        }
    }

    void Start()
    {
        visuals = GetComponentInChildren<SlimeVisuals>();
        rb = GetComponent<Rigidbody>();
        fp = GetComponent<FootprintCollider>();
        player = GameObject.FindWithTag("Player").transform;
        playerHealth = player.GetComponentInParent<PlayerHealth>();
        baseSpeed = enemy.speed;

        enemy.invulnerable = true;      // can't be hurt during the intro
        enemy.externalControl = true;
        introStart = Time.time;
        enemy.OnDeath += HandleDeath;
        if (GoldManager.Instance != null) GoldManager.Instance.infinite = true;
        StartCoroutine(Brain());
    }

    // ---------------- brain ----------------

    void Update()
    {
        if (Dead) return;
        ratioNow = enemy.Health > 0 ? Mathf.Clamp01((float)enemy.Health / enemy.maxHealth) : 1f;
        ghost = ghost < ratioNow ? ratioNow : Mathf.MoveTowards(ghost, ratioNow, 0.3f * Time.deltaTime);

        if (!introDone || splitting || pendingSplit) return;
        if (phase == 1 && ratioNow <= phase2At) pendingSplit = true;
        else if (phase == 2 && ratioNow <= phase3At) pendingSplit = true;
    }

    IEnumerator Brain()
    {
        yield return new WaitForSeconds(introTime);
        introDone = true;
        enemy.invulnerable = false;

        while (!Dead)
        {
            // chase for a bit
            enemy.externalControl = false;
            enemy.speed = baseSpeed * P(chaseSpeedMult);
            float until = Time.time + P(chaseTime);
            while (Time.time < until && !pendingSplit && !Dead) yield return null;
            if (Dead) yield break;

            if (pendingSplit) { yield return Split(); continue; }

            // then attack
            enemy.externalControl = true;
            Stop();
            bool canSummon = AliveMinions() < maxMinions;
            if (canSummon && Random.value < P(summonChance)) yield return Summon();
            else yield return SlamCombo();
        }
    }

    void Stop() { if (rb != null) rb.linearVelocity = Vector3.zero; }   // rb.velocity on older Unity

    void ResetPose()
    {
        visuals.bonusLift = 0f;
        visuals.bonusSquashX = 1f;
        visuals.bonusSquashY = 1f;
    }

    // ---------------- attacks ----------------

    IEnumerator SlamCombo()
    {
        int n = phase == 1 ? 1 : phase == 2 ? (Random.value < 0.35f ? 2 : 1) : (Random.value < 0.5f ? 3 : 2);
        for (int i = 0; i < n; i++)
        {
            yield return Slam();
            if (Dead) yield break;
        }
        yield return new WaitForSeconds(slamRecover);
    }

    IEnumerator Slam()
    {
        Vector3 target = ClampToArena(player.position);
        float windup = P(slamWindup);
        WarningZone.Spawn(target, slamRadius, windup);

        // crouch while the zone fills
        float crouch = Mathf.Max(0.15f, windup - jumpTime);
        float t = 0f;
        while (t < crouch)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / crouch);
            visuals.bonusSquashY = 1f - 0.3f * k;
            visuals.bonusSquashX = 1f + 0.2f * k;
            yield return null;
            if (Dead) yield break;
        }

        // leap toward the zone (contact damage is off while airborne)
        int savedContact = enemy.contactDamage;
        enemy.contactDamage = 0;
        t = 0f;
        while (t < jumpTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / jumpTime);
            float arc = Mathf.Sin(k * Mathf.PI);
            visuals.bonusLift = arc * jumpHeight;
            visuals.bonusSquashY = 1f + 0.25f * arc;
            visuals.bonusSquashX = 1f - 0.12f * arc;
            Vector3 to = target - transform.position;
            to.z = 0f;
            rb.linearVelocity = to / Mathf.Max(0.05f, jumpTime - t + Time.deltaTime);
            yield return null;
            if (Dead) yield break;
        }
        Stop();
        visuals.bonusLift = 0f;
        enemy.contactDamage = savedContact;
        Impact(target);

        // landing squash
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float k = t / 0.2f;
            visuals.bonusSquashY = Mathf.Lerp(0.7f, 1f, k);
            visuals.bonusSquashX = Mathf.Lerp(1.25f, 1f, k);
            yield return null;
            if (Dead) yield break;
        }
        ResetPose();
    }

    void Impact(Vector3 center)
    {
        WarningZone.Burst(center, slamRadius);
        if (playerHealth != null && Vector2.Distance(player.position, center) <= slamRadius)
            playerHealth.TakeHit(slamDamage);
    }

    IEnumerator Summon()
    {
        int n = Mathf.Min(P(summonCount), maxMinions - AliveMinions());

        float t = 0f, rear = 0.7f;
        while (t < rear)
        {
            t += Time.deltaTime;
            float k = t / rear;
            visuals.bonusSquashY = 1f + 0.3f * k;
            visuals.bonusSquashX = 1f - 0.15f * k;
            yield return null;
            if (Dead) yield break;
        }
        for (int i = 0; i < n; i++) SpawnMinion(smallScale, smallHealthMult);
        WarningZone.Burst(transform.position, 2.2f, 0.35f);
        ResetPose();
        yield return new WaitForSeconds(0.8f);
    }

    // ---------------- splitting (the phase changes) ----------------

    IEnumerator Split()
    {
        splitting = true;
        pendingSplit = false;
        enemy.externalControl = true;
        Stop();
        enemy.invulnerable = true;

        WarningZone.Spawn(transform.position, 3.4f, splitWindup);
        float t = 0f, nextFlash = 0f;
        while (t < splitWindup)
        {
            t += Time.deltaTime;
            float k = t / splitWindup;
            float shake = Mathf.Sin(t * 45f) * 0.05f;
            visuals.bonusSquashX = 1f + 0.3f * k + shake;
            visuals.bonusSquashY = 1f - 0.12f * k - shake;
            if (t >= nextFlash) { visuals.Flash(); nextFlash = t + 0.22f; }
            yield return null;
            if (Dead) yield break;
        }

        phase++;
        WarningZone.Burst(transform.position, 3.4f, 0.5f);
        if (phase == 2) for (int i = 0; i < phase2Princes; i++) SpawnMinion(princeScale, princeHealthMult);
        else            for (int i = 0; i < phase3Smalls; i++)  SpawnMinion(smallScale, smallHealthMult);

        // shrink to the new size
        float from = visuals.scaleMultiplier, to = P(sizeMult);
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            visuals.scaleMultiplier = Mathf.Lerp(from, to, t / 0.5f);
            visuals.bonusSquashX = Mathf.Lerp(visuals.bonusSquashX, 1f, 0.2f);
            visuals.bonusSquashY = Mathf.Lerp(visuals.bonusSquashY, 1f, 0.2f);
            yield return null;
            if (Dead) yield break;
        }
        visuals.scaleMultiplier = to;
        ResetPose();
        if (fp != null) fp.Fit();           // re-fit the hitbox to the smaller body

        enemy.invulnerable = false;
        splitting = false;
        yield return new WaitForSeconds(0.6f);
    }

    // ---------------- minions ----------------

    Enemy SpawnMinion(float scale, float healthMult)
    {
        if (slimePrefab == null) return null;
        Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2.2f, 3.4f));
        pos = ClampToArena(pos);
        var go = Instantiate(slimePrefab, pos, Quaternion.identity);

        var e = go.GetComponent<Enemy>();
        if (e != null)
        {
            WaveManager wm = WaveManager.Instance;
            float w = wm != null ? wm.Wave : 1f;
            float hg = wm != null ? wm.healthGrowth : 0f;
            float sg = wm != null ? wm.speedGrowth : 0f;
            e.countsForWave = false;
            e.maxHealth = Mathf.RoundToInt(e.maxHealth * healthMult * (1f + hg * (w - 1f)));
            e.speed *= 1f + sg * (w - 1f);
            e.coinsDropped = scale > 1f ? 3 : 1;
            minions.Add(e);
        }
        StartCoroutine(PopIn(go.transform, scale));
        return e;
    }

    IEnumerator PopIn(Transform tr, float multiplier)
    {
        Vector3 full = tr.localScale * multiplier;
        tr.localScale = full * 0.1f;
        float t = 0f;
        while (t < 0.3f && tr != null)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(full * 0.1f, full, t / 0.3f);
            yield return null;
        }
        if (tr != null) tr.localScale = full;
    }

    int AliveMinions()
    {
        minions.RemoveAll(m => m == null || m.IsDead);
        return minions.Count;
    }

    Vector3 ClampToArena(Vector3 p)
    {
        if (WaveManager.Instance != null)
        {
            Vector2 h = WaveManager.Instance.arenaHalfSize;
            p.x = Mathf.Clamp(p.x, -h.x + 2f, h.x - 2f);
            p.y = Mathf.Clamp(p.y, -h.y + 2f, h.y - 2f);
        }
        p.z = 0f;
        return p;
    }

    void HandleDeath()
    {
        if (GoldManager.Instance != null) GoldManager.Instance.infinite = false;
        StopAllCoroutines();
        if (visuals != null) ResetPose();
        foreach (var m in minions)
        {
            if (m == null || m.IsDead) continue;
            var rbm = m.GetComponent<Rigidbody>();
            if (rbm != null) rbm.linearVelocity = Vector3.zero;
            var v = m.GetComponentInChildren<SlimeVisuals>();
            if (v != null) v.Die(m.gameObject); else Destroy(m.gameObject);
        }
        minions.Clear();
    }

    void OnDestroy()
    {
        if (GoldManager.Instance != null) GoldManager.Instance.infinite = false;
    }

    // ---------------- intro banner + health bar ----------------

    void EnsureStyles()
    {
        if (introStyle != null) return;
        introStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
    }

    void OnGUI()
    {
        if (enemy == null || enemy.IsDead) return;
        EnsureStyles();
        GUI.depth = 4;

        float w = Screen.width, h = Screen.height;
        float s = h / 1080f, f = s * fontScale;
        introStyle.font = font; subStyle.font = font; nameStyle.font = font;
        introStyle.fontSize = Mathf.RoundToInt(150f * f);
        subStyle.fontSize = Mathf.RoundToInt(44f * f);
        nameStyle.fontSize = Mathf.RoundToInt(40f * f);

        // intro banner
        float elapsed = Time.time - introStart;
        if (elapsed < introTime)
        {
            float a = Mathf.Clamp01(elapsed / 0.4f) * Mathf.Clamp01((introTime - elapsed) / 0.5f);
            MagicGui.Shadowed(new Rect(0, h * 0.30f, w, 190f * s), bossName, introStyle, new Color(0.65f, 0.95f, 0.45f), 6f * s, a);
            MagicGui.Shadowed(new Rect(0, h * 0.30f + 180f * s, w, 70f * s), introSubtitle, subStyle, new Color(1f, 0.92f, 0.6f), 3f * s, a);
        }

        // health bar
        float bw = w * barWidth, bh = 34f * s;
        float bx = (w - bw) / 2f, by = 78f * s;
        float rad = bh / 2f;
        float minW = bh + 6f * s;
        float b = 5f * s;

        MagicGui.Shadowed(new Rect(bx, by - 54f * s, bw, 50f * s), bossName, nameStyle, new Color(1f, 0.92f, 0.6f), 2f * s);
        MagicGui.RFill(new Rect(bx - b, by - b, bw + b * 2f, bh + b * 2f), new Color(0.12f, 0.05f, 0.1f), rad + b);
        MagicGui.RFill(new Rect(bx, by, bw, bh), new Color(0.05f, 0.02f, 0.04f, 0.95f), rad);

        if (ghost > 0.01f)
            MagicGui.RFill(new Rect(bx, by, Mathf.Max(minW, bw * ghost), bh), new Color(1f, 0.85f, 0.5f, 0.85f), rad);
        if (ratioNow > 0.01f)
        {
            Rect fill = new Rect(bx, by, Mathf.Max(minW, bw * ratioNow), bh);
            MagicGui.RFill(fill, new Color(0.8f, 0.14f, 0.18f), rad);
            MagicGui.RFill(new Rect(fill.x + 4f * s, fill.y + 3f * s, fill.width - 8f * s, bh * 0.35f), new Color(1f, 1f, 1f, 0.18f), rad * 0.6f);
        }

        // phase markers
        MagicGui.Fill(new Rect(bx + bw * phase2At - 2f * s, by, 4f * s, bh), new Color(0f, 0f, 0f, 0.85f));
        MagicGui.Fill(new Rect(bx + bw * phase3At - 2f * s, by, 4f * s, bh), new Color(0f, 0f, 0f, 0.85f));
    }
}