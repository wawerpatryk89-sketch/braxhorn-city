using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// PORT KONTENEROWY OD NOWA (wykonawca A, 2.11). Właściciel 15:09: „tam, gdzie stoją kontenery i dźwig — ładny plac, jakieś
// firmy; ma tam stać statek, kontenerowiec/barka na kontenery”; 16:00: „PORT i METRO to mają być priorytety… tam jest
// największy syf”. Audyt §12: płot „TEREN PORTU”, za nim jeden rząd kontenerów jak ściana, plac z trawnika, turkusowej płyty
// i cegły, nabrzeże bez polerów i odbojnic, dźwig bez statku.
// GRANICA Z B (B_ETAP1: plac wieżowców x ≤ 66, z −35…−6; nabrzeże B x 84, z −68…−31): port od z −30.5 (wcześniej −40 —
// zachodził 9 m na nabrzeże B). Dźwig (krok „port: dźwig nad statkiem”) stoi na z −29…−21 — tam bez kontenerów.
// ZDJĘTE (BraxMiastoA.Wyczysc): wszystko w obrysie placu x 66.05…83.9, z −30.5…17.4, y −0.25…14 (źródło miasta: płot, brama,
// nawierzchnie, kontenery; renowacja: „ściana kontenerów” z BraxCityEnvironment, palety, drobiazgi) — przed budową.
// NOWE (× k, poziom placu 0.15 jak bulwar): nawierzchnia z płyt betonowych z oznakowaniem w płaszczyźnie (pasy, pola
// kontenerów — osobne pola tej samej płaszczyzny, bez nakładania), oczep nabrzeża z żółtą krawędzią, polery co 10 m,
// odbojnice gumowe na licu nabrzeża, drabinki; ZACUMOWANY KONTENEROWIEC 46 × 12 m (kadłub, pas antyporostowy, burta,
// pokład, zrębnice, kontenery w 2 sekcjach × 3 rzędy × 2–3 warstwy, nadbudówka z mostkiem i skrzydłami, komin, maszt
// z radarem, nazwa na dziobie), cumy dziobowe, rufowe i szpringi do polerów, TRAP z poręczami (wejście na pokład);
// stosy kontenerów 20' i 40' (2–4 warstwy, różne kolory, numery, przetłoczenia i drzwi z ryglami); ciągniki terminalowe
// z naczepami; biuro firmy BRAXHORN LOGISTYKA (2 kondygnacje, okna z pokojem za szybą, recepcja za szklanymi drzwiami
// automatycznymi, szyld, klimatyzatory); brama z budką strażnika i szlabanem, ogrodzenie panelowe, maszty oświetleniowe.
// Punkty akcji pod misje: biuro (zleceniodawca), trap statku, strażnik bramy, magazyn.
internal static class BraxMiastoPort
{
    const float Y = .15f, QX = 84f, X0 = 66.05f, Z0 = -30.5f, Z1 = 17.4f;   // poziom placu, lico nabrzeża, obrys placu
    const float W = -1.7f;
    static float K => BraxMiastoA.K;
    static Transform root, kol;

