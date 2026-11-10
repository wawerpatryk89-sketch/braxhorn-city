using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Railings of the waterfront, rebuilt 10.11 for the owner ("promenada, barierki tam, gdzie trzeba zrobić"):
// * the promenade quay edge (z -150, x 700-1150): the promenade drops 2.2 m to the harbour water and had NO railing -
//   now a glass balustrade (stainless posts every 1.6 m, round stainless handrail at 1.08 m, clear glass infill) on a
//   granite coping, open only at the gangway gates; both ends of the promenade are closed too (x 50 and x 1150);
// * the gangways from the promenade down to the yacht and Riva pontoons: stainless handrails following the slope;
// * the jetski ramp along the quay wall: seaward handrail and its west end;
// * the heads of the fixed quays (ferry, cruise) and the ferry quay's west face (no berth there).
// Finger pontoons and the berth faces stay open: people step aboard there.
// One combined mesh per material (BraxChampionsMeshBatch) + thin box colliders per straight run.
public static class BraxChampionsHarbourSafety
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const float Height=1.08f,PostStep=1.6f;
    static Material glass,coping;
    public static int Runs,Posts;

    static float RampY(BraxChampionsFoundation.Pier p,float x,float z)
    {
        if(p.role=="quay_ramp")return .16f-1.24f*Mathf.Clamp01((-z-150f)/20f);
        if(p.role=="quay_ramp_along")return .16f-1.53f*Mathf.Clamp01((x-868f)/15f);
        if(p.role=="quay_jetski")return BraxChampionsHarbour.SkiPontoonY;
        if(p.role=="quay_wood")return BraxChampionsHarbour.PontoonY;
        return BraxChampionsHarbour.QuayY;
    }

    /// <summary>Glass balustrade on a straight level run a->b (nominal metres, a.y = walking surface).</summary>
    static void GlassRun(Transform parent,BraxChampionsMeshBatch m,string name,Vector3 a,Vector3 b)
    {
        float len=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));if(len<.3f)return;
        int n=Mathf.Max(1,Mathf.CeilToInt(len/PostStep));
        var dir=(b-a)/n;var steel=BraxChampionsHarbour.Steel;
        var side=Vector3.Cross(Vector3.up,(b-a).normalized);
        for(int i=0;i<=n;i++)
        {
            var p=a+dir*i;
            m.Cylinder(steel,p,.032f,Height,12);
            m.Cylinder(steel,p,.055f,.03f,12);Posts++;
            if(i==n)break;
            var q=p+dir;
            // glass panel between the posts, 4 cm clear of each post
            var g0=p+(q-p).normalized*.06f;var g1=q-(q-p).normalized*.06f;
            var c=(g0+g1)*.5f+Vector3.up*.56f;float w=Vector3.Distance(g0,g1);
            m.Box(glass,c,new Vector3(w,.88f,.016f),Quaternion.LookRotation(side,Vector3.up));
            // two clamps per panel side
            foreach(float h in new[]{.25f,.85f})
            {
                m.Box(steel,p+(q-p).normalized*.05f+Vector3.up*h,new Vector3(.05f,.06f,.04f),Quaternion.LookRotation(side,Vector3.up));
                m.Box(steel,q-(q-p).normalized*.05f+Vector3.up*h,new Vector3(.05f,.06f,.04f),Quaternion.LookRotation(side,Vector3.up));
            }
        }
        m.Tube(steel,a+Vector3.up*Height,b+Vector3.up*Height,.028f,10);
        float yaw=Mathf.Atan2(b.x-a.x,b.z-a.z)*Mathf.Rad2Deg;
        BraxChampionsMeshBatch.Collider(parent,name+" | kolizja",(a+b)*.5f+Vector3.up*Height*.5f,new Vector3(.08f,Height,len),yaw);
        Runs++;
    }

    /// <summary>Stainless handrail on posts following a sloped walking surface (gangways, ramp).</summary>
    static void HandrailRun(Transform parent,BraxChampionsMeshBatch m,string name,BraxChampionsFoundation.Pier p,Vector3 a,Vector3 b)
    {
        float len=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));if(len<.3f)return;
        int n=Mathf.Max(1,Mathf.CeilToInt(len/2.2f));var steel=BraxChampionsHarbour.Steel;
        Vector3 At(float t){var q=Vector3.Lerp(a,b,t);q.y=RampY(p,q.x,q.z);return q;}
        for(int i=0;i<=n;i++){m.Cylinder(steel,At((float)i/n),.03f,1.0f,10);Posts++;}
        for(int i=0;i<n;i++)
        {
            var p0=At((float)i/n);var p1=At((float)(i+1)/n);
            m.Tube(steel,p0+Vector3.up*1.0f,p1+Vector3.up*1.0f,.026f,10);
            m.Tube(steel,p0+Vector3.up*.52f,p1+Vector3.up*.52f,.016f,8);
            float yaw=Mathf.Atan2(p1.x-p0.x,p1.z-p0.z)*Mathf.Rad2Deg;
            BraxChampionsMeshBatch.Collider(parent,name+" | kolizja "+i,(p0+p1)*.5f+Vector3.up*.5f,new Vector3(.06f,1.0f,Vector3.Distance(p0,p1)),yaw);
        }
        Runs++;
    }

    public static void Generate(Transform harbour,BraxChampionsFoundation.Pier[] piers,BraxChampionsFoundation.Opening[] openings)
    {
        if(!harbour||piers==null)throw new InvalidOperationException("Railings need the harbour and its piers");
        Runs=Posts=0;
        glass=BraxChampionsHarbour.Surface("balustrade_glass",new Color(.80f,.89f,.93f,.24f),.04f,0f,null,true);
        coping=BraxChampionsFoundation.SourceMaterial("prom_quay_coping_granite","granite_tile",1.6f,new Color(.88f,.88f,.86f));
        var colliders=new GameObject("Barierki - kolizje").transform;colliders.SetParent(harbour,false);
        var m=new BraxChampionsMeshBatch();
        const float Y=.16f,Z=-149.85f;

        // promenade quay edge: coping + glass balustrade, open at every gate (gate posts stand at x0-.15 / x1+.15)
        m.BoxMinMax(coping,700f,1150f,Y,Y+.03f,-150.08f,-149.55f);
        var cuts=openings.OrderBy(o=>o.x0).Select(o=>(o.x0-.15f,o.x1+.15f)).ToList();
        float start=700.1f;
        foreach(var (c0,c1) in cuts)
        {
            if(c0>start)GlassRun(colliders,m,"Balustrada promenady "+start.ToString("0")+"-"+c0.ToString("0"),new Vector3(start,Y+.03f,Z),new Vector3(c0,Y+.03f,Z));
            start=Mathf.Max(start,c1);
        }
        GlassRun(colliders,m,"Balustrada promenady "+start.ToString("0")+"-1150",new Vector3(start,Y+.03f,Z),new Vector3(1149.85f,Y+.03f,Z));
        // closed ends of the promenade (beyond them there is no ground)
        GlassRun(colliders,m,"Balustrada - wschodni koniec promenady",new Vector3(1149.85f,Y,Z),new Vector3(1149.85f,Y,-120.2f));
        GlassRun(colliders,m,"Balustrada - zachodni koniec promenady",new Vector3(50.15f,Y,-160.85f),new Vector3(50.15f,Y,-120.2f));

        foreach(var p in piers)
        {
            float x0=p.x,x1=p.x+p.w,z0=p.z,z1=p.z+p.d;
            if(p.role=="quay_ramp")
            {
                HandrailRun(colliders,m,p.id+" poręcz zachodnia",p,new Vector3(x0+.08f,0,z1-.05f),new Vector3(x0+.08f,0,z0));
                HandrailRun(colliders,m,p.id+" poręcz wschodnia",p,new Vector3(x1-.08f,0,z1-.05f),new Vector3(x1-.08f,0,z0));
            }
            else if(p.role=="quay_ramp_along")
            {
                HandrailRun(colliders,m,p.id+" poręcz od morza",p,new Vector3(x0+.05f,0,z0+.08f),new Vector3(x1,0,z0+.08f));
                HandrailRun(colliders,m,p.id+" poręcz zachodnia",p,new Vector3(x0+.08f,0,z0+.08f),new Vector3(x0+.08f,0,z1-.12f));
            }
            else if(p.role=="quay_cruise")
                GlassRun(colliders,m,p.id+" głowica",new Vector3(x0+.15f,Y,z0+.15f),new Vector3(x1-.15f,Y,z0+.15f));
            else if(p.role=="quay_ferry")
            {
                GlassRun(colliders,m,p.id+" głowica",new Vector3(x0+.15f,Y,z0+.15f),new Vector3(x1-.15f,Y,z0+.15f));
                GlassRun(colliders,m,p.id+" strona zachodnia",new Vector3(x0+.15f,Y,z0+.15f),new Vector3(x0+.15f,Y,z1-.3f));
            }
        }
        m.Build(harbour,"Barierki: balustrada promenady, poręcze trapów i ramp, głowice nabrzeży",Output+"/Meshes/Harbour");
        Debug.Log("CHAMPIONS_RAILINGS_10_11 runs="+Runs+" posts="+Posts+" openings="+openings.Length+" glassBalustrade=promenade+quays handrails=gangways+jetskiRamp");
    }
}
