using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// MIASTO R1 — płot przed pierwszym rondem, wszystkie latarnie i słupy
// w obrębie miasta. Audyt/naprawa sceny tylko w edytorze Unity 6.
// Nie dotyka stadionu, wyspy, osiedla, funkcjonalnych sygnalizatorów,
// źródłowych budynków ani nawierzchni. Nie blokuje sceny przy ostrzeżeniu.
public static class BraxR1StreetLightingAudit
{
    const string Output = "Assets/PlayableGenerated/R1SceneAcceptance";

    [Serializable]
    sealed class Receipt
    {
        public string unityVersion, verdict;
        public int lampsFound, streetLamps, parkLamps, legacyLamps;
        public int lampsRemoved, doublePolesRemoved, badDirections, unresolvedLamps;
        public int trafficLights, duplicateTrafficLights, blockersAcrossMainRoad;
        public string[] removedNames, warnings;
    }

    sealed class Lamp
    {
        public Transform t;
        public Vector3 foot;
        public bool street, park, legacy;
        public int priority;
    }

    static bool Near(Vector3 a, Vector3 b, float distance)
    {
        var dx=a.x-b.x;var dz=a.z-b.z;
        return dx*dx+dz*dz<=distance*distance;
    }

    static bool OutsideCity(Transform t)
    {
        for(var p=t;p;p=p.parent)
        {
            var n=p.name.ToLowerInvariant();
            if(n.StartsWith("claude wyspa") || n.Contains("osiedle") ||
               n.StartsWith("braxhorn port") || n.StartsWith("claude plaża") ||
               n.Contains("miasto mistrzów")) return true;
        }
        return false;
    }

    static bool HasWord(string text,params string[] words)
    {
        return words.Any(x=>text.IndexOf(x,StringComparison.OrdinalIgnoreCase)>=0);
    }

    static bool IsCandidate(Transform t,out bool street,out bool park,out bool legacy)
    {
        string n=t.name;
        street=n=="Latarnia uliczna";
        park=n=="Latarnia parkowa";
        legacy=n.IndexOf("street lamp",StringComparison.OrdinalIgnoreCase)>=0 ||
               n.IndexOf("streetlight",StringComparison.OrdinalIgnoreCase)>=0 ||
               n.IndexOf("lamp post",StringComparison.OrdinalIgnoreCase)>=0 ||
               n.IndexOf("lamppost",StringComparison.OrdinalIgnoreCase)>=0;
        // NIGDY nie rozpoznawaj po samym "lamp" / "light" — sygnalizator
        // i oprawa budynku to inny obiekt, nie wolno ich kasować.
        if(HasWord(n,"traffic light","traffic signal","sygnaliz","road sign"))
            return false;
        return street||park||legacy;
    }

    static bool TryLamp(Transform t,bool street,bool park,bool legacy,
                        out Lamp record)
    {
        record=null;
        if(!t || !t.gameObject.activeInHierarchy || OutsideCity(t))return false;
        if(t.GetComponentInChildren<BraxSwiatloDrogowe>(true))return false;
        var renders=t.GetComponentsInChildren<Renderer>(true)
            .Where(x=>x && x.enabled).ToArray();
        var colliders=t.GetComponentsInChildren<Collider>(true)
            .Where(x=>x && x.enabled && !x.isTrigger).ToArray();
        if(renders.Length==0 || renders.Length>12 || colliders.Length>12)
            return false;
        Bounds union=renders[0].bounds;
        foreach(var r in renders.Skip(1))union.Encapsulate(r.bounds);
        if(union.size.y<2.5f || union.size.y>22f ||
           union.size.x>10f || union.size.z>10f)
            return false; // importowany zestaw wielu lamp = raport, nie usuwać hurtem
        var pole=colliders.FirstOrDefault(x=>HasWord(x.name,"latarnia kolizja","latarnia: kolizja",
            "streetlight","street lamp","lamp post"));
        Vector3 foot;
        if(pole)
            foot=new Vector3(pole.bounds.center.x,pole.bounds.min.y,pole.bounds.center.z);
        else
            foot=new Vector3(union.center.x,union.min.y,union.center.z);
        record=new Lamp { t=t, foot=foot,street=street,park=park,legacy=legacy,
            priority=street?30:park?20:10 };
        return true;
    }

