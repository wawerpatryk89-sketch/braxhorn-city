using UnityEditor;
using UnityEngine;

// Muzyka ekranu startowego (piosenka willi, Resources/BraxStartMuzyka.ogg z zaszyfrowanej paczki grafik): strumieniowanie
// z dysku zamiast rozpakowania do pamięci (≈ 30 MB PCM na słabym telefonie przy samym starcie), Vorbis, mono-kompatybilnie.
public sealed class BraxStartMuzykaImport : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').EndsWith("/Resources/BraxStartMuzyka.ogg")) return;
        var ai = (AudioImporter)assetImporter;
        var s = ai.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .6f;
        ai.defaultSampleSettings = s; ai.loadInBackground = true;
    }
}
