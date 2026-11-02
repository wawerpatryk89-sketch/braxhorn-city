using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Import polskich kwestii (Resources/OsiedleGlosy, źródło: unity-tools/claude/osiedle_glosy.py — Ogg Vorbis q6 po obróbce
// highpass 80 Hz / +3 dB przy 3 kHz / −16 LUFS). Build 45 (właściciel: mowa „zamulająca”): build 44 kodował drugi raz Vorbis
// jakość 0.65 na źródle Ogg q3 = podwójna mocna kompresja. Teraz: krótkie kwestie (< 1.0 s — okrzyki, „Hej!”) PCM
// (bez drugiej straty, ~44 KB/s przy 22 kHz mono), dłuższe Vorbis jakość 1.0 (druga kompresja praktycznie bez strat);
// częstotliwość źródła (22 050 Hz) zachowana, mono.
public sealed class BraxGlosyImport : AssetPostprocessor
{
    public const float KrotkaKwestia = 1.0f;   // 45 kwestii (35 s) = ~1.5 MB PCM w APK; dłuższe Vorbis 1.0
    public override uint GetVersion() => 2;

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/OsiedleGlosy/")) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        float sek = DlugoscOgg(assetPath);
        bool krotka = sek > 0f && sek < KrotkaKwestia;
        settings.loadType = krotka ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = krotka ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
        settings.quality = 1f;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.ClearSampleSettingOverride("Android");
    }

    // Długość Ogg Vorbis z nagłówka (częstotliwość z pakietu identyfikacyjnego, liczba próbek = granule ostatniej strony).
    // Błąd odczytu = −1 (wtedy Vorbis 1.0 — bezpiecznie dla każdej długości).
    public static float DlugoscOgg(string sciezka)
    {
        try
        {
            var b = File.ReadAllBytes(sciezka);
            return DlugoscOgg(b);
        }
        catch (Exception) { return -1f; }
    }

    public static float DlugoscOgg(byte[] b)
    {
        if (b == null || b.Length < 64 || b[0] != 'O' || b[1] != 'g' || b[2] != 'g' || b[3] != 'S') return -1f;
        int seg = b[26], dane = 27 + seg;                                 // pierwsza strona: pakiet identyfikacyjny Vorbis
        if (dane + 16 > b.Length || b[dane] != 1 || b[dane + 1] != 'v') return -1f;
        uint rate = BitConverter.ToUInt32(b, dane + 12);
        if (rate == 0) return -1f;
        for (int i = b.Length - 27; i >= 0; i--)                           // ostatnia strona „OggS”
            if (b[i] == 'O' && b[i + 1] == 'g' && b[i + 2] == 'g' && b[i + 3] == 'S')
            {
                long granule = BitConverter.ToInt64(b, i + 6);
                return granule > 0 ? granule / (float)rate : -1f;
            }
        return -1f;
    }
}
