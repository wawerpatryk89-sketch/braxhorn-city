using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// MIASTO R1 — OZNAKOWANIE POZIOME W TEKSTURZE I LICZNIK NAKŁADEK (właściciel 16.11.2026 13:57, przeciw migotaniu pasów:
// „1. Linie węższe niż 25 cm — w teksturę, nie w geometrię (tekstura ma mipmapy, geometria nie). 2. Żadnej farby wiszącej nad
// powierzchnią. Licznik nakladki=0 przed oddaniem gałęzi.”).
//
// 1) LINIA < 25 cm NIE JEST JUŻ WIELOKĄTEM FARBY. Nawierzchnia (asfalt, beton placu, żywica posadzki) dostaje w miejscu linii PAS
//    szerokości szer_linii / Wypelnienie (12 cm → 40 cm), wbudowany w tę samą płaszczyznę co reszta nawierzchni (styk krawędzią),
//    a sama linia jest MASKĄ W TEKSTURZE: CityStaticPBR, słowo _BRAX_ZNAKI, UV2 — x: położenie w poprzek linii biegnącej wzdłuż
//    osi X świata (0..1 w pasie), y: to samo dla linii wzdłuż Z (komórka skrzyżowania ma oba). Poza pasami UV2 = (0, 0) → czarny
//    brzeg maski (clamp) = sama nawierzchnia. Maska 128 × 4, środek 30 % biały, mipmapy, trilinear, aniso 8: z daleka mipmapa
//    uśrednia linię do 30 % farby w całym pasie — tyle farby, ile linia ma naprawdę, bez migotania cienkiej geometrii.
//    Linie ≥ 25 cm (zebry 50 cm, linie zatrzymania, strzałki) zostają wielokątami farby W WYCIĘCIACH nawierzchni (styk krawędzią).
// 2) LICZNIK NAKŁADEK (Kontrola, po wszystkich kontrolach jakości miasta — także po rozsuwaniu warstw przez kontrolę migotania):
//    każdy trójkąt farby (renderery i materiały oznakowania) — czy pod nim, 0–6 cm niżej albo w tej samej wysokości, leży inna
//    płaska powierzchnia (asfalt, płyty, druga farba). Farba w wycięciu nie ma nic pod sobą → 0. Drugi licznik: linie farby
//    węższe niż 25 cm, które zostały w geometrii. Log BRAXHORN_NAKLADKI nakladki=N cienkie_geometria=M (N, M > 0 → DO POPRAWKI).
public static class BraxZnakiPoziome
{
    public const float Prog = .25f;          // linie węższe niż 25 cm → tekstura
    public const float Wypelnienie = .3f;    // udział linii w szerokości pasa (biały środek maski)
    public static float SzerPasa(float linia) => linia / Wypelnienie;
    public static bool Cienka(Rect r) => Mathf.Min(r.width, r.height) < Prog - 1e-4f;
    public static int Pasow, Komorek;

    static BraxRegion.Report dla;
    static Texture2D maska;
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static void Stan() { if (dla != BraxRegion.Rep) { dla = BraxRegion.Rep; maska = null; mats.Clear(); Pasow = Komorek = 0; } }

    // ------------------------------------------------------------------ maska i materiał
    public static Texture2D Maska()
    {
        Stan();
        if (maska) return maska;
        const int W = 128, H = 4;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, true, true)
        { name = "Maska linii poziomej (środek 30 %, mipmapy)", wrapModeU = TextureWrapMode.Clamp, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
        var c = new Color32[W * H];
        for (int i = 0; i < W; i++)
        {
            float u = (i + .5f) / W, a = Mathf.Clamp01((Wypelnienie * .5f - Mathf.Abs(u - .5f)) * W + .5f);
            byte b = (byte)Mathf.RoundToInt(a * 255f);
            for (int j = 0; j < H; j++) c[j * W + i] = new Color32(b, b, b, 255);
        }
        c[0] = c[W - 1] = new Color32(0, 0, 0, 255);
        t.SetPixels32(c); t.Apply(true, false);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        BraxZapisAssetu.Utworz(t, BraxRegion.Out + "/Materials/znaki_maska_linii.asset");
        maska = t; return t;
    }

