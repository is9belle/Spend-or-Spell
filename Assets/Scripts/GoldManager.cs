using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;
    public int gold = 20;

    void Awake()
    {
        Instance = this;
    }

    public bool TrySpend(int cost)
    {
        if (gold < cost) return false;
        gold -= cost;
        return true;
    }

    public void Add(int amount)
    {
        gold += amount;
    }

    GUIStyle style;

void OnGUI() // temporary display until we build the real UI
{
    if (style == null)
    {
        style = new GUIStyle(GUI.skin.label);
        style.fontSize = 32;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;
    }
    GUI.Label(new Rect(10, 10, 400, 50), "Gold: " + gold, style);
}
}