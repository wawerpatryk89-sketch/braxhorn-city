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
        var web=t.GetPlatformTextureSettings("WebGL");web.name="WebGL";web.overridden=true;web.maxTextureSize=4096;
        web.format=TextureImporterFormat.ETC2_RGBA8;web.compressionQuality=90;t.SetPlatformTextureSettings(web);
    }
}
