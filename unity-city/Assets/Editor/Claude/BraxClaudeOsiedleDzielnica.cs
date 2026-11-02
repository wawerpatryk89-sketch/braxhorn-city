using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// DZIELNICA ZACHODNIA MIASTA (2.11, właściciel w mieście 13.1: „całe miasto za małe — musi być powiększone”, „musi być posterunek
// policji, komisariat, mały szpital, różne rzeczy”, „na osiedlu mamy w chuj większy teren zagospodarowany niż tutaj”; 11:00 „w mieście
// dużo wysokich wieżowców”, 11:01 kierunek: śródmieście jak Manhattan). Plan z prostokątami: scratchpad/plan/DZIELNICA_PLAN.md.
//
// Teren: x −290.5 … −104, z −69.5 … 113.5 (na zachód od końca ulicy głównej x −104; galeria na wschód od ulicy S1, x ≥ −112).
// Siatka ulic: Główna (oś z 28.5, przedłużenie ulicy z miasta), S1 x −121, S2 x −201, S3 x −281, P z −60, N z 104.
//
// Budowa tymi samymi narzędziami co osiedle (partial BraxClaudeOsiedle: siatki łączone per grupa i materiał, kolizje BoxCollider,
// litery przestrzenne, biblioteka wnętrz usług 175339c — łazienki, meble szpitala), ale:
//  • korzeń sceny „CLAUDE dzielnica zachodnia” (osobny obszar doczytywany BraxhornCity_Dzielnica — BraxGymBuild),
//  • siatki zapisywane w Assets/PlayableGenerated/ClaudeDzielnica (DzlFlush), a materiały na końcu przepisane na shader miasta
//    Braxhorn/CityStaticPBR (to samo słońce i otoczenie co reszta miasta; wnętrza bez słońca: _SunIntensity 0) i zapisane w tym
//    samym folderze — nic w scenie miasta nie odwołuje się do plików osiedla (build „oba” nadpisuje folder osiedla),
//  • skala Braxa: wszystko × k = 1.294 (K), także meble biblioteki (Ku = K).
// ZASADA 11:00 („ściągasz, kładziesz nową, a nie na to”): wszystko stare na terenie dzielnicy (grunt-lustro, płot końca ulicy,
// trawa, stare bryły i kolizje źródła) jest wycinane/usuwane PRZED budową — lista w logu BRAXHORN_DZIELNICA usunięte.
public static partial class BraxClaudeOsiedle
{
    public const string DzlKorzen = "CLAUDE dzielnica zachodnia";
    public const string DzlScena = "BraxhornCity_Dzielnica";
    public const string DzlZDalekaNazwa = "BRAXHORN dzielnica — bryły z daleka";
    const string DzlOut = "Assets/PlayableGenerated/ClaudeDzielnica";

    // ---------------- układ (prawdziwe współrzędne sceny) ----------------
    public static readonly Rect DzlTeren = Rect.MinMaxRect(-290.5f, -69.5f, -104f, 113.5f);
    const float DzlGaleriaX = -112f;                         // zachodnia ściana galerii (wykonawca GALERIA) — na północ od ulicy głównej
    const float GZ = 28.5f, GJ0 = 24f, GJ1 = 33f;            // ulica główna: oś, jezdnia (jak w mieście: asfalt y .003, krawężniki z 23.875 / 33.125)
    const float GCh0 = 17f, GCh1 = 40f;                       // lica pierzei dzielnicy (chodniki 7 m)
    const float GKoniecX = -285.5f;                           // zachodni koniec ulicy głównej (skrzyżowanie z S3)
    static readonly float[] DzlSx = { -121f, -201f, -281f };   // osie ulic N-S
    const float DzlPz = -60f, DzlNz = 104f;                   // osie ulic E-W (południowa P, północna N)
    const float DzlPolJezdni = 4.5f, DzlChodnik = 5f;         // pół jezdni (2 × 3,5 m × k ≈ 9 m), chodnik ulic bocznych
    const float DzlYJ = .003f, DzlYC = .15f;                  // wierzch asfaltu / chodnika (jak ulica miasta)
    const float DzlZ0 = -64.5f, DzlZ1 = 108.5f;               // zasięg jezdni N-S (od krawędzi P do krawędzi N)

    static BraxCityWorld dzlWorld;
    static int dzlUsuniete, dzlWyciete, dzlKolizjeWyciete;
    static readonly List<string> dzlUsunieteLista = new List<string>();
    static readonly List<string> dzlRaport = new List<string>();
    static readonly HashSet<string> dzlWnetrza = new HashSet<string>();      // grupy wnętrz (materiały bez słońca)
    static readonly List<(Rect r, float h, int styl)> dzlBryly = new List<(Rect, float, int)>();   // bryły do LOD z daleka
    static readonly List<Vector3> dzlSkrzyzowania = new List<Vector3>();

    static void DzlNota(string t) { dzlRaport.Add(t); Debug.Log("BRAXHORN_DZIELNICA " + t); }
    static void DzlGuard(string co, Action a)
    {
        var g = group;
        var t0 = DateTime.UtcNow;
        try { a(); DzlNota("krok OK: " + co + " (" + (DateTime.UtcNow - t0).TotalSeconds.ToString("0.0") + " s)"); }
        catch (Exception e) { DzlNota("DO POPRAWKI: krok " + co + ": " + e.Message); Debug.LogError("BRAXHORN_DZIELNICA krok " + co + ": " + e); }
        group = g;
    }

