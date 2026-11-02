using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — mała architektura nabrzeża w SKALI BRAXA: każdy wymiar to wymiar rzeczywisty (norma / katalog) × k.
// Geometria trafia do Zbioru (siatki scalane per materiał), kolizje osobno (pudełka), siedziska jako BraxSeat.
//   balustrada 1,10 m · poręcz schodów 0,90 m · stopień 0,15 × 0,30 m · ławka: siedzisko 0,45 m, długość 1,80 m ·
//   leżak 1,95 × 0,65 m · parasol 2,30 m, czasza Ø 2,40 m · kosz 0,95 m · latarnia 5,00 m · prysznic 2,20 m ·
//   poler cumowniczy 0,55 m · knaga 0,30 m · koło ratunkowe Ø 0,75 m.
public static class BraxNabrzezeMebel
{
    static float K => BraxNabrzeze.K;

    public sealed class Mat
    {
        public Material stal, stalCiemna, drewno, beton, granit, plotno0, plotno1, plotno2, plotno3, recznik0, recznik1, bialy, czerwony, zolty, guma, szklo, zielen;
    }

    public static Mat Materialy()
    {
        var m = new Mat();
        m.stal = BraxNabrzeze.Kolor("Nabrzeże: stal nierdzewna szczotkowana", new Color(.62f, .64f, .65f), .32f, .9f);
        m.stalCiemna = BraxNabrzeze.Kolor("Nabrzeże: stal malowana grafit", new Color(.16f, .18f, .19f), .55f, .45f);
        m.drewno = BraxNabrzeze.Mat("Nabrzeże: drewno egzotyczne (ławki, leżaki)", 1.2f, new Color(.92f, .82f, .70f), "teak_veneer", "wood_planks_grey", "wooden_planks");
        m.beton = BraxNabrzeze.Mat("Nabrzeże: beton architektoniczny", 2.2f, new Color(.86f, .85f, .82f), "concrete_wall_008", "concrete_floor_02");
        m.granit = BraxNabrzeze.Mat("Nabrzeże: granit płomieniowany", 1.6f, new Color(.92f, .91f, .9f), "granite_tile_02", "granite_tile", "concrete_floor_02");
        m.plotno0 = BraxNabrzeze.Mat("Parasol: płótno ecru", .6f, new Color(.95f, .92f, .84f), "rough_linen");
        m.plotno1 = BraxNabrzeze.Mat("Parasol: płótno morskie", .6f, new Color(.16f, .45f, .52f), "rough_linen");
        m.plotno2 = BraxNabrzeze.Mat("Parasol: płótno w pasy", .6f, new Color(.95f, .95f, .95f), "gingham_check", "rough_linen");
        m.plotno3 = BraxNabrzeze.Mat("Parasol: płótno granat", .6f, new Color(.13f, .19f, .36f), "rough_linen");
        m.recznik0 = BraxNabrzeze.Mat("Ręcznik: frotte pomarańcz", .45f, new Color(.93f, .55f, .2f), "terry_cloth", "rough_linen");
        m.recznik1 = BraxNabrzeze.Mat("Ręcznik: frotte błękit", .45f, new Color(.28f, .6f, .85f), "terry_cloth", "rough_linen");
        m.bialy = BraxNabrzeze.Kolor("Nabrzeże: biały lakier", new Color(.86f, .87f, .85f), .5f);
        m.czerwony = BraxNabrzeze.Kolor("Nabrzeże: czerwony sygnałowy", new Color(.72f, .1f, .07f), .55f);
        m.zolty = BraxNabrzeze.Kolor("Nabrzeże: żółty sygnałowy", new Color(.93f, .72f, .08f), .5f);
        m.guma = BraxNabrzeze.Kolor("Nabrzeże: guma odbojnicy", new Color(.05f, .05f, .055f), .9f);
        m.szklo = BraxNabrzeze.Kolor("Nabrzeże: szkło przydymione", new Color(.22f, .30f, .33f, .45f), .1f, .2f);
        m.zielen = BraxNabrzeze.Kolor("Nabrzeże: zieleń okuć", new Color(.12f, .22f, .18f), .6f, .3f);
        return m;
    }

    static Vector3 L(Transform t, float x, float y, float z) => t.TransformPoint(new Vector3(x, y, z));

