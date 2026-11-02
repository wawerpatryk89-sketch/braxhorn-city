using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// SZYLDY, TABLICE, EKRANY galerii — tekstury generowane w kodzie (≤ 1K, PNG w Assets/PlayableGenerated/ClaudeRegion/Galeria/Tex)
// i litery przestrzenne. Krój: ten sam geometryczny bezszeryfowy co szyldy osiedla (BraxClaudeOsiedleLitery — linie środkowe
// kresek; czytany przez odbicie, bez kopii danych), rasteryzowany z wygładzaniem (odległość piksela od kreski). Zero plików
// czcionek, zero cudzych marek. Nic nie świeci (ekrany i kasetony jasne kolorem, nie emisją).
public static partial class BraxRegionGaleria
{
    sealed class Znak { public float w; public List<Vector2[]> k; public List<bool> z; public List<Vector2> d; }
    static Dictionary<char, Znak> kroj;
    static readonly Dictionary<string, Material> tekstury = new Dictionary<string, Material>();

    static bool Kroj()
    {
        if (kroj != null) return kroj.Count > 0;
        kroj = new Dictionary<char, Znak>();
        try
        {
            var t = typeof(BraxClaudeOsiedle);
            var f = BindingFlags.NonPublic | BindingFlags.Static;
            t.GetMethod("Krojownia", f).Invoke(null, null);
            var dict = (System.Collections.IDictionary)t.GetField("glify", f).GetValue(null);
            foreach (System.Collections.DictionaryEntry e in dict)
            {
                var gl = e.Value; var gt = gl.GetType();
                kroj[(char)e.Key] = new Znak
                {
                    w = (float)gt.GetField("w").GetValue(gl),
                    k = (List<Vector2[]>)gt.GetField("kreski").GetValue(gl),
                    z = (List<bool>)gt.GetField("zamkniete").GetValue(gl),
                    d = (List<Vector2>)gt.GetField("kropki").GetValue(gl)
                };
            }
        }
        catch (Exception e) { BraxRegion.Note("galeria: krój liter niedostępny (" + e.Message + ") — napisy pominięte"); }
        return kroj.Count > 0;
    }
    const float Odstep = .42f;
    static float Szer(string tekst, float cap, float s = .15f, float track = .12f)
    {
        if (!Kroj()) return 0f;
        float x = 0; int n = 0;
        foreach (char c0 in tekst)
        {
            char c = char.ToUpperInvariant(c0);
            if (c == ' ' || !kroj.TryGetValue(c, out var z)) { x += Odstep * cap; continue; }
            x += (z.w * (1f - s) + s) * cap + track * cap; n++;
        }
        return n > 0 ? x - track * cap : x;
    }
    /// <summary>Przechodzi po kreskach napisu: callback(p0, p1, grubość) w układzie (x w prawo, y w górę) od lewego końca linii bazowej.</summary>
    static void Kreski(string tekst, float cap, float s, float track, Action<Vector2, Vector2, float> seg)
    {
        if (!Kroj()) return;
        float x = 0, sw = s * cap, k = (1f - s) * cap;
        foreach (char c0 in tekst)
        {
            char c = char.ToUpperInvariant(c0);
            if (c == ' ' || !kroj.TryGetValue(c, out var z)) { x += Odstep * cap; continue; }
            Vector2 P(Vector2 q) => new Vector2(x + sw * .5f + q.x * k, sw * .5f + q.y * k);
            for (int i = 0; i < z.k.Count; i++)
            {
                var pts = z.k[i];
                for (int j = 0; j < pts.Length - 1; j++) seg(P(pts[j]), P(pts[j + 1]), sw);
                if (z.z[i] && pts.Length > 2) seg(P(pts[pts.Length - 1]), P(pts[0]), sw);
                if (pts.Length == 1) seg(P(pts[0]), P(pts[0]), sw);
            }
            foreach (var d in z.d) seg(P(d), P(d) + new Vector2(.001f, 0), sw * 1.15f);
            x += (z.w * (1f - s) + s) * cap + track * cap;
        }
    }

