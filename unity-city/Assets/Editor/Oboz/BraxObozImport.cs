using System;
using UnityEditor;

// Runs after the shared private-asset importer. Only camp source assets differ.
public sealed class BraxObozImport : AssetPostprocessor
{
    const string Root = "Assets/Source/Prywatne/Oboz/";
    public override int GetPostprocessOrder() => 100;
    public override uint GetVersion() => 5;
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
        if (!(assetImporter is ModelImporter m)) return;
        if (!assetPath.StartsWith(Root + "characters/", StringComparison.Ordinal) &&
            !assetPath.StartsWith(Root + "animations/", StringComparison.Ordinal)) return;
        m.animationType = ModelImporterAnimationType.Human;
        m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        m.optimizeGameObjects = false;
        m.useFileScale = true; m.globalScale = 1f;
        m.importCameras = false; m.importLights = false;
        m.materialImportMode = ModelImporterMaterialImportMode.None;
        m.importBlendShapes = true; m.isReadable = true;
        m.importTangents = ModelImporterTangents.CalculateMikk;
        m.importAnimation = assetPath.StartsWith(Root + "animations/", StringComparison.Ordinal);
        m.resampleCurves = true;
        m.animationCompression = ModelImporterAnimationCompression.Off;
    }
    void OnPreprocessTexture()
    {
        // Generated, verified pilot PBR packs are linear data textures; never
        // run the private GLB/character importer over these new outputs.
        if (assetPath.StartsWith("Assets/PlayableGenerated/Oboz/PackedPbr/", StringComparison.Ordinal))
        {
            if (assetImporter is TextureImporter packed)
                BraxObozPbrPack.ConfigureImporter(packed);
            return;
        }
        // Public PBR maps need the same explicit 2D shape on the first import
        // and after restoring old metadata. Do not rely on inferred defaults.
        if (assetPath.StartsWith("Assets/Source/Public/Oboz/", StringComparison.Ordinal))
        {
            if (assetImporter is TextureImporter publicMap)
                BraxObozMaterials.ConfigurePublicMap(publicMap, System.IO.Path.GetFileNameWithoutExtension(assetPath));
            return;
        }
        if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
        if (!(assetImporter is TextureImporter t)) return;
        string n = System.IO.Path.GetFileName(assetPath).ToLowerInvariant();
        bool normal = n.Contains("normal"), color = n.Contains("color") || n.Contains("diff");
        if (assetPath.Contains("/environment/forest-ground/")) { color = n == "image_0.jpg"; normal = n == "image_2.jpg"; }
        // glTFast reads XYZ normals from ordinary data textures, not Unity's
        // packed NormalMap convention. External GLB maps keep their source bytes.
        bool glbMap = assetPath.Contains("_textures/");
        if (glbMap) { normal = false; color = n.Contains("_color"); }
        bool surface = assetPath.Contains("/environment/materials/") || assetPath.Contains("/environment/forest-ground/");
        bool vegetation = assetPath.Contains("/environment/vegetation/");
        bool billboard = assetPath.Contains("/environment/vegetation/billboards/");
        // Mobile quality policy. Source pixels stay unchanged; restrict only
        // Android imported resolution. Preserve full-size 4-view billboards.
        bool hero = assetPath.StartsWith(Root + "characters/monika/", StringComparison.Ordinal);
        bool otherCharacter = assetPath.StartsWith(Root + "characters/", StringComparison.Ordinal) && !hero;
        bool tiledGravel = assetPath.StartsWith(Root + "environment/materials/gravel/", StringComparison.Ordinal);
        bool forestFloor = assetPath.StartsWith(Root + "environment/forest-ground/", StringComparison.Ordinal);
        bool tiled = tiledGravel || forestFloor;
        bool premiumSurface = assetPath.StartsWith(Root + "environment/materials/floor/", StringComparison.Ordinal) ||
            assetPath.StartsWith(Root + "environment/materials/wood/", StringComparison.Ordinal) ||
            assetPath.StartsWith(Root + "environment/materials/metal/", StringComparison.Ordinal) ||
            assetPath.StartsWith(Root + "environment/materials/towel/", StringComparison.Ordinal);
        bool vegetationMap = assetPath.StartsWith(Root + "environment/vegetation/vegetation_textures/", StringComparison.Ordinal);
        // Unrecognized camp assets (bunker, gym, bus) retain their existing policy.
        int editorMax = billboard ? 8192 : otherCharacter ? 2048 : 4096;
        int androidMax = billboard ? 8192 : tiled ? 1024 :
            otherCharacter || premiumSurface || vegetationMap ? 2048 : 4096;
        bool background = tiled || vegetationMap;
        var androidFormat = billboard || vegetationMap || tiled && !normal
            ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
        t.textureShape = TextureImporterShape.Texture2D;
        t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        t.sRGBTexture = color; t.mipmapEnabled = true; t.anisoLevel = 8;
        t.maxTextureSize = editorMax; t.textureCompression = TextureImporterCompression.CompressedHQ;
        t.compressionQuality = 90; t.isReadable = false;
        t.filterMode = UnityEngine.FilterMode.Trilinear;
        if (surface) t.wrapMode = UnityEngine.TextureWrapMode.Repeat;
        if (vegetation && color) t.alphaIsTransparency = true;
        var a = t.GetPlatformTextureSettings("Android");
        a.name = "Android"; a.overridden = true; a.maxTextureSize = androidMax;
        a.format = androidFormat;
        a.compressionQuality = 90;
        t.SetPlatformTextureSettings(a);
        // Monika's 4K PBR remains available close to the camera.
        // Six 2K characters are not streamed below their requested 2K level.
        BraxObozMaterials.ConfigureStreaming(t, assetPath);
    }
}

