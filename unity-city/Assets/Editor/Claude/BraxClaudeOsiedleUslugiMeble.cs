using System.Collections.Generic;
using UnityEngine;

// BUILD 15 „USŁUGI” — biblioteka mebli i sprzętu z kodu (wymiary rzeczywiste × Ku). Każdy typ ma obrys w UObrys
// (BraxClaudeOsiedleUslugi.cs), punkt = środek obrysu, lokalne +z = przód (strona użytkownika), −z = ściana / zagłówek.
// Geometria do siatek łączonych bieżącej grupy (pomieszczenia), kolizja BoxCollider, wpisy rejestru wyposażenia.
public static partial class BraxClaudeOsiedle
{
    static float uCY = 3.2f;                    // sufit bieżącego wnętrza (szyny zasłon, lampy)
    static string uEkranPc = "us_ekran_pc";     // ekran komputerów (szpital / policja)
    static readonly List<(Vector3 p, Vector3 glowa, float y)> uLozkaZPacjentem = new List<(Vector3, Vector3, float)>();
    static readonly List<(Vector3 p, Vector3 przod, float y)> uSiedziska = new List<(Vector3, Vector3, float)>();   // wolne siedziska (poczekalnia, krzesła) dla ludzi

    static void UMebel(string typ, float x, float z, float yaw, float fy)
    {
        float k = Ku; var c = new Vector3(x, fy, z);
        URamka(c, yaw);
        switch (typ)
        {
            case "lozko": case "lozko_p": ULozko(c, yaw, fy, typ == "lozko_p"); break;
            case "szafka_przyl": USzafkaPrzyl(); break;
            case "kroplowka": UKroplowka(); break;
            case "krzeslo":
                if (offChair) { Put(offChair, c, yaw, .03f, true); uSiedziska.Add((c, Quaternion.Euler(0, yaw, 0) * Vector3.forward, fy + .45f * k)); }
                break;
            case "fotel": if (offArm) Put(offArm, c, yaw, .03f, true); break;
            case "rzad3": case "rzad4": URzadKrzesel(typ == "rzad3" ? 3 : 4); break;
            case "biurko": UBiurko(); break;
            case "kozetka": UKozetka(); break;
            case "parawan": UParawan(); break;
            case "umywalka": UUmywalka(UL(0, 0, -.225f * k), yaw, fy, true, true); break;
            case "szafka_med": USzafkaMed(); break;
            case "waga": UWaga(); break;
            case "kosz_med": UKoszMed(); break;
            case "automat_napoje": UAutomatNapoje(); break;
            case "dystrybutor": UDystrybutor(); break;
            case "roslina": URoslina(c); break;
            case "wozek_inw": UWozekInw(); break;
            case "lada_rej": ULadaRej(); break;
            case "szafa_akt": USzafaAkt(); break;
            case "lampa_zab": ULampaZab(); break;
            case "wozek_zab": UWozekZab(); break;
            case "lodowka": ULodowka(); break;
            case "szafa_lekow": USzafaLekow(); break;
            case "aneks": UAneks(); break;
            case "stol": UStol(); break;
            case "biletomat": UBiletomat(); break;
            case "sedes": USedes(UL(0, 0, -.31f * k), yaw, fy); break;
            case "pisuar": UPisuar(UL(0, 0, -.18f), yaw, fy); break;
            case "drukarka": UDrukarka(); break;
            case "lezanka_wozek": ULezankaWozek(); break;
            case "szafka_ubr": USzafkaUbr(); break;
            case "lawka": ULawka(); break;
            default: rep.notes.Add("usługi: nieznany typ mebla " + typ); break;
        }
    }

