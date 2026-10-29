using System;
using System.IO;
using UnityEditor;

// Only images externalized by unity-tools/kickboxing/externalize_models.py.
// City, character and other project texture import settings are untouched.
public sealed class BraxKickboxingModelTextures : AssetPostprocessor
{
    const string Root = "Assets/Kickboxing/Models/Images/";

    public override uint GetVersion() { return 1; }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
        string name = Path.GetFileName(assetPath);
        bool srgb = name.StartsWith("s_", StringComparison.Ordinal);
        bool linear = name.StartsWith("l_", StringComparison.Ordinal);
        if (!srgb && !linear) return;
        bool alpha = name.StartsWith("s_rgba_", StringComparison.Ordinal)
                  || name.StartsWith("l_rgba_", StringComparison.Ordinal);
        var texture = (TextureImporter)assetImporter;

        // glTFast's glTF shaders decode the original XYZ normal channels.
        // Keep them linear Default textures; Unity NormalMap repacking would
        // change their channel representation. PNG pixels remain unmodified.
        texture.textureType = TextureImporterType.Default;
        texture.sRGBTexture = srgb;
        texture.mipmapEnabled = true;
        texture.isReadable = false;
        texture.alphaIsTransparency = false;
        texture.anisoLevel = 4;
        texture.maxTextureSize = 2048;
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.textureCompression = TextureImporterCompression.CompressedHQ;
        var web = texture.GetPlatformTextureSettings("WebGL");
        web.name = "WebGL";
        web.overridden = true;
        web.maxTextureSize = 2048;
        web.format = alpha ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
        web.compressionQuality = 90;
        web.crunchedCompression = false;
        texture.SetPlatformTextureSettings(web);
    }
}
