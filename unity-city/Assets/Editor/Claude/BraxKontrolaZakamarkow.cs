using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// KONTROLA ZAKAMARKÓW MIASTA R1 (właściciel 16.11.2026 13:32: „samochody w powietrzu wiszą, las, slumsy — masz przejrzeć każdy
// zakamarek… sklepy: z zewnątrz niby mur, płyta, a wchodzisz do środka i szkło albo pustka na tej ścianie, prześwit”; 13:37:
// „każde wykończenie, każdy detal — poprawione jak trzeba”). Krok buildu rejonu (po ruchu aut i ludziach, przed anty-migotaniem),
// mierzy CAŁE miasto automatem, naprawia to, co da się naprawić bezpiecznie, i resztę wypisuje z miejscem (log + Logs/r1-zakamarki.json):
//
//  A. UZIEMIENIE — nic nie wisi i nic nie jest zakopane:
//     • auta stojące (parkingi, warsztaty, fawela) i jednoślady: pod KAŻDYM kołem promień w dół do nawierzchni z kolizją; auto, które
//       żadnym kołem nie dotyka, opada do pierwszego koła; auto wpuszczone kołem w nawierzchnię wychodzi na nią (bez podnośnika);
//     • las, zieleń faweli, drzewa, krzewy, paprocie, pniaki, kamienie: najniższy punkt modelu na NAJNIŻSZEJ nawierzchni pod
//       podstawą (środek + 4 punkty na obwodzie pnia / kępy) minus wpuszczenie — na stoku nie wisi strona od doliny;
//     • ławki, kosze, donice, stojaki, latarnie, hydranty na zewnątrz (nad nimi niebo): spód na nawierzchni.
//     Grunt = siatka z kolizją (MeshCollider / Terrain) albo duża płyta (≥ 4 × 4 m); kolizje pni, kamieni, aut — pomijane.
//  B. PRZEŚWITY ŚCIAN WE WNĘTRZACH — z każdego wnętrza z drzwiami (BraxCityDoor) i z każdego punktu lady / recepcji: 36 promieni
//     poziomo na 1.1 m i 2.1 m nad posadzką na tymczasowych kolizjach siatek WIDOCZNYCH (warstwa 30, trafiają też tył ścian):
//       – pierwsza ściana trafiona od TYŁU (lico na zewnątrz, z środka niewidoczna — „pustka na tej ścianie”) = PRZEŚWIT;
//       – szyba, a za nią w ≤ 1.2 m tył muru = SZKŁO NA MURZE („wchodzisz do środka i szkło”);
//       – nic w 40 m (poza osią drzwi) = PUSTKA.
//     Naprawa prześwitów i szkła na murze: PODSZYCIE — te same trójkąty ściany (ta sama płaszczyzna, ± 3 cm, w promieniu 25 m od
//     wnętrza, z otworami okien i drzwi jak w murze), odwrócone do środka i odsunięte 1.2 cm do wnętrza, z tynkiem Poly Haven
//     (white_plaster_02 / white_stucco, PBR, rzut w metrach). Z zewnątrz nic się nie zmienia (tył podszycia nie jest rysowany),
//     zero migotania (lico podszycia patrzy w przeciwną stronę niż lico muru). Pustki — tylko lista (nie ma czego podszyć).
public static class BraxKontrolaZakamarkow
{
    [Serializable] public class Wpis { public string rodzaj, nazwa, gdzie; public float cm; }
    [Serializable] public class Raport
    {
        public string kiedy;
        public int auta, autaOpuszczone, autaPodniesione, autaBezGruntu;
        public int rosliny, roslinyOpuszczone, roslinyBezGruntu;
        public int meble, mebleOpuszczone;
        public int wnetrza, wnetrzaPominiete, promienie, przeswity, szkloNaMurze, pustki, scianyPodszyte, trojkatyPodszyc;
        public float czasUziemienia, czasPrzeswitow;
        public List<Wpis> wpisy = new List<Wpis>();
    }
    static Raport rep;
    const int Warstwa = 30;

