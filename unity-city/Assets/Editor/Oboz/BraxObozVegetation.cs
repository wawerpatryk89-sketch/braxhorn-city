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
    [Serializable] public sealed class Atlas
    {
        public string model, file;
        public float span, centerY, sourceBottomY, sourceTopY;
        public bool sourceBoundsMeasured, trunkCenteredSpan;
    }
    [Serializable] public sealed class Atlases { public bool complete; public Atlas[] billboards; }
    static readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();

    public static void Populate(Transform parent)
    {
        BraxObozCampusLayout.Reload();
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Editor/Oboz/VegetationLayout.json"));
        var atlas = JsonUtility.FromJson<Atlases>(File.ReadAllText(Source + "billboards/metadata.json"));
        if (layout.schema != 1 || Mathf.Abs(layout.worldScale-K)>.00001f || !atlas.complete ||
            layout.prototypes.Length!=BraxObozAuthorizedTrees.LegacyCount || atlas.billboards.Length != BraxObozAuthorizedTrees.LegacyCount)
            throw new InvalidOperationException("Niepełne dane roślinności obozu");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(); materials.Clear();
        BraxObozTreePlacement.Begin(parent);
        var forest = new GameObject("Las — źródłowe LOD, układ do odbioru").transform; forest.SetParent(parent,false);
        var namedTrees=BraxObozAuthorizedTrees.Load();
        var prototypes = new GameObject[BraxObozAuthorizedTrees.LegacyCount+namedTrees.Length];
        try
        {
            for (int i=0;i<layout.prototypes.Length;i++)
                prototypes[i]=Prototype(layout.prototypes[i],atlas.billboards.Single(a=>a.model==layout.prototypes[i]+"_LOD2.glb"));
            for(int i=0;i<namedTrees.Length;i++)
            {
                var definition=namedTrees[i];
                try { prototypes[BraxObozAuthorizedTrees.LegacyCount+i]=Prototype(definition.name,definition.billboard,definition.missingAuthoredLod2); }
                catch(Exception ex){BraxBuildQuality.Warn("OBÓZ wskazany gatunek pominięty: "+definition.name+" — "+ex);}
            }
            int placedTrees=0;
            foreach (var point in layout.trees)
            {
                if (Mathf.Abs(point.x)>138f || Mathf.Abs(point.z)>138f) continue; // poza terenem 280x280
                if(BraxObozCampusLayout.Reserved(point.x,point.z,3))continue;
                if (point.prototype<0 || point.prototype>=BraxObozAuthorizedTrees.LegacyCount) throw new InvalidOperationException("Błędny oryginalny wariant drzewa");
                var tree=Object.Instantiate(prototypes[point.prototype],forest);
                tree.name=layout.prototypes[point.prototype]; tree.transform.localPosition=new Vector3(point.x,0,point.z)*K;
                tree.transform.localRotation=Quaternion.Euler(0,point.yaw,0);tree.transform.localScale=Vector3.one*(point.scale*K);
                BraxObozTreePlacement.Place(tree.transform);
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
            placedTrees+=BraxObozAuthorizedTrees.Plant(forest,prototypes,namedTrees);
            var pathRects=BraxObozEnvironment.PathRectangles();
            bool OnWalkway(Item p)
            {
                foreach(var path in pathRects)
                {
                    float dx=Mathf.Max(path.xMin-p.x,0,p.x-path.xMax);
                    float dz=Mathf.Max(path.yMin-p.z,0,p.z-path.yMax);
                    if(dx*dx+dz*dz<2.25f)return true;
                }
                return false;
            }
            var grass=layout.grass.Where(p=>Mathf.Abs(p.x)<=138f&&Mathf.Abs(p.z)<=138f&&
                BraxObozCampusLayout.InsidePlot(p.x,p.z,11f)&&
                !BraxObozCampusLayout.ReservedLawn(p.x,p.z,.8f)&&
                !FightCampBareGround(p.x,p.z)&&!OnWalkway(p)).ToList();
            int originalGrass=grass.Count;
            AddLawnGrass(grass);
            AddRondoGrass(grass);
            Grass(parent,grass.ToArray());
            Debug.Log("BRAXHORN_OBOZ las: drzewa="+placedTrees+" pas_zewnetrzny="+fenceTrees+
                " oryginalna_trawa=Grass_Medium_01_02_2K kępy="+grass.Count+
                " nowe_kepy_trawnika="+(grass.Count-originalGrass)+"; test telefonu nadal wymagany");
        }
        finally
        {
            foreach(var p in prototypes)if(p)Object.DestroyImmediate(p);
            BraxObozTreePlacement.Write();
        }
    }
    // Istniejący layout lasu pochodził ze starego prostokąta. Po redukcji
    // działki tylko część oryginalnych pozycji leży blisko nowego płotu.
    // Ten deterministyczny pas używa WYŁĄCZNIE gotowych źródeł jodeł/sadzonek,
    // bez atrap billboardów w zbliżeniu i bez importowania nowych modeli.
    static int AddFenceWoodland(Transform forest,GameObject[] prototypes)
    {
        if(prototypes == null || prototypes.Length < BraxObozAuthorizedTrees.LegacyCount)
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
            // Named pine/small trees have their own explicit twelve-per-species
            // budget. The original 160-sample fir belt never selects them.
            int prototypeIndex=(int)((seed>>16)%(uint)BraxObozAuthorizedTrees.LegacyCount);
            var tree=Object.Instantiate(prototypes[prototypeIndex],forest);
            tree.name=prototypes[prototypeIndex].name+" — przy ogrodzeniu";
            tree.transform.localPosition=new Vector3(x,0,z)*K;
            tree.transform.localRotation=Quaternion.Euler(0,(seed%359u),0);
            tree.transform.localScale=Vector3.one*((.74f+(seed%37u)*.012f)*K);
            BraxObozTreePlacement.Place(tree.transform);
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

    static GameObject Prototype(string name,Atlas atlas,bool singleAuthoredLod=false)
    {
        var root=new GameObject(name);
        try
        {
            var near=Model(Source+name+"_LOD1__unity.glb",root.transform);
            var mid=singleAuthoredLod?null:Model(Source+name+"_LOD2__unity.glb",root.transform);
            var reference=mid?mid:near;
            var midBounds=BraxObozPlacement.Bounds(reference.GetComponentsInChildren<Renderer>());
            float sourceSpan=Mathf.Max(midBounds.size.x,midBounds.size.y,midBounds.size.z)*1.04f;
            if(atlas.trunkCenteredSpan)
                sourceSpan=Mathf.Max(midBounds.size.y,2*Mathf.Max(Mathf.Abs(midBounds.min.x),Mathf.Abs(midBounds.max.x),
                    Mathf.Abs(midBounds.min.z),Mathf.Abs(midBounds.max.z)))*1.04f;
            if(Mathf.Abs(midBounds.center.y-atlas.centerY)>.01f || Mathf.Abs(sourceSpan-atlas.span)>.01f)
                BraxBuildQuality.Warn("OBÓZ TRENINGOWY — atlas niezgodny z obrysem rzeczywistego źródłowego LOD: "+name);
            if(atlas.sourceBoundsMeasured &&
                (Mathf.Abs(midBounds.min.y-atlas.sourceBottomY)>.01f ||
                 Mathf.Abs(midBounds.max.y-atlas.sourceTopY)>.01f))
                BraxBuildQuality.Warn("OBÓZ TRENINGOWY — atlas ma inną podstawę niż rzeczywisty źródłowy LOD: "+name);
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
            if(mid)lod.SetLODs(new[]{new LOD(.40f,near.GetComponentsInChildren<Renderer>()),new LOD(.075f,mid.GetComponentsInChildren<Renderer>()),new LOD(.012f,new Renderer[]{renderer})});
            else lod.SetLODs(new[]{new LOD(.40f,near.GetComponentsInChildren<Renderer>()),new LOD(.012f,new Renderer[]{renderer})});
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
    // Nie sadzimy kęp na polanie walk, błotnistych stanowiskach ani w ciągu ruchu.
    static bool FightCampBareGround(float x,float z)
    {
        foreach(var center in new[]{new Vector2(-23,15),new Vector2(23,15),
            new Vector2(0,-5),new Vector2(-23,-40),new Vector2(23,-40),
            new Vector2(-40,34)})
            if(Vector2.Distance(new Vector2(x,z),center)<17f)return true;
        return BraxObozCampusLayout.FenceDistance(x,z)>22f;
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
                BraxObozCampusLayout.ReservedLawn(x,z,.8f) ||
                FightCampBareGround(x,z))continue;
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
    static void AddRondoGrass(List<Item> points)
    {
        // 3D grass matching the owner's GREEN central roundabout island.
        // Avoid the real bull pedestal; never place leaves on road asphalt.
        const float spacing=.70f;
        for(int ix=-8;ix<=8;ix++)
        for(int iz=-8;iz<=8;iz++)
        {
            float x=ix*spacing;
            float localZ=iz*spacing;
            float r2=x*x+localZ*localZ;
            if(r2>5.55f*5.55f || r2<2.8f*2.8f)continue;
            // The real bull's rectangular pedestal is 3.4 x 5.4.
            if(Mathf.Abs(x)<1.85f&&Mathf.Abs(localZ)<2.88f)continue;
            uint h=unchecked((uint)(ix+100)*73856093u^(uint)(iz+200)*19349663u);
            points.Add(new Item {
                x=x+.08f*Mathf.Sin(ix*1.7f),
                z=-94f+localZ+.08f*Mathf.Cos(iz*1.7f),
                yaw=(h%360u),
                scale=.72f+(h%27u)*.011f
            });
        }
    }

    static T Save<T>(T obj,string path) where T:Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old){EditorUtility.CopySerialized(obj,old);Object.DestroyImmediate(obj);return old;}
        AssetDatabase.CreateAsset(obj,path);return obj;
    }
    [Serializable] sealed class GrassGroundReport
    {
        public string title="OBÓZ TRENINGOWY — rzeczywiste podłoże Grass Medium";
        public int requested,placed,missingSupport;
        public float rayStartMetres=.31f,maximumSurfaceMetres=.30f,minimumNormalY=.65f,clearanceMetres=.005f;
        public bool runtimeRaycasts=false,sourceGeometryChanged=false,apkBuilt=false;
        public Vector3[] missingExamples;
    }
    static (Item point,float y)[] MeasureGrassGround(Transform parent,Item[] points)
    {
        // One editor pass. Low floors, ground and the actual raised island may
        // support grass; equipment and roofs are outside this short ray band.
        Physics.SyncTransforms();
        float baseY=parent.position.y;
        var supports=parent.GetComponentsInChildren<Collider>(true).Where(c=>c&&c.enabled&&
            c.gameObject.activeInHierarchy&&!c.isTrigger&&c.bounds.max.y<=baseY+.30f*K).ToArray();
        var placed=new List<(Item point,float y)>();var missing=new List<Vector3>();int missingCount=0;
        foreach(var point in points)
        {
            var position=parent.TransformPoint(new Vector3(point.x,0,point.z)*K);
            var from=new Vector3(position.x,baseY+.31f*K,position.z);
            var ray=new Ray(from,Vector3.down);float highest=float.NegativeInfinity;
            foreach(var support in supports)
            {
                var bounds=support.bounds;
                if(position.x<bounds.min.x||position.x>bounds.max.x||position.z<bounds.min.z||position.z>bounds.max.z)continue;
                if(support.Raycast(ray,out var hit,.62f*K)&&hit.normal.y>.65f&&
                    hit.point.y<=baseY+.30f*K&&hit.point.y>highest)highest=hit.point.y;
            }
            if(float.IsNegativeInfinity(highest))
            {
                missingCount++;if(missing.Count<24)missing.Add(position);continue;
            }
            placed.Add((point,highest+.005f*K));
        }
        var report=new GrassGroundReport{requested=points.Length,placed=placed.Count,
            missingSupport=missingCount,missingExamples=missing.ToArray()};
        string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"grass-grounding.json"),JsonUtility.ToJson(report,true));
        if(missingCount>0)BraxBuildQuality.Warn("OBÓZ TRENINGOWY — pominięto "+missingCount+
            " kęp Grass Medium bez niskiej rzeczywistej podpory; szczegóły grass-grounding.json");
        Debug.Log("BRAXHORN_OBOZ_GRASS_GROUND measured="+placed.Count+" missing="+missingCount+" runtime_raycast=false");
        return placed.ToArray();
    }
    static void Grass(Transform parent,Item[] points)
    {
        // Mandatory original owner geometry. No Bermuda seedlings, no impostor
        // grass and no replacement blades procedurally generated from quads.
        // The two decompressed .blend originals are exported to PRIVATE GLBs
        // before the Unity scene is generated. Shared meshes + DrawMeshInstanced
        // keep the mobile rendering cost independent of prefab duplication.
        const string privateRoot="Assets/Source/Prywatne/Oboz/OwnerAssets/";
        if(points==null || points.Length<100)
            throw new InvalidOperationException("Za mało prawdziwej trawy na terenie obozu");
        var grounded=MeasureGrassGround(parent,points);
        var holder=new GameObject("Oryginalna murawa 3D — Grass Medium 01 + 02");
        holder.transform.SetParent(parent,false);
        for(int variant=1;variant<=2;variant++)
        {
            string id="grass_medium_0"+variant+"_2k";
            string path=privateRoot+id+"/"+id+".glb";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab)throw new InvalidOperationException(
                "Brak oryginalnej wyeksportowanej siatki Grass Medium: "+path+
                ". Nie uruchamiaj APK z dawnymi kępkami Bermuda.");
            var original=Object.Instantiate(prefab);
            original.name="Weryfikacja oryginału "+id;
            try
            {
                var filters=original.GetComponentsInChildren<MeshFilter>(true)
                    .Where(f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()!=null).ToArray();
                if(filters.Length==0)
                    throw new InvalidOperationException("Oryginalna Grass Medium nie ma siatek GLB: "+id);
                var renderers=filters.Select(f=>f.GetComponent<MeshRenderer>()).ToArray();
                var bounds=renderers[0].bounds;
                foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                if(bounds.size.y<.001f || bounds.size.y>50f)
                    throw new InvalidOperationException("Grass Medium: nieprawidłowa wysokość obrysu "+id);
                var combine=new List<CombineInstance>();
                // Imported Sketchfab/Blender pivots may be far from geometry.
                // Centre XY footprint and ground Y without altering any
                // ORIGINAL vertex inside the private GLB source.
                var bottom=Matrix4x4.Translate(
                    new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z));
                foreach(var filter in filters)
                {
                    for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                        combine.Add(new CombineInstance {
                            mesh=filter.sharedMesh,
                            subMeshIndex=sub,
                            transform=bottom*original.transform.worldToLocalMatrix*
                                filter.transform.localToWorldMatrix
                        });
                }
                var mesh=new Mesh {
                    name="ORYGINALNY Grass Medium "+variant+" — bez zmniejszania geometrii",
                    indexFormat=IndexFormat.UInt32
                };
                mesh.CombineMeshes(combine.ToArray(),true,true);
                mesh.RecalculateBounds();
                if(mesh.vertexCount==0 || mesh.bounds.size.y<.001f)
                    throw new InvalidOperationException("Nie można zbudować wspólnej siatki Grass Medium "+id);
                // Split deterministic grass placements into original 01 and 02.
                var selected=grounded.Where((p,i)=>(i&1)==(variant-1)).ToArray();
                var data=ScriptableObject.CreateInstance<BraxClaudeTrawaDane>();
                float targetHeight=.33f*K;
                data.wysokosc=targetHeight/mesh.bounds.size.y;
                data.granicaLaki=.17f*K;
                data.kepy=selected.Select(p=>{
                    var world=parent.TransformPoint(new Vector3(p.point.x,0,p.point.z)*K);
                    return new Vector4(world.x,world.z,p.point.yaw,p.point.scale);
                }).ToArray();
                data.y=selected.Select(p=>p.y).ToArray();
                var node=new GameObject("Grass Medium "+variant+" — tylko oryginalny GLB");
                node.transform.SetParent(holder.transform,false);
                var grass=node.AddComponent<BraxClaudeTrawa>();
                grass.dane=Save(data,Output+id+"-GrassData.asset");
                grass.siatka=Save(mesh,Output+id+"-GrassMesh.asset");
                grass.material=BraxObozMaterials.OwnerGrassMediumPbr(variant);
                grass.kawalek=9f*K;
                // Culling distance is a camera budget in world metres, not a
                // model dimension. Both variants fade to ground texture by 25 m.
                grass.zasiegMurawy=25f;
                grass.zasiegLaki=25f;
                grass.zwolnijPo=45f*K;
                Debug.Log("BRAXHORN_OBOZ_OWNER_GRASS_MESH variant="+variant+
                    " source="+id+" vertices="+mesh.vertexCount+
                    " instances="+selected.Length+" sourceTextures=2048x2048"+
                    " ownerGeometryDecimation=false phoneFPSUnverified=true");
            }
            finally { Object.DestroyImmediate(original); }
        }
    }

}

