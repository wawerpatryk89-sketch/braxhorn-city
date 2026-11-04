using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Authored source LOD meshes + source-rendered far views. No mesh decimation.
// Camp-only generated assets; does not run a build or change the city/estate.
public static class BraxObozVegetation
{
    const float K = 1.294f;
    const string Source = "Assets/Source/Prywatne/Oboz/environment/vegetation/";
    const string Output = "Assets/PlayableGenerated/Oboz/Vegetation/";
    [Serializable] public sealed class Item { public float x, z, yaw, scale; public int prototype; public bool solid; }
    [Serializable] public sealed class Layout { public int schema; public float worldScale; public string[] prototypes; public Item[] trees, grass; }
    [Serializable] public sealed class Atlas { public string model, file; public float span, centerY; }
    [Serializable] public sealed class Atlases { public bool complete; public Atlas[] billboards; }
    static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();

    public static void Populate(Transform parent)
    {
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/Oboz/VegetationLayout.json"));
        var atlas = JsonUtility.FromJson<Atlases>(File.ReadAllText(Source + "billboards/metadata.json"));
        if (layout.schema != 1 || Mathf.Abs(layout.worldScale-K)>.00001f || !atlas.complete || atlas.billboards.Length != 6)
            throw new InvalidOperationException("Niepełne dane roślinności obozu");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(); materials.Clear();
        var forest = new GameObject("Las — źródłowe LOD, układ do odbioru").transform; forest.SetParent(parent,false);
        var prototypes = new GameObject[layout.prototypes.Length];
        try
        {
            for (int i=0;i<prototypes.Length;i++)
                prototypes[i]=Prototype(layout.prototypes[i],atlas.billboards.Single(a=>a.model==layout.prototypes[i]+"_LOD2.glb"));
            foreach (var point in layout.trees)
            {
                if (point.prototype<0 || point.prototype>=prototypes.Length) throw new InvalidOperationException("Błędny wariant drzewa");
                var tree=Object.Instantiate(prototypes[point.prototype],forest);
                tree.name=layout.prototypes[point.prototype]; tree.transform.localPosition=new Vector3(point.x,0,point.z)*K;
                tree.transform.localRotation=Quaternion.Euler(0,point.yaw,0);tree.transform.localScale=Vector3.one*(point.scale*K);
                if (point.solid)
                {
                    var trunk=tree.AddComponent<CapsuleCollider>();trunk.direction=1;trunk.radius=point.prototype<3?.20f:.085f;
                    trunk.height=3;trunk.center=new Vector3(0,1.5f,0);
                }
            }
            Grass(parent,layout.grass);
            Debug.Log("BRAXHORN_OBOZ las: drzewa="+layout.trees.Length+" kępy="+layout.grass.Length+"; test telefonu nadal wymagany");
        }
        finally { foreach(var p in prototypes)if(p)Object.DestroyImmediate(p); }
    }
    static GameObject Prototype(string name,Atlas atlas)
    {
        var root=new GameObject(name);
        try
        {
            var near=Model(Source+name+"_LOD1__unity.glb",root.transform);
            var mid=Model(Source+name+"_LOD2__unity.glb",root.transform);
            var far=new GameObject("Daleki widok oryginalnego drzewa");far.transform.SetParent(root.transform,false);
            float h=atlas.span*.5f;var mesh=new Mesh{name=name+" — billboard"};
            mesh.vertices=new[]{new Vector3(-h,atlas.centerY-h,0),new Vector3(h,atlas.centerY-h,0),new Vector3(-h,atlas.centerY+h,0),new Vector3(h,atlas.centerY+h,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();
            mesh.bounds=new Bounds(new Vector3(0,atlas.centerY,0),Vector3.one*atlas.span);
            far.AddComponent<MeshFilter>().sharedMesh=Save(mesh,Output+name+"-billboard.asset");
            var shader=Shader.Find("Braxhorn/ObozTreeBillboard");if(!shader)throw new InvalidOperationException("Brak shadera dalekiego lasu");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"billboards/"+atlas.file);if(!texture)throw new InvalidOperationException("Brak atlasu "+atlas.file);
            var mat=new Material(shader){name=name+" — widoki źródła",enableInstancing=true};mat.mainTexture=texture;
            var renderer=far.AddComponent<MeshRenderer>();renderer.sharedMaterial=Save(mat,Output+name+"-billboard.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var lod=root.AddComponent<LODGroup>();
            // Large detailed source trees are kept close. Far views retain
            // silhouettes without drawing the heavy source canopy everywhere.
            lod.SetLODs(new[]{new LOD(.85f,near.GetComponentsInChildren<Renderer>()),new LOD(.38f,mid.GetComponentsInChildren<Renderer>()),new LOD(.018f,new Renderer[]{renderer})});
            lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();return root;
        }
        catch { Object.DestroyImmediate(root);throw; }
    }
    static GameObject Model(string path,Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new InvalidOperationException("Brak zaimportowanego modelu "+path);
        var obj=Object.Instantiate(asset,parent,false);
        foreach(var r in obj.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(Material).ToArray();r.shadowCastingMode=ShadowCastingMode.On;
        }
        return obj;
    }
    static Material Material(Material source)
    {
        if (!source) throw new InvalidOperationException("Brak materiału roślinności");
        if(materials.TryGetValue(source,out var saved))return saved;
        if(!source.HasProperty("baseColorFactor"))throw new InvalidOperationException("Nieobsługiwany materiał źródła "+source.name);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);if(!shader)throw new InvalidOperationException("Brak wspólnego shadera PBR");
        var mat=new Material(shader){name=source.name+" — obóz",enableInstancing=true};
        mat.SetColor("_Color",source.GetColor("baseColorFactor"));mat.SetFloat("_Roughness",source.GetFloat("roughnessFactor"));mat.SetFloat("_Metallic",source.GetFloat("metallicFactor"));
        CopyMap(source,mat,"baseColorTexture","_MainTex");CopyMap(source,mat,"normalTexture","_BumpMap");CopyMap(source,mat,"metallicRoughnessTexture","_ARM");
        mat.SetFloat("_NormalEncoding",0);mat.SetFloat("_UseARMForAO",0);mat.SetFloat("_BumpScale",1);
        // Foliage transparency becomes alpha test for depth sorting and mobile
        // overdraw, with source alpha intact. No texture colours are retinted.
        bool foliage=source.name.IndexOf("twig",StringComparison.OrdinalIgnoreCase)>=0;
        mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;mat.SetFloat("_AlphaClip",foliage?1:0);mat.SetFloat("_Cutoff",foliage?.35f:0);
        mat.SetFloat("_Transparent",0);mat.SetFloat("_SrcBlend",1);mat.SetFloat("_DstBlend",0);mat.SetFloat("_ZWrite",1);mat.SetColor("_EmissionColor",Color.black);
        mat.renderQueue=foliage?2450:2000;mat.SetOverrideTag("RenderType",foliage?"TransparentCutout":"Opaque");
        string hash=Hash128.Compute(AssetDatabase.GetAssetPath(source)+"|"+source.name).ToString();
        saved=Save(mat,Output+"Material-"+hash+".mat");materials.Add(source,saved);return saved;
    }
    static void CopyMap(Material src,Material dst,string from,string to)
    {
        if(!src.HasProperty(from)||!src.GetTexture(from))return;
        dst.SetTexture(to,src.GetTexture(from));dst.SetTextureScale(to,src.GetTextureScale(from));dst.SetTextureOffset(to,src.GetTextureOffset(from));
    }
    static T Save<T>(T obj,string path) where T:Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(obj,old);Object.DestroyImmediate(obj);return old;}
        AssetDatabase.CreateAsset(obj,path);return obj;
    }
    static void Grass(Transform parent,Item[] points)
    {
        var holder=new GameObject("Trawa — instancje oryginalnych źdźbeł");holder.transform.SetParent(parent,false);
        var source=Model(Source+"grass_bermuda_01_seedling_d__unity.glb",null);
        try
        {
            var filters=source.GetComponentsInChildren<MeshFilter>();
            if(filters.Length!=1||filters[0].sharedMesh.subMeshCount!=1)throw new InvalidOperationException("Niejednoznaczna kępa źródłowa");
            var filter=filters[0];var combine=new List<CombineInstance>();
            for(int i=0;i<4;i++)
            {
                float angle=i*90;var offset=Quaternion.Euler(0,angle,0)*new Vector3(.045f,0,.025f);
                combine.Add(new CombineInstance{mesh=filter.sharedMesh,transform=Matrix4x4.TRS(offset,Quaternion.Euler(0,angle,0),Vector3.one)*source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            }
            var mesh=new Mesh{name="Cztery oryginalne źdźbła"};mesh.CombineMeshes(combine.ToArray(),true,true);
            var data=ScriptableObject.CreateInstance<BraxClaudeTrawaDane>();data.wysokosc=K;data.granicaLaki=3*K;
            data.kepy=points.Select(p=>new Vector4(p.x*K,p.z*K,p.yaw,p.scale)).ToArray();data.y=points.Select(_=>.005f*K).ToArray();
            var grass=holder.AddComponent<BraxClaudeTrawa>();grass.dane=Save(data,Output+"GrassData.asset");grass.siatka=Save(mesh,Output+"GrassClump.asset");
            grass.material=filter.GetComponent<MeshRenderer>().sharedMaterial;grass.kawalek=9*K;grass.zasiegMurawy=25*K;grass.zasiegLaki=30*K;grass.zwolnijPo=45*K;
        }
        finally { Object.DestroyImmediate(source); }
    }
}
