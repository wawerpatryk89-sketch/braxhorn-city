using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// MUZEUM — WYPOSAŻENIE I EKSPONATY Z KODU (11.11.2026, właściciel 23:09: „Muzeum Miasta drzwi zjebałeś, zobacz… Je wyposażenie.
// Zrób muzeum.”; 21:52: „Wszystko ma być dopieszczone”; 21:20: „żeby nie robić podwójnej roboty” — klasa do użycia w każdym
// rejonie). Wymiary rzeczywiste × K, siatki raz na wariant (BraxChlodnie.Wez) i stawiane jako obiekty z obrotem (lokalne +z = przód).
//  • KINKIET — latarnia ścienna z czarnej stali: płytka, ramię z zastrzałem, podstawa, 4 słupki narożne, szyby mleczne świecące
//    ciepłą bielą, daszek-ostrosłup z kulką (portal muzeum zamiast latarni na chodniku — zasada 19:40 „na chodnikach nic”).
// Bez złota i mosiądzu (zasada 10.11): okucia i drobny metal ze stali polerowanej lub czarnej.
internal static class BraxMuzeumKod
{
    static float K => BraxClaudeOsiedle.K;
    const string Katalog = BraxRegion.Out + "/Muzeum";
    static Material czern, stal, swiatlo;
    static readonly Color32[] Bl = { new Color32(255, 255, 255, 255) };
    public static int Postawione;

    static void Materialy()
    {
        if (czern) return;
        czern = BraxCityMaterials.Surface("muzeum: stal czarna malowana (latarnie, ramy)", new Color(.045f, .045f, .05f), .38f, .55f);
        stal = BraxCityMaterials.Surface("muzeum: stal nierdzewna polerowana", new Color(.86f, .87f, .88f), .12f, 1f);
        swiatlo = BraxMiastoA.Swieci("muzeum: szkło mleczne latarni (ciepła biel)", new Color(1f, .96f, .88f), new Color(1.5f, 1.38f, 1.18f));
    }

    // ---------------------------------------------------------------- bryły toczone wokół dowolnej osi
    /// <summary>Bryła toczona wokół osi os przez punkt p: profil (r, t wzdłuż osi, indeks koloru) jak BraxChlodnie.Toczony
    /// (normalne po prawej stronie kierunku profilu = na zewnątrz).</summary>
    static void TocOs(UlSiatka s, Vector3 p, Vector3 os, List<Vector3> pr, int seg, Color32[] kol = null)
    {
        var tmp = new UlSiatka(); BraxChlodnie.Toczony(tmp, Vector3.zero, pr, kol ?? Bl, seg);
        var q = Quaternion.FromToRotation(Vector3.up, os.normalized); int k0 = s.v.Count;
        for (int i = 0; i < tmp.v.Count; i++)
        {
            s.v.Add(p + q * tmp.v[i]); s.n.Add(q * tmp.n[i]);
            var t3 = q * new Vector3(tmp.t[i].x, tmp.t[i].y, tmp.t[i].z); s.t.Add(new Vector4(t3.x, t3.y, t3.z, tmp.t[i].w));
            s.uv.Add(tmp.uv[i]); s.c.Add(tmp.c[i]);
        }
        foreach (int ix in tmp.i) s.i.Add(k0 + ix);
    }
    /// <summary>Profil torusa (pierścień o przekroju elipsy ra × rt wokół promienia R, na wysokości t0) — obieg przeciwny do
    /// wskazówek: zewnątrz w górę, góra do środka, wewnątrz w dół, spód na zewnątrz.</summary>
    static List<Vector3> Torus(float R, float ra, float rt, float t0 = 0f, int n = 10)
    {
        var l = new List<Vector3>();
        for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; l.Add(new Vector3(R + ra * Mathf.Cos(a), t0 + rt * Mathf.Sin(a), 0f)); }
        return l;
    }
    /// <summary>Profil kuli (od bieguna dolnego do górnego), środek na wysokości t0 osi.</summary>
    static List<Vector3> KulaProfil(float r, float t0 = 0f, int n = 8)
    {
        var l = new List<Vector3>();
        for (int i = 0; i <= n; i++) { float f = i * Mathf.PI / n; l.Add(new Vector3(r * Mathf.Sin(f), t0 - r * Mathf.Cos(f), 0f)); }
        return l;
    }

    static Transform Postaw(Transform rodzic, string nazwa, string klucz, System.Action<BraxChlodnie.Partie> buduj, Vector3 p, Vector3 przod, bool cien = true)
    {
        Materialy();
        var w = BraxChlodnie.Wez(klucz, buduj);
        var g = BraxChlodnie.Korzen(rodzic, nazwa, p, przod);
        BraxChlodnie.Wstaw(g, nazwa + " (siatka)", w, Vector3.zero, cien);
        Postawione++;
        return g;
    }

    // ---------------------------------------------------------------- kinkiet (latarnia ścienna)
    /// <summary>Kinkiet: punkt = środek płytki na licu ściany, przod = od ściany. Wysokość latarni ~0.5 m × K (płytka w osi ramienia).</summary>
    public static Transform Kinkiet(Transform rodzic, string nazwa, Vector3 punktSciany, Vector3 przod)
        => Postaw(rodzic, "Kinkiet · " + nazwa, "muzeum_kinkiet", BudujKinkiet, punktSciany, przod);

    static void BudujKinkiet(BraxChlodnie.Partie p)
    {
        var c = p[czern]; var sw = p[swiatlo];
        c.Poducha(new Vector3(-.065f, -.13f, 0f), new Vector3(.065f, .13f, .022f), .008f, 0f, 3);          // płytka ścienna
        c.Rura(new Vector3(0f, 0f, .015f), new Vector3(0f, 0f, .31f), .014f, 10);                          // ramię
        c.Rura(new Vector3(0f, -.105f, .015f), new Vector3(0f, -.008f, .21f), .008f, 8);                   // zastrzał
        const float zc = .31f, a = .085f, y0 = .014f, y1 = .32f, sl = .016f;
        c.Poducha(new Vector3(-a - .012f, y0 - .028f, zc - a - .012f), new Vector3(a + .012f, y0, zc + a + .012f), .006f, 0f, 3);   // podstawa
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {   // słupki narożne
                float x0 = sx > 0 ? a - sl : -a, z0 = zc + (sz > 0 ? a - sl : -a);
                c.Box(new Vector3(x0, y0, z0), new Vector3(x0 + sl, y1, z0 + sl));
            }
        // szyby mleczne (świecą) w osi słupków — krawędzie schowane w słupkach, bez wspólnych płaszczyzn
        float xs = a - sl * .5f, zi0 = zc - a + sl, zi1 = zc + a - sl;
        sw.Quad(new Vector3(xs, y0, zi0), new Vector3(xs, y0, zi1), new Vector3(xs, y1, zi1), new Vector3(xs, y1, zi0), Vector3.right);
        sw.Quad(new Vector3(-xs, y0, zi0), new Vector3(-xs, y0, zi1), new Vector3(-xs, y1, zi1), new Vector3(-xs, y1, zi0), Vector3.left);
        sw.Quad(new Vector3(-a + sl, y0, zc + xs), new Vector3(a - sl, y0, zc + xs), new Vector3(a - sl, y1, zc + xs), new Vector3(-a + sl, y1, zc + xs), Vector3.forward);
        sw.Quad(new Vector3(-a + sl, y0, zc - xs), new Vector3(a - sl, y0, zc - xs), new Vector3(a - sl, y1, zc - xs), new Vector3(-a + sl, y1, zc - xs), Vector3.back);
        // daszek: płyta, ostrosłup, kulka
        float r0 = a + .016f, yd = y1 + .02f, ya = y1 + .115f;
        c.Poducha(new Vector3(-r0, y1, zc - r0), new Vector3(r0, yd, zc + r0), .005f, 0f, 3);
        var A = new Vector3(0f, ya, zc);
        var P = new[] { new Vector3(-r0, yd, zc - r0), new Vector3(r0, yd, zc - r0), new Vector3(r0, yd, zc + r0), new Vector3(-r0, yd, zc + r0) };
        for (int i = 0; i < 4; i++)
        {
            var b0 = P[i]; var b1 = P[(i + 1) % 4]; var srodekKr = (b0 + b1) * .5f - new Vector3(0f, yd, zc); srodekKr.y = 0f;
            var n = (srodekKr.normalized * (ya - yd) + Vector3.up * r0).normalized;
            c.Tri(b0, b1, A, n);
        }
        c.Rura(A - Vector3.up * .01f, A + Vector3.up * .035f, .007f, 8);
        BraxChlodnie.Toczony(c, A + Vector3.up * .05f, KulaProfil(.016f), Bl, 10);
    }
}
