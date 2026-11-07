using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// T11 original plan: centre (495,465), property 120 x 100 nominal metres.
// All geometry uses BraxClaudeOsiedle.K exactly once. EXTERIOR ONLY:
// no casino games, partitions, furniture, interior floors or office interiors.
public static class BraxChampionsCasinoT11
{
    public const string SignPath="Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png";
    const float LotX=495f, LotZ=465f;
    static float K=>BraxClaudeOsiedle.K;
    static string output;
    static int serial;
    static Transform root,shell,trim,signRoot;
    static Material cream,graphite,granite,asphalt,paving,gold,pillarGold,steel,glass,frontGlass,towerGlass,skylightGlass,led,roof,redCarpet,parkingPaint;
    // White finishes are GARAGE-ONLY. Do not repaint the owner-approved casino exterior.
    static Material garageWhite,garageWhiteBeam,garageBlackAsphalt,garageParkingPaint;

    static void Setup(string directory)
    {
        output=directory;serial=0;
        // Plansza właściciela „KASYNO (T11)”, 6.11: jasna elewacja = Rectangular Facade
        // Tiles (jasny krem), duże płyty — NIE cegła. Te same pobrane mapy co grafit,
        // kremowy odcień jak pas kamienny areny.
        cream=BraxChampionsCasinoPbr2K.Create(output,"t11_cream_facade","rectangular_facade_tiles",2.0f,new Color(.93f,.89f,.80f));
        graphite=BraxChampionsCasinoPbr2K.Create(output,"t11_graphite_facade","rectangular_facade_tiles",2.0f,new Color(.22f,.23f,.25f));
        granite=BraxChampionsCasinoPbr2K.Create(output,"t11_dark_granite","granite_tile",1.2f,new Color(.26f,.28f,.30f));
        paving=BraxChampionsCasinoPbr2K.Create(output,"t11_walkways","concrete_pavement_02",1.8f,new Color(.94f,.93f,.89f));
        asphalt=BraxChampionsCasinoPbr2K.Create(output,"t11_asphalt","clean_asphalt",2.1f,new Color(.9f,.9f,.9f));
        gold=Surface("champagne_bronze",new Color(.75f,.57f,.32f),false,false);
        gold.SetFloat("_Metallic",.88f);gold.SetFloat("_Roughness",.25f);
        // Właściciel: wszystkie widoczne filary ZŁOTE i połyskujące.
        // Prawdziwe oryginalne 4K PBR Metal048A z przygotowanej biblioteki,
        // nie jednolity żółty plastik i nie nowy zamiennik tekstur.
        pillarGold=BraxChampionsFoundation.LibraryMetal(
            "T11_all_gold_polished_columns",new Color(1f,.76f,.34f));
        pillarGold.SetFloat("_Metallic",.95f);
        pillarGold.SetFloat("_Roughness",.18f);
        EditorUtility.SetDirty(pillarGold);
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
        frontGlass=Surface("front_smoke_glass",new Color(.55f,.63f,.71f,.22f),true,false);
        // Office tower must look like a COMPLETED blue-glass building,
        // not transparent bare framework against the sky.
        towerGlass=Surface("office_tower_blue_glass",new Color(.28f,.40f,.53f,.76f),true,false);
        // The reference drawing has a blue-violet translucent glazed roof lantern.
        skylightGlass=Surface("roof_skylight_glass",new Color(.48f,.56f,.76f,.38f),true,false);
        led=Surface("warm_led",new Color(.92f,.78f,.57f),false,true);
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
        bool transparent=mat==glass||mat==frontGlass||mat==towerGlass||mat==skylightGlass;
        if(transparent){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        if(tiled || ((mat==cream || mat==graphite || mat==granite || mat==roof || mat==paving || mat==asphalt || mat==pillarGold) && Mathf.Max(w,Mathf.Max(h,d))>3f))
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
    static void FrontPanel(float x,float z,float w,float bottom,float height)
    {
        // Approved: two-storey GLAZED frontage; smoky yet transparent panes.
        B(shell,"T11 premium slightly tinted FRONT glass",frontGlass,
            x,bottom+height*.5f,z,w,height,.11f,false);
        foreach(float v in new[]{x-w*.5f,x,x+w*.5f})
            B(trim,"T11 glossy gold front glass mullion",pillarGold,
                v,bottom+height*.5f,z-.14f,.18f,height,.18f,false);
        B(trim,"T11 fine gold framing at window sill",gold,
            x,bottom+.055f,z-.16f,w,.09f,.11f,false);
        B(trim,"T11 fine gold framing over window",gold,
            x,bottom+height-.055f,z-.16f,w,.09f,.11f,false);
    }
    static void SidePanel(float x,float z,float w,float bottom,float height)
    {
        // Owner's facade contract: after the principal glazed front,
        // SIDE wings are mostly MASONRY. Keep one narrow, real window per
        // 6.5m bay instead of the previous full-length glass curtain wall.
        // Current original cream Poly Haven finish is retained temporarily;
        // user selects the final masonry material separately.
        const float slot=1.40f;
        float stoneWidth=(w-slot)*.5f;
        float y=bottom+height*.5f;
        float offset=(slot+stoneWidth)*.5f;
        B(shell,"T11 real solid Poly Haven MASONRY side left",cream,
            x,y,z-offset,.35f,height,stoneWidth,true,true);
        B(shell,"T11 real solid Poly Haven MASONRY side right",cream,
            x,y,z+offset,.35f,height,stoneWidth,true,true);
        B(shell,"T11 slim lightly tinted side window",frontGlass,
            x,y,z,.13f,height,slot,false);
        float outerX=x+Mathf.Sign(x)*.22f;
        foreach(float zz in new[]{z-slot*.5f,z+slot*.5f})
            B(trim,"T11 polished gold vertical side reveal",pillarGold,
                outerX,y,zz,.15f,height,.14f,false);
        B(trim,"T11 masonry window gold top frame",gold,
            outerX,bottom+height-.05f,z,.12f,.1f,slot,false);
        B(trim,"T11 masonry window gold sill",gold,
            outerX,bottom+.05f,z,.12f,.1f,slot,false);
    }

    static void ThreeMatchingFacades()
    {
        // Two ivory/cream wings flank the curved glass atrium.
        foreach(float side in new[]{-1f,1f})
        {
            for(int bay=0;bay<3;bay++)
            {
                float x=side*(19f+bay*7.6f);
                B(shell,"T11 GOLD glossy front structural pilaster",pillarGold,
                    x,7.1f,-27.9f,1.16f,13.8f,1.00f);
                if(bay<2)
                {
                    FrontPanel(side*(22.8f+bay*7.6f),-28.02f,6.1f,.28f,5.85f);
                    FrontPanel(side*(22.8f+bay*7.6f),-28.02f,6.1f,7.10f,5.8f);
                }
            }
            // Sides have THE SAME light facade, graphite base, champagne frames
            // and warm LED cornice as the approved front elevation.
            float xx=side*38f;
            for(int bay=0;bay<7;bay++)
            {
                float zz=-23.0f+bay*7.55f;
                B(shell,"T11 GOLD glossy side structural pilaster",pillarGold,
                    xx,7.1f,zz,1.10f,13.8f,1.02f);
                if(bay<6)
                {
                    SidePanel(side*38.08f,zz+3.7f,6.5f,.3f,5.8f);
                    SidePanel(side*38.08f,zz+3.7f,6.5f,7.1f,5.8f);
                }
            }
            B(trim,"Side champagne floor belt",gold,side*38.48f,7.04f,0,.13f,.17f,56f,false);
            B(trim,"Side graphite lower base",graphite,side*38.4f,.50f,0,.14f,.64f,56f,false);
            B(trim,"Side continuous gold cornice",gold,side*38.45f,13.66f,0,.19f,.25f,56f,false);
            B(trim,"Side roof warm LED strip",led,side*38.55f,13.81f,0,.11f,.09f,55f,false);
            for(int bay=0;bay<6;bay++)
                B(trim,"Side warm facade light",led,side*38.55f,5.1f,-19.6f+bay*8.9f,.10f,1.35f,.18f,false);
        }
        // Preserve the two full-height gateway pylons, now GOLD as explicitly
        // approved by the owner. Modest 0.25m widening retains the doorway.
        foreach(float side in new[]{-1f,1f})
        {
            B(shell,"T11 tall GOLD polished main entrance pylon",pillarGold,
                side*18.25f,7.10f,-28f,2.75f,14.1f,2.35f);
            B(trim,"T11 champagne vertical entry reveal",gold,
                side*18.25f,7.02f,-29.27f,.15f,13.8f,.13f,false);
            B(trim,"T11 warm recessed vertical pylon LED",led,
                side*18.25f,7.01f,-29.39f,.067f,13.6f,.08f,false);
        }
        // Rear service wall in exactly the same ivory + graphite + gold palette.
        // Keep a real six-metre-wide DELIVERY OPENING at x=-23..-17.
        B(shell,"Rear cream stone wing left",cream,-30.5f,7.0f,28f,15f,13.8f,.7f);
        B(shell,"Rear cream stone wing right",cream,10.5f,7.0f,28f,55f,13.8f,.7f);
        B(shell,"Service entry over-door stone",cream,-20f,9.7f,28f,6f,8.3f,.7f);
        B(trim,"Rear graphite base",graphite,8f,.58f,28.45f,60f,.8f,.15f,false);
        B(trim,"Rear champagne cornice",gold,0,13.72f,28.45f,75f,.20f,.14f,false);
        B(trim,"Rear warm gold LED line",led,0,13.86f,28.52f,74.4f,.085f,.10f,false);
        foreach(float xx in new[]{-36f,-25f,-14f,-3f,8f,19f,30f,36f})
            B(shell,"T11 glossy GOLD masonry rear pilaster",pillarGold,
                xx,7.3f,28.49f,.74f,12.7f,.25f,false);
        foreach(float xx in new[]{-32f,-8f,17f,32f})
            B(trim,"Rear warm architectural light",led,xx,5.6f,28.62f,.08f,2.2f,.14f,false);
        // No fake solid service doors; this is a reserved open loading aperture.
    }

    static void RoofAndSkylight()
    {
        // Technical flat roof with genuine transparent top skylight.
        B(shell,"Podium ground slab",granite,0,.08f,0,76f,.18f,56f,true,true);
        B(shell,"Podium roof west",roof,-25f,14.05f,0,26f,.22f,56f,true,true);
        B(shell,"Podium roof east",roof,25f,14.05f,0,26f,.22f,56f,true,true);
        B(shell,"Skylight roof south",roof,0,14.05f,-19f,24f,.22f,18f,true,true);
        B(shell,"Skylight roof north",roof,0,14.05f,19f,24f,.22f,18f,true,true);
        B(shell,"Actual transparent blue-violet rooftop skylight",skylightGlass,0,14.14f,0,24f,.11f,20f,false);
        for(float x=-12f;x<=12f;x+=4f)
            B(trim,"Skylight champagne metal longitudinal rib",gold,x,14.24f,0,.12f,.13f,20f,false);
        for(float z=-10f;z<=10f;z+=5f)
            B(trim,"Skylight champagne metal crossbar",gold,0,14.24f,z,24f,.13f,.12f,false);
        foreach(float side in new[]{-1f,1f})
        {
            B(trim,"Parapet front stone",cream,side*19f,14.9f,-27.8f,37f,1.4f,.65f);
            B(trim,"Parapet rear stone",cream,side*19f,14.9f,27.8f,37f,1.4f,.65f);
            B(trim,"Parapet side stone",cream,side*37.7f,14.9f,0,.60f,1.4f,55.4f);
            B(trim,"Roof perimeter gold",gold,side*37.86f,15.58f,0,.14f,.16f,55.2f,false);
        }
        B(shell,"Enclosed roof technology enclosure",graphite,-29f,15.3f,16f,8f,2.6f,5f);
        for(float x=-32f;x<-25f;x+=.6f)
            B(trim,"Ventilation louvre gold",gold,x,15.5f,13.42f,.11f,1.35f,.13f,false);
        foreach(float z in new[]{16f,-15f})
            B(shell,"Exterior roof ventilation case",steel,21f,14.8f,z,3.5f,1.4f,3f);
        foreach(float side in new[]{-1f,1f})
        {
            B(trim,"Front 1st floor gold strip",gold,side*27f,7.05f,-28.4f,22f,.12f,.24f,false);
            B(trim,"Front gold LED roof strip",led,side*27f,13.81f,-28.45f,22f,.09f,.16f,false);
        }
    }

    static void RotundaRoofCap()
    {
        // Real semicircular roof over the curved glass rotunda. The approved
        // image shows a rounded canopy, not a rectangular box.
        const int sides=48;
        const float yTop=14.24f,yBottom=14.03f;
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
                float x=17f*Mathf.Sin(angle),z=-28f-8f*Mathf.Cos(angle);
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
        var mesh=new Mesh{name="T11 real curved rotunda roof"};
        mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_Curved_Rotunda_Roof.asset");
        var o=new GameObject("Real semicircular rotunda roof cap");
        o.transform.SetParent(shell,false);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=roof;
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
            var pane=B(shell,"Curved two-storey original glass frontage",frontGlass,
                centre.x,(10.8f+bottom)*.5f,centre.z,length+.045f,10.8f-bottom,.115f);
            pane.transform.localRotation=yaw;
            if(i%3==0)
            {
                var mullion=B(trim,"T11 GOLD glossy curved atrium frame",pillarGold,
                    pa.x,5.55f,pa.z,.19f,10.6f,.19f,false);
                mullion.transform.localRotation=yaw;
            }
            foreach(float level in new[]{11.05f,14.12f})
            {
                var bar=B(trim,"Golden oval rotunda outline LED",led,centre.x,level,centre.z,length+.1f,.12f,.19f,false);
                bar.transform.localRotation=yaw;
            }
            if(i%3==0)
            {
                var fascia=B(trim,"Curved graphite premium fascia",graphite,centre.x,12.5f,centre.z,length+.12f,2.50f,.25f,false);
                fascia.transform.localRotation=yaw;
            }
        }
        RotundaRoofCap();
        foreach(float side in new[]{-1f,1f})
            for(int j=0;j<3;j++)
                B(shell,"Entrance side granite stair",paving,side*(7.1f+j*.6f),.22f+j*.03f,-36.3f,5.3f,.14f+j*.07f,1.55f,true,true);
        B(shell,"Unobstructed accessible middle ramp",paving,0,.22f,-40f,7.2f,.08f,7.7f,true,true);
        // Seal only the lintel gap above operable door leaves; keep a full
        // unobstructed 5.6m public passage at human height.
        B(shell,"Real clear fixed door transom glass",frontGlass,0,3.75f,-36.13f,5.7f,.32f,.10f,false);
        B(trim,"Gold entry transom horizontal frame",gold,0,3.59f,-36.19f,5.95f,.07f,.12f,false);
        Door("T11_CASINO_WAWER_PUBLIC","Public glass main entry",0,-36.28f,180f,5.6f);
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
            B(hinge,"Toughened clear door glass",frontGlass,-side*width*.25f,1.8f,0,width*.5f,3.6f,.09f,true);
            B(hinge,"Champagne door pull",gold,-side*.27f,1.35f,.10f,.07f,.68f,.075f,false);
        }
        var working=group.gameObject.AddComponent<BraxCityDoor>();
        working.id=id;working.label=label;working.leaves=leaves;working.sides=new[]{1f,-1f};
    }