    /// <summary>Wariant materiału nawierzchni z liniami w teksturze (ten sam materiał + _BRAX_ZNAKI, maska, kolor farby).</summary>
    public static Material Mat(Material baza, Color kolor, float szorstkosc = .62f)
    {
        Stan();
        if (!baza) return null;
        string k = baza.name + "|" + ColorUtility.ToHtmlStringRGB(kolor) + "|" + szorstkosc.ToString("F2");
        if (mats.TryGetValue(k, out var have) && have) return have;
        var m = new Material(baza) { name = baza.name + " + linie w teksturze #" + ColorUtility.ToHtmlStringRGB(kolor) };
        m.EnableKeyword("_BRAX_ZNAKI"); m.SetFloat("_Znaki", 1f);
        m.SetTexture("_ZnakiTex", Maska()); m.SetColor("_ZnakiKolor", kolor); m.SetFloat("_ZnakiSzorstkosc", szorstkosc);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Materials/znaki_" + mats.Count.ToString("D2") + ".mat");
        mats[k] = m; return m;
    }

    // ------------------------------------------------------------------ pasy linii prostokątnych (osie X / Z)
    public sealed class Pas
    {
        public Rect linia, pas; public bool wzdluzX; public int grupa;
        public float Uv(Vector3 p) => wzdluzX ? .5f + (p.z - pas.center.y) / pas.height : .5f + (p.x - pas.center.x) / pas.width;
        public float Odl(Vector2 c) { float dx = Mathf.Max(0f, Mathf.Max(linia.xMin - c.x, c.x - linia.xMax)), dz = Mathf.Max(0f, Mathf.Max(linia.yMin - c.y, c.y - linia.yMax)); return Mathf.Sqrt(dx * dx + dz * dz); }
    }
    public static Pas PasLinii(Rect l, int grupa = 0)
    {
        bool x = l.width >= l.height; float s = SzerPasa(x ? l.height : l.width);
        var p = x ? Rect.MinMaxRect(l.xMin, l.center.y - s * .5f, l.xMax, l.center.y + s * .5f)
                  : Rect.MinMaxRect(l.center.x - s * .5f, l.yMin, l.center.x + s * .5f, l.yMax);
        return new Pas { linia = l, pas = p, wzdluzX = x, grupa = grupa };
    }

    /// <summary>Zestaw pasów jednej nawierzchni. Komórka siatki (środek c) należy do pasa wzdłuż X i/lub wzdłuż Z; przy pasach
    /// równoległych zachodzących na siebie — do linii bliższej (krawędzie linii są krawędziami siatki, więc żadna linia nie traci
    /// farby); przy skrzyżowaniu linii różnych kolorów — do bliższej.</summary>
    public sealed class Pasy
    {
        public readonly List<Pas> l = new List<Pas>();
        public Pasy() { }
        public Pasy(IEnumerable<Rect> linie, int grupa = 0) { Dodaj(linie, grupa); }
        public void Dodaj(IEnumerable<Rect> linie, int grupa = 0) { foreach (var r in linie) { l.Add(PasLinii(r, grupa)); Pasow++; } }
        public int Count => l.Count;
        /// <summary>Krawędzie pasów i linii leżące wewnątrz obszaru → do siatki komórek.</summary>
        public void Krawedzie(Rect a, ICollection<float> xs, ICollection<float> zs)
        {
            void X(float x) { if (x > a.xMin + .001f && x < a.xMax - .001f) xs.Add(x); }
            void Z(float z) { if (z > a.yMin + .001f && z < a.yMax - .001f) zs.Add(z); }
            foreach (var p in l)
            {
                if (!p.pas.Overlaps(a)) continue;
                X(p.pas.xMin); X(p.pas.xMax); Z(p.pas.yMin); Z(p.pas.yMax);
                X(p.linia.xMin); X(p.linia.xMax); Z(p.linia.yMin); Z(p.linia.yMax);
            }
        }
        public bool Komorka(Vector2 c, out Pas px, out Pas pz)
        {
            px = pz = null; float dx = 1e9f, dz = 1e9f;
            foreach (var p in l)
            {
                if (!p.pas.Contains(c)) continue;
                float d = p.Odl(c);
                if (p.wzdluzX) { if (d < dx) { dx = d; px = p; } }
                else if (d < dz) { dz = d; pz = p; }
            }
            if (px != null && pz != null && px.grupa != pz.grupa) { if (dx <= dz) pz = null; else px = null; }
            return px != null || pz != null;
        }
        public static Func<Vector3, Vector2> Uv2(Pas px, Pas pz) => q => new Vector2(px != null ? px.Uv(q) : 0f, pz != null ? pz.Uv(q) : 0f);
    }

