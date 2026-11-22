using System.Collections;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int row;
    public int column;
    public bool isSafe;
    public int stepIndex = -1;
    public Renderer rend;

    // The 3D tiles deliberately stay neutral/black. Only the map reveals the safe path.
    public Color neutralColor = new Color(0.055f, 0.062f, 0.068f);
    public Color completedColor = new Color(0.085f, 0.092f, 0.098f);

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
        block.SetColor("_EmissionColor", Color.black);
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
        float duration = 0.28f;
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
                block.SetColor("_BaseColor", Color.Lerp(neutralColor, new Color(0.55f, 0.03f, 0.03f), pulse));
                block.SetColor("_EmissionColor", new Color(0.7f, 0.02f, 0.02f) * pulse * 2f);
                rend.SetPropertyBlock(block);
            }
            yield return null;
        }
        ApplyColor(false);
    }
}
