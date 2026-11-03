using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// PANORAMA 3D (pkt 7, 7b, 15d; właściciel 2.11: „panorama z wieżowca ma być petarda”, „horyzont lądu za miastem to płaski
// pierścień-obraz jak naklejka, na niebie pionowe tęczowe smugi”).
// ZDJĘTE: pas gór ze zdjęcia na walcu nieba (płaska naklejka; próbkowany bez mipmap → migoczące pionowe smugi) i grzbiety
// 2D w shaderze nieba (płaskie sylwetki). POŁOŻONE: pierścień horyzontu z BRYŁ 3D (BraxPierscien + shader MiastoPierscien):
//  • od morza ARCHIPELAG: 9 wysp 2.8–6.2 km (las, łąki, plaże, klify), 2 latarnie (biel z czerwonymi pasami, laternia),
//    przysiółki rybackie; NASZA WYSPA (x 1200) w prawdziwym kierunku i odległości, dopóki jej scena nie jest wczytana;
//  • od lądu 3 PLANY: wzgórza 1.8 km (pola w szachownicę, lasy, wsie), wzgórza leśne 3.2 km, góry 7 km (skała, las, mgiełka);
//  • mgiełka wg odległości zapieczona w wierzchołkach (kolor mgiełki nieba), cieniowanie słońcem sceny.
// Pierścień jedzie z kamerą (nie da się dopłynąć ani dojść), bryły zawsze za sceną. Log BRAXHORN_PANORAMA.
public static class BraxPanorama
{
    static Material mat;
    static Vector2 Ref => new Vector2(BraxMiastoGranice.SrodekX, BraxMiastoGranice.SrodekZ);
    static float N(float x, float z, int oct = 3) { float s = 0, a = 1, f = 1, n = 0; for (int i = 0; i < oct; i++) { s += a * (Mathf.PerlinNoise(x * f + 311f * i + 77f, z * f + 191f * i + 33f) * 2f - 1f); n += a; a *= .5f; f *= 2.03f; } return s / n; }
    static Color32 C(float r, float g, float b, float mg) => new Color32((byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255), (byte)(Mathf.Clamp01(b) * 255), (byte)(Mathf.Clamp01(mg) * 255));
    static bool Morze(float x, float z) => !BraxMiastoGranice.Lad(x, z);

