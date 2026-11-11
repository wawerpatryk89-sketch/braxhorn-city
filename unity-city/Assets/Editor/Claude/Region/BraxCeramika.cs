using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// CERAMIKA I WYPOSAŻENIE ŁAZIENEK R1 — wspólne dla całego miasta (11.11.2026, właściciel 19:56: „taka jakość, jak jest w łazience,
// to jest sztos… I tak ze wszystkim”; 19:59: „Masz już stworzone jedno, to wystarczy to poprzynosić… toalety… żeby były
// ceramiczne, wszystko”). Wzorzec: WC dworca R1. Pisuar (misa lejem do odpływu w najniższym punkcie), umywalka nablatowa
// (misa toczona z odpływem i sitkiem), bryła toczona, lustro z odbiciem (BraxLustro) — z kodu, wymiary rzeczywiste × K.
// Materiały własne: ceramika szkliwiona (fotoskan white_stucco), chrom (czysty PBR metalu gładkiego), grafit sitek.
internal static class BraxCeramika
{
    static float K => BraxClaudeOsiedle.K;
    static UlSiatka S(Material m) => BraxUliceKit.S(m);
    internal static Material ceramika, chrom, grafit, lustroM;
    internal static void Materialy()
    {
        if (ceramika) return;
        ceramika = BraxUliceKit.Tex("ceramika sanitarna (biała, szkliwiona)", 1.0f, new Color(1f, 1f, .99f), new Color(.97f, .97f, .96f), .12f, "white_stucco", "white_plaster_02");
        chrom = BraxCityMaterials.Surface("armatura — chrom polerowany", new Color(.88f, .89f, .9f), .08f, 1f);
        grafit = BraxUliceKit.Mat("sitko odpływu — grafit", new Color(.16f, .17f, .19f), .5f, .6f);
        var shL = Shader.Find("Braxhorn/Lustro");
        if (shL) { lustroM = new Material(shL) { name = "R1: lustro (tafla z odbiciem)" }; Directory.CreateDirectory(BraxRegion.Out + "/Ceramika"); BraxZapisAssetu.Utworz(lustroM, BraxRegion.Out + "/Ceramika/lustro.mat"); }
        else lustroM = BraxCityMaterials.Surface("R1: lustro", new Color(.78f, .82f, .84f), .05f, .92f);
    }

    /// <summary>Tafla lustra z prawdziwym odbiciem (BraxLustro): środek c, lico w stronę n, w × h, pod rodzicem.</summary>
    public static GameObject Lustro(Transform rodzic, string nazwa, Vector3 c, Vector3 n, float w, float h, bool lokalnie = false)
    {
        Materialy();
        var g = new GameObject("lustro — " + nazwa).transform; g.SetParent(rodzic, false);
        if (lokalnie) { g.localPosition = c; g.localRotation = Quaternion.LookRotation(-n, Vector3.up); } else { g.position = c; g.rotation = Quaternion.LookRotation(-n, Vector3.up); }
        var s = new UlSiatka(); s.Quad(new Vector3(-w * .5f, -h * .5f, 0f), new Vector3(w * .5f, -h * .5f, 0f), new Vector3(w * .5f, h * .5f, 0f), new Vector3(-w * .5f, h * .5f, 0f), Vector3.back);
        var t = BraxMiastoA.Obiekt(g, "lustro — " + nazwa + " (tafla)", s, lustroM);
        t.AddComponent<BraxLustro>();
        return t;
    }

