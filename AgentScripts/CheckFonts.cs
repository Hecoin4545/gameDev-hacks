using UnityEngine;
using TMPro;
using UnityEditor;

public class CheckFonts
{
    public static void Run()
    {
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            if (f == null) continue;
            var atlas = f.atlasTexture;
            var mat = f.material;
            Debug.Log(p + " | atlas=" + (atlas != null ? atlas.name + " " + atlas.width + "x" + atlas.height : "NULL")
                + " | mat=" + (mat != null ? mat.name : "NULL")
                + " | shader=" + (mat != null && mat.shader != null ? mat.shader.name : "NULL")
                + " | mode=" + f.atlasPopulationMode);
        }
        var sh = Shader.Find("TextMeshPro/Distance Field");
        Debug.Log("TMP shader find: " + (sh != null ? sh.name : "NULL"));
        var ss = AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        Debug.Log("TMP settings: " + (ss != null ? "OK" : "NULL"));
    }
}