    // ---------------- płótno ----------------
    sealed class Plotno
    {
        public int W, H; public Color[] px;
        public Plotno(int w, int h, Color tlo) { W = w; H = h; px = Enumerable.Repeat(tlo, w * h).ToArray(); }
        public void Mieszaj(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || a <= 0f) return;
            int i = y * W + x; var o = px[i]; a = Mathf.Clamp01(a) * c.a;
            px[i] = new Color(Mathf.Lerp(o.r, c.r, a), Mathf.Lerp(o.g, c.g, a), Mathf.Lerp(o.b, c.b, a), Mathf.Max(o.a, a));
        }
        public void Prost(float x0, float y0, float x1, float y1, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0)); y < Mathf.Min(H, Mathf.CeilToInt(y1)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0)); x < Mathf.Min(W, Mathf.CeilToInt(x1)); x++)
                {
                    float ax = Mathf.Clamp01(Mathf.Min(x + 1 - x0, x1 - x)), ay = Mathf.Clamp01(Mathf.Min(y + 1 - y0, y1 - y));
                    Mieszaj(x, y, c, ax * ay);
                }
        }
        public void Kapsula(Vector2 a, Vector2 b, float r, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r - 1)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r - 1)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r + 1));
            var ab = b - a; float l2 = Mathf.Max(ab.sqrMagnitude, 1e-6f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x + .5f, y + .5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                    float d = Vector2.Distance(p, a + ab * t);
                    Mieszaj(x, y, c, r - d + .5f);
                }
        }
        public void Kolo(Vector2 c, float r, Color k) => Kapsula(c, c + new Vector2(.001f, 0), r, k);
        /// <summary>Napis: align −1 = od x w prawo, 0 = środek w x, 1 = kończy się w x. y = linia bazowa (piksele od dołu).</summary>
        public float Tekst(string t, float x, float y, float cap, Color c, int align = 0, float s = .15f, float track = .12f)
        {
            float w = Szer(t, cap, s, track); float x0 = align == 0 ? x - w * .5f : align < 0 ? x : x - w;
            Kreski(t, cap, s, track, (p0, p1, sw) => Kapsula(new Vector2(x0, y) + p0, new Vector2(x0, y) + p1, sw * .5f, c));
            return w;
        }
        public void Gradient(Color dol, Color gora) { for (int y = 0; y < H; y++) { var c = Color.Lerp(dol, gora, y / (float)(H - 1)); for (int x = 0; x < W; x++) px[y * W + x] = c; } }
    }

    /// <summary>Zapis płótna jako PNG + import (≤ 1K) + materiał miasta (przezroczystość z alfy: maska).</summary>
    static Material ZPlotna(string plik, Plotno p, bool maska = false, float chrop = .55f)
    {
        if (tekstury.TryGetValue(plik, out var m) && m) return m;
        var tex = new Texture2D(p.W, p.H, TextureFormat.RGBA32, false); tex.SetPixels(p.px); tex.Apply();
        string path = Dir + "/Tex/" + plik + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = maska; imp.maxTextureSize = Mathf.Min(1024, Mathf.NextPowerOfTwo(Mathf.Max(p.W, p.H)));
            imp.textureCompression = TextureImporterCompression.Compressed; imp.anisoLevel = 4;
            imp.SaveAndReimport();
        }
        var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        m = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Galeria: tablica " + plik, enableInstancing = true };
        m.SetTexture("_MainTex", t2); m.SetColor("_Color", Color.white); m.SetFloat("_Roughness", chrop); m.SetFloat("_Metallic", 0f);
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        if (maska)
        {
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
        }
        AssetDatabase.CreateAsset(m, Dir + "/Materials/tab_" + plik + ".mat");
        tekstury[plik] = m;
        return m;
    }

    /// <summary>Szyld z jednym napisem (wyśrodkowany). capFrac = wysokość wersalików jako ułamek wysokości tekstury.</summary>
    static Material TeksturaNapisu(string plik, int W, int H, Color tlo, Color kolor, string tekst, float capFrac, string pod = null)
    {
        var p = new Plotno(W, H, tlo);
        float cap = H * capFrac;
        if (pod == null)
        {
            float w = Szer(tekst, cap); if (w > W * .9f) cap *= W * .9f / w;
            p.Tekst(tekst, W * .5f, (H - cap) * .5f, cap, kolor);
        }
        else
        {
            float w = Szer(tekst, cap); if (w > W * .9f) cap *= W * .9f / w;
            float cap2 = cap * .38f; float w2 = Szer(pod, cap2, .17f); if (w2 > W * .9f) cap2 *= W * .9f / w2;
            float total = cap + cap2 * 1.9f, y0 = (H - total) * .5f;
            p.Tekst(pod, W * .5f, y0, cap2, kolor, 0, .17f);
            p.Tekst(tekst, W * .5f, y0 + cap2 * 1.9f, cap, kolor);
        }
        return ZPlotna(plik, p, tlo.a < .5f);
    }

    static Material TeksturaPasy(string plik, Color a, Color b)
    {
        var p = new Plotno(512, 64, b);
        for (int x = -64; x < 512 + 64; x += 64)
            for (int y = 0; y < 64; y++)
                for (int dx = 0; dx < 32; dx++) { int xx = x + dx + y; if (xx >= 0 && xx < 512) p.px[y * 512 + xx] = a; }
        return ZPlotna(plik, p);
    }

    /// <summary>Tablica z wierszami: (tekst lewy, tekst prawy) — cennik, oferta, tablica zakładów.</summary>
    static Material TeksturaTablicy(string plik, int W, int H, Color tlo, Color naglowekTlo, Color tekst, Color akcent, string naglowek, IList<(string l, string p)> wiersze, string stopka = null)
    {
        var p = new Plotno(W, H, tlo);
        float hN = H * .17f;
        p.Prost(0, H - hN, W, H, naglowekTlo);
        float capN = hN * .48f; float wN = Szer(naglowek, capN); if (wN > W * .92f) capN *= W * .92f / wN;
        p.Tekst(naglowek, W * .5f, H - hN + (hN - capN) * .5f, capN, akcent);
        float dol = stopka != null ? H * .1f : H * .04f;
        float rowH = (H - hN - dol - H * .03f) / Mathf.Max(1, wiersze.Count);
        float cap = Mathf.Min(rowH * .46f, H * .05f);
        for (int i = 0; i < wiersze.Count; i++)
        {
            float y = H - hN - H * .02f - (i + 1) * rowH;
            if (i % 2 == 1) p.Prost(W * .03f, y - rowH * .2f, W * .97f, y + rowH * .8f, new Color(tlo.r + .05f, tlo.g + .05f, tlo.b + .05f, 1));
            float c = cap; float wl = Szer(wiersze[i].l, c), wp = Szer(wiersze[i].p, c);
            if (wl + wp > W * .86f) c *= W * .86f / (wl + wp);
            p.Tekst(wiersze[i].l, W * .05f, y + (rowH * .6f - c) * .5f, c, tekst, -1);
            p.Tekst(wiersze[i].p, W * .95f, y + (rowH * .6f - c) * .5f, c, akcent, 1);
        }
        if (stopka != null) { float c = H * .035f; float w = Szer(stopka, c, .17f); if (w > W * .9f) c *= W * .9f / w; p.Tekst(stopka, W * .5f, H * .03f, c, tekst, 0, .17f); }
        return ZPlotna(plik, p);
    }

    /// <summary>Plakat / ekran reklamowy: gradient, pas, duży napis, podtytuł (bez cudzych marek).</summary>
    static Material TeksturaPlakatu(string plik, int W, int H, Color dol, Color gora, Color pas, Color napis, string tytul, string pod, int wzor = 0)
    {
        var p = new Plotno(W, H, dol); p.Gradient(dol, gora);
        var rnd = new System.Random(plik.GetHashCode());
        if (wzor == 0) for (int i = 0; i < 9; i++) p.Kolo(new Vector2((float)rnd.NextDouble() * W, (float)rnd.NextDouble() * H), H * (.05f + (float)rnd.NextDouble() * .18f), new Color(1, 1, 1, .07f));
        if (wzor == 1) for (int i = 0; i < 7; i++) { float x = W * (.08f + i * .14f); p.Kapsula(new Vector2(x, 0), new Vector2(x + H * .6f, H), H * .05f, new Color(pas.r, pas.g, pas.b, .35f)); }
        p.Prost(0, H * .12f, W, H * .16f, pas);
        float cap = H * .3f; float w = Szer(tytul, cap); if (w > W * .86f) cap *= W * .86f / w;
        p.Tekst(tytul, W * .5f, H * .45f, cap, napis);
        if (!string.IsNullOrEmpty(pod)) { float c2 = H * .1f; float w2 = Szer(pod, c2, .17f); if (w2 > W * .86f) c2 *= W * .86f / w2; p.Tekst(pod, W * .5f, H * .24f, c2, napis, 0, .17f); }
        return ZPlotna(plik, p);
    }

    /// <summary>Pozioma plansza (napis na nawierzchni) — UV 0..1, przód do góry, obrót yaw.</summary>
    static void PlanszaPozioma(Vector3 c, float w, float h, float yaw, Material m)
    {
        var q = Yaw(yaw); Vector3 r = q * Vector3.right * w * .5f, f = q * Vector3.forward * h * .5f;
        S(m).Quad(c - r - f, c - r + f, c + r + f, c + r - f, g.bake, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
    }

    /// <summary>Litery przestrzenne: o = środek linii bazowej, n = kierunek lica (do oglądającego), cap = wysokość wersalików (m),
    /// depth = głębokość liter. Kreski jako bryły (styk na zakładkę). Zwraca szerokość napisu.</summary>
    static float Napis3D(string tekst, Vector3 o, Vector3 n, float cap, float depth, Material m, float s = .16f)
    {
        if (!Kroj()) return 0f;
        n = n.normalized; var r = Vector3.Cross(Vector3.up, -n).normalized; var up = Vector3.up;
        float w = Szer(tekst, cap, s, .14f);
        Vector3 start = o - r * w * .5f + n * depth * .5f;
        Kreski(tekst, cap, s, .14f, (p0, p1, sw) =>
        {
            Vector3 a = start + r * p0.x + up * p0.y, b = start + r * p1.x + up * p1.y;
            var d = b - a; float L = d.magnitude;
            var q = L < 1e-4f ? Quaternion.LookRotation(n, up) : Quaternion.LookRotation(n, d / L);   // lokalnie y = wzdłuż kreski
            // lokalnie: z = n (głębokość), y = wzdłuż kreski, x = grubość
            BR((a + b) * .5f, new Vector3(sw, L + sw, depth), q, m);
        });
        return w;
    }
}