    // ---------------- balustrada (1,10 m × k): słupki, pochwyt, dwie przeciągi, wypełnienie prętami co 11 cm × k ----------------
    /// <summary>Balustrada wzdłuż odcinka a→b (y = poziom nawierzchni). kolizja: niewidzialna ściana 2,2 m (nie wejdzie się na nią ani nie przeskoczy).</summary>
    public static void Balustrada(BraxNabrzeze.Zbior z, Transform kol, Mat m, Vector3 a, Vector3 b, Material kolor = null, bool pretami = true)
    {
        var mat = kolor ? kolor : m.stal;
        float h = 1.10f * K, len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
        if (len < .2f) return;
        var dir = (new Vector3(b.x, 0, b.z) - new Vector3(a.x, 0, a.z)).normalized;
        int nPost = Mathf.Max(1, Mathf.CeilToInt(len / (1.5f * K)));
        var s = z[mat];
        for (int i = 0; i <= nPost; i++)
        {
            var p = Vector3.Lerp(a, b, (float)i / nPost);
            s.Rura(p, p + Vector3.up * h, .024f * K, 8);
            s.Box(p + Vector3.up * .006f, new Vector3(.12f, .012f, .12f) * K);   // stopka słupka
        }
        s.Rura(a + Vector3.up * h, b + Vector3.up * h, .026f * K, 10);       // pochwyt
        s.Rura(a + Vector3.up * (h * .52f), b + Vector3.up * (h * .52f), .012f * K, 6);
        s.Rura(a + Vector3.up * .1f * K, b + Vector3.up * .1f * K, .012f * K, 6);
        if (pretami)
        {
            int nBar = Mathf.FloorToInt(len / (.115f * K));
            for (int i = 1; i < nBar; i++)
            {
                var p = Vector3.Lerp(a, b, (float)i / nBar);
                s.Rura(p + Vector3.up * .1f * K, p + Vector3.up * (h - .03f), .008f * K, 5, -1, false);
            }
        }
        if (kol)
        {
            var c = (a + b) * .5f; float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            BraxNabrzeze.Kol(kol, "Balustrada", new Vector3(c.x, (a.y + b.y) * .5f + 1.1f, c.z), new Vector3(.25f, 2.2f, len), yaw);
        }
    }

