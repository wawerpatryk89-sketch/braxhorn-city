using System.Collections.Generic;
using UnityEngine;

// SYGNALIZACJA ŚWIETLNA OSIEDLA A (build 14, wykonawca „drogi”; właściciel 19:53: „nie mamy nic z takich rzeczy”, wzór:
// zdjęcie Urban City). Skrzyżowania: Poznańska × B3 (wjazd z tunelu, 4 wloty) i Poznańska × B1 (T, 3 wloty).
// Na każdym wlocie, za zebrą: linia zatrzymania (P-14), ciągła linia osiowa 15 m (w UlicaOdcinki), strzałka „prosto”,
// słup ocynkowany z wysięgnikiem nad pasem: sygnalizator nad jezdnią (dolna krawędź ~4.8 m) + sygnalizator na słupie
// (dolna krawędź 2.2 m), komory 200 mm czerwona/żółta/zielona z daszkami, tarcza kontrastowa (czarna z białą obwódką).
// Przy każdym końcu każdej zebry sygnalizator dla pieszych (czerwony stojący / zielony idący ludzik) i przycisk.
// Szafa sterownika na narożniku. Logika faz: BraxSwiatloDrogowe (BraxTrafficCycle z miasta). Wymiary × K.
public static partial class BraxClaudeOsiedle
{
    static Material SlupSygn => Plain("słup sygnalizacji ocynk", new Color(.58f, .60f, .62f), .45f, .75f);
    static Material CzarnySygn => Plain("sygnalizator czarny lakier", new Color(.035f, .035f, .04f), .55f, .25f);
    static Material SzybaSygn => Plain("sygnalizator okno ciemne", new Color(.02f, .02f, .025f), .9f, .1f);
    static Material ZoltySygn => Plain("przycisk dla pieszych żółty", new Color(.92f, .72f, .08f), .5f, .1f);
    static Material SzafaSygn => Plain("szafa sterownika szara", new Color(.55f, .57f, .58f), .35f, .5f);
    static int glowicePojazdy, glowicePiesi, skrzyzowaniaSwiatla;

    sealed class SiatkiSygn { public Siatka czA = new Siatka(), zoA = new Siatka(), ziA = new Siatka(), czB = new Siatka(), zoB = new Siatka(), ziB = new Siatka(), pCz = new Siatka(), pZi = new Siatka(); }

