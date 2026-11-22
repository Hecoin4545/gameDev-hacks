using UnityEngine;
using TMPro;
using UnityEditor;

public class FixFont
{
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter-Regular SDF.asset");
        if (font == null) { Debug.LogError("font missing"); return; }
        var shader = Shader.Find("TextMeshPro/Distance Field");
        if (shader == null) { Debug.LogError("TMP shader missing"); return; }

        var mat = font.material;
        if (mat == null) { Debug.LogError("font material missing"); return; }
        mat.shader = shader;
        if (font.atlasTexture != null)
        {
            mat.SetTexture("_MainTex", font.atlasTexture);
            mat.SetFloat("_TextureWidth", font.atlasTexture.width);
            mat.SetFloat("_TextureHeight", font.atlasTexture.height);
        }
        mat.SetFloat("_GradientScale", 10f);
        mat.SetFloat("_ScaleRatioA", 1f);
        EditorUtility.SetDirty(mat);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        Debug.Log("Inter font shader set to: " + mat.shader.name);
    }
}