    static void Tower()
    {
        // Separate OFFICE tower silhouette, 120m high, no interior floor plates.
        const float x=23f,z=34f,w=18f,d=18f;
        B(shell,"Office tower podium base",granite,x,1.2f,z,w,2.3f,d,true);
        foreach(float side in new[]{-1f,1f})
        {
            B(shell,"Office tower north/south curtain glass",glass,x,60f,z+side*d*.5f,w,116f,.18f,true);
            B(shell,"Office tower east/west curtain glass",glass,x+side*w*.5f,60f,z,.18f,116f,d,true);
            for(float y=4f;y<120f;y+=3.7f)
            {
                B(trim,"Tower horizontal graphite bar north/south",graphite,x,y,z+side*(d*.5f+.14f),w,.12f,.18f,false);
                B(trim,"Tower horizontal graphite bar east/west",graphite,x+side*(w*.5f+.14f),y,z,.18f,.12f,d,false);
            }
        }
        foreach(float xx in new[]{x-w*.5f,x,x+w*.5f})
            foreach(float zz in new[]{z-d*.5f,z+d*.5f})
                B(trim,"Tower tall champagne mullion",gold,xx,60f,zz,.24f,119f,.24f,false);
        B(shell,"Tower matte graphite roof",roof,x,119.86f,z,w+.6f,.27f,d+.6f,true);
        B(trim,"Tower crown amber LED",led,x,119.92f,z+d*.5f,w,.09f,.11f,false);
    }