    static void Sygnalizacja(WezelA w)
    {
        group = "Osiedle A / sygnalizacja";
        float k = Ks;
        var c = w.C; var sg = new SiatkiSygn();
        var stopS = new List<Vector3>(); var stopK = new List<Vector3>(); var stopO = new List<int>();
        var pS = new List<Vector3>(); var pK = new List<Vector3>();
        bool szafa = false;
        foreach (var r in Ramiona(w))
        {
            var u = r.u; float cw = u.cw; int os = u.alongX ? 0 : 1;
            float a0 = r.o.cw + r.o.kerb + .25f, a1 = r.o.HalfS(r.s);
            int sR = (int)Mathf.Sign(Vector3.Dot(r.R, u.Wpoprzek));
            // linia zatrzymania i strzałka „prosto” na pasie wlotu (prawa strona jadących do węzła)
            { var (x0, x1, z0, z1) = RectR(c, r, a1 + 1.0f, a1 + 1.4f, .1f, cw - .05f); Farba(x0, x1, z0, z1); }
            {
                float d = cw * .5f, t = a1 + 7f;   // grot przy t, ogon przy t + 5
                FarbaWielokat(new[] { PR(c, r, t + 1.2f, d - .09f, YFarba), PR(c, r, t + 5f, d - .09f, YFarba), PR(c, r, t + 5f, d + .09f, YFarba), PR(c, r, t + 1.2f, d + .09f, YFarba) });
                FarbaWielokat(new[] { PR(c, r, t, d, YFarba), PR(c, r, t + 1.2f, d + .3f, YFarba), PR(c, r, t + 1.2f, d - .3f, YFarba) });
            }
            // słup z wysięgnikiem po prawej stronie wlotu, tuż za linią zatrzymania
            float dp = cw + u.kerb + .45f, ap = a1 + 1.2f;
            var baza = PR(c, r, ap, dp);
            Slup(baza, 6.3f * k, .085f);
            var ramieKon = PR(c, r, ap, cw * .5f, 6.0f * k);
            Tube(SlupSygn, baza + Vector3.up * 6.0f * k, ramieKon, .055f, 10);
            Tube(SlupSygn, baza + Vector3.up * 5.25f * k, Vector3.Lerp(baza + Vector3.up * 6.0f * k, ramieKon, .45f), .03f, 8);   // zastrzał
            Cylinder(CzarnySygn, ramieKon - Vector3.up * .06f, .02f, .06f, 6, false, false);   // wieszak
            GlowicaPojazdy(ramieKon - Vector3.up * (.6f * k + .06f), r.dir, os, sg);
            // sygnalizator na słupie: obudowa i tarcza kontrastowa przed słupem (słup za tarczą, nic jej nie przecina)
            var bok = baza + r.dir * (.11f + .205f * k) + Vector3.up * 2.75f * k;
            OBox(SlupSygn, baza + r.dir * .1f + Vector3.up * 2.75f * k, new Vector3(.06f, .5f * k, .16f), Mathf.Atan2(r.dir.x, r.dir.z) * Mathf.Rad2Deg);   // wspornik
            GlowicaPojazdy(bok, r.dir, os, sg);
            stopS.Add(PR(c, r, a1 + 1.4f + 2.5f, cw * .5f, YAsf)); stopK.Add(-r.dir); stopO.Add(os);
            // sygnalizatory dla pieszych na obu końcach zebry tego wlotu (własne słupy tuż za zebrą, patrzą przez jezdnię)
            float am = (a0 + a1) * .5f;
            foreach (int sd in new[] { 1, -1 })
            {
                var f = r.R * -sd;   // lico do przeciwnego końca zebry
                int side = sd > 0 ? sR : -sR;
                var pole = PR(c, r, a1 + .35f, sd * (cw + u.kerb + Mathf.Max(.3f, Mathf.Min(.45f, u.GreenS(side) + .15f))));
                Slup(pole, 3.0f * k, .06f);
                GlowicaPiesi(pole + f * (.08f + .2f * k) + Vector3.up * 2.45f * k, f, sg);
                // przycisk na słupie od strony przejścia (−dir: do zebry)
                var bx = pole - r.dir * (.07f + .05f) + Vector3.up * 1.1f * k;
                OBox(ZoltySygn, bx, new Vector3(.12f * k, .2f * k, .1f * k), Mathf.Atan2(-r.dir.x, -r.dir.z) * Mathf.Rad2Deg);
                OBox(CzarnySygn, bx - r.dir * .051f * k + Vector3.up * .02f * k, new Vector3(.07f * k, .07f * k, .004f), Mathf.Atan2(-r.dir.x, -r.dir.z) * Mathf.Rad2Deg);
                pS.Add(PR(c, r, am, sd * (cw + .4f), .1f)); pK.Add(-r.R * sd);
            }
            // szafa sterownika przy pierwszym wlocie, na zewnętrznej krawędzi chodnika
            if (!szafa)
            {
                szafa = true;
                var sp = PR(c, r, ap + 2.6f, u.HalfS(sR) - .35f);
                if (!Physics.CheckBox(sp + Vector3.up * .7f, new Vector3(.45f, .6f, .45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    float yaw = Mathf.Atan2(r.dir.x, r.dir.z) * Mathf.Rad2Deg;
                    OBox(Concrete, sp + Vector3.up * .08f, new Vector3(.9f, .16f, .55f), yaw, true);
                    OBox(SzafaSygn, sp + Vector3.up * .76f, new Vector3(.8f, 1.2f, .42f), yaw, true);
                    OBox(SzafaSygn, sp + Vector3.up * 1.39f, new Vector3(.86f, .05f, .48f), yaw);
                    for (int q = 0; q < 6; q++) OBox(CzarnySygn, sp + Vector3.up * (.5f + q * .05f) - r.R * .212f, new Vector3(.4f, .015f, .006f), yaw);
                }
            }
        }
        // komponent: szkła (osobne siatki — materiał przełączany), strefy
        var go = new GameObject("Sygnalizacja świetlna (" + w.x + ", " + w.z + ")"); go.transform.SetParent(Group(group), false);
        var s = go.AddComponent<BraxSwiatloDrogowe>();
        Material czW = Szklo("czerwone", new Color(1f, .07f, .04f), true), czO = Szklo("czerwone", new Color(1f, .07f, .04f), false);
        Material zoW = Szklo("żółte", new Color(1f, .6f, .04f), true), zoO = Szklo("żółte", new Color(1f, .6f, .04f), false);
        Material ziW = Szklo("zielone", new Color(.1f, 1f, .45f), true), ziO = Szklo("zielone", new Color(.1f, 1f, .45f), false);
        s.czerwoneA = ObiektSiatki("szkła czerwone (oś x)", go.transform, sg.czA, czO); s.zolteA = ObiektSiatki("szkła żółte (oś x)", go.transform, sg.zoA, zoO); s.zieloneA = ObiektSiatki("szkła zielone (oś x)", go.transform, sg.ziA, ziO);
        s.czerwoneB = ObiektSiatki("szkła czerwone (oś z)", go.transform, sg.czB, czO); s.zolteB = ObiektSiatki("szkła żółte (oś z)", go.transform, sg.zoB, zoO); s.zieloneB = ObiektSiatki("szkła zielone (oś z)", go.transform, sg.ziB, ziO);
        s.pieszyCzerwony = ObiektSiatki("ludziki czerwone", go.transform, sg.pCz, czO); s.pieszyZielony = ObiektSiatki("ludziki zielone", go.transform, sg.pZi, ziO);
        s.czerwonyWl = czW; s.czerwonyWyl = czO; s.zoltyWl = zoW; s.zoltyWyl = zoO; s.zielonyWl = ziW; s.zielonyWyl = ziO;
        s.ludzikCzerwonyWl = czW; s.ludzikCzerwonyWyl = czO; s.ludzikZielonyWl = ziW; s.ludzikZielonyWyl = ziO;
        s.stopSrodek = stopS.ToArray(); s.stopKierunek = stopK.ToArray(); s.stopOs = stopO.ToArray();
        s.pieszySrodek = pS.ToArray(); s.pieszyKierunek = pK.ToArray();
        s.przesuniecie = skrzyzowaniaSwiatla * 20f;   // drugie skrzyżowanie przesunięte o pół cyklu
        skrzyzowaniaSwiatla++;
        rep.notes.Add("sygnalizacja (" + w.x + ", " + w.z + "): " + stopS.Count + " wloty, " + pS.Count + " sygnalizatorów dla pieszych, fazy BraxTrafficCycle (osie na zmianę)");
    }

    // Słup ocynkowany: stopa z kołnierzem i śrubami, trzon, zaślepka; kolizja.
    static void Slup(Vector3 p, float h, float r)
    {
        p.y = 0f;
        Cylinder(SlupSygn, p, r * 2.2f, .05f, 12, true, false);
        Cylinder(SlupSygn, p + Vector3.up * .05f, r * 1.35f, .35f, 12, true, false);
        Cylinder(SlupSygn, p + Vector3.up * .4f, r, h - .4f, 12, false, false);
        Cylinder(CzarnySygn, p + Vector3.up * h, r + .006f, .03f, 12, true, false);
        for (int q = 0; q < 4; q++) { var d = Quaternion.Euler(0, 45f + q * 90f, 0) * Vector3.forward * r * 1.75f; Cylinder(Steel, p + d + Vector3.up * .05f, .012f, .03f, 6, true, false); }
        Collider(p + Vector3.up * (h * .5f), new Vector3(r * 2.4f, h, r * 2.4f));
    }

    // Sygnalizator dla pojazdów (3 × 200 mm): c = środek lica obudowy, f = kierunek lica (do kierowców).
    static void GlowicaPojazdy(Vector3 c, Vector3 f, int os, SiatkiSygn sg)
    {
        float k = Ks; f.y = 0; f.Normalize();
        var R = Vector3.Cross(Vector3.up, f).normalized; float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        OBox(CzarnySygn, c - f * .1f * k, new Vector3(.32f * k, .98f * k, .2f * k), yaw);
        // tarcza kontrastowa: czarne pole + biała obwódka (bez nakładania)
        var tb = c - f * .205f * k; float tw = .44f * k, th = 1.1f * k, ob = .03f * k;
        OBox(CzarnySygn, tb, new Vector3(tw, th, .015f), yaw);
        OBox(ZnBialy, tb + R * (tw * .5f + ob * .5f), new Vector3(ob, th + 2f * ob, .015f), yaw); OBox(ZnBialy, tb - R * (tw * .5f + ob * .5f), new Vector3(ob, th + 2f * ob, .015f), yaw);
        OBox(ZnBialy, tb + Vector3.up * (th * .5f + ob * .5f), new Vector3(tw, ob, .015f), yaw); OBox(ZnBialy, tb - Vector3.up * (th * .5f + ob * .5f), new Vector3(tw, ob, .015f), yaw);
        Siatka[] sz = os == 0 ? new[] { sg.czA, sg.zoA, sg.ziA } : new[] { sg.czB, sg.zoB, sg.ziB };
        for (int q = 0; q < 3; q++)
        {
            var lc = c + Vector3.up * ((.31f - q * .31f) * k);
            sz[q].Dysk(lc + f * .006f, R, Vector3.up, f, .1f * k, 20);
            // pierścień oprawy i daszek (górna połowa, 17 cm)
            for (int s = 0; s < 8; s++)
            {
                float t0 = s * Mathf.PI / 8f, t1 = (s + 1) * Mathf.PI / 8f;
                Vector3 d0 = R * Mathf.Cos(t0) + Vector3.up * Mathf.Sin(t0), d1 = R * Mathf.Cos(t1) + Vector3.up * Mathf.Sin(t1);
                float rv = .118f * k;
                QuadObustronny(CzarnySygn, lc + d0 * rv, lc + d1 * rv, lc + d1 * rv + f * .17f * k, lc + d0 * rv + f * .17f * k, ((d0 + d1) * .5f).normalized);
            }
        }
        glowicePojazdy++;
    }

    // Sygnalizator dla pieszych (2 komory): czerwony stojący ludzik u góry, zielony idący na dole; c = środek lica.
    static void GlowicaPiesi(Vector3 c, Vector3 f, SiatkiSygn sg)
    {
        float k = Ks; f.y = 0; f.Normalize();
        var R = Vector3.Cross(Vector3.up, f).normalized; var U = Vector3.up; float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        OBox(CzarnySygn, c - f * .1f * k, new Vector3(.3f * k, .64f * k, .2f * k), yaw);
        foreach (float dy in new[] { .16f, -.16f })
        {
            var oc = c + U * dy * k;
            OBox(SzybaSygn, oc + f * .002f, new Vector3(.24f * k, .24f * k, .004f), yaw);
            OBox(CzarnySygn, oc + U * .135f * k + f * .06f * k, new Vector3(.27f * k, .012f, .12f * k), yaw);   // daszek
            var s = dy > 0 ? sg.pCz : sg.pZi; var o = oc + f * .009f;
            Vector2 P(float x, float y) => new Vector2(x * k, y * k);
            if (dy > 0)
            {   // stojący
                s.Dysk(o + U * .072f * k, R, U, f, .022f * k, 12);
                s.Prost(o, R, U, f, -.028f * k, .028f * k, -.01f * k, .05f * k);
                s.Kreska(o, R, U, f, P(-.014f, -.01f), P(-.016f, -.09f), .02f * k); s.Kreska(o, R, U, f, P(.014f, -.01f), P(.016f, -.09f), .02f * k);
                s.Kreska(o, R, U, f, P(-.032f, .045f), P(-.036f, -.02f), .014f * k); s.Kreska(o, R, U, f, P(.032f, .045f), P(.036f, -.02f), .014f * k);
            }
            else
            {   // idący
                s.Dysk(o + R * .012f * k + U * .072f * k, R, U, f, .02f * k, 12);
                s.Kreska(o, R, U, f, P(.008f, .05f), P(-.004f, -.012f), .03f * k);
                s.Kreska(o, R, U, f, P(-.004f, -.012f), P(-.05f, -.088f), .018f * k); s.Kreska(o, R, U, f, P(-.004f, -.012f), P(.045f, -.085f), .018f * k);
                s.Kreska(o, R, U, f, P(.004f, .04f), P(-.045f, -.002f), .013f * k); s.Kreska(o, R, U, f, P(.004f, .04f), P(.05f, .01f), .013f * k);
            }
        }
        glowicePiesi++;
    }
}
