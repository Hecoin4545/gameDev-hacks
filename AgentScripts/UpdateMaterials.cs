using UnityEngine;
using UnityEditor;

public class UpdateMaterials
{
    public static void Run()
    {
        string[] paths = { "Assets/Materials/SafeTile.mat", "Assets/Materials/EndPlatform.mat" };
        Color[] colors = { new Color(0.15f,0.85f,0.35f), new Color(1f,0.85f,0.2f) };
        for (int i = 0; i < paths.Length; i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
            if (mat == null) { Debug.LogWarning("not found " + paths[i]); continue; }
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", colors[i] * 1.5f);
            EditorUtility.SetDirty(mat);
        }
        // unsafe tile slightly darker
        var um = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/UnsafeTile.mat");
        if (um != null) { um.DisableKeyword("_EMISSION"); EditorUtility.SetDirty(um); }
        AssetDatabase.SaveAssets();
        Debug.Log("Materials updated");
    }
}
