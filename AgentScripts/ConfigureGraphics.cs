using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class ConfigureGraphics
{
    public static void Run()
    {
        var assetPath = "Assets/Settings/PC_RPAsset.asset";
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        if (rp == null) { Debug.LogError("RP asset not found"); return; }

        rp.renderScale = 1.0f;
        rp.msaaSampleCount = 4;
        rp.shadowDistance = 80f;
        rp.shadowCascadeCount = 4;
        rp.mainLightShadowmapResolution = 2048;
        rp.additionalLightsShadowmapResolution = 1024;
        EditorUtility.SetDirty(rp);

        // Quality settings
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
        QualitySettings.shadowDistance = 80f;
        QualitySettings.lodBias = 1.5f;
        QualitySettings.maximumLODLevel = 0;

        AssetDatabase.SaveAssets();
        Debug.Log("Graphics configured: MSAA 4x, renderScale 1.0, shadows high, anisotropic forced");
    }
}
