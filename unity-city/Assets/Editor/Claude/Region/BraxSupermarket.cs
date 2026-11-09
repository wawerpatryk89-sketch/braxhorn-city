using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// SUPERMARKET BRAXHORN (właściciel 3.11 13:39–13:46: „wystarczy jeden zbudować i potem go kopiować, poprzestawiać kasy”;
// „piękny szyld premium, wózki przed wejściem, parę sztuk, w środku ładnie zrobione”).
// Jeden moduł w lokalnym układzie (x wzdłuż frontu, z w głąb, front w z = 0): hala W × D × H (× K), jedno wejście dla klientów
// (drzwi automatyczne), witryna ze szprosami, podświetlany szyld, alejki regałów-wysp z towarem (BraxSklepyTowar), kasy,
// lodówki pod tylną ścianą, stoisko warzyw, wózki w środku i przed wejściem. Kopia nr 2: odbicie lustrzane (wejście i kasy po
// drugiej stronie, kasy obrócone). Miejsce: automatyczne szukanie wolnej, płaskiej działki na lądzie — bez jezdni pod halą
// (z marginesem 4 m), z jezdnią przed frontem (6–16 m), bez kolizji nad gruntem; kopie ≥ 150 m od siebie.
// Twarde ściany bez nakładania równoległych ścian (bez migotania). Log BRAXHORN_SUPERMARKET.
public static class BraxSupermarket
{
    static float K => BraxClaudeOsiedle.K;
    static float W => 28f * K;
    static float D => 20f * K;
    static float H => 4.6f * K;
    const float Podloga = .12f, Grub = .3f;

    // segmenty regału-wyspy regal_sklep (sklepy_przetworz.py, metry modelu przed × K): z0, z1, poziomy półek, asortyment
    static readonly (float z0, float z1, float[] y, string typ)[] Segmenty =
    {
        (-2.30f, -1.20f, new[] { .21f, .47f, .75f, 1.03f, 1.31f }, "sm_a"),
        (-.55f, .55f, new[] { .21f, .57f, .91f, 1.25f }, "sm_b"),
        (1.20f, 2.30f, new[] { .21f, .67f, 1.15f }, "sm_c"),
    };