    sealed class S { public List<Vector3> v = new List<Vector3>(); public List<Color32> c = new List<Color32>(); public List<int> t = new List<int>();
        public void Tri(int a, int b, int d) { t.Add(a); t.Add(b); t.Add(d); }
        public Mesh Zbuduj(string n)
        {
            var m = new Mesh { name = n, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    public static void Apply(Transform cityRoot)
    {
        var sh = Shader.Find("Braxhorn/MiastoPierscien");
        if (!sh) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_PANORAMA brak shadera Braxhorn/MiastoPierscien"); return; }
        mat = new Material(sh) { name = "Panorama: pierścień horyzontu 3D", enableInstancing = false };
        mat.SetColor("_Mgla", new Color(.70f, .76f, .83f)); var sun = BraxCityMaterials.SunDirection.normalized; mat.SetVector("_Slonce", new Vector4(sun.x, sun.y, sun.z, 0));
        System.IO.Directory.CreateDirectory(BraxRegion.Out + "/Panorama");
        BraxZapisAssetu.Utworz(mat, BraxRegion.Out + "/Panorama/pierscien.mat");
        var root = BraxRegion.Root(cityRoot, "CLAUDE panorama 3D (pierścień horyzontu: archipelag, wzgórza, góry)");
        var pier = root.gameObject.AddComponent<BraxPierscien>();
        var lista = new List<BraxPierscien.Element>();
        int nWysp = 0, nLatarni = 0, nWzg = 0, nGor = 0, pominiete = 0;
        var rnd = new System.Random(7031);
        // ---- ARCHIPELAG (kąt od +x, odległość prawdziwa) ----
        var wyspy = new (float kat, float D, float R, float h)[] { (-41f, 5200f, 520f, 120f), (-30f, 3300f, 300f, 55f), (-19f, 6100f, 650f, 170f), (-8f, 2900f, 240f, 38f), (9f, 4500f, 420f, 95f), (18f, 3500f, 280f, 60f), (28f, 5700f, 600f, 150f), (37f, 3100f, 230f, 42f), (44f, 4300f, 380f, 85f) };
        int idx = 0;
        foreach (var w in wyspy)
        {
            float a = w.kat * Mathf.Deg2Rad; var off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w.D;
            var p = Ref + off; if (!Morze(p.x, p.y)) { pominiete++; continue; }
            bool latarnia = idx == 1 || idx == 6, wies = idx == 4 || idx == 2;
            var m = Wyspa(w.R, w.h, w.D, idx * 13.7f, latarnia, wies);
            lista.Add(Element(root, "Archipelag: wyspa " + (idx + 1) + " (" + (w.D / 1000f).ToString("F1") + " km)", m, new Vector3(off.x, -1.7f, off.y), 120f + 380f * w.D / 7000f));
            nWysp++; if (latarnia) nLatarni++; idx++;
        }
        // ---- NASZA WYSPA (prawdziwe położenie) ----
        try
        {
            var mw = BraxMiastoWyspa.SylwetkaPierscien();
            var e = Element(root, "Nasza wyspa (z daleka, prawdziwy kierunek)", mw, new Vector3(BraxMiastoWyspa.CX, -1.7f, BraxMiastoWyspa.CZ), 300f);
            e.dynamiczny = true; e.scena = "BraxhornCity_Wyspa"; lista.Add(e);
        }
        catch (System.Exception ex) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_PANORAMA nasza wyspa: " + ex.Message); }
        // ---- LĄD: 3 plany (kąty od +x co 15°, tylko tam, gdzie w tej odległości jest ląd) ----
        foreach (var (D, glab, hMin, hMax, plan) in new[] { (1800f, 650f, 35f, 110f, 1), (3200f, 950f, 70f, 210f, 2), (7000f, 2600f, 380f, 900f, 3) })
            for (float kat = 60f; kat <= 300f; kat += 15f)
            {
                float a = kat * Mathf.Deg2Rad; var off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (D + glab * .5f);
                var p = Ref + off; if (Morze(p.x, p.y)) continue;
                var m = Pasmo(D, glab, 17f * Mathf.Deg2Rad, a, hMin, hMax, plan, kat * 3.1f + plan * 97f);
                lista.Add(Element(root, "Ląd za miastem: plan " + plan + " (" + (D / 1000f).ToString("F1") + " km, " + kat.ToString("F0") + "°)", m, new Vector3(off.x, 0f, off.y), 120f + 380f * D / 9000f));
                if (plan == 3) nGor++; else nWzg++;
            }
        pier.elementy = lista.ToArray();
        // ---- zdjęcie naklejki: pas gór ze zdjęcia i grzbiety 2D w niebie ----
        var niebo = RenderSettings.skybox;
        if (niebo && niebo.shader && niebo.shader.name == "Braxhorn/MiastoNiebo")
        {
            niebo.SetFloat("_PasmoJest", 0f);
            var g = niebo.GetVector("_GrzbietyR"); niebo.SetVector("_GrzbietyR", new Vector4(g.x, g.y, 0f, 0f));
            EditorUtility.SetDirty(niebo);
        }
        // wydajność (koordynator 03:xx): pierścień nigdy nie rzuca ani nie odbiera cieni, bez kolizji, bez sond odbić/światła
        int bezCienia = 0, kolizjeUsuniete = 0;
        foreach (var rr in root.GetComponentsInChildren<Renderer>(true))
        {
            if (rr.shadowCastingMode != ShadowCastingMode.Off || rr.receiveShadows) bezCienia++;
            rr.shadowCastingMode = ShadowCastingMode.Off; rr.receiveShadows = false;
            rr.lightProbeUsage = LightProbeUsage.Off; rr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        foreach (var cc in root.GetComponentsInChildren<Collider>(true)) { UnityEngine.Object.DestroyImmediate(cc); kolizjeUsuniete++; }
        string log = "BRAXHORN_PANORAMA wyspy=" + nWysp + " poprawione_cienie=" + bezCienia + " kolizje_usuniete=" + kolizjeUsuniete + " latarnie=" + nLatarni + " nasza_wyspa=tak wzgorza=" + nWzg + " gory=" + nGor + " pominiete_na_ladzie=" + pominiete
            + " pasmo_zdjecia=wylaczone grzbiety_2D=wylaczone smugi=0 (pierścień 3D z kamerą, głębia za sceną)";
        Debug.Log(log); BraxRegion.Note(log);
    }

    // element: obiekt z siatką w prawdziwej skali (środek = 0); offset — środek względem kamery (pierścień) albo punkt świata
    static BraxPierscien.Element Element(Transform root, string nazwa, Mesh m, Vector3 offset, float R)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(root, false);
        // przy buildzie: maleńki, głęboko pod startem (okolica siłowni zostaje w scenie startowej — nie trafi do kwadratu,
        // nie przeszkadza kontrolom); w grze BraxPierscien stawia go co klatkę przy kamerze
        g.transform.position = new Vector3(1.5f, -5000f, 2.8f); g.transform.localScale = Vector3.one * .001f;
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Panorama/" + BraxUliceKit.Bezpieczna(nazwa) + ".asset");
        g.AddComponent<MeshFilter>().sharedMesh = m;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return new BraxPierscien.Element { t = g.transform, offset = offset, R = R };
    }

    // wyspa archipelagu (prawdziwa skala, podstawa na lustrze wody y = 0 lokalnie)
    static Mesh Wyspa(float R, float hPeak, float D, float ziarno, bool latarnia, bool wies)
    {
        var s = new S(); int n = 30; float ext = R * 1.35f, mg0 = Mathf.Clamp01(.18f + .42f * (D - 2800f) / 3500f);
        float H(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z), warp = N(x / R * 1.3f + ziarno, z / R * 1.3f) * R * .28f, rr = r + warp;
            if (rr >= R) return -2f;
            float u = 1f - rr / R; float h = hPeak * Mathf.Pow(u, 1.4f) * (.75f + .25f * N(x / R * 3f + ziarno, z / R * 3f));
            return h + Mathf.Max(0f, N(x / R * 6f, z / R * 6f + ziarno)) * hPeak * .12f * u;
        }
        var idx = new int[n + 1, n + 1]; var hh = new float[n + 1, n + 1];
        for (int i = 0; i <= n; i++) for (int j = 0; j <= n; j++)
            {
                float x = -ext + 2f * ext * i / n, z = -ext + 2f * ext * j / n, h = H(x, z); hh[i, j] = h;
                idx[i, j] = s.v.Count; s.v.Add(new Vector3(x, Mathf.Max(h, -2f), z));
                float sl = 0f; if (i > 0 && j > 0) sl = Mathf.Abs(h - hh[i - 1, j]) + Mathf.Abs(h - hh[i, j - 1]);
                float las = Mathf.Clamp01(.5f + N(x * .01f + ziarno, z * .01f) * 1.4f), mg = mg0 * (1f - .25f * Mathf.Clamp01(h / hPeak));
                Color32 c;
                if (h < 3.5f) c = C(.78f, .72f, .56f, mg);                                            // plaża
                else if (sl > ext / n * 1.1f) c = C(.46f, .45f, .42f, mg);                            // klif / skała
                else if (las > .45f) c = C(.17f + .05f * las, .27f + .06f * las, .14f, mg);           // las
                else c = C(.40f, .50f, .25f, mg);                                                     // łąka
                s.c.Add(c);
            }
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            {
                if (hh[i, j] < -1.5f && hh[i + 1, j] < -1.5f && hh[i, j + 1] < -1.5f && hh[i + 1, j + 1] < -1.5f) continue;
                s.Tri(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1]); s.Tri(idx[i, j], idx[i + 1, j + 1], idx[i + 1, j]);
            }
        if (latarnia)
        {   // latarnia na brzegu (wieża 32 m: biel z 2 czerwonymi pasami, galeria, laternia, czerwona kopuła)
            float lx = R * .78f, lz = 0f; for (float t = .9f; t > .4f; t -= .02f) { if (H(R * t, 0) > 3f) { lx = R * t; break; } }
            float y0 = Mathf.Max(0f, H(lx, lz));
            Walec(s, new Vector3(lx, y0, lz), 3.4f, 2.5f, 26f, 16, y => (y > 8f && y < 12f) || (y > 18f && y < 22f) ? C(.75f, .1f, .08f, mg0) : C(.94f, .94f, .92f, mg0));
            Walec(s, new Vector3(lx, y0 + 26f, lz), 3.6f, 3.6f, .8f, 16, y => C(.2f, .2f, .22f, mg0));
            Walec(s, new Vector3(lx, y0 + 26.8f, lz), 2.2f, 2.2f, 3.4f, 16, y => C(.55f, .6f, .62f, mg0));
            Walec(s, new Vector3(lx, y0 + 30.2f, lz), 2.6f, .2f, 2.2f, 16, y => C(.62f, .1f, .08f, mg0));
        }
        if (wies)
        {   // przysiółek rybacki: kilka domów (biel, czerwone dachy) na łagodnym stoku przy brzegu
            var rr = new System.Random((int)ziarno + 5); int nd = 0;
            for (int k = 0; k < 40 && nd < 7; k++)
            {
                float x = (float)(rr.NextDouble() * 2 - 1) * R * .7f, z = (float)(rr.NextDouble() * 2 - 1) * R * .7f, h = H(x, z);
                if (h < 4f || h > hPeak * .35f) continue;
                Dom(s, new Vector3(x, h, z), 9f + (float)rr.NextDouble() * 5f, 6f, 5f, (float)rr.NextDouble() * 180f, mg0); nd++;
            }
        }
        return s.Zbuduj("Archipelag: wyspa");
    }

