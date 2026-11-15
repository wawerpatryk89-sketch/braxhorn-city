using System;
using UnityEditor;

public sealed class BraxObsadaImport : AssetPostprocessor
{
    public const string Katalog = "Assets/Source/ObsadaOsiedla/";
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog, StringComparison.Ordinal)) return;
        var model = (ModelImporter)assetImporter;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.importAnimation = true;
        model.importBlendShapes = true;
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
        t.sRGBTexture = assetPath.EndsWith("/Color.png", StringComparison.Ordinal);
        t.mipmapEnabled = true;
        t.streamingMipmaps = true;
        t.maxTextureSize = assetPath.Contains("/patryk/") ? 4096 : 2048;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = t.maxTextureSize;
        android.format = TextureImporterFormat.ASTC_4x4;
        t.SetPlatformTextureSettings(android);
    }
}
