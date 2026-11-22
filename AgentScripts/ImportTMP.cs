using UnityEditor;
using UnityEngine;

public class ImportTMP
{
    public static void Run()
    {
        const string path = "H:/32bit/Library/PackageCache/com.unity.ugui@e13c0366db78/Package Resources/TMP Essential Resources.unitypackage";
        AssetDatabase.ImportPackage(path, false);
        AssetDatabase.Refresh();
        var settings = AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        Debug.Log("TMP Settings: " + (settings != null ? "OK" : "MISSING"));
    }
}
