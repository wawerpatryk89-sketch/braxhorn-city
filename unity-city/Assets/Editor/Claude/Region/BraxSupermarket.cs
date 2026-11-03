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
            if (!Szukaj(miejsca, out var c, out float yaw)) { Debug.LogWarning("BRAXHORN_SUPERMARKET kopia " + (i + 1) + ": brak wolnej działki — DO POPRAWKI (miejsce ręczne)"); break; }
            miejsca.Add(c);
            var t = new GameObject("SUPERMARKET BRAXHORN " + (i + 1)).transform; t.SetParent(root.transform, false);
            t.position = c; t.rotation = Quaternion.Euler(0f, yaw, 0f);
            bool lustro = i % 2 == 1;
            int towar = Zbuduj(t, lustro, i + 1);
            Physics.SyncTransforms();
            Debug.Log("BRAXHORN_SUPERMARKET kopia " + (i + 1) + ": front (środek) " + c.ToString("F1") + ", obrót " + yaw + "°, hala " + W.ToString("F1") + " × " + D.ToString("F1") + " × " + H.ToString("F1") + " m"
                      + (lustro ? ", odbicie lustrzane (wejście i kasy po drugiej stronie)" : "") + ", towar modeli " + towar + ", szukanie " + stoper.ElapsedMilliseconds + " ms");
        }
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

    static bool Pasuje(Vector2 q, float yaw, out float y)
    {
        y = 0f;
        var rot = Quaternion.Euler(0f, yaw, 0f); Vector3 R = rot * Vector3.right, F = rot * Vector3.forward;
        Vector3 P(float x, float z) => new Vector3(q.x, 0f, q.y) + R * x + F * z;
        // 1) ląd pod całą halą z marginesem 4 m (najpierw najtańszy test)
        for (float x = -W * .5f - 4f; x <= W * .5f + 4.01f; x += 4f)
            for (float z = -4f; z <= D + 4.01f; z += 4f) { var p = P(x, z); if (!BraxMiastoGranice.Lad(p.x, p.z)) return false; }
        // 2) jezdnia przed frontem (6–16 m od witryny, na osi wejścia) — wejście od ulicy
        bool droga = false;
        for (float z = -6f; z >= -16f && !droga; z -= 2f) droga = BraxRegionMiastoB.NaJezdni(P(0f, z));
        if (!droga) return false;
        // 3) żadnej jezdni pod halą ani w pasie 4 m wokół
        for (float x = -W * .5f - 4f; x <= W * .5f + 4.01f; x += 4f)
            for (float z = -4f; z <= D + 4.01f; z += 4f) if (BraxRegionMiastoB.NaJezdni(P(x, z))) return false;
        // 4) nic nad gruntem (budynki, drzewa, latarnie, auta, ławki, płoty)
        float yProbe = BraxRegion.Ground(q.x, q.y, -20f, 80f, out var h0) ? h0.point.y : 0f;
        var srodek = P(0f, D * .5f) + Vector3.up * (yProbe + .45f + H * .5f);
        if (Physics.OverlapBox(srodek, new Vector3(W * .5f + 1.5f, H * .5f, D * .5f + 1.5f), rot, ~0, QueryTriggerInteraction.Ignore).Length > 0) return false;
        // 5) grunt płaski (rozrzut ≤ 0.6 m) — hala stoi na najwyższym punkcie
        float lo = float.MaxValue, hi = float.MinValue;
        for (int i = 0; i <= 6; i++)
            for (int j = 0; j <= 5; j++)
            {
                var p = P(-W * .5f + W * i / 6f, D * j / 5f);
                if (!BraxRegion.Ground(p.x, p.z, -20f, 80f, out var h)) return false;
                lo = Mathf.Min(lo, h.point.y); hi = Mathf.Max(hi, h.point.y);
            }
        if (hi - lo > .6f) return false;
        y = hi;
        return true;
    }

    // ---------------- budowa jednej kopii ----------------
    static int Zbuduj(Transform t, bool lustro, int nr)
    {
        float L(float x) => lustro ? -x : x;
        float hw = W * .5f, xe = L(hw - 5f * K), wd = 2.4f * K, hd = 2.6f * K, yC = .5f * K, yS = 3.4f * K, yA = H + 1.4f * K;
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
        BraxRegionMeble.Napis(t, "SUPERMARKET BRAXHORN", Swiat(0f, ys - .35f * K, -.08f), -t.forward, .95f * K, new Color(.98f, .96f, .90f));
        // oprawy LED pod sufitem (siatka)
        for (float x = -hw + 2.5f * K; x < hw - 2f * K; x += 3.5f * K)
            for (float z = 2.5f * K; z < D - 1.5f * K; z += 3.5f * K) sL.Box(new Vector3(x - .6f * K, H - .04f, z - .15f * K), new Vector3(x + .6f * K, H, z + .15f * K), 8);

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
                        BraxSklepyTowar.Polka(s.typ, tw, start, t.forward, -sg * t.right, (s.z1 - s.z0 - .06f) * K, rng, wolne);
                    }
            BraxSklepyTowar.Zakoncz(tw, "supermarket " + nr + " regał " + (i + 1));
            towar += tw.GetComponentsInChildren<Renderer>(true).Length;
        }
        // lodówki pod tylną ścianą (front modelu = −z → w stronę sklepu)
        var lod = BraxMiastoAssety.Model("miasto_sklepy", "lodowka");
        if (lod)
        {
            float sz = .854f * K + .02f, zl = D - Grub - .944f * K * .5f - .03f; int n = 0;
            for (float x = -hw + Grub + .4f + sz * .5f; x < hw - Grub - .4f - sz * .5f; x += sz, n++)
                BraxMiastoAssety.Postaw(wyp, lod, "Lodówka " + (n + 1), Swiat(x, Podloga, zl), yaw0, 1f, true, true);
            Kol("lodówki", new Vector3(-hw + Grub + .4f, Podloga, zl - .944f * K * .5f), new Vector3(hw - Grub - .4f, Podloga + 2.184f * K, zl + .944f * K * .5f));
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

        // siatki do sceny
        void Obj(string n, UlSiatka s, Material m, bool cien)
        {
            if (s.Wierzcholki == 0) return;
            var g = BraxMiastoA.Obiekt(t, "Supermarket " + nr + ": " + n, s, m, cien);
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | (cien ? StaticEditorFlags.OccluderStatic : 0));
        }
        Obj("posadzka", sP, mPos, false); Obj("ściany", sS, mSc, true); Obj("elewacja i dach", sF, mFas, true); Obj("ramy witryny", sR, mRam, true);
        Obj("szyby witryny", sG, mSz, false); Obj("oprawy LED", sL, mLamp, false); Obj("podświetlenie szyldu", sPas, mPas, false); Obj("stoły warzyw", sT, mDrew, true);
        BraxMiastoA.DrzwiPrzesuwne(t, "SUPERMARKET BRAXHORN " + nr, Swiat(xe, Podloga, .11f), t.right, wd, hd, mSz, mRam);
        BraxUliceKit.Punkt("supermarket_" + nr + "_wejscie", "przed wejściem: SUPERMARKET BRAXHORN " + nr + " — drzwi automatyczne, wózki", Swiat(xe, 0f, -2.5f * K));
        BraxUliceKit.Punkt("supermarket_" + nr + "_srodek", "w środku: SUPERMARKET BRAXHORN " + nr + " — alejki, kasy, lodówki", Swiat(L(-1.5f * K), Podloga, 9.5f * K));
        return towar;
    }
}
