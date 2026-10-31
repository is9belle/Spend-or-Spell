using UnityEngine;
using System.Collections.Generic;

public enum SpellType { Fire, Frost }

[System.Serializable]
public class Spell
{
    public string name;
    public SpellType type;
    public int cost = 1;
    public int damage = 1;
    public float cooldown = 0.25f;
    public bool unlocked;
    public Sprite icon;      // shown on the spell bar
    public Color color;      // slot color. Leave it blank (alpha 0) to use the default for its type

    // runtime only, not saved in the Inspector
    [System.NonSerialized] public float nextCast;       // when this spell can be cast again
    [System.NonSerialized] public float lastCd = 0.25f; // length of its current cooldown
}

public class PlayerCaster : MonoBehaviour
{
    public Projectile projectilePrefab;
    public List<Spell> spells = new List<Spell>();

    public float damageMultiplier = 1f;   // shop upgrades raise this
    public float cooldownMultiplier = 1f; // shop upgrades lower this (faster casting)

    public float novaRadius = 4f;
    public float slowFactor = 0.4f;       // enemy speed multiplier while slowed
    public float slowDuration = 2.5f;

    [Header("Frost Nova visual")]
    public Sprite[] novaFrames;           // one sprite, or several frames in order
    public float novaFxTime = 0.4f;

    [Header("Spell bar")]
    public Font font;                     // your gothic font
    public Font bodyFont;                 // optional, easier to read for the small stats
    public float fontScale = 1.1f;

    int selected;
    PlayerController player;
    GUIStyle nameStyle, statStyle, keyStyle;

    void Awake()
    {
        if (spells == null || spells.Count == 0) BuildDefaults();
    }

    void OnValidate()
    {
        if (spells == null || spells.Count == 0) BuildDefaults();
    }

    void BuildDefaults()
    {
        spells = new List<Spell>
        {
            new Spell { name = "Fire Bolt",  type = SpellType.Fire,  cost = 1, damage = 10, cooldown = 0.25f, unlocked = true },
            new Spell { name = "Frost Nova", type = SpellType.Frost, cost = 4, damage = 5, cooldown = 1f,    unlocked = false },
        };
    }

    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    public bool IsUnlocked(SpellType t)
    {
        foreach (Spell s in spells)
            if (s.type == t) return s.unlocked;
        return true;
    }

    public void Unlock(SpellType t)
    {
        foreach (Spell s in spells)
            if (s.type == t) s.unlocked = true;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        for (int i = 0; i < spells.Count; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && spells[i].unlocked) selected = i;

        Spell s = spells[selected];                       // must come BEFORE the if below
        if (Input.GetMouseButton(0) && !AudioManager.PointerOverUI && Time.time >= s.nextCast)
        {
            if (GoldManager.Instance.CanAfford(s.cost) && TryCast(s))
            {
                GoldManager.Instance.TrySpend(s.cost);
                s.lastCd = s.cooldown * cooldownMultiplier;
                s.nextCast = Time.time + s.lastCd;
            }
        }
    }

    int Damage(Spell s)
    {
        return Mathf.Max(1, Mathf.RoundToInt(s.damage * damageMultiplier));
    }

    bool TryCast(Spell s)
    {
        switch (s.type)
        {
            case SpellType.Fire:  return CastFire(s);
            case SpellType.Frost: return CastFrost(s);
        }
        return false;
    }

    bool CastFire(Spell s)
    {
        Vector3 dir = player.AimDirection();
        Projectile p = Instantiate(projectilePrefab,
            transform.position + dir * 1f, Quaternion.identity);
        p.damage = Damage(s);
        p.Launch(dir);
        AudioManager.FireCast(transform.position);
        return true;
    }

    bool CastFrost(Spell s)
    {
        bool any = false;
        foreach (Collider c in Physics.OverlapSphere(transform.position, novaRadius))
        {
            Enemy e = c.GetComponent<Enemy>();
            if (e == null)
            {
                var b = c.GetComponentInParent<Breakable>();
                if (b != null) { b.Hit(); any = true; }
                continue;
            }
            e.Slow(slowFactor, slowDuration);
            e.TakeDamage(Damage(s));
            any = true;
        }
        if (any) { ShowNovaFlash(); AudioManager.FrostCast(transform.position); }
        return any; // nothing in range = no cast, no gold spent, no cooldown
    }

