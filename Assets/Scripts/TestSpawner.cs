using UnityEngine;

public class TestSpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public float interval = 3f;
    public Vector2 arenaHalfSize = new Vector2(19f, 11f);
    float next;

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + interval;

        for (int tries = 0; tries < 10; tries++)
        {
            Vector3 pos = new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x),
                                      Random.Range(-arenaHalfSize.y, arenaHalfSize.y), 0f);
            if (Vector3.Distance(pos, player.position) > 10f)
            {
                Instantiate(enemyPrefab, pos, Quaternion.identity);
                return;
            }
        }
    }
}