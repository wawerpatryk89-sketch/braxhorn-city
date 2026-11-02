using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// KOMISARIAT POLICJI (dzielnica zachodnia, kwartał C, narożnik ulicy głównej i S1). Bryła x −156.5 … −130.5, z −9 … 17 (26 × 26 m),
// dwie kondygnacje (parter 0.17 … 4.07, piętro 4.37 … 8.27, attyka 9.5 m). Wejście główne z ulicy głównej (x −146, z 17).
// PARTER: hol z poczekalnią i tablicą POSZUKIWANI → lada dyżurki za szybą (okienko) → korytarz służbowy: dyżurka, pokój
// przesłuchań z lustrem weneckim i pokój obserwacji, areszt (3 cele z kratami, korytarz z ławką i ścianą do zdjęć), zbrojownia
// (zamknięta — stalowe drzwi z zamkiem szyfrowym), szatnia z szafkami i tylnym wyjściem na parking; WC przy holu; klatka schodowa.
// PIĘTRO: wydział kryminalny (6 biurek z komputerami, tablica śledztwa), gabinet komendanta, pokój socjalny z aneksem,
// sala odpraw, WC. Parking radiowozów za budynkiem (z −46 … −12) ze szlabanem od ulicy S1, radiowozy z kodu (bez cudzych modeli).
// POD MISJE: AKCJA na dyżurce (zgłoszenie), w areszcie (zatrzymanie), przy radiowozie (start pościgu).
public static partial class BraxClaudeOsiedle
{
    const float KomX0 = -156.5f, KomX1 = -130.5f, KomZ0 = -9f, KomZ1 = 17f, KomT = .35f;
    const float KomFY = .17f, KomCY = 4.07f, KomFY2 = 4.37f, KomCY2 = 8.27f, KomDach = 8.6f, KomAttyka = 9.5f;
    static float KomIX0 => KomX0 + KomT; static float KomIX1 => KomX1 - KomT; static float KomIZ0 => KomZ0 + KomT; static float KomIZ1 => KomZ1 - KomT;
    // podziały (osie ścian działowych)
    const float KomHolZ = 10.5f, KomKorN = 5.0f, KomKorS = 2.5f, KomKlatkaX = -135.5f, KomWcZ = 11.6f, KomCelaZ = -3.0f;
    const float KomPrzX = -152.0f, KomObsX = -149.0f, KomAresztX = -142.0f, KomZbrX = -136.0f, KomSchodyZ = 5.8f;
    static readonly List<(string rola, string gdzie, Vector3 p, float yaw, bool siedzi)> dzlObsada = new List<(string, string, Vector3, float, bool)>();

    static Material KomElewacja => Pbr("dzl komisariat tynk jasny", "white_stucco_02", 2.5f, new Color(.95f, .95f, .94f));
    static Material KomCokol => Pbr("dzl komisariat cokół granit grafit", "rough_block_wall", 2.2f, new Color(.30f, .30f, .32f));
    static Material KomGranat => Plain("dzl komisariat pas granatowy (blacha)", new Color(.05f, .09f, .22f), .45f, .3f);
    static Material KomFarbaWew => Pbr("dzl ściany wnętrz farba jasna", "painted_plaster_wall", 2.2f, new Color(.93f, .93f, .91f));
    static Material KomLamperia => Plain("dzl lamperia farba olejna szaroniebieska", new Color(.62f, .68f, .74f), .35f);
    static Material KomGres => Pbr("dzl posadzka gres szary", "large_grey_tiles", 2.4f, new Color(.82f, .83f, .84f), .5f);
    static Material KomBetonPos => Pbr("dzl posadzka beton zatarty (areszt)", "concrete_pavement_02", 3.2f, new Color(.62f, .62f, .61f));
    static Material KomWykladzina => Plain("dzl wykładzina dywanowa grafitowa (biuro)", new Color(.22f, .24f, .27f), .05f);
    static Material KomParkiet => Pbr("dzl parkiet dębowy (gabinet)", "wood_floor", 1.8f, new Color(.90f, .84f, .78f), .45f);

    static UWyk? KomWyk(Vector3 p)
    {
        // WC: płytki do 2.1 m × k; areszt: lamperia; reszta: farba + cokół
        bool wc = p.x > KomKlatkaX && p.z > KomWcZ;
        if (wc) return new UWyk { gora = KomFarbaWew, dol = UPlytkiSciana, cokol = null, hDol = 2.1f * K };
        bool areszt = p.x < KomAresztX && p.z < KomKorS && p.y < KomCY;
        if (areszt) return new UWyk { gora = KomFarbaWew, dol = KomLamperia, cokol = Frame, hDol = 1.5f * K };
        return new UWyk { gora = KomFarbaWew, dol = null, cokol = Frame, hDol = 0f };
    }

