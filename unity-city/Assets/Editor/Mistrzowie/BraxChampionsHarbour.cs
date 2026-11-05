using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO MISTRZÓW — premium harbour is an extension of the actual Unity scene.
// Uses only original source GLBs from the owner's existing HarborBeach library.
// Ships larger than yachts (two excursion vessels + ferry) have REAL empty
// berths, gangways and signage, but NO fabricated substitute ship geometry.
public static class BraxChampionsHarbour
{
    const string Out="Assets/PlayableGenerated/Mistrzowie";
    const float WaterLevel=-2.0f;   // nominal world units, EXACT foundation sea
    static float K=>BraxClaudeOsiedle.K;

    static readonly Dictionary<string,string> OriginalFiles=new Dictionary<string,string>
    {
        {"yacht","yacht.glb"},
        {"motorboat","bateau_riva_boat.glb"},
        {"jetski_odd","jetski_01.glb"},
        {"jetski_even","jetski_02.glb"}
    };
    static Material metal,rubber,power,redFlag;

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
    static Material CreateSurface(string id,Color c,float roughness,float metallic)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Champions harbour requires existing CityStaticPBR shader");
        var m=new Material(shader){name="Champions harbour "+id,enableInstancing=true};
        m.SetColor("_Color",c);m.SetFloat("_Roughness",roughness);
        m.SetFloat("_Metallic",metallic);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        return Save(m,Out+"/Materials/harbour_"+id+".mat");
    }

    static GameObject Primitive(Transform parent,string name,PrimitiveType shape,
                                Vector3 centre,Vector3 scale,Material material,bool collision=false)
    {
        var o=GameObject.CreatePrimitive(shape);
        o.name=name;o.transform.SetParent(parent,false);
        o.transform.position=centre;o.transform.localScale=scale;
        var rend=o.GetComponent<Renderer>();rend.sharedMaterial=material;
        rend.shadowCastingMode=ShadowCastingMode.Off;rend.receiveShadows=false;
        if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic);
        return o;
    }

    static void Label(Transform parent,string name,string message,Vector3 at,float scale=.30f)
    {
        var t=new GameObject(name);
        t.transform.SetParent(parent,false);t.transform.position=at;
        t.transform.rotation=Quaternion.Euler(0,180,0);
        var mesh=t.AddComponent<TextMesh>();
        mesh.text=message;mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.fontSize=64;mesh.characterSize=scale;mesh.anchor=TextAnchor.MiddleCenter;
        mesh.alignment=TextAlignment.Center;mesh.color=new Color(.96f,.97f,.93f);
        t.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
    }

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

    static int PlaceBoat(Transform parent,BraxChampionsFoundation.Berth bay,GameObject original)
    {
        if(original==null)throw new ArgumentNullException(nameof(original));
        var root=new GameObject(bay.id+" | "+bay.kind+" | approved original owner GLB");
        root.transform.SetParent(parent,false);
        var centre=new Vector3(bay.x*K,WaterLevel*K,bay.z*K);
        root.transform.position=centre;
        var model=UnityEngine.Object.Instantiate(original);
        model.name=bay.id+" | original "+(bay.kind=="yacht"?"yacht":bay.kind=="jetski"?"jetski":"Riva");
        model.transform.SetParent(root.transform,false);

        // Original hull direction is derived from actual imported renderer bounds
        // (not from hand-created placeholder cubes), as in the first-city marina.
        var bounds=BraxRegion.Bounds(model);
        if(bounds.size.x>bounds.size.z)
        {
            model.transform.RotateAround(bounds.center,Vector3.up,90f);
            bounds=BraxRegion.Bounds(model);
        }
        if(bounds.size.z<.05f)throw new InvalidOperationException("Degenerate imported boat model: "+bay.id);
        float desiredLength=bay.kind=="yacht"?19.5f:bay.kind=="motorboat"?8.5f:3.25f;
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

        // Boarding point on the actual named wooden finger dock (not the sea).
        // Carry boat logic used by the original city, without editing characters.
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
        float pierY=bay.kind=="jetski"?-1.37f:-1.08f;
        Vector3 boarding=new Vector3(bay.accessX*K,pierY*K+.03f,bay.accessZ*K);
        vessel.wejscie=root.transform.InverseTransformPoint(boarding);
        vessel.moor=centre;vessel.moorYaw=bay.yaw;
        return 1;
    }

    public static int PlaceOriginalFleet(Transform foundation,BraxChampionsFoundation.Berth[] berths,
                                         BraxChampionsFoundation.Pier[] piers)
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

        var sources=new Dictionary<string,GameObject>();
        PrepareOriginalLibrary(sources);
        var root=new GameObject("MIASTO MISTRZÓW | 15 yachts | 10 motorboats | 10 jetskis | ferry | 2 cruise berths").transform;
        root.SetParent(foundation,false);
        var boats=new GameObject("Original user-owned moored models; shared meshes").transform;
        boats.SetParent(root,false);
        var details=new GameObject("Bollards, fenders, marina services and berth boards").transform;
        details.SetParent(root,false);
        metal=CreateSurface("satin_stainless",new Color(.44f,.49f,.51f,1),.30f,.82f);
        rubber=CreateSurface("black_marine_rubber",new Color(.08f,.095f,.10f,1),.88f,.02f);
        power=CreateSurface("navy_service_power",new Color(.08f,.19f,.29f,1),.48f,.42f);
        redFlag=CreateSurface("maritime_red_safety",new Color(.72f,.16f,.12f,1),.48f,.1f);

        int placed=0,skisPlaced=0;
        foreach(var bay in berths)
        {
            if(bay.kind=="yacht"||bay.kind=="motorboat"||bay.kind=="jetski")
            {
                string file=bay.kind=="yacht"?"yacht":bay.kind=="motorboat"?"motorboat":
                    (++skisPlaced%2==1?"jetski_odd":"jetski_even");
                placed+=PlaceBoat(boats,bay,sources[file]);
            }
            float signY=(bay.kind=="cruise_ship"||bay.kind=="ferry")?.20f:
                        bay.kind=="jetski"?-1.31f:-1.00f;
            var access=new Vector3(bay.accessX*K,signY*K,bay.accessZ*K);
            string label=bay.kind=="cruise_ship"?"WYCIECZKOWIEC / STANOWISKO":
                         bay.kind=="ferry"?"PROM / TERMINAL":
                         bay.kind=="jetski"?"SKUTER":bay.kind=="motorboat"?"MOTORÓWKA":"JACHT";
            Label(details,bay.id+" numbering board",bay.id+" | "+label,
                  access+Vector3.up*.90f*K,.23f*K);
            // One bollard per berth, set back from the boarding point.
            var berthSide=bay.kind=="cruise_ship"||bay.kind=="ferry"?2.1f:1.65f;
            var bollardAt=access+new Vector3(berthSide*K,-.03f*K,0);
            Primitive(details,bay.id+" real mooring bollard",PrimitiveType.Cylinder,
                      bollardAt+Vector3.up*.23f*K,
                      new Vector3(.28f,.45f,.28f)*K,metal,true);
            Primitive(details,bay.id+" rubber bumper",PrimitiveType.Cube,
                      bollardAt+Vector3.down*.18f*K,
                      new Vector3(.45f,.28f,.38f)*K,rubber,false);
            if(bay.kind=="yacht"||bay.kind=="motorboat")
                Primitive(details,bay.id+" electric and fresh-water shore pedestal",
                          PrimitiveType.Cube,access+new Vector3(-1.4f*K,.40f*K,.35f*K),
                          new Vector3(.36f,.80f,.36f)*K,power,true);
        }
        // Fenders every ~24m along the high passenger ship quay.
        for(int z=-445;z<=-210;z+=24)
            Primitive(details,"Cruise quay commercial fender z"+z,PrimitiveType.Cube,
                new Vector3(1117*K,-.38f*K,z*K),
                new Vector3(.45f,1.20f,2.6f)*K,rubber,true);
        // Harbour safety is separate from the untouched promenade.
        foreach(float z in new[]{-219f,-350f,-449f})
            Primitive(details,"Cruise quay rescue station "+z,PrimitiveType.Cylinder,
                new Vector3(1112*K,.7f*K,z*K),
                new Vector3(.36f,1.1f,.36f)*K,redFlag,true);
        Label(details,"Ferry terminal direction",
              "BRAXHORN | TERMINAL PROMOWY | F01",
              new Vector3(749*K,2.3f*K,-180*K),.32f*K);
        Label(details,"Cruise passenger terminal",
              "BRAXHORN | PORT WYCIECZKOWCÓW | W01 / W02",
              new Vector3(1112*K,2.3f*K,-183*K),.27f*K);
        Label(details,"Main marina pier name",
              "BRAXHORN MARINA | J01–J15",
              new Vector3(830*K,.62f*K,-170*K),.32f*K);
        Label(details,"Motorboat U marina",
              "BRAXHORN MOTOR | L01–L10",
              new Vector3(955*K,.62f*K,-174*K),.28f*K);
        Label(details,"Jetski pontoon",
              "BRAXHORN WATERSPORTS | S01–S10",
              new Vector3(1055*K,.62f*K,-169*K),.28f*K);
        // Marine safety ropes and rails follow the exact NEW dock layout.
        // Openings at every yacht/motorboat/jetski finger or boarding gangway;
        // no rail or collision across an approved boarding entrance.
        BraxChampionsHarbourSafety.Generate(root,piers);
        AssetDatabase.SaveAssets();
        Debug.Log("CHAMPIONS_PORT_ORIGINAL_FLEET_35 yacht=15 motorboat=10 jetski=10 "
            +"ferryBerths=1 cruiseBerths=2 generatedMeshes=false fullBoatModels=owner_library originalGLBs");
        return placed;
    }
}
