using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int row;
    public int column;
    public bool isSafe;
    public int stepIndex = -1;
    public Renderer rend;

    public Color neutralColor = new Color(0.055f, 0.062f, 0.068f);
    public Color completedColor = new Color(0.085f, 0.092f, 0.098f);
    public Color completedEmission = Color.black;

    private MaterialPropertyBlock block;

    void Awake()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        ApplyColor(false);
    }

    public void ApplyColor(bool completed)
    {
        if (rend == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor("_BaseColor", completed ? completedColor : neutralColor);
        block.SetColor("_EmissionColor", completed ? completedEmission : Color.black);
        rend.SetPropertyBlock(block);
    }

    /// <summary>Small tactile bounce when the tile is correctly landed on.</summary>
    public void PlaySuccessFeedback()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(BounceRoutine());
    }

    /// <summary>Clear red danger flash before the restart.</summary>
    public void FlashDanger()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(DangerRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        ApplyColor(true);
        float duration = 0.32f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = Mathf.Sin((time / duration) * Mathf.PI);
            float s = 1f - k * 0.07f;
            transform.localScale = new Vector3(s, 1f, s);
            yield return null;
        }
        transform.localScale = Vector3.one;
        ApplyColor(true);
    }

    private IEnumerator DangerRoutine()
    {
        float duration = 0.75f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float pulse = Mathf.Abs(Mathf.Sin(time * 22f));
            if (rend != null)
            {
                rend.GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(neutralColor, new Color(0.65f, 0.04f, 0.04f), pulse));
                block.SetColor("_EmissionColor", new Color(0.9f, 0.04f, 0.04f) * pulse * 2f);
                rend.SetPropertyBlock(block);
            }
            yield return null;
        }
        ApplyColor(false);
    }
}
