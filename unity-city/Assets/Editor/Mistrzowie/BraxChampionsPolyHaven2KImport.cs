using System.IO;
using UnityEditor;
using UnityEngine;

// Keep original Poly Haven 2K glTF textures at full 2048px on Android.
// glTFast 6.20 imports the original model geometry and material assignments;
// this hook only sets platform texture quality, never recolors source artwork.
// Only this isolated next-release subdirectory is affected.
public sealed class BraxChampionsPolyHaven2KImport : AssetPostprocessor
{
    public override uint GetVersion() => 4; // 9.11 evening: size by real object size, ASTC 6x6
    // Owner rule "wszystko ma być odpowiednie pod skalę": a 9 cm lemon does not
    // need 2048 px (0.05 mm per pixel). Android size from the model's REAL
    // longest dimension (Poly Haven API, 9.11): <=0.35 m 512, <=1.0 m 1024,
    // larger 2048. The source files on disk stay the original 2K.
    static readonly System.Collections.Generic.Dictionary<string,int> RealSizeMax=
        new System.Collections.Generic.Dictionary<string,int>{
        {"croissant",512},{"carrot_cake",512},{"strawberry_chocolate_cake",512},{"hamburger_buns",1024},
        {"food_apple_01",512},{"bananas",1024},{"lemon",512},{"food_kiwi_01",512},{"food_lime_01",512},
        {"food_pomegranate_01",512},{"wooden_cutting_board",1024},{"wooden_display_shelves_01",2048},
        {"painted_wooden_shelves",2048},{"plastic_crate_02",1024},{"wicker_basket_02",1024},
        {"outdoor_table_chair_set_01",2048},{"metal_stool_03",2048},{"life_jacket",1024},{"binoculars",512},
        {"long_life_food",1024},{"wooden_bowl_02",512},{"tea_set_01",1024},{"brass_goblets",1024},
        {"plastic_thermos",512},{"wooden_crate_02",2048},
        // promenade planters (owner board 10.11 18:12): flowers 1.3-1.6 m long
        {"flower_empodium",1024},{"flower_gazania",1024}};
    public static int AndroidMax(string assetPath)
    {
        var rest=assetPath.Substring("Assets/Source/Mistrzowie/PolyHaven/".Length);
        int slash=rest.IndexOf('/');
        string id=slash>0?rest.Substring(0,slash):rest;
        return RealSizeMax.TryGetValue(id,out int px)?px:2048;
    }

    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/Mistrzowie/PolyHaven/"))
            return;
        var filename=Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        if(!filename.EndsWith("_2k"))
        {
            // Wyjątek wewnątrz importera psuje import; tylko ostrzegamy.
            Debug.LogWarning("Promenada: tekstura spoza paczki 2K, ustawienia domyślne: "+assetPath);
            return;
        }
        var imp=(TextureImporter)assetImporter;
        imp.textureType=TextureImporterType.Default; // OpenGL normal must remain RGB
        imp.textureShape=TextureImporterShape.Texture2D;
        imp.sRGBTexture=filename.Contains("_diff_")||filename.Contains("_col_");
        imp.maxTextureSize=2048;
        imp.npotScale=TextureImporterNPOTScale.ToLarger;
        imp.mipmapEnabled=true;
        imp.streamingMipmaps=true;
        imp.anisoLevel=8;
        imp.isReadable=false;
        imp.filterMode=FilterMode.Trilinear;
        imp.wrapMode=TextureWrapMode.Repeat;
        imp.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=imp.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=AndroidMax(assetPath);
        android.format=TextureImporterFormat.ASTC_6x6;
        android.compressionQuality=100;
        imp.SetPlatformTextureSettings(android);
    }
}