    public static void Wykonaj(BraxCityWorld world, Transform root)
    {
        rep = new Raport { kiedy = DateTime.UtcNow.ToString("u") };
        Physics.SyncTransforms();
        var t0 = DateTime.UtcNow;
        try { Uziemienie(root); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_UZIEMIENIE " + e); }
        rep.czasUziemienia = (float)(DateTime.UtcNow - t0).TotalSeconds;
        Physics.SyncTransforms();
        var t1 = DateTime.UtcNow;
        try { Przeswity(root.gameObject.scene, world, root, null); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_PRZESWITY " + e); }
        rep.czasPrzeswitow = (float)(DateTime.UtcNow - t1).TotalSeconds;
        Physics.SyncTransforms();
        try
        {
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/r1-zakamarki.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(p));
            File.WriteAllText(p, JsonUtility.ToJson(rep, true));
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_ZAKAMARKI zapis raportu: " + e.Message); }
        BraxRegion.Note("kontrola zakamarków: auta " + rep.auta + " (opuszczone " + rep.autaOpuszczone + ", podniesione " + rep.autaPodniesione + "), rośliny " + rep.rosliny +
                        " (opuszczone " + rep.roslinyOpuszczone + "), meble na zewnątrz " + rep.meble + " (opuszczone " + rep.mebleOpuszczone + "), wnętrza " + rep.wnetrza +
                        ", prześwity " + rep.przeswity + ", szkło na murze " + rep.szkloNaMurze + ", pustki " + rep.pustki + ", ściany podszyte " + rep.scianyPodszyte);
    }

    /// <summary>Druga runda prześwitów dla wnętrz zbudowanych PO kroku rejonu (supermarket — BraxGymBuild): ziarna, których nazwa
    /// (drzwi, punkt świata, przodek drzwi) zawiera `filtr`; podszycia pod osobnym korzeniem sceny. Raport Logs/r1-zakamarki-&lt;filtr&gt;.json.</summary>
    public static void WnetrzaPoBudowie(UnityEngine.SceneManagement.Scene scena, string filtr)
    {
        rep = new Raport { kiedy = DateTime.UtcNow.ToString("u") };
        var world = UnityEngine.Object.FindFirstObjectByType<BraxCityWorld>();
        var g = new GameObject("CLAUDE kontrola zakamarków (po budowie): " + filtr);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scena);
        string f = filtr.ToLowerInvariant();
        Physics.SyncTransforms();
        var t1 = DateTime.UtcNow;
        try { Przeswity(scena, world, g.transform, n => n.ToLowerInvariant().Contains(f)); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_PRZESWITY " + filtr + ": " + e); }
        rep.czasPrzeswitow = (float)(DateTime.UtcNow - t1).TotalSeconds;
        if (g.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(g);
        try
        {
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/r1-zakamarki-" + f + ".json"));
            Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p, JsonUtility.ToJson(rep, true));
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_ZAKAMARKI zapis raportu: " + e.Message); }
        Debug.Log("BRAXHORN_ZAKAMARKI " + filtr + ": wnętrza " + rep.wnetrza + ", prześwity " + rep.przeswity + ", szkło na murze " + rep.szkloNaMurze + ", pustki " + rep.pustki + ", ściany podszyte " + rep.scianyPodszyte);
    }

    static void Dodaj(string rodzaj, string nazwa, Vector3 p, float cm)
    {
        if (rep.wpisy.Count < 4000) rep.wpisy.Add(new Wpis { rodzaj = rodzaj, nazwa = nazwa, gdzie = p.ToString("F1"), cm = Mathf.Round(cm * 10f) / 10f });
    }

    // ================================================================================================================ A. UZIEMIENIE
    static Bounds Obrys(GameObject g, out bool jest)
    {
        jest = false; var b = new Bounds(g.transform.position, Vector3.zero);
        foreach (var r in g.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!jest) { b = r.bounds; jest = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    static bool Grunt(Collider c)
    {
        if (!c || c.isTrigger || !c.enabled) return false;
        if (c is MeshCollider || c is TerrainCollider) return true;
        var b = c.bounds; return b.size.x >= 4f && b.size.z >= 4f;
    }

    /// <summary>Najwyższa nawierzchnia z kolizją pod punktem (x, z), nie wyżej niż maxY; pomija własne kolizje obiektu i auta.</summary>
    static bool Nawierzchnia(Vector3 p, float maxY, Transform wlasny, out float y)
    {
        y = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(new Vector3(p.x, maxY + 2.5f, p.z), Vector3.down, 9f, ~((1 << Warstwa) | (1 << 8)), QueryTriggerInteraction.Ignore))
        {
            if (h.point.y > maxY || h.normal.y < .55f || !Grunt(h.collider)) continue;
            var t = h.collider.transform;
            if (wlasny && (t == wlasny || t.IsChildOf(wlasny))) continue;
            if (h.collider.GetComponentInParent<BraxAuto>() || h.collider.GetComponentInParent<BraxVessel>()) continue;
            if (h.point.y > y) y = h.point.y;
        }
        return !float.IsNegativeInfinity(y);
    }

    static string N(Transform t) => t.name.ToLowerInvariant();
    static readonly string[] Rosliny = { "drzew", "krzew", "paproć", "paproc", "pniak", "kłoda", "kloda", "kamień na stoku", "kamien na stoku", "głaz", "glaz", "shrub", "fern", "tree_stump", "boulder" };
    static readonly string[] NieRosliny = { "kolizja", "kolizji", "pień", "pien", "donic", "doniczk", "krata", "kratach", "w kracie", "napis", "szyld", "mural", "plakat", "billboard", "obraz", "tapeta", "z daleka", "atrapa", "liście", "korona" };
    static readonly string[] MebleZew = { "ławk", "lawk", "bench", "kosz", "litter bin", "donic", "planter", "stojak", "latarni", "hydrant", "słupek", "slupek", "bollard" };

    static void Uziemienie(Transform root)
    {
        var scena = root.gameObject.scene;
        var wszystkie = scena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(false)).ToList();
        // --- auta i jednoślady
        var auta = new List<Transform>();
        foreach (var t in wszystkie)
        {
            string n = t.name;
            bool auto = (n.StartsWith("Auto zaparkowane", StringComparison.Ordinal) || n.StartsWith("Auto w warsztacie", StringComparison.Ordinal)) && !n.Contains("podnośnik");
            var v = t.GetComponent<BraxVessel>(); if (v && v.lad) auto = true;
            if (auto) auta.Add(t);
        }
        foreach (var t in auta)
        {
            rep.auta++;
            if (!LokalnyObrys(t, out var lmin, out var lmax)) continue;
            // koła: ± 0.31 długości wzdłuż osi auta, ± 0.36 szerokości w bok, na spodzie modelu
            float zc = (lmin.z + lmax.z) * .5f, xc = (lmin.x + lmax.x) * .5f, dz = (lmax.z - lmin.z) * .31f, dx = (lmax.x - lmin.x) * .36f;
            var kola = new[] { new Vector3(xc - dx, lmin.y, zc - dz), new Vector3(xc + dx, lmin.y, zc - dz), new Vector3(xc - dx, lmin.y, zc + dz), new Vector3(xc + dx, lmin.y, zc + dz) };
            float minOdstep = float.PositiveInfinity, maxWkop = 0f; int trafione = 0;
            foreach (var k in kola)
            {
                var w = t.TransformPoint(k);
                if (!Nawierzchnia(w, w.y + .6f, t, out var g)) continue;
                trafione++; float odst = w.y - g;
                minOdstep = Mathf.Min(minOdstep, odst); maxWkop = Mathf.Max(maxWkop, -odst);
            }
            if (trafione < 3) { rep.autaBezGruntu++; Dodaj("auto bez nawierzchni pod kołami", t.name, t.position, 0f); continue; }
            if (minOdstep > .015f && minOdstep < 2.5f)
            {   // wisi: żadne koło nie dotyka — w dół do pierwszego koła (2 mm ugięcia opony)
                t.position -= Vector3.up * (minOdstep + .002f); rep.autaOpuszczone++; Dodaj("auto wisiało — opuszczone", t.name, t.position, minOdstep * 100f);
            }
            else if (maxWkop > .06f && maxWkop < 1.2f)
            {   // koło w nawierzchni (krawężnik, stok) — auto na nawierzchnię
                t.position += Vector3.up * (maxWkop - .002f); rep.autaPodniesione++; Dodaj("auto zakopane kołem — podniesione", t.name, t.position, maxWkop * 100f);
            }
            else if (minOdstep >= 2.5f) Dodaj("auto bardzo wysoko nad nawierzchnią — do sprawdzenia", t.name, t.position, minOdstep * 100f);
        }
        // --- rośliny i kamienie
        var zbior = new HashSet<Transform>();
        foreach (var t in wszystkie)
        {
            string n = N(t);
            if (!Rosliny.Any(n.Contains) || NieRosliny.Any(n.Contains)) continue;
            if (!t.GetComponentInChildren<Renderer>(false) || t.GetComponentInParent<BraxAuto>() || t.GetComponentInChildren<SkinnedMeshRenderer>(false)) continue;
            zbior.Add(t);
        }
        var rosliny = zbior.Where(t => { for (var q = t.parent; q; q = q.parent) if (zbior.Contains(q)) return false; return true; }).ToList();
        int bezGruntuRosl = 0;
        foreach (var t in rosliny)
        {
            var b = Obrys(t.gameObject, out var jest); if (!jest || b.size.y > 45f || b.size.x > 30f || b.size.z > 30f) continue;
            if (t.childCount > 40 && b.size.x > 12f) continue;   // grupa (las, zagajnik) — liczone są jej dzieci
            rep.rosliny++;
            string n = N(t);
            bool drzewo = n.Contains("drzew") || (n.Contains("tree") && !n.Contains("stump"));
            bool kamien = n.Contains("kamień") || n.Contains("kamien") || n.Contains("głaz") || n.Contains("glaz") || n.Contains("boulder");
            float rf = drzewo ? .32f : Mathf.Clamp(.35f * Mathf.Min(b.extents.x, b.extents.z), .12f, 1.1f);
            float wpust = drzewo ? .06f : kamien ? .08f : .03f;
            var c = new Vector3(t.position.x, b.min.y, t.position.z);
            if (!drzewo) c = new Vector3(b.center.x, b.min.y, b.center.z);
            float gMin = float.PositiveInfinity; int traf = 0;
            foreach (var o in new[] { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                if (!Nawierzchnia(c + o * rf, b.min.y + .6f, t, out var g)) continue;
                traf++; gMin = Mathf.Min(gMin, g);
            }
            if (traf < 3) { bezGruntuRosl++; continue; }
            float wisi = b.min.y - (gMin - wpust);
            if (wisi > .02f && wisi < 2.5f)
            {
                t.position -= Vector3.up * wisi; rep.roslinyOpuszczone++;
                if (wisi > .08f) Dodaj("roślina / kamień wisiała — opuszczona", t.name, t.position, wisi * 100f);
            }
            else if (wisi >= 2.5f) Dodaj("roślina / kamień bardzo wysoko nad nawierzchnią — do sprawdzenia", t.name, t.position, wisi * 100f);
        }
        rep.roslinyBezGruntu = bezGruntuRosl;
        // --- meble miejskie na zewnątrz
        var zm = new HashSet<Transform>();
        foreach (var t in wszystkie)
        {
            string n = N(t);
            if (!MebleZew.Any(n.Contains) || n.Contains("kolizja") || n.Contains("napis") || n.Contains("szyld") || n.Contains("wisz") || n.Contains("kinkiet")) continue;
            if (!t.GetComponentInChildren<Renderer>(false) || t.GetComponentInParent<BraxAuto>()) continue;
            zm.Add(t);
        }
        foreach (var t in zm.Where(t => { for (var q = t.parent; q; q = q.parent) if (zm.Contains(q)) return false; return true; }))
        {
            var b = Obrys(t.gameObject, out var jest); if (!jest || b.size.x > 8f || b.size.z > 8f || b.size.y > 14f) continue;
            // tylko na zewnątrz: nad meblem niebo (nic z kolizją w 30 m)
            bool dach = Physics.RaycastAll(new Vector3(b.center.x, b.max.y + .05f, b.center.z), Vector3.up, 30f, ~((1 << Warstwa) | (1 << 8)), QueryTriggerInteraction.Ignore)
                .Any(h => !h.collider.transform.IsChildOf(t));
            if (dach) continue;
            rep.meble++;
            float gMax = float.NegativeInfinity; int traf = 0;
            var ins = new Vector3(Mathf.Max(0f, b.extents.x - .05f), 0f, Mathf.Max(0f, b.extents.z - .05f));
            foreach (var o in new[] { Vector3.zero, new Vector3(ins.x, 0, ins.z), new Vector3(-ins.x, 0, ins.z), new Vector3(ins.x, 0, -ins.z), new Vector3(-ins.x, 0, -ins.z) })
            {
                if (!Nawierzchnia(new Vector3(b.center.x, b.min.y, b.center.z) + o, b.min.y + .3f, t, out var g)) continue;
                traf++; gMax = Mathf.Max(gMax, g);
            }
            if (traf < 3) continue;
            float wisi = b.min.y - gMax;
            if (wisi > .02f && wisi < .8f) { t.position -= Vector3.up * wisi; rep.mebleOpuszczone++; Dodaj("mebel miejski wisiał — opuszczony", t.name, t.position, wisi * 100f); }
        }
        Debug.Log("BRAXHORN_UZIEMIENIE auta=" + rep.auta + " opuszczone=" + rep.autaOpuszczone + " podniesione=" + rep.autaPodniesione + " bez_gruntu=" + rep.autaBezGruntu +
                  " | rośliny/kamienie=" + rep.rosliny + " opuszczone=" + rep.roslinyOpuszczone + " bez_gruntu=" + rep.roslinyBezGruntu +
                  " | meble_zewn=" + rep.meble + " opuszczone=" + rep.mebleOpuszczone +
                  " | największe: " + string.Join("; ", rep.wpisy.OrderByDescending(w => w.cm).Take(25).Select(w => w.nazwa + " " + w.cm.ToString("F0") + " cm @" + w.gdzie)));
    }

    /// <summary>Obrys siatek obiektu w jego własnym układzie (auto obrócone i pochylone — koła w osiach auta).</summary>
    static bool LokalnyObrys(Transform t, out Vector3 mn, out Vector3 mx)
    {
        mn = Vector3.one * float.PositiveInfinity; mx = Vector3.one * float.NegativeInfinity; bool jest = false;
        var w2l = t.worldToLocalMatrix;
        foreach (var mf in t.GetComponentsInChildren<MeshFilter>(false))
        {
            var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled || !mf.sharedMesh) continue;
            var b = mf.sharedMesh.bounds; var m = w2l * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var p = m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); jest = true;
            }
        }
        return jest;
    }

    // ================================================================================================================ B. PRZEŚWITY
    sealed class Cel { public Renderer r; public Mesh m; public Material[] mats; }
    sealed class Ziarno { public string nazwa; public Vector3 p; public Vector3 drzwiNaZewn; public bool zDrzwi; }
    struct Trafienie { public Collider c; public int tri; public Vector3 p, n; public float d; }

    static bool Przezroczysty(Material m)
    {
        if (!m) return false;
        if (m.renderQueue >= 2900) return true;
        if (m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f) return true;
        if (m.HasProperty("_Mode") && m.GetFloat("_Mode") >= 2f) return true;
        string n = m.name.ToLowerInvariant();
        return n.Contains("szkło") || n.Contains("szklo") || n.Contains("szyba") || n.Contains("glass") || n.Contains("witryn");
    }
    static bool Dwustronny(Material m) => m && m.HasProperty("_Cull") && m.GetFloat("_Cull") < .5f;
    // meble i wyposażenie (plecy regałów, lady, skrzydła drzwi) bywają jednostronne — to nie ściany; liczone i podszywane są tylko ściany
    static readonly string[] Meblowe = { "regał", "regal", "lada", "mebl", "półk", "polk", "szafk", "stół", "stol", "krzes", "sofa", "kanap", "ekran", "tv", "telewiz", "drzwi", "door", "skrzydł", "witryna chłodnicza", "lodówk", "chłodni", "towar", "kasa", "manekin", "lustro", "obraz", "plakat", "tablica" };
    static bool Sciana(Cel c, Vector3 n) => Mathf.Abs(n.y) < .35f && c.r.bounds.size.y >= 2.0f && !Meblowe.Any(N(c.r.transform).Contains);
    static readonly string[] Pomijaj = { "rośl", "rosl", "kwiat", "plant", "fern", "liść", "lisc", "leaf", "palm", "drzew", "krzew", "trawa", "grass", "z daleka", "niebo", "sky", "horyzont", "panorama", "cień", "shadow", "napis", "litery" };

    static Material MatTrojkata(Cel c, int tri)
    {
        if (c.mats == null || c.mats.Length == 0) return null;
        if (c.m.subMeshCount <= 1 || tri < 0) return c.mats[0];
        int idx = tri * 3;
        for (int s = 0; s < c.m.subMeshCount; s++)
        {
            var d = c.m.GetSubMesh(s);
            if (d.topology != MeshTopology.Triangles) continue;
            if (idx >= d.indexStart && idx < d.indexStart + d.indexCount) return c.mats[Mathf.Min(s, c.mats.Length - 1)];
        }
        return c.mats[0];
    }

    static string Przodkowie(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 300; p = p.parent) s = p.name + "/" + s; return s; }

