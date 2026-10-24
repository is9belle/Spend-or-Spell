using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int lives = 3;
    public float invincibleTime = 1.5f;

    float invincibleUntil;
    bool gameOver;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public void TakeHit()
    {
        if (gameOver || Time.time < invincibleUntil) return;

        lives--;
        invincibleUntil = Time.time + invincibleTime;

        if (lives <= 0)
        {
            gameOver = true;
            rend.enabled = true;
            Time.timeScale = 0f; // freezes the game
        }
    }

    void Update()
    {
        if (gameOver) return;
        // blink while invincible
        bool blinkOff = Time.time < invincibleUntil && ((int)(Time.time * 10f) % 2 == 0);
        rend.enabled = !blinkOff;
    }

    void OnGUI() // temporary UI until we build the real one
    {
        // lives shown as red squares (stand-ins for pixel hearts)
        GUI.color = Color.red;
        for (int i = 0; i < lives; i++)
            GUI.DrawTexture(new Rect(10 + i * 40, 70, 32, 32), Texture2D.whiteTexture);
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
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}