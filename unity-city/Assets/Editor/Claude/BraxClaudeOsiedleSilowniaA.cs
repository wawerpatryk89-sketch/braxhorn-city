using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// SIŁOWNIA A — wyposażenie po filmach właściciela 1.11 (17:52–18:18): worki premium na łańcuchach, które się bujają po ciosie,
// grube białe liny ringu reagujące na Braxa, strefa siłowa pod muralem (klatka ze sztangą, ławki, hantle z lustrem, talerze),
// stojak na piłki zamiast rozrzuconych kul (3 piłki z fizyką na podłodze), prawdziwe ławki zamiast czarnych klocków,
// regał z kettlami zamiast „przegródek”, szyld BRAXHORN GYM przed pilastrami, ostre logo byka, okna i lampy bez świecenia,
// przybudówka z szatnią, prysznicami i WC (wejście z hali), lampy wnętrza ForceVertex, zasięg kamery w zamkniętej sali.
// Układ modelu hali: świat = (gx + x_modelu, y, gz + z_modelu) — jak w GymA (x_modelu Unity = −x z pliku three.js).
public static partial class BraxClaudeOsiedle
{
    // przejście z hali do przybudówki: ściana wschodnia modelu (x_modelu 18.0…18.6), z_modelu AnexZ0…AnexZ1, do AnexH
    const float AnexZ0 = 5.4f, AnexZ1 = 6.6f, AnexH = 2.75f;
    // grupy wnętrz: siatki bez rzucania cieni (słońce i tak nie wpada pod dach, a mapa cieni to koszt na telefonie)
    static readonly HashSet<string> bezCieni = new HashSet<string>();

    // Kolizje z gym-collisions.json, które zastępuje nowe wyposażenie (x, z w układzie modelu Unity).
    static bool GymColliderOut(string tag, float ux0, float ux1, float uz0, float uz1, float top)
    {
        bool In(float a0, float a1, float b0, float b1) => ux0 > a0 - .02f && ux1 < a1 + .02f && uz0 > b0 - .02f && uz1 < b1 + .02f;
        if (tag == "rope") return true;                                           // liny ringu → BraxLina (kolizja za liną)
        if (In(5.6f, 7.8f, 2.15f, 4.0f) && top < 1f) return true;                 // czarne klocki → ławki
        if (In(14.9f, 17.35f, -12.6f, -11.1f) && top < 2f) return true;           // żółte kartony → stojak na piłki
        if (In(16.75f, 17.35f, 5.15f, 9.65f) && top < .6f) return true;           // niskie czarne ławy pod ścianą (przejście do szatni)
        if (In(17.95f, 21.05f, -16.05f, 14.05f) && top > 8f) return true;         // pustka między halą a obudową — z przerwą na przejście
        return false;
    }

