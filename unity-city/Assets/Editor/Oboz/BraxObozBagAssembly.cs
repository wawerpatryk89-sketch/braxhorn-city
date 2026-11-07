using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Owner GLB: 57 parts, one PBR material, 26,950 triangles. Combine once without
// decimation; scale ONLY against the measured leather body, never the chains.
public static class BraxObozBagAssembly
{
    public const float K=1.294f;
    public const string BodyNode="Low_Poly_lambert1_0";
    public const string OriginalSha256="09aa34aea1d750f5931a07def5bd6547fa29afac4cf4e921783c093fa9190f61";
    const string CanonicalSource="Assets/Source/Prywatne/Oboz/OwnerAssets/leather_punching_bag.glb";
    const string Generated="Assets/PlayableGenerated/Oboz/Environment/TrainingAssets";
    static readonly Dictionary<string,Prototype> Cache=new Dictionary<string,Prototype>();

    public sealed class Prototype
    {
        public Mesh mesh;
        public Material material;
        public Bounds body;
        public int bodyVertexCount,attachmentVertex,sourceTriangles;
        public string sourcePath,dependencyHash;
        public float HeightMetres => mesh.bounds.size.y;
        public float MountAboveFloorMetres => .6f-body.min.y;
    }

    static string PathOf(Transform root,Transform node)
    {
        string path=node.name;
        while(node.parent && node.parent!=root){node=node.parent;path=node.name+"/"+path;}
        return path;
    }

