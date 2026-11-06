using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Two ORIGINAL, unmodified panoramic owner images on the A1 hall's OPPOSITE
// SIDE walls. Camera viewpoint: public glass entry, looking toward the cage.
// LEFT = full fighter lineup; RIGHT = gold BRAXHORN CHAMPION belt.
// This is a printed non-colliding surface, never a wall across a walkway.
public static class BraxChampionsArenaSideMurals
{
    const string Output = "Assets/PlayableGenerated/Mistrzowie/Arena/Mural";
    const string Source = "Assets/Source/Mistrzowie/ArenaMurals/";
    public const string LeftFighters = Source + "BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg";
    public const string RightBelt = Source + "BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg";

    const float HallLeftX = -26f;    // after the original 180-degree bowl rotation
    const float HallRightX = -146f;  // this is the spectator's right side
    const float WallNearZ = 98.3f;  // nearest the public entrance
    const float WallFarZ = 28.3f;   // toward the athletes' rear entrance
    const float BottomY = 2.65f;    // safely above the original red 2.4-2.56m stripe
    const float WallSpan = WallNearZ - WallFarZ; // 70 nominal metres
    const float ImageHeight = WallSpan / 3f;     // exact 1536:512 aspect
    const int ImageWidthPx = 1536;
    const int ImageHeightPx = 512;

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
        material.SetColor("_Color", Color.white); // no recolouring or blending
        material.SetColor("_EmissionColor", new Color(.18f, .18f, .18f));
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Roughness", .88f);
        material.SetFloat("_VertexTint", 0f);
        material.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material, Output + "/Side_" + side + "_Original.mat");
        return material;
    }

    static bool Side(Transform bowl, string sourcePath, string side, bool left)
    {
        var photo = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (!photo)
        {
            Debug.LogWarning("CHAMPIONS_A1_SIDE_MURAL_MISSING: " + sourcePath +
                             " (no substitute image or AI-generated replacement)");
            return false;
        }
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("A1 original side mural has no image importer: " + sourcePath);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth != ImageWidthPx || sourceHeight != ImageHeightPx)
            throw new InvalidDataException("A1 original side mural source dimensions have changed: " + sourcePath);
        if (photo.width != ImageWidthPx || photo.height != ImageHeightPx)
            BraxBuildQuality.Warn("A1 mural source is unchanged 1536x512; Android import size differs: " +
                sourcePath + " imported=" + photo.width + "x" + photo.height);
        if (!importer.mipmapEnabled || importer.maxTextureSize != 2048)
            BraxBuildQuality.Warn("A1 mural mobile import should be reviewed, but is not an APK blocker: " + sourcePath);

        Directory.CreateDirectory(Output);
        var material = CreateOriginalMaterial(photo, side);
        // Old bowl coordinates before K and 180-degree rotation:
        // left spectator wall (original X1) normal -X, right (original X0) normal +X.
        float x = left ? HallLeftX - .11f : HallRightX + .10f;
        float start = left ? WallNearZ : WallFarZ;
        float end = left ? WallFarZ : WallNearZ;
        var mesh = new Mesh { name = "A1 full original " + side + " wall panoramic 3:1" };
        mesh.vertices = new[]
        {
            new Vector3(x, BottomY, start),
            new Vector3(x, BottomY + ImageHeight, start),
            new Vector3(x, BottomY + ImageHeight, end),
            new Vector3(x, BottomY, end)
        };
        mesh.uv = new[] { new Vector2(0f,0f), new Vector2(0f,1f),
                          new Vector2(1f,1f), new Vector2(1f,0f) };
        mesh.triangles = new[] { 0,1,2, 0,2,3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Output + "/Side_" + side + "_OriginalMesh.asset");

        var surface = new GameObject("A1 MURAL " + side + " | OWNER ORIGINAL | 70m side wall");
        surface.transform.SetParent(bowl, false);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        // Intentional: no collider, no blocking the viewer/aisle, no replacement of
        // the original hall's load-bearing side wall and its existing structural collider.
        GameObjectUtility.SetStaticEditorFlags(surface, StaticEditorFlags.OccludeeStatic);
        return true;
    }

    public static int Generate(Transform originalBowl)
    {
        if (!originalBowl) throw new ArgumentNullException(nameof(originalBowl));
        int applied = 0;
        if (Side(originalBowl, LeftFighters, "LEFT_FIGHTERS", true)) applied++;
        if (Side(originalBowl, RightBelt, "RIGHT_BELT", false)) applied++;
        Debug.Log("CHAMPIONS_A1_SIDE_MURALS originals=" + applied +
                  "/2 each 1536x512, wall=70x23.333m, doorway/entry untouched, source pixels unchanged");
        return applied;
    }
}
