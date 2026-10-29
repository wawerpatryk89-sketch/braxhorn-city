using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public static partial class BraxCityMaterials
{
    const string MeshFolder = Generated + "/LitMeshes";
    const int MaxBakeTriangles = 2000000;
    const int MaxCachedSamples = 131072;
    const float RayBias = .015f;
    const float AmbientDistance = 2.2f;
    const float SunDistance = 350f;
    const float BakeMarker = 2718f;

    [Serializable]
    public sealed class BakeReport
    {
        public string kind = "editor-only vertex sun visibility and six-ray ambient visibility";
        public string root, unity;
        public int receivers, occluderRenderers, triangles, vertices, samples, cacheHits;
        public int channel3Meshes, channel7Meshes, rebuiltNormals;
        public long rays, alphaTexels;
        public double seconds;
        public bool sourceMeshesModified = false, globalLightingModified = false;
        public string limitation = "Spatial detail follows receiver vertices; this is not a lightmap/GI bake or a mobile performance result.";
    }

    public static BakeReport Bake(Transform newlyAuthoredRoot)
    { return Bake(newlyAuthoredRoot, Array.Empty<Transform>()); }

    /// <summary>
    /// Bake only authored renderers using CityStaticPBR. Additional roots contribute
    /// read-only occlusion. No Physics/RenderSettings/Light changes and no runtime BVH.
    /// </summary>
    public static BakeReport Bake(Transform newlyAuthoredRoot, Transform[] occluderRoots)
    { return Bake(newlyAuthoredRoot,occluderRoots,true); }

    /// <summary>
    /// The complete city uses audited coarse *lighting-only* silhouette
    /// proxies. This is independent of actual visible facade meshes, whose
    /// PBR and geometry are left intact. The source city itself can still
    /// supply real occlusion. Never silently raise the 2M triangle BVH guard.
    /// </summary>
    public static BakeReport Bake(Transform newlyAuthoredRoot, Transform[] occluderRoots,
        bool includeAuthoredGeometryAsOccluder)
    {
        if (!newlyAuthoredRoot) throw new ArgumentNullException(nameof(newlyAuthoredRoot));
        if (EditorApplication.isPlaying) throw new InvalidOperationException("City lighting is an offline Editor operation.");
        var timer = Stopwatch.StartNew();
        var report = new BakeReport { root = newlyAuthoredRoot.name, unity = Application.unityVersion };
        var receivers = new List<MeshFilter>();
        foreach (var filter in newlyAuthoredRoot.GetComponentsInChildren<MeshFilter>(false))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!Eligible(filter, renderer)) continue;
            // Massive high-rise curtain walls already receive accurate PBR
            // per-pixel sun/sky lighting in CityStaticPBR. Vertex AO on every
            // mullion and every tiny stone side would consume millions of
            // rays, without improving phone-visible detail.
            if (IsHighFrequencyDynamicLightingFallback(filter.transform)) continue;
            foreach (var material in renderer.sharedMaterials)
                if (IsCityMaterial(material)) { receivers.Add(filter); break; }
        }
        if (receivers.Count == 0) throw new InvalidOperationException("No authored CityStaticPBR receivers under " + newlyAuthoredRoot.name);
        // Validate channels before generating replacements. Editor snapshots also
        // support sources imported with runtime Read/Write disabled.
        foreach (var filter in receivers)
        {
            BakeChannel(filter.sharedMesh);
        }
        var occluders = new List<MeshFilter>();
        var seen = new HashSet<MeshFilter>();
        if(includeAuthoredGeometryAsOccluder)
            AddOccluders(newlyAuthoredRoot, occluders, seen);
        foreach (var root in occluderRoots ?? Array.Empty<Transform>())
            AddOccluders(root,occluders,seen);
        Debug.Log("CITY_LIGHTING_OCCLUDER_POLICY authoredDetail="+
            includeAuthoredGeometryAsOccluder+" supplementaryRoots="+
            (occluderRoots==null?0:occluderRoots.Length)+
            " prioritizedReceivers="+receivers.Count);
        EnsureFolder(MeshFolder);
        var samples = new Dictionary<SampleKey, Vector2>();
        try
        {
            using (var world = new BakeWorld(occluders, report))
            {
                Debug.Log("CITY_STATIC_BAKE_BEGIN receivers=" + receivers.Count + " occluders="
                    + report.occluderRenderers + " triangles=" + report.triangles);
                for (int meshIndex = 0; meshIndex < receivers.Count; meshIndex++)
                {
                    var filter = receivers[meshIndex];
                    var source = filter.sharedMesh;
                    int channel = BakeChannel(source);
                    var copy = CopyForBake(source);
                    copy.name = source.name + "_CityStaticLight";
                    bool saved = false;
                    try
                    {
                        if (!copy.HasVertexAttribute(VertexAttribute.Normal))
                        { copy.RecalculateNormals(); report.rebuiltNormals++; }
                        Vector3[] positions = copy.vertices, normals = copy.normals;
                        if (normals.Length != positions.Length) throw new InvalidOperationException("Incomplete normals: " + filter.name);
                        var light = new List<Vector4>(positions.Length);
                        Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                        Matrix4x4 normalMatrix = matrix.inverse.transpose;
                        for (int vertex = 0; vertex < positions.Length; vertex++)
                        {
                            Vector3 position = matrix.MultiplyPoint3x4(positions[vertex]);
                            Vector3 normal = normalMatrix.MultiplyVector(normals[vertex]).normalized;
                            if (normal.sqrMagnitude < .5f) throw new InvalidOperationException("Zero normal in " + filter.name);
                            var key = new SampleKey(position,normal);
                            if (!samples.TryGetValue(key,out var result))
                            {
                                result = SampleLighting(world,position,normal,report);
                                if (samples.Count >= MaxCachedSamples) samples.Clear();
                                samples[key] = result;
                                report.samples++;
                            }
                            else report.cacheHits++;
                            light.Add(new Vector4(result.x,result.y,1,BakeMarker));
                            if ((vertex & 8191) == 0 && !Application.isBatchMode
                                && EditorUtility.DisplayCancelableProgressBar("Braxhorn city static lighting",
                                    filter.name + " — " + report.samples + " samples",
                                    (meshIndex+(float)vertex/Math.Max(1,positions.Length))/receivers.Count))
                                throw new OperationCanceledException("City static lighting cancelled; original sources were not modified.");
                        }
                        copy.SetUVs(channel,light);
                        string identity = newlyAuthoredRoot.gameObject.scene.name + "/" + HierarchyPath(filter.transform);
                        string path = MeshFolder + "/lit_" + Hash128.Compute(identity) + ".asset";
                        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (existing)
                        {
                            EditorUtility.CopySerialized(copy,existing);
                            UnityEngine.Object.DestroyImmediate(copy);
                            copy = existing;
                            EditorUtility.SetDirty(existing);
                        }
                        else AssetDatabase.CreateAsset(copy,path);
                        saved = true;
                        if (!Application.isBatchMode) Undo.RecordObject(filter,"Bake authored city lighting");
                        filter.sharedMesh = copy;
                        EditorUtility.SetDirty(filter);
                        report.vertices += positions.Length;
                        report.receivers++;
                        if (channel == 3) report.channel3Meshes++; else report.channel7Meshes++;
                    }
                    finally { if (!saved && copy) UnityEngine.Object.DestroyImmediate(copy); }
                    if ((meshIndex+1)%25 == 0) Debug.Log("CITY_STATIC_BAKE_PROGRESS " + (meshIndex+1) + "/" + receivers.Count);
                }
            }
            timer.Stop(); report.seconds = timer.Elapsed.TotalSeconds;
            EnsureFolder(Generated + "/Lighting");
            string reportPath = Generated + "/Lighting/" + SafeName(newlyAuthoredRoot.name) + ".json";
            File.WriteAllText(reportPath,JsonUtility.ToJson(report,true));
            AssetDatabase.ImportAsset(reportPath);
            AssetDatabase.SaveAssets();
            Debug.Log("CITY_STATIC_BAKE_COMPLETE " + JsonUtility.ToJson(report));
            return report;
        }
        finally
        {
            samples.Clear();
            if (!Application.isBatchMode) EditorUtility.ClearProgressBar();
        }
    }

    static bool IsHighFrequencyDynamicLightingFallback(Transform t)
    {
        // This selector affects only optional offline vertex AO, not scene
        // geometry, renderers, materials, collision or real-time movement.
        for(Transform p=t;p;p=p.parent)
            if(p.name.StartsWith("High-rise ",StringComparison.Ordinal))
                return true;
        var name=t.name;
        return name.Contains("pavers") || name.Contains("stone variation") ||
               name.Contains("street lantern") || name.Contains("Streetlamp");
    }

    static bool Eligible(MeshFilter filter, MeshRenderer renderer)
    {
        if (!filter || !filter.sharedMesh || !renderer || !renderer.enabled || !filter.gameObject.activeInHierarchy) return false;
        for (Transform t=filter.transform;t;t=t.parent)
        {
            if (t.gameObject.layer == 8 || t.GetComponent<SkinnedMeshRenderer>() || t.GetComponent<Animator>()
                || t.GetComponent<Animation>() || t.GetComponent<Rigidbody>() || t.GetComponent("BraxCityDoor")
                || t.GetComponent("BraxCityMovingLiftPart") || t.name == "Original opening door") return false;
        }
        return true;
    }
    static void AddOccluders(Transform root, List<MeshFilter> output, HashSet<MeshFilter> seen)
    {
        if (!root) return;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(false))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!Eligible(filter,renderer) || !seen.Add(filter)) continue;
            var lod = filter.GetComponentInParent<LODGroup>();
            if (lod)
            {
                var levels = lod.GetLODs();
                if (levels.Length > 0 && Array.IndexOf(levels[0].renderers,renderer) < 0) continue;
            }
            output.Add(filter);
        }
    }
    static Mesh CopyForBake(Mesh source)
    {
        if(source.isReadable) return UnityEngine.Object.Instantiate(source);
        if(source.blendShapeCount>0 || source.HasVertexAttribute(VertexAttribute.BlendWeight))
            throw new InvalidOperationException("Animated geometry is not a static city bake target: "+source.name);
        var copy=new Mesh {name=source.name};
        var writable=Mesh.AllocateWritableMeshData(1);
        bool applied=false;
        try
        {
            using(var snapshot=MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var read=snapshot[0]; var write=writable[0];
                write.SetVertexBufferParams(source.vertexCount,source.GetVertexAttributes());
                for(int stream=0;stream<source.vertexBufferCount;stream++)
                    read.GetVertexData<byte>(stream).CopyTo(write.GetVertexData<byte>(stream));
                int indexStride=source.indexFormat==IndexFormat.UInt32?4:2;
                write.SetIndexBufferParams(read.GetIndexData<byte>().Length/indexStride,source.indexFormat);
                read.GetIndexData<byte>().CopyTo(write.GetIndexData<byte>());
                write.subMeshCount=source.subMeshCount;
                for(int sub=0;sub<source.subMeshCount;sub++)
                    write.SetSubMesh(sub,read.GetSubMesh(sub),MeshUpdateFlags.DontRecalculateBounds);
            }
            Mesh.ApplyAndDisposeWritableMeshData(writable,copy,MeshUpdateFlags.DontRecalculateBounds);
            applied=true; copy.bounds=source.bounds;
            return copy;
        }
        catch {UnityEngine.Object.DestroyImmediate(copy);throw;}
        finally {if(!applied) writable.Dispose();}
    }
    static int BakeChannel(Mesh mesh)
    {
        foreach (int channel in new[] { 3,7 })
        {
            var attribute = channel == 3 ? VertexAttribute.TexCoord3 : VertexAttribute.TexCoord7;
            if (!mesh.HasVertexAttribute(attribute)) return channel;
            if (AssetDatabase.GetAssetPath(mesh).StartsWith(MeshFolder + "/",StringComparison.Ordinal))
            {
                var values = new List<Vector4>(); mesh.GetUVs(channel,values);
                if (values.Count == mesh.vertexCount && values.Count > 0
                    && Mathf.Abs(values[0].w-BakeMarker) < .1f && values[0].z == 1) return channel;
            }
        }
        throw new InvalidOperationException("Both UV channels 3 and 7 are occupied by source data: " + mesh.name
            + ". No UV data was overwritten; allocate a city lighting channel explicitly.");
    }
    static string HierarchyPath(Transform transform)
    {
        string path = "";
        for (var t=transform;t;t=t.parent) path = t.name + "[" + t.GetSiblingIndex() + "]/" + path;
        return path;
    }
    static readonly Vector3[] AmbientRays = {
        new Vector3(0,0,1), new Vector3(.8660254f,0,.5f),
        new Vector3(.2676166f,.8236391f,.5f), new Vector3(-.7006293f,.5090369f,.5f),
        new Vector3(-.7006293f,-.5090369f,.5f), new Vector3(.2676166f,-.8236391f,.5f)
    };
    static Vector2 SampleLighting(BakeWorld world, Vector3 position, Vector3 normal, BakeReport report)
    {
        // Cast towards the sun from its side of the surface, including thin two-sided leaves.
        Vector3 sunOrigin = position + normal*(Vector3.Dot(normal,SunDirection)>=0 ? RayBias : -RayBias)
                            + SunDirection*.003f;
        float sun = world.Occluded(sunOrigin,SunDirection,SunDistance) ? 0 : 1;
        Vector3 tangent = Vector3.Cross(Mathf.Abs(normal.y)>.9f ? Vector3.forward : Vector3.up,normal).normalized;
        Vector3 bitangent = Vector3.Cross(normal,tangent);
        Vector3 origin = position+normal*RayBias;
        int visible = 0;
        foreach (var ray in AmbientRays)
        {
            Vector3 direction = tangent*ray.x+bitangent*ray.y+normal*ray.z;
            if (!world.Occluded(origin,direction,AmbientDistance)) visible++;
        }
        report.rays += 1+AmbientRays.Length;
        return new Vector2(sun,(float)visible/AmbientRays.Length);
    }
    struct SampleKey : IEquatable<SampleKey>
    {
        readonly int x,y,z,nx,ny,nz;
        public SampleKey(Vector3 p, Vector3 n)
        { x=Mathf.RoundToInt(p.x*200); y=Mathf.RoundToInt(p.y*200); z=Mathf.RoundToInt(p.z*200);
          nx=Mathf.RoundToInt(n.x*64); ny=Mathf.RoundToInt(n.y*64); nz=Mathf.RoundToInt(n.z*64); }
        public bool Equals(SampleKey v) { return x==v.x && y==v.y && z==v.z && nx==v.nx && ny==v.ny && nz==v.nz; }
        public override bool Equals(object v) { return v is SampleKey other && Equals(other); }
        public override int GetHashCode()
        { unchecked { int h=x; h=h*397^y; h=h*397^z; h=h*397^nx; h=h*397^ny; return h*397^nz; } }
    }

    // All of this data is Editor-only and released after Bake. Repeated runtime
    // geometry stays modular. No colliders, geometry changes, or scene physics queries.
    sealed class BakeWorld : IDisposable
    {
        struct Triangle
        {
            public Vector3 p,e1,e2;
            public Vector3 uvA,uvB,uvC; // z holds vertex alpha, where used by the source shader.
            public int mask;
            public Vector3 Center { get { return p+(e1+e2)/3; } }
        }
        struct Node { public Vector3 min,max; public int start,count,left,right; }
        readonly Triangle[] triangles;
        readonly List<Node> nodes = new List<Node>();
        readonly List<AlphaMask> masks = new List<AlphaMask>();
        readonly Dictionary<Material,int> maskIds = new Dictionary<Material,int>();
        readonly Dictionary<Texture,AlphaPixels> alphaImages = new Dictionary<Texture,AlphaPixels>();
        readonly BakeReport report;
        int used;

        public BakeWorld(List<MeshFilter> filters, BakeReport report)
        {
            this.report=report;
            long capacity=0;
            foreach (var f in filters)
            {
                var materials=f.GetComponent<MeshRenderer>().sharedMaterials;
                for(int s=0;s<f.sharedMesh.subMeshCount;s++)
                    if(f.sharedMesh.GetTopology(s)==MeshTopology.Triangles && AlphaMode(Slot(materials,s))!=2)
                        capacity+=(long)f.sharedMesh.GetIndexCount(s)/3;
                if(capacity>MaxBakeTriangles) throw new InvalidOperationException("City bake exceeds "
                    +MaxBakeTriangles+" static occluder triangles. Select a proper source LOD or smaller module set; nothing was silently simplified.");
            }
            triangles=new Triangle[(int)capacity];
            foreach(var filter in filters) AddMesh(filter);
            if(used>0) BuildNode(0,used);
            report.triangles=used;
        }
        static Material Slot(Material[] materials,int index)
        { return materials.Length==0 ? null : materials[Math.Min(index,materials.Length-1)]; }
        void AddMesh(MeshFilter filter)
        {
            // A temporary byte-preserving Editor copy is bounded to one mesh and
            // lets non-readable imported occluders remain entirely untouched.
            var mesh=filter.sharedMesh.isReadable?filter.sharedMesh:CopyForBake(filter.sharedMesh);
            try { AddMeshData(filter,mesh); }
            finally {if(mesh!=filter.sharedMesh) UnityEngine.Object.DestroyImmediate(mesh);}
        }
        void AddMeshData(MeshFilter filter,Mesh mesh)
        {
            var vertices=mesh.vertices;
            var uv0=mesh.uv; var uv1=mesh.uv2;
            var colours=mesh.colors32;
            var materials=filter.GetComponent<MeshRenderer>().sharedMaterials;
            Matrix4x4 matrix=filter.transform.localToWorldMatrix;
            for(int i=0;i<vertices.Length;i++) vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
            int before=used;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                Material material=Slot(materials,sub);
                int mode=AlphaMode(material);
                if(mode==2 || mesh.GetTopology(sub)!=MeshTopology.Triangles) continue;
                int mask=mode==1 ? GetMask(material) : -1;
                var alpha=mask>=0 ? masks[mask] : null;
                var indices=mesh.GetIndices(sub);
                for(int j=0;j+2<indices.Length;j+=3)
                {
                    int a=indices[j],b=indices[j+1],c=indices[j+2];
                    Vector3 p=vertices[a], e1=vertices[b]-p, e2=vertices[c]-p;
                    if(Vector3.Cross(e1,e2).sqrMagnitude<1e-14f) continue;
                    var tri=new Triangle { p=p,e1=e1,e2=e2,mask=mask };
                    if(alpha!=null)
                    {
                        var uv=alpha.uvSet==1 ? uv1 : uv0;
                        if(uv.Length!=vertices.Length && alpha.pixels!=null)
                            throw new InvalidOperationException("Alpha mask UV"+alpha.uvSet+" missing: "+filter.name);
                        tri.uvA=alpha.TransformUV(uv.Length==vertices.Length?uv[a]:Vector2.zero,
                            colours.Length==vertices.Length?colours[a].a/255f:1);
                        tri.uvB=alpha.TransformUV(uv.Length==vertices.Length?uv[b]:Vector2.zero,
                            colours.Length==vertices.Length?colours[b].a/255f:1);
                        tri.uvC=alpha.TransformUV(uv.Length==vertices.Length?uv[c]:Vector2.zero,
                            colours.Length==vertices.Length?colours[c].a/255f:1);
                    }
                    triangles[used++]=tri;
                }
            }
            if(used>before) report.occluderRenderers++;
        }
        int GetMask(Material material)
        {
            if(maskIds.TryGetValue(material,out int id)) return id;
            var mask=new AlphaMask(material);
            if(mask.texture)
            {
                if(!alphaImages.TryGetValue(mask.texture,out var pixels))
                {
                    pixels=new AlphaPixels(mask.texture);
                    report.alphaTexels+=(long)pixels.width*pixels.height;
                    if(report.alphaTexels>64L*1024*1024) throw new InvalidOperationException("City bake alpha cache exceeds 64 MiB; supply smaller city alpha-mask derivatives.");
                    alphaImages.Add(mask.texture,pixels);
                }
                mask.pixels=pixels;
            }
            id=masks.Count; masks.Add(mask); maskIds.Add(material,id); return id;
        }
        int BuildNode(int start,int count)
        {
            Vector3 min=triangles[start].p,max=min,cMin=triangles[start].Center,cMax=cMin;
            for(int i=start;i<start+count;i++)
            {
                var t=triangles[i]; min=Vector3.Min(min,Vector3.Min(t.p,Vector3.Min(t.p+t.e1,t.p+t.e2)));
                max=Vector3.Max(max,Vector3.Max(t.p,Vector3.Max(t.p+t.e1,t.p+t.e2)));
                cMin=Vector3.Min(cMin,t.Center); cMax=Vector3.Max(cMax,t.Center);
            }
            int nodeIndex=nodes.Count;
            var node=new Node {min=min,max=max,start=start,count=count,left=-1,right=-1}; nodes.Add(node);
            if(count<=8) return nodeIndex;
            Vector3 size=cMax-cMin;
            int axis=size.x>=size.y && size.x>=size.z?0:size.y>=size.z?1:2;
            float pivot=(cMin[axis]+cMax[axis])*.5f;
            int lo=start,hi=start+count-1;
            while(lo<=hi)
            {
                if(triangles[lo].Center[axis]<pivot) lo++;
                else {var swap=triangles[lo];triangles[lo]=triangles[hi];triangles[hi]=swap;hi--;}
            }
            int split=lo;
            if(split==start || split==start+count) split=start+count/2;
            node.count=0; node.left=BuildNode(start,split-start); node.right=BuildNode(split,start+count-split);
            nodes[nodeIndex]=node; return nodeIndex;
        }
        public bool Occluded(Vector3 origin,Vector3 direction,float distance)
        { return used>0 && Trace(0,origin,direction,new Vector3(Inv(direction.x),Inv(direction.y),Inv(direction.z)),distance); }
        static float Inv(float v) { return Mathf.Abs(v)<1e-12f?0:1/v; }
        bool Trace(int index,Vector3 origin,Vector3 direction,Vector3 inverse,float distance)
        {
            var node=nodes[index];
            if(!HitBounds(node,origin,direction,inverse,distance)) return false;
            if(node.count==0) return Trace(node.left,origin,direction,inverse,distance)
                                  || Trace(node.right,origin,direction,inverse,distance);
            for(int i=node.start;i<node.start+node.count;i++)
            {
                var t=triangles[i]; Vector3 p=Vector3.Cross(direction,t.e2);
                float determinant=Vector3.Dot(t.e1,p);
                if(Mathf.Abs(determinant)<1e-10f) continue;
                float inv=1/determinant; Vector3 s=origin-t.p;
                float u=Vector3.Dot(s,p)*inv; if(u<0 || u>1) continue;
                Vector3 q=Vector3.Cross(s,t.e1); float v=Vector3.Dot(direction,q)*inv;
                if(v<0 || u+v>1) continue;
                float hit=Vector3.Dot(t.e2,q)*inv; if(hit<=.0005f || hit>=distance) continue;
                if(t.mask<0 || masks[t.mask].Blocks(t.uvA*(1-u-v)+t.uvB*u+t.uvC*v)) return true;
            }
            return false;
        }
        static bool HitBounds(Node n,Vector3 origin,Vector3 direction,Vector3 inverse,float maxDistance)
        {
            float near=0,far=maxDistance;
            for(int a=0;a<3;a++)
            {
                if(Mathf.Abs(direction[a])<1e-12f) {if(origin[a]<n.min[a] || origin[a]>n.max[a]) return false;continue;}
                float t0=(n.min[a]-origin[a])*inverse[a],t1=(n.max[a]-origin[a])*inverse[a];
                near=Mathf.Max(near,Mathf.Min(t0,t1)); far=Mathf.Min(far,Mathf.Max(t0,t1));
                if(far<near) return false;
            }
            return true;
        }
        public void Dispose() { nodes.Clear(); masks.Clear(); maskIds.Clear(); alphaImages.Clear(); }
    }

    sealed class AlphaMask
    {
        public readonly Texture texture;
        public readonly int uvSet;
        public AlphaPixels pixels;
        readonly Vector2 scale,offset,rotation;
        readonly float alpha,cutoff,vertexTint;
        public AlphaMask(Material material)
        {
            string property=material.HasProperty("baseColorTexture")?"baseColorTexture":material.HasProperty("_MainTex")?"_MainTex":"_BaseMap";
            if(material.HasProperty(property))
            {
                texture=material.GetTexture(property); scale=material.GetTextureScale(property); offset=material.GetTextureOffset(property);
                if(material.HasProperty(property+"_Rotation")) rotation=material.GetVector(property+"_Rotation");
                uvSet=Mathf.RoundToInt(Value(material,property+"_texCoord",0));
            }
            else scale=Vector2.one;
            string color=material.HasProperty("baseColorFactor")?"baseColorFactor":"_Color";
            alpha=material.HasProperty(color)?material.GetColor(color).a:1;
            cutoff=Value(material,"alphaCutoff",Value(material,"_Cutoff",.5f));
            vertexTint=IsCityMaterial(material)?Value(material,"_VertexTint",1):material.HasProperty("baseColorFactor")?1:0;
        }
        public Vector3 TransformUV(Vector2 uv,float vertexAlpha)
        { return new Vector3(uv.x*scale.x+uv.y*rotation.y+offset.x,uv.x*rotation.x+uv.y*scale.y+offset.y,
                             Mathf.Lerp(1,vertexAlpha,vertexTint)); }
        public bool Blocks(Vector3 uv)
        { return (pixels==null?1:pixels.Sample(uv.x,uv.y))*alpha*uv.z>=cutoff; }
    }
    sealed class AlphaPixels
    {
        public readonly int width,height;
        readonly byte[] values;
        readonly TextureWrapMode uWrap,vWrap;
        public AlphaPixels(Texture texture)
        {
            var input=texture as Texture2D;
            if(!input) throw new InvalidOperationException("Expected 2D alpha source: "+texture.name);
            Texture2D decoded=null;
            try
            {
                Color32[] colours;
                string path=AssetDatabase.GetAssetPath(input),extension=Path.GetExtension(path).ToLowerInvariant();
                if((extension==".png" || extension==".jpg" || extension==".jpeg") && File.Exists(path))
                {
                    decoded=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                    if(!decoded.LoadImage(File.ReadAllBytes(path),false)) throw new IOException("Cannot decode city alpha source: "+path);
                    width=decoded.width; height=decoded.height; colours=decoded.GetPixels32();
                }
                else if(input.isReadable) {width=input.width;height=input.height;colours=input.GetPixels32();}
                else throw new InvalidOperationException("Cannot read alpha without mutating original texture: "+path
                    +" / "+input.name+". Provide an external PNG city derivative for this masked occluder.");
                values=new byte[colours.Length];
                for(int i=0;i<colours.Length;i++) values[i]=colours[i].a;
                uWrap=texture.wrapModeU; vWrap=texture.wrapModeV;
            }
            finally {if(decoded) UnityEngine.Object.DestroyImmediate(decoded);}
        }
        static float Wrap(float v,TextureWrapMode mode)
        {
            if(mode==TextureWrapMode.Clamp) return Mathf.Clamp01(v);
            if(mode==TextureWrapMode.Mirror || mode==TextureWrapMode.MirrorOnce)
            {
                if(mode==TextureWrapMode.MirrorOnce) return Mathf.Clamp01(Mathf.Abs(v));
                float p=Mathf.Repeat(v,2); return p<=1?p:2-p;
            }
            return Mathf.Repeat(v,1);
        }
        public float Sample(float u,float v)
        {
            float x=Wrap(u,uWrap)*(width-1),y=Wrap(v,vWrap)*(height-1);
            int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y),x1=Math.Min(x0+1,width-1),y1=Math.Min(y0+1,height-1);
            float a=Mathf.Lerp(values[y0*width+x0],values[y0*width+x1],x-x0);
            float b=Mathf.Lerp(values[y1*width+x0],values[y1*width+x1],x-x0);
            return Mathf.Lerp(a,b,y-y0)/255f;
        }
    }
}
