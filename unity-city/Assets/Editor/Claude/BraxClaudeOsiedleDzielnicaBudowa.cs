using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// DZIELNICA ZACHODNIA — narzędzia budynków z wnętrzami (komisariat, szpital, bank): ściany zewnętrzne z otworami (okna z ramą,
// szybą przezroczystą i parapetami, drzwi), ściany działowe z drzwiami (biblioteka usług UScianyZDanych), drzwi prawdziwe
// (BraxCityDoor, skrzydła z własnymi siatkami), schody z kolizją pochyłą, stropy, sufity z lampami. Wszystko × k.
public static partial class BraxClaudeOsiedle
{
    // otwór w ścianie zewnętrznej: o0..o1 wzdłuż ściany, y0..y1; rodzaj: 0 okno (szyba przezroczysta), 1 drzwi (pusty otwór
    // pod drzwi prawdziwe), 2 okno wysokie z kratą (areszt), 3 okno matowe (WC)
    public struct DzlOtwor { public float o0, o1, y0, y1; public int rodzaj; public DzlOtwor(float a, float b, float c, float d, int r) { o0 = a; o1 = b; y0 = c; y1 = d; rodzaj = r; } }

    static Material DzlSzybaOkna => Plain("dzl szyba okienna przezroczysta", new Color(.66f, .74f, .78f, .22f), .92f, .1f).With(m => DzlPrzezroczysty(m));
    static Material DzlSzybaMatowa => Plain("dzl szyba matowa (WC)", new Color(.86f, .88f, .9f, .82f), .5f).With(m => DzlPrzezroczysty(m));
    static Material DzlRamaOkna => Plain("dzl rama okna aluminium antracyt", new Color(.09f, .095f, .1f), .45f, .55f);
    static Material DzlParapet => Plain("dzl parapet konglomerat jasny", new Color(.82f, .81f, .78f), .35f);
    static Material DzlKrata => Plain("dzl krata stal malowana", new Color(.16f, .17f, .18f), .4f, .7f);

    static Material DzlPrzezroczysty(Material m)
    {
        if (m.renderQueue >= 3000) return m;
        m.SetFloat("_Mode", 3); m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(m); return m;
    }

