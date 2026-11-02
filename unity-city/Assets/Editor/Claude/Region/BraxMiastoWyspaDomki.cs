using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// C2 (właściciel 2.11, 21:56–22:35): WYSPA DALEJ, WIĘKSZA, PREMIUM. Ten plik: plaża premium (południe), ośrodek domków
// letniskowych (północ i wschód: domki, chodniki z krawężnikami i fugami, latarnie, ławki, recepcja, sklep, plac zabaw — bez aut),
// tablica „WYSPA” na przystani, otwarte morze do wyspy (woda, dno, strażnik głębokości), TOR WODNY z mariny (boje, kontrola
// kolizji), mapa miasta ze znacznikiem WYSPA i trasą łodzi, raport BRAXHORN_WYSPA. Wymiary rzeczywiste × K. Nic nie leży na
// niczym: chodniki to płyty z cokołem (nie płaszczyzny na terenie), domki na pełnych cokołach (nie wiszą), okiennice 1 cm od ściany.
internal static partial class BraxMiastoWyspa
{
    static readonly List<(Vector3 p, Vector3 d)> wiezyczki = new List<(Vector3 p, Vector3 d)>();
    static int nDomki, nLatarnie, nLawki, nKosze, nPrzebieralnie, nKapiacy, nRowerki, nDeski, nKrawezniki, nLezaki;
    static float mChodnika, mDeptakuPlazy;
    static Vector3 recepcja, sklep, placZabaw;
    public static void StartC2()
    {
        wiezyczki.Clear(); nDomki = nLatarnie = nLawki = nKosze = nPrzebieralnie = nKapiacy = nRowerki = nDeski = nKrawezniki = nLezaki = 0;
        mChodnika = mDeptakuPlazy = 0; recepcja = sklep = placZabaw = Vector3.zero;
    }
    static Vector3 Sw(float lx, float lz) => new Vector3(CX + lx, Y(CX + lx, CZ + lz), CZ + lz);   // punkt świata na terenie (lokalne x, z)
    // czworokąt licem do „chce” (bez pomyłek w kolejności wierzchołków)
    static void Q(BraxNabrzeze.Siatka s, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 chce)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), chce) < 0) s.Quad(a, d, c, b); else s.Quad(a, b, c, d);
    }

    // =================================================================================================================
    // PLAŻA PREMIUM
    // =================================================================================================================
    static void FlagaRatownika(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m, Bounds bb, Vector3 d, Vector3 t)
    {
        var p = new Vector3(bb.center.x, bb.max.y, bb.center.z) - d * (bb.extents.z * .3f);
        z[m.bialy].Rura(p - Vector3.up * .4f, p + Vector3.up * 2.6f * K, .035f, 8);
        var f0 = p + Vector3.up * 2.5f * K; var w = t * 1.1f * K; var h = Vector3.down * .7f * K;
        foreach (var (mat, o) in new[] { (m.czerwony, 0f), (m.zolty, .5f) })
        {
            var a = f0 + h * o; var b = a + w; var c = b + h * .5f; var e = a + h * .5f;
            Q(z[mat], a, b, c, e, Vector3.Cross(w, h)); Q(z[mat], a, e, c, b, -Vector3.Cross(w, h));   // flaga dwustronna
        }
    }

    static void PlazaPremium(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m)
    {
        var str = new GameObject("Wyspa: plaża premium (deptak, przebieralnie, WC, wypożyczalnia, bar)").transform; str.SetParent(root, false);
        var deski = BraxNabrzeze.Mat("Wyspa: deski deptaku plażowego", 1.5f, new Color(.86f, .76f, .62f), "wood_planks_grey", "wooden_planks", "wood_planks");
        var legar = BraxNabrzeze.Kolor("Wyspa: legary deptaku (impregnat)", new Color(.32f, .25f, .18f), .9f);
        float hw = 1.5f * K;   // deptak plażowy 3 m × k
        // 1) DEPTAK Z DESEK wzdłuż plaży, ~38 m od wody (za rzędami leżaków), na legarach 25 cm nad piaskiem
        var pts = new List<Vector3>();
        for (float a = PlazaOd + 4f; a <= PlazaDo - 4f; a += .9f)
        {
            float rs = LiniaWody(a); var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
            var p = new Vector3(CX, 0, CZ) + d * (rs - 38f);
            if (Vector2.Distance(new Vector2(p.x - CX, p.z - CZ), Plac) < PlacR + 6f) continue;
            float yMax = W; for (float o = -hw; o <= hw; o += hw) { var q = p + Vector3.Cross(Vector3.up, d) * 0 + d * o; yMax = Mathf.Max(yMax, Y(q.x, q.z)); }
            p.y = yMax + .25f; pts.Add(p);
        }
        // wygładzenie wysokości (bez schodków)
        for (int it = 0; it < 3; it++) for (int i = 1; i < pts.Count - 1; i++) pts[i] = new Vector3(pts[i].x, Mathf.Max(pts[i].y, (pts[i - 1].y + pts[i + 1].y) * .5f), pts[i].z);
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            var a = pts[i]; var b = pts[i + 1]; var dir = b - a; float len = dir.magnitude; if (len < .1f || len > 6f) continue;
            var rot = Quaternion.LookRotation(dir / len, Vector3.up);
            for (float s = 0; s < len - .05f; s += .16f) z[deski].Box(a + dir / len * (s + .075f) + Vector3.down * .03f, new Vector3(hw * 2f, .05f, .14f), rot);
            foreach (float o in new[] { -hw + .25f, hw - .25f }) z[legar].Box((a + b) * .5f + rot * new Vector3(o, -.14f, 0), new Vector3(.12f, .16f, len + .02f), rot);
            foreach (float o in new[] { -hw + .25f, hw - .25f }) { var pp = a + rot * new Vector3(o, 0, 0); z[legar].Rura(new Vector3(pp.x, Y(pp.x, pp.z) - .4f, pp.z), pp + Vector3.down * .2f, .07f, 6); }
            BraxNabrzeze.KolRot(kol, "Deptak plażowy", (a + b) * .5f + Vector3.down * .1f, new Vector3(hw * 2f, .2f, len + .03f), rot);
            mDeptakuPlazy += len;
            if (i % 20 == 5) { var lp = a + rot * new Vector3(-hw - .5f, 0, 0); lp.y = Y(lp.x, lp.z); BraxNabrzezeMebel.Latarnia(z, str, m, lp, rot.eulerAngles.y); nLatarnie++; Zajmij(lp, .6f); }
            if (i % 26 == 12)
            {
                var lp = a + rot * new Vector3(hw + .9f, 0, 0); lp.y = Y(lp.x, lp.z);
                float yaw = Quaternion.LookRotation(rot * Vector3.right).eulerAngles.y;      // ławka przodem do morza
                BraxNabrzezeMebel.Lawka(z, str, m, lp, yaw, "Ławka deptaku plażowego"); nLawki++; Zajmij(lp, 1.4f);
                var kp = lp + rot * new Vector3(0, 0, 1.6f * K); kp.y = Y(kp.x, kp.z); BraxNabrzezeMebel.Kosz(z, str, m, kp); nKosze++;
            }
        }
        // 2) KŁADKI z deptaku do każdej wieżyczki ratownika (po piasku, 1.8 m × k)
        foreach (var (wp, wd) in wiezyczki)
        {
            float ang = Mathf.Atan2(wd.z, wd.x) * Mathf.Rad2Deg; float rs = LiniaWody(ang);
            var a = new Vector3(CX, 0, CZ) + wd * (rs - 37f); var b = new Vector3(CX, 0, CZ) + wd * (rs - 16f);
            var dir = (b - a); float len = dir.magnitude; var rot = Quaternion.LookRotation(dir / len, Vector3.up);
            for (float s = 0; s < len; s += .16f)
            {
                var p = a + dir / len * (s + .075f); p.y = Y(p.x, p.z) + .1f;
                z[deski].Box(p, new Vector3(.9f * K * 2f, .05f, .14f), rot);
                if (((int)(s / .16f)) % 6 == 0) foreach (float o in new[] { -.7f * K, .7f * K }) z[legar].Box(p + rot * new Vector3(o, -.16f, 0), new Vector3(.1f, .24f, 1.0f), rot);   // legary w piasku (deski nie wiszą)
            }
        }
        // 3) ZAPLECZE PLAŻY za deptakiem: przebieralnie, WC, wypożyczalnia, bar na plaży
        var drewno = BraxMiastoA.Tex("Wyspa: deskowanie budek plażowych", 1.2f, new Color(.95f, .93f, .88f), new Color(.85f, .82f, .76f), .8f, "wood_plank_wall", "wood_planks_grey");
        var tynk = BraxMiastoA.Tex("Wyspa: tynk biały (WC)", 2f, new Color(.95f, .95f, .93f), new Color(.92f, .92f, .9f), .85f, "white_plaster_02", "concrete_wall_008");
        var dach = BraxMiastoA.Mat("Wyspa: dach budek (grafit)", new Color(.2f, .21f, .22f), .7f);
        var drzwi = BraxMiastoA.Mat("Wyspa: drzwi budek (błękit morski)", new Color(.16f, .42f, .55f), .55f);
        Vector3 Zaplecze(float ang, float odWody) { float rs = LiniaWody(ang); var d = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0, Mathf.Sin(ang * Mathf.Deg2Rad)); var p = new Vector3(CX, 0, CZ) + d * (rs - odWody); p.y = Y(p.x, p.z); return p; }
        Quaternion KuMorzu(float ang) => Quaternion.LookRotation(new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0, Mathf.Sin(ang * Mathf.Deg2Rad)), Vector3.up);
        // przebieralnie (3 + 3)
        foreach (float ang in new[] { 236f, 300f })
        {
            var c = Zaplecze(ang, 45f); var rot = KuMorzu(ang); var rg = rot * Vector3.right; var fw = rot * Vector3.forward;
            for (int k = -1; k <= 1; k++)
            {
                var p = c + rg * k * 1.5f * K; float y0 = Mathf.Max(Y(p.x - .7f, p.z - .7f), Y(p.x + .7f, p.z + .7f), Y(p.x + .7f, p.z - .7f), Y(p.x - .7f, p.z + .7f)) + .12f;
                float s = 1.25f * K * .5f, h = 2.2f * K;
                z[legar].Box(new Vector3(p.x, y0 - .3f, p.z), new Vector3(s * 2f + .1f, .6f, s * 2f + .1f), rot);                       // podest (do piasku)
                foreach (var (o, sz) in new[] { (new Vector3(-s, h * .5f, 0), new Vector3(.06f, h, s * 2f)), (new Vector3(s, h * .5f, 0), new Vector3(.06f, h, s * 2f)), (new Vector3(0, h * .5f, -s), new Vector3(s * 2f, h, .06f)) })
                    z[drewno].Box(new Vector3(p.x, y0, p.z) + rot * o, sz, rot);
                z[drzwi].Box(new Vector3(p.x, y0, p.z) + rot * new Vector3(0, .25f + (h - .5f) * .5f, s - .03f), new Vector3(s * 2f - .1f, h - .5f, .05f), rot);   // drzwiczki (prześwit 25 cm u dołu, jak w budkach)
                z[dach].Box(new Vector3(p.x, y0, p.z) + rot * new Vector3(0, h + .05f, 0), new Vector3(s * 2f + .3f, .1f, s * 2f + .3f), rot);
                BraxNabrzeze.Kol(kol, "Przebieralnia", new Vector3(p.x, y0 + h * .5f, p.z), new Vector3(s * 2f, h, s * 2f), rot.eulerAngles.y);
                Zajmij(p, 1.4f); nPrzebieralnie++;
            }
            BraxUliceKit.Napis3D("PRZEBIERALNIE", c + Vector3.up * (2.2f * K + .45f) + fw * (1.25f * K * .5f + .2f), fw, .18f, .02f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
            // prysznic obok przebieralni
            var pr = c + rg * 3.6f * K; pr.y = Y(pr.x, pr.z); BraxNabrzezeMebel.Prysznic(z, str, m, pr, rot.eulerAngles.y); Zajmij(pr, 1f);
        }
        // WC (budynek murowany z dwojgiem drzwi)
        {
            float ang = 270f; var c = Zaplecze(ang, 48f); var rot = KuMorzu(ang); var fw = rot * Vector3.forward; var rg = rot * Vector3.right;
            float w = 5f * K, d = 3.5f * K, h = 2.8f * K;
            float y0 = W; for (float a = -1; a <= 1; a += 1) for (float b = -1; b <= 1; b += 1) { var q = c + rg * a * w * .5f + fw * b * d * .5f; y0 = Mathf.Max(y0, Y(q.x, q.z)); }
            y0 += .15f; float yMin = Mathf.Min(Y(c.x - w, c.z - d), Y(c.x + w, c.z + d), Y(c.x, c.z)) - .3f;
            z[BraxNabrzeze.Kolor("Wyspa: cokół WC (beton)", new Color(.6f, .6f, .58f), .9f)].Box(new Vector3(c.x, (yMin + y0) * .5f, c.z), new Vector3(w + .1f, y0 - yMin, d + .1f), rot);
            z[tynk].Box(new Vector3(c.x, y0 + h * .5f, c.z), new Vector3(w, h, d), rot);
            z[dach].Box(new Vector3(c.x, y0 + h + .08f, c.z), new Vector3(w + .6f, .16f, d + .6f), rot);
            foreach (float o in new[] { -1f, 1f })
            {
                var dp = new Vector3(c.x, y0, c.z) + rg * o * w * .25f + fw * (d * .5f + .025f);
                z[drzwi].Box(dp + Vector3.up * 1.05f * K, new Vector3(.9f * K, 2.1f * K, .05f), rot);
                BraxUliceKit.Napis3D(o < 0 ? "WC DAMSKI" : "WC MĘSKI", dp + Vector3.up * (2.1f * K + .25f) + fw * .02f, fw, .16f, .015f, BraxMiastoA.Mat("Wyspa: litery granat", new Color(.1f, .16f, .3f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
            }
            BraxNabrzeze.Kol(kol, "WC plaży", new Vector3(c.x, y0 + h * .5f, c.z), new Vector3(w, h, d), rot.eulerAngles.y);
            Zajmij(c, Mathf.Max(w, d) * .7f);
            Miejsca["wyspa_wc"] = c + fw * (d * .5f + 1.2f);
        }
        // WYPOŻYCZALNIA: kiosk z ladą, stojak z deskami SUP, rowerki wodne przy wodzie
        {
            float ang = 252f; var c = Zaplecze(ang, 46f); var rot = KuMorzu(ang); var fw = rot * Vector3.forward; var rg = rot * Vector3.right;
            float w = 4f * K, d = 3f * K, h = 2.7f * K;
            float y0 = Mathf.Max(Y(c.x - w * .5f, c.z - d * .5f), Y(c.x + w * .5f, c.z + d * .5f), Y(c.x + w * .5f, c.z - d * .5f), Y(c.x - w * .5f, c.z + d * .5f)) + .15f;
            z[legar].Box(new Vector3(c.x, y0 - .35f, c.z), new Vector3(w + .2f, .7f, d + .2f), rot);
            z[drewno].Box(new Vector3(c.x, y0 + .55f * K, c.z), new Vector3(w, 1.1f * K, d), rot);                                // dół z ladą
            z[BraxMiastoA.Mat("Wyspa: blat lady (teak)", new Color(.45f, .3f, .18f), .45f)].Box(new Vector3(c.x, y0 + 1.1f * K + .03f, c.z) + fw * .1f, new Vector3(w + .1f, .06f, d * .5f), rot);
            foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                z[legar].Rura(new Vector3(c.x, y0, c.z) + rg * o.x * (w * .5f - .1f) + fw * o.y * (d * .5f - .1f), new Vector3(c.x, y0 + h, c.z) + rg * o.x * (w * .5f - .1f) + fw * o.y * (d * .5f - .1f), .06f, 8);
            z[drewno].Box(new Vector3(c.x, y0 + h * .5f + .55f * K, c.z) - fw * (d * .5f - .04f), new Vector3(w, h - 1.1f * K, .08f), rot);   // ściana tylna z półkami
            z[dach].Box(new Vector3(c.x, y0 + h + .06f, c.z), new Vector3(w + .8f, .12f, d + .8f), rot);
            BraxUliceKit.Napis3D("WYPOŻYCZALNIA", new Vector3(c.x, y0 + h - .25f, c.z) + fw * (d * .5f + .41f), fw, .2f, .02f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
            BraxNabrzeze.Kol(kol, "Wypożyczalnia", new Vector3(c.x, y0 + .55f * K, c.z), new Vector3(w, 1.1f * K, d), rot.eulerAngles.y);
            Czlowiek("Wypożyczalnia", "obsługa wypożyczalni", "stoi", new Vector3(c.x, y0, c.z) - fw * .4f, fw, KlLada);
            Miejsca["wyspa_wypozyczalnia"] = new Vector3(c.x, y0, c.z) + fw * (d * .5f + 1.2f);
            BraxMiastoA.Akcja(root, "wyspa_wypozyczalnia", "WYPOŻYCZ", "Wypożyczalnia: deski SUP, rowerki wodne, kajaki — na godzinę albo na cały dzień.", Miejsca["wyspa_wypozyczalnia"], 2f);
            Zajmij(c, 3.2f);
            // stojak z deskami SUP (deski oparte o belkę, różne kolory)
            var sp = c + rg * (w * .5f + 2.2f); sp.y = Y(sp.x, sp.z);
            var kolory = new[] { new Color(.95f, .45f, .1f), new Color(.15f, .55f, .8f), new Color(.95f, .95f, .92f), new Color(.2f, .7f, .45f), new Color(.9f, .2f, .3f) };
            z[legar].Box(sp + Vector3.up * 1.4f, new Vector3(.12f, .12f, 3.2f), rot * Quaternion.Euler(0, 90, 0));
            foreach (float o in new[] { -1.5f, 1.5f }) z[legar].Rura(sp + rg * o + Vector3.down * .4f, sp + rg * o + Vector3.up * 1.45f, .06f, 6);
            for (int k = 0; k < 5; k++)
            {
                var bp = sp + rg * (-1.2f + k * .6f) + fw * .45f + Vector3.up * 1.42f;
                z[BraxNabrzeze.Kolor("Wyspa: deska SUP " + k, kolory[k], .35f)].Box(bp + Vector3.down * .2f, new Vector3(.7f, 2.9f, .1f), rot * Quaternion.Euler(-15f, 0, 0)); nDeski++;
            }
            BraxNabrzeze.Kol(kol, "Stojak SUP", sp + Vector3.up * .9f, new Vector3(3.4f, 1.8f, 1.2f), rot.eulerAngles.y + 90f);
            Zajmij(sp, 2f);
            // rowerki wodne przy brzegu (2 pływaki, siedzisko, zjeżdżalnia) — na mokrym piasku, przodem do wody
            for (int k = 0; k < 3; k++)
            {
                float a = ang + (k - 1) * 2.2f; float rs = LiniaWody(a); var dd = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                var p = new Vector3(CX, 0, CZ) + dd * (rs - 3.5f); p.y = Mathf.Max(W, Y(p.x, p.z)) + .25f;
                var r2 = Quaternion.LookRotation(dd, Vector3.up); var rgr = r2 * Vector3.right;
                var kad = BraxNabrzeze.Kolor("Wyspa: rowerek wodny " + k, k == 1 ? new Color(.95f, .85f, .2f) : new Color(.95f, .95f, .95f), .4f);
                foreach (float o in new[] { -.75f, .75f }) z[kad].Rura(p + rgr * o * K - dd * 1.4f * K, p + rgr * o * K + dd * 1.4f * K, .3f * K, 12, .18f * K);
                z[kad].Box(p + Vector3.up * .3f * K, new Vector3(1.6f * K, .1f, 1.6f * K), r2);
                z[BraxNabrzeze.Kolor("Wyspa: siedzisko rowerka (niebieskie)", new Color(.15f, .35f, .7f), .5f)].Box(p + Vector3.up * .55f * K - dd * .3f, new Vector3(1.2f * K, .45f * K, .3f), r2);
                z[m.stal].Rura(p + Vector3.up * .5f * K + dd * .4f, p + Vector3.up * .8f * K + dd * .4f, .03f, 6);
                BraxNabrzeze.Kol(kol, "Rowerek wodny", p + Vector3.up * .3f, new Vector3(2.2f * K, .8f, 3f * K), r2.eulerAngles.y);
                Zajmij(p, 2.2f); nRowerki++;
            }
        }
        // BAR NA PLAŻY (strzecha, lada, hokery, barman) — przy deptaku, środek plaży
        try { var bc = Zaplecze(285f, 50f); float yb = bc.y; for (float a = -4f; a <= 4f; a += 2f) for (float b = -3f; b <= 3f; b += 1.5f) yb = Mathf.Max(yb, Y(bc.x + a, bc.z + b)); bc.y = yb + .05f; BarPlazowy(bc, "wyspa_bar_plaza", "BAR PLAŻOWY"); Zajmij(bc, 6f); } catch (System.Exception e) { BraxMiastoA.Log("WYSPA DO POPRAWKI bar na plaży: " + e.Message); }
        // 4) LUDZIE W WODZIE przy brzegu (po pas, przed wieżyczkami — w strefie boi) i spacerujący brzegiem
        var rnd = new System.Random(5150);
        foreach (var (wp, wd) in wiezyczki)
        {
            float ang = Mathf.Atan2(wd.z, wd.x) * Mathf.Rad2Deg;
            for (int k = 0; k < 4; k++)
            {
                float a = ang + ((float)rnd.NextDouble() - .5f) * 8f; float rs = LiniaWody(a); var dd = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                for (float o = 3f; o < 14f; o += .5f)
                {
                    var p = new Vector3(CX, 0, CZ) + dd * (rs + o); float gl = W - Y(p.x, p.z);
                    if (gl >= .55f && gl <= .95f) { p.y = Y(p.x, p.z); Czlowiek("Woda przy plaży", "kąpiący się (strój kąpielowy)", "stoi", p, -dd * ((k % 2) * 2 - 1), KlStoi); nKapiacy++; break; }
                }
            }
            // spacerujący brzegiem (mokry piasek, 6 m od wody)
            var t0 = new List<Vector3>();
            for (float a = ang - 10f; a <= ang + 10f; a += 4f) t0.Add(NaBrzegu(a, 6f));
            Czlowiek("Brzeg plaży", "spacerowicz (strój plażowy)", "spacer", t0[0], t0[1] - t0[0], KlSpacer, t0.ToArray());
        }
        // pole plaży (piasek od wody do deptaku) — do raportu
        plazaM2 = 0;
        for (float x = -240f; x <= 240f; x += 2f) for (float zz = -240f; zz <= 0f; zz += 2f)
            {
                float ang = Mathf.Repeat(Mathf.Atan2(zz, x) * Mathf.Rad2Deg, 360f); if (ang < PlazaOd || ang > PlazaDo) continue;
                float h = H(x, zz); if (h < 0f || h > 3.2f) continue;
                if (Vector2.Distance(new Vector2(x, zz), Plac) < PlacR + 12f) continue;
                if (Mathf.Sqrt(x * x + zz * zz) < LiniaWody(ang) - 44f) continue;
                plazaM2 += 4f;
            }
        BraxMiastoA.Log("WYSPA plaża premium: deptak z desek " + mDeptakuPlazy.ToString("F0") + " m, kładek do ratowników " + wiezyczki.Count + ", przebieralni " + nPrzebieralnie + ", WC 1, wypożyczalnia 1 (deski SUP " + nDeski + ", rowerki wodne " + nRowerki + "), bar na plaży 1, latarni " + nLatarnie + ", ławek " + nLawki + ", koszy " + nKosze + ", kąpiących się " + nKapiacy + ", plaża " + plazaM2.ToString("F0") + " m²");
    }
    static float plazaM2;

    // =================================================================================================================
    // OŚRODEK DOMKÓW LETNISKOWYCH (północ i wschód)
    // =================================================================================================================
    static Material matChodnik, matKraw, matSciezka;
    static readonly List<(Vector2 a, Vector2 b, float hw)> chodniki = new List<(Vector2 a, Vector2 b, float hw)>();

    static bool NaLadzie(float lx, float lz, float min = 1.0f) => H(lx, lz) >= min;

    /// <summary>Chodnik osiowy a→b (lokalnie): płyta 9 cm nad terenem z cokołem w dół (nie płaszczyzna na terenie), kostka z fugami
    /// (tekstura), krawężniki po 1 m z fugą 6 mm. Odcinki poza lądem i na placu areny pominięte.</summary>
    // chodniki planowane najpierw (wszystkie), budowane potem: na skrzyżowaniach i przy dojściach płyta tylko jednego chodnika
    // (wcześniejszego na liście), krawężniki przerwane tam, gdzie wchodzi inny chodnik — nic nie leży na niczym.
    static readonly List<(Vector2 a, Vector2 b, float szer, bool kraw, string nazwa)> plan = new List<(Vector2 a, Vector2 b, float szer, bool kraw, string nazwa)>();
    static void Planuj(Vector2 a, Vector2 b, float szer, bool kraw, string nazwa) { if (Vector2.Distance(a, b) >= .5f) { plan.Add((a, b, szer, kraw, nazwa)); chodniki.Add((a, b, szer * .5f)); } }
    static bool WInnym(Vector2 p, int ja, float zapas, bool tylkoWczesniejsze)
    {
        for (int k = 0; k < chodniki.Count; k++)
        {
            if (k == ja || (tylkoWczesniejsze && k > ja)) continue;
            var (a, b, hw) = chodniki[k]; var d = b - a; float L = d.magnitude; if (L < .01f) continue; d /= L;
            var q = p - a; float al = Vector2.Dot(q, d), ac = Mathf.Abs(q.x * d.y - q.y * d.x);
            if (al >= -zapas && al <= L + zapas && ac <= hw + zapas) return true;
        }
        return false;
    }
    static void Chodnik(BraxNabrzeze.Zbior z, int ja)
    {
        var (a, b, szer, kraw, nazwa) = plan[ja];
        var dir = (b - a); float L = dir.magnitude; if (L < .5f) return; dir /= L;
        var bok = new Vector2(dir.y, -dir.x); float hw = szer * .5f;
        var top = z[matChodnik]; var cok = z[matKraw];
        float TopY(Vector2 p) { float y = -99f; for (float o = -hw - .2f; o <= hw + .2f; o += hw * .5f) { var q = p + bok * o; y = Mathf.Max(y, H(q.x, q.y)); } return W + y + .09f; }
        float MinY(Vector2 p) { float y = 99f; for (float o = -hw - .2f; o <= hw + .2f; o += hw * .5f) { var q = p + bok * o; y = Mathf.Min(y, H(q.x, q.y)); } return W + y - .35f; }
        Vector3 S3(Vector2 p, float y) => new Vector3(CX + p.x, y, CZ + p.y);
        var bok3 = new Vector3(bok.x, 0, bok.y); var dir3 = new Vector3(dir.x, 0, dir.y);
        bool wRun = false; Vector2 runStart = a; float yRunStart = 0;
        int n = Mathf.CeilToInt(L);
        for (int i = 0; i < n; i++)
        {
            var p0 = a + dir * i; var p1 = a + dir * Mathf.Min(L, i + 1);
            var pm = (p0 + p1) * .5f;
            bool ok = NaLadzie(pm.x, pm.y) && NaLadzie((pm + bok * hw).x, (pm + bok * hw).y) && NaLadzie((pm - bok * hw).x, (pm - bok * hw).y)
                      && Vector2.Distance(pm, Plac) > PlacR - .5f && !WInnym(pm, ja, 0f, true);
            if (!ok) { if (wRun) { Koniec(runStart, p0, yRunStart); wRun = false; } continue; }
            float y0 = TopY(p0), y1 = TopY(p1), m0 = MinY(p0), m1 = MinY(p1);
            if (!wRun) { wRun = true; runStart = p0; yRunStart = y0; Q(cok, S3(p0 - bok * hw, y0), S3(p0 + bok * hw, y0), S3(p0 + bok * hw, m0), S3(p0 - bok * hw, m0), -dir3); }
            Q(top, S3(p0 - bok * hw, y0), S3(p0 + bok * hw, y0), S3(p1 + bok * hw, y1), S3(p1 - bok * hw, y1), Vector3.up);
            Q(cok, S3(p0 + bok * hw, y0), S3(p1 + bok * hw, y1), S3(p1 + bok * hw, m1), S3(p0 + bok * hw, m0), bok3);
            Q(cok, S3(p0 - bok * hw, y0), S3(p1 - bok * hw, y1), S3(p1 - bok * hw, m1), S3(p0 - bok * hw, m0), -bok3);
            mChodnika += Vector2.Distance(p0, p1);
            if (kraw)
            {   // krawężniki 15 × 25 cm × k, odcinki 1 m z fugą 6 mm, wierzch 10 cm nad chodnikiem, pochylone wzdłuż spadku
                float kw = .15f * K, kh = .3f;
                foreach (float s in new[] { -1f, 1f })
                {
                    var c0 = p0 + bok * s * (hw + kw * .5f); var c1 = p1 + bok * s * (hw + kw * .5f);
                    if (WInnym((c0 + c1) * .5f, ja, kw, false)) continue;                 // wejście innego chodnika — krawężnik przerwany
                    var A = S3(c0, y0 + .1f - kh * .5f); var B = S3(c1, y1 + .1f - kh * .5f); var d3 = B - A; float l3 = d3.magnitude; if (l3 < .05f) continue;
                    var rot = Quaternion.LookRotation(d3 / l3, Vector3.up);
                    z[matKraw].Box((A + B) * .5f, new Vector3(kw, kh, l3 - .006f), rot); nKrawezniki++;
                }
            }
            if (i % 4 == 0)
            {
                var A = S3(p0, y0); var B = S3(a + dir * Mathf.Min(L, i + 4), TopY(a + dir * Mathf.Min(L, i + 4))); var d3 = B - A; if (d3.magnitude > .5f)
                    BraxNabrzeze.KolRot(kol, "Chodnik " + nazwa, (A + B) * .5f + Vector3.down * .15f, new Vector3(szer + (kraw ? .3f * K : 0f), .3f, d3.magnitude + .05f), Quaternion.LookRotation(d3.normalized, Vector3.up));
            }
            if (i % 4 == 2) Zajmij(S3(pm, 0), hw + .8f);
        }
        if (wRun) Koniec(runStart, a + dir * L, yRunStart);
        void Koniec(Vector2 s0, Vector2 e, float ys)
        {
            float ye = TopY(e), me = MinY(e);
            Q(cok, S3(e - bok * hw, ye), S3(e + bok * hw, ye), S3(e + bok * hw, me), S3(e - bok * hw, me), dir3);
        }
    }

    struct Paleta { public Color sciana, obrobki, dach, okiennice; }
    static readonly Paleta[] Palety =
    {
        new Paleta { sciana = new Color(.93f, .91f, .86f), obrobki = new Color(.18f, .26f, .32f), dach = new Color(.22f, .23f, .25f), okiennice = new Color(.18f, .33f, .45f) },   // biały, granat
        new Paleta { sciana = new Color(.55f, .17f, .13f), obrobki = new Color(.95f, .95f, .93f), dach = new Color(.16f, .16f, .17f), okiennice = new Color(.95f, .95f, .93f) },   // czerwień szwedzka
        new Paleta { sciana = new Color(.42f, .52f, .6f), obrobki = new Color(.95f, .95f, .93f), dach = new Color(.3f, .3f, .32f), okiennice = new Color(.95f, .95f, .93f) },     // szaroniebieski
        new Paleta { sciana = new Color(.55f, .62f, .47f), obrobki = new Color(.95f, .94f, .9f), dach = new Color(.36f, .2f, .15f), okiennice = new Color(.35f, .42f, .3f) },     // szałwia
        new Paleta { sciana = new Color(.72f, .56f, .38f), obrobki = new Color(.3f, .22f, .15f), dach = new Color(.25f, .26f, .24f), okiennice = new Color(.3f, .22f, .15f) },    // naturalne drewno
        new Paleta { sciana = new Color(.85f, .72f, .38f), obrobki = new Color(.95f, .95f, .93f), dach = new Color(.2f, .3f, .27f), okiennice = new Color(.2f, .38f, .32f) },     // ochra
    };

    /// <summary>Domek letniskowy: c = środek bryły (lokalnie x, z), fw = kierunek frontu (do chodnika), w × d (m × k).
    /// Cokół kamienny do terenu, ściany z deskowania z otworami (ościeża), okna z ramą, szprosem, parapetem i okiennicami, drzwi
    /// z klamką i tabliczką, taras z deską, balustradą i schodami, dach dwuspadowy z okapem, deską czołową, rynnami i rurą
    /// spustową, kalenica, lampa przy drzwiach, ścieżka do chodnika. Zwraca stopę schodów (lokalnie) albo null, gdy teren zły.</summary>
    static Vector2? Domek(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m, Vector2 c, Vector2 fw2, float w, float d, int paleta, string tabliczka, string szyld = null)
    {
        var fw = new Vector3(fw2.x, 0, fw2.y).normalized; var rg = Vector3.Cross(Vector3.up, fw);
        float tD = 2.2f * K, h = 2.6f * K;
        // teren pod bryłą i tarasem
        float hMax = -99f, hMin = 99f;
        for (float a = -w * .5f - .3f; a <= w * .5f + .31f; a += w * .25f)
            for (float b = -d * .5f - .3f; b <= d * .5f + tD + .31f; b += (d + tD) * .25f)
            { var q = c + new Vector2(rg.x, rg.z) * a + fw2.normalized * b; float hh = H(q.x, q.y); hMax = Mathf.Max(hMax, hh); hMin = Mathf.Min(hMin, hh); }
        if (hMin < 1.6f || hMax - hMin > 2.2f) return null;
        float y0 = W + hMax + .6f, yMin = W + hMin - .3f;
        var C = new Vector3(CX + c.x, y0, CZ + c.y);
        var P = new System.Func<float, float, float, Vector3>((lx, ly, lz) => C + rg * lx + Vector3.up * ly + fw * lz);
        var rot = Quaternion.LookRotation(fw, Vector3.up);
        var pal = Palety[paleta % Palety.Length];
        var sciana = BraxMiastoA.Tex("Wyspa: domek deskowanie " + paleta, 1.1f, pal.sciana, pal.sciana, .82f, "wood_plank_wall", "wood_planks_grey", "wooden_planks");
        var obrob = BraxMiastoA.Mat("Wyspa: domek obróbki " + paleta, pal.obrobki, .6f);
        var dachM = BraxMiastoA.Tex("Wyspa: domek dach " + paleta, 1.4f, pal.dach, pal.dach, .75f, "roof_tiles", "clay_roof_tiles_02", "roof_slates_02");
        var okien = BraxMiastoA.Mat("Wyspa: domek okiennice " + paleta, pal.okiennice, .6f);
        var szyba = BraxMiastoA.Mat("Wyspa: szyba domku (przydymiona, odbicie)", new Color(.12f, .16f, .19f), .08f, .35f);
        var kamien = BraxMiastoA.Tex("Wyspa: cokół domku (kamień)", 1.2f, new Color(.78f, .76f, .72f), new Color(.55f, .53f, .5f), .9f, "white_sandstone_blocks_02", "concrete_wall_008");
        var deskaT = BraxNabrzeze.Mat("Wyspa: deska tarasowa", 1.4f, new Color(.82f, .7f, .55f), "wood_planks_grey", "wooden_planks", "wood_planks");
        // 1) cokół (pełny, do terenu) pod bryłą i pod tarasem
        z[kamien].Box((P(0, 0, 0) + new Vector3(0, (yMin - y0) * .5f, 0)), new Vector3(w + .1f, y0 - yMin, d + .1f), rot);
        z[kamien].Box(P(0, 0, d * .5f + tD * .5f) + new Vector3(0, (yMin - y0 - .06f) * .5f, 0), new Vector3(w + .1f, y0 - .06f - yMin, tD), rot);
        // 2) ściany z otworami (ościeża 14 cm), narożniki
        var s = BraxUliceKit.S(sciana); var so = BraxUliceKit.S(obrob);
        float ow = 1.0f * K, oh = 1.15f * K, oy = .85f * K, dw = .95f * K, dh = 2.05f * K;
        var front = new List<Rect> { new Rect(w * .5f - dw * .5f, 0f, dw, dh), new Rect(w * .18f - ow * .5f, oy, ow, oh), new Rect(w * .82f - ow * .5f, oy, ow, oh) };
        var tyl = new List<Rect> { new Rect(w * .3f - ow * .5f, oy, ow, oh), new Rect(w * .7f - ow * .5f, oy, ow, oh) };
        var bok = new List<Rect> { new Rect(d * .5f - ow * .5f, oy, ow, oh) };
        var sciany = new (Vector3 A, Vector3 R, Vector3 N, float W, List<Rect> o)[]
        {
            (P(-w * .5f, 0, d * .5f), rg, fw, w, front), (P(w * .5f, 0, -d * .5f), -rg, -fw, w, tyl),
            (P(-w * .5f, 0, -d * .5f), fw, -rg, d, bok), (P(w * .5f, 0, d * .5f), -fw, rg, d, bok),
        };
        foreach (var sc in sciany)
        {
            BraxMiastoA.Sciana(s, sc.A, sc.R, sc.N, sc.W, 0f, h, sc.o, .14f, so);
            foreach (var o in sc.o)
            {
                bool drzwi = o.yMin < .01f;
                Vector3 L(float a, float y, float dd) => sc.A + sc.R * a + Vector3.up * y + sc.N * dd;
                var ro = Quaternion.LookRotation(sc.N, Vector3.up);
                if (drzwi)
                {   // skrzydło drzwi (zamknięte) w głębi ościeża, klamka, tabliczka, lampa
                    z[okien].Box(L(o.center.x, o.yMin + dh * .5f, -.1f), new Vector3(dw - .02f, dh - .01f, .05f), ro);
                    z[m.stal].Rura(L(o.xMax - .12f, 1.02f * K, -.06f), L(o.xMax - .12f, 1.02f * K, .02f), .015f, 6);
                    z[m.stal].Box(L(o.xMax - .18f, 1.02f * K, .025f), new Vector3(.14f, .02f, .02f), ro);
                    var lp = L(o.xMax + .35f, 2.15f * K, .06f);
                    z[m.stalCiemna].Box(lp, new Vector3(.14f, .22f, .12f), ro); z[BraxMiastoA.Mat("Wyspa: klosz lampy (mleczny)", new Color(.95f, .93f, .86f), .3f)].Box(lp + Vector3.down * .04f + sc.N * .02f, new Vector3(.1f, .12f, .08f), ro);
                    if (!string.IsNullOrEmpty(tabliczka))
                    {
                        z[obrob].Box(L(o.center.x, dh + .25f, .015f), new Vector3(.5f, .16f, .03f), ro);
                        BraxUliceKit.Napis3D(tabliczka, L(o.center.x, dh + .2f, .032f), sc.N, .085f, .01f, BraxMiastoA.Mat("Wyspa: litery tabliczki (grafit)", new Color(.1f, .1f, .11f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
                    }
                    continue;
                }
                // okno: rama 6 cm, szpros, szyba w głębi ościeża, parapet zewnętrzny, okiennice 1 cm od ściany
                float g = -.1f;
                z[obrob].Box(L(o.center.x, o.yMin + .03f, g), new Vector3(o.width, .06f, .06f), ro);
                z[obrob].Box(L(o.center.x, o.yMax - .03f, g), new Vector3(o.width, .06f, .06f), ro);
                z[obrob].Box(L(o.xMin + .03f, o.center.y, g), new Vector3(.06f, o.height, .06f), ro);
                z[obrob].Box(L(o.xMax - .03f, o.center.y, g), new Vector3(.06f, o.height, .06f), ro);
                z[obrob].Box(L(o.center.x, o.center.y, g), new Vector3(.045f, o.height - .1f, .05f), ro);
                z[szyba].Box(L(o.center.x, o.center.y, g - .01f), new Vector3(o.width - .1f, o.height - .1f, .012f), ro);
                z[obrob].Box(L(o.center.x, o.yMin - .025f, .03f), new Vector3(o.width + .12f, .05f, .2f), ro);
                foreach (float sgn in new[] { -1f, 1f })
                {
                    float x = sgn < 0 ? o.xMin - o.width * .25f - .02f : o.xMax + o.width * .25f + .02f;
                    z[okien].Box(L(x, o.center.y, .03f), new Vector3(o.width * .5f, o.height + .04f, .04f), ro);
                    for (float yy = o.yMin + .15f; yy < o.yMax - .1f; yy += .16f) z[okien].Box(L(x, yy, .055f), new Vector3(o.width * .44f, .025f, .012f), ro);   // żaluzja okiennicy
                }
            }
        }
        foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) z[obrob].Box(P(a * (w * .5f - .02f), h * .5f, b * (d * .5f - .02f)), new Vector3(.14f, h, .14f), rot);   // narożniki
        z[obrob].Box(P(0, .07f, 0), new Vector3(w + .06f, .14f, d + .06f), rot);   // listwa cokołowa
        // 3) dach dwuspadowy (kalenica równoległa do frontu), szczyty, okap 0.5 m, deski czołowe, rynny i rura spustowa
        float kat = 32f * Mathf.Deg2Rad, ovE = .5f, ovG = .4f, hr = d * .5f * Mathf.Tan(kat);
        var ss = BraxUliceKit.S(sciana);
        foreach (float sx in new[] { -1f, 1f })
        {
            var a0 = P(sx * w * .5f, h, -d * .5f); var a1 = P(sx * w * .5f, h, d * .5f); var ap = P(sx * w * .5f, h + hr, 0);
            ss.Tri(a0, a1, ap, rg * sx);
        }
        float sl = (d * .5f + ovE) / Mathf.Cos(kat);
        foreach (float sz in new[] { -1f, 1f })
        {
            var pr = rot * Quaternion.Euler(sz * kat * Mathf.Rad2Deg, 0, 0);
            var cen = P(0, h + hr - (d * .5f + ovE) * .5f * Mathf.Tan(kat), sz * (d * .5f + ovE) * .5f) + Vector3.up * .08f;
            z[dachM].Box(cen, new Vector3(w + ovG * 2f, .1f, sl), pr);
            var eave = P(0, h - ovE * Mathf.Tan(kat), sz * (d * .5f + ovE)) + Vector3.up * .05f;
            z[obrob].Box(eave + fw * sz * .03f, new Vector3(w + ovG * 2f, .2f, .04f), rot);                                                       // deska czołowa
            z[m.stal].Rura(eave + fw * sz * .12f - rg * (w * .5f + ovG) + Vector3.down * .08f, eave + fw * sz * .12f + rg * (w * .5f + ovG) + Vector3.down * .08f, .07f, 8);   // rynna
            if (sz > 0)
            {
                var rs0 = eave + fw * .12f + rg * (w * .5f + ovG - .1f) + Vector3.down * .1f;
                z[m.stal].Rura(rs0, new Vector3(rs0.x, yMin + .5f, rs0.z) + fw * .0f, .045f, 8);                                                      // rura spustowa
                z[m.stal].Rura(new Vector3(rs0.x, yMin + .5f, rs0.z), new Vector3(rs0.x, yMin + .5f, rs0.z) + fw * .25f + Vector3.down * .15f, .045f, 8);
            }
        }
        z[obrob].Rura(P(-w * .5f - ovG, h + hr + .16f, 0), P(w * .5f + ovG, h + hr + .16f, 0), .08f, 8);   // gąsior kalenicy
        // 4) taras: deski z fugą, legary, balustrada (bez frontu przy schodach), schody do terenu
        for (float t = .07f; t < w - .05f; t += .15f) z[deskaT].Box(P(-w * .5f + t, -.03f, d * .5f + tD * .5f), new Vector3(.135f, .05f, tD), rot);
        var bal = BraxMiastoA.Mat("Wyspa: balustrada tarasu " + paleta, pal.obrobki, .6f);
        var tA = P(-w * .5f + .05f, 0, d * .5f + .05f); var tB = P(-w * .5f + .05f, 0, d * .5f + tD - .05f); var tC = P(w * .5f - .05f, 0, d * .5f + tD - .05f); var tDd = P(w * .5f - .05f, 0, d * .5f + .05f);
        BraxNabrzezeMebel.Balustrada(z, kol, m, tA, tB, bal, true); BraxNabrzezeMebel.Balustrada(z, kol, m, tC, tDd, bal, true);
        var sL = P(-.7f * K, 0, d * .5f + tD - .05f); var sR = P(.7f * K, 0, d * .5f + tD - .05f);
        BraxNabrzezeMebel.Balustrada(z, kol, m, tB, sL, bal, true); BraxNabrzezeMebel.Balustrada(z, kol, m, sR, tC, bal, true);
        var stopa = c + fw2.normalized * (d * .5f + tD);
        float yStopa = W + H(stopa.x + fw2.normalized.x * 1.5f, stopa.y + fw2.normalized.y * 1.5f);
        BraxNabrzezeMebel.Schody(z, kol, m, P(0, 0, d * .5f + tD), fw, 1.3f * K, yStopa, true);
        int nSt = Mathf.Max(1, Mathf.RoundToInt((y0 - yStopa) / (.15f * K)));
        stopa += fw2.normalized * (nSt * .30f * K + .05f);                          // stopa schodów (tu zaczyna się dojście)
        // meble tarasu: stolik i dwa krzesła (proste, drewno)
        var stol = P(w * .25f, 0, d * .5f + tD * .5f);
        z[deskaT].Box(stol + Vector3.up * .72f, new Vector3(.8f, .04f, .8f), rot); z[m.stalCiemna].Rura(stol, stol + Vector3.up * .7f, .03f, 6);
        foreach (float o in new[] { -.75f, .75f }) { var kp = stol + rg * o; z[deskaT].Box(kp + Vector3.up * .45f, new Vector3(.45f, .04f, .45f), rot); z[deskaT].Box(kp + Vector3.up * .7f + rg * Mathf.Sign(o) * .2f, new Vector3(.04f, .5f, .45f), rot); foreach (var (a, b) in new[] { (-.2f, -.2f), (.2f, -.2f), (-.2f, .2f), (.2f, .2f) }) z[m.stalCiemna].Rura(kp + rg * a + fw * b, kp + rg * a + fw * b + Vector3.up * .43f, .015f, 5); }
        // 5) kolizje bryły i tarasu
        BraxNabrzeze.Kol(kol, "Domek " + tabliczka, P(0, h * .5f + .02f, 0), new Vector3(w, h + .04f, d), rot.eulerAngles.y);
        BraxNabrzeze.Kol(kol, "Taras domku " + tabliczka, P(0, -.25f, d * .5f + tD * .5f), new Vector3(w, .5f, tD), rot.eulerAngles.y);
        BraxNabrzeze.Kol(kol, "Dach domku " + tabliczka, P(0, h + hr * .5f, 0), new Vector3(w + ovG, hr, d * .8f), rot.eulerAngles.y);
        if (!string.IsNullOrEmpty(szyld))
            BraxUliceKit.Napis3D(szyld, P(0, h + .05f - ovE * Mathf.Tan(kat) - .25f, d * .5f + .02f), fw, .3f, .03f, BraxMiastoA.Mat("Wyspa: litery szyldu (granat)", new Color(.08f, .14f, .26f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
        Zajmij(new Vector3(CX + c.x, 0, CZ + c.y) + fw * tD * .5f, Mathf.Max(w, d + tD) * .65f + 1f);
        // zieleń przy domku (krzewy z boków)
        var krz = BraxRegion.Model("shrub_01", "shrub_02", "shrub_03");
        if (krz) foreach (float o in new[] { -1f, 1f }) { var kp = P(o * (w * .5f + 1.1f), 0, d * .25f); kp.y = Y(kp.x, kp.z); var g = BraxRegion.Put(root, krz, "Wyspa: krzew przy domku", kp, Mathf.Repeat(c.x * 37f + c.y * 11f + o * 90f, 360f), .8f, false); if (g) foreach (var rr in g.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.Off; }
        nDomki++;
        return stopa;
    }

    static void PlacZabaw(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m, Vector2 c)
    {
        var C = Sw(c.x, c.y); float y0 = -99; for (float a = -7; a <= 7; a += 3.5f) for (float b = -6; b <= 6; b += 3f) y0 = Mathf.Max(y0, Y(C.x + a, C.z + b));
        var piasek = BraxMiastoA.Tex("Wyspa: piasek piaskownicy", 1.5f, new Color(.95f, .88f, .7f), new Color(.86f, .78f, .6f), .95f, "coast_sand_02", "sand_01");
        var drewno = BraxNabrzeze.Mat("Wyspa: drewno placu zabaw", 1.2f, new Color(.8f, .66f, .5f), "wood_planks_grey", "wooden_planks");
        var czer = BraxMiastoA.Mat("Wyspa: plac zabaw (czerwony)", new Color(.8f, .15f, .12f), .5f);
        var zol = BraxMiastoA.Mat("Wyspa: plac zabaw (żółty)", new Color(.95f, .78f, .15f), .5f);
        var guma = BraxMiastoA.Mat("Wyspa: nawierzchnia bezpieczna (guma)", new Color(.32f, .45f, .3f), .95f);
        // nawierzchnia bezpieczna: płyta 14 × 12 m z cokołem do terenu (nie na terenie)
        float yMin = y0 - 1.2f; for (float a = -7; a <= 7; a += 3.5f) for (float b = -6; b <= 6; b += 3f) yMin = Mathf.Min(yMin, Y(C.x + a, C.z + b) - .3f);
        z[guma].BoxMM(new Vector3(C.x - 7f, yMin, C.z - 6f), new Vector3(C.x + 7f, y0 + .08f, C.z + 6f));
        BraxMiastoA.Kol(kol, "plac zabaw: nawierzchnia", new Vector3(C.x - 7f, yMin, C.z - 6f), new Vector3(C.x + 7f, y0 + .08f, C.z + 6f));
        float y = y0 + .08f;
        // piaskownica (rama z desek, piasek 5 cm niżej niż rama)
        var pk = new Vector3(C.x - 4f, y, C.z + 2.5f);
        foreach (var (o, sz) in new[] { (new Vector3(0, .15f, -1.5f), new Vector3(3.2f, .3f, .2f)), (new Vector3(0, .15f, 1.5f), new Vector3(3.2f, .3f, .2f)), (new Vector3(-1.5f, .15f, 0), new Vector3(.2f, .3f, 2.8f)), (new Vector3(1.5f, .15f, 0), new Vector3(.2f, .3f, 2.8f)) })
            z[drewno].Box(pk + o, sz);
        z[piasek].BoxMM(pk + new Vector3(-1.4f, .01f, -1.4f), pk + new Vector3(1.4f, .22f, 1.4f));
        BraxNabrzeze.Kol(kol, "Piaskownica", pk + Vector3.up * .15f, new Vector3(3.2f, .3f, 3.2f));
        // huśtawka (rama A, belka, 2 siedziska na łańcuchach)
        var hs = new Vector3(C.x + 2.5f, y, C.z + 2.5f); float hh = 2.4f * K * .9f;
        foreach (float o in new[] { -1.8f, 1.8f }) foreach (float l in new[] { -.7f, .7f }) z[czer].Rura(hs + new Vector3(o, 0, l), hs + new Vector3(o, hh, 0), .06f, 8);
        z[czer].Rura(hs + new Vector3(-1.9f, hh, 0), hs + new Vector3(1.9f, hh, 0), .07f, 8);
        foreach (float o in new[] { -.8f, .8f }) { foreach (float l in new[] { -.22f, .22f }) z[m.stal].Rura(hs + new Vector3(o + l, hh, 0), hs + new Vector3(o + l, .45f, 0), .012f, 5); z[zol].Box(hs + new Vector3(o, .43f, 0), new Vector3(.5f, .05f, .22f)); }
        BraxNabrzeze.Kol(kol, "Huśtawka", hs + Vector3.up * hh * .5f, new Vector3(4f, hh, .3f));
        // zjeżdżalnia (podest z drabinką i ślizg)
        var zj = new Vector3(C.x + 2f, y, C.z - 3f); float hp = 1.5f;
        foreach (var (a, b) in new[] { (-.6f, -.6f), (.6f, -.6f), (-.6f, .6f), (.6f, .6f) }) z[drewno].Rura(zj + new Vector3(a, 0, b), zj + new Vector3(a, hp + 1f, b), .06f, 8);
        z[drewno].Box(zj + Vector3.up * hp, new Vector3(1.3f, .08f, 1.3f));
        z[czer].Box(zj + Vector3.up * (hp + 1f), new Vector3(1.5f, .1f, 1.5f));
        for (float t = .3f; t < hp; t += .3f) z[m.stal].Rura(zj + new Vector3(-.4f, t, -.65f), zj + new Vector3(.4f, t, -.65f), .025f, 5);
        var s0 = zj + new Vector3(0, hp, .65f); var s1 = zj + new Vector3(0, .3f, 3.4f); var sd = s1 - s0;
        z[zol].Box((s0 + s1) * .5f, new Vector3(.6f, .05f, sd.magnitude), Quaternion.LookRotation(sd.normalized, Vector3.up));
        foreach (float o in new[] { -.32f, .32f }) z[zol].Box((s0 + s1) * .5f + Vector3.right * o + Vector3.up * .1f, new Vector3(.04f, .2f, sd.magnitude), Quaternion.LookRotation(sd.normalized, Vector3.up));
        BraxNabrzeze.Kol(kol, "Zjeżdżalnia", zj + Vector3.up * hp * .5f, new Vector3(1.4f, hp, 1.4f));
        // ogrodzenie niskie z furtką (od strony chodnika: zachód)
        var plot = BraxMiastoA.Mat("Wyspa: płotek placu zabaw", new Color(.95f, .95f, .93f), .6f);
        var rogi = new[] { new Vector3(C.x - 7f, y, C.z - 6f), new Vector3(C.x + 7f, y, C.z - 6f), new Vector3(C.x + 7f, y, C.z + 6f), new Vector3(C.x - 7f, y, C.z + 6f) };
        for (int i = 0; i < 4; i++)
        {
            var a = rogi[i]; var b = rogi[(i + 1) % 4];
            if (i == 3) { var mid = (a + b) * .5f; BraxNabrzezeMebel.Balustrada(z, kol, m, a, mid + (a - mid).normalized * .8f, plot, false); BraxNabrzezeMebel.Balustrada(z, kol, m, mid + (b - mid).normalized * .8f, b, plot, false); }
            else BraxNabrzezeMebel.Balustrada(z, kol, m, a, b, plot, false);
        }
        BraxNabrzezeMebel.Lawka(z, root, m, new Vector3(C.x - 5.5f, y, C.z - 3.5f), 90f, "Ławka placu zabaw"); nLawki++;
        Zajmij(C, 9f);
        placZabaw = C;
        Miejsca["wyspa_plac_zabaw"] = new Vector3(C.x - 7.5f, y, C.z);
        Czlowiek("Plac zabaw", "rodzic", "siedzi", new Vector3(C.x - 5.5f, y + .45f * K, C.z - 3.5f), Vector3.right, KlSiedzi);
    }

    static void Osrodek(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m)
    {
        chodniki.Clear();
        matChodnik = BraxMiastoA.Tex("Wyspa: chodnik (kostka betonowa z fugami)", .9f, new Color(.86f, .84f, .8f), new Color(.72f, .7f, .66f), .9f, "concrete_pavers_03", "concrete_pavers_02", "concrete_pavement_02");
        matKraw = BraxMiastoA.Tex("Wyspa: krawężnik i cokół chodnika (granit)", 1.2f, new Color(.75f, .74f, .72f), new Color(.6f, .6f, .58f), .85f, "granite_tile_02", "concrete_floor_02");
        float szer = 2.5f * K;
        // ALEJE (bez aut): A1 z = 45 (plac areny → wschód), łącznik od placu, A2 x = 45 (plaża → północ), A3 z = 85
        plan.Clear();
        var A1a = new Vector2(Plac.x, 45f); var A1b = new Vector2(120f, 45f);
        Planuj(A1a + new Vector2(-szer * .5f, 0), A1b, szer, true, "aleja A1");
        Planuj(new Vector2(45f, -92f), new Vector2(45f, 115f), szer, true, "aleja A2 (plaża ↔ domki)");
        Planuj(new Vector2(-30f, 85f), new Vector2(105f, 85f), szer, true, "aleja A3");
        Planuj(new Vector2(Plac.x, Plac.y + PlacR - 2f), A1a, szer, true, "łącznik z placu areny");
        int glowne = plan.Count;
        // RECEPCJA (przy wejściu od placu), SKLEP (skrzyżowanie A1/A2), PLAC ZABAW (między A1 i A3)
        int pal = 0;
        var rec = Domek(z, m, new Vector2(-24f, 60f), new Vector2(0, -1), 11f * K * .85f, 7f * K * .85f, 0, "RECEPCJA", "RECEPCJA OŚRODKA");
        if (rec.HasValue) { recepcja = Sw(rec.Value.x, rec.Value.y); Miejsca["wyspa_recepcja"] = recepcja; BraxMiastoA.Akcja(root, "wyspa_recepcja", "ZAMELDUJ", "Recepcja ośrodka: domki na doby i tygodnie, klucze, ręczniki, rowerki wodne na plaży.", recepcja, 2.2f); Czlowiek("Recepcja", "recepcjonistka", "stoi", recepcja + new Vector3(0, 0, -.5f), Vector3.forward, KlStoi); Planuj(rec.Value, new Vector2(rec.Value.x, 45f), 1.6f * K, false, "dojście do recepcji"); }
        var sk = Domek(z, m, new Vector2(60f, 30f), new Vector2(-1, 0), 8f * K * .85f, 6f * K * .85f, 4, "SKLEP", "SKLEP · KAWIARNIA");
        if (sk.HasValue) { sklep = Sw(sk.Value.x, sk.Value.y); Miejsca["wyspa_sklep"] = sklep; BraxMiastoA.Akcja(root, "wyspa_sklep", "KUP", "Sklep ośrodka: woda, lody, krem z filtrem, pieczywo rano.", sklep, 2.2f); Planuj(sk.Value, new Vector2(45f, sk.Value.y), 1.6f * K, false, "dojście do sklepu"); }
        try { PlacZabaw(z, m, new Vector2(62f, 65f)); } catch (System.Exception e) { BraxMiastoA.Log("WYSPA DO POPRAWKI plac zabaw: " + e.Message); }
        // DOMKI: działki wzdłuż alej, front do chodnika; na złym terenie (woda, stromo) — pominięte
        var dzialki = new List<(Vector2 c, Vector2 fw, Vector2 aleja)>();
        foreach (float x in new[] { -6f, 14f, 30f, 76f, 96f }) dzialki.Add((new Vector2(x, 31f), new Vector2(0, 1), new Vector2(x, 45f - szer * .5f)));     // A1 południe
        foreach (float x in new[] { -4f, 16f, 80f, 100f }) dzialki.Add((new Vector2(x, 59f), new Vector2(0, -1), new Vector2(x, 45f + szer * .5f)));       // A1 północ
        foreach (float x in new[] { -14f, 6f, 26f, 64f, 84f }) dzialki.Add((new Vector2(x, 99f), new Vector2(0, -1), new Vector2(x, 85f + szer * .5f)));  // A3 północ
        foreach (float x in new[] { -12f, 8f, 28f, 88f }) dzialki.Add((new Vector2(x, 72f), new Vector2(0, 1), new Vector2(x, 85f - szer * .5f)));         // A3 południe
        foreach (float zz in new[] { 8f, -12f }) { dzialki.Add((new Vector2(31f, zz), new Vector2(1, 0), new Vector2(45f - szer * .5f, zz))); dzialki.Add((new Vector2(59f, zz), new Vector2(-1, 0), new Vector2(45f + szer * .5f, zz))); }   // A2
        var rnd = new System.Random(1150); int nr = 0;
        foreach (var dz in dzialki)
        {
            if (nDomki >= 22) break;
            if (Vector2.Distance(dz.c, Plac) < PlacR + 12f) continue;
            if (Zajete(CX + dz.c.x, CZ + dz.c.y, 3f)) continue;
            float w = (5.6f + (float)rnd.NextDouble() * 1.6f) * K * .85f, d = (4.4f + (float)rnd.NextDouble() * 1.0f) * K * .85f;
            var st = Domek(z, m, dz.c, dz.fw, w, d, pal++, "DOMEK " + (nr + 1));
            if (!st.HasValue) { pal--; continue; }
            nr++;
            Planuj(st.Value, dz.aleja + dz.fw.normalized * szer * .5f, 1.3f * K, false, "dojście do domku " + nr);
            Miejsca["wyspa_domek_" + nr] = Sw(st.Value.x, st.Value.y);
            if (nr % 3 == 1) Czlowiek("Taras domku " + nr, "wczasowicz", "siedzi", Sw(dz.c.x, dz.c.y) + new Vector3(dz.fw.x, 0, dz.fw.y) * 3f, new Vector3(dz.fw.x, 0, dz.fw.y), KlSiedzi);
        }
        // latarnie i ławki przy alejach
        foreach (var (a, b, hw) in chodniki.Take(glowne).ToArray())
        {
            var dir = (b - a).normalized; var bk = new Vector2(dir.y, -dir.x); float L = Vector2.Distance(a, b);
            for (float t = 6f; t < L - 3f; t += 22f)
            {
                var p = a + dir * t + bk * (hw + .2f * K + .6f) * ((int)(t / 22f) % 2 == 0 ? 1 : -1);
                if (!NaLadzie(p.x, p.y, 1.2f) || Vector2.Distance(p, Plac) < PlacR + 1f || WInnym(p, -1, .5f, false) || Zajete(CX + p.x, CZ + p.y, .8f)) continue;
                var w3 = Sw(p.x, p.y); BraxNabrzezeMebel.Latarnia(z, root, m, w3, Quaternion.LookRotation(new Vector3(-bk.x, 0, -bk.y)).eulerAngles.y); nLatarnie++; Zajmij(w3, .6f);
                if ((int)(t / 22f) % 3 == 1)
                {
                    var lp = a + dir * (t + 5f) + bk * (hw + .2f * K + 1.0f); if (!NaLadzie(lp.x, lp.y, 1.2f) || WInnym(lp, -1, 1.2f, false) || Zajete(CX + lp.x, CZ + lp.y, 1.2f)) continue;
                    var l3 = Sw(lp.x, lp.y); BraxNabrzezeMebel.Lawka(z, root, m, l3, Quaternion.LookRotation(new Vector3(-bk.x, 0, -bk.y)).eulerAngles.y, "Ławka alei ośrodka"); nLawki++; Zajmij(l3, 1.4f);
                    var kp = a + dir * (t + 6.6f) + bk * (hw + .2f * K + .7f); var k3 = Sw(kp.x, kp.y); BraxNabrzezeMebel.Kosz(z, root, m, k3); nKosze++;
                }
            }
        }
        // budowa wszystkich chodników (główne pierwsze — mają pierwszeństwo na skrzyżowaniach i przy dojściach)
        for (int k = 0; k < plan.Count; k++) { try { Chodnik(z, k); } catch (System.Exception e) { BraxMiastoA.Log("WYSPA DO POPRAWKI chodnik " + plan[k].nazwa + ": " + e.Message); } }
        // spacerowicze po alejach (trasy wzdłuż A1 i A2)
        var tA1 = new List<Vector3>(); for (float x = Plac.x + 10f; x <= 110f; x += 12f) if (NaLadzie(x, 45f, 1.2f)) tA1.Add(Sw(x, 45f) + Vector3.up * .09f);
        var tA2 = new List<Vector3>(); for (float zz = -50f; zz <= 110f; zz += 12f) if (NaLadzie(45f, zz, 1.2f)) tA2.Add(Sw(45f, zz) + Vector3.up * .09f);
        for (int k = 0; k < 6; k++) { var t = k % 2 == 0 ? tA1 : tA2; if (t.Count < 2) continue; int s = (k * 3) % (t.Count - 1); Czlowiek("Aleja ośrodka", "wczasowicz", "spacer", t[s], t[s + 1] - t[s], KlSpacer, t.ToArray()); }
        BraxMiastoA.Log("WYSPA ośrodek domków: domków " + nDomki + " (z recepcją i sklepem), chodników " + mChodnika.ToString("F0") + " m, krawężników " + nKrawezniki + " (odcinki 1 m z fugą), latarni " + nLatarnie + ", ławek " + nLawki + ", koszy " + nKosze + ", plac zabaw " + (placZabaw != Vector3.zero ? "tak" : "nie") + ", aut 0");
    }

    // tablica „WYSPA” na głowicy pomostu (widoczna z łodzi) i drogowskazy na placu
    static void TablicaWyspa(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m)
    {
        if (!Miejsca.TryGetValue("wyspa_przystan", out var pr)) return;
        var tab = pr + new Vector3(-1.5f, 0, 4.5f);
        var deska = BraxMiastoA.Mat("Wyspa: tablica WYSPA (granat)", new Color(.06f, .14f, .28f), .6f);
        foreach (float o in new[] { -2.4f, 2.4f }) z[m.stalCiemna].Rura(tab + new Vector3(0, -.2f, o), tab + new Vector3(0, 3.6f, o), .08f, 10);
        z[deska].Box(tab + new Vector3(0, 2.9f, 0), new Vector3(.12f, 1.2f, 5.4f));
        BraxUliceKit.Napis3D("WYSPA", tab + new Vector3(-.07f, 2.75f, 0), Vector3.left, .75f, .06f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
        BraxNabrzeze.Kol(kol, "Tablica WYSPA", tab + new Vector3(0, 1.8f, 0), new Vector3(.3f, 3.6f, 5.4f));
        // drogowskaz przy wyjściu z placu na łącznik do ośrodka
        var dw = Sw(Plac.x + 3.5f, Plac.y + PlacR - 1f); dw.y = Mathf.Max(dw.y, W + PlacH);
        z[m.stalCiemna].Rura(dw, dw + Vector3.up * 2.6f, .05f, 8);
        z[deska].Box(dw + new Vector3(0, 2.3f, .9f), new Vector3(.05f, .35f, 1.9f));
        z[deska].Box(dw + new Vector3(.9f, 1.85f, 0), new Vector3(1.9f, .35f, .05f));
        BraxUliceKit.Napis3D("RECEPCJA · DOMKI", dw + new Vector3(-.03f, 2.24f, .9f), Vector3.left, .12f, .01f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
        BraxUliceKit.Napis3D("PLAŻA", dw + new Vector3(.9f, 1.8f, -.03f), Vector3.back, .12f, .01f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
    }

    // =================================================================================================================
    // OTWARTE MORZE DO WYSPY: woda za x 884 (woda portu kończy się na 884), dno (kolizja), strażnik głębokości
    // =================================================================================================================
    static void MorzeOtwarte(Transform cityRoot)
    {
        var r = BraxRegion.Root(cityRoot, "CLAUDE A: morze otwarte do wyspy (woda, dno)");
        Material woda = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/CityRebuildWater/HarbourWater.mat");
        if (!woda) foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (mr.sharedMaterial && mr.sharedMaterial.name.Contains("HarbourWater")) { woda = mr.sharedMaterial; break; }
        const float x0 = 884f, x1 = 1720f, z0 = -340f, z1 = 460f; const int n = 8;
        if (woda)
        {
            var v = new List<Vector3>(); var nn = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++) { var p = new Vector3(Mathf.Lerp(x0, x1, i / (float)n), W, Mathf.Lerp(z0, z1, j / (float)n)); v.Add(p); nn.Add(Vector3.up); uv.Add(new Vector2(p.x, p.z)); }
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) { int a = i * (n + 1) + j, b = a + n + 1; t.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b }); }
            var mesh = new Mesh { name = "Morze otwarte do wyspy" }; mesh.SetVertices(v); mesh.SetNormals(nn); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var bnd = mesh.bounds; bnd.Expand(new Vector3(0, .4f, 0)); mesh.bounds = bnd;
            BraxZapisAssetu.Utworz(mesh, BraxMiastoA.Out + "/Wyspa/morze_otwarte.asset");
            var g = new GameObject("Morze otwarte (x 884–1720)"); g.transform.SetParent(r, false); g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr2 = g.AddComponent<MeshRenderer>(); mr2.sharedMaterial = woda; mr2.shadowCastingMode = ShadowCastingMode.Off;
        }
        else BraxMiastoA.Log("WYSPA DO POPRAWKI: brak materiału HarbourWater — morze za x 884 bez wody");
        var dno = new GameObject("Morze otwarte: dno (kolizja)"); dno.transform.SetParent(r, false);
        var bc = dno.AddComponent<BoxCollider>(); bc.center = new Vector3((x0 + 16f + x1) * .5f, -7.5f, (z0 + z1) * .5f); bc.size = new Vector3(x1 - x0 - 16f, 1f, z1 - z0);
        foreach (var st in Object.FindObjectsByType<BraxWodaGlebia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { st.morze = Rect.MinMaxRect(84f, -520f, x1, 470f); EditorUtility.SetDirty(st); }
    }

    // =================================================================================================================
    // RAPORT
    // =================================================================================================================
    static void RaportWyspy(int tris, int palm)
    {
        float ladM2 = 0; float lx0 = 1e9f, lx1 = -1e9f, lz0 = 1e9f, lz1 = -1e9f;
        for (float x = -240f; x <= 240f; x += 2f) for (float zz = -240f; zz <= 240f; zz += 2f)
                if (H(x, zz) > 0f) { ladM2 += 4f; lx0 = Mathf.Min(lx0, x); lx1 = Mathf.Max(lx1, x); lz0 = Mathf.Min(lz0, zz); lz1 = Mathf.Max(lz1, zz); }
        float brzegZach = CX - LiniaWody(180f), brzegMiasta = BraxMiastoGranice.Brzeg(CZ);
        // płyty nad terenem bez podparcia (M10): płaskie (< 0.6 m), ≥ 4 m², spód > 1 m nad terenem na lądzie
        var plyty = new List<string>();
        foreach (var rr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var b = rr.bounds; if (b.size.y > .6f || b.size.x * b.size.z < 4f || b.size.x > 60f || b.size.z > 60f) continue;
            string nm = rr.name.ToLowerInvariant(); if (nm.Contains("arena") || nm.Contains("trybun") || nm.Contains("dach") || nm.Contains("telebim") || nm.Contains("klatk")) continue;
            float g = Y(b.center.x, b.center.z); if (g < W + .3f) continue;
            if (b.min.y > g + 1.0f) plyty.Add(rr.name + " @" + b.center.ToString("F0") + " " + (b.min.y - g).ToString("F1") + " m nad terenem");
        }
        string linia = "BRAXHORN_WYSPA odleglosc_od_brzegu=" + (brzegZach - brzegMiasta).ToString("F0") + " m (brzeg miasta x " + brzegMiasta.ToString("F0") + " → brzeg wyspy x " + brzegZach.ToString("F0") + ")"
            + " srodek=(" + CX + ", " + CZ + ") lad=" + (lx1 - lx0).ToString("F0") + "×" + (lz1 - lz0).ToString("F0") + " m (" + ladM2.ToString("F0") + " m²)"
            + " plaza_m2=" + plazaM2.ToString("F0") + " domki=" + nDomki + " lezaki=" + nLezaki + " ratownicy=" + wiezyczki.Count + " przebieralnie=" + nPrzebieralnie
            + " chodniki_m=" + mChodnika.ToString("F0") + " krawezniki=" + nKrawezniki + " deptak_plazy_m=" + mDeptakuPlazy.ToString("F0") + " latarnie=" + nLatarnie + " lawki=" + nLawki
            + " kapiacy_sie=" + nKapiacy + " palmy=" + palm + " teren_trojkatow=" + tris + " auta=0 arena=bez_zmian(przeniesiona)"
            + " plyty_nad_terenem=" + plyty.Count + (plyty.Count > 0 ? " [" + string.Join(" | ", plyty.Take(12)) + "]" : "");
        Debug.Log(linia); BraxRegion.Note(linia);
    }

    // =================================================================================================================
    // TOR WODNY z mariny do wyspy + kontrola wyjść łodzi + mapa miasta (krok po statkach — kontenerowiec na redzie już stoi)
    // =================================================================================================================
    static bool Przeszkoda(Vector3 p, out Collider c)
    {
        c = null;
        if (!Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out var hit, 20f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) return false;
        if (hit.point.y <= W - .8f) return false;
        if (hit.collider.GetComponentInParent<BraxVessel>()) return false;
        c = hit.collider; return true;
    }

    public static void TorWodny(Transform cityRoot)
    {
        Physics.SyncTransforms();
        float pz = CZ + Plac.y;
        Vector3 cel = Miejsca.TryGetValue("wyspa_przystan", out var pr) ? new Vector3(pr.x - 4.2f * K - 14f, W, pz) : new Vector3(CX - 240f, W, pz);
        var start = new Vector3(BraxNabrzezeMarina.DX1 + 22f, W, pz);
        float pol = 30f * K;   // połowa szerokości toru (60 m × k)
        var kolizje = new Dictionary<string, int>(); float minSz = 999f; int probki = 0;
        for (float x = start.x; x <= cel.x; x += 5f)
        {
            float l = 0, rgt = 0;
            for (float o = 0; o <= pol + 12f; o += 1.5f) { if (Przeszkoda(new Vector3(x, W, pz + o), out var c)) { if (o <= pol) { string k = c.name + " (" + c.transform.root.name + ")"; kolizje[k] = kolizje.TryGetValue(k, out var q) ? q + 1 : 1; } break; } l = o; }
            for (float o = 0; o <= pol + 12f; o += 1.5f) { if (Przeszkoda(new Vector3(x, W, pz - o), out var c)) { if (o <= pol) { string k = c.name + " (" + c.transform.root.name + ")"; kolizje[k] = kolizje.TryGetValue(k, out var q) ? q + 1 : 1; } break; } rgt = o; }
            minSz = Mathf.Min(minSz, l + rgt); probki++;
        }
        // boje toru co 60 m: czerwone (południe), zielone (północ) — bez kolizji (łódź ich nie „łapie”)
        var r = BraxRegion.Root(cityRoot, "CLAUDE A: tor wodny do wyspy (boje)");
        var z = new BraxNabrzeze.Zbior(); var m = BraxNabrzezeMebel.Materialy();
        var ziel = BraxNabrzeze.Kolor("Tor wodny: boja zielona", new Color(.1f, .55f, .25f), .5f);
        int boje = 0;
        for (float x = start.x + 10f; x <= cel.x - 10f; x += 60f)
        {
            foreach (var (s, mat) in new[] { (-1f, m.czerwony), (1f, ziel) })
            {
                var p = new Vector3(x, W, pz + s * (pol + 2f));
                BraxNabrzezeMebel.Boja(z, mat, p + Vector3.down * .15f, .55f);
                z[mat].Rura(p + Vector3.up * .3f, p + Vector3.up * 1.8f, .06f, 8);
                z[m.zolty].Box(p + Vector3.up * 1.9f, new Vector3(.3f, .3f, .3f));
                boje++;
            }
        }
        z.Emit(r, "Tor wodny: boje");
        // wyjścia łodzi z mariny: 60 m przed dziobem wolne?
        var wyjscia = new List<string>();
        foreach (var v in Object.FindObjectsByType<BraxVessel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var f = v.transform.forward; f.y = 0; f.Normalize(); string wynik = "wolne 60 m";
            for (float d = v.length * .5f + .6f; d <= 60f; d += 2f)
            {
                var p = v.transform.position + f * d;
                if (Przeszkoda(p, out var c)) { wynik = "PRZESZKODA " + c.name + " po " + d.ToString("F0") + " m"; break; }
                bool lodz = false;
                foreach (var o in Object.FindObjectsByType<BraxVessel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (o == v) continue; var dd = p - o.transform.position; dd.y = 0; if (dd.sqrMagnitude > o.length * o.length) continue;
                    var of = o.transform.forward; of.y = 0; of.Normalize(); float along = Mathf.Clamp(Vector3.Dot(dd, of), -o.length * .5f + o.width * .5f, o.length * .5f - o.width * .5f);
                    if ((dd - of * along).magnitude < o.width * .5f + v.width * .5f) { wynik = "PRZESZKODA łódź " + o.label + " po " + d.ToString("F0") + " m"; lodz = true; break; }
                }
                if (lodz) break;
            }
            wyjscia.Add(v.label + " " + v.name + ": " + wynik);
        }
        int nKol = kolizje.Count;
        Debug.Log("BRAXHORN_TOR_WODNY szerokosc_min=" + (minSz < 999f ? minSz.ToString("F0") : "?") + " m (wymagane " + (pol * 2f).ToString("F0") + ") kolizje_na_trasie=" + nKol
                  + " dlugosc=" + (cel.x - start.x).ToString("F0") + " m od=(" + start.x.ToString("F0") + ", " + pz.ToString("F0") + ") do=(" + cel.x.ToString("F0") + ", " + pz.ToString("F0") + ") probek=" + probki + " boje=" + boje
                  + (nKol > 0 ? " [" + string.Join(", ", kolizje.OrderByDescending(k => k.Value).Take(15).Select(k => k.Key + "×" + k.Value)) + "]" : "")
                  + "\nBRAXHORN_TOR_WODNY wyjscia_lodzi: " + string.Join(" | ", wyjscia));
        try { MapaMiasta(cityRoot, start, cel); } catch (System.Exception e) { Debug.LogWarning("DO POPRAWKI: mapa miasta: " + e); }
    }

    // MAPA MIASTA (MENU → MAPA): ląd, woda, marina, wyspa (plaża, ośrodek, arena), tor wodny przerywaną linią, podpisy
    static void MapaMiasta(Transform cityRoot, Vector3 start, Vector3 cel)
    {
        const float x0 = -880f, z0 = -360f, w = 2600f, h = 840f, ppm = .5f;
        int Wd = (Mathf.RoundToInt(w * ppm) + 3) / 4 * 4, Hd = (Mathf.RoundToInt(h * ppm) + 3) / 4 * 4;
        var px = new Color32[Wd * Hd];
        Color32 woda = new Color32(74, 132, 176, 255), lad = new Color32(206, 202, 192, 255), poza = new Color32(110, 140, 92, 255), piasek = new Color32(232, 214, 160, 255),
                zielen = new Color32(96, 146, 70, 255), plac = new Color32(150, 150, 154, 255), chodnik = new Color32(240, 236, 226, 255), tor = new Color32(255, 214, 40, 255), pomost = new Color32(120, 84, 52, 255);
        for (int j = 0; j < Hd; j++)
            for (int i = 0; i < Wd; i++)
            {
                float x = x0 + i / ppm, zz = z0 + j / ppm; Color32 c;
                float hw = H(x - CX, zz - CZ);
                if (Mathf.Abs(x - CX) < 245f && Mathf.Abs(zz - CZ) < 245f && hw > 0f)
                {
                    float ang = Mathf.Repeat(Mathf.Atan2(zz - CZ, x - CX) * Mathf.Rad2Deg, 360f);
                    bool plaza = ang >= PlazaOd && ang <= PlazaDo && hw < 3.2f;
                    c = Vector2.Distance(new Vector2(x - CX, zz - CZ), Plac) < PlacR ? plac : plaza || hw < .8f ? piasek : zielen;
                }
                else if (BraxMiastoGranice.Lad(x, zz)) c = BraxMiastoGranice.Grywalny(x, zz) ? lad : poza;
                else c = woda;
                px[j * Wd + i] = c;
            }
        void Prost(float ax, float bx, float az, float bz, Color32 c)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(ax, bx) - x0) * ppm), 0, Wd - 1), i1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(ax, bx) - x0) * ppm), 0, Wd - 1);
            int j0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(az, bz) - z0) * ppm), 0, Hd - 1), j1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(az, bz) - z0) * ppm), 0, Hd - 1);
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) px[j * Wd + i] = c;
        }
        foreach (var (a, b, hw) in chodniki) Prost(CX + Mathf.Min(a.x, b.x) - hw, CX + Mathf.Max(a.x, b.x) + hw, CZ + Mathf.Min(a.y, b.y) - hw, CZ + Mathf.Max(a.y, b.y) + hw, chodnik);
        // pomosty mariny i przystań wyspy
        Prost(BraxNabrzezeMarina.PomX0, BraxNabrzezeMarina.PomX1, BraxNabrzezeMarina.PomZ0, BraxNabrzezeMarina.PomZ1, pomost);
        Prost(BraxNabrzezeMarina.DX0, BraxNabrzezeMarina.DX1, BraxNabrzezeMarina.PomZ0, BraxNabrzezeMarina.PomZ1, pomost);
        if (Miejsca.TryGetValue("wyspa_przystan", out var pr)) Prost(pr.x - 4f, CX - LiniaWody(180f), start.z - 2f, start.z + 2f, pomost);
        // tor wodny: przerywana linia (kreski 24 m) + krawędzie
        for (float x = start.x; x < cel.x; x += 40f) Prost(x, Mathf.Min(cel.x, x + 24f), start.z - 2.5f, start.z + 2.5f, tor);
        Prost(start.x, start.x + 4f, start.z - 14f, start.z + 14f, tor); Prost(cel.x - 4f, cel.x, start.z - 14f, start.z + 14f, tor);
        var tex = new Texture2D(Wd, Hd, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
        Directory.CreateDirectory(BraxMiastoA.Out + "/Wyspa");
        string path = BraxMiastoA.Out + "/Wyspa/mapa_miasta.png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = false; imp.isReadable = false; imp.maxTextureSize = 2048;
            imp.wrapMode = TextureWrapMode.Clamp; imp.textureCompression = TextureImporterCompression.Compressed; imp.npotScale = TextureImporterNPOTScale.None; imp.SaveAndReimport();
        }
        var go = new GameObject("BRAXHORN mapa miasta"); go.transform.SetParent(cityRoot, false);
        var mp = go.AddComponent<BraxMapa>(); mp.tytul = "BRAXHORN CITY · WYSPA"; mp.obraz = AssetDatabase.LoadAssetAtPath<Texture2D>(path); mp.swiat = new Rect(x0, z0, w, h);
        var lista = new List<BraxMapa.Miejsce>();
        void M(string n, float x, float zz) => lista.Add(new BraxMapa.Miejsce { nazwa = n, xz = new Vector2(x, zz) });
        M("SIŁOWNIA (START)", 1.5f, 2.8f); M("MARINA", 125f, 52f); M("PORT", 76f, -40f); M("PLAŻA", 112f, 140f); M("RONDO", -126f, 28.5f);
        M("TUNEL NA OSIEDLE", -126f, -80f); M("TOR WODNY → WYSPA", (start.x + cel.x) * .5f, start.z + 46f);
        M("WYSPA", CX, CZ + 175f); M("ARENA BRAXHORN", CX + Plac.x, CZ + Plac.y); M("PLAŻA WYSPY", CX + 10f, CZ - 120f); M("DOMKI LETNISKOWE", CX + 40f, CZ + 72f);
        if (recepcja != Vector3.zero) M("RECEPCJA", recepcja.x, recepcja.z);
        if (Miejsca.TryGetValue("wyspa_przystan", out var p2)) M("PRZYSTAŃ", p2.x, p2.z - 14f);
        mp.miejsca = lista.ToArray();
        Debug.Log("BRAXHORN_MAPA_MIASTA " + Wd + "×" + Hd + " px, świat x " + x0 + "…" + (x0 + w) + " z " + z0 + "…" + (z0 + h) + ", podpisów " + lista.Count + " (WYSPA, tor wodny przerywaną linią)");
    }
}