    // ---------------- wejście (BraxRegion.Apply — krok „dzielnica zachodnia”) ----------------
    public static void DzielnicaZachodnia(BraxCityWorld world)
    {
        var repPrev = rep; var rootPrev = root; var colPrev = colliders; var groupPrev = group;
        try
        {
            Directory.CreateDirectory(DzlOut); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            rep = new Report(); mats.Clear(); tile.Clear(); batches.Clear(); groups.Clear(); group = "Dzielnica";
            hardCells.Clear(); hardRound.Clear(); pavedCells.Clear(); pavingSet = null; grassMat = null; spodyStropow.Clear();
            treeSpots.Clear(); trees.Clear(); treeTried = false; treeSrc = null; WyczyscWspolne14(); WyczyscElewacje();
            uMisjeLista.Clear(); uLozkaZPacjentem.Clear(); uSiedziska.Clear(); uSiedziskaWozek.Clear(); uRzeczy = uMisje = uSondy = 0;
            dzlUsuniete = dzlWyciete = dzlKolizjeWyciete = 0; dzlUsunieteLista.Clear(); dzlRaport.Clear(); dzlWnetrza.Clear(); dzlBryly.Clear(); dzlSkrzyzowania.Clear();
            dzlFlushN = 0; dzlMatyMiasta.Clear(); dzlMatyWnetrz.Clear(); dzlObsada.Clear(); dzlDrzwi.Clear(); dzlDrzwiN = 0; dzlZadania.Clear(); dzlWjazdy.Clear(); dzlMatN = 0;
            dzlWjazdy.Add(Rect.MinMaxRect(KomX1 - .5f, -30.5f, DzlSx[0] - DzlPolJezdni + .3f, -23.5f));   // wjazd na parking komisariatu z S1
            ZmierzBohatera();
            dzlWorld = world;
            var stary = GameObject.Find(DzlKorzen); if (stary) UnityEngine.Object.DestroyImmediate(stary);
            root = new GameObject(DzlKorzen).transform; root.position = Vector3.zero;
            colliders = new GameObject("kolizje").transform; colliders.SetParent(root, false);
            DzlNota("start: teren " + DzlTeren.xMin + "…" + DzlTeren.xMax + " × " + DzlTeren.yMin + "…" + DzlTeren.yMax + ", k = " + K.ToString("0.000"));

            DzlGuard("usunięcie starego terenu (zasada 11:00)", DzlUsunStare);
            DzlGuard("grunt, jezdnie, chodniki, krawężniki, przejścia", DzlUlice);
            DzlGuard("wyposażenie ulic (latarnie, drzewa w kratach, kosze, kratki odpływowe)", DzlWyposazenieUlic);
            DzlGuard("komisariat policji (bryła, elewacja, parking, szlaban)", DzlKomisariat);
            DzlGuard("komisariat — wnętrza (parter i piętro)", DzlKomisariatWnetrza);

            DzlGuard("miejsca AKCJI pod misje i miejsca obsady", DzlMiejscaZadan);
            DzlFlush();
            DzlGuard("drzwi i szlaban: skrzydła na zawiasach", DzlPrzypnijDrzwi);
            DzlGuard("materiały miasta (CityStaticPBR, wnętrza bez słońca)", DzlMaterialyMiasta);
            DzlGuard("bryły z daleka (scena główna)", DzlZDaleka);
            DzlGuard("punkty podróży", DzlPunkty);
            AssetDatabase.SaveAssets();
            DzlNota("koniec: siatki " + rep.meshes + ", trójkąty " + rep.triangles + ", usunięte stare obiekty " + dzlUsuniete + ", wycięte siatki " + dzlWyciete +
                    ", wycięte kolizje " + dzlKolizjeWyciete + ", rzeczy w rejestrze wyposażenia " + uRzeczy + ", miejsca zadań " + uMisje);
            File.WriteAllText(DzlOut + "/dzielnica.json", "{\n  \"notatki\": [\n" + string.Join(",\n", dzlRaport.Concat(rep.notes).Select(s => "    \"" + s.Replace("\\", "/").Replace("\"", "'") + "\"")) + "\n  ]\n}\n");
        }
        finally
        {
            rep = repPrev; root = rootPrev; colliders = colPrev; group = groupPrev;
            mats.Clear(); tile.Clear(); batches.Clear(); groups.Clear(); pavingSet = null; grassMat = null; treeTried = false; treeSrc = null;
        }
    }