    // Ściana zewnętrzna wzdłuż x (stałe z = c) lub wzdłuż z (stałe x = c), od a0 do a1, y0..y1, grubość t; strona zewnętrzna
    // zew = +1 (na +z / +x) albo −1. Rdzeń z materiałem wnętrza (lico wewnętrzne), okładzina zewnętrzna 3 cm (osobny materiał,
    // nad cokołem); otwory z ościeżami (boki segmentów), ramą, szybą i parapetami.
    static void DzlScianaZew(bool wzdluzX, float c, float a0, float a1, float y0, float y1, float t, int zew, Material wew, Material elew, Material cokol, float hCokol, List<DzlOtwor> otw, string grupaWew)
    {
        otw = (otw ?? new List<DzlOtwor>()).OrderBy(o => o.o0).ToList();
        string gZew = group;
        void Prost(Material m, float b0, float b1, float yy0, float yy1, float n0, float n1, bool solid)
        { if (b1 - b0 < .004f || yy1 - yy0 < .004f) return; if (wzdluzX) BoxMinMax(m, b0, b1, yy0, yy1, Mathf.Min(n0, n1), Mathf.Max(n0, n1), solid); else BoxMinMax(m, Mathf.Min(n0, n1), Mathf.Max(n0, n1), yy0, yy1, b0, b1, solid); }
        float wIn = c - zew * t * .5f, wOut = c + zew * t * .5f;
        void Seg(float b0, float b1, float yy0, float yy1)
        {
            if (b1 - b0 < .004f || yy1 - yy0 < .004f) return;
            group = grupaWew; Prost(wew, b0, b1, yy0, yy1, wIn, wOut - zew * .03f, true);
            group = gZew;
            if (yy0 < y0 + hCokol && cokol) Prost(cokol, b0, b1, yy0, Mathf.Min(yy1, y0 + hCokol), wOut - zew * .03f, wOut + zew * .02f, false);
            if (yy1 > y0 + hCokol) Prost(elew, b0, b1, Mathf.Max(yy0, y0 + hCokol), yy1, wOut - zew * .03f, wOut, false);
        }
        float cur = a0;
        foreach (var o in otw)
        {
            Seg(cur, o.o0, y0, y1);
            Seg(o.o0, o.o1, o.y1, y1);
            if (o.rodzaj != 1) Seg(o.o0, o.o1, y0, o.y0);
            cur = o.o1;
            group = gZew;
            float fz = c + zew * (t * .5f - .12f);   // rama w głębi ściany (12 cm od lica)
            if (o.rodzaj == 1) continue;
            // rama, szyba, słupek środkowy w szerokich oknach, parapety
            Prost(DzlRamaOkna, o.o0, o.o1, o.y0, o.y0 + .06f, fz - .035f, fz + .035f, false);
            Prost(DzlRamaOkna, o.o0, o.o1, o.y1 - .06f, o.y1, fz - .035f, fz + .035f, false);
            Prost(DzlRamaOkna, o.o0, o.o0 + .06f, o.y0, o.y1, fz - .035f, fz + .035f, false);
            Prost(DzlRamaOkna, o.o1 - .06f, o.o1, o.y0, o.y1, fz - .035f, fz + .035f, false);
            float w = o.o1 - o.o0;
            if (w > 1.9f) Prost(DzlRamaOkna, (o.o0 + o.o1) * .5f - .04f, (o.o0 + o.o1) * .5f + .04f, o.y0, o.y1, fz - .035f, fz + .035f, false);
            if (o.y1 - o.y0 > 2.2f && o.rodzaj == 0) Prost(DzlRamaOkna, o.o0, o.o1, o.y1 - .55f * K, o.y1 - .55f * K + .06f, fz - .035f, fz + .035f, false);   // naświetle
            Prost(o.rodzaj == 3 ? DzlSzybaMatowa : DzlSzybaOkna, o.o0 + .06f, o.o1 - .06f, o.y0 + .06f, o.y1 - .06f, fz - .006f, fz + .006f, false);
            if (wzdluzX) Collider(new Vector3((o.o0 + o.o1) * .5f, (o.y0 + o.y1) * .5f, fz), new Vector3(w, o.y1 - o.y0, .08f));
            else Collider(new Vector3(fz, (o.y0 + o.y1) * .5f, (o.o0 + o.o1) * .5f), new Vector3(.08f, o.y1 - o.y0, w));
            Prost(DzlParapet, o.o0 - .04f, o.o1 + .04f, o.y0 - .04f, o.y0, wOut + zew * .06f, fz + zew * .035f, false);                    // parapet zewnętrzny (blacha / konglomerat)
            group = grupaWew;
            Prost(DzlParapet, o.o0 - .05f, o.o1 + .05f, o.y0 - .03f, o.y0, wIn - zew * .2f, fz - zew * .035f, false);                     // parapet wewnętrzny
            group = gZew;
            if (o.rodzaj == 2)   // krata stalowa od zewnątrz (pręty co 12 cm, 2 poprzeczki)
            {
                float kz = wOut + zew * .05f;
                for (float b = o.o0 + .1f; b < o.o1 - .05f; b += .12f) Prost(DzlKrata, b - .012f, b + .012f, o.y0, o.y1, kz - .012f, kz + .012f, false);
                foreach (float yy in new[] { o.y0 + .05f, o.y1 - .08f }) Prost(DzlKrata, o.o0, o.o1, yy, yy + .03f, kz - .02f, kz + .02f, false);
            }
        }
        Seg(cur, a1, y0, y1);
        group = gZew;
    }

