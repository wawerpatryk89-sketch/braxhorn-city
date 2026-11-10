using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// ULICE MIASTA (przebudowa 2.11) — wspólne narzędzia etapów ulic: siatki budowane kodem z UV planarnym w METRACH ŚWIATA
// (cegła i płyty ciągną się bez szwów przez sąsiednie elementy), kolorami wierzchołków (CityStaticPBR mnoży albedo przez kolor
// wierzchołka — jeden materiał na wiele odcieni towaru, pasków markiz, płyt), litery przestrzenne (własny krój jak na osiedlu,
// każda litera bryłą z głębią), materiały z fotoskanów Poly Haven z zamiennikiem kolorystycznym, okna „pokój za szybą”
// (shader osiedla Braxhorn/OsiedleOkno + atlas pokoje.png). Skala: k = BraxClaudeOsiedle.K (Brax 2.33 m) — wymiary
// przedmiotów = wymiar rzeczywisty × k. Siatki: każda partia (grupa × materiał) to jeden plik .asset.
// Zasada anty-migotania (właściciel 2.11 11:00 „zawsze ściągasz, kładziesz nową, a nie na to”): żadna powierzchnia nie leży
// w tej samej płaszczyźnie co inna nakładająca się powierzchnia — elementy na ścianach są budowane bez tylnej ściany
// (ŚcianaPrzy), poziome warstwy mają odstęp ≥ 2 cm, stare elementy są niszczone przed postawieniem nowych.
internal sealed class UlSiatka
{
    public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
    public readonly List<Vector4> t = new List<Vector4>();
    public readonly List<Vector2> uv = new List<Vector2>();
    public readonly List<Color32> c = new List<Color32>();
    public readonly List<int> i = new List<int>();
    public Color32 kolor = new Color32(255, 255, 255, 255);
    public int Wierzcholki => v.Count;

    static void Planar(Vector3 nn, out Vector3 u, out Vector3 w)
    {
        if (Mathf.Abs(nn.y) > .7f) { u = Vector3.right; w = Vector3.forward; }
        else if (Mathf.Abs(nn.x) > Mathf.Abs(nn.z)) { u = Vector3.forward; w = Vector3.up; }
        else { u = Vector3.right; w = Vector3.up; }
    }

