using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FloorGenerator : MonoBehaviour
{
    public enum Plane { FlatXZ, UprightXY }

    public Sprite[] floorSprites;
    public Plane plane = Plane.UprightXY;
    public int columns = 12;
    public int rows = 8;
    public float tileSize = 1f;
    public float artFill = 0.83f;
    public float lift = 0.05f;

#if UNITY_EDITOR
    [ContextMenu("Generate Floor")]
    void Generate()
    {
        if (floorSprites == null || floorSprites.Length == 0)
        {
            Debug.LogWarning("Floor Sprites list is empty.");
            return;
        }
        Clear();

        Vector3 origin = transform.position;
        Vector3 ls = transform.lossyScale;
        int made = 0;

        for (int i = 0; i < columns; i++)
        for (int j = 0; j < rows; j++)
        {
            var tile = new GameObject($"Floor_{i}_{j}");
            tile.transform.SetParent(transform, false);

            Sprite sp = floorSprites[Random.Range(0, floorSprites.Length)];
            float s = (tileSize / artFill) / sp.bounds.size.x;

            float x = (i - (columns - 1) / 2f) * tileSize;
            float y = (j - (rows - 1) / 2f) * tileSize;

            if (plane == Plane.FlatXZ)
            {
                tile.transform.position = origin + new Vector3(x, lift, y);
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = new Vector3(s / ls.x, s / ls.z, 1);
            }
            else
            {
                tile.transform.position = origin + new Vector3(x, y, lift);
                tile.transform.rotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(s / ls.x, s / ls.y, 1);
            }

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = -10;
            Undo.RegisterCreatedObjectUndo(tile, "Generate Floor");
            made++;
        }
        Debug.Log("FloorGenerator made " + made + " tiles around " + origin);
    }

    [ContextMenu("Clear Floor")]
    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (c.name.StartsWith("Floor_")) Undo.DestroyObjectImmediate(c);
        }
    }
#endif
}