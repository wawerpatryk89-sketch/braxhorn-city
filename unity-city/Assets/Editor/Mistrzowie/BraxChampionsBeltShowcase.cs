using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A1: ONE owner-supplied championship belt, next to the JUDGES (not the lobby).
// The parent arena uses nominal metres and applies the shared city scale K once.
// This generator changes no fighters, seats, public doors, neighbouring buildings
// or physics layers. Only the visible rhombus podium has physical collision.
public static class BraxChampionsBeltShowcase
{
    const string SourceBelt =
        "Assets/Source/Mistrzowie/Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx";
    const string BeltMaps =
        "Assets/Source/Mistrzowie/Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/";
    const string BeltArm = "Assets/Source/Mistrzowie/Trophies/ChampionshipBelt_ARM.png";
    // Exact owner-provided 1536 x 512 photo with the gold bull/BRAXHORN ARENA
    // and B crest. Its pixels and lettering must not be regenerated or recoloured.
    public const string OriginalPlaque =
        "Assets/Source/Mistrzowie/Trophies/BRAXHORN_ARENA_CASE_PLAQUE.jpg";
    const string Meshes = "Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/";
    const string Materials = "Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/";

    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static void Cube(Transform parent, string name, Material material,
        Vector3 position, Vector3 scale)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        // Decorative glass, frame and plaque cannot block spectator movement.
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        if (material && material.renderQueue >= 3000)
        {
            var renderer = cube.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    // True diamond/rhombus, with four diagonal edges and its own thickness.
    // A rotated square or invisible rectangular blocker is NOT used.
    static Mesh Diamond(string filename, float halfWidth, float halfDepth,
        float bottom, float top)
    {
        var corners = new[] {
            new Vector2(-halfWidth, 0), new Vector2(0, -halfDepth),
            new Vector2(halfWidth, 0), new Vector2(0, halfDepth)
        };
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int offset = v.Count;
            v.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] {
                new Vector2(a.x, a.z), new Vector2(b.x, b.z),
                new Vector2(c.x, c.z), new Vector2(d.x, d.z)
            });
            triangles.AddRange(new[] { offset, offset + 1, offset + 2,
                offset, offset + 2, offset + 3 });
        }
        // Face winding points outward; the diamond is visible from each side.
        Quad(new Vector3(corners[0].x, top, corners[0].y),
            new Vector3(corners[3].x, top, corners[3].y),
            new Vector3(corners[2].x, top, corners[2].y),
            new Vector3(corners[1].x, top, corners[1].y));
        Quad(new Vector3(corners[0].x, bottom, corners[0].y),
            new Vector3(corners[1].x, bottom, corners[1].y),
            new Vector3(corners[2].x, bottom, corners[2].y),
            new Vector3(corners[3].x, bottom, corners[3].y));
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = corners[i], b = corners[(i + 1) % 4];
            Quad(new Vector3(a.x, top, a.y), new Vector3(b.x, top, b.y),
                new Vector3(b.x, bottom, b.y), new Vector3(a.x, bottom, a.y));
        }
        var mesh = new Mesh { name = filename };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Meshes + filename + ".asset");
        return mesh;
    }

    static void Podium(Transform display, Material granite, Material gold)
    {
        void Layer(string name, Material material, float halfWidth,
            float halfDepth, float bottom, float top, bool realCollision)
        {
            Mesh mesh = Diamond(name, halfWidth, halfDepth, bottom, top);
            var element = new GameObject(name);
            element.transform.SetParent(display, false);
            element.AddComponent<MeshFilter>().sharedMesh = mesh;
            element.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (realCollision)
            {
                // Exact visible stone shape only, never an invisible blocking wall.
                element.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }
        Layer("A1_RealRhombus_GraniteStone", granite, 2.05f, 1.70f,
            .055f, .325f, true);
        Layer("A1_Rhombus_GoldInlayEdge", gold, 2.01f, 1.66f,
            .326f, .372f, false);
        Layer("A1_Rhombus_GraniteInsetTop", granite, 1.88f, 1.53f,
            .374f, .428f, false);
    }

    static Bounds VisualBounds(Transform display, Transform objectRoot)
    {
        var renderers = objectRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Owner's championship belt FBX has no renderers");
        bool started = false;
        var result = new Bounds();
        foreach (var renderer in renderers)
        {
            // Convert the eight corners of each world AABB to case-local metres.
            var b = renderer.bounds;
            for (int xi = 0; xi < 2; xi++)
            for (int yi = 0; yi < 2; yi++)
            for (int zi = 0; zi < 2; zi++)
            {
                Vector3 p = display.InverseTransformPoint(new Vector3(
                    xi == 0 ? b.min.x : b.max.x,
                    yi == 0 ? b.min.y : b.max.y,
                    zi == 0 ? b.min.z : b.max.z));
                if (!started) { result = new Bounds(p, Vector3.zero); started = true; }
                else result.Encapsulate(p);
            }
        }
        if (!started || Mathf.Max(result.size.x, result.size.y, result.size.z) < 0.00001f)
            throw new InvalidOperationException("Owner's championship belt FBX has invalid bounds");
        return result;
    }

    static void OriginalBelt(Transform display)
    {
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(SourceBelt);
        if (!original)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_BELT_MISSING: original owner's FBX was not imported: " + SourceBelt);
            return;
        }
        var instance = PrefabUtility.InstantiatePrefab(original) as GameObject;
        if (!instance) throw new InvalidOperationException("Cannot instantiate original belt FBX");
        instance.name = "ORIGINAL owner championship belt 3D - ONE exhibit";
        instance.transform.SetParent(display, false);
        var b = VisualBounds(display, instance.transform);
        // Use its native mesh and 3D detail. Rotate only if the longest axis
        // is horizontal: the championship belt must HANG vertically.
        if (b.size.x > b.size.y && b.size.x >= b.size.z)
            instance.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        else if (b.size.z > b.size.y && b.size.z > b.size.x)
            instance.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        b = VisualBounds(display, instance.transform);
        float factor = Mathf.Min(1.75f / Mathf.Max(b.size.y, .0001f),
            1.85f / Mathf.Max(b.size.x, .0001f));
        instance.transform.localScale *= factor;
        b = VisualBounds(display, instance.transform);
        // Suspend belt above the gold plaque, clear of the glass and wood.
        instance.transform.localPosition += new Vector3(0, 2.34f, .055f) - b.center;

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltMaps + "Color.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltMaps + "Normal.png");
        var arm = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltArm);
        if (!albedo || !normal || !arm)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_BELT_PBR_MISSING: original imported material preserved; review on phone");
        }
        else
        {
            var shader = Shader.Find(BraxCityMaterials.ShaderName);
            if (!shader) throw new InvalidOperationException("A1 belt CityStaticPBR shader missing");
            var material = new Material(shader) {
                name = "A1 owner original 3D belt PBR", enableInstancing = true
            };
            material.SetColor("_Color", Color.white);
            material.SetTexture("_MainTex", albedo);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_ARM", arm);
            material.SetFloat("_NormalEncoding", 0);
            material.SetFloat("_Roughness", 1);
            material.SetFloat("_Metallic", 1);
            material.SetFloat("_UseARMForAO", 0);
            material.SetFloat("_VertexTint", 0);
            material.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
            AssetDatabase.CreateAsset(material, Materials + "A1_owner_belt_showcase_pbr.mat");
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
        }
        // The decorative exhibit is not a second cage or a physics obstacle.
        foreach (var c in instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
    }

    static void OriginalRectangleArtwork(Transform display, Material gold, Material bronze)
    {
        // Real 3-D gold rectangle located BELOW the suspended 3-D belt.
        Cube(display, "Raised bronze rim of the 3D rectangular emblem",
            bronze, new Vector3(0, 1.075f, -.245f),
            new Vector3(2.31f, .81f, .115f));
        Cube(display, "SOLID GOLD RECTANGLE for owner BRAXHORN ARENA sticker",
            gold, new Vector3(0, 1.075f, -.319f),
            new Vector3(2.22f, .74f, .135f));

        var image = AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalPlaque);
        if (!image)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_PENDING: use exact original photo " + OriginalPlaque);
            return;
        }
        var importer = AssetImporter.GetAtPath(OriginalPlaque) as TextureImporter;
        if (!importer)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_IMPORTER_MISSING: " + OriginalPlaque);
            return;
        }
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth != 1536 || sourceHeight != 512)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_WRONG_SOURCE_SIZE: expected 1536x512, source="+
                sourceWidth+"x"+sourceHeight+" "+OriginalPlaque);
            return;
        }
        if (image.width != 1536 || image.height != 512)
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_ANDROID_IMPORT: original image remains 1536x512, "+
                "imported="+image.width+"x"+image.height+". Keep the owner's original artwork on the plaque.");
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("A1 display plaque PBR shader missing");
        var material = new Material(shader) {
            name = "A1 owner's unmodified BRAXHORN ARENA logo", enableInstancing = true
        };
        material.SetTexture("_MainTex", image);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_VertexTint", 0);
        material.SetFloat("_Roughness", .35f);
        material.SetFloat("_Metallic", 0);
        material.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material, Materials + "A1_owner_case_plaque.mat");
        var sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sticker.name = "EXACT owner black-and-gold bull BRAXHORN ARENA artwork on gold rectangle";
        sticker.transform.SetParent(display, false);
        sticker.transform.localPosition = new Vector3(0, 1.075f, -.390f);
        sticker.transform.localRotation = Quaternion.Euler(0, 180, 0);
        sticker.transform.localScale = new Vector3(2.13f, .71f, 1);
        sticker.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(sticker.GetComponent<Collider>());
    }

    public static void Generate(Transform arenaInterior)
    {
        // Judges' table occupies x=-78.7..-78.0 and z=61.0..65.6.
        // Visible podium stands beside it at x=-73.9: NOT the 6 m centre
        // entrance aisle, NOT the octagon, NOT the VIP chairs at z=70/72.
        var display = Group(arenaInterior, "A1 PREMIUM museum showcase | JUDGES | one 3D belt");
        display.localPosition = new Vector3(-73.9f, 0, 63.3f);
        display.localRotation = Quaternion.Euler(0, 90, 0); // front faces cage/judges
        var granite = BraxChampionsFoundation.SourceMaterial(
            "A1_belt_showcase_PolyHaven_granite_tile", "granite_tile", 1.1f, Color.white);
        var teak = BraxChampionsFoundation.SourceMaterial(
            "A1_belt_showcase_PolyHaven_teak_veneer", "teak_veneer", 1.15f, Color.white);
        var gold = BraxChampionsFoundation.LibraryMetal("A1_belt_showcase_gold",
            new Color(.96f, .72f, .24f));
        var bronze = BraxChampionsFoundation.LibraryMetal("A1_belt_showcase_bronze",
            new Color(.54f, .38f, .17f));
        var glass = AssetDatabase.LoadAssetAtPath<Material>(
            Materials + "clear_glass.mat");
        var warm = AssetDatabase.LoadAssetAtPath<Material>(
            Materials + "light.mat");
        if (!glass || glass.renderQueue < 3000 || !warm)
            throw new InvalidOperationException("A1 showroom requires the existing TRUE transparent glass and warm light materials");

        Podium(display, granite, gold);
        // Native Poly Haven teak wood: backboard, bottom shelf, structural edges.
        Cube(display, "Teak PBR cabinet bottom shelf",
            teak, new Vector3(0, .535f, 0), new Vector3(2.64f, .20f, 1.12f));
        Cube(display, "Teak PBR solid museum back panel",
            teak, new Vector3(0, 1.98f, .555f), new Vector3(2.61f, 2.79f, .11f));
        Cube(display, "Teak cabinet crown",
            teak, new Vector3(0, 3.46f, 0), new Vector3(2.72f, .19f, 1.18f));
        foreach (float x in new[] { -1.32f, 1.32f })
            foreach (float z in new[] { -.55f, .55f })
                Cube(display, "True teak frame upright", teak,
                    new Vector3(x, 2.0f, z), new Vector3(.095f, 2.81f, .095f));
        // Continuous real visible GLASS, but NO phantom collision boxes.
        Cube(display, "Museum GLASS FRONT no invisible collider",
            glass, new Vector3(0, 2.0f, -.593f), new Vector3(2.57f, 2.79f, .045f));
        foreach (float x in new[] { -1.302f, 1.302f })
            Cube(display, "Museum GLASS SIDE no invisible collider",
                glass, new Vector3(x, 2.0f, 0),
                new Vector3(.045f, 2.79f, 1.06f));
        // Do not put an opaque glass back across the wood-backed belt.
        OriginalRectangleArtwork(display, gold, bronze);
        OriginalBelt(display);
        Cube(display, "Visible gold LED roof diffuser", warm,
            new Vector3(0, 3.326f, -.28f), new Vector3(1.93f, .052f, .10f));
        Cube(display, "Visible gold LED base diffuser", warm,
            new Vector3(0, .67f, -.27f), new Vector3(1.84f, .043f, .085f));
        var lamp = new GameObject("Museum case warm mobile vertex light").AddComponent<Light>();
        lamp.transform.SetParent(display, false);
        lamp.transform.localPosition = new Vector3(0, 3.16f, -.12f);
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, .87f, .63f);
        lamp.intensity = .85f;
        lamp.range = 4.2f * BraxClaudeOsiedle.K;
        lamp.shadows = LightShadows.None;
        lamp.renderMode = LightRenderMode.ForceVertex;
    }
}
