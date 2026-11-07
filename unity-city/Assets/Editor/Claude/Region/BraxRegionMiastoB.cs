using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — ROZBUDOWA MIASTA, ETAP 1 (wykonawca B, 2.11; układ: scratchpad/plan/B_ETAP1.md).
// Decyzja właściciela 15:13: „Rób to skrzyżowanie, żeby powiększyć miasto. Jak wychodzisz z siłowni, idziesz na wiadukt —
// w PRAWO i w LEWO ma być powiększone miasto”; „to ma być GŁÓWNA ULICA — ta, co jest; potem jedna w prawo, druga w lewo”.
//  0) USUWA (zasada 11:00 „zdejmij stare, połóż nowe”): stary wiadukt „Riveted steel viaduct” (BraxCityGround) z kolizjami,
//     przęsła/portal/warsztaty/wzgórze z tyłów (krok BraxRegionBackyard nie stawia już wiaduktu — patrz BraxRegionTylyB),
//     płot końca ulicy x −104, kreski osi w obrębie nowego skrzyżowania, chodnik i krawężnik ulicy głównej w wylocie
//     ul. Wiaduktowej, wszystkie siatki i kolizje oryginału (grunt-lustro) na terenie rozbudowy;
//  1) skrzyżowanie w miejscu wiaduktu: ul. Wiaduktowa (x −66.2) w poziomie ulic, z sygnalizacją na wysięgnikach;
//  2) ulica główna przez miejsce po wiadukcie dalej na zachód do RONDA (−126, 28.5); z ronda Aleja Północna (w prawo)
//     i Aleja Południowa (w lewo), szeroka jezdnia 2 × 3.5 m × k + pasy zieleni z drzewami + chodniki 5 m;
//  3) pierwsze kwartały obu alej: kamienice z kompletem detali (BraxRegionKamienica), warsztaty spod łuków jako pierzeja
//     ul. Warsztatowej (jeden otwarty), plac ustawek 3 na 3, przystanek, wyjazd na osiedle (szlaban, znak, tunel w zboczu).
// Wymiary × k; nawierzchnie: asfalt y 0.003 (jak ulica główna), chodnik/krawężnik y 0.15, trawa 0.12 (w otworach nawierzchni).
// Żadna płaska powierzchnia nie leży na innej w tej samej wysokości (styk krawędzią albo odstęp ≥ 2 cm).
public static partial class BraxRegionMiastoB   // partial: nowe dzielnice ze Sketchfaba (BraxRegionMiastoBDzielnice.cs — te same drogi)
{
    static float K => BraxClaudeOsiedle.K;
    // ---------------- UKŁAD (B_ETAP1.md) ----------------
    public const float RX = -126f, RZ = 28.5f;            // rondo
    public const float RWyspa = 7.5f, RJezdnia = 14.5f, RPlac = 22f;
    public const float AX = -126f, AH = 4.5f;              // osie alej (x), połowa jezdni
    public const float Kr = .25f, Ziel = 2.25f, Chod = 5f; // krawężnik, pas zieleni, chodnik → lico pierzei 12 m od osi
    public const float AN1 = 96f;                          // koniec 1. kwartału Alei Północnej (dalej pętla)
    public const float PetlaZ = 104f, PetlaR = RJezdnia, PetlaWyspa = RWyspa, PetlaPlac = RPlac;   // pętla = rondo tej samej wielkości (zawracanie aut i autobusu)
    public const float AS1 = -84f;                         // portal tunelu (wyjazd na osiedle)
    public const float WX = -66.2f, WH = 3f;               // ul. Wiaduktowa
    public const float SZ = -19f, SH = 3f, SX1 = -61.4f;   // ul. Warsztatowa
    public const float GZ = 28.5f, GH = 4.5f;              // ulica główna
    const float YA = .003f, YC = .15f;
    static readonly float[] WlotyRonda = { 0f, 90f, 180f, 270f };   // kąty wlotów ronda (0 = +x E, 90 = +z N, 180 = −x W, 270 = −z S)
    static readonly (Vector3 dir, float hj)[] RamionaRonda = { (Vector3.right, GH), (Vector3.forward, AH), (Vector3.left, AH), (Vector3.back, AH) };
    // ETAP 2: ul. Zachodnia (druga główna ulica, zachodnie ramię ronda) z pętlą ZACHÓD i budynkami publicznymi
    public const float ZachX1 = RX - RPlac;              // −148: wschodni koniec (krawędź placu ronda)
    public const float PetlaZX = -240f;                  // pętla ZACHÓD (rondo tej samej wielkości co rondo i pętla PÓŁNOC), plac x −262…−218
    public const float ZachX0 = PetlaZX + PetlaPlac;     // −218: zachodni koniec (krawędź placu pętli)
    public static readonly Rect Komisariat = Rect.MinMaxRect(-214f, -1f, -186f, RZ - 12f);   // front (lico) z 16.5, wejście od ulicy
    public static readonly Rect Szpital = Rect.MinMaxRect(-184f, -1f, -156f, RZ - 12f);
    // ETAP 3: CENTRUM (biznes) na północ od ul. Zachodniej — plac z płyt x −262…−156, z 58…120, dwa rzędy wieżowców wejściem na +z
    // (rząd 1 z 60…80 → aleja A1 z 80…92, rząd 2 z 92…112 → aleja A2 z 112…120); GALERIA za komisariatem (front z −8, przedpole do
    // placu pętli ZACHÓD). Wysokości 80…200 m × k (22…54 kondygnacje).
    public static readonly Rect PlacCentrum = Rect.MinMaxRect(-262f, 58f, -156f, 120f);
    public static readonly Rect[] WiezeCentrum =
    {
        Rect.MinMaxRect(-258f, 60f, -232f, 80f), Rect.MinMaxRect(-224f, 60f, -198f, 80f), Rect.MinMaxRect(-190f, 60f, -164f, 80f),
        Rect.MinMaxRect(-258f, 92f, -232f, 112f), Rect.MinMaxRect(-224f, 92f, -198f, 112f), Rect.MinMaxRect(-190f, 92f, -164f, 112f),
    };
    public static readonly Rect Galeria = Rect.MinMaxRect(-250f, -38f, -196f, -8f);
    static readonly Rect PrzedpoleGalerii = Rect.MinMaxRect(-262f, -8f, -196f, -1f);
    static readonly Rect DojscieGalerii = Rect.MinMaxRect(-262f, -1f, -214f, RZ - PetlaPlac);           // z −1…6.5 do placu pętli
    static readonly Rect PrzejscieDoGalerii = Rect.MinMaxRect(-218f, RZ - PetlaPlac, -214f, RZ - 12f);  // z 6.5…16.5 (ul. Zachodnia → galeria)
    static readonly Rect PrzejscieDoCentrum = Rect.MinMaxRect(-218f, RZ + 12f, -214f, 58f);            // z 40.5…58 (ul. Zachodnia → plac)
    static readonly Rect PrzejscieCentrumPetla = Rect.MinMaxRect(-156f, 82f, AX - PetlaPlac - 3f, 92f);  // aleja A1 → pętla PÓŁNOC
    // parking przy warsztatach (za warsztatami, wjazd z Alei Południowej przez zjazd we wschodnim pasie zieleni): 2 rzędy po 12
    // stanowisk prostopadłych 2.5 × 5 m × k, aleja manewrowa 6 m; powierzchnia na poziomie chodnika
    public const float ParkX0 = -112f, ParkX1 = -72f, ParkZ0 = -66f, ParkZ1 = -46f, ParkAleja0 = -59f, ParkAleja1 = -53f;
    public static readonly List<(Vector3 c, Vector3 przod, Vector2 rozmiar)> MiejscaParkingu = new List<(Vector3, Vector3, Vector2)>();

    static Transform root;
    static Material asfalt, plyty, plytyCiemne, krawMat, farba, granit, beton, stal, cegla, granitPlac;
    static readonly List<string> usuniete = new List<string>();
    static readonly List<string> kroki = new List<string>();
    static int drzewa, latarnie, zebry, sygnalizatory;
    public static readonly List<Vector3> SkrzyzowaniaZeSwiatlami = new List<Vector3>();