    // ---------------- łóżko szpitalne ----------------
    // Rama na kółkach z mechanizmem, materac 0.88 × 2.0 m (wierzch 0.58 m), zagłówek i podnóżek z tworzywa, barierki boczne
    // (podniesione przy pacjencie), pościel: prześcieradło, poduszka, koc (u pacjenta przykrywa ciało do piersi); na ścianie panel
    // nadłóżkowy (tlen, gniazda, lampka), przy pacjencie monitor funkcji życiowych na ramieniu; szyna zasłony pod sufitem.
    static void ULozko(Vector3 c, float yaw, float fy, bool pacjent)
    {
        float k = Ku, L = 1.0f * k, Wd = .45f * k;   // połowa długości / szerokości materaca
        var rama = Plain("rama łóżka szpitalnego (szary lakier)", new Color(.70f, .72f, .74f), .5f, .3f);
        var tworzywo = Plain("szczyty łóżka (tworzywo jasne)", new Color(.86f, .87f, .85f), .45f);
        float y0 = fy, ym = y0 + .42f * k, yt = y0 + .58f * k;
        foreach (float sx in new[] { -.4f, .4f }) foreach (float sz in new[] { -.95f, .95f })
            { UCyl(UGuma, sx * k, y0, sz * k, .05f, .1f, 10); URura(rama, sx * k, y0 + .1f, sz * k, sx * k, y0 + .3f * k, sz * k, .02f); }
        UB(rama, -.42f * k, .42f * k, y0 + .3f * k, y0 + .38f * k, -1.0f * k, 1.0f * k);           // rama nośna
        UB(USzaryPlastik, -.28f * k, .28f * k, y0 + .14f * k, y0 + .3f * k, -.7f * k, .7f * k);       // osłona mechanizmu
        UB(UMaterac, -Wd, Wd, ym - .04f, yt - .01f, -L, L);
        UB(UPrzescieradlo, -Wd - .005f, Wd + .005f, yt - .012f, yt, -L - .005f, L + .005f);
        UB(tworzywo, -.48f * k, .48f * k, y0 + .3f * k, y0 + .98f * k, -1.06f * k, -1.0f * k);       // zagłówek
        UB(tworzywo, -.48f * k, .48f * k, y0 + .3f * k, y0 + .86f * k, 1.0f * k, 1.06f * k);         // podnóżek
        UB(rama, -.3f * k, .3f * k, y0 + .82f * k, y0 + .84f * k, 1.06f * k, 1.1f * k);               // uchwyt podnóżka
        // barierki: dwie poprzeczki + 3 słupki na boku, od zagłówka do połowy łóżka
        foreach (int sd in new[] { -1, 1 })
        {
            bool gora = pacjent || sd < 0; float xr = sd * (Wd + .04f), ya = gora ? yt + .12f : yt - .1f, yb = gora ? yt + .28f : yt + .02f;
            URura(rama, xr, ya, -.92f * k, xr, ya, .15f * k, .014f); URura(rama, xr, yb, -.92f * k, xr, yb, .15f * k, .014f);
            foreach (float zz in new[] { -.9f, -.38f, .13f }) URura(rama, xr, ym - .05f, zz * k, xr, yb, zz * k, .012f);
        }
        URzecz("łóżko szpitalne z materacem", 0, yt, 0); URzecz("barierki boczne łóżka", Wd, yt + .2f, -.4f * k); URzecz("pościel (prześcieradło, koc)", 0, yt, .2f);
        // poduszka
        UB(UPrzescieradlo, -.3f * k, .3f * k, yt, yt + .12f * k, -.95f * k, -.55f * k); URzecz("poduszka", 0, yt + .06f, -.75f * k);
        if (pacjent)
        {
            // koc przykrywający ciało (od stóp do piersi), zwisa po bokach
            UB(UKoc, -Wd - .02f, Wd + .02f, yt + .02f, yt + .24f * k, -.42f * k, L + .02f);
            foreach (int sd in new[] { -1, 1 }) UB(UKoc, sd * (Wd + .01f) - .01f, sd * (Wd + .01f) + .01f, ym - .02f, yt + .22f * k, -.42f * k, L + .02f);
            UB(UPrzescieradlo, -Wd - .02f, Wd + .02f, yt + .2f * k, yt + .25f * k, -.5f * k, -.38f * k);   // wywinięta poszwa
            uLozkaZPacjentem.Add((UL(0, 0, 0), UKier(0, -1f), yt));
        }
        else
        {
            UB(UKoc, -Wd, Wd, yt, yt + .02f, -.4f * k, L);                                                      // koc wygładzony
            UB(UKoc, -Wd + .02f, Wd - .02f, yt + .02f, yt + .1f, .55f * k, .85f * k);                           // drugi koc złożony w nogach
        }
        UKol(-.5f * k, .5f * k, y0, yt + .3f * k, -1.06f * k, 1.1f * k);
        // panel nadłóżkowy na ścianie za zagłówkiem (lico ściany: z = −1.10 k)
        float zw = -1.10f * k;
        UB(UBialyPlastik, -.65f * k, .65f * k, fy + 1.32f, fy + 1.62f, zw, zw + .09f);
        UB(Plain("gniazdo tlenu (zielone)", new Color(.10f, .55f, .25f), .5f), -.5f * k, -.42f * k, fy + 1.38f, fy + 1.46f, zw + .09f, zw + .12f);
        UB(Plain("gniazdo powietrza (szare)", new Color(.40f, .42f, .45f), .5f), -.38f * k, -.3f * k, fy + 1.38f, fy + 1.46f, zw + .09f, zw + .12f);
        for (int i = 0; i < 3; i++) UB(UBialyPlastik, (.15f + i * .12f) * k, (.23f + i * .12f) * k, fy + 1.38f, fy + 1.45f, zw + .09f, zw + .11f);
        UB(ULampa, -.6f * k, .6f * k, fy + 1.56f, fy + 1.6f, zw + .09f, zw + .1f);
        URzecz("panel nadłóżkowy (tlen, powietrze, gniazda, lampka)", 0, fy + 1.47f, zw + .1f);
        UB(Plain("przycisk przywołania (czerwony)", new Color(.75f, .08f, .08f), .5f), .55f * k, .6f * k, fy + 1.4f, fy + 1.45f, zw + .09f, zw + .11f);
        if (pacjent)
        {
            // monitor funkcji życiowych na ramieniu ściennym (po prawej od zagłówka)
            URura(rama, .62f * k, fy + 1.9f, zw, .62f * k, fy + 1.9f, zw + .3f, .02f);
            UB(UCzarnyPlastik, .44f * k, .8f * k, fy + 1.76f, fy + 2.04f, zw + .3f, zw + .38f);
            UNapis(UEkran("us_ekg"), new Vector3(.62f * k, fy + 1.9f, zw + .385f), Vector3.forward, Vector3.up, .32f * k, .24f);
            URzecz("monitor funkcji życiowych (kardiomonitor)", .62f * k, fy + 1.9f, zw + .38f);
            // przewody z monitora do pacjenta
            URura(UCzarnyPlastik, .62f * k, fy + 1.76f, zw + .34f, .3f * k, yt + .2f, -.55f * k, .005f, 4);
        }
        // szyna zasłony pod sufitem (U wokół łóżka) i zasłona zebrana przy zagłówku + częściowo rozsunięta z boku
        float ys = uCY - .04f;
        UB(Frame, -.78f * k, -.75f * k, ys - .02f, ys, zw, 1.3f * k); UB(Frame, .75f * k, .78f * k, ys - .02f, ys, zw, 1.3f * k); UB(Frame, -.78f * k, .78f * k, ys - .02f, ys, 1.27f * k, 1.3f * k);
        UB(UTkaninaZaslon, .72f * k, .8f * k, uCY - 2.35f, ys - .03f, zw + .05f, zw + .35f);
        UB(UTkaninaZaslon, -.775f * k, -.755f * k, uCY - 2.35f, ys - .03f, zw + .05f, zw + .95f);
        URzecz("zasłona przyłóżkowa (szyna sufitowa)", .76f * k, uCY - 1.2f, zw + .2f);
    }

