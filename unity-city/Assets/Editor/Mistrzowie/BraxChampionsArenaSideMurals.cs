using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Two owner originals in the hall, left and right of the fighters' door on the wall every visitor faces (12.11).
// The third mural belongs to the entrance lobby and is generated separately, exactly once.
public static class BraxChampionsArenaSideMurals
{
    const string Output = "Assets/PlayableGenerated/Mistrzowie/Arena/Mural";
    const string HdSource = "Assets/Source/Mistrzowie/ArenaMuralsHD/";
    public const string LeftFightersHd = HdSource + "MURAL_LEWA_ZAWODNICY_6144x2048.png";
    public const string RightBeltHd = HdSource + "MURAL_PRAWA_PAS_6144x2048.png";

    // b33 / APK 35 phone: both side walls showed BLACK although the log said APPLIED (one 8192-wide padded ASTC
    // texture sampled 4-5 mips down, lit only by a dim interior ambient). Since 9.11 23:43 every mural is hung by
    // BraxChampionsArenaMuralPrint: three 2048 tiles, back-lit, gold frame.
    public const float MuralStandOff = .12f;

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

    // Owner 12.11 00:30-01:55 (APK 38): "Te dwa murale na arenie ... mają być, między nimi chodzi", "W arenie. Po prawej i
    // po lewej. Od 5 buildów ich nie ma", "mają być i chuj". On the two short walls (x -26 / -146) the prints hung 60 m to
    // the side of anyone walking in from the public door or standing at the cage: out of the picture on the phone, behind
    // the side stands. Now both hang on the wall EVERY visitor faces - the long wall of the fighters' door (Z0), seen
    // straight ahead from the public door, from the aisle and from the cage: FIGHTERS on the LEFT of the fighters' door,
    // BELT on the RIGHT (as seen from the hall), the fighters walk out BETWEEN them. 52.5 x 17.5 m each (3:1, the owner's
    // 6144 x 2048 untouched), bottom 8.0 m (over the top row of the stands and the HALA WALK sign), top 25.5 m (under the
    // ceiling beams), 12 cm in front of the quilted leather, back-lit tiles drawn from both sides, gold frame. Each owner
    // mural exactly once (owner 10.11 13:20): the short walls keep their leather.
    public const float DoorWallZ = 28.3f, DoorCentreX = -86f;
    public const float WallMuralWidth = 52.5f, WallMuralHeight = 17.5f, WallMuralBottom = 8.0f, WallMuralCentreOffset = 28.5f;

    static bool Side(Transform bowl, string sourcePath, string side, bool left)
    {
        if(string.IsNullOrEmpty(sourcePath))return false;
        if(!ConfigureOwnerHdImporter(sourcePath,side))return false;
        // Print 12 cm in front of the long wall's inner face (z = 28.345).
        float z = DoorWallZ + .045f + MuralStandOff;
        // Seen from the hall the visitor looks along -Z: the viewer's right is -X, so the LEFT mural is the +X one.
        var facing = Vector3.forward;
        var right = Vector3.left;
        float xc = DoorCentreX + (left ? WallMuralCentreOffset : -WallMuralCentreOffset);
        string name = "A1 MURAL " + side + " | OWNER ORIGINAL | fighters' door wall, " + (left ? "left" : "right") + " of the door";
        int hung = BraxChampionsArenaMuralPrint.Hang(bowl, name, "Side_" + side, sourcePath,
            new Vector3(xc + WallMuralWidth * .5f, WallMuralBottom, z), right, facing, WallMuralWidth, WallMuralHeight, out var group);
        if (hung == 0)
        {
            BraxBuildQuality.Warn("CHAMPIONS_A1_SIDE_MURAL_MISSING: " + sourcePath + "; no substitute image");
            return false;
        }
        BraxChampionsArenaMuralPrint.Frame(group, "Side_" + side,
            new Vector3(xc, WallMuralBottom + WallMuralHeight * .5f, z), right, facing, WallMuralWidth, WallMuralHeight);
        Debug.Log("CHAMPIONS_A1_SIDE_MURAL_APPLIED " + side + " source=" + sourcePath + " panels=" + hung +
            " sourcePixels=6144x2048 wall=Z0 (fighters' door) x=" + (xc - WallMuralWidth * .5f).ToString("F2") + ".." +
            (xc + WallMuralWidth * .5f).ToString("F2") + " y=" + WallMuralBottom + ".." + (WallMuralBottom + WallMuralHeight) +
            " art=52.5x17.5 seenFrom=public door, aisle, cage backlit=true frame=gold");
        return true;
    }

    public static int Generate(Transform originalBowl)
    {
        if (!originalBowl) throw new ArgumentNullException(nameof(originalBowl));
        int applied = 0;
        // Owner 10.11 13:20: "Czemu zrobiłeś duplikaty? Murale mają być tam, gdzie ja chcę" - each owner mural exactly once:
        // the two hall murals left and right of the fighters' door (12.11), the third in the lobby.
        bool left=Side(originalBowl,PickRequiredHd(LeftFightersHd,"LEFT"),"LEFT_FIGHTERS",true);
        bool right=Side(originalBowl,PickRequiredHd(RightBeltHd,"RIGHT"),"RIGHT_BELT",false);
        if(left)applied++; if(right)applied++;
        if(!left)BraxBuildQuality.Warn("LEFT_MURAL = FAIL");
        else Debug.Log("LEFT_MURAL = PASS");
        if(!right)BraxBuildQuality.Warn("RIGHT_MURAL = FAIL");
        else Debug.Log("RIGHT_MURAL = PASS");
        if(applied!=2)BraxBuildQuality.Warn("SIDE_MURALS = "+applied+"/2 FAIL");
        else Debug.Log("SIDE_MURALS = 2/2 PASS");
        Debug.Log("CHAMPIONS_A1_SIDE_MURALS ownerHD="+applied+
                  "/2 wall=Z0 left+right of the fighters' door art=52.5x17.5 y=8.0..25.5 source=6144x2048 noFallback=true");
        return applied;
    }
}
