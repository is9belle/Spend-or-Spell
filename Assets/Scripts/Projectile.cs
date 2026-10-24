using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 3f;

    public void Launch(Vector3 direction)
    {
        GetComponent<Rigidbody>().linearVelocity = direction * speed; // rb.velocity in older Unity
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall")) Destroy(gameObject);
    }
}