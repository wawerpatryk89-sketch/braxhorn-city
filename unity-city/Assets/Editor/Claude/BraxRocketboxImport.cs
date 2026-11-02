using UnityEditor;
using UnityEngine;

// IMPORT LUDZI ROCKETBOX (build 14, zlecenie „ludzie”; pliki pobiera unity-tools/claude/fetch_rocketbox.py do
// Assets/Source/Rocketbox). Postacie i animacje paczki mają TEN SAM szkielet 3ds Max Biped (Bip01, 81 kości; dwa warianty
// proporcji: męski i żeński — animacje m_* i f_* zrobione dokładnie na nich). Dlatego import LEGACY (komponent Animation):
// klipy grają na postaciach bez przepinania (retargetu) — ścieżki kości identyczne, długości kości identyczne → zero
// deformacji. Humanoid odpada: retarget Mecanim przelicza pozę przez mięśnie (i gubi kości twarzy, palce bywają krzywe),
// a tu nie ma czego przeliczać.
//   • modele: bez animacji, bez kształtów mimiki, materiały z FBX tylko jako nazwy podsiatek (tekstury przypisuje
//     generator — BraxClaudeOsiedleLudzie), skala z pliku (cm → m);
//   • animacje: Legacy, redukcja klatek (błąd 0.4°, 0.4 %) + kompresja (poza chodem/biegiem), pętla;
//   • tekstury: kolor 1024, normalne 512 (NormalMap), włosy (opacity) z alfą; kompresja platformy (ETC2/ASTC, DXT).
public sealed class BraxRocketboxImport : AssetPostprocessor
{
    public const string Root = "Assets/Source/Rocketbox/";

    public override uint GetVersion() { return 3; }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
        var mi = (ModelImporter)assetImporter;
        bool anim = assetPath.Contains("/Animations/");
        mi.globalScale = 1f; mi.useFileScale = true;
        mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false; mi.importVisibility = false;
        mi.importConstraints = false;
        mi.isReadable = false;
        mi.meshCompression = ModelImporterMeshCompression.Off;
        mi.animationType = ModelImporterAnimationType.Legacy;
        if (anim)
        {
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.resampleCurves = true;
            // chód i bieg (krótkie, ~1 s): bez kompresji — generator czyta krzywe i robi kopię „w miejscu”; reszta (postoje,
            // rozmowy, siedzenie, taniec — 4–47 s, w sumie ~20 min ruchu) z kompresją Unity: kilka razy mniej pamięci telefonu
            string nazwa = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            mi.animationCompression = nazwa.Contains("_walk_") || nazwa.Contains("_run_") ? ModelImporterAnimationCompression.KeyframeReduction : ModelImporterAnimationCompression.KeyframeReductionAndCompression;
            mi.animationRotationError = .4f; mi.animationPositionError = .4f; mi.animationScaleError = .8f;
            mi.animationWrapMode = WrapMode.Loop;
        }
        else
        {
            mi.importAnimation = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.skinWeights = ModelImporterSkinWeights.Standard;   // 4 kości na wierzchołek (jak w paczce)
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
        var t = (TextureImporter)assetImporter;
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        bool normal = n.Contains("_normal"), opacity = n.Contains("opacity") && !normal, body = n.Contains("_body_color");
        t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        t.sRGBTexture = !normal;
        t.mipmapEnabled = true; t.isReadable = false; t.anisoLevel = 2;
        t.alphaSource = opacity || body ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        t.alphaIsTransparency = opacity;      // włosy, rzęsy; w ciele alfa = maska ubrań (dane, nie przezroczystość)
        int max = normal ? 512 : 1024;
        t.maxTextureSize = max; t.textureCompression = TextureImporterCompression.Compressed;
        foreach (var platform in new[] { "Android", "WebGL" })
        {
            var ps = t.GetPlatformTextureSettings(platform);
            ps.name = platform; ps.overridden = true; ps.maxTextureSize = max;
            ps.format = TextureImporterFormat.Automatic; ps.textureCompression = TextureImporterCompression.Compressed;
            ps.compressionQuality = 50;
            t.SetPlatformTextureSettings(ps);
        }
    }
}
