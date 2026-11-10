using System.IO;
using UnityEditor;

// Ustawienia importu prywatnych zasobów właściciela (Assets/Source/Prywatne — odszyfrowane w CI przez
// unity-tools/claude/prywatne.py, poza gitem). Ustawiane PRZED pierwszym importem (bez ponownego importu 30 MB FBX):
//   patryk/*.fbx  — Humanoid (awatar z modelu, kontrola w BRAXHORN_PATRYK), bez materiałów i animacji z pliku;
//   ruchy/patryk_owner/*.fbx — Generic bez awatara (10.11: ruchy właściciela zrobione pod Patryka idą PROSTO na jego kości,
//                  bez konwersji Humanoid — BraxClaudePatrykBezposrednio), krzywe bez kompresji;
//   ruchy/**.fbx  — pozostałe (Brax): Humanoid, krzywe bez kompresji (przenoszenie przez HumanPoseHandler);
//   patryk/*.png  — Color/Normal 4K, ORM 2K, Android ASTC 4×4, jakość 50; normalne i ORM liniowo.
//   pozostałe png — najwyżej 2K, kompresja jakości 50.
public sealed class BraxPrywatneImport : AssetPostprocessor
{
    const string Katalog = "Assets/Source/Prywatne/";

    public override uint GetVersion() { return 6; }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        if (assetPath.StartsWith(Katalog + "Osiedle/")) return;   // modele osiedla ze Sketchfaba (glTF) — własne ustawienia
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
        else if (assetPath.StartsWith(Katalog + "ruchy/patryk_owner/"))
        {
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            mi.optimizeGameObjects = false;
            mi.importAnimation = true;
            mi.resampleCurves = true;
            mi.animationCompression = ModelImporterAnimationCompression.Off;
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
        if (assetPath.StartsWith(Katalog + "Osiedle/")) return;   // BraxOsiedleAssetyTekstury
        var ti = assetImporter as TextureImporter;
        if (ti == null) return;
        string n = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 8;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.compressionQuality = 50;
        if(assetPath.StartsWith(Katalog+"patryk/"))
        {
            ti.maxTextureSize = n.Contains("basecolor") || n.Contains("normal") ? 4096 : 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.crunchedCompression = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.streamingMipmaps = false;
            var a = ti.GetPlatformTextureSettings("Android");
            a.overridden = true; a.maxTextureSize = ti.maxTextureSize; a.format = TextureImporterFormat.ASTC_4x4; a.compressionQuality = 50;
            ti.SetPlatformTextureSettings(a);
        }
        if (n.Contains("normal")) { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; }
        else if (n.Contains("orm") || n.Contains("rough") || n.Contains("metal")) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; }
        else { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; }
        var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");   // postać z bliska — jak tekstury Braxa
        if (ign != null && ign.CanWrite) ign.SetValue(ti, true);
    }
}
