using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyIntro
{
    public string enemyName;                  // the enemy prefab's name, e.g. Bat
    [TextArea(2, 4)] public string[] lines;   // one entry per message box
    public bool pause;                        // true: the game pauses until Space is pressed
}

[RequireComponent(typeof(AudioSource))]
public class MentorDialogue : MonoBehaviour
{
    public static MentorDialogue Instance;

    [Header("Who is talking")]
    public string speakerName = "Mentor";
    public Sprite portrait;                  // optional: the mentor's portrait

    [Header("Look")]
    public Font font;                        // your gothic font
    public float fontScale = 1.1f;
    public float textSize = 28f;   // base size of the dialogue text (was 34)
    public float bottomOffset = 190f;        // room left for the spell bar (1080p pixels)
    public float widthFraction = 0.62f;      // box width as a share of the screen

    [Header("Behaviour")]
    public float charsPerSecond = 45f;
    public float defaultHold = 3.5f;         // seconds a tip stays after it finishes typing
    public AudioClip blip;                   // optional "talking" sound
    [Range(0f, 1f)] public float blipVolume = 0.5f;

    [Header("Boss")]
    public string[] bossIntroLines =
    {
        "That is the Slime King. One of these comes for you every ten waves, and each one is tougher than the last.",
        "When it crouches, a warning circle appears. That is where it will land. Get out of the circle before it comes down.",
        "Right after a slam, it stands still. That is your chance to strike.",
        "When it is weakened, it splits apart and calls minions. Do not panic. Keep moving.",
        "For this fight, your mana is unlimited. Make good use of it. Countdowns still apply, so aim carefully."
    };
    public string[] bossReturnLines =
    {
        "The Slime King again, and tougher than before. Same rules as last time."
    };

    static bool bossSeen;

    public void PlayBossIntro()
    {
        if (!bossSeen)
        {
            bossSeen = true;
            TalkNow(bossIntroLines);       // full explanation the first time
        }
        else if (bossReturnLines != null && bossReturnLines.Length > 0)
        {
            Say(bossReturnLines[Random.Range(0, bossReturnLines.Length)], -1f, true);   // short reminder after that
        }
    }

    // Starts a conversation right away, dropping any tips that are showing or waiting.
    public void TalkNow(params string[] lines)
    {
        if (cur != null && !cur.block) cur = null;
        var keep = new List<Line>();
        foreach (Line l in queue) if (l.block) keep.Add(l);
        queue.Clear();
        foreach (Line l in keep) queue.Enqueue(l);
        foreach (string l in lines) queue.Enqueue(new Line { text = l, block = true });
    }
    [Header("Lines (edit these in the Inspector)")]
    public string[] clearedTips =
    {
        "Not bad. Spend your mana wisely, or save it. Your choice.",
        "Mana you hold earns interest. Patience is a spell too.",
        "Remember: the dungeon keeps its mana. You only keep what you learn."
    };
    public string[] gameOverLines =
    {
        "That's enough. I'm pulling you out.",
        "Back to the start with you. Try not to look so surprised."
    };
    public string[] introLines =
    {
        "Ah, you're awake. Good.",
        "This dungeon is enchanted. Everything you collect in here is mana, and it fades the moment you leave.",
        "Move with WASD, aim with the mouse, and click to cast. Every spell costs mana.",
        "Walk over the crystals the creatures drop to collect them. And never spend all of it. Mana you hold earns interest.",
        "Try not to die. It reflects badly on me."
    };
    [Header("Enemy introductions (shown the first time each type appears)")]
    public EnemyIntro[] enemyIntros =
    {
        new EnemyIntro { enemyName = "Slime", lines = new[] { "Slimes. Their hops are faster than you'd think. Keep moving." } },
        new EnemyIntro { enemyName = "Bat", lines = new[] { "Bats. Fast and erratic. Let them come to you." } },
        new EnemyIntro { enemyName = "Archer", lines = new[] { "An archer. It shoots from a distance, so close in fast or keep moving." } },
        new EnemyIntro { enemyName = "Golem", lines = new[] { "A golem. Slow, but it takes a great many hits. Keep your distance." } }
    };

    static readonly HashSet<string> seenEnemies = new HashSet<string>();
    float nextScan;