    // ---------------- 1) usunięcie starego (zasada właściciela 11:00) ----------------
    // Prostokąt dzielnicy bez pasa galerii (x ≥ −112 na północ od z 40) i bez istniejących pierzei (x ≥ −104).
    static IEnumerable<Bounds> DzlObjetosciCiecia()
    {
        // 30 cm zapasu od ściany galerii (x −112) i od lica pierzei miasta (x −104): ściana leżąca dokładnie na granicy cięcia
        // byłaby uznana za „w środku” i wycięta (BraxCityMeshCut: odległość 0 = wewnątrz)
        yield return MinMax(-292f, DzlGaleriaX - .3f, -3f, 120f, -71f, 115f);     // cały zachód (od galerii w stronę horyzontu)
        yield return MinMax(DzlGaleriaX - .3f, -104.3f, -3f, 120f, -71f, 40f);    // pas między galerią a pierzejami miasta (ulica i skrzyżowanie)
    }
    static readonly Type[] DzlZostaja = { typeof(BraxGymPlayer), typeof(BraxCityWorld), typeof(Camera), typeof(BraxObszary), typeof(BraxCityElevator), typeof(BraxVessel), typeof(BraxNutritionCounter), typeof(BraxCityDoor) };
    static void DzlUsunStare()
    {
        var scena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var korzenie = scena.GetRootGameObjects().Where(g => g.transform != root).ToList();
        bool Chroniony(Transform t)
        {
            for (var p = t; p; p = p.parent)
            {
                if (p.name.StartsWith("braxhorn-original-gym", StringComparison.Ordinal) || p.name.StartsWith(DzlKorzen, StringComparison.Ordinal)) return true;
                foreach (var z in DzlZostaja) if (p.GetComponent(z)) return true;
                var l = p.GetComponent<Light>(); if (l && l.type == LightType.Directional) return true;
            }
            return false;
        }
        // płot końca ulicy (x −104.4…−104, z 19.6…37.1) — cały obiekt; od x −104 na wschód zaczynają się budynki pierzei
        var plot = MinMax(-105.2f, -103.85f, -1f, 4.5f, 19f, 38f);
        foreach (var g in korzenie)
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true).ToList())
            {
                if (!r || Chroniony(r.transform)) continue;
                var b = r.bounds;
                if (plot.Contains(b.min) && plot.Contains(b.max) && b.size.x < 1.2f)
                { dzlUsunieteLista.Add(DzlSciezka(r.transform) + " (płot końca ulicy)"); UnityEngine.Object.DestroyImmediate(r.gameObject); dzlUsuniete++; }
            }
        foreach (var v in DzlObjetosciCiecia())
        {
            foreach (var g in korzenie)
            {
                if (!g || Chroniony(g.transform)) continue;
                // całe obiekty w środku: precz (z kolizjami); nachodzące (grunt-lustro, trawa, las): wycięte trójkąty w objętości
                foreach (var r in g.GetComponentsInChildren<Renderer>(true).ToList())
                {
                    if (!r || Chroniony(r.transform) || r is SkinnedMeshRenderer) continue;
                    var b = r.bounds; if (!b.Intersects(v)) continue;
                    if (v.Contains(b.min) && v.Contains(b.max))
                    {
                        dzlUsunieteLista.Add(DzlSciezka(r.transform));
                        var go = r.gameObject;
                        if (go.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(go);
                        else { var mf = go.GetComponent<MeshFilter>(); UnityEngine.Object.DestroyImmediate(r); if (mf) UnityEngine.Object.DestroyImmediate(mf); }
                        dzlUsuniete++;
                    }
                }
                if (!g) continue;
                int n = BraxCityMeshCut.Subtract(g.transform, v, DzlOut + "/Ciecia", "dzielnica");
                if (n > 0) { dzlWyciete += n; dzlUsunieteLista.Add(g.name + ": wycięte trójkąty w " + n + " siatkach (grunt / trawa / las w prostokącie dzielnicy)"); }
                foreach (var c in g.GetComponentsInChildren<Collider>(true).ToList())
                {
                    if (!c || !c.enabled || c.isTrigger || Chroniony(c.transform)) continue;
                    var b = c.bounds; if (!b.Intersects(v)) continue;
                    if (v.Contains(b.min) && v.Contains(b.max)) { c.enabled = false; dzlKolizjeWyciete++; continue; }
                    if (c is BoxCollider && Quaternion.Angle(c.transform.rotation, Quaternion.identity) < .01f)
                    { try { dzlKolizjeWyciete += BraxCityMeshCut.SubtractBoxColliders(c.transform, v); } catch (Exception e) { rep.notes.Add("kolizja " + c.name + ": " + e.Message); } }
                    else if (c is MeshCollider mc && mc.sharedMesh && b.size.x > 40f) { /* siatka gruntu: wycięta wyżej razem z rendererem (MeshCollider dostaje nową siatkę) */ }
                    else if (v.Contains(b.center)) { c.enabled = false; dzlKolizjeWyciete++; }
                }
            }
        }
        foreach (var s in dzlUsunieteLista.Take(60)) Debug.Log("BRAXHORN_DZIELNICA usunięte: " + s);
        DzlNota("usunięte stare: obiekty " + dzlUsuniete + ", siatki wycięte " + dzlWyciete + ", kolizje " + dzlKolizjeWyciete + (dzlUsunieteLista.Count > 60 ? " (lista w logu skrócona do 60)" : ""));
    }
    static string DzlSciezka(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 120; p = p.parent) s = p.name + "/" + s; return s; }

    // ---------------- materiały dzielnicy (Standard → na końcu CityStaticPBR) ----------------
    static Material DzlAsfalt => Pbr("dzl asfalt jezdni", "asphalt_04", 3.5f, new Color(.40f, .40f, .41f), .9f);
    static Material DzlPlyty => Pbr("dzl chodnik płyty betonowe duże", "concrete_pavement_02", 2.6f, new Color(.86f, .86f, .85f));
    static Material DzlPlytyCiemne => Pbr("dzl chodnik płyty ciemniejsze", "concrete_pavement_02", 2.6f, new Color(.66f, .66f, .66f));
    static Material DzlKrawez => Pbr("dzl krawężnik granit", "concrete_pavement_02", 1.2f, new Color(.70f, .70f, .71f));
    static Material DzlFarba => Plain("dzl farba drogowa biała", new Color(.90f, .90f, .87f), .25f);
    static Material DzlZeliwo => Plain("dzl żeliwo (kratki, włazy)", new Color(.10f, .10f, .105f), .35f, .6f);
    static Material DzlGrafit => Plain("dzl stal grafit malowana", new Color(.13f, .135f, .14f), .45f, .55f);
    static Material DzlBeton => Pbr("dzl beton architektoniczny", "concrete_pavement_02", 2.8f, new Color(.74f, .74f, .73f));

    // ---------------- 2) ulice ----------------
    // Jezdnie z asfaltu, chodniki z dużych płyt (bryła 0…0.15 m z kolizją), krawężniki granitowe 20 cm (lico od jezdni), na
    // przejściach obniżone; zebry; linie środkowe i stopu. Skrzyżowania: kwadrat asfaltu. Podłoga fizyczna pod całą dzielnicą.
    static readonly List<Rect> dzlJezdnie = new List<Rect>();
    static void DzlUlice()
    {
        group = "Dzielnica / ulice / nawierzchnie"; bezCieni.Add(group);
        // podłoga fizyczna (poziom jezdni) pod całym terenem
        var fl = new GameObject("podłoga dzielnicy"); fl.transform.SetParent(colliders, false);
        fl.transform.position = new Vector3(DzlTeren.center.x, DzlYJ - .25f, DzlTeren.center.y);
        fl.AddComponent<BoxCollider>().size = new Vector3(DzlTeren.width + 4f, .5f, DzlTeren.height + 4f);
        dzlJezdnie.Clear();
        // jezdnie: główna (od x −104 do S3), N-S, P, N
        dzlJezdnie.Add(Rect.MinMaxRect(GKoniecX, GJ0, -104f, GJ1));
        foreach (float sx in DzlSx) dzlJezdnie.Add(Rect.MinMaxRect(sx - DzlPolJezdni, DzlZ0, sx + DzlPolJezdni, DzlZ1));
        foreach (float sz in new[] { DzlPz, DzlNz }) dzlJezdnie.Add(Rect.MinMaxRect(DzlSx[2] - DzlPolJezdni, sz - DzlPolJezdni, DzlSx[0] + DzlPolJezdni, sz + DzlPolJezdni));
        foreach (var j in dzlJezdnie) for (float x = j.xMin; x < j.xMax - .01f; x += 30f) for (float z = j.yMin; z < j.yMax - .01f; z += 30f)
                    Top(DzlAsfalt, x, Mathf.Min(x + 30f, j.xMax), z, Mathf.Min(z + 30f, j.yMax), DzlYJ);
        foreach (float sx in DzlSx) foreach (float sz in new[] { DzlPz, GZ, DzlNz }) dzlSkrzyzowania.Add(new Vector3(sx, 0, sz));
        // skrzyżowania do wspólnego API (ruch / sygnalizacja — wykonawca „ruch”): główna × S1 i × S2 ze światłami
        foreach (var c in dzlSkrzyzowania)
            SkrzyzowaniaInfo.Add(new SkrzyzowanieInfo { srodek = c, promien = DzlPolJezdni + 1f, rodzaj = (Mathf.Abs(c.z - GZ) < .1f && c.x > -260f) ? RodzajSkrzyzowania.ZeSwiatlami : RodzajSkrzyzowania.Zwykle });

        // chodniki: każdy kwartał otoczony chodnikiem (od krawędzi jezdni na zewnątrz), główna 7 m, boczne 5 m
        group = "Dzielnica / ulice / chodniki";
        var kwartaly = DzlKwartaly();
        foreach (var q in kwartaly) DzlChodnikWokol(q.chodnik, q.lico);
        // chodnik wschodni S1 (wzdłuż galerii i kieszeni wschodniej) i pas przy końcu ulicy głównej (x −116.5…−104)
        DzlChodnikPas(Rect.MinMaxRect(DzlSx[0] + DzlPolJezdni, GJ1, DzlGaleriaX, DzlZ1), true);          // przy galerii (z 33…108.5)
        DzlChodnikPas(Rect.MinMaxRect(DzlSx[0] + DzlPolJezdni, DzlZ0 + 9f, -111.5f, GJ0), true);          // wschód S1 na południe od głównej
        DzlChodnikPas(Rect.MinMaxRect(-111.5f, GCh0, -104f, GJ0), false);                                   // płd. chodnik głównej przy końcu ulicy miasta
        DzlChodnikPas(Rect.MinMaxRect(DzlGaleriaX, GJ1, -104f, 36.1f), false);                              // płn. (styk z chodnikiem miasta z 33…36.1)
        DzlChodnikPas(Rect.MinMaxRect(-111.5f, 8f, -104f, GCh0), false);                                     // zaułek za narożnikiem (do kieszeni E)
        // zachodni chodnik S3 (krawędź dzielnicy)
        DzlChodnikPas(Rect.MinMaxRect(DzlTeren.xMin, DzlZ0 - DzlChodnik, DzlSx[2] - DzlPolJezdni, DzlZ1 + DzlChodnik), true);

        // krawężniki wzdłuż wszystkich krawędzi jezdni (bez skrzyżowań)
        group = "Dzielnica / ulice / krawężniki";
        DzlKrawezniki();
        // oznakowanie poziome: linie środkowe (przerywane), zebry na wlotach skrzyżowań (bez sygnalizacji — wykonawca „ruch”)
        group = "Dzielnica / ulice / oznakowanie"; bezCieni.Add(group);
        DzlOznakowanie();
        DzlNota("ulice: jezdnie " + dzlJezdnie.Count + ", skrzyżowania " + dzlSkrzyzowania.Count + ", przejścia " + Przejscia.Count + ", kwartały " + kwartaly.Count);
    }

    // kwartał: prostokąt między krawędziami chodników (lico zabudowy) i prostokąt po krawędziach jezdni (zewnętrzny brzeg chodnika)
    struct DzlKwartal { public string nazwa; public Rect lico, chodnik; }
    static List<DzlKwartal> DzlKwartaly()
    {
        var l = new List<DzlKwartal>();
        float[] xs = { DzlSx[2], DzlSx[1], DzlSx[0] };                       // z zachodu na wschód: −281, −201, −121
        string[] nz = { "B", "A" }, sz = { "D", "C" };
        for (int i = 0; i < 2; i++)
        {
            float x0 = xs[i] + DzlPolJezdni, x1 = xs[i + 1] - DzlPolJezdni;  // krawędzie jezdni
            // północ: od jezdni głównej (z 33) do jezdni N (z 99.5)
            l.Add(new DzlKwartal { nazwa = nz[i], chodnik = Rect.MinMaxRect(x0, GJ1, x1, DzlNz - DzlPolJezdni), lico = Rect.MinMaxRect(x0 + DzlChodnik, GCh1, x1 - DzlChodnik, DzlNz - DzlPolJezdni - DzlChodnik) });
            // południe: od jezdni P (z −55.5) do jezdni głównej (z 24)
            l.Add(new DzlKwartal { nazwa = sz[i], chodnik = Rect.MinMaxRect(x0, DzlPz + DzlPolJezdni, x1, GJ0), lico = Rect.MinMaxRect(x0 + DzlChodnik, DzlPz + DzlPolJezdni + DzlChodnik, x1 - DzlChodnik, GCh0) });
        }
        return l;
    }
    public static Rect DzlLico(string kwartal) => DzlKwartaly().First(q => q.nazwa == kwartal).lico;

    // pierścień chodnika: między zewnętrznym prostokątem (krawędź jezdni) a licem zabudowy
    static void DzlChodnikWokol(Rect zew, Rect lico)
    {
        DzlChodnikPas(Rect.MinMaxRect(zew.xMin, zew.yMin, zew.xMax, lico.yMin), true);
        DzlChodnikPas(Rect.MinMaxRect(zew.xMin, lico.yMax, zew.xMax, zew.yMax), true);
        DzlChodnikPas(Rect.MinMaxRect(zew.xMin, lico.yMin, lico.xMin, lico.yMax), true);
        DzlChodnikPas(Rect.MinMaxRect(lico.xMax, lico.yMin, zew.xMax, lico.yMax), true);
    }
    // pas chodnika: bryła 0…0.15 (kolizja), wierzch z dużych płyt; co 10 m pas płyt ciemniejszych (dylatacja); płyty × k
    static void DzlChodnikPas(Rect r, bool solid)
    {
        if (r.width < .05f || r.height < .05f) return;
        BoxMinMax(DzlBeton, r.xMin, r.xMax, -.2f, DzlYC - .01f, r.yMin, r.yMax, solid);
        Top(DzlPlyty, r.xMin, r.xMax, r.yMin, r.yMax, DzlYC);
        Paved(r.xMin, r.xMax, r.yMin, r.yMax);
    }

    static bool DzlWSkrzyzowaniu(float x, float z, float zapas = 0f)
    {
        foreach (var c in dzlSkrzyzowania) if (Mathf.Abs(x - c.x) < DzlPolJezdni + zapas && Mathf.Abs(z - c.z) < DzlPolJezdni + zapas) return true;
        // wylot ulicy głównej na wschód (x > −116.5) — styk z jezdnią miasta
        return false;
    }
    // krawężnik granitowy 20 × 30 cm (lico 15 cm nad asfaltem, ścięta krawędź), odcinki 1 m z fugą; przerwy na skrzyżowaniach
    static void DzlKrawezniki()
    {
        var kr = DzlKrawez;
        void OdcinekX(float x0, float x1, float z, int strona)   // strona: +1 = chodnik na +z
        {
            for (float x = x0; x < x1 - .05f; x += 1f)
            {
                float e = Mathf.Min(x + .99f, x1); float xc = (x + e) * .5f;
                if (DzlWSkrzyzowaniu(xc, z, .01f)) continue;
                float z0 = strona > 0 ? z : z - .2f, z1 = strona > 0 ? z + .2f : z;
                bool przej = Przejscia.Any(p => Mathf.Abs(p.srodek.x - xc) < 2.1f && Mathf.Abs(p.srodek.z - z) < 6f);
                BoxMinMax(kr, x, e, -.15f, przej ? DzlYJ + .03f : DzlYC, z0, z1, false);
            }
        }
        void OdcinekZ(float z0, float z1, float x, int strona)
        {
            for (float z = z0; z < z1 - .05f; z += 1f)
            {
                float e = Mathf.Min(z + .99f, z1); float zc = (z + e) * .5f;
                if (DzlWSkrzyzowaniu(x, zc, .01f)) continue;
                float x0 = strona > 0 ? x : x - .2f, x1 = strona > 0 ? x + .2f : x;
                bool przej = Przejscia.Any(p => Mathf.Abs(p.srodek.z - zc) < 2.1f && Mathf.Abs(p.srodek.x - x) < 6f) || dzlWjazdy.Any(w => w.Contains(new Vector2(x, zc)));
                BoxMinMax(kr, x0, x1, -.15f, przej ? DzlYJ + .03f : DzlYC, z, e, false);
            }
        }
        // przejścia (zebry) najpierw — krawężnik obniżony na ich szerokości
        DzlPrzejscia();
        OdcinekX(GKoniecX, -104f, GJ0, -1); OdcinekX(GKoniecX, -104f, GJ1, 1);
        foreach (float sx in DzlSx) { OdcinekZ(DzlZ0, DzlZ1, sx - DzlPolJezdni, -1); OdcinekZ(DzlZ0, DzlZ1, sx + DzlPolJezdni, 1); }
        foreach (float sz in new[] { DzlPz, DzlNz }) { OdcinekX(DzlSx[2] - DzlPolJezdni, DzlSx[0] + DzlPolJezdni, sz - DzlPolJezdni, -1); OdcinekX(DzlSx[2] - DzlPolJezdni, DzlSx[0] + DzlPolJezdni, sz + DzlPolJezdni, 1); }
    }

    // zebry: na każdym wlocie skrzyżowań ulicy głównej z S1 i S2 (4 wloty) i po jednej na pozostałych wlotach N-S; pasy 0.5 m × k
    static void DzlPrzejscia()
    {
        void Zebra(Vector3 c, bool wzdluzX, bool swiatla)
        {
            // wzdluzX: jezdnia biegnie wzdłuż x (przejście w poprzek: pasy wzdłuż x? — pasy równoległe do osi jezdni)
            float szer = 4f * K * .8f;   // szerokość przejścia ≈ 4 m
            Przejscia.Add(new Przejscie { srodek = c, wzdluzJezdni = wzdluzX ? Vector3.right : Vector3.forward, szerokoscJezdni = DzlPolJezdni * 2f, zeSwiatlami = swiatla });
            float pas = .5f * K, przerwa = .5f * K;
            for (float a = -DzlPolJezdni + .3f; a < DzlPolJezdni - .3f; a += pas + przerwa)
            {
                float a1 = Mathf.Min(a + pas, DzlPolJezdni - .3f);
                if (wzdluzX) Top(DzlFarba, c.x - szer * .5f, c.x + szer * .5f, c.z + a, c.z + a1, DzlYJ + .004f);
                else Top(DzlFarba, c.x + a, c.x + a1, c.z - szer * .5f, c.z + szer * .5f, DzlYJ + .004f);
            }
        }
        foreach (int i in new[] { 0, 1 })
        {
            float sx = DzlSx[i];
            float off = DzlPolJezdni + 3.2f;
            Zebra(new Vector3(sx - off, 0, GZ), true, true); Zebra(new Vector3(sx + off, 0, GZ), true, true);         // przez główną po obu stronach skrzyżowania
            Zebra(new Vector3(sx, 0, GZ - off), false, true); Zebra(new Vector3(sx, 0, GZ + off), false, true);        // przez S1/S2
        }
        Zebra(new Vector3(DzlSx[2] + DzlPolJezdni + 3.2f, 0, GZ), true, false);
        foreach (float sx in DzlSx) { Zebra(new Vector3(sx, 0, DzlPz + DzlPolJezdni + 3.2f), false, false); Zebra(new Vector3(sx, 0, DzlNz - DzlPolJezdni - 3.2f), false, false); }
        Zebra(new Vector3(-240f, 0, GZ), true, false);                                                               // przy szpitalu
    }

    // linie środkowe przerywane (3 m kreska / 6 m przerwa × k), linie stopu przed zebrami na ulicy głównej
    static void DzlOznakowanie()
    {
        float kr = 3f, prz = 3f * K;
        for (float x = GKoniecX + 6f; x < -105f; x += kr + prz) if (!DzlWSkrzyzowaniu(x + kr * .5f, GZ, 6.5f)) Top(DzlFarba, x, x + kr, GZ - .06f, GZ + .06f, DzlYJ + .004f);
        foreach (float sx in DzlSx)
            for (float z = DzlZ0 + 2f; z < DzlZ1 - 2f; z += kr + prz) if (!DzlWSkrzyzowaniu(sx, z + kr * .5f, 6.5f)) Top(DzlFarba, sx - .06f, sx + .06f, z, z + kr, DzlYJ + .004f);
        foreach (float sz in new[] { DzlPz, DzlNz })
            for (float x = DzlSx[2] + 2f; x < DzlSx[0] - 2f; x += kr + prz) if (!DzlWSkrzyzowaniu(x + kr * .5f, sz, 6.5f)) Top(DzlFarba, x, x + kr, sz - .06f, sz + .06f, DzlYJ + .004f);
        // linie stopu (prawy pas, 0.4 m) przed zebrami przy skrzyżowaniach ze światłami
        foreach (int i in new[] { 0, 1 })
        {
            float sx = DzlSx[i], o = DzlPolJezdni + 5.6f;
            Top(DzlFarba, sx + o, sx + o + .4f, GZ, GJ1 - .2f, DzlYJ + .004f);          // jadący na zachód (pas północny? ruch prawostronny: na zachód jedzie pas z > oś)
            Top(DzlFarba, sx - o - .4f, sx - o, GJ0 + .2f, GZ, DzlYJ + .004f);          // jadący na wschód (pas południowy)
            Top(DzlFarba, sx - DzlPolJezdni + .2f, sx, GZ + o, GZ + o + .4f, DzlYJ + .004f);   // z północy (jadący na południe: pas zachodni)
            Top(DzlFarba, sx, sx + DzlPolJezdni - .2f, GZ - o - .4f, GZ - o, DzlYJ + .004f);   // z południa (jadący na północ: pas wschodni)
        }
    }

    // ---------------- 3) wyposażenie ulic ----------------
    static void DzlWyposazenieUlic()
    {
        int lamp = 0, drz = 0, kosz = 0, krat = 0;
        // ulica główna: latarnie co 24 m po obu stronach (naprzemiennie), drzewa w kratach między nimi, kosze przy przejściach
        for (float x = -110f; x > GKoniecX + 6f; x -= 12f)
        {
            if (DzlWSkrzyzowaniu(x, GZ, 8f)) continue;
            int i = Mathf.RoundToInt((-110f - x) / 12f);
            float zs = GJ0 - .9f, zn = GJ1 + .9f;
            if (i % 2 == 0) { if (DzlLatarnia(new Vector3(x, DzlYC, i % 4 == 0 ? zs : zn), i % 4 == 0 ? Vector3.forward : Vector3.back)) lamp++; }
            else
            {
                if (DzlDrzewoWKracie(new Vector3(x, DzlYC, GJ0 - 1.6f))) drz++;
                if (x < -116f && DzlDrzewoWKracie(new Vector3(x, DzlYC, GJ1 + 1.6f))) drz++;
            }
        }
        // ulice N-S i E-W: latarnie co 26 m (jedna strona), drzewa co 13 m
        foreach (float sx in DzlSx)
            for (float z = DzlZ0 + 8f; z < DzlZ1 - 8f; z += 13f)
            {
                if (DzlWSkrzyzowaniu(sx, z, 8f)) continue;
                int i = Mathf.RoundToInt((z - DzlZ0) / 13f);
                if (i % 2 == 0) { if (DzlLatarnia(new Vector3(sx + DzlPolJezdni + .9f, DzlYC, z), Vector3.left)) lamp++; }
                else if (sx > -280f && DzlDrzewoWKracie(new Vector3(sx - DzlPolJezdni - 1.5f, DzlYC, z))) drz++;
            }
        foreach (float sz in new[] { DzlPz, DzlNz })
            for (float x = DzlSx[0] - 8f; x > DzlSx[2] + 8f; x -= 13f)
            {
                if (DzlWSkrzyzowaniu(x, sz, 8f)) continue;
                int i = Mathf.RoundToInt((DzlSx[0] - x) / 13f);
                if (i % 2 == 0) { if (DzlLatarnia(new Vector3(x, DzlYC, sz - DzlPolJezdni - .9f), Vector3.forward)) lamp++; }
                else if (DzlDrzewoWKracie(new Vector3(x, DzlYC, sz + DzlPolJezdni + 1.5f))) drz++;
            }
        // kratki odpływowe przy krawężnikach (co ~20 m), kosze przy przejściach
        group = "Dzielnica / ulice / kratki"; bezCieni.Add(group);
        foreach (var j in dzlJezdnie)
        {
            bool wzdluzX = j.width > j.height;
            float dl = wzdluzX ? j.width : j.height;
            for (float s = 10f; s < dl - 4f; s += 20f)
                foreach (int st in new[] { -1, 1 })
                {
                    Vector3 p = wzdluzX ? new Vector3(j.xMin + s, DzlYJ, st < 0 ? j.yMin + .32f : j.yMax - .32f) : new Vector3(st < 0 ? j.xMin + .32f : j.xMax - .32f, DzlYJ, j.yMin + s);
                    if (DzlWSkrzyzowaniu(p.x, p.z, 1f) || Przejscia.Any(q => (q.srodek - p).sqrMagnitude < 16f)) continue;
                    DzlKratka(p, wzdluzX); krat++;
                }
        }
        foreach (var p in Przejscia.ToList())
        {
            var d = p.wzdluzJezdni; var pop = Vector3.Cross(Vector3.up, d);
            var c = p.srodek + pop * (DzlPolJezdni + 1.2f) + d * 2.6f; c.y = DzlYC;
            if (DzlKosz(c)) kosz++;
        }
        DzlNota("wyposażenie ulic: latarnie " + lamp + ", drzewa w kratach " + drz + ", kratki odpływowe " + krat + ", kosze " + kosz);
    }

    static bool DzlWolne(Vector3 p, float r, float h = 1.6f) => !Physics.CheckCapsule(p + Vector3.up * (r + .2f), p + Vector3.up * h, r, ~0, QueryTriggerInteraction.Ignore);

    // latarnia uliczna (× k): stopa, słup stożkowy 8.4 m, wysięgnik nad jezdnię, oprawa LED (bez świecenia); dir = w stronę jezdni
    static bool DzlLatarnia(Vector3 at, Vector3 dir)
    {
        if (!DzlWolne(at, .3f)) return false;
        group = "Dzielnica / ulice / latarnie";
        float k = K, h = 6.5f * k; float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        var m = DzlGrafit;
        Cylinder(m, at, .16f * k, .5f, 16, true, false);
        Cylinder(m, at + Vector3.up * .5f, .1f * k, h - .5f, 14, true, false);
        Tube(m, at + Vector3.up * (h - .25f), at + Vector3.up * (h + .1f) + dir * 1.1f * k, .05f * k, 10);
        OBox(m, at + Vector3.up * (h + .12f) + dir * 1.5f * k, new Vector3(.36f * k, .1f * k, .8f * k), yaw);
        OBox(LampGlass, at + Vector3.up * (h + .065f) + dir * 1.5f * k, new Vector3(.28f * k, .012f, .66f * k), yaw);
        Collider(at + Vector3.up * h * .5f, new Vector3(.3f, h, .3f));
        return true;
    }
    // drzewo w żeliwnej kracie 1.5 × 1.5 m × k
    static bool DzlDrzewoWKracie(Vector3 p)
    {
        if (!DzlWolne(p, .9f, 3f)) return false;
        group = "Dzielnica / ulice / kraty drzew"; bezCieni.Add(group);
        float a = .75f * K;
        BoxMinMax(DzlZeliwo, p.x - a, p.x + a, p.y - .01f, p.y + .015f, p.z - a, p.z + a, false);
        for (int q = 0; q < 8; q++) { float t = q * Mathf.PI / 4f; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); OBox(DzlZeliwo, p + d * .45f * K + Vector3.up * .025f, new Vector3(.025f, .02f, .5f * K), t * Mathf.Rad2Deg); }
        group = "Dzielnica / ulice / drzewa";
        var tr = Tree(new Vector3(p.x, p.y, p.z), 7.5f * K * (.9f + .2f * Mathf.Repeat(p.x * .37f + p.z * .11f, 1f)), p.x * 31f + p.z * 7f);
        return tr;
    }
    static void DzlKratka(Vector3 p, bool wzdluzX)
    {
        float w = .5f * K, d = .3f * K;
        float sx = wzdluzX ? w : d, sz = wzdluzX ? d : w;
        BoxMinMax(DzlZeliwo, p.x - sx * .5f, p.x + sx * .5f, p.y, p.y + .008f, p.z - sz * .5f, p.z + sz * .5f, false);
        for (int i = 0; i < 7; i++)
        {
            float t = -.5f + (i + .5f) / 7f;
            if (wzdluzX) BoxMinMax(DzlGrafit, p.x + t * sx - .012f, p.x + t * sx + .012f, p.y + .008f, p.y + .014f, p.z - sz * .45f, p.z + sz * .45f, false);
            else BoxMinMax(DzlGrafit, p.x - sx * .45f, p.x + sx * .45f, p.y + .008f, p.y + .014f, p.z + t * sz - .012f, p.z + t * sz + .012f, false);
        }
    }
    // kosz uliczny z daszkiem (× k): stal ocynk + drewno, kolizja
    static bool DzlKosz(Vector3 p)
    {
        if (!DzlWolne(p, .3f)) return false;
        group = "Dzielnica / ulice / kosze";
        float k = K;
        Cylinder(DzlGrafit, p, .22f * k, .78f * k, 14, false, true);
        Cylinder(DzlZeliwo, p + Vector3.up * .78f * k, .25f * k, .05f * k, 14, true, false);
        Cylinder(DzlGrafit, p + Vector3.up * .83f * k, .06f * k, .05f * k, 8, true, false);
        return true;
    }

    // ---------------- 4) siatki (jak Flush osiedla, ale pliki w folderze dzielnicy) ----------------
    static int dzlFlushN;
    static void DzlFlush()
    {
        foreach (var kv in batches)
        {
            var b = kv.Value; if (b.v.Count == 0) continue;
            var parent = Group(kv.Key.Item1);
            var mesh = new Mesh { name = "Dzielnica " + kv.Key.Item1 + " / " + kv.Key.Item2.name, indexFormat = b.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(b.v); mesh.SetNormals(b.n); mesh.SetTangents(b.t); mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.i, 0);
            mesh.RecalculateBounds();
            BraxZapisAssetu.Utworz(mesh, DzlOut + "/siatka_" + (dzlFlushN++).ToString("D4") + ".asset");
            var g = new GameObject(kv.Key.Item2.name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item2;
            r.shadowCastingMode = bezCieni.Contains(kv.Key.Item1) ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            rep.meshes++; rep.triangles += b.i.Count / 3;
        }
        batches.Clear();
    }

    // ---------------- 5) materiały miasta ----------------
    // Standard (osiedle) → Braxhorn/CityStaticPBR: kolor, mapa koloru, normalne (Unity packed), szorstkość = 1 − gładkość efektywna
    // (średnia mapy × mnożnik, jak w kontroli gładkości osiedla), metal, AO; przezroczyste → mieszanie alfa. Posadzki matowe
    // (szorstkość ≥ 0.5 — zasada „posadzki nie lustrzane”). Materiały innych shaderów (pokój za szybą, liście) kopiowane do folderu
    // dzielnicy; materiały modeli glTF → BraxRegion.CityMaterial. Wnętrza: wariant z _SunIntensity 0 (słońce nie świeci przez strop).
    static readonly Dictionary<Material, Material> dzlMatyMiasta = new Dictionary<Material, Material>();
    static readonly Dictionary<Material, Material> dzlMatyWnetrz = new Dictionary<Material, Material>();
    static int dzlMatN;
    static Material DzlNaMiasto(Material m)
    {
        if (!m) return m;
        if (dzlMatyMiasta.TryGetValue(m, out var have)) return have;
        Material r = m;
        string path = AssetDatabase.GetAssetPath(m) ?? "";
        if (m.shader && m.shader.name == "Standard")
        {
            var sh = Shader.Find(BraxCityMaterials.ShaderName);
            if (sh)
            {
                r = new Material(sh) { name = m.name.Replace("Osiedle: ", "Dzielnica: "), enableInstancing = true };
                r.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
                r.SetColor("_Color", m.color);
                if (m.mainTexture) r.SetTexture("_MainTex", m.mainTexture);
                var bump = m.GetTexture("_BumpMap");
                if (bump) { r.SetTexture("_BumpMap", bump); var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(bump)) as TextureImporter; r.SetFloat("_NormalEncoding", imp && imp.textureType == TextureImporterType.NormalMap ? 1 : 0); r.SetFloat("_BumpScale", m.GetFloat("_BumpScale")); }
                float smooth = m.GetFloat("_Glossiness");
                var mg = m.GetTexture("_MetallicGlossMap");
                if (mg) { float mean = SmoothMean(AssetDatabase.GetAssetPath(mg)); smooth = (mean > 0 ? mean : .5f) * m.GetFloat("_GlossMapScale"); }
                float rough = Mathf.Clamp01(1f - smooth);
                string ln = m.name.ToLowerInvariant();
                if (ln.Contains("posadzk") || ln.Contains("podłog") || ln.Contains("parkiet") || ln.Contains("gres") || ln.Contains("płytki pod") || ln.Contains("chodnik") || ln.Contains("asfalt") || ln.Contains("wykładzin")) rough = Mathf.Max(rough, .55f);
                r.SetFloat("_Roughness", rough); r.SetFloat("_Metallic", m.GetFloat("_Metallic"));
                var ao = m.GetTexture("_OcclusionMap"); if (ao) { r.SetTexture("_OcclusionMap", ao); r.SetFloat("_OcclusionStrength", 1f); }
                r.SetFloat("_UseARMForAO", 0);
                int mode = Mathf.RoundToInt(m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0);
                bool cut = mode == 1 || m.IsKeywordEnabled("_ALPHATEST_ON"), blend = mode >= 2 || m.renderQueue >= 3000;
                r.SetFloat("_AlphaClip", cut ? 1 : 0); r.SetFloat("_Cutoff", cut ? m.GetFloat("_Cutoff") : 0); r.SetFloat("_Transparent", blend ? 1 : 0);
                r.SetFloat("_SrcBlend", (float)(blend ? BlendMode.SrcAlpha : BlendMode.One)); r.SetFloat("_DstBlend", (float)(blend ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
                r.SetFloat("_ZWrite", blend ? 0 : 1); r.renderQueue = blend ? 3000 : cut ? 2450 : 2000;
                r.SetOverrideTag("RenderType", blend ? "Transparent" : cut ? "TransparentCutout" : "Opaque");
                if (blend) r.SetFloat("_ReflectionStrength", .6f);
                BraxZapisAssetu.Utworz(r, DzlOut + "/mat_" + (dzlMatN++).ToString("D3") + "_" + Safe(r.name) + ".mat");
            }
        }
        else if (path.StartsWith("Assets/PlayableGenerated/ClaudeOsiedle/", StringComparison.Ordinal))
        {
            r = new Material(m) { name = m.name.Replace("Osiedle: ", "Dzielnica: ") };
            BraxZapisAssetu.Utworz(r, DzlOut + "/mat_" + (dzlMatN++).ToString("D3") + "_" + Safe(r.name) + ".mat");
        }
        else if (!path.StartsWith(DzlOut, StringComparison.Ordinal) && !BraxCityMaterials.IsCityMaterial(m)) r = BraxRegion.CityMaterial(m);
        dzlMatyMiasta[m] = r; return r;
    }
    static Material DzlWnetrzowy(Material m)
    {
        if (!m || !BraxCityMaterials.IsCityMaterial(m)) return m;
        if (dzlMatyWnetrz.TryGetValue(m, out var have)) return have;
        var r = new Material(m) { name = m.name + " (wnętrze)" };
        r.SetFloat("_SunIntensity", 0f); r.SetFloat("_AmbientIntensity", .55f);
        BraxZapisAssetu.Utworz(r, DzlOut + "/mat_" + (dzlMatN++).ToString("D3") + "_wn_" + Safe(m.name) + ".mat");
        dzlMatyWnetrz[m] = r; return r;
    }
    static bool DzlJestWnetrzem(Transform t)
    {
        for (var p = t; p && p != root; p = p.parent) if (dzlWnetrza.Contains(p.name)) return true;
        return false;
    }
    static void DzlMaterialyMiasta()
    {
        int n = 0, w = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials; bool wn = DzlJestWnetrzem(r.transform);
            for (int i = 0; i < ms.Length; i++)
            {
                var c = DzlNaMiasto(ms[i]);
                if (wn) { var cw = DzlWnetrzowy(c); if (cw != c) w++; c = cw; }
                if (c != ms[i]) n++;
                ms[i] = c;
            }
            r.sharedMaterials = ms;
        }
        DzlNota("materiały: przepisane " + n + " (w tym wnętrza bez słońca " + w + "), nowych materiałów " + dzlMatN);
    }

    // ---------------- 6) bryły z daleka (scena główna) ----------------
    // Gdy obszar dzielnicy nie jest wczytany (gracz dalej niż ~180 m), w scenie głównej stoi jedna siatka brył (pudełka z teksturą
    // okien) — sylwetka miasta z tarasu Tower, z plaży i z wiaduktu. Chowa ją BraxDzielnicaZDaleka, gdy obszar się wczyta.
    static void DzlZDaleka()
    {
        if (dzlBryly.Count == 0) { DzlNota("bryły z daleka: brak brył"); return; }
        var mats = new List<Material>(); var subs = new List<List<int>>();
        var v = new List<Vector3>(); var nn = new List<Vector3>(); var uv = new List<Vector2>();
        Material MatStylu(int styl)
        {
            var c = styl == 1 ? new Color(.30f, .35f, .40f) : styl == 2 ? new Color(.46f, .42f, .37f) : styl == 3 ? new Color(.42f, .26f, .20f) : new Color(.62f, .60f, .56f);
            return BraxCityMaterials.Surface("Dzielnica z daleka: bryła " + styl, c, .7f, styl == 1 ? .3f : 0f);
        }
        foreach (var (r, h, styl) in dzlBryly)
        {
            var m = MatStylu(styl); int si = mats.IndexOf(m); if (si < 0) { mats.Add(m); subs.Add(new List<int>()); si = mats.Count - 1; }
            var tri = subs[si];
            void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int k = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int i = 0; i < 4; i++) nn.Add(n);
                uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0) tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); else tri.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            }
            float x0 = r.xMin, x1 = r.xMax, z0 = r.yMin, z1 = r.yMax, y0 = 0f, y1 = h;
            Q(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.up);
            Q(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0), Vector3.back);
            Q(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.forward);
            Q(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), Vector3.left);
            Q(new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), Vector3.right);
        }
        var mesh = new Mesh { name = "Dzielnica: bryły z daleka", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetNormals(nn); mesh.SetUVs(0, uv); mesh.subMeshCount = subs.Count;
        for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        BraxZapisAssetu.Utworz(mesh, DzlOut + "/z_daleka.asset");
        var old = GameObject.Find(DzlZDalekaNazwa); if (old) UnityEngine.Object.DestroyImmediate(old);
        var g = new GameObject(DzlZDalekaNazwa);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = mats.ToArray(); mr.shadowCastingMode = ShadowCastingMode.Off;
        var z = g.AddComponent<BraxDzielnicaZDaleka>(); z.scena = DzlScena; z.bryly = mr;
        DzlNota("bryły z daleka: " + dzlBryly.Count + " brył, " + v.Count + " wierzchołków (scena główna, chowane po wczytaniu obszaru " + DzlScena + ")");
    }

    // ---------------- 7) punkty podróży ----------------
    static void DzlPunkty()
    {
        if (!dzlWorld) return;
        var add = new[]
        {
            new BraxCityPoint { id = "dzielnica_wjazd", label = "Dzielnica Zachodnia — wjazd z ulicy głównej", position = new Vector3(-112f, DzlYC, 21.5f) },
            new BraxCityPoint { id = "komisariat", label = "Komisariat Policji", position = new Vector3(KomX0 + 13f, DzlYC, GCh0 + 2.2f) },
        };
        dzlWorld.points = dzlWorld.points.Where(p => p == null || !add.Any(a => a.id == p.id)).Concat(add).ToArray();
    }
}
