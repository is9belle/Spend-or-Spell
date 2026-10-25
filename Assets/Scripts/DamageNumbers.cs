using UnityEngine;
using System.Collections.Generic;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance;

    public float lifetime = 0.8f;
    public float riseSpeed = 1.5f; // world units per second
    public int fontSize = 30;

    class Popup
    {
        public Vector3 pos;
        public int amount;
        public float born;
    }

    List<Popup> popups = new List<Popup>();
    GUIStyle style;

    void Awake()
    {
        Instance = this;
    }

    public static void Spawn(Vector3 worldPos, int amount)
    {
        if (Instance == null) return;
        Instance.popups.Add(new Popup
        {
            pos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0f),
            amount = amount,
            born = Time.time
        });
    }

    void OnGUI() // temporary, same approach as the other HUD pieces
    {
        if (Event.current.type != EventType.Repaint) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
        style.fontSize = fontSize;

        Camera cam = Camera.main;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            Popup p = popups[i];
            float age = Time.time - p.born;
            if (age > lifetime)
            {
                popups.RemoveAt(i);
                continue;
            }

            Vector3 world = p.pos + Vector3.up * riseSpeed * age;
            Vector3 sp = cam.WorldToScreenPoint(world);
            float alpha = 1f - age / lifetime;
            Rect r = new Rect(sp.x - 50f, Screen.height - sp.y - 20f, 100f, 40f);

            style.normal.textColor = new Color(0f, 0f, 0f, alpha);   // outline for readability
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), p.amount.ToString(), style);
            style.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(r, p.amount.ToString(), style);
        }
    }
}