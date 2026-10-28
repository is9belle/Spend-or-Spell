using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        public int count = 3;
        public int maxCount = 0;      // if higher than Count, picks a random number between the two
        public float radius = 0.5f;   // personal space around this obstacle
    }

    public Entry[] entries;
    public Vector2 areaSize = new Vector2(14f, 8f);   // the spawnable area, centered on this object
    public bool useZ = false;                          // tick if your ground is the XZ plane
    public Transform keepClearAround;                  // drag the Player here
    public float keepClearRadius = 2.5f;
    public float extraSpacing = 0.5f;                  // minimum gap between obstacles
    public int seed = 0;                               // 0 = different every time, any other number = fixed layout
    public bool spawnOnStart = true;
    public int triesPerObstacle = 40;

    readonly List<GameObject> spawned = new List<GameObject>();

    void Start() { if (spawnOnStart) Respawn(); }

    Vector2 Flat(Vector3 v) { return useZ ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y); }

    Vector3 ToWorld(Vector2 p, Vector3 prefabPos)
    {
        return useZ ? new Vector3(p.x, prefabPos.y, p.y) : new Vector3(p.x, p.y, prefabPos.z);
    }

    [ContextMenu("Respawn")]
    public void Respawn()
    {
        Clear();
        var oldState = Random.state;
        if (seed != 0) Random.InitState(seed);

        var placed = new List<(Vector2 pos, float r)>();
        Vector2 center = Flat(transform.position);
        Vector2 clearCenter = keepClearAround ? Flat(keepClearAround.position) : center;

        foreach (var e in entries)
        {
            if (e.prefab == null) continue;
            int n = e.maxCount > e.count ? Random.Range(e.count, e.maxCount + 1) : e.count;

            for (int i = 0; i < n; i++)
            {
                for (int t = 0; t < triesPerObstacle; t++)
                {
                    Vector2 p = center + new Vector2(
                        Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                        Random.Range(-areaSize.y / 2f, areaSize.y / 2f));

                    if (Vector2.Distance(p, clearCenter) < keepClearRadius + e.radius) continue;

                    bool ok = true;
                    foreach (var o in placed)
                        if (Vector2.Distance(p, o.pos) < o.r + e.radius + extraSpacing) { ok = false; break; }
                    if (!ok) continue;

                    var go = Instantiate(e.prefab, ToWorld(p, e.prefab.transform.position),
                                         e.prefab.transform.rotation, transform);
                    spawned.Add(go);
                    placed.Add((p, e.radius));
                    break;
                }
            }
        }
        Random.state = oldState;
    }

    public void Clear()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 size = useZ ? new Vector3(areaSize.x, 0.1f, areaSize.y) : new Vector3(areaSize.x, areaSize.y, 0.1f);
        Gizmos.DrawWireCube(transform.position, size);
        if (keepClearAround)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(keepClearAround.position, keepClearRadius);
        }
    }
}