    public static void Buduj(Transform portRoot, Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxMiastoA.WyczyscPamiec();
        var usun = BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(X0, QX - .1f, -.25f, 14f, Z0, Z1), "port_kontenerowy", portRoot);
        // pas starego placu z −40…−30.5 (przy granicy z B): zdjęte tylko to, co stoi NAD gruntem (stare kontenery, płot, ściana
        // kontenerów) — nawierzchni nie ruszamy (B buduje tam swoje nabrzeże/plac później; bez dziury, jeśli tam nic nie postawi)
        usun += "; pas przy B: " + BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(X0, QX - .1f, .22f, 14f, -40f, Z0), "port_pas_przy_B", portRoot);
        root = new GameObject("CLAUDE A: port kontenerowy (od nowa)").transform; root.SetParent(portRoot, false);
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        BraxUliceKit.UstawGrupe("Port: nawierzchnia i nabrzeże");
        Nawierzchnia(); Nabrzeze();
        BraxUliceKit.UstawGrupe("Port: kontenery", 160f);
        kontModel = kontKod = 0; int kont = Kontenery();
        BraxMiastoA.Log("PORT: kontenery z modelu (20′) " + kontModel + ", z kodu " + kontKod);
        // WŁAŚCICIEL 3.11 10:41 (zdjęcie): kontenerowiec z kodu (bryły kontenerów, kadłub z prostopadłościanów) — „zjebanie
        // wygląda”; zasada: źle i nie da się porządnie → usunąć. Nabrzeże zostaje wolne (dźwig, polery), statek tylko z modelu.
        BraxMiastoA.Log("PORT: statek z kodu WYŁĄCZONY (właściciel 3.11 — klocki)");
        BraxUliceKit.UstawGrupe("Port: biuro, brama, ogrodzenie, maszty", 140f);
        Biuro(); Brama(); Maszty(); Ciagniki();
        int siatek = BraxUliceKit.Zapisz(root, "port");
        BraxMiastoA.Log("PORT: plac " + (QX - X0).ToString("F0") + "×" + (Z1 - Z0).ToString("F0") + " m, kontenerów " + kont + ", kontenerowiec 46×12 m przy nabrzeżu, biuro, brama, siatek " + siatek + "; zdjęte: " + usun);
    }

    // ---------------- nawierzchnia z oznakowaniem W PŁASZCZYŹNIE (pola, nie naklejki) ----------------
    static void Plaszczyzna(UlSiatka s, float x0, float x1, float z0, float z1, float y, IList<Rect> dziury)
    {
        var xs = new SortedSet<float> { x0, x1 }; var zs = new SortedSet<float> { z0, z1 };
        foreach (var d in dziury) { xs.Add(Mathf.Clamp(d.xMin, x0, x1)); xs.Add(Mathf.Clamp(d.xMax, x0, x1)); zs.Add(Mathf.Clamp(d.yMin, z0, z1)); zs.Add(Mathf.Clamp(d.yMax, z0, z1)); }
        var X = xs.ToArray(); var Z = zs.ToArray();
        for (int i = 0; i < X.Length - 1; i++) for (int j = 0; j < Z.Length - 1; j++)
            {
                if (X[i + 1] - X[i] < .002f || Z[j + 1] - Z[j] < .002f) continue;
                var c = new Vector2((X[i] + X[i + 1]) * .5f, (Z[j] + Z[j + 1]) * .5f);
                if (dziury.Any(d => d.Contains(c))) continue;
                s.Quad(new Vector3(X[i], y, Z[j]), new Vector3(X[i + 1], y, Z[j]), new Vector3(X[i + 1], y, Z[j + 1]), new Vector3(X[i], y, Z[j + 1]), Vector3.up);
            }
    }
    static void Nawierzchnia()
    {
        var beton = BraxMiastoA.Tex("Port: płyty betonowe placu", 3.0f, new Color(.78f, .78f, .76f), new Color(.52f, .52f, .5f), .85f, "concrete_floor_02", "concrete_wall_008");
        var zolty = BraxMiastoA.Mat("Port: oznakowanie żółte (farba w nawierzchni)", new Color(.88f, .7f, .1f), .7f);
        var bialy = BraxMiastoA.Mat("Port: oznakowanie białe (farba w nawierzchni)", new Color(.92f, .92f, .9f), .7f);
        var linie = new List<Rect>(); var biale = new List<Rect>();
        // krawędź drogi technicznej wzdłuż nabrzeża (x 77.4) i linia strefy bezpieczeństwa 2 m od krawędzi nabrzeża
        linie.Add(Rect.MinMaxRect(77.3f, Z0, 77.45f, Z1 - .5f)); linie.Add(Rect.MinMaxRect(81.85f, Z0, 82.0f, Z1 - .5f));
        // przerywana oś drogi
        for (float z = Z0 + 1f; z < Z1 - 3f; z += 6f) biale.Add(Rect.MinMaxRect(79.6f, z, 79.75f, z + 3f));
        // pola stosów (obrys białą linią 12 cm)
        foreach (var b in Bloki()) { biale.Add(Rect.MinMaxRect(b.xMin - .25f, b.yMin - .25f, b.xMax + .25f, b.yMin - .13f)); biale.Add(Rect.MinMaxRect(b.xMin - .25f, b.yMax + .13f, b.xMax + .25f, b.yMax + .25f)); biale.Add(Rect.MinMaxRect(b.xMin - .25f, b.yMin - .13f, b.xMin - .13f, b.yMax + .13f)); biale.Add(Rect.MinMaxRect(b.xMax + .13f, b.yMin - .13f, b.xMax + .25f, b.yMax + .13f)); }
        var wszystkie = linie.Concat(biale).ToList();
        var zDziurami = wszystkie.Concat(new[] { Rect.MinMaxRect(66.3f, 6.5f, 74.3f, 16.5f) }).ToList();   // pod biurem posadzka biura (nie płyta na płycie)
        Plaszczyzna(BraxUliceKit.S(beton), X0, 83.3f, Z0, Z1, Y, zDziurami);
        foreach (var r in linie) BraxUliceKit.S(zolty).Quad(new Vector3(r.xMin, Y, r.yMin), new Vector3(r.xMax, Y, r.yMin), new Vector3(r.xMax, Y, r.yMax), new Vector3(r.xMin, Y, r.yMax), Vector3.up);
        foreach (var r in biale) BraxUliceKit.S(bialy).Quad(new Vector3(r.xMin, Y, r.yMin), new Vector3(r.xMax, Y, r.yMin), new Vector3(r.xMax, Y, r.yMax), new Vector3(r.xMin, Y, r.yMax), Vector3.up);
        BraxMiastoA.Kol(kol, "nawierzchnia placu portu", new Vector3(X0, Y - .4f, Z0), new Vector3(83.3f, Y, Z1));
    }

    static IEnumerable<Rect> Bloki()
    {
        yield return Rect.MinMaxRect(67.2f, -1.5f, 75.1f, 5.15f);     // blok A: kontenery 20' (wzdłuż x, 2 rzędy) między blokiem B a biurem
        yield return Rect.MinMaxRect(67.0f, -19.0f, 76.9f, -2.8f);    // blok B: kontenery 40'
    }

    // ---------------- nabrzeże: oczep z żółtą krawędzią, polery, odbojnice, drabinki ----------------
    static void Nabrzeze()
    {
        var oczep = BraxMiastoA.Tex("Port: oczep nabrzeża (beton)", 2.0f, new Color(.72f, .72f, .7f), new Color(.5f, .5f, .48f), .9f, "concrete_wall_008", "concrete_floor_02");
        var zolty = BraxMiastoA.Mat("Port: krawędź nabrzeża żółta", new Color(.9f, .72f, .08f), .6f);
        var zeliwo = BraxMiastoA.Mat("Port: polery żeliwne (czarne)", new Color(.07f, .07f, .075f), .5f, .5f);
        var guma = BraxMiastoA.Mat("Port: odbojnice gumowe", new Color(.05f, .05f, .055f), .9f);
        var stal = BraxMiastoA.Mat("Port: stal ocynkowana", new Color(.62f, .63f, .64f), .45f, .85f);
        var o = BraxUliceKit.S(oczep);
        // oczep 0.85 m szer., 20 cm nad placem; wierzch: beton + żółty pas 25 cm przy krawędzi (dwa pola jednej płaszczyzny)
        o.Box(new Vector3(83.3f, Y, Z0), new Vector3(QX + .15f, Y + .2f, Z1), 8 | 4);
        o.Quad(new Vector3(83.3f, Y + .2f, Z0), new Vector3(QX - .1f, Y + .2f, Z0), new Vector3(QX - .1f, Y + .2f, Z1), new Vector3(83.3f, Y + .2f, Z1), Vector3.up);
        BraxUliceKit.S(zolty).Quad(new Vector3(QX - .1f, Y + .2f, Z0), new Vector3(QX + .15f, Y + .2f, Z0), new Vector3(QX + .15f, Y + .2f, Z1), new Vector3(QX - .1f, Y + .2f, Z1), Vector3.up);
        BraxMiastoA.Kol(kol, "oczep nabrzeża", new Vector3(83.3f, Y, Z0), new Vector3(QX + .15f, Y + .2f, Z1));
        // polery co 10 m (kształt grzybkowy) i odbojnice D co 5 m na licu
        for (float z = Z0 + 4.5f; z < Z1 - 1f; z += 10f)
        {
            var p = new Vector3(83.75f, Y + .2f, z);
            var s = BraxUliceKit.S(zeliwo);
            s.Walec(p, .26f * K, .06f, 16); s.Rura(p + Vector3.up * .06f, p + Vector3.up * .45f * K, .17f * K, 14); s.Walec(p + Vector3.up * .45f * K, .24f * K, .07f, 16);
            var g = new GameObject("kolizja: poler"); g.transform.SetParent(kol, false); g.transform.position = p + Vector3.up * .3f; var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .25f * K; cc.height = .6f * K;
        }
        for (float z = Z0 + 2f; z < Z1; z += 5f) BraxUliceKit.S(guma).Box(new Vector3(QX + .15f, W - .9f, z - .2f), new Vector3(QX + .55f, Y + .05f, z + .2f));
        // drabinki z wody (stal) co 25 m
        for (float z = Z0 + 9f; z < Z1; z += 25f)
        {
            foreach (float d in new[] { -.25f, .25f }) BraxUliceKit.S(stal).Rura(new Vector3(QX + .2f, W - 1.2f, z + d), new Vector3(QX + .2f, Y + 1.0f, z + d), .025f, 8);
            for (float y = W - 1f; y < Y; y += .3f) BraxUliceKit.S(stal).Rura(new Vector3(QX + .2f, y, z - .25f), new Vector3(QX + .2f, y, z + .25f), .018f, 6);
        }
    }

    // ---------------- kontenery (20' i 40', × k): przetłoczenia, drzwi z ryglami, numery ----------------
    static readonly Color32[] KolKont = { BraxUliceKit.C(.62f, .12f, .08f), BraxUliceKit.C(.12f, .25f, .52f), BraxUliceKit.C(.15f, .38f, .2f), BraxUliceKit.C(.85f, .45f, .1f), BraxUliceKit.C(.55f, .56f, .57f), BraxUliceKit.C(.88f, .88f, .86f), BraxUliceKit.C(.45f, .12f, .38f) };
    static int kontModel, kontKod;
    static void Kontener(Vector3 dolSrodek, bool czterdziesci, bool wzdluzZ, int nr, Color32 kolor)
    {
        float L = (czterdziesci ? 12.19f : 6.06f) * K, B = 2.44f * K, H = 2.59f * K;
        // 3.11 (właściciel 11:42 — model kontenera; „kontenerowiec z tymi kontenerami normalnie przy porcie”): PRAWDZIWY kontener
        // 20′ z modelu (BraxMiastoAssety.Kontener20); 40′ = dwa 20′ czołami (szczelina 7 cm). Z kodu tylko, gdy brak paczki.
        float yaw = (wzdluzZ ? 90f : 0f) + (nr % 2 == 0 ? 0f : 180f);
        var ax0 = wzdluzZ ? Vector3.forward : Vector3.right;
        if (!czterdziesci)
        {
            if (BraxMiastoAssety.Kontener20(root, dolSrodek, yaw, nr * 7 + 3)) { kontModel++; return; }
        }
        else
        {
            var a1 = BraxMiastoAssety.Kontener20(root, dolSrodek - ax0 * (3.03f * K + .035f), yaw, nr * 7 + 3);
            var a2 = a1 ? BraxMiastoAssety.Kontener20(root, dolSrodek + ax0 * (3.03f * K + .035f), yaw, nr * 7 + 3) : null;
            if (a1 && a2) { kontModel += 2; return; }
            if (a1) Object.DestroyImmediate(a1);
        }
        kontKod++;
        var blacha = BraxMiastoA.Mat("Port: blacha kontenerów (kolory z wierzchołków)", Color.white, .65f, .35f);
        var ciemna = BraxMiastoA.Mat("Port: rygle i naroża kontenerów", new Color(.12f, .12f, .13f), .55f, .5f);
        var s = BraxUliceKit.S(blacha); s.kolor = kolor;
        Vector3 ax = wzdluzZ ? Vector3.forward : Vector3.right, sb = wzdluzZ ? Vector3.right : Vector3.back;
        var c = dolSrodek + Vector3.up * H * .5f;
        // korpus (boki cofnięte 3 cm za przetłoczenia — przetłoczenia to bryły na całej wysokości, nie płytki na ścianie)
        s.OBox(c, ax, Vector3.up, sb, L - .04f, H - .06f, B - .06f);   // korpus 2–3 cm w obrysie naroży (naroża wystają — bez wspólnych płaszczyzn)
        int zebra = Mathf.FloorToInt(L / .55f);
        for (int i = 1; i < zebra; i++)
        {
            float t = -L * .5f + i * L / zebra;
            foreach (float sg in new[] { -1f, 1f }) s.OBox(c + ax * t + sb * sg * (B * .5f - .015f), ax, Vector3.up, sb, .14f, H - .25f, .03f);
        }
        s.kolor = BraxUliceKit.Bialy;
        // naroża (odlewy) i ramy czołowe, rygle drzwi
        var d = BraxUliceKit.S(ciemna);
        foreach (float a in new[] { -1f, 1f }) foreach (float b in new[] { -1f, 1f }) foreach (float y in new[] { 0f, H - .16f })
                    d.OBox(dolSrodek + ax * a * (L * .5f - .09f) + sb * b * (B * .5f - .09f) + Vector3.up * (y + .08f), ax, Vector3.up, sb, .18f, .16f, .18f);
        foreach (float r in new[] { -.75f, -.25f, .25f, .75f }) d.OBox(dolSrodek + ax * (L * .5f + .02f) + sb * r * B * .5f + Vector3.up * H * .5f, sb, Vector3.up, ax, .04f, H - .4f, .04f);
        // numer (litery przestrzenne na drzwiach)
        BraxUliceKit.Napis3D("BRXU " + (412000 + nr), dolSrodek + ax * (L * .5f + .01f) + Vector3.up * (H - .45f), ax, .13f, .01f, BraxMiastoA.Mat("Port: numery kontenerów (białe)", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Port: numery kontenerów (białe)", new Color(.95f, .95f, .93f), .5f));
    }
    static int Kontenery()
    {
        int n = 0; var rng = new System.Random(4120); float B = 2.44f * K, H = 2.59f * K, gap = .3f;
        foreach (var (blok, czterdz) in new[] { (Bloki().First(), false), (Bloki().Last(), true) })
        {
            float L = (czterdz ? 12.19f : 6.06f) * K; bool wzdluzZ = czterdz;   // 40' wzdłuż z, 20' wzdłuż x
            float dx = wzdluzZ ? B : L, dz = wzdluzZ ? L : B, kx = wzdluzZ ? B + gap : L + .5f, kz = wzdluzZ ? L + .5f : B + gap;
            for (float x = blok.xMin + dx * .5f; x <= blok.xMax - dx * .5f + .01f; x += kx)
                for (float z = blok.yMin + dz * .5f; z <= blok.yMax - dz * .5f + .01f; z += kz)
                {
                    int h = 2 + rng.Next(czterdz ? 3 : 2);
                    for (int lv = 0; lv < h; lv++) { Kontener(new Vector3(x, Y + lv * H, z), czterdz, wzdluzZ, n, KolKont[rng.Next(KolKont.Length)]); n++; }
                    BraxMiastoA.Kol(kol, "stos kontenerów", new Vector3(x - dx * .5f, Y, z - dz * .5f), new Vector3(x + dx * .5f, Y + h * H, z + dz * .5f));
                }
        }
        return n;
    }

    // ---------------- kontenerowiec przy nabrzeżu ----------------
    const float SX0 = 84.6f, SX1 = 96.6f, SZ0 = -30f, SZ1 = 16f;   // 46 m (rufa z −30 — granica z nabrzeżem B)
    static float Deck => W + 3.2f;   // pokład 3.2 m nad wodą (1.5 — trap z nabrzeża 0.35)
    static void Statek()
    {
        var granat = BraxMiastoA.Mat("Statek: burta granatowa", new Color(.06f, .12f, .26f), .45f, .3f);
        var czerw = BraxMiastoA.Mat("Statek: farba antyporostowa (czerwona)", new Color(.48f, .08f, .07f), .6f, .1f);
        var biala = BraxMiastoA.Mat("Statek: biel nadbudówki i linii wodnej", new Color(.92f, .92f, .9f), .45f, .1f);
        var poklad = BraxMiastoA.Mat("Statek: pokład (zieleń okrętowa)", new Color(.22f, .32f, .26f), .7f, .2f);
        var szyby = BraxMiastoA.Mat("Statek: szyby mostka", new Color(.08f, .12f, .15f), .1f, .6f);
        var stal = BraxMiastoA.Mat("Port: stal ocynkowana", new Color(.62f, .63f, .64f), .45f, .85f);
        var komin = BraxMiastoA.Mat("Statek: komin (czerń z pasem marki)", new Color(.05f, .05f, .055f), .5f, .3f);
        float bm = (SX1 - SX0) * .5f, cx = (SX0 + SX1) * .5f, yb = W - 3.0f;
        // przekroje: półszerokość od rufy do dziobu
        float Pol(float z) { if (z > 6f) { float t = (z - 6f) / (SZ1 - 6f); return bm * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * .92f)); } if (z < SZ0 + 2f) return bm * (.85f + .15f * (z - SZ0) / 2f); return bm; }
        var zs = new List<float>(); for (float z = SZ0; z < 6f; z += 4f) zs.Add(z); for (float z = 6f; z <= SZ1 + .001f; z += 1f) zs.Add(z);
        var kad = new Dictionary<Material, UlSiatka> { { granat, BraxUliceKit.S(granat) }, { czerw, BraxUliceKit.S(czerw) }, { biala, BraxUliceKit.S(biala) } };
        float yA = W + .25f, yBt = W + .45f;   // antyporost do 25 cm nad wodą, biały pas wodnicy do 45 cm
        for (int i = 0; i < zs.Count - 1; i++)
        {
            float za = zs[i], zb = zs[i + 1], pa = Pol(za), pb = Pol(zb);
            foreach (float sg in new[] { -1f, 1f })
            {
                bool furta = sg < 0 && za < -7f && zb > -9f;   // furta trapu w nadburciu (lewa burta, od nabrzeża)
                Vector3 V(float z, float p, float y) => new Vector3(cx + sg * p, y, z);
                var n = new Vector3(sg, 0, -(pb - pa) / (zb - za) * sg).normalized; if (sg < 0) n = new Vector3(-1, 0, (pb - pa) / (zb - za)).normalized;
                n = Vector3.Cross(Vector3.up, V(zb, pb, 0) - V(za, pa, 0)).normalized * (sg > 0 ? -1f : 1f);
                if (Vector3.Dot(n, new Vector3(sg, 0, 0)) < 0) n = -n;
                kad[czerw].Quad(V(za, pa, yb), V(zb, pb, yb), V(zb, pb, yA), V(za, pa, yA), n);
                kad[biala].Quad(V(za, pa, yA), V(zb, pb, yA), V(zb, pb, yBt), V(za, pa, yBt), n);
                kad[granat].Quad(V(za, pa, yBt), V(zb, pb, yBt), V(zb, pb, furta ? Deck : Deck + 1.1f), V(za, pa, furta ? Deck : Deck + 1.1f), n);   // burta z nadburciem 1.1 m
                if (furta) kad[granat].Quad(V(za, pa - .15f, Deck), V(zb, pb - .15f, Deck), V(zb, pb, Deck), V(za, pa, Deck), Vector3.up);
                else
                {
                    // wewnętrzna strona nadburcia i jego wierzch
                    kad[granat].Quad(V(za, pa - .15f, Deck), V(zb, Mathf.Max(0f, pb - .15f), Deck), V(zb, Mathf.Max(0f, pb - .15f), Deck + 1.1f), V(za, pa - .15f, Deck + 1.1f), -n);
                    kad[granat].Quad(V(za, pa - .15f, Deck + 1.1f), V(zb, Mathf.Max(0f, pb - .15f), Deck + 1.1f), V(zb, pb, Deck + 1.1f), V(za, pa, Deck + 1.1f), Vector3.up);
                }
                BraxUliceKit.S(poklad).Quad(V(za, 0, Deck), V(zb, 0, Deck), V(zb, Mathf.Max(0f, pb - .15f), Deck), V(za, pa - .15f, Deck), Vector3.up);
            }
        }
        // dziobnica (czoło dziobu)
        float pd = Pol(SZ1);
        kad[czerw].Quad(new Vector3(cx - pd, yb, SZ1), new Vector3(cx + pd, yb, SZ1), new Vector3(cx + pd, yA, SZ1), new Vector3(cx - pd, yA, SZ1), Vector3.forward);
        kad[biala].Quad(new Vector3(cx - pd, yA, SZ1), new Vector3(cx + pd, yA, SZ1), new Vector3(cx + pd, yBt, SZ1), new Vector3(cx - pd, yBt, SZ1), Vector3.forward);
        kad[granat].Quad(new Vector3(cx - pd, yBt, SZ1), new Vector3(cx + pd, yBt, SZ1), new Vector3(cx + pd, Deck + 1.1f, SZ1), new Vector3(cx - pd, Deck + 1.1f, SZ1), Vector3.forward);
        // rufa (pawęż)
        float pr = Pol(SZ0);
        kad[czerw].Quad(new Vector3(cx - pr, yb, SZ0), new Vector3(cx + pr, yb, SZ0), new Vector3(cx + pr, yA, SZ0), new Vector3(cx - pr, yA, SZ0), Vector3.back);
        kad[biala].Quad(new Vector3(cx - pr, yA, SZ0), new Vector3(cx + pr, yA, SZ0), new Vector3(cx + pr, yBt, SZ0), new Vector3(cx - pr, yBt, SZ0), Vector3.back);
        kad[granat].Quad(new Vector3(cx - pr, yBt, SZ0), new Vector3(cx + pr, yBt, SZ0), new Vector3(cx + pr, Deck + 1.1f, SZ0), new Vector3(cx - pr, Deck + 1.1f, SZ0), Vector3.back);
        // nazwa na dziobie (obie burty) i na rufie
        var litery = BraxMiastoA.Mat("Statek: litery nazwy (białe)", new Color(.95f, .95f, .93f), .4f);
        foreach (float sg in new[] { -1f, 1f })
        {
            float zN = 9f, p = Pol(zN) + .02f;
            BraxUliceKit.Napis3D("BRAXHORN", new Vector3(cx + sg * p, Deck - .4f, zN), new Vector3(sg, 0, 0), .55f, .03f, litery, litery);
        }
        BraxUliceKit.Napis3D("BRAXHORN · GDYNIA", new Vector3(cx, Deck - .5f, SZ0 - .02f), Vector3.back, .4f, .03f, litery, litery);
        // zrębnice ładowni i kontenery na pokładzie: 2 sekcje 40' × 3 rzędy × 2–3 warstwy
        float B = 2.44f * K, H = 2.59f * K, L = 12.19f * K; var rng = new System.Random(77);
        int nr = 900;
        foreach (float zc in new[] { -11.6f, 4.7f })
        {
            BraxUliceKit.S(granat).Box(new Vector3(cx - 4.4f, Deck, zc - L * .5f - .3f), new Vector3(cx + 4.4f, Deck + .9f, zc + L * .5f + .3f), 4);
            for (int r = -1; r <= 1; r++)
            {
                int h = 2 + rng.Next(2);
                for (int lv = 0; lv < h; lv++) Kontener(new Vector3(cx + r * (B + .15f), Deck + .9f + lv * H, zc), true, true, nr++, KolKont[rng.Next(KolKont.Length)]);
                BraxMiastoA.Kol(kol, "kontenery na statku", new Vector3(cx + r * (B + .15f) - B * .5f, Deck + .9f, zc - L * .5f), new Vector3(cx + r * (B + .15f) + B * .5f, Deck + .9f + h * H, zc + L * .5f));
            }
            BraxMiastoA.Kol(kol, "zrębnica ładowni", new Vector3(cx - 4.4f, Deck, zc - L * .5f - .3f), new Vector3(cx + 4.4f, Deck + .9f, zc + L * .5f + .3f));
        }
        // nadbudówka na rufie: 4 kondygnacje, mostek z oknami dookoła i skrzydłami, komin, maszt z radarem
        float n0 = SZ0 + 1.2f, n1 = SZ0 + 9.2f, hx = 4.2f; var bi = BraxUliceKit.S(biala);
        for (int lv = 0; lv < 4; lv++)
        {
            float y0 = Deck + lv * 2.8f, y1 = y0 + 2.8f, cof = lv * .25f;
            var a = new Vector3(cx - hx + cof, y0, n0 + cof); var b = new Vector3(cx + hx - cof, y1, n1 - cof);
            bi.Box(a, b, 4);
            // pas okien (szyby w otworach — osobne pola lica: okna jako bryły wpuszczone 5 cm)
            if (lv >= 1)
                for (float x = a.x + .8f; x < b.x - .6f; x += 1.4f)
                    BraxUliceKit.S(szyby).Box(new Vector3(x, y0 + 1.0f, b.z - .05f), new Vector3(x + .9f, y0 + 2.0f, b.z + .02f));
        }
        float yM = Deck + 4 * 2.8f;
        bi.Box(new Vector3(cx - hx - 2.2f, yM - 2.8f + .9f, n1 - 3.2f), new Vector3(cx + hx + 2.2f, yM - 2.8f + 1.1f, n1 - .8f));   // skrzydła mostka
        BraxUliceKit.S(szyby).Box(new Vector3(cx - hx + 1.0f, yM - 1.9f, n1 - .79f), new Vector3(cx + hx - 1.0f, yM - .8f, n1 - .72f));
        BraxUliceKit.S(komin).Box(new Vector3(cx - 1.2f, yM, n0 + 1.0f), new Vector3(cx + 1.2f, yM + 3.4f, n0 + 3.2f));
        BraxUliceKit.S(BraxMiastoA.Mat("Statek: pas marki na kominie", new Color(.35f, .75f, .15f), .5f)).Box(new Vector3(cx - 1.25f, yM + 2.2f, n0 + .95f), new Vector3(cx + 1.25f, yM + 2.7f, n0 + 3.25f));
        BraxUliceKit.S(stal).Rura(new Vector3(cx, yM, n1 - 2.5f), new Vector3(cx, yM + 6f, n1 - 2.5f), .09f, 8);
        BraxUliceKit.S(stal).Box(new Vector3(cx - 1.6f, yM + 4.8f, n1 - 2.6f), new Vector3(cx + 1.6f, yM + 4.95f, n1 - 2.4f));   // radar
        BraxMiastoA.Kol(kol, "nadbudówka", new Vector3(cx - hx, Deck, n0), new Vector3(cx + hx, yM, n1));
        // kadłub: kolizja (pokład chodliwy) — prostopadłościan części cylindrycznej + dziób z kolizji wypukłej
        BraxMiastoA.Kol(kol, "kadłub (pokład)", new Vector3(SX0, yb, SZ0), new Vector3(SX1, Deck, 6f));
        var dz = new UlSiatka(); for (float z = 6f; z < SZ1; z += 1f) { float a = Pol(z), b = Pol(z + 1f); dz.Quad(new Vector3(cx - a, yb, z), new Vector3(cx + a, yb, z), new Vector3(cx + b, Deck, z + 1f), new Vector3(cx - b, Deck, z + 1f), Vector3.up); }
        var dm = dz.Zbuduj("A: dziób statku (kolizja)"); BraxZapisAssetu.Utworz(dm, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Port_dziob_kolizja.asset"));
        var dc = new GameObject("kolizja: dziób"); dc.transform.SetParent(kol, false); var mc = dc.AddComponent<MeshCollider>(); mc.sharedMesh = dm; mc.convex = true;
        BraxMiastoA.Kol(kol, "nadburcie prawe", new Vector3(SX1 - .15f, Deck, SZ0), new Vector3(SX1, Deck + 1.1f, 6f));
        BraxMiastoA.Kol(kol, "nadburcie lewe", new Vector3(SX0, Deck, SZ0), new Vector3(SX0 + .15f, Deck + 1.1f, -10f));
        BraxMiastoA.Kol(kol, "nadburcie lewe", new Vector3(SX0, Deck, -6f), new Vector3(SX0 + .15f, Deck + 1.1f, 6f));
        // CUMY: dziobowa, rufowa i dwa szpringi do polerów nabrzeża (zwis liny)
        var lina = BraxMiastoA.Mat("Statek: cumy (lina poliamidowa)", new Color(.85f, .82f, .7f), .9f);
        void Cuma(Vector3 a, Vector3 b) { var prev = a; for (int q = 1; q <= 10; q++) { float t = q / 10f; var p = Vector3.Lerp(a, b, t) + Vector3.down * (.9f * 4f * t * (1 - t)); BraxUliceKit.S(lina).Rura(prev, p, .045f, 6, false); prev = p; } }
        Cuma(new Vector3(SX0 + 1f, Deck + 1.1f, 13f), new Vector3(83.75f, Y + .5f, 14f));
        Cuma(new Vector3(SX0 + 1f, Deck + 1.1f, SZ0 + 1.5f), new Vector3(83.75f, Y + .5f, Z0 + 4.5f));
        Cuma(new Vector3(SX0 + .5f, Deck + 1.1f, -6f), new Vector3(83.75f, Y + .5f, -16f));
        Cuma(new Vector3(SX0 + .5f, Deck + 1.1f, -14f), new Vector3(83.75f, Y + .5f, -6f));
        // TRAP z nabrzeża na pokład (ramp 3.6 m z listwami i poręczami), otwór w nadburciu przy trapie
        float tz = -8f, tx0 = 81.4f, tx1 = SX0, ty0 = Y + .2f, ty1 = Deck + .02f, tw = .9f * K;   // trap od oczepu do krawędzi pokładu w furcie
        var deski = BraxMiastoA.Mat("Statek: trap (aluminium ryflowane)", new Color(.6f, .61f, .62f), .4f, .8f);
        var dir = new Vector3(tx1 - tx0, ty1 - ty0, 0).normalized; var len = Vector3.Distance(new Vector3(tx0, ty0, tz), new Vector3(tx1, ty1, tz));
        var tc = new Vector3((tx0 + tx1) * .5f, (ty0 + ty1) * .5f, tz);
        BraxUliceKit.S(deski).OBox(tc, dir, Vector3.Cross(Vector3.forward, dir), Vector3.forward, len, .08f, tw);
        for (float t = .2f; t < len - .1f; t += .4f) BraxUliceKit.S(stal).OBox(new Vector3(tx0, ty0, tz) + dir * t + Vector3.Cross(Vector3.forward, dir) * .05f, dir, Vector3.Cross(Vector3.forward, dir), Vector3.forward, .04f, .03f, tw - .1f);
        foreach (float sg in new[] { -1f, 1f })
        {
            var a = new Vector3(tx0, ty0 + 1.0f * K, tz + sg * tw * .5f); var b = new Vector3(tx1, ty1 + 1.0f * K, tz + sg * tw * .5f);
            BraxUliceKit.S(stal).Rura(a, b, .03f, 8);
            for (int q = 0; q <= 4; q++) { var p = Vector3.Lerp(new Vector3(tx0, ty0, tz + sg * tw * .5f), new Vector3(tx1, ty1, tz + sg * tw * .5f), q / 4f); BraxUliceKit.S(stal).Rura(p, p + Vector3.up * 1.0f * K, .02f, 6); }
        }
        var tr = new GameObject("kolizja: trap"); tr.transform.SetParent(kol, false); tr.transform.position = tc - Vector3.Cross(Vector3.forward, dir) * .1f;
        tr.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, dir)); tr.AddComponent<BoxCollider>().size = new Vector3(len, .12f, tw);
        BraxMiastoA.Akcja(root, "port_statek_trap", "WEJDŹ NA STATEK", "Wachtowy: Statek BRAXHORN — załadunek kontenerów. Na pokład tylko z przepustką z biura.", new Vector3(80.6f, Y, tz), 1.8f);
        BraxUliceKit.Punkt("port_statek_poklad", "pokład kontenerowca (cel misji)", new Vector3(cx, Deck, -10f));
    }

    // ---------------- biuro BRAXHORN LOGISTYKA ----------------
    static void Biuro()
    {
        float x0 = 66.3f, x1 = 74.3f, z0 = 6.5f, z1 = 16.5f, h1 = 3.8f, h2 = 3.4f, H = h1 + h2 + .9f;
        var tynk = BraxMiastoA.Tex("Port: biuro tynk jasnoszary", 2.4f, new Color(.86f, .87f, .88f), new Color(.72f, .73f, .74f), .9f, "white_plaster_02");
        var okl = BraxMiastoA.Tex("Port: biuro okładzina grafit", 1.6f, new Color(.32f, .33f, .35f), new Color(.15f, .16f, .17f), .6f, "exterior_wall_cladding_02", "exterior_wall_cladding");
        var alu = BraxMiastoA.Mat("Port: aluminium okien grafit", new Color(.1f, .11f, .12f), .4f, .55f);
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        var okno = BraxUliceKit.OknoPokoj(2.5f, h2, Y + h1, 2, 4, 4f, 0f);
        var lit = BraxMiastoA.Swieci("Port: litery szyldu biura (podświetlane)", Color.white, new Color(1.8f, 1.8f, 1.8f));
        var boki = BraxMiastoA.Mat("Port: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        // ściany zewnętrzne: wschód (od drogi) z wejściem i witrynami recepcji, północ (od bramy) z oknami, zachód i południe pełne z oknami
        var sc = BraxUliceKit.S(tynk); var so = BraxUliceKit.S(okl);
        // wschód: parter przeszklony (recepcja), piętro z oknami
        var otwPartE = new List<Rect> { Rect.MinMaxRect(1.0f, Y, 3.4f, Y + 2.85f), Rect.MinMaxRect(4.0f, Y + .6f, 9.0f, Y + 3.2f) };   // s od z0 (oś +z)
        var otwPietro = new List<Rect>(); for (float s = 1.0f; s < 9.2f; s += 2.5f) otwPietro.Add(Rect.MinMaxRect(s, Y + h1 + .9f, s + 1.5f, Y + h1 + 2.75f));
        BraxMiastoA.Sciana(so, new Vector3(x1, 0, z0), Vector3.forward, Vector3.right, z1 - z0, Y, Y + h1, otwPartE, .25f, so);
        BraxMiastoA.Sciana(sc, new Vector3(x1, 0, z0), Vector3.forward, Vector3.right, z1 - z0, Y + h1, Y + H, otwPietro, .25f, sc);
        foreach (var o in otwPietro) BraxUliceKit.S(okno).Quad(new Vector3(x1 - .2f, o.yMin, z0 + o.xMin), new Vector3(x1 - .2f, o.yMin, z0 + o.xMax), new Vector3(x1 - .2f, o.yMax, z0 + o.xMax), new Vector3(x1 - .2f, o.yMax, z0 + o.xMin), Vector3.right);
        // witryna recepcji (szkło przezroczyste w ramie) i drzwi automatyczne
        var w = otwPartE[1];
        BraxUliceKit.S(szklo).Quad(new Vector3(x1 - .12f, w.yMin, z0 + w.xMin), new Vector3(x1 - .12f, w.yMin, z0 + w.xMax), new Vector3(x1 - .12f, w.yMax, z0 + w.xMax), new Vector3(x1 - .12f, w.yMax, z0 + w.xMin), Vector3.right);
        BraxUliceKit.S(alu).Box(new Vector3(x1 - .16f, w.yMin, z0 + w.xMin), new Vector3(x1 - .08f, w.yMin + .08f, z0 + w.xMax)); BraxUliceKit.S(alu).Box(new Vector3(x1 - .16f, w.yMax - .08f, z0 + w.xMin), new Vector3(x1 - .08f, w.yMax, z0 + w.xMax));
        BraxMiastoA.DrzwiPrzesuwne(root, "BRAXHORN LOGISTYKA — wejście", new Vector3(x1 - .12f, Y, z0 + 2.2f), Vector3.forward, 2.4f, 2.85f, szklo, alu);
        // pozostałe ściany (lico na zewnątrz) z oknami piętra i parteru
        void Pelna(Vector3 A, Vector3 R, Vector3 N, float dl)
        {
            var op = new List<Rect>(); var oparter = new List<Rect>();
            for (float s = 1.0f; s < dl - 1.6f; s += 2.5f) { op.Add(Rect.MinMaxRect(s, Y + h1 + .9f, s + 1.5f, Y + h1 + 2.75f)); oparter.Add(Rect.MinMaxRect(s, Y + 1.0f, s + 1.5f, Y + 2.9f)); }
            BraxMiastoA.Sciana(so, A, R, N, dl, Y, Y + h1, oparter, .25f, so);
            BraxMiastoA.Sciana(sc, A, R, N, dl, Y + h1, Y + H, op, .25f, sc);
            foreach (var o in op.Concat(oparter)) BraxUliceKit.S(okno).Quad(A + R * o.xMin + Vector3.up * o.yMin - N * .2f, A + R * o.xMax + Vector3.up * o.yMin - N * .2f, A + R * o.xMax + Vector3.up * o.yMax - N * .2f, A + R * o.xMin + Vector3.up * o.yMax - N * .2f, N);
        }
        Pelna(new Vector3(x1, 0, z1), Vector3.left, Vector3.forward, x1 - x0);
        Pelna(new Vector3(x0, 0, z0), Vector3.right, Vector3.back, x1 - x0);
        Pelna(new Vector3(x0, 0, z1), Vector3.back, Vector3.left, z1 - z0);
        // gzyms między kondygnacjami, attyka z obróbką, dach
        // gzyms międzykondygnacyjny: pierścień PRZED ścianami (nie przez wnętrze — strop recepcji zostaje sam)
        var gz = BraxUliceKit.S(okl); float g0 = Y + h1 - .1f, g1 = Y + h1 + .1f;
        gz.Box(new Vector3(x0 - .12f, g0, z0 - .12f), new Vector3(x1 + .12f, g1, z0), 32); gz.Box(new Vector3(x0 - .12f, g0, z1), new Vector3(x1 + .12f, g1, z1 + .12f), 16);
        gz.Box(new Vector3(x0 - .12f, g0, z0), new Vector3(x0, g1, z1), 2); gz.Box(new Vector3(x1, g0, z0), new Vector3(x1 + .12f, g1, z1), 1);
        BraxUliceKit.S(BraxMiastoA.Mat("Port: dach biura (papa)", new Color(.2f, .2f, .21f), .95f)).Quad(new Vector3(x0 + .25f, Y + H - .3f, z0 + .25f), new Vector3(x1 - .25f, Y + H - .3f, z0 + .25f), new Vector3(x1 - .25f, Y + H - .3f, z1 - .25f), new Vector3(x0 + .25f, Y + H - .3f, z1 - .25f), Vector3.up);
        BraxUliceKit.S(BraxMiastoA.Mat("Port: obróbka attyki (blacha)", new Color(.4f, .42f, .44f), .5f, .7f)).Box(new Vector3(x0 - .05f, Y + H, z0 - .05f), new Vector3(x1 + .05f, Y + H + .08f, z1 + .05f), 4);
        // szyld z liter na wschodniej ścianie nad wejściem, klimatyzatory na dachu
        BraxUliceKit.Napis3D("BRAXHORN LOGISTYKA", new Vector3(x1 + .25f, Y + h1 + .2f + 2.95f, (z0 + z1) * .5f), Vector3.right, .42f, .08f, lit, boki);
        for (int i = 0; i < 2; i++) { var p = new Vector3(x0 + 1.5f + i * 2.2f, Y + H, z0 + 1.5f); BraxUliceKit.S(BraxMiastoA.Mat("Port: klimatyzator", new Color(.85f, .86f, .85f), .5f)).Box(p, p + new Vector3(1.4f, .9f, .6f)); }
        // wnętrze recepcji (parter): podłoga, ściany, sufit, lada, krzesła, tablica
        float ix0 = x0 + .25f, ix1 = x1 - .25f, iz0 = z0 + .25f, iz1 = z1 - .25f;
        BraxMiastoA.Pokoj(BraxMiastoA.Tex("Port: biuro posadzka", 1.0f, new Color(.7f, .7f, .7f), new Color(.5f, .5f, .5f), .6f, "floor_tiles_06", "square_tiles"), tynk, BraxMiastoA.Mat("Port: sufit biura", new Color(.93f, .93f, .92f), .9f), ix0, ix1, iz0, iz1, Y, Y + h1 - .1f,
            new[] { oparterRects(ix1 - ix0, x1 - x0, true), Odwr(otwPartE, z1 - z0, iz0 - z0, iz1 - iz0), oparterRects(ix1 - ix0, x1 - x0, false), Odwr2(z1 - z0, iz1 - iz0) });
        var lada = BraxMiastoA.Mat("Port: lada recepcji (dąb)", new Color(.55f, .42f, .28f), .55f);
        BraxUliceKit.S(lada).Box(new Vector3(68.2f, Y, 9.5f), new Vector3(69.0f, Y + 1.05f * K, 13.5f));
        BraxMiastoA.Kol(kol, "lada recepcji portu", new Vector3(68.2f, Y, 9.5f), new Vector3(69.0f, Y + 1.05f * K, 13.5f));
        BraxMiastoA.Kol(kol, "biuro ściany", new Vector3(x0, Y, z0), new Vector3(x0 + .25f, Y + H, z1)); BraxMiastoA.Kol(kol, "biuro ściany", new Vector3(x0, Y, z1 - .25f), new Vector3(x1, Y + H, z1));
        BraxMiastoA.Kol(kol, "biuro ściany", new Vector3(x0, Y, z0), new Vector3(x1, Y + H, z0 + .25f));
        BraxMiastoA.Kol(kol, "biuro ściana wsch.", new Vector3(x1 - .25f, Y, z0), new Vector3(x1, Y + H, z0 + 1.0f)); BraxMiastoA.Kol(kol, "biuro ściana wsch.", new Vector3(x1 - .25f, Y, z0 + 3.4f), new Vector3(x1, Y + H, z1));
        BraxMiastoA.Kol(kol, "biuro nadproże", new Vector3(x1 - .25f, Y + 2.85f, z0 + 1.0f), new Vector3(x1, Y + H, z0 + 3.4f));
        BraxMiastoA.Kol(kol, "biuro strop", new Vector3(x0, Y + h1 - .1f, z0), new Vector3(x1, Y + h1 + .1f, z1));
        BraxRegion.Warm(root, "Port: światło recepcji", new Vector3(70.3f, Y + 3.2f, 11.5f), 1.2f, 8f);
        BraxMiastoA.Akcja(root, "port_biuro", "ZLECENIE", "Spedytor: Mam kontener do odebrania ze statku — przepustka na trap, odbiór przy dźwigu.", new Vector3(69.6f, Y, 11.5f), 1.8f);
    }
    static Rect[] oparterRects(float dlIn, float dl, bool poludnie) => new Rect[0];
    static Rect[] Odwr(List<Rect> otw, float dl, float off, float dlIn) => otw.Select(o => Rect.MinMaxRect(dlIn - (o.xMax - off), o.yMin, dlIn - (o.xMin - off), o.yMax)).ToArray();
    static Rect[] Odwr2(float dl, float dlIn) => new Rect[0];

    // ---------------- brama z budką strażnika i szlabanem, ogrodzenie panelowe ----------------
    static void Brama()
    {
        var panel = BraxMiastoA.Mat("Port: ogrodzenie panelowe (stal ocynkowana)", new Color(.55f, .57f, .58f), .45f, .8f);
        var biala = BraxMiastoA.Mat("Port: budka biała", new Color(.9f, .9f, .88f), .5f);
        var czerw = BraxMiastoA.Mat("Port: szlaban czerwony", new Color(.8f, .1f, .08f), .5f);
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        float zf = Z1 - .1f, h = 2.0f * K;
        // ogrodzenie: południe (x 66…77.3) i zachód (x 66.1, z −40…−6) — panele 2.5 m ze słupkami i drutami poziomymi/pionowymi
        void Plot(Vector3 a, Vector3 b)
        {
            var d = b - a; float L = d.magnitude; d /= L; var s = BraxUliceKit.S(panel);
            for (float t = 0; t <= L + .01f; t += 2.5f) s.Rura(a + d * t, a + d * t + Vector3.up * h, .04f, 8);
            foreach (float y in new[] { .1f, h * .5f, h - .05f }) s.Rura(a + Vector3.up * y, b + Vector3.up * y, .018f, 6, false);
            for (float t = .2f; t < L; t += .2f) s.Rura(a + d * t + Vector3.up * .1f, a + d * t + Vector3.up * (h - .05f), .006f, 4, false);
            var g = new GameObject("kolizja: ogrodzenie portu"); g.transform.SetParent(kol, false); g.transform.position = (a + b) * .5f + Vector3.up * h * .5f; g.transform.rotation = Quaternion.LookRotation(Vector3.Cross(d, Vector3.up), Vector3.up);
            g.AddComponent<BoxCollider>().size = new Vector3(L, h, .1f);
        }
        Plot(new Vector3(X0 + .1f, Y, Z0 + .2f), new Vector3(77.2f, Y, Z0 + .2f));
        Plot(new Vector3(X0 + .1f, Y, Z0 + .2f), new Vector3(X0 + .1f, Y, -6.2f));
        // budka strażnika (1.6 × 2.0 m × k) z oknami dookoła i daszkiem, przy wjeździe (x 74.6–76.6)
        float bx0 = 74.6f, bx1 = bx0 + 1.6f * K, bz0 = zf - .2f - 2.0f * K, bz1 = zf - .2f, bh = 2.6f * K;
        var bud = BraxUliceKit.S(biala);
        bud.Box(new Vector3(bx0, Y, bz0), new Vector3(bx1, Y + 1.0f, bz1), 4);
        foreach (var (a, b) in new[] { (new Vector3(bx0, Y + 1f, bz0), new Vector3(bx0 + .1f, Y + bh, bz0 + .1f)), (new Vector3(bx1 - .1f, Y + 1f, bz0), new Vector3(bx1, Y + bh, bz0 + .1f)), (new Vector3(bx0, Y + 1f, bz1 - .1f), new Vector3(bx0 + .1f, Y + bh, bz1)), (new Vector3(bx1 - .1f, Y + 1f, bz1 - .1f), new Vector3(bx1, Y + bh, bz1)) }) bud.Box(a, b);
        BraxUliceKit.S(szklo).Box(new Vector3(bx0 + .03f, Y + 1f, bz0 + .1f), new Vector3(bx0 + .07f, Y + bh - .1f, bz1 - .1f)); BraxUliceKit.S(szklo).Box(new Vector3(bx1 - .07f, Y + 1f, bz0 + .1f), new Vector3(bx1 - .03f, Y + bh - .1f, bz1 - .1f));
        BraxUliceKit.S(szklo).Box(new Vector3(bx0 + .1f, Y + 1f, bz1 - .07f), new Vector3(bx1 - .1f, Y + bh - .1f, bz1 - .03f)); BraxUliceKit.S(szklo).Box(new Vector3(bx0 + .1f, Y + 1f, bz0 + .03f), new Vector3(bx1 - .1f, Y + bh - .1f, bz0 + .07f));
        bud.Box(new Vector3(bx0 - .3f, Y + bh - .1f, bz0 - .3f), new Vector3(bx1 + .3f, Y + bh + .1f, bz1 + .3f));
        BraxMiastoA.Kol(kol, "budka strażnika", new Vector3(bx0, Y, bz0), new Vector3(bx1, Y + bh + .1f, bz1));
        // szlaban: słupek z napędem i ramię podniesione (dzień — brama otwarta), pasy czerwono-białe kolorem wierzchołków
        var sl = new Vector3(76.9f, Y, zf - .6f);
        bud.Box(sl + new Vector3(-.2f, 0, -.2f), sl + new Vector3(.2f, 1.0f, .2f));
        var ramie = BraxUliceKit.S(czerw);
        for (int i = 0; i < 6; i++) { ramie.kolor = i % 2 == 0 ? BraxUliceKit.C(.8f, .1f, .08f) : BraxUliceKit.C(.95f, .95f, .93f); var a = sl + Vector3.up * (1.0f + i * .9f); ramie.Rura(a, a + Vector3.up * .9f, .045f, 8); }
        ramie.kolor = BraxUliceKit.Bialy;
        BraxUliceKit.Napis3D("PORT BRAXHORN · BRAMA 1", new Vector3((bx0 + bx1) * .5f, Y + bh + .15f, bz1 + .3f), Vector3.forward, .16f, .03f, BraxMiastoA.Mat("Port: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Port: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f));
        BraxMiastoA.Akcja(root, "port_brama", "POROZMAWIAJ", "Strażnik: Wjazd tylko z przepustką. Biuro spedycji po lewej.", new Vector3(76.0f, Y, zf + .8f), 1.8f);
    }

    static void Maszty()
    {
        var stal = BraxMiastoA.Mat("Port: stal ocynkowana", new Color(.62f, .63f, .64f), .45f, .85f);
        var lampa = BraxMiastoA.Swieci("Port: naświetlacze masztów", new Color(.95f, .95f, .9f), new Color(.6f, .6f, .55f));
        foreach (var p in new[] { new Vector3(82.6f, Y, 1f), new Vector3(82.6f, Y, -28f) })
        {
            BraxUliceKit.S(stal).Walec(p, .5f, .3f, 12); BraxUliceKit.S(stal).Rura(p + Vector3.up * .3f, p + Vector3.up * 22f, .22f, 12);
            BraxUliceKit.S(stal).Box(p + new Vector3(-1.4f, 21.6f, -1.4f), p + new Vector3(1.4f, 21.8f, 1.4f));
            foreach (var d in new[] { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) }) BraxUliceKit.S(lampa).OBox(p + Vector3.up * 21.4f + d * 1.0f, Vector3.Cross(Vector3.up, d), Vector3.up, d, .7f, .4f, .2f);
            var g = new GameObject("kolizja: maszt oświetleniowy"); g.transform.SetParent(kol, false); g.transform.position = p + Vector3.up * 2f; var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .5f; cc.height = 4f;
        }
    }

    static void Ciagniki()
    {
        var zolty = BraxMiastoA.Mat("Port: ciągnik terminalowy (żółty)", new Color(.92f, .7f, .1f), .5f, .2f);
        var czarny = BraxMiastoA.Mat("Port: opony i rama", new Color(.05f, .05f, .055f), .8f);
        var szyby = BraxMiastoA.Mat("Statek: szyby mostka", new Color(.08f, .12f, .15f), .1f, .6f);
        int n = 0;
        foreach (var (p, zKont) in new[] { (new Vector3(80.6f, Y, -11f), true), (new Vector3(80.6f, Y, 4.5f), false) })
        {
            // kabina (z przodu, na północ), rama naczepy, koła, kontener na naczepie (pod dźwigiem)
            var c = BraxUliceKit.S(zolty); var r = BraxUliceKit.S(czarny);
            c.Box(p + new Vector3(-1.25f, .9f, 5.5f), p + new Vector3(1.25f, 3.4f, 7.6f));
            BraxUliceKit.S(szyby).Box(p + new Vector3(-1.1f, 2.2f, 7.6f), p + new Vector3(1.1f, 3.2f, 7.65f));
            r.Box(p + new Vector3(-1.1f, .6f, -7.6f), p + new Vector3(1.1f, .9f, 7.0f));
            foreach (float z in new[] { 6.6f, 4.6f, -4.8f, -6.4f }) foreach (float sg in new[] { -1f, 1f }) r.Rura(p + new Vector3(sg * 1.05f, .55f, z), p + new Vector3(sg * 1.45f, .55f, z), .55f, 14);
            if (zKont) Kontener(p + new Vector3(0, .9f, -.6f), true, true, 777 + n, KolKont[1]);
            BraxMiastoA.Kol(kol, "ciągnik terminalowy", p + new Vector3(-1.5f, 0, -7.6f), p + new Vector3(1.5f, zKont ? 4.3f : 3.4f, 7.6f));
            n++;
        }
    }
}
