using UnityEditor;
using UnityEngine;
// R2-only mobile texture budget. Original Library 4K sources are never edited.
public sealed class BraxEstatePBRTextureSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/EstateRenovation/"))return;
        var t=(TextureImporter)assetImporter;
        bool normal=assetPath.EndsWith("_nor_gl_1k.png");
        bool linear=normal||assetPath.EndsWith("_rough_1k.png")||
            assetPath.EndsWith("_metallic_gloss_1k.png");
        t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        t.sRGBTexture=!linear;
        t.mipmapEnabled=true;
        t.maxTextureSize=1024;
        t.anisoLevel=8;
        t.isReadable=false;
        t.textureCompression=TextureImporterCompression.CompressedHQ;
        t.wrapMode=TextureWrapMode.Repeat;
        var web=t.GetPlatformTextureSettings("WebGL");
        web.name="WebGL";web.overridden=true;web.maxTextureSize=1024;
        web.format=TextureImporterFormat.ETC2_RGBA8;
        web.compressionQuality=90;
        t.SetPlatformTextureSettings(web);
    }
}
