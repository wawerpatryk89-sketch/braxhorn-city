using System;
using UnityEngine;

// Editor measurement against the actual support surface. In particular,
// MeshCollider.convex=false never reaches Collider/Physics.ClosestPoint.
public static class BraxObozSupportSurface
{
    public const int MaximumMeshTriangles=250000;
    [Serializable] public sealed class Measurement
    {
        public bool measured;
        public string method,status;
        public Vector3 closestWorld;
        public float distanceWorld=-1f;
        public int trianglesTested,triangleIndex=-1;
    }
    static bool Finite(Vector3 p)=>
        !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
        !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
        !float.IsNaN(p.z) && !float.IsInfinity(p.z);

    static Vector3 Segment(Vector3 p,Vector3 a,Vector3 b)
    {
        var edge=b-a;float length=edge.sqrMagnitude;
        return length<=1e-20f?a:a+edge*Mathf.Clamp01(Vector3.Dot(p-a,edge)/length);
    }
    static Vector3 Triangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
    {
        var ab=b-a;var ac=c-a;
        float longest=Mathf.Max(ab.sqrMagnitude,Mathf.Max(ac.sqrMagnitude,(c-b).sqrMagnitude));
        if(longest<=1e-20f || Vector3.Cross(ab,ac).sqrMagnitude<=1e-14f*longest*longest)
        {
            var first=Segment(p,a,b);var second=Segment(p,b,c);var third=Segment(p,c,a);
            if((second-p).sqrMagnitude<(first-p).sqrMagnitude)first=second;
            return (third-p).sqrMagnitude<(first-p).sqrMagnitude?third:first;
        }
        var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0 && d2<=0)return a;
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
        if(d3>=0 && d4<=d3)return b;
        float vc=d1*d4-d3*d2;
        if(vc<=0 && d1>=0 && d3<=0)return a+ab*(d1/(d1-d3));
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
        if(d6>=0 && d5<=d6)return c;
        float vb=d5*d2-d1*d6;
        if(vb<=0 && d2>=0 && d6<=0)return a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;
        if(va<=0 && d4-d3>=0 && d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
        float inverse=1f/(va+vb+vc);return a+ab*(vb*inverse)+ac*(vc*inverse);
    }
    static bool Finish(Measurement result,Vector3 point,Vector3 closest,string method)
    {
        if(!Finite(closest)) {result.status="niezmierzone: niefinitywna powierzchnia";return false;}
        result.closestWorld=closest;result.distanceWorld=Vector3.Distance(point,closest);
        result.measured=true;result.method=method;result.status="zmierzona rzeczywista powierzchnia";
        return true;
    }
    static bool MeshSurface(MeshCollider collider,Vector3 point,Measurement result)
    {
        var mesh=collider.sharedMesh;
        if(!mesh){result.status="niezmierzone: brak siatki podpory";return false;}
        ulong indices=0;
        for(int i=0;i<mesh.subMeshCount;i++)
        {
            if(mesh.GetTopology(i)!=MeshTopology.Triangles)
            {result.status="niezmierzone: topologia podpory inna niż trójkąty";return false;}
            indices+=mesh.GetIndexCount(i);
        }
        if(indices==0 || indices%3!=0 || indices/3>MaximumMeshTriangles)
        {result.status="niezmierzone: pusty mesh lub budżet "+MaximumMeshTriangles+" trójkątów przekroczony";return false;}
        var vertices=mesh.vertices;var triangles=mesh.triangles;
        var world=new Vector3[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            world[i]=collider.transform.TransformPoint(vertices[i]);
            if(!Finite(world[i])){result.status="niezmierzone: nieprawidłowy wierzchołek podpory";return false;}
        }
        float best=float.PositiveInfinity;var closest=Vector3.zero;
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
            if(a<0 || b<0 || c<0 || a>=world.Length || b>=world.Length || c>=world.Length)
            {result.status="niezmierzone: indeks podpory poza siatką";return false;}
            var candidate=Triangle(point,world[a],world[b],world[c]);
            float squared=(candidate-point).sqrMagnitude;result.trianglesTested++;
            if(squared<best){best=squared;closest=candidate;result.triangleIndex=i/3;}
        }
        if(float.IsInfinity(best)){result.status="niezmierzone: brak poprawnych trójkątów";return false;}
        return Finish(result,point,closest,"actual MeshCollider sharedMesh triangles");
    }
    static bool BoxSurface(BoxCollider box,Vector3 point,Measurement result)
    {
        var half=box.size*.5f;var vertices=new Vector3[8];
        for(int i=0;i<vertices.Length;i++)vertices[i]=box.transform.TransformPoint(box.center+
            new Vector3((i&1)==0?-half.x:half.x,(i&2)==0?-half.y:half.y,(i&4)==0?-half.z:half.z));
        var triangles=new[]{0,2,3,0,3,1,4,5,7,4,7,6,0,1,5,0,5,4,
            2,6,7,2,7,3,0,4,6,0,6,2,1,3,7,1,7,5};
        float best=float.PositiveInfinity;var closest=Vector3.zero;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var candidate=Triangle(point,vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]);
            float distance=(point-candidate).sqrMagnitude;result.trianglesTested++;
            if(distance<best){best=distance;closest=candidate;result.triangleIndex=i/3;}
        }
        return Finish(result,point,closest,"BoxCollider six actual faces");
    }

    public static bool TryMeasure(Collider support,Vector3 worldPoint,out Measurement measurement)
    {
        measurement=new Measurement {status="niezmierzone: brak aktywnej podpory lub poprawnego punktu"};
        if(!support || !support.enabled || support.isTrigger ||
            !support.gameObject.activeInHierarchy || !Finite(worldPoint))return false;
        try
        {
            if(support is MeshCollider mesh)return MeshSurface(mesh,worldPoint,measurement);
            if(support is BoxCollider box)return BoxSurface(box,worldPoint,measurement);
            var scale=support.transform.lossyScale;
            scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            if(support is SphereCollider sphere)
            {
                var centre=sphere.transform.TransformPoint(sphere.center);
                float radius=sphere.radius*Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
                var direction=worldPoint-centre;
                if(direction.sqrMagnitude<1e-16f)direction=Vector3.up;
                return Finish(measurement,worldPoint,centre+direction.normalized*radius,"SphereCollider surface");
            }
            if(support is CapsuleCollider capsule)
            {
                int axis=capsule.direction;
                var localAxis=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
                var direction=capsule.transform.rotation*localAxis;
                var centre=capsule.transform.TransformPoint(capsule.center);
                float along=axis==0?scale.x:axis==1?scale.y:scale.z;
                float across=axis==0?Mathf.Max(scale.y,scale.z):axis==1?Mathf.Max(scale.x,scale.z):Mathf.Max(scale.x,scale.y);
                float radius=capsule.radius*across;
                float half=Mathf.Max(0,capsule.height*along*.5f-radius);
                var onAxis=Segment(worldPoint,centre-direction*half,centre+direction*half);
                var outward=worldPoint-onAxis;
                if(outward.sqrMagnitude<1e-16f)
                    outward=capsule.transform.rotation*(axis==0?Vector3.up:Vector3.right);
                return Finish(measurement,worldPoint,onAxis+outward.normalized*radius,"CapsuleCollider surface");
            }
            measurement.status="niezmierzone: nieobsługiwany typ podpory "+support.GetType().Name;
            return false;
        }
        catch(Exception ex)
        {
            measurement.measured=false;measurement.distanceWorld=-1f;
            measurement.status="niezmierzone: "+ex.GetType().Name+": "+ex.Message;
            return false;
        }
    }
}