    // pasmo lądu (plan 1–3) w kierunku kąta a: pas wzgórz szerokości kątowej dk na odległości D (głębia glab), lokalnie wokół 0
    // (środek pierścienia = kamera), podstawa y = 0 (poziom lądu)
    static Mesh Pasmo(float D, float glab, float dk, float a, float hMin, float hMax, int plan, float ziarno)
    {
        var s = new S(); int nA = 26, nR = 9; float mg0 = plan == 1 ? .22f : plan == 2 ? .42f : .62f;
        var idx = new int[nA + 1, nR + 1]; float Dc = D + glab * .5f; var cS = new Vector3(Mathf.Cos(a) * Dc, 0f, Mathf.Sin(a) * Dc);   // środek pasa (lokalne 0)
        for (int i = 0; i <= nA; i++)
            for (int j = 0; j <= nR; j++)
            {
                float ka = a - dk * .5f + dk * i / nA, rr = D + glab * j / nR, u = (float)j / nR;
                float x = Mathf.Cos(ka) * rr, z = Mathf.Sin(ka) * rr;
                float grzbiet = Mathf.Lerp(hMin, hMax, Mathf.Clamp01(.5f + N(ka * 9f + ziarno, plan * 3.3f, 4) * .9f));
                float prof = Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI * .9f);                      // od podnóża (0) do grzbietu i za grzbiet
                float h = grzbiet * prof * (.85f + .3f * N(x * .004f + ziarno, z * .004f));
                if (j == 0) h = 0f;
                idx[i, j] = s.v.Count; s.v.Add(new Vector3(x, h, z) - cS);
                float las = N(x * .006f + ziarno, z * .006f + 4f), mg = mg0 * (1f - .2f * Mathf.Clamp01(h / hMax)) + .08f * u;
                Color32 c;
                if (plan == 3) c = h > hMax * .7f ? C(.50f, .50f, .52f, mg) : (las > -.1f ? C(.20f, .28f, .20f, mg) : C(.42f, .44f, .40f, mg));
                else if (plan == 2) c = las > -.3f ? C(.16f, .25f, .13f, mg) : C(.36f, .45f, .24f, mg);
                else
                {
                    bool pole = h < hMax * .45f && las < .15f;
                    if (pole) { int px = Mathf.FloorToInt(x / 140f), pz = Mathf.FloorToInt(z / 110f); int w = ((px * 7349) ^ (pz * 3163)) & 3;
                        c = w == 0 ? C(.62f, .58f, .30f, mg) : w == 1 ? C(.42f, .54f, .24f, mg) : w == 2 ? C(.52f, .60f, .30f, mg) : C(.36f, .46f, .22f, mg); }
                    else c = C(.17f, .27f, .14f, mg);
                }
                s.c.Add(c);
            }
        for (int i = 0; i < nA; i++) for (int j = 0; j < nR; j++) { s.Tri(idx[i, j], idx[i + 1, j + 1], idx[i, j + 1]); s.Tri(idx[i, j], idx[i + 1, j], idx[i + 1, j + 1]); }   // kąt rośnie przeciwnie do wskazówek — lico w górę
        if (plan == 1)
        {   // wsie: skupiska domów na polach przy podnóżu
            var rr = new System.Random((int)ziarno); int nd = 0;
            for (int k = 0; k < 30 && nd < 10; k++)
            {
                float ka = a + ((float)rr.NextDouble() - .5f) * dk * .8f, r0 = D + glab * (.1f + .25f * (float)rr.NextDouble());
                float x = Mathf.Cos(ka) * r0, z = Mathf.Sin(ka) * r0, u = (r0 - D) / glab;
                float h = Mathf.Lerp(hMin, hMax, .5f) * Mathf.Sin(Mathf.Clamp01(u * 1.15f) * Mathf.PI * .9f) * .85f;
                Dom(s, new Vector3(x, h, z) - cS, 10f + (float)rr.NextDouble() * 6f, 8f, 6f, (float)rr.NextDouble() * 180f, mg0 * .9f); nd++;
            }
        }
        return s.Zbuduj("Ląd za miastem: plan " + plan);
    }

    static void Walec(S s, Vector3 p, float r0, float r1, float h, int seg, System.Func<float, Color32> kolor)
    {
        int pierscieni = Mathf.Max(2, Mathf.CeilToInt(h / 2f));
        int b = s.v.Count;
        for (int j = 0; j <= pierscieni; j++)
        {
            float y = h * j / pierscieni, r = Mathf.Lerp(r0, r1, (float)j / pierscieni);
            for (int i = 0; i < seg; i++) { float a = i * Mathf.PI * 2f / seg; s.v.Add(p + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); s.c.Add(kolor(y)); }
        }
        for (int j = 0; j < pierscieni; j++) for (int i = 0; i < seg; i++)
            {
                int a0 = b + j * seg + i, a1 = b + j * seg + (i + 1) % seg, c0 = a0 + seg, c1 = a1 + seg;
                s.Tri(a0, c0, c1); s.Tri(a0, c1, a1);
            }
        int top = s.v.Count; s.v.Add(p + Vector3.up * h); s.c.Add(kolor(h));
        for (int i = 0; i < seg; i++) s.Tri(b + pierscieni * seg + i, top, b + pierscieni * seg + (i + 1) % seg);
    }

    static void Dom(S s, Vector3 p, float w, float d, float h, float yaw, float mg)
    {
        var q = Quaternion.Euler(0, yaw, 0); Vector3 P(float x, float y, float z) => p + q * new Vector3(x, y, z);
        var sc = C(.92f, .91f, .88f, mg); var dc = C(.62f, .2f, .14f, mg);
        int b = s.v.Count;
        var pts = new[] { P(-w / 2, -1, -d / 2), P(w / 2, -1, -d / 2), P(w / 2, -1, d / 2), P(-w / 2, -1, d / 2), P(-w / 2, h, -d / 2), P(w / 2, h, -d / 2), P(w / 2, h, d / 2), P(-w / 2, h, d / 2) };
        foreach (var v in pts) { s.v.Add(v); s.c.Add(sc); }
        int[,] f = { { 0, 4, 5, 1 }, { 1, 5, 6, 2 }, { 2, 6, 7, 3 }, { 3, 7, 4, 0 } };
        for (int k = 0; k < 4; k++) { s.Tri(b + f[k, 0], b + f[k, 1], b + f[k, 2]); s.Tri(b + f[k, 0], b + f[k, 2], b + f[k, 3]); }
        int r = s.v.Count;   // dach dwuspadowy
        foreach (var v in new[] { P(-w / 2 - .4f, h, -d / 2 - .4f), P(w / 2 + .4f, h, -d / 2 - .4f), P(w / 2 + .4f, h, d / 2 + .4f), P(-w / 2 - .4f, h, d / 2 + .4f), P(-w / 2 - .4f, h + d * .45f, 0), P(w / 2 + .4f, h + d * .45f, 0) }) { s.v.Add(v); s.c.Add(dc); }
        s.Tri(r + 0, r + 4, r + 5); s.Tri(r + 0, r + 5, r + 1); s.Tri(r + 2, r + 5, r + 4); s.Tri(r + 2, r + 4, r + 3); s.Tri(r + 3, r + 4, r + 0); s.Tri(r + 1, r + 5, r + 2);
    }
}
