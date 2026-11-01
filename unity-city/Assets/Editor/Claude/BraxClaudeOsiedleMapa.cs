using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// MAPA OSIEDLA (właściciel 1.11, 17:02: „jakąś mapkę trzeba zrobić, żeby wiedzieć, gdzie iść”). Malowana przy buildzie z tych
// samych danych co drogi i budynki (DrogiA: ulice, rondo, parkingi; część B: ulica, aleja, działki; budynki Codexa), zapisana
// jako tekstura (PNG → import z kompresją) i podpięta do BraxMapa w scenie. MENU → MAPA rysuje ją z pozycją Braxa i podpisami.
public static partial class BraxClaudeOsiedle
{
    static void MapaOsiedla()
    {
        const float x0 = -125f, z0 = -245f, w = 250f, h = 355f, ppm = 3f;
        int W = (Mathf.RoundToInt(w * ppm) + 3) / 4 * 4, H = (Mathf.RoundToInt(h * ppm) + 3) / 4 * 4;   // wielokrotność 4 (kompresja ETC2)
        var px = new Color32[W * H];
        Color32 trawa = new Color32(86, 128, 58, 255), park = new Color32(104, 150, 66, 255), asf = new Color32(52, 55, 58, 255),
                chodnik = new Color32(206, 198, 178, 255), dzialka = new Color32(196, 184, 150, 255), budynek = new Color32(232, 226, 214, 255),
                obiekt = new Color32(170, 120, 92, 255), farba = new Color32(250, 250, 250, 255), woda = new Color32(90, 150, 200, 255);
        for (int i = 0; i < px.Length; i++) px[i] = trawa;
        void Prost(float ax, float bx, float az, float bz, Color32 c)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(ax, bx) - x0) * ppm), 0, W - 1), i1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(ax, bx) - x0) * ppm), 0, W - 1);
            int j0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(az, bz) - z0) * ppm), 0, H - 1), j1 = Mathf.Clamp(Mathf.CeilToInt((Mathf.Max(az, bz) - z0) * ppm), 0, H - 1);
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) px[j * W + i] = c;
        }
        void Kolo(float cx, float cz, float r0, float r1, Color32 c)
        {
            for (int j = Mathf.FloorToInt((cz - r1 - z0) * ppm); j <= Mathf.CeilToInt((cz + r1 - z0) * ppm); j++)
                for (int i = Mathf.FloorToInt((cx - r1 - x0) * ppm); i <= Mathf.CeilToInt((cx + r1 - x0) * ppm); i++)
                {
                    if (i < 0 || j < 0 || i >= W || j >= H) continue;
                    float dx = x0 + i / ppm - cx, dz = z0 + j / ppm - cz, d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d >= r0 && d <= r1) px[j * W + i] = c;
                }
        }
        // parki
        Prost(-55f, 55f, -99f, -67f, park);                       // Park Wiśniowy (A)
        Prost(X0, X1, ParkZ0, FenceA, park);                      // park centralny (B)
        // część B: działki, ulica główna, aleja N-S
        Prost(X0, X1, StreetZ + Half, ParkZ0 - .5f, dzialka);
        Prost(X0, X1, SouthZ, StreetZ - Half, dzialka);
        Prost(X0, X1, StreetZ - Half, StreetZ + Half, chodnik); Prost(X0, X1, StreetZ - Carriage, StreetZ + Carriage, asf);
        Prost(AxisX - Half, AxisX + Half, ParkZ0, StreetZ, chodnik); Prost(AxisX - Carriage, AxisX + Carriage, ParkZ0, StreetZ, asf);
        Prost(AxisX - 2f, AxisX + 2f, FenceA - 2f, ParkZ0, chodnik);   // aleja parku
        Kolo(AxisX + 6f, -132f, 0f, 4.5f, woda);                       // fontanna
        // część A: budynki usługowe, domy, willa
        foreach (var (fx, fz) in new[] { (86f, -42f), (86f, 14f), (-86f, -42f), (-86f, 14f), (86f, 64f), (-86f, 64f) }) Prost(fx - 20f, fx + 20f, fz - 15f, fz + 15f, obiekt);
        foreach (var hs in Houses) Prost(hs.x - 11.5f, hs.x + 11.5f, hs.z - hs.hd, hs.z + hs.hd, dzialka);
        foreach (var hs in Houses) Prost(hs.x - 6f, hs.x + 6f, hs.z - 5f, hs.z + 5f, budynek);
        Prost(-12f, 12f, -46f, -30f, budynek);                         // willa Codexa (Wiśniowa 4)
        Prost(74f, 98f, -78f, -60f, obiekt);                           // siłownia A (za kasynem)
        // drogi A (DrogiA)
        if (ulice != null)
        {
            foreach (var u in ulice)
            {
                float hn = u.HalfNeg, hp = u.HalfPos;
                if (u.alongX) Prost(u.a0, u.a1, u.c - hn, u.c + hp, chodnik); else Prost(u.c - hn, u.c + hp, u.a0, u.a1, chodnik);
            }
            foreach (var u in ulice) { if (u.alongX) Prost(u.a0, u.a1, u.c - u.cw, u.c + u.cw, asf); else Prost(u.c - u.cw, u.c + u.cw, u.a0, u.a1, asf); }
            Kolo(0f, 39f, RondoJezdnia, RondoChodnik, chodnik); Kolo(0f, 39f, RondoWyspa, RondoJezdnia, asf); Kolo(0f, 39f, 0f, RondoWyspa, park);
            foreach (float s in new[] { -1f, 1f })
            {
                foreach (var (a, b) in new[] { (-21.4f, -4.6f), (34.6f, 45.4f), (82.4f, 95.6f) }) Prost(s * 64f, s * 107.5f, a, b, asf);
                Prost(s * 55f, s * 66.6f, -95f, -62f, asf);
            }
        }
        Prost(-4.7f, 4.7f, 97.5f, 110f, asf);   // tunel do miasta
        // obrys
        Prost(X0, X1, FenceA - .4f, FenceA + .4f, farba);
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px); tex.Apply();
        string path = Out + "/mapa_osiedla.png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp)
        {
            imp.textureType = TextureImporterType.Default; imp.mipmapEnabled = false; imp.isReadable = false; imp.maxTextureSize = 1024;
            imp.wrapMode = TextureWrapMode.Clamp; imp.textureCompression = TextureImporterCompression.Compressed; imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
        }
        var go = new GameObject("BRAXHORN mapa osiedla"); go.transform.SetParent(Group(group), false);
        var m = go.AddComponent<BraxMapa>();
        m.tytul = "OSIEDLE TUR POD ZAMOŚCIEM";
        m.obraz = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        m.swiat = new Rect(x0, z0, w, h);
        var lista = new List<BraxMapa.Miejsce>();
        void M(string n, float x, float z) => lista.Add(new BraxMapa.Miejsce { nazwa = n, xz = new Vector2(x, z) });
        M("KASYNO", 86, -42); M("DYSKOTEKA", 86, 14); M("KOMISARIAT", -86, -42); M("SZPITAL", -86, 14); M("SALA BOKSERSKA", -86, 64); M("HALA WALK", 86, 64);
        M("SIŁOWNIA", 86, -69); M("DOM WAWERÓW", -28, 59); M("WILLA", 0, -38); M("RONDO", 0, 39); M("TUNEL DO MIASTA", 0, 104);
        M("PARK WIŚNIOWY", 0, -83); M("PARK", AxisX - 18f, -122f); M("FONTANNA", AxisX + 6f, -138f);
        M("SKLEPY", AxisX + Half + 31f, StreetZ + 14f); M("WILLE", 55f, StreetZ - 24f); M("WILLE", -70f, StreetZ + 24f);
        M("ul. POZNAŃSKA", 9f, 70f); M("ul. WIŚNIOWA", -66f, 2f); M("ul. BYDGOSKA", 66f, 2f);
        m.miejsca = lista.ToArray();
        rep.notes.Add("mapa osiedla: " + W + "×" + H + " px, " + lista.Count + " podpisów");
    }
}
