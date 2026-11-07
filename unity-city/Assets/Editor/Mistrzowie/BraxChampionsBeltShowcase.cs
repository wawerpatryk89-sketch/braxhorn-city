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
// or physics layers. Only the visible case plinth has physical collision.
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

    // Render a REGION of the owner's original 3:1 label atlas. Never rewrite,
    // generate, crop, stretch the source pixels into a new image, or change the
    // artwork. The atlas has two front emblems and a separate B side emblem.
    // The old code displayed all of them together on a single FRONT quad.
    static void AtlasLabel(Transform display, Material material,
        string id, Vector3 position, Quaternion rotation,
        Vector2 size, float u0, float u1, float v0, float v1)
    {
        var mesh = new Mesh { name = "A1 owner original atlas UV | " + id };
        mesh.vertices = new[] {
            new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),
            new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)
        };
        mesh.uv = new[] {
            new Vector2(u0,v0),new Vector2(u1,v0),
            new Vector2(u1,v1),new Vector2(u0,v1)
        };
        mesh.triangles = new[] {0,1,2,0,2,3};
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Meshes+"A1_owner_case_"+id+"_UV.asset");
        var label = new GameObject("OWNER ORIGINAL LABEL | "+id);
        label.transform.SetParent(display,false);
        label.transform.localPosition=position;
        label.transform.localRotation=rotation;
        label.transform.localScale=new Vector3(size.x,size.y,1f);
        label.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=label.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        // Printed image only; no collider or hidden obstructing panel.
    }

    static void OriginalRectangleArtwork(Transform display, Material gold, Material bronze)
    {
        // Narrow solid GOLD rectangular plate below the championship belt.
        // Front and right-hand SIDE are two distinct surfaces, not one atlas
        // stretched over the front. Both are inside the crystal glass case.
        Cube(display,"BRAXHORN | narrower bronze plate outline",bronze,
            new Vector3(0,1.075f,-.239f),new Vector3(1.88f,.88f,.12f));
        Cube(display,"SOLID GOLD RECTANGLE for owner BRAXHORN ARENA sticker",gold,
            new Vector3(0,1.075f,-.321f),new Vector3(1.78f,.80f,.135f));
        Cube(display,"SOLID GOLD SIDE RECTANGLE for owner B emblem",gold,
            new Vector3(.920f,1.075f,-.321f),new Vector3(.075f,.78f,.65f));

        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalPlaque);
        if(!image)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_PENDING: original label atlas missing: "+OriginalPlaque);
            return;
        }
        var importer=AssetImporter.GetAtPath(OriginalPlaque) as TextureImporter;
        if(!importer)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_IMPORTER_MISSING: "+OriginalPlaque);
            return;
        }
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth!=1536||sourceHeight!=512)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_OWNER_IMAGE_WRONG_SOURCE_SIZE: expected 1536x512, source="+
                sourceWidth+"x"+sourceHeight+" "+OriginalPlaque);
            return;
        }
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 original label PBR shader missing");
        var material=new Material(shader) {
            name="A1 original BRAXHORN ARENA + B atlas | front and side, unedited",
            enableInstancing=true
        };
        material.SetTexture("_MainTex",image);
        material.SetColor("_Color",Color.white);
        material.SetFloat("_VertexTint",0f);
        material.SetFloat("_Roughness",.35f);
        material.SetFloat("_Metallic",0f);
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,Materials+"A1_owner_case_plaque.mat");

        // Source atlas proportions: second BRAXHORN ARENA emblem, then side B.
        // UV masks select only existing artwork; no image is regenerated.
        // The front print faces towards the front glass at local -Z.
        AtlasLabel(display,material,"FRONT_BRAXHORN_ARENA",
            new Vector3(0,1.075f,-.403f),Quaternion.Euler(0,180f,0),
            new Vector2(1.59f,.73f),.397f,.777f,.12f,.90f);
        // The B emblem belongs exclusively on the RIGHT SIDE (+X).
        AtlasLabel(display,material,"RIGHT_SIDE_B_CRESt",
            new Vector3(.970f,1.075f,-.321f),Quaternion.Euler(0,90f,0),
            new Vector2(.52f,.70f),.791f,.938f,.12f,.90f);
        Debug.Log("CHAMPIONS_A1_PLAQUE_ATLAS_OWNER_ORIGINAL front=BRAXHORN_ARENA"+
            " side=B frontPanels=1 sidePanels=1 ownerPixelsUnchanged=true"+
            " narrowedGoldRectangle=true source="+sourceWidth+"x"+sourceHeight);
    }

    public static void Generate(Transform arenaInterior)
    {
        // Judges' table occupies x=-78.7..-78.0 and z=61.0..65.6.
        // Visible podium stands beside it at x=-73.9: NOT the 6 m centre
        // entrance aisle, NOT the octagon, NOT the VIP chairs at z=70/72.
        var display = Group(arenaInterior, "A1 PREMIUM museum showcase | JUDGES | one 3D belt");
        display.localPosition = new Vector3(-73.9f, 0, 63.3f);
        display.localRotation = Quaternion.Euler(0, 90, 0); // front faces cage/judges
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

        // WZÓR WŁAŚCICIELA 6.11: pionowa gablota ze szkła przed filarem z czarnego
        // marmuru, ciemne skórzane tło, złote ramy LED u góry i u dołu, złote uchwyty
        // w narożnikach szkła, mosiężna tabliczka, brązowe lamele obok.
        // Doprecyzowanie właściciela: podstawa ma prawdziwe drewniane wykończenie.
        var marble = BraxChampionsFoundation.SourceMaterial(
            "A1_belt_showcase_black_marble", "granite_tile", .9f, new Color(.13f, .13f, .14f));
        var backing = BraxChampionsFoundation.LibraryLeather("A1_belt_showcase_black_leather",
            new Color(.10f, .095f, .09f));
        // Reuse the existing native 2K teak PBR source; do not manufacture a
        // flat brown substitute or change the owner's belt/plaque textures.
        var wood = BraxChampionsPoolP1Pbr2K.Create(
            "Assets/PlayableGenerated/Mistrzowie/Buildings", "A1_belt_showcase_wood_base",
            "teak_veneer", 1.5f, Color.white);
        wood.name = "A1 showcase | original teak veneer PBR | wood base";
        EditorUtility.SetDirty(wood);
        // Wood-finished structural plinth, original marble pillar and leather back.
        // Keep the existing dimensions and one matching physical plinth collider.
        Cube(display, "Wood veneer showcase plinth", wood,
            new Vector3(0, .30f, .05f), new Vector3(2.90f, .60f, 1.30f));
        var plinthCollider = new GameObject("Wood veneer showcase plinth collision").AddComponent<BoxCollider>();
        plinthCollider.transform.SetParent(display, false);
        plinthCollider.center = new Vector3(0, .30f, .05f);
        plinthCollider.size = new Vector3(2.90f, .60f, 1.30f);
        Cube(display, "Black marble pillar behind the showcase", marble,
            new Vector3(0, 2.45f, .78f), new Vector3(3.30f, 4.90f, .32f));
        Cube(display, "Black leather back panel", backing,
            new Vector3(0, 2.0f, .555f), new Vector3(2.52f, 2.74f, .06f));
        Cube(display, "Wood veneer case base", wood,
            new Vector3(0, .63f, 0), new Vector3(2.66f, .06f, 1.16f));
        // Szkło z pięciu stron (front, boki, góra), bez kolizji.
        Cube(display, "Museum GLASS FRONT no invisible collider",
            glass, new Vector3(0, 2.0f, -.593f), new Vector3(2.57f, 2.79f, .03f));
        foreach (float x in new[] { -1.302f, 1.302f })
            Cube(display, "Museum GLASS SIDE no invisible collider",
                glass, new Vector3(x, 2.0f, 0), new Vector3(.03f, 2.79f, 1.16f));
        Cube(display, "Museum GLASS TOP", glass,
            new Vector3(0, 3.41f, 0), new Vector3(2.60f, .03f, 1.19f));
        // Złota rama LED u góry (prostokąt) i świecący pas u dołu.
        foreach (float z in new[] { -.50f, .45f })
            Cube(display, "Gold LED top frame long", gold,
                new Vector3(0, 3.30f, z), new Vector3(2.30f, .06f, .07f));
        foreach (float x in new[] { -1.12f, 1.12f })
            Cube(display, "Gold LED top frame short", gold,
                new Vector3(x, 3.30f, -.025f), new Vector3(.07f, .06f, 1.02f));
        // Złote uchwyty szkła w ośmiu narożnikach.
        foreach (float x in new[] { -1.30f, 1.30f })
            foreach (float y in new[] { .70f, 3.36f })
                foreach (float z in new[] { -.59f, .55f })
                    Cube(display, "Gold glass corner bracket", gold,
                        new Vector3(x, y, z), new Vector3(.13f, .13f, .13f));
        // Brązowe pionowe lamele po prawej stronie filara.
        for (int i = 0; i < 7; i++)
            Cube(display, "Bronze vertical slat " + (i + 1), bronze,
                new Vector3(1.85f + i * .13f, 2.45f, .82f), new Vector3(.06f, 4.90f, .10f));
        // Pas i tabliczka z grafiką właściciela na ciemnym skórzanym tle.
        OriginalRectangleArtwork(display, gold, bronze);
        OriginalBelt(display);
        // Four warm diffuser strips under the existing gold rectangle, as in
        // the reference. They are emission geometry, not extra real-time lights.
        foreach (float z in new[] { -.50f, .45f })
            Cube(display, "Warm LED rectangle long diffuser", warm,
                new Vector3(0, 3.262f, z), new Vector3(2.20f, .012f, .045f));
        foreach (float x in new[] { -1.12f, 1.12f })
            Cube(display, "Warm LED rectangle short diffuser", warm,
                new Vector3(x, 3.262f, -.025f), new Vector3(.045f, .012f, .94f));
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
