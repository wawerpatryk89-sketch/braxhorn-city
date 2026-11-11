using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Only called AFTER foundation and complete metro checks pass. Existing regions are untouched.
public static class BraxChampionsBuildings
{
    const string Source="Assets/Source/Mistrzowie/",Out="Assets/PlayableGenerated/Mistrzowie/Buildings";
    static float K=>BraxClaudeOsiedle.K;
    [Serializable] sealed class ExteriorData { public float scaleK;public Exterior[] buildings; }
    [Serializable] sealed class Exterior
    {
        public string id,name,sign;public float x,z,height,signYaw;public float[] signAnchor;
        public BraxChampionsFoundation.MeshData[] lod0,lod1,collision;
    }
    [Serializable] sealed class SignData { public float scaleK;public Sign[] signs; }
    [Serializable] sealed class Sign
    {
        public string id;public bool needsSculptedEmblem;public float[] emblemAnchor;
        public BraxChampionsFoundation.MeshData[] meshes;
    }
    static T Read<T>(string file)
    {
        using(var f=File.OpenRead(Source+file))using(var zip=new GZipStream(f,CompressionMode.Decompress))
        using(var r=new StreamReader(zip))return JsonUtility.FromJson<T>(r.ReadToEnd());
    }
    public static int Generate()
    {
        if(Directory.Exists(Out))throw new InvalidOperationException("Existing building assets must be updated without replacing GUIDs or manual edits");
        var d=Read<ExteriorData>("Exteriors.bytes");var signs=Read<SignData>("Signs.bytes");
        if(d==null||signs==null||d.buildings.Length!=2||Mathf.Abs(d.scaleK-K)>.00001f||Mathf.Abs(signs.scaleK-K)>.00001f)
            throw new InvalidOperationException("Exterior source or shared scale invalid");
        Directory.CreateDirectory(Out+"/Meshes");Directory.CreateDirectory(Out+"/Materials");Directory.CreateDirectory(Out+"/Prefabs");
        var materials=Materials();
        foreach(var b in d.buildings)
        {
            var root=new GameObject(b.id+" | "+b.name+" | exterior").transform;root.position=new Vector3(b.x,0,b.z)*K;
            var levels=new List<LOD>();
            foreach(var entry in new[]{b.lod0,b.lod1})
            {
                int level=levels.Count;var child=new GameObject("LOD"+level).transform;child.SetParent(root,false);
                var renderers=new List<Renderer>();
                foreach(var m in entry)renderers.Add(MeshObject(child,b.id+"_LOD"+level+"_"+m.id,m,materials[m.role],false));
                // Owner 10.11 00:43 "pojawianie się budynków popraw, strasznie się pokazuje": the detailed shell was swapped
                // for the low one below 18% of screen height (40% in the low graphics mode, lodBias .45) = a visible
                // jump while standing next to the arena / hotel. LOD1 has almost the same triangle count, so the
                // detailed shell now stays until the building is a speck.
                levels.Add(new LOD(level==0?.012f:.0001f,renderers.ToArray()));
            }
            var group=root.gameObject.AddComponent<LODGroup>();group.SetLODs(levels.ToArray());group.RecalculateBounds();
            var collisions=new GameObject("Permanent exterior collision, independent of LOD").transform;collisions.SetParent(root,false);
            // Collision uses the structural low-detail surfaces; no water collision or invented interior.
            foreach(var m in b.collision)
            {
                var o=new GameObject(m.id);o.transform.SetParent(collisions,false);
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Out+"/Meshes/"+b.id+"_LOD1_"+m.id+".asset");
                if(!saved)throw new InvalidOperationException("Missing structural collision mesh: "+m.id);
                o.AddComponent<MeshCollider>().sharedMesh=saved;
            }
            // Owner's sign artwork supersedes the original default A1 front lettering.
            // If the source PNGs have not been imported, keep the old 3D lettering.
            bool originalArenaGraphics=b.id=="A1"&&BraxChampionsArenaSigns.HasSources();
            if(!originalArenaGraphics)
            {
                var sign=signs.signs.Single(s=>s.id==b.sign);
                var sr=new GameObject(b.sign+" | original generated 3D lettering").transform;
                sr.SetParent(root,false);
                sr.localPosition=new Vector3(b.signAnchor[0],b.signAnchor[1],b.signAnchor[2])*K;
                sr.localRotation=Quaternion.Euler(0,b.signYaw,0);
                foreach(var m in sign.meshes)MeshObject(sr,b.id+"_sign_"+m.id,m,materials[m.role],false);
                if(sign.needsSculptedEmblem)
                {
                    var anchor=new GameObject("SOURCE_REQUIRED | approved BRAXHORN sculpted bull").transform;
                    anchor.SetParent(sr,false);
                    anchor.localPosition=new Vector3(sign.emblemAnchor[0],sign.emblemAnchor[1],sign.emblemAnchor[2]);
                }
            }
            // Save local-space reusable prefab, then restore the approved world plot position.
            var at=root.position;root.position=Vector3.zero;
            if(b.id=="A1")
            {
                BraxChampionsArena.Generate(root);
                BraxChampionsArenaArrival.Generate(root, materials);
                BraxChampionsArenaSigns.Generate(root, materials);
                // owner 10.11 19:14: the BRAXHORN bull mark on the roof, seen from the hotel and from above
                BraxChampionsFoundation.Optional("byk BRAXHORN na dachu areny",()=>BraxChampionsArenaRoofBull.Generate(root));
            }
            var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Out+"/Prefabs/"+b.id+"_Exterior.prefab",InteractionMode.AutomatedAction);
            root.position=at;if(!prefab)throw new IOException("Exterior prefab save failed: "+b.id);
        }
        // Approved T11/WAWER plot at (495,465) is a third independent exterior.
        // It must NOT change the existing A1 or T01 meshes, prefabs or positions.
        // Kasyno i basen nigdy nie przeszły pełnego Unity w tej gałęzi: błąd = ostrzeżenie
        // z pełnym wyjątkiem w logu, reszta miasta i APK budują się dalej.
        bool casinoGenerated=false;
        BraxChampionsFoundation.Optional("kasyno T11",()=>
        {
            BraxChampionsCasinoT11.Generate(Out);
            casinoGenerated=true;
        });
        // Owner 10.11 19:18-20:12 (board "E1 CENTRUM ROZRYWKI", "rób od podstaw E1 dokładnie, parking podziemny", "na
        // taksówki przed E1"): the entertainment centre by the promenade with its B1 garage, taxi lay-by and site.
        bool e1Generated=false;
        BraxChampionsFoundation.Optional("E1 centrum rozrywki",()=>
        {
            BraxChampionsE1.Generate(Out);
            e1Generated=true;
        });
        // Owner 11.11 12:56-13:09 (board "D2 DWORZEC KOLEJOWO-AUTOBUSOWY", the railway out of the city on a viaduct and into
        // a tunnel): the station, its platforms, the bus terminal and the forecourt.
        bool d2Generated=false;
        BraxChampionsFoundation.Optional("D2 dworzec kolejowo-autobusowy",()=>
        {
            BraxChampionsD2.Generate(Out);
            d2Generated=true;
        });
        // Owner 11.11 16:33-16:36 (board "G1 GALERIA GŁÓWNA + KINO", "zrób galerię na gotowo ... zadbaj o detale wszystkie",
        // "kino ... żeby leciały nasze filmy ... recepcja ... wybór repertuarów"): the mall, the cinema, the square, the car
        // parks, the deliveries.
        bool g1Generated=false;
        BraxChampionsFoundation.Optional("G1 galeria główna + kino",()=>
        {
            BraxChampionsD2.GenerateG1(Out);
            g1Generated=true;
        });
        // 9.11 owner: "Ten basen usuń całkowicie" - P1 removed from the game.
        // 9.11 owner: apartment buildings on the rental plots, one at a time,
        // each finished A-Z inside and out (empty, no furniture).
        int apartmentBuildings=0;
        foreach(string apt in BraxChampionsApartments.Ids)
            BraxChampionsFoundation.Optional("apartamentowiec "+apt,()=>
            {
                BraxChampionsApartments.Generate(apt,materials,Out);
                apartmentBuildings++;
            });
        AssetDatabase.SaveAssets();
        int generated=d.buildings.Length+(casinoGenerated?1:0)+apartmentBuildings+(e1Generated?1:0)+(d2Generated?1:0)+(g1Generated?1:0);
        Debug.Log("CHAMPIONS_BUILDING_GENERATION base="+d.buildings.Length+
            " T11="+casinoGenerated+" E1="+e1Generated+" D2="+d2Generated+" G1="+g1Generated+" APARTMENTS="+apartmentBuildings+"/"+BraxChampionsApartments.Ids.Length+" P1=REMOVED_BY_OWNER"+
            " generated="+generated+" visualAccepted=false");
        return generated;
    }
    static Renderer MeshObject(Transform parent,string id,BraxChampionsFoundation.MeshData d,Material material,bool collision)
    {
        int count=d.positions.Length/3;
        if(d.positions.Length%3!=0||d.uv.Length!=count*2||d.triangles.Any(i=>i<0||i>=count))throw new InvalidOperationException("Invalid exterior mesh: "+id);
        var v=new Vector3[count];var uv=new Vector2[count];
        for(int i=0;i<count;i++){v[i]=new Vector3(d.positions[3*i],d.positions[3*i+1],d.positions[3*i+2]);uv[i]=new Vector2(d.uv[2*i],d.uv[2*i+1]);}
        var mesh=new Mesh{name=id,indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.vertices=v;mesh.uv=uv;mesh.triangles=d.triangles;
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Out+"/Meshes/"+id+".asset");
        var o=new GameObject(id);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        if(collision)o.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic);return r;
    }
    static Dictionary<string,Material> Materials()
    {
        var bronze=BraxChampionsFoundation.LibraryMetal("exterior_bronze",new Color(.58f,.42f,.22f));
        var gold=BraxChampionsGold.Apply(BraxChampionsFoundation.LibraryMetal("sign_gold",new Color(.95f,.72f,.30f)));
        // ISSUE #27 dedicated arena finish materials: do not reuse glass/stone as fake metal.
        var arenaGold=BraxChampionsGold.Apply(Surface("arena_premium_gold",new Color(.95f,.70f,.26f),.16f,.96f));
        var arenaBlack=Surface("arena_premium_black_metal",new Color(.045f,.050f,.060f),.23f,.92f);
        var steel=BraxChampionsFoundation.LibraryMetal("exterior_steel",new Color(.72f,.74f,.76f));
        var stone=BraxChampionsFoundation.SourceMaterial("exterior_stone","rectangular_facade_tiles",2.4f,new Color(.91f,.87f,.79f));
        var dark=BraxChampionsFoundation.SourceMaterial("exterior_dark_stone","granite_tile_02",1.2f,new Color(.30f,.32f,.34f));
        var roof=BraxChampionsFoundation.LibraryMetal("exterior_roof",new Color(.30f,.32f,.34f));
        var teak=BraxChampionsFoundation.SourceMaterial("exterior_teak","teak_veneer",1.8f,Color.white);
        var tile=BraxChampionsFoundation.SourceMaterial("pool_tile","granite_tile",.4f,new Color(.75f,.85f,.84f));
        // The shader preserves alpha only with _Transparent=1 and SrcAlpha blending.
        // The old facade_glass was OPAQUE: its dark RGB looked like black plastic from outside.
        // Use transparent dielectrics (metallic 0), never milky/white glass.
        // Owner reference 7.11 + phone proof 8.11: clearly graphite-smoked glazing,
        // but transparent enough to keep the lobby/VIP lounge readable through the facade.
        // Both A1 levels: lower .18..10.75 and upper 10.75..27.15 m
        // share graphite-smoked glossy dielectric glass. The hotel T01 may
        // share this finish as explicitly permitted by the owner 8 Nov.
        // Public and athlete door leaves use separate clear_glass below.
        var glass=Surface("facade_glass",new Color(.18f,.20f,.23f,.48f),.075f,0f);
        glass.SetFloat("_ReflectionStrength",.62f);
        ConfigureGlass(glass);
        var clear=Surface("clear_glass",new Color(.78f,.88f,.93f,.19f),.10f,0f);
        ConfigureGlass(clear);
        var spandrel=Surface("spandrel",new Color(.07f,.10f,.13f),.32f,.55f);
        var ivory=Surface("ivory",new Color(.91f,.88f,.80f),.7f,0);
        var light=Surface("light",new Color(.91f,.78f,.52f),.4f,0);light.SetColor("_EmissionColor",new Color(.45f,.31f,.14f));
        var sea=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/sea.mat");
        if(!sea)throw new InvalidOperationException("Existing water shader must be generated by foundation first");
        var water=new Material(sea){name="Mistrzowie rooftop pool"};
        water.SetColor("_DeepColor",new Color(.045f,.27f,.31f));water.SetColor("_EdgeColor",new Color(.08f,.38f,.40f));
        water.SetFloat("_SwellStrength",0);water.SetFloat("_RippleStrength",.18f);water.SetFloat("_ReflectionMip",1.8f);
        AssetDatabase.CreateAsset(water,Out+"/Materials/pool_water.mat");
        return new Dictionary<string,Material>{{"stone",stone},{"dark_stone",dark},{"bronze",bronze},{"steel",steel},{"roof",roof},
            {"teak",teak},{"pool_tile",tile},{"pool_water",water},{"facade_glass",glass},{"clear_glass",clear},{"spandrel",spandrel},
            {"ivory",ivory},{"gold",gold},{"arena_gold",arenaGold},{"arena_black",arenaBlack},
            {"dark",bronze},{"accent",gold},{"light",light}};
    }
    static void ConfigureGlass(Material m)
    {
        m.SetFloat("_Transparent",1);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite",0);
        m.SetFloat("_Cull",2);
        m.renderQueue=3000;
        m.SetOverrideTag("RenderType","Transparent");
    }
    static Material Surface(string id,Color color,float roughness,float metallic)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);if(!shader)throw new InvalidOperationException("Existing city shader unavailable");
        var m=new Material(shader){name="Mistrzowie exterior "+id,enableInstancing=true};
        m.SetColor("_Color",color);m.SetFloat("_Roughness",roughness);m.SetFloat("_Metallic",metallic);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);AssetDatabase.CreateAsset(m,Out+"/Materials/"+id+".mat");return m;
    }
}
