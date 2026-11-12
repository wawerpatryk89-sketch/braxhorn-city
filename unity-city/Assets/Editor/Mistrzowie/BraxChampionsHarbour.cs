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
    public static Material Steel,Graphite,Rubber,PedestalWhite,Led,SignFace,SignDark,Fender,Rope,CastIron,ShelterGlass,GateStone;
    // gangway portal (owner 10.11 10:17 "zakaz tanich napisów"): white marble pylons + lintel, graphite sign band
    public const float GatePylon=.9f,GateTop=4.4f,GateLintel=1.15f,GateFront=-148.75f,GateBack=-149.95f;

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

    /// <summary>Owner 12.11 11:39 ("wszystkie białe linie pasy migotają, nic nie ma migotać"): paint lying on a surface (road
    /// lane lines, zebras, parking bays, garage stripes) stands ~1 cm over it - on a phone, from 100 m on, the depth buffer
    /// cannot tell 1 cm apart and the line flickers. Its material gets CityStaticPBR's depth offset (polygon offset -1/-1, the
    /// same the city uses for its renovation): the paint is always drawn in front of the surface it lies on, at any distance.
    /// Only for materials used as paint (nothing else shares them).</summary>
    public static Material Nakladka(Material m)
    {
        if(!m||!m.HasProperty("_OffsetFactor"))return m;
        m.SetFloat("_OffsetFactor",-1f);m.SetFloat("_OffsetUnits",-1f);
        EditorUtility.SetDirty(m);
        return m;
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
        // 3D lettering (ZASADA-NAPISY-PREMIUM 10.11): brushed stainless faces with a soft warm halo of their own
        // (readable at dusk and in the shade of a band), graphite returns; never cream "LED text"
        SignFace=Surface("sign_letters_brushed_stainless",new Color(.90f,.91f,.92f),.24f,.80f,new Color(.34f,.32f,.28f));
        SignDark=Surface("sign_letter_returns_graphite",new Color(.08f,.085f,.09f),.38f,.55f);
        Fender=Surface("boat_fender_white",new Color(.94f,.95f,.95f),.46f,0f);
        Rope=Surface("mooring_line_navy",new Color(.10f,.14f,.24f),.82f,0f);
        CastIron=Surface("bollard_cast_iron",new Color(.11f,.115f,.12f),.52f,.65f);
        ShelterGlass=Surface("shelter_glass",new Color(.80f,.88f,.93f,.30f),.05f,0f,null,true);
        GateStone=BraxChampionsFoundation.LibraryMarbleWhite("harbour_gate_white_marble",new Color(1.0f,1.0f,1.0f),1.6f);
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

    // Owner 10.11 12:52 "wszystko w skalę": the yacht GLB is a real 40.33 m superyacht (8.21 m beam, decks 2.1 / 4.8 / 6.8 /
    // 9.8 m above the waterline) - moored at its real size (it was squeezed to 19.5 m: doors half a person high).
    public const float YachtLength=40.33f;
    static float Length(BraxChampionsFoundation.Berth b)=>b.kind=="yacht"?YachtLength:b.kind=="motorboat"?8.5f:3.25f;

    // ---------------------------------------------------------------- yacht liveries and names
    // Owner 10.11: "dużo ich zrób ... naklejki na nich narobić, coś, żeby troszeczkę się różniły". Five hulls (original
    // white + four repainted atlases from build_yacht_liveries.py: only the painted hull topsides change) and a solid 3D
    // name with the home port on the raked transom of every yacht.
    public static readonly string[] YachtLiveries={"","navy","black","emerald","sapphire"};
    public static readonly string[] YachtNames={"BRAXHORN ONE","AURELIA","MISTRAL","ZEPHYR","ALTAIR","CORALIE","ESTRELLA",
        "AQUAMARINE","ORION","LUNA","SOLEIL","VALKYRIA","SERENITY","NOVA STAR","ATLANTICA"};
    public static int LiveriesApplied,YachtNamesBuilt;
    static readonly Dictionary<string,Material> liveryMaterials=new Dictionary<string,Material>();
    static Material Livery(Material city,string id)
    {
        if(string.IsNullOrEmpty(id)||!city)return city;
        if(liveryMaterials.TryGetValue(id,out var m)&&m)return m;
        string png="Assets/Source/HarborBeach/Liveries/yacht_livery_"+id+".png";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        if(!tex){BraxBuildQuality.Warn("DO POPRAWKI YACHT_LIVERY_MISSING "+png+" (build_yacht_liveries.py) - white hull");return city;}
        m=new Material(city){name="Mistrzowie yacht livery "+id,enableInstancing=true};
        m.SetTexture("_MainTex",tex);
        m=Save(m,Out+"/Materials/harbour_yacht_livery_"+id+".mat");
        liveryMaterials[id]=m;
        return m;
    }

    // ---------------------------------------------------------------- cruise ship B at W01
    // Owner 10.11 (ships to finish the port): model B "cruise-ship.glb" by Innerscene Corp, CC0 1.0 (licence in the GLB
    // asset block), a real 289.4 m ship in metres: keel at y 0, boot-top 7.5-8.6 m (waterline 8.0), white hull 8.6-15.3,
    // sun deck 48.1, pools x 143-182. Moored at its real size alongside the cruise quay, bow to the sea, its name on both
    // bows and on the stern, the sun deck furnished with teak loungers round the pools.
    // Owner 10.11 16:08: "Wycieczkowiec nazwał DIVINA P&M" - the ship is the DIVINA P&M (was BRAXHORN), home port MIASTO MISTRZÓW.
    public const string CruiseShipName="DIVINA P&M";
    public const string CruiseShipFile="cruise_ship_innerscene.glb";
    public const float CruiseLength=289.4f,CruiseWaterline=8.0f,CruiseSunDeck=48.1f;
    public static int CruiseShips,CruiseLoungers,CruiseNames;

    // text on a (slightly) tilted face: centre and normal in the parent's frame, letters 3 cm proud
    static void FaceText(Transform parent,string label,string text,Vector3 centre,Vector3 normal,float cap,float maxWidth,
                         Material face,Material ret,float unit,float depth=.025f,float proud=.006f)
    {
        var n=normal.normalized;
        var go=new GameObject(label).transform;go.SetParent(parent,false);
        go.localPosition=centre+n*(proud*(unit>0f?unit:K));
        go.localRotation=Quaternion.LookRotation(-n,Vector3.up);         // face n, letters upright (no roll on a raked face)
        var m=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(m,face,ret,text,new Vector3(0,-cap*.5f,0),180f,cap,depth,.14f,maxWidth);
        BraxChampionsLetters3D.Build(m,go,label,unit);
    }

    static int PlaceCruiseShip(Transform parent,BraxChampionsFoundation.Berth bay)
    {
        var src=BraxHarborBeachBuild.Model(CruiseShipFile);
        if(!src){BraxBuildQuality.Warn("DO POPRAWKI CRUISE_SHIP_MISSING "+CruiseShipFile+" - berth "+bay.id+" stays free");return 0;}
        var root=new GameObject(bay.id+" | cruise_ship | "+CruiseShipName+" | owner model B (Innerscene, CC0)").transform;
        root.SetParent(parent,false);
        var centre=new Vector3(bay.x*K,WaterLevel*K,bay.z*K);
        root.position=centre;
        var model=UnityEngine.Object.Instantiate(src);
        model.name=bay.id+" | "+CruiseShipName+" cruise ship (real 289 m)";
        model.transform.SetParent(root.transform,false);
        model.transform.localScale=Vector3.one*K;                       // the GLB is in metres: real size
        // long axis to the root's z, bow (the anchors) to +z
        model.transform.localRotation=Quaternion.Euler(0,90f,0);
        var bounds=BraxRegion.Bounds(model);
        if(bounds.size.x>bounds.size.z){model.transform.localRotation=Quaternion.identity;bounds=BraxRegion.Bounds(model);}
        var anchors=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Anchors");
        if(anchors)
        {
            var ab=BraxRegion.Bounds(anchors.gameObject);
            if(ab.center.z<bounds.center.z){model.transform.localRotation=Quaternion.Euler(0,180f,0)*model.transform.localRotation;bounds=BraxRegion.Bounds(model);}
        }
        else BraxBuildQuality.Warn("DO POPRAWKI CRUISE_SHIP_NO_ANCHORS node - bow direction assumed");
        if(Mathf.Abs(bounds.size.z-CruiseLength*K)>2f*K)
            throw new InvalidOperationException("Cruise ship B is not its real 289.4 m (got "+bounds.size.z/K+" m)");
        model.transform.position+=centre-new Vector3(bounds.center.x,bounds.min.y+CruiseWaterline*K,bounds.center.z);
        root.transform.rotation=Quaternion.Euler(0,bay.yaw,0);           // bow (+z of the root) to the open sea
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }
        // Livery + deck in the model's own frame (glTF metres, glTFast mirrors x: Unity x = -glTF x), parent carries K.
        var t=model.transform;
        var navy=Surface("cruise_name_navy_gloss",new Color(.05f,.09f,.22f),.25f,.15f);
        // the name on both bows: the flat hull side (half beam 16.10 m) from glTF x 199 to 211, letters up to 1.8 m high
        foreach(float side in new[]{1f,-1f})
        {
            var m=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(m,navy,Graphite,CruiseShipName,new Vector3(-205f,10.95f,side*16.13f),side>0?0f:180f,1.8f,.12f,.14f,12.5f);
            BraxChampionsLetters3D.Build(m,t,"cruise ship bow name "+(side>0?"starboard":"port"),1f);
            CruiseNames++;
        }
        // stern (raked, glTF x 0.40-1.43 over y 14.5-9.7). Owner 10.11 16:40 "Poprawka": the name ran through the upper
        // porthole row and the home port through the lower one. The hull texture has two porthole rows on the stern,
        // centres y 12.97 and 10.61 (r .24): the name now fills the clear band between them (10.85-12.73), the home
        // port the band above the upper row (13.21-14.50), each on the transom plane measured at its height.
        FaceText(t,"cruise ship stern name",CruiseShipName,new Vector3(-.868f,11.79f,0),new Vector3(.976f,-.219f,0),1.25f,22f,navy,Graphite,1f,.10f,.03f);
        FaceText(t,"cruise ship stern home port","MIASTO MISTRZÓW",new Vector3(-.497f,13.85f,0),new Vector3(.990f,-.140f,0),.42f,16f,navy,Graphite,1f,.05f,.03f);
        CruiseNames+=2;
        CruiseLoungers+=SunDeck(t);
        CruiseShips++;
        return 1;
    }

    // Owner 10.11 ("Po kasynie dokończ plaża port"): the second excursion ship, the owner's early-1900 liner (model C,
    // Mixmamo.studio, CC BY 4.0 - HarborBeach/CREDITS.md), at berth W02 on the west face of the cruise pier in its real
    // size: hull 118 m (123.7 m with the stern ensign staff), beam 16.9 m. Exterior-only derivative of the owner's 215 MB
    // file (the full interior removed with glTF-Transform, 204k triangles, 14 MB). Its glTF origin lies on the waterline
    // (the red antifouling ends at y +0.16), the bow at glTF x 0, the ensign staff (node Object_1220) over the stern.
    public const string LinerFile="cruise_liner_1900.glb";
    public const float LinerHull=118.0f,LinerLengthOverall=123.7f;
    public static int LinersPlaced{get;private set;}
    static int PlaceLinerC(Transform parent,BraxChampionsFoundation.Berth bay)
    {
        var src=BraxHarborBeachBuild.Model(LinerFile);
        if(!src){BraxBuildQuality.Warn("DO POPRAWKI LINER_C_MISSING "+LinerFile+" - berth "+bay.id+" stays free");return 0;}
        var root=new GameObject(bay.id+" | cruise_ship | liner 1900 | owner model C (Mixmamo.studio, CC BY 4.0)").transform;
        root.SetParent(parent,false);
        var centre=new Vector3(bay.x*K,WaterLevel*K,bay.z*K);
        root.position=centre;
        var model=UnityEngine.Object.Instantiate(src);
        model.name=bay.id+" | liner 1900 (real 118 m hull)";
        model.transform.SetParent(root.transform,false);
        model.transform.localScale=Vector3.one*K;                       // the GLB is in metres: real size
        // long axis (glTF x) to the root's z; glTFast mirrors x (Unity x = -glTF x), so -90 puts the bow (glTF x 0) at +z
        model.transform.localRotation=Quaternion.Euler(0,-90f,0);
        var nodes=model.GetComponentsInChildren<Transform>(true);
        var hull=nodes.FirstOrDefault(t=>t.name=="Object_19");
        var ensign=nodes.FirstOrDefault(t=>t.name=="Object_1220");
        var hb=BraxRegion.Bounds(hull?hull.gameObject:model);
        if(hb.size.x>hb.size.z){model.transform.localRotation=Quaternion.identity;hb=BraxRegion.Bounds(hull?hull.gameObject:model);}
        // bow to +z of the root (the root then turns it to the sea): the ensign at the stern must lie towards -z
        if(ensign&&BraxRegion.Bounds(ensign.gameObject).center.z>hb.center.z)
        {
            model.transform.localRotation=Quaternion.Euler(0,180f,0)*model.transform.localRotation;
            hb=BraxRegion.Bounds(hull?hull.gameObject:model);
        }
        else if(!ensign)BraxBuildQuality.Warn("DO POPRAWKI LINER_C_NO_ENSIGN node - bow direction assumed");
        // the hull node alone is 118 m; without node names (never in Unity) the whole model with the ensign staff is 123.7 m
        float expected=hull?LinerHull:LinerLengthOverall;
        if(Mathf.Abs(hb.size.z-expected*K)>3f*K)
            throw new InvalidOperationException("Liner C is not its real size (got "+hb.size.z/K+" m, expected "+expected+")");
        if(!hull)hb.center+=new Vector3(0,0,(LinerLengthOverall-LinerHull)*.5f*K);   // the staff sticks out at the stern (-z)
        // hull centred on the berth; the glTF origin (waterline) stays on the water level of the root
        model.transform.position+=new Vector3(centre.x-hb.center.x,0f,centre.z-hb.center.z);
        root.transform.rotation=Quaternion.Euler(0,bay.yaw,0);           // bow to the open sea
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }
        LinersPlaced++;
        return 1;
    }

    // sun deck (glTF y 48.1, pools glTF x 143-182, z +-5, wind screens at +-11.6): two rows of teak loungers on each
    // side, feet to the pools, white cotton cushions (same real materials as the beach club)
    static int SunDeck(Transform ship)
    {
        var teak=BraxChampionsFoundation.SourceMaterial("beach_lounger_silver_teak","teak_veneer",1.1f,new Color(.91f,1.20f,1.72f));
        var cushion=BraxChampionsPromenade.Fabric("beach_cushion_white_cotton","cotton_jersey",new Color(1.20f,1.36f,1.46f),.6f);
        var m=new BraxChampionsMeshBatch();int n=0;
        const float y=CruiseSunDeck,w=.62f,L=1.95f,h=.30f,lie=L*.62f,back=L-lie;
        foreach(float side in new[]{1f,-1f})
        foreach(float rowFeet in new[]{5.75f,8.55f})
        for(float gx=144.5f;gx<=180.6f;gx+=1.5f)
        {
            float x=-gx;                                                   // Unity frame
            // feet at |z| = rowFeet (pool side), head outward
            // local frame: origin at the head end (outboard), +z toward the feet at the pool, backrest 0..back, seat back..L
            var head=new Vector3(x,y,side*(rowFeet+L));var r=Quaternion.LookRotation(new Vector3(0,0,-side),Vector3.up);
            Vector3 P(float lx,float ly,float lz)=>head+r*new Vector3(lx,ly,lz);
            foreach(float sx in new[]{-.30f,.30f})
            {
                m.Box(Graphite,P(sx,h,back+lie*.5f),new Vector3(.035f,.05f,lie),r);
                foreach(float lz in new[]{back+.06f,L-.05f})m.Box(Graphite,P(sx,h*.5f,lz),new Vector3(.035f,h,.035f),r);
            }
            m.Box(teak,P(0,h+.035f,back+lie*.5f),new Vector3(w*.92f,.02f,lie-.02f),r);
            m.Box(cushion,P(0,h+.075f,back+lie*.5f),new Vector3(w*.86f,.06f,lie*.94f),r);
            var tilt=r*Quaternion.Euler(40f,0,0);                           // backrest raised toward the head end
            var bc=P(0,h,back)+tilt*new Vector3(0,0,-back*.5f);
            m.Box(teak,bc+tilt*new Vector3(0,.035f,0),new Vector3(w*.92f,.02f,back*.96f),tilt);
            m.Box(cushion,bc+tilt*new Vector3(0,.075f,0),new Vector3(w*.86f,.06f,back*.90f),tilt);
            n++;
        }
        m.Build(ship,"cruise ship sun deck | teak loungers round the pools",Out+"/Meshes/Harbour",false,1f);
        return n;
    }

    // Raked tender-garage transom of yacht.glb, measured in the GLB mesh (10.11): name facet centre at 5.56 % of the length
    // from the stern and 29.2 % of the height from the keel, normal (aft, up) = (0.84, 0.55); the home-port facet below it
    // at 4.40 % / 24.2 %, normal (0.88, 0.47). Panel 3.1 m wide.
    static void YachtName(Transform root,Bounds b,int index)
    {
        string name=YachtNames[index%YachtNames.Length];
        bool white=string.IsNullOrEmpty(YachtLiveries[index%YachtLiveries.Length]);
        Material face=white?Graphite:Steel,ret=white?Steel:Graphite;
        void Line(string text,float fz,float fy,Vector3 normal,float cap,float maxWidth,string label)
        {
            var n=normal.normalized;
            var rot=Quaternion.LookRotation(-n,Vector3.up);               // face n, letters upright (no roll)
            var centre=new Vector3(b.center.x,b.min.y+fy*b.size.y,b.min.z+fz*b.size.z);
            var go=new GameObject(label).transform;go.SetParent(root,false);
            go.localPosition=root.InverseTransformPoint(centre)+n*(.006f*K);
            go.localRotation=rot;
            var m=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(m,face,ret,text,new Vector3(0,-cap*.5f,0),180f,cap,.025f,.14f,maxWidth);
            BraxChampionsLetters3D.Build(m,go,label,K);
        }
        Line(name,.0556f,.2923f,new Vector3(0,.55f,-.84f),.30f,2.6f,"yacht name "+name);
        Line("BRAXHORN",.0440f,.2424f,new Vector3(0,.47f,-.88f),.13f,1.6f,"yacht home port");
        YachtNamesBuilt++;
    }

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
        int yachtIndex=bay.kind=="yacht"&&int.TryParse(bay.id.Substring(1),out int nr)?nr-1:-1;
        if(yachtIndex>=0)YachtName(root.transform,BraxRegion.Bounds(model),yachtIndex);
        root.transform.rotation=Quaternion.Euler(0,bay.yaw,0);

        string livery=yachtIndex>=0?YachtLiveries[yachtIndex%YachtLiveries.Length]:"";
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(BraxRegion.CityMaterial).Select(c=>Livery(c,livery)).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }
        if(livery!="")LiveriesApplied++;

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
        // yacht: the helm on the bridge deck (6.8 m above the waterline, 11 % of the length ahead of midships)
        vessel.seat=new Vector3(0,(bay.kind=="yacht"?7.25f:.8f)*K,
                                (bay.kind=="yacht"?.114f:-.08f)*desiredLength*K);
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
        Letters(signs,"BERTH_"+number,at+r*new Vector3(0,.70f,.152f),faceYaw,1f,Graphite,SignDark);   // graphite on white composite
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
        // real 40 m yacht: fairleads on the main deck 2.06 m above the water, half beam 4.1 m
        float hullTop=WaterLevel+(b.kind=="yacht"?2.06f:.95f);
        float hullSide=b.z+(edgeZ>b.z?1:-1)*(b.kind=="yacht"?4.10f:1.10f);   // the hull side facing the finger
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
            // Owner 10.11 14:47 "mosty też mają swoją grubość - wszystkie": the quay reads as a solid concrete structure -
            // a 1.06 m edge beam 25 cm proud of the quay wall along every berth face and across the head, fenders on it.
            // 14:56 "nie tylko beton wylany": the edge beam is clad in speckled grey granite blocks (granite_tile_03, its
            // warm cast neutralised: no beige), under the graphite coping of the berth apron
            var edge=BraxChampionsFoundation.SourceMaterial("harbour_quay_granite_facing","granite_tile_03",2.4f,new Color(1.0f,1.10f,1.22f));
            m.BoxMinMax(edge,x0-.25f,x1+.25f,QuayY-1.0f,QuayY+.06f,z0-.25f,z0);
            m.BoxMinMax(edge,x0-.25f,x0,QuayY-1.0f,QuayY+.06f,z0,z1-.02f);
            m.BoxMinMax(edge,x1,x1+.25f,QuayY-1.0f,QuayY+.06f,z0,z1-.02f);
            foreach(float fx in faces)
            {
                float inward=fx==x0?1f:-1f;
                for(float z=z1-8f;z>z0+3f;z-=15f)Bollard(m,new Vector3(fx+inward*.75f,QuayY,z));
                for(float z=z1-4f;z>z0+2f;z-=8f)
                    m.Cylinder(Rubber,new Vector3(fx-inward*(.25f+.36f),-1.75f,z),.34f,1.80f,14,Quaternion.identity,true,true);
                // granite coping along the berth edge
                m.BoxMinMax(Graphite,Mathf.Min(fx,fx+inward*.45f),Mathf.Max(fx,fx+inward*.45f),QuayY,QuayY+.06f,z0,z1);
            }
            ApronFence(m,cruise,x0,x1,z0,z1);
            // the lamps stand in the palm rows of the walk (foundation furniture, port_layout.quay_fit): the walk between
            // the rows and the shelter stay clear (the old centre-line lamp at z1-12 stood inside the shelter)
            float cx=(x0+x1)*.5f;
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
                HarbourLight(harbour,m,edge,new Vector3(cx,QuayY,z0+5.5f));
            }
        }
        m.Build(harbour,"Nabrzeża stałe: pachołki, odbojnice, latarnie, wiata",Out+"/Meshes/Harbour");
    }

    // Owner 10.11 15:31 (asked where the barriers go: "Fartuch od alei"): the dark berth apron is separated from the
    // palm walk by black cast posts (0.90 m, stainless ring) with a sagging black chain, 30 cm inside the apron. A 3 m
    // gap at every lamp (the dark cross band leads through it to the ship) and at the root; the head has its railing.
    // Must match port_layout.quay_fit: aprons 4.5 m (cruise, both faces) / 3.0 m (ferry, east); lamps first+12-24k
    // with the first bed at z1-40 (cruise) / z1-20 (ferry).
    public const float ApronCruise=4.5f,ApronFerry=3.0f,QuayStep=24f;
    static void ApronFence(BraxChampionsMeshBatch m,bool cruise,float x0,float x1,float z0,float z1)
    {
        var lines=cruise?new[]{x0+ApronCruise-.30f,x1-ApronCruise+.30f}:new[]{x1-ApronFerry+.30f};
        float firstLamp=cruise?z1-40f+QuayStep*.5f:z1-20f+QuayStep*.5f;
        var gaps=new List<float>();
        for(float z=firstLamp;z>=z0+3f;z-=QuayStep)gaps.Add(z);
        float start=z1-6f,end=z0+2.5f;
        // runs between the gaps (from the root toward the head)
        var cuts=new List<(float hi,float lo)>();float hiZ=start;
        foreach(float g in gaps){if(g+1.5f<hiZ&&g-1.5f>end){cuts.Add((hiZ,g+1.5f));hiZ=g-1.5f;}}
        cuts.Add((hiZ,end));
        foreach(float x in lines)
            foreach(var (hi,lo) in cuts)
            {
                float len=hi-lo;if(len<1f)continue;
                int spans=Mathf.Max(1,Mathf.CeilToInt(len/2.4f));float step=len/spans;
                for(int i=0;i<=spans;i++)
                {
                    var b=new Vector3(x,QuayY,hi-i*step);
                    m.Cylinder(CastIron,b,.09f,.06f,14,Quaternion.identity,true,false);                          // foot
                    m.Cylinder(CastIron,b+Vector3.up*.06f,.055f,.80f,12,Quaternion.identity,false,false,.045f);   // post
                    m.Cylinder(Steel,b+Vector3.up*.70f,.052f,.03f,12,Quaternion.identity,false,false);            // ring
                    m.Cylinder(CastIron,b+Vector3.up*.86f,.06f,.04f,12,Quaternion.identity,true,false,.035f);     // cap
                    if(i==spans)break;
                    // sagging chain to the next post: 6 straight links of a catenary, 12 cm sag
                    Vector3 P(float t)=>new Vector3(x,QuayY+.78f-.12f*4f*t*(1f-t),hi-(i+t)*step);
                    for(int k=0;k<6;k++)
                    {
                        Vector3 a=P(k/6f),c=P((k+1)/6f),d=c-a;
                        m.Cylinder(CastIron,a,.011f,d.magnitude,6,Quaternion.FromToRotation(Vector3.up,d.normalized),false,false);
                    }
                }
            }
        ApronPosts+=lines.Length;
    }
    public static int ApronPosts;

    // Owner 10.11 14:57 "most wykończ": the head of the cruise pier gets its harbour light - a granite plinth, a tapered
    // white tower with two green bands (solid stacked sections, no paint on paint), a graphite gallery with a railing
    // and a glowing green lantern (the starboard light of the harbour entrance) under a graphite cap.
    static void HarbourLight(Transform harbour,BraxChampionsMeshBatch m,Material plinth,Vector3 at)
    {
        var green=Surface("harbour_light_green_band",new Color(.05f,.32f,.17f),.38f,.05f);
        var lens=Surface("harbour_light_green_lantern",new Color(.20f,.85f,.42f),.15f,0f,new Color(.25f,1.25f,.55f));
        m.Cylinder(plinth,at,1.5f,.45f,24,Quaternion.identity,true,false);
        float y0=.45f,y1=7.65f,r0=.85f,r1=.65f;
        float R(float y)=>r0+(r1-r0)*(y-y0)/(y1-y0);
        float[] cut={y0,2.85f,4.05f,5.25f,6.45f,y1};
        for(int i=0;i<cut.Length-1;i++)
            m.Cylinder(i%2==1?green:PedestalWhite,at+Vector3.up*cut[i],R(cut[i]),cut[i+1]-cut[i],24,Quaternion.identity,false,false,R(cut[i+1]));   // open ends: the plinth and the gallery close it
        m.Cylinder(Graphite,at+Vector3.up*y1,1.25f,.12f,24,Quaternion.identity,true,true);                 // gallery deck
        const int posts=16;float rr=1.17f,top=y1+.12f+1.0f;
        for(int i=0;i<posts;i++)
        {
            float a=i*Mathf.PI*2f/posts,b=(i+1)*Mathf.PI*2f/posts;
            var p=at+new Vector3(Mathf.Cos(a)*rr,0,Mathf.Sin(a)*rr);
            m.Cylinder(Steel,p+Vector3.up*(y1+.12f),.022f,.96f,8,Quaternion.identity,false,false);
            var q=at+new Vector3(Mathf.Cos(b)*rr,0,Mathf.Sin(b)*rr);
            var mid=(p+q)*.5f;float len=(q-p).magnitude;
            m.Box(Steel,mid+Vector3.up*(top-.02f),new Vector3(.045f,.04f,len+.02f),Quaternion.LookRotation(q-p,Vector3.up));   // top rail
        }
        m.Cylinder(Graphite,at+Vector3.up*(y1+.12f),.50f,.30f,20,Quaternion.identity,true,false);           // lantern base
        m.Cylinder(lens,at+Vector3.up*(y1+.42f),.38f,.62f,20,Quaternion.identity,false,false);             // green lens
        m.Cylinder(Graphite,at+Vector3.up*(y1+1.04f),.52f,.46f,20,Quaternion.identity,false,true,.08f);    // cap
        m.Cylinder(Steel,at+Vector3.up*(y1+1.50f),.025f,.55f,8,Quaternion.identity,true,false);            // finial
        BraxChampionsMeshBatch.Collider(harbour,"Latarnia portowa - głowica nabrzeża",at+Vector3.up*3.8f,new Vector3(1.8f,7.6f,1.8f));
        HarbourLights++;
    }
    public static int HarbourLights;

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
    // Gangway portal at the top of every gangway (owner 10.11 10:17, photo of the old gate: a thin black frame with cream
    // letters standing on its top bar - "kompletnie zakazuje takich tanich napisów ... globalnie"). Now architecture
    // carries the name: two white marble pylons flanking the gangway (brushed stainless plinths, a warm LED reveal on
    // the inner faces), a deep marble lintel with a recessed graphite sign band on both faces, the BRAXHORN lockup in
    // brushed stainless letters mounted on the band (front toward the promenade, back toward the pontoons), a warm LED
    // line washing the letters from below and downlights in the soffit. The glass balustrade ends at the pylons.
    static void Gates(Transform harbour,BraxChampionsFoundation.Pier[] piers,BraxChampionsFoundation.Opening[] openings)
    {
        var m=new BraxChampionsMeshBatch();
        var signs=new GameObject("Bramy pomostów - portale z nazwą 3D").transform;signs.SetParent(harbour,false);
        const float P=GatePylon,zf=GateFront,zb=GateBack,top=GateTop,lin=GateLintel,band0=.14f;
        float y0=QuayY,lb=y0+top-lin,cz=(zf+zb)*.5f,depth=zf-zb;
        foreach(var o in openings)
        {
            var pier=piers.FirstOrDefault(p=>p.z+p.d>=-150.01f&&p.x<o.x1&&p.x+p.w>o.x0);
            string id=pier==null?null:GateSign(pier);
            if(id==null){BraxBuildQuality.Warn("Port: przejście bez pomostu "+o.x0+"-"+o.x1);continue;}
            float x0=o.x0,x1=o.x1,cx=(x0+x1)*.5f,L0=x0-P,L1=x1+P;
            foreach(var (a,b,inner) in new[]{(L0,x0,x0),(x1,L1,x1)})
            {
                m.BoxMinMax(GateStone,a,b,y0+.12f,lb,zb,zf);                                       // marble pylon
                m.BoxMinMax(Steel,a-.02f,b+.02f,y0,y0+.12f,zb-.02f,zf+.02f);                       // stainless plinth
                // warm LED reveal on the face toward the gangway
                // (a real 15 mm profile standing on the stone - a 6 mm sheet lay in the marble plane and flickered)
                if(inner==x0)m.BoxMinMax(Led,x0,x0+.015f,y0+.45f,lb-.25f,cz-.05f,cz+.05f);
                else m.BoxMinMax(Led,x1-.015f,x1,y0+.45f,lb-.25f,cz-.05f,cz+.05f);
                BraxChampionsMeshBatch.Collider(harbour,id+" pylon",new Vector3((a+b)*.5f,y0+top*.5f,cz),new Vector3(b-a,top,depth));
            }
            m.BoxMinMax(GateStone,L0,L1,lb,y0+top,zb,zf);                                           // lintel
            m.BoxMinMax(Steel,L0-.02f,L1+.02f,y0+top,y0+top+.035f,zb-.02f,zf+.02f);                 // stainless coping
            float bx0=L0+.22f,bx1=L1-.22f,by0=lb+band0,by1=y0+top-band0;
            foreach(float face in new[]{1f,-1f})
            {
                float zs=face>0?zf:zb;
                m.BoxMinMax(Graphite,bx0,bx1,by0,by1,face>0?zs:zs-.015f,face>0?zs+.015f:zs);       // sign band
                m.BoxMinMax(Led,bx0+.05f,bx1-.05f,by0+.015f,by0+.027f,face>0?zs+.015f:zs-.035f,face>0?zs+.035f:zs-.015f);
            }
            for(float x=x0+.6f;x<=x1-.59f;x+=Mathf.Max(1.2f,(x1-x0-1.2f)/3f))
                m.Cylinder(Led,new Vector3(x,lb-.012f,cz),.07f,.012f,14,Quaternion.identity,false,true); // soffit downlights
            // the lockup: fitted inside the band (.5 m side margins), vertically centred
            float w=SignWidth(id),bandW=bx1-bx0-1.0f,lockH=.66f;
            float scale=w>bandW&&w>0f?bandW/w:1f;
            float yb=by0+((by1-by0)-lockH*scale)*.5f;
            Letters(signs,id,new Vector3(cx,yb,zf+.017f),0f,scale);
            Letters(signs,id,new Vector3(cx,yb,zb-.017f),180f,scale);
        }
        m.Build(harbour,"Bramy pomostów - portale",Out+"/Meshes/Harbour",true);
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
        LiveriesApplied=YachtNamesBuilt=0;liveryMaterials.Clear();CruiseShips=CruiseLoungers=CruiseNames=0;HarbourLights=0;LinersPlaced=0;
        var sources=new Dictionary<string,GameObject>();
        PrepareOriginalLibrary(sources);
        var root=new GameObject("MIASTO MISTRZÓW | PORT 10.11 | 15 yachts | 10 motorboats | 10 jetskis | ferry berth | the owner's ships B (W01) and C (W02)").transform;
        root.SetParent(foundation,false);
        var boats=new GameObject("Original user-owned moored models; shared meshes").transform;
        boats.SetParent(root,false);

        int placed=0,skisPlaced=0;
        foreach(var bay in berths)
        {
            if(bay.kind=="cruise_ship"&&bay.id=="W01"){placed+=PlaceCruiseShip(boats,bay);continue;}
            if(bay.kind=="cruise_ship"&&bay.id=="W02"){placed+=PlaceLinerC(boats,bay);continue;}
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
            +" signs3D="+SignsPlaced+" signsMissing="+SignsMissing+" textMeshLabels=0 originalGLBs yachtLength="+YachtLength
            +" yachtLiveries="+LiveriesApplied+"/15 yachtNames3D="+YachtNamesBuilt
            +" cruiseShipB="+CruiseShips+" cruiseNames3D="+CruiseNames+" sunDeckLoungers="+CruiseLoungers+" linerC="+LinersPlaced);
        return placed;
    }
}
