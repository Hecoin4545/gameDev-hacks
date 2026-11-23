using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FixCanvas
{
    public static void Run()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.Log("No canvas"); return; }
        Debug.Log("Canvas scale=" + canvas.transform.localScale + " renderMode=" + canvas.renderMode
            + " active=" + canvas.gameObject.activeInHierarchy + " children=" + canvas.transform.childCount
            + " rect=" + canvas.GetComponent<RectTransform>().rect);

        var top = GameObject.Find("BackdropTop");
        if (top != null) { top.SetActive(false); Debug.Log("Disabled BackdropTop"); }

        var title = GameObject.Find("Title");
        if (title != null)
        {
            var t = title.GetComponent<TextMeshProUGUI>();
            Debug.Log("Title active=" + title.activeInHierarchy + " alpha=" + t.alpha + " color=" + t.color
                + " canvasAlpha=" + t.canvasRenderer.GetAlpha() + " pos=" + title.transform.position
                + " lossyScale=" + title.transform.lossyScale);
        }

        var cards = GameObject.Find("LevelCards");
        if (cards != null) Debug.Log("Cards active=" + cards.activeInHierarchy + " pos=" + cards.transform.position
            + " scale=" + cards.transform.lossyScale);
    }
}
