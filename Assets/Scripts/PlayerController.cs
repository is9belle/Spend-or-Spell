using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    Rigidbody rb;
    Camera cam;
    Vector3 move;
    public Vector3 MoveInput { get { return move; } }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    void Update()
    {
        move = new Vector3(Input.GetAxisRaw("Horizontal"),
                           Input.GetAxisRaw("Vertical"), 0f).normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * speed; // use rb.velocity in older Unity versions
    }

    public Vector3 AimDirection()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -cam.transform.position.z; // distance from camera to the play plane
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return (world - transform.position).normalized;
    }
}