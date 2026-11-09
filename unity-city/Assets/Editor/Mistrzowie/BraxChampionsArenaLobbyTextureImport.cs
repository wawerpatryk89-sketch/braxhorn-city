using UnityEditor;

// Textures of the CC0 Poly Haven lobby models (prepare_arena_lobby_models.py).
// glTFast uses these project texture assets at Editor import instead of making
// uncompressed RGBA32 copies, so the APK gets real ASTC. 6x6 keeps the native
// 1K/2K detail at ~44% of the 4x4 size (owner 9.11: the APK must shrink).
// Data maps (normal GL, ARM, roughness) are linear; only *_diff_* is sRGB.
// Normals stay Default type: glTFast reads XYZ, not DXT5nm.
public sealed class BraxChampionsArenaLobbyTextureImport : AssetPostprocessor
{
    public const string Folder="/Source/Mistrzowie/ArenaLobby/";
    public const string TrophyFolder="/Source/Mistrzowie/ArenaTrophies/";
    // HOTEL SOBCZAK T03 furniture and decor (prepare_hotel_sobczak_models.py), same ASTC 6x6 contract.
    public const string HotelFolder="/Source/Mistrzowie/HotelSobczak/";
    public override uint GetVersion()=>3;
    void OnPreprocessTexture()
    {
        if(assetPath.Contains(TrophyFolder))
        {
            // Engraved trophy plates: crisp lettering -> ASTC 4x4, never streamed.
            var p=(TextureImporter)assetImporter;
            p.textureType=TextureImporterType.Default;p.sRGBTexture=true;p.mipmapEnabled=true;
            p.isReadable=false;p.anisoLevel=8;p.streamingMipmaps=false;p.wrapMode=UnityEngine.TextureWrapMode.Clamp;
            p.maxTextureSize=2048;p.textureCompression=TextureImporterCompression.Compressed;
            var a=p.GetPlatformTextureSettings("Android");
            a.name="Android";a.overridden=true;a.maxTextureSize=2048;a.format=TextureImporterFormat.ASTC_4x4;a.compressionQuality=100;
            p.SetPlatformTextureSettings(a);
            return;
        }
        if(!assetPath.Contains(Folder)&&!assetPath.Contains(HotelFolder))return;
        var t=(TextureImporter)assetImporter;
        string name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
        t.textureType=TextureImporterType.Default;
        t.sRGBTexture=name.Contains("_diff_");
        t.mipmapEnabled=true;t.isReadable=false;t.anisoLevel=4;t.streamingMipmaps=true;
        t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=false;
        t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Compressed;
        var android=t.GetPlatformTextureSettings("Android");
        android.name="Android";android.overridden=true;android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=100;
        t.SetPlatformTextureSettings(android);
    }
}
