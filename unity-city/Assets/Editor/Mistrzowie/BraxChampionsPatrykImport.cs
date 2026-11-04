using System.IO;
using UnityEditor;

// Ustawienia importu prywatnych zasobów właściciela (Assets/Source/Prywatne — odszyfrowane w CI przez
// unity-tools/claude/prywatne.py, poza gitem). Ustawiane PRZED pierwszym importem (bez ponownego importu 30 MB FBX):
//   patryk/*.fbx  — Humanoid (awatar z modelu, kontrola w BRAXHORN_PATRYK), bez materiałów i animacji z pliku;
//   ruchy/**.fbx  — Humanoid, krzywe bez kompresji (źródło przenoszenia na kości Patryka, BraxClaudeOsiedlePatryk);
//   patryk/*.png  — Color/Normal/ORM 4K, Android ASTC 4×4, jakość 100; normalne i ORM liniowo.
//   pozostałe png — najwyżej 2K, kompresja jakości 50.
public sealed class BraxChampionsPatrykImport : AssetPostprocessor
{
    const string Katalog = "Assets/Source/Mistrzowie/Player/";

    public override uint GetVersion() { return 6; }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        if (assetPath.StartsWith(Katalog + "Osiedle/") || assetPath.StartsWith(Katalog + "Miasto/")) return;   // modele osiedla ze Sketchfaba (glTF) — własne ustawienia
        var mi = assetImporter as ModelImporter;
        if (mi == null) return;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false; mi.importVisibility = false;
        mi.useFileScale = true; mi.globalScale = 1f;
        if (assetPath.StartsWith(Katalog + "patryk/"))
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = false;
            mi.optimizeGameObjects = false;
            mi.isReadable = true;                                   // pomiar wzrostu z wierzchołków (10 tys. — ~1 MB, bez znaczenia)
            mi.importTangents = ModelImporterTangents.CalculateMikk;  // mapa normalnych w Braxhorn/HeroPremium
        }
        else
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.optimizeGameObjects = false;
            mi.importAnimation = true;
            mi.resampleCurves = true;
            mi.animationCompression = ModelImporterAnimationCompression.Off;
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        if (assetPath.StartsWith(Katalog + "Osiedle/") || assetPath.StartsWith(Katalog + "Miasto/")) return;   // BraxOsiedleAssetyTekstury
        var ti = assetImporter as TextureImporter;
        if (ti == null) return;
        string n = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 8;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.compressionQuality = 50;
        if(assetPath.StartsWith(Katalog+"patryk/"))
        {
            ti.maxTextureSize = 4096; ti.compressionQuality = 100;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.crunchedCompression = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.streamingMipmaps = false;
            var a = ti.GetPlatformTextureSettings("Android");
            a.overridden = true; a.maxTextureSize = ti.maxTextureSize; a.format = TextureImporterFormat.ASTC_4x4; a.compressionQuality = 100;
            ti.SetPlatformTextureSettings(a);
        }
        if (n.Contains("normal")) { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; }
        else if (n.Contains("orm") || n.Contains("rough") || n.Contains("metal")) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; }
        else { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; }
        var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");   // postać z bliska — jak tekstury Braxa
        if (ign != null && ign.CanWrite) ign.SetValue(ti, true);
    }
}

