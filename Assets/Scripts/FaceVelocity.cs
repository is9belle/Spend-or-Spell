using UnityEngine;

public class FaceVelocity : MonoBehaviour
{
    public float angleOffset = 0f;   // 0 if the art points right, -90 if it points up, 180 if it points left
    public float spin = 0f;          // optional extra spin in degrees per second

    Vector3 last;
    float baseAngle, extra;

    void Start() { last = transform.position; }

    void LateUpdate()
    {
        Vector3 d = transform.position - last;
        last = transform.position;
        if (d.sqrMagnitude > 0.000001f)
            baseAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + angleOffset;

        extra += spin * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + extra);
    }
}