    // wyposażenie odłożone do etapu 3 (stawiane na gotowej nawierzchni) i rejestr kolizji nawierzchni (do pomiaru wysokości)
    static readonly List<(string co, Action a)> wyposazenie = new List<(string, Action)>();
    static readonly HashSet<Collider> nawierzchnie = new HashSet<Collider>();
    static void Pozniej(string co, Action a) => wyposazenie.Add((co, a));
    static void Nawierzchnia(GameObject g) { if (g) foreach (var c in g.GetComponentsInChildren<Collider>(true)) nawierzchnie.Add(c); }
    static void Nawierzchnia(Collider c) { if (c) nawierzchnie.Add(c); }
    // wysokość gotowej nawierzchni pod punktem (najwyższa trafiona nawierzchnia z rejestru); brak = wartość domyślna (i log)
    static int bezPodloza;
    static float Podloze(float x, float z, float dom)
    {
        float best = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(new Vector3(x, 60f, z), Vector3.down, 70f, ~0, QueryTriggerInteraction.Ignore))
            if (nawierzchnie.Contains(h.collider) && h.point.y > best) best = h.point.y;
        if (float.IsNegativeInfinity(best)) { bezPodloza++; if (bezPodloza <= 6) Debug.LogWarning("BRAXHORN_MIASTO_B brak nawierzchni pod (" + x.ToString("0.0") + ", " + z.ToString("0.0") + ") — wysokość domyślna " + dom); return dom; }
        return best;
    }
    static Vector3 NaPodlozu(Vector3 p) { p.y = Podloze(p.x, p.z, p.y); return p; }
    static void Wyposazenie()
    {
        Physics.SyncTransforms();
        int ok = 0;
        foreach (var (co, a) in wyposazenie) { try { a(); ok++; } catch (Exception e) { Debug.LogError("BRAXHORN_MIASTO_B wyposażenie " + co + ": " + e); } }
        BraxRegion.Note("miasto B: wyposażenie na podłożu " + ok + "/" + wyposazenie.Count + ", bez nawierzchni pod spodem: " + bezPodloza);
        wyposazenie.Clear();
    }

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        usuniete.Clear(); kroki.Clear(); SkrzyzowaniaZeSwiatlami.Clear(); asfalty.Clear(); wyposazenie.Clear(); nawierzchnie.Clear(); zjazdy.Clear(); MiejscaParkingu.Clear(); drzewa = latarnie = zebry = sygnalizatory = 0; MigotaniePary = 0;
        BraxRegionKamienica.Zeruj(); BraxZnakiPL.Zeruj();
        root = BraxRegion.Root(cityRoot, "CLAUDE miasto B: rondo, aleje, ul. Wiaduktowa i Warsztatowa");
        Krok("usunięcie wiaduktu, płotu końca ulicy i gruntu-lustra", () => UsunStare(cityRoot));
        Krok("materiały", Materialy);
        // KOLEJNOŚĆ JAK NA BUDOWIE (właściciel 16:04): 1) teren i nawierzchnie (grunt → jezdnia → krawężnik → chodnik → oznakowanie
        // w nawierzchni), 2) budynki, 3) wyposażenie ulicy (drzewa, latarnie, ławki, słupki, znaki, sygnalizacja, przystanek) —
        // każdy element postawiony na GOTOWYM podłożu (wysokość z promienia w dół na nawierzchnię), 4) ruch (krok osobny, później).
        Krok("1. nawierzchnie: rondo (pierścień, wyspa, chodniki)", Rondo);
        Krok("1. nawierzchnie: Aleja Północna i pętla", AlejaPolnocna);
        Krok("1. nawierzchnie: Aleja Południowa do tunelu", AlejaPoludniowa);
        Krok("1. nawierzchnie: ul. Zachodnia i pętla ZACHÓD (ETAP 2)", UlicaZachodnia);
        Krok("1. nawierzchnie: NOWE DZIELNICE (Aleja Centralna, ulice C0–C3, Nowojorska, Tramwajowa, kwartały, skwer)", NoweDzielniceNawierzchnie);
        Krok("1. nawierzchnie: ul. Wiaduktowa i ul. Warsztatowa", UliceBoczne);
        Krok("1. teren między ulicami (trawniki, place, skwer)", Teren);
        Krok("1. parking przy warsztatach (asfalt z liniami w wycięciach, wjazd)", Parking);
        Krok("1. jezdnie z oznakowaniem w wycięciach nawierzchni", PlanOznakowania);
        Krok("2. budynki: pierzeje alej (kamienice z detalami)", Pierzeje);
        Krok("2. budynki: warsztaty (jeden otwarty) i plac ustawek", Warsztaty);
        Krok("2. budynki: komisariat policji i szpital miejski (ul. Zachodnia)", () => BraxRegionTylyB.BudynkiZachodu(world, cityRoot, Komisariat, Szpital));
        Krok("2. budynki: CENTRUM — 6 wieżowców i GALERIA (ETAP 3)", () => BraxRegionTylyB.Centrum(world, cityRoot, WiezeCentrum, Galeria));
        Krok("2. budynki: NOWE DZIELNICE (modele Sketchfab: centrum city5, kamienice city3, ATLANTA, LONDON, KINNAIRD)", () => NoweDzielniceBudynki(cityRoot));
        Krok("2. budowle: portal tunelu i zbocze", WyjazdNaOsiedle);
        Krok("3. wyposażenie na gotowym podłożu (drzewa, latarnie, ławki, słupki, rzeźba)", Wyposazenie);
        Krok("3. sygnalizacja na wysięgnikach i znaki", Sygnalizacja);
        Krok("3. NOWE DZIELNICE: sygnalizacja alei, przystanki (autobus, tramwaj), wyposażenie", NoweDzielniceWyposazenie);
        Krok("3. przystanek autobusowy", Przystanek);
        Krok("3. szlaban i znak OSIEDLE", SzlabanIZnak);
        Krok("4. rejestracja jezdni dla ruchu (RUCH_API)", RejestracjaRuchu);
        Krok("4. NOWE DZIELNICE: rejestracja jezdni", NoweDzielniceRejestracja);
        BraxRegionKamienica.Raport();
        BraxRegion.Note("miasto B: " + string.Join("; ", kroki));
        Debug.Log("BRAXHORN_MIASTO_B_UKLAD rondo=(" + RX + "," + RZ + ") R=" + RJezdnia + " aleja_pn x=" + AX + " z " + (RZ + 13.8f).ToString("0.0") + "…" + PetlaZ +
                  " aleja_pd z " + (RZ - 13.8f).ToString("0.0") + "…" + AS1 + " wiaduktowa x=" + WX + " warsztatowa z=" + SZ +
                  " kamienic=" + BraxRegionKamienica.Budynki + " drzew=" + drzewa + " latarni=" + latarnie + " zebr=" + zebry + " sygnalizatorow=" + sygnalizatory);
        Debug.Log("BRAXHORN_MIASTO_B_MIGOTANIE pary=" + MigotaniePary + " (nawierzchnie i oznakowanie miasta B: nic nie leży na niczym w tej samej wysokości)");
        Debug.Log("BRAXHORN_MIASTO_B_USUNIETE (" + usuniete.Count + "): " + string.Join(" | ", usuniete.Take(80)));
    }

    static void Krok(string nazwa, Action a)
    {
        try { a(); kroki.Add(nazwa + ": OK"); }
        catch (Exception e) { kroki.Add(nazwa + ": BŁĄD " + e.Message); Debug.LogError("BRAXHORN_MIASTO_B " + nazwa + ": " + e); }
    }

    static Bounds Bx(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    // =====================================================================================================================
    // 0) usunięcie starego
    static readonly Type[] Zostaja = { typeof(BraxGymPlayer), typeof(BraxCityWorld), typeof(Camera), typeof(BraxCityElevator), typeof(BraxVessel), typeof(BraxNutritionCounter), typeof(BraxCityDoor) };
    static bool Chroniony(Transform t)
    {
        for (var p = t; p; p = p.parent)
        {
            if (p.name.StartsWith("braxhorn-original-gym", StringComparison.Ordinal) || p.name.StartsWith("CLAUDE miasto B", StringComparison.Ordinal) || p.name.StartsWith("CLAUDE tyły B", StringComparison.Ordinal)) return true;
            foreach (var z in Zostaja) if (p.GetComponent(z)) return true;
            var l = p.GetComponent<Light>(); if (l && l.type == LightType.Directional) return true;
        }
        return false;
    }

    // teren rozbudowy (bez pierzei ulicy głównej x ≥ −104 i bez budynków przy wiadukcie)
    static IEnumerable<Bounds> ObjetosciCiecia()
    {
        yield return Bx(-270f, -104.3f, -3f, 140f, -100f, 128f);        // rondo, aleje, pierzeje alej, wyjazd na osiedle, ul. Zachodnia (ETAP 2)
        yield return Bx(-104.3f, -71.3f, -3f, 140f, -100f, 11.3f);      // za pierzeją płd. ulicy głównej: warsztaty, plac ustawek
        yield return Bx(-104.3f, -61.7f, -3f, 140f, 48.3f, 128f);       // za pierzeją płn.: skwer (dawny teren pod wiaduktem)
        yield return Bx(-70.95f, -61.45f, -3f, 3f, -100f, 20.55f);      // korytarz dawnego wiaduktu na płd. (ul. Wiaduktowa) — tylko grunt (y < 3)
        yield return Bx(-70.95f, -61.45f, -3f, 3f, 36.2f, 48.3f);       // przejście pod dawnym wiaduktem na płn. — tylko grunt
        yield return Bx(-61.7f, -48.3f, -3f, 3f, 48.3f, 126f);          // skwer wsch. (do x −48, przed podwórkiem A) — tylko grunt
    }

    static void UsunStare(Transform cityRoot)
    {
        var scena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var korzenie = scena.GetRootGameObjects().ToList();
        // a) stary wiadukt (BraxCityGround.Viaduct): cały obiekt z kolizjami (pomost, filary, stopy, szyny)
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
        {
            if (!t) continue;
            if (t.name == "Riveted steel viaduct" || t.name == "Wiadukt: przedłużenie na południe")
            { usuniete.Add("wiadukt: " + Sciezka(t) + " (" + t.GetComponentsInChildren<Renderer>(true).Length + " rendererów)"); UnityEngine.Object.DestroyImmediate(t.gameObject); }
        }
        // b) kolizje oryginału w korytarzu wiaduktu (stare filary w fizyce źródła) — już wycięte w BraxCityRebuild (x −71.3…−61.1)
        // c) PŁOT + SZLABAN PRZED PIERWSZYM RONDEM na głównej:
        // Stary test plot.Contains(b.min/max) pomijał dłuższe ogrodzenie
        // i osobne podpory. Chronimy asfalt, sklepy, oświetlenie i zebry.
        UsunPlotPrzedPierwszymRondem(korzenie);
        // d) kreski osi ulicy głównej w obrębie nowego skrzyżowania (malowane od nowa: linia ciągła przed skrzyżowaniem)
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
            if (t && t.name == "Centre line dash" && t.position.x > -104.5f && t.position.x < WX + 26f)
            { usuniete.Add("kreska osi x=" + t.position.x.ToString("0.0")); UnityEngine.Object.DestroyImmediate(t.gameObject); }
        // e) chodnik i krawężnik płd. ulicy głównej w wylocie ul. Wiaduktowej (x −69.45…−62.95, z 20.5…24.05)
        var wylot = Bx(WX - WH - Kr, WX + WH + Kr, -.6f, .6f, 20.5f, 23.999f);
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
            if (t && t.name == "Street pavement and dry asphalt")
            {
                int n = BraxCityMeshCut.Subtract(t, wylot, BraxRegion.Out + "/Cuts", "wylot_wiaduktowej");
                usuniete.Add("chodnik + krawężnik ulicy głównej w wylocie ul. Wiaduktowej: " + n + " siatek");
            }
        if (BraxClaudeGround.SourcePhysics)
            try { usuniete.Add("kolizje chodnika w wylocie: " + BraxCityMeshCut.SubtractBoxColliders(BraxClaudeGround.SourcePhysics, wylot)); } catch (Exception e) { usuniete.Add("kolizje wylotu: " + e.Message); }
        // f) wszystko stare na terenie rozbudowy: całe obiekty w środku precz, nachodzące (grunt-lustro, łąka) — wycięte trójkąty
        int obiekty = 0, siatki = 0, kolizje = 0;
        foreach (var v in ObjetosciCiecia())
            foreach (var g in korzenie)
            {
                if (!g || Chroniony(g.transform)) continue;
                foreach (var r in g.GetComponentsInChildren<Renderer>(true).ToList())
                {
                    if (!r || Chroniony(r.transform) || r is SkinnedMeshRenderer) continue;
                    if (r.name.StartsWith("Architectural lettering")) continue;
                    var b = r.bounds; if (!b.Intersects(v)) continue;
                    if (v.Contains(b.min) && v.Contains(b.max))
                    {
                        if (obiekty < 40) usuniete.Add("w terenie rozbudowy: " + Sciezka(r.transform));
                        var go = r.gameObject;
                        if (go.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(go);
                        else { var mf = go.GetComponent<MeshFilter>(); UnityEngine.Object.DestroyImmediate(r); if (mf) UnityEngine.Object.DestroyImmediate(mf); }
                        obiekty++;
                    }
                }
                if (!g) continue;
                try { siatki += BraxCityMeshCut.Subtract(g.transform, v, BraxRegion.Out + "/Cuts", "miastoB"); } catch (Exception e) { usuniete.Add("cięcie " + g.name + ": " + e.Message); }
                foreach (var c in g.GetComponentsInChildren<Collider>(true).ToList())
                {
                    if (!c || !c.enabled || c.isTrigger || Chroniony(c.transform)) continue;
                    var b = c.bounds; if (!b.Intersects(v)) continue;
                    if (v.Contains(b.min) && v.Contains(b.max)) { c.enabled = false; kolizje++; continue; }
                    if (c is BoxCollider && Quaternion.Angle(c.transform.rotation, Quaternion.identity) < .01f)
                    { try { kolizje += BraxCityMeshCut.SubtractBoxColliders(c.transform, v); } catch (Exception) { } }
                    else if (!(c is MeshCollider) && v.Contains(b.center)) { c.enabled = false; kolizje++; }
                }
            }
        usuniete.Add("teren rozbudowy: obiekty " + obiekty + ", siatki wycięte " + siatki + ", kolizje " + kolizje);
    }

    // Ulica główna z = 28.5, źródłowy płot przecinający ją przy x≈−104.
    // Zdjęcie właściciela 7.11: siatka, szlaban czerwono-biały i betonowe
    // zapory na zebrze; usunąć ZARÓWNO geometrię, JAK I kolizje.
    static void UsunPlotPrzedPierwszymRondem(List<GameObject> korzenie)
    {
        var exact = Bx(-106.25f, -102.95f, -.20f, 5.0f, 18.9f, 38.1f);
        var road = Bx(-107.0f, -102.5f, .10f, 5.1f, GZ - GH, GZ + GH);
        // Oryginał Unity importuje nazwy 724 siatek jako CityMesh_N,
        // więc sama nazwa Renderera NIE identyfikuje siatki płotu.
        // Manifest source GLTF zachowuje ich opisowe originalName.
        var labels = new Dictionary<string,string>(StringComparer.Ordinal);
        const string manifestPath = "Assets/Source/City/city-manifest.json";
        try
        {
            if(File.Exists(manifestPath))
            {
                var manifest=JsonUtility.FromJson<BraxCityBuild.Manifest>(
                    File.ReadAllText(manifestPath));
                if(manifest!=null && manifest.meshes!=null)
                    foreach(var item in manifest.meshes)
                        if(item!=null && !string.IsNullOrEmpty(item.name) &&
                           !string.IsNullOrEmpty(item.originalName))
                            labels[item.name]=item.originalName;
            }
            else usuniete.Add("PŁOT GŁÓWNA: brak manifestu oryginalnych nazw siatek");
        }
        catch(Exception e)
        {
            usuniete.Add("PŁOT GŁÓWNA: nie można odczytać manifestu: "+e.Message);
        }
        bool ObcaPrzeszkoda(string nazwa)
        {
            string n = (nazwa ?? "").ToLowerInvariant();
            return n.Contains("fence") || n.Contains("chain link") ||
                   n.Contains("chainlink") || n.Contains("ogrodzen") ||
                   n.Contains("płot") || n.Contains("plot ") ||
                   n.Contains("szlaban") || n.Contains("barrier") ||
                   n.Contains("barricade") || n.Contains("roadblock") ||
                   n.Contains("zapora") || n.Contains("kozioł") ||
                   n.Contains("kozlo") || n.Contains("pachoł") ||
                   n.Contains("pachol");
        }
        bool Poprzeczna(Bounds b) =>
            b.Intersects(road) && Mathf.Abs(b.center.x + 104.5f) < 2.7f &&
            b.size.x < 3.5f && b.size.z > 6.5f &&
            b.size.y > .45f && b.size.y < 6.0f &&
            b.min.z < GZ-1f && b.max.z > GZ+1f;
        var removed = 0; var disabled = 0; var clipped = 0; var colliders = 0; var splitColliders = 0; var unresolved = 0;
        var descriptions = new List<string>();
        foreach (var g in korzenie)
        {
            if (!g || Chroniony(g.transform)) continue;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true).ToArray())
            {
                if (!r || !r.enabled || Chroniony(r.transform)) continue;
                Bounds q = r.bounds;
                if (!q.Intersects(exact) || q.max.y < .5f) continue;
                bool named = ObcaPrzeszkoda(r.name) ||
                    (labels.TryGetValue(r.name,out var originalName) &&
                     ObcaPrzeszkoda(originalName));
                for(var t = r.transform.parent; t && !named && t != g.transform.parent; t = t.parent)
                    named = ObcaPrzeszkoda(t.name);
                // Nienazwana siatka musi wyglądać geometrycznie jak płot
                // w POPRZEK jezdni. Sam fakt położenia obok nie wystarcza.
                bool shape = Poprzeczna(q);
                if (!named && !shape) continue;
                if (!q.Intersects(road)) continue;
                // Nie niszczymy długiego ogrodzenia (może ciągnąć się
                // za jezdnią) ani połączonego mesha. Wycinamy jedynie pas
                // blokujący przejście; pozostałe trójkąty źródła zostają.
                if (q.size.y > 6.0f || q.size.x > 21.0f || q.size.z > 24.0f)
                {
                    if (named && q.size.y <= 8f)
                    {
                        try
                        {
                            int cut = BraxCityMeshCut.Subtract(r.transform, exact,
                                BraxRegion.Out + "/Cuts", "plot_poprzeczny_glowna");
                            if (cut > 0) { clipped += cut; continue; }
                        }
                        catch (Exception e)
                        {
                            usuniete.Add("PŁOT GŁÓWNA DO POPRAWKI cięcie siatki: " + e.Message);
                        }
                    }
                    unresolved++;
                    continue;
                }
                string id = Sciezka(r.transform);
                if(r.transform.childCount==0)
                {
                    UnityEngine.Object.DestroyImmediate(r.gameObject);
                    removed++;
                }
                else
                {
                    // Inny renderer może mieć wspólnego rodzica z budynkiem;
                    // znikają wyłącznie trójkąty pojedynczej przeszkody.
                    r.enabled = false;
                    foreach(var c in r.GetComponents<Collider>()) c.enabled=false;
                    disabled++;
                }
                if (descriptions.Count < 25) descriptions.Add(id + " [" +
                    (shape?"geometria_poprzeczna":"nazwa_zapory") + "]");
            }
            // Stare collidery często były na osobnym pustym obiekcie:
            // nie zależą od tego, czy usunięto renderer.
            foreach(var c in g.GetComponentsInChildren<Collider>(true).ToArray())
            {
                if(!c || !c.enabled || c.isTrigger || Chroniony(c.transform) ||
                    !c.bounds.Intersects(exact) || !c.bounds.Intersects(road))continue;
                var q=c.bounds;
                bool shape=Poprzeczna(q);
                bool named=ObcaPrzeszkoda(c.name);
                for(var t=c.transform.parent;t && !named && t!=g.transform.parent;t=t.parent)
                    named=ObcaPrzeszkoda(t.name);
                if(!(shape||named) || q.size.y>8f || q.size.x>21f)continue;
                if (q.size.z>24f && c is BoxCollider)
                {
                    try
                    {
                        int cut = BraxCityMeshCut.SubtractBoxColliders(c.transform, exact);
                        if (cut>0) { splitColliders+=cut; continue; }
                    }
                    catch(Exception e)
                    {
                        usuniete.Add("PŁOT GŁÓWNA DO POPRAWKI cięcie kolizji: "+e.Message);
                    }
                    unresolved++;
                    continue;
                }
                if(q.size.z>24f) { unresolved++; continue; }
                c.enabled=false;
                colliders++;
            }
        }
        usuniete.Add("PŁOT GŁÓWNA PRZED RONDEM: rendererów usuniętych=" +
            removed+" wyłączonych="+disabled+" cięć_siatek="+clipped+
            " kolizji="+colliders+" cięć_kolizji="+splitColliders+
            " nierozpoznanych="+unresolved+
            (descriptions.Count>0?" ["+string.Join("; ",descriptions)+"]":""));
        if (removed+disabled+clipped==0)
            Debug.LogWarning("BRAXHORN_GLOWNA_PLOT DO POPRAWKI: brak rozpoznanej " +
                "siatki płotu x=-104.5 z=28.5; nie usuwano elementów na ślepo");
    }

    static string Sciezka(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 160; p = p.parent) s = p.name + "/" + s; return s; }

    // =====================================================================================================================
    // materiały — fotoskany (CC0): asfalt i płyty jak ulica główna (ciągłość), granit, beton
    static void Materialy()
    {
        try { asfalt = BraxCityArchitecture.SurfaceTexture("Miasto B: asfalt (jak ulica główna)", "clean_asphalt", "clean_asphalt_diff_2k.jpg", 2.1f); }
        catch (Exception) { asfalt = BraxRegionMeble.Tex("Miasto B: asfalt", 3.0f, new Color(.55f, .55f, .55f), .92f, "asphalt_04"); }
        asfalt.SetFloat("_Roughness", Mathf.Max(.75f, asfalt.GetFloat("_Roughness")));
        string dir = "Assets/Source/CityRebuild/textures/concrete_pavement_02/";
        plyty = BraxRegionMeble.Tex("Miasto B: płyty chodnika", 1.4f, new Color(.80f, .77f, .72f), .88f, "concrete_pavers_02", "concrete_pavement_02");
        plytyCiemne = BraxRegionMeble.Tex("Miasto B: płyty placu (granit)", 1.2f, new Color(.70f, .68f, .65f), .82f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
        krawMat = BraxCityMaterials.Surface("Miasto B: krawężnik granitowy", new Color(.44f, .43f, .40f), .9f);
        farba = BraxCityMaterials.Surface("Miasto B: farba drogowa biała", new Color(.86f, .86f, .82f), .7f);
        granit = BraxRegionMeble.Tex("Miasto B: granit", 1.2f, new Color(.62f, .62f, .60f), .8f, "granite_tile_02", "rough_block_wall");
        beton = BraxRegionMeble.Tex("Miasto B: beton", 2.0f, new Color(.78f, .77f, .74f), .9f, "concrete_floor_02", "concrete_pavement_02", "concrete_wall_008");
        stal = BraxCityMaterials.Surface("Miasto B: stal ocynkowana", new Color(.58f, .60f, .62f), .5f, .7f);
        cegla = BraxRegionMeble.Tex("Miasto B: cegła warsztatów", 1.0f, new Color(.85f, .74f, .68f), .9f, "red_brick_03", "brick_wall_001", "brick_wall_10");
        granitPlac = BraxRegionMeble.Tex("Miasto B: płyty granitowe placu CENTRUM", 1.2f, new Color(.66f, .65f, .63f), .85f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
    }

    // =====================================================================================================================
    // nawierzchnie (prostopadłościany z kolizją; górna ściana = nawierzchnia)
    static void Plyta(string n, Material m, float x0, float x1, float z0, float z1, float top, float grub = .3f, bool kolizja = true)
    {
        if (x1 - x0 < .01f || z1 - z0 < .01f) return;
        var b = new BraxRegionMeble.Bud(); b.MinMax(new Vector3(x0, top - grub, z0), new Vector3(x1, top, z1));
        b.Obiekt(n, root, m, false, false);
        if (kolizja) Nawierzchnia(BraxRegionMeble.Kolizja(root, n + " kolizja", new Vector3((x0 + x1) * .5f, top - grub * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, grub, z1 - z0)));
    }
    // ASFALT Z OZNAKOWANIEM W WYCIĘCIACH (zasada 15:40 „nigdy nic nie nakładamy”): jezdnie są tylko rejestrowane, a na końcu
    // (BudujAsfalt) powstaje górna powierzchnia asfaltu z OTWORAMI w miejscach farby i farba dokładnie w tych otworach, na tej
    // samej wysokości (styk krawędzią, bez dwóch płaszczyzn jedna na drugiej). Farba na ulicy głównej (asfalt A): asfalt
    // wycinany (BraxCityMeshCut) w kształcie pasa, farba w wycięciu.
    static readonly List<(string n, Rect r)> asfalty = new List<(string, Rect)>();
    static void Asfalt(string n, float x0, float x1, float z0, float z1) { if (x1 - x0 > .01f && z1 - z0 > .01f) asfalty.Add((n, Rect.MinMaxRect(x0, z0, x1, z1))); }

    sealed class Farba
    {
        public readonly List<Rect> r = new List<Rect>();
        public void MinMax(Vector3 a, Vector3 b) { var q = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)); if (q.width > .01f && q.height > .01f) r.Add(q); }
    }
    public static int MigotaniePary;

    static Rect Przetnij(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
    static bool Niepuste(Rect r) => r.width > .005f && r.height > .005f;

    // górna powierzchnia prostokąta z otworami (siatka komórek na krawędziach otworów), farba w otworach, boki, kolizja
    static void Wstawki(string n, Rect a, float y, List<Rect> otwory, Material top, Material paint, BraxRegionMeble.Bud bTop, BraxRegionMeble.Bud bPaint, float grub = .3f, bool kolizja = true)
    {
        var xs = new SortedSet<float> { a.xMin, a.xMax }; var zs = new SortedSet<float> { a.yMin, a.yMax };
        for (float x = a.xMin + 6f; x < a.xMax - .5f; x += 6f) xs.Add(x);
        for (float z = a.yMin + 6f; z < a.yMax - .5f; z += 6f) zs.Add(z);
        var h = otwory.Select(o => Przetnij(o, a)).Where(Niepuste).ToList();
        foreach (var o in h) { xs.Add(o.xMin); xs.Add(o.xMax); zs.Add(o.yMin); zs.Add(o.yMax); }
        var X = xs.ToArray(); var Z = zs.ToArray();
        for (int i = 0; i + 1 < X.Length; i++)
            for (int j = 0; j + 1 < Z.Length; j++)
            {
                if (X[i + 1] - X[i] < .002f || Z[j + 1] - Z[j] < .002f) continue;
                var c = new Vector2((X[i] + X[i + 1]) * .5f, (Z[j] + Z[j + 1]) * .5f);
                var b = h.Any(o => o.Contains(c)) ? bPaint : bTop;
                b.Quad(new Vector3(X[i], y, Z[j]), new Vector3(X[i], y, Z[j + 1]), new Vector3(X[i + 1], y, Z[j + 1]), new Vector3(X[i + 1], y, Z[j]));
            }
        // boki (do dołu warstwy) — tylko widoczne krawędzie (nie dublują sąsiednich płyt: boki są pionowe, sąsiad ma bok w tej samej
        // płaszczyźnie odwrócony — oba wewnątrz styku, niewidoczne)
        Vector3 P(float x, float yy, float z) => new Vector3(x, yy, z);
        bTop.Quad(P(a.xMin, y, a.yMin), P(a.xMax, y, a.yMin), P(a.xMax, y - grub, a.yMin), P(a.xMin, y - grub, a.yMin));
        bTop.Quad(P(a.xMax, y, a.yMax), P(a.xMin, y, a.yMax), P(a.xMin, y - grub, a.yMax), P(a.xMax, y - grub, a.yMax));
        bTop.Quad(P(a.xMin, y, a.yMax), P(a.xMin, y, a.yMin), P(a.xMin, y - grub, a.yMin), P(a.xMin, y - grub, a.yMax));
        bTop.Quad(P(a.xMax, y, a.yMin), P(a.xMax, y, a.yMax), P(a.xMax, y - grub, a.yMax), P(a.xMax, y - grub, a.yMin));
        if (kolizja) Nawierzchnia(BraxRegionMeble.Kolizja(root, n + " kolizja", new Vector3(a.center.x, y - grub * .5f, a.center.y), new Vector3(a.width, grub, a.height)));
    }

    static void BudujAsfalt(Farba f)
    {
        // a) jezdnie B z farbą w otworach
        int nA = 0;
        var top = new BraxRegionMeble.Bud(); var paint = new BraxRegionMeble.Bud();
        foreach (var (n, r) in asfalty)
        {
            Wstawki("Jezdnia: " + n, r, YA, f.r, asfalt, farba, top, paint);
            if (++nA % 40 == 0) { top.Obiekt("Jezdnie miasta B (asfalt)", root, asfalt, false, false); paint.Obiekt("Oznakowanie poziome (farba w wycięciach asfaltu)", root, farba, false, false); top = new BraxRegionMeble.Bud(); paint = new BraxRegionMeble.Bud(); }
        }
        top.Obiekt("Jezdnie miasta B (asfalt)", root, asfalt, false, false);
        paint.Obiekt("Oznakowanie poziome (farba w wycięciach asfaltu)", root, farba, false, false);
        // b) farba poza jezdniami B (ulica główna A: skrzyżowanie z Wiaduktową, zebra przy rondzie, osie) — wycięcie asfaltu A
        var reszta = new List<Rect>();
        foreach (var q in f.r)
        {
            // część pasa leżąca poza jezdniami B
            var w = new List<Rect> { q };
            foreach (var (_, r) in asfalty) w = w.SelectMany(x => Odejmij(x, r)).ToList();
            reszta.AddRange(w.Where(Niepuste));
        }
        Transform ulica = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t && t.name == "Street pavement and dry asphalt") { ulica = t; break; }
        var pA = new BraxRegionMeble.Bud(); int wyc = 0;
        foreach (var q in reszta)
        {
            if (q.yMin < 23.99f || q.yMax > 33.01f || q.xMin < -104.01f) { MigotaniePary++; Debug.LogWarning("BRAXHORN_MIASTO_B_MIGOTANIE farba poza jezdnią: " + q); continue; }
            if (ulica) wyc += BraxCityMeshCut.Subtract(ulica, Bx(q.xMin, q.xMax, -.06f, .06f, q.yMin, q.yMax), BraxRegion.Out + "/Cuts", "farba_ulica_glowna");
            pA.Quad(new Vector3(q.xMin, .003f, q.yMin), new Vector3(q.xMin, .003f, q.yMax), new Vector3(q.xMax, .003f, q.yMax), new Vector3(q.xMax, .003f, q.yMin));
        }
        pA.Obiekt("Oznakowanie ulicy głównej (farba w wycięciach asfaltu A)", root, farba, false, false);
        // c) kontrola: farba na farbie, asfalt na asfalcie (te same wysokości) — pary nachodzące
        for (int i = 0; i < asfalty.Count; i++) for (int j = i + 1; j < asfalty.Count; j++) if (Niepuste(Przetnij(asfalty[i].r, asfalty[j].r))) { MigotaniePary++; Debug.LogWarning("BRAXHORN_MIASTO_B_MIGOTANIE asfalt×asfalt " + asfalty[i].n + " × " + asfalty[j].n); }
        for (int i = 0; i < f.r.Count; i++) for (int j = i + 1; j < f.r.Count; j++) if (Niepuste(Przetnij(f.r[i], f.r[j]))) { MigotaniePary++; Debug.LogWarning("BRAXHORN_MIASTO_B_MIGOTANIE farba×farba " + f.r[i] + " × " + f.r[j]); }
        Debug.Log("BRAXHORN_MIASTO_B_OZNAKOWANIE jezdnie=" + asfalty.Count + " pasy_farby=" + f.r.Count + " na_ulicy_A=" + reszta.Count + " (wycięte siatki asfaltu A: " + wyc + ") — farba W WYCIĘCIACH, bez nakładania");
    }

    // czy punkt leży na jezdni miasta B (test ruchu aut): prostokąty asfaltu, pierścienie rond, ulica główna (A)
    public static bool NaJezdni(Vector3 p)
    {
        var q = new Vector2(p.x, p.z);
        // kwadraty placów ronda i pętli są zarejestrowane jako asfalt (pod chodnikiem placu) — jezdnią jest tylko pierścień i wloty
        foreach (var (n, r) in asfalty)
        {
            if (n == "rondo (pierścień i wloty)" || n == "pętla PÓŁNOC" || n == "pętla ZACHÓD") continue;
            var rr = r; rr.xMin -= .3f; rr.yMin -= .3f; rr.xMax += .3f; rr.yMax += .3f; if (rr.Contains(q)) return true;
        }
        if (Rect.MinMaxRect(-104.5f, 23.7f, 66f, 33.3f).Contains(q)) return true;
        foreach (var c in new[] { new Vector2(RX, RZ), new Vector2(AX, PetlaZ), new Vector2(PetlaZX, RZ) }) { float d = Vector2.Distance(q, c); if (d >= RWyspa - .3f && d <= RJezdnia + .3f) return true; }
        // wloty w kwadratach placów (od pierścienia do krawędzi placu, pół jezdni + 0.3 m)
        foreach (var (c, kat, hj) in WlotyPlacow())
        {
            var d = new Vector2(Mathf.Cos(kat * Mathf.Deg2Rad), Mathf.Sin(kat * Mathf.Deg2Rad)); var v = q - c;
            float wz = Vector2.Dot(v, d), wp = Mathf.Abs(d.x * v.y - d.y * v.x);
            if (wz >= RWyspa && wz <= RPlac + .3f && wp <= hj + .3f) return true;
        }
        return false;
    }

    static IEnumerable<(Vector2 c, float kat, float hj)> WlotyPlacow()
    {
        foreach (var k in WlotyRonda) yield return (new Vector2(RX, RZ), k, AH);
        yield return (new Vector2(AX, PetlaZ), 270f, AH);
        yield return (new Vector2(PetlaZX, RZ), 0f, AH);
        yield return (new Vector2(PetlaZX, RZ), 180f, AH);   // wlot zachodni (Aleja Centralna)
    }

    // a − b (prostokąty osiowe) → do 4 prostokątów
    static IEnumerable<Rect> Odejmij(Rect a, Rect b)
    {
        var p = Przetnij(a, b);
        if (!Niepuste(p)) { yield return a; yield break; }
        if (p.yMin > a.yMin) yield return Rect.MinMaxRect(a.xMin, a.yMin, a.xMax, p.yMin);
        if (p.yMax < a.yMax) yield return Rect.MinMaxRect(a.xMin, p.yMax, a.xMax, a.yMax);
        if (p.xMin > a.xMin) yield return Rect.MinMaxRect(a.xMin, p.yMin, p.xMin, p.yMax);
        if (p.xMax < a.xMax) yield return Rect.MinMaxRect(p.xMax, p.yMin, a.xMax, p.yMax);
    }
    static void Chodnik(string n, float x0, float x1, float z0, float z1) => Plyta("Chodnik: " + n, plyty, x0, x1, z0, z1, YC, .3f - YA);
    static void Kraweznik(float x0, float x1, float z0, float z1) => Plyta("Krawężnik", krawMat, x0, x1, z0, z1, YC, .3f - YA);
    // trawnik/chodnik na prostokącie bez wskazanych prostokątów (podział bez nakładania — Odejmij)
    static void TrawaBez(string n, Rect a, bool laka, params Rect[] bez)
    {
        var l = new List<Rect> { a };
        foreach (var b in bez) l = l.SelectMany(x => Odejmij(x, b)).ToList();
        foreach (var r in l) Trawa(n, r.xMin, r.xMax, r.yMin, r.yMax, laka);
    }
    static void ChodnikR(string n, Rect r) => Chodnik(n, r.xMin, r.xMax, r.yMin, r.yMax);
    static void Trawa(string n, float x0, float x1, float z0, float z1, bool laka = false)
    {
        if (x1 - x0 < .6f || z1 - z0 < .6f) return;
        Nawierzchnia(BraxRegionTrawa.Trawnik(root, n, Rect.MinMaxRect(x0, z0, x1, z1), YC - .03f, null, true, true, laka));
        // pod trawnikiem ziemia (bez lustra pod spodem, kolizja trawnika jest płaska)
        Plyta("Podłoże trawnika: " + n, beton, x0, x1, z0, z1, YC - .06f, .3f, false);
    }

    // przekrój ulicy wzdłuż osi z (stałe x = c): jezdnia, krawężniki, pasy zieleni, chodniki, od z0 do z1
    // zjazdy przez pas zieleni (strona s: −1 zach., +1 wsch.; zakres z): krawężnik obniżony (4 cm nad jezdnią), w pasie zieleni
    // rampa asfaltowa od krawężnika do poziomu chodnika (chodnik idzie dalej bez przerwy — auto wjeżdża na jego poziom)
    static readonly List<(int s, float a, float b)> zjazdy = new List<(int, float, float)>();
    static void PrzekrojZ(string n, float c, float hj, float ziel, float chod, float z0, float z1, bool drzewaTu, params (float a, float b)[] przerwyZieleni)
    {
        Asfalt(n, c - hj, c + hj, z0, z1);
        foreach (int s in new[] { -1, 1 })
        {
            var zj = zjazdy.Where(q => q.s == s && q.b > z0 && q.a < z1).OrderBy(q => q.a).ToList();
            float k0 = s < 0 ? c - hj - Kr : c + hj, k1 = s < 0 ? c - hj : c + hj + Kr;
            float kz = z0;
            foreach (var q in zj)
            {
                float a = Mathf.Max(z0, q.a), b = Mathf.Min(z1, q.b);
                Kraweznik(k0, k1, kz, a); Plyta("Krawężnik obniżony (zjazd)", krawMat, k0, k1, a, b, YA + .04f, .3f - YA); kz = b;
            }
            Kraweznik(k0, k1, kz, z1);
            float e0 = c + s * (hj + Kr), e1 = c + s * (hj + Kr + ziel), e2 = c + s * (hj + Kr + ziel + chod);
            float zx0 = Mathf.Min(e0, e1), zx1 = Mathf.Max(e0, e1), cx0 = Mathf.Min(e1, e2), cx1 = Mathf.Max(e1, e2);
            bool Przerwa(float z, float m) => przerwyZieleni.Any(p => z > p.a - m && z < p.b + m) || zj.Any(p => z > p.a - m && z < p.b + m);
            if (ziel > .1f)
            {
                // pas zieleni z przerwami (przejścia — chodnik; zjazdy — rampa)
                float cur = z0;
                foreach (var p in przerwyZieleni.Select(q => (q.a, q.b, zjazd: false)).Concat(zj.Select(q => (q.a, q.b, zjazd: true))).OrderBy(q => q.a))
                {
                    if (p.b <= z0 || p.a >= z1) continue;
                    Trawa(n + " pas zieleni", zx0, zx1, cur, Mathf.Max(cur, p.a));
                    if (p.zjazd) Rampa(n + " zjazd przez pas zieleni", zx0, zx1, Mathf.Max(z0, p.a), Mathf.Min(z1, p.b), s);
                    else Chodnik(n + " przejście przez pas zieleni", zx0, zx1, Mathf.Max(z0, p.a), Mathf.Min(z1, p.b));
                    cur = Mathf.Min(z1, p.b);
                }
                Trawa(n + " pas zieleni", zx0, zx1, cur, z1);
                if (drzewaTu)
                    for (float z = z0 + 6f; z < z1 - 3f; z += 13f)
                    {
                        if (Przerwa(z, 2.5f)) continue;
                        Drzewo(new Vector3((zx0 + zx1) * .5f, YC - .03f, z));
                    }
                for (float z = z0 + 12.5f; z < z1 - 3f; z += 26f)
                {
                    if (Przerwa(z, 1.5f)) continue;
                    Latarnia(new Vector3((zx0 + zx1) * .5f, YC - .03f, z), new Vector3(-s, 0, 0), hj * 1.0f + Kr + ziel * .5f);
                }
            }
            Chodnik(n, cx0, cx1, z0, z1);
        }
    }

    // rampa zjazdu w pasie zieleni: od obniżonego krawężnika (strona jezdni) do poziomu chodnika; kolizja = obrócona płyta
    static void Rampa(string n, float x0, float x1, float z0, float z1, int s)
    {
        float yL = YA + .04f, yH = YC, yb = -.15f;
        float ya = s > 0 ? yL : yH, yc = s > 0 ? yH : yL;   // wysokość przy x0 i przy x1 (niska strona przy jezdni)
        var b = new BraxRegionMeble.Bud();
        b.Quad(new Vector3(x0, ya, z0), new Vector3(x0, ya, z1), new Vector3(x1, yc, z1), new Vector3(x1, yc, z0));
        b.Quad(new Vector3(x0, ya, z0), new Vector3(x1, yc, z0), new Vector3(x1, yb, z0), new Vector3(x0, yb, z0));
        b.Quad(new Vector3(x1, yc, z1), new Vector3(x0, ya, z1), new Vector3(x0, yb, z1), new Vector3(x1, yb, z1));
        b.Obiekt("Zjazd: " + n, root, asfalt, false, false);
        float run = x1 - x0, rise = yc - ya, len = Mathf.Sqrt(run * run + rise * rise);
        var rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(rise, run) * Mathf.Rad2Deg);
        var top = new Vector3((x0 + x1) * .5f, (ya + yc) * .5f, (z0 + z1) * .5f);
        Nawierzchnia(BraxRegionMeble.Kolizja(root, "Zjazd kolizja: " + n, top - rot * Vector3.up * .15f, new Vector3(len, .3f, z1 - z0), rot));
    }

    // ETAP 2 — ul. Zachodnia: przekrój jak aleje (jezdnia 9 m, krawężniki, pasy zieleni z drzewami i latarniami, chodniki 5 m),
    // od placu ronda (x −148) do placu pętli ZACHÓD (x −218); pętla = rondo tej samej wielkości (zawracanie aut).
    static void UlicaZachodnia()
    {
        PrzekrojX2("ul. Zachodnia", RZ, AH, Ziel, Chod, ZachX0, ZachX1, true, (ZachX1 - 6f, ZachX1), (ZachX0, ZachX0 + 6f));
        var c = new Vector2(PetlaZX, RZ);
        Asfalt("pętla ZACHÓD", PetlaZX - PetlaPlac, PetlaZX + PetlaPlac, RZ - PetlaPlac, RZ + PetlaPlac);
        PlacZPierscieniem("pętla ZACHÓD", c, PetlaR, PetlaPlac, new[] { 0f, 180f }, AH);   // wlot zach. = Aleja Centralna (nowe dzielnice)
        var kraw = new BraxRegionMeble.Bud(); Pierscien(kraw, c, PetlaWyspa - Kr, PetlaWyspa, YC, 48); kraw.Obiekt("Pętla ZACHÓD: krawężnik wyspy", root, krawMat, false, false);
        var tr = Dysk("Pętla ZACHÓD: trawnik wyspy", c, PetlaWyspa - Kr, YC - .03f, 48); Nawierzchnia(tr); BraxRegionTrawa.Teren(tr.GetComponent<MeshRenderer>(), true, false);
        Plyta("Pętla ZACHÓD: podłoże wyspy", beton, PetlaZX - PetlaWyspa, PetlaZX + PetlaWyspa, RZ - PetlaWyspa, RZ + PetlaWyspa, YC - .06f, .3f, false);
        Drzewo(new Vector3(PetlaZX, YC - .03f, RZ));
    }

    static void PrzekrojX2(string n, float c, float hj, float ziel, float chod, float x0, float x1, bool drzewaTu, params (float a, float b)[] przerwy)
    {
        Asfalt(n, x0, x1, c - hj, c + hj);
        Kraweznik(x0, x1, c - hj - Kr, c - hj); Kraweznik(x0, x1, c + hj, c + hj + Kr);
        bool Przerwa(float x, float m) => przerwy.Any(p => x > p.a - m && x < p.b + m);
        foreach (int s in new[] { -1, 1 })
        {
            float e0 = c + s * (hj + Kr), e1 = c + s * (hj + Kr + ziel), e2 = c + s * (hj + Kr + ziel + chod);
            float zz0 = Mathf.Min(e0, e1), zz1 = Mathf.Max(e0, e1), cz0 = Mathf.Min(e1, e2), cz1 = Mathf.Max(e1, e2);
            float cur = x0;
            foreach (var p in przerwy.OrderBy(q => q.a))
            {
                if (p.b <= x0 || p.a >= x1) continue;
                Trawa(n + " pas zieleni", cur, Mathf.Max(cur, p.a), zz0, zz1);
                Chodnik(n + " przejście przez pas zieleni", Mathf.Max(x0, p.a), Mathf.Min(x1, p.b), zz0, zz1);
                cur = Mathf.Min(x1, p.b);
            }
            Trawa(n + " pas zieleni", cur, x1, zz0, zz1);
            if (drzewaTu)
                for (float x = x0 + 6f; x < x1 - 3f; x += 13f) { if (Przerwa(x, 2.5f)) continue; Drzewo(new Vector3(x, YC - .03f, (zz0 + zz1) * .5f)); }
            for (float x = x0 + 12.5f; x < x1 - 3f; x += 26f) { if (Przerwa(x, 1.5f)) continue; Latarnia(new Vector3(x, YC - .03f, (zz0 + zz1) * .5f), new Vector3(0, 0, -s), hj + Kr + ziel * .5f); }
            Chodnik(n, x0, x1, cz0, cz1);
        }
    }

    // przekrój ulicy wzdłuż osi x (stałe z = c)
    static void PrzekrojX(string n, float c, float hj, float chod, float x0, float x1, bool krawezniki = true)
    {
        Asfalt(n, x0, x1, c - hj, c + hj);
        if (krawezniki) { Kraweznik(x0, x1, c - hj - Kr, c - hj); Kraweznik(x0, x1, c + hj, c + hj + Kr); }
        if (chod > .05f) { Chodnik(n, x0, x1, c - hj - Kr - chod, c - hj - Kr); Chodnik(n, x0, x1, c + hj + Kr, c + hj + Kr + chod); }
    }

    // =====================================================================================================================
    // 1) RONDO: kwadrat placu ±RPlac, pierścień jezdni R 7.5…14.5, chodnik w narożnikach (wachlarze od narożnika kwadratu),
    //    krawężnik po łuku i wzdłuż wlotów, wyspa: krawężnik, trawnik, drzewo, rzeźba (stalowy łuk BRAXHORN).
    static void Rondo()
    {
        var c = new Vector2(RX, RZ);
        Asfalt("rondo (pierścień i wloty)", RX - RPlac, RX + RPlac, RZ - RPlac, RZ + RPlac);
        // wloty: E (ulica główna), N, S (aleje); W — ETAP 2 (do tego czasu chodnik przez cały zachód placu)
        PlacZPierscieniem("rondo", c, RJezdnia, RPlac, WlotyRonda, AH);
        // wyspa: krawężnik (pierścień 0.25 m, wys. 0.15), trawnik (dysk 3 cm niżej), drzewo i rzeźba
        var kraw = new BraxRegionMeble.Bud(); Pierscien(kraw, c, RWyspa - Kr, RWyspa, YC, 64);
        kraw.Obiekt("Rondo: krawężnik wyspy", root, krawMat, false, false);
        var trawa = Dysk("Rondo: trawnik wyspy", c, RWyspa - Kr, YC - .03f, 64); Nawierzchnia(trawa);
        BraxRegionTrawa.Teren(trawa.GetComponent<MeshRenderer>(), true, false);
        Plyta("Rondo: podłoże wyspy", beton, RX - RWyspa, RX + RWyspa, RZ - RWyspa, RZ + RWyspa, YC - .06f, .3f, false);
        Pozniej("rzeźba", () => Rzezba(NaPodlozu(new Vector3(RX, YC - .03f, RZ))));
        foreach (var a in new[] { 40f, 160f, 280f }) { var p = c + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 4.6f; Drzewo(new Vector3(p.x, YC - .03f, p.y)); }
        // latarnie na narożnikach placu
        foreach (var a in new[] { 45f, 135f, 225f, 315f })
        {
            var p = c + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * (RJezdnia + 1.4f);
            var dir = new Vector3(c.x - p.x, 0, c.y - p.y).normalized;
            Latarnia(new Vector3(p.x, YC, p.y), dir, 3.4f);
        }
    }

    // plac kwadratowy z pierścieniem jezdni: wachlarze chodnika między granicami (wloty: linie krawędzi jezdni ±(hj+Kr); bez
    // wlotu — podział w osi), krawężnik po łuku i wzdłuż wlotów.
    static void PlacZPierscieniem(string n, Vector2 c, float R, float S, float[] wloty, float hj)
    {
        float Ro = R + Kr;
        var granice = new List<(float kat, float off, bool wlot, int strona)>();   // strona: −1 = linia po prawej od osi, +1 po lewej
        foreach (var k in new[] { 0f, 90f, 180f, 270f })
        {
            bool w = wloty.Any(x => Mathf.Abs(Mathf.DeltaAngle(x, k)) < 1f);
            if (w) { granice.Add((k, -(hj + Kr), true, -1)); granice.Add((k, hj + Kr, true, 1)); }
            else granice.Add((k, 0f, false, 0));
        }
        // ćwiartki: od granicy „lewej” kąta k do granicy „prawej” kąta k+90
        var plyta = new BraxRegionMeble.Bud(); var kraw = new BraxRegionMeble.Bud(); var trawnik = new BraxRegionMeble.Bud();
        foreach (var k in new[] { 0f, 90f, 180f, 270f })
        {
            var gA = granice.Where(g => Mathf.Abs(Mathf.DeltaAngle(g.kat, k)) < 1f).OrderByDescending(g => g.off).First();      // lewa krawędź wlotu k (albo podział)
            var gB = granice.Where(g => Mathf.Abs(Mathf.DeltaAngle(g.kat, k + 90f)) < 1f).OrderBy(g => g.off).First();         // prawa krawędź wlotu k+90
            Vector2 dA = Kier(k), nA = Kier(k + 90f), dB = Kier(k + 90f), nB = Kier(k);   // nA, nB: od osi wlotu w stronę ćwiartki
            float oA = gA.off, oB = -gB.off;   // przesunięcie w stronę ćwiartki
            float tA = Mathf.Sqrt(Mathf.Max(0f, Ro * Ro - oA * oA)), tB = Mathf.Sqrt(Mathf.Max(0f, Ro * Ro - oB * oB));
            Vector2 A_out = c + dA * S + nA * oA, A_in = c + dA * tA + nA * oA;
            Vector2 B_in = c + dB * tB + nB * oB, B_out = c + dB * S + nB * oB;
            Vector2 rog = c + dA * S + nA * S;
            float k0 = Mathf.Atan2(A_in.y - c.y, A_in.x - c.x), k1 = Mathf.Atan2(B_in.y - c.y, B_in.x - c.x);
            while (k1 < k0) k1 += Mathf.PI * 2f;
            var obrys = new List<Vector2> { A_out, A_in };
            int segs = Mathf.Max(4, Mathf.CeilToInt((k1 - k0) / (2.5f * Mathf.Deg2Rad)));
            for (int i = 1; i < segs; i++) { float t = Mathf.Lerp(k0, k1, i / (float)segs); obrys.Add(c + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * Ro); }
            obrys.Add(B_in); obrys.Add(B_out);
            // WŁAŚCICIEL 3.11 10:58 („rondo okrągłe, ale za nim kwadrat”): zamiast kwadratu z płyt — chodnik 4.5 m po okręgu
            // (i 3 m wzdłuż wlotów), a narożnik placu trawnikiem (3 cm niżej, jak wyspa ronda). Pas chodnika: pasy czworokątów
            // między obrysem pierścienia a łukiem R + 4.5 m; trawnik: wachlarz od narożnika do łuku.
            {
                const float Pas = 4.5f, Wzdl = 3f;
                float oA2 = oA + (gA.wlot ? Wzdl : 0f), oB2 = oB + (gB.wlot ? Wzdl : 0f), Rb = Ro + Pas;
                float tA2 = Mathf.Sqrt(Mathf.Max(0f, Rb * Rb - oA2 * oA2)), tB2 = Mathf.Sqrt(Mathf.Max(0f, Rb * Rb - oB2 * oB2));
                Vector2 A_c = c + dA * Mathf.Min(tA2, S) + nA * oA2, B_c = c + dB * Mathf.Min(tB2, S) + nB * oB2;
                Vector2 A_o2 = c + dA * S + nA * oA2, B_o2 = c + dB * S + nB * oB2;
                float q0 = Mathf.Atan2(A_c.y - c.y, A_c.x - c.x), q1 = Mathf.Atan2(B_c.y - c.y, B_c.x - c.x);
                while (q1 < q0) q1 += Mathf.PI * 2f;
                var zew = new List<Vector2> { A_o2, A_c };
                for (int i = 1; i < segs; i++) { float t = Mathf.Lerp(q0, q1, i / (float)segs); zew.Add(c + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * Rb); }
                zew.Add(B_c); zew.Add(B_o2);
                for (int i = 0; i + 1 < obrys.Count; i++)
                {
                    TrojkatGora(plyta, zew[i], obrys[i], obrys[i + 1], YC);          // ściana od jezdni (krawędź obrysu)
                    TrojkatGora(plyta, obrys[i + 1], zew[i + 1], zew[i], YC);        // ściana od trawnika (krawędź łuku)
                }
                for (int i = 0; i + 1 < zew.Count; i++) TrojkatTrawa(trawnik, rog, zew[i], zew[i + 1], YC - .03f);
            }
            // krawężnik: łuk R…R+Kr i (dla wlotów) linie wzdłuż jezdni wlotu od pierścienia do brzegu placu
            for (int i = 0; i < segs; i++)
            {
                float t0 = Mathf.Lerp(k0, k1, i / (float)segs), t1 = Mathf.Lerp(k0, k1, (i + 1) / (float)segs);
                Vector2 u0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)), u1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
                KlinGora(kraw, c + u0 * R, c + u1 * R, c + u1 * Ro, c + u0 * Ro, YC);
            }
            if (gA.wlot) { float tr = Mathf.Sqrt(Mathf.Max(0f, R * R - (Mathf.Abs(oA) - Kr) * (Mathf.Abs(oA) - Kr))); KlinGora(kraw, c + dA * tr + nA * (oA - Mathf.Sign(oA) * Kr), c + dA * S + nA * (oA - Mathf.Sign(oA) * Kr), c + dA * S + nA * oA, c + dA * tA + nA * oA, YC); }
            if (gB.wlot) { float tr = Mathf.Sqrt(Mathf.Max(0f, R * R - (Mathf.Abs(oB) - Kr) * (Mathf.Abs(oB) - Kr))); KlinGora(kraw, c + dB * tB + nB * oB, c + dB * S + nB * oB, c + dB * S + nB * (oB - Mathf.Sign(oB) * Kr), c + dB * tr + nB * (oB - Mathf.Sign(oB) * Kr), YC); }
        }
        Nawierzchnia(plyta.Obiekt("Plac: chodnik " + n, root, plyty, true, false));
        { var gt = trawnik.Obiekt("Plac: trawnik narożników " + n, root, BraxRegionTrawa.Material, true, false); if (gt) { foreach (var r in gt.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off; Nawierzchnia(gt); } }
        Nawierzchnia(kraw.Obiekt("Plac: krawężnik " + n, root, krawMat, true, false));
    }

    static Vector2 Kier(float stopnie) => new Vector2(Mathf.Cos(stopnie * Mathf.Deg2Rad), Mathf.Sin(stopnie * Mathf.Deg2Rad));

    // pryzmat: trójkąt (góra y, dół y−0.3), ściany boczne tylko po krawędziach (a→b), normalna góry w górę
    static void TrojkatGora(BraxRegionMeble.Bud b, Vector2 a, Vector2 p, Vector2 q, float y = YC)
    {
        if (Vector2.Distance(p, q) < 1e-4f || Vector2.Distance(a, p) < 1e-4f || Vector2.Distance(a, q) < 1e-4f) return;
        var A = new Vector3(a.x, y, a.y); var P = new Vector3(p.x, y, p.y); var Q = new Vector3(q.x, y, q.y);
        if (Vector3.Cross(P - A, Q - A).y < 0) { var t = P; P = Q; Q = t; }
        b.Tri(A, P, Q);   // Unity: przód trójkąta = Cross(P−A, Q−A) (tu y > 0 — normalna w górę)
        // ściana boczna krawędzi zewnętrznej p–q (krawężnik pierścienia jej nie zasłania od strony jezdni — jest ścianą chodnika)
        var P0 = new Vector3(P.x, y - .3f, P.z); var Q0 = new Vector3(Q.x, y - .3f, Q.z);
        b.Quad(P, Q, Q0, P0); b.Quad(Q, P, P0, Q0);
    }
    // trójkąt trawnika: sama powierzchnia (normalna w górę), bez ścian
    static void TrojkatTrawa(BraxRegionMeble.Bud b, Vector2 a, Vector2 p, Vector2 q, float y)
    {
        if (Vector2.Distance(p, q) < 1e-4f || Vector2.Distance(a, p) < 1e-4f || Vector2.Distance(a, q) < 1e-4f) return;
        var A = new Vector3(a.x, y, a.y); var P = new Vector3(p.x, y, p.y); var Q = new Vector3(q.x, y, q.y);
        if (Vector3.Cross(P - A, Q - A).y < 0) { var t = P; P = Q; Q = t; }
        b.Tri(A, P, Q);
    }
    static void KlinGora(BraxRegionMeble.Bud b, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float y)
    {
        Vector3 V(Vector2 v, float yy) => new Vector3(v.x, yy, v.y);
        var a = V(p0, y); var bb = V(p1, y); var c = V(p2, y); var d = V(p3, y);
        if (Vector3.Cross(bb - a, c - a).y < 0) { b.Quad(a, d, c, bb); } else b.Quad(a, bb, c, d);
        // boki (od jezdni i od chodnika) do −0.3
        foreach (var (u, w) in new[] { (p0, p1), (p2, p3) }) { var U = V(u, y); var W = V(w, y); var U0 = V(u, y - .3f); var W0 = V(w, y - .3f); b.Quad(U, W, W0, U0); b.Quad(W, U, U0, W0); }
    }
    static void Pierscien(BraxRegionMeble.Bud b, Vector2 c, float r0, float r1, float y, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float t0 = i * Mathf.PI * 2f / n, t1 = (i + 1) * Mathf.PI * 2f / n;
            Vector2 u0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)), u1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
            KlinGora(b, c + u0 * r0, c + u1 * r0, c + u1 * r1, c + u0 * r1, y);
        }
    }
    static GameObject Dysk(string n, Vector2 c, float r, float y, int seg)
    {
        var v = new List<Vector3> { new Vector3(c.x, y, c.y) }; var t = new List<int>(); var uv = new List<Vector2> { c };
        for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; v.Add(new Vector3(p.x, y, p.y)); uv.Add(p); }
        for (int i = 1; i <= seg; i++) { t.Add(0); t.Add(i + 1); t.Add(i); }
        var m = new Mesh { name = n }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
        var g = new GameObject(n); g.transform.SetParent(root, false);
        g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = BraxRegionTrawa.Material; mr.shadowCastingMode = ShadowCastingMode.Off;
        g.AddComponent<MeshCollider>().sharedMesh = m;
        return g;
    }

    // rzeźba na wyspie: stalowy łuk-brama z napisem BRAXHORN na kamiennym cokole (× k), bez świecenia
    static void Rzezba(Vector3 p)
    {
        var g = new GameObject("Rondo: rzeźba „BRAXHORN”"); g.transform.SetParent(root, false);
        var kam = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud();
        kam.MinMax(p + new Vector3(-1.6f * K, 0, -1.0f * K), p + new Vector3(1.6f * K, .7f * K, 1.0f * K));
        for (int i = 0; i < 24; i++)
        {
            float a0 = Mathf.PI * i / 24f, a1 = Mathf.PI * (i + 1) / 24f; float r = 2.4f * K;
            var q0 = p + new Vector3(Mathf.Cos(a0) * r * .55f, .7f * K + Mathf.Sin(a0) * r, 0); var q1 = p + new Vector3(Mathf.Cos(a1) * r * .55f, .7f * K + Mathf.Sin(a1) * r, 0);
            st.Cyl(q0, q1, .16f * K, 10);
        }
        kam.Obiekt("Rzeźba: cokół granitowy", g.transform, granit, false, true);
        st.Obiekt("Rzeźba: łuk stal corten", g.transform, BraxCityMaterials.Surface("Miasto B: stal corten", new Color(.42f, .22f, .12f), .7f, .5f), false, true);
        BraxRegionMeble.Kolizja(g.transform, "Rzeźba kolizja", p + Vector3.up * 1.6f * K, new Vector3(3.2f * K, 3.2f * K, 2f * K));
        BraxRegionMeble.Napis(g.transform, "BRAXHORN", p + new Vector3(0, .38f * K, 1.0f * K + .02f), Vector3.forward, .34f * K, new Color(.92f, .9f, .85f));
        BraxRegionMeble.Napis(g.transform, "BRAXHORN", p + new Vector3(0, .38f * K, -1.0f * K - .02f), Vector3.back, .34f * K, new Color(.92f, .9f, .85f));
    }

    // =====================================================================================================================
    // 2) aleje
    static float LicoW => AX - AH - Kr - Ziel - Chod;   // −138: lico pierzei zachodniej
    static float LicoE => AX + AH + Kr + Ziel + Chod;   // −114: lico pierzei wschodniej

    static void AlejaPolnocna()
    {
        float z0 = RZ + RPlac;                // 50.5
        float z1 = PetlaZ - PetlaPlac;        // 95
        PrzekrojZ("Aleja Północna", AX, AH, Ziel, Chod, z0, z1, true, (z0, z0 + 6f), (z1 - 6f, z1));
        // WŁAŚCICIEL 7.11: zamiast ronda PÓŁNOC przy SUPERMARKET
        // BRAXHORN (sklep #1 x=-120,z=132) — prawdziwy parking.
        // Rondo z drzewem i jego kolizjami NIE powstaje.
        // Jedyny poziom asfaltu; pasy malowane później przez Wstawki, nie nałożone.
        Asfalt("PARKING SUPERMARKET BRAXHORN (dawna pętla PÓŁNOC)",
            AX - PetlaPlac, AX + PetlaPlac,
            PetlaZ - PetlaPlac, PetlaZ + PetlaPlac);
        // Główna aleja manewrowa po osi x=-126 (szer. 12m), od jezdni
        // do pasa pieszego przy witrynie. Samochody nie wjeżdżają na chodnik.
        float szer = 2.5f * K, dl = 5f * K;
        for (int i = 0; i < 9; i++)
        {
            float z = PetlaZ - 16f + i * 3.5f;
            MiejscaParkingu.Add((new Vector3(AX - 10.0f, YC, z),
                Vector3.right, new Vector2(szer, dl)));
            MiejscaParkingu.Add((new Vector3(AX + 10.0f, YC, z),
                Vector3.left, new Vector2(szer, dl)));
        }
        // Prosty, szeroki chodnik z kostki od parkingu pod wejście sklepu.
        // Styk krawędzią z północnym końcem asfaltu, bez nowej powierzchni na starej.
        Chodnik("SUPERMARKET BRAXHORN: dojście od parkingu",
            AX - PetlaPlac, AX + PetlaPlac + 2f,
            PetlaZ + PetlaPlac, PetlaZ + PetlaPlac + 5.5f);
        // Obie boczne krawędzie parkingu: chodnik dla pieszych.
        Chodnik("SUPERMARKET BRAXHORN: chodnik parkingu W",
            AX - PetlaPlac - 3f, AX - PetlaPlac,
            PetlaZ - PetlaPlac, PetlaZ + PetlaPlac);
        Chodnik("SUPERMARKET BRAXHORN: chodnik parkingu E",
            AX + PetlaPlac, AX + PetlaPlac + 3f,
            PetlaZ - PetlaPlac, PetlaZ + PetlaPlac);
        Debug.Log("BRAXHORN_SUPERMARKET_PARKING RONDO_POLNOC_USUNIETE=tak" +
            " centrum=" + AX + "," + PetlaZ + " miejsca=" + 18 +
            " alejka_12m=tak dojscie_sklep=tak glówne_rondo_miasta_zostaje=tak");
    }

    static void AlejaPoludniowa()
    {
        float z1 = RZ - RPlac;    // 6.5
        float zW0 = SZ - SH - Kr - 2.5f, zW1 = SZ + SH + Kr + 2.5f;   // wylot ul. Warsztatowej z chodnikami: −24.75 … −13.25
        // od ronda do Warsztatowej (z −13.25 … 6.5) i od Warsztatowej do tunelu (z −84 … −24.75)
        PrzekrojZ("Aleja Południowa", AX, AH, Ziel, Chod, zW1, z1, true, (z1 - 6f, z1), (zW1, zW1 + 4.5f));
        Asfalt("tunel", AX - 5.6f, AX + 5.6f, AS1 - 20f, AS1);   // jezdnia w tunelu (portal i ściany: etap 2)
        zjazdy.Add((1, ParkAleja0, ParkAleja1));   // wjazd na parking przy warsztatach
        PrzekrojZ("Aleja Południowa (do tunelu)", AX, AH, Ziel, Chod, AS1, zW0, true, (zW0 - 6f, zW0), (AS1, AS1 + 14f), (PrzystanekZ - 10f, PrzystanekZ + 6f));
        // skrzyżowanie z Warsztatową: jezdnia alei przez wylot, krawężniki po zachodniej stronie, po wschodniej wylot ulicy
        Asfalt("Aleja Południowa × Warsztatowa", AX - AH, AX + AH, zW0, zW1);
        Kraweznik(AX - AH - Kr, AX - AH, zW0, zW1);
        Trawa("Aleja Płd. pas zieleni zach. przy Warsztatowej", AX - AH - Kr - Ziel, AX - AH - Kr, zW0 + 6f, zW1 - 6f);
        Chodnik("Aleja Płd. przejście zach. (pd.)", AX - AH - Kr - Ziel, AX - AH - Kr, zW0, zW0 + 6f);
        Chodnik("Aleja Płd. przejście zach. (pn.)", AX - AH - Kr - Ziel, AX - AH - Kr, zW1 - 6f, zW1);
        Chodnik("Aleja Płd. zach. przy Warsztatowej", LicoW, AX - AH - Kr - Ziel, zW0, zW1);
        // narożniki wschodnie (chodnik alei przechodzi w chodniki Warsztatowej)
        Asfalt("wylot Warsztatowej", AX + AH, AX + AH + Kr + Ziel + Chod, SZ - SH, SZ + SH);
        foreach (int s in new[] { -1, 1 })
        {
            float zk0 = s < 0 ? SZ - SH - Kr : SZ + SH, zk1 = s < 0 ? SZ - SH : SZ + SH + Kr;
            Kraweznik(AX + AH, LicoE, zk0, zk1);
            float zc0 = s < 0 ? zW0 : SZ + SH + Kr, zc1 = s < 0 ? SZ - SH - Kr : zW1;
            Chodnik("Aleja Płd. narożnik wsch.", AX + AH + Kr, LicoE, zc0, zc1);
            Kraweznik(AX + AH, AX + AH + Kr, zc0, zc1);
        }
    }

    // =====================================================================================================================
    // 3) ul. Wiaduktowa i ul. Warsztatowa
    static void UliceBoczne()
    {
        float zN = 23.875f - .12f;   // lico krawężnika płd. ulicy głównej (kerb .24 na 23.875)
        // Wiaduktowa: jezdnia x −69.2…−63.2, z (Warsztatowa pn. krawędź jezdni) … do jezdni ulicy głównej (z 24)
        Asfalt("ul. Wiaduktowa", WX - WH, WX + WH, SZ + SH, 24f);
        Kraweznik(WX - WH - Kr, WX - WH, SZ + SH + Kr, 24f - .005f); Kraweznik(WX + WH, WX + WH + Kr, SZ + SH + Kr, 24f - .005f);
        Chodnik("ul. Wiaduktowa zach.", -70.95f, WX - WH - Kr, SZ + SH + Kr + 2.5f, 20.55f);
        Chodnik("ul. Wiaduktowa wsch.", WX + WH + Kr, -61.45f, SZ + SH + Kr + 2.5f, 20.55f);
        // chodnik ulicy głównej po obu stronach wylotu zostaje (oryginał A); jego wycięta krawędź = mój krawężnik Wiaduktowej
        // Warsztatowa: jezdnia z −22…−16 od alei (x −121.5 = AX+AH) do x −61.4 (koniec przy placu wieżowca B, słupki)
        float xa = LicoE;   // od lica pierzei wsch. alei (chodnik alei kończy się na LicoE)
        Asfalt("ul. Warsztatowa", xa, SX1, SZ - SH, SZ + SH);
        Kraweznik(xa, WX - WH - Kr, SZ + SH, SZ + SH + Kr); Kraweznik(WX + WH + Kr, SX1 + Kr, SZ + SH, SZ + SH + Kr);
        Kraweznik(xa, SX1 + Kr, SZ - SH - Kr, SZ - SH);
        Chodnik("ul. Warsztatowa pd.", xa, SX1 + Kr, SZ - SH - Kr - 2.5f, SZ - SH - Kr);
        Chodnik("ul. Warsztatowa pn.", xa, WX - WH - Kr, SZ + SH + Kr, SZ + SH + Kr + 2.5f);
        Chodnik("ul. Warsztatowa pn. (wsch.)", WX + WH + Kr, SX1 + Kr, SZ + SH + Kr, SZ + SH + Kr + 2.5f);
        // koniec Warsztatowej: krawężnik poprzeczny i plac z granitu do placu wieżowców (x −61.4 … −58)
        Kraweznik(SX1, SX1 + Kr, SZ - SH, SZ + SH);   // krawężnik poprzeczny na końcu jezdni; dalej plac wieżowców (BraxRegionTylyB, od x −61.15)
        Pozniej("słupki", () =>
        {
            var sl = new BraxRegionMeble.Bud();
            for (float z = SZ - SH + .6f; z <= SZ + SH - .5f; z += 1.5f) BraxRegionMeble.Slupek(root, sl, NaPodlozu(new Vector3(SX1 + 1.0f, YC, z)));
            sl.Obiekt("Słupki blokujące wjazd na plac", root, stal, false, true);
        });
    }

    // =====================================================================================================================
    // 4) teren między ulicami (bez lustra): trawniki, place, skwer na dawnym terenie pod wiaduktem
    static void Teren()
    {
        // trawniki NIE nachodzą na siebie ani na nawierzchnie (każdy prostokąt osobno, styk krawędzią)
        float zW0 = SZ - SH - Kr - 2.5f, zW1 = SZ + SH + Kr + 2.5f, zb = zW0 - 13.5f;
        // zachód (ETAP 2: ul. Zachodnia, pętla ZACHÓD, komisariat, szpital, pierzeja płn.) — prostokąty bez nakładania
        float zL = RZ - 12f, zP = RZ + 12f, wx0 = NX, pzx0 = PetlaZX - PetlaPlac;   // trawniki B do krawędzi nowych dzielnic (x −262)
        Trawa("L1 za pierzeją zach. Alei Północnej", -156f, LicoW - 16f, RZ + RPlac, PetlaZ - PetlaPlac);
        TrawaBez("L2 na zachód od pętli", Rect.MinMaxRect(-156f, PetlaZ - PetlaPlac, AX - PetlaPlac - 3f, 126f), true, PrzejscieCentrumPetla);
        TrawaBez("Z-a za komisariatem i szpitalem (do skarpy)", Rect.MinMaxRect(wx0, ZboczeZ, LicoW - 16f, Komisariat.yMin), true, Galeria, PrzedpoleGalerii);
        TrawaBez("Z-b pod pętlą ZACHÓD", Rect.MinMaxRect(wx0, Komisariat.yMin, Komisariat.xMin, RZ - PetlaPlac), false, DojscieGalerii);
        ChodnikR("przejście ul. Zachodnia → galeria", PrzejscieDoGalerii);
        ChodnikR("przedpole galerii", PrzedpoleGalerii); ChodnikR("dojście do galerii od pętli ZACHÓD", DojscieGalerii);
        Trawa("Z-d między komisariatem a szpitalem", Komisariat.xMax, Szpital.xMin, Komisariat.yMin, zL);
        Trawa("Z-e między szpitalem a pierzeją alei", Szpital.xMax, LicoW - 16f, Szpital.yMin, RZ - RPlac);
        Chodnik("narożnik pd.-zach. ronda (przed fryzjerem)", Szpital.xMax, ZachX1, RZ - RPlac, zL);
        Trawa("Z-g na zachód od pętli", wx0, pzx0, RZ - PetlaPlac, RZ + PetlaPlac);
        ChodnikR("przejście ul. Zachodnia → plac CENTRUM", PrzejscieDoCentrum);
        Chodnik("narożnik pn.-zach. ronda (przed apteką)", -156f, ZachX1, zP, RZ + RPlac);
        TrawaBez("Z-j na północ od pętli", Rect.MinMaxRect(wx0, RZ + PetlaPlac, ZachX0 + 4f, 126f), true, PlacCentrum, PrzejscieDoCentrum);
        TrawaBez("Z-k za pierzeją płn. ul. Zachodniej", Rect.MinMaxRect(ZachX0 + 4f, zP + 16f, -156f, 126f), true, PlacCentrum);
        ChodnikR("przejście aleja A1 → pętla PÓŁNOC", PrzejscieCentrumPetla);
        // plac CENTRUM: płyty wokół wieżowców (bez płyt pod budynkami — posadzki holi leżą w poziomie placu)
        foreach (var r in WiezeCentrum.Aggregate(new List<Rect> { PlacCentrum }, (l, w) => l.SelectMany(x => Odejmij(x, w)).ToList()))
            Plyta("Plac CENTRUM: płyty", granitPlac, r.xMin, r.xMax, r.yMin, r.yMax, YC, .3f - YA);
        for (float x = wx0 + 12f; x < -160f; x += 15f) { Drzewo(new Vector3(x, YC - .03f, -48f)); Drzewo(new Vector3(x + 7f, YC - .03f, -60f)); Drzewo(new Vector3(x, YC - .03f, -74f)); }
        for (float x = -184f; x < -158f; x += 9f) Drzewo(new Vector3(x, YC - .03f, -18f));   // na wschód od galerii
        // aleje placu CENTRUM: drzewa w kratach, latarnie, ławki (wyposażenie — etap 3)
        foreach (float zA in new[] { 89f, 118f })
            for (float x = -256f; x < -158f; x += 12f)
            {
                float xx = x, za = zA;
                Pozniej("drzewo w kracie (CENTRUM)", () => { var p = NaPodlozu(new Vector3(xx, YC, za)); BraxRegionMeble.KrataDrzewa(root, p); DrzewoTeraz(p); });
                Pozniej("latarnia (CENTRUM)", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(xx + 6f, YC, za))); latarnie++; });
                // R1: ławki na ciągu ulicznym CENTRUM usunięte.
            }
        Trawa("L3 na wschód od pętli", AX + PetlaPlac + 3f, LicoE + 16f, PetlaZ - PetlaPlac, 126f, true);
        Trawa("L4 na północ od pętli", AX - PetlaPlac - 3f, AX + PetlaPlac + 3f, PetlaZ + PetlaPlac, 126f, true);
        Trawa("L7 park zach. Alei Południowej", LicoW - 16f, LicoW, ZboczeZ, -14f, true);
        for (float z = AS1 + 6f; z < -20f; z += 11f) Drzewo(new Vector3(LicoW - 7f + ((int)z % 3), YC - .03f, z));
        Trawa("L8 park wsch. Alei Południowej (pd.)", LicoE, ParkX0, ZboczeZ, ParkAleja0);
        Trawa("L8 park wsch. Alei Południowej (pn.)", LicoE, ParkX0, ParkAleja1, zW0);
        Trawa("L9 za warsztatami (do skarpy)", -112f, -61.4f, ZboczeZ, ParkZ0, true);
        Trawa("L9 za warsztatami (przy parkingu)", -112f, -61.4f, ParkZ1, zb, true);
        Trawa("L9 na wschód od parkingu", ParkX1, -61.4f, ParkZ0, ParkZ1, true);
        Trawa("L10 między warsztatami a placem B", -72f, -61.4f, zb, zW0);
        for (float x = -106f; x < -66f; x += 9f) Drzewo(new Vector3(x, YC - .03f, zb - 6f));
        // między ul. Warsztatową a pierzeją płd. ulicy głównej: plac ustawek (w Warsztaty), trawnik za pierzeją, skwerek przy Wiaduktowej
        Trawa("L11 za pierzeją ulicy głównej", -104f, -70.95f, 2.5f, 11.3f);
        Trawa("L12 przy Wiaduktowej", -78f, -70.95f, zW1, 2.5f);
        Drzewo(new Vector3(-74.5f, YC - .03f, -6f));
        // narożnik pn.-wsch. ronda między pierzeją A (estate_apartments_2) a pierzeją wsch. alei
        Chodnik("narożnik pn.-wsch. ronda", RX + RPlac, LicoE + 16f, 48.3f, RZ + RPlac);
        // skwer na północ od pierzei ulicy głównej (dawny teren pod wiaduktem): trawnik z alejką, drzewa, ławki, latarnie
        Trawa("L13 skwer zach.", LicoE + 16f, -70.95f, 48.3f, 126f);
        Chodnik("skwer: alejka wzdłuż osi dawnego wiaduktu", -70.95f, -61.45f, 36.2f, 126f);
        Trawa("L14 skwer wsch.", -61.45f, -48f, 48.3f, 126f);
        for (float z = 54f; z < 120f; z += 12f) { Drzewo(new Vector3(-75f, YC - .03f, z)); Drzewo(new Vector3(-57f, YC - .03f, z + 6f)); }
        for (float z = 60f; z < 118f; z += 24f)
        {
            float zz = z;
            // Ławki wzdłuż ulicy usunięte. Kosze zachowane.
            Pozniej("kosz", () => BraxRegionMeble.Kosz(root, NaPodlozu(new Vector3(-70.95f - .3f * K, YC - .03f, zz + 2.2f * K)), 90f));
        }
        for (float z = 66f; z < 118f; z += 26f) { float zz = z; Pozniej("latarnia parkowa", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(-61.45f - .5f, YC, zz))); latarnie++; }); }
    }

    // parking przy warsztatach: asfalt na poziomie chodnika z liniami stanowisk W WYCIĘCIACH (zasada 15:40), wjazd od chodnika
    // alei (pas L8), stanowiska do MiejscaParkingu (auta zaparkowane — krok ruchu, na końcu budowy); wyposażenie w etapie 3
    static void Parking()
    {
        float w = 2.5f * K, d = 5f * K, xs = ParkX0 + .6f; const int N = 12;
        var linie = new List<Rect>();
        foreach (var (zA, zB) in new[] { (ParkAleja1, ParkAleja1 + d), (ParkAleja0 - d, ParkAleja0) })
            for (int i = 0; i <= N; i++) { float x = xs + i * w; linie.Add(Rect.MinMaxRect(x - .06f, zA + (zA == ParkAleja1 ? .1f : 0f), x + .06f, zB - (zB == ParkAleja0 ? .1f : 0f))); }
        var top = new BraxRegionMeble.Bud(); var paint = new BraxRegionMeble.Bud();
        Wstawki("Parking przy warsztatach", Rect.MinMaxRect(ParkX0, ParkZ0, ParkX1, ParkZ1), YC, linie, asfalt, farba, top, paint);
        Wstawki("Parking: wjazd od alei", Rect.MinMaxRect(LicoE, ParkAleja0, ParkX0, ParkAleja1), YC, new List<Rect>(), asfalt, farba, top, paint);
        top.Obiekt("Parking przy warsztatach: asfalt", root, asfalt, false, false);
        paint.Obiekt("Parking przy warsztatach: linie stanowisk (farba w wycięciach)", root, farba, false, false);
        for (int i = 0; i < N; i++)
        {
            float x = xs + (i + .5f) * w;
            MiejscaParkingu.Add((new Vector3(x, YC, ParkAleja1 + d * .5f), Vector3.forward, new Vector2(w, d)));
            MiejscaParkingu.Add((new Vector3(x, YC, ParkAleja0 - d * .5f), Vector3.back, new Vector2(w, d)));
        }
        // wyposażenie (na gotowym podłożu): latarnie między drzewami, znak P przy wjeździe, kosz
        foreach (float x in new[] { -101.5f, -83.5f })
        {
            float xx = x;
            Pozniej("latarnia parkingu", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(xx, YC, ParkZ1 + .7f))); latarnie++; });
            Pozniej("latarnia parkingu", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(xx, YC, ParkZ0 - .7f))); latarnie++; });
        }
        Pozniej("znak P", () => ZnakParking(NaPodlozu(new Vector3(LicoE + 1f, YC, ParkAleja0 - .8f))));
        Pozniej("kosz parkingu", () => BraxRegionMeble.Kosz(root, NaPodlozu(new Vector3(ParkX0 - .8f, YC, ParkAleja1 + .8f)), 0f));
        BraxRegion.Note("miasto B: parking przy warsztatach — stanowisk " + MiejscaParkingu.Count + " (" + w.ToString("F2") + " × " + d.ToString("F2") + " m), wjazd z Alei Południowej z " + ParkAleja0 + "…" + ParkAleja1);
    }

    // znak D-18 „parking” (niebieska tablica, białe P) na słupku ocynkowanym, lico do jadących aleją na północ (−z → widać od południa)
    static void ZnakParking(Vector3 p)
    {
        if (BraxZnakiPL.Slup(root, "Znak D-18 PARKING (wzór urzędowy)", p, Vector3.back, "D-18")) return;
        Debug.LogWarning("DO POPRAWKI: BRAXHORN_ZNAKI_PL brak wzoru D-18 — znak parkingu z kodu");
        var g = new GameObject("Znak D-18 PARKING"); g.transform.SetParent(root, false);
        var slup = new BraxRegionMeble.Bud(); slup.Cyl(p, p + Vector3.up * 2.6f * K, .03f * K, 10);
        slup.Obiekt("Znak: słupek", g.transform, stal, false, true);
        var t = new BraxRegionMeble.Bud(); var c = p + Vector3.up * 2.25f * K;
        t.MinMax(c + new Vector3(-.3f * K, -.3f * K, -.035f), c + new Vector3(.3f * K, .3f * K, -.015f));
        t.Obiekt("Znak: tablica (niebieska)", g.transform, BraxCityMaterials.Surface("Znak: niebieski odblask", new Color(.05f, .25f, .62f), .35f), false, false);
        BraxRegionMeble.Napis(g.transform, "P", c + new Vector3(0f, -.17f * K, -.04f), Vector3.back, .4f * K, Color.white);
        BraxRegionMeble.Napis(g.transform, "WARSZTATY", c + new Vector3(0f, -.42f * K, -.04f), Vector3.back, .07f * K, Color.white);
        BraxRegionMeble.Kolizja(g.transform, "Znak kolizja", p + Vector3.up * 1.3f * K, new Vector3(.1f, 2.6f * K, .1f));
    }

    // =====================================================================================================================
    // 5) pierzeje: pierwszy kwartał obu alej
    static void Pierzeje()
    {
        var p = new GameObject("CLAUDE miasto B obszar: pierzeje alej").transform; p.SetParent(root, false);
        float w0 = LicoW - 16f, w1 = LicoW, e0 = LicoE, e1 = LicoE + 16f;
        float zW1 = SZ + SH + Kr + 2.5f;
        var cz = BraxRegionKamienica.CeglaCzerwona(); var cc = BraxRegionKamienica.CeglaCiemna(); var cj = BraxRegionKamienica.CeglaJasna();
        var ty = BraxRegionKamienica.Tynk(); var ka = BraxRegionKamienica.Kamien();
        // Aleja Północna — zachód (lico x −138, front na +x = strona 1)
        Rzad(p, new[] {
            S("Aleja Północna 2 (narożnik ronda)", w0, w1, RZ + RPlac, 67f, 7, cz, 1, "APTEKA", "2", schody: true, frontPd: true),
            S("Aleja Północna 4 (narożnik pętli)", w0, w1, 67f, PetlaZ - PetlaPlac, 6, ty, 1, "PIEKARNIA", "4", frontPn: true),
        });
        // Aleja Północna — wschód (lico x −114, front na −x = strona 3)
        Rzad(p, new[] {
            S("Aleja Północna 1 (narożnik ronda)", e0, e1, RZ + RPlac, 66f, 6, ka, 3, "BANK", "1", frontPd: true),
            S("Aleja Północna 3 (narożnik pętli)", e0, e1, 66f, PetlaZ - PetlaPlac, 7, cj, 3, "KAWIARNIA", "3", schody: true, frontPn: true),
        });
        // ul. Zachodnia — północ (lico z 40.5, front na −z = strona 2), od pętli do narożnika ronda
        RzadX(p, new[] {
            S("ul. Zachodnia 5", ZachX0 + 4f, -196f, RZ + 12f, RZ + 28f, 5, ty, 2, "KWIACIARNIA", "5"),
            S("ul. Zachodnia 3", -196f, -176f, RZ + 12f, RZ + 28f, 6, cc, 2, "PIZZERIA", "3", schody: true),
            S("ul. Zachodnia 1 (przy rondzie)", -176f, -156f, RZ + 12f, RZ + 28f, 7, ka, 2, "SKLEP SPOŻYWCZY", "1"),
        });
        // Aleja Południowa — zachód (narożnik ronda)
        Rzad(p, new[] { S("Aleja Południowa 2 (narożnik ronda)", w0, w1, -14f, RZ - RPlac, 6, cj, 1, "FRYZJER", "2", frontPn: true, schody: true) });
        // Aleja Południowa — wschód: narożnik ronda i ul. Warsztatowej
        Rzad(p, new[] { S("Aleja Południowa 1 (narożnik Warsztatowej)", e0, -104f, zW1, RZ - RPlac, 5, cc, 3, "KEBAB", "1", frontPn: true, frontPd: true) });
    }

    static BraxRegionKamienica.Segment S(string nazwa, float x0, float x1, float z0, float z1, int pietra, BraxRegionKamienica.Styl styl, int front, string szyld, string numer,
                                          bool schody = false, bool frontPn = false, bool frontPd = false)
    {
        var s = new BraxRegionKamienica.Segment { nazwa = nazwa, x0 = x0, x1 = x1, z0 = z0, z1 = z1, pietra = pietra, styl = styl, numer = numer, schodyPozarowe = schody, ziarno = nazwa.Length * 131 + pietra };
        s.sklepy[front] = true; s.ulica[front] = true; s.szyldy[front] = szyld;
        if (frontPn) { s.sklepy[0] = true; s.ulica[0] = true; s.szyldy[0] = szyld; }
        if (frontPd) { s.sklepy[2] = true; s.ulica[2] = true; s.szyldy[2] = szyld; }
        return s;
    }

    // rząd budynków wzdłuż z: ściana wspólna (ślepa, bez okien) u OBU sąsiadów — każda w swoim budynku (bez nakładania),
    // nad niższym sąsiadem widać ślepą ścianę wyższego (jak w prawdziwej pierzei)
    static void Rzad(Transform p, BraxRegionKamienica.Segment[] rz)
    {
        for (int i = 0; i + 1 < rz.Length; i++)
            if (Mathf.Abs(rz[i].z1 - rz[i + 1].z0) < .05f) { rz[i].sciepa[0] = true; rz[i + 1].sciepa[2] = true; rz[i].sklepy[0] = rz[i + 1].sklepy[2] = false; }
        foreach (var s in rz) BraxRegionKamienica.Budynek(p, s);
    }

    // rząd budynków wzdłuż x (ulica wzdłuż x): ściany wspólne po stronach ±x (1 / 3), jak Rzad
    static void RzadX(Transform p, BraxRegionKamienica.Segment[] rz)
    {
        for (int i = 0; i + 1 < rz.Length; i++)
            if (Mathf.Abs(rz[i].x1 - rz[i + 1].x0) < .05f) { rz[i].sciepa[1] = true; rz[i + 1].sciepa[3] = true; rz[i].sklepy[1] = rz[i + 1].sklepy[3] = false; }
        foreach (var s in rz) BraxRegionKamienica.Budynek(p, s);
    }

    // =====================================================================================================================
    // 6) warsztaty (przeniesione spod łuków) — pierzeja płd. ul. Warsztatowej; jeden OTWARTY (AUTO NAPRAWA); plac ustawek
    static void Warsztaty()
    {
        float zf = SZ - SH - Kr - 2.5f;   // lico (−24.75)
        float zb = zf - 13.5f;            // tył
        string[] nazwy = { "WULKANIZACJA", "AUTO NAPRAWA", "GARAŻ 12", "MAGAZYN" };
        var w = new GameObject("Warsztaty ul. Warsztatowej").transform; w.SetParent(root, false);
        var sc = new BraxRegionMeble.Bud(); var bet = new BraxRegionMeble.Bud(); var met = new BraxRegionMeble.Bud(); var bramy = new BraxRegionMeble.Bud(); var szk = new BraxRegionMeble.Bud(); var pos = new BraxRegionMeble.Bud();
        float h = 5.2f * K, x0 = -112f;
        var szyba = BraxCityMaterials.Surface("Warsztat: szyba (jasna, z odbiciem)", new Color(.45f, .53f, .57f), .15f, .1f);
        for (int i = 0; i < 4; i++)
        {
            float a = x0 + i * 10f, b = a + 10f, m = (a + b) * .5f;
            bool otwarty = i == 1;
            // ściany: tył, boki (wspólne — jedna na granicy), front z bramą 3.6 × 3.6 m × k
            sc.MinMax(new Vector3(a, YC, zb), new Vector3(b, YC + h, zb + .35f));
            if (i == 0) sc.MinMax(new Vector3(a, YC, zb), new Vector3(a + .35f, YC + h, zf));
            sc.MinMax(new Vector3(b - .35f, YC, zb), new Vector3(b, YC + h, zf));
            float bw = 3.6f * K, bh = 3.6f * K;
            sc.MinMax(new Vector3(a + (i == 0 ? .35f : 0f), YC, zf - .35f), new Vector3(m - bw * .5f, YC + h, zf));
            sc.MinMax(new Vector3(m + bw * .5f, YC, zf - .35f), new Vector3(b - .35f, YC + h, zf));
            sc.MinMax(new Vector3(m - bw * .5f, YC + bh, zf - .35f), new Vector3(m + bw * .5f, YC + h, zf));
            // okno obok bramy (szyba w głębi 0.2)
            // dach + attyka
            bet.MinMax(new Vector3(a, YC + h, zb), new Vector3(b, YC + h + .3f, zf + .3f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja tył", new Vector3(m, YC + h * .5f, zb + .17f), new Vector3(10f, h, .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja front L", new Vector3((a + m - bw * .5f) * .5f, YC + h * .5f, zf - .17f), new Vector3(m - bw * .5f - a, h, .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja front P", new Vector3((m + bw * .5f + b) * .5f, YC + h * .5f, zf - .17f), new Vector3(b - m - bw * .5f, h, .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja bok", new Vector3(b - .17f, YC + h * .5f, (zb + zf) * .5f), new Vector3(.35f, h, zf - zb));
            if (i == 0) BraxRegionMeble.Kolizja(w, "Warsztat kolizja bok", new Vector3(a + .17f, YC + h * .5f, (zb + zf) * .5f), new Vector3(.35f, h, zf - zb));
            // posadzka (beton gładki, 5 cm nad chodnikiem — próg skośny niepotrzebny: 5 cm)
            pos.MinMax(new Vector3(a + (i == 0 ? .35f : 0f), YC - .25f, zb + .35f), new Vector3(b - .35f, YC + .05f, zf - .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat posadzka", new Vector3(m, YC - .1f, (zb + zf) * .5f), new Vector3(10f, .3f, zf - zb));
            // szyld (kaseton nad bramą) — litery 3D
            met.MinMax(new Vector3(m - 3.2f * K, YC + bh + .35f * K, zf), new Vector3(m + 3.2f * K, YC + bh + 1.15f * K, zf + .18f));
            BraxRegionMeble.Napis(w, nazwy[i], new Vector3(m, YC + bh + .75f * K, zf + .2f), Vector3.forward, .5f * K, new Color(.95f, .93f, .86f));
            BraxRegionMeble.Napis(w, "ul. Warsztatowa " + (2 * i + 2), new Vector3(b - 1.0f * K, YC + 2.6f * K, zf + .05f), Vector3.forward, .12f * K, Color.white);
            if (!otwarty)
            {
                // brama rolowana zamknięta (lamele co 10 cm × k), skrzynka rolety
                for (float y = YC + .05f; y < YC + bh - .05f; y += .1f * K) bramy.MinMax(new Vector3(m - bw * .5f, y, zf - .3f), new Vector3(m + bw * .5f, y + .085f * K, zf - .24f));
                met.MinMax(new Vector3(m - bw * .5f - .1f, YC + bh - .02f, zf - .34f), new Vector3(m + bw * .5f + .1f, YC + bh + .3f * K, zf - .05f));
                BraxRegionMeble.Kolizja(w, "Brama zamknięta", new Vector3(m, YC + bh * .5f, zf - .27f), new Vector3(bw, bh, .1f));
            }
            else
            {
                // OTWARTY: brama podniesiona (skrzynka), wnętrze: ściany tynk, oprawy, podnośnik 2-kolumnowy, stół warsztatowy,
                // szafka narzędziowa, regał z oponami, kompresor, plamy oleju? (nie — czysto)
                met.MinMax(new Vector3(m - bw * .5f - .1f, YC + bh - .02f, zf - .34f), new Vector3(m + bw * .5f + .1f, YC + bh + .3f * K, zf - .05f));
                var tynk = BraxRegionMeble.Tex("Warsztat: tynk wnętrza", 1.5f, new Color(.86f, .86f, .84f), .9f, "white_plaster_02", "white_stucco_02");
                var wn = new BraxRegionMeble.Bud();
                wn.MinMax(new Vector3(a + .37f, YC + .05f, zb + .37f), new Vector3(b - .37f, YC + h - .05f, zb + .39f));
                wn.MinMax(new Vector3(a + .02f, YC + .05f, zb + .37f), new Vector3(a + .04f, YC + h - .05f, zf - .37f));
                wn.MinMax(new Vector3(b - .39f, YC + .05f, zb + .37f), new Vector3(b - .37f, YC + h - .05f, zf - .37f));
                wn.MinMax(new Vector3(a + .04f, YC + h - .07f, zb + .37f), new Vector3(b - .39f, YC + h - .05f, zf - .37f));
                wn.Obiekt("Warsztat otwarty: tynk wnętrza", w, tynk, false, false);
                Podnosnik(w, new Vector3(m, YC + .05f, (zb + zf) * .5f - .5f));
                var stol = new BraxRegionMeble.Bud();
                stol.MinMax(new Vector3(a + .5f, YC + .05f + .9f * K, zb + .45f), new Vector3(a + .5f + 2.2f * K, YC + .05f + .95f * K, zb + .45f + .75f * K));
                foreach (float dx in new[] { .05f, 2.1f }) foreach (float dz in new[] { .05f, .68f }) stol.MinMax(new Vector3(a + .5f + dx * K, YC + .05f, zb + .45f + dz * K), new Vector3(a + .5f + (dx + .06f) * K, YC + .05f + .9f * K, zb + .45f + (dz + .06f) * K));
                stol.Obiekt("Warsztat: stół warsztatowy", w, BraxCityMaterials.Surface("Warsztat: blat stal ocynk", new Color(.55f, .57f, .58f), .45f, .7f), false, true);
                BraxRegionMeble.Kolizja(w, "Stół kolizja", new Vector3(a + .5f + 1.1f * K, YC + .5f * K, zb + .45f + .38f * K), new Vector3(2.2f * K, .95f * K, .75f * K));
                var szafa = new BraxRegionMeble.Bud();
                szafa.MinMax(new Vector3(b - .45f - .7f * K, YC + .05f, zb + .45f), new Vector3(b - .45f, YC + .05f + 1.8f * K, zb + .45f + .5f * K));
                szafa.Obiekt("Warsztat: szafa narzędziowa (czerwona)", w, BraxCityMaterials.Surface("Warsztat: lakier czerwony", new Color(.62f, .08f, .06f), .4f, .3f), false, true);
                BraxRegionMeble.Kolizja(w, "Szafa kolizja", new Vector3(b - .45f - .35f * K, YC + .9f * K, zb + .45f + .25f * K), new Vector3(.7f * K, 1.8f * K, .5f * K));
                // regał z oponami
                var opony = new BraxRegionMeble.Bud(); var regal = new BraxRegionMeble.Bud();
                for (int r = 0; r < 3; r++)
                {
                    float y = YC + .05f + (.1f + r * .62f) * K;
                    regal.MinMax(new Vector3(b - .45f - 1.9f * K, y - .04f, zf - 1.2f * K), new Vector3(b - .45f, y, zf - .5f));
                    for (int q = 0; q < 3; q++) { var c = new Vector3(b - .45f - (.35f + q * .6f) * K, y + .33f * K, zf - .9f * K); opony.Cyl(c - Vector3.forward * .11f * K, c + Vector3.forward * .11f * K, .32f * K, 18); }
                }
                regal.Obiekt("Warsztat: regał", w, BraxCityMaterials.Surface("Warsztat: regał stal niebieska", new Color(.12f, .22f, .45f), .45f, .5f), false, true);
                opony.Obiekt("Warsztat: opony", w, BraxCityMaterials.Surface("Warsztat: guma opon", new Color(.05f, .05f, .05f), .85f), false, true);
                BraxRegionMeble.Kolizja(w, "Regał kolizja", new Vector3(b - .45f - .95f * K, YC + 1f * K, zf - .9f * K), new Vector3(1.9f * K, 2f * K, .8f * K));
                foreach (float dx in new[] { -2.5f, 2.5f }) { BraxRegion.Warm(w, "Warsztat: oprawa", new Vector3(m + dx, YC + h - .4f, (zb + zf) * .5f), 1.6f, 9f); }
                var opr = new BraxRegionMeble.Bud(); foreach (float dx in new[] { -2.5f, 2.5f }) opr.MinMax(new Vector3(m + dx - .7f * K, YC + h - .2f, (zb + zf) * .5f - .1f * K), new Vector3(m + dx + .7f * K, YC + h - .12f, (zb + zf) * .5f + .1f * K));
                opr.Obiekt("Warsztat: oprawy LED", w, BraxRegionMeble.Swiecacy("Warsztat: oprawa LED", new Color(.95f, .95f, .9f), .6f), false, false);
                world_point("R1_WARSZTAT_AUTO_NAPRAWA", "AUTO NAPRAWA (otwarty warsztat)", new Vector3(m, YC + .05f, zf - 2f));
            }
        }
        sc.Obiekt("Warsztaty: cegła", w, cegla, false, true);
        bet.Obiekt("Warsztaty: dach i attyka", w, beton, false, true);
        met.Obiekt("Warsztaty: stal (szyldy, skrzynki rolet)", w, BraxCityMaterials.Surface("Warsztat: stal grafit", new Color(.12f, .13f, .14f), .5f, .6f), false, true);
        bramy.Obiekt("Warsztaty: bramy rolowane", w, BraxCityMaterials.Surface("Warsztat: brama rolowana (ocynk lakier)", new Color(.62f, .64f, .66f), .45f, .6f), false, true);
        pos.Obiekt("Warsztaty: posadzka", w, beton, false, false);
        // PLAC USTAWEK 3 na 3: x −104…−78, z −13.25…2.5 (≥ 20 × 12 × k = 25.9 × 15.5), asfalt z liniami boiska, ławki kibiców dookoła
        float px0 = -104f, px1 = -78f, pz0 = SZ + SH + Kr + 2.5f, pz1 = 2.5f;
        // nawierzchnia asfaltowa placu z liniami obwiedni W WYCIĘCIACH (bez farby na asfalcie)
        var linie = new List<Rect> { Rect.MinMaxRect(px0 + 1f, pz0 + 1f, px1 - 1f, pz0 + 1.08f), Rect.MinMaxRect(px0 + 1f, pz1 - 1.08f, px1 - 1f, pz1 - 1f),
                                     Rect.MinMaxRect(px0 + 1f, pz0 + 1f, px0 + 1.08f, pz1 - 1f), Rect.MinMaxRect(px1 - 1.08f, pz0 + 1f, px1 - 1f, pz1 - 1f) };
        var pt = new BraxRegionMeble.Bud(); var pl = new BraxRegionMeble.Bud();
        Wstawki("Plac ustawek", Rect.MinMaxRect(px0, pz0, px1, pz1), YC, linie, asfalt, farba, pt, pl, .3f - YA);   // nawierzchnia przed ławkami (ławki: etap 3)
        pt.Obiekt("Plac ustawek: nawierzchnia (asfalt)", root, asfalt, false, false);
        pl.Obiekt("Plac ustawek: linie (farba w wycięciach)", root, farba, false, false);
        for (float x = px0 + 3f; x < px1 - 2f; x += 6.5f) { float xx = x; Pozniej("ławka kibiców", () => BraxRegionMeble.Lawka(root, NaPodlozu(new Vector3(xx, YC, pz1 + .1f + .3f * K)), 180f, "Ławka kibiców")); }
        world_point("R1_PLAC_USTAWEK", "plac ustawek 3 na 3", new Vector3((px0 + px1) * .5f, YC, (pz0 + pz1) * .5f));
    }

    static BraxCityWorld swiat;
    static void world_point(string id, string label, Vector3 p)
    {
        if (!swiat) swiat = UnityEngine.Object.FindFirstObjectByType<BraxCityWorld>();
        if (!swiat) return;
        swiat.points = swiat.points.Where(q => q.id != id).Concat(new[] { new BraxCityPoint { id = id, label = label, position = p } }).ToArray();
    }

    // podnośnik dwukolumnowy 4 t (kolumny 3.0 m × k, ramiona), z kolizją kolumn
    static void Podnosnik(Transform w, Vector3 p)
    {
        var b = new BraxRegionMeble.Bud(); var r = new BraxRegionMeble.Bud();
        foreach (float s in new[] { -1f, 1f })
        {
            var c = p + new Vector3(s * 1.55f * K, 0, 0);
            b.MinMax(c + new Vector3(-.2f * K, 0, -.25f * K), c + new Vector3(.2f * K, 3.0f * K, .25f * K));
            b.MinMax(c + new Vector3(-.3f * K, 0, -.35f * K), c + new Vector3(.3f * K, .05f, .35f * K));
            foreach (float dz in new[] { -1f, 1f }) r.Box(c + new Vector3(-s * .55f * K, .25f * K, dz * .45f * K), new Vector3(1.0f * K, .08f * K, .12f * K), Quaternion.Euler(0, dz * s * 20f, 0));
            BraxRegionMeble.Kolizja(w, "Podnośnik kolumna", c + Vector3.up * 1.5f * K, new Vector3(.4f * K, 3f * K, .5f * K));
        }
        b.MinMax(p + new Vector3(-1.75f * K, 3.0f * K, -.12f * K), p + new Vector3(1.75f * K, 3.12f * K, .12f * K));
        b.Obiekt("Podnośnik: kolumny (niebieski lakier)", w, BraxCityMaterials.Surface("Podnośnik: lakier niebieski", new Color(.1f, .25f, .55f), .4f, .4f), false, true);
        r.Obiekt("Podnośnik: ramiona", w, BraxCityMaterials.Surface("Podnośnik: ramiona stal", new Color(.25f, .26f, .27f), .45f, .6f), false, true);
    }

    // =====================================================================================================================
    // 7) przystanek RONDO (Aleja Płd., chodnik wsch., za skrzyżowaniem z Warsztatową; autobus jedzie na PÓŁNOC prawym = wsch. pasem,
    //    staje przodem przy znaczniku 5 m przed linią zatrzymania — cały poza skrzyżowaniem i zebrą; pas zieleni przerwany = peron)
    public const float PrzystanekZ = -42f;
    static void Przystanek()
    {
        float x = AX + AH + Kr + Ziel + 1.2f * K, z = PrzystanekZ;
        var g = new GameObject("Przystanek RONDO (wiata)").transform; g.SetParent(root, false);
        var st = new BraxRegionMeble.Bud(); var sz = new BraxRegionMeble.Bud(); var dach = new BraxRegionMeble.Bud();
        float L = 4.2f * K, D = 1.5f * K, H = 2.5f * K;
        foreach (float dz in new[] { -L * .5f, L * .5f }) foreach (float dx in new[] { 0f, D }) st.Cyl(new Vector3(x + dx, YC, z + dz), new Vector3(x + dx, YC + H, z + dz), .04f * K, 10);
        dach.MinMax(new Vector3(x - .1f, YC + H, z - L * .5f - .1f), new Vector3(x + D + .2f, YC + H + .08f * K, z + L * .5f + .1f));
        sz.MinMax(new Vector3(x + D - .01f, YC + .12f * K, z - L * .5f + .05f), new Vector3(x + D + .01f, YC + H - .1f, z + L * .5f - .05f));   // tylna szyba
        sz.MinMax(new Vector3(x + .05f, YC + .12f * K, z - L * .5f - .01f), new Vector3(x + D - .05f, YC + H - .1f, z - L * .5f + .01f));     // boczna
        st.Obiekt("Przystanek: stelaż", g, stal, false, true);
        dach.Obiekt("Przystanek: dach", g, stal, false, true);
        sz.Obiekt("Przystanek: szyby", g, BraxCityMaterials.Surface("Przystanek: szkło hartowane", new Color(.74f, .82f, .84f, .28f), .08f, .05f), false, false);
        BraxRegionMeble.Kolizja(g, "Przystanek: kolizja szyby tył", new Vector3(x + D, YC + H * .5f, z), new Vector3(.08f, H, L));
        // R1: ławka przy ulicznym przystanku usunięta, wiata i pozostałe wyposażenie zostają.
        // słupek D-15 z nazwą i rozkładem
        var sl = new BraxRegionMeble.Bud(); var p0 = new Vector3(x - .3f, YC, z + L * .5f + 1.2f);
        sl.Cyl(p0, p0 + Vector3.up * 2.9f * K, .035f * K, 10);
        sl.Obiekt("Przystanek: słupek D-15", g, stal, false, true);
        var tab = new BraxRegionMeble.Bud(); tab.MinMax(p0 + new Vector3(-.03f, 2.3f * K, -.3f * K), p0 + new Vector3(.03f, 2.9f * K, .3f * K));
        tab.Obiekt("Przystanek: znak D-15", g, BraxCityMaterials.Surface("Znak: żółty D-15", new Color(.95f, .78f, .08f), .4f), false, false);
        BraxRegionMeble.Napis(g, "RONDO", p0 + new Vector3(-.05f, 2.55f * K, 0), Vector3.left, .14f * K, new Color(.05f, .05f, .05f));
        BraxRegionMeble.Kolizja(g, "Słupek kolizja", p0 + Vector3.up * 1.4f * K, new Vector3(.12f, 2.9f * K, .12f));
        var m = new GameObject("BRAXHORN przystanek autobusowy: RONDO"); m.transform.SetParent(root, false);
        m.transform.position = new Vector3(AX + AH, YA, z + L * .5f + 1.5f); m.transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }

    // =====================================================================================================================
    // 8) wyjazd na osiedle: znak kierunku, szlaban z budką, portal tunelu w zboczu (wzgórze = teren za granicą, portal tutaj)
    static void SzlabanIZnak()
    {
        Physics.SyncTransforms();
        var g = new GameObject("Wyjazd na osiedle: szlaban i znak OSIEDLE").transform; g.SetParent(root, false);
        // znak drogowskazowy E-2 (niebieski) „OSIEDLE ↓” na chodniku wsch. przy z −58
        var sl = new BraxRegionMeble.Bud(); var p0 = NaPodlozu(new Vector3(AX + AH + Kr + .6f, YC, -58f));
        sl.Cyl(p0, p0 + Vector3.up * 3.2f * K, .04f * K, 10);
        sl.Obiekt("Znak OSIEDLE: słup", g, stal, false, true);
        var tb = new BraxRegionMeble.Bud(); tb.MinMax(p0 + new Vector3(-1.5f * K, 2.3f * K, -.03f), p0 + new Vector3(.2f * K, 3.1f * K, .03f));
        tb.Obiekt("Znak OSIEDLE: tablica", g, BraxCityMaterials.Surface("Znak: niebieski E-2", new Color(.04f, .2f, .55f), .35f), false, false);
        BraxRegionMeble.Napis(g, "OSIEDLE  ↓  TUNEL", p0 + new Vector3(-.65f * K, 2.7f * K, .05f), Vector3.forward, .2f * K, Color.white);
        BraxRegionMeble.Kolizja(g, "Znak kolizja", p0 + Vector3.up * 1.6f * K, new Vector3(.12f, 3.2f * K, .12f));
        // pkt 42 (właściciel): szlaban „WJAZD ZAMKNIĘTY” w poprzek jezdni = błąd — usunięty razem z budką i kolizją; droga do tunelu
        // otwarta. Za portalem tunel długi i oświetlony; bez osiedla w aplikacji BraxTunelZawrot zawraca gracza z komunikatem.
    }

    static void WyjazdNaOsiedle()
    {
        var g = new GameObject("Wyjazd na osiedle: portal tunelu i zbocze").transform; g.SetParent(root, false);
        // portal tunelu (lico z = AS1): kamienny mur z łukiem, nadproże, czarne wnętrze 18 m z jezdnią, ściany zbocza
        var kam = BraxRegionMeble.Tex("Tunel: kamień portalu", 1.2f, new Color(.78f, .76f, .72f), .9f, "rough_block_wall", "white_sandstone_blocks_02", "rock_boulder_dry");
        var mur = new BraxRegionMeble.Bud();
        float zp = AS1, W = 12f, Hs = 7.2f;
        mur.MinMax(new Vector3(AX - 22f, -.5f, zp - 1.4f), new Vector3(AX - W * .5f, 13f, zp));
        mur.MinMax(new Vector3(AX + W * .5f, -.5f, zp - 1.4f), new Vector3(AX + 22f, 13f, zp));
        mur.MinMax(new Vector3(AX - W * .5f, Hs, zp - 1.4f), new Vector3(AX + W * .5f, 13f, zp));
        mur.MinMax(new Vector3(AX - 22.4f, 13f, zp - 1.6f), new Vector3(AX + 22.4f, 13.5f, zp + .2f));   // gzyms portalu
        mur.Obiekt("Tunel: portal kamienny", g, kam, false, true);
        BraxRegionMeble.Kolizja(g, "Portal kolizja L", new Vector3(AX - 14f, 6f, zp - .7f), new Vector3(16f, 13f, 1.4f));
        BraxRegionMeble.Kolizja(g, "Portal kolizja P", new Vector3(AX + 14f, 6f, zp - .7f), new Vector3(16f, 13f, 1.4f));
        // pkt 42: bez czarnej ściany — wnętrze z betonu (jasne lico), 62 m z oprawami co 4 m, jezdnia za dawnym końcem (z −20…−62),
        // chodniki techniczne z krawężnikiem, odboje; koniec daleko (zawrót gracza przy 6 m: BraxTunelZawrot)
        var beton = BraxRegionMeble.Tex("Tunel: beton ścian (płyty)", 2.4f, new Color(.62f, .61f, .58f), .85f, "concrete_wall_008", "concrete_wall_004", "rough_block_wall");
        float Lt = 62f;
        var tun = new BraxRegionMeble.Bud();
        tun.MinMax(new Vector3(AX - W * .5f, -.3f, zp - Lt), new Vector3(AX - W * .5f + .4f, Hs, zp - 1.4f));
        tun.MinMax(new Vector3(AX + W * .5f - .4f, -.3f, zp - Lt), new Vector3(AX + W * .5f, Hs, zp - 1.4f));
        tun.MinMax(new Vector3(AX - W * .5f, Hs, zp - Lt), new Vector3(AX + W * .5f, Hs + .4f, zp - 1.4f));
        tun.MinMax(new Vector3(AX - W * .5f, -.3f, zp - Lt - .4f), new Vector3(AX + W * .5f, Hs, zp - Lt));
        tun.Obiekt("Tunel: wnętrze", g, beton, false, false);
        var jezd = new BraxRegionMeble.Bud(); jezd.MinMax(new Vector3(AX - W * .5f + .4f, YA - .3f, zp - Lt), new Vector3(AX + W * .5f - .4f, YA, zp - 20f));
        jezd.Obiekt("Tunel: jezdnia (za dawnym końcem)", g, BraxCityMaterials.Surface("Tunel: asfalt", new Color(.16f, .16f, .17f), .9f), false, true);
        var odb = new BraxRegionMeble.Bud();
        foreach (float sx in new[] { -1f, 1f })
        {
            float xw = AX + sx * (W * .5f - .4f);
            odb.MinMax(new Vector3(Mathf.Min(xw, xw - sx * .9f), YA, zp - Lt), new Vector3(Mathf.Max(xw, xw - sx * .9f), YA + .2f, zp - 20f));   // chodnik techniczny
            odb.MinMax(new Vector3(Mathf.Min(xw, xw - sx * .06f), YA + .2f, zp - Lt), new Vector3(Mathf.Max(xw, xw - sx * .06f), YA + .55f, zp - 1.4f));   // odbój przy ścianie
        }
        odb.Obiekt("Tunel: chodniki techniczne i odboje", g, BraxRegionMeble.Tex("Tunel: beton chodnika", 1.2f, new Color(.7f, .69f, .66f), .9f, "concrete_floor_02", "concrete_wall_004"), false, true);
        BraxRegionMeble.Kolizja(g, "Tunel kolizja koniec", new Vector3(AX, 3f, zp - Lt + .3f), new Vector3(W, 6f, .6f));
        var zaw = new GameObject("Tunel: zawrót bez osiedla (BraxTunelZawrot)").AddComponent<BraxTunelZawrot>(); zaw.transform.SetParent(g, false);
        zaw.portal = new Vector3(AX, YA, zp); zaw.polSzer = W * .5f - .2f; zaw.glebokosc = 6f; zaw.dlugosc = Lt; zaw.powrot = new Vector3(AX, YA + .14f, zp + 3.5f); zaw.yawPowrotu = 180f;
        var opr = new BraxRegionMeble.Bud(); for (float z = zp - 3f; z > zp - Lt + 1f; z -= 4f) opr.MinMax(new Vector3(AX - .6f, Hs - .12f, z - .4f), new Vector3(AX + .6f, Hs - .02f, z + .4f));
        opr.Obiekt("Tunel: oprawy", g, BraxRegionMeble.Swiecacy("Tunel: oprawa sodowa", new Color(1f, .78f, .45f), .9f), false, false);
        BraxRegionMeble.Napis(g, "TUNEL · OSIEDLE", new Vector3(AX, Hs + 2.6f, zp + .05f), Vector3.forward, .6f * K, new Color(.92f, .9f, .85f));
        // zbocze nad portalem: siatka terenu (łąka) od z = AS1 w głąb, do 26 m, szer. ±60 m — zakrywa koniec (dalej teren horyzontu)
        Zbocze(g, zp);
    }

    // zbocze nad tunelem: siatka terenu od linii z = AS1 − 1.45 (tył muru portalu) w głąb na płd.; przy portalu stromo (mur
    // oporowy portalu je zakrywa), poza portalem łagodnie (skarpa z łąką) — dalej teren horyzontu (BraxMiastoGranice.WzgorzeTylow)
    public static float ZboczeZ => AS1 - 1.45f;
    static void Zbocze(Transform g, float zp)
    {
        int nx = 60, nz = 30; float x0 = AX - 80f, x1 = AX + 66f, z1 = ZboczeZ, z0 = z1 - 72f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++)
        {
            float x = Mathf.Lerp(x0, x1, i / (float)nx), z = Mathf.Lerp(z0, z1, j / (float)nz);
            float dz = z1 - z;
            bool portal = Mathf.Abs(x - AX) < 22f;
            float ramp = portal ? Mathf.Clamp01((dz + 3f) / 5f) : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dz / 18f));
            float boki = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - x0) / 30f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x1 - x) / 30f));   // zbocze schodzi do łąki na końcach
            float h = (YC - .03f) + (13.5f + 10f * Mathf.Clamp01(dz / 60f) + 3f * (Mathf.PerlinNoise(x * .05f, z * .05f) - .5f)) * ramp * (portal ? 1f : boki);
            v.Add(new Vector3(x, h, z)); uv.Add(new Vector2(x * .2f, z * .2f));
        }
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) { int a = i * (nz + 1) + j, b = a + 1, c = a + nz + 1, d = c + 1; t.AddRange(new[] { a, b, c, b, d, c }); }
        var m = new Mesh { name = "Zbocze nad tunelem" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
        // normalne w górę (kolejność wierzchołków liczona z siatki x/z — sprawdzenie i odwrócenie, gdy w dół)
        if (m.normals.Length > 0 && m.normals[m.normals.Length / 2].y < 0) { var tt = m.triangles; for (int q = 0; q < tt.Length; q += 3) { var x = tt[q + 1]; tt[q + 1] = tt[q + 2]; tt[q + 2] = x; } m.triangles = tt; m.RecalculateNormals(); m.RecalculateTangents(); }
        var go = new GameObject("Zbocze nad tunelem (łąka)"); go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = m; var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
        go.AddComponent<MeshCollider>().sharedMesh = m;
        BraxRegionTrawa.Teren(mr, false, true);
        Physics.SyncTransforms();
        var src = BraxRegion.Model("drzewo_verdant");
        for (int i = 0; i < 22 && src; i++)
        {
            float x = x0 + 8f + (i * 37 % 13) * 10.5f, z = z1 - 10f - (i * 17 % 6) * 9.5f;
            if (Mathf.Abs(x - AX) < 12f && z > z1 - 26f) continue;   // nad tunelem bez drzew przy portalu
            if (!Physics.Raycast(new Vector3(x, 80f, z), Vector3.down, out var h, 120f)) continue;
            if (BraxRegion.Put(g, src, "Zbocze: drzewo", h.point, (i * 53) % 360, 1f, false, 12f)) drzewa++;
        }
    }

    // =====================================================================================================================
    // 9) sygnalizacja na wysięgnikach (skrzyżowania T), zebry, linie zatrzymania, znaki ronda
    static Material czW, czO, zoW, zoO, ziW, ziO, czarny, tarcza;
    sealed class Sz { public BraxRegionMeble.Bud czA = new BraxRegionMeble.Bud(), zoA = new BraxRegionMeble.Bud(), ziA = new BraxRegionMeble.Bud(), czB = new BraxRegionMeble.Bud(), zoB = new BraxRegionMeble.Bud(), ziB = new BraxRegionMeble.Bud(), pCz = new BraxRegionMeble.Bud(), pZi = new BraxRegionMeble.Bud(), obud = new BraxRegionMeble.Bud(), slup = new BraxRegionMeble.Bud(), biel = new BraxRegionMeble.Bud(), zolty = new BraxRegionMeble.Bud(); }

    static void PlanOznakowania()
    {
        czW = BraxRegionMeble.Swiecacy("Sygnalizator: czerwone (świeci)", new Color(1f, .07f, .04f), 2.2f); czO = BraxCityMaterials.Surface("Sygnalizator: czerwone (zgaszone)", new Color(.22f, .03f, .02f), .2f);
        zoW = BraxRegionMeble.Swiecacy("Sygnalizator: żółte (świeci)", new Color(1f, .6f, .04f), 2.2f); zoO = BraxCityMaterials.Surface("Sygnalizator: żółte (zgaszone)", new Color(.22f, .14f, .02f), .2f);
        ziW = BraxRegionMeble.Swiecacy("Sygnalizator: zielone (świeci)", new Color(.1f, 1f, .45f), 2.2f); ziO = BraxCityMaterials.Surface("Sygnalizator: zielone (zgaszone)", new Color(.02f, .18f, .08f), .2f);
        czarny = BraxCityMaterials.Surface("Sygnalizator: obudowa czarna", new Color(.035f, .035f, .04f), .55f, .25f);
        tarcza = BraxCityMaterials.Surface("Sygnalizator: obwódka tarczy biała", new Color(.9f, .9f, .88f), .5f);
        var farbaB = new Farba();
        // J1: ulica główna × ul. Wiaduktowa (T: E, W, S) — oś A = x (główna), oś B = z (Wiaduktowa)
        SkrzyzowanieT(farbaB, new Vector3(WX, YA, GZ), GH, WH, J1, 0f);
        // J2: Aleja Płd. × ul. Warsztatowa (T: N, S, E) — oś A = x (Warsztatowa), oś B = z (aleja)
        SkrzyzowanieT(farbaB, new Vector3(AX, YA, SZ), SH, AH, J2, 20f);
        // zebry na wlotach ronda (bez świateł, D-6 + A-7 + C-12) — 6 m od krawędzi placu
        foreach (var (dir, hj) in RamionaRonda)
        {
            var c = new Vector3(RX, YA, RZ) + dir * (RPlac + 3f);
            Zebra(farbaB, c, dir, hj);
        }
        // Przejście do parkingu: stare przejście „wlot ronda” usunięte.
        Zebra(farbaB, new Vector3(AX, YA, PetlaZ - PetlaPlac - 3f), Vector3.forward, AH);   // wejście na parking
        Zebra(farbaB, new Vector3(PetlaZX + PetlaPlac + 3f, YA, RZ), Vector3.right, AH);     // wlot pętli ZACHÓD
        // oś ul. Zachodniej: przerywana (bez odcinków przy zebrach)
        for (float x = ZachX1 - 10f; x - 3f > ZachX0 + 9f; x -= 7f) farbaB.MinMax(new Vector3(x - 3f, YA + .02f, RZ - .06f), new Vector3(x, YA + .021f, RZ + .06f));
        // oś ulicy głównej przed J1: linia ciągła 15 m po obu stronach (zamiast usuniętych kresek), dalej przerywana do ronda
        foreach (float sg in new[] { -1f, 1f })
            farbaB.MinMax(new Vector3(Mathf.Min(WX + sg * 10.8f, WX + sg * 25.8f), YA, GZ - .06f), new Vector3(Mathf.Max(WX + sg * 10.8f, WX + sg * 25.8f), YA, GZ + .06f));
        for (float x = WX - WH - 25f; x - 3f > -98f; x -= 7f) farbaB.MinMax(new Vector3(x - 3f, YA + .02f, GZ - .06f), new Vector3(x, YA + .021f, GZ + .06f));
        // osie alej: przerywane, przed skrzyżowaniami ciągłe
        for (float z = RZ + RPlac + 10f; z + 3f < PetlaZ - PetlaPlac - 7f; z += 7f) farbaB.MinMax(new Vector3(AX - .06f, YA + .02f, z), new Vector3(AX + .06f, YA + .021f, z + 3f));
        for (float z = RZ - RPlac - 10f; z > SZ + SH + 18f; z -= 7f) farbaB.MinMax(new Vector3(AX - .06f, YA + .02f, z - 3f), new Vector3(AX + .06f, YA + .021f, z));
        for (float z = SZ - SH - 18f; z > AS1 + 16f; z -= 7f) farbaB.MinMax(new Vector3(AX - .06f, YA + .02f, z - 3f), new Vector3(AX + .06f, YA + .021f, z));
        // Linie stanowisk wierzchnią farbą W WYCIĘCIACH asfaltu, bez z-fightingu.
        // Parkowanie prostopadłe do osi alei, po obydwu bokach. Brak znaków ronda.
        for (int i = 0; i <= 9; i++)
        {
            float z = PetlaZ - 17.75f + i * 3.5f;
            farbaB.MinMax(new Vector3(AX - 17f, YA, z - .06f),
                          new Vector3(AX - 6f, YA, z + .06f));
            farbaB.MinMax(new Vector3(AX + 6f, YA, z - .06f),
                          new Vector3(AX + 17f, YA, z + .06f));
        }
        try { NoweDzielniceOznakowanie(farbaB); } catch (Exception e) { Debug.LogError("BRAXHORN_MIASTO_ASSETY oznakowanie nowych dzielnic DO POPRAWKI: " + e); }
        BudujAsfalt(farbaB);
    }

    static readonly (Vector3 dir, float hj, float chod)[] J1 = { (Vector3.right, GH, 3.1f), (Vector3.left, GH, 3.1f), (Vector3.back, WH, 1.6f) };
    static readonly (Vector3 dir, float hj, float chod)[] J2 = { (Vector3.forward, AH, Chod), (Vector3.back, AH, Chod), (Vector3.right, SH, 2.5f) };

    // etap 3: sprzęt sygnalizacji (słupy na gotowym chodniku) i znaki ronda
    static void Sygnalizacja()
    {
        Physics.SyncTransforms();
        SkrzyzowanieT(null, new Vector3(WX, YA, GZ), GH, WH, J1, 0f);
        SkrzyzowanieT(null, new Vector3(AX, YA, SZ), SH, AH, J2, 20f);
        // przejścia bez świateł (wloty ronda i pętli): auta ustępują pieszym — strefy zatrzymania przed pasami w obu kierunkach
        var zs = new List<Vector3>(); var zp = new List<Vector3>(); var ss = new List<Vector3>(); var sk = new List<Vector3>(); var szb = new List<int>();
        void ZebraBezSwiatel(Vector3 c, Vector3 dir, float hj)
        {
            var R = Vector3.Cross(Vector3.up, dir).normalized; float w = 4f * K * .5f;
            int i = zs.Count; zs.Add(c);
            var pol = R * (hj + Kr + .8f) + dir * w; zp.Add(new Vector3(Mathf.Abs(pol.x), 2f, Mathf.Abs(pol.z)));
            // R = prawa ręka jadącego w stronę +dir (Cross(up, dir)) — ruch prawostronny: pas +dir po stronie +R, pas −dir po −R
            ss.Add(c - dir * (w + 2.5f) + R * hj * .5f); sk.Add(dir); szb.Add(i);
            ss.Add(c + dir * (w + 2.5f) - R * hj * .5f); sk.Add(-dir); szb.Add(i);
        }
        // rondo i pętla: „ustąp pierwszeństwa” na pasie wjazdowym (−R ramienia), kontrola pierścienia pod prąd (ruch przeciwnie do
        // wskazówek zegara: auto nadjeżdża od kąta ramienia −40°) — jak strefy ronda osiedla (BraxClaudeOsiedleDrogiA)
        var rs = new List<Vector3>(); var rk = new List<Vector3>(); var rc = new List<Vector3>();
        void Wlot(Vector3 srodek, Vector3 dir, float hj)
        {
            var R = Vector3.Cross(Vector3.up, dir).normalized;
            rs.Add(srodek + dir * (RJezdnia + 2.4f) - R * hj * .5f); rk.Add(-dir);
            float phi = Mathf.Atan2(dir.z, dir.x) - 40f * Mathf.Deg2Rad, rm = (RWyspa + RJezdnia) * .5f;
            rc.Add(srodek + new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi)) * rm + Vector3.up * .8f);
        }
        var cR = new Vector3(RX, YA, RZ); var cP = new Vector3(AX, YA, PetlaZ);
        foreach (var (dir, hj) in RamionaRonda) { ZebraBezSwiatel(cR + dir * (RPlac + 3f), dir, hj); Wlot(cR, dir, hj); }
        // Dawna pętla PÓŁNOC jest parkingiem; bez niewidzialnej bramki
        // ronda, która zatrzymywałaby auta na pasie parkingowym.
        ZebraBezSwiatel(cP + Vector3.back * (PetlaPlac + 3f), Vector3.back, AH);
        var cWz = new Vector3(PetlaZX, YA, RZ);
        ZebraBezSwiatel(cWz + Vector3.right * (PetlaPlac + 3f), Vector3.right, AH); Wlot(cWz, Vector3.right, AH);
        var gz = new GameObject("Strefy zatrzymania: przejścia bez świateł i wloty ronda/pętli"); gz.transform.SetParent(root, false);
        var sd = gz.AddComponent<BraxStrefyDrogowe>();
        sd.zebraSrodek = zs.ToArray(); sd.zebraPolowa = zp.ToArray(); sd.stopSrodek = ss.ToArray(); sd.stopKierunek = sk.ToArray(); sd.stopZebra = szb.ToArray();
        sd.rondoSrodek = rs.ToArray(); sd.rondoKierunek = rk.ToArray(); sd.rondoKontrola = rc.ToArray();
        Debug.Log("BRAXHORN_MIASTO_B_STREFY przejścia_bez_świateł=" + zs.Count + " strefy_aut=" + ss.Count + " wloty_ronda=" + rs.Count);
        foreach (var (dir, hj) in RamionaRonda)
        {
            var c = new Vector3(RX, YA, RZ) + dir * (RPlac + 3f);
            ZnakRonda(NaPodlozu(c - Vector3.Cross(Vector3.up, dir) * (hj + Kr + .5f) + dir * 4f + Vector3.up * YC), dir);   // po prawej wjeżdżających, lico do nich
        }
        // 3.11 (właściciel: „znaki normalne prawdziwe na drogach”): wloty obu pętli też jak rondo (A-7 + C-12), a przy każdym
        // przejściu bez świateł D-6 po prawej stronie jadących w obu kierunkach (lico do nadjeżdżających), tuż przed pasami
        foreach (var (cc, dir, hj) in new[] { (cWz + Vector3.right * (PetlaPlac + 3f), Vector3.right, AH) })
            ZnakRonda(NaPodlozu(cc - Vector3.Cross(Vector3.up, dir) * (hj + Kr + .5f) + dir * 4f + Vector3.up * YC), dir);
        int nD6 = 0;
        for (int i = 0; i < zs.Count; i++)
        {
            var dir = sk[2 * i]; var R = Vector3.Cross(Vector3.up, dir).normalized;
            float hj = Mathf.Abs(Vector3.Dot(new Vector3(zp[i].x, 0, zp[i].z), R)) - Kr - .8f;   // połowa jezdni (zebraPolowa = R·(hj+Kr+.8) + dir·w)
            float w = 4f * K * .5f;
            foreach (float sg in new[] { 1f, -1f })   // sg = +1: jadący +dir (prawa = +R) — znak przed pasami, lico −dir
            {
                var p = zs[i] + R * (sg * (hj + Kr + .45f)) - dir * (sg * (w + .35f)); p.y = YC;
                if (BraxZnakiPL.Slup(root, "Znak D-6 przejście dla pieszych (wzór urzędowy)", NaPodlozu(p), -dir * sg, "D-6")) nD6++;
            }
        }
        foreach (var (zc, dir, hj) in ZebryBezSwiatelNd)   // nowe dzielnice
        {
            var R = Vector3.Cross(Vector3.up, dir).normalized; float w = 4f * K * .5f;
            foreach (float sg in new[] { 1f, -1f })
            {
                var p = zc + R * (sg * (hj + Kr + .45f)) - dir * (sg * (w + .35f)); p.y = YC;
                if (BraxZnakiPL.Slup(root, "Znak D-6 przejście dla pieszych (wzór urzędowy)", NaPodlozu(p), -dir * sg, "D-6")) nD6++;
            }
        }
        Debug.Log("BRAXHORN_MIASTO_B_ZNAKI D-6 przy przejściach bez świateł: " + nD6 + "; " + BraxZnakiPL.Raport());
    }

    static void Zebra(Farba f, Vector3 c, Vector3 dir, float hj)
    {
        // pasy 0.5 m co 1.0 m, szerokość przejścia 4 m × k, w poprzek jezdni (pasy wzdłuż kierunku jazdy)
        var R = Vector3.Cross(Vector3.up, dir).normalized; float w = 4f * K;
        for (float o = -hj + .25f; o <= hj - .4f; o += 1f)
        {
            var p0 = c + R * o - dir * w * .5f; var p1 = c + R * (o + .5f) + dir * w * .5f;
            f.MinMax(new Vector3(Mathf.Min(p0.x, p1.x), YA + .02f, Mathf.Min(p0.z, p1.z)), new Vector3(Mathf.Max(p0.x, p1.x), YA + .021f, Mathf.Max(p0.z, p1.z)));
        }
        zebry++;
    }

    // skrzyżowanie typu T z sygnalizacją: wloty (kierunek OD środka, połowa jezdni wlotu, szerokość chodnika)
    // f != null: TYLKO oznakowanie (zebry, linie zatrzymania) — etap 1, przed budową asfaltu (farba w wycięciach);
    // f == null: TYLKO sprzęt (słupy z wysięgnikami, sygnalizatory, przyciski, sterowanie) — etap 3, na gotowym chodniku.
    static void SkrzyzowanieT(Farba f, Vector3 c, float hjX, float hjZ, (Vector3 dir, float hj, float chod)[] wloty, float przes)
    {
        if (f != null)
        {
            foreach (var (dir, hj, chod) in wloty)
            {
                var R = Vector3.Cross(Vector3.up, dir).normalized;
                float poprzeczna = Mathf.Abs(dir.x) > .5f ? hjZ : hjX;
                float zebraOd = poprzeczna + Kr + .6f, zebraW = 4f * K, stop = zebraOd + zebraW + 1.0f;
                Zebra(f, c + dir * (zebraOd + zebraW * .5f), dir, hj);
                var s0 = c + dir * stop - R * .05f; var s1 = c + dir * (stop + .5f) - R * (hj - .1f);
                f.MinMax(s0, s1);
            }
            return;
        }
        var sz = new Sz();
        var stopS = new List<Vector3>(); var stopK = new List<Vector3>(); var stopO = new List<int>();
        var pS = new List<Vector3>(); var pK = new List<Vector3>();
        var g = new GameObject("Sygnalizacja świetlna (" + c.x.ToString("0") + ", " + c.z.ToString("0") + ")"); g.transform.SetParent(root, false);
        foreach (var (dir, hj, chod) in wloty)
        {
            var R = Vector3.Cross(Vector3.up, dir).normalized;      // prawa strona patrząc OD środka; wlot do środka jedzie −dir, jego prawa strona = −R
            float poprzeczna = Mathf.Abs(dir.x) > .5f ? hjZ : hjX;   // połowa jezdni ulicy poprzecznej
            float zebraOd = poprzeczna + Kr + .6f, zebraW = 4f * K, stop = zebraOd + zebraW + 1.0f;
            int os = Mathf.Abs(dir.x) > .5f ? 0 : 1;
            var zc = c + dir * (zebraOd + zebraW * .5f);
            // słup z wysięgnikiem po prawej stronie wjeżdżających (−R), tuż za linią zatrzymania (na gotowym chodniku)
            var baza = c + dir * (stop + .8f) - R * (hj + Kr + .6f); baza.y = Podloze(baza.x, baza.z, YC);
            Slup(sz, baza, 6.3f * K, .09f);
            var koniec = c + dir * (stop + .8f) - R * (hj * .5f); koniec.y = baza.y + 6.0f * K;
            sz.slup.Cyl(baza + Vector3.up * 6.0f * K, koniec, .055f, 10);
            sz.slup.Cyl(baza + Vector3.up * 5.2f * K, Vector3.Lerp(baza + Vector3.up * 6.0f * K, koniec, .45f), .03f, 8);
            Glowica(sz, koniec - Vector3.up * (.62f * K + .06f), dir, os);
            Glowica(sz, baza + dir * (.12f + .21f * K) + Vector3.up * 2.75f * K, dir, os);
            BraxRegionMeble.Kolizja(g.transform, "Słup sygnalizacji kolizja", baza + Vector3.up * 3.1f * K, new Vector3(.25f, 6.3f * K, .25f));
            stopS.Add(c + dir * (stop + 2.5f) - R * hj * .5f); stopK.Add(-dir); stopO.Add(os);
            // sygnalizatory dla pieszych na obu końcach zebry
            foreach (int sd in new[] { 1, -1 })
            {
                var pole = c + dir * (zebraOd - .35f) + R * sd * (hj + Kr + .45f); pole.y = Podloze(pole.x, pole.z, YC);
                Slup(sz, pole, 3.0f * K, .06f);
                var fwd = -R * sd;
                GlowicaPiesi(sz, pole + fwd * (.09f + .2f * K) + Vector3.up * 2.45f * K, fwd);
                sz.zolty.MinMax(pole - dir * .08f + new Vector3(-.06f * K, 1.0f * K, -.06f * K), pole - dir * .08f + new Vector3(.06f * K, 1.2f * K, .06f * K));
                BraxRegionMeble.Kolizja(g.transform, "Słupek pieszych kolizja", pole + Vector3.up * 1.5f * K, new Vector3(.15f, 3f * K, .15f));
                pS.Add(zc + R * sd * (hj + .4f)); pK.Add(-R * sd);
            }
        }
        var s = g.AddComponent<BraxSwiatloDrogowe>();
        sz.slup.Obiekt("Słupy sygnalizacji (ocynk)", g.transform, stal, false, true);
        sz.obud.Obiekt("Sygnalizatory: obudowy", g.transform, czarny, false, true);
        sz.biel.Obiekt("Sygnalizatory: tarcze (obwódka)", g.transform, tarcza, false, false);
        sz.zolty.Obiekt("Przyciski dla pieszych", g.transform, BraxCityMaterials.Surface("Przycisk dla pieszych: żółty", new Color(.92f, .72f, .08f), .5f, .1f), false, false);
        s.czerwoneA = R_(sz.czA, "szkła czerwone (oś x)", g.transform, czO); s.zolteA = R_(sz.zoA, "szkła żółte (oś x)", g.transform, zoO); s.zieloneA = R_(sz.ziA, "szkła zielone (oś x)", g.transform, ziO);
        s.czerwoneB = R_(sz.czB, "szkła czerwone (oś z)", g.transform, czO); s.zolteB = R_(sz.zoB, "szkła żółte (oś z)", g.transform, zoO); s.zieloneB = R_(sz.ziB, "szkła zielone (oś z)", g.transform, ziO);
        s.pieszyCzerwony = R_(sz.pCz, "ludziki czerwone", g.transform, czO); s.pieszyZielony = R_(sz.pZi, "ludziki zielone", g.transform, ziO);
        s.czerwonyWl = czW; s.czerwonyWyl = czO; s.zoltyWl = zoW; s.zoltyWyl = zoO; s.zielonyWl = ziW; s.zielonyWyl = ziO;
        s.ludzikCzerwonyWl = czW; s.ludzikCzerwonyWyl = czO; s.ludzikZielonyWl = ziW; s.ludzikZielonyWyl = ziO;
        s.stopSrodek = stopS.ToArray(); s.stopKierunek = stopK.ToArray(); s.stopOs = stopO.ToArray();
        s.pieszySrodek = pS.ToArray(); s.pieszyKierunek = pK.ToArray(); s.przesuniecie = przes;
        SkrzyzowaniaZeSwiatlami.Add(c);
    }

    static Renderer R_(BraxRegionMeble.Bud b, string n, Transform p, Material m) { var g = b.Obiekt(n, p, m, false, false); return g ? g.GetComponent<Renderer>() : null; }

    static void Slup(Sz sz, Vector3 p, float h, float r)
    {
        sz.slup.Cyl(p, p + Vector3.up * .05f, r * 2.2f, 12);
        sz.slup.Cyl(p + Vector3.up * .05f, p + Vector3.up * .4f, r * 1.35f, 12);
        sz.slup.Cyl(p + Vector3.up * .4f, p + Vector3.up * h, r, 12);
        sz.obud.Cyl(p + Vector3.up * h, p + Vector3.up * (h + .03f), r + .006f, 12);
    }

    // sygnalizator 3-komorowy (200 mm × k) z tarczą kontrastową: c = środek lica, f = kierunek lica (do kierowców = dir od środka)
    static void Glowica(Sz sz, Vector3 c, Vector3 f, int os)
    {
        f.y = 0; f.Normalize(); var R = Vector3.Cross(Vector3.up, f).normalized; var rot = Quaternion.LookRotation(f);
        sz.obud.Box(c - f * .1f * K, new Vector3(.32f * K, .98f * K, .2f * K), rot);
        sz.obud.Box(c - f * .205f * K, new Vector3(.44f * K, 1.1f * K, .015f), rot);
        float tw = .44f * K, th = 1.1f * K, ob = .03f * K; var tb = c - f * .21f * K;
        sz.biel.Box(tb + R * (tw * .5f + ob * .5f), new Vector3(ob, th + 2 * ob, .015f), rot); sz.biel.Box(tb - R * (tw * .5f + ob * .5f), new Vector3(ob, th + 2 * ob, .015f), rot);
        sz.biel.Box(tb + Vector3.up * (th * .5f + ob * .5f), new Vector3(tw, ob, .015f), rot); sz.biel.Box(tb - Vector3.up * (th * .5f + ob * .5f), new Vector3(tw, ob, .015f), rot);
        var sl = os == 0 ? new[] { sz.czA, sz.zoA, sz.ziA } : new[] { sz.czB, sz.zoB, sz.ziB };
        for (int q = 0; q < 3; q++)
        {
            var lc = c + Vector3.up * ((.31f - q * .31f) * K);
            sl[q].Cyl(lc - f * .01f, lc + f * .008f, .1f * K, 18);
            // daszek (górna połowa pierścienia)
            for (int s = 0; s < 8; s++)
            {
                float t0 = s * Mathf.PI / 8f, t1 = (s + 1) * Mathf.PI / 8f;
                Vector3 d0 = R * Mathf.Cos(t0) + Vector3.up * Mathf.Sin(t0), d1 = R * Mathf.Cos(t1) + Vector3.up * Mathf.Sin(t1); float rv = .118f * K;
                sz.obud.Quad(lc + d0 * rv, lc + d1 * rv, lc + d1 * rv + f * .17f * K, lc + d0 * rv + f * .17f * K);
                sz.obud.Quad(lc + d1 * rv, lc + d0 * rv, lc + d0 * rv + f * .17f * K, lc + d1 * rv + f * .17f * K);
            }
        }
        sygnalizatory++;
    }

    static void GlowicaPiesi(Sz sz, Vector3 c, Vector3 f)
    {
        f.y = 0; f.Normalize(); var R = Vector3.Cross(Vector3.up, f).normalized; var rot = Quaternion.LookRotation(f);
        sz.obud.Box(c - f * .1f * K, new Vector3(.3f * K, .64f * K, .2f * K), rot);
        foreach (float dy in new[] { .16f, -.16f })
        {
            var oc = c + Vector3.up * dy * K;
            var s = dy > 0 ? sz.pCz : sz.pZi;
            // sylwetka ludzika (uproszczona: głowa + tułów + nogi) na ciemnym polu
            s.Cyl(oc + Vector3.up * .072f * K - f * .005f, oc + Vector3.up * .072f * K + f * .006f, .022f * K, 10);
            s.Box(oc + Vector3.up * .01f * K, new Vector3(.05f * K, .08f * K, .012f), rot);
            if (dy > 0) { s.Box(oc + R * .014f * K - Vector3.up * .05f * K, new Vector3(.018f * K, .08f * K, .012f), rot); s.Box(oc - R * .014f * K - Vector3.up * .05f * K, new Vector3(.018f * K, .08f * K, .012f), rot); }
            else { s.Box(oc + R * .02f * K - Vector3.up * .05f * K, new Vector3(.018f * K, .08f * K, .012f), rot * Quaternion.Euler(0, 0, 25f)); s.Box(oc - R * .02f * K - Vector3.up * .05f * K, new Vector3(.018f * K, .08f * K, .012f), rot * Quaternion.Euler(0, 0, -25f)); }
            sz.obud.Box(oc + Vector3.up * .135f * K + f * .06f * K, new Vector3(.27f * K, .012f, .12f * K), rot);
        }
        sygnalizatory++;
    }

    static void ZnakRonda(Vector3 p, Vector3 przod)
    {
        // słupek z A-7 (ustąp pierwszeństwa) i C-12 (ruch okrężny) — PRAWDZIWE wzory (BraxZnakiPL); z kodu tylko, gdy brak pliku wzoru
        { var pp = p; pp.y = YC; if (BraxZnakiPL.Slup(root, "Znaki ronda A-7 + C-12 (wzory urzędowe)", pp, przod, "A-7", "C-12")) return; }
        Debug.LogWarning("DO POPRAWKI: BRAXHORN_ZNAKI_PL brak wzorów A-7/C-12 — znaki ronda z kodu");
        var g = new GameObject("Znaki ronda A-7 + C-12"); g.transform.SetParent(root, false);
        var sl = new BraxRegionMeble.Bud(); p.y = YC; sl.Cyl(p, p + Vector3.up * 2.9f * K, .03f * K, 10); sl.Obiekt("Znak: słupek", g.transform, stal, false, true);
        var rot = Quaternion.LookRotation(przod);
        var tr = new BraxRegionMeble.Bud(); var ob = new BraxRegionMeble.Bud(); var nb = new BraxRegionMeble.Bud();
        var c7 = p + Vector3.up * 2.55f * K + przod * .05f;
        tr.Tri(c7 + Vector3.up * -.32f * K + przod * .01f, c7 + Vector3.up * .22f * K + Vector3.Cross(Vector3.up, przod) * .31f * K + przod * .01f, c7 + Vector3.up * .22f * K - Vector3.Cross(Vector3.up, przod) * .31f * K + przod * .01f);
        tr.Tri(c7 + Vector3.up * -.32f * K + przod * .01f, c7 + Vector3.up * .22f * K - Vector3.Cross(Vector3.up, przod) * .31f * K + przod * .01f, c7 + Vector3.up * .22f * K + Vector3.Cross(Vector3.up, przod) * .31f * K + przod * .01f);
        var c12 = p + Vector3.up * 1.95f * K + przod * .05f;
        nb.Cyl(c12, c12 + przod * .012f, .3f * K, 24);
        ob.Cyl(c12 + przod * .012f, c12 + przod * .016f, .22f * K, 24);
        tr.Obiekt("Znak A-7 (biało-czerwony)", g.transform, BraxCityMaterials.Surface("Znak: czerwony", new Color(.8f, .06f, .06f), .4f), false, false);
        nb.Obiekt("Znak C-12 (niebieski)", g.transform, BraxCityMaterials.Surface("Znak: niebieski C-12", new Color(.04f, .22f, .6f), .4f), false, false);
        ob.Obiekt("Znak C-12 strzałki (biel)", g.transform, BraxCityMaterials.Surface("Znak: biel", new Color(.92f, .92f, .9f), .4f), false, false);
        BraxRegionMeble.Kolizja(g.transform, "Znak kolizja", p + Vector3.up * 1.4f * K, new Vector3(.1f, 2.9f * K, .1f));
    }

    // =====================================================================================================================
    // drzewa i latarnie
    static void Drzewo(Vector3 p) => Pozniej("drzewo", () => DrzewoTeraz(NaPodlozu(p)));
    static void DrzewoTeraz(Vector3 p)
    {
        var src = BraxRegion.Model("drzewo_verdant");
        if (!src) return;
        var o = BraxRegion.Put(root, src, "Aleja: drzewo", p, (p.x * 31f + p.z * 17f) % 360f, 1f, false, 10.5f);
        if (!o) return;
        var c = new GameObject("Aleja: pień kolizja").AddComponent<CapsuleCollider>(); c.transform.SetParent(root, false);
        c.transform.position = p + Vector3.up * 1.5f; c.radius = .3f; c.height = 3f;
        BraxRegionTrawa.Drzewo(p);
        drzewa++;
    }

    // latarnia uliczna z wysięgnikiem: słup 8 m × k, wysięgnik nad jezdnię (dir), oprawa LED (nie świeci w dzień)
    static void Latarnia(Vector3 p, Vector3 dir, float wysieg) => Pozniej("latarnia", () => LatarniaTeraz(NaPodlozu(p), dir, wysieg));
    static void LatarniaTeraz(Vector3 p, Vector3 dir, float wysieg)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < .1f)
        {
            Debug.LogWarning("BRAXHORN_LATARNIE DO POPRAWKI: brak kierunku oprawy @" + p);
            return;
        }
        dir.Normalize();
        // Odbiór R1: wysięgnik musi wskazywać jezdnię, nie elewację.
        // Wcześniej test był ograniczony do 2.2*K (~2.85 m) i nie dochodził
        // do jezdni za pasem zieleni; obie strony wychodziły jako "brak".
        float maxProbe = Mathf.Max(9f, wysieg + 4f);
        bool JezdniaPoStronie(Vector3 strona)
        {
            for (float d = .8f; d <= maxProbe; d += .8f)
                if (NaJezdni(p + strona * d)) return true;
            return false;
        }
        bool frontRoad = JezdniaPoStronie(dir), backRoad = JezdniaPoStronie(-dir);
        if (!frontRoad && backRoad)
        {
            dir = -dir;
            Debug.Log("BRAXHORN_LATARNIE odwrocony wysiegnik na JEZDNIE @" +
                p.ToString("F1"));
        }
        else if (!frontRoad && !backRoad)
            Debug.LogWarning("BRAXHORN_LATARNIE DO WERYFIKACJI: brak jezdni w promieniu " +
                maxProbe.ToString("F1") + " m przy " + p.ToString("F1"));
        var g = new GameObject("Latarnia uliczna"); g.transform.SetParent(root, false);
        var b = new BraxRegionMeble.Bud(); float h = 8f * K;
        b.Cyl(p, p + Vector3.up * .6f * K, .14f * K, 14, .11f * K);
        b.Cyl(p + Vector3.up * .6f * K, p + Vector3.up * h, .085f * K, 14, .055f * K);
        var top = p + Vector3.up * h; var end = top + dir * Mathf.Min(wysieg, 2.2f * K) + Vector3.up * .25f * K;
        b.Cyl(top, end, .045f * K, 10);
        b.Obiekt("Latarnia: słup i wysięgnik", g.transform, BraxCityMaterials.Surface("Latarnia: stal grafit", new Color(.13f, .14f, .15f), .45f, .6f), false, true);
        var o = new BraxRegionMeble.Bud(); o.Box(end + dir * .3f * K - Vector3.up * .06f * K, new Vector3(.32f * K, .1f * K, .62f * K), Quaternion.LookRotation(dir));
        o.Obiekt("Latarnia: oprawa", g.transform, BraxCityMaterials.Surface("Latarnia: oprawa", new Color(.2f, .21f, .22f), .4f, .5f), false, true);
        var kl = new BraxRegionMeble.Bud(); kl.Box(end + dir * .3f * K - Vector3.up * .115f * K, new Vector3(.24f * K, .012f, .5f * K), Quaternion.LookRotation(dir));
        kl.Obiekt("Latarnia: klosz", g.transform, BraxCityMaterials.Surface("Latarnia: klosz mleczny", new Color(.9f, .9f, .86f), .3f), false, false);
        BraxRegionMeble.Kolizja(g.transform, "Latarnia kolizja", p + Vector3.up * h * .5f, new Vector3(.25f * K, h, .25f * K));
        latarnie++;
    }

    // =====================================================================================================================
    // 10) rejestracja jezdni dla kroku ruchu (RUCH_API.md) — osie równoległe do osi świata
    static void RejestracjaRuchu()
    {
        void J(string id, Vector3 a, Vector3 b, float pol) => BraxClaudeOsiedle.OsieJezdni.Add(new BraxClaudeOsiedle.OsJezdni { id = id, a = a, b = b, polowaJezdni = pol });
        J("MIASTO Aleja Północna; chodniki " + Chod + " " + Chod, new Vector3(AX, YA, RZ + RJezdnia), new Vector3(AX, YA, PetlaZ - PetlaR), AH);
        J("MIASTO Aleja Południowa; chodniki " + Chod + " " + Chod + "; ukryty B", new Vector3(AX, YA, RZ - RJezdnia), new Vector3(AX, YA, AS1 - 18f), AH);
        J("MIASTO Główna (do ronda); chodniki 3.1 3.4", new Vector3(-104f, YA, GZ), new Vector3(RX + RJezdnia, YA, GZ), GH);
        J("MIASTO Wiaduktowa; chodniki 1.55 1.55", new Vector3(WX, YA, GZ), new Vector3(WX, YA, SZ), WH);
        J("MIASTO Warsztatowa; chodniki 2.5 2.5", new Vector3(AX, YA, SZ), new Vector3(SX1, YA, SZ), SH);
        BraxClaudeOsiedle.SkrzyzowaniaInfo.Add(new BraxClaudeOsiedle.SkrzyzowanieInfo { srodek = new Vector3(RX, YA, RZ), promien = RJezdnia, rodzaj = BraxClaudeOsiedle.RodzajSkrzyzowania.Rondo });
        // Pętla PÓŁNOC skasowana: parking nie jest węzłem ronda w sieci ruchu.
        J("MIASTO Zachodnia; chodniki " + Chod + " " + Chod, new Vector3(RX - RJezdnia, YA, RZ), new Vector3(PetlaZX + PetlaR, YA, RZ), AH);
        BraxClaudeOsiedle.SkrzyzowaniaInfo.Add(new BraxClaudeOsiedle.SkrzyzowanieInfo { srodek = new Vector3(PetlaZX, YA, RZ), promien = PetlaR, rodzaj = BraxClaudeOsiedle.RodzajSkrzyzowania.Rondo });
    }
}
