using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Editor-only subtraction of bounded renovation volumes. The imported source
// meshes remain immutable; UVs, source lighting, normals and material slots are
// interpolated on the exact cut boundary. In particular, a metro opening must
// never be made by disabling a whole merged promenade renderer.
public static class BraxCityMeshCut
{
    const float Epsilon = .00001f;
    struct Vertex
    {
        public Vector3 p, n, world;
        public Vector4 tangent;
        public Vector2 uv, uv2;
        public Color color;
        public int original;
        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
            return new Vertex { p=Vector3.LerpUnclamped(a.p,b.p,t), world=Vector3.LerpUnclamped(a.world,b.world,t),
                n=Vector3.LerpUnclamped(a.n,b.n,t).normalized, tangent=Vector4.LerpUnclamped(a.tangent,b.tangent,t),
                uv=Vector2.LerpUnclamped(a.uv,b.uv,t), uv2=Vector2.LerpUnclamped(a.uv2,b.uv2,t),
                color=Color.LerpUnclamped(a.color,b.color,t), original=-1 };
        }
    }
    static float Distance(Vector3 p, Bounds b, int plane)
    {
        switch(plane) { case 0:return p.x-b.min.x;case 1:return b.max.x-p.x;
            case 2:return p.y-b.min.y;case 3:return b.max.y-p.y;
            case 4:return p.z-b.min.z;default:return b.max.z-p.z; }
    }
    static List<Vertex> Half(List<Vertex> input, Bounds b, int plane, bool inside)
    {
        var output=new List<Vertex>(input.Count+1);
        if(input.Count==0)return output;
        Vertex a=input[input.Count-1];float da=Distance(a.world,b,plane);bool keepA=inside?da>=-Epsilon:da<=Epsilon;
        foreach(var v in input)
        {
            float dv=Distance(v.world,b,plane);bool keep=inside?dv>=-Epsilon:dv<=Epsilon;
            if(keep!=keepA)
            {
                float denom=da-dv;
                if(Mathf.Abs(denom)>Epsilon)output.Add(Vertex.Lerp(a,v,Mathf.Clamp01(da/denom)));
            }
            if(keep)output.Add(v);
            a=v;da=dv;keepA=keep;
        }
        return output;
    }
    static bool TriangleOutside(Vector3 a, Vector3 b, Vector3 c, Bounds cut)
    {
        for(int plane=0;plane<6;plane++)
            if(Distance(a,cut,plane)<-Epsilon&&Distance(b,cut,plane)<-Epsilon&&Distance(c,cut,plane)<-Epsilon)return true;
        return false;
    }
    public static int Subtract(Transform root, Bounds volume, string outputDirectory, string label)
    {
        Directory.CreateDirectory(outputDirectory);AssetDatabase.Refresh();
        int altered=0;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();var source=filter.sharedMesh;
            if(!renderer||!renderer.enabled||!source||!renderer.bounds.Intersects(volume))continue;
            var positions=source.vertices;var normals=source.normals;var tangents=source.tangents;
            var uv=source.uv;var uv2=source.uv2;var colors=source.colors;
            var vertices=new Vertex[positions.Length];
            for(int i=0;i<positions.Length;i++)vertices[i]=new Vertex {p=positions[i],world=filter.transform.TransformPoint(positions[i]),
                n=normals.Length==positions.Length?normals[i]:Vector3.up,tangent=tangents.Length==positions.Length?tangents[i]:new Vector4(1,0,0,1),
                uv=uv.Length==positions.Length?uv[i]:Vector2.zero,uv2=uv2.Length==positions.Length?uv2[i]:Vector2.zero,
                color=colors.Length==positions.Length?colors[i]:Color.white,original=i};
            var result=new List<Vertex>();var oldToNew=new Dictionary<int,int>();
            var submeshes=new List<int[]>();bool changed=false;
            Func<Vertex,int> add=v=> {int index;if(v.original>=0&&oldToNew.TryGetValue(v.original,out index))return index;
                index=result.Count;result.Add(v);if(v.original>=0)oldToNew.Add(v.original,index);return index;};
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var input=source.GetTriangles(sub);var indices=new List<int>(input.Length);
                Action<List<Vertex>> emit=polygon=> {
                    for(int k=1;k<polygon.Count-1;k++)
                    {
                        var a=polygon[0];var b=polygon[k];var c=polygon[k+1];
                        if(Vector3.Cross(b.p-a.p,c.p-a.p).sqrMagnitude<1e-14f)continue;
                        indices.Add(add(a));indices.Add(add(b));indices.Add(add(c));
                    }
                };
                for(int j=0;j<input.Length;j+=3)
                {
                    var a=vertices[input[j]];var b=vertices[input[j+1]];var c=vertices[input[j+2]];
                    if(TriangleOutside(a.world,b.world,c.world,volume)){indices.Add(add(a));indices.Add(add(b));indices.Add(add(c));continue;}
                    // Successive outside pieces are disjoint. The final inside
                    // polygon is discarded, so no triangles cross the opening.
                    var remainder=new List<Vertex>{a,b,c};
                    for(int plane=0;plane<6&&remainder.Count>0;plane++)
                    {emit(Half(remainder,volume,plane,false));remainder=Half(remainder,volume,plane,true);}
                    changed=true;
                }
                submeshes.Add(indices.ToArray());
            }
            if(!changed)continue;
            altered++;
            int triangles=0;foreach(var indices in submeshes)triangles+=indices.Length;
            if(triangles==0)
            {
                renderer.enabled=false;
                foreach(var col in filter.GetComponents<Collider>())col.enabled=false;
                continue;
            }
            var mesh=new Mesh {name=source.name+"_"+label,indexFormat=result.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            var pOut=new Vector3[result.Count];var nOut=new Vector3[result.Count];var tOut=new Vector4[result.Count];
            var uOut=new Vector2[result.Count];var u2Out=new Vector2[result.Count];var cOut=new Color[result.Count];
            for(int i=0;i<result.Count;i++){var v=result[i];pOut[i]=v.p;nOut[i]=v.n;tOut[i]=v.tangent;uOut[i]=v.uv;u2Out[i]=v.uv2;cOut[i]=v.color;}
            mesh.vertices=pOut;mesh.normals=nOut;mesh.tangents=tOut;mesh.uv=uOut;mesh.uv2=u2Out;mesh.colors=cOut;
            mesh.subMeshCount=submeshes.Count;for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i);
            mesh.RecalculateBounds();
            var path=AssetDatabase.GenerateUniqueAssetPath(outputDirectory+"/"+filter.name+"_"+label+".asset");
            AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;
            foreach(var collider in filter.GetComponents<MeshCollider>())collider.sharedMesh=mesh;
        }
        Debug.Log("CITY_RENOVATION_CUT "+label+" meshes="+altered+" volume="+volume);
        return altered;
    }
    public static int SubtractBoxColliders(Transform root, Bounds volume)
    {
        int count=0;
        foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if(!c.enabled||c.isTrigger||!c.bounds.Intersects(volume))continue;
            if(Quaternion.Angle(c.transform.rotation,Quaternion.identity)>.01f)
                throw new InvalidOperationException("Cannot axis-clip rotated source collision: "+c.name);
            Bounds b=c.bounds;Vector3 lo=Vector3.Max(b.min,volume.min),hi=Vector3.Min(b.max,volume.max);
            if(hi.x-lo.x<Epsilon||hi.y-lo.y<Epsilon||hi.z-lo.z<Epsilon)continue;
            Action<Vector3,Vector3> add=(a,z)=> {
                Vector3 size=z-a;if(size.x<Epsilon||size.y<Epsilon||size.z<Epsilon)return;
                var piece=new GameObject(c.name+" renovation remainder");piece.transform.SetParent(root,true);piece.transform.position=(a+z)*.5f;
                piece.AddComponent<BoxCollider>().size=size;
            };
            add(b.min,new Vector3(lo.x,b.max.y,b.max.z));
            add(new Vector3(hi.x,b.min.y,b.min.z),b.max);
            add(new Vector3(lo.x,b.min.y,b.min.z),new Vector3(hi.x,lo.y,b.max.z));
            add(new Vector3(lo.x,hi.y,b.min.z),new Vector3(hi.x,b.max.y,b.max.z));
            add(new Vector3(lo.x,lo.y,b.min.z),new Vector3(hi.x,hi.y,lo.z));
            add(new Vector3(lo.x,lo.y,hi.z),new Vector3(hi.x,hi.y,b.max.z));
            c.enabled=false;count++;
        }
        return count;
    }
}