    static int FenceBlockers(Scene scene,List<string> problems)
    {
        // x≈−104.5 przed R1 rondem (x−126), główna z=28.5.
        // Pomiary poprzeczne co 0.6m przez 9m jezdni; obiekty dynamiczne
        // i posadzki wyłączone z wyniku, żeby nie mylić auta z płotem.
        int blockers=0;
        for(float z=24.3f;z<=32.7f;z+=.6f)
        {
            Vector3 p=new Vector3(-104.5f,.15f,z);
            var hits=Physics.OverlapCapsule(p+Vector3.up*.44f,
                p+Vector3.up*2.20f,.33f,
                ~((1<<8)|(1<<29)),QueryTriggerInteraction.Ignore);
            foreach(var c in hits)
            {
                if(!c || !c.enabled || c.isTrigger ||
                   c.GetComponentInParent<BraxAuto>() ||
                   c.GetComponentInParent<BraxGymPlayer>() ||
                   c.bounds.max.y<p.y+.37f)continue;
                // Nazwy i pozycje każdego utrudnienia do raportu,
                // nawet gdy wcześniej był nienazwany "SourceCollider".
                blockers++;
                if(problems.Count<70)
                    problems.Add("GŁÓWNA PRZED RONDEM: "+c.name+
                        " x="+p.x.ToString("F1")+" z="+z.ToString("F1"));
            }
        }
        return blockers;
    }

    public static void Run(Scene scene)
    {
        if(!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("BRAXHORN_R1_LATARNIE DO POPRAWKI: brak sceny Unity");
            return;
        }
        Physics.SyncTransforms();
        var notes=new List<string>();
        var removed=new List<string>();
        var roots=scene.GetRootGameObjects();
        var lamps=new List<Lamp>();
        int unresolved=0, streetCount=0,parkCount=0,legacyCount=0;
        foreach(var root in roots)
        foreach(var tr in root.GetComponentsInChildren<Transform>(true))
        {
            if(!tr || !IsCandidate(tr,out bool street,out bool park,out bool legacy))continue;
            if(OutsideCity(tr))continue;
            if(!TryLamp(tr,street,park,legacy,out var lamp))
            {
                unresolved++;
                if(notes.Count<70)notes.Add("LATARNIA NIEROZPOZNANA: "+tr.name);
                continue;
            }
            lamps.Add(lamp);
            if(street)streetCount++;if(park)parkCount++;if(legacy)legacyCount++;
        }

        // Sygnalizacja jest ważniejsza niż dekoracyjna latarnia. Lampy
        // nie mogą stać razem ze słupem sygnalizatora lub przejścia.
        var signals=roots.SelectMany(g=>g.GetComponentsInChildren<BraxSwiatloDrogowe>(true))
            .Where(x=>x && x.gameObject.activeInHierarchy).ToArray();
        var trafficPoles=roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider>(true))
            .Where(c=>c && c.enabled && c.gameObject.activeInHierarchy &&
                HasWord(c.name,"słup sygnalizacji kolizja","słupek pieszych kolizja")).ToArray();

        var kept=new List<Lamp>();
        int removedCount=0, closeToSignal=0, incorrect=0;
        foreach(var lamp in lamps.OrderByDescending(x=>x.priority)
                                 .ThenBy(x=>x.foot.x).ThenBy(x=>x.foot.z))
        {
            if(!lamp.t)continue;
            bool inRoad=BraxRegionMiastoB.NaJezdni(lamp.foot);
            var another=kept.FirstOrDefault(q=>Near(q.foot,lamp.foot,3.6f));
            bool nearSignal=trafficPoles.Any(x=>
                Near(x.bounds.center,lamp.foot,1.75f));
            // Wiele lamp z tej samej paczki, stojących 3m od siebie,
            // powodowało zdjęcia z "dwoma słupami koło siebie".
            if(inRoad || another!=null || nearSignal)
            {
                string reason=inRoad?"latarnia na asfalcie":
                    another!=null?"duplikat obok "+another.t.name:
                    "latarnia obok sygnalizatora";
                if(lamp.t.GetComponentsInChildren<BraxSwiatloDrogowe>(true).Length>0)
                {unresolved++;continue;}
                var name=lamp.t.name+" @"+lamp.foot.ToString("F1")+" "+reason;
                if(removed.Count<70)removed.Add(name);
                if(nearSignal)closeToSignal++;
                UnityEngine.Object.DestroyImmediate(lamp.t.gameObject);
                removedCount++;
                continue;
            }
            kept.Add(lamp);
            if(lamp.street)
            {
                var fixture=lamp.t.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(r=>r && r.name.Contains("Latarnia: oprawa"));
                if(fixture)
                {
                    Vector3 facing=fixture.bounds.center-lamp.foot;facing.y=0f;
                    if(facing.sqrMagnitude>.5f)
                    {
                        facing.Normalize();
                        float reach=Mathf.Min(3f,Mathf.Sqrt(fixture.bounds.size.x*
                            fixture.bounds.size.x+fixture.bounds.size.z*fixture.bounds.size.z)+2f);
                        var toward=BraxRegionMiastoB.NaJezdni(lamp.foot+facing*reach);
                        var away=BraxRegionMiastoB.NaJezdni(lamp.foot-facing*reach);
                        if(!toward && away)
                        {
                            incorrect++;
                            if(notes.Count<70)notes.Add("KIERUNEK LATARNI NIEPRAWIDŁOWY: "+
                                lamp.t.name+" @"+lamp.foot.ToString("F1"));
                        }
                    }
                }
            }
        }

