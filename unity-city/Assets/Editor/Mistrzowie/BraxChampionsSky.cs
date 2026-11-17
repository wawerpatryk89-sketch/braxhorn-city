using System.IO;
using UnityEditor;
using UnityEngine;

// Owner 10.11 19:12 (photos from the Hotel Sobczak roof): "Nieba też brak", "I panorama realistyczną HDR 360". The region
// had Unity's procedural atmosphere: a flat gradient and a flat grey-green ground colour beyond the city. Now the sky is a real
// 360 HDR panorama on Unity's Skybox/Panoramic, the original 4K Radiance .hdr (prepare_assets.py, provider size + MD5), kept
// HDR on the phone (ASTC HDR 6x6). Owner 17.11 (the helicopter's flights, a choice of 5 skies at the game's brightness):
// "sunflowers" - Poly Haven CC0 sunflowers_puresky, a deep blue sky with bright cumulus all round (before: kloofendal_48d).
// Its sun (equirect u .600, elevation 43.0 deg, measured in the .hdr; the city's directional light is fixed at about 48)
// is turned onto the region's light (pitch 48, yaw -35): sun azimuth -55.0 deg (atan2 z,x) - HDRI -36.0 deg -> _Rotation
// 19.0. Exposure .89: the sky band 20..45 deg as bright on screen as kloofendal's at 1.18 (the camera is LDR, no HDR).
public static class BraxChampionsSky
{
    public const string Slug = "sunflowers_puresky";
    public const string Source = "Assets/Source/Mistrzowie/Sky/" + Slug + "_4k.hdr";
    public const float Rotation = 19.0f, Exposure = .89f;
    public const string MaterialPath = "Assets/PlayableGenerated/Mistrzowie/Materials/champions_hdr360_sky.mat";

    public static Material Create()
    {
        if (!File.Exists(Source)) { BraxBuildQuality.Warn("DO POPRAWKI CHAMPIONS_SKY_HDR_MISSING " + Source + " (prepare_assets.py SKY_HDRIS)"); return null; }
        var shader = Shader.Find("Skybox/Panoramic");
        if (!shader) { BraxBuildQuality.Warn("DO POPRAWKI CHAMPIONS_SKY_PANORAMIC_SHADER_MISSING"); return null; }
        AssetDatabase.ImportAsset(Source, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(Source);
        if (imp == null) { BraxBuildQuality.Warn("DO POPRAWKI CHAMPIONS_SKY_IMPORTER_MISSING " + Source); return null; }
        imp.textureType = TextureImporterType.Default;
        imp.textureShape = TextureImporterShape.Texture2D;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.sRGBTexture = false;                                   // HDR radiance is linear
        imp.mipmapEnabled = true; imp.isReadable = false;
        imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Trilinear;
        imp.maxTextureSize = 4096;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        foreach (var platform in new[] { "Android", "iPhone" })
        {
            var s = imp.GetPlatformTextureSettings(platform);
            s.name = platform; s.overridden = true; s.maxTextureSize = 4096;
            s.format = TextureImporterFormat.ASTC_HDR_6x6;
            imp.SetPlatformTextureSettings(s);
        }
        var pc = imp.GetPlatformTextureSettings("Standalone");
        pc.name = "Standalone"; pc.overridden = true; pc.maxTextureSize = 4096; pc.format = TextureImporterFormat.BC6H;
        imp.SetPlatformTextureSettings(pc);
        imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        if (!tex) { BraxBuildQuality.Warn("DO POPRAWKI CHAMPIONS_SKY_IMPORT_FAILED " + Source); return null; }
        var m = new Material(shader) { name = "Champions HDR 360 sky | Poly Haven CC0 " + Slug };
        m.SetTexture("_MainTex", tex);
        m.SetFloat("_Mapping", 1f); m.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
        m.SetFloat("_ImageType", 0f);                              // 360 degrees
        m.SetFloat("_Exposure", Exposure);
        m.SetFloat("_Rotation", Rotation);
        var old = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (old) { EditorUtility.CopySerialized(m, old); Object.DestroyImmediate(m); m = old; EditorUtility.SetDirty(m); }
        else AssetDatabase.CreateAsset(m, MaterialPath);
        Debug.Log("CHAMPIONS_SKY_HDR360 " + Slug + " " + tex.width + "x" + tex.height + " rotation=" + Rotation + " exposure=" + Exposure);
        return m;
    }
}
