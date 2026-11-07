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
    const string Source = "Assets/Source/Mistrzowie/ArenaMurals/";
    public const string LeftFighters = Source + "BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg";
    public const string RightBelt = Source + "BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg";
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
    const float WallSpan = WallNearZ - WallFarZ;
    const float ImageHeight = WallSpan / 3f;

    static string Pick(string hd, string original)
    {
        // Unity asset paths are relative to the project, while the CI process
        // can run from the repository root. File.Exists("Assets/...") therefore
        // depends on the process working directory and can miss an installed HD
        // mural. AssetDatabase is the authoritative lookup inside the Unity project.
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(hd)) return hd;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(original))
        {
            BraxBuildQuality.Warn("A1 HD mural unavailable: " + hd +
                "; using verified owner original for this scene");
            return original;
        }
        BraxBuildQuality.Warn("CHAMPIONS_A1_SIDE_MURAL_SOURCE_MISSING hd=" + hd +
            " original=" + original + " buildContinuesForReport=true");
        return original;
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
        material.SetColor("_EmissionColor", new Color(.18f, .18f, .18f));
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Roughness", .88f);
        material.SetFloat("_VertexTint", 0f);
        // Keep the printed wall visible even if a future mesh/export changes
        // triangle winding. This changes no pixels, UVs, scale or wall placement.
        material.SetFloat("_Cull", 0f);
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
        var vertices = new Vector3[8];
        var uv = new Vector2[8];
        var triangles = new int[18];
        for (int row = 0; row < 4; row++)
        {
            vertices[2 * row] = new Vector3(x, y[row], start);
            vertices[2 * row + 1] = new Vector3(x, y[row], end);
            uv[2 * row] = new Vector2(0f, v[row]);
            uv[2 * row + 1] = new Vector2(1f, v[row]);
        }
        for (int band = 0; band < 3; band++)
        {
            int a = 2 * band, t = 6 * band;
            triangles[t] = a; triangles[t + 1] = a + 2; triangles[t + 2] = a + 3;
            triangles[t + 3] = a; triangles[t + 4] = a + 3; triangles[t + 5] = a + 1;
        }
        var mesh = new Mesh { name = "A1 " + side + " full wall | unchanged 3:1 artwork" };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    static bool Side(Transform bowl, string sourcePath, string side, bool left)
    {
        var photo = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (!photo)
        {
            BraxBuildQuality.Warn("CHAMPIONS_A1_SIDE_MURAL_MISSING: " + sourcePath + "; no substitute image");
            return false;
        }
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("A1 mural importer missing: " + sourcePath);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth != sourceHeight * 3)
            BraxBuildQuality.Warn("A1 mural aspect is not 3:1: " + sourcePath);
        if (!importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp)
        {
            // Edge continuation must sample the edge, not repeat the opposite side.
            // Importer-only change; retain file bytes, GUID, and resolution settings.
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            photo = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (!photo) throw new InvalidOperationException("A1 mural reimport failed: " + sourcePath);
        }
        Directory.CreateDirectory(Output);
        var material = CreateOriginalMaterial(photo, side);
        float x = left ? HallLeftX - .11f : HallRightX + .10f;
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
        Debug.Log("CHAMPIONS_A1_SIDE_MURAL_APPLIED " + side + " source=" + sourcePath +
            " sourcePixels=" + sourceWidth + "x" + sourceHeight +
            " wall=70x27.945 originalArt=70x23.333 twoSided=true");
        return true;
    }

    public static int Generate(Transform originalBowl)
    {
        if (!originalBowl) throw new ArgumentNullException(nameof(originalBowl));
        int applied = 0;
        if (Side(originalBowl, Pick(LeftFightersHd, LeftFighters), "LEFT_FIGHTERS", true)) applied++;
        if (Side(originalBowl, Pick(RightBeltHd, RightBelt), "RIGHT_BELT", false)) applied++;
        Debug.Log("CHAMPIONS_A1_SIDE_MURALS originals=" + applied +
                  "/2 whole side walls; entrance mural separate; source pixels unchanged");
        return applied;
    }
}
