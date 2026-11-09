using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A1: ONE owner-supplied championship belt in the owner-photo vitrine (lobby, 9.11).
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
        Vector3 centre,Vector3 size,float radius,bool collision=false)
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
        if(collision)
        {
            // This flag is used only by the visible 2.20x0.90 showcase base.
            // One collider matches the COMPLETE 0.28m two-layer base, not only
            // its 0.20m upper stone skin.
            var box=go.AddComponent<BoxCollider>();
            box.center=new Vector3(centre.x,.14f,centre.z);
            box.size=new Vector3(size.x,.28f,size.z);
        }
    }

    // True 12mm visual thickness with ONE transparent front face plus four
    // physical edge faces. No rear transparent face, so mobile alpha is not
    // doubled into a milky pane when viewed head-on.
    // The old six-face transparent cubes layered alpha, producing a white
    // veil in front of the owner's belt on mobile. Glass must read as almost
    // invisible while edges and the real belt remain visible.
    static void CrystalGlassPanel(Transform parent,string name,Material material,
        Vector3 centre,Vector2 dimensions,Quaternion facing)
    {
        const float thickness=.012f; // 12 mm, approved range 10-12 mm
        float hx=dimensions.x*.5f,hy=dimensions.y*.5f,hz=thickness*.5f;
        var mesh=new Mesh{name=name+" | 12mm optical glass"};
        mesh.vertices=new[]{
            new Vector3(-hx,-hy,hz),new Vector3(hx,-hy,hz),
            new Vector3(hx,hy,hz),new Vector3(-hx,hy,hz),
            new Vector3(-hx,-hy,-hz),new Vector3(hx,-hy,-hz),
            new Vector3(hx,hy,-hz),new Vector3(-hx,hy,-hz)
        };
        mesh.uv=new[]{
            new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),
            new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)
        };
        // Front only + four thickness edges. Material is double-sided, therefore
        // the pane remains readable from inside without a second alpha layer.
        mesh.triangles=new[]{
            0,1,2,0,2,3,
            0,4,5,0,5,1,
            1,5,6,1,6,2,
            2,6,7,2,7,3,
            3,7,4,3,4,0
        };
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        string safe=name.Replace(' ','_').Replace('|','_').Replace('/','_');
        AssetDatabase.CreateAsset(mesh,Meshes+"A1_glass_"+safe+".asset");
        var panel=new GameObject(name);
        panel.transform.SetParent(parent,false);
        panel.transform.localPosition=centre;
        panel.transform.localRotation=facing;
        panel.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=panel.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        // Showcase glass is decorative; the physical low base is the obstacle.
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
        OriginalBelt(display,2.25f,1.54f,.52f,new Vector3(0,1.80f,.02f),.43f,3.08f,-.26f,.26f);
    }

    // Fit the owner's FBX (uniform scale only) into a target box in display-
    // local nominal metres; returns the final belt bounds for brackets.
    static Bounds OriginalBelt(Transform display,float maxH,float maxW,float maxD,Vector3 centre,
        float minY,float maxY,float minZ,float maxZ)
    {
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(SourceBelt);
        if (!original)
        {
            BraxBuildQuality.Warn("A1_SHOWCASE_BELT_MISSING: original owner's FBX was not imported: " + SourceBelt);
            return new Bounds(centre,Vector3.zero);
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
        float factor=Mathf.Min(maxH/Mathf.Max(b.size.y,.0001f),
            Mathf.Min(maxW/Mathf.Max(b.size.x,.0001f),
                maxD/Mathf.Max(b.size.z,.0001f)));
        instance.transform.localScale*=factor;
        b=VisualBounds(display,instance.transform);
        instance.transform.localPosition+=centre-b.center;
        b=VisualBounds(display,instance.transform);
        if(b.min.y<minY||b.max.y>maxY||
           b.min.z<minZ||b.max.z>maxZ||b.size.x>maxW+.001f)
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
        return VisualBounds(display,instance.transform);
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

    // OWNER PHOTO 9.11 (attachment d014d6e6-image.png, 1024x1536): "pas ma być
    // w tej kopule co na zdjęciu", placed where it works best -> the public
    // LOBBY, right of the red carpet, front (-Z) facing the entrance doors.
    // Sizes are MEASURED on the photo, not guessed. Ruler = the belt itself:
    // UFC Legacy championship belt, length 50 in = 1.27 m (ufc.com), which is
    // 962 px on the photo -> 1.32 mm per pixel. A frontal photo cannot show
    // depth: case depth = belt depth + LED tray clearance. The photo crops the
    // pillar top, so its height is NOT measurable: 3.00 m is a design value
    // (owner may set it), the rest is measured.
    // The display is scaled by K ONCE, so every number below is real metres.
    // Owner 9.11 (phone): "pas ma stać w dobrym miejscu, po co ta deska obok".
    // The photo shows a WALL vitrine, not a pillar standing in the lobby. It
    // now hangs on the hall front wall, RIGHT of the hall door as you walk the
    // red carpet in, back face 5 mm in front of the mural print. Door half
    // width 2.05 + gold portal .18 + 1.24 clear + half pillar .53 = x 4.0.
    // Mural print face in building space: 63.3-(lobby wall face+.03) = -35.435.
    public const float WallMuralFaceZ=63.3f-(BraxChampionsArena.FrontWallLobbyFaceZ+.03f);
    public const float LobbyX=4.0f, LobbyZ=WallMuralFaceZ-PylonD-.005f;
    public const float BeltLength=1.27f;                        // ufc.com: 50 in
    public const float CaseW=.63f, CaseH=1.79f, CaseD=.28f;     // 478 x 1358 px
    public const float CaseBottom=.185f;                        // 140 px above floor
    public const float ShelfAbove=.28f;                         // glass shelf, 213 px
    public const float PylonW=1.06f, PylonD=.45f, PylonH=3.00f; // 801 px wide
    public const float PlaqueW=.46f, PlaqueH=.18f;              // 347 x 135 px
    // Photo ratios relative to the hung belt length (pins and strap brackets).
    const float PinOffsetX=.0697f, TopPinBelowTop=.132f, BottomPinAboveBottom=.10f;

    static string Safe(string name)=>name.Replace(' ','_').Replace('|','_').Replace('/','_').Replace('.','_');

    // Unity's primitive cube has 0..1 UV on every face: a 4K marble would be
    // stretched differently on each side. Metric UV = texture repeat in metres.
    static GameObject MetricCube(Transform parent,string name,Material material,
        Vector3 position,Vector3 size,bool solid)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=position;go.transform.localScale=size;
        var filter=go.GetComponent<MeshFilter>();
        var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
        var v=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;
        for(int i=0;i<uv.Length;i++)
        {
            var p=v[i];
            if(Mathf.Abs(n[i].y)>.7f)uv[i]=new Vector2((p.x+.5f)*size.x,(p.z+.5f)*size.z);
            else if(Mathf.Abs(n[i].x)>.7f)uv[i]=new Vector2((p.z+.5f)*size.z,(p.y+.5f)*size.y);
            else uv[i]=new Vector2((p.x+.5f)*size.x,(p.y+.5f)*size.y);
        }
        mesh.uv=uv;mesh.name=name+" | metric UV";mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,Meshes+"A1_vitrine_"+Safe(name)+".asset");
        filter.sharedMesh=mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void EngravedText(Transform parent,string text,Vector3 at,float lineHeight,Color color)
    {
        var o=new GameObject("A1 VITRINE | engraved text | "+text);
        o.transform.SetParent(parent,false);o.transform.localPosition=at;
        // Phone proof b33: with identity rotation the engraving read MIRRORED
        // from the case front (-Z). Yaw 180 puts the readable side on -Z.
        o.transform.localRotation=Quaternion.Euler(0,180f,0);
        var t=o.AddComponent<TextMesh>();t.text=text;
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize=64;t.characterSize=lineHeight*10f/64f;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
        var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
    }

    public static void GenerateLobby(Transform arrivalRoot)
    {
        float k=BraxClaudeOsiedle.K;
        var display=Group(arrivalRoot,"A1 LOBBY | owner-photo belt vitrine | black marble pillar | ONE 3D belt");
        display.localPosition=new Vector3(LobbyX,0,LobbyZ)*k;
        display.localRotation=Quaternion.identity; // front (-Z) faces the lobby / entrance doors
        display.localScale=Vector3.one*k;
        // Stand on the real lobby floor (global rule §3: raycast down).
        Physics.SyncTransforms();
        var probe=display.position+Vector3.up*6f*k;
        float floorY=.245f*k,expected=.245f*k,best=float.PositiveInfinity;
        bool onFloor=false;
        foreach(var hit in Physics.RaycastAll(probe,Vector3.down,12f*k,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.normal.y<=.7f)continue;
            float y=arrivalRoot.InverseTransformPoint(hit.point).y;
            // Lobby floor = the walkable surface nearest the paving height,
            // never the upper lounge slab (18 m) above.
            if(Mathf.Abs(y-expected)<best&&Mathf.Abs(y-expected)<1.2f*k){best=Mathf.Abs(y-expected);floorY=y;onFloor=true;}
        }
        display.localPosition=new Vector3(LobbyX*k,onFloor?floorY:.245f*k,LobbyZ*k);
        if(!onFloor)BraxBuildQuality.Warn("A1_VITRINE_FLOOR_RAYCAST_MISS: lobby floor not found, using paving height .245");

        // ---- materials (photo colours sampled from the reference) ----
        var gold=SolidPbr("A1_belt_showcase_clean_gold",new Color(.835f,.639f,.231f),.19f,.98f);
        var brass=SolidPbr("A1_vitrine_brushed_brass",new Color(.78f,.62f,.30f),.34f,.95f);
        var dark=SolidPbr("A1_belt_showcase_plaque_recess",new Color(.035f,.037f,.042f),.24f,.35f);
        Material marble;
        bool veinedMarble=true;
        try
        {
            // Black marble with thin white veins, 3 m texture repeat -> sparse
            // veins like the photo. Albedo nudged warm/dark to the photo (49,40,38).
            marble=BraxChampionsFoundation.LibraryMarble("A1_vitrine_black_marble_veined",new Color(.86f,.80f,.78f),3.0f);
        }
        catch(Exception e)
        {
            veinedMarble=false;
            BraxBuildQuality.Warn("A1_VITRINE_MARBLE016_MISSING: "+e.Message+
                " -> dark granite_tile until prepare_assets.py restores ambientCG Marble016");
            marble=BraxChampionsFoundation.SourceMaterial("A1_vitrine_dark_stone_fallback","granite_tile",.82f,new Color(.12f,.12f,.13f));
        }
        var originalGlass=AssetDatabase.LoadAssetAtPath<Material>(Materials+"clear_glass.mat");
        var warm=AssetDatabase.LoadAssetAtPath<Material>(Materials+"light.mat");
        if(!originalGlass||originalGlass.renderQueue<3000||!warm)
            throw new InvalidOperationException("A1 vitrine requires existing transparent glass and warm light materials");
        var glass=new Material(originalGlass){name="A1 vitrine | optical clear glass",enableInstancing=true};
        glass.SetColor("_Color",new Color(.94f,.96f,.98f,.025f));
        glass.SetFloat("_Roughness",.035f);glass.SetFloat("_Metallic",0f);glass.SetFloat("_VertexTint",0f);
        glass.SetFloat("_Transparent",1f);glass.SetFloat("_AlphaClip",0f);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0f);glass.SetFloat("_Cull",0f);
        glass.renderQueue=3001;glass.SetOverrideTag("RenderType","Transparent");
        AssetDatabase.CreateAsset(glass,Materials+"A1_vitrine_clear_glass.mat");

        // 1) Black marble pillar straight to the floor (photo: no plinth),
        //    front face at z=0. Its collider is the one real obstacle.
        MetricCube(display,"A1 VITRINE | black marble pillar",marble,
            new Vector3(0,PylonH*.5f,PylonD*.5f),new Vector3(PylonW,PylonH,PylonD),true);

        // 2) Frameless 12 mm glass case on the marble face.
        float top=CaseBottom+CaseH,yc=CaseBottom+CaseH*.5f,front=-CaseD;
        CrystalGlassPanel(display,"A1 VITRINE | FRONT glass",glass,new Vector3(0,yc,front),new Vector2(CaseW,CaseH),Quaternion.Euler(0,180f,0));
        foreach(float side in new[]{-1f,1f})
            CrystalGlassPanel(display,"A1 VITRINE | SIDE glass "+side,glass,new Vector3(side*CaseW*.5f,yc,front*.5f),new Vector2(CaseD,CaseH),Quaternion.Euler(0,side*90f,0));
        CrystalGlassPanel(display,"A1 VITRINE | TOP glass",glass,new Vector3(0,top,front*.5f),new Vector2(CaseW,CaseD),Quaternion.Euler(90f,0,0));
        CrystalGlassPanel(display,"A1 VITRINE | BOTTOM glass",glass,new Vector3(0,CaseBottom,front*.5f),new Vector2(CaseW,CaseD),Quaternion.Euler(-90f,0,0));
        // Glass shelf separating the belt from the plaque compartment.
        CrystalGlassPanel(display,"A1 VITRINE | plaque shelf glass",glass,new Vector3(0,CaseBottom+ShelfAbove,front*.5f),new Vector2(CaseW-.02f,CaseD-.01f),Quaternion.Euler(-90f,0,0));

        // 3) Four brass side brackets holding the side glass to the marble:
        //    .20 m under the top and .14 m over the bottom (photo).
        foreach(float side in new[]{-1f,1f})
        foreach(float y in new[]{top-.20f,CaseBottom+.14f})
            // b33 phone proof: full-depth bars read as rods sticking out. The
            // photo shows short clamps at the marble: 25 x 46 x 50 mm.
            Cube(display,"A1 VITRINE | brass side bracket",gold,new Vector3(side*(CaseW*.5f+.0125f),y,-.025f),new Vector3(.025f,.046f,.05f));

        // 4) Warm LED (emissive, no shadows): recessed tray with a glowing
        //    frame under the top, two vertical lines at the back corners and
        //    one line along the bottom front edge.
        float trayW=CaseW-.14f,trayD=CaseD-.08f,trayZ=front*.5f;
        Cube(display,"A1 VITRINE | LED tray",dark,new Vector3(0,top-.02f,trayZ),new Vector3(trayW,.02f,trayD));
        foreach(float s in new[]{-1f,1f})
        {
            Cube(display,"A1 VITRINE | LED tray frame long",warm,new Vector3(0,top-.035f,trayZ+s*trayD*.5f),new Vector3(trayW,.012f,.012f));
            Cube(display,"A1 VITRINE | LED tray frame short",warm,new Vector3(s*trayW*.5f,top-.035f,trayZ),new Vector3(.012f,.012f,trayD));
            Cube(display,"A1 VITRINE | LED vertical back corner",warm,new Vector3(s*(CaseW*.5f-.02f),yc,-.02f),new Vector3(.012f,CaseH-.10f,.012f));
        }
        Cube(display,"A1 VITRINE | LED bottom front line",warm,new Vector3(0,CaseBottom+.012f,front+.03f),new Vector3(CaseW-.06f,.012f,.012f));

        // 5) The owner's ONE 3D belt, hung vertically: 1.27 m (real length),
        //    bottom .32 m over the case bottom (photo), 3 cm off the marble.
        float beltCentreY=CaseBottom+.32f+BeltLength*.5f;
        // Fit by the REAL LENGTH only: width/depth limits are the case interior,
        // so they bind (and warn) only if the model physically cannot fit.
        var belt=OriginalBelt(display,BeltLength,CaseW-.06f,CaseD-.05f,new Vector3(0,beltCentreY,-(CaseD-.05f)*.5f-.01f),
            CaseBottom+ShelfAbove,top-.08f,front+.02f,-.005f);
        if(belt.size.y>.01f&&Mathf.Abs(belt.size.y-BeltLength)>.02f)
            BraxBuildQuality.Warn("A1_VITRINE_BELT_NOT_REAL_LENGTH: "+belt.size.y.ToString("F2")+" m instead of "+BeltLength+
                " m; the FBX is wider/deeper than the case interior");
        int pins=0;
        if(belt.size.y>.01f)
        {
            // Gold pins either side of the strap, top and bottom (photo ratios).
            float L=belt.size.y,px=PinOffsetX*L;
            foreach(float y in new[]{belt.max.y-TopPinBelowTop*L,belt.min.y+BottomPinAboveBottom*L})
            foreach(float x in new[]{-px,px})
            {
                Cube(display,"A1 VITRINE | gold belt pin",gold,new Vector3(x,y,belt.min.z*.5f),new Vector3(.05f,.035f,Mathf.Max(.04f,-belt.min.z)));
                pins++;
            }
        }

        // 6) Brushed brass plaque on the marble in the lower compartment:
        //    owner's B crest (original atlas region, pixels unedited) +
        //    engraved BRAXHORN ARENA / CHAMPION'S BELT as on the photo.
        float plaqueY=CaseBottom+.17f;
        Cube(display,"A1 VITRINE | brushed brass plaque",brass,new Vector3(0,plaqueY,-.006f),new Vector3(PlaqueW,PlaqueH,.008f));
        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalPlaque);
        if(!image)BraxBuildQuality.Warn("A1_VITRINE_PLAQUE_PENDING: "+OriginalPlaque);
        else
        {
            var shader=Shader.Find(BraxCityMaterials.ShaderName);
            var label=new Material(shader){name="A1 vitrine | original B crest, unedited",enableInstancing=true};
            label.SetTexture("_MainTex",image);label.SetColor("_Color",Color.white);
            label.SetFloat("_VertexTint",0f);label.SetFloat("_Roughness",.35f);label.SetFloat("_Metallic",0f);
            label.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
            AssetDatabase.CreateAsset(label,Materials+"A1_vitrine_plaque.mat");
            // Atlas 1536x512: B crest = x 1298..1519, y 85..477 (measured).
            // Yaw 180 => swap U endpoints so the crest is not mirrored.
            AtlasLabel(display,label,"VITRINE_B_CREST",new Vector3(0,plaqueY+.052f,-.012f),
                Quaternion.Euler(0,180f,0),new Vector2(.031f,.055f),.9889f,.8451f,.0684f,.8340f);
        }
        var engraving=new Color(.16f,.11f,.06f);
        EngravedText(display,"BRAXHORN ARENA",new Vector3(0,plaqueY-.005f,-.012f),.034f,engraving);
        EngravedText(display,"CHAMPION'S BELT",new Vector3(0,plaqueY-.047f,-.012f),.018f,engraving);
        foreach(float s in new[]{-1f,1f})
            Cube(display,"A1 VITRINE | engraved rule",dark,new Vector3(s*.135f,plaqueY-.047f,-.0115f),new Vector3(.07f,.002f,.001f));

        // One warm vertex light in front of the case (no realtime shadows).
        var lamp=new GameObject("A1 VITRINE | warm vertex light").AddComponent<Light>();
        lamp.transform.SetParent(display,false);
        lamp.transform.localPosition=new Vector3(0,top-.25f,-.6f);
        lamp.type=LightType.Point;lamp.color=new Color(1f,.86f,.62f);lamp.intensity=.85f;
        lamp.range=4.2f*k;lamp.shadows=LightShadows.None;lamp.renderMode=LightRenderMode.ForceVertex;

        Debug.Log("A1_VITRINE_OWNER_PHOTO location=hall_front_wall x="+LobbyX+" z="+LobbyZ+" facing=entrance"+
            " ruler=UFC_belt_1.27m=962px case="+CaseW+"x"+CaseH+"x"+CaseD+" bottom="+CaseBottom+
            " pillar="+PylonW+"x"+PylonH+"x"+PylonD+" marble="+(veinedMarble?"ambientCG_Marble016":"granite_tile_FALLBACK")+
            " belt="+belt.size.ToString("F2")+" pins="+pins+" brackets=4 LED=tray+2vertical+bottom"+
            " plaque="+PlaqueW+"x"+PlaqueH+" mount=hall_wall_right_of_door slats=REMOVED plant=REMOVED onFloor="+onFloor+" scale=K_once");
    }

}
