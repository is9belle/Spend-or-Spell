using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;
    public int gold = 20;

    [Header("HUD")]
    public Sprite manaIcon;                 // drag your mana crystal sprite here
    public float iconSize = 44f;
    public Font font;                       // optional: same gothic font as the shop
    public int fontSize = 36;
    public Color textColor = new Color(1f, 0.92f, 0.6f);
    public Vector2 position = new Vector2(10f, 10f);

    GUIStyle style;

    void Awake()
    {
        Instance = this;
    }

    public bool TrySpend(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        return true;
    }

    public void Add(int amount)
    {
        gold += amount;
    }

    void DrawSprite(Sprite sp, Rect r)
    {
        Texture2D t = sp.texture;
        Rect tr = sp.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);

        float aspect = tr.width / tr.height;
        float w = r.width, h = r.height;
        if (aspect > 1f) h = w / aspect; else w = h * aspect;
        Rect fit = new Rect(r.x + (r.width - w) / 2f, r.y + (r.height - h) / 2f, w, h);

        GUI.color = Color.white;
        GUI.DrawTextureWithTexCoords(fit, t, uv);
    }

    void OnGUI() // temporary display until we build the real UI
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
        }
        style.font = font;
        style.fontSize = fontSize;

        float x = position.x, y = position.y;
        string text = gold.ToString();

        if (manaIcon != null)
        {
            DrawSprite(manaIcon, new Rect(x, y, iconSize, iconSize));
            x += iconSize + 8f;
        }
        else
        {
            text = "Mana: " + text;     // falls back to the old text if no icon is assigned
        }

        Rect r = new Rect(x, y, 300f, iconSize);

        // soft shadow so it reads over the floor
        GuiText.SetColor(style, new Color(0f, 0f, 0f, 0.6f));
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
        GuiText.SetColor(style, textColor);
        GUI.Label(r, text, style);
    }
}