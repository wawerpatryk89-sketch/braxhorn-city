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
        // Nie przerywaj importu tekstury przez wyjątek: ostrzeżenie i dalej.
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
                if (tex == null)
                {
                    UnityEngine.Debug.LogWarning("BRAXHORN_OBOZ_IMPORT: brak importera maski liści: "+assetPath);
                    return;
                }
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
                androidFoliage.maxTextureSize = 2048; // odległe drzewa na Androidzie, nie 4K
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
        // Właścicielskie murale, szyld i tekstura FBX to grafiki COLOR,
        // nawet jeśli nazwa JPEG nie zawiera słowa 'Color'. Żadnej zmiany
        // pliku źródłowego; poprawne sRGB jest konieczne, by nie zmienić barw.
        bool ownerArtwork = assetPath.StartsWith(Root+"OwnerAssets/Murale/",StringComparison.Ordinal);
        bool ownerShelterMap = assetPath.StartsWith(Root+"OwnerAssets/Przystanek/",StringComparison.Ordinal);
        if(ownerArtwork||ownerShelterMap)
        {
            t.textureShape=TextureImporterShape.Texture2D;
            t.textureType=TextureImporterType.Default;
            t.sRGBTexture=true;
            t.mipmapEnabled=true;
            t.filterMode=UnityEngine.FilterMode.Trilinear;
            t.anisoLevel=8;
            t.maxTextureSize=4096;
            t.isReadable=false;
            t.textureCompression=TextureImporterCompression.CompressedHQ;
            t.compressionQuality=90;
            var androidArtwork=t.GetPlatformTextureSettings("Android");
            androidArtwork.name="Android";
            androidArtwork.overridden=true;
            androidArtwork.maxTextureSize=4096; // nie obniżaj oryginalnej jakości
            androidArtwork.format=TextureImporterFormat.ASTC_4x4;
            androidArtwork.compressionQuality=90;
            t.SetPlatformTextureSettings(androidArtwork);
            return;
        }

        // Do przyszłego pełnego importu materiałów 2K/4K trawy, błota i płytek.
        // Mapy normalnych w Poly Haven używają '_nor_gl_', nie '_normal'.
        // Trzymaj oryginalne 4K, na Androidzie także bez ukrytej redukcji.
        if(assetPath.StartsWith(Root+"OwnerAssets/",StringComparison.Ordinal) &&
            assetPath.Contains("/textures/",StringComparison.Ordinal) &&
            !assetPath.Contains("/OwnerAssets/rubberized_track_2k/",StringComparison.Ordinal))
        {
            bool sourceNormal=n.Contains("_nor_gl_");
            bool sourceColor=n.Contains("_diff_");
            bool is4k=assetPath.Contains("_4k/",StringComparison.OrdinalIgnoreCase) ||
                n.Contains("_4k.");
            int limit=is4k?4096:2048;
            t.textureShape=TextureImporterShape.Texture2D;
            t.textureType=sourceNormal?TextureImporterType.NormalMap:TextureImporterType.Default;
            t.sRGBTexture=sourceColor;
            t.mipmapEnabled=true;
            t.streamingMipmaps=false; // opcja dopiero po teście UV/shadera
            t.anisoLevel=8;
            t.filterMode=UnityEngine.FilterMode.Trilinear;
            t.wrapMode=UnityEngine.TextureWrapMode.Repeat;
            t.maxTextureSize=limit;
            t.isReadable=false;
            t.textureCompression=TextureImporterCompression.CompressedHQ;
            t.compressionQuality=90;
            var mobileSource=t.GetPlatformTextureSettings("Android");
            mobileSource.name="Android";
            mobileSource.overridden=true;
            mobileSource.maxTextureSize=limit;
            mobileSource.format=sourceNormal?TextureImporterFormat.ASTC_4x4:
                TextureImporterFormat.ASTC_6x6;
            mobileSource.compressionQuality=90;
            t.SetPlatformTextureSettings(mobileSource);
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
        // Pozostałe historyczne zasoby obozu zachowują dotychczasowy profil importu.
        int editorMax = billboard ? 8192 : otherCharacter ? 2048 : 4096;
        // Tiled materials retain a sharp 2K near the camera; ordinary mipmaps
        // and selective streaming provide lower-detail versions at distance.
        int androidMax = hero ? 4096 : 2048; // mapa roślinności/świata 2K; 4K bohater
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

