using System;
using UnityEngine;

// Executed by the authorized estate generator; no Unity Test Framework dependency.
public static class BraxTestSterowaniaOsiedla
{
    static void Require(bool condition,string message)
    {if(!condition)throw new InvalidOperationException("Regresja sterowania osiedla: "+message);}

    public static void Wykonaj()
    {
        var root=new GameObject("test IK kierowcy");
        try
        {
            var a=new GameObject("ramie").transform;a.SetParent(root.transform,false);
            var b=new GameObject("lokiec").transform;b.SetParent(a,false);b.localPosition=Vector3.forward*.42f;
            var c=new GameObject("dlon").transform;c.SetParent(b,false);c.localPosition=Vector3.forward*.37f;
            root.transform.SetPositionAndRotation(new Vector3(25f,2f,-17f),Quaternion.Euler(0,127f,0));
            root.transform.localScale=Vector3.one*1.294f;
            float upper=Vector3.Distance(a.position,b.position),lower=Vector3.Distance(b.position,c.position);
            foreach(var local in new[]{new Vector3(-.25f,-.2f,.55f),new Vector3(.25f,-.15f,.55f),new Vector3(0,0,.3f)})
            {
                var target=root.transform.TransformPoint(local);
                Require(BraxKierowcaIK.Rozwiaz(a,b,c,target,root.transform.TransformPoint(new Vector3(-.5f,-.5f,0))),"osiągalny chwyt");
                Require(Vector3.Distance(c.position,target)<.001f,"dokładność dłoni przy obrocie i skali auta");
                Require(Mathf.Abs(Vector3.Distance(a.position,b.position)-upper)<.0001f && Mathf.Abs(Vector3.Distance(b.position,c.position)-lower)<.0001f,"brak rozciągania kości");
            }
            Require(!BraxKierowcaIK.Rozwiaz(a,b,c,a.position+Vector3.forward*10,Vector3.down),"nieosiągalny chwyt musi być zgłoszony");
            Require(Mathf.Abs(Vector3.Distance(b.position,c.position)-lower)<.0001f,"brak wydłużania ręki do odległego celu");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}

        var points=new Vector3[201];for(int i=0;i<points.Length;i++)points[i]=new Vector3(0,0,i*.5f);
        var route=new[]{new BraxTrasaAut{nazwa="test hamowania",start=0,ile=201,dl=100,krok=.5f}};
        var sim=new BraxRuchAutSym(points,route,new BraxZdarzenieAut[0],new BraxWezelAut[0],42);
        sim.pytajStrefy=false;sim.widoczne=_=>true;
        sim.Dodaj(0,4f,5f,2f,3f,-1.5f,12f);sim.UstawStart();
        // A 4 cm wall across the lane, supplied through the same adapter as scene physics.
        sim.przeszkodaSceny=(p,f,hw,hl)=>Mathf.Abs(p.z-25f)<hl+.02f;
        bool approached=false;
        for(int i=0;i<300;i++)
        {
            sim.Krok(.1f);var car=sim.auta[0];
            Require(car.poz.z+car.dl*.5f<25f,"auto przejechało przez cienką przeszkodę");
            if(car.poz.z>14f)approached=true;
        }
        Require(approached && sim.auta[0].v<.1f,"dojazd i zatrzymanie przed przeszkodą");
        sim.przeszkodaSceny=null;
        for(int i=0;i<60;i++)sim.Krok(.1f);
        Require(sim.auta[0].v>1f,"wznowienie jazdy po zwolnieniu drogi");
        Debug.Log("BRAXHORN_STEROWANIE_REGRESJA_OK IK=chwyt_i_dlugosci_kosci ruch=hamowanie_i_wznowienie");
    }
}
