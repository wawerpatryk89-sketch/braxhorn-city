using System.Collections.Generic;
using UnityEngine;

// 23.11: the towers' granite forecourts (BraxChampionsWiezowce.Forecourts) cut out of the region's lawn exactly - the
// granite never lies on the grass in one plane (ZASADY 4.7: two surfaces in one place fight over depth and flicker).
// Its own file so the preview without Unity (wiezowce_preview) cuts the lawn with the same code.
public static class BraxChampionsLawnCut
{
    /// <summary>Cuts rectangles (world nominal x0, x1, z0, z1) out of a lawn mesh in game units: every triangle that
    /// touches one is split along the rectangle's lines (Sutherland-Hodgman, position and UV interpolated) and only its
    /// parts outside are kept. True when anything was cut.</summary>
    public static bool Cut(ref Vector3[] v,ref Vector2[] uv,ref int[] tri,List<Vector4> rects,float K)
    {
        var nv=new List<Vector3>(v);var nuv=new List<Vector2>(uv);var nt=new List<int>(tri.Length);bool cut=false;
        var R=new List<Vector4>();foreach(var r in rects)R.Add(new Vector4(r.x*K,r.y*K,r.z*K,r.w*K));
        List<(Vector3 p,Vector2 t)> Clip(List<(Vector3 p,Vector2 t)> poly,int axis,float c,bool keepLess)
        {
            var o=new List<(Vector3 p,Vector2 t)>();
            float F(Vector3 p)=>axis==0?p.x:p.z;
            bool In((Vector3 p,Vector2 t) q)=>keepLess?F(q.p)<=c:F(q.p)>=c;
            for(int i=0;i<poly.Count;i++)
            {
                var a=poly[i];var b=poly[(i+1)%poly.Count];bool ia=In(a),ib=In(b);
                if(ia)o.Add(a);
                if(ia!=ib){float s=(c-F(a.p))/(F(b.p)-F(a.p));o.Add((Vector3.Lerp(a.p,b.p,s),Vector2.Lerp(a.t,b.t,s)));}
            }
            return o;
        }
        float Area(List<(Vector3 p,Vector2 t)> q){float A=0;for(int i=0;i<q.Count;i++){var a=q[i].p;var b=q[(i+1)%q.Count].p;A+=a.x*b.z-b.x*a.z;}return Mathf.Abs(A)*.5f;}
        for(int f=0;f<tri.Length;f+=3)
        {
            var parts=new List<List<(Vector3 p,Vector2 t)>>{new List<(Vector3 p,Vector2 t)>{(v[tri[f]],uv[tri[f]]),(v[tri[f+1]],uv[tri[f+1]]),(v[tri[f+2]],uv[tri[f+2]])}};
            bool touched=false;
            foreach(var r in R)
            {
                var next=new List<List<(Vector3 p,Vector2 t)>>();
                foreach(var poly in parts)
                {
                    float x0=float.MaxValue,x1=float.MinValue,z0=float.MaxValue,z1=float.MinValue;
                    foreach(var q in poly){x0=Mathf.Min(x0,q.p.x);x1=Mathf.Max(x1,q.p.x);z0=Mathf.Min(z0,q.p.z);z1=Mathf.Max(z1,q.p.z);}
                    if(x1<=r.x||x0>=r.y||z1<=r.z||z0>=r.w){next.Add(poly);continue;}
                    touched=true;
                    // the parts outside: west of x0, east of x1 (between), south of z0, north of z1 (inside x)
                    var rest=poly;
                    var w=Clip(rest,0,r.x,true);if(w.Count>=3&&Area(w)>1e-6f)next.Add(w);rest=Clip(rest,0,r.x,false);
                    var e=Clip(rest,0,r.y,false);if(e.Count>=3&&Area(e)>1e-6f)next.Add(e);rest=Clip(rest,0,r.y,true);
                    var s=Clip(rest,1,r.z,true);if(s.Count>=3&&Area(s)>1e-6f)next.Add(s);rest=Clip(rest,1,r.z,false);
                    var n=Clip(rest,1,r.w,false);if(n.Count>=3&&Area(n)>1e-6f)next.Add(n);
                }
                parts=next;
            }
            if(!touched){nt.Add(tri[f]);nt.Add(tri[f+1]);nt.Add(tri[f+2]);continue;}
            cut=true;
            foreach(var poly in parts)                                    // convex, the original winding: a fan
            {
                int i0=nv.Count;foreach(var q in poly){nv.Add(q.p);nuv.Add(q.t);}
                for(int k=1;k+1<poly.Count;k++){nt.Add(i0);nt.Add(i0+k);nt.Add(i0+k+1);}
            }
        }
        if(!cut)return false;
        v=nv.ToArray();uv=nuv.ToArray();tri=nt.ToArray();return true;
    }
}
