using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FootprintCollider : MonoBehaviour
{
    [Range(0.1f, 1f)] public float widthFraction = 0.7f;    // how much of the sprite's width the box covers
    [Range(0.05f, 1f)] public float heightFraction = 0.25f; // how much of the sprite's height, measured up from its bottom
    public float depth = 1f;                                // thickness along Z
    public bool liveUpdate = false;                         // tick to tune the numbers while playing

    BoxCollider box;
    SpriteRenderer sr;

    void Awake() { Fit(); }
    void LateUpdate() { if (liveUpdate) Fit(); }

    [ContextMenu("Fit Collider")]
    public void Fit()
    {
        if (box == null) box = GetComponent<BoxCollider>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Bounds b = sr.bounds;                               // the sprite's real world-space box
        float w = b.size.x * widthFraction;
        float h = b.size.y * heightFraction;

        // box sits on the bottom edge of the art
        Vector3 centerWorld = new Vector3(b.center.x, b.min.y + h / 2f, b.center.z);

        Vector3 ls = transform.lossyScale;
        box.center = transform.InverseTransformPoint(centerWorld);
        box.size = new Vector3(w / Mathf.Abs(ls.x), h / Mathf.Abs(ls.y), depth / Mathf.Abs(ls.z));
    }
}