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
    // b33 phone: both side walls showed BLACK although the log said APPLIED.
    // Geometry, winding and texture check out on paper, so the print now uses
    // the PROVEN entrance-mural path: no interior UV3 marker, same lighting,
    // 30 cm off the wall, and a mirrored back face so no winding/culling case
    // can hide it. The build log reports what stands between hall and print.
    public const float MuralStandOff = .30f;
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

    static Material CreateOriginalMaterial(Texture2D texture, string side)
    {
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("A1 original CityStaticPBR shader missing");
        var material = new Material(shader)
        {
            name = "A1 ORIGINAL owner-side mural - " + side,
            enableInstancing = true
        };
        material.SetTexture("_MainTex", texture);
        material.SetTexture("_EmissionMap", texture);
        material.SetColor("_Color", Color.white);
        // Same lighting path as the entrance mural (no interior marker): albedo
        // lit like the lobby print plus .42 of its own colours as emission.
        material.SetColor("_EmissionColor", new Color(.42f, .42f, .42f));
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Roughness", .88f);
        material.SetFloat("_VertexTint", 0f);
        material.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material, Output + "/Side_" + side + "_Original.mat");
        return material;
    }

    static Mesh FullWall(float x, float start, float end, string side)
    {
        // The physical wall is 70 x 27.945 m, not 3:1. Preserve the entire
        // 70 x 23.333 m composition. Continue ONLY its edge background above
        // and below; do not crop/duplicate/stretch faces, belt or lettering.
        float[] y = { WallBottomY, BottomY, BottomY + ImageHeight, WallTopY };
        float[] v = { 0f, 0f, 1f, 1f };
        // 8 front vertices + the same 8 again for the mirrored back face.
        var vertices = new Vector3[16];
        var uv = new Vector2[16];
        var triangles = new int[36];
        for (int row = 0; row < 4; row++)
        {
            vertices[2 * row] = new Vector3(x, y[row], start);
            vertices[2 * row + 1] = new Vector3(x, y[row], end);
            uv[2 * row] = new Vector2(0f, v[row]);
            uv[2 * row + 1] = new Vector2(1f, v[row]);
            vertices[8 + 2 * row] = vertices[2 * row];
            vertices[8 + 2 * row + 1] = vertices[2 * row + 1];
            uv[8 + 2 * row] = uv[2 * row];
            uv[8 + 2 * row + 1] = uv[2 * row + 1];
        }
        for (int band = 0; band < 3; band++)
        {
            int a = 2 * band, t = 6 * band;
            triangles[t] = a; triangles[t + 1] = a + 2; triangles[t + 2] = a + 3;
            triangles[t + 3] = a; triangles[t + 4] = a + 3; triangles[t + 5] = a + 1;
            int b = 8 + a, u = 18 + t;
            triangles[u] = b; triangles[u + 1] = b + 3; triangles[u + 2] = b + 2;
            triangles[u + 3] = b; triangles[u + 4] = b + 1; triangles[u + 5] = b + 3;
        }
        var mesh = new Mesh { name = "A1 " + side + " full wall | unchanged 3:1 artwork | two-sided" };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateUVDistributionMetrics();
        return mesh;
    }

    static bool Side(Transform bowl, string sourcePath, string side, bool left)
    {
        if(string.IsNullOrEmpty(sourcePath))return false;
        if(!ConfigureOwnerHdImporter(sourcePath,side))return false;
        var photo = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (!photo)
        {
            BraxBuildQuality.Warn("CHAMPIONS_A1_SIDE_MURAL_MISSING: " + sourcePath + "; no substitute image");
            return false;
        }
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("A1 mural importer missing: " + sourcePath);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if(sourceWidth!=6144||sourceHeight!=2048||sourceWidth!=sourceHeight*3)
        {
            BraxBuildQuality.Warn("A1_"+side+"_MURAL_FAIL: source is not exact approved 6144x2048 3:1: "+
                sourceWidth+"x"+sourceHeight);
            return false;
        }
        Directory.CreateDirectory(Output);
        var material = CreateOriginalMaterial(photo, side);
        // Printed panel on 12.5 cm stand-offs in front of the structural wall's
        // inner face (x=-26.045 / -145.955): no shared depth with the wall at
        // 60-90 m viewing distance on a phone depth buffer.
        float x = left ? HallLeftX - MuralStandOff : HallRightX + MuralStandOff;
        float start = left ? WallNearZ : WallFarZ;
        float end = left ? WallFarZ : WallNearZ;
        var mesh = FullWall(x, start, end, side);
        AssetDatabase.CreateAsset(mesh, Output + "/Side_" + side + "_OriginalMesh.asset");
        var surface = new GameObject("A1 MURAL " + side + " | OWNER ORIGINAL | whole cage-side wall");
        surface.transform.SetParent(bowl, false);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(surface, StaticEditorFlags.OccludeeStatic);
        // Printed surface only: do not create a collider or change structural walls.
        // Diagnostic for the black-wall report: which renderers cross the line
        // from the hall centre (cage height 15 m) to the centre of the print.
        Physics.SyncTransforms();
        var from = bowl.TransformPoint(new Vector3(-86f, 15f, 63.3f));
        var to = bowl.TransformPoint(new Vector3(x, BottomY + ImageHeight * .5f, (start + end) * .5f));
        var ray = new Ray(from, (to - from).normalized);
        float length = Vector3.Distance(from, to);
        var blockers = new System.Collections.Generic.List<string>();
        foreach (var r in bowl.GetComponentsInChildren<Renderer>(true))
        {
            if (r == renderer) continue;
            if (r.bounds.IntersectRay(ray, out float d) && d < length - .5f)
                blockers.Add(r.name + "@" + d.ToString("F1"));
        }
        var nrm = mesh.normals[0];
        var toHall = (new Vector3(-86f, 15f, 63.3f) - new Vector3(x, 15f, 63.3f)).normalized;
        Debug.Log("A1_SIDE_MURAL_LINE_OF_SIGHT " + side + " frontFacesHall=" + (Vector3.Dot(nrm, toHall) > .9f) +
            " blockers=" + blockers.Count + " " + string.Join(",", blockers.GetRange(0, Mathf.Min(12, blockers.Count))));
        Debug.Log("CHAMPIONS_A1_SIDE_MURAL_APPLIED " + side + " source=" + sourcePath +
            " sourcePixels=" + sourceWidth + "x" + sourceHeight + " wall=70x27.945 originalArt=70x23.333");
        return true;
    }

    public static int Generate(Transform originalBowl)
    {
        if (!originalBowl) throw new ArgumentNullException(nameof(originalBowl));
        int applied = 0;
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
                  "/2 wall=70x27.945 art=70x23.333 y=2.65..25.983 source=6144x2048 noFallback=true");
        return applied;
    }
}