    // ---------------- poręcz schodów / rampy (0,90 m × k nad krawędzią stopnia) ----------------
    public static void Porecz(BraxNabrzeze.Zbior z, Mat m, Vector3 a, Vector3 b)
    {
        float h = .90f * K; var s = z[m.stal];
        s.Rura(a + Vector3.up * h, b + Vector3.up * h, .021f * K, 10);
        float len = Vector3.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(len / (1.4f * K)));
        for (int i = 0; i <= n; i++) { var p = Vector3.Lerp(a, b, (float)i / n); s.Rura(p, p + Vector3.up * h, .02f * K, 8); }
    }

    // ---------------- schody terenowe: n stopni od yGora w dół, bieg w kierunku dir ----------------
    /// <summary>Schody z betonu z okładziną: start = środek górnej krawędzi; dir = kierunek schodzenia (poziomo); szer. w m.</summary>
    public static void Schody(BraxNabrzeze.Zbior z, Transform kol, Mat m, Vector3 start, Vector3 dir, float szer, float yDol, bool porecze = true, float dolDo = -9f)
    {
        dir.y = 0; dir.Normalize(); var bok = Vector3.Cross(Vector3.up, dir).normalized;
        float rise = .15f * K, run = .30f * K;
        int n = Mathf.Max(1, Mathf.RoundToInt((start.y - yDol) / rise)); rise = (start.y - yDol) / n;
        float fundament = dolDo > -8f ? dolDo : yDol - .5f;
        for (int i = 0; i < n; i++)
        {
            float top = start.y - rise * (i + 1);
            var c = start + dir * (run * i + run * .5f);
            var size = new Vector3(szer, top - fundament, run);
            var rot = Quaternion.LookRotation(dir);
            z[m.beton].Box(new Vector3(c.x, (top + fundament) * .5f, c.z), size, rot);
            z[m.granit].Box(new Vector3(c.x, top - .02f, c.z) + dir * .01f, new Vector3(szer + .02f, .04f, run + .02f), rot);   // płyta stopnicy z noskiem
            if (kol) BraxNabrzeze.Kol(kol, "Schody stopień", new Vector3(c.x, (top + fundament) * .5f, c.z), size, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
        }
        float dl = run * n;
        // policzki (murki boczne) ze skośnym wierzchem równoległym do linii nosków (+0,15 m) i poręcze po obu stronach
        float yaw0 = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        foreach (float sgn in new[] { -1f, 1f })
        {
            var e = start + bok * (szer * .5f + .12f) * sgn;
            Klin(z[m.beton], e, dir, bok, .24f, dl, fundament, start.y + .15f, yDol + rise + .15f);
            var mid = e + dir * dl * .5f;
            if (kol) BraxNabrzeze.Kol(kol, "Schody policzek", new Vector3(mid.x, (start.y + fundament) * .5f + .6f, mid.z), new Vector3(.24f, start.y - fundament + 1.2f, dl), yaw0);
            if (porecze)
            {
                var na = e; na.y = start.y + .15f;
                var nb = e + dir * dl; nb.y = yDol + rise + .15f;
                Porecz(z, m, na, nb);
                var pa = na - dir * .3f * K; var pb = nb + dir * .3f * K; pb.y = yDol + .15f;
                z[m.stal].Rura(na + Vector3.up * .9f * K, pa + Vector3.up * .9f * K, .021f * K, 10);
                z[m.stal].Rura(nb + Vector3.up * .9f * K, pb + Vector3.up * .9f * K, .021f * K, 10);
            }
        }
    }

    /// <summary>Murek z płaskim spodem (y0) i skośnym wierzchem (yA na początku, yB na końcu), grubość th, długość dl wzdłuż dir.</summary>
    public static void Klin(BraxNabrzeze.Siatka s, Vector3 start, Vector3 dir, Vector3 bok, float th, float dl, float y0, float yA, float yB)
    {
        Vector3 P(float along, float side, float y) { var q = start + dir * along + bok * side; q.y = y; return q; }
        float h = th * .5f;
        // boki
        s.Quad(P(0, -h, y0), P(dl, -h, y0), P(dl, -h, yB), P(0, -h, yA));
        s.Quad(P(dl, h, y0), P(0, h, y0), P(0, h, yA), P(dl, h, yB));
        // czoła
        s.Quad(P(0, h, y0), P(0, -h, y0), P(0, -h, yA), P(0, h, yA));
        s.Quad(P(dl, -h, y0), P(dl, h, y0), P(dl, h, yB), P(dl, -h, yB));
        // wierzch i spód
        s.Quad(P(0, -h, yA), P(dl, -h, yB), P(dl, h, yB), P(0, h, yA));
        s.Quad(P(0, h, y0), P(dl, h, y0), P(dl, -h, y0), P(0, -h, y0));
    }

    // ---------------- rampa (spadek 1:12), poręcze obustronne na dwóch wysokościach, krawężniki ----------------
    public static void Rampa(BraxNabrzeze.Zbior z, Transform kol, Mat m, Vector3 gora, Vector3 dir, float szer, float yDol)
    {
        dir.y = 0; dir.Normalize(); var bok = Vector3.Cross(Vector3.up, dir).normalized;
        float dh = gora.y - yDol, dl = dh * 12f;
        var dol = gora + dir * dl; dol.y = yDol;
        var rot = Quaternion.LookRotation((dol - gora).normalized);
        var mid = (gora + dol) * .5f;
        float grub = .25f;
        z[m.granit].Box(mid + rot * new Vector3(0, -grub * .5f, 0), new Vector3(szer, grub, Vector3.Distance(gora, dol)), rot);
        // podpora rampy (ściana pod spodem do gruntu)
        z[m.beton].Box(new Vector3(mid.x, (mid.y - .9f) * .5f + (yDol - .5f) * .5f, mid.z), new Vector3(szer, Mathf.Max(.3f, mid.y - yDol + .4f), dl), Quaternion.LookRotation(dir));
        if (kol)
        {
            BraxNabrzeze.KolRot(kol, "Rampa", mid + rot * new Vector3(0, -.15f, 0), new Vector3(szer, .3f, Vector3.Distance(gora, dol) + .2f), rot);
            foreach (float sgn in new[] { -1f, 1f })
                BraxNabrzeze.KolRot(kol, "Rampa bariera", mid + bok * (szer * .5f + .1f) * sgn + Vector3.up * .9f, new Vector3(.2f, 2f, Vector3.Distance(gora, dol)), rot);
        }
        foreach (float sgn in new[] { -1f, 1f })
        {
            var a = gora + bok * (szer * .5f + .05f) * sgn; var b = dol + bok * (szer * .5f + .05f) * sgn;
            z[m.beton].Box((a + b) * .5f + rot * new Vector3(0, .06f, 0), new Vector3(.1f, .12f, Vector3.Distance(a, b)), rot);   // krawężnik
            Porecz(z, m, a, b);
            z[m.stal].Rura(a + Vector3.up * .70f * K, b + Vector3.up * .70f * K, .018f * K, 8);       // niższa poręcz (dzieci, wózki)
        }
    }

    // ---------------- ławka (siedzisko 0,45 m, dł. 1,80 m, oparcie) + BraxSeat ----------------
    public static void Lawka(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at, float yaw, string nazwa = "Ławka bulwaru")
    {
        var t = new GameObject(nazwa).transform; t.SetParent(root, false); t.position = at; t.rotation = Quaternion.Euler(0, yaw, 0);
        float L1 = 1.8f * K, hs = .45f * K, d = .46f * K;
        var rot = t.rotation;
        foreach (float x in new[] { -L1 * .42f, L1 * .42f })
        {
            z[m.stalCiemna].Box(L(t, x, hs * .5f, 0), new Vector3(.06f * K, hs, d * .9f), rot);                         // noga-płyta
            z[m.stalCiemna].Box(L(t, x, hs + .22f * K, -d * .5f + .03f), new Vector3(.05f * K, .44f * K, .05f * K), rot * Quaternion.Euler(-12f, 0, 0));
        }
        for (int i = 0; i < 4; i++)
            z[m.drewno].Box(L(t, 0, hs - .02f * K, -d * .38f + i * d * .25f), new Vector3(L1, .04f * K, .1f * K), rot);   // listwy siedziska
        for (int i = 0; i < 3; i++)
            z[m.drewno].Box(L(t, 0, hs + .14f * K + i * .12f * K, -d * .5f - i * .02f * K), new Vector3(L1, .09f * K, .03f * K), rot * Quaternion.Euler(-12f, 0, 0));
        var c = t.gameObject.AddComponent<BoxCollider>(); c.center = new Vector3(0, hs * .5f + .1f, -.03f); c.size = new Vector3(L1, hs + .2f, d);
        Siedzisko(t, new Vector3(0, hs, .05f), 0f, false, hs + at.y, "ŁAWKA");
    }

    /// <summary>Punkt siedzenia / leżenia (BraxSeat) — kierunek: lokalne +Z.</summary>
    public static void Siedzisko(Transform t, Vector3 local, float yawLocal, bool lie, float surfaceY, string label)
    {
        var g = new GameObject("Siedzisko " + label.ToLowerInvariant()); g.transform.SetParent(t, false);
        g.transform.localPosition = local; g.transform.localRotation = Quaternion.Euler(0, yawLocal, 0);
        var s = g.AddComponent<BraxSeat>(); s.lie = lie; s.surfaceY = surfaceY; s.label = label;
    }

    // ---------------- kosz na śmieci (0,95 m) ----------------
    public static void Kosz(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at)
    {
        float h = .95f * K, r = .23f * K;
        z[m.stalCiemna].Rura(at, at + Vector3.up * h, r, 14);
        z[m.stal].Rura(at + Vector3.up * (h - .04f * K), at + Vector3.up * (h + .05f * K), r * 1.04f, 14, r * .8f);
        z[m.stalCiemna].Rura(at + Vector3.up * (h + .05f * K), at + Vector3.up * (h + .06f * K), r * .8f, 14);
        var g = new GameObject("Kosz na śmieci"); g.transform.SetParent(root, false); g.transform.position = at;
        var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * h * .5f; c.height = h; c.radius = r;
    }

    // ---------------- latarnia (5,0 m) z wysięgnikiem i oprawą LED (nie świeci w dzień — bez światła czasu rzeczywistego) ----------------
    public static void Latarnia(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at, float yaw)
    {
        float h = 5.0f * K; var rot = Quaternion.Euler(0, yaw, 0); var fw = rot * Vector3.forward;
        z[m.stalCiemna].Rura(at, at + Vector3.up * h, .07f * K, 12, .045f * K);
        z[m.stalCiemna].Rura(at, at + Vector3.up * .6f * K, .1f * K, 12, .08f * K);
        var top = at + Vector3.up * h;
        z[m.stalCiemna].Rura(top, top + fw * .7f * K + Vector3.up * .08f * K, .03f * K, 8);
        var head = top + fw * .8f * K + Vector3.up * .05f * K;
        z[m.stalCiemna].Box(head, new Vector3(.24f, .08f, .56f) * K, rot);
        z[m.szklo].Box(head + Vector3.down * .045f * K, new Vector3(.2f, .015f, .48f) * K, rot);
        var g = new GameObject("Latarnia bulwaru"); g.transform.SetParent(root, false); g.transform.position = at;
        var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * 1.5f; c.height = 3f; c.radius = .12f * K;
    }

    // ---------------- parasol plażowy (2,30 m, czasza Ø 2,40 m, 8 żeber) ----------------
    public static void Parasol(BraxNabrzeze.Zbior z, Transform root, Mat m, Material plotno, Vector3 at, float pochyl)
    {
        float h = 2.3f * K, R = 1.2f * K;
        var tilt = Quaternion.Euler(pochyl, Mathf.Repeat(at.x * 37.1f + at.z * 13.7f, 360f), 0);
        var top = at + tilt * Vector3.up * h;
        z[m.bialy].Rura(at + Vector3.down * .35f * K, top + tilt * Vector3.up * .12f * K, .02f * K, 8);
        int seg = 16; var s = z[plotno];
        var apex = top + tilt * Vector3.up * .3f * K;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
            // czasza z lekkim ugięciem między żebrami (fale materiału)
            float sag0 = (i % 2 == 0) ? 0 : .05f * K, sag1 = ((i + 1) % 2 == 0) ? 0 : .05f * K;
            var p0 = top + tilt * new Vector3(Mathf.Cos(a0) * R, -.12f * K - sag0, Mathf.Sin(a0) * R);
            var p1 = top + tilt * new Vector3(Mathf.Cos(a1) * R, -.12f * K - sag1, Mathf.Sin(a1) * R);
            var m0 = top + tilt * new Vector3(Mathf.Cos(a0) * R * .55f, .12f * K - sag0 * .5f, Mathf.Sin(a0) * R * .55f);
            var m1 = top + tilt * new Vector3(Mathf.Cos(a1) * R * .55f, .12f * K - sag1 * .5f, Mathf.Sin(a1) * R * .55f);
            s.Tri(apex, m1, m0); s.Tri(apex, m0, m1);                 // dwustronnie (spód czaszy widać z leżaka)
            s.Quad(m0, m1, p1, p0); s.Quad(p0, p1, m1, m0);
            // falbana
            s.Quad(p0, p1, p1 + tilt * Vector3.down * .14f * K, p0 + tilt * Vector3.down * .14f * K);
            s.Quad(p0 + tilt * Vector3.down * .14f * K, p1 + tilt * Vector3.down * .14f * K, p1, p0);
            if (i % 2 == 0) z[m.bialy].Rura(top + tilt * Vector3.down * .35f * K, (p0 + m0) * .5f + tilt * Vector3.down * .05f, .006f * K, 4, -1, false);
        }
        var g = new GameObject("Parasol plażowy"); g.transform.SetParent(root, false); g.transform.position = at;
        var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * 1.2f; c.height = 2.4f; c.radius = .05f * K;
    }

    // ---------------- leżak plażowy (1,95 × 0,65 m, oparcie regulowane) + BraxSeat (leżenie) ----------------
    public static void Lezak(BraxNabrzeze.Zbior z, Transform root, Mat m, Material recznik, Vector3 at, float yaw)
    {
        var t = new GameObject("Leżak plażowy").transform; t.SetParent(root, false); t.position = at; t.rotation = Quaternion.Euler(0, yaw, 0);
        float Lg = 1.95f * K, W = .65f * K, hs = .32f * K; var rot = t.rotation;
        float lieL = Lg * .62f;
        // rama i nogi (stal), siedzisko z listew (drewno), oparcie pod kątem 40°
        foreach (float x in new[] { -W * .46f, W * .46f })
        {
            z[m.bialy].Box(L(t, x, hs, -Lg * .5f + lieL * .5f), new Vector3(.035f * K, .05f * K, lieL), rot);
            foreach (float zz in new[] { -Lg * .46f, -Lg * .5f + lieL - .05f })
                z[m.bialy].Box(L(t, x, hs * .5f, zz), new Vector3(.035f * K, hs, .035f * K), rot);
        }
        for (int i = 0; i < 9; i++)
            z[m.drewno].Box(L(t, 0, hs + .03f * K, -Lg * .5f + .06f * K + i * lieL / 9f), new Vector3(W * .9f, .02f * K, lieL / 9f * .78f), rot);
        var back = rot * Quaternion.Euler(-40f, 0, 0);
        float bl = Lg - lieL;
        var bc = L(t, 0, hs, -Lg * .5f + lieL) + back * new Vector3(0, 0, bl * .5f);
        for (int i = 0; i < 5; i++) z[m.drewno].Box(bc + back * new Vector3(0, .03f * K, -bl * .4f + i * bl * .2f), new Vector3(W * .9f, .02f * K, bl * .16f), back);
        foreach (float x in new[] { -W * .46f, W * .46f }) z[m.bialy].Box(bc + back * new Vector3(x, 0, 0), new Vector3(.03f * K, .04f * K, bl), back);
        if (recznik)
        {
            z[recznik].Box(L(t, 0, hs + .05f * K, -Lg * .5f + lieL * .55f), new Vector3(W * .82f, .012f * K, lieL * .9f), rot);
            z[recznik].Box(bc + back * new Vector3(0, .05f * K, 0), new Vector3(W * .82f, .012f * K, bl * .8f), back);
        }
        var c = t.gameObject.AddComponent<BoxCollider>(); c.center = new Vector3(0, hs * .6f, 0); c.size = new Vector3(W, hs * 1.2f + .1f, Lg * .95f);
        // leżenie: stopy w stronę −Z (BraxSeat.lie: stopy przy „forward”) → punkt obrócony o 180°
        Siedzisko(t, new Vector3(0, hs + .04f * K, -Lg * .12f), 180f, true, at.y + hs + .04f * K, "LEŻAK");
    }

    // ---------------- prysznic plażowy (2,20 m) z kratką z desek ----------------
    public static void Prysznic(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at, float yaw)
    {
        var rot = Quaternion.Euler(0, yaw, 0); var fw = rot * Vector3.forward; float h = 2.2f * K;
        z[m.stal].Rura(at, at + Vector3.up * h, .04f * K, 12);
        z[m.stal].Rura(at + Vector3.up * h, at + Vector3.up * h + fw * .35f * K, .025f * K, 10);
        z[m.stal].Rura(at + Vector3.up * (h - .02f * K) + fw * .35f * K, at + Vector3.up * (h - .07f * K) + fw * .35f * K, .09f * K, 14, .1f * K);   // deszczownica
        z[m.stal].Rura(at + Vector3.up * 1.05f * K, at + Vector3.up * 1.05f * K + fw * .06f * K, .03f * K, 10);    // przycisk
        z[m.stal].Rura(at + Vector3.up * .55f * K + fw * .03f, at + Vector3.up * .45f * K + fw * .12f * K, .012f * K, 6); // kranik do stóp
        var deck = at + fw * .35f * K;
        for (int i = 0; i < 7; i++)
            z[m.drewno].Box(deck + rot * new Vector3(-.5f * K + i * .166f * K, .03f * K, 0), new Vector3(.12f * K, .04f * K, 1.1f * K), rot);
        var g = new GameObject("Prysznic plażowy"); g.transform.SetParent(root, false); g.transform.position = at;
        var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * 1.4f; c.height = 2.8f; c.radius = .07f * K;
    }

    // ---------------- koło ratunkowe na stojaku (Ø 0,75 m) ----------------
    public static void KoloRatunkowe(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at, float yaw)
    {
        var rot = Quaternion.Euler(0, yaw, 0); float h = 1.5f * K;
        z[m.stalCiemna].Rura(at, at + Vector3.up * h, .03f * K, 8);
        var c = at + Vector3.up * (h - .45f * K) + rot * Vector3.forward * .07f * K;
        int seg = 20; float R = .30f * K, r = .075f * K;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
            var p0 = c + rot * new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R, 0); var p1 = c + rot * new Vector3(Mathf.Cos(a1) * R, Mathf.Sin(a1) * R, 0);
            z[(i / 5) % 2 == 0 ? m.czerwony : m.bialy].Rura(p0, p1, r, 8, -1, false);
        }
        var g = new GameObject("Koło ratunkowe na stojaku"); g.transform.SetParent(root, false); g.transform.position = at;
        var cc = g.AddComponent<CapsuleCollider>(); cc.center = Vector3.up * 1f; cc.height = 2f; cc.radius = .08f;
    }

    // ---------------- tablica informacyjna (słupki + tablica + napis) ----------------
    public static void Tablica(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at, float yaw, string tytul, string tresc, Color tlo, float szer = 1.6f)
    {
        var rot = Quaternion.Euler(0, yaw, 0); var fw = rot * Vector3.forward; var bok = rot * Vector3.right;
        float W = szer * K, H = 1.0f * K, y0 = 1.0f * K;
        foreach (float s in new[] { -1f, 1f }) z[m.stalCiemna].Rura(at + bok * W * .45f * s, at + bok * W * .45f * s + Vector3.up * (y0 + H), .03f * K, 8);
        var mid = at + Vector3.up * (y0 + H * .5f);
        z[BraxNabrzeze.Kolor("Tablica: " + ColorUtility.ToHtmlStringRGB(tlo), tlo, .6f)].Box(mid, new Vector3(W, H, .03f * K), rot);
        BraxNabrzeze.Napis(root, tytul, mid + fw * .03f * K + Vector3.up * H * .28f, yaw, .16f * K, new Color(.97f, .96f, .92f));
        if (!string.IsNullOrEmpty(tresc)) BraxNabrzeze.Napis(root, tresc, mid + fw * .03f * K - Vector3.up * H * .12f, yaw, .065f * K, new Color(.93f, .93f, .9f));
        BraxNabrzeze.Kol(root, "Tablica", mid, new Vector3(W, H * 2f, .15f), yaw);
    }

    // ---------------- poler cumowniczy (0,55 m, żeliwo) i knaga (0,30 m) ----------------
    public static void Poler(BraxNabrzeze.Zbior z, Transform root, Mat m, Vector3 at)
    {
        float h = .55f * K, r = .14f * K;
        z[m.stalCiemna].Box(at + Vector3.up * .02f * K, new Vector3(.42f, .04f, .42f) * K);
        z[m.stalCiemna].Rura(at, at + Vector3.up * h, r * 1.1f, 14, r);
        z[m.stalCiemna].Rura(at + Vector3.up * h, at + Vector3.up * (h + .07f * K), r * 1.45f, 14, r * 1.3f);
        var g = new GameObject("Poler cumowniczy"); g.transform.SetParent(root, false); g.transform.position = at;
        var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * h * .5f; c.height = h + .1f; c.radius = r * 1.3f;
    }

    public static void Knaga(BraxNabrzeze.Zbior z, Mat m, Vector3 at, float yaw)
    {
        var rot = Quaternion.Euler(0, yaw, 0); var bok = rot * Vector3.right;
        float h = .12f * K, L1 = .30f * K;
        z[m.stal].Box(at + Vector3.up * .01f, new Vector3(.12f * K, .02f, .18f * K), rot);
        foreach (float s in new[] { -.25f, .25f }) z[m.stal].Rura(at + bok * L1 * s, at + bok * L1 * s + Vector3.up * h, .018f * K, 8);
        z[m.stal].Rura(at - bok * L1 * .5f + Vector3.up * h, at + bok * L1 * .5f + Vector3.up * h, .02f * K, 8, .014f * K);
    }

    /// <summary>Boja (kula na wodzie, pół zanurzona).</summary>
    public static void Boja(BraxNabrzeze.Zbior z, Material kolor, Vector3 at, float r)
    {
        var s = z[kolor]; int seg = 10, rings = 5;
        for (int j = 0; j < rings; j++)
        {
            float b0 = Mathf.Lerp(-.25f, 1f, (float)j / rings) * Mathf.PI * .5f, b1 = Mathf.Lerp(-.25f, 1f, (float)(j + 1) / rings) * Mathf.PI * .5f;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                Vector3 P(float a, float b) => at + new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b)) * r;
                s.Quad(P(a0, b0), P(a0, b1), P(a1, b1), P(a1, b0));
            }
        }
    }
}
