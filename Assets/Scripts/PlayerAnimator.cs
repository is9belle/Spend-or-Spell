using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Sprite[] walkDown;  // row 1 of the sheet
    public Sprite[] walkSide;  // row 2 (faces right)
    public Sprite[] walkUp;    // row 4
    public float framesPerSecond = 8f;

    SpriteRenderer sr;
    PlayerController controller;
    Sprite[] current;
    float timer;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponentInParent<PlayerController>();
        current = walkDown;
    }

    void Update()
    {
        if (walkDown == null || walkDown.Length == 0) return;

        Vector3 m = controller.MoveInput;
        bool moving = m.sqrMagnitude > 0.01f;

        if (moving)
        {
            if (Mathf.Abs(m.y) > Mathf.Abs(m.x))
            {
                current = m.y > 0 ? walkUp : walkDown;
                sr.flipX = false;
            }
            else
            {
                current = walkSide;
                sr.flipX = m.x < 0; // the side sprites face right, so flip when going left
            }
            timer += Time.deltaTime * framesPerSecond;
        }
        else
        {
            timer = 0f; // standing still shows the first frame
        }

        sr.sprite = current[(int)timer % current.Length];
    }
}