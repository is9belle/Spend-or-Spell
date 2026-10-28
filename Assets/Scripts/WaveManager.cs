using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public GameObject enemyPrefab;
    public GameObject coinPrefab;
    public Transform player;
    public ShopManager shop;

    public int baseEnemies = 3;           // enemies in wave 1
    public int enemiesPerWave = 2;        // extra enemies each wave
    public float spawnInterval = 1.5f;    // seconds between spawns in wave 1
    public float intervalShrink = 0.05f;  // spawns get this much faster each wave
    public float minSpawnInterval = 0.5f; // never faster than this
    public float speedGrowth = 0.05f;     // +5% enemy speed per wave
    public float healthGrowth = 0.15f;    // +15% enemy health per wave
    public int startingCoins = 5;         // coins scattered at the start of each wave
    public float shopDelay = 3f;          // seconds you can keep moving after the last kill
    public Vector2 arenaHalfSize = new Vector2(19f, 11f);

    [Header("HUD text (top left)")]
    public Font font;                      // your gothic font
    public Font bodyFont;                  // optional, easier to read for small text
    public float fontScale = 1.1f;         // gothic fonts look small; tune live
    public int hudFontSize = 32;
    public float hudY = 135f;              // top of the "Wave" line (was 110)
    public float hudLineSpacing = 42f;
    public Color hudColor = new Color(1f, 0.92f, 0.6f);

    [Header("Wave cleared banner")]
    public string clearedText = "Wave {0} cleared!";   // {0} becomes the wave number
    public string countdownLabel = "The spellbook opens in";   // leave empty to hide
    public int bannerFontSize = 84;
    public int numberFontSize = 150;
    public int labelFontSize = 34;
    public Color bannerColor = new Color(1f, 0.85f, 0.35f);
    public Color numberColor = new Color(1f, 0.95f, 0.75f);
    public float bannerY = 0.26f;          // banner height as a fraction of screen height
    public float numberPulse = 0.35f;      // how much each number pops when it changes

    int wave;
    int remaining; // enemies left to kill, including ones not spawned yet
    float shopOpenTime;
    float clearStart;
    bool clearing;
    GUIStyle hudStyle, bannerStyle, numberStyle, labelStyle;

    public int Wave { get { return wave; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (shop == null)
            Debug.LogError("WaveManager: the Shop slot is empty. Add the ShopManager script to GameManager, then drag GameManager into this slot.");
        StartNextWave();
    }

    public void StartNextWave()
    {
        wave++;

        for (int i = 0; i < startingCoins; i++)
            Instantiate(coinPrefab, RandomPoint(), Quaternion.identity);

        int total = baseEnemies + (wave - 1) * enemiesPerWave;
        remaining = total;
        StartCoroutine(SpawnRoutine(total));
    }

    IEnumerator SpawnRoutine(int total)
    {
        float interval = Mathf.Max(minSpawnInterval, spawnInterval - intervalShrink * (wave - 1));
        for (int i = 0; i < total; i++)
        {
            Enemy e = Instantiate(enemyPrefab, SpawnPoint(), Quaternion.identity).GetComponent<Enemy>();
            e.speed *= 1f + speedGrowth * (wave - 1);
            e.maxHealth = Mathf.RoundToInt(e.maxHealth * (1f + healthGrowth * (wave - 1)));
            yield return new WaitForSeconds(interval);
        }
    }

    public void EnemyDied()
    {
        remaining--;
        if (remaining <= 0 && !clearing) StartCoroutine(OpenShopAfterDelay());
    }

    IEnumerator OpenShopAfterDelay()
    {
        clearing = true;
        clearStart = Time.time;
        shopOpenTime = Time.time + shopDelay;
        yield return new WaitForSeconds(shopDelay);
        clearing = false;
        if (shop != null) shop.Open();
    }

    Vector3 RandomPoint()
    {
        return new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x),
                           Random.Range(-arenaHalfSize.y, arenaHalfSize.y), 0f);
    }

    Vector3 SpawnPoint()
    {
        Vector3 pos = RandomPoint();
        for (int tries = 0; tries < 10; tries++)
        {
            if (Vector3.Distance(pos, player.position) > 10f) return pos;
            pos = RandomPoint();
        }
        return pos;
    }

    // text with a soft drop shadow, so it reads over the floor
    void DrawText(Rect r, string text, GUIStyle st, Color col, float shadowOffset, float alpha)
    {
        GuiText.SetColor(st, new Color(0f, 0f, 0f, 0.65f * alpha));
        GUI.Label(new Rect(r.x + shadowOffset, r.y + shadowOffset, r.width, r.height), text, st);
        GuiText.SetColor(st, new Color(col.r, col.g, col.b, col.a * alpha));
        GUI.Label(r, text, st);
    }

    void EnsureStyles()
    {
        if (hudStyle != null) return;
        hudStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        bannerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        numberStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
    }

    void OnGUI() // temporary UI until we build the real one
    {
        if (shop != null && shop.IsOpen) return; // the spellbook has its own screen
        EnsureStyles();

        float s = Screen.height / 1080f;
        float f = s * fontScale;
        Font body = bodyFont != null ? bodyFont : font;

        hudStyle.font = body;
        bannerStyle.font = font;
        numberStyle.font = font;
        labelStyle.font = body;

        // ---- wave + enemies (top left, under the hearts) ----
        hudStyle.fontSize = Mathf.RoundToInt(hudFontSize * fontScale);
        DrawText(new Rect(10f, hudY, 500f, hudLineSpacing), "Wave " + wave, hudStyle, hudColor, 2f, 1f);
        DrawText(new Rect(10f, hudY + hudLineSpacing, 500f, hudLineSpacing),
                 "Enemies left: " + Mathf.Max(0, remaining), hudStyle, hudColor, 2f, 1f);

        // ---- wave cleared + countdown (center) ----
        if (!clearing) return;

        float fade = Mathf.Clamp01((Time.time - clearStart) / 0.35f);   // fades in
        float timeLeft = Mathf.Max(0f, shopOpenTime - Time.time);
        int secs = Mathf.CeilToInt(timeLeft);
        float sinceChange = 1f - (timeLeft - (secs - 1));              // 0 right when the number changes
        float pop = 1f + numberPulse * Mathf.Pow(1f - Mathf.Clamp01(sinceChange), 2f);

        float w = Screen.width, h = Screen.height;
        float cy = h * bannerY;

        bannerStyle.fontSize = Mathf.RoundToInt(bannerFontSize * f);
        numberStyle.fontSize = Mathf.RoundToInt(numberFontSize * f);
        labelStyle.fontSize = Mathf.RoundToInt(labelFontSize * f);

        DrawText(new Rect(0, cy, w, 110f * s), string.Format(clearedText, wave),
                 bannerStyle, bannerColor, 3f * s, fade);

        float ny = cy + 100f * s;
        if (!string.IsNullOrEmpty(countdownLabel))
        {
            DrawText(new Rect(0, ny, w, 50f * s), countdownLabel, labelStyle, bannerColor, 2f * s, fade);
            ny += 50f * s;
        }

        // the number pops, then settles
        Matrix4x4 old = GUI.matrix;
        Vector2 pivot = new Vector2(w / 2f, ny + 100f * s);
        GUIUtility.ScaleAroundPivot(new Vector2(pop, pop), pivot);
        DrawText(new Rect(0, ny, w, 200f * s), secs.ToString(), numberStyle, numberColor, 4f * s, fade);
        GUI.matrix = old;
    }
}