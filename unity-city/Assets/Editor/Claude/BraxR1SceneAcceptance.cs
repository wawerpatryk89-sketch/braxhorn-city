using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// MIASTO R1: rzeczywista kontrola po wygenerowaniu sceny UNITY 6.
// Nie wymusza APK, nie włącza sztucznie kolizji ani nie fałszuje PASS.
// Raport zbiorczy zawiera nazwę każdej przeszkody i pierwszy błąd.
public static class BraxR1SceneAcceptance
{
    const string Folder = "Assets/PlayableGenerated/R1SceneAcceptance";

    [Serializable]
    sealed class Receipt
    {
        public string unity, sourceScene, status, notes;
        public string[] warnings;
        public int checkedVehicles, scheduledBuses, overlappingBusCars;
        public int tramUnits, tramDoorLeaves, platforms;
        public int northParkingSpots, oldNorthRoundabouts;
        public int supermarkets, supermarketDoors, entranceSamples, entranceBlockers;
    }

    static bool IsNear(Bounds bounds, Vector3 p, float maxRadius)
    {
        var d = bounds.center - p; d.y = 0f;
        return d.sqrMagnitude < maxRadius * maxRadius;
    }

    public static void Run(Scene scene)
    {
        var bad = new List<string>();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("BRAXHORN_R1_SCENA NIEGOTOWE: brak aktywnej sceny Unity");
            return;
        }
        var roots = scene.GetRootGameObjects();
        var cars = roots.SelectMany(g=>g.GetComponentsInChildren<BraxAuto>(true))
            .Where(x=>x).ToArray();
        var controllers = roots.SelectMany(g=>g.GetComponentsInChildren<BraxRuchAut>(true))
            .Where(x=>x).ToArray();
        var buses = cars.Where(x=>x.name.Contains("Autobus KURSOWY")).ToArray();
        var tram = roots.SelectMany(g=>g.GetComponentsInChildren<BraxTramT1>(true))
            .Where(x=>x).ToArray();
        var parking = BraxRegionMiastoB.MiejscaParkingu.Count(p =>
            Mathf.Abs(p.c.x-BraxRegionMiastoB.AX) < BraxRegionMiastoB.PetlaPlac &&
            Mathf.Abs(p.c.z-BraxRegionMiastoB.PetlaZ) < BraxRegionMiastoB.PetlaPlac);
        int phantomCircle = 0;
        foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
            if(r && r.enabled && r.name.Contains("Pętla: trawnik wyspy") &&
               IsNear(r.bounds, new Vector3(BraxRegionMiastoB.AX,0f,BraxRegionMiastoB.PetlaZ),22f))
                phantomCircle++;

        int overlaps=0, doorPanels=0, stops=0;
        if(buses.Length!=1)bad.Add("AUTOBUS: oczekiwany jeden kursowy, jest "+buses.Length);
        foreach(var bus in buses)
        {
            var traffic=controllers.FirstOrDefault(c=>c && c.auta!=null && c.auta.Contains(bus));
            if(!traffic || traffic.autobusIndex<0 ||
               traffic.autobusIndex>=traffic.auta.Length ||
               traffic.auta[traffic.autobusIndex]!=bus ||
               traffic.autobusStopS<0f)
                bad.Add("AUTOBUS: brak połączenia z jedną symulacją aut i postojem");
            if(!bus.GetComponent<Rigidbody>() || !bus.GetComponent<BoxCollider>())
                bad.Add("AUTOBUS: brak fizycznego poruszającego się kadłuba");
            foreach(var a in cars)
            {
                if(a==bus || !a || !a.gameObject.activeInHierarchy)continue;
                bool collision=BraxRuchAutSym.Prost(bus.transform.position,bus.transform.forward,
                    bus.szer*.5f,bus.dl*.5f,a.transform.position,a.transform.forward,
                    a.szer*.5f,a.dl*.5f);
                if(!collision)continue;
                overlaps++;
                bad.Add("AUTOBUS: nachodzi na auto "+a.name+" @"+a.transform.position.ToString("F1"));
            }
        }
        if(tram.Length!=1)bad.Add("TRAMWAJ: oczekiwany jeden T1, jest "+tram.Length);
        foreach(var t in tram)
        {
            if(!t.GetComponent<Rigidbody>() || !t.GetComponent<BoxCollider>())
                bad.Add("TRAMWAJ: brak ruchomego kadłuba/kolidującej bryły");
            if(t.wschodX-t.zachodX<50f)
                bad.Add("TRAMWAJ: za krótki odcinek ruchu dla CAF");
            if(Mathf.Abs(t.transform.position.z-t.torZ)>.15f)
                bad.Add("TRAMWAJ: oś wagonu nie na torze "+
                    t.transform.position.ToString("F1"));
            try
            {
                int n=t.AuditDoorNodes();
                doorPanels+=n;
                if(n<2)bad.Add("TRAMWAJ: BRAK ODDZIELNYCH DRZWI CAF — niepotwierdzone otwieranie na peronie");
            }
            catch(Exception e){bad.Add("TRAMWAJ: audyt siatek drzwi "+e.Message);}
            stops++;
        }

