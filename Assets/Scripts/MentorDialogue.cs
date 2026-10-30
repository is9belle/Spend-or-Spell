using UnityEngine;
using System.Collections.Generic;

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
    public float bottomOffset = 190f;        // room left for the spell bar (1080p pixels)
    public float widthFraction = 0.62f;      // box width as a share of the screen

    [Header("Behaviour")]
    public float charsPerSecond = 45f;
    public float defaultHold = 3.5f;         // seconds a tip stays after it finishes typing
    public AudioClip blip;                   // optional "talking" sound
    [Range(0f, 1f)] public float blipVolume = 0.5f;

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
    public void Say(string text, float hold = -1f)
    {
        if (queue.Count >= 3) return;        // don't let tips pile up
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
        if (cur == null && queue.Count > 0) Begin(queue.Dequeue());
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
        textStyle.fontSize = Mathf.RoundToInt(34f * f);
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
        int n = Mathf.Clamp(Mathf.FloorToInt(typed), 0, cur.text.Length);
        string shown = cur.text.Substring(0, n) + "<color=#00000000>" + cur.text.Substring(n) + "</color>";
        Rect textRect = new Rect(textX, y + 28f * sc, x + w - pad - textX, h - 70f * sc);
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