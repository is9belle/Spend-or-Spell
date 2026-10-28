using UnityEngine;
using UnityEngine.UI;

public class ScreenVignette : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity = 0.35f;
    [Range(0f, 0.95f)] public float start = 0.55f;   // where the darkening begins (0 = center)
    public Color color = Color.black;

    Image img;
    string last;

    void Start()
    {
        var cgo = new GameObject("Vignette", typeof(Canvas));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;                  // behind other UI

        var igo = new GameObject("Image", typeof(RectTransform), typeof(Image));
        igo.transform.SetParent(cgo.transform, false);
        var rt = (RectTransform)igo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        img = igo.GetComponent<Image>();
        img.raycastTarget = false;
        Build();
    }

    string Key() => $"{intensity}|{start}|{color}";

    void Build()
    {
        last = Key();
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float nx = x / (n - 1f) * 2f - 1f;
            float ny = y / (n - 1f) * 2f - 1f;
            float d = Mathf.Sqrt(nx * nx + ny * ny) / 1.414f;   // corners = 1
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, 1f, d)) * intensity;
            tex.SetPixel(x, y, new Color(color.r, color.g, color.b, a));
        }
        tex.Apply();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    void Update() { if (Key() != last) Build(); }
}