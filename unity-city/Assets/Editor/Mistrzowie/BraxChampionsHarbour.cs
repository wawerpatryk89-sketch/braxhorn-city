using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO MISTRZÓW — the harbour, rebuilt 10.11 for the owner ("ten port ma być lepiej zrobiony, mniejszy ... jachty mają
// być dobrze ustawione ... skuterki ... przy brzegu zaraz ... cały port jest do zmiany ... ma zrobić prawidłowo pod nas").
// Layout = port_layout.py (Foundation.json): every boat lies alongside its own finger pontoon, stern a metre off the main
// pontoon, bow to open water; jetskis at a low pontoon along the promenade quay. Only the owner's original GLBs
// (yacht, Riva, two jetskis). No floating text labels any more: berth numbers and pier names are solid 3D letters
// (build_harbour_signs.py -> HarbourSigns.bytes) on pedestals, posts and the gangway gates (ZASADA-SZYLDY-3D).
// Cruise and ferry berths stay free for the owner's ship models later (owner 09:14).
public static class BraxChampionsHarbour
{
    const string Out="Assets/PlayableGenerated/Mistrzowie";
    const float WaterLevel=-2.0f;   // nominal world units, EXACT foundation sea
    public const float PontoonY=-1.08f, SkiPontoonY=-1.37f, QuayY=.16f;
    static float K=>BraxClaudeOsiedle.K;

    static readonly Dictionary<string,string> OriginalFiles=new Dictionary<string,string>
    {
        {"yacht","yacht.glb"},
        {"motorboat","bateau_riva_boat.glb"},
        {"jetski_odd","jetski_01.glb"},
        {"jetski_even","jetski_02.glb"}
    };
    public static Material Steel,Graphite,Rubber,PedestalWhite,Led,SignFace,SignDark,Fender,Rope,CastIron,ShelterGlass;

    static Material Save(Material material,string path)
    {
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old)
        {
            EditorUtility.CopySerialized(material,old);
            UnityEngine.Object.DestroyImmediate(material);
            EditorUtility.SetDirty(old);
            return old;
        }
        AssetDatabase.CreateAsset(material,path);
        return material;
    }
    public static Material Surface(string id,Color c,float roughness,float metallic,Color? emission=null,bool transparent=false)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Champions harbour requires existing CityStaticPBR shader");
        var m=new Material(shader){name="Champions harbour "+id,enableInstancing=true};
        m.SetColor("_Color",c);m.SetFloat("_Roughness",roughness);
        m.SetFloat("_Metallic",metallic);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        if(emission.HasValue)m.SetColor("_EmissionColor",emission.Value);
        if(transparent)
        {
            m.SetFloat("_Transparent",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);
            m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");
        }
        return Save(m,Out+"/Materials/harbour_"+id+".mat");
    }

    public static void Materials()
    {
        Directory.CreateDirectory(Out+"/Materials");Directory.CreateDirectory(Out+"/Meshes/Harbour");
        // Brushed marine stainless (railings, cleats, gates trims): Metal048A maps, neutral tint (its colour map is warm).
        // neutral marine stainless (the Metal048A map is warm: tinted it read olive-green on the renders)
        Steel=Surface("marine_stainless_neutral",new Color(.80f,.82f,.84f),.26f,.78f);
        Graphite=Surface("graphite_powder_coat",new Color(.09f,.095f,.10f),.42f,.55f);
        Rubber=Surface("black_marine_rubber",new Color(.05f,.055f,.06f),.86f,0f);
        PedestalWhite=Surface("pedestal_white_composite",new Color(.93f,.94f,.94f),.34f,.04f);
        Led=Surface("warm_led",new Color(1f,.92f,.78f),.3f,0f,new Color(1.15f,.94f,.66f));
        // 3D lettering: warm white faces with their own light (readable at dusk), graphite returns
        SignFace=Surface("sign_letters_warm_white",new Color(.97f,.96f,.92f),.28f,.05f,new Color(.95f,.86f,.66f));
        SignDark=Surface("sign_letter_returns_graphite",new Color(.08f,.085f,.09f),.38f,.55f);
        Fender=Surface("boat_fender_white",new Color(.94f,.95f,.95f),.46f,0f);
        Rope=Surface("mooring_line_navy",new Color(.10f,.14f,.24f),.82f,0f);
        CastIron=Surface("bollard_cast_iron",new Color(.11f,.115f,.12f),.52f,.65f);
        ShelterGlass=Surface("shelter_glass",new Color(.80f,.88f,.93f,.30f),.05f,0f,null,true);
        BraxChampionsPromenadeFurniture.Materials();
    }

    // ---------------------------------------------------------------- 3D lettering (HarbourSigns.bytes)
    const string SignsPath="Assets/Source/Mistrzowie/HarbourSigns.bytes";
    [Serializable] sealed class SignFile{public float scaleK;public SignMesh[] signs;}
    [Serializable] sealed class SignMesh{public string id;public float nominalWidth;public BraxChampionsFoundation.MeshData[] meshes;}
    static SignFile signs;
    static readonly Dictionary<string,Mesh> signMeshes=new Dictionary<string,Mesh>();
    public static int SignsPlaced,SignsMissing;

    public static float SignWidth(string id)
    {
        LoadSigns();return signs?.signs?.FirstOrDefault(s=>s.id==id)?.nominalWidth??0f;
    }
    static void LoadSigns()
    {
        if(signs!=null)return;
        if(!File.Exists(SignsPath)){BraxBuildQuality.Warn("DO POPRAWKI HARBOUR_3D_SIGNS_MISSING "+SignsPath+" (build_harbour_signs.py)");signs=new SignFile{signs=new SignMesh[0]};return;}
        using(var f=File.OpenRead(SignsPath))using(var zip=new GZipStream(f,CompressionMode.Decompress))using(var r=new StreamReader(zip))
            signs=JsonUtility.FromJson<SignFile>(r.ReadToEnd());
    }
    /// <summary>Solid 3D letters (faces toward +Z at yaw 0, centred on x, baseline at y) at a nominal position.</summary>
    public static bool Letters(Transform parent,string id,Vector3 nominal,float yaw,float scale,Material face=null,Material dark=null)
    {
        LoadSigns();
        var sign=signs.signs?.FirstOrDefault(s=>s.id==id);
        if(sign==null){SignsMissing++;BraxBuildQuality.Warn("DO POPRAWKI HARBOUR_3D_SIGN_EMPTY "+id);return false;}
        // "sign" in the name: BraxSceneAudit counts it as a sign, never as a vessel
        var g=new GameObject("3D sign "+id).transform;g.SetParent(parent,false);
        g.localPosition=nominal*K;g.localRotation=Quaternion.Euler(0,yaw,0);g.localScale=Vector3.one*scale;
        foreach(var m in sign.meshes)
        {
            int count=m.positions==null?0:m.positions.Length/3;if(count==0||m.triangles==null)continue;
            if(!signMeshes.TryGetValue(m.id,out var mesh)||!mesh)
            {
                var v=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++)
                {
                    v[i]=new Vector3(m.positions[3*i],m.positions[3*i+1],m.positions[3*i+2]);
                    uv[i]=m.uv!=null&&m.uv.Length>=2*count?new Vector2(m.uv[2*i],m.uv[2*i+1]):Vector2.zero;
                }
                mesh=new Mesh{name="harbour 3D sign "+m.id,indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.vertices=v;mesh.uv=uv;mesh.triangles=m.triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                string path=Out+"/Meshes/Harbour/sign_"+m.id+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);mesh=old;}
                else AssetDatabase.CreateAsset(mesh,path);
                signMeshes[m.id]=mesh;
            }
            var o=new GameObject(id+" sign | "+m.role);o.transform.SetParent(g,false);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=m.role=="dark"?(dark?dark:SignDark):(face?face:SignFace);
            r.shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        }
        SignsPlaced++;
        return true;
    }

    // ---------------------------------------------------------------- the owner's boats
    static void PrepareOriginalLibrary(IDictionary<string,GameObject> sources)
    {
        foreach(var entry in OriginalFiles)
        {
            string full="Assets/Source/HarborBeach/Models/"+entry.Value;
            if(!File.Exists(full))throw new FileNotFoundException(
                "Missing original owner harbour GLB; no invented mesh will be substituted",full);
            var prefab=BraxHarborBeachBuild.Model(entry.Value);
            if(!prefab)throw new InvalidOperationException("Original imported harbour model not recognised: "+full);
            sources[entry.Key]=prefab;
        }
    }

    static float Length(BraxChampionsFoundation.Berth b)=>b.kind=="yacht"?19.5f:b.kind=="motorboat"?8.5f:3.25f;

    static int PlaceBoat(Transform parent,BraxChampionsFoundation.Berth bay,GameObject original,float pierY)
    {
        if(original==null)throw new ArgumentNullException(nameof(original));
        var root=new GameObject(bay.id+" | "+bay.kind+" | approved original owner GLB");
        root.transform.SetParent(parent,false);
        var centre=new Vector3(bay.x*K,WaterLevel*K,bay.z*K);
        root.transform.position=centre;
        var model=UnityEngine.Object.Instantiate(original);
        model.name=bay.id+" | original "+(bay.kind=="yacht"?"yacht":bay.kind=="jetski"?"jetski":"Riva");
        model.transform.SetParent(root.transform,false);

        // Bow of every owner model points to its +long axis (measured 10.11: the hull narrows there). The jetskis are
        // modelled along x: turn them -90 degrees so EVERY bow points to +z in the boat root (the old +90 turned the
        // jetskis with their bows to the land); the berth yaw then points the bow to open water.
        var bounds=BraxRegion.Bounds(model);
        if(bounds.size.x>bounds.size.z)
        {
            model.transform.RotateAround(bounds.center,Vector3.up,-90f);
            bounds=BraxRegion.Bounds(model);
        }
        if(bounds.size.z<.05f)throw new InvalidOperationException("Degenerate imported boat model: "+bay.id);
        float desiredLength=Length(bay);
        float size=desiredLength*K/bounds.size.z;
        if(!float.IsFinite(size)||size<.01f||size>100f)
            throw new InvalidOperationException("Unsafe actual boat size on "+bay.id+": "+size);
        model.transform.localScale*=size;
        bounds=BraxRegion.Bounds(model);
        var submerged=bounds.size.y*(bay.kind=="yacht"?.12f:bay.kind=="motorboat"?.17f:.20f);
        model.transform.position+=centre-new Vector3(bounds.center.x,bounds.min.y+submerged,bounds.center.z);
        root.transform.rotation=Quaternion.Euler(0,bay.yaw,0);

        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }

        // Boarding point on the finger / pontoon beside the hull, carry boat logic of the original city (BraxVessel).
        var vessel=root.AddComponent<BraxVessel>();
        vessel.label=bay.kind=="yacht"?"JACHT":bay.kind=="motorboat"?"MOTORÓWKA":"SKUTER";
        vessel.waterY=WaterLevel*K;
        vessel.draft=0f;
        vessel.length=desiredLength*K;
        vessel.width=bay.width*K;
        vessel.maxSpeed=bay.kind=="yacht"?7.5f:bay.kind=="motorboat"?13f:15f;
        vessel.turnRate=bay.kind=="yacht"?22f:bay.kind=="motorboat"?45f:65f;
        vessel.accel=bay.kind=="yacht"?1.6f:bay.kind=="motorboat"?3.5f:4.7f;
        vessel.sit=true;
        vessel.seat=new Vector3(0,(bay.kind=="yacht"?1.6f:.8f)*K,
                                (bay.kind=="yacht"?-.28f:-.08f)*desiredLength*K);
        Vector3 boarding=new Vector3(bay.accessX*K,pierY*K+.03f,bay.accessZ*K);
        vessel.wejscie=root.transform.InverseTransformPoint(boarding);
        vessel.moor=centre;vessel.moorYaw=bay.yaw;
        return 1;
    }

    // ---------------------------------------------------------------- pontoon fittings
    static Vector3 Bow(BraxChampionsFoundation.Berth b)=>Quaternion.Euler(0,b.yaw,0)*Vector3.forward;

    static void Cleat(BraxChampionsMeshBatch m,Vector3 at,float yaw)
    {
        var r=Quaternion.Euler(0,yaw,0);
        m.Box(Steel,at+new Vector3(0,.025f,0),new Vector3(.12f,.05f,.10f),r);
        m.Box(Steel,at+r*new Vector3(0,.075f,0),new Vector3(.05f,.06f,.05f),r);
        m.Box(Steel,at+new Vector3(0,.115f,0),new Vector3(.40f,.035f,.06f),r);
    }

    static void DeckLight(BraxChampionsMeshBatch m,Vector3 at)
    {
        m.Cylinder(Graphite,at,.075f,.50f,10);
        m.Cylinder(Led,at+Vector3.up*.50f,.078f,.05f,10);
        m.Cylinder(Graphite,at+Vector3.up*.55f,.085f,.025f,10);
    }

    static void RubRails(BraxChampionsMeshBatch m,BraxChampionsFoundation.Pier p,float deckY)
    {
        float x0=p.x,x1=p.x+p.w,z0=p.z,z1=p.z+p.d,y0=deckY-.20f,y1=deckY-.015f,t=.08f;
        m.BoxMinMax(Rubber,x0-t,x1+t,y0,y1,z0-t,z0);
        m.BoxMinMax(Rubber,x0-t,x1+t,y0,y1,z1,z1+t);
        m.BoxMinMax(Rubber,x0-t,x0,y0,y1,z0,z1);
        m.BoxMinMax(Rubber,x1,x1+t,y0,y1,z0,z1);
    }

    static void Pedestal(Transform signs,BraxChampionsMeshBatch m,Vector3 at,float faceYaw,string number)
    {
        // Shore power + water pedestal (marina standard): white composite body, stainless cap, warm LED crown,
        // the berth number in 3D letters on the face turned to the main pontoon.
        var r=Quaternion.Euler(0,faceYaw,0);
        m.Box(Graphite,at+new Vector3(0,.04f,0),new Vector3(.50f,.08f,.36f),r);
        m.Box(PedestalWhite,at+new Vector3(0,.55f,0),new Vector3(.44f,.94f,.30f),r);
        m.Box(Steel,at+new Vector3(0,1.04f,0),new Vector3(.48f,.05f,.34f),r);
        m.Box(Led,at+new Vector3(0,1.09f,0),new Vector3(.40f,.05f,.26f),r);
        m.Box(Graphite,at+r*new Vector3(0,.30f,.155f),new Vector3(.30f,.22f,.02f),r);     // socket door
        Letters(signs,"BERTH_"+number,at+r*new Vector3(0,.70f,.152f),faceYaw,1f);
    }

    static void BerthFittings(Transform signs,BraxChampionsMeshBatch m,BraxChampionsFoundation.Berth b,BraxChampionsFoundation.Pier dock,float deckY)
    {
        var bow=Bow(b);float len=Length(b);
        var stern=new Vector3(b.x,0,b.z)-bow*len*.5f;
        if(b.kind=="jetski")
        {
            // number post on the pontoon edge behind the stern, cleat beside it, a short stern line
            var post=new Vector3(b.x+.75f,deckY,-153f+.25f);
            m.Box(Graphite,post+new Vector3(0,.32f,0),new Vector3(.10f,.64f,.10f));
            m.Box(Graphite,post+new Vector3(0,.70f,0),new Vector3(.46f,.22f,.05f));
            Letters(signs,"BERTH_"+b.id,post+new Vector3(0,.64f,.028f),0f,1f);
            var cleat=new Vector3(b.x-.55f,deckY,-153f+.18f);Cleat(m,cleat,90f);
            m.Tube(Rope,cleat+Vector3.up*.11f,new Vector3(b.x,WaterLevel+.55f,-153f-.45f),.016f,6,6,.06f);
            return;
        }
        // finger edge facing this boat, along the boat
        float fz=dock.z+dock.d*.5f;
        float side=Mathf.Sign(b.z-fz);
        float edgeZ=fz+side*dock.d*.5f;
        float sx=Mathf.Sign(bow.x);                         // +1 bow east, -1 bow west
        float rootX=sx>0?dock.x:dock.x+dock.w;              // finger root at the main pontoon
        float tipX=sx>0?dock.x+dock.w:dock.x;
        // pedestal shared by the two boats of a finger: one per berth, offset to its own side of the finger
        Pedestal(signs,m,new Vector3(rootX+sx*1.1f,deckY,fz+side*dock.d*.18f),sx>0?270f:90f,b.id);
        // cleats: at the root (stern line), mid-boat (spring) and tip (bow line), all on this boat's edge
        float sternX=stern.x;
        var cRoot=new Vector3(sternX+sx*.8f,deckY,edgeZ-side*.18f);
        var cMid=new Vector3(sternX+sx*len*.55f,deckY,edgeZ-side*.18f);
        var cTip=new Vector3(tipX-sx*.35f,deckY,edgeZ-side*.18f);
        Cleat(m,cRoot,0f);Cleat(m,cMid,0f);Cleat(m,cTip,0f);
        float hullTop=WaterLevel+(b.kind=="yacht"?1.55f:.95f);
        float hullSide=b.z+(edgeZ>b.z?1:-1)*(b.kind=="yacht"?1.80f:1.10f);   // the hull side facing the finger
        m.Tube(Rope,cRoot+Vector3.up*.11f,new Vector3(sternX+sx*1.3f,hullTop,hullSide),.018f,6,6,.10f);
        m.Tube(Rope,cMid+Vector3.up*.11f,new Vector3(sternX+sx*len*.40f,hullTop,hullSide),.018f,6,6,.08f);
        m.Tube(Rope,cTip+Vector3.up*.11f,new Vector3(sternX+sx*len*.82f,hullTop-.1f,hullSide),.018f,6,6,.12f);
        // fenders hang from the finger edge between hull and pontoon
        foreach(float f in b.kind=="yacht"?new[]{.22f,.50f,.74f}:new[]{.28f,.62f})
        {
            var at=new Vector3(sternX+sx*len*f,deckY-.70f,edgeZ+side*.13f);
            m.Cylinder(Fender,at,.12f,.62f,10,Quaternion.identity,true,true);
            m.Tube(Rope,at+Vector3.up*.62f,new Vector3(at.x,deckY+.02f,edgeZ-side*.05f),.008f,4);
        }
    }

    static void PontoonDressing(Transform harbour,BraxChampionsFoundation.Pier[] piers,BraxChampionsFoundation.Berth[] berths)
    {
        var m=new BraxChampionsMeshBatch();
        var signs=new GameObject("Numery stanowisk 3D").transform;signs.SetParent(harbour,false);
        foreach(var p in piers)
        {
            if(p.role!="quay_wood"&&p.role!="quay_jetski")continue;
            float y=p.role=="quay_jetski"?SkiPontoonY:PontoonY;
            RubRails(m,p,y);
            bool finger=p.id.Contains("_FINGER_");
            if(finger)
            {
                // two cleats at the tip of every finger
                if(p.id.StartsWith("JETSKI"))
                {
                    Cleat(m,new Vector3(p.x+p.w*.5f,y,p.z+.30f),0f);
                }
                else
                {
                    float tip=p.id.Contains("_W_")?p.x+.30f:p.x+p.w-.30f;
                    Cleat(m,new Vector3(tip,y,p.z+.30f),90f);Cleat(m,new Vector3(tip,y,p.z+p.d-.30f),90f);
                }
                continue;
            }
            // deck lights along the main pontoons, alternate edges, every 9 m
            bool alongX=p.w>p.d;float len=alongX?p.w:p.d;int n=Mathf.Max(1,Mathf.FloorToInt(len/9f));
            for(int i=0;i<=n;i++)
            {
                float t=(i+.5f)/(n+1);
                float edge=(i%2==0)?.28f:-.28f;
                var at=alongX?new Vector3(p.x+p.w*t,y,(edge>0?p.z+p.d-.28f:p.z+.28f)):new Vector3((edge>0?p.x+p.w-.28f:p.x+.28f),y,p.z+p.d*t);
                DeckLight(m,at);
            }
        }
        foreach(var b in berths)
        {
            if(b.kind=="cruise_ship"||b.kind=="ferry")continue;
            var dock=piers.FirstOrDefault(p=>p.id==b.dock);
            if(dock==null){BraxBuildQuality.Warn("Port: brak pomostu "+b.dock+" dla "+b.id);continue;}
            BerthFittings(signs,m,b,dock,dock.role=="quay_jetski"?SkiPontoonY:PontoonY);
        }
        m.Build(harbour,"Pomosty: odbojnice, knagi, słupki prądu i wody, lampki, cumy, odbijacze",Out+"/Meshes/Harbour");
    }

    // ---------------------------------------------------------------- fixed quays (ferry, cruise)
    static void Bollard(BraxChampionsMeshBatch m,Vector3 at)
    {
        m.Cylinder(CastIron,at,.30f,.12f,14);
        m.Cylinder(CastIron,at+Vector3.up*.12f,.19f,.42f,14,Quaternion.identity,true,false,.17f);
        m.Cylinder(CastIron,at+Vector3.up*.54f,.27f,.10f,14);
        m.Cylinder(CastIron,at+Vector3.up*.64f,.27f,.06f,14,Quaternion.identity,true,false,.18f);
    }
    static void Lamp(BraxChampionsMeshBatch m,Vector3 at,float height,float armYaw,bool twin)
    {
        m.Box(Graphite,at+new Vector3(0,.05f,0),new Vector3(.36f,.10f,.36f));
        m.Cylinder(Graphite,at+Vector3.up*.10f,.085f,height-.1f,12,Quaternion.identity,true,false,.06f);
        foreach(float s in twin?new[]{1f,-1f}:new[]{1f})
        {
            var r=Quaternion.Euler(0,armYaw+(s<0?180f:0f),0);
            var head=at+Vector3.up*(height-.05f)+r*new Vector3(0,0,.55f);
            m.Box(Graphite,at+Vector3.up*(height-.05f)+r*new Vector3(0,0,.28f),new Vector3(.06f,.06f,.56f),r);
            m.Box(Graphite,head,new Vector3(.30f,.08f,.62f),r);
            m.Box(Led,head-Vector3.up*.045f,new Vector3(.24f,.012f,.54f),r);
        }
    }

    static void FixedQuays(Transform harbour,BraxChampionsFoundation.Pier[] piers)
    {
        var m=new BraxChampionsMeshBatch();
        foreach(var p in piers)
        {
            if(p.role!="quay_cruise"&&p.role!="quay_ferry")continue;
            bool cruise=p.role=="quay_cruise";
            float x0=p.x,x1=p.x+p.w,z0=p.z,z1=p.z+p.d;
            // berth faces: cruise both long sides, ferry its east side (the west side gets a railing)
            var faces=cruise?new[]{x0,x1}:new[]{x1};
            foreach(float fx in faces)
            {
                float inward=fx==x0?1f:-1f;
                for(float z=z1-8f;z>z0+3f;z-=15f)Bollard(m,new Vector3(fx+inward*.75f,QuayY,z));
                for(float z=z1-4f;z>z0+2f;z-=8f)
                    m.Cylinder(Rubber,new Vector3(fx-inward*.36f,-1.75f,z),.34f,1.80f,14,Quaternion.identity,true,true);
                // granite coping along the berth edge
                m.BoxMinMax(Graphite,Mathf.Min(fx,fx+inward*.45f),Mathf.Max(fx,fx+inward*.45f),QuayY,QuayY+.06f,z0,z1);
            }
            // lamps on the centre line
            float cx=(x0+x1)*.5f;
            for(float z=z1-12f;z>z0+4f;z-=24f)Lamp(m,new Vector3(cx,QuayY,z),7f,90f,true);
            if(cruise)
            {
                // passenger shelter at the quay root: slim steel frame, glass roof, two benches
                float sz0=z1-24f,sz1=z1-8f,sx0=cx-3f,sx1=cx+3f;
                foreach(float x in new[]{sx0,sx1})foreach(float z in new[]{sz0,sz1,(sz0+sz1)*.5f})
                    m.Box(Graphite,new Vector3(x,QuayY+1.6f,z),new Vector3(.14f,3.2f,.14f));
                m.BoxMinMax(Graphite,sx0-.2f,sx1+.2f,QuayY+3.2f,QuayY+3.36f,sz0-.2f,sz0);
                m.BoxMinMax(Graphite,sx0-.2f,sx1+.2f,QuayY+3.2f,QuayY+3.36f,sz1,sz1+.2f);
                m.BoxMinMax(Graphite,sx0-.2f,sx0,QuayY+3.2f,QuayY+3.36f,sz0,sz1);
                m.BoxMinMax(Graphite,sx1,sx1+.2f,QuayY+3.2f,QuayY+3.36f,sz0,sz1);
                m.BoxMinMax(ShelterGlass,sx0,sx1,QuayY+3.30f,QuayY+3.33f,sz0,sz1);
                m.BoxMinMax(Led,sx0+.4f,sx1-.4f,QuayY+3.185f,QuayY+3.2f,(sz0+sz1)*.5f-.04f,(sz0+sz1)*.5f+.04f);
                foreach(float x in new[]{cx-1.6f,cx+1.6f})
                {
                    m.BoxMinMax(Graphite,x-.25f,x+.25f,QuayY,QuayY+.42f,sz0+2f,sz1-2f);
                    m.BoxMinMax(BraxChampionsPromenadeFurniture.Teak,x-.28f,x+.28f,QuayY+.42f,QuayY+.47f,sz0+2f,sz1-2f);
                }
                BraxChampionsMeshBatch.Collider(harbour,"Wiata pasażerska - słupy",new Vector3(cx,QuayY+1.6f,(sz0+sz1)*.5f),new Vector3(6.2f,3.2f,.3f));
            }
        }
        m.Build(harbour,"Nabrzeża stałe: pachołki, odbojnice, latarnie, wiata",Out+"/Meshes/Harbour");
    }

    // ---------------------------------------------------------------- gangway gates with the pier names
    static string GateSign(BraxChampionsFoundation.Pier p)
    {
        if(p.id.StartsWith("FERRY"))return "GATE_FERRY";
        if(p.id.StartsWith("YACHT"))return "GATE_MARINA";
        if(p.id.StartsWith("MOTOR"))return "GATE_MOTOR";
        if(p.id.StartsWith("JETSKI"))return "GATE_WATERSPORTS";
        if(p.id.StartsWith("CRUISE"))return "GATE_CRUISE";
        return null;
    }
    static void Gates(Transform harbour,BraxChampionsFoundation.Pier[] piers,BraxChampionsFoundation.Opening[] openings)
    {
        var m=new BraxChampionsMeshBatch();
        var signs=new GameObject("Bramy pomostów - nazwy 3D").transform;signs.SetParent(harbour,false);
        foreach(var o in openings)
        {
            var pier=piers.FirstOrDefault(p=>p.z+p.d>=-150.01f&&p.x<o.x1&&p.x+p.w>o.x0);
            string id=pier==null?null:GateSign(pier);
            if(id==null){BraxBuildQuality.Warn("Port: przejście bez pomostu "+o.x0+"-"+o.x1);continue;}
            float x0=o.x0-.15f,x1=o.x1+.15f,z=-149.85f,top=4.10f,cx=(x0+x1)*.5f;   // on the promenade, in line with the balustrade
            foreach(float x in new[]{x0,x1})
            {
                // slim graphite post, brushed stainless plinth and cap, warm LED strip on the inner face
                float inner=x==x0?1f:-1f;
                m.Box(Graphite,new Vector3(x,QuayY+top*.5f,z),new Vector3(.22f,top,.22f));
                m.Box(Steel,new Vector3(x,QuayY+.09f,z),new Vector3(.30f,.18f,.30f));
                m.Box(Steel,new Vector3(x,QuayY+top+.02f,z),new Vector3(.28f,.04f,.28f));
                m.Box(Led,new Vector3(x+inner*.112f,QuayY+.35f+(top-.9f)*.5f,z),new Vector3(.006f,top-.9f,.05f));
                BraxChampionsMeshBatch.Collider(harbour,id+" słup bramy",new Vector3(x,QuayY+top*.5f,z),new Vector3(.22f,top,.22f));
            }
            m.BoxMinMax(Graphite,x0-.11f,x1+.11f,QuayY+top-.26f,QuayY+top,z-.13f,z+.13f);
            m.BoxMinMax(Steel,x0-.11f,x1+.11f,QuayY+top-.29f,QuayY+top-.26f,z-.13f,z+.13f);
            m.BoxMinMax(Led,x0+.12f,x1-.12f,QuayY+top-.302f,QuayY+top-.29f,z-.05f,z+.05f);
            float w=SignWidth(id);float span=x1-x0+1.6f;
            float scale=w>span?span/w:1f;
            Letters(signs,id,new Vector3(cx,QuayY+top+.06f,z),0f,scale);
        }
        m.Build(harbour,"Bramy pomostów",Out+"/Meshes/Harbour");
    }

    public static int PlaceOriginalFleet(Transform foundation,BraxChampionsFoundation.Berth[] berths,
                                         BraxChampionsFoundation.Pier[] piers,BraxChampionsFoundation.Opening[] openings)
    {
        if(!foundation)throw new ArgumentNullException(nameof(foundation));
        if(berths==null||berths.Length!=38)throw new InvalidOperationException(
            "Premium harbour contract requires 15 yachts + 10 Rivas + 10 jetskis, two excursion and one ferry berths");
        int yachts=berths.Count(b=>b.kind=="yacht");
        int motor=berths.Count(b=>b.kind=="motorboat");
        int skis=berths.Count(b=>b.kind=="jetski");
        int cruise=berths.Count(b=>b.kind=="cruise_ship");
        int ferry=berths.Count(b=>b.kind=="ferry");
        if(yachts!=15||motor!=10||skis!=10||cruise!=2||ferry!=1)
            throw new InvalidOperationException("Owner-approved port berth counts changed");

        Materials();SignsPlaced=SignsMissing=0;signs=null;signMeshes.Clear();
        var sources=new Dictionary<string,GameObject>();
        PrepareOriginalLibrary(sources);
        var root=new GameObject("MIASTO MISTRZÓW | PORT 10.11 | 15 yachts | 10 motorboats | 10 jetskis | ferry berth | 2 free berths for the owner's ships").transform;
        root.SetParent(foundation,false);
        var boats=new GameObject("Original user-owned moored models; shared meshes").transform;
        boats.SetParent(root,false);

        int placed=0,skisPlaced=0;
        foreach(var bay in berths)
        {
            if(bay.kind!="yacht"&&bay.kind!="motorboat"&&bay.kind!="jetski")continue;
            string file=bay.kind=="yacht"?"yacht":bay.kind=="motorboat"?"motorboat":
                (++skisPlaced%2==1?"jetski_odd":"jetski_even");
            var dock=piers.FirstOrDefault(p=>p.id==bay.dock);
            float pierY=dock!=null&&dock.role=="quay_jetski"?SkiPontoonY:PontoonY;
            placed+=PlaceBoat(boats,bay,sources[file],pierY);
        }
        BraxChampionsFoundation.Optional("pomosty: wyposażenie i numery",()=>PontoonDressing(root,piers,berths));
        BraxChampionsFoundation.Optional("nabrzeża promu i wycieczkowców",()=>FixedQuays(root,piers));
        BraxChampionsFoundation.Optional("bramy pomostów z nazwami 3D",()=>Gates(root,piers,openings??new BraxChampionsFoundation.Opening[0]));
        // Railings: the promenade quay edge (glass balustrade with openings at every gate), gangway handrails,
        // the jetski ramp and the heads of the fixed quays.
        BraxChampionsFoundation.Optional("barierki promenady i pomostów",()=>BraxChampionsHarbourSafety.Generate(root,piers,openings??new BraxChampionsFoundation.Opening[0]));
        AssetDatabase.SaveAssets();
        Debug.Log("CHAMPIONS_PORT_10_11 yacht=15 motorboat=10 jetski=10 ferryBerths=1 cruiseBerths=2 boats="+placed
            +" signs3D="+SignsPlaced+" signsMissing="+SignsMissing+" textMeshLabels=0 originalGLBs");
        return placed;
    }
}