    static void USzafkaPrzyl()
    {
        float k = Ku; var korpus = Plain("szafka przyłóżkowa (biały laminat)", new Color(.92f, .92f, .9f), .4f); var front = Plain("front szafki (błękit)", new Color(.58f, .72f, .82f), .45f);
        foreach (float sx in new[] { -.2f, .2f }) foreach (float sz in new[] { -.18f, .18f }) UCyl(UGuma, sx * k, 0, sz * k, .03f, .06f, 8);
        UB(korpus, -.24f * k, .24f * k, .06f, .84f * k, -.23f * k, .23f * k);
        UB(front, -.22f * k, .22f * k, .1f, .48f * k, .23f * k, .24f * k); UB(front, -.22f * k, .22f * k, .52f * k, .68f * k, .23f * k, .24f * k);
        UB(UChrom2, -.08f, .08f, .62f * k, .64f * k, .24f * k, .26f * k); UB(UChrom2, .14f * k, .16f * k, .2f, .38f * k, .24f * k, .26f * k);
        UB(korpus, -.25f * k, .25f * k, .84f * k, .87f * k, -.24f * k, .24f * k);
        UCyl(Plain("butelka wody (błękitna)", new Color(.55f, .75f, .9f), .85f), -.1f, .87f * k, 0, .035f, .24f, 10); UCyl(UBialyPlastik, .08f, .87f * k, .05f, .04f, .1f, 10);
        URzecz("szafka przyłóżkowa", 0, .5f, 0); URzecz("kubek i woda", 0, .95f, 0);
        UKol(-.25f * k, .25f * k, 0, .87f * k, -.24f * k, .24f * k);
    }

