using UnityEditor;
// Preserve original pixel dimensions; use native mobile GPU compression.
public sealed class BraxCityTextures : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/City/Images/"))return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Default;
        t.sRGBTexture=System.IO.Path.GetFileName(assetPath).StartsWith("s_");
        t.mipmapEnabled=true;t.isReadable=false;t.anisoLevel=8;t.maxTextureSize=4096;
        t.npotScale=TextureImporterNPOTScale.None;t.textureCompression=TextureImporterCompression.CompressedHQ;
        // 1K od pierwszego importu (1.11, Opus): wcześniej CapFacadeTextures ustawiał 1K po imporcie i przeimportowywał
        // ~880 tekstur pojedynczo, każda ciągnęła za sobą ponowny import braxhorn-city/estate.gltf (build 8: ponad
        // godzina w APK), a w WebGL ten preprocesor i tak przywracał 4096. Teraz: 1K dla Androida i WebGL od razu.
        var web=t.GetPlatformTextureSettings("WebGL");web.name="WebGL";web.overridden=true;web.maxTextureSize=1024;
        web.format=TextureImporterFormat.ETC2_RGBA8;web.compressionQuality=90;t.SetPlatformTextureSettings(web);
        var andr=t.GetPlatformTextureSettings("Android");andr.name="Android";andr.overridden=true;andr.maxTextureSize=1024;
        andr.format=TextureImporterFormat.Automatic;andr.textureCompression=TextureImporterCompression.Compressed;andr.compressionQuality=50;t.SetPlatformTextureSettings(andr);
    }
}