    // ---------------- drzwi prawdziwe (BraxCityDoor) ----------------
    // Skrzydło budowane w osobnej grupie (siatka własna), po DzlFlush przypinane do zawiasu; kolizja skrzydła na zawiasie.
    sealed class DzlDrzwiRek { public string grupa; public Transform zawias; }
    static readonly List<DzlDrzwiRek> dzlDrzwi = new List<DzlDrzwiRek>();
    static int dzlDrzwiN;
    // c = środek otworu na posadzce w osi ściany, wzdluzX = ściana wzdłuż x; szer = szerokość skrzydła; szklane = drzwi
    // wejściowe z szybą; zawiasPlus = zawias po stronie +a; zostaw zamknięte bez komponentu (zbrojownia) — `zamkniete`.
    static void DzlDrzwi(string id, string etykieta, Vector3 c, bool wzdluzX, float szer, float wys, bool szklane, bool zawiasPlus, bool zamkniete = false, Material skrzydloM = null)
    {
        var a = wzdluzX ? Vector3.right : Vector3.forward; var n = wzdluzX ? Vector3.forward : Vector3.right;
        string g0 = group; string gr = "Dzielnica / drzwi / " + id + " / skrzydło";
        var holder = new GameObject("Drzwi " + etykieta + " (" + id + ")").transform; holder.SetParent(Group("Dzielnica / drzwi"), false);
        holder.position = c; holder.rotation = Quaternion.LookRotation(n, Vector3.up);
        var zaw = new GameObject("zawias skrzydła").transform; zaw.SetParent(holder, false);
        zaw.position = c + a * (zawiasPlus ? szer * .5f : -szer * .5f);
        // skrzydło (świat): od zawiasu wzdłuż ±a
        group = gr;
        var s = zawiasPlus ? -1f : 1f; var p0 = zaw.position; var mid = p0 + a * s * szer * .5f;
        var rama = skrzydloM ?? (szklane ? DzlRamaOkna : Plain("dzl skrzydło drzwi (płyta laminowana jasny dąb)", new Color(.72f, .60f, .45f), .35f));
        void B3(Material m, Vector3 cc, float la, float h, float g) => OBoxQ(m, cc, new Vector3(la, h, g), Quaternion.LookRotation(n, Vector3.up), false);
        if (szklane)
        {
            B3(rama, mid + Vector3.up * (wys - .05f), szer, .1f, .06f); B3(rama, mid + Vector3.up * .06f, szer, .12f, .06f);
            B3(rama, p0 + a * s * .04f + Vector3.up * wys * .5f, .08f, wys, .06f); B3(rama, p0 + a * s * (szer - .04f) + Vector3.up * wys * .5f, .08f, wys, .06f);
            B3(DzlSzybaOkna, mid + Vector3.up * wys * .5f, szer - .16f, wys - .2f, .02f);
            foreach (float sd in new[] { -.05f, .05f }) OBoxQ(UInox, p0 + a * s * (szer - .22f) + Vector3.up * 1.05f * K + n * sd, new Vector3(.03f, .9f * K, .03f), Quaternion.identity);
        }
        else
        {
            B3(rama, mid + Vector3.up * wys * .5f, szer, wys, .045f);
            foreach (float sd in new[] { -.04f, .04f }) OBoxQ(UInox, p0 + a * s * (szer - .12f) + Vector3.up * 1.05f + n * sd, new Vector3(.14f, .025f, .025f), Quaternion.LookRotation(n, Vector3.up));
        }
        group = g0;
        var col = zaw.gameObject.AddComponent<BoxCollider>();
        col.center = zaw.InverseTransformPoint(mid + Vector3.up * wys * .5f); col.size = new Vector3(szer, wys, .1f);
        dzlDrzwi.Add(new DzlDrzwiRek { grupa = gr, zawias = zaw });
        if (zamkniete) return;
        var door = holder.gameObject.AddComponent<BraxCityDoor>();
        door.id = "DZL_" + id; door.label = etykieta; door.leaves = new[] { zaw }; door.sides = new[] { zawiasPlus ? -1f : 1f };
        dzlDrzwiN++;
    }
    // po DzlFlush: siatki skrzydeł do zawiasów (obrót zawiasu obraca skrzydło)
    static void DzlPrzypnijDrzwi()
    {
        foreach (var d in dzlDrzwi)
        {
            if (!groups.TryGetValue(d.grupa, out var g) || !d.zawias) continue;
            g.SetParent(d.zawias, true);
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }
        foreach (var t in groups.Values.Where(t => t && t.name.StartsWith("Dzielnica / drzwi", StringComparison.Ordinal))) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
    }