    static void DzlKomisariat()
    {
        offDesk = Model("metal_office_desk"); offChair = Model("dining_chair_02"); offArm = Model("modern_arm_chair_01"); offSofa = Model("sofa_02");
        offLamp = Model("modern_ceiling_lamp_01"); offShelf = Model("drawer_cabinet"); offPads = Model("office_notepads"); offBoard = Model("standing_chalkboard_01");
        group = "Dzielnica / komisariat / bryła";
        string gWew = "Dzielnica / komisariat / wnętrze — ściany"; dzlWnetrza.Add(gWew);
        float ix0 = KomIX0, ix1 = KomIX1, iz0 = KomIZ0, iz1 = KomIZ1;
        float g1 = KomFY + .9f, g2 = KomFY + 3.1f, u1 = KomFY2 + .9f, u2 = KomFY2 + 3.1f;
        // fundament / płyta parteru (kolizja) i strop
        BoxMinMax(KomBetonPos, KomX0, KomX1, -.3f, KomFY - .01f, KomZ0, KomZ1, true);
        // ściany zewnętrzne: parter (do wierzchu stropu) i piętro (z attyką)
        var frontP = new List<DzlOtwor> { new DzlOtwor(-155.4f, -151.0f, g1, g2, 0), new DzlOtwor(-147.3f, -144.7f, KomFY, KomFY + 2.78f, 1), new DzlOtwor(-142.0f, -137.6f, g1, g2, 0), new DzlOtwor(-133.9f, -132.7f, KomFY + 1.7f, KomFY + 2.6f, 3) };
        var frontU = new List<DzlOtwor>(); foreach (float x in new[] { -155.4f, -152.0f, -148.6f, -145.2f, -141.8f, -138.4f }) frontU.Add(new DzlOtwor(x, x + 2.2f, u1, u2, 0)); frontU.Add(new DzlOtwor(-133.9f, -132.7f, KomFY2 + 1.7f, KomFY2 + 2.6f, 3));
        DzlScianaZew(true, KomZ1 - KomT * .5f, KomX0, KomX1, -.2f, KomFY2, KomT, 1, KomFarbaWew, KomElewacja, KomCokol, .9f, frontP, gWew);
        DzlScianaZew(true, KomZ1 - KomT * .5f, KomX0, KomX1, KomFY2, KomAttyka, KomT, 1, KomFarbaWew, KomElewacja, null, 0f, frontU, gWew);
        var tylP = new List<DzlOtwor>(); foreach (float x in new[] { -154.2f, -149.5f, -144.8f }) tylP.Add(new DzlOtwor(x - .45f, x + .45f, KomFY + 2.5f, KomFY + 3.25f, 2)); tylP.Add(new DzlOtwor(-134.0f, -132.75f, KomFY, KomFY + 2.78f, 1));
        var tylU = new List<DzlOtwor>(); foreach (float x in new[] { -155.0f, -151.0f, -144.2f, -137.4f, -134.0f }) tylU.Add(new DzlOtwor(x, x + 2.4f, u1, u2, 0));
        DzlScianaZew(true, KomZ0 + KomT * .5f, KomX0, KomX1, -.2f, KomFY2, KomT, -1, KomFarbaWew, KomElewacja, KomCokol, .9f, tylP, gWew);
        DzlScianaZew(true, KomZ0 + KomT * .5f, KomX0, KomX1, KomFY2, KomAttyka, KomT, -1, KomFarbaWew, KomElewacja, null, 0f, tylU, gWew);
        var wschP = new List<DzlOtwor> { new DzlOtwor(-6.0f, -3.0f, KomFY + 1.9f, KomFY + 3.0f, 3), new DzlOtwor(3.0f, 4.5f, g1, g2, 0), new DzlOtwor(7.0f, 9.6f, g1, g2, 0) };
        var wschU = new List<DzlOtwor> { new DzlOtwor(-7.5f, -5.1f, u1, u2, 0), new DzlOtwor(-4.1f, -1.7f, u1, u2, 0), new DzlOtwor(3.0f, 4.5f, u1, u2, 0), new DzlOtwor(7.0f, 9.6f, u1, u2, 0) };
        DzlScianaZew(false, KomX1 - KomT * .5f, KomZ0, KomZ1, -.2f, KomFY2, KomT, 1, KomFarbaWew, KomElewacja, KomCokol, .9f, wschP, gWew);
        DzlScianaZew(false, KomX1 - KomT * .5f, KomZ0, KomZ1, KomFY2, KomAttyka, KomT, 1, KomFarbaWew, KomElewacja, null, 0f, wschU, gWew);
        var zachP = new List<DzlOtwor> { new DzlOtwor(3.0f, 4.5f, g1, g2, 0), new DzlOtwor(11.6f, 15.4f, g1, g2, 0) };
        var zachU = new List<DzlOtwor> { new DzlOtwor(-7.0f, -4.6f, u1, u2, 0), new DzlOtwor(-3.6f, -1.2f, u1, u2, 0), new DzlOtwor(3.0f, 4.5f, u1, u2, 0), new DzlOtwor(6.0f, 8.4f, u1, u2, 0), new DzlOtwor(9.4f, 11.8f, u1, u2, 0), new DzlOtwor(12.8f, 15.2f, u1, u2, 0) };
        DzlScianaZew(false, KomX0 + KomT * .5f, KomZ0, KomZ1, -.2f, KomFY2, KomT, -1, KomFarbaWew, KomElewacja, KomCokol, .9f, zachP, gWew);
        DzlScianaZew(false, KomX0 + KomT * .5f, KomZ0, KomZ1, KomFY2, KomAttyka, KomT, -1, KomFarbaWew, KomElewacja, null, 0f, zachU, gWew);

        // pas granatowy na wysokości stropu (cała elewacja), gzyms attyki, obróbka blacharska, dach z żwirem
        group = "Dzielnica / komisariat / bryła";
        float b0 = KomCY - .05f, b1 = KomFY2 + .3f;
        BoxN(KomGranat, KomX0 - .04f, KomX1 + .04f, b0, b1, KomZ1, KomZ1 + .04f); BoxN(KomGranat, KomX0 - .04f, KomX1 + .04f, b0, b1, KomZ0 - .04f, KomZ0);
        BoxN(KomGranat, KomX1, KomX1 + .04f, b0, b1, KomZ0, KomZ1); BoxN(KomGranat, KomX0 - .04f, KomX0, b0, b1, KomZ0, KomZ1);
        BoxN(MatAlu, KomX0 - .08f, KomX1 + .08f, KomAttyka, KomAttyka + .06f, KomZ0 - .08f, KomZ1 + .08f);
        BoxMinMax(KomBetonPos, KomIX0, KomIX1, KomCY2, KomDach, KomIZ0, KomIZ1, true, true);
        Top(Gravel, KomIX0, KomIX1, KomIZ0, KomIZ1, KomDach + .01f);
        // urządzenia na dachu: klimatyzatory, wywiewki, wyłaz
        foreach (var (x, z) in new[] { (-152f, -4f), (-148f, -4f), (-138f, 8f) }) { BoxMinMax(MatAlu, x - .8f, x + .8f, KomDach, KomDach + 1.1f, z - .6f, z + .6f, true); BoxN(Frame, x - .5f, x + .5f, KomDach + 1.1f, KomDach + 1.12f, z - .4f, z + .4f); }
        BoxMinMax(MatAlu, -134.5f, -133f, KomDach, KomDach + .7f, 13f, 14.5f, true);

        // strop między kondygnacjami (bez otworu klatki schodowej nad biegami)
        group = "Dzielnica / komisariat / stropy";
        BoxMinMax(KomBetonPos, ix0, KomKlatkaX, KomCY, KomFY2, iz0, iz1, true, true);
        BoxMinMax(KomBetonPos, KomKlatkaX, ix1, KomCY, KomFY2, iz0, KomKorN + .8f, true, true);
        BoxMinMax(KomBetonPos, KomKlatkaX, ix1, KomCY, KomFY2, KomWcZ, iz1, true, true);

        // wejście: daszek szklany na wspornikach, litery POLICJA i gwiazda na pasie granatowym, tablica urzędowa, flagi
        group = "Dzielnica / komisariat / wejście";
        float dx = -146f, fz = KomZ1;
        BoxN(Frame, dx - 3.2f, dx + 3.2f, KomFY + 3.1f, KomFY + 3.28f, fz, fz + 1.9f);
        BoxN(DzlSzybaOkna, dx - 3.1f, dx + 3.1f, KomFY + 3.29f, KomFY + 3.31f, fz + .05f, fz + 1.85f);
        foreach (float sx in new[] { -2.9f, 2.9f }) RuraN(Frame, new Vector3(dx + sx, KomFY + 4.0f, fz), new Vector3(dx + sx, KomFY + 3.3f, fz + 1.75f), .03f, 8);
        Top(KomCokol, dx - 2.2f, dx + 2.2f, fz, fz + 1.6f, DzlYC + .015f);   // wycieraczka z granitu przy progu
        var biale = MatLicoBiale; var bokiS = MatBokiStal;
        Napis3D("POLICJA", new Vector3(dx + .5f, KomCY + .02f, fz + .04f), Vector3.forward, .42f, .06f, biale, bokiS);
        Gwiazda(new Vector3(dx - 3.3f, KomCY + .23f, fz + .045f), Vector3.forward, .3f, .04f, Plain("dzl odznaka złota", new Color(.80f, .64f, .26f), .7f, .9f), bokiS);
        // tablica urzędowa przy drzwiach (godło + KOMISARIAT POLICJI)
        BoxN(Plain("dzl tablica urzędowa czerwona", new Color(.62f, .06f, .06f), .45f), dx + 1.75f, dx + 2.75f, KomFY + 1.3f, KomFY + 2.05f, fz, fz + .02f);
        Napis3D("KOMISARIAT", new Vector3(dx + 2.25f, KomFY + 1.58f, fz + .02f), Vector3.forward, .08f, .006f, biale, biale);
        Napis3D("POLICJI", new Vector3(dx + 2.25f, KomFY + 1.42f, fz + .02f), Vector3.forward, .08f, .006f, biale, biale);
        Krazek(new Vector3(dx + 2.25f, KomFY + 1.87f, fz + .02f), Vector3.forward, .1f, .008f, biale, biale);
        // drzwi wejściowe (2 skrzydła szklane) i tylne (stalowe, na parking)
        DzlDrzwi("KOM_WEJSCIE_L", "Komisariat — wejście", new Vector3(dx - .65f, KomFY, KomZ1 - KomT * .5f), true, 1.25f, 2.7f, true, false);
        DzlDrzwi("KOM_WEJSCIE_P", "Komisariat — wejście", new Vector3(dx + .65f, KomFY, KomZ1 - KomT * .5f), true, 1.25f, 2.7f, true, true);
        DzlDrzwi("KOM_TYL", "Komisariat — wyjście na parking", new Vector3(-133.375f, KomFY, KomZ0 + KomT * .5f), true, 1.2f, 2.7f, false, true, false, Plain("dzl drzwi stalowe szare", new Color(.42f, .44f, .46f), .4f, .5f));
        dzlBryly.Add((Rect.MinMaxRect(KomX0, KomZ0, KomX1, KomZ1), KomAttyka, 0));

        DzlGuard("komisariat — parking i szlaban", DzlKomisariatParking);
        DzlNota("komisariat: bryła " + KomX0 + "…" + KomX1 + " × " + KomZ0 + "…" + KomZ1 + ", 2 kondygnacje, attyka " + KomAttyka + " m");
    }

