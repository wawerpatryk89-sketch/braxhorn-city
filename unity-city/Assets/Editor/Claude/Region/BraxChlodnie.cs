using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// CHŁODNIE R1 — z kodu, wspólne dla całego miasta (11.11.2026). Właściciel 19:44: „sklep mięsny… mięsa są na półkach. A mają być
// w lodówkach. Stwórz normalnie jakieś lodówki szklone. Sam możesz to zrobić w kodzie… Tak jak to powinno być w normalnym życiu”;
// 20:53: „te takie lodówki tam widzę z napojami… puste, jakieś dziwne, te modele 3D, które ja ściągnąłem. I lepiej jak ty
// stworzysz taką lodówkę z napojami”. Trzy meble, wymiary rzeczywiste × K (Brax 2.33 m):
//  • LODÓWKA Z NAPOJAMI — 1–3 drzwi po 0.70 m, gł. 0.62 m, wys. 2.06 m: boki z blachy lakierowanej (krawędzie zaokrąglone),
//    drzwi w czarnej ramie aluminiowej z szybą zespoloną, pionowy uchwyt ze stali na dwóch wspornikach, słupek między drzwiami,
//    plecy z mlecznego panelu podświetlonego, pionowe listwy LED przy krawędziach, 4 półki z listwą cenową (cenówki), cokół
//    z kratką wentylacyjną, kaseton u góry z literami LED „ZIMNE NAPOJE”. W środku NAPOJE Z KODU, w rzędach jak w sklepie (po
//    2–4 sztuki tego samego obok siebie, dwa rzędy w głąb): butelki PET 1.5 l (dół), PET 0.5 l, puszki 0.33 i 0.5 l, butelki
//    szklane — bryły toczone z płynem w kolorze napoju, nakrętką, pustą szyjką; puszki z rantem i wieczkiem; ETYKIETY Z NAZWAMI
//    (atlas z kodu: BRAXHORN COLA / ZERO / ISO / ENERGY / POWER, WAWER WODA / TONIK / LAGER / PILS, SOBCZAK ORANŻADA / LEMONIADA /
//    ICE TEA / LIMONKA).
//  • SZAFA CHŁODNICZA Z MIĘSEM — ta sama obudowa (kaseton „MIĘSO”), na półkach mięso z biblioteki właściciela (BraxSklepyTowar:
//    steki, łosoś, kurczak, mięso, kiełbasy, parówki, sery); bez paczki — tacki z mięsem z kodu.
//  • LADA CHŁODNICZA (mięsny) — korpus ze stali nierdzewnej na cofniętym cokole, odbojnica na wspornikach od klienta, policzki
//    z czarnego aluminium w kształcie szyby, szyba frontowa GIĘTA (ćwiartka elipsy) i szyba górna, belka z listwą LED nad
//    ekspozycją, wanna ekspozycyjna ze stali z mięsem w dwóch rzędach, cenówki na szpilkach, od sprzedawcy blat roboczy z deską
//    do krojenia i wagą z wyświetlaczem z obu stron.
// Zero migotania: równoległe nakładające się lica w odstępie ≥ 2 cm albo przeciwne (niewidoczne), szyby tylko w otworach ram.
// Siatki budowane RAZ na wariant (pamięć podręczna) i stawiane jako obiekty z obrotem — dowolny kierunek ściany, jedna siatka
// dla wielu lodówek w mieście (mało pamięci w APK). Towar z paczki stawiany osobno (modele właściciela).
internal static class BraxChlodnie
{
    static float K => BraxClaudeOsiedle.K;
    const string Katalog = BraxRegion.Out + "/Chlodnie";
    static Material blacha, stal, czarny, szklo, led, plecy, napisM, pet, puszka, szkloBut, kratka, mieso, tacka, ekran, deska, etykiety;

    static void Materialy()
    {
        if (blacha) return;
        blacha = BraxCityMaterials.Surface("chłodnie: blacha lakierowana biała", new Color(.93f, .94f, .95f), .3f, .25f);
        stal = BraxCityMaterials.Surface("chłodnie: stal nierdzewna szlifowana", new Color(.78f, .79f, .81f), .22f, 1f);
        czarny = BraxUliceKit.Tex("drzwi lokalu: czarne aluminium anodowane", 1.2f, new Color(.075f, .075f, .085f), new Color(.06f, .06f, .07f), .45f, "blue_metal_plate", "metal_plate");
        szklo = BraxUliceKit.SzkloPrzezroczyste;
        led = BraxMiastoA.Swieci("chłodnie: listwa LED (biała zimna)", Color.white, new Color(1.7f, 1.75f, 1.8f));
        plecy = BraxMiastoA.Swieci("chłodnie: plecy podświetlone (panel mleczny)", new Color(.93f, .95f, .97f), new Color(.5f, .53f, .56f));
        napisM = BraxMiastoA.Swieci("chłodnie: litery LED kasetonu", Color.white, new Color(1.4f, 1.4f, 1.4f));
        ekran = BraxMiastoA.Swieci("chłodnie: wyświetlacz wagi", new Color(.2f, .45f, .3f), new Color(.25f, .9f, .45f));
        pet = BraxUliceKit.Mat("napoje: butelki PET (połysk)", Color.white, .12f, 0f);
        puszka = BraxUliceKit.Mat("napoje: puszki (aluminium lakierowane)", Color.white, .22f, .65f);
        szkloBut = BraxUliceKit.Mat("napoje: butelki szklane", Color.white, .08f, 0f);
        kratka = BraxUliceKit.Mat("chłodnie: wnęka cokołu (czerń)", new Color(.035f, .035f, .04f), .8f, 0f);
        mieso = BraxUliceKit.Mat("chłodnie: mięso z kodu (zapas bez paczki sklepów)", Color.white, .5f, 0f);
        tacka = BraxUliceKit.Mat("chłodnie: tacki i cenówki", Color.white, .45f, 0f);
        deska = BraxUliceKit.Tex("chłodnie: deska do krojenia (dąb)", .6f, null, new Color(.62f, .48f, .32f), .55f, "oak_veneer_01", "wood_table_001");
        etykiety = AtlasEtykiet();
    }

