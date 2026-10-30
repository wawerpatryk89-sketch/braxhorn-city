using System.IO;
using UnityEditor;
using UnityEngine;

// Ostrzejsze niebo w mieście: to samo kloofendal_48d_partly_cloudy_puresky (Poly Haven, CC0),
// które już było, ale 4096x2048 zamiast 2048x1024. Tylko testowy build Claude.
public static class BraxClaudeSky
{
    const string Sky = "Assets/Source/Claude/PolyHaven/sky/kloofendal_4k.jpg";

    public static void Upgrade()
    {
        if (!BraxClaudeTowerTerrace.Enabled || !File.Exists(Sky) || !RenderSettings.skybox) return;
        AssetDatabase.ImportAsset(Sky, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(Sky);
        imp.textureType = TextureImporterType.Default;
        imp.textureShape = TextureImporterShape.Texture2D;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.isReadable = false;
        imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Trilinear;
        imp.textureCompression = TextureImporterCompression.Compressed;
        imp.maxTextureSize = 4096;
        foreach (var platform in new[] { "WebGL", "Android", "iPhone" })
        {
            var s = imp.GetPlatformTextureSettings(platform);
            s.name = platform; s.overridden = true; s.maxTextureSize = 4096;
            s.format = platform == "WebGL" ? TextureImporterFormat.ETC2_RGB4 : TextureImporterFormat.ASTC_6x6;
            s.compressionQuality = 100;
            imp.SetPlatformTextureSettings(s);
        }
        imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Sky);
        if (!tex) { Debug.LogError("BRAXHORN_CLAUDE_SKY import failed"); return; }
        RenderSettings.skybox.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(RenderSettings.skybox);
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_CLAUDE_SKY kloofendal 4096x2048 (było 2048x1024)");
    }
}
