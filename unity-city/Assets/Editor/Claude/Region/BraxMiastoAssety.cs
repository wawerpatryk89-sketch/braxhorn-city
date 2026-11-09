using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MODELE ZE SKETCHFABA W MIEŚCIE (etap „assety”, 2.11). Paczki zaszyfrowane w repo (unity-city/Prywatne/miasto_*.tar.gz.enc —
// licencje Free Standard nie pozwalają rozpowszechniać plików), odszyfrowywane w CI przez unity-tools/claude/prywatne.py do
// Assets/Source/Prywatne/Miasto/<paczka>/ (poza gitem). Brak klucza albo paczki = miasto buduje się bez tych modeli
// (log BRAXHORN_MIASTO_ASSETY brak …), nic się nie wywraca.
// Modele przetworzone offline (scratchpad/skrypty, raport w ASSETY_PLAN.md): PRAWDZIWE metry, podstawa y = 0, środek obrysu
// parteru w (0, 0), drogi/chodniki/auta/drzewa z dzielnic usunięte, logo firm zdjęte, tekstury ≤ 2K (pojazdy/wyposażenie ≤ 1K)
// jako PLIKI obok modelu (_tekstury/…) — Unity je kompresuje (BraxMiastoAssetyTekstury). Tu: × K (świat pod Braxa).
// Każde postawienie w osobnym try/catch (błąd = „BRAXHORN_MIASTO_ASSETY … DO POPRAWKI”, budowa idzie dalej).
public static class BraxMiastoAssety
{
    public const string Baza = "Assets/Source/Prywatne/Miasto/";
    static float K => BraxClaudeOsiedle.K;
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static readonly HashSet<string> brakZgloszony = new HashSet<string>();
    public static int Postawione, Bledy;

    public static void Log(string s) => Debug.Log("BRAXHORN_MIASTO_ASSETY " + s);
    public static void Blad(string co, Exception e) { Bledy++; Debug.LogError("BRAXHORN_MIASTO_ASSETY " + co + " DO POPRAWKI: " + e); }

    /// <summary>Czy paczka jest odszyfrowana (katalog z co najmniej jednym .glb).</summary>
    public static bool Jest(string paczka) => Directory.Exists(Baza + paczka) && Directory.GetFiles(Baza + paczka, "*.glb").Length > 0;

    /// <summary>Model z paczki (import glTFast). null = brak (zgłoszone raz w logu).</summary>
    const bool KontenerowiecNaRedzie = false;
    public static GameObject Model(string paczka, string plik)
    {
        string p = Baza + paczka + "/" + plik + ".glb";
        if (cache.TryGetValue(p, out var g) && g) return g;
        if (!File.Exists(p))
        {
            if (brakZgloszony.Add(p)) Log("brak " + p + " (paczka " + paczka + " nieodszyfrowana — sekret/paczka w CI) — miejsce zostaje bez modelu");
            return null;
        }
        // tekstury najpierw (glTFast bierze asset tekstury z projektu zamiast nieskompresowanej kopii)
        string tex = Baza + paczka + "/_tekstury/" + plik;
        if (Directory.Exists(tex)) AssetDatabase.ImportAsset(tex, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        g = Wczytaj(p);
        if (g && Directory.Exists(tex) && !Teksturowany(p))
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            g = Wczytaj(p);
            Log("ponowny import (tekstury po modelu): " + p + " → " + (Teksturowany(p) ? "z teksturami" : "NADAL bez tekstur — DO POPRAWKI"));
        }
        if (!g) { if (brakZgloszony.Add(p)) Log("import nieudany: " + p + " — DO POPRAWKI"); return null; }
        cache[p] = g;
        return g;
    }

    static GameObject Wczytaj(string p) => AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();

    static bool Teksturowany(string p)
    {
        foreach (var m in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>())
            foreach (var n in m.GetTexturePropertyNames())
                if (m.GetTexture(n)) return true;
        return false;
    }

    public static Transform Znajdz(Transform t, string nazwa)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == nazwa) return c;
        return null;
    }

    // Materiał miasta dla materiału glTFast: konwersja BraxRegion.CityMaterial (CityStaticPBR) i WŁASNA kopia jako plik
    // (Assets/PlayableGenerated/ClaudeRegion/Materials/assety-N.mat). Powód: metro buduje się przed BraxRegion.Apply, a Apply
    // zeruje licznik konwersji i nadpisuje pliki model-N.mat — bez kopii bramki metra straciłyby materiał.
    static readonly Dictionary<Material, Material> materialy = new Dictionary<Material, Material>();
    static int nMat;
    public static Material MaterialMiasta(Material src)
    {
        if (!src) return src;
        if (materialy.TryGetValue(src, out var m) && m) return m;
        var c = BraxRegion.CityMaterial(src);
        if (c && c != src)
        {
            var kopia = new Material(c) { name = c.name };
            Directory.CreateDirectory(BraxRegion.Out + "/Materials");
            string path;
            do path = BraxRegion.Out + "/Materials/assety-" + (nMat++) + ".mat"; while (File.Exists(path));
            BraxZapisAssetu.Utworz(kopia, path);
            c = kopia;
        }
        materialy[src] = c;
        return c;
    }

    /// <summary>Materiały glTFast → CityStaticPBR (to samo słońce, cienie i alfa co reszta miasta), cienie, flagi statyczne.</summary>
    public static void Przygotuj(GameObject o, bool statyczny = true, bool cienie = true)
    {
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(MaterialMiasta).ToArray();
            r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            if (statyczny) GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        }
        foreach (var a in o.GetComponentsInChildren<Animation>(true)) UnityEngine.Object.DestroyImmediate(a);
        foreach (var a in o.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
    }

    /// <summary>Kopia modelu: podstawa (środek obrysu, y = 0 modelu) w punkcie p, obrót yaw, skala × K (model w prawdziwych metrach).</summary>
    public static GameObject Postaw(Transform parent, GameObject zrodlo, string nazwa, Vector3 p, float yaw, float skalaRealna = 1f, bool statyczny = true, bool cienie = true)
    {
        if (!zrodlo) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(zrodlo, parent, false);
        o.name = nazwa;
        o.transform.position = p;
        o.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        o.transform.localScale = Vector3.one * (skalaRealna * K);
        Przygotuj(o, statyczny, cienie);
        Postawione++;
        return o;
    }

    public static Bounds Granice(GameObject o) => BraxRegion.Bounds(o);

    /// <summary>LODGroup z węzłów „…_LOD0/1/2” (paczki miasto_statki* — statki_hd.py); progi = wysokość na ekranie.</summary>
    public static void LodyZWezlow(GameObject o, float[] progi)
    {
        var lv = new List<LOD>(); var wszystkie = o.GetComponentsInChildren<Transform>(true);
        for (int k = 0; k < 3; k++)
        {
            var t = wszystkie.FirstOrDefault(x => x.name.EndsWith("_LOD" + k));
            if (!t) break;
            lv.Add(new LOD(progi[Mathf.Min(k, progi.Length - 1)], t.GetComponentsInChildren<Renderer>(true)));
        }
        if (lv.Count < 2) { Log("LOD: " + o.name + " — brak węzłów _LOD (jedna siatka)"); return; }
        var lg = o.GetComponent<LODGroup>(); if (!lg) lg = o.AddComponent<LODGroup>();
        lg.SetLODs(lv.ToArray()); lg.RecalculateBounds();
    }

    /// <summary>Kolizja kadłuba nad wodą (łodzie i skuter się o nią opierają) — jak przy drobnicowcu.</summary>
    static void KadlubKolizja(Transform root, string nazwa, Bounds b, float woda)
    {
        var kad = new GameObject(nazwa).AddComponent<BoxCollider>(); kad.transform.SetParent(root, false);
        kad.transform.position = new Vector3(b.center.x, woda + 3.5f * K * .5f, b.center.z); kad.size = new Vector3(b.size.x * .92f, 3.5f * K + 2f, b.size.z * .9f);
    }

    /// <summary>Ile punktów siatki 9×9 pod obrysem leży na lądzie (BraxMiastoGranice).</summary>
    static int LadPod(Bounds b)
    {
        int n = 0;
        for (int i = 0; i <= 8; i++) for (int j = 0; j <= 8; j++) if (BraxMiastoGranice.Lad(Mathf.Lerp(b.min.x + 1f, b.max.x - 1f, i / 8f), Mathf.Lerp(b.min.z, b.max.z, j / 8f))) n++;
        return n;
    }

    // KONTENER 20′ (model właściciela „freight shipping container — rusted”, proporcje ISO 6.06 × 2.59 × 2.44 m × K, 3 LOD):
    // instancje GPU (bez statycznego łączenia), 4 odcienie lakieru (mnożnik koloru na rdzawej blasze — różne firmy).
    static readonly Color[] OdcienieKont = { Color.white, new Color(.72f, .82f, 1f), new Color(1f, .8f, .72f), new Color(.8f, .95f, .8f) };
    static readonly Dictionary<(Material, int), Material> kontMat = new Dictionary<(Material, int), Material>();
    public static GameObject Kontener20(Transform parent, Vector3 podstawa, float yaw, int odcien)
    {
        var m = Model("miasto_statki2", "kontener_20");
        if (!m) return null;
        var o = Postaw(parent, m, "Kontener 20′", podstawa, yaw, 1f, false, true);
        LodyZWezlow(o, new[] { .10f, .03f, .004f });
        int w = ((odcien % OdcienieKont.Length) + OdcienieKont.Length) % OdcienieKont.Length;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.OccludeeStatic);
            r.sharedMaterials = r.sharedMaterials.Select(src =>
            {
                if (!src) return src;
                if (kontMat.TryGetValue((src, w), out var have) && have) return have;
                var c = new Material(src) { name = src.name + " (kontener odcień " + w + ")", enableInstancing = true };
                if (c.HasProperty("_Color")) c.SetColor("_Color", c.GetColor("_Color") * OdcienieKont[w]);
                Directory.CreateDirectory("Assets/PlayableGenerated/Kontenery");
                BraxZapisAssetu.Utworz(c, "Assets/PlayableGenerated/Kontenery/kontener_" + kontMat.Count + ".mat");
                kontMat[(src, w)] = c; return c;
            }).ToArray();
        }
        return o;
    }

    /// <summary>Kolizja pudełkowa wokół obiektu (osie świata) — budynki, pojazdy, sprzęt.</summary>
    public static BoxCollider Kolizja(Transform parent, GameObject o, string nazwa, float wezej = 0f)
    {
        var b = Granice(o);
        var c = new GameObject(nazwa).AddComponent<BoxCollider>(); c.transform.SetParent(parent, false);
        c.transform.position = b.center; c.size = new Vector3(Mathf.Max(.05f, b.size.x - wezej * 2f), b.size.y, Mathf.Max(.05f, b.size.z - wezej * 2f));
        return c;
    }

    // =====================================================================================================================
    // SPRZĘT SIŁOWNI W SKLEPIE BRAXHORN SPORT (BraxMiastoSport.Wnetrze, układ lokalny sklepu: front = −z, posadzka F).
    // ZAMIAST prymitywnego stojaka z hantlami z kodu (zdjęty) — ekspozycja sprzętu na sprzedaż: atlas (miejsce stojaka),
    // klatka do przysiadów z gryfem i bieżnia w witrynie na lewo od wejścia, ławka skośna na prawo od wejścia (przejście
    // do drzwi x −4…−2 wolne). true = modele postawione (stary stojak NIE powstaje); false = brak paczki → stary stojak.
    public static bool SprzetSklepu(Transform sklep, Transform kolizje, float F, float z1)
    {
        if (!Jest("miasto_silownia_metro")) { Log("BRAXHORN SPORT: brak paczki miasto_silownia_metro — zostaje stojak hantli z kodu"); return false; }
        var klatka = Model("miasto_silownia_metro", "silownia_klatka"); var bieznia = Model("miasto_silownia_metro", "silownia_bieznia");
        var lawka = Model("miasto_silownia_metro", "silownia_lawka"); var atlas = Model("miasto_silownia_metro", "silownia_atlas");
        if (!klatka || !bieznia || !lawka || !atlas) { Log("BRAXHORN SPORT: niekompletna paczka siłowni — zostaje stojak hantli z kodu"); return false; }
        int n = 0;
        // (model, środek lokalny x/z, obrót lokalny, nazwa) — wymiary × K: atlas 2.69 × 2.41, klatka (gryf wzdłuż x) 3.16 × 2.02, bieżnia 1.41 × 2.59, ławka 1.0 × 1.81
        var lista = new (GameObject m, Vector2 c, float yaw, string nazwa)[]
        {
            (atlas, new Vector2(-9.85f, z1 - 1.45f), 180f, "Sprzęt: atlas wielofunkcyjny (ekspozycja)"),
            (klatka, new Vector2(-8.3f, -3.85f), 90f, "Sprzęt: klatka do przysiadów z gryfem (witryna)"),
            (bieznia, new Vector2(-5.6f, -3.6f), 90f, "Sprzęt: bieżnia (witryna)"),
            (lawka, new Vector2(-.8f, -4.0f), 0f, "Sprzęt: ławka skośna (witryna)"),
        };
        foreach (var (m, c, yaw, nazwa) in lista)
        {
            try
            {
                var o = Postaw(sklep, m, nazwa, sklep.TransformPoint(new Vector3(c.x, F, c.y)), sklep.eulerAngles.y + yaw, 1f, true, true);
                var b = Granice(o);
                var col = new GameObject("kolizja: " + nazwa).AddComponent<BoxCollider>(); col.transform.SetParent(kolizje, false);
                col.transform.position = b.center; col.transform.rotation = sklep.rotation;
                var lok = sklep.InverseTransformVector(b.size); col.size = new Vector3(Mathf.Abs(lok.x), Mathf.Abs(lok.y), Mathf.Abs(lok.z)) * .92f;
                n++;
            }
            catch (Exception e) { Blad("BRAXHORN SPORT sprzęt " + nazwa, e); }
        }
        Log("BRAXHORN SPORT: sprzęt ze Sketchfaba " + n + "/4 (atlas, klatka z gryfem, bieżnia, ławka) — prymitywny stojak hantli z kodu ZDJĘTY");
        return n > 0;
    }

    // KLUB FITNESS w wieżowcach B (PARK TOWER p.2, szpital — rehabilitacja, GALERIA): zamiast bieżni i ławki z brył z kodu —
    // modele. p = środek stanowiska na posadzce, yaw = obrót (bieżnia długością wzdłuż lokalnego z). true = postawiony.
    public static bool Bieznia(Transform parent, Vector3 p, float yaw)
    {
        var m = Jest("miasto_silownia_metro") ? Model("miasto_silownia_metro", "silownia_bieznia") : null;
        if (!m) return false;
        var o = Postaw(parent, m, "Fitness: bieżnia (model)", p, yaw + 90f);
        BraxRegionMeble.Kolizja(parent, "Bieżnia kolizja", Granice(o).center, Granice(o).size * .95f);
        return true;
    }
    public static bool Lawka(Transform parent, Vector3 p, float yaw)
    {
        var m = Jest("miasto_silownia_metro") ? Model("miasto_silownia_metro", "silownia_lawka") : null;
        if (!m) return false;
        var o = Postaw(parent, m, "Fitness: ławka skośna (model)", p, yaw);
        BraxRegionMeble.Kolizja(parent, "Ławka kolizja", Granice(o).center, Granice(o).size * .95f);
        return true;
    }
    public static bool Klatka(Transform parent, Vector3 p, float yaw)
    {
        var m = Jest("miasto_silownia_metro") ? Model("miasto_silownia_metro", "silownia_klatka") : null;
        if (!m) return false;
        var o = Postaw(parent, m, "Fitness: klatka do przysiadów (model)", p, yaw);
        BraxRegionMeble.Kolizja(parent, "Klatka kolizja", Granice(o).center, Granice(o).size * .95f);
        return true;
    }

    // =====================================================================================================================
    // BRAMKI BILETOWE (dziś: wejście areny na wyspie; metra w mieście nie ma od 9.11): zamiast obudów i szybek z brył — model ericdavis (CC-BY):
    // jeden egzemplarz = dwie obudowy i przejście z 4 klapami; klapy chowają się w obudowy (BraxDrzwiPrzesuwne, jak dotąd),
    // blokada przejścia zamknięta, dopóki gracz nie podejdzie. Rząd n przejść wyśrodkowany na środku starego rzędu.
    // true = postawione (stare obudowy i szybki NIE powstają); zA/zB = krawędzie rzędu (do przegród szklanych).
    public static bool BramkiMetra(Transform podziemie, float gx, float yM, float srodekZ, int przejsc, out float zA, out float zB)
    {
        zA = zB = 0f;
        var m = Jest("miasto_silownia_metro") ? Model("miasto_silownia_metro", "metro_bramka") : null;
        if (!m) { Log("METRO: brak modelu bramki — zostają bramki z kodu"); return false; }
        // szerokość egzemplarza (wzdłuż z świata po obrocie 90°): z granic modelu
        var probny = (GameObject)UnityEngine.Object.Instantiate(m); probny.transform.localScale = Vector3.one * K;
        probny.transform.rotation = Quaternion.Euler(0, 90f, 0); var pb = BraxRegion.Bounds(probny);
        float krok = pb.size.z + .01f; UnityEngine.Object.DestroyImmediate(probny);
        zA = srodekZ - krok * przejsc * .5f; zB = srodekZ + krok * przejsc * .5f;
        int ok = 0;
        for (int i = 0; i < przejsc; i++)
        {
            try
            {
                var p = new Vector3(gx, yM, zA + krok * (i + .5f));
                var o = Postaw(podziemie, m, "bramka biletowa " + (i + 1) + " (model)", p, 90f, 1f, false, false);
                var b = BraxRegion.Bounds(o); o.transform.position += new Vector3(p.x - b.center.x, yM - b.min.y, p.z - b.center.z);
                // klapy → skrzydła drzwi przesuwnych (chowają się do swojej obudowy: kierunek do środka obudowy, 0.27 m × K)
                var drz = o.AddComponent<BraxDrzwiPrzesuwne>(); drz.etykieta = "bramka metra " + (i + 1); drz.promien = 1.9f; drz.czasRuchu = .35f;
                var sk = new List<Transform>(); var ps = new List<Vector3>();
                foreach (var nm in new[] { "frontDoor1", "backDoor1", "frontDoor2", "backDoor2" })
                {
                    var d = Znajdz(o.transform, nm); var obud = Znajdz(o.transform, "ticketGateMain" + nm.Substring(nm.Length - 1));
                    if (!d || !obud) continue;
                    var rd = d.GetComponentInChildren<Renderer>(); var ro = obud.GetComponentInChildren<Renderer>();
                    if (!rd || !ro) continue;
                    var kier = ro.bounds.center - rd.bounds.center; kier.y = 0; kier = Vector3.Project(kier, o.transform.right); if (kier.sqrMagnitude < 1e-6f) continue;
                    var swiat = kier.normalized * .27f * K;
                    sk.Add(d); ps.Add(d.parent ? d.parent.InverseTransformVector(swiat) : swiat);
                }
                drz.skrzydla = sk.ToArray(); drz.przesuniecie = ps.ToArray();
                var blok = new GameObject("blokada przejścia"); blok.transform.SetParent(o.transform, false);
                blok.transform.position = new Vector3(p.x, yM + .6f * K, p.z); blok.transform.rotation = Quaternion.identity;
                var bc = blok.AddComponent<BoxCollider>(); bc.size = new Vector3(.1f, 1.2f * K, krok * .55f) / K;
                drz.blokady = new Collider[] { bc };
                // obudowy: kolizje (obie obudowy egzemplarza)
                foreach (var nm in new[] { "ticketGateMain1", "ticketGateMain2" })
                {
                    var t = Znajdz(o.transform, nm); var r = t ? t.GetComponentInChildren<Renderer>() : null; if (!r) continue;
                    var c = new GameObject("kolizja obudowy").AddComponent<BoxCollider>(); c.transform.SetParent(podziemie, false);
                    c.transform.position = r.bounds.center; c.size = r.bounds.size;
                }
                if (sk.Count == 4) ok++;
            }
            catch (Exception e) { Blad("METRO bramka " + (i + 1), e); }
        }
        Log("METRO: bramki biletowe (model) " + ok + "/" + przejsc + " z klapami w BraxDrzwiPrzesuwne, rząd z " + zA.ToString("0.00") + "…" + zB.ToString("0.00") + " — stare obudowy i szybki z kodu NIE zbudowane");
        return ok > 0;
    }

    // =====================================================================================================================
    // PORT: drobnicowiec (127 m × K) przycumowany przy nabrzeżu B (x 84, burta 0.55 m od lica muru), dziobem na południe,
    // rufa 3 m za rufą kontenerowca A (z −30) — poza dźwigiem (z −29…−21) i daleko od mariny (z ≥ 21);
    // kontenerowiec 250 m × K na redzie (x 720, z 120 — za wyspą, widać z plaży i tarasu Tower). Linia wodna z zanurzenia
    // typowego dla statku (7 m i 12 m × K). Kolizja kadłuba nad wodą (łodzie i skuter się o nią opierają).
    public static void Port(Transform cityRoot)
    {
        if (!Jest("miasto_port")) { Log("PORT: brak paczki miasto_port — bez statków ze Sketchfaba"); return; }
        var root = BraxRegion.Root(cityRoot, "CLAUDE assety: statki (drobnicowiec przy nabrzeżu B, kontenerowiec na redzie)");
        float woda = BraxMiastoGranice.PoziomMorza;
        // 3.11 (właściciel 11:49: „ten zardzewiały… realistyczny mega… lepiej go dać przy porcie, do załadunku kontenerów, bo pusty”):
        // MASOWIEC ze skanu (190 m × K, 2 żurawie pokładowe, 3 LOD, tekstura 4K) przy nabrzeżu B zamiast drobnicowca — pusty
        // (zanurzenie balastowe 6 m × K). Drobnicowiec tylko, gdy paczki miasto_statki brak.
        bool masowiec = false;
        try
        {
            var m = Model("miasto_statki", "statek_masowiec");
            if (m)
            {
                var o = Postaw(root, m, "Statek: masowiec przy nabrzeżu B (zardzewiały, pusty — do załadunku)", Vector3.zero, -90f, 1f, true, true);
                LodyZWezlow(o, new[] { .30f, .08f, .004f });
                var b = Granice(o);
                float zanurzenie = 6.0f * K, rufaZ = -33f, burtaX = 84.08f + .55f;
                o.transform.position += new Vector3(burtaX - b.min.x, (woda - zanurzenie) - b.min.y, rufaZ - b.max.z);
                b = Granice(o);
                KadlubKolizja(root, "Masowiec: kolizja kadłuba", b, woda);
                int lad = LadPod(b);
                Log("PORT: masowiec (skan) " + b.size.z.ToString("0") + " × " + b.size.x.ToString("0") + " m, x " + b.min.x.ToString("0.0") + "…" + b.max.x.ToString("0.0") + ", z " + b.min.z.ToString("0") + "…" + b.max.z.ToString("0") + (lad > 0 ? " — DO POPRAWKI: ląd pod kadłubem w " + lad + " punktach" : ""));
                masowiec = true;
            }
        }
        catch (Exception e) { Blad("PORT masowiec", e); }
        if (!masowiec) try
        {
            var m = Model("miasto_port", "statek_drobnicowiec");
            if (m)
            {
                // model: długość wzdłuż x (glTF: dziób +x, nadbudówka i śruba −x; glTFast odbija x → w Unity dziób −x lokalnie),
                // dół = stępka; obrót −90° → rufa na północy (przy porcie A), dziób na południe
                var o = Postaw(root, m, "Statek: drobnicowiec przy nabrzeżu", Vector3.zero, -90f, 1f, true, true);
                var b = Granice(o);
                float zanurzenie = 7.0f * K, rufaZ = -33f, burtaX = 84.08f + .55f;
                o.transform.position += new Vector3(burtaX - b.min.x, (woda - zanurzenie) - b.min.y, rufaZ - b.max.z);
                b = Granice(o);
                Log("PORT: drobnicowiec " + b.size.z.ToString("0") + " × " + b.size.x.ToString("0") + " m, x " + b.min.x.ToString("0.0") + "…" + b.max.x.ToString("0.0") + ", z " + b.min.z.ToString("0") + "…" + b.max.z.ToString("0") + ", stępka y " + b.min.y.ToString("0.0") + ", linia wodna " + woda);
                var kad = new GameObject("Drobnicowiec: kolizja kadłuba").AddComponent<BoxCollider>(); kad.transform.SetParent(root, false);
                kad.transform.position = new Vector3(b.center.x, woda + 3.5f * K * .5f, b.center.z); kad.size = new Vector3(b.size.x * .92f, 3.5f * K + 2f, b.size.z * .9f);
            }
        }
        catch (Exception e) { Blad("PORT drobnicowiec", e); }
        // 3.11 (właściciel 11:46: „kontenerowiec zjebałeś… on jest 4K”): KONTENEROWIEC w jakości oryginału (189 tys. trójkątów,
        // tekstury 4K, 3 LOD) na kotwicy ~250 m przed portem (za długi na nabrzeże — 323 m), równolegle do brzegu, poza torem
        // wodnym na wyspę (z 40–60). WYCIECZKOWIEC (atrapa, logo BRAXHORN CRUISES) na redzie, wzdłuż x, poza torem.
        try
        {
            var m = Model("miasto_statki", "statek_kontenerowiec");
            if (m)
            {
                var o = Postaw(root, m, "Statek: kontenerowiec na kotwicy przed portem", Vector3.zero, -90f, 1f, true, false);
                LodyZWezlow(o, new[] { .25f, .07f, .003f });
                var b = Granice(o); var cel = new Vector3(330f, 0f, -150f);
                o.transform.position += new Vector3(cel.x - b.center.x, (woda - 12f * K) - b.min.y, cel.z - b.center.z);
                b = Granice(o);
                for (int i = 0; i < 10 && LadPod(b) > 0; i++) { o.transform.position += Vector3.right * 40f; b = Granice(o); }
                KadlubKolizja(root, "Kontenerowiec: kolizja kadłuba", b, woda);
                Log("PORT: kontenerowiec HD na kotwicy " + b.size.z.ToString("0") + " × " + b.size.x.ToString("0") + " m, środek (" + b.center.x.ToString("0") + ", " + b.center.z.ToString("0") + ")");
            }
        }
        catch (Exception e) { Blad("PORT kontenerowiec HD", e); }
        try
        {
            var m = Model("miasto_statki2", "statek_wycieczkowiec");
            if (m)
            {
                var o = Postaw(root, m, "Statek: wycieczkowiec BRAXHORN CRUISES na redzie", Vector3.zero, 0f, 1f, true, false);
                LodyZWezlow(o, new[] { .22f, .06f, .003f });
                var b = Granice(o); var cel = new Vector3(650f, 0f, 175f);
                o.transform.position += new Vector3(cel.x - b.center.x, (woda - 9.3f * K) - b.min.y, cel.z - b.center.z);
                b = Granice(o);
                KadlubKolizja(root, "Wycieczkowiec: kolizja kadłuba", b, woda);
                Log("PORT: wycieczkowiec na redzie " + b.size.x.ToString("0") + " × " + b.size.z.ToString("0") + " m, środek (" + b.center.x.ToString("0") + ", " + b.center.z.ToString("0") + "), z " + b.min.z.ToString("0") + "…" + b.max.z.ToString("0"));
            }
        }
        catch (Exception e) { Blad("PORT wycieczkowiec", e); }
        try
        {
            // stary kontenerowiec (1024 px, zdecymowany) — wyłączony na stałe
            var m = KontenerowiecNaRedzie ? Model("miasto_port", "statek_kontenerowiec") : null;
            if (!KontenerowiecNaRedzie) Log("PORT: kontenerowiec na redzie WYŁĄCZONY (właściciel 3.11)");
            if (m)
            {
                var o = Postaw(root, m, "Statek: kontenerowiec na redzie", Vector3.zero, 90f, 1f, true, false);
                var b = Granice(o);
                o.transform.position += new Vector3(720f - b.center.x, (woda - 12f * K) - b.min.y, 120f - b.center.z);
                b = Granice(o);
                // właściciel 3.11 (nagranie): motorówka przepływała PRZEZ kadłub — kolizja kadłuba jak przy drobnicowcu
                var kad = new GameObject("Kontenerowiec: kolizja kadłuba").AddComponent<BoxCollider>(); kad.transform.SetParent(root, false);
                kad.transform.position = new Vector3(b.center.x, woda + 3.5f * K * .5f, b.center.z); kad.size = new Vector3(b.size.x * .92f, 3.5f * K + 2f, b.size.z * .9f);
                Log("PORT: kontenerowiec na redzie " + b.size.z.ToString("0") + " m, środek (" + b.center.x.ToString("0") + ", " + b.center.z.ToString("0") + "), kolizja kadłuba " + kad.size.x.ToString("0") + " × " + kad.size.z.ToString("0") + " m");
            }
        }
        catch (Exception e) { Blad("PORT kontenerowiec", e); }
    }
}