    static Color32 C(float r, float g, float b) => new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);

    // ---------------------------------------------------------------- wzory: siatki raz na wariant, wiele kopii
    sealed class Wzor { public Mesh[] siatki; public Material[] mat; }
    sealed class Partie
    {
        public readonly Dictionary<Material, UlSiatka> d = new Dictionary<Material, UlSiatka>();
        public UlSiatka this[Material m] { get { if (!d.TryGetValue(m, out var s)) d[m] = s = new UlSiatka(); return s; } }
    }
    static readonly Dictionary<string, Wzor> wzory = new Dictionary<string, Wzor>();
    public static int Siatek, Kopii;

    static Wzor Wez(string klucz, System.Action<Partie> buduj)
    {
        if (wzory.TryGetValue(klucz, out var w) && w.siatki.All(m => m)) return w;
        Materialy();
        var p = new Partie(); buduj(p);
        Directory.CreateDirectory(Katalog);
        var ls = new List<Mesh>(); var lm = new List<Material>();
        foreach (var kv in p.d)
        {
            var s = kv.Value; if (s.Wierzcholki == 0) continue;
            for (int i = 0; i < s.v.Count; i++) s.v[i] *= K;   // budowa w metrach rzeczywistych → skala świata (× K)
            var mesh = s.Zbuduj("chłodnia: " + klucz + " / " + kv.Key.name);
            BraxZapisAssetu.Utworz(mesh, Katalog + "/" + BraxUliceKit.Bezpieczna(klucz) + "_" + ls.Count + ".asset");
            ls.Add(mesh); lm.Add(kv.Key); Siatek++;
        }
        return wzory[klucz] = new Wzor { siatki = ls.ToArray(), mat = lm.ToArray() };
    }

    static Transform Wstaw(Transform rodzic, string nazwa, Wzor w, Vector3 lokalnie, bool cien)
    {
        var g = new GameObject(nazwa).transform; g.SetParent(rodzic, false); g.localPosition = lokalnie;
        for (int i = 0; i < w.siatki.Length; i++)
        {
            var c = new GameObject(w.mat[i].name); c.transform.SetParent(g, false);
            c.AddComponent<MeshFilter>().sharedMesh = w.siatki[i];
            var r = c.AddComponent<MeshRenderer>(); r.sharedMaterial = w.mat[i];
            r.shadowCastingMode = cien && w.mat[i].renderQueue < 2900 ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
        }
        Kopii++;
        return g;
    }

    static Transform Korzen(Transform rodzic, string nazwa, Vector3 p, Vector3 przodLokalneZ)
    {
        var g = new GameObject(nazwa).transform; g.SetParent(rodzic, false);
        przodLokalneZ.y = 0f; g.position = p; g.rotation = Quaternion.LookRotation(przodLokalneZ.normalized, Vector3.up);
        return g;
    }

    static Vector3 Sw(Transform t, float x, float y, float z) => t.position + t.rotation * (new Vector3(x, y, z) * K);

    static void Kolizja(Transform t, string nazwa, Vector3 lo, Vector3 hi)
    {
        var g = new GameObject("kolizja: " + nazwa); g.transform.SetParent(t, false);
        var bc = g.AddComponent<BoxCollider>(); bc.center = (lo + hi) * .5f * K; bc.size = (hi - lo) * K;
    }

    static void Lod(Transform t, float metry)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
        var lod = t.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(Mathf.Clamp(2.4f * K / (2f * metry * .52f), .01f, .5f), rs) }); lod.RecalculateBounds();
    }

    // obie strony tafli (szyba widziana z obu stron)
    static void Szyba(UlSiatka s, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n) { s.Quad(a, b, c, d, n); s.Quad(d, c, b, a, -n); }

    // ---------------------------------------------------------------- bryły toczone z kolorem na pierścień
    /// <summary>Bryła toczona wokół pionu przez p: profil (r, y, indeks koloru) — normalne gładkie (po prawej stronie kierunku
    /// profilu = na zewnątrz), granice kolorów jako zdublowane punkty (ostra zmiana barwy bez nakładania).</summary>
    static void Toczony(UlSiatka s, Vector3 p, List<Vector3> pr, Color32[] kol, int seg)
    {
        int P = pr.Count, k0 = s.v.Count; var dl = new float[P];
        for (int j = 1; j < P; j++) dl[j] = dl[j - 1] + Vector2.Distance(pr[j - 1], pr[j]);
        for (int i = 0; i <= seg; i++)
        {
            float a = -Mathf.PI * .5f + i * Mathf.PI * 2f / seg; var kier = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));   // od −90° jak pas etykiety
            for (int j = 0; j < P; j++)
            {
                Vector2 d = (Vector2)pr[Mathf.Min(P - 1, j + 1)] - (Vector2)pr[Mathf.Max(0, j - 1)];
                if (d.sqrMagnitude < 1e-12f) d = (Vector2)pr[Mathf.Min(P - 1, j + 2)] - (Vector2)pr[Mathf.Max(0, j - 2)];
                var n2 = new Vector2(d.y, -d.x).normalized;
                s.v.Add(p + kier * pr[j].x + Vector3.up * pr[j].y); s.n.Add((kier * n2.x + Vector3.up * n2.y).normalized);
                s.t.Add(new Vector4(tan.x, tan.y, tan.z, 1f)); s.c.Add(kol[(int)pr[j].z]); s.uv.Add(new Vector2(a * Mathf.Max(.02f, pr[j].x), dl[j]));
            }
        }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < P - 1; j++)
            {
                int a0 = k0 + i * P + j, a1 = a0 + 1, b0 = a0 + P, b1 = b0 + 1;
                if ((s.v[a1] - s.v[a0]).sqrMagnitude < 1e-12f) continue;   // granica koloru (zerowy pas)
                var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
            }
    }

    // ---------------------------------------------------------------- napoje: kształty, marki, etykiety z nazwami
    // Właściciel 21:27: „te napoje też dobrze stworzyłeś… tylko musiałbyś nazwy jakieś im dać… żeby uzupełnić asortyment nasz…
    // nazwać jakoś fajnie”. Marki = firmy z gry (BRAXHORN, WAWER, SOBCZAK — właściciel 3.11), nazwy napojów zwykłymi słowami.
    // Etykieta to osobny pas toczony (bez nakładania na butelkę) z ATLASU 1024 × 1024 (4 × 4 kwadraty po 256 px): wysokość pasa
    // = 1/3 obwodu, więc przód butelki pokazuje całą etykietę bez zniekształceń; litery krojem Bebas (jak szyldy miasta).
    struct Etykieta { public string marka, nazwa, dopisek; public Color tlo, pas, tekst, tekstMarki; }
    static Etykieta E(string marka, string nazwa, string dopisek, Color tlo, Color pas, Color tekst, Color tekstMarki)
        => new Etykieta { marka = marka, nazwa = nazwa, dopisek = dopisek, tlo = tlo, pas = pas, tekst = tekst, tekstMarki = tekstMarki };
    static readonly Color Bi = new Color(.97f, .97f, .96f), Cz = new Color(.06f, .06f, .07f), Czerw = new Color(.78f, .06f, .07f), Zolty = new Color(.98f, .8f, .15f),
        Granat = new Color(.1f, .25f, .65f), Ziel = new Color(.1f, .52f, .22f), Nieb = new Color(.15f, .45f, .85f);
    internal static readonly string[] NazwyNapojow = { "BRAXHORN COLA", "BRAXHORN ZERO", "SOBCZAK ORANŻADA", "SOBCZAK LEMONIADA", "WAWER WODA NIEGAZOWANA", "WAWER WODA GAZOWANA",
        "SOBCZAK ICE TEA", "BRAXHORN ISO", "BRAXHORN ENERGY", "BRAXHORN POWER", "BRAXHORN COLA (puszka)", "SOBCZAK ORANŻADA (puszka)", "SOBCZAK LIMONKA", "WAWER TONIK", "WAWER LAGER", "WAWER PILS" };
    static readonly Etykieta[] Etykiety =
    {
        E("BRAXHORN", "COLA", "ORYGINALNA", Czerw, Bi, Bi, Bi),                                         // 0 PET
        E("BRAXHORN", "ZERO", "BEZ CUKRU", Cz, Czerw, Bi, Czerw),                                       // 1
        E("SOBCZAK", "ORANŻADA", "GAZOWANA", Granat, Zolty, Bi, Zolty),                                // 2
        E("SOBCZAK", "LEMONIADA", "CYTRYNA · MIĘTA", Ziel, Zolty, Bi, Zolty),                          // 3
        E("WAWER", "WODA", "NIEGAZOWANA", Nieb, Bi, Bi, Bi),                                            // 4
        E("WAWER", "WODA", "GAZOWANA", Ziel, Bi, Bi, Bi),                                               // 5
        E("SOBCZAK", "ICE TEA", "BRZOSKWINIA", Zolty, new Color(.15f, .45f, .2f), Cz, new Color(.15f, .45f, .2f)),   // 6
        E("BRAXHORN", "ISO", "IZOTONIK", Bi, Nieb, Nieb, Cz),                                           // 7
        E("BRAXHORN", "ENERGY", "500 ML", Cz, new Color(.4f, .85f, .2f), new Color(.4f, .85f, .2f), Bi),   // 8 puszki
        E("BRAXHORN", "POWER", "ENERGY DRINK", new Color(.74f, .76f, .8f), Granat, Granat, Cz),        // 9
        E("BRAXHORN", "COLA", "ORYGINALNA", Czerw, Bi, Bi, Bi),                                         // 10
        E("SOBCZAK", "ORANŻADA", "GAZOWANA", new Color(.98f, .5f, .1f), Granat, Granat, Bi),           // 11
        E("SOBCZAK", "LIMONKA", "GAZOWANA", new Color(.4f, .75f, .2f), Bi, Bi, Cz),                    // 12
        E("WAWER", "TONIK", "GORZKI", Bi, Czerw, Czerw, Cz),                                            // 13
        E("WAWER", "LAGER", "JASNE PEŁNE", new Color(.95f, .93f, .86f), new Color(.6f, .1f, .1f), new Color(.15f, .1f, .08f), new Color(.6f, .1f, .1f)),   // 14 szkło
        E("WAWER", "PILS", "JASNE", new Color(.95f, .93f, .86f), new Color(.1f, .35f, .15f), new Color(.1f, .3f, .14f), new Color(.15f, .1f, .08f)),        // 15
    };

    // NAPOJE: ks 0 PET 0.5 l, 1 PET 1.5 l, 2 puszka 0.33 l, 3 puszka 0.5 l, 4 butelka szklana 0.33 l; et = komórka atlasu
    struct Napoj { public int ks, et; public Color32 plyn, nak; }
    static Napoj N(int ks, int et, Color32 plyn, Color32 nak) => new Napoj { ks = ks, et = et, plyn = plyn, nak = nak };
    static readonly Color32 Cola = C(.17f, .07f, .03f), Woda = C(.84f, .91f, .97f), Alu = C(.84f, .85f, .87f), Srebro = C(.75f, .76f, .78f);
    static readonly Napoj[] Pet =
    {
        N(0, 0, Cola, C(.78f, .06f, .07f)), N(0, 1, Cola, C(.08f, .08f, .09f)), N(0, 2, C(.98f, .55f, .08f), C(.1f, .25f, .65f)), N(0, 3, C(.86f, .9f, .45f), C(.1f, .55f, .2f)),
        N(0, 4, Woda, C(.15f, .45f, .85f)), N(0, 5, Woda, C(.1f, .5f, .3f)), N(0, 6, C(.62f, .32f, .08f), C(.95f, .78f, .1f)), N(0, 7, C(.35f, .7f, .95f), C(.15f, .4f, .85f)),
    };
    static readonly Napoj[] Puszki = { N(2, 8, Alu, Alu), N(2, 9, Alu, Alu), N(2, 10, Alu, Alu), N(2, 11, Alu, Alu), N(2, 12, Alu, Alu), N(2, 13, Alu, Alu) };
    static readonly Napoj[] Szklane = { N(4, 14, C(.32f, .17f, .05f), Srebro), N(4, 15, C(.12f, .38f, .15f), Srebro) };

    // kształty: profil pod etykietą (dol) i nad nią (gora) — (r, y, kolor: 0 płyn / szkło, 1 nadruk puszki, 3 nakrętka, 4 PET nad płynem,
    // 5 aluminium); między nimi pas etykiety y0…y1 (wysokość = 1/3 obwodu)
    static float Obw3(float r) => 2f * Mathf.PI * r / 3f;
    static void ProfilPet(float r, float h, out float y0, out float y1, List<Vector3> dol, List<Vector3> gora)
    {
        y0 = h * .30f; y1 = y0 + Obw3(r); float yy1 = y1;
        void D(float rr, float yy, int k) => dol.Add(new Vector3(rr, yy, k)); void G(float rr, float yy, int k) => gora.Add(new Vector3(rr, yy, k));
        D(0f, 0f, 0); D(r * .72f, 0f, 0); D(r * .95f, h * .025f, 0); D(r, h * .07f, 0); D(r, y0, 0);
        G(r, yy1, 0); G(r, Mathf.Max(yy1 + .004f, h * .64f), 0); G(r * .97f, h * .69f, 0); G(r * .88f, h * .73f, 0); G(r * .88f, h * .73f, 4); G(r * .62f, h * .80f, 4);
        G(.0135f, h - .03f, 4); G(.0135f, h - .021f, 4); G(.0135f, h - .021f, 3); G(.0152f, h - .021f, 3); G(.0152f, h - .003f, 3); G(.0125f, h, 3); G(0f, h, 3);
    }
    static void ProfilPuszka(float r, float h, out float y0, out float y1, List<Vector3> dol, List<Vector3> gora)
    {
        float b0 = .013f, b1 = h - .014f; y0 = (b0 + b1) * .5f - Obw3(r) * .5f; y1 = y0 + Obw3(r); float yy0 = y0, yy1 = y1;
        void D(float rr, float yy, int k) => dol.Add(new Vector3(rr, yy, k)); void G(float rr, float yy, int k) => gora.Add(new Vector3(rr, yy, k));
        D(0f, .009f, 5); D(r * .62f, .004f, 5); D(r * .8f, 0f, 5); D(r * .94f, .004f, 5); D(r, b0, 5); D(r, b0, 1); D(r, yy0, 1);
        G(r, yy1, 1); G(r, b1, 1); G(r, b1, 5); G(r * .9f, h - .005f, 5); G(r * .84f, h - .002f, 5); G(r * .84f, h, 5); G(r * .78f, h, 5); G(r * .77f, h - .004f, 5); G(0f, h - .004f, 5);
    }
    static void ProfilSzklo(float r, float h, out float y0, out float y1, List<Vector3> dol, List<Vector3> gora)
    {
        y0 = h * .17f; y1 = y0 + Obw3(r); float yy0 = y0, yy1 = y1;
        void D(float rr, float yy, int k) => dol.Add(new Vector3(rr, yy, k)); void G(float rr, float yy, int k) => gora.Add(new Vector3(rr, yy, k));
        D(0f, 0f, 0); D(r * .85f, 0f, 0); D(r, .008f, 0); D(r, yy0, 0);
        G(r, yy1, 0); G(r, h * .53f, 0); G(r * .9f, h * .64f, 0); G(r * .55f, h * .76f, 0); G(.0135f, h * .86f, 0); G(.0135f, h - .016f, 0); G(.0158f, h - .013f, 0);
        G(.0158f, h - .013f, 3); G(.0158f, h - .002f, 3); G(.012f, h, 3); G(0f, h, 3);
    }

    static void Postaw(Partie p, Napoj n, Vector3 q, int seg)
    {
        var et = Etykiety[n.et]; var tlo = (Color32)et.tlo;
        var kol = new[] { n.plyn, tlo, tlo, n.nak, C(.86f, .9f, .92f), Alu };
        var dol = new List<Vector3>(); var gora = new List<Vector3>(); float y0, y1, r = Promien(n.ks); Material m;
        switch (n.ks)
        {
            case 1: ProfilPet(r, .32f, out y0, out y1, dol, gora); m = pet; break;
            case 2: ProfilPuszka(r, .115f, out y0, out y1, dol, gora); m = puszka; break;
            case 3: ProfilPuszka(r, .168f, out y0, out y1, dol, gora); m = puszka; break;
            case 4: ProfilSzklo(r, .23f, out y0, out y1, dol, gora); m = szkloBut; break;
            default: ProfilPet(r, .205f, out y0, out y1, dol, gora); m = pet; break;
        }
        Toczony(p[m], q, dol, kol, seg); Toczony(p[m], q, gora, kol, seg);
        PasEtykiety(p[etykiety], q, r, y0, y1, n.et, seg);
    }
    static float Promien(int ks) => ks == 1 ? .045f : ks == 4 ? .03f : ks == 0 ? .0325f : .033f;

    /// <summary>Pas etykiety (walec r, y0…y1) z UV do komórki atlasu: przód (+z) = środek etykiety, ±60° = jej krawędzie (dalej tło).
    /// Kąty jak w Toczony (od −90°), więc pierścienie brzegowe pokrywają się z bryłą butelki — bez szczelin i bez nakładania.</summary>
    static void PasEtykiety(UlSiatka s, Vector3 p, float r, float y0, float y1, int kom, int seg)
    {
        int k0 = s.v.Count, col = kom % 4, row = kom / 4; const float mg = .012f;
        for (int i = 0; i <= seg; i++)
        {
            float a = -Mathf.PI * .5f + i * Mathf.PI * 2f / seg, d = a - Mathf.PI * .5f;   // d: −π…π, 0 = przód
            var kier = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            float u = (col + mg + Mathf.Clamp01(.5f + d / (Mathf.PI * 2f / 3f)) * (1f - 2f * mg)) * .25f;
            foreach (float y in new[] { y0, y1 })
            {
                s.v.Add(p + kier * r + Vector3.up * y); s.n.Add(kier); s.t.Add(new Vector4(tan.x, tan.y, tan.z, 1f)); s.c.Add(new Color32(255, 255, 255, 255));
                s.uv.Add(new Vector2(u, (row + mg + (y > y0 ? 1f - 2f * mg : 0f)) * .25f));
            }
        }
        for (int i = 0; i < seg; i++)
        {
            int a0 = k0 + 2 * i, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
            if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), s.n[a0] + s.n[b0]) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
            else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
        }
    }

    // atlas etykiet: tło, pasek u dołu z dopiskiem, cienka linia u góry, marka, nazwa (Bebas, wygładzanie 4 próbki na piksel)
    static Material AtlasEtykiet()
    {
        const int S = 1024, CW = 256;
        var px = new Color[S * S];
        for (int i = 0; i < Etykiety.Length; i++)
        {
            var e = Etykiety[i]; int ox = (i % 4) * CW, oy = (i / 4) * CW;
            for (int y = 0; y < CW; y++) for (int x = 0; x < CW; x++) px[(oy + y) * S + ox + x] = (y >= 22 && y < 66) || (y >= 226 && y < 234) ? e.pas : e.tlo;
            float jas = e.pas.r * .3f + e.pas.g * .59f + e.pas.b * .11f;
            TekstAtlasu(px, S, ox, oy, CW, CW, e.marka, 176f, 34f, e.tekstMarki);
            TekstAtlasu(px, S, ox, oy, CW, CW, e.nazwa, 88f, 74f, e.tekst);
            TekstAtlasu(px, S, ox, oy, CW, CW, e.dopisek, 33f, 22f, jas > .55f ? Cz : Bi);
        }
        var tx = new Texture2D(S, S, TextureFormat.RGB24, true); tx.SetPixels(px); tx.Apply();
        Directory.CreateDirectory(Katalog); string path = Katalog + "/etykiety_napojow.png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = BraxCityMaterials.Surface("napoje: etykiety (atlas marek BRAXHORN / WAWER / SOBCZAK)", Color.white, .35f, 0f);
        if (t) { m.SetTexture("_MainTex", t); EditorUtility.SetDirty(m); }
        return m;
    }
    /// <summary>Napis krojem Bebas w obrazie (px szerokości S): w prostokącie ox, oy, CW × CH, wyśrodkowany, linia bazowa by,
    /// wysokość wersalika cap (zmniejszana do 88 % szerokości), wygładzanie 2 × 2 próbki na piksel.</summary>
    internal static void TekstAtlasu(Color[] px, int S, int ox, int oy, int CW, int CH, string tekst, float by, float cap, Color kol)
    {
        if (string.IsNullOrEmpty(tekst)) return;
        float w = BraxLiteryBebas.Szerokosc(tekst, cap); if (w > CW * .88f) cap *= CW * .88f / w;
        var maska = new byte[CW * CH];   // próbki 2 × 2 na piksel (bity), suma po wszystkich trójkątach napisu — bez szwów między trójkątami
        foreach (var tr in BraxLiteryBebas.Trojkaty(tekst, new Vector3(CW * .5f, by, 0f), Vector3.right, Vector3.up, cap))
        {
            Vector2 a = tr[0], b = tr[1], c = tr[2];
            float pole = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); if (Mathf.Abs(pole) < 1e-6f) continue;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), x1 = Mathf.Min(CW - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)))), y1 = Mathf.Min(CH - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int q = 0; q < 4; q++)
                    {
                        float sx = x + .25f + .5f * (q & 1), sy = y + .25f + .5f * (q >> 1);
                        float w0 = (b.x - a.x) * (sy - a.y) - (b.y - a.y) * (sx - a.x), w1 = (c.x - b.x) * (sy - b.y) - (c.y - b.y) * (sx - b.x), w2 = (a.x - c.x) * (sy - c.y) - (a.y - c.y) * (sx - c.x);
                        if (pole > 0f ? (w0 >= 0f && w1 >= 0f && w2 >= 0f) : (w0 <= 0f && w1 <= 0f && w2 <= 0f)) maska[y * CW + x] |= (byte)(1 << q);
                    }
        }
        for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++)
            {
                int bity = maska[y * CW + x]; if (bity == 0) continue;
                float cov = ((bity & 1) + ((bity >> 1) & 1) + ((bity >> 2) & 1) + ((bity >> 3) & 1)) * .25f;
                int i = (oy + y) * S + ox + x; px[i] = Color.Lerp(px[i], kol, cov);
            }
    }

    // ---------------------------------------------------------------- obudowa lodówki / szafy chłodniczej
    const float Dz = .62f, Hc = 1.84f, Hn = .22f, Wd = .70f, Bok = .04f, Sufit = 1.815f;
    static readonly float[] Poziomy = { .16f, .53f, .87f, 1.18f, 1.48f };   // wierzch dna i czterech półek
    static float Szerokosc(int drzwi) => drzwi * Wd + 2f * Bok;
    /// <summary>Szerokość lodówki (m świata) dla liczby drzwi.</summary>
    public static float Szer(int drzwi) => Szerokosc(drzwi) * K;
    public static float Glebokosc => (Dz + .06f) * K;     // z uchwytem
    public static float Wysokosc => (Hc + Hn) * K;
    public static float GlebokoscLady => 1.0f * K;
    public static float WysokoscLady => 1.29f * K;

    static void Obudowa(Partie p, int n, string napis)
    {
        float W = Szerokosc(n), x0 = -W * .5f, x1 = W * .5f, xi0 = x0 + Bok, xi1 = x1 - Bok;
        p[blacha].Poducha(new Vector3(x0, 0f, 0f), new Vector3(xi0, Hc, Dz), .012f, 0f, 3);                // boki
        p[blacha].Poducha(new Vector3(xi1, 0f, 0f), new Vector3(x1, Hc, Dz), .012f, 0f, 3);
        p[czarny].Poducha(new Vector3(x0, Hc, 0f), new Vector3(x1, Hc + Hn, Dz + .015f), .014f, 0f, 3);    // kaseton
        float cap = .10f, wN = BraxLiteryBebas.Szerokosc(napis, cap); if (wN > W - .12f) { cap *= (W - .12f) / wN; }
        BraxLiteryBebas.Napis(p[napisM], p[czarny], napis, new Vector3(0f, Hc + (Hn - cap) * .5f, Dz + .015f), Vector3.left, Vector3.up, Vector3.forward, cap, .018f, .05f, .005f);
        // wnętrze: plecy podświetlone (mleczny panel), osłona tyłu, sufit, dno-wanna, cokół z kratką
        p[plecy].Quad(new Vector3(xi0, Poziomy[0], .03f), new Vector3(xi1, Poziomy[0], .03f), new Vector3(xi1, Sufit, .03f), new Vector3(xi0, Sufit, .03f), Vector3.forward);
        p[blacha].Quad(new Vector3(xi0, 0f, 0f), new Vector3(xi1, 0f, 0f), new Vector3(xi1, Hc, 0f), new Vector3(xi0, Hc, 0f), Vector3.back);
        p[blacha].Quad(new Vector3(xi0, Sufit, .03f), new Vector3(xi1, Sufit, .03f), new Vector3(xi1, Sufit, Dz - .04f), new Vector3(xi0, Sufit, Dz - .04f), Vector3.down);
        p[blacha].Box(new Vector3(xi0, .12f, .03f), new Vector3(xi1, Poziomy[0], Dz - .04f), 16);
        p[kratka].Quad(new Vector3(xi0, 0f, Dz - .08f), new Vector3(xi1, 0f, Dz - .08f), new Vector3(xi1, .12f, Dz - .08f), new Vector3(xi0, .12f, Dz - .08f), Vector3.forward);
        for (int i = 0; i < 4; i++) { float y = .014f + i * .03f; p[czarny].Box(new Vector3(xi0 + .004f, y, Dz - .045f), new Vector3(xi1 - .004f, y + .014f, Dz - .02f)); }
        for (int i = 0; i < n; i++)
        {
            float s0 = xi0 + i * Wd, s1 = s0 + Wd;
            if (i > 0) p[czarny].Box(new Vector3(s0 - .012f, Poziomy[0], Dz - .10f), new Vector3(s0 + .012f, Sufit, Dz - .045f));   // słupek
            float a0 = i > 0 ? s0 + .012f : s0, a1 = i < n - 1 ? s1 - .012f : s1;
            // listwy LED pionowe przy krawędziach sekcji (1.6 × 1.75 cm — lica ≥ 2 cm × K od ściany boku i listwy cenowej)
            p[led].Box(new Vector3(a0, Poziomy[0] + .04f, Dz - .075f), new Vector3(a0 + .016f, Sufit - .03f, Dz - .0575f));
            p[led].Box(new Vector3(a1 - .016f, Poziomy[0] + .04f, Dz - .075f), new Vector3(a1, Sufit - .03f, Dz - .0575f));
            for (int k = 1; k < Poziomy.Length; k++)
            {
                float y = Poziomy[k];
                p[blacha].Box(new Vector3(a0 + .004f, y - .02f, .04f), new Vector3(a1 - .004f, y, Dz - .095f));
                // listwa cenowa: szara, co ok. 20 cm biała cenówka z czerwonym paskiem (osobne odcinki, bez nakładania)
                float x = a0 + .004f, xk = a1 - .004f; int q = 0; var tk = p[tacka];
                while (x < xk - .001f)
                {
                    bool cena = q % 2 == 1; float w = cena ? .05f : Mathf.Min(.15f, xk - x); if (cena && x + w > xk) w = xk - x;
                    tk.kolor = cena ? C(.97f, .97f, .95f) : C(.3f, .31f, .33f);
                    tk.Box(new Vector3(x, y - .035f, Dz - .095f), new Vector3(x + w, cena ? y - .004f : y + .01f, Dz - .075f));
                    if (cena) { tk.kolor = C(.8f, .1f, .1f); tk.Box(new Vector3(x, y - .004f, Dz - .095f), new Vector3(x + w, y + .01f, Dz - .075f)); }
                    x += w; q++;
                }
                tk.kolor = new Color32(255, 255, 255, 255);
            }
            Drzwi(p, s0 + .003f, s1 - .003f, n == 1 || i % 2 == 1);
        }
    }

    static void Drzwi(Partie p, float a, float b, bool klamkaPrawa)
    {
        float y0 = .12f, y1 = Hc - .005f, z0 = Dz - .04f, z1 = Dz, sw = .045f, gr = .06f, dr = .08f; var c = p[czarny];
        c.Poducha(new Vector3(a, y0, z0), new Vector3(a + sw, y1, z1), .006f, 0f, 3);
        c.Poducha(new Vector3(b - sw, y0, z0), new Vector3(b, y1, z1), .006f, 0f, 3);
        c.Poducha(new Vector3(a + sw, y1 - gr, z0), new Vector3(b - sw, y1, z1), .006f, 0f, 3);
        c.Poducha(new Vector3(a + sw, y0, z0), new Vector3(b - sw, y0 + dr, z1), .006f, 0f, 3);
        float zs = Dz - .02f, ga = a + sw, gb = b - sw, gy0 = y0 + dr, gy1 = y1 - gr;
        Szyba(p[szklo], new Vector3(ga, gy0, zs), new Vector3(gb, gy0, zs), new Vector3(gb, gy1, zs), new Vector3(ga, gy1, zs), Vector3.forward);
        // uchwyt pionowy ze stali na dwóch wspornikach (strona otwierania); x lokalne: dla patrzącego na lodówkę prawo = −x
        float xh = klamkaPrawa ? a + sw * .5f : b - sw * .5f; var st = p[stal];
        foreach (float yh in new[] { .86f, 1.40f }) st.Rura(new Vector3(xh, yh, z1), new Vector3(xh, yh, z1 + .045f), .008f, 8);
        st.Rura(new Vector3(xh, .80f, z1 + .045f), new Vector3(xh, 1.46f, z1 + .045f), .013f, 10);
    }

    // napoje jednej sekcji (szerokość wnętrza Wd − 2 cm z każdej strony), układ jak w sklepie: grupy 2–4 sztuk tego samego
    static void NapojeSekcji(Partie p, int wariant)
    {
        var rng = new System.Random(7919 * (wariant + 1));
        float xa = -Wd * .5f + .02f, xb = Wd * .5f - .02f;
        int[][] uklady = { new[] { 1, 0, 3, 0, 2 }, new[] { 1, 0, 4, 2, 0 }, new[] { 1, 3, 0, 0, 2 }, new[] { 1, 0, 2, 4, 3 } };
        var uk = uklady[wariant % uklady.Length];
        for (int k = 0; k < Poziomy.Length; k++)
        {
            int ks = uk[k]; float r = Promien(ks), krok = 2f * r + .004f, y = Poziomy[k] + .0005f;
            var lista = ks <= 1 ? Pet : ks == 4 ? Szklane : Puszki;
            float x = xa + r; int grupa = 0; Napoj n = default;
            while (x + r <= xb)
            {
                if (grupa == 0) { n = lista[rng.Next(lista.Length)]; n.ks = ks; grupa = 2 + rng.Next(3); }
                float z0 = Dz - .095f - .008f - r;
                Postaw(p, n, new Vector3(x, y, z0), 12);
                Postaw(p, n, new Vector3(x, y, z0 - 2f * r - .008f), 8);
                if (ks == 2) { Postaw(p, n, new Vector3(x, y + .1155f, z0), 12); Postaw(p, n, new Vector3(x, y + .1155f, z0 - 2f * r - .008f), 8); }   // puszki 0.33 w dwóch warstwach
                x += krok; grupa--;
            }
        }
    }

    // mięso z kodu (bez paczki sklepów): tacki z mięsem, kiełbasy
    static void MiesoKod(Partie p, float xa, float xb, float y, float zc, System.Random r)
    {
        Color32[] kol = { C(.62f, .12f, .1f), C(.72f, .2f, .16f), C(.92f, .72f, .62f), C(.55f, .1f, .12f) };
        for (float x = xa; x + .2f <= xb; x += .215f)
        {
            var t = p[tacka]; t.kolor = C(.08f, .08f, .09f);
            t.Poducha(new Vector3(x, y, zc - .075f), new Vector3(x + .2f, y + .03f, zc + .075f), .01f, 0f, 3);
            var m = p[mieso]; m.kolor = kol[r.Next(kol.Length)];
            m.Poducha(new Vector3(x + .02f, y + .03f, zc - .055f), new Vector3(x + .18f, y + .068f, zc + .055f), .016f, .006f, 4);
            t.kolor = m.kolor = new Color32(255, 255, 255, 255);
        }
    }

    // ---------------------------------------------------------------- API
    /// <summary>LODÓWKA Z NAPOJAMI z kodu. p = punkt posadzki przy ścianie w połowie szerokości (tył lodówki), przod = kierunek
    /// frontu (do sklepu). drzwi 1–3, wariant = układ napojów. kolizja: BoxCollider w obrysie (false — wołający stawia swoją).</summary>
    public static Transform LodowkaNapojow(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int drzwi, int wariant, bool kolizja = true)
    {
        drzwi = Mathf.Clamp(drzwi, 1, 3);
        var t = Korzen(rodzic, nazwa, p, przod);
        Wstaw(t, "obudowa", Wez("lodowka_napoje_" + drzwi, q => Obudowa(q, drzwi, "ZIMNE NAPOJE")), Vector3.zero, true);
        float xi0 = -Szerokosc(drzwi) * .5f + Bok;
        for (int i = 0; i < drzwi; i++)
        {
            int w = (wariant + i) & 3;
            var sek = Wstaw(t, "napoje " + (i + 1), Wez("napoje_sekcja_" + w, q => NapojeSekcji(q, w)), new Vector3(xi0 + (i + .5f) * Wd, 0f, 0f) * K, false);
            Lod(sek, 30f);
        }
        if (kolizja) Kolizja(t, nazwa, new Vector3(-Szerokosc(drzwi) * .5f, 0f, 0f), new Vector3(Szerokosc(drzwi) * .5f, Hc + Hn, Dz + .06f));
        return t;
    }

    /// <summary>SZAFA CHŁODNICZA Z MIĘSEM (szklane drzwi) — jak lodówka; towar: mięso z biblioteki właściciela na półkach
    /// (BraxSklepyTowar ch_dol / ch_srodek / ch_gora pod towar), bez paczki — tacki z mięsem z kodu.</summary>
    public static Transform SzafaMiesna(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int drzwi, Transform towar, System.Random rng, bool kolizja = true)
    {
        drzwi = Mathf.Clamp(drzwi, 1, 3);
        var t = Korzen(rodzic, nazwa, p, przod);
        string napis = drzwi >= 2 ? "MIĘSO · WĘDLINY" : "MIĘSO";
        Wstaw(t, "obudowa", Wez("szafa_mieso_" + drzwi, q => Obudowa(q, drzwi, napis)), Vector3.zero, true);
        float xi0 = -Szerokosc(drzwi) * .5f + Bok, zc = (.04f + Dz - .10f) * .5f;
        string[] typ = { "ch_dol", "ch_dol", "ch_srodek", "ch_srodek", "ch_gora" };
        bool paczka = towar && BraxSklepyTowar.Ma("ch_dol");
        Partie kod = null;
        for (int i = 0; i < drzwi; i++)
            for (int k = 0; k < Poziomy.Length; k++)
            {
                float xa = xi0 + i * Wd + .03f, xb = xi0 + (i + 1) * Wd - .03f, y = Poziomy[k];
                float hMax = (k + 1 < Poziomy.Length ? Poziomy[k + 1] - .02f : Sufit) - y - .03f;
                if (paczka) BraxSklepyTowar.Polka(typ[k], towar, Sw(t, xa, y + .004f, zc), t.right, -t.forward, (xb - xa) * K, rng, hMax * K);
                else { kod ??= new Partie(); Materialy(); MiesoKod(kod, xa, xb, y, Dz - .22f, rng); }
            }
        if (kod != null) foreach (var kv in kod.d)
            {
                var s = kv.Value; for (int i = 0; i < s.v.Count; i++) s.v[i] *= K;
                var g = new GameObject("mięso z kodu: " + kv.Key.name); g.transform.SetParent(t, false);
                var mesh = s.Zbuduj("szafa mięsna: " + nazwa + " / " + kv.Key.name);
                BraxZapisAssetu.Utworz(mesh, Katalog + "/" + BraxUliceKit.Bezpieczna(nazwa) + "_mieso_" + Siatek++ + ".asset");
                g.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = kv.Key; mr.shadowCastingMode = ShadowCastingMode.Off;
            }
        if (kolizja) Kolizja(t, nazwa, new Vector3(-Szerokosc(drzwi) * .5f, 0f, 0f), new Vector3(Szerokosc(drzwi) * .5f, Hc + Hn, Dz + .06f));
        return t;
    }

    /// <summary>LADA CHŁODNICZA (mięsny, wędliny): p = punkt posadzki w połowie długości lady na licu od klienta, przod = kierunek
    /// do klienta, dlugosc w metrach świata. Głębokość 1.0 m × K (od klienta do sprzedawcy), wys. 1.29 m × K.</summary>
    public static Transform LadaChlodnicza(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, float dlugosc, Transform towar, System.Random rng, bool kolizja = true, string typPrzod = "ch_dol", string typTyl = "ch_srodek")
    {
        float dl = Mathf.Max(1.0f, dlugosc / K), h = dl * .5f;
        dl = Mathf.Round(dl * 50f) / 50f; h = dl * .5f;
        var t = Korzen(rodzic, nazwa, p, -przod);   // lokalne +z = od klienta do sprzedawcy
        Wstaw(t, "lada", Wez("lada_chlodnicza_" + Mathf.RoundToInt(dl * 100f), q => Lada(q, dl)), Vector3.zero, true);
        bool paczka = towar && BraxSklepyTowar.Ma(typPrzod);
        if (paczka)
        {
            BraxSklepyTowar.Polka(typPrzod, towar, Sw(t, -h + .07f, .824f, .30f), t.right, t.forward, (dl - .14f) * K, rng, .40f * K);
            BraxSklepyTowar.Polka(typTyl, towar, Sw(t, -h + .07f, .824f, .56f), t.right, t.forward, (dl - .14f) * K, rng, .36f * K);
        }
        else
        {
            var kod = Wez("lada_mieso_kod_" + Mathf.RoundToInt(dl * 100f), q =>
            {
                var r2 = new System.Random(31);
                MiesoKod(q, -h + .07f, h - .07f, .82f, .30f, r2); MiesoKod(q, -h + .07f, h - .07f, .82f, .53f, r2);
            });
            Wstaw(t, "mięso z kodu", kod, Vector3.zero, false);
        }
        if (kolizja) Kolizja(t, nazwa, new Vector3(-h, 0f, -.06f), new Vector3(h, 1.29f, 1.0f));
        return t;
    }

    static void Lada(Partie p, float dl)
    {
        float h = dl * .5f, Dl = 1.0f, hi = h - .035f;
        p[czarny].Box(new Vector3(-h + .03f, 0f, .06f), new Vector3(h - .03f, .10f, Dl - .06f));                   // cokół cofnięty
        p[stal].Poducha(new Vector3(-h, .10f, 0f), new Vector3(h, .80f, Dl), .015f, 0f, 3);                        // korpus
        // odbojnica od klienta na wspornikach
        p[stal].Rura(new Vector3(-h + .05f, .60f, -.04f), new Vector3(h - .05f, .60f, -.04f), .016f, 10);
        int nw = Mathf.Max(2, Mathf.CeilToInt((dl - .2f) / .8f) + 1);
        for (int i = 0; i < nw; i++) { float x = Mathf.Lerp(-h + .15f, h - .15f, i / (float)(nw - 1)); p[stal].Rura(new Vector3(x, .60f, 0f), new Vector3(x, .60f, -.04f), .008f, 8); }
        // listwa przednia pod szybą, wanna ekspozycyjna
        p[stal].Poducha(new Vector3(-hi, .80f, .02f), new Vector3(hi, .85f, .07f), .008f, 0f, 3);
        p[stal].Box(new Vector3(-hi, .80f, .07f), new Vector3(hi, .82f, .70f));
        // policzki z czarnego aluminium w kształcie szyby (profil: ćwiartka elipsy + szyba górna)
        var pol = new List<Vector2> { new Vector2(.03f, .80f) };
        for (int i = 0; i <= 10; i++) { float a = i * Mathf.PI * .5f / 10f; pol.Add(new Vector2(.42f - .39f * Mathf.Cos(a), .85f + .44f * Mathf.Sin(a))); }
        pol.Add(new Vector2(.70f, 1.29f)); pol.Add(new Vector2(.70f, .80f));
        Profil(p[czarny], pol, -h, -hi); Profil(p[czarny], pol, hi, h);
        // szyba frontowa gięta + szyba górna
        for (int i = 0; i < 12; i++)
        {
            float a0 = i * Mathf.PI * .5f / 12f, a1 = (i + 1) * Mathf.PI * .5f / 12f, am = (a0 + a1) * .5f;
            Vector2 q0 = new Vector2(.42f - .37f * Mathf.Cos(a0), .85f + .42f * Mathf.Sin(a0)), q1 = new Vector2(.42f - .37f * Mathf.Cos(a1), .85f + .42f * Mathf.Sin(a1));
            var nn = new Vector3(0f, Mathf.Sin(am) / .42f, -Mathf.Cos(am) / .37f).normalized;
            Szyba(p[szklo], new Vector3(-hi, q0.y, q0.x), new Vector3(hi, q0.y, q0.x), new Vector3(hi, q1.y, q1.x), new Vector3(-hi, q1.y, q1.x), nn);
        }
        Szyba(p[szklo], new Vector3(-hi, 1.27f, .42f), new Vector3(hi, 1.27f, .42f), new Vector3(hi, 1.27f, .66f), new Vector3(-hi, 1.27f, .66f), Vector3.up);
        p[stal].Poducha(new Vector3(-hi, 1.25f, .66f), new Vector3(hi, 1.29f, .70f), .008f, 0f, 3);               // belka z LED
        p[led].Box(new Vector3(-hi + .02f, 1.228f, .648f), new Vector3(hi - .02f, 1.246f, .686f));
        // od sprzedawcy: blat roboczy (stal) na całej długości
        p[stal].Poducha(new Vector3(-h, .80f, .70f), new Vector3(h, .95f, Dl), .012f, 0f, 3);
        // cenówki na szpilkach przed towarem
        int nc = Mathf.Max(2, Mathf.FloorToInt((dl - .2f) / .45f));
        var tk = p[tacka];
        for (int i = 0; i < nc; i++)
        {
            float x = Mathf.Lerp(-hi + .15f, hi - .15f, (i + .5f) / nc);
            p[stal].Rura(new Vector3(x, .82f, .10f), new Vector3(x, .86f, .10f), .003f, 6);
            tk.kolor = C(.97f, .97f, .95f); tk.Box(new Vector3(x - .035f, .86f, .097f), new Vector3(x + .035f, .89f, .103f));
            tk.kolor = C(.8f, .1f, .1f); tk.Box(new Vector3(x - .035f, .89f, .097f), new Vector3(x + .035f, .905f, .103f));
        }
        tk.kolor = new Color32(255, 255, 255, 255);
        // waga z wyświetlaczem (od klienta i od sprzedawcy) na lewym końcu blatu (dla sprzedawcy), deska do krojenia
        float c = h - .35f;
        p[stal].Poducha(new Vector3(c - .16f, .95f, .75f), new Vector3(c + .16f, 1.0f, .99f), .01f, 0f, 3);
        p[stal].Poducha(new Vector3(c - .14f, 1.0f, .77f), new Vector3(c + .14f, 1.012f, .94f), .004f, 0f, 3);
        p[czarny].Rura(new Vector3(c, 1.0f, .965f), new Vector3(c, 1.22f, .965f), .01f, 8);
        p[czarny].Box(new Vector3(c - .11f, 1.22f, .94f), new Vector3(c + .11f, 1.32f, .99f), 16 | 32);
        p[ekran].Quad(new Vector3(c - .11f, 1.22f, .94f), new Vector3(c + .11f, 1.22f, .94f), new Vector3(c + .11f, 1.32f, .94f), new Vector3(c - .11f, 1.32f, .94f), Vector3.back);
        p[ekran].Quad(new Vector3(c - .11f, 1.22f, .99f), new Vector3(c + .11f, 1.22f, .99f), new Vector3(c + .11f, 1.32f, .99f), new Vector3(c - .11f, 1.32f, .99f), Vector3.forward);
        if (c - .16f - (-h + .62f) > .34f) p[deska].Poducha(new Vector3(-h + .62f, .95f, .76f), new Vector3(-h + .94f, .975f, .97f), .008f, 0f, 3);
    }

    /// <summary>Bryła z wielokąta wypukłego w płaszczyźnie (z, y), wyciągnięta wzdłuż x od x0 do x1 (policzek lady).</summary>
    static void Profil(UlSiatka s, List<Vector2> pol, float x0, float x1)
    {
        int n = pol.Count; float pole = 0f;
        for (int i = 0; i < n; i++) { var a = pol[i]; var b = pol[(i + 1) % n]; pole += a.x * b.y - b.x * a.y; }
        float zw = Mathf.Sign(pole);
        var sr = Vector2.zero; foreach (var q in pol) sr += q; sr /= n;
        Vector3 P(float x, Vector2 q) => new Vector3(x, q.y, q.x);
        for (int i = 0; i < n; i++)
        {
            var a = pol[i]; var b = pol[(i + 1) % n];
            s.Tri(P(x0, sr), P(x0, a), P(x0, b), Vector3.left); s.Tri(P(x1, sr), P(x1, a), P(x1, b), Vector3.right);
            var e = b - a; if (e.sqrMagnitude < 1e-10f) continue;
            var n2 = new Vector2(e.y, -e.x).normalized * zw;   // na zewnątrz wielokąta (z, y)
            s.Quad(P(x0, a), P(x0, b), P(x1, b), P(x1, a), new Vector3(0f, n2.y, n2.x));
        }
    }
}