    static void DzlKomisariatWnetrza()
    {
        float ix0 = KomIX0, ix1 = KomIX1, iz0 = KomIZ0, iz1 = KomIZ1, FY = KomFY, CY = KomCY, F2 = KomFY2, C2 = KomCY2, k = K;
        string W(string n) { var g = "Dzielnica / komisariat / wnętrze — " + n; dzlWnetrza.Add(g); return g; }
        var skrzydlo = Plain("dzl skrzydło drzwi (płyta laminowana jasny dąb)", new Color(.72f, .60f, .45f), .35f);

        // ---------- PARTER: ściany działowe z drzwiami (oś, drzwi: x, z, wzdłuż X, zawias, strona otwarcia) ----------
        var sciany = new[]
        {
            (ix0, KomHolZ, KomKlatkaX, KomHolZ),                 // hol | przesłuchania, obserwacja, dyżurka (z okienkiem)
            (KomKlatkaX, KomKorN, KomKlatkaX, iz1),              // hol | WC, klatka
            (KomKlatkaX, KomWcZ, ix1, KomWcZ),                   // WC | klatka
            (ix0, KomKorN, ix1, KomKorN),                        // korytarz — północ
            (KomPrzX, KomKorN, KomPrzX, KomHolZ),                // przesłuchania | obserwacja (lustro weneckie)
            (KomObsX, KomKorN, KomObsX, KomHolZ),                // obserwacja | dyżurka
            (ix0, KomKorS, ix1, KomKorS),                        // korytarz — południe
            (KomAresztX, iz0, KomAresztX, KomKorS),              // areszt | zbrojownia
            (KomZbrX, iz0, KomZbrX, KomKorS),                    // zbrojownia | szatnia
        };
        var drzwi = new[]
        {
            (KomKlatkaX, 14.1f, false, -1, 1),                   // hol → WC
            (-154.1f, KomKorN, true, -1, 1),                     // korytarz → przesłuchania
            (-150.5f, KomKorN, true, 1, 1),                      // korytarz → obserwacja
            (-137.4f, KomKorN, true, -1, 1),                     // korytarz → dyżurka
            (-133.2f, KomKorN, true, 1, 1),                      // korytarz → klatka schodowa
            (-149.0f, KomKorS, true, -1, -1),                    // korytarz → areszt (drzwi stalowe)
            (-133.4f, KomKorS, true, 1, -1),                     // korytarz → szatnia
            (-138.5f, KomHolZ, true, 1, 1),                      // dyżurka → hol (drzwi służbowe obok lady)
        };
        var okienka = new[] { (-143.9f, KomHolZ, true, 7.2f, FY + 1.12f, FY + 2.35f), (KomPrzX, 7.75f, false, 3.0f, FY + 1.0f, FY + 2.3f) };
        UScianyZDanych(sciany, drzwi, FY, CY, .15f, KomFarbaWew, KomWyk, skrzydlo, W("ściany parteru"), okienka);
        // zbrojownia: drzwi stalowe ZAMKNIĘTE (wpuszczone w ścianę od strony korytarza) z zamkiem szyfrowym i tablicą
        group = W("zbrojownia");
        var stal = Plain("dzl drzwi stalowe antywłamaniowe", new Color(.30f, .32f, .34f), .4f, .6f);
        BoxMinMax(stal, -139.6f, -138.4f, FY, FY + 2.65f, KomKorS + .075f, KomKorS + .11f, false);
        BoxMinMax(Frame, -139.7f, -138.3f, FY + 2.65f, FY + 2.72f, KomKorS + .075f, KomKorS + .12f, false);
        BoxMinMax(UCzarnyPlastik, -138.25f, -138.1f, FY + 1.2f, FY + 1.45f, KomKorS + .075f, KomKorS + .1f, false);
        BoxMinMax(UInox, -138.55f, -138.48f, FY + 1.0f, FY + 1.3f, KomKorS + .11f, KomKorS + .14f, false);
        BoxMinMax(Plain("dzl tablica ostrzegawcza żółta", new Color(.92f, .74f, .08f), .4f), -139.4f, -138.6f, FY + 1.75f, FY + 2.1f, KomKorS + .11f, KomKorS + .115f, false);
        Napis3D("ZBROJOWNIA", new Vector3(-139f, FY + 1.9f, KomKorS + .115f), Vector3.forward, .06f, .004f, UCzarnyPlastik, UCzarnyPlastik);
        Napis3D("WSTEP WZBRONIONY", new Vector3(-139f, FY + 1.79f, KomKorS + .115f), Vector3.forward, .035f, .003f, UCzarnyPlastik, UCzarnyPlastik);
        URzeczW("drzwi zbrojowni zamknięte (zamek szyfrowy)", new Vector3(-139f, FY + 1.2f, KomKorS + .1f));

        // ---------- posadzki, sufity, lampy (parter) ----------
        group = W("hol"); DzlPokoj(KomGres, ix0, KomKlatkaX, KomHolZ, iz1, FY, CY, 2);
        group = W("WC"); DzlPokoj(UPlytkiPodloga, KomKlatkaX, ix1, KomWcZ, iz1, FY, CY, 1, 5f);
        group = W("korytarz"); DzlPokoj(KomGres, ix0, ix1, KomKorS, KomKorN, FY, CY, 2);
        group = W("przesłuchania"); DzlPokoj(KomGres, ix0, KomPrzX, KomKorN, KomHolZ, FY, CY, 1, 5f);
        group = W("obserwacja"); Top(KomGres, KomPrzX, KomObsX, KomKorN, KomHolZ, FY); BoxMinMax(Plain("dzl sufit podwieszany biały", new Color(.93f, .93f, .91f), .1f), KomPrzX, KomObsX, CY, CY + .02f, KomKorN, KomHolZ, false, true);   // ciemno (lustro weneckie działa)
        group = W("dyżurka"); DzlPokoj(KomGres, KomObsX, KomKlatkaX, KomKorN, KomHolZ, FY, CY, 1);
        group = W("areszt"); DzlPokoj(KomBetonPos, ix0, KomAresztX, iz0, KomKorS, FY, CY, 2);
        group = W("zbrojownia"); DzlPokoj(KomBetonPos, KomAresztX, KomZbrX, iz0, KomKorS, FY, CY, 0);
        group = W("szatnia"); DzlPokoj(KomGres, KomZbrX, ix1, iz0, KomKorS, FY, CY, 1);
        group = W("klatka schodowa"); Top(KomGres, KomKlatkaX, ix1, KomKorN, KomWcZ, FY);

        // ---------- hol: lada dyżurki, poczekalnia, tablica POSZUKIWANI ----------
        group = W("hol");
        var drewno = UDrewnoCiemne;
        BoxMinMax(drewno, -147.5f, -140.3f, FY, FY + 1.08f, KomHolZ + .075f, KomHolZ + .5f, true);
        BoxMinMax(Pbr("dzl blat konglomerat", "large_grey_tiles", 1.2f, new Color(.32f, .32f, .34f), .5f), -147.6f, -140.2f, FY + 1.08f, FY + 1.12f, KomHolZ + .075f, KomHolZ + .62f, false);
        BoxMinMax(UInox, -147.5f, -140.3f, FY + .02f, FY + .1f, KomHolZ + .49f, KomHolZ + .51f, false);
        URzeczW("lada dyżurki (od holu)", new Vector3(-143.9f, FY + 1.1f, KomHolZ + .3f));
        UMebel("rzad4", ix0 + .45f, 13.8f, 90f, FY); UMebel("rzad3", ix0 + .45f, 11.6f + .1f, 90f, FY);
        UMebel("rzad4", -144f, iz1 - .45f, 180f, FY + 0f);
        UMebel("roslina", -136.2f, iz1 - .6f, 0f, FY); UMebel("dystrybutor", -136.1f, KomHolZ + .5f, 180f, FY); UKosz(new Vector3(-137.0f, 0, KomHolZ + .45f), FY);
        // tablica POSZUKIWANI (korek w ramie, zdjęcia i listy gończe — kartki z „zdjęciem”, nagłówek literami)
        DzlTablicaPoszukiwanych(new Vector3(-152.9f, FY + 1.75f, KomHolZ + .075f), Vector3.forward, 3.4f, 1.5f);
        // informacja przy wejściu (tablica z godzinami)
        BoxMinMax(Plain("dzl tablica informacyjna biała", new Color(.93f, .93f, .9f), .3f), -150.9f, -149.9f, FY + 1.3f, FY + 2.2f, iz1 - .03f, iz1, false);
        UMisja("dyżurka — zgłoszenie (AKCJA przy ladzie)", new Vector3(-143.9f, FY, KomHolZ + 1.2f), "zgłoszenie na komisariacie: rozmowa z dyżurnym, start / zakończenie zlecenia");
        dzlZadania.Add(("ZGŁOSZENIE · DYŻURNY", new Vector3(-143.9f, FY, KomHolZ + 1.2f), 1.6f, "komisariat_dyzurka"));

        // ---------- dyżurka ----------
        group = W("dyżurka");
        UMebel("biurko", -145.9f, KomHolZ - .62f, 0f, FY); UMebel("biurko", -142.2f, KomHolZ - .62f, 0f, FY);
        UMebel("krzeslo", -145.9f, KomHolZ - 1.5f, 0f, FY); UMebel("krzeslo", -142.2f, KomHolZ - 1.5f, 0f, FY);
        // ściana monitorów (CCTV) na ścianie wschodniej, radiostacja, szafy na akta, drukarka
        for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++)
            {
                float zc = 6.3f + i * 1.15f, yc = FY + 1.75f + j * .72f;
                BoxMinMax(UCzarnyPlastik, KomKlatkaX - .16f, KomKlatkaX - .08f, yc - .32f, yc + .32f, zc - .52f, zc + .52f, false);
                URamka(new Vector3(KomKlatkaX - .165f, 0, zc), 270f); UNapis(UEkran(uEkranPc), new Vector3(0, yc, 0), Vector3.forward, Vector3.up, .98f, .58f);
            }
        URzeczW("monitoring (6 ekranów)", new Vector3(KomKlatkaX - .2f, FY + 2f, 7.5f));
        BoxMinMax(UCzarnyPlastik, -140.6f, -139.6f, FY + .76f * k, FY + .76f * k + .22f, 5.6f, 6.0f, false); URzeczW("radiostacja", new Vector3(-140.1f, FY + 1f, 5.8f));
        UMebel("szafa_akt", -147.9f, KomKorN + .4f, 0f, FY); UMebel("drukarka", -139.5f, KomKorN + .45f, 0f, FY);
        BoxMinMax(Steel, -140.6f, -139.6f, FY, FY + .76f * k, 5.4f, 6.2f, true);
        dzlObsada.Add(("policja", "komisariat — dyżurny przy ladzie", new Vector3(-145.9f, FY, KomHolZ - 1.5f), 0f, true));
        dzlObsada.Add(("policja", "komisariat — drugi dyżurny (monitoring)", new Vector3(-142.2f, FY, KomHolZ - 1.5f), 0f, true));