    static Bounds VertexBounds(MeshFilter filter,Matrix4x4 matrix)
    {
        var vertices=filter.sharedMesh.vertices;
        if(vertices.Length==0)throw new InvalidOperationException("Brak wierzchołków worka: "+filter.name);
        var bounds=new Bounds(matrix.MultiplyPoint3x4(vertices[0]),Vector3.zero);
        for(int i=1;i<vertices.Length;i++)bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[i]));
        return bounds;
    }

    public static Prototype Prepare(GameObject original)
    {
        if(!original)throw new InvalidOperationException("Brak dostarczonego oryginału worka");
        string path=AssetDatabase.GetAssetPath(original);
        string hash=AssetDatabase.GetAssetDependencyHash(path).ToString();
        // G may externalize the exact same GLB geometry into a generated import.
        // Key by its actual dependencies, so a new material cannot reuse an old one.
        string key=path+"|"+hash+"|"+AssetDatabase.GetAssetDependencyHash(CanonicalSource);
        if(Cache.TryGetValue(key,out var cached) && cached.mesh && cached.material)return cached;
        using(var bytes=File.OpenRead(CanonicalSource))
        using(var sha=SHA256.Create())
            if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=OriginalSha256)
                throw new InvalidOperationException("Oryginał worka nie odpowiada zmierzonemu profilowi SHA-256");
        GameObject probe=null;Mesh combined=null;
        try
        {
            probe=Object.Instantiate(original);
            probe.name="temporary original bag measurement";
            probe.hideFlags=HideFlags.HideAndDontSave;
            var source=probe.GetComponentsInChildren<MeshFilter>(true)
                .Where(f=>f.sharedMesh && f.GetComponent<MeshRenderer>()).ToArray();
            var bodies=source.Where(f=>f.name==BodyNode).ToArray();
            if(bodies.Length!=1 || source.Length!=57)
                throw new InvalidOperationException("Worek wymaga zweryfikowanego profilu: "+
                    path+" body="+bodies.Length+" meshParts="+source.Length);
            var body=bodies[0];
            var ordered=new[]{body}.Concat(source.Where(f=>f!=body)
                .OrderBy(f=>PathOf(probe.transform,f.transform),StringComparer.Ordinal)).ToArray();
            var materials=ordered.SelectMany(f=>f.GetComponent<MeshRenderer>().sharedMaterials).Distinct().ToArray();
            if(materials.Length!=1 || !materials[0])
                throw new InvalidOperationException("Oryginalny worek nie ma dokładnie jednego materiału PBR");
            // Include the imported root's axis conversion and unit scale. A
            // prefab root may carry a necessary GLB orientation transform.
            Matrix4x4 frame=Matrix4x4.identity;
            var bodyMatrix=frame*body.transform.localToWorldMatrix;
            var bodyBounds=VertexBounds(body,bodyMatrix);
            var fullBounds=bodyBounds;
            foreach(var filter in ordered.Skip(1))
                fullBounds.Encapsulate(VertexBounds(filter,frame*filter.transform.localToWorldMatrix));
            if(bodyBounds.size.y<=.001f)throw new InvalidOperationException("Niepoprawna wysokość korpusu źródłowego");
            float factor=1.2f/bodyBounds.size.y;
            Vector3 origin=new Vector3(bodyBounds.center.x,fullBounds.max.y,bodyBounds.center.z);
            Matrix4x4 normalize=Matrix4x4.TRS(-origin*factor,Quaternion.identity,Vector3.one*factor);
            var inputs=new CombineInstance[ordered.Length];int vertices=0,triangles=0;
            for(int i=0;i<ordered.Length;i++)
            {
                var mesh=ordered[i].sharedMesh;
                if(mesh.subMeshCount!=1 || mesh.GetTopology(0)!=MeshTopology.Triangles)
                    throw new InvalidOperationException("Nieoczekiwana topologia oryginalnego worka: "+ordered[i].name);
                inputs[i]=new CombineInstance{mesh=mesh,subMeshIndex=0,
                    transform=normalize*frame*ordered[i].transform.localToWorldMatrix};
                vertices+=mesh.vertexCount;triangles+=checked((int)mesh.GetIndexCount(0))/3;
            }
            combined=new Mesh{name="OBÓZ oryginalny Leather — wszystkie części bez decymacji"};
            if(vertices>65535)combined.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(inputs,true,true,false);
            // Do not infer a vertex range from undocumented ordering. Verify
            // all positions/UV/normals/tangents in the actual combined output.
            var actual=combined.vertices;var uv=combined.uv;
            var sourceBody=body.sharedMesh.vertices;var sourceUv=body.sharedMesh.uv;
            if(actual.Length!=vertices || combined.GetIndexCount(0)!=(uint)(triangles*3))
                throw new InvalidOperationException("Łączenie zmieniło liczbę wierzchołków/trójkątów worka");
            var normals=combined.normals;var tangents=combined.tangents;int at=0;
            for(int part=0;part<ordered.Length;part++)
            {
                var mesh=ordered[part].sharedMesh;
                var inputPositions=mesh.vertices;var inputUv=mesh.uv;
                var inputNormals=mesh.normals;var inputTangents=mesh.tangents;
                var matrix=inputs[part].transform;var normalMatrix=matrix.inverse.transpose;
                if(inputUv.Length!=inputPositions.Length || inputNormals.Length!=inputPositions.Length ||
                    inputTangents.Length!=inputPositions.Length || normals.Length!=vertices || tangents.Length!=vertices)
                    throw new InvalidOperationException("Oryginalny worek ma niekompletne kanały PBR");
                for(int v=0;v<inputPositions.Length;v++,at++)
                {
                    Vector3 tangent=matrix.MultiplyVector(new Vector3(inputTangents[v].x,
                        inputTangents[v].y,inputTangents[v].z)).normalized;
                    Vector3 actualTangent=new Vector3(tangents[at].x,tangents[at].y,tangents[at].z).normalized;
                    float handedness=inputTangents[v].w*Mathf.Sign(matrix.determinant);
                    if((actual[at]-matrix.MultiplyPoint3x4(inputPositions[v])).sqrMagnitude>1e-10f ||
                        (uv[at]-inputUv[v]).sqrMagnitude>1e-12f ||
                        (normals[at].normalized-normalMatrix.MultiplyVector(inputNormals[v]).normalized).sqrMagnitude>1e-8f ||
                        (actualTangent-tangent).sqrMagnitude>1e-8f || Mathf.Abs(tangents[at].w-handedness)>.0001f)
                        throw new InvalidOperationException("Łączenie zmieniło oryginalny kanał siatki: "+ordered[part].name+" vertex="+v);
                }
            }
            var measured=new Bounds(actual[0],Vector3.zero);
            for(int i=1;i<sourceBody.Length;i++)measured.Encapsulate(actual[i]);
            int attachment=0;
            for(int i=1;i<actual.Length;i++)if(actual[i].y>actual[attachment].y)attachment=i;
            if(Mathf.Abs(measured.size.y-1.2f)>.00001f || Mathf.Abs(actual[attachment].y)>.00001f)
                throw new InvalidOperationException("Kalibracja rzeczywistego korpusu lub zawieszenia niezgodna");
            combined.RecalculateBounds();
            Mesh saved=BraxObozStableMesh.Save(combined);combined=null;
            if(!Directory.Exists(Generated)){Directory.CreateDirectory(Generated);AssetDatabase.Refresh();}
            string materialPath=Generated+"/OwnerLeather-"+hash+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material)
            {
                material=new Material(materials[0]){name="Leather właściciela / wspólny oryginalny PBR",enableInstancing=true};
                AssetDatabase.CreateAsset(material,materialPath);
            }
            var result=new Prototype{mesh=saved,material=material,body=measured,
                bodyVertexCount=sourceBody.Length,attachmentVertex=attachment,
                sourceTriangles=triangles,sourcePath=path,dependencyHash=hash};
            Cache[key]=result;
            Debug.Log("BRAXHORN_OBOZ_BAG_SOURCE bodyVertices="+sourceBody.Length+
                " bodyMetres="+measured.size.y+" assemblyMetres="+result.HeightMetres+
                " mountMetres="+result.MountAboveFloorMetres+" triangles="+triangles+
                " sourceUnchanged=true decimation=false");
            return result;
        }
        finally
        {
            if(combined)Object.DestroyImmediate(combined);
            if(probe)Object.DestroyImmediate(probe);
        }
    }

    public static bool TryFloor(Vector3 approximateFloor,Transform exclude,out RaycastHit floor)
    {
        floor=default;Physics.SyncTransforms();
        var hits=Physics.RaycastAll(approximateFloor+Vector3.up*5f*K,Vector3.down,8f*K,
            ~0,QueryTriggerInteraction.Ignore);
        bool found=false;
        foreach(var hit in hits)
        {
            if(!hit.collider || (exclude && hit.collider.transform.IsChildOf(exclude)) ||
                hit.normal.y<.7f || hit.point.y>approximateFloor.y+.4f*K)continue;
            if(found && hit.point.y<=floor.point.y)continue;
            floor=hit;found=true;
        }
        return found;
    }

    public static BraxWorek Create(Transform parent,string name,Prototype prototype,
        RaycastHit floor,Collider beam)
    {
        if(!prototype.mesh || !floor.collider || !beam)
            throw new InvalidOperationException("Brak oryginalnej siatki albo potwierdzonego podparcia worka");
        GameObject instance=null;
        try
        {
            instance=new GameObject(name);
            var pivot=instance.transform;pivot.SetParent(parent,false);
            Vector3 scale=parent?parent.lossyScale:Vector3.one;
            if(scale.x<=0 || Mathf.Abs(scale.x-scale.y)>.0001f || Mathf.Abs(scale.x-scale.z)>.0001f)
                throw new InvalidOperationException("Niejednorodna skala rodzica worka; nie wolno deformować modelu");
            pivot.localScale=Vector3.one*(K/scale.x);
            pivot.position=floor.point+Vector3.up*(prototype.MountAboveFloorMetres*K);
            var filter=instance.AddComponent<MeshFilter>();filter.sharedMesh=prototype.mesh;
            var renderer=instance.AddComponent<MeshRenderer>();renderer.sharedMaterial=prototype.material;
            var collider=instance.AddComponent<CapsuleCollider>();collider.direction=1;
            collider.center=prototype.body.center;
            collider.height=prototype.body.size.y;
            collider.radius=Mathf.Max(prototype.body.extents.x,prototype.body.extents.z);
            var bag=instance.AddComponent<BraxWorek>();
            bag.gora=-prototype.body.max.y*K;bag.dol=-prototype.body.min.y*K;
            bag.dlugosc=-prototype.body.center.y*K;bag.promien=collider.radius*K;
            var geometry=instance.AddComponent<BraxObozBagGeometry>();
            geometry.Configure(filter,0,prototype.bodyVertexCount,prototype.attachmentVertex,
                prototype.sourcePath,OriginalSha256,BodyNode,floor.collider,beam);
            Physics.SyncTransforms();
            if(!geometry.TryMeasureAttachment(out var attachment) ||
                !BraxObozSupportSurface.TryMeasure(beam,attachment,out var mounting) || mounting.distanceWorld>.03f*K)
                throw new InvalidOperationException("Oryginalne zawieszenie nie dotyka drewnianej belki");
            if(!geometry.TryMeasureBodyBounds(out var body) ||
                Mathf.Abs(body.size.y/K-1.2f)>.001f ||
                Mathf.Abs((body.min.y-floor.point.y)/K-.6f)>.001f)
                throw new InvalidOperationException("Pomiar geometrii worka po montażu niezgodny z wymiarami właściciela");
            return bag;
        }
        catch{if(instance)Object.DestroyImmediate(instance);throw;}
    }
}
