using UnityEngine;

public class WalkBob : MonoBehaviour
{
    public float stepLength = 0.45f;    // world units travelled per footstep. Smaller = faster bouncing
    public float bobHeight = 0.07f;     // how high each step lifts him
    public float squash = 0.04f;        // slight squash when a foot lands
    public float tiltDegrees = 3f;      // sway left and right with each step
    public float minSpeed = 0.15f;      // below this he counts as standing still
    public float blendSpeed = 12f;      // how quickly the effect fades in and out

    Rigidbody rb;
    Transform root;
    Vector3 lastRootPos;
    float baseY;
    Vector3 baseScale;
    float phase, amount;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        root = transform.parent != null ? transform.parent : transform;
        lastRootPos = root.position;
        baseY = transform.localPosition.y;
        baseScale = transform.localScale;
    }

    float Speed()
    {
        if (rb != null) return rb.linearVelocity.magnitude;   // use rb.velocity on older Unity
        float v = (root.position - lastRootPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastRootPos = root.position;
        return v;
    }

    void LateUpdate()
    {
        float speed = Speed();
        bool moving = speed > minSpeed && Time.timeScale > 0f;

        if (moving) phase += speed * Time.deltaTime / stepLength;
        amount = Mathf.MoveTowards(amount, moving ? 1f : 0f, blendSpeed * Time.deltaTime);

        float bounce = Mathf.Abs(Mathf.Sin(phase * Mathf.PI));   // one bounce per step
        float side = Mathf.Sin(phase * Mathf.PI) * ((Mathf.FloorToInt(phase) % 2 == 0) ? 1f : -1f);
        float landing = 1f - bounce;                              // 1 when the foot touches down

        Vector3 p = transform.localPosition;
        p.y = baseY + bounce * bobHeight * amount;
        transform.localPosition = p;

        transform.localScale = new Vector3(baseScale.x * (1f + squash * 0.5f * landing * amount),
                                           baseScale.y * (1f - squash * landing * amount),
                                           baseScale.z);

        transform.localRotation = Quaternion.Euler(0f, 0f, side * tiltDegrees * amount);
    }
}