        if(parking!=18)bad.Add("PARKING: stanowisk przy SUPERMARKET BRAXHORN "+parking+"/18");
        if(phantomCircle!=0)bad.Add("PARKING: stara wyspa północnego ronda nadal ma "+
                                    phantomCircle+" rendererów");
        var stores=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .Where(t=>t && (t.name=="SUPERMARKET BRAXHORN 1" ||
                           t.name=="SUPERMARKET BRAXHORN 2")).ToArray();
        if(stores.Length!=2)bad.Add("SKLEPY: znaleziono "+stores.Length+"/2");
        var doors=roots.SelectMany(g=>g.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true))
            .Where(d=>d && d.etykieta!=null && d.etykieta.StartsWith("SUPERMARKET BRAXHORN")).ToArray();
        if(doors.Length!=2)bad.Add("SKLEPY: drzwi przesuwne "+doors.Length+"/2");
        int samples=0, obstacles=0;
        foreach(var d in doors)
        {
            if(d.skrzydla==null || d.skrzydla.Length!=2 ||
               d.blokady==null || d.blokady.Length!=2)
                bad.Add("SKLEP: nierówna liczba skrzydeł i blokad "+d.etykieta);
            float radius=.33f, height=2.23f;
            for(int k=0;k<=16;k++)
            {
                // Oś: z parkingu przez drzwi automatyczne, 3 m w głąb sklepu.
                var at=d.transform.position+d.transform.forward*Mathf.Lerp(-3f,3f,k/16f);
                var foot=new Vector3(at.x,d.transform.position.y,at.z);
                samples++;
                foreach(var hit in Physics.OverlapCapsule(foot+Vector3.up*(radius+.07f),
                    foot+Vector3.up*(height-radius),radius,
                    ~((1<<8)|(1<<29)),QueryTriggerInteraction.Ignore))
                {
                    if(!hit||!hit.enabled||hit.isTrigger ||
                       hit.GetComponentInParent<BraxDrzwiPrzesuwne>() ||
                       hit.GetComponentInParent<BraxGymPlayer>() ||
                       hit.bounds.max.y<=foot.y+.19f)continue;
                    obstacles++;
                    if(bad.Count<70)bad.Add("SKLEP "+d.etykieta+": kolizja "+hit.name+
                        " próbka="+k+" @"+foot.ToString("F1"));
                }
            }
        }
        var receipt=new Receipt {
            unity=Application.unityVersion,sourceScene=scene.name,
            checkedVehicles=cars.Length,scheduledBuses=buses.Length,
            overlappingBusCars=overlaps,tramUnits=tram.Length,tramDoorLeaves=doorPanels,
            platforms=stops,northParkingSpots=parking,oldNorthRoundabouts=phantomCircle,
            supermarkets=stores.Length,supermarketDoors=doors.Length,
            entranceSamples=samples,entranceBlockers=obstacles,
            status=bad.Count==0?"SCENA_BEZ_WIDOCZNYCH_BLEDOW_W_PROBKACH (PlayMode/telefon niepotwierdzony)":"NIEGOTOWE",
            notes="Audyty Editor (kolizje, plan i oryginalne modele). Nie jest to przejazd gracza na telefonie.",
            warnings=bad.ToArray()
        };
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder+"/r1-transport-sklep.json",JsonUtility.ToJson(receipt,true));
        string summary="BRAXHORN R1 | Unity "+receipt.unity+" | "+receipt.status+"\n"+
            "Auta: "+receipt.checkedVehicles+"; autobus: "+receipt.scheduledBuses+
            "; nachodzenia: "+receipt.overlappingBusCars+"\n"+
            "Tramwaje: "+receipt.tramUnits+"; drzwi CAF: "+receipt.tramDoorLeaves+"\n"+
            "Parking: "+receipt.northParkingSpots+"/18; stare rondo: "+receipt.oldNorthRoundabouts+"\n"+
            "Sklepy: "+receipt.supermarkets+"/2; drzwi: "+receipt.supermarketDoors+
            "/2; próbki: "+receipt.entranceSamples+"; przeszkody: "+receipt.entranceBlockers+"\n"+
            "Ostrzeżenia: "+bad.Count+"\n"+string.Join("\n",bad.Take(100));
        File.WriteAllText(Folder+"/r1-transport-sklep.txt",summary);
        Debug.Log("BRAXHORN_R1_SCENA_TEST "+summary.Replace("\n"," | "));
        if(bad.Count>0)
            Debug.LogWarning("BRAXHORN_R1_SCENA_NIEGOTOWE pierwszy_bląd="+bad[0]+
                " pozostałe="+(bad.Count-1)+" (tylko raport, bez anulowania builda)");
    }
}