    // Czworokąt p0..p3 (obwód w dowolnym kierunku), normalna nn; trójkąty zwrócone w stronę nn.
    public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 nn)
    {
        nn = nn.normalized; Planar(nn, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(nn, u), w) >= 0 ? 1f : -1f;
        int k = v.Count;
        foreach (var p in new[] { p0, p1, p2, p3 })
        {
            v.Add(p); n.Add(nn); t.Add(new Vector4(u.x, u.y, u.z, sign)); c.Add(kolor);
            uv.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, w)));
        }
        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), nn) >= 0) i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        else i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
    }
    public void Tri(Vector3 a, Vector3 b, Vector3 d, Vector3 nn)
    {
        nn = nn.normalized; Planar(nn, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(nn, u), w) >= 0 ? 1f : -1f;
        int k = v.Count;
        foreach (var p in new[] { a, b, d }) { v.Add(p); n.Add(nn); t.Add(new Vector4(u.x, u.y, u.z, sign)); c.Add(kolor); uv.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, w))); }
        if (Vector3.Dot(Vector3.Cross(b - a, d - a), nn) >= 0) i.AddRange(new[] { k, k + 1, k + 2 }); else i.AddRange(new[] { k, k + 2, k + 1 });
    }

    // Prostopadłościan osiowy (min/max). pomin: maska ścian do pominięcia: 1 −x, 2 +x, 4 −y, 8 +y, 16 −z, 32 +z.
    public void Box(Vector3 a, Vector3 b, int pomin = 0)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b);
        Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
        if ((pomin & 8) == 0) Quad(P(lo.x, hi.y, lo.z), P(hi.x, hi.y, lo.z), P(hi.x, hi.y, hi.z), P(lo.x, hi.y, hi.z), Vector3.up);
        if ((pomin & 4) == 0) Quad(P(lo.x, lo.y, lo.z), P(hi.x, lo.y, lo.z), P(hi.x, lo.y, hi.z), P(lo.x, lo.y, hi.z), Vector3.down);
        if ((pomin & 16) == 0) Quad(P(lo.x, lo.y, lo.z), P(hi.x, lo.y, lo.z), P(hi.x, hi.y, lo.z), P(lo.x, hi.y, lo.z), Vector3.back);
        if ((pomin & 32) == 0) Quad(P(lo.x, lo.y, hi.z), P(hi.x, lo.y, hi.z), P(hi.x, hi.y, hi.z), P(lo.x, hi.y, hi.z), Vector3.forward);
        if ((pomin & 1) == 0) Quad(P(lo.x, lo.y, lo.z), P(lo.x, lo.y, hi.z), P(lo.x, hi.y, hi.z), P(lo.x, hi.y, lo.z), Vector3.left);
        if ((pomin & 2) == 0) Quad(P(hi.x, lo.y, lo.z), P(hi.x, lo.y, hi.z), P(hi.x, hi.y, hi.z), P(hi.x, hi.y, lo.z), Vector3.right);
    }
    public void BoxC(Vector3 srodek, Vector3 rozmiar, int pomin = 0) => Box(srodek - rozmiar * .5f, srodek + rozmiar * .5f, pomin);

    // PODUCHA OKRĄGŁA (10.11, właściciel 12:19: „krzesła — siedziska za płaskie, zrób to normalnie”): siedzisko hokera jako bryła
    // obrotowa — p = środek spodu, promień r, grubość h, zaokrąglenie krawędzi górnej rr (dolnej rr/2), kopułka puf na środku.
    // Normalne gładkie z profilu, UV w metrach (obwód × długość profilu) — tekstura skóry bez rozciągania.
    public void PoduchaOkragla(Vector3 p, float r, float h, float rr, float puf = .015f, int seg = 28)
    {
        rr = Mathf.Clamp(rr, .004f, Mathf.Min(r * .45f, h * .6f)); float rd = rr * .5f;
        var prof = new List<Vector2>(); var nprof = new List<Vector2>();
        prof.Add(new Vector2(0f, 0f)); nprof.Add(Vector2.down);
        prof.Add(new Vector2(r - rd, 0f)); nprof.Add(Vector2.down);
        for (int i = 1; i <= 4; i++) { float a = -Mathf.PI * .5f + i * Mathf.PI * .125f; prof.Add(new Vector2(r - rd + Mathf.Cos(a) * rd, rd + Mathf.Sin(a) * rd)); nprof.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a))); }
        for (int i = 0; i <= 6; i++) { float a = i * Mathf.PI / 12f; prof.Add(new Vector2(r - rr + Mathf.Cos(a) * rr, h - rr + Mathf.Sin(a) * rr)); nprof.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a))); }
        float rt = r - rr;
        for (int i = 1; i <= 6; i++)
        {
            float x = rt * (1f - i / 6f), t = x / Mathf.Max(rt, .001f);
            prof.Add(new Vector2(x, h + puf * (1f - t * t)));
            var nn = new Vector2(2f * puf * t / Mathf.Max(rt, .001f), 1f).normalized; nprof.Add(nn);
        }
        var dl = new float[prof.Count]; for (int i = 1; i < prof.Count; i++) dl[i] = dl[i - 1] + Vector2.Distance(prof[i - 1], prof[i]);
        int k0 = v.Count, P = prof.Count;
        for (int s = 0; s <= seg; s++)
        {
            float a = s * Mathf.PI * 2f / seg; var kier = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            for (int j = 0; j < P; j++)
            {
                v.Add(p + kier * prof[j].x + Vector3.up * prof[j].y);
                n.Add((kier * nprof[j].x + Vector3.up * nprof[j].y).normalized);
                t.Add(new Vector4(tan.x, tan.y, tan.z, 1f)); c.Add(kolor);
                uv.Add(new Vector2(a * r, dl[j]));
            }
        }
        for (int s = 0; s < seg; s++)
            for (int j = 0; j < P - 1; j++)
            {
                int a0 = k0 + s * P + j, a1 = a0 + 1, b0 = a0 + P, b1 = b0 + 1;
                // profil idzie od spodu na zewnątrz, w górę i do środka wierzchu; obrót przeciwnie do wskazówek (widok z góry)
                i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
            }
    }

    // PODUCHA (10.11, właściciel 10:29: „miękkie, okrągłe poduszki”): bryła osiowa min/max o zaokrąglonych krawędziach i narożach
    // (promień r) i lekko wypchniętych ścianach (puf — jak wypchana pianką tapicerka). Siatka: sześcian dzielony na ściany N×N, punkty
    // zagęszczone ku krawędziom, rzutowane na zaokrąglony prostopadłościan; normalne gładkie; UV planarne w metrach jak w Box
    // (tekstura skóry pikowanej / gładkiej ciągnie się przez zaokrąglenie). Spód (−y) płaski — poducha leży na cokole.
    public void Poducha(Vector3 a, Vector3 b, float r, float puf = .012f, int N = 10)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b); var sr = (lo + hi) * .5f; var h = (hi - lo) * .5f;
        r = Mathf.Clamp(r, .002f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .95f);
        var hin = h - Vector3.one * r;
        Vector3[] fns = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var fn in fns)
        {
            Vector3 ua = Mathf.Abs(fn.x) > .5f ? Vector3.forward : Vector3.right, wa = Mathf.Abs(fn.y) > .5f ? Vector3.forward : Vector3.up;
            float hf = Vector3.Dot(h, new Vector3(Mathf.Abs(fn.x), Mathf.Abs(fn.y), Mathf.Abs(fn.z))), hu = Vector3.Dot(h, ua), hw = Vector3.Dot(h, wa);
            float pf = fn.y < -.5f ? 0f : Mathf.Min(puf, hf * .3f);
            Planar(fn, out var pu, out var pw); float sign = Vector3.Dot(Vector3.Cross(fn, pu), pw) >= 0 ? 1f : -1f;
            int k0 = v.Count;
            for (int iw = 0; iw <= N; iw++)
                for (int iu = 0; iu <= N; iu++)
                {
                    float su = Mathf.Sin(((float)iu / N - .5f) * Mathf.PI), sw = Mathf.Sin(((float)iw / N - .5f) * Mathf.PI);
                    var q = fn * hf + ua * su * hu + wa * sw * hw;                                   // punkt na ścianie prostopadłościanu
                    var wn = new Vector3(Mathf.Clamp(q.x, -hin.x, hin.x), Mathf.Clamp(q.y, -hin.y, hin.y), Mathf.Clamp(q.z, -hin.z, hin.z));
                    var d = q - wn; var nr = d.sqrMagnitude > 1e-10f ? d.normalized : fn;
                    float B = (1f - su * su) * (1f - sw * sw);                                      // wypchnięcie środka ściany
                    var p = sr + wn + nr * r + fn * (pf * B);
                    var nn = (nr - ua * (pf * -2f * su * (1f - sw * sw) / Mathf.Max(hu, .01f)) - wa * (pf * -2f * sw * (1f - su * su) / Mathf.Max(hw, .01f))).normalized;
                    v.Add(p); n.Add(nn); t.Add(new Vector4(pu.x, pu.y, pu.z, sign)); c.Add(kolor);
                    uv.Add(new Vector2(Vector3.Dot(p, pu), Vector3.Dot(p, pw)));
                }
            bool zgodnie = Vector3.Dot(Vector3.Cross(ua, wa), fn) >= 0;
            for (int iw = 0; iw < N; iw++)
                for (int iu = 0; iu < N; iu++)
                {
                    int p00 = k0 + iw * (N + 1) + iu, p10 = p00 + 1, p01 = p00 + N + 1, p11 = p01 + 1;
                    if (zgodnie) i.AddRange(new[] { p00, p10, p11, p00, p11, p01 }); else i.AddRange(new[] { p00, p11, p10, p00, p01, p11 });   // jak Quad: Cross(p1−p0, p2−p0) ku fn
                }
        }
    }

    // Prostopadłościan obrócony: środek c, osie a (szer.), b (wys.), nn (głęb.) ortonormalne; bezTylu = bez ściany −nn.
    public void OBox(Vector3 c0, Vector3 a, Vector3 b, Vector3 nn, float sa, float sb, float sn, bool bezTylu = false, bool bezSpodu = false)
    {
        Vector3 A = a * sa * .5f, B = b * sb * .5f, N = nn * sn * .5f;
        Vector3 P(int x, int y, int z) => c0 + A * x + B * y + N * z;
        Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), nn);
        if (!bezTylu) Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), -nn);
        Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), b);
        if (!bezSpodu) Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), -b);
        Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), a);
        Quad(P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1), -a);
    }

    // Rura od a do b (promień r, boki seg), z deklami na końcach.
    public void Rura(Vector3 a, Vector3 b, float r, int seg = 10, bool dekle = true)
    {
        var ax = (b - a); if (ax.sqrMagnitude < 1e-8f) return; ax.Normalize();
        var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(ax, u);
        for (int s = 0; s < seg; s++)
        {
            float t0 = s * Mathf.PI * 2f / seg, t1 = (s + 1) * Mathf.PI * 2f / seg;
            var d0 = u * Mathf.Cos(t0) + w * Mathf.Sin(t0); var d1 = u * Mathf.Cos(t1) + w * Mathf.Sin(t1);
            var nn = (d0 + d1).normalized;
            Quad(a + d0 * r, a + d1 * r, b + d1 * r, b + d0 * r, nn);
            if (dekle) { Tri(a, a + d1 * r, a + d0 * r, -ax); Tri(b, b + d0 * r, b + d1 * r, ax); }
        }
    }
    // Walec pionowy: środek podstawy p, promień r, wysokość h.
    public void Walec(Vector3 p, float r, float h, int seg = 16) => Rura(p, p + Vector3.up * h, r, seg, true);

    // Pierścień płaski (linie boiska, okręgi) na wysokości y: promienie r0..r1, łuk a0..a1 (stopnie, 0 = +x, w stronę +z).
    public void Pierscien(Vector3 srodek, float r0, float r1, float a0 = 0f, float a1 = 360f, int seg = 64)
    {
        for (int s = 0; s < seg; s++)
        {
            float t0 = Mathf.Lerp(a0, a1, s / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (s + 1) / (float)seg) * Mathf.Deg2Rad;
            var d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)); var d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
            Quad(srodek + d0 * r0, srodek + d0 * r1, srodek + d1 * r1, srodek + d1 * r0, Vector3.up);
        }
    }
    public void Kolo(Vector3 srodek, float r, int seg = 24) { for (int s = 0; s < seg; s++) { float t0 = s * Mathf.PI * 2f / seg, t1 = (s + 1) * Mathf.PI * 2f / seg; Tri(srodek, srodek + new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)) * r, srodek + new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1)) * r, Vector3.up); } }

    public Mesh Zbuduj(string nazwa)
    {
        var m = new Mesh { name = nazwa, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        m.SetVertices(v); m.SetNormals(n); m.SetTangents(t); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(i, 0);
        m.RecalculateBounds(); return m;
    }
}