    static void UKroplowka()
    {
        float k = Ku;
        for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2f / 5f; float ex = Mathf.Cos(a) * .27f, ez = Mathf.Sin(a) * .27f; URura(UInox, 0, .09f, 0, ex, .07f, ez, .012f, 6); UCyl(UGuma, ex, 0, ez, .025f, .05f, 8); }
        URura(UInox, 0, .07f, 0, 0, 2.05f * k, 0, .014f, 8);
        for (int i = 0; i < 4; i++) { float a = i * Mathf.PI * .5f + .4f; URura(UInox, 0, 2.0f * k, 0, Mathf.Cos(a) * .12f, 2.06f * k, Mathf.Sin(a) * .12f, .006f, 4); }
        var plyn = Plain("worek kroplówki (płyn)", new Color(.86f, .92f, .95f), .9f);
        UB(plyn, .06f, .17f, 1.68f * k, 1.92f * k, -.02f, .02f); UB(UBialyPlastik, .1f, .13f, 1.6f * k, 1.68f * k, -.01f, .01f);
        URura(UBialyPlastik, .115f, 1.6f * k, 0, .3f, .95f, .25f, .003f, 4);
        URzecz("stojak na kroplówkę", 0, 1f, 0); URzecz("kroplówka (worek z płynem)", .11f, 1.8f, 0);
        UKol(-.12f, .12f, 0, 1.8f, -.12f, .12f);
    }

    static void URzadKrzesel(int n)
    {
        float k = Ku, pitch = .58f * k, W = n * pitch + .1f; var siedz = UTapicerkaNieb;
        UB(Steel, -W * .5f + .05f, W * .5f - .05f, .3f * k, .36f * k, -.06f, .06f);                                 // belka
        foreach (float sx in new[] { -W * .5f + .3f, W * .5f - .3f }) { UB(Steel, sx - .03f, sx + .03f, 0, .33f * k, -.03f, .03f); UB(Steel, sx - .04f, sx + .04f, 0, .02f, -.28f, .28f); }
        for (int i = 0; i < n; i++)
        {
            float sx = -(n - 1) * pitch * .5f + i * pitch;
            UB(siedz, sx - .25f * k, sx + .25f * k, .41f * k, .46f * k, -.22f * k, .24f * k);                          // siedzisko
            UBx(siedz, new Vector3(sx, .7f * k, -.26f * k), new Vector3(.48f * k, .44f * k, .05f), -10f);              // oparcie
            URura(Steel, sx, .36f * k, -.05f, sx, .47f * k, -.24f * k, .015f, 6);
            uSiedziska.Add((UL(sx, 0, .02f), UKier(0, 1), uP.y + .46f * k));
        }
        for (int i = 0; i <= n; i++) { float ax = -n * pitch * .5f + i * pitch; UB(Steel, ax - .02f, ax + .02f, .46f * k, .64f * k, -.12f, .18f); UB(UCzarnyPlastik, ax - .025f, ax + .025f, .64f * k, .66f * k, -.18f, .2f); }
        URzecz("rząd krzeseł (siedziska)", 0, .45f, 0);
        UKol(-W * .5f, W * .5f, 0, .5f * k, -.28f * k, .28f * k); UKol(-W * .5f, W * .5f, .5f * k, .95f * k, -.31f * k, -.2f * k);
    }

    static void UBiurko()
    {
        float k = Ku; var blat = UDrewnoJasne; var biel = Plain("biurko (biały laminat)", new Color(.9f, .9f, .88f), .4f);
        float h = .75f * k, w = .7f * k, d = .36f * k;
        UB(blat, -w, w, h - .03f, h, -d, d);
        UB(biel, -w, -w + .025f, 0, h - .03f, -d + .02f, d - .02f); UB(biel, w - .45f * k, w, 0, h - .03f, -d + .02f, d - .02f);   // bok + kontener szuflad
        for (int i = 0; i < 3; i++) UB(UChrom2, w - .3f * k, w - .15f * k, .15f + i * .2f * k, .17f + i * .2f * k, -d, -d + .015f);
        UB(biel, -w + .025f, w - .45f * k, .3f * k, h - .03f, d - .04f, d - .02f);                                             // blenda
        // monitor przy dalszej krawędzi, przodem do siedzącego (−z), klawiatura, mysz, telefon, dokumenty, kubek z długopisami
        UB(UCzarnyPlastik, -.06f, .06f, h, h + .02f, d - .22f, d - .1f); UB(UCzarnyPlastik, -.02f, .02f, h + .02f, h + .15f, d - .17f, d - .13f);
        UB(UCzarnyPlastik, -.29f, .29f, h + .12f, h + .47f, d - .16f, d - .12f);
        UNapis(UEkran(uEkranPc), new Vector3(0, h + .295f, d - .165f), Vector3.back, Vector3.up, .54f, .32f);
        UB(UCzarnyPlastik, -.22f, .22f, h, h + .02f, -d + .12f, -d + .28f); UB(UCzarnyPlastik, .3f, .36f, h, h + .02f, -d + .15f, -d + .24f);
        UB(UCzarnyPlastik, -.6f * k, -.42f * k, h, h + .05f, -.05f, .15f); UB(UCzarnyPlastik, -.58f * k, -.44f * k, h + .05f, h + .07f, .07f, .13f);
        UB(UPapier, .32f, .56f, h, h + .03f, -.05f, .28f); UCyl(UCzarnyPlastik, .58f * k, h, .2f, .04f, .1f, 8);
        URzecz("biurko", 0, h, 0); URzecz("komputer (monitor, klawiatura)", 0, h + .3f, d - .14f); URzecz("telefon", -.5f * k, h + .05f, .05f); URzecz("dokumenty", .44f, h + .02f, .1f);
        UKol(-w, w, 0, h, -d, d);
    }

    static void UKozetka()
    {
        float k = Ku; var obicie = Plain("obicie kozetki (szaroniebieskie)", new Color(.42f, .55f, .66f), .45f);
        foreach (float sx in new[] { -.28f, .28f }) foreach (float sz in new[] { -.85f, .85f }) UB(UChrom2, sx * k - .02f, sx * k + .02f, 0, .58f * k, sz * k - .02f, sz * k + .02f);
        UB(UChrom2, -.3f * k, .3f * k, .54f * k, .58f * k, -.9f * k, .9f * k);
        UB(obicie, -.32f * k, .32f * k, .58f * k, .68f * k, -.35f * k, .95f * k);
        UBx(obicie, new Vector3(0, .78f * k, -.62f * k), new Vector3(.64f * k, .1f, .6f * k), -20f);                          // podgłówek uniesiony
        UB(UPapier, -.25f * k, .25f * k, .68f * k, .685f * k, -.3f * k, .95f * k);
        URura(UPapier, -.26f * k, .62f * k, .97f * k, .26f * k, .62f * k, .97f * k, .05f, 10); URura(UChrom2, -.3f * k, .62f * k, .97f * k, .3f * k, .62f * k, .97f * k, .008f, 4);
        URzecz("kozetka lekarska", 0, .65f, 0); URzecz("prześcieradło papierowe na rolce", 0, .69f, .95f * k);
        UKol(-.33f * k, .33f * k, 0, .7f * k, -.95f * k, .99f * k);
    }

    static void UParawan()
    {
        float k = Ku; var tk = Plain("tkanina parawanu (biała)", new Color(.93f, .94f, .93f), .1f);
        for (int i = 0; i < 3; i++)
        {
            float cx = (i - 1) * .6f * k, ang = i == 1 ? 0f : (i == 0 ? 18f : -18f);
            var q = uQ * Quaternion.Euler(0, ang, 0);
            OBoxQ(tk, UL(cx, 1.0f * k, 0), new Vector3(.56f * k, 1.4f * k, .015f), q);
            foreach (float e in new[] { -.29f, .29f }) { var p = UL(cx, 0, 0) + q * new Vector3(e * k, 0, 0); Tube(UChrom2, p + Vector3.up * .08f, p + Vector3.up * 1.75f * k, .012f, 6); Cylinder(UGuma, p, .025f, .06f, 8, true, false); }
            var a = UL(cx, 0, 0) + q * new Vector3(-.29f * k, 1.72f * k, 0); var b = UL(cx, 0, 0) + q * new Vector3(.29f * k, 1.72f * k, 0); Tube(UChrom2, a, b, .01f, 6);
        }
        URzecz("parawan (składany)", 0, 1f, 0);
        UKol(-.9f * k, .9f * k, 0, 1.75f * k, -.15f, .15f);
    }

    static void USzafkaMed()
    {
        float k = Ku; var biel = Plain("szafka medyczna (biała stal)", new Color(.93f, .93f, .92f), .55f, .2f);
        UB(biel, -.4f * k, .4f * k, .05f, 1.9f * k, -.2f * k, .18f * k);
        UB(biel, -.38f * k, .38f * k, .07f, .78f * k, .18f * k, .2f * k);                                    // drzwi dolne pełne
        UB(GlassClear, -.38f * k, .38f * k, .82f * k, 1.86f * k, .18f * k, .19f * k);                           // drzwi przeszklone
        UB(UChrom2, -.01f, .01f, .82f * k, 1.86f * k, .19f * k, .2f * k);
        var pud = new[] { Plain("opakowanie leku (biel-błękit)", new Color(.85f, .9f, .96f), .3f), Plain("opakowanie leku (pomarańcz)", new Color(.92f, .55f, .2f), .3f), Plain("opakowanie leku (zieleń)", new Color(.4f, .7f, .45f), .3f) };
        for (int s = 0; s < 3; s++)
        {
            float y = (.9f + s * .32f) * k; UB(GlassClear, -.37f * k, .37f * k, y - .01f, y, -.18f * k, .16f * k);
            for (int i = 0; i < 6; i++) UB(pud[(i + s) % 3], (-.33f + i * .11f) * k, (-.25f + i * .11f) * k, y, y + (.1f + (i % 3) * .04f), -.12f, .06f);
        }
        URzecz("szafka medyczna (przeszklona)", 0, 1f, 0);
        UKol(-.4f * k, .4f * k, 0, 1.9f * k, -.2f * k, .2f * k);
    }

    static void UWaga()
    {
        float k = Ku; var biel = UBialyPlastik;
        UB(biel, -.17f, .17f, 0, .06f, -.15f, .2f); UB(UGuma, -.15f, .15f, .06f, .065f, -.1f, .18f);
        UB(biel, -.03f, .03f, .06f, 1.1f * k, -.2f, -.15f); UB(UCzarnyPlastik, -.1f, .1f, 1.1f * k, 1.25f * k, -.22f, -.13f);
        UB(Plain("wyświetlacz wagi", new Color(.5f, .7f, .55f), .7f), -.06f, .06f, 1.16f * k, 1.21f * k, -.13f, -.125f);
        UB(UChrom2, -.01f, .01f, 1.25f * k, 2.0f * k, -.19f, -.17f); UB(UChrom2, -.01f, .12f, 1.85f * k, 1.86f * k, -.19f, -.06f);
        URzecz("waga lekarska ze wzrostomierzem", 0, 1f, -.1f); UKol(-.18f, .18f, 0, 1.3f, -.22f, .2f);
    }

    static void UKoszMed()
    {
        var zolty = Plain("pojemnik na odpady medyczne (żółty)", new Color(.92f, .76f, .1f), .4f); var czer = Plain("pokrywa odpadów medycznych (czerwona)", new Color(.72f, .08f, .08f), .45f);
        UB(zolty, -.18f, .18f, 0, .55f, -.18f, .18f); UB(czer, -.19f, .19f, .55f, .6f, -.19f, .19f); UB(czer, -.06f, .06f, 0, .03f, .18f, .24f);
        URzecz("kosz na odpady medyczne", 0, .3f, 0); URzecz("kosz", 0, .3f, 0); UKol(-.19f, .19f, 0, .6f, -.19f, .19f);
    }

    static void UAutomatNapoje()
    {
        float k = Ku; var obud = Plain("obudowa automatu (grafit metalik)", new Color(.16f, .17f, .19f), .5f, .5f);
        UB(obud, -.45f * k, .45f * k, 0, 1.83f * k, -.42f * k, .38f * k);
        UNapis(UEkran("us_automat_napoje"), new Vector3(0, .95f * k, .385f * k), Vector3.forward, Vector3.up, .84f * k, 1.68f * k);
        UB(GlassClear, -.42f * k, .2f * k, .2f * k, 1.6f * k, .39f * k, .4f * k);
        URzecz("automat z napojami", 0, 1f, .3f); UKol(-.45f * k, .45f * k, 0, 1.83f * k, -.42f * k, .42f * k);
    }

    static void UDystrybutor()
    {
        float k = Ku; var biel = UBialyPlastik;
        UB(biel, -.15f, .15f, 0, .98f * k, -.15f, .15f); UB(UCzarnyPlastik, -.12f, .12f, .55f * k, .75f * k, .15f, .17f);
        UB(Plain("kran dystrybutora (niebieski)", new Color(.15f, .35f, .8f), .5f), -.08f, -.03f, .82f * k, .88f * k, .15f, .2f); UB(Plain("kran dystrybutora (czerwony)", new Color(.8f, .1f, .1f), .5f), .03f, .08f, .82f * k, .88f * k, .15f, .2f);
        UCyl(Plain("butla wody (błękit)", new Color(.55f, .78f, .95f), .9f), 0, .98f * k, 0, .14f, .42f, 18);
        UB(UBialyPlastik, .16f, .24f, .8f * k, 1.1f * k, -.04f, .04f);
        URzecz("dystrybutor wody", 0, .9f, 0); URzecz("kubeczki", .2f, 1f, 0); UKol(-.16f, .16f, 0, 1.4f * k, -.16f, .16f);
    }

    static void URoslina(Vector3 c)
    {
        if (!plantPot) return;
        var o = Put(plantPot, c, c.x * 7f + c.z * 3f, .03f, false); if (!o) return;
        o.transform.localScale *= 3.0f; var b = RB(o); o.transform.position += new Vector3(c.x - b.center.x, c.y - b.min.y, c.z - b.center.z);
        b = RB(o); Collider(new Vector3(b.center.x, c.y + .4f, b.center.z), new Vector3(Mathf.Min(b.size.x, .55f), .8f, Mathf.Min(b.size.z, .55f)));
        URzeczW("roślina w donicy", c + Vector3.up * .5f);
    }

    static void UWozekInw()
    {
        float k = Ku; var ram = UChrom2; var tk = Plain("siedzisko wózka (czarna tkanina)", new Color(.06f, .06f, .07f), .25f);
        foreach (int sd in new[] { -1, 1 })
        {
            float x = sd * .31f * k, zc = -.18f * k, R = .3f * k, yc = R;
            Tube(UGuma, UL(x - .015f, yc, zc), UL(x + .015f, yc, zc), R, 22); Tube(ram, UL(x + sd * .03f, yc, zc), UL(x + sd * .035f, yc, zc), R - .03f, 22);
            for (int s = 0; s < 8; s++) { float a = s * Mathf.PI / 4f; URura(ram, x, yc, zc, x, yc + Mathf.Sin(a) * (R - .03f), zc + Mathf.Cos(a) * (R - .03f), .003f, 3); }
            UCyl(ram, x, yc - .03f, zc, .035f, .06f, 8);
            URura(ram, sd * .24f * k, .5f * k, -.3f * k, sd * .24f * k, .5f * k, .3f * k, .012f, 6);             // rama siedziska
            URura(ram, sd * .24f * k, .5f * k, -.3f * k, sd * .24f * k, .98f * k, -.36f * k, .012f, 6);          // oparcie
            URura(ram, sd * .24f * k, .98f * k, -.36f * k, sd * .24f * k, .98f * k, -.5f * k, .014f, 6);         // rączka
            URura(ram, sd * .24f * k, .5f * k, .3f * k, sd * .2f * k, .12f * k, .42f * k, .012f, 6);              // podnóżek
            UB(UCzarnyPlastik, sd * .2f * k - .07f, sd * .2f * k + .07f, .1f * k, .12f * k, .38f * k, .5f * k);
            Tube(UGuma, UL(sd * .24f * k - .015f, .075f, .38f * k), UL(sd * .24f * k + .015f, .075f, .38f * k), .075f, 12);
            URura(ram, sd * .24f * k, .15f, .38f * k, sd * .24f * k, .5f * k, .3f * k, .012f, 6);
            URura(ram, sd * .25f * k, .7f * k, -.25f * k, sd * .25f * k, .7f * k, .2f * k, .014f, 6);             // podłokietnik
        }
        UB(tk, -.23f * k, .23f * k, .47f * k, .5f * k, -.3f * k, .3f * k); UBx(tk, new Vector3(0, .75f * k, -.33f * k), new Vector3(.46f * k, .44f * k, .02f), -8f);
        URzecz("wózek inwalidzki", 0, .5f, 0); UKol(-.34f * k, .34f * k, 0, 1.0f * k, -.5f * k, .5f * k);
        uSiedziskaWozek.Add((UL(0, 0, -.02f), UKier(0, 1), uP.y + .5f * k));
    }
    static readonly List<(Vector3 p, Vector3 przod, float y)> uSiedziskaWozek = new List<(Vector3, Vector3, float)>();

    // Lada rejestracji 6.4 m (długość wzdłuż lokalnego x), klient od +z: front z okleiną, półka klienta 1.10 m, blat pracy 0.75 m,
    // szyba z okienkami do mówienia, 2 stanowiska (monitor, klawiatura, telefon), drukarka, segregatory.
    static void ULadaRej()
    {
        float k = Ku, Lh = 3.2f, d = .4f, hk = 1.1f * k, hp = .75f * k;
        var okl = UDrewnoJasne; var kw = Plain("blat kwarc biały", new Color(.93f, .92f, .90f), .8f); var biel = Plain("biurko (biały laminat)", new Color(.9f, .9f, .88f), .4f);
        UB(okl, -Lh, Lh, .1f, hk, d - .05f, d); UB(UCzarnyPlastik, -Lh, Lh, 0, .1f, d - .08f, d - .03f);
        UB(kw, -Lh - .02f, Lh + .02f, hk, hk + .03f, .1f, d + .04f);
        UB(biel, -Lh, Lh, hp - .03f, hp, -d, d - .05f);
        foreach (float sx in new[] { -Lh + .02f, -1.0f, 1.0f, Lh - .02f }) UB(biel, sx - .02f, sx + .02f, 0, hp - .03f, -d, d - .05f);
        UB(biel, -Lh, Lh, hp, hk, d - .25f, d - .05f);
        UB(GlassClear, -Lh, Lh, hk + .03f, 2.05f * k, .2f, .21f);
        foreach (float sx in new[] { -Lh, -1.6f, 0f, 1.6f, Lh }) UB(UChrom2, sx - .015f, sx + .015f, hk + .03f, 2.05f * k, .19f, .22f);
        UB(UChrom2, -Lh, Lh, 2.05f * k, 2.08f * k, .19f, .22f);
        foreach (float sx in new[] { -1.6f, 1.6f })
        {
            UB(UCzarnyPlastik, sx - .29f, sx + .29f, hp + .12f, hp + .47f, -.05f, -.01f); UB(UCzarnyPlastik, sx - .03f, sx + .03f, hp, hp + .14f, -.06f, -.02f);
            UNapis(UEkran(uEkranPc), new Vector3(sx, hp + .295f, -.055f), Vector3.back, Vector3.up, .54f, .32f);
            UB(UCzarnyPlastik, sx - .22f, sx + .22f, hp, hp + .02f, -.32f, -.16f);
            UB(UCzarnyPlastik, sx + .45f, sx + .62f, hp, hp + .05f, -.2f, 0f); UB(UCzarnyPlastik, sx + .47f, sx + .6f, hp + .05f, hp + .07f, -.18f, -.03f);
            URzecz("komputer (stanowisko rejestracji)", sx, hp + .3f, -.03f); URzecz("telefon", sx + .53f, hp + .05f, -.1f);
        }
        UB(USzaryPlastik, -.35f, .35f, hp, hp + .25f, -.35f, .05f); URzecz("drukarka etykiet / skaner", 0, hp + .15f, -.15f);
        for (int i = 0; i < 8; i++) UB(Plain("segregator " + (i % 4), new[] { new Color(.15f, .3f, .6f), new Color(.7f, .15f, .12f), new Color(.2f, .5f, .3f), new Color(.85f, .7f, .2f) }[i % 4], .35f), 2.3f + i * .08f, 2.36f + i * .08f, hp, hp + .32f, -.35f, -.05f);
        URzecz("lada rejestracji", 0, hk, d); URzecz("szyba z okienkami", 0, 1.6f, .2f);
        UKol(-Lh, Lh, 0, hk + .03f, -d, d + .04f); UKol(-Lh, Lh, hk, 2.08f * k, .18f, .23f);
    }

    static void USzafaAkt()
    {
        float k = Ku; var st = Plain("regał metalowy (jasnoszary)", new Color(.75f, .77f, .78f), .45f, .3f);
        UB(st, -.5f * k, -.48f * k, 0, 2.0f * k, -.22f * k, .22f * k); UB(st, .48f * k, .5f * k, 0, 2.0f * k, -.22f * k, .22f * k); UB(st, -.5f * k, .5f * k, 0, 2.0f * k, -.22f * k, -.2f * k);
        var kol = new[] { new Color(.15f, .3f, .6f), new Color(.7f, .15f, .12f), new Color(.2f, .5f, .3f), new Color(.85f, .7f, .2f), new Color(.3f, .3f, .32f) };
        for (int s = 0; s < 5; s++)
        {
            float y = (.05f + s * .4f) * k; UB(st, -.48f * k, .48f * k, y, y + .02f, -.2f * k, .22f * k);
            if (s == 4) continue;
            for (int i = 0; i < 12; i++) UB(Plain("segregator " + ((i + s) % 5), kol[(i + s) % 5], .35f), (-.46f + i * .077f) * k, (-.4f + i * .077f) * k, y + .02f, y + .34f * k, -.18f * k, .16f * k);
        }
        URzecz("szafa z dokumentacją (segregatory)", 0, 1f, 0); UKol(-.5f * k, .5f * k, 0, 2.0f * k, -.22f * k, .22f * k);
    }

    static void ULampaZab()
    {
        float k = Ku;
        for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2f / 5f; URura(UInox, 0, .08f, 0, Mathf.Cos(a) * .27f, .06f, Mathf.Sin(a) * .27f, .012f, 6); UCyl(UGuma, Mathf.Cos(a) * .27f, 0, Mathf.Sin(a) * .27f, .025f, .05f, 8); }
        URura(UInox, 0, .06f, 0, 0, 1.55f * k, 0, .016f, 8); URura(UInox, 0, 1.55f * k, 0, -.45f, 1.75f * k, 0, .014f, 8);
        UCyl(UBialyPlastik, -.45f, 1.62f * k, 0, .16f, .1f, 18); UCyl(ULampa, -.45f, 1.615f * k, 0, .13f, .006f, 18);
        URzecz("lampa zabiegowa", -.45f, 1.65f, 0); UKol(-.15f, .15f, 0, 1.6f, -.15f, .15f);
    }

    static void UWozekZab()
    {
        float k = Ku;
        foreach (float sx in new[] { -.27f, .27f }) foreach (float sz in new[] { -.19f, .19f }) { UB(UInox, sx * k - .012f, sx * k + .012f, .08f, .9f * k, sz * k - .012f, sz * k + .012f); UCyl(UGuma, sx * k, 0, sz * k, .035f, .06f, 8); }
        foreach (float y in new[] { .25f, .55f, .88f }) UB(UInox, -.3f * k, .3f * k, y * k, y * k + .015f, -.22f * k, .22f * k);
        URura(UInox, -.28f * k, .95f * k, -.25f * k, .28f * k, .95f * k, -.25f * k, .01f, 6);
        UB(Plain("taca nerkowata (stal)", new Color(.75f, .76f, .78f), .7f, .9f), -.15f, .1f, .9f * k, .93f * k, -.1f, .1f);
        UB(Plain("opakowanie gazy (białe)", new Color(.95f, .95f, .95f), .2f), .12f, .26f, .9f * k, .95f * k, -.15f, .05f);
        UB(Plain("pojemnik na igły (żółty)", new Color(.92f, .76f, .1f), .4f), -.25f, -.15f, .565f * k, .7f * k, -.1f, .05f);
        URzecz("wózek zabiegowy", 0, .9f, 0); UKol(-.31f * k, .31f * k, 0, .95f * k, -.23f * k, .23f * k);
    }

    static void ULodowka()
    {
        float k = Ku; var biel = Plain("lodówka (biała)", new Color(.94f, .94f, .93f), .6f);
        UB(biel, -.3f * k, .3f * k, 0, .85f * k, -.32f * k, .3f * k); UB(UChrom2, .2f * k, .23f * k, .5f * k, .75f * k, .3f * k, .33f * k);
        URzecz("lodówka (leki)", 0, .5f, 0); UKol(-.3f * k, .3f * k, 0, .85f * k, -.32f * k, .32f * k);
    }

    static void USzafaLekow()
    {
        float k = Ku; var biel = Plain("szafa na leki (biała stal)", new Color(.94f, .94f, .93f), .5f, .2f);
        UB(biel, -.5f * k, .5f * k, 0, 2.0f * k, -.22f * k, .2f * k); UB(UChrom2, -.005f, .005f, .05f, 1.95f * k, .2f * k, .21f * k);
        foreach (float sx in new[] { -.06f, .06f }) UB(UChrom2, sx - .01f, sx + .01f, .9f * k, 1.1f * k, .21f * k, .24f * k);
        UB(Plain("zamek szafy na leki", new Color(.3f, .3f, .32f), .6f, .8f), -.02f, .02f, 1.15f * k, 1.2f * k, .21f * k, .23f * k);
        URzecz("szafa na leki (zamykana)", 0, 1f, 0); UKol(-.5f * k, .5f * k, 0, 2.0f * k, -.22f * k, .22f * k);
    }

    // Blat z szafkami i zlewem (dyżurka, pokój socjalny): bateria z wylewką, „BRAXHORN kran”, czajnik, ekspres przelewowy
    static void UAneks()
    {
        float k = Ku; var front = Plain("fronty szafek (szary mat)", new Color(.6f, .62f, .64f), .4f); var blat = Plain("blat laminat (dąb)", new Color(.62f, .48f, .34f), .45f);
        UB(front, -.9f * k, .9f * k, .1f, .86f * k, -.31f * k, .28f * k); UB(UCzarnyPlastik, -.9f * k, .9f * k, 0, .1f, -.31f * k, .24f * k);
        UB(blat, -.92f * k, .92f * k, .86f * k, .9f * k, -.31f * k, .31f * k);
        for (int i = 0; i < 3; i++) UB(UChrom2, (-.75f + i * .6f) * k, (-.55f + i * .6f) * k, .78f * k, .8f * k, .28f * k, .3f * k);
        UB(UInox, -.6f * k, -.15f * k, .72f * k, .9f * k, -.22f * k, .2f * k); UB(Plain("komora zlewu (cień)", new Color(.3f, .31f, .33f), .7f, .8f), -.57f * k, -.18f * k, .901f * k, .902f * k, -.19f * k, .17f * k);
        UCyl(UChrom2, -.37f * k, .9f * k, -.26f * k, .025f, .28f, 12); URura(UChrom2, -.37f * k, 1.18f * k, -.26f * k, -.37f * k, 1.18f * k, -.06f, .012f, 8);
        var kran = new GameObject("BRAXHORN kran"); kran.transform.SetParent(Group(group), false); kran.transform.position = UL(-.37f * k, 1.16f * k, -.06f); kran.transform.rotation = uQ * Quaternion.Euler(90f, 0, 0);
        UCyl(UInox, .35f * k, .9f * k, -.05f, .08f, .22f, 14); URzecz("czajnik", .35f * k, 1f, -.05f);
        UB(UCzarnyPlastik, .55f * k, .78f * k, .9f * k, 1.25f * k, -.25f, .0f); URzecz("ekspres do kawy", .66f * k, 1.1f, -.12f);
        URzecz("zlew z baterią", -.37f * k, .9f, 0); URzecz("bateria (kran)", -.37f * k, 1.1f, -.2f); URzecz("szafki", 0, .5f, 0);
        UKol(-.92f * k, .92f * k, 0, .9f * k, -.31f * k, .31f * k);
    }

    static void UStol()
    {
        float k = Ku; var blat = UDrewnoJasne;
        UB(blat, -.6f * k, .6f * k, .72f * k, .75f * k, -.4f * k, .4f * k);
        foreach (float sx in new[] { -.55f, .55f }) foreach (float sz in new[] { -.35f, .35f }) UB(Steel, sx * k - .02f, sx * k + .02f, 0, .72f * k, sz * k - .02f, sz * k + .02f);
        URzecz("stół", 0, .75f, 0); UKol(-.6f * k, .6f * k, 0, .75f * k, -.4f * k, .4f * k);
    }

    static void UBiletomat()
    {
        float k = Ku; var ob = Plain("obudowa biletomatu (granat)", new Color(.1f, .18f, .34f), .5f, .3f);
        UB(ob, -.2f, .2f, 0, 1.3f * k, -.17f, .15f); UBx(UCzarnyPlastik, new Vector3(0, 1.1f * k, .12f), new Vector3(.32f, .24f, .04f), 20f);
        UB(UCzarnyPlastik, -.06f, .06f, .9f * k, .93f * k, .15f, .17f);
        URzecz("biletomat kolejkowy", 0, 1f, 0); UKol(-.2f, .2f, 0, 1.3f * k, -.17f, .17f);
    }

    static void UDrukarka()
    {
        float k = Ku; UB(Plain("szafka pod drukarkę (biel)", new Color(.9f, .9f, .88f), .4f), -.3f, .3f, 0, .7f * k, -.27f, .27f);
        UB(USzaryPlastik, -.25f, .25f, .7f * k, .98f * k, -.22f, .22f); UB(UCzarnyPlastik, -.2f, .2f, .98f * k, 1.0f * k, -.18f, .1f);
        URzecz("drukarka", 0, .85f, 0); UKol(-.3f, .3f, 0, 1.0f * k, -.27f, .27f);
    }

    static void ULezankaWozek()
    {
        float k = Ku; var mat = Plain("materac wózka (niebieski)", new Color(.12f, .3f, .55f), .4f);
        foreach (float sx in new[] { -.28f, .28f }) foreach (float sz in new[] { -.85f, .85f }) { UB(UInox, sx * k - .015f, sx * k + .015f, .1f, .82f * k, sz * k - .015f, sz * k + .015f); UCyl(UGuma, sx * k, 0, sz * k, .06f, .1f, 10); }
        UB(UInox, -.32f * k, .32f * k, .78f * k, .82f * k, -.95f * k, .95f * k); UB(mat, -.3f * k, .3f * k, .82f * k, .9f * k, -.95f * k, .95f * k);
        UB(UInox, -.32f * k, .32f * k, .3f * k, .32f * k, -.85f * k, .85f * k);
        foreach (int sd in new[] { -1, 1 }) URura(UInox, sd * .34f * k, .95f * k, -.7f * k, sd * .34f * k, .95f * k, .7f * k, .012f, 6);
        URzecz("wózek transportowy (leżanka)", 0, .9f, 0); UKol(-.35f * k, .35f * k, 0, .95f * k, -1.0f * k, 1.0f * k);
    }

    static void USzafkaUbr()
    {
        float k = Ku; var m = Plain("szafki szatni (grafit lakier)", new Color(.12f, .125f, .13f), .55f, .2f);
        UB(m, -.2f * k, .2f * k, .1f, 1.95f * k, -.25f * k, .24f * k); UB(Steel, -.21f * k, .21f * k, 0, .1f, -.25f * k, .22f * k);
        for (int q = 0; q < 3; q++) UB(Steel, -.13f, .13f, 1.72f * k + q * .05f, 1.735f * k + q * .05f, .24f * k, .25f * k);
        UB(UChrom2, .12f * k, .14f * k, 1.0f * k, 1.15f * k, .24f * k, .27f * k);
        URzecz("szafka ubraniowa", 0, 1f, 0);
    }
    static void ULawka()
    {
        float k = Ku; var dab = Pbr("ławka szatni (dąb)", "wood_floor", 1.2f, new Color(.95f, .9f, .85f), .5f);
        UB(dab, -.9f * k, .9f * k, .42f * k, .47f * k, -.2f * k, .2f * k);
        foreach (float sx in new[] { -.75f, .75f }) UB(Steel, sx * k - .03f, sx * k + .03f, 0, .42f * k, -.17f * k, .17f * k);
        URzecz("ławka", 0, .45f, 0); UKol(-.9f * k, .9f * k, 0, .47f * k, -.2f * k, .2f * k);
        for (int i = 0; i < 3; i++) uSiedziska.Add((UL((-.6f + i * .6f) * k, 0, 0), UKier(0, 1), uP.y + .47f * k));
    }
}