    public void PlayIntro() { Intro(introLines); }
    static string Normalize(string s)
    {
        return s.Replace("(Clone)", "").Replace(" ", "").ToLowerInvariant();
    }

    void ScanForNewEnemies()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.4f;
        if (enemyIntros == null || enemyIntros.Length == 0) return;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            string key = Normalize(e.gameObject.name);
            if (!seenEnemies.Add(key)) continue;      // already introduced

            foreach (EnemyIntro intro in enemyIntros)
            {
                if (Normalize(intro.enemyName) != key) continue;
                PlayEnemyIntro(intro);
                break;
            }
        }
    }

    void PlayEnemyIntro(EnemyIntro intro)
    {
        if (intro.lines == null || intro.lines.Length == 0) return;
        if (intro.pause) Talk(intro.lines);
        else foreach (string l in intro.lines) Say(l, -1f, true);
    }

    class Line { public string text; public bool block; public float hold; }

    static bool introSeen;                   // so Retry doesn't replay the intro
    readonly Queue<Line> queue = new Queue<Line>();
    Line cur;
    float typed, shownAt, doneAt;
    bool pausedByMe;
    AudioSource src;
    GUIStyle nameStyle, textStyle, hintStyle;

    void Awake()
    {
        Instance = this;
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (pausedByMe) Time.timeScale = 1f;
    }

    // ---------- what the rest of the game calls ----------

    // A short tip: the game keeps running and the box fades out by itself.
    public void Say(string text, float hold = -1f, bool force = false)
    {
        if (!force && queue.Count >= 3) return;        // don't let tips pile up
        queue.Enqueue(new Line { text = text, block = false, hold = hold < 0f ? defaultHold : hold });
    }

    // A conversation: the game pauses and the player presses Space to continue.
    public void Talk(params string[] lines)
    {
        foreach (string l in lines)
            queue.Enqueue(new Line { text = l, block = true });
    }

    // Same as Talk, but only the first time per play session.
    public void Intro(params string[] lines)
    {
        if (introSeen) return;
        introSeen = true;
        Talk(lines);
    }

    public void SayRandom(string[] options)
    {
        if (options != null && options.Length > 0)
            Say(options[Random.Range(0, options.Length)]);
    }

    public void WaveCleared() { SayRandom(clearedTips); }

    public void GameOver()
    {
        if (gameOverLines != null && gameOverLines.Length > 0)
            Say(gameOverLines[Random.Range(0, gameOverLines.Length)], 600f);
    }

    // ---------- logic ----------

    bool AdvancePressed()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
    }

    void Update()
    {
        ScanForNewEnemies();

        // a conversation keeps the game paused, even if another script tries to unpause it
        if (pausedByMe && cur != null && cur.block) Time.timeScale = 0f;

        if (cur == null && queue.Count > 0)
        {
            // if another screen (title, shop, game over) has the game paused, wait for it to finish first
            bool otherPause = Time.timeScale == 0f && !pausedByMe;
            if (!(queue.Peek().block && otherPause)) Begin(queue.Dequeue());
        }
        if (cur == null) return;

        if (typed < cur.text.Length)
        {
            float before = typed;
            typed = Mathf.Min(cur.text.Length, typed + charsPerSecond * Time.unscaledDeltaTime);
            if (blip != null && Mathf.FloorToInt(typed / 2f) != Mathf.FloorToInt(before / 2f))
                src.PlayOneShot(blip, blipVolume);
            if (cur.block && AdvancePressed()) typed = cur.text.Length;   // Space skips the typing
            if (typed >= cur.text.Length) doneAt = Time.unscaledTime;
        }
        else if (cur.block)
        {
            if (AdvancePressed()) End();
        }
        else if (Time.unscaledTime - doneAt >= cur.hold)
        {
            End();
        }
    }

    void Begin(Line l)
    {
        cur = l;
        typed = 0f;
        shownAt = Time.unscaledTime;
        doneAt = float.MaxValue;
        if (l.block && Time.timeScale > 0f)   // don't fight the shop or game over screens
        {
            Time.timeScale = 0f;
            pausedByMe = true;
        }
    }

    void End()
    {
        cur = null;
        bool moreTalk = queue.Count > 0 && queue.Peek().block;
        if (pausedByMe && !moreTalk)
        {
            Time.timeScale = 1f;
            pausedByMe = false;
        }
    }

    // ---------- drawing ----------

    void EnsureStyles()
    {
        if (textStyle != null) return;
        nameStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true };
        hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, wordWrap = false };
    }

    void DrawSprite(Sprite sp, Rect box, float alpha)
    {
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
        if (cur == null) return;
        EnsureStyles();
        GUI.depth = 3;   // above the spell bar (5) and boss bar (4)

        float sc = Screen.height / 1080f;
        float f = sc * fontScale;
        nameStyle.font = font; textStyle.font = font; hintStyle.font = font;
        nameStyle.fontSize = Mathf.RoundToInt(30f * f);
        hintStyle.fontSize = Mathf.RoundToInt(24f * f);

        // fade in, and fade out for tips
        float a = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.2f);
        if (!cur.block && typed >= cur.text.Length)
            a *= Mathf.Clamp01((cur.hold - (Time.unscaledTime - doneAt)) / 0.35f);

        float w = Mathf.Min(Screen.width * widthFraction, 1300f * sc);
        float h = 190f * sc;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - h - bottomOffset * sc + (1f - a) * 24f * sc;
        float rad = 22f * sc, b = 5f * sc, pad = 18f * sc;

        Color cream = new Color(1f, 0.92f, 0.6f);
        Color gold = new Color(1f, 0.85f, 0.4f);

        // frame + body
        MagicGui.RFill(new Rect(x - b, y - b, w + b * 2f, h + b * 2f), new Color(gold.r, gold.g, gold.b, a), rad + b);
        MagicGui.RFill(new Rect(x, y, w, h), new Color(0.08f, 0.06f, 0.12f, 0.95f * a), rad);

        // portrait
        float textX = x + pad;
        if (portrait != null)
        {
            float ps = h - pad * 2f;
            Rect pr = new Rect(x + pad, y + pad, ps, ps);
            MagicGui.RFill(pr, new Color(0.18f, 0.14f, 0.26f, a), rad * 0.6f);
            DrawSprite(portrait, new Rect(pr.x + 6f * sc, pr.y + 6f * sc, pr.width - 12f * sc, pr.height - 12f * sc), a);
            textX = pr.xMax + pad;
        }

        // name tab sitting on the top edge of the box
        Rect tab = new Rect(textX, y - 26f * sc, 240f * sc, 46f * sc);
        MagicGui.RFill(new Rect(tab.x - 3f * sc, tab.y - 3f * sc, tab.width + 6f * sc, tab.height + 6f * sc), new Color(gold.r, gold.g, gold.b, a), 14f * sc);
        MagicGui.RFill(tab, new Color(0.08f, 0.06f, 0.12f, 0.98f * a), 12f * sc);
        MagicGui.Shadowed(tab, speakerName, nameStyle, cream, 1.5f * sc, a);

        // text, typed out. The not-yet-typed part is transparent so the words don't jump around.
        Rect textRect = new Rect(textX, y + 28f * sc, x + w - pad - textX, h - 70f * sc);

        // shrink the text until the whole line fits inside the box
        int size = Mathf.RoundToInt(textSize * f);
        textStyle.fontSize = size;
        GUIContent measure = new GUIContent(cur.text);
        while (size > 14 && textStyle.CalcHeight(measure, textRect.width) > textRect.height)
        {
            size -= 2;
            textStyle.fontSize = size;
        }

        int n = Mathf.Clamp(Mathf.FloorToInt(typed), 0, cur.text.Length);
        string shown = cur.text.Substring(0, n) + "<color=#00000000>" + cur.text.Substring(n) + "</color>";
        MagicGui.Shadowed(textRect, shown, textStyle, cream, 2f * sc, a);

        // "press Space" hint, only for conversations and only once the line is finished
        if (cur.block && typed >= cur.text.Length)
        {
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            MagicGui.Shadowed(new Rect(x + w - 260f * sc - pad, y + h - 46f * sc, 260f * sc, 36f * sc),
                              "Space  >", hintStyle, gold, 1.5f * sc, a * blink);
        }
        GUI.color = Color.white;
    }
}