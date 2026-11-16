using System;
using UnityEditor;

public sealed class BraxObsadaImport : AssetPostprocessor
{
    public const string Katalog = "Assets/Source/ObsadaOsiedla/";
    public const string KatalogBaru = "Assets/Source/Prywatne/BarOsiedla/";
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog, StringComparison.Ordinal) && !assetPath.StartsWith(KatalogBaru, StringComparison.Ordinal)) return;
        var model = (ModelImporter)assetImporter;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.importAnimation = true;
        model.importBlendShapes = true;
        model.generateSecondaryUV = false;
        model.swapUVChannels = false;
        model.importNormals = ModelImporterNormals.Import;
        model.importTangents = ModelImporterTangents.CalculateMikk;
        model.meshCompression = ModelImporterMeshCompression.Off;
        model.animationCompression = ModelImporterAnimationCompression.Off;
        model.materialImportMode = ModelImporterMaterialImportMode.None;
        model.optimizeGameObjects = false;
        model.preserveHierarchy = true;
        model.addCollider = false;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Katalog, StringComparison.Ordinal)) return;
        var t = (TextureImporter)assetImporter;
        bool normal = assetPath.EndsWith("/Normal.png", StringComparison.Ordinal);
        if (normal) t.textureType = TextureImporterType.NormalMap;
        bool mimika = assetPath.EndsWith("/Mimika.png", StringComparison.Ordinal);
        t.sRGBTexture = mimika || assetPath.EndsWith("/Color.png", StringComparison.Ordinal);
        if (mimika)
        {
            t.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            t.alphaSource = TextureImporterAlphaSource.FromInput;
            t.alphaIsTransparency = true;
        }
        if (assetPath.EndsWith("/MetalSmooth.png", StringComparison.Ordinal))
        {
            t.alphaSource = TextureImporterAlphaSource.FromInput;
            t.alphaIsTransparency = false;
        }
        t.mipmapEnabled = true;
        t.streamingMipmaps = true;
        // Keep the authored resolution ceiling. A 2K source remains 2K and distant mips still stream.
        t.maxTextureSize = mimika ? 1024 : 4096;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = t.maxTextureSize;
        android.format = TextureImporterFormat.ASTC_4x4;
        t.SetPlatformTextureSettings(android);
    }
}
