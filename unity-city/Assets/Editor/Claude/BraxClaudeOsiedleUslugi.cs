using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BUILD 15 „USŁUGI” — wspólne narzędzia wnętrz usługowych osiedla A (szpital, komisariat, kasyno, dyskoteka, siłownie):
//  • RAMKA MEBLA: mebel budowany w układzie lokalnym (x w prawo, y w górę, z = przód mebla — strona, z której podchodzi
//    użytkownik; −z = ściana / zagłówek), obrót o yaw wokół pionu; bryły trafiają do siatek łączonych (Box/OBox → Flush),
//    kolizje osobne (BoxCollider). Wymiary rzeczywiste × Ku (skala względem bohatera, k = H / 1.80 m, ograniczona do
//    0.95–1.10, żeby układ sal z wymiarami ścian zawsze się mieścił).
//  • UKŁAD Z DANYCH: każde wnętrze ma tablice ścian (osie), drzwi, pokoi i mebli — z nich powstaje geometria i z tych samych
//    tablic skrypt unity-tools/claude/plan_uslugi.py rysuje plany z góry (wymiary, przejścia, drzwi, kolizje mebli ze ścianami).
//  • ŁAZIENKI (właściciel 23:43: „w łazienkach lustro zawsze… wszystko tak, jak powinno być”): umywalka z baterią i odpływem
//    (w miejscu wylewki pusty obiekt „BRAXHORN kran” — wodę po AKCJI zrobi inny wykonawca), lustro odbijające (sonda odbić
//    na pomieszczenie, odświeżana raz przy wczytaniu, mały zasięg), lampa nad lustrem, WC z deską i spłuczką, uchwyt z papierem,
//    szczotka, suszarka / ręczniki, kosz, kabiny z drzwiami, prysznic ze słuchawką na drążku i deszczownicą.
//  • REJESTR WYPOSAŻENIA: przy każdej zbudowanej rzeczy pusty obiekt z jej nazwą (np. „wyposażenie: bateria (kran)”) w grupie
//    pomieszczenia — kontrola BraxKontrolaPomieszczen czyta nazwy, a geometria zostaje w siatkach łączonych (mało rendererów).
//  • POD MISJE: obiekty „BRAXHORN misja: …” (zleceniodawca, cel, czynność) + notatka w raporcie.
public static partial class BraxClaudeOsiedle
{
    static float Ku => Mathf.Clamp(K, .95f, 1.10f);
    static int uRzeczy, uMisje, uSondy;
    static readonly List<string> uMisjeLista = new List<string>();

