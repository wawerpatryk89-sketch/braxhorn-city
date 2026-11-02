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
            // 2.11 dyspozytor: wysokość głowy z pozycji KOŚCI (świat), nie z BakeMesh+TransformPoint (skala liczona dwukrotnie:
            // build #42 wyliczył prześwit 4.13 m). Czubek głowy = najwyższa kość (HeadTop_End) + margines na włosy/skórę.
            foreach(var r in player.visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach(var b in r.bones) if(b) headroom=Mathf.Max(headroom,b.position.y-hipsOffset.y);
            headroom+=.12f*K;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            anim.GetClip("Idle").SampleAnimation(anim.gameObject,0);
            player.transform.SetPositionAndRotation(oldPos,oldRot);
        }
        float Przeswit(AutoSzablon t)=>t.go.GetComponentsInChildren<BraxFotelAuta>().Where(f=>f.name==FotelKierowcy).Select(f=>f.przeswit).DefaultIfEmpty(0f).Max();
        var template=templates.Where(t=>t.kol==4 && Przeswit(t)>=headroom+.04f*K)
            .OrderBy(t=>t.id=="car_s4"?0:1).ThenBy(t=>t.dl).FirstOrDefault();
        if(template==null)
        {
            template=templates.Where(t=>t.kol==4).OrderByDescending(Przeswit).FirstOrDefault();
            Debug.LogError("BRAXHORN_AUTO_PATRYKA DO POPRAWKI: żadna kabina nie ma prześwitu "+(headroom+.04f*K).ToString("F2")+" m — wzięta najwyższa: "+(template!=null?template.id+" "+Przeswit(template).ToString("F2"):"brak"));
            if(template==null)return;
        }
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
        Debug.LogError("BRAXHORN_AUTO_PATRYKA DO POPRAWKI: nie znaleziono wolnej zatoki przy ARENA BRAXHORN");
    }
}