internal static class BraxUliceKit
{
    public const string Out = BraxRegion.Out + "/Ulice";
    public static float K => BraxClaudeOsiedle.K;
    static readonly Dictionary<(string, Material), UlSiatka> partie = new Dictionary<(string, Material), UlSiatka>();
    static readonly Dictionary<string, float> zasiegGrup = new Dictionary<string, float>();
    public static string Grupa = "ulice";
    static int plik;
    public static readonly List<string> Usuniete = new List<string>();

    public static void Start()
    {
        partie.Clear(); zasiegGrup.Clear(); Grupa = "ulice";
        Directory.CreateDirectory(Out); Directory.CreateDirectory(Out + "/Materialy");
    }

    /// <summary>Bieżąca grupa scalania; zasięg &gt; 0 = drobne detale znikają dalej niż ok. tyle metrów (LODGroup).</summary>
    public static void UstawGrupe(string nazwa, float zasieg = 0f)
    {
        Grupa = nazwa;
        if (zasieg > 0f) zasiegGrup[nazwa] = zasiegGrup.TryGetValue(nazwa, out var z) ? Mathf.Max(z, zasieg) : zasieg;
    }

    public static UlSiatka S(Material m)
    {
        if (!m) m = Mat("zamiennik: szary", new Color(.5f, .5f, .5f), .8f);
        if (!partie.TryGetValue((Grupa, m), out var s)) partie[(Grupa, m)] = s = new UlSiatka();
        return s;
    }