    // ---------------- schody dwubiegowe (klatka) ----------------
    // Bieg 1 od (x0..xm, z0) w stronę +z do spocznika, bieg 2 z powrotem (xm..x1) do z0 na górę; stopnie × k, policzki, poręcze,
    // kolizja pochyła (prostopadłościan obrócony) — postać wchodzi płynnie.
    static void DzlSchodyU(float xBieg1a, float xBieg1b, float xBieg2a, float xBieg2b, float zStart, float zSpocznik, float zSpocznikKon, float fy0, float fy1, Material stopien, Material okladzina)
    {
        int n = 10; float h = (fy1 - fy0) * .5f, rise = h / n, run = (zSpocznik - zStart) / n;
        // bieg 1: w górę w stronę +z
        for (int i = 0; i < n; i++)
        {
            float y = fy0 + rise * (i + 1), z0 = zStart + run * i;
            BoxMinMax(stopien, xBieg1a, xBieg1b, fy0, y, z0, z0 + run, false);
            BoxMinMax(Plain("dzl nosek stopnia antypoślizgowy", new Color(.18f, .18f, .19f), .3f), xBieg1a, xBieg1b, y - .004f, y + .003f, z0 + run - .05f, z0 + run, false);
        }
        // spocznik
        float ys = fy0 + h;
        BoxMinMax(stopien, Mathf.Min(xBieg1a, xBieg2a), Mathf.Max(xBieg1b, xBieg2b), ys - .3f, ys, zSpocznik, zSpocznikKon, true);
        // bieg 2: z spocznika w stronę −z (w górę)
        for (int i = 0; i < n; i++)
        {
            float y = ys + rise * (i + 1), z1 = zSpocznik - run * i;
            BoxMinMax(stopien, xBieg2a, xBieg2b, y - .3f, y, z1 - run, z1, false);
            BoxMinMax(Plain("dzl nosek stopnia antypoślizgowy", new Color(.18f, .18f, .19f), .3f), xBieg2a, xBieg2b, y - .004f, y + .003f, z1 - run, z1 - run + .05f, false);
        }
        // kolizje pochyłe
        void Rampa(float xa, float xb, float za, float zb, float ya, float yb)
        {
            var g = new GameObject("schody kolizja"); g.transform.SetParent(colliders, false);
            var mid = new Vector3((xa + xb) * .5f, (ya + yb) * .5f - .1f, (za + zb) * .5f); g.transform.position = mid;
            float len = Mathf.Sqrt((zb - za) * (zb - za) + (yb - ya) * (yb - ya)); float ang = Mathf.Atan2(yb - ya, zb - za) * Mathf.Rad2Deg;
            g.transform.rotation = Quaternion.Euler(-ang, 0, 0);
            g.AddComponent<BoxCollider>().size = new Vector3(xb - xa, .2f, len);
        }
        Rampa(xBieg1a, xBieg1b, zStart, zSpocznik, fy0, ys);
        Rampa(xBieg2a, xBieg2b, zSpocznik, zStart, ys, fy1);
        // ścianka między biegami (policzek) i poręcze
        float xm = (xBieg1b + xBieg2a) * .5f;
        BoxMinMax(okladzina, xm - .06f, xm + .06f, fy0, fy1 + .1f, zStart + run, zSpocznik, true);
        var por = Plain("poręcz buk lakier", new Color(.72f, .55f, .36f), .55f);
        Tube(por, new Vector3(xm + .1f, fy0 + .95f * K, zStart), new Vector3(xm + .1f, ys + .95f * K, zSpocznik), .025f, 8);
        Tube(por, new Vector3(xm - .1f, ys + .95f * K, zSpocznik), new Vector3(xm - .1f, fy1 + .95f * K, zStart), .025f, 8);
    }

    // balustrada (stal + pochwyt), wysokość 1.1 m × k, od a do b, z kolizją
    static void DzlBalustrada(Vector3 a, Vector3 b, float y)
    {
        var d = b - a; float L = d.magnitude; if (L < .2f) return; d /= L; float h = 1.1f * K;
        int n = Mathf.Max(1, Mathf.RoundToInt(L / 1.2f));
        for (int i = 0; i <= n; i++) { var p = a + d * (L * i / n); Cylinder(UInox, new Vector3(p.x, y, p.z), .025f, h, 8, true, false); }
        Tube(UInox, new Vector3(a.x, y + h, a.z), new Vector3(b.x, y + h, b.z), .03f, 8);
        Tube(UInox, new Vector3(a.x, y + h * .5f, a.z), new Vector3(b.x, y + h * .5f, b.z), .012f, 6);
        var g = new GameObject("balustrada kolizja"); g.transform.SetParent(colliders, false); g.transform.position = (a + b) * .5f + Vector3.up * (y + h * .5f);
        g.transform.rotation = Quaternion.LookRotation(d, Vector3.up); g.AddComponent<BoxCollider>().size = new Vector3(.08f, h, L);
    }

    // posadzka + strop + sufit podwieszany z lampami (pomieszczenie x0..x1, z0..z1, poziom fy, sufit cy)
    static void DzlPokoj(Material posadzka, float x0, float x1, float z0, float z1, float fy, float cy, int swiatla = 1, float zasieg = 7f)
    {
        Top(posadzka, x0, x1, z0, z1, fy);
        BoxMinMax(Plain("dzl sufit podwieszany biały", new Color(.93f, .93f, .91f), .1f), x0, x1, cy, cy + .02f, z0, z1, false, true);
        ULampySufit(x0, x1, z0, z1, cy, 2.4f, swiatla, zasieg, .9f);
    }
}

static class DzlRozszerzenia
{
    public static Material With(this Material m, Action<Material> f) { if (m) f(m); return m; }
}
