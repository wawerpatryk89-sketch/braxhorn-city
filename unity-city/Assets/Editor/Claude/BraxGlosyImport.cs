using UnityEditor;
using UnityEngine;

public sealed class BraxGlosyImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/OsiedleGlosy/")) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .65f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
