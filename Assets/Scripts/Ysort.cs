using UnityEngine;

public class Ysort : MonoBehaviour
{
    public int baseOrder = 50;      // stays between the floor/walls and the darkness layer
    public float perUnit = 5f;
    public bool useZ = false;       // tick if your ground is the XZ plane
    SpriteRenderer sr;

    void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); }

    void LateUpdate()
    {
        float feet = useZ ? transform.position.z : sr.bounds.min.y;
        sr.sortingOrder = baseOrder - Mathf.RoundToInt(feet * perUnit);
    }
}