    public static void Wykonaj(Scene scena, int ile = 2)
    {
        if (!BraxMiastoAssety.Jest("miasto_sklepy")) { Debug.LogWarning("BRAXHORN_SUPERMARKET brak paczki miasto_sklepy — supermarket pominięty (bez regałów byłby pusty)"); return; }
        Physics.SyncTransforms();
        var root = new GameObject("SUPERMARKET BRAXHORN (moduł kopiowany)");
        SceneManager.MoveGameObjectToScene(root, scena);
        var miejsca = new List<Vector3>(); var stoper = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < ile; i++)
        {
            Vector3 c; float yaw;
            if (i == 0)
            {
                // 9.11 (właściciel: „rondo koło sklepu precz ma być parking dla sklepu, w sklepie stoi drzewo i nie można wejść,
                // chodnik źle poprowadzony”). W APK 8.11 parking stał na stałych (−126, 104), a sklep #1 Szukaj() postawił na (−108, 132):
                // dojście kończyło się 22 m przed frontem i 18 m w bok. Sklep #1 stoi NA STAŁE przed parkingiem: oś x = AX (−126),
                // front na północnej krawędzi dojścia (PetlaZ + PetlaPlac + 5.5), wejście od parkingu (−z). Sklep #2 — nadal Szukaj().
                float frontZ = BraxRegionMiastoB.PetlaZ + BraxRegionMiastoB.PetlaPlac + 5.5f;
                float y = BraxRegion.Ground(BraxRegionMiastoB.AX, frontZ + D * .5f, -20f, 80f, out var hg) ? hg.point.y : 0f;
                c = new Vector3(BraxRegionMiastoB.AX, y, frontZ); yaw = 0f;
                bool wolne = Pasuje(new Vector2(c.x, c.z), yaw, out _);
                Debug.Log("BRAXHORN_SUPERMARKET kopia 1: miejsce STAŁE przy parkingu (oś alei x=" + BraxRegionMiastoB.AX + ", front z=" + frontZ.ToString("F1") +
                          ", hala do z=" + (frontZ + D).ToString("F1") + ") test_dzialki=" + (wolne ? "wolna" : "KOLIZJA_Z_OTOCZENIEM — sprawdzić na nagraniu (OczyscWejscie usuwa drzewa/płoty w hali i w drzwiach)"));
            }
            else if (!Szukaj(miejsca, out c, out yaw)) { Debug.LogWarning("BRAXHORN_SUPERMARKET kopia " + (i + 1) + ": brak wolnej działki — DO POPRAWKI (miejsce ręczne)"); break; }
            miejsca.Add(c);
            var t = new GameObject("SUPERMARKET BRAXHORN " + (i + 1)).transform; t.SetParent(root.transform, false);
            t.position = c; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            bool lustro = i % 2 == 1;
            // Odbiór po zbudowaniu faktycznej bryły. Tylko drzewo/płot,
            // którego siatka przecina wnętrze lub światło wejścia.
            int towar = Zbuduj(t, lustro, i + 1);
            OczyscWejscie(scena, root.transform, t, lustro, i + 1);
            try { OczyscHale(scena, root.transform, t, i + 1); } catch (Exception e) { Debug.LogWarning("BRAXHORN_SUPERMARKET_HALA DO POPRAWKI sklep " + (i + 1) + ": " + e.Message); }
            Physics.SyncTransforms();
            Debug.Log("BRAXHORN_SUPERMARKET kopia " + (i + 1) + ": front (środek) " + c.ToString("F1") + ", obrót " + yaw + "°, hala " + W.ToString("F1") + " × " + D.ToString("F1") + " × " + H.ToString("F1") + " m"
                      + (lustro ? ", odbicie lustrzane (wejście i kasy po drugiej stronie)" : "") + ", towar modeli " + towar + ", szukanie " + stoper.ElapsedMilliseconds + " ms");
        }
    }

    // Utrzymanie istniejącego sklepu i zatwierdzonych produktów.
    // Źródło problemu na zdjęciu: drzewo i odcinki płotu spoza modułu
    // supermarketu potrafią wizualnie przechodzić przez wejście.
    static void OczyscWejscie(Scene scene, Transform shopRoot, Transform store,
                             bool mirror, int nr)
    {
        float half = W * .5f, entryX = WejscieX(mirror, nr);
        float doorWidth = 2.4f*K, bodyHeight = 2.6f*K;
        Vector3 door = store.TransformPoint(new Vector3(entryX, 0f, 0f));
        // Objętość wycięcia w WORLD AABB musi uwzględnić obrót sklepu
        // 0/90/180/270; inaczej w sklepie obróconym o 90° tnie obok drzwi.
        var xaxis = store.right;
        var zaxis = store.forward;
        var bbWidth = doorWidth+1.25f;
        var bbDepth = 9.8f;
        var cutSize = new Vector3(
            Mathf.Abs(xaxis.x)*bbWidth+Mathf.Abs(zaxis.x)*bbDepth,
            bodyHeight+.4f,
            Mathf.Abs(xaxis.z)*bbWidth+Mathf.Abs(zaxis.z)*bbDepth);
        var opening = new Bounds(store.TransformPoint(
            new Vector3(entryX, bodyHeight*.5f, -.9f)), cutSize);
        // Oś sklepu może być obrócona o 90°, zatem weryfikacja terenu
        // i wejścia używa współrzędnych LOKALNYCH sklepu.
        bool Inside(Vector3 p, bool hall)
        {
            var q = store.InverseTransformPoint(p);
            return hall
                ? Mathf.Abs(q.x) < half-.15f && q.z > 0f && q.z < D-.1f
                : Mathf.Abs(q.x-entryX) < doorWidth*.5f+.8f && q.z > -5.3f && q.z < 2f;
        }
        bool Named(string x, params string[] parts) =>
            parts.Any(p => x.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        // 9.11: drzewa alej B to „Aleja: drzewo” + osobna kapsuła „Aleja: pień kolizja”; drzewa R1 „Drzewo: …” + „Drzewo: kolizja pnia”;
        // las/plaża „Las za plażą: drzewo”. Kapsuły pni rozpoznajemy po „pień/pnia/trunk”. „street” zawiera „tree”: NIE usuwaj ulicy.
        bool IsTree(string x) => Named(x,"drzewo","tree_","_tree","tree ","palm_","palma","palm tree","verdant","pień","pnia","trunk");
        bool IsFence(string x) => Named(x,"płot","plot","fence","ogrodzen");
        int trees=0, trunks=0, fenceParts=0, unidentified=0;
        var generated = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go || go.transform == shopRoot || go.transform.IsChildOf(shopRoot)) continue;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r || !r.gameObject.activeInHierarchy || !r.enabled) continue;
                var candidate=r.transform;
                Transform tree=null,fence=null;
                for (var p=candidate; p && p!=go.transform.parent; p=p.parent)
                {
                    if (IsTree(p.name) && tree==null) tree=p;
                    if (IsFence(p.name) && fence==null) fence=p;
                }
                if (tree && !generated.Contains(tree.gameObject) &&
                    (Inside(tree.position,true) || Inside(tree.position,false)))
                {
                    if (tree.childCount > 128 || tree==go.transform)
                    { unidentified++; continue; }  // nie kasuj całego lasu/budynku
                    generated.Add(tree.gameObject);
                    trees++;
                    continue;
                }
                if (!fence || !r.bounds.Intersects(opening)) continue;
                // Wytnij 2.4m przejścia z SIATKI PŁOTU, a nie cały płot wokół działki.
                try
                {
                    fenceParts += BraxCityMeshCut.Subtract(candidate, opening,
                        BraxRegion.Out + "/Cuts","supermarket_przejscie_"+nr);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("DO POPRAWKI: SUPERMARKET "+nr+
                        " płot "+r.name+" "+e.Message); unidentified++;
                }
            }
        }
        // 9.11 (właściciel: „w sklepie stoi drzewo i nie można wejść” — nadal w APK 9.11, zdjęcie 16:40): drzewa źródła i zieleni
        // nie mają nazw „drzewo/tree” (scalone, „CityMesh_…”, LOD Verdant) — rozpoznanie po nazwie nic nie dało (usuniete_drzewa=0).
        // GEOMETRIA: każdy obcy obiekt stojący (wys. > 0.6 m, obrys < 12 m — drzewo, krzew, latarnia, ławka, kosz, znak) ze ŚRODKIEM
        // w hali albo w świetle wejścia — precz w całości (z LODGroup, żeby nie została korona/pień innego LOD). Nawierzchnie
        // (płaskie) i budynki (duże) nietknięte.
        var obce = new HashSet<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go) continue;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r || r is SkinnedMeshRenderer || r.transform.IsChildOf(shopRoot) || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var rb = r.bounds;
                if (rb.size.y < .6f || rb.size.x > 12f || rb.size.z > 12f) continue;
                var stopa = new Vector3(rb.center.x, rb.min.y + .05f, rb.center.z);
                if (!Inside(stopa, true) && !Inside(stopa, false)) continue;
                var lg = r.GetComponentInParent<LODGroup>();
                var cel = lg && !lg.transform.IsChildOf(shopRoot) && BraxRegion.Bounds(lg.gameObject).size.x < 12f ? lg.gameObject : r.gameObject;
                if (cel.transform == go.transform && go.transform.childCount > 64) continue;   // nie kasuj całego korzenia sceny
                obce.Add(cel);
            }
        }
        foreach (var o in obce) if (o && !generated.Contains(o)) { generated.Add(o); trees++; }
        foreach (var tr in generated) if (tr) UnityEngine.Object.DestroyImmediate(tr);
        // kolizje bez renderera (pnie, słupki) w hali i w wejściu — też precz (nie kolizje samego sklepu)
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go) continue;
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                if (!c || c.transform.IsChildOf(shopRoot) || c.isTrigger) continue;
                var cb = c.bounds;
                if (cb.size.y < .6f || cb.size.x > 12f || cb.size.z > 12f) continue;
                var stopa = new Vector3(cb.center.x, cb.min.y + .05f, cb.center.z);
                if (Inside(stopa, true) || Inside(stopa, false)) { UnityEngine.Object.DestroyImmediate(c); trunks++; }
            }
        }

        // Usunięte drzewa nie mogą pozostawić niewidzialnego pnia kolizji.
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go || go.transform==shopRoot) continue;
            foreach(var c in go.GetComponentsInChildren<CapsuleCollider>(true))
            {
                if (!c || !c.enabled || !IsTree(c.name)) continue;
                if (Inside(c.transform.position,true) || Inside(c.transform.position,false))
                { UnityEngine.Object.DestroyImmediate(c.gameObject); trunks++; }
            }
        }
        Physics.SyncTransforms();

        // 12 próbek wejście → wnętrze, capsule Braxa; drzwi ruchome
        // mają własne skrzydła/collider i mogą być zamknięte w edit mode.
        // Takich drzwi nie traktujemy jako niewidzialnej przeszkody.
        int samples=0;var obstacles=new HashSet<string>();
        float radius=.32f, h=2.2f*K;
        for(int i=0;i<=11;i++)
        {
            float z=Mathf.Lerp(-4.0f,3.5f,i/11f);
            Vector3 foot=store.TransformPoint(new Vector3(entryX,.12f,z));
            foreach(var c in Physics.OverlapCapsule(foot+Vector3.up*(radius+.06f),
                foot+Vector3.up*(h-radius),radius,
                ~((1<<8)|(1<<29)),QueryTriggerInteraction.Ignore))
            {
                if(!c || !c.enabled || c.isTrigger)continue;
                if(c.transform.IsChildOf(store))
                {
                    if(c.GetComponentInParent<BraxDrzwiPrzesuwne>())continue;
                    // podłoga leży poniżej kapsuły, kas i regałów nie usuwamy
                }
                if (obstacles.Count < 28) obstacles.Add(c.name);
            }
            samples++;
        }
        Debug.Log("BRAXHORN_SUPERMARKET_WEJSCIE sklep="+nr+
            " usuniete_drzewa="+trees+" pnie="+trunks+" wyciete_ploty="+fenceParts+
            " brak_rozpoznania="+unidentified+" probki="+samples+
            " potencjalne_przeszkody="+obstacles.Count+
            (obstacles.Count>0?" ["+string.Join("; ",obstacles)+"]":""));
    }

    // 9.11 (właściciel, zdjęcie 17:30: „sklep funkcjonalny, drzewa usunąć z niego, dojście ma być, wszystko”; w APK 9.11 w hali
    // stało drzewo, na posadzce trawnik, z tyłu płot): sklep stoi na stałe przy parkingu, na terenie z trawnikiem, drzewami
    // i ogrodzeniem. Wnętrze hali (obrys ścian, od 0.25 m pod wierzchem posadzki do 3 m nad dachem) jest WYCINANE z każdej obcej
    // siatki sceny (trawnik, płot, drzewo, grunt wystający nad posadzkę) — BraxCityMeshCut, kolizje siatek aktualizowane;
    // obce kolizje pudełkowe przycięte do hali, kapsuły/kule i drobne kolizje w hali usunięte; kępy trawy (instancje) z hali
    // usunięte z danych. Sam sklep (korzeń supermarketu), gracz i kamery nietknięte.
    static void OczyscHale(Scene scene, Transform shopRoot, Transform store, int nr)
    {
        Physics.SyncTransforms();
        float hw = W * .5f, y0 = store.position.y + Podloga - .25f, y1 = store.position.y + H + 3f;
        Vector3 lo = new Vector3(float.MaxValue, y0, float.MaxValue), hi = new Vector3(float.MinValue, y1, float.MinValue);
        foreach (var q in new[] { new Vector3(-hw, 0, 0), new Vector3(hw, 0, 0), new Vector3(-hw, 0, D), new Vector3(hw, 0, D) })
        { var w = store.TransformPoint(q); lo.x = Mathf.Min(lo.x, w.x); lo.z = Mathf.Min(lo.z, w.z); hi.x = Mathf.Max(hi.x, w.x); hi.z = Mathf.Max(hi.z, w.z); }
        var hala = new Bounds(); hala.SetMinMax(lo, hi);   // sklep obrócony o 0/90/180/270 — obrys w osiach świata dokładny
        bool Chroniony(Transform t) => t.IsChildOf(shopRoot) || t.GetComponentInParent<BraxGymPlayer>() || t.GetComponentInParent<Camera>();
        bool WHali(Vector3 p) { var q = store.InverseTransformPoint(p); return Mathf.Abs(q.x) < hw && q.z > 0f && q.z < D; }
        int siatki = 0, kolPrzyciete = 0, kolUsuniete = 0, kepy = 0;
        // 1) siatki: precyzyjne cięcie (zostaje to, co poza halą)
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go || Chroniony(go.transform)) continue;
            if (!go.GetComponentsInChildren<MeshRenderer>(true).Any(r => r && r.enabled && r.bounds.Intersects(hala) && !Chroniony(r.transform))) continue;
            try { siatki += BraxCityMeshCut.Subtract(go.transform, hala, BraxRegion.Out + "/Cuts", "supermarket_hala_" + nr); }
            catch (Exception e) { Debug.LogWarning("BRAXHORN_SUPERMARKET_HALA cięcie " + go.name + ": " + e.Message); }
        }
        // 2) kolizje obce w hali
        var reszta = new GameObject("SUPERMARKET " + nr + ": przycięte kolizje otoczenia").transform; reszta.SetParent(shopRoot, false);
        foreach (var go in scene.GetRootGameObjects())
        {
            if (!go || Chroniony(go.transform)) continue;
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
            {
                if (!c || !c.enabled || c.isTrigger || Chroniony(c.transform)) continue;
                var b = c.bounds; if (!b.Intersects(hala)) continue;
                if (c is MeshCollider mc && mc.sharedMesh && mc.sharedMesh.name.Contains("supermarket_hala_")) continue;   // już przycięta z siatką
                bool srodek = WHali(b.center) && b.max.y > y0 + .3f;
                bool prosta = c is BoxCollider && Quaternion.Angle(c.transform.rotation, Quaternion.identity) < .01f;
                if (prosta && !(b.size.x < 12f && b.size.z < 12f && srodek))
                {
                    Vector3 l = Vector3.Max(b.min, hala.min), h = Vector3.Min(b.max, hala.max);
                    if (h.x - l.x < .01f || h.y - l.y < .01f || h.z - l.z < .01f) continue;
                    void Dodaj(Vector3 a, Vector3 z)
                    {
                        var sz = z - a; if (sz.x < .01f || sz.y < .01f || sz.z < .01f) return;
                        var k = new GameObject(c.name + " (poza halą)"); k.transform.SetParent(reszta, false); k.transform.position = (a + z) * .5f;
                        k.AddComponent<BoxCollider>().size = sz;
                    }
                    Dodaj(b.min, new Vector3(l.x, b.max.y, b.max.z)); Dodaj(new Vector3(h.x, b.min.y, b.min.z), b.max);
                    Dodaj(new Vector3(l.x, b.min.y, b.min.z), new Vector3(h.x, l.y, b.max.z)); Dodaj(new Vector3(l.x, h.y, b.min.z), new Vector3(h.x, b.max.y, b.max.z));
                    Dodaj(new Vector3(l.x, l.y, b.min.z), new Vector3(h.x, h.y, l.z)); Dodaj(new Vector3(l.x, l.y, h.z), new Vector3(h.x, h.y, b.max.z));
                    c.enabled = false; kolPrzyciete++;
                }
                else if (srodek || (b.size.x < 15f && b.size.z < 15f && WHali(new Vector3(b.center.x, store.position.y, b.center.z)) && b.max.y > y0 + .3f))
                { c.enabled = false; kolUsuniete++; }
            }
        }
        // 3) kępy trawy rysowane instancjami (BraxClaudeTrawa) — dane kęp z hali usunięte
        foreach (var tr in UnityEngine.Object.FindObjectsByType<BraxClaudeTrawa>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!tr || !tr.dane || tr.dane.kepy == null) continue;
            var d = tr.dane; bool maY = d.y != null && d.y.Length == d.kepy.Length;
            var nk = new List<Vector4>(d.kepy.Length); var ny = new List<float>(d.kepy.Length);
            for (int i = 0; i < d.kepy.Length; i++)
            {
                var k = d.kepy[i];
                var q = store.InverseTransformPoint(new Vector3(k.x, store.position.y, k.y));
                if (Mathf.Abs(q.x) < hw + .3f && q.z > -.3f && q.z < D + .3f) { kepy++; continue; }
                nk.Add(k); if (maY) ny.Add(d.y[i]);
            }
            if (nk.Count == d.kepy.Length) continue;
            d.kepy = nk.ToArray(); if (maY) d.y = ny.ToArray(); EditorUtility.SetDirty(d);
        }
        Physics.SyncTransforms();
        Debug.Log("BRAXHORN_SUPERMARKET_HALA sklep=" + nr + " hala " + hala.min.ToString("F1") + "…" + hala.max.ToString("F1") + ": wycięte siatki otoczenia " + siatki +
                  ", kolizje przycięte " + kolPrzyciete + ", kolizje usunięte " + kolUsuniete + ", kępy trawy usunięte " + kepy);
    }

    // ---------------- miejsce ----------------
    static bool Szukaj(List<Vector3> zajete, out Vector3 c, out float yaw)
    {
        c = default; yaw = 0f;
        var kand = new List<Vector2>();
        for (float x = -480f; x <= 240f; x += 12f) for (float z = -420f; z <= 420f; z += 12f) kand.Add(new Vector2(x, z));
        foreach (var q in kand.OrderBy(v => v.sqrMagnitude))
        {
            if (zajete.Any(m => Vector2.Distance(new Vector2(m.x, m.z), q) < 150f)) continue;
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
                if (Pasuje(q, a, out float y)) { c = new Vector3(q.x, y, q.y); yaw = a; return true; }
        }
        return false;
    }

    /// <summary>Najwyższy poziom gruntu, na którym wolno postawić sklep (ulice miasta leżą na 0…0.3 m; dachy i tarasy wyżej).</summary>
    const float MaksGrunt = 4f;

    static bool Pasuje(Vector2 q, float yaw, out float y)
    {
        y = 0f;
        var rot = Quaternion.Euler(0f, yaw, 0f); Vector3 R = rot * Vector3.right, F = rot * Vector3.forward;
        Vector3 P(float x, float z) => new Vector3(q.x, 0f, q.y) + R * x + F * z;
        // 1) ląd pod całą halą z marginesem 4 m (najpierw najtańszy test); w granicach miasta, nie na stoku faweli
        for (float x = -W * .5f - 4f; x <= W * .5f + 4.01f; x += 4f)
            for (float z = -4f; z <= D + 4.01f; z += 4f)
            {
                var p = P(x, z);
                if (!BraxMiastoGranice.Lad(p.x, p.z) || !BraxMiastoGranice.Grywalny(p.x, p.z) || BraxMiastoGranice.WFaweli(p.x, p.z)) return false;
            }
        // 2) jezdnia przed frontem (6–16 m od witryny, na osi wejścia) — wejście od ulicy
        bool droga = false;
        for (float z = -6f; z >= -16f && !droga; z -= 2f) droga = BraxRegionMiastoB.NaJezdni(P(0f, z));
        if (!droga) return false;
        // 3) żadnej jezdni pod halą ani w pasie 4 m wokół
        for (float x = -W * .5f - 4f; x <= W * .5f + 4.01f; x += 4f)
            for (float z = -4f; z <= D + 4.01f; z += 4f) if (BraxRegionMiastoB.NaJezdni(P(x, z))) return false;
        // 4) nic nad gruntem (budynki, drzewa, latarnie, auta, ławki, płoty)
        // 9.11 23:47 (właściciel: „nadal to wisi nad historycznym budynkiem”; log APK 18:14: „kopia 2: front (-312, 43.6, -24)”):
        // Ground(−20…80) brał za grunt DACH zabytku KINNAIRD (kolizja bryły 43 m) i hala stanęła 43.6 m nad ulicą. Sklep stoi
        // WYŁĄCZNIE na poziomie ulicy: grunt szukany do MaksGrunt (4 m) — dach jest pomijany, a bryła budynku nad gruntem odrzuca
        // działkę w teście kolizji niżej.
        float yProbe = BraxRegion.Ground(q.x, q.y, -20f, MaksGrunt, out var h0) ? h0.point.y : 0f;
        // Nowy chodnik parkingu dochodzi pod same drzwi od strony z<0.
        // Nie odrzucaj sklepu z powodu WŁASNEGO dojścia tuż przed frontem.
        // Obejmuje całą halę i pas za nią; wnętrze nadal bez kolizji.
        var depth = D + 1.5f;
        var srodek = P(0f, (D - .03f + 1.5f) * .5f) +
            Vector3.up * (yProbe + .45f + H * .5f);
        if (Physics.OverlapBox(srodek,
            new Vector3(W * .5f + 1.0f, H * .5f, depth * .5f - .03f),
            rot, ~0, QueryTriggerInteraction.Ignore).Length > 0) return false;
        // 5) grunt płaski (rozrzut ≤ 0.6 m) — hala stoi na najwyższym punkcie
        float lo = float.MaxValue, hi = float.MinValue;
        for (int i = 0; i <= 6; i++)
            for (int j = 0; j <= 5; j++)
            {
                var p = P(-W * .5f + W * i / 6f, D * j / 5f);
                if (!BraxRegion.Ground(p.x, p.z, -20f, MaksGrunt, out var h)) return false;
                if (BraxRegion.Ground(p.x, p.z, MaksGrunt, 400f, out _)) return false;   // coś z płaskim wierzchem nad działką (dach, wiadukt, taras) — nie tutaj
                lo = Mathf.Min(lo, h.point.y); hi = Mathf.Max(hi, h.point.y);
            }
        if (hi - lo > .6f) return false;
        y = hi;
        return true;
    }

    // ---------------- budowa jednej kopii ----------------
        // 9.11: wejście sklepu #1 w osi alei (x lokalne 0 → parking i dojście prowadzą prosto do drzwi); pozostałe kopie jak dotąd.
    static float WejscieX(bool lustro, int nr) => nr == 1 ? 0f : (lustro ? -(W * .5f - 5f * K) : W * .5f - 5f * K);

