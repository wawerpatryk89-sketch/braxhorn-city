using System;
using UnityEditor;

// Runs after the shared private-asset importer. Only camp source assets differ.
public sealed class BraxObozImport : AssetPostprocessor
{
    const string Root = "Assets/Source/Prywatne/Oboz/";
    public override int GetPostprocessOrder() => 100;
    public override uint GetVersion() => 7;
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
        // Copies generated from foliage alpha-mask sources must not fall back
        // to uncompressed Android RGBA. Keep original source and alpha cutoff,
        // but import distant branches at 2K / ASTC 6x6. Four-view billboard
        // atlases keep original 8K source; distant Android LOD uses a 4K copy.
        const string generatedFoliage = "Assets/PlayableGenerated/Oboz/Vegetation/";
        if (assetPath.StartsWith(generatedFoliage, StringComparison.Ordinal))
        {
            string name = assetPath.Substring(generatedFoliage.Length);
            bool isLeaf = name.StartsWith("Leaf-", StringComparison.Ordinal);
            bool isBillboard = name.StartsWith("Billboard-", StringComparison.Ordinal);
            if (isLeaf || isBillboard)
            {
                var tex = assetImporter as TextureImporter;
                if (tex == null) throw new InvalidOperationException("Brak importera maski listow obozu");
                tex.textureShape = TextureImporterShape.Texture2D;
                tex.textureType = TextureImporterType.Default;
                tex.sRGBTexture = true;
                tex.alphaIsTransparency = true;
                tex.mipMapsPreserveCoverage = true;
                tex.mipmapEnabled = true;
                tex.isReadable = false;
                tex.maxTextureSize = isBillboard ? 8192 : 4096;
                tex.textureCompression = TextureImporterCompression.CompressedHQ;
                tex.compressionQuality = 90;
                tex.filterMode = UnityEngine.FilterMode.Trilinear;
                tex.anisoLevel = 4;
                tex.streamingMipmaps = false; // Custom 4-view UVs / GPU instancing.
                var androidFoliage = tex.GetPlatformTextureSettings("Android");
                androidFoliage.name = "Android";
                androidFoliage.overridden = true;
                androidFoliage.maxTextureSize = isBillboard ? 4096 : 2048;
                androidFoliage.format = TextureImporterFormat.ASTC_6x6;
                androidFoliage.compressionQuality = 90;
                tex.SetPlatformTextureSettings(androidFoliage);
                return;
            }
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

        // Skybox HDR Montorfano: zachowaj rzeczywiste 4096×2048 i liniowe HDR.
        // Unity wybiera format HDR zgodny z targetem Android (ASTC_HDR jest
        // opcjonalne sprzętowo); nie wymuszać niepoprawnego ASTC LDR.
        if (assetPath.EndsWith("/OwnerAssets/Panoramy/montorfano_4k.hdr",
            StringComparison.OrdinalIgnoreCase))
        {
            t.textureShape = TextureImporterShape.Texture2D;
            t.textureType = TextureImporterType.Default;
            t.sRGBTexture = false;
            t.mipmapEnabled = true;
            t.filterMode = UnityEngine.FilterMode.Trilinear;
            t.maxTextureSize = 4096;
            t.isReadable = false;
            var hdri = t.GetPlatformTextureSettings("Android");
            hdri.name = "Android";
            hdri.overridden = true;
            hdri.maxTextureSize = 4096;
            hdri.format = TextureImporterFormat.Automatic;
            t.SetPlatformTextureSettings(hdri);
            return;
        }

        // Radiance HDRI 360 zachowuje liniowe dane HDR. Nie wolno przepakować
        // 4K panoramy jako zwykłego LDR ASTC 4x4 tylko dlatego, że jest w OwnerAssets.
        if(assetPath.StartsWith(Root+"OwnerAssets/Panoramy/",StringComparison.Ordinal) &&
            n.EndsWith(".hdr",StringComparison.Ordinal))
        {
            t.textureShape=TextureImporterShape.Texture2D;
            t.textureType=TextureImporterType.Default;
            t.sRGBTexture=false;
            t.mipmapEnabled=true;
            t.streamingMipmaps=false; // Skybox panoramiczny; nie zwykłe UV0
            t.maxTextureSize=4096;
            t.filterMode=UnityEngine.FilterMode.Trilinear;
            t.wrapMode=UnityEngine.TextureWrapMode.Repeat;
            t.isReadable=false;
            var androidHdr=t.GetPlatformTextureSettings("Android");
            androidHdr.name="Android";
            androidHdr.overridden=false; // Unity samo wybiera format zgodny z HDR
            t.SetPlatformTextureSettings(androidHdr);
            return;
        }
        // Nowy prywatny materiał bieżni: dotyczy wyłącznie OwnerAssets,
        // postaci/animacji/importu dotychczasowych źródeł nie zmieniamy.
        if (assetPath.Contains("/OwnerAssets/rubberized_track_2k/", StringComparison.Ordinal))
        {
            bool sourceNormal = n.Contains("_nor_gl_");
            bool sourceColor = n.Contains("_diff_");
            t.textureShape = TextureImporterShape.Texture2D;
            t.textureType = sourceNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.sRGBTexture = sourceColor;
            t.mipmapEnabled = true;
            t.streamingMipmaps = true; // zwykłe UV0 na torze, nie shader trawy
            t.anisoLevel = 8;
            t.filterMode = UnityEngine.FilterMode.Trilinear;
            t.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            t.maxTextureSize = 4096;  // zachowaj bajty źródła (źródło ma 2K)
            t.isReadable = false;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.compressionQuality = 90;
            var mobile = t.GetPlatformTextureSettings("Android");
            mobile.name = "Android";
            mobile.overridden = true;
            mobile.maxTextureSize = 2048;
            mobile.format = sourceNormal ? TextureImporterFormat.ASTC_4x4 :
                TextureImporterFormat.ASTC_6x6;
            mobile.compressionQuality = 90;
            t.SetPlatformTextureSettings(mobile);
            return;
        }

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
        // Android imported resolution. Source 8K is preserved; distant LOD 4K.
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
        // Tiled materials retain a sharp 2K near the camera; ordinary mipmaps
        // and selective streaming provide lower-detail versions at distance.
        int androidMax = billboard ? 4096 :
            tiled || otherCharacter || premiumSurface || vegetationMap ? 2048 : 4096;
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
        // All eligible 2K characters may release their highest mip in the distance.
        BraxObozMaterials.ConfigureStreaming(t, assetPath);
    }
}

