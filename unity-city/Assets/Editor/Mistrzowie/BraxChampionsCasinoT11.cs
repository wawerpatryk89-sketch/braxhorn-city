using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// T11 original plan: centre (495,465), property 120 x 100 nominal metres.
// All geometry uses BraxClaudeOsiedle.K exactly once. Exterior per the owner
// board 9.11 + the rotunda lobby seen through the glass (chandelier, gallery,
// violet glow). No casino games, tables or office interiors yet.
public static partial class BraxChampionsCasinoT11
{
    public const string SignPath="Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png";
    const string TaxiSignPath="Assets/Source/Mistrzowie/CitySigns/03_TRANSPORT_KURORT/TAXI_TAXI.png";
    const string ParkingSignPath="Assets/Source/Mistrzowie/CitySigns/03_TRANSPORT_KURORT/PARKING_PARKING.png";
    const string SignSha256="78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b";
    const string TaxiSignSha256="ebf5e0ca3fa789cd709e0e621af399c0c10f31e96bb29cb44e4e5e66aa0adada";
    const string ParkingSignSha256="6e756f193e3c764f63960f049524ea265017f95b58d95991885951ed1fbe9247";
    const string OwnerOriginalsReleaseUrl=
        "https://github.com/wawerpatryk89-sketch/braxhorn-city/releases/download/champions-a1-20261105/BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip";
    const string OwnerOriginalsZipSha256="43ea47f02a1f2e9e4f707bb75d16ec0a3fe79ec02762929ca591e317defa1088";
    // EXACT owner-supplied Pixabay / DEZYNE_3D ID 140 GLB, no fabricated stand-in.
    public const string DollarPath=
        "Assets/Source/Mistrzowie/CasinoT11/OwnerDollar/dezyne_3d-dollar-sign-140.glb";
    const float LotX=495f, LotZ=465f;
    static float K=>BraxClaudeOsiedle.K;
    static string output;
    static int serial;
    static Transform root,shell,trim,signRoot;
    static Material cream,graphite,windowFrame,interiorBack,violet,granite,asphalt,paving,gold,pillarGold,steel,glass,frontGlass,blackGlass,skylightGlass,led,roof,redCarpet,parkingPaint,signBacking,curtain,palmBed;
    static readonly System.Collections.Generic.Dictionary<string,Mesh> bevelMeshes=
        new System.Collections.Generic.Dictionary<string,Mesh>();
    // White finishes are GARAGE-ONLY. Do not repaint the owner-approved casino exterior.
    static Material garageWhite,garageWhiteBeam,garageBlackAsphalt,garageParkingPaint;

    static void Setup(string directory)
    {
        output=directory;serial=0;bevelMeshes.Clear();
        // Plansza właściciela „KASYNO (T11)”, 6.11: jasna elewacja = Rectangular Facade
        // Tiles (jasny krem), duże płyty — NIE cegła. Te same pobrane mapy co grafit,
        // kremowy odcień jak pas kamienny areny.
        // Plansza 9.11 (jasny krem, duże bloki na mijankę): Poly Haven floor_tiles_04
        // (kremowe bloki kamienne, gładkie) — najbliższy oryginał do próbki z planszy.
        // rectangular_facade_tiles_02 z Poly Haven jest brązowy w pasy, nie kremowy.
        cream=BraxChampionsCasinoPbr2K.Create(output,"t11_cream_limestone_blocks","floor_tiles_04",4.0f,new Color(1f,.97f,.90f));
        windowFrame=Surface("black_window_frames",new Color(.045f,.05f,.055f),false,false);
        windowFrame.SetFloat("_Metallic",.70f);windowFrame.SetFloat("_Roughness",.30f);
        // Owner 10.11 00:23 on the render: "W takim kasynie mogą być szyby jasne, jakieś zajebiste, bo jak będą
        // wyglądać tak jak tutaj, to chujowe są." The windows were a dark brown board behind smoked glass.
        // The glass is clear and bright, and (10.11 00:30) there is no board behind it: the windows show the real
        // casino hall with its columns, chandeliers and carpet; ivory curtains hang at both sides of every window.
        interiorBack=Surface("interior_warm_backdrop",new Color(.20f,.15f,.11f),false,false);
        interiorBack.SetFloat("_Roughness",.80f);
        curtain=Surface("window_ivory_curtain",new Color(.97f,.94f,.87f),false,true);
        curtain.SetColor("_EmissionColor",new Color(.16f,.14f,.11f));
        curtain.SetFloat("_Roughness",.85f);
        violet=Surface("casino_violet_glow",new Color(.62f,.30f,1f),false,true);
        violet.SetColor("_EmissionColor",new Color(.50f,.16f,.92f));
        graphite=BraxChampionsCasinoPbr2K.Create(output,"t11_graphite_facade","rectangular_facade_tiles",2.0f,new Color(.22f,.23f,.25f));
        granite=BraxChampionsCasinoPbr2K.Create(output,"t11_dark_granite","granite_tile",1.2f,new Color(.26f,.28f,.30f));
        paving=BraxChampionsCasinoPbr2K.Create(output,"t11_walkways","concrete_pavement_02",1.8f,new Color(.94f,.93f,.89f));
        asphalt=BraxChampionsCasinoPbr2K.Create(output,"t11_asphalt","clean_asphalt",2.1f,new Color(.9f,.9f,.9f));
        // Issue #29: all visible champagne-gold trim uses the existing Metal048A PBR maps.
        gold=BraxChampionsFoundation.LibraryMetal(
            "T11_champagne_gold_trim",new Color(.93f,.73f,.39f));
        gold.SetFloat("_Metallic",.97f);gold.SetFloat("_Roughness",.17f);
        EditorUtility.SetDirty(gold);
        // Owner 10.11 00:37 (photo of fine-gold bars): "Wszystko, co złote, ma być złote." Real yellow gold.
        BraxChampionsGold.Apply(gold);
        // Właściciel: wszystkie widoczne filary ZŁOTE i połyskujące.
        // Prawdziwe oryginalne 4K PBR Metal048A z przygotowanej biblioteki,
        // nie jednolity żółty plastik i nie nowy zamiennik tekstur.
        pillarGold=BraxChampionsFoundation.LibraryMetal(
            "T11_all_gold_polished_columns",new Color(1f,.76f,.34f));
        pillarGold.SetFloat("_Metallic",.98f);
        pillarGold.SetFloat("_Roughness",.16f);
        EditorUtility.SetDirty(pillarGold);
        BraxChampionsGold.Apply(pillarGold);
        steel=Surface("satin_steel",new Color(.32f,.34f,.37f),false,false);
        steel.SetFloat("_Metallic",.83f);steel.SetFloat("_Roughness",.38f);
        roof=BraxChampionsCasinoPbr2K.Create(output,"t11_roof","concrete_floor_02",2.1f,new Color(.27f,.29f,.32f));
        redCarpet=BraxChampionsCasinoPbr2K.Create(output,"t11_casino_red_carpet","fabric_pattern_05",.55f,new Color(.64f,.028f,.038f));
        parkingPaint=Surface("painted_parking_stripes",new Color(.97f,.97f,.93f),false,false);
        garageParkingPaint=Surface("B1_brilliant_white_floor_stripes",new Color(.99f,.99f,.99f),false,false);
        garageParkingPaint.SetFloat("_Roughness",.70f);
        // Washable matte white walls, white structural columns and a white
        // reinforced ceiling ONLY below grade. The casino facade remains cream,
        // dark graphite and champagne gold as in the owner's reference photo.
        garageWhite=Surface("B1_clean_white_wall_and_columns",new Color(.975f,.978f,.965f),false,false);
        garageWhite.SetFloat("_Roughness",.82f);
        garageWhiteBeam=Surface("B1_white_structural_roof_beams",new Color(.92f,.93f,.92f),false,false);
        garageWhiteBeam.SetFloat("_Roughness",.76f);
        // Black, real original Poly Haven 2K asphalt. Separate material prevents
        // the underground surface tint from darkening VIP/TAXI roads outside.
        garageBlackAsphalt=BraxChampionsCasinoPbr2K.Create(output,"t11_B1_black_asphalt",
            "clean_asphalt",2.1f,new Color(.40f,.40f,.41f));
        glass=Surface("clear_glass",new Color(.68f,.80f,.88f,.31f),true,false);
        // Front: SLIGHTLY darkened but legible transparent glass, warm gold
        // frame divisions and visible interior. Does not tint side masonry.
        frontGlass=Surface("front_smoke_glass",new Color(.82f,.91f,.97f,.15f),true,false);
        frontGlass.SetFloat("_ReflectionStrength",.85f);
        // Owner 10.11 12:28-12:50 (all buildings): only the door leaves are light glass, the glazing round them is black,
        // "zakaz szarego ... albo czarne". Rotunda, tall windows and the door transom: black reflective glass.
        blackGlass=Surface("black_architectural_glass",new Color(.025f,.03f,.035f,.92f),true,false);
        blackGlass.SetFloat("_ReflectionStrength",1f);
        // palms stand in soil beds, never in the stone apron (owner 12:39 "palma w kostce rośnie")
        palmBed=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/grass.mat");
         // The reference drawing has a blue-violet translucent glazed roof lantern.
        skylightGlass=Surface("roof_skylight_glass",new Color(.48f,.56f,.76f,.38f),true,false);
        led=Surface("warm_led",new Color(.92f,.78f,.57f),false,true);
        led.SetColor("_EmissionColor",new Color(.34f,.22f,.095f));
        EditorUtility.SetDirty(led);
        signBacking=Surface("sign_backplate_graphite_metal",new Color(.055f,.065f,.080f),false,false);
        signBacking.SetFloat("_Metallic",.68f);
        signBacking.SetFloat("_Roughness",.24f);
    }

