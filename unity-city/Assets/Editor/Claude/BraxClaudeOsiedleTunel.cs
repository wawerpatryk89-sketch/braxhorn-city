using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// TUNEL DO MIASTA — build 14 (zdjęcia właściciela z buildu 12, 22:14 i 22:58): w otworze tunelu była płaska zielona trawa
// (wzgórze kraniec mapy przechodziło przez wnętrze), przed wjazdem płot obwodowy osiedla, nad portalem płaska zielona
// „ściana”. Teraz:
//  • wnętrze: prawdziwy tunel 110 m w głąb — jezdnia z liniami, chodniki serwisowe z krawężnikiem, ściany z jasnych płyt,
//    sklepienie z ciemnego betonu, rząd lamp tunelowych po obu stronach co 4 m, odblaski na ścianach, jasny wylot daleko.
//    Światło policzone przy budowie w wierzchołkach (dzień przez portal, plamy lamp, dzień od wylotu) — ciemno w środku
//    w każdym trybie grafiki (shader Braxhorn/OsiedleTunel, nic nie świeci na otoczenie);
//  • płot obwodowy i stara tablica „CENTRUM · WRÓĆ DO MIASTA” sprzed portalu usunięte, pas betonu przed portalem;
//  • SZLABAN: słupek z napędem, ramię biało-czerwone (pasy co 0.5 m) z podpórką, budka szlabanowa z oknami, tablica
//    „MIASTO · WKRÓTCE” na ramieniu. W aplikacji „osiedle” (miasta nie ma w paczce) szlaban zamknięty, AKCJA przy nim:
//    „TUNEL · MIASTO WKRÓTCE”; gdy miasto jest w paczce — podnosi się przy podejściu (BraxSzlaban);
//  • wzgórze nad tunelem (zbocze z trawą, skałą, drzewami, krzewami) — BraxClaudeOsiedleKraniec.cs (teren dookoła).
public static partial class BraxClaudeOsiedle
{
    const float TunPz = 99.5f, TunHalf = 4.7f, TunZ0 = 101f, TunDl = 110f, TunScian = 4.4f, TunSzczyt = 6f, TunChodnik = .3f, TunJezdnia = 3.7f;
    static int tunelLampy;

