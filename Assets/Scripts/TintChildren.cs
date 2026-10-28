using UnityEngine;

public class TintChildren : MonoBehaviour
{
    public Color tint = new Color(0.45f, 0.45f, 0.5f, 1f);
    Color last;

    void Start() { Apply(); }

    void Update()
    {
        if (tint != last) Apply();   // lets you tweak live in Play mode
    }

    void Apply()
    {
        last = tint;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.color = tint;
    }
}