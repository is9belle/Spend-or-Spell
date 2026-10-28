using UnityEngine;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    public bool randomStart = true;   // so a group of slimes don't hop in sync

    SpriteRenderer sr;
    float offset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (randomStart) offset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        int i = (int)((Time.time + offset) * fps) % frames.Length;
        sr.sprite = frames[i];
    }
}