    // PISUAR WISZĄCY (11.11, właściciel 19:39 „te takie dwie… pisuary, czy co to jest?”) — ceramika z kodu jak w życiu, wymiary
    // rzeczywiste × K: szer. 36 cm, wys. 66 cm (spód 46 cm, warga misy 58 cm nad posadzką), głębokość 33.5 cm przy wardze.
    // Jedna gładka powłoka: obrys przy ścianie (superelipsa) → boki wychodzą prostopadle ze ściany → zaokrąglona warga (r 1.8 cm)
    // → misa (krzywa Béziera od wargi: w głąb ku ścianie, potem w dół) zbiega lejem do odpływu w NAJNIŻSZYM punkcie — w dnie misy,
    // 7 cm PONIŻEJ dolnej wargi, poziomo (właściciel 19:52–19:59: „odpływ na dole… nie ma szans, żeby woda spłynęła”; woda
    // z każdego miejsca misy spływa w dół do odpływu) → studzienka 2 cm → sitko poziome (pierścienie chrom/grafit,
    // równo w jednej płaszczyźnie — bez nakładania). Nad pisuarem zawór spłukujący (chrom): rura z kołpaka, korpus, dźwignia,
    // przyłącze do ściany z rozetą. Bez tylnej ściany — korpus przylega do płytek (zasada zero migotania).
    // o: punkt na licu ściany na poziomie posadzki pod środkiem pisuaru; ex: wzdłuż ściany; en: od ściany do pomieszczenia.
    public static void Pisuar(Vector3 o, Vector3 ex, Vector3 en)
    {
        const int M = 72;
        const float pO = 2.4f, pR = 2.6f, rl = .018f, rd = .024f, yD = .525f, zD = .175f;   // odpływ: Ø 4.8 cm × K, dno misy 52.5 cm
        var Bc = new Vector2(0f, .77f);             // środek wargi (kierunek zaokrąglenia do wnętrza)
        float Se(float c, float p) => Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p);
        Vector2 Obrys(float th) { float c = Mathf.Cos(th), s = Mathf.Sin(th); return new Vector2(.18f * (1f - .06f * (1f - s)) * Se(c, pO), .79f + .33f * Se(s, pO)); }
        Vector2 Warga(float th) { float c = Mathf.Cos(th), s = Mathf.Sin(th); return new Vector2(.14f * (1f - .08f * (1f - s)) * Se(c, pR), .77f + .19f * Se(s, pR)); }
        float Zr(float th) => .25f - .085f * Mathf.Sin(th);
        Vector3 W(Vector2 q, float z) => o + (ex * q.x + Vector3.up * q.y + en * z) * K;
        // segmenty profilu: 0 boki (ściana → szczyt wargi), 1 warga, 2 misa, 3 studzienka odpływu
        Vector3 P(int seg, float th, float t)
        {
            Vector2 O = Obrys(th), R = Warga(th); float zr = Zr(th); var dIn = (Bc - R).normalized; var Rin = R + dIn * rl;
            switch (seg)
            {
                case 0: { float f = t * Mathf.PI * .5f; return W(O + (R - O) * (1f - Mathf.Cos(f)), -.004f + (zr + .004f) * Mathf.Sin(f)); }
                case 1: { float a = t * Mathf.PI * .5f; return W(R + dIn * (rl * Mathf.Sin(a)), zr - rl + rl * Mathf.Cos(a)); }
                case 2:
                {
                    // Bézier: od wargi w głąb (styczna −en jak koniec wargi), potem pionowo w dół do okręgu odpływu (styczna −y)
                    float sn = Mathf.Sin(th), z0 = zr - rl;
                    var B0 = new Vector3(Rin.x, Rin.y, z0);
                    var B1 = new Vector3(Rin.x, Rin.y, Mathf.Lerp(z0, .06f, .55f + .25f * sn));
                    var B3 = new Vector3(rd * Mathf.Cos(th), yD, zD - rd * sn);
                    var B2 = B3 + Vector3.up * ((Rin.y - yD) * .6f);
                    float u = 1f - t; var q = u * u * u * B0 + 3f * u * u * t * B1 + 3f * u * t * t * B2 + t * t * t * B3;
                    return W(new Vector2(q.x, q.y), q.z);
                }
                default: { float sn = Mathf.Sin(th); return W(new Vector2(rd * Mathf.Cos(th), Mathf.Lerp(yD, yD - .02f, t)), zD - rd * sn); }
            }
        }
        int[] podzial = { 10, 5, 16, 1 };
        Materialy(); var s = S(ceramika);
        // orientacja: na szczycie wargi (dół misy) normalna patrzy od ściany (+en)
        float Zn(int seg, float th, float t)
        {
            const float e = 1e-3f;
            var a = (P(seg, th + e, t) - P(seg, th - e, t)) / e;
            var b = (P(seg, th, Mathf.Min(1f, t + e)) - P(seg, th, Mathf.Max(0f, t - e))) / e;
            return Vector3.Dot(Vector3.Cross(a, b), en);
        }
        float znak = Zn(0, -Mathf.PI * .5f, 1f) >= 0f ? 1f : -1f;
        for (int seg = 0; seg < 4; seg++)
        {
            int nT = podzial[seg], k0 = s.v.Count; float dl = 0f;
            for (int j = 0; j <= M; j++)
            {
                float th = -Mathf.PI * .5f + j * Mathf.PI * 2f / M; dl = 0f;
                for (int q = 0; q <= nT; q++)
                {
                    float t = (float)q / nT; var p = P(seg, th, t);
                    if (q > 0) dl += Vector3.Distance(p, P(seg, th, (float)(q - 1) / nT));
                    const float e = 1e-3f;
                    // pochodne podzielone przez krok (różnice rzędu 1e-3 m: iloczyn wektorowy poniżej progu Normalize w Unity)
                    var a = (P(seg, th + e, t) - P(seg, th - e, t)) / e;
                    var b = (P(seg, th, Mathf.Min(1f, t + e)) - P(seg, th, Mathf.Max(0f, t - e))) / e;
                    var nn = Vector3.Cross(a, b) * znak; nn = nn.sqrMagnitude > 1e-8f ? nn.normalized : en;
                    var tg = a.sqrMagnitude > 1e-8f ? a.normalized : ex;
                    s.v.Add(p); s.n.Add(nn); s.t.Add(new Vector4(tg.x, tg.y, tg.z, 1f)); s.c.Add(s.kolor);
                    s.uv.Add(new Vector2(j * 1.4f * K / M, dl + seg * .3f));
                }
            }
            for (int j = 0; j < M; j++)
                for (int q = 0; q < nT; q++)
                {
                    int a0 = k0 + j * (nT + 1) + q, a1 = a0 + 1, b0 = a0 + nT + 1, b1 = b0 + 1;
                    var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                    if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                    else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
                }
        }
        // sitko odpływu poziome w dnie misy: koło dzielone na pierścienie chrom / grafit (jedna płaszczyzna, bez nakładania)
        var cS = W(new Vector2(0f, yD - .02f), zD);
        float[] pas = { 0f, .22f, .34f, .54f, .66f, 1f };
        for (int b = 0; b < pas.Length - 1; b++)
        {
            var sm = S(b % 2 == 0 ? chrom : grafit);
            if (b == 0) sm.Kolo(cS, rd * K * pas[1], M); else sm.Pierscien(cS, rd * K * pas[b], rd * K * pas[b + 1], 0f, 360f, M);
        }
        // zawór spłukujący nad pisuarem (chrom): rura z kołpaka (wchodzi 2.5 cm w ceramikę), korpus, dźwignia, przyłącze w ścianę z rozetą
        var ch = S(chrom); float zz = .07f;
        ch.Rura(W(new Vector2(0f, 1.08f), zz), W(new Vector2(0f, 1.33f), zz), .016f * K, 14);
        ch.Rura(W(new Vector2(0f, 1.30f), zz), W(new Vector2(0f, 1.40f), zz), .028f * K, 18);
        ch.PoduchaOkragla(W(new Vector2(0f, 1.40f), zz), .028f * K, .018f * K, .008f * K, 0f, 18);
        ch.Rura(W(new Vector2(.01f, 1.345f), zz), W(new Vector2(.10f, 1.345f), zz), .007f * K, 10);           // dźwignia (koniec w gałce)
        ch.Rura(W(new Vector2(.095f, 1.345f), zz), W(new Vector2(.125f, 1.345f), zz), .012f * K, 12);         // gałka
        ch.Rura(W(new Vector2(0f, 1.37f), zz), W(new Vector2(0f, 1.37f), -.02f), .016f * K, 14);
        ch.Rura(W(new Vector2(0f, 1.37f), -.025f), W(new Vector2(0f, 1.37f), .016f), .034f * K, 20);          // rozeta 2 cm przed płytkami
    }

    // UMYWALKA NABLATOWA (11.11, właściciel 19:53: „czy mają dziurę… odpływ, czy są jak talerze”) — misa ceramiczna toczona z kodu:
    // Ø 38 cm × K, wys. 13 cm × K; ścianka zewnętrzna od blatu (podstawa Ø 0.62) wybrzuszona do krawędzi, zaokrąglona krawędź
    // (r 1.2 cm), wnętrze schodzi do płaskiego dna 4.5 cm nad blatem, odpływ Ø 5.6 cm × K z sitkiem 2.5 cm nad blatem (pierścienie
    // chrom / grafit w jednej płaszczyźnie). Spód nie jest budowany — misa stoi na blacie (zasada zero migotania).
    public static void Umywalka(Vector3 p, float skala = 1f)
    {
        float R = .19f * K * skala, H = .13f * K * skala, rr = .012f * K, rb = R * .62f, rd = .022f * K, yIn = .045f, ySit = .025f;
        const int seg = 48;
        var segmenty = new List<List<Vector2>>();
        List<Vector2> L(int n, System.Func<float, Vector2> f) { var l = new List<Vector2>(); for (int i = 0; i <= n; i++) l.Add(f((float)i / n)); return l; }
        segmenty.Add(L(10, u => { float a = u * Mathf.PI * .5f; return new Vector2(rb + (R - rb) * Mathf.Sin(a), (H - rr) * (1f - Mathf.Cos(a))); }));          // ścianka zewn.
        segmenty.Add(L(8, u => { float b = u * Mathf.PI; return new Vector2(R - rr + rr * Mathf.Cos(b), H - rr + rr * Mathf.Sin(b)); }));                   // krawędź
        segmenty.Add(L(12, u => { float a = (1f - u) * Mathf.PI * .5f; return new Vector2(rd + (R - 2f * rr - rd) * Mathf.Sin(a), yIn + (H - rr - yIn) * (1f - Mathf.Cos(a))); }));   // wnętrze i dno
        segmenty.Add(L(1, u => new Vector2(rd, Mathf.Lerp(yIn, ySit, u))));                                                                                  // studzienka
        Materialy(); var s = S(ceramika);
        foreach (var pr in segmenty) Toczony(s, p, pr, seg);
        float[] pas = { 0f, .22f, .34f, .54f, .66f, 1f };
        for (int b = 0; b < pas.Length - 1; b++)
        {
            var sm = S(b % 2 == 0 ? chrom : grafit);
            if (b == 0) sm.Kolo(p + Vector3.up * ySit, rd * pas[1], seg);
            else sm.Pierscien(p + Vector3.up * ySit, rd * pas[b], rd * pas[b + 1], 0f, 360f, seg);
        }
    }

    /// <summary>Bryła toczona wokół pionu przez p: profil (r, y) — normalna po prawej stronie kierunku profilu (ścianka zewn. w górę
    /// = na zewnątrz, wnętrze w dół = do osi); UV w metrach (obwód × długość profilu).</summary>
    public static void Toczony(UlSiatka s, Vector3 p, List<Vector2> pr, int seg)
    {
        int P = pr.Count, k0 = s.v.Count; var dl = new float[P];
        for (int j = 1; j < P; j++) dl[j] = dl[j - 1] + Vector2.Distance(pr[j - 1], pr[j]);
        for (int i = 0; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg; var kier = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            for (int j = 0; j < P; j++)
            {
                var d = pr[Mathf.Min(P - 1, j + 1)] - pr[Mathf.Max(0, j - 1)]; var n2 = new Vector2(d.y, -d.x).normalized;
                s.v.Add(p + kier * pr[j].x + Vector3.up * pr[j].y); s.n.Add((kier * n2.x + Vector3.up * n2.y).normalized);
                s.t.Add(new Vector4(tan.x, tan.y, tan.z, 1f)); s.c.Add(s.kolor); s.uv.Add(new Vector2(a * Mathf.Max(.05f, pr[j].x), dl[j]));
            }
        }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < P - 1; j++)
            {
                int a0 = k0 + i * P + j, a1 = a0 + 1, b0 = a0 + P, b1 = b0 + 1;
                var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
            }
    }

    // FOTEL FRYZJERSKI (11.11, salon fryzjerski miasta B i ulicy) — z kodu, wymiary rzeczywiste × K: podstawa chrom Ø 60 cm, kolumna
    // hydrauliki, siedzisko i oparcie ze skóry (krawędzie r 4–5 cm, lekko wypchnięte), zagłówek na pręcie, podłokietniki na wspornikach,
    // podnóżek. Ustawienie tylko przodem wzdłuż osi świata (±x / ±z) — bryły osiowe. p = środek podstawy na posadzce, przod = kierunek
    // patrzenia klienta (do lustra).
    static Material skoraF;
    public static void FotelFryzjerski(Vector3 p, Vector3 przod, Transform kolizje)
    {
        Materialy();
        if (!skoraF) skoraF = BraxUliceKit.Tex("fotel fryzjerski: skóra czarna", .8f, new Color(.32f, .3f, .3f), new Color(.08f, .08f, .08f), .45f, "brown_leather", "fabric_leather_02");
        przod.y = 0f; przod.Normalize(); var prawo = Vector3.Cross(Vector3.up, przod);
        Vector3 L(float x, float y, float z) => p + (prawo * x + Vector3.up * y + przod * z) * K;
        void Bryla(Material m, float x0, float y0, float z0, float x1, float y1, float z1, float r, float puf)
        { var a = L(x0, y0, z0); var b = L(x1, y1, z1); S(m).Poducha(Vector3.Min(a, b), Vector3.Max(a, b), r, puf, 6); }
        var ch = S(chrom);
        ch.PoduchaOkragla(p, .3f * K, .035f * K, .012f, 0f, 32);                                          // podstawa
        ch.Rura(L(0f, .035f, 0f), L(0f, .40f, 0f), .065f * K, 20);                                        // kolumna hydrauliki
        Bryla(grafit, -.25f, .37f, -.22f, .25f, .42f, .26f, .01f, 0f);                                    // rama siedziska
        Bryla(skoraF, -.27f, .42f, -.24f, .27f, .54f, .29f, .05f, .015f);                                 // siedzisko
        Bryla(skoraF, -.26f, .56f, -.33f, .26f, 1.22f, -.2f, .05f, .012f);                                // oparcie
        Bryla(skoraF, -.13f, 1.31f, -.31f, .13f, 1.46f, -.21f, .04f, .01f);                               // zagłówek
        ch.Rura(L(0f, 1.18f, -.27f), L(0f, 1.33f, -.27f), .012f * K, 10);
        foreach (float sx in new[] { -1f, 1f })
        {
            Bryla(skoraF, sx * .275f, .68f, -.22f, sx * .37f, .75f, .27f, .025f, .006f);                  // podłokietnik
            foreach (float zz in new[] { -.14f, .2f }) ch.Rura(L(sx * .29f, .44f, zz), L(sx * .32f, .69f, zz), .011f * K, 10);
        }
        ch.Rura(L(0f, .3f, .05f), L(0f, .17f, .43f), .016f * K, 12);                                      // ramię podnóżka
        Bryla(chrom, -.2f, .14f, .42f, .2f, .17f, .56f, .012f, 0f);                                       // podnóżek
        if (kolizje) BraxMiastoA.Kol(kolizje, "fotel fryzjerski", L(-.37f, 0f, -.33f), L(.37f, 1.46f, .56f));
    }

    // MISKA WC WISZĄCA (11.11 — kompakt „jak w życiu” zamiast dwóch poduszek): korpus ceramiczny z kodu, przekroje zaokrąglonego
    // prostokąta (superelipsa p=3) wzdłuż głębokości 54 cm × K, z góry owal zwężający się do przodu, spód podnosi się ku przodowi;
    // deska (2 cm) i klapa zamknięta (2.5 cm) z tworzywa (biel), zawiasy chrom, przycisk spłuczki na ścianie (płytka 2.5 cm z dwoma
    // przyciskami). Wymiary rzeczywiste × K: szer. 36, wys. krawędzi 40, głęb. 54 cm. o: punkt na licu ściany na posadzce pod osią miski.
    static Material deskaM;
    public static void MiskaWiszaca(Vector3 o, Vector3 ex, Vector3 en)
    {
        Materialy();
        if (!deskaM) deskaM = BraxUliceKit.Mat("deska i klapa WC (tworzywo białe, połysk)", new Color(.97f, .97f, .96f), .2f, 0f);
        const int M = 40, N = 18; const float zf = .54f, yt = .40f, p = 3f;
        Vector3 W(float x, float y, float z) => o + (ex * x + Vector3.up * y + en * z) * K;
        float Se(float c) => Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p);
        float A(float z) => z <= .2f ? .18f : .18f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((z - .2f) / (zf - .2f), 2f)));
        float Yb(float z) => Mathf.Lerp(.17f, .27f, z / zf);
        // przekrój w głębokości z: pół-szerokość A(z), pół-wysokość Hh(z) (z przodu maleje do zera — nos zamknięty w punkcie u góry)
        float Zt(float t) => (1f - Mathf.Pow(1f - t, 1.6f)) * zf - .004f * (1f - t);
        float Hh(float z) => (yt - Yb(z)) * .5f * (z <= .2f ? 1f : Mathf.Sqrt(A(z) / .18f));
        Vector3 P(float fi, float t)
        {
            float z = Zt(t), a = A(z), hh = Hh(z);
            return W(a * Se(Mathf.Cos(fi)), yt - hh + hh * Se(Mathf.Sin(fi)), z);
        }
        var s = S(ceramika); int k0 = s.v.Count;
        for (int j = 0; j <= M; j++)
            for (int q = 0; q <= N; q++)
            {
                float fi = j * Mathf.PI * 2f / M, t = (float)q / N; const float e = 1e-3f;
                var pp = P(fi, t);
                var a = (P(fi + e, t) - P(fi - e, t)) / e; var b = (P(fi, Mathf.Min(1f, t + e)) - P(fi, Mathf.Max(0f, t - e))) / e;
                var nn = Vector3.Cross(b, a); if (Vector3.Dot(nn, pp - W(0f, .33f, .22f)) < 0f) nn = -nn;   // na zewnątrz od punktu wnętrza korpusu (bryła gwiaździsta)
                nn = nn.sqrMagnitude > 1e-8f ? nn.normalized : en;
                s.v.Add(pp); s.n.Add(nn); var tg = a.sqrMagnitude > 1e-8f ? a.normalized : ex; s.t.Add(new Vector4(tg.x, tg.y, tg.z, 1f)); s.c.Add(s.kolor);
                s.uv.Add(new Vector2(j * 1.1f * K / M, t * zf * K));
            }
        for (int j = 0; j < M; j++)
            for (int q = 0; q < N; q++)
            {
                int a0 = k0 + j * (N + 1) + q, a1 = a0 + 1, b0 = a0 + N + 1, b1 = b0 + 1;
                var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
            }
        // deska i klapa: graniastosłupy po obrysie z góry (owal), deska 2 cm, klapa 2.5 cm (zamknięta), bez wspólnych płaszczyzn
        void Plyta(UlSiatka u, float y0, float y1, float ins, float zp)
        {
            var obw = new List<Vector2>();
            for (int i = 0; i <= 24; i++) { float z = zp + (zf - .02f - zp) * i / 24f; obw.Add(new Vector2(A(z) - ins, z)); }
            for (int i = 24; i >= 0; i--) { float z = zp + (zf - .02f - zp) * i / 24f; obw.Add(new Vector2(-(A(z) - ins), z)); }
            var c = Vector2.zero; foreach (var q in obw) c += q; c /= obw.Count;
            for (int i = 0; i < obw.Count; i++)
            {
                var q0 = obw[i]; var q1 = obw[(i + 1) % obw.Count];
                Vector3 g0 = W(q0.x, y0, q0.y), g1 = W(q1.x, y0, q1.y), h0 = W(q0.x, y1, q0.y), h1 = W(q1.x, y1, q1.y);
                var bok = Vector3.Cross(Vector3.up, g1 - g0).normalized; if (Vector3.Dot(bok, g0 - W(c.x, y0, c.y)) < 0f) bok = -bok;
                u.Quad(g0, g1, h1, h0, bok);
                u.Tri(W(c.x, y1, c.y), h0, h1, Vector3.up); u.Tri(W(c.x, y0, c.y), g0, g1, Vector3.down);
            }
        }
        var de = S(deskaM);
        Plyta(de, yt, yt + .02f, .004f, .06f);
        Plyta(de, yt + .02f, yt + .045f, .008f, .05f);
        var ch = S(chrom);
        foreach (float sx in new[] { -.1f, .1f }) ch.Rura(W(sx - .025f, yt + .035f, .045f), W(sx + .025f, yt + .035f, .045f), .012f * K, 10);
        // przycisk spłuczki na ścianie: płytka 25 × 16 cm × K, 2.5 cm przed licem, dwa przyciski (mały / duży) wysunięte 2 cm
        ch.Poducha(Vector3.Min(W(-.125f, .98f, -.002f), W(.125f, 1.14f, .025f)), Vector3.Max(W(-.125f, .98f, -.002f), W(.125f, 1.14f, .025f)), .008f, 0f, 4);
        var pz = S(deskaM);
        pz.Poducha(Vector3.Min(W(-.105f, 1.0f, .025f), W(-.01f, 1.12f, .045f)), Vector3.Max(W(-.105f, 1.0f, .025f), W(-.01f, 1.12f, .045f)), .01f, 0f, 4);
        pz.Poducha(Vector3.Min(W(.01f, 1.0f, .025f), W(.105f, 1.12f, .045f)), Vector3.Max(W(.01f, 1.0f, .025f), W(.105f, 1.12f, .045f)), .01f, 0f, 4);
    }

    // BATERIA UMYWALKOWA Z WODĄ (11.11, właściciel 20:00: „woda w kranie leci, wszystko po kolei robimy funkcjonalne”): korpus
    // chrom (kolumna, wylewka z perlatorem), strumień wody (przezroczysty walec z kodu) od perlatora do misy — BraxKran włącza go,
    // gdy gracz stoi przy umywalce (≤ 1.1 m), szum wody. baza = punkt blatu pod kolumną, wysieg = kierunek wylewki, hMisy = wysokość
    // dna misy nad blatem (koniec strumienia).
    static Material wodaM;
    public static void Bateria(Transform rodzic, string nazwa, Vector3 baza, Vector3 wysieg, float wys, float dl, float hMisy, bool lokalnie = false)
    {
        Materialy();
        if (!wodaM) wodaM = BraxUliceKit.Mat("woda z kranu (strumień)", new Color(.78f, .88f, .95f, .45f), .05f, 0f);
        wysieg.y = 0f; wysieg.Normalize();
        var ch = S(chrom);
        ch.PoduchaOkragla(baza, .028f * K, .022f, .006f, 0f, 18);                                    // rozeta na blacie (2.2 cm — lico ≥ 2 cm nad blatem)
        ch.Rura(baza + Vector3.up * .01f, baza + Vector3.up * wys, .018f * K, 16);                     // kolumna
        ch.Rura(baza + Vector3.up * (wys - .012f) - wysieg * .02f, baza + Vector3.up * (wys - .012f) + wysieg * dl, .016f * K, 14);   // wylewka
        var perl = baza + Vector3.up * (wys - .012f) + wysieg * (dl - .012f);
        ch.Rura(perl, perl - Vector3.up * .035f, .012f * K, 12);                                      // perlator
        ch.Rura(baza + Vector3.up * (wys - .005f), baza + Vector3.up * (wys + .03f) - wysieg * .06f, .009f * K, 10);   // dźwignia mieszacza na górze, skośnie do tyłu
        // strumień: osobny obiekt (włączany przez BraxKran), walec Ø 1.2 cm od perlatora do dna misy
        var g = new GameObject("Kran: " + nazwa).transform; g.SetParent(rodzic, false); if (lokalnie) g.localPosition = perl; else g.position = perl;   // lokalnie: współrzędne w układzie rodzica (siatki partii pod nim)
        var w = new UlSiatka(); w.Rura(Vector3.down * .036f, Vector3.down * Mathf.Max(.05f, (wys - .012f - .036f - hMisy)), .006f * K, 10, false);
        var go = BraxMiastoA.Obiekt(g, "Kran: " + nazwa + " — strumień wody", w, wodaM);
        var kran = g.gameObject.AddComponent<BraxKran>(); kran.strumien = go.GetComponent<Renderer>(); kran.promien = 1.1f * K;
        GameObjectUtility.SetStaticEditorFlags(go, 0);
    }

    // ZESTAW UMYWALKOWY (11.11 — do każdej łazienki i WC miasta): blat dębowy wiszący (szer × 0.5 m × K, 0.85 m × K nad posadzką),
    // umywalka nablatowa z odpływem, bateria z wodą (BraxKran), lustro z prawdziwym odbiciem w ramie chrom. p = punkt na licu ściany
    // na posadzce pod środkiem blatu, wzdluz = kierunek wzdłuż ściany (oś świata), od = od ściany do pomieszczenia (oś świata).
    static Material dabB;
    public static void ZestawUmywalkowy(Transform rodzic, string nazwa, Vector3 p, Vector3 wzdluz, Vector3 od, float szer)
    {
        Materialy();
        if (!dabB) dabB = BraxUliceKit.Tex("blat łazienki: okleina dębowa", 1.2f, null, new Color(.6f, .45f, .3f), .55f, "oak_veneer_01", "teak_veneer");
        float gl = .5f * K, yb = .85f * K, sk = Mathf.Clamp(szer / (.62f * K), .7f, 1f);
        Vector3 L(float a, float y, float d) => p + wzdluz * a + Vector3.up * y + od * d;
        void Br(Material m, float a0, float a1, float y0, float y1, float d0, float d1, float r)
        { var A = L(a0, y0, d0); var B = L(a1, y1, d1); S(m).Poducha(Vector3.Min(A, B), Vector3.Max(A, B), r, 0f, 4); }
        Br(dabB, -szer * .5f, szer * .5f, yb - .07f, yb, .005f, gl, .012f);                                   // blat (5 mm od ściany)
        Br(dabB, -szer * .5f + .02f, szer * .5f - .02f, yb - .32f, yb - .07f, .005f, gl - .04f, .01f);       // szafka pod blatem
        Umywalka(L(0f, yb, gl * .56f), sk);
        Bateria(rodzic, nazwa, L(0f, yb, .075f), od, .28f, .13f, .045f);
        float ly0 = yb + .33f, ly1 = yb + .33f + .8f * K, lw = Mathf.Min(szer, .8f * K);
        Lustro(rodzic, nazwa, L(0f, (ly0 + ly1) * .5f, .03f), od, lw - .06f, ly1 - ly0 - .06f);
        foreach (var (a0, a1, y0, y1) in new[] { (-lw * .5f, -lw * .5f + .03f, ly0, ly1), (lw * .5f - .03f, lw * .5f, ly0, ly1), (-lw * .5f + .03f, lw * .5f - .03f, ly0, ly0 + .03f), (-lw * .5f + .03f, lw * .5f - .03f, ly1 - .03f, ly1) })
            Br(chrom, a0, a1, y0, y1, 0f, .045f, .005f);
    }
}
