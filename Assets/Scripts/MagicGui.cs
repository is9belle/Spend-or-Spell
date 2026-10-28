using System.Collections.Generic;
using UnityEngine;

public static class MagicGui
{
    static readonly Dictionary<int, GUIStyle> round = new Dictionary<int, GUIStyle>();
    static GUIStyle textStyle;
    static readonly Color gold = new Color(1f, 0.92f, 0.6f);

    static GUIStyle Round(float radiusF)
    {
        int radius = Mathf.Max(2, Mathf.RoundToInt(radiusF));
        GUIStyle st;
        if (round.TryGetValue(radius, out st) && st.normal.background != null) return st;

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
        round[radius] = st;
        return st;
    }

    public static void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void RFill(Rect r, Color c, float radius)
    {
        GUI.color = c;
        GUI.Box(r, GUIContent.none, Round(radius));
        GUI.color = Color.white;
    }

    public static void Shadowed(Rect r, string text, GUIStyle st, Color col, float offset, float alpha = 1f)
    {
        GuiText.SetColor(st, new Color(0f, 0f, 0f, 0.6f * alpha));
        GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, st);
        GuiText.SetColor(st, new Color(col.r, col.g, col.b, col.a * alpha));
        GUI.Label(r, text, st);
    }

    static void Sparkle(Vector2 center, float size, Color c)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, center);
        Fill(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), c);
        GUI.matrix = old;
    }

    // glowing, pulsing spell button. Returns true when clicked.
    public static bool Button(Rect r, string label, Color baseColor, Font font, int fontSize, float s, bool enabled = true)
    {
        bool hover = enabled && r.Contains(Event.current.mousePosition);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        float rad = r.height * 0.35f;

        Color edge, body, text;
        if (enabled)
        {
            float glow = (hover ? 0.55f : 0.25f) + 0.2f * pulse;
            float g = (hover ? 10f : 6f) * s;
            RFill(new Rect(r.x - g, r.y - g, r.width + g * 2f, r.height + g * 2f),
                  new Color(baseColor.r + 0.3f, baseColor.g + 0.3f, baseColor.b + 0.3f, glow * 0.6f), rad + g);
            edge = Color.Lerp(baseColor, Color.white, hover ? 0.85f : 0.6f);
            body = Color.Lerp(baseColor, Color.black, hover ? 0.3f : 0.5f);
            text = gold;
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
        RFill(new Rect(r.x + b * 2f, r.y + b * 1.5f, r.width - b * 4f, r.height * 0.38f),
              new Color(1f, 1f, 1f, enabled ? 0.12f : 0.04f), rad * 0.7f);

        if (enabled)
        {
            float sz = (7f + 4f * pulse) * s;
            Color sp = new Color(1f, 0.95f, 0.7f, 0.6f + 0.4f * pulse);
            Sparkle(new Vector2(r.x + r.height * 0.35f, r.center.y), sz, sp);
            Sparkle(new Vector2(r.xMax - r.height * 0.35f, r.center.y), sz, sp);
        }

        if (textStyle == null)
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
        textStyle.font = font;
        textStyle.fontSize = fontSize;
        float textW = textStyle.CalcSize(new GUIContent(label)).x;
        float maxW = r.width - r.height;
        if (textW > maxW) textStyle.fontSize = Mathf.Max(8, Mathf.FloorToInt(fontSize * maxW / textW));
        Shadowed(r, label, textStyle, text, 2f * s);

        return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
}