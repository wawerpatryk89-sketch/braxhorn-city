using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// P1 BASEN MIEJSKI + SPA. The owner's plan is the dimensional authority.
// Generates only P1 on the fixed master-plan plot (520,180), 160x120 nominal m.
// Three real sunlit blue-tiled pools WITH WATER and swimming triggers per owner approval. Jacuzzi bubbles activate with original tub.
// This module is isolated from Marina/Beach, A1, T01, T11 and their source assets.
public static class BraxChampionsPoolP1
{
    const string LayoutPath="Assets/Source/Mistrzowie/PoolP1/plot.json";
    const string Output="Assets/PlayableGenerated/Mistrzowie/Buildings/P1";
    const float LotX=520f, LotZ=180f, Deck=.26f;
    static float K=>BraxClaudeOsiedle.K;
    static Transform root,structure,rooms,outdoors,parking,roof,details;
    static Material facade,white,stone,floor,blue,lightBlue,smallMosaic,paving,patternPaving,asphalt,wood,roofFinish;
    static Material glass,frame,steel,led,paint,azure,navy,gold,plants;
    static int meshSerial,parked;
    [Serializable] sealed class Data
    {
        public string schema;public Plot lot;public Basin[] basins;
        public bool waterDeferred,waterReady;public int parkingSpaces,texResolution;
    }
    [Serializable] sealed class Plot
    {
        public string id;public float x,z,w,d,maximumHeight;
    }
    [Serializable] sealed class Basin
    {
        public string id;public float x,z,w,d,depth;
    }
    static Data layout;

    static Data ReadLayout()
    {
        if(!File.Exists(LayoutPath))throw new FileNotFoundException("P1 approved layout missing",LayoutPath);
        var d=JsonUtility.FromJson<Data>(File.ReadAllText(LayoutPath));
        if(d==null||d.lot==null||d.basins==null||d.basins.Length!=3
            ||d.lot.id!="P1"||d.lot.x!=LotX||d.lot.z!=LotZ||d.lot.w!=160f||d.lot.d!=120f
            ||d.texResolution!=2048||d.parkingSpaces!=95||d.waterDeferred||!d.waterReady)
            throw new InvalidOperationException("P1 plot, source resolution or empty-pool contract changed");
        var expected=new[]{"SPORT_25M","FAMILY","CHILDREN"};
        foreach(var id in expected)
            if(d.basins.Count(b=>b.id==id)!=1)throw new InvalidOperationException("Missing original P1 basin: "+id);
        foreach(var b in d.basins)
        {
            if(b.depth<=0f||b.w<=0f||b.d<=0f
                ||Mathf.Abs(b.x-LotX)+b.w/2f>80f ||Mathf.Abs(b.z-LotZ)+b.d/2f>60f)
                throw new InvalidOperationException("P1 basin outside owner-approved lot: "+b.id);
        }
        return d;
    }

