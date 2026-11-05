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
    static Material cream,graphite,granite,asphalt,paving,gold,steel,glass,led,roof,redCarpet,parkingPaint;

    static void Setup(string directory)
    {
        output=directory;serial=0;
        cream=BraxChampionsFoundation.SourceMaterial("t11_cream_facade","rectangular_facade_tiles_02",2.1f,new Color(.93f,.88f,.78f));
        graphite=BraxChampionsFoundation.SourceMaterial("t11_graphite_facade","rectangular_facade_tiles",2.0f,new Color(.22f,.23f,.25f));
        granite=BraxChampionsFoundation.SourceMaterial("t11_dark_granite","granite_tile",1.2f,new Color(.26f,.28f,.30f));
        paving=BraxChampionsFoundation.SourceMaterial("t11_walkways","concrete_pavement_02",1.8f,new Color(.94f,.93f,.89f));
        asphalt=BraxChampionsFoundation.SourceMaterial("t11_asphalt","clean_asphalt",2.1f,new Color(.9f,.9f,.9f));
        gold=BraxChampionsFoundation.LibraryMetal("t11_bronze_champagne",new Color(.75f,.57f,.32f));
        steel=BraxChampionsFoundation.LibraryMetal("t11_satin_steel",new Color(.32f,.34f,.37f));
        roof=BraxChampionsFoundation.SourceMaterial("t11_roof","concrete_floor_02",2.1f,new Color(.27f,.29f,.32f));
        redCarpet=BraxChampionsFoundation.SourceMaterial("t11_casino_red_carpet","fabric_pattern_05",.55f,new Color(.64f,.028f,.038f));
        parkingPaint=Surface("painted_parking_stripes",new Color(.97f,.97f,.93f),false,false);
        glass=Surface("clear_glass",new Color(.68f,.80f,.88f,.31f),true,false);
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
        if(mat==glass){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        if(tiled || ((mat==cream || mat==graphite || mat==granite || mat==roof || mat==paving || mat==asphalt) && Mathf.Max(w,Mathf.Max(h,d))>3f))
            Tile(o,new Vector3(w,h,d));
        if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        if(mat!=glass && mat!=led)
            GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return o;
    }
    static Transform Group(Transform parent,string name)
    {
        var o=new GameObject(name).transform;o.SetParent(parent,false);return o;
    }
    static void FrontPanel(float x,float z,float w,float bottom,float height)
    {
        B(shell,"T11 two-storey front glazing",glass,x,bottom+height*.5f,z,w,height,.11f);
        foreach(float v in new[]{x-w*.5f,x+w*.5f})
            B(trim,"Champagne vertical glass mullion",gold,v,bottom+height*.5f,z-.1f,.13f,height,.14f,false);
    }
    static void SidePanel(float x,float z,float w,float bottom,float height)
    {
        B(shell,"T11 two-storey side glazing",glass,x,bottom+height*.5f,z,.11f,height,w);
        foreach(float v in new[]{z-w*.5f,z+w*.5f})
            B(trim,"Champagne side frame",gold,x-.1f,bottom+height*.5f,v,.14f,height,.13f,false);
    }

    static void ThreeMatchingFacades()
    {
        // Two ivory/cream wings flank the curved glass atrium.
        foreach(float side in new[]{-1f,1f})
        {
            for(int bay=0;bay<3;bay++)
            {
                float x=side*(19f+bay*7.6f);
                B(shell,"Front pale facade pilaster",cream,x,7.1f,-27.9f,.82f,13.8f,.85f);
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
                B(shell,"Cream stone side pilaster",cream,xx,7.1f,zz,.82f,13.8f,.92f);
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
        // Rear service wall in exactly the same ivory + graphite + gold palette.
        // Keep a real six-metre-wide DELIVERY OPENING at x=-23..-17.
        B(shell,"Rear cream stone wing left",cream,-30.5f,7.0f,28f,15f,13.8f,.7f);
        B(shell,"Rear cream stone wing right",cream,10.5f,7.0f,28f,55f,13.8f,.7f);
        B(shell,"Service entry over-door stone",cream,-20f,9.7f,28f,6f,8.3f,.7f);
        B(trim,"Rear graphite base",graphite,8f,.58f,28.45f,60f,.8f,.15f,false);
        B(trim,"Rear champagne cornice",gold,0,13.72f,28.45f,75f,.20f,.14f,false);
        B(trim,"Rear warm gold LED line",led,0,13.86f,28.52f,74.4f,.085f,.10f,false);
        foreach(float xx in new[]{-36f,-25f,-14f,-3f,8f,19f,30f,36f})
            B(trim,"Rear polished graphite pilaster",graphite,xx,7.3f,28.49f,.58f,12.7f,.18f,false);
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
        B(shell,"Actual transparent rooftop skylight",glass,0,14.14f,0,24f,.11f,20f,false);
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
            var pane=B(shell,"Curved two-storey original glass frontage",glass,
                centre.x,(10.8f+bottom)*.5f,centre.z,length+.045f,10.8f-bottom,.115f);
            pane.transform.localRotation=yaw;
            if(i%4==0)
            {
                var mullion=B(trim,"Curved bronze vertical frame",gold,pa.x,5.55f,pa.z,.13f,10.6f,.14f,false);
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
            B(hinge,"Toughened clear door glass",glass,-side*width*.25f,1.8f,0,width*.5f,3.6f,.09f,true);
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

    static void Site()
    {
        // Exact 120 x 100 site; no synthetic second road system across city.
        B(shell,"T11 plot stone/cobble apron",paving,0,.18f,0,119.6f,.055f,99.5f,true,true);
        foreach(float side in new[]{-1f,1f})
        {
            B(shell,"Fire service route",asphalt,side*53f,.215f,0,7.0f,.055f,94f,true,true);
            B(shell,side<0?"VIP arrival":"TAXI arrival",asphalt,side*27f,.235f,-41.9f,24f,.055f,9.7f,true,true);
            B(trim,"VIP or taxi champagne curb",gold,side*27f,.34f,-36.4f,24f,.11f,.14f,false);
            for(int i=0;i<5;i++)
                B(trim,"Actual white painted asphalt parking bay line",parkingPaint,side*(22f+i*4.2f),.285f,-44.5f,.11f,.014f,5.6f,false);
            foreach(float z in new[]{-32f,33f})
                B(shell,"Champagne black granite planter",granite,side*43f,.48f,z,3.4f,.6f,3.3f,true);
        }
        B(shell,"Front VIP/TAXI circulating driveway",asphalt,0,.221f,-42.5f,105f,.055f,10f,true,true);
        // REAL ASPHALT SURFACE PARKING along both sides, separate from fire lanes.
        // Building outside face x=+/-38; fire route begins |x|=49.5. Bays fit
        // in the 11.5m strip without consuming the 7m fire-service road.
        foreach(float side in new[]{-1f,1f})
        {
            B(shell,"T11 marked side car park on Poly Haven Clean Asphalt",asphalt,
                side*45f,.252f,4f,6.2f,.06f,26f,true,true);
            for(int i=0;i<=5;i++)
            {
                float row=-9f+i*5.2f;
                B(trim,"Real white side parking bay border",parkingPaint,
                    side*45f,.291f,row,5.95f,.012f,.115f,false);
            }
            B(trim,"Parking court outer curb",granite,side*48.35f,.30f,4f,.22f,.22f,26f);
            B(shell,"Paved pedestrian walk alongside casino facade",paving,
                side*39.95f,.255f,4f,2.6f,.07f,25.7f,true,true);
        }
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
        B(shell,"Garage portal west upright",graphite,57.7f,2.5f,4f,.85f,4.5f,1.0f,true);
        B(shell,"Garage portal east upright",graphite,57.7f,2.5f,11.4f,.85f,4.5f,1.0f,true);
        B(shell,"Garage above-entry champagne beam",gold,57.7f,4.9f,7.7f,.9f,.34f,8.8f,true);
        // Portal stands outside the 7m side fire route (x=49.5..56.5).
        // Garage interior and descending ramp intentionally deferred by owner.
        // Existing owned palm models (no fabricated vegetation).
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm)throw new FileNotFoundException("T11 original city palms missing");
        foreach(var at in new[]{
            new Vector2(-43,-32),new Vector2(43,-32),new Vector2(-45,33),
            new Vector2(45,33),new Vector2(-41,-26),new Vector2(41,-26)
        })
        {
            var location=shell.TransformPoint(new Vector3(at.x,.22f,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(shell,palm,"T11 approved city palm",location,0f,1f,false,true);
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
        }
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

    static void MainSign()
    {
        // The only approved brand for plot T11 is C1_KASYNO_KASYNO_WAWER.
        var picture=AssetDatabase.LoadAssetAtPath<Texture2D>(SignPath);
        if(!picture)throw new FileNotFoundException("T11 original WAWER casino sign missing: "+SignPath);
        if(picture.width!=2048||picture.height!=512)
            throw new InvalidOperationException("Original KASYNO WAWER sign must remain 2048x512 4:1");
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
        Site();ThreeMatchingFacades();RoofAndSkylight();CurvedFoyer();Tower();MainSign();PremiumExteriorLighting();
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
