using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Music and ambience (both loop, both use the Music slider)")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicBase = 0.5f;
    public AudioClip ambience;
    [Range(0f, 1f)] public float ambienceBase = 0.6f;

    [Header("Player")]
    public AudioClip[] playerSteps;
    public AudioClip[] fireCast;
    public AudioClip[] frostCast;
    public AudioClip[] playerHurt;
    public AudioClip[] playerDeath;

    [Header("World")]
    public AudioClip[] barrelHit;
    public AudioClip[] barrelBreak;
    public AudioClip[] pickup;                                 // gems, coins, etc.
    public AudioClip[] drips;                                  // optional random water drips near the player
    public Vector2 dripDelay = new Vector2(4f, 10f);
    [Range(0f, 1f)] public float dripVolume = 0.2f;

    [Header("UI")]
    public AudioClip[] hover;
    public AudioClip[] click;

    [Header("Distance falloff")]
    public float fullVolumeDistance = 3f;                      // closer than this = full volume
    public float maxHearDistance = 16f;                        // farther than this = silent
    public int voices = 24;                                    // how many sounds can overlap

    [Header("Sound menu style")]
    public Font font;                                          // optional, only used for the slider labels
    public float fontScale = 1.1f;
    public Sprite soundSprite, muteSprite, unmuteSprite, doneSprite;   // buttons with the words already drawn in
    public float buttonWidth = 210f;                           // on-screen width at 1080p
    public Color mintColor = new Color(0.65f, 0.97f, 0.85f);   // title, slider labels, slider fill
    public Color woodColor = new Color(0.45f, 0.28f, 0.15f);   // panel frame
    public Color panelFill = new Color(0.1f, 0.1f, 0.16f);     // dark stone panel
    public Color knobColor = new Color(0.85f, 0.7f, 0.5f);

    // saved settings
    float master = 1f, musicVol = 0.8f, sfx = 1f;
    bool muted;
    bool panelOpen;

    AudioSource[] pool;
    int cursor;
    AudioSource musicSrc, ambSrc;
    Transform player;
    float nextDrip;
    Rect muteRect, soundRect, panelRect;
    GUIStyle labelStyle;
    int sliderCount, draggingSlider;

    public static float SfxVolume { get { return I != null ? I.sfx : 1f; } }
    public static bool PointerOverUI { get; private set; }     // true while the mouse is over the sound buttons or panel

    // ---------------- setup ----------------

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }   // safe to put one in every scene
        I = this;
        DontDestroyOnLoad(gameObject);

        panelOpen = false;
        master = PlayerPrefs.GetFloat("vol_master", 1f);
        musicVol = PlayerPrefs.GetFloat("vol_music", 0.8f);
        sfx = PlayerPrefs.GetFloat("vol_sfx", 1f);
        muted = PlayerPrefs.GetInt("vol_muted", 0) == 1;

        pool = new AudioSource[Mathf.Max(4, voices)];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f;
        }

        musicSrc = MakeLoop(music);
        ambSrc = MakeLoop(ambience);
        ApplyVolumes();
        nextDrip = Time.time + Random.Range(dripDelay.x, dripDelay.y);
    }

    AudioSource MakeLoop(AudioClip clip)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        if (clip != null) s.Play();
        return s;
    }

    void ApplyVolumes()
    {
        AudioListener.volume = muted ? 0f : master;
        musicSrc.volume = musicBase * musicVol;
        ambSrc.volume = ambienceBase * musicVol;
    }

    void SaveValues()
    {
        PlayerPrefs.SetFloat("vol_master", master);
        PlayerPrefs.SetFloat("vol_music", musicVol);
        PlayerPrefs.SetFloat("vol_sfx", sfx);
        PlayerPrefs.SetInt("vol_muted", muted ? 1 : 0);
    }

    void SaveToDisk()
    {
        SaveValues();
        PlayerPrefs.Save();
    }

    void OnApplicationQuit() { SaveToDisk(); }

    // ---------------- core playing ----------------

    Transform GetPlayer()
    {
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }
        return player;
    }

    // 1 = full volume, 0 = out of earshot. Use maxDist to override the default range.
    public static float DistanceVolume(Vector3 pos, float maxDist = -1f)
    {
        if (I == null) return 1f;
        Transform p = I.GetPlayer();
        if (p == null) return 1f;
        float max = maxDist > 0f ? maxDist : I.maxHearDistance;
        float d = Vector2.Distance(pos, p.position);
        float t = Mathf.InverseLerp(max, I.fullVolumeDistance, d);
        return t * t;
    }

    // -1 (left) to 1 (right), based on where the sound is compared to the player
    public static float Pan(Vector3 pos)
    {
        if (I == null) return 0f;
        Transform p = I.GetPlayer();
        if (p == null) return 0f;
        return Mathf.Clamp((pos.x - p.position.x) / I.maxHearDistance, -1f, 1f) * 0.7f;
    }

    static AudioClip Pick(AudioClip[] c)
    {
        if (c == null || c.Length == 0) return null;
        return c[Random.Range(0, c.Length)];
    }

    // Positional sound: gets quieter with distance from the player and pans left/right.
    public static void Play(AudioClip[] clips, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        if (I == null) return;
        I.PlayAt(Pick(clips), pos, vol, pitch, false);
    }

    // Flat sound (UI): same volume everywhere.
    public static void PlayFlat(AudioClip[] clips, float vol = 1f)
    {
        if (I == null) return;
        I.PlayAt(Pick(clips), Vector3.zero, vol, 1f, true);
    }

    void PlayAt(AudioClip clip, Vector3 pos, float vol, float pitch, bool flat)
    {
        if (clip == null) return;
        float v = vol * sfx * (flat ? 1f : DistanceVolume(pos));
        if (v <= 0.01f) return;

        AudioSource s = NextVoice();
        s.clip = clip;
        s.volume = Mathf.Clamp01(v);
        s.pitch = pitch + (flat ? 0f : Random.Range(-0.08f, 0.08f));
        s.panStereo = flat ? 0f : Pan(pos);
        s.Play();
    }

    AudioSource NextVoice()
    {
        for (int n = 0; n < pool.Length; n++)
        {
            int i = (cursor + n) % pool.Length;
            if (!pool[i].isPlaying) { cursor = (i + 1) % pool.Length; return pool[i]; }
        }
        AudioSource s = pool[cursor];                 // all busy: steal the oldest
        cursor = (cursor + 1) % pool.Length;
        return s;
    }

    // ---------------- one-liners for your scripts ----------------

    public static void PlayerStep(Vector3 p)  { if (I != null) Play(I.playerSteps, p, 0.6f); }
    public static void FireCast(Vector3 p)    { if (I != null) Play(I.fireCast, p, 0.8f); }
    public static void FrostCast(Vector3 p)   { if (I != null) Play(I.frostCast, p, 1f); }
    public static void PlayerHurt(Vector3 p)  { if (I != null) Play(I.playerHurt, p, 1f); }
    public static void PlayerDeath(Vector3 p) { if (I != null) Play(I.playerDeath, p, 1f); }
    public static void BarrelHit(Vector3 p)   { if (I != null) Play(I.barrelHit, p, 0.8f); }
    public static void BarrelBreak(Vector3 p) { if (I != null) Play(I.barrelBreak, p, 1f); }
    public static void Pickup(Vector3 p)      { if (I != null) Play(I.pickup, p, 0.8f); }
    public static void Hover()                { if (I != null) PlayFlat(I.hover, 0.6f); }
    public static void Click()                { if (I != null) PlayFlat(I.click, 0.9f); }

    // ---------------- update, drips, mouse blocking ----------------

    void Update()
    {
        if (I != this) return;

        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            panelOpen = false;
            SaveToDisk();
        }

        ApplyVolumes();

        Layout();
        Vector2 mp = Input.mousePosition;
        mp.y = Screen.height - mp.y;                   // IMGUI rects count from the top
        PointerOverUI = muteRect.Contains(mp) || soundRect.Contains(mp) || (panelOpen && panelRect.Contains(mp));

        if (Time.time >= nextDrip)
        {
            nextDrip = Time.time + Random.Range(dripDelay.x, dripDelay.y);
            Transform p = GetPlayer();
            if (p != null && drips != null && drips.Length > 0)
            {
                Vector2 o = Random.insideUnitCircle * 6f;
                Play(drips, p.position + new Vector3(o.x, o.y, 0f), dripVolume);
            }
        }
    }

    // ---------------- mute button and sound panel ----------------

    float BtnH(Sprite sp, float w, float s)
    {
        return sp != null ? w * sp.rect.height / sp.rect.width : 70f * s;
    }

    void Layout()
    {
        float s = Screen.height / 1080f;
        float bw = buttonWidth * s, m = 16f * s;
        float bh = BtnH(soundSprite, bw, s);
        muteRect = new Rect(Screen.width - m - bw, m, bw, bh);
        soundRect = new Rect(muteRect.x - bw - 10f * s, m, bw, bh);
        panelRect = new Rect(Screen.width - m - 500f * s, m + bh + 12f * s, 500f * s, 440f * s);
    }

    void DrawSprite(Sprite sp, Rect r)
    {
        Texture2D t = sp.texture;
        Rect tr = sp.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        GUI.DrawTextureWithTexCoords(r, t, uv);
    }

    bool WoodButton(Rect r, Sprite sp, string fallbackText, float s)
    {
        Event e = Event.current;
        bool clicked = false;
        if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
        {
            clicked = true;
            e.Use();
            Click();
        }

        if (e.type == EventType.Repaint)
        {
            bool hover = r.Contains(e.mousePosition);
            float d = hover ? 1f : 0.85f;                      // dims a little until you hover
            GUI.color = new Color(d, d, d, 1f);
            if (sp != null) DrawSprite(sp, r);
            else
            {
                // fallback if a sprite slot is empty: plain wood with text
                MagicGui.RFill(new Rect(r.x - 3f * s, r.y - 3f * s, r.width + 6f * s, r.height + 6f * s),
                               Color.Lerp(woodColor, Color.black, 0.6f), 10f * s);
                MagicGui.RFill(r, woodColor, 8f * s);
                GUI.color = Color.white;
                MagicGui.Shadowed(r, fallbackText, labelStyle, mintColor, 2f * s);
            }
            GUI.color = Color.white;
        }
        return clicked;
    }

    float Slider(string label, float v, float x, ref float y, float w, float s, GUIStyle style)
    {
        MagicGui.Shadowed(new Rect(x, y, w, 36f * s), label + "  " + Mathf.RoundToInt(v * 100f) + "%",
                          style, mintColor, 1.5f * s);
        y += 44f * s;

        Rect track = new Rect(x, y + 8f * s, w, 14f * s);
        Rect hit = new Rect(track.x - 14f * s, track.y - 16f * s, track.width + 28f * s, track.height + 32f * s);
        int id = ++sliderCount;
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition)) draggingSlider = id;

        if (draggingSlider == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            v = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
            e.Use();
        }
        if (e.type == EventType.MouseUp && draggingSlider == id)
        {
            draggingSlider = 0;
            e.Use();
        }

        float r = track.height / 2f;
        MagicGui.RFill(track, new Color(0f, 0f, 0f, 0.5f), r);
        MagicGui.RFill(new Rect(track.x, track.y, Mathf.Max(track.height, track.width * v), track.height), mintColor, r);
        float kn = 30f * s;
        MagicGui.RFill(new Rect(track.x + track.width * v - kn / 2f, track.y + r - kn / 2f, kn, kn), knobColor, kn / 2f);

        y += 50f * s;
        return v;
    }

    void OnGUI()
    {
        if (I != this) return;
        GUI.depth = -200;                              // on top of the start screen and game over screen
        Layout();
        sliderCount = 0;

        float s = Screen.height / 1080f;
        float f = s * fontScale;

        if (labelStyle == null)
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        labelStyle.font = font;
        labelStyle.fontSize = Mathf.RoundToInt(30f * f);

        // mute button
        if (WoodButton(muteRect, muted ? unmuteSprite : muteSprite, muted ? "Unmute" : "Mute", s))
        {
            muted = !muted;
            SaveToDisk();
        }

        // sound button
        if (WoodButton(soundRect, soundSprite, "Sound", s))
        {
            panelOpen = !panelOpen;
            if (!panelOpen) SaveToDisk();
        }

        if (!panelOpen) { draggingSlider = 0; return; }

        // panel
        Rect p = panelRect;
        float rad = 18f * s, b = 6f * s;
        MagicGui.RFill(new Rect(p.x - b, p.y - b, p.width + b * 2f, p.height + b * 2f),
                       Color.Lerp(woodColor, Color.black, 0.35f), rad + b);
        MagicGui.RFill(p, panelFill, rad);

        float x = p.x + 28f * s, w = p.width - 56f * s, y = p.y + 16f * s;
        MagicGui.Shadowed(new Rect(x, y, w, 56f * s), "Sound", labelStyle, mintColor, 2f * s);
        y += 64f * s;

        float m0 = master, m1 = musicVol, m2 = sfx;
        master = Slider("Master", master, x, ref y, w, s, labelStyle);
        musicVol = Slider("Music", musicVol, x, ref y, w, s, labelStyle);
        sfx = Slider("Effects", sfx, x, ref y, w, s, labelStyle);
        if (m0 != master || m1 != musicVol || m2 != sfx) SaveValues();

        float dw = buttonWidth * s;
        float dh = BtnH(doneSprite, dw, s);
        if (WoodButton(new Rect(p.center.x - dw / 2f, y + 6f * s, dw, dh), doneSprite, "Done", s))
        {
            panelOpen = false;
            SaveToDisk();
        }
    }
}