        // ---------- przesłuchania + obserwacja (lustro weneckie: lustro tylko od strony przesłuchań) ----------
        group = W("przesłuchania");
        UMebel("stol", -154.1f, 7.75f, 90f, FY);
        UMebel("krzeslo", -155.15f, 7.75f, 90f, FY); UMebel("krzeslo", -153.0f, 7.3f, 270f, FY); UMebel("krzeslo", -153.0f, 8.2f, 270f, FY);
        Quad(ULustro, new Vector3(KomPrzX - .08f, FY + 1.0f, 6.25f), new Vector3(KomPrzX - .08f, FY + 1.0f, 9.25f), new Vector3(KomPrzX - .08f, FY + 2.3f, 9.25f), new Vector3(KomPrzX - .08f, FY + 2.3f, 6.25f), Vector3.left);
        URzeczW("lustro weneckie (od strony przesłuchań)", new Vector3(KomPrzX - .09f, FY + 1.6f, 7.75f));
        BoxMinMax(UCzarnyPlastik, ix0 + .02f, ix0 + .14f, CY - .45f, CY - .3f, KomHolZ - .25f, KomHolZ - .1f, false); URzeczW("kamera w rogu", new Vector3(ix0 + .1f, CY - .4f, KomHolZ - .2f));
        group = W("obserwacja");
        UMebel("biurko", KomObsX - .55f, 7.75f, 270f, FY); UMebel("krzeslo", KomObsX - 1.4f, 7.3f, 270f, FY); UMebel("krzeslo", KomObsX - 1.4f, 8.3f, 270f, FY);
        PointLamp(new Vector3((KomPrzX + KomObsX) * .5f, CY - .4f, 7.75f), 3.5f, .25f, new Color(1f, .92f, .8f));

