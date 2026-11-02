using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MODEL AUTA Z GEOMETRII (build 14, 2.11; build 12: auta jeździły tyłem — przód zgadywany po nazwach części; koła stały —
// nie wykryte po nazwach). Każdy model (dowolny .glb) przygotowany raz na szablon:
//  • długa oś → z; KOŁA = najniższe składowe spójne siatek o kształcie dysku (średnica ~ wysokość, cienkie w osi x) przy
//    narożnikach obrysu, po jednej na ćwiartkę; do koła dołączane współosiowe części (felga, tarcza, śruby) — wszystkie
//    trójkąty koła wydzielone do osobnej siatki z osią w środku koła (także gdy koła są w jednej siatce z nadwoziem);
//  • PRZÓD = głosowanie z geometrii: lusterka (boczny wystający szczyt szerokości na wysokości szyb — zawsze przy
//    przedniej szybie, przed środkiem rozstawu osi) i profil końców (przód niżej niż tył: maska/zderzak vs bagażnik/spojler);
//    przy małej pewności — tabela ręczna per model. Log: BRAXHORN_AUTA_PRZOD <model> <oś> <pewność>;
//  • skala: realna długość modelu (4,2–5,0 m) × K (miara bohatera), spód kół na y = 0;
//  • fotele kierowcy (lewa strona) i pasażera, światła STOP i kierunkowskazy (małe płytki), punkt spalin, lakier.
public static partial class BraxClaudeOsiedle
{
    sealed class AutoSzablon
    {
        public string id; public GameObject go;
        public float dl, szer, szerNadw, wys, rozstaw = 2.7f, osTyl = -1.3f, promienKola = .33f;
        public int kol; public Material lakier; public string przod, uwagi = "";
        public Bounds obrys;
    }

    // Oś przodu w układzie importu (glTFast: x odwrócone) — zabezpieczenie, gdy geometria nie rozstrzyga.
    static readonly Dictionary<string, string> PrzodTabela = new Dictionary<string, string>
    {
        { "car_918", "+z" }, { "car_dc", "+z" }, { "car_dia", "+z" }, { "car_gmc", "-x" }, { "car_mar", "+z" },
        { "car_mus", "+z" }, { "car_r8", "+z" }, { "car_rev", "+z" }, { "car_s4", "+z" }, { "car_zuk", "+z" },
    };
    // Realne długości (m): 918 Spyder, Challenger, Diablo, Sierra (krótka kabina), Marussia, Mustang SN95, R8, Revuelto, S4 B8, Żuk A.
    static readonly Dictionary<string, float> DlugoscRealna = new Dictionary<string, float>
    {
        { "car_918", 4.64f }, { "car_dc", 5.0f }, { "car_dia", 4.47f }, { "car_gmc", 4.9f }, { "car_mar", 4.7f },
        { "car_mus", 4.61f }, { "car_r8", 4.43f }, { "car_rev", 4.95f }, { "car_s4", 4.72f }, { "car_zuk", 4.43f },
    };

    sealed class CzescAuta { public MeshFilter mf; public MeshRenderer mr; public Mesh mesh; public Matrix4x4 m; public Vector3[] w; public int[][] tri; public Material[] mats; public bool ujemna; }
    sealed class Skladowa { public int cz; public List<int> sub = new List<int>(), t = new List<int>(); public List<int> wierz = new List<int>(); public Vector3 mn, mx; public Vector3 S => mx - mn; public Vector3 C => (mn + mx) * .5f; }
    sealed class KoloAuta { public Vector3 c; public float R, x0, x1; public List<int> skl = new List<int>(); public List<(int cz, int sub, int t)> dodatkowe = new List<(int, int, int)>(); public bool lustro; }
    sealed class AnalizaAuta
    {
        public List<CzescAuta> cz = new List<CzescAuta>(); public List<Skladowa> skl = new List<Skladowa>();
        public Vector3 mn, mx; public Vector3 S => mx - mn;
        public KoloAuta[] kola = new KoloAuta[4];   // LP, PP, LT, PT (lewy/prawy, przód/tył przy przodzie +z)
        public int ileKol; public HashSet<int> kolaSkl = new HashSet<int>();
        public float glosLusterka, glosProfil, zLusterka = float.NaN, szerNadw;
    }

    static AnalizaAuta AnalizujAuto(Transform root)
    {
        var A = new AnalizaAuta();
        var w2l = root.worldToLocalMatrix;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh;
            if (!mr || !mesh) continue;
            Vector3[] v; try { v = mesh.vertices; } catch { continue; }
            if (v == null || v.Length == 0) continue;
            var c = new CzescAuta { mf = mf, mr = mr, mesh = mesh, m = w2l * mf.transform.localToWorldMatrix, mats = mr.sharedMaterials };
            c.ujemna = c.m.determinant < 0f;
            c.w = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) c.w[i] = c.m.MultiplyPoint3x4(v[i]);
            c.tri = new int[mesh.subMeshCount][]; for (int s = 0; s < mesh.subMeshCount; s++) { try { c.tri[s] = mesh.GetTriangles(s); } catch { c.tri[s] = new int[0]; } }
            A.cz.Add(c);
        }
        if (A.cz.Count == 0) return null;
        bool pierwszy = true;
        foreach (var c in A.cz) foreach (var p in c.w) { if (pierwszy) { A.mn = A.mx = p; pierwszy = false; } else { A.mn = Vector3.Min(A.mn, p); A.mx = Vector3.Max(A.mx, p); } }
        float L = Mathf.Max(A.S.x, A.S.z), q = Mathf.Max(1e-6f, L * 2e-4f);
        // składowe spójne (wierzchołki sklejone po położeniu)
        for (int ci = 0; ci < A.cz.Count; ci++)
        {
            var c = A.cz[ci]; int n = c.w.Length;
            var klucz = new Dictionary<Vector3Int, int>(); var id = new int[n];
            for (int i = 0; i < n; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(c.w[i].x / q), Mathf.RoundToInt(c.w[i].y / q), Mathf.RoundToInt(c.w[i].z / q));
                if (!klucz.TryGetValue(k, out int j)) { j = klucz.Count; klucz[k] = j; }
                id[i] = j;
            }
            var par = new int[klucz.Count]; for (int i = 0; i < par.Length; i++) par[i] = i;
            int Find(int x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }
            for (int s = 0; s < c.tri.Length; s++)
            {
                var t = c.tri[s];
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    int a = Find(id[t[i]]), b = Find(id[t[i + 1]]), d = Find(id[t[i + 2]]);
                    if (a != b) par[b] = a; int a2 = Find(a); if (a2 != Find(d)) par[Find(d)] = a2;
                }
            }
            var mapa = new Dictionary<int, Skladowa>();
            var widziany = new HashSet<long>();
            for (int s = 0; s < c.tri.Length; s++)
            {
                var t = c.tri[s];
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    int r = Find(id[t[i]]);
                    if (!mapa.TryGetValue(r, out var sk)) { sk = new Skladowa { cz = ci, mn = c.w[t[i]], mx = c.w[t[i]] }; mapa[r] = sk; }
                    sk.sub.Add(s); sk.t.Add(i);
                    for (int k = 0; k < 3; k++)
                    {
                        int vi = t[i + k];
                        if (widziany.Add(((long)r << 32) | (uint)vi)) { sk.wierz.Add(vi); sk.mn = Vector3.Min(sk.mn, c.w[vi]); sk.mx = Vector3.Max(sk.mx, c.w[vi]); }
                    }
                }
            }
            A.skl.AddRange(mapa.Values);
        }
        Kola(A);
        Przod(A);
        return A;
    }

    // Koła: kandydaci = dyski przy ziemi w zewnętrznych ćwiartkach; w ćwiartce największy (opona); do koła — współosiowe
    // części mieszczące się w walcu koła (felga, tarcza, śruby); zaciski (poza osią) zostają na nadwoziu.
    static void Kola(AnalizaAuta A)
    {
        var S = A.S; float L = S.z, H = S.y, W = S.x; float cx = (A.mn.x + A.mx.x) * .5f, cz = (A.mn.z + A.mx.z) * .5f;
        var najl = new int[4]; for (int i = 0; i < 4; i++) najl[i] = -1;
        for (int k = 0; k < A.skl.Count; k++)
        {
            var o = A.skl[k]; var s = o.S; float d = Mathf.Max(s.y, s.z);
            if (d < .09f * L || d > .26f * L) continue;
            if (Mathf.Abs(s.y - s.z) > .22f * d || s.x > .7f * d) continue;
            if (o.mn.y > A.mn.y + .1f * H) continue;
            if (Mathf.Abs(o.C.z - cz) < .15f * L || Mathf.Abs(o.C.x - cx) < .2f * W) continue;
            int cw = (o.C.x < cx ? 0 : 1) + (o.C.z > cz ? 0 : 2);   // LP=0 (x<cx, z>cz), PP=1, LT=2, PT=3
            if (najl[cw] < 0 || Mathf.Max(A.skl[najl[cw]].S.y, A.skl[najl[cw]].S.z) < d) najl[cw] = k;
        }
        for (int i = 0; i < 4; i++)
        {
            if (najl[i] < 0) continue;
            var o = A.skl[najl[i]];
            A.kola[i] = new KoloAuta { c = o.C, R = Mathf.Max(o.S.y, o.S.z) * .5f, x0 = o.mn.x, x1 = o.mx.x };
        }
        // brakujące koło: lustrzane odbicie koła z drugiej strony (kształt z wydzielenia trójkątów w walcu)
        for (int i = 0; i < 4; i++)
        {
            if (A.kola[i] != null) continue;
            var lus = A.kola[i ^ 1]; if (lus == null) continue;
            A.kola[i] = new KoloAuta { c = new Vector3(2f * cx - lus.c.x, lus.c.y, lus.c.z), R = lus.R, x0 = 2f * cx - lus.x1, x1 = 2f * cx - lus.x0, lustro = true };
        }
        foreach (var kolo in A.kola)
        {
            if (kolo == null) continue;
            A.ileKol++;
            float R = kolo.R, mar = .15f * R;
            for (int k = 0; k < A.skl.Count; k++)
            {
                if (A.kolaSkl.Contains(k)) continue;
                var o = A.skl[k]; var s = o.S;
                if (o.mn.x < kolo.x0 - mar || o.mx.x > kolo.x1 + mar) continue;
                if (Mathf.Max(s.y, s.z) > 2.1f * R) continue;
                float dc = new Vector2(o.C.y - kolo.c.y, o.C.z - kolo.c.z).magnitude;
                if (!(dc < .2f * R || Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < .22f * R)) continue;
                var cz2 = A.cz[o.cz]; bool w = true;
                foreach (var vi in o.wierz) { var p = cz2.w[vi]; float dy = p.y - kolo.c.y, dz = p.z - kolo.c.z; if (dy * dy + dz * dz > 1.04f * 1.04f * R * R) { w = false; break; } }
                if (!w) continue;
                kolo.skl.Add(k); A.kolaSkl.Add(k);
            }
            if (kolo.skl.Count == 0)
            {
                // koło w jednej bryle z nadwoziem: trójkąty w całości w walcu koła
                for (int ci = 0; ci < A.cz.Count; ci++)
                {
                    var c = A.cz[ci];
                    for (int s = 0; s < c.tri.Length; s++)
                        for (int t = 0; t + 2 < c.tri[s].Length; t += 3)
                        {
                            bool w = true;
                            for (int q = 0; q < 3 && w; q++)
                            {
                                var p = c.w[c.tri[s][t + q]]; float dy = p.y - kolo.c.y, dz = p.z - kolo.c.z;
                                if (dy * dy + dz * dz > 1.02f * 1.02f * R * R || p.x < kolo.x0 - mar || p.x > kolo.x1 + mar) w = false;
                            }
                            if (w) kolo.dodatkowe.Add((ci, s, t));
                        }
                }
            }
        }
    }

    // Przód z geometrii (+1 = przód na +z): lusterka i profil końców.
    static void Przod(AnalizaAuta A)
    {
        var S = A.S; float L = S.z, H = S.y, W = S.x; float cx = (A.mn.x + A.mx.x) * .5f;
        float zc = (A.mn.z + A.mx.z) * .5f;
        if (A.kola.All(k => k != null)) zc = (A.kola.Max(k => k.c.z) + A.kola.Min(k => k.c.z)) * .5f;
        const int nb = 48; var szer = new float[nb]; var dol = new float[nb]; for (int i = 0; i < nb; i++) { szer[i] = -1f; dol[i] = -1f; }
        float hPrzod = 0f, hTyl = 0f, e = .07f * L;
        for (int k = 0; k < A.skl.Count; k++)
        {
            if (A.kolaSkl.Contains(k)) continue;
            var o = A.skl[k]; var c = A.cz[o.cz];
            foreach (var vi in o.wierz)
            {
                var p = c.w[vi]; float y = p.y - A.mn.y;
                int b = Mathf.Clamp((int)((p.z - A.mn.z) / Mathf.Max(1e-6f, L) * nb), 0, nb - 1);
                float ax = Mathf.Abs(p.x - cx);
                if (y > .45f * H && y < .9f * H) szer[b] = Mathf.Max(szer[b], ax);
                if (y > .15f * H && y < .55f * H) dol[b] = Mathf.Max(dol[b], ax);
                if (p.z > A.mx.z - e) hPrzod = Mathf.Max(hPrzod, y);
                if (p.z < A.mn.z + e) hTyl = Mathf.Max(hTyl, y);
            }
        }
        var wew = new List<float>(); int bpk = -1; float pk = -1f;
        for (int b = (int)(nb * .15f); b < (int)(nb * .85f); b++) { if (szer[b] < 0f) continue; wew.Add(szer[b]); if (szer[b] > pk) { pk = szer[b]; bpk = b; } }
        if (wew.Count > 3 && bpk >= 0)
        {
            wew.Sort(); float med = wew[wew.Count / 2], prom = pk - med;
            if (prom > .012f * W)
            {
                A.zLusterka = A.mn.z + (bpk + .5f) / nb * L;
                A.glosLusterka = Mathf.Sign(A.zLusterka - zc) * Mathf.Min(1f, prom / (.04f * W));
            }
        }
        A.glosProfil = Mathf.Clamp((hTyl - hPrzod) / (.12f * H), -1f, 1f);
        var d2 = dol.Where(x => x > 0f).OrderBy(x => x).ToList();
        A.szerNadw = d2.Count > 0 ? 2f * d2[(int)(d2.Count * .8f)] : W * .95f;
    }

    static void ObrocAuto(Transform inst, float yaw) { var q = Quaternion.Euler(0f, yaw, 0f); inst.localRotation = q * inst.localRotation; inst.localPosition = q * inst.localPosition; }

    static Mesh quadSwiatla; static Material matStop, matMigacz, matDym;
    static Mesh QuadSwiatla()
    {
        if (quadSwiatla) return quadSwiatla;
        var m = new Mesh { name = "płytka światła auta" };
        m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
        m.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };   // przód płytki na −z
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, Out + "/auta_plytka_swiatla.asset");
        quadSwiatla = m; return m;
    }
    static Material MatSwiatla(bool stop)
    {
        if (stop && matStop) return matStop; if (!stop && matMigacz) return matMigacz;
        var sh = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        var m = new Material(sh) { name = stop ? "Auto: światło STOP" : "Auto: kierunkowskaz", color = stop ? new Color(1f, .06f, .04f) : new Color(1f, .55f, .05f), enableInstancing = true };
        AssetDatabase.CreateAsset(m, Out + "/" + (stop ? "auta_stop.mat" : "auta_migacz.mat"));
        if (stop) matStop = m; else matMigacz = m; return m;
    }
    static Material MatDym()
    {
        if (matDym) return matDym;
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "spaliny kłąb", wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r); a = a * a * (3f - 2f * a);
                px[y * 64 + x] = new Color32(235, 235, 235, (byte)(a * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        AssetDatabase.CreateAsset(tex, Out + "/auta_spaliny.asset");
        var sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Mobile/Particles/Alpha Blended");
        var m = new Material(sh) { name = "Auto: spaliny", mainTexture = tex };
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(.5f, .5f, .5f, .5f));
        AssetDatabase.CreateAsset(m, Out + "/auta_spaliny.mat");
        matDym = m; return m;
    }

    static int autoSiatki;
    static AutoSzablon PrzygotujAuto(GameObject src, Transform rodzic)
    {
        var go = new GameObject("Auto · " + src.name); go.transform.SetParent(rodzic, false);
        var nad = new GameObject("nadwozie").transform; nad.SetParent(go.transform, false);
        var inst = (GameObject)UnityEngine.Object.Instantiate(src, nad, false); inst.name = src.name;
        inst.transform.localPosition = Vector3.zero;
        // Author explicit built-in materials with preserved glTF maps/UVs. Imported glTF shader
        // variants on these cars showed magenta on the phone; use the existing project converter.
        foreach (var renderer in inst.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] && materials[i].HasProperty("baseColorFactor") && materials[i].HasProperty("roughnessFactor"))
                    materials[i] = BraxCityMaterials.Imported(materials[i]);
            renderer.sharedMaterials = materials;
        }
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.enabled = false;
        var A = AnalizujAuto(go.transform);
        if (A == null || A.S.x < 1e-5f) { UnityEngine.Object.DestroyImmediate(go); return null; }
        // 1) długa oś → z
        float yaw0 = A.S.x > A.S.z * 1.15f ? 90f : 0f;
        if (yaw0 != 0f) { ObrocAuto(inst.transform, yaw0); A = AnalizujAuto(go.transform); }
        // 2) przód
        float glos = .6f * A.glosLusterka + .4f * A.glosProfil, pewn = Mathf.Clamp01(Mathf.Abs(glos));
        bool plusZ = glos >= 0f; string zrodlo = "geometria";
        string OsSurowa(bool pz) => yaw0 == 0f ? (pz ? "+z" : "-z") : (pz ? "-x" : "+x");
        bool maTab = PrzodTabela.TryGetValue(src.name, out var tab);
        if (maTab && pewn < .3f) { plusZ = tab == OsSurowa(true); zrodlo = "tabela (geometria niepewna)"; }
        string zgodnosc = maTab ? (tab == OsSurowa(plusZ) ? ", zgodne z tabelą" : ", NIEZGODNE z tabelą (" + tab + ")") : "";
        float vL = A.glosLusterka, vP = A.glosProfil; int kol1 = A.ileKol;
        if (!plusZ) { ObrocAuto(inst.transform, 180f); A = AnalizujAuto(go.transform); }
        Debug.Log("BRAXHORN_AUTA_PRZOD " + src.name + " " + OsSurowa(plusZ) + " " + pewn.ToString("F2") + " (" + zrodlo + "; głosy wzdłuż długiej osi: lusterka " + vL.ToString("+0.00;-0.00") +
                  ", profil końców " + vP.ToString("+0.00;-0.00") + zgodnosc + ", kół " + kol1 + ")");
        // 3) skala: realna długość × K
        float Lm = A.S.z;
        float cel = (DlugoscRealna.TryGetValue(src.name, out var dr) ? dr : (Lm > 3.4f && Lm < 6.5f ? Lm : 4.6f)) * K;
        float f = cel / Mathf.Max(1e-5f, Lm);
        inst.transform.localScale *= f; inst.transform.localPosition *= f;
        A = AnalizujAuto(go.transform);
        // 4) środek w x, z = 0; spód kół na y = 0
        float ziemia = A.ileKol == 4 ? A.kola.Min(k => k.c.y - k.R) : A.mn.y;
        inst.transform.localPosition += new Vector3(-(A.mn.x + A.mx.x) * .5f, -ziemia, -(A.mn.z + A.mx.z) * .5f);
        A = AnalizujAuto(go.transform);
        var sz = new AutoSzablon { id = src.name, go = go, dl = A.S.z, szer = A.S.x, szerNadw = Mathf.Min(A.S.x, A.szerNadw), wys = A.S.y, kol = A.ileKol, przod = OsSurowa(plusZ) + " " + pewn.ToString("F2") };
        sz.obrys = new Bounds((A.mn + A.mx) * .5f, A.S);
        // 5) koła → osobne siatki z osią w środku koła
        string[] nazwy = { "LP", "PP", "LT", "PT" };
        var usun = new Dictionary<int, HashSet<long>>();   // część → (sub << 32 | trójkąt)
        var spin = new Transform[4]; var piv = new Transform[4];
        if (A.ileKol == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                var kolo = A.kola[i];
                var tris = new List<(int cz, int sub, int t)>();
                foreach (var k in kolo.skl) { var o = A.skl[k]; for (int j = 0; j < o.t.Count; j++) tris.Add((o.cz, o.sub[j], o.t[j])); }
                tris.AddRange(kolo.dodatkowe);
                var p = new GameObject("koło " + nazwy[i]).transform; p.SetParent(go.transform, false); p.localPosition = kolo.c;
                var obr = new GameObject("obrót").transform; obr.SetParent(p, false);
                piv[i] = p; spin[i] = obr;
                if (tris.Count == 0) continue;
                var mesh = SiatkaKola(A, tris, kolo.c, out var mats);
                if (!mesh) continue;
                AssetDatabase.CreateAsset(mesh, Out + "/auto_" + Safe(src.name) + "_kolo_" + nazwy[i] + "_" + (autoSiatki++) + ".asset");
                obr.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = obr.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
                foreach (var tr in tris) { if (!usun.TryGetValue(tr.cz, out var hs)) usun[tr.cz] = hs = new HashSet<long>(); hs.Add(((long)tr.sub << 32) | (uint)tr.t); }
            }
            foreach (var kv in usun)
            {
                var c = A.cz[kv.Key]; var nowa = UnityEngine.Object.Instantiate(c.mesh); nowa.name = c.mesh.name + " (bez kół)";
                int zostalo = 0;
                for (int s = 0; s < c.tri.Length; s++)
                {
                    var l = new List<int>(c.tri[s].Length);
                    for (int t = 0; t + 2 < c.tri[s].Length; t += 3) if (!kv.Value.Contains(((long)s << 32) | (uint)t)) { l.Add(c.tri[s][t]); l.Add(c.tri[s][t + 1]); l.Add(c.tri[s][t + 2]); }
                    nowa.SetTriangles(l, s, false); zostalo += l.Count;
                }
                if (zostalo == 0) { UnityEngine.Object.DestroyImmediate(nowa); UnityEngine.Object.DestroyImmediate(c.mr); UnityEngine.Object.DestroyImmediate(c.mf); continue; }
                nowa.RecalculateBounds();
                AssetDatabase.CreateAsset(nowa, Out + "/auto_" + Safe(src.name) + "_nadwozie_" + (autoSiatki++) + ".asset");
                c.mf.sharedMesh = nowa;
            }
            sz.rozstaw = Mathf.Max(1.5f, (A.kola[0].c.z + A.kola[1].c.z) * .5f - (A.kola[2].c.z + A.kola[3].c.z) * .5f);
            sz.osTyl = (A.kola[2].c.z + A.kola[3].c.z) * .5f;
            sz.promienKola = A.kola.Average(k => k.R);
        }
        else
        {
            sz.rozstaw = .6f * sz.dl; sz.osTyl = -.3f * sz.dl; sz.promienKola = .33f * K;
            sz.uwagi += " kół nie wykryto (" + A.ileKol + ") — bez obrotu kół;";
        }
        Debug.Log("BRAXHORN_AUTA_KOLA " + src.name + " " + A.ileKol + " promień " + sz.promienKola.ToString("F2") + " m, rozstaw osi " + sz.rozstaw.ToString("F2") + " m, wydzielone części " + A.kola.Where(k => k != null).Sum(k => k.skl.Count + (k.dodatkowe.Count > 0 ? 1 : 0)) + (A.kola.Any(k => k != null && k.lustro) ? " (część z odbicia lustrzanego)" : ""));
        // 6) fotele: kierowca po lewej (Polska), wysokość siedziska, za przednią szybą
        float hSiedz = Mathf.Clamp(.36f * sz.wys, .30f * K, .85f * K);
        float zF = !float.IsNaN(A.zLusterka) ? A.zLusterka - .78f * K : sz.osTyl + .55f * sz.rozstaw;
        zF = Mathf.Clamp(zF, sz.osTyl + .15f * sz.rozstaw, sz.osTyl + .9f * sz.rozstaw);
        float xF = .21f * sz.szerNadw;
        var fk = new GameObject(FotelKierowcy).transform; fk.SetParent(nad, false); fk.localPosition = new Vector3(-xF, hSiedz, zF);
        var fp = new GameObject(FotelPasazera).transform; fp.SetParent(nad, false); fp.localPosition = new Vector3(xF, hSiedz, zF);
        BraxFotelAuta.Rodzaj(sz.wys / K, out string rodzajKabiny, out float odchB, out float odchM);
        foreach (var seat in new[] { fk, fp })
        {
            var fa = seat.gameObject.AddComponent<BraxFotelAuta>();
            fa.przeswit = PrzeswitKabiny(A, seat.localPosition, hSiedz);
            fa.kabina = rodzajKabiny; fa.odchylenieBaza = odchB; fa.odchylenieMax = odchM;
            MapaDachu(A, seat.localPosition, hSiedz, fa);   // build 45: dach nad konkretnym miejscem (głowa odchylonej osoby)
        }
        // 7) światła i spaliny
        Swiatla(A, sz, nad);
        Spaliny(A, sz, nad);
        // 8) lakier (do przemalowania): największy nieteksturowany materiał o nazwie lakieru
        sz.lakier = Lakier(A);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer && r.sharedMaterial != matStop && r.sharedMaterial != matMigacz) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        GameObjectUtility.SetStaticEditorFlags(go, 0);
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        rep.notes.Add("auto " + src.name + ": " + sz.dl.ToString("F2") + " × " + sz.szerNadw.ToString("F2") + " × " + sz.wys.ToString("F2") + " m, przód " + sz.przod + ", kół " + sz.kol + ", lakier " + (sz.lakier ? sz.lakier.name : "—") + ";" + sz.uwagi);
        return sz;
    }

    // Sample the roof above a head-sized footprint, rather than using the tallest point of the entire car.
    static float PrzeswitKabiny(AnalizaAuta car,Vector3 seat,float seatY)
    {
        float roof=car.mx.y;
        foreach(var offset in new[]{Vector2.zero,new Vector2(-.12f,0),new Vector2(.12f,0),new Vector2(0,-.12f),new Vector2(0,.12f)})
        {
            float x=seat.x+offset.x*K,z=seat.z+offset.y*K,top=float.NegativeInfinity;
            foreach(var part in car.cz)foreach(var indices in part.tri)for(int i=0;i+2<indices.Length;i+=3)
            {
                var a=part.w[indices[i]];var b=part.w[indices[i+1]];var c=part.w[indices[i+2]];
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(den)<1e-8f)continue;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den;
                float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;
                if(u<0||v<0||u+v>1)continue;
                float y=u*a.y+v*b.y+(1-u-v)*c.y;
                if(y>seatY+.4f*K)top=Mathf.Max(top,y);
            }
            if(!float.IsNegativeInfinity(top))roof=Mathf.Min(roof,top);
        }
        return Mathf.Max(.2f,roof-seatY-.08f*K);
    }

    // Build 45: mapa dachu nad miejscem (siatka 9 × 21 co 6 cm × K: x ±24 cm × K, z od 80 cm × K za siedziskiem do 40 cm × K
    // przed nim). Dla każdego węzła — najwyższa powierzchnia auta nad siedziskiem + 0.4 m × K (zewnętrzna skóra dachu / szyby)
    // minus 8 cm × K (blacha + podsufitka), jak PrzeswitKabiny. Brak powierzchni (auto bez dachu) = najwyższy punkt auta.
    // Rasteryzacja trójkątów po obrysie xz (bez pętli węzeł × wszystkie trójkąty).
    static void MapaDachu(AnalizaAuta car, Vector3 seat, float seatY, BraxFotelAuta f)
    {
        const int nx = 9, nz = 21; float krok = .06f * K, x0 = -.24f * K, z0 = -.8f * K, minY = seatY + .4f * K;
        var top = new float[nx * nz]; for (int i = 0; i < top.Length; i++) top[i] = float.NegativeInfinity;
        foreach (var part in car.cz) foreach (var indices in part.tri) for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            var a = part.w[indices[i]]; var b = part.w[indices[i + 1]]; var c = part.w[indices[i + 2]];
            if (a.y < minY && b.y < minY && c.y < minY) continue;
            float lx0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - seat.x, lx1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - seat.x;
            float lz0 = Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - seat.z, lz1 = Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - seat.z;
            int ix0 = Mathf.Max(0, Mathf.CeilToInt((lx0 - x0) / krok)), ix1 = Mathf.Min(nx - 1, Mathf.FloorToInt((lx1 - x0) / krok));
            int iz0 = Mathf.Max(0, Mathf.CeilToInt((lz0 - z0) / krok)), iz1 = Mathf.Min(nz - 1, Mathf.FloorToInt((lz1 - z0) / krok));
            if (ix0 > ix1 || iz0 > iz1) continue;
            float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(den) < 1e-8f) continue;
            for (int iz = iz0; iz <= iz1; iz++) for (int ix = ix0; ix <= ix1; ix++)
            {
                float x = seat.x + x0 + ix * krok, z = seat.z + z0 + iz * krok;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den;
                if (u < 0 || v < 0 || u + v > 1) continue;
                float y = u * a.y + v * b.y + (1 - u - v) * c.y;
                if (y > minY && y > top[iz * nx + ix]) top[iz * nx + ix] = y;
            }
        }
        f.mapaNx = nx; f.mapaNz = nz; f.mapaKrok = krok; f.mapaX0 = x0; f.mapaZ0 = z0; f.mapa = new float[nx * nz];
        for (int i = 0; i < top.Length; i++) f.mapa[i] = Mathf.Max(.2f, (float.IsNegativeInfinity(top[i]) ? car.mx.y : top[i]) - seatY - .08f * K);
    }

    static Mesh SiatkaKola(AnalizaAuta A, List<(int cz, int sub, int t)> tris, Vector3 srodek, out Material[] mats)
    {
        var poz = new List<Vector3>(); var nor = new List<Vector3>(); var uv = new List<Vector2>(); var tan = new List<Vector4>();
        var mapy = new Dictionary<long, int>(); var poMat = new Dictionary<Material, List<int>>(); var kolej = new List<Material>();
        var cache = new Dictionary<int, (Vector3[] n, Vector2[] uv, Vector4[] t)>(); var macierzN = new Dictionary<int, Matrix4x4>();
        foreach (var (ci, s, t) in tris)
        {
            var c = A.cz[ci];
            if (!cache.TryGetValue(ci, out var d))
            {
                Vector3[] n = null; Vector2[] u = null; Vector4[] tg = null;
                try { n = c.mesh.normals; } catch { } try { u = c.mesh.uv; } catch { } try { tg = c.mesh.tangents; } catch { }
                d = (n != null && n.Length == c.w.Length ? n : null, u != null && u.Length == c.w.Length ? u : null, tg != null && tg.Length == c.w.Length ? tg : null); cache[ci] = d;
            }
            var mat = s < c.mats.Length ? c.mats[s] : (c.mats.Length > 0 ? c.mats[c.mats.Length - 1] : null);
            if (!mat) continue;
            if (!poMat.TryGetValue(mat, out var lista)) { poMat[mat] = lista = new List<int>(); kolej.Add(mat); }
            if (!macierzN.TryGetValue(ci, out var nm)) { nm = c.m.inverse.transpose; macierzN[ci] = nm; }
            var idx = new int[3];
            for (int q = 0; q < 3; q++)
            {
                int vi = c.tri[s][t + q]; long key = ((long)ci << 32) | (uint)vi;
                if (!mapy.TryGetValue(key, out int ni))
                {
                    ni = poz.Count; mapy[key] = ni;
                    poz.Add(c.w[vi] - srodek);
                    nor.Add(d.n != null ? nm.MultiplyVector(d.n[vi]).normalized : Vector3.up);
                    uv.Add(d.uv != null ? d.uv[vi] : Vector2.zero);
                    if (d.t != null) { var tt = d.t[vi]; var tv = c.m.MultiplyVector(new Vector3(tt.x, tt.y, tt.z)).normalized; tan.Add(new Vector4(tv.x, tv.y, tv.z, c.ujemna ? -tt.w : tt.w)); }
                    else tan.Add(new Vector4(1, 0, 0, 1));
                }
                idx[q] = ni;
            }
            if (c.ujemna) { lista.Add(idx[0]); lista.Add(idx[2]); lista.Add(idx[1]); } else { lista.Add(idx[0]); lista.Add(idx[1]); lista.Add(idx[2]); }
        }
        mats = kolej.ToArray();
        if (poz.Count == 0) return null;
        var mesh = new Mesh { name = "koło (wydzielone)", indexFormat = poz.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(poz); mesh.SetNormals(nor); mesh.SetUVs(0, uv); mesh.SetTangents(tan);
        mesh.subMeshCount = kolej.Count;
        for (int i = 0; i < kolej.Count; i++) mesh.SetTriangles(poMat[kolej[i]], i, false);
        mesh.RecalculateBounds();
        return mesh;
    }

    static string NazwaMat(Material m) => m ? m.name.ToLowerInvariant() : "";
    static bool CzerwonawyMat(Material m)
    {
        if (!m) return false; Color c = Color.white;
        foreach (var p in new[] { "baseColorFactor", "_Color", "_BaseColor" }) if (m.HasProperty(p)) { c = m.GetColor(p); break; }
        return c.r > .45f && c.g < .25f && c.b < .25f;
    }

    // Światła STOP (czerwone płytki na tylnych lampach) i kierunkowskazy (pomarańczowe, 4 narożniki) — domyślnie wyłączone.
    static void Swiatla(AnalizaAuta A, AutoSzablon sz, Transform nad)
    {
        float W = sz.szerNadw, H = sz.wys, L = sz.dl, zmin = A.mn.z, zmax = A.mx.z;
        var lampy = new Bounds?[2];
        for (int k = 0; k < A.skl.Count; k++)
        {
            if (A.kolaSkl.Contains(k)) continue;
            var o = A.skl[k]; var s = o.S; var c = o.C;
            if (c.z > zmin + .12f * L || s.x > .4f * W || Mathf.Abs(c.x) < .15f * W || c.y < .3f * H || c.y > .85f * H) continue;
            var cz = A.cz[o.cz]; int sub = o.sub.Count > 0 ? o.sub[0] : 0; var mat = sub < cz.mats.Length ? cz.mats[sub] : null; var nm = NazwaMat(mat);
            bool lampa = nm.Contains("tail") || nm.Contains("brake") || nm.Contains("stop") || nm.Contains("rear_l") || nm.Contains("taillight") || nm.Contains("backlight") || nm.Contains("tyl") || CzerwonawyMat(mat);
            if (!lampa) continue;
            int i = c.x < 0f ? 0 : 1;
            var b = new Bounds(c, s);
            if (lampy[i] == null || b.size.x * b.size.y > lampy[i].Value.size.x * lampy[i].Value.size.y) lampy[i] = b;
        }
        var stop = new List<Renderer>(); var migL = new List<Renderer>(); var migP = new List<Renderer>();
        float NajdalejZ(float x, float y, bool przod)
        {
            float best = przod ? zmin : zmax; bool jest = false;
            for (int k = 0; k < A.skl.Count; k++)
            {
                if (A.kolaSkl.Contains(k)) continue;
                var o = A.skl[k]; if (o.mx.x < x - .15f || o.mn.x > x + .15f || o.mx.y < y - .12f || o.mn.y > y + .12f) continue;
                var c = A.cz[o.cz];
                foreach (var vi in o.wierz)
                {
                    var p = c.w[vi]; if (Mathf.Abs(p.x - x) > .12f || Mathf.Abs(p.y - y) > .1f) continue;
                    if (przod ? p.z > best : p.z < best) { best = p.z; jest = true; }
                }
            }
            return jest ? best : (przod ? zmax : zmin);
        }
        Renderer Plytka(string nazwa, Vector3 p, Vector2 rozm, bool przod, bool stopMat)
        {
            var g = new GameObject(nazwa); g.transform.SetParent(nad, false);
            g.transform.localPosition = p; g.transform.localRotation = przod ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            g.transform.localScale = new Vector3(rozm.x, rozm.y, 1f);
            g.AddComponent<MeshFilter>().sharedMesh = QuadSwiatla();
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = MatSwiatla(stopMat); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.enabled = false;
            return r;
        }
        for (int i = 0; i < 2; i++)
        {
            float sgn = i == 0 ? -1f : 1f;
            Vector3 p; Vector2 rozm;
            if (lampy[i] != null) { var b = lampy[i].Value; p = new Vector3(b.center.x, b.center.y, b.min.z - .012f); rozm = new Vector2(Mathf.Clamp(b.size.x * .8f, .12f, .32f), Mathf.Clamp(b.size.y * .7f, .05f, .12f)); }
            else { float x = sgn * .36f * W, y = .62f * H; p = new Vector3(x, y, NajdalejZ(x, y, false) - .012f); rozm = new Vector2(.22f, .08f) * K; }
            stop.Add(Plytka("światło STOP " + (i == 0 ? "L" : "P"), p, rozm, false, true));
            var mt = Plytka("kierunkowskaz tył " + (i == 0 ? "L" : "P"), p + new Vector3(sgn * (rozm.x * .5f + .05f), 0f, 0f), new Vector2(.08f, .06f) * K, false, false);
            (i == 0 ? migL : migP).Add(mt);
            float xf = sgn * .38f * W, yf = .48f * H;
            var mf = Plytka("kierunkowskaz przód " + (i == 0 ? "L" : "P"), new Vector3(xf, yf, NajdalejZ(xf, yf, true) + .012f), new Vector2(.09f, .05f) * K, true, false);
            (i == 0 ? migL : migP).Add(mf);
        }
        szablonSwiatla[sz.id] = (stop.ToArray(), migL.ToArray(), migP.ToArray());
    }
    static readonly Dictionary<string, (Renderer[] stop, Renderer[] l, Renderer[] p)> szablonSwiatla = new Dictionary<string, (Renderer[], Renderer[], Renderer[])>();

    // Spaliny: końcówka wydechu z geometrii (mała część z tyłu u dołu, chrom/metal/wydech), inaczej pod zderzakiem z prawej.
    static void Spaliny(AnalizaAuta A, AutoSzablon sz, Transform nad)
    {
        float W = sz.szerNadw, H = sz.wys, L = sz.dl; Vector3 p = new Vector3(.3f * W, .25f * K, A.mn.z + .05f); float best = -1f;
        for (int k = 0; k < A.skl.Count; k++)
        {
            if (A.kolaSkl.Contains(k)) continue;
            var o = A.skl[k]; var s = o.S; var c = o.C;
            if (c.z > A.mn.z + .1f * L || c.y > .3f * H || Mathf.Max(s.x, s.y) > .18f * W) continue;
            var cz = A.cz[o.cz]; int sub = o.sub.Count > 0 ? o.sub[0] : 0; var nm = NazwaMat(sub < cz.mats.Length ? cz.mats[sub] : null);
            if (!(nm.Contains("exhaust") || nm.Contains("chrome") || nm.Contains("pipe") || nm.Contains("tip") || nm.Contains("wydech") || nm.Contains("metal") || nm.Contains("mirror"))) continue;
            float wynik = Mathf.Abs(c.x);
            if (wynik > best) { best = wynik; p = new Vector3(c.x, c.y, o.mn.z - .02f); }
        }
        var g = new GameObject("spaliny"); g.transform.SetParent(nad, false);
        g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // emisja do tyłu (−z auta)
        // bez modułu cząsteczek (wyłączony w projekcie): 4 kłęby = płytki z miękką teksturą; ruch, rozrost i zanik liczy BraxAuto blisko gracza
        for (int i = 0; i < 4; i++)
        {
            var k = new GameObject("kłąb " + i); k.transform.SetParent(g.transform, false); k.transform.localScale = Vector3.one * .1f * K;
            k.AddComponent<MeshFilter>().sharedMesh = QuadSwiatla();
            var rr = k.AddComponent<MeshRenderer>(); rr.sharedMaterial = MatDym(); rr.shadowCastingMode = ShadowCastingMode.Off; rr.receiveShadows = false; rr.enabled = false;
        }
    }

    // Lakier: materiał bez tekstury koloru o nazwie lakieru/karoserii, największa powierzchnia nadwozia (≥ 4 %).
    static Material Lakier(AnalizaAuta A)
    {
        var pole = new Dictionary<Material, float>(); float suma = 0f;
        for (int k = 0; k < A.skl.Count; k++)
        {
            if (A.kolaSkl.Contains(k)) continue;
            var o = A.skl[k]; var c = A.cz[o.cz];
            for (int j = 0; j < o.t.Count; j++)
            {
                int s = o.sub[j], t = o.t[j]; var m = s < c.mats.Length ? c.mats[s] : null; if (!m) continue;
                var a = c.w[c.tri[s][t]]; var b = c.w[c.tri[s][t + 1]]; var d = c.w[c.tri[s][t + 2]];
                float p = Vector3.Cross(b - a, d - a).magnitude * .5f; suma += p;
                pole[m] = (pole.TryGetValue(m, out var x) ? x : 0f) + p;
            }
        }
        Material best = null; float bp = 0f;
        foreach (var kv in pole)
        {
            var nm = NazwaMat(kv.Key);
            if (!(nm.Contains("paint") || nm.Contains("body") || nm.Contains("lakier") || nm.Contains("karoser"))) continue;
            if (nm.Contains("glass") || nm.Contains("window") || nm.Contains("black") || nm.Contains("chrome") || nm.Contains("interior") || nm.Contains("inerior") || nm.Contains("rim") || nm.Contains("tire") || nm.Contains("light") || nm.Contains("plast")) continue;
            if (BaseTex(kv.Key)) continue;
            if (kv.Value > bp) { bp = kv.Value; best = kv.Key; }
        }
        return best && bp >= .04f * suma ? best : null;
    }
}
