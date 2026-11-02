using System.IO;
using UnityEditor;

// Ustawienia importu prywatnych zasobów właściciela (Assets/Source/Prywatne — odszyfrowane w CI przez
// unity-tools/claude/prywatne.py, poza gitem). Ustawiane PRZED pierwszym importem (bez ponownego importu 30 MB FBX):
//   patryk/*.fbx  — Humanoid (awatar z modelu, kontrola w BRAXHORN_PATRYK), bez materiałów i animacji z pliku;
//   ruchy/**.fbx  — Legacy, krzywe bez kompresji (źródło przenoszenia na kości Patryka, BraxClaudeOsiedlePatryk);
//   *.png         — najwyżej 2K (telefon), mipmapy, kompresja wysokiej jakości; normalne i ORM liniowo.
public sealed class BraxPrywatneImport : AssetPostprocessor
{
    const string Katalog = "Assets/Source/Prywatne/";

    public override uint GetVersion() { return 1; }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        var mi = assetImporter as ModelImporter;
        if (mi == null) return;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.importCameras = false; mi.importLights = false; mi.importBlendShapes = false; mi.importVisibility = false;
        mi.useFileScale = true; mi.globalScale = 1f;
        if (assetPath.StartsWith(Katalog + "patryk/"))
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.importAnimation = false;
            mi.optimizeGameObjects = false;
            mi.isReadable = false;
        }
        else
        {
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.resampleCurves = true;
            mi.animationCompression = ModelImporterAnimationCompression.Off;
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Katalog)) return;
        var ti = assetImporter as TextureImporter;
        if (ti == null) return;
        string n = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        ti.maxTextureSize = 2048; ti.mipmapEnabled = true; ti.anisoLevel = 8;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        if (n.Contains("normal")) { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; }
        else if (n.Contains("orm") || n.Contains("rough") || n.Contains("metal")) { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = false; }
        else { ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; }
        var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");   // postać z bliska — jak tekstury Braxa
        if (ign != null && ign.CanWrite) ign.SetValue(ti, true);
    }
}