    // filtr == null: wszystkie wnętrza sceny (krok rejonu); filtr: tylko ziarna o pasującej nazwie (runda po budowie supermarketu)
    static void Przeswity(UnityEngine.SceneManagement.Scene scena, BraxCityWorld world, Transform root, Func<string, bool> filtr)
    {
        // --- ziarna: wnętrza za drzwiami, miejsca lad / recepcji / kas i punkty „w środku” sklepów i lokali
        var ziarna = new List<Ziarno>();
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!d || d.gameObject.scene != scena) continue;
            if (filtr != null && !filtr(Przodkowie(d.transform) + " " + d.label + " " + d.id)) continue;
            var f = d.transform.forward; f.y = 0f; if (f.sqrMagnitude < 1e-4f) continue; f.Normalize();
            ziarna.Add(new Ziarno { nazwa = "drzwi " + (string.IsNullOrEmpty(d.label) ? d.id : d.label), p = d.transform.position, drzwiNaZewn = f, zDrzwi = true });
        }
        if (world && world.points != null)
            foreach (var pt in world.points)
            {
                if (pt == null || string.IsNullOrEmpty(pt.id)) continue;
                string id = pt.id.ToLowerInvariant();
                if (filtr != null ? filtr(id) : (id.Contains("lada") || id.Contains("recepcja") || id.Contains("wnetrze") || id.Contains("kasa") || id.Contains("srodek") || id.Contains("sklep")))
                    ziarna.Add(new Ziarno { nazwa = "punkt " + pt.id, p = pt.position });
            }
        if (ziarna.Count == 0) { Debug.Log("BRAXHORN_PRZESWITY brak drzwi i punktów wnętrz — nic do sprawdzenia"); return; }
        // --- tymczasowe kolizje siatek widocznych (warstwa 30) w zasięgu 26 m od ziaren
        var cele = new Dictionary<Collider, Cel>(); var tymczasowe = new List<GameObject>();
        var pola = ziarna.Select(z => new Bounds(z.p, new Vector3(52f, 24f, 52f))).ToList();
        var start = DateTime.UtcNow; bool przerwane = false;
        bool hitBack = Physics.queriesHitBackfaces;
        try
        {
            foreach (var r in scena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)))
            {
                if ((DateTime.UtcNow - start).TotalSeconds > 150) { przerwane = true; break; }
                if (!r.enabled || r.gameObject.layer == 8 || r.gameObject.layer == BraxKontrola.WarstwaSondy) continue;
                var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m || m.vertexCount < 3) continue;
                string n = N(r.transform); if (Pomijaj.Any(n.Contains)) continue;
                var rb = r.bounds; bool blisko = false;
                foreach (var pb in pola) if (pb.Intersects(rb)) { blisko = true; break; }
                if (!blisko) continue;
                var go = new GameObject("zakamarki: kolizja tymczasowa") { layer = Warstwa, hideFlags = HideFlags.DontSave };
                go.transform.SetParent(r.transform, false);
                MeshCollider mc;
                try { mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = m; }
                catch { UnityEngine.Object.DestroyImmediate(go); continue; }
                tymczasowe.Add(go); cele[mc] = new Cel { r = r, m = m, mats = r.sharedMaterials };
            }
            Physics.SyncTransforms();
            Physics.queriesHitBackfaces = true;
            int maska = 1 << Warstwa;
            var podszyc = new Dictionary<Collider, List<(Vector3 p, Vector3 n, Vector3 z, float y0)>>();
            foreach (var z in ziarna)
            {
                // posadzka i strona wnętrza: dla drzwi — strona z sufitem 2–4 m za progiem
                var kand = new List<Vector3>();
                if (z.zDrzwi) { kand.Add(z.p - z.drzwiNaZewn * 2.2f); kand.Add(z.p - z.drzwiNaZewn * 4.0f); kand.Add(z.p + z.drzwiNaZewn * 2.2f); }
                else kand.Add(z.p);
                Vector3? wnetrze = null; float posadzka = 0f;
                foreach (var k in kand)
                {
                    var gora = new Vector3(k.x, k.y + 1.2f, k.z);
                    bool sufit = Physics.Raycast(gora, Vector3.up, 12f, maska, QueryTriggerInteraction.Ignore);
                    if (!sufit) continue;
                    float py = k.y;
                    if (Physics.Raycast(gora, Vector3.down, out var hp, 3f, maska, QueryTriggerInteraction.Ignore)) py = hp.point.y;
                    wnetrze = new Vector3(k.x, py, k.z); posadzka = py; break;
                }
                if (wnetrze == null) { rep.wnetrzaPominiete++; continue; }
                var w = wnetrze.Value;
                // zamknięte? ≥ 70 % promieni coś trafia w 25 m (inaczej to plac / ulica, nie wnętrze)
                int traf = 0;
                for (int a = 0; a < 12; a++) { var d = Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward; if (Physics.Raycast(w + Vector3.up * 1.4f, d, 25f, maska, QueryTriggerInteraction.Ignore)) traf++; }
                if (traf < 9) { rep.wnetrzaPominiete++; continue; }
                rep.wnetrza++;
                int przZ = 0, szkZ = 0, pusZ = 0;
                foreach (float hy in new[] { 1.1f, 2.1f })
                    for (int a = 0; a < 36; a++)
                    {
                        var dir = Quaternion.Euler(0f, a * 10f + 5f, 0f) * Vector3.forward;
                        var o = w + Vector3.up * hy; rep.promienie++;
                        var hits = Physics.RaycastAll(o, dir, 40f, maska, QueryTriggerInteraction.Ignore).Where(h => h.distance > .15f).OrderBy(h => h.distance).ToArray();
                        if (hits.Length == 0)
                        {
                            if (z.zDrzwi && Vector3.Angle(dir, z.drzwiNaZewn) < 35f) continue;   // przez drzwi na ulicę
                            pusZ++; rep.pustki++; if (pusZ <= 2) Dodaj("PUSTKA (nic w 40 m)", z.nazwa, o + dir * 3f, 0f); continue;
                        }
                        var h0 = hits[0]; if (!cele.TryGetValue(h0.collider, out var c0)) continue;
                        var m0 = MatTrojkata(c0, h0.triangleIndex);
                        if (Przezroczysty(m0))
                        {
                            for (int i = 1; i < hits.Length; i++)
                            {
                                if (hits[i].distance - h0.distance > 1.2f) break;
                                if (!cele.TryGetValue(hits[i].collider, out var ci)) continue;
                                var mi = MatTrojkata(ci, hits[i].triangleIndex);
                                if (Przezroczysty(mi)) continue;
                                if (Vector3.Dot(hits[i].normal, dir) > 0f && !Dwustronny(mi) && Sciana(ci, hits[i].normal))
                                {
                                    szkZ++; rep.szkloNaMurze++;
                                    if (!podszyc.TryGetValue(hits[i].collider, out var l1)) podszyc[hits[i].collider] = l1 = new List<(Vector3, Vector3, Vector3, float)>();
                                    l1.Add((hits[i].point, hits[i].normal, w, posadzka));
                                    if (szkZ <= 2) Dodaj("SZKŁO NA MURZE od środka: " + ci.r.name, z.nazwa, hits[i].point, 0f);
                                }
                                break;
                            }
                            continue;
                        }
                        if (Vector3.Dot(h0.normal, dir) > 0f && !Dwustronny(m0) && Sciana(c0, h0.normal))
                        {
                            przZ++; rep.przeswity++;
                            if (!podszyc.TryGetValue(h0.collider, out var l0)) podszyc[h0.collider] = l0 = new List<(Vector3, Vector3, Vector3, float)>();
                            l0.Add((h0.point, h0.normal, w, posadzka));
                            if (przZ <= 2) Dodaj("PRZEŚWIT (ściana widoczna tylko z zewnątrz): " + c0.r.name, z.nazwa, h0.point, 0f);
                        }
                    }
                if (przZ + szkZ + pusZ > 0)
                    Debug.LogWarning("BRAXHORN_PRZESWITY " + z.nazwa + " @" + w.ToString("F1") + ": prześwity " + przZ + ", szkło na murze " + szkZ + ", pustki " + pusZ + " (na 72 promienie)");
            }
            // --- podszycie od środka
            if (podszyc.Count > 0) Podszyj(root, podszyc, cele);
        }
        finally
        {
            Physics.queriesHitBackfaces = hitBack;
            foreach (var g in tymczasowe) if (g) UnityEngine.Object.DestroyImmediate(g);
            Physics.SyncTransforms();
        }
        Debug.Log("BRAXHORN_PRZESWITY wnętrz=" + rep.wnetrza + " (pominięte jako plener " + rep.wnetrzaPominiete + "), promieni=" + rep.promienie + ", prześwity=" + rep.przeswity +
                  ", szkło_na_murze=" + rep.szkloNaMurze + ", pustki=" + rep.pustki + ", podszyte ściany=" + rep.scianyPodszyte + " (trójkątów " + rep.trojkatyPodszyc + ")" +
                  (przerwane ? ", KOLIZJE TYMCZASOWE PRZERWANE LIMITEM CZASU (część wnętrz bez sprawdzenia)" : "") + ", czas " + (DateTime.UtcNow - start).TotalSeconds.ToString("F0") + " s");
    }

    static void Podszyj(Transform root, Dictionary<Collider, List<(Vector3 p, Vector3 n, Vector3 z, float y0)>> podszyc, Dictionary<Collider, Cel> cele)
    {
        var mat = BraxUliceKit.Tex("wnętrze: tynk ściany (podszycie od środka)", 3.0f, new Color(.96f, .95f, .93f), new Color(.90f, .89f, .86f), .9f, "white_plaster_02", "white_stucco");
        var rodzic = BraxRegion.Root(root, "CLAUDE kontrola zakamarków: podszycia ścian od środka");
        string dir = BraxRegion.Out + "/Zakamarki"; Directory.CreateDirectory(dir);
        int lacznie = 0, nr = 0;
        foreach (var kv in podszyc)
        {
            if (!cele.TryGetValue(kv.Key, out var c) || !c.r) continue;
            var m = c.m; var v = m.vertices; var tri = m.triangles; var l2w = c.r.transform.localToWorldMatrix;
            var s = new UlSiatka(); var uzyte = new HashSet<int>();
            foreach (var (hp, hn, z, y0) in kv.Value)
            {
                var n = hn.normalized;
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    int ti = i / 3; if (uzyte.Contains(ti)) continue;
                    var a = l2w.MultiplyPoint3x4(v[tri[i]]); var b = l2w.MultiplyPoint3x4(v[tri[i + 1]]); var d = l2w.MultiplyPoint3x4(v[tri[i + 2]]);
                    var cr = Vector3.Cross(b - a, d - a); if (cr.sqrMagnitude < 1e-10f) continue;
                    var nt = cr.normalized; if (Vector3.Dot(nt, n) < .985f || Mathf.Abs(nt.y) > .35f) continue;
                    var sr = (a + b + d) / 3f;
                    if (Mathf.Abs(Vector3.Dot(sr - hp, n)) > .03f) continue;
                    if ((sr - z).sqrMagnitude > 25f * 25f || sr.y < y0 - .3f || sr.y > y0 + 7f) continue;
                    if (Vector3.Dot(z - sr, n) >= 0f) continue;          // wnętrze musi być za tą ścianą (po stronie tyłu lica)
                    uzyte.Add(ti);
                    var off = -n * .012f;
                    s.Tri(a + off, b + off, d + off, -n);
                    if (uzyte.Count > 20000) break;
                }
                if (uzyte.Count > 20000) break;
            }
            if (s.Wierzcholki == 0) continue;
            var mesh = s.Zbuduj("Podszycie od środka: " + c.r.name);
            BraxZapisAssetu.Utworz(mesh, dir + "/podszycie_" + (nr++).ToString("D4") + ".asset");
            var go = new GameObject("Podszycie ściany od środka (tynk): " + c.r.name);
            go.transform.SetParent(rodzic, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            rep.scianyPodszyte++; rep.trojkatyPodszyc += uzyte.Count; lacznie += uzyte.Count;
            Dodaj("podszyta ściana (tynk od środka)", c.r.name, kv.Value[0].p, 0f);
            if (lacznie > 250000) { Debug.LogWarning("BRAXHORN_PRZESWITY podszycia: limit 250 tys. trójkątów — reszta tylko na liście"); break; }
        }
    }
}
