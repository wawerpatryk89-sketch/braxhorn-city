using System.IO;
using UnityEditor;

// Ustawienia importu prywatnych zasobów właściciela (Assets/Source/Prywatne — odszyfrowane w CI przez
// unity-tools/claude/prywatne.py, poza gitem). Ustawiane PRZED pierwszym importem (bez ponownego importu 30 MB FBX):
//   patryk/*.fbx  — Humanoid (awatar z modelu, kontrola w BRAXHORN_PATRYK), bez materiałów i animacji z pliku;
//   ruchy/**.fbx  — Humanoid, krzywe bez kompresji (źródło przenoszenia na kości Patryka, BraxClaudeOsiedlePatryk);
//   patryk/*.png  — Color/Normal/ORM 4K, Android ASTC 4×4, jakość 100; normalne i ORM liniowo.
//   pozostałe png — najwyżej 2K, kompresja jakości 50.
//   Postacie/patryk/ — globalny Patryk z 13.11 (23.11, restore_patryk_globalny.py): Patryk.fbx Humanoid jak wyżej,
//                  Ruchy/*.fbx Generic bez awatara (prosto na jego kości), mapy 2K: kolor i normalne ASTC 4×4, ORM 6×6.
public sealed class BraxChampionsPatrykImport : AssetPostprocessor
{
    const string Katalog = "Assets/Source/Mistrzowie/Player/";

    public override uint GetVersion() { return 7; }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        if (assetPath.StartsWith(Katalog + "Osiedle/") || assetPath.StartsWith(Katalog + "Miasto/")) return;   // modele osiedla ze Sketchfaba (glTF) — własne ustawienia
        var mi = assetImporter as ModelImporter;
        if (mi == null) return;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false; mi.importVisibility = false;
        mi.useFileScale = true; mi.globalScale = 1f;
        if (assetPath.StartsWith(Katalog + "Postacie/patryk/Ruchy/"))
        {
            // 23.11: the global Patryk's own motions go straight onto his bones (BraxChampionsPatrykGlobalny), as on Osiedle
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            mi.optimizeGameObjects = false;
            mi.importAnimation = true;
            mi.resampleCurves = true;
            mi.animationCompression = ModelImporterAnimationCompression.Off;
        }
        else if (assetPath.StartsWith(Katalog + "patryk/") || assetPath == Katalog + "Postacie/patryk/Patryk.fbx")
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
        if (assetPath.StartsWith(Katalog + "Postacie/patryk/"))
        {
            // the global Patryk's maps are 2K at the source: kept 2K; colour and normals ASTC 4x4, ORM 6x6 (ZASADY 3.2)
            bool orm = n.Contains("orm");
            ti.maxTextureSize = 2048; ti.crunchedCompression = false; ti.npotScale = TextureImporterNPOTScale.None;
            ti.streamingMipmaps = true; ti.streamingMipmapsPriority = 100;   // ZASADY A.5: streaming for characters too, the hero first
            var g = ti.GetPlatformTextureSettings("Android");
            g.overridden = true; g.maxTextureSize = 2048; g.format = orm ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4; g.compressionQuality = 50;
            ti.SetPlatformTextureSettings(g);
        }
        else if(assetPath.StartsWith(Katalog+"patryk/"))
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

