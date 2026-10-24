using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2.5f;
    public int maxHealth = 3;
    public GameObject coinPrefab;
    public int coinsDropped = 2;

    int health;
    bool dead;
    Rigidbody rb;
    Transform player;

    void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.z = 0f;
        rb.linearVelocity = dir.normalized * speed; // rb.velocity in older Unity
    }

    void OnCollisionStay(Collision collision)
{
    if (collision.collider.CompareTag("Player"))
    {
        collision.collider.GetComponent<PlayerHealth>().TakeHit();
    }
}

    public void TakeDamage(int amount)
    {
        if (dead) return;
        health -= amount;
        if (health <= 0) Die();
    }

    void Die()
    {
        dead = true;
        for (int i = 0; i < coinsDropped; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            Instantiate(coinPrefab, transform.position + offset, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}