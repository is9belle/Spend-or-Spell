using UnityEngine;

public class BlocksProjectiles : MonoBehaviour
{
    void OnCollisionEnter(Collision c) { Stop(c.gameObject); }
    void OnTriggerEnter(Collider o) { Stop(o.gameObject); }

    void Stop(GameObject o)
    {
        var p = o.GetComponentInParent<Projectile>();
        if (p != null) Destroy(p.gameObject);
    }
}