    // ------------------------------------------------------------------ wypalone światło nawierzchni źródłowej (miasto A)
    /// <summary>Trójkąty płaskich siatek źródłowych (UV3 / UV7 CityStaticPBR) w obszarze — łata wbudowana w nawierzchnię miasta A
    /// dostaje to samo wypalone słońce i otoczenie co asfalt dookoła (bez jasnych prostokątów w cieniu budynków).</summary>
    public sealed class Wypalenie
    {
        readonly List<(Vector3 a, Vector3 b, Vector3 c, Vector4 wa, Vector4 wb, Vector4 wc)> t = new List<(Vector3, Vector3, Vector3, Vector4, Vector4, Vector4)>();
        public int Trojkaty => t.Count;
        public Wypalenie(Transform zrodlo, Rect obszar)
        {
            if (!zrodlo) return;
            var b = new Bounds(new Vector3(obszar.center.x, 0f, obszar.center.y), new Vector3(obszar.width + .2f, 400f, obszar.height + .2f));
            foreach (var mf in zrodlo.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>(); var m = mf.sharedMesh;
                if (!r || !r.enabled || !m || !r.bounds.Intersects(b) || r.bounds.size.y > .2f) continue;
                var w = new List<Vector4>(); m.GetUVs(3, w);
                bool ok = w.Count == m.vertexCount && w.Any(q => Mathf.Abs(q.w - 2718f) < .1f);
                if (!ok) { w.Clear(); m.GetUVs(7, w); ok = w.Count == m.vertexCount && w.Any(q => Mathf.Abs(q.w - 2718f) < .1f); }
                if (!ok) continue;
                var v = m.vertices; var tr = m.triangles; var l2w = mf.transform.localToWorldMatrix;
                for (int i = 0; i + 2 < tr.Length; i += 3)
                {
                    Vector3 p0 = l2w.MultiplyPoint3x4(v[tr[i]]), p1 = l2w.MultiplyPoint3x4(v[tr[i + 1]]), p2 = l2w.MultiplyPoint3x4(v[tr[i + 2]]);
                    if (Mathf.Max(p0.x, p1.x, p2.x) < b.min.x || Mathf.Min(p0.x, p1.x, p2.x) > b.max.x || Mathf.Max(p0.z, p1.z, p2.z) < b.min.z || Mathf.Min(p0.z, p1.z, p2.z) > b.max.z) continue;
                    if (Vector3.Cross(p1 - p0, p2 - p0).normalized.y < .9f) continue;
                    t.Add((p0, p1, p2, w[tr[i]], w[tr[i + 1]], w[tr[i + 2]]));
                }
            }
        }
        public bool Probka(Vector3 p, out Vector4 wy)
        {
            wy = new Vector4(1f, 1f, 1f, 2718f); float best = 1e9f; bool jest = false;
            foreach (var q in t)
            {
                var a = new Vector2(q.a.x, q.a.z); var b = new Vector2(q.b.x, q.b.z); var c = new Vector2(q.c.x, q.c.z); var x = new Vector2(p.x, p.z);
                float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y); if (Mathf.Abs(d) < 1e-9f) continue;
                float l1 = ((b.y - c.y) * (x.x - c.x) + (c.x - b.x) * (x.y - c.y)) / d, l2 = ((c.y - a.y) * (x.x - c.x) + (a.x - c.x) * (x.y - c.y)) / d, l3 = 1f - l1 - l2;
                float poza = Mathf.Max(0f, -l1) + Mathf.Max(0f, -l2) + Mathf.Max(0f, -l3);
                if (poza < best) { best = poza; l1 = Mathf.Max(0f, l1); l2 = Mathf.Max(0f, l2); l3 = Mathf.Max(0f, l3); float s = l1 + l2 + l3; wy = (q.wa * l1 + q.wb * l2 + q.wc * l3) / s; jest = true; if (poza <= 0f) break; }
            }
            if (jest) { wy.z = 1f; wy.w = 2718f; }
            return jest && best < .05f;
        }
        /// <summary>UV3 (wypalenie) dla wszystkich wierzchołków siatki obiektu; zwraca liczbę wierzchołków z próbką.</summary>
        public int Nadaj(GameObject g)
        {
            if (!g || t.Count == 0) return 0;
            var mf = g.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) return 0;
            var v = m.vertices; var l2w = g.transform.localToWorldMatrix; var w = new List<Vector4>(v.Length); int n = 0;
            foreach (var p in v) { if (Probka(l2w.MultiplyPoint3x4(p), out var q)) n++; w.Add(q); }
            m.SetUVs(3, w); EditorUtility.SetDirty(m);
            return n;
        }
    }

    // ------------------------------------------------------------------ licznik nakładek i cienkich linii w geometrii
    static readonly string[] SlowaFarby = { "farba drogowa", "farba w wycieciach", "farba w nawierzchni", "oznakowanie", "road paint", "marking paint",
                                             "road weathered white", "centre line", "linie bhp", "linia bhp", "linie stanowisk", "pasy zebry", "zebra", "torowisko farba" };
    static bool Farba(Renderer r)
    {
        string n = BraxKontrola.Norm(r.name);
        if (SlowaFarby.Any(s => n.Contains(" " + s))) return true;
        foreach (var m in r.sharedMaterials) if (m) { string mn = BraxKontrola.Norm(m.name); if (SlowaFarby.Any(s => mn.Contains(" " + s))) return true; }
        return false;
    }

    [Serializable] public class Wynik { public int renderery, trojkaty, pominieteBryly, nakladki, wspolplaszczyznowe, cienkie, materialyZnakow; public List<string> przyklady = new List<string>(); }

    /// <summary>Licznik nakładek farby i linii &lt; 25 cm w geometrii dla całej sceny (log BRAXHORN_NAKLADKI, Logs/r1-nakladki.json).</summary>
    public static Wynik Kontrola(UnityEngine.SceneManagement.Scene scena)
    {
        var w = new Wynik(); var t0 = DateTime.UtcNow;
        var wszystkie = scena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)).Where(r => r && r.enabled).ToList();
        var farby = wszystkie.Where(Farba).ToList();
        var farbySet = new HashSet<Renderer>(farby); int niefarba = 0;
        w.renderery = farby.Count;
        w.materialyZnakow = wszystkie.SelectMany(r => r.sharedMaterials).Where(m => m && m.IsKeywordEnabled("_BRAX_ZNAKI")).Distinct().Count();
        // płaskie trójkąty (n.y > .9) wszystkich rendererów w pobliżu farby — siatka kubełków 2 m
        var kub = new Dictionary<long, List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc, Renderer r)>>();
        long K(int x, int z) => ((long)x << 32) ^ (uint)z;
        var obszar = farby.Select(r => r.bounds).ToList();
        foreach (var r in wszystkie)
        {
            var rb = r.bounds; if (rb.size.y > 40f) continue;
            bool blisko = false; foreach (var b in obszar) if (b.Intersects(new Bounds(rb.center, rb.size + new Vector3(0, .14f, 0)))) { blisko = true; break; }
            if (!blisko) continue;
            var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) continue;
            var v = m.vertices; var tr = m.triangles; var l2w = r.transform.localToWorldMatrix;
            if (tr.Length > 3 * 400000) continue;
            bool farbaR = farbySet.Contains(r);
            for (int i = 0; i + 2 < tr.Length; i += 3)
            {
                Vector3 p0 = l2w.MultiplyPoint3x4(v[tr[i]]), p1 = l2w.MultiplyPoint3x4(v[tr[i + 1]]), p2 = l2w.MultiplyPoint3x4(v[tr[i + 2]]);
                var cr = Vector3.Cross(p1 - p0, p2 - p0); if (cr.sqrMagnitude < 1e-12f || cr.normalized.y < .9f) continue;
                var c = (p0 + p1 + p2) / 3f; bool przy = false;
                foreach (var b in obszar) if (c.x >= b.min.x - .1f && c.x <= b.max.x + .1f && c.z >= b.min.z - .1f && c.z <= b.max.z + .1f && c.y >= b.min.y - .1f && c.y <= b.max.y + .02f) { przy = true; break; }
                if (!przy) continue;
                int x0 = Mathf.FloorToInt(Mathf.Min(p0.x, p1.x, p2.x) / 2f), x1 = Mathf.FloorToInt(Mathf.Max(p0.x, p1.x, p2.x) / 2f);
                int z0 = Mathf.FloorToInt(Mathf.Min(p0.z, p1.z, p2.z) / 2f), z1 = Mathf.FloorToInt(Mathf.Max(p0.z, p1.z, p2.z) / 2f);
                if ((x1 - x0 + 1) * (z1 - z0 + 1) > 400) continue;
                var e = (new Vector2(p0.x, p0.z), new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z), p0.y, p1.y, p2.y, (Renderer)r);
                if (!farbaR) niefarba++;
                for (int x = x0; x <= x1; x++) for (int z = z0; z <= z1; z++) { if (!kub.TryGetValue(K(x, z), out var l)) kub[K(x, z)] = l = new List<(Vector2, Vector2, Vector2, float, float, float, Renderer)>(); l.Add(e); }
            }
        }
        bool Wysokosc(Vector2 p, (Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc, Renderer r) q, out float y)
        {
            y = 0f; float d = (q.b.y - q.c.y) * (q.a.x - q.c.x) + (q.c.x - q.b.x) * (q.a.y - q.c.y); if (Mathf.Abs(d) < 1e-10f) return false;
            float l1 = ((q.b.y - q.c.y) * (p.x - q.c.x) + (q.c.x - q.b.x) * (p.y - q.c.y)) / d, l2 = ((q.c.y - q.a.y) * (p.x - q.c.x) + (q.a.x - q.c.x) * (p.y - q.c.y)) / d, l3 = 1f - l1 - l2;
            if (l1 < 1e-4f || l2 < 1e-4f || l3 < 1e-4f) return false;   // wnętrze trójkąta (styk krawędzią to nie nakładka)
            y = q.ya * l1 + q.yb * l2 + q.yc * l3; return true;
        }
        bool WFarbie1(Renderer r, Vector2 p, float y)
        {
            if (!kub.TryGetValue(K(Mathf.FloorToInt(p.x / 2f), Mathf.FloorToInt(p.y / 2f)), out var l)) return false;
            foreach (var q in l) if (q.r == r && Wysokosc(p, q, out var h) && Mathf.Abs(h - y) < .01f) return true;
            return false;
        }
        // punkt na samej krawędzi dwóch komórek farby (wnętrze trójkąta wymagane) — druga próba o 1.3 mm obok
        bool WFarbie(Renderer r, Vector2 p, float y) => WFarbie1(r, p, y) || WFarbie1(r, p + new Vector2(.0013f, .0007f), y);
        // farba NA ZIEMI (oznakowanie poziome), nie pomalowana część bryły (kozioł oporowy, słupek, ramię krzyża): w promieniu 0.3 m
        // leży nawierzchnia (nie farba) w tej samej wysokości albo do 6 cm niżej. rowno: nawierzchnia dookoła jest W TEJ SAMEJ
        // płaszczyźnie co farba (± 2 mm) — farba wpuszczona w warstwę wierzchnią; to, co leży niżej, jest pod całą jezdnią, nie pod farbą.
        bool NaZiemi(Vector2 c, float y, out bool rowno)
        {
            bool jest = false; rowno = false;
            foreach (var d in new[] { Vector2.zero, new Vector2(.3f, 0), new Vector2(-.3f, 0), new Vector2(0, .3f), new Vector2(0, -.3f) })
            {
                var p = c + d;
                if (!kub.TryGetValue(K(Mathf.FloorToInt(p.x / 2f), Mathf.FloorToInt(p.y / 2f)), out var l)) continue;
                foreach (var q in l)
                {
                    if (farbySet.Contains(q.r) || !Wysokosc(p, q, out var h)) continue;
                    if (h >= y - .06f && h <= y + .005f) jest = true;
                    if (d != Vector2.zero && Mathf.Abs(h - y) <= .002f) { rowno = true; return true; }
                }
            }
            return jest;
        }
        foreach (var r in farby)
        {
            var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) continue;
            var v = m.vertices; var tr = m.triangles; var l2w = r.transform.localToWorldMatrix;
            int krok = Mathf.Max(1, tr.Length / 3 / 6000), cienkieR = 0, nakR = 0;
            for (int i = 0; i + 2 < tr.Length; i += 3 * krok)
            {
                Vector3 p0 = l2w.MultiplyPoint3x4(v[tr[i]]), p1 = l2w.MultiplyPoint3x4(v[tr[i + 1]]), p2 = l2w.MultiplyPoint3x4(v[tr[i + 2]]);
                // poziome (n.y > .995): najazdy progów zwalniających i pochyłe lica brył to nie oznakowanie poziome
                var cr = Vector3.Cross(p1 - p0, p2 - p0); if (cr.sqrMagnitude < 1e-12f || cr.normalized.y < .995f) continue;
                var c = (p0 + p1 + p2) / 3f; var c2 = new Vector2(c.x, c.z);
                if (!NaZiemi(c2, c.y, out bool wpuszczona)) { w.pominieteBryly++; continue; }
                w.trojkaty++;
                // a) nakładka: inna płaska powierzchnia pod farbą (0–6 cm niżej) albo w tej samej wysokości
                if (kub.TryGetValue(K(Mathf.FloorToInt(c.x / 2f), Mathf.FloorToInt(c.z / 2f)), out var l))
                    foreach (var q in l)
                    {
                        if (q.r == r) continue;
                        if (!Wysokosc(c2, q, out var h)) continue;
                        float dy = c.y - h;
                        if (dy < -.0005f || dy > .06f) continue;
                        if (dy > .0005f && wpuszczona) continue;   // farba w płaszczyźnie nawierzchni; niżej leży podłoże całej jezdni
                        if (dy <= .0005f) w.wspolplaszczyznowe++;
                        w.nakladki++; nakR++;
                        if (w.przyklady.Count < 40 && nakR <= 2) w.przyklady.Add("NAKŁADKA " + r.name + " nad " + q.r.name + " o " + (dy * 100f).ToString("F1") + " cm @" + c.ToString("F1"));
                        break;
                    }
                // b) linia węższa niż 25 cm w geometrii: zasięg farby przez środek trójkąta w 4 kierunkach
                float min = 1e9f;
                foreach (var d in new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(.7071f, .7071f), new Vector2(.7071f, -.7071f) })
                {
                    float s = 0f;
                    for (int sg = -1; sg <= 1; sg += 2)
                        for (float k = .01f; k <= .135f; k += .01f) { if (!WFarbie(r, c2 + d * (k * sg), c.y)) break; s += .01f; }
                    min = Mathf.Min(min, s + .01f);
                    if (min < Prog - .015f) break;
                }
                if (min < Prog - .015f)
                {
                    w.cienkie++; cienkieR++;
                    if (w.przyklady.Count < 40 && cienkieR <= 2) w.przyklady.Add("CIENKA LINIA W GEOMETRII " + r.name + " (" + (min * 100f).ToString("F0") + " cm) @" + c.ToString("F1"));
                }
            }
        }
        try
        {
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/r1-nakladki.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p, JsonUtility.ToJson(w, true));
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_NAKLADKI zapis: " + e.Message); }
        var sb = new StringBuilder("BRAXHORN_NAKLADKI nakladki=" + w.nakladki + " (w tej samej wysokości " + w.wspolplaszczyznowe + ") cienkie_geometria=" + w.cienkie +
                                   " — renderery farby " + w.renderery + ", trójkąty farby na ziemi sprawdzone " + w.trojkaty + " (pominięte pomalowane bryły " + w.pominieteBryly +
                                   "), trójkąty nawierzchni w kubełkach " + niefarba + ", materiały z liniami w teksturze " + w.materialyZnakow +
                                   ", czas " + (DateTime.UtcNow - t0).TotalSeconds.ToString("F1") + " s");
        foreach (var x in w.przyklady.Take(12)) sb.Append(" | ").Append(x);
        if (w.nakladki > 0 || w.cienkie > 0) Debug.LogWarning("DO POPRAWKI: " + sb); else Debug.Log(sb.ToString());
        return w;
    }
}