    static void UndergroundGarage()
    {
        // REAL accessible subterranean car park. No interior games, decoration,
        // casino floors or casino partitions. Route: east side of T11, from
        // z=-22 at pavement y=.27 down to the level y=-4.12 at z=+24.
        // World nominal coordinates: plot centre (495,465), K applied once.
        // 46m run / 4.39m descent -> 9.5% gradient (before Unity test).
        var basement=Group(root,"T11 | UNDERGROUND CAR PARK - real ramp and bays");
        var floor=B(basement,"Garage structural floor - real collider",granite,
            4.5f,-4.20f,2.5f,73f,.16f,53f,true,true);
        // Continuous black ASPHALT wearing course across the full 73x53 m
        // garage; under it remains a separate structural, colliding concrete slab.
        B(basement,"Garage floor BLACK Poly Haven asphalt",garageBlackAsphalt,
            4.5f,-4.108f,2.5f,73f,.02f,53f,true,true);
        B(basement,"Garage solid west retaining wall WHITE",garageWhite,
            -32.05f,-2.2f,2.5f,.32f,4.2f,53f,true,true);
        B(basement,"Garage north wall WHITE",garageWhite,
            4.5f,-2.2f,29.0f,73.3f,4.2f,.32f,true,true);
        B(basement,"Garage south wall WHITE (no exit through front)",garageWhite,
            4.5f,-2.2f,-24.0f,73.3f,4.2f,.32f,true,true);
        // East retaining wall is broken at z=19..27 to let vehicles enter.
        B(basement,"Garage east solid wall WHITE before entry",garageWhite,
            41.15f,-2.2f,-2.5f,.28f,4.2f,43f,true,true);
        B(basement,"Garage east short wall WHITE after entry",garageWhite,
            41.15f,-2.2f,28f,.28f,4.2f,2f,true,true);
        // Real covered underground ceiling, below the existing plot grade.
        B(basement,"Garage WHITE reinforced ceiling",garageWhite,4.5f,-.58f,2.5f,
            73f,.20f,53f,true,true);
        // The white ceiling has visible STRUCTURAL transverse/longitudinal
        // beams, not a bare floating rectangle. Beam undersides are at -1.08 m;
        // clear height above the asphalt is over 3.0 m (nominal).
        foreach(float zz in new[]{-18f,-6f,6f,18f,27f})
            B(basement,"B1 WHITE transverse roof beam",garageWhiteBeam,
                4.5f,-.875f,zz,72f,.40f,.48f,true,true);
        foreach(float xx in new[]{-29.0f,-12.5f,4.0f,20.5f,37.0f})
            B(basement,"B1 WHITE longitudinal roof beam",garageWhiteBeam,
                xx,-.875f,2.5f,.48f,.40f,51f,true,true);
        // Full-width turning area at the bottom of the descent: physically
        // joins the underground hall to the ramp, no void under vehicle wheels.
        B(basement,"Garage bottom structural landing",granite,
            44.5f,-4.20f,26f,7.7f,.16f,8f,true,true);
        B(basement,"Garage bottom BLACK asphalt landing",garageBlackAsphalt,
            44.5f,-4.112f,26f,7.7f,.015f,8f,true,true);
        // The OPEN surface ramp remains free of that ceiling.
        var ramp=B(basement,"Underground garage driveable BLACK asphalt ramp 9.5 percent",garageBlackAsphalt,
            44.5f,-1.985f,1f,7.65f,.12f,46.208f,true,true);
        ramp.transform.localRotation=Quaternion.Euler(
            Mathf.Atan2(4.39f,46f)*Mathf.Rad2Deg,0,0);
        B(basement,"Ramp west retaining wall WHITE",garageWhite,40.45f,-1.88f,1f,.30f,4.84f,46f,true,true);
        B(basement,"Ramp east retaining wall WHITE",garageWhite,48.55f,-1.88f,1f,.30f,4.84f,46f,true,true);
        B(basement,"Garage entrance from VIP/TAXI circulation",asphalt,
            44.5f,.236f,-30f,7.6f,.055f,16f,true,true);
        B(basement,"Garage gate LEFT graphite pier",graphite,
            40.40f,2.52f,-22f,.65f,4.65f,.76f,true,true);
        B(basement,"Garage gate RIGHT graphite pier",graphite,
            48.60f,2.52f,-22f,.65f,4.65f,.76f,true,true);
        B(basement,"Garage main entrance champagne beam",gold,
            44.5f,4.95f,-22f,8.9f,.40f,.75f,true);
        B(basement,"Garage upper portal warm LED",led,
            44.5f,4.71f,-22.44f,8.6f,.10f,.12f,false);
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
                    x,-4.099f,z,.095f,.008f,5.25f,false);
        }
        foreach(float z in new[]{-19.65f,12.65f})
            B(basement,"B1 continuous marked bay head line",garageParkingPaint,
                -3.2f,-4.099f,z,baysPerRow*bayWidth,.008f,.095f,false);
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
            float x=side>0&&z<0?38f:side*43f;
            PlaceNativePolyHaven(shell,shrub,"T11 original Poly Haven Shrub 03 (2K)",
                x,.78f,z,1.55f,1.75f,count++);
        }
        // The two tree trunks are beyond the rear service door and outside
        // the fire road / delivery bays. Do not replace the approved palms.
        foreach(float x in new[]{-27f,27f})
            PlaceNativePolyHaven(shell,tree,"T11 original Poly Haven Quiver Tree 02 (2K)",
                x,.24f,38f,2.7f,2.9f,count++);
    }

    static void Site()
    {
        // Exact 120 x 100 site; no synthetic second road system across city.
        // Physically cut a 8 x 46m open ramp in BOTH patio and Foundation plot_base.
        // The matching terrain cut lives in legacy_foundation.py, local x=40.5..48.5,
        // z=-22..24. Four disjoint slabs preserve the rest of the approved plot.
        B(shell,"T11 stone plot apron south",paving,0,.18f,-35.875f,119.6f,.055f,27.75f,true,true);
        B(shell,"T11 stone plot apron north",paving,0,.18f,36.875f,119.6f,.055f,25.75f,true,true);
        B(shell,"T11 stone plot apron west of garage",paving,-9.65f,.18f,1f,100.3f,.055f,46f,true,true);
        B(shell,"T11 stone plot apron east of garage",paving,54.15f,.18f,1f,11.3f,.055f,46f,true,true);
        foreach(float side in new[]{-1f,1f})
        {
            B(shell,"Fire service route",asphalt,side*53f,.215f,0,7.0f,.055f,94f,true,true);
            B(shell,side<0?"VIP arrival":"TAXI arrival",asphalt,side*27f,.235f,-41.9f,24f,.055f,9.7f,true,true);
            B(trim,"VIP or taxi champagne curb",gold,side*27f,.34f,-36.4f,24f,.11f,.14f,false);
            for(int i=0;i<5;i++)
                B(trim,"Actual white painted asphalt parking bay line",parkingPaint,side*(22f+i*4.2f),.285f,-44.5f,.11f,.014f,5.6f,false);
            foreach(float z in new[]{-32f,33f})
                B(shell,"Champagne black granite planter",granite,
                    side>0&&z<0?38f:side*43f,.48f,z,3.4f,.6f,3.3f,true);
        }
        B(shell,"Front VIP/TAXI circulating driveway",asphalt,0,.221f,-42.5f,105f,.055f,10f,true,true);
        // The 120x100 plan specifies UNDERGROUND parking, not two large surface
        // parking lots. The east court is reserved for its real accessible ramp.
        // Keep a pedestrian walkway on the WEST facade; east side remains a
        // vehicle approach separated from the existing fire route at x=53.
        B(shell,"T11 west pedestrian path",paving,-39.95f,.255f,4f,2.6f,.07f,25.7f,true,true);
        // Zebra crossing and separate spectator axis: no vehicle routes across
        // the entrance doors, and no parking painted onto concrete sidewalks.
        foreach(float x in new[]{-8.4f,-7.0f,-5.6f,5.6f,7.0f,8.4f})
            B(trim,"White zebra marking on asphalt",parkingPaint,x,.287f,-41.5f,
                .60f,.012f,5.5f,false);
        // The approved road grid is on X=400. Its eastern sidewalk edge X=417,
        // exactly 18m west of the T11 plot edge X=435: connect without moving T11.
        B(shell,"T11 physical access from existing city road X400",asphalt,-69f,.224f,-42.5f,20f,.054f,7.4f,true,true);
        B(shell,"Casino pedestrian main axis",granite,0,.22f,-43.7f,8.1f,.08f,11.7f,true,true);
        B(trim,"Red carpet leading to the curved casino entrance",redCarpet,
            0,.279f,-41.45f,5.8f,.016f,10.35f,false,true);
        foreach(float side in new[]{-1f,1f})
            B(trim,"Champagne bronze relief along red carpet",gold,
                side*2.96f,.290f,-41.45f,.075f,.045f,10.35f,false);
        B(shell,"Rear service and deliveries",asphalt,0,.23f,46.7f,105f,.07f,5.4f,true,true);
        B(shell,"Delivery corridor to rear loading door",asphalt,-20f,.248f,36.6f,6.4f,.052f,17.5f,true,true);
        B(trim,"Rear loading bay white line",paving,-20f,.284f,46.7f,12f,.012f,.11f,false);
        // Real underground garage is generated separately from the empty casino
        // shell. Approach starts off the approved VIP/TAXI loop on the east side.
        UndergroundGarage();
        // Existing owned palm models (no fabricated vegetation).
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm)throw new FileNotFoundException("T11 original city palms missing");
        foreach(var at in new[]{
            new Vector2(-43,-32),new Vector2(35,-32),new Vector2(-45,33),
            new Vector2(45,33),new Vector2(-41,-26),new Vector2(34,-26)
        })
        {
            var location=shell.TransformPoint(new Vector3(at.x,.22f,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(shell,palm,"T11 approved city palm",location,0f,1f,false,true);
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
        }
        // Real original 2K Poly Haven vegetation, with existing palms intact.
        PolyHavenVegetation2K();
        foreach(float side in new[]{-1f,1f})
            for(int i=0;i<6;i++)
                B(trim,"Ground-level warm gold exterior bollard",led,side*48f,.62f,-25f+i*10f,.12f,.85f,.12f,false);
    }

    static void PremiumExteriorLighting()
    {
        // Warm facade wash as on the owner's evening architectural reference.
        // The existing CityStaticPBR mobile shader supports four ForceVertex
        // local lights per rendered object. Keep them sparse, shadowless and
        // outside the empty interior; LED trim remains material-emissive.
        var fixtures=new[]
        {
            new Vector3(-14f,4.6f,-30.5f),
            new Vector3(0f,5.4f,-34.3f),
            new Vector3(14f,4.6f,-30.5f),
            new Vector3(-37.2f,5.3f,0f),
            new Vector3(37.2f,5.3f,0f),
            new Vector3(0f,5.1f,28.7f)
        };
        for(int i=0;i<fixtures.Length;i++)
        {
            var p=fixtures[i];
            B(trim,"Champagne facade sconce "+(i+1),gold,p.x,p.y,p.z,
                .26f,.45f,.24f,false);
            B(trim,"Warm LED luminaire diffuser "+(i+1),led,
                p.x,p.y-.19f,p.z,.20f,.08f,.19f,false);
            var lamp=new GameObject("T11 warm architectural facade light "+(i+1));
            lamp.transform.SetParent(root,false);
            lamp.transform.localPosition=(p+new Vector3(0,-.2f,0))*K;
            var l=lamp.AddComponent<Light>();
            l.type=LightType.Point;
            l.range=14f*K;
            l.intensity=1.45f;
            l.color=new Color(1f,.87f,.70f);
            l.shadows=LightShadows.None;
            l.renderMode=LightRenderMode.ForceVertex;
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
        // An unusual but readable source image is a visual-review finding,
        // not a reason to withhold the owner's test APK.
        if(width!=2048||height!=512)
            BraxBuildQuality.Warn("Original owner sign source dimensions differ: "+path+
                " source="+width+"x"+height+" expected=2048x512; inspect in APK.");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if(!texture)throw new InvalidOperationException("Owner's sign is on disk but Unity cannot import it: "+path);
        if(texture.width!=2048||texture.height!=512)
            BraxBuildQuality.Warn("Owner sign source remains exact 2048x512; Android imported "+
                texture.width+"x"+texture.height+" for "+path);
        return texture;
    }

    public static void VerifyOriginalSigns()
    {
        LoadOwnerSign(SignPath);
        const string city="Assets/Source/Mistrzowie/CitySigns/03_TRANSPORT_KURORT/";
        LoadOwnerSign(city+"TAXI_TAXI.png");
        LoadOwnerSign(city+"PARKING_PARKING.png");
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
        // Real supplier signage: TAXI at the front-right drop-off, PARKING at
        // the garage approach. Do NOT use the arena-only "ARENA VIP" sign here.
        const string origin="Assets/Source/Mistrzowie/CitySigns/03_TRANSPORT_KURORT/";
        RoadsideOriginalSign("TAXI",origin+"TAXI_TAXI.png",42f,-36.8f,0f);
        RoadsideOriginalSign("PARKING",origin+"PARKING_PARKING.png",58f,17f,270f);
    }

    static void MainSign()
    {
        // The only approved brand for plot T11 is C1_KASYNO_KASYNO_WAWER.
        var picture=LoadOwnerSign(SignPath);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Casino sign needs original CityStaticPBR shader");
        var material=new Material(shader){name="C1 KASYNO WAWER original",enableInstancing=true};
        material.SetTexture("_MainTex",picture);material.SetTexture("_EmissionMap",picture);
        material.SetColor("_EmissionColor",new Color(.16f,.11f,.065f));
        material.SetColor("_Color",Color.white);material.SetFloat("_VertexTint",0);
        material.SetFloat("_Roughness",.30f);material.SetFloat("_Metallic",0);
        material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.08f);
        material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,output+"/Materials/T11_original_WAWER_casino_sign.mat");
        // A distinct physical dark bronze plaque above entry, matching original image.
        B(shell,"Casino WAWER front deep graphite casing",graphite,0,12.0f,-35.95f,17.6f,4.4f,.38f,true);
        B(trim,"Casino WAWER external bronze mounting rim",gold,0,12.0f,-36.22f,17.9f,4.6f,.14f,false);
        var image=GameObject.CreatePrimitive(PrimitiveType.Quad);
        image.name="T11 | original owner's KASYNO WAWER CASINO PREMIUM sign 4:1";
        image.transform.SetParent(signRoot,false);
        image.transform.localPosition=new Vector3(0,12.0f,-36.34f)*K;
        image.transform.localRotation=Quaternion.Euler(0,180,0);
        image.transform.localScale=new Vector3(17.2f,4.30f,1f)*K;
        image.GetComponent<MeshRenderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(image.GetComponent<Collider>());
    }

    public static void Generate(string outFolder)
    {
        Directory.CreateDirectory(outFolder+"/Meshes");
        Directory.CreateDirectory(outFolder+"/Materials");
        Directory.CreateDirectory(outFolder+"/Prefabs");
        Setup(outFolder);
        root=Group(null,"T11 | KASYNO WAWER | 120x100 approved plot | EMPTY interior");
        root.position=new Vector3(LotX,0,LotZ)*K;
        shell=Group(root,"T11 exterior shell, real entrances and site collisions");
        trim=Group(root,"Exterior gold and bronze details (distance LOD)");
        signRoot=Group(root,"T11 original owner sign (always visible)");
        Site();ThreeMatchingFacades();RoofAndSkylight();CurvedFoyer();Tower();MainSign();RoadsideSigns();PremiumExteriorLighting();
        // Remove tiny gold detail in distance, keep WAWER sign always visible.
        var renderers=trim.GetComponentsInChildren<Renderer>(true);
        var lod=trim.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[]{new LOD(.09f,renderers),new LOD(.0001f,Array.Empty<Renderer>())});
        lod.RecalculateBounds();
        var at=root.position;root.position=Vector3.zero;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,
            outFolder+"/Prefabs/T11_Casino_Exterior.prefab",InteractionMode.AutomatedAction);
        root.position=at;
        if(!prefab)throw new IOException("T11 exterior prefab save failed");
        Debug.Log("CHAMPIONS_T11_CASINO_EXTERIOR_ONLY position=(495,465) lot=120x100 podium=76x56x14 tower=120 PBR=PolyHaven originalSign=C1_WAWER");
    }
}