    /// <summary>Wszystkie partie do sceny pod rodzicem (jedna siatka na grupę i materiał), pliki .asset. Zwraca liczbę siatek.</summary>
    public static int Zapisz(Transform rodzic, string prefiks)
    {
        int n = 0; var grupy = new Dictionary<string, Transform>();
        foreach (var kv in partie)
        {
            var s = kv.Value; if (s.Wierzcholki == 0) continue;
            if (!grupy.TryGetValue(kv.Key.Item1, out var g))
            {
                g = new GameObject(kv.Key.Item1).transform; g.SetParent(rodzic, false); grupy[kv.Key.Item1] = g;
            }
            var mesh = s.Zbuduj(prefiks + " / " + kv.Key.Item1 + " / " + kv.Key.Item2.name);
            BraxZapisAssetu.Utworz(mesh, Out + "/" + Bezpieczna(prefiks) + "_" + (plik++).ToString("D5") + ".asset");
            var go = new GameObject(kv.Key.Item2.name); go.transform.SetParent(g, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item2;
            r.shadowCastingMode = kv.Key.Item2.renderQueue >= 2900 ? ShadowCastingMode.Off : ShadowCastingMode.On; r.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
            n++;
        }
        foreach (var kv in grupy)
        {
            if (!zasiegGrup.TryGetValue(kv.Key, out var z) || z <= 0f) continue;
            var rs = kv.Value.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var lod = kv.Value.gameObject.AddComponent<LODGroup>();
            float rozmiar = Mathf.Max(.5f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            // wysokość względna ekranu przy odległości z (FOV 55°): rozmiar / (2 · z · tan 27.5°)
            float prog = Mathf.Clamp(rozmiar / (2f * z * .52f), .004f, .5f);
            lod.SetLODs(new[] { new LOD(prog, rs) }); lod.RecalculateBounds();
        }
        partie.Clear(); zasiegGrup.Clear();
        return n;
    }

    public static string Bezpieczna(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

    // ---------------- materiały ----------------
    public static Material Mat(string nazwa, Color kolor, float szorstkosc = .85f, float metal = 0f)
        => BraxCityMaterials.Surface("Ulice: " + nazwa, kolor, szorstkosc, metal);

    /// <summary>Fotoskan Poly Haven (pierwszy dostępny slug), metry na jedno powtórzenie; brak tekstur → kolor zamienny.</summary>
    public static Material Tex(string nazwa, float metry, Color? odcien, Color zamiennik, float szorstkosc, params string[] slugi)
    {
        try { return BraxRegion.Pbr("Ulice: " + nazwa, metry, odcien, slugi); }
        catch (Exception) { return Mat(nazwa + " (bez tekstury)", zamiennik, szorstkosc); }
    }

    /// <summary>Tekstura z pliku Assets (np. osiedla) jako materiał miasta; brak → kolor zamienny.</summary>
    public static Material TexPlik(string nazwa, string katalog, string slug, float metry, Color? odcien, Color zamiennik, float szorstkosc)
    {
        string d = katalog + slug + "/";
        // 10.11: bez ponownego importu tej samej tekstury przy każdym materiale (BraxRegion.TeksturaRaz)
        Texture2D T(params string[] nazwy) { foreach (var x in nazwy) { var p = d + x; if (File.Exists(p)) { var t = BraxRegion.TeksturaRaz(p); if (t) return t; } } return null; }
        var dif = T("diffuse.jpg", "diffuse.png", "albedo.jpg", slug + "_diff_2k.jpg", slug + "_diff_1k.jpg");
        if (!dif) return Mat(nazwa + " (bez tekstury)", zamiennik, szorstkosc);
        var nor = T("normal.jpg", "normal.png", slug + "_nor_gl_2k.png", slug + "_nor_gl_1k.png");
        var arm = T("arm.jpg", "arm.png", slug + "_arm_2k.jpg", slug + "_arm_1k.jpg");
        var m = BraxCityMaterials.Textured("Ulice: " + nazwa + " [" + slug + "]", dif, nor, arm, 1f / Mathf.Max(.05f, metry));
        if (odcien.HasValue) { m.SetColor("_Color", odcien.Value); EditorUtility.SetDirty(m); }
        return m;
    }

    /// <summary>Szkło przezroczyste (witryny, drzwi): widać prawdziwe wnętrze.</summary>
    public static Material SzkloPrzezroczyste => Mat("szkło witryn (przezroczyste)", new Color(.62f, .70f, .72f, .22f), .06f, .1f);

    static readonly Dictionary<string, Material> okna = new Dictionary<string, Material>();
    /// <summary>Okno mieszkania: pokój za szybą (shader osiedla, atlas pokoje.png); siatka pokoi w metrach świata:
    /// szerokość = rozstaw okien (okna w środkach komórek), wysokość = kondygnacja, poziom pierwszej podłogi floor0.</summary>
    public static Material OknoPokoj(float rozstaw, float kondygnacja, float floor0, int wariant0, int wariantow, float glebokosc, float offsetU)
    {
        string klucz = rozstaw.ToString("F3") + "|" + kondygnacja.ToString("F3") + "|" + floor0.ToString("F3") + "|" + wariant0 + "|" + wariantow + "|" + glebokosc.ToString("F2") + "|" + Mathf.Repeat(offsetU, rozstaw).ToString("F3");
        if (okna.TryGetValue(klucz, out var have) && have) return have;
        var sh = Shader.Find("Braxhorn/UliceOkno") ?? Shader.Find("Braxhorn/OsiedleOkno"); const string atlas = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
        var imp = AssetImporter.GetAtPath(atlas) as TextureImporter;
        Material m;
        if (!sh || !imp) m = Mat("szkło okien przyciemniane (brak atlasu pokoi)", new Color(.10f, .13f, .15f), .1f, .5f);
        else
        {
            if (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp)
            {
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            }
            m = new Material(sh) { name = "Ulice: okno z pokojem " + okna.Count };
            m.SetTexture("_Rooms", AssetDatabase.LoadAssetAtPath<Texture2D>(atlas));
            m.SetFloat("_Variant", wariant0); m.SetFloat("_Variants", wariantow); m.SetFloat("_CellW", rozstaw); m.SetFloat("_CellH", kondygnacja);
            m.SetFloat("_Floor0", floor0); m.SetFloat("_Depth", glebokosc); m.SetFloat("_Curtains", .55f);
            if (m.HasProperty("_OffsetU")) m.SetFloat("_OffsetU", offsetU);
            m.enableInstancing = true;
            BraxZapisAssetu.Utworz(m, Out + "/Materialy/okno_" + okna.Count + ".mat");
        }
        okna[klucz] = m; return m;
    }
    public static void WyczyscOkna() => okna.Clear();

    // ---------------- usuwanie starego (zasada 2.11: zdejmij stare, połóż nowe) ----------------
    public static void Usun(GameObject g, string powod)
    {
        if (!g) return;
        Usuniete.Add(g.name + " (" + powod + ")");
        UnityEngine.Object.DestroyImmediate(g);
    }

    // ---------------- litery przestrzenne ----------------
    // Krój jak na osiedlu (BraxClaudeOsiedleLitery: geometryczny bezszeryfowy, wersaliki z polskimi znakami, cyfry), ale
    // własna kopia — generator osiedla jest poza kluczem pamięci miasta w CI, więc kod miasta nie może od niego zależeć.
    sealed class Glif { public float w; public readonly List<Vector2[]> kreski = new List<Vector2[]>(); public readonly List<Vector2> kropki = new List<Vector2>(); }
    static Dictionary<char, Glif> glify;
    static Vector2[] Pk(params float[] xy) { var r = new Vector2[xy.Length / 2]; for (int q = 0; q < r.Length; q++) r[q] = new Vector2(xy[2 * q], xy[2 * q + 1]); return r; }
    static Vector2[] Luk(float cx, float cy, float rx, float ry, float a0, float a1, int seg = 0)
    {
        if (seg <= 0) seg = Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 11.25f));
        var r = new Vector2[seg + 1];
        for (int q = 0; q <= seg; q++) { float a = Mathf.Lerp(a0, a1, q / (float)seg) * Mathf.Deg2Rad; r[q] = new Vector2(cx + rx * Mathf.Cos(a), cy + ry * Mathf.Sin(a)); }
        return r;
    }
    static Vector2[] Laczy(params Vector2[][] cz)
    {
        var l = new List<Vector2>();
        foreach (var x in cz) foreach (var p in x) if (l.Count == 0 || (l[l.Count - 1] - p).sqrMagnitude > 1e-6f) l.Add(p);
        return l.ToArray();
    }
    static void Krojownia()
    {
        if (glify != null) return;
        glify = new Dictionary<char, Glif>();
        Glif G(char ch, float w) { var g = new Glif { w = w }; glify[ch] = g; return g; }
        void Kr(Glif g, Vector2[] p) => g.kreski.Add(p);
        Glif Kopia(char ch, char z) { var s = glify[z]; var g = G(ch, s.w); g.kreski.AddRange(s.kreski); g.kropki.AddRange(s.kropki); return g; }
        void Ostry(Glif g, float x) => Kr(g, Pk(x - .07f, 1.13f, x + .07f, 1.33f));
        void Ogonek(Glif g, float x) => Kr(g, Laczy(Pk(x, 0f), Luk(x - .1f, -.11f, .1f, .11f, 0f, -150f, 6), Pk(x - .02f, -.27f)));
        var g_ = G('A', .72f); Kr(g_, Pk(0, 0, .36f, 1, .72f, 0)); Kr(g_, Pk(.115f, .32f, .605f, .32f));
        g_ = G('B', .685f); Kr(g_, Pk(0, 0, 0, 1)); Kr(g_, Laczy(Pk(0, 1, .4f, 1), Luk(.4f, .755f, .245f, .245f, 90, -90), Pk(0, .51f))); Kr(g_, Laczy(Pk(0, .51f, .43f, .51f), Luk(.43f, .255f, .255f, .255f, 90, -90), Pk(0, 0)));
        g_ = G('C', .82f); Kr(g_, Luk(.41f, .5f, .41f, .5f, 42, 318, 26));
        g_ = G('D', .72f); Kr(g_, Laczy(Pk(0, 0, 0, 1, .25f, 1), Luk(.25f, .5f, .47f, .5f, 90, -90, 16), Pk(0, 0)));
        g_ = G('E', .6f); Kr(g_, Pk(0, 0, 0, 1)); Kr(g_, Pk(0, 1, .6f, 1)); Kr(g_, Pk(0, .5f, .52f, .5f)); Kr(g_, Pk(0, 0, .6f, 0));
        g_ = G('F', .58f); Kr(g_, Pk(0, 0, 0, 1)); Kr(g_, Pk(0, 1, .58f, 1)); Kr(g_, Pk(0, .5f, .5f, .5f));
        g_ = G('G', .82f); Kr(g_, Luk(.41f, .5f, .41f, .5f, 42, 360, 26)); Kr(g_, Pk(.46f, .47f, .82f, .47f));
        g_ = G('H', .66f); Kr(g_, Pk(0, 0, 0, 1)); Kr(g_, Pk(.66f, 0, .66f, 1)); Kr(g_, Pk(0, .5f, .66f, .5f));
        g_ = G('I', 0f); Kr(g_, Pk(0, 0, 0, 1));
        g_ = G('J', .52f); Kr(g_, Laczy(Pk(.52f, 1, .52f, .3f), Luk(.26f, .3f, .26f, .3f, 0, -165, 12)));
        g_ = G('K', .64f); Kr(g_, Pk(0, 0, 0, 1)); Kr(g_, Pk(.64f, 1, 0, .36f)); Kr(g_, Pk(.22f, .58f, .66f, 0));
        g_ = G('L', .56f); Kr(g_, Pk(0, 1, 0, 0, .56f, 0));
        g_ = G('M', .84f); Kr(g_, Pk(0, 0, 0, 1, .42f, .28f, .84f, 1, .84f, 0));
        g_ = G('N', .68f); Kr(g_, Pk(0, 0, 0, 1, .68f, 0, .68f, 1));
        g_ = G('O', .86f); Kr(g_, Luk(.43f, .5f, .43f, .5f, 0, 360, 32));
        g_ = G('P', .645f); Kr(g_, Laczy(Pk(0, 0, 0, 1, .38f, 1), Luk(.38f, .735f, .265f, .265f, 90, -90), Pk(0, .47f)));
        g_ = G('R', .66f); Kr(g_, Laczy(Pk(0, 0, 0, 1, .38f, 1), Luk(.38f, .735f, .265f, .265f, 90, -90), Pk(0, .47f))); Kr(g_, Pk(.3f, .47f, .66f, 0));
        g_ = G('S', .64f); Kr(g_, Laczy(Luk(.32f, .75f, .3f, .25f, 28, 270, 16), Luk(.32f, .25f, .32f, .25f, 90, -152, 16)));
        g_ = G('T', .64f); Kr(g_, Pk(0, 1, .64f, 1)); Kr(g_, Pk(.32f, 1, .32f, 0));
        g_ = G('U', .66f); Kr(g_, Laczy(Pk(0, 1, 0, .33f), Luk(.33f, .33f, .33f, .33f, 180, 360, 12), Pk(.66f, 1)));
        g_ = G('V', .72f); Kr(g_, Pk(0, 1, .36f, 0, .72f, 1));
        g_ = G('W', 1f); Kr(g_, Pk(0, 1, .23f, 0, .5f, .72f, .77f, 0, 1, 1));
        g_ = G('X', .66f); Kr(g_, Pk(0, 0, .66f, 1)); Kr(g_, Pk(0, 1, .66f, 0));
        g_ = G('Y', .68f); Kr(g_, Pk(0, 1, .34f, .46f)); Kr(g_, Pk(.68f, 1, .34f, .46f, .34f, 0));
        g_ = G('Z', .62f); Kr(g_, Pk(0, 1, .62f, 1, 0, 0, .62f, 0));
        g_ = G('0', .6f); Kr(g_, Luk(.3f, .5f, .3f, .5f, 0, 360, 28));
        g_ = G('1', .32f); Kr(g_, Pk(0, .76f, .3f, 1, .3f, 0));
        g_ = G('2', .62f); Kr(g_, Laczy(Luk(.31f, .69f, .3f, .31f, 160, -35, 14), Pk(0, 0, .62f, 0)));
        g_ = G('3', .6f); Kr(g_, Laczy(Luk(.29f, .75f, .27f, .25f, 150, -90, 14), Luk(.29f, .26f, .31f, .26f, 90, -150, 14)));
        g_ = G('4', .66f); Kr(g_, Pk(.5f, 0, .5f, 1, 0, .3f, .66f, .3f));
        g_ = G('5', .6f); Kr(g_, Laczy(Pk(.56f, 1, .1f, 1, .141f, .591f), Luk(.3f, .32f, .3f, .32f, 122, -150, 16)));
        g_ = G('6', .62f); Kr(g_, Laczy(Luk(.62f, .31f, .62f, .69f, 105, 180, 8), Luk(.31f, .31f, .31f, .31f, 180, -180, 24)));
        g_ = G('7', .6f); Kr(g_, Pk(0, 1, .6f, 1, .18f, 0));
        g_ = G('8', .6f); Kr(g_, Luk(.3f, .755f, .26f, .245f, 0, 360, 22)); Kr(g_, Luk(.3f, .265f, .3f, .265f, 0, 360, 24));
        g_ = G('9', .62f); foreach (var kk in glify['6'].kreski) { var r = new Vector2[kk.Length]; for (int q = 0; q < kk.Length; q++) r[q] = new Vector2(.62f - kk[q].x, 1f - kk[q].y); Kr(g_, r); }
        g_ = Kopia('Ą', 'A'); Ogonek(g_, .68f);
        g_ = Kopia('Ć', 'C'); Ostry(g_, .45f);
        g_ = Kopia('Ę', 'E'); Ogonek(g_, .58f);
        g_ = Kopia('Ł', 'L'); Kr(g_, Pk(-.1f, .34f, .26f, .62f));
        g_ = Kopia('Ń', 'N'); Ostry(g_, .34f);
        g_ = Kopia('Ó', 'O'); Ostry(g_, .43f);
        g_ = Kopia('Ś', 'S'); Ostry(g_, .32f);
        g_ = Kopia('Ź', 'Z'); Ostry(g_, .31f);
        g_ = Kopia('Ż', 'Z'); g_.kropki.Add(new Vector2(.31f, 1.24f));
        g_ = G('·', 0f); g_.kropki.Add(new Vector2(0, .5f));
        g_ = G('.', 0f); g_.kropki.Add(new Vector2(0, 0));
        g_ = G('-', .36f); Kr(g_, Pk(0, .45f, .36f, .45f));
        g_ = G('/', .4f); Kr(g_, Pk(0, 0, .4f, 1));
        g_ = G('+', .56f); Kr(g_, Pk(0, .5f, .56f, .5f)); Kr(g_, Pk(.28f, .22f, .28f, .78f));
    }
    const float OdstepSlow = .42f;
    public static float SzerokoscNapisu(string tekst, float cap, float s = .15f, float track = .14f)
    {
        Krojownia(); float x = 0; int n = 0;
        foreach (char c0 in tekst)
        {
            char ch = char.ToUpperInvariant(c0);
            if (ch == ' ' || !glify.TryGetValue(ch, out var g)) { x += OdstepSlow * cap; continue; }
            x += (g.w * (1f - s) + s) * cap + track * cap; n++;
        }
        return n > 0 ? x - track * cap : x;
    }
    /// <summary>Napis z liter przestrzennych: o = środek linii bazowej na płaszczyźnie montażu, nn = lico (do oglądającego),
    /// cap = wysokość wersalika, glebokosc = grubość liter. Litery bez tylnej ściany (stoją na ścianie / licu). Zwraca szerokość.</summary>
    public static float Napis3D(string tekst, Vector3 o, Vector3 nn, float cap, float glebokosc, Material lico, Material boki, float s = .15f, float track = .14f)
    {
        Krojownia();
        var r = Vector3.Cross(Vector3.up, -nn).normalized; var up = Vector3.Cross(r, nn).normalized;
        float wTot = SzerokoscNapisu(tekst, cap, s, track), x = -wTot * .5f, sw = s * cap, kk = (1f - s) * cap;
        foreach (char c0 in tekst)
        {
            char ch = char.ToUpperInvariant(c0);
            if (ch == ' ' || !glify.TryGetValue(ch, out var g)) { x += OdstepSlow * cap; continue; }
            float x0 = x;
            Vector3 P(Vector2 q) => o + r * (x0 + sw * .5f + q.x * kk) + up * (sw * .5f + q.y * kk);
            foreach (var kr in g.kreski) Kreska(kr, P, nn, sw, glebokosc, lico, boki);
            foreach (var d in g.kropki) Kostka(P(d), nn, r, up, sw * 1.15f, sw * 1.15f, glebokosc, lico, boki);
            x += (g.w * (1f - s) + s) * cap + track * cap;
        }
        return wTot;
    }
    /// <summary>Ten sam krój narysowany W TEKSTURZE (ekrany, telebimy — napis jest obrazem, nie nakładką): px — piksele
    /// (wiersz 0 = dół), cx — środek napisu, by — linia bazowa, cap — wysokość wersalika (wszystko w pikselach).</summary>
    public static void NapisWTeksturze(Color[] px, int W, int H, string tekst, float cx, float by, float cap, Color kolor, float s = .15f, float track = .14f)
    {
        Krojownia();
        float wTot = SzerokoscNapisu(tekst, cap, s, track), x = cx - wTot * .5f, sw = s * cap, kk = (1f - s) * cap;
        foreach (char c0 in tekst)
        {
            char ch = char.ToUpperInvariant(c0);
            if (ch == ' ' || !glify.TryGetValue(ch, out var g)) { x += OdstepSlow * cap; continue; }
            float x0 = x;
            Vector2 P(Vector2 q) => new Vector2(x0 + sw * .5f + q.x * kk, by + sw * .5f + q.y * kk);
            foreach (var kr in g.kreski) for (int q = 0; q < kr.Length - 1; q++) OdcinekWTeksturze(px, W, H, P(kr[q]), P(kr[q + 1]), sw * .5f, kolor, false);
            foreach (var d in g.kropki) { var p = P(d); OdcinekWTeksturze(px, W, H, p, p, sw * .575f, kolor, true); }
            x += (g.w * (1f - s) + s) * cap + track * cap;
        }
    }
    static void OdcinekWTeksturze(Color[] px, int W, int H, Vector2 a, Vector2 b, float r, Color kol, bool kwadrat)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r - 1)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r - 1)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r + 1));
        var ab = b - a; float L2 = Mathf.Max(ab.sqrMagnitude, 1e-6f);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + .5f, y + .5f); float d;
                if (kwadrat) d = Mathf.Max(Mathf.Abs(p.x - a.x), Mathf.Abs(p.y - a.y));
                else { float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / L2); d = (p - (a + ab * t)).magnitude; }
                float cov = Mathf.Clamp01(r - d + .5f) * kol.a; if (cov <= 0f) continue;
                int i = y * W + x; var c = px[i];
                px[i] = new Color(Mathf.Lerp(c.r, kol.r, cov), Mathf.Lerp(c.g, kol.g, cov), Mathf.Lerp(c.b, kol.b, cov), 1f);
            }
    }
    static void Kreska(Vector2[] pts, Func<Vector2, Vector3> P, Vector3 nn, float sw, float depth, Material lico, Material boki)
    {
        int m = pts.Length; if (m < 2) return;
        var W = new Vector3[m]; for (int q = 0; q < m; q++) W[q] = P(pts[q]);
        for (int q = 0; q < m - 1; q++)
        {
            var a = W[q]; var b = W[q + 1]; var d = b - a; float len = d.magnitude; if (len < 1e-5f) continue; d /= len;
            // przedłużenie o pół grubości: łagodne łączenia zachodzą, końce kwadratowe
            Kostka((a + b) * .5f, nn, d, Vector3.Cross(nn, d).normalized, len + sw * .9f, sw, depth, lico, boki);
        }
    }
    static void Kostka(Vector3 c0, Vector3 nn, Vector3 a, Vector3 b, float la, float lb, float depth, Material lico, Material boki)
    {
        Vector3 A = a * la * .5f, B = b * lb * .5f, D = nn * depth;
        Vector3 p00 = c0 - A - B, p10 = c0 + A - B, p11 = c0 + A + B, p01 = c0 - A + B;
        S(lico).Quad(p00 + D, p10 + D, p11 + D, p01 + D, nn);
        var sb = S(boki);
        sb.Quad(p00, p10, p10 + D, p00 + D, -b); sb.Quad(p01, p11, p11 + D, p01 + D, b);
        sb.Quad(p00, p01, p01 + D, p00 + D, -a); sb.Quad(p10, p11, p11 + D, p10 + D, a);
    }

    // ---------------- punkty pod misje ----------------
    public static void Punkt(string id, string opis, Vector3 gdzie)
    {
        var world = UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>();
        if (!world) return;
        var lista = world.points.Where(p => p != null && p.id != id).ToList();
        lista.Add(new BraxCityPoint { id = id, label = opis, position = gdzie });
        world.points = lista.ToArray();
        EditorUtility.SetDirty(world);
    }

    // ---------------- pomoc ----------------
    public static Color32 C(float r, float g, float b) => new Color32((byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255), (byte)(Mathf.Clamp01(b) * 255), 255);
    public static readonly Color32 Bialy = new Color32(255, 255, 255, 255);
}
