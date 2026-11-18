using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    const string ModeleFloty = "Assets/Source/Prywatne/FlotaOsiedla";
    static BraxAutoGracza patrolOsiedla;
    static Vector3 biodraKierowcy, korektaWejsciaFloty;
    static float obrotWejsciaFloty;

    static Bounds ObrysFloty(Transform t)
    {
        var meshes = t.GetComponentsInChildren<MeshRenderer>(true);
        if (meshes.Length == 0) throw new InvalidDataException("Brak siatki " + t.name);
        Bounds result = meshes[0].bounds;
        foreach (var r in meshes.Skip(1)) result.Encapsulate(r.bounds);
        return result;
    }

    static Transform OsFloty(string name, Transform parent, Vector3 position)
    {
        var axis = new GameObject(name).transform;
        axis.SetParent(parent, false);
        axis.position = position;
        return axis;
    }

    static BraxAutoGracza PrzygotujPojazdFloty(string path, int nr)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!src) throw new InvalidDataException(path);
        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
        go.transform.SetParent(Group("Flota Osiedla"), false);
        var model = UnityEngine.Object.Instantiate(src, go.transform, false).transform;
        model.localScale *= K * (nr == 10 ? 4.75f : 1f);
        var nodes = model.GetComponentsInChildren<Transform>(true);
        Transform Node(string key) => nodes.Single(t => t.name == key);
        var wheels = new[] {"FL","FR","RL","RR"}.Select(s => Node("BX_Tire_" + s)).ToArray();
        Vector3 forward = (wheels[0].position + wheels[1].position - wheels[2].position - wheels[3].position);
        forward.y = 0f;
        model.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(forward, Vector3.forward, Vector3.up), Vector3.up) * model.rotation;
        var bounds = ObrysFloty(model);
        // SPÓD POJAZDU, NIE SAME KOŁA (właściciel 18.11, zdjęcie: „samochód się zatapia, opona”).
        // Zmierzone wprost z modeli floty (min Y względem punktu zerowego modelu):
        //   Ambulans BX10004 — najniższy obiekt to „Tyre sidewall” (−0,087), a koła Wheel_FL/FR/RL (−0,0004).
        //   „Tyre sidewall” nie jest żadnym z czterech BX_Tire_*, więc wyrównanie po samych kołach
        //   zostawiało gumę 0,087 × K ≈ 11 cm POD asfaltem. Rim stał na drodze, opona w niej tonęła.
        //   Roadster Metalik BX10010 — cały model ma 0,295 jedn. wysokości i jest skalowany ×4,75,
        //   więc każdy błąd wyrównania mnoży się przez ten sam współczynnik.
        // Bierzemy niższy z dwóch: spód kół i spód CAŁEGO pojazdu. Nic nie ma prawa być pod jezdnią.
        // Dla modeli wyrównanych poprawnie (12 z 14 ma spód dokładnie 0,000) nie zmienia to niczego.
        float dolKol = wheels.Min(w => ObrysFloty(w).min.y);
        float dolPojazdu = bounds.min.y;
        model.position -= new Vector3(bounds.center.x, Mathf.Min(dolKol, dolPojazdu), bounds.center.z);
        bounds = ObrysFloty(model);
        var car = go.AddComponent<BraxAutoGracza>();
        car.rejestracja = "BX " + (10000+nr);
        car.obrys = go.AddComponent<BoxCollider>();
        car.obrys.center = go.transform.InverseTransformPoint(bounds.center);
        car.obrys.size = bounds.size;
        car.obrys.enabled = false;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        car.rozstaw = Mathf.Abs(wheels[0].position.z-wheels[2].position.z);
        car.promienKola = wheels.Average(w => ObrysFloty(w).extents.y);
        car.kola = new Transform[4];
        car.skret = new Transform[2];
        for (int i=0; i<4; i++)
        {
            var pivot = OsFloty("Zawrotnica " + i,go.transform,wheels[i].position);
            var roll = OsFloty("Toczenie " + i,pivot,wheels[i].position);
            wheels[i].SetParent(roll,true);
            car.kola[i] = roll;
            if(i<2) car.skret[i] = pivot;
        }
        car.wieniec = Node("BX_Steering");
        car.osKolumny = car.wieniec.InverseTransformDirection((Node("BX_ColumnAxis").position-car.wieniec.position).normalized);
        car.spoczynkowaKierownicy = car.wieniec.localRotation;
        car.fotel = OsFloty("Biodra kierowcy",go.transform,car.wieniec.position + K *
            (Vector3.back*(nr==6?.55f:.43f) + Vector3.down*(nr==6?.30f:.31f)));
        car.przesuniecieGracza = car.fotel.localPosition-biodraKierowcy;
        car.korektaWsiadania = korektaWejsciaFloty;
        car.obrotWsiadania = obrotWejsciaFloty;
        DrzwiFloty(car, go);
        return car;
    }

    // DRZWI W AUTACH FLOTY (właściciel 18.11: „drzwi od samochodu się nie otwierają tak samo”).
    // Dotąd zawias dostawało TYLKO auto Patryka (BraxClaudeAutoPatryka) — czternaście aut floty nie
    // miało ustawionego pola `drzwi` w ogóle, więc nie było czego obracać. Używamy tego samego
    // szukacza co tam, z jego filtrami: część musi mieć w nazwie „door” i leżeć po stronie fotela
    // (|x| > 0,25 m od osi auta). Dzięki temu modele, w których drzwi są ZROŚNIĘTE z nadwoziem w jeden
    // obiekt („04 Coachwork | doors and clamshell bonnet”, „Bonnet, doors and flush hardware”), odpadają
    // same — ich środek leży na osi auta. Lepiej, żeby drzwi zostały zamknięte, niż żeby przy wsiadaniu
    // odjechała połowa karoserii z maską.
    // Dodatkowy bezpiecznik rozmiaru: drzwi nie mogą być dłuższe niż 60 % auta ani wyższe niż 75 %.
    static void DrzwiFloty(BraxAutoGracza car, GameObject go)
    {
        var czesci = BraxClaudeAutoPatryka.DrzwiKierowcy(go, car.fotel, out var ob);
        var auto = car.obrys.size;
        if (czesci.Count == 0 || ob.size.z > auto.z*.6f || ob.size.y > auto.y*.75f || ob.size.x > auto.x*.55f)
        {
            Debug.Log("BRAX_FLOTA_DRZWI "+car.rejestracja+" drzwi=nie ("+
                      (czesci.Count==0 ? "model bez wydzielonych drzwi kierowcy" :
                       "znalezione części za duże jak na drzwi: "+ob.size.ToString("F2")+" przy aucie "+auto.ToString("F2"))+
                      ") — wsiadanie bez animacji drzwi");
            return;
        }
        var piv = new GameObject("drzwi kierowcy (zawias)").transform;
        piv.SetParent(go.transform, false);
        piv.position = go.transform.TransformPoint(new Vector3(ob.center.x, ob.min.y+.5f*ob.size.y, ob.max.z-.03f*ob.size.z));
        piv.rotation = go.transform.rotation;
        foreach (var d in czesci) d.SetParent(piv, true);
        car.drzwi = piv; car.osDrzwi = Vector3.up;
        car.katDrzwi = ob.center.x < 0f ? 65f : -65f;
        Debug.Log("BRAX_FLOTA_DRZWI "+car.rejestracja+" drzwi="+czesci.Count+" części, obrys "+ob.size.ToString("F2")+
                  ", zawias na przedniej krawędzi, kąt "+car.katDrzwi.ToString("F0")+"°");
    }

    static bool ParkingPrzedBudynkiem(Vector3 miejsce, int lot)
    {
        var (z0, z1) = LotyBulwarow[lot];
        return miejsce.x >= -107.5f && miejsce.x <= -64f && miejsce.z >= z0 && miejsce.z <= z1;
    }

    static bool ParkingFloty(BraxAutoGracza car,int nr)
    {
        var size = car.obrys.size;
        int lot = nr == 4 ? 1 : (nr == 1 || nr == 3) ? 0 : -1;
        var dest = new Vector3(0, 0, 34);
        if (lot >= 0)
        {
            var (z0, z1) = LotyBulwarow[lot];
            dest = new Vector3(-86f, 0f, (z0 + z1) * .5f);
        }
        Physics.SyncTransforms();
        foreach (var m in MiejscaParkingowe.OrderBy(m => (m.srodek-dest).sqrMagnitude))
        {
            if (lot >= 0 && !ParkingPrzedBudynkiem(m.srodek, lot)) continue;
            foreach(bool parallel in new[]{false,true})
            {
                var f = parallel ? Vector3.Cross(Vector3.up,m.przod).normalized : m.przod.normalized;
                if (!parallel && (size.z>m.rozmiar.y || size.x>m.rozmiar.x)) continue;
                if (parallel && (size.x+.4f>m.rozmiar.y || size.z<m.rozmiar.y)) continue;
                // Longer vehicles occupy contiguous marked bays parallel to the row.
                if (parallel && new[]{-1,1}.Any(side => !MiejscaParkingowe.Any(other =>
                    Vector3.Dot(m.przod,other.przod)>.99f &&
                    Vector3.Distance(other.srodek,m.srodek+side*f*(size.z*.5f-m.rozmiar.x*.35f))<=m.rozmiar.x*.54f))) continue;
                var rotation = Quaternion.LookRotation(f);
                var half = new Vector3(size.x*.5f+.08f,Mathf.Max(.1f,(size.y-.32f)*.5f),size.z*.5f+.14f);
                var p = m.srodek;
                if (Physics.CheckBox(p+Vector3.up*(half.y+.32f),half,rotation,~0,QueryTriggerInteraction.Ignore)) continue;
                var door=p+rotation*new Vector3(-(size.x*.5f+.65f),0,car.fotel.localPosition.z);
                if(Physics.CheckCapsule(door+Vector3.up*.43f,door+Vector3.up*1.7f,.34f,~0,QueryTriggerInteraction.Ignore))continue;
                car.transform.SetPositionAndRotation(p,rotation);
                car.obrys.enabled=true;
                Physics.SyncTransforms();
                return true;
            }
        }
        return false;
    }

    static void FlotaOsiedla()
    {
        var paths = Directory.GetFiles(ModeleFloty,"*.glb");
        if (paths.Length!=14) throw new InvalidDataException("Flota: oczekiwano 14 pojazdów");
        patrolOsiedla=null;
        foreach (int nr in new[]{1,4,3,6,2,5,7,8,9,10,11,12,13,14})
        {
            string path=paths.Single(p=>p.EndsWith("_BX"+(10000+nr)+".glb",StringComparison.Ordinal));
            var car=PrzygotujPojazdFloty(path.Replace('\\','/'),nr);
            if(nr==2) patrolOsiedla=car;
            else if(!ParkingFloty(car,nr)) throw new InvalidDataException("Brak wolnego miejsca dla "+car.rejestracja);
            Debug.Log("BRAX_FLOTA "+car.rejestracja+" "+car.transform.position.ToString("F2"));
        }
    }

    static BraxAuto WstawPatrol(BraxAuto old)
    {
        if(!patrolOsiedla)throw new InvalidDataException("Brak radiowozu patrolowego BX 10002");
        var car=patrolOsiedla;
        var ai=car.gameObject.AddComponent<BraxAuto>();
        ai.trasaNr=old.trasaNr; ai.sStart=old.sStart; ai.trasa=old.trasa;
        ai.vmax=old.vmax; ai.klakson=old.klakson;
        ai.dl=car.obrys.size.z; ai.szer=car.obrys.size.x; ai.rozstaw=car.rozstaw;
        ai.osTyl=car.kola[2].parent.localPosition.z; ai.promienKola=car.promienKola;
        ai.kola=car.kola; ai.skret=car.skret; ai.autoTestowe=car; car.patrol=ai;
        car.obrys.enabled=true;
        UnityEngine.Object.DestroyImmediate(old.gameObject);
        return ai;
    }

    public static void SprawdzFlote(UnityEngine.SceneManagement.Scene scena)
    {
        var fleet=scena.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BraxAutoGracza>(true))
            .Where(c=>!string.IsNullOrEmpty(c.rejestracja)).ToArray();
        if(fleet.Select(c=>c.rejestracja).Distinct().Count()!=14 || fleet.Length!=14)
            throw new InvalidDataException("Scena Osiedla: niekompletna flota "+fleet.Length+"/14");
        foreach(var c in fleet)
        {
            if(!c.obrys || !c.obrys.enabled || !c.fotel || !c.wieniec || c.kola.Length!=4 || c.kola.Any(k=>!k) ||
                c.skret.Length!=2 || c.skret.Any(k=>!k) || c.rozstaw<1f || c.promienKola<.12f ||
                c.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith(c.rejestracja+" "))!=2)
                throw new InvalidDataException("Niekompletne mocowania "+c.rejestracja);
        }
        foreach (var c in fleet)
        {
            bool komisariat = c.rejestracja == "BX 10001" || c.rejestracja == "BX 10003";
            bool szpital = c.rejestracja == "BX 10004";
            if ((komisariat || szpital) && !ParkingPrzedBudynkiem(c.transform.position, szpital ? 1 : 0))
                throw new InvalidDataException("Pojazd poza parkingiem służb: " + c.rejestracja);
        }
        var traffic=scena.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BraxRuchAut>(true));
        if(traffic.SelectMany(r=>r.auta).Count(a=>a && a.autoTestowe && a.autoTestowe.rejestracja=="BX 10002")!=1)
            throw new InvalidDataException("Scena Osiedla: brak patrolu na trasie");
        Debug.Log("BRAX_FLOTA_SCENA 14 aut, 56 kół, 14 kierownic, 28 tablic, 1 patrol");
    }
}