    public static void Generate()
    {
        layout=ReadLayout();
        if(AssetDatabase.IsValidFolder(Output)||Directory.Exists(Output))
            throw new InvalidOperationException("P1 exists; never overwrite the owner's saved scene or existing GUIDs. Use a reviewed update.");
        Directory.CreateDirectory(Output+"/Materials");
        Directory.CreateDirectory(Output+"/Meshes");
        meshSerial=0;parked=0;
        SetupMaterials();
        root=new GameObject("P1 | BASEN MIEJSKI + SPA | 160x120 | WATER FILLED SWIMMABLE POOLS").transform;
        root.position=new Vector3(LotX*K,0,LotZ*K);
        structure=Group(root,"P1 | halls, actual open rooms, walls and roofs");
        rooms=Group(root,"P1 | tiled interiors, changing rooms, reception and SPA");
        details=Group(root,"P1 | architectural glazing, lights, handrails");
        roof=Group(root,"P1 | curved roof and clerestory");
        outdoors=Group(root,"P1 | landscape, terrace, paving and access");
        parking=Group(root,"P1 | 95 marked bays plus 6 TAXI");

        MainSportsHall();
        RecreationWing();
        SpaAndReception();
        ConnectWings();
        SpaMassageRooms();
        foreach(var b in layout.basins)BuildBlueBasin(b);
        FixturesAndFurniture();
        EstateAndParking();
        BraxChampionsPoolP1Slide.Generate(outdoors,paving,steel,blue);
        // Original SPA prefabs are mounted only from real owner source assets.
        // Any still-unimported model is reported by the furnishing module.
        BraxChampionsPoolP1SpaFurnishing.Generate(rooms,details,smallMosaic,steel);
        BraxChampionsPoolP1Equipment.InstallRemaining(rooms,outdoors);
        NightAndInteriorLights();

        if(parked!=layout.parkingSpaces)
            throw new InvalidOperationException("P1 real marked parking bay count is "+parked+" instead of 95");
        if(root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterial==null))
            throw new InvalidOperationException("P1 has a renderer without a real finish");
        // World plot coordinates are applied only once, and keep the source K unchanged.
        var at=root.position;root.position=Vector3.zero;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,
            Output+"/P1_BASEN_MIEJSKI_SPA.prefab",InteractionMode.AutomatedAction);
        root.position=at;
        if(!prefab)throw new IOException("P1 prefab serialization failed");
        AssetDatabase.SaveAssets();
        Debug.Log("P1 BASEN SOURCE_AUTHORING_READY_FOR_UNITY_REVIEW | 95 spaces | three WATER-FILLED swimming/wading basins | original jacuzzi model pending | original sign artwork required | plot (520,180) | Unity compile and phone test not run");
    }
    static Transform Group(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    static void SetupMaterials()
    {
        facade=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_facade_concrete","rectangular_facade_tiles",2.4f,new Color(.90f,.91f,.90f));
        white=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_bright_white_stucco","white_stucco_02",1.3f,Color.white);
        stone=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_granite_03","granite_tile_03",1.35f,new Color(.85f,.88f,.88f));
        floor=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_white_ceramic_floor","long_white_tiles",.72f,new Color(.97f,.99f,1f));
        blue=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_sports_pool_cyan_tiles","long_white_tiles",.50f,new Color(.13f,.61f,.94f));
        lightBlue=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_kids_pool_blue_tiles","long_white_tiles",.42f,new Color(.32f,.78f,1f));
        smallMosaic=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_spa_mosaic","marble_mosaic_tiles",.72f,new Color(.91f,.92f,.88f));
        paving=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_paving","concrete_pavement_02",1.8f,new Color(.94f,.95f,.93f));
        // A distinct warm-toned herringbone forecourt only on P1: keep the
        // established large concrete pavement in secondary technical paths.
        patternPaving=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_warm_herringbone_paving","herringbone_brick_02",1.8f,new Color(.93f,.85f,.76f));
        asphalt=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_black_asphalt","clean_asphalt",2.1f,new Color(.40f,.40f,.41f));
        wood=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_warm_teak_veneer","teak_veneer",1.5f,new Color(.95f,.82f,.64f));
        roofFinish=BraxChampionsPoolP1Pbr2K.Create(Output,"P1_roof","concrete_floor_02",2.8f,new Color(.92f,.95f,.98f));
        // Genuine clear glass: transparency is alpha-blended; never painted black/opaque.
        glass=Solid("P1_TRUE_CLEAR_GLASS",new Color(.90f,.965f,1f,.12f),.07f,0f,true);
        frame=Solid("P1_SILVER_ALUMINIUM_FRAMES",new Color(.72f,.77f,.80f),.30f,.86f);
        steel=Solid("P1_BRUSHED_STAINLESS_STEEL",new Color(.79f,.83f,.84f),.22f,.87f);
        paint=Solid("P1_WHITE_ROAD_AND_LANE_PAINT",new Color(.97f,.99f,1f),.68f,0);
        azure=Solid("P1_POOL_RIM_TURQUOISE",new Color(.11f,.68f,.90f),.18f,0);
        navy=Solid("P1_LANE_MARKER_NAVY",new Color(.025f,.18f,.38f),.35f,0);
        gold=Solid("P1_SIGN_BRUSHED_CHAMPAGNE",new Color(.78f,.65f,.42f),.34f,.70f);
        plants=Solid("P1_PLANTER_EARTH_TRIM",new Color(.33f,.38f,.33f),.85f,0);
        led=Solid("P1_WARM_WHITE_LIGHT_DIFFUSER",new Color(1f,.96f,.87f),.24f,0);
        led.SetColor("_EmissionColor",new Color(.9f,.71f,.49f));
        led.EnableKeyword("_EMISSION");
    }
    static Material Solid(string id,Color colour,float roughness,float metallic,bool transparency=false)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Built-in CityStaticPBR shader missing");
        var m=new Material(shader){name=id,enableInstancing=!transparency};
        m.SetColor("_Color",colour);m.SetFloat("_Roughness",roughness);
        m.SetFloat("_Metallic",metallic);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        if(transparency)
        {
            m.SetFloat("_Transparent",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",2);
            m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
        }
        AssetDatabase.CreateAsset(m,Output+"/Materials/"+id+".mat");
        return m;
    }
    // All coordinates and dimensions below are in nominal metres. Apply K ONCE.
    static GameObject Box(Transform parent,string name,Material mat,
        float x,float y,float z,float w,float h,float d,bool collision=true)
    {
        if(w<=0||h<=0||d<=0)throw new InvalidOperationException("Invalid P1 box: "+name);
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;
        o.transform.localScale=new Vector3(w,h,d)*K;
        var renderer=o.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;
        if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        if(mat==glass){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        else GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        if(mat.HasProperty("_MainTex")&&mat.GetTexture("_MainTex")&&Mathf.Max(w,h,d)>2.4f)
            TileUVInMetres(o,w,h,d);
        return o;
    }
    static void TileUVInMetres(GameObject o,float w,float h,float d)
    {
        // A 72m wall must not stretch a single ceramic tile across its full width.
        var f=o.GetComponent<MeshFilter>();
        var m=UnityEngine.Object.Instantiate(f.sharedMesh);
        var verts=m.vertices;var normals=m.normals;var uv=new Vector2[verts.Length];
        for(int i=0;i<verts.Length;i++)
        {
            var n=normals[i];var p=verts[i];
            if(Mathf.Abs(n.y)>.5f)uv[i]=new Vector2((p.x+.5f)*w,(p.z+.5f)*d);
            else if(Mathf.Abs(n.x)>.5f)uv[i]=new Vector2((p.z+.5f)*d,(p.y+.5f)*h);
            else uv[i]=new Vector2((p.x+.5f)*w,(p.y+.5f)*h);
        }
        m.uv=uv;m.name="P1_real_metre_tiling_"+meshSerial;
        AssetDatabase.CreateAsset(m,Output+"/Meshes/box_"+(meshSerial++)+".asset");
        f.sharedMesh=m;
    }
    static GameObject Cylinder(Transform parent,string name,Material mat,
        float x,float y,float z,float radius,float height)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;
        o.transform.SetParent(parent,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;
        o.transform.localScale=new Vector3(radius*2f,height*.5f,radius*2f)*K;
        o.GetComponent<MeshRenderer>().sharedMaterial=mat;
        return o;
    }
    static float RoofY(float x)
    {
        float t=Mathf.Clamp(x/36f,-1f,1f);
        return 14f+2f*(1f-t*t);
    }
    static void RoofQuad(float x0,float x1,float z0,float z1,Material mat,bool underside)
    {
        float y0=RoofY(x0)+(underside?-.19f:0), y1=RoofY(x1)+(underside?-.19f:0);
        var a=new Vector3(x0,y0,z0)*K;var b=new Vector3(x0,y0,z1)*K;
        var c=new Vector3(x1,y1,z1)*K;var d=new Vector3(x1,y1,z0)*K;
        var mesh=new Mesh{name="P1_smooth_arch_roof_"+meshSerial};
        mesh.vertices=new[]{a,b,c,d};mesh.uv=new[]{new Vector2(x0,z0),new Vector2(x0,z1),
            new Vector2(x1,z1),new Vector2(x1,z0)};
        mesh.triangles=underside?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"/Meshes/roof_"+(meshSerial++)+".asset");
        var o=new GameObject("Curved white roof segmented P1 | "+x0).transform;
        o.SetParent(roof,false);o.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    static void GlazedFront(float z,float x0,float x1,bool back)
    {
        for(float x=x0;x<x1-.01f;x+=3f)
        {
            float width=Mathf.Min(3f,x1-x),mid=x+width*.5f;
            // Keep the central passage to the SPA clear, not a glass barrier.
            if(!back&&mid>-7.8f&&mid<7.8f)continue;
            float h=RoofY(mid)-.65f;
            Box(details,"P1 clear curtain wall full-height (not black)",glass,
                mid,h*.5f,z,width-.12f,h,.085f,false);
            Box(details,"P1 silver vertical mullion",frame,x,.5f*h,z,.12f,h,.15f,false);
            foreach(float t in new[]{4.5f,9f})
                if(t<h-.5f)Box(details,"P1 glass transom",frame,mid,t,z,width,.12f,.16f,false);
        }
        Box(details,"P1 glazing corner mullion",frame,x1,RoofY(x1)*.5f,z,.17f,RoofY(x1),.17f,false);
    }
    static void GlazedSide(float x,float z0,float z1,bool west)
    {
        for(float z=z0;z<z1-.01f;z+=3f)
        {
            float length=Mathf.Min(3f,z1-z),mid=z+length*.5f;
            if(west&&mid>19.5f&&mid<28f)continue;
            float height=RoofY(x)-.6f;
            Box(details,"P1 bright visible interior through side glass",glass,
                x,height*.5f,mid,.09f,height,length-.12f,false);
            Box(details,"P1 thin silver side frame",frame,x,height*.5f,z,.16f,height,.14f,false);
            foreach(float level in new[]{4.5f,9f})
                Box(details,"P1 cross mullion",frame,x,level,mid,.16f,.09f,length,false);
        }
    }
    static void DeckWithBasins(string name,float xmin,float xmax,float zmin,float zmax)
    {
        var xs=new SortedSet<float>{xmin,xmax};
        var zs=new SortedSet<float>{zmin,zmax};
        foreach(var b in layout.basins)
        {
            float a=b.x-LotX-b.w*.5f,c=b.x-LotX+b.w*.5f;
            float n=b.z-LotZ-b.d*.5f,s=b.z-LotZ+b.d*.5f;
            if(c<=xmin||a>=xmax||s<=zmin||n>=zmax)continue;
            xs.Add(Mathf.Clamp(a,xmin,xmax));xs.Add(Mathf.Clamp(c,xmin,xmax));
            zs.Add(Mathf.Clamp(n,zmin,zmax));zs.Add(Mathf.Clamp(s,zmin,zmax));
        }
        var xa=xs.ToArray();var za=zs.ToArray();
        for(int i=0;i<xa.Length-1;i++)for(int j=0;j<za.Length-1;j++)
        {
            float x=(xa[i]+xa[i+1])*.5f,z=(za[j]+za[j+1])*.5f;
            if(layout.basins.Any(b=>Mathf.Abs(x-(b.x-LotX))<b.w*.5f&&Mathf.Abs(z-(b.z-LotZ))<b.d*.5f))
                continue; // ACTUAL DECK OPENING, never a pool sticker over solid ground.
            Box(structure,name+" | tiled floor slab",floor,x,Deck-.10f,z,
                xa[i+1]-xa[i],.20f,za[j+1]-za[j]);
        }
    }
    static void MainSportsHall()
    {
        // Main 72 x 42 m hall, z=+6..+48; overall 16m height.
        DeckWithBasins("SPORT HALL 72x42",-36,36,6,48);
        GlazedFront(6,-36,36,false);GlazedFront(48,-36,36,true);
        GlazedSide(36,6,48,false);GlazedSide(-36,6,48,true);
        for(int i=0;i<24;i++)
        {
            float x0=-36+i*3f,x1=x0+3f;
            RoofQuad(x0,x1,5.4f,48.6f,roofFinish,false);
            RoofQuad(x0,x1,5.4f,48.6f,white,true);
            if(i%2==0)
            {
                float x=x0,y=RoofY(x);
                Box(roof,"P1 curved white roof rib",frame,x,y-.08f,27,.15f,.38f,43.2f,false);
            }
        }
        for(float xx=-36;xx<=36;xx+=6)
            for(float zz=6;zz<=48;zz+=42)
                Box(structure,"P1 real lightweight frame support",frame,xx,5.95f,zz,.32f,11.9f,.32f);
        for(float zz=10;zz<=46;zz+=12)
        {
            Box(details,"P1 suspended linear lighting",led,-6,10.5f,zz,46f,.07f,.20f,false);
            Box(details,"P1 ceiling truss satin steel",frame,-6,10.7f,zz,46f,.16f,.18f,false);
        }
        // Rear technical services: three accessible internal rooms, not opaque slab.
        Room("POM. TECHNICZNE FILTRY",28.5f,42,13f,8f,3.8f,white);
        Room("MAGAZYN I CHEMIA",28.5f,32,13f,8f,3.8f,white);
        Room("POM. TECHNICZNE HVAC",28.5f,22,13f,8f,3.8f,white);
        // Changing facilities and pool-side rescue, as in the provided ground plan.
        Room("SZATNIE K", -27,12.0f,15,9,3.5f,white);
        Room("SZATNIE RODZINNE",-12.7f,12f,12,9,3.5f,white);
        Room("SZATNIE M",13.0f,12f,10,9,3.5f,white);
        // Physically independent wet room: real source shower cabins belong
        // here, not in the family locker room (keep a 2.5m clear entry aisle).
        Room("PRYSZNICE PREMIUM",0f,12f,10.8f,8f,3.5f,white);
        Room("RATOWNICY",21f,12f,6,9,3.5f,white);
        // Stainless lane handrails along pool deck; the big basin stays empty.
        for(float z=19f;z<=35f;z+=5.4f)
            Box(details,"Lane change equipment - not submerged",steel,-20.6f,.9f,z,.07f,1.2f,.07f,false);
    }
    static void RectGlazing(float x,float z,float width,float height,bool alongX,float baseY=0f,float doorHalfWidth=0f)
    {
        if(alongX)
        {
            for(float left=-width*.5f;left<width*.5f-.01f;left+=2.8f)
            {
                float span=Mathf.Min(2.8f,width*.5f-left);
                if(doorHalfWidth>0f&&Mathf.Abs(left+span*.5f)<doorHalfWidth)continue;
                Box(details,"P1 bright transparent glass",glass,x+left+span*.5f,baseY+height*.5f,z,
                    span-.10f,height,.09f,false);
                Box(details,"P1 champagne anodized aluminium frame",frame,x+left,baseY+height*.5f,z,.12f,height,.15f,false);
            }
        }
        else
        {
            for(float a=-width*.5f;a<width*.5f-.01f;a+=2.8f)
            {
                float span=Mathf.Min(2.8f,width*.5f-a);
                if(doorHalfWidth>0f&&Mathf.Abs(a+span*.5f)<doorHalfWidth)continue;
                Box(details,"P1 clear facade side",glass,x,baseY+height*.5f,z+a+span*.5f,
                    .09f,height,span-.10f,false);
                Box(details,"P1 slender side frame",frame,x,baseY+height*.5f,z+a,.15f,height,.13f,false);
            }
        }
    }
    static void RecreationWing()
    {
        // Western 28x22 family hall, physically connected to main pool lobby.
        DeckWithBasins("RECREATION 28x22",-69,-41,12,34);
        for(float x=-69;x<=-41;x+=3.5f)
            Box(structure,"P1 recreation roof soffit",white,x+1.7f,8.55f,23,3.6f,.25f,22f,true);
        RectGlazing(-55,12,28,7.9f,true);RectGlazing(-55,34,28,7.9f,true);
        RectGlazing(-69,23,22,7.9f,false);RectGlazing(-41,23,22,7.9f,false,0f,3.3f);
        for(float x=-69;x<=-41;x+=7)
            Box(structure,"P1 family hall white structural column",white,x,4.3f,12,.45f,8.3f,.45f);
        Box(details,"P1 family area premium signage band",wood,-55,7.2f,11.8f,26,.70f,.28f,false);
        Sign("STREFA RODZINNA",new Vector3(-55,6.95f,11.55f),180,.25f);
    }
    static void RectWall(Transform parent,string name,Material m,
        float x,float z,float width,float height,bool horizontal)
    {
        if(horizontal)Box(parent,name,m,x,height*.5f,z,width,height,.18f);
        else Box(parent,name,m,x,height*.5f,z,.18f,height,width);
    }
    static void SpaAndReception()
    {
        // Street-facing 36x24m SPA/reception wing, front is south (z=-28).
        DeckWithBasins("SPA / RECEPTION 36x24",-18,18,-28,-4);
        Box(structure,"P1 spa flat white roof",roofFinish,0,8.35f,-16,36.4f,.30f,24.4f);
        RectGlazing(0,-28,25f,5.7f,true,0f,3.5f);
        RectWall(structure,"P1 front wood facade left",wood,-15.2f,-28,5.6f,8.2f,true);
        RectWall(structure,"P1 front wood facade right",wood,15.2f,-28,5.6f,8.2f,true);
        RectGlazing(-18,-16,24,6.4f,false);RectGlazing(18,-16,24,6.4f,false);
        // Open the north internal link also for the real massage-suite
        // corridor at X=-8.85; opaque glass cannot seal the clinic entrance.
        RectGlazing(0,-4,24f,6.4f,true,0f,10f);
        // Lobby, reception desk and independent tiled rooms on ground floor.
        Room("KAWIARNIA",-12,-12,10.5f,12,3.4f,white);
        Room("BIURO / OBSLUGA",-12,-23,10.5f,7,3.4f,white,+1);
        Room("SAUNA",12.4f,-22.5f,8.9f,8f,3.4f,white,-1);
        // Guest SPA entrance faces the spacious CENTRAL corridor, not the
        // narrow 0.8m strip between SAUNA north wall and jacuzzi room.
        Room("SPA RELAKS",12f,-13.8f,10.8f,7.8f,3.4f,white,-1);
        // The outdoor-facing main public entrance remains a genuine open aperture.
        Box(details,"P1 two-sided covered entrance awning",wood,0,5.0f,-30.5f,19f,.28f,4.7f,false);
        Box(details,"P1 entry canopy fascia gold",gold,0,4.9f,-33,19f,.11f,.12f,false);
        foreach(float x in new[]{-8.7f,8.7f})
            Box(details,"P1 entrance canopy column",frame,x,2.35f,-32.6f,.24f,4.7f,.24f);
        // Original artwork from the owner’s CitySigns release is the only approved
        // exterior identity. No competing TextMesh lettering above the entry.
        // If the verified BASEN PNG is not imported yet, the sign stays absent
        // (with a named source socket) rather than showing a made-up substitute.
        BraxChampionsPoolP1OriginalSign.Generate(root);
    }
    static void ConnectWings()
    {
        // Two actual walkable, glass-sided internal links; not blocked by glass sheets.
        DeckWithBasins("SPORT <-> SPA OPEN PASSAGE",-7.75f,7.75f,-4f,6f);
        Box(structure,"P1 spa-sports link roof",roofFinish,0,6.5f,1f,15.7f,.24f,10f);
        // The west glass wall leaves a genuine doorway into two SPA massage rooms.
        RectGlazing(-7.75f,1f,10f,6.2f,false,0f,1.35f);
        RectGlazing(7.75f,1f,10f,6.2f,false);
        DeckWithBasins("SPORT <-> FAMILY LINK",-41f,-36f,20f,27.8f);
        Box(structure,"P1 family link roof",white,-38.5f,7.8f,23.9f,5f,.21f,7.8f);
        RectGlazing(-38.5f,20f,5,7.5f,true);
        RectGlazing(-38.5f,27.8f,5,7.5f,true);
        // Reception furniture with real PBR tile floors, visible from the street.
        Box(rooms,"Reception counter finish teak",wood,0,1.03f,-16.7f,7.5f,.95f,1.1f);
        Box(rooms,"Reception counter stone top",stone,0,1.56f,-16.7f,8f,.18f,1.35f);
        for(float x=-5;x<=5;x+=2.5f)
            Box(rooms,"Lobby ceiling pendant",led,x,3.8f,-19,.65f,.10f,.65f,false);
        Box(rooms,"Public waiting seating upholstered base",wood,0,.64f,-10.6f,4.8f,.48f,1.0f);
    }
    // Treatment suites have an independent functional entrance from the
    // main SPA<->sports glass link. The owner's street-facing glazed facade,
    // existing sauna, cafe, pools and entrance remain exactly where they are.
    // Two separate lockable rooms, not massage beds scattered in the lobby.
    static void SpaMassageRooms()
    {
        DeckWithBasins("SPA TWO GENUINE MASSAGE ROOMS",-18.1f,-7.7f,-4f,6f);
        Box(structure,"P1 massage suite calm white roof",roofFinish,-12.9f,4.05f,1f,10.55f,.25f,10.4f);
        Box(structure,"P1 massage suite west finished exterior wall",facade,-18.15f,2.0f,1f,.18f,4.0f,10.1f);
        // Leave a genuine 2.2m passage at X=-9.9..-7.7 from the lobby
        // to the massage-room corridor; previously this full wall sealed it.
        Box(structure,"P1 massage suite south insulated wall",white,-14.0f,2.0f,-4f,8.15f,4.0f,.18f);
        // Two 8m×5m treatment rooms, each entered from a 2m side corridor.
        Room("GABINET MASAZU 1",-14.0f,-1.5f,8.0f,4.8f,3.45f,white,+1);
        Room("GABINET MASAZU 2",-14.0f,3.45f,8.0f,4.8f,3.45f,white,+1);
        Box(rooms,"P1 massage suite clean central corridor PBR",floor,-8.85f,Deck+.013f,1f,2.05f,.026f,9.7f,false);
        Box(details,"P1 massage corridor header with no blocked opening",steel,-7.78f,3.95f,1f,.12f,.13f,2.75f,false);
        for(float z=-1.5f;z<=3.5f;z+=5f)
            Box(details,"P1 massage room warm clinical ceiling LED",led,-14.0f,3.0f,z,3.8f,.06f,.38f,false);
    }

    // Room has four real walls, an OPEN door passage and a ceramic floor. No fake closed panel.
    static void Room(string name,float x,float z,float w,float d,float h,Material wall,int corridorSide=0)
    {
        var t=Group(rooms,name+" | FINISHED room / OPEN ENTRY");
        float a=x-w*.5f,b=x+w*.5f,n=z-d*.5f,s=z+d*.5f;
        Box(t,"P1 room ceramic floor",floor,x,Deck+.014f,z,w,.028f,d);
        float gap=Mathf.Min(2f,w*.35f);
        Box(t,"P1 north wall 2K",wall,x,h*.5f,s,w,h,.18f);
        if(corridorSide==0)
        {
            // Accessible south-facing changing room / public room entrance.
            Box(t,"P1 west wall white real PBR",wall,a,h*.5f,z,.18f,h,d);
            Box(t,"P1 east wall white real PBR",wall,b,h*.5f,z,.18f,h,d);
            Box(t,"P1 south wall left of door",wall,(a+x-gap*.5f)*.5f,h*.5f,n,
                x-gap*.5f-a,h,.18f);
            Box(t,"P1 south wall right of door",wall,(x+gap*.5f+b)*.5f,h*.5f,n,
                b-x-gap*.5f,h,.18f);
            Box(t,"P1 door header over OPEN entrance",wall,x,h-.20f,n,gap,.40f,.18f);
            Box(t,"P1 premium brushed door upright L",steel,x-gap*.5f,1.12f,n-.08f,.08f,2.25f,.09f,false);
            Box(t,"P1 premium brushed door upright R",steel,x+gap*.5f,1.12f,n-.08f,.08f,2.25f,.09f,false);
            Sign(name,new Vector3(x,h-.63f,n-.17f),180,.10f);
        }
        else
        {
            // Office and sauna are entered from the CENTRAL lobby corridor, not from
            // outside through the front curtain wall. All four sides still have true walls.
            Box(t,"P1 south enclosed wall",wall,x,h*.5f,n,w,h,.18f);
            float doorX=corridorSide>0?b:a;
            float closedX=corridorSide>0?a:b;
            Box(t,"P1 other long room wall",wall,closedX,h*.5f,z,.18f,h,d);
            float passage=Mathf.Min(1.9f,d*.4f);
            Box(t,"P1 corridor wall before door",wall,doorX,h*.5f,(n+z-passage*.5f)*.5f,
                .18f,h,z-passage*.5f-n);
            Box(t,"P1 corridor wall after door",wall,doorX,h*.5f,(z+passage*.5f+s)*.5f,
                .18f,h,s-z-passage*.5f);
            Box(t,"P1 corridor side door header",wall,doorX,h-.22f,z,.18f,.44f,passage);
            foreach(float zz in new[]{z-passage*.5f,z+passage*.5f})
                Box(t,"P1 brushed frame, lateral public door",steel,doorX,1.15f,zz,.10f,2.3f,.09f,false);
            Sign(name,new Vector3(doorX+(corridorSide>0?.15f:-.15f),h-.65f,z),
                corridorSide>0?90:270,.095f);
        }
        Box(t,"P1 inside washed wall skirting",smallMosaic,x,.18f,s-.12f,w,.27f,.07f,false);
        Box(t,"P1 clean interior ceiling",roofFinish,x,h+.15f,z,w,.24f,d);
    }
    static void BuildBlueBasin(Basin b)
    {
        float x=b.x-LotX,z=b.z-LotZ,w=b.w,d=b.d,depth=b.depth;
        var t=Group(structure,"REAL BLUE TILED EXCAVATED BASIN WITH ANIMATED WATER | "+b.id);
        var m=b.id=="CHILDREN"?lightBlue:blue;
        float bottom=Deck-depth;
        // The underlying -0.25m foundation terrain is carved by legacy_foundation.py
        // using EXACTLY these same plot.json rectangles before this hollow basin is built.
        Box(t,b.id+" turquoise PBR bottom (NOT water)",m,x,bottom-.08f,z,w+.25f,.16f,d+.25f);
        Box(t,b.id+" 2K blue inner long NORTH wall",m,x,Deck-depth*.5f,z+d*.5f,w,depth,.16f);
        Box(t,b.id+" 2K blue inner long SOUTH wall",m,x,Deck-depth*.5f,z-d*.5f,w,depth,.16f);
        Box(t,b.id+" 2K blue inner WEST wall",m,x-w*.5f,Deck-depth*.5f,z,.16f,depth,d);
        Box(t,b.id+" 2K blue inner EAST wall",m,x+w*.5f,Deck-depth*.5f,z,.16f,depth,d);
        // Wide white anti-slip coping and grilles, made of PBR ceramic.
        for(int side=-1;side<=1;side+=2)
        {
            Box(t,b.id+" real pool coping long",floor,x,Deck+.025f,z+side*(d*.5f+.23f),
                w+.88f,.05f,.46f);
            Box(t,b.id+" real pool coping short",floor,x+side*(w*.5f+.23f),Deck+.025f,z,
                .46f,.05f,d+.88f);
            Box(t,b.id+" drain grating long",steel,x,Deck+.051f,z+side*(d*.5f+.45f),w,.018f,.11f,false);
        }
        // 25m competition pool; five lane separation markings on the actual EMPTY bottom.
        if(b.id=="SPORT_25M")
        {
            for(int lane=1;lane<6;lane++)
            {
                float zz=z-d*.5f+lane*d/6f;
                Box(t,"P1 painted pool floor 25m lane line "+lane,navy,x,bottom+.013f,zz,w-.9f,.013f,.07f,false);
                foreach(float xx in new[]{x-w*.5f+2.2f,x+w*.5f-2.2f})
                    Box(t,"P1 competition end T",navy,xx,bottom+.016f,zz,.08f,.013f,1.3f,false);
            }
        }
        // Swimming water plane is not a solid collider. The real bottom and sidewalls
        // remain collidable; a separate trigger provides buoyancy for deep basins.
        BraxChampionsPoolP1Water.Basin(t,b.id,x,z,w,d,depth,b.id!="CHILDREN");
        // Two real owner steel ladder models mounted separately,
        // with source mesh collision, no procedural duplicates.

    }
    static void FixturesAndFurniture()
    {
        // Wet rooms and changing facilities: finished surfaces, proper fixtures.
        // Lockers are OWNER-SUPPLIED original 3D models, not wood-box stand-ins.
        // Their real mounting positions are reserved in OwnerEquipmentAnchors().
        for(float x=-30;x<=-19;x+=3.5f)
            Box(rooms,"Real teak changing bench",wood,x,.62f,9.2f,2.8f,.38f,.55f);
        // Tiled shower stations with actual pipe, valves and drain.
        for(int i=0;i<5;i++)
        {
            float x=-15f+i*2.2f;
            Cylinder(rooms,"Shower chrome riser",steel,x,1.1f,9.8f,.045f,2.0f);
            Cylinder(rooms,"Shower circular head",steel,x,2.22f,9.8f,.22f,.08f);
            Box(rooms,"Shower wet-floor drain",stone,x,Deck+.029f,10.8f,.34f,.02f,.26f,false);
        }
        // Café: counter, teak tables, stools, wall-facing wood panels.
        Box(rooms,"P1 cafeteria timber service counter",wood,-12,.96f,-11.2f,7.2f,.9f,1.0f);
        Box(rooms,"P1 cafe polished tile counter top",smallMosaic,-12,1.47f,-11.2f,7.4f,.15f,1.1f);
        for(float x=-15;x<=-9;x+=3f)
            for(float z=-16;z<=-14;z+=2)
            {
                Cylinder(rooms,"P1 cafe table leg",steel,x,.79f,z,.045f,1.05f);
                Cylinder(rooms,"P1 cafe tabletop granite",stone,x,1.3f,z,.68f,.10f);
            }
        // The owner will provide actual sauna and loungers.
        // Finished floor, walls and ceiling remain; no substitute furniture.
        // Accessible toilet cubicles beside family changing.
        for(int i=0;i<3;i++)
        {
            float x=-17f+i*2.2f;
            Cylinder(rooms,"Ceramic WC bowl shaping (replace with approved model if supplied)",floor,x,.62f,14.4f,.42f,.65f);
            Box(rooms,"White PBR WC cistern",white,x,1.0f,15.0f,.74f,.68f,.28f);
            Cylinder(rooms,"White PBR washbasin",floor,x,1.03f,10.6f,.38f,.15f);
            Cylinder(rooms,"Chrome basin mixer",steel,x,1.17f,10.9f,.045f,.26f);
        }
        // The supplied real lifeguard high-chair is installed once near
        // the water by BraxChampionsPoolP1Equipment.
        // SPA staff supplies shelving, filtration equipment boxes, real doors kept passable.
        for(float z=14;z<=45;z+=12)
        {
            Box(rooms,"Technician control console",steel,31,1.0f,z,4.2f,1.35f,1.25f);
            Box(rooms,"Technician inspection board",floor,34,2.0f,z,.15f,1.8f,4.0f);
        }
    }
    static void ArcPaint(Transform t,float x,float y,float z,float radius,Material material)
    {
        int n=28;
        for(int i=0;i<n;i++)
        {
            float a=(i+.5f)*2*Mathf.PI/n;
            // Two real curb-free parking entries at EAST and WEST.
            if(Mathf.Abs(Mathf.Cos(a))>.92f)continue;
            var o=Box(t,"Roundabout curb stone segment",material,x+radius*Mathf.Cos(a),y,z+radius*Mathf.Sin(a),
                .25f,.17f,1.4f,false);
            o.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
        }
    }
    static void EstateAndParking()
    {
        // Forecourt parking: 95 physically marked stalls with aisle space,
        // separate 6 TAXI bays and street access. Not parking painted on grass.
        Box(parking,"P1 WEST asphalt parking surface 2K",asphalt,-44,.195f,-44,68,.13f,28);
        Box(parking,"P1 EAST asphalt parking surface 2K",asphalt,44,.195f,-44,68,.13f,28);
        Box(parking,"P1 SOUTH / MAIN DRIVE from existing Z70 street",asphalt,0,.195f,-78,12,.13f,70);
        // Actual connected asphalt spurs: the 4m gaps between the south
        // arrival road and the two parking banks previously made both lots
        // physically inaccessible to a vehicle.
        Box(parking,"P1 WEST parking access connected to roundabout",asphalt,-8f,.195f,-43f,4.2f,.13f,8f);
        Box(parking,"P1 EAST parking access connected to roundabout",asphalt,8f,.195f,-43f,4.2f,.13f,8f);
        Box(outdoors,"P1 front entrance pedestrian piazza PBR HERRINGBONE",patternPaving,0,.28f,-29f,40f,.15f,9f);
        Box(outdoors,"P1 main pedestrian leading path HERRINGBONE",patternPaving,0,.28f,-35.5f,8f,.13f,8f);
        // 4 rows of cars divided in two banks; one reserved bay stays clear -> 95.
        for(int bank=0;bank<2;bank++)
            for(int row=0;row<2;row++)
                for(int i=0;i<24;i++)
                {
                    if(bank==1&&row==1&&i==23)continue;
                    float x=(bank==0?-73f:11f)+(i+.5f)*2.55f;
                    // Keep 1.7m separation from the pedestrian piazza;
                    // maintain an 8.4m parking aisle between front/rear rows.
                    float z=row==0?-52f:-38f;
                    float front=row==0?z+2.85f:z-2.85f;
                    Box(parking,"P1 marked stall "+(parked+1)+" left line",paint,x-1.22f,.278f,z,.075f,.015f,5.6f,false);
                    Box(parking,"P1 marked stall "+(parked+1)+" head line",paint,x,.280f,front,2.45f,.014f,.075f,false);
                    Box(parking,"P1 marked stall "+(parked+1)+" right line",paint,x+1.22f,.278f,z,.075f,.015f,5.6f,false);
                    if(parked==0||parked==48)
                        Box(parking,"P1 accessible parking blue pavement marker",azure,x,.281f,z,1.5f,.017f,1.6f,false);
                    parked++;
                }
        // Drive aisle and white pedestrian crossings.
        for(float x=-72;x<=72;x+=9)
            Box(parking,"P1 parking shared aisle markings",paint,x,.283f,-43.0f,2.8f,.014f,.10f,false);
        for(float z=-62;z<=-36;z+=3.1f)
            Box(parking,"P1 arrival zebra crossing",paint,0,.288f,z,7.1f,.015f,1.5f,false);
        // Separate taxi and delivery. No collision with facade or 95 parking spaces.
        Box(outdoors,"P1 taxi real granite access pavement",paving,39,.28f,-25,38,.16f,10);
        for(int i=0;i<6;i++)
        {
            float x=23+i*6f;
            Box(outdoors,"P1 TAXI 6 places side border",paint,x-.02f,.375f,-25,.10f,.014f,7.2f,false);
            Box(outdoors,"P1 TAXI space rear marking",paint,x+2.8f,.376f,-21.4f,5.8f,.014f,.11f,false);
        }
        Sign("TAXI",new Vector3(42,.95f,-30.2f),180,.35f);
        Sign("PARKING 95",new Vector3(-43,1.15f,-60),180,.31f);
        ArcPaint(outdoors,0,.32f,-42.7f,6.3f,paving);
        Cylinder(outdoors,"P1 circular landscaped entry island PBR",stone,0,.49f,-42.7f,3.6f,.22f);
        Cylinder(outdoors,"P1 circular planted centre",plants,0,.63f,-42.7f,3.25f,.08f);
        // East café terrace, distinct from the public drop-off.
        Box(outdoors,"P1 east recreation outdoor herringbone terrace",patternPaving,58f,.28f,21f,40f,.16f,44f);
        for(int i=0;i<8;i++)
        {
            float x=44+(i%4)*8f, z=8+(i/4)*20f;
            Cylinder(outdoors,"P1 east terrace table leg brushed stainless",steel,x,.85f,z,.07f,.95f);
            Cylinder(outdoors,"P1 east terrace stone tabletop",stone,x,1.36f,z,.85f,.15f);
            foreach(float s in new[]{-1f,1f})
                Box(outdoors,"P1 east terrace teak bench",wood,x+s*1.8f,.57f,z,1.5f,.22f,.66f);
        }
        // Tree/planting buffer and service walk behind building, original approved world layout preserved.
        // Stop the original rear path before the slide's true water landing;
        // otherwise paving would cut through its basin (Z-fighting).
        Box(outdoors,"P1 rear service path with paving",paving,6.5f,.28f,54.0f,97f,.14f,5.3f);
        for(float x=-73;x<=73;x+=17.5f)
        {
            // Preserve the clear original slide landing basin at x≈-54.
            if(Mathf.Abs(x)<8 || (x>=-64f&&x<=-41f))continue;
            Box(outdoors,"P1 rear planted bed concrete coping",stone,x,.38f,57f,8.8f,.26f,2.5f);
            Box(outdoors,"P1 rear planted bed top",plants,x,.56f,57f,8.0f,.09f,2.0f);
        }
        // Lighting and stone bollards along public access path, no new fake palm meshes.
        for(float z=-60;z<=-30;z+=10)
            foreach(float x in new[]{-9f,9f})
            {
                Cylinder(outdoors,"P1 pedestrian bollard stone base",stone,x,.28f,z,.17f,.29f);
                Cylinder(outdoors,"P1 light bollard brushed steel",steel,x,.86f,z,.095f,.92f);
                Cylinder(outdoors,"P1 warm bollard diffused LED cap",led,x,1.37f,z,.12f,.10f);
            }
        for(float x=-75;x<=75;x+=15)
            if(Mathf.Abs(x)>19)
                Box(outdoors,"P1 boundary decorative kerb",stone,x,.24f,-59.5f,12f,.27f,.42f);
    }
    static void Sign(string label,Vector3 position,float yaw,float size)
    {
        // Text is an actual 3D scene object, not a screenshot of a text label.
        var go=new GameObject("P1 SIGN | "+label);
        go.transform.SetParent(details,false);
        go.transform.localPosition=position*K;
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var tm=go.AddComponent<TextMesh>();
        tm.text=label;tm.fontSize=64;tm.characterSize=size*K;tm.anchor=TextAnchor.MiddleCenter;
        tm.alignment=TextAlignment.Center;tm.color=new Color(.94f,.82f,.61f);
        var r=go.GetComponent<MeshRenderer>();r.shadowCastingMode=ShadowCastingMode.Off;
    }
    static void NightAndInteriorLights()
    {
        // Real bounded illumination supplements daylight through true clear glass.
        foreach(var v in new[]{new Vector3(-23,9,19),new Vector3(0,9,19),
            new Vector3(19,9,30),new Vector3(-17,9,41),new Vector3(8,9,43),
            new Vector3(-55,5.4f,24),new Vector3(0,5.3f,-14)})
        {
            var o=new GameObject("P1 ceiling downlight warm LED");o.transform.SetParent(details,false);
            o.transform.localPosition=v*K;
            var light=o.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.95f,.88f);
            light.intensity=.95f;light.range=19*K;light.shadows=LightShadows.None;
            Cylinder(details,"P1 visible downlight diffuser",led,v.x,v.y+.05f,v.z,.16f,.07f);
        }
    }
}
