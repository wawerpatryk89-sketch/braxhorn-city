using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A1: ONE owner-supplied championship belt, next to the JUDGES (not the lobby).
// The parent arena uses nominal metres and applies the shared city scale K once.
// This generator changes no fighters, seats, public doors, neighbouring buildings
// or physics layers. The reference showcase is decorative; existing arena structure
// remains the physical wall and the glass/belt add no invisible collision.
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
        // Phone evidence: the original belt presents its reverse to visitors.
        // Turn the belt itself 180 degrees towards the -Z front glass; do not
        // flip the showcase, its owner artwork or other arena meshes.
        instance.transform.localRotation=Quaternion.Euler(0,180f,0)*
            instance.transform.localRotation;
        b = VisualBounds(display, instance.transform);
        float factor = Mathf.Min(1.70f / Mathf.Max(b.size.y, .0001f),
            1.30f / Mathf.Max(b.size.x, .0001f));
        instance.transform.localScale *= factor;
        b = VisualBounds(display, instance.transform);
        // Suspend the original belt above the plaque, clear of glass and leather backing.
        instance.transform.localPosition += new Vector3(0, 2.48f, .045f) - b.center;
        Debug.Log("CHAMPIONS_A1_BELT_FACES_FRONT turned180=true"+
            " width="+b.size.x.ToString("F2")+" height="+b.size.y.ToString("F2")+
            " sourceMeshUnchanged=true visualReviewPending=true");

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
        // Owner reference 7.11: ONE narrow brushed-gold plaque under the belt.
        // No oversized plinth, no rhombus and no atlas stretched across the front.
        Cube(display,"REFERENCE narrow bronze plaque outline",bronze,
            new Vector3(0,.78f,-.375f),new Vector3(1.50f,.56f,.050f));
        Cube(display,"REFERENCE solid gold plaque face",gold,
            new Vector3(0,.78f,-.408f),new Vector3(1.40f,.48f,.018f));

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
            name="A1 original BRAXHORN ARENA + B atlas | reference showcase, unedited",
            enableInstancing=true
        };
        material.SetTexture("_MainTex",image);
        material.SetColor("_Color",Color.white);
        material.SetFloat("_VertexTint",0f);
        material.SetFloat("_Roughness",.35f);
        material.SetFloat("_Metallic",0f);
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,Materials+"A1_owner_case_plaque.mat");

        AtlasLabel(display,material,"FRONT_BRAXHORN_ARENA",
            new Vector3(0,.78f,-.421f),Quaternion.Euler(0,180f,0),
            new Vector2(1.24f,.48f),.397f,.777f,.12f,.90f);
        // Preserve the owner's B crest from the same atlas as a small right-side detail.
        AtlasLabel(display,material,"RIGHT_SIDE_B_CREST",
            new Vector3(.765f,.78f,-.215f),Quaternion.Euler(0,90f,0),
            new Vector2(.26f,.36f),.791f,.938f,.12f,.90f);
        Debug.Log("CHAMPIONS_A1_PLAQUE_REFERENCE ownerPixelsUnchanged=true"+
            " narrowGoldRectangle=true frontPanels=1 sideCrest=1 source="+sourceWidth+"x"+sourceHeight);
    }

    public static void Generate(Transform arenaInterior)
    {
        // Keep the approved judges-side location. Rebuild ONLY the exhibit.
        var display = Group(arenaInterior, "A1 REFERENCE wall-mounted museum showcase | JUDGES | one 3D belt");
        display.localPosition = new Vector3(-73.9f, 0, 63.3f);
        display.localRotation = Quaternion.Euler(0, 90, 0);

        var gold = BraxChampionsFoundation.LibraryMetal("A1_belt_showcase_gold",
            new Color(.96f, .72f, .24f));
        var bronze = BraxChampionsFoundation.LibraryMetal("A1_belt_showcase_bronze",
            new Color(.54f, .38f, .17f));
        var originalGlass = AssetDatabase.LoadAssetAtPath<Material>(Materials + "clear_glass.mat");
        var warm = AssetDatabase.LoadAssetAtPath<Material>(Materials + "light.mat");
        if (!originalGlass || originalGlass.renderQueue < 3000 || !warm)
            throw new InvalidOperationException("A1 showroom requires existing transparent glass and warm light materials");

        var glass=new Material(originalGlass){
            name="A1 reference showcase | optical-clear glass",
            enableInstancing=true
        };
        glass.SetColor("_Color",new Color(.97f,.99f,1f,.032f));
        glass.SetFloat("_Roughness",.012f);
        glass.SetFloat("_Metallic",0f);
        glass.SetFloat("_VertexTint",0f);
        glass.SetFloat("_Transparent",1f);
        glass.SetFloat("_AlphaClip",0f);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0f);
        glass.SetFloat("_Cull",2f);
        glass.renderQueue=3001;
        glass.SetOverrideTag("RenderType","Transparent");
        AssetDatabase.CreateAsset(glass,Materials+"A1_case_clear_slightly_smoked_glass.mat");

        var marble = BraxChampionsFoundation.SourceMaterial(
            "A1_belt_showcase_black_marble", "granite_tile", .82f, new Color(.075f,.075f,.082f));
        var backing = BraxChampionsFoundation.LibraryLeather(
            "A1_belt_showcase_black_leather", new Color(.075f,.070f,.068f));

        // Reference photo: wall-mounted black-marble column, not a floor podium.
        Cube(display,"REFERENCE black marble wall column",marble,
            new Vector3(0,2.30f,.63f),new Vector3(2.34f,4.55f,.30f));
        Cube(display,"REFERENCE black leather back panel",backing,
            new Vector3(0,2.27f,.405f),new Vector3(1.58f,3.20f,.055f));

        // Slim crystal-clear case: front + sides + top + bottom. Decorative only.
        Cube(display,"REFERENCE optical clear glass FRONT",glass,
            new Vector3(0,2.28f,-.505f),new Vector3(1.86f,3.72f,.020f));
        foreach(float x in new[]{-.94f,.94f})
            Cube(display,"REFERENCE optical clear glass SIDE",glass,
                new Vector3(x,2.28f,-.055f),new Vector3(.020f,3.72f,.92f));
        Cube(display,"REFERENCE optical clear glass TOP",glass,
            new Vector3(0,4.15f,-.055f),new Vector3(1.90f,.020f,.94f));
        Cube(display,"REFERENCE optical clear glass BOTTOM",glass,
            new Vector3(0,.41f,-.055f),new Vector3(1.90f,.020f,.94f));

        // Warm rectangular ceiling light and vertical edge glow visible in the reference.
        foreach(float z in new[]{-.34f,.19f})
            Cube(display,"REFERENCE warm top LED long diffuser",warm,
                new Vector3(0,3.94f,z),new Vector3(1.42f,.030f,.040f));
        foreach(float x in new[]{-.69f,.69f})
            Cube(display,"REFERENCE warm top LED short diffuser",warm,
                new Vector3(x,3.94f,-.075f),new Vector3(.040f,.030f,.57f));
        foreach(float x in new[]{-.77f,.77f})
            Cube(display,"REFERENCE warm vertical side LED",warm,
                new Vector3(x,2.26f,.345f),new Vector3(.025f,3.05f,.030f));
        Cube(display,"REFERENCE warm lower case LED",warm,
            new Vector3(0,.48f,-.31f),new Vector3(1.46f,.030f,.045f));

        // Small gold clamps only. No thick gold posts and NO red stripe.
        foreach(float x in new[]{-.93f,.93f})
        foreach(float y in new[]{.53f,2.28f,4.02f})
            Cube(display,"REFERENCE small gold glass bracket",gold,
                new Vector3(x,y,-.515f),new Vector3(.12f,.12f,.10f));

        // Bronze vertical slats stay next to the cabinet, never across the glass.
        for(int i=0;i<5;i++)
            Cube(display,"REFERENCE bronze vertical slat "+(i+1),bronze,
                new Vector3(1.28f+i*.11f,2.30f,.66f),
                new Vector3(.040f,4.55f,.080f));

        OriginalRectangleArtwork(display, gold, bronze);
        OriginalBelt(display);

        var lamp = new GameObject("REFERENCE showcase warm mobile vertex light").AddComponent<Light>();
        lamp.transform.SetParent(display,false);
        lamp.transform.localPosition=new Vector3(0,3.72f,-.12f);
        lamp.type=LightType.Point;
        lamp.color=new Color(1f,.87f,.63f);
        lamp.intensity=.72f;
        lamp.range=3.8f*BraxClaudeOsiedle.K;
        lamp.shadows=LightShadows.None;
        lamp.renderMode=LightRenderMode.ForceVertex;

        Debug.Log("CHAMPIONS_A1_SHOWCASE_REFERENCE wallMounted=true rhombus=false woodPlinth=false"+
            " redInset=false clearGlass=true smallGoldBrackets=true ownerBeltUnchanged=true ownerPlaqueUnchanged=true");
    }

}
