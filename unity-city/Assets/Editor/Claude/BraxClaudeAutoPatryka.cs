using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    static void AutoPatryka(List<AutoSzablon> templates)
    {
        var player=UnityEngine.Object.FindAnyObjectByType<BraxGymPlayer>();
        if(!player||!player.visual)throw new InvalidOperationException("Nie ma Patryka do dopasowania prywatnego auta");
        var anim=player.visual.GetComponent<Animation>();
        if(!anim||!anim.GetClip("CarIdle"))throw new InvalidOperationException("Brak pozy kierowcy Patryka");
        var oldPos=player.transform.position;var oldRot=player.transform.rotation;
        Vector3 hipsOffset=Vector3.zero;float headroom=0;
        var mesh=new Mesh();
        try
        {
            player.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            anim.GetClip("CarIdle").SampleAnimation(anim.gameObject,0);
            var hips=player.visual.GetComponentsInChildren<Transform>(true).First(t=>PatrykKosc(t.name)=="Hips");
            hipsOffset=hips.position;
            foreach(var r in player.visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.BakeMesh(mesh);
                foreach(var v in mesh.vertices)headroom=Mathf.Max(headroom,r.transform.TransformPoint(v).y-hipsOffset.y+.08f*K);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            anim.GetClip("Idle").SampleAnimation(anim.gameObject,0);
            player.transform.SetPositionAndRotation(oldPos,oldRot);
        }
        var template=templates.Where(t=>t.kol==4 && t.go.GetComponentsInChildren<BraxFotelAuta>().Any(f=>f.name==FotelKierowcy&&f.przeswit>=headroom+.04f*K))
            .OrderBy(t=>t.id=="car_s4"?0:1).ThenBy(t=>t.dl).FirstOrDefault();
        if(template==null)throw new InvalidOperationException("Żadna kabina nie mieści Patryka bez zmniejszania: wymagany prześwit "+headroom);
        Physics.SyncTransforms();
        foreach(var spot in MiejscaParkingowe.Where(m=>m.srodek.x < -65f && m.srodek.z>80f && m.srodek.z<97f)
            .OrderBy(m=>(m.srodek-new Vector3(-86f,0,81f)).sqrMagnitude))
        {
            if(template.dl>spot.rozmiar.y+.2f || template.szerNadw>spot.rozmiar.x-.15f)continue;
            var rotation=Quaternion.LookRotation(spot.przod);
            var half=new Vector3(template.szer*.5f+.05f,Mathf.Max(.2f,(template.wys-.3f)*.5f),template.dl*.5f+.2f);
            if(Physics.CheckBox(spot.srodek+Vector3.up*(.3f+half.y),half,rotation,~0,QueryTriggerInteraction.Ignore))continue;
            var go=AutoInstancja(template,Group("Auto Patryka / ARENA BRAXHORN"),"Auto Patryka · tylko gracz",new System.Random(180),out var color);
            go.transform.SetPositionAndRotation(spot.srodek,rotation);
            var box=go.AddComponent<BoxCollider>();box.center=template.obrys.center;box.size=template.obrys.size;
            var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var car=go.AddComponent<BraxAutoGracza>();car.obrys=box;
            car.fotel=go.GetComponentsInChildren<Transform>().First(t=>t.name==FotelKierowcy);
            car.przesuniecieGracza=go.transform.InverseTransformPoint(car.fotel.position)+Vector3.up*.08f*K-hipsOffset;
            car.rozstaw=template.rozstaw;car.promienKola=template.promienKola;
            car.kola=new[]{"LP","PP","LT","PT"}.Select(n=>go.transform.Find("koło "+n+"/obrót")).Where(t=>t).ToArray();
            car.skret=new[]{"LP","PP"}.Select(n=>go.transform.Find("koło "+n)).Where(t=>t).ToArray();
            rep.notes.Add("Auto Patryka przy ARENA BRAXHORN: "+template.id+", "+color+", "+spot.srodek+"; wsiadanie AKCJA, gaz/ster joystick, wyjście po zatrzymaniu. Bez dostępu NPC. Głowa nad siedzeniem="+headroom.ToString("F3")+", prześwit="+car.fotel.GetComponent<BraxFotelAuta>().przeswit.ToString("F3"));
            Physics.SyncTransforms();return;
        }
        throw new InvalidOperationException("Nie znaleziono wolnej zatoki dla auta Patryka przy ARENA BRAXHORN");
    }
}
