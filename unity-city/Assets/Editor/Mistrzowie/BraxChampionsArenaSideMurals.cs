using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Two owner originals on the opposite walls beside the cage. The third mural
// belongs to the entrance lobby and is generated separately, exactly once.
public static class BraxChampionsArenaSideMurals
{
    const string Output = "Assets/PlayableGenerated/Mistrzowie/Arena/Mural";
    const string HdSource = "Assets/Source/Mistrzowie/ArenaMuralsHD/";
    public const string LeftFightersHd = HdSource + "MURAL_LEWA_ZAWODNICY_6144x2048.png";
    public const string RightBeltHd = HdSource + "MURAL_PRAWA_PAS_6144x2048.png";

    const float HallLeftX = -26f;
    const float HallRightX = -146f;
    const float WallNearZ = 98.3f;
    const float WallFarZ = 28.3f;
    const float WallBottomY = .055f;
    const float WallTopY = 28f;
    const float BottomY = 2.65f;
    // b33 / APK 35 phone: both side walls showed BLACK although the log said APPLIED (one 8192-wide padded ASTC
    // texture sampled 4-5 mips down, lit only by a dim interior ambient). Since 9.11 23:43 every mural is hung by
    // BraxChampionsArenaMuralPrint: three 2048 tiles, back-lit, gold frame.
    public const float MuralStandOff = .12f;
    const float WallSpan = WallNearZ - WallFarZ;
    const float ImageHeight = WallSpan / 3f;

    static string PickRequiredHd(string hd, string label)
    {
        if (!File.Exists(hd))
        {
            BraxBuildQuality.Warn("A1_"+label+"_MURAL_FAIL: required owner HD source missing: "+hd+
                "; NO FALLBACK and NO substitute image permitted");
            return null;
        }
        AssetDatabase.ImportAsset(hd, ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(hd);
        if(!texture)
        {
            BraxBuildQuality.Warn("A1_"+label+"_MURAL_FAIL: Unity did not import required owner HD source: "+hd);
            return null;
        }
        return hd;
    }

    static bool ConfigureOwnerHdImporter(string sourcePath,string label)
    {
        var importer=AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if(!importer)return false;
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth!=6144||sourceHeight!=2048)
        {
            BraxBuildQuality.Warn("A1_"+label+"_MURAL_FAIL: required owner source must remain 6144x2048, got "+
                sourceWidth+"x"+sourceHeight);
            return false;
        }
        bool dirty=false;
        if(importer.maxTextureSize!=8192){importer.maxTextureSize=8192;dirty=true;}
        if(!importer.mipmapEnabled){importer.mipmapEnabled=true;dirty=true;}
        if(importer.streamingMipmaps){importer.streamingMipmaps=false;dirty=true;} // always full resolution
        if(importer.wrapMode!=TextureWrapMode.Clamp){importer.wrapMode=TextureWrapMode.Clamp;dirty=true;}
        if(importer.filterMode!=FilterMode.Trilinear){importer.filterMode=FilterMode.Trilinear;dirty=true;}
        if(importer.anisoLevel!=4){importer.anisoLevel=4;dirty=true;}
        var android=importer.GetPlatformTextureSettings("Android");
        if(!android.overridden||android.maxTextureSize!=8192||
           android.format!=TextureImporterFormat.ASTC_4x4||android.compressionQuality!=100)
        {
            android.overridden=true;
            android.maxTextureSize=8192;
            android.format=TextureImporterFormat.ASTC_4x4;
            android.compressionQuality=100;
            importer.SetPlatformTextureSettings(android);
            dirty=true;
        }
        if(dirty)importer.SaveAndReimport();
        return true;
    }

    static bool Side(Transform bowl, string sourcePath, string side, bool left)
    {
        if(string.IsNullOrEmpty(sourcePath))return false;
        if(!ConfigureOwnerHdImporter(sourcePath,side))return false;
        // Print 12 cm in front of the structural wall's inner face (x=-26.045 / -145.955).
        float x = left ? HallLeftX - MuralStandOff : HallRightX + MuralStandOff;
        // Seen from the hall: the left wall is looked at along +X (viewer-right = -Z), the right wall along -X.
        var facing = left ? Vector3.left : Vector3.right;
        var right = left ? Vector3.back : Vector3.forward;
        float startZ = left ? WallNearZ : WallFarZ;
        string name = "A1 MURAL " + side + " | OWNER ORIGINAL | whole cage-side wall";
        int hung = BraxChampionsArenaMuralPrint.Hang(bowl, name, "Side_" + side, sourcePath,
            new Vector3(x, BottomY, startZ), right, facing, WallSpan, ImageHeight, out var group);
        if (hung == 0)
        {
            BraxBuildQuality.Warn("CHAMPIONS_A1_SIDE_MURAL_MISSING: " + sourcePath + "; no substitute image");
            return false;
        }
        BraxChampionsArenaMuralPrint.Frame(group, "Side_" + side,
            new Vector3(x, BottomY + ImageHeight * .5f, (WallNearZ + WallFarZ) * .5f), right, facing, WallSpan - .34f, ImageHeight);
        Debug.Log("CHAMPIONS_A1_SIDE_MURAL_APPLIED " + side + " source=" + sourcePath + " panels=" + hung +
            " sourcePixels=6144x2048 wall=70x27.945 originalArt=70x23.333 backlit=true frame=gold");
        return true;
    }

