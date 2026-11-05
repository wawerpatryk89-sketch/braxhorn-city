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
        BraxObozCampusLayout.Reload();
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
            int placedTrees=0;
            foreach (var point in layout.trees)
            {
                if (Mathf.Abs(point.x)>138f || Mathf.Abs(point.z)>138f) continue; // poza terenem 280x280
                if(BraxObozCampusLayout.Reserved(point.x,point.z,3))continue;
                if (point.prototype<0 || point.prototype>=prototypes.Length) throw new InvalidOperationException("Błędny wariant drzewa");
                var tree=Object.Instantiate(prototypes[point.prototype],forest);
                tree.name=layout.prototypes[point.prototype]; tree.transform.localPosition=new Vector3(point.x,0,point.z)*K;
                tree.transform.localRotation=Quaternion.Euler(0,point.yaw,0);tree.transform.localScale=Vector3.one*(point.scale*K);
                placedTrees++;
                if (point.solid)
                {
                    var trunk=tree.AddComponent<CapsuleCollider>();trunk.direction=1;trunk.radius=point.prototype<3?.20f:.085f;
                    trunk.height=3;trunk.center=new Vector3(0,1.5f,0);
                }
            }
            // Dodatkowy pas prawdziwych drzew 3D tuż ZA zaokrąglonym płotem.
            // Nie zmienia gotowych postaci i nie zasłania drogi/dojazdu.
            int fenceTrees=AddFenceWoodland(forest,prototypes);
            placedTrees+=fenceTrees;
            var grass=layout.grass.Where(p=>Mathf.Abs(p.x)<=138f&&Mathf.Abs(p.z)<=138f&&!BraxObozCampusLayout.Reserved(p.x,p.z,.5f)).ToList();
            int originalGrass=grass.Count;
            AddLawnGrass(grass);
            Grass(parent,grass.ToArray());
            Debug.Log("BRAXHORN_OBOZ las: drzewa="+placedTrees+" pas_zewnetrzny="+fenceTrees+" kępy="+grass.Count+
                " nowe_kepy_trawnika="+(grass.Count-originalGrass)+"; test telefonu nadal wymagany");
        }
        finally { foreach(var p in prototypes)if(p)Object.DestroyImmediate(p); }
    }
    // Istniejący layout lasu pochodził ze starego prostokąta. Po redukcji
    // działki tylko część oryginalnych pozycji leży blisko nowego płotu.
    // Ten deterministyczny pas używa WYŁĄCZNIE gotowych źródeł jodeł/sadzonek,
    // bez atrap billboardów w zbliżeniu i bez importowania nowych modeli.
    static int AddFenceWoodland(Transform forest,GameObject[] prototypes)
    {
        if(prototypes == null || prototypes.Length != 6)
            throw new InvalidOperationException("Brak sześciu źródłowych wariantów drzew obozu");
        const int ringSamples=160;
        int placed=0;
        for(int i=0;i<ringSamples;i++)
        {
            uint seed=unchecked((uint)(i+79)*2654435761u);
            seed^=seed>>16;seed=unchecked(seed*2246822519u);seed^=seed>>13;
            float a=(i+.17f)*Mathf.PI*2f/ringSamples;
            float offset=5f+(seed&15u)*.45f;
            float x=(BraxObozCampusLayout.PlotRadiusX+offset)*Mathf.Cos(a);
            float z=BraxObozCampusLayout.PlotCenterZ+
                    (BraxObozCampusLayout.PlotRadiusZ+offset)*Mathf.Sin(a);
            // Nie sadzić poza mesh terenu, na bramie i przy dojeździe.
            if(Mathf.Abs(x)>135f||Mathf.Abs(z)>135f)continue;
            if(z < -105f && Mathf.Abs(x)<23f)continue;
            if(BraxObozCampusLayout.Reserved(x,z,2f))continue;
            int prototypeIndex=(int)((seed>>16)%(uint)prototypes.Length);
            var tree=Object.Instantiate(prototypes[prototypeIndex],forest);
            tree.name=prototypes[prototypeIndex].name+" — przy ogrodzeniu";
            tree.transform.localPosition=new Vector3(x,0,z)*K;
            tree.transform.localRotation=Quaternion.Euler(0,(seed%359u),0);
            tree.transform.localScale=Vector3.one*((.74f+(seed%37u)*.012f)*K);
            // Za płotem nie dodajemy zbędnych colliderów co kilka metrów.
            // Minimalna liczba pni zachowuje podstawową fizykę dla dostępnego pasa.
            if(i%7==0)
            {
                var trunk=tree.AddComponent<CapsuleCollider>();
                trunk.direction=1;trunk.radius=prototypeIndex<3?.2f:.085f;
                trunk.height=3;trunk.center=new Vector3(0,1.5f,0);
            }
            placed++;
        }
        return placed;
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
            var mat=new Material(shader){name=name+" — widoki źródła",enableInstancing=true};mat.mainTexture=LeafTexture(texture,.35f,1);
            var renderer=far.AddComponent<MeshRenderer>();renderer.sharedMaterial=Save(mat,Output+name+"-billboard.mat");renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var lod=root.AddComponent<LODGroup>();
            // Large detailed source trees are kept close. Far views retain
            // silhouettes without drawing the heavy source canopy everywhere.
            // Keep real canopy geometry through the middle distance. The old
            // .38 threshold replaced even large nearby trees with flat cards.
            lod.SetLODs(new[]{new LOD(.40f,near.GetComponentsInChildren<Renderer>()),new LOD(.075f,mid.GetComponentsInChildren<Renderer>()),new LOD(.012f,new Renderer[]{renderer})});
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
        if(!source.HasProperty("baseColorFactor") || !source.HasProperty("roughnessFactor") || !source.HasProperty("metallicFactor"))
            throw new InvalidOperationException("Nieobsługiwany materiał źródła "+source.name);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);if(!shader)throw new InvalidOperationException("Brak wspólnego shadera PBR");
        var mat=new Material(shader){name=source.name+" — obóz",enableInstancing=true};
        mat.SetColor("_Color",source.GetColor("baseColorFactor"));mat.SetFloat("_Roughness",source.GetFloat("roughnessFactor"));mat.SetFloat("_Metallic",source.GetFloat("metallicFactor"));
        CopyMap(source,mat,"baseColorTexture","_MainTex");CopyMap(source,mat,"normalTexture","_BumpMap");CopyMap(source,mat,"metallicRoughnessTexture","_ARM");
        CopyMap(source,mat,"occlusionTexture","_OcclusionMap");
        mat.SetFloat("_NormalEncoding",0);mat.SetFloat("_UseARMForAO",0);
        mat.SetFloat("_BumpScale",source.HasProperty("normalTexture_scale")?source.GetFloat("normalTexture_scale"):1);
        mat.SetFloat("_OcclusionStrength",source.HasProperty("occlusionTexture_strength")?source.GetFloat("occlusionTexture_strength"):1);
        // Foliage transparency becomes alpha test for depth sorting and mobile
        // overdraw, with source alpha intact. No texture colours are retinted.
        string type=source.GetTag("RenderType",false,"");
        bool foliage=source.IsKeywordEnabled("_ALPHATEST_ON") || type=="TransparentCutout" ||
            type=="Transparent" || source.renderQueue>=3000 ||
            source.name.IndexOf("twig",StringComparison.OrdinalIgnoreCase)>=0 ||
            source.name.IndexOf("grass",StringComparison.OrdinalIgnoreCase)>=0;
        float cutoff=source.HasProperty("alphaCutoff")?source.GetFloat("alphaCutoff"):.35f;
        if(foliage)mat.SetTexture("_MainTex",LeafTexture(mat.GetTexture("_MainTex"),cutoff,mat.GetColor("_Color").a));
        mat.SetFloat("_Cull",foliage?0:2);mat.doubleSidedGI=foliage;mat.SetFloat("_AlphaClip",foliage?1:0);mat.SetFloat("_Cutoff",foliage?Mathf.Clamp01(cutoff):0);
        mat.SetFloat("_Transparent",0);mat.SetFloat("_SrcBlend",1);mat.SetFloat("_DstBlend",0);mat.SetFloat("_ZWrite",1);mat.SetColor("_EmissionColor",Color.black);
        mat.renderQueue=foliage?2450:2000;mat.SetOverrideTag("RenderType",foliage?"TransparentCutout":"Opaque");
        string hash=Hash128.Compute(AssetDatabase.GetAssetPath(source)+"|"+source.name).ToString();
        saved=Save(mat,Output+"Material-"+hash+".mat");materials.Add(source,saved);return saved;
    }
    static void CopyMap(Material src,Material dst,string from,string to)
    {
        if(!src.HasProperty(from)||!src.GetTexture(from))return;
        dst.SetTexture(to,src.GetTexture(from));dst.SetTextureScale(to,src.GetTextureScale(from));dst.SetTextureOffset(to,src.GetTextureOffset(from));
        if(src.HasProperty(from+"_Rotation"))dst.SetVector(to+"_Rotation",src.GetVector(from+"_Rotation"));
        if(src.HasProperty(from+"_texCoord"))
        {
            float uv=src.GetFloat(from+"_texCoord");
            if(uv!=0&&uv!=1)throw new InvalidOperationException("Nieobsługiwany kanał UV roślinności: "+src.name);
            dst.SetFloat(to+"_texCoord",uv);
        }
    }
    static Texture LeafTexture(Texture texture,float cutoff,float factorAlpha)
    {
        if(!texture)return texture;
        string source=AssetDatabase.GetAssetPath(texture);
        var sourceImporter=AssetImporter.GetAtPath(source) as TextureImporter;
        if(!sourceImporter || !sourceImporter.DoesSourceTextureHaveAlpha())return texture;
        float threshold=Mathf.Clamp01(cutoff/Mathf.Max(.0001f,factorAlpha));
        string role=source.Contains("/billboards/")?"Billboard-":"Leaf-";
        string key=source+"|"+AssetDatabase.GetAssetDependencyHash(source)+"|"+
            threshold.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
        string path=Output+role+Hash128.Compute(key)+Path.GetExtension(source);
        if(!File.Exists(path)&&!AssetDatabase.CopyAsset(source,path))throw new IOException("Nie skopiowano maski liści: "+source);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Brak importera maski liści: "+path);
        // A generated variant protects source material references during GLB
        // iteration and supports different alpha cutoffs on the same source map.
        if(!importer.alphaIsTransparency || !importer.mipMapsPreserveCoverage ||
            Mathf.Abs(importer.alphaTestReferenceValue-threshold)>=.0001f)
        {
            importer.alphaIsTransparency=true;importer.mipMapsPreserveCoverage=true;
            importer.alphaTestReferenceValue=threshold;importer.SaveAndReimport();
        }
        var result=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!result)throw new InvalidOperationException("Nie zaimportowano maski liści: "+path);
        return result;
    }
    static void AddLawnGrass(List<Item> points)
    {
        // Reuse the original blade model and Claude's local instancing on lawns
        // that the old forest-cluster layout left empty. Fixed cells preserve
        // deterministic scenes without touching Unity's global Random state.
        // Spójna 3D murawa na całym mniejszym obozie, bez pustej wschodniej połowy.
        // Kamera rysuje tylko pobliskie instancje (BraxClaudeTrawa), bez kopii GLB.
        const float spacing=.90f;
        var occupied=new HashSet<long>();
        long Cell(int x,int z)=>((long)x<<32)^(uint)z;
        foreach(var p in points)occupied.Add(Cell(Mathf.FloorToInt(p.x/spacing),Mathf.FloorToInt(p.z/spacing)));
        var paths=BraxObozEnvironment.PathRectangles();
        int xMin=Mathf.FloorToInt(-BraxObozCampusLayout.PlotRadiusX/spacing)-2;
        int xMax=Mathf.CeilToInt(BraxObozCampusLayout.PlotRadiusX/spacing)+2;
        int zMin=Mathf.FloorToInt(
            (BraxObozCampusLayout.PlotCenterZ-BraxObozCampusLayout.PlotRadiusZ)/spacing)-2;
        int zMax=Mathf.CeilToInt(
            (BraxObozCampusLayout.PlotCenterZ+BraxObozCampusLayout.PlotRadiusZ)/spacing)+2;
        for(int iz=zMin;iz<=zMax;iz++)for(int ix=xMin;ix<=xMax;ix++)
        {
            if(occupied.Contains(Cell(ix,iz)))continue;
            uint h=unchecked((uint)(ix+1000)*73856093u^(uint)(iz+1000)*19349663u^20261104u);
            h^=h>>13;h=unchecked(h*1274126177u);h^=h>>16;
            if((h&255u)<38u)continue;
            float x=(ix+.15f+((h>>8)&255u)/255f*.7f)*spacing;
            float z=(iz+.15f+((h>>16)&255u)/255f*.7f)*spacing;
            if(!BraxObozCampusLayout.InsidePlot(x,z,11f) ||
                BraxObozCampusLayout.ReservedLawn(x,z,.8f))continue;
            bool pathShoulder=false;
            foreach(var path in paths)
            {
                float dx=Mathf.Max(path.xMin-x,0,x-path.xMax),dz=Mathf.Max(path.yMin-z,0,z-path.yMax);
                if(dx*dx+dz*dz<4){pathShoulder=true;break;}
            }
            if(pathShoulder)continue;
            points.Add(new Item{x=x,z=z,yaw=(h&65535u)*360f/65535f,scale=.72f+((h>>24)&255u)/255f*.34f});
        }
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

