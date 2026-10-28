using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2.5f;
    public int maxHealth = 3;
    public GameObject coinPrefab;
    public int coinsDropped = 2;
    public bool hopToMove = true;        // only moves while the slime is in the air
    public float airSpeedBoost = 2f;     // makes up for standing still half the time

    int health;
    bool dead;
    Rigidbody rb;
    Transform player;
    SpriteRenderer sr;
    SlimeVisuals visuals;
    Color baseColor;
    float slowFactor = 1f;
    float slowUntil;

    void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player").transform;
        sr = GetComponentInChildren<SpriteRenderer>();
        visuals = GetComponentInChildren<SlimeVisuals>();
        baseColor = sr.color;
    }

    void FixedUpdate()
    {
        if (player == null || dead) return;
        Vector3 dir = player.position - transform.position;
        dir.z = 0f;
        float mult = Time.time < slowUntil ? slowFactor : 1f;
        if (hopToMove && visuals != null)
            mult *= visuals.IsAirborne ? airSpeedBoost : 0f;
        rb.linearVelocity = dir.normalized * speed * mult; // rb.velocity in older Unity
    }

    void Update()
    {
        // SlimeVisuals owns the color while flashing or dying
        if (visuals != null && (visuals.IsFlashing || visuals.IsDying)) return;
        sr.color = Time.time < slowUntil ? Color.cyan : baseColor;
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
            collision.collider.GetComponent<PlayerHealth>().TakeHit();
    }

    public void Slow(float factor, float duration)
    {
        slowFactor = factor;
        slowUntil = Time.time + duration;
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;
        DamageNumbers.Spawn(transform.position, amount);
        health -= amount;
        if (health <= 0) Die();
        else if (visuals != null) visuals.Flash();
    }

    void Die()
    {
        dead = true;
        for (int i = 0; i < coinsDropped; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            Instantiate(coinPrefab, transform.position + offset, Quaternion.identity);
        }
        if (WaveManager.Instance != null) WaveManager.Instance.EnemyDied();

        if (visuals != null)
        {
            rb.linearVelocity = Vector3.zero;   // stop sliding before it goes kinematic
            visuals.Die(gameObject);                      // plays the squash/fade, then destroys the enemy
        }
        else
        {
            Destroy(gameObject);
        }
    }
}