    // Owner 10.11 12:31 (APK 37, photos of the hall from the public door and the VIP floor): "Mają być murale ... od 10
    // buildów". From the doors and from the stands one looks at the two LONG walls (z 28.3 / 98.3, 120 m, black
    // quilted leather with gold divisions) - the side murals are 60 m away left and right, out of the picture. The
    // owner's hall murals now hang on those long walls too, above the doors and the stand tops: FIGHTERS on the wall
    // behind the fighters' door (the wall the public faces), BELT on the wall over the public door (the wall the
    // fighters face on their walkout). 57 x 19 m, 3:1, same back-lit tiles and gold frame; the gold divisions of the
    // leather stop where a mural hangs (BraxChampionsArena.Clad), nothing lies on top of anything.
    public const float EndMuralBottom = 4.80f, EndMuralHeight = 19.0f, EndMuralWidth = 57.0f, EndMuralCentreX = -86f;
    static bool End(Transform bowl, string sourcePath, string side, bool farWall)
    {
        if(string.IsNullOrEmpty(sourcePath))return false;
        float inner = farWall ? WallFarZ + .045f : WallNearZ - .045f;
        float z = farWall ? inner + MuralStandOff : inner - MuralStandOff;
        var facing = farWall ? Vector3.forward : Vector3.back;          // from the print toward the hall
        var right = farWall ? Vector3.left : Vector3.right;            // viewer's right looking at the wall
        float startX = EndMuralCentreX - right.x * EndMuralWidth * .5f;
        string name = "A1 MURAL " + side + " | OWNER ORIGINAL | long wall over the door";
        int hung = BraxChampionsArenaMuralPrint.Hang(bowl, name, "End_" + side, sourcePath,
            new Vector3(startX, EndMuralBottom, z), right, facing, EndMuralWidth, EndMuralHeight, out var group);
        if (hung == 0) { BraxBuildQuality.Warn("CHAMPIONS_A1_END_MURAL_MISSING: " + sourcePath); return false; }
        BraxChampionsArenaMuralPrint.Frame(group, "End_" + side,
            new Vector3(EndMuralCentreX, EndMuralBottom + EndMuralHeight * .5f, z), right, facing, EndMuralWidth, EndMuralHeight);
        Debug.Log("CHAMPIONS_A1_END_MURAL_APPLIED " + side + " wall z=" + (farWall ? WallFarZ : WallNearZ) + " panels=" + hung +
                  " size=57x19 y=" + EndMuralBottom + ".." + (EndMuralBottom + EndMuralHeight) + " backlit=true frame=gold");
        return true;
    }
    /// <summary>x range of the long-wall murals incl. the gold frame (the leather's gold divisions stop there).</summary>
    public static bool CoversLongWall(float x) => Mathf.Abs(x - EndMuralCentreX) < EndMuralWidth * .5f + BraxChampionsArenaMuralPrint.FrameBar + .30f;

    public static int Generate(Transform originalBowl)
    {
        if (!originalBowl) throw new ArgumentNullException(nameof(originalBowl));
        int applied = 0;
        string fightersHd = PickRequiredHd(LeftFightersHd,"LEFT"), beltHd = PickRequiredHd(RightBeltHd,"RIGHT");
        bool endFar = End(originalBowl, fightersHd, "FAR_FIGHTERS", true);
        bool endNear = End(originalBowl, beltHd, "NEAR_BELT", false);
        if (!endFar || !endNear) BraxBuildQuality.Warn("END_MURALS = FAIL far=" + endFar + " near=" + endNear);
        else Debug.Log("END_MURALS = 2/2 PASS (long walls, seen from the doors and the stands)");
        bool left=Side(originalBowl,fightersHd,"LEFT_FIGHTERS",true);
        bool right=Side(originalBowl,beltHd,"RIGHT_BELT",false);
        if(left)applied++; if(right)applied++;
        if(!left)BraxBuildQuality.Warn("LEFT_MURAL = FAIL");
        else Debug.Log("LEFT_MURAL = PASS");
        if(!right)BraxBuildQuality.Warn("RIGHT_MURAL = FAIL");
        else Debug.Log("RIGHT_MURAL = PASS");
        if(applied!=2)BraxBuildQuality.Warn("SIDE_MURALS = "+applied+"/2 FAIL");
        else Debug.Log("SIDE_MURALS = 2/2 PASS");
        Debug.Log("CHAMPIONS_A1_SIDE_MURALS ownerHD="+applied+
                  "/2 wall=70x27.945 art=70x23.333 y=2.65..25.983 source=6144x2048 noFallback=true");
        return applied;
    }
}