    static Material TunelMat(string key, string tex, float kafel, Color c, float jasnosc)
    {
        if (mats.TryGetValue("tunel " + key, out var have)) return have;
        var sh = Shader.Find("Braxhorn/OsiedleTunel");
        var m = sh ? new Material(sh) : new Material(Shader.Find("Unlit/Texture"));
        m.name = "Osiedle: tunel " + key;
        if (!string.IsNullOrEmpty(tex) && System.IO.File.Exists(Tex + tex + "/albedo.jpg")) m.mainTexture = Map(Tex + tex + "/albedo.jpg", false, true);
        m.SetColor("_Color", c); m.SetFloat("_Jasnosc", jasnosc);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe("tunel " + key) + ".mat"); mats["tunel " + key] = m; tile[m] = kafel; return m;
    }

    // Światło wnętrza w punkcie p o normalnej n (liniowo): dzień przez portal i od wylotu, lampy po obu stronach co 4 m.
    static Color SwiatloTunelu(Vector3 p, Vector3 n)
    {
        float z = p.z - TunZ0;
        float dzien = .95f * Mathf.Exp(-Mathf.Max(0, z) / 6f) * Mathf.Clamp01(.35f + .65f * Mathf.Max(0, -n.z) + .4f * Mathf.Max(0, n.y));
        float wylot = .8f * Mathf.Exp(-Mathf.Max(0, TunDl - z) / 8f);
        float lampy = 0f;
        int k0 = Mathf.FloorToInt((z - 2f) / 4f);
        for (int k = k0 - 4; k <= k0 + 4; k++)
        {
            float lz = TunZ0 + 2f + 4f * k; if (lz < TunZ0 + 1.5f || lz > TunZ0 + TunDl - 1.5f) continue;
            foreach (float sx in new[] { -1f, 1f })
            {
                var d = new Vector3(sx * 4.4f, 4.7f, lz) - p; float dist = Mathf.Max(.25f, d.magnitude);
                float cos = Mathf.Max(.12f, Vector3.Dot(n, d / dist));
                lampy += .6f * cos / (1f + dist * dist / 3.2f);
            }
        }
        var c = new Color(1f, .93f, .80f) * lampy + new Color(1f, .98f, .94f) * (dzien + wylot) + new Color(.85f, .9f, 1f) * .03f;
        c.a = 1f; return c;
    }

    sealed class TunelSiatka
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Color> c = new List<Color>(); public readonly List<int> i = new List<int>();
        public bool swiatlo = true;
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 n)
        {
            int k = v.Count;
            foreach (var (p, u) in new[] { (a, ua), (b, ub), (cc, uc), (d, ud) })
            { v.Add(p); uv.Add(u); c.Add(swiatlo ? SwiatloTunelu(p, n) : Color.white); }
            bool flip = Vector3.Dot(Vector3.Cross(b - a, cc - a), n) < 0;
            if (!flip) i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); else i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
        }
    }

    static int tunelSiatki;
    static GameObject TunelObiekt(string nazwa, TunelSiatka s, Material m)
    {
        if (s.v.Count == 0 || !m) return null;
        var mesh = new Mesh { name = "Tunel " + nazwa, indexFormat = s.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(s.v); mesh.SetUVs(0, s.uv); mesh.SetColors(s.c); mesh.SetTriangles(s.i, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Out + "/tunel_" + (tunelSiatki++) + ".asset");
        var g = new GameObject("tunel: " + nazwa); g.transform.SetParent(Group(group), false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return g;
    }

    static void TunelDoMiasta(Transform estate, Transform physics)
    {
        group = "Osiedle A / tunel do miasta";
        // 0) stare wnętrze (ciemne pudło 14 m z płaską ścianą końcową) i jezdnia tunelu wyjęte z paczek geometrii portalu
        int stare = 0; const string gPortal = "Osiedle A / wylot tunelu z miasta";
        foreach (var key in batches.Keys.Where(k => k.Item1 == gPortal && k.Item2 && (k.Item2.name.Contains("tunel mrok") || k.Item2 == Asphalt)).ToList())
        { stare += batches[key].i.Count / 3; batches.Remove(key); }
        // jezdnia przed portalem (w słońcu) jak była: −4 m … wylot
        BoxMinMax(Asphalt, -TunHalf, TunHalf, 0, .06f, TunPz - 4f, TunZ0, false);
        // 1) płot obwodowy (z ≈ 99.05) i tablica Codexa (z 97.8) sprzed portalu; pas betonu na miejscu płotu
        int przed = rep.oldFenceTrianglesRemoved;
        CutVolumes(estate, physics, new List<Bounds> { MinMax(-7.6f, 7.6f, -.05f, 1.75f, 98.55f, 99.45f), MinMax(-3.4f, 3.4f, 1.4f, 3.05f, 97.5f, 98.1f) });
        int plot = rep.oldFenceTrianglesRemoved - przed;
        BoxMinMax(Concrete, -8.2f, -TunHalf, 0, .035f, 98.5f, 99.5f, false); BoxMinMax(Concrete, TunHalf, 8.2f, 0, .035f, 98.5f, 99.5f, false);

        // 2) wnętrze tunelu
        var jezdnia = new TunelSiatka(); var chodnik = new TunelSiatka(); var plyty = new TunelSiatka(); var beton = new TunelSiatka();
        var farba = new TunelSiatka(); var lampy = new TunelSiatka(); var odblaski = new TunelSiatka(); var wylot = new TunelSiatka();
        lampy.swiatlo = false; odblaski.swiatlo = false; wylot.swiatlo = false;
        const float dz = 1f; int seg = Mathf.RoundToInt(TunDl / dz);
        const int luk = 10;
        Vector3 Luk(int k, float z) { float t = Mathf.PI * k / luk; return new Vector3(TunHalf * Mathf.Cos(t), TunScian + (TunSzczyt - TunScian) * Mathf.Sin(t), z); }
        for (int s = 0; s < seg; s++)
        {
            float z0 = TunZ0 + s * dz, z1 = z0 + dz;
            // jezdnia (asfalt) w 4 pasach (gradient światła w poprzek)
            for (int q = 0; q < 4; q++)
            {
                float xa = -TunJezdnia + q * TunJezdnia * .5f, xb = xa + TunJezdnia * .5f;
                jezdnia.Quad(new Vector3(xa, .06f, z0), new Vector3(xb, .06f, z0), new Vector3(xb, .06f, z1), new Vector3(xa, .06f, z1),
                             new Vector2(xa / 3.5f, z0 / 3.5f), new Vector2(xb / 3.5f, z0 / 3.5f), new Vector2(xb / 3.5f, z1 / 3.5f), new Vector2(xa / 3.5f, z1 / 3.5f), Vector3.up);
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                // chodnik serwisowy z krawężnikiem
                float xk = sx * TunJezdnia, xw = sx * TunHalf;
                chodnik.Quad(new Vector3(xk, TunChodnik, z0), new Vector3(xw, TunChodnik, z0), new Vector3(xw, TunChodnik, z1), new Vector3(xk, TunChodnik, z1),
                             new Vector2(xk, z0), new Vector2(xw, z0), new Vector2(xw, z1), new Vector2(xk, z1), Vector3.up);
                chodnik.Quad(new Vector3(xk, .06f, z0), new Vector3(xk, TunChodnik, z0), new Vector3(xk, TunChodnik, z1), new Vector3(xk, .06f, z1),
                             new Vector2(z0, 0), new Vector2(z0, .25f), new Vector2(z1, .25f), new Vector2(z1, 0), new Vector3(-sx, 0, 0));
                // ściana: jasne płyty do 3.4 m (3 rzędy), ciemniejszy beton wyżej
                var nn = new Vector3(-sx, 0, 0);
                float[] ys = { TunChodnik, 1.3f, 2.35f, 3.4f };
                for (int r = 0; r < ys.Length - 1; r++)
                    plyty.Quad(new Vector3(xw, ys[r], z0), new Vector3(xw, ys[r], z1), new Vector3(xw, ys[r + 1], z1), new Vector3(xw, ys[r + 1], z0),
                               new Vector2(z0 / 1.2f, ys[r] / 1.2f), new Vector2(z1 / 1.2f, ys[r] / 1.2f), new Vector2(z1 / 1.2f, ys[r + 1] / 1.2f), new Vector2(z0 / 1.2f, ys[r + 1] / 1.2f), nn);
                beton.Quad(new Vector3(xw, 3.4f, z0), new Vector3(xw, 3.4f, z1), new Vector3(xw, TunScian, z1), new Vector3(xw, TunScian, z0),
                           new Vector2(z0 / 2.8f, 3.4f / 2.8f), new Vector2(z1 / 2.8f, 3.4f / 2.8f), new Vector2(z1 / 2.8f, TunScian / 2.8f), new Vector2(z0 / 2.8f, TunScian / 2.8f), nn);
            }
            // sklepienie (łuk eliptyczny 4.4 → 6.0 m)
            for (int k = 0; k < luk; k++)
            {
                Vector3 a0 = Luk(k, z0), a1 = Luk(k + 1, z0), b1 = Luk(k + 1, z1), b0 = Luk(k, z1);
                var mid = (a0 + a1) * .5f; var n = (new Vector3(0, TunScian, mid.z) - mid); n = new Vector3(n.x / (TunHalf * TunHalf), n.y / ((TunSzczyt - TunScian) * (TunSzczyt - TunScian)), 0).normalized;
                float u0 = k * 1.2f / 2.8f, u1 = (k + 1) * 1.2f / 2.8f;
                beton.Quad(a0, a1, b1, b0, new Vector2(z0 / 2.8f, u0), new Vector2(z0 / 2.8f, u1), new Vector2(z1 / 2.8f, u1), new Vector2(z1 / 2.8f, u0), n);
            }
        }
        // linie: środkowa przerywana (3 m / 6 m) i krawędziowe ciągłe
        for (float z = TunZ0; z < TunZ0 + TunDl - .1f; z += 9f)
            farba.Quad(new Vector3(-.06f, .066f, z), new Vector3(.06f, .066f, z), new Vector3(.06f, .066f, z + 3f), new Vector3(-.06f, .066f, z + 3f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector3.up);
        foreach (float sx in new[] { -1f, 1f })
            for (float z = TunZ0; z < TunZ0 + TunDl - .1f; z += 2f)
            {
                float xa = sx * (TunJezdnia - .35f), xb = sx * (TunJezdnia - .23f);
                farba.Quad(new Vector3(xa, .066f, z), new Vector3(xb, .066f, z), new Vector3(xb, .066f, z + 2f), new Vector3(xa, .066f, z + 2f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector3.up);
            }
        // lampy tunelowe: oprawy 1.3 m pod łukiem po obu stronach co 4 m (jasna oprawa, nie emisja)
        void Pud(TunelSiatka t, Vector3 c, Vector3 r)
        {
            var h = r * .5f;
            Vector3 P(float x, float y, float z) => c + new Vector3(x * h.x, y * h.y, z * h.z);
            t.Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector3.down);
            t.Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector3.left);
            t.Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector3.right);
            t.Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector3.back);
            t.Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector3.forward);
        }
        tunelLampy = 0;
        for (float lz = TunZ0 + 2f; lz <= TunZ0 + TunDl - 1.5f; lz += 4f)
            foreach (float sx in new[] { -1f, 1f }) { Pud(lampy, new Vector3(sx * 4.42f, 4.72f, lz), new Vector3(.2f, .1f, 1.3f)); tunelLampy++; }
        // odblaski na ścianach co 12 m (pomarańczowe prostokąty na 0.9 m)
        for (float oz = TunZ0 + 6f; oz < TunZ0 + TunDl - 2f; oz += 12f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float x = sx * (TunHalf - .012f);
                odblaski.Quad(new Vector3(x, .85f, oz - .05f), new Vector3(x, .85f, oz + .05f), new Vector3(x, 1.05f, oz + .05f), new Vector3(x, 1.05f, oz - .05f),
                              Vector2.zero, Vector2.right, Vector2.one, Vector2.up, new Vector3(-sx, 0, 0));
            }
        // jasny wylot daleko: przekrój tunelu wypełniony światłem dnia (u góry niebo, niżej jaśniejsza zieleń i droga)
        {
            float z = TunZ0 + TunDl;
            var pts = new List<Vector3> { new Vector3(-TunHalf, .06f, z), new Vector3(TunHalf, .06f, z) };
            for (int k = 0; k <= luk; k++) pts.Add(Luk(k, z));
            pts.Add(new Vector3(-TunHalf, TunScian, z));
            var cen = new Vector3(0, 2.6f, z);
            Color Kol(Vector3 p) { float t = Mathf.Clamp01(p.y / TunSzczyt); return Color.Lerp(new Color(.80f, .84f, .74f), new Color(.92f, .96f, 1.0f), t); }
            int k0 = wylot.v.Count; wylot.v.Add(cen); wylot.uv.Add(Vector2.zero); wylot.c.Add(Kol(cen) * 1.05f);
            var obrys = new List<Vector3> { new Vector3(-TunHalf, .06f, z), new Vector3(TunHalf, .06f, z) };
            for (int k = 0; k <= luk; k++) obrys.Add(Luk(k, z));
            for (int k = 0; k < obrys.Count; k++) { wylot.v.Add(obrys[k]); wylot.uv.Add(Vector2.zero); wylot.c.Add(Kol(obrys[k])); }
            for (int k = 0; k < obrys.Count; k++) { int a = k0 + 1 + k, b = k0 + 1 + (k + 1) % obrys.Count; wylot.i.AddRange(new[] { k0, b, a }); }
        }
        var mAsf = TunelMat("jezdnia", "asphalt_04", 3.5f, new Color(.62f, .62f, .64f), 1f);
        var mChod = TunelMat("chodnik", "concrete_pavement_02", 1f, new Color(.78f, .78f, .76f), 1f);
        var mPly = TunelMat("płyty ścian", "concrete_pavement_02", 1.2f, new Color(.93f, .92f, .88f), 1.05f);
        var mBet = TunelMat("sklepienie", "concrete_pavement_02", 2.8f, new Color(.42f, .42f, .43f), 1f);
        var mFar = TunelMat("linie", null, 1f, new Color(.92f, .92f, .88f), 1f);
        var mLamp = TunelMat("oprawy lamp", null, 1f, new Color(1f, .95f, .84f), 2.2f);
        var mOdb = TunelMat("odblaski", null, 1f, new Color(1f, .55f, .12f), .9f);
        var mWyl = TunelMat("wylot (dzień)", null, 1f, Color.white, 1.35f);
        TunelObiekt("jezdnia", jezdnia, mAsf); TunelObiekt("chodniki", chodnik, mChod); TunelObiekt("ściany z płyt", plyty, mPly);
        TunelObiekt("sklepienie", beton, mBet); TunelObiekt("linie", farba, mFar); TunelObiekt("lampy", lampy, mLamp);
        TunelObiekt("odblaski", odblaski, mOdb); TunelObiekt("wylot", wylot, mWyl);
        // ściany z kolizją do miejsca wczytania miasta (wariant z miastem), dalej i tak nikt nie dojdzie
        Collider(new Vector3(-TunHalf - .15f, 3f, TunZ0 + 10f), new Vector3(.3f, 6f, 20f)); Collider(new Vector3(TunHalf + .15f, 3f, TunZ0 + 10f), new Vector3(.3f, 6f, 20f));

        // 3) szlaban z budką
        Szlaban();
        rep.notes.Add("tunel do miasta: wnętrze " + TunDl + " m (lampy " + tunelLampy + ", odblaski, linie, jasny wylot), szlaban zamknięty w aplikacji osiedla, stare wnętrze −" + stare + " trójkątów, płot przed portalem −" + plot + " trójkątów");
        Debug.Log("BRAXHORN_TUNEL wnętrze=" + TunDl + "m lampy=" + tunelLampy + " szlaban=tak budka=tak płot_przed_portalem_usunięty=" + plot + " trójkątów stare_wnętrze_usunięte=" + stare + " trójkątów (ściana z trawy: wzgórze omija tunel)");
    }

    // Szlaban: słupek z napędem (x 5.25, z 98.2), ramię 9.6 m do podpórki po lewej, budka obok portalu (x 5.65…7.45).
    static void Szlaban()
    {
        const float zS = 98.2f, xS = 5.25f, yR = 1.0f;
        var white = Plain("szlaban biały", new Color(.93f, .93f, .91f), .45f);
        var red = Plain("szlaban czerwony", new Color(.78f, .06f, .07f), .45f);
        var grey = Plain("szlaban szary napęd", new Color(.55f, .57f, .58f), .4f, .3f);
        // napęd (szafka) i podpórka ramienia
        BoxMinMax(grey, xS - .2f, xS + .2f, 0, 1.0f, zS - .2f, zS + .2f, true);
        BoxMinMax(red, xS - .21f, xS + .21f, .88f, 1.0f, zS - .21f, zS + .21f, false);
        BoxMinMax(white, -4.62f, -4.5f, 0, .9f, zS - .06f, zS + .06f, true);
        BoxMinMax(Steel, -4.7f, -4.42f, .9f, .96f, zS - .1f, zS + .1f, false);
        // budka szlabanowa: ściany białe, okna z trzech stron, płaski dach z okapem
        float bx0 = 5.65f, bx1 = 7.45f, bz0 = 97.6f, bz1 = 99.2f, bh = 2.45f;
        var tynk = Stucco;
        BoxMinMax(tynk, bx0, bx1, 0, .9f, bz0, bz1, true);
        BoxMinMax(tynk, bx0, bx0 + .12f, .9f, bh, bz0, bz1, false); BoxMinMax(tynk, bx1 - .12f, bx1, .9f, bh, bz0, bz1, false);
        BoxMinMax(tynk, bx0, bx1, .9f, bh, bz1 - .12f, bz1, false); BoxMinMax(tynk, bx0, bx1, 2.05f, bh, bz0, bz0 + .12f, false);
        BoxMinMax(GlassClear, bx0 + .12f, bx1 - .12f, .9f, 2.05f, bz0 + .03f, bz0 + .07f, false);             // okno od osiedla
        BoxMinMax(GlassClear, bx0 + .02f, bx0 + .06f, .9f, 2.05f, bz0 + .12f, bz1 - .12f, false);             // okno na jezdnię
        foreach (var (a, b) in new[] { (bx0 + .12f, bx1 - .12f) }) { BoxMinMax(Frame, a, b, 2.0f, 2.05f, bz0 + .02f, bz0 + .08f, false); BoxMinMax(Frame, a, b, .9f, .95f, bz0 + .02f, bz0 + .08f, false); }
        BoxMinMax(Frame, bx0 - .15f, bx1 + .15f, bh, bh + .14f, bz0 - .2f, bz1 + .05f, false, true);         // dach z okapem
        Collider(new Vector3((bx0 + bx1) * .5f, bh * .5f, (bz0 + bz1) * .5f), new Vector3(bx1 - bx0, bh, bz1 - bz0));
        // ramię na osi obrotu (osobny obiekt — podnosi się w wariancie z miastem)
        var os = new GameObject("szlaban: oś ramienia"); os.transform.SetParent(Group(group), false); os.transform.position = new Vector3(xS, yR, zS);
        var ramie = new GameObject("szlaban: ramię"); ramie.transform.SetParent(os.transform, false);
        var vb = new List<Vector3>(); var nb = new List<Vector3>(); var ub = new List<Vector2>(); var tw = new List<int>(); var tr = new List<int>();
        void Prost(List<int> idx, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            Vector3[] P = { new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x0, y1, z0), new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1) };
            int[][] f = { new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 3, 7, 6, 2 }, new[] { 0, 1, 5, 4 }, new[] { 0, 4, 7, 3 }, new[] { 1, 2, 6, 5 } };
            Vector3[] N = { Vector3.back, Vector3.forward, Vector3.up, Vector3.down, Vector3.left, Vector3.right };
            for (int q = 0; q < 6; q++)
            {
                int k = vb.Count;
                foreach (int c in f[q]) { vb.Add(P[c]); nb.Add(N[q]); ub.Add(Vector2.zero); }
                idx.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
        }
        // przeciwwaga za osią, ramię 9.75 m w stronę −x w pasach 0.5 m (biały/czerwony)
        Prost(tw, 0f, .55f, -.07f, .07f, -.06f, .06f);
        int pas = 0;
        for (float x = -.05f; x > -9.8f; x -= .5f, pas++) Prost(pas % 2 == 0 ? tr : tw, Mathf.Max(-9.8f, x - .5f), x, -.045f, .045f, -.035f, .035f);
        var mesh = new Mesh { name = "Szlaban ramię" }; mesh.subMeshCount = 2;
        mesh.SetVertices(vb); mesh.SetNormals(nb); mesh.SetUVs(0, ub); mesh.SetTriangles(tw, 0); mesh.SetTriangles(tr, 1); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Out + "/szlaban_ramie.asset");
        ramie.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = ramie.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { white, red }; mr.shadowCastingMode = ShadowCastingMode.On;
        // tablica „MIASTO · WKRÓTCE” pod środkiem ramienia (z obu stron), obraca się z ramieniem
        var tab = new GameObject("szlaban: tablica MIASTO WKRÓTCE"); tab.transform.SetParent(ramie.transform, false); tab.transform.localPosition = new Vector3(-4.9f, -.32f, 0);
        var tm = SignMat("szlaban_wkrotce");
        var tv = new List<Vector3>(); var tn = new List<Vector3>(); var tu = new List<Vector2>(); var ti = new List<int>();
        foreach (float sz in new[] { -1f, 1f })
        {
            int k = tv.Count; float z = sz * .012f;
            var P = new[] { new Vector3(.62f, -.23f, z), new Vector3(-.62f, -.23f, z), new Vector3(-.62f, .23f, z), new Vector3(.62f, .23f, z) };
            if (sz > 0) P = new[] { new Vector3(-.62f, -.23f, z), new Vector3(.62f, -.23f, z), new Vector3(.62f, .23f, z), new Vector3(-.62f, .23f, z) };
            tv.AddRange(P); tn.AddRange(Enumerable.Repeat(new Vector3(0, 0, sz), 4)); tu.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            if (sz < 0) ti.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 }); else ti.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }
        var tmesh = new Mesh { name = "Szlaban tablica" }; tmesh.SetVertices(tv); tmesh.SetNormals(tn); tmesh.SetUVs(0, tu); tmesh.SetTriangles(ti, 0); tmesh.RecalculateBounds();
        AssetDatabase.CreateAsset(tmesh, Out + "/szlaban_tablica.asset");
        tab.AddComponent<MeshFilter>().sharedMesh = tmesh; tab.AddComponent<MeshRenderer>().sharedMaterial = tm;
        // blokada: cały wjazd między obramieniami portalu (gracz nie przejdzie bokiem ramienia)
        var blok = new GameObject("szlaban: blokada"); blok.transform.SetParent(colliders, false); blok.transform.position = new Vector3(0, 1.5f, 98.65f);
        var bc = blok.AddComponent<BoxCollider>(); bc.size = new Vector3(10.8f, 3f, 1.4f);
        var sz2 = os.AddComponent<BraxSzlaban>(); sz2.ramie = ramie.transform; sz2.blokada = bc; sz2.tablica = tab; sz2.strefa = new Vector3(0, 0, 97f);
        Hard(-5.4f, 7.6f, 97.5f, 99.5f);
    }
}