        // ---------- areszt: 3 cele z kratami, korytarz z ławką i ścianą do zdjęć ----------
        group = W("areszt");
        float cw = (KomAresztX - ix0) / 3f;
        var mattress = Plain("dzl materac celi granat", new Color(.18f, .24f, .36f), .2f); var inox = UInox;
        for (int c = 0; c < 3; c++)
        {
            float x0 = ix0 + c * cw, x1 = x0 + cw, xc = (x0 + x1) * .5f;
            if (c > 0) BoxMinMax(KomFarbaWew, x0 - .075f, x0 + .075f, FY, CY, iz0, KomCelaZ, true);   // ściana między celami
            // krata od korytarza aresztu (z = KomCelaZ) z drzwiami kratowymi 1.2 × 2.65 m (uchylone o 0 — zamknięte, z zamkiem)
            for (float x = x0 + .12f; x < x1 - .06f; x += .12f) BoxMinMax(DzlKrata, x - .014f, x + .014f, FY, CY, KomCelaZ - .014f, KomCelaZ + .014f, false);
            foreach (float y in new[] { FY + 1.0f, FY + 2.1f }) BoxMinMax(DzlKrata, x0, x1, y, y + .06f, KomCelaZ - .05f, KomCelaZ + .05f, false);
            foreach (float d in new[] { -.6f, .6f }) BoxMinMax(DzlKrata, xc + d - .03f, xc + d + .03f, FY, FY + 2.68f, KomCelaZ - .06f, KomCelaZ + .06f, false);
            BoxMinMax(DzlKrata, xc - .63f, xc + .63f, FY + 2.65f, FY + 2.71f, KomCelaZ - .06f, KomCelaZ + .06f, false);
            BoxMinMax(Plain("dzl zamek celi", new Color(.55f, .56f, .58f), .55f, .8f), xc + .42f, xc + .54f, FY + 1.0f, FY + 1.25f, KomCelaZ - .1f, KomCelaZ + .1f, false);
            Collider(new Vector3(xc, CY * .5f, KomCelaZ), new Vector3(cw, CY, .12f));
            Napis3D("CELA " + (c + 1), new Vector3(xc, FY + 2.9f, KomCelaZ + .07f), Vector3.forward, .12f, .01f, MatLicoBiale, MatLicoBiale);
            // prycza przy ścianie tylnej (z iz0), sedes ze stali z umywalką w rogu, lampa
            float px0 = c % 2 == 0 ? x0 + .1f : x1 - .1f - 2.05f * k;
            BoxMinMax(Steel, px0, px0 + 2.05f * k, FY, FY + .37f * k, iz0 + .05f, iz0 + .85f * k, true);
            BoxMinMax(Plain("dzl prycza deska", new Color(.50f, .37f, .24f), .3f), px0, px0 + 2.05f * k, FY + .37f * k, FY + .45f * k, iz0 + .02f, iz0 + .88f * k, false);
            BoxMinMax(mattress, px0 + .05f, px0 + 2.0f * k, FY + .45f * k, FY + .53f * k, iz0 + .06f, iz0 + .83f * k, false);
            float tx = c % 2 == 0 ? x1 - .55f : x0 + .55f;
            Cylinder(inox, new Vector3(tx, FY, iz0 + .45f), .2f * k, .42f * k, 18, true, true);
            BoxMinMax(inox, tx - .2f, tx + .2f, FY, FY + .75f * k, iz0, iz0 + .12f, true);
            BoxMinMax(inox, tx - .2f, tx + .2f, FY + .82f * k, FY + .92f * k, iz0, iz0 + .4f, false);
            URzeczW("cela " + (c + 1) + ": prycza z materacem", new Vector3(px0 + k, FY + .5f, iz0 + .45f)); URzeczW("cela " + (c + 1) + ": sedes stalowy z umywalką", new Vector3(tx, FY + .5f, iz0 + .4f));
            PointLamp(new Vector3(xc, CY - .3f, (iz0 + KomCelaZ) * .5f), 4f, .35f, new Color(1f, .96f, .9f));
        }
        // korytarz aresztu: ławka z poręczą do kajdanek, stanowisko przyjęć (biurko), ściana do zdjęć z miarą wzrostu
        UMebel("lawka", -146f, KomKorS - .35f, 180f, FY);
        Tube(UInox, new Vector3(-147.6f, FY + .75f, KomKorS - .1f), new Vector3(-144.4f, FY + .75f, KomKorS - .1f), .02f, 6);
        UMebel("biurko", -153.3f, KomKorS - .55f, 180f, FY); UMebel("krzeslo", -153.3f, KomKorS - 1.4f, 0f, FY);
        // ściana do zdjęć: pasy co 10 cm × k na ścianie zachodniej, liczby co 50 cm
        var bialaSc = Plain("dzl ściana do zdjęć biała", new Color(.92f, .92f, .9f), .2f);
        BoxMinMax(bialaSc, ix0, ix0 + .02f, FY + .3f, FY + 2.7f, -2.6f, -.6f, false);
        for (int i = 0; i <= 24; i++) { float y = FY + .3f + i * .1f; BoxMinMax(UCzarnyPlastik, ix0 + .02f, ix0 + .025f, y, y + (i % 5 == 0 ? .012f : .006f), -2.6f, i % 5 == 0 ? -1.6f : -2.2f, false); }
        URzeczW("ściana do zdjęć zatrzymanych (miara wzrostu)", new Vector3(ix0 + .05f, FY + 1.5f, -1.6f));
        UMisja("areszt — zatrzymanie (AKCJA przy celi)", new Vector3(-149.5f, FY, -1.5f), "doprowadzenie zatrzymanego do celi / przesłuchanie");
        dzlZadania.Add(("ZATRZYMANIE · CELA", new Vector3(-149.5f, FY, -1.5f), 1.8f, "komisariat_areszt"));
        dzlObsada.Add(("policja", "komisariat — strażnik aresztu", new Vector3(-153.3f, FY, KomKorS - 1.4f), 0f, true));

        // ---------- zbrojownia (zamknięta): stalowe szafy na broń ----------
        group = W("zbrojownia");
        for (int i = 0; i < 4; i++) UMebel("szafa_akt", KomAresztX + .7f, -7.4f + i * 1.25f, 90f, FY);
        BoxMinMax(Steel, KomZbrX - .6f, KomZbrX - .1f, FY, FY + 1.9f * k, -7.8f, -3.5f, true); URzeczW("stojak na broń długą (zamknięty)", new Vector3(KomZbrX - .4f, FY + 1.2f, -5.6f));

        // ---------- szatnia ----------
        group = W("szatnia");
        for (int i = 0; i < 9; i++) UMebel("szafka_ubr", KomZbrX + .35f, iz0 + 1.6f + i * .54f * k, 90f, FY);
        for (int i = 0; i < 5; i++) UMebel("szafka_ubr", ix1 - .35f, -1.2f + -i * .54f * k + -.8f, 270f, FY);
        UMebel("lawka", -133.6f, -1.5f, 90f, FY);
        ULustroScienne(new Vector3(-133.4f, 0, KomKorS - .075f), 180f, FY + 1.0f, .8f, 1.0f);
        dzlObsada.Add(("policja", "komisariat — funkcjonariusz w szatni", new Vector3(-133.2f, FY, -3.5f), 270f, false));

        // ---------- WC przy holu ----------
        group = W("WC");
        USedes(new Vector3(ix1, 0, 13.2f), 270f, FY);
        UUmywalka(new Vector3(-133.2f, 0, KomWcZ + .075f), 0f, FY, true, true);
        USuszarka(new Vector3(-134.6f, 0, KomWcZ + .075f), 0f, FY + 1.2f); UKosz(new Vector3(-135.0f, 0, KomWcZ + .5f), FY);
        UWentylacja(new Vector3(ix1, 0, 15.6f), 270f, CY - .4f);

        // ---------- klatka schodowa ----------
        group = W("klatka schodowa");
        float xm = -133.35f;
        DzlSchodyU(xm + .08f, ix1 - .02f, KomKlatkaX + .08f, xm - .08f, KomSchodyZ, 9.4f, KomWcZ - .08f, FY, F2, KomGres, KomFarbaWew);
        DzlBalustrada(new Vector3(xm + .1f, 0, KomSchodyZ), new Vector3(ix1 - .05f, 0, KomSchodyZ), F2);
        DzlBalustrada(new Vector3(KomKlatkaX + .1f, 0, KomWcZ - .12f), new Vector3(ix1 - .05f, 0, KomWcZ - .12f), F2);
        PointLamp(new Vector3(-133.4f, F2 - .5f, 8.5f), 6f, .7f, new Color(1f, .96f, .9f));

