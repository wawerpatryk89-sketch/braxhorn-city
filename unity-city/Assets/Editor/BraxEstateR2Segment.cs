using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// First visible checkpoint: dom-04 original plaster, brick plinth, pitched roof
// and a bounded part of its existing approach. No other house is selected.
public static class BraxEstateR2Segment
{
    const string Output = "Assets/PlayableGenerated/R2Segment";
    [Serializable] sealed class Record
    {
        public string renderer, role, source;
        public Vector3 unityMin, unityMax;
        public int sourceVertices, outputVertices, insideTriangles;
        public float repeatMetres;
        public Vector3 changedUnityMin, changedUnityMax;
    }
    [Serializable] sealed class Face { public Vector3 center; public bool selected; }
    [Serializable] sealed class Selection { public string renderer; public Face[] faces; }
    [Serializable] sealed class Audit
    {
        public string scene = "BraxhornEstate", stage = "WORK_IN_PROGRESS__EARLY_CHECKPOINT__VISUAL_REVIEW_PENDING";
        public string sourceHouse = "dom-03 / willa_nowoczesna; dom-04 / dom_z_wnetrzem", coordinateConversion = "Unity X = -source X; Y/Z unchanged";
        public bool fullRenovationApproved = false, visualQAConfirmed = false, windowsImported = false, doorsImported = false;
        public Record[] bindings;
        public int realTreeInstances, realShrubInstances, replacedProceduralTreeRenderers;
        public string plantingSource = "Original Verdant Trail meshes 439/446/453/460/467; local material clones, shared meshes, LOD distance culling";
    }
    struct Vertex
    {
        public Vector3 p, n;
        public Vector2 uv;
        public Vector4 tangent;
        public Color color;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex { p=Vector3.LerpUnclamped(a.p,b.p,t), n=Vector3.LerpUnclamped(a.n,b.n,t).normalized, uv=Vector2.LerpUnclamped(a.uv,b.uv,t), tangent=Vector4.LerpUnclamped(a.tangent,b.tangent,t), color=Color.LerpUnclamped(a.color,b.color,t) };
    }
    static Texture2D Texture(string path, bool normal)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new Exception("Missing verified segment map: " + path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = path.Contains("Albedo");
        importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string key, string source, Color tint)
    {
        var shader = Shader.Find("Standard");
        if (!shader) throw new Exception("Standard PBR shader missing");
        var m = new Material(shader) { name="R2 dom04 / " + key, color=tint, enableInstancing=true };
        m.mainTexture = Texture(source + "Albedo.png", false);
        m.SetTexture("_BumpMap", Texture(source + "Normal.png", true));
        m.SetTexture("_MetallicGlossMap", Texture(source + "MR.png", false));
        m.SetFloat("_BumpScale", key.Contains("Plaster") ? .2f : .45f); m.SetFloat("_GlossMapScale", .7f);
        if (File.Exists(source + "AO.png"))
        {
            m.SetTexture("_OcclusionMap", Texture(source + "AO.png", false));
            m.SetFloat("_OcclusionStrength", .55f);
        }
        m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICGLOSSMAP");
        AssetDatabase.CreateAsset(m, Output + "/" + key + ".mat"); return m;
    }
    // Plane clips the EXISTING triangles; there are no overlay planes or z-fighting.
    static List<Vertex> Clip(List<Vertex> polygon, int axis, float edge, bool greater)
    {
        var result = new List<Vertex>();
        if (polygon.Count == 0) return result;
        var a = polygon[polygon.Count - 1]; float da = (a.p[axis] - edge) * (greater ? 1 : -1);
        foreach (var b in polygon)
        {
            float db = (b.p[axis] - edge) * (greater ? 1 : -1);
            if ((da >= 0) != (db >= 0)) result.Add(Vertex.Lerp(a, b, da / (da - db)));
            if (db >= 0) result.Add(b);
            a=b; da=db;
        }
        return result;
    }
    static void Add(List<Vertex> polygon, List<Vertex> output, bool projected, Vector3 normal, float metres, bool roof=false)
    {
        for (int i=1; i+1<polygon.Count; i++)
        foreach (var v0 in new[]{polygon[0],polygon[i],polygon[i+1]})
        {
            var v=v0;
            if (projected)
            {
                // Roof: project along slope (not a stretched horizontal projection).
                var up = Mathf.Abs(normal.y) > .95f ? Vector3.forward : Vector3.up;
                var u = Vector3.Cross(up, normal).normalized;
                var w = Vector3.Cross(normal, u).normalized;
                // A roof tile has curved micro-faces: changing the projection frame
                // on each facet made a mosaic. One continuous world-XZ chart
                // preserves phase and orientation across the actual roof geometry.
                v.uv = roof ? new Vector2(v.p.x,v.p.z) / metres : new Vector2(Vector3.Dot(v.p,u),Vector3.Dot(v.p,w)) / metres;
            }
            output.Add(v);
        }
    }
    static BraxR2ProgressEvidence.Binding Bind(MeshRenderer r, Material material, string role, float metres, List<Record> records, bool approach=false, bool atlasSelection=false, Material preservedMaterial=null)
    {
        var filter = r.GetComponent<MeshFilter>();
        var original = filter.sharedMesh;
        if (!original || original.subMeshCount != 1) throw new Exception("Unexpected original mesh: " + r.name);
        var oldMaterials=r.sharedMaterials;
        if (!approach)
        {
            bool dom03=atlasSelection;
            if (r.bounds.min.x < (dom03?20f:-35.1f) || r.bounds.max.x > (dom03?38f:-20.9f) || r.bounds.min.z < (dom03?-12f:-6.8f) || r.bounds.max.z > (dom03?12f:6.8f))
                throw new Exception("Scoped renderer escaped verified source bounds: " + r.name);
        }
        var positions=original.vertices; var normals=original.normals; var uvs=original.uv;
        var tangents=original.tangents; var colors=original.colors;
        var outside=new List<Vertex>(); var inside=new List<Vertex>();
        var indices=original.GetTriangles(0);
        Selection selection=null;
        if(atlasSelection)
        {
            selection=JsonUtility.FromJson<Selection>(File.ReadAllText("Assets/Source/City/R2Segment/"+r.name+"-selection.json"));
            if(selection.renderer!=r.name||selection.faces.Length!=indices.Length/3)throw new Exception("Source triangle selection identity mismatch: "+r.name);
        }
        for (int i=0;i<indices.Length;i+=3)
        {
            var poly=new List<Vertex>();
            for (int j=0;j<3;j++)
            {
                int v=indices[i+j];
                poly.Add(new Vertex{p=r.transform.TransformPoint(positions[v]), n=r.transform.TransformDirection(normals[v]).normalized, uv=uvs.Length==positions.Length?uvs[v]:Vector2.zero, tangent=tangents.Length==positions.Length?tangents[v]:Vector4.zero, color=colors.Length==positions.Length?colors[v]:Color.white});
            }
            var face=Vector3.Cross(poly[1].p-poly[0].p,poly[2].p-poly[0].p).normalized;
            if(selection!=null)
            {
                var sourceFace=selection.faces[i/3];
                if(Vector3.Distance((poly[0].p+poly[1].p+poly[2].p)/3,sourceFace.center)>.003f)throw new Exception("Unity source face ordering differs: "+r.name+" / "+(i/3));
                if(!sourceFace.selected){Add(poly,outside,false,face,metres);continue;}
            }
            if (approach)
            {
                // Union of the two existing source X 7..21 / -21..-7 approaches.
                // Remainders retain the exact original material and UVs.
                var pending=new List<List<Vertex>>{poly};
                foreach(int side in new[]{-1,1})
                {
                    var remaining=new List<List<Vertex>>();
                    foreach(var part in pending)
                    {
                        var current=part;
                        foreach(var plane in new[]{new Vector3(0,side<0?-21:7,1),new Vector3(0,side<0?-7:21,-1),new Vector3(2,-2,1),new Vector3(2,2,-1)})
                        {
                            var rejected=Clip(current,(int)plane.x,plane.y,plane.z<0);
                            if(rejected.Count>2)remaining.Add(rejected);
                            current=Clip(current,(int)plane.x,plane.y,plane.z>0);
                        }
                        Add(current,inside,true,face,metres);
                    }
                    pending=remaining;
                }
                foreach(var part in pending)Add(part,outside,false,face,metres);
                continue;
            }
            Add(poly,inside,true,face,metres,role.Contains("roof"));
        }
        if (inside.Count==0) throw new Exception("No actual geometry selected for " + role);
        var all=outside.Concat(inside).ToArray();
        var mesh=new Mesh{name="R2 local UV copy / " + r.name,indexFormat=IndexFormat.UInt32};
        mesh.vertices=all.Select(v=>r.transform.InverseTransformPoint(v.p)).ToArray();
        mesh.normals=all.Select(v=>r.transform.InverseTransformDirection(v.n).normalized).ToArray();
        mesh.uv=all.Select(v=>v.uv).ToArray(); mesh.colors=all.Select(v=>v.color).ToArray();
        mesh.tangents=all.Select(v=>v.tangent).ToArray();
        mesh.subMeshCount=outside.Count>0?2:1;
        if(outside.Count>0)mesh.SetTriangles(Enumerable.Range(0,outside.Count).ToArray(),0);
        mesh.SetTriangles(Enumerable.Range(outside.Count,inside.Count).ToArray(),outside.Count>0?1:0);
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,Output+"/"+r.name+".asset");
        filter.sharedMesh=mesh;
        r.sharedMaterials=outside.Count>0?new[]{preservedMaterial?preservedMaterial:oldMaterials[0],material}:new[]{material};
        var changedBounds=new Bounds(inside[0].p,Vector3.zero); foreach(var v in inside)changedBounds.Encapsulate(v.p);
        records.Add(new Record{renderer=r.name,role=role,source=material.name,unityMin=r.bounds.min,unityMax=r.bounds.max,changedUnityMin=changedBounds.min,changedUnityMax=changedBounds.max,sourceVertices=original.vertexCount,outputVertices=mesh.vertexCount,insideTriangles=inside.Count/3,repeatMetres=metres});
        return new BraxR2ProgressEvidence.Binding{renderer=r,filter=filter,beforeMesh=original,afterMesh=mesh,beforeMaterials=oldMaterials,afterMaterials=r.sharedMaterials};
    }
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate"); if(!root)throw new Exception("Original estate missing");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var plaster=Material("WarmMineralPlaster","Assets/Source/City/R2Segment/Stucco",new Color(.965f,.945f,.905f));
        var brick=Material("RedBrickPlinth","Assets/Source/City/R2Segment/RedBrick",new Color(.86f,.83f,.78f));
        var whiteBrick=Material("WhitewashedBrickDom03","Assets/Source/City/R2Segment/WhiteBrick",Color.white);
        var paving=Material("PavingStones151","Assets/Source/City/R2Segment/Paving",Color.white);
        var roofSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/R2Approved/R2RoofTiles.mat");
        if(!roofSource)throw new Exception("Verified Poly Haven roof source missing");
        var roof=new Material(roofSource){name="R2 dom04 / real roof tiles / 2m",enableInstancing=true};
        AssetDatabase.CreateAsset(roof,Output+"/RoofTiles.mat");
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
        var records=new List<Record>(); var bindings=new List<BraxR2ProgressEvidence.Binding>();
        foreach(var task in new[]{("EstateMesh_327",plaster,"exterior stucco",1.5f),("EstateMesh_312",plaster,"entrance plaster",1.5f),("EstateMesh_321",brick,"brick plinth",2f),("EstateMesh_323",roof,"pitched roof",2f),("EstateMesh_275",paving,"both bounded existing approaches",5.4f)})
        {
            var r=renderers.Single(x=>x.name==task.Item1);
            bindings.Add(Bind(r,task.Item2,task.Item3,task.Item4,records,task.Item1=="EstateMesh_275"));
        }
        foreach(var name in new[]{"EstateMesh_309","EstateMesh_311"})
            bindings.Add(Bind(renderers.Single(x=>x.name==name),whiteBrick,"dom03 original atlas brick faces",2f,records,false,true));
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Source/City/braxhorn-estate.gltf");
        var originalRoof=prefab.GetComponentsInChildren<MeshRenderer>(true).Single(x=>x.name=="EstateMesh_310").sharedMaterial;
        bindings.Add(Bind(renderers.Single(x=>x.name=="EstateMesh_310"),roof,"dom03 pitched roof; original fascia preserved",2f,records,false,true,originalRoof));
        var evidence=new GameObject("R2ProgressEvidence").AddComponent<BraxR2ProgressEvidence>(); evidence.bindings=bindings.ToArray();
        Plant(renderers,evidence);
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/r2-segment-bindings.json",JsonUtility.ToJson(new Audit{bindings=records.ToArray(),realTreeInstances=2,realShrubInstances=12,replacedProceduralTreeRenderers=8},true));
        Debug.Log("R2_DOM04_SCOPED_PBR_IMPLEMENTED__VISUAL_REVIEW_PENDING " + records.Count);
    }
    static void Plant(MeshRenderer[] renderers,BraxR2ProgressEvidence evidence)
    {
        var additions=new List<GameObject>();var clones=new Dictionary<Material,Material>();
        MeshRenderer Part(string source,Transform parent,string label,float scale)
        {
            var original=renderers.Single(r=>r.name==source);
            var o=new GameObject(label);o.transform.SetParent(parent,false);o.transform.localScale=Vector3.one*scale;
            o.AddComponent<MeshFilter>().sharedMesh=original.GetComponent<MeshFilter>().sharedMesh;
            var r=o.AddComponent<MeshRenderer>();
            r.sharedMaterials=original.sharedMaterials.Select(m=>{
                if(!clones.TryGetValue(m,out var copy))
                {
                    copy=new Material(m){name="R2 local planting "+m.name,enableInstancing=true};
                    AssetDatabase.CreateAsset(copy,Output+"/Plant_"+clones.Count+".mat");clones.Add(m,copy);
                }
                return copy;
            }).ToArray();
            r.shadowCastingMode=source=="EstateMesh_439"?ShadowCastingMode.On:ShadowCastingMode.Off;
            return r;
        }
        // Genuine existing model components; no generated canopy spheres.
        foreach(float sourceX in new[]{-37f,36f})
        {
            var tree=new GameObject("R2 original Verdant tree / "+sourceX); additions.Add(tree);
            float scale=4.7f/(9.336063f-.798673f);
            tree.transform.position=new Vector3(-sourceX,.04f-.798673f*scale,7);
            var near=new[]{"EstateMesh_439","EstateMesh_446","EstateMesh_453","EstateMesh_460"}.Select(s=>Part(s,tree.transform,"Near / "+s,scale)).Cast<Renderer>().ToArray();
            var far=new[]{"EstateMesh_439","EstateMesh_453"}.Select(s=>Part(s,tree.transform,"Far / "+s,scale)).Cast<Renderer>().ToArray();
            var lod=tree.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.22f,near),new LOD(.07f,far)});lod.RecalculateBounds();
        }
        foreach(var source in new[]{new Vector2(18,6),new Vector2(18,-6),new Vector2(18,9.5f),new Vector2(18,-9.5f),new Vector2(37,9),new Vector2(37,-9),new Vector2(-19,6),new Vector2(-19,-6),new Vector2(-19,9.5f),new Vector2(-19,-9.5f),new Vector2(-39,9),new Vector2(-39,-9)})
        {
            var plant=new GameObject("R2 original Verdant shrub / "+source);additions.Add(plant);
            plant.transform.position=new Vector3(-source.x,.04f,source.y);
            plant.transform.rotation=Quaternion.Euler(0,Mathf.Repeat(source.x*29+source.y*47,360),0);
            var r=Part("EstateMesh_467",plant.transform,"Real shrub",.4f);
            var lod=plant.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.018f,new Renderer[]{r})});lod.RecalculateBounds();
        }
        var names=new[]{"EstateMesh_84","EstateMesh_85","EstateMesh_86","EstateMesh_87","EstateMesh_89","EstateMesh_90","EstateMesh_91","EstateMesh_92"};
        evidence.replacedTreeRenderers=names.Select(n=>renderers.Single(r=>r.name==n)).ToArray();
        foreach(var r in evidence.replacedTreeRenderers)
        {
            if(r.bounds.min.x < -45||r.bounds.max.x>45||r.bounds.min.z < -14||r.bounds.max.z>14)throw new Exception("Tree replacement escaped approved segment");
            r.enabled=false;
        }
        evidence.addedPlanting=additions.ToArray();
    }
}
