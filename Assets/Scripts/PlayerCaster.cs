using UnityEngine;

public class PlayerCaster : MonoBehaviour
{
    public Projectile projectilePrefab;
    public int spellCost = 1;
    public float fireCooldown = 0.25f;
    PlayerController player;
    float nextFire;

    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Input.GetMouseButton(0) && Time.time >= nextFire
            && GoldManager.Instance.TrySpend(spellCost))
        {
            nextFire = Time.time + fireCooldown;
            Vector3 dir = player.AimDirection();
            Projectile p = Instantiate(projectilePrefab,
                transform.position + dir * 1f, Quaternion.identity);
            p.Launch(dir);
        }
    }
}