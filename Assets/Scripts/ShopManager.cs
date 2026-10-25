using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    class Card
    {
        public string title;
        public string description;
        public int cost;
        public System.Action apply;
        public bool sold;
    }

    public PlayerCaster caster;
    public PlayerController controller;
    public PlayerHealth health;

    public int cardsShown = 3;
    public float priceScale = 1f;                  // multiplies every card price
    [Range(0f, 1f)] public float interestRate = 0.1f; // 10% of the gold you hold
    public int interestCap = 15;                   // max interest per wave

    bool open;
    List<Card> cards = new List<Card>();
    GUIStyle titleStyle, bodyStyle, cardTitleStyle, buttonStyle;

    public bool IsOpen { get { return open; } }

    void Start()
    {
        if (caster == null || controller == null || health == null)
            Debug.LogError("ShopManager: assign Caster, Controller, and Health (drag Player into each slot).");
    }

    int Price(int basePrice)
    {
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * priceScale));
    }

    List<Card> BuildPool()
    {
        var pool = new List<Card>();
        pool.Add(new Card { title = "Sharper Spells", description = "+20% spell damage", cost = Price(8),
            apply = () => caster.damageMultiplier *= 1.2f });
        pool.Add(new Card { title = "Quick Cast", description = "Cast 20% faster", cost = Price(6),
            apply = () => caster.cooldownMultiplier *= 0.8f });
        pool.Add(new Card { title = "Swift Boots", description = "Move 15% faster", cost = Price(5),
            apply = () => controller.speed *= 1.15f });
        if (!health.IsFull())
            pool.Add(new Card { title = "Restore Life", description = "Get 1 life back", cost = Price(10),
                apply = () => health.Heal() });
        if (!caster.IsUnlocked(SpellType.Frost))
            pool.Add(new Card { title = "Learn Frost Nova", description = "Blast and slow nearby enemies", cost = Price(12),
                apply = () => caster.Unlock(SpellType.Frost) });
        if (caster.IsUnlocked(SpellType.Frost))
            pool.Add(new Card { title = "Wider Nova", description = "Frost Nova reaches farther", cost = Price(9),
                apply = () => caster.novaRadius += 1f });
        return pool;
    }

    public void Open()
    {
        var pool = BuildPool();
        cards.Clear();
        while (cards.Count < cardsShown && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            cards.Add(pool[i]);
            pool.RemoveAt(i);
        }
        open = true;
        Time.timeScale = 0f; // pauses the game while shopping
    }

    int PreviewInterest()
    {
        int gold = GoldManager.Instance.gold;
        return Mathf.Min(Mathf.FloorToInt(gold * interestRate), interestCap);
    }

    void Close(int interest)
    {
        GoldManager.Instance.Add(interest);
        open = false;
        Time.timeScale = 1f;
        WaveManager.Instance.StartNextWave();
    }

    void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter, wordWrap = true };
        bodyStyle.normal.textColor = Color.white;
        cardTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        cardTitleStyle.normal.textColor = Color.yellow;
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
    }

    void OnGUI() // temporary UI until we build the pixel version
    {
        if (!open) return;
        EnsureStyles();

        float w = Screen.width;
        GoldManager gm = GoldManager.Instance;
        int nextWave = WaveManager.Instance.Wave + 1;

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, w, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, 30, w, 60), "Spellbook", titleStyle);
        GUI.Label(new Rect(0, 95, w, 40), "Your gold: " + gm.gold, bodyStyle);

        float cardW = 260f, cardH = 200f, gap = 30f;
        float startX = (w - (cards.Count * cardW + (cards.Count - 1) * gap)) / 2f;
        for (int i = 0; i < cards.Count; i++)
        {
            Card c = cards[i];
            Rect r = new Rect(startX + i * (cardW + gap), 150, cardW, cardH);
            GUI.Box(r, "");
            GUI.Label(new Rect(r.x + 10, r.y + 10, cardW - 20, 40), c.title, cardTitleStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 60, cardW - 20, 60), c.description, bodyStyle);

            if (c.sold)
            {
                GUI.Label(new Rect(r.x, r.y + cardH - 55, cardW, 40), "SOLD", cardTitleStyle);
            }
            else
            {
                GUI.enabled = gm.gold >= c.cost;
                if (GUI.Button(new Rect(r.x + 20, r.y + cardH - 60, cardW - 40, 44),
                               "Buy (" + c.cost + " gold)", buttonStyle))
                {
                    if (gm.TrySpend(c.cost))
                    {
                        c.apply();
                        c.sold = true;
                    }
                }
                GUI.enabled = true;
            }
        }

        int interest = PreviewInterest();
        float y = 150 + cardH + 30;
        GUI.Label(new Rect(0, y, w, 40),
            "Interest: +" + interest + " (" + Mathf.RoundToInt(interestRate * 100f) + "% of your gold, max " + interestCap + ")",
            bodyStyle);
        GUI.Label(new Rect(0, y + 35, w, 40), "Gold after interest: " + (gm.gold + interest), bodyStyle);

        string label = interest > 0
            ? "Collect +" + interest + " interest & start wave " + nextWave
            : "Start wave " + nextWave;
        if (GUI.Button(new Rect(w / 2f - 220, y + 85, 440, 70), label, buttonStyle))
            Close(interest);
    }
}