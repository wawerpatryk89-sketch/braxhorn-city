using System.Collections.Generic;
using UnityEngine;

// Construction in source metres, converted once by K. Existing buildings,
// entrance locations and roundabout geometry are deliberately not moved.
public static partial class BraxObozEnvironment
{
    internal readonly struct EntranceApproach
    {
        public readonly string name;
        public readonly Vector2 start,end;
        public readonly float width,height;
        public EntranceApproach(string name,float ax,float az,float bx,float bz,
            float width,float height)
        {
            this.name=name;start=new Vector2(ax,az);end=new Vector2(bx,bz);
            this.width=width;this.height=height;
        }
    }

    internal static EntranceApproach[] EntranceApproachPlan()=>new[] {
        new EntranceApproach("Domek 1",-52.75f,-18f,-53.85f,-18f,1.6f,.13f),
        new EntranceApproach("Domek 2",-52.75f,-36f,-53.85f,-36f,1.6f,.13f),
        new EntranceApproach("Domek 3",-49.25f,-18f,-48.15f,-18f,1.6f,.13f),
        new EntranceApproach("Domek 4",-49.25f,-36f,-48.15f,-36f,1.6f,.13f),
        new EntranceApproach("Stołówka",-40f,-76.2f,-40f,-74.5f,1.10f,.16f),
        new EntranceApproach("Sauna",-47f,-89.7f,-47f,-88f,1.10f,.16f),
        new EntranceApproach("Punkt medyczny",-25f,-89.7f,-25f,-88f,1.10f,.16f),
        new EntranceApproach("Magazyn",-25f,-71.7f,-25f,-70f,1.10f,.16f),
        new EntranceApproach("WC / ŁAŹNIA",-66f,-44.9f,-66f,-47f,2.40f,.24f),
        new EntranceApproach("Siłownia pod dachem",-24.2f,39f,-26.5f,39f,2.40f,.22f)
    };

    static void EntranceApproaches()
    {
        var parent=Group("Ciągłe dojścia do każdego wejścia",root,Vector3.zero);
        foreach(var entry in EntranceApproachPlan())
        {
            // Timber construction, not a replacement furniture model.
            // Top and MeshCollider share the exact same rising surface.
            var start=new Vector3(entry.start.x,.016f,entry.start.y);
            var end=new Vector3(entry.end.x,entry.height,entry.end.y);
            var forward=end-start;forward.y=0f;forward.Normalize();
            var right=Vector3.Cross(Vector3.up,forward)*(entry.width*.5f);
            var a=start-right;var b=end-right;var c=end+right;var d=start+right;
            var aa=new Vector3(a.x,0f,a.z);var bb=new Vector3(b.x,0f,b.z);
            var cc=new Vector3(c.x,0f,c.z);var dd=new Vector3(d.x,0f,d.z);
            var vertices=new List<Vector3>(24);
            var uv=new List<Vector2>(24);var triangles=new List<int>(36);
            void Face(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3)
            {
                int offset=vertices.Count;
                vertices.Add(p0*K);vertices.Add(p1*K);vertices.Add(p2*K);vertices.Add(p3*K);
                float u=Vector3.Distance(p0,p1),v=Vector3.Distance(p1,p2);
                uv.Add(Vector2.zero);uv.Add(new Vector2(u,0));
                uv.Add(new Vector2(u,v));uv.Add(new Vector2(0,v));
                triangles.Add(offset);triangles.Add(offset+1);triangles.Add(offset+2);
                triangles.Add(offset);triangles.Add(offset+2);triangles.Add(offset+3);
            }
            Face(a,b,c,d);Face(dd,cc,bb,aa);
            Face(aa,bb,b,a);Face(d,c,cc,dd);
            Face(aa,a,d,dd);Face(b,bb,cc,c);
            var mesh=new Mesh {name="Dojście bez uskoku — "+entry.name};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            mesh=SaveMesh(mesh);
            var go=Group(mesh.name,parent,Vector3.zero).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=wood;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        Physics.SyncTransforms();
        Debug.Log("BRAXHORN_OBOZ_APPROACHES count="+EntranceApproachPlan().Length+
            " sourceMetres=true continuousSlope=true phoneVerified=false");
    }
}
