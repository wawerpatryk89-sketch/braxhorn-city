using System;
using UnityEditor;

public sealed class BraxObsadaImport : AssetPostprocessor
{
    // NUMER WERSJI IMPORTU. Unity trzyma zaimportowane pliki w pamieci podrecznej i zmiana
    // ustawien w tym pliku SAMA jej nie uniewaznia. Bez podbicia numeru build wzialby obsade
    // z pamieci (klucz library-android-v2-osiedle-...) i waga APK nie zmienilaby sie ani o bajt,
    // a wygladaloby to na brak efektu.
    // 4 — 18.11.2026: osiedle wyrownane do ZASAD (main): sufit 2048, ASTC 6x6 na ciele,
    //     redukcja klatek kluczowych. Numer wyzszy niz 3 z main, zeby nie zderzyl sie z cache.
    public override uint GetVersion() => 4;

    public const string Katalog = "Assets/Source/ObsadaOsiedla/";
    public const string KatalogGlobalny = "Assets/Source/PostacieGlobalne/";
    static bool ModelPostaci(string path) =>
        path.StartsWith(Katalog, StringComparison.Ordinal) || path.StartsWith(KatalogGlobalny, StringComparison.Ordinal);

    public const string KatalogBaru = "Assets/Source/Prywatne/BarOsiedla/";
    void OnPreprocessModel()
    {
        if (!ModelPostaci(assetPath) && !assetPath.StartsWith(KatalogBaru, StringComparison.Ordinal)) return;
        var model = (ModelImporter)assetImporter;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.importAnimation = true;
        model.importBlendShapes = true;
        model.generateSecondaryUV = false;
        model.swapUVChannels = false;
        model.importNormals = ModelImporterNormals.Import;
        model.importTangents = ModelImporterTangents.CalculateMikk;
        // SIATKI BEZ KOMPRESJI — zostaje (wlasciciel 16.11, 18:00: "zero kwadratowych, kanciatych
        // ksztaltow"). Kompresja siatki kwantyzuje pozycje i normalne, a na ciele daje fasetowanie.
        model.meshCompression = ModelImporterMeshCompression.Off;
        // REDUKCJA KLATEK KLUCZOWYCH zamiast Off — to samo ustawienie co na main.
        // Zmierzone 16.11 na APK pokoju z piecioma postaciami (309,6 MB danych):
        //   animacje 195,0 MB (63 %), tekstury 83,3 MB (27 %), siatki 30,6 MB (10 %).
        // Osobno zmierzone wprost z FBX: przesuniecie i skala KAZDEJ z 65 kosci sa STALE —
        // 0,00 % zmiany w calym ujeciu (animacja tylko obraca stawy). Przy Off Unity zapisywalo
        // te stale klatka po klatce dla wszystkich kosci: okolo 117 MB z tych 195 MB to ta sama
        // liczba w kolko. Redukcja zwija stala krzywa do jednego klucza — znika bez zadnej straty.
        // Tolerancje ciasne (0,2 % zamiast domyslnych 0,5 %): ruch ma zostac taki, jaki jest.
        // To NIE jest dobieranie ani przerabianie ruchow — zaden klip nie zmienia wlasciciela,
        // dlugosci ani kolejnosci. To zapis tych samych krzywych bez powtorzen.
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
        if (!ModelPostaci(assetPath)) return;
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
        // ZASADY punkt 2.1: SUFIT 2048 na wszystkim. 4096 tylko z pisemnym wyjatkiem wlasciciela,
        // a obsada osiedla takiego wyjatku nie ma. Zmierzone w paczce BRAXHORN_Osiedle_19_postaci:
        // 16 postaci ma mapy 2048, trzy maja 4096 — te trzy same w sobie to +67 MB w buildzie.
        // MIMIKA MA SUFIT 2048, NIE 1024: atlas ma osiem poziomow ulozonych 4 na 2, wiec plik to
        // 2048x1024. Sufit 1024 zmniejszylby go z powrotem i zeby znow mialyby po dwa piksele.
        t.maxTextureSize = 2048;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = t.maxTextureSize;
        // ZASADY punkt 2.2: ASTC 6x6 (3,56 bita na piksel) na ciele zamiast 4x4 (8 bitow) — 2,25x lzej.
        // Zmierzone na tej paczce: 483 MB w 4x4 z sufitem 4096 wobec 155 MB wedlug zasad.
        // TWARZ (Mimika) ZOSTAJE NA 4x4: tam sa zeby, powieki i linia ust, i patrzy sie na nie z bliska.
        android.format = mimika ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
        t.SetPlatformTextureSettings(android);
    }
}