    static Material Surface(string name,Color tint,bool transparent,bool emissive)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("T11 needs original CityStaticPBR shader");
        var m=new Material(shader){name="T11 "+name,enableInstancing=true};
        m.SetColor("_Color",tint);m.SetFloat("_VertexTint",0);
        m.SetFloat("_Metallic",0);m.SetFloat("_Roughness",transparent?.075f:.32f);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        if(transparent)
        {
            m.SetFloat("_Transparent",1);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0);
            m.SetFloat("_Cull",2);
            m.SetFloat("_AlphaClip",0);
            m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
        }
        if(emissive)m.SetColor("_EmissionColor",new Color(1.1f,.70f,.34f));
        AssetDatabase.CreateAsset(m,output+"/Materials/T11_"+name+".mat");
        return m;
    }

    static void Tile(GameObject go,Vector3 metres)
    {
        var f=go.GetComponent<MeshFilter>();
        var m=UnityEngine.Object.Instantiate(f.sharedMesh);
        var v=m.vertices;var normals=m.normals;var uv=m.uv;
        for(int i=0;i<uv.Length;i++)
        {
            Vector3 p=v[i],n=normals[i];
            uv[i]=Mathf.Abs(n.y)>.7f?new Vector2((p.x+.5f)*metres.x,(p.z+.5f)*metres.z)
                :Mathf.Abs(n.x)>.7f?new Vector2((p.z+.5f)*metres.z,(p.y+.5f)*metres.y)
                :new Vector2((p.x+.5f)*metres.x,(p.y+.5f)*metres.y);
        }
        m.uv=uv;m.name="T11 real metre scaled UV "+serial;
        AssetDatabase.CreateAsset(m,output+"/Meshes/T11_tiled_"+(serial++)+".asset");
        f.sharedMesh=m;
    }

    static GameObject B(Transform parent,string name,Material mat,float x,float y,float z,float w,float h,float d,bool solid=true,bool tiled=false)
    {
        if(w<=0||h<=0||d<=0)throw new InvalidOperationException("T11 positive geometry dimensions required");
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;
        o.transform.localScale=new Vector3(w,h,d)*K;
        var renderer=o.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=mat;
        bool transparent=mat==glass||mat==frontGlass||mat==blackGlass||mat==skylightGlass;
        if(transparent){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        if(tiled || ((mat==cream || mat==graphite || mat==granite || mat==roof || mat==paving || mat==asphalt || mat==gold || mat==pillarGold) && Mathf.Max(w,Mathf.Max(h,d))>3f))
            Tile(o,new Vector3(w,h,d));
        if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        if(!transparent && mat!=led)
            GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return o;
    }
    static Transform Group(Transform parent,string name)
    {
        var o=new GameObject(name).transform;o.SetParent(parent,false);return o;
    }
    static GameObject BeveledBox(Transform parent,string name,Material mat,
        float x,float y,float z,float w,float h,float d,float radius,bool solid=true)
    {
        if(w<=0||h<=0||d<=0)throw new InvalidOperationException("T11 bevel dimensions must be positive");
        float hx=w*.5f,hy=h*.5f,hz=d*.5f;
        float r=Mathf.Clamp(radius,.001f,Mathf.Min(hx,Mathf.Min(hy,hz))*.92f);
        string key=w.ToString("F3")+"_"+h.ToString("F3")+"_"+d.ToString("F3")+"_"+r.ToString("F3");
        if(!bevelMeshes.TryGetValue(key,out var mesh) || !mesh)
        {
            var verts=new System.Collections.Generic.List<Vector3>(24);
            var tris=new System.Collections.Generic.List<int>(132);
            var index=new int[2,2,2,3];
            for(int ix=0;ix<2;ix++)
            for(int iy=0;iy<2;iy++)
            for(int iz=0;iz<2;iz++)
            {
                float sx=ix==0?-1f:1f,sy=iy==0?-1f:1f,sz=iz==0?-1f:1f;
                var p=new[]{
                    new Vector3(sx*hx,sy*(hy-r),sz*(hz-r)),
                    new Vector3(sx*(hx-r),sy*hy,sz*(hz-r)),
                    new Vector3(sx*(hx-r),sy*(hy-r),sz*hz)
                };
                for(int axis=0;axis<3;axis++)
                {
                    index[ix,iy,iz,axis]=verts.Count;
                    verts.Add(p[axis]*K);
                }
            }
            void Face(params int[] face)
            {
                var normal=Vector3.Cross(verts[face[1]]-verts[face[0]],
                    verts[face[2]]-verts[face[0]]);
                Vector3 centre=Vector3.zero;
                foreach(int i in face)centre+=verts[i];
                centre/=face.Length;
                if(Vector3.Dot(normal,centre)<0)System.Array.Reverse(face);
                for(int i=1;i<face.Length-1;i++)
                {
                    tris.Add(face[0]);tris.Add(face[i]);tris.Add(face[i+1]);
                }
            }
            for(int ix=0;ix<2;ix++)
                Face(index[ix,0,0,0],index[ix,0,1,0],index[ix,1,1,0],index[ix,1,0,0]);
            for(int iy=0;iy<2;iy++)
                Face(index[0,iy,0,1],index[1,iy,0,1],index[1,iy,1,1],index[0,iy,1,1]);
            for(int iz=0;iz<2;iz++)
                Face(index[0,0,iz,2],index[0,1,iz,2],index[1,1,iz,2],index[1,0,iz,2]);
            for(int ix=0;ix<2;ix++)
            for(int iy=0;iy<2;iy++)
                Face(index[ix,iy,0,0],index[ix,iy,1,0],index[ix,iy,1,1],index[ix,iy,0,1]);
            for(int ix=0;ix<2;ix++)
            for(int iz=0;iz<2;iz++)
                Face(index[ix,0,iz,0],index[ix,1,iz,0],index[ix,1,iz,2],index[ix,0,iz,2]);
            for(int iy=0;iy<2;iy++)
            for(int iz=0;iz<2;iz++)
                Face(index[0,iy,iz,1],index[1,iy,iz,1],index[1,iy,iz,2],index[0,iy,iz,2]);
            for(int ix=0;ix<2;ix++)
            for(int iy=0;iy<2;iy++)
            for(int iz=0;iz<2;iz++)
                Face(index[ix,iy,iz,0],index[ix,iy,iz,1],index[ix,iy,iz,2]);
            mesh=new Mesh{name="T11 shared chamfered premium box "+key};
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);
            var uv=new Vector2[verts.Count];
            for(int i=0;i<verts.Count;i++)
            {
                Vector3 p=verts[i]/K;
                uv[i]=new Vector2(p.x+p.z,p.y);
            }
            mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_bevel_"+(serial++)+".asset");
            bevelMeshes[key]=mesh;
        }
        var o=new GameObject(name);
        o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(solid)o.AddComponent<MeshCollider>().sharedMesh=mesh;
        if(mat!=led)GameObjectUtility.SetStaticEditorFlags(o,
            StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return o;
    }

    static void RoofAndSkylight()
    {
        // Technical flat roof with genuine transparent top skylight.
        B(shell,"Podium ground slab",granite,0,.08f,-.01f,76f,.18f,55.98f,true,true);   // rear end 2 cm inside the wall face (no flicker)
        B(shell,"Podium roof west",roof,-25f,14.05f,-.01f,26f,.22f,55.98f,true,true);
        B(shell,"Podium roof east",roof,25f,14.05f,-.01f,26f,.22f,55.98f,true,true);
        B(shell,"Skylight roof south",roof,0,14.05f,-19f,24f,.22f,18f,true,true);
        B(shell,"Skylight roof north",roof,0,14.05f,18.99f,24f,.22f,17.98f,true,true);
        B(shell,"Actual transparent blue-violet rooftop skylight",skylightGlass,0,14.14f,0,24f,.11f,20f,false);
        for(float x=-12f;x<=12f;x+=4f)
            BeveledBox(trim,"Skylight rounded champagne longitudinal rib",gold,
                x,14.24f,0,.12f,.13f,20f,.040f,false);
        for(float z=-10f;z<=10f;z+=5f)
            BeveledBox(trim,"Skylight rounded champagne crossbar",gold,
                0,14.24f,z,24f,.13f,.12f,.040f,false);
        // one front parapet stone run (the two halves left a 1 m notch right under the KASYNO WAWER crown)
        B(trim,"Parapet front cream limestone",cream,0,14.9f,-27.8f,75f,1.4f,.65f);
        B(trim,"Parapet rear cream limestone",cream,0,14.9f,27.8f,75f,1.4f,.65f);
        foreach(float side in new[]{-1f,1f})
        {
            B(trim,"Parapet side cream limestone",cream,side*37.7f,14.9f,0,.60f,1.4f,55.4f);
            BeveledBox(trim,"Roof rounded perimeter gold",gold,
                side*37.86f,15.58f,0,.14f,.16f,55.2f,.045f,false);
        }
        B(shell,"Enclosed roof technology enclosure",graphite,-29f,14.78f,16f,8f,1.25f,5f);
        for(float x=-32f;x<-25f;x+=.6f)
            BeveledBox(trim,"Ventilation rounded gold louvre",gold,
                x,15.5f,13.42f,.11f,1.35f,.13f,.035f,false);
        foreach(float z in new[]{16f,-15f})
            B(shell,"Exterior roof ventilation case",steel,21f,14.72f,z,3.5f,1.05f,3f);
    }

    static void RotundaRoofCap()
    {
        // Owner 10.11 16:36-16:39: "to czarne coś ... te jajo, na czym jest szyld, to wszystko psuje" / "to coś
        // zaokrąglone znika". The black 30 cm roof slab with the name standing on it is gone: the glass rotunda now ends
        // in a thin cream limestone coping (15 cm, 15 cm proud of the glass) like every parapet of the building, and
        // the name stands on the front parapet against the sky (CasinoName).
        const int sides=48;
        const float yTop=10.95f,yBottom=10.80f,over=.15f;
        var verts=new System.Collections.Generic.List<Vector3>();
        var uv=new System.Collections.Generic.List<Vector2>();
        // Centre is on the chord where the rotunda meets the main podium.
        for(int level=0;level<2;level++)
        {
            float y=level==0?yTop:yBottom;
            verts.Add(new Vector3(0,y,-28)*K);
            uv.Add(new Vector2(0,-28));
            for(int i=0;i<=sides;i++)
            {
                float angle=-Mathf.PI*.5f + Mathf.PI*i/sides;
                float x=(17f+over)*Mathf.Sin(angle),z=-28f-(8f+over)*Mathf.Cos(angle);
                verts.Add(new Vector3(x,y,z)*K);
                uv.Add(new Vector2(x,z));
            }
        }
        var tris=new System.Collections.Generic.List<int>();
        int bottomOffset=sides+2;
        for(int i=0;i<sides;i++)
        {
            int a=i+1,next=a+1;
            // Top faces upward; underside faces downward.
            tris.Add(0);tris.Add(next);tris.Add(a);
            tris.Add(bottomOffset);tris.Add(bottomOffset+a);tris.Add(bottomOffset+next);
            tris.Add(a);tris.Add(next);tris.Add(bottomOffset+next);
            tris.Add(a);tris.Add(bottomOffset+next);tris.Add(bottomOffset+a);
        }
        var mesh=new Mesh{name="T11 rotunda cream limestone coping roof"};
        mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_Curved_Rotunda_Roof.asset");
        var o=new GameObject("Rotunda thin cream limestone roof coping");
        o.transform.SetParent(shell,false);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=cream;
        o.AddComponent<MeshCollider>().sharedMesh=mesh;
    }

    static void CurvedFoyer()
    {
        const int segments=48;
        Vector3 P(float t)=>new Vector3(17f*Mathf.Sin(t),0,-28f-8f*Mathf.Cos(t));
        for(int i=0;i<segments;i++)
        {
            float a=-Mathf.PI*.5f+i*Mathf.PI/segments;
            float b=-Mathf.PI*.5f+(i+1)*Mathf.PI/segments;
            var pa=P(a);var pb=P(b);var centre=(pa+pb)*.5f;
            Vector3 delta=pb-pa;
            float length=delta.magnitude;
            Quaternion yaw=Quaternion.Euler(0,Mathf.Atan2(-delta.z,delta.x)*Mathf.Rad2Deg,0);
            float bottom=Mathf.Abs(centre.x)<3.1f?3.9f:.26f;
            var pane=B(shell,"T11 curved black glass | rotunda only",blackGlass,
                centre.x,(10.8f+bottom)*.5f,centre.z,length+.045f,10.8f-bottom,.115f,false);
            pane.transform.localRotation=yaw;

            if(i%3==0)
            {
                // Owner 10.11 00:45: "nie masz wszystkiego robić na złoto, ta gra nie jest ze złota". Gold stays where it
                // means something (sign, dollar, entrance portal, doors, columns, chandeliers, thin lines); the glass
                // wall's own frames are black metal like every other window of the building.
                var mullion=BeveledBox(trim,"T11 champagne rounded curved rotunda frame",windowFrame,
                    pa.x,5.55f,pa.z,.19f,10.6f,.19f,.045f,false);
                mullion.transform.localRotation=yaw;
            }

            // Owner 10.11 13:03: the 2.5 m black fascia band and the black dome on top ("ta wielka kopuła czarna") are
            // gone; 16:36 the slim black roof and its gold edge line went too (RotundaRoofCap: thin cream coping).
        }
        RotundaRoofCap();
        // Above the rotunda, between the pylons, the facade was open up to the parapet: from the street one looked into
        // the hall's ceiling and panelling (the white and brown patches beside the pylons). It is closed with the same
        // black glass as the rotunda, in black frames that run into the coping below and the parapet above.
        B(shell,"T11 black glass wall above the rotunda",blackGlass,0,(10.90f+14.25f)*.5f,-28.0f,33.9f,3.35f,.10f,false);
        for(int k=-5;k<=5;k++)
            B(trim,"T11 black frame above the rotunda",windowFrame,k*2.8f,(10.90f+14.25f)*.5f,-28.08f,.12f,3.30f,.16f,false);
        foreach(float side in new[]{-1f,1f})
            for(int j=0;j<3;j++)
                B(shell,"Entrance side granite stair",paving,side*(7.1f+j*.6f),.22f+j*.03f,-36.3f,5.3f,.14f+j*.07f,1.55f,true,true);
        B(shell,"Unobstructed accessible middle ramp",paving,0,.22f,-40f,7.2f,.08f,7.7f,true,true);
        B(shell,"Black fixed door transom glass",blackGlass,0,3.75f,-36.13f,5.7f,.32f,.10f,false);
        BeveledBox(trim,"Gold entry transom horizontal frame",gold,
            0,3.59f,-36.19f,5.95f,.07f,.12f,.025f,false);
        Door("T11_CASINO_WAWER_PUBLIC","Public glass main entry",0,-36.28f,180f,5.6f);
        EntrancePortal();
    }

    // Owner 10.11 00:37 on the render: "Te wejście to zlane z tą szybą, w ogóle tam nie wiadomo o co chodzi."
    // The doors were two bare panes in a wall of glass. Now a portal marks them from the street: two solid gold
    // jambs and a gold lintel around the doors, a black canopy with a gold edge and light lines over the red
    // carpet, two gold lanterns either side.
    static void EntrancePortal()
    {
        const float half=2.95f,zFace=-36.62f,top=4.02f;
        foreach(float s in new[]{-1f,1f})
        {
            BeveledBox(shell,"T11 entrance portal black jamb",windowFrame,s*(half+.24f),(.21f+top)*.5f,zFace,.48f,top-.21f,.62f,.05f,true);
            B(trim,"T11 entrance portal jamb gold inner edge",gold,s*(half-.0065f),(.21f+top)*.5f,zFace,.017f,top-.25f,.58f,false);
            B(trim,"T11 entrance portal jamb light reveal",led,s*(half+.24f),2.1f,zFace-.315f,.07f,3.4f,.02f,false);
            // lantern on a gold post beside the carpet
            BeveledBox(trim,"T11 entrance lantern gold post",gold,s*4.35f,1.21f,-38.9f,.14f,2.0f,.14f,.03f,false);
            BeveledBox(trim,"T11 entrance lantern warm glass",led,s*4.35f,2.42f,-38.9f,.34f,.46f,.34f,.05f,false);
            BeveledBox(trim,"T11 entrance lantern gold cap",gold,s*4.35f,2.70f,-38.9f,.42f,.08f,.42f,.03f,false);
        }
        BeveledBox(shell,"T11 entrance portal black lintel",windowFrame,0,top+.27f,zFace,2f*half+1.44f,.54f,.62f,.05f,false);
        B(trim,"T11 entrance portal lintel gold inner edge",gold,0,top-.0065f,zFace,2f*half-.04f,.017f,.58f,false);
        // canopy: black body, gold edge all round, three light lines underneath
        const float cy=4.74f,cz=-38.05f,cw=8.6f,cd=3.3f;
        B(shell,"T11 entrance canopy black body",signBacking,0,cy,cz,cw,.26f,cd,false);
        B(trim,"T11 entrance canopy gold front edge",gold,0,cy,cz-cd*.5f-.03f,cw+.12f,.30f,.06f,false);
        foreach(float s in new[]{-1f,1f})
            B(trim,"T11 entrance canopy gold side edge",gold,s*(cw*.5f+.03f),cy,cz,.06f,.30f,cd,false);
        foreach(float lz in new[]{-39.2f,-38.1f,-37.0f})
            B(trim,"T11 entrance canopy warm light line",led,0,cy-.138f,lz,cw-1.0f,.016f,.08f,false);
        Debug.Log("T11_ENTRANCE_PORTAL jambs=2_solid_gold lintel=gold canopy=8.6x3.3_black_gold_edge lightLines=3 lanterns=2 framedGoldLeaves=2 clearOpening=5.6x3.6");
    }

    static void Door(string id,string label,float x,float z,float yaw,float width)
    {
        var group=Group(shell,label+" | operable double door");
        group.localPosition=new Vector3(x,.21f,z)*K;
        group.localRotation=Quaternion.Euler(0,yaw,0);
        var leaves=new Transform[2];
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1f:1f;
            var hinge=Group(group,"Glass door leaf "+(i+1));
            hinge.localPosition=new Vector3(side*width*.5f,0,0)*K;
            leaves[i]=hinge;
            B(hinge,"Toughened clear door glass",frontGlass,-side*width*.25f,1.8f,0,width*.5f-.20f,3.30f,.06f,true);   // edges inside the frame
            // 10.11 00:37 "wejście zlane z szybą": every leaf is a framed gold door, not a bare pane in a glass wall.
            float lw=width*.5f,lc=-side*width*.25f;
            // owner 10.11 13:04 (casino doors): one door standard everywhere - black framed leaves, clear glass, gold pulls
            B(hinge,"Black door leaf top rail",windowFrame,lc,3.51f,0,lw,.18f,.12f,false);
            B(hinge,"Black door leaf bottom rail",windowFrame,lc,.14f,0,lw,.28f,.12f,false);
            B(hinge,"Black door leaf hinge stile",windowFrame,-side*.07f,1.8f,0,.14f,3.6f,.12f,false);
            B(hinge,"Black door leaf meeting stile",windowFrame,-side*(lw-.07f),1.8f,0,.14f,3.6f,.12f,false);
            foreach(float face in new[]{-1f,1f})
                B(hinge,"Champagne door pull",gold,-side*(lw-.36f),1.55f,face*.11f,.07f,1.30f,.075f,false);
        }
        var working=group.gameObject.AddComponent<BraxCityDoor>();
        working.id=id;working.label=label;working.leaves=leaves;working.sides=new[]{1f,-1f};
    }

    // The owner's 7 Nov instruction removes the office skyscraper COMPLETELY.
    // No tower silhouette, glass cage, crown or tower prefabs are generated.
    // The remaining approved building is the low two-storey casino only.

    static Bounds DollarBounds(GameObject model)
    {
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)
            throw new InvalidOperationException("Owner DEZYNE_3D dollar GLB contains no geometry: "+DollarPath);
        Bounds result=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))result.Encapsulate(renderer.bounds);
        if(result.size.y<.00001f || result.size.x<.00001f)
            throw new InvalidOperationException("Owner dollar model dimensions are invalid: "+DollarPath);
        return result;
    }

    static void RoofDollar()
    {
        // Full GLB is imported as the ORIGINAL model (textures and mesh
        // unchanged). Pin the owner's uploaded bytes, not a merely
        // similar-looking model downloaded from the same stock website.
        const string ownerSha256=
            "49e4fa9149aa69637db6231d2e98e611b3a41806b13a185daf7ad18e1a078640";
        if(File.Exists(DollarPath))
        {
            using(var stream=File.OpenRead(DollarPath))
            using(var sha=SHA256.Create())
            {
                string actual=BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-","").ToLowerInvariant();
                if(actual!=ownerSha256)
                {
                    BraxBuildQuality.Warn("T11_ROOF_DOLLAR_SOURCE_CHANGED: "+DollarPath+
                        " does not match owner's submitted GLB; do not substitute");
                    return;
                }
            }
        }
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(DollarPath) ??
            AssetDatabase.LoadAllAssetsAtPath(DollarPath).OfType<GameObject>().FirstOrDefault();
        if(!source)
        {
            BraxBuildQuality.Warn("T11_ROOF_DOLLAR_OWNER_GLB_NOT_STAGED: "+
                DollarPath+"; keep original source, never make a substitute");
            return;
        }
        // The WEST roof wing is solid (x=-38..-12). Stay away from the
        // central atrium, front glass, main sign and roof skylight x=-12..12.
        // Owner 10.11 00:35: "tego dolara to bym dał na środku". Centre axis of the building, on the solid roof
        // between the rotunda and the roof lantern (lantern glass is z -10..10), straight above the sign and the door.
        // Owner 10.11 17:15: "ten dolar mógłby być na dachu i się kręcić" - on again, 9 m on the roof axis behind the
        // KASYNO WAWER crown, turning slowly about its own vertical axis (BraxChampionsObrot, never static-batched).
        const float x=0f,z=-21f,roofY=14.40f,nominalHeight=9.0f;
        B(shell,"T11 OWNER DOLLAR rooftop structural steel support",steel,
            x,14.25f,z,5.0f,.30f,2.5f,true);
        var holder=Group(root,"T11 | OWNER ORIGINAL DEZYNE_3D GOLD DOLLAR | roof");
        holder.localPosition=new Vector3(x,roofY,z)*K;
        var dollar=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!dollar)throw new InvalidOperationException("Could not instantiate original roof dollar GLB");
        dollar.name="ORIGINAL dezyne_3d-dollar-sign-140.glb | tall rooftop dollar";
        dollar.transform.SetParent(holder,false);
        dollar.transform.localPosition=Vector3.zero;
        dollar.transform.localRotation=Quaternion.Euler(0f,180f,0f);
        var original=DollarBounds(dollar);
        dollar.transform.localScale*=nominalHeight*K/original.size.y;
        var scaled=DollarBounds(dollar);
        // Anchor the actual mesh's bottom to the real structural roof mount:
        // imported GLBs often have off-centre pivots, so do not assume pivot=foot.
        dollar.transform.position+=new Vector3(
            holder.position.x-scaled.center.x,
            holder.position.y-scaled.min.y,
            holder.position.z-scaled.center.z);
        foreach(var collider in dollar.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        // Owner 10.11 00:36: "Ten dolar ma być złoty, świecący się. Jasne złoto, żółte takie." The model's own
        // material read dull olive. Shape untouched; every surface gets bright yellow gold that shines by itself.
        var dollarGold=BraxChampionsGold.Apply(Surface("roof_dollar_bright_yellow_gold",BraxChampionsGold.Colour,false,true));
        dollarGold.SetColor("_EmissionColor",new Color(.46f,.34f,.06f));   // the roof sign glows a little more than trim
        EditorUtility.SetDirty(dollarGold);
        foreach(var r in dollar.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length;i++)mats[i]=dollarGold;
            r.sharedMaterials=mats;
        }
        var spin=holder.gameObject.AddComponent<BraxChampionsObrot>();
        spin.os=Vector3.up;spin.stopniNaSekunde=30f;spin.zasieg=600f;          // seen from the streets round the casino
        var finalBounds=DollarBounds(dollar);
        Debug.Log("CHAMPIONS_T11_ROOF_DOLLAR_OWNER_ORIGINAL visible_height="+
            (finalBounds.size.y/K).ToString("F2")+
            "m roofY="+roofY+" x="+x+" z="+z+
            " model_source=dezyne_3d-dollar-sign-140.glb material=bright_yellow_gold_self_lit centred=true spinning=30deg/s"+
            " phoneVisualReviewRequired=true");
    }

    // Single, exact-height drivable plane. Unlike a tilted BoxCollider slab
    // it produces NO artificial lip at the garage turn or the street crossing.
    // For ramps, y changes with Z; the road approach slopes gently along X.
    static GameObject GradedDrive(Transform parent,string id,Material finish,
        float x0,float x1,float z0,float z1,float y0,float y1,bool slopeAlongX)
    {
        if(x1<=x0||z1<=z0 || Mathf.Abs(y0-y1)>5f)
            throw new InvalidOperationException("T11 invalid graded vehicle road: "+id);
        float y01=slopeAlongX?y0:y1;
        float y10=slopeAlongX?y1:y0;
        var mesh=new Mesh{name="T11 drivable no-step surface | "+id};
        mesh.vertices=new[]{
            new Vector3(x0,y0,z0)*K,
            new Vector3(x0,y01,z1)*K,
            new Vector3(x1,y1,z1)*K,
            new Vector3(x1,y10,z0)*K
        };
        // PBR tiling is in NOMINAL metres, exactly like the original
        // Poly Haven source materials; the game K appears only on vertices.
        mesh.uv=new[]{
            new Vector2(x0,z0),new Vector2(x0,z1),
            new Vector2(x1,z1),new Vector2(x1,z0)
        };
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_"+id+".asset");
        var obj=new GameObject("T11 driveable graded "+id);
        obj.transform.SetParent(parent,false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=finish;
        renderer.receiveShadows=true;
        var collision=obj.AddComponent<MeshCollider>();
        collision.sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(obj,
            StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return obj;
    }

    // Smooth, SINGLE surface for the down-ramp. Vehicle wheels receive
    // continuous height at BOTH junctions; 2m quadratic easing at the ends
    // removes the abrupt hinge at z=-22 and the LEFT TURN floor at z=17.
    // Physically shared mesh vertices + one collider; mobile-friendly (<200 verts).
    static GameObject SmoothGarageRamp(Transform parent,Material material,
        float x0,float x1,float z0,float z1,float y0,float y1)
    {
        if(x1<=x0||z1<=z0||y0<=y1)
            throw new InvalidOperationException("T11 bad ramp dimensions or descent");
        float length=z1-z0;
        const float ease=2f;
        if(length<=ease*2.5f)
            throw new InvalidOperationException("T11 ramp too short for eased joins");
        float grade=(y0-y1)/(length-ease);
        if(grade>.12f)
            throw new InvalidOperationException("T11 maximum ramp grade exceeds 12 percent");
        const int segments=78; // 0.5m over the approved 39m run
        var points=new System.Collections.Generic.List<Vector3>((segments+1)*2);
        var uv=new System.Collections.Generic.List<Vector2>((segments+1)*2);
        var tris=new System.Collections.Generic.List<int>(segments*6);
        for(int i=0;i<=segments;i++)
        {
            float distance=length*i/segments;
            float drop;
            if(distance<=ease)
                drop=grade*distance*distance/(2f*ease);
            else if(distance>=length-ease)
            {
                float t=distance-(length-ease);
                drop=grade*(length-1.5f*ease+t-t*t/(2f*ease));
            }
            else
                drop=grade*(distance-ease*.5f);
            float z=z0+distance;
            float y=y0-drop;
            points.Add(new Vector3(x0,y,z)*K);
            points.Add(new Vector3(x1,y,z)*K);
            uv.Add(new Vector2(x0,z));
            uv.Add(new Vector2(x1,z));
            if(i<segments)
            {
                int n=i*2;
                tris.Add(n);tris.Add(n+2);tris.Add(n+3);
                tris.Add(n);tris.Add(n+3);tris.Add(n+1);
            }
        }
        var mesh=new Mesh{name="T11 B1 continuous eased left-turn ramp"};
        mesh.SetVertices(points);mesh.SetUVs(0,uv);
        mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_B1_EasedRamp.asset");
        var obj=new GameObject("T11 B1 eased real COLLISION ramp | 39m descent");
        obj.transform.SetParent(parent,false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;renderer.receiveShadows=true;
        obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(obj,
            StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        Debug.Log("T11_B1_RAMP_SMOOTH mean_grade="+
            ((y0-y1)/length*100f).ToString("F2")+
            " max_grade="+(grade*100f).ToString("F2")+
            " percent up/down joins level and continuous; no step or duplicate paint");
        return obj;
    }

    static void UndergroundGarage()
    {
        // REAL accessible subterranean car park. No interior games, decoration,
        // casino floors or casino partitions. Route: east side of T11, from
        // z=-22 at road y=.2485 to flat B1 at z=17, y=-4.098.
        // Local turn pocket z=17..29 is a full 12m deep, level 90-degree
        // LEFT turn, not a narrow 5m gap behind the old 46m ramp.
        // World plot centre (495,465); Unity scale K is applied ONCE.
        var basement=Group(root,"T11 | UNDERGROUND CAR PARK - real ramp and bays");
        // The garage floor ends exactly at the western edge of the turn
        // landing (x=39.55). Both asphalt finishes meet EDGE-TO-EDGE at
        // y=-4.098: no coplanar duplicate surfaces, no car-wheel bump.
        var floor=B(basement,"Garage structural floor - real collider",granite,
            3.775f,-4.20f,2.5f,71.55f,.16f,53f,true,true);
        B(basement,"Garage floor BLACK Poly Haven asphalt",garageBlackAsphalt,
            3.775f,-4.109f,2.5f,71.55f,.022f,53f,true,true);
        B(basement,"Garage solid west retaining wall WHITE",garageWhite,
            -32.05f,-2.2f,2.5f,.32f,4.2f,53f,true,true);
        // Hall and ramp terminal walls meet on a SINGLE north line z=29:
        // no exposed 4m void directly in the driver's forward direction.
        B(basement,"Garage north wall WHITE",garageWhite,
            3.50f,-2.2f,29.0f,71.30f,4.2f,.32f,true,true);
        B(basement,"Garage south wall WHITE (no exit through front)",garageWhite,
            4.5f,-2.2f,-24.0f,73.3f,4.2f,.32f,true,true);
        // Full 12m-wide LEFT-TURN aperture z17..29. The separating wall
        // ends only at the real level-bottom point so the turning wheels never
        // drop 20-90cm onto the already-flat underground garage floor.
        B(basement,"Garage east solid wall WHITE before LEFT TURN opening",garageWhite,
            39.30f,-1.75f,-3.5f,.30f,5.10f,41f,true,true);
        // No old short return at z29..33: it pinched the turn and left
        // an uncovered cavity behind the garage north wall.
        // Real covered underground ceiling, below the existing plot grade.
        B(basement,"Garage WHITE reinforced ceiling",garageWhite,
            3.575f,-.58f,2.5f,71.15f,.20f,53f,true,true);
        // The white ceiling has visible STRUCTURAL transverse/longitudinal
        // beams, not a bare floating rectangle. Beam undersides are at -1.08 m;
        // clear height above the asphalt is over 3.0 m (nominal).
        foreach(float zz in new[]{-18f,-6f,6f,18f,27f})
            B(basement,"B1 WHITE transverse roof beam",garageWhiteBeam,
                3.3f,-.875f,zz,70.6f,.40f,.48f,true,true);
        foreach(float xx in new[]{-29.0f,-12.5f,4.0f,20.5f,37.0f})
            B(basement,"B1 WHITE longitudinal roof beam",garageWhiteBeam,
                xx,-.875f,2.5f,.48f,.40f,51f,true,true);
        // The 9.35m road is fully flat by z=17, matching the B1 asphalt
        // surface at y=-4.098 without a vertical seam. Parking-hall asphalt
        // ENDS at x39.55, and the separate 12m-long landing STARTS there:
        // no overlapping co-planar surfaces and no inverted road wedge.
        const float rampX0=39.55f,rampX1=48.90f;
        const float roadY=.2485f,garageY=-4.098f;
        B(basement,"Garage bottom structural landing",granite,
            44.225f,-4.20f,23f,9.35f,.16f,12f,true,true);
        B(basement,"Garage bottom BLACK asphalt landing",garageBlackAsphalt,
            44.225f,-4.109f,23f,9.35f,.022f,12f,true,true);
        SmoothGarageRamp(basement,garageBlackAsphalt,
            rampX0,rampX1,-22f,17f,roadY,garageY);
        // West side shares the main garage east wall above; no duplicate.
        B(basement,"Ramp east retaining wall WHITE",garageWhite,
            49.12f,-1.88f,3.5f,.20f,4.84f,51f,true,true);
        // This wall was genuinely MISSING straight ahead in the old build.
        // It closes the end of the ramp but preserves a wide turn to the LEFT.
        B(basement,"T11 SOLID END WALL straight ahead at ramp bottom",garageWhite,
            44.225f,-2.05f,29.0f,9.85f,4.10f,.30f,true,true);
        // Top approach joins the front forecourt at z=-37.5 exactly, with
        // identical surface elevation. It ends where the ramp begins (z=-22).
        B(basement,"Garage entrance from VIP/TAXI circulation",asphalt,
            44.225f,.228f,-29.75f,9.35f,.041f,15.5f,true,true);
        B(basement,"Garage gate LEFT graphite pier",graphite,
            39.20f,2.52f,-22f,.45f,4.65f,.76f,true,true);
        B(basement,"Garage gate RIGHT graphite pier",graphite,
            49.18f,2.52f,-22f,.45f,4.65f,.76f,true,true);
        B(basement,"Garage main entrance champagne beam",gold,
            44.19f,4.95f,-22f,10.65f,.40f,.80f,true);
        B(basement,"Garage upper portal warm LED",led,
            44.19f,4.71f,-22.44f,10.35f,.10f,.12f,false);
        // Owner 9.11 23:40 "wjazd do parkingu poprawić ... z zewnątrz petarda": the gate was two thin posts and a beam.
        // Now a real portal: gold light reveals on both piers, a graphite canopy with downlight lines over the ramp
        // head, dark stone copings and a champagne guard rail on both ramp walls (the drop is 4.3 m).
        foreach(float px in new[]{39.20f,49.18f})
        {
            B(trim,"Garage portal pier warm light reveal",led,px,2.45f,-22.395f,.07f,4.3f,.03f,false);
            B(trim,"Garage portal pier champagne base shoe",gold,px,.29f,-22f,.53f,.16f,.84f,false);
        }
        B(basement,"Garage portal graphite canopy over ramp head",graphite,44.19f,5.04f,-20.32f,10.59f,.20f,2.60f,false,true);
        B(trim,"Garage portal canopy champagne rear edge",gold,44.19f,5.05f,-18.995f,10.65f,.21f,.05f,false);
        foreach(float lz in new[]{-21.2f,-20.3f,-19.4f})
            B(trim,"Garage portal canopy warm downlight line",led,44.19f,4.942f,lz,9.2f,.016f,.07f,false);
        B(trim,"Ramp WEST wall dark stone coping",granite,39.30f,.835f,-3.5f,.42f,.07f,41.04f,false,true);
        B(trim,"Ramp EAST wall dark stone coping",granite,49.12f,.575f,3.70f,.32f,.07f,50.64f,false,true);
        B(trim,"Ramp WEST champagne guard rail",windowFrame,39.30f,1.30f,-3.5f,.06f,.05f,41.0f,false);
        B(trim,"Ramp EAST champagne guard rail",windowFrame,49.12f,1.30f,3.5f,.06f,.05f,51.0f,false);
        for(int i=0;i<=13;i++)
            B(trim,"Ramp WEST guard rail post",windowFrame,39.30f,1.0725f,-23.8f+i*3.123f,.045f,.405f,.045f,false);
        for(int i=0;i<=17;i++)
            B(trim,"Ramp EAST guard rail post",windowFrame,49.12f,.9425f,-21.8f+i*2.976f,.045f,.665f,.045f,false);
        Debug.Log("T11_B1_GARAGE_LEFT_TURN_GEOMETRY rampClear=9.35m"+
            " westExitZ=17..29 width=12m bottomLanding=9.35x12m"+
            " roadTop=.2485 garageTop=-4.098 smoothRampNoLip=true"+
            " solidEndWallAtZ29=true protectedRoof=true deviceDriveTestPending=true");
        // 32 actual marked bays: two rows x sixteen, 3.10m wide x 5.25m
        // long. Seventeen lines are necessary to enclose sixteen stalls.
        // The broad middle lane and northern turning area remain unmarked.
        const int baysPerRow=16;
        const float bayWidth=3.10f;
        for(int i=0;i<=baysPerRow;i++)
        {
            float x=-28f+i*bayWidth;
            foreach(float z in new[]{-17f,10f})
                B(basement,"B1 physical car-space border white #"+i,garageParkingPaint,
                    x,-4.094f,z,.095f,.008f,5.25f,false);
        }
        foreach(float z in new[]{-19.65f,12.65f})
            B(basement,"B1 continuous marked bay head line",garageParkingPaint,
                -3.2f,-4.094f,z,baysPerRow*bayWidth,.008f,.095f,false);
        // Legible two-way drive aisle. Short WHITE centre dashes never
        // obstruct parking slots; the full asphalt surface remains black.
        for(int n=0;n<8;n++)
            B(basement,"B1 WHITE dashed driving-aisle centre line",garageParkingPaint,
                -23f+n*6.25f,-4.090f,-3.5f,3.0f,.008f,.095f,false);

        // Genuine interior supports reach BOTH foundation and ceiling.
        // They are outside the bays, and no support enters the accessible
        // 8m-wide underground ramp or the north turning zone.
        foreach(float x in new[]{-30.25f,29.35f})
            foreach(float z in new[]{-18f,0f,13.5f})
            {
                // Bottom -4.12: meets the slab. Top -.68: meets the ceiling.
                B(basement,"B1 structural reinforced WHITE support column",garageWhite,
                    x,-2.4f,z,.78f,3.44f,.78f,true,true);
                // White column base is seated on the asphalt, not floating.
                B(basement,"B1 WHITE column protective plinth",garageWhite,
                    x,-4.015f,z,1.0f,.18f,1.0f,false);
            }

        // Eight real economical vertex lights, not emissive-only decorations:
        // CityStaticPBR uses up to four local lights per object without
        // additional shadow-casting passes on Android.
        int lightingId=0;
        foreach(float x in new[]{-24f,-8f,8f,24f})
            foreach(float z in new[]{-13f,9f})
            {
                B(basement,"B1 ceiling luminaire "+(++lightingId),led,
                    x,-1.17f,z,1.40f,.075f,.36f,false);
                var light=new GameObject("B1 real warm-white parking light "+lightingId);
                light.transform.SetParent(basement,false);
                light.transform.localPosition=new Vector3(x,-1.22f,z)*K;
                var bulb=light.AddComponent<Light>();
                bulb.type=LightType.Point;
                bulb.range=16f*K;
                bulb.intensity=1.12f;
                bulb.color=new Color(1f,.95f,.86f);
                bulb.shadows=LightShadows.None;
                bulb.renderMode=LightRenderMode.ForceVertex;
            }
        Debug.Log("T11_B1_UNDERGROUND_32_BAYS columns=6 vertexLights=8 floor=73x53");
    }

    static void PlaceNativePolyHaven(Transform parent,GameObject prefab,
        string label,float x,float y,float z,float height,float width,int index)
    {
        var anchor=Group(parent,label);
        anchor.localPosition=new Vector3(x,y,z)*K;
        var planted=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!planted)throw new InvalidOperationException("T11 original Poly Haven GLTF did not instantiate: "+label);
        planted.transform.SetParent(anchor,false);
        planted.transform.localPosition=Vector3.zero;
        planted.transform.localRotation=Quaternion.Euler(0,(index*79)%360,0);
        var renderer=planted.GetComponentsInChildren<Renderer>(true);
        if(renderer.Length==0)throw new InvalidOperationException("Poly Haven GLTF has zero visible meshes: "+label);
        Bounds bounds=renderer[0].bounds;
        foreach(var r in renderer.Skip(1))bounds.Encapsulate(r.bounds);
        float sourceHeight=bounds.size.y,sourceWidth=Mathf.Max(bounds.size.x,bounds.size.z);
        if(sourceHeight<.001f||sourceWidth<.001f)
            throw new InvalidOperationException("Invalid original Poly Haven GLTF bounds: "+label);
        float factor=Mathf.Min(height*K/sourceHeight,width*K/sourceWidth);
        planted.transform.localScale*=factor; // uniform; shape and original textures untouched
        bounds=renderer[0].bounds;
        foreach(var r in renderer.Skip(1))bounds.Encapsulate(r.bounds);
        planted.transform.position+=anchor.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        // Reuse one cheap trunk collider, never a collider for each leaf triangle.
        foreach(var collider in planted.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        var trunk=anchor.gameObject.AddComponent<CapsuleCollider>();
        trunk.center=Vector3.up*(height*K*.45f);
        trunk.height=height*K*.9f;trunk.radius=width*K*.16f;
        foreach(var r in renderer)
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.OccludeeStatic);
    }

    static void PolyHavenVegetation2K()
    {
        const string basePath="Assets/Source/Mistrzowie/CasinoT11/Vegetation2K/";
        var shrub=AssetDatabase.LoadAssetAtPath<GameObject>(
            basePath+"shrub_03/shrub_03_2k_embedded.glb")
            ?? AssetDatabase.LoadAllAssetsAtPath(basePath+"shrub_03/shrub_03_2k_embedded.glb").OfType<GameObject>().FirstOrDefault();
        var tree=AssetDatabase.LoadAssetAtPath<GameObject>(
            basePath+"quiver_tree_02/quiver_tree_02_2k_embedded.glb")
            ?? AssetDatabase.LoadAllAssetsAtPath(basePath+"quiver_tree_02/quiver_tree_02_2k_embedded.glb").OfType<GameObject>().FirstOrDefault();
        if(!shrub||!tree)
            throw new FileNotFoundException("T11 original Poly Haven 2K lossless embedded GLB missing; check prepare_casino_vegetation_2k.py");
        int count=0;
        foreach(float side in new[]{-1f,1f})
        foreach(float z in new[]{-32f,33f})
        {
            float x=side>0&&z<0?37.6f:side*43f;
            // 10.11: one 1.5 m shrub in a 3.4 m planter read as an empty black block; four fill the bed.
            foreach(var o in new[]{new Vector2(-.80f,-.76f),new Vector2(.80f,-.76f),new Vector2(-.80f,.76f),new Vector2(.80f,.76f)})
                PlaceNativePolyHaven(shell,shrub,"T11 original Poly Haven Shrub 03 (2K)",
                    x+o.x,.80f,z+o.y,1.55f,1.75f,count++);
        }
        // The two tree trunks are beyond the rear service door and outside
        // the fire road / delivery bays. Do not replace the approved palms.
        foreach(float x in new[]{-27f,27f})
            PlaceNativePolyHaven(shell,tree,"T11 original Poly Haven Quiver Tree 02 (2K)",
                x,.24f,38f,2.7f,2.9f,count++);
    }

    static void Site()
    {
        // Original 120x100 lot. Below grade T11 ramp starts at Z=-22 and
        // has a fully horizontal LEFT-TURN floor from Z=17 to Z=29.
        // All surface slabs are DISJOINT in plan: no artificial upper pavement
        // over the descending incline, and no old ground collider in the tunnel.
        // The turning tunnel's WALKABLE ROOF is separate, with top exactly
        // aligned with existing patio paving at y=.2075, 4.1m B1 headroom.
        B(shell,"T11 stone plot apron south",paving,
            0,.18f,-35.875f,119.6f,.055f,27.75f,true,true);
        B(shell,"T11 stone plot apron north",paving,
            0,.18f,39.375f,119.6f,.055f,20.75f,true,true);
        B(shell,"T11 stone plot apron west of garage",paving,
            -10.325f,.18f,1f,98.95f,.055f,46f,true,true);
        B(shell,"T11 stone plot apron east of garage",paving,
            54.55f,.18f,1f,10.5f,.055f,46f,true,true);
        // Reconnect both pavement wings from Z=24 to Z=29, but never cover
        // the actual 10.15m roadway void or duplicate an upper slab there.
        B(shell,"T11 stone plot apron west of bottom left turn",paving,
            -10.325f,.18f,26.5f,98.95f,.055f,5f,true,true);
        B(shell,"T11 stone plot apron east of bottom left turn",paving,
            54.55f,.18f,26.5f,10.5f,.055f,5f,true,true);
        // Intentional load-bearing roof, NOT another ground collider at B1:
        // overhead above the 12m x 9.35m level turning chamber.
        B(shell,"T11 B1 solid WALKABLE roof over left-turn tunnel",paving,
            44.225f,.0775f,23f,10.15f,.26f,12f,true,true);
        B(shell,"T11 B1 WHITE ceiling underside under turn roof",garageWhite,
            44.225f,-.063f,23f,10.15f,.015f,12f,false);
        // All exterior asphalt around T11 shares one exact finish elevation:
        // top y=.2485. Junctions meet EDGE-TO-EDGE; no duplicated road slabs.
        // The stone plot base finishes at y=.2075, so the wearing course begins
        // exactly there: physical contact without penetrating the base volume.
        const float roadY=.228f, roadH=.041f, roadTop=.2485f;
        void ConcreteCurb(string id,float x,float z,float w,float d)
            => B(trim,id,paving,x,roadTop+.070f,z,w,.14f,d,true,true);
        void Drain(string id,float x,float z,float w,float d)
            => B(trim,id,steel,x,roadTop+.006f,z,w,.012f,d,false,true);

        foreach(float side in new[]{-1f,1f})
        {
            // Side fire lanes occupy x=+-49.5..+-56.5. Front/rear roads stop
            // exactly at x=+-49.5 so there is no asphalt-on-asphalt overlap.
            B(shell,"Fire service route",asphalt,side*53f,roadY,3.25f,7.0f,roadH,81.5f,true,true);
            // Decorative champagne strip is inlaid into the stone apron;
            // the actual road kerb is concrete and physically separate.
            B(trim,"VIP/TAXI champagne paving inlay",gold,
                side*27f,.2135f,-36.4f,24f,.012f,.14f,false);
            // Owner 10.11 13:01: "Te parkingi mają zniknąć w ten sposób" - no painted bays on the drop-off loop; the cars
            // park in the underground garage (east ramp).
            foreach(var (px,z) in new[]{(side>0?37.6f:side*43f,-32f),(side*43f,33f),(side*26f,-33f)})
            {
                B(shell,"Champagne black granite planter",granite,px,.48f,z,3.4f,.6f,3.3f,true);
                // a planter, not a black block: champagne rim and a lawn bed the palm is planted in
                B(trim,"Planter champagne gold rim",gold,px,.795f,z,3.48f,.03f,3.38f,false);
                B(trim,"Planter lawn bed",palmBed?palmBed:roof,px,.812f,z,3.16f,.012f,3.06f,false);
            }
        }
        // Front road fills only the clear span between both fire lanes.
        B(shell,"Front VIP/TAXI circulating driveway",asphalt,0,roadY,-42.5f,99f,roadH,10f,true,true);

        // Proper concrete kerbs: low enough to read as road construction,
        // segmented at the central red-carpet/pedestrian axis and all road junctions.
        foreach(float side in new[]{-1f,1f})
        {
            if(side<0)ConcreteCurb("Front driveway NORTH concrete curb",side*26.85f,-37.41f,45.3f,.18f);
            else
            {
                // 9.11 owner "wjazd do parkingu poprawić": the right kerb ran straight ACROSS the garage driveway mouth
                // (x 39.55..48.9), a 14 cm step for every car. It now stops at the driveway and turns along both sides.
                ConcreteCurb("Front driveway NORTH concrete curb",21.875f,-37.41f,35.35f,.18f);
                ConcreteCurb("Garage driveway WEST concrete curb",39.46f,-30.66f,.18f,13.32f);
                ConcreteCurb("Garage driveway EAST concrete curb",49.11f,-29.94f,.42f,15.12f);
            }
            if(side<0)
            {
                // the south kerb opens for the drive from road Z330 (x -23.5..-16.5)
                ConcreteCurb("Front driveway SOUTH concrete curb west",-36.5f,-47.59f,26f,.18f);
                ConcreteCurb("Front driveway SOUTH concrete curb middle",-10.35f,-47.59f,12.3f,.18f);
            }
            else ConcreteCurb("Front driveway SOUTH concrete curb",side*26.85f,-47.59f,45.3f,.18f);
            ConcreteCurb("Fire lane INNER concrete curb",side*49.41f,3.25f,.18f,81.5f);
            ConcreteCurb("Fire lane OUTER concrete curb",side*56.59f,3.25f,.18f,81.5f);
        }
        // The 120x100 plan specifies UNDERGROUND parking, not two large surface
        // parking lots. The east court is reserved for its real accessible ramp.
        // Keep a pedestrian walkway on the WEST facade; east side remains a
        // vehicle approach separated from the existing fire route at x=53.
        B(shell,"T11 west pedestrian path",paving,-39.95f,.255f,4f,2.6f,.07f,25.7f,true,true);
        // Owner 10.11 00:22: "A co te pasy tam robią? Dla kogo te pasy są? Na pewno nie dla ludzi, ani dla
        // samochodów." The six short white stripes beside the red carpet are removed.
        // both street connections: real mouths with kerb arcs, ramp to the sidewalk, edging (BraxChampionsCasinoT11Street)
        StreetMouths();
        B(shell,"Casino pedestrian main axis",granite,0,.22f,-46.7f,8.1f,.08f,5.7f,true,true);          // z -49.55..-43.85, the ramp continues (no overlap)
        B(trim,"Red carpet leading to the curved casino entrance",redCarpet,
            0,.279f,-41.45f,5.8f,.016f,10.35f,false,true);
        foreach(float side in new[]{-1f,1f})
            B(trim,"Champagne bronze relief along red carpet",gold,
                side*2.96f,.290f,-41.45f,.075f,.045f,10.35f,false);
        // Rear service road also stops at the fire-lane edges and uses the
        // same asphalt top elevation as every other surface road.
        B(shell,"Rear service and deliveries",asphalt,0,roadY,46.7f,99f,roadH,5.4f,true,true);
        // Loading connector runs from rear wall z=28 to road edge z=44 exactly:
        // no 1.35m coplanar overlap with the rear service road.
        B(shell,"Delivery corridor to rear loading door",asphalt,-20f,roadY,36f,6.4f,roadH,16f,true,true);
        B(trim,"Rear loading bay white line",parkingPaint,-20f,roadTop+.006f,46.7f,12f,.012f,.11f,false);
        // Rear-road kerbs, with a 7m opening kept for the loading connector.
        ConcreteCurb("Rear service SOUTH concrete curb left",-36.5f,43.91f,26f,.18f);
        ConcreteCurb("Rear service SOUTH concrete curb right",16.5f,43.91f,66f,.18f);
        ConcreteCurb("Rear service NORTH concrete curb",0f,49.49f,99f,.18f);
        ConcreteCurb("Delivery corridor WEST concrete curb",-23.29f,35.9f,.18f,15.8f);
        ConcreteCurb("Delivery corridor EAST concrete curb",-16.71f,35.9f,.18f,15.8f);
        // Flush drainage details: visual only, never a wheel obstacle.
        Drain("Garage approach flush trench drain",44.225f,-22.18f,9.0f,.14f);
        Drain("Rear loading flush trench drain",-20f,28.16f,6.0f,.14f);
        // Real underground garage is generated separately from the empty casino
        // shell. Approach starts off the approved VIP/TAXI loop on the east side.
        UndergroundGarage();
        // Existing owned palm models (no fabricated vegetation).
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm)throw new FileNotFoundException("T11 original city palms missing");
        // owner 10.11 12:39 ("palma w kostce rośnie"): every palm in the centre of its granite planter, in the lawn bed
        foreach(var at in new[]{
            new Vector2(-43,-32),new Vector2(37.6f,-32),new Vector2(-43,33),
            new Vector2(43,33),new Vector2(-26,-33),new Vector2(26,-33)
        })
        {
            var location=shell.TransformPoint(new Vector3(at.x,.818f,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(shell,palm,"T11 approved city palm",location,0f,1f,false,true);
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
            BraxChampionsPalmTrunk.OnSpot(instance,location);
        }
        // Real original 2K Poly Haven vegetation, with existing palms intact.
        PolyHavenVegetation2K();
        foreach(float side in new[]{-1f,1f})
            for(int i=0;i<6;i++)
                // east: x 48 was on the garage driveway / in the air over the ramp; now on the fire-lane kerb
                B(trim,"Ground-level warm gold exterior bollard",led,side>0?49.41f:-48f,side>0?.8135f:.62f,-25f+i*10f,.12f,.85f,.12f,false);
    }

    static void PremiumExteriorLighting()
    {
        // Mobile-first Issue #29: emissive fixtures only; T11 adds zero realtime facade Point Lights.
        // Underground B1 lighting is outside this exterior-only task and remains unchanged.
        var fixtures=new[]
        {
            new Vector3(-14f,4.6f,-30.5f),new Vector3(0f,5.4f,-34.3f),
            new Vector3(14f,4.6f,-30.5f),new Vector3(-37.2f,5.3f,0f),
            new Vector3(37.2f,5.3f,0f),new Vector3(0f,5.1f,28.7f)
        };
        for(int i=0;i<fixtures.Length;i++)
        {
            var p=fixtures[i];
            BeveledBox(trim,"T11 champagne facade sconce "+(i+1),gold,
                p.x,p.y,p.z,.26f,.45f,.24f,.060f,false);
            BeveledBox(trim,"T11 warm LED luminaire diffuser "+(i+1),led,
                p.x,p.y-.235f,p.z,.18f,.055f,.16f,.018f,false);
        }

        // Issue #29 explicitly requires convincing metal/glass reflections without realtime lighting.
        // This is one tiny Editor-baked cubemap: no runtime refresh, no realtime probe rendering.
        var probeObject=new GameObject("T11 mobile baked reflection probe | 64px");
        probeObject.transform.SetParent(root,false);
        probeObject.transform.localPosition=new Vector3(0,7f,-2f)*K;
        var probe=probeObject.AddComponent<ReflectionProbe>();
        probe.mode=ReflectionProbeMode.Baked;
        probe.resolution=64;
        probe.size=new Vector3(92f,26f,72f)*K;
        probe.intensity=.90f;
        probe.boxProjection=true;
        probe.importance=2;
        probe.cullingMask=~0;
        probe.nearClipPlane=.10f*K;
        probe.farClipPlane=120f*K;
        string probePath=output+"/Textures/T11_Casino_Baked_Reflection.exr";
        Directory.CreateDirectory(output+"/Textures");
        if(!Lightmapping.BakeReflectionProbe(probe,probePath))
            throw new InvalidOperationException("T11 baked reflection probe failed");
        AssetDatabase.ImportAsset(probePath,ImportAssetOptions.ForceSynchronousImport);
        var baked=AssetDatabase.LoadAssetAtPath<Texture>(probePath);
        if(!baked)
            throw new InvalidOperationException("T11 baked reflection cubemap asset missing: "+probePath);
        probe.bakedTexture=baked;
        Debug.Log("T11_REFLECTION_PROBE_PASS mode=BAKED resolution=64 runtimeRefresh=NO cubemap="+probePath);
    }

    static string Sha256File(string path)
    {
        using(var stream=File.OpenRead(path))
        using(var sha=SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }

    static void ExtractReleaseEntry(ZipArchive archive,string basename,string destination,string expectedSha)
    {
        var matches=archive.Entries.Where(e=>
            string.Equals(Path.GetFileName(e.FullName),basename,StringComparison.OrdinalIgnoreCase)).ToArray();
        if(matches.Length!=1)
            throw new InvalidOperationException("Pinned T11 originals release must contain exactly one "+basename+
                "; found="+matches.Length);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        using(var input=matches[0].Open())
        using(var outputFile=new FileStream(destination,FileMode.Create,FileAccess.Write,FileShare.None))
            input.CopyTo(outputFile);
        if(Sha256File(destination)!=expectedSha)
        {
            File.Delete(destination);
            throw new InvalidOperationException("Restored owner sign SHA-256 mismatch: "+basename);
        }
        var meta=archive.Entries.FirstOrDefault(e=>
            string.Equals(e.FullName,matches[0].FullName+".meta",StringComparison.OrdinalIgnoreCase));
        if(meta!=null&&!File.Exists(destination+".meta"))
        {
            using(var input=meta.Open())
            using(var outputFile=new FileStream(destination+".meta",FileMode.Create,FileAccess.Write,FileShare.None))
                input.CopyTo(outputFile);
        }
    }

    static void EnsureT11OwnerSigns()
    {
        var required=new[]{
            new[]{SignPath,"C1_KASYNO_KASYNO_WAWER.png",SignSha256},
            new[]{TaxiSignPath,"TAXI_TAXI.png",TaxiSignSha256},
            new[]{ParkingSignPath,"PARKING_PARKING.png",ParkingSignSha256}
        };
        var missing=new System.Collections.Generic.List<string[]>();
        foreach(var item in required)
        {
            if(File.Exists(item[0]))
            {
                if(Sha256File(item[0])!=item[2])
                    throw new InvalidOperationException("Existing owner sign differs from approved manifest: "+item[0]);
            }
            else missing.Add(item);
        }
        if(missing.Count==0)return;

        string zip=Path.Combine(Path.GetTempPath(),"BRAXHORN_T11_ORIGINALY_43ea47f0.zip");
        try
        {
            if(File.Exists(zip))File.Delete(zip);
            using(var client=new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent]="BRAXHORN-Unity6-T11-SourceRestore/1.0";
                client.DownloadFile(OwnerOriginalsReleaseUrl,zip);
            }
            if(Sha256File(zip)!=OwnerOriginalsZipSha256)
                throw new InvalidOperationException("Pinned A1/T11 originals Release ZIP SHA-256 mismatch");
            using(var archive=ZipFile.OpenRead(zip))
                foreach(var item in missing)ExtractReleaseEntry(archive,item[1],item[0],item[2]);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("BRAX_T11_OWNER_SIGNS_RESTORED release=champions-a1-20261105 files="+missing.Count+
                " zip_sha256="+OwnerOriginalsZipSha256);
        }
        finally
        {
            if(File.Exists(zip))File.Delete(zip);
        }
    }

    // A source image's approved 2048x512 dimensions are not necessarily the
    // same as Unity's imported Android Texture2D dimensions (platform overrides).
    static Texture2D LoadOwnerSign(string path)
    {
        if(!File.Exists(path))
            throw new FileNotFoundException("Owner's original signage is genuinely missing: "+path,path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("No TextureImporter for original owner sign: "+path);
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        if(width!=2048||height!=512)
            throw new InvalidOperationException("Original owner sign dimensions changed: "+path+
                " source="+width+"x"+height+" expected=2048x512");
        if(path==SignPath)
        {
            const string expected=SignSha256;
            using(var stream=File.OpenRead(path))
            using(var sha=SHA256.Create())
            {
                string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
                if(actual!=expected)
                    throw new InvalidOperationException("C1 KASYNO WAWER source SHA-256 differs from owner manifest");
            }
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if(!texture)throw new InvalidOperationException("Owner's sign is on disk but Unity cannot import it: "+path);
        // Imported Android dimensions may be platform-compressed; source dimensions above are authoritative.
        return texture;
    }

    public static void VerifyOriginalSigns()
    {
        EnsureT11OwnerSigns();
        LoadOwnerSign(SignPath);
        LoadOwnerSign(TaxiSignPath);
        LoadOwnerSign(ParkingSignPath);
        Debug.Log("CHAMPIONS_T11_OWNER_SIGNS_PASS originals=3 untouched=YES source_size=2048x512");
    }

    static void RoadsideOriginalSign(string id,string source,float x,float z,float yaw)
    {
        // Preserve the owner's original 2048 x 512 artwork, including typography,
        // colour and bull logo; fit 4:1 without stretching, and use real posts.
        var texture=LoadOwnerSign(source);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Original sign PBR shader missing");
        var material=new Material(shader){name="T11 original roadside "+id,enableInstancing=true};
        material.SetTexture("_MainTex",texture);
        material.SetTexture("_EmissionMap",texture);
        material.SetColor("_EmissionColor",new Color(.13f,.10f,.06f));
        material.SetColor("_Color",Color.white);
        material.SetFloat("_Roughness",.35f);
        material.SetFloat("_Metallic",0);
        material.SetFloat("_VertexTint",0);
        material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.08f);
        material.SetOverrideTag("RenderType","TransparentCutout");
        material.renderQueue=2450;
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,output+"/Materials/T11_roadside_"+id+".mat");
        var stand=Group(signRoot,"Approved "+id+" | original CitySigns 4x1");
        stand.localPosition=new Vector3(x,0,z)*K;
        stand.localRotation=Quaternion.Euler(0,yaw,0);
        B(stand,"Premium matte-black sign carrier",graphite,0,2.55f,0,4.22f,1.20f,.30f);
        B(stand,"Gold sign bottom relief",gold,0,1.95f,-.16f,4.27f,.095f,.09f,false);
        foreach(float side in new[]{-1f,1f})
            B(stand,"Champagne supporting sign column",gold,side*1.83f,1.05f,.025f,.12f,2.15f,.14f);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name=id+" | original owner sign texture 4:1";
        quad.transform.SetParent(stand,false);
        quad.transform.localPosition=new Vector3(0,2.55f,-.171f)*K;
        quad.transform.localRotation=Quaternion.Euler(0,180,0);
        quad.transform.localScale=new Vector3(4f,1f,1f)*K;
        quad.GetComponent<MeshRenderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
    }

    static void RoadsideSigns()
    {
        // Owner 7.11: remove the freestanding PARKING sign from the garage approach.
        // Owner 9.11 23:36: the TAXI board by the garage entrance stood empty ("jakaś tabliczka stoi": its
        // picture Quad faced away from the street) - removed; the garage portal carries a 3D PARKING sign.
        GarageSign();
    }

    // Issue #29 sign carrier contour was derived from the actual alpha silhouette
    // of owner SHA-256 78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b.
    // The source is 2048x512 RGBA; alpha bbox is x=34..2013, y=50..460.
    // Verified against the full alpha convex envelope: clearance 0.085..0.109m; gold ring 0.070m.
    static readonly Vector2[] SignCarrierOuter={
        new Vector2(-6.8699f,-1.8233f),new Vector2(-7.4314f,-1.6857f),
        new Vector2(-7.7120f,-1.5277f),new Vector2(-7.9723f,-1.3040f),
        new Vector2(-8.2906f,-.8219f),new Vector2(-8.4245f,-.2687f),
        new Vector2(-8.4239f,.2965f),new Vector2(-8.3624f,.6398f),
        new Vector2(-7.9248f,1.5459f),new Vector2(-7.8570f,1.6008f),
        new Vector2(-7.0283f,1.8222f),new Vector2(6.8725f,1.8395f),
        new Vector2(7.8970f,1.5801f),new Vector2(8.2822f,.8387f),
        new Vector2(8.3982f,.4439f),new Vector2(8.3982f,-.4271f),
        new Vector2(8.3158f,-.7379f),new Vector2(8.1736f,-1.0308f),
        new Vector2(7.9639f,-1.3040f),new Vector2(7.7036f,-1.5277f),
        new Vector2(7.4147f,-1.6894f),new Vector2(7.0199f,-1.8054f)
    };
    static readonly Vector2[] SignCarrierInner={
        new Vector2(-6.8615f,-1.7533f),new Vector2(-7.4054f,-1.6200f),
        new Vector2(-7.6716f,-1.4701f),new Vector2(-7.9193f,-1.2572f),
        new Vector2(-8.2255f,-.7935f),new Vector2(-8.3544f,-.2604f),
        new Vector2(-8.3539f,.2902f),new Vector2(-8.2952f,.6180f),
        new Vector2(-7.8686f,1.5014f),new Vector2(-7.8245f,1.5371f),
        new Vector2(-7.0190f,1.7522f),new Vector2(6.8638f,1.7695f),
        new Vector2(7.8493f,1.5199f),new Vector2(8.2170f,.8125f),
        new Vector2(8.3282f,.4338f),new Vector2(8.3282f,-.4180f),
        new Vector2(8.2499f,-.7134f),new Vector2(8.1138f,-.9938f),
        new Vector2(7.9128f,-1.2556f),new Vector2(7.6633f,-1.4700f),
        new Vector2(7.3874f,-1.6245f),new Vector2(7.0097f,-1.7354f)
    };

    static GameObject PolygonPrism(Transform parent,string name,Material mat,
        Vector2[] loop,float centreY,float centreZ,float depth,bool solid)
    {
        int n=loop.Length;
        if(n<3)throw new InvalidOperationException("T11 sign carrier needs >=3 contour points");
        var verts=new System.Collections.Generic.List<Vector3>(n*2);
        var uv=new System.Collections.Generic.List<Vector2>(n*2);
        for(int layer=0;layer<2;layer++)
        {
            float z=(layer==0?-depth*.5f:depth*.5f)*K;
            foreach(var p in loop){verts.Add(new Vector3(p.x,p.y,0)*K+Vector3.forward*z);uv.Add(p);}
        }
        var tris=new System.Collections.Generic.List<int>();
        void AddTriOriented(int a,int b,int d,Vector3 desired)
        {
            Vector3 normal=Vector3.Cross(verts[b]-verts[a],verts[d]-verts[a]);
            if(Vector3.Dot(normal,desired)<0){int t=b;b=d;d=t;}
            tris.Add(a);tris.Add(b);tris.Add(d);
        }
        for(int i=1;i<n-1;i++)
        {
            AddTriOriented(0,i,i+1,Vector3.back);
            AddTriOriented(n,n+i+1,n+i,Vector3.forward);
        }
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;
            Vector3 radial=new Vector3(loop[i].x+loop[j].x,loop[i].y+loop[j].y,0).normalized;
            AddTriOriented(i,j,n+j,radial);
            AddTriOriented(i,n+j,n+i,radial);
        }
        var mesh=new Mesh{name=name+" mesh"};
        mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_sign_body_"+(serial++)+".asset");
        var o=new GameObject(name);o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(0,centreY,centreZ)*K;
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(solid)o.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(o,
            StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return o;
    }

    static GameObject PolygonRing(Transform parent,string name,Material mat,
        Vector2[] outer,Vector2[] inner,float centreY,float centreZ,float depth)
    {
        if(outer.Length!=inner.Length||outer.Length<3)
            throw new InvalidOperationException("T11 sign gold ring contour mismatch");
        int n=outer.Length;
        var verts=new System.Collections.Generic.List<Vector3>(n*4);
        var uv=new System.Collections.Generic.List<Vector2>(n*4);
        foreach(float z in new[]{-depth*.5f,depth*.5f})
        {
            foreach(var p in outer){verts.Add(new Vector3(p.x,p.y,z)*K);uv.Add(p);}
            foreach(var p in inner){verts.Add(new Vector3(p.x,p.y,z)*K);uv.Add(p);}
        }
        var tris=new System.Collections.Generic.List<int>();
        void Quad(int a,int b,int d,int e,Vector3 desired)
        {
            Vector3 normal=Vector3.Cross(verts[b]-verts[a],verts[d]-verts[a]);
            if(Vector3.Dot(normal,desired)<0){int t=b;b=e;e=t;}
            tris.Add(a);tris.Add(b);tris.Add(d);
            tris.Add(a);tris.Add(d);tris.Add(e);
        }
        int fo=0,fi=n,bo=2*n,bi=3*n;
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;
            Quad(fo+i,fo+j,fi+j,fi+i,Vector3.back);
            Quad(bo+i,bi+i,bi+j,bo+j,Vector3.forward);
            Vector3 outRadial=new Vector3(outer[i].x+outer[j].x,outer[i].y+outer[j].y,0).normalized;
            Quad(fo+i,bo+i,bo+j,fo+j,outRadial);
            Vector3 inRadial=-new Vector3(inner[i].x+inner[j].x,inner[i].y+inner[j].y,0).normalized;
            Quad(fi+i,fi+j,bi+j,bi+i,inRadial);
        }
        var mesh=new Mesh{name=name+" mesh"};
        mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_sign_gold_ring_"+(serial++)+".asset");
        var o=new GameObject(name);o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(0,centreY,centreZ)*K;
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=mat;
        GameObjectUtility.SetStaticEditorFlags(o,
            StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return o;
    }

    // Owner 10.11 16:36-16:45: no rounded black roof under the name, the name "lepszy i czytelniejszy", "premium
    // premium", and of his two reference pictures "ten drugi lepszy": heavy gold capitals in a red outline and a gold
    // rim, the four card suits fanned behind the top, gold ribbons with a red stripe flying from both ends and white
    // sparkles (build_casino_sign.py roof_crown, one layered relief in real depth). 17:15: "na tym zaokrąglonym na górze
    // powinien być ten szyld" - the relief is bent round the glass rotunda and stands on its thin cream coping (10.95)
    // on slim black steel posts (part of the sign mesh), 1.75 m capitals right over the entrance.
    public const float CrownZ=-28f,CrownBaseY=11.316f;
    static void CasinoName()
    {
        if(!Place3DSign("KASYNO_WAWER_crown",signRoot,"T11 KASYNO WAWER crown sign | round the rotunda",new Vector3(0,CrownBaseY,CrownZ),180f,1f))
        {
            BraxBuildQuality.Warn("DO POPRAWKI T11_NAME_CROWN_MISSING");
            return;
        }
        Debug.Log("T11_NAME_CROWN 'KASYNO WAWER' cap=1.75 gold+redOutline+goldRim suits=4 ribbons=2 sparkles=5 bentRoundRotunda=16.52x7.52"+
            " onCoping=10.95 rotundaRoof=thin_cream_coping blackCap=REMOVED letterRail=REMOVED");
    }
    static void MainSign()
    {
        PolygonPrism(signRoot,"T11 WAWER sign real-outline graphite metal carrier",
            signBacking,SignCarrierOuter,12.0f,-36.14f,.24f,true);
        PolygonRing(signRoot,"T11 WAWER sign real-outline rounded champagne gold border",
            gold,SignCarrierOuter,SignCarrierInner,12.0f,-36.2725f,.025f);
        // 3D gold WAWER / KASYNO on the carrier (10 x 3.24 m, centred at y 12.0); the old PNG Quad faced into the building
        // composition box x +-6.6, y -.38..3.10, depth .36: at .93 it fills 12.3 x 3.24 m of the 16.8 x 3.65 m carrier
        if(Place3DSign("WAWER_kasyno",signRoot,"T11 3D gold KASYNO WAWER sign | rotunda",new Vector3(0,10.735f,-36.29f),180f,.93f))
        {
            SignMarquee();
            Debug.Log("T11_3D_SIGN WAWER_KASYNO gold solid glyphs parts="+sign3DParts);
            return;
        }
        // fallback: the owner's original PNG, facing the street (Quad front is -Z: no rotation)
        var picture=LoadOwnerSign(SignPath);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Casino sign needs original CityStaticPBR shader");
        var material=new Material(shader){name="C1 KASYNO WAWER original",enableInstancing=true};
        material.SetTexture("_MainTex",picture);material.SetTexture("_EmissionMap",picture);
        material.SetColor("_EmissionColor",new Color(.11f,.08f,.045f));
        material.SetColor("_Color",Color.white);material.SetFloat("_VertexTint",0);
        material.SetFloat("_Roughness",.30f);material.SetFloat("_Metallic",0);
        material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.08f);
        material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,output+"/Materials/T11_original_WAWER_casino_sign.mat");
        var image=GameObject.CreatePrimitive(PrimitiveType.Quad);
        image.name="T11 | ORIGINAL C1 KASYNO WAWER | 2048x512 | 4:1";
        image.transform.SetParent(signRoot,false);
        image.transform.localPosition=new Vector3(0,12.0f,-36.34f)*K;
        image.transform.localScale=new Vector3(17.2f,4.30f,1f)*K;
        image.GetComponent<MeshRenderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(image.GetComponent<Collider>());
        Debug.Log("T11_ISSUE29_SIGN ORIGINAL_SIGN_PRESENT=SOURCE_CHECK_PASS LABEL_ASPECT_4_TO_1=SOURCE_GEOMETRY_PASS "+
            "BACKPLATE_FOLLOWS_REAL_LABEL_OUTLINE=SOURCE_GEOMETRY_PASS GOLD_BEVEL_ROUNDED=SOURCE_GEOMETRY_PASS "+
            "FALLBACK_PICTURE_FACES_STREET=YES UNITY_SCREENSHOTS_ATTACHED=PENDING_REAL_UNITY_RENDER");
    }

    public static void Generate(string outFolder)
    {
        Directory.CreateDirectory(outFolder+"/Meshes");
        Directory.CreateDirectory(outFolder+"/Materials");
        Directory.CreateDirectory(outFolder+"/Prefabs");
        Setup(outFolder);
        root=Group(null,"T11 | KASYNO WAWER | 120x100 owner board plot | rotunda lobby, no gaming floor yet");
        root.position=new Vector3(LotX,0,LotZ)*K;
        shell=Group(root,"T11 exterior shell, real entrances and site collisions");
        trim=Group(root,"Exterior gold and bronze details (distance LOD)");
        signRoot=Group(root,"T11 original owner sign (always visible)");
        Site();ReferenceFacades();RoofAndSkylight();CurvedFoyer();
        BraxChampionsFoundation.Optional("kasyno T11: lobby rotundy z żyrandolem",RotundaLobby);
        BraxChampionsFoundation.Optional("kasyno T11: otwarta sala kasyna",CasinoHall);
        // Owner board 9.11 shows no rooftop $: kept in code, off until the owner decides.
        // The 120 m office tower on the board is "docelowo" (later, separate building).
        if(RoofDollarOnOwnerBoard)RoofDollar();
        sign3D=null;sign3DParts=0;signGold=null;signGoldDeep=null;
        // Owner 10.11 13:03: the curved sign band with the crown and KASYNO WAWER is removed ("mało czytelny ... całe do
        // zdjęcia"); the name is premium 3D lettering standing on the slim rotunda roof (CasinoName).
        CasinoName();RoadsideSigns();PremiumExteriorLighting();
        // Remove tiny gold detail in distance, keep WAWER sign always visible.
        var renderers=trim.GetComponentsInChildren<Renderer>(true);
        var lod=trim.gameObject.AddComponent<LODGroup>();
        // 9.11: culling the whole casino below 9% of screen height made it pop in.
        lod.SetLODs(new[]{new LOD(.012f,renderers),new LOD(.0001f,Array.Empty<Renderer>())});
        lod.RecalculateBounds();
        var at=root.position;root.position=Vector3.zero;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,
            outFolder+"/Prefabs/T11_Casino_Exterior.prefab",InteractionMode.AutomatedAction);
        root.position=at;
        if(!prefab)throw new IOException("T11 exterior prefab save failed");
        Debug.Log("CHAMPIONS_T11_OWNER_BOARD_9_11 position=(495,465) lot=120x100 podium=76x56x14"+
            " facade=cream_floor_tiles_04+graphite_rectangular_facade_tiles+granite_tile windows=front6+side16"+
            " pylons=graphite_goldLED rotunda=glass48+lobby_chandelier rooftopDollar="+RoofDollarOnOwnerBoard+
            " tower=LATER originalSign=C1_WAWER exteriorPointLights=0 lobbyVertexLights=2 visualReview=PENDING");
    }
}