        // Funkcjonalne zestawy świateł mają osobne kontrolery. Dokładnie
        // ten sam węzeł powielony przez generator kasujemy tylko wtedy,
        // gdy nazwa skrzyżowania i punkty zatrzymania się pokrywają.
        int signalsRemoved=0;
        var names=new Dictionary<string,BraxSwiatloDrogowe>();
        foreach(var light in signals)
        {
            if(!light || !light.name.StartsWith("Sygnalizacja świetlna (",
                                              StringComparison.Ordinal))continue;
            if(!names.TryGetValue(light.name,out var old))
            {
                names.Add(light.name,light);
                continue;
            }
            if(old && old.stopSrodek!=null && light.stopSrodek!=null &&
               old.stopSrodek.Length==light.stopSrodek.Length &&
               old.stopSrodek.Zip(light.stopSrodek,
                 (a,b)=>Vector3.Distance(a,b)<.1f).All(x=>x))
            {
                if(removed.Count<70)removed.Add("PODWÓJNY SYGNALIZATOR: "+light.name);
                UnityEngine.Object.DestroyImmediate(light.gameObject);
                signalsRemoved++;
            }
            else if(notes.Count<70)notes.Add("PODEJRZANA PODWÓJNA SYGNALIZACJA: "+light.name);
        }

        Physics.SyncTransforms();
        int barriers=FenceBlockers(scene,notes);
        int duplicatePolePairs=0;
        var poles=trafficPoles.Where(x=>x && x.enabled).ToArray();
        for(int i=0;i<poles.Length;i++)
        for(int j=i+1;j<poles.Length;j++)
        {
            if(poles[i].name!=poles[j].name ||
                !Near(poles[i].bounds.center,poles[j].bounds.center,.65f))continue;
            duplicatePolePairs++;
            if(notes.Count<70)notes.Add("PODWÓJNY SŁUP SYGNALIZACJI: "+
                poles[i].name+" "+poles[i].bounds.center.ToString("F1"));
        }

        var receipt=new Receipt {
            unityVersion=Application.unityVersion,
            lampsFound=lamps.Count,streetLamps=streetCount,
            parkLamps=parkCount,legacyLamps=legacyCount,
            lampsRemoved=removedCount,doublePolesRemoved=signalsRemoved,
            badDirections=incorrect,unresolvedLamps=unresolved,
            trafficLights=signals.Length,duplicateTrafficLights=duplicatePolePairs,
            blockersAcrossMainRoad=barriers,
            verdict=barriers==0 && incorrect==0 && duplicatePolePairs==0 &&
                unresolved==0?"SCENA: BRAK PRZESZKÓD W PRÓBKACH (telefon niepotwierdzony)":"NIEGOTOWE",
            removedNames=removed.ToArray(),warnings=notes.ToArray()
        };
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output+"/r1-plot-latarnie.json",JsonUtility.ToJson(receipt,true));
        string summary="BRAXHORN_R1_PLOT_LATARNIE "+receipt.verdict+
            " latarnie="+lamps.Count+" (uliczne="+streetCount+", parkowe="+parkCount+
            ", źródłowe="+legacyCount+") usunięto="+removedCount+
            " sygnalizatory_duplikaty="+signalsRemoved+
            " kolizje_główna="+barriers+
            " błędne_kierunki="+incorrect+" podwójne_słupy="+duplicatePolePairs+
            " nierozpoznane="+unresolved+
            " uwagi="+notes.Count+" [ "+string.Join("; ",notes.Take(18))+" ]";
        File.WriteAllText(Output+"/r1-plot-latarnie.txt",summary+"\n"+
            string.Join("\n",removed.Take(80))+"\n"+string.Join("\n",notes.Take(80)));
        Debug.Log(summary);
        if(barriers>0 || incorrect>0 || unresolved>0 || duplicatePolePairs>0)
            Debug.LogWarning("DO POPRAWKI: BRAXHORN_R1_PLOT_LATARNIE — "+
                             "pełne nazwy i próbki w r1-plot-latarnie.json");
    }
}
