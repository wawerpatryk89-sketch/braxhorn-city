using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// AUTO PATRYKA (build 45). Właściciel 20:38: „zmienić mi auto na jakieś sportowe” — wybór z istniejących modeli sportowych
// (car_rev, car_918, car_r8, car_mus, car_dc; bez car_dia / car_mar z kogutem). Kolejność kryteriów:
//   1) wydzielone drzwi kierowcy (węzły modelu z „door” w nazwie po stronie fotela kierowcy) — animacja otwierania (E9),
//   2) Patryk 2.27 m mieści się za kierownicą przy odchyleniu ≤ 30° (mapa dachu nad fotelem, skóra głowy w pozie CarIdle,
//      zagłębienie 3–6 cm × K — jak ludzie w BraxClaudeKabiny; zapas ≥ 0),
//   3) wygląd sportowy (kolejność listy), 4) nie radiowozy.
// Stoi POD ARENĄ BRAXHORN (wejście x −88…−84, z 79): najbliższa wolna zatoka z MiejscaParkingowe, przodem do wyjazdu.
// Drzwi: obrys części drzwi → zawias na przedniej krawędzi; Revuelto — nożycowe (oś poprzeczna, unoszą się 65°), inne — oś
// pionowa 65° na zewnątrz. Żuk zostaje w ruchu i na parkingach. Modele z markami: oznaczenie „DO WYMIANY PRZED SPRZEDAŻĄ” bez zmian.
public static partial class BraxClaudeOsiedle
{
    static readonly string[] AutaSportowePatryka = { "car_rev", "car_918", "car_r8", "car_mus", "car_dc" };
    static readonly Vector3 WejscieAreny = new Vector3(-86f, 0f, 79f);

