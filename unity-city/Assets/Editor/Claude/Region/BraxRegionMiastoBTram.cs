using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — T1: jedna zamknieta linia po ulicach nowych dzielnic.
// Tramwajowa -> C0 -> Nowojorska -> C3 -> Tramwajowa; zadnego biegu wstecznego.
// R=26 m w czterech lukach; ustawienie tak, by dlugi model CAF nie dotykal
// zinwentaryzowanych bryl nowych kwartalow (ostateczna kontrola w Unity/Android).
public static partial class BraxRegionMiastoB
{
    const float T1R = 26f;
    static Vector3[] t1Trasa;

    static Vector3[] NdTrasaT1()
    {
        if (t1Trasa != null && t1Trasa.Length > 100) return t1Trasa;
        float ex = NC[0] + AH * .5f, wx = NC[3] - AH * .5f;
        float sz = NS1 - AH * .5f, nz = NN1 - AH * .5f;
        float rad = T1R;
        var q = new List<Vector3>(1400);
        void Prosta(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(d / 1.5f));
            for (int i = 0; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                if (q.Count == 0 || Vector3.Distance(q[q.Count - 1], new Vector3(p.x, YA, p.y)) > .04f)
                    q.Add(new Vector3(p.x, YA, p.y));
            }
        }
        void Luk(float x, float z, float a, float b)
        {
            int n = Mathf.CeilToInt(Mathf.Abs(b - a) * rad / 1.5f);
            for (int i = 0; i < n; i++)
            {
                float k = Mathf.Lerp(a, b, i / (float)n);
                var p = new Vector3(x + rad * Mathf.Cos(k), YA, z + rad * Mathf.Sin(k));
                if (q.Count == 0 || Vector3.Distance(q[q.Count - 1], p) > .04f) q.Add(p);
            }
        }
        // Południe: Tramwajowa -> wschód.
        Prosta(new Vector2(wx + rad, sz), new Vector2(ex - rad, sz));
        Luk(ex - rad, sz + rad, -Mathf.PI * .5f, 0f);
        // Wschód: C0 -> północ.
        Prosta(new Vector2(ex, sz + rad), new Vector2(ex, nz - rad));
        Luk(ex - rad, nz - rad, 0f, Mathf.PI * .5f);
        // Północ: Nowojorska -> zachód; pas południowy unika fasad rzędu B.
        Prosta(new Vector2(ex - rad, nz), new Vector2(wx + rad, nz));
        Luk(wx + rad, nz - rad, Mathf.PI * .5f, Mathf.PI);
        // Zachód: C3 -> południe.
        Prosta(new Vector2(wx, nz - rad), new Vector2(wx, sz + rad));
        Luk(wx + rad, sz + rad, Mathf.PI, Mathf.PI * 1.5f);
        t1Trasa = q.ToArray();
        return t1Trasa;
    }

    static int NdIndeksTram(Vector3[] punkty, float x, float z)
    {
        int best = 0; float dd = float.MaxValue;
        var q = new Vector3(x, YA, z);
        for (int i = 0; i < punkty.Length; i++)
        {
            float d = (punkty[i] - q).sqrMagnitude;
            if (d < dd) { dd = d; best = i; }
        }
        return best;
    }

    // Wstawki stalowej główki i rowka 13 mm nad jezdnią.
    // Nie budujemy drugiego asfaltu na starej nawierzchni: jego jedynym
    // właścicielem pozostaje BudujAsfalt. Stal to fizyczna część toru,
    // a nie nakładana na stare fasady nowa tekstura.
    static void NdSzynyTramT1()
    {
        var trasa = NdTrasaT1();
        float gauge = Rozstaw * K;
        var metal = BraxCityMaterials.Surface("T1: stal szynowa szlifowana",
            new Color(.57f, .59f, .61f), .25f, .80f);
        var rowek = BraxCityMaterials.Surface("T1: rowek szyny",
            new Color(.10f, .105f, .11f), .90f, .18f);
        var steel = new BraxRegionMeble.Bud();
        var dark = new BraxRegionMeble.Bud();
        Vector3 P(Vector3 v, float y) => new Vector3(v.x, y, v.z);
        void Pas(BraxRegionMeble.Bud b, Vector3 p0, Vector3 p1, Vector3 side,
                 float along, float width, float y)
        {
            var a = P(p0 + side * (along - width * .5f), y);
            var b0 = P(p1 + side * (along - width * .5f), y);
            var b1 = P(p1 + side * (along + width * .5f), y);
            var a1 = P(p0 + side * (along + width * .5f), y);
            b.Quad(a, b0, b1, a1);
        }
        int cnt = 0;
        for (int i = 0; i < trasa.Length; i++)
        {
            var a = trasa[i]; var b = trasa[(i + 1) % trasa.Length];
            var dir = b - a; dir.y = 0;
            if (dir.sqrMagnitude < .02f) continue;
            var side = Vector3.Cross(Vector3.up, dir.normalized);
            foreach (float off in new[] { -gauge * .5f, gauge * .5f })
            {
                Pas(dark, a, b, side, off, .115f * K, YA + .003f);
                Pas(steel, a, b, side, off, .070f * K, YA + .015f);
            }
            // Osobne meshe co ~120m: zwiedzanie Android nie renderuje calych 1.2km naraz.
            if (++cnt % 80 == 0 || i == trasa.Length - 1)
            {
                steel.Obiekt("T1: szyny jezdne odcinek " + cnt / 80, root, metal, false, false);
                dark.Obiekt("T1: rowki szyn odcinek " + cnt / 80, root, rowek, false, false);
                steel = new BraxRegionMeble.Bud(); dark = new BraxRegionMeble.Bud();
            }
        }
        NdLog("TRAM_T1_TRASA geometria zamknieta: punkty=" + trasa.Length +
              ", promien lukow=" + T1R.ToString("F1") +
              " m, szyny bez podwojnego asfaltu, podzielone na male paczki do cullingu");
    }
    // Sześć przystanków rozmieszczonych przy chodnikach obwodnicy nowych dzielnic.
    // Dane poziome są wykorzystywane wspólnie: T1, tablice, perony, mapka i QA.
    static readonly (string nazwa, float x, float z)[] T1Postoje =
    {
        ("TRAMWAJOWA ZACHÓD", -690f, NS1 - AH * .5f),
        ("TRAMWAJOWA CENTRUM", -485f, NS1 - AH * .5f),
        ("C0 — NOWA DZIELNICA", -300f + AH * .5f, 18f),
        ("NOWOJORSKA WSCHÓD", -425f, NN1 - AH * .5f),
        ("NOWOJORSKA ZACHÓD", -660f, NN1 - AH * .5f),
        ("C3 — ALEJA CENTRALNA", -750f - AH * .5f, 18f)
    };

    static void NdTramT1Buduj()
    {
        var trasa = NdTrasaT1();
        var indeksy = new int[T1Postoje.Length];
        var nazwy = new string[T1Postoje.Length];
        var stopRoot = new GameObject("T1: 6 przystanków, od południa przez Nowojorską").transform;
        stopRoot.SetParent(root, false);
        var metal = BraxCityMaterials.Surface("T1: tablice stal grafit",
            new Color(.12f, .16f, .20f), .36f, .65f);
        var srebro = BraxCityMaterials.Surface("T1: aluminium szczotkowane",
            new Color(.66f, .69f, .70f), .40f, .68f);
        var wiaty = BraxMiastoAssety.Jest("miasto_wyposazenie") ?
            BraxMiastoAssety.Model("miasto_wyposazenie", "miasto_wyposazenie") : null;
        int wiatyGotowe = 0;
        for (int i = 0; i < T1Postoje.Length; i++)
        {
            var cfg = T1Postoje[i];
            indeksy[i] = NdIndeksTram(trasa, cfg.x, cfg.z);
            nazwy[i] = cfg.nazwa;
            var s = trasa[indeksy[i]];
            var przed = trasa[(indeksy[i] - 3 + trasa.Length) % trasa.Length];
            var dalej = trasa[(indeksy[i] + 3) % trasa.Length];
            var tangent = (dalej - przed).normalized;
            var prawa = Vector3.Cross(Vector3.up, tangent).normalized;
            // Prawa strona toru = przystanek na zewnętrznym chodniku (pojazd jedzie w jedną stronę).
            var peron = s + prawa * (AH * .5f + Kr + 1.55f);
            peron.y = YC;
            var p = new GameObject("T1: przystanek " + (i + 1) + " " + cfg.nazwa).transform;
            // Mesh Bud i Napis otrzymują współrzędne świata. Rodzic w (0,0,0)
            // zapobiega PODWÓJNEMU przesunięciu wiat i tablic.
            p.SetParent(stopRoot, false);
            p.localPosition = Vector3.zero;
            var znak = new BraxRegionMeble.Bud();
            znak.Cyl(peron, peron + Vector3.up * 2.9f * K, .045f * K, 12);
            znak.MinMax(peron + new Vector3(-.09f, 2.65f * K, -.09f),
                        peron + new Vector3(.09f, 2.75f * K, .09f));
            znak.Obiekt("T1: słup przystanku " + (i + 1), p, srebro, false, true);
            BraxRegionMeble.Napis(p, "T1  " + cfg.nazwa,
                peron + Vector3.up * 2.9f * K + prawa * .05f,
                prawa, .17f * K, new Color(.94f, .94f, .92f));
            // Wiata z prawdziwego pakietu. Brak paczki = jawny log, nie plastikowy zamiennik.
            if (wiaty)
            {
                var kopia = (GameObject)UnityEngine.Object.Instantiate(wiaty);
                var t = BraxMiastoAssety.Znajdz(kopia.transform, "wiata_przystanku");
                if (t)
                {
                    t.SetParent(p, true);
                    t.name = "T1: wiata " + cfg.nazwa;
                    t.position = peron + prawa * 3.8f;
                    t.rotation = Quaternion.LookRotation(-prawa, Vector3.up);
                    t.localScale = Vector3.one * K;
                    BraxMiastoAssety.Przygotuj(t.gameObject);
                    BraxMiastoAssety.Kolizja(p, t.gameObject, "T1: kolizja wiaty " + cfg.nazwa, .25f);
                    wiatyGotowe++;
                }
                UnityEngine.Object.DestroyImmediate(kopia);
            }
        }

        // JEDEN rzeczywisty model CAF. Root i jedyna ruchoma kolizja mają skalę 1.
        var tram = BraxMiastoAssety.Jest("miasto_pojazdy") ?
            BraxMiastoAssety.Model("miasto_pojazdy", "tramwaj_caf") : null;
        if (!tram)
        {
            Debug.LogError("BRAXHORN_TRAM_T1_NIEGOTOWY: nie wczytano tramwaj_caf z paczki miasto_pojazdy");
            return;
        }
        var pojazd = new GameObject("T1 — tramwaj CAF, cała nowa dzielnica").transform;
        pojazd.SetParent(root, false);
        var body = BraxMiastoAssety.Postaw(pojazd, tram, "T1: oryginalny model CAF",
            Vector3.zero, 0f, 1f, false, true);
        if (!body) { UnityEngine.Object.DestroyImmediate(pojazd.gameObject); return; }
        var before = BraxRegion.Bounds(body);
        // Sprowadzamy środek bryły do zera root wagonu; tor w świecie na y=YA.
        body.transform.position += new Vector3(-before.center.x, -before.min.y, -before.center.z);
        foreach (var t in body.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        foreach (var c in body.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c); // stare, przypadkowe collidery z importu
        var bounds = BraxRegion.Bounds(body);
        var kol = pojazd.gameObject.AddComponent<BoxCollider>();
        kol.center = new Vector3(0f, bounds.size.y * .5f, 0f);
        kol.size = bounds.size * .96f;
        var rb = pojazd.gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var driver = pojazd.gameObject.AddComponent<BraxTramwajT1>();
        driver.trasa = trasa;
        driver.przystankiIndeksy = indeksy;
        driver.nazwyPrzystankow = nazwy;
        driver.szerokoscWagonu = Mathf.Min(bounds.size.x, bounds.size.z);
        driver.modelForward = bounds.size.x > bounds.size.z ? Vector3.right : Vector3.forward;
        driver.szybkosc = 8.5f; driver.postojSekundy = 10f;
        driver.promienWejscia = 5.8f;

        // Autentyczne (osobne) skrzydła modelu poruszamy tylko gdy model ma
        // wyodrębnione elementy. Nie tworzymy fasadowych drzwi na wierzchu CAF.
        var doorNodes = body.GetComponentsInChildren<Transform>(true)
            .Where(t => t && t != body.transform && (
                t.name.IndexOf("door_left", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.name.IndexOf("door_right", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.name.IndexOf("drzwi_lew", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.name.IndexOf("drzwi_praw", StringComparison.OrdinalIgnoreCase) >= 0) &&
                (t.GetComponentsInChildren<MeshFilter>(true).Any(f => f && f.sharedMesh) ||
                 t.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r && r.sharedMesh)))
            .ToArray();
        // Keep the named moving root, not both it and a nested named child.
        doorNodes = doorNodes.Where(t => !doorNodes.Any(parent => parent != t && t.IsChildOf(parent))).ToArray();
        driver.skrzydlaDrzwi = doorNodes;
        driver.kierunkiDrzwi = new Vector3[doorNodes.Length];
        for (int i = 0; i < doorNodes.Length; i++)
            driver.kierunkiDrzwi[i] = i % 2 == 0 ? Vector3.left : Vector3.right;
        if (doorNodes.Length == 0)
            Debug.LogWarning("BRAXHORN_TRAM_T1_DRZWI_NIEGOTOWE: brak odrębnych węzłów drzwi modelu CAF");

        // Prawdziwa pozująca siedząca postać gracza; widok kamery jedzie z tramwajem.
        var sit = new GameObject("T1: miejsce pasażera").transform;
        sit.SetParent(pojazd, false);
        sit.localPosition = new Vector3(0f, 0f, 0f);
        sit.localRotation = Quaternion.FromToRotation(Vector3.forward, driver.modelForward);
        var seat = sit.gameObject.AddComponent<BraxSeat>();
        seat.tramOwner = driver;
        seat.label = "TRAMWAJ T1"; seat.surfaceY = YA + 1.1f * K;
        driver.miejsceGracza = seat;
        var pasazerowie = pojazd.gameObject.AddComponent<BraxTramPasazerowie>();
        pasazerowie.tram = driver;
        pasazerowie.limit = 2; // tylko dwie dodatkowe osoby Rocketbox blisko kamery

        NdKontrolaT1Bryly(trasa, Mathf.Max(bounds.size.x, bounds.size.z),
            Mathf.Min(bounds.size.x, bounds.size.z));

        double dl = 0d;
        for (int i = 0; i < trasa.Length; i++)
            dl += Vector3.Distance(trasa[i], trasa[(i + 1) % trasa.Length]);
        Debug.Log("BRAXHORN_TRAM_T1_PETLA_OK obwod=" + dl.ToString("F1") +
            " m, stacje=" + nazwy.Length + ", wiaty=" + wiatyGotowe +
            ", CAF=" + bounds.size.ToString("F1") +
            ", drzwi_animowalne=" + doorNodes.Length +
            ", wejscie AKCJA, wysiadanie tylko na stacjach");
    }

    // Kontrola RZECZYWISTEGO bryłowego obrysu CAF podczas jazdy po łukach 26 m.
    // SAT czterech osi na rzucie XZ: nie wystarczy sprawdzić sam środek toru,
    // bo 34-metrowy wagon zachodzi przodem na chodnik i narożniki budynków.
    static void NdKontrolaT1Bryly(Vector3[] trasa, float dlugosc, float szerokosc)
    {
        if (trasa == null || trasa.Length < 20 || ndBudynki == null || ndBudynki.Count != NoweBudynki.Length)
            throw new InvalidOperationException("BRAXHORN_TRAM_T1_KONTROLA_NIEGOTOWA: brak kompletnej trasy lub 21 budynków");
        float polL = dlugosc * .5f + .3f, polW = szerokosc * .5f + .3f;
        var coll = new List<string>();
        int samples = 0;
        foreach (var bud in ndBudynki)
        {
            if (!bud) continue;
            var b = BraxRegion.Bounds(bud);
            var p = new Vector2(b.center.x, b.center.z);
            var ext = new Vector2(b.extents.x, b.extents.z);
            if (ext.x < .5f || ext.y < .5f) continue;
            bool trafiony = false;
            for (int i = 0; i < trasa.Length; i += 3)
            {
                samples++;
                var pos = new Vector2(trasa[i].x, trasa[i].z);
                var v1 = trasa[(i + 3) % trasa.Length];
                var v0 = trasa[(i + trasa.Length - 3) % trasa.Length];
                var d = new Vector2(v1.x - v0.x, v1.z - v0.z).normalized;
                if (d.sqrMagnitude < .001f) continue;
                var boczny = new Vector2(-d.y, d.x);
                var delta = p - pos;
                // Rzut wagonu i budynku na cztery osie, zgodnie z twierdzeniem
                // o osiach rozdzielających (SAT, OBB wagon vs AABB fasada).
                bool Obok(Vector2 axis)
                {
                    float c = Mathf.Abs(Vector2.Dot(delta, axis));
                    float a = polL * Mathf.Abs(Vector2.Dot(d, axis)) +
                              polW * Mathf.Abs(Vector2.Dot(boczny, axis));
                    float e = ext.x * Mathf.Abs(axis.x) + ext.y * Mathf.Abs(axis.y);
                    return c > a + e;
                }
                if (Obok(d) || Obok(boczny) || Obok(Vector2.right) || Obok(Vector2.up))
                    continue;
                trafiony = true;
                break;
            }
            if (trafiony)
            {
                coll.Add(bud.name);
                Debug.LogError("BRAXHORN_TRAM_T1_BUDYNEK_KOLIZJA " + bud.name +
                               " (kontrola całej bryły wagonu; wymagane przesunięcie toru/budynku przed APK)");
            }
        }
        Debug.Log("BRAXHORN_TRAM_T1_OBRYS bryla=" + dlugosc.ToString("F1") + "x" +
                  szerokosc.ToString("F1") + " m, próbki=" + samples +
                  ", kolizje_z_budynkami=" + coll.Count +
                  (coll.Count > 0 ? " | " + string.Join(", ", coll) : " — OK"));
        if (coll.Count > 0)
            throw new InvalidOperationException("BRAXHORN_TRAM_T1_BUDYNEK_KOLIZJA: " + coll.Count +
                " budynków | " + string.Join(", ", coll) + " — poprawić trasę lub rozmieszczenie przed APK");
    }

}