static int Zbuduj(Transform t, bool lustro, int nr)
    {
        float L(float x) => lustro ? -x : x;
        float hw = W * .5f, xe = WejscieX(lustro, nr), wd = 2.4f * K, hd = 2.6f * K, yC = .5f * K, yS = 3.4f * K, yA = H + 1.4f * K;   // 9.11: sklep #1 — drzwi w osi alei
        Vector3 Swiat(float x, float y, float z) => t.TransformPoint(new Vector3(x, y, z));
        var rng = new System.Random(4421 * nr);

        var mPos = BraxNabrzeze.Mat("Supermarket: posadzka (gres jasny)", 1.0f, new Color(.95f, .95f, .93f), "floor_tiles_06");
        var mSc = BraxUliceKit.Mat("Supermarket: ściany (tynk biały)", new Color(.90f, .90f, .88f), .9f);
        var mFas = BraxUliceKit.Mat("Supermarket: elewacja (panel kompozytowy grafit)", new Color(.14f, .15f, .17f), .35f, .5f);
        var mRam = BraxUliceKit.Mat("Supermarket: ramy witryny (aluminium grafit)", new Color(.20f, .21f, .23f), .3f, .8f);
        var mSz = BraxUliceKit.SzkloPrzezroczyste;
        var mLamp = BraxMiastoA.Swieci("Supermarket: oprawy LED", new Color(.97f, .97f, .95f), new Color(1.5f, 1.5f, 1.45f));
        var mPas = BraxMiastoA.Swieci("Supermarket: podświetlenie szyldu (biel ciepła)", new Color(.98f, .95f, .88f), new Color(2.2f, 2.0f, 1.6f));
        var mDrew = BraxUliceKit.Mat("Supermarket: stoły warzyw (dąb)", new Color(.55f, .38f, .22f), .7f);

        var sP = new UlSiatka(); var sS = new UlSiatka(); var sF = new UlSiatka(); var sR = new UlSiatka(); var sG = new UlSiatka(); var sL = new UlSiatka(); var sPas = new UlSiatka(); var sT = new UlSiatka();
        var kol = new GameObject("Kolizje supermarketu " + nr).transform; kol.SetParent(t, false);
        void Kol(string n, Vector3 a, Vector3 b)
        {
            var g = new GameObject(n); g.transform.SetParent(kol, false);
            var bc = g.AddComponent<BoxCollider>(); bc.center = (a + b) * .5f; bc.size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        }

        // posadzka (wewnątrz ścian) + próg drzwi
        sP.Box(new Vector3(-hw + Grub, 0f, .25f), new Vector3(hw - Grub, Podloga, D - Grub), 1 | 2 | 4 | 16 | 32);
        sP.Box(new Vector3(xe - wd * .5f, 0f, 0f), new Vector3(xe + wd * .5f, Podloga, .25f), 1 | 2 | 4 | 32);
        Kol("posadzka", new Vector3(-hw + Grub, -.3f, 0f), new Vector3(hw - Grub, Podloga, D - Grub));
        // ściany: tył (pełna szerokość), boki (do tylnej ściany) — bez nakładania równoległych lic
        sS.Box(new Vector3(-hw, 0f, D - Grub), new Vector3(hw, H, D));
        sS.Box(new Vector3(-hw, 0f, 0f), new Vector3(-hw + Grub, H, D - Grub));
        sS.Box(new Vector3(hw - Grub, 0f, 0f), new Vector3(hw, H, D - Grub));
        Kol("ściana tylna", new Vector3(-hw, 0f, D - Grub), new Vector3(hw, H, D));
        Kol("ściana lewa", new Vector3(-hw, 0f, 0f), new Vector3(-hw + Grub, H, D - Grub));
        Kol("ściana prawa", new Vector3(hw - Grub, 0f, 0f), new Vector3(hw, H, D - Grub));
        // stropodach (od tyłu frontu), attyka boków i tyłu
        sF.Box(new Vector3(-hw, H, .25f), new Vector3(hw, H + .35f, D));
        sF.Box(new Vector3(-hw, H + .35f, .25f), new Vector3(-hw + Grub, yA, D));
        sF.Box(new Vector3(hw - Grub, H + .35f, .25f), new Vector3(hw, yA, D));
        sF.Box(new Vector3(-hw + Grub, H + .35f, D - Grub), new Vector3(hw - Grub, yA, D));
        // front: cokół + szyba + szprosy w dwóch odcinkach obok drzwi, nadświetle nad drzwiami, panel elewacji z szyldem
        foreach (var (a, b) in new[] { (Mathf.Min(-hw + Grub, xe - wd * .5f), Mathf.Max(-hw + Grub, xe - wd * .5f)), (Mathf.Min(xe + wd * .5f, hw - Grub), Mathf.Max(xe + wd * .5f, hw - Grub)) })
        {
            if (b - a < .3f) continue;
            sF.Box(new Vector3(a, 0f, 0f), new Vector3(b, yC, .25f));
            sG.Box(new Vector3(a, yC, .10f), new Vector3(b, yS, .12f));
            int n = Mathf.Max(1, Mathf.RoundToInt((b - a) / (2.2f * K)));
            for (int k = 0; k <= n; k++) { float x = Mathf.Lerp(a + .05f, b - .05f, k / (float)n); sR.Box(new Vector3(x - .05f, yC, .05f), new Vector3(x + .05f, yS, .17f)); }
            sR.Box(new Vector3(a, yC, .05f), new Vector3(b, yC + .06f, .17f));
            Kol("witryna", new Vector3(a, 0f, 0f), new Vector3(b, yS, .25f));
        }
        sG.Box(new Vector3(xe - wd * .5f, hd + .1f, .10f), new Vector3(xe + wd * .5f, yS, .12f));
        sR.Box(new Vector3(xe - wd * .5f, hd, .05f), new Vector3(xe + wd * .5f, hd + .1f, .17f));
        Kol("nad drzwiami", new Vector3(xe - wd * .5f, hd, 0f), new Vector3(xe + wd * .5f, yS, .25f));
        sF.Box(new Vector3(-hw + Grub, yS, 0f), new Vector3(hw - Grub, yA, .25f), 32);
        sF.Box(new Vector3(-hw, H + .35f, 0f), new Vector3(-hw + Grub, yA, .25f), 32 | 2);
        sF.Box(new Vector3(hw - Grub, H + .35f, 0f), new Vector3(hw, yA, .25f), 32 | 1);
        Kol("elewacja nad witryną", new Vector3(-hw, yS, 0f), new Vector3(hw, yA, .25f));
        // SZYLD PREMIUM: podświetlana listwa pod napisem i nad nim (biel ciepła), napis nad wejściem
        float sx0 = -hw + 2.5f * K, sx1 = hw - 2.5f * K, ys = (yS + yA) * .5f;
        sPas.Box(new Vector3(sx0, yS + .18f * K, -.06f), new Vector3(sx1, yS + .24f * K, 0f), 32);
        sPas.Box(new Vector3(sx0, yA - .24f * K, -.06f), new Vector3(sx1, yA - .18f * K, 0f), 32);
        // szyld-kaseton z grafiki właściciela (3.11 14:33, paczka miasto_sklepy/szyld_supermarket: 6 m × K, front +z → na zewnątrz)
        var szyld = BraxMiastoAssety.Model("miasto_sklepy", "szyld_supermarket");
        // 3.11 16:20 (właściciel: trzy firmy — BRAXHORN, WAWER, SOBCZAK): drugi supermarket = WAWER SUPERMARKET (grafika z arkusza)
        if (nr == 2 && BraxMiastoBillboardy.Mural(t, "wawer_supermarket", Swiat(0f, ys - .58f * K + .6f * K, -.12f), -t.forward, 6f * K, 1.2f * K)) { }
        else if (szyld) BraxMiastoAssety.Postaw(t, szyld, "Szyld: SUPERMARKET BRAXHORN (kaseton)", Swiat(0f, ys - .58f * K, -.1f), t.eulerAngles.y + 180f, 1f, true, false);
        else BraxRegionMeble.Napis(t, "SUPERMARKET BRAXHORN", Swiat(0f, ys - .35f * K, -.08f), -t.forward, .95f * K, new Color(.98f, .96f, .90f));
        // oprawy LED pod sufitem (siatka)
        for (float x = -hw + 2.5f * K; x < hw - 2f * K; x += 3.5f * K)
            for (float z = 2.5f * K; z < D - 1.5f * K; z += 3.5f * K) sL.Box(new Vector3(x - .6f * K, H - .04f, z - .15f * K), new Vector3(x + .6f * K, H, z + .15f * K), 8);

        void Obj(string n, UlSiatka s, Material m, bool cien)
        {
            if (s.Wierzcholki == 0) return;
            var g = BraxMiastoA.Obiekt(t, "Supermarket " + nr + ": " + n, s, m, cien);
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | (cien ? StaticEditorFlags.OccluderStatic : 0));
        }
        // ---- wyposażenie (modele) ----
        int towar = 0;
        var wyp = new GameObject("Wyposażenie supermarketu " + nr).transform; wyp.SetParent(t, false);
        float yaw0 = t.eulerAngles.y;
        // regały-wyspy: 9 alejek, długą osią w głąb hali
        var regal = BraxMiastoAssety.Model("miasto_sklepy", "regal_sklep");
        for (int i = 0; regal && i < 9; i++)
        {
            float x = L(-12f * K + i * 3f * K), zc = 12.8f * K;
            var o = BraxMiastoAssety.Postaw(wyp, regal, "Regał-wyspa " + (i + 1), Swiat(x, Podloga, zc), yaw0, 1f, true, true);
            if (!o) continue;
            BraxMiastoAssety.LodyZWezlow(o, new[] { .22f, .05f });
            BraxMiastoAssety.Kolizja(kol, o, "kolizja: regał-wyspa " + (i + 1), .03f);
            var tw = BraxSklepyTowar.Korzen(wyp, "supermarket " + nr + " regał " + (i + 1));
            foreach (var sg in new[] { -1f, 1f })
                foreach (var s in Segmenty)
                    for (int k = 0; k < s.y.Length; k++)
                    {
                        float wolne = (k + 1 < s.y.Length ? s.y[k + 1] - s.y[k] - .03f : .45f) * K;
                        var start = Swiat(x + sg * .22f * K, Podloga + s.y[k] * K + .004f, zc + (s.z0 + .03f) * K);
                        BraxSklepyTowar.Polka(BraxSklepyTowar.Ma("sm_g" + i) ? "sm_g" + i : s.typ, tw, start, t.forward, -sg * t.right, (s.z1 - s.z0 - .06f) * K, rng, wolne);
                    }
            BraxSklepyTowar.Zakoncz(tw, "supermarket " + nr + " regał " + (i + 1));
            towar += tw.GetComponentsInChildren<Renderer>(true).Length;
        }
        // lodówki pod tylną ścianą (front modelu = −z → w stronę sklepu)
        // REGAŁ CHŁODNICZY (otwarty, wielopółkowy) przy tylnej ścianie: mięso, wędliny, sery, ryby (właściciel 13:56); reszta ściany — lodówki
        float zw = D - Grub, cA0 = -hw + Grub + .4f, cB0 = cA0 + 9f * K, cA = lustro ? -cB0 : cA0, cB = lustro ? -cA0 : cB0;
        {
            var sCh = new UlSiatka(); float dC = .95f * K, y1 = Podloga + .45f * K, y2 = Podloga + 1.0f * K, y3 = Podloga + 1.45f * K, yK = Podloga + 2.05f * K;
            sCh.Box(new Vector3(cA, Podloga, zw - dC), new Vector3(cB, y1, zw), 32);                                   // podstawa z wanną ekspozycji
            sCh.Box(new Vector3(cA, y1, zw - .08f * K), new Vector3(cB, yK, zw), 32);                                  // plecy
            sCh.Box(new Vector3(cA + .02f, y2 - .03f, zw - .08f * K - .55f * K), new Vector3(cB - .02f, y2, zw - .08f * K));   // półka 2
            sCh.Box(new Vector3(cA + .02f, y3 - .03f, zw - .08f * K - .42f * K), new Vector3(cB - .02f, y3, zw - .08f * K));   // półka 3
            sCh.Box(new Vector3(cA, yK, zw - .6f * K), new Vector3(cB, yK + .15f * K, zw), 32);                         // daszek
            sCh.Box(new Vector3(cA - .04f, Podloga, zw - dC), new Vector3(cA, yK + .15f * K, zw), 32);                  // boki
            sCh.Box(new Vector3(cB, Podloga, zw - dC), new Vector3(cB + .04f, yK + .15f * K, zw), 32);
            sL.Box(new Vector3(cA + .05f, yK - .03f, zw - .58f * K), new Vector3(cB - .05f, yK, zw - .5f * K), 8);    // listwa LED pod daszkiem
            Obj("regał chłodniczy (mięso, wędliny, sery)", sCh, BraxUliceKit.Mat("Supermarket: regał chłodniczy (blacha biała)", new Color(.93f, .94f, .95f), .35f, .3f), true);
            Kol("regał chłodniczy", new Vector3(cA - .04f, Podloga, zw - dC), new Vector3(cB + .04f, yK + .15f * K, zw));
            var twC = BraxSklepyTowar.Korzen(wyp, "supermarket " + nr + " chłodnia");
            BraxSklepyTowar.Polka("ch_dol", twC, Swiat(cA + .06f, y1 + .004f, zw - dC * .5f), t.right, t.forward, cB - cA - .12f, rng, .5f * K);
            BraxSklepyTowar.Polka("ch_srodek", twC, Swiat(cA + .06f, y2 + .004f, zw - .08f * K - .275f * K), t.right, t.forward, cB - cA - .12f, rng, .4f * K);
            BraxSklepyTowar.Polka("ch_gora", twC, Swiat(cA + .06f, y3 + .004f, zw - .08f * K - .21f * K), t.right, t.forward, cB - cA - .12f, rng, .5f * K);
            BraxSklepyTowar.Zakoncz(twC, "supermarket " + nr + " chłodnia");
            towar += twC.GetComponentsInChildren<Renderer>(true).Length;
            BraxRegionMeble.Napis(t, "MIĘSO · WĘDLINY · SERY", Swiat((cA + cB) * .5f, yK + .7f * K, zw - .02f), -t.forward, .32f * K, new Color(.55f, .12f, .10f));
        }
        var lod = BraxMiastoAssety.Model("miasto_sklepy", "lodowka");
        if (lod)
        {
            float sz = .854f * K + .02f, zl = D - Grub - .944f * K * .5f - .03f; int n = 0;
            for (float x = -hw + Grub + .4f + sz * .5f; x < hw - Grub - .4f - sz * .5f; x += sz)
            {
                if (x + sz * .5f > cA - .1f && x - sz * .5f < cB + .1f) continue;
                BraxMiastoAssety.Postaw(wyp, lod, "Lodówka " + (++n), Swiat(x, Podloga, zl), yaw0, 1f, true, true);
            }
            float kA = lustro ? -hw + Grub + .4f : cB + .1f, kB = lustro ? cA - .1f : hw - Grub - .4f;
            Kol("lodówki", new Vector3(kA, Podloga, zl - .944f * K * .5f), new Vector3(kB, Podloga + 2.184f * K, zl + .944f * K * .5f));
        }
        // kasy (3 podwójne) między wejściem a alejkami; w kopii lustrzanej obrócone
        var kasa = BraxMiastoAssety.Model("miasto_sklepy", "kasa");
        for (int i = 0; kasa && i < 3; i++)
        {
            var o = BraxMiastoAssety.Postaw(wyp, kasa, "Kasa " + (i + 1), Swiat(L(-10f * K + i * 4.4f * K), Podloga, 3f * K + 3.969f * K * .5f), yaw0 + (lustro ? 180f : 0f), 1f, true, true);
            if (o) { BraxMiastoAssety.LodyZWezlow(o, new[] { .2f, .05f }); BraxMiastoAssety.Kolizja(kol, o, "kolizja: kasa " + (i + 1), .1f); }
        }
        // stoisko warzyw i owoców: dwa dębowe stoły (blat + 4 nogi) ze skrzynkami
        var twW = BraxSklepyTowar.Korzen(wyp, "supermarket " + nr + " warzywa");
        foreach (float xc in new[] { L(4.6f * K), L(8.4f * K) })
        {
            float a = 1.4f * K, g = .55f * K, h = .8f * K, zc = 6.6f * K;
            sT.Box(new Vector3(xc - a, h - .05f, zc - g), new Vector3(xc + a, h, zc + g));
            foreach (var (dx, dz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                sT.Box(new Vector3(xc + dx * (a - .1f) - .03f, Podloga, zc + dz * (g - .1f) - .03f), new Vector3(xc + dx * (a - .1f) + .03f, h - .05f, zc + dz * (g - .1f) + .03f));
            Kol("stół warzyw", new Vector3(xc - a, Podloga, zc - g), new Vector3(xc + a, h, zc + g));
            BraxSklepyTowar.Polka("warzywniak", twW, Swiat(xc - a + .05f, h + .004f, zc), t.right, t.forward, 2f * a - .1f, rng);
        }
        BraxSklepyTowar.Zakoncz(twW, "supermarket " + nr + " warzywa");
        towar += twW.GetComponentsInChildren<Renderer>(true).Length;
        // wózki: w środku przy wejściu i parę sztuk przed wejściem (jak w prawdziwym sklepie)
        Vector3 doSciany = lustro ? -t.right : t.right;
        BraxSklepyTowar.Wozki(wyp, kol, Swiat(xe + (lustro ? -1f : 1f) * (wd * .5f + .5f), Podloga, 1.4f * K), doSciany, 4);
        var przed = Swiat(xe - (lustro ? -1f : 1f) * (wd * .5f + 1.2f * K), 0f, -1.5f * K);
        if (BraxRegion.Ground(przed.x, przed.z, przed.y - 2f, przed.y + .5f, out var gPrzed)) przed.y = gPrzed.point.y;   // chodnik przed halą (do 0.6 m niżej)
        BraxSklepyTowar.Wozki(wyp, kol, przed, -doSciany, 4);

        // plakaty promocji z grafik właściciela (3.11 14:37) na ścianie bocznej od strony kas, w ramach 0,6 × 0,9 m × K
        for (int i = 0; i < 3; i++)
        {
            float xw = L(-hw + Grub + .02f * K * .5f + .005f);
            // 3.11 16:27 (właściciel): w lokalu WAWER plakaty WAWER (drugi supermarket), towar mieszany
            if (nr == 2)
            {
                var nPl = Quaternion.Euler(0f, yaw0 + (lustro ? -90f : 90f), 0f) * Vector3.forward;
                if (BraxMiastoBillboardy.Mural(wyp, "wawer_plakat_" + (i % 2 + 1), Swiat(xw, Podloga + 1.7f * K, (3.5f + i * 2.6f) * K) + nPl * .02f, nPl, .6f * K, .9f * K)) continue;
            }
            var pl = BraxMiastoAssety.Model("miasto_sklepy", "plakat_0" + (i + 1)); if (!pl) break;
            BraxMiastoAssety.Postaw(wyp, pl, "Plakat promocji " + (i + 1), Swiat(xw, Podloga + 1.25f * K, (3.5f + i * 2.6f) * K), yaw0 + (lustro ? -90f : 90f), 1f, true, false);
        }
        // siatki do sceny
        Obj("posadzka", sP, mPos, false); Obj("ściany", sS, mSc, true); Obj("elewacja i dach", sF, mFas, true); Obj("ramy witryny", sR, mRam, true);
        Obj("szyby witryny", sG, mSz, false); Obj("oprawy LED", sL, mLamp, false); Obj("podświetlenie szyldu", sPas, mPas, false); Obj("stoły warzyw", sT, mDrew, true);
        BraxMiastoA.DrzwiPrzesuwne(t, "SUPERMARKET BRAXHORN " + nr, Swiat(xe, Podloga, .11f), t.right, wd, hd, mSz, mRam);
        BraxUliceKit.Punkt("supermarket_" + nr + "_wejscie", "przed wejściem: SUPERMARKET BRAXHORN " + nr + " — drzwi automatyczne, wózki", Swiat(xe, 0f, -2.5f * K));
        BraxUliceKit.Punkt("supermarket_" + nr + "_srodek", "w środku: SUPERMARKET BRAXHORN " + nr + " — alejki, kasy, lodówki", Swiat(L(-1.5f * K), Podloga, 9.5f * K));
        return towar;
    }
}