    static bool CzescDrzwi(Transform t, Transform korzen)
    {
        for (var x = t; x && x != korzen; x = x.parent) if (x.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
    // Najwyższe węzły części drzwi po stronie kierowcy (x tego samego znaku co fotel), w układzie auta.
    static List<Transform> DrzwiKierowcy(GameObject auto, Transform fotel, out Bounds obrys)
    {
        obrys = default; var wynik = new List<Transform>(); bool jest = false;
        float xf = auto.transform.InverseTransformPoint(fotel.position).x;
        foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
        {
            if (!CzescDrzwi(r.transform, auto.transform)) continue;
            var c = auto.transform.InverseTransformPoint(r.bounds.center);
            if (Mathf.Sign(c.x) != Mathf.Sign(xf) || Mathf.Abs(c.x) < .25f) continue;
            var top = r.transform; while (top.parent && top.parent != auto.transform && CzescDrzwi(top.parent, auto.transform)) top = top.parent;
            if (!wynik.Contains(top)) wynik.Add(top);
            foreach (var p in new[] { r.bounds.min, r.bounds.max })
            { var l = auto.transform.InverseTransformPoint(p); if (!jest) { obrys = new Bounds(l, Vector3.zero); jest = true; } else obrys.Encapsulate(l); }
        }
        return wynik;
    }

    static void AutoPatryka(List<AutoSzablon> templates)
    {
        var player=UnityEngine.Object.FindAnyObjectByType<BraxGymPlayer>();
        if(!player||!player.visual)throw new InvalidOperationException("Nie ma Patryka do dopasowania prywatnego auta");
        var anim=player.visual.GetComponent<Animation>();
        if(!anim||!anim.GetClip("CarIdle"))throw new InvalidOperationException("Brak pozy kierowcy Patryka");
        var oldPos=player.transform.position;var oldRot=player.transform.rotation;
        var kosci=player.visual.GetComponentsInChildren<Transform>(true);
        var hips=kosci.First(t=>PatrykKosc(t.name)=="Hips");
        var spine=kosci.FirstOrDefault(t=>PatrykKosc(t.name)=="Spine"); var head=kosci.FirstOrDefault(t=>PatrykKosc(t.name)=="Head");
        var skora=GlowaWierzcholki(player.visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.sharedMesh),head);
        Vector3 hipsOffset;
        var kandydaci=new List<(AutoSzablon t,bool drzwi,WynikKabiny w,bool miesci,string opis)>();
        try
        {
            player.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            anim.GetClip("CarIdle").SampleAnimation(anim.gameObject,0);
            hipsOffset=hips.position;
            foreach(string id in AutaSportowePatryka)
            {
                var t=templates.FirstOrDefault(q=>q.id==id&&q.kol==4); if(t==null){kandydaci.Add((null,false,default,false,id+": brak modelu"));continue;}
                var fotel=t.go.GetComponentsInChildren<Transform>(true).FirstOrDefault(q=>q.name==FotelKierowcy); var kab=fotel?fotel.GetComponent<BraxFotelAuta>():null;
                if(!fotel||!kab){kandydaci.Add((t,false,default,false,id+": brak fotela"));continue;}
                var drzwi=DrzwiKierowcy(t.go,fotel,out var ob);
                WynikKabiny w=new WynikKabiny { zapas=float.NegativeInfinity }; bool miesci=false;
                // Najpierw standardowe ustawienie. Dodatkowo badamy sportowe
                // siedzenie do 4.5 cm nizej; skala i szkielet Patryka bez zmian.
                // Log poprzedniego buildu: najblizszy pasujacy car_dc brak 3.1 cm.
                var pozycje = KabinaProby(kab.odchylenieBaza,42f,K);
                foreach (float z in new[]{.075f,.09f,.105f})
                    pozycje.Add((42f,z*K,10f));
                foreach(var (odch,zagl,poch) in pozycje)
                {
                    var rot=fotel.rotation; player.transform.SetPositionAndRotation(fotel.position+fotel.up*(.08f*K-zagl)-rot*hipsOffset,rot);
                    anim.GetClip("CarIdle").SampleAnimation(anim.gameObject,0);
                    BraxMieszkaniec.PozaWAucie(spine,head,player.transform.right,odch,KabinaWzrok*odch+poch);
                    var proba=new WynikKabiny{zapas=float.PositiveInfinity,odchylenie=odch,zaglebienie=zagl,pochylenie=poch};
                    ZapasPozy(skora,new Renderer[0],fotel,kab,ref proba);
                    if(proba.zapas>w.zapas)w=proba;
                    if(proba.zapas>=0f){miesci=true;break;}
                }
                kandydaci.Add((t,drzwi.Count>0,w,miesci,id+": drzwi="+(drzwi.Count>0?"tak ("+drzwi.Count+" węzłów)":"nie")+" kabina="+kab.kabina+" zapas="+w.zapas.ToString("F3")+" odchylenie="+w.odchylenie.ToString("F0")+"°"));
            }
        }
        finally
        {
            anim.GetClip("Idle").SampleAnimation(anim.gameObject,0);
            player.transform.SetPositionAndRotation(oldPos,oldRot);
        }
        // Nie wybieraj sportowego auta, ktorego kabina pasuje, ale ktore
        // fizycznie nie miesci sie w zadnej zatoce przy arenie.
        var ok=kandydaci.Where(k=>k.t!=null && k.miesci &&
            MiejscaParkingowe.Any(spot =>
                (new Vector2(spot.srodek.x-WejscieAreny.x,spot.srodek.z-WejscieAreny.z)).sqrMagnitude<=40f*40f &&
                k.t.dl+.2f<=spot.rozmiar.y && k.t.szer+.8f*K<=spot.rozmiar.x)).ToList();
        var wybor=ok.Where(k=>k.drzwi&&k.miesci).Select(k=>(k.t,"drzwi kierowcy wydzielone i Patryk się mieści")).FirstOrDefault();
        if(wybor.t==null)wybor=ok.Where(k=>k.miesci).Select(k=>(k.t,"mieści się, bez wydzielonych drzwi (drzwi bez animacji)")).FirstOrDefault();
        // Nie stawiaj auta wyłącznie dekoracyjnego: Patryk ma realnie mieścić
        // się w kabinie, inaczej wejście przez AKCJA będzie deformować głowę.
        if(wybor.t==null)
            throw new InvalidOperationException("BRAXHORN_AUTO_PATRYKA: żaden sportowy samochód nie mieści Patryka w kabinie. "+string.Join("; ",kandydaci.Select(k=>k.opis)));
        var template=wybor.t; var wybrany=kandydaci.First(k=>k.t==template);
        Debug.Log("BRAXHORN_AUTO_PATRYKA wybór="+template.id+" powód="+wybor.Item2+" kandydaci: "+string.Join("; ",kandydaci.Select(k=>k.opis)));
        if(!wybrany.miesci)Debug.LogError("BRAXHORN_AUTO_PATRYKA DO POPRAWKI: Patryk nie mieści się w "+template.id+" przy najlepszym odchyleniu "+wybrany.w.odchylenie.ToString("F0")+"° — brakuje "+(-wybrany.w.zapas*100f/K).ToString("F1")+" cm rzeczywistych");

        // Model ma już realną długość * K. Dodatkowe +25% robiło z 4,95 m
        // samochodu pojazd długości 6,19 m rzeczywistych i psuło dobór dróg.
        // Zostaw najlepszą zmierzoną pozycję siedzenia; jej brak miejsca trafia do diagnostyki.
        const float skalaAuta = 1f;
        Physics.SyncTransforms();
        foreach(var spot in MiejscaParkingowe.OrderBy(m=>(new Vector3(m.srodek.x,0,m.srodek.z)-WejscieAreny).sqrMagnitude))
        {
            if((new Vector3(spot.srodek.x,0,spot.srodek.z)-WejscieAreny).magnitude>40f)break;
            if(template.dl*skalaAuta+.2f>spot.rozmiar.y || template.szer*skalaAuta+.8f*K>spot.rozmiar.x)continue;
            var rotation=Quaternion.LookRotation(spot.przod);
            var half=new Vector3(template.szer*skalaAuta*.5f+.05f,Mathf.Max(.2f,(template.wys*skalaAuta-.3f)*.5f),template.dl*skalaAuta*.5f+.2f);
            var fotelSzablonu=template.go.GetComponentsInChildren<Transform>(true).First(t=>t.name==FotelKierowcy);
            float zDrzwi=template.go.transform.InverseTransformPoint(fotelSzablonu.position).z;
            float promien=player.body.radius, wysokosc=player.body.height;
            // Zatoka moze miec słupek lub krawężnik w jednym narożniku.
            // Sprawdz kilka położeń w jej granicach, nie stawiaj przez obiekty.
            Vector3 srodek=spot.srodek; bool wolne=false;
            foreach(float odsun in new[]{0f,-.25f,.25f,-.5f,.5f})
            {
                if(Mathf.Abs(odsun)+template.dl*.5f+.2f>spot.rozmiar.y*.5f)continue;
                var p=spot.srodek+rotation*Vector3.forward*odsun;
                if(Physics.CheckBox(p+Vector3.up*(.3f+half.y),half,rotation,~0,QueryTriggerInteraction.Ignore))continue;
                var wejscie=p+rotation*new Vector3(-(template.obrys.size.x*.5f+.65f)*skalaAuta,0f,zDrzwi*skalaAuta);
                if(Physics.CheckCapsule(wejscie+Vector3.up*(promien+.08f),wejscie+Vector3.up*(wysokosc-promien),
                    promien,~0,QueryTriggerInteraction.Ignore))continue;
                srodek=p;wolne=true;break;
            }
            if(!wolne)continue;
            var go=AutoInstancja(template,Group("Auto Patryka / ARENA BRAXHORN"),"Auto Patryka · tylko gracz",new System.Random(180),out var color);
            go.transform.SetPositionAndRotation(srodek,rotation);
            Debug.Log("BRAXHORN_AUTO_PATRYKA skala_dodatkowa=1.000 dl="+template.dl.ToString("F3")+" szer_z_lusterkami="+template.szer.ToString("F3")+" K="+K.ToString("F4"));
            var box=go.AddComponent<BoxCollider>();box.center=template.obrys.center;box.size=template.obrys.size;
            var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.Interpolate;
            var car=go.AddComponent<BraxAutoGracza>();car.obrys=box;
            car.fotel=go.GetComponentsInChildren<Transform>().First(t=>t.name==FotelKierowcy);
            car.przesuniecieGracza=go.transform.InverseTransformPoint(car.fotel.position + car.fotel.up*(.08f*K-wybrany.w.zaglebienie) - rotation*hipsOffset);
            car.rozstaw=template.rozstaw*skalaAuta;car.promienKola=template.promienKola*skalaAuta;
            car.kola=new[]{"LP","PP","LT","PT"}.Select(n=>go.transform.Find("koło "+n+"/obrót")).Where(t=>t).ToArray();
            car.skret=new[]{"LP","PP"}.Select(n=>go.transform.Find("koło "+n)).Where(t=>t).ToArray();
            // poza Patryka za kierownicą (jak ludzie w kabinach): odchylenie od kręgu Spine, głowa — LateUpdate auta
            car.odchylenie=wybrany.w.odchylenie; car.pochylenieGlowy=KabinaWzrok*wybrany.w.odchylenie+wybrany.w.pochylenie; car.kregoslupGracza=spine; car.glowaGracza=head;
            // E9: drzwi kierowcy na zawiasie
            string drzwiOpis="brak wydzielonych drzwi";
            var dz=DrzwiKierowcy(go,car.fotel,out var ob);
            if(dz.Count>0)
            {
                bool nozycowe=template.id=="car_rev";
                var nad=car.fotel.parent;
                var piv=new GameObject("drzwi kierowcy (zawias)").transform; piv.SetParent(nad,false);
                piv.position=go.transform.TransformPoint(new Vector3(ob.center.x,ob.min.y+(nozycowe?.3f:.5f)*ob.size.y,ob.max.z-.03f*ob.size.z));
                piv.rotation=go.transform.rotation;
                foreach(var d in dz)d.SetParent(piv,true);
                car.drzwi=piv; car.osDrzwi=nozycowe?Vector3.right:Vector3.up;
                car.katDrzwi=nozycowe?65f:(ob.center.x<0f?65f:-65f);
                drzwiOpis=dz.Count+" części, obrys "+ob.size.x.ToString("F2")+"×"+ob.size.y.ToString("F2")+"×"+ob.size.z.ToString("F2")+" m";
                Debug.Log("BRAXHORN_AUTO_DRZWI model="+template.id+" drzwi="+drzwiOpis+" zawias="+(nozycowe?"nożycowy (przednia krawędź, oś poprzeczna)":"przednia krawędź, oś pionowa")+" kąt="+car.katDrzwi.ToString("F0")+"° punkt="+piv.localPosition.ToString("F2"));
            }
            else Debug.Log("BRAXHORN_AUTO_DRZWI model="+template.id+" drzwi=nie zawias=— (model bez wydzielonych drzwi — wsiadanie bez animacji drzwi)");
            float odl=(new Vector3(srodek.x,0,srodek.z)-WejscieAreny).magnitude;
            Debug.Log("BRAXHORN_AUTO_PATRYKA miejsce="+srodek.ToString("F2")+" (przy arenie, "+odl.ToString("F1")+" m od wejścia ARENA BRAXHORN) przód="+spot.przod.ToString("F2")+" kabina zapas="+wybrany.w.zapas.ToString("F3")+" odchylenie="+wybrany.w.odchylenie.ToString("F0")+"°");
            rep.notes.Add("Auto Patryka przy ARENA BRAXHORN: "+template.id+", "+color+", "+srodek+" ("+odl.ToString("F1")+" m od wejścia); wsiadanie AKCJA (drzwi: "+drzwiOpis+"), gaz/ster joystick, wyjście po zatrzymaniu. Bez dostępu NPC.");
            Physics.SyncTransforms();return;
        }
        throw new InvalidOperationException("BRAXHORN_AUTO_PATRYKA: brak wolnej zatoki i dojścia do drzwi przy ARENA BRAXHORN (40 m od wejścia)");
    }
}