    // ---------------- ramka lokalna ----------------
    static Vector3 uP; static Quaternion uQ = Quaternion.identity;
    static void URamka(Vector3 p, float yaw) { uP = p; uQ = Quaternion.Euler(0, yaw, 0); }
    static Vector3 UL(float x, float y, float z) => uP + uQ * new Vector3(x, y, z);
    static Vector3 UKier(float x, float z) => uQ * new Vector3(x, 0, z);
    // prostopadłościan w układzie lokalnym (min/max), bez kolizji
    static void UB(Material m, float x0, float x1, float y0, float y1, float z0, float z1)
        => OBoxQ(m, UL((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), uQ);
    // prostopadłościan obrócony dodatkowo wokół lokalnej osi x (np. podniesione oparcie łóżka)
    static void UBx(Material m, Vector3 c, Vector3 s, float pitch) => OBoxQ(m, UL(c.x, c.y, c.z), s, uQ * Quaternion.Euler(pitch, 0, 0));
    static void UKol(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false);
        g.transform.position = UL((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f); g.transform.rotation = uQ;
        g.AddComponent<BoxCollider>().size = new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0));
    }
    static void UCyl(Material m, float x, float y, float z, float r, float h, int seg = 16) => Cylinder(m, UL(x, y, z), r, h, seg, true, false);
    static void URura(Material m, float ax, float ay, float az, float bx, float by, float bz, float r, int seg = 8) => Tube(m, UL(ax, ay, az), UL(bx, by, bz), r, seg);
    // czworokąt z teksturą 0..1 (ekran, plakat, sukno): środek lokalnie, szerokość w (lokalne x lub zadana oś), wysokość h
    // goraLok = kierunek górnej krawędzi obrazu; prawa krawędź liczona jak w Sign(): prawo oglądającego = cross(góra, −n)
    static void UNapis(Material m, Vector3 cLok, Vector3 nLok, Vector3 goraLok, float w, float h)
    {
        var c = UL(cLok.x, cLok.y, cLok.z); var n = (uQ * nLok).normalized; var up = (uQ * goraLok).normalized; var right = Vector3.Cross(up, -n).normalized;
        var b = B(m); int k = b.v.Count; var f0 = c + n * .002f;
        var p = new[] { f0 - right * w * .5f - up * h * .5f, f0 + right * w * .5f - up * h * .5f, f0 + right * w * .5f + up * h * .5f, f0 - right * w * .5f + up * h * .5f };
        var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        for (int q = 0; q < 4; q++) { b.v.Add(p[q]); b.n.Add(n); b.t.Add(new Vector4(right.x, right.y, right.z, 1)); b.uv.Add(uv[q]); }
        if (Vector3.Dot(Vector3.Cross(p[1] - p[0], p[2] - p[0]), n) >= 0) b.i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        else b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
    }
    // wpis do rejestru wyposażenia (pusty obiekt z nazwą rzeczy w miejscu rzeczy) — w grupie bieżącego pomieszczenia
    static void URzecz(string nazwa, float x, float y, float z)
    {
        var g = new GameObject("wyposażenie: " + nazwa); g.transform.SetParent(Group(group), false); g.transform.position = UL(x, y, z); g.transform.rotation = uQ; uRzeczy++;
    }
    static void URzeczW(string nazwa, Vector3 p) { var g = new GameObject("wyposażenie: " + nazwa); g.transform.SetParent(Group(group), false); g.transform.position = p; uRzeczy++; }
    // miejsce zadania (POD MISJE): zleceniodawca / cel / czynność
    static void UMisja(string nazwa, Vector3 p, string opis)
    {
        var g = new GameObject("BRAXHORN misja: " + nazwa); g.transform.SetParent(Group("Usługi / miejsca zadań (misje)"), false); g.transform.position = p;
        uMisje++; uMisjeLista.Add(nazwa + " " + F2(p) + " — " + opis); rep.notes.Add("POD MISJE: " + nazwa + " " + F2(p) + " — " + opis);
    }
    static string F2(Vector3 p) => "(" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + ")";

    // ---------------- materiały ----------------
    static Material UPorcelana => Plain("porcelana sanitarna", new Color(.95f, .95f, .94f), .85f);
    static Material ULustro => Plain("lustro łazienkowe (odbija sondę)", new Color(.93f, .94f, .95f), .97f, 1f);
    static Material UInox => Plain("stal nierdzewna szczotkowana", new Color(.70f, .71f, .73f), .62f, .9f);
    static Material UBialyPlastik => Plain("plastik biały (osprzęt)", new Color(.93f, .93f, .92f), .45f);
    static Material USzaryPlastik => Plain("plastik szary (osprzęt)", new Color(.45f, .47f, .50f), .4f);
    static Material UCzarnyPlastik => Plain("plastik czarny (ekrany, obudowy)", new Color(.03f, .03f, .035f), .55f);
    static Material ULaminat => Plain("laminat HPL kabin WC (szary)", new Color(.56f, .58f, .60f), .35f);
    static Material UPapier => Plain("papier toaletowy", new Color(.97f, .97f, .95f), .1f);
    static Material UPrzescieradlo => Plain("pościel szpitalna biała", new Color(.94f, .95f, .96f), .12f);
    static Material UKoc => Plain("koc szpitalny błękitny", new Color(.55f, .70f, .82f), .1f);
    static Material UMaterac => Plain("materac szpitalny granatowy", new Color(.16f, .24f, .38f), .3f);
    static Material UDrewnoJasne => Pbr("drewno jasne (buk, mebel)", "wood_floor", 1.0f, new Color(1.05f, .95f, .82f), .5f);
    static Material UDrewnoCiemne => Pbr("drewno orzech (mebel)", "wood_floor", 1.0f, new Color(.55f, .38f, .26f), .55f);
    static Material UTkaninaZaslon => Plain("zasłona przyłóżkowa (tkanina mięta)", new Color(.62f, .78f, .74f), .08f);
    static Material UTapicerkaNieb => Plain("tapicerka granat (krzesła, kozetki)", new Color(.10f, .17f, .30f), .3f);
    static Material UGuma => GumaCzarna;
    static Material UPlytkiSciana => Pbr("płytki ścienne białe 30×60", "large_grey_tiles", .6f, new Color(1.05f, 1.05f, 1.06f), .6f);
    static Material UPlytkiPodloga => Pbr("gres antypoślizgowy szary", "large_grey_tiles", .9f, new Color(.62f, .64f, .66f), .45f);
    static Material ULampa => LampGlass;
    static Material UEkran(string id) => SignMat(id);

    // ---------------- sonda odbić (lustro odbija pomieszczenie; bez lustra w czasie rzeczywistym) ----------------
    static void USonda(Bounds pokoj)
    {
        var g = new GameObject("sonda odbić (lustro)"); g.transform.SetParent(Group(group), false); g.transform.position = pokoj.center;
        var p = g.AddComponent<ReflectionProbe>();
        p.mode = ReflectionProbeMode.Realtime; p.refreshMode = ReflectionProbeRefreshMode.OnAwake; p.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        p.resolution = 64; p.boxProjection = true; p.size = pokoj.size + new Vector3(.1f, .1f, .1f); p.nearClipPlane = .05f;
        p.farClipPlane = Mathf.Max(3f, pokoj.size.magnitude + .5f); p.importance = 2; p.hdr = false; p.shadowDistance = 0f;
        uSondy++;
    }

    // ---------------- wyposażenie łazienek (ściana po stronie −z lokalnie, punkt = lico ściany na podłodze) ----------------
    // Umywalka 55 × 45 cm, wierzch 85 cm: misa (wgłębienie), bateria stojąca z wylewką, odpływ, syfon, lustro 60 × 80 z lampą,
    // dozownik mydła, pojemnik na ręczniki papierowe (opcjonalnie).
    static void UUmywalka(Vector3 lico, float yaw, float fy, bool lustro = true, bool reczniki = true, bool szafka = false)
    {
        float k = Ku; URamka(lico, yaw);
        float w = .55f * k, d = .45f * k, top = fy + .85f * k, gr = .14f * k;
        var por = UPorcelana;
        if (szafka) { UB(UDrewnoJasne, -w * .5f, w * .5f, fy + .12f, top - gr, .01f, d - .03f); UB(UChrom2, -.08f, .08f, top - gr - .1f, top - gr - .08f, d - .03f, d - .01f); URzecz("szafka podumywalkowa", 0, fy + .5f, d * .5f); }
        // misa: ścianki dookoła wgłębienia + dno
        UB(por, -w * .5f, w * .5f, top - gr, top - gr + .03f, 0, d);                               // dno misy
        UB(por, -w * .5f, -w * .5f + .05f, top - gr, top, 0, d); UB(por, w * .5f - .05f, w * .5f, top - gr, top, 0, d);
        UB(por, -w * .5f, w * .5f, top - gr, top, d - .05f, d); UB(por, -w * .5f, w * .5f, top - gr, top, 0, .12f * k);   // tył z półką na baterię
        UCyl(UChrom2, 0, top - gr + .03f, d * .55f, .022f, .004f, 12); URzecz("odpływ umywalki", 0, top - gr + .03f, d * .55f);
        URura(UChrom2, 0, top - gr, d * .55f, 0, fy + .45f * k, d * .45f, .018f); URura(UChrom2, 0, fy + .45f * k, d * .45f, 0, fy + .45f * k, 0, .018f);   // syfon do ściany
        URzecz("syfon", 0, fy + .5f * k, d * .4f);
        // bateria: korpus, wylewka, dźwignia
        UCyl(UChrom2, 0, top, .07f * k, .025f, .17f * k, 14); URura(UChrom2, 0, top + .15f * k, .07f * k, 0, top + .15f * k, .19f * k, .012f);
        UB(UChrom2, -.01f, .01f, top + .17f * k, top + .19f * k, .04f * k, .12f * k);
        URzecz("bateria (kran)", 0, top + .1f, .07f * k);
        var kran = new GameObject("BRAXHORN kran"); kran.transform.SetParent(Group(group), false);       // pusty: tu wylatuje woda (AKCJA — inny wykonawca)
        kran.transform.position = UL(0, top + .14f * k, .19f * k); kran.transform.rotation = uQ * Quaternion.Euler(90f, 0, 0);
        URzecz("umywalka", 0, top - .05f, d * .5f);
        UKol(-w * .5f, w * .5f, top - gr - .05f, top + .05f, 0, d);
        // dozownik mydła na ścianie obok
        UB(UBialyPlastik, w * .5f + .05f, w * .5f + .15f, top + .2f, top + .42f, 0, .1f); URzecz("dozownik mydła", w * .5f + .1f, top + .3f, .05f);
        if (lustro)
        {
            UB(ULustro, -.3f * k, .3f * k, top + .3f, top + 1.1f * k, 0, .008f); URzecz("lustro", 0, top + .7f, .01f);
            UB(UChrom2, -.31f * k, .31f * k, top + .29f, top + .3f, 0, .012f);
            UB(UChrom2, -.25f * k, .25f * k, top + 1.16f * k, top + 1.22f * k, 0, .07f); UB(ULampa, -.24f * k, .24f * k, top + 1.155f * k, top + 1.16f * k, .005f, .065f);
            URzecz("lampa nad lustrem (kinkiet)", 0, top + 1.2f * k, .04f);
        }
        if (reczniki) { UB(UInox, -w * .5f - .45f, -w * .5f - .15f, top + .15f, top + .55f, 0, .11f); UB(UCzarnyPlastik, -w * .5f - .4f, -w * .5f - .2f, top + .14f, top + .16f, .03f, .09f); URzecz("ręczniki papierowe (pojemnik)", -w * .5f - .3f, top + .35f, .05f); }
    }
    static Material UChrom2 => Chrom;

    // Miska WC podwieszana z deską (klapa podniesiona), stelaż w ścianie z przyciskiem spłuczki, uchwyt z rolką papieru, szczotka.
    static void USedes(Vector3 lico, float yaw, float fy, bool porecze = false)
    {
        float k = Ku; URamka(lico, yaw); var por = UPorcelana;
        float sy = fy + .41f * k;   // wierzch deski
        UB(por, -.18f * k, .18f * k, fy + .18f * k, sy - .04f, .02f, .2f * k);                       // tył miski przy ścianie
        UCyl(por, 0, fy + .2f * k, .36f * k, .18f * k, sy - .04f - fy - .2f * k, 20);                // korpus miski
        UB(por, -.17f * k, .17f * k, fy + .2f * k, sy - .04f, .16f * k, .36f * k);
        UCyl(UBialyPlastik, 0, sy - .035f, .36f * k, .19f * k, .025f, 22); URzecz("deska sedesowa", 0, sy, .36f * k);
        UBx(UBialyPlastik, new Vector3(0, sy + .2f * k, .05f), new Vector3(.36f * k, .4f * k, .025f), -8f); URzecz("klapa", 0, sy + .2f, .05f);
        URzecz("sedes (miska WC)", 0, sy - .1f, .3f * k);
        UB(UBialyPlastik, -.12f * k, .12f * k, fy + .98f * k, fy + 1.14f * k, 0, .015f); UB(UChrom2, -.1f * k, -.005f, fy + 1.0f * k, fy + 1.12f * k, .015f, .022f); UB(UChrom2, .005f, .1f * k, fy + 1.0f * k, fy + 1.12f * k, .015f, .022f);
        URzecz("przycisk spłuczki (stelaż podtynkowy)", 0, fy + 1.06f, .02f);
        UKol(-.2f * k, .2f * k, fy, sy, 0, .58f * k);
        // uchwyt z rolką papieru (po prawej, 70 cm)
        UB(UChrom2, .3f * k, .32f * k, fy + .66f * k, fy + .72f * k, 0, .08f); URura(UChrom2, .31f * k, fy + .69f * k, .07f, .43f * k, fy + .69f * k, .07f, .008f);
        URura(UPapier, .32f * k, fy + .69f * k, .07f, .43f * k, fy + .69f * k, .07f, .055f, 14);   // rolka na trzpieniu (oś pozioma)
        URzecz("uchwyt na papier toaletowy z rolką", .37f * k, fy + .69f, .07f);
        // szczotka WC w pojemniku (po lewej)
        UCyl(UInox, -.32f * k, fy, .12f, .055f, .3f, 12); URura(UBialyPlastik, -.32f * k, fy + .3f, .12f, -.32f * k, fy + .45f, .12f, .01f); URzecz("szczotka WC", -.32f * k, fy + .2f, .12f);
        if (porecze)
        {
            foreach (float sx in new[] { -.38f, .38f }) { URura(UInox, sx * k, fy + .78f * k, .05f, sx * k, fy + .78f * k, .7f * k, .018f); URura(UInox, sx * k, fy + .78f * k, .7f * k, sx * k, fy + .55f * k, .7f * k, .018f); }
            URzecz("poręcze przy WC (dla niepełnosprawnych)", 0, fy + .78f, .4f);
        }
    }
    static void UPisuar(Vector3 lico, float yaw, float fy)
    {
        float k = Ku; URamka(lico, yaw); var por = UPorcelana;
        UB(por, -.18f * k, .18f * k, fy + .5f * k, fy + 1.1f * k, 0, .1f); UB(por, -.18f * k, .18f * k, fy + .5f * k, fy + .62f * k, .1f, .34f * k);
        UB(por, -.18f * k, -.15f * k, fy + .62f * k, fy + 1.0f * k, .1f, .32f * k); UB(por, .15f * k, .18f * k, fy + .62f * k, fy + 1.0f * k, .1f, .32f * k);
        UB(UChrom2, -.03f, .03f, fy + 1.12f * k, fy + 1.2f * k, 0, .06f); URzecz("pisuar", 0, fy + .8f, .2f); URzecz("spłuczka pisuaru", 0, fy + 1.15f, .03f);
        UKol(-.18f * k, .18f * k, fy + .5f * k, fy + 1.1f * k, 0, .34f * k);
    }
    static void USuszarka(Vector3 lico, float yaw, float y)
    {
        URamka(lico, yaw);
        UB(UInox, -.14f, .14f, y, y + .28f, 0, .16f); UB(UCzarnyPlastik, -.06f, .06f, y - .005f, y + .01f, .06f, .14f); URzecz("suszarka do rąk", 0, y + .14f, .08f);
    }
    static void UKosz(Vector3 p, float fy, bool pedal = true, float yaw = 0f)
    {
        float k = Ku; URamka(new Vector3(p.x, fy, p.z), yaw);
        UCyl(UInox, 0, 0, 0, .14f * k, .4f * k, 16); UCyl(UCzarnyPlastik, 0, .4f * k, 0, .145f * k, .02f, 16);
        if (pedal) UB(UCzarnyPlastik, -.05f, .05f, 0, .03f, .12f * k, .19f * k);
        URzecz("kosz na śmieci", 0, .2f, 0); UKol(-.15f * k, .15f * k, 0, .42f * k, -.15f * k, .15f * k);
    }
    static void UWentylacja(Vector3 lico, float yaw, float y) { URamka(lico, yaw); UB(UBialyPlastik, -.12f, .12f, y - .12f, y + .12f, 0, .02f); for (int i = 0; i < 6; i++) UB(USzaryPlastik, -.1f, .1f, y - .1f + i * .04f, y - .09f + i * .04f, .02f, .025f); URzecz("kratka wentylacyjna", 0, y, .02f); }
    static void UDywanik(Vector3 c, float fy, float w, float d) { URamka(new Vector3(c.x, fy, c.z), 0); UB(Plain("dywanik łazienkowy (szary)", new Color(.42f, .45f, .48f), .05f), -w * .5f, w * .5f, 0, .012f, -d * .5f, d * .5f); URzecz("dywanik łazienkowy", 0, .01f, 0); }
    static void UWieszakRecznik(Vector3 lico, float yaw, float y, Color kolor)
    {
        URamka(lico, yaw); URura(UChrom2, -.3f, y, .07f, .3f, y, .07f, .012f); UB(UChrom2, -.32f, -.3f, y - .03f, y + .03f, 0, .08f); UB(UChrom2, .3f, .32f, y - .03f, y + .03f, 0, .08f);
        UB(Plain("ręcznik " + kolor.r.ToString("0.00"), kolor, .05f), -.25f, .25f, y - .55f, y + .015f, .06f, .085f); URzecz("ręcznik na wieszaku", 0, y - .2f, .07f);
    }
    // Kabina WC 2.0 m (płyty HPL 2.5 cm, 15 cm nad podłogą), drzwi uchylone o 25° — prostokąt x0..x1 (szerokość), z0..z1, front od strony fz (+1 = przód na +z).
    static void UKabinaWC(float x0, float x1, float z0, float z1, int front, float fy, bool lewaSciana, bool prawaSciana, bool alongX = true)
    {
        // układ: alongX — kabina „szerokością” wzdłuż x, głębokością wzdłuż z (front na z0 lub z1)
        float h0 = fy + .15f, h1 = fy + 2.0f, t = .025f;
        var lam = ULaminat;
        float fzz = front > 0 ? z1 : z0;
        if (alongX)
        {
            if (lewaSciana) { BoxMinMax(lam, x0, x0 + t, h0, h1, z0, z1, true); }
            if (prawaSciana) { BoxMinMax(lam, x1 - t, x1, h0, h1, z0, z1, true); }
            // front: dwa wąskie pasy + drzwi 0.65 m uchylone do środka
            float dw = Mathf.Min(.65f, x1 - x0 - .2f), dx0 = (x0 + x1) * .5f - dw * .5f, dx1 = dx0 + dw;
            BoxMinMax(lam, x0, dx0, h0, h1, fzz - t * .5f, fzz + t * .5f, true); BoxMinMax(lam, dx1, x1, h0, h1, fzz - t * .5f, fzz + t * .5f, true);
            // zawias przy dx0, skrzydło uchylone o 25° do środka kabiny: Euler(0,θ,0)·(1,0,0) = (cos θ, 0, −sin θ)
            var zaw = new Vector3(dx0, 0, fzz);
            var q = Quaternion.Euler(0, front > 0 ? 25f : -25f, 0); var c = zaw + q * new Vector3(dw * .5f, 0, 0); c.y = (h0 + h1) * .5f;
            var dq = q; OBoxQ(lam, c, new Vector3(dw, h1 - h0, t), dq, true);
            OBoxQ(UChrom2, c + dq * new Vector3(dw * .38f, -.05f, front > 0 ? .02f : -.02f), new Vector3(.12f, .02f, .02f), dq);   // klamka / wskaźnik zamka
            BoxMinMax(UInox, x0, x1, h1 - .02f, h1, fzz - .02f, fzz + .02f, false);   // kątownik górny
            URzeczW("kabina WC z drzwiami", new Vector3((x0 + x1) * .5f, 1f, (z0 + z1) * .5f)); URzeczW("drzwi kabiny", c);
        }
        else
        {
            if (lewaSciana) { BoxMinMax(lam, x0, x1, h0, h1, z0, z0 + t, true); }
            if (prawaSciana) { BoxMinMax(lam, x0, x1, h0, h1, z1 - t, z1, true); }
            float fx = front > 0 ? x1 : x0;
            float dw = Mathf.Min(.65f, z1 - z0 - .2f), dz0 = (z0 + z1) * .5f - dw * .5f, dz1 = dz0 + dw;
            BoxMinMax(lam, fx - t * .5f, fx + t * .5f, h0, h1, z0, dz0, true); BoxMinMax(lam, fx - t * .5f, fx + t * .5f, h0, h1, dz1, z1, true);
            // zawias przy dz0, skrzydło (wzdłuż z) uchylone do środka: Euler(0,θ,0)·(0,0,1) = (sin θ, 0, cos θ)
            var zaw = new Vector3(fx, 0, dz0);
            var q = Quaternion.Euler(0, front > 0 ? -25f : 25f, 0); var c = zaw + q * new Vector3(0, 0, dw * .5f); c.y = (h0 + h1) * .5f;
            OBoxQ(lam, c, new Vector3(t, h1 - h0, dw), q, true);
            BoxMinMax(UInox, fx - .02f, fx + .02f, h1 - .02f, h1, z0, z1, false);
            URzeczW("kabina WC z drzwiami", new Vector3((x0 + x1) * .5f, 1f, (z0 + z1) * .5f)); URzeczW("drzwi kabiny", c);
        }
    }
    // Prysznic (brodzik z odpływem liniowym, szklana ścianka, bateria termostatyczna, słuchawka na drążku, deszczownica,
    // półka z kosmetykami) — prostokąt x0..x1 / z0..z1, ściana z armaturą: −z lokalnie przy `lico`, yaw.
    static void UPrysznic(Vector3 lico, float yaw, float fy, float szer, float gleb, bool szyba = true, bool siedzisko = false)
    {
        URamka(lico, yaw);
        UB(UBialyPlastik, -szer * .5f, szer * .5f, fy, fy + .03f, 0, gleb); URzecz("brodzik", 0, fy + .02f, gleb * .5f);
        UB(UInox, -szer * .4f, szer * .4f, fy + .03f, fy + .034f, gleb - .12f, gleb - .06f); URzecz("odpływ liniowy prysznica", 0, fy + .03f, gleb - .09f);
        if (szyba) { UB(GlassClear, szer * .5f - .01f, szer * .5f, fy + .03f, fy + 2.0f, 0, gleb * .75f); UKol(szer * .5f - .02f, szer * .5f, fy, fy + 2f, 0, gleb * .75f); URzecz("ścianka prysznicowa (szkło)", szer * .5f, fy + 1f, gleb * .4f); }
        UB(UChrom2, -.12f, .12f, fy + 1.0f, fy + 1.1f, 0, .06f); UB(UChrom2, -.13f, -.1f, fy + 1.02f, fy + 1.08f, .06f, .1f); UB(UChrom2, .1f, .13f, fy + 1.02f, fy + 1.08f, .06f, .1f);
        URzecz("bateria prysznicowa (termostat)", 0, fy + 1.05f, .05f);
        URura(UChrom2, -.25f, fy + .9f, .04f, -.25f, fy + 2.0f, .04f, .012f); URzecz("drążek prysznicowy", -.25f, fy + 1.4f, .04f);
        UB(UChrom2, -.27f, -.23f, fy + 1.55f, fy + 1.6f, .02f, .07f); UCyl(UChrom2, -.25f, fy + 1.6f, .11f, .045f, .03f, 14); URura(UChrom2, -.25f, fy + 1.6f, .07f, -.25f, fy + 1.75f, .11f, .015f);
        URzecz("słuchawka prysznicowa", -.25f, fy + 1.68f, .1f);
        URura(UChrom2, 0, fy + 2.2f, 0, 0, fy + 2.2f, .35f, .015f); UCyl(UChrom2, 0, fy + 2.16f, .38f, .14f, .025f, 20); URzecz("deszczownica", 0, fy + 2.17f, .38f);
        UB(UInox, szer * .5f - .35f, szer * .5f - .05f, fy + 1.3f, fy + 1.32f, 0, .14f); UCyl(UBialyPlastik, szer * .5f - .28f, fy + 1.32f, .07f, .03f, .17f, 10); UCyl(Plain("butelka szamponu (błękit)", new Color(.25f, .5f, .8f), .5f), szer * .5f - .18f, fy + 1.32f, .07f, .03f, .2f, 10);
        URzecz("półka z kosmetykami", szer * .5f - .2f, fy + 1.32f, .07f);
        if (siedzisko) { UB(UBialyPlastik, -szer * .5f + .05f, -szer * .5f + .45f, fy + .46f, fy + .5f, .02f, .42f); URura(UInox, -szer * .5f + .05f, fy + .85f, .05f, szer * .5f - .5f, fy + .85f, .05f, .016f); URzecz("siedzisko prysznicowe i poręcz", -szer * .5f + .25f, fy + .48f, .2f); }
    }
    // Lustro z lampą na ścianie (np. przy szafkach w szatni)
    static void ULustroScienne(Vector3 lico, float yaw, float y0, float w, float h)
    {
        URamka(lico, yaw); UB(ULustro, -w * .5f, w * .5f, y0, y0 + h, 0, .008f); UB(UChrom2, -w * .5f - .01f, w * .5f + .01f, y0 - .01f, y0, 0, .012f); URzecz("lustro", 0, y0 + h * .5f, .01f);
        UB(UChrom2, -w * .4f, w * .4f, y0 + h + .05f, y0 + h + .11f, 0, .07f); UB(ULampa, -w * .39f, w * .39f, y0 + h + .045f, y0 + h + .05f, .005f, .065f); URzecz("lampa nad lustrem (kinkiet)", 0, y0 + h + .08f, .04f);
    }

    // ---------------- ściany, drzwi, okna z danych ----------------
    // Ściana: oś (x0,z0)–(x1,z1) wzdłuż x albo z; drzwi (środek na osi): otwór 1.2 × 2.65 m (właściciel 16:36), ościeżnica 7 cm,
    // nadproże do sufitu; skrzydło otwarte o 90° do pomieszczenia (strona ±1 = +z/+x), zawias przy ościeżnicy −1/+1.
    // Wykończenie każdej strony ściany wybiera funkcja `wyk(punkt 10 cm przed licem)` (lamperia / płytki / farba).
    // Wykończenie strony ściany: góra (farba), dół do hDol (lamperia 1.2 m / płytki 2.1 m), cokół 10 cm.
    public struct UWyk { public Material gora, dol, cokol; public float hDol; }
    static void UScianyZDanych((float x0, float z0, float x1, float z1)[] sciany, (float x, float z, bool wzdluzX, int zawias, int strona)[] drzwi,
                               float fy, float cy, float t, Material rdzen, Func<Vector3, UWyk?> wyk, Material skrzydlo, string grupaScian,
                               (float x, float z, bool wzdluzX, float w, float y0, float y1)[] okienka = null)
    {
        const float dw = 1.2f, dh = 2.65f, of = .07f;
        foreach (var s in sciany)
        {
            bool ax = Mathf.Abs(s.z1 - s.z0) < .001f;
            float a0 = ax ? Mathf.Min(s.x0, s.x1) : Mathf.Min(s.z0, s.z1), a1 = ax ? Mathf.Max(s.x0, s.x1) : Mathf.Max(s.z0, s.z1), c = ax ? s.z0 : s.x0;
            var otw = new List<(float o0, float o1, float y0, float y1, bool dr, int idx)>();
            for (int i = 0; i < drzwi.Length; i++)
            {
                var d = drzwi[i]; if (d.wzdluzX != ax) continue;
                float dc = ax ? d.z : d.x, da = ax ? d.x : d.z;
                if (Mathf.Abs(dc - c) > .01f || da < a0 || da > a1) continue;
                otw.Add((da - dw * .5f, da + dw * .5f, fy, fy + dh, true, i));
            }
            if (okienka != null)
                foreach (var o in okienka)
                {
                    if (o.wzdluzX != ax) continue; float oc = ax ? o.z : o.x, oa = ax ? o.x : o.z;
                    if (Mathf.Abs(oc - c) > .01f || oa < a0 || oa > a1) continue;
                    otw.Add((oa - o.w * .5f, oa + o.w * .5f, o.y0, o.y1, false, -1));
                }
            otw.Sort((p, q) => p.o0.CompareTo(q.o0));
            group = grupaScian;
            // prostokąt w układzie ściany: b wzdłuż osi ściany, n w poprzek (c ± …)
            void Prost(Material m, float b0, float b1, float y0, float y1, float n0, float n1, bool solid, bool bottom = false)
            { if (ax) BoxMinMax(m, b0, b1, y0, y1, n0, n1, solid, bottom); else BoxMinMax(m, n0, n1, y0, y1, b0, b1, solid, bottom); }
            void Seg(float b0, float b1, float y0, float y1)
            {
                if (b1 - b0 < .005f || y1 - y0 < .005f) return;
                Prost(rdzen, b0, b1, y0, y1, c - t * .5f, c + t * .5f, true, y0 > fy + .01f);
                foreach (int sd in new[] { -1, 1 })
                {
                    float f = c + sd * (t * .5f);
                    var pt = ax ? new Vector3((b0 + b1) * .5f, 1f, f + sd * .1f) : new Vector3(f + sd * .1f, 1f, (b0 + b1) * .5f);
                    var w = wyk(pt); if (!w.HasValue) continue; var W = w.Value;
                    float g0 = sd > 0 ? f : f - .004f, g1 = g0 + .004f;
                    float yd = fy + (W.dol ? W.hDol : 0f);
                    if (W.gora) { float lo = Mathf.Max(y0, yd), hi = y1; if (hi > lo) Prost(W.gora, b0, b1, lo, hi, g0, g1, false); }
                    if (W.dol) { float lo = y0, hi = Mathf.Min(y1, yd); if (hi > lo) Prost(W.dol, b0, b1, lo, hi, sd > 0 ? f : f - .005f, sd > 0 ? f + .005f : f, false); }
                    if (W.cokol && y0 <= fy + .01f) Prost(W.cokol, b0, b1, fy, fy + .1f, sd > 0 ? f : f - .012f, sd > 0 ? f + .012f : f, false);
                }
            }
            float cur = a0;
            foreach (var o in otw)
            {
                Seg(cur, o.o0, fy, cy);
                Seg(o.o0, o.o1, o.y1 + (o.dr ? of : 0f), cy);            // nadproże / ściana nad okienkiem
                if (!o.dr) Seg(o.o0, o.o1, fy, o.y0);                    // ściana pod okienkiem
                group = grupaScian;
                float tt = t * .5f + .015f;
                if (o.dr)
                {
                    // ościeżnica z opaskami z obu stron
                    Prost(Frame, o.o0 - of, o.o0, fy, o.y1 + of, c - tt, c + tt, false); Prost(Frame, o.o1, o.o1 + of, fy, o.y1 + of, c - tt, c + tt, false);
                    Prost(Frame, o.o0 - of, o.o1 + of, o.y1, o.y1 + of, c - tt, c + tt, false, true);
                    // skrzydło otwarte o 90° do pomieszczenia (strona ±1), przy ościeżnicy zawiasu; klamka przy wolnym końcu
                    var d = drzwi[o.idx]; float lw = dw - .06f;
                    if (skrzydlo)
                    {
                        float hb = d.zawias < 0 ? o.o0 + .03f : o.o1 - .03f;                       // płaszczyzna skrzydła wzdłuż osi ściany
                        float n0 = c + d.strona * (t * .5f + .01f), n1 = c + d.strona * (t * .5f + .01f + lw);
                        Prost(skrzydlo, hb - .02f, hb + .02f, fy + .01f, fy + dh - .01f, Mathf.Min(n0, n1), Mathf.Max(n0, n1), true);
                        float nk = c + d.strona * (t * .5f + lw - .1f);
                        Prost(UChrom2, hb - .08f, hb + .08f, fy + 1.03f, fy + 1.06f, nk - .015f, nk + .015f, false);
                    }
                }
                else
                {
                    // okienko (dyżurka, recepcja): szyba w ramie, blat podawczy po obu stronach
                    Prost(GlassClear, o.o0, o.o1, o.y0, o.y1, c - .006f, c + .006f, false);
                    Prost(Frame, o.o0, o.o1, o.y0 - .03f, o.y0, c - t * .5f - .14f, c + t * .5f + .14f, false, true);
                    foreach (float b in new[] { o.o0, o.o1 }) Prost(Frame, b - .02f, b + .02f, o.y0, o.y1, c - .03f, c + .03f, false);
                    Prost(Frame, o.o0, o.o1, o.y1 - .02f, o.y1 + .02f, c - .03f, c + .03f, false, true);
                    var cc = ax ? new Vector3((o.o0 + o.o1) * .5f, (o.y0 + o.y1) * .5f, c) : new Vector3(c, (o.y0 + o.y1) * .5f, (o.o0 + o.o1) * .5f);
                    Collider(cc, ax ? new Vector3(o.o1 - o.o0, o.y1 - o.y0, .06f) : new Vector3(.06f, o.y1 - o.y0, o.o1 - o.o0));
                }
                cur = o.o1;
            }
            Seg(cur, a1, fy, cy);
        }
    }
    // Okno od środka w ścianie zewnętrznej (zgodnie z oknami elewacji): wnęka, rama, parapet, żaluzje opuszczone (światło
    // rozproszone, bez szyby na ulicę — ściana skorupy jest pełna). n = normalna do wnętrza.
    static void UOknoWew(Vector3 srodekLica, Vector3 n, float szer, float y0, float y1)
    {
        var right = Vector3.Cross(Vector3.up, n).normalized; float yaw = Quaternion.LookRotation(n).eulerAngles.y;
        URamka(new Vector3(srodekLica.x, 0, srodekLica.z), yaw);
        UB(Frame, -szer * .5f - .06f, szer * .5f + .06f, y1, y1 + .06f, 0, .07f); UB(Frame, -szer * .5f - .06f, -szer * .5f, y0, y1, 0, .07f); UB(Frame, szer * .5f, szer * .5f + .06f, y0, y1, 0, .07f);
        UB(Frame, -.03f, .03f, y0, y1, 0, .05f);
        UB(UBialyPlastik, -szer * .5f - .1f, szer * .5f + .1f, y0 - .04f, y0, 0, .24f);   // parapet
        UB(TexMat("us_zaluzja", .4f, .25f), -szer * .5f, szer * .5f, y0 + .01f, y1 - .08f, .025f, .03f);   // żaluzje opuszczone (listwy 2.5 cm)
        UB(UBialyPlastik, -szer * .5f, szer * .5f, y1 - .08f, y1, .02f, .1f);              // kaseta żaluzji
        URzecz("okno z żaluzjami", 0, (y0 + y1) * .5f, .03f);
    }
    // Poręcz korytarzowa (drewno + uchwyty co 1.2 m) i odbojnica przy podłodze — wzdłuż lica od a do b, n = do wnętrza
    static void UPorecz(Vector3 a, Vector3 b, Vector3 n, float fy)
    {
        var dir = (b - a); float L = dir.magnitude; if (L < .5f) return; dir /= L;
        var pa = a + n * .07f + Vector3.up * (fy + .9f); var pb = b + n * .07f + Vector3.up * (fy + .9f);
        Tube(Plain("poręcz buk lakier", new Color(.72f, .55f, .36f), .55f), pa, pb, .025f, 10);
        for (float s = .3f; s < L - .2f; s += 1.2f) { var q = a + dir * s; BoxMinMax(UChrom2, Mathf.Min(q.x, q.x + n.x * .07f) - .015f, Mathf.Max(q.x, q.x + n.x * .07f) + .015f, fy + .82f, fy + .88f, Mathf.Min(q.z, q.z + n.z * .07f) - .015f, Mathf.Max(q.z, q.z + n.z * .07f) + .015f, false); }
        var o0 = a + n * .02f; var o1 = b + n * .02f;
        BoxMinMax(Plain("odbojnica PCV szara", new Color(.62f, .65f, .68f), .4f), Mathf.Min(o0.x, o1.x) - (Mathf.Abs(n.x) > .5f ? .02f : 0), Mathf.Max(o0.x, o1.x) + (Mathf.Abs(n.x) > .5f ? .02f : 0), fy + .25f, fy + .4f,
                  Mathf.Min(o0.z, o1.z) - (Mathf.Abs(n.z) > .5f ? .02f : 0), Mathf.Max(o0.z, o1.z) + (Mathf.Abs(n.z) > .5f ? .02f : 0), false);
        URzeczW("poręcz korytarzowa", (pa + pb) * .5f);
    }
    // Lampy: panele 60 × 60 w suficie (mleczne, nie świecą) w siatce + światła ForceVertex (mało — telefon)
    static void ULampySufit(float x0, float x1, float z0, float z1, float cy, float krok, int swiatla, float zasieg = 7f, float moc = .85f)
    {
        int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / krok)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / krok));
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
            {
                float x = x0 + (x1 - x0) * (i + .5f) / nx, z = z0 + (z1 - z0) * (j + .5f) / nz;
                BoxMinMax(ULampa, x - .3f, x + .3f, cy - .012f, cy, z - .3f, z + .3f, false, true);
                BoxMinMax(UBialyPlastik, x - .32f, x + .32f, cy - .008f, cy - .002f, z - .32f, z + .32f, false, true);
            }
        URzeczW("lampy panelowe w suficie", new Vector3((x0 + x1) * .5f, cy - .01f, (z0 + z1) * .5f));
        for (int s = 0; s < swiatla; s++)
        {
            float t = swiatla == 1 ? .5f : (s + .5f) / swiatla; bool wzdluzX = x1 - x0 >= z1 - z0;
            var p = wzdluzX ? new Vector3(Mathf.Lerp(x0, x1, t), cy - .5f, (z0 + z1) * .5f) : new Vector3((x0 + x1) * .5f, cy - .5f, Mathf.Lerp(z0, z1, t));
            PointLamp(p, zasieg, moc, new Color(1f, .97f, .92f));
        }
    }

    // ---------------- drzwi Codexa w środku (stare wnętrza) ----------------
    // Wycięcie siatek zostawiało niewidzialne skrzydła z kolizją i napis AKCJI „OTWÓRZ DRZWI” w środku sali (dyskoteka: „drzwi
    // wolnostojące na środku sali”, komisariat: skrzydła w holu) — obiekty drzwi usuwane razem z tablicy świata.
    static int UsunDrzwiCodexa(params string[] ids)
    {
        int n = 0;
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
        {
            if (!d || !ids.Any(i => d.id == i || (i.EndsWith("*") && d.id != null && d.id.StartsWith(i.TrimEnd('*'))))) continue;
            UnityEngine.Object.DestroyImmediate(d.gameObject); n++;
        }
        foreach (var w in UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.doors != null) w.doors = w.doors.Where(x => x).ToArray();
        return n;
    }
    static BraxCityDoor DrzwiCodexa(string id) => UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d && d.id == id);

    // Wycięcie starego wnętrza budynku Codexa (siatki + kolizje w środku), skorupa i drzwi zewnętrzne zostają.
    static int UWytnijWnetrze(Transform estate, Transform physics, Bounds cut)
    {
        CutVolumes(estate, physics, new List<Bounds> { cut });
        int off = 0;
        if (physics) { var e = cut; e.Expand(new Vector3(0f, .14f, 0f)); foreach (var c in physics.GetComponentsInChildren<Collider>(true)) if (c.enabled && e.Contains(c.bounds.min) && e.Contains(c.bounds.max)) { c.enabled = false; off++; } }
        return off;
    }

    // ---------------- meble z danych: grupa pomieszczenia wg prostokątów pokoi ----------------
    static string UGrupaPokoju((string nazwa, float x0, float z0, float x1, float z1)[] pokoje, float x, float z, string domyslna)
    {
        foreach (var p in pokoje) if (x > Mathf.Min(p.x0, p.x1) && x < Mathf.Max(p.x0, p.x1) && z > Mathf.Min(p.z0, p.z1) && z < Mathf.Max(p.z0, p.z1)) return p.nazwa;
        return domyslna;
    }

    // Obrys mebli (szerokość wzdłuż lokalnego x × głębokość wzdłuż lokalnego z, m, przy k = 1) — z tej tabeli plan_uslugi.py
    // rysuje rzuty i sprawdza kolizje ze ścianami, drzwiami i przejściami. Punkt mebla = środek obrysu.
    static readonly Dictionary<string, Vector2> UObrys = new Dictionary<string, Vector2>
    {
        { "lozko", new Vector2(1.02f, 2.20f) }, { "lozko_p", new Vector2(1.02f, 2.20f) }, { "szafka_przyl", new Vector2(.50f, .48f) }, { "kroplowka", new Vector2(.56f, .56f) },
        { "krzeslo", new Vector2(.52f, .55f) }, { "rzad3", new Vector2(1.80f, .62f) }, { "rzad4", new Vector2(2.38f, .62f) }, { "biurko", new Vector2(1.40f, .72f) },
        { "kozetka", new Vector2(.66f, 1.96f) }, { "parawan", new Vector2(1.80f, .40f) }, { "umywalka", new Vector2(.55f, .45f) }, { "szafka_med", new Vector2(.80f, .40f) },
        { "waga", new Vector2(.40f, .45f) }, { "kosz_med", new Vector2(.40f, .40f) }, { "automat_napoje", new Vector2(.90f, .85f) }, { "dystrybutor", new Vector2(.34f, .34f) },
        { "roslina", new Vector2(.60f, .60f) }, { "wozek_inw", new Vector2(.66f, 1.05f) }, { "lada_rej", new Vector2(6.40f, .80f) }, { "szafa_akt", new Vector2(1.00f, .45f) },
        { "lampa_zab", new Vector2(.60f, .60f) }, { "wozek_zab", new Vector2(.62f, .46f) }, { "lodowka", new Vector2(.60f, .64f) }, { "szafa_lekow", new Vector2(1.00f, .45f) },
        { "aneks", new Vector2(1.80f, .62f) }, { "stol", new Vector2(1.20f, .80f) }, { "regal", new Vector2(1.20f, .50f) }, { "wozek_porz", new Vector2(.95f, .52f) },
        { "biletomat", new Vector2(.42f, .36f) }, { "sedes", new Vector2(.44f, .62f) }, { "pisuar", new Vector2(.40f, .36f) }, { "fotel", new Vector2(.80f, .80f) },
        { "drukarka", new Vector2(.60f, .55f) }, { "lezanka_wozek", new Vector2(.70f, 2.00f) },
        // kasyno
        { "ruletka", new Vector2(3.00f, 1.40f) }, { "blackjack", new Vector2(2.10f, 1.10f) }, { "poker", new Vector2(2.60f, 1.30f) }, { "automat_gry", new Vector2(.72f, .76f) },
        { "stolek", new Vector2(.42f, .42f) }, { "hoker", new Vector2(.42f, .42f) }, { "stolik_bar", new Vector2(.70f, .70f) }, { "sofa", new Vector2(2.20f, .92f) }, { "stolik_kaw", new Vector2(1.10f, .60f) },
        // klub
        { "loza", new Vector2(3.00f, 2.20f) }, { "konsoleta", new Vector2(2.60f, .90f) }, { "kolumna", new Vector2(.70f, .80f) }, { "sub", new Vector2(.90f, .90f) },
        // komisariat
        { "szafka_ubr", new Vector2(.42f, .50f) }, { "lawka", new Vector2(1.80f, .40f) }, { "prycza", new Vector2(.82f, 2.00f) },
        // siłownia
        { "bieznia", new Vector2(.85f, 2.00f) }, { "rowerek", new Vector2(.60f, 1.20f) },
    };
}
