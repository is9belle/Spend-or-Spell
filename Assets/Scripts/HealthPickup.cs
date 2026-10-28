using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int amount = 1;
    public bool onlyIfNotFull = true;   // stays on the floor if you're already at max health
    public float lifetime = 0f;         // seconds before it disappears (0 = forever)
    public float bobHeight = 0.08f;
    public float bobSpeed = 3f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    void OnTriggerEnter(Collider other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;
        if (onlyIfNotFull && hp.IsFull()) return;

        for (int i = 0; i < amount; i++) hp.Heal();
        Destroy(gameObject);
    }
}