    void ShowNovaFlash()
    {
        if (novaFrames != null && novaFrames.Length > 0 && novaFrames[0] != null)
        {
            NovaEffect.Spawn(new Vector3(transform.position.x, transform.position.y, 0f),
                             novaFrames, novaRadius * 2f, novaFxTime);
            return;
        }

        // fallback: the old placeholder disc, used when no sprites are assigned
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(disc.GetComponent<Collider>());
        disc.transform.position = new Vector3(transform.position.x, transform.position.y, 0.5f);
        disc.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localScale = new Vector3(novaRadius * 2f, 0.02f, novaRadius * 2f);
        disc.GetComponent<Renderer>().material.color = new Color(0.5f, 0.8f, 1f);
        Destroy(disc, 0.2f);
    }

    // ---------------- spell bar ----------------

    Color SlotColor(Spell s)
    {
        if (s.color.a > 0.01f) return s.color;
        switch (s.type)
        {
            case SpellType.Fire:  return new Color(0.78f, 0.30f, 0.12f);
            case SpellType.Frost: return new Color(0.15f, 0.40f, 0.78f);
        }
        return new Color(0.4f, 0.3f, 0.6f);
    }

    void BarStyles(float f)
    {
        if (nameStyle == null)
        {
            nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            statStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            keyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        }
        Font body = bodyFont != null ? bodyFont : font;
        nameStyle.font = font;
        statStyle.font = body;
        keyStyle.font = font;
        nameStyle.fontSize = Mathf.RoundToInt(30f * f);
        statStyle.fontSize = Mathf.RoundToInt(25f * f);
        keyStyle.fontSize = Mathf.RoundToInt(26f * f);
    }

    void DrawIcon(Sprite sp, Rect box, float alpha)
    {
        if (sp == null) return;
        Texture2D t = sp.texture;
        Rect tr = sp.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);

        float aspect = tr.width / tr.height;
        float w = box.width, h = box.height;
        if (aspect > 1f) h = w / aspect; else w = h * aspect;
        Rect fit = new Rect(box.x + (box.width - w) / 2f, box.y + (box.height - h) / 2f, w, h);

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTextureWithTexCoords(fit, t, uv);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (spells == null || spells.Count == 0) return;
        GUI.depth = 5;   // drawn behind the shop and game over screens

        float sc = Screen.height / 1080f;
        BarStyles(sc * fontScale);

