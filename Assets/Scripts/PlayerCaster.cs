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

    int selected;
    float nextCast;
    PlayerController player;
    GUIStyle slotStyle;

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

        if (Input.GetMouseButton(0) && Time.time >= nextCast)
        {
            Spell s = spells[selected];
            if (GoldManager.Instance.gold >= s.cost && TryCast(s))
            {
                GoldManager.Instance.TrySpend(s.cost);
                nextCast = Time.time + s.cooldown * cooldownMultiplier;
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
        return true;
    }

    bool CastFrost(Spell s)
    {
        bool any = false;
        foreach (Collider c in Physics.OverlapSphere(transform.position, novaRadius))
        {
            Enemy e = c.GetComponent<Enemy>();
            if (e == null) continue;
            e.Slow(slowFactor, slowDuration);
            e.TakeDamage(Damage(s));
            any = true;
        }
        if (any) ShowNovaFlash();
        return any; // no enemies in range = no cast, no gold spent
    }

    void ShowNovaFlash() // placeholder visual: a flat blue disc for a moment
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(disc.GetComponent<Collider>());
        disc.transform.position = new Vector3(transform.position.x, transform.position.y, 0.5f);
        disc.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localScale = new Vector3(novaRadius * 2f, 0.02f, novaRadius * 2f);
        disc.GetComponent<Renderer>().material.color = new Color(0.5f, 0.8f, 1f);
        Destroy(disc, 0.2f);
    }

    void OnGUI() // temporary spell bar until we build the pixel UI
    {
        if (slotStyle == null)
        {
            slotStyle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            slotStyle.normal.textColor = Color.white;
        }

        float w = 200f, h = 60f, gap = 10f;
        float y = Screen.height - h - 10f;
        for (int i = 0; i < spells.Count; i++)
        {
            Spell s = spells[i];
            string text = s.unlocked
                ? "[" + (i + 1) + "] " + s.name + "\n" + s.cost + " mana"
                : "[" + (i + 1) + "] Locked";
            if (!s.unlocked) GUI.color = new Color(1f, 1f, 1f, 0.4f);
            else GUI.color = (i == selected) ? Color.yellow : Color.white;
            GUI.Box(new Rect(10f + i * (w + gap), y, w, h), text, slotStyle);
        }
        GUI.color = Color.white;
    }
}