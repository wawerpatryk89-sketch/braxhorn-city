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
        // WAGA APK — zmierzone 16.11 na APK pokoju z pięcioma postaciami (309,6 MB danych):
        //   animacje 195,0 MB (63 %), tekstury 83,3 MB (27 %), siatki 30,6 MB (10 %).
        // Czyli prawie dwie trzecie wagi to animacje, nie tekstury. Pojedyncze ujęcia: KickBall
        // 12,08 MB, DanceHouse 11,16 MB.
        //
        // REDUKCJA KLATEK KLUCZOWYCH zamiast Off. Osobno zmierzone wprost z FBX: przesunięcie
        // i skala KAŻDEJ z 65 kości są STAŁE — 0,00 % zmiany w całym ujęciu (animacja tylko
        // obraca stawy). Przy kompresji Off Unity i tak zapisywało te stałe klatka po klatce,
        // dla wszystkich kości. To jest około 117 MB z tych 195 MB — zapis w kółko tej samej
        // liczby. Redukcja zwija stałą krzywą do jednego klucza, więc znika bez żadnej straty.
        // Tolerancje ciasne (0,2 % zamiast domyślnych 0,5 %), żeby obrotów praktycznie nie ruszać:
        // na stałych krzywych błąd i tak wynosi zero, a ruch ma wyglądać dokładnie tak, jak go
        // właściciel dostarczył.
        model.meshCompression = ModelImporterMeshCompression.Medium;
        model.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        model.animationPositionError = .2f;
        model.animationRotationError = .2f;
        model.animationScaleError = .2f;
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
        // ASTC 6x6 (3,56 bita na piksel) zamiast 4x4 (8 bitów) na mapach ciała: 2,25x lżej.
        // Zmierzone: tekstury to 83,3 MB z 309,6 MB w APK pięciu postaci — schodzą do ~37 MB.
        // Postać ogląda się z kilku metrów, więc różnicy na skórze i ubraniu nie widać.
        // TWARZ (Mimika) ZOSTAJE NA 4x4: tam są zęby, powieki i linia ust na atlasie 1024 —
        // to jedyne miejsce, gdzie blok 6x6 byłoby widać, bo patrzy się na nie z bliska.
        android.format = mimika ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
        t.SetPlatformTextureSettings(android);
    }
}
