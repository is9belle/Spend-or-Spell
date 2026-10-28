using UnityEngine;

public class StartScreen : MonoBehaviour
{
    public string title = "My Game";
    public string subtitle = "Press Enter or click Start";
    public Color backgroundColor = new Color(0.08f, 0.1f, 0.18f, 1f);
    public Sprite titleImage;   // optional: use a logo instead of text

    static bool hasStarted;     // so Retry doesn't bring the start screen back

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() { hasStarted = false; }

    void Awake()
    {
        if (hasStarted) { enabled = false; return; }
        Time.timeScale = 0f;
    }

    void StartGame()
    {
        hasStarted = true;
        Time.timeScale = 1f;
        enabled = false;
    }

    void OnGUI()
    {
        GUI.depth = -100;   // draw on top of everything else

        // full-screen background
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        // title (image or text)
        if (titleImage != null)
        {
            Texture2D t = titleImage.texture;
            Rect tr = titleImage.textureRect;
            Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
            float w = Screen.width * 0.5f;
            float h = w * tr.height / tr.width;
            GUI.DrawTextureWithTexCoords(new Rect(cx - w / 2f, cy - 220f - h / 2f, w, h), t, uv);
        }
        else
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 80, alignment = TextAnchor.MiddleCenter };
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, cy - 270f, Screen.width, 120f), title, big);
        }

        var small = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        small.normal.textColor = new Color(0.7f, 0.75f, 0.95f);
        GUI.Label(new Rect(0, cy - 130f, Screen.width, 40f), subtitle, small);

        // buttons
        var btn = new GUIStyle(GUI.skin.button) { fontSize = 32 };
        if (GUI.Button(new Rect(cx - 120f, cy - 40f, 240f, 70f), "Start", btn))
            StartGame();
        if (GUI.Button(new Rect(cx - 120f, cy + 50f, 240f, 70f), "Quit", btn))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Enter / Space also starts
        Event e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space))
            StartGame();
    }
}