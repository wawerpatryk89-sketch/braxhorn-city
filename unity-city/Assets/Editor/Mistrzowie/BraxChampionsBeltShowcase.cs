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

    static Material SolidPbr(string id,Color color,float roughness,float metallic)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 showcase CityStaticPBR shader missing");
        string path=Materials+id+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m)
        {
            m=new Material(shader){name=id,enableInstancing=true};
            AssetDatabase.CreateAsset(m,path);
        }
        m.SetColor("_Color",color);
        m.SetFloat("_Roughness",roughness);
        m.SetFloat("_Metallic",metallic);
        m.SetFloat("_VertexTint",0f);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        EditorUtility.SetDirty(m);
        return m;
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

    static void RoundedBase(Transform parent,string name,Material material,
        Vector3 centre,Vector3 size,float radius)
    {
        const int cornerSegments=6;
        float hx=size.x*.5f,hz=size.z*.5f,hy=size.y*.5f;
        float r=Mathf.Clamp(radius,.001f,Mathf.Min(hx,hz)-.001f);
        var outline=new System.Collections.Generic.List<Vector2>();
        void Arc(float cx,float cz,float a0,float a1)
        {
            for(int i=0;i<=cornerSegments;i++)
            {
                float a=Mathf.Lerp(a0,a1,i/(float)cornerSegments)*Mathf.Deg2Rad;
                outline.Add(new Vector2(cx+r*Mathf.Cos(a),cz+r*Mathf.Sin(a)));
            }
        }
        Arc(hx-r,hz-r,0,90);Arc(-hx+r,hz-r,90,180);
        Arc(-hx+r,-hz+r,180,270);Arc(hx-r,-hz+r,270,360);
        var verts=new System.Collections.Generic.List<Vector3>();
        var tris=new System.Collections.Generic.List<int>();
        for(int i=0;i<outline.Count;i++)
        {
            int n=(i+1)%outline.Count,b=verts.Count;
            verts.Add(centre+new Vector3(outline[i].x,-hy,outline[i].y));
            verts.Add(centre+new Vector3(outline[n].x,-hy,outline[n].y));
            verts.Add(centre+new Vector3(outline[n].x,hy,outline[n].y));
            verts.Add(centre+new Vector3(outline[i].x,hy,outline[i].y));
            tris.Add(b);tris.Add(b+2);tris.Add(b+1);tris.Add(b);tris.Add(b+3);tris.Add(b+2);
        }
        for(int i=0;i<outline.Count;i++)
        {
            int n=(i+1)%outline.Count,b=verts.Count;
            verts.Add(centre+new Vector3(0,hy,0));
            verts.Add(centre+new Vector3(outline[i].x,hy,outline[i].y));
            verts.Add(centre+new Vector3(outline[n].x,hy,outline[n].y));
            tris.Add(b);tris.Add(b+1);tris.Add(b+2);
            b=verts.Count;
            verts.Add(centre+new Vector3(0,-hy,0));
            verts.Add(centre+new Vector3(outline[n].x,-hy,outline[n].y));
            verts.Add(centre+new Vector3(outline[i].x,-hy,outline[i].y));
            tris.Add(b);tris.Add(b+1);tris.Add(b+2);
        }
        var mesh=new Mesh{name=name+" rounded base R="+radius};
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string safe=name.Replace(' ','_').Replace('|','_');
        AssetDatabase.CreateAsset(mesh,Meshes+safe+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        // Exhibit base is decorative beside judges: no invisible movement blocker.
    }

    // Single plane per glass wall rather than six faces of a Unity cube.
    // The old six-face transparent cubes layered alpha, producing a white
    // veil in front of the owner's belt on mobile. Glass must read as almost
    // invisible while edges and the real belt remain visible.
    static void CrystalGlassPanel(Transform parent,string name,Material material,
        Vector3 centre,Vector2 dimensions,Quaternion facing)
    {
        var panel=GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name=name;
        panel.transform.SetParent(parent,false);
        panel.transform.localPosition=centre;
        panel.transform.localRotation=facing;
        panel.transform.localScale=new Vector3(dimensions.x,dimensions.y,1f);
        var renderer=panel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
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
        // MODEL-DRIVEN pose correction: the owner's screenshots showed only
        // the thin EDGE of the belt. A blind 90deg tilt + 180deg yaw does not
        // prove the embossed face points at the viewer. Inspect all 24 rigid
        // right-angle orientations of the UNMODIFIED FBX and choose the one
        // which is tall, widest from FRONT, and thinnest through glass.
        // This changes ONLY the instance transform, never geometry/materials.
        var authoredRotation=instance.transform.localRotation;
        Quaternion bestRotation=authoredRotation;
        float bestScore=float.PositiveInfinity;
        Bounds bestBounds=new Bounds();
        int candidates=0;
        Vector3[] axes={Vector3.up,Vector3.down,Vector3.right,Vector3.left,
            Vector3.forward,Vector3.back};
        foreach(var up in axes)
        foreach(var forward in axes)
        {
            if(Mathf.Abs(Vector3.Dot(up,forward))>.1f)continue;
            var candidate=Quaternion.LookRotation(forward,up)*authoredRotation;
            instance.transform.localRotation=candidate;
            var pose=VisualBounds(display,instance.transform);
            float h=pose.size.y,w=pose.size.x,d=pose.size.z;
            if(h<.0001f||w<.0001f||d<.0001f)continue;
            // A real belt is a vertical portrait, BROAD when seen through
            // front glass and shallow in depth. Side-on poses score badly.
            float portrait=h/w,edge=d/w;
            float score=Mathf.Abs(Mathf.Log(portrait/2.35f))*2.8f+
                edge*6f;
            if(h<=w*1.30f)score+=20f;
            if(d>=w)score+=20f;
            // When a meaningful centre-gold-plate renderer exists, prefer
            // the embossed plate closer to the viewer (front is local -Z).
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)
            {
                string name=renderer.gameObject.name.ToLowerInvariant();
                if(!(name.Contains("plate")||name.Contains("buckle")||
                     name.Contains("medallion")||name.Contains("emblem")))continue;
                Vector3 plate=display.InverseTransformPoint(renderer.bounds.center);
                float frontOffset=(plate.z-pose.center.z)/Mathf.Max(d,.001f);
                score+=Mathf.Clamp(frontOffset,-.5f,.5f)*.35f;
                break;
            }
            candidates++;
            if(score<bestScore-.0001f)
            {
                bestScore=score;
                bestRotation=candidate;
                bestBounds=pose;
            }
        }
        if(candidates!=24)
            throw new InvalidOperationException("A1 belt orthogonal pose search incomplete");
        // Phone proof 8.11 showed the embossed face presented backwards after the
        // geometric 24-pose search. Preserve that upright pose, then flip only the
        // local display-facing axis; source mesh/materials remain unchanged.
        // Owner phone proof: reverse the belt around ITS SELECTED LOCAL UP axis.
        // Post-multiply; pre-multiplying rotated around the display frame and could
        // leave the embossed face backwards after an authored FBX axis correction.
        bestRotation=bestRotation*Quaternion.Euler(0,180f,0);
        instance.transform.localRotation=bestRotation;
        var b=VisualBounds(display,instance.transform);
        if(b.size.y<=b.size.x*1.30f||b.size.z>=b.size.x)
            BraxBuildQuality.Warn("A1_BELT_GEOMETRY_UNCERTAIN: source FBX must be inspected in Unity");
        // New lightweight case interior is 1.84 x .72 x 2.95 m above
        // a .28m base. Keep >=15cm side/top clearance and >=10cm front/back.
        float factor=Mathf.Min(2.25f/Mathf.Max(b.size.y,.0001f),
            Mathf.Min(1.54f/Mathf.Max(b.size.x,.0001f),
                .52f/Mathf.Max(b.size.z,.0001f)));
        instance.transform.localScale*=factor;
        b=VisualBounds(display,instance.transform);
        instance.transform.localPosition+=new Vector3(0,1.80f,.02f)-b.center;
        b=VisualBounds(display,instance.transform);
        if(b.min.y<.43f||b.max.y>3.08f||
           b.min.z<-.26f||b.max.z>.26f||b.size.x>1.54f)
            BraxBuildQuality.Warn("A1_BELT_CASE_CLEARANCE: imported FBX exceeds approved lightweight case clearance");
        Debug.Log("A1_BELT_OWNER_3D_FRONT_PRESENTATION candidateSearch="+candidates+
            " score="+bestScore.ToString("F3")+
            " dimensions="+b.size.ToString("F3")+
            " rotation="+bestRotation.eulerAngles.ToString("F1")+
            " originalSourceMeshUnchanged=true phoneProofFrontFlip=true visualReviewPending=true");

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
        // Original owner artwork lives on the FRONT of the new low base:
        // 1.20 x .40 m (3:1) with a 35mm polished-gold frame.
        Cube(display,"A1 SHOWCASE | gold plaque frame",gold,
            new Vector3(0,.31f,-.462f),new Vector3(1.27f,.47f,.020f));
        Cube(display,"A1 SHOWCASE | black plaque recess",bronze,
            new Vector3(0,.31f,-.474f),new Vector3(1.20f,.40f,.010f));

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

        // Both label quads face outwards: FRONT yaw=180 and RIGHT yaw=90.
        // For those two outward-facing views mesh +U points to the viewer's
        // LEFT (Unity Right=Cross(Up,LookDirection)). The original artwork
        // looked mirror-reversed. Swap UV ENDPOINTS only, never the owner's
        // 1536x512 source pixels, logo, text, colour or the physical plaques.
        AtlasLabel(display,material,"FRONT_BRAXHORN_ARENA",
            new Vector3(0,.31f,-.482f),Quaternion.Euler(0,180f,0),
            new Vector2(1.20f,.40f),.777f,.397f,.12f,.90f);
        // Original owner's separate B emblem belongs on the RIGHT SIDE of the base.
        AtlasLabel(display,material,"RIGHT_SIDE_B_CREST",
            new Vector3(1.112f,.30f,.05f),Quaternion.Euler(0,90f,0),
            new Vector2(.26f,.34f),.938f,.791f,.12f,.90f);
        Debug.Log("CHAMPIONS_A1_PLAQUE_REFERENCE ownerPixelsUnchanged=true"+
            " narrowGoldRectangle=true frontPanels=1 sideCrest=1 source="+sourceWidth+"x"+sourceHeight);
    }

    public static void Generate(Transform arenaInterior)
    {
        // Keep the approved judges-side location. Rebuild ONLY the exhibit.
        var display = Group(arenaInterior, "A1 REFERENCE wall-mounted museum showcase | JUDGES | one 3D belt");
        display.localPosition = new Vector3(-73.9f, 0, 63.3f);
        display.localRotation = Quaternion.Euler(0, 90, 0);

        var gold=SolidPbr("A1_belt_showcase_clean_gold",
            new Color(.835f,.639f,.231f),.19f,.98f);
        var blackMetal=SolidPbr("A1_belt_showcase_clean_black_metal",
            new Color(.045f,.050f,.060f),.22f,.92f);
        var plaqueRecess=SolidPbr("A1_belt_showcase_plaque_recess",
            new Color(.035f,.037f,.042f),.24f,.35f);
        var originalGlass = AssetDatabase.LoadAssetAtPath<Material>(Materials + "clear_glass.mat");
        var warm = AssetDatabase.LoadAssetAtPath<Material>(Materials + "light.mat");
        if (!originalGlass || originalGlass.renderQueue < 3000 || !warm)
            throw new InvalidOperationException("A1 showroom requires existing transparent glass and warm light materials");

        var glass=new Material(originalGlass){
            name="A1 premium showcase | optical clear slightly smoked glass",
            enableInstancing=true
        };
        glass.SetColor("_Color",new Color(.94f,.96f,.98f,.025f));
        glass.SetFloat("_Roughness",.035f);
        glass.SetFloat("_Metallic",0f);
        glass.SetFloat("_VertexTint",0f);
        glass.SetFloat("_Transparent",1f);
        glass.SetFloat("_AlphaClip",0f);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0f);
        glass.SetFloat("_Cull",0f);
        glass.renderQueue=3001;
        glass.SetOverrideTag("RenderType","Transparent");
        AssetDatabase.CreateAsset(glass,Materials+"A1_case_clear_slightly_smoked_glass.mat");

        var granite=BraxChampionsFoundation.SourceMaterial(
            "A1_belt_showcase_granite_tile","granite_tile",.82f,
            new Color(.12f,.125f,.14f));
        granite.SetFloat("_Roughness",.26f);

        // LOW two-layer base only. NO tall black wall, NO leather backing plate.
        RoundedBase(display,"A1 SHOWCASE | recessed lower granite plinth",granite,
            new Vector3(0,.04f,0),new Vector3(2.08f,.08f,.78f),.025f);
        RoundedBase(display,"A1 SHOWCASE | premium granite base",granite,
            new Vector3(0,.18f,0),new Vector3(2.20f,.20f,.90f),.025f);

        // Lightweight 1.84 x .72 x 2.95 m glass case above y=.28.
        const float caseY=.28f+2.95f*.5f;
        CrystalGlassPanel(display,"A1 SHOWCASE | FRONT optical glass",glass,
            new Vector3(0,caseY,-.36f),new Vector2(1.84f,2.95f),
            Quaternion.Euler(0,180f,0));
        CrystalGlassPanel(display,"A1 SHOWCASE | BACK optical glass",glass,
            new Vector3(0,caseY,.36f),new Vector2(1.84f,2.95f),
            Quaternion.identity);
        foreach(float side in new[]{-1f,1f})
            CrystalGlassPanel(display,"A1 SHOWCASE | SIDE optical glass "+side,glass,
                new Vector3(side*.92f,caseY,0),new Vector2(.72f,2.95f),
                Quaternion.Euler(0,side*90f,0));
        CrystalGlassPanel(display,"A1 SHOWCASE | TOP optical glass",glass,
            new Vector3(0,3.23f,0),new Vector2(1.84f,.72f),
            Quaternion.Euler(90f,0,0));

        // Four 45mm gold corner profiles + 55mm top frame; glass and belt remain dominant.
        foreach(float x in new[]{-.92f,.92f})
        foreach(float z in new[]{-.36f,.36f})
            Cube(display,"A1 SHOWCASE | slender gold corner profile",gold,
                new Vector3(x,caseY,z),new Vector3(.045f,2.95f,.045f));
        foreach(float z in new[]{-.36f,.36f})
            Cube(display,"A1 SHOWCASE | top gold front/back frame",gold,
                new Vector3(0,3.2025f,z),new Vector3(1.84f,.055f,.045f));
        foreach(float x in new[]{-.92f,.92f})
            Cube(display,"A1 SHOWCASE | top gold side frame",gold,
                new Vector3(x,3.2025f,0),new Vector3(.045f,.055f,.72f));

        // Two black 30mm supports behind the belt, invisible from a straight-on view.
        foreach(float x in new[]{-.34f,.34f})
            Cube(display,"A1 SHOWCASE | hidden black belt support",blackMetal,
                new Vector3(x,1.22f,.20f),new Vector3(.030f,1.88f,.030f));

        // Warm 3000K-style emissive lines: one upper, one lower. No shadow light strips.
        Cube(display,"A1 SHOWCASE | upper warm LED",warm,
            new Vector3(0,3.165f,-.31f),new Vector3(1.56f,.028f,.020f));
        Cube(display,"A1 SHOWCASE | lower warm LED",warm,
            new Vector3(0,.315f,-.31f),new Vector3(1.50f,.020f,.020f));

        OriginalRectangleArtwork(display,gold,plaqueRecess);
        OriginalBelt(display);

        // One cheap mobile light only.
        var lamp = new GameObject("A1 SHOWCASE | one warm mobile vertex light").AddComponent<Light>();
        lamp.transform.SetParent(display,false);
        lamp.transform.localPosition=new Vector3(0,2.72f,-.05f);
        lamp.type=LightType.Point;
        lamp.color=new Color(1f,.87f,.63f);
        lamp.intensity=.62f;
        lamp.range=3.3f*BraxClaudeOsiedle.K;
        lamp.shadows=LightShadows.None;
        lamp.renderMode=LightRenderMode.ForceVertex;

        Debug.Log("A1_SHOWCASE_PREMIUM base=2.20x.90x.28 lower=2.08x.78x.08 R=.025"+
            " case=1.84x.72x2.95 totalHeight=3.23 glass=10to12mm_visual"+
            " tallBackPlate=false leatherBacking=false goldProfiles=.045 topFrame=.055"+
            " beltTargetHeight<=2.25 beltCenterY=1.80 localAxisFrontFlip=true"+
            " frontPlaque=1.20x.40 goldFrame=.035 sideCrest=.26x.34"+
            " LEDtop=.028 LEDbottom=.020 realtimeLights=1 visualFrontProofRequired=true");
    }

}
