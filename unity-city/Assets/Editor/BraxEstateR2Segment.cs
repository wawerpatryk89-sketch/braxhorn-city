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
    }
    [Serializable] sealed class Audit
    {
        public string scene = "BraxhornEstate", stage = "WORK_IN_PROGRESS__EARLY_CHECKPOINT__VISUAL_REVIEW_PENDING";
        public string sourceHouse = "dom-04 / dom_z_wnetrzem", coordinateConversion = "Unity X = -source X; Y/Z unchanged";
        public bool fullRenovationApproved = false, visualQAConfirmed = false, windowsImported = false, doorsImported = false;
        public Record[] bindings;
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
    static void Add(List<Vertex> polygon, List<Vertex> output, bool projected, Vector3 normal, float metres)
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
                v.uv = new Vector2(Vector3.Dot(v.p,u),Vector3.Dot(v.p,w)) / metres;
            }
            output.Add(v);
        }
    }
    static BraxR2ProgressEvidence.Binding Bind(MeshRenderer r, Material material, string role, float metres, List<Record> records, bool approach=false)
    {
        var filter = r.GetComponent<MeshFilter>();
        var original = filter.sharedMesh;
        if (!original || original.subMeshCount != 1) throw new Exception("Unexpected original mesh: " + r.name);
        var oldMaterials=r.sharedMaterials;
        if (!approach && (r.bounds.min.x < -35.1f || r.bounds.max.x > -20.9f || r.bounds.min.z < -6.8f || r.bounds.max.z > 6.8f))
            throw new Exception("dom04 renderer escaped verified source bounds: " + r.name);
        var positions=original.vertices; var normals=original.normals; var uvs=original.uv;
        var tangents=original.tangents; var colors=original.colors;
        var outside=new List<Vertex>(); var inside=new List<Vertex>();
        var indices=original.GetTriangles(0);
        for (int i=0;i<indices.Length;i+=3)
        {
            var poly=new List<Vertex>();
            for (int j=0;j<3;j++)
            {
                int v=indices[i+j];
                poly.Add(new Vertex{p=r.transform.TransformPoint(positions[v]), n=r.transform.TransformDirection(normals[v]).normalized, uv=uvs.Length==positions.Length?uvs[v]:Vector2.zero, tangent=tangents.Length==positions.Length?tangents[v]:Vector4.zero, color=colors.Length==positions.Length?colors[v]:Color.white});
            }
            var face=Vector3.Cross(poly[1].p-poly[0].p,poly[2].p-poly[0].p).normalized;
            if (approach)
            {
                // Source X 7..21, Z -2..2: existing approach only, no street/other parcels.
                foreach (var plane in new[]{new Vector3(0,-21,1),new Vector3(0,-7,-1),new Vector3(2,-2,1),new Vector3(2,2,-1)})
                {
                    Add(Clip(poly,(int)plane.x,plane.y,plane.z<0),outside,false,face,metres);
                    poly=Clip(poly,(int)plane.x,plane.y,plane.z>0);
                }
            }
            Add(poly,inside,true,face,metres);
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
        r.sharedMaterials=outside.Count>0?new[]{oldMaterials[0],material}:new[]{material};
        records.Add(new Record{renderer=r.name,role=role,source=material.name,unityMin=r.bounds.min,unityMax=r.bounds.max,sourceVertices=original.vertexCount,outputVertices=mesh.vertexCount,insideTriangles=inside.Count/3,repeatMetres=metres});
        return new BraxR2ProgressEvidence.Binding{renderer=r,filter=filter,beforeMesh=original,afterMesh=mesh,beforeMaterials=oldMaterials,afterMaterials=r.sharedMaterials};
    }
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate"); if(!root)throw new Exception("Original estate missing");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var plaster=Material("WarmMineralPlaster","Assets/Source/City/R2Segment/Stucco",new Color(.965f,.945f,.905f));
        var brick=Material("OriginalBrickPlinth","Assets/Source/City/R2Segment/117",new Color(.86f,.83f,.78f));
        var paving=Material("PavingStones151","Assets/Source/City/R2Segment/Paving",Color.white);
        var roofSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/R2Approved/R2RoofTiles.mat");
        if(!roofSource)throw new Exception("Verified Poly Haven roof source missing");
        var roof=new Material(roofSource){name="R2 dom04 / real roof tiles / 2m",enableInstancing=true};
        AssetDatabase.CreateAsset(roof,Output+"/RoofTiles.mat");
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
        var records=new List<Record>(); var bindings=new List<BraxR2ProgressEvidence.Binding>();
        foreach(var task in new[]{("EstateMesh_327",plaster,"exterior stucco",1.5f),("EstateMesh_312",plaster,"entrance plaster",1.5f),("EstateMesh_321",brick,"brick plinth",3f),("EstateMesh_323",roof,"pitched roof",2f),("EstateMesh_275",paving,"bounded existing approach",5.4f)})
        {
            var r=renderers.Single(x=>x.name==task.Item1);
            bindings.Add(Bind(r,task.Item2,task.Item3,task.Item4,records,task.Item1=="EstateMesh_275"));
        }
        var evidence=new GameObject("R2ProgressEvidence").AddComponent<BraxR2ProgressEvidence>(); evidence.bindings=bindings.ToArray();
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/r2-segment-bindings.json",JsonUtility.ToJson(new Audit{bindings=records.ToArray()},true));
        Debug.Log("R2_DOM04_SCOPED_PBR_IMPLEMENTED__VISUAL_REVIEW_PENDING " + records.Count);
    }
}
