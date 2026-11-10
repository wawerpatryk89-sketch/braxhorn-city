using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxChampionsInfrastructure
{
    public const string Out="Assets/PlayableGenerated/Mistrzowie";
    static float K=>BraxClaudeOsiedle.K;
    [Serializable] public sealed class Station { public string id,name,axis; public float x,z,entryX,entryZ; }
    [Serializable] public sealed class Label { public string id,text; public float x,y,z,yaw,size; }
    [Serializable] public sealed class Lamp { public float x,y,z; }
    [Serializable] public sealed class Counts { public int metroStations,metroEntrances,metroTracks,tramTracks,tramPlatforms,busPlatforms,crossings,curbstones; }
    [Serializable] public sealed class Data
    {
        public string version;public float scaleK;public BraxChampionsFoundation.MeshData[] meshes;
        public Station[] metroStations;public Label[] labels;public Lamp[] metroLights;public Counts counts;
    }
    public static readonly List<Transform> Stations=new List<Transform>();
    public static Data Generate()
    {
        var d=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/Source/Mistrzowie/Infrastructure.json"));
        if(d==null||Mathf.Abs(d.scaleK-K)>.00001f||d.metroStations.Length!=5 || d.metroStations.Select(s=>s.id).Distinct().Count()!=5 || d.metroStations.Any(s=>s.axis!="x"&&s.axis!="z"))throw new InvalidOperationException("Invalid infrastructure layout/scale");
        Directory.CreateDirectory(Out+"/InfrastructureMeshes");Directory.CreateDirectory(Out+"/Materials");
        var materials=Materials();var root=new GameObject("Mistrzowie | streets and transit").transform;
        foreach(var m in d.meshes)
        {
            if(!materials.TryGetValue(m.role,out var material))throw new InvalidOperationException("Unmapped material: "+m.role);
            if(m.positions.Length%3!=0||m.uv.Length!=m.positions.Length/3*2||m.triangles.Any(i=>i<0||i>=m.positions.Length/3))throw new InvalidOperationException("Invalid mesh "+m.id);
            var v=new Vector3[m.positions.Length/3];var uv=new Vector2[v.Length];
            for(int i=0;i<v.Length;i++){v[i]=new Vector3(m.positions[i*3],m.positions[i*3+1],m.positions[i*3+2]);uv[i]=new Vector2(m.uv[i*2],m.uv[i*2+1]);}
            var mesh=new Mesh{name=m.id,indexFormat=v.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=m.triangles;
            if(m.role.StartsWith("metro_"))mesh.SetUVs(3,Enumerable.Repeat(new Vector4(0,1,1.5f,2718),v.Length).ToList());
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Out+"/InfrastructureMeshes/"+m.id+".asset");
            var o=new GameObject(m.id);o.transform.SetParent(root,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=material;
            if(m.role!="paint"&&m.role!="tactile"&&m.role!="rail_web")o.AddComponent<MeshCollider>().sharedMesh=mesh;
            GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic);
            if(m.role.StartsWith("metro_"))
            {
                var cull=o.AddComponent<BraxWidocznoscWnetrza>();cull.nazwa="Metro connection "+m.id;
                var b=r.bounds;b.Expand(new Vector3(140f,4f,140f));cull.strefy=new[]{b};
            }
        }
        if(d.metroLights==null||d.metroLights.Length==0)throw new InvalidOperationException("Tunnel lighting layout missing");
        foreach(var at in d.metroLights)
        {
            var o=new GameObject("Metro tunnel maintenance light");o.transform.SetParent(root,false);o.transform.position=new Vector3(at.x,at.y,at.z)*K;
            var l=o.AddComponent<Light>();l.type=LightType.Point;l.range=11f;l.intensity=.55f;l.color=new Color(.93f,.94f,1f);
            l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForceVertex;
            var visibility=o.AddComponent<BraxWidocznoscWnetrza>();visibility.nazwa="Tunnel lighting";
            visibility.strefy=new[]{new Bounds(o.transform.position,new Vector3(80,20,80))};
        }
        // stop names and arena wayfinding: solid 3D letters on their navy carriers (owner 10.11 "zakaz tanich napisów")
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        var letters=new BraxChampionsMeshBatch();
        foreach(var l in d.labels)
        {
            float yaw=l.yaw+180f,cap=.62f*l.size;                                  // TextMesh read from -z at yaw 0, the kit from +z
            var facing=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            var at=new Vector3(l.x,l.y-cap*.5f,l.z)-facing*.004f;                   // 2 mm proud of the carrier face
            BraxChampionsLetters3D.Text(letters,BraxChampionsHarbour.SignFace,BraxChampionsHarbour.Graphite,l.text,at,yaw,cap,.025f,.12f,4.4f);
        }
        letters.Build(root,"Transit names | 3D letters",Out+"/InfrastructureMeshes");
        var template=BraxClaudeMetro.ChampionsStationTemplate();Stations.Clear();
        for(int i=0;i<d.metroStations.Length;i++)
        {
            var s=d.metroStations[i];var o=UnityEngine.Object.Instantiate(template);o.name=s.id+" | "+s.name+" | reused city metro";
            o.transform.SetParent(root,false);if(i==0)BraxClaudeMetro.ChampionsParkedTrain(o.transform);var q=Quaternion.Euler(0,s.axis=="x"?0:-90,0);
            var shift=new Vector3(s.x*K,.16f*K-.15f,s.z*K)-q*new Vector3(22.5f,0,28.5f);
            o.transform.SetPositionAndRotation(shift,q);o.transform.localScale=Vector3.one;
            foreach(var drive in o.GetComponentsInChildren<BraxSchodyRuchomeJazda>(true)){drive.gora=q*drive.gora+shift;drive.dol=q*drive.dol+shift;}
            foreach(var seat in o.GetComponentsInChildren<BraxSeat>(true))seat.surfaceY+=shift.y;
            Restyle(o,s.id,i);Stations.Add(o.transform);
            var underground=o.transform.Find("Existing station and open tunnels");
            var visibility=underground.gameObject.AddComponent<BraxWidocznoscWnetrza>();visibility.nazwa=s.id+" Miasto Mistrzów";
            visibility.strefy=new[]{WorldBounds(o.transform,new Vector3(-75,-16,14),new Vector3(117,-.55f,43)),
                WorldBounds(o.transform,new Vector3(56,-.55f,8),new Vector3(90,14,48))};
            var range=underground.gameObject.AddComponent<BraxClaudeZasiegWnetrza>();range.zasieg=160f;
            range.sale=new[]{WorldBounds(o.transform,new Vector3(-75,-16,14),new Vector3(117,-1.2f,43))};
        }
        UnityEngine.Object.DestroyImmediate(template);
        PersistTransientAssets(root);AssetDatabase.SaveAssets();return d;
    }
    static Dictionary<string,Material> Materials()
    {
        var paving=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/paving.mat");
        if(!paving)throw new InvalidOperationException("Generate the foundation first");
        var concrete=BraxChampionsFoundation.SourceMaterial("infrastructure_concrete","concrete_floor_02",2.4f,Color.white);
        var stone=BraxChampionsFoundation.SourceMaterial("infrastructure_kerb","granite_tile",.9f,new Color(.78f,.79f,.80f));
        var wood=BraxChampionsFoundation.SourceMaterial("infrastructure_bench","teak_veneer",.8f,new Color(.66f,.53f,.39f));
        var steel=Surface("steel",new Color(.56f,.58f,.61f),.30f,.9f);
        var rail=Surface("rail",new Color(.45f,.47f,.50f),.30f,.9f);
        var glass=Surface("glass",new Color(.05f,.07f,.08f,.34f),.08f,0);
        glass.SetFloat("_Transparent",1);glass.SetFloat("_SrcBlend",(float)BlendMode.One);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.renderQueue=3000;glass.SetOverrideTag("RenderType","Transparent");
        var yellow=Surface("tactile",new Color(.85f,.68f,.12f),.7f,0);
        return new Dictionary<string,Material>{{"paving",paving},{"curb",stone},{"paint",Surface("paint",new Color(.88f,.88f,.84f),.7f,0)},
            {"tactile",yellow},{"steel",steel},{"rail",rail},{"rail_web",rail},{"glass",glass},{"bench",wood},{"sign",Surface("sign",new Color(.04f,.07f,.12f),.45f,.2f)},
            {"metro_rail",rail},{"metro_concrete",concrete},{"metro_third",yellow},{"metro_steel",steel},{"metro_light",TunnelLight()}};
    }
    static Material Surface(string id,Color color,float roughness,float metal)
    {
        var m=new Material(Shader.Find(BraxCityMaterials.ShaderName)){name="Mistrzowie "+id,enableInstancing=true};
        m.SetColor("_Color",color);m.SetFloat("_Roughness",roughness);m.SetFloat("_Metallic",metal);m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);AssetDatabase.CreateAsset(m,Out+"/Materials/"+id+".mat");return m;
    }
    static Material TunnelLight()
    {
        var m=Surface("metro_light",new Color(.92f,.91f,.83f),.4f,0);
        if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",new Color(.42f,.43f,.37f));
        return m;
    }
    static Bounds WorldBounds(Transform t,Vector3 a,Vector3 b)
    {
        var bounds=new Bounds(t.TransformPoint(a),Vector3.zero);
        foreach(float x in new[]{a.x,b.x})foreach(float y in new[]{a.y,b.y})foreach(float z in new[]{a.z,b.z})
            bounds.Encapsulate(t.TransformPoint(new Vector3(x,y,z)));
        return bounds;
    }
    static void Restyle(GameObject station,string id,int index)
    {
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/Mistrzowie/metro_"+id+".png");
        if(!atlas)throw new FileNotFoundException("Missing station-specific atlas: "+id);
        // Five visibly distinct, realistic interior finishes, NOT five identical
        // tunnels with only a different name. Reuse the actual city prefab and its
        // approved PBR maps; tint, tiling, roughness and trim are per-station
        // materials. Keep yellow warning strips, emergency signs and glass intact.
        var wallColors=new[]{
            new Color(.91f,.91f,.90f), // M1: silver granite terminal
            new Color(.53f,.62f,.68f), // M2: blue-grey financial centre
            new Color(.93f,.84f,.69f), // M3: champagne stone arena/hotels
            new Color(.59f,.72f,.64f), // M4: green-grey sports district
            new Color(.73f,.67f,.77f)  // M5: graphite-violet retail district
        };
        var floorColors=new[]{
            new Color(.87f,.88f,.88f),
            new Color(.66f,.72f,.77f),
            new Color(.94f,.85f,.73f),
            new Color(.64f,.70f,.66f),
            new Color(.75f,.72f,.79f)
        };
        var trimColors=new[]{
            new Color(.30f,.32f,.34f),
            new Color(.22f,.34f,.43f),
            new Color(.50f,.38f,.25f),
            new Color(.24f,.38f,.32f),
            new Color(.39f,.31f,.46f)
        };
        if(index<0||index>=5)throw new InvalidOperationException("Unknown metro station finish: "+id);
        var changed=new Dictionary<Material,Material>();
        foreach(var r in station.GetComponentsInChildren<Renderer>(true))
        {
            var array=r.sharedMaterials;
            for(int j=0;j<array.Length;j++)
            {
                var old=array[j];if(!old)continue;
                string name=old.name.ToLowerInvariant();
                bool signage=name.Contains("oznakowanie")&&name.Contains("atlas");
                bool wall=name=="mistrzowie metro_wall";
                bool floor=name=="mistrzowie metro_floor"||name=="mistrzowie metro_edge";
                bool skirting=name=="mistrzowie metro_skirting";
                bool stairs=name=="mistrzowie metro_stairs";
                bool ceiling=name.Contains("metro sufit")||name.Contains("metro belki");
                if(!signage&&!wall&&!floor&&!skirting&&!stairs&&!ceiling)continue;
                if(!changed.TryGetValue(old,out var m))
                {
                    m=new Material(old){name=id+" "+old.name,enableInstancing=true};
                    if(signage)
                    {
                        m.mainTexture=atlas;
                        if(m.HasProperty("_EmissionMap"))m.SetTexture("_EmissionMap",atlas);
                    }
                    else
                    {
                        Color tint=wall?wallColors[index]:
                                   (skirting||ceiling)?trimColors[index]:floorColors[index];
                        if(m.HasProperty("_Color"))m.SetColor("_Color",tint);
                        if(m.HasProperty("_Roughness"))
                            m.SetFloat("_Roughness",wall ? .42f : skirting ? .30f : ceiling ? .40f : .62f);
                        if(wall)
                        {
                            var tiling=new Vector2(new[]{.40f,.31f,.46f,.34f,.43f}[index],
                                                   new[]{.40f,.31f,.46f,.34f,.43f}[index]);
                            foreach(string prop in new[]{"_MainTex","_BumpMap","_ARM"})
                                if(m.HasProperty(prop))m.SetTextureScale(prop,tiling);
                        }
                    }
                    AssetDatabase.CreateAsset(m,Out+"/Materials/"+id+"_variant_"+changed.Count+".mat");
                    changed.Add(old,m);
                }
                array[j]=m;
            }
            r.sharedMaterials=array;
        }
        // Maps, name boards and platform destinations use each station's
        // own 1024x1024 metro atlas with unchanged original sign UVs.
    }
    static void PersistTransientAssets(Transform root)
    {
        Directory.CreateDirectory(Out+"/MetroMeshes");int serial=0;
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            if(f.sharedMesh&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh)))
                AssetDatabase.CreateAsset(f.sharedMesh,Out+"/MetroMeshes/mesh_"+(serial++).ToString("D4")+".asset");
    }
}
