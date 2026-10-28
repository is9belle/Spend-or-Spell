using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    class Card
    {
        public string title;
        public string description;
        public int cost;
        public Sprite icon;
        public Color color;
        public System.Action apply;
        public bool sold;
    }

    [System.Serializable]
    public class SpellOffer
    {
        public SpellType spell;
        public string title = "Learn Spell";
        [TextArea] public string description = "Describe the spell";
        public int baseCost = 12;
        public Sprite icon;
        public Color color = new Color(0.15f, 0.40f, 0.78f);
    }

    public PlayerCaster caster;
    public PlayerController controller;
    public PlayerHealth health;

    [Header("Spell cards (one entry per spell you can buy)")]
    public SpellOffer[] spellOffers;

    [Header("Stat cards: icon + card color")]
    public Sprite damageIcon;
    public Color damageColor = new Color(0.36f, 0.17f, 0.60f);
    public Sprite castSpeedIcon;
    public Color castSpeedColor = new Color(0.62f, 0.40f, 0.08f);
    public Sprite bootsIcon;
    public Color bootsColor = new Color(0.48f, 0.30f, 0.15f);
    public Sprite lifeIcon;
    public Color lifeColor = new Color(0.70f, 0.14f, 0.18f);
    public Sprite novaUpgradeIcon;
    public Color novaUpgradeColor = new Color(0.10f, 0.50f, 0.72f);

    [Header("Look")]
    public Font titleFont;                 // gothic font for titles and buttons
    public Font bodyFont;                  // optional, more readable font for descriptions
    public float fontScale = 1.1f;         // gothic fonts often look small; tune live
    public float cornerRadius = 28f;       // card corner roundness (at 1080p)
    public Color buttonTextColor = new Color(1f, 0.92f, 0.6f);
    public Color startButtonColor = new Color(0.42f, 0.2f, 0.7f);
    public Vector2 startButtonSize = new Vector2(900f, 110f);   // width, height at 1080p
    public float startButtonFont = 44f;
    public float interestFont = 38f;        // size of the interest line (at 1080p)
    public float cardsTop = 190f;           // gap from the top of the screen to the cards (was 150)
    [Range(0.4f, 0.7f)] public float cardsHeight = 0.55f;   // card height as a fraction of screen height

    [Header("Shop settings")]
    public int cardsShown = 3;
    public float priceScale = 1f;
    [Range(0f, 1f)] public float interestRate = 0.1f;
    public int interestCap = 15;

    bool open;
    List<Card> cards = new List<Card>();
    GUIStyle titleStyle, bodyStyle, cardTitleStyle, buttonTextStyle, interestStyle;
    readonly Dictionary<int, GUIStyle> roundStyles = new Dictionary<int, GUIStyle>();

    public bool IsOpen { get { return open; } }

    void Start()
    {
        if (caster == null || controller == null || health == null)
            Debug.LogError("ShopManager: assign Caster, Controller, and Health (drag Player into each slot).");
    }

    int Price(int basePrice)
    {
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * priceScale));
    }

    List<Card> BuildPool()
    {
        var pool = new List<Card>();

        pool.Add(new Card { title = "Sharper Spells", description = "+20% spell damage", cost = Price(8),
            icon = damageIcon, color = damageColor,
            apply = () => caster.damageMultiplier *= 1.2f });
        pool.Add(new Card { title = "Quick Cast", description = "Cast 20% faster", cost = Price(6),
            icon = castSpeedIcon, color = castSpeedColor,
            apply = () => caster.cooldownMultiplier *= 0.8f });
        pool.Add(new Card { title = "Swift Boots", description = "Move 15% faster", cost = Price(5),
            icon = bootsIcon, color = bootsColor,
            apply = () => controller.speed *= 1.15f });

        if (!health.IsFull())
            pool.Add(new Card { title = "Restore Life", description = "Get 1 life back", cost = Price(10),
                icon = lifeIcon, color = lifeColor,
                apply = () => health.Heal() });

        if (spellOffers != null)
        {
            foreach (var o in spellOffers)
            {
                var offer = o;
                if (caster.IsUnlocked(offer.spell)) continue;
                Color col = offer.color.a < 0.01f ? new Color(0.15f, 0.40f, 0.78f) : offer.color;
                pool.Add(new Card { title = offer.title, description = offer.description,
                    cost = Price(offer.baseCost), icon = offer.icon, color = col,
                    apply = () => caster.Unlock(offer.spell) });
            }
        }

        if (caster.IsUnlocked(SpellType.Frost))
            pool.Add(new Card { title = "Wider Nova", description = "Frost Nova reaches farther", cost = Price(9),
                icon = novaUpgradeIcon, color = novaUpgradeColor,
                apply = () => caster.novaRadius += 1f });

        return pool;
    }

    public void Open()
    {
        var pool = BuildPool();
        cards.Clear();
        while (cards.Count < cardsShown && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            cards.Add(pool[i]);
            pool.RemoveAt(i);
        }
        open = true;
        Time.timeScale = 0f;
    }

    int PreviewInterest()
    {
        int gold = GoldManager.Instance.gold;
        return Mathf.Min(Mathf.FloorToInt(gold * interestRate), interestCap);
    }

    void Close(int interest)
    {
        GoldManager.Instance.Add(interest);
        open = false;
        Time.timeScale = 1f;
        WaveManager.Instance.StartNextWave();
    }

    // ---------- drawing helpers ----------

    void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;
        bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true };
        bodyStyle.normal.textColor = Color.white;
        cardTitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        cardTitleStyle.normal.textColor = Color.white;
        buttonTextStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
        buttonTextStyle.normal.textColor = buttonTextColor;
        interestStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        interestStyle.normal.textColor = new Color(1f, 0.92f, 0.6f);   // warm gold, matches the buttons
        foreach (var st in new[] { titleStyle, bodyStyle, cardTitleStyle, buttonTextStyle, interestStyle })
            GuiText.SetColor(st, st.normal.textColor);
    }

    // a white rounded-rectangle texture, used as a 9-slice so corners stay round at any size
    GUIStyle Round(float radiusF)
    {
        int radius = Mathf.Max(2, Mathf.RoundToInt(radiusF));
        GUIStyle st;
        if (roundStyles.TryGetValue(radius, out st)) return st;

        int size = radius * 2 + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
            float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
        }
        tex.Apply();

        st = new GUIStyle();
        st.normal.background = tex;
        st.border = new RectOffset(radius + 1, radius + 1, radius + 1, radius + 1);
        roundStyles[radius] = st;
        return st;
    }

    void RFill(Rect r, Color c, float radius)
    {
        GUI.color = c;
        GUI.Box(r, GUIContent.none, Round(radius));
        GUI.color = Color.white;
    }

    void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Shadowed(Rect r, string text, GUIStyle st, float offset)
    {
        Color old = st.normal.textColor;
        GuiText.SetColor(st, new Color(0f, 0f, 0f, 0.55f));
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, st);
        GuiText.SetColor(st, old);
        GUI.Label(r, text, st);
    }

    void DrawIcon(Sprite s, Rect box)
    {
        if (s == null) return;
        Texture2D t = s.texture;
        Rect tr = s.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);

        float aspect = tr.width / tr.height;
        float w = box.width, h = box.height;
        if (aspect > 1f) h = w / aspect; else w = h * aspect;
        Rect fit = new Rect(box.x + (box.width - w) / 2f, box.y + (box.height - h) / 2f, w, h);

        GUI.color = Color.white;
        GUI.DrawTextureWithTexCoords(fit, t, uv);
    }

    void Sparkle(Vector2 center, float size, Color c)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, center);
        Fill(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), c);
        GUI.matrix = old;
    }

    // glowing, pulsing "spell" button. Returns true when clicked.
    bool MagicButton(Rect r, string label, Color baseColor, bool enabled, float s)
    {
        bool hover = enabled && r.Contains(Event.current.mousePosition);
        float t = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f);
        float rad = r.height * 0.35f;

        Color edge, body, text;
        if (enabled)
        {
            // soft aura around the button
            float glow = (hover ? 0.55f : 0.25f) + 0.2f * pulse;
            float g = (hover ? 10f : 6f) * s;
            RFill(new Rect(r.x - g, r.y - g, r.width + g * 2f, r.height + g * 2f),
                  new Color(baseColor.r + 0.3f, baseColor.g + 0.3f, baseColor.b + 0.3f, glow * 0.6f), rad + g);

            edge = Color.Lerp(baseColor, Color.white, hover ? 0.85f : 0.6f);
            body = Color.Lerp(baseColor, Color.black, hover ? 0.3f : 0.5f);
            text = buttonTextColor;
        }
        else
        {
            edge = new Color(0.3f, 0.3f, 0.35f);
            body = new Color(0.14f, 0.14f, 0.17f);
            text = new Color(0.5f, 0.5f, 0.55f);
        }

        float b = 3f * s;
        RFill(r, edge, rad);
        RFill(new Rect(r.x + b, r.y + b, r.width - b * 2f, r.height - b * 2f), body, rad - b);
        // lighter sheen along the top half
        RFill(new Rect(r.x + b * 2f, r.y + b * 1.5f, r.width - b * 4f, r.height * 0.38f),
              new Color(1f, 1f, 1f, enabled ? 0.12f : 0.04f), rad * 0.7f);

        if (enabled)
        {
            float sz = (7f + 4f * pulse) * s;
            Color sp = new Color(1f, 0.95f, 0.7f, 0.6f + 0.4f * pulse);
            Sparkle(new Vector2(r.x + r.height * 0.35f, r.center.y), sz, sp);
            Sparkle(new Vector2(r.xMax - r.height * 0.35f, r.center.y), sz, sp);
        }

        GuiText.SetColor(buttonTextStyle, text);
        int savedSize = buttonTextStyle.fontSize;
        float textW = buttonTextStyle.CalcSize(new GUIContent(label)).x;
        float maxW = r.width - r.height;          // leaves room for the sparkles at each end
        if (textW > maxW)
            buttonTextStyle.fontSize = Mathf.Max(8, Mathf.FloorToInt(savedSize * maxW / textW));
        Shadowed(r, label, buttonTextStyle, 2f * s);
        buttonTextStyle.fontSize = savedSize;

        return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    // ---------- the shop screen ----------

    void OnGUI()
    {
        if (!open) return;
        EnsureStyles();

        float w = Screen.width, h = Screen.height;
        float s = h / 1080f;
        float f = s * fontScale;

        titleStyle.font = titleFont;
        cardTitleStyle.font = titleFont;
        buttonTextStyle.font = titleFont;
        bodyStyle.font = bodyFont != null ? bodyFont : titleFont;
        interestStyle.font = bodyStyle.font;

        titleStyle.fontSize = Mathf.RoundToInt(64f * f);
        cardTitleStyle.fontSize = Mathf.RoundToInt(42f * f);
        bodyStyle.fontSize = Mathf.RoundToInt(28f * f);
        buttonTextStyle.fontSize = Mathf.RoundToInt(32f * f);
        interestStyle.fontSize = Mathf.RoundToInt(interestFont * f);

        GoldManager gm = GoldManager.Instance;
        int nextWave = WaveManager.Instance.Wave + 1;

        Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.82f));

        GUI.Label(new Rect(0, 20f * s, w, 80f * s), "Spellbook", titleStyle);
        GUI.Label(new Rect(0, 100f * s, w, 44f * s), "Current mana: " + gm.gold, bodyStyle);

        // ---- cards ----
        int n = Mathf.Max(1, cards.Count);
        float gap = 40f * s;
        float cardW = Mathf.Min((w * 0.86f - gap * (n - 1)) / n, 560f * s);
        float cardH = h * cardsHeight;
        float startX = (w - (n * cardW + (n - 1) * gap)) / 2f;
        float top = cardsTop * s;

        float rad = cornerRadius * s;
        float border = 6f * s;
        float inset = 14f * s;
        float btnH = 66f * s;
        float titleH = 56f * s;
        float descW = cardW - 40f * s;

        float descH = 0f;
        foreach (var cd in cards)
            descH = Mathf.Max(descH, bodyStyle.CalcHeight(new GUIContent(cd.description), descW));

        float fixedH = inset + 10f * s + titleH + 4f * s + descH + 16f * s + btnH + 24f * s;
        float panelH = Mathf.Max(100f * s, cardH - fixedH);

        for (int i = 0; i < cards.Count; i++)
        {
            Card c = cards[i];
            Rect r = new Rect(startX + i * (cardW + gap), top, cardW, cardH);

            RFill(new Rect(r.x - border, r.y - border, r.width + border * 2f, r.height + border * 2f),
                  Color.Lerp(c.color, Color.black, 0.6f), rad + border);
            RFill(r, c.color, rad);

            Rect panel = new Rect(r.x + inset, r.y + inset, cardW - inset * 2f, panelH);
            RFill(panel, Color.Lerp(c.color, Color.white, 0.16f), rad * 0.7f);

            float iconSize = Mathf.Min(panel.width * 0.9f, panel.height - 16f * s);
            DrawIcon(c.icon, new Rect(panel.center.x - iconSize / 2f, panel.center.y - iconSize / 2f, iconSize, iconSize));

            float ty = panel.yMax + 10f * s;
            Shadowed(new Rect(r.x + 12f * s, ty, cardW - 24f * s, titleH), c.title, cardTitleStyle, 2f * s);

            float dy = ty + titleH + 4f * s;
            Shadowed(new Rect(r.x + 20f * s, dy, descW, descH), c.description, bodyStyle, 1.5f * s);

            float btnY = dy + descH + 16f * s;
            Rect btn = new Rect(r.x + 28f * s, btnY, cardW - 56f * s, btnH);

            if (c.sold)
            {
                RFill(r, new Color(0f, 0f, 0f, 0.6f), rad);
                Shadowed(new Rect(r.x, r.center.y - 40f * s, cardW, 80f * s), "SOLD", cardTitleStyle, 2f * s);
            }
            else if (MagicButton(btn, "Buy for " + c.cost + " mana", c.color, gm.gold >= c.cost, s))
            {
                if (gm.TrySpend(c.cost))
                {
                    c.apply();
                    c.sold = true;
                }
            }
        }

        // ---- interest + start ----
        int interest = PreviewInterest();
        float y = top + cardH + 26f * s;

        Shadowed(new Rect(0, y, w, 56f * s),
            "Interest: +" + interest + " mana  (" + Mathf.RoundToInt(interestRate * 100f) + "%, max " + interestCap + ")",
            interestStyle, 2f * s);

        string label = interest > 0
            ? "Collect +" + interest + " interest and start wave " + nextWave
            : "Start wave " + nextWave;

        Rect startRect = new Rect(w / 2f - startButtonSize.x * s / 2f, y + 70f * s,
                                  startButtonSize.x * s, startButtonSize.y * s);
        int oldSize = buttonTextStyle.fontSize;
        buttonTextStyle.fontSize = Mathf.RoundToInt(startButtonFont * f);
        bool startClicked = MagicButton(startRect, label, startButtonColor, true, s);
        buttonTextStyle.fontSize = oldSize;
        if (startClicked) Close(interest);
    }
}