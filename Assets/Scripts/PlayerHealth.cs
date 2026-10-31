using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int lives = 3;
    public float invincibleTime = 1.5f;
    public int maxLives = 3;
    public Sprite heartSprite;
    public float heartSize = 32f;
    public float heartY = 58f;                              // lower number = higher on screen
    public Color heartTint = new Color(1f, 0.7f, 0.7f);     // pulls the pink back toward red
    public Color outlineColor = new Color(0.25f, 0f, 0f);   // dark red-black
    public float outlineSize = 2f;                          // thickness in screen pixels

    [Header("Game over screen")]
    public Font font;                                       // your gothic font
    public float fontScale = 1.1f;
    public Color titleColor = new Color(0.85f, 0.15f, 0.2f);
    public Color retryColor = new Color(0.7f, 0.14f, 0.18f);
    public Color menuColor = new Color(0.42f, 0.2f, 0.7f);
    public string menuSceneName = "MenuScreen";             // leave empty to hide the Main Menu button

    public bool IsFull()
    {
       return lives >= maxLives;
    }

    public void Heal()
    {  
        lives = Mathf.Min(lives + 1, maxLives);
    }

    [Range(0f, 1f)] public float blinkAlpha = 0.3f;

    float invincibleUntil;
    bool gameOver;
    SpriteRenderer sr;
    Color baseColor;
    GUIStyle titleStyle, subStyle;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseColor = sr.color;
    }

    void SetAlpha(float a)
    {
        Color c = baseColor;
        c.a = a;
        sr.color = c;
    }

    public void TakeHit(int damage = 1)
    {
        if (gameOver || Time.time < invincibleUntil) return;

        lives -= damage;
        invincibleUntil = Time.time + invincibleTime;

        if (lives <= 0)
        {
            gameOver = true;
            AudioManager.PlayerDeath(transform.position);
            if (MentorDialogue.Instance != null) MentorDialogue.Instance.GameOver();
            SetAlpha(1f);
            Time.timeScale = 0f;
        }
        else AudioManager.PlayerHurt(transform.position);
    }

    void Update()
    {
        if (gameOver) return;
        bool blinkOff = Time.time < invincibleUntil && ((int)(Time.time * 10f) % 2 == 0);
        SetAlpha(blinkOff ? blinkAlpha : 1f);
    }

    void DrawSprite(Sprite s, Rect r)
    {
        Texture2D t = s.texture;
        Rect tr = s.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        GUI.DrawTextureWithTexCoords(r, t, uv);
    }

    void OnGUI() // temporary UI until we build the real one
    {
        GUI.color = heartTint;
        for (int i = 0; i < lives; i++)
        {
            Rect r = new Rect(10 + i * (heartSize + 8), heartY, heartSize, heartSize);

            if (heartSprite != null)
            {
                // outline: dark copies shifted in 8 directions
                GUI.color = outlineColor;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    DrawSprite(heartSprite, new Rect(r.x + dx * outlineSize, r.y + dy * outlineSize, r.width, r.height));
                }

                // the heart itself
                GUI.color = heartTint;
                DrawSprite(heartSprite, r);
            }
            else
            {
                GUI.color = Color.red;
                GUI.DrawTexture(r, Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;

        if (gameOver) DrawGameOver();
    }

    void DrawGameOver()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        }

        float w = Screen.width, h = Screen.height;
        float s = h / 1080f;
        float f = s * fontScale;

        titleStyle.font = font;
        subStyle.font = font;
        titleStyle.fontSize = Mathf.RoundToInt(130f * f);
        subStyle.fontSize = Mathf.RoundToInt(40f * f);

        MagicGui.Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.78f));

        MagicGui.Shadowed(new Rect(0, h * 0.5f - 300f * s, w, 170f * s), "Game Over", titleStyle, titleColor, 5f * s);

        if (WaveManager.Instance != null)
            MagicGui.Shadowed(new Rect(0, h * 0.5f - 130f * s, w, 60f * s),
                "You reached wave " + WaveManager.Instance.Wave, subStyle, new Color(1f, 0.92f, 0.6f), 2f * s);

        int btnFont = Mathf.RoundToInt(48f * f);
        Rect retry = new Rect(w / 2f - 270f * s, h * 0.5f - 30f * s, 540f * s, 100f * s);
        if (MagicGui.Button(retry, "Retry", retryColor, font, btnFont, s))
        {
            AudioManager.Click();
            Time.timeScale = 1f;
            SceneFader.Load(SceneManager.GetActiveScene().name);
        }

        if (!string.IsNullOrEmpty(menuSceneName))
        {
            Rect menu = new Rect(w / 2f - 230f * s, h * 0.5f + 110f * s, 460f * s, 84f * s);
            if (MagicGui.Button(menu, "Main Menu", menuColor, font, Mathf.RoundToInt(40f * f), s))
            {
                AudioManager.Click();
                Time.timeScale = 1f;
                SceneFader.Load(menuSceneName);
            }
        }
    }
}