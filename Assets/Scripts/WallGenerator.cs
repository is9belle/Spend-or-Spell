using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WallGenerator : MonoBehaviour
{
    public enum Side { Top, Bottom, Left, Right }

    public Side side = Side.Top;
    public Sprite[] wallSprites;
    public Vector2 arenaHalfSize = new Vector2(20f, 12f);
    public float tileWidth = 1.1f;   // how big the stones look, in world units
    public float overlap = 0.06f;    // how much tiles overlap to hide gaps (0.06 = 6%)
    public int sortingOrder = 0;
    public float lift = 0.02f;

#if UNITY_EDITOR
    [ContextMenu("Generate Wall")]
    void Generate()
    {
        if (wallSprites == null || wallSprites.Length == 0)
        {
            Debug.LogWarning("Wall Sprites list is empty.");
            return;
        }
        Clear();

        bool horizontal = (side == Side.Top || side == Side.Bottom);
        Sprite first = wallSprites[0];
        float s0 = tileWidth / first.bounds.size.x;
        float thickness = horizontal ? first.bounds.size.y * s0 : tileWidth;
        float along = horizontal ? tileWidth : first.bounds.size.y * s0;
        float pitch = along * (1f - overlap);

        float length = horizontal ? arenaHalfSize.x * 2f : arenaHalfSize.y * 2f + tileWidth * 2f;
        int count = Mathf.CeilToInt(length / pitch) + 1;

        Vector3 origin = transform.position;
        Vector3 ls = transform.lossyScale;

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * pitch;
            Sprite sp = wallSprites[Random.Range(0, wallSprites.Length)];
            float s = tileWidth / sp.bounds.size.x;

            Vector3 pos;
            switch (side)
            {
                case Side.Top:    pos = new Vector3(offset, arenaHalfSize.y + thickness / 2f, lift); break;
                case Side.Bottom: pos = new Vector3(offset, -(arenaHalfSize.y + thickness / 2f), lift); break;
                case Side.Left:   pos = new Vector3(-(arenaHalfSize.x + thickness / 2f), offset, lift); break;
                default:          pos = new Vector3(arenaHalfSize.x + thickness / 2f, offset, lift); break;
            }

            var tile = new GameObject("Wall_" + i);
            tile.transform.SetParent(transform, false);
            tile.transform.position = origin + pos;
            tile.transform.rotation = Quaternion.identity;
            tile.transform.localScale = new Vector3(s / ls.x, s / ls.y, 1f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortingOrder;
            sr.flipY = (side == Side.Bottom);
            Undo.RegisterCreatedObjectUndo(tile, "Generate Wall");
        }
        Debug.Log("WallGenerator (" + side + "): " + count + " tiles. Visible wall thickness = "
                  + thickness.ToString("F2") + " - set the camera Edge Margin to about this.");
    }

    [ContextMenu("Clear Wall")]
    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (c.name.StartsWith("Wall_")) Undo.DestroyObjectImmediate(c);
        }
    }
#endif
}