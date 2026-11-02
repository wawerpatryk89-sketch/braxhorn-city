using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ULICA GŁÓWNA — WYPOSAŻENIE I REKLAMY (wykonawca A, 2.11; właściciel z48–z49: „LAMPY i ZNAKI mają stać odpowiednio i być
// prawdziwe”, „BILLBOARDY PREMIUM w odpowiednich miejscach”; audyt §1: „na ulicy nie ma drzew w kratach, słupków, ławek ani
// stojaków”). Kolejność jak w budownictwie (właściciel 16:04): nawierzchnia → budynki → WYPOSAŻENIE (ten krok, po pierzejach
// i lokalach) → ludzie. Wysokość każdej rzeczy z nawierzchni pod spodem (promień w dół), nic nie wisi i nic nie zakopane.
//  • drzewa w kratach żeliwnych (krata wpuszczona: chodnik pod nią wycina kontrola migotania, pod kratą ściółka 8 cm niżej),
//    ławki miejskie × k z oparciem i podłokietnikami (siedziska BraxSeat), kosze na śmieci z daszkiem, stojaki rowerowe,
//    słupki przy przejściu, hydranty (Poly Haven CC0), tablice nazw ulic w polskim standardzie (niebieska tablica, biały
//    napis, na słupie przy krawężniku);
//  • REKLAMY: stare tablice „BRAXHORN original artwork billboard *” (płaskie płyty 0.6 m przed oknami nowych elewacji)
//    ZDJĘTE; nowe: ekrany LED w ramach na ścianach wieżowców nad ulicą (bez okien za ekranem — ekran ma własną bryłę bez
//    tylnej ściany, stoi przy elewacji wieży), widoczne w osi ulicy z obu końców; treść — grafiki marki z repo (bez cudzych).
// Wolne strefy: drzwi lokali, wejście NOVA z kolejką, brama podwórka, przejście z sygnalizacją (x 49…55), wiadukt, metro.
internal static class BraxMiastoUlica
{
    const float Chodnik = .15f, KrawPld = 23.755f, KrawPln = 33.245f;
    static float K => BraxMiastoA.K;
    static Transform root, kol;

    static bool Wolne(float x, bool polnoc)
    {
        if (x > 48.5f && x < 55.5f) return false;                 // przejście z sygnalizacją
        if (x > -73f && x < -59f) return false;                   // wiadukt
        if (x > 64.5f || x < -102f) return false;                 // końce ulicy (metro, wylot)
        if (polnoc && x > 1.5f && x < 18.5f) return false;        // brama podwórka
        if (polnoc && x > 32.5f && x < 45.5f) return false;       // NOVA: kolejka i wejście
        if (!polnoc && x > 18.8f && x < 23.2f) return false;      // Nutrition: drzwi
        if (!polnoc && x > 2.5f && x < 9.5f) return false;        // siłownia: wyjście
        foreach (float l in new[] { -98f, -82f, -66f, -50f, -34f, -2f, 14f, 30f, 46f, 62f }) if (Mathf.Abs(x - l) < 1.4f) return false;   // latarnie
        return true;
    }
    static float Grunt(float x, float z)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(x, 6f, z), Vector3.down, 9f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            if (h.normal.y > .8f && h.point.y < 1f && h.point.y > -.5f) return h.point.y;
        return Chodnik;
    }

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxMiastoA.WyczyscPamiec();
        // 1) stare reklamy — zdjęte
        int stare = 0;
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("BRAXHORN original artwork billboard")).ToArray())
            if (t) { BraxUliceKit.Usun(t.gameObject, "stara tablica reklamowa przed oknami elewacji"); stare++; }
        root = BraxRegion.Root(cityRoot, "CLAUDE A: ulica główna — wyposażenie i reklamy");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        Physics.SyncTransforms();
        BraxUliceKit.UstawGrupe("Ulica: wyposażenie chodników", 70f);
        int drzew = 0, lawek = 0, koszy = 0;
        foreach (bool polnoc in new[] { false, true })
        {
            float zl = polnoc ? KrawPln + .95f : KrawPld - .95f;   // linia mebli 0.95 m od krawężnika
            foreach (float x in new[] { -90f, -74f, -42f, -26f, -10f, 6f, 22f, 38f, 58f })
            {
                if (!Wolne(x, polnoc)) continue;
                if (Drzewo(cityRoot, new Vector3(x, Grunt(x, zl), zl))) drzew++;
                float xb = x + 2.6f;
                if (Wolne(xb, polnoc) && Wolne(xb + 1.2f, polnoc)) { Lawka(new Vector3(xb + .6f, Grunt(xb + .6f, zl), zl), polnoc); lawek++; }
                float xk = x - 2.1f;
                if (Wolne(xk, polnoc)) { Kosz(new Vector3(xk, Grunt(xk, zl), zl)); koszy++; }
            }
            // stojaki rowerowe przy piekarni / delikatesach
            float xs = polnoc ? 24.5f : 30.5f;
            if (Wolne(xs, polnoc)) for (int i = 0; i < 3; i++) Stojak(new Vector3(xs + i * .9f, Grunt(xs, zl), zl));
            // słupki przy przejściu (narożniki, 0.5 m od krawężnika)
            foreach (float x in new[] { 48.2f, 55.8f }) foreach (float d in new[] { .5f, 1.4f })
                { var p = new Vector3(x, 0, polnoc ? KrawPln + d : KrawPld - d); p.y = Grunt(p.x, p.z); Slupek(p); }
            // hydrant przy krawężniku
            var hp = new Vector3(polnoc ? -20f : -30f, 0, polnoc ? KrawPln + .55f : KrawPld - .55f); hp.y = Grunt(hp.x, hp.z);
            var hm = BraxRegion.Model("fire_hydrant"); if (hm) BraxRegion.Put(root, hm, "Hydrant", hp, polnoc ? 180f : 0f, 1f, true, .85f * K);
        }
        // tablice nazw ulic (polski standard) przy końcach i przy przejściu
        foreach (var (p, polnoc, tekst) in new[] { (new Vector3(63.2f, 0, KrawPln + .45f), true, "ul. PORTOWA"), (new Vector3(63.2f, 0, KrawPld - .45f), false, "BULWAR NADMORSKI"), (new Vector3(-100.5f, 0, KrawPld - .45f), false, "ul. PORTOWA"), (new Vector3(10.5f, 0, KrawPld - .45f), false, "ul. PORTOWA") })
        { var q = p; q.y = Grunt(q.x, q.z); TablicaUlicy(q, polnoc, tekst); }
        BraxUliceKit.UstawGrupe("Ulica: ekrany LED na wieżowcach");
        int ekr = Ekrany(cityRoot);
        int siatek = BraxUliceKit.Zapisz(root, "ulica_wyposazenie");
        BraxMiastoA.Log("ULICA: drzew w kratach " + drzew + ", ławek " + lawek + ", koszy " + koszy + ", ekranów LED " + ekr + ", siatek " + siatek + "; zdjęte stare tablice reklamowe: " + stare);
    }

    static bool Drzewo(Transform cityRoot, Vector3 p)
    {
        // chodnik pod kratą ZDJĘTY (krata i ściółka w jego miejscu, nie na nim)
        { float a0 = .75f * K; BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(p.x - a0, p.x + a0, p.y - .12f, p.y + .04f, p.z - a0, p.z + a0), "krata_drzewa", root); }
        // krata żeliwna 1.5 × 1.5 m × k (pierścienie promieniste), wierzch 1 cm nad chodnikiem — chodnik pod nią wycina kontrola migotania
        float a = .75f * K; var zel = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: krata drzewa (żeliwo)", new Color(.12f, .12f, .13f), .55f, .6f));
        zel.Box(new Vector3(p.x - a, p.y - .04f, p.z - a), new Vector3(p.x + a, p.y + .01f, p.z - a + .08f)); zel.Box(new Vector3(p.x - a, p.y - .04f, p.z + a - .08f), new Vector3(p.x + a, p.y + .01f, p.z + a));
        zel.Box(new Vector3(p.x - a, p.y - .04f, p.z - a + .08f), new Vector3(p.x - a + .08f, p.y + .01f, p.z + a - .08f)); zel.Box(new Vector3(p.x + a - .08f, p.y - .04f, p.z - a + .08f), new Vector3(p.x + a, p.y + .01f, p.z + a - .08f));
        for (int i = 0; i < 4; i++) { float r = .25f + i * .14f; zel.Pierscien(new Vector3(p.x, p.y + .01f, p.z), r, r + .035f, 0, 360, 28); }
        for (int i = 0; i < 12; i++) { float an = i * 30f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)); zel.Box(new Vector3(p.x, p.y - .03f, p.z) + d * .2f - Vector3.Cross(Vector3.up, d) * .015f, new Vector3(p.x, p.y + .008f, p.z) + d * (a - .06f) + Vector3.Cross(Vector3.up, d) * .015f); }
        // ściółka pod kratą (8 cm niżej niż chodnik — widać ją przez szczeliny)
        BraxUliceKit.S(BraxMiastoA.Mat("Ulica: ściółka (kora)", new Color(.22f, .15f, .1f), .95f)).Quad(new Vector3(p.x - a, p.y - .08f, p.z - a), new Vector3(p.x + a, p.y - .08f, p.z - a), new Vector3(p.x + a, p.y - .08f, p.z + a), new Vector3(p.x - a, p.y - .08f, p.z + a), Vector3.up);
        BraxMiastoA.Kol(kol, "krata drzewa", new Vector3(p.x - a, p.y - .3f, p.z - a), new Vector3(p.x + a, p.y, p.z + a));
        var m = BraxRegion.Model("drzewo_verdant");
        if (!m) return false;
        var t = BraxRegion.Put(root, m, "Drzewo w kracie (ulica)", p - Vector3.up * .08f, (p.x * 37f) % 360f, 1f, false, 6.0f * K);
        var c = new GameObject("kolizja: pień drzewa"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * 1.2f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = .2f; cc.height = 2.4f;
        return t;
    }

    static void Lawka(Vector3 p, bool polnoc)
    {
        // ławka miejska 1.8 m × k: rama żeliwna, siedzisko i oparcie z listew, podłokietniki; przodem do budynków (plecami do jezdni)
        float L = 1.8f * K, hs = .45f * K, gl = .45f * K; var przod = polnoc ? Vector3.forward : Vector3.back; var bok = Vector3.right;
        var drewno = BraxUliceKit.S(BraxMiastoA.Tex("Ulica: ławka listwy (drewno)", 1.0f, new Color(.85f, .7f, .52f), new Color(.5f, .36f, .24f), .7f, "teak_veneer", "wood_planks"));
        var zel = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: rama ławki (żeliwo grafit)", new Color(.1f, .1f, .11f), .5f, .6f));
        for (int i = 0; i < 4; i++) drewno.OBox(p + Vector3.up * (hs - .02f) + przod * (gl * .5f - .06f - i * .11f), bok, Vector3.up, przod, L, .04f, .09f);
        for (int i = 0; i < 3; i++) { var q = Quaternion.AngleAxis(-15f, bok); drewno.OBox(p + Vector3.up * (hs + .18f + i * .13f) - przod * (gl * .5f + .02f + i * .03f), bok, q * Vector3.up, q * przod, L, .1f, .035f); }
        foreach (float s in new[] { -L * .5f + .08f, L * .5f - .08f })
        {
            var b = p + bok * s;
            zel.OBox(b + Vector3.up * hs * .5f + przod * (gl * .35f), bok, Vector3.up, przod, .06f, hs, .06f);
            zel.OBox(b + Vector3.up * ((hs + .5f) * .5f) - przod * (gl * .5f), bok, Vector3.up, przod, .06f, hs + .5f, .06f);   // noga tylna z oparciem
            zel.OBox(b + Vector3.up * (hs + .22f) + przod * .02f, bok, Vector3.up, przod, .06f, .05f, gl + .05f);   // podłokietnik
        }
        var c = new GameObject("kolizja: ławka"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * hs * .5f; c.AddComponent<BoxCollider>().size = new Vector3(L, hs, gl);
        BraxMiastoA.Siedzisko(root, "ŁAWKA", p + Vector3.up * hs, przod);
    }

    static void Kosz(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: kosz na śmieci (stal grafit)", new Color(.16f, .17f, .18f), .5f, .6f));
        float r = .24f * K, h = .9f * K;
        st.Walec(p, r, h - .1f, 16); st.Walec(p + Vector3.up * (h - .1f), r + .03f, .04f, 16);
        st.Walec(p + Vector3.up * (h - .06f), r - .02f, .06f, 16);   // daszek z otworem wrzutowym (czarny pierścień niżej)
        var c = new GameObject("kolizja: kosz"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * h * .5f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = r; cc.height = h;
    }

    static void Stojak(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: stal nierdzewna (stojaki, słupki)", new Color(.62f, .63f, .65f), .35f, .9f));
        float w = .7f * K, h = .8f * K;
        st.Rura(p + Vector3.left * w * .5f, p + Vector3.left * w * .5f + Vector3.up * (h - .2f), .025f, 8);
        st.Rura(p + Vector3.right * w * .5f, p + Vector3.right * w * .5f + Vector3.up * (h - .2f), .025f, 8);
        var prev = p + Vector3.left * w * .5f + Vector3.up * (h - .2f);
        for (int i = 1; i <= 8; i++) { float a = Mathf.PI * i / 8f; var q = p + new Vector3(-Mathf.Cos(a) * w * .5f, h - .2f + Mathf.Sin(a) * .2f, 0); st.Rura(prev, q, .025f, 8, false); prev = q; }
        var c = new GameObject("kolizja: stojak rowerowy"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * h * .5f; c.AddComponent<BoxCollider>().size = new Vector3(w, h, .08f);
    }

    static void Slupek(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: słupek (stal grafit)", new Color(.16f, .17f, .18f), .5f, .6f));
        st.Walec(p, .07f, .9f * K, 12); st.Walec(p + Vector3.up * .9f * K, .08f, .04f, 12);
        BraxUliceKit.S(BraxMiastoA.Mat("Ulica: pasek odblaskowy słupka", new Color(.92f, .92f, .9f), .3f)).Walec(p + Vector3.up * (.9f * K - .2f), .072f, .06f, 12);
        var c = new GameObject("kolizja: słupek"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * .5f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = .08f; cc.height = 1f;
    }

    static void TablicaUlicy(Vector3 p, bool polnoc, string tekst)
    {
        // słup ocynkowany 2.9 m × k, tablica niebieska 0.9 × 0.25 m × k z białą obwódką (pierścień) i napisem z liter (polski wzór)
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: stal ocynkowana (słupy znaków)", new Color(.65f, .66f, .67f), .45f, .85f));
        var nieb = BraxMiastoA.Mat("Ulica: tablica nazwy ulicy (niebieska)", new Color(.05f, .22f, .55f), .4f);
        var biel = BraxMiastoA.Mat("Ulica: tablica nazwy ulicy (biel)", new Color(.95f, .95f, .95f), .4f);
        float hs = 2.9f * K, w = Mathf.Max(.9f, BraxUliceKit.SzerokoscNapisu(tekst, .1f * K) + .25f), h = .25f * K;
        st.Walec(p, .045f, hs, 10); st.Walec(p + Vector3.up * hs, .05f, .03f, 10);
        var c = p + Vector3.up * (hs - .3f); var r = Vector3.right; var n = polnoc ? Vector3.back : Vector3.forward;
        // tablica dwustronna: obwódka biała (pierścień 2 cm), pole niebieskie w otworze, napis z liter po obu stronach
        BraxUliceKit.S(biel).OBox(c + Vector3.up * (h * .5f - .01f), r, Vector3.up, n, w, .02f, .02f); BraxUliceKit.S(biel).OBox(c - Vector3.up * (h * .5f - .01f), r, Vector3.up, n, w, .02f, .02f);
        BraxUliceKit.S(biel).OBox(c + r * (w * .5f - .01f), r, Vector3.up, n, .02f, h - .04f, .02f); BraxUliceKit.S(biel).OBox(c - r * (w * .5f - .01f), r, Vector3.up, n, .02f, h - .04f, .02f);
        BraxUliceKit.S(nieb).OBox(c, r, Vector3.up, n, w - .04f, h - .04f, .02f);
        foreach (var nn in new[] { n, -n }) BraxUliceKit.Napis3D(tekst, c + nn * .01f - Vector3.up * .05f * K, nn, .1f * K, .006f, biel, biel);
        foreach (float y in new[] { -h * .3f, h * .3f }) st.Box(p + Vector3.up * (hs - .3f + y) + new Vector3(-.06f, -.02f, -.04f), p + Vector3.up * (hs - .3f + y) + new Vector3(.06f, .02f, .04f));
        var k = new GameObject("kolizja: słup tablicy"); k.transform.SetParent(kol, false); k.transform.position = p + Vector3.up * 1.2f; var cc = k.AddComponent<CapsuleCollider>(); cc.radius = .06f; cc.height = 2.4f;
    }

    // ekrany LED na wieżowcach nad ulicą (bryła ekranu bez tylnej ściany, przy elewacji wieży; treść — grafiki marki, podświetlenie)
    static int Ekrany(Transform cityRoot)
    {
        int n = 0;
        var plany = new[] { ("corner_store", "brand-poster.png", false), ("restaurant", "brax-fighter.jpg", true), ("hotel", "nyxa-pink.jpg", true), ("gallery", "brand-poster.png", true) };   // ekran na ścianie wolnej od sąsiedniej wieży, w osi ulicy
        foreach (var (id, plik, naWschod) in plany)
        {
            var plan = BraxCityStreetSkyline.HighRises.FirstOrDefault(p => p.id == id); var b = BraxCityArchitecture.StreetBuildings.FirstOrDefault(x => x.id == id);
            if (plan == null || b == null) continue;
            var fp = BraxCityStreetSkyline.Footprint(b, plan); float w = Mathf.Min(fp.height - 1.2f, 8f), h = w * 1.5f, y0 = b.Height + 6f;   // ekran pionowy 2:3 (grafiki pionowe)
            // ekran na ścianie wieży skierowanej wzdłuż ulicy (wschód albo zachód) — widoczny w osi ulicy
            float xs = naWschod ? fp.xMax : fp.xMin; var N = naWschod ? Vector3.right : Vector3.left; float zc = fp.center.y;
            var rama = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: rama ekranu LED (aluminium czarne)", new Color(.05f, .05f, .055f), .45f, .6f));
            // rama-pierścień 0.35 m głębokości (bez tylnej ściany) i ekran jako lico w jej otworze
            var c = new Vector3(xs, y0 + h * .5f, zc) + N * .175f; var R = Vector3.Cross(Vector3.up, -N).normalized;
            rama.OBox(c + Vector3.up * (h * .5f + .1f), R, Vector3.up, N, w + .4f, .2f, .35f, true); rama.OBox(c - Vector3.up * (h * .5f + .1f), R, Vector3.up, N, w + .4f, .2f, .35f, true);
            rama.OBox(c + R * (w * .5f + .1f), R, Vector3.up, N, .2f, h, .35f, true); rama.OBox(c - R * (w * .5f + .1f), R, Vector3.up, N, .2f, h, .35f, true);
            var ld = new Vector3(xs, y0, zc) + N * .35f - R * w * .5f;
            BraxMiastoA.Obraz(root, "ekran LED " + id, "Assets/CityRebuild/Brand/" + plik, ld, R, w, h, N, 1.1f, .3f);
            // osłona boczna ekranu (blacha) między ekranem a elewacją wieży — ekran nie „wisi” przed szybą
            var bl = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: obudowa ekranu (blacha grafit)", new Color(.08f, .08f, .09f), .6f, .4f));
            foreach (float sg in new[] { -1f, 1f })
                bl.Quad(new Vector3(xs, y0, zc) + R * sg * (w * .5f + .2f) - Vector3.up * .2f, new Vector3(xs, y0, zc) + R * sg * (w * .5f + .2f) + N * .35f - Vector3.up * .2f, new Vector3(xs, y0 + h, zc) + R * sg * (w * .5f + .2f) + N * .35f + Vector3.up * .2f, new Vector3(xs, y0 + h, zc) + R * sg * (w * .5f + .2f) + Vector3.up * .2f, R * sg);
            n++;
        }
        return n;
    }
}
