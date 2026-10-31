using UnityEngine;

// Put this on the same object as the Enemy script (slime, bat, archer, golem).
// Each enemy prefab gets its own clips.
public class EnemySounds : MonoBehaviour
{
    public AudioClip[] steps;          // walk / wing flap / hop landing
    public AudioClip[] attack;         // bow shot, slam, bite
    public AudioClip[] hurt;
    public AudioClip[] death;

    public float volume = 1f;
    public float pitch = 1f;           // below 1 sounds heavier (good for the golem)
    public float stepInterval = 0.45f; // seconds between steps / flaps while moving
    public bool stepOnLanding = false; // tick this for slimes: plays when a hop lands
    public float attackCooldown = 1f;  // so touching the player doesn't spam the sound

    Enemy enemy;
    Rigidbody rb;
    SlimeVisuals visuals;
    float stepTimer, nextAttack;
    bool wasAirborne;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        rb = GetComponent<Rigidbody>();
        visuals = GetComponentInChildren<SlimeVisuals>();
    }

    void Update()
    {
        if (enemy != null && enemy.IsDead) return;

        if (stepOnLanding && visuals != null)
        {
            bool air = visuals.IsAirborne;
            if (wasAirborne && !air) AudioManager.Play(steps, transform.position, volume, pitch);
            wasAirborne = air;
            return;
        }

        if (rb != null && rb.linearVelocity.magnitude > 0.3f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = stepInterval;
                AudioManager.Play(steps, transform.position, volume, pitch);
            }
        }
        else stepTimer = 0f;   // first step plays right when it starts moving
    }

    public void Attack()
    {
        if (Time.time < nextAttack) return;
        nextAttack = Time.time + attackCooldown;
        AudioManager.Play(attack, transform.position, volume, pitch);
    }

    public void Hurt()  { AudioManager.Play(hurt, transform.position, volume, pitch); }
    public void Death() { AudioManager.Play(death, transform.position, volume, pitch); }
}
