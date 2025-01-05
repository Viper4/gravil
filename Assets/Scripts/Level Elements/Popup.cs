using UnityEngine;
using TMPro;
using System.Collections;

public class Popup : MonoBehaviour
{
    public bool visible {get; private set;}
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject popupUI;
    [SerializeField] private TextMeshProUGUI popupText;

    public void Show()
    {
        if (visible)
            return;
        visible = true;
        highlight.SetActive(true);
        popupUI.SetActive(true);
    }

    public void Show(string text)
    {
        if (visible) 
            return;
        visible = true;
        highlight.SetActive(true);
        popupUI.SetActive(true);
        popupText.text = text;
    }

    public void Show(string text, float duration)
    {
        StartCoroutine(PopupRoutine(text, duration));
    }

    private IEnumerator PopupRoutine(string text, float duration)
    {
        Show(text);
        yield return new WaitForSeconds(duration);
        Hide();
    }

    public void Hide()
    {
        if (!visible)
            return;
        visible = false;
        highlight.SetActive(false);
        popupUI.SetActive(false);
    }
}
