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

    public void TakeHit()
    {
        if (gameOver || Time.time < invincibleUntil) return;

        lives--;
        invincibleUntil = Time.time + invincibleTime;

        if (lives <= 0)
        {
            gameOver = true;
            SetAlpha(1f);
            Time.timeScale = 0f; // freezes the game
        }
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

        if (gameOver)
        {
            GUIStyle big = new GUIStyle(GUI.skin.label);
            big.fontSize = 64;
            big.alignment = TextAnchor.MiddleCenter;
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f - 100, Screen.width, 100), "Game Over", big);

            if (GUI.Button(new Rect(Screen.width / 2f - 100, Screen.height / 2f + 10, 200, 60), "Retry"))
            {
                Time.timeScale = 1f;
                SceneFader.Load(SceneManager.GetActiveScene().name);
            }
        }
    }
}