        Color cream = new Color(1f, 0.92f, 0.6f);
        GoldManager gm = GoldManager.Instance;
        Sprite manaIcon = gm != null ? gm.manaIcon : null;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);

        float slotW = 362f * sc, slotH = 150f * sc, gap = 18f * sc, pad = 10f * sc;
        float rad = 22f * sc;
        float baseY = Screen.height - slotH - 18f * sc;

        for (int i = 0; i < spells.Count; i++)
        {
            Spell sp = spells[i];
            bool unlocked = sp.unlocked;
            bool sel = unlocked && i == selected;
            bool affordable = gm != null && gm.CanAfford(sp.cost);

            // each spell has its own cooldown
            float remaining = Mathf.Max(0f, sp.nextCast - Time.time);
            bool ready = remaining <= 0f;
            float progress = sp.lastCd > 0.0001f ? 1f - Mathf.Clamp01(remaining / sp.lastCd) : 1f;

            Rect r = new Rect(14f * sc + i * (slotW + gap), baseY - (sel ? 10f * sc : 0f), slotW, slotH);
            Color col = unlocked ? SlotColor(sp) : new Color(0.22f, 0.23f, 0.28f);

            // glow around the selected spell
            if (sel)
            {
                float g = (8f + 4f * pulse) * sc;
                MagicGui.RFill(new Rect(r.x - g, r.y - g, r.width + g * 2f, r.height + g * 2f),
                               new Color(col.r + 0.3f, col.g + 0.3f, col.b + 0.3f, 0.35f + 0.2f * pulse), rad + g);
            }

            // frame + solid body
            float b = 5f * sc;
            MagicGui.RFill(new Rect(r.x - b, r.y - b, r.width + b * 2f, r.height + b * 2f),
                           sel ? Color.Lerp(col, Color.white, 0.7f) : Color.Lerp(col, Color.black, 0.6f), rad + b);
            MagicGui.RFill(r, col, rad);

            // icon panel
            Rect panel = new Rect(r.x + pad, r.y + pad, r.height - pad * 2f, r.height - pad * 2f);
            MagicGui.RFill(panel, Color.Lerp(col, Color.white, 0.16f), rad * 0.6f);
            DrawIcon(sp.icon, new Rect(panel.x + 8f * sc, panel.y + 8f * sc, panel.width - 16f * sc, panel.height - 16f * sc),
                     unlocked ? 1f : 0.3f);

            // key number badge
            Rect badge = new Rect(panel.x - 8f * sc, panel.y - 8f * sc, 40f * sc, 40f * sc);
            MagicGui.RFill(new Rect(badge.x - 2f * sc, badge.y - 2f * sc, badge.width + 4f * sc, badge.height + 4f * sc),
                           new Color(1f, 0.85f, 0.4f), 14f * sc);
            MagicGui.RFill(badge, new Color(0.08f, 0.06f, 0.12f, 0.96f), 12f * sc);
            MagicGui.Shadowed(badge, (i + 1).ToString(), keyStyle, cream, 1.5f * sc);

            // text column
            float cx = panel.xMax + 14f * sc;
            float cw = r.xMax - cx - pad;

            if (!unlocked)
            {
                MagicGui.Shadowed(new Rect(cx, r.y + 8f * sc, cw, 36f * sc), "Locked", nameStyle, new Color(0.65f, 0.66f, 0.75f), 2f * sc);
                MagicGui.Shadowed(new Rect(cx, r.y + 50f * sc, cw, 60f * sc), "Learn it in\nthe spellbook", statStyle, new Color(0.55f, 0.56f, 0.65f), 1.5f * sc);
                continue;
            }

            MagicGui.Shadowed(new Rect(cx, r.y + 8f * sc, cw, 36f * sc), sp.name, nameStyle, Color.white, 2f * sc);

            // mana cost
            Color costCol = affordable ? cream : new Color(1f, 0.45f, 0.4f);
            float rowY = r.y + 46f * sc;
            if (manaIcon != null)
            {
                DrawIcon(manaIcon, new Rect(cx, rowY + 2f * sc, 26f * sc, 26f * sc), 1f);
                MagicGui.Shadowed(new Rect(cx + 32f * sc, rowY, cw - 32f * sc, 30f * sc), sp.cost.ToString(), statStyle, costCol, 1.5f * sc);
            }
            else
            {
                MagicGui.Shadowed(new Rect(cx, rowY, cw, 30f * sc), "Cost: " + sp.cost + " mana", statStyle, costCol, 1.5f * sc);
            }

            // damage (turns green when shop upgrades raise it)
            int dmg = Damage(sp);
            Color dmgCol = dmg > sp.damage ? new Color(0.7f, 1f, 0.6f) : Color.white;
            MagicGui.Shadowed(new Rect(cx, r.y + 76f * sc, cw, 30f * sc), "Damage: " + dmg, statStyle, dmgCol, 1.5f * sc);

            // cooldown text + bar
            string cdText = ready ? "Ready" : remaining.ToString("0.0") + "s";
            Color cdCol = ready ? new Color(0.75f, 1f, 0.7f) : new Color(1f, 0.7f, 0.4f);
            MagicGui.Shadowed(new Rect(cx, r.y + 104f * sc, cw, 30f * sc), cdText, statStyle, cdCol, 1.5f * sc);

            float barY = r.y + 136f * sc, barH = 8f * sc;
            MagicGui.RFill(new Rect(cx, barY, cw, barH), new Color(0f, 0f, 0f, 0.45f), barH / 2f);
            if (progress > 0.03f)
                MagicGui.RFill(new Rect(cx, barY, Mathf.Max(barH, cw * progress), barH), cdCol, barH / 2f);

            // darken slots you can't afford right now
            if (!affordable)
                MagicGui.RFill(r, new Color(0f, 0f, 0f, 0.3f), rad);
        }
        GUI.color = Color.white;
    }
}