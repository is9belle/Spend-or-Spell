using UnityEngine;
using UnityEngine.EventSystems;

// Add this to any Unity UI Button for hover and click sounds.
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData e) { AudioManager.Hover(); }
    public void OnPointerClick(PointerEventData e) { AudioManager.Click(); }
}
