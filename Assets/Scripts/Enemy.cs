using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum Behavior { Chase, Erratic, Ranged }

    [Header("Stats")]
    public float speed = 2.5f;
    public int maxHealth = 3;
    public int contactDamage = 1;                  // hearts taken when it touches you
    [Range(0f, 1f)] public float slowResistance = 0f;   // 1 = immune to Frost Nova's slow
    public GameObject coinPrefab;
    public int coinsDropped = 2;

    [Header("Movement style")]
    public Behavior behavior = Behavior.Chase;
    public bool hopToMove = true;                  // slimes: only move while in the air
    public float airSpeedBoost = 2f;
    public bool flipToFace = false;                // turn the sprite toward the player
    public bool spriteFacesRight = true;

    [Header("Erratic (bat)")]
    public float weaveAngle = 55f;                 // how far it veers off a straight line
    public float weaveSpeed = 4f;

    [Header("Ranged (archer)")]
    public float preferredDistance = 7f;
    public float distanceTolerance = 1.5f;
    public float fireRange = 13f;                  // it only shoots inside this distance
    public float fireInterval = 2f;
    public float firstShotDelay = 1f;
    public float arrowSpeed = 8f;
    public int arrowDamage = 1;
    public Sprite arrowSprite;                     // optional: a built-in arrow is used if empty
    public float arrowScale = 1f;
    public float recoveryPause = 0.6f;     // seconds he stands still after releasing the arrow

    int health;
    bool dead;
    Rigidbody rb;
    Transform player;
    SpriteRenderer sr;
    SlimeVisuals visuals;
    Color baseColor;
    float slowFactor = 1f;
    float slowUntil;
    float phase, nextShot, strafeSign = 1f, nextStrafeFlip;
    float pauseUntil;

    [Header("Boss / special")]
    public bool countsForWave = true;                      // false for minions, so they don't count as "Enemies left"
    [System.NonSerialized] public bool externalControl;    // a boss script is driving the movement
    [System.NonSerialized] public bool invulnerable;
    public System.Action OnDeath;
    public int Health { get { return health; } }
    public bool IsDead { get { return dead; } }

    void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindWithTag("Player").transform;
        sr = GetComponentInChildren<SpriteRenderer>();
        visuals = GetComponentInChildren<SlimeVisuals>();
        baseColor = sr.color;
        phase = Random.Range(0f, 6.28f);
        nextShot = Time.time + firstShotDelay + Random.Range(0f, 0.8f);
    }

    void FireArrow()
    {
        if (dead || player == null) return;
        Vector3 d = player.position - transform.position;
        d.z = 0f;
        d = d.normalized;
        EnemyProjectile.Spawn(transform.position + d * 0.6f, d, arrowSprite, arrowScale, arrowSpeed, arrowDamage);
        pauseUntil = Time.time + recoveryPause;
    }

    void FixedUpdate()
    {
        if (player == null || dead) return;

        if (externalControl) return;

        if (behavior == Behavior.Ranged &&
            ((visuals != null && visuals.IsAttacking) || Time.time < pauseUntil))
        {
            rb.linearVelocity = Vector3.zero;   // rb.velocity in older Unity
            return;                             // he can't move while drawing or recovering
        }

        Vector3 to = player.position - transform.position;
        to.z = 0f;
        float dist = to.magnitude;
        Vector3 dir = to.normalized;
        Vector3 move = dir;

        switch (behavior)
        {
            case Behavior.Erratic:
                move = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * weaveSpeed + phase) * weaveAngle) * dir;
                break;

            case Behavior.Ranged:
                if (dist < preferredDistance - distanceTolerance) move = -dir;          // too close: back off
                else if (dist > preferredDistance + distanceTolerance) move = dir;      // too far: approach
                else
                {
                    if (Time.time >= nextStrafeFlip) { strafeSign = -strafeSign; nextStrafeFlip = Time.time + Random.Range(1.5f, 3.5f); }
                    move = new Vector3(-dir.y, dir.x, 0f) * strafeSign * 0.6f;          // circle slowly
                }
                break;
        }

        float mult = 1f;
        if (Time.time < slowUntil) mult = Mathf.Lerp(slowFactor, 1f, slowResistance);
        if (hopToMove && visuals != null) mult *= visuals.IsAirborne ? airSpeedBoost : 0f;
        rb.linearVelocity = move * speed * mult;   // rb.velocity in older Unity
    }

    void Update()
    {
        if (dead || player == null) return;

        if (behavior == Behavior.Ranged && Time.time >= nextShot)
        {
            Vector3 to = player.position - transform.position;
            to.z = 0f;
            if (to.magnitude <= fireRange)
            {
                nextShot = Time.time + fireInterval;
                if (visuals != null && visuals.HasAttackAnimation) visuals.PlayAttack(FireArrow);
                else FireArrow();
            }
        }

        if (flipToFace)
            sr.flipX = (player.position.x < transform.position.x) == spriteFacesRight;

        // SlimeVisuals owns the color while flashing or dying
        if (visuals != null && (visuals.IsFlashing || visuals.IsDying)) return;
        sr.color = Time.time < slowUntil ? Color.cyan : baseColor;
    }

    void OnCollisionStay(Collision collision)
    {
        if (dead) return;
        if (collision.collider.CompareTag("Player"))
            collision.collider.GetComponent<PlayerHealth>().TakeHit(contactDamage);
    }

    public void Slow(float factor, float duration)
    {
        slowFactor = factor;
        slowUntil = Time.time + duration;
    }

    public void TakeDamage(int amount)
    {
        if (dead || invulnerable) return;
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
        if (countsForWave && WaveManager.Instance != null) WaveManager.Instance.EnemyDied();
        if (OnDeath != null) OnDeath();

        if (visuals != null)
        {
            rb.linearVelocity = Vector3.zero;
            visuals.Die(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}