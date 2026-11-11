using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// SZYLD BRAXHORN GYM W 3D (MIASTO R1, 11.11.2026; właściciel 14:43: „ten napis BRAXHORN GYM ma zrobić ładny w 3D. Tam to usuń”).
// Dotąd: płaski szyld oryginału nad drzwiami siłowni — Gym_Mesh_48 (granatowa tablica 3.6 × 1.0 m) i Gym_Mesh_34 (obrazek
// z zielonym napisem) leżą w płaszczyźnie szarej ściany (z 11.6), mural 19 omijał je jako otwór. Teraz oba WYŁĄCZONE przed
// muralem (mural przechodzi przez to miejsce), a szyld powstaje z brył wg zasad szyldów 3D (9.11) i napisów premium (10.11):
//  • litery Bebas Neue (SIL OFL 1.1, BraxLiteryBebas) — krój jak na płaskim szyldzie, lico zielone marki (to samo co
//    BRAXHORN NUTRITION obok), boki i tył czarny lakier, litery dystansowe 4 mm od nośnika;
//  • wariant A — TABLICA 3D w miejscu płaskiego szyldu: skrzynka z grafitu malowanego proszkowo 14 cm, boki ze stali
//    szczotkowanej, pod literami listwa LED;
//  • wariant B — DASZEK NAD DRZWIAMI: płyta stalowa 4.2 × 1.4 m na dwóch cięgnach do muru, litery na czole daszka,
//    3 oprawy downlight nad wejściem; mural nad daszkiem bez przerwy.
// Wymiary z pomiaru sceny (obrys płaskiego szyldu i drzwi), w razie braku — z APK 2bfadac. Log BRAXHORN_GYM_SZYLD_3D.
internal static class BraxSzyldGym3D
{
    /// <summary>A = tablica 3D w miejscu płaskiego szyldu; B = daszek nad drzwiami z literami na czole (wybór właściciela).</summary>
    public const char Wariant = 'B';   // właściciel 11.11 15:04: „B.”
    const string Tekst = "BRAXHORN GYM";
    static Bounds plaski = new Bounds(new Vector3(6f, 4.85f, 11.61f), new Vector3(3.6f, 1f, .03f));
    static readonly List<string> zdjete = new List<string>();

    /// <summary>Wyłącza płaski szyld oryginału nad drzwiami siłowni (tablica + obrazek w płaszczyźnie szarej ściany). Wołane
    /// PRZED muralem 19 — wtedy mural nie robi w tym miejscu otworu. Zwraca liczbę wyłączonych brył.</summary>
    public static int ZdejmijPlaski()
    {
        zdjete.Clear(); bool jest = false; var b = new Bounds();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r || !r.enabled || !r.name.StartsWith("Gym_Mesh_", System.StringComparison.Ordinal)) continue;
            var rb = r.bounds;
            if (rb.center.x < 2.5f || rb.center.x > 9.5f || rb.center.y < 3.9f || rb.center.y > 6f || rb.center.z < 11.2f || rb.center.z > 12f) continue;
            if (rb.size.x < 1.5f || rb.size.x > 5f || rb.size.y > 1.6f || rb.size.z > .2f) continue;
            r.enabled = false;
            foreach (var c in r.GetComponents<Collider>()) c.enabled = false;
            zdjete.Add(r.name + " " + rb.size.x.ToString("0.00") + "×" + rb.size.y.ToString("0.00") + " m");
            if (!jest) { b = rb; jest = true; } else b.Encapsulate(rb);
        }
        if (jest) plaski = b;
        Debug.Log("BRAXHORN_GYM_SZYLD_3D płaski szyld oryginału wyłączony: " + (zdjete.Count > 0 ? string.Join(", ", zdjete) : "BRAK (nie znaleziono — szyld 3D stanie w miejscu z APK 2bfadac)") +
                  "; obrys x " + plaski.min.x.ToString("0.00") + "…" + plaski.max.x.ToString("0.00") + ", y " + plaski.min.y.ToString("0.00") + "…" + plaski.max.y.ToString("0.00"));
        return zdjete.Count;
    }

    static Bounds Drzwi(float lico)
    {
        bool jest = false; var b = new Bounds();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r || !r.name.StartsWith("Gym_Mesh_", System.StringComparison.Ordinal)) continue;
            var rb = r.bounds;
            if (rb.min.y > .15f || rb.max.y < 2.6f || rb.max.y > 3.4f || rb.size.x < 1.2f || rb.size.x > 2.4f) continue;
            if (Mathf.Abs(rb.center.x - plaski.center.x) > 2f || rb.max.z < lico - .2f || rb.min.z > lico + .2f) continue;
            if (!jest) { b = rb; jest = true; } else b.Encapsulate(rb);
        }
        if (!jest) b.SetMinMax(new Vector3(5.08f, 0f, lico - .1f), new Vector3(6.92f, 3.12f, lico));
        return b;
    }

    /// <summary>Szyld 3D na szarej ścianie (lico = z lica ściany, zwrot +z do ulicy). Zwraca opis do logu.</summary>
    public static string Postaw(Transform rodzic, float lico)
    {
        if (float.IsNaN(lico)) lico = 11.6f;
        var g = new GameObject("BRAXHORN GYM — szyld 3D (wariant " + Wariant + ")").transform; g.SetParent(rodzic, false);
        BraxUliceKit.Start(); BraxUliceKit.UstawGrupe("GYM: szyld 3D");
        var zielone = BraxMiastoA.Swieci("GYM szyld: litery zielone (neon marki)", new Color(.45f, 1f, .2f), new Color(.9f, 2.4f, .35f));
        var boki = BraxMiastoA.Mat("GYM szyld: boki i tył liter (czarny lakier)", new Color(.035f, .035f, .04f), .4f, .5f);
        var grafit = BraxMiastoA.Mat("GYM szyld: grafit malowany proszkowo", new Color(.085f, .09f, .1f), .5f, .55f);
        var stal = BraxMiastoA.Mat("GYM szyld: stal szczotkowana", new Color(.6f, .61f, .62f), .32f, .9f);
        var led = BraxMiastoA.Swieci("GYM szyld: listwa LED zielona", new Color(.5f, 1f, .3f), new Color(1.2f, 3.2f, .5f));
        var lampa = BraxMiastoA.Swieci("GYM szyld: oprawa downlight", new Color(1f, .97f, .9f), new Color(2.4f, 2.3f, 2.1f));
        string opis;
        if (Wariant == 'B') opis = Daszek(lico, zielone, boki, grafit, stal, lampa);
        else opis = Tablica(lico, zielone, boki, grafit, stal, led);
        int siatek = BraxUliceKit.Zapisz(g, "gym_szyld_3d");
        opis = "szyld BRAXHORN GYM 3D wariant " + Wariant + ": " + opis + ", siatek " + siatek + "; płaski szyld wyłączony: " + (zdjete.Count > 0 ? string.Join(", ", zdjete) : "brak");
        Debug.Log("BRAXHORN_GYM_SZYLD_3D " + opis);
        return opis;
    }

    // A: skrzynka 3D w miejscu płaskiego szyldu — lico grafit, boki stal, litery 7 cm na licu, listwa LED pod literami
    static string Tablica(float L, Material zielone, Material boki, Material grafit, Material stal, Material led)
    {
        const float cap = .52f, gl = .14f, track = .05f;
        float w = BraxLiteryBebas.Szerokosc(Tekst, cap, track);
        float cx = plaski.center.x, cy = plaski.center.y;
        float x0 = cx - w * .5f - .32f, x1 = cx + w * .5f + .32f, y0 = cy - (cap + .5f) * .5f, y1 = cy + (cap + .5f) * .5f, zf = L + gl;
        // skrzynka bez tylnej ściany (stoi na murze — zasada anty-migotania)
        BraxUliceKit.S(grafit).Quad(new Vector3(x0, y0, zf), new Vector3(x1, y0, zf), new Vector3(x1, y1, zf), new Vector3(x0, y1, zf), Vector3.forward);
        var s = BraxUliceKit.S(stal);
        s.Quad(new Vector3(x0, y1, L), new Vector3(x1, y1, L), new Vector3(x1, y1, zf), new Vector3(x0, y1, zf), Vector3.up);
        s.Quad(new Vector3(x0, y0, L), new Vector3(x1, y0, L), new Vector3(x1, y0, zf), new Vector3(x0, y0, zf), Vector3.down);
        s.Quad(new Vector3(x0, y0, L), new Vector3(x0, y1, L), new Vector3(x0, y1, zf), new Vector3(x0, y0, zf), Vector3.left);
        s.Quad(new Vector3(x1, y0, L), new Vector3(x1, y1, L), new Vector3(x1, y1, zf), new Vector3(x1, y0, zf), Vector3.right);
        float yb = cy - cap * .5f + .05f;
        BraxLiteryBebas.Napis(Tekst, new Vector3(cx, yb, zf), Vector3.forward, cap, .07f, zielone, boki, track, .004f);
        // listwa LED pod literami: profil 12 × 12 mm na całą szerokość napisu
        BraxUliceKit.S(led).Box(new Vector3(cx - w * .5f, yb - .112f, zf), new Vector3(cx + w * .5f, yb - .1f, zf + .012f), 16);
        return "tablica 3D " + (x1 - x0).ToString("0.00") + "×" + (y1 - y0).ToString("0.00") + "×" + gl.ToString("0.00") + " m (grafit, boki stal) w miejscu płaskiego szyldu x " + x0.ToString("0.00") + "…" + x1.ToString("0.00") + ", y " + y0.ToString("0.00") + "…" + y1.ToString("0.00") +
               ", litery Bebas Neue wersalik " + cap.ToString("0.00") + " m, szer. " + w.ToString("0.00") + " m, grubość 0.07 m, listwa LED";
    }

    // B: daszek nad drzwiami — płyta stalowa na dwóch cięgnach, litery na czole, downlighty nad wejściem
    static string Daszek(float L, Material zielone, Material boki, Material grafit, Material stal, Material lampa)
    {
        var d = Drzwi(L);
        const float cap = .34f, track = .05f, glDaszka = 1.4f, czolo = .56f;
        float dx = d.center.x, ys = d.max.y + .28f, yt = ys + czolo, x0 = dx - 2.1f, x1 = dx + 2.1f, zf = L + glDaszka;
        // drobne części oryginału nad drzwiami, które weszłyby w daszek (Gym_Mesh_152: płytka 0.32 m na y 4.0, 0.46 m od muru)
        var bryla = new Bounds(); bryla.SetMinMax(new Vector3(x0, ys - .05f, L - .05f), new Vector3(x1, yt + .2f, zf + .05f));
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (r && r.enabled && r.name.StartsWith("Gym_Mesh_", System.StringComparison.Ordinal) && r.bounds.size.x < 1f && r.bounds.size.y < 1f && bryla.Intersects(r.bounds) && r.bounds.max.z > L + .05f)
            { r.enabled = false; zdjete.Add(r.name + " (w obrysie daszka)"); }
        var gr = BraxUliceKit.S(grafit);
        gr.Quad(new Vector3(x0, yt, L), new Vector3(x1, yt, L), new Vector3(x1, yt, zf), new Vector3(x0, yt, zf), Vector3.up);       // wierzch
        gr.Quad(new Vector3(x0, ys, zf), new Vector3(x1, ys, zf), new Vector3(x1, yt, zf), new Vector3(x0, yt, zf), Vector3.forward); // czoło
        gr.Quad(new Vector3(x0, ys, L), new Vector3(x0, yt, L), new Vector3(x0, yt, zf), new Vector3(x0, ys, zf), Vector3.left);
        gr.Quad(new Vector3(x1, ys, L), new Vector3(x1, yt, L), new Vector3(x1, yt, zf), new Vector3(x1, ys, zf), Vector3.right);
        BraxUliceKit.S(stal).Quad(new Vector3(x0, ys, L), new Vector3(x1, ys, L), new Vector3(x1, ys, zf), new Vector3(x0, ys, zf), Vector3.down);  // podsufitka
        float w = BraxLiteryBebas.Napis(Tekst, new Vector3(dx, ys + (czolo - cap) * .5f, zf), Vector3.forward, cap, .05f, zielone, boki, track, .004f);
        var s = BraxUliceKit.S(stal);
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = dx + sx * 1.85f;
            s.Rura(new Vector3(x, yt + 1.3f, L + .03f), new Vector3(x, yt - .02f, zf - .1f), .02f, 10);
            s.Box(new Vector3(x - .08f, yt + 1.22f, L), new Vector3(x + .08f, yt + 1.38f, L + .03f), 16);   // kotwa na murze
        }
        var lp = BraxUliceKit.S(lampa);
        foreach (float ox in new[] { -1.2f, 0f, 1.2f })
        {
            var c = new Vector3(dx + ox, ys - .004f, L + glDaszka * .5f);
            for (int k = 0; k < 20; k++)
            {
                float a0 = k * Mathf.PI * 2f / 20, a1 = (k + 1) * Mathf.PI * 2f / 20;
                lp.Tri(c, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * .07f, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * .07f, Vector3.down);
            }
        }
        return "daszek 4.2×1.4 m nad drzwiami (x " + d.min.x.ToString("0.00") + "…" + d.max.x.ToString("0.00") + ", góra drzwi " + d.max.y.ToString("0.00") + " m), podsufitka " + ys.ToString("0.00") + " m, czoło " + czolo.ToString("0.00") +
               " m, litery Bebas Neue wersalik " + cap.ToString("0.00") + " m, szer. " + w.ToString("0.00") + " m, 2 cięgna, 3 downlighty";
    }
}