    // ---------------- mała siatka z kodu (wiele materiałów, normalne liczone, kierunek trójkątów sam się ustawia) ----------------
    sealed class MB
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int>[] s;
        public MB(int subs) { s = new List<int>[subs]; for (int i = 0; i < subs; i++) s[i] = new List<int>(); }
        public int V(Vector3 p, Vector3 nn, Vector2 u) { v.Add(p); n.Add(nn.normalized); uv.Add(u); return v.Count - 1; }
        // czworokąt a-b-c-d (obwód); przód tam, gdzie wskazują normalne wierzchołków
        public void Q(int sub, int a, int b, int c, int d)
        {
            var nr = n[a] + n[b] + n[c] + n[d];
            if (Vector3.Dot(Vector3.Cross(v[c] - v[a], v[d] - v[b]), nr) >= 0) s[sub].AddRange(new[] { a, b, c, a, c, d });
            else s[sub].AddRange(new[] { a, c, b, a, d, c });
        }
        public void T(int sub, int a, int b, int c)
        {
            var nr = n[a] + n[b] + n[c];
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), nr) >= 0) s[sub].AddRange(new[] { a, b, c }); else s[sub].AddRange(new[] { a, c, b });
        }
        // bryła obrotowa: profil (r, y) od góry do dołu, oś wzdłuż `os` (domyślnie pion), UV w metrach / sk
        public void Lathe(int sub, Vector3 c, Vector2[] prof, int seg, float sk, Quaternion obr)
        {
            var acc = new float[prof.Length];
            for (int i = 1; i < prof.Length; i++) acc[i] = acc[i - 1] + Vector2.Distance(prof[i], prof[i - 1]);
            int k0 = v.Count;
            for (int i = 0; i < prof.Length; i++)
            {
                Vector2 t = (i == 0 ? prof[1] - prof[0] : i == prof.Length - 1 ? prof[i] - prof[i - 1] : prof[i + 1] - prof[i - 1]).normalized;
                var pn = new Vector2(-t.y, t.x);
                for (int q = 0; q <= seg; q++)
                {
                    float a = q * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    V(c + obr * (d * prof[i].x + Vector3.up * prof[i].y), obr * (d * pn.x + Vector3.up * pn.y), new Vector2(a * Mathf.Max(.02f, prof[i].x) / sk, acc[i] / sk));
                }
            }
            for (int i = 0; i + 1 < prof.Length; i++)
                for (int q = 0; q < seg; q++)
                {
                    int p0 = k0 + i * (seg + 1) + q, p1 = p0 + seg + 1;
                    if (Vector3.Distance(v[p0], v[p0 + 1]) < 1e-5f && Vector3.Distance(v[p1], v[p1 + 1]) < 1e-5f) continue;
                    Q(sub, p0, p0 + 1, p1 + 1, p1);
                }
        }
        public void Kula(int sub, Vector3 c, float r, int seg, float sk)
        {
            var prof = new Vector2[9];
            for (int i = 0; i <= 8; i++) { float a = Mathf.PI * .5f - Mathf.PI * i / 8f; prof[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
            Lathe(sub, c, prof, seg, sk, Quaternion.identity);
        }
        // walec od a do b (z denkami)
        public void Walec(int sub, Vector3 a, Vector3 b, float r, int seg, float sk = 1f)
        {
            var os = b - a; float l = os.magnitude; if (l < 1e-4f) return;
            var obr = Quaternion.FromToRotation(Vector3.down, os / l);
            Lathe(sub, a, new[] { new Vector2(0, 0), new Vector2(r, 0), new Vector2(r, -l), new Vector2(0, -l) }, seg, sk, obr);
        }
        public void Pud(int sub, Vector3 c, Vector3 size, Quaternion obr)
        {
            var h = size * .5f;
            var sc = new[] { Vector3.up, Vector3.right, Vector3.forward, Vector3.down, Vector3.right, Vector3.forward, Vector3.right, Vector3.up, Vector3.forward,
                             Vector3.left, Vector3.up, Vector3.forward, Vector3.forward, Vector3.right, Vector3.up, Vector3.back, Vector3.right, Vector3.up };
            for (int f = 0; f < 6; f++)
            {
                Vector3 nn = sc[f * 3], ua = sc[f * 3 + 1], ub = sc[f * 3 + 2];
                var cc = Vector3.Scale(nn, h); var ea = Vector3.Scale(ua, h); var eb = Vector3.Scale(ub, h); var wn = obr * nn;
                int a = V(c + obr * (cc - ea - eb), wn, new Vector2(0, 0)), b = V(c + obr * (cc + ea - eb), wn, new Vector2(1, 0));
                int d2 = V(c + obr * (cc + ea + eb), wn, new Vector2(1, 1)), e = V(c + obr * (cc - ea + eb), wn, new Vector2(0, 1));
                Q(sub, a, b, d2, e);
            }
        }
        // ogniwo łańcucha: wydłużony torus w płaszczyźnie (os, pl), długość wewn. dl, szerokość sz, drut r
        public void Ogniwo(int sub, Vector3 c, Vector3 os, Vector3 pl, float dl, float sz, float r)
        {
            const int U = 10, W = 5; var pn = Vector3.Cross(os, pl).normalized; int k0 = v.Count;
            for (int i = 0; i <= U; i++)
            {
                float u = i * Mathf.PI * 2f / U;
                var P = c + os * (Mathf.Cos(u) * dl * .5f) + pl * (Mathf.Sin(u) * sz * .5f);
                var Tg = (-os * (Mathf.Sin(u) * dl * .5f) + pl * (Mathf.Cos(u) * sz * .5f)).normalized;
                var Nin = Vector3.Cross(Tg, pn).normalized; if (Vector3.Dot(Nin, P - c) < 0) Nin = -Nin;
                for (int j = 0; j <= W; j++) { float w = j * Mathf.PI * 2f / W; var d = Nin * Mathf.Cos(w) + pn * Mathf.Sin(w); V(P + d * r, d, new Vector2(i, j)); }
            }
            for (int i = 0; i < U; i++) for (int j = 0; j < W; j++) { int p0 = k0 + i * (W + 1) + j, p1 = p0 + W + 1; Q(sub, p0, p0 + 1, p1 + 1, p1); }
        }
        public Mesh Siatka(string name)
        {
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.subMeshCount = s.Length;
            for (int i = 0; i < s.Length; i++) m.SetTriangles(s[i], i);
            m.RecalculateBounds(); m.RecalculateTangents(); return m;
        }
    }
    static int mbN;
    static GameObject Wstaw(MB mb, string name, Material[] ms, Transform parent, Vector3 pos)
    {
        var mesh = mb.Siatka(name); AssetDatabase.CreateAsset(mesh, Out + "/mb_" + (mbN++) + ".asset");
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = pos;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = ms; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        rep.meshes++; rep.triangles += mb.s.Sum(l => l.Count) / 3;
        return g;
    }
    // skóra z połyskiem i ziarnem (mapa normalnych leather_red_02, Poly Haven CC0), kolor z kodu
    static Material Skora(string key, Color c, float gloss, float bump = .7f)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, color = c, enableInstancing = true };
        m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", 0f);
        string nm = Tex + "leather_red_02/normal.jpg";
        if (File.Exists(nm)) { m.SetTexture("_BumpMap", Map(nm, true, false)); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", bump); m.mainTextureScale = new Vector2(1, 1); }
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = 1f; return m;
    }
    static Material Chrom => Plain("chrom (okucia, gryfy)", new Color(.86f, .87f, .89f), .9f, 1f);
    static Material GumaCzarna => Plain("guma talerzy i hantli", new Color(.045f, .045f, .05f), .32f);
    static Material Akcent => Plain("zieleń BRAXHORN (farba)", new Color(.20f, .62f, .06f), .45f);
    static Material Tapicerka => Skora("tapicerka ławek (czarna skóra)", new Color(.035f, .035f, .04f), .5f, .5f);

    // Krążek (talerz, głowica hantla): oś `os`, z denkami; seg 6 = hantel sześciokątny.
    static void Krazek(Material m, Vector3 c, Vector3 os, float r, float gr, int seg = 24)
    {
        os.Normalize(); var a = c - os * gr * .5f; var b = c + os * gr * .5f;
        var u = Vector3.Cross(os, Mathf.Abs(os.y) > .9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(os, u);
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
            Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
            Quad(m, a + d0 * r, a + d1 * r, b + d1 * r, b + d0 * r, ((d0 + d1) * .5f).normalized);
            Tri(m, a, a + d0 * r, a + d1 * r, -os); Tri(m, b, b + d1 * r, b + d0 * r, os);
        }
    }

    // ================= wywołanie z GymA (po recepcji) =================
    static void GymAWyposazenie(Transform g, float gx, float gz)
    {
        Guard("siłownia A: stare zastępcze rzeczy (kapsuły, klocki, kartony, kule, przegródki, cienkie liny)", () => GymAPorzadki(g, gx, gz));
        Guard("siłownia A: worki premium na łańcuchach", () => GymAWorki(g, gx, gz));
        Guard("siłownia A: grube białe liny ringu", () => GymALiny(gx, gz));
        Guard("siłownia A: strefa siłowa pod muralem", () => GymAStrefaSilowa(gx, gz));
        Guard("siłownia A: ławki, stojak na piłki, kettle, piłki z fizyką", () => GymAStojaki(gx, gz));
        Guard("siłownia A: szyld, byk, okna, lampy", () => GymASzyldy(g, gx, gz));
        Guard("siłownia A: przybudówka — szatnia, prysznice, WC", () => GymASzatnia(gx, gz));
        Guard("siłownia A: światła wnętrza, zasięg kamery, podłoga z kolizją", () => GymAWydajnosc(gx, gz));
    }

    static void GymAPorzadki(Transform g, float gx, float gz)
    {
        var drop = new HashSet<int>();
        for (int i = 81; i <= 112; i++) drop.Add(i);        // kapsuły worków z cienkimi prętami
        for (int i = 127; i <= 132; i++) drop.Add(i);       // „przegródki” we wnękach (białe/zielone/czarne walce)
        foreach (int i in new[] { 136, 137, 143, 145, 157, 158, 159, 160 }) drop.Add(i);   // cienkie liny, czarne klocki, kartony, kule
        int gone = 0;
        foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true).ToList())
        {
            if (!r || !r.name.StartsWith("Gym_Mesh_") || !int.TryParse(r.name.Substring(9), out var id) || !drop.Contains(id)) continue;
            UnityEngine.Object.DestroyImmediate(r.gameObject); gone++;
        }
        Bounds M(float x0, float x1, float y0, float y1, float z0, float z1) => MinMax(gx + x0, gx + x1, y0, y1, gz + z0, gz + z1);
        CutVolumes(g, null, new List<Bounds> {
            M(5.6f, 7.8f, -.01f, .85f, 2.15f, 4.0f),          // zielone krawędzie czarnych klocków
            M(16.7f, 17.4f, -.01f, .6f, 5.1f, 9.7f),          // niskie czarne ławy pod ścianą wschodnią
            M(17.4f, 18.7f, -.01f, 3.0f, 5.0f, 7.0f) });      // otwór w ścianie wschodniej: przejście do szatni
        rep.notes.Add("siłownia A: usunięte zastępcze siatki: " + gone + " (kapsuły worków, klocki, kartony, kule, przegródki, stare liny)");
    }

    // ---------------- worki ----------------
    // Ten sam układ co w oryginale: 4 worki pod belką ramy (x_modelu 14, z −8.4 / −5.4 / −2.4 / 0.6). Spód belki y 4.44.
    // Na belce obejma stalowa z hakiem, krętlik, łańcuch (ogniwa 7.5 cm, chrom), kółko rozdzielające, 4 krótkie łańcuchy
    // do D-ringów na pasach worka. Worek 0.56 × 1.65 m (dół 1.0 m nad podłogą): czarna skóra z połyskiem i ziarnem,
    // wzmocnienia z ciemniejszej skóry na górze i dole, 6 przeszyć pionowych, zielony pas BRAXHORN, chromowane okucia.
    static void GymAWorki(Transform g, float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — worki";
        var skora = Skora("worek: skóra czarna z połyskiem", new Color(.085f, .088f, .095f), .66f, .75f);
        var wzm = Skora("worek: wzmocnienia skórzane", new Color(.045f, .046f, .05f), .5f, .9f);
        var szew = Plain("worek: przeszycia", new Color(.55f, .57f, .52f), .2f);
        const float Y = 4.44f, gora = WorekGora, dol = WorekDol, R = WorekR;
        var mb = new MB(5);   // 0 skóra, 1 wzmocnienia, 2 przeszycia, 3 chrom, 4 zieleń
        // obejma na belce (zaciski po obu stronach środnika) + hak
        mb.Pud(3, new Vector3(0, .16f, 0), new Vector3(.3f, .025f, .14f), Quaternion.identity);
        foreach (float sx in new[] { -.14f, .14f }) mb.Pud(3, new Vector3(sx, .08f, 0), new Vector3(.02f, .16f, .12f), Quaternion.identity);
        mb.Pud(3, new Vector3(0, -.012f, 0), new Vector3(.3f, .025f, .14f), Quaternion.identity);
        mb.Walec(3, new Vector3(0, -.02f, 0), new Vector3(0, -.09f, 0), .016f, 10);
        mb.Ogniwo(3, new Vector3(0, -.12f, 0), Vector3.up, Vector3.right, .05f, .05f, .008f);
        mb.Walec(3, new Vector3(0, -.15f, 0), new Vector3(0, -.22f, 0), .02f, 10);   // krętlik
        // łańcuch główny do kółka rozdzielającego (y −1.42)
        float yk = -1.42f, y = -.25f; int k = 0;
        while (y - .075f > yk + .03f) { var pl = (k++ % 2 == 0) ? Vector3.right : Vector3.forward; mb.Ogniwo(3, new Vector3(0, y - .0375f, 0), Vector3.up, pl, .06f, .035f, .0065f); y -= .058f; }
        mb.Ogniwo(3, new Vector3(0, yk, 0), Vector3.right, Vector3.forward, .09f, .09f, .009f);   // kółko (poziome)
        // 4 krótkie łańcuchy do D-ringów na pasach (pod kątem 45°)
        for (int q = 0; q < 4; q++)
        {
            float a = (45f + 90f * q) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var p0 = new Vector3(0, yk, 0) + d * .04f; var p1 = d * (R - .05f) + Vector3.up * (-gora + .02f);
            var os = (p1 - p0); float l = os.magnitude; os /= l; var pl2 = Vector3.Cross(os, Vector3.up).normalized;
            int nl = Mathf.Max(3, Mathf.RoundToInt(l / .05f));
            for (int j = 0; j < nl; j++) mb.Ogniwo(3, p0 + os * (l * (j + .5f) / nl), os, j % 2 == 0 ? pl2 : Vector3.Cross(os, pl2).normalized, .045f, .028f, .005f);
            mb.Ogniwo(3, p1, Vector3.up, pl2, .04f, .04f, .006f);   // D-ring
            mb.Pud(1, d * (R + .004f) + Vector3.up * (-gora - .09f), new Vector3(.04f, .2f, .006f), Quaternion.LookRotation(d));   // pas
        }
        // korpus: górne „ramię” zaokrąglone, walec, zaokrąglony spód
        mb.Lathe(1, Vector3.zero, new[] { new Vector2(0, -gora + .005f), new Vector2(.17f, -gora + .005f), new Vector2(.24f, -gora - .012f), new Vector2(.272f, -gora - .045f), new Vector2(R, -gora - .085f) }, 28, .35f, Quaternion.identity);
        mb.Lathe(0, Vector3.zero, new[] { new Vector2(R, -gora - .085f), new Vector2(R, -dol + .08f) }, 28, .35f, Quaternion.identity);
        mb.Lathe(1, Vector3.zero, new[] { new Vector2(R, -dol + .08f), new Vector2(.272f, -dol + .035f), new Vector2(.24f, -dol + .01f), new Vector2(.15f, -dol), new Vector2(0, -dol) }, 28, .35f, Quaternion.identity);
        // wzmocnienia (pierścienie skóry na górze i dole), zielony pas, przeszycia
        foreach (var (y0, y1, sub) in new[] { (-gora - .085f, -gora - .2f, 1), (-dol + .2f, -dol + .08f, 1), (-gora - .62f, -gora - .7f, 4) })
            mb.Lathe(sub, Vector3.zero, new[] { new Vector2(R + .0035f, y0), new Vector2(R + .0035f, y1) }, 28, .35f, Quaternion.identity);
        foreach (float ys in new[] { -gora - .2f, -dol + .2f, -gora - .62f, -gora - .7f })
            mb.Lathe(2, Vector3.zero, new[] { new Vector2(R + .0055f, ys + .003f), new Vector2(R + .0055f, ys - .003f) }, 28, .35f, Quaternion.identity);
        for (int q = 0; q < 6; q++)
        {
            float a = q * 60f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            mb.Pud(2, d * (R + .002f) + Vector3.up * ((-gora - .2f - dol + .2f) * .5f), new Vector3(.005f, dol - gora - .4f, .006f), Quaternion.LookRotation(d));
        }
        var mesh = mb.Siatka("worek premium (łańcuch, okucia, skóra)"); AssetDatabase.CreateAsset(mesh, Out + "/worek_premium.asset");
        var ms = new[] { skora, wzm, szew, Chrom, Akcent };
        worekMesh = mesh; worekMats = ms;
        foreach (float z in new[] { -8.4f, -5.4f, -2.4f, .6f }) Worek(new Vector3(gx + 14f, Y, gz + z));
        rep.notes.Add("siłownia A: 4 worki premium (" + mesh.vertexCount + " wierzchołków, skóra z ziarnem i połyskiem, łańcuch " + k + " ogniw, chrom) — BraxWorek: bujanie po ciosie/kopnięciu i od ciała");
    }

    // Worek premium pod hakiem w punkcie `piv` (spód belki): siatka i materiały z GymAWorki (wspólne dla wszystkich worków osiedla).
    static Mesh worekMesh; static Material[] worekMats;
    const float WorekGora = 1.8f, WorekDol = 3.45f, WorekR = .28f;
    static GameObject Worek(Vector3 piv)
    {
        if (!worekMesh) return null;
        var p = new GameObject("Worek premium (punkt zawieszenia)"); p.transform.SetParent(Group(group), false); p.transform.position = piv;
        var vis = new GameObject("worek, łańcuch, okucia"); vis.transform.SetParent(p.transform, false);
        vis.AddComponent<MeshFilter>().sharedMesh = worekMesh;
        var mr = vis.AddComponent<MeshRenderer>(); mr.sharedMaterials = worekMats; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
        var col = new GameObject("kolizja worka"); col.transform.SetParent(p.transform, false); col.transform.localPosition = Vector3.down * ((WorekGora + WorekDol) * .5f);
        var cc = col.AddComponent<CapsuleCollider>(); cc.direction = 1; cc.radius = WorekR; cc.height = WorekDol - WorekGora;
        var rb = p.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var w = p.AddComponent<BraxWorek>(); w.gora = WorekGora; w.dol = WorekDol; w.promien = WorekR; w.dlugosc = (WorekGora + WorekDol) * .5f;
        GameObjectUtility.SetStaticEditorFlags(p, 0);
        rep.meshes++; rep.triangles += worekMesh.triangles.Length / 3;
        return p;
    }

    // ---------------- liny ringu ----------------
    // Ring: platforma x_modelu −13.2…−4.8, z −9.7…−1.3, wierzch 0.95; słupki w narożnikach (−12.5 / −5.5, −9 / −2), wejście
    // od strony z −2 między słupkami −9.85 i −8.15 (zostaje — przejście na ring). 4 liny Ø 4.8 cm na 1.30 / 1.62 / 1.94 / 2.26 m,
    // białe z delikatnym połyskiem, 2 linki łączące na bok. Kolizja boku: pudło 0.18 m, lico 0.19 m za osią liny (lina ugina się
    // pod Braxem o maks. 0.2 m i zatrzymuje go — z ringu się nie wypada).
    static void GymALiny(float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — liny ringu";
        var biala = Plain("lina ringu biała (gruba, połysk)", new Color(.94f, .94f, .92f), .55f);
        Vector3 W(float x, float z) => new Vector3(gx + x, 0, gz + z);
        var boki = new[] {
            (W(-12.5f, -9f), W(-5.5f, -9f), Vector3.back, "S"), (W(-12.5f, -9f), W(-12.5f, -2f), Vector3.left, "W"),
            (W(-5.5f, -9f), W(-5.5f, -2f), Vector3.right, "E"), (W(-12.5f, -2f), W(-9.85f, -2f), Vector3.forward, "N1"), (W(-8.15f, -2f), W(-5.5f, -2f), Vector3.forward, "N2") };
        foreach (var (a0, b0, nz, id) in boki)
        {
            var os = (b0 - a0).normalized;
            // końce: w słupku narożnym (r 0.2) lina wchodzi 8 cm w obicie, przy słupku wejścia (r 0.1) — do lica
            bool aRog = id != "N2", bRog = id != "N1";
            var a = a0 + os * (aRog ? .12f : .08f); var b = b0 - os * (bRog ? .12f : .08f);
            var go = new GameObject("Liny ringu — bok " + id); go.transform.SetParent(Group(group), false); go.transform.position = Vector3.zero;
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = biala; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
            var l = go.AddComponent<BraxLina>(); l.a = a; l.b = b; l.naZewnatrz = nz; l.wysokosci = new[] { 1.30f, 1.62f, 1.94f, 2.26f }; l.promien = .024f; l.maksUgiecie = .2f;
            l.Zbuduj();
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            if (mesh) { AssetDatabase.CreateAsset(mesh, Out + "/liny_ringu_" + id + ".asset"); rep.meshes++; rep.triangles += mesh.triangles.Length / 3; }
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            // kolizja boku za liną (przejście wejścia na ring zostaje otwarte)
            var c = (a0 + b0) * .5f + nz * .28f; float len = Vector3.Distance(a0, b0);
            var col = new GameObject("kolizja lin " + id); col.transform.SetParent(colliders, false); col.transform.position = new Vector3(c.x, 1.78f, c.z);
            col.AddComponent<BoxCollider>().size = Mathf.Abs(nz.x) > .5f ? new Vector3(.18f, 1.64f, len) : new Vector3(len, 1.64f, .18f);
        }
        rep.notes.Add("siłownia A: ring — 4 grube białe liny Ø 4.8 cm z linkami łączącymi, reagują na Braxa (BraxLina), kolizja za liną; stare cienkie liny usunięte");
    }

    // ---------------- strefa siłowa pod muralem ----------------
    // Ściana z muralem: lico z_modelu −13.0, mural x −13.0…6.2 od 0.64 m. Przed nim tylko niskie rzeczy (do ~1.0 m): mata gumowa
    // strefy z zieloną obwódką, 2 ławki płaskie + 1 skośna (prostopadle do ściany), drzewko na talerze. Po lewej stronie muralu
    // (x 6.5…11, przed filarem): klatka do przysiadów ze sztangą i talerzami, stojak z 12 parami hantli (2.5–30 kg) pod lustrem.
    // Ring stoi 3.3 m przed ścianą (x −13.2…−4.8) — strefa od x −4.4, przejścia ≥ 1.2 m.
    static void GymAStrefaSilowa(float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — strefa siłowa"; bezCieni.Add(group);
        Vector3 W(float x, float y, float z) => new Vector3(gx + x, y, gz + z);
        var mata = Plain("mata gumowa siłowni", new Color(.05f, .05f, .055f), .15f);
        const float zW = -12.98f;
        Top(mata, gx - 4.4f, gx + 11.0f, gz + zW, gz - 8.3f, .012f);
        foreach (var (x0, x1, z0, z1) in new[] { (-4.4f, 11.0f, -8.38f, -8.3f), (-4.4f, -4.32f, zW, -8.3f), (10.92f, 11.0f, zW, -8.3f) })
            Top(Akcent, gx + x0, gx + x1, gz + z0, gz + z1, .016f);
        // KLATKA (power rack) x 6.65…7.75 (talerze na zewnątrz słupów), z −12.9…−11.5: 4 słupy 7.5 cm z otworami (chrom kołki), rama dołem, poprzeczki u góry,
        // drążek do podciągania, hak J, ramiona asekuracyjne, sztanga 2.2 m z talerzami 20 kg (Ø 45 cm) na 1.35 m
        float rx0 = gx + 6.65f, rx1 = gx + 7.75f, rz0 = gz - 12.88f, rz1 = gz - 11.5f, H = 2.3f;
        foreach (float x in new[] { rx0, rx1 }) foreach (float z in new[] { rz0, rz1 })
        {
            BoxMinMax(Steel, x - .0375f, x + .0375f, 0, H, z - .0375f, z + .0375f, false);
            for (float yh = .5f; yh < H - .1f; yh += .1f) BoxMinMax(Chrom, x - .01f, x + .01f, yh, yh + .02f, z + (z < (rz0 + rz1) * .5f ? .0375f : -.0385f), z + (z < (rz0 + rz1) * .5f ? .0385f : -.0375f), false);
        }
        foreach (float z in new[] { rz0, rz1 }) { BoxMinMax(Steel, rx0 - .04f, rx1 + .04f, 0, .06f, z - .04f, z + .04f, false); BoxMinMax(Steel, rx0, rx1, H - .07f, H, z - .0375f, z + .0375f, false); }
        foreach (float x in new[] { rx0, rx1 }) BoxMinMax(Steel, x - .04f, x + .04f, 0, .06f, rz0, rz1, false);
        Tube(Chrom, new Vector3(rx0, H - .14f, rz1 - .03f), new Vector3(rx1, H - .14f, rz1 - .03f), .016f);   // drążek
        foreach (float x in new[] { rx0, rx1 })
        {
            BoxMinMax(Chrom, x - .03f, x + .03f, 1.3f, 1.42f, rz1 + .035f, rz1 + .13f, false);                    // hak J
            BoxMinMax(Chrom, x - .025f, x + .025f, .72f, .78f, rz0 + .04f, rz1 + .45f, false);                     // ramię asekuracyjne
        }
        Collider(new Vector3((rx0 + rx1) * .5f, H * .5f, rz0 + .04f), new Vector3(rx1 - rx0 + .1f, H, .1f));
        foreach (float x in new[] { rx0, rx1 }) Collider(new Vector3(x, H * .5f, (rz0 + rz1) * .5f), new Vector3(.1f, H, rz1 - rz0 + .1f));
        float by = 1.42f + .03f, bz = rz1 + .08f, bcx = (rx0 + rx1) * .5f;
        // sztanga na hakach — osobny obiekt (chowany, gdy Brax robi przysiad: BraxCwiczenie trzyma swoją na barkach)
        var sb = new MB(2);
        sb.Walec(0, new Vector3(-1.1f, 0, 0), new Vector3(1.1f, 0, 0), .014f, 10);
        foreach (float sd in new[] { -1f, 1f })
        {
            sb.Walec(0, new Vector3(sd * .65f, 0, 0), new Vector3(sd * 1.1f, 0, 0), .025f, 10);
            sb.Walec(1, new Vector3(sd * .705f, 0, 0), new Vector3(sd * .755f, 0, 0), .225f, 24);
            sb.Walec(1, new Vector3(sd * .77f, 0, 0), new Vector3(sd * .81f, 0, 0), .2f, 24);
            sb.Walec(0, new Vector3(sd * .665f, 0, 0), new Vector3(sd * .7f, 0, 0), .035f, 12);   // zacisk
        }
        var sztanga = Wstaw(sb, "sztanga w klatce", new[] { Chrom, GumaCzarna }, Group(group), new Vector3(bcx, by, bz));
        sztanga.AddComponent<MeshCollider>().sharedMesh = sztanga.GetComponent<MeshFilter>().sharedMesh;
        // STANOWISKA ĆWICZEŃ (AKCJA: BraxCwiczenie przez rejestr BraxInterakcje): przysiad w klatce, wyciskanie na ławkach, uginanie przy hantlach
        void Stanowisko(Vector3 at, float yaw, BraxCwiczenie.Rodzaj r, GameObject ukryj)
        {
            var o = new GameObject("stanowisko ćwiczeń: " + r); o.transform.SetParent(Group(group), false);
            o.transform.position = at; o.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var c = o.AddComponent<BraxCwiczenie>(); c.rodzaj = r; c.wysokosc = .46f; c.ukryj = ukryj; c.chrom = Chrom; c.guma = GumaCzarna;
        }
        Stanowisko(new Vector3(bcx, 0, (rz0 + rz1) * .5f - .05f), 0f, BraxCwiczenie.Rodzaj.Przysiad, sztanga);
        Stanowisko(W(9.6f, 0, -11.55f), 180f, BraxCwiczenie.Rodzaj.Uginanie, null);
        foreach (float x in new[] { -2.6f, .4f }) Stanowisko(W(x, 0, -11.35f), 0f, BraxCwiczenie.Rodzaj.Wyciskanie, null);
        foreach (float x in new[] { 6.05f, 7.35f }) Stanowisko(W(x, 0, 3.1f), 0f, BraxCwiczenie.Rodzaj.Wyciskanie, null);
        // ŁAWKI: 2 płaskie (x −2.6, 0.4) + skośna (x 3.4), prostopadle do ściany, 1.25 m od ściany
        void Lawka(float x, bool skosna)
        {
            var c = W(x, 0, -11.35f);
            BoxMinMax(Steel, c.x - .04f, c.x + .04f, .1f, .36f, c.z - .55f, c.z + .55f, false);                     // belka
            foreach (float dz in new[] { -.55f, .55f }) { BoxMinMax(Steel, c.x - .28f, c.x + .28f, 0, .06f, c.z + dz - .04f, c.z + dz + .04f, false); BoxMinMax(Steel, c.x - .035f, c.x + .035f, .06f, .36f, c.z + dz - .035f, c.z + dz + .035f, false); }
            foreach (float dz in new[] { -.55f, .55f }) foreach (float dx in new[] { -.28f, .28f }) BoxMinMax(GumaCzarna, c.x + dx - .03f, c.x + dx + .03f, 0, .02f, c.z + dz - .05f, c.z + dz + .05f, false);
            if (!skosna) BoxMinMax(Tapicerka, c.x - .15f, c.x + .15f, .36f, .46f, c.z - .62f, c.z + .62f, false, true);
            else
            {
                BoxMinMax(Tapicerka, c.x - .15f, c.x + .15f, .36f, .46f, c.z + .05f, c.z + .5f, false, true);       // siedzisko (od strony sali)
                OBoxQ(Tapicerka, new Vector3(c.x, .74f, c.z - .3f), new Vector3(.3f, .1f, .8f), Quaternion.Euler(-35f, 0, 0));   // oparcie 35°
                Tube(Steel, new Vector3(c.x, .36f, c.z - .55f), new Vector3(c.x, .62f, c.z - .45f), .025f);
            }
            Collider(new Vector3(c.x, .25f, c.z), new Vector3(.6f, .5f, 1.25f));
        }
        Lawka(-2.6f, false); Lawka(.4f, false); Lawka(3.4f, true);
        // HANTLE pod lustrem (x 8.3…10.9): stojak dwupoziomowy, 12 par 2.5–30 kg (głowice sześciokątne, chrom uchwyt)
        float hx0 = gx + 8.3f, hx1 = gx + 10.9f, hz0 = gz + zW + .02f, hz1 = hz0 + .6f;
        foreach (float x in new[] { hx0 + .05f, hx1 - .05f }) { BoxMinMax(Steel, x - .04f, x + .04f, 0, .82f, hz0 + .05f, hz0 + .13f, false); BoxMinMax(Steel, x - .04f, x + .04f, 0, .05f, hz0, hz1, false); }
        float[] kg = { 2.5f, 5f, 7.5f, 10f, 12.5f, 15f, 17.5f, 20f, 22.5f, 25f, 27.5f, 30f };
        for (int tier = 0; tier < 2; tier++)
        {
            float ty = tier == 0 ? .78f : .42f, tz = tier == 0 ? hz0 + .22f : hz0 + .42f;
            BoxMinMax(Steel, hx0, hx1, ty - .03f, ty, tz - .16f, tz + .16f, false, true);
            for (int p = 0; p < 6; p++)
            {
                float m = kg[tier == 0 ? p : 6 + p], r = .05f + m * .0022f, cx = hx0 + .25f + p * (hx1 - hx0 - .5f) / 5f;
                Krazek(GumaCzarna, new Vector3(cx, ty + r, tz - .1f), Vector3.forward, r, .07f + m * .0012f, 6);
                Krazek(GumaCzarna, new Vector3(cx, ty + r, tz + .1f), Vector3.forward, r, .07f + m * .0012f, 6);
                Tube(Chrom, new Vector3(cx, ty + r, tz - .06f), new Vector3(cx, ty + r, tz + .06f), .016f, 8);
            }
        }
        Collider(new Vector3((hx0 + hx1) * .5f, .45f, (hz0 + hz1) * .5f), new Vector3(hx1 - hx0, .9f, hz1 - hz0));
        var lustro = Plain("lustro (srebrzone szkło)", new Color(.78f, .8f, .81f), .97f, 1f);
        BoxMinMax(lustro, hx0, hx1, .95f, 2.45f, gz + zW, gz + zW + .012f, false);
        BoxMinMax(Frame, hx0 - .03f, hx1 + .03f, 2.45f, 2.49f, gz + zW, gz + zW + .02f, false, true);
        BoxMinMax(Frame, hx0 - .03f, hx1 + .03f, .91f, .95f, gz + zW, gz + zW + .02f, false);
        // DRZEWKO NA TALERZE (x −3.7): podstawa, słup, 6 trzpieni z talerzami 5–25 kg
        var tc = W(-3.7f, 0, -12.35f);
        BoxMinMax(Steel, tc.x - .3f, tc.x + .3f, 0, .05f, tc.z - .3f, tc.z + .3f, false);
        BoxMinMax(Steel, tc.x - .035f, tc.x + .035f, .05f, 1.15f, tc.z - .035f, tc.z + .035f, false);
        foreach (var (yy, sd, r) in new[] { (.35f, -1f, .225f), (.35f, 1f, .225f), (.68f, -1f, .19f), (.68f, 1f, .19f), (.98f, -1f, .14f), (.98f, 1f, .14f) })
        {
            Tube(Chrom, new Vector3(tc.x, yy, tc.z), new Vector3(tc.x + sd * .2f, yy + .03f, tc.z), .02f, 8);
            Krazek(GumaCzarna, new Vector3(tc.x + sd * .1f, yy, tc.z), Vector3.right, r, .05f);
        }
        Collider(new Vector3(tc.x, .6f, tc.z), new Vector3(.6f, 1.2f, .6f));
        rep.notes.Add("siłownia A: strefa siłowa pod muralem — klatka ze sztangą i talerzami, 2 ławki płaskie + skośna, 12 par hantli 2.5–30 kg pod lustrem, drzewko z talerzami, mata z obwódką");
    }

    // ---------------- ławki zamiast klocków, stojak na piłki, kettle, piłki z fizyką, skrzynie plyo ----------------
    static void GymAStojaki(float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — stojaki"; bezCieni.Add(group);
        Vector3 W(float x, float y, float z) => new Vector3(gx + x, y, gz + z);
        // ławki treningowe w miejscu czarnych klocków (x 5.6…7.8, z 2.2…4.0): dwie płaskie obok siebie
        foreach (float x in new[] { 6.05f, 7.35f })
        {
            var c = W(x, 0, 3.1f);
            BoxMinMax(Steel, c.x - .04f, c.x + .04f, .1f, .36f, c.z - .55f, c.z + .55f, false);
            foreach (float dz in new[] { -.55f, .55f }) { BoxMinMax(Steel, c.x - .28f, c.x + .28f, 0, .06f, c.z + dz - .04f, c.z + dz + .04f, false); BoxMinMax(Steel, c.x - .035f, c.x + .035f, .06f, .36f, c.z + dz - .035f, c.z + dz + .035f, false); }
            BoxMinMax(Tapicerka, c.x - .15f, c.x + .15f, .36f, .46f, c.z - .62f, c.z + .62f, false, true);
            Collider(new Vector3(c.x, .25f, c.z), new Vector3(.6f, .5f, 1.25f));
        }
        // stojak na piłki lekarskie w narożniku (dawne kartony): x 15.0…17.2 przy ścianie z −13.0, 3 półki × 3 piłki
        var sklejka = Pbr("sklejka (skrzynie plyo)", "wood_floor", 1.2f, new Color(1f, .93f, .82f), .35f);
        var pilkaM = new[] { Plain("piłka lekarska czarna", new Color(.07f, .07f, .075f), .35f), Plain("piłka lekarska zielona", new Color(.12f, .42f, .08f), .35f), Plain("piłka lekarska szara", new Color(.32f, .33f, .35f), .35f) };
        float sx0 = gx + 15.0f, sx1 = gx + 17.2f, sz0 = gz - 12.95f, sz1 = gz - 12.3f;
        foreach (float x in new[] { sx0, sx1 }) BoxMinMax(Steel, x - .03f, x + .03f, 0, 1.45f, sz0 + .05f, sz1 - .05f, false);
        var mb = new MB(4);
        int[] ileR = { 3, 3, 3 }; float[] ty = { .12f, .6f, 1.08f }; float[] pr = { .17f, .15f, .13f };
        for (int t = 0; t < 3; t++)
        {
            BoxMinMax(Steel, sx0, sx1, ty[t] - .03f, ty[t], sz0 + .05f, sz1 - .05f, false, true);
            for (int i = 0; i < ileR[t]; i++)
            {
                var c = new Vector3(sx0 + .37f + i * .73f, ty[t] + pr[t], (sz0 + sz1) * .5f);
                mb.Kula((i + t) % 3, c - W(0, 0, 0), pr[t], 20, .3f);
                mb.Lathe(3, c - W(0, 0, 0), new[] { new Vector2(pr[t] + .002f, .012f), new Vector2(pr[t] + .002f, -.012f) }, 20, .3f, Quaternion.Euler(0, 0, 90f));   // szew piłki
            }
        }
        Wstaw(mb, "piłki lekarskie na stojaku", new[] { pilkaM[0], pilkaM[1], pilkaM[2], GumaCzarna }, Group(group), W(0, 0, 0));
        Collider(new Vector3((sx0 + sx1) * .5f, .75f, (sz0 + sz1) * .5f), new Vector3(sx1 - sx0 + .06f, 1.5f, sz1 - sz0));
        // 3 piłki luzem z fizyką (Brax je popycha i kopie) — z dala od przejść
        var kulaSiatka = new MB(2); kulaSiatka.Kula(0, Vector3.zero, .17f, 20, .3f);
        kulaSiatka.Lathe(1, Vector3.zero, new[] { new Vector2(.172f, .012f), new Vector2(.172f, -.012f) }, 20, .3f, Quaternion.Euler(0, 0, 90f));
        var kulaMesh = kulaSiatka.Siatka("piłka lekarska (fizyka)"); AssetDatabase.CreateAsset(kulaMesh, Out + "/pilka_lekarska.asset");
        int pilki = 0;
        foreach (var (x, z, m) in new[] { (10.2f, -4.2f, 0), (9.3f, -2.9f, 1), (2.6f, -1.2f, 2) })
        {
            var go = new GameObject("piłka lekarska (fizyka)"); go.transform.SetParent(Group(group), false); go.transform.position = W(x, .17f, z);
            go.AddComponent<MeshFilter>().sharedMesh = kulaMesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterials = new[] { pilkaM[m], GumaCzarna }; r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<SphereCollider>().radius = .17f;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 5f; rb.linearDamping = .6f; rb.angularDamping = 1.6f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<BraxPilka>().promien = .17f;
            go.AddComponent<BraxOdbicie>().rodzaj = 2;   // głuchy łomot o podłogę (1.11)
            GameObjectUtility.SetStaticEditorFlags(go, 0); pilki++;
        }
        // kettle we wnęce przy workach (x 11.18…15.82, z −16.0…−15.2): regał dwupoziomowy, 8 kettli 8–32 kg
        float kx0 = gx + 11.5f, kx1 = gx + 15.5f, kz0 = gz - 15.95f, kz1 = gz - 15.3f;
        foreach (float x in new[] { kx0, kx1 }) BoxMinMax(Steel, x - .03f, x + .03f, 0, .95f, kz0, kz1, false);
        var kb = new MB(2);
        for (int tier = 0; tier < 2; tier++)
        {
            float yy = tier == 0 ? .1f : .55f;
            BoxMinMax(Steel, kx0, kx1, yy - .03f, yy, kz0, kz1, false, true);
            for (int i = 0; i < 4; i++)
            {
                float rr = .1f + (tier * 4 + i) * .007f; var c = new Vector3(kx0 + .45f + i * (kx1 - kx0 - .9f) / 3f, yy + rr * .92f, (kz0 + kz1) * .5f) - W(0, 0, 0);
                kb.Kula(0, c, rr, 16, .3f);
                kb.Pud(0, c + Vector3.up * (rr * .9f), new Vector3(rr * 1.1f, rr * .35f, rr * .5f), Quaternion.identity);   // podstawa uchwytu
                kb.Ogniwo(1, c + Vector3.up * (rr * 1.3f), Vector3.right, Vector3.up, rr * 1.3f, rr * .9f, .016f);          // uchwyt
            }
        }
        Wstaw(kb, "kettle na regale", new[] { GumaCzarna, Steel }, Group(group), W(0, 0, 0));
        Collider(new Vector3((kx0 + kx1) * .5f, .475f, (kz0 + kz1) * .5f), new Vector3(kx1 - kx0 + .06f, .95f, kz1 - kz0));
        // skrzynie plyo (sklejka) pod ścianą wschodnią, poza strefą przed drzwiami szatni
        foreach (var (z, h) in new[] { (8.35f, .75f), (9.05f, .6f) })
        {
            BoxMinMax(sklejka, gx + 16.75f, gx + 17.4f, 0, h, gz + z - .3f, gz + z + .3f, true);
            BoxMinMax(Steel, gx + 16.74f, gx + 16.75f, h * .55f, h * .55f + .06f, gz + z - .1f, gz + z + .1f, false);   // otwór uchwytu (ciemny)
        }
        rep.notes.Add("siłownia A: 2 ławki zamiast czarnych klocków, stojak z 9 piłkami (narożnik po kartonach), " + pilki + " piłki luzem z fizyką, 8 kettli we wnęce, 2 skrzynie plyo");
    }

    // ---------------- szyld, byk, okna, lampy ----------------
    static void GymASzyldy(Transform g, float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — szyldy";
        Transform Part(string n) => g.GetComponentsInChildren<Transform>(true).FirstOrDefault(q => q.name == n);
        // szyld BRAXHORN GYM (Gym_Mesh_15 na x_modelu 17.92) i jego czarna tablica (23) — przed lico pilastrów (17.5): litera H
        // była zasłonięta pilastrem przy z −3. Przesunięcie 0.52 m w stronę sali — tablica dotyka lica pilastra.
        int moved = 0;
        foreach (var n in new[] { "Gym_Mesh_15", "Gym_Mesh_23" }) { var t = Part(n); if (t) { t.position += Vector3.left * .52f; moved++; } }
        // byk: stary rozmyty kontur (Gym_Mesh_16, x 17.95, y 2.0…4.4, z 2.3…4.7) → ostre logo BRAXHORN (512 px, czarne tło)
        // na czarnej tablicy 1.7 × 1.7 m z zieloną ramką, 2 cm od ściany
        var old = Part("Gym_Mesh_16"); if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        float lx = gx + 17.98f, lz = gz + 3.5f, ly = 3.2f;
        BoxMinMax(Plain("tablica logo czarna mat", new Color(.02f, .02f, .022f), .3f), lx - .03f, lx, ly - .87f, ly + .87f, lz - .87f, lz + .87f, false);
        foreach (var (y0, y1, z0, z1) in new[] { (ly + .84f, ly + .87f, lz - .87f, lz + .87f), (ly - .87f, ly - .84f, lz - .87f, lz + .87f), (ly - .84f, ly + .84f, lz - .87f, lz - .84f), (ly - .84f, ly + .84f, lz + .84f, lz + .87f) })
            BoxMinMax(Akcent, lx - .035f, lx, y0, y1, z0, z1, false);
        Sign("byk_braxhorn", new Vector3(lx - .036f, ly, lz), 1.62f, 1.62f, Vector3.left, false);
        // okna (Gym_Mesh_22: płaskie niebieskie łuki, materiał bez światła — świeciły jak neon) → ciemne szkło z odbiciem
        var szklo = Plain("szkło okien hali (przyciemnione)", new Color(.12f, .15f, .18f), .92f, .25f);
        var mleczny = LampGlass; var bialy = Plain("listwa biała mat", new Color(.82f, .82f, .8f), .3f);
        int sw = 0;
        foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material nm = r.name == "Gym_Mesh_22" ? szklo : r.name == "Gym_Mesh_5" ? mleczny : (r.name == "Gym_Mesh_13" || r.name == "Gym_Mesh_21") ? bialy : null;
            if (!nm) continue;
            r.sharedMaterials = Enumerable.Repeat(nm, r.sharedMaterials.Length).ToArray(); sw++;
        }
        rep.notes.Add("siłownia A: szyld BRAXHORN GYM przed pilastrami (" + moved + "/2 części), nowe logo byka 1.7 m, okna jako szkło i lampy/listwy bez świecenia (" + sw + " siatek)");
    }

    // ---------------- przybudówka: szatnia, prysznice, WC ----------------
    // Wejście z hali: ściana wschodnia (x_modelu 18.0, świat x gx+18), z_modelu 5.4…6.6 (drzwi 1.2 × 2.75 m), korytarzyk przez
    // grubą ścianę hali i obudowę (x gx+18 … gx+21.4). Przybudówka x gx+21.4 … gx+26.4, z gz+(−1.25) … gz+11.75 (tył w linii
    // frontu hali), ściany z klinkieru jak hala, płaski dach, okna wysokie z mlecznego szkła. W środku (podłoga 0, sufit 3.0):
    // szatnia z szafkami (2 rzędy) i ławką, lustro; przegroda z −74.6 (świat) z drzwiami do pryszniców i do WC.
    static void GymASzatnia(float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — szatnia i WC"; bezCieni.Add(group);
        float hw = gx + 18.0f, ox = gx + 21.4f, ex = gx + 26.4f, et = .25f;      // lico ściany hali, lico obudowy, ściana wschodnia przybudówki
        float dz0 = gz + AnexZ0, dz1 = gz + AnexZ1, dzc = (dz0 + dz1) * .5f;
        float az0 = gz - 1.25f, az1 = gz + 11.75f;                                // przybudówka (zewnętrzne lica)
        float ix0 = ox, ix1 = ex - et, iz0 = az0 + et, iz1 = az1 - et, CY = 3.0f;
        var plytki = Pbr("płytki łazienki (gres 30 cm)", "large_grey_tiles", .6f, new Color(.96f, .96f, .98f), .6f);
        var posadzka = Pbr("posadzka szatni (gres)", "large_grey_tiles", 1.2f, new Color(.7f, .72f, .74f), .5f);
        var tynk = InteriorPaint; var sufit = Plain("sufit szatni", new Color(.92f, .92f, .9f), .1f);
        var porcelana = Plain("porcelana sanitarna", new Color(.95f, .95f, .94f), .85f);
        var drzwi = Plain("drzwi wewnętrzne grafit", new Color(.16f, .17f, .18f), .45f);
        var szafkaM = Plain("szafki szatni (grafit lakier)", new Color(.12f, .125f, .13f), .55f, .2f);
        var drewno = Pbr("ławka szatni (dąb)", "wood_floor", 1.2f, new Color(.95f, .9f, .85f), .5f);
        var lustro = Plain("lustro (srebrzone szkło)", new Color(.78f, .8f, .81f), .97f, 1f);
        var szron = Plain("szkło mleczne okien szatni", new Color(.74f, .79f, .82f), .85f, .1f);
        // korytarzyk przez ścianę: podłoga, ściany, sufit, ościeżnica i opaska od strony hali
        Top(posadzka, hw - .02f, ox, dz0, dz1, .01f);
        BoxMinMax(tynk, hw, ox, 0, AnexH + .1f, dz0 - .1f, dz0, true); BoxMinMax(tynk, hw, ox, 0, AnexH + .1f, dz1, dz1 + .1f, true);
        BoxMinMax(sufit, hw, ox, AnexH, AnexH + .1f, dz0 - .1f, dz1 + .1f, true, true);
        foreach (float z in new[] { dz0 - .07f, dz1 }) BoxMinMax(Frame, hw - .04f, hw + .1f, 0, AnexH + .07f, z, z + .07f, false);
        BoxMinMax(Frame, hw - .04f, hw + .1f, AnexH, AnexH + .07f, dz0 - .07f, dz1 + .07f, false, true);
        // opaska w wyciętym otworze ściany hali (z_modelu 5.0…7.0, do 3.0 m) — grafitowy tynk wokół drzwi
        var grafit = Plain("tynk grafit wokół przejścia", new Color(.17f, .175f, .185f), .2f);
        BoxMinMax(grafit, hw - .05f, hw + .62f, 0, 3.0f, gz + 4.95f, dz0 - .07f, false); BoxMinMax(grafit, hw - .05f, hw + .62f, 0, 3.0f, dz1 + .07f, gz + 7.05f, false);
        BoxMinMax(grafit, hw - .05f, hw + .62f, AnexH + .07f, 3.05f, dz0 - .07f, dz1 + .07f, false, true);
        Sign("szatnia", new Vector3(hw - .07f, 3.32f, dzc), 1.5f, .37f, Vector3.left);
        Sign("wc", new Vector3(hw - .07f, 3.32f, dzc + 1.25f), .6f, .37f, Vector3.left);
        // bryła przybudówki: ściany zewnętrzne z klinkieru (zachodnia = obudowa hali), dach, attyka
        BoxMinMax(Clinker, ix1, ex, 0, 3.6f, az0, az1, true);
        BoxMinMax(Clinker, ox, ix1, 0, 3.6f, az0, iz0, true); BoxMinMax(Clinker, ox, ix1, 0, 3.6f, iz1, az1, true);
        BoxMinMax(Concrete, ox, ex, 3.25f, 3.6f, az0, az1, false, true);
        BoxMinMax(Frame, ox, ex + .08f, 3.6f, 3.75f, az0 - .08f, az1 + .08f, false, true);
        BoxMinMax(Plinth, ex, ex + .04f, 0, .5f, az0, az1, false);
        // okna wysokie (mleczne szkło w ramie) na ścianie wschodniej i południowej, po obu stronach ściany
        foreach (var (z0, z1) in new[] { (iz1 - 5.6f, iz1 - .6f), (iz0 + .6f, iz0 + 2.0f), (iz0 + 2.6f, iz0 + 4.0f) })
            foreach (float x in new[] { ex + .005f, ix1 - .005f })
            {
                bool out_ = x > ix1;
                BoxMinMax(szron, out_ ? ex : ix1 - .012f, out_ ? ex + .012f : ix1, 2.2f, 2.8f, z0, z1, false);
                foreach (var (y0, y1) in new[] { (2.16f, 2.2f), (2.8f, 2.84f) }) BoxMinMax(Frame, out_ ? ex : ix1 - .02f, out_ ? ex + .02f : ix1, y0, y1, z0 - .04f, z1 + .04f, false, true);
                for (float z = z0; z <= z1 + .01f; z += (z1 - z0) / Mathf.Max(1, Mathf.Round((z1 - z0) / 1.2f))) BoxMinMax(Frame, out_ ? ex : ix1 - .02f, out_ ? ex + .02f : ix1, 2.2f, 2.8f, z - .02f, z + .02f, false);
            }
        // wnętrze: posadzka, tynk ścian (zachodnia — okładzina na klinkierze obudowy), sufit
        Top(posadzka, ix0, ix1, iz0, iz1, .012f);
        BoxMinMax(sufit, ix0, ix1, CY, CY + .03f, iz0, iz1, false, true);
        BoxMinMax(tynk, ix0, ix0 + .03f, 0, CY, iz0, dz0 - .1f, false); BoxMinMax(tynk, ix0, ix0 + .03f, 0, CY, dz1 + .1f, iz1, false);
        BoxMinMax(tynk, ix0, ix0 + .03f, AnexH + .1f, CY, dz0 - .1f, dz1 + .1f, false);
        BoxMinMax(tynk, ix1 - .03f, ix1, 0, CY, iz0, iz1, false);
        BoxMinMax(tynk, ix0, ix1, 0, CY, iz1 - .03f, iz1, false); BoxMinMax(tynk, ix0, ix1, 0, CY, iz0, iz0 + .03f, false);
        // przegroda szatnia | prysznice + WC (z pz) z dwojgiem drzwi 1.2 × 2.65 m; ściana między prysznicami a WC (x px)
        float pz = gz + 3.45f, px = gx + 23.85f, d1c = gx + 22.65f, d2c = gx + 24.9f;   // pz = z świata −74.6
        void Sciana(float x0, float x1, float z) => BoxMinMax(tynk, x0, x1, 0, CY, z - .05f, z + .05f, true);
        Sciana(ix0, d1c - .67f, pz); Sciana(d1c + .67f, d2c - .67f, pz); Sciana(d2c + .67f, ix1, pz);
        foreach (float dc in new[] { d1c, d2c })
        {
            BoxMinMax(tynk, dc - .6f, dc + .6f, 2.72f, CY, pz - .05f, pz + .05f, false, true);
            foreach (float sx in new[] { dc - .67f, dc + .6f }) BoxMinMax(Frame, sx, sx + .07f, 0, 2.72f, pz - .08f, pz + .08f, false);
            BoxMinMax(Frame, dc - .67f, dc + .67f, 2.65f, 2.72f, pz - .08f, pz + .08f, false, true);
        }
        BoxMinMax(tynk, px - .05f, px + .05f, 0, CY, iz0, pz - .05f, true);
        Sign("prysznice", new Vector3(d1c, 2.86f, pz + .06f), 1.1f, .26f, Vector3.forward);
        Sign("wc", new Vector3(d2c, 2.86f, pz + .06f), .5f, .26f, Vector3.forward);
        // SZATNIA: szafki (grafit, 0.42 m, 2.0 m, kratki, uchwyt chrom) wzdłuż ściany wschodniej i północnej, ławka dębowa, lustro
        int szafek = 0;
        void Szafka(Vector3 c, bool wzdluzZ)
        {
            Vector3 s = wzdluzZ ? new Vector3(.5f, 1.9f, .42f) : new Vector3(.42f, 1.9f, .5f);
            Box(szafkaM, c + Vector3.up * 1.05f, s - new Vector3(wzdluzZ ? 0 : .01f, 0, wzdluzZ ? .01f : 0), false);
            Box(Steel, c + Vector3.up * .05f, new Vector3(s.x, .1f, s.z), false);
            var front = wzdluzZ ? Vector3.left : Vector3.back; var fp = c + front * (.25f + .004f);
            for (int q = 0; q < 3; q++) Box(Steel, fp + Vector3.up * (1.72f + q * .05f), wzdluzZ ? new Vector3(.006f, .015f, .26f) : new Vector3(.26f, .015f, .006f), false);
            Box(Chrom, fp + Vector3.up * 1.1f + (wzdluzZ ? Vector3.forward : Vector3.right) * .14f, wzdluzZ ? new Vector3(.03f, .14f, .02f) : new Vector3(.02f, .14f, .03f), false);
            szafek++;
        }
        float lzA = pz + 2.55f;   // szafki wschodnie od 2.5 m przed drzwiami WC (strefa wolna za drzwiami)
        for (float z = lzA + .21f; z < iz1 - .55f; z += .42f) Szafka(new Vector3(ix1 - .28f, 0, z), true);
        Collider(new Vector3(ix1 - .28f, 1f, (lzA + iz1 - .5f) * .5f), new Vector3(.52f, 2f, iz1 - .5f - lzA));
        for (float x = ix0 + .25f; x < ix1 - .6f; x += .42f) Szafka(new Vector3(x, 0, iz1 - .28f), false);
        Collider(new Vector3((ix0 + ix1 - .6f) * .5f, 1f, iz1 - .28f), new Vector3(ix1 - .6f - ix0, 2f, .52f));
        float bx = ix1 - 2.0f, bz0 = pz + 2.7f, bz1 = iz1 - 1.0f;
        BoxMinMax(drewno, bx - .22f, bx + .22f, .4f, .45f, bz0, bz1, false, true);
        foreach (float z in new[] { bz0 + .2f, bz1 - .2f }) BoxMinMax(Steel, bx - .2f, bx + .2f, 0, .4f, z - .03f, z + .03f, false);
        Collider(new Vector3(bx, .23f, (bz0 + bz1) * .5f), new Vector3(.44f, .45f, bz1 - bz0));
        BoxMinMax(lustro, ix0 + .03f, ix0 + .042f, 1.0f, 2.2f, dz1 + .9f, dz1 + 3.1f, false);
        BoxMinMax(Frame, ix0 + .03f, ix0 + .05f, 2.2f, 2.24f, dz1 + .88f, dz1 + 3.12f, false, true);
        // PRYSZNICE (x ix0…px, z iz0…pz): płytki na ścianach do 2.2 m, 2 stanowiska z deszczownicą, szklana ścianka, odpływ
        Top(plytki, ix0, px - .05f, iz0, pz - .05f, .016f);
        BoxMinMax(plytki, ix0 + .03f, ix0 + .045f, 0, 2.2f, iz0, pz - .05f, false); BoxMinMax(plytki, px - .065f, px - .05f, 0, 2.2f, iz0, pz - .05f, false);
        BoxMinMax(plytki, ix0, px - .05f, 0, 2.2f, iz0 + .03f, iz0 + .045f, false);
        float sm = (ix0 + px) * .5f;
        BoxMinMax(GlassClear, sm - .01f, sm + .01f, .05f, 2.0f, iz0 + .05f, iz0 + 1.4f, false);
        Collider(new Vector3(sm, 1f, iz0 + .72f), new Vector3(.04f, 2f, 1.35f));
        foreach (float x in new[] { (ix0 + sm) * .5f, (sm + px) * .5f })
        {
            Tube(Chrom, new Vector3(x, 2.25f, iz0 + .05f), new Vector3(x, 2.25f, iz0 + .45f), .012f, 8);
            Krazek(Chrom, new Vector3(x, 2.22f, iz0 + .45f), Vector3.up, .13f, .02f, 20);
            BoxMinMax(Chrom, x - .1f, x + .1f, 1.05f, 1.15f, iz0 + .045f, iz0 + .08f, false);
            BoxMinMax(Steel, x - .07f, x + .07f, .016f, .02f, iz0 + .6f, iz0 + .74f, false);
        }
        // WC (x px…ix1, z iz0…pz): płytki, miska ustępowa przy ścianie południowej, umywalka z lustrem przy ścianie wschodniej
        Top(plytki, px + .05f, ix1, iz0, pz - .05f, .016f);
        BoxMinMax(plytki, px + .05f, px + .065f, 0, 1.6f, iz0, pz - .05f, false); BoxMinMax(plytki, ix1 - .045f, ix1 - .03f, 0, 1.6f, iz0, pz - .05f, false);
        BoxMinMax(plytki, px + .05f, ix1, 0, 1.6f, iz0 + .03f, iz0 + .045f, false);
        float tx = (px + ix1) * .5f;
        Cylinder(porcelana, new Vector3(tx, 0, iz0 + .42f), .2f, .38f, 20, true, false);
        BoxMinMax(porcelana, tx - .2f, tx + .2f, .38f, .41f, iz0 + .22f, iz0 + .64f, false, true);     // deska
        BoxMinMax(porcelana, tx - .26f, tx + .26f, .7f, 1.1f, iz0 + .045f, iz0 + .2f, false, true);   // stelaż z przyciskiem
        BoxMinMax(Chrom, tx - .08f, tx + .08f, .98f, 1.04f, iz0 + .2f, iz0 + .21f, false);
        Collider(new Vector3(tx, .4f, iz0 + .35f), new Vector3(.45f, .8f, .65f));
        float uz = pz - 1.0f;
        BoxMinMax(porcelana, ix1 - .48f, ix1 - .03f, .78f, .9f, uz - .28f, uz + .28f, true, true);
        BoxMinMax(Chrom, ix1 - .2f, ix1 - .17f, .9f, 1.12f, uz - .02f, uz + .02f, false);
        BoxMinMax(lustro, ix1 - .045f, ix1 - .033f, 1.2f, 1.9f, uz - .3f, uz + .3f, false);
        // lampy sufitowe (płaskie panele mleczne, nie świecą) + światła ForceVertex
        foreach (var p in new[] { new Vector2((ix0 + ix1) * .5f, (pz + iz1) * .5f - 1.6f), new Vector2((ix0 + ix1) * .5f, (pz + iz1) * .5f + 1.6f), new Vector2(sm, (iz0 + pz) * .5f), new Vector2(tx, (iz0 + pz) * .5f) })
        {
            BoxMinMax(LampGlass, p.x - .3f, p.x + .3f, CY - .03f, CY, p.y - .3f, p.y + .3f, false, true);
            PointLamp(new Vector3(p.x, CY - .4f, p.y), 6f, .9f, new Color(1f, .96f, .9f));
        }
        PointLamp(new Vector3((hw + ox) * .5f, AnexH - .3f, dzc), 4f, .6f, new Color(1f, .96f, .9f));
        Hard(ox, ex, az0, az1);
        rep.notes.Add("siłownia A: przybudówka " + (ex - ox).ToString("0.0") + " × " + (az1 - az0).ToString("0.0") + " m (wejście z hali, ściana wschodnia): szatnia z " + szafek
                      + " szafkami, ławką i lustrem, 2 prysznice, WC z umywalką; drzwi 1.2 × 2.65 m, 2.5 m wolnego za drzwiami, tabliczki SZATNIA / WC / PRYSZNICE");
    }

    // ---------------- światła, zasięg kamery, podłoga ----------------
    static void GymAWydajnosc(float gx, float gz)
    {
        group = "Osiedle A / siłownia BRAXHORN — światła";
        // lampy wnętrza (ForceVertex, bez cieni) — oświetlają tylko nowe wyposażenie (hala ma wypalone światło w kolorach wierzchołków)
        foreach (var (x, z) in new[] { (14f, -4f), (14f, -11f), (-9f, -5.5f), (2f, -10.5f), (8.5f, -11f), (-1f, 8.5f), (6.7f, 3f), (16f, 7f) })
            PointLamp(new Vector3(gx + x, 4.3f, gz + z), 9f, .85f, new Color(1f, .96f, .9f));
        // podłoga hali z kolizją (piłki z fizyką nie mogą przelecieć; gracz stoi i tak)
        var fl = new GameObject("kolizja podłogi hali"); fl.transform.SetParent(colliders, false);
        fl.transform.position = new Vector3(gx, -.1f, gz + (-16.6f + 11.0f) * .5f); fl.AddComponent<BoxCollider>().size = new Vector3(37.2f, .2f, 27.6f);
        // zasięg kamery w zamkniętej sali (hala + przejście + przybudówka)
        var zw = new GameObject("zasięg kamery w hali (BraxClaudeZasiegWnetrza)"); zw.transform.SetParent(Group(group), false);
        var c = zw.AddComponent<BraxClaudeZasiegWnetrza>(); c.zasieg = 60f;
        c.sale = new[] { MinMax(gx - 18.6f, gx + 18.6f, -.5f, 8.6f, gz - 16.6f, gz + 11.0f), MinMax(gx + 18.0f, gx + 26.4f, -.5f, 3.2f, gz - 1.25f, gz + 11.75f) };
        rep.notes.Add("siłownia A: 8 lamp ForceVertex, kolizja podłogi, zasięg kamery 60 m w hali (bez parku i osiedla B za ścianą), trawa 3D wyłączona w środku; siatki wyposażenia bez cieni");
    }
}