        // ---------- PIĘTRO ----------
        var scianyU = new[]
        {
            (ix0, KomKorN, KomKlatkaX, KomKorN),                 // biuro | korytarz
            (KomKlatkaX, KomKorN + .8f, KomKlatkaX, iz1),         // biuro | klatka, WC
            (KomKlatkaX, KomWcZ, ix1, KomWcZ),                    // WC | pustka klatki
            (ix0, KomKorS, ix1, KomKorS),                         // korytarz — południe
            (-146.5f, iz0, -146.5f, KomKorS),                     // komendant | socjalny
            (-138.5f, iz0, -138.5f, KomKorS),                     // socjalny | odprawy
        };
        var drzwiU = new[]
        {
            (-146.0f, KomKorN, true, -1, 1), (-140.0f, KomKorN, true, 1, 1),       // korytarz → biuro (2 wejścia)
            (KomKlatkaX, 14.1f, false, 1, 1),                                        // biuro → WC
            (-148.5f, KomKorS, true, -1, -1), (-142.5f, KomKorS, true, 1, -1), (-134.5f, KomKorS, true, 1, -1),   // komendant, socjalny, odprawy
        };
        UScianyZDanych(scianyU, drzwiU, F2, C2, .15f, KomFarbaWew, KomWyk, skrzydlo, W("ściany piętra"));
        group = W("biuro"); DzlPokoj(KomWykladzina, ix0, KomKlatkaX, KomKorN, iz1, F2, C2, 3);
        group = W("korytarz piętra"); DzlPokoj(KomGres, ix0, ix1, KomKorS, KomKorN + .8f, F2, C2, 2);
        Top(KomGres, KomKlatkaX, ix1, KomKorN, KomKorN + .8f, F2);
        group = W("gabinet komendanta"); DzlPokoj(KomParkiet, ix0, -146.5f, iz0, KomKorS, F2, C2, 1);
        group = W("pokój socjalny"); DzlPokoj(KomGres, -146.5f, -138.5f, iz0, KomKorS, F2, C2, 1);
        group = W("sala odpraw"); DzlPokoj(KomWykladzina, -138.5f, ix1, iz0, KomKorS, F2, C2, 1);
        group = W("WC piętra"); DzlPokoj(UPlytkiPodloga, KomKlatkaX, ix1, KomWcZ, iz1, F2, C2, 1, 5f);
        // biuro wydziału kryminalnego: 2 rzędy po 3 biurka (siedzący przodem do siebie), tablica śledztwa, szafy, drukarka
        group = W("biuro");
        foreach (float x in new[] { -153.2f, -148.6f, -144.0f })
        {
            UMebel("biurko", x, 9.6f, 180f, F2); UMebel("krzeslo", x, 10.45f, 180f, F2);
            UMebel("biurko", x, 12.6f, 0f, F2); UMebel("krzeslo", x, 11.75f, 0f, F2);
        }
        dzlObsada.Add(("policja", "komisariat — policjant przy biurku (wydział)", new Vector3(-148.6f, F2, 11.75f), 0f, true));
        dzlObsada.Add(("policja", "komisariat — policjantka przy biurku (wydział)", new Vector3(-144.0f, F2, 10.45f), 180f, true));
        DzlTablicaPoszukiwanych(new Vector3(-138.5f, F2 + 1.7f, KomKorN + .075f), Vector3.forward, 3.4f, 1.5f, "SPRAWA 14/26");
        for (int i = 0; i < 3; i++) UMebel("szafa_akt", ix0 + .35f, 6.2f + i * 1.15f, 90f, F2);
        UMebel("drukarka", -137.2f, iz1 - .45f, 180f, F2); UMebel("dystrybutor", -136.1f, 12.5f, 270f, F2); UMebel("roslina", ix0 + .6f, iz1 - .6f, 0f, F2);
        // gabinet komendanta
        group = W("gabinet komendanta");
        UMebel("biurko", -151.5f, -4.5f, 0f, F2);
        if (offArm) Put(offArm, new Vector3(-151.5f, F2, -5.45f), 0f, .03f, true);
        UMebel("krzeslo", -152.2f, -3.2f, 180f, F2); UMebel("krzeslo", -150.8f, -3.2f, 180f, F2);
        UMebel("szafa_akt", ix0 + .35f, -1.0f, 90f, F2); UMebel("szafa_akt", ix0 + .35f, .2f, 90f, F2); UMebel("roslina", -147.2f, iz0 + .6f, 0f, F2);
        // flaga Polski na stojaku przy biurku
        Cylinder(UInox, new Vector3(-154.6f, F2, -7.6f), .02f, 2.3f * k, 8, true, false);
        BoxMinMax(Plain("dzl flaga biel", new Color(.95f, .95f, .95f), .1f), -154.58f, -154.56f, F2 + 2.3f * k - .5f, F2 + 2.3f * k - .25f, -7.6f, -6.8f, false);
        BoxMinMax(Plain("dzl flaga czerwień", new Color(.78f, .06f, .12f), .1f), -154.58f, -154.56f, F2 + 2.3f * k - .75f, F2 + 2.3f * k - .5f, -7.6f, -6.8f, false);
        dzlObsada.Add(("policja", "komisariat — komendant za biurkiem", new Vector3(-151.5f, F2, -5.45f), 0f, true));
        // pokój socjalny
        group = W("pokój socjalny");
        UMebel("aneks", -142.5f, iz0 + .35f, 0f, F2); UMebel("lodowka", -138.95f, iz0 + .35f, 0f, F2);
        UMebel("stol", -142.5f, -2.8f, 0f, F2);
        foreach (var (x, z, y) in new[] { (-143.1f, -3.6f, 0f), (-141.9f, -3.6f, 0f), (-143.1f, -2.0f, 180f), (-141.9f, -2.0f, 180f) }) UMebel("krzeslo", x, z, y, F2);
        UMebel("automat_napoje", -145.9f, -1.0f, 90f, F2);
        // sala odpraw: długi stół, 8 krzeseł, tablica
        group = W("sala odpraw");
        UMebel("stol", -134.7f, -4.6f, 90f, F2); UMebel("stol", -134.7f, -2.0f, 90f, F2);
        foreach (float z in new[] { -5.5f, -4.2f, -2.9f, -1.6f }) { UMebel("krzeslo", -135.6f, z, 90f, F2); UMebel("krzeslo", -133.8f, z, 270f, F2); }
        if (offBoard) Put(offBoard, new Vector3(-137.6f, F2, -7.6f), 45f, .03f, true);
        // WC piętra
        group = W("WC piętra");
        USedes(new Vector3(ix1, 0, 13.2f), 270f, F2); UUmywalka(new Vector3(-133.2f, 0, KomWcZ + .075f), 0f, F2, true, true);
        USuszarka(new Vector3(-134.6f, 0, KomWcZ + .075f), 0f, F2 + 1.2f); UKosz(new Vector3(-135.0f, 0, KomWcZ + .5f), F2);
        DzlNota("komisariat: wnętrza parteru i piętra, drzwi prawdziwe " + dzlDrzwiN + ", obsada " + dzlObsada.Count);
    }

    // tablica korkowa w ramie: nagłówek literami, kartki „listów gończych” (zdjęcie + linie tekstu), pinezki
    static void DzlTablicaPoszukiwanych(Vector3 c, Vector3 n, float w, float h, string tytul = "POSZUKIWANI")
    {
        var r = Prawo(n);
        var korek = Plain("dzl korek tablicy", new Color(.62f, .45f, .28f), .1f);
        OBoxN(Frame, c + n * .015f, r, Vector3.up, n, w + .08f, h + .08f, .03f);
        OBoxN(korek, c + n * .032f, r, Vector3.up, n, w, h, .006f);
        Napis3D(tytul, c + n * .036f + Vector3.up * (h * .5f - .17f), n, .1f, .006f, Plain("dzl napis czerwony", new Color(.70f, .06f, .06f), .3f), UCzarnyPlastik);
        var papier = UPapier; var foto = Plain("dzl zdjęcie (szaro-beżowe)", new Color(.45f, .42f, .40f), .3f); var tekst = UCzarnyPlastik;
        var pinezka = Plain("dzl pinezka czerwona", new Color(.75f, .08f, .08f), .5f);
        int cols = Mathf.Max(3, Mathf.FloorToInt(w / .5f)), rows = 2; float cw2 = .36f, ch = .5f;
        for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++)
            {
                float u = -w * .5f + (i + .5f) * w / cols + ((i + j) % 2 == 0 ? .02f : -.02f), v = h * .5f - .4f - j * (ch + .08f) - ch * .5f;
                var p = c + n * .037f + r * u + Vector3.up * v;
                OBoxN(papier, p, r, Vector3.up, n, cw2, ch, .002f);
                OBoxN(foto, p + n * .002f + Vector3.up * .07f, r, Vector3.up, n, cw2 * .7f, ch * .45f, .001f);
                for (int l = 0; l < 3; l++) OBoxN(tekst, p + n * .002f - Vector3.up * (.13f + l * .045f), r, Vector3.up, n, cw2 * (l == 0 ? .75f : .6f), .012f, .001f);
                Cylinder(pinezka, p + n * .003f + Vector3.up * (ch * .5f - .025f), .008f, .012f, 6, true, false);
            }
        URzeczW("tablica " + tytul.ToLowerInvariant() + " (listy gończe)", c + n * .04f);
    }

    // ---------------- parking radiowozów + szlaban ----------------
    static void DzlKomisariatParking()
    {
        group = "Dzielnica / komisariat / parking"; bezCieni.Add(group);
        float x0 = KomX0, x1 = KomX1 + 0f, z0 = -46f, z1 = KomZ0;
        BoxMinMax(DzlBeton, x0, x1, -.2f, DzlYC - .01f, z0, z1, true);
        Top(DzlAsfalt, x0, x1, z0, z1, DzlYC);
        // miejsca parkingowe prostopadłe (2.5 × 5 m × k) wzdłuż zachodniej i południowej krawędzi, linie
        float bw = 2.5f * K, bd = 5f * K; int n = 0;
        for (float z = z0 + .5f; z + bw <= z1 - 6f; z += bw)
        {
            Top(DzlFarba, x0 + .3f, x0 + .3f + bd, z, z + .12f, DzlYC + .004f);
            var srodek = new Vector3(x0 + .3f + bd * .5f, DzlYC, z + bw * .5f);
            if (z + bw * 2 <= z1 - 6f || true) MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = srodek, przod = Vector3.right, rozmiar = new Vector2(bw, bd), przyScianie = true });
            n++;
        }
        for (float x = x0 + bd + 3.5f; x + bw <= x1 - 7f; x += bw)
        {
            Top(DzlFarba, x, x + .12f, z0 + .3f, z0 + .3f + bd, DzlYC + .004f);
            MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = new Vector3(x + bw * .5f, DzlYC, z0 + .3f + bd * .5f), przod = Vector3.forward, rozmiar = new Vector2(bw, bd), przyScianie = true });
            n++;
        }
        // radiowozy z kodu (sedan oznakowany POLICJA) na części miejsc, przodem do alejki
        int auta = 0;
        foreach (var m in MiejscaParkingowe.Where(m => m.srodek.x > KomX0 - 1f && m.srodek.x < KomX1 + 1f && m.srodek.z < KomZ0 && m.srodek.z > -47f).ToList())
        {
            int i = MiejscaParkingowe.IndexOf(m);
            if (i % 3 == 2) continue;
            float yaw = Mathf.Atan2(m.przod.x, m.przod.z) * Mathf.Rad2Deg;
            DzlAuto(m.srodek, yaw, i % 5 == 0 ? 1 : 0); auta++;
        }
        // ogrodzenie panelowe (zachód i południe) i brama ze szlabanem od ulicy S1 (wjazd z = −30 … −24)
        group = "Dzielnica / komisariat / ogrodzenie";
        var panel = Plain("dzl ogrodzenie panelowe grafit", new Color(.15f, .16f, .17f), .45f, .5f);
        void Plot(Vector3 a, Vector3 b)
        {
            var d = b - a; float L = d.magnitude; d /= L; float h = 1.6f * K;
            for (float s = 0; s <= L + .01f; s += 2.5f) { var p = a + d * Mathf.Min(s, L); BoxMinMax(panel, p.x - .03f, p.x + .03f, DzlYC, DzlYC + h + .1f, p.z - .03f, p.z + .03f, false); }
            int pr = Mathf.RoundToInt(L / .2f);
            for (int i = 0; i <= pr; i++) { var p = a + d * (L * i / pr); BoxMinMax(panel, p.x - .006f, p.x + .006f, DzlYC + .05f, DzlYC + h, p.z - .006f, p.z + .006f, false); }
            foreach (float y in new[] { .1f, h * .5f, h - .05f }) { var c0 = a + Vector3.up * (DzlYC + y); var c1 = b + Vector3.up * (DzlYC + y); Tube(panel, c0, c1, .02f, 5); }
            var g = new GameObject("ogrodzenie kolizja"); g.transform.SetParent(colliders, false); g.transform.position = (a + b) * .5f + Vector3.up * (DzlYC + h * .5f);
            g.transform.rotation = Quaternion.LookRotation(d, Vector3.up); g.AddComponent<BoxCollider>().size = new Vector3(.08f, h, L);
        }
        Plot(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1));
        Plot(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0));
        Plot(new Vector3(x1, 0, z0), new Vector3(x1, 0, -30.5f));
        Plot(new Vector3(x1, 0, -23.5f), new Vector3(x1, 0, z1));
        DzlSzlaban(new Vector3(x1 + .3f, DzlYC, -30.2f), -23.8f);
        UMisja("parking radiowozów — start pościgu (AKCJA przy radiowozie)", new Vector3(x0 + 7.5f, DzlYC, -28f), "wsiadasz do radiowozu / wyjazd na sygnale — pościg ulicami dzielnicy");
        dzlZadania.Add(("POŚCIG · RADIOWÓZ", new Vector3(x0 + 7.5f, DzlYC, -28f), 2.2f, "komisariat_parking"));
        DzlNota("komisariat: parking " + n + " miejsc, radiowozy " + auta + ", szlaban od S1 (z −30.5…−23.5)");
    }
    static readonly List<Rect> dzlWjazdy = new List<Rect>();
    static readonly List<(string napis, Vector3 p, float r, string id)> dzlZadania = new List<(string, Vector3, float, string)>();

    // szlaban: słupek z napędem, ramię biało-czerwone na zawiasie (BraxSzlaban — podnosi się, gdy gracz podejdzie), znak STOP
    static void DzlSzlaban(Vector3 slupek, float zKoniec)
    {
        group = "Dzielnica / komisariat / szlaban";
        float k = K;
        BoxMinMax(Plain("dzl szlaban obudowa żółta", new Color(.85f, .66f, .08f), .45f, .2f), slupek.x - .2f * k, slupek.x + .2f * k, slupek.y, slupek.y + 1.05f * k, slupek.z - .25f * k, slupek.z + .25f * k, true);
        var os = new GameObject("BRAXHORN szlaban komisariatu"); os.transform.SetParent(Group("Dzielnica / komisariat / szlaban"), false); os.transform.position = slupek;
        var ramie = new GameObject("ramię szlabanu").transform; ramie.SetParent(os.transform, false); ramie.localPosition = new Vector3(0, .95f * k, 0);
        // ramię wzdłuż +z (lokalnie +x po obrocie): obrót obiektu os tak, żeby lokalne x wskazywało +z świata
        os.transform.rotation = Quaternion.Euler(0, -90f, 0);
        float L = Mathf.Abs(zKoniec - slupek.z) - .3f;
        string gr = "Dzielnica / komisariat / szlaban — ramię"; group = gr;
        var b = Plain("dzl ramię szlabanu biel", new Color(.94f, .94f, .92f), .5f); var c = Plain("dzl ramię szlabanu czerwień", new Color(.78f, .08f, .06f), .5f);
        var p0 = ramie.position;
        for (int i = 0; i < 8; i++) { float s0 = .25f + i * L / 8f, s1 = .25f + (i + 1) * L / 8f; BoxMinMax(i % 2 == 0 ? c : b, p0.x - .05f, p0.x + .05f, p0.y - .06f, p0.y + .06f, p0.z + s0, p0.z + s1, false); }
        group = "Dzielnica / komisariat / szlaban";
        dzlDrzwi.Add(new DzlDrzwiRek { grupa = gr, zawias = ramie });
        var bc = ramie.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(L * .5f + .25f, 0, 0); bc.size = new Vector3(L, .3f, .3f);
        var sz = os.AddComponent<BraxSzlaban>(); sz.ramie = ramie; sz.blokada = bc; sz.strefa = slupek + new Vector3(0, 0, (zKoniec - slupek.z) * .5f); sz.miasto = "BraxhornCity";
    }

    // ---------------- auto z kodu (licencja: tylko nasz kod) ----------------
    // Sedan 4.7 × 1.8 × 1.45 m × k: podwozie, karoseria z maską i bagażnikiem, kabina z szybami, koła z felgami, lusterka,
    // światła; wersja 1/0: radiowóz (biało-granatowy, napis POLICJA na drzwiach, belka świetlna na dachu, „997” — numer alarmowy
    // nie: „POLICJA” i numer boczny). Siatki trafiają do grupy aut (łączone).
    static void DzlAuto(Vector3 c, float yaw, int wariant, Color? lakier = null, bool policja = true)
    {
        string g0 = group; group = "Dzielnica / auta (z kodu)";
        float k = K; var q = Quaternion.Euler(0, yaw, 0); Vector3 F = q * Vector3.forward, R = q * Vector3.right;
        Vector3 P(float x, float y, float z) => c + R * x + Vector3.up * y + F * z;
        float L = 4.7f * k, W = 1.8f * k, H = 1.45f * k;
        var biel = Plain("dzl lakier biały (auto)", new Color(.93f, .93f, .92f), .85f, .3f);
        var granat = Plain("dzl lakier granatowy (radiowóz)", new Color(.05f, .09f, .25f), .85f, .3f);
        var lak = policja ? biel : Plain("dzl lakier " + ColorUtility.ToHtmlStringRGB(lakier ?? Color.gray), lakier ?? Color.gray, .85f, .35f);
        var szyba = Plain("dzl szyba auta przyciemniona", new Color(.08f, .10f, .12f), .95f, .2f);
        var guma = UGuma; var felga = Plain("dzl felga aluminium", new Color(.62f, .63f, .65f), .7f, .9f);
        var plast = UCzarnyPlastik;
        // podwozie i nadwozie
        OBox(plast, P(0, .35f * k, 0), new Vector3(W - .1f, .22f * k, L - .3f), yaw);
        OBox(lak, P(0, .62f * k, 0), new Vector3(W, .42f * k, L), yaw);                               // pas boczny
        OBox(lak, P(0, .85f * k, L * .33f), new Vector3(W - .04f, .06f * k, L * .32f), yaw);          // maska
        OBox(lak, P(0, .85f * k, -L * .38f), new Vector3(W - .04f, .06f * k, L * .22f), yaw);         // klapa bagażnika
        // kabina: słupki + szyby (przednia i tylna pochylone)
        float kz0 = -L * .26f, kz1 = L * .17f, yk0 = .88f * k, yk1 = H;
        OBox(lak, P(0, yk1 - .03f * k, (kz0 + kz1) * .5f), new Vector3(W - .2f, .06f * k, kz1 - kz0 - .2f), yaw);   // dach
        foreach (int s in new[] { -1, 1 })
        {
            OBox(szyba, P(s * (W * .5f - .1f), (yk0 + yk1) * .5f, (kz0 + kz1) * .5f), new Vector3(.02f, yk1 - yk0 - .05f, kz1 - kz0 - .25f), yaw);
            OBox(lak, P(s * (W * .5f - .1f), (yk0 + yk1) * .5f, (kz0 + kz1) * .5f + .05f), new Vector3(.05f, yk1 - yk0, .08f), yaw);             // słupek B
            OBox(plast, P(s * (W * .5f + .06f), yk0 + .05f, kz1 - .15f), new Vector3(.16f, .1f, .08f), yaw);                                     // lusterko
        }
        OBoxQ(szyba, P(0, (yk0 + yk1) * .5f, kz1 + .2f), new Vector3(W - .25f, (yk1 - yk0) * 1.12f, .02f), q * Quaternion.Euler(-58f, 0, 0));
        OBoxQ(szyba, P(0, (yk0 + yk1) * .5f, kz0 - .18f), new Vector3(W - .25f, (yk1 - yk0) * 1.1f, .02f), q * Quaternion.Euler(52f, 0, 0));
        // koła
        foreach (int s in new[] { -1, 1 }) foreach (float z in new[] { L * .32f, -L * .31f })
            {
                var wc = P(s * (W * .5f - .12f), .34f * k, z);
                Tube(guma, wc - R * .12f * k, wc + R * .12f * k, .34f * k, 16);
                Tube(felga, wc + R * s * .12f * k, wc + R * s * .125f * k, .22f * k, 12);
            }
        // światła
        var lampP = Plain("dzl reflektor (klosz)", new Color(.85f, .87f, .88f), .9f, .3f); var lampT = Plain("dzl lampa tylna czerwona", new Color(.55f, .04f, .04f), .8f, .1f);
        foreach (int s in new[] { -1, 1 }) { OBox(lampP, P(s * W * .33f, .74f * k, L * .5f), new Vector3(.36f * k, .1f * k, .03f), yaw); OBox(lampT, P(s * W * .36f, .76f * k, -L * .5f), new Vector3(.3f * k, .1f * k, .03f), yaw); }
        OBox(plast, P(0, .45f * k, L * .5f), new Vector3(W - .2f, .16f * k, .05f), yaw); OBox(plast, P(0, .45f * k, -L * .5f), new Vector3(W - .2f, .16f * k, .05f), yaw);
        if (policja)
        {
            foreach (int s in new[] { -1, 1 })
            {
                OBox(granat, P(s * (W * .5f + .004f), .58f * k, 0), new Vector3(.01f, .2f * k, L * .96f), yaw);   // pas granatowy
                var n = R * s; var o = P(s * (W * .5f + .012f), .74f * k, -.1f * k);
                Napis3D("POLICJA", o, n, .16f * k, .006f, granat, granat);
            }
            // belka świetlna (niebieskie klosze, bez świecenia)
            OBox(plast, P(0, yk1 + .05f * k, (kz0 + kz1) * .5f + .1f), new Vector3(W * .72f, .06f * k, .24f * k), yaw);
            foreach (int s in new[] { -1, 1 }) OBox(Plain("dzl klosz niebieski belki", new Color(.10f, .25f, .85f), .9f, .1f), P(s * W * .2f, yk1 + .11f * k, (kz0 + kz1) * .5f + .1f), new Vector3(W * .3f, .08f * k, .2f * k), yaw);
            Napis3D("POLICJA", P(0, .86f * k + .035f, L * .36f), Vector3.up, .14f * k, .004f, granat, granat, .15f, .14f, true, -R, F);
        }
        ColliderRot(P(0, .75f * k, 0), new Vector3(W, 1.5f * k, L), yaw);
        Hard(c.x - L * .5f, c.x + L * .5f, c.z - L * .5f, c.z + L * .5f);
        URzeczW(policja ? "radiowóz (sedan oznakowany)" : "samochód osobowy", c + Vector3.up);
        group = g0;
    }

    // ---------------- miejsca AKCJI pod misje (BraxMiejsceZadania — rejestr AKCJI) ----------------
    static void DzlMiejscaZadan()
    {
        foreach (var (napis, p, r, id) in dzlZadania)
        {
            var g = new GameObject("BRAXHORN AKCJA: " + napis); g.transform.SetParent(Group("Dzielnica / miejsca zadań (AKCJA)"), false); g.transform.position = p;
            var mz = g.AddComponent<BraxMiejsceZadania>(); mz.id = id; mz.napis = napis; mz.promien = r;
        }
        foreach (var (rola, gdzie, p, yaw, siedzi) in dzlObsada)
        {
            var g = new GameObject("BRAXHORN miejsce obsady: " + rola + " — " + gdzie + (siedzi ? " (siedzi)" : " (stoi)")); g.transform.SetParent(Group("Dzielnica / miejsca obsady (ludzie)"), false);
            g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        DzlNota("miejsca AKCJI: " + dzlZadania.Count + ", miejsca obsady: " + dzlObsada.Count + ", siedziska klientów: " + uSiedziska.Count);
    }
}
