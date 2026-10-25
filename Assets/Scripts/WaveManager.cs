using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    public GameObject enemyPrefab;
    public GameObject coinPrefab;
    public Transform player;
    public ShopManager shop;

    public int baseEnemies = 3;           // enemies in wave 1
    public int enemiesPerWave = 2;        // extra enemies each wave
    public float spawnInterval = 1.5f;    // seconds between spawns in wave 1
    public float intervalShrink = 0.05f;  // spawns get this much faster each wave
    public float minSpawnInterval = 0.5f; // never faster than this
    public float speedGrowth = 0.05f;     // +5% enemy speed per wave
    public int startingCoins = 5;         // coins scattered at the start of each wave
    public float shopDelay = 3f;          // seconds you can keep moving after the last kill
    public Vector2 arenaHalfSize = new Vector2(19f, 11f);

    int wave;
    int remaining; // enemies left to kill, including ones not spawned yet
    float shopOpenTime;
    bool clearing;
    GUIStyle label, banner, countdownStyle;

    public int Wave { get { return wave; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (shop == null)
            Debug.LogError("WaveManager: the Shop slot is empty. Add the ShopManager script to GameManager, then drag GameManager into this slot.");
        StartNextWave();
    }

    public void StartNextWave()
    {
        wave++;

        for (int i = 0; i < startingCoins; i++)
            Instantiate(coinPrefab, RandomPoint(), Quaternion.identity);

        int total = baseEnemies + (wave - 1) * enemiesPerWave;
        remaining = total;
        StartCoroutine(SpawnRoutine(total));
    }

    IEnumerator SpawnRoutine(int total)
    {
        float interval = Mathf.Max(minSpawnInterval, spawnInterval - intervalShrink * (wave - 1));
        for (int i = 0; i < total; i++)
        {
            Enemy e = Instantiate(enemyPrefab, SpawnPoint(), Quaternion.identity).GetComponent<Enemy>();
            e.speed *= 1f + speedGrowth * (wave - 1);
            yield return new WaitForSeconds(interval);
        }
    }

    public void EnemyDied()
    {
        remaining--;
        if (remaining <= 0 && !clearing) StartCoroutine(OpenShopAfterDelay());
    }

    IEnumerator OpenShopAfterDelay()
    {
        clearing = true;
        shopOpenTime = Time.time + shopDelay;
        yield return new WaitForSeconds(shopDelay);
        clearing = false;
        if (shop != null) shop.Open();
    }

    Vector3 RandomPoint()
    {
        return new Vector3(Random.Range(-arenaHalfSize.x, arenaHalfSize.x),
                           Random.Range(-arenaHalfSize.y, arenaHalfSize.y), 0f);
    }

    Vector3 SpawnPoint()
    {
        Vector3 pos = RandomPoint();
        for (int tries = 0; tries < 10; tries++)
        {
            if (Vector3.Distance(pos, player.position) > 10f) return pos;
            pos = RandomPoint();
        }
        return pos;
    }

    void OnGUI() // temporary UI until we build the real one
    {
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 28 };
            label.normal.textColor = Color.white;
            banner = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
            banner.normal.textColor = Color.yellow;
            countdownStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, alignment = TextAnchor.MiddleCenter };
            countdownStyle.normal.textColor = Color.white;
        }

        if (shop != null && shop.IsOpen) return; // the spellbook has its own screen

        GUI.Label(new Rect(10, 110, 300, 40), "Wave " + wave, label);
        GUI.Label(new Rect(10, 150, 400, 40), "Enemies left: " + Mathf.Max(0, remaining), label);

        if (clearing)
        {
            int secs = Mathf.Max(0, Mathf.CeilToInt(shopOpenTime - Time.time));
            GUI.Label(new Rect(0, Screen.height / 2f - 150, Screen.width, 70), "Wave " + wave + " cleared!", banner);
            GUI.Label(new Rect(0, Screen.height / 2f - 80, Screen.width, 80), secs.ToString(), countdownStyle);
        }
    }
}