using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 arenaHalfSize = new Vector2(20f, 12f);
    public float edgeMargin = 1.5f; // how far past the inner wall edge the camera can go
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float limitX = arenaHalfSize.x + edgeMargin - halfW;
        float limitY = arenaHalfSize.y + edgeMargin - halfH;
        float x = Mathf.Clamp(target.position.x, -limitX, limitX);
        float y = Mathf.Clamp(target.position.y, -limitY, limitY);
        transform.position = new Vector3(x, y, transform